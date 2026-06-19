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
[Route("api/librarian")]
[Authorize(Roles = Roles.Librarian)]
public class LibrarianController(AppDbContext db, NotificationService notifier) : ControllerBase
{
    private int SchoolId => User.GetSchoolId();


    [HttpPost("books")]
    public async Task<IActionResult> CreateBook(BookRequest request)
    {
        var book = new Book
        {
            SchoolId = SchoolId,
            Title = request.Title,
            Author = request.Author ?? "",
            Isbn = request.Isbn ?? "",
            Copies = request.Copies,
            AvailableCopies = request.Copies,
        };
        db.Books.Add(book);
        await db.SaveChangesAsync();
        return Created($"api/librarian/books/{book.Id}", book);
    }

    [HttpGet("books")]
    public async Task<IActionResult> GetBooks() =>
        Ok(await db.Books.Where(b => b.SchoolId == SchoolId).ToListAsync());

    [HttpPut("books/{id:int}")]
    public async Task<IActionResult> UpdateBook(int id, BookRequest request)
    {
        var book = await db.Books.FirstOrDefaultAsync(b => b.Id == id && b.SchoolId == SchoolId);
        if (book is null) return NotFound();
        var loaned = book.Copies - book.AvailableCopies;
        if (request.Copies < loaned)
            return BadRequest(new { message = $"يوجد {loaned} نسخة معارة — لا يمكن إنقاص النسخ دونها" });
        book.Title = request.Title;
        book.Author = request.Author ?? book.Author;
        book.Isbn = request.Isbn ?? book.Isbn;
        book.AvailableCopies = request.Copies - loaned;
        book.Copies = request.Copies;
        await db.SaveChangesAsync();
        return Ok(book);
    }

    [HttpDelete("books/{id:int}")]
    public async Task<IActionResult> DeleteBook(int id)
    {
        var book = await db.Books.FirstOrDefaultAsync(b => b.Id == id && b.SchoolId == SchoolId);
        if (book is null) return NotFound();
        if (await db.BookLoans.AnyAsync(l => l.BookId == id && l.Status == LoanStatus.Active))
            return BadRequest(new { message = "لا يمكن حذف كتاب له إعارات نشطة" });
        db.BookReservations.RemoveRange(db.BookReservations.Where(r => r.BookId == id));
        db.BookLoans.RemoveRange(db.BookLoans.Where(l => l.BookId == id));
        db.Books.Remove(book);
        await db.SaveChangesAsync();
        return Ok(new { message = "تم حذف الكتاب" });
    }


    [HttpPost("members")]
    public async Task<IActionResult> CreateMember(MemberRequest request)
    {
        if (!await db.Students.AnyAsync(s => s.Id == request.StudentId && s.SchoolId == SchoolId))
            return BadRequest(new { message = "الطالب غير موجود في مدرستك" });
        if (await db.LibraryMembers.AnyAsync(m => m.StudentId == request.StudentId))
            return BadRequest(new { message = "الطالب عضو في المكتبة بالفعل" });

        var member = new LibraryMember { StudentId = request.StudentId, SchoolId = SchoolId };
        db.LibraryMembers.Add(member);
        await db.SaveChangesAsync();
        return Created($"api/librarian/members/{member.Id}", member);
    }

    [HttpGet("members")]
    public async Task<IActionResult> GetMembers() =>
        Ok(await db.LibraryMembers.Include(m => m.Student).Where(m => m.SchoolId == SchoolId).ToListAsync());

    [HttpPatch("members/{id:int}/status")]
    public async Task<IActionResult> SetMemberStatus(int id, [FromQuery] MemberStatus status)
    {
        var member = await db.LibraryMembers.FirstOrDefaultAsync(m => m.Id == id && m.SchoolId == SchoolId);
        if (member is null) return NotFound();
        member.Status = status;
        await db.SaveChangesAsync();
        return Ok(member);
    }



    [HttpPost("loans")]
    public async Task<IActionResult> CreateLoan(LoanRequest request)
    {
        var book = await db.Books.FirstOrDefaultAsync(b => b.Id == request.BookId && b.SchoolId == SchoolId);
        if (book is null) return NotFound(new { message = "الكتاب غير موجود" });
        if (book.AvailableCopies <= 0) return BadRequest(new { message = "لا توجد نسخ متاحة" });

        var member = await db.LibraryMembers.Include(m => m.Student)
            .FirstOrDefaultAsync(m => m.Id == request.MemberId && m.SchoolId == SchoolId);
        if (member is null) return NotFound(new { message = "العضو غير موجود" });
        if (member.Status != MemberStatus.Active) return BadRequest(new { message = "عضوية الطالب موقوفة" });

        book.AvailableCopies--;
        var loan = new BookLoan
        {
            BookId = book.Id,
            MemberId = member.Id,
            LoanDate = DateOnly.FromDateTime(DateTime.Today),
            DueDate = request.DueDate,
        };
        db.BookLoans.Add(loan);

   
        var reservation = await db.BookReservations.FirstOrDefaultAsync(r =>
            r.BookId == book.Id && r.MemberId == member.Id && r.Status == ReservationStatus.Pending);
        if (reservation is not null) reservation.Status = ReservationStatus.Fulfilled;

        await db.SaveChangesAsync();
        return Created($"api/librarian/loans/{loan.Id}", loan);
    }

    [HttpPost("loans/{id:int}/return")]
    public async Task<IActionResult> ReturnLoan(int id)
    {
        var loan = await db.BookLoans.Include(l => l.Book)
            .FirstOrDefaultAsync(l => l.Id == id && l.Book!.SchoolId == SchoolId);
        if (loan is null) return NotFound();
        if (loan.Status == LoanStatus.Returned) return BadRequest(new { message = "الكتاب مُعاد بالفعل" });

        loan.Status = LoanStatus.Returned;
        loan.ReturnDate = DateOnly.FromDateTime(DateTime.Today);
        loan.Book!.AvailableCopies++;
        await db.SaveChangesAsync();
        return Ok(loan);
    }

    [HttpGet("loans")]
    public async Task<IActionResult> GetLoans([FromQuery] bool? overdue)
    {
        var query = db.BookLoans.Include(l => l.Book).Include(l => l.Member)
            .Where(l => l.Book!.SchoolId == SchoolId);
        if (overdue == true)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            query = query.Where(l => l.Status != LoanStatus.Returned && l.DueDate < today);
        }
        return Ok(await query.OrderByDescending(l => l.LoanDate).Take(500).ToListAsync());
    }


    [HttpPost("loans/notify-due")]
    public async Task<IActionResult> NotifyDue()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dueLoans = await db.BookLoans
            .Include(l => l.Book)
            .Include(l => l.Member).ThenInclude(m => m!.Student)
            .Where(l => l.Book!.SchoolId == SchoolId &&
                        l.Status != LoanStatus.Returned &&
                        l.DueDate <= today.AddDays(1))
            .ToListAsync();

        foreach (var loan in dueLoans)
        {
            if (loan.DueDate < today) loan.Status = LoanStatus.Overdue;
            var student = loan.Member?.Student;
            if (student is not null)
                await notifier.SendAsync(student.Id, UserType.Student,
                    "استحقاق إعادة كتاب", $"كتاب \"{loan.Book!.Title}\" مستحق الإعادة بتاريخ {loan.DueDate}", "library_due");
        }
        await db.SaveChangesAsync();
        return Ok(new { notified = dueLoans.Count });
    }



    [HttpGet("reservations")]
    public async Task<IActionResult> GetReservations() =>
        Ok(await db.BookReservations.Include(r => r.Book).Include(r => r.Member)
            .Where(r => r.Book!.SchoolId == SchoolId)
            .OrderByDescending(r => r.Date).ToListAsync());

    [HttpPatch("reservations/{id:int}")]
    public async Task<IActionResult> DecideReservation(int id, ReservationDecisionRequest request)
    {
        var reservation = await db.BookReservations.Include(r => r.Book)
            .FirstOrDefaultAsync(r => r.Id == id && r.Book!.SchoolId == SchoolId);
        if (reservation is null) return NotFound();
        reservation.Status = request.Status;
        await db.SaveChangesAsync();
        return Ok(reservation);
    }
}
