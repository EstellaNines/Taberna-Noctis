using UnityEngine;

namespace TabernaNoctis.RandomSystem
{
    public sealed class ES3RandomStateStore
    {
        public bool Exists(string key)
        {
            return ES3.KeyExists(key);
        }

        public void Save(string key, UnityEngine.Random.State state)
        {
            ES3.Save(key, state);
        }

        public UnityEngine.Random.State Load(string key)
        {
            return ES3.Load<UnityEngine.Random.State>(key);
        }
    }
}


