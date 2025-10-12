using System;
using UnityEngine;

namespace TabernaNoctis.RandomSystem
{
    public sealed class RandomService
    {
        public static RandomService Instance { get; } = new RandomService();

        private readonly ES3RandomStateStore _store;

        private RandomService()
        {
            _store = new ES3RandomStateStore();
        }

        public IRandomSource GetDailyStream(string streamKey, string playerId, DateTime? date = null, bool autoSave = true)
        {
            DateTime d = date ?? DateTime.Now;
            string es3Key = "rng/daily/" + (playerId ?? string.Empty) + "/" + d.ToString("yyyyMMdd") + "/" + (streamKey ?? string.Empty);

            if (_store.Exists(es3Key))
            {
                var st = _store.Load(es3Key);
                return new UnityRandomStream(st, autoSave ? (s => _store.Save(es3Key, s)) : null);
            }
            else
            {
                var st = CreateStateFromSeed(HashUtil.SeedFrom(playerId, d, streamKey));
                _store.Save(es3Key, st);
                return new UnityRandomStream(st, autoSave ? (s => _store.Save(es3Key, s)) : null);
            }
        }

        public IRandomSource GetPersistentStream(string streamKey, string playerId, bool autoSave = true)
        {
            string es3Key = "rng/persistent/" + (playerId ?? string.Empty) + "/" + (streamKey ?? string.Empty);
            if (_store.Exists(es3Key))
            {
                var st = _store.Load(es3Key);
                return new UnityRandomStream(st, autoSave ? (s => _store.Save(es3Key, s)) : null);
            }
            else
            {
                // Seed is stable for the pair (playerId, streamKey)
                var baseDate = new DateTime(2000, 1, 1);
                var st = CreateStateFromSeed(HashUtil.SeedFrom(playerId, baseDate, streamKey + "|PERSIST"));
                _store.Save(es3Key, st);
                return new UnityRandomStream(st, autoSave ? (s => _store.Save(es3Key, s)) : null);
            }
        }

        private static UnityEngine.Random.State CreateStateFromSeed(int seed)
        {
            var original = UnityEngine.Random.state;
            try
            {
                UnityEngine.Random.InitState(seed);
                return UnityEngine.Random.state;
            }
            finally
            {
                UnityEngine.Random.state = original;
            }
        }
    }
}


