using System.Text;

namespace Zenthium.Editor.DataImport
{
    /// <summary>
    /// Turns a data id into a C# identifier and asset file name: "Gold-Plated Chrome" is GoldPlatedChrome,
    /// "summon_lifetime_s" is SummonLifetimeS.
    /// </summary>
    public static class IdentifierFormatter
    {
        /// <returns>False if the id has no ASCII letters or digits.</returns>
        public static bool TryFormat(string id, out string identifier)
        {
            var result = new StringBuilder(id.Length);
            bool startOfWord = true;
            foreach (char c in id)
            {
                if (c < 128 && char.IsLetterOrDigit(c))
                {
                    result.Append(startOfWord ? char.ToUpperInvariant(c) : c);
                    startOfWord = false;
                }
                else
                {
                    startOfWord = true;
                }
            }

            if (result.Length > 0 && char.IsDigit(result[0]))
            {
                result.Insert(0, '_');
            }

            identifier = result.ToString();
            return identifier.Length > 0;
        }
    }
}
