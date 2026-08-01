using LithoManager.Application.Features.Authentication
    .GetCurrentUser;
using LithoManager.UnitTests.TestDoubles.Persistence;
using Xunit;

namespace LithoManager.UnitTests.Features.Authentication
    .GetCurrentUser;

public sealed class GetCurrentUserServiceTests
{
    [Fact]
    public async Task GetAsync_WhenUserIdIsInvalid_ReturnsInvalidRequest()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new();

        GetCurrentUserService service =
            new(repository);

        // Act
        CurrentUserResult result =
            await service.GetAsync(
                userId: 0,
                CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal(
            CurrentUserErrorCode.InvalidRequest,
            result.ErrorCode);
        Assert.Null(result.User);

        Assert.Equal(
            0,
            repository.GetCurrentUserByIdCallCount);
        Assert.Null(repository.RequestedUserId);
    }

    [Fact]
    public async Task GetAsync_WhenUserDoesNotExist_ReturnsUserNotFound()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new()
            {
                CurrentUserToReturn = null
            };

        GetCurrentUserService service =
            new(repository);

        // Act
        CurrentUserResult result =
            await service.GetAsync(
                userId: 1,
                CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal(
            CurrentUserErrorCode.UserNotFound,
            result.ErrorCode);
        Assert.Null(result.User);

        Assert.Equal(
            1,
            repository.GetCurrentUserByIdCallCount);
        Assert.Equal(
            1,
            repository.RequestedUserId);
    }

    [Fact]
    public async Task GetAsync_WhenAccountIsInactive_ReturnsAccountInactive()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new()
            {
                CurrentUserToReturn =
                    CreateValidSuperAdministrator(
                        isActive: false)
            };

        GetCurrentUserService service =
            new(repository);

        // Act
        CurrentUserResult result =
            await service.GetAsync(
                userId: 1,
                CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal(
            CurrentUserErrorCode.AccountInactive,
            result.ErrorCode);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task GetAsync_WhenEmailIsNotConfirmed_ReturnsEmailNotConfirmed()
    {
        // Arrange
        CurrentUserData currentUser =
            CreateValidSuperAdministrator();

        currentUser =
            CopyWith(
                currentUser,
                isEmailConfirmed: false);

        FakeAuthenticationRepository repository =
            new()
            {
                CurrentUserToReturn = currentUser
            };

        GetCurrentUserService service =
            new(repository);

        // Act
        CurrentUserResult result =
            await service.GetAsync(
                userId: 1,
                CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal(
            CurrentUserErrorCode.EmailNotConfirmed,
            result.ErrorCode);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task GetAsync_WhenRoleIsInactive_ReturnsRoleInactive()
    {
        // Arrange
        CurrentUserData currentUser =
            CreateValidSuperAdministrator();

        currentUser =
            CopyWith(
                currentUser,
                isRoleActive: false);

        FakeAuthenticationRepository repository =
            new()
            {
                CurrentUserToReturn = currentUser
            };

        GetCurrentUserService service =
            new(repository);

        // Act
        CurrentUserResult result =
            await service.GetAsync(
                userId: 1,
                CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal(
            CurrentUserErrorCode.RoleInactive,
            result.ErrorCode);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task GetAsync_WhenPasswordChangeIsRequired_ReturnsPasswordChangeRequired()
    {
        // Arrange
        CurrentUserData currentUser =
            CreateValidSuperAdministrator();

        currentUser =
            CopyWith(
                currentUser,
                requiresPasswordChange: true);

        FakeAuthenticationRepository repository =
            new()
            {
                CurrentUserToReturn = currentUser
            };

        GetCurrentUserService service =
            new(repository);

        // Act
        CurrentUserResult result =
            await service.GetAsync(
                userId: 1,
                CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal(
            CurrentUserErrorCode.PasswordChangeRequired,
            result.ErrorCode);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task GetAsync_WhenEmployeeIsInactive_ReturnsEmployeeInactive()
    {
        // Arrange
        CurrentUserData currentUser =
            CreateValidEmployee();

        currentUser =
            CopyWith(
                currentUser,
                isEmployeeActive: false);

        FakeAuthenticationRepository repository =
            new()
            {
                CurrentUserToReturn = currentUser
            };

        GetCurrentUserService service =
            new(repository);

        // Act
        CurrentUserResult result =
            await service.GetAsync(
                userId: 2,
                CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal(
            CurrentUserErrorCode.EmployeeInactive,
            result.ErrorCode);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task GetAsync_WhenDepartmentIsInactive_ReturnsDepartmentInactive()
    {
        // Arrange
        CurrentUserData currentUser =
            CreateValidEmployee();

        currentUser =
            CopyWith(
                currentUser,
                isDepartmentActive: false);

        FakeAuthenticationRepository repository =
            new()
            {
                CurrentUserToReturn = currentUser
            };

        GetCurrentUserService service =
            new(repository);

        // Act
        CurrentUserResult result =
            await service.GetAsync(
                userId: 2,
                CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal(
            CurrentUserErrorCode.DepartmentInactive,
            result.ErrorCode);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task GetAsync_WhenSuperAdministratorIsValid_ReturnsUserWithoutEmployee()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new()
            {
                CurrentUserToReturn =
                    CreateValidSuperAdministrator()
            };

        GetCurrentUserService service =
            new(repository);

        // Act
        CurrentUserResult result =
            await service.GetAsync(
                userId: 1,
                CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal(
            CurrentUserErrorCode.None,
            result.ErrorCode);

        CurrentUserInfo user =
            Assert.IsType<CurrentUserInfo>(
                result.User);

        Assert.Equal(1, user.UserId);
        Assert.Equal(
            "admin@lithomanager.com",
            user.EmailAddress);
        Assert.Equal(
            "SuperAdministrator",
            user.RoleCode);
        Assert.Equal(
            "Super Administrator",
            user.RoleDisplayName);

        Assert.Null(user.EmployeeId);
        Assert.Null(user.FirstName);
        Assert.Null(user.DepartmentId);

        Assert.Equal(
            1,
            repository.GetCurrentUserByIdCallCount);
        Assert.Equal(
            1,
            repository.RequestedUserId);
    }

    [Fact]
    public async Task GetAsync_WhenEmployeeIsValid_ReturnsEmployeeInformation()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new()
            {
                CurrentUserToReturn =
                    CreateValidEmployee()
            };

        GetCurrentUserService service =
            new(repository);

        // Act
        CurrentUserResult result =
            await service.GetAsync(
                userId: 2,
                CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal(
            CurrentUserErrorCode.None,
            result.ErrorCode);

        CurrentUserInfo user =
            Assert.IsType<CurrentUserInfo>(
                result.User);

        Assert.Equal(2, user.UserId);
        Assert.Equal(
            "employee@lithomanager.com",
            user.EmailAddress);
        Assert.Equal(
            "Employee",
            user.RoleCode);

        Assert.Equal(10, user.EmployeeId);
        Assert.Equal("Ana", user.FirstName);
        Assert.Equal("Rodriguez", user.LastName);
        Assert.Equal(
            "Business Analyst",
            user.JobTitle);

        Assert.Equal(3, user.DepartmentId);
        Assert.Equal(
            "INFORMATION_TECHNOLOGY",
            user.DepartmentCode);
        Assert.Equal(
            "Information Technology",
            user.DepartmentName);
    }

    private static CurrentUserData
        CreateValidSuperAdministrator(
            bool isActive = true)
    {
        return new CurrentUserData
        {
            UserId = 1,
            EmailAddress =
                "admin@lithomanager.com",
            IsEmailConfirmed = true,
            IsActive = isActive,
            RequiresPasswordChange = false,

            RoleCode =
                "SuperAdministrator",
            RoleDisplayName =
                "Super Administrator",
            IsRoleActive = true,

            EmployeeId = null,
            FirstName = null,
            LastName = null,
            JobTitle = null,
            ProfileImagePath = null,
            IsEmployeeActive = null,

            DepartmentId = null,
            DepartmentCode = null,
            DepartmentName = null,
            IsDepartmentActive = null
        };
    }

    private static CurrentUserData
        CreateValidEmployee()
    {
        return new CurrentUserData
        {
            UserId = 2,
            EmailAddress =
                "employee@lithomanager.com",
            IsEmailConfirmed = true,
            IsActive = true,
            RequiresPasswordChange = false,

            RoleCode = "Employee",
            RoleDisplayName = "Employee",
            IsRoleActive = true,

            EmployeeId = 10,
            FirstName = "Ana",
            LastName = "Rodriguez",
            JobTitle = "Business Analyst",
            ProfileImagePath =
                "/profiles/employee-10.jpg",
            IsEmployeeActive = true,

            DepartmentId = 3,
            DepartmentCode =
                "INFORMATION_TECHNOLOGY",
            DepartmentName =
                "Information Technology",
            IsDepartmentActive = true
        };
    }

    private static CurrentUserData CopyWith(
        CurrentUserData source,
        bool? isEmailConfirmed = null,
        bool? isRoleActive = null,
        bool? requiresPasswordChange = null,
        bool? isEmployeeActive = null,
        bool? isDepartmentActive = null)
    {
        return new CurrentUserData
        {
            UserId = source.UserId,
            EmailAddress = source.EmailAddress,

            IsEmailConfirmed =
                isEmailConfirmed
                ?? source.IsEmailConfirmed,

            IsActive = source.IsActive,

            RequiresPasswordChange =
                requiresPasswordChange
                ?? source.RequiresPasswordChange,

            RoleCode = source.RoleCode,
            RoleDisplayName =
                source.RoleDisplayName,

            IsRoleActive =
                isRoleActive
                ?? source.IsRoleActive,

            EmployeeId = source.EmployeeId,
            FirstName = source.FirstName,
            LastName = source.LastName,
            JobTitle = source.JobTitle,
            ProfileImagePath =
                source.ProfileImagePath,

            IsEmployeeActive =
                isEmployeeActive
                ?? source.IsEmployeeActive,

            DepartmentId =
                source.DepartmentId,
            DepartmentCode =
                source.DepartmentCode,
            DepartmentName =
                source.DepartmentName,

            IsDepartmentActive =
                isDepartmentActive
                ?? source.IsDepartmentActive
        };
    }
}