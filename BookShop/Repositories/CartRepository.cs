﻿using Microsoft.AspNet.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BookShop.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly Microsoft.AspNetCore.Identity.UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public CartRepository(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor,
            Microsoft.AspNetCore.Identity.UserManager<IdentityUser> userManager)
        {

            _db = db;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;

        }

        public async Task<int> AddItem(int bookId, int qty )
        {
            string userId = GetUserId();
            try
            {
                using var transaction = _db.Database.BeginTransaction();
                if (string.IsNullOrEmpty(userId))
                    throw new Exception("user is not loged-in");
                var cart = await GetCart(userId);

                if (cart is null)
                {
                    cart = new ShoppingCart
                    {
                        UserId = userId,
                    };
                    _db.ShoppingCarts.Add(cart);
                }
                _db.SaveChanges();
                var cartItem = _db.CartDetails.FirstOrDefault(a => a.ShoppingCartId == cart.Id && a.BookId == bookId);

                if(cartItem is not null)
                {
                    cartItem.Quantity += qty;
                }
                else
                {
                    var book = _db.Books.Find(bookId);
                    cartItem = new CartDetail
                    {
                        BookId = bookId,
                        ShoppingCartId = cart.Id,
                        Quantity = qty,
                        UnitPrice = book.Price
                    };
                    _db.CartDetails.Add(cartItem);
                }
                _db.SaveChanges();
                transaction.Commit();
               
            }
            catch ( Exception ex )
            {
              
            }

            var cartItemCount = await GetCartItemCount(userId);
            return cartItemCount;

        }


        public async Task<int> RemoveItem(int bookId)
        {
            string userId = GetUserId();
            try
            {
                if (string.IsNullOrEmpty(userId))
                    throw new Exception("user id not loged-in");
                var cart = await GetCart(userId);

                if (cart is null)
                    throw new Exception("invalid cart");
                _db.SaveChanges();
                var cartItem = _db.CartDetails.FirstOrDefault(a => a.ShoppingCartId == cart.Id && a.BookId == bookId);

                if (cartItem is null)
                    throw new Exception("not item in cart");
                else if (cartItem.Quantity == 1)
                {
                    _db.CartDetails.Remove(cartItem);
                }
                else
                {
                    cartItem.Quantity--;
                }
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
              
            }
            var cartItemCount = await GetCartItemCount(userId);
            return cartItemCount;
        }

        public async Task<ShoppingCart> GetUserCart()
        {
            var userId = GetUserId();
            if (userId == null)
                throw new Exception("Invalid user");

            //var shoppingCart = await _db.ShoppingCarts
            //                        .Include(a => a.CartDetails)
            //                        .ThenInclude(a => a.Book)
            //                        .ThenInclude(a => a.Genre)
            //                        .Where(a => a.UserId == userId).FirstOrDefaultAsync();

            var shoppingCart = await _db.ShoppingCarts
                                  .Include(a => a.CartDetails)
                                  .ThenInclude(a => a.Book)
                                  .ThenInclude(a => a.Stock)
                                  .Include(a => a.CartDetails)
                                  .ThenInclude(a => a.Book)
                                  .ThenInclude(a => a.Genre)
                                  .Where(a => a.UserId == userId).FirstOrDefaultAsync();


            return shoppingCart;
        }

        public async Task<ShoppingCart> GetCart(string userId)
        {
            var result = await _db.ShoppingCarts.FirstOrDefaultAsync(x => x.UserId == userId);
            return result;
        }

        public async Task<int> GetCartItemCount(string userId = "")
        {

            if (string.IsNullOrEmpty(userId))
            {
                userId = GetUserId();
            }
           // Logger.WriteLog(userId);
            var data = await (from cart in _db.ShoppingCarts
                              join cartDetail in _db.CartDetails
                              on cart.Id equals cartDetail.ShoppingCartId
                              join shopingCart in _db.ShoppingCarts
                              on cartDetail.ShoppingCartId equals shopingCart.Id
                              where shopingCart.UserId == userId
                              select new {cartDetail.Id}
                              ).ToListAsync();

            return data.Count;

        }

        private string GetUserId()
        {
            //Logger.WriteLog("get user id ----");
            var principal = _httpContextAccessor.HttpContext.User;
            //string jsonString = JsonSerializer.Serialize(principal);
            //Logger.WriteLog(jsonString);
            string userId = _userManager.GetUserId(principal);
            return userId;
        }

        public async Task<bool> DoCheckout(CheckoutModel model)
        {
            using var transaction = _db.Database.BeginTransaction();
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                    throw new UnauthorizedAccessException("User is not logged-in");
                var cart = await GetCart(userId);
                if (cart is null)
                    throw new InvalidOperationException("Invalid cart");
                var cartDetail = _db.CartDetails
                                    .Where(a => a.ShoppingCartId == cart.Id).ToList();
                if (cartDetail.Count == 0)
                    throw new InvalidOperationException("Cart is empty");
                var pendingRecord = _db.OrderStatuses.FirstOrDefault(s => s.StatusName == "Pending");
                if (pendingRecord is null)
                    throw new InvalidOperationException("Order status does not have Pending status");
                var order = new Order
                {
                    UserId = userId,
                    CreateDate = DateTime.UtcNow,
                    Name = model.Name,
                    Email = model.Email,
                    MobileNumber = model.MobileNumber,
                    PaymentMethod = model.PaymentMethod,
                    Address = model.Address,
                    IsPaid = false,
                    OrderStatusId = pendingRecord.Id
                };
                _db.Orders.Add(order);
                _db.SaveChanges();
                foreach (var item in cartDetail)
                {
                    var orderDetail = new OrderDetail
                    {
                        BookId = item.BookId,
                        OrderId = order.Id,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    };
                    _db.OrderDetails.Add(orderDetail);

                    var stock = await _db.Stocks.FirstOrDefaultAsync(a => a.BookId == item.BookId);
                    if (stock == null)
                    {
                        throw new InvalidOperationException("Stock is null");
                    }

                    if (item.Quantity > stock.Quantity)
                    {
                        throw new InvalidOperationException($"Only {stock.Quantity} items(s) are available in the stock");
                    }
                    stock.Quantity -= item.Quantity;
                }
                //_db.SaveChanges();

                _db.CartDetails.RemoveRange(cartDetail);
                _db.SaveChanges();
                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {

                return false;
            }
        }


    }
}
