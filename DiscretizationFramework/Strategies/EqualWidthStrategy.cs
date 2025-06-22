using System;
using System.Collections.Generic;
using System.Linq;
using DiscretizationFramework.Data.DataModels;
using DiscretizationFramework.Discretization.Core;
using DiscretizationFramework.Discretization.Steps; // Pre CommonSteps

namespace DiscretizationFramework.Discretization.Strategies
{
    /// <summary>
    /// Implementuje logiku pre Equal-Width diskretizáciu.
    /// </summary>
    public static class EqualWidthStrategy
    {
        /// <summary>
        /// Logika pre Equal-Width binovanie. Vyžaduje parameter 'numBins' alebo použije 'optimalNumBins' z kontextu.
        /// </summary>
        public static List<double> BinningLogic(DiscretizationContext context)
        {
            int numBins;
            // Priorita: numBins z explicitných parametrov, potom optimalNumBins, inak predvolená hodnota.
            if (context.Parameters.TryGetValue("numBins", out object? explicitNumBinsObj) && explicitNumBinsObj is int explicitNumBins && explicitNumBins > 1)
            {
                numBins = explicitNumBins;
                Console.WriteLine($"  Používam explicitný numBins: {numBins}.");
            }
            else if (context.Parameters.TryGetValue("optimalNumBins", out object? optimalNumBinsObj) && optimalNumBinsObj is int optimalNumBins && optimalNumBins > 1)
            {
                numBins = optimalNumBins;
                Console.WriteLine($"  Používam optimálny numBins z kontextu: {numBins}.");
            }
            else
            {
                numBins = 5; // Predvolená hodnota, ak nie je zadaná ani vypočítaná
                Console.WriteLine($"  Žiadny numBins nebol zadaný/vypočítaný. Používam predvolenú hodnotu: {numBins}.");
            }

            if (!context.NumericValues.Any())
            {
                return new List<double>();
            }

            var minValue = context.NumericValues.Min();
            var maxValue = context.NumericValues.Max();

            // Ak je rozsah hodnôt príliš malý alebo nulový, nemôžeme vytvoriť viac ako 1 bin
            if (maxValue - minValue <= double.Epsilon)
            {
                return new List<double>();
            }

            // Zabezpečíme, že počet binov nepresiahne počet unikátnych hodnôt - 1
            // (inak by sme mali duplicitné cut-pointy alebo prázdne biny, ak sú hodnoty diskrétne a opakované)
            var distinctCount = context.NumericValues.Distinct().Count();
            if (numBins > distinctCount)
            {
                numBins = distinctCount;
            }
            if (numBins <= 1 && distinctCount > 1) numBins = 2; // Minimálne 2 biny, ak je viac ako 1 unikátna hodnota
            if (distinctCount <= 1) return new List<double>(); // Ak len jedna unikátna hodnota, žiadne cut-pointy

            var binWidth = (maxValue - minValue) / numBins;
            var cutPoints = new List<double>();

            for (int i = 1; i < numBins; i++)
            {
                double cutPoint = minValue + i * binWidth;
                // Zabezpečíme, aby cut-point nebol presne rovný maxValue, aby nevznikali problémy s posledným intervalom
                if (cutPoint < maxValue)
                {
                    cutPoints.Add(cutPoint);
                }
            }

            return cutPoints.Distinct().OrderBy(cp => cp).ToList(); // Odstráni duplicity a zoradí
        }
    }
}