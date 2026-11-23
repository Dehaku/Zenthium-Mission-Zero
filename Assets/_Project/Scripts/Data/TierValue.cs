using System;
using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>
    /// A standard tier word (Light, Short, Huge...) and the number it stood for at import.
    /// The word is kept for display.
    /// </summary>
    [Serializable]
    public struct TierValue
    {
        [SerializeField] private string word;
        [SerializeField] private float value;

        public TierValue(string word, float value)
        {
            this.word = word;
            this.value = value;
        }

        public string Word => word;
        public float Value => value;
        public bool IsSet => !string.IsNullOrEmpty(word);
    }
}
