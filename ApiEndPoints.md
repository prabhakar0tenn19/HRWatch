# HRWatch 2.0 — Enterprise API Specification & Reference

This document serves as the authoritative, production-grade technical specification for all backend REST API endpoints in the **HRWatch 2.0 Attendance & Compliance Intelligence System**.

---

## 1. System Architecture & Base Configuration

- **Base URL (Local/Dev):** `http://localhost:5101` (HTTP) / `https://localhost:7119` (HTTPS)
- **Content-Type:** `application/json`
- **Timezone Standard:** Indian Standard Time (IST, UTC+5:30)
- **Interactive Swagger Documentation:** `http://localhost:5101/swagger/index.html`

### 1.1 Universal Attendance Status Codes
| Code | Status Name | Badge Styling | Description & Evaluation Outcome |
|:---:|---|---|---|
| **`P`** | Present | Green (`#10B981`) | Physical biometric IN-punch recorded via Matrix COSEC device. Counts towards physical office quota. |
| **`H`** | Holiday | Teal (`#0D9488`) | Official company/public holiday from CG1 calendar. Counts as non-working day; causes **0 shortfall**. |
| **`L`** | Leave | Amber (`#F59E0B`) | Authorized full-day leave approved in CG1 portal. Causes **0 shortfall**. |
| **`W`** | WFH | Blue (`#3B82F6`) | Authorized work-from-home approved in CG1 portal. Causes **0 shortfall**. |
| **`E`** | Exception | Purple (`#8B5CF6`) | Authorized HR override/exception logged in HRWatch. Causes **0 shortfall**. |
| **`A`** | Absent | Red (`#EF4444`) | Unauthorized absence (no punch, no leave, no exception). **Creates shortfall if WFO quota is unmet.** |
| **`WO`**| Weekend Off | Slate (`#64748B`) | Saturday / Sunday non-working day. |

---

### 1.2 Evaluation Hierarchy & Quota Business Logic

#### A. Daily Priority Hierarchy (First-Match Rule):
$$\text{COSEC Biometric Punch ('P')} \longrightarrow \text{CG1 Holiday ('H')} \longrightarrow \text{CG1 Leave ('L')} \longrightarrow \text{CG1 WFH ('W')} \longrightarrow \text{HR Exception ('E')} \longrightarrow \text{Absent ('A')}$$

#### B. Weekly Compliance & Violator Formula:
An employee is evaluated over the standard Monday–Friday operational window (5 working days).
1. **Compliant (Passed):**
   - If $\text{actualPresentDays } (P) \ge \text{requiredDays } (R)$ (e.g., a Manager required to come 3 days achieves $P \ge 3$) $\longrightarrow$ **`isViolator = false, shortfallDays = 0`**.
   - If $\text{absentDays } (A) == 0$ (all non-office days are covered by approved Leave, WFH, Holiday, or Exception) $\longrightarrow$ **`isViolator = false, shortfallDays = 0`**.
2. **Violator (Failed):**
   - Triggered when $\text{actualPresentDays } (P) < \text{requiredDays } (R)$ **AND** unauthorized absence exists ($\text{absentDays } (A) > 0$).
   - **Shortfall Formula:**
     $$\text{Shortfall} = \min(\text{requiredDays} - \text{actualPresentDays}, \text{absentDays})$$
   - **Severity Tiers:**
     - $\text{Shortfall} = 1 \longrightarrow \text{Low}$
     - $\text{Shortfall} = 2 \longrightarrow \text{Medium}$
     - $\text{Shortfall} \ge 3 \longrightarrow \text{High}$

---

## 2. API Endpoints by Feature Module

---

### 2.1 Feature 1: Weekly Violators Dashboard

#### 2.1.1 Past Weeks Summary (Aggregated Dashboard & Top 5 Widget)
- **Route:** `GET /api/violations/summary-past-weeks`
- **Purpose:** Supplies complete multi-week historical compliance data in a single network request. Populates the past weeks accordion cards on the left and the **Top 5 Shortfall Employees** leaderboard widget on the right.

##### Query Parameters:
| Parameter | Type | Required | Default | Description & Fallback |
|---|---|:---:|---|---|
| `weeksCount` | `integer` | No | `4` | Number of historical weeks to analyze (Min: 1, Max: 12). |
| `designation` | `string` | No | `null` | Filters records by designation (e.g., `SDE`, `Manager`). If omitted, returns all roles. |
| `searchTerm` | `string` | No | `null` | Filters by employee full name, email address, or employee code. |

##### Sample Request:
```http
GET /api/violations/summary-past-weeks?weeksCount=4 HTTP/1.1
Host: localhost:5101
```

##### Sample Response (`200 OK`):
```json
{
  "totalWeeksEvaluated": 4,
  "weeks": [
    {
      "weekStartDate": "2026-08-17",
      "weekEndDate": "2026-08-21",
      "weekLabel": "17 Aug - 21 Aug",
      "totalViolators": 18,
      "criticalViolators": 4,
      "violators": [
        {
          "employeeId": "6abf411b-8435-4ac6-af71-90ec04c034c2",
          "employeeCode": "INT256",
          "fullName": "Pratham Madan",
          "email": "pratham.madan@cginfinity.com",
          "designation": "SDE",
          "isDeployed": false,
          "weekStartDate": "2026-08-17",
          "weekEndDate": "2026-08-21",
          "requiredDays": 5,
          "actualPresentDays": 2,
          "leaveDays": 0,
          "wfhDays": 0,
          "absentDays": 3,
          "shortfallDays": 3,
          "severity": "High"
        }
      ]
    }
  ],
  "topShortfallEmployees": [
    {
      "employeeId": "6abf411b-8435-4ac6-af71-90ec04c034c2",
      "employeeCode": "INT256",
      "fullName": "Pratham Madan",
      "email": "pratham.madan@cginfinity.com",
      "designation": "SDE",
      "isDeployed": false,
      "totalShortfallDays": 5,
      "weeksWithViolations": 2
    }
  ]
}
```

---

#### 2.1.2 Single Week Violators List
- **Route:** `GET /api/violations/weekly`
- **Purpose:** Fetches the granular list of non-compliant employees for a specific Monday–Friday operational cycle.

##### Query Parameters:
| Parameter | Type | Required | Default | Description & Fallback |
|---|---|:---:|---|---|
| `weekStartDate` | `date` | No | Current Week Monday | Target week date. Automatically normalizes any arbitrary weekday to its preceding Monday. |
| `designation` | `string` | No | `null` | Substring filter on job title/designation. |
| `searchTerm` | `string` | No | `null` | Substring filter on Name, Code, or Email. |

##### Sample Response (`200 OK`):
```json
[
  {
    "employeeId": "6abf411b-8435-4ac6-af71-90ec04c034c2",
    "employeeCode": "INT256",
    "fullName": "Pratham Madan",
    "email": "pratham.madan@cginfinity.com",
    "designation": "SDE",
    "isDeployed": false,
    "weekStartDate": "2026-08-17",
    "weekEndDate": "2026-08-21",
    "requiredDays": 5,
    "actualPresentDays": 2,
    "leaveDays": 0,
    "wfhDays": 0,
    "absentDays": 3,
    "shortfallDays": 3,
    "severity": "High"
  }
]
```

---

### 2.2 Feature 2: Attendance Calendar

#### 2.2.1 Employee Attendance Calendar Feed
- **Route:** `GET /api/attendance/calendar`
- **Purpose:** Returns day-by-day status codes, punch timestamps, and active exceptions across any requested date range for calendar views.

##### Query Parameters:
| Parameter | Type | Required | Default | Description & Fallback |
|---|---|:---:|---|---|
| `startDate` | `date` | **Yes** | — | Start date of query range (`YYYY-MM-DD`). |
| `endDate` | `date` | **Yes** | — | End date of query range (`YYYY-MM-DD`). |
| `searchTerm` | `string` | No | `null` | Filter by employee name, code, or email. |
| `designation` | `string` | No | `null` | Filter by employee designation. |

##### Sample Request:
```http
GET /api/attendance/calendar?startDate=2026-08-17&endDate=2026-08-23&searchTerm=prabhakar HTTP/1.1
Host: localhost:5101
```

##### Sample Response (`200 OK`):
```json
[
  {
    "employeeId": "e229e34e-0a06-4078-a3f1-739bece1f422",
    "employeeCode": "INT259",
    "fullName": "Prabhakar Lal",
    "email": "prabhakar.lal@cginfinity.com",
    "designation": "SDE",
    "isDeployed": false,
    "days": [
      {
        "date": "2026-08-17",
        "dayOfWeek": "Monday",
        "statusCode": "P",
        "leaveType": null,
        "punchTime": "09:42 AM"
      },
      {
        "date": "2026-08-18",
        "dayOfWeek": "Tuesday",
        "statusCode": "P",
        "leaveType": null,
        "punchTime": "09:50 AM"
      },
      {
        "date": "2026-08-19",
        "dayOfWeek": "Wednesday",
        "statusCode": "H",
        "leaveType": "Holiday",
        "punchTime": null
      },
      {
        "date": "2026-08-20",
        "dayOfWeek": "Thursday",
        "statusCode": "P",
        "leaveType": null,
        "punchTime": "09:35 AM"
      },
      {
        "date": "2026-08-21",
        "dayOfWeek": "Friday",
        "statusCode": "P",
        "leaveType": null,
        "punchTime": "09:40 AM"
      },
      {
        "date": "2026-08-22",
        "dayOfWeek": "Saturday",
        "statusCode": "WO",
        "leaveType": null,
        "punchTime": null
      },
      {
        "date": "2026-08-23",
        "dayOfWeek": "Sunday",
        "statusCode": "WO",
        "leaveType": null,
        "punchTime": null
      }
    ],
    "activeExceptions": []
  }
]
```

---

### 2.3 Feature 3: Employee Records & Details Drawer

#### 2.3.1 Get All Employees (With Aggregated Metrics)
- **Route:** `GET /api/employees`
- **Purpose:** Powers the master employee directory with pre-calculated attendance totals, absence percentages, and deployment status.

##### Query Parameters:
| Parameter | Type | Required | Default | Description & Fallback |
|---|---|:---:|---|---|
| `searchTerm` | `string` | No | `null` | Substring search on Name, Code, or Email. |
| `designation` | `string` | No | `null` | Designation filter. |
| `isDeployed` | `boolean` | No | `null` | `true` = Client Deployed, `false` = Bench / Internal HQ. If omitted, returns all. |
| `onlyActive` | `boolean` | No | `true` | `true` = Active employees only, `false` = Includes deactivated employees. |

##### Sample Response (`200 OK`):
```json
[
  {
    "id": "e229e34e-0a06-4078-a3f1-739bece1f422",
    "employeeCode": "INT259",
    "fullName": "Prabhakar Lal",
    "email": "prabhakar.lal@cginfinity.com",
    "designation": "SDE",
    "isDeployed": false,
    "isActive": true,
    "location": "Noida / India",
    "presentDays": 18,
    "absentDays": 2,
    "leaveDays": 1,
    "wfhDays": 0,
    "exceptionDays": 0,
    "absentPercentage": 9.5,
    "createdAt": "2026-08-01T00:00:00Z"
  }
]
```

---

#### 2.3.2 Get Employee Detail by ID (Sliding Drawer Feed)
- **Route:** `GET /api/employees/{id}`
- **Purpose:** Supplies comprehensive profile metadata, compliance statistics, and the recent 10-day attendance feed with physical in-punch timestamps for the profile sliding drawer.

##### Route Parameters:
| Parameter | Type | Required | Description |
|---|---|:---:|---|
| `id` | `Guid` | **Yes** | Unique Employee Identifier. |

##### Sample Response (`200 OK`):
```json
{
  "id": "e229e34e-0a06-4078-a3f1-739bece1f422",
  "employeeCode": "INT259",
  "fullName": "Prabhakar Lal",
  "email": "prabhakar.lal@cginfinity.com",
  "designation": "SDE",
  "isDeployed": false,
  "isActive": true,
  "location": "Noida / India",
  "createdAt": "2026-08-01T00:00:00Z",
  "presentDays": 18,
  "absentDays": 2,
  "leaveDays": 1,
  "wfhDays": 0,
  "exceptionDays": 0,
  "absentPercentage": 9.5,
  "totalExceptionsCount": 0,
  "recentAttendances": [
    {
      "date": "2026-08-21",
      "dayOfWeek": "Friday",
      "status": "P",
      "leaveType": null,
      "firstPunchTime": "09:40:12 AM"
    }
  ]
}
```

---

### 2.4 Feature 4: Exceptions Management

#### 2.4.1 Get Exceptions List
- **Route:** `GET /api/exceptions`
- **Purpose:** Fetches active and historical HR exceptions with employee profile details.

##### Query Parameters:
| Parameter | Type | Required | Default | Description & Fallback |
|---|---|:---:|---|---|
| `employeeId` | `Guid` | No | `null` | Filter exceptions for a specific employee. |
| `activeOnly` | `boolean` | No | `true` | `true` = Active exceptions only, `false` = Full audit history. |

##### Sample Response (`200 OK`):
```json
[
  {
    "id": "8f3b207e-52db-43da-8e12-32cb90a42f61",
    "employeeId": "e229e34e-0a06-4078-a3f1-739bece1f422",
    "employeeCode": "INT259",
    "fullName": "Prabhakar Lal",
    "email": "prabhakar.lal@cginfinity.com",
    "fromDate": "2026-08-25",
    "toDate": "2026-08-27",
    "reason": "Client On-site visit at Dallas headquarters",
    "createdBy": "HR Admin",
    "isActive": true,
    "createdAt": "2026-08-21T10:15:00Z"
  }
]
```

---

#### 2.4.2 Create Exception
- **Route:** `POST /api/exceptions`
- **Purpose:** Creates a date-range exception. Automatically validates against overlapping exceptions and triggers background re-evaluation of affected attendance records.

##### Request Body:
```json
{
  "employeeId": "e229e34e-0a06-4078-a3f1-739bece1f422",
  "fromDate": "2026-08-25",
  "toDate": "2026-08-27",
  "reason": "Client On-site visit at Dallas headquarters",
  "createdBy": "HR Admin"
}
```

##### Response (`201 Created`):
```json
{
  "exceptionId": "8f3b207e-52db-43da-8e12-32cb90a42f61",
  "message": "Exception created successfully."
}
```

---

#### 2.4.3 Revoke Exception
- **Route:** `DELETE /api/exceptions/{id}`
- **Purpose:** Soft-deletes/revokes an active exception and re-evaluates attendance for the affected dates.

##### Response (`200 OK`):
```json
{
  "message": "Exception revoked successfully."
}
```

---

### 2.5 Feature 5: Policies & Version History

#### 2.5.1 Get Active Policy
- **Route:** `GET /api/policies/active`
- **Purpose:** Fetches the currently enforced organizational attendance policy and rule configurations.

##### Sample Response (`200 OK`):
```json
{
  "id": "1c7d2426-302a-436f-b258-450f757270e5",
  "version": 3,
  "policyName": "Standard WFO Policy 2026",
  "rulesJson": "[{\"category\":\"SDE\",\"normalWfoDays\":5,\"onBenchDays\":5},{\"category\":\"Manager\",\"normalWfoDays\":3,\"onBenchDays\":5}]",
  "effectiveFrom": "2026-08-01T00:00:00Z",
  "effectiveTo": null,
  "isActive": true,
  "createdBy": "System Admin",
  "createdAt": "2026-08-01T00:00:00Z"
}
```

---

#### 2.5.2 Get Policy Version History
- **Route:** `GET /api/policies/history`
- **Purpose:** Returns the complete version history of all active and archived organizational policies.

##### Sample Response (`200 OK`):
```json
[
  {
    "id": "1c7d2426-302a-436f-b258-450f757270e5",
    "version": 3,
    "policyName": "Standard WFO Policy 2026",
    "rulesJson": "[...]",
    "effectiveFrom": "2026-08-01T00:00:00Z",
    "effectiveTo": null,
    "isActive": true,
    "createdBy": "System Admin",
    "createdAt": "2026-08-01T00:00:00Z"
  },
  {
    "id": "0b6e1315-201a-325e-a147-340e646160d4",
    "version": 2,
    "policyName": "Legacy Hybrid WFO Policy",
    "rulesJson": "[...]",
    "effectiveFrom": "2026-01-01T00:00:00Z",
    "effectiveTo": "2026-07-31T23:59:59Z",
    "isActive": false,
    "createdBy": "System Admin",
    "createdAt": "2026-01-01T00:00:00Z"
  }
]
```

---

#### 2.5.3 Create New Policy Version
- **Route:** `POST /api/policies/new-version`
- **Purpose:** Publishes a new policy version, automatically sets its `isActive = true`, and archives the previous active version.

##### Request Body:
```json
{
  "policyName": "Updated Q4 WFO Policy",
  "rulesJson": "[{\"category\":\"SDE\",\"normalWfoDays\":5,\"onBenchDays\":5},{\"category\":\"Manager\",\"normalWfoDays\":3,\"onBenchDays\":5}]",
  "effectiveFrom": "2026-09-01T00:00:00Z",
  "createdBy": "HR Admin"
}
```

##### Response (`201 Created`):
```json
{
  "policyId": "3e9f4567-e89b-12d3-a456-426614174000",
  "message": "Policy version 4 created and activated successfully."
}
```

---

### 2.6 Feature 6: Admin Tools & Manual Sync Controls

#### 2.6.1 Live Employee Synchronization
- **Route:** `POST /api/attendance/sync-employees`
- **Purpose:** Connects to the deployed CG1 Azure API (`https://cg-one-ntier-dev.azurewebsites.net/api/v2/EmployeeWeeklyOverview`) using server-side secure headers, synchronizes the master employee roster, and reconciles active/inactive statuses.

##### Sample Response (`200 OK`):
```json
{
  "totalFetched": 265,
  "employeesCreated": 17,
  "employeesUpdated": 248,
  "employeesDeactivated": 120,
  "syncedAt": "2026-08-21T10:20:00Z"
}
```

---

#### 2.6.2 Evaluate Daily Attendance
- **Route:** `POST /api/attendance/evaluate-daily`
- **Purpose:** Executes the full reconciliation engine for a given target date. Cross-references Matrix COSEC in-punches, CG1 Leaves/WFH/Holidays, and active HR Exceptions.

##### Query Parameters:
| Parameter | Type | Required | Default | Description & Fallback |
|---|---|:---:|---|---|
| `targetDate` | `date` | No | Current Date (IST) | Target date to evaluate (`YYYY-MM-DD`). |

##### Sample Request:
```http
POST /api/attendance/evaluate-daily?targetDate=2026-08-20 HTTP/1.1
Host: localhost:5101
```

##### Sample Response (`200 OK`):
```json
{
  "evaluationDate": "2026-08-20",
  "totalActiveEmployees": 265,
  "presentCount": 138,
  "leaveCount": 14,
  "wfhCount": 22,
  "exceptionCount": 2,
  "absentCount": 89,
  "weekendOrHolidayCount": 0,
  "evaluatedAt": "2026-08-21T10:25:00Z"
}
```

---

#### 2.6.3 Evaluate Date Range
- **Route:** `POST /api/attendance/evaluate-range`
- **Purpose:** Iteratively executes batch evaluation across multiple sequential calendar days.

##### Query Parameters:
| Parameter | Type | Required | Description |
|---|---|:---:|---|
| `startDate` | `date` | **Yes** | Range start date (`YYYY-MM-DD`). |
| `endDate` | `date` | **Yes** | Range end date (`YYYY-MM-DD`). |

##### Sample Response (`200 OK`):
```json
{
  "startDate": "2026-08-17",
  "endDate": "2026-08-21",
  "totalDaysEvaluated": 5,
  "dailyResults": [
    {
      "evaluationDate": "2026-08-17",
      "totalActiveEmployees": 265,
      "presentCount": 142,
      "leaveCount": 10,
      "wfhCount": 18,
      "exceptionCount": 1,
      "absentCount": 94,
      "weekendOrHolidayCount": 0,
      "evaluatedAt": "2026-08-21T10:30:00Z"
    }
  ],
  "completedAt": "2026-08-21T10:30:05Z"
}
```

---

### 2.7 Feature 7: Authentication & Access Control

#### 2.7.1 User Login
- **Route:** `POST /api/auth/login`
- **Request Body:**
```json
{
  "email": "admin@cginfinity.com",
  "password": "Password@123"
}
```
- **Response (`200 OK`):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiration": "2026-08-22T10:00:00Z",
  "email": "admin@cginfinity.com",
  "fullName": "System Administrator",
  "role": "Admin"
}
```

#### 2.7.2 User Registration
- **Route:** `POST /api/auth/register`
- **Request Body:**
```json
{
  "email": "user@cginfinity.com",
  "password": "Password@123",
  "fullName": "John Doe",
  "role": "User"
}
```
- **Response (`200 OK`):**
```json
{
  "userId": "5a4b3c2d-1e0f-9a8b-7c6d-5e4f3a2b1c0d",
  "message": "User registered successfully."
}
```
