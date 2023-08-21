using System.Text.RegularExpressions;

namespace RPM.Api.Model
{
    /// <summary>
    /// Cron 표현식을 변경하는 변환기를 제공합니다.
    /// </summary>
    public class CronExpressionConverter
    {
        /// <summary>
        /// 유닉스 계열 cron 표현식을 Quartz Cron 표현식으로 변경합니다.
        /// </summary>
        public static string ConvertToQuartzCronFormat(string cronExpression)
        {
            string[] cronParts = cronExpression.Split(' ');

            if (cronParts.Length == 7)
                return cronExpression;

            if (cronParts.Length < 5)
                return string.Empty;

            if (cronParts.Length == 5)
            {
                Array.Resize(ref cronParts, 7);
                Array.Copy(cronParts, 0, cronParts, 1, cronParts.Length - 1);
                cronParts[0] = "0";
            }

            string dayOfWeek = cronParts[5];
            if (IsIntegerDayOfWeek(dayOfWeek))
            {
                string quartzDayOfWeek = ConvertDayOfWeekToQuartzFormat(dayOfWeek);
                cronParts[5] = quartzDayOfWeek;

                if (cronParts[3] == "*")
                    cronParts[3] = "?";
            }
            else if (dayOfWeek == "*")
                cronParts[5] = "?";

            return string.Join(" ", cronParts);
        }

        private static bool IsIntegerDayOfWeek(string dayOfWeek)
        {
            string pattern = @"^\d+(-\d+)?$";
            return Regex.IsMatch(dayOfWeek, pattern);
        }

        private static string ConvertDayOfWeekToQuartzFormat(string dayOfWeek)
        {
            if (dayOfWeek.Contains("-"))
            {
                string[] range = dayOfWeek.Split('-');
                int startDay = int.Parse(range[0]);
                int endDay = int.Parse(range[1]);

                if (startDay < 0 || startDay > 7 || endDay < 0 || endDay > 7 || startDay > endDay)
                {
                    throw new ArgumentException("Invalid day of week range.");
                }

                return $"{ChangeToDayName(range[0])}-{ChangeToDayName(range[1])}";
            }
            else
            {
                return ChangeToDayName(dayOfWeek);
            }
        }

        private static string ChangeToDayName(string dayOfWeek)
        {
            switch (dayOfWeek)
            {
                case "0": return "SUN";
                case "1": return "MON";
                case "2": return "TUE";
                case "3": return "WED";
                case "4": return "THU";
                case "5": return "FRI";
                case "6": return "SAT";
                case "7": return "SUN";
                default: throw new ArgumentException("Invalid day of week.");
            }
        }
    }
}
