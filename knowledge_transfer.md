# ?? HRWatch — Complete Knowledge Transfer & AI Handoff Guide

> **For the AI Assistant / Developer on the Target Machine**:
> Read this document completely BEFORE writing code. It contains the 100% authoritative context, domain models, architecture decisions, external API integration rules, and troubleshooting steps for **HRWatch**.

---

## ?? 1. Project Overview & Business Goal

**HRWatch** is an enterprise HR Monitoring, Work-From-Office (WFO) Compliance, Versioned Policy Management, and Attendance Analytics platform built for corporate workforce tracking.

### ?? Corporate Rules (Cyber Group India Guidelines):
1. **WFO Guidelines**:
   - SDE, C1, C2: Required **5 WFO days per week**.
   - A1 and above (A2, M1, M2, M3, P1, P2): Minimum **3 WFO days per week**.
   - Employees under Probation, Notice Period, Bench, or PIP: Required **5 WFO days per week**.
2. **Leave Policy**:
   - Earned Leave (EL): 15 days/yr (1.25/month).
   - Casual Leave (CL): 10 days/yr. (Probation limit: 1 CL/month).
   - Sick Leave (SL): 15 days/yr.
   - Total statutory paid leave allowance = 40 days/year.
3. **Default Attendance State**:
   - Every weekday is "P" (Present) by default, unless the employee logs "L" (Leave/Absence) or WFH.

---

## ??? 2. Technology Stack & Architectural Principles

- **Framework**: ASP.NET Core (.NET 10)
- **Architecture Pattern**: Clean Architecture + CQRS + Vertical Slice Pattern
- **Database**: SQL Server (LocalDB (localdb)\MSSQLLocalDB, Database: HRWatchDb)
- **ORM**: Entity Framework Core 10 (Code First with Fluent Configurations)
- **Mediator**: Lightweight Custom CQRS Command/Query Mediator (ICommandMediator, IQueryMediator)
- **Background Jobs**: Hangfire 1.8 (UseSqlServerStorage)
- **Validation**: FluentValidation
- **Authentication**: JWT Bearer Tokens (AddJwtAuthentication, BCrypt Password Hashing)
- **Logging**: Serilog (Console + Rolling File logs in HRWatch.API/logs/)

### ?? Project Structure:
`	ext
HRWatch.sln
+-- HRWatch.Domain/          # Core Domain Entities, ValueObjects, Enums, Domain Services
+-- HRWatch.Application/     # CQRS Commands/Queries, DTOs, Abstractions, Interfaces
+-- HRWatch.Infrastructure/  # EF Core DbContext, Repositories, External API Clients, Hangfire
+-- HRWatch.API/             # ASP.NET Core Controllers, Swagger UI, Middleware, appsettings.json
+-- HRWatch.Tests/           # Unit & Integration Tests
`

---

## ?? 3. Real Third-Party External API Integration

- **Endpoint**: GET /api/v2.0/EmployeeWeeklyOverview
- **Configuration Path**: HRWatch.API/appsettings.json $\rightarrow$ ExternalApis:EmployeeApi:BaseUrl
- **Sample Payload**:
  `json
  [
    {
      "id": 4887,
      "name": "Prabhakar Lal",
      "email": "prabhakar.lal@cginfinity.com",
      "designation": "SDE",
      "startDate": "2026-08-10T00:00:00+05:30",
      "endDate": "2026-08-14T00:00:00+05:30",
      "leave": ["P", "P", "L", "P", "P"],
      "isDeployed": false
    }
  ]
  `
- **Parsing Rules**:
  - leave: Array of 5 day status codes.
  - Count of "P" = PresentCount.
  - Count of "L" = LeaveCount.
  - RawLeaveJson = "[\"P\",\"P\",\"L\",\"P\",\"P\"]".

---

## ?? 4. Core Domain Entities

1. **User** (Users table):
   - Authentication identity with Username, Email, PasswordHash, Role (SuperAdmin, Admin, HR).
2. **Employee** (Employees table):
   - ExternalId, FirstName, LastName, Email, Department, Designation, IsActive.
3. **WeeklyAttendance** (WeeklyAttendances table):
   - EmployeeId (FK), WeekStartDate, WeekEndDate, PresentCount, LeaveCount, RawLeaveJson.
4. **Policy** (Policies table - Versioned Aggregate):
   - Version, ParentPolicyId, DesignationId, RulesJson (MinWfoDaysPerWeek, MaxAllowedLeavesPerMonth).
5. **Violation** (Violations table):
   - EmployeeId, Type (UnauthorizedAbsence), Severity (Low, Medium, High), OccurredOn, Description, IsAcknowledged.

---

## ??? 5. Implemented Modules Overview (Modules 1 to 5)

- **Module 1: DTO & External API Client**:
  - ExternalEmployeeWeeklyOverviewDto.cs & EmployeeWeeklyOverviewApiClient.cs.
- **Module 2: Domain Entity Alignment**:
  - WeeklyAttendance entity & EF Core WeeklyAttendanceConfiguration mapping in ApplicationDbContext.
- **Module 3: Ingestion Command & Sync Feature**:
  - SyncWeeklyOverviewCommand.cs & SyncWeeklyOverviewCommandHandler.cs. Upserts employees and weekly attendance records.
- **Module 4: Automated Policy Violation Engine**:
  - ViolationCalculationService.cs calculates WFO shortfall (PresentCount < MinWfoDaysPerWeek). Auto-flags Violation records during sync!
- **Module 5: Rest API Controllers & Query Handlers**:
  - GET /api/violations/weekly: Returns weekly violators list.
  - GET /api/reports/monthly-attendance: Aggregates monthly leave counts per employee, allowing sorting by TotalLeaveDays (Desc/Asc) to find top leave takers!

---

## ?? 6. How to Run, Test, and Troubleshoot on Target Machine

### 1?? Git Sync:
`ash
cd HRWatch
git fetch origin
git reset --hard origin/main
git clean -fd
`

### 2?? Configure External API BaseUrl:
Open HRWatch.API/appsettings.json and set "BaseUrl" under ExternalApis:EmployeeApi to the local port where the mock/external API is running on your machine:
`json
"ExternalApis": {
  "EmployeeApi": {
    "BaseUrl": "https://localhost:5092"
  }
}
`

### 3?? Update Database:
`ash
dotnet ef database update --project HRWatch.Infrastructure --startup-project HRWatch.API
`

### 4?? Run Application:
`ash
dotnet run --project HRWatch.API
`

### 5?? Open Swagger UI:
- Open **http://localhost:5000/** in browser. (Swagger UI serves directly at the root /).
- Open **http://localhost:5000/hangfire** for Hangfire background job dashboard.

### 6?? Test Ingestion & Data Population:
- In Swagger UI, execute **POST /api/attendance/sync-weekly** with body {"triggeredBy": "admin"}.
- Then execute **GET /api/violations/weekly** to view auto-generated WFO violators!
- Then execute **GET /api/reports/monthly-attendance?year=2026&month=8** to view monthly leave analytics sorted by top leave takers!

---

*Document compiled by AI Handoff System — Last Update: 2026-08-15*
