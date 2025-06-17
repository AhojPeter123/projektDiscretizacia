// DiscretizationFramework/Discretization/Steps/GeneralIterativeStep.cs
using System;
using System.Collections.Generic;
using System.Linq;
using DiscretizationFramework.Data.DataModels;
using DiscretizationFramework.Discretization.Core;

namespace DiscretizationFramework.Discretization.Steps
{
    /// <summary>
    /// Obsahuje všeobecné kroky pre iteratívne diskretizačné algoritmy.
    /// Tieto kroky sú vysoko abstraktné a prijímajú delegátov pre konkrétne stratégie.
    /// </summary>
    public static class GeneralIterativeStep
    {
        /// <summary>
        /// Všeobecný iteratívny krok, ktorý vykonáva cyklus a v každej iterácii volá
        /// špecifickú funkciu (CutPointGenerator) na generovanie rezových bodov.
        /// Vyžaduje parameter "NumberOfBins" v kontexte.
        /// </summary>
        /// <param name="context">Aktuálny kontext diskretizácie.</param>
        /// <param name="generator">Funkcia (delegát), ktorá definuje, ako sa generuje cut-point v každej iterácii.</param>
        /// <returns>Aktualizovaný kontext s vygenerovanými cut-points.</returns>
        public static DiscretizationContext IterativeBinning(DiscretizationContext context, CutPointGenerator generator)
        {
            Console.WriteLine("    [GeneralIterativeStep] Spúšťam všeobecné iteratívne binovanie.");

            if (!context.Parameters.TryGetValue("NumberOfBins", out object numBinsObj) || !(numBinsObj is int numberOfBins) || numberOfBins <= 0)
            {
                throw new ArgumentException("Parameter 'NumberOfBins' (int > 0) musí byť nastavený v kontexte pre iteratívne binovanie.");
            }

            if (!context.NumericValues.Any())
            {
                Console.WriteLine("    Žiadne numerické hodnoty. Cut-points prázdne.");
                context.CutPoints.Clear();
                return context;
            }

            context.CutPoints.Clear();

            // Pripravíme si DataRow objekty patriace k atribútu pre prípad, že ich generátor potrebuje (napr. pre cieľovú premennú)
            var relevantDataRows = context.OriginalDataSet.Rows
                .Where(r => r.Attributes.ContainsKey(context.AttributeName) &&
                            (r.Attributes[context.AttributeName] is double || r.Attributes[context.AttributeName] is int || r.Attributes[context.AttributeName] is float))
                .ToList();

            // Táto slučka je teraz VŠEOBECNÁ pre všetky iteratívne algoritmy založené na numberOfBins.
            for (int i = 0; i < numberOfBins - 1; i++) // Iterujeme pre každý cut-point
            {
                // Voláme špecifickú generátorovú funkciu
                List<double> generatedPoints = generator(context, i, context.NumericValues, relevantDataRows);
                if (generatedPoints != null)
                {
                    context.CutPoints.AddRange(generatedPoints);
                }
            }

            context.CutPoints = context.CutPoints.Distinct().OrderBy(c => c).ToList();

            Console.WriteLine($"    Všeobecné iteratívne binovanie dokončené. Vygenerovaných {context.CutPoints.Count} cut-points.");
            return context;
        }
    }
}