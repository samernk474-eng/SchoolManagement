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
[Route("api/teacher")]
[Authorize(Roles = Roles.Teacher)]
public class TeacherController(
    AppDbContext db,
    SchoolRulesService rules,
    NotificationService notifier) : ControllerBase
{
    private int TeacherId => User.GetUserId();

    private async Task<List<int>> GetSchoolIdsAsync() =>
        await db.TeacherAssignments
            .Where(t => t.EmployeeId == TeacherId)
            .Select(t => t.SchoolId)
            .ToListAsync();

    [HttpGet("subjects")]
    public async Task<IActionResult> GetSubjects() =>
        Ok(await db.Subjects.Where(s => s.TeacherId == TeacherId).ToListAsync());


    [HttpGet("schedule")]
    public async Task<IActionResult> GetSchedule()
    {
        var periods = await db.SchedulePeriods
            .Include(p => p.Subject)
            .Where(p => p.TeacherId == TeacherId)
            .Join(db.Schedules, p => p.ScheduleId, s => s.Id,
                (p, s) => new { s.Day, s.SectionId, p.Order, p.SubjectId, SubjectName = p.Subject!.Name })
            .OrderBy(x => x.Day).ThenBy(x => x.Order)
            .ToListAsync();
        return Ok(periods);
    }

   

    [HttpPost("attendance")]
    public async Task<IActionResult> TakeAttendance(StudentAttendanceRequest request)
    {
    
        var day = request.Date.DayOfWeek;
        var scheduled = await db.SchedulePeriods
            .Where(p => p.TeacherId == TeacherId)
            .Join(db.Schedules.Where(s => s.SectionId == request.SectionId && s.Day == day),
                p => p.ScheduleId, s => s.Id, (p, s) => p)
            .AnyAsync();
        if (!scheduled)
            return BadRequest(new { message = "ليست لديك حصة في هذه الشعبة في هذا اليوم" });

        return await AttendanceHelper.RecordAsync(db, request, TeacherId, this);
    }

    

    [HttpPost("marks")]
    public async Task<IActionResult> UpsertMark(MarkRequest request)
    {
    
        var blocked = await rules.ValidateSecondPeriodAttendanceTakenAsync(TeacherId);
        if (blocked is not null) return StatusCode(403, new { message = blocked });

        var subject = await db.Subjects.FirstOrDefaultAsync(s => s.Id == request.SubjectId && s.TeacherId == TeacherId);
        if (subject is null) return BadRequest(new { message = "هذه المادة ليست من موادك" });

        var schoolIds = await GetSchoolIdsAsync();
        var student = await db.Students.FirstOrDefaultAsync(s => s.Id == request.StudentId && schoolIds.Contains(s.SchoolId));
        if (student is null) return BadRequest(new { message = "الطالب غير موجود في مدارسك" });

        
        var config = await db.MarkConfigs.FirstOrDefaultAsync(c => c.SchoolId == subject.SchoolId)
                     ?? new MarkConfig { SchoolId = subject.SchoolId };
        if (request.Oral > config.MaxOral || request.Quiz1 > config.MaxQuiz1 ||
            request.Quiz2 > config.MaxQuiz2 || request.Homework > config.MaxHomework ||
            request.FinalExam > config.MaxFinalExam)
            return BadRequest(new { message = "علامة تتجاوز الحد الأعلى المضبوط للمدرسة" });

        var mark = await db.Marks.FirstOrDefaultAsync(m =>
            m.StudentId == request.StudentId && m.SubjectId == request.SubjectId && m.Semester == request.Semester);
        if (mark is null)
        {
            mark = new Mark { StudentId = request.StudentId, SubjectId = request.SubjectId, Semester = request.Semester };
            db.Marks.Add(mark);
        }
        mark.Oral = request.Oral;
        mark.Quiz1 = request.Quiz1;
        mark.Quiz2 = request.Quiz2;
        mark.Homework = request.Homework;
        mark.FinalExam = request.FinalExam;
        mark.Total = request.Oral + request.Quiz1 + request.Quiz2 + request.Homework + request.FinalExam;
        mark.EnteredById = TeacherId;
        mark.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await notifier.SendAsync(student.Id, UserType.Student,
            "علامة جديدة", $"رُصدت علامتك في {subject.Name} (الفصل {request.Semester}): {mark.Total}", "mark");
        return Ok(mark);
    }

    [HttpPut("marks/{id:int}")]
    public async Task<IActionResult> UpdateMark(int id, MarkRequest request)
    {
        var mark = await db.Marks.FindAsync(id);
        if (mark is null || mark.SubjectId != request.SubjectId) return NotFound();
        return await UpsertMark(request);
    }

    [HttpGet("marks")]
    public async Task<IActionResult> GetMarks([FromQuery] int? subjectId, [FromQuery] int? semester)
    {
        var query = db.Marks.Where(m => db.Subjects.Any(s => s.Id == m.SubjectId && s.TeacherId == TeacherId));
        if (subjectId is not null) query = query.Where(m => m.SubjectId == subjectId);
        if (semester is not null) query = query.Where(m => m.Semester == semester);
        return Ok(await query.ToListAsync());
    }



    [HttpPost("performance-reports")]
    public async Task<IActionResult> CreatePerformanceReport(PerformanceReportRequest request)
    {
        var blocked = await rules.ValidateSecondPeriodAttendanceTakenAsync(TeacherId);
        if (blocked is not null) return StatusCode(403, new { message = blocked });

        if (!await db.Subjects.AnyAsync(s => s.Id == request.SubjectId && s.TeacherId == TeacherId))
            return BadRequest(new { message = "هذه المادة ليست من موادك" });
        var schoolIds = await GetSchoolIdsAsync();
        if (!await db.Students.AnyAsync(s => s.Id == request.StudentId && schoolIds.Contains(s.SchoolId)))
            return BadRequest(new { message = "الطالب غير موجود في مدارسك" });

        var report = new PerformanceReport
        {
            StudentId = request.StudentId,
            TeacherId = TeacherId,
            SubjectId = request.SubjectId,
            Semester = request.Semester,
            Behavior = request.Behavior ?? "",
            Notes = request.Notes ?? "",
        };
        db.PerformanceReports.Add(report);
        await db.SaveChangesAsync();
        return Created($"api/teacher/performance-reports/{report.Id}", report);
    }

    [HttpGet("performance-reports")]
    public async Task<IActionResult> GetPerformanceReports([FromQuery] int? studentId)
    {
        var query = db.PerformanceReports.Where(r => r.TeacherId == TeacherId);
        if (studentId is not null) query = query.Where(r => r.StudentId == studentId);
        return Ok(await query.OrderByDescending(r => r.CreatedAt).ToListAsync());
    }

    // ===== الشكاوى =====

    [HttpPost("complaints")]
    public async Task<IActionResult> CreateComplaint(ComplaintRequest request)
    {
        var complaint = new Complaint
        {
            FromUserId = TeacherId,
            FromUserType = UserType.Employee,
            FromName = User.Identity?.Name ?? "",
            Against = request.Against,
            SchoolId = User.GetSchoolId(),
            Content = request.Content,
        };
        db.Complaints.Add(complaint);
        await db.SaveChangesAsync();
        return Created($"api/teacher/complaints/{complaint.Id}", complaint);
    }

    // ===== Full Profile =====

    [HttpGet("full-profile")]
    public async Task<IActionResult> GetFullProfile()
    {
        var me = await db.Employees.FindAsync(TeacherId);
        if (me is null) return NotFound();

        var teacher = new TeacherBasicInfo(
            me.Id, me.Name, me.Email, me.SchoolId,
            me.Phone, me.Address, me.BirthDate, me.Qualification,
            me.IsDismissed, me.CreatedAt);

        var assignments = await db.TeacherAssignments
            .Where(t => t.EmployeeId == TeacherId)
            .Join(db.Schools, t => t.SchoolId, s => s.Id, (t, s) => new { SchoolId = s.Id, SchoolName = s.Name })
            .ToListAsync();

        var schools = new List<TeacherSchoolInfo>();
        foreach (var a in assignments)
        {
            var subjects = await db.Subjects
                .Where(s => s.TeacherId == TeacherId && s.SchoolId == a.SchoolId)
                .Join(db.Grades, s => s.GradeId, g => g.Id, (s, g) =>
                    new TeacherProfileSubject(s.Id, s.Name, g.Id, g.Name, g.Level))
                .ToListAsync();

            var gradeIds = subjects.Select(s => s.GradeId).Distinct().ToList();

            var sections = await db.Sections.Include(s => s.Grade)
                .Where(s => s.SchoolId == a.SchoolId && gradeIds.Contains(s.GradeId))
                .ToListAsync();

            var sectionInfos = new List<TeacherProfileSection>();
            foreach (var sec in sections)
            {
                var students = await db.Students
                    .Where(s => s.SectionId == sec.Id)
                    .Select(s => new TeacherProfileStudent(s.Id, s.Name))
                    .ToListAsync();
                sectionInfos.Add(new TeacherProfileSection(sec.Id, sec.Name,
                    sec.Grade?.Name ?? "", sec.Grade?.Level ?? 0, students));
            }

            var joined = await db.SchedulePeriods
                .Include(p => p.Subject)
                .Where(p => p.TeacherId == TeacherId && p.Subject != null && p.Subject.SchoolId == a.SchoolId)
                .Join(db.Schedules.Include(s => s.Section), p => p.ScheduleId, s => s.Id,
                    (p, s) => new { Period = p, Schedule = s })
                .ToListAsync();

            var schedule = joined
                .GroupBy(x => x.Schedule.Day)
                .OrderBy(g => g.Key)
                .Select(g => new TeacherProfileDaySchedule(
                    g.Key.ToString(),
                    g.OrderBy(x => x.Period.Order)
                        .Select(x => new TeacherProfilePeriod(x.Period.Order, x.Period.Subject?.Name ?? "", x.Schedule.SectionId, x.Schedule.Section?.Name ?? ""))
                        .ToList()))
                .ToList();

            schools.Add(new TeacherSchoolInfo(a.SchoolId, a.SchoolName, subjects, sectionInfos, schedule));
        }

        var marks = await db.Marks
            .Where(m => db.Subjects.Any(s => s.Id == m.SubjectId && s.TeacherId == TeacherId))
            .OrderByDescending(m => m.UpdatedAt).Take(500)
            .Select(m => new TeacherProfileMark(m.Id, m.StudentId, m.Student!.Name, m.SubjectId, m.Subject!.Name,
                m.Semester, m.Oral, m.Quiz1, m.Quiz2, m.Homework, m.FinalExam, m.Total, m.UpdatedAt))
            .ToListAsync();

        var attendance = await db.EmployeeAttendances
            .Where(a => a.EmployeeId == TeacherId)
            .OrderByDescending(a => a.Date).Take(200)
            .Select(a => new TeacherProfileAttendance(a.Date, a.Status.ToString()))
            .ToListAsync();

        var leaves = await db.Leaves
            .Where(l => l.EmployeeId == TeacherId)
            .OrderByDescending(l => l.StartDate)
            .Select(l => new TeacherProfileLeave(l.Id, l.StartDate, l.EndDate, l.Reason))
            .ToListAsync();

        var perfReports = await db.PerformanceReports
            .Where(r => r.TeacherId == TeacherId)
            .Join(db.Subjects, r => r.SubjectId, s => s.Id, (r, s) => new { r, s })
            .OrderByDescending(x => x.r.CreatedAt)
            .Select(x => new TeacherProfilePerformanceReport(x.r.Id, x.r.StudentId,
                x.r.Student!.Name, x.s.Name, x.r.Semester, x.r.Behavior, x.r.Notes, x.r.CreatedAt))
            .ToListAsync();

        var complaints = await db.Complaints
            .Where(c => c.FromUserId == TeacherId && c.FromUserType == UserType.Employee)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new TeacherProfileComplaint(c.Id, c.Against, c.Content,
                c.Status.ToString(), c.Resolution, c.CreatedAt))
            .ToListAsync();

        var punishments = await db.Punishments
            .Where(p => p.EmployeeId == TeacherId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new TeacherProfilePunishment(p.Id, p.Reason, p.Type, p.CreatedAt))
            .ToListAsync();

        var notifications = await db.Notifications
            .Where(n => n.UserId == TeacherId && n.UserType == UserType.Employee)
            .OrderByDescending(n => n.CreatedAt).Take(100)
            .Select(n => new TeacherProfileNotification(n.Id, n.Title, n.Body, n.Type, n.IsRead, n.CreatedAt))
            .ToListAsync();

        return Ok(new TeacherFullProfileResponse(teacher, schools, marks, attendance, leaves,
            perfReports, complaints, punishments, notifications));
    }
}
