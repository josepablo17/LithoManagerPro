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
using LithoManager.Application.Features.Authentication
    .RefreshTokens;


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

    private const string
    GetUserTokenValidationByIdProcedure =
        "Security.GetUserTokenValidationById";

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

    private const string CreateRefreshTokenProcedure =
        "Security.CreateRefreshToken";

    private const string
    GetRefreshTokenContextByTokenHashProcedure =
        "Security.GetRefreshTokenContextByTokenHash";

    private const string RotateRefreshTokenProcedure =
        "Security.RotateRefreshToken";

    private const string RevokeRefreshTokenProcedure =
        "Security.RevokeRefreshToken";

    private const string RevokeUserRefreshTokensProcedure =
        "Security.RevokeUserRefreshTokens";

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

    public async Task<UserTokenValidationData?>
    GetUserTokenValidationByIdAsync(
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
                GetUserTokenValidationByIdProcedure,
            parameters: parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await connection
            .QuerySingleOrDefaultAsync<
                UserTokenValidationData>(
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
            short maximumFailedLoginAttempts,
            int lockoutDurationMinutes,
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

        if (maximumFailedLoginAttempts is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumFailedLoginAttempts),
                "Maximum failed login attempts must be " +
                "between 1 and 20.");
        }

        if (lockoutDurationMinutes is < 1 or > 1440)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lockoutDurationMinutes),
                "Lockout duration minutes must be " +
                "between 1 and 1440.");
        }

        ArgumentNullException.ThrowIfNull(
            requestContext);

        var parameters = new
        {
            AttemptedEmailAddress =
                attemptedEmailAddress.Trim(),
            UserId = userId,
            MaximumFailedAttempts =
                maximumFailedLoginAttempts,
            LockoutDurationMinutes =
                lockoutDurationMinutes,
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

    public async Task<CreateRefreshTokenData>
    CreateRefreshTokenAsync(
        int userId,
        byte[] tokenHash,
        Guid tokenFamilyId,
        DateTime expiresAtUtc,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userId),
                "UserId must be greater than zero.");
        }

        ValidateTokenHash(
            tokenHash,
            nameof(tokenHash),
            "refresh token");

        if (tokenFamilyId == Guid.Empty)
        {
            throw new ArgumentException(
                "TokenFamilyId is required.",
                nameof(tokenFamilyId));
        }

        ValidateUtcDateTime(
            expiresAtUtc,
            nameof(expiresAtUtc),
            "refresh token expiration");

        ValidateRequestContext(requestContext);

        var parameters = new DynamicParameters();

        parameters.Add(
            "UserId",
            userId,
            DbType.Int32,
            ParameterDirection.Input);

        parameters.Add(
            "TokenHash",
            tokenHash,
            DbType.Binary,
            ParameterDirection.Input,
            size: 32);

        parameters.Add(
            "TokenFamilyId",
            tokenFamilyId,
            DbType.Guid,
            ParameterDirection.Input);

        parameters.Add(
            "ExpiresAtUtc",
            expiresAtUtc,
            DbType.DateTime2,
            ParameterDirection.Input);

        AddRequestContextParameters(
            parameters,
            requestContext);

        var command = new CommandDefinition(
            commandText:
                CreateRefreshTokenProcedure,
            parameters:
                parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        CreateRefreshTokenData result =
            await connection
                .QuerySingleAsync<CreateRefreshTokenData>(
                    command);

        result.ExpiresAtUtc =
            DateTime.SpecifyKind(
                result.ExpiresAtUtc,
                DateTimeKind.Utc);

        result.CreatedAtUtc =
            DateTime.SpecifyKind(
                result.CreatedAtUtc,
                DateTimeKind.Utc);

        return result;
    }

    public async Task<RefreshTokenContextData?>
    GetRefreshTokenContextByTokenHashAsync(
        byte[] tokenHash,
        CancellationToken cancellationToken)
    {
        ValidateTokenHash(
            tokenHash,
            nameof(tokenHash),
            "refresh token");

        var parameters = new DynamicParameters();

        parameters.Add(
            "TokenHash",
            tokenHash,
            DbType.Binary,
            ParameterDirection.Input,
            size: 32);

        var command = new CommandDefinition(
            commandText:
                GetRefreshTokenContextByTokenHashProcedure,
            parameters:
                parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        RefreshTokenContextData? result =
            await connection
                .QuerySingleOrDefaultAsync<
                    RefreshTokenContextData>(
                        command);

        if (result is null)
        {
            return null;
        }

        result.ExpiresAtUtc =
            DateTime.SpecifyKind(
                result.ExpiresAtUtc,
                DateTimeKind.Utc);

        result.CreatedAtUtc =
            DateTime.SpecifyKind(
                result.CreatedAtUtc,
                DateTimeKind.Utc);

        if (result.LastUsedAtUtc
            is DateTime returnedLastUsedAtUtc)
        {
            result.LastUsedAtUtc =
                DateTime.SpecifyKind(
                    returnedLastUsedAtUtc,
                    DateTimeKind.Utc);
        }

        return result;
    }

    public async Task<RotateRefreshTokenData>
    RotateRefreshTokenAsync(
        byte[] currentTokenHash,
        byte[] newTokenHash,
        DateTime expiresAtUtc,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        ValidateTokenHash(
            currentTokenHash,
            nameof(currentTokenHash),
            "current refresh token");

        ValidateTokenHash(
            newTokenHash,
            nameof(newTokenHash),
            "new refresh token");

        if (currentTokenHash.SequenceEqual(newTokenHash))
        {
            throw new ArgumentException(
                "The new refresh token hash must be " +
                "different from the current hash.",
                nameof(newTokenHash));
        }

        ValidateUtcDateTime(
            expiresAtUtc,
            nameof(expiresAtUtc),
            "refresh token expiration");

        ValidateRequestContext(requestContext);

        var parameters = new DynamicParameters();

        parameters.Add(
            "CurrentTokenHash",
            currentTokenHash,
            DbType.Binary,
            ParameterDirection.Input,
            size: 32);

        parameters.Add(
            "NewTokenHash",
            newTokenHash,
            DbType.Binary,
            ParameterDirection.Input,
            size: 32);

        parameters.Add(
            "ExpiresAtUtc",
            expiresAtUtc,
            DbType.DateTime2,
            ParameterDirection.Input);

        AddRequestContextParameters(
            parameters,
            requestContext);

        var command = new CommandDefinition(
            commandText:
                RotateRefreshTokenProcedure,
            parameters:
                parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        RotateRefreshTokenData result =
            await connection
                .QuerySingleAsync<RotateRefreshTokenData>(
                    command);

        if (result.ExpiresAtUtc
            is DateTime returnedExpiresAtUtc)
        {
            result.ExpiresAtUtc =
                DateTime.SpecifyKind(
                    returnedExpiresAtUtc,
                    DateTimeKind.Utc);
        }

        if (result.RotatedAtUtc
            is DateTime returnedRotatedAtUtc)
        {
            result.RotatedAtUtc =
                DateTime.SpecifyKind(
                    returnedRotatedAtUtc,
                    DateTimeKind.Utc);
        }

        return result;
    }

    public async Task<RevokeRefreshTokenData>
    RevokeRefreshTokenAsync(
        byte[] tokenHash,
        string revokedReason,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        ValidateTokenHash(
            tokenHash,
            nameof(tokenHash),
            "refresh token");

        ArgumentException.ThrowIfNullOrWhiteSpace(
            revokedReason);

        ValidateRequestContext(requestContext);

        var parameters = new DynamicParameters();

        parameters.Add(
            "TokenHash",
            tokenHash,
            DbType.Binary,
            ParameterDirection.Input,
            size: 32);

        AddRequestContextParameters(
            parameters,
            requestContext);

        parameters.Add(
            "RevokedReason",
            revokedReason.Trim(),
            DbType.String,
            ParameterDirection.Input,
            size: 100);

        var command = new CommandDefinition(
            commandText:
                RevokeRefreshTokenProcedure,
            parameters:
                parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        RevokeRefreshTokenData result =
            await connection
                .QuerySingleAsync<RevokeRefreshTokenData>(
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

    public async Task<RevokeUserRefreshTokensData>
    RevokeUserRefreshTokensAsync(
        int userId,
        string revokedReason,
        int? actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userId),
                "UserId must be greater than zero.");
        }

        if (actorUserId is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actorUserId),
                "ActorUserId must be greater than zero when provided.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            revokedReason);

        ValidateRequestContext(requestContext);

        var parameters = new DynamicParameters();

        parameters.Add(
            "UserId",
            userId,
            DbType.Int32,
            ParameterDirection.Input);

        AddRequestContextParameters(
            parameters,
            requestContext);

        parameters.Add(
            "RevokedReason",
            revokedReason.Trim(),
            DbType.String,
            ParameterDirection.Input,
            size: 100);

        parameters.Add(
            "ActorUserId",
            actorUserId,
            DbType.Int32,
            ParameterDirection.Input);

        var command = new CommandDefinition(
            commandText:
                RevokeUserRefreshTokensProcedure,
            parameters:
                parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        RevokeUserRefreshTokensData result =
            await connection
                .QuerySingleAsync<RevokeUserRefreshTokensData>(
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

    private static void ValidateTokenHash(
        byte[] tokenHash,
        string parameterName,
        string tokenName)
    {
        ArgumentNullException.ThrowIfNull(
            tokenHash,
            parameterName);

        if (tokenHash.Length != 32)
        {
            throw new ArgumentException(
                $"The {tokenName} hash must contain " +
                "exactly 32 bytes.",
                parameterName);
        }
    }

    private static void ValidateUtcDateTime(
        DateTime value,
        string parameterName,
        string valueName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                $"The {valueName} must use UTC.",
                parameterName);
        }
    }

    private static void ValidateRequestContext(
        AuthenticationRequestContext requestContext)
    {
        ArgumentNullException.ThrowIfNull(
            requestContext);

        if (requestContext.CorrelationId
            == Guid.Empty)
        {
            throw new ArgumentException(
                "CorrelationId is required.",
                nameof(requestContext));
        }
    }

    private static void AddRequestContextParameters(
        DynamicParameters parameters,
        AuthenticationRequestContext requestContext)
    {
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
    }
}
