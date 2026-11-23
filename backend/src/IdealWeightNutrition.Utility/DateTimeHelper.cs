using System;

namespace IdealWeightNutrition.Utility
{
    /// <summary>
    /// Helper class to get current time in UAE timezone (Asia/Dubai, UTC+4)
    /// </summary>
    public static class DateTimeHelper
    {
        private static readonly TimeZoneInfo _uaeTimeZone;
        private static readonly TimeSpan _uaeOffset = TimeSpan.FromHours(4); // UAE is UTC+4
        
        static DateTimeHelper()
        {
            // Get UAE timezone (Asia/Dubai, UTC+4)
            try
            {
                // Try Windows timezone ID first
                _uaeTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Arabian Standard Time");
            }
            catch
            {
                try
                {
                    // Try Linux/Mac timezone ID
                    _uaeTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Dubai");
                }
                catch
                {
                    // Final fallback: Create custom timezone for UTC+4 (Gulf Standard Time)
                    _uaeTimeZone = TimeZoneInfo.CreateCustomTimeZone(
                        "UAE Standard Time",
                        _uaeOffset,
                        "UAE Standard Time",
                        "UAE Standard Time");
                }
            }
        }

        /// <summary>
        /// Gets the current date and time in UAE timezone (UTC+4)
        /// </summary>
        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _uaeTimeZone);

        /// <summary>
        /// Gets the current date in UAE timezone (UTC+4)
        /// </summary>
        public static DateTime Today => Now.Date;

        /// <summary>
        /// Gets the current UTC time
        /// </summary>
        public static DateTime UtcNow => DateTime.UtcNow;

        /// <summary>
        /// Converts a UTC DateTime to UAE timezone
        /// </summary>
        public static DateTime ToUaeTime(DateTime utcDateTime)
        {
            if (utcDateTime.Kind == DateTimeKind.Unspecified)
            {
                // Assume it's UTC if unspecified
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc), _uaeTimeZone);
            }
            return TimeZoneInfo.ConvertTime(utcDateTime, _uaeTimeZone);
        }

        /// <summary>
        /// Converts a UAE DateTime to UTC
        /// </summary>
        public static DateTime ToUtc(DateTime uaeDateTime)
        {
            if (uaeDateTime.Kind == DateTimeKind.Unspecified)
            {
                // Assume it's UAE time if unspecified
                return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(uaeDateTime, DateTimeKind.Unspecified), _uaeTimeZone);
            }
            return TimeZoneInfo.ConvertTime(uaeDateTime, _uaeTimeZone).ToUniversalTime();
        }

        /// <summary>
        /// Adds working days to a date, excluding weekends (Friday and Saturday in UAE)
        /// </summary>
        /// <param name="startDate">The starting date</param>
        /// <param name="workingDays">Number of working days to add</param>
        /// <returns>The date after adding the specified working days</returns>
        public static DateTime AddWorkingDays(DateTime startDate, int workingDays)
        {
            if (workingDays == 0) return startDate;

            var currentDate = startDate;
            var daysToAdd = workingDays > 0 ? 1 : -1;
            var remainingDays = Math.Abs(workingDays);

            while (remainingDays > 0)
            {
                currentDate = currentDate.AddDays(daysToAdd);

                // Skip weekends (Sunday = 5, Saturday = 6)
                if (currentDate.DayOfWeek != DayOfWeek.Sunday && currentDate.DayOfWeek != DayOfWeek.Saturday)
                {
                    remainingDays--;
                }
            }

            return currentDate;
        }

        /// <summary>
        /// Gets estimated delivery message (within 48 hours in working days)
        /// </summary>
        /// <returns>Formatted string with delivery timeframe</returns>
        public static string GetEstimatedDeliveryRange()
        {
            return "Within 48 hours (working days)";
        }

        /// <summary>
        /// Gets estimated delivery message with language support
        /// </summary>
        /// <param name="cultureCode">Culture code (e.g., "ar" for Arabic, "en" for English)</param>
        /// <returns>Localized delivery message</returns>
        public static string GetEstimatedDeliveryRange(string cultureCode)
        {
            if (!string.IsNullOrEmpty(cultureCode) && cultureCode.StartsWith("ar", StringComparison.OrdinalIgnoreCase))
            {
                return "خلال 48 ساعة (أيام العمل)";
            }
            return "Within 48 hours (working days)";
        }

        /// <summary>
        /// Gets estimated delivery date range with start and end dates (legacy support)
        /// </summary>
        /// <param name="startWorkingDays">Number of working days for start date (default: 2)</param>
        /// <param name="endWorkingDays">Number of working days for end date (default: 2)</param>
        /// <returns>Formatted string with delivery timeframe</returns>
        public static string GetEstimatedDeliveryRange(int startWorkingDays = 2, int endWorkingDays = 2)
        {
            // Return the standard 48 hours message regardless of parameters
            // to avoid showing specific dates
            return "Within 48 hours (working days)";
        }
    }
}
