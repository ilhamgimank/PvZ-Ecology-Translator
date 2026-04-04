using System.Text.RegularExpressions;
using System.Globalization; // [TAMBAHAN] Wajib untuk format angka desimal dengan titik (bukan koma)

namespace PvZEcologyTranslator.Features
{
    public static class CurrencyConverter
    {
        public static bool EnableConversion = true;
        public static string TargetCurrency = "IDR";

        public static string ConvertString(string input, string ignoredLanguage = "")
        {
            if (!EnableConversion || string.IsNullOrEmpty(input) || !input.Contains("$"))
                return input;

            return Regex.Replace(input, @"\$\s*(\d+)", match =>
            {
                if (long.TryParse(match.Groups[1].Value, out long originalValue))
                {
                    // Melakukan konversi dan memformatnya dengan singkatan K/M/B
                    switch (TargetCurrency)
                    {
                        case "IDR": // Rupiah
                            return "Rp " + FormatWithAbbreviations(originalValue * 15000);
                        case "EUR": // Euro
                            return "€" + FormatWithAbbreviations((long)(originalValue * 0.9f));
                        case "CNY": // Yuan
                            return "¥" + FormatWithAbbreviations(originalValue * 7);
                        case "JPY": // Yen
                            return "¥" + FormatWithAbbreviations(originalValue * 150);
                        case "GBP": // Poundsterling
                            return "£" + FormatWithAbbreviations((long)(originalValue * 0.8f));
                        case "USD": // Dollar asli
                        default:
                            return "$" + FormatWithAbbreviations(originalValue);
                    }
                }

                return match.Value;
            });
        }

        // [FITUR BARU 0.10.8] Fungsi memotong angka raksasa menjadi huruf singkatan RPG
        private static string FormatWithAbbreviations(long number)
        {
            // Jika lebih dari 1 Miliar (Billion)
            if (number >= 1000000000)
                return (number / 1000000000D).ToString("0.##", CultureInfo.InvariantCulture) + "B";

            // Jika lebih dari 1 Juta (Million)
            if (number >= 1000000)
                return (number / 1000000D).ToString("0.##", CultureInfo.InvariantCulture) + "M";

            // Jika lebih dari 1 Ribu (Kilo)
            if (number >= 1000)
                return (number / 1000D).ToString("0.##", CultureInfo.InvariantCulture) + "K";

            // Jika di bawah seribu, biarkan normal
            return number.ToString();
        }
    }
}