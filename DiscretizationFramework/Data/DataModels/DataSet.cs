// DiscretizationFramework/Data/DataModels/DataSet.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiscretizationFramework.Data.DataModels
{
    /// <summary>
    /// Reprezentuje celý dataset ako kolekciu DataRow objektov.
    /// Obsahuje aj meta-informácie o hlavičkách a inferovaných typoch atribútov.
    /// </summary>
    public class DataSet
    {
        /// <summary>
        /// Zoznam všetkých riadkov dát v datasete.
        /// </summary>
        public List<DataRow> Rows { get; set; } = new List<DataRow>();

        /// <summary>
        /// Zoznam názvov hlavičiek (stĺpcov) datasetu, v poradí, v akom sa nachádzajú v súbore.
        /// </summary>
        public List<string> Headers { get; set; } = new List<string>();

        /// <summary>
        /// Slovník mapujúci názov atribútu na jeho inferovaný dátový typ (napr. typeof(int), typeof(double), typeof(string)).
        /// </summary>
        public Dictionary<string, Type> AttributeTypes { get; set; } = new Dictionary<string, Type>();

        /// <summary>
        /// Názov stĺpca, ktorý bol určený ako cieľová premenná (label), ktorú model predpovedá.
        /// </summary>
        public string TargetAttributeName { get; set; }

        /// <summary>
        /// Pridá nový riadok dát do datasetu.
        /// </summary>
        /// <param name="row">Objekt DataRow, ktorý sa má pridať.</param>
        public void AddRow(DataRow row)
        {
            Rows.Add(row);
        }

        /// <summary>
        /// Získa zoznam všetkých numerických hodnôt pre daný atribút z celého datasetu.
        /// Predpokladá, že atribút bol inferovaný ako int, double alebo float.
        /// </summary>
        /// <param name="attributeName">Názov atribútu.</param>
        /// <returns>Zoznam double hodnôt. Ak atribút nie je numerický, vráti prázdny zoznam.</returns>
        public List<double> GetNumericValues(string attributeName)
        {
            if (AttributeTypes.TryGetValue(attributeName, out Type type) &&
                (type == typeof(double) || type == typeof(int) || type == typeof(float)))
            {
                return Rows.Select(r => Convert.ToDouble(r.Attributes[attributeName])).ToList();
            }
            return new List<double>();
        }
    }
}