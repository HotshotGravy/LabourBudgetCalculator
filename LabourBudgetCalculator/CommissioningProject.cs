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
                PlannedTotal = 0;
                CurrentTotal = 0;
                ForecastTotal = 0;

                if (Resources == null || !Resources.Any())
                {
                    return;
                }

                foreach (var resource in Resources)
                {
                    // Ensure resource has valid daily data
                    if (resource.DailyData == null || !resource.DailyData.Any() || resource.IsDirty)
                    {
                        resource.InitializeFromSchedule();
                    }

                    // Calculate resource totals
                    resource.CalculateResourceTotals();

                    // Add to project totals
                    PlannedTotal += resource.PlannedResourceTotal;
                    CurrentTotal += resource.ActualResourceTotal;
                    ForecastTotal += resource.ForecastResourceTotal;
                }

                IsDirty = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CalculateTotals for project {ProjectName}: {ex.Message}");
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