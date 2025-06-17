// DiscretizationFramework/Discretization/Core/DiscretizationContext.cs
using System;
using System.Collections.Generic;
using System.Linq;
using DiscretizationFramework.Data.DataModels;

namespace DiscretizationFramework.Discretization.Core
{
    /// <summary>
    /// Reprezentuje kontext diskretizácie pre jeden atribút.
    /// Slúži ako dátový kontajner, ktorý prechádza medzi jednotlivými krokmi diskretizačného algoritmu.
    /// </summary>
    public class DiscretizationContext
    {
        /// <summary>
        /// Odkaz na celý pôvodný dataset. Potrebný pre prístup k všetkým riadkom,
        /// napríklad pre výpočet entropie, ktorá závisí od cieľovej premennej.
        /// </summary>
        public DataSet OriginalDataSet { get; private set; }

        /// <summary>
        /// Názov atribútu, ktorý je aktuálne diskretizovaný.
        /// </summary>
        public string AttributeName { get; private set; }

        /// <summary>
        /// Zoznam numerických hodnôt atribútu, ktorý sa diskretizuje, zoradených vzostupne.
        /// Tieto hodnoty sú z celého atribútu. Pre sub-segmenty sa odovzdajú do generátorov.
        /// </summary>
        public List<double> NumericValues { get; set; }

        /// <summary>
        /// Aktuálny zoznam rezových bodov (cut-points), ktoré definujú hranice binov.
        /// Tento zoznam sa počas diskretizácie kumuluje a na konci obsahuje finálne cut-points.
        /// </summary>
        public List<double> CutPoints { get; set; } = new List<double>();

        /// <summary>
        /// Slovník pre uloženie špecifických parametrov algoritmu.
        /// Napr. "NumberOfBins", "MinGainThreshold", "MaxDepth", atď.
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Inicializuje novú inštanciu triedy DiscretizationContext.
        /// </summary>
        /// <param name="originalDataSet">Pôvodný dataset.</param>
        /// <param name="attributeName">Názov atribútu, ktorý sa má diskretizovať.</param>
        /// <param name="parameters">Voliteľné počiatočné parametre pre algoritmus.</param>
        public DiscretizationContext(DataSet originalDataSet, string attributeName, Dictionary<string, object> parameters = null)
        {
            OriginalDataSet = originalDataSet ?? throw new ArgumentNullException(nameof(originalDataSet));
            AttributeName = attributeName ?? throw new ArgumentNullException(nameof(attributeName));

            // Extrahujeme a zoradíme numerické hodnoty hneď pri inicializácii kontextu pre efektívnosť.
            NumericValues = originalDataSet.GetNumericValues(attributeName).OrderBy(x => x).ToList();

            if (parameters != null)
            {
                Parameters = new Dictionary<string, object>(parameters);
            }
        }
    }
}