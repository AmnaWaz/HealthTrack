using System.Linq;

namespace HealthTrack.Helpers
{
    public static class PhoneHelper
    {
        // "+94 (77) 123-4567" -> "+94771234567"   (keeps a leading + and digits only)
        public static string Normalize(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null;
            phone = phone.Trim();
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            if (digits.Length == 0) return null;
            return (phone.StartsWith("+") ? "+" : "") + digits;
        }
    }
}
