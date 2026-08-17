namespace HRWatch.Domain.Enums;

public enum AttendanceStatus
{
    P = 1,   // Present (Biometric punch confirmed)
    L = 2,   // Leave (Approved leave in CG1 API)
    E = 3,   // Exception (HR approved exception)
    A = 4,   // Absent (Unauthorized absence / Violator)
    WO = 5,  // Weekend Off (Saturday / Sunday)
    H = 6    // Public Holiday
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
