using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Api.Models;

public sealed class CreateOrderRequest
{
    [Required(ErrorMessage = "O cliente é obrigatório.")]
    [StringLength(200)]
    public string Cliente { get; init; } = string.Empty;

    [Required(ErrorMessage = "O produto é obrigatório.")]
    [StringLength(200)]
    public string Produto { get; init; } = string.Empty;

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ParseLimitsInInvariantCulture = true, ErrorMessage = "O valor deve ser maior que zero.")]
    public decimal Valor { get; init; }
}
