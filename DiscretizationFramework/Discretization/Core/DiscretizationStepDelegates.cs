// DiscretizationFramework/Discretization/Core/DiscretizationStepDelegates.cs
using System;
using System.Collections.Generic;
using DiscretizationFramework.Data.DataModels; // Pre DataRow

namespace DiscretizationFramework.Discretization.Core
{
    /// <summary>
    /// Deklarácia delegáta, ktorý reprezentuje jeden krok v diskretizačnom algoritme.
    /// Každý krok prijíma DiscretizationContext a vracia modifikovaný DiscretizationContext.
    /// </summary>
    public delegate DiscretizationContext DiscretizationStep(DiscretizationContext context);

    /// <summary>
    /// Delegát pre funkciu, ktorá generuje zoznam rezových bodov (cut-points)
    /// na základe aktuálneho DiscretizationContextu a čísla aktuálnej iterácie/splitu.
    /// Táto funkcia bude volaná vnútri všeobecnej iteratívnej alebo rekurzívnej slučky.
    /// </summary>
    /// <param name="context">Aktuálny DiscretizationContext obsahujúci dáta a parametre.</param>
    /// <param name="iterationOrSplitIndex">Index aktuálnej iterácie (napr. pre N-tý bin) alebo index pre split.</param>
    /// <param name="segmentNumericValues">Numerické hodnoty aktuálneho segmentu dát.</param>
    /// <param name="segmentDataRows">Príslušné DataRow objekty pre segment (pre algoritmy ako MDLP).</param>
    /// <returns>Zoznam novogenerovaných cut-pointov pre túto iteráciu/split.</returns>
    public delegate List<double> CutPointGenerator(
        DiscretizationContext context,
        int iterationOrSplitIndex,
        List<double> segmentNumericValues,
        List<DataRow> segmentDataRows); // Pridané pre rekurzívne algoritmy (napr. MDLP)

    /// <summary>
    /// Delegát pre funkciu, ktorá nájde optimálny rezový bod (split point)
    /// pre daný segment dát v kontexte rekurzívneho algoritmu.
    /// Vráti navrhovaný cut-point. Null, ak sa nenájde žiadny platný split alebo ak nie je potrebný.
    /// </summary>
    /// <param name="context">DiscretizationContext.</param>
    /// <param name="segmentNumericValues">Numerické hodnoty aktuálneho segmentu dát.</param>
    /// <param name="segmentDataRows">Príslušné DataRow objekty pre segment (pre entropiu a cieľovú premennú).</param>
    /// <returns>Nullable double reprezentujúci optimálny cut-point. Null, ak sa nenájde žiadny platný split.</returns>
    public delegate double? OptimalSplitFinder(
        DiscretizationContext context,
        List<double> segmentNumericValues,
        List<DataRow> segmentDataRows);
}