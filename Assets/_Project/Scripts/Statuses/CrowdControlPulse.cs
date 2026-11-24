namespace Zenthium.Statuses
{
    /// <summary>
    /// "While inside" crowd control on Leaders and the player pulses instead of holding: on, then off, repeating
    /// (statuses.md, Crowd control).
    /// </summary>
    public static class CrowdControlPulse
    {
        /// <param name="secondsInside">Game seconds since the entity entered the area.</param>
        public static bool IsOn(float secondsInside, float onSeconds, float offSeconds)
        {
            float period = onSeconds + offSeconds;
            if (period <= 0f || offSeconds <= 0f)
            {
                return true;
            }

            return secondsInside % period < onSeconds;
        }
    }
}
