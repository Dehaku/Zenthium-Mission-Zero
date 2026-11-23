namespace Zenthium.Core
{
    /// <summary>
    /// Rolls with Unity's shared random generator.
    /// </summary>
    public sealed class UnityRandomSource : IRandomSource
    {
        public float Value01()
        {
            return UnityEngine.Random.value;
        }
    }
}
