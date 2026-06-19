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
[Route("api/activities")]
[Authorize(Roles = Roles.ActivitySupervisor)]
public class ActivitiesController(AppDbContext db, NotificationService notifier) : ControllerBase
{
    private int SchoolId => User.GetSchoolId();

    [HttpPost]
    public async Task<IActionResult> Create(ActivityRequest request)
    {
        var activity = new Activity
        {
            SchoolId = SchoolId,
            Name = request.Name,
            Type = request.Type,
            Schedule = request.Schedule ?? "",
            Capacity = request.Capacity,
            SupervisorId = User.GetUserId(),
        };
        db.Activities.Add(activity);
        await db.SaveChangesAsync();
        return Created($"api/activities/{activity.Id}", activity);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await db.Activities.Where(a => a.SchoolId == SchoolId).ToListAsync());

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ActivityRequest request)
    {
        var activity = await db.Activities.FirstOrDefaultAsync(a => a.Id == id && a.SchoolId == SchoolId);
        if (activity is null) return NotFound();
        activity.Name = request.Name;
        activity.Type = request.Type;
        activity.Schedule = request.Schedule ?? activity.Schedule;
        activity.Capacity = request.Capacity;
        await db.SaveChangesAsync();
        return Ok(activity);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var activity = await db.Activities.FirstOrDefaultAsync(a => a.Id == id && a.SchoolId == SchoolId);
        if (activity is null) return NotFound();
        db.ActivityRegistrations.RemoveRange(db.ActivityRegistrations.Where(r => r.ActivityId == id));
        db.Activities.Remove(activity);
        await db.SaveChangesAsync();
        return Ok(new { message = "تم حذف النشاط" });
    }


    [HttpGet("{id:int}/registrations")]
    public async Task<IActionResult> GetRegistrations(int id) =>
        Ok(await db.ActivityRegistrations.Include(r => r.Student)
            .Where(r => r.ActivityId == id &&
                        db.Activities.Any(a => a.Id == r.ActivityId && a.SchoolId == SchoolId))
            .ToListAsync());


    [HttpPatch("registrations/{id:int}")]
    public async Task<IActionResult> DecideRegistration(int id, RegistrationDecisionRequest request)
    {
        var registration = await db.ActivityRegistrations
            .Include(r => r.Activity)
            .FirstOrDefaultAsync(r => r.Id == id && r.Activity!.SchoolId == SchoolId);
        if (registration is null) return NotFound();

        if (request.Status == RegistrationStatus.Approved)
        {
            var approved = await db.ActivityRegistrations.CountAsync(r =>
                r.ActivityId == registration.ActivityId && r.Status == RegistrationStatus.Approved);
            if (approved >= registration.Activity!.Capacity)
                return BadRequest(new { message = "اكتملت سعة النشاط" });
        }

        registration.Status = request.Status;
        await db.SaveChangesAsync();
        await notifier.SendAsync(registration.StudentId, UserType.Student,
            request.Status == RegistrationStatus.Approved ? "قبول تسجيل نشاط" : "تحديث تسجيل نشاط",
            $"حالة تسجيلك في \"{registration.Activity!.Name}\": {request.Status}", "activity");
        return Ok(registration);
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
            Type = AnnouncementType.Activity,
            CreatedById = User.GetUserId(),
        };
        db.Announcements.Add(announcement);
        await db.SaveChangesAsync();
        return Created($"api/activities/announcements/{announcement.Id}", announcement);
    }
}
