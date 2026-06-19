using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Api.Auth;
using SchoolManagement.Api.Data;
using SchoolManagement.Api.Dtos;
using SchoolManagement.Api.Models;

namespace SchoolManagement.Api.Controllers;


[ApiController]
[Route("api/student")]
[Authorize(Roles = Roles.Student)]
public class StudentController(AppDbContext db) : ControllerBase
{
    private int StudentId => User.GetUserId();
    private int SchoolId => User.GetSchoolId();

    private async Task<Student?> MeAsync() => await db.Students.FindAsync(StudentId);


    [HttpGet("schedule")]
    public async Task<IActionResult> GetSchedule()
    {
        var me = await MeAsync();
        if (me?.SectionId is null) return Ok(Array.Empty<object>());
        var schedules = await db.Schedules
            .Include(s => s.Periods)
            .ThenInclude(p => p.Subject)
            .Where(s => s.SectionId == me.SectionId)
            .OrderBy(s => s.Day)
            .ToListAsync();
        return Ok(schedules);
    }

    [HttpGet("subjects")]
    public async Task<IActionResult> GetSubjects()
    {
        var me = await MeAsync();
        if (me?.SectionId is null) return Ok(Array.Empty<object>());
        var section = await db.Sections.FindAsync(me.SectionId);
        return Ok(await db.Subjects.Where(s => s.GradeId == section!.GradeId).ToListAsync());
    }

    [HttpGet("marks")]
    public async Task<IActionResult> GetMarks([FromQuery] int? semester)
    {
        var query = db.Marks.Include(m => m.Subject).Where(m => m.StudentId == StudentId);
        if (semester is not null) query = query.Where(m => m.Semester == semester);
        return Ok(await query.ToListAsync());
    }

    [HttpGet("report-cards")]
    public async Task<IActionResult> GetReportCards() =>
        Ok(await db.ReportCards.Include(r => r.Subjects)
            .Where(r => r.StudentId == StudentId)
            .OrderByDescending(r => r.Year).ThenByDescending(r => r.Semester)
            .ToListAsync());

    [HttpGet("attendance")]
    public async Task<IActionResult> GetAttendance() =>
        Ok(await db.StudentAttendances.Where(a => a.StudentId == StudentId)
            .OrderByDescending(a => a.Date).Take(200).ToListAsync());

    [HttpGet("announcements")]
    public async Task<IActionResult> GetAnnouncements() =>
        Ok(await db.Announcements
            .Where(a => a.SchoolId == SchoolId && a.Audience != AnnouncementAudience.Employees)
            .OrderByDescending(a => a.CreatedAt).Take(100).ToListAsync());


    [HttpPost("complaints")]
    public async Task<IActionResult> CreateComplaint(ComplaintRequest request)
    {
        var complaint = new Complaint
        {
            FromUserId = StudentId,
            FromUserType = UserType.Student,
            FromName = User.Identity?.Name ?? "",
            Against = request.Against,
            SchoolId = SchoolId,
            Content = request.Content,
        };
        db.Complaints.Add(complaint);
        await db.SaveChangesAsync();
        return Created($"api/student/complaints/{complaint.Id}", complaint);
    }

    [HttpGet("complaints")]
    public async Task<IActionResult> GetMyComplaints() =>
        Ok(await db.Complaints
            .Where(c => c.FromUserId == StudentId && c.FromUserType == UserType.Student)
            .OrderByDescending(c => c.CreatedAt).ToListAsync());



    [HttpGet("activities")]
    public async Task<IActionResult> GetActivities() =>
        Ok(await db.Activities.Where(a => a.SchoolId == SchoolId).ToListAsync());

    [HttpPost("activities/{id:int}/register")]
    public async Task<IActionResult> RegisterActivity(int id)
    {
        var activity = await db.Activities.FirstOrDefaultAsync(a => a.Id == id && a.SchoolId == SchoolId);
        if (activity is null) return NotFound();
        if (await db.ActivityRegistrations.AnyAsync(r => r.ActivityId == id && r.StudentId == StudentId))
            return BadRequest(new { message = "أنت مسجل في هذا النشاط بالفعل" });

        var approved = await db.ActivityRegistrations.CountAsync(r =>
            r.ActivityId == id && r.Status == RegistrationStatus.Approved);
        if (approved >= activity.Capacity)
            return BadRequest(new { message = "اكتملت سعة النشاط" });

        var registration = new ActivityRegistration { ActivityId = id, StudentId = StudentId };
        db.ActivityRegistrations.Add(registration);
        await db.SaveChangesAsync();
        return Created($"api/student/activities/{id}/register", registration);
    }

    [HttpGet("activities/registrations")]
    public async Task<IActionResult> GetMyRegistrations() =>
        Ok(await db.ActivityRegistrations.Include(r => r.Activity)
            .Where(r => r.StudentId == StudentId).ToListAsync());



    [HttpGet("library/books")]
    public async Task<IActionResult> GetBooks() =>
        Ok(await db.Books.Where(b => b.SchoolId == SchoolId).ToListAsync());

    [HttpGet("library/loans")]
    public async Task<IActionResult> GetMyLoans() =>
        Ok(await db.BookLoans.Include(l => l.Book)
            .Where(l => db.LibraryMembers.Any(m => m.Id == l.MemberId && m.StudentId == StudentId))
            .OrderByDescending(l => l.LoanDate).ToListAsync());


    [HttpPost("library/books/{id:int}/reserve")]
    public async Task<IActionResult> ReserveBook(int id)
    {
        var book = await db.Books.FirstOrDefaultAsync(b => b.Id == id && b.SchoolId == SchoolId);
        if (book is null) return NotFound();

        var member = await db.LibraryMembers.FirstOrDefaultAsync(m => m.StudentId == StudentId);
        if (member is null) return BadRequest(new { message = "لست عضواً في المكتبة — راجع أمين المكتبة" });
        if (member.Status != MemberStatus.Active) return BadRequest(new { message = "عضويتك موقوفة" });
        if (await db.BookReservations.AnyAsync(r =>
                r.BookId == id && r.MemberId == member.Id && r.Status == ReservationStatus.Pending))
            return BadRequest(new { message = "لديك حجز معلق على هذا الكتاب" });

        var reservation = new BookReservation
        {
            BookId = id,
            MemberId = member.Id,
            Date = DateOnly.FromDateTime(DateTime.Today),
        };
        db.BookReservations.Add(reservation);
        await db.SaveChangesAsync();
        return Created($"api/student/library/reservations/{reservation.Id}", reservation);
    }

    [HttpGet("library/reservations")]
    public async Task<IActionResult> GetMyReservations() =>
        Ok(await db.BookReservations.Include(r => r.Book)
            .Where(r => db.LibraryMembers.Any(m => m.Id == r.MemberId && m.StudentId == StudentId))
            .OrderByDescending(r => r.Date).ToListAsync());

    

    [HttpGet("warnings")]
    public async Task<IActionResult> GetMyWarnings() =>
        Ok(await db.Warnings.Where(w => w.StudentId == StudentId)
            .OrderByDescending(w => w.CreatedAt).ToListAsync());

    [HttpGet("punishments")]
    public async Task<IActionResult> GetMyPunishments() =>
        Ok(await db.Punishments.Where(p => p.StudentId == StudentId)
            .OrderByDescending(p => p.CreatedAt).ToListAsync());

    // ===== Full Profile =====

    [HttpGet("full-profile")]
    public async Task<IActionResult> GetFullProfile()
    {
        var me = await db.Students.FindAsync(StudentId);
        if (me is null) return NotFound();

        var student = new StudentBasicInfo(
            me.Id, me.Name, me.Email, me.SchoolId, me.SectionId,
            me.GuardianName, me.GuardianPhone, me.BloodType,
            me.ChronicDiseases, me.Allergies, me.HealthNotes,
            me.BirthDate, me.Address,
            me.DismissalWarning, me.CreatedAt);

        Section? section = me.SectionId is null ? null
            : await db.Sections.Include(s => s.Grade).FirstOrDefaultAsync(s => s.Id == me.SectionId);
        var sectionInfo = section is null ? null
            : new StudentProfileSection(section.Id, section.Name, section.Grade?.Name ?? "", section.Grade?.Level ?? 0);

        var subjects = section is null ? [] : await db.Subjects
            .Where(s => s.GradeId == section.GradeId)
            .Select(s => new StudentProfileSubject(s.Id, s.Name, s.TeacherId))
            .ToListAsync();

        var marks = await db.Marks.Where(m => m.StudentId == StudentId)
            .OrderByDescending(m => m.Semester)
            .Select(m => new StudentProfileMark(m.SubjectId, m.Subject!.Name, m.Semester,
                m.Oral, m.Quiz1, m.Quiz2, m.Homework, m.FinalExam, m.Total, m.UpdatedAt))
            .ToListAsync();

        var attendance = await db.StudentAttendances
            .Where(a => a.StudentId == StudentId)
            .OrderByDescending(a => a.Date).Take(200)
            .Select(a => new StudentProfileAttendance(a.Date, a.Status.ToString()))
            .ToListAsync();

        var reportCards = await db.ReportCards
            .Where(r => r.StudentId == StudentId)
            .OrderByDescending(r => r.Year).ThenByDescending(r => r.Semester)
            .Select(r => new StudentProfileReportCard(r.Id, r.Semester, r.Year, r.Average, r.Rank, r.Passed,
                r.Subjects.Select(s => new StudentProfileReportCardSubject(s.SubjectName, s.Total)).ToList()))
            .ToListAsync();

        var perfReports = await db.PerformanceReports
            .Where(r => r.StudentId == StudentId)
            .Join(db.Subjects, r => r.SubjectId, s => s.Id, (r, s) => new { r, s })
            .OrderByDescending(x => x.r.CreatedAt)
            .Select(x => new StudentProfilePerformanceReport(x.r.Id, x.s.Name, x.r.Semester, x.r.Behavior, x.r.Notes, x.r.CreatedAt))
            .ToListAsync();

        List<StudentProfileSchedule> schedule = [];
        if (me.SectionId is not null)
            schedule = await db.Schedules
                .Where(s => s.SectionId == me.SectionId)
                .OrderBy(s => s.Day)
                .Select(s => new StudentProfileSchedule(s.Day.ToString(),
                    s.Periods.OrderBy(p => p.Order)
                        .Select(p => new StudentProfilePeriod(p.Order, p.Subject!.Name))
                        .ToList()))
                .ToListAsync();

        var member = await db.LibraryMembers
            .Where(m => m.StudentId == StudentId)
            .Select(m => new StudentProfileLibraryMember(m.Id, m.Status.ToString()))
            .FirstOrDefaultAsync();

        var loans = member is null ? [] : await db.BookLoans
            .Where(l => l.MemberId == member.Id)
            .OrderByDescending(l => l.LoanDate)
            .Select(l => new StudentProfileLoan(l.Id, l.Book!.Title, l.LoanDate, l.DueDate, l.ReturnDate, l.Status.ToString()))
            .ToListAsync();

        var reservations = member is null ? [] : await db.BookReservations
            .Where(r => r.MemberId == member.Id)
            .OrderByDescending(r => r.Date)
            .Select(r => new StudentProfileReservation(r.Id, r.Book!.Title, r.Date, r.Status.ToString()))
            .ToListAsync();

        var library = new StudentProfileLibrary(member, loans, reservations);

        var activities = await db.ActivityRegistrations
            .Where(r => r.StudentId == StudentId)
            .Select(r => new StudentProfileActivity(r.Activity!.Id, r.Activity.Name, r.Activity.Type.ToString(),
                r.Activity.Schedule, r.Status.ToString()))
            .ToListAsync();

        var warnings = await db.Warnings.Where(w => w.StudentId == StudentId)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new StudentProfileWarning(w.Id, w.Type.ToString(), w.Reason, w.CreatedAt))
            .ToListAsync();

        var punishments = await db.Punishments.Where(p => p.StudentId == StudentId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new StudentProfilePunishment(p.Id, p.Reason, p.Type, p.CreatedAt))
            .ToListAsync();

        var summons = await db.GuardianSummons.Where(s => s.StudentId == StudentId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new StudentProfileGuardianSummon(s.Id, s.Reason, s.Date, s.CreatedAt))
            .ToListAsync();

        var complaints = await db.Complaints
            .Where(c => c.FromUserId == StudentId && c.FromUserType == UserType.Student)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new StudentProfileComplaint(c.Id, c.Against, c.Content, c.Status.ToString(), c.Resolution, c.CreatedAt))
            .ToListAsync();

        var notifications = await db.Notifications
            .Where(n => n.UserId == StudentId && n.UserType == UserType.Student)
            .OrderByDescending(n => n.CreatedAt).Take(100)
            .Select(n => new StudentProfileNotification(n.Id, n.Title, n.Body, n.Type, n.IsRead, n.CreatedAt))
            .ToListAsync();

        return Ok(new StudentFullProfileResponse(student, sectionInfo, subjects, marks, reportCards,
            perfReports, attendance, schedule, library, activities,
            warnings, punishments, summons, complaints, notifications));
    }
}
