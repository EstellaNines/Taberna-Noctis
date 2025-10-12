using System.Collections.Generic;

namespace TabernaNoctis.RandomSystem
{
    public interface IRandomSource
    {
        int Range(int minInclusive, int maxExclusive);
        float Range(float minInclusive, float maxInclusive);
        float Value01();

        void Shuffle<T>(IList<T> list);

        List<T> Deal<T>(IList<T> deck, int count, bool removeFromSource = false);

        T PickOne<T>(IList<T> list);

        T PickWeighted<T>(IList<T> items, IList<float> weights);
    }
}


