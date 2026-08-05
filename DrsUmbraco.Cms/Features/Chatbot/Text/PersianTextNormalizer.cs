using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DrsUmbraco.Cms.Features.Chatbot.Text;

public sealed class PersianTextNormalizer
    : IPersianTextNormalizer
{
    public string Normalize(string? text)
    {
        // 1. بررسی null یا whitespace
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }
        // 2. نرمال‌سازی Unicode
        string normalizedText = text.Normalize(NormalizationForm.FormKC);
        // 3. تبدیل حروف عربی به فارسی
        normalizedText = normalizedText
            .Replace('ي', 'ی')
            .Replace('ى', 'ی')
            .Replace('ك', 'ک');
        // 4. حذف اعراب
        normalizedText = RemoveDiacritics(normalizedText);
        // 5. تبدیل نیم‌فاصله به فاصله
        normalizedText = normalizedText.Replace('\u200C', ' ');
        // 6. حذف علائم نگارشی
        normalizedText = ReplacePunctuationWithSpaces(normalizedText);
        // 7. یکسان‌سازی فاصله‌ها
        normalizedText = Regex.Replace(normalizedText, @"\s+", " ");
        // 8. تبدیل حروف انگلیسی به lowercase
        normalizedText = normalizedText.ToLowerInvariant();
        // 9. Trim و return
        return normalizedText.Trim();
    }

    private static string RemoveDiacritics(string text)
    {
        string decomposedText =
            text.Normalize(NormalizationForm.FormD);

        StringBuilder result = new();

        foreach (char character in decomposedText)
        {
            UnicodeCategory category =
                CharUnicodeInfo.GetUnicodeCategory(character);

            if (category != UnicodeCategory.NonSpacingMark)
            {
                result.Append(character);
            }
        }

        return result
            .ToString()
            .Normalize(NormalizationForm.FormC);
    }

    private static string ReplacePunctuationWithSpaces(string text)
    {
        StringBuilder result = new();

        foreach (char character in text)
        {
            if (char.IsPunctuation(character))
            {
                result.Append(' ');
                continue;
            }

            result.Append(character);
        }

        return result.ToString();
    }
}