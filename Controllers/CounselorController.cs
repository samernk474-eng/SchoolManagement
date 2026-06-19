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
[Route("api/counselor")]
[Authorize(Roles = Roles.Counselor)]
public class CounselorController(AppDbContext db, NotificationService notifier) : ControllerBase
{
    private int CounselorId => User.GetUserId();

    private IQueryable<Section> MySections() =>
        db.Sections.Where(s => s.CounselorId == CounselorId);

    [HttpGet("sections")]
    public async Task<IActionResult> GetSections() =>
        Ok(await MySections().Include(s => s.Grade).ToListAsync());



    [HttpGet("attendance")]
    public async Task<IActionResult> GetAttendance([FromQuery] int sectionId, [FromQuery] DateOnly? date)
    {
        if (!await MySections().AnyAsync(s => s.Id == sectionId))
            return BadRequest(new { message = "هذه الشعبة ليست من شعبك" });
        var query = db.StudentAttendances.Where(a => a.SectionId == sectionId);
        if (date is not null) query = query.Where(a => a.Date == date);
        return Ok(await query.OrderByDescending(a => a.Date).Take(500).ToListAsync());
    }

    [HttpPost("attendance")]
    public async Task<IActionResult> TakeAttendance(StudentAttendanceRequest request)
    {
        if (!await MySections().AnyAsync(s => s.Id == request.SectionId))
            return BadRequest(new { message = "هذه الشعبة ليست من شعبك" });
        return await AttendanceHelper.RecordAsync(db, request, CounselorId, this);
    }

    [HttpPost("warnings")]
    public async Task<IActionResult> CreateWarning(WarningRequest request)
    {
        var student = await StudentInMySectionsAsync(request.StudentId);
        if (student is null) return BadRequest(new { message = "الطالب ليس في شعبك" });

        var warning = new Warning
        {
            StudentId = request.StudentId,
            Type = request.Type,
            Reason = request.Reason,
            IssuedById = CounselorId,
        };
        db.Warnings.Add(warning);
        await db.SaveChangesAsync();
        await notifier.SendAsync(student.Id, UserType.Student, "تحذير", request.Reason, "warning");
        await notifier.SendToGuardianAsync(student, "تحذير لابنكم", $"{student.Name}: {request.Reason}", "warning");
        return Created($"api/counselor/warnings/{warning.Id}", warning);
    }

    [HttpGet("warnings")]
    public async Task<IActionResult> GetWarnings([FromQuery] int? studentId)
    {
        var query = db.Warnings.Where(w =>
            db.Students.Any(s => s.Id == w.StudentId &&
                                 s.SectionId != null &&
                                 db.Sections.Any(x => x.Id == s.SectionId && x.CounselorId == CounselorId)));
        if (studentId is not null) query = query.Where(w => w.StudentId == studentId);
        return Ok(await query.OrderByDescending(w => w.CreatedAt).ToListAsync());
    }

    [HttpPost("dismissal-warning")]
    public async Task<IActionResult> DismissalWarning(WarningRequest request)
    {
        var student = await StudentInMySectionsAsync(request.StudentId);
        if (student is null) return BadRequest(new { message = "الطالب ليس في شعبك" });

        student.DismissalWarning = true;
        db.Warnings.Add(new Warning
        {
            StudentId = student.Id,
            Type = WarningType.DismissalWarning,
            Reason = request.Reason,
            IssuedById = CounselorId,
        });
        await db.SaveChangesAsync();
        await notifier.SendAsync(student.Id, UserType.Student, "إنذار بالفصل", request.Reason, "dismissal_warning");
        await notifier.SendToGuardianAsync(student, "إنذار بالفصل لابنكم", $"{student.Name}: {request.Reason}", "dismissal_warning");
        return Ok(student);
    }

    [HttpPost("summon-guardian")]
    public async Task<IActionResult> SummonGuardian(SummonRequest request)
    {
        var student = await StudentInMySectionsAsync(request.StudentId);
        if (student is null) return BadRequest(new { message = "الطالب ليس في شعبك" });

        var summon = new GuardianSummon
        {
            StudentId = student.Id,
            Reason = request.Reason,
            Date = request.Date,
            IssuedById = CounselorId,
        };
        db.GuardianSummons.Add(summon);
        await db.SaveChangesAsync();
        await notifier.SendToGuardianAsync(student,
            "استدعاء ولي أمر", $"يرجى مراجعة المدرسة بتاريخ {request.Date} — {request.Reason}", "guardian_summon");
        return Created($"api/counselor/summons/{summon.Id}", summon);
    }

    [HttpGet("students/{studentId:int}/full-profile")]
    public async Task<IActionResult> GetStudentFullProfile(int studentId)
    {
        var student = await StudentInMySectionsAsync(studentId);
        if (student is null) return BadRequest(new { message = "الطالب ليس في شعبك" });

        var basic = new StudentBasicInfo(
            student.Id, student.Name, student.Email, student.SchoolId, student.SectionId,
            student.GuardianName, student.GuardianPhone, student.BloodType,
            student.ChronicDiseases, student.Allergies, student.HealthNotes,
            student.BirthDate, student.Address,
            student.DismissalWarning, student.CreatedAt);

        Section? section = student.SectionId is null ? null
            : await db.Sections.Include(s => s.Grade).FirstOrDefaultAsync(s => s.Id == student.SectionId);
        var sectionInfo = section is null ? null
            : new StudentProfileSection(section.Id, section.Name, section.Grade?.Name ?? "", section.Grade?.Level ?? 0);

        var subjects = section is null ? [] : await db.Subjects
            .Where(s => s.GradeId == section.GradeId)
            .Select(s => new StudentProfileSubject(s.Id, s.Name, s.TeacherId))
            .ToListAsync();

        var marks = await db.Marks.Where(m => m.StudentId == studentId)
            .OrderByDescending(m => m.Semester)
            .Select(m => new StudentProfileMark(m.SubjectId, m.Subject!.Name, m.Semester,
                m.Oral, m.Quiz1, m.Quiz2, m.Homework, m.FinalExam, m.Total, m.UpdatedAt))
            .ToListAsync();

        var attendance = await db.StudentAttendances
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.Date).Take(200)
            .Select(a => new StudentProfileAttendance(a.Date, a.Status.ToString()))
            .ToListAsync();

        var reportCards = await db.ReportCards.Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.Year).ThenByDescending(r => r.Semester)
            .Select(r => new StudentProfileReportCard(r.Id, r.Semester, r.Year, r.Average, r.Rank, r.Passed,
                r.Subjects.Select(s => new StudentProfileReportCardSubject(s.SubjectName, s.Total)).ToList()))
            .ToListAsync();

        var perfReports = await db.PerformanceReports.Where(r => r.StudentId == studentId)
            .Join(db.Subjects, r => r.SubjectId, s => s.Id, (r, s) => new { r, s })
            .OrderByDescending(x => x.r.CreatedAt)
            .Select(x => new StudentProfilePerformanceReport(x.r.Id, x.s.Name, x.r.Semester, x.r.Behavior, x.r.Notes, x.r.CreatedAt))
            .ToListAsync();

        List<StudentProfileSchedule> schedule = [];
        if (student.SectionId is not null)
            schedule = await db.Schedules.Where(s => s.SectionId == student.SectionId)
                .OrderBy(s => s.Day)
                .Select(s => new StudentProfileSchedule(s.Day.ToString(),
                    s.Periods.OrderBy(p => p.Order)
                        .Select(p => new StudentProfilePeriod(p.Order, p.Subject!.Name)).ToList()))
                .ToListAsync();

        var member = await db.LibraryMembers.Where(m => m.StudentId == studentId)
            .Select(m => new StudentProfileLibraryMember(m.Id, m.Status.ToString())).FirstOrDefaultAsync();

        var loans = member is null ? [] : await db.BookLoans.Where(l => l.MemberId == member.Id)
            .OrderByDescending(l => l.LoanDate)
            .Select(l => new StudentProfileLoan(l.Id, l.Book!.Title, l.LoanDate, l.DueDate, l.ReturnDate, l.Status.ToString()))
            .ToListAsync();

        var reservations = member is null ? [] : await db.BookReservations.Where(r => r.MemberId == member.Id)
            .OrderByDescending(r => r.Date)
            .Select(r => new StudentProfileReservation(r.Id, r.Book!.Title, r.Date, r.Status.ToString()))
            .ToListAsync();

        var library = new StudentProfileLibrary(member, loans, reservations);

        var activities = await db.ActivityRegistrations.Where(r => r.StudentId == studentId)
            .Select(r => new StudentProfileActivity(r.Activity!.Id, r.Activity.Name, r.Activity.Type.ToString(),
                r.Activity.Schedule, r.Status.ToString()))
            .ToListAsync();

        var warnings = await db.Warnings.Where(w => w.StudentId == studentId)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new StudentProfileWarning(w.Id, w.Type.ToString(), w.Reason, w.CreatedAt))
            .ToListAsync();

        var punishments = await db.Punishments.Where(p => p.StudentId == studentId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new StudentProfilePunishment(p.Id, p.Reason, p.Type, p.CreatedAt))
            .ToListAsync();

        var summons = await db.GuardianSummons.Where(s => s.StudentId == studentId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new StudentProfileGuardianSummon(s.Id, s.Reason, s.Date, s.CreatedAt))
            .ToListAsync();

        var complaints = await db.Complaints
            .Where(c => c.FromUserId == studentId && c.FromUserType == UserType.Student)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new StudentProfileComplaint(c.Id, c.Against, c.Content, c.Status.ToString(), c.Resolution, c.CreatedAt))
            .ToListAsync();

        var notifications = await db.Notifications
            .Where(n => n.UserId == studentId && n.UserType == UserType.Student)
            .OrderByDescending(n => n.CreatedAt).Take(100)
            .Select(n => new StudentProfileNotification(n.Id, n.Title, n.Body, n.Type, n.IsRead, n.CreatedAt))
            .ToListAsync();

        return Ok(new StudentFullProfileResponse(basic, sectionInfo, subjects, marks, reportCards,
            perfReports, attendance, schedule, library, activities,
            warnings, punishments, summons, complaints, notifications));
    }

    // ===== Full Profile =====

    [HttpGet("full-profile")]
    public async Task<IActionResult> GetFullProfile()
    {
        var me = await db.Employees.FindAsync(CounselorId);
        if (me is null) return NotFound();

        var counselor = new CounselorBasicInfo(me.Id, me.Name, me.Email, me.Phone, me.CreatedAt);

        var sections = await MySections().Include(s => s.Grade)
            .Select(s => new CounselorSectionInfo(s.Id, s.Name, s.Grade!.Name, s.Grade.Level,
                db.Students.Count(x => x.SectionId == s.Id)))
            .ToListAsync();

        var warnings = await db.Warnings
            .Where(w => db.Students.Any(s => s.Id == w.StudentId && s.SectionId != null &&
                db.Sections.Any(x => x.Id == s.SectionId && x.CounselorId == CounselorId)))
            .OrderByDescending(w => w.CreatedAt).Take(100)
            .Select(w => new CounselorWarningSimple(w.Id, w.StudentId, w.Student!.Name,
                w.Type.ToString(), w.Reason, w.CreatedAt))
            .ToListAsync();

        var summons = await db.GuardianSummons
            .Where(s => db.Students.Any(st => st.Id == s.StudentId && st.SectionId != null &&
                db.Sections.Any(x => x.Id == st.SectionId && x.CounselorId == CounselorId)))
            .OrderByDescending(s => s.CreatedAt).Take(100)
            .Select(s => new CounselorSummonSimple(s.Id, s.StudentId, s.Student!.Name,
                s.Reason, s.Date, s.CreatedAt))
            .ToListAsync();

        var recentAttendance = await db.StudentAttendances
            .Where(a => db.Students.Any(s => s.Id == a.StudentId && s.SectionId != null &&
                db.Sections.Any(x => x.Id == s.SectionId && x.CounselorId == CounselorId)))
            .OrderByDescending(a => a.Date).Take(100)
            .Select(a => new CounselorAttendanceRecent(a.StudentId, a.Student!.Name, a.Date, a.Status.ToString()))
            .ToListAsync();

        return Ok(new CounselorFullProfileResponse(counselor, sections, warnings, summons, recentAttendance));
    }

    // ===== Section Students (paginated) =====

    [HttpGet("sections/{sectionId:int}/students")]
    public async Task<IActionResult> GetSectionStudents(int sectionId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!await MySections().AnyAsync(s => s.Id == sectionId))
            return BadRequest(new { message = "هذه الشعبة ليست من شعبك" });

        var query = db.Students.Where(s => s.SectionId == sectionId);
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new CounselorStudentItem(s.Id, s.Name, s.BloodType, s.GuardianPhone, s.DismissalWarning))
            .ToListAsync();

        return Ok(new PaginatedResponse<CounselorStudentItem>(items, totalCount, page, pageSize, totalPages));
    }

    private async Task<Student?> StudentInMySectionsAsync(int studentId) =>
        await db.Students.FirstOrDefaultAsync(s =>
            s.Id == studentId && s.SectionId != null &&
            db.Sections.Any(x => x.Id == s.SectionId && x.CounselorId == CounselorId));
}
