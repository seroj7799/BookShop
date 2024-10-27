using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookShop.Models
{
    [Table("Order")]

    public class Order
    {
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; }

        public DateTime CreateDate { get; set; } = DateTime.UtcNow;

        [Required] 
        public int OrderStatusId { get; set; }

        [Required]
        public string? Name { get; set; }

        [Required,EmailAddress]
        public string? Email { get; set; }

        public string? MobileNumber { get; set; }

        public string? Address { get; set; }

        public string PaymentMethod { get; set; }

        public bool IsPaid { get; set; }
        public bool IsDeleted { get; set; } = false;
        public OrderStatus OrderStatus { get; set; }

        public List<OrderDetail> OrderDetail { get; set; }
    }
}
