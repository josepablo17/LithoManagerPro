using System.ComponentModel.DataAnnotations;

namespace LithoManager.Api.Contracts.Authentication;

public sealed class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string EmailAddress
    {
        get;
        init;
    } = string.Empty;
}