using System.ComponentModel.DataAnnotations;

namespace LithoManager.Api.Contracts.Authentication;

public sealed class ResetPasswordRequest
{
    [Required]
    [StringLength(512)]
    public string Token
    {
        get;
        init;
    } = string.Empty;

    [Required]
    [StringLength(
        128,
        MinimumLength = 12)]
    public string NewPassword
    {
        get;
        init;
    } = string.Empty;

    [Required]
    [StringLength(
        128,
        MinimumLength = 12)]
    [Compare(nameof(NewPassword))]
    public string ConfirmNewPassword
    {
        get;
        init;
    } = string.Empty;
}