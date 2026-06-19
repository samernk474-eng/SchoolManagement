using Microsoft.EntityFrameworkCore;
using SchoolManagement.Api.Models;

namespace SchoolManagement.Api.Data;


public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Admins.AnyAsync()) return;

        var admin = new Admin
        {
            Name = "أدمن الوزارة",
            Email = "admin@moe.sy",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
        };
        db.Admins.Add(admin);
        await db.SaveChangesAsync();

        var school = new School
        {
            Name = "مدرسة دمشق الثانوية",
            Type = SchoolType.Secondary,
            Address = "دمشق - المزة",
            Phone = "0111234567",
            AdminId = admin.Id,
        };
        db.Schools.Add(school);
        await db.SaveChangesAsync();

        db.MarkConfigs.Add(new MarkConfig { SchoolId = school.Id });

        string Hash(string p) => BCrypt.Net.BCrypt.HashPassword(p);

        var manager = new Employee { Name = "مدير المدرسة", Email = "Principal@school.sy", PasswordHash = Hash("Manager@123"), Role = EmployeeRole.Principal, SchoolId = school.Id };
        var secretary = new Employee { Name = "أمين السر", Email = "secretary@school.sy", PasswordHash = Hash("Secretary@123"), Role = EmployeeRole.Secretary, SchoolId = school.Id };
        var counselor = new Employee { Name = "الموجه", Email = "counselor@school.sy", PasswordHash = Hash("Counselor@123"), Role = EmployeeRole.Counselor, SchoolId = school.Id };
        var librarian = new Employee { Name = "أمين المكتبة", Email = "librarian@school.sy", PasswordHash = Hash("Librarian@123"), Role = EmployeeRole.Librarian, SchoolId = school.Id };
        var supervisor = new Employee { Name = "مشرف النشاطات", Email = "activities@school.sy", PasswordHash = Hash("Activities@123"), Role = EmployeeRole.ActivitySupervisor, SchoolId = school.Id };
        var teacher = new Employee { Name = "معلم الرياضيات", Email = "teacher@school.sy", PasswordHash = Hash("Teacher@123"), Role = EmployeeRole.Teacher, SchoolId = school.Id };
        var teacher2 = new Employee { Name = "معلمة العربية", Email = "teacher2@school.sy", PasswordHash = Hash("Teacher@123"), Role = EmployeeRole.Teacher, SchoolId = school.Id };
        db.Employees.AddRange(manager, secretary, counselor, librarian, supervisor, teacher, teacher2);
        await db.SaveChangesAsync();

        db.TeacherAssignments.AddRange(
            new TeacherAssignment { EmployeeId = teacher.Id, SchoolId = school.Id },
            new TeacherAssignment { EmployeeId = teacher2.Id, SchoolId = school.Id });
        await db.SaveChangesAsync();

        var grade10 = new Grade { SchoolId = school.Id, Name = "الصف العاشر", Level = 10 };
        db.Grades.Add(grade10);
        await db.SaveChangesAsync();

        var sectionA = new Section { GradeId = grade10.Id, SchoolId = school.Id, Name = "العاشر - أ", CounselorId = counselor.Id };
        db.Sections.Add(sectionA);
        await db.SaveChangesAsync();

        var math = new Subject { Name = "الرياضيات", GradeId = grade10.Id, TeacherId = teacher.Id, SchoolId = school.Id };
        var arabic = new Subject { Name = "اللغة العربية", GradeId = grade10.Id, TeacherId = teacher2.Id, SchoolId = school.Id };
        db.Subjects.AddRange(math, arabic);
        await db.SaveChangesAsync();

        
        db.Schedules.Add(new Schedule
        {
            SectionId = sectionA.Id,
            Day = DayOfWeek.Sunday,
            Periods =
            [
                new SchedulePeriod { Order = 1, SubjectId = arabic.Id, TeacherId = teacher2.Id },
                new SchedulePeriod { Order = 2, SubjectId = math.Id, TeacherId = teacher.Id },
            ],
        });

        var student1 = new Student
        {
            Name = "أحمد محمد", Email = "student1@school.sy", PasswordHash = Hash("Student@123"),
            SchoolId = school.Id, SectionId = sectionA.Id,
            GuardianName = "محمد أحمد", GuardianPhone = "0991111111",
            BloodType = "O+",
        };
        var student2 = new Student
        {
            Name = "ليلى خالد", Email = "student2@school.sy", PasswordHash = Hash("Student@123"),
            SchoolId = school.Id, SectionId = sectionA.Id,
            GuardianName = "خالد يوسف", GuardianPhone = "0992222222",
            BloodType = "A+",
        };
        db.Students.AddRange(student1, student2);
        await db.SaveChangesAsync();

        db.Books.Add(new Book { SchoolId = school.Id, Title = "الأيام", Author = "طه حسين", Isbn = "978-977-416-001-1", Copies = 5, AvailableCopies = 5 });
        db.Activities.Add(new Activity { SchoolId = school.Id, Name = "رحلة إلى تدمر", Type = ActivityType.Trip, Schedule = "الخميس القادم 8 صباحاً", Capacity = 40, SupervisorId = supervisor.Id });
        await db.SaveChangesAsync();
    }
}
