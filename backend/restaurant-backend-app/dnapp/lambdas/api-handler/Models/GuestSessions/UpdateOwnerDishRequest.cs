using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace APIHandler.Models.GuestSessions;

public class UpdateOwnerDishRequest
{
    [Required]
    [JsonPropertyName("dishId")]
    public string DishId { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
}
