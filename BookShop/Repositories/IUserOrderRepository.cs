﻿namespace BookShop.Repositories
{
    public interface IUserOrderRepository
    {
        Task<IEnumerable<Order>> UserOrders(bool getAll = false);
        Task ChangeOrderStatus(UpdateOrderStatusModel model);
        Task TogglePaymentStatus(int orderId);
        Task <Order?> GetOrderById(int id);
        Task <IEnumerable<OrderStatus>> GetOrderStatuses();


    }
}
