using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>
    /// A generated data asset for one CSV row. The id is the row's name column, verbatim;
    /// code names entries through the generated <c>DataIds</c> constants.
    /// </summary>
    public abstract class DataEntry : ScriptableObject
    {
        [SerializeField] private string id;

        public string Id => id;

        internal void SetId(string newId)
        {
            id = newId;
        }
    }
}
