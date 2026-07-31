using System.ComponentModel.DataAnnotations;

namespace LithoManager.Api.Contracts.Authentication;

public sealed class ChangeTemporaryPasswordRequest
{
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