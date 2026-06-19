using Microsoft.EntityFrameworkCore;
using SchoolManagement.Api.Data;
using SchoolManagement.Api.Models;

namespace SchoolManagement.Api.Services;


public class SchoolRulesService(AppDbContext db)
{

    public static readonly EmployeeRole[] UniqueRoles =
    [
        EmployeeRole.Principal,
        EmployeeRole.Secretary,
        EmployeeRole.Librarian,
        EmployeeRole.ActivitySupervisor,
    ];


    public async Task<string?> ValidateHireAsync(int schoolId, EmployeeRole role, int? excludeEmployeeId = null)
    {
        if (!await db.Schools.AnyAsync(s => s.Id == schoolId))
            return "المدرسة غير موجودة";

        var staff = db.Employees.Where(e =>
            e.SchoolId == schoolId && !e.IsDismissed && e.Id != excludeEmployeeId);

        var hasManager = await staff.AnyAsync(e => e.Role == EmployeeRole.Principal);


        if (role == EmployeeRole.Principal)
        {
            if (hasManager)
                return "يوجد مدير لهذه المدرسة بالفعل (مدير واحد فقط لكل مدرسة)";
        }
        else if (!hasManager)
        {
            return "لا يمكن إضافة أي موظف قبل تعيين مدير للمدرسة";
        }

        if (UniqueRoles.Contains(role) && await staff.AnyAsync(e => e.Role == role))
            return $"يوجد موظف بدور {role} في هذه المدرسة بالفعل (واحد فقط مسموح)";

       
        if (role == EmployeeRole.Counselor)
        {
            var sections = await db.Sections.CountAsync(s => s.SchoolId == schoolId);
            var maxCounselors = Math.Max(1, (int)Math.Ceiling(sections / 12.0));
            var counselors = await staff.CountAsync(e => e.Role == EmployeeRole.Counselor);
            if (counselors >= maxCounselors)
                return $"عدد الموجهين مكتمل ({counselors}/{maxCounselors}) — يُسمح بموجه واحد لكل 12 شعبة";
        }

        return null;
    }


    public async Task<string?> ValidateSchoolDeleteAsync(int schoolId)
    {
        if (await db.Students.AnyAsync(s => s.SchoolId == schoolId))
            return "لا يمكن حذف المدرسة وفيها طلاب";
        if (await db.Employees.AnyAsync(e => e.SchoolId == schoolId && e.Role != EmployeeRole.Principal))
            return "لا يمكن حذف المدرسة وفيها موظفون — يجب حذف جميع الموظفين أولاً (المدير آخراً)";
        return null;
    }

 
    public async Task<string?> ValidateEmployeeDeleteAsync(Employee employee)
    {
        if (employee.Role == EmployeeRole.Principal &&
            await db.Employees.AnyAsync(e =>
                e.SchoolId == employee.SchoolId && e.Id != employee.Id))
        {
            return "لا يمكن حذف المدير قبل حذف بقية موظفي المدرسة (المدير يُحذف آخراً)";
        }
        return null;
    }

 
    public async Task<string?> ValidateStudentCreatorAsync(int schoolId, EmployeeRole creatorRole)
    {
        if (creatorRole == EmployeeRole.Secretary) return null;
        if (creatorRole == EmployeeRole.Principal)
        {
            var hasSecretary = await db.Employees.AnyAsync(e =>
                e.SchoolId == schoolId && e.Role == EmployeeRole.Secretary && !e.IsDismissed);
            return hasSecretary
                ? "إضافة الطلاب من اختصاص أمين السر — يضيف المدير الطلاب فقط عند عدم وجود أمين سر"
                : null;
        }
        return "غير مصرح بإضافة طلاب";
    }

 
    public async Task<string?> ValidateSecondPeriodAttendanceTakenAsync(int teacherId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var day = DateTime.Today.DayOfWeek;

        var sectionIds = await db.SchedulePeriods
            .Where(p => p.TeacherId == teacherId && p.Order == 2)
            .Join(db.Schedules.Where(s => s.Day == day),
                p => p.ScheduleId, s => s.Id, (p, s) => s.SectionId)
            .Distinct()
            .ToListAsync();

        foreach (var sectionId in sectionIds)
        {
            var taken = await db.StudentAttendances.AnyAsync(a => a.SectionId == sectionId && a.Date == today);
            if (!taken)
                return "أنت معلم الحصة الثانية اليوم — يجب تسجيل حضور الشعبة قبل إدخال العلامات أو التقارير";
        }
        return null;
    }
}
