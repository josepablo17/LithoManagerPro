using System.ComponentModel.DataAnnotations;

namespace LithoManager.Api.Contracts.LeaveManagement;

public sealed class CreateLeaveRequestRequest
{
    [Required]
    public DateTime? StartDate { get; init; }

    [Required]
    public DateTime? EndDate { get; init; }

    public string? LeaveTypeCode { get; init; }
}
