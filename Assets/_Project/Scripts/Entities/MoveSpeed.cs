using System;
using Zenthium.Data;

namespace Zenthium.Entities
{
    /// <summary>
    /// An entity's move speed (combat.md): the base speed, with percentage bonuses of it added together, then every
    /// slow multiplied on top, so slows never reach 100% (statuses.md).
    /// </summary>
    public sealed class MoveSpeed
    {
        private readonly EntityConstants constants;
        private readonly ModifierStack bonuses = new ModifierStack();
        private readonly ModifierStack slows = new ModifierStack();

        internal MoveSpeed(EntityConstants constants)
        {
            this.constants = constants ?? throw new ArgumentNullException(nameof(constants));
            bonuses.OnChanged += RaiseMoveSpeedChanged;
            slows.OnChanged += RaiseMoveSpeedChanged;
        }

        public event Action OnMoveSpeedChanged;

        /// <summary>Meters per second. Never below 0.</summary>
        public float Current =>
            Math.Max(0f, constants.BaseMoveSpeed * (1f + bonuses.Sum()) * slows.RemainingAfterReductions());

        /// <param name="fraction">The bonus as a fraction of the base speed: 0.5 is +50%.</param>
        public ModifierHandle AddBonus(float fraction)
        {
            return bonuses.Add(fraction);
        }

        public bool RemoveBonus(ModifierHandle handle)
        {
            return bonuses.Remove(handle);
        }

        /// <param name="fraction">The slow as a fraction: 0.3 takes 30% off what's left after other slows.</param>
        public ModifierHandle AddSlow(float fraction)
        {
            return slows.Add(fraction);
        }

        public bool RemoveSlow(ModifierHandle handle)
        {
            return slows.Remove(handle);
        }

        private void RaiseMoveSpeedChanged()
        {
            OnMoveSpeedChanged?.Invoke();
        }
    }
}
