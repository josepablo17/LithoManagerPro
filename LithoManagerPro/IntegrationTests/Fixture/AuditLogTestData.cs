namespace LithoManager.IntegrationTests.Fixtures;

public sealed class AuditLogTestData
{
    public long AuditLogId { get; init; }

    public Guid CorrelationId { get; init; }

    public string ModuleName { get; init; } =
        string.Empty;

    public string ActionName { get; init; } =
        string.Empty;

    public string? EntityName { get; init; }

    public string? EntityId { get; init; }

    public string ActorType { get; init; } =
        string.Empty;

    public int? ActorUserId { get; init; }

    public string? ActorEmailAddress { get; init; }

    public string? ActorRoleCode { get; init; }

    public bool IsSuccessful { get; init; }

    public string? EventDescription { get; init; }

    public string? ClientIpAddress { get; init; }

    public string? UserAgent { get; init; }

    public string? HttpMethod { get; init; }

    public string? RequestPath { get; init; }

    public DateTime OccurredAtUtc { get; init; }
}