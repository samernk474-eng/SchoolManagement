using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Api.Auth;
using SchoolManagement.Api.Data;
using SchoolManagement.Api.Dtos;

namespace SchoolManagement.Api.Controllers;


[ApiController]
[Route("api/secretary")]
[Authorize(Roles = Roles.Secretary)]
public class SecretaryController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    private int SchoolId => User.GetSchoolId();

    [HttpPost("students")]
    public Task<IActionResult> Create(StudentCreateRequest request) =>
        StudentsHelper.CreateAsync(db, SchoolId, request, this);

    [HttpGet("students")]
    public async Task<IActionResult> GetAll() =>
        Ok(await db.Students.Include(s => s.Section).Where(s => s.SchoolId == SchoolId).ToListAsync());

    [HttpGet("students/{id:int}")]
    public async Task<IActionResult> GetOne(int id)
    {
        var student = await db.Students.Include(s => s.Section)
            .FirstOrDefaultAsync(s => s.Id == id && s.SchoolId == SchoolId);
        return student is null ? NotFound() : Ok(student);
    }

    [HttpPut("students/{id:int}")]
    public Task<IActionResult> Update(int id, StudentUpdateRequest request) =>
        StudentsHelper.UpdateAsync(db, SchoolId, id, request, this);

    [HttpDelete("students/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var student = await db.Students.FirstOrDefaultAsync(s => s.Id == id && s.SchoolId == SchoolId);
        if (student is null) return NotFound();


        var memberIds = db.LibraryMembers.Where(m => m.StudentId == id).Select(m => m.Id);
        db.BookLoans.RemoveRange(db.BookLoans.Where(l => memberIds.Contains(l.MemberId)));
        db.BookReservations.RemoveRange(db.BookReservations.Where(r => memberIds.Contains(r.MemberId)));
        db.LibraryMembers.RemoveRange(db.LibraryMembers.Where(m => m.StudentId == id));
        db.ActivityRegistrations.RemoveRange(db.ActivityRegistrations.Where(r => r.StudentId == id));
        db.StudentAttendances.RemoveRange(db.StudentAttendances.Where(a => a.StudentId == id));
        db.Marks.RemoveRange(db.Marks.Where(m => m.StudentId == id));
        db.Warnings.RemoveRange(db.Warnings.Where(w => w.StudentId == id));
        db.GuardianSummons.RemoveRange(db.GuardianSummons.Where(g => g.StudentId == id));
        db.PerformanceReports.RemoveRange(db.PerformanceReports.Where(p => p.StudentId == id));
        db.ReportCards.RemoveRange(db.ReportCards.Where(r => r.StudentId == id));

        db.Students.Remove(student);
        await db.SaveChangesAsync();
        return Ok(new { message = "تم حذف الطالب" });
    }


    [HttpPost("students/{id:int}/photo")]
    public async Task<IActionResult> UploadPhoto(int id, IFormFile file)
    {
        var student = await db.Students.FirstOrDefaultAsync(s => s.Id == id && s.SchoolId == SchoolId);
        if (student is null) return NotFound();
        if (file.Length == 0) return BadRequest(new { message = "ملف فارغ" });

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext)) return BadRequest(new { message = "صيغة الصورة غير مدعومة" });

        var uploads = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads");
        Directory.CreateDirectory(uploads);
        var fileName = $"student-{id}-{Guid.NewGuid():N}{ext}";
        await using (var stream = System.IO.File.Create(Path.Combine(uploads, fileName)))
        {
            await file.CopyToAsync(stream);
        }

        student.Photo = $"/uploads/{fileName}";
        await db.SaveChangesAsync();
        return Ok(new { photo = student.Photo });
    }
}
