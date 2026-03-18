using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ApiProject.Models.DTOs;

public class RejectAppointmentDto
{
    [MaxLength(500)]
    [JsonPropertyName("rejectionReason")]
    public string? RejectionReason { get; set; }
}

