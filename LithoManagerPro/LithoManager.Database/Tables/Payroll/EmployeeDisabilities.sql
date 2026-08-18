CREATE TABLE [Payroll].[EmployeeDisabilities]
(
    [EmployeeDisabilityId] int IDENTITY(1,1) NOT NULL,
    [EmployeeId] int NOT NULL,
    [DisabilityTypeId] int NOT NULL,
    [IssuerInstitution] nvarchar(30) NOT NULL,
    [ReferenceNumber] nvarchar(100) NULL,
    [StartDate] date NOT NULL,
    [EndDate] date NOT NULL,
    [ReportedDate] date NOT NULL
        CONSTRAINT [DfEmployeeDisabilitiesReportedDate]
        DEFAULT (CONVERT(date, SYSUTCDATETIME())),

    [DisabilityStatus] nvarchar(30) NOT NULL
        CONSTRAINT [DfEmployeeDisabilitiesDisabilityStatus]
        DEFAULT (N'Pending'),

    [EmployerPaidAmount] decimal(18,2) NULL,
    [SubsidyAmount] decimal(18,2) NULL,
    [ApprovedAtUtc] datetime2(3) NULL,
    [ApprovedByUserId] int NULL,
    [CancelledAtUtc] datetime2(3) NULL,
    [CancelledByUserId] int NULL,
    [CancellationReason] nvarchar(300) NULL,
    [Notes] nvarchar(500) NULL,

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfEmployeeDisabilitiesCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [CreatedByUserId] int NULL,
    [UpdatedAtUtc] datetime2(3) NULL,
    [UpdatedByUserId] int NULL,
    [RowVersion] rowversion NOT NULL,

    CONSTRAINT [PkEmployeeDisabilities]
        PRIMARY KEY CLUSTERED ([EmployeeDisabilityId]),

    CONSTRAINT [FkEmployeeDisabilitiesEmployeesEmployeeId]
        FOREIGN KEY ([EmployeeId])
        REFERENCES [HumanResources].[Employees] ([EmployeeId]),

    CONSTRAINT [FkEmployeeDisabilitiesDisabilityTypesDisabilityTypeId]
        FOREIGN KEY ([DisabilityTypeId])
        REFERENCES [Payroll].[DisabilityTypes] ([DisabilityTypeId]),

    CONSTRAINT [FkEmployeeDisabilitiesUsersApprovedByUserId]
        FOREIGN KEY ([ApprovedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkEmployeeDisabilitiesUsersCancelledByUserId]
        FOREIGN KEY ([CancelledByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkEmployeeDisabilitiesUsersCreatedByUserId]
        FOREIGN KEY ([CreatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkEmployeeDisabilitiesUsersUpdatedByUserId]
        FOREIGN KEY ([UpdatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [CkEmployeeDisabilitiesIssuerInstitution]
        CHECK
        (
            [IssuerInstitution] IN
            (
                N'CCSS',
                N'INS',
                N'Employer',
                N'Other'
            )
        ),

    CONSTRAINT [CkEmployeeDisabilitiesReferenceNumberNotBlank]
        CHECK
        (
            [ReferenceNumber] IS NULL
            OR LEN(LTRIM(RTRIM([ReferenceNumber]))) > 0
        ),

    CONSTRAINT [CkEmployeeDisabilitiesReferenceNumberTrimmed]
        CHECK
        (
            [ReferenceNumber] IS NULL
            OR [ReferenceNumber] = LTRIM(RTRIM([ReferenceNumber]))
        ),

    CONSTRAINT [CkEmployeeDisabilitiesDates]
        CHECK ([EndDate] >= [StartDate]),

    CONSTRAINT [CkEmployeeDisabilitiesStatus]
        CHECK
        (
            [DisabilityStatus] IN
            (
                N'Pending',
                N'Approved',
                N'Cancelled'
            )
        ),

    CONSTRAINT [CkEmployeeDisabilitiesAmountsNonNegative]
        CHECK
        (
            ([EmployerPaidAmount] IS NULL OR [EmployerPaidAmount] >= 0)
            AND ([SubsidyAmount] IS NULL OR [SubsidyAmount] >= 0)
        ),

    CONSTRAINT [CkEmployeeDisabilitiesApprovalFields]
        CHECK
        (
            (
                [DisabilityStatus] <> N'Approved'
                AND [ApprovedAtUtc] IS NULL
                AND [ApprovedByUserId] IS NULL
            )
            OR
            (
                [DisabilityStatus] = N'Approved'
                AND [ApprovedAtUtc] IS NOT NULL
                AND [ApprovedByUserId] IS NOT NULL
                AND [CancelledAtUtc] IS NULL
                AND [CancelledByUserId] IS NULL
                AND [CancellationReason] IS NULL
            )
        ),

    CONSTRAINT [CkEmployeeDisabilitiesCancellationFields]
        CHECK
        (
            (
                [DisabilityStatus] <> N'Cancelled'
                AND [CancelledAtUtc] IS NULL
                AND [CancelledByUserId] IS NULL
                AND [CancellationReason] IS NULL
            )
            OR
            (
                [DisabilityStatus] = N'Cancelled'
                AND [CancelledAtUtc] IS NOT NULL
                AND [CancelledByUserId] IS NOT NULL
                AND LEN(LTRIM(RTRIM([CancellationReason]))) > 0
                AND [ApprovedAtUtc] IS NULL
                AND [ApprovedByUserId] IS NULL
            )
        ),

    CONSTRAINT [CkEmployeeDisabilitiesCancellationReasonTrimmed]
        CHECK
        (
            [CancellationReason] IS NULL
            OR [CancellationReason] = LTRIM(RTRIM([CancellationReason]))
        ),

    CONSTRAINT [CkEmployeeDisabilitiesNotesNotBlank]
        CHECK
        (
            [Notes] IS NULL
            OR LEN(LTRIM(RTRIM([Notes]))) > 0
        ),

    CONSTRAINT [CkEmployeeDisabilitiesNotesTrimmed]
        CHECK
        (
            [Notes] IS NULL
            OR [Notes] = LTRIM(RTRIM([Notes]))
        )
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UxEmployeeDisabilitiesReferenceNumber]
    ON [Payroll].[EmployeeDisabilities]
    (
        [ReferenceNumber]
    )
    WHERE [ReferenceNumber] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IxEmployeeDisabilitiesEmployeeIdDates]
    ON [Payroll].[EmployeeDisabilities]
    (
        [EmployeeId],
        [StartDate],
        [EndDate]
    )
    INCLUDE
    (
        [DisabilityTypeId],
        [DisabilityStatus],
        [IssuerInstitution]
    );
GO

CREATE NONCLUSTERED INDEX [IxEmployeeDisabilitiesStatusDates]
    ON [Payroll].[EmployeeDisabilities]
    (
        [DisabilityStatus],
        [StartDate],
        [EndDate]
    )
    INCLUDE
    (
        [EmployeeId],
        [DisabilityTypeId],
        [IssuerInstitution]
    );
GO
