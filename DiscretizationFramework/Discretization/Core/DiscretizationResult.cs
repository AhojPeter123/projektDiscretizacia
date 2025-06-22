using System.Collections.Generic;
using DiscretizationFramework.Data.DataModels;

namespace DiscretizationFramework.Discretization.Core
{
    /// <summary>
    /// Obalová trieda pre výsledok diskretizácie jedného atribútu.
    /// </summary>
    public class DiscretizationResult
    {
        public List<DataRow> DiscretizedRows { get; set; } = new List<DataRow>(); // Dátové riadky s diskretizovaným atribútom
        public List<double> FinalCutPoints { get; set; } = new List<double>(); // Finálne cut-pointy použité pre diskretizáciu
        public List<double> OriginalNumericValues { get; set; } = new List<double>(); // Pôvodné numerické hodnoty diskretizovaného atribútu
        public string? DiscretizedAttributeName { get; set; } // Názov diskretizovaného atribútu
    }
}