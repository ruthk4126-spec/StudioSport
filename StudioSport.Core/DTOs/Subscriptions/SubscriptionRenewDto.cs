using System.ComponentModel.DataAnnotations;

namespace StudioSport.Core.DTOs.Subscriptions;

public class SubscriptionRenewDto
{
    [Required]
    public int UserId { get; set; }
}