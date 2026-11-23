namespace Zenthium.Entities
{
    /// <summary>
    /// Clears an entity's DoTs and debuffs when Defiance procs, except Carrying Treasure and hidden markers
    /// (stats.md). Milestone 4's status system implements it; until then a proc cleanses nothing.
    /// </summary>
    public interface IDefianceCleanse
    {
        void CleanseForDefiance();
    }
}
