using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Api.Auth;
using SchoolManagement.Api.Data;
using SchoolManagement.Api.Dtos;
using SchoolManagement.Api.Models;
using SchoolManagement.Api.Services;

namespace SchoolManagement.Api.Controllers;


[ApiController]
[Route("api/admin")]
[Authorize(Roles = Roles.Admin)]
public class AdminController(
    AppDbContext db,
    SchoolRulesService rules,
    NotificationService notifier) : ControllerBase
{


    [HttpPost("schools")]
    public async Task<IActionResult> CreateSchool(SchoolRequest request)
    {
        var school = new School
        {
            Name = request.Name,
            Type = request.Type,
            Address = request.Address ?? "",
            Phone = request.Phone ?? "",
            AdminId = User.GetUserId(),
        };
        db.Schools.Add(school);
        await db.SaveChangesAsync();

        db.MarkConfigs.Add(new MarkConfig { SchoolId = school.Id });
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetSchool), new { id = school.Id }, school);
    }

    [HttpGet("schools")]
    public async Task<IActionResult> GetSchools() => Ok(await db.Schools.ToListAsync());

    [HttpGet("schools/{id:int}")]
    public async Task<IActionResult> GetSchool(int id)
    {
        var school = await db.Schools.FindAsync(id);
        return school is null ? NotFound() : Ok(school);
    }

    [HttpPut("schools/{id:int}")]
    public async Task<IActionResult> UpdateSchool(int id, SchoolRequest request)
    {
        var school = await db.Schools.FindAsync(id);
        if (school is null) return NotFound();
        school.Name = request.Name;
        school.Type = request.Type;
        school.Address = request.Address ?? school.Address;
        school.Phone = request.Phone ?? school.Phone;
        await db.SaveChangesAsync();
        return Ok(school);
    }


    [HttpDelete("schools/{id:int}")]
    public async Task<IActionResult> DeleteSchool(int id)
    {
        var school = await db.Schools.FindAsync(id);
        if (school is null) return NotFound();

        var error = await rules.ValidateSchoolDeleteAsync(id);
        if (error is not null) return BadRequest(new { message = error });

        
        var staffIds = db.Employees.Where(e => e.SchoolId == id).Select(e => e.Id);
        db.EmployeeAttendances.RemoveRange(db.EmployeeAttendances.Where(a => staffIds.Contains(a.EmployeeId)));
        db.Leaves.RemoveRange(db.Leaves.Where(l => staffIds.Contains(l.EmployeeId)));
        db.Schedules.RemoveRange(db.Schedules.Where(s => db.Sections.Any(x => x.Id == s.SectionId && x.SchoolId == id)));
        db.Subjects.RemoveRange(db.Subjects.Where(s => s.SchoolId == id));
        db.Sections.RemoveRange(db.Sections.Where(s => s.SchoolId == id));
        db.Grades.RemoveRange(db.Grades.Where(g => g.SchoolId == id));
        db.MarkConfigs.RemoveRange(db.MarkConfigs.Where(c => c.SchoolId == id));
        db.TeacherAssignments.RemoveRange(db.TeacherAssignments.Where(t => t.SchoolId == id));
        db.Employees.RemoveRange(db.Employees.Where(e => e.SchoolId == id));
        db.Schools.Remove(school);
        await db.SaveChangesAsync();
        return Ok(new { message = "تم حذف المدرسة" });
    }



    [HttpPost("schools/{schoolId:int}/employees")]
    public async Task<IActionResult> CreateEmployee(int schoolId, EmployeeCreateRequest request)
    {
        var error = await rules.ValidateHireAsync(schoolId, request.Role);
        if (error is not null) return BadRequest(new { message = error });

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Employees.AnyAsync(e => e.Email == email))
            return BadRequest(new { message = "الإيميل مستخدم بالفعل" });

        var employee = new Employee
        {
            Name = request.Name,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            SchoolId = schoolId,
            Phone = request.Phone ?? "",
            Address = request.Address ?? "",
            BirthDate = request.BirthDate,
            Qualification = request.Qualification ?? "",
        };
        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        if (employee.Role == EmployeeRole.Teacher)
        {
            db.TeacherAssignments.Add(new TeacherAssignment { EmployeeId = employee.Id, SchoolId = schoolId });
            await db.SaveChangesAsync();
        }

        return Created($"api/admin/employees/{employee.Id}", employee);
    }

    [HttpGet("schools/{schoolId:int}/employees")]
    public async Task<IActionResult> GetSchoolEmployees(int schoolId) =>
        Ok(await db.Employees.Where(e => e.SchoolId == schoolId).ToListAsync());

    [HttpPut("employees/{id:int}")]
    public async Task<IActionResult> UpdateEmployee(int id, EmployeeUpdateRequest request)
    {
        var employee = await db.Employees.FindAsync(id);
        if (employee is null) return NotFound();
        employee.Name = request.Name ?? employee.Name;
        employee.Phone = request.Phone ?? employee.Phone;
        employee.Address = request.Address ?? employee.Address;
        employee.BirthDate = request.BirthDate ?? employee.BirthDate;
        employee.Qualification = request.Qualification ?? employee.Qualification;
        if (request.Password is not null)
            employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        await db.SaveChangesAsync();
        return Ok(employee);
    }

    [HttpDelete("employees/{id:int}")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var employee = await db.Employees.FindAsync(id);
        if (employee is null) return NotFound();

        var error = await rules.ValidateEmployeeDeleteAsync(employee);
        if (error is not null) return BadRequest(new { message = error });


        db.EmployeeAttendances.RemoveRange(db.EmployeeAttendances.Where(a => a.EmployeeId == id));
        db.Leaves.RemoveRange(db.Leaves.Where(l => l.EmployeeId == id));
        db.TeacherAssignments.RemoveRange(db.TeacherAssignments.Where(t => t.EmployeeId == id));

        db.Employees.Remove(employee);
        await db.SaveChangesAsync();
        return Ok(new { message = "تم حذف الموظف" });
    }



    [HttpPatch("transfer/student/{id:int}")]
    public async Task<IActionResult> TransferStudent(int id, TransferRequest request)
    {
        var student = await db.Students.FindAsync(id);
        if (student is null) return NotFound();
        if (!await db.Schools.AnyAsync(s => s.Id == request.SchoolId))
            return BadRequest(new { message = "المدرسة الهدف غير موجودة" });
        if (request.SectionId is not null &&
            !await db.Sections.AnyAsync(s => s.Id == request.SectionId && s.SchoolId == request.SchoolId))
            return BadRequest(new { message = "الشعبة غير موجودة في المدرسة الهدف" });

        student.SchoolId = request.SchoolId;
        student.SectionId = request.SectionId;
        await db.SaveChangesAsync();
        await notifier.SendAsync(student.Id, UserType.Student, "نقل مدرسي", "تم نقلك إلى مدرسة جديدة", "transfer");
        return Ok(student);
    }


    [HttpPatch("transfer/employee/{id:int}")]
    public async Task<IActionResult> TransferEmployee(int id, TransferRequest request)
    {
        var employee = await db.Employees.FindAsync(id);
        if (employee is null) return NotFound();

        var error = await rules.ValidateHireAsync(request.SchoolId, employee.Role, excludeEmployeeId: id);
        if (error is not null) return BadRequest(new { message = error });


        if (employee.Role == EmployeeRole.Principal &&
            await db.Employees.AnyAsync(e => e.SchoolId == employee.SchoolId && e.Id != id && !e.IsDismissed))
            return BadRequest(new { message = "لا يمكن نقل المدير ومدرسته فيها موظفون" });

        employee.SchoolId = request.SchoolId;
        await db.SaveChangesAsync();
        await notifier.SendAsync(employee.Id, UserType.Employee, "نقل وظيفي", "تم نقلك إلى مدرسة جديدة", "transfer");
        return Ok(employee);
    }

   
    
    [HttpPost("teachers/assign/{employeeId:int}/{schoolId:int}")]
    public async Task<IActionResult> AssignTeacherToSchool(int employeeId, int schoolId)
    {
        var teacher = await db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId && e.Role == EmployeeRole.Teacher && !e.IsDismissed);
        if (teacher is null) return NotFound(new { message = "المعلم غير موجود" });
        if (!await db.Schools.AnyAsync(s => s.Id == schoolId))
            return BadRequest(new { message = "المدرسة غير موجودة" });
        if (await db.TeacherAssignments.AnyAsync(t => t.EmployeeId == employeeId && t.SchoolId == schoolId))
            return BadRequest(new { message = "المعلم معين مسبقاً في هذه المدرسة" });

        db.TeacherAssignments.Add(new TeacherAssignment { EmployeeId = employeeId, SchoolId = schoolId });
        await db.SaveChangesAsync();
        return Ok(new { message = "تم تعيين المعلم في المدرسة" });
    }

    [HttpPost("leaves")]
    public async Task<IActionResult> GrantLeave(LeaveRequest request)
    {
        var employee = await db.Employees.FindAsync(request.EmployeeId);
        if (employee is null) return NotFound(new { message = "الموظف غير موجود" });
        if (request.EndDate < request.StartDate)
            return BadRequest(new { message = "تاريخ نهاية الإجازة قبل بدايتها" });

        var leave = new Leave
        {
            EmployeeId = request.EmployeeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = request.Reason ?? "",
            GrantedByAdminId = User.GetUserId(),
        };
        db.Leaves.Add(leave);
        await db.SaveChangesAsync();
        await notifier.SendAsync(employee.Id, UserType.Employee,
            "إجازة ممنوحة", $"مُنحت إجازة من {request.StartDate} إلى {request.EndDate}", "leave");
        return Created($"api/admin/leaves/{leave.Id}", leave);
    }

    [HttpGet("leaves")]
    public async Task<IActionResult> GetLeaves() =>
        Ok(await db.Leaves.Include(l => l.Employee).OrderByDescending(l => l.CreatedAt).ToListAsync());


    [HttpPost("run-attendance-checks")]
    public async Task<IActionResult> RunAttendanceChecks([FromServices] AttendanceWarningJob job)
    {
        await job.RunChecksAsync();
        return Ok(new { message = "تم تشغيل فحص الغياب والإنذارات" });
    }
}
