// DiscretizationFramework/Discretization/Steps/CommonSteps.cs
using System;
using System.Linq;
using DiscretizationFramework.Discretization.Core;

namespace DiscretizationFramework.Discretization.Steps
{
    /// <summary>
    /// Obsahuje statické metódy, ktoré predstavujú spoločné kroky použiteľné vo viacerých diskretizačných algoritmoch.
    /// Každá metóda má podpis DiscretizationStep.
    /// </summary>
    public static class CommonSteps
    {
        /// <summary>
        /// Prípravný krok, ktorý zabezpečuje, že numerické hodnoty sú v kontexte správne pripravené.
        /// Kontext.NumericValues by už mali byť zoradené z konštruktora DiscretizationContext.
        /// </summary>
        /// <param name="context">Aktuálny kontext diskretizácie.</param>
        /// <returns>Aktualizovaný kontext diskretizácie.</returns>
        public static DiscretizationContext PrepareNumericValues(DiscretizationContext context)
        {
            Console.WriteLine("    [CommonStep] Pripravujem numerické hodnoty (zoradenie je už hotové).");
            // Môžete tu pridať ďalšie všeobecné spracovanie, napr. odstránenie duplicitných hodnôt,
            // ak je to pre algoritmus žiaduce:
            // context.NumericValues = context.NumericValues.Distinct().ToList();
            return context;
        }
    }
}