using SwiftCare.Data;

namespace SwiftCare.Services
{
    public class OrderNumberGenerator
    {
        private SwiftCareDbContext _context;
        private Random _random;
        private HashSet<int> _usedNumbers;

        public OrderNumberGenerator(SwiftCareDbContext context)
        {
            _context = context;
            _random = new Random();

            var orderNumbers = _context.Orders
                                        .Select(o => o.OrderNumber.Substring(8))
                                        .ToList();

            _usedNumbers = new HashSet<int>(orderNumbers.Select(int.Parse));
        }


        public string GenerateOrderNumber()
        {
            string year = DateTime.Now.Year.ToString("00");
            string month = DateTime.Now.Month.ToString("00");
            string day = DateTime.Now.Day.ToString("00");

            int sequentialDigits;
            do
            {
                sequentialDigits = _random.Next(1000, 9999);
            } while (_usedNumbers.Contains(sequentialDigits));

            _usedNumbers.Add(sequentialDigits);

            return $"{year}{month}{day}{sequentialDigits.ToString("0000")}";
        }



        public string GenerateMedicationOrderNumber()
        {
            string year = DateTime.Now.Year.ToString("00");
            string month = DateTime.Now.Month.ToString("00");
            string day = DateTime.Now.Day.ToString("00");

            int sequentialDigits;
            do
            {
                sequentialDigits = _random.Next(1000, 9999);
            } while (_usedNumbers.Contains(sequentialDigits));

            _usedNumbers.Add(sequentialDigits);

            return $"MED-{year}{month}{day}{sequentialDigits:0000}";
        }

    }

}
