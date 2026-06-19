namespace SchoolManagement.Api.Models;


public class StudentAttendance
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public int SectionId { get; set; }
    public DateOnly Date { get; set; }
    public AttendanceStatus Status { get; set; }
  
    public int TakenById { get; set; }
}

public class EmployeeAttendance
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateOnly Date { get; set; }
    public AttendanceStatus Status { get; set; }

    public bool OnLeave { get; set; }
}


public class Leave
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Reason { get; set; } = "";
    public int GrantedByAdminId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
