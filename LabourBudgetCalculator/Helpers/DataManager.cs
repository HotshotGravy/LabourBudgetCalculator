using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;
using TimeExpenseCalculator.Models;

namespace TimeExpenseCalculator.Helpers
{
    public static class DataManager
    {
        private static string dataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TimeExpenseCalculator");

        private static string ratesFile = Path.Combine(dataPath, "RateSheets.xml");

        static DataManager()
        {
            // Create directory if it doesn't exist
            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }
        }

        public static void SaveRateSheets(List<RateSheet> rateSheets)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<RateSheet>));
                using (FileStream fs = new FileStream(ratesFile, FileMode.Create))
                {
                    serializer.Serialize(fs, rateSheets);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving rate sheets: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static List<RateSheet> LoadRateSheets()
        {
            List<RateSheet> rateSheets = new List<RateSheet>();

            if (File.Exists(ratesFile))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<RateSheet>));
                    using (FileStream fs = new FileStream(ratesFile, FileMode.Open))
                    {
                        rateSheets = (List<RateSheet>)serializer.Deserialize(fs);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading rate sheets: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            // If no rate sheets or error loading, create a default one
            if (rateSheets.Count == 0)
            {
                rateSheets.Add(CreateDefaultRateSheet());
            }

            return rateSheets;
        }

        private static RateSheet CreateDefaultRateSheet()
        {
            return new RateSheet("Default")
            {
                RegularLabourRate = 200,
                OvertimeLabourRate = 300,
                PremiumLabourRate = 400,
                RegularTravelRate = 160,
                OvertimeTravelRate = 240,
                PremiumTravelRate = 320,
                HotelCost = 120,
                PerDiemRate = 80,
                MileageRate = 0.70m,
                RentalCarRate = 120
            };
        }
    }
}