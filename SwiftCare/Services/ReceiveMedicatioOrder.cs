using SwiftCare.Data;
using SwiftCare.Interfaces;
using SwiftCare.Models;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace SwiftCare.Services
{
    public class ReceiveEquipmentOrder
    {
        private readonly SwiftCareDbContext _context;
        private readonly UserManager<UserBaseModel> _userManager;
        private readonly IEncryptionService _encryptionService;
        private readonly QrCodeService _qrCodeService;
        private readonly IBackgroundJobClient _backgroundJobs;

        public ReceiveEquipmentOrder(
            SwiftCareDbContext context,
            UserManager<UserBaseModel> userManager,
            IEncryptionService encryptionService,
            QrCodeService qrCodeService,
            IBackgroundJobClient backgroundJobs)
        {
            _context = context;
            _userManager = userManager;
            _encryptionService = encryptionService;
            _qrCodeService = qrCodeService;
            _backgroundJobs = backgroundJobs;
        }


        // ============================================================
        // NOTIFY ADMINISTRATOR WHEN EQUIPMENT ORDER IS ON THE WAY
        // ============================================================

        public async Task NotifyAdministratorOrderOnTheWayAsync(
            string encryptedOrderId)
        {
            try
            {
                // ====================================================
                // DECRYPT ORDER ID
                // ====================================================

                int orderId;

                try
                {
                    orderId =
                        _encryptionService.DecryptToInt(
                            encryptedOrderId
                        );
                }
                catch
                {
                    return;
                }


                // ====================================================
                // GET ORDER
                // ====================================================

                var order = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Equipment)
                    .FirstOrDefaultAsync(
                        o => o.OrderId == orderId
                    );

                if (order == null)
                {
                    return;
                }


                // ====================================================
                // GET USER
                // ====================================================

                var user = order.User;

                if (user == null)
                {
                    return;
                }

                var firstName =
                    string.IsNullOrWhiteSpace(user.FirstName)
                        ? "Administrator"
                        : user.FirstName.Trim();

                var phone = user.PhoneNumber;
                var email = user.Email;


                // ====================================================
                // EQUIPMENT DETAILS
                // ====================================================

                var totalQuantity =
                    order.OrderItems?.Sum(
                        item => item.Quantity
                    ) ?? 0;

                var equipmentTypes =
                    order.OrderItems?.Count ?? 0;

                var equipmentItems =
                    order.OrderItems?
                        .Select(item =>
                        {
                            var equipmentName =
                                item.Equipment?.EquipmentName
                                ?? "Equipment";

                            return
                                $"{equipmentName} x{item.Quantity}";
                        })
                        .ToList()
                    ?? new List<string>();

                var equipmentSummary =
                    string.Join(
                        ", ",
                        equipmentItems.Take(3)
                    );

                if (equipmentItems.Count > 3)
                {
                    equipmentSummary +=
                        $" and {equipmentItems.Count - 3} more item(s)";
                }


                // ====================================================
                // APPLICATION URL
                // ====================================================

                string baseUrl =
                    "https://swiftcare20260821114126-dec5cfdefeesfuhj.spaincentral-01.azurewebsites.net";

                string encodedOrderId =
                    WebUtility.UrlEncode(
                        encryptedOrderId
                    );

                string orderDetailsLink =
                    $"{baseUrl}/Orders/ReceiveOrder" +
                    $"?orderId={encodedOrderId}";


                // ====================================================
                // SMS MESSAGE
                // ====================================================

                string smsMessage =
                    $"SwiftCare Equipment Delivery Update\n\n" +
                    $"Hi {firstName}, your equipment order " +
                    $"{order.OrderNumber} has been dispatched and is now on the way.\n\n" +
                    $"Total equipment: {totalQuantity} item(s).\n" +
                    $"Equipment types: {equipmentTypes}.\n\n" +
                    $"View your order: {orderDetailsLink}\n\n" +
                    $"SwiftCare";


                // ====================================================
                // EQUIPMENT SUMMARY
                // ====================================================

                string equipmentSummarySection =
                    string.Empty;

                if (!string.IsNullOrWhiteSpace(equipmentSummary))
                {
                    equipmentSummarySection = $@"

<div style=""margin:25px 0;
            padding:20px;
            background:#fafdf7;
            border-radius:10px;
            border:1px solid #e2ecd9;"">

    <strong style=""color:#1f5c3a;"">
        Equipment Included
    </strong>

    <p style=""margin:10px 0 0;
              color:#526158;
              line-height:1.6;"">

        {WebUtility.HtmlEncode(equipmentSummary)}

    </p>

</div>";
                }


                // ====================================================
                // EMAIL MESSAGE
                // ====================================================

                string emailMessage = $@"
<!DOCTYPE html>

<html>

<head>
    <meta charset=""UTF-8"">
</head>

<body style=""margin:0;
             padding:0;
             background:#f3f7f4;
             font-family:Arial, Helvetica, sans-serif;"">

    <div style=""max-width:700px;
                margin:40px auto;
                background:#ffffff;
                border-radius:16px;
                overflow:hidden;
                box-shadow:0 10px 35px rgba(20,70,45,0.12);"">

        <div style=""background:#123f2d;
                    padding:35px;
                    text-align:center;
                    color:#ffffff;"">

            <h1 style=""margin:0;
                       font-size:28px;"">

                SwiftCare

            </h1>

            <p style=""margin:8px 0 0;
                      color:#c6ff72;
                      font-size:14px;"">

                Equipment Delivery Update

            </p>

        </div>


        <div style=""padding:40px;
                    color:#25332b;"">

            <h2 style=""margin-top:0;
                       color:#1f5c3a;"">

                Your Equipment Is On The Way

            </h2>


            <p>

                Dear
                <strong>{WebUtility.HtmlEncode(firstName)}</strong>,

            </p>


            <p>

                Great news! Your SwiftCare equipment order has
                been dispatched and is currently on its way.

            </p>


            <div style=""margin:30px 0;
                        padding:25px;
                        background:#f4faed;
                        border-left:5px solid #9acd32;
                        border-radius:12px;"">

                <p style=""margin:0 0 10px;
                          color:#6c7a70;
                          font-size:13px;
                          text-transform:uppercase;
                          letter-spacing:1px;"">

                    Order Number

                </p>


                <h3 style=""margin:0 0 25px;
                           color:#123f2d;"">

                    {WebUtility.HtmlEncode(order.OrderNumber)}

                </h3>


                <p style=""margin:8px 0;"">

                    <strong>Total Equipment:</strong>

                    {totalQuantity} item(s)

                </p>


                <p style=""margin:8px 0;"">

                    <strong>Equipment Types:</strong>

                    {equipmentTypes}

                </p>


                <p style=""margin:8px 0;"">

                    <strong>Current Status:</strong>

                    <span style=""color:#27659c;
                                 font-weight:bold;"">

                        On The Way

                    </span>

                </p>

            </div>


            {equipmentSummarySection}


            <p>

                You can view the complete equipment order and
                monitor its delivery progress using the button below.

            </p>


            <div style=""text-align:center;
                        margin:35px 0;"">

                <a href=""{orderDetailsLink}""
                   style=""display:inline-block;
                          padding:14px 28px;
                          background:#1f5c3a;
                          color:#ffffff;
                          text-decoration:none;
                          border-radius:8px;
                          font-weight:bold;"">

                    View Equipment Order

                </a>

            </div>


            <div style=""padding:18px;
                        background:#f6f9f6;
                        border-radius:10px;
                        color:#617067;
                        font-size:14px;
                        line-height:1.6;"">

                Your order status will be updated once the
                equipment has been received and confirmed.

            </div>


            <p style=""margin-top:35px;"">

                Kind regards,

                <br />

                <strong style=""color:#1f5c3a;"">

                    The SwiftCare Team

                </strong>

            </p>

        </div>


        <div style=""padding:20px;
                    text-align:center;
                    background:#123f2d;
                    color:#dce9df;
                    font-size:12px;"">

            SwiftCare Equipment Management System

        </div>

    </div>

</body>

</html>";


                // ====================================================
                // QUEUE SMS WITH HANGFIRE
                // ====================================================

                if (!string.IsNullOrWhiteSpace(phone))
                {
                    _backgroundJobs.Enqueue<NotificationJobService>(
                        service =>
                            service.SendSmsAsync(
                                phone,
                                smsMessage
                            )
                    );
                }


                // ====================================================
                // QUEUE EMAIL WITH HANGFIRE
                // ====================================================

                if (!string.IsNullOrWhiteSpace(email))
                {
                    _backgroundJobs.Enqueue<NotificationJobService>(
                        service =>
                            service.SendEmailAsync(
                                email,
                                $"SwiftCare Equipment Delivery - {order.OrderNumber}",
                                emailMessage,
                                "SwiftCare"
                            )
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Error notifying administrator about " +
                    $"equipment delivery: {ex.Message}"
                );
            }
        }


        // ============================================================
        // NOTIFY ADMINISTRATOR THAT ORDER IS READY
        // ============================================================

        public async Task NotifyAdministratorOrderReadyAsync(
            string encryptedOrderId)
        {
            try
            {
                // ====================================================
                // DECRYPT ORDER ID
                // ====================================================

                int orderId;

                try
                {
                    orderId =
                        _encryptionService.DecryptToInt(
                            encryptedOrderId
                        );
                }
                catch
                {
                    return;
                }


                // ====================================================
                // GET ORDER
                // ====================================================

                var order = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Equipment)
                    .FirstOrDefaultAsync(
                        o => o.OrderId == orderId
                    );

                if (order == null || order.User == null)
                {
                    return;
                }

                var user = order.User;

                var firstName =
                    string.IsNullOrWhiteSpace(user.FirstName)
                        ? "Administrator"
                        : user.FirstName.Trim();

                var phone = user.PhoneNumber;
                var email = user.Email;

                var totalQuantity =
                    order.OrderItems?.Sum(
                        item => item.Quantity
                    ) ?? 0;


                // ====================================================
                // APPLICATION URL
                // ====================================================

                string baseUrl =
                    "https://swiftcare20260821114126-dec5cfdefeesfuhj.spaincentral-01.azurewebsites.net";

                string encodedOrderId =
                    WebUtility.UrlEncode(
                        encryptedOrderId
                    );

                string orderLink =
                    $"{baseUrl}/Orders/ReceiveOrder" +
                    $"?orderId={encodedOrderId}";


                // ====================================================
                // SMS MESSAGE
                // ====================================================

                string smsMessage =
                    $"SwiftCare Equipment Update\n\n" +
                    $"Hi {firstName}, your equipment order " +
                    $"{order.OrderNumber} has been packaged and is ready for delivery.\n\n" +
                    $"Total equipment: {totalQuantity} item(s).\n\n" +
                    $"View your order: {orderLink}\n\n" +
                    $"SwiftCare";


                // ====================================================
                // EMAIL MESSAGE
                // ====================================================

                string emailMessage = $@"
<!DOCTYPE html>

<html>

<head>
    <meta charset=""UTF-8"">
</head>

<body style=""margin:0;
             padding:0;
             background:#f3f7f4;
             font-family:Arial, Helvetica, sans-serif;"">

    <div style=""max-width:700px;
                margin:40px auto;
                background:#ffffff;
                border-radius:16px;
                overflow:hidden;
                box-shadow:0 10px 35px rgba(20,70,45,0.12);"">

        <div style=""background:#123f2d;
                    padding:35px;
                    text-align:center;
                    color:#ffffff;"">

            <h1 style=""margin:0;
                       font-size:28px;"">

                SwiftCare

            </h1>

            <p style=""margin:8px 0 0;
                      color:#c6ff72;
                      font-size:14px;"">

                Equipment Order Update

            </p>

        </div>


        <div style=""padding:40px;
                    color:#25332b;"">

            <h2 style=""margin-top:0;
                       color:#1f5c3a;"">

                Your Equipment Is Ready

            </h2>


            <p>

                Dear
                <strong>{WebUtility.HtmlEncode(firstName)}</strong>,

            </p>


            <p>

                Your SwiftCare equipment order has been
                successfully packaged and is ready
                to be dispatched for delivery.

            </p>


            <div style=""margin:30px 0;
                        padding:25px;
                        background:#f4faed;
                        border-left:5px solid #9acd32;
                        border-radius:12px;"">

                <p style=""margin:0 0 10px;
                          color:#6c7a70;
                          font-size:13px;
                          text-transform:uppercase;"">

                    Order Number

                </p>


                <h3 style=""margin:0 0 20px;
                           color:#123f2d;"">

                    {WebUtility.HtmlEncode(order.OrderNumber)}

                </h3>


                <p style=""margin:8px 0;"">

                    <strong>Total Equipment:</strong>

                    {totalQuantity} item(s)

                </p>


                <p style=""margin:8px 0;"">

                    <strong>Current Status:</strong>

                    <span style=""color:#1f5c3a;
                                 font-weight:bold;"">

                        Packaged

                    </span>

                </p>

            </div>


            <div style=""text-align:center;
                        margin:35px 0;"">

                <a href=""{orderLink}""
                   style=""display:inline-block;
                          padding:14px 28px;
                          background:#1f5c3a;
                          color:#ffffff;
                          text-decoration:none;
                          border-radius:8px;
                          font-weight:bold;"">

                    Receive Equipment Order

                </a>

            </div>


            <p style=""margin-top:35px;"">

                Kind regards,

                <br />

                <strong style=""color:#1f5c3a;"">

                    The SwiftCare Team

                </strong>

            </p>

        </div>


        <div style=""padding:20px;
                    text-align:center;
                    background:#123f2d;
                    color:#dce9df;
                    font-size:12px;"">

            SwiftCare Equipment Management System

        </div>

    </div>

</body>

</html>";


                // ====================================================
                // QUEUE SMS WITH HANGFIRE
                // ====================================================

                if (!string.IsNullOrWhiteSpace(phone))
                {
                    _backgroundJobs.Enqueue<NotificationJobService>(
                        service =>
                            service.SendSmsAsync(
                                phone,
                                smsMessage
                            )
                    );
                }


                // ====================================================
                // QUEUE EMAIL WITH HANGFIRE
                // ====================================================

                if (!string.IsNullOrWhiteSpace(email))
                {
                    _backgroundJobs.Enqueue<NotificationJobService>(
                        service =>
                            service.SendEmailAsync(
                                email,
                                $"SwiftCare Equipment Order {order.OrderNumber} - Packaged",
                                emailMessage,
                                "SwiftCare"
                            )
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Error notifying administrator: {ex.Message}"
                );
            }
        }
    }
}