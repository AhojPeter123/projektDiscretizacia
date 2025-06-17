// DiscretizationFramework/Discretization/Steps/GeneralRecursiveStep.cs
using System;
using System.Collections.Generic;
using System.Linq;
using DiscretizationFramework.Data.DataModels;
using DiscretizationFramework.Discretization.Core;

namespace DiscretizationFramework.Discretization.Steps
{
    /// <summary>
    /// Obsahuje všeobecné kroky pre rekurzívne diskretizačné algoritmy.
    /// Tieto kroky sú vysoko abstraktné a prijímajú delegátov pre konkrétne stratégie splitov.
    /// </summary>
    public static class GeneralRecursiveStep
    {
        /// <summary>
        /// Všeobecný rekurzívny krok, ktorý hľadá optimálne rezové body v segmentoch dát.
        /// Tento krok je motorom pre algoritmy ako MDLP.
        /// </summary>
        /// <param name="context">Aktuálny kontext diskretizácie.</param>
        /// <param name="splitFinder">Funkcia (delegát), ktorá nájde optimálny split point pre daný segment.</param>
        /// <returns>Aktualizovaný kontext s finálnymi rekurzívne nájdenými cut-pointmi.</returns>
        public static DiscretizationContext RecursiveBinning(DiscretizationContext context, OptimalSplitFinder splitFinder)
        {
            Console.WriteLine("    [GeneralRecursiveStep] Spúšťam všeobecné rekurzívne binovanie.");

            context.CutPoints.Clear();

            // Pripravíme si DataRow objekty a ich numerické hodnoty pre celý atribút na začiatok rekurzie
            List<DataRow> fullSegmentDataRows = context.OriginalDataSet.Rows
                .Where(r => r.Attributes.ContainsKey(context.AttributeName) &&
                            (r.Attributes[context.AttributeName] is double || r.Attributes[context.AttributeName] is int || r.Attributes[context.AttributeName] is float))
                .ToList();

            List<double> fullSegmentNumericValues = fullSegmentDataRows
                .Select(r => Convert.ToDouble(r.Attributes[context.AttributeName]))
                .OrderBy(x => x)
                .ToList();

            // Interná rekurzívna funkcia, ktorá sa bude volať
            FindSplitsRecursive(context, splitFinder, fullSegmentNumericValues, fullSegmentDataRows);

            context.CutPoints = context.CutPoints.Distinct().OrderBy(c => c).ToList();

            Console.WriteLine($"    Všeobecné rekurzívne binovanie dokončené. Vygenerovaných {context.CutPoints.Count} cut-points.");
            return context;
        }

        /// <summary>
        /// Rekurzívna pomocná funkcia, ktorá aplikuje stratégiu hľadania splitov.
        /// </summary>
        /// <param name="context">DiscretizationContext.</param>
        /// <param name="splitFinder">Funkcia (delegát), ktorá nájde optimálny split.</param>
        /// <param name="segmentNumericValues">Numerické hodnoty aktuálneho segmentu.</param>
        /// <param name="segmentDataRows">Originálne DataRow objekty patriace do segmentu.</param>
        private static void FindSplitsRecursive(
            DiscretizationContext context,
            OptimalSplitFinder splitFinder,
            List<double> segmentNumericValues,
            List<DataRow> segmentDataRows)
        {
            // Podmienky zastavenia rekurzie
            if (segmentNumericValues.Count < 2) // Potrebujeme aspoň dve hodnoty pre potenciálny split
            {
                return;
            }

            // Zavoláme delegáta, ktorý nájde najlepší split pre tento segment
            double? bestSplitPoint = splitFinder(context, segmentNumericValues, segmentDataRows);

            if (bestSplitPoint.HasValue)
            {
                context.CutPoints.Add(bestSplitPoint.Value);
                Console.WriteLine($"      [RecursiveStep] Náhľad: Nájdený split: {bestSplitPoint.Value:F2}");

                // Rozdelenie segmentu na ľavý a pravý na základe nájdeného split pointu
                var leftSegmentDataRows = new List<DataRow>();
                var rightSegmentDataRows = new List<DataRow>();

                foreach (var row in segmentDataRows)
                {
                    double value = Convert.ToDouble(row.Attributes[context.AttributeName]);
                    if (value <= bestSplitPoint.Value)
                    {
                        leftSegmentDataRows.Add(row);
                    }
                    else
                    {
                        rightSegmentDataRows.Add(row);
                    }
                }

                // Extrahovanie a zoradenie numerických hodnôt pre nové segmenty
                List<double> leftSegmentNumericValues = leftSegmentDataRows
                    .Select(r => Convert.ToDouble(r.Attributes[context.AttributeName]))
                    .OrderBy(x => x)
                    .ToList();
                List<double> rightSegmentNumericValues = rightSegmentDataRows
                    .Select(r => Convert.ToDouble(r.Attributes[context.AttributeName]))
                    .OrderBy(x => x)
                    .ToList();

                // Rekurzívne volania pre oba nové segmenty
                FindSplitsRecursive(context, splitFinder, leftSegmentNumericValues, leftSegmentDataRows);
                FindSplitsRecursive(context, splitFinder, rightSegmentNumericValues, rightSegmentDataRows);
            }
            // Ak bestSplitPoint nemá hodnotu (je null), znamená to, že sa segment už nedelí (napr. nízky informačný zisk alebo všetky hodnoty sú rovnaké)
        }
    }
}