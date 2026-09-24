# HRWatch 2.0 - Office Biometric Relay & Attendance Sync Script
# Supports: Single Day Sync, Multi-Day Date Range Batch Sync, and Weekend Auto-Evaluation

param (
    [string]$StartDate = "",
    [string]$EndDate = "",
    [switch]$NonInteractive
)

Clear-Host
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   HRWatch 2.0 - CG Infinity Office Attendance Sync Tool   " -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Live Cloud Portal : https://hrwatch-web.vercel.app" -ForegroundColor Gray
Write-Host " Live API Backend  : https://hrwatch.onrender.com" -ForegroundColor Gray
Write-Host " Biometric Device  : http://172.24.120.88 (Matrix COSEC)" -ForegroundColor Gray
Write-Host "==========================================================`n" -ForegroundColor Cyan

# 1. Authenticate with Cloud API (Render)
$renderUrl = "https://hrwatch.onrender.com"
Write-Host "[*] Connecting and authenticating with Cloud API..." -ForegroundColor Gray

$loginBody = @{
    usernameOrEmail = "admin"
    password = "Admin@1234"
} | ConvertTo-Json

try {
    $authRes = Invoke-RestMethod -Uri "$renderUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json" -TimeoutSec 45
    $token = $authRes.token
    Write-Host "[+] Successfully authenticated as $($authRes.username) ($($authRes.role))`n" -ForegroundColor Green
} catch {
    Write-Host "[-] Failed to authenticate with Cloud API: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "[!] Note: If Render was asleep, wait 30 seconds and run again." -ForegroundColor Yellow
    exit 1
}

# 2. Determine Sync Mode & Dates
$today = (Get-Date).Date
$todayStr = $today.ToString("yyyy-MM-dd")

if (-not $NonInteractive -and [string]::IsNullOrWhiteSpace($StartDate)) {
    Write-Host "Select Sync Mode:" -ForegroundColor Yellow
    Write-Host "  [1] Sync Today Only ($todayStr) [Default - Press Enter]" -ForegroundColor White
    Write-Host "  [2] Sync Date Range (e.g. from 2026-08-19 or 2026-09-03 to Today)" -ForegroundColor White
    Write-Host "  [3] Sync Specific Custom Date" -ForegroundColor White
    Write-Host ""
    $mode = Read-Host "Choose option [1/2/3]"

    if ($mode -eq "2") {
        $defaultStart = "2026-09-03" # Last date with punches was 2026-09-02
        $inputStart = Read-Host "Enter Start Date (YYYY-MM-DD) [Default: $defaultStart]"
        $StartDate = if ([string]::IsNullOrWhiteSpace($inputStart)) { $defaultStart } else { $inputStart.Trim() }

        $inputEnd = Read-Host "Enter End Date (YYYY-MM-DD) [Default: $todayStr]"
        $EndDate = if ([string]::IsNullOrWhiteSpace($inputEnd)) { $todayStr } else { $inputEnd.Trim() }
    } elseif ($mode -eq "3") {
        $inputDate = Read-Host "Enter Date (YYYY-MM-DD)"
        $StartDate = $inputDate.Trim()
        $EndDate = $StartDate
    } else {
        $StartDate = $todayStr
        $EndDate = $todayStr
    }
}

if ([string]::IsNullOrWhiteSpace($StartDate)) { $StartDate = $todayStr }
if ([string]::IsNullOrWhiteSpace($EndDate)) { $EndDate = $StartDate }

# Parse and validate dates
try {
    $dtStart = [datetime]::ParseExact($StartDate, "yyyy-MM-dd", [System.Globalization.CultureInfo]::InvariantCulture).Date
    $dtEnd = [datetime]::ParseExact($EndDate, "yyyy-MM-dd", [System.Globalization.CultureInfo]::InvariantCulture).Date
} catch {
    Write-Host "[!] Invalid date format. Please use YYYY-MM-DD (e.g. 2026-09-24)" -ForegroundColor Red
    exit 1
}

if ($dtStart -gt $dtEnd) {
    Write-Host "[!] Start date ($StartDate) cannot be after End date ($EndDate)." -ForegroundColor Red
    exit 1
}

$totalDays = ($dtEnd - $dtStart).Days + 1
Write-Host "`n[*] Starting Sync Job: From $($dtStart.ToString('yyyy-MM-dd')) To $($dtEnd.ToString('yyyy-MM-dd')) ($totalDays total days)`n" -ForegroundColor Cyan

# 3. Setup Matrix COSEC Connectivity
$cosecUrl = "http://172.24.120.88"
$base64Auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("API:Api@123"))
$cosecheaders = @{ "Authorization" = "Basic $base64Auth" }

# Quick connectivity test to Matrix device
$isOfficeLanConnected = $false
try {
    Write-Host "[*] Testing connection to Matrix device at $cosecUrl..." -ForegroundColor Gray
    $testReq = Invoke-WebRequest -Uri "$cosecUrl" -Method Head -TimeoutSec 3 -ErrorAction Stop
    $isOfficeLanConnected = $true
    Write-Host "[+] Office Biometric Network Connected!" -ForegroundColor Green
} catch {
    Write-Host "[-] Matrix device ($cosecUrl) is unreachable (not currently on office Wi-Fi/LAN)." -ForegroundColor Yellow
}

$simulationEmployees = $null
if (-not $isOfficeLanConnected) {
    Write-Host "`n[?] You are not on office Wi-Fi. Do you want to generate simulated punches for off-site testing? (Y/N): " -NoNewline -ForegroundColor Cyan
    $simChoice = Read-Host
    if ($simChoice -match '^[Yy]') {
        Write-Host "[*] Fetching active employees list for realistic simulation..." -ForegroundColor Gray
        $authHeaders = @{ "Authorization" = "Bearer $token" }
        $simulationEmployees = Invoke-RestMethod -Uri "$renderUrl/api/employees?onlyActive=true" -Method Get -Headers $authHeaders
        Write-Host "[+] Loaded $($simulationEmployees.Count) employees for simulation." -ForegroundColor Green
    } else {
        Write-Host "[!] Aborting sync. Please run this script while connected to CG Infinity Office Wi-Fi/LAN tomorrow!" -ForegroundColor Yellow
        exit 0
    }
}

# 4. Process Each Day in Range
$currentDt = $dtStart
$dayIndex = 1
$summaryResults = @()

while ($currentDt -le $dtEnd) {
    $currentDateStr = $currentDt.ToString("yyyy-MM-dd")
    $currentDdMMyyyy = $currentDt.ToString("ddMMyyyy")
    $dayOfWeek = $currentDt.DayOfWeek

    Write-Host "`n----------------------------------------------------------" -ForegroundColor DarkGray
    Write-Host "[$dayIndex/$totalDays] Processing: $currentDateStr ($dayOfWeek)" -ForegroundColor White

    # Weekend Handling
    if ($dayOfWeek -eq [DayOfWeek]::Saturday -or $dayOfWeek -eq [DayOfWeek]::Sunday) {
        Write-Host "     -> Weekend detected ($dayOfWeek). Marking as Weekend Off (WO)..." -ForegroundColor DarkCyan
        $payload = @{
            TargetDate = $currentDateStr
            Punches = @()
            EvaluateAttendance = $true
        } | ConvertTo-Json

        try {
            $headers = @{ "Authorization" = "Bearer $token"; "Content-Type" = "application/json" }
            $res = Invoke-RestMethod -Uri "$renderUrl/api/attendance/ingest-punches" -Method Post -Headers $headers -Body $payload -TimeoutSec 30
            Write-Host "     [+] Successfully evaluated as Weekend Off for all employees." -ForegroundColor Green
            $summaryResults += [PSCustomObject]@{
                Date = $currentDateStr
                Day = $dayOfWeek
                Punches = 0
                Present = 0
                Absent = 0
                Leave = 0
                WFH = 0
                Status = "Weekend (WO)"
            }
        } catch {
            Write-Host "     [-] Error setting weekend off: $($_.Exception.Message)" -ForegroundColor Red
        }

        $currentDt = $currentDt.AddDays(1)
        $dayIndex++
        Start-Sleep -Milliseconds 250
        continue
    }

    # Weekday - Fetch Real Punches from Matrix COSEC
    $punches = @()

    if ($isOfficeLanConnected) {
        $fromStr = "${currentDdMMyyyy}000000"
        $toStr = "${currentDdMMyyyy}235959"
        $queryPath = "/cosec/api.svc/v2/event-ta?action=get;date-range=$fromStr-$toStr;field-name=userid,edate,device_name,etime,entryexittype"
        $fullUri = "$cosecUrl$queryPath"

        try {
            Write-Host "     -> Querying Matrix COSEC device for punches..." -ForegroundColor Gray
            $rawResponse = Invoke-RestMethod -Uri $fullUri -Headers $cosecheaders -Method Get -TimeoutSec 15

            if (-not [string]::IsNullOrWhiteSpace($rawResponse)) {
                $lines = $rawResponse -split "`r?`n"
                foreach ($line in $lines) {
                    $trimmed = $line.Trim()
                    if ([string]::IsNullOrWhiteSpace($trimmed) -or $trimmed.StartsWith("UserID", [StringComparison]::OrdinalIgnoreCase)) {
                        continue
                    }
                    $parts = $trimmed -split '\|'
                    if ($parts.Length -ge 4) {
                        $code = $parts[0].Trim()
                        $dStr = $parts[1].Trim()
                        $dev = $parts[2].Trim()
                        $tStr = $parts[3].Trim()
                        $ee = 1
                        if ($parts.Length -gt 4) { [int]::TryParse($parts[4].Trim(), [ref]$ee) | Out-Null }
                        $idx = if ($parts.Length -gt 5) { $parts[5].Trim() } else { "$code-$dStr-$tStr" }

                        $punchTimeFormatted = $tStr
                        if ($punchTimeFormatted.Length -eq 5) { $punchTimeFormatted = "$punchTimeFormatted:00" }

                        $punches += @{
                            EmployeeCode = $code
                            PunchDate = $currentDateStr
                            PunchTime = $punchTimeFormatted
                            DeviceName = $dev
                            EntryExitType = $ee
                            IndexNo = $idx
                        }
                    }
                }
            }
            Write-Host "     [+] Retrieved $($punches.Count) punches from Matrix COSEC." -ForegroundColor Green
        } catch {
            Write-Host "     [-] Matrix query error for $currentDateStr: $($_.Exception.Message)" -ForegroundColor Red
        }
    } elseif ($simulationEmployees) {
        # Simulation Mode
        $rand = New-Object System.Random
        foreach ($emp in $simulationEmployees) {
            if ($rand.Next(100) -lt 72) {
                $hour = $rand.Next(8, 11)
                $minute = $rand.Next(0, 59)
                $second = $rand.Next(0, 59)
                $timeStr = "{0:D2}:{1:D2}:{2:D2}" -f $hour, $minute, $second
                $punches += @{
                    EmployeeCode = $emp.employeeCode
                    PunchDate = $currentDateStr
                    PunchTime = $timeStr
                    DeviceName = "Main Entry Turnstile"
                    EntryExitType = 1
                    IndexNo = "SIM-$($emp.employeeCode)-$currentDateStr"
                }
            }
        }
        Write-Host "     [*] Generated $($punches.Count) simulated biometric punches." -ForegroundColor Gray
    }

    # Ingest punches & evaluate attendance in Cloud Backend
    Write-Host "     -> Sending punches to Cloud API & evaluating attendance..." -ForegroundColor Gray
    $payload = @{
        TargetDate = $currentDateStr
        Punches = $punches
        EvaluateAttendance = $true
    } | ConvertTo-Json -Depth 5

    try {
        $headers = @{ "Authorization" = "Bearer $token"; "Content-Type" = "application/json" }
        $res = Invoke-RestMethod -Uri "$renderUrl/api/attendance/ingest-punches" -Method Post -Headers $headers -Body $payload -TimeoutSec 60

        Write-Host "     [+] Attendance Evaluated!" -ForegroundColor Green
        Write-Host "         Present: $($res.presentCount) | Absent: $($res.absentCount) | Leave: $($res.leaveCount) | WFH: $($res.wfhCount) | Exception: $($res.exceptionCount)" -ForegroundColor White

        $summaryResults += [PSCustomObject]@{
            Date = $currentDateStr
            Day = $dayOfWeek
            Punches = $punches.Count
            Present = $res.presentCount
            Absent = $res.absentCount
            Leave = $res.leaveCount
            WFH = $res.wfhCount
            Status = "Evaluated OK"
        }
    } catch {
        Write-Host "     [-] Ingest failed for $currentDateStr: $($_.Exception.Message)" -ForegroundColor Red
        $summaryResults += [PSCustomObject]@{
            Date = $currentDateStr
            Day = $dayOfWeek
            Punches = $punches.Count
            Present = 0
            Absent = 0
            Leave = 0
            WFH = 0
            Status = "Failed"
        }
    }

    $currentDt = $currentDt.AddDays(1)
    $dayIndex++
    Start-Sleep -Milliseconds 300
}

# 5. Print Final Summary Table
Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host "                 ATTENDANCE SYNC SUMMARY                  " -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Cyan
$summaryResults | Format-Table -AutoSize

Write-Host "`n[+] Cloud Portal updated successfully!" -ForegroundColor Green
Write-Host "    View live dashboard: https://hrwatch-web.vercel.app`n" -ForegroundColor Cyan
