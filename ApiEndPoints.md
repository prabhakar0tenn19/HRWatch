# HRWatch 2.0 — Complete API Endpoints Documentation

Yeh document HRWatch system ke saare backend endpoints, unke parameters, optional fallbacks, exact response formats, aur real-world business logic ka comprehensive guide hai.

---

## Base Configuration
- **Base URL:** `http://localhost:5101` (HTTP) / `https://localhost:7119` (HTTPS)
- **Content-Type:** `application/json`
- **Swagger Documentation:** `http://localhost:5101/swagger/index.html`

---

## 📑 Quick Navigation (By Page / Feature)
1. [Page 1: Weekly Violators Dashboard](#1-weekly-violators-dashboard)
2. [Page 2: Attendance Calendar](#2-attendance-calendar)
3. [Page 3: Employee Records & Details Drawer](#3-employee-records--details-drawer)
4. [Page 4: Exceptions Management](#4-exceptions-management)
5. [Page 5: Policies & Version History](#5-policies--version-history)
6. [Page 6: Admin Tools & Manual Controls](#6-admin-tools--manual-controls)
7. [Authentication Endpoints](#7-authentication-endpoints)

---

## 1. Weekly Violators Dashboard

### 1.1 Past Weeks Summary (Dashboard Cards + Top 5 Widget)
- **Endpoint:** `GET /api/violations/summary-past-weeks`
- **Description:** Dashboard load hote hi single API call mein pichle N weeks (default 4 weeks) ka aggregated data deta hai. Left side par week-by-week cards (Total Violators, Critical Violators) aur right side par **Top 5 Shortfall Employees** widget render karta hai.

#### Parameters:
| Param | Type | In | Required? | Default | Description / Fallback if omitted |
|---|---|---|---|---|---|
| `weeksCount` | `int` | Query | Optional | `4` | Kitne weeks ka summary chahiye (Min: 1, Max: 12). Default 4 weeks leta hai. |
| `designation` | `string` | Query | Optional | `null` | Specific designation filter (e.g. `SDE`, `Manager`). Omit karne par all roles. |
| `searchTerm` | `string` | Query | Optional | `null` | Name, Email ya EmployeeCode search. Omit karne par saare employees. |

#### Sample Request:
```http
GET /api/violations/summary-past-weeks?weeksCount=4
```

#### Sample Response (`200 OK`):
```json
{
  "totalWeeksEvaluated": 4,
  "weeks": [
    {
      "weekStartDate": "2026-08-17",
      "weekEndDate": "2026-08-21",
      "weekLabel": "17 Aug - 21 Aug",
      "totalViolators": 12,
      "criticalViolators": 3,
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
      "totalShortfallDays": 4,
      "weeksWithViolations": 2
    }
  ]
}
```

---

### 1.2 Single Week Violators List
- **Endpoint:** `GET /api/violations/weekly`
- **Description:** Kisi single week ke violators fetch karta hai. Agar koi bhi day of week pass kiya jaye (e.g. Wednesday `2026-08-12`), backend **automatically Monday to Friday 5 working days normalize** kar leta hai.

#### Parameters:
| Param | Type | In | Required? | Default | Description / Fallback if omitted |
|---|---|---|---|---|---|
| `weekStartDate` | `date` | Query | Optional | Current Week Monday | Agar omit kiya, toh **Current Week (IST Monday)** automatic calculate hota hai. |
| `designation` | `string` | Query | Optional | `null` | Role filter (e.g. `SDE`, `Associate 1`). |
| `searchTerm` | `string` | Query | Optional | `null` | Name, Email, ya EmployeeCode search term. |

#### Sample Request:
```http
GET /api/violations/weekly?weekStartDate=2026-08-17&searchTerm=pratham
```

#### Sample Response (`200 OK`):
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
    "actualPresentDays": 4,
    "leaveDays": 0,
    "wfhDays": 0,
    "absentDays": 1,
    "shortfallDays": 1,
    "severity": "Low"
  }
]
```

---

## 2. Attendance Calendar

### 2.1 Calendar View With In-Punch Times & Exceptions
- **Endpoint:** `GET /api/attendance/calendar`
- **Description:** Monthly ya custom date range mein har din ka attendance status code (`P`, `L`, `W`, `E`, `A`, `WO`, `H`, `-`), pehla biometric punch time (`09:15 AM`), aur employee ki active exceptions single payload mein return karta hai.

#### Parameters:
| Param | Type | In | Required? | Default | Description / Fallback if omitted |
|---|---|---|---|---|---|
| `startDate` | `date` | Query | **Required** | - | Range start date (e.g. `2026-08-01`). |
| `endDate` | `date` | Query | **Required** | - | Range end date (e.g. `2026-08-31`). |
| `searchTerm` | `string` | Query | Optional | `null` | Specific employee code/name/email filter. |
| `designation` | `string` | Query | Optional | `null` | Role filter. |

#### Sample Request:
```http
GET /api/attendance/calendar?startDate=2026-08-17&endDate=2026-08-20&searchTerm=INT259
```

#### Sample Response (`200 OK`):
```json
[
  {
    "employeeId": "c633d608-79ee-44c5-ac0b-4381a2493a05",
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
        "punchTime": "09:18 AM"
      },
      {
        "date": "2026-08-18",
        "dayOfWeek": "Tuesday",
        "statusCode": "P",
        "leaveType": null,
        "punchTime": "09:15 AM"
      },
      {
        "date": "2026-08-19",
        "dayOfWeek": "Wednesday",
        "statusCode": "A",
        "leaveType": null,
        "punchTime": null
      },
      {
        "date": "2026-08-20",
        "dayOfWeek": "Thursday",
        "statusCode": "P",
        "leaveType": null,
        "punchTime": "09:15 AM"
      }
    ],
    "activeExceptions": []
  }
]
```

---

## 3. Employee Records & Details Drawer

### 3.1 Get All Active Employees (Main Table View)
- **Endpoint:** `GET /api/employees`
- **Description:** Database ke sabhi employees fetch karta hai with aggregated health metrics (`PresentDays`, `AbsentDays`, `LeaveDays`, `WfhDays`, `AbsentPercentage`).

#### Parameters:
| Param | Type | In | Required? | Default | Description / Fallback if omitted |
|---|---|---|---|---|---|
| `searchTerm` | `string` | Query | Optional | `null` | Search across FullName, Email, aur EmployeeCode. |
| `designation` | `string` | Query | Optional | `null` | Filter by designation. |
| `isDeployed` | `bool` | Query | Optional | `null` | `true` = Deployed, `false` = Bench, `null` = All. |
| `onlyActive` | `bool` | Query | Optional | `true` | Sirf active employees (default `true`). |

#### Sample Request:
```http
GET /api/employees?searchTerm=Prabhakar
```

#### Sample Response (`200 OK`):
```json
[
  {
    "id": "c633d608-79ee-44c5-ac0b-4381a2493a05",
    "employeeCode": "INT259",
    "fullName": "Prabhakar Lal",
    "email": "prabhakar.lal@cginfinity.com",
    "designation": "SDE",
    "isDeployed": false,
    "isActive": true,
    "location": "India",
    "presentDays": 3,
    "absentDays": 1,
    "leaveDays": 0,
    "wfhDays": 0,
    "exceptionDays": 0,
    "absentPercentage": 25.0,
    "createdAt": "2026-08-18T18:18:23.456Z"
  }
]
```

---

### 3.2 Get Employee By ID (Details Drawer)
- **Endpoint:** `GET /api/employees/{id}`
- **Description:** Kisi employee par click karne par khulne wale Right Drawer ke liye profile info, health metrics, aur last 10 days ka recent attendance history feed (with punch times) return karta hai.

#### Parameters:
| Param | Type | In | Required? | Description |
|---|---|---|---|---|
| `id` | `guid` | Path | **Required** | Employee GUID Id. |

#### Sample Response (`200 OK`):
```json
{
  "id": "c633d608-79ee-44c5-ac0b-4381a2493a05",
  "employeeCode": "INT259",
  "fullName": "Prabhakar Lal",
  "email": "prabhakar.lal@cginfinity.com",
  "designation": "SDE",
  "isDeployed": false,
  "isActive": true,
  "location": "India",
  "createdAt": "2026-08-18T18:18:23.456Z",
  "presentDays": 3,
  "absentDays": 1,
  "leaveDays": 0,
  "wfhDays": 0,
  "exceptionDays": 0,
  "absentPercentage": 25.0,
  "totalExceptionsCount": 0,
  "recentAttendances": [
    {
      "date": "2026-08-20",
      "dayOfWeek": "Thursday",
      "status": "P",
      "leaveType": null,
      "firstPunchTime": "09:15 AM"
    },
    {
      "date": "2026-08-19",
      "dayOfWeek": "Wednesday",
      "status": "A",
      "leaveType": null,
      "firstPunchTime": null
    }
  ]
}
```

---

## 4. Exceptions Management

### 4.1 Create Exception
- **Endpoint:** `POST /api/exceptions`
- **Description:** Employee ke liye approved exception create karta hai. Us date range ke purane `Status = 'A'` (Absent) records **instantaneously `Status = 'E'` (Exception) mein reconcile ho jate hain**.

#### Request Body (`application/json`):
```json
{
  "employeeId": "c633d608-79ee-44c5-ac0b-4381a2493a05",
  "fromDate": "2026-08-19",
  "toDate": "2026-08-19",
  "reason": "Client Office Visit / On-duty",
  "createdBy": "HR Admin"
}
```

#### Sample Response (`200 OK`):
```json
{
  "exceptionId": "8f5a2b1c-99ea-4d1e-bf11-456789abcdef",
  "message": "Exception created successfully."
}
```

#### Validation Error Responses:
- **`400 Bad Request` (`OVERLAPPING_EXCEPTION`):** Agar already us date range mein active exception exist karti hai.
- **`400 Bad Request` (`INVALID_DATE_RANGE`):** Agar `fromDate > toDate`.
- **`404 Not Found` (`NOT_FOUND`):** Agar `employeeId` exist nahi karta.

---

### 4.2 Revoke Exception (Soft Delete)
- **Endpoint:** `DELETE /api/exceptions/{id}`
- **Description:** Active exception ko revoke karta hai (`IsActive = false`). Us date range ke `'E'` records wapas `'A'` (Absent) ban jaate hain.

#### Sample Response (`200 OK`):
```json
{
  "message": "Exception revoked successfully."
}
```

---

### 4.3 Get Exceptions List / Audit History
- **Endpoint:** `GET /api/exceptions`
- **Parameters:**
  - `employeeId` (Optional `guid`): Specific employee filter.
  - `activeOnly` (Optional `bool`, default `true`): `true` = currently active only, `false` = full history with revoked exceptions.

#### Sample Response (`200 OK`):
```json
[
  {
    "id": "8f5a2b1c-99ea-4d1e-bf11-456789abcdef",
    "employeeId": "c633d608-79ee-44c5-ac0b-4381a2493a05",
    "employeeCode": "INT259",
    "fullName": "Prabhakar Lal",
    "email": "prabhakar.lal@cginfinity.com",
    "fromDate": "2026-08-19",
    "toDate": "2026-08-19",
    "reason": "Client Office Visit / On-duty",
    "createdBy": "HR Admin",
    "isActive": true,
    "createdAt": "2026-08-21T09:00:00Z"
  }
]
```

---

## 5. Policies & Version History

### 5.1 Get Active Policy
- **Endpoint:** `GET /api/policies/active`
- **Description:** Currently active WFO attendance rule return karta hai.

#### Sample Response (`200 OK`):
```json
{
  "id": "2195f190-77a8-48b7-b089-8d1421a221f7",
  "version": 1,
  "policyName": "Default CG India WFO Policy",
  "rulesJson": "{\"MinWfoDaysPerWeek\":{\"SDE\":5,\"Consultant\":5,\"Intern\":5,\"Associate\":3,\"Manager\":3,\"Principal\":3,\"Bench\":5},\"DefaultRequiredDays\":5}",
  "effectiveFrom": "2025-08-18",
  "effectiveTo": null,
  "isActive": true,
  "createdBy": "System",
  "createdAt": "2026-08-18T18:18:23.456Z"
}
```

---

### 5.2 Get Policy Version History
- **Endpoint:** `GET /api/policies/history`
- **Description:** Saari historical aur active policy versions sorted order (`Version DESC`) mein deta hai.

---

### 5.3 Create New Policy Version
- **Endpoint:** `POST /api/policies/new-version`
- **Description:** Purani active version ko archive (`IsActive = false`) karta hai aur new version activate karta hai.

#### Request Body (`application/json`):
```json
{
  "policyName": "Updated Q3 WFO Policy",
  "rulesJson": "{\"MinWfoDaysPerWeek\":{\"SDE\":5,\"Consultant\":5,\"Associate\":3,\"Manager\":3},\"DefaultRequiredDays\":5}",
  "effectiveFrom": "2026-09-01",
  "createdBy": "HR Manager"
}
```

---

## 6. Admin Tools & Manual Controls

### 6.1 Manual Sync Employees from CG1
- **Endpoint:** `POST /api/attendance/sync-employees`
- **Description:** CG1 Master API se live active India (`Offshore`) employees sync karta hai. Missing employees ko **Auto-Deactivate** karta hai.

#### Sample Response (`200 OK`):
```json
{
  "totalFetched": 174,
  "employeesCreated": 0,
  "employeesUpdated": 174,
  "employeesDeactivated": 0,
  "syncedAt": "2026-08-21T09:20:00Z"
}
```

---

### 6.2 Manual Evaluate Daily Attendance
- **Endpoint:** `POST /api/attendance/evaluate-daily`
- **Parameters:**
  - `targetDate` (Optional `date`): Evaluate karne ki date. Omit karne par **`IndiaDateTime.Today`** (IST date) evaluate hoti hai.

#### Sample Request:
```http
POST /api/attendance/evaluate-daily?targetDate=2026-08-20
```

#### Sample Response (`200 OK`):
```json
{
  "evaluationDate": "2026-08-20",
  "totalActiveEmployees": 174,
  "presentCount": 140,
  "leaveCount": 5,
  "wfhCount": 10,
  "exceptionCount": 2,
  "absentCount": 17,
  "weekendOrHolidayCount": 0,
  "evaluatedAt": "2026-08-21T09:20:00Z"
}
```

---

### 6.3 Manual Evaluate Range (Or Re-evaluate Week)
- **Endpoint:** `POST /api/attendance/evaluate-range`
- **Parameters:**
  - `startDate` (**Required** `date`): e.g. `2026-08-17`
  - `endDate` (**Required** `date`): e.g. `2026-08-21`

#### Sample Response (`200 OK`):
```json
{
  "startDate": "2026-08-17",
  "endDate": "2026-08-21",
  "totalDaysEvaluated": 5,
  "dailyResults": [ ... ],
  "completedAt": "2026-08-21T09:25:00Z"
}
```

---

## 7. Authentication Endpoints

### 7.1 Login
- **Endpoint:** `POST /api/auth/login`
- **Body:** `{"usernameOrEmail": "admin", "password": "Password123!"}`
- **Response:** `{"token": "JWT_TOKEN_STRING", "username": "admin", "email": "admin@cginfinity.com", "role": "Admin"}`

### 7.2 Register
- **Endpoint:** `POST /api/auth/register`
- **Body:** `{"username": "hr_manager", "email": "hr@cginfinity.com", "password": "Password123!", "role": "HR"}`
