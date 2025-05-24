using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Linq;

namespace LabourBudgetCalculator
{
    [Serializable]
    public class CommissioningProject
    {
        public string ProjectID { get; set; }
        public string ProjectName { get; set; }
        public DateTime ProjectDate { get; set; }
        public string ClientName { get; set; }
        public string ProjectDescription { get; set; }
        public decimal InitialEstimate { get; set; }
        public List<CommissioningResource> Resources { get; set; }

        [XmlIgnore]
        public bool IsDirty { get; set; }

        [XmlIgnore]
        public decimal PlannedTotal { get; private set; }

        [XmlIgnore]
        public decimal CurrentTotal { get; private set; }

        [XmlIgnore]
        public decimal ForecastTotal { get; private set; }

        public CommissioningProject()
        {
            ProjectID = Guid.NewGuid().ToString();
            ProjectName = "New Project";
            ProjectDate = DateTime.Today;
            Resources = new List<CommissioningResource>();
            IsDirty = true;
        }

        /// <summary>
        /// Calculates the project totals by summing all resource totals
        /// </summary>
        public void CalculateTotals()
        {
            try
            {
                PlannedTotal = InitialEstimate; // Start with the quoted amount
                CurrentTotal = 0;
                ForecastTotal = 0;

                if (Resources != null && Resources.Any())
                {
                    // First ensure all resource totals are calculated
                    foreach (var resource in Resources)
                    {
                        resource.CalculateResourceTotals();
                    }

                    // Then sum them up
                    PlannedTotal = Resources.Sum(r => r.PlannedResourceTotal);
                    CurrentTotal = Resources.Sum(r => r.ActualResourceTotal);
                    ForecastTotal = Resources.Sum(r => r.ForecastResourceTotal);
                }

                IsDirty = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CalculateTotals: {ex.Message}");
                IsDirty = true;
            }
        }

        // Add this method to the CommissioningProject class if it doesn't already exist
        public void RecalculateExpenses()
        {
            if (Resources == null) return;

            foreach (var resource in Resources)
            {
                Helpers.ExpenseCalculator.CalculateResourceExpenses(resource);
                resource.CalculateResourceTotals();
            }

            // Update project-level totals
            CalculateTotals();
        }
    }
}