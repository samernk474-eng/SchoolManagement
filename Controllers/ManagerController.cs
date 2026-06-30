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
[Route("api/manager")]
[Authorize(Roles = Roles.Manager)]
public class ManagerController(
    AppDbContext db,
    SchoolRulesService rules,
    NotificationService notifier,
    ReportCardService reportCards) : ControllerBase
{
    private int SchoolId => User.GetSchoolId();



    [HttpPost("grades")]
    public async Task<IActionResult> CreateGrade(GradeRequest request)
    {
        if (await db.Grades.AnyAsync(g => g.SchoolId == SchoolId && g.Name == request.Name))
            return BadRequest(new { message = "هذا الصف موجود مسبقاً" });

        var grade = new Grade { SchoolId = SchoolId, Name = request.Name, Level = request.Level };
        db.Grades.Add(grade);
        await db.SaveChangesAsync();
        return Created($"api/manager/grades/{grade.Id}", grade);
    }

    [HttpGet("grades")]
    public async Task<IActionResult> GetGrades() =>
        Ok(await db.Grades.Where(g => g.SchoolId == SchoolId).ToListAsync());

    [HttpPost("sections")]
    public async Task<IActionResult> CreateSection(SectionRequest request)
    {
        if (!await db.Grades.AnyAsync(g => g.Id == request.GradeId && g.SchoolId == SchoolId))
            return BadRequest(new { message = "الصف غير موجود في مدرستك" });
        if (request.CounselorId is not null &&
            !await db.Employees.AnyAsync(e => e.Id == request.CounselorId && e.SchoolId == SchoolId && e.Role == EmployeeRole.Counselor && !e.IsDismissed))
            return BadRequest(new { message = "الموجه غير موجود في مدرستك" });

        if (await db.Sections.AnyAsync(s => s.SchoolId == SchoolId && s.GradeId == request.GradeId && s.Name == request.Name))
            return BadRequest(new { message = "هذه الشعبة موجودة مسبقاً" });

        var section = new Section
        {
            GradeId = request.GradeId,
            SchoolId = SchoolId,
            Name = request.Name,
            CounselorId = request.CounselorId,
        };
        db.Sections.Add(section);
        await db.SaveChangesAsync();
        return Created($"api/manager/sections/{section.Id}", section);
    }

    [HttpGet("sections")]
    public async Task<IActionResult> GetSections() =>
        Ok(await db.Sections.Include(s => s.Grade).Where(s => s.SchoolId == SchoolId).ToListAsync());

    [HttpPut("sections/{id:int}")]
    public async Task<IActionResult> UpdateSection(int id, SectionRequest request)
    {
        var section = await db.Sections.FirstOrDefaultAsync(s => s.Id == id && s.SchoolId == SchoolId);
        if (section is null) return NotFound();
        section.Name = request.Name;
        section.GradeId = request.GradeId;
        section.CounselorId = request.CounselorId;
        await db.SaveChangesAsync();
        return Ok(section);
    }

    [HttpPost("subjects")]
    public async Task<IActionResult> CreateSubject(SubjectRequest request)
    {
        if (!await db.Grades.AnyAsync(g => g.Id == request.GradeId && g.SchoolId == SchoolId))
            return BadRequest(new { message = "الصف غير موجود في مدرستك" });
        if (request.TeacherId is not null)
        {
            if (!await db.Employees.AnyAsync(e => e.Id == request.TeacherId && e.Role == EmployeeRole.Teacher && !e.IsDismissed))
                return BadRequest(new { message = "المعلم غير موجود" });
            if (!await db.TeacherAssignments.AnyAsync(t => t.EmployeeId == request.TeacherId && t.SchoolId == SchoolId))
                return BadRequest(new { message = "المعلم غير معين في مدرستك" });
        }

        var subject = new Subject
        {
            Name = request.Name,
            GradeId = request.GradeId,
            TeacherId = request.TeacherId,
            SchoolId = SchoolId,
        };
        db.Subjects.Add(subject);
        await db.SaveChangesAsync();
        return Created($"api/manager/subjects/{subject.Id}", subject);
    }

    [HttpGet("subjects")]
    public async Task<IActionResult> GetSubjects() =>
        Ok(await db.Subjects.Where(s => s.SchoolId == SchoolId).ToListAsync());

    [HttpPost("schedules")]
    public async Task<IActionResult> CreateSchedule(ScheduleRequest request)
    {
        if (!await db.Sections.AnyAsync(s => s.Id == request.SectionId && s.SchoolId == SchoolId))
            return BadRequest(new { message = "الشعبة غير موجودة في مدرستك" });

        var existing = await db.Schedules
            .Include(s => s.Periods)
            .FirstOrDefaultAsync(s => s.SectionId == request.SectionId && s.Day == request.Day);
        if (existing is not null) db.Schedules.Remove(existing);

        var schedule = new Schedule
        {
            SectionId = request.SectionId,
            Day = request.Day,
            Periods = request.Periods.Select(p => new SchedulePeriod
            {
                Order = p.Order,
                SubjectId = p.SubjectId,
                TeacherId = p.TeacherId,
            }).ToList(),
        };
        db.Schedules.Add(schedule);
        await db.SaveChangesAsync();
        return Created($"api/manager/schedules/{schedule.Id}", schedule);
    }

    [HttpGet("schedules")]
    public async Task<IActionResult> GetSchedules([FromQuery] int? sectionId)
    {
        var query = db.Schedules
            .Include(s => s.Periods)
            .Where(s => db.Sections.Any(x => x.Id == s.SectionId && x.SchoolId == SchoolId));
        if (sectionId is not null) query = query.Where(s => s.SectionId == sectionId);
        return Ok(await query.ToListAsync());
    }

  

    [HttpGet("employees")]
    public async Task<IActionResult> GetEmployees() =>
        Ok(await db.Employees.Where(e => e.SchoolId == SchoolId).ToListAsync());


    [HttpGet("students")]
    public async Task<IActionResult> GetStudents() =>
        Ok(await db.Students.Include(s => s.Section).Where(s => s.SchoolId == SchoolId).ToListAsync());

 
    [HttpPost("students")]
    public async Task<IActionResult> CreateStudent(StudentCreateRequest request)
    {
        var error = await rules.ValidateStudentCreatorAsync(SchoolId, EmployeeRole.Principal);
        if (error is not null) return BadRequest(new { message = error });
        return await StudentsHelper.CreateAsync(db, SchoolId, request, this);
    }


    [HttpPost("employee-attendance")]
    public async Task<IActionResult> TakeEmployeeAttendance(EmployeeAttendanceRequest request)
    {
        foreach (var entry in request.Entries)
        {
            var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == entry.EmployeeId && e.SchoolId == SchoolId);
            if (employee is null)
                return BadRequest(new { message = $"الموظف {entry.EmployeeId} غير موجود في مدرستك" });

            var onLeave = entry.Status == AttendanceStatus.Absent &&
                          await db.Leaves.AnyAsync(l => l.EmployeeId == entry.EmployeeId &&
                                                        l.StartDate <= request.Date && request.Date <= l.EndDate);

            var existing = await db.EmployeeAttendances
                .FirstOrDefaultAsync(a => a.EmployeeId == entry.EmployeeId && a.Date == request.Date);
            if (existing is not null)
            {
                existing.Status = entry.Status;
                existing.OnLeave = onLeave;
            }
            else
            {
                db.EmployeeAttendances.Add(new EmployeeAttendance
                {
                    EmployeeId = entry.EmployeeId,
                    Date = request.Date,
                    Status = entry.Status,
                    OnLeave = onLeave,
                });
            }
        }
        await db.SaveChangesAsync();
        return Ok(new { message = "تم تسجيل حضور الموظفين" });
    }

    [HttpGet("employee-attendance")]
    public async Task<IActionResult> GetEmployeeAttendance([FromQuery] DateOnly? date, [FromQuery] int? employeeId)
    {
        var query = db.EmployeeAttendances
            .Where(a => db.Employees.Any(e => e.Id == a.EmployeeId && e.SchoolId == SchoolId));
        if (date is not null) query = query.Where(a => a.Date == date);
        if (employeeId is not null) query = query.Where(a => a.EmployeeId == employeeId);
        return Ok(await query.OrderByDescending(a => a.Date).Take(500).ToListAsync());
    }


    [HttpGet("reports/overview")]
    public async Task<IActionResult> Overview()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return Ok(new
        {
            students = await db.Students.CountAsync(s => s.SchoolId == SchoolId),
            employees = await db.Employees.CountAsync(e => e.SchoolId == SchoolId && !e.IsDismissed),
            sections = await db.Sections.CountAsync(s => s.SchoolId == SchoolId),
            subjects = await db.Subjects.CountAsync(s => s.SchoolId == SchoolId),
            studentsWithDismissalWarning = await db.Students.CountAsync(s => s.SchoolId == SchoolId && s.DismissalWarning),
            employeesWithDismissalWarning = await db.Employees.CountAsync(e => e.SchoolId == SchoolId && e.DismissalWarning && !e.IsDismissed),
            openComplaints = await db.Complaints.CountAsync(c => c.SchoolId == SchoolId && c.Status == ComplaintStatus.Open),
            absentStudentsToday = await db.StudentAttendances.CountAsync(a =>
                a.Date == today && a.Status == AttendanceStatus.Absent &&
                db.Students.Any(s => s.Id == a.StudentId && s.SchoolId == SchoolId)),
        });
    }


    [HttpGet("reports/student-absence")]
    public async Task<IActionResult> StudentAbsenceReport()
    {
        var report = await db.StudentAttendances
            .Where(a => db.Students.Any(s => s.Id == a.StudentId && s.SchoolId == SchoolId))
            .GroupBy(a => a.StudentId)
            .Select(g => new
            {
                StudentId = g.Key,
                Total = g.Count(),
                Unexcused = g.Count(a => a.Status == AttendanceStatus.Absent),
                Justified = g.Count(a => a.Status == AttendanceStatus.Justified),
            })
            .ToListAsync();
        return Ok(report);
    }

  
    [HttpGet("reports/health-records")]
    public async Task<IActionResult> HealthRecords() =>
        Ok(await db.Students.Where(s => s.SchoolId == SchoolId)
            .Select(s => new { s.Id, s.Name, s.BloodType, s.ChronicDiseases, s.Allergies, s.HealthNotes })
            .ToListAsync());



    [HttpGet("complaints")]
    public async Task<IActionResult> GetComplaints() =>
        Ok(await db.Complaints.Where(c => c.SchoolId == SchoolId).OrderByDescending(c => c.CreatedAt).ToListAsync());

    [HttpPatch("complaints/{id:int}")]
    public async Task<IActionResult> ResolveComplaint(int id, ComplaintResolveRequest request)
    {
        var complaint = await db.Complaints.FirstOrDefaultAsync(c => c.Id == id && c.SchoolId == SchoolId);
        if (complaint is null) return NotFound();
        complaint.Status = request.Status;
        complaint.Resolution = request.Resolution ?? complaint.Resolution;
        await db.SaveChangesAsync();
        await notifier.SendAsync(complaint.FromUserId, complaint.FromUserType,
            "تحديث على شكواك", $"حالة الشكوى: {request.Status}", "complaint");
        return Ok(complaint);
    }

    [HttpPost("punishments")]
    public async Task<IActionResult> CreatePunishment(PunishmentRequest request)
    {
        if (request.StudentId is null == (request.EmployeeId is null))
            return BadRequest(new { message = "حدد طالباً أو موظفاً (واحد فقط)" });

        var punishment = new Punishment
        {
            StudentId = request.StudentId,
            EmployeeId = request.EmployeeId,
            SchoolId = SchoolId,
            Reason = request.Reason,
            Type = request.Type,
            IssuedById = User.GetUserId(),
        };
        db.Punishments.Add(punishment);
        await db.SaveChangesAsync();

        if (request.StudentId is not null)
            await notifier.SendAsync(request.StudentId.Value, UserType.Student, "عقوبة", request.Reason, "punishment");
        else
            await notifier.SendAsync(request.EmployeeId!.Value, UserType.Employee, "عقوبة", request.Reason, "punishment");
        return Created($"api/manager/punishments/{punishment.Id}", punishment);
    }



    [HttpPost("announcements")]
    public async Task<IActionResult> CreateAnnouncement(AnnouncementRequest request)
    {
        var announcement = new Announcement
        {
            SchoolId = SchoolId,
            Title = request.Title,
            Body = request.Body,
            Audience = request.Audience,
            Type = request.Type,
            CreatedById = User.GetUserId(),
        };
        db.Announcements.Add(announcement);
        await db.SaveChangesAsync();
        return Created($"api/manager/announcements/{announcement.Id}", announcement);
    }

    [HttpGet("announcements")]
    public async Task<IActionResult> GetAnnouncements() =>
        Ok(await db.Announcements.Where(a => a.SchoolId == SchoolId).OrderByDescending(a => a.CreatedAt).ToListAsync());

    [HttpDelete("announcements/{id:int}")]
    public async Task<IActionResult> DeleteAnnouncement(int id)
    {
        var announcement = await db.Announcements.FirstOrDefaultAsync(a => a.Id == id && a.SchoolId == SchoolId);
        if (announcement is null) return NotFound();
        db.Announcements.Remove(announcement);
        await db.SaveChangesAsync();
        return Ok(new { message = "تم حذف الإعلان" });
    }


    [HttpPost("report-cards")]
    public async Task<IActionResult> GenerateReportCards(ReportCardRequest request)
    {
        if (!await db.Sections.AnyAsync(s => s.Id == request.SectionId && s.SchoolId == SchoolId))
            return BadRequest(new { message = "الشعبة غير موجودة في مدرستك" });
        try
        {
            var cards = await reportCards.GenerateForSectionAsync(
                request.SectionId, request.Semester, request.Year, User.GetUserId());
            return Ok(cards);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("report-cards")]
    public async Task<IActionResult> GetReportCards([FromQuery] int? sectionId, [FromQuery] int? semester, [FromQuery] int? year)
    {
        var query = db.ReportCards
            .Include(r => r.Subjects)
            .Include(r => r.Student)
            .Where(r => r.Student!.SchoolId == SchoolId);
        if (sectionId is not null) query = query.Where(r => r.Student!.SectionId == sectionId);
        if (semester is not null) query = query.Where(r => r.Semester == semester);
        if (year is not null) query = query.Where(r => r.Year == year);
        return Ok(await query.OrderBy(r => r.Rank).ToListAsync());
    }

   
    [HttpPut("mark-config")]
    public async Task<IActionResult> UpdateMarkConfig(MarkConfigRequest request)
    {
        var config = await db.MarkConfigs.FirstOrDefaultAsync(c => c.SchoolId == SchoolId);
        if (config is null)
        {
            config = new MarkConfig { SchoolId = SchoolId };
            db.MarkConfigs.Add(config);
        }
        config.MaxOral = request.MaxOral;
        config.MaxQuiz1 = request.MaxQuiz1;
        config.MaxQuiz2 = request.MaxQuiz2;
        config.MaxHomework = request.MaxHomework;
        config.MaxFinalExam = request.MaxFinalExam;
        config.PassPercent = request.PassPercent;
        await db.SaveChangesAsync();
        return Ok(config);
    }

    [HttpGet("mark-config")]
    public async Task<IActionResult> GetMarkConfig() =>
        Ok(await db.MarkConfigs.FirstOrDefaultAsync(c => c.SchoolId == SchoolId) ?? new MarkConfig { SchoolId = SchoolId });



    [HttpPost("employees/{id:int}/dismissal-warning")]
    public async Task<IActionResult> EmployeeDismissalWarning(int id)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == id && e.SchoolId == SchoolId);
        if (employee is null) return NotFound();
        employee.DismissalWarning = true;
        await db.SaveChangesAsync();
        await notifier.SendAsync(employee.Id, UserType.Employee,
            "إنذار بالفصل", "صدر بحقك إنذار بالفصل من إدارة المدرسة", "dismissal_warning");
        return Ok(employee);
    }

    [HttpPost("employees/{id:int}/dismiss")]
    public async Task<IActionResult> DismissEmployee(int id)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == id && e.SchoolId == SchoolId);
        if (employee is null) return NotFound();
        if (employee.Role == EmployeeRole.Principal)
            return BadRequest(new { message = "لا يمكن للمدير فصل نفسه" });
        employee.IsDismissed = true;
        await db.SaveChangesAsync();
        await notifier.SendAsync(employee.Id, UserType.Employee, "قرار فصل", "تم فصلك من العمل", "dismissal");
        return Ok(employee);
    }

    [HttpPost("contact-guardian/{studentId:int}")]
    public async Task<IActionResult> ContactGuardian(int studentId, ContactGuardianRequest request)
    {
        var student = await db.Students.FirstOrDefaultAsync(s => s.Id == studentId && s.SchoolId == SchoolId);
        if (student is null) return NotFound();
        await notifier.SendToGuardianAsync(student, request.Title, request.Body, "guardian_contact");
        return Ok(new { message = "تم إرسال الإشعار لولي الأمر" });
    }
}
