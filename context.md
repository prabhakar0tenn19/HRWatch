# HRWatch 2.0 - Master Architecture & Project Context

> **Project Name:** HRWatch 2.0  
> **Status:** Production-Ready Core Backend Complete (All 35 Unit Tests Passing)  
> **Target Framework:** .NET 10 | EF Core 10 | MS SQL Server  
> **Solution Path:** c:\Users\PrabhakarLal\OneDrive - CG Infinity\Documents\HRWatch_external_folder\HRWatch\HRWatch.sln  
> **Git Repository:** https://github.com/prabhakar0tenn19/HRWatch (Branch: main)

---

## 1. PROJECT ARCHITECTURE & DESIGN
- **Architecture Pattern:** Clean Architecture + Vertical Slice Feature Folders + CQRS.
- **Key Frameworks & Libraries:**
  - LiteBus 6.0.2: In-process command and query mediator.
  - Coravel 6.0.2: Lightweight in-process background job scheduler.
  - EF Core 10: High-performance ORM with SQL Server.
  - BCrypt.Net-Next & System.IdentityModel.Tokens.Jwt: Secure authentication.
  - IndiaDateTime.cs: Robust cross-platform IST timezone engine supporting Windows (India Standard Time), Linux/Docker (Asia/Kolkata), and UTC+05:30 fallback.

---

## 2. EXTERNAL INTEGRATIONS & CREDENTIALS
### A. Matrix COSEC Biometric Device
- **Base URL:** http://172.24.120.88
- **Endpoint:** /cosec/api.svc/v2/event-ta
- **Auth:** HTTP Basic (API / Api@123)
- **Mapping:** COSEC UserID maps 1-to-1 to Employee.EmployeeCode (e.g. INT259, INT258, CGI816).

### B. CG1 Core HR System (Deployed Azure Server)
- **Base URL:** https://cg-one-ntier-dev.azurewebsites.net
- **Secret-Key Header:** Secret-Key: 7X#r@2oH8*Ql%5sP!3bY
- **Master API:** GET /api/v2/EmployeeWeeklyOverview (Offshore India employees sync)
- **Leaves & WFH API:** GET /api/v2/EmployeeWeeklyOverview/by-emails?emailIds=...&startDate=...&endDate=...

---

## 3. CORE BUSINESS RULES & EVALUATION HIERARCHY

### Attendance Evaluation 6-Tier Priority Flow:
1. **Biometric Punch in COSEC:** -> Present ('P') (First morning in-punch recorded in DailyPunchLogs).
2. **CG1 Status = 'H':** -> Holiday ('H') (Counts as valid holiday, 0 shortfall).
3. **CG1 Status = 'L':** -> Approved Leave ('L').
4. **CG1 Status = 'W':** -> Approved WFH ('W').
5. **Local DB Active Exception:** -> Approved HR Exception ('E').
6. **No Punch, No Leave, No Exception:** -> Unauthorized Absence ('A' -> Violator).

### Weekly Violator Golden Rule:
- An employee is **ONLY** flagged as a violator if bsentDays (A) > 0.
- **Shortfall = absentDays**.
- Severity: 1 Shortfall = Low, 2 Shortfall = Medium, 3+ Shortfall = High (Critical).

### Employee Soft-Deactivation:
- If an employee is present in local DB (IsActive = true) but missing from incoming active CG1 list, they are automatically soft-deactivated (IsActive = false). If CG1 call fails (0 records), deactivation is safely skipped.

---

## 4. DATABASE SCHEMA (Exact 6 Tables)
1. Employees (Id, EmployeeCode [Unique], FullName, Email [Unique], Designation, IsDeployed [Bench=0], IsActive, Location, CreatedAt)
2. DailyAttendance (Id, EmployeeId [FK], Date, Status ['P','L','W','E','A','WO','H'], LeaveType, RuleVersionId [FK], CreatedAt, UpdatedAt)
3. EmployeeExceptions (Id, EmployeeId [FK], FromDate, ToDate, Reason, CreatedBy, IsActive, CreatedAt)
4. Policies (Id, Version, PolicyName, RulesJson, EffectiveFrom, EffectiveTo, IsActive, CreatedBy, CreatedAt)
5. DailyPunchLogs (Id, EmployeeCode, EmployeeId [FK], PunchDate, PunchTime, DeviceName, EntryExitType, RawLogIndex, CreatedAt)
6. Users (Id, Username [Unique], Email [Unique], PasswordHash, Role [HR/Admin/SuperAdmin], IsActive, CreatedAt)

---

## 5. COMPLETE API ENDPOINTS SUMMARY

| Module | Method | Endpoint | Description |
|---|---|---|---|
| Dashboard | GET | /api/violations/summary-past-weeks | Past N weeks cards + Top 5 highest shortfall employees widget |
| Violations | GET | /api/violations/weekly | Single week violator list with automatic Monday-to-Friday normalization |
| Calendar | GET | /api/attendance/calendar | Day-by-day status codes, first punch times, and active exceptions |
| Employees | GET | /api/employees | Active employees with SQL-computed Present, Absent, Leave, WFH, and Absent % |
| Employees | GET | /api/employees/{id} | Right Drawer profile stats + last 10 days recent attendance feed & punch times |
| Exceptions | POST | /api/exceptions | Create exception (overlap validation + instant real-time 'A' to 'E' reconciliation) |
| Exceptions | DELETE | /api/exceptions/{id} | Soft revoke exception (reverts 'E' to 'A' in attendance) |
| Exceptions | GET | /api/exceptions | Active exceptions or full audit history list |
| Policies | GET | /api/policies/active | Active WFO rules and configuration |
| Policies | GET | /api/policies/history | Full historical audit trail of policy versions |
| Policies | POST | /api/policies/new-version | Create and activate new policy version |
| Admin Tools | POST | /api/attendance/sync-employees | Manual sync from deployed CG1 Azure API |
| Admin Tools | POST | /api/attendance/evaluate-daily | Manual single-day attendance evaluation |
| Admin Tools | POST | /api/attendance/evaluate-range | Manual date range / week re-evaluation |
| Auth | POST | /api/auth/login | Login and JWT generation |
| Auth | POST | /api/auth/register | User account registration |

---

## 6. SCHEDULED JOBS (Coravel)
- **11:30 PM (IST):** DailyAttendanceEvaluationJob - Evaluates daily physical attendance from COSEC biometric punch data, cross-checks CG1 leaves, holidays, and exceptions.
- **12:00 AM (IST):** DailyEmployeeSyncJob - Synchronizes active employee master data from deployed CG1 Azure API.

---

## 7. ASSOCIATED DOCUMENTATION FILES
- ApiEndPoints.md: Comprehensive Markdown API reference with exact JSON payloads and response models.
- ApiEndpoints.txt: ASCII table cheatsheet for quick reference during frontend development.
- Explanation.md: Step-by-step developer learning guide and workflow diagrams.
- DBSchema.md: Master database schema and relational design.
- discussion.md: Chronological record of design decisions and requirements.