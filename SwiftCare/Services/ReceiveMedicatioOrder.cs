using SwiftCare.Data;
using SwiftCare.Interfaces;
using SwiftCare.Models;
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
        private readonly EmailService _emailService;
        private readonly QrCodeService _qrCodeService;
        public readonly SmsService _smsService;

        public ReceiveEquipmentOrder(
            SwiftCareDbContext context,
            UserManager<UserBaseModel> userManager,
            IEncryptionService encryptionService,
            QrCodeService qrCodeService,
            EmailService emailService,
            SmsService smsService)
        {
            _context = context;
            _userManager = userManager;
            _encryptionService = encryptionService;
            _emailService = emailService;
            _qrCodeService = qrCodeService;
            _smsService = smsService;
        }


        // ============================================================
        // NOTIFY ADMINISTRATOR THAT EQUIPMENT ORDER IS ON THE WAY
        // ============================================================

        public async Task NotifyAdministratorOrderOnTheWayAsync(
            string encryptedOrderId)
        {
            try
            {
                // ----------------------------------------------------
                // DECRYPT ORDER ID
                // ----------------------------------------------------

                int orderId;

                try
                {
                    orderId = int.Parse(
                        _encryptionService.Decrypt(
                            encryptedOrderId));
                }
                catch
                {
                    return;
                }


                // ----------------------------------------------------
                // GET EQUIPMENT ORDER
                // ----------------------------------------------------

                var order = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Equipment)
                    .FirstOrDefaultAsync(o =>
                        o.OrderId == orderId);


                if (order == null)
                {
                    return;
                }


                // ----------------------------------------------------
                // GET USER
                // ----------------------------------------------------

                var user = order.User;

                if (user == null)
                {
                    return;
                }


                // ----------------------------------------------------
                // CONTACT DETAILS
                // ----------------------------------------------------

                var phone = user.PhoneNumber;
                var email = user.Email;


                // ----------------------------------------------------
                // URL
                // ----------------------------------------------------

                string baseUrl =
                    "https://4.222.233.134:2005";


                string encodedOrderId =
                    WebUtility.UrlEncode(
                        encryptedOrderId);


                string receiveOrderLink =
                    $"{baseUrl}/Orders/OrderDetails?orderId={encodedOrderId}";


                // ----------------------------------------------------
                // SMS
                // ----------------------------------------------------

                string firstName =
                    string.IsNullOrWhiteSpace(user.FirstName)
                        ? "Administrator"
                        : user.FirstName;


                string smsMessage =
                    $"Dear {firstName}, your SwiftCare equipment order " +
                    $"{order.OrderNumber} is now On The Way. " +
                    $"Please view your order here: " +
                    $"{receiveOrderLink}";


                // ----------------------------------------------------
                // EMAIL
                // ----------------------------------------------------

                string emailMessage = $@"
                    <html>
                    <body>

                        <p>
                            Dear {WebUtility.HtmlEncode(firstName)},
                        </p>

                        <p>
                            Your SwiftCare equipment order
                            <strong>
                                {WebUtility.HtmlEncode(order.OrderNumber)}
                            </strong>
                            is now <strong>On The Way</strong>.
                        </p>

                        <p>
                            Your equipment order has been dispatched
                            and is currently being delivered.
                        </p>

                        <p>
                            <a href=""{receiveOrderLink}""
                               style=""
                               display:inline-block;
                               padding:12px 20px;
                               background:#48AAAD;
                               color:#ffffff;
                               text-decoration:none;
                               border-radius:6px;
                               "">
                                View Order
                            </a>
                        </p>

                        <p>
                            You can use the link above to view the
                            current status and equipment associated
                            with your order.
                        </p>

                        <p>
                            Kind regards,<br/>
                            <strong>SwiftCare</strong>
                        </p>

                    </body>
                    </html>
                ";


                // ====================================================
                // SEND SMS
                // ====================================================

                if (!string.IsNullOrWhiteSpace(phone))
                {
                    try
                    {
                        await _smsService.SendSmsAsync(
                            phone,
                            smsMessage);
                    }
                    catch (Exception smsEx)
                    {
                        Console.WriteLine(
                            $"Failed to send SMS to {phone}: " +
                            smsEx.Message);
                    }
                }


                // ====================================================
                // SEND EMAIL
                // ====================================================

                if (!string.IsNullOrWhiteSpace(email))
                {
                    try
                    {
                        await _emailService.SendEmailAsync(
                            to: email,
                            subject:
                                $"SwiftCare Equipment Order {order.OrderNumber} - On The Way",
                            body: emailMessage,
                            senderName: "SwiftCare"
                        );
                    }
                    catch (Exception emailEx)
                    {
                        Console.WriteLine(
                            $"Failed to send email to {email}: " +
                            emailEx.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Error notifying administrator about " +
                    $"equipment order: {ex.Message}");
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
                int orderId;

                try
                {
                    orderId = int.Parse(
                        _encryptionService.Decrypt(
                            encryptedOrderId));
                }
                catch
                {
                    return;
                }


                var order = await _context.Orders
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o =>
                        o.OrderId == orderId);


                if (order == null ||
                    order.User == null)
                {
                    return;
                }


                var user = order.User;

                var phone = user.PhoneNumber;
                var email = user.Email;

                var firstName =
                    string.IsNullOrWhiteSpace(user.FirstName)
                        ? "Administrator"
                        : user.FirstName;


                string baseUrl =
                    "";


                string encodedOrderId =
                    WebUtility.UrlEncode(
                        encryptedOrderId);


                string orderLink =
                    $"{baseUrl}/Orders/OrderDetails?orderId={encodedOrderId}";


                string smsMessage =
                    $"Dear {firstName}, your SwiftCare equipment " +
                    $"order {order.OrderNumber} is ready for delivery. " +
                    $"View your order here: {orderLink}";


                string emailMessage = $@"
                    <html>
                    <body>

                        <p>
                            Dear {WebUtility.HtmlEncode(firstName)},
                        </p>

                        <p>
                            Your SwiftCare equipment order
                            <strong>
                                {WebUtility.HtmlEncode(order.OrderNumber)}
                            </strong>
                            is ready for delivery.
                        </p>

                        <p>
                            <a href=""{orderLink}""
                               style=""
                               display:inline-block;
                               padding:12px 20px;
                               background:#48AAAD;
                               color:#ffffff;
                               text-decoration:none;
                               border-radius:6px;
                               "">
                                View Order
                            </a>
                        </p>

                        <p>
                            Kind regards,<br/>
                            <strong>SwiftCare</strong>
                        </p>

                    </body>
                    </html>
                ";


                if (!string.IsNullOrWhiteSpace(phone))
                {
                    try
                    {
                        await _smsService.SendSmsAsync(
                            phone,
                            smsMessage);
                    }
                    catch (Exception smsEx)
                    {
                        Console.WriteLine(
                            $"Failed to send SMS: " +
                            smsEx.Message);
                    }
                }


                if (!string.IsNullOrWhiteSpace(email))
                {
                    try
                    {
                        await _emailService.SendEmailAsync(
                            to: email,
                            subject:
                                $"SwiftCare Equipment Order {order.OrderNumber} - Ready",
                            body: emailMessage,
                            senderName: "SwiftCare"
                        );
                    }
                    catch (Exception emailEx)
                    {
                        Console.WriteLine(
                            $"Failed to send email: " +
                            emailEx.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Error notifying administrator: " +
                    ex.Message);
            }
        }
    }
}