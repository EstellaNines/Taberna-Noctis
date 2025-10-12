using System;

namespace TabernaNoctis.RandomSystem
{
    public static class HashUtil
    {
        public static int SeedFrom(string playerId, DateTime date, string streamKey)
        {
            string s = (playerId ?? string.Empty) + "|" + date.ToString("yyyyMMdd") + "|" + (streamKey ?? string.Empty);
            unchecked
            {
                const uint offset = 2166136261u;
                const uint prime = 16777619u;
                uint hash = offset;
                for (int i = 0; i < s.Length; i++)
                {
                    hash ^= s[i];
                    hash *= prime;
                }
                return (int)hash;
            }
        }
    }
}


