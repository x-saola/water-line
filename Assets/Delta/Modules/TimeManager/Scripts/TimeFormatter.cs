using System;

namespace Delta.Modules.Utilities
{
    public static class TimeFormatter
    {
        public static string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.Days > 0)
                return $"{timeSpan.Days}d {timeSpan.Hours}h";
            else
            {
                if (timeSpan.Hours > 0)
                    return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
                else
                {
                    if (timeSpan.Minutes > 0)
                        return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
                    else
                        return $"{timeSpan.Seconds}s";
                }
            }
        }
    }
}