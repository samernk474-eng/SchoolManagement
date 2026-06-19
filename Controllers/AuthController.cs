using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Api.Auth;
using SchoolManagement.Api.Data;
using SchoolManagement.Api.Dtos;
using SchoolManagement.Api.Models;

namespace SchoolManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, TokenService tokens) : ControllerBase
{

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (request.UserType is null or UserType.Admin)
        {
            var admin = await db.Admins.FirstOrDefaultAsync(a => a.Email == email);
            if (admin is not null && BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash))
            {
                if (request.FcmToken is not null) admin.FcmToken = request.FcmToken;
                await db.SaveChangesAsync();
                return new LoginResponse(
                    tokens.CreateToken(admin.Id, admin.Name, admin.Email, Roles.Admin, null),
                    UserType.Admin, Roles.Admin, admin.Id, admin.Name, null);
            }
            if (request.UserType == UserType.Admin) return Unauthorized(new { message = "بيانات الدخول غير صحيحة" });
        }

        if (request.UserType is null or UserType.Employee)
        {
            var employee = await db.Employees.FirstOrDefaultAsync(e => e.Email == email);
            if (employee is not null && BCrypt.Net.BCrypt.Verify(request.Password, employee.PasswordHash))
            {
                if (employee.IsDismissed)
                    return Unauthorized(new { message = "هذا الموظف مفصول من العمل" });
                if (request.FcmToken is not null) employee.FcmToken = request.FcmToken;
                await db.SaveChangesAsync();
                var role = employee.Role.ToString();
                return new LoginResponse(
                    tokens.CreateToken(employee.Id, employee.Name, employee.Email, role, employee.SchoolId),
                    UserType.Employee, role, employee.Id, employee.Name, employee.SchoolId);
            }
            if (request.UserType == UserType.Employee) return Unauthorized(new { message = "بيانات الدخول غير صحيحة" });
        }

        if (request.UserType is null or UserType.Student)
        {
            var student = await db.Students.FirstOrDefaultAsync(s => s.Email == email);
            if (student is not null && BCrypt.Net.BCrypt.Verify(request.Password, student.PasswordHash))
            {
                if (request.FcmToken is not null) student.FcmToken = request.FcmToken;
                await db.SaveChangesAsync();
                return new LoginResponse(
                    tokens.CreateToken(student.Id, student.Name, student.Email, Roles.Student, student.SchoolId),
                    UserType.Student, Roles.Student, student.Id, student.Name, student.SchoolId);
            }
        }

        return Unauthorized(new { message = "بيانات الدخول غير صحيحة" });
    }
}
