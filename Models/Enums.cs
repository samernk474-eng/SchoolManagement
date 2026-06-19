namespace SchoolManagement.Api.Models;


public enum SchoolType
{
    Primary,  
    Middle,    
    Secondary, 
}

public enum EmployeeRole
{
    Principal,          
    Secretary,        
    Counselor,        
    Librarian,         
    ActivitySupervisor, 
    Teacher,       
}

public enum UserType
{
    Admin,
    Employee,
    Student,
}

public enum AttendanceStatus
{
    Present,
    Absent,
    Justified, 
}

public enum ComplaintStatus
{
    Open,
    Resolved,
    Rejected,
}

public enum WarningType
{
    Absence,        
    Behavior,       
    DismissalWarning,
}

public enum AnnouncementType
{
    General,
    Activity,
}

public enum AnnouncementAudience
{
    All,
    Students,
    Employees,
}

public enum ActivityType
{
    Trip, 
    Camp,
    Club, 
    Other,
}

public enum LoanStatus
{
    Active,
    Returned,
    Overdue,
}

public enum ReservationStatus
{
    Pending,
    Fulfilled,
    Cancelled,
}

public enum RegistrationStatus
{
    Pending,
    Approved,
    Rejected,
}

public enum MemberStatus
{
    Active,
    Suspended,
}
