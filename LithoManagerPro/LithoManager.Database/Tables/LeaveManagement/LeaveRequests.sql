CREATE TABLE [LeaveManagement].[LeaveRequests]
(
    [LeaveRequestId] int IDENTITY(1,1) NOT NULL,
    [EmployeeId] int NOT NULL,
    [LeaveTypeId] int NOT NULL,
    [LeaveRequestStatusCode] nvarchar(30) NOT NULL
        CONSTRAINT [DfLeaveRequestsLeaveRequestStatusCode]
        DEFAULT (N'Pending'),

    [StartDate] date NOT NULL,
    [EndDate] date NOT NULL,
    [RequestedDays] decimal(9,2) NOT NULL,

    [RespondedAtUtc] datetime2(3) NULL,
    [RespondedByUserId] int NULL,
    [CancelledAtUtc] datetime2(3) NULL,
    [CancelledByUserId] int NULL,

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfLeaveRequestsCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [CreatedByUserId] int NOT NULL,
    [UpdatedAtUtc] datetime2(3) NULL,
    [UpdatedByUserId] int NULL,
    [RowVersion] rowversion NOT NULL,

    CONSTRAINT [PkLeaveRequests]
        PRIMARY KEY CLUSTERED ([LeaveRequestId]),

    CONSTRAINT [FkLeaveRequestsEmployeesEmployeeId]
        FOREIGN KEY ([EmployeeId])
        REFERENCES [HumanResources].[Employees] ([EmployeeId]),

    CONSTRAINT [FkLeaveRequestsLeaveTypesLeaveTypeId]
        FOREIGN KEY ([LeaveTypeId])
        REFERENCES [LeaveManagement].[LeaveTypes] ([LeaveTypeId]),

    CONSTRAINT [FkLeaveRequestsLeaveRequestStatusesLeaveRequestStatusCode]
        FOREIGN KEY ([LeaveRequestStatusCode])
        REFERENCES [LeaveManagement].[LeaveRequestStatuses] ([LeaveRequestStatusCode]),

    CONSTRAINT [FkLeaveRequestsUsersRespondedByUserId]
        FOREIGN KEY ([RespondedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkLeaveRequestsUsersCancelledByUserId]
        FOREIGN KEY ([CancelledByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkLeaveRequestsUsersCreatedByUserId]
        FOREIGN KEY ([CreatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkLeaveRequestsUsersUpdatedByUserId]
        FOREIGN KEY ([UpdatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [CkLeaveRequestsDateRange]
        CHECK ([EndDate] >= [StartDate]),

    CONSTRAINT [CkLeaveRequestsRequestedDaysPositive]
        CHECK ([RequestedDays] > 0),

    CONSTRAINT [CkLeaveRequestsStatusTimestamps]
        CHECK
        (
            (
                [LeaveRequestStatusCode] = N'Pending'
                AND [RespondedAtUtc] IS NULL
                AND [RespondedByUserId] IS NULL
                AND [CancelledAtUtc] IS NULL
                AND [CancelledByUserId] IS NULL
            )
            OR
            (
                [LeaveRequestStatusCode] IN (N'Approved', N'Rejected')
                AND [RespondedAtUtc] IS NOT NULL
                AND [RespondedByUserId] IS NOT NULL
                AND [CancelledAtUtc] IS NULL
                AND [CancelledByUserId] IS NULL
            )
            OR
            (
                [LeaveRequestStatusCode] = N'Cancelled'
                AND [RespondedAtUtc] IS NULL
                AND [RespondedByUserId] IS NULL
                AND [CancelledAtUtc] IS NOT NULL
                AND [CancelledByUserId] IS NOT NULL
            )
        ),

    CONSTRAINT [CkLeaveRequestsRespondedAfterCreation]
        CHECK
        (
            [RespondedAtUtc] IS NULL
            OR [RespondedAtUtc] >= [CreatedAtUtc]
        ),

    CONSTRAINT [CkLeaveRequestsCancelledAfterCreation]
        CHECK
        (
            [CancelledAtUtc] IS NULL
            OR [CancelledAtUtc] >= [CreatedAtUtc]
        )
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UxLeaveRequestsEmployeeIdPending]
    ON [LeaveManagement].[LeaveRequests]
    (
        [EmployeeId]
    )
    WHERE [LeaveRequestStatusCode] = N'Pending';
GO

CREATE NONCLUSTERED INDEX [IxLeaveRequestsEmployeeIdStatusStartDate]
    ON [LeaveManagement].[LeaveRequests]
    (
        [EmployeeId],
        [LeaveRequestStatusCode],
        [StartDate] DESC
    )
    INCLUDE
    (
        [LeaveTypeId],
        [EndDate],
        [RequestedDays],
        [RespondedAtUtc],
        [CancelledAtUtc]
    );
GO

CREATE NONCLUSTERED INDEX [IxLeaveRequestsStatusStartDate]
    ON [LeaveManagement].[LeaveRequests]
    (
        [LeaveRequestStatusCode],
        [StartDate] DESC
    )
    INCLUDE
    (
        [EmployeeId],
        [LeaveTypeId],
        [EndDate],
        [RequestedDays],
        [CreatedAtUtc]
    );
GO
