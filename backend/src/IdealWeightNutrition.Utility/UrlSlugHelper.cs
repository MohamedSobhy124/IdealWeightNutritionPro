using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace IdealWeightNutrition.Utility
{
    public static class UrlSlugHelper
    {
        /// <summary>
        /// Generates a URL-friendly slug from a product name
        /// </summary>
        /// <param name="text">The text to convert to a slug</param>
        /// <param name="maxLength">Maximum length of the slug (default: 100 for better SEO)</param>
        /// <returns>URL-friendly slug</returns>
        public static string GenerateSlug(string text, int maxLength = 100)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Convert to lowercase
            text = text.ToLowerInvariant().Trim();

            // Remove Arabic diacritics and normalize
            text = RemoveDiacritics(text);

            // Step 1: Replace ALL whitespace (spaces, tabs, newlines) with hyphens FIRST
            text = Regex.Replace(text, @"\s+", "-", RegexOptions.Compiled);
            
            // Step 2: Replace ALL punctuation and special characters with hyphens
            // This includes: commas, periods, semicolons, colons, exclamation, question marks, parentheses, brackets, braces, quotes, etc.
            text = Regex.Replace(text, @"[,\\.;:!?()\[\]{}""'`~@#$%^&*+=|\\/<>_]", "-", RegexOptions.Compiled);

            // Step 3: Remove all characters that are not letters, numbers, or hyphens
            // Allow Unicode word characters (Arabic, English, numbers) and hyphens only
            text = Regex.Replace(text, @"[^\p{L}\p{N}\-]", "", RegexOptions.Compiled);

            // Step 4: Replace multiple consecutive hyphens with single hyphen
            text = Regex.Replace(text, @"-+", "-", RegexOptions.Compiled);

            // Step 5: Trim hyphens from start and end
            text = text.Trim('-');

            // If empty after processing, return empty
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Step 6: Truncate to max length (prefer breaking at word boundaries)
            if (text.Length > maxLength)
            {
                text = text.Substring(0, maxLength);
                // Try to break at a hyphen if possible
                var lastHyphen = text.LastIndexOf('-');
                if (lastHyphen > maxLength * 0.7 && lastHyphen > 0) // If hyphen is in last 30% of string
                {
                    text = text.Substring(0, lastHyphen);
                }
                else
                {
                    text = text.TrimEnd('-');
                }
            }

            return text;
        }

        /// <summary>
        /// Generates a unique slug by appending a number if the slug already exists
        /// </summary>
        /// <param name="baseSlug">The base slug to make unique</param>
        /// <param name="existingSlugs">Collection of existing slugs to check against</param>
        /// <returns>Unique slug</returns>
        public static string GenerateUniqueSlug(string baseSlug, System.Collections.Generic.IEnumerable<string> existingSlugs)
        {
            if (string.IsNullOrWhiteSpace(baseSlug))
                return string.Empty;

            var slug = baseSlug;
            var slugSet = existingSlugs?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var counter = 1;

            while (slugSet.Contains(slug))
            {
                var suffix = $"-{counter}";
                var maxBaseLength = 200 - suffix.Length;
                var truncatedBase = baseSlug.Length > maxBaseLength 
                    ? baseSlug.Substring(0, maxBaseLength).TrimEnd('-')
                    : baseSlug;
                slug = truncatedBase + suffix;
                counter++;
            }

            return slug;
        }

        /// <summary>
        /// Removes diacritics from Arabic and other characters
        /// </summary>
        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
