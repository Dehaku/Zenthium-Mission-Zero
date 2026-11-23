using System;
using System.Collections.Generic;

namespace Zenthium.Entities
{
    /// <summary>
    /// Identifies one modifier added to a <see cref="ModifierStack"/>, so its source can remove it later.
    /// </summary>
    public readonly struct ModifierHandle
    {
        internal ModifierHandle(int id)
        {
            Id = id;
        }

        internal int Id { get; }
    }

    /// <summary>
    /// A list of modifier values from different sources, combined the way the owning rule needs:
    /// added, multiplied as boosts, or multiplied as reductions.
    /// </summary>
    public sealed class ModifierStack
    {
        private struct Entry
        {
            public int Id;
            public float Value;
        }

        private readonly List<Entry> entries = new List<Entry>();

        // Starts at 1 so a default handle never matches an entry.
        private int nextId = 1;

        public event Action OnChanged;

        public int Count => entries.Count;

        public ModifierHandle Add(float value)
        {
            var handle = new ModifierHandle(nextId++);
            entries.Add(new Entry { Id = handle.Id, Value = value });
            OnChanged?.Invoke();
            return handle;
        }

        /// <returns>False if the handle isn't in this stack.</returns>
        public bool Remove(ModifierHandle handle)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Id == handle.Id)
                {
                    entries.RemoveAt(i);
                    OnChanged?.Invoke();
                    return true;
                }
            }

            return false;
        }

        public void Clear()
        {
            if (entries.Count == 0)
            {
                return;
            }

            entries.Clear();
            OnChanged?.Invoke();
        }

        /// <summary>All values added together. 0 when empty.</summary>
        public float Sum()
        {
            float sum = 0f;
            foreach (Entry entry in entries)
            {
                sum += entry.Value;
            }

            return sum;
        }

        /// <summary>Percentage boosts multiplied: +100% and +50% give 3. 1 when empty.</summary>
        public float BoostProduct()
        {
            float product = 1f;
            foreach (Entry entry in entries)
            {
                product *= 1f + entry.Value;
            }

            return product;
        }

        /// <summary>The fraction left after every reduction: two 50% reductions leave 0.25. 1 when empty.</summary>
        public float RemainingAfterReductions()
        {
            float remaining = 1f;
            foreach (Entry entry in entries)
            {
                remaining *= 1f - entry.Value;
            }

            return remaining;
        }
    }
}
