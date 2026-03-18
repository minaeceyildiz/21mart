using System.ComponentModel.DataAnnotations;

namespace ApiProject.Models.DTOs;

public class CreateOrderItemDto
{
    [Required(ErrorMessage = "Menü öğesi ID gereklidir")]
    public int MenuItemId { get; set; }

    [Required(ErrorMessage = "Adet gereklidir")]
    [Range(1, int.MaxValue, ErrorMessage = "Adet en az 1 olmalıdır")]
    public int Quantity { get; set; }
}

public class CreateOrderDto
{
    [Required(ErrorMessage = "Sipariş öğeleri gereklidir")]
    [MinLength(1, ErrorMessage = "En az bir ürün seçmelisiniz")]
    public List<CreateOrderItemDto> OrderItems { get; set; } = new();
}

