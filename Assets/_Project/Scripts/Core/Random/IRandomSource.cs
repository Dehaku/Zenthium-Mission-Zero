namespace Zenthium.Core
{
    /// <summary>
    /// Random rolls for game rules, so tests can force the outcome.
    /// </summary>
    public interface IRandomSource
    {
        /// <returns>A number from 0 to 1. A roll succeeds when it's below the chance.</returns>
        float Value01();
    }
}
