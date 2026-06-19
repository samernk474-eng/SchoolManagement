namespace SchoolManagement.Api.Models;


public class School
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public SchoolType Type { get; set; }
    public string Address { get; set; } = "";
    public string Phone { get; set; } = "";
    public int AdminId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


public class Grade
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public string Name { get; set; } = "";
    public int Level { get; set; }
}

public class Section
{
    public int Id { get; set; }
    public int GradeId { get; set; }
    public Grade? Grade { get; set; }
    public int SchoolId { get; set; }
    public string Name { get; set; } = "";

    public int? CounselorId { get; set; }
}

public class Subject
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int GradeId { get; set; }
    public Grade? Grade { get; set; }
    public int? TeacherId { get; set; }
    public int SchoolId { get; set; }
}


public class Schedule
{
    public int Id { get; set; }
    public int SectionId { get; set; }
    public Section? Section { get; set; }
   
    public DayOfWeek Day { get; set; }
    public List<SchedulePeriod> Periods { get; set; } = [];
}

public class SchedulePeriod
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
   
    public int Order { get; set; }
    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public int TeacherId { get; set; }
}
