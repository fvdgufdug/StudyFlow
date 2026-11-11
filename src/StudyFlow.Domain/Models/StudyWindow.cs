namespace StudyFlow.Domain.Models;

public sealed record StudyWindow(Guid Id, DateTime Start, int Minutes, int Revision = 0);
public sealed record StudySession(Guid Id, Guid WindowId, Guid TaskId, DateTime Start, int Minutes);
