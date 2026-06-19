using System.ComponentModel.DataAnnotations;
using SchoolManagement.Api.Models;

namespace SchoolManagement.Api.Dtos;


public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password,
    UserType? UserType,
    string? FcmToken);

public record LoginResponse(string Token, UserType UserType, string Role, int Id, string Name, int? SchoolId);

public record FcmTokenRequest([Required] string Token);


public record SchoolRequest(
    [Required] string Name,
    [Required] SchoolType Type,
    string? Address,
    string? Phone);

public record EmployeeCreateRequest(
    [Required] string Name,
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [Required] EmployeeRole Role,
    string? Phone,
    string? Address,
    DateTime? BirthDate,
    string? Qualification);

public record EmployeeUpdateRequest(
    string? Name,
    string? Phone,
    string? Address,
    DateTime? BirthDate,
    string? Qualification,
    [MinLength(6)] string? Password);

public record TransferRequest([Required] int SchoolId, int? SectionId);

public record LeaveRequest(
    [Required] int EmployeeId,
    [Required] DateOnly StartDate,
    [Required] DateOnly EndDate,
    string? Reason);


public record StudentCreateRequest(
    [Required] string Name,
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    int? SectionId,
    string? GuardianName,
    string? GuardianPhone,
    string? BloodType,
    string? ChronicDiseases,
    string? Allergies,
    string? HealthNotes,
    DateTime? BirthDate,
    string? Address);

public record StudentUpdateRequest(
    string? Name,
    int? SectionId,
    string? GuardianName,
    string? GuardianPhone,
    string? BloodType,
    string? ChronicDiseases,
    string? Allergies,
    string? HealthNotes,
    DateTime? BirthDate,
    string? Address,
    [MinLength(6)] string? Password);


public record GradeRequest([Required] string Name, [Required] int Level);
public record SectionRequest([Required] int GradeId, [Required] string Name, int? CounselorId);
public record SubjectRequest([Required] string Name, [Required] int GradeId, int? TeacherId);

public record SchedulePeriodRequest([Required] int Order, [Required] int SubjectId, [Required] int TeacherId);
public record ScheduleRequest(
    [Required] int SectionId,
    [Required] DayOfWeek Day,
    [Required] List<SchedulePeriodRequest> Periods);

public record StudentAttendanceEntry([Required] int StudentId, [Required] AttendanceStatus Status);
public record StudentAttendanceRequest(
    [Required] int SectionId,
    [Required] DateOnly Date,
    [Required] List<StudentAttendanceEntry> Entries);

public record EmployeeAttendanceEntry([Required] int EmployeeId, [Required] AttendanceStatus Status);
public record EmployeeAttendanceRequest(
    [Required] DateOnly Date,
    [Required] List<EmployeeAttendanceEntry> Entries);


public record MarkRequest(
    [Required] int StudentId,
    [Required] int SubjectId,
    [Required, Range(1, 2)] int Semester,
    [Range(0, 1000)] decimal Oral,
    [Range(0, 1000)] decimal Quiz1,
    [Range(0, 1000)] decimal Quiz2,
    [Range(0, 1000)] decimal Homework,
    [Range(0, 1000)] decimal FinalExam);

public record MarkConfigRequest(
    [Range(0, 1000)] decimal MaxOral,
    [Range(0, 1000)] decimal MaxQuiz1,
    [Range(0, 1000)] decimal MaxQuiz2,
    [Range(0, 1000)] decimal MaxHomework,
    [Range(0, 1000)] decimal MaxFinalExam,
    [Range(0, 100)] decimal PassPercent);

public record ReportCardRequest(
    [Required] int SectionId,
    [Required, Range(1, 2)] int Semester,
    [Required] int Year);

public record PerformanceReportRequest(
    [Required] int StudentId,
    [Required] int SubjectId,
    [Required, Range(1, 2)] int Semester,
    string? Behavior,
    string? Notes);


public record ComplaintRequest([Required] string Against, [Required] string Content);
public record ComplaintResolveRequest([Required] ComplaintStatus Status, string? Resolution);
public record PunishmentRequest(int? StudentId, int? EmployeeId, [Required] string Reason, [Required] string Type);
public record WarningRequest([Required] int StudentId, [Required] WarningType Type, [Required] string Reason);
public record SummonRequest([Required] int StudentId, [Required] string Reason, [Required] DateOnly Date);
public record ContactGuardianRequest([Required] string Title, [Required] string Body);
public record AnnouncementRequest(
    [Required] string Title,
    [Required] string Body,
    AnnouncementAudience Audience = AnnouncementAudience.All,
    AnnouncementType Type = AnnouncementType.General);


public record BookRequest(
    [Required] string Title,
    string? Author,
    string? Isbn,
    [Required, Range(0, 100000)] int Copies);

public record MemberRequest([Required] int StudentId);
public record LoanRequest([Required] int BookId, [Required] int MemberId, [Required] DateOnly DueDate);
public record ReservationDecisionRequest([Required] ReservationStatus Status);


public record ActivityRequest(
    [Required] string Name,
    [Required] ActivityType Type,
    string? Schedule,
    [Required, Range(1, 100000)] int Capacity);

public record RegistrationDecisionRequest([Required] RegistrationStatus Status);

// ===== Student Full Profile =====
public record StudentBasicInfo(
    int Id, string Name, string Email, int SchoolId, int? SectionId,
    string GuardianName, string GuardianPhone, string BloodType,
    string ChronicDiseases, string Allergies, string HealthNotes,
    DateTime? BirthDate, string Address,
    bool DismissalWarning, DateTime CreatedAt);

public record StudentProfileSection(int Id, string Name, string GradeName, int GradeLevel);

public record StudentProfileSubject(int Id, string Name, int? TeacherId);

public record StudentProfileMark(
    int SubjectId, string SubjectName, int Semester,
    decimal Oral, decimal Quiz1, decimal Quiz2, decimal Homework,
    decimal FinalExam, decimal Total, DateTime UpdatedAt);

public record StudentProfileReportCardSubject(string SubjectName, decimal Total);
public record StudentProfileReportCard(
    int Id, int Semester, int Year, decimal Average, int? Rank, bool Passed,
    List<StudentProfileReportCardSubject> Subjects);

public record StudentProfilePerformanceReport(
    int Id, string SubjectName, int Semester, string Behavior,
    string Notes, DateTime CreatedAt);

public record StudentProfileAttendance(DateOnly Date, string Status);

public record StudentProfilePeriod(int Order, string SubjectName);
public record StudentProfileSchedule(string Day, List<StudentProfilePeriod> Periods);

public record StudentProfileLibraryMember(int Id, string Status);
public record StudentProfileLoan(int Id, string BookTitle, DateOnly LoanDate,
    DateOnly DueDate, DateOnly? ReturnDate, string Status);
public record StudentProfileReservation(int Id, string BookTitle, DateOnly Date, string Status);
public record StudentProfileLibrary(
    StudentProfileLibraryMember? Membership,
    List<StudentProfileLoan> Loans,
    List<StudentProfileReservation> Reservations);

public record StudentProfileActivity(int Id, string Name, string Type,
    string? Schedule, string? RegistrationStatus);

public record StudentProfileWarning(int Id, string Type, string Reason, DateTime CreatedAt);
public record StudentProfilePunishment(int Id, string Reason, string Type, DateTime CreatedAt);
public record StudentProfileGuardianSummon(int Id, string Reason, DateOnly Date, DateTime CreatedAt);
public record StudentProfileComplaint(int Id, string Against, string Content,
    string Status, string? Resolution, DateTime CreatedAt);
public record StudentProfileNotification(int Id, string Title, string Body,
    string Type, bool IsRead, DateTime CreatedAt);

public record StudentFullProfileResponse(
    StudentBasicInfo Student,
    StudentProfileSection? Section,
    List<StudentProfileSubject> Subjects,
    List<StudentProfileMark> Marks,
    List<StudentProfileReportCard> ReportCards,
    List<StudentProfilePerformanceReport> PerformanceReports,
    List<StudentProfileAttendance> Attendance,
    List<StudentProfileSchedule> Schedule,
    StudentProfileLibrary Library,
    List<StudentProfileActivity> Activities,
    List<StudentProfileWarning> Warnings,
    List<StudentProfilePunishment> Punishments,
    List<StudentProfileGuardianSummon> GuardianSummons,
    List<StudentProfileComplaint> Complaints,
    List<StudentProfileNotification> Notifications);

// ===== Teacher Full Profile =====
public record TeacherBasicInfo(
    int Id, string Name, string Email, int SchoolId,
    string Phone, string Address, DateTime? BirthDate, string Qualification,
    bool IsDismissed, DateTime CreatedAt);

public record TeacherProfileSubject(int Id, string Name, int GradeId, string GradeName, int GradeLevel);
public record TeacherProfileStudent(int Id, string Name);
public record TeacherProfileSection(int Id, string Name, string GradeName, int GradeLevel, List<TeacherProfileStudent> Students);
public record TeacherProfilePeriod(int Order, string SubjectName, int SectionId, string SectionName);
public record TeacherProfileDaySchedule(string Day, List<TeacherProfilePeriod> Periods);
public record TeacherSchoolInfo(
    int SchoolId,
    string SchoolName,
    List<TeacherProfileSubject> Subjects,
    List<TeacherProfileSection> Sections,
    List<TeacherProfileDaySchedule> Schedule);

public record TeacherProfileMark(
    int MarkId, int StudentId, string StudentName, int SubjectId, string SubjectName,
    int Semester, decimal Oral, decimal Quiz1, decimal Quiz2, decimal Homework,
    decimal FinalExam, decimal Total, DateTime UpdatedAt);

public record TeacherProfileAttendance(DateOnly Date, string Status);
public record TeacherProfileLeave(int Id, DateOnly StartDate, DateOnly EndDate, string Reason);
public record TeacherProfilePerformanceReport(
    int Id, int StudentId, string StudentName, string SubjectName,
    int Semester, string Behavior, string Notes, DateTime CreatedAt);
public record TeacherProfileComplaint(int Id, string Against, string Content,
    string Status, string? Resolution, DateTime CreatedAt);
public record TeacherProfilePunishment(int Id, string Reason, string Type, DateTime CreatedAt);
public record TeacherProfileNotification(int Id, string Title, string Body,
    string Type, bool IsRead, DateTime CreatedAt);

// ===== Paginated Response (generic) =====
public record PaginatedResponse<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

// ===== Counselor Full Profile =====
public record CounselorBasicInfo(int Id, string Name, string Email, string Phone, DateTime CreatedAt);
public record CounselorSectionInfo(int Id, string Name, string GradeName, int GradeLevel, int StudentCount);
public record CounselorWarningSimple(int Id, int StudentId, string StudentName, string Type, string Reason, DateTime CreatedAt);
public record CounselorSummonSimple(int Id, int StudentId, string StudentName, string Reason, DateOnly Date, DateTime CreatedAt);
public record CounselorAttendanceRecent(int StudentId, string StudentName, DateOnly Date, string Status);
public record CounselorStudentItem(int Id, string Name, string? BloodType, string? GuardianPhone, bool DismissalWarning);

public record CounselorFullProfileResponse(
    CounselorBasicInfo Counselor,
    List<CounselorSectionInfo> Sections,
    List<CounselorWarningSimple> Warnings,
    List<CounselorSummonSimple> GuardianSummons,
    List<CounselorAttendanceRecent> RecentAttendance);

public record TeacherFullProfileResponse(
    TeacherBasicInfo Teacher,
    List<TeacherSchoolInfo> Schools,
    List<TeacherProfileMark> Marks,
    List<TeacherProfileAttendance> Attendance,
    List<TeacherProfileLeave> Leaves,
    List<TeacherProfilePerformanceReport> PerformanceReports,
    List<TeacherProfileComplaint> Complaints,
    List<TeacherProfilePunishment> Punishments,
    List<TeacherProfileNotification> Notifications);
