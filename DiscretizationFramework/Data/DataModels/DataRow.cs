// DiscretizationFramework/Data/DataModels/DataRow.cs
using System.Collections.Generic;

namespace DiscretizationFramework.Data.DataModels
{
    /// <summary>
    /// Reprezentuje jeden riadok dát v datasete.
    /// Atribúty sú dynamicky uložené v slovníku, čo umožňuje flexibilné typy dát.
    /// </summary>
    public class DataRow
    {
        /// <summary>
        /// Slovník obsahujúci atribúty riadku. Kľúčom je názov atribútu (string),
        /// hodnotou je samotná hodnota atribútu (object), ktorá môže byť rôzneho typu (int, double, string, atď.).
        /// </summary>
        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Cieľová premenná (trieda/label) pre daný riadok. Vždy je uložená ako string.
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// Inicializuje novú inštanciu triedy DataRow s danými atribútmi a cieľovou premennou.
        /// </summary>
        /// <param name="attributes">Slovník atribútov riadku.</param>
        /// <param name="target">Hodnota cieľovej premennej.</param>
        public DataRow(Dictionary<string, object> attributes, string target)
        {
            Attributes = attributes;
            Target = target;
        }

        /// <summary>
        /// Inicializuje novú prázdnu inštanciu triedy DataRow.
        /// </summary>
        public DataRow() { }
    }
}