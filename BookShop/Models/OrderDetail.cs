﻿using System.ComponentModel.DataAnnotations.Schema;

namespace BookShop.Models
{
    [Table("OrderDetail")]

    public class OrderDetail
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public int BookId { get; set; }

        public int Quantity { get; set; }

        public double UnitPrice { get; set; }

        public Order Order { get; set; }

        public Book Book { get; set; }
    }
}
