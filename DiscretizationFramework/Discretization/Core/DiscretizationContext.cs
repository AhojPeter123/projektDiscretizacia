using System.Collections.Generic;
using System.Linq;
using DiscretizationFramework.Data.DataModels;

namespace DiscretizationFramework.Discretization.Core
{
    public class DiscretizationContext
    {
        public DataSet OriginalDataSet { get; }
        public string AttributeName { get; }
        public List<double> NumericValues { get; set; } // Konvertované numerické hodnoty pre daný atribút
        public List<double> CutPoints { get; set; } // Výsledné cut-pointy
        public Dictionary<string, object> Parameters { get; set; } // Parametre pre diskretizačné algoritmy

        public DiscretizationContext(DataSet dataSet, string attributeName, Dictionary<string, object>? initialParameters = null)
        {
            OriginalDataSet = dataSet;
            AttributeName = attributeName;
            NumericValues = new List<double>(); // Bude naplnené v CommonSteps.ConvertAttributesToNumeric
            CutPoints = new List<double>();
            Parameters = initialParameters ?? new Dictionary<string, object>();
        }
    }
}