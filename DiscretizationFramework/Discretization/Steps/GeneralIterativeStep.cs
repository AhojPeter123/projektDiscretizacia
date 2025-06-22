using System;
using System.Collections.Generic;
using DiscretizationFramework.Data.DataModels;
using DiscretizationFramework.Discretization.Core;

namespace DiscretizationFramework.Discretization.Steps
{
    /// <summary>
    /// Reprezentuje generický iteratívny krok diskretizácie.
    /// Obmedzuje opakujúcu sa logiku iterácie a umožňuje injektovať špecifickú binovaciu logiku.
    /// </summary>
    public class GeneralIterativeStep
    {
        // Delegát pre špecifickú logiku binovania, ktorá sa vykoná v tomto iteratívnom kroku.
        // Kontext je vstup, zoznam cut-pointov je výstup.
        public delegate List<double> IterativeBinningLogic(DiscretizationContext context);

        private readonly string _stepName;
        private readonly IterativeBinningLogic _binningLogic;

        /// <summary>
        /// Inicializuje nový generický iteratívny krok diskretizácie.
        /// </summary>
        /// <param name="stepName">Názov tohto kroku (pre logovanie).</param>
        /// <param name="binningLogic">Funkcia implementujúca špecifickú binovaciu logiku.</param>
        public GeneralIterativeStep(string stepName, IterativeBinningLogic binningLogic)
        {
            _stepName = stepName ?? throw new ArgumentNullException(nameof(stepName));
            _binningLogic = binningLogic ?? throw new ArgumentNullException(nameof(binningLogic));
        }

        /// <summary>
        /// Vykoná iteratívny diskretizačný krok.
        /// </summary>
        /// <param name="context">DiscretizationContext obsahujúci dáta a parametre.</param>
        /// <returns>Modifikovaný DiscretizationContext s vypočítanými cut-pointami.</returns>
        public DiscretizationContext Execute(DiscretizationContext context)
        {
            Console.WriteLine($"  Spúšťam iteratívny krok: {_stepName}.");

            if (!context.NumericValues.Any())
            {
                Console.WriteLine($"  Upozornenie: Žiadne numerické hodnoty pre {_stepName}. Preskakujem.");
                return context;
            }

            try
            {
                // Zavolá špecifickú logiku binovania
                context.CutPoints = _binningLogic(context);
                context.CutPoints = context.CutPoints.OrderBy(cp => cp).ToList(); // Zabezpečí zoradenie
                Console.WriteLine($"  * {_stepName} generoval {context.CutPoints.Count} cut-pointov.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Chyba počas vykonávania iteratívneho kroku {_stepName}: {ex.Message}");
                // Rozhodni sa, či tu hodiť výnimku, alebo vrátiť kontext v chybovom stave
                throw;
            }

            return context;
        }

        // Implicitná konverzia na DiscretizationStep delegáta
        public static implicit operator DiscretizationStep(GeneralIterativeStep step)
        {
            return step.Execute;
        }
    }
}