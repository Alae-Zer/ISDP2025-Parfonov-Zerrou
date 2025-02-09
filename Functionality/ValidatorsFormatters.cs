using System.Text.RegularExpressions;

namespace ISDP2025_Parfonov_Zerrou.Functionality
{
    public static class ValidatorsFormatters
    {
        // Formatting methods
        public static string FormatPostalCode(string postalCode)
        {
            if (string.IsNullOrWhiteSpace(postalCode)) return string.Empty;
            postalCode = CleanPostalCode(postalCode);
            return postalCode.Length == 6
                ? $"{postalCode.Substring(0, 3)} {postalCode.Substring(3)}"
                : postalCode;
        }

        public static string FormatPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return string.Empty;
            phone = CleanPhoneNumber(phone);
            return phone.Length == 10
                ? $"({phone.Substring(0, 3)}) {phone.Substring(3, 3)}-{phone.Substring(6)}"
                : phone;
        }

        // Cleaning methods
        public static string CleanPostalCode(string postalCode)
        {
            return postalCode?.Replace(" ", "").ToUpper() ?? string.Empty;
        }

        public static string CleanPhoneNumber(string phone)
        {
            return phone?.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "") ?? string.Empty;
        }

        // Validation methods
        public static bool IsValidPostalCode(string postalCode)
        {
            if (string.IsNullOrWhiteSpace(postalCode)) return false;
            string cleaned = CleanPostalCode(postalCode);
            return cleaned.Length == 6 && Regex.IsMatch(cleaned, @"^[A-Z]\d[A-Z]\d[A-Z]\d$");
        }

        public static bool IsValidPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            string cleaned = CleanPhoneNumber(phone);
            return cleaned.Length == 10 && cleaned.All(char.IsDigit);
        }

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return Regex.IsMatch(name, @"^[a-zA-Z\s\-']+$");
        }

        public static bool IsValidInteger(string value, int minValue = int.MinValue, int maxValue = int.MaxValue)
        {
            if (int.TryParse(value, out int result))
            {
                return result >= minValue && result <= maxValue;
            }
            return false;
        }

        public static bool IsValidDecimal(string value, decimal minValue = decimal.MinValue, decimal maxValue = decimal.MaxValue)
        {
            if (decimal.TryParse(value, out decimal result))
            {
                return result >= minValue && result <= maxValue;
            }
            return false;
        }

        // String manipulation methods
        public static string TruncateString(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return input.Length <= maxLength ? input : input.Substring(0, maxLength);
        }

        public static string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            // Remove any HTML tags
            input = Regex.Replace(input, "<.*?>", string.Empty);
            // Remove special characters except basic punctuation
            return Regex.Replace(input, @"[^\w\s\-.,!?@]", string.Empty);
        }

        // Currency formatting
        public static string FormatCurrency(decimal amount)
        {
            return amount.ToString("C2");
        }

        // Date formatting
        public static string FormatDate(DateTime date)
        {
            return date.ToString("yyyy-MM-dd");
        }

        public static string FormatDateTime(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }

        // Additional utility methods
        public static bool IsInRange(int value, int min, int max)
        {
            return value >= min && value <= max;
        }

        public static bool IsInRange(decimal value, decimal min, decimal max)
        {
            return value >= min && value <= max;
        }

        public static bool IsInRange(DateTime value, DateTime min, DateTime max)
        {
            return value >= min && value <= max;
        }
    }
}