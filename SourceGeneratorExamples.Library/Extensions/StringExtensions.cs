namespace SourceGeneratorExamples.Library.Extensions
{
    public static class StringExtensions
    {
        private static (string firstChar, string remaining) GetStringParts(string value)
        {
            if (string.IsNullOrEmpty(value)) return ("", "");
            return value.Length == 1 ? (value.Substring(0, 1), "") : (value.Substring(0, 1), value.Substring(1));
        }

        private static string ChangeFirstChar(string value, bool isLower)
        {
            if (string.IsNullOrEmpty(value)) return value;
            var (firstChar, remaining) = GetStringParts(value);
            return $"{(isLower ? firstChar.ToLowerInvariant() : firstChar.ToUpperInvariant())}{remaining}";
        }

        public static string ToUnderscoreCase(this string value)
        {
            return $"_{ChangeFirstChar(value, true)}";
        }

        public static string ToCamelCase(this string value)
        {
            return $"{ChangeFirstChar(value, true)}";
        }

        public static string ToPascalCase(this string value)
        {
            return $"{ChangeFirstChar(value, false)}";
        }
    }
}