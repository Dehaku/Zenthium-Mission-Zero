namespace Zenthium.Core
{
    /// <summary>
    /// Rolls from its own seeded generator, so the same seed repeats the same rolls, such as a map layout. It doesn't
    /// touch Unity's shared generator.
    /// </summary>
    public sealed class SeededRandomSource : IRandomSource
    {
        private readonly System.Random generator;

        public SeededRandomSource(int seed)
        {
            generator = new System.Random(seed);
        }

        public float Value01()
        {
            return (float)generator.NextDouble();
        }
    }
}
