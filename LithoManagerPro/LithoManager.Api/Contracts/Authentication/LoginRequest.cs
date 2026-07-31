using System.ComponentModel.DataAnnotations;

namespace LithoManager.Api.Contracts.Authentication;

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string EmailAddress { get; init; } = string.Empty;

    [Required]
    [StringLength(1024, MinimumLength = 1)]
    public string Password { get; init; } = string.Empty;
}