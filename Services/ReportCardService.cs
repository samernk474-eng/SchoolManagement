using Microsoft.EntityFrameworkCore;
using SchoolManagement.Api.Data;
using SchoolManagement.Api.Models;

namespace SchoolManagement.Api.Services;

public class ReportCardService(AppDbContext db)
{
    public async Task<List<ReportCard>> GenerateForSectionAsync(int sectionId, int semester, int year, int managerId)
    {
        var students = await db.Students
            .Where(s => s.SectionId == sectionId)
            .ToListAsync();
        if (students.Count == 0)
            throw new InvalidOperationException("لا يوجد طلاب في هذه الشعبة");

        var schoolId = students[0].SchoolId;
        var config = await db.MarkConfigs.FirstOrDefaultAsync(c => c.SchoolId == schoolId)
                     ?? new MarkConfig { SchoolId = schoolId };
        var maxTotal = config.MaxOral + config.MaxQuiz1 + config.MaxQuiz2 + config.MaxHomework + config.MaxFinalExam;
        var passMark = maxTotal * config.PassPercent / 100m;

        var studentIds = students.Select(s => s.Id).ToList();
        var marks = await db.Marks
            .Include(m => m.Subject)
            .Where(m => studentIds.Contains(m.StudentId) && m.Semester == semester)
            .ToListAsync();

       
        var old = await db.ReportCards
            .Where(r => studentIds.Contains(r.StudentId) && r.Semester == semester && r.Year == year)
            .ToListAsync();
        db.ReportCards.RemoveRange(old);

        var cards = new List<ReportCard>();
        foreach (var student in students)
        {
            var studentMarks = marks.Where(m => m.StudentId == student.Id).ToList();
            var card = new ReportCard
            {
                StudentId = student.Id,
                Semester = semester,
                Year = year,
                CreatedById = managerId,
                Subjects = studentMarks.Select(m => new ReportCardSubject
                {
                    SubjectId = m.SubjectId,
                    SubjectName = m.Subject?.Name ?? "",
                    Total = m.Total,
                }).ToList(),
            };
            card.Average = studentMarks.Count > 0 ? Math.Round(studentMarks.Average(m => m.Total), 2) : 0;
         
            card.Passed = studentMarks.Count > 0 && studentMarks.All(m => m.Total >= passMark);
            cards.Add(card);
        }

    
        var ranked = cards.OrderByDescending(c => c.Average).ToList();
        for (var i = 0; i < ranked.Count; i++)
            ranked[i].Rank = i + 1;

        db.ReportCards.AddRange(cards);
        await db.SaveChangesAsync();
        return ranked;
    }
}
