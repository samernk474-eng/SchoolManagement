namespace SchoolManagement.Api.Models;


public class Complaint
{
    public int Id { get; set; }
    public int FromUserId { get; set; }
    public UserType FromUserType { get; set; }
    public string FromName { get; set; } = "";

    public string Against { get; set; } = "";
    public int SchoolId { get; set; }
    public string Content { get; set; } = "";
    public ComplaintStatus Status { get; set; } = ComplaintStatus.Open;
    public string Resolution { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


public class Punishment
{
    public int Id { get; set; }
    public int? StudentId { get; set; }
    public int? EmployeeId { get; set; }
    public int SchoolId { get; set; }
    public string Reason { get; set; } = "";
    public string Type { get; set; } = "";
    public int IssuedById { get; set; } 
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


public class Warning
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public WarningType Type { get; set; }

    public int? IssuedById { get; set; }
    public string Reason { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


public class GuardianSummon
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public string Reason { get; set; } = "";
    public DateOnly Date { get; set; }
    public int? IssuedById { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Announcement
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public AnnouncementAudience Audience { get; set; } = AnnouncementAudience.All;
    public AnnouncementType Type { get; set; } = AnnouncementType.General;
    public int CreatedById { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
