using System;
using System.Collections.Generic;
using System.Linq;
using DiscretizationFramework.Data.DataModels;
using DiscretizationFramework.Discretization.Core;
using DiscretizationFramework.Discretization.Steps; // Pre CommonSteps

namespace DiscretizationFramework.Discretization.Strategies
{
    /// <summary>
    /// Obsahuje konkrétne implementácie logiky Informačnej Hustoty pre použitie
    /// s GeneralRecursiveStep.
    /// </summary>
    public static class InformationDensityStrategy
    {
        // --- Pomocné funkcie pre výpočty informačnej hustoty (pre obe verzie) ---

        /// <summary>
        /// Vypočíta informačnú hustotu podľa vzorca Popela (2003), vzorec (3).
        /// H(V) / log2(|V|+1)
        /// </summary>
        private static double CalculateInformationDensity(double entropy, int totalDistinctValues)
        {
            if (totalDistinctValues <= 0) return 0.0;
            return entropy / Math.Log(totalDistinctValues + 1, 2);
        }

        /// <summary>
        /// Vypočíta podmienenú informačnú hustotu.
        /// Zovšeobecnená funkcia pre supervised/unsupervised verziu.
        /// </summary>
        private static double CalculateConditionalInformationDensity(
            List<DataRow> data,
            string attributeName,
            double cutPoint,
            double minVal,
            double maxVal,
            bool isSupervised,
            string? targetAttributeName = null) // targetAttributeName len pre supervised
        {
            var leftPartition = new List<DataRow>();
            var rightPartition = new List<DataRow>();

            foreach (var row in data)
            {
                if (row.Attributes.TryGetValue(attributeName, out object? valueObj))
                {
                    double? value = CommonSteps.TryConvertToDouble(valueObj);
                    if (value.HasValue)
                    {
                        if (value.Value < cutPoint)
                        {
                            leftPartition.Add(row);
                        }
                        else
                        {
                            rightPartition.Add(row);
                        }
                    }
                }
            }

            var delta_T = maxVal - minVal;
            if (delta_T <= 0) return 0;

            var p1 = (cutPoint - minVal) / delta_T;
            var p2 = (maxVal - cutPoint) / delta_T;

            double density1 = 0;
            if (leftPartition.Any())
            {
                var values1 = leftPartition.Select(r => CommonSteps.TryConvertToDouble(r.Attributes[attributeName]))
                                           .Where(v => v.HasValue)
                                           .Select(v => v.Value)
                                           .ToList();
                var distinctValues1 = values1.Distinct().Count();

                double entropy1;
                if (isSupervised)
                {
                    entropy1 = CommonSteps.CalculateClassEntropy(leftPartition.Select(r => r.Target).ToList());
                }
                else
                {
                    entropy1 = CommonSteps.CalculateValueEntropy(values1);
                }
                density1 = CalculateInformationDensity(entropy1, distinctValues1);
            }

            double density2 = 0;
            if (rightPartition.Any())
            {
                var values2 = rightPartition.Select(r => CommonSteps.TryConvertToDouble(r.Attributes[attributeName]))
                                            .Where(v => v.HasValue)
                                            .Select(v => v.Value)
                                            .ToList();
                var distinctValues2 = values2.Distinct().Count();

                double entropy2;
                if (isSupervised)
                {
                    entropy2 = CommonSteps.CalculateClassEntropy(rightPartition.Select(r => r.Target).ToList());
                }
                else
                {
                    entropy2 = CommonSteps.CalculateValueEntropy(values2);
                }
                density2 = CalculateInformationDensity(entropy2, distinctValues2);
            }
            
            return p1 * density1 + p2 * density2;
        }

        // --- Logika pre SUPERVISOVANÚ Informačnú hustotu ---

        /// <summary>
        /// Logika delenia pre Supervised Information Density Discretization.
        /// </summary>
        public static (double? bestCutPoint, bool shouldSplit) SupervisedSplitCriterion(
            List<DataRow> currentRows,
            string attributeName,
            double currentMin,
            double currentMax,
            HashSet<double> allFoundCutPoints,
            Dictionary<string, object> parameters)
        {
            // Pár základných kontrol
            if (!currentRows.Any() || currentMax <= currentMin) return (null, false);

            var numericDataPoints = currentRows
                .Select(r => new
                {
                    Value = CommonSteps.TryConvertToDouble(r.Attributes.GetValueOrDefault(attributeName)),
                    Target = r.Target
                })
                .Where(x => x.Value.HasValue)
                .Select(x => new { Value = x.Value.Value, x.Target })
                .OrderBy(x => x.Value)
                .ToList();

            if (numericDataPoints.Count < 2) return (null, false);

            double bestCutPoint = double.NaN;
            double minConditionalInformationDensity = double.MaxValue;

            var distinctSortedValues = numericDataPoints.Select(x => x.Value).Distinct().OrderBy(v => v).ToList();

            if (distinctSortedValues.Count < 2) return (null, false);

            double initialEntropy = CommonSteps.CalculateClassEntropy(numericDataPoints.Select(r => r.Target).ToList());
            int totalDistinctOriginalValues = distinctSortedValues.Count;
            double initialInformationDensity = CalculateInformationDensity(initialEntropy, totalDistinctOriginalValues);

            for (int i = 0; i < distinctSortedValues.Count - 1; i++)
            {
                double potentialCutPoint = (distinctSortedValues[i] + distinctSortedValues[i + 1]) / 2.0;

                double conditionalInformationDensity = CalculateConditionalInformationDensity(
                    currentRows, attributeName, potentialCutPoint, currentMin, currentMax, isSupervised: true);

                if (conditionalInformationDensity < minConditionalInformationDensity)
                {
                    minConditionalInformationDensity = conditionalInformationDensity;
                    bestCutPoint = potentialCutPoint;
                }
            }
            
            // Podmienka delenia: Ak je nájdený validný cut-point a podmienená hustota je nižšia ako počiatočná
            bool shouldSplit = !double.IsNaN(bestCutPoint) && minConditionalInformationDensity < initialInformationDensity;

            if (shouldSplit)
            {
                 Console.WriteLine($"    Nájdený cut-point (S): {bestCutPoint:F2} (ID_cond: {minConditionalInformationDensity:F4}, Initial ID: {initialInformationDensity:F4})");
            }
            else
            {
                 // Console.WriteLine($"    SupervisedSplitCriterion: Žiadne zlepšenie v rozsahu [{currentMin:F2}-{currentMax:F2}].");
            }

            return (shouldSplit ? bestCutPoint : null, shouldSplit);
        }

        // --- Logika pre UNSUPERVISOVANÚ Informačnú hustotu ---

        /// <summary>
        /// Logika delenia pre Unsupervised Information Density Discretization.
        /// </summary>
        public static (double? bestCutPoint, bool shouldSplit) UnsupervisedSplitCriterion(
            List<DataRow> currentRows,
            string attributeName,
            double currentMin,
            double currentMax,
            HashSet<double> allFoundCutPoints,
            Dictionary<string, object> parameters)
        {
            if (!currentRows.Any() || currentMax <= currentMin) return (null, false);

            var numericDataPoints = currentRows
                .Select(r => CommonSteps.TryConvertToDouble(r.Attributes.GetValueOrDefault(attributeName)))
                .Where(x => x.HasValue)
                .Select(x => x.Value)
                .OrderBy(x => x)
                .ToList();

            if (numericDataPoints.Count < 2) return (null, false);

            double bestCutPoint = double.NaN;
            double minConditionalInformationDensity = double.MaxValue;

            var distinctSortedValues = numericDataPoints.Distinct().OrderBy(v => v).ToList();

            if (distinctSortedValues.Count < 2) return (null, false);

            double initialEntropy = CommonSteps.CalculateValueEntropy(numericDataPoints);
            int totalDistinctOriginalValues = distinctSortedValues.Count;
            double initialInformationDensity = CalculateInformationDensity(initialEntropy, totalDistinctOriginalValues);

            for (int i = 0; i < distinctSortedValues.Count - 1; i++)
            {
                double potentialCutPoint = (distinctSortedValues[i] + distinctSortedValues[i + 1]) / 2.0;

                double conditionalInformationDensity = CalculateConditionalInformationDensity(
                    currentRows, attributeName, potentialCutPoint, currentMin, currentMax, isSupervised: false);

                if (conditionalInformationDensity < minConditionalInformationDensity)
                {
                    minConditionalInformationDensity = conditionalInformationDensity;
                    bestCutPoint = potentialCutPoint;
                }
            }

            bool shouldSplit = !double.IsNaN(bestCutPoint) && minConditionalInformationDensity < initialInformationDensity;

            if (shouldSplit)
            {
                 Console.WriteLine($"    Nájdený cut-point (U): {bestCutPoint:F2} (ID_cond: {minConditionalInformationDensity:F4}, Initial ID: {initialInformationDensity:F4})");
            }
            else
            {
                // Console.WriteLine($"    UnsupervisedSplitCriterion: Žiadne zlepšenie v rozsahu [{currentMin:F2}-{currentMax:F2}].");
            }

            return (shouldSplit ? bestCutPoint : null, shouldSplit);
        }
    }
}