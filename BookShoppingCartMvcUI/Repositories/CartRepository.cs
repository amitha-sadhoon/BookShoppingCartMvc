using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Core.Types;

namespace BookShoppingCartMvcUI.Repositories
{
    public class CartRepository: ICartRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public CartRepository(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor,
            UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<bool> AddItem(int bookId, int qty)
        {
            using var transation=_db.Database.BeginTransaction();
            try
            {
                string userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                    return false;
                var cart = await GetCart(userId);
                if (cart is null)
                {
                    cart = new ShoppingCart
                    {
                        UserId = userId
                    };
                    _db.ShoppingCarts.Add(cart);
                }
                _db.SaveChanges();
                //cart detail section
                var CartItem=_db.CartDetails.FirstOrDefault(a => a.ShoppingCartId == cart.Id && a.BookId==bookId);
                if(CartItem is not null)
                {
                    CartItem.Quantity += qty;
                }
                else
                {
                    CartItem = new CartDetail
                    {
                        BookId = bookId,
                        ShoppingCartId = cart.Id,
                        Quantity = qty
                    };
                    _db.CartDetails.Add(CartItem);
                }
                _db.SaveChanges();
                transation.Commit();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public async Task<bool> RemoveItem(int bookId)
        {
            //using var transation = _db.Database.BeginTransaction();
            try
            {
                string userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                    return false;
                var cart = await GetCart(userId);
                if (cart is null)
                {
                    return false;
                }
                _db.SaveChanges();
                //cart detail section
                var CartItem = _db.CartDetails.FirstOrDefault(a => a.ShoppingCartId == cart.Id && a.BookId == bookId);
                if(CartItem is null)
                    return false;
               else if(CartItem.Quantity==1)
                {
                    _db.CartDetails.Remove(CartItem);
                }
                else
                {
                    CartItem.Quantity = CartItem.Quantity - 1;
                }
                _db.SaveChanges();
                //transation.Commit();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<IEnumerable<ShoppingCart>> GetUserCart()
        {
            var userId = GetUserId();
            if (userId == null)
                throw new Exception("Invalid userid");
            var shoppingCart=await _db.ShoppingCarts
                                      .Include(a => a.CartDetails)
                                      .ThenInclude(a => a.Book)
                                       .ThenInclude(a => a.Genre)
                                       .Where(a => a.UserId == userId).ToListAsync();
            return shoppingCart;

        }
        private async Task<ShoppingCart> GetCart(string userId)
        {
            var cart = await _db.ShoppingCarts.FirstOrDefaultAsync(x => x.UserId == userId);
            return cart;
        }

        private string GetUserId()
        {
            var principal = _httpContextAccessor.HttpContext.User;
            string userId = _userManager.GetUserId(principal);
            return userId;
        }
    }  

}
