using Microsoft.EntityFrameworkCore;
using SchoolManagement.Api.Data;
using SchoolManagement.Api.Models;

namespace SchoolManagement.Api.Services;


public class AttendanceWarningJob(IServiceScopeFactory scopeFactory, ILogger<AttendanceWarningJob> logger)
    : BackgroundService
{
    public const decimal StudentAbsenceLimit = 0.15m;
    public const int EmployeeWarningDays = 18;      
    public const int EmployeeDismissalDays = 30;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        do
        {
            try
            {
                await RunChecksAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Attendance warning checks failed");
            }
        } while (await timer.WaitForNextTickAsync(ct));
    }


    public async Task RunChecksAsync(CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notifier = scope.ServiceProvider.GetRequiredService<NotificationService>();

        await CheckStudentsAsync(db, notifier, ct);
        await CheckEmployeesAsync(db, notifier, ct);
        logger.LogInformation("Daily attendance warning checks completed");
    }

    private static async Task CheckStudentsAsync(AppDbContext db, NotificationService notifier, CancellationToken ct)
    {
        var stats = await db.StudentAttendances
            .GroupBy(a => a.StudentId)
            .Select(g => new
            {
                StudentId = g.Key,
                Total = g.Count(),
                Unexcused = g.Count(a => a.Status == AttendanceStatus.Absent),
            })
            .Where(s => s.Total > 0)
            .ToListAsync(ct);

        foreach (var stat in stats)
        {
            var student = await db.Students.FindAsync([stat.StudentId], ct);
            if (student is null) continue;

      
            var rate = (decimal)stat.Unexcused / stat.Total;
            if (rate > StudentAbsenceLimit && !student.DismissalWarning)
            {
                student.DismissalWarning = true;
                db.Warnings.Add(new Warning
                {
                    StudentId = student.Id,
                    Type = WarningType.DismissalWarning,
                    Reason = $"غياب غير مبرر بنسبة {rate:P0} تجاوز الحد المسموح 15%",
                });
                await db.SaveChangesAsync(ct);
                await notifier.SendAsync(student.Id, UserType.Student,
                    "إنذار بالفصل", "تجاوز غيابك غير المبرر 15% من الدوام الرسمي", "dismissal_warning");
                await notifier.SendToGuardianAsync(student,
                    "إنذار بالفصل لابنكم", $"تجاوز غياب {student.Name} غير المبرر 15% من الدوام الرسمي", "dismissal_warning");
            }

   
            var warningsCount = await db.Warnings.CountAsync(w => w.StudentId == student.Id, ct);
            if (warningsCount > 3)
            {
                var lastWarning = await db.Warnings.Where(w => w.StudentId == student.Id)
                    .MaxAsync(w => w.CreatedAt, ct);
                var alreadySummoned = await db.GuardianSummons.AnyAsync(g =>
                    g.StudentId == student.Id && g.IssuedById == null && g.CreatedAt >= lastWarning, ct);
                if (!alreadySummoned)
                {
                    db.GuardianSummons.Add(new GuardianSummon
                    {
                        StudentId = student.Id,
                        Reason = $"تجاوز عدد التحذيرات ({warningsCount}) الحد المسموح",
                        Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                    });
                    await db.SaveChangesAsync(ct);
                    await notifier.SendToGuardianAsync(student,
                        "استدعاء ولي أمر", $"يرجى مراجعة إدارة المدرسة بخصوص {student.Name} — تجاوز عدد التحذيرات الحد المسموح", "guardian_summon");
                }
            }
        }
    }

    private static async Task CheckEmployeesAsync(AppDbContext db, NotificationService notifier, CancellationToken ct)
    {
        var stats = await db.EmployeeAttendances
            .Where(a => a.Status == AttendanceStatus.Absent && !a.OnLeave)
            .GroupBy(a => a.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Unexcused = g.Count() })
            .ToListAsync(ct);

        foreach (var stat in stats)
        {
            var employee = await db.Employees.FindAsync([stat.EmployeeId], ct);
            if (employee is null || employee.IsDismissed) continue;

            employee.UnexcusedAbsenceDays = stat.Unexcused;

            if (stat.Unexcused >= EmployeeDismissalDays)
            {
          
                employee.IsDismissed = true;
                await db.SaveChangesAsync(ct);
                await notifier.SendAsync(employee.Id, UserType.Employee,
                    "قرار فصل", $"تم فصلك لبلوغ غيابك بدون إجازة {stat.Unexcused} يوماً", "dismissal");
                await NotifyManagerAsync(db, notifier, employee,
                    "فصل موظف", $"تم فصل الموظف {employee.Name} لبلوغ غيابه بدون إجازة {stat.Unexcused} يوماً");
            }
            else if (stat.Unexcused >= EmployeeWarningDays && !employee.DismissalWarning)
            {
               
                employee.DismissalWarning = true;
                await db.SaveChangesAsync(ct);
                await notifier.SendAsync(employee.Id, UserType.Employee,
                    "إنذار بالفصل", $"غيابك بدون إجازة بلغ {stat.Unexcused} يوماً ويقترب من حد الفصل (30 يوماً)", "dismissal_warning");
                await NotifyManagerAsync(db, notifier, employee,
                    "موظف يقترب من حد الفصل", $"غياب الموظف {employee.Name} بدون إجازة بلغ {stat.Unexcused} يوماً");
            }
            else
            {
                await db.SaveChangesAsync(ct);
            }
        }
    }

    private static async Task NotifyManagerAsync(AppDbContext db, NotificationService notifier, Employee employee, string title, string body)
    {
        var manager = await db.Employees.FirstOrDefaultAsync(e =>
            e.SchoolId == employee.SchoolId && e.Role == EmployeeRole.Principal && !e.IsDismissed);
        if (manager is not null && manager.Id != employee.Id)
            await notifier.SendAsync(manager.Id, UserType.Employee, title, body, "employee_absence");
    }
}
