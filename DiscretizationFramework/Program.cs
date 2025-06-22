using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using DiscretizationFramework.Data.DataModels;
using DiscretizationFramework.Data.DataReaders;
using DiscretizationFramework.Discretization.Core;
using DiscretizationFramework.Discretization.Steps;
using DiscretizationFramework.Discretization.Strategies;

namespace DiscretizationFramework
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            string irisCsvFilePath = "iris.csv";
            string irisUrl = "https://gist.githubusercontent.com/netj/8836201/raw/6f9306ad21398ea43cba4f7d537619d0e07d5ae3/iris.csv";

            await DownloadIrisDataset(irisUrl, irisCsvFilePath);

            try
            {
                Console.WriteLine("--- Začínam Načítanie Iris Datasetu ---");
                List<string> irisHeaders = new List<string> {
                    "Sepal.Length", "Sepal.Width", "Petal.Length", "Petal.Width", "Species"
                };
                DataSet loadedDataSet = CsvDataReader.LoadCsv(irisCsvFilePath, delimiter: ',', hasHeader: false, headers: irisHeaders, targetAttributeName: "Species");

                if (!loadedDataSet.Rows.Any())
                {
                    Console.WriteLine("Iris dataset je prázdny alebo došlo k chybe. Ukončujem.");
                    return;
                }
                PrintDataSetInfo(loadedDataSet, sampleRows: 0);

                Console.WriteLine("Inferované typy atribútov po načítaní (z DataSet):");
                foreach (var entry in loadedDataSet.AttributeTypes)
                {
                    Console.WriteLine($"  - {entry.Key}: {entry.Value.Name}");
                }
                if (!loadedDataSet.AttributeTypes.Any())
                {
                    Console.WriteLine("  (Žiadne typy neboli inferované alebo DataSet.AttributeTypes je prázdne - očakáva sa konverzia v prvom kroku)");
                }


                Console.WriteLine("\n--- Spúšťam Diskretizáciu s Generickými Krokmi ---");

                // Príklad 1: Unsupervised Information Density (Používa GeneralRecursiveStep)
                Console.WriteLine("\n*** Test: Diskretizujem 'Petal.Length' pomocou UNSUPERVISED Information Density ***");
                var unsupervisedIDDiscretizer = Discretizer.Create(
                    "Unsupervised Information Density Discretization",
                    new List<DiscretizationStep>
                    {
                        CommonSteps.ConvertAttributesToNumeric,
                        new GeneralRecursiveStep("Unsupervised IDD", InformationDensityStrategy.UnsupervisedSplitCriterion)
                    }
                );
                var resultPetalLengthUnsup = unsupervisedIDDiscretizer.Discretize(loadedDataSet, "Petal.Length");
                Console.WriteLine($"Finálne Cut-points pre 'Petal.Length' (Unsupervised): [{string.Join(", ", resultPetalLengthUnsup.FinalCutPoints.Select(cp => cp.ToString("F2")))}]");


                // Príklad 2: Supervised Information Density (Používa GeneralRecursiveStep)
                Console.WriteLine("\n*** Test: Diskretizujem 'Sepal.Length' pomocou SUPERVISED Information Density ***");
                var supervisedIDDiscretizer = Discretizer.Create(
                    "Supervised Information Density Discretization",
                    new List<DiscretizationStep>
                    {
                        CommonSteps.ConvertAttributesToNumeric,
                        new GeneralRecursiveStep("Supervised IDD", InformationDensityStrategy.SupervisedSplitCriterion)
                    }
                );
                var resultSepalLengthSup = supervisedIDDiscretizer.Discretize(loadedDataSet, "Sepal.Length");
                Console.WriteLine($"Finálne Cut-points pre 'Sepal.Length' (Supervised): [{string.Join(", ", resultSepalLengthSup.FinalCutPoints.Select(cp => cp.ToString("F2")))}]");


                // Príklad 3: Equal Width s automatickým určením počtu binov (Sturgesovo pravidlo)
                Console.WriteLine("\n*** Test: Diskretizujem 'Sepal.Width' pomocou Equal Width Discretization (auto-bins Sturges) ***");
                var equalWidthAutoBinsDiscretizer = Discretizer.Create(
                    "Equal Width Discretization (Auto Bins - Sturges)",
                    new List<DiscretizationStep>
                    {
                        CommonSteps.ConvertAttributesToNumeric, // Krok 1: Konverzia
                        CommonSteps.CalculateOptimalNumberOfBins, // Krok 2: Vypočíta a uloží "optimalNumBins" do kontextu
                        new GeneralIterativeStep("Equal Width", EqualWidthStrategy.BinningLogic) // Krok 3: Použije "optimalNumBins"
                    }
                );
                var resultSepalWidthEWAuto = equalWidthAutoBinsDiscretizer.Discretize(loadedDataSet, "Sepal.Width");
                Console.WriteLine($"Finálne Cut-points pre 'Sepal.Width' (Equal Width, Auto Bins): [{string.Join(", ", resultSepalWidthEWAuto.FinalCutPoints.Select(cp => cp.ToString("F2")))}]");


                // Príklad 4: Equal Frequency s explicitným počtom binov
                Console.WriteLine("\n*** Test: Diskretizujem 'Petal.Width' pomocou Equal Frequency Discretization (explicit-bins) ***");
                var equalFrequencyExplicitBinsDiscretizer = Discretizer.Create(
                    "Equal Frequency Discretization (Explicit Bins)",
                    new List<DiscretizationStep>
                    {
                        CommonSteps.ConvertAttributesToNumeric,
                        new GeneralIterativeStep("Equal Frequency", EqualFrequencyStrategy.BinningLogic)
                    }
                );
                var parametersEF = new Dictionary<string, object> { { "numBins", 3 } }; // Explicitne 3 biny
                var resultPetalWidthEFExplicit = equalFrequencyExplicitBinsDiscretizer.Discretize(loadedDataSet, "Petal.Width", parametersEF);
                Console.WriteLine($"Finálne Cut-points pre 'Petal.Width' (Equal Frequency, 3 bins): [{string.Join(", ", resultPetalWidthEFExplicit.FinalCutPoints.Select(cp => cp.ToString("F2")))}]");

                // Príklad 5: Equal Frequency s automatickým určením počtu binov (Sturgesovo pravidlo)
                Console.WriteLine("\n*** Test: Diskretizujem 'Petal.Width' pomocou Equal Frequency Discretization (auto-bins Sturges) ***");
                var equalFrequencyAutoBinsDiscretizer = Discretizer.Create(
                    "Equal Frequency Discretization (Auto Bins - Sturges)",
                    new List<DiscretizationStep>
                    {
                        CommonSteps.ConvertAttributesToNumeric, // Krok 1: Konverzia
                        CommonSteps.CalculateOptimalNumberOfBins, // Krok 2: Vypočíta a uloží "optimalNumBins" do kontextu
                        new GeneralIterativeStep("Equal Frequency", EqualFrequencyStrategy.BinningLogic) // Krok 3: Použije "optimalNumBins"
                    }
                );
                var resultPetalWidthEFAuto = equalFrequencyAutoBinsDiscretizer.Discretize(loadedDataSet, "Petal.Width");
                Console.WriteLine($"Finálne Cut-points pre 'Petal.Width' (Equal Frequency, Auto Bins): [{string.Join(", ", resultPetalWidthEFAuto.FinalCutPoints.Select(cp => cp.ToString("F2")))}]");


            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n!!! Vyskytla sa chyba počas behu programu: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                if (File.Exists(irisCsvFilePath))
                {
                    File.Delete(irisCsvFilePath);
                    Console.WriteLine($"\nTestovací CSV súbor '{irisCsvFilePath}' bol vymazaný.");
                }
                Console.WriteLine("\nTestovanie dokončené. Pre ukončenie stlačte ľubovoľnú klávesu...");
                Console.ReadKey();
            }
        }

        private static async Task DownloadIrisDataset(string url, string filePath)
        {
            if (File.Exists(filePath))
            {
                Console.WriteLine($"Súbor '{Path.GetFileName(filePath)}' už existuje, preskakujem stiahnutie.");
                return;
            }

            Console.WriteLine($"Sťahujem Iris dataset z {url}...");
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string csvContent = await client.GetStringAsync(url);
                    File.WriteAllText(filePath, csvContent);
                    Console.WriteLine($"Iris dataset uložený do '{Path.GetFileName(filePath)}'.");
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"Chyba pri sťahovaní datasetu: {e.Message}");
                    Console.WriteLine("Skontrolujte pripojenie na internet alebo URL adresu.");
                    Environment.Exit(1);
                }
            }
        }

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
            if (sampleRows > 0)
            {
                Console.WriteLine($"\n--- Vzorka prvých {sampleRows} riadkov pôvodných dát ---");
                PrintSampleRows(dataSet.Rows, dataSet.Headers, dataSet.TargetAttributeName, sampleRows);
            }
        }

        private static void PrintSampleRows(List<DataRow> rows, List<string> headers, string targetAttributeName, int count)
        {
            if (!rows.Any() || count <= 0)
            {
                Console.WriteLine("Žiadne riadky na zobrazenie.");
                return;
            }

            var displayHeaders = headers.Where(h => h != targetAttributeName).ToList();
            displayHeaders.Add(targetAttributeName);

            Console.WriteLine(string.Join("\t", displayHeaders));
            Console.WriteLine(new string('-', (displayHeaders.Count * 8) - 1));

            int printedCount = 0;
            foreach (var row in rows)
            {
                if (printedCount >= count) break;

                var valuesToPrint = new List<string>();
                foreach (var header in displayHeaders.Where(h => h != targetAttributeName))
                {
                    valuesToPrint.Add(row.Attributes.TryGetValue(header, out object? value) ? (value?.ToString() ?? "N/A") : "N/A");
                }
                valuesToPrint.Add(row.Target);

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