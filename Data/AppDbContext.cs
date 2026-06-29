using Microsoft.EntityFrameworkCore;
using SchoolManagement.Api.Models;

namespace SchoolManagement.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<School> Schools => Set<School>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Student> Students => Set<Student>();

    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<SchedulePeriod> SchedulePeriods => Set<SchedulePeriod>();

    public DbSet<StudentAttendance> StudentAttendances => Set<StudentAttendance>();
    public DbSet<EmployeeAttendance> EmployeeAttendances => Set<EmployeeAttendance>();
    public DbSet<Leave> Leaves => Set<Leave>();

    public DbSet<Mark> Marks => Set<Mark>();
    public DbSet<MarkConfig> MarkConfigs => Set<MarkConfig>();
    public DbSet<ReportCard> ReportCards => Set<ReportCard>();
    public DbSet<ReportCardSubject> ReportCardSubjects => Set<ReportCardSubject>();
    public DbSet<PerformanceReport> PerformanceReports => Set<PerformanceReport>();

    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<Punishment> Punishments => Set<Punishment>();
    public DbSet<Warning> Warnings => Set<Warning>();
    public DbSet<GuardianSummon> GuardianSummons => Set<GuardianSummon>();
    public DbSet<Announcement> Announcements => Set<Announcement>();

    public DbSet<Book> Books => Set<Book>();
    public DbSet<LibraryMember> LibraryMembers => Set<LibraryMember>();
    public DbSet<BookLoan> BookLoans => Set<BookLoan>();
    public DbSet<BookReservation> BookReservations => Set<BookReservation>();

    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<ActivityRegistration> ActivityRegistrations => Set<ActivityRegistration>();

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<TeacherAssignment> TeacherAssignments => Set<TeacherAssignment>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
    
        mb.Entity<Admin>().HasIndex(a => a.Email).IsUnique();
        mb.Entity<Employee>().HasIndex(e => e.Email).IsUnique();
        mb.Entity<Employee>().HasIndex(e => e.Phone).IsUnique();
        mb.Entity<Student>().HasIndex(s => s.Email).IsUnique();

     
        mb.Entity<School>().Property(s => s.Type).HasConversion<string>().HasMaxLength(20);
        mb.Entity<Employee>().Property(e => e.Role).HasConversion<string>().HasMaxLength(30);
        mb.Entity<StudentAttendance>().Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        mb.Entity<EmployeeAttendance>().Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        mb.Entity<Complaint>().Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        mb.Entity<Complaint>().Property(c => c.FromUserType).HasConversion<string>().HasMaxLength(20);
        mb.Entity<Warning>().Property(w => w.Type).HasConversion<string>().HasMaxLength(30);
        mb.Entity<Announcement>().Property(a => a.Type).HasConversion<string>().HasMaxLength(20);
        mb.Entity<Announcement>().Property(a => a.Audience).HasConversion<string>().HasMaxLength(20);
        mb.Entity<Activity>().Property(a => a.Type).HasConversion<string>().HasMaxLength(20);
        mb.Entity<ActivityRegistration>().Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        mb.Entity<BookLoan>().Property(l => l.Status).HasConversion<string>().HasMaxLength(20);
        mb.Entity<BookReservation>().Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        mb.Entity<LibraryMember>().Property(m => m.Status).HasConversion<string>().HasMaxLength(20);
        mb.Entity<Notification>().Property(n => n.UserType).HasConversion<string>().HasMaxLength(20);

       
        mb.Entity<Employee>()
            .HasIndex(e => new { e.SchoolId, e.Role })
            .IsUnique()
            .HasFilter("[Role] IN (N'Principal', N'Secretary', N'Librarian', N'ActivitySupervisor') AND [IsDismissed] = 0");

        mb.Entity<MarkConfig>().HasIndex(c => c.SchoolId).IsUnique();
        mb.Entity<Mark>().HasIndex(m => new { m.StudentId, m.SubjectId, m.Semester }).IsUnique();
        mb.Entity<StudentAttendance>().HasIndex(a => new { a.StudentId, a.Date }).IsUnique();
        mb.Entity<EmployeeAttendance>().HasIndex(a => new { a.EmployeeId, a.Date }).IsUnique();
        mb.Entity<ReportCard>().HasIndex(r => new { r.StudentId, r.Semester, r.Year }).IsUnique();
        mb.Entity<Schedule>().HasIndex(s => new { s.SectionId, s.Day }).IsUnique();
        mb.Entity<LibraryMember>().HasIndex(m => m.StudentId).IsUnique();
        mb.Entity<ActivityRegistration>().HasIndex(r => new { r.ActivityId, r.StudentId }).IsUnique();

        mb.Entity<TeacherAssignment>().HasKey(t => new { t.EmployeeId, t.SchoolId });

        foreach (var property in mb.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal)))
        {
            property.SetColumnType("decimal(6,2)");
        }

       
        foreach (var fk in mb.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }

   
        mb.Entity<Schedule>()
            .HasMany(s => s.Periods)
            .WithOne()
            .HasForeignKey(p => p.ScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<ReportCard>()
            .HasMany(r => r.Subjects)
            .WithOne()
            .HasForeignKey(s => s.ReportCardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
