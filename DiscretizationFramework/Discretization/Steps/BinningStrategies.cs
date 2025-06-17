// DiscretizationFramework/Discretization/Steps/BinningStrategies.cs
using System;
using System.Collections.Generic;
using System.Linq;
using DiscretizationFramework.Data.DataModels;
using DiscretizationFramework.Discretization.Core;

namespace DiscretizationFramework.Discretization.Steps
{
    /// <summary>
    /// Obsahuje konkrétne implementácie stratégií pre generovanie rezových bodov
    /// (pre iteratívne algoritmy) a vyhľadávanie optimálnych splitov (pre rekurzívne algoritmy).
    /// </summary>
    public static class BinningStrategies
    {
        // --- Stratégie pre iteratívne algoritmy (implementujú CutPointGenerator) ---

        /// <summary>
        /// Generátor cut-pointov pre Equal-Width Binning.
        /// </summary>
        public static List<double> EqualWidthGenerator(
            DiscretizationContext context,
            int iterationIndex,
            List<double> segmentNumericValues,
            List<DataRow> segmentDataRows) // segmentDataRows sú tu pre konzistentnosť podpisu, ale Equal-Width ich nepoužíva
        {
            double min = segmentNumericValues.Min();
            double max = segmentNumericValues.Max();
            int numberOfBins = (int)context.Parameters["NumberOfBins"]; // Vieme, že parameter je nastavený

            double binWidth = (max - min) / numberOfBins;

            // iterationIndex (0-based) pre 0 až numberOfBins-2
            // Cut-point je (iterationIndex + 1)-tá hranica binu.
            double cutPoint = min + (iterationIndex + 1) * binWidth;
            Console.WriteLine($"      [Strategy] Equal-Width: Iterácia {iterationIndex}, Cut-point: {cutPoint:F2}");

            // Zabezpečenie, aby cut-point nebol presne rovný max hodnote v segmente (môže sa stať kvôli floating point presnosti)
            if (cutPoint >= max && iterationIndex < numberOfBins - 1)
            {
                // Ak cut-point dosiahol maximum predčasne, jemne ho posunieme pod max,
                // alebo ak je to posledná iterácia, nemusíme robiť nič (posledný bin ide až do max).
                cutPoint = max - double.Epsilon; // Malé číslo, aby bol cut-point stále platný
            }
            return new List<double> { cutPoint };
        }

        /// <summary>
        /// Generátor cut-pointov pre Equal-Frequency Binning.
        /// </summary>
        public static List<double> EqualFrequencyGenerator(
            DiscretizationContext context,
            int iterationIndex,
            List<double> segmentNumericValues,
            List<DataRow> segmentDataRows) // segmentDataRows sú tu pre konzistentnosť podpisu, ale Equal-Frequency ich nepoužíva
        {
            int numberOfBins = (int)context.Parameters["NumberOfBins"];
            int totalValues = segmentNumericValues.Count;
            int valuesPerBin = totalValues / numberOfBins;

            // iterationIndex (0-based)
            int cutPointDataIndex = ((iterationIndex + 1) * valuesPerBin) - 1;

            // Ošetrenie okrajových prípadov, aby index nebol mimo rozsahu
            if (cutPointDataIndex < 0) cutPointDataIndex = 0;
            // Zabezpečíme, aby sme nešli za posledný index, ktorý by mohol byť cut-pointom
            if (cutPointDataIndex >= totalValues - 1) cutPointDataIndex = totalValues - 2; // -2, pretože posledná hodnota nemôže byť cut-pointom (potrebujeme aspoň 1 hodnotu vpravo)

            double cutPoint = segmentNumericValues[cutPointDataIndex];
            Console.WriteLine($"      [Strategy] Equal-Frequency: Iterácia {iterationIndex}, Cut-point: {cutPoint:F2}");

            // Malé úpravy pre robustnosť:
            // Ak je cutPoint rovnaký ako ďalšia hodnota, posunieme ho, aby sa predišlo prázdnym binom.
            if (cutPointDataIndex + 1 < totalValues && cutPoint == segmentNumericValues[cutPointDataIndex + 1])
            {
                // Nájdeme prvú hodnotu, ktorá je väčšia ako aktuálny cutPoint
                double? nextDistinctValue = segmentNumericValues.Skip(cutPointDataIndex + 1).FirstOrDefault(v => v > cutPoint);
                if (nextDistinctValue.HasValue)
                {
                    cutPoint = nextDistinctValue.Value;
                } else {
                    // Ak všetky zvyšné hodnoty sú rovnaké, potom cut-point na tomto mieste nemá zmysel
                    // a môže byť ošetrené vyššou úrovňou (Discretizer by potom cut-point filtroval ako duplicitný, ak by bol).
                    // Pre MDLP je toto dôležité, pre jednoduché binovanie menej kritické.
                }
            }


            return new List<double> { cutPoint };
        }

        // --- Stratégie pre rekurzívne algoritmy (implementujú OptimalSplitFinder) ---

        /// <summary>
        /// Simulácia stratégie pre vyhľadávanie optimálneho splitu (napr. pre MDLP).
        /// V reálnej implementácii by toto zahŕňalo výpočet entropie a informačného zisku.
        /// </summary>
        public static double? SimulatedMdlpSplitFinder(
            DiscretizationContext context,
            List<double> segmentNumericValues,
            List<DataRow> segmentDataRows)
        {
            if (segmentNumericValues.Count < 2)
            {
                return null; // Nedá sa deliť
            }

            // Simulujeme nájdenie "optimálneho" splitu.
            // V reálnom MDLP by ste iterovali cez všetky možné cut-pointy (stredy medzi distinct hodnotami)
            // a počítali informačný zisk, až kým by nespĺňal kritérium zastavenia (MDLP criterion).

            // Pre jednoduchosť vezmeme len jeden zo stredov.
            // Získame unikátne zoradené hodnoty pre presnejšie stredy
            var distinctValues = segmentNumericValues.Distinct().OrderBy(v => v).ToList();

            if (distinctValues.Count < 2)
            {
                return null; // Všetky hodnoty sú rovnaké, nedá sa deliť.
            }

            // Simulujeme, že "najlepší" split je zhruba v strede segmentu,
            // ale len ak sú aspoň 3 unikátne hodnoty, aby sme mali 2 intervaly.
            if (distinctValues.Count > 1)
            {
                // Vezmeme stredný bod ako potenciálny split (napr. priemer prvej a poslednej hodnoty)
                double potentialSplit = distinctValues[0] + (distinctValues[distinctValues.Count - 1] - distinctValues[0]) / 2.0;

                // Aby bol split realistický, mal by byť medzi dvoma dátovými bodmi.
                // Nájdeme najbližšie dve odlišné hodnoty, medzi ktorými leží potentialSplit.
                double lowerBound = distinctValues.LastOrDefault(v => v < potentialSplit);
                double upperBound = distinctValues.FirstOrDefault(v => v > potentialSplit);

                if (lowerBound == 0 && upperBound == 0) // Ak sa nenájdu vhodné hranice
                {
                     // Nájdi stred medzi prvou a druhou unikátnou hodnotou
                    if (distinctValues.Count >= 2)
                    {
                        return (distinctValues[0] + distinctValues[1]) / 2.0;
                    }
                    return null;
                }

                if (lowerBound != 0 && upperBound != 0)
                {
                    return (lowerBound + upperBound) / 2.0;
                }
                else if (lowerBound != 0) // Ak existuje len lowerBound (potentialSplit je príliš vysoko)
                {
                    return (lowerBound + distinctValues.Last()) / 2.0;
                }
                else if (upperBound != 0) // Ak existuje len upperBound (potentialSplit je príliš nízko)
                {
                    return (distinctValues.First() + upperBound) / 2.0;
                }
                else
                {
                    return null; // Žiadny zmysluplný split
                }
            }

            // Kritériá zastavenia MDLP (napr. informačný zisk je príliš nízky, alebo segment je "čistý")
            // Na demonštračné účely simulujeme, že sa delí iba raz alebo dvakrát.
            if (context.CutPoints.Count > 2) // Simulované kritérium zastavenia pre prehľadnosť
            {
                Console.WriteLine("      [Strategy] MDLP: Simulácia: dosiahnutý limit splitov.");
                return null;
            }

            return null; // Ak sa nenájde žiadny "optimálny" split (v reálnom MDLP by to znamenalo nízky zisk)
        }


        // Funkcie pre výpočet entropie a informačného zisku (len pre kontext MDLP)
        /*
        private static double CalculateEntropy(IEnumerable<DataRow> segmentDataRows, string targetAttributeName)
        {
            var targetCounts = segmentDataRows.GroupBy(r => r.Target)
                                             .ToDictionary(g => g.Key, g => g.Count());
            double total = segmentDataRows.Count();
            double entropy = 0.0;

            foreach (var count in targetCounts.Values)
            {
                double p = count / total;
                entropy -= p * Math.Log(p, 2);
            }
            return entropy;
        }

        private static double CalculateInformationGain(double parentEntropy,
                                                      List<DataRow> leftSegment,
                                                      List<DataRow> rightSegment,
                                                      string targetAttributeName)
        {
            double total = leftSegment.Count + rightSegment.Count;
            double gain = parentEntropy -
                          ((leftSegment.Count / total) * CalculateEntropy(leftSegment, targetAttributeName)) -
                          ((rightSegment.Count / total) * CalculateEntropy(rightSegment, targetAttributeName));
            return gain;
        }

        private static double CalculateMdlpCriterion(double gain, double parentEntropy, int k, int k_prime, int N)
        {
            // MDLP formula: Gain > log2(N-1)/N + delta
            // kde delta = (k * entropy_parent - k_prime * entropy_children) / N
            // Toto je zjednodušená verzia. Skutočné kritérium je komplexnejšie.
            // Zistite si presnú formu kritéria, ak idete implementovať plné MDLP.
            return gain - (Math.Log(N - 1, 2) / N); // Zjednodušená verzia
        }
        */
    }
}