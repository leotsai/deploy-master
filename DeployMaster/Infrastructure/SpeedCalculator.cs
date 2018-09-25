using System;
using System.Collections.Generic;
using System.Linq;

namespace DeployMaster
{
    public class SpeedCalculator
    {
        private const int SECONDS = 10;
        private static readonly List<SpeedItem> _items = new List<SpeedItem>();
        private DateTime _startTime;

        public static void Reset()
        {
            lock (_items)
            {
                _items.Clear();
            }
        }

        public static void Entry(long value)
        {
            lock (_items)
            {
                _items.Add(new SpeedItem()
                {
                    Time = DateTime.Now,
                    Value = value
                });
            }
        }

        public static string GetSpeed()
        {
            lock (_items)
            {
                if (_items.Count == 0)
                {
                    return "未知";
                }
                var minTime = _items.First().Time;
                var seconds = (int)((DateTime.Now - minTime).TotalSeconds);
                if (seconds > SECONDS)
                {
                    seconds = SECONDS;
                }
                if (seconds == 0)
                {
                    return "未知";
                }
                var since = DateTime.Now.AddSeconds(-SECONDS);
                _items.RemoveAll(x => x.Time < since);
                var total = _items.Sum(x => x.Value);
                return (total / seconds).GetFileLength() + "/秒";
            }
        }

        public static long GetBytesPerSecond()
        {
            lock (_items)
            {
                var since = DateTime.Now.AddSeconds(-SECONDS);
                _items.RemoveAll(x => x.Time < since);
                var total = _items.Sum(x => x.Value);
                return total / SECONDS;
            }
        }

        private class SpeedItem
        {
            public DateTime Time { get; set; }
            public long Value { get; set; }
        }
    }
}
