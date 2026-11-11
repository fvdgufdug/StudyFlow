using Microsoft.EntityFrameworkCore;
using StudyFlow.Domain.Models;

namespace StudyFlow.Infrastructure;

internal static class PlannerModel
{
    public static void Configure(ModelBuilder model)
    {
        model.Entity<StudyWindow>(e =>
        {
            e.ToTable("Windows", t => t.HasCheckConstraint("CK_Window_Minutes", "Minutes BETWEEN 15 AND 1440"));
            e.HasKey(w => w.Id);
            e.Property(w => w.Revision).IsConcurrencyToken();
            e.HasIndex(w => w.Start);
        });
        model.Entity<StudySession>(e =>
        {
            e.ToTable("Sessions", t => t.HasCheckConstraint("CK_Session_Minutes", "Minutes BETWEEN 1 AND 1440"));
            e.HasKey(s => s.Id);
            e.HasOne<StudyWindow>().WithMany().HasForeignKey(s => s.WindowId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<StudyTask>().WithMany().HasForeignKey(s => s.TaskId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(s => s.Start);
        });
        model.Entity<Course>(e =>
        {
            e.ToTable("Courses", t => t.HasCheckConstraint("CK_Course_Name", "length(trim(Name)) BETWEEN 1 AND 80"));
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(80);
            e.Property(c => c.Revision).IsConcurrencyToken();
        });
        model.Entity<StudyTask>(e =>
        {
            e.ToTable("Tasks", t =>
            {
                t.HasCheckConstraint("CK_Task_Title", "length(trim(Title)) BETWEEN 1 AND 120");
                t.HasCheckConstraint("CK_Task_Importance", "Importance BETWEEN 1 AND 5");
                t.HasCheckConstraint("CK_Task_Minutes", "EstimatedMinutes BETWEEN 15 AND 1440");
                t.HasCheckConstraint("CK_Task_Status", "Status BETWEEN 0 AND 2");
            });
            e.HasKey(t => t.Id);
            e.Property(t => t.Title).HasMaxLength(120);
            e.Property(t => t.Revision).IsConcurrencyToken();
            e.HasOne<Course>().WithMany().HasForeignKey(t => t.CourseId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(t => new { t.Status, t.DueDate });
        });
    }
}
