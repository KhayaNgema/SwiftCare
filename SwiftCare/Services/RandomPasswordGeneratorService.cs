using System;

namespace SwiftCare.Services
{
    public class RandomPasswordGeneratorService
    {
        private const string _validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const string _specialChars = "!@#$%&*()?";

        public string GenerateRandomPassword(int length = 10)
        {
            Random random = new Random();
            char[] chars = new char[length];

            chars[0] = _validChars[random.Next(26) + 26];
            chars[1] = _specialChars[random.Next(_specialChars.Length)];
            chars[2] = _validChars[random.Next(10) + 52];

            for (int i = 3; i < length; i++)
            {
                chars[i] = _validChars[random.Next(_validChars.Length)];
            }

            for (int i = length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                char temp = chars[i];
                chars[i] = chars[j];
                chars[j] = temp;
            }

            return new string(chars);
        }
    }
}
