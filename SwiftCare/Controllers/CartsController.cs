
using SwiftCare.Data;
using SwiftCare.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace SwiftCare.Controllers
{
    public class CartController : Controller
    {
        private readonly SwiftCareDbContext _context;

        public CartController(SwiftCareDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // CART INDEX
        // ============================================================

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var cart = await GetUserCart(userId);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.Now
                };

                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var cartItems = await _context.CartItems
                .Include(c => c.Equipment)
                .Where(c => c.CartId == cart.CartId && !c.Deleted)
                .OrderBy(c => c.CartItemId)
                .ToListAsync();

            return View(cartItems);
        }


        // ============================================================
        // ADD EQUIPMENT TO CART
        // ============================================================

        [HttpPost]
        public async Task<IActionResult> AddToCart(
            int equipmentId,
            int quantity)
        {
            try
            {
                var userId = GetCurrentUserId();

                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new
                    {
                        success = false,
                        message = "You must be logged in to add equipment to your cart."
                    });
                }

                if (quantity < 1)
                {
                    quantity = 1;
                }

                var equipment = await _context.Equipment
                    .FirstOrDefaultAsync(e =>
                        e.EquipmentId == equipmentId);

                if (equipment == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Equipment could not be found."
                    });
                }


                // ----------------------------------------------------
                // GET OR CREATE USER CART
                // ----------------------------------------------------

                var cart = await GetUserCart(userId);

                if (cart == null)
                {
                    cart = new Cart
                    {
                        UserId = userId,
                        CreatedAt = DateTime.Now
                    };

                    _context.Carts.Add(cart);

                    await _context.SaveChangesAsync();
                }


                // ----------------------------------------------------
                // CHECK EXISTING CART ITEM
                // ----------------------------------------------------

                var existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(c =>
                        c.CartId == cart.CartId &&
                        c.EquipmentId == equipmentId &&
                        !c.Deleted);


                // ----------------------------------------------------
                // UPDATE EXISTING
                // ----------------------------------------------------

                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;

                    _context.CartItems.Update(existingItem);
                }


                // ----------------------------------------------------
                // CREATE NEW ITEM
                // ----------------------------------------------------

                else
                {
                    var newCartItem = new CartItem
                    {
                        CartId = cart.CartId,
                        EquipmentId = equipmentId,
                        Quantity = quantity,
                        Deleted = false
                    };

                    _context.CartItems.Add(newCartItem);
                }


                await _context.SaveChangesAsync();


                // ----------------------------------------------------
                // GET UPDATED COUNT
                // ----------------------------------------------------

                var count = await _context.CartItems
                    .CountAsync(c =>
                        c.CartId == cart.CartId &&
                        !c.Deleted);


                return Json(new
                {
                    success = true,
                    count = count,
                    message = $"{equipment.EquipmentName} added to your cart."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Unable to add equipment to cart.",
                    error = ex.Message
                });
            }
        }


        // ============================================================
        // GET CART ITEMS
        // RETURNS PARTIAL VIEW
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> GetCartItems()
        {
            try
            {
                var userId = GetCurrentUserId();

                if (string.IsNullOrEmpty(userId))
                {
                    return PartialView(
                        "~/Views/Cart/_CartItems.cshtml",
                        new List<CartItem>());
                }


                // ----------------------------------------------------
                // GET USER CART
                // ----------------------------------------------------

                var cart = await GetUserCart(userId);

                if (cart == null)
                {
                    return PartialView(
                        "~/Views/Cart/_CartItems.cshtml",
                        new List<CartItem>());
                }


                // ----------------------------------------------------
                // GET CART ITEMS
                // ----------------------------------------------------

                var cartItems = await _context.CartItems
                    .Include(c => c.Equipment)
                    .Where(c =>
                        c.CartId == cart.CartId &&
                        !c.Deleted)
                    .OrderBy(c => c.CartItemId)
                    .ToListAsync();


                // ----------------------------------------------------
                // RETURN PARTIAL
                // ----------------------------------------------------

                return PartialView(
                    "~/Views/Cart/_CartItems.cshtml",
                    cartItems);
            }
            catch (Exception ex)
            {
                // Log the actual error in the server console/output.
                Console.WriteLine(
                    "GET CART ERROR: " + ex);

                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message = "Unable to load cart.",
                        error = ex.Message
                    });
            }
        }


        // ============================================================
        // GET CART ITEM COUNT
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> GetCartItemCount()
        {
            try
            {
                var userId = GetCurrentUserId();

                if (string.IsNullOrEmpty(userId))
                {
                    return Json(0);
                }

                var cart = await GetUserCart(userId);

                if (cart == null)
                {
                    return Json(0);
                }

                var count = await _context.CartItems
                    .CountAsync(c =>
                        c.CartId == cart.CartId &&
                        !c.Deleted);

                return Json(count);
            }
            catch
            {
                return Json(0);
            }
        }


        // ============================================================
        // UPDATE QUANTITY
        // ============================================================

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(
            int id,
            int quantity)
        {
            try
            {
                var userId = GetCurrentUserId();

                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new
                    {
                        success = false,
                        message = "You must be logged in."
                    });
                }

                if (quantity < 1)
                {
                    quantity = 1;
                }


                var cart = await GetUserCart(userId);

                if (cart == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Cart could not be found."
                    });
                }


                var cartItem = await _context.CartItems
                    .FirstOrDefaultAsync(c =>
                        c.CartItemId == id &&
                        c.CartId == cart.CartId &&
                        !c.Deleted);


                if (cartItem == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Cart item could not be found."
                    });
                }


                cartItem.Quantity = quantity;

                await _context.SaveChangesAsync();


                return Json(new
                {
                    success = true
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Unable to update quantity.",
                    error = ex.Message
                });
            }
        }


        // ============================================================
        // DELETE CART ITEM
        // ============================================================

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = GetCurrentUserId();

                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new
                    {
                        success = false,
                        message = "You must be logged in."
                    });
                }


                var cart = await GetUserCart(userId);

                if (cart == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Cart could not be found."
                    });
                }


                var cartItem = await _context.CartItems
                    .FirstOrDefaultAsync(c =>
                        c.CartItemId == id &&
                        c.CartId == cart.CartId &&
                        !c.Deleted);


                if (cartItem == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Cart item could not be found."
                    });
                }


                _context.CartItems.Remove(cartItem);

                await _context.SaveChangesAsync();


                var count = await _context.CartItems
                    .CountAsync(c =>
                        c.CartId == cart.CartId &&
                        !c.Deleted);


                return Json(new
                {
                    success = true,
                    count = count
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Unable to remove equipment.",
                    error = ex.Message
                });
            }
        }


        // ============================================================
        // CLEAR CART
        // ============================================================

        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var userId = GetCurrentUserId();

                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new
                    {
                        success = false,
                        message = "You must be logged in."
                    });
                }


                var cart = await GetUserCart(userId);

                if (cart == null)
                {
                    return Json(new
                    {
                        success = true,
                        count = 0
                    });
                }


                var cartItems = await _context.CartItems
                    .Where(c =>
                        c.CartId == cart.CartId &&
                        !c.Deleted)
                    .ToListAsync();


                if (cartItems.Any())
                {
                    _context.CartItems.RemoveRange(cartItems);

                    await _context.SaveChangesAsync();
                }


                return Json(new
                {
                    success = true,
                    count = 0
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Unable to clear cart.",
                    error = ex.Message
                });
            }
        }


        // ============================================================
        // GET USER CART
        // ============================================================

        private async Task<Cart?> GetUserCart(string userId)
        {
            return await _context.Carts
                .FirstOrDefaultAsync(c =>
                    c.UserId == userId);
        }


        // ============================================================
        // CURRENT USER ID
        // ============================================================

        private string? GetCurrentUserId()
        {
            return User.FindFirstValue(
                ClaimTypes.NameIdentifier);
        }
    }
}
