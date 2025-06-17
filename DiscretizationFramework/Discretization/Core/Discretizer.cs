// DiscretizationFramework/Discretization/Core/Discretizer.cs
using System;
using System.Collections.Generic;
using System.Linq;
using DiscretizationFramework.Data.DataModels;

namespace DiscretizationFramework.Discretization.Core
{
    /// <summary>
    /// Hlavný orchestrátor pre diskretizáciu.
    /// Je konfigurovaný zoznamom krokov (delegátov), ktoré predstavujú jednotlivé fázy diskretizačného algoritmu.
    /// </summary>
    public class Discretizer
    {
        private readonly List<DiscretizationStep> _steps;
        private readonly string _algorithmName;

        /// <summary>
        /// Privátny konštruktor. Inštancie sa vytvárajú pomocou statickej factory metódy Create().
        /// </summary>
        /// <param name="algorithmName">Názov diskretizačného algoritmu (pre logovanie).</param>
        /// <param name="steps">Zoznam delegátov (funkcií), ktoré tvoria kroky tohto algoritmu.</param>
        private Discretizer(string algorithmName, List<DiscretizationStep> steps)
        {
            _algorithmName = algorithmName ?? "Unknown Algorithm";
            _steps = steps ?? throw new ArgumentNullException(nameof(steps), "Discretization steps cannot be null.");
        }

        /// <summary>
        /// Factory metóda na vytvorenie novej inštancie Discretizer s danou konfiguráciou krokov.
        /// Toto je preferovaný spôsob vytvárania Discretizer objektov.
        /// </summary>
        /// <param name="algorithmName">Názov diskretizačného algoritmu.</param>
        /// <param name="steps">Zoznam krokov (delegátov) definujúcich algoritmus.</param>
        /// <returns>Nová inštancia Discretizer.</returns>
        public static Discretizer Create(string algorithmName, List<DiscretizationStep> steps)
        {
            return new Discretizer(algorithmName, steps);
        }

        /// <summary>
        /// Spustí diskretizačný proces na špecifikovaný numerický atribút datasetu.
        /// Iteruje cez konfigurované kroky a aplikuje ich na DiscretizationContext.
        /// </summary>
        /// <param name="dataSet">Pôvodný dataset, ktorý obsahuje dáta na diskretizáciu.</param>
        /// <param name="attributeName">Názov atribútu, ktorý sa má diskretizovať (musí byť numerický).</param>
        /// <param name="initialParameters">Voliteľné počiatočné parametre, ktoré sa odovzdajú do kontextu.</param>
        /// <returns>Zoznam DataRow objektov, kde je špecifikovaný atribút diskretizovaný (prevedený na string).</returns>
        public List<DataRow> Discretize(DataSet dataSet, string attributeName, Dictionary<string, object> initialParameters = null)
        {
            if (dataSet == null)
            {
                throw new ArgumentNullException(nameof(dataSet), "DataSet cannot be null.");
            }
            if (string.IsNullOrWhiteSpace(attributeName))
            {
                throw new ArgumentNullException(nameof(attributeName), "Attribute name cannot be null or empty.");
            }

            Console.WriteLine($"\n--- Spúšťam diskretizáciu pre atribút '{attributeName}' pomocou algoritmu: '{_algorithmName}' ---");

            var context = new DiscretizationContext(dataSet, attributeName, initialParameters);

            if (!dataSet.AttributeTypes.ContainsKey(attributeName) ||
                !(dataSet.AttributeTypes[attributeName] == typeof(double) ||
                  dataSet.AttributeTypes[attributeName] == typeof(int) ||
                  dataSet.AttributeTypes[attributeName] == typeof(float)))
            {
                Console.WriteLine($"Upozornenie: Atribút '{attributeName}' nie je numerický alebo nebol nájdený. Preskakujem diskretizáciu.");
                return dataSet.Rows.Select(r => new DataRow(new Dictionary<string, object>(r.Attributes), r.Target)).ToList();
            }

            if (!context.NumericValues.Any())
            {
                Console.WriteLine($"Upozornenie: Atribút '{attributeName}' neobsahuje žiadne numerické hodnoty pre diskretizáciu. Preskakujem.");
                return dataSet.Rows.Select(r => new DataRow(new Dictionary<string, object>(r.Attributes), r.Target)).ToList();
            }

            foreach (var step in _steps)
            {
                try
                {
                    Console.WriteLine($"  -> Vykonávam krok: {step.Method.Name}");
                    context = step(context);
                    if (context == null)
                    {
                        Console.WriteLine($"Krok '{step.Method.Name}' vrátil null kontext. Diskretizácia prerušená.");
                        return dataSet.Rows.Select(r => new DataRow(new Dictionary<string, object>(r.Attributes), r.Target)).ToList();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Chyba pri vykonávaní kroku '{step.Method.Name}' pre atribút '{attributeName}': {ex.Message}");
                    return dataSet.Rows.Select(r => new DataRow(new Dictionary<string, object>(r.Attributes), r.Target)).ToList();
                }
            }

            var discretizedRows = new List<DataRow>();
            foreach (var originalRow in dataSet.Rows)
            {
                var newAttributes = new Dictionary<string, object>(originalRow.Attributes);
                if (originalRow.Attributes.ContainsKey(attributeName) &&
                    (originalRow.Attributes[attributeName] is double ||
                     originalRow.Attributes[attributeName] is int ||
                     originalRow.Attributes[attributeName] is float))
                {
                    double valueToDiscretize = Convert.ToDouble(originalRow.Attributes[attributeName]);
                    newAttributes[attributeName] = GetBinName(valueToDiscretize, context.CutPoints);
                }
                else
                {
                    newAttributes[attributeName] = originalRow.Attributes.ContainsKey(attributeName) ? originalRow.Attributes[attributeName]?.ToString() : "N/A";
                }
                discretizedRows.Add(new DataRow(newAttributes, originalRow.Target));
            }

            Console.WriteLine($"Diskretizácia atribútu '{attributeName}' dokončená. Vytvorených {context.CutPoints.Count + 1} binov.");
            return discretizedRows;
        }

        /// <summary>
        /// Pomocná metóda na priradenie numerickej hodnoty k príslušnému binu na základe rezových bodov.
        /// </summary>
        /// <param name="value">Numerická hodnota, ktorú treba priradiť.</param>
        /// <param name="cutPoints">Zoradený zoznam rezových bodov.</param>
        /// <returns>Názov binu (napr. "Bin_0", "Bin_1").</returns>
        private string GetBinName(double value, List<double> cutPoints)
        {
            for (int i = 0; i < cutPoints.Count; i++)
            {
                if (value <= cutPoints[i])
                {
                    return $"Bin_{i}";
                }
            }
            return $"Bin_{cutPoints.Count}";
        }
    }
}