namespace SchoolManagement.Api.Models;


public class Mark
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }
  
    public int Semester { get; set; }
    public decimal Oral { get; set; }     
    public decimal Quiz1 { get; set; }     
    public decimal Quiz2 { get; set; }     
    public decimal Homework { get; set; }  
    public decimal FinalExam { get; set; } 
   
    public decimal Total { get; set; }
    public int EnteredById { get; set; } 
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}


public class MarkConfig
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public decimal MaxOral { get; set; } = 10;
    public decimal MaxQuiz1 { get; set; } = 10;
    public decimal MaxQuiz2 { get; set; } = 10;
    public decimal MaxHomework { get; set; } = 10;
    public decimal MaxFinalExam { get; set; } = 60;

    public decimal PassPercent { get; set; } = 50;
}


public class ReportCard
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public int Semester { get; set; }
    public int Year { get; set; }
    public decimal Average { get; set; }

    public int Rank { get; set; }
    public bool Passed { get; set; }
    public int CreatedById { get; set; } 
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<ReportCardSubject> Subjects { get; set; } = [];
}

public class ReportCardSubject
{
    public int Id { get; set; }
    public int ReportCardId { get; set; }
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = "";
    public decimal Total { get; set; }
}


public class PerformanceReport
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public int TeacherId { get; set; }
    public int SubjectId { get; set; }
    public int Semester { get; set; }
    public string Behavior { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
