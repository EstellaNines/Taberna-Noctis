using System;
using System.Collections.Generic;
using UnityEngine;

namespace TabernaNoctis.RandomSystem
{
    public sealed class UnityRandomStream : IRandomSource
    {
        private UnityEngine.Random.State _state;
        private readonly Action<UnityEngine.Random.State> _autoSaver;

        public UnityRandomStream(UnityEngine.Random.State initialState, Action<UnityEngine.Random.State> autoSaver = null)
        {
            _state = initialState;
            _autoSaver = autoSaver;
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return Execute(() => UnityEngine.Random.Range(minInclusive, maxExclusive));
        }

        public float Range(float minInclusive, float maxInclusive)
        {
            if (maxInclusive <= minInclusive) return minInclusive;
            return Execute(() => UnityEngine.Random.Range(minInclusive, maxInclusive));
        }

        public float Value01()
        {
            return Execute(() => UnityEngine.Random.value);
        }

        public void Shuffle<T>(IList<T> list)
        {
            if (list == null || list.Count <= 1) return;
            Execute(() =>
            {
                for (int i = list.Count - 1; i > 0; i--)
                {
                    int j = UnityEngine.Random.Range(0, i + 1);
                    if (j == i) continue;
                    T tmp = list[i];
                    list[i] = list[j];
                    list[j] = tmp;
                }
            });
        }

        public List<T> Deal<T>(IList<T> deck, int count, bool removeFromSource = false)
        {
            if (deck == null || count <= 0) return new List<T>();
            int take = Mathf.Min(count, deck.Count);
            if (take <= 0) return new List<T>();

            return Execute(() =>
            {
                var hand = new List<T>(take);
                if (removeFromSource)
                {
                    // Randomly remove from source deck without replacement
                    // Note: IList<T> might not support RemoveAt for some custom implementations
                    // so we copy to List<T> when needed.
                    if (deck is List<T> concrete)
                    {
                        for (int k = 0; k < take; k++)
                        {
                            int idx = UnityEngine.Random.Range(0, concrete.Count);
                            hand.Add(concrete[idx]);
                            concrete.RemoveAt(idx);
                        }
                    }
                    else
                    {
                        var buffer = new List<T>(deck);
                        for (int k = 0; k < take; k++)
                        {
                            int idx = UnityEngine.Random.Range(0, buffer.Count);
                            hand.Add(buffer[idx]);
                            buffer.RemoveAt(idx);
                        }
                        // write back if possible
                        if (deck is T[] array)
                        {
                            // arrays are fixed-size, cannot mutate length; skip write-back
                        }
                        else
                        {
                            // try to clear and add back
                            if (deck is ICollection<T> col && deck is IList<T> l)
                            {
                                l.Clear();
                                for (int i = 0; i < buffer.Count; i++) l.Add(buffer[i]);
                            }
                        }
                    }
                }
                else
                {
                    // Sample without replacement but keep the original deck intact
                    var indices = new List<int>(deck.Count);
                    for (int i = 0; i < deck.Count; i++) indices.Add(i);
                    // reuse this stream's Shuffle
                    Shuffle(indices);
                    for (int i = 0; i < take; i++) hand.Add(deck[indices[i]]);
                }

                return hand;
            });
        }

        public T PickOne<T>(IList<T> list)
        {
            if (list == null || list.Count == 0) return default;
            return Execute(() =>
            {
                int idx = UnityEngine.Random.Range(0, list.Count);
                return list[idx];
            });
        }

        public T PickWeighted<T>(IList<T> items, IList<float> weights)
        {
            if (items == null || weights == null) return default;
            if (items.Count == 0 || items.Count != weights.Count) return default;

            return Execute(() =>
            {
                float total = 0f;
                for (int i = 0; i < weights.Count; i++)
                {
                    float w = weights[i];
                    if (w > 0f) total += w;
                }
                if (total <= 0f) return default;

                float r = UnityEngine.Random.value * total;
                float acc = 0f;
                for (int i = 0; i < items.Count; i++)
                {
                    float w = weights[i];
                    if (w <= 0f) continue;
                    acc += w;
                    if (r <= acc) return items[i];
                }
                return items[items.Count - 1];
            });
        }

        private void Execute(Action action)
        {
            var original = UnityEngine.Random.state;
            try
            {
                UnityEngine.Random.state = _state;
                action();
                _state = UnityEngine.Random.state;
                _autoSaver?.Invoke(_state);
            }
            finally
            {
                UnityEngine.Random.state = original;
            }
        }

        private T Execute<T>(Func<T> func)
        {
            var original = UnityEngine.Random.state;
            try
            {
                UnityEngine.Random.state = _state;
                T result = func();
                _state = UnityEngine.Random.state;
                _autoSaver?.Invoke(_state);
                return result;
            }
            finally
            {
                UnityEngine.Random.state = original;
            }
        }
    }
}


