namespace HRWatch.Domain.Enums;

public enum AttendanceStatus
{
    P = 1,   // Present (Biometric punch confirmed)
    L = 2,   // Leave (Approved leave in CG1 API)
    W = 3,   // WFH (Approved Work From Home in CG1 API)
    E = 4,   // Exception (HR approved exception)
    A = 5,   // Absent (Unauthorized absence / Violator)
    WO = 6,  // Weekend Off (Saturday / Sunday)
    H = 7    // Public Holiday
}

public enum UserRole
{
    HR = 1,
    Admin = 2,
    SuperAdmin = 3
}

public enum ViolationSeverity
{
    Low = 1,
    Medium = 2,
    High = 3
}
