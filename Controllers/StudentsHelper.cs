using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Api.Data;
using SchoolManagement.Api.Dtos;
using SchoolManagement.Api.Models;

namespace SchoolManagement.Api.Controllers;


public static class StudentsHelper
{
    public static async Task<IActionResult> CreateAsync(
        AppDbContext db, int schoolId, StudentCreateRequest request, ControllerBase controller)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Students.AnyAsync(s => s.Email == email))
            return controller.BadRequest(new { message = "الإيميل مستخدم بالفعل" });
        if (request.SectionId is not null &&
            !await db.Sections.AnyAsync(s => s.Id == request.SectionId && s.SchoolId == schoolId))
            return controller.BadRequest(new { message = "الشعبة غير موجودة في مدرستك" });

        var student = new Student
        {
            Name = request.Name,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            SchoolId = schoolId,
            SectionId = request.SectionId,
            GuardianName = request.GuardianName ?? "",
            GuardianPhone = request.GuardianPhone ?? "",
            BloodType = request.BloodType ?? "",
            ChronicDiseases = request.ChronicDiseases ?? "",
            Allergies = request.Allergies ?? "",
            HealthNotes = request.HealthNotes ?? "",
            BirthDate = request.BirthDate,
            Address = request.Address ?? "",
        };
        db.Students.Add(student);
        await db.SaveChangesAsync();
        return controller.Created($"api/students/{student.Id}", student);
    }

    public static async Task<IActionResult> UpdateAsync(
        AppDbContext db, int schoolId, int id, StudentUpdateRequest request, ControllerBase controller)
    {
        var student = await db.Students.FirstOrDefaultAsync(s => s.Id == id && s.SchoolId == schoolId);
        if (student is null) return controller.NotFound();
        if (request.SectionId is not null &&
            !await db.Sections.AnyAsync(s => s.Id == request.SectionId && s.SchoolId == schoolId))
            return controller.BadRequest(new { message = "الشعبة غير موجودة في مدرستك" });

        student.Name = request.Name ?? student.Name;
        student.SectionId = request.SectionId ?? student.SectionId;
        student.GuardianName = request.GuardianName ?? student.GuardianName;
        student.GuardianPhone = request.GuardianPhone ?? student.GuardianPhone;
        student.BloodType = request.BloodType ?? student.BloodType;
        student.ChronicDiseases = request.ChronicDiseases ?? student.ChronicDiseases;
        student.Allergies = request.Allergies ?? student.Allergies;
        student.HealthNotes = request.HealthNotes ?? student.HealthNotes;
        student.BirthDate = request.BirthDate ?? student.BirthDate;
        student.Address = request.Address ?? student.Address;
        if (request.Password is not null)
            student.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        await db.SaveChangesAsync();
        return controller.Ok(student);
    }
}
