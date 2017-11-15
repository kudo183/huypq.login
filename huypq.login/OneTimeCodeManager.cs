using System;
using System.Collections.Generic;

namespace huypq.login
{
    public class OneTimeCodeManager
    {
        public class OneTimeCodeEntry
        {
            public string Key { get; set; }
            public string Data { get; set; }
            public bool IsUsed { get; set; }
            public long ExpireTime { get; set; }

            public bool IsExpired()
            {
                return ExpireTime < DateTime.UtcNow.Ticks;
            }
        }

        static List<OneTimeCodeEntry> buffer = new List<OneTimeCodeEntry>(100);

        public static OneTimeCodeEntry FindEntry(string key)
        {
            foreach (var item in buffer)
            {
                if (item.IsUsed == false && item.Key == key)
                {
                    item.IsUsed = true;//mark as used

                    return item;
                }
            }

            return null;
        }

        public static void AddEntry(string key, string data)
        {
            foreach (var item in buffer)
            {
                if (item.IsUsed == true || item.IsExpired() == true)
                {
                    item.IsUsed = false;
                    item.Key = key;
                    item.Data = data;
                    item.ExpireTime = DateTime.UtcNow.AddMinutes(5).Ticks;
                    return;
                }
            }

            buffer.Add(new OneTimeCodeEntry()
            {
                IsUsed = false,
                Key = key,
                Data = data,
                ExpireTime = DateTime.UtcNow.AddMinutes(5).Ticks
            });
        }
    }
}
