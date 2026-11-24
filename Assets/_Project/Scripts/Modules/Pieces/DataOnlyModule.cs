using Zenthium.Data;

namespace Zenthium.Modules
{
    /// <summary>
    /// A module with no behavior of its own: shared rules read its data while it's equipped. Sprinter's resources
    /// and Immovable's KnockbackResistant tag work through the entity; Sprinter's <c>sprint_bonus</c> through Sprint;
    /// Express Delivery's <c>citizen_capacity</c> through Drag.
    /// </summary>
    public sealed class DataOnlyModule : ModuleBehaviour
    {
        public DataOnlyModule(ModuleData data) : base(data)
        {
        }
    }
}
