# HRWatch 2.0 — Production Deployment & Infrastructure Guide

This document provides formal technical instructions for IT, Infrastructure, and DevOps teams responsible for deploying the HRWatch 2.0 backend and frontend services to production environments.

---

## 1. System Overview & Architecture

HRWatch 2.0 is an enterprise attendance evaluation and compliance system consisting of:

- **Backend API:** ASP.NET Core Web API (.NET 10), Entity Framework Core, LiteBus CQRS, Coravel In-Process Scheduler.
- **Frontend Portal:** Next.js 14 (App Router), TypeScript, Tailwind CSS.
- **Primary Database:** Microsoft SQL Server (Azure SQL Database or on-premise SQL Server).
- **External Dependencies:**
  1. **CG1 Enterprise Azure API:** Cloud-hosted REST API (`https://cg-one-ntier-dev.azurewebsites.net`) providing employee roster, leaves, and holidays.
  2. **Matrix COSEC Biometric Controller:** On-premise hardware controller (`http://172.24.120.88`) recording physical employee in-punches.
  3. **SMTP Relay:** Gmail SMTP (`smtp.gmail.com:587`) for weekly compliance audit emails.

---

## 2. Network Connectivity: Matrix COSEC Biometric Controller

### 2.1 The Issue
The Matrix COSEC device resides on a private, non-routable local area network IP (`http://172.24.120.88`). 
Public cloud environments (e.g., standard Azure App Service) cannot directly address private IP ranges (`172.16.0.0/12`) across the public internet. If the backend is deployed to Azure without network integration, all biometric requests will time out, resulting in zero recorded punches and incorrect violation calculations.

### 2.2 Recommended Infrastructure Solutions

#### Option A: Azure App Service Hybrid Connection (Recommended)
This approach does not require full corporate network restructuring or public IP exposition.
1. In the Azure Portal, navigate to the App Service instance > **Networking** > **Hybrid Connections**.
2. Create a new Hybrid Connection targeting host `172.24.120.88` on port `80`.
3. Download and install the **Azure Hybrid Connection Manager (HCM)** on any Windows Server located inside the office network that can communicate with `172.24.120.88`.
4. Connect the HCM agent using the connection string provided in the Azure Portal.
5. In HRWatch backend configuration, set `Cosec:BaseUrl` to the configured Hybrid Connection hostname.

#### Option B: Azure Virtual Network (VNet) Integration with Site-to-Site VPN
If an existing corporate Site-to-Site VPN or ExpressRoute exists between Azure and the CG Infinity office datacenter:
1. Configure Azure App Service **VNet Integration** to join a delegated subnet in the virtual network.
2. Ensure routing tables and network security groups permit outbound HTTP traffic on port 80 to `172.24.120.88`.
3. Set `Cosec:BaseUrl` directly to `http://172.24.120.88`.

#### Option C: Internal Corporate Hosting
Deploy the backend application directly to an internal Windows Server running IIS or an internal corporate virtual machine within the same network zone as the COSEC device.

---

## 3. Azure App Service Configuration: Always On

The backend utilizes an in-process background scheduler (Coravel) to run three mission-critical automated workflows:
- **Daily Attendance Evaluation:** Every night at 11:30 PM IST (23:30).
- **Employee Master Roster Sync:** Every midnight at 12:00 AM IST (00:00).
- **Weekly Violators Email Report:** Every Sunday at 10:00 PM IST (22:00).

### Required Action:
By default, Azure App Service terminates or idles worker processes after 20 minutes without incoming HTTP traffic.
1. Ensure the App Service is provisioned on a **Basic (B1)**, **Standard (S1)**, or higher service plan.
2. In the Azure Portal, open the App Service > **Configuration** > **General Settings**.
3. Set **Always On** to **On** and save the configuration.
4. Failure to enable Always On will cause scheduled nightly and weekend jobs to miss their execution window.

---

## 4. Database Setup & Automatic Seeding

1. Provision an Azure SQL Database (Serverless or Provisioned) or dedicated SQL Server instance.
2. Ensure the connection string includes:
   ```text
   Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
   ```
3. **Database Migration & Initialization:**
   - The application automatically executes pending Entity Framework Core migrations on startup (`Database.Migrate()`).
   - If the database is completely empty upon initial deployment, the startup initializer automatically seeds:
     - **Policy Version 1:** Default CG India WFO Policy (SDE: 5d, Intern: 5d, Consultant: 5d, Associate: 3d, Manager: 3d, Principal: 3d, Bench: 5d, Probation: 5d).
     - **Default SuperAdmin Account:**
       - Username: `admin`
       - Email: `admin@cginfinity.com`
       - Default Password: `Admin@1234` (must be updated after initial login).

---

## 5. Production Environment Variables & App Settings

Do not commit production secrets to source control. In Azure App Service, configure the following values under **Configuration** > **Application Settings**:

| Configuration Key | Sample / Expected Value | Description |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Activates production runtime optimizations. |
| `ConnectionStrings__DefaultConnection` | `Server=tcp:<server>.database.windows.net,1433;Initial Catalog=HRWatch;User ID=<user>;Password=<password>;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True;` | Primary database connection string. |
| `PortalUrl` | `https://hrwatch.cginfinity.com` | Base URL of frontend portal. Used for links in automated emails. |
| `Cors__AllowedOrigins__0` | `https://hrwatch.cginfinity.com` | Allowed production frontend origin for browser CORS requests. |
| `CG1__BaseUrl` | `https://cg-one-ntier-dev.azurewebsites.net` | CG1 Enterprise API endpoint. |
| `CG1__SecretKey` | `<SecretKey>` | Shared secret header for CG1 API. |
| `Cosec__BaseUrl` | `http://172.24.120.88` (or hybrid hostname) | Matrix COSEC REST endpoint. |
| `Cosec__Username` | `API` | Basic authentication username. |
| `Cosec__Password` | `<Password>` | Basic authentication password. |
| `Jwt__Key` | `<Minimum_32_Character_Strong_Secret>` | Secret key used to sign and validate JWT tokens. |
| `Jwt__Issuer` | `HRWatch` | JWT token issuer claim. |
| `Jwt__Audience` | `HRWatchPortal` | JWT token audience claim. |
| `EmailSettings__Host` | `smtp.gmail.com` | SMTP relay server host. |
| `EmailSettings__Port` | `587` | SMTP relay port (STARTTLS). |
| `EmailSettings__Username` | `<SenderEmailAddress>` | Authenticated email sender username. |
| `EmailSettings__Password` | `<AppPassword>` | SMTP application-specific password. |
| `EmailSettings__FromEmail` | `<SenderEmailAddress>` | Outgoing sender email address. |
| `EmailSettings__HrRecipients__0` | `hr@cginfinity.com` | Primary recipient for weekly audit emails. |

---

## 6. Frontend Deployment (`hrwatch-web`)

The frontend is a Next.js 14 application that can be hosted on Azure Static Web Apps, Azure App Service, or Vercel.

### 6.1 Build-Time Environment Variables
Next.js bakes environment variables prefixed with `NEXT_PUBLIC_` into client-side JavaScript during the build step:

```bash
NEXT_PUBLIC_API_URL=https://<your-backend-domain>/api
```

Ensure this environment variable is present in the build pipeline prior to running `npm run build`.

---

## 7. Post-Deployment Verification

1. **Health Probe:**
   Send a GET request to `https://<backend-domain>/health`. Verify response returns HTTP `200 OK` with body `Healthy`.
2. **Database Verification:**
   Verify the `Employees`, `Policies`, `DailyAttendance`, and `Users` tables have been created and Policy Version 1 is marked active.
3. **SMTP Verification:**
   Execute a POST request to `/api/notifications/test-email` to verify outbound email delivery to configured HR recipients.
4. **Portal Login:**
   Navigate to the frontend portal, log in using the provisioned administrative credentials, and verify the Dashboard metrics load properly.
