using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealmsForgotten
{
    public static class Extensions
    {
        private static Random _random = new Random();

        public static T RandomElementByWeight<T>(this Dictionary<T, int> dictionary, Func<KeyValuePair<T, int>, int> weightSelector)
        {
            int totalWeight = dictionary.Sum(weightSelector);

            int randomWeight = _random.Next(totalWeight) + 1;

            foreach (var kvp in dictionary)
            {
                if (randomWeight <= kvp.Value)
                    return kvp.Key;

                randomWeight -= kvp.Value;
            }

            throw new InvalidOperationException("Dictionary is empty or has negative weights.");
        }
    }
}
