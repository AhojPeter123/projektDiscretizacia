// DiscretizationFramework/Data/DataReaders/CsvDataReader.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;

using DiscretizationFramework.Data.DataModels;

namespace DiscretizationFramework.Data.DataReaders
{
    /// <summary>
    /// Statická trieda pre načítanie dát z CSV súborov do objektu DataSet.
    /// Zvláda parsovanie hlavičiek a inferenciu typov atribútov.
    /// </summary>
    public static class CsvDataReader
    {
        /// <summary>
        /// Načíta CSV súbor do objektu DataSet.
        /// Predpokladá, že prvý riadok je hlavička.
        /// Pokúsi sa inferovať typy atribútov (int, double, string) na základe vzorky riadkov.
        /// </summary>
        /// <param name="filePath">Cesta k CSV súboru.</param>
        /// <param name="delimiter">Oddeľovač stĺpcov (napr. ',', ';'). Predvolené je čiarka.</param>
        /// <param name="targetAttributeName">Názov cieľového atribútu. Ak je null, predvolí sa posledný stĺpec v hlavičke.</param>
        /// <param name="inferenceSampleSize">Počet riadkov použitých na inferenciu typov atribútov. Vyššia vzorka zvyšuje presnosť, ale môže spomaliť.</param>
        /// <returns>Objekt DataSet obsahujúci načítané dáta a meta-informácie.</returns>
        public static DataSet LoadCsv(string filePath, char delimiter = ',', string targetAttributeName = null, int inferenceSampleSize = 100)
        {
            var dataSet = new DataSet();

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Chyba: Súbor '{filePath}' neexistuje.");
                return dataSet;
            }

            var lines = File.ReadAllLines(filePath).ToList();

            if (!lines.Any())
            {
                Console.WriteLine("CSV súbor je prázdny.");
                return dataSet;
            }

            dataSet.Headers = lines[0].Split(delimiter).Select(h => h.Trim()).ToList();

            if (string.IsNullOrEmpty(targetAttributeName))
            {
                dataSet.TargetAttributeName = dataSet.Headers.Last();
            }
            else
            {
                if (!dataSet.Headers.Contains(targetAttributeName))
                {
                    Console.WriteLine($"Chyba: Cieľový atribút '{targetAttributeName}' nebol nájdený v hlavičke CSV.");
                    return new DataSet();
                }
                dataSet.TargetAttributeName = targetAttributeName;
            }

            int actualSampleSize = Math.Min(lines.Count - 1, inferenceSampleSize);
            var columnValues = new Dictionary<int, List<string>>();

            for (int i = 0; i < dataSet.Headers.Count; i++)
            {
                columnValues[i] = new List<string>();
            }

            for (int j = 1; j <= actualSampleSize; j++)
            {
                if (j >= lines.Count) break;
                var values = lines[j].Split(delimiter).Select(v => v.Trim()).ToList();
                for (int k = 0; k < Math.Min(values.Count, dataSet.Headers.Count); k++)
                {
                    columnValues[k].Add(values[k]);
                }
            }

            for (int i = 0; i < dataSet.Headers.Count; i++)
            {
                string header = dataSet.Headers[i];
                bool canBeDouble = true;
                bool canBeInt = true;

                if (!columnValues.ContainsKey(i) || !columnValues[i].Any())
                {
                    dataSet.AttributeTypes[header] = typeof(string);
                    continue;
                }

                foreach (var valueStr in columnValues[i])
                {
                    if (string.IsNullOrWhiteSpace(valueStr)) continue;

                    if (!double.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    {
                        canBeDouble = false;
                        canBeInt = false;
                        break;
                    }
                    if (!int.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    {
                        canBeInt = false;
                    }
                }

                if (canBeInt)
                {
                    dataSet.AttributeTypes[header] = typeof(int);
                }
                else if (canBeDouble)
                {
                    dataSet.AttributeTypes[header] = typeof(double);
                }
                else
                {
                    dataSet.AttributeTypes[header] = typeof(string);
                }
            }

            for (int i = 1; i < lines.Count; i++)
            {
                var values = lines[i].Split(delimiter).Select(v => v.Trim()).ToList();
                if (values.Count != dataSet.Headers.Count)
                {
                    Console.WriteLine($"Upozornenie: Preskakujem riadok {i + 1} kvôli nesprávnemu počtu stĺpcov: {lines[i]}");
                    continue;
                }

                var attributes = new Dictionary<string, object>();
                string targetValue = "";

                for (int j = 0; j < dataSet.Headers.Count; j++)
                {
                    string header = dataSet.Headers[j];
                    string valueStr = values[j];

                    if (header == dataSet.TargetAttributeName)
                    {
                        targetValue = valueStr;
                        continue;
                    }

                    if (dataSet.AttributeTypes.TryGetValue(header, out Type inferredType))
                    {
                        try
                        {
                            if (inferredType == typeof(int))
                            {
                                attributes[header] = int.Parse(valueStr, CultureInfo.InvariantCulture);
                            }
                            else if (inferredType == typeof(double))
                            {
                                attributes[header] = double.Parse(valueStr, CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                attributes[header] = valueStr;
                            }
                        }
                        catch (FormatException)
                        {
                            Console.WriteLine($"Upozornenie: Nevedel som parsovať '{valueStr}' pre atribút '{header}' ako {inferredType.Name}. Uložím ako string.");
                            attributes[header] = valueStr;
                        }
                    }
                    else
                    {
                        attributes[header] = valueStr;
                        dataSet.AttributeTypes[header] = typeof(string);
                    }
                }
                dataSet.AddRow(new DataRow(attributes, targetValue));
            }

            Console.WriteLine($"Načítanie datasetu dokončené: {dataSet.Rows.Count} riadkov a {dataSet.Headers.Count} atribútov.");
            return dataSet;
        }
    }
}