using System;
using System.Collections.Generic;
using System.Linq;
using DiscretizationFramework.Data.DataModels;
using DiscretizationFramework.Discretization.Core;

namespace DiscretizationFramework.Discretization.Steps
{
    /// <summary>
    /// Reprezentuje generický rekurzívny diskretizačný krok (top-down delenie).
    /// Umožňuje injektovať logiku pre výpočet kritéria delenia a akceptácie cut-pointu.
    /// </summary>
    public class GeneralRecursiveStep
    {
        // Delegát pre logiku rozhodovania o delení pre danú partíciu.
        // Vráti najlepší cut-point a bool indikujúci, či sa má delenie vykonať.
        // Particia, Min Hodnota, Max Hodnota, Nájdené Cut-points (pre pridanie)
        public delegate (double? bestCutPoint, bool shouldSplit) RecursiveSplitCriterionLogic(
            List<DataRow> currentRows,
            string attributeName,
            double currentMin,
            double currentMax,
            HashSet<double> allFoundCutPoints, // Pre pridanie cut-pointu priamo z logiky kritéria
            Dictionary<string, object> parameters // Kontextové parametre
        );

        private readonly string _stepName;
        private readonly RecursiveSplitCriterionLogic _splitCriterionLogic;
        private readonly int _maxDepth; // Limit hĺbky rekurzie

        /// <summary>
        /// Inicializuje nový generický rekurzívny krok diskretizácie.
        /// </summary>
        /// <param name="stepName">Názov tohto kroku (pre logovanie).</param>
        /// <param name="splitCriterionLogic">Funkcia implementujúca logiku delenia (nájdenie cut-pointu a rozhodnutie o delení).</param>
        /// <param name="maxDepth">Maximálna hĺbka rekurzie na zabránenie nekonečnej slučke.</param>
        public GeneralRecursiveStep(string stepName, RecursiveSplitCriterionLogic splitCriterionLogic, int maxDepth = 100)
        {
            _stepName = stepName ?? throw new ArgumentNullException(nameof(stepName));
            _splitCriterionLogic = splitCriterionLogic ?? throw new ArgumentNullException(nameof(splitCriterionLogic));
            _maxDepth = maxDepth;
        }

        /// <summary>
        /// Vykoná rekurzívny diskretizačný krok.
        /// </summary>
        /// <param name="context">DiscretizationContext obsahujúci dáta a parametre.</param>
        /// <returns>Modifikovaný DiscretizationContext s vypočítanými cut-pointami.</returns>
        public DiscretizationContext Execute(DiscretizationContext context)
        {
            Console.WriteLine($"  Spúšťam rekurzívny krok: {_stepName}.");

            if (!context.NumericValues.Any())
            {
                Console.WriteLine($"  Upozornenie: Žiadne numerické hodnoty pre {_stepName}. Preskakujem.");
                return context;
            }

            var allRows = context.OriginalDataSet.Rows;
            var attributeName = context.AttributeName;
            var initialMinVal = context.NumericValues.Min();
            var initialMaxVal = context.NumericValues.Max();

            var finalCutPoints = new HashSet<double>();

            try
            {
                // Spustí rekurzívny proces
                RecursiveSplit(allRows, attributeName, initialMinVal, initialMaxVal, finalCutPoints, context.Parameters, 0);

                context.CutPoints = finalCutPoints.OrderBy(cp => cp).ToList();
                Console.WriteLine($"  * {_stepName} generoval {context.CutPoints.Count} cut-pointov.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Chyba počas vykonávania rekurzívneho kroku {_stepName}: {ex.Message}");
                throw;
            }

            return context;
        }

        /// <summary>
        /// Vnútorná rekurzívna funkcia pre delenie partícií.
        /// </summary>
        private void RecursiveSplit(
            List<DataRow> currentRows,
            string attributeName,
            double currentMin,
            double currentMax,
            HashSet<double> allFoundCutPoints,
            Dictionary<string, object> parameters,
            int depth)
        {
            // Základné podmienky pre ukončenie rekurzie:
            // 1. Žiadne riadky alebo prázdny interval
            // 2. Prekročená maximálna hĺbka
            if (!currentRows.Any() || currentMax <= currentMin || depth >= _maxDepth)
            {
                // Console.WriteLine($"    Rekurzia zastavená (Hĺbka {depth}, Riadky: {currentRows.Count}, Rozsah: [{currentMin:F2}-{currentMax:F2}]).");
                return;
            }

            // Zavolá injektovanú logiku pre určenie cut-pointu a rozhodnutie o delení
            var (bestCutPoint, shouldSplit) = _splitCriterionLogic(
                currentRows, attributeName, currentMin, currentMax, allFoundCutPoints, parameters);

            if (shouldSplit && bestCutPoint.HasValue && !double.IsNaN(bestCutPoint.Value))
            {
                allFoundCutPoints.Add(bestCutPoint.Value);
                // Console.WriteLine($"    Nájdený cut-point v hĺbke {depth}: {bestCutPoint.Value:F2}");

                // Rozdelenie dát na dve partície na základe nájdeného cut-pointu
                var leftPartitionRows = new List<DataRow>();
                var rightPartitionRows = new List<DataRow>();

                foreach (var row in currentRows)
                {
                    if (row.Attributes.TryGetValue(attributeName, out object? valueObj))
                    {
                        double? value = CommonSteps.TryConvertToDouble(valueObj);
                        if (value.HasValue)
                        {
                            if (value.Value < bestCutPoint.Value)
                            {
                                leftPartitionRows.Add(row);
                            }
                            else
                            {
                                rightPartitionRows.Add(row);
                            }
                        }
                    }
                }
                
                // Rekurzívne volanie pre obe nové partície
                RecursiveSplit(leftPartitionRows, attributeName, currentMin, bestCutPoint.Value, allFoundCutPoints, parameters, depth + 1);
                RecursiveSplit(rightPartitionRows, attributeName, bestCutPoint.Value, currentMax, allFoundCutPoints, parameters, depth + 1);
            }
            // else
            // {
            //     Console.WriteLine($"    Rekurzia zastavená v hĺbke {depth}: Žiadny zlepšujúci cut-point pre '{attributeName}' v rozsahu [{currentMin:F2}-{currentMax:F2}].");
            // }
        }

        // Implicitná konverzia na DiscretizationStep delegáta
        public static implicit operator DiscretizationStep(GeneralRecursiveStep step)
        {
            return step.Execute;
        }
    }
}