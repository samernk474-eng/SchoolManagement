```mermaid
erDiagram
    %% ========== CORE ==========
    School {
        int Id PK
        string Name
        string Type "SchoolType"
        string Address
        string Phone
        int AdminId "FK Admin logical"
        datetime CreatedAt
    }

    Admin {
        int Id PK
        string Name
        string Email "UK"
        string PasswordHash
        string FcmToken "nullable"
        datetime CreatedAt
    }

    Grade {
        int Id PK
        int SchoolId FK
        string Name
        int Level
    }

    Section {
        int Id PK
        int GradeId FK
        int SchoolId FK
        string Name
        int CounselorId "nullable FK Employee logical"
    }

    Subject {
        int Id PK
        string Name
        int GradeId FK
        int TeacherId "nullable FK Employee logical"
        int SchoolId FK
    }

    Schedule {
        int Id PK
        int SectionId FK "UK SectionId+Day"
        string Day "DayOfWeek"
    }

    SchedulePeriod {
        int Id PK
        int ScheduleId FK "cascade"
        int Order
        int SubjectId FK
        int TeacherId "FK Employee logical"
    }

    %% ========== USERS ==========
    Employee {
        int Id PK
        string Name
        string Email "UK"
        string PasswordHash
        string Role "EmployeeRole"
        int SchoolId FK "UK SchoolId+Role filtered"
        string Phone
        string Address
        datetime BirthDate "nullable"
        string Qualification
        string Photo
        string FcmToken "nullable"
        int UnexcusedAbsenceDays
        bool DismissalWarning
        bool IsDismissed
        datetime CreatedAt
    }

    Student {
        int Id PK
        string Name
        string Email "UK"
        string PasswordHash
        int SchoolId FK
        int SectionId FK "nullable"
        string GuardianName
        string GuardianPhone
        string GuardianFcmToken "nullable"
        string BloodType
        string ChronicDiseases
        string Allergies
        string HealthNotes
        datetime BirthDate "nullable"
        string Address
        string Photo
        string FcmToken "nullable"
        bool DismissalWarning
        datetime CreatedAt
    }

    %% ========== NOTIFICATIONS ==========
    Notification {
        int Id PK
        int UserId "polymorphic"
        string UserType "UserType enum"
        string Title
        string Body
        string Type
        bool IsRead
        datetime CreatedAt
    }

    %% ========== LIBRARY ==========
    LibraryMember {
        int Id PK
        int StudentId FK "UK"
        int SchoolId FK
        string Status "MemberStatus"
        datetime CreatedAt
    }

    Book {
        int Id PK
        int SchoolId FK
        string Title
        string Author
        string Isbn
        int Copies
        int AvailableCopies
    }

    BookLoan {
        int Id PK
        int BookId FK
        int MemberId FK
        date LoanDate
        date DueDate
        date ReturnDate "nullable"
        string Status "LoanStatus"
    }

    BookReservation {
        int Id PK
        int BookId FK
        int MemberId FK
        date Date
        string Status "ReservationStatus"
    }

    %% ========== ATTENDANCE ==========
    StudentAttendance {
        int Id PK
        int StudentId FK "UK StudentId+Date"
        int SectionId
        date Date
        string Status "AttendanceStatus"
        int TakenById "FK Employee logical"
    }

    EmployeeAttendance {
        int Id PK
        int EmployeeId FK "UK EmployeeId+Date"
        date Date
        string Status "AttendanceStatus"
        bool OnLeave
    }

    Leave {
        int Id PK
        int EmployeeId FK
        date StartDate
        date EndDate
        string Reason
        int GrantedByAdminId "FK Admin logical"
        datetime CreatedAt
    }

    %% ========== ASSESSMENT ==========
    MarkConfig {
        int Id PK
        int SchoolId FK "UK"
        decimal MaxOral
        decimal MaxQuiz1
        decimal MaxQuiz2
        decimal MaxHomework
        decimal MaxFinalExam
        decimal PassPercent
    }

    Mark {
        int Id PK
        int StudentId FK "UK StudentId+SubjectId+Semester"
        int SubjectId FK
        int Semester
        decimal Oral
        decimal Quiz1
        decimal Quiz2
        decimal Homework
        decimal FinalExam
        decimal Total
        int EnteredById "FK Employee logical"
        datetime UpdatedAt
    }

    ReportCard {
        int Id PK
        int StudentId FK "UK StudentId+Semester+Year"
        int Semester
        int Year
        decimal Average
        int Rank
        bool Passed
        int CreatedById "FK Employee logical"
        datetime CreatedAt
    }

    ReportCardSubject {
        int Id PK
        int ReportCardId FK "cascade"
        int SubjectId
        string SubjectName "denormalized"
        decimal Total
    }

    PerformanceReport {
        int Id PK
        int StudentId FK
        int TeacherId "FK Employee logical"
        int SubjectId
        int Semester
        string Behavior
        string Notes
        datetime CreatedAt
    }

    %% ========== DISCIPLINE ==========
    Complaint {
        int Id PK
        int FromUserId "polymorphic"
        string FromUserType "UserType enum"
        string FromName "denormalized"
        string Against
        int SchoolId FK
        string Content
        string Status "ComplaintStatus"
        string Resolution
        datetime CreatedAt
    }

    Punishment {
        int Id PK
        int StudentId "nullable FK Student logical"
        int EmployeeId "nullable FK Employee logical"
        int SchoolId FK
        string Reason
        string Type
        int IssuedById "FK Employee logical"
        datetime CreatedAt
    }

    Warning {
        int Id PK
        int StudentId FK
        string Type "WarningType"
        int IssuedById "nullable FK Employee logical"
        string Reason
        datetime CreatedAt
    }

    GuardianSummon {
        int Id PK
        int StudentId FK
        string Reason
        date Date
        int IssuedById "nullable FK Employee logical"
        datetime CreatedAt
    }

    Announcement {
        int Id PK
        int SchoolId FK
        string Title
        string Body
        string Audience "AnnouncementAudience"
        string Type "AnnouncementType"
        int CreatedById "FK Employee logical"
        datetime CreatedAt
    }

    %% ========== ACTIVITIES ==========
    Activity {
        int Id PK
        int SchoolId FK
        string Name
        string Type "ActivityType"
        string Schedule
        int Capacity
        int SupervisorId "nullable FK Employee logical"
        datetime CreatedAt
    }

    ActivityRegistration {
        int Id PK
        int ActivityId FK "UK ActivityId+StudentId"
        int StudentId FK
        string Status "RegistrationStatus"
        datetime CreatedAt
    }

    %% ========== RELATIONSHIPS ==========
    School ||--o{ Grade : has
    School ||--o{ Section : has
    School ||--o{ Subject : has
    School ||--o{ Employee : employs
    School ||--o{ Student : enrolls
    School ||--o{ Book : owns
    School ||--o{ LibraryMember : has
    School ||--o{ Complaint : receives
    School ||--o{ Punishment : records
    School ||--o{ Announcement : publishes
    School ||--o{ Activity : organizes
    School ||--o{ MarkConfig : configures

    Grade ||--o{ Section : contains
    Grade ||--o{ Subject : offers

    Section ||--o{ Student : assigned
    Section ||--o{ Schedule : has

    Schedule ||--o{ SchedulePeriod : contains_cascade

    Student ||--o{ StudentAttendance : records
    Student ||--o{ Mark : earns
    Student ||--o{ ReportCard : receives
    Student ||--o{ PerformanceReport : evaluated
    Student ||--o{ Warning : gets
    Student ||--o{ GuardianSummon : summoned
    Student ||--o{ Punishment : receives
    Student ||--|| LibraryMember : is_member

    Employee ||--o{ EmployeeAttendance : records
    Employee ||--o{ Leave : takes

    LibraryMember ||--o{ BookLoan : borrows
    LibraryMember ||--o{ BookReservation : reserves

    Book ||--o{ BookLoan : lent
    Book ||--o{ BookReservation : reserved

    Activity ||--o{ ActivityRegistration : has

    Subject ||--o{ SchedulePeriod : taught
    Subject ||--o{ Mark : assessed
```
