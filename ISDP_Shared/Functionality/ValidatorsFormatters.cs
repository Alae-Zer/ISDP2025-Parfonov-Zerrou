
//LOTS OF CONTENT JUST FROM STACKOVERFLOW!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

//ISDP Project
//Mohammed Alae-Zerrou, Serhii Parfonov
//NBCC, Winter 2025
//Completed By Mohammed with some changes from serhii
//Last Modified by Serhii on Feb 16,2025
public static class ValidatorsFormatters
{
    //Checks if string is empty or whitespace
    //Sends String to check
    //Returns Bool true if empty, false if not
    public static bool IsEmpty(string value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    //Validates if string is a valid integer within range
    //Sends String value to check, Int minimum value (optional), Int maximum value (optional)
    //Returns Bool true if valid integer in range, false if not
    public static bool IsValidInteger(string value, int minValue = int.MinValue, int maxValue = int.MaxValue)
    {
        if (!int.TryParse(value, out int result))
            return false;
        return result >= minValue && result <= maxValue;
    }

    //Checks if string length is valid
    //Sends String to check and Int maximum length
    //Returns Bool true if valid length, false if not
    public static bool IsValidLength(string value, int maxLength)
    {
        if (IsEmpty(value)) return false;
        return value.Length <= maxLength;
    }

    //Validates item name
    //Sends String item name
    //Returns Bool true if valid, false if not
    public static bool ValidateItemName(string name)
    {
        if (IsEmpty(name)) return false;
        if (!IsValidLength(name, 100)) return false;
        return true;
    }

    //Gets error message for item name
    //Sends String item name
    //Returns String error message if invalid, empty if valid
    public static string GetItemNameError(string name)
    {
        if (IsEmpty(name)) return "Item name cannot be empty";
        if (!IsValidLength(name, 100)) return "Item name cannot exceed 100 characters";
        return string.Empty;
    }

    //Validates SKU
    //Sends String SKU
    //Returns Bool true if valid, false if not
    public static bool ValidateSKU(string sku)
    {
        if (IsEmpty(sku)) return false;
        if (!IsValidLength(sku, 20)) return false;
        return true;
    }

    //Gets error message for SKU
    //Sends String SKU
    //Returns String error message if invalid, empty if valid
    public static string GetSKUError(string sku)
    {
        if (IsEmpty(sku)) return "SKU cannot be empty";
        if (!IsValidLength(sku, 20)) return "SKU cannot exceed 20 characters";
        return string.Empty;
    }

    //Validates category
    //Sends String category
    //Returns Bool true if valid, false if not
    public static bool ValidateCategory(string category)
    {
        if (IsEmpty(category)) return false;
        if (!IsValidLength(category, 32)) return false;
        return true;
    }

    //Gets error message for category
    //Sends String category
    //Returns String error message if invalid, empty if valid
    public static string GetCategoryError(string category)
    {
        if (IsEmpty(category)) return "Category cannot be empty";
        if (!IsValidLength(category, 32)) return "Category cannot exceed 32 characters";
        return string.Empty;
    }

    //Validates weight
    //Sends String weight
    //Returns Bool true if valid, false if not
    public static bool ValidateWeight(string weight)
    {
        if (!decimal.TryParse(weight, out decimal value)) return false;
        if (value < 0) return false;
        if (value > 99999999.99m) return false;
        return true;
    }

    //Gets error message for weight
    //Sends String weight
    //Returns String error message if invalid, empty if valid
    public static string GetWeightError(string weight)
    {
        if (!decimal.TryParse(weight, out decimal value)) return "Weight must be a valid decimal number";
        if (value < 0) return "Weight cannot be negative";
        if (value > 99999999.99m) return "Weight cannot exceed 99,999,999.99";
        return string.Empty;
    }

    //Validates price
    //Sends String price
    //Returns Bool true if valid, false if not
    public static bool ValidatePrice(string price)
    {
        if (!decimal.TryParse(price, out decimal value)) return false;
        if (value < 0) return false;
        if (value > 99999999.99m) return false;
        return true;
    }

    //Gets error message for price
    //Sends String price
    //Returns String error message if invalid, empty if valid
    public static string GetPriceError(string price)
    {
        if (!decimal.TryParse(price, out decimal value)) return "Price must be a valid decimal number";
        if (value < 0) return "Price cannot be negative";
        if (value > 99999999.99m) return "Price cannot exceed 99,999,999.99";
        return string.Empty;
    }

    //Validates case size
    //Sends String case size
    //Returns Bool true if valid, false if not
    public static bool ValidateCaseSize(string caseSize)
    {
        if (!int.TryParse(caseSize, out int value)) return false;
        if (value < 0) return false;
        return true;
    }

    //Gets error message for case size
    //Sends String case size
    //Returns String error message if invalid, empty if valid
    public static string GetCaseSizeError(string caseSize)
    {
        if (!int.TryParse(caseSize, out int value)) return "Case size must be a valid integer";
        if (value < 0) return "Case size cannot be negative";
        return string.Empty;
    }

    //Formats postal code
    //Sends String not formatted postal code
    //Returns String formatted postal code
    public static string FormatPostalCode(string postalCode)
    {
        if (IsEmpty(postalCode)) return string.Empty;
        string cleaned = CleanPostalCode(postalCode);
        return cleaned.Length == 6 ? $"{cleaned.Substring(0, 3)} {cleaned.Substring(3)}" : cleaned;
    }

    //Formats phone number
    //Sends String not formatted phone number
    //Returns String formatted phone number
    public static string FormatPhoneNumber(string phone)
    {
        if (IsEmpty(phone)) return string.Empty;
        string cleaned = CleanPhoneNumber(phone);
        return cleaned.Length == 10 ? $"({cleaned.Substring(0, 3)}) {cleaned.Substring(3, 3)}-{cleaned.Substring(6)}" : cleaned;
    }

    //Cleans postal code of spaces and makes uppercase
    //Sends String postal code to clean
    //Returns String cleaned postal code
    public static string CleanPostalCode(string postalCode)
    {
        return postalCode?.Replace(" ", "").ToUpper() ?? string.Empty;
    }

    //Cleans phone number of non-digit characters
    //Sends String phone number to clean
    //Returns String cleaned phone number
    public static string CleanPhoneNumber(string phone)
    {
        if (IsEmpty(phone)) return string.Empty;
        return new string(phone.Where(char.IsDigit).ToArray());
    }

    //Validates postal code format
    //Sends String postal code
    //Returns Bool true if valid format, false if not
    public static bool IsValidPostalCode(string postalCode)
    {
        if (IsEmpty(postalCode)) return false;
        string cleaned = CleanPostalCode(postalCode);
        return cleaned.Length == 6 &&
               char.IsLetter(cleaned[0]) && char.IsDigit(cleaned[1]) &&
               char.IsLetter(cleaned[2]) && char.IsDigit(cleaned[3]) &&
               char.IsLetter(cleaned[4]) && char.IsDigit(cleaned[5]);
    }

    //Validates phone number has 10 digits
    //Sends String phone number
    //Returns Bool true if valid, false if not
    public static bool IsValidPhoneNumber(string phone)
    {
        if (IsEmpty(phone)) return false;
        string cleaned = CleanPhoneNumber(phone);
        return cleaned.Length == 10;
    }

    //Validates email format
    //Sends String email
    //Returns Bool true if valid format, false if not
    public static bool IsValidEmail(string email)
    {
        if (IsEmpty(email)) return false;
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

    //Validates name contains only letters, spaces, hyphens and apostrophes
    //Sends String name
    //Returns Bool true if valid format, false if not
    public static bool IsValidName(string name)
    {
        if (IsEmpty(name)) return false;
        return name.All(c => char.IsLetter(c) || c == ' ' || c == '-' || c == '\'');
    }

    //Formats currency to two decimal places
    //Sends Decimal amount
    //Returns String formatted currency
    public static string FormatCurrency(decimal amount)
    {
        return amount.ToString("C2");
    }

    //Formats date to yyyy-MM-dd
    //Sends DateTime date
    //Returns String formatted date
    public static string FormatDate(DateTime date)
    {
        return date.ToString("yyyy-MM-dd");
    }

    //Formats date and time
    //Sends DateTime date time
    //Returns String formatted date time
    public static string FormatDateTime(DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
    }

    //Checks if value is in range
    //Sends Int value and Int min and max range
    //Returns Bool true if in range, false if not
    public static bool IsInRange(int value, int min, int max)
    {
        return value >= min && value <= max;
    }

    //Checks if value is in range
    //Sends Decimal value and Decimal min and max range
    //Returns Bool true if in range, false if not
    public static bool IsInRange(decimal value, decimal min, decimal max)
    {
        return value >= min && value <= max;
    }

    //Checks if date is in range
    //Sends DateTime value and DateTime min and max range
    //Returns Bool true if in range, false if not
    public static bool IsInRange(DateTime value, DateTime min, DateTime max)
    {
        return value >= min && value <= max;
    }
}