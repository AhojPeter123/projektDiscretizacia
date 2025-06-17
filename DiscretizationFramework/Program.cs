using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using DiscretizationFramework.Data.DataModels;
using DiscretizationFramework.Data.DataReaders;
using DiscretizationFramework.Discretization.Core;
using DiscretizationFramework.Discretization.Steps;

namespace DiscretizationFramework
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            string csvFilePath = "sample_data.csv";
            CreateSampleCsv(csvFilePath); // Vytvorí súbor pre test

            try
            {
                Console.WriteLine("--- Začínam Načítanie Datasetu ---");
                DataSet loadedDataSet = CsvDataReader.LoadCsv(csvFilePath, delimiter: ';', targetAttributeName: "BuysComputer");

                if (!loadedDataSet.Rows.Any())
                {
                    Console.WriteLine("Dataset je prázdny alebo došlo k chybe. Ukončujem.");
                    return;
                }
                PrintDataSetInfo(loadedDataSet, sampleRows: 5); // Pomocná funkcia na výpis info o datasete

                Console.WriteLine("\n--- Spúšťam Diskretizáciu pre testovacie scenáre ---");

                // --- Test 1: Equal-Width Binning na atribúte 'Age' (4 biny) ---
                Console.WriteLine("\n*** Test 1: Diskretizujem 'Age' pomocou Equal-Width Binningu (4 biny) ***");
                var equalWidthDiscretizer = Discretizer.Create(
                    "Equal-Width Binning",
                    new List<DiscretizationStep>
                    {
                        (context) => {
                            context.Parameters["NumberOfBins"] = 4;
                            Console.WriteLine("      Nastavujem parameter: NumberOfBins = 4 pre Equal-Width.");
                            return context;
                        },
                        CommonSteps.PrepareNumericValues,
                        (context) => GeneralIterativeStep.IterativeBinning(context, BinningStrategies.EqualWidthGenerator)
                    }
                );
                var resultAgeEW = equalWidthDiscretizer.Discretize(loadedDataSet, "Age");
                Console.WriteLine("\n--- Výsledok diskretizácie 'Age' (Equal-Width, prvých 5 riadkov) ---");
                PrintSampleRows(resultAgeEW, loadedDataSet.Headers, loadedDataSet.TargetAttributeName, 5);

                // --- Test 2: Equal-Frequency Binning na atribúte 'CreditScore' (3 biny) ---
                Console.WriteLine("\n*** Test 2: Diskretizujem 'CreditScore' pomocou Equal-Frequency Binningu (3 biny) ***");
                var equalFrequencyDiscretizer = Discretizer.Create(
                    "Equal-Frequency Binning",
                    new List<DiscretizationStep>
                    {
                        (context) => {
                            context.Parameters["NumberOfBins"] = 3;
                            Console.WriteLine("      Nastavujem parameter: NumberOfBins = 3 pre Equal-Frequency.");
                            return context;
                        },
                        CommonSteps.PrepareNumericValues,
                        (context) => GeneralIterativeStep.IterativeBinning(context, BinningStrategies.EqualFrequencyGenerator)
                    }
                );
                var resultCreditScoreEF = equalFrequencyDiscretizer.Discretize(loadedDataSet, "CreditScore");
                Console.WriteLine("\n--- Výsledok diskretizácie 'CreditScore' (Equal-Frequency, prvých 5 riadkov) ---");
                PrintSampleRows(resultCreditScoreEF, loadedDataSet.Headers, loadedDataSet.TargetAttributeName, 5);


                // --- Test 3: Simulovaná rekurzívna diskretizácia na atribúte 'Age' ---
                Console.WriteLine("\n*** Test 3: Diskretizujem 'Age' pomocou Simulovanej Rekurzívnej Diskretizácie ***");
                var recursiveMdlpDiscretizer = Discretizer.Create(
                    "Recursive MDLP-like Discretization",
                    new List<DiscretizationStep>
                    {
                        (context) => {
                            context.Parameters["MinGainThreshold"] = 0.01; // Simulovaný prah
                            context.Parameters["MaxDepth"] = 2; // Simulovaná hĺbka pre zjednodušenie
                            Console.WriteLine("      Nastavujem parametre pre simulovanú rekurziu: MinGainThreshold=0.01, MaxDepth=2.");
                            return context;
                        },
                        CommonSteps.PrepareNumericValues,
                        (context) => GeneralRecursiveStep.RecursiveBinning(context, BinningStrategies.SimulatedMdlpSplitFinder)
                    }
                );
                var resultAgeMDLP = recursiveMdlpDiscretizer.Discretize(loadedDataSet, "Age");
                Console.WriteLine("\n--- Výsledok diskretizácie 'Age' (Simulovaná Rekurzívna, prvých 5 riadkov) ---");
                PrintSampleRows(resultAgeMDLP, loadedDataSet.Headers, loadedDataSet.TargetAttributeName, 5);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n!!! Vyskytla sa chyba počas behu programu: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                if (File.Exists(csvFilePath))
                {
                    File.Delete(csvFilePath);
                    Console.WriteLine($"\nTestovací CSV súbor '{csvFilePath}' bol vymazaný.");
                }
                Console.WriteLine("\nTestovanie dokončené. Pre ukončenie stlačte ľubovoľnú klávesu...");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// Pomocná metóda na vytvorenie simulovaného CSV súboru pre testovanie.
        /// </summary>
        private static void CreateSampleCsv(string filePath)
        {
            string csvContent = "Age;Income;Student;CreditScore;BuysComputer\n" +
                                "20;Low;No;Excellent;No\n" +
                                "25;Low;No;Fair;No\n" +
                                "30;High;No;Excellent;Yes\n" +
                                "35;Medium;No;Fair;Yes\n" +
                                "40;High;Yes;Excellent;Yes\n" +
                                "45;Medium;Yes;Fair;No\n" +
                                "50;Low;No;Excellent;Yes\n" +
                                "55;High;No;Fair;Yes\n" +
                                "60;Medium;Yes;Excellent;No\n" +
                                "65;Low;No;Fair;No\n" +
                                "70;High;Yes;Excellent;Yes\n" +
                                "22;Medium;No;Fair;No\n" +
                                "38;Low;Yes;Excellent;Yes\n" +
                                "48;High;No;Fair;Yes\n" +
                                "29;Low;No;Fair;No\n" +
                                "33;High;No;Excellent;Yes";

            File.WriteAllText(filePath, csvContent);
            Console.WriteLine($"Simulovaný CSV súbor '{Path.GetFileName(filePath)}' bol vytvorený.");
        }

        /// <summary>
        /// Vypíše základné informácie o datasete.
        /// </summary>
        private static void PrintDataSetInfo(DataSet dataSet, int sampleRows)
        {
            Console.WriteLine($"\n--- Informácie o načítanom datasete ({dataSet.Rows.Count} riadkov) ---");
            Console.WriteLine($"Hlavičky: {string.Join(", ", dataSet.Headers)}");
            Console.WriteLine($"Cieľový atribút: {dataSet.TargetAttributeName}");
            Console.WriteLine("Inferované typy atribútov:");
            foreach (var entry in dataSet.AttributeTypes)
            {
                Console.WriteLine($"  - {entry.Key}: {entry.Value.Name}");
            }

            Console.WriteLine($"\n--- Vzorka prvých {sampleRows} riadkov pôvodných dát ---");
            PrintSampleRows(dataSet.Rows, dataSet.Headers, dataSet.TargetAttributeName, sampleRows);
        }

        /// <summary>
        /// Vypíše vzorku riadkov datasetu do konzoly.
        /// </summary>
        private static void PrintSampleRows(List<DataRow> rows, List<string> headers, string targetAttributeName, int count)
        {
            if (!rows.Any())
            {
                Console.WriteLine("Žiadne riadky na zobrazenie.");
                return;
            }

            // Vytvoríme hlavičkový riadok (bez cieľového atribútu na začiatku, pridáme ho na koniec)
            var displayHeaders = headers.Where(h => h != targetAttributeName).ToList();
            displayHeaders.Add(targetAttributeName); // Cieľový atribút na koniec pre konzistentnosť

            Console.WriteLine(string.Join("\t", displayHeaders));
            Console.WriteLine(new string('-', (displayHeaders.Count * 8) - 1)); // Podčiarknutie

            int printedCount = 0;
            foreach (var row in rows)
            {
                if (printedCount >= count) break;

                var valuesToPrint = new List<string>();
                foreach (var header in displayHeaders.Where(h => h != targetAttributeName))
                {
                    valuesToPrint.Add(row.Attributes.TryGetValue(header, out object value) ? value.ToString() : "N/A");
                }
                valuesToPrint.Add(row.Target); // Pridáme cieľovú hodnotu na koniec

                Console.WriteLine(string.Join("\t", valuesToPrint));
                printedCount++;
            }
            if (rows.Count > count)
            {
                Console.WriteLine($"... (ďalších {rows.Count - count} riadkov)");
            }
        }
    }
} 