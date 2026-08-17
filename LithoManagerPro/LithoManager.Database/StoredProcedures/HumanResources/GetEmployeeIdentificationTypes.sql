CREATE PROCEDURE [HumanResources].[GetEmployeeIdentificationTypes]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        IdentificationTypes.[IdentificationType],
        IdentificationTypes.[Name],
        IdentificationTypes.[MinLength],
        IdentificationTypes.[MaxLength],
        IdentificationTypes.[IsNumericOnly],
        IdentificationTypes.[AllowsLeadingZero],
        IdentificationTypes.[SortOrder]
    FROM
    (
        VALUES
        (
            N'CEDULA_FISICA',
            N'Cedula fisica',
            9,
            9,
            CONVERT(bit, 1),
            CONVERT(bit, 0),
            1
        ),
        (
            N'DIMEX',
            N'DIMEX',
            11,
            12,
            CONVERT(bit, 1),
            CONVERT(bit, 0),
            2
        ),
        (
            N'PASAPORTE',
            N'Pasaporte',
            6,
            20,
            CONVERT(bit, 0),
            CONVERT(bit, 1),
            3
        )
    ) AS IdentificationTypes
    (
        [IdentificationType],
        [Name],
        [MinLength],
        [MaxLength],
        [IsNumericOnly],
        [AllowsLeadingZero],
        [SortOrder]
    )
    ORDER BY
        IdentificationTypes.[SortOrder];
END;
GO
