namespace Zenthium.Editor.DataImport
{
    /// <summary>
    /// The text of every design CSV, as read from <c>Docs/design/data/</c>.
    /// </summary>
    public sealed class DesignDataSource
    {
        public const string ConstantsFile = "constants.csv";
        public const string ModulesFile = "modules.csv";
        public const string EntitiesFile = "entities.csv";
        public const string HeroesFile = "heroes.csv";
        public const string PropsFile = "props.csv";
        public const string PricesFile = "prices.csv";

        public DesignDataSource(string constants, string modules, string entities, string heroes, string props, string prices)
        {
            Constants = constants;
            Modules = modules;
            Entities = entities;
            Heroes = heroes;
            Props = props;
            Prices = prices;
        }

        public string Constants { get; }
        public string Modules { get; }
        public string Entities { get; }
        public string Heroes { get; }
        public string Props { get; }
        public string Prices { get; }
    }
}
