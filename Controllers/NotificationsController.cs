using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Api.Auth;
using SchoolManagement.Api.Data;
using SchoolManagement.Api.Dtos;
using SchoolManagement.Api.Models;

namespace SchoolManagement.Api.Controllers;


[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(AppDbContext db) : ControllerBase
{
    private UserType CurrentUserType() => User.GetRole() switch
    {
        Roles.Admin => UserType.Admin,
        Roles.Student => UserType.Student,
        _ => UserType.Employee,
    };

    [HttpGet]
    public async Task<IActionResult> GetMine()
    {
        var userId = User.GetUserId();
        var userType = CurrentUserType();
        var items = await db.Notifications
            .Where(n => n.UserId == userId && n.UserType == userType)
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .ToListAsync();
        return Ok(items);
    }

    [HttpPatch("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var notification = await db.Notifications.FindAsync(id);
        if (notification is null || notification.UserId != User.GetUserId() || notification.UserType != CurrentUserType())
            return NotFound();
        notification.IsRead = true;
        await db.SaveChangesAsync();
        return Ok(notification);
    }


    [HttpPost("fcm-token")]
    public async Task<IActionResult> UpdateFcmToken(FcmTokenRequest request)
    {
        var userId = User.GetUserId();
        switch (CurrentUserType())
        {
            case UserType.Admin:
                var admin = await db.Admins.FindAsync(userId);
                if (admin is null) return NotFound();
                admin.FcmToken = request.Token;
                break;
            case UserType.Student:
                var student = await db.Students.FindAsync(userId);
                if (student is null) return NotFound();
                student.FcmToken = request.Token;
                break;
            default:
                var employee = await db.Employees.FindAsync(userId);
                if (employee is null) return NotFound();
                employee.FcmToken = request.Token;
                break;
        }
        await db.SaveChangesAsync();
        return Ok(new { message = "تم تحديث رمز الإشعارات" });
    }
}
