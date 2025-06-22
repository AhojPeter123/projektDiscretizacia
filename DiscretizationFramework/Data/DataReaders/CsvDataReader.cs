using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DiscretizationFramework.Data.DataModels;

namespace DiscretizationFramework.Data.DataReaders
{
    /// <summary>
    /// Poskytuje metódy na načítanie dát z CSV súborov.
    /// </summary>
    public static class CsvDataReader
    {
        /// <summary>
        /// Načíta CSV súbor do objektu DataSet.
        /// </summary>
        /// <param name="filePath">Cesta k CSV súboru.</param>
        /// <param name="delimiter">Oddeľovač stĺpcov (predvolene čiarka).</param>
        /// <param name="hasHeader">Určuje, či súbor obsahuje hlavičkový riadok (predvolene true).</param>
        /// <param name="headers">Voliteľný zoznam hlavičiek, ak súbor neobsahuje hlavičkový riadok.</param>
        /// <param name="targetAttributeName">Názov cieľového atribútu (predvolene posledný stĺpec).</param>
        /// <returns>Objekt DataSet obsahujúci načítané dáta.</returns>
        /// <exception cref="FileNotFoundException">Vyhodí sa, ak súbor nebol nájdený.</exception>
        /// <exception cref="InvalidOperationException">Vyhodí sa, ak súbor je prázdny alebo hlavičky chýbajú.</exception>
        /// <exception cref="ArgumentException">Vyhodí sa, ak pre súbor bez hlavičky neboli poskytnuté hlavičky.</exception>
        public static DataSet LoadCsv(string filePath, char delimiter = ',', bool hasHeader = true, List<string>? headers = null, string targetAttributeName = "")
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Súbor nebol nájdený: {filePath}");
            }

            var rows = new List<DataRow>();
            var attributeTypes = new Dictionary<string, Type>();
            List<string> finalHeaders = new List<string>();

            using (var reader = new StreamReader(filePath))
            {
                string? headerLine = null;
                if (hasHeader)
                {
                    headerLine = reader.ReadLine();
                    if (headerLine == null) throw new InvalidOperationException("CSV súbor je prázdny alebo neobsahuje hlavičkový riadok.");
                    finalHeaders = headerLine.Split(delimiter).Select(h => h.Trim()).ToList();
                }
                else if (headers != null && headers.Any())
                {
                    finalHeaders = headers.Select(h => h.Trim()).ToList();
                }
                else
                {
                    throw new ArgumentException("Ak súbor nemá hlavičkový riadok (hasHeader=false), musia byť hlavičky explicitne poskytnuté cez parameter 'headers'.");
                }

                string actualTargetAttributeName = string.IsNullOrWhiteSpace(targetAttributeName) ? finalHeaders.LastOrDefault() ?? string.Empty : targetAttributeName;
                if (string.IsNullOrWhiteSpace(actualTargetAttributeName) && finalHeaders.Any())
                {
                    actualTargetAttributeName = finalHeaders.Last();
                }
                else if (!finalHeaders.Any())
                {
                    throw new InvalidOperationException("Nemožno určiť cieľový atribút bez hlavičiek alebo explicitného názvu.");
                }

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var values = line.Split(delimiter).Select(v => v.Trim()).ToList();
                    if (values.Count != finalHeaders.Count)
                    {
                        Console.WriteLine($"Upozornenie: Riadok '{line}' má nesprávny počet stĺpcov ({values.Count} namiesto {finalHeaders.Count}). Riadok bol preskočený.");
                        continue;
                    }

                    var attributes = new Dictionary<string, object>();
                    string targetValue = string.Empty;

                    for (int i = 0; i < finalHeaders.Count; i++)
                    {
                        string header = finalHeaders[i];
                        string value = values[i];

                        if (header.Equals(actualTargetAttributeName, StringComparison.OrdinalIgnoreCase))
                        {
                            targetValue = value;
                        }
                        else
                        {
                            attributes[header] = value;
                        }
                    }
                    rows.Add(new DataRow(attributes, targetValue));
                }

                // Inferovanie typov (len pre prvých 100 riadkov alebo menej, pre optimalizáciu)
                var sampleRowsForTypeInference = rows.Take(100).ToList();
                if (sampleRowsForTypeInference.Any())
                {
                    foreach (string header in finalHeaders)
                    {
                        if (header.Equals(actualTargetAttributeName, StringComparison.OrdinalIgnoreCase))
                        {
                            attributeTypes[header] = typeof(string);
                            continue;
                        }

                        bool isNumeric = true;
                        bool isBoolean = true;
                        foreach (var row in sampleRowsForTypeInference)
                        {
                            if (row.Attributes.TryGetValue(header, out object? valObj))
                            {
                                string? val = valObj?.ToString();
                                if (string.IsNullOrWhiteSpace(val))
                                {
                                    isNumeric = false;
                                    isBoolean = false;
                                    // Pokračuj v kontrole pre ostatné typy, ale tento riadok neprejde ako numerický/boolean
                                }
                                else
                                {
                                    if (!double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                                    {
                                        isNumeric = false;
                                    }
                                    if (!bool.TryParse(val, out _))
                                    {
                                        isBoolean = false;
                                    }
                                }
                            }
                            else
                            {
                                isNumeric = false; // Ak hodnota chýba, nemôžeme ju inferovať ako numerickú/boolean
                                isBoolean = false;
                            }
                            // Ak už vieme, že to nie je ani numerické ani boolean, môžeme preskočiť zvyšné riadky
                            if (!isNumeric && !isBoolean) break;
                        }

                        if (isNumeric)
                        {
                            attributeTypes[header] = typeof(double);
                        }
                        else if (isBoolean)
                        {
                            attributeTypes[header] = typeof(bool);
                        }
                        else
                        {
                            attributeTypes[header] = typeof(string);
                        }
                    }
                }
                else
                {
                    foreach (string header in finalHeaders)
                    {
                        attributeTypes[header] = typeof(string);
                    }
                }

                return new DataSet(rows, finalHeaders, actualTargetAttributeName, attributeTypes);
            }
        }
    }
}