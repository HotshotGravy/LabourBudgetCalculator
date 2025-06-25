using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LabourBudgetCalculator.Models;
using LabourBudgetCalculator.Helpers;

namespace LabourBudgetCalculator
{
    public partial class SetupForm : Form
    {
        public List<RateSheet> UpdatedRateSheets { get; private set; }
        private List<RateSheet> rateSheets;
        private RateSheet currentRateSheet;
        private ListBox listBoxRateSheets;
        private bool isUpdatingRates = false;

        public SetupForm(List<RateSheet> existingRateSheets)
        {
            InitializeComponent(); // This calls the designer-generated method

            // Set form properties that weren't set in the designer
            this.Text = "Rate Sheet Setup";
            this.Size = new Size(600, 700);
            this.StartPosition = FormStartPosition.CenterParent;

            // Make a deep copy of the rate sheets
            rateSheets = new List<RateSheet>();
            foreach (var sheet in existingRateSheets)
            {
                rateSheets.Add(new RateSheet
                {
                    Name = sheet.Name,
                    RegularLabourRate = sheet.RegularLabourRate,
                    OvertimeLabourRate = sheet.OvertimeLabourRate,
                    PremiumLabourRate = sheet.PremiumLabourRate,
                    RegularTravelRate = sheet.RegularTravelRate,
                    OvertimeTravelRate = sheet.OvertimeTravelRate,
                    PremiumTravelRate = sheet.PremiumTravelRate,
                    HotelCost = sheet.HotelCost,
                    PerDiemRate = sheet.PerDiemRate,
                    MileageRate = sheet.MileageRate,
                    RentalCarRate = sheet.RentalCarRate,
                    FlightCost = sheet.FlightCost
                });
            }

            UpdatedRateSheets = rateSheets;

            // Create UI elements and set up their event handlers
            CreateUI();
            LoadRateSheets();
            SetupEventHandlers();
        }

        private void CreateUI()
        {
            // Set form properties
            this.Text = "Rate Sheet Setup";
            this.Size = new Size(600, 600);
            this.StartPosition = FormStartPosition.CenterParent;

            // Create list box for rate sheets
            listBoxRateSheets = new ListBox
            {
                Name = "listBoxRateSheets",
                Location = new Point(20, 20),
                Size = new Size(200, 400),
                SelectionMode = SelectionMode.One
            };
            this.Controls.Add(listBoxRateSheets);

            // Create group box for rate details
            GroupBox groupBoxRateDetails = new GroupBox
            {
                Name = "groupBoxRateDetails",
                Text = "Rate Sheet Details",
                Location = new Point(240, 20),
                Size = new Size(330, 400)
            };
            this.Controls.Add(groupBoxRateDetails);

            // Create labels and inputs for rate details
            Label lblName = new Label { Text = "Name:", Location = new Point(20, 30), AutoSize = true };
            TextBox txtName = new TextBox { Name = "txtName", Location = new Point(150, 27), Size = new Size(150, 25) };

            Label lblRegularLabour = new Label { Text = "Regular Labour Rate:", Location = new Point(20, 60), AutoSize = true };
            NumericUpDown numRegularLabour = new NumericUpDown
            {
                Name = "numRegularLabour",
                Location = new Point(150, 57),
                Size = new Size(80, 25),
                Maximum = 1000,
                DecimalPlaces = 2
            };

            Label lblOvertimeLabour = new Label { Text = "Overtime Labour Rate:", Location = new Point(20, 90), AutoSize = true };
            NumericUpDown numOvertimeLabour = new NumericUpDown
            {
                Name = "numOvertimeLabour",
                Location = new Point(150, 87),
                Size = new Size(80, 25),
                Maximum = 1000,
                DecimalPlaces = 2,
                ReadOnly = true,
                TabStop = false
            };

            Label lblPremiumLabour = new Label { Text = "Premium Labour Rate:", Location = new Point(20, 120), AutoSize = true };
            NumericUpDown numPremiumLabour = new NumericUpDown
            {
                Name = "numPremiumLabour",
                Location = new Point(150, 117),
                Size = new Size(80, 25),
                Maximum = 1000,
                DecimalPlaces = 2,
                ReadOnly = true,
                TabStop = false
            };

            Label lblRegularTravel = new Label { Text = "Regular Travel Rate:", Location = new Point(20, 150), AutoSize = true };
            NumericUpDown numRegularTravel = new NumericUpDown
            {
                Name = "numRegularTravel",
                Location = new Point(150, 147),
                Size = new Size(80, 25),
                Maximum = 1000,
                DecimalPlaces = 2
            };

            Label lblOvertimeTravel = new Label
            {
                Text = "Overtime Travel Rate:",
                Location = new Point(20, 180),
                AutoSize = true
            };
            NumericUpDown numOvertimeTravel = new NumericUpDown
            {
                Name = "numOvertimeTravel",
                Location = new Point(150, 177),
                Size = new Size(80, 25),
                Maximum = 1000,
                DecimalPlaces = 2,
                ReadOnly = true,
                TabStop = false
            };

            Label lblPremiumTravel = new Label { Text = "Premium Travel Rate:", Location = new Point(20, 210), AutoSize = true };
            NumericUpDown numPremiumTravel = new NumericUpDown
            {
                Name = "numPremiumTravel",
                Location = new Point(150, 207),
                Size = new Size(80, 25),
                Maximum = 1000,
                DecimalPlaces = 2,
                ReadOnly = true,
                TabStop = false
            };

            Label lblHotel = new Label { Text = "Hotel Cost:", Location = new Point(20, 240), AutoSize = true };
            NumericUpDown numHotel = new NumericUpDown
            {
                Name = "numHotel",
                Location = new Point(150, 237),
                Size = new Size(80, 25),
                Maximum = 1000,
                DecimalPlaces = 2
            };

            Label lblPerDiem = new Label { Text = "Per Diem Rate:", Location = new Point(20, 270), AutoSize = true };
            NumericUpDown numPDRate = new NumericUpDown
            {
                Name = "numPDRate",
                Location = new Point(150, 267),
                Size = new Size(80, 25),
                Maximum = 500,
                DecimalPlaces = 2
            };

            Label lblMileage = new Label { Text = "Mileage Rate:", Location = new Point(20, 300), AutoSize = true };
            NumericUpDown numMileage = new NumericUpDown
            {
                Name = "numMileage",
                Location = new Point(150, 297),
                Size = new Size(80, 25),
                Maximum = 10,
                DecimalPlaces = 2,
                Increment = 0.01m
            };

            Label lblRentalCar = new Label { Text = "Rental Car Rate:", Location = new Point(20, 330), AutoSize = true };
            NumericUpDown numRentalCar = new NumericUpDown
            {
                Name = "numRentalCar",
                Location = new Point(150, 327),
                Size = new Size(80, 25),
                Maximum = 500,
                DecimalPlaces = 2
            };

            Label lblFlightCost = new Label { Text = "Flight Cost:", Location = new Point(20, 360), AutoSize = true };
            NumericUpDown numFlightCost = new NumericUpDown
            {
                Name = "numFlightCost",
                Location = new Point(150, 357),
                Size = new Size(80, 25),
                Maximum = 2000,
                DecimalPlaces = 2
            };

            // Add explanation labels for calculated rates
            Label lblOvertimeExplanation = new Label
            {
                Text = "(1.5x Regular)",
                Location = new Point(235, 90),
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Italic)
            };

            Label lblPremiumExplanation = new Label
            {
                Text = "(2x Regular)",
                Location = new Point(235, 120),
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Italic)
            };

            Label lblOvertimeTravelExplanation = new Label
            {
                Text = "(1.5x Regular)",
                Location = new Point(235, 180),
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Italic)
            };

            Label lblPremiumTravelExplanation = new Label
            {
                Text = "(2x Regular)",
                Location = new Point(235, 210),
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Italic)
            };

            // Add controls to group box
            groupBoxRateDetails.Controls.AddRange(new Control[] {
               lblName, txtName,
               lblRegularLabour, numRegularLabour,
               lblOvertimeLabour, numOvertimeLabour, lblOvertimeExplanation,
               lblPremiumLabour, numPremiumLabour, lblPremiumExplanation,
               lblRegularTravel, numRegularTravel,
               lblOvertimeTravel, numOvertimeTravel, lblOvertimeTravelExplanation,
               lblPremiumTravel, numPremiumTravel, lblPremiumTravelExplanation,
               lblHotel, numHotel,
               lblPerDiem, numPDRate,
               lblMileage, numMileage,
               lblRentalCar, numRentalCar,
               lblFlightCost, numFlightCost
           });

            // Create buttons
            Button btnNew = new Button { Name = "btnNew", Text = "New", Location = new Point(20, 460), Size = new Size(80, 30) };
            Button btnSave = new Button { Name = "btnSave", Text = "Save", Location = new Point(110, 460), Size = new Size(80, 30) };
            Button btnDelete = new Button { Name = "btnDelete", Text = "Delete", Location = new Point(200, 460), Size = new Size(80, 30) };
            Button btnClose = new Button { Name = "btnClose", Text = "Close", Location = new Point(480, 460), Size = new Size(80, 30) };

            // Add buttons to form
            this.Controls.AddRange(new Control[] {
               btnNew, btnSave, btnDelete, btnClose
           });
        }

        private void LoadRateSheets()
        {
            ListBox listBoxRateSheets = (ListBox)Controls.Find("listBoxRateSheets", true)[0];
            listBoxRateSheets.Items.Clear();

            foreach (var sheet in rateSheets)
            {
                listBoxRateSheets.Items.Add(sheet.Name);
            }

            if (listBoxRateSheets.Items.Count > 0)
            {
                listBoxRateSheets.SelectedIndex = 0;
                currentRateSheet = rateSheets[0];
                DisplayRateSheet();
            }
        }

        private void SetupEventHandlers()
        {
            // Rate sheet selection change
            ListBox listBoxRateSheets = (ListBox)Controls.Find("listBoxRateSheets", true)[0];
            listBoxRateSheets.SelectedIndexChanged += (sender, e) =>
            {
                if (listBoxRateSheets.SelectedIndex >= 0 && listBoxRateSheets.SelectedIndex < rateSheets.Count)
                {
                    currentRateSheet = rateSheets[listBoxRateSheets.SelectedIndex];
                    DisplayRateSheet();
                }
            };

            // Set up event handlers for automatic rate calculations
            NumericUpDown numRegularLabour = (NumericUpDown)Controls.Find("numRegularLabour", true)[0];
            numRegularLabour.ValueChanged += (sender, e) =>
            {
                if (!isUpdatingRates)
                {
                    UpdateDerivedLabourRates(numRegularLabour.Value);
                }
            };

            NumericUpDown numRegularTravel = (NumericUpDown)Controls.Find("numRegularTravel", true)[0];
            numRegularTravel.ValueChanged += (sender, e) =>
            {
                if (!isUpdatingRates)
                {
                    UpdateDerivedTravelRates(numRegularTravel.Value);
                }
            };

            // New button
            Button btnNew = (Button)Controls.Find("btnNew", true)[0];
            btnNew.Click += (sender, e) =>
            {
                string newName = "New Rate Sheet";
                int counter = 1;

                // Check if name already exists
                bool nameExists = true;
                while (nameExists)
                {
                    nameExists = false;
                    foreach (var sheet in rateSheets)
                    {
                        if (sheet.Name == newName)
                        {
                            newName = $"New Rate Sheet {counter++}";
                            nameExists = true;
                            break;
                        }
                    }
                }

                // Create new rate sheet with rates properly calculated
                decimal regularLabor = 200m;
                decimal regularTravel = 160m;

                RateSheet newSheet = new RateSheet(newName)
                {
                    RegularLabourRate = regularLabor,
                    OvertimeLabourRate = regularLabor * 1.5m,
                    PremiumLabourRate = regularLabor * 2m,
                    RegularTravelRate = regularTravel,
                    OvertimeTravelRate = regularTravel * 1.5m,
                    PremiumTravelRate = regularTravel * 2m,
                    HotelCost = 120,
                    PerDiemRate = 80,
                    MileageRate = 0.70m,
                    RentalCarRate = 120,
                    FlightCost = 600
                };

                rateSheets.Add(newSheet);

                // Refresh list and select new sheet
                listBoxRateSheets = (ListBox)Controls.Find("listBoxRateSheets", true)[0];
                listBoxRateSheets.Items.Add(newName);
                listBoxRateSheets.SelectedIndex = listBoxRateSheets.Items.Count - 1;
                currentRateSheet = newSheet;
                DisplayRateSheet();
            };

            // Save button
            Button btnSave = (Button)Controls.Find("btnSave", true)[0];
            btnSave.Click += (sender, e) =>
            {
                if (currentRateSheet == null)
                    return;

                // Get values from form
                currentRateSheet.Name = ((TextBox)Controls.Find("txtName", true)[0]).Text;
                currentRateSheet.RegularLabourRate = ((NumericUpDown)Controls.Find("numRegularLabour", true)[0]).Value;
                currentRateSheet.OvertimeLabourRate = ((NumericUpDown)Controls.Find("numOvertimeLabour", true)[0]).Value;
                currentRateSheet.PremiumLabourRate = ((NumericUpDown)Controls.Find("numPremiumLabour", true)[0]).Value;
                currentRateSheet.RegularTravelRate = ((NumericUpDown)Controls.Find("numRegularTravel", true)[0]).Value;
                currentRateSheet.OvertimeTravelRate = ((NumericUpDown)Controls.Find("numOvertimeTravel", true)[0]).Value;
                currentRateSheet.PremiumTravelRate = ((NumericUpDown)Controls.Find("numPremiumTravel", true)[0]).Value;
                currentRateSheet.HotelCost = ((NumericUpDown)Controls.Find("numHotel", true)[0]).Value;
                currentRateSheet.PerDiemRate = ((NumericUpDown)Controls.Find("numPDRate", true)[0]).Value;
                currentRateSheet.MileageRate = ((NumericUpDown)Controls.Find("numMileage", true)[0]).Value;
                currentRateSheet.RentalCarRate = ((NumericUpDown)Controls.Find("numRentalCar", true)[0]).Value;
                currentRateSheet.FlightCost = ((NumericUpDown)Controls.Find("numFlightCost", true)[0]).Value;

                // Update list item
                listBoxRateSheets = (ListBox)Controls.Find("listBoxRateSheets", true)[0];
                if (listBoxRateSheets.SelectedIndex >= 0)
                {
                    listBoxRateSheets.Items[listBoxRateSheets.SelectedIndex] = currentRateSheet.Name;
                }

                // Save to file
                DataManager.SaveRateSheets(rateSheets);
                UpdatedRateSheets = rateSheets;

                MessageBox.Show("Rate sheet saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            // Delete button
            Button btnDelete = (Button)Controls.Find("btnDelete", true)[0];
            btnDelete.Click += (sender, e) =>
            {
                listBoxRateSheets = (ListBox)Controls.Find("listBoxRateSheets", true)[0];
                if (listBoxRateSheets.SelectedIndex >= 0)
                {
                    // Confirm deletion
                    if (MessageBox.Show($"Are you sure you want to delete the rate sheet '{currentRateSheet.Name}'?",
                        "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        // Don't allow deleting the last rate sheet
                        if (rateSheets.Count <= 1)
                        {
                            MessageBox.Show("Cannot delete the last rate sheet.", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        // Remove from list
                        int selectedIndex = listBoxRateSheets.SelectedIndex;
                        rateSheets.RemoveAt(selectedIndex);
                        listBoxRateSheets.Items.RemoveAt(selectedIndex);

                        // Select another sheet
                        if (selectedIndex >= rateSheets.Count)
                            selectedIndex = rateSheets.Count - 1;

                        if (selectedIndex >= 0)
                        {
                            listBoxRateSheets.SelectedIndex = selectedIndex;
                            currentRateSheet = rateSheets[selectedIndex];
                            DisplayRateSheet();
                        }

                        // Save to file
                        DataManager.SaveRateSheets(rateSheets);
                        UpdatedRateSheets = rateSheets;
                    }
                }
            };

            // Close button
            Button btnClose = (Button)Controls.Find("btnClose", true)[0];
            btnClose.Click += (sender, e) =>
            {
                DialogResult = DialogResult.OK;
                Close();
            };

            // Form closing
            this.FormClosing += (sender, e) =>
            {
                if (DialogResult != DialogResult.OK)
                {
                    DialogResult = DialogResult.OK;
                }
            };
        }

        private void UpdateDerivedLabourRates(decimal regularRate)
        {
            isUpdatingRates = true;
            try
            {
                // Calculate overtime and premium rates
                decimal overtimeRate = regularRate * 1.5m;
                decimal premiumRate = regularRate * 2m;

                // Update UI
                ((NumericUpDown)Controls.Find("numOvertimeLabour", true)[0]).Value = overtimeRate;
                ((NumericUpDown)Controls.Find("numPremiumLabour", true)[0]).Value = premiumRate;
            }
            finally
            {
                isUpdatingRates = false;
            }
        }

        private void UpdateDerivedTravelRates(decimal regularRate)
        {
            isUpdatingRates = true;
            try
            {
                // Calculate overtime and premium rates
                decimal overtimeRate = regularRate * 1.5m;
                decimal premiumRate = regularRate * 2m;

                // Update UI
                ((NumericUpDown)Controls.Find("numOvertimeTravel", true)[0]).Value = overtimeRate;
                ((NumericUpDown)Controls.Find("numPremiumTravel", true)[0]).Value = premiumRate;
            }
            finally
            {
                isUpdatingRates = false;
            }
        }

        private void DisplayRateSheet()
        {
            if (currentRateSheet == null)
                return;

            isUpdatingRates = true;
            try
            {
                ((TextBox)Controls.Find("txtName", true)[0]).Text = currentRateSheet.Name;

                // Set regular rates first
                ((NumericUpDown)Controls.Find("numRegularLabour", true)[0]).Value = currentRateSheet.RegularLabourRate;
                ((NumericUpDown)Controls.Find("numRegularTravel", true)[0]).Value = currentRateSheet.RegularTravelRate;

                // Set derived rates
                ((NumericUpDown)Controls.Find("numOvertimeLabour", true)[0]).Value = currentRateSheet.OvertimeLabourRate;
                ((NumericUpDown)Controls.Find("numPremiumLabour", true)[0]).Value = currentRateSheet.PremiumLabourRate;
                ((NumericUpDown)Controls.Find("numOvertimeTravel", true)[0]).Value = currentRateSheet.OvertimeTravelRate;
                ((NumericUpDown)Controls.Find("numPremiumTravel", true)[0]).Value = currentRateSheet.PremiumTravelRate;

                // Set other rates
                ((NumericUpDown)Controls.Find("numHotel", true)[0]).Value = currentRateSheet.HotelCost;
                ((NumericUpDown)Controls.Find("numPDRate", true)[0]).Value = currentRateSheet.PerDiemRate;
                ((NumericUpDown)Controls.Find("numMileage", true)[0]).Value = currentRateSheet.MileageRate;
                ((NumericUpDown)Controls.Find("numRentalCar", true)[0]).Value = currentRateSheet.RentalCarRate;
                ((NumericUpDown)Controls.Find("numFlightCost", true)[0]).Value = currentRateSheet.FlightCost;
            }
            finally
            {
                isUpdatingRates = false;
            }
        }
    }
}
