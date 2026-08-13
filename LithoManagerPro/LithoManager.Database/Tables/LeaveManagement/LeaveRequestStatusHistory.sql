CREATE TABLE [LeaveManagement].[LeaveRequestStatusHistory]
(
    [LeaveRequestStatusHistoryId] int IDENTITY(1,1) NOT NULL,
    [LeaveRequestId] int NOT NULL,
    [FromLeaveRequestStatusCode] nvarchar(30) NULL,
    [ToLeaveRequestStatusCode] nvarchar(30) NOT NULL,

    [ChangedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfLeaveRequestStatusHistoryChangedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [ChangedByUserId] int NOT NULL,

    CONSTRAINT [PkLeaveRequestStatusHistory]
        PRIMARY KEY CLUSTERED ([LeaveRequestStatusHistoryId]),

    CONSTRAINT [FkLeaveRequestStatusHistoryLeaveRequestsLeaveRequestId]
        FOREIGN KEY ([LeaveRequestId])
        REFERENCES [LeaveManagement].[LeaveRequests] ([LeaveRequestId]),

    CONSTRAINT [FkLeaveRequestStatusHistoryLeaveRequestStatusesFromLeaveRequestStatusCode]
        FOREIGN KEY ([FromLeaveRequestStatusCode])
        REFERENCES [LeaveManagement].[LeaveRequestStatuses] ([LeaveRequestStatusCode]),

    CONSTRAINT [FkLeaveRequestStatusHistoryLeaveRequestStatusesToLeaveRequestStatusCode]
        FOREIGN KEY ([ToLeaveRequestStatusCode])
        REFERENCES [LeaveManagement].[LeaveRequestStatuses] ([LeaveRequestStatusCode]),

    CONSTRAINT [FkLeaveRequestStatusHistoryUsersChangedByUserId]
        FOREIGN KEY ([ChangedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [CkLeaveRequestStatusHistoryStatusChanged]
        CHECK
        (
            [FromLeaveRequestStatusCode] IS NULL
            OR [FromLeaveRequestStatusCode] <> [ToLeaveRequestStatusCode]
        )
);
GO

CREATE NONCLUSTERED INDEX [IxLeaveRequestStatusHistoryLeaveRequestIdChangedAtUtc]
    ON [LeaveManagement].[LeaveRequestStatusHistory]
    (
        [LeaveRequestId],
        [ChangedAtUtc] DESC
    )
    INCLUDE
    (
        [FromLeaveRequestStatusCode],
        [ToLeaveRequestStatusCode],
        [ChangedByUserId]
    );
GO
