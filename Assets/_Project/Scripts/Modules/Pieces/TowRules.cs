namespace Zenthium.Modules
{
    /// <summary>What Tow Beam can take (modules.md: "a prop, citizen or Valuable"), so never a hero or a downed enemy.</summary>
    public static class TowRules
    {
        public static bool CanTow(DragTargetKind kind)
        {
            return kind == DragTargetKind.Citizen || kind == DragTargetKind.Prop || kind == DragTargetKind.Valuable;
        }
    }
}
