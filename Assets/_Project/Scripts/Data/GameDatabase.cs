using System.Collections.Generic;
using UnityEngine;

namespace Zenthium.Data
{
    /// <summary>
    /// Every generated data asset and constant, made by the importer. Reference this asset directly;
    /// look entries up with the generated <c>DataIds</c> constants.
    /// </summary>
    public sealed class GameDatabase : ScriptableObject
    {
        [SerializeField] private FormulaConstants formulaConstants = new FormulaConstants();
        [SerializeField] private EntityConstants entityConstants = new EntityConstants();
        [SerializeField] private ModuleConstants moduleConstants = new ModuleConstants();
        [SerializeField] private RankScalingConstants rankScaling = new RankScalingConstants();
        [SerializeField] private EconomyConstants economy = new EconomyConstants();
        [SerializeField] private WorldConstants worldConstants = new WorldConstants();
        [SerializeField] private FireConstants fireConstants = new FireConstants();
        [SerializeField] private ValuableConstants valuableConstants = new ValuableConstants();
        [SerializeField] private TierTables tiers = new TierTables();
        [SerializeField] private DataTable<ModuleData> modules = new DataTable<ModuleData>();
        [SerializeField] private DataTable<FactionData> factions = new DataTable<FactionData>();
        [SerializeField] private DataTable<EntityData> entities = new DataTable<EntityData>();
        [SerializeField] private DataTable<HeroData> heroes = new DataTable<HeroData>();
        [SerializeField] private DataTable<PropData> props = new DataTable<PropData>();
        [SerializeField] private DataTable<PriceData> prices = new DataTable<PriceData>();

        [System.NonSerialized] private BalanceFormulas formulas;

        public FormulaConstants FormulaConstants => formulaConstants;
        public EntityConstants EntityConstants => entityConstants;
        public ModuleConstants ModuleConstants => moduleConstants;
        public RankScalingConstants RankScaling => rankScaling;
        public EconomyConstants Economy => economy;
        public WorldConstants WorldConstants => worldConstants;
        public FireConstants FireConstants => fireConstants;
        public ValuableConstants ValuableConstants => valuableConstants;
        public TierTables Tiers => tiers;
        public DataTable<ModuleData> Modules => modules;
        public DataTable<FactionData> Factions => factions;
        public DataTable<EntityData> Entities => entities;
        public DataTable<HeroData> Heroes => heroes;
        public DataTable<PropData> Props => props;
        public DataTable<PriceData> Prices => prices;

        public BalanceFormulas Formulas
        {
            get
            {
                if (formulas == null)
                {
                    formulas = new BalanceFormulas(formulaConstants);
                }

                return formulas;
            }
        }

        internal void Populate(
            FormulaConstants newFormulaConstants,
            EntityConstants newEntityConstants,
            ModuleConstants newModuleConstants,
            RankScalingConstants newRankScaling,
            EconomyConstants newEconomy,
            WorldConstants newWorldConstants,
            FireConstants newFireConstants,
            ValuableConstants newValuableConstants,
            TierTables newTiers,
            List<ModuleData> newModules,
            List<FactionData> newFactions,
            List<EntityData> newEntities,
            List<HeroData> newHeroes,
            List<PropData> newProps,
            List<PriceData> newPrices)
        {
            formulaConstants = newFormulaConstants;
            entityConstants = newEntityConstants;
            moduleConstants = newModuleConstants;
            rankScaling = newRankScaling;
            economy = newEconomy;
            worldConstants = newWorldConstants;
            fireConstants = newFireConstants;
            valuableConstants = newValuableConstants;
            tiers = newTiers;
            modules.SetEntries(newModules);
            factions.SetEntries(newFactions);
            entities.SetEntries(newEntities);
            heroes.SetEntries(newHeroes);
            props.SetEntries(newProps);
            prices.SetEntries(newPrices);
            formulas = null;
        }
    }
}
