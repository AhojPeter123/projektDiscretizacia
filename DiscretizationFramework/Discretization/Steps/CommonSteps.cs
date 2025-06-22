using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DiscretizationFramework.Data.DataModels;
using DiscretizationFramework.Discretization.Core;

namespace DiscretizationFramework.Discretization.Steps
{
    /// <summary>
    /// Obsahuje spoločné pomocné metódy pre diskretizačné kroky.
    /// </summary>
    public static class CommonSteps
    {
        /// <summary>
        /// Konvertuje všetky relevantné atribúty v datasete na ich inferované numerické typy,
        /// ak sú uložené ako string a mali by byť double.
        /// Toto by mal byť prvý krok diskretizačného pipeline, ak sú hodnoty v DataRow.Attributes stringy.
        /// </summary>
        /// <param name="context">DiscretizationContext obsahujúci dataset a informácie o atribútoch.</param>
        /// <returns>Modifikovaný DiscretizationContext s aktualizovanými NumericValues a potenciálne upravenými DataRow.Attributes.</returns>
        public static DiscretizationContext ConvertAttributesToNumeric(DiscretizationContext context)
        {
            var attributeName = context.AttributeName;
            var targetAttributeName = context.OriginalDataSet.TargetAttributeName; // Predpokladáme, že je k dispozícii

            // Vyčistíme NumericValues, pretože ich teraz naplníme konvertovanými hodnotami
            context.NumericValues.Clear();

            var updatedRows = new List<DataRow>();

            foreach (var row in context.OriginalDataSet.Rows)
            {
                var newAttributes = new Dictionary<string, object>(row.Attributes);
                
                // Pre aktuálny diskretizovaný atribút
                if (row.Attributes.TryGetValue(attributeName, out object? valueObj))
                {
                    double? numericValue = TryConvertToDouble(valueObj);
                    if (numericValue.HasValue)
                    {
                        newAttributes[attributeName] = numericValue.Value; // Ulož do novej mapy ako double
                        context.NumericValues.Add(numericValue.Value); // Naplň NumericValues v kontexte
                    }
                    else
                    {

                    }
                }
                
                updatedRows.Add(new DataRow(newAttributes, row.Target));
            }
            
            Console.WriteLine($"  * Konvertovaných {context.NumericValues.Count} numerických hodnôt pre atribút '{attributeName}'.");

            return context;
        }

        /// <summary>
        /// Pomocná metóda na robustnú konverziu objektu na double.
        /// </summary>
        /// <param name="valueObj">Objekt, ktorý sa má konvertovať.</param>
        /// <returns>Hodnota double, ak je konverzia úspešná; inak null.</returns>
        public static double? TryConvertToDouble(object? valueObj)
        {
            if (valueObj == null)
            {
                return null;
            }

            if (valueObj is double d) return d;
            if (valueObj is int i) return (double)i;
            if (valueObj is float f) return (double)f;
            if (valueObj is string s)
            {
                if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double resultInvariant)) return resultInvariant;
                if (double.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out double resultCurrent)) return resultCurrent;
                if (s.Contains(',') && double.TryParse(s.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double resultCommaReplaced)) return resultCommaReplaced;
            }

            return null; // Konverzia zlyhala
        }

        /// <summary>
        /// Vypočíta Shannonovu entropiu pre danú distribúciu pravdepodobností.
        /// </summary>
        private static double CalculateEntropy(IEnumerable<double> probabilities)
        {
            return -probabilities.Where(p => p > 0).Sum(p => p * Math.Log(p, 2));
        }

        /// <summary>
        /// Vypočíta entropiu distribúcie tried pre danú skupinu cieľových hodnôt.
        /// </summary>
        public static double CalculateClassEntropy(List<string> targetValues)
        {
            if (!targetValues.Any()) return 0.0;

            var totalCount = targetValues.Count;
            var classCounts = targetValues.GroupBy(tv => tv)
                                          .ToDictionary(g => g.Key, g => g.Count());

            var probabilities = classCounts.Values.Select(count => (double)count / totalCount);

            return CalculateEntropy(probabilities);
        }
        
        /// <summary>
        /// Vypočíta entropiu distribúcie NUMERICKÝCH hodnôt v danej skupine dát.
        /// Používa frekvenciu unikátnych hodnôt.
        /// </summary>
        public static double CalculateValueEntropy(List<double> values)
        {
            if (!values.Any()) return 0.0;

            var totalCount = values.Count;
            // Skupinujeme podľa hodnôt a počítame frekvencie
            var valueCounts = values.GroupBy(v => v)
                .ToDictionary(g => g.Key, g => g.Count());

            // Vypočítame pravdepodobnosti
            var probabilities = valueCounts.Values.Select(count => (double)count / totalCount);

            // Použijeme už existujúcu generickú CalculateEntropy metódu
            return CalculateEntropy(probabilities);
        }
        
        /// <summary>
        /// Krok diskretizácie, ktorý vypočíta optimálny počet binov a uloží ho do DiscretizationContext.Parameters.
        /// Používa Sturgesovo pravidlo: k = ceil(1 + log2(n))
        /// </summary>
        /// <param name="context">DiscretizationContext obsahujúci dáta.</param>
        /// <returns>Modifikovaný DiscretizationContext s pridaným parametrom "optimalNumBins".</returns>
        public static DiscretizationContext CalculateOptimalNumberOfBins(DiscretizationContext context)
        {
            Console.WriteLine("  Spúšťam krok: Výpočet optimálneho počtu binov (Sturgesovo pravidlo).");

            if (!context.NumericValues.Any())
            {
                Console.WriteLine("  Upozornenie: Žiadne numerické hodnoty na výpočet optimálneho počtu binov.");
                context.Parameters["optimalNumBins"] = 1; // Predvolené minimum, alebo 0
                return context;
            }

            int n = context.NumericValues.Count;
            int optimalBins = 0;

            if (n > 1) // Pre n=1 alebo 0 nemá log2 zmysel alebo by bolo 0
            {
                optimalBins = (int)Math.Ceiling(1 + Math.Log(n, 2));
            }
            else
            {
                optimalBins = 1; // Minimálne 1 bin pre jeden dátový bod
            }
            
            // Zabezpečíme, aby počet binov bol aspoň 2, ak je dostatok unikátnych hodnôt
            // A tiež aby nebol viac ako počet unikátnych hodnôt - 1
            var distinctCount = context.NumericValues.Distinct().Count();
            if (optimalBins > distinctCount -1 && distinctCount > 1) {
                optimalBins = distinctCount - 1; // Max cut-points = unikátne hodnoty - 1
            }
            if (optimalBins < 1) optimalBins = 1; // aspoň jeden bin

            context.Parameters["optimalNumBins"] = optimalBins;
            Console.WriteLine($"  * Optimálny počet binov pre atribút '{context.AttributeName}': {optimalBins} (podľa Sturgesovho pravidla pre {n} záznamov).");

            return context;
        }
        
        /// <summary>
        /// Krok diskretizácie, ktorý normalizuje numerické hodnoty atribútu na rozsah [0, 1] (Min-Max Normalizácia).
        /// Upozornenie: Táto normalizácia transformuje `NumericValues` v kontexte.
        /// Ak potrebujete pôvodné hodnoty po normalizácii, musíte si ich predtým uložiť.
        /// </summary>
        /// <param name="context">DiscretizationContext s NumericValues.</param>
        /// <returns>Modifikovaný DiscretizationContext s normalizovanými NumericValues.</returns>
        public static DiscretizationContext NormalizeNumericValuesMinMax(DiscretizationContext context)
        {
            Console.WriteLine("  Spúšťam krok: Min-Max Normalizácia numerických hodnôt.");

            if (!context.NumericValues.Any())
            {
                Console.WriteLine("  Upozornenie: Žiadne numerické hodnoty na normalizáciu.");
                return context;
            }

            double minValue = context.NumericValues.Min();
            double maxValue = context.NumericValues.Max();

            // Ak sú všetky hodnoty rovnaké, alebo rozsah je nulový, normalizácia nie je potrebná/možná.
            // Nastavíme všetky hodnoty na 0.5 (stred rozsahu 0-1) alebo ich ponecháme, ak sú už v rozsahu.
            if (Math.Abs(maxValue - minValue) < double.Epsilon)
            {
                Console.WriteLine("  Všetky numerické hodnoty sú rovnaké. Normalizácia preskočená (alebo nastavené na 0.5).");
                // Môžeme zmeniť všetky na 0.5, ak chceme, aby boli v normovanom rozsahu,
                // alebo ich ponechať, ak sú už 0 alebo 1.
                for (int i = 0; i < context.NumericValues.Count; i++)
                {
                    // Toto zabezpečí, že ak sú všetky hodnoty X, budú transformované na 0.5.
                    // Ak už boli normalizované inak, ponechá ich to.
                    if (context.NumericValues[i] == minValue) 
                    {
                        context.NumericValues[i] = 0.5;
                    }
                }
                return context;
            }

            var normalizedValues = new List<double>();
            foreach (var val in context.NumericValues)
            {
                normalizedValues.Add((val - minValue) / (maxValue - minValue));
            }

            context.NumericValues = normalizedValues;
            Console.WriteLine($"  * Hodnoty atribútu '{context.AttributeName}' boli normalizované do rozsahu [0, 1].");

            // POZNÁMKA: Normalizácia mení rozsah dát. Ak sa neskôr použijú cut-pointy,
            // ktoré boli vypočítané na normalizovaných dátach, budú tiež v rozsahu [0,1].
            // Pri aplikácii na pôvodné dáta (v GetBinLabel) je potrebné buď:
            // a) Pôvodné cut-pointy denormalizovať naspäť do pôvodného rozsahu.
            // b) Alebo normalizovať aj pôvodné hodnoty pred priradením do binov.
            // Súčasná implementácia Discretizer.GetBinLabel pracuje s pôvodnými hodnotami,
            // takže je potrebné denormalizovať cut-pointy PRED odovzdaním do DiscretizerResult.FinalCutPoints,
            // alebo v Discretizer.GetBinLabel normalizovať vstupnú hodnotu.
            // Aktuálna logika v Discretizer predpokladá, že FinalCutPoints sú v pôvodnom rozsahu.
            // Musíme to buď upraviť, alebo si uvedomiť, že tento krok ZMENÍ chovanie ďalších krokov.
            // Pre Min-Max normalizáciu by bolo najlepšie denormalizovať cut-pointy na konci pipeline.
            // Uložme si normalizačné parametre do kontextu:
            context.Parameters["Normalization_Min"] = minValue;
            context.Parameters["Normalization_Max"] = maxValue;

            return context;
        }

        /// <summary>
        /// Krok diskretizácie, ktorý normalizuje numerické hodnoty atribútu pomocou Z-score (Standardizácia).
        /// Upozornenie: Táto normalizácia transformuje `NumericValues` v kontexte.
        /// </summary>
        /// <param name="context">DiscretizationContext s NumericValues.</param>
        /// <returns>Modifikovaný DiscretizationContext s normalizovanými NumericValues.</returns>
        public static DiscretizationContext NormalizeNumericValuesZScore(DiscretizationContext context)
        {
            Console.WriteLine("  Spúšťam krok: Z-score Normalizácia numerických hodnôt.");

            if (!context.NumericValues.Any())
            {
                Console.WriteLine("  Upozornenie: Žiadne numerické hodnoty na normalizáciu.");
                return context;
            }

            double mean = context.NumericValues.Average();
            double sumOfSquaresOfDifferences = context.NumericValues.Sum(val => Math.Pow(val - mean, 2));
            double standardDeviation = Math.Sqrt(sumOfSquaresOfDifferences / context.NumericValues.Count);

            // Ak je štandardná odchýlka nulová (všetky hodnoty sú rovnaké), normalizácia nie je potrebná/možná.
            if (Math.Abs(standardDeviation) < double.Epsilon)
            {
                Console.WriteLine("  Všetky numerické hodnoty sú rovnaké. Z-score Normalizácia preskočená (alebo nastavené na 0).");
                for (int i = 0; i < context.NumericValues.Count; i++)
                {
                    context.NumericValues[i] = 0.0; // Všetky hodnoty budú 0 po Z-score, ak sú rovnaké
                }
                return context;
            }

            var normalizedValues = new List<double>();
            foreach (var val in context.NumericValues)
            {
                normalizedValues.Add((val - mean) / standardDeviation);
            }

            context.NumericValues = normalizedValues;
            Console.WriteLine($"  * Hodnoty atribútu '{context.AttributeName}' boli Z-score normalizované (Mean=0, StdDev=1).");

            // Uložíme si parametre pre denormalizáciu, ak by bola potrebná
            context.Parameters["Normalization_Mean"] = mean;
            context.Parameters["Normalization_StdDev"] = standardDeviation;

            return context;
        }

        /// <summary>
        /// Krok, ktorý denormalizuje cut-pointy z [0,1] späť na pôvodný rozsah.
        /// Používa parametre "Normalization_Min" a "Normalization_Max" uložené v kontexte.
        /// Mal by byť volaný po binovacom algoritme, ak bol pred ním použitý normalizačný krok.
        /// </summary>
        public static DiscretizationContext DenormalizeCutPointsMinMax(DiscretizationContext context)
        {
            Console.WriteLine("  Spúšťam krok: Denormalizácia cut-pointov (Min-Max).");

            if (!context.Parameters.TryGetValue("Normalization_Min", out object? minObj) || !(minObj is double originalMin) ||
                !context.Parameters.TryGetValue("Normalization_Max", out object? maxObj) || !(maxObj is double originalMax))
            {
                Console.WriteLine("  Upozornenie: Normalizačné parametre (Min/Max) neboli nájdené v kontexte. Denormalizácia preskočená.");
                return context;
            }

            if (Math.Abs(originalMax - originalMin) < double.Epsilon)
            {
                Console.WriteLine("  Pôvodný rozsah dát bol nulový. Denormalizácia cut-pointov preskočená.");
                // Cut-pointy by v takom prípade nemali existovať, ale ak existujú, sú zvyčajne len 0.5
                context.CutPoints = new List<double>(); // Vrátime prázdne cut-pointy
                return context;
            }

            var denormalizedCutPoints = new List<double>();
            foreach (var cp in context.CutPoints)
            {
                // X = X_norm * (Max - Min) + Min
                denormalizedCutPoints.Add(cp * (originalMax - originalMin) + originalMin);
            }

            context.CutPoints = denormalizedCutPoints.OrderBy(cp => cp).ToList();
            Console.WriteLine($"  * Cut-pointy boli denormalizované na pôvodný rozsah [{originalMin:F2}, {originalMax:F2}].");

            return context;
        }

        /// <summary>
        /// Krok, ktorý denormalizuje cut-pointy zo Z-score späť na pôvodný rozsah.
        /// Používa parametre "Normalization_Mean" a "Normalization_StdDev" uložené v kontexte.
        /// Mal by byť volaný po binovacom algoritme, ak bol pred ním použitý Z-score normalizačný krok.
        /// </summary>
        public static DiscretizationContext DenormalizeCutPointsZScore(DiscretizationContext context)
        {
            Console.WriteLine("  Spúšťam krok: Denormalizácia cut-pointov (Z-score).");

            if (!context.Parameters.TryGetValue("Normalization_Mean", out object? meanObj) || !(meanObj is double originalMean) ||
                !context.Parameters.TryGetValue("Normalization_StdDev", out object? stdDevObj) || !(stdDevObj is double originalStdDev))
            {
                Console.WriteLine("  Upozornenie: Normalizačné parametre (Mean/StdDev) neboli nájdené v kontexte. Denormalizácia preskočená.");
                return context;
            }

            if (Math.Abs(originalStdDev) < double.Epsilon)
            {
                Console.WriteLine("  Pôvodná štandardná odchýlka bola nulová. Denormalizácia cut-pointov preskočená.");
                context.CutPoints = new List<double>(); // Vrátime prázdne cut-pointy
                return context;
            }

            var denormalizedCutPoints = new List<double>();
            foreach (var cp in context.CutPoints)
            {
                // X = X_norm * StdDev + Mean
                denormalizedCutPoints.Add(cp * originalStdDev + originalMean);
            }

            context.CutPoints = denormalizedCutPoints.OrderBy(cp => cp).ToList();
            Console.WriteLine($"  * Cut-pointy boli denormalizované na pôvodný rozsah (Mean: {originalMean:F2}, StdDev: {originalStdDev:F2}).");

            return context;
        }
    }
}