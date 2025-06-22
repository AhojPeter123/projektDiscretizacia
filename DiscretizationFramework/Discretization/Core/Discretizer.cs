using System;
using System.Collections.Generic;
using System.Linq;
using DiscretizationFramework.Data.DataModels;
using DiscretizationFramework.Discretization.Steps; // Stále potrebné pre CommonSteps

namespace DiscretizationFramework.Discretization.Core
{
    public class Discretizer
    {
        private readonly string _algorithmName;
        private readonly List<DiscretizationStep> _steps; // Zoznam delegátov

        private Discretizer(string algorithmName, List<DiscretizationStep> steps)
        {
            _algorithmName = algorithmName;
            _steps = steps;
        }

        public static Discretizer Create(string algorithmName, List<DiscretizationStep> steps)
        {
            return new Discretizer(algorithmName, steps);
        }

        public DiscretizationResult Discretize(DataSet dataSet, string attributeName, Dictionary<string, object>? initialParameters = null)
        {
            if (dataSet == null) throw new ArgumentNullException(nameof(dataSet), "DataSet cannot be null.");
            if (string.IsNullOrWhiteSpace(attributeName)) throw new ArgumentNullException(nameof(attributeName), "Attribute name cannot be null or empty.");

            Console.WriteLine($"\n--- Spúšťam diskretizáciu pre atribút '{attributeName}' pomocou algoritmu: '{_algorithmName}' ---");

            var context = new DiscretizationContext(dataSet, attributeName, initialParameters);
            
            // Pred vykonaním krokov inicializujeme NumericValues volaním CommonSteps.ConvertAttributesToNumeric
            // ako súčasti pipeline, nie tu. Takže tu už nie je priame volanie.
            // Spoliehame sa, že ConvertAttributesToNumeric bude prvý krok v _steps.

            // Vykonaj všetky definované diskretizačné kroky
            foreach (var step in _steps)
            {
                context = step(context); // Každý krok vezme kontext a vráti modifikovaný
                if (context == null)
                {
                    Console.WriteLine($"Chyba: Krok diskretizácie '{step.Method.Name}' vrátil null kontext. Ukončujem.");
                    return new DiscretizationResult();
                }
            }

            // Po všetkých krokoch získať výsledné numerické hodnoty z kontextu,
            // ktoré boli naplnené v CommonSteps.ConvertAttributesToNumeric
            List<double> originalNumericValues = new List<double>(context.NumericValues);

            // Kontrola, či sa podarilo získať nejaké numerické hodnoty po konverznom kroku
            if (!originalNumericValues.Any())
            {
                 Console.WriteLine($"Upozornenie: Atribút '{attributeName}' neobsahuje žiadne konvertovateľné numerické hodnoty. Preskakujem diskretizáciu.");
                 return new DiscretizationResult
                 {
                    DiscretizedRows = dataSet.Rows.Select(r => new DataRow(new Dictionary<string, object>(r.Attributes), r.Target)).ToList(),
                    FinalCutPoints = new List<double>(),
                    OriginalNumericValues = originalNumericValues,
                    DiscretizedAttributeName = attributeName
                 };
            }

            // Konvertovať riadky na diskretizované
            var discretizedRows = new List<DataRow>();
            foreach (var row in dataSet.Rows)
            {
                var newAttributes = new Dictionary<string, object>(row.Attributes);
                if (row.Attributes.TryGetValue(attributeName, out object? originalValueObj))
                {
                    double? originalNumericVal = CommonSteps.TryConvertToDouble(originalValueObj);
                    if (originalNumericVal.HasValue)
                    {
                        string binLabel = GetBinLabel(originalNumericVal.Value, context.CutPoints);
                        newAttributes[attributeName] = binLabel;
                    }
                    // Ak sa nedá konvertovať, ponecháme pôvodnú hodnotu (string)
                }
                discretizedRows.Add(new DataRow(newAttributes, row.Target));
            }

            return new DiscretizationResult
            {
                DiscretizedRows = discretizedRows,
                FinalCutPoints = context.CutPoints,
                OriginalNumericValues = originalNumericValues,
                DiscretizedAttributeName = attributeName
            };
        }

        private static string GetBinLabel(double value, List<double> cutPoints)
        {
            // Predpokladáme, že cutPoints sú už zoradené
            for (int i = 0; i < cutPoints.Count; i++)
            {
                if (value < cutPoints[i])
                {
                    double lowerBound = (i == 0) ? double.NegativeInfinity : cutPoints[i - 1];
                    return $"[{lowerBound:F2}, {cutPoints[i]:F2})";
                }
            }
            // Posledný interval
            double lowerLastBound = cutPoints.Any() ? cutPoints.Last() : double.NegativeInfinity;
            return $"[{lowerLastBound:F2}, {double.PositiveInfinity:F2})";
        }
    }
}