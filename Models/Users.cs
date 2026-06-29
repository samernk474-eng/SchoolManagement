using System.Text.Json.Serialization;

namespace SchoolManagement.Api.Models;

public class Admin
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    [JsonIgnore]
    public string PasswordHash { get; set; } = "";
    public string? FcmToken { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    [JsonIgnore]
    public string PasswordHash { get; set; } = "";
    public EmployeeRole Role { get; set; }
    public int SchoolId { get; set; }
    public School? School { get; set; }

    public string? Phone { get; set; }
    public string Address { get; set; } = "";
    public DateTime? BirthDate { get; set; }
    public string Qualification { get; set; } = "";
    public string Photo { get; set; } = "";

    public string? FcmToken { get; set; }

    public int UnexcusedAbsenceDays { get; set; }
    public bool DismissalWarning { get; set; } 
    public bool IsDismissed { get; set; }      

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    [JsonIgnore]
    public string PasswordHash { get; set; } = "";
    public int SchoolId { get; set; }
    public School? School { get; set; }
    public int? SectionId { get; set; }
    public Section? Section { get; set; }

    public string GuardianName { get; set; } = "";
    public string GuardianPhone { get; set; } = "";
    public string? GuardianFcmToken { get; set; }


    public string BloodType { get; set; } = "";
    public string ChronicDiseases { get; set; } = "";
    public string Allergies { get; set; } = "";
    public string HealthNotes { get; set; } = "";

    public DateTime? BirthDate { get; set; }
    public string Address { get; set; } = "";
    public string Photo { get; set; } = "";

    public string? FcmToken { get; set; }
    public bool DismissalWarning { get; set; } 
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
