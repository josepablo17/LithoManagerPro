using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features.Authentication
    .GetCurrentUser;

public sealed class GetCurrentUserService
    : IGetCurrentUserService
{
    private readonly IAuthenticationRepository
        _authenticationRepository;

    public GetCurrentUserService(
        IAuthenticationRepository authenticationRepository)
    {
        ArgumentNullException.ThrowIfNull(
            authenticationRepository);

        _authenticationRepository =
            authenticationRepository;
    }

    public async Task<CurrentUserResult> GetAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            return CurrentUserResult.Failure(
                CurrentUserErrorCode.InvalidRequest);
        }

        CurrentUserData? currentUser =
            await _authenticationRepository
                .GetCurrentUserByIdAsync(
                    userId,
                    cancellationToken);

        if (currentUser is null)
        {
            return CurrentUserResult.Failure(
                CurrentUserErrorCode.UserNotFound);
        }

        ValidateReturnedData(
            currentUser,
            expectedUserId: userId);

        if (!currentUser.IsActive)
        {
            return CurrentUserResult.Failure(
                CurrentUserErrorCode.AccountInactive);
        }

        if (!currentUser.IsEmailConfirmed)
        {
            return CurrentUserResult.Failure(
                CurrentUserErrorCode.EmailNotConfirmed);
        }

        if (!currentUser.IsRoleActive)
        {
            return CurrentUserResult.Failure(
                CurrentUserErrorCode.RoleInactive);
        }

        if (currentUser.RequiresPasswordChange)
        {
            return CurrentUserResult.Failure(
                CurrentUserErrorCode
                    .PasswordChangeRequired);
        }

        if (currentUser.EmployeeId.HasValue)
        {
            if (currentUser.IsEmployeeActive != true)
            {
                return CurrentUserResult.Failure(
                    CurrentUserErrorCode
                        .EmployeeInactive);
            }

            if (currentUser.IsDepartmentActive != true)
            {
                return CurrentUserResult.Failure(
                    CurrentUserErrorCode
                        .DepartmentInactive);
            }

            ValidateEmployeeData(currentUser);
        }

        CurrentUserInfo user =
            new(
                UserId: currentUser.UserId,
                EmailAddress:
                    currentUser.EmailAddress,
                RoleCode:
                    currentUser.RoleCode,
                RoleDisplayName:
                    currentUser.RoleDisplayName,
                EmployeeId:
                    currentUser.EmployeeId,
                FirstName:
                    currentUser.FirstName,
                LastName:
                    currentUser.LastName,
                JobTitle:
                    currentUser.JobTitle,
                ProfileImagePath:
                    currentUser.ProfileImagePath,
                DepartmentId:
                    currentUser.DepartmentId,
                DepartmentCode:
                    currentUser.DepartmentCode,
                DepartmentName:
                    currentUser.DepartmentName);

        return CurrentUserResult.Success(user);
    }

    private static void ValidateReturnedData(
        CurrentUserData currentUser,
        int expectedUserId)
    {
        if (currentUser.UserId != expectedUserId)
        {
            throw new InvalidOperationException(
                "The current-user query returned an " +
                "unexpected UserId.");
        }

        if (string.IsNullOrWhiteSpace(
                currentUser.EmailAddress))
        {
            throw new InvalidOperationException(
                "The current-user query returned an " +
                "invalid email address.");
        }

        if (string.IsNullOrWhiteSpace(
                currentUser.RoleCode))
        {
            throw new InvalidOperationException(
                "The current-user query returned an " +
                "invalid role code.");
        }

        if (string.IsNullOrWhiteSpace(
                currentUser.RoleDisplayName))
        {
            throw new InvalidOperationException(
                "The current-user query returned an " +
                "invalid role display name.");
        }
    }

    private static void ValidateEmployeeData(
        CurrentUserData currentUser)
    {
        if (currentUser.EmployeeId is not > 0)
        {
            throw new InvalidOperationException(
                "The current-user query returned an " +
                "invalid EmployeeId.");
        }

        if (currentUser.DepartmentId is not > 0)
        {
            throw new InvalidOperationException(
                "An employee must have a valid department.");
        }

        if (string.IsNullOrWhiteSpace(
                currentUser.FirstName))
        {
            throw new InvalidOperationException(
                "The current-user query returned an " +
                "invalid employee first name.");
        }

        if (string.IsNullOrWhiteSpace(
                currentUser.LastName))
        {
            throw new InvalidOperationException(
                "The current-user query returned an " +
                "invalid employee last name.");
        }

        if (string.IsNullOrWhiteSpace(
                currentUser.JobTitle))
        {
            throw new InvalidOperationException(
                "The current-user query returned an " +
                "invalid employee job title.");
        }

        if (string.IsNullOrWhiteSpace(
                currentUser.DepartmentCode))
        {
            throw new InvalidOperationException(
                "The current-user query returned an " +
                "invalid department code.");
        }

        if (string.IsNullOrWhiteSpace(
                currentUser.DepartmentName))
        {
            throw new InvalidOperationException(
                "The current-user query returned an " +
                "invalid department name.");
        }
    }
}