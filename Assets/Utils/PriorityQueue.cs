using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("PathFinder.Tests")]

namespace Utils
{
    // Extension methods for OrderedDictionary
    public static class OrderedDictionaryExtensions
    {
        /// <summary>
        /// Binary search for values in an OrderedDictionary.
        /// Returns the index if found, or a negative number that represents the bitwise 
        /// complement of the index where the item should be inserted.
        /// </summary>
        public static int BinarySearchValues(this OrderedDictionary dictionary, float valueToFind)
        {
            if (dictionary.Count == 0)
                return ~0; // Return bitwise complement of 0

            var values = dictionary.Values.OfType<float>().ToArray();
            
            int low = 0;
            int high = values.Length - 1;
            
            while (low <= high)
            {
                int mid = low + (high - low) / 2;
                float midValue = values[mid];
                
                if (midValue == valueToFind)
                    return mid;
                
                if (midValue < valueToFind)
                    low = mid + 1;
                else
                    high = mid - 1;
            }
            
            // Not found, return bitwise complement of where it would be inserted
            return ~low;
        }
    }

    public class PriorityQueue
    {
        internal OrderedDictionary OrderedDictionary { get; } = new();

        public int Count => OrderedDictionary.Count;

        public bool Contains(Vector2Int key)
        {
            return OrderedDictionary.Contains(key);
        }

        public void Clear()
        {
            OrderedDictionary.Clear();
        }

        /// <summary>
        ///     Binary search to find where to insert based on priority
        /// </summary>
        /// <param name="vector">The vector to enqueue</param>
        /// <param name="priority">The priority value (lower is better)</param>
        public void Enqueue(Vector2Int vector, float priority)
        {
            var insertPosition = OrderedDictionary.BinarySearchValues(priority);
            if (insertPosition < 0) insertPosition = ~insertPosition; // Convert to insert position
            OrderedDictionary.Insert(insertPosition, vector, priority);
        }

        public Vector2Int Dequeue()
        {
            var key = OrderedDictionary.Keys.OfType<Vector2Int>().FirstOrDefault();
            OrderedDictionary.RemoveAt(0);
            return key;
        }

        public void Remove(Vector2Int neighbour)
        {
            OrderedDictionary.Remove(neighbour);
        }
    }
}