CREATE PROCEDURE [Audit].[GetAuditLogByCorrelationId]
    @CorrelationId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;

    IF @CorrelationId IS NULL
       OR @CorrelationId =
            '00000000-0000-0000-0000-000000000000'
    BEGIN
        THROW 51301,
            N'The CorrelationId is required.',
            1;
    END;

    SELECT TOP (1)
        [AuditLogId],
        [CorrelationId],
        [ModuleName],
        [ActionName],
        [EntityName],
        [EntityId],
        [ActorType],
        [ActorUserId],
        [ActorEmailAddress],
        [ActorRoleCode],
        [IsSuccessful],
        [EventDescription],
        [ClientIpAddress],
        [UserAgent],
        [HttpMethod],
        [RequestPath],
        [OccurredAtUtc]

    FROM [Audit].[AuditLogs]

    WHERE [CorrelationId] = @CorrelationId

    ORDER BY [AuditLogId] DESC;
END;
GO