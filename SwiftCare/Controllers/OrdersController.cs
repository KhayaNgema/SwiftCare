using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using SwiftCare.Data;
using SwiftCare.Interfaces;
using SwiftCare.Models;
using SwiftCare.Services;
using System.Net;
using System.Security.Claims;

namespace SwiftCare.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly SwiftCareDbContext _context;
        private readonly UserManager<UserBaseModel> _userManager;
        private readonly OrderNumberGenerator _orderNumberGenerator;
        private readonly IEncryptionService _encryptionService;

        public OrdersController(
            SwiftCareDbContext context,
            UserManager<UserBaseModel> userManager,
            OrderNumberGenerator orderNumberGenerator,
             IEncryptionService encryptionService)
        {
            _context = context;
            _userManager = userManager;
            _orderNumberGenerator = orderNumberGenerator;
            _encryptionService = encryptionService;
        }


        // ============================================================
        // GET EQUIPMENT ORDERS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> EquipmentOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Equipment)
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // ============================================================
        // ORDER PLACED SUCCESSFULLY
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> OrderPlacedSuccessfully(int orderId)
        {
            try
            {
                // ----------------------------------------------------
                // GET CURRENT USER
                // ----------------------------------------------------

                var userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized();
                }


                // ----------------------------------------------------
                // GET ORDER
                // ----------------------------------------------------

                var order = await _context.Orders
                    .AsNoTracking()
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Equipment)
                    .FirstOrDefaultAsync(o =>
                        o.OrderId == orderId &&
                        o.UserId == userId);


                // ----------------------------------------------------
                // ORDER NOT FOUND
                // ----------------------------------------------------

                if (order == null)
                {
                    return NotFound();
                }


                // ----------------------------------------------------
                // GENERATE QR CODE INFORMATION
                // ----------------------------------------------------

                var qrText =
                    $"SwiftCare Equipment Order\n" +
                    $"Order Number: {order.OrderNumber}\n" +
                    $"Order ID: {order.OrderId}\n" +
                    $"Status: {order.Status}\n" +
                    $"Order Date: {order.OrderDate:dd MMM yyyy HH:mm}";


                // ----------------------------------------------------
                // CREATE QR CODE
                // ----------------------------------------------------

                using var qrGenerator = new QRCodeGenerator();

                using QRCodeData qrCodeData =
                    qrGenerator.CreateQrCode(
                        qrText,
                        QRCodeGenerator.ECCLevel.Q);


                // ----------------------------------------------------
                // GENERATE PNG
                // ----------------------------------------------------

                var qrCode =
                    new PngByteQRCode(qrCodeData);

                byte[] qrCodeBytes =
                    qrCode.GetGraphic(20);


                // ----------------------------------------------------
                // CONVERT TO BASE64
                // ----------------------------------------------------

                var qrCodeBase64 =
                    Convert.ToBase64String(qrCodeBytes);


                // ----------------------------------------------------
                // SEND QR CODE TO VIEW
                // ----------------------------------------------------

                ViewBag.QrCode =
                    $"data:image/png;base64,{qrCodeBase64}";


                // ----------------------------------------------------
                // ORDER INFORMATION
                // ----------------------------------------------------

                ViewBag.OrderNumber =
                    order.OrderNumber;

                ViewBag.OrderStatus =
                    order.Status.ToString();

                ViewBag.OrderId =
                    order.OrderId;

                ViewBag.OrderDate =
                    order.OrderDate;


                // ----------------------------------------------------
                // RETURN VIEW
                // ----------------------------------------------------

                return View(order);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "Error loading successful equipment order: "
                    + ex);

                return View("Error");
            }
        }

        // ============================================================
        // MY ORDERS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Equipment)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }


        // ============================================================
        // ORDER DETAILS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> OrderDetails(string orderId)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var decryptedOrderId = _encryptionService.DecryptToInt(orderId);

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Equipment)
                .FirstOrDefaultAsync(o =>
                    o.OrderId == decryptedOrderId &&
                    o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }


[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> PlaceOrder()
        {
            try
            {
                // --------------------------------------------------------
                // GET CURRENT USER
                // --------------------------------------------------------

                var userId = GetCurrentUserId();

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Json(new
                    {
                        success = false,
                        message = "You must be logged in to place an order."
                    });
                }


                // --------------------------------------------------------
                // FIND USER
                // --------------------------------------------------------

                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "The current user could not be found."
                    });
                }


                // --------------------------------------------------------
                // FIND CART
                // --------------------------------------------------------

                var cart = await _context.Carts
                    .Include(c => c.Items)
                        .ThenInclude(ci => ci.Equipment)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Your equipment cart could not be found."
                    });
                }


                // --------------------------------------------------------
                // GET VALID CART ITEMS
                // --------------------------------------------------------

                var cartItems = cart.Items
                    .Where(ci =>
                        ci.Quantity > 0 &&
                        ci.Equipment != null)
                    .ToList();

                if (!cartItems.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = "Your equipment cart is empty."
                    });
                }


                // --------------------------------------------------------
                // VERIFY EQUIPMENT STILL EXISTS
                // --------------------------------------------------------

                var equipmentIds = cartItems
                    .Select(x => x.EquipmentId)
                    .Distinct()
                    .ToList();

                var existingEquipmentIds = await _context.Equipment
                    .Where(e => equipmentIds.Contains(e.EquipmentId))
                    .Select(e => e.EquipmentId)
                    .ToListAsync();

                var invalidItems = cartItems
                    .Where(x => !existingEquipmentIds.Contains(x.EquipmentId))
                    .ToList();

                if (invalidItems.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = "One or more equipment items in your cart are no longer available. Please refresh your cart."
                    });
                }


                // --------------------------------------------------------
                // GENERATE ORDER NUMBER
                // --------------------------------------------------------

                var orderNumber =
                    _orderNumberGenerator.GenerateOrderNumber();


                // --------------------------------------------------------
                // CREATE ORDER
                // --------------------------------------------------------

                var order = new Order
                {
                    OrderNumber = orderNumber,
                    OrderDate = DateTime.Now,
                    LastUpdated = DateTime.Now,
                    UserId = userId,
                    Status = OrderStatus.Pending,

                    OrderItems = new List<OrderItem>()
                };


                // --------------------------------------------------------
                // CREATE ORDER ITEMS
                // --------------------------------------------------------

                foreach (var cartItem in cartItems)
                {
                    order.OrderItems.Add(new OrderItem
                    {
                        EquipmentId = cartItem.EquipmentId,
                        Quantity = cartItem.Quantity
                    });
                }


                // --------------------------------------------------------
                // DATABASE TRANSACTION
                // --------------------------------------------------------

                await using var transaction =
                    await _context.Database.BeginTransactionAsync();

                try
                {
                    // Add order
                    _context.Orders.Add(order);

                    // Remove cart items
                    _context.CartItems.RemoveRange(cartItems);

                    // Save
                    await _context.SaveChangesAsync();

                    // Commit
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }


                // --------------------------------------------------------
                // SUCCESS
                // --------------------------------------------------------

                return Json(new
                {
                    success = true,
                    message = "Your equipment order has been successfully placed.",
                    orderId = order.OrderId,
                    orderNumber = order.OrderNumber,
                    redirectUrl = Url.Action(
                        nameof(OrderPlacedSuccessfully),
                        "Orders",
                        new
                        {
                            orderId = order.OrderId
                        })
                });
            }
            catch (DbUpdateException ex)
            {
                var databaseError =
                    ex.InnerException?.Message ??
                    ex.Message;

                Console.WriteLine("========================================");
                Console.WriteLine("EQUIPMENT ORDER DATABASE ERROR");
                Console.WriteLine("========================================");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("========================================");

                return StatusCode(500, new
                {
                    success = false,
                    message = "The server could not save the equipment order.",
                    error = databaseError
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("========================================");
                Console.WriteLine("EQUIPMENT ORDER ERROR");
                Console.WriteLine("========================================");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("========================================");

                return StatusCode(500, new
                {
                    success = false,
                    message = "The server encountered an error while placing the order.",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ReceiveOrder(string orderId)
        {
            if (string.IsNullOrEmpty(orderId))
            {
                return BadRequest("Missing orderId parameter");
            }

            string decodedOrderId = WebUtility.UrlDecode(orderId);

            int decryptedOrderId;

            try
            {
                decryptedOrderId = _encryptionService.DecryptToInt(decodedOrderId);
            }
            catch
            {
                return BadRequest("Invalid orderId");
            }

            var order = await _context.Orders
                .Where(o => o.OrderId == decryptedOrderId)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }


        [HttpPost]
        public async Task<IActionResult> ReceiveOrder(int orderId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                var order = await _context.Orders
                    .Where(o => o.OrderId == orderId)
                    .FirstOrDefaultAsync();

                order.Status = OrderStatus.Collected;
                order.LastUpdated = DateTime.Now;

                _context.Update(order);
                await _context.SaveChangesAsync();

                TempData["Message"] = $"You have successfully received your order. Thanks for verifying qantities.";

                return RedirectToAction(nameof(Orders));
            }
            catch
            {

            }

            return View();

        }

        // ============================================================
        // PENDING ORDERS
        // ============================================================

        [Authorize(Roles = "Administrator")]
        [HttpGet]
        public async Task<IActionResult> PendingOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Equipment)
                .Where(o =>
                    o.Status == OrderStatus.Pending)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }


        // ============================================================
        // ALL ORDERS
        // ============================================================

        [Authorize(Roles = "Administrator")]
        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Equipment)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }


        // ============================================================
        // MARK ORDER AS READY
        // ============================================================

        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsReady(int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o =>
                        o.OrderId == orderId);

                if (order == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Order could not be found."
                    });
                }

                order.Status = OrderStatus.OnTheWay;

                order.LastUpdated = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Equipment order is now ready."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Unable to update the order.",
                    error = ex.Message
                });
            }
        }


        // ============================================================
        // MARK ORDER AS ON THE WAY
        // ============================================================

        [Authorize(Roles = "Administrator, Supplier Driver")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsOnTheWay(int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o =>
                        o.OrderId == orderId);

                if (order == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Order could not be found."
                    });
                }

                order.Status = OrderStatus.OnTheWay;

                order.LastUpdated = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Equipment order is now on the way."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Unable to update the order.",
                    error = ex.Message
                });
            }
        }


        // ============================================================
        // COMPLETE ORDER
        // ============================================================

        [Authorize(Roles = "Administrator, Supplier Driver")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteOrder(int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o =>
                        o.OrderId == orderId);

                if (order == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Order could not be found."
                    });
                }

                order.Status = OrderStatus.Collected;

                order.LastUpdated = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message =
                        "Equipment order has been completed."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message =
                        "Unable to complete the order.",
                    error = ex.Message
                });
            }
        }


        // ============================================================
        // GET ORDER STATUS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> GetOrderStatus(int orderId)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var order = await _context.Orders
                .Where(o =>
                    o.OrderId == orderId &&
                    o.UserId == userId)
                .Select(o => new
                {
                    o.OrderId,
                    o.OrderNumber,
                    Status = o.Status.ToString(),
                    o.OrderDate,
                    o.LastUpdated
                })
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return NotFound();
            }

            return Json(new
            {
                success = true,
                order
            });
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