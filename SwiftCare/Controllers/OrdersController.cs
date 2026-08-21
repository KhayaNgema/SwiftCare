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
        private readonly QrCodeService _qrCodeService;
        private readonly ReceiveEquipmentOrder _receiveEquipmentOrder;
        private readonly BarcodeService _barcodeService;

        public OrdersController(
            SwiftCareDbContext context,
            UserManager<UserBaseModel> userManager,
            OrderNumberGenerator orderNumberGenerator,
             IEncryptionService encryptionService,
             QrCodeService qrCodeService,
             ReceiveEquipmentOrder receiveEquipmentOrder,
             BarcodeService barcodeService)
        {
            _context = context;
            _userManager = userManager;
            _orderNumberGenerator = orderNumberGenerator;
            _encryptionService = encryptionService;
            _qrCodeService = qrCodeService;
            _receiveEquipmentOrder = receiveEquipmentOrder;
            _barcodeService = barcodeService;
        }


        // ============================================================
        // OrdersController.cs
        // DELIVERIES
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Deliveries()
        {
            try
            {
                // ========================================================
                // GET ALL ACTIVE DELIVERY ORDERS
                // ========================================================

                var orders = await _context.Orders
                    .Where(order =>
                        order.Status == OrderStatus.OnTheWay)
                    .Include(order => order.User)
                    .Include(order => order.OrderItems)
                        .ThenInclude(orderItem =>
                            orderItem.Equipment)
                    .OrderByDescending(order =>
                        order.LastUpdated)
                    .ToListAsync();


                // ========================================================
                // NO ORDERS
                // ========================================================

                if (!orders.Any())
                {
                    return View(orders);
                }


                // ========================================================
                // GET ORDER IDS
                // ========================================================

                var orderIds = orders
                    .Select(order => order.OrderId)
                    .ToList();


                // ========================================================
                // GET PACKAGE RECORDS DIRECTLY FROM DATABASE
                //
                // THIS DOES NOT DEPEND ONLY ON Include().
                // ========================================================

                var packageOrders = await _context.PackageOrders
                    .Where(packageOrder =>
                        orderIds.Contains(
                            packageOrder.OrderId))
                    .Include(packageOrder =>
                        packageOrder.OrderItem)
                        .ThenInclude(orderItem =>
                            orderItem.Equipment)
                    .OrderBy(packageOrder =>
                        packageOrder.PackageNumber)
                    .ToListAsync();


                // ========================================================
                // GROUP PACKAGES BY ORDER
                // ========================================================

                var packagesByOrder =
                    packageOrders
                        .GroupBy(packageOrder =>
                            packageOrder.OrderId)
                        .ToDictionary(
                            group => group.Key,
                            group => group.ToList()
                        );


                // ========================================================
                // ATTACH PACKAGE RECORDS TO EACH ORDER
                //
                // NOW item.PackageOrders WILL CONTAIN
                // THE ACTUAL PackageOrders FROM THE DATABASE.
                // ========================================================

                foreach (var order in orders)
                {
                    if (packagesByOrder.TryGetValue(
                        order.OrderId,
                        out var orderPackages))
                    {
                        order.PackageOrders =
                            orderPackages;
                    }
                    else
                    {
                        order.PackageOrders =
                            new List<PackageOrder>();
                    }
                }


                return View(orders);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Error loading deliveries: {ex.Message}");

                return View(
                    new List<Order>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPackageQrCode(
    int packageOrderId)
        {
            var packageOrder =
                await _context.PackageOrders
                    .FirstOrDefaultAsync(
                        p =>
                            p.PackageOrderId ==
                            packageOrderId
                    );

            if (packageOrder == null)
            {
                return NotFound();
            }

            var qrCodeBytes =
                _qrCodeService.GenerateQrCode(
                    packageOrder.QrCodeValue
                );

            return File(
                qrCodeBytes,
                "image/png"
            );
        }

        [HttpGet]
        public async Task<IActionResult> EquipmentOrders()
        {
            var orders = await _context.Orders
                .Where(o => o.Status == OrderStatus.Pending)
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Equipment)
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> PackagedOrders()
        {
            var orders = await _context.Orders
                .Where(o => o.Status == OrderStatus.Packaged)
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeliverOrder(
    string orderId)
        {
            try
            {
                var userId = GetCurrentUserId();

                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new
                    {
                        success = false,
                        message = "You are not authorized."
                    });
                }


                // ============================================================
                // VALIDATE ORDER ID
                // ============================================================

                if (string.IsNullOrWhiteSpace(orderId))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Order ID is missing."
                    });
                }


                // ============================================================
                // DECRYPT ORDER ID
                // ============================================================

                int decryptedOrderId;

                try
                {
                    decryptedOrderId =
                        _encryptionService.DecryptToInt(orderId);
                }
                catch
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid order ID."
                    });
                }


                // ============================================================
                // GET ORDER
                // ============================================================

                var order = await _context.Orders
                    .FirstOrDefaultAsync(
                        o => o.OrderId == decryptedOrderId
                    );


                if (order == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "The order could not be found."
                    });
                }


                // ============================================================
                // CHECK STATUS
                // ============================================================

                if (order.Status != OrderStatus.Packaged)
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "This order is no longer waiting for delivery."
                    });
                }


                // ============================================================
                // UPDATE ORDER STATUS
                // ============================================================

                order.Status = OrderStatus.OnTheWay;

                order.LastUpdated = DateTime.Now;


                // ============================================================
                // SAVE CHANGES
                // ============================================================

                await _context.SaveChangesAsync();


                // ============================================================
                // NOTIFY THE PERSON WHO CREATED / OWNS THE ORDER
                //
                // The encrypted orderId is passed to the service so that
                // it can generate the Order Details link and send SMS + Email.
                // ============================================================

                try
                {
                    await _receiveEquipmentOrder
                        .NotifyAdministratorOrderOnTheWayAsync(
                            orderId
                        );
                }
                catch (Exception notificationEx)
                {
                    // Delivery was successfully started even if a notification
                    // provider fails, so we log the error instead of failing
                    // the whole delivery action.

                    Console.WriteLine(
                        $"Order {order.OrderNumber} was marked as On The Way, " +
                        $"but the notification failed: " +
                        $"{notificationEx.Message}"
                    );
                }


                // ============================================================
                // SUCCESS
                // ============================================================

                return Json(new
                {
                    success = true,

                    message =
                        $"Order {order.OrderNumber} is now on the way for delivery. " +
                        $"The order owner has been notified by SMS and email.",

                    orderId = order.OrderId,

                    newStatus = "OnTheWay"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,

                    message =
                        "An error occurred while starting delivery.",

                    error =
                        ex.InnerException?.Message ??
                        ex.Message
                });
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
            if (string.IsNullOrWhiteSpace(orderId))
            {
                return BadRequest("Order ID was not provided.");
            }

            int decryptedOrderId;

            try
            {
                decryptedOrderId =
                    _encryptionService.DecryptToInt(orderId);
            }
            catch (Exception ex)
            {
                return BadRequest(
                    $"Unable to decrypt Order ID. Error: {ex.Message}"
                );
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Equipment)
                .FirstOrDefaultAsync(o =>
                    o.OrderId == decryptedOrderId);

            if (order == null)
            {
                return NotFound(
                    $"Order with ID {decryptedOrderId} was not found."
                );
            }

            return View(order);
        }


[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> PackageOrderItem(
    string orderId,
    int orderItemId)
        {
            try
            {
                // ============================================================
                // GET CURRENT USER
                // ============================================================

                var userId = GetCurrentUserId();

                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new
                    {
                        success = false,
                        message = "You are not authorized."
                    });
                }


                // ============================================================
                // VALIDATE INPUT
                // ============================================================

                if (string.IsNullOrWhiteSpace(orderId))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Order ID is missing."
                    });
                }

                if (orderItemId <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Equipment item ID is missing."
                    });
                }


                // ============================================================
                // DECRYPT ORDER ID
                // ============================================================

                int decryptedOrderId;

                try
                {
                    decryptedOrderId =
                        _encryptionService.DecryptToInt(orderId);
                }
                catch
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid order ID."
                    });
                }


                // ============================================================
                // GET ORDER
                // ============================================================

                var order = await _context.Orders
                    .FirstOrDefaultAsync(o =>
                        o.OrderId == decryptedOrderId);

                if (order == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "The order could not be found."
                    });
                }


                // ============================================================
                // GET ORDER ITEM
                // ============================================================

                var orderItem = await _context.OrderItems
                    .Include(oi => oi.Equipment)
                    .FirstOrDefaultAsync(oi =>
                        oi.OrderItemId == orderItemId &&
                        oi.OrderId == order.OrderId);

                if (orderItem == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "The equipment item could not be found for this order."
                    });
                }


                // ============================================================
                // CHECK IF ALREADY PACKAGED
                // ============================================================

                if (orderItem.IsPackaged)
                {
                    return Json(new
                    {
                        success = false,
                        message = "This equipment item has already been packaged."
                    });
                }


                // ============================================================
                // CREATE PACKAGE NUMBER
                // ============================================================

                var packageNumber =
                    $"PKG-{DateTime.Now:yyyyMMddHHmmss}-{orderItem.OrderItemId}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";


                // ============================================================
                // CREATE QR CODE VALUE
                // ============================================================

                var qrCodeValue =
                    $"SWIFTCARE|PACKAGE:{packageNumber}|ORDER:{order.OrderId}|ORDERITEM:{orderItem.OrderItemId}|TOKEN:{Guid.NewGuid()}";


                // ============================================================
                // GENERATE QR CODE IMAGE
                // ============================================================

                byte[] qrCodeBytes =
                    _qrCodeService.GenerateQrCode(qrCodeValue);


                // ============================================================
                // CREATE PACKAGE
                // ============================================================

                var packageOrder = new PackageOrder
                {
                    OrderId = order.OrderId,

                    OrderItemId = orderItem.OrderItemId,

                    PackageNumber = packageNumber,

                    QrCodeValue = qrCodeValue,

                    IsReceived = false,

                    CreatedDate = DateTime.Now
                };


                // ============================================================
                // UPDATE ORDER ITEM
                // ============================================================

                orderItem.IsPackaged = true;


                // ============================================================
                // UPDATE ORDER
                // ============================================================

                order.LastUpdated = DateTime.Now;


                // ============================================================
                // CHECK WHETHER ENTIRE ORDER IS NOW PACKAGED
                // ============================================================

                var remainingItems =
                    await _context.OrderItems
                        .Where(oi =>
                            oi.OrderId == order.OrderId &&
                            oi.OrderItemId != orderItem.OrderItemId &&
                            !oi.IsPackaged)
                        .AnyAsync();

                if (!remainingItems)
                {
                    order.Status = OrderStatus.Packaged;
                }


                // ============================================================
                // SAVE PACKAGE
                // ============================================================

                _context.PackageOrders.Add(packageOrder);

                await _context.SaveChangesAsync();


                // ============================================================
                // RETURN SUCCESS
                // ============================================================

                return Json(new
                {
                    success = true,

                    message =
                        $"{orderItem.Equipment?.EquipmentName ?? "Equipment item"} packaged successfully.",

                    orderItemId = orderItem.OrderItemId,

                    packageOrderId = packageOrder.PackageOrderId,

                    packageNumber = packageNumber,

                    qrCodeValue = qrCodeValue,

                    qrCodeBase64 =
                        Convert.ToBase64String(qrCodeBytes)
                });
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message;

                if (ex.InnerException != null)
                {
                    errorMessage +=
                        " | INNER ERROR: " +
                        ex.InnerException.Message;
                }

                return Json(new
                {
                    success = false,

                    message =
                        "An error occurred while packaging the equipment item.",

                    error = errorMessage
                });
            }
        }


[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> PackageEntireOrder(
    string orderId)
        {
            try
            {
                // ============================================================
                // GET CURRENT USER
                // ============================================================

                var userId = GetCurrentUserId();

                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new
                    {
                        success = false,
                        message = "You are not authorized."
                    });
                }


                // ============================================================
                // VALIDATE ORDER ID
                // ============================================================

                if (string.IsNullOrWhiteSpace(orderId))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Order ID is missing."
                    });
                }


                // ============================================================
                // DECRYPT ORDER ID
                // ============================================================

                int decryptedOrderId;

                try
                {
                    decryptedOrderId =
                        _encryptionService.DecryptToInt(orderId);
                }
                catch
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid order ID."
                    });
                }


                // ============================================================
                // GET ORDER WITH ITEMS
                // ============================================================

                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Equipment)
                    .FirstOrDefaultAsync(o =>
                        o.OrderId == decryptedOrderId);

                if (order == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "The order could not be found."
                    });
                }


                // ============================================================
                // GET UNPACKAGED ITEMS
                // ============================================================

                var unpackagedItems =
                    order.OrderItems
                        .Where(oi => !oi.IsPackaged)
                        .ToList();


                // ============================================================
                // CHECK IF EVERYTHING IS ALREADY PACKAGED
                // ============================================================

                if (!unpackagedItems.Any())
                {
                    return Json(new
                    {
                        success = true,
                        alreadyPackaged = true,
                        message = "The entire order has already been packaged."
                    });
                }


                // ============================================================
                // CREATE PACKAGE FOR EACH ORDER ITEM
                // ============================================================

                var createdPackages =
                    new List<object>();


                foreach (var orderItem in unpackagedItems)
                {
                    // --------------------------------------------------------
                    // CREATE UNIQUE PACKAGE NUMBER
                    // --------------------------------------------------------

                    var packageNumber =
                        $"PKG-{DateTime.Now:yyyyMMddHHmmss}-{orderItem.OrderItemId}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";


                    // --------------------------------------------------------
                    // CREATE QR VALUE
                    // --------------------------------------------------------

                    var qrCodeValue =
                        $"SWIFTCARE|PACKAGE:{packageNumber}|ORDER:{order.OrderId}|ORDERITEM:{orderItem.OrderItemId}|TOKEN:{Guid.NewGuid()}";


                    // --------------------------------------------------------
                    // GENERATE QR CODE
                    // --------------------------------------------------------

                    byte[] qrCodeBytes =
                        _qrCodeService.GenerateQrCode(qrCodeValue);


                    // --------------------------------------------------------
                    // CREATE PACKAGE RECORD
                    // --------------------------------------------------------

                    var packageOrder =
                        new PackageOrder
                        {
                            OrderId = order.OrderId,

                            OrderItemId = orderItem.OrderItemId,

                            PackageNumber = packageNumber,

                            QrCodeValue = qrCodeValue,

                            IsReceived = false,

                            CreatedDate = DateTime.Now
                        };


                    // --------------------------------------------------------
                    // ADD PACKAGE
                    // --------------------------------------------------------

                    _context.PackageOrders.Add(
                        packageOrder
                    );


                    // --------------------------------------------------------
                    // MARK ITEM AS PACKAGED
                    // --------------------------------------------------------

                    orderItem.IsPackaged = true;


                    // --------------------------------------------------------
                    // ADD TO RESPONSE LIST
                    // --------------------------------------------------------

                    createdPackages.Add(
                        new
                        {
                            orderItemId =
                                orderItem.OrderItemId,

                            equipmentName =
                                orderItem.Equipment?.EquipmentName
                                ?? "Equipment",

                            packageNumber =
                                packageNumber,

                            qrCodeValue =
                                qrCodeValue,

                            qrCodeBase64 =
                                Convert.ToBase64String(
                                    qrCodeBytes
                                )
                        }
                    );
                }


                // ============================================================
                // UPDATE ORDER STATUS
                // ============================================================

                order.Status =
                    OrderStatus.Packaged;

                order.LastUpdated =
                    DateTime.Now;


                // ============================================================
                // SAVE EVERYTHING ONCE
                // ============================================================

                await _context.SaveChangesAsync();


                // ============================================================
                // RETURN SUCCESS
                // ============================================================

                return Json(new
                {
                    success = true,

                    message =
                        $"{unpackagedItems.Count} equipment item(s) packaged successfully.",

                    orderId =
                        order.OrderId,

                    packages =
                        createdPackages
                });
            }
            catch (Exception ex)
            {
                var errorMessage =
                    ex.Message;

                if (ex.InnerException != null)
                {
                    errorMessage +=
                        " | INNER ERROR: " +
                        ex.InnerException.Message;
                }

                return Json(new
                {
                    success = false,

                    message =
                        "An error occurred while packaging the order.",

                    error =
                        errorMessage
                });
            }
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScanPackage(
     string qrCodeValue,
     int orderId)
        {
            if (orderId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "A valid order was not provided."
                });
            }

            if (string.IsNullOrWhiteSpace(qrCodeValue))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "No QR code value was provided."
                });
            }

            qrCodeValue = qrCodeValue.Trim();

            var package = await _context.PackageOrders
                .Where(p => p.Order.Status != OrderStatus.Collected)
                .Include(p => p.OrderItem)
                    .ThenInclude(oi => oi.Equipment)
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p =>
                    p.QrCodeValue != null &&
                    p.QrCodeValue.Trim() == qrCodeValue &&
                    p.OrderId == orderId);

            if (package == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "This QR code does not belong to a package in this order."
                });
            }

            if (package.OrderItem == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "The package was found, but no order item is linked to it."
                });
            }

            var equipmentName =
                package.OrderItem.Equipment?.EquipmentName
                ?? "Equipment";

            return Json(new
            {
                success = true,

                packageOrderId = package.PackageOrderId,

                packageId = package.PackageOrderId,

                packageNumber =
                    !string.IsNullOrWhiteSpace(package.PackageNumber?.ToString())
                        ? package.PackageNumber.ToString()
                        : $"Package #{package.PackageOrderId}",

                orderId = package.OrderId,

                orderItemId = package.OrderItemId,

                equipmentName = equipmentName,

                expectedQuantity = package.OrderItem.Quantity,

                qrCodeValue = package.QrCodeValue
            });
        }

        [HttpGet]
        public async Task<IActionResult> ReceiveOrder(string orderId)
        {
            // ============================================================
            // VALIDATE PARAMETER
            // ============================================================

            if (string.IsNullOrWhiteSpace(orderId))
            {
                return BadRequest("Missing orderId parameter");
            }

            try
            {
                // ========================================================
                // CLEAN THE INCOMING ORDER ID
                // ========================================================

                string encryptedOrderId = orderId.Trim();

                /*
                 * ASP.NET normally URL-decodes query-string values for us.
                 *
                 * However, depending on how the encrypted ID was generated
                 * and passed to this page, it may still contain encoded
                 * characters such as:
                 *
                 * %2B = +
                 * %3D = =
                 * %2F = /
                 *
                 * Decode repeatedly in case the value was encoded more
                 * than once.
                 */

                for (int i = 0; i < 3; i++)
                {
                    string decoded = WebUtility.UrlDecode(encryptedOrderId);

                    if (decoded == encryptedOrderId)
                    {
                        break;
                    }

                    encryptedOrderId = decoded;
                }

                // ========================================================
                // FIX '+' CHARACTER
                // ========================================================

                /*
                 * Base64 encrypted strings can contain '+'.
                 *
                 * If the encrypted value was placed into a query string
                 * without proper URL encoding, '+' can arrive as a space.
                 *
                 * Since an encrypted Base64 value should never contain
                 * actual spaces, safely convert them back to '+'.
                 */

                encryptedOrderId = encryptedOrderId
                    .Replace(" ", "+")
                    .Trim();

                // ========================================================
                // VALIDATE AFTER CLEANING
                // ========================================================

                if (string.IsNullOrWhiteSpace(encryptedOrderId))
                {
                    return BadRequest("Invalid orderId");
                }

                // ========================================================
                // DECRYPT ORDER ID
                // ========================================================

                int decryptedOrderId;

                try
                {
                    decryptedOrderId =
                        _encryptionService.DecryptToInt(
                            encryptedOrderId);
                }
                catch (Exception)
                {
                    return BadRequest("Invalid orderId");
                }

                // ========================================================
                // VALIDATE DECRYPTED ID
                // ========================================================

                if (decryptedOrderId <= 0)
                {
                    return BadRequest("Invalid orderId");
                }

                // ========================================================
                // LOAD ORDER
                // ========================================================

                var order = await _context.Orders

                    // ----------------------------------------------------
                    // Load equipment ordered
                    // ----------------------------------------------------

                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Equipment)

                    // ----------------------------------------------------
                    // Load packages created for this order
                    // ----------------------------------------------------

                    .Include(o => o.PackageOrders)
                        .ThenInclude(po => po.OrderItem)
                            .ThenInclude(oi => oi.Equipment)

                    // ----------------------------------------------------
                    // Find order
                    // ----------------------------------------------------

                    .FirstOrDefaultAsync(
                        o => o.OrderId == decryptedOrderId
                    );

                // ========================================================
                // ORDER NOT FOUND
                // ========================================================

                if (order == null)
                {
                    return NotFound(
                        $"Order with ID {decryptedOrderId} could not be found.");
                }

                if (order.Status == OrderStatus.Collected)
                {
                    return NotFound(
                        $"Order with ID {decryptedOrderId} was already collected.");
                }

                // ========================================================
                // RETURN VIEW
                // ========================================================

                return View(order);
            }
            catch (Exception ex)
            {
                // ========================================================
                // UNEXPECTED ERROR
                // ========================================================

                return BadRequest(
                    $"Unable to process orderId: {ex.Message}");
            }
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

        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteOrder(int orderId)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // ========================================================
                // FIND ORDER
                // ========================================================

                var order =
                    await _context.Orders
                        .Include(o => o.OrderItems)
                            .ThenInclude(oi => oi.Equipment)
                        .FirstOrDefaultAsync(
                            o => o.OrderId == orderId
                        );

                if (order == null)
                {
                    await transaction.RollbackAsync();

                    return Json(new
                    {
                        success = false,
                        message = "Order could not be found."
                    });
                }


                // ========================================================
                // PREVENT DUPLICATE COMPLETION
                // ========================================================

                if (order.Status == OrderStatus.Collected)
                {
                    await transaction.RollbackAsync();

                    return Json(new
                    {
                        success = false,
                        message = "This order has already been completed."
                    });
                }


                // ========================================================
                // CHECK ORDER ITEMS
                // ========================================================

                if (order.OrderItems == null ||
                    !order.OrderItems.Any())
                {
                    await transaction.RollbackAsync();

                    return Json(new
                    {
                        success = false,
                        message =
                            "This order does not contain any equipment items."
                    });
                }


                // ========================================================
                // CREATE EQUIPMENT UNITS
                // ========================================================

                foreach (var orderItem in order.OrderItems)
                {
                    // ====================================================
                    // CHECK EQUIPMENT
                    // ====================================================

                    if (orderItem.Equipment == null)
                    {
                        await transaction.RollbackAsync();

                        return Json(new
                        {
                            success = false,
                            message =
                                $"Equipment for order item {orderItem.OrderItemId} could not be found."
                        });
                    }


                    var equipment =
                        orderItem.Equipment;


                    // ====================================================
                    // GET QUANTITY
                    // ====================================================

                    int quantity =
                        orderItem.Quantity;

                    if (quantity <= 0)
                    {
                        quantity = 1;
                    }


                    // ====================================================
                    // CREATE EACH PHYSICAL EQUIPMENT UNIT
                    // ====================================================

                    for (int i = 0; i < quantity; i++)
                    {
                        // =================================================
                        // GENERATE UNIQUE BARCODE VALUE
                        // =================================================

                        var barcodeValue =
                            GenerateBarcode();


                        // =================================================
                        // CREATE UNIQUE BARCODE FILE NAME
                        // =================================================

                        var fileName =
                            $"{equipment.EquipmentName}_{barcodeValue}.png";


                        // =================================================
                        // GENERATE + SAVE BARCODE IMAGE
                        // =================================================

                        var barcodeImage =
                            await _barcodeService
                                .GenerateAndSaveBarcodeAsync(
                                    barcodeValue,
                                    fileName
                                );


                        // =================================================
                        // CREATE OUR EQUIPMENT
                        // =================================================

                        var ourEquipment =
                            new OurEquipment
                            {
                                EquipmentName =
                                    equipment.EquipmentName,

                                ImageUrl =
                                    equipment.ImageUrl,

                                Description =
                                    equipment.Description,

                                BarcodeValue =
                                    barcodeValue,

                                BarcodeImage =
                                    barcodeImage,

                                Status =
                                    EquipmentStatus.Available
                            };


                        // =================================================
                        // ADD EQUIPMENT TO DATABASE
                        // =================================================

                        await _context.OurEquipment
                            .AddAsync(ourEquipment);
                    }
                }


                // ========================================================
                // MARK ORDER AS COLLECTED
                // ========================================================

                order.Status =
                    OrderStatus.Collected;

                order.LastUpdated =
                    DateTime.Now;


                // ========================================================
                // SAVE EVERYTHING
                // ========================================================

                await _context.SaveChangesAsync();


                // ========================================================
                // COMMIT TRANSACTION
                // ========================================================

                await transaction.CommitAsync();


                // ========================================================
                // SUCCESS
                // ========================================================

                return Json(new
                {
                    success = true,

                    message =
                        "Equipment order has been completed and the equipment has been added to your equipment inventory."
                });
            }
            catch (Exception ex)
            {
                // ========================================================
                // ROLLBACK
                // ========================================================

                try
                {
                    await transaction.RollbackAsync();
                }
                catch
                {
                    // Ignore rollback failure
                }


                // ========================================================
                // RETURN ERROR
                // ========================================================

                return Json(new
                {
                    success = false,

                    message =
                        "Unable to complete the order and create the equipment.",

                    error =
                        ex.InnerException?.Message
                        ?? ex.Message
                });
            }
        }


        // ============================================================
        // GENERATE BARCODE VALUE
        // ============================================================

        private string GenerateBarcode()
        {
            return Random.Shared.Next(
                1000000000,
                2000000000
            ).ToString();
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