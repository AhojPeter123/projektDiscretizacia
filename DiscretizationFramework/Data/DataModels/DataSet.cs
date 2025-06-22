using System;
using System.Collections.Generic;
using System.Linq;

namespace DiscretizationFramework.Data.DataModels
{
    /// <summary>
    /// Reprezentuje celú dátovú sadu, kolekciu DataRow objektov.
    /// Obsahuje aj informácie o hlavičkách, cieľovom atribúte a inferovaných typoch.
    /// </summary>
    public class DataSet
    {
        public List<DataRow> Rows { get; }
        public List<string> Headers { get; }
        public string TargetAttributeName { get; } // Názov cieľového atribútu
        public Dictionary<string, Type> AttributeTypes { get; } // Inferované typy pre každý atribút

        /// <summary>
        /// Inicializuje novú inštanciu triedy DataSet.
        /// </summary>
        /// <param name="rows">Zoznam riadkov dát.</param>
        /// <param name="headers">Zoznam názvov hlavičiek (atribútov).</param>
        /// <param name="targetAttributeName">Názov cieľového atribútu.</param>
        /// <param name="attributeTypes">Slovník inferovaných typov pre každý atribút.</param>
        public DataSet(List<DataRow> rows, List<string> headers, string targetAttributeName, Dictionary<string, Type> attributeTypes)
        {
            Rows = rows ?? new List<DataRow>();
            Headers = headers ?? new List<string>();
            TargetAttributeName = targetAttributeName ?? string.Empty; // Zabezpečí, že TargetAttributeName nikdy nebude null
            AttributeTypes = attributeTypes ?? new Dictionary<string, Type>();
        }

        /// <summary>
        /// Získa hodnotu atribútu z daného riadku.
        /// </summary>
        /// <param name="row">Dátový riadok.</param>
        /// <param name="attributeName">Názov atribútu.</param>
        /// <returns>Hodnota atribútu alebo null, ak atribút neexistuje.</returns>
        public object? GetAttributeValue(DataRow row, string attributeName)
        {
            return row.Attributes.GetValueOrDefault(attributeName);
        }
    }
}