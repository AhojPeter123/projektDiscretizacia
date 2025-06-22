using System;
using System.Collections.Generic;
using System.Linq;
using DiscretizationFramework.Data.DataModels;
using DiscretizationFramework.Discretization.Core;
using DiscretizationFramework.Discretization.Steps; // Pre CommonSteps

namespace DiscretizationFramework.Discretization.Strategies
{
    /// <summary>
    /// Implementuje logiku pre Equal-Frequency (Quantile) diskretizáciu.
    /// </summary>
    public static class EqualFrequencyStrategy
    {
        /// <summary>
        /// Logika pre Equal-Frequency (Quantile) binovanie. Vyžaduje parameter 'numBins' alebo použije 'optimalNumBins' z kontextu.
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

            // Získaj zoradené unikátne hodnoty. Equal Frequency by mal brať do úvahy aj duplicity,
            // ale pre cut-pointy je efektívnejšie pracovať s unikátnymi a potom ich zoradiť.
            var sortedValues = context.NumericValues.OrderBy(v => v).ToList();

            if (sortedValues.Count < numBins)
            {
                // Ak je menej unikátnych hodnôt ako požadovaných binov,
                // použijeme všetky unikátne hodnoty ako cut-pointy (okrem poslednej, aby biny neboli prázdne)
                // Alebo radšej len 1 cut-point pre 2 unikátne hodnoty (delenie na 2 biny)
                if (sortedValues.Distinct().Count() < 2) return new List<double>(); // Ak len 1 unikátna hodnota
                return sortedValues.Distinct().OrderBy(v => v).Take(sortedValues.Distinct().Count() - 1).ToList();
            }

            var cutPoints = new List<double>();
            // Počet prvkov v každom binu, na základe celkového počtu dátových bodov
            var elementsPerBin = (double)sortedValues.Count / numBins;

            for (int i = 1; i < numBins; i++)
            {
                // Index v zoradenom zozname, ktorý zodpovedá i-temu kvantilu
                var index = (int)Math.Floor(i * elementsPerBin);
                if (index > 0 && index < sortedValues.Count)
                {
                    // Cut-point je priemerná hodnota medzi posledným prvkom pred kvantilom
                    // a prvým prvkom po kvantile. Toto je typické pre quantile diskretizáciu.
                    double cutPoint = (sortedValues[index - 1] + sortedValues[index]) / 2.0;
                    cutPoints.Add(cutPoint);
                }
            }

            return cutPoints.Distinct().OrderBy(cp => cp).ToList(); // Odstráni duplicity a zoradí
        }
    }
}