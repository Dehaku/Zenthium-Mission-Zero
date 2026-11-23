using System;
using Zenthium.Data;

namespace Zenthium.Entities
{
    /// <summary>
    /// An entity's move speed (combat.md): the base speed, with percentage bonuses of it added together.
    /// Slows arrive with statuses in Milestone 4.
    /// </summary>
    public sealed class MoveSpeed
    {
        private readonly EntityConstants constants;
        private readonly ModifierStack bonuses = new ModifierStack();

        internal MoveSpeed(EntityConstants constants)
        {
            this.constants = constants ?? throw new ArgumentNullException(nameof(constants));
            bonuses.OnChanged += RaiseMoveSpeedChanged;
        }

        public event Action OnMoveSpeedChanged;

        /// <summary>Meters per second. Never below 0.</summary>
        public float Current => Math.Max(0f, constants.BaseMoveSpeed * (1f + bonuses.Sum()));

        /// <param name="fraction">The bonus as a fraction of the base speed: 0.5 is +50%.</param>
        public ModifierHandle AddBonus(float fraction)
        {
            return bonuses.Add(fraction);
        }

        public bool RemoveBonus(ModifierHandle handle)
        {
            return bonuses.Remove(handle);
        }

        private void RaiseMoveSpeedChanged()
        {
            OnMoveSpeedChanged?.Invoke();
        }
    }
}
