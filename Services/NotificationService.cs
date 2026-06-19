using FirebaseAdmin.Messaging;
using SchoolManagement.Api.Data;
using SchoolManagement.Api.Models;

namespace SchoolManagement.Api.Services;

public class NotificationService(AppDbContext db, ILogger<NotificationService> logger)
{
    public async Task SendAsync(int userId, UserType userType, string title, string body, string type = "general")
    {
        db.Notifications.Add(new Models.Notification
        {
            UserId = userId,
            UserType = userType,
            Title = title,
            Body = body,
            Type = type,
        });
        await db.SaveChangesAsync();

        var fcmToken = userType switch
        {
            UserType.Admin => (await db.Admins.FindAsync(userId))?.FcmToken,
            UserType.Employee => (await db.Employees.FindAsync(userId))?.FcmToken,
            UserType.Student => (await db.Students.FindAsync(userId))?.FcmToken,
            _ => null,
        };
        await PushAsync(fcmToken, title, body);
    }

   
    public async Task SendToGuardianAsync(Student student, string title, string body, string type = "guardian")
    {
        db.Notifications.Add(new Models.Notification
        {
            UserId = student.Id,
            UserType = UserType.Student,
            Title = title,
            Body = body,
            Type = type,
        });
        await db.SaveChangesAsync();
        await PushAsync(student.GuardianFcmToken, title, body);
    }

    private async Task PushAsync(string? fcmToken, string title, string body)
    {
        if (!FirebaseInitializer.IsReady || string.IsNullOrWhiteSpace(fcmToken)) return;
        try
        {
            await FirebaseMessaging.DefaultInstance.SendAsync(new Message
            {
                Token = fcmToken,
                Notification = new FirebaseAdmin.Messaging.Notification { Title = title, Body = body },
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "FCM push failed");
        }
    }
}
