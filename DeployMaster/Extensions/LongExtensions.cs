namespace DeployMaster
{
    public static class LongExtensions
    {
        public static string GetFileLength(this long value)
        {
            if (value < 1024)
            {
                return value + " B";
            }
            if (value < 1024 * 1024)
            {
                return (value * 1F / 1024).ToString("0.0") + " KB";
            }
            if (value < 1024 * 1024 * 1024)
            {
                return (value * 1F / 1024 / 1024).ToString("0.0") + " MB";
            }
            return (value * 1F / 1024 / 1024 / 1024).ToString("0.0") + " GB";
        }

        public static string ToTime(this long seconds)
        {
            if (seconds < 60)
            {
                return seconds + "秒";
            }
            if (seconds < 3600)
            {
                return (seconds / 60) + "分" + (seconds % 60) + "秒";
            }
            return (seconds / 3600) + "时" + (seconds % 3600 / 60) + "分" + (seconds % 3600 % 60) + "秒";
        }
    }
}
