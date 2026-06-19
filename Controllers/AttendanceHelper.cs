using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Api.Data;
using SchoolManagement.Api.Dtos;
using SchoolManagement.Api.Models;

namespace SchoolManagement.Api.Controllers;


public static class AttendanceHelper
{
    public static async Task<IActionResult> RecordAsync(
        AppDbContext db, StudentAttendanceRequest request, int takenById, ControllerBase controller)
    {
        foreach (var entry in request.Entries)
        {
            if (!await db.Students.AnyAsync(s => s.Id == entry.StudentId && s.SectionId == request.SectionId))
                return controller.BadRequest(new { message = $"الطالب {entry.StudentId} ليس في هذه الشعبة" });

            var existing = await db.StudentAttendances
                .FirstOrDefaultAsync(a => a.StudentId == entry.StudentId && a.Date == request.Date);
            if (existing is not null)
            {
                existing.Status = entry.Status;
                existing.TakenById = takenById;
            }
            else
            {
                db.StudentAttendances.Add(new StudentAttendance
                {
                    StudentId = entry.StudentId,
                    SectionId = request.SectionId,
                    Date = request.Date,
                    Status = entry.Status,
                    TakenById = takenById,
                });
            }
        }
        await db.SaveChangesAsync();
        return controller.Ok(new { message = "تم تسجيل الحضور" });
    }
}
