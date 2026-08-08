using System.Data;
using Dapper;
using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Application.Features.Authentication
    .ChangeTemporaryPassword;
using LithoManager.Application.Features.Authentication
    .GetCurrentUser;
using LithoManager.Application.Features.Authentication.Login;
using LithoManager.Infrastructure.Persistence.Dapper;
using LithoManager.Application.Features.Authentication
    .ChangePassword;
using LithoManager.Application.Features.Authentication.ForgotPassword;
using LithoManager.Application.Features.Authentication
    .ResetPassword;


namespace LithoManager.Infrastructure.Persistence
    .Repositories.Security;

public sealed class AuthenticationRepository
    : IAuthenticationRepository
{
    private const string GetUserForAuthenticationProcedure =
        "Security.GetUserForAuthentication";

    private const string
    GetUserForAuthenticationByIdProcedure =
        "Security.GetUserForAuthenticationById";

    private const string GetCurrentUserByIdProcedure =
        "Security.GetCurrentUserById";

    private const string RegisterSuccessfulLoginProcedure =
        "Security.RegisterSuccessfulLogin";

    private const string RegisterFailedLoginProcedure =
        "Security.RegisterFailedLogin";

    private const string ChangeTemporaryPasswordProcedure =
        "Security.ChangeTemporaryPassword";

    private const string ChangePasswordProcedure =
    "Security.ChangePassword";

    private const string CreatePasswordResetTokenProcedure =
    "Security.CreatePasswordResetToken";

    private const string
    RevokePasswordResetTokenAfterDeliveryFailureProcedure =
        "Security." +
        "RevokePasswordResetTokenAfterDeliveryFailure";

    private const string
    GetPasswordResetContextByTokenHashProcedure =
        "Security.GetPasswordResetContextByTokenHash";

    private const string CompletePasswordResetProcedure =
        "Security.CompletePasswordReset";

    private readonly ISqlConnectionFactory _connectionFactory;

    public AuthenticationRepository(
        ISqlConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(
            connectionFactory);

        _connectionFactory = connectionFactory;
    }

    public async Task<AuthenticationUserData?>
        GetUserForAuthenticationAsync(
            string emailAddress,
            CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            emailAddress);

        var parameters = new
        {
            EmailAddress = emailAddress.Trim()
        };

        var command = new CommandDefinition(
            commandText:
                GetUserForAuthenticationProcedure,
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await connection
            .QuerySingleOrDefaultAsync<
                AuthenticationUserData>(
                    command);
    }

    public async Task<AuthenticationUserData?>
    GetUserForAuthenticationByIdAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userId),
                "UserId must be greater than zero.");
        }

        var parameters = new
        {
            UserId = userId
        };

        var command = new CommandDefinition(
            commandText:
                GetUserForAuthenticationByIdProcedure,
            parameters: parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await connection
            .QuerySingleOrDefaultAsync<
                AuthenticationUserData>(
                    command);
    }

    public async Task<CurrentUserData?>
        GetCurrentUserByIdAsync(
            int userId,
            CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userId),
                "UserId must be greater than zero.");
        }

        var parameters = new
        {
            UserId = userId
        };

        var command = new CommandDefinition(
            commandText:
                GetCurrentUserByIdProcedure,
            parameters: parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await connection
            .QuerySingleOrDefaultAsync<
                CurrentUserData>(
                    command);
    }

    public async Task<SuccessfulLoginRegistrationData>
        RegisterSuccessfulLoginAsync(
            int userId,
            AuthenticationRequestContext requestContext,
            CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userId),
                "UserId must be greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(
            requestContext);

        var parameters = new
        {
            UserId = userId,
            requestContext.CorrelationId,
            requestContext.ClientIpAddress,
            requestContext.UserAgent,
            requestContext.RequestPath
        };

        var command = new CommandDefinition(
            commandText:
                RegisterSuccessfulLoginProcedure,
            parameters: parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await connection
            .QuerySingleAsync<
                SuccessfulLoginRegistrationData>(
                    command);
    }

    public async Task<FailedLoginRegistrationData>
        RegisterFailedLoginAsync(
            string attemptedEmailAddress,
            int? userId,
            AuthenticationRequestContext requestContext,
            CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            attemptedEmailAddress);

        if (userId is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userId),
                "UserId must be greater than zero when provided.");
        }

        ArgumentNullException.ThrowIfNull(
            requestContext);

        var parameters = new
        {
            AttemptedEmailAddress =
                attemptedEmailAddress.Trim(),
            UserId = userId,
            requestContext.CorrelationId,
            requestContext.ClientIpAddress,
            requestContext.UserAgent,
            requestContext.RequestPath
        };

        var command = new CommandDefinition(
            commandText:
                RegisterFailedLoginProcedure,
            parameters: parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await connection
            .QuerySingleAsync<
                FailedLoginRegistrationData>(
                    command);
    }

    public async Task<TemporaryPasswordChangeData>
        ChangeTemporaryPasswordAsync(
            int userId,
            string newPasswordHash,
            AuthenticationRequestContext requestContext,
            CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userId),
                "UserId must be greater than zero.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            newPasswordHash);

        ArgumentNullException.ThrowIfNull(
            requestContext);

        var parameters = new
        {
            UserId = userId,
            NewPasswordHash = newPasswordHash,
            requestContext.CorrelationId,
            requestContext.ClientIpAddress,
            requestContext.UserAgent,
            requestContext.RequestPath
        };

        var command = new CommandDefinition(
            commandText:
                ChangeTemporaryPasswordProcedure,
            parameters: parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await connection
            .QuerySingleAsync<
                TemporaryPasswordChangeData>(
                    command);
    }

    public async Task<ChangePasswordData>
    ChangePasswordAsync(
        int userId,
        string newPasswordHash,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userId),
                "UserId must be greater than zero.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            newPasswordHash);

        ArgumentNullException.ThrowIfNull(
            requestContext);

        var parameters = new
        {
            UserId = userId,
            NewPasswordHash = newPasswordHash,
            requestContext.CorrelationId,
            requestContext.ClientIpAddress,
            requestContext.UserAgent,
            requestContext.RequestPath
        };

        var command = new CommandDefinition(
            commandText:
                ChangePasswordProcedure,
            parameters: parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await connection
            .QuerySingleAsync<ChangePasswordData>(
                command);
    }

    public async Task<CreatePasswordResetTokenData>
    CreatePasswordResetTokenAsync(
        string emailAddress,
        byte[] tokenHash,
        DateTime expiresAtUtc,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            emailAddress);

        ArgumentNullException.ThrowIfNull(
            tokenHash);

        ArgumentNullException.ThrowIfNull(
            requestContext);

        if (tokenHash.Length != 32)
        {
            throw new ArgumentException(
                "The password reset token hash must contain exactly 32 bytes.",
                nameof(tokenHash));
        }

        if (expiresAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "The password reset expiration must use UTC.",
                nameof(expiresAtUtc));
        }

        var parameters = new DynamicParameters();

        parameters.Add(
            "EmailAddress",
            emailAddress.Trim(),
            DbType.String,
            ParameterDirection.Input,
            size: 254);

        parameters.Add(
            "TokenHash",
            tokenHash,
            DbType.Binary,
            ParameterDirection.Input,
            size: 32);

        parameters.Add(
            "ExpiresAtUtc",
            expiresAtUtc,
            DbType.DateTime2,
            ParameterDirection.Input);

        parameters.Add(
            "CorrelationId",
            requestContext.CorrelationId,
            DbType.Guid,
            ParameterDirection.Input);

        parameters.Add(
            "ClientIpAddress",
            requestContext.ClientIpAddress,
            DbType.String,
            ParameterDirection.Input,
            size: 45);

        parameters.Add(
            "UserAgent",
            requestContext.UserAgent,
            DbType.String,
            ParameterDirection.Input,
            size: 512);

        parameters.Add(
            "RequestPath",
            requestContext.RequestPath,
            DbType.String,
            ParameterDirection.Input,
            size: 500);

        var command = new CommandDefinition(
            commandText:
                CreatePasswordResetTokenProcedure,
            parameters:
                parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        var result =
            await connection
                .QuerySingleAsync<CreatePasswordResetTokenData>(
                    command);

        if (result.ExpiresAtUtc
            is DateTime returnedExpiresAtUtc)
        {
            result.ExpiresAtUtc =
                DateTime.SpecifyKind(
                    returnedExpiresAtUtc,
                    DateTimeKind.Utc);
        }

        return result;
    }

    public async Task<RevokePasswordResetTokenData>
    RevokePasswordResetTokenAfterDeliveryFailureAsync(
        int passwordResetTokenId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        if (passwordResetTokenId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(passwordResetTokenId),
                "PasswordResetTokenId must be " +
                "greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(
            requestContext);

        if (requestContext.CorrelationId
            == Guid.Empty)
        {
            throw new ArgumentException(
                "CorrelationId is required.",
                nameof(requestContext));
        }

        var parameters = new
        {
            PasswordResetTokenId =
                passwordResetTokenId,

            requestContext.CorrelationId,
            requestContext.ClientIpAddress,
            requestContext.UserAgent,
            requestContext.RequestPath
        };

        var command = new CommandDefinition(
            commandText:
                RevokePasswordResetTokenAfterDeliveryFailureProcedure,
            parameters:
                parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        RevokePasswordResetTokenData result =
            await connection
                .QuerySingleAsync<
                    RevokePasswordResetTokenData>(
                        command);

        if (result.RevokedAtUtc
            is DateTime returnedRevokedAtUtc)
        {
            result.RevokedAtUtc =
                DateTime.SpecifyKind(
                    returnedRevokedAtUtc,
                    DateTimeKind.Utc);
        }

        return result;
    }

    public async Task<PasswordResetContextData?>
GetPasswordResetContextByTokenHashAsync(
    byte[] tokenHash,
    CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            tokenHash);

        if (tokenHash.Length != 32)
        {
            throw new ArgumentException(
                "The password reset token hash must " +
                "contain exactly 32 bytes.",
                nameof(tokenHash));
        }

        var parameters =
            new DynamicParameters();

        parameters.Add(
            "TokenHash",
            tokenHash,
            DbType.Binary,
            ParameterDirection.Input,
            size: 32);

        var command =
            new CommandDefinition(
                commandText:
                    GetPasswordResetContextByTokenHashProcedure,
                parameters:
                    parameters,
                commandType:
                    CommandType.StoredProcedure,
                cancellationToken:
                    cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        PasswordResetContextData? result =
            await connection
                .QuerySingleOrDefaultAsync<
                    PasswordResetContextData>(
                        command);

        if (result is not null)
        {
            result.ExpiresAtUtc =
                DateTime.SpecifyKind(
                    result.ExpiresAtUtc,
                    DateTimeKind.Utc);
        }

        return result;
    }

    public async Task<CompletePasswordResetData>
CompletePasswordResetAsync(
    byte[] tokenHash,
    string expectedPasswordHash,
    string newPasswordHash,
    AuthenticationRequestContext requestContext,
    CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            tokenHash);

        if (tokenHash.Length != 32)
        {
            throw new ArgumentException(
                "The password reset token hash must " +
                "contain exactly 32 bytes.",
                nameof(tokenHash));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            expectedPasswordHash);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            newPasswordHash);

        if (expectedPasswordHash.Length > 500)
        {
            throw new ArgumentException(
                "The expected password hash cannot " +
                "exceed 500 characters.",
                nameof(expectedPasswordHash));
        }

        if (newPasswordHash.Length > 500)
        {
            throw new ArgumentException(
                "The new password hash cannot " +
                "exceed 500 characters.",
                nameof(newPasswordHash));
        }

        ArgumentNullException.ThrowIfNull(
            requestContext);

        if (requestContext.CorrelationId
            == Guid.Empty)
        {
            throw new ArgumentException(
                "CorrelationId is required.",
                nameof(requestContext));
        }

        var parameters =
            new DynamicParameters();

        parameters.Add(
            "TokenHash",
            tokenHash,
            DbType.Binary,
            ParameterDirection.Input,
            size: 32);

        parameters.Add(
            "ExpectedPasswordHash",
            expectedPasswordHash,
            DbType.String,
            ParameterDirection.Input,
            size: 500);

        parameters.Add(
            "NewPasswordHash",
            newPasswordHash,
            DbType.String,
            ParameterDirection.Input,
            size: 500);

        parameters.Add(
            "CorrelationId",
            requestContext.CorrelationId,
            DbType.Guid,
            ParameterDirection.Input);

        parameters.Add(
            "ClientIpAddress",
            requestContext.ClientIpAddress,
            DbType.String,
            ParameterDirection.Input,
            size: 45);

        parameters.Add(
            "UserAgent",
            requestContext.UserAgent,
            DbType.String,
            ParameterDirection.Input,
            size: 512);

        parameters.Add(
            "RequestPath",
            requestContext.RequestPath,
            DbType.String,
            ParameterDirection.Input,
            size: 500);

        var command =
            new CommandDefinition(
                commandText:
                    CompletePasswordResetProcedure,
                parameters:
                    parameters,
                commandType:
                    CommandType.StoredProcedure,
                cancellationToken:
                    cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        CompletePasswordResetData result =
            await connection
                .QuerySingleAsync<
                    CompletePasswordResetData>(
                        command);

        if (result.PasswordChangedAtUtc
            is DateTime returnedPasswordChangedAtUtc)
        {
            result.PasswordChangedAtUtc =
                DateTime.SpecifyKind(
                    returnedPasswordChangedAtUtc,
                    DateTimeKind.Utc);
        }

        return result;
    }
}