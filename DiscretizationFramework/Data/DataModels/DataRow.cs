using System.Collections.Generic;
using System.Linq;

namespace DiscretizationFramework.Data.DataModels
{
    /// <summary>
    /// Reprezentuje jeden riadok dát s atribútmi a cieľovou (target) hodnotou.
    /// </summary>
    public class DataRow
    {
        public Dictionary<string, object> Attributes { get; }
        public string Target { get; } // Cieľový atribút (kategorický alebo diskretizovaný)

        /// <summary>
        /// Inicializuje novú inštanciu triedy DataRow.
        /// </summary>
        /// <param name="attributes">Slovník názvov atribútov a ich hodnôt.</param>
        /// <param name="target">Cieľová (target) hodnota riadku.</param>
        public DataRow(Dictionary<string, object> attributes, string target)
        {
            Attributes = attributes ?? new Dictionary<string, object>();
            Target = target ?? string.Empty; // Zabezpečí, že Target nikdy nebude null
        }

        public override string ToString()
        {
            var attrStrings = Attributes.Select(kv => $"{kv.Key}: {kv.Value}");
            return $"[{string.Join(", ", attrStrings)}], Target: {Target}";
        }
    }
}