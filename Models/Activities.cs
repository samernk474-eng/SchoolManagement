namespace SchoolManagement.Api.Models;


public class Activity
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public string Name { get; set; } = "";
    public ActivityType Type { get; set; }

    public string Schedule { get; set; } = "";
    public int Capacity { get; set; }
    public int? SupervisorId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


public class ActivityRegistration
{
    public int Id { get; set; }
    public int ActivityId { get; set; }
    public Activity? Activity { get; set; }
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
