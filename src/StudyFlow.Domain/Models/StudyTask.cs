namespace StudyFlow.Domain.Models;

public enum TaskStatus { ToDo, InProgress, Done }

public sealed record StudyTask(
    Guid Id,
    Guid CourseId,
    string Title,
    DateOnly DueDate,
    int Importance,
    int EstimatedMinutes,
    TaskStatus Status = TaskStatus.ToDo,
    int Revision = 0);
