using System.ComponentModel.DataAnnotations;

namespace LithoManager.Api.Contracts.Authentication;

public sealed class ChangePasswordRequest
{
    [Required]
    [StringLength(1024)]
    public string CurrentPassword { get; init; } =
        string.Empty;

    [Required]
    [StringLength(
        128,
        MinimumLength = 12)]
    public string NewPassword { get; init; } =
        string.Empty;

    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmNewPassword { get; init; } =
        string.Empty;
}