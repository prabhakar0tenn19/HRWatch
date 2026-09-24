# HRWatch 2.0 — System Master Context & Technical Architecture

## 1. System Overview
**HRWatch 2.0** is an enterprise-grade automated workforce attendance, compliance evaluation, and violator monitoring system built for **CG Infinity**. It aggregates biometric hardware in-punches from on-premise **Matrix COSEC** devices and leave/WFH/holiday approvals from the cloud-hosted **CG1 ERP Azure API**, evaluating compliance against configurable organizational Work-From-Office (WFO) policies and automatically dispatching weekly audit emails to HR.

---

## 2. Infrastructure & External Integrations

### 2.1 Matrix COSEC Biometric Device
- **Protocol:** HTTP REST
- **Live Endpoint:** `http://172.24.120.88/cosec/api.svc/v2/event-ta`
- **Authentication:** Basic Auth (`API` / `Api@123`)
- **Data Retrieved:** Physical IN/OUT timestamp punches by employee biometric PIN/code.

### 2.2 CG1 Enterprise Azure API
- **Live Base URL:** `https://cg-one-ntier-dev.azurewebsites.net`
- **Authentication:** Custom Request Header (`Secret-Key: 7X#r@2oH8*Ql%5sP!3bY`)
- **Master Employee Overview:** `GET /api/v2/EmployeeWeeklyOverview`
  * Returns master employee roster (Active/Inactive, Department, Designation, Project Deployment, `isOnProbation`, and `dateOfJoining`).
- **Filter by Email & Dates:** `GET /api/v2/EmployeeWeeklyOverview/by-emails?emailIds={email}&startDate={start}&endDate={end}`
  * Data Retrieved: Approved Leaves (`L`), Approved WFH (`W`), and Public/Company Holidays (`H`).
  * **Batching Architecture:** Queries are chunked in batches of 25 emails (`emailList.Chunk(25)`) to prevent Azure IIS query string length overflow (HTTP 404 URL too long).

### 2.3 Local Database (Microsoft SQL Server)
- **Server:** `IN-PRABHAKAR-LA`
- **Database:** `HRWatch`
- **Authentication:** SQL Server (`sa` / `Cyber1234`)
- **ORM:** Entity Framework Core (Code-First Migrations)
- **Schema Highlights:**
  * `Employees` table includes `IsOnProbation` (bit) and `DateOfJoining` (datetime2 nullable).

### 2.4 SMTP Notification & Scheduler Engine
- **SMTP Server:** `smtp.gmail.com:587` (TLS / STARTTLS)
- **Automated Scheduler:** Coravel Fluent Invocable
- **Execution Schedule:** Every Sunday at 10:00 PM IST (22:00) (`Cron: 0 22 * * 0`)
- **Payload:** Generates responsive HTML email table with previous completed week's violators (Name, Code, Email, Required, Present, Shortfall, Severity).
- **Portal Link:** Dynamically configured via `PortalUrl` in `appsettings.json` (pointing to production web portal).

### 2.5 Security, CORS & Health Probes
- **Authentication & JWT:** Custom Bearer JWT issuance (`POST /api/auth/login`, `POST /api/auth/register`) with BCrypt salted password hashing.
  * Default SuperAdmin: `admin` / `Admin@1234` (Email: `admin@cginfinity.com`).
- **CORS Architecture:** Configurable origins via `Cors:AllowedOrigins` in `appsettings.json` (defaults to `["http://localhost:3000"]`). Zero C# code changes required on deployment.
- **SSL Certificate Handling:** Secure public SSL validation for cloud endpoints (`https://cg-one-ntier-dev.azurewebsites.net`), with environment-aware fallback for local mock testing.
- **Health Check Endpoint:** `GET /health` returns HTTP 200 OK `Healthy` for Azure App Service liveness and container health probes.
- **Database Initializer:** Auto-migrates EF Core schema on startup. If database is fresh, automatically seeds:
  1. Default Policy Version 1 (Active).
  2. Default SuperAdmin User (`admin` / `Admin@1234`).

---

## 3. Core Business & Compliance Rules

### 3.1 Daily Attendance Reconciliation (Priority Hierarchy)
When reconciling an employee's daily status for any given date:
$$\text{COSEC Biometric Punch ('P')} \longrightarrow \text{CG1 Holiday ('H')} \longrightarrow \text{CG1 Leave ('L')} \longrightarrow \text{CG1 WFH ('W')} \longrightarrow \text{HR Exception ('E')} \longrightarrow \text{Absent ('A')}$$

1. **Present (`P`):** Physical punch recorded in COSEC device $\rightarrow$ marked Present.
2. **Holiday (`H`):** Recorded as holiday in CG1 API $\rightarrow$ marked Holiday (0 shortfall).
3. **Leave (`L`):** Approved leave in CG1 API $\rightarrow$ marked Leave (0 shortfall).
4. **WFH (`W`):** Approved WFH in CG1 API $\rightarrow$ marked WFH (0 shortfall).
5. **Exception (`E`):** Active HR override recorded in HRWatch $\rightarrow$ marked Exception (0 shortfall).
6. **Absent (`A`):** No punch, no leave, no WFH, no holiday, no exception $\rightarrow$ marked Absent.

---

### 3.2 Weekly WFO Compliance & Violator Evaluation
Operational weeks run from Monday to Friday (5 working days).

#### Standard Policy Quotas:
- **Employees on Probation (`IsOnProbation == true`):** Requires **5 physical office days/week** (overrides standard designation rules like Manager/Associate).
- **Client Deployed SDE, Intern, or Consultant:** Requires **5 physical office days/week**.
- **Client Deployed Associate or Manager:** Requires **3 physical office days/week**.
- **Bench / Internal HQ (Any Role):** Requires **5 physical office days/week**.

#### Compliance Evaluation Rules:
1. **Pass / Compliant Condition:**
   - If $\text{actualPresentDays } (P) \ge \text{requiredDays } (R)$ (quota fully met) $\rightarrow$ **`isViolator = false, shortfallDays = 0`**.
   - If $\text{absentDays } (A) == 0$ (no unauthorized absences) $\rightarrow$ **`isViolator = false, shortfallDays = 0`**.
2. **Fail / Violator Condition:**
   - Triggered only when $\text{actualPresentDays } (P) < \text{requiredDays } (R)$ **AND** unauthorized absence exists ($\text{absentDays } (A) > 0$).
   - **Shortfall Calculation:**
     $$\text{Shortfall} = \min(\text{requiredDays} - \text{actualPresentDays}, \text{absentDays})$$
   - **Severity Levels:**
     - $\text{Shortfall} = 1 \longrightarrow \text{Low}$
     - $\text{Shortfall} = 2 \longrightarrow \text{Medium}$
     - $\text{Shortfall} \ge 3 \longrightarrow \text{High}$

---

## 4. Frontend Application (`hrwatch-web`)
- **Technology:** Next.js 14 (App Router), React 18, TypeScript, Tailwind CSS, Lucide Icons.
- **Design System:** CG-1 Enterprise Theme (Amber `#F59E0B` active pills, Warm light cream `#FAF8F5` sidebar, Pure White `#FFFFFF` cards, Status badges for `P`, `H`, `L`, `W`, `E`, `A`, `WO`).
- **Feature Pages:**
  1. `/` (Weekly Violators Dashboard with Past 4 Weeks accordion cards & Top 5 Shortfall widget)
  2. `/calendar` (Attendance Calendar Grid with day cells, in-punch times, and Holiday indicators)
  3. `/employees` (Master Directory with sliding detail drawer and probation badges)
  4. `/exceptions` (HR Override modal and active/history table)
  5. `/policies` (Version history, Probation rules, and WFO category rules)
  6. `/admin` (Live Sync & Manual Evaluation controls)