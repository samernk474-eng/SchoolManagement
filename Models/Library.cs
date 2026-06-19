namespace SchoolManagement.Api.Models;

public class Book
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string Isbn { get; set; } = "";
    public int Copies { get; set; }
    public int AvailableCopies { get; set; }
}


public class LibraryMember
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public int SchoolId { get; set; }
    public MemberStatus Status { get; set; } = MemberStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


public class BookLoan
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public Book? Book { get; set; }
    public int MemberId { get; set; }
    public LibraryMember? Member { get; set; }
    public DateOnly LoanDate { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly? ReturnDate { get; set; }
    public LoanStatus Status { get; set; } = LoanStatus.Active;
}


public class BookReservation
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public Book? Book { get; set; }
    public int MemberId { get; set; }
    public LibraryMember? Member { get; set; }
    public DateOnly Date { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
}
