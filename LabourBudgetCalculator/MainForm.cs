using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TimeExpenseCalculator.Models;
using TimeExpenseCalculator.Helpers;
using TimeExpenseCalculator.Forms;
using System.IO;
using System.Xml;

using OfficeOpenXml;
using OfficeOpenXml.Style;
// using System.Reflection.Emit;

namespace LabourBudgetCalculator
{
    public partial class MainForm : Form
    {

        private List<RateSheet> rateSheets;
        private RateSheet currentRateSheet;
        private Label lblGrandTotalSection;
        private Label lblGrandTotal;
        private DataGridView dataGridViewDays;
        private Label lblTotalDaysValue, lblLabourHoursValue;
        private Label lblLabourCostValue, lblTravelHoursValue;
        private Label lblTravelCostValue, lblExpensesCostValue;
        private ToolTip toolTip;
        private Button btnReset;

        // Dark mode stuff
        private bool isDarkMode = false;
        private Button btnDarkMode;
        private Color darkBackColor = Color.FromArgb(40, 44, 52);
        private Color darkTextColor = Color.FromArgb(220, 223, 228);
        private Color darkControlBackColor = Color.FromArgb(54, 60, 69);
        private Color darkBorderColor = Color.FromArgb(90, 100, 120);
        private Color darkButtonBackColor = Color.FromArgb(60, 70, 85);
        private Color darkButtonForeColor = Color.FromArgb(220, 223, 228);
        private Color darkPanelBackColor = Color.FromArgb(50, 55, 65);
        private Color darkGridBackColor = Color.FromArgb(45, 50, 60);
        private Color darkGridHeaderBackColor = Color.FromArgb(60, 70, 85);
        private Color darkGridCellBackColor = Color.FromArgb(55, 65, 80);

        // Dark mode color scheme for day panels
        private Color darkModeTravelDay = Color.FromArgb(130, 110, 30); // Darker yellow
        private Color darkModeWorkDay = Color.FromArgb(30, 80, 130);    // Darker blue
        private Color darkModeHoldoverDay = Color.FromArgb(30, 130, 70); // Darker green

        // Light mode original colors (for toggling back)
        private Color lightBackColor;
        private Color lightTextColor;
        private Color lightControlBackColor;
        private Color lightPanelBackColor;
        private Color lightGridBackColor;
        private Color lightTravelDay = Color.Yellow;
        private Color lightWorkDay = Color.LightBlue;
        private Color lightHoldoverDay = Color.LightGreen;






        public MainForm()
        {
            InitializeComponent();
            this.Load += MainForm_Load;

            this.Size = new Size(1438, 920);
            this.Text = "Time & Expense Calculator";
            this.StartPosition = FormStartPosition.CenterScreen;

            SetupUI();

            LoadData();
        }


        private void MainForm_Load(object sender, EventArgs e)
        {
            // Set minimum size to ensure all controls are visible
            // this.MinimumSize = new Size(1450, 920);
            this.Size = new Size(1380, 850);
            //this.MinimumSize = new Size(1380, 850);

            // Make the form resizable
            this.FormBorderStyle = FormBorderStyle.Fixed3D;

            // Add anchoring to key controls
            GroupBox groupBoxResults = (GroupBox)Controls.Find("groupBoxResults", true)[0];
            groupBoxResults.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            DataGridView dataGridViewDays = (DataGridView)Controls.Find("dataGridViewDays", true)[0];
            dataGridViewDays.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // Find the Grand Total panel we created and set its anchoring
            foreach (Control control in groupBoxResults.Controls)
            {
                if (control is Panel && control.Contains(lblGrandTotal))
                {
                    // Anchor the Grand Total panel to the right side
                    control.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                    break;
                }
            }
            lightBackColor = this.BackColor;
            lightTextColor = this.ForeColor;
            lightControlBackColor = SystemColors.Control;
            lightPanelBackColor = SystemColors.Control;
            lightGridBackColor = SystemColors.Window;

            LoadDarkModePreference();


        }
        private void SetupUI()
        {
            // Set form properties
            this.Text = "Time & Expense Calculator";
            this.Size = new Size(1438, 920);
            this.StartPosition = FormStartPosition.CenterScreen;

            CreateRatesSection();
            CreateDaysSection();
            CreateTravelSection();
            CreateExpensesSection();
            CreateProjectInfoSection();
            CreateCalendarSection();
            CreateResultsSection();

            EnhanceVisualAppearance();

            // Setup event handlers
            SetupEventHandlers();
            SetupTravelTimeDistanceLink();
            SetupTooltips();
        }

        private void CreateRatesSection()
        {
            GroupBox groupBoxRates = new GroupBox
            {
                Name = "groupBoxRates",
                Text = "Rates",
                Location = new Point(25, 25),  // More spacing
                Size = new Size(420, 180)   // Changed from 200 to 220
            };
            this.Controls.Add(groupBoxRates);

            // Add label and combobox for rate sheet selection
            Label lblRateSheet = new Label { Text = "Rate Sheet:", Location = new Point(20, 23), AutoSize = true };
            ComboBox comboBoxRateSheet = new ComboBox
            {
                Name = "comboBoxRateSheet",
                Location = new Point(120, 20),
                Size = new Size(180, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Discount field
            Label lblDiscount = new Label { Text = "Discount", Location = new Point(20, 50), AutoSize = true };
            NumericUpDown numDiscount = new NumericUpDown
            {
                Name = "numDiscount",
                Location = new Point(120, 48),
                Size = new Size(60, 25),
                Maximum = 100,
                Minimum = 0,
                Value = 0
            };
            Label lblPercentage = new Label { Text = "%", Location = new Point(181, 50), AutoSize = true };

            // Emergency checkbox
            CheckBox chkEmergency = new CheckBox
            {
                Name = "chkEmergency",
                Text = "Emergency",
                Location = new Point(220, 50),
                AutoSize = true
            };

            // Add labels and textboxes for rates
            Label lblRegularLabour = new Label { Text = "Regular Labour", Location = new Point(20, 100), AutoSize = true };
            Label lblOvertimeLabour = new Label { Text = "Overtime Labour", Location = new Point(20, 125), AutoSize = true };
            Label lblPremiumLabour = new Label { Text = "Premium Labour", Location = new Point(20, 150), AutoSize = true };
            Label lblRegularTravel = new Label { Text = "Regular Travel", Location = new Point(220, 100), AutoSize = true };
            Label lblOvertimeTravel = new Label { Text = "Overtime Travel", Location = new Point(220, 125), AutoSize = true };
            Label lblPremiumTravel = new Label { Text = "Premium Travel", Location = new Point(220, 150), AutoSize = true };

            TextBox txtRegularLabour = new TextBox { Name = "txtRegularLabour", Location = new Point(120, 97), Size = new Size(60, 25), ReadOnly = true };
            TextBox txtOvertimeLabour = new TextBox { Name = "txtOvertimeLabour", Location = new Point(120, 122), Size = new Size(60, 25), ReadOnly = true };
            TextBox txtPremiumLabour = new TextBox { Name = "txtPremiumLabour", Location = new Point(120, 147), Size = new Size(60, 25), ReadOnly = true };
            TextBox txtRegularTravel = new TextBox { Name = "txtRegularTravel", Location = new Point(320, 97), Size = new Size(60, 25), ReadOnly = true };
            TextBox txtOvertimeTravel = new TextBox { Name = "txtOvertimeTravel", Location = new Point(320, 122), Size = new Size(60, 25), ReadOnly = true };
            TextBox txtPremiumTravel = new TextBox { Name = "txtPremiumTravel", Location = new Point(320, 147), Size = new Size(60, 25), ReadOnly = true };

            Label lblHr1 = new Label { Text = "/hr.", Location = new Point(181, 100), AutoSize = true };
            Label lblHr2 = new Label { Text = "/hr.", Location = new Point(181, 125), AutoSize = true };
            Label lblHr3 = new Label { Text = "/hr.", Location = new Point(181, 150), AutoSize = true };
            Label lblHr4 = new Label { Text = "/hr.", Location = new Point(381, 100), AutoSize = true };
            Label lblHr5 = new Label { Text = "/hr.", Location = new Point(381, 125), AutoSize = true };
            Label lblHr6 = new Label { Text = "/hr.", Location = new Point(381, 150), AutoSize = true };

            // Add all controls to the group box
            groupBoxRates.Controls.AddRange(new Control[] {
                lblRateSheet, comboBoxRateSheet,
                lblDiscount, numDiscount, lblPercentage,
                chkEmergency,
                lblRegularLabour, txtRegularLabour, lblHr1,
                lblOvertimeLabour, txtOvertimeLabour, lblHr2,
                lblPremiumLabour, txtPremiumLabour, lblHr3,
                lblRegularTravel, txtRegularTravel, lblHr4,
                lblOvertimeTravel, txtOvertimeTravel, lblHr5,
                lblPremiumTravel, txtPremiumTravel, lblHr6
            });
        }

        private void CreateDaysSection()
        {
            // Create Days Configuration Section
            GroupBox groupBoxDays = new GroupBox
            {
                Name = "groupBoxDays",
                Text = "Days Configuration",
                Location = new Point(460, 25),
                Size = new Size(420, 180)  // Reduced height from 220 to 180
            };
            this.Controls.Add(groupBoxDays);

            Label lblDaysOnSite = new Label
            {
                Text = "Days on Site:",
                Location = new Point(20, 30),
                AutoSize = true
            };

            NumericUpDown numDaysOnSite = new NumericUpDown
            {
                Name = "numDaysOnSite",
                Location = new Point(120, 27),
                Size = new Size(60, 25),
                Minimum = 1,
                Maximum = 30,
                Value = 1
            };

            Label lblHoursPerDay = new Label
            {
                Text = "Hours per Day:",
                Location = new Point(220, 30),
                AutoSize = true
            };

            NumericUpDown numHoursPerDay = new NumericUpDown
            {
                Name = "numHoursPerDay",
                Location = new Point(320, 27),
                Size = new Size(60, 25),
                Minimum = 1,
                Maximum = 24,
                Value = 10
            };

            Label lblStartDay = new Label
            {
                Text = "Start Day On Site:",
                Location = new Point(20, 70),
                AutoSize = true
            };

            ComboBox comboBoxStartDay = new ComboBox
            {
                Name = "comboBoxStartDay",
                Location = new Point(140, 67),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            CheckBox chkHoldoverDay = new CheckBox
            {
                Name = "chkHoldoverDay",
                Text = "Holdover Day",
                Location = new Point(20, 110),
                AutoSize = true
            };

            ComboBox comboBoxHoldoverDay = new ComboBox
            {
                Name = "comboBoxHoldoverDay",
                Location = new Point(140, 107),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };

            // Add controls to groupBoxDays
            groupBoxDays.Controls.AddRange(new Control[] {
        lblDaysOnSite, numDaysOnSite,
        lblHoursPerDay, numHoursPerDay,
        lblStartDay, comboBoxStartDay,
        chkHoldoverDay, comboBoxHoldoverDay
    });
        }


        private void CreateTravelSection()
        {
            // Create Travel Options Section
            GroupBox groupBoxTravel = new GroupBox
            {
                Name = "groupBoxTravel",
                Text = "Travel Options",
                Location = new Point(25, 215), // Changed from 300 to 220 (moved up 80 points)
                Size = new Size(420, 260)
            };
            this.Controls.Add(groupBoxTravel);

            // Separate Travel Day checkboxes
            Label lblSeparateTravel = new Label { Text = "Separate Travel Day", Location = new Point(20, 30), AutoSize = true };
            CheckBox chkSeparateTravelTo = new CheckBox { Name = "chkSeparateTravelTo", Text = "To", Location = new Point(255, 30), AutoSize = true };
            CheckBox chkSeparateTravelFrom = new CheckBox { Name = "chkSeparateTravelFrom", Text = "From", Location = new Point(295, 30), AutoSize = true };

            // Travel Method
            Label lblTravelMethod = new Label { Text = "Travel Method to Site Area:", Location = new Point(20, 60), AutoSize = true };
            ComboBox comboBoxTravelMethod = new ComboBox
            {
                Name = "comboBoxTravelMethod",
                Location = new Point(255, 57),
                Size = new Size(75, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Travel Distance
            Label lblTravelDistance = new Label { Text = "Driving Distance (First and Last Days Only):", Location = new Point(20, 90), AutoSize = true };
            NumericUpDown numTravelDistance = new NumericUpDown
            {
                Name = "numTravelDistance",
                Location = new Point(255, 87),
                Size = new Size(75, 25),
                Maximum = 10000
            };
            Label lblTravelDistanceUnit = new Label { Text = "miles / km", Location = new Point(335, 90), AutoSize = true };

            // Total Travel Time
            Label lblTravelTime = new Label { Text = "Total Travel Time to Site Area (Including Flight):", Location = new Point(20, 120), AutoSize = true };
            NumericUpDown numTravelTime = new NumericUpDown
            {
                Name = "numTravelTime",
                Location = new Point(255, 117),
                Size = new Size(75, 25),
                Maximum = 48,
                Increment = 0.5m,
                DecimalPlaces = 1
            };
            Label lblTravelTimeUnit = new Label { Text = "hours", Location = new Point(335, 120), AutoSize = true };

            // Daily Travel Distance
            Label lblDailyTravelDistance = new Label { Text = "Daily Driving Distance (One Way):", Location = new Point(20, 150), AutoSize = true };

            NumericUpDown numDailyTravelDistance = new NumericUpDown
            {
                Name = "numDailyTravelDistance",
                Location = new Point(255, 147),
                Size = new Size(75, 25),
                Maximum = 1000,
                Increment = 15m
            };
            Label lblDailyTravelDistanceUnit = new Label { Text = "miles / km", Location = new Point(335, 150), AutoSize = true };

            // Daily Travel Time
            Label lblDailyTravelTime = new Label { Text = "Daily Travel Time (One way):", Location = new Point(20, 180), AutoSize = true };
            NumericUpDown numDailyTravelTime = new NumericUpDown
            {
                Name = "numDailyTravelTime",
                Location = new Point(255, 177),
                Size = new Size(75, 25),
                Maximum = 24,
                Increment = 0.25m,
                DecimalPlaces = 2
            };
            Label lblDailyTravelTimeUnit = new Label { Text = "hours", Location = new Point(335, 180), AutoSize = true };

            // Add controls to groupBoxTravel
            groupBoxTravel.Controls.AddRange(new Control[] {
        lblSeparateTravel, chkSeparateTravelTo, chkSeparateTravelFrom,
        lblTravelMethod, comboBoxTravelMethod,
        lblTravelDistance, numTravelDistance, lblTravelDistanceUnit,
        lblTravelTime, numTravelTime, lblTravelTimeUnit,
        lblDailyTravelDistance, numDailyTravelDistance, lblDailyTravelDistanceUnit,
        lblDailyTravelTime, numDailyTravelTime, lblDailyTravelTimeUnit
    });
        }

        private void CreateExpensesSection()
        {
            // Create Expenses Section
            GroupBox groupBoxExpenses = new GroupBox
            {
                Name = "groupBoxExpenses",
                Text = "Expenses",
                Location = new Point(25, 485),
                Size = new Size(420, 310)
            };
            this.Controls.Add(groupBoxExpenses);

            // Flight Cost
            Label lblFlightCost = new Label { Text = "Flight Cost (One Way):", Location = new Point(20, 30), AutoSize = true };
            NumericUpDown numFlightCost = new NumericUpDown
            {
                Name = "numFlightCost",
                Location = new Point(170, 27),
                Size = new Size(80, 25),
                Maximum = 10000
            };
            Label lblFlightCostUnit = new Label { Text = "", Location = new Point(255, 30), AutoSize = true };

            // Rental Car
            CheckBox chkRentalCar = new CheckBox { Name = "chkRentalCar", Text = "Rental Car", Location = new Point(20, 60), AutoSize = true };
            Label lblDailyCost = new Label { Text = "per day", Location = new Point(255, 60), AutoSize = true };
            NumericUpDown numRentalCarCost = new NumericUpDown
            {
                Name = "numRentalCarCost",
                Location = new Point(170, 57),
                Size = new Size(80, 25),
                Maximum = 500
            };
            Label lblRentalCarCostUnit = new Label { Text = "", Location = new Point(255, 60), AutoSize = true };

            CheckBox chkHotel = new CheckBox
            {
                Name = "chkHotel",
                Text = "Hotel",
                Location = new Point(20, 90),
                AutoSize = true
            };

            Label lblHotelCost = new Label
            {
                Text = "",
                Location = new Point(110, 90),
                AutoSize = true
            };

            NumericUpDown numHotelCost = new NumericUpDown
            {
                Name = "numHotelCost",
                Location = new Point(170, 87),
                Size = new Size(80, 25),
                Maximum = 1000
            };

            Label lblHotelCostUnit = new Label
            {
                Text = "per night",
                Location = new Point(255, 90),
                AutoSize = true
            };

            // Mileage Rate
            Label lblMileageRate = new Label { Text = "Mileage:", Location = new Point(20, 120), AutoSize = true };
            NumericUpDown numMileageRate = new NumericUpDown
            {
                Name = "numMileageRate",
                Location = new Point(170, 117),
                Size = new Size(80, 25),
                Maximum = 10,
                DecimalPlaces = 2,
                Increment = 0.01m
            };
            Label lblMileageRateUnit = new Label { Text = "per mile / km", Location = new Point(255, 120), AutoSize = true };

            // Per Diem
            Label lblPerDiem = new Label { Text = "Per Diem:", Location = new Point(20, 150), AutoSize = true };
            NumericUpDown numPerDiem = new NumericUpDown
            {
                Name = "numPerDiem",
                Location = new Point(170, 147),
                Size = new Size(80, 25),
                Maximum = 500
            };
            Label lblPerDiemUnit = new Label { Text = "per day", Location = new Point(255, 150), AutoSize = true };

            // Edit Rate Sheets button
            Button btnSetup = new Button
            {
                Name = "btnSetup",
                Text = "Edit Rate Sheets",
                Location = new Point(20, 190),
                Size = new Size(150, 25)
            };

            // Reset button - positioned to the right of Setup button
            btnReset = new Button
            {
                Name = "btnReset",
                Text = "Reset",
                Location = new Point(180, 190), // Position it right after the Setup button
                Size = new Size(100, 25)
            };

            // Add the reset button click handler
            btnReset.Click += (sender, e) =>
            {
                DialogResult result = MessageBox.Show(
                    "Are you sure you want to reset all values to default?",
                    "Confirm Reset",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    ResetForm();
                }
            };

            // Add Export button below Setup Rate Sheets
            Button btnExport = new Button
            {
                Name = "btnExport",
                Text = "Export to Excel",
                Location = new Point(20, 220), // Positioned below Setup button
                Size = new Size(150, 25)
            };

            // Add the export button click handler
            btnExport.Click += (sender, e) => ExportToExcel();


            btnDarkMode = new Button
            {
                Name = "btnDarkMode",
                Text = "",
                Location = new Point(290, 190), // Position it right after Reset button
                Size = new Size(9, 25),
                BackColor = Color.DarkGray
            };

            btnDarkMode.Click += (sender, e) => ToggleDarkMode();




            // Add controls to groupBoxExpenses including btnReset
            groupBoxExpenses.Controls.AddRange(new Control[] {
    lblFlightCost, numFlightCost, lblFlightCostUnit,
    chkRentalCar, lblDailyCost, numRentalCarCost, lblRentalCarCostUnit,
    chkHotel, lblHotelCost, numHotelCost, lblHotelCostUnit,  // Added chkHotel here
    lblMileageRate, numMileageRate, lblMileageRateUnit,
    lblPerDiem, numPerDiem, lblPerDiemUnit,
    btnSetup, btnReset, btnDarkMode, btnExport
});
        }

        private void CreateProjectInfoSection()
        {
            // Create Project Info Section
            GroupBox groupBoxProjectInfo = new GroupBox
            {
                Name = "groupBoxProjectInfo",
                Text = "Project Info",
                Location = new Point(890, 25), // Aligned with Days Configuration top
                Size = new Size(450, 180)  // Match height of other top panels
            };
            this.Controls.Add(groupBoxProjectInfo);

            // Project Number
            Label lblProjectNumber = new Label
            {
                Text = "Project Number:",
                Location = new Point(20, 30),
                AutoSize = true
            };

            TextBox txtProjectNumber = new TextBox
            {
                Name = "txtProjectNumber",
                Text = "", // Default empty
                Location = new Point(120, 27),
                Size = new Size(100, 25),
            };

            // Customer
            Label lblCustomer = new Label
            {
                Text = "Customer:",
                Location = new Point(230, 30),
                AutoSize = true
            };

            TextBox txtCustomer = new TextBox
            {
                Name = "txtCustomer",
                Text = "", // Default empty
                Location = new Point(290, 27),
                Size = new Size(140, 25),
            };

            // Technician Name - moved down
            Label lblTechnician = new Label
            {
                Text = "Technician:",
                Location = new Point(20, 60), // Moved down
                AutoSize = true
            };

            TextBox txtTechnician = new TextBox
            {
                Name = "txtTechnician",
                Text = "Unspecified", // Default value
                Location = new Point(120, 57), // Moved down
                Size = new Size(200, 25),
            };

            // Start Date - moved down
            Label lblStartDate = new Label
            {
                Text = "Start Date:",
                Location = new Point(20, 90), // Moved down
                AutoSize = true
            };

            DateTimePicker dtpStartDate = new DateTimePicker
            {
                Name = "dtpStartDate",
                Location = new Point(120, 87), // Moved down
                Size = new Size(200, 25),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "MMMM d, yyyy",
                ShowCheckBox = true,
                Checked = false
            };

            Button btnClearStartDate = new Button
            {
                Name = "btnClearStartDate",
                Text = "Clear",
                Location = new Point(330, 87), // Moved down
                Size = new Size(60, 25),
            };

            // End Date - moved down
            Label lblEndDate = new Label
            {
                Text = "End Date:",
                Location = new Point(20, 120), // Moved down
                AutoSize = true
            };

            DateTimePicker dtpEndDate = new DateTimePicker
            {
                Name = "dtpEndDate",
                Location = new Point(120, 117), // Moved down
                Size = new Size(200, 25),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "MMMM d, yyyy",
                ShowCheckBox = true,
                Checked = false
            };

            Button btnClearEndDate = new Button
            {
                Name = "btnClearEndDate",
                Text = "Clear",
                Location = new Point(330, 117), // Moved down
                Size = new Size(60, 25),
            };

            // Add controls to groupBoxProjectInfo
            groupBoxProjectInfo.Controls.AddRange(new Control[] {
        lblProjectNumber, txtProjectNumber,
        lblCustomer, txtCustomer,
        lblTechnician, txtTechnician,
        lblStartDate, dtpStartDate, btnClearStartDate,
        lblEndDate, dtpEndDate, btnClearEndDate
    });

            // Set up event handlers for the date controls
            SetupDateControlEvents(dtpStartDate, dtpEndDate, btnClearStartDate, btnClearEndDate);
        }

        // These are separate methods at the class level - not inside CreateProjectInfoSection
        private void SetupDateControlEvents(DateTimePicker dtpStartDate, DateTimePicker dtpEndDate,
                                            Button btnClearStartDate, Button btnClearEndDate)
        {
            // When unchecked, show "Not specified"
            dtpStartDate.ValueChanged += (s, e) =>
            {
                if (!dtpStartDate.Checked)
                {
                    dtpStartDate.CustomFormat = " ";
                }
                else
                {
                    dtpStartDate.CustomFormat = "MMMM d, yyyy";
                    UpdateStartDayFromDate(dtpStartDate.Value);
                }
                UpdateScheduleDates();
            };

            dtpEndDate.ValueChanged += (s, e) =>
            {
                if (!dtpEndDate.Checked)
                {
                    dtpEndDate.CustomFormat = " ";
                }
                else
                {
                    dtpEndDate.CustomFormat = "MMMM d, yyyy";
                    ValidateDateRange();
                }
                UpdateScheduleDates();
            };

            // Clear button handlers
            btnClearStartDate.Click += (s, e) =>
            {
                dtpStartDate.Checked = false;
                dtpStartDate.CustomFormat = " ";
                EnableStartDayControl(true);
                UpdateScheduleDates();
            };

            btnClearEndDate.Click += (s, e) =>
            {
                dtpEndDate.Checked = false;
                dtpEndDate.CustomFormat = " ";
                EnableDaysOnSiteControl(true);
                UpdateScheduleDates();
            };

            // Initialize date pickers as "Not specified"
            dtpStartDate.CustomFormat = " ";
            dtpEndDate.CustomFormat = " ";
        }

        private void UpdateStartDayFromDate(DateTime startDate)
        {
            // Get the day of week from the start date
            int dayOfWeek = (int)startDate.DayOfWeek;

            // Convert to the app's day of week format (0-6 where 0 is Monday)
            // DayOfWeek enum: 0=Sunday, 1=Monday, ... 6=Saturday
            int appDayOfWeek = dayOfWeek == 0 ? 6 : dayOfWeek - 1;

            // Update the start day combobox
            ComboBox comboBoxStartDay = (ComboBox)Controls.Find("comboBoxStartDay", true)[0];
            comboBoxStartDay.SelectedIndex = appDayOfWeek;

            // Gray out the control
            EnableStartDayControl(false);

            // If end date is also set, calculate days on site
            DateTimePicker dtpEndDate = (DateTimePicker)Controls.Find("dtpEndDate", true)[0];
            if (dtpEndDate.Checked)
            {
                CalculateDaysOnSite();
            }
        }

        private void EnableStartDayControl(bool enabled)
        {
            // Enable/disable the start day combobox
            ComboBox comboBoxStartDay = (ComboBox)Controls.Find("comboBoxStartDay", true)[0];
            comboBoxStartDay.Enabled = enabled;
            comboBoxStartDay.BackColor = enabled ? SystemColors.Window : SystemColors.Control;
        }

        private void EnableDaysOnSiteControl(bool enabled)
        {
            // Enable/disable the days on site numeric control
            NumericUpDown numDaysOnSite = (NumericUpDown)Controls.Find("numDaysOnSite", true)[0];
            numDaysOnSite.Enabled = enabled;
            numDaysOnSite.BackColor = enabled ? SystemColors.Window : SystemColors.Control;
        }

        private void ValidateDateRange()
        {
            DateTimePicker dtpStartDate = (DateTimePicker)Controls.Find("dtpStartDate", true)[0];
            DateTimePicker dtpEndDate = (DateTimePicker)Controls.Find("dtpEndDate", true)[0];

            // Only validate if both dates are specified
            if (dtpStartDate.Checked && dtpEndDate.Checked)
            {
                TimeSpan duration = dtpEndDate.Value.Date - dtpStartDate.Value.Date;
                int days = duration.Days + 1; // Include both start and end days

                if (days > 14 || days <= 0)
                {
                    MessageBox.Show(
                        "The date range must be between 1 and 14 days. End date has been reset.",
                        "Invalid Date Range",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    dtpEndDate.Checked = false;
                    dtpEndDate.CustomFormat = " ";
                    EnableDaysOnSiteControl(true);
                }
                else
                {
                    // Set days on site
                    NumericUpDown numDaysOnSite = (NumericUpDown)Controls.Find("numDaysOnSite", true)[0];
                    numDaysOnSite.Value = days;
                    EnableDaysOnSiteControl(false);
                }
            }
            else if (!dtpStartDate.Checked && dtpEndDate.Checked)
            {
                // Calculate start date based on end date and days on site
                CalculateStartDateFromEnd();
            }
        }

        private void CalculateDaysOnSite()
        {
            DateTimePicker dtpStartDate = (DateTimePicker)Controls.Find("dtpStartDate", true)[0];
            DateTimePicker dtpEndDate = (DateTimePicker)Controls.Find("dtpEndDate", true)[0];

            if (dtpStartDate.Checked && dtpEndDate.Checked)
            {
                TimeSpan duration = dtpEndDate.Value.Date - dtpStartDate.Value.Date;
                int days = duration.Days + 1; // Include both start and end days

                NumericUpDown numDaysOnSite = (NumericUpDown)Controls.Find("numDaysOnSite", true)[0];
                numDaysOnSite.Value = days;
                EnableDaysOnSiteControl(false);
            }
        }

        private void CalculateStartDateFromEnd()
        {
            DateTimePicker dtpStartDate = (DateTimePicker)Controls.Find("dtpStartDate", true)[0];
            DateTimePicker dtpEndDate = (DateTimePicker)Controls.Find("dtpEndDate", true)[0];
            NumericUpDown numDaysOnSite = (NumericUpDown)Controls.Find("numDaysOnSite", true)[0];

            if (dtpEndDate.Checked && !dtpStartDate.Checked)
            {
                // Calculate start date by subtracting days on site - 1
                int daysOnSite = (int)numDaysOnSite.Value;
                DateTime startDate = dtpEndDate.Value.Date.AddDays(-(daysOnSite - 1));

                // Set the start date
                dtpStartDate.Value = startDate;
                dtpStartDate.Checked = true;
                dtpStartDate.CustomFormat = "MMMM d, yyyy";

                // Update start day and disable it
                UpdateStartDayFromDate(startDate);
            }
        }

        private void UpdateScheduleDates()
        {
            DateTimePicker dtpStartDate = (DateTimePicker)Controls.Find("dtpStartDate", true)[0];
            NumericUpDown numDaysOnSite = (NumericUpDown)Controls.Find("numDaysOnSite", true)[0];
            CheckBox chkSeparateTravelTo = (CheckBox)Controls.Find("chkSeparateTravelTo", true)[0];
            CheckBox chkSeparateTravelFrom = (CheckBox)Controls.Find("chkSeparateTravelFrom", true)[0];

            // Calculate total days
            int daysOnSite = (int)numDaysOnSite.Value;
            int totalDays = daysOnSite;
            if (chkSeparateTravelTo.Checked) totalDays++;
            if (chkSeparateTravelFrom.Checked) totalDays++;

            // Update day panels
            for (int i = 0; i < 14; i++)
            {
                try
                {
                    Panel dayPanel = (Panel)Controls.Find($"dayPanel{i + 1}", true)[0];
                    Label dayNumLabel = (Label)dayPanel.Controls.Find($"dayNumLabel{i + 1}", false)[0];

                    if (i < totalDays && dtpStartDate.Checked)
                    {
                        // Calculate the date for this day
                        DateTime dayDate;

                        if (chkSeparateTravelTo.Checked && i == 0)
                        {
                            // First day is travel day, one day before start date
                            dayDate = dtpStartDate.Value.Date.AddDays(-1);
                        }
                        else if (chkSeparateTravelTo.Checked)
                        {
                            // Adjust for separate travel TO day
                            dayDate = dtpStartDate.Value.Date.AddDays(i - 1);
                        }
                        else
                        {
                            // No separate travel day, start with the start date
                            dayDate = dtpStartDate.Value.Date.AddDays(i);
                        }

                        // Set the day label to show the date
                        dayNumLabel.Text = dayDate.ToString("MMM d");
                    }
                    else
                    {
                        // Reset to default "Day X" format
                        dayNumLabel.Text = $"Day {i + 1}";
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating day panel {i + 1}: {ex.Message}");
                }
            }
        }

        private void CreateCalendarSection()
        {
            // Create Calendar/Day Visualization Section
            GroupBox groupBoxCalendar = new GroupBox
            {
                Name = "groupBoxCalendar",
                Text = "Schedule",
                Location = new Point(460, 215), // Moved down by adjusting Y coordinate (from 260 to 215)
                Size = new Size(880, 220)       // Increased height from 180 to 220
            };
            this.Controls.Add(groupBoxCalendar);

            // Create two rows of 7 days each (14 days total)
            int dayWidth = 120;    // Maintain width
            int dayHeight = 85;    // Increased height from 70 to 85
            int startX = 15;
            int startY1 = 30;
            int startY2 = 125;     // Adjusted second row position (from 105 to 125)

            // First row days (1-7)
            for (int i = 0; i < 7; i++)
            {
                Panel dayPanel = new Panel
                {
                    Name = $"dayPanel{i + 1}",
                    Location = new Point(startX + (i * dayWidth), startY1),
                    Size = new Size(dayWidth, dayHeight),
                    BorderStyle = BorderStyle.FixedSingle,
                    Tag = i + 1 // Store the day number in the Tag property
                };

                Label dayNumLabel = new Label
                {
                    Name = $"dayNumLabel{i + 1}",
                    Text = $"Day {i + 1}",
                    Location = new Point(5, 5),
                    AutoSize = true,
                    Font = new Font(this.Font, FontStyle.Bold)
                };

                Label dayOfWeekLabel = new Label
                {
                    Name = $"dayOfWeekLabel{i + 1}",
                    Location = new Point(5, 25),
                    AutoSize = true
                };

                // Add labels for labor and travel hours
                Label laborHoursLabel = new Label
                {
                    Name = $"laborHoursLabel{i + 1}",
                    Text = "Labor: 0 hrs",
                    Location = new Point(5, 45),
                    AutoSize = true,
                    Font = new Font(this.Font.FontFamily, 8),
                    Tag = "0" // Store hours value in Tag
                };

                Label travelHoursLabel = new Label
                {
                    Name = $"travelHoursLabel{i + 1}",
                    Text = "Travel: 0 hrs",
                    Location = new Point(5, 65), // Moved down to be more visible
                    AutoSize = true,
                    Font = new Font(this.Font.FontFamily, 8),
                    Tag = "0" // Store hours value in Tag
                };

                dayPanel.Controls.Add(dayNumLabel);
                dayPanel.Controls.Add(dayOfWeekLabel);
                dayPanel.Controls.Add(laborHoursLabel);
                dayPanel.Controls.Add(travelHoursLabel);

                // Add click event to panel
                dayPanel.Click += DayPanel_Click;
                groupBoxCalendar.Controls.Add(dayPanel);
            }

            // Second row days (8-14)
            for (int i = 0; i < 7; i++)
            {
                Panel dayPanel = new Panel
                {
                    Name = $"dayPanel{i + 8}",
                    Location = new Point(startX + (i * dayWidth), startY2),
                    Size = new Size(dayWidth, dayHeight),
                    BorderStyle = BorderStyle.FixedSingle,
                    Tag = i + 8 // Store the day number in the Tag property
                };

                Label dayNumLabel = new Label
                {
                    Name = $"dayNumLabel{i + 8}",
                    Text = $"Day {i + 8}",
                    Location = new Point(5, 5),
                    AutoSize = true,
                    Font = new Font(this.Font, FontStyle.Bold)
                };

                Label dayOfWeekLabel = new Label
                {
                    Name = $"dayOfWeekLabel{i + 8}",
                    Location = new Point(5, 25),
                    AutoSize = true
                };

                // Add labels for labor and travel hours
                Label laborHoursLabel = new Label
                {
                    Name = $"laborHoursLabel{i + 8}",
                    Text = "Labor: 0 hrs",
                    Location = new Point(5, 45),
                    AutoSize = true,
                    Font = new Font(this.Font.FontFamily, 8),
                    Tag = "0" // Store hours value in Tag
                };

                Label travelHoursLabel = new Label
                {
                    Name = $"travelHoursLabel{i + 8}",
                    Text = "Travel: 0 hrs",
                    Location = new Point(5, 65), // Moved down to be more visible
                    AutoSize = true,
                    Font = new Font(this.Font.FontFamily, 8),
                    Tag = "0" // Store hours value in Tag
                };

                dayPanel.Controls.Add(dayNumLabel);
                dayPanel.Controls.Add(dayOfWeekLabel);
                dayPanel.Controls.Add(laborHoursLabel);
                dayPanel.Controls.Add(travelHoursLabel);

                // Add click event to panel
                dayPanel.Click += DayPanel_Click;
                groupBoxCalendar.Controls.Add(dayPanel);
            }
        }

        private void CreateResultsSection()
        {
            // Create Results Section
            GroupBox groupBoxResults = new GroupBox
            {
                Name = "groupBoxResults",
                Text = "Results",
                Location = new Point(460, 445),
                Size = new Size(938, 350), // Reduced height for better visibility
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
            };
            this.Controls.Add(groupBoxResults);

            // DataGridView for day details
            dataGridViewDays = new DataGridView
            {
                Name = "dataGridViewDays",
                Location = new Point(20, 30),
                Size = new Size(760, 160),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            groupBoxResults.Controls.Add(dataGridViewDays);

            // Create table layout directly on the group box
            // Start with the horizontal lines
            Panel topLine = new Panel
            {
                Location = new Point(30, 210),
                Size = new Size(470, 1),
                BorderStyle = BorderStyle.FixedSingle
            };
            groupBoxResults.Controls.Add(topLine);

            Panel headerLine = new Panel
            {
                Location = new Point(30, 235),
                Size = new Size(470, 1),
                BorderStyle = BorderStyle.FixedSingle
            };
            groupBoxResults.Controls.Add(headerLine);

            Panel dataLine = new Panel
            {
                Location = new Point(30, 260),
                Size = new Size(470, 1),
                BorderStyle = BorderStyle.FixedSingle
            };
            groupBoxResults.Controls.Add(dataLine);

            Panel bottomLine = new Panel
            {
                Location = new Point(30, 285),
                Size = new Size(470, 1),
                BorderStyle = BorderStyle.FixedSingle
            };
            groupBoxResults.Controls.Add(bottomLine);

            // Column headers
            Label lblLabelHeader = new Label
            {
                Text = "Labour",
                Location = new Point(130, 215),
                Size = new Size(100, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(this.Font, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };
            groupBoxResults.Controls.Add(lblLabelHeader);

            Label lblTravelHeader = new Label
            {
                Text = "Travel",
                Location = new Point(260, 215),
                Size = new Size(100, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(this.Font, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };
            groupBoxResults.Controls.Add(lblTravelHeader);

            Label lblExpensesHeader = new Label
            {
                Text = "Expenses",
                Location = new Point(390, 215),
                Size = new Size(100, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(this.Font, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };
            groupBoxResults.Controls.Add(lblExpensesHeader);

            // Row headers
            Label lblHoursRow = new Label
            {
                Text = "Total Hours",
                Location = new Point(40, 240),
                Size = new Size(80, 20),
                Font = new Font(this.Font, FontStyle.Regular)
            };
            groupBoxResults.Controls.Add(lblHoursRow);

            Label lblCostRow = new Label
            {
                Text = "Total Cost",
                Location = new Point(40, 265),
                Size = new Size(80, 20),
                Font = new Font(this.Font, FontStyle.Regular)
            };
            groupBoxResults.Controls.Add(lblCostRow);

            // Data cells
            lblLabourHoursValue = new Label
            {
                Name = "lblLabourHoursValue",
                Text = "0",
                Location = new Point(130, 240),
                Size = new Size(100, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            groupBoxResults.Controls.Add(lblLabourHoursValue);

            lblLabourCostValue = new Label
            {
                Name = "lblLabourCostValue",
                Text = "$0.00",
                Location = new Point(130, 265),
                Size = new Size(100, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DarkBlue
            };
            groupBoxResults.Controls.Add(lblLabourCostValue);

            lblTravelHoursValue = new Label
            {
                Name = "lblTravelHoursValue",
                Text = "0",
                Location = new Point(260, 240),
                Size = new Size(100, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            groupBoxResults.Controls.Add(lblTravelHoursValue);

            lblTravelCostValue = new Label
            {
                Name = "lblTravelCostValue",
                Text = "$0.00",
                Location = new Point(260, 265),
                Size = new Size(100, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DarkBlue
            };
            groupBoxResults.Controls.Add(lblTravelCostValue);

            Label lblExpensesHours = new Label
            {
                Text = "—",
                Location = new Point(390, 240),
                Size = new Size(100, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            groupBoxResults.Controls.Add(lblExpensesHours);

            lblExpensesCostValue = new Label
            {
                Name = "lblExpensesCostValue",
                Text = "$0.00",
                Location = new Point(390, 265),
                Size = new Size(100, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DarkBlue
            };
            groupBoxResults.Controls.Add(lblExpensesCostValue);

            // Expenses note
            Label lblExpensesNote = new Label
            {
                Text = "(Cost + 10%, not incl. per Diem)",
                Location = new Point(360, 290),
                Size = new Size(170, 15),
                Font = new Font(this.Font.FontFamily, 8),
                ForeColor = Color.DarkSlateGray
            };
            groupBoxResults.Controls.Add(lblExpensesNote);

            // Total days
            lblTotalDaysValue = new Label
            {
                Name = "lblTotalDaysValue",
                Text = "0",
                Location = new Point(155, 288),
                Size = new Size(40, 25),
                Font = new Font(this.Font.FontFamily, 18, FontStyle.Bold)
            };
            groupBoxResults.Controls.Add(lblTotalDaysValue);

            Label lblTotalDays = new Label
            {
                Text = "Total Days:",
                Location = new Point(40, 290),
                AutoSize = true,
                Font = new Font(this.Font.FontFamily, 14, FontStyle.Bold)
            };
            groupBoxResults.Controls.Add(lblTotalDays);

            // Grand Total panel
            Panel grandTotalPanel = new Panel
            {
                Name = "grandTotalPanel",
                Size = new Size(260, 100),
                BorderStyle = BorderStyle.FixedSingle,
                // BackColor = Color.WhiteSmoke,
                Location = new Point(530, 210)
            };
            groupBoxResults.Controls.Add(grandTotalPanel);

            // Grand total label
            lblGrandTotalSection = new Label
            {
                Text = "Grand total",
                Location = new Point(20, 15),
                AutoSize = true,
                Font = new Font(this.Font.FontFamily, 14, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                BackColor = Color.Transparent
            };
            grandTotalPanel.Controls.Add(lblGrandTotalSection);

            // Grand total value
            lblGrandTotal = new Label
            {
                Name = "lblGrandTotal",
                Text = "$0.00",
                Location = new Point(20, 50),
                Size = new Size(220, 40),
                Font = new Font(this.Font.FontFamily, 20, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            grandTotalPanel.Controls.Add(lblGrandTotal);

        }



        private void LoadData()
        {
            // Load day of week options
            ComboBox comboBoxStartDay = (ComboBox)Controls.Find("comboBoxStartDay", true)[0];
            ComboBox comboBoxHoldoverDay = (ComboBox)Controls.Find("comboBoxHoldoverDay", true)[0];

            string[] daysOfWeek = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
            comboBoxStartDay.Items.AddRange(daysOfWeek);
            comboBoxHoldoverDay.Items.AddRange(daysOfWeek);

            if (comboBoxStartDay.Items.Count > 0)
                comboBoxStartDay.SelectedIndex = 0;

            if (comboBoxHoldoverDay.Items.Count > 0)
                comboBoxHoldoverDay.SelectedIndex = 6; // Default to Sunday for holdover

            // Load travel methods
            ComboBox comboBoxTravelMethod = (ComboBox)Controls.Find("comboBoxTravelMethod", true)[0];
            string[] travelMethods = { "Driving", "Flight" };
            comboBoxTravelMethod.Items.AddRange(travelMethods);

            if (comboBoxTravelMethod.Items.Count > 0)
                comboBoxTravelMethod.SelectedIndex = 0;

            // Load rate sheets
            rateSheets = DataManager.LoadRateSheets();
            ComboBox comboBoxRateSheet = (ComboBox)Controls.Find("comboBoxRateSheet", true)[0];

            foreach (var rateSheet in rateSheets)
            {
                comboBoxRateSheet.Items.Add(rateSheet.Name);
            }

            if (comboBoxRateSheet.Items.Count > 0)
            {
                comboBoxRateSheet.SelectedIndex = 0;
                currentRateSheet = rateSheets[0];
                DisplayRateSheet();
            }

            // Set up day panels with initial values
            UpdateDayPanels();

            // Set up DataGridView columns
            SetupDataGridView();
        }

        private void SetupDataGridView()
        {
            DataGridView dataGridViewDays = (DataGridView)Controls.Find("dataGridViewDays", true)[0];

            // Set up columns
            dataGridViewDays.Columns.Clear();
            dataGridViewDays.Columns.Add("Day", "Day");
            dataGridViewDays.Columns.Add("Labour", "Labour");
            dataGridViewDays.Columns.Add("Travel", "Travel");
            dataGridViewDays.Columns.Add("Mileage", "Mileage");
            dataGridViewDays.Columns.Add("Hotel", "Hotel");
            dataGridViewDays.Columns.Add("Rental", "Rental");
            dataGridViewDays.Columns.Add("Flight", "Flight");
            dataGridViewDays.Columns.Add("PerDiem", "Per Diem");
            dataGridViewDays.Columns.Add("Total", "Total");

            // Set column formats to show currency for cost columns
            foreach (DataGridViewColumn column in dataGridViewDays.Columns)
            {
                if (column.Name != "Day")
                {
                    column.DefaultCellStyle.Format = "C2";
                }
            }
        }

        private void UpdateDayPanels()
        {
            // Get required controls
            ComboBox comboBoxStartDay = (ComboBox)Controls.Find("comboBoxStartDay", true)[0];
            NumericUpDown numDaysOnSite = (NumericUpDown)Controls.Find("numDaysOnSite", true)[0];
            NumericUpDown numHoursPerDay = (NumericUpDown)Controls.Find("numHoursPerDay", true)[0];
            CheckBox chkSeparateTravelTo = (CheckBox)Controls.Find("chkSeparateTravelTo", true)[0];
            CheckBox chkSeparateTravelFrom = (CheckBox)Controls.Find("chkSeparateTravelFrom", true)[0];
            CheckBox holdoverDayCheckBox = (CheckBox)Controls.Find("chkHoldoverDay", true)[0];
            ComboBox holdoverDayComboBox = (ComboBox)Controls.Find("comboBoxHoldoverDay", true)[0];
            NumericUpDown numTravelTime = (NumericUpDown)Controls.Find("numTravelTime", true)[0];
            NumericUpDown numDailyTravelTime = (NumericUpDown)Controls.Find("numDailyTravelTime", true)[0];

            if (comboBoxStartDay.SelectedIndex >= 0)
            {
                int startDayIndex = comboBoxStartDay.SelectedIndex;
                string[] dayNames = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

                // Calculate total days
                int daysOnSite = (int)numDaysOnSite.Value;
                int totalDays = daysOnSite;
                if (chkSeparateTravelTo.Checked) totalDays++;
                if (chkSeparateTravelFrom.Checked) totalDays++;

                // Get hours values
                decimal hoursPerDay = numHoursPerDay.Value;
                decimal travelTimeToSite = numTravelTime.Value;
                decimal dailyTravelTime = numDailyTravelTime.Value;

                // Determine holdover day (if any)
                int holdoverDayOfWeek = -1;
                if (holdoverDayCheckBox.Checked && holdoverDayComboBox.SelectedIndex >= 0)
                {
                    holdoverDayOfWeek = holdoverDayComboBox.SelectedIndex;
                }

                // Weekend formatting
                // Weekend formatting
                Color weekendColor = isDarkMode ? Color.Yellow : Color.Gray;
                Font weekdayFont = new Font(this.Font, FontStyle.Regular);
                Font weekendFont = new Font(this.Font, FontStyle.Bold);

                // Reset panels outside of active days - KEY CHANGE HERE
                for (int i = 0; i < 14; i++)
                {
                    Panel dayPanel = (Panel)Controls.Find($"dayPanel{i + 1}", true)[0];

                    // If day is beyond our project, reset it and clear the 'manual' tag
                    if (i >= totalDays)
                    {
                        dayPanel.BackColor = isDarkMode ? darkControlBackColor : SystemColors.Control;
                        Label laborHoursLabel = (Label)dayPanel.Controls.Find($"laborHoursLabel{i + 1}", false)[0];
                        Label travelHoursLabel = (Label)dayPanel.Controls.Find($"travelHoursLabel{i + 1}", false)[0];
                        Label dayOfWeekLabel = (Label)dayPanel.Controls.Find($"dayOfWeekLabel{i + 1}", false)[0];

                        // Make label backgrounds transparent
                        laborHoursLabel.BackColor = Color.Transparent;
                        travelHoursLabel.BackColor = Color.Transparent;
                        dayOfWeekLabel.BackColor = Color.Transparent;

                        laborHoursLabel.Text = "Labor: 0 hrs";
                        laborHoursLabel.Tag = "0";
                        travelHoursLabel.Text = "Travel: 0 hrs";
                        travelHoursLabel.Tag = "0";
                        dayOfWeekLabel.ForeColor = isDarkMode ? Color.White : SystemColors.ControlText;
                        dayOfWeekLabel.Font = weekdayFont;

                        // Clear any manual tag
                        dayPanel.Tag = "auto";
                    }
                }

                // Update each active day panel
                // Update each active day panel
                for (int i = 0; i < totalDays; i++)
                {
                    try
                    {
                        Panel dayPanel = (Panel)Controls.Find($"dayPanel{i + 1}", true)[0];

                        // Find all labels in the day panel
                        foreach (Control control in dayPanel.Controls)
                        {
                            if (control is Label)
                            {
                                // Make ALL labels in day panels transparent
                                control.BackColor = Color.Transparent;

                                // Set text color to white in dark mode
                                if (isDarkMode)
                                {
                                    control.ForeColor = Color.White;
                                }
                            }
                        }

                        Label dayOfWeekLabel = (Label)dayPanel.Controls.Find($"dayOfWeekLabel{i + 1}", false)[0];
                        Label laborHoursLabel = (Label)dayPanel.Controls.Find($"laborHoursLabel{i + 1}", false)[0];
                        Label travelHoursLabel = (Label)dayPanel.Controls.Find($"travelHoursLabel{i + 1}", false)[0];
                        Label dayNumLabel = (Label)dayPanel.Controls.Find($"dayNumLabel{i + 1}", false)[0];

                        // Ensure day number label is transparent too
                        dayNumLabel.BackColor = Color.Transparent;
                        if (isDarkMode)
                        {
                            dayNumLabel.ForeColor = Color.White;
                        }

                        // Calculate the day of week
                        int dayIndex = (startDayIndex + i) % 7;
                        dayOfWeekLabel.Text = dayNames[dayIndex];

                        // Set weekend formatting
                        if (dayIndex == 5 || dayIndex == 6) // 5 = Saturday, 6 = Sunday
                        {
                            dayOfWeekLabel.ForeColor = weekendColor;
                            dayOfWeekLabel.Font = weekendFont;
                        }
                        else
                        {
                            dayOfWeekLabel.ForeColor = isDarkMode ? Color.White : SystemColors.ControlText;
                            dayOfWeekLabel.Font = weekdayFont;
                        }

                        // Skip days that are manually set
                        if (dayPanel.Tag.ToString() == "manual")
                        {
                            continue;
                        }

                        decimal laborHours = 0;
                        decimal travelHours = 0;

                        // Handle special day assignments
                        if (chkSeparateTravelTo.Checked && i == 0)
                        {
                            // Travel To day
                            dayPanel.BackColor = isDarkMode ? darkModeTravelDay : Color.Yellow;
                            travelHours = travelTimeToSite;
                            laborHours = 0;
                        }
                        else if (chkSeparateTravelFrom.Checked && i == totalDays - 1)
                        {
                            // Travel From day
                            dayPanel.BackColor = isDarkMode ? darkModeTravelDay : Color.Yellow;
                            travelHours = travelTimeToSite;
                            laborHours = 0;
                        }
                        else if (holdoverDayCheckBox.Checked && dayIndex == holdoverDayOfWeek)
                        {
                            // Holdover day - 8 hours at regular rate
                            dayPanel.BackColor = isDarkMode ? darkModeHoldoverDay : Color.LightGreen;
                            laborHours = 8; // Per requirements, holdover days get 8 hours at regular rate
                            travelHours = 0;
                        }
                        else
                        {
                            // Regular work day
                            dayPanel.BackColor = isDarkMode ? darkModeWorkDay : Color.LightBlue;

                            // Default to daily travel time
                            travelHours = dailyTravelTime * 2;

                            decimal displayTravelTime = travelHours;

                            // Check for first day without separate travel TO
                            if (!chkSeparateTravelTo.Checked && i == 0 && travelTimeToSite > 0)
                            {
                                // First day gets the site travel time
                                travelHours = travelTimeToSite;
                            }
                            // Check for last day without separate travel FROM
                            else if (!chkSeparateTravelFrom.Checked && i == totalDays - 1 && travelTimeToSite > 0)
                            {
                                // Last day gets the site travel time
                                travelHours = travelTimeToSite;
                            }
                            else
                            {
                                // For regular work days (not first/last day special cases)
                                // Double the daily travel time for calculation
                                travelHours = dailyTravelTime * 2;
                            }

                            // Update display and tag
                            travelHoursLabel.Text = $"Travel: {displayTravelTime} hrs";
                            travelHoursLabel.Tag = travelHours.ToString();

                            laborHours = hoursPerDay;
                        }

                        // Update hour labels
                        laborHoursLabel.Text = $"Labour: {laborHours} hrs";
                        laborHoursLabel.Tag = laborHours.ToString();
                        travelHoursLabel.Text = $"Travel: {travelHours} hrs";
                        travelHoursLabel.Tag = travelHours.ToString();
                    }
                    catch (Exception ex)
                    {
                        // Log the error
                        System.Diagnostics.Debug.WriteLine($"Error updating day panel {i + 1}: {ex.Message}");
                    }
                }
                DateTimePicker dtpStartDate = GetControlSafely<DateTimePicker>("dtpStartDate");
                if (dtpStartDate != null && dtpStartDate.Checked)
                {
                    UpdateScheduleDates();
                }

            }
        }

        // Add this method to your MainForm class
        private void EnhanceVisualAppearance()
        {
            // 1. Style all section headers
            StyleSectionHeaders();
        }

        private void StyleSectionHeaders()
        {
            // Apply consistent styling to all section headers - updated for table layout
            try
            {
                // Find the column header labels in the results panel
                Panel resultsPanel = null;
                foreach (Control c in this.Controls)
                {
                    if (c is GroupBox && c.Name == "groupBoxResults")
                    {
                        foreach (Control rc in c.Controls)
                        {
                            if (rc is Panel && rc.Name == "resultsPanel")
                            {
                                resultsPanel = (Panel)rc;
                                break;
                            }
                        }
                        break;
                    }
                }

                if (resultsPanel != null)
                {
                    // Apply style to all labels with Bold font in the results panel
                    foreach (Control c in resultsPanel.Controls)
                    {
                        if (c is Label && c.Font.Bold)
                        {
                            c.ForeColor = Color.DarkBlue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"Error styling section headers: {ex.Message}");
            }
        }

        // Method to enhance Grand Total section

        private void SetupEventHandlers()
        {
            try
            {
                // Rate sheet selection change
                Control[] rateSheetControls = Controls.Find("comboBoxRateSheet", true);
                if (rateSheetControls.Length > 0 && rateSheetControls[0] is ComboBox)
                {
                    ComboBox comboBoxRateSheet = (ComboBox)rateSheetControls[0];
                    comboBoxRateSheet.SelectedIndexChanged += (sender, e) =>
                    {
                        if (comboBoxRateSheet.SelectedIndex >= 0 && comboBoxRateSheet.SelectedIndex < rateSheets.Count)
                        {
                            currentRateSheet = rateSheets[comboBoxRateSheet.SelectedIndex];
                            DisplayRateSheet();
                            CalculateAndDisplayResults(); // Auto-calculate on change
                        }
                    };
                }

                // Emergency checkbox
                Control[] emergencyControls = Controls.Find("chkEmergency", true);
                if (emergencyControls.Length > 0 && emergencyControls[0] is CheckBox)
                {
                    CheckBox chkEmergency = (CheckBox)emergencyControls[0];
                    chkEmergency.CheckedChanged += (sender, e) =>
                    {
                        // Update the rate display when Emergency is toggled
                        UpdateRateDisplay();
                        // Recalculate results
                        CalculateAndDisplayResults();
                    };
                }

                // Holdover day checkbox - initial setup
                Control[] holdoverCheckControls = Controls.Find("chkHoldoverDay", true);
                Control[] holdoverComboControls = Controls.Find("comboBoxHoldoverDay", true);

                if (holdoverCheckControls.Length > 0 && holdoverCheckControls[0] is CheckBox &&
                    holdoverComboControls.Length > 0 && holdoverComboControls[0] is ComboBox)
                {
                    CheckBox holdoverDayCheckBox = (CheckBox)holdoverCheckControls[0];
                    ComboBox holdoverDayComboBox = (ComboBox)holdoverComboControls[0];

                    holdoverDayCheckBox.CheckedChanged += (sender, e) =>
                    {
                        holdoverDayComboBox.Enabled = holdoverDayCheckBox.Checked;
                        CalculateAndDisplayResults(); // Auto-calculate on change
                        UpdateDayPanels(); // Update schedule display
                    };

                    // Also set up the combo box's event handler
                    holdoverDayComboBox.SelectedIndexChanged += (s, e) => UpdateDayPanels();
                }

                // Start day change
                Control[] startDayControls = Controls.Find("comboBoxStartDay", true);
                if (startDayControls.Length > 0 && startDayControls[0] is ComboBox)
                {
                    ComboBox comboBoxStartDay = (ComboBox)startDayControls[0];
                    comboBoxStartDay.SelectedIndexChanged += (sender, e) =>
                    {
                        UpdateDayPanels();
                        CalculateAndDisplayResults(); // Auto-calculate on change
                    };
                }

                // Setup button
                Control[] setupControls = Controls.Find("btnSetup", true);
                if (setupControls.Length > 0 && setupControls[0] is Button)
                {
                    Button btnSetup = (Button)setupControls[0];
                    btnSetup.Click += (sender, e) =>
                    {
                        using (var setupForm = new SetupForm(rateSheets))
                        {
                            if (setupForm.ShowDialog() == DialogResult.OK)
                            {
                                rateSheets = setupForm.UpdatedRateSheets;

                                // Find rate sheet combo box safely
                                Control[] rateSheetBoxes = Controls.Find("comboBoxRateSheet", true);
                                if (rateSheetBoxes.Length > 0 && rateSheetBoxes[0] is ComboBox)
                                {
                                    ComboBox comboBoxRateSheet = (ComboBox)rateSheetBoxes[0];

                                    // Refresh rate sheet combo box
                                    comboBoxRateSheet.Items.Clear();
                                    foreach (var rateSheet in rateSheets)
                                    {
                                        comboBoxRateSheet.Items.Add(rateSheet.Name);
                                    }

                                    if (comboBoxRateSheet.Items.Count > 0)
                                    {
                                        comboBoxRateSheet.SelectedIndex = 0;
                                        currentRateSheet = rateSheets[0];
                                        DisplayRateSheet();
                                        CalculateAndDisplayResults(); // Auto-calculate on change
                                    }
                                }
                            }
                        }
                    };
                }



                // Additional event handlers for schedule updates
                SetupAdditionalEventHandlers();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting up event handlers: {ex.Message}");
            }
        }

        private void SetupAdditionalEventHandlers()
        {
            try
            {
                // Days on site
                Control[] daysOnSiteControls = Controls.Find("numDaysOnSite", true);
                if (daysOnSiteControls.Length > 0 && daysOnSiteControls[0] is NumericUpDown)
                {
                    NumericUpDown numDaysOnSite = (NumericUpDown)daysOnSiteControls[0];
                    numDaysOnSite.ValueChanged += (s, e) => UpdateDayPanels();
                }

                // Hours per day
                Control[] hoursPerDayControls = Controls.Find("numHoursPerDay", true);
                if (hoursPerDayControls.Length > 0 && hoursPerDayControls[0] is NumericUpDown)
                {
                    NumericUpDown numHoursPerDay = (NumericUpDown)hoursPerDayControls[0];
                    numHoursPerDay.ValueChanged += (s, e) => UpdateDayPanels();
                }

                // Travel checkboxes
                SetupTravelCheckboxes();

                // Travel time controls
                SetupTravelTimeControls();

                // Set up change events for all inputs to auto-calculate
                SetupAutoCalculateEvents();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting up additional event handlers: {ex.Message}");
            }
        }

        private void SetupTravelCheckboxes()
        {
            // Separate travel TO checkbox
            Control[] travelToControls = Controls.Find("chkSeparateTravelTo", true);
            if (travelToControls.Length > 0 && travelToControls[0] is CheckBox)
            {
                CheckBox chkSeparateTravelTo = (CheckBox)travelToControls[0];
                chkSeparateTravelTo.CheckedChanged += (s, e) => {
                    UpdateDayPanels();
                    CalculateAndDisplayResults();
                };
            }

            // Separate travel FROM checkbox
            Control[] travelFromControls = Controls.Find("chkSeparateTravelFrom", true);
            if (travelFromControls.Length > 0 && travelFromControls[0] is CheckBox)
            {
                CheckBox chkSeparateTravelFrom = (CheckBox)travelFromControls[0];
                chkSeparateTravelFrom.CheckedChanged += (s, e) => {
                    UpdateDayPanels();
                    CalculateAndDisplayResults();
                };
            }
        }

        private void SetupTravelTimeControls()
        {
            // Travel time to site
            Control[] travelTimeControls = Controls.Find("numTravelTime", true);
            if (travelTimeControls.Length > 0 && travelTimeControls[0] is NumericUpDown)
            {
                NumericUpDown numTravelTime = (NumericUpDown)travelTimeControls[0];
                numTravelTime.ValueChanged += (s, e) => UpdateDayPanels();
            }

            // Daily travel time
            Control[] dailyTravelTimeControls = Controls.Find("numDailyTravelTime", true);
            if (dailyTravelTimeControls.Length > 0 && dailyTravelTimeControls[0] is NumericUpDown)
            {
                NumericUpDown numDailyTravelTime = (NumericUpDown)dailyTravelTimeControls[0];
                numDailyTravelTime.ValueChanged += (s, e) => UpdateDayPanels();
            }
        }

        private void UpdateRateDisplay()
        {
            // Get the emergency checkbox
            CheckBox chkEmergency = (CheckBox)Controls.Find("chkEmergency", true)[0];
            bool isEmergency = chkEmergency.Checked;

            // Get the discount value
            NumericUpDown numDiscount = (NumericUpDown)Controls.Find("numDiscount", true)[0];
            decimal discountPercent = numDiscount.Value;
            decimal discountMultiplier = 1.0m;
            if (discountPercent > 0)
            {
                discountMultiplier = 1 - (discountPercent / 100);
            }

            // Get the rate textboxes
            TextBox txtRegularLabour = (TextBox)Controls.Find("txtRegularLabour", true)[0];
            TextBox txtOvertimeLabour = (TextBox)Controls.Find("txtOvertimeLabour", true)[0];
            TextBox txtPremiumLabour = (TextBox)Controls.Find("txtPremiumLabour", true)[0];
            TextBox txtRegularTravel = (TextBox)Controls.Find("txtRegularTravel", true)[0];
            TextBox txtOvertimeTravel = (TextBox)Controls.Find("txtOvertimeTravel", true)[0];
            TextBox txtPremiumTravel = (TextBox)Controls.Find("txtPremiumTravel", true)[0];

            // Get the current rate sheet
            if (currentRateSheet == null)
                return;

            // Define an array of textboxes for easier handling
            TextBox[] rateTextBoxes = new TextBox[]
            {
        txtRegularLabour, txtOvertimeLabour, txtPremiumLabour,
        txtRegularTravel, txtOvertimeTravel, txtPremiumTravel
            };

            if (isEmergency)
            {
                // Show premium rates for all categories, with discount applied
                decimal discountedPremiumLabour = currentRateSheet.PremiumLabourRate * discountMultiplier;
                decimal discountedPremiumTravel = currentRateSheet.PremiumTravelRate * discountMultiplier;

                txtRegularLabour.Text = discountedPremiumLabour.ToString("F2");
                txtOvertimeLabour.Text = discountedPremiumLabour.ToString("F2");
                txtPremiumLabour.Text = discountedPremiumLabour.ToString("F2");
                txtRegularTravel.Text = discountedPremiumTravel.ToString("F2");
                txtOvertimeTravel.Text = discountedPremiumTravel.ToString("F2");
                txtPremiumTravel.Text = discountedPremiumTravel.ToString("F2");

                // Set text color to red for ALL textboxes
                foreach (TextBox textBox in rateTextBoxes)
                {
                    textBox.ReadOnly = true;
                    textBox.BackColor = SystemColors.Window;
                    textBox.ForeColor = Color.Red;
                }
            }
            else
            {
                // Show normal rates with discount applied
                txtRegularLabour.Text = (currentRateSheet.RegularLabourRate * discountMultiplier).ToString("F2");
                txtOvertimeLabour.Text = (currentRateSheet.OvertimeLabourRate * discountMultiplier).ToString("F2");
                txtPremiumLabour.Text = (currentRateSheet.PremiumLabourRate * discountMultiplier).ToString("F2");
                txtRegularTravel.Text = (currentRateSheet.RegularTravelRate * discountMultiplier).ToString("F2");
                txtOvertimeTravel.Text = (currentRateSheet.OvertimeTravelRate * discountMultiplier).ToString("F2");
                txtPremiumTravel.Text = (currentRateSheet.PremiumTravelRate * discountMultiplier).ToString("F2");

                // Reset text color to default for ALL textboxes
                foreach (TextBox textBox in rateTextBoxes)
                {
                    textBox.ReadOnly = true;
                    textBox.BackColor = SystemColors.Window;
                    textBox.ForeColor = SystemColors.WindowText;
                }
            }
        }
        private void SetupAutoCalculateEvents()
        {
            // Days Configuration controls
            NumericUpDown numDaysOnSite = (NumericUpDown)Controls.Find("numDaysOnSite", true)[0];
            NumericUpDown numHoursPerDay = (NumericUpDown)Controls.Find("numHoursPerDay", true)[0];
            ComboBox comboBoxHoldoverDay = (ComboBox)Controls.Find("comboBoxHoldoverDay", true)[0];

            // Add value changed event handlers
            numDaysOnSite.ValueChanged += (s, e) => CalculateAndDisplayResults();
            numHoursPerDay.ValueChanged += (s, e) => CalculateAndDisplayResults();
            comboBoxHoldoverDay.SelectedIndexChanged += (s, e) => CalculateAndDisplayResults();

            // Rates controls
            NumericUpDown numDiscount = (NumericUpDown)Controls.Find("numDiscount", true)[0];
            CheckBox chkEmergency = (CheckBox)Controls.Find("chkEmergency", true)[0];

            numDiscount.ValueChanged += (s, e) => {
                UpdateRateDisplay();  // Add this line
                CalculateAndDisplayResults();
            };
            chkEmergency.CheckedChanged += (s, e) => CalculateAndDisplayResults();

            // Travel controls
            CheckBox chkSeparateTravelTo = (CheckBox)Controls.Find("chkSeparateTravelTo", true)[0];
            CheckBox chkSeparateTravelFrom = (CheckBox)Controls.Find("chkSeparateTravelFrom", true)[0];
            ComboBox comboBoxTravelMethod = (ComboBox)Controls.Find("comboBoxTravelMethod", true)[0];
            NumericUpDown numTravelDistance = (NumericUpDown)Controls.Find("numTravelDistance", true)[0];
            NumericUpDown numTravelTime = (NumericUpDown)Controls.Find("numTravelTime", true)[0];
            NumericUpDown numDailyTravelDistance = (NumericUpDown)Controls.Find("numDailyTravelDistance", true)[0];
            NumericUpDown numDailyTravelTime = (NumericUpDown)Controls.Find("numDailyTravelTime", true)[0];

            chkSeparateTravelTo.CheckedChanged += (s, e) => CalculateAndDisplayResults();
            chkSeparateTravelFrom.CheckedChanged += (s, e) => CalculateAndDisplayResults();
            comboBoxTravelMethod.SelectedIndexChanged += (s, e) => CalculateAndDisplayResults();
            numTravelDistance.ValueChanged += (s, e) => CalculateAndDisplayResults();
            numTravelTime.ValueChanged += (s, e) => CalculateAndDisplayResults();
            numDailyTravelDistance.ValueChanged += (s, e) => CalculateAndDisplayResults();
            numDailyTravelTime.ValueChanged += (s, e) => CalculateAndDisplayResults();

            // Expenses controls
            NumericUpDown numFlightCost = (NumericUpDown)Controls.Find("numFlightCost", true)[0];
            CheckBox chkRentalCar = (CheckBox)Controls.Find("chkRentalCar", true)[0];
            NumericUpDown numRentalCarCost = (NumericUpDown)Controls.Find("numRentalCarCost", true)[0];
            NumericUpDown numHotelCost = (NumericUpDown)Controls.Find("numHotelCost", true)[0];
            NumericUpDown numMileageRate = (NumericUpDown)Controls.Find("numMileageRate", true)[0];
            NumericUpDown numPerDiem = (NumericUpDown)Controls.Find("numPerDiem", true)[0];
            CheckBox chkHotel = GetControlSafely<CheckBox>("chkHotel");
            if (chkHotel != null)
                chkHotel.CheckedChanged += (s, e) => CalculateAndDisplayResults();

            numFlightCost.ValueChanged += (s, e) => CalculateAndDisplayResults();
            chkRentalCar.CheckedChanged += (s, e) => CalculateAndDisplayResults();
            numRentalCarCost.ValueChanged += (s, e) => CalculateAndDisplayResults();
            numHotelCost.ValueChanged += (s, e) => CalculateAndDisplayResults();
            numMileageRate.ValueChanged += (s, e) => CalculateAndDisplayResults();
            numPerDiem.ValueChanged += (s, e) => CalculateAndDisplayResults();
        }

        private void DisplayRateSheet()
        {
            if (currentRateSheet == null)
                return;

            TextBox txtRegularLabour = (TextBox)Controls.Find("txtRegularLabour", true)[0];
            TextBox txtOvertimeLabour = (TextBox)Controls.Find("txtOvertimeLabour", true)[0];
            TextBox txtPremiumLabour = (TextBox)Controls.Find("txtPremiumLabour", true)[0];
            TextBox txtRegularTravel = (TextBox)Controls.Find("txtRegularTravel", true)[0];
            TextBox txtOvertimeTravel = (TextBox)Controls.Find("txtOvertimeTravel", true)[0];
            TextBox txtPremiumTravel = (TextBox)Controls.Find("txtPremiumTravel", true)[0];

            txtRegularLabour.Text = currentRateSheet.RegularLabourRate.ToString("F2");
            txtOvertimeLabour.Text = currentRateSheet.OvertimeLabourRate.ToString("F2");
            txtPremiumLabour.Text = currentRateSheet.PremiumLabourRate.ToString("F2");
            txtRegularTravel.Text = currentRateSheet.RegularTravelRate.ToString("F2");
            txtOvertimeTravel.Text = currentRateSheet.OvertimeTravelRate.ToString("F2");
            txtPremiumTravel.Text = currentRateSheet.PremiumTravelRate.ToString("F2");

            NumericUpDown numHotelCost = (NumericUpDown)Controls.Find("numHotelCost", true)[0];
            NumericUpDown numPerDiem = (NumericUpDown)Controls.Find("numPerDiem", true)[0];
            NumericUpDown numMileageRate = (NumericUpDown)Controls.Find("numMileageRate", true)[0];
            NumericUpDown numFlightCost = (NumericUpDown)Controls.Find("numFlightCost", true)[0];
            NumericUpDown numRentalCarCost = (NumericUpDown)Controls.Find("numRentalCarCost", true)[0];

            numHotelCost.Value = currentRateSheet.HotelCost;
            numPerDiem.Value = currentRateSheet.PerDiemRate;
            numMileageRate.Value = currentRateSheet.MileageRate;
            numRentalCarCost.Value = currentRateSheet.RentalCarRate;
            numFlightCost.Value = currentRateSheet.FlightCost;

            UpdateRateDisplay();

        }

        private decimal GetNumericValueSafely(string controlName, decimal defaultValue = 0)
        {
            try
            {
                Control[] controls = Controls.Find(controlName, true);
                if (controls.Length == 0)
                    return defaultValue;

                Control control = controls[0];
                decimal result = defaultValue;

                if (control is TextBox)
                {
                    string text = ((TextBox)control).Text;
                    if (!decimal.TryParse(text, out result))
                        return defaultValue;
                }
                else if (control is NumericUpDown)
                {
                    result = ((NumericUpDown)control).Value;
                }

                return result;
            }
            catch
            {
                return defaultValue;
            }
        }
        private T GetControlSafely<T>(string controlName, T defaultValue = default(T)) where T : Control
        {
            try
            {
                Control[] controls = Controls.Find(controlName, true);
                if (controls.Length == 0 || !(controls[0] is T))
                    return defaultValue;

                return (T)controls[0];
            }
            catch
            {
                return defaultValue;
            }
        }

        private void CalculateAndDisplayResults()
        {
            try
            {
                // Get the days on site value
                decimal daysOnSiteValue = GetNumericValueSafely("numDaysOnSite", 1);
                int daysOnSite = (int)daysOnSiteValue;

                // Calculate total days
                int totalDays = daysOnSite;

                // Safely get checkbox states
                bool separateTravelTo = false;
                bool separateTravelFrom = false;

                CheckBox chkSeparateTravelTo = GetControlSafely<CheckBox>("chkSeparateTravelTo");
                if (chkSeparateTravelTo != null)
                    separateTravelTo = chkSeparateTravelTo.Checked;

                CheckBox chkSeparateTravelFrom = GetControlSafely<CheckBox>("chkSeparateTravelFrom");
                if (chkSeparateTravelFrom != null)
                    separateTravelFrom = chkSeparateTravelFrom.Checked;

                if (separateTravelTo) totalDays++;
                if (separateTravelFrom) totalDays++;

                // Update total days display
                Label lblTotalDaysValue = GetControlSafely<Label>("lblTotalDaysValue");
                if (lblTotalDaysValue != null)
                    lblTotalDaysValue.Text = totalDays.ToString();

                // Get rates from current rate sheet - with safe defaults
                decimal regularLabourRate = GetNumericValueSafely("txtRegularLabour", 100);
                decimal overtimeLabourRate = GetNumericValueSafely("txtOvertimeLabour", 150);
                decimal premiumLabourRate = GetNumericValueSafely("txtPremiumLabour", 200);
                decimal regularTravelRate = GetNumericValueSafely("txtRegularTravel", 80);
                decimal overtimeTravelRate = GetNumericValueSafely("txtOvertimeTravel", 120);
                decimal premiumTravelRate = GetNumericValueSafely("txtPremiumTravel", 160);

                // Check if emergency rate applies
                bool emergencyRate = false;
                CheckBox chkEmergency = GetControlSafely<CheckBox>("chkEmergency");
                if (chkEmergency != null)
                    emergencyRate = chkEmergency.Checked;

                // Get day of week for the start day
                int startDayIndex = 0; // Default to Monday
                ComboBox comboBoxStartDay = GetControlSafely<ComboBox>("comboBoxStartDay");
                if (comboBoxStartDay != null && comboBoxStartDay.SelectedIndex >= 0)
                    startDayIndex = comboBoxStartDay.SelectedIndex;

                // Calculate totals for labor and travel
                decimal totalRegularLaborHours = 0;
                decimal totalOvertimeLaborHours = 0;
                decimal totalPremiumLaborHours = 0;
                decimal totalRegularTravelHours = 0;
                decimal totalOvertimeTravelHours = 0;
                decimal totalPremiumTravelHours = 0;

                // Get the grid
                DataGridView dataGridViewDays = GetControlSafely<DataGridView>("dataGridViewDays");
                if (dataGridViewDays == null)
                    return; // Cannot proceed without the grid

                // Ensure grid has columns
                if (dataGridViewDays.Columns.Count == 0)
                {
                    SetupDataGridView();
                }

                // Clear the grid
                dataGridViewDays.Rows.Clear();

                // Calculate total expenses and per diem
                decimal totalExpenses = 0;
                decimal totalPerDiem = 0;
                decimal totalMileage = 0; // Track mileage separately since it doesn't get markup

                // Get travel method
                string travelMethod = "Driving"; // Default value
                ComboBox comboBoxTravelMethod = GetControlSafely<ComboBox>("comboBoxTravelMethod");
                if (comboBoxTravelMethod != null && comboBoxTravelMethod.SelectedItem != null)
                {
                    travelMethod = comboBoxTravelMethod.SelectedItem.ToString();
                }

                // Get rental car option
                bool rentalCarChecked = false;
                CheckBox chkRentalCar = GetControlSafely<CheckBox>("chkRentalCar");
                if (chkRentalCar != null)
                    rentalCarChecked = chkRentalCar.Checked;

                // Get hotel option
                bool hotelChecked = false;
                CheckBox chkHotel = GetControlSafely<CheckBox>("chkHotel");
                if (chkHotel != null)
                    hotelChecked = chkHotel.Checked;

                decimal rentalCarCost = GetNumericValueSafely("numRentalCarCost", 0);
                decimal flightCost = GetNumericValueSafely("numFlightCost", 0);
                decimal mileageRate = GetNumericValueSafely("numMileageRate", 0);
                decimal travelDistanceToSite = GetNumericValueSafely("numTravelDistance", 0);
                decimal dailyTravelDistance = GetNumericValueSafely("numDailyTravelDistance", 0);
                decimal hotelCost = GetNumericValueSafely("numHotelCost", 0);
                decimal perDiemRate = GetNumericValueSafely("numPerDiem", 0);

                // Process each day
                for (int i = 0; i < totalDays; i++)
                {
                    try
                    {
                        Panel dayPanel = (Panel)Controls.Find($"dayPanel{i + 1}", true)[0];
                        Label laborHoursLabel = (Label)dayPanel.Controls.Find($"laborHoursLabel{i + 1}", false)[0];
                        Label travelHoursLabel = (Label)dayPanel.Controls.Find($"travelHoursLabel{i + 1}", false)[0];
                        Label dayOfWeekLabel = (Label)dayPanel.Controls.Find($"dayOfWeekLabel{i + 1}", false)[0];

                        // Safely parse hour values
                        decimal laborHours = 0;
                        decimal travelHours = 0;
                        decimal.TryParse(laborHoursLabel.Tag?.ToString() ?? "0", out laborHours);
                        decimal.TryParse(travelHoursLabel.Tag?.ToString() ?? "0", out travelHours);

                        // Determine day of week
                        int dayIndex = (startDayIndex + i) % 7;
                        bool isSaturday = (dayIndex == 5);
                        bool isSunday = (dayIndex == 6);
                        bool isWeekday = (!isSaturday && !isSunday);

                        // Initialize hourly costs
                        decimal regularLaborHours = 0;
                        decimal overtimeLaborHours = 0;
                        decimal premiumLaborHours = 0;
                        decimal regularTravelHours = 0;
                        decimal overtimeTravelHours = 0;
                        decimal premiumTravelHours = 0;

                        // Check for special day types
                        bool isTravelToDay = (separateTravelTo && i == 0);
                        bool isTravelFromDay = (separateTravelFrom && i == totalDays - 1);
                        bool isFirstDay = (i == 0);
                        bool isLastDay = (i == totalDays - 1);

                        // Apply special day logic first
                        if (isTravelToDay || isTravelFromDay)
                        {
                            // Get travel time for special travel days
                            decimal specialTravelTime = GetNumericValueSafely("numTravelTime", 0);
                            travelHours = specialTravelTime;
                            laborHours = 0;
                        }

                        // Apply holdover rule for holdover days
                        if (dayPanel.BackColor == Color.LightGreen ||
                            (isDarkMode && dayPanel.BackColor == darkModeHoldoverDay))
                        {
                            // Holdover days are 8 hours at regular rate no matter what day of week
                            regularLaborHours = laborHours;
                            regularTravelHours = travelHours;
                        }
                        // Apply emergency rule
                        else if (emergencyRate)
                        {
                            // Emergency rates apply premium rate to all hours
                            premiumLaborHours = laborHours;
                            premiumTravelHours = travelHours;
                        }
                        else
                        {
                            // Normal rate calculation based on day of week
                            if (isWeekday) // Monday-Friday
                            {
                                // Calculate total hours first
                                decimal totalHours = laborHours + travelHours;

                                if (totalHours <= 8)
                                {
                                    // All hours are regular rate
                                    regularLaborHours = laborHours;
                                    regularTravelHours = travelHours;
                                }
                                else
                                {
                                    // Labor hours get priority for regular rate
                                    regularLaborHours = Math.Min(8, laborHours);
                                    decimal remainingRegularHours = Math.Max(0, 8 - regularLaborHours);
                                    regularTravelHours = Math.Min(remainingRegularHours, travelHours);

                                    // Any excess labor hours go to overtime
                                    overtimeLaborHours = Math.Max(0, laborHours - regularLaborHours);

                                    // Any excess travel hours go to overtime
                                    overtimeTravelHours = Math.Max(0, travelHours - regularTravelHours);
                                }
                            }
                            else if (isSaturday) // Saturday
                            {
                                // All hours at overtime rate
                                overtimeLaborHours = laborHours;
                                overtimeTravelHours = travelHours;
                            }
                            else if (isSunday) // Sunday
                            {
                                // All hours at premium rate
                                premiumLaborHours = laborHours;
                                premiumTravelHours = travelHours;
                            }
                        }

                        // Calculate costs for this day
                        decimal laborCost = (regularLaborHours * regularLabourRate) +
                                          (overtimeLaborHours * overtimeLabourRate) +
                                          (premiumLaborHours * premiumLabourRate);

                        decimal travelCost = (regularTravelHours * regularTravelRate) +
                                           (overtimeTravelHours * overtimeTravelRate) +
                                           (premiumTravelHours * premiumTravelRate);

                        // Add to totals
                        totalRegularLaborHours += regularLaborHours;
                        totalOvertimeLaborHours += overtimeLaborHours;
                        totalPremiumLaborHours += premiumLaborHours;
                        totalRegularTravelHours += regularTravelHours;
                        totalOvertimeTravelHours += overtimeTravelHours;
                        totalPremiumTravelHours += premiumTravelHours;

                        // Calculate expenses for this day
                        int dayNumber = i + 1;
                        string dayLabel = $"Day {dayNumber}";

                        decimal dayMileage = 0;
                        decimal dayHotel = 0;
                        decimal dayRental = 0;
                        decimal dayFlight = 0;
                        decimal dayPerDiem = 0;

                        // Hotel cost - applied to all days except the last, and only if hotel is checked
                        if (hotelChecked && i < totalDays - 1) // No hotel on last day
                        {
                            dayHotel = hotelCost;
                        }

                        // Rental car cost - applied to all days if checked
                        if (rentalCarChecked)
                        {
                            dayRental = rentalCarCost;
                        }

                        // Flight cost - only on travel days or first/last day if no separate travel days
                        if (travelMethod == "Flight")
                        {
                            if (isTravelToDay || isTravelFromDay ||
                                (isFirstDay && !separateTravelTo) ||
                                (isLastDay && !separateTravelFrom))
                            {
                                dayFlight = flightCost;
                            }
                        }

                        // Mileage calculation
                        if (isFirstDay || isLastDay)
                        {
                            // First day or last day mileage - always apply if travel distance is specified
                            if (travelDistanceToSite > 0)
                            {
                                dayMileage = mileageRate * travelDistanceToSite;
                            }
                        }
                        else if (travelMethod == "Driving" && !rentalCarChecked)
                        {
                            // For middle days, only apply mileage if driving and no rental car
                            dayMileage = mileageRate * (dailyTravelDistance * 2);
                        }

                        // TRAVEL TIME SUPERSEDING LOGIC
                        // Override travel hours for first/last day as needed
                        decimal travelTimeToSite = GetNumericValueSafely("numTravelTime", 0);
                        decimal dailyTravelTime = GetNumericValueSafely("numDailyTravelTime", 0);

                        // Check if we need to override travel hours for first/last day without separate travel
                        if (!separateTravelTo && i == 0 && travelTimeToSite > 0)
                        {
                            // First day gets the site travel time
                            travelHours = travelTimeToSite;
                            travelHoursLabel.Text = $"Travel: {travelTimeToSite} hrs";
                            travelHoursLabel.Tag = travelTimeToSite.ToString();
                        }
                        else if (!separateTravelFrom && i == totalDays - 1 && travelTimeToSite > 0)
                        {
                            // Last day gets the site travel time
                            travelHours = travelTimeToSite;
                            travelHoursLabel.Text = $"Travel: {travelTimeToSite} hrs";
                            travelHoursLabel.Tag = travelTimeToSite.ToString();
                        }

                        // Per diem
                        if (laborHours > 0 || travelHours > 0) // If there's any work or travel
                        {
                            dayPerDiem = perDiemRate;
                        }

                        // Track totals for expenses and per diem
                        totalExpenses += dayHotel + dayRental + dayFlight;
                        totalMileage += dayMileage; // Track mileage separately (no markup)
                        totalPerDiem += dayPerDiem;

                        // Total for the day
                        decimal dayTotal = laborCost + travelCost + dayMileage + dayHotel + dayRental + dayFlight + dayPerDiem;

                        // Add row to grid
                        dataGridViewDays.Rows.Add(
                            dayLabel,
                            laborCost,
                            travelCost,
                            dayMileage,
                            dayHotel,
                            dayRental,
                            dayFlight,
                            dayPerDiem,
                            dayTotal
                        );
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing day {i + 1}: {ex.Message}");
                    }
                }

                // Calculate and display summary values
                decimal totalLaborHours = totalRegularLaborHours + totalOvertimeLaborHours + totalPremiumLaborHours;
                decimal totalTravelHours = totalRegularTravelHours + totalOvertimeTravelHours + totalPremiumTravelHours;

                // Labor cost calculation
                decimal totalLaborCost = (totalRegularLaborHours * regularLabourRate) +
                                      (totalOvertimeLaborHours * overtimeLabourRate) +
                                      (totalPremiumLaborHours * premiumLabourRate);

                // Travel cost calculation
                decimal totalTravelCost = (totalRegularTravelHours * regularTravelRate) +
                                       (totalOvertimeTravelHours * overtimeTravelRate) +
                                       (totalPremiumTravelHours * premiumTravelRate);
           
                // Update summary displays
                Label lblLabourHoursValue = (Label)Controls.Find("lblLabourHoursValue", true)[0];
                Label lblTravelHoursValue = (Label)Controls.Find("lblTravelHoursValue", true)[0];
                Label lblLabourCostValue = (Label)Controls.Find("lblLabourCostValue", true)[0];
                Label lblTravelCostValue = (Label)Controls.Find("lblTravelCostValue", true)[0];

                lblLabourHoursValue.Text = totalLaborHours.ToString();
                lblTravelHoursValue.Text = totalTravelHours.ToString();
                lblLabourCostValue.Text = totalLaborCost.ToString("C2");
                lblTravelCostValue.Text = totalTravelCost.ToString("C2");

                // Add 10% to expenses (excluding mileage and per diem)
                totalExpenses *= 1.1m;

                // Update expenses display - include mileage here
                Label lblExpensesCostValue = (Label)Controls.Find("lblExpensesCostValue", true)[0];
                lblExpensesCostValue.Text = (totalExpenses + totalMileage).ToString("C2");

                // Grand total
                decimal grandTotal = totalLaborCost + totalTravelCost + totalExpenses + totalMileage + totalPerDiem;

                // Update grand total display
                Label lblGrandTotal = (Label)Controls.Find("lblGrandTotal", true)[0];
                lblGrandTotal.Text = grandTotal.ToString("C2");
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"Error in calculation: {ex.Message}");
                MessageBox.Show($"Calculation error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void ToggleDarkMode()
        {
            isDarkMode = !isDarkMode;
            ApplyTheme();
            SaveDarkModePreference();
        }

        private void ApplyTheme()
        {
            if (isDarkMode)
            {
                // Apply dark theme
                this.BackColor = darkBackColor;
                this.ForeColor = darkTextColor;

                // Update button appearance to indicate it's in dark mode
                btnDarkMode.BackColor = Color.LightGray;

                // Apply dark theme to all group boxes
                foreach (Control control in this.Controls)
                {
                    if (control is GroupBox)
                    {
                        ApplyDarkThemeToControl(control);
                    }
                }

                // Apply dark theme to data grid
                ApplyDarkThemeToDataGrid();

                // Update day panels with dark theme colors
                UpdateDayPanelsTheme();
            }
            else
            {
                // Restore light theme
                this.BackColor = lightBackColor;
                this.ForeColor = lightTextColor;

                // Update button appearance to indicate it's in light mode
                btnDarkMode.BackColor = Color.DarkGray;

                // Restore light theme to all group boxes
                foreach (Control control in this.Controls)
                {
                    if (control is GroupBox)
                    {
                        ApplyLightThemeToControl(control);
                    }
                }

                // Restore light theme to data grid
                ApplyLightThemeToDataGrid();

                // Update day panels with light theme colors
                UpdateDayPanelsTheme();
            }
        }

        // Add recursive methods to apply themes to controls
        private void ApplyDarkThemeToControl(Control control)
        {
            control.BackColor = darkControlBackColor;
            // Use white text instead of gray for better contrast
            control.ForeColor = Color.White;

            if (control is TextBox)
            {
                TextBox textBox = (TextBox)control;
                textBox.BackColor = darkPanelBackColor;
                textBox.ForeColor = Color.White;
                textBox.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (control is Button)
            {
                Button button = (Button)control;
                button.BackColor = darkButtonBackColor;
                button.ForeColor = Color.White;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = darkBorderColor;
            }
            else if (control is Panel)
            {
                Panel panel = (Panel)control;
                // For day panels, only set the border style but not the background
                if (panel.Name.StartsWith("dayPanel"))
                {
                    panel.BorderStyle = BorderStyle.FixedSingle;
                    // Make label backgrounds transparent for day panels
                    foreach (Control childControl in panel.Controls)
                    {
                        if (childControl is Label)
                        {
                            Label label = (Label)childControl;
                            label.BackColor = Color.Transparent;
                            label.ForeColor = Color.White;
                        }
                    }
                }
                else if (panel.Name == "grandTotalPanel")
                {
                    panel.BackColor = darkPanelBackColor;
                    panel.BorderStyle = BorderStyle.FixedSingle;

                    // Explicitly set all contained labels to transparent
                    foreach (Control childControl in panel.Controls)
                    {
                        if (childControl is Label)
                        {
                            Label label = (Label)childControl;
                            label.BackColor = Color.Transparent;
                            if (label.Name == "lblGrandTotal" || label.Name == "lblGrandTotalSection")
                            {
                                label.ForeColor = Color.LightSkyBlue;
                            }
                            else
                            {
                                label.ForeColor = Color.White;
                            }
                        }
                    }

                
            }
            else
                {
                    panel.BackColor = darkPanelBackColor;
                    panel.BorderStyle = BorderStyle.FixedSingle;
                }
            }
            else if (control is ComboBox)
            {
                ComboBox comboBox = (ComboBox)control;
                comboBox.BackColor = darkPanelBackColor;
                comboBox.ForeColor = Color.White;
            }
            else if (control is NumericUpDown)
            {
                NumericUpDown numericUpDown = (NumericUpDown)control;
                numericUpDown.BackColor = darkPanelBackColor;
                numericUpDown.ForeColor = Color.White;
            }
            else if (control is Label)
            {
                // For labels that are directly on day panels, make background transparent
                Control parent = control.Parent;
                if (parent != null && parent is Panel && parent.Name.StartsWith("dayPanel"))
                {
                    control.BackColor = Color.Transparent;
                }

                // Special handling for Grand Total label
                if (control.Name == "lblGrandTotal")
                {
                    control.ForeColor = Color.LightSkyBlue; // Highlight total in dark mode
                    control.BackColor = Color.Transparent;
                }
                else if (control.Name == "lblGrandTotalSection")
                {
                    control.ForeColor = Color.LightSkyBlue;
                    control.BackColor = Color.Transparent;// Highlight heading in dark mode
                }
            }

            else if (control is DateTimePicker)
            {
                DateTimePicker dateTimePicker = (DateTimePicker)control;
                dateTimePicker.BackColor = darkPanelBackColor;
                dateTimePicker.ForeColor = Color.White;
                dateTimePicker.CalendarForeColor = Color.White;
                dateTimePicker.CalendarMonthBackground = darkGridBackColor;
            }

            // Recursively apply to child controls
            foreach (Control child in control.Controls)
            {
                ApplyDarkThemeToControl(child);
            }
        }

        private void ApplyLightThemeToControl(Control control)
        {
            control.BackColor = lightControlBackColor;
            control.ForeColor = lightTextColor;

            if (control is TextBox)
            {
                TextBox textBox = (TextBox)control;
                textBox.BackColor = SystemColors.Window;
                textBox.ForeColor = SystemColors.WindowText;
                textBox.BorderStyle = BorderStyle.Fixed3D;
            }
            else if (control is Button)
            {
                Button button = (Button)control;
                button.BackColor = SystemColors.Control;
                button.ForeColor = SystemColors.ControlText;
                button.FlatStyle = FlatStyle.Standard;
            }
            else if (control is Panel)
            {
                Panel panel = (Panel)control;
                // Don't change day panel colors here, handle in UpdateDayPanelsTheme
                if (!panel.Name.StartsWith("dayPanel"))
                {
                    panel.BackColor = SystemColors.Control;
                    panel.BorderStyle = BorderStyle.FixedSingle;
                }
            }
            else if (control is ComboBox)
            {
                ComboBox comboBox = (ComboBox)control;
                comboBox.BackColor = SystemColors.Window;
                comboBox.ForeColor = SystemColors.WindowText;
            }
            else if (control is NumericUpDown)
            {
                NumericUpDown numericUpDown = (NumericUpDown)control;
                numericUpDown.BackColor = SystemColors.Window;
                numericUpDown.ForeColor = SystemColors.WindowText;
            }
            else if (control is Label)
            {
                // Special handling for labels
                if (control.Name == "lblGrandTotal")
                {
                    control.ForeColor = SystemColors.ControlText;
                    control.BackColor = Color.Transparent;
                }
                else if (control.Name == "lblGrandTotalSection")
                {
                    control.ForeColor = Color.DarkBlue; // Original color
                    control.BackColor = Color.Transparent;
                }
            }
            else if (control is DateTimePicker)
            {
                DateTimePicker dateTimePicker = (DateTimePicker)control;
                dateTimePicker.BackColor = SystemColors.Window;
                dateTimePicker.ForeColor = SystemColors.WindowText;
                dateTimePicker.CalendarForeColor = SystemColors.WindowText;
                dateTimePicker.CalendarMonthBackground = SystemColors.Window;
            }

            // Recursively apply to child controls
            foreach (Control child in control.Controls)
            {
                ApplyLightThemeToControl(child);
            }
        }

        // Add method to update day panels with theme colors
        private void UpdateDayPanelsTheme()
        {
            Color workDayColor = isDarkMode ? darkModeWorkDay : lightWorkDay;
            Color travelDayColor = isDarkMode ? darkModeTravelDay : lightTravelDay;
            Color holdoverDayColor = isDarkMode ? darkModeHoldoverDay : lightHoldoverDay;

            for (int i = 1; i <= 14; i++)
            {
                try
                {
                    Panel dayPanel = (Panel)Controls.Find($"dayPanel{i}", true)[0];

                    // Maintain the day type color coding while applying the theme
                    if (dayPanel.BackColor == lightWorkDay || dayPanel.BackColor == darkModeWorkDay)
                    {
                        dayPanel.BackColor = workDayColor;
                    }
                    else if (dayPanel.BackColor == lightTravelDay || dayPanel.BackColor == darkModeTravelDay)
                    {
                        dayPanel.BackColor = travelDayColor;
                    }
                    else if (dayPanel.BackColor == lightHoldoverDay || dayPanel.BackColor == darkModeHoldoverDay)
                    {
                        dayPanel.BackColor = holdoverDayColor;
                    }

                    // Update text color for labels within the day panel
                    foreach (Control control in dayPanel.Controls)
                    {
                        if (control is Label)
                        {
                            control.ForeColor = isDarkMode ? darkTextColor : SystemColors.ControlText;

                            // Special handling for day of week label (weekend formatting)
                            if (control.Name.Contains("dayOfWeekLabel"))
                            {
                                Label dayOfWeekLabel = (Label)control;
                                string dayText = dayOfWeekLabel.Text;

                                if (dayText == "Saturday" || dayText == "Sunday")
                                {
                                    dayOfWeekLabel.ForeColor = isDarkMode ? Color.Yellow : Color.Gray;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating day panel {i}: {ex.Message}");
                }
            }
        }

        // Add method to apply dark theme to the DataGridView
        private void ApplyDarkThemeToDataGrid()
        {
            DataGridView grid = dataGridViewDays;
            if (grid != null)
            {
                grid.BackgroundColor = darkGridBackColor;
                grid.ForeColor = darkTextColor;
                grid.GridColor = darkBorderColor;

                grid.ColumnHeadersDefaultCellStyle.BackColor = darkGridHeaderBackColor;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = darkTextColor;
                grid.RowHeadersDefaultCellStyle.BackColor = darkGridHeaderBackColor;
                grid.RowHeadersDefaultCellStyle.ForeColor = darkTextColor;

                grid.DefaultCellStyle.BackColor = darkGridCellBackColor;
                grid.DefaultCellStyle.ForeColor = darkTextColor;
                grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(80, 100, 140);
                grid.DefaultCellStyle.SelectionForeColor = Color.White;

                grid.EnableHeadersVisualStyles = false;
            }
        }

        // Add method to apply light theme to the DataGridView
        private void ApplyLightThemeToDataGrid()
        {
            DataGridView grid = dataGridViewDays;
            if (grid != null)
            {
                grid.BackgroundColor = SystemColors.Window;
                grid.ForeColor = SystemColors.ControlText;
                grid.GridColor = SystemColors.ControlDark;

                grid.ColumnHeadersDefaultCellStyle.BackColor = SystemColors.Control;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = SystemColors.ControlText;
                grid.RowHeadersDefaultCellStyle.BackColor = SystemColors.Control;
                grid.RowHeadersDefaultCellStyle.ForeColor = SystemColors.ControlText;

                grid.DefaultCellStyle.BackColor = SystemColors.Window;
                grid.DefaultCellStyle.ForeColor = SystemColors.ControlText;
                grid.DefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
                grid.DefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;

                grid.EnableHeadersVisualStyles = true;
            }
        }

        // Add methods to save and load dark mode preference using settings
        private void SaveDarkModePreference()
        {
            try
            {
                // Create an XML settings file in the application directory
                string settingsPath = Path.Combine(
                    Path.GetDirectoryName(Application.ExecutablePath),
                    "TimeExpenseSettings.xml");

                XmlDocument doc = new XmlDocument();
                XmlElement root;

                if (File.Exists(settingsPath))
                {
                    doc.Load(settingsPath);
                    root = doc.DocumentElement;
                }
                else
                {
                    root = doc.CreateElement("Settings");
                    doc.AppendChild(root);
                }

                // Update or create the DarkMode element
                XmlElement darkModeElement = null;
                foreach (XmlElement element in root.GetElementsByTagName("DarkMode"))
                {
                    darkModeElement = element;
                    break;
                }

                if (darkModeElement == null)
                {
                    darkModeElement = doc.CreateElement("DarkMode");
                    root.AppendChild(darkModeElement);
                }

                darkModeElement.InnerText = isDarkMode.ToString();
                doc.Save(settingsPath);
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"Error saving dark mode preference: {ex.Message}");
            }
        }

        private void LoadDarkModePreference()
        {
            try
            {
                string settingsPath = Path.Combine(
                    Path.GetDirectoryName(Application.ExecutablePath),
                    "TimeExpenseSettings.xml");

                if (File.Exists(settingsPath))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(settingsPath);

                    XmlNodeList darkModeNodes = doc.GetElementsByTagName("DarkMode");
                    if (darkModeNodes.Count > 0)
                    {
                        bool.TryParse(darkModeNodes[0].InnerText, out isDarkMode);

                        // Apply theme based on loaded preference
                        ApplyTheme();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"Error loading dark mode preference: {ex.Message}");
            }
        }
        private void SetupTooltips()
        {
            // Initialize the class-level toolTip
            toolTip = new ToolTip();

            // Configure the toolTip
            toolTip.AutoPopDelay = 10000;
            toolTip.InitialDelay = 500;
            toolTip.ReshowDelay = 200;
            toolTip.ShowAlways = true;

            // Apply tooltips to controls
            try
            {
                // First, find the travel group box
                Control[] groupBoxes = this.Controls.Find("groupBoxTravel", true);
                if (groupBoxes.Length > 0 && groupBoxes[0] is GroupBox)
                {
                    GroupBox travelGroup = (GroupBox)groupBoxes[0];

                    // Look for the labels within the travel group
                    foreach (Control c in travelGroup.Controls)
                    {
                        if (c is Label && c.Text.Contains("Driving Distance (First and Last Days Only"))
                        {
                            // Apply tooltip to travel distance label
                            toolTip.SetToolTip(c,
                                "Distance to site, car rental location or airport. ");

                            System.Diagnostics.Debug.WriteLine("Tooltip applied to: " + c.Text);
                        }

                        if (c is Label && c.Text.Contains("Total Travel Time to Site Area"))
                        {
                            // Apply tooltip to total travel time label
                            toolTip.SetToolTip(c,
                                "This is usually for separate travel days, and includes travel time for flights. " +
                                "If there is no separate travel day, this time will supersede the daily travel time for that day");

                            System.Diagnostics.Debug.WriteLine("Tooltip applied to: " + c.Text);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in tooltip setup: " + ex.Message);
            }
        }
        private void SetupTravelTimeDistanceLink()
        {
            // Get the numeric up down controls
            NumericUpDown numDailyTravelTime = (NumericUpDown)Controls.Find("numDailyTravelTime", true)[0];
            NumericUpDown numDailyTravelDistance = (NumericUpDown)Controls.Find("numDailyTravelDistance", true)[0];

            // Flag to prevent recursive updates
            bool isUpdating = false;

            // Add event handler for travel time changes
            numDailyTravelTime.ValueChanged += (sender, e) =>
            {
                if (!isUpdating)
                {
                    isUpdating = true;
                    // Convert time to distance (0.5 hours = 30 miles, which is 60 miles per hour)
                    numDailyTravelDistance.Value = numDailyTravelTime.Value * 60;
                    isUpdating = false;
                }
            };

            // Add event handler for travel distance changes
            numDailyTravelDistance.ValueChanged += (sender, e) =>
            {
                if (!isUpdating)
                {
                    isUpdating = true;
                    // Convert distance to time (30 miles = 0.5 hours)
                    numDailyTravelTime.Value = numDailyTravelDistance.Value / 60;
                    isUpdating = false;
                }
            };
        }
        private void UpdateDataGridView(int totalDays, decimal laborRate, decimal travelRate,
                                decimal mileageRate, decimal hotelCost, decimal rentalCost,
                                decimal flightCost, decimal perDiemRate)
        {
            DataGridView dataGridViewDays = (DataGridView)Controls.Find("dataGridViewDays", true)[0];
            dataGridViewDays.Rows.Clear();

            // Get checkbox values
            CheckBox chkRentalCar = (CheckBox)Controls.Find("chkRentalCar", true)[0];

            // Get travel method
            string travelMethod = ((ComboBox)Controls.Find("comboBoxTravelMethod", true)[0]).SelectedItem.ToString();

            // Add rows for each day
            for (int i = 0; i < totalDays; i++)
            {
                Panel dayPanel = (Panel)Controls.Find($"dayPanel{i + 1}", true)[0];
                Label laborHoursLabel = (Label)dayPanel.Controls.Find($"laborHoursLabel{i + 1}", false)[0];
                Label travelHoursLabel = (Label)dayPanel.Controls.Find($"travelHoursLabel{i + 1}", false)[0];

                decimal laborHours = decimal.Parse(laborHoursLabel.Tag.ToString());
                decimal travelHours = decimal.Parse(travelHoursLabel.Tag.ToString());

                // Calculate costs based on hours
                decimal dayLabor = laborHours * laborRate;
                decimal dayTravel = travelHours * travelRate;

                // Determine day type for expenses
                int dayNumber = i + 1;
                string dayLabel = $"Day {dayNumber}";

                decimal dayMileage = 0;
                decimal dayHotel = 0;
                decimal dayRental = 0;
                decimal dayFlight = 0;
                decimal dayPerDiem = perDiemRate;

                // Calculate mileage based on day type
                if (travelHours > 0)
                {
                    if (travelMethod == "Driving")
                    {
                        // Long distance travel
                        if (laborHours == 0 && travelHours >= ((NumericUpDown)Controls.Find("numTravelTime", true)[0]).Value)
                        {
                            dayMileage = mileageRate * ((NumericUpDown)Controls.Find("numTravelDistance", true)[0]).Value;
                        }
                        else // Daily travel
                        {
                            dayMileage = mileageRate * ((NumericUpDown)Controls.Find("numDailyTravelDistance", true)[0]).Value;
                        }
                    }
                }

                // Hotel stays (assuming stay the night before next work day)
                if (i < totalDays - 1)
                {
                    dayHotel = hotelCost;
                }

                // Flight costs for travel days
                if (travelMethod == "Flight" && laborHours == 0 && travelHours > 0)
                {
                    dayFlight = flightCost;
                }

                // Rental car (if selected)
                if (chkRentalCar.Checked)
                {
                    dayRental = rentalCost;
                }

                // Add the row to the grid
                decimal dayTotal = dayLabor + dayTravel + dayMileage + dayHotel + dayRental + dayFlight + dayPerDiem;
                dataGridViewDays.Rows.Add(dayLabel, dayLabor, dayTravel, dayMileage, dayHotel, dayRental, dayFlight, dayPerDiem, dayTotal);
            }
        }
        private void DayPanel_Click(object sender, EventArgs e)
        {
            Panel clickedPanel = sender as Panel;
            if (clickedPanel != null)
            {
                // Fix the issue with Tag - extract day number from panel name
                string panelName = clickedPanel.Name;
                int dayNumber;

                if (panelName.StartsWith("dayPanel") && int.TryParse(panelName.Substring(8), out dayNumber))
                {
                    // Successfully extracted the day number from the panel name
                }
                else
                {
                    return; // Invalid panel
                }

                // Find the labor and travel hour labels
                Label laborHoursLabel = (Label)clickedPanel.Controls.Find($"laborHoursLabel{dayNumber}", false)[0];
                Label travelHoursLabel = (Label)clickedPanel.Controls.Find($"travelHoursLabel{dayNumber}", false)[0];
                Label dayOfWeekLabel = (Label)clickedPanel.Controls.Find($"dayOfWeekLabel{dayNumber}", false)[0];

                // Get current hour values - use TryParse to avoid format errors
                decimal laborHours = 0;
                decimal travelHours = 0;
                decimal.TryParse(laborHoursLabel.Tag?.ToString() ?? "0", out laborHours);
                decimal.TryParse(travelHoursLabel.Tag?.ToString() ?? "0", out travelHours);

                // Get current day type based on background color
                string currentDayType = "Work Day";
                if (clickedPanel.BackColor == Color.Yellow)
                    currentDayType = "Travel Day";
                else if (clickedPanel.BackColor == Color.LightGreen)
                    currentDayType = "Holdover Day";

                // Show a dialog to edit hours and day type
                using (Form editForm = new Form())
                {
                    editForm.Text = $"Edit Day {dayNumber}";
                    editForm.Size = new Size(300, 240);
                    editForm.StartPosition = FormStartPosition.CenterParent;
                    editForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                    editForm.MaximizeBox = false;
                    editForm.MinimizeBox = false;

                    // Day type selection
                    Label lblDayType = new Label { Text = "Day Type:", Location = new Point(20, 20), AutoSize = true };
                    ComboBox comboDayType = new ComboBox
                    {
                        Location = new Point(150, 17),
                        Size = new Size(120, 25),
                        DropDownStyle = ComboBoxStyle.DropDownList
                    };

                    comboDayType.Items.AddRange(new string[] { "Work Day", "Travel Day", "Holdover Day", "Nil" });
                    comboDayType.SelectedItem = currentDayType;

                    Label lblDay = new Label { Text = $"Day of Week: {dayOfWeekLabel.Text}", Location = new Point(20, 50), AutoSize = true };

                    Label lblLabor = new Label { Text = "Labor Hours:", Location = new Point(20, 80), AutoSize = true };
                    NumericUpDown numLabor = new NumericUpDown
                    {
                        Location = new Point(150, 78),
                        Size = new Size(80, 25),
                        DecimalPlaces = 1,
                        Minimum = 0,
                        Maximum = 24,
                        Value = laborHours,
                        Increment = 0.5m
                    };

                    Label lblTravel = new Label { Text = "Travel Hours:", Location = new Point(20, 110), AutoSize = true };
                    NumericUpDown numTravel = new NumericUpDown
                    {
                        Location = new Point(150, 108),
                        Size = new Size(80, 25),
                        DecimalPlaces = 1,
                        Minimum = 0,
                        Maximum = 24,
                        Value = travelHours,
                        Increment = 0.5m
                    };

                    // Add event handler for day type change
                    comboDayType.SelectedIndexChanged += (s, evt) =>
                    {
                        string selectedType = comboDayType.SelectedItem.ToString();

                        // Set default hours based on day type
                        if (selectedType == "Work Day")
                        {
                            NumericUpDown numHoursPerDay = (NumericUpDown)Controls.Find("numHoursPerDay", true)[0];
                            NumericUpDown numDailyTravelTime = (NumericUpDown)Controls.Find("numDailyTravelTime", true)[0];

                            numLabor.Value = numHoursPerDay.Value;
                            numTravel.Value = numDailyTravelTime.Value;
                        }
                        else if (selectedType == "Travel Day")
                        {
                            NumericUpDown numTravelTime = (NumericUpDown)Controls.Find("numTravelTime", true)[0];

                            numLabor.Value = 0;
                            numTravel.Value = numTravelTime.Value;
                        }
                        else if (selectedType == "Holdover Day")
                        {
                            numLabor.Value = 8; // Per requirements, holdover days are 8 hours at regular rate
                            numTravel.Value = 0;
                        }
                        else if (selectedType == "Nil") // Add this condition
                        {
                            numLabor.Value = 0;
                            numTravel.Value = 0;
                        }

                        // string selectedType = comboDayType.SelectedItem.ToString();
                        if (selectedType == "Work Day")
                            clickedPanel.BackColor = isDarkMode ? darkModeWorkDay : lightWorkDay;
                        else if (selectedType == "Travel Day")
                            clickedPanel.BackColor = isDarkMode ? darkModeTravelDay : lightTravelDay;
                        else if (selectedType == "Holdover Day")
                            clickedPanel.BackColor = isDarkMode ? darkModeHoldoverDay : lightHoldoverDay;
                        else if (selectedType == "Nil")
                            clickedPanel.BackColor = isDarkMode ? darkControlBackColor : SystemColors.Control;

                    };

                    Button btnOK = new Button { Text = "OK", Location = new Point(70, 150), Size = new Size(70, 30), DialogResult = DialogResult.OK };
                    Button btnCancel = new Button { Text = "Cancel", Location = new Point(160, 150), Size = new Size(70, 30), DialogResult = DialogResult.Cancel };

                    editForm.Controls.AddRange(new Control[] { lblDayType, comboDayType, lblDay, lblLabor, numLabor, lblTravel, numTravel, btnOK, btnCancel });
                    editForm.AcceptButton = btnOK;
                    editForm.CancelButton = btnCancel;

                    if (editForm.ShowDialog() == DialogResult.OK)
                    {
                        // Update the hour values
                        laborHoursLabel.Text = $"Labor: {numLabor.Value} hrs";
                        laborHoursLabel.Tag = numLabor.Value.ToString();
                        travelHoursLabel.Text = $"Travel: {numTravel.Value} hrs";
                        travelHoursLabel.Tag = numTravel.Value.ToString();

                        // Update panel color based on selected day type
                        string selectedType = comboDayType.SelectedItem.ToString();
                        if (selectedType == "Work Day")
                            clickedPanel.BackColor = isDarkMode ? darkModeWorkDay : Color.LightBlue;
                        else if (selectedType == "Travel Day")
                            clickedPanel.BackColor = isDarkMode ? darkModeTravelDay : Color.Yellow;
                        else if (selectedType == "Holdover Day")
                            clickedPanel.BackColor = isDarkMode ? darkModeHoldoverDay : Color.LightGreen;
                        else if (selectedType == "Nil")
                            clickedPanel.BackColor = isDarkMode ? darkControlBackColor : SystemColors.Control;

                        // Mark as manually set
                        clickedPanel.Tag = "manual";

                        // Recalculate totals
                        CalculateAndDisplayResults();
                    }
                }
            }
        }
        private void ExportToExcel()
        {
            // Get project number and customer
            string projectNumber = GetControlSafely<TextBox>("txtProjectNumber")?.Text ?? "";
            string customer = GetControlSafely<TextBox>("txtCustomer")?.Text ?? "";

            // Create the filename
            string filename = "";
            if (!string.IsNullOrWhiteSpace(projectNumber) && !string.IsNullOrWhiteSpace(customer))
            {
                // Both project number and customer provided
                filename = $"{projectNumber} {customer} - Time and Expenses Estimate {DateTime.Now:yyyy-MM-dd}";
            }
            else if (!string.IsNullOrWhiteSpace(projectNumber))
            {
                // Only project number provided
                filename = $"{projectNumber} - Time and Expenses Estimate {DateTime.Now:yyyy-MM-dd}";
            }
            else if (!string.IsNullOrWhiteSpace(customer))
            {
                // Only customer provided
                filename = $"{customer} - Time and Expenses Estimate {DateTime.Now:yyyy-MM-dd}";
            }
            else
            {
                // Neither provided
                filename = $"Time and Expenses Estimate {DateTime.Now:yyyy-MM-dd}";
            }

            // Create save file dialog
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Excel Files (*.xlsx)|*.xlsx";
            saveDialog.Title = "Export Time & Expense Report";
            saveDialog.DefaultExt = "xlsx";
            saveDialog.FileName = filename;

            // Show dialog and handle result
            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // EPPlus 4.5.3.x doesn't require setting license context
                    using (var package = new ExcelPackage())
                    {
                        CreateSummarySheet(package);
                        CreateDetailSheet(package);
                        CreateRatesSheet(package);

                        // Save the Excel package to file
                        var fileInfo = new FileInfo(saveDialog.FileName);
                        package.SaveAs(fileInfo);

                        MessageBox.Show("Report exported successfully!", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting to Excel: {ex.Message}", "Export Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void CreateSummarySheet(ExcelPackage package)
        {
            var summarySheet = package.Workbook.Worksheets.Add("Summary");

            // Add header information
            summarySheet.Cells[1, 1].Value = "Time & Expense Report";
            summarySheet.Cells[1, 1].Style.Font.Size = 16;
            summarySheet.Cells[1, 1].Style.Font.Bold = true;

            // Get project and customer info
            string projectNumber = GetControlSafely<TextBox>("txtProjectNumber")?.Text ?? "Not specified";
            string customer = GetControlSafely<TextBox>("txtCustomer")?.Text ?? "Not specified";

            summarySheet.Cells[3, 1].Value = "Project Number:";
            summarySheet.Cells[3, 2].Value = projectNumber;

            summarySheet.Cells[4, 1].Value = "Customer:";
            summarySheet.Cells[4, 2].Value = customer;

            // Get technician name
            string technicianName = GetControlSafely<TextBox>("txtTechnician")?.Text ?? "Not specified";
            summarySheet.Cells[5, 1].Value = "Technician:";
            summarySheet.Cells[5, 2].Value = technicianName;

            // Get date range
            DateTimePicker dtpStartDate = GetControlSafely<DateTimePicker>("dtpStartDate");
            DateTimePicker dtpEndDate = GetControlSafely<DateTimePicker>("dtpEndDate");
            string dateRange = "Not specified";

            if (dtpStartDate != null && dtpEndDate != null)
            {
                if (dtpStartDate.Checked && dtpEndDate.Checked)
                    dateRange = $"{dtpStartDate.Value:MMM d, yyyy} - {dtpEndDate.Value:MMM d, yyyy}";
                else if (dtpStartDate.Checked)
                    dateRange = $"{dtpStartDate.Value:MMM d, yyyy} onwards";
                else if (dtpEndDate.Checked)
                    dateRange = $"Until {dtpEndDate.Value:MMM d, yyyy}";
            }

            summarySheet.Cells[6, 1].Value = "Date Range:";
            summarySheet.Cells[6, 2].Value = dateRange;

            // Get discount and emergency status 
            decimal discount = GetNumericValueSafely("numDiscount", 0);
            bool emergencyRate = GetControlSafely<CheckBox>("chkEmergency")?.Checked ?? false;

            summarySheet.Cells[7, 1].Value = "Discount Applied:";
            summarySheet.Cells[7, 2].Value = $"{discount}%";

            summarySheet.Cells[8, 1].Value = "Emergency Rates:";
            summarySheet.Cells[8, 2].Value = emergencyRate ? "Yes" : "No";

            // Add labor breakdown
            summarySheet.Cells[10, 1].Value = "HOURS BREAKDOWN";
            summarySheet.Cells[10, 1].Style.Font.Bold = true;

            summarySheet.Cells[11, 1].Value = "Category";
            summarySheet.Cells[11, 2].Value = "Regular Rate";
            summarySheet.Cells[11, 3].Value = "Overtime Rate";
            summarySheet.Cells[11, 4].Value = "Premium Rate";
            summarySheet.Cells[11, 5].Value = "Total";

            // Format headers
            var headerRange = summarySheet.Cells[11, 1, 11, 5];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightGray);

            // Calculate hours by rate
            CalculateHoursByRate(out decimal regularLaborHours, out decimal overtimeLaborHours,
                out decimal premiumLaborHours, out decimal regularTravelHours,
                out decimal overtimeTravelHours, out decimal premiumTravelHours);

            // Add labor hours
            summarySheet.Cells[12, 1].Value = "Labor Hours";
            summarySheet.Cells[12, 2].Value = regularLaborHours;
            summarySheet.Cells[12, 3].Value = overtimeLaborHours;
            summarySheet.Cells[12, 4].Value = premiumLaborHours;
            summarySheet.Cells[12, 5].Value = regularLaborHours + overtimeLaborHours + premiumLaborHours;

            // Add travel hours
            summarySheet.Cells[13, 1].Value = "Travel Hours";
            summarySheet.Cells[13, 2].Value = regularTravelHours;
            summarySheet.Cells[13, 3].Value = overtimeTravelHours;
            summarySheet.Cells[13, 4].Value = premiumTravelHours;
            summarySheet.Cells[13, 5].Value = regularTravelHours + overtimeTravelHours + premiumTravelHours;

            // Add totals row
            summarySheet.Cells[14, 1].Value = "Total Hours";
            summarySheet.Cells[14, 2].Value = regularLaborHours + regularTravelHours;
            summarySheet.Cells[14, 3].Value = overtimeLaborHours + overtimeTravelHours;
            summarySheet.Cells[14, 4].Value = premiumLaborHours + premiumTravelHours;
            summarySheet.Cells[14, 5].Value = regularLaborHours + overtimeLaborHours + premiumLaborHours +
                                            regularTravelHours + overtimeTravelHours + premiumTravelHours;

            // Format totals row
            var totalsRange = summarySheet.Cells[14, 1, 14, 5];
            totalsRange.Style.Font.Bold = true;

            // Add cost breakdown
            decimal regularLabourRate = GetNumericValueSafely("txtRegularLabour");
            decimal overtimeLabourRate = GetNumericValueSafely("txtOvertimeLabour");
            decimal premiumLabourRate = GetNumericValueSafely("txtPremiumLabour");
            decimal regularTravelRate = GetNumericValueSafely("txtRegularTravel");
            decimal overtimeTravelRate = GetNumericValueSafely("txtOvertimeTravel");
            decimal premiumTravelRate = GetNumericValueSafely("txtPremiumTravel");

            // Calculate costs
            decimal laborRegularCost = regularLaborHours * regularLabourRate;
            decimal laborOvertimeCost = overtimeLaborHours * overtimeLabourRate;
            decimal laborPremiumCost = premiumLaborHours * premiumLabourRate;
            decimal travelRegularCost = regularTravelHours * regularTravelRate;
            decimal travelOvertimeCost = overtimeTravelHours * overtimeTravelRate;
            decimal travelPremiumCost = premiumTravelHours * premiumTravelRate;

            // Apply discount if any
            decimal discountMultiplier = 1.0m;
            if (discount > 0)
            {
                discountMultiplier = 1 - (discount / 100);
                laborRegularCost *= discountMultiplier;
                laborOvertimeCost *= discountMultiplier;
                laborPremiumCost *= discountMultiplier;
                travelRegularCost *= discountMultiplier;
                travelOvertimeCost *= discountMultiplier;
                travelPremiumCost *= discountMultiplier;
            }

            // Add cost section
            summarySheet.Cells[16, 1].Value = "COST BREAKDOWN";
            summarySheet.Cells[16, 1].Style.Font.Bold = true;

            summarySheet.Cells[17, 1].Value = "Category";
            summarySheet.Cells[17, 2].Value = "Regular Rate";
            summarySheet.Cells[17, 3].Value = "Overtime Rate";
            summarySheet.Cells[17, 4].Value = "Premium Rate";
            summarySheet.Cells[17, 5].Value = "Total";

            // Format cost headers
            var costHeaderRange = summarySheet.Cells[17, 1, 17, 5];
            costHeaderRange.Style.Font.Bold = true;
            costHeaderRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            costHeaderRange.Style.Fill.BackgroundColor.SetColor(Color.LightGray);

            // Add labor costs
            summarySheet.Cells[18, 1].Value = "Labor Cost";
            summarySheet.Cells[18, 2].Value = laborRegularCost;
            summarySheet.Cells[18, 3].Value = laborOvertimeCost;
            summarySheet.Cells[18, 4].Value = laborPremiumCost;
            summarySheet.Cells[18, 5].Value = laborRegularCost + laborOvertimeCost + laborPremiumCost;

            // Add travel costs
            summarySheet.Cells[19, 1].Value = "Travel Cost";
            summarySheet.Cells[19, 2].Value = travelRegularCost;
            summarySheet.Cells[19, 3].Value = travelOvertimeCost;
            summarySheet.Cells[19, 4].Value = travelPremiumCost;
            summarySheet.Cells[19, 5].Value = travelRegularCost + travelOvertimeCost + travelPremiumCost;

            // Add expense totals
            decimal hotelTotal = 0;
            decimal rentalTotal = 0;
            decimal flightTotal = 0;
            decimal mileageTotal = 0;
            decimal perDiemTotal = 0;

            DataGridView dataGrid = GetControlSafely<DataGridView>("dataGridViewDays");
            if (dataGrid != null)
            {
                foreach (DataGridViewRow row in dataGrid.Rows)
                {
                    try
                    {
                        if (row.Cells["Hotel"]?.Value != null)
                            hotelTotal += Convert.ToDecimal(row.Cells["Hotel"].Value);

                        if (row.Cells["Rental"]?.Value != null)
                            rentalTotal += Convert.ToDecimal(row.Cells["Rental"].Value);

                        if (row.Cells["Flight"]?.Value != null)
                            flightTotal += Convert.ToDecimal(row.Cells["Flight"].Value);

                        if (row.Cells["Mileage"]?.Value != null)
                            mileageTotal += Convert.ToDecimal(row.Cells["Mileage"].Value);

                        if (row.Cells["PerDiem"]?.Value != null)
                            perDiemTotal += Convert.ToDecimal(row.Cells["PerDiem"].Value);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing expense row: {ex.Message}");
                    }
                }
            }

            // Add expenses section
            summarySheet.Cells[21, 1].Value = "EXPENSES SUMMARY";
            summarySheet.Cells[21, 1].Style.Font.Bold = true;

            summarySheet.Cells[22, 1].Value = "Category";
            summarySheet.Cells[22, 2].Value = "Amount";

            // Format expense headers
            var expenseHeaderRange = summarySheet.Cells[22, 1, 22, 2];
            expenseHeaderRange.Style.Font.Bold = true;
            expenseHeaderRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            expenseHeaderRange.Style.Fill.BackgroundColor.SetColor(Color.LightGray);

            // Add expense items
            summarySheet.Cells[23, 1].Value = "Hotel:";
            summarySheet.Cells[23, 2].Value = hotelTotal;

            summarySheet.Cells[24, 1].Value = "Rental Car:";
            summarySheet.Cells[24, 2].Value = rentalTotal;

            summarySheet.Cells[25, 1].Value = "Flights:";
            summarySheet.Cells[25, 2].Value = flightTotal;

            summarySheet.Cells[26, 1].Value = "Mileage:";
            summarySheet.Cells[26, 2].Value = mileageTotal;

            summarySheet.Cells[27, 1].Value = "Per Diem:";
            summarySheet.Cells[27, 2].Value = perDiemTotal;

            // Add expense 10% markup note
            decimal expensesWithMarkup = (hotelTotal + rentalTotal + flightTotal) * 1.1m + mileageTotal;
            summarySheet.Cells[28, 1].Value = "Expense Total (incl. 10% markup on applicable items):";
            summarySheet.Cells[28, 2].Value = expensesWithMarkup;
            summarySheet.Cells[28, 1, 28, 2].Style.Font.Bold = true;

            // Add grand total
            summarySheet.Cells[30, 1].Value = "GRAND TOTAL:";
            summarySheet.Cells[30, 1].Style.Font.Bold = true;
            summarySheet.Cells[30, 1].Style.Font.Size = 12;

            decimal grandTotal = laborRegularCost + laborOvertimeCost + laborPremiumCost +
                                 travelRegularCost + travelOvertimeCost + travelPremiumCost +
                                 expensesWithMarkup + perDiemTotal;

            summarySheet.Cells[30, 2].Value = grandTotal;
            summarySheet.Cells[30, 2].Style.Font.Bold = true;
            summarySheet.Cells[30, 2].Style.Font.Size = 12;

            // Format currency cells
            var currencyRanges = new List<ExcelRange> {
        summarySheet.Cells[18, 2, 19, 5],
        summarySheet.Cells[23, 2, 28, 2],
        summarySheet.Cells[30, 2]
    };

            foreach (var range in currencyRanges)
            {
                range.Style.Numberformat.Format = "$#,##0.00";
            }

            // Format hour cells
            summarySheet.Cells[12, 2, 14, 5].Style.Numberformat.Format = "#,##0.0";

            // Auto-fit all columns
            summarySheet.Cells[summarySheet.Dimension.Address].AutoFitColumns();
        }

        private void CreateDetailSheet(ExcelPackage package)
        {
            var detailSheet = package.Workbook.Worksheets.Add("Daily Breakdown");

            // Add headers
            detailSheet.Cells[1, 1].Value = "Day";
            detailSheet.Cells[1, 2].Value = "Date";
            detailSheet.Cells[1, 3].Value = "Day of Week";

            // Labor columns with sub-columns
            detailSheet.Cells[1, 4].Value = "Labor Hours (Reg)";
            detailSheet.Cells[1, 5].Value = "Labor Hours (OT)";
            detailSheet.Cells[1, 6].Value = "Labor Hours (Prem)";
            detailSheet.Cells[1, 7].Value = "Labor Cost";

            // Travel columns with sub-columns
            detailSheet.Cells[1, 8].Value = "Travel Hours (Reg)";
            detailSheet.Cells[1, 9].Value = "Travel Hours (OT)";
            detailSheet.Cells[1, 10].Value = "Travel Hours (Prem)";
            detailSheet.Cells[1, 11].Value = "Travel Cost";

            // Expense columns
            detailSheet.Cells[1, 12].Value = "Mileage";
            detailSheet.Cells[1, 13].Value = "Hotel";
            detailSheet.Cells[1, 14].Value = "Rental";
            detailSheet.Cells[1, 15].Value = "Flight";
            detailSheet.Cells[1, 16].Value = "Per Diem";
            detailSheet.Cells[1, 17].Value = "Daily Total";

            // Format header row
            var headerRange = detailSheet.Cells[1, 1, 1, 17];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightGray);

            // Get required data
            DateTimePicker dtpStartDate = GetControlSafely<DateTimePicker>("dtpStartDate");
            ComboBox comboBoxStartDay = GetControlSafely<ComboBox>("comboBoxStartDay");
            int startDayIndex = comboBoxStartDay?.SelectedIndex ?? 0;
            string[] dayNames = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

            // Get labor rates
            decimal regularLabourRate = GetNumericValueSafely("txtRegularLabour");
            decimal overtimeLabourRate = GetNumericValueSafely("txtOvertimeLabour");
            decimal premiumLabourRate = GetNumericValueSafely("txtPremiumLabour");
            decimal regularTravelRate = GetNumericValueSafely("txtRegularTravel");
            decimal overtimeTravelRate = GetNumericValueSafely("txtOvertimeTravel");
            decimal premiumTravelRate = GetNumericValueSafely("txtPremiumTravel");

            // Check for emergency rates
            bool emergencyRate = GetControlSafely<CheckBox>("chkEmergency")?.Checked ?? false;

            // Apply discount if any
            decimal discount = GetNumericValueSafely("numDiscount");
            decimal discountMultiplier = 1.0m;
            if (discount > 0)
            {
                discountMultiplier = 1 - (discount / 100);
            }

            // Add day data from data grid
            DataGridView dataGrid = GetControlSafely<DataGridView>("dataGridViewDays");
            if (dataGrid != null)
            {
                int row = 2; // Start at row 2

                foreach (DataGridViewRow gridRow in dataGrid.Rows)
                {
                    try
                    {
                        // Safely get the day value with null check
                        string dayText = gridRow.Cells["Day"].Value?.ToString() ?? "Day 0";

                        // Extract day number with safer parsing
                        int dayNumber = 0;
                        if (dayText.StartsWith("Day "))
                        {
                            int.TryParse(dayText.Replace("Day ", ""), out dayNumber);
                        }

                        // Skip rows with invalid day numbers
                        if (dayNumber <= 0)
                        {
                            continue;
                        }

                        // Calculate day of week
                        int dayOfWeekIndex = (startDayIndex + dayNumber - 1) % 7;
                        string dayOfWeek = dayNames[dayOfWeekIndex];

                        // Determine if weekend
                        bool isWeekend = (dayOfWeekIndex == 5 || dayOfWeekIndex == 6);

                        // Get the panel for this day - add null check
                        Panel dayPanel = Controls.Find($"dayPanel{dayNumber}", true).FirstOrDefault() as Panel;
                        if (dayPanel == null) continue;

                        // Basic day info
                        detailSheet.Cells[row, 1].Value = dayText;

                        // Get date if available
                        if (dtpStartDate != null && dtpStartDate.Checked)
                        {
                            DateTime baseDate = dtpStartDate.Value;
                            // Adjust for travel day if necessary
                            if (GetControlSafely<CheckBox>("chkSeparateTravelTo")?.Checked ?? false)
                                baseDate = baseDate.AddDays(-1);

                            detailSheet.Cells[row, 2].Value = baseDate.AddDays(dayNumber - 1).ToString("MMM d, yyyy");
                        }

                        detailSheet.Cells[row, 3].Value = dayOfWeek;

                        // Get hours from day panel with null checks
                        Label laborHoursLabel = dayPanel.Controls.Find($"laborHoursLabel{dayNumber}", false).FirstOrDefault() as Label;
                        Label travelHoursLabel = dayPanel.Controls.Find($"travelHoursLabel{dayNumber}", false).FirstOrDefault() as Label;

                        decimal laborHours = 0;
                        decimal travelHours = 0;

                        if (laborHoursLabel != null)
                            decimal.TryParse(laborHoursLabel.Tag?.ToString() ?? "0", out laborHours);

                        if (travelHoursLabel != null)
                            decimal.TryParse(travelHoursLabel.Tag?.ToString() ?? "0", out travelHours);

                        // Calculate hours by rate type
                        decimal regularLaborHours = 0;
                        decimal overtimeLaborHours = 0;
                        decimal premiumLaborHours = 0;
                        decimal regularTravelHours = 0;
                        decimal overtimeTravelHours = 0;
                        decimal premiumTravelHours = 0;

                        // Check panel color safely
                        bool isHoldoverDay = false;
                        if (dayPanel != null)
                        {
                            isHoldoverDay = (dayPanel.BackColor == Color.LightGreen ||
                                           (isDarkMode && dayPanel.BackColor == darkModeHoldoverDay));
                        }

                        // Apply rate logic
                        if (emergencyRate)
                        {
                            // All hours at premium rate
                            premiumLaborHours = laborHours;
                            premiumTravelHours = travelHours;
                        }
                        else if (isHoldoverDay)
                        {
                            // Holdover days are all regular rate
                            regularLaborHours = laborHours;
                            regularTravelHours = travelHours;
                        }
                        else if (dayOfWeekIndex == 5) // Saturday
                        {
                            // All hours at overtime rate
                            overtimeLaborHours = laborHours;
                            overtimeTravelHours = travelHours;
                        }
                        else if (dayOfWeekIndex == 6) // Sunday
                        {
                            // All hours at premium rate
                            premiumLaborHours = laborHours;
                            premiumTravelHours = travelHours;
                        }
                        else // Weekday
                        {
                            // Regular hours (max 8), then overtime
                            regularLaborHours = Math.Min(8, laborHours);
                            overtimeLaborHours = Math.Max(0, laborHours - 8);

                            regularTravelHours = Math.Min(8, travelHours);
                            overtimeTravelHours = Math.Max(0, travelHours - 8);
                        }

                        // Add hours breakdown
                        detailSheet.Cells[row, 4].Value = regularLaborHours;
                        detailSheet.Cells[row, 5].Value = overtimeLaborHours;
                        detailSheet.Cells[row, 6].Value = premiumLaborHours;

                        detailSheet.Cells[row, 8].Value = regularTravelHours;
                        detailSheet.Cells[row, 9].Value = overtimeTravelHours;
                        detailSheet.Cells[row, 10].Value = premiumTravelHours;

                        // Calculate costs
                        decimal laborCost = (regularLaborHours * regularLabourRate +
                                          overtimeLaborHours * overtimeLabourRate +
                                          premiumLaborHours * premiumLabourRate) * discountMultiplier;

                        decimal travelCost = (regularTravelHours * regularTravelRate +
                                            overtimeTravelHours * overtimeTravelRate +
                                            premiumTravelHours * premiumTravelRate) * discountMultiplier;

                        // Add costs from data grid with null checks
                        detailSheet.Cells[row, 7].Value = laborCost;
                        detailSheet.Cells[row, 11].Value = travelCost;
                        detailSheet.Cells[row, 12].Value = gridRow.Cells["Mileage"].Value ?? 0;
                        detailSheet.Cells[row, 13].Value = gridRow.Cells["Hotel"].Value ?? 0;
                        detailSheet.Cells[row, 14].Value = gridRow.Cells["Rental"].Value ?? 0;
                        detailSheet.Cells[row, 15].Value = gridRow.Cells["Flight"].Value ?? 0;
                        detailSheet.Cells[row, 16].Value = gridRow.Cells["PerDiem"].Value ?? 0;
                        detailSheet.Cells[row, 17].Value = gridRow.Cells["Total"].Value ?? 0;

                        // Format weekend days
                        if (isWeekend)
                        {
                            detailSheet.Cells[row, 3].Style.Font.Bold = true;
                            if (isHoldoverDay)
                            {
                                // Highlight holdover days
                                detailSheet.Cells[row, 1, row, 17].Style.Fill.PatternType = ExcelFillStyle.Solid;
                                detailSheet.Cells[row, 1, row, 17].Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
                            }
                        }

                        row++;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing row: {ex.Message}");
                        // Continue to next row instead of failing the entire export
                    }
                }

                // Add totals row
                int totalRow = row;
                detailSheet.Cells[totalRow, 1].Value = "TOTALS";
                detailSheet.Cells[totalRow, 1].Style.Font.Bold = true;

                // Sum columns
                for (int col = 4; col <= 17; col++)
                {
                    // Create sum formula
                    if (col >= 4) // Skip first 3 columns (Day, Date, Day of Week)
                    {
                        detailSheet.Cells[totalRow, col].Formula = $"SUM({GetExcelColumnLetter(col)}2:{GetExcelColumnLetter(col)}{totalRow - 1})";
                        detailSheet.Cells[totalRow, col].Style.Font.Bold = true;
                    }
                }
            }

            // Format number columns
            detailSheet.Cells[2, 4, detailSheet.Dimension.End.Row, 10].Style.Numberformat.Format = "#,##0.0";

            // Format currency columns
            detailSheet.Cells[2, 7, detailSheet.Dimension.End.Row, 7].Style.Numberformat.Format = "$#,##0.00";
            detailSheet.Cells[2, 11, detailSheet.Dimension.End.Row, 17].Style.Numberformat.Format = "$#,##0.00";

            // Auto-fit all columns
            detailSheet.Cells[detailSheet.Dimension.Address].AutoFitColumns();
        }

        private void CreateRatesSheet(ExcelPackage package)
        {
            var ratesSheet = package.Workbook.Worksheets.Add("Rates & Configuration");

            // Add rate information
            ratesSheet.Cells[1, 1].Value = "RATE INFORMATION";
            ratesSheet.Cells[1, 1].Style.Font.Bold = true;
            ratesSheet.Cells[1, 1].Style.Font.Size = 12;

            // Add labor rates
            ratesSheet.Cells[2, 1].Value = "Regular Labour Rate:";
            ratesSheet.Cells[2, 2].Value = GetNumericValueSafely("txtRegularLabour");

            ratesSheet.Cells[3, 1].Value = "Overtime Labour Rate:";
            ratesSheet.Cells[3, 2].Value = GetNumericValueSafely("txtOvertimeLabour");
            ratesSheet.Cells[4, 1].Value = "Premium Labour Rate:";
            ratesSheet.Cells[4, 2].Value = GetNumericValueSafely("txtPremiumLabour");

            // Add travel rates
            ratesSheet.Cells[5, 1].Value = "Regular Travel Rate:";
            ratesSheet.Cells[5, 2].Value = GetNumericValueSafely("txtRegularTravel");

            ratesSheet.Cells[6, 1].Value = "Overtime Travel Rate:";
            ratesSheet.Cells[6, 2].Value = GetNumericValueSafely("txtOvertimeTravel");

            ratesSheet.Cells[7, 1].Value = "Premium Travel Rate:";
            ratesSheet.Cells[7, 2].Value = GetNumericValueSafely("txtPremiumTravel");

            // Add expense rates
            ratesSheet.Cells[9, 1].Value = "EXPENSE CONFIGURATION";
            ratesSheet.Cells[9, 1].Style.Font.Bold = true;
            ratesSheet.Cells[9, 1].Style.Font.Size = 12;

            ratesSheet.Cells[10, 1].Value = "Hotel Cost:";
            ratesSheet.Cells[10, 2].Value = GetNumericValueSafely("numHotelCost");

            ratesSheet.Cells[11, 1].Value = "Rental Car Cost:";
            ratesSheet.Cells[11, 2].Value = GetNumericValueSafely("numRentalCarCost");

            ratesSheet.Cells[12, 1].Value = "Flight Cost:";
            ratesSheet.Cells[12, 2].Value = GetNumericValueSafely("numFlightCost");

            ratesSheet.Cells[13, 1].Value = "Mileage Rate:";
            ratesSheet.Cells[13, 2].Value = GetNumericValueSafely("numMileageRate");

            ratesSheet.Cells[14, 1].Value = "Per Diem:";
            ratesSheet.Cells[14, 2].Value = GetNumericValueSafely("numPerDiem");

            // Add travel configuration
            ratesSheet.Cells[16, 1].Value = "TRAVEL CONFIGURATION";
            ratesSheet.Cells[16, 1].Style.Font.Bold = true;
            ratesSheet.Cells[16, 1].Style.Font.Size = 12;

            ratesSheet.Cells[17, 1].Value = "Travel Method:";
            ComboBox travelMethodCombo = GetControlSafely<ComboBox>("comboBoxTravelMethod");
            ratesSheet.Cells[17, 2].Value = travelMethodCombo?.SelectedItem?.ToString() ?? "Driving";

            ratesSheet.Cells[18, 1].Value = "Daily Distance (One Way):";
            ratesSheet.Cells[18, 2].Value = GetNumericValueSafely("numDailyTravelDistance");

            ratesSheet.Cells[19, 1].Value = "Daily Travel Time (One Way):";
            ratesSheet.Cells[19, 2].Value = GetNumericValueSafely("numDailyTravelTime");

            ratesSheet.Cells[20, 1].Value = "Travel Distance to Site:";
            ratesSheet.Cells[20, 2].Value = GetNumericValueSafely("numTravelDistance");

            ratesSheet.Cells[21, 1].Value = "Travel Time to Site:";
            ratesSheet.Cells[21, 2].Value = GetNumericValueSafely("numTravelTime");

            ratesSheet.Cells[22, 1].Value = "Separate Travel Day To:";
            ratesSheet.Cells[22, 2].Value = GetControlSafely<CheckBox>("chkSeparateTravelTo")?.Checked ?? false ? "Yes" : "No";

            ratesSheet.Cells[23, 1].Value = "Separate Travel Day From:";
            ratesSheet.Cells[23, 2].Value = GetControlSafely<CheckBox>("chkSeparateTravelFrom")?.Checked ?? false ? "Yes" : "No";

            // Add general configuration
            ratesSheet.Cells[25, 1].Value = "GENERAL CONFIGURATION";
            ratesSheet.Cells[25, 1].Style.Font.Bold = true;
            ratesSheet.Cells[25, 1].Style.Font.Size = 12;

            ratesSheet.Cells[26, 1].Value = "Days on Site:";
            ratesSheet.Cells[26, 2].Value = GetNumericValueSafely("numDaysOnSite");

            ratesSheet.Cells[27, 1].Value = "Hours per Day:";
            ratesSheet.Cells[27, 2].Value = GetNumericValueSafely("numHoursPerDay");

            ratesSheet.Cells[28, 1].Value = "Start Day:";
            ComboBox startDayCombo = GetControlSafely<ComboBox>("comboBoxStartDay");
            ratesSheet.Cells[28, 2].Value = startDayCombo?.SelectedItem?.ToString() ?? "Monday";

            ratesSheet.Cells[29, 1].Value = "Holdover Day:";
            CheckBox holdoverCheck = GetControlSafely<CheckBox>("chkHoldoverDay");
            ratesSheet.Cells[29, 2].Value = holdoverCheck?.Checked ?? false ? "Yes" : "No";

            if (holdoverCheck?.Checked ?? false)
            {
                ComboBox holdoverDayCombo = GetControlSafely<ComboBox>("comboBoxHoldoverDay");
                ratesSheet.Cells[30, 1].Value = "Holdover Day of Week:";
                ratesSheet.Cells[30, 2].Value = holdoverDayCombo?.SelectedItem?.ToString() ?? "Sunday";
            }

            // Add export info
            ratesSheet.Cells[32, 1].Value = "REPORT INFORMATION";
            ratesSheet.Cells[32, 1].Style.Font.Bold = true;
            ratesSheet.Cells[32, 1].Style.Font.Size = 12;

            ratesSheet.Cells[33, 1].Value = "Generated On:";
            ratesSheet.Cells[33, 2].Value = DateTime.Now.ToString("MMM d, yyyy h:mm tt");

            // Format rate cells
            ratesSheet.Cells[2, 2, 7, 2].Style.Numberformat.Format = "$#,##0.00";
            ratesSheet.Cells[10, 2, 12, 2].Style.Numberformat.Format = "$#,##0.00";
            ratesSheet.Cells[13, 2].Style.Numberformat.Format = "$#,##0.00";
            ratesSheet.Cells[14, 2].Style.Numberformat.Format = "$#,##0.00";

            // Format distance and time cells
            ratesSheet.Cells[18, 2].Style.Numberformat.Format = "#,##0.0";
            ratesSheet.Cells[19, 2].Style.Numberformat.Format = "#,##0.00";
            ratesSheet.Cells[20, 2].Style.Numberformat.Format = "#,##0.0";
            ratesSheet.Cells[21, 2].Style.Numberformat.Format = "#,##0.00";

            // Auto-fit all columns
            ratesSheet.Cells[ratesSheet.Dimension.Address].AutoFitColumns();
        }

        // Helper method to calculate hours by rate type
        private void CalculateHoursByRate(out decimal regularLabor, out decimal overtimeLabor,
                                         out decimal premiumLabor, out decimal regularTravel,
                                         out decimal overtimeTravel, out decimal premiumTravel)
        {
            regularLabor = 0;
            overtimeLabor = 0;
            premiumLabor = 0;
            regularTravel = 0;
            overtimeTravel = 0;
            premiumTravel = 0;

            // Get required controls and data
            int totalDays = int.Parse(GetControlSafely<Label>("lblTotalDaysValue")?.Text ?? "0");
            int startDayIndex = GetControlSafely<ComboBox>("comboBoxStartDay")?.SelectedIndex ?? 0;
            bool emergencyRate = GetControlSafely<CheckBox>("chkEmergency")?.Checked ?? false;

            // Process each day panel to determine hours by rate
            for (int i = 0; i < totalDays; i++)
            {
                try
                {
                    Panel dayPanel = (Panel)Controls.Find($"dayPanel{i + 1}", true)[0];
                    Label laborHoursLabel = (Label)dayPanel.Controls.Find($"laborHoursLabel{i + 1}", false)[0];
                    Label travelHoursLabel = (Label)dayPanel.Controls.Find($"travelHoursLabel{i + 1}", false)[0];

                    // Get labor and travel hours 
                    decimal laborHours = 0;
                    decimal travelHours = 0;
                    decimal.TryParse(laborHoursLabel.Tag?.ToString() ?? "0", out laborHours);
                    decimal.TryParse(travelHoursLabel.Tag?.ToString() ?? "0", out travelHours);

                    // Skip if no hours
                    if (laborHours <= 0 && travelHours <= 0)
                        continue;

                    // Determine day of week
                    int dayIndex = (startDayIndex + i) % 7;
                    bool isSaturday = (dayIndex == 5);
                    bool isSunday = (dayIndex == 6);

                    // Apply holdover rule for holdover days
                    if (dayPanel.BackColor == Color.LightGreen || dayPanel.BackColor == darkModeHoldoverDay)
                    {
                        // Holdover days are regular rate regardless of day of week
                        regularLabor += laborHours;
                        regularTravel += travelHours;
                    }
                    // Apply emergency rule
                    else if (emergencyRate)
                    {
                        // Emergency rates apply premium rate to all hours
                        premiumLabor += laborHours;
                        premiumTravel += travelHours;
                    }
                    else
                    {
                        // Normal rate calculation based on day of week
                        if (!isSaturday && !isSunday) // Monday-Friday
                        {
                            // Regular hours (max 8), then overtime
                            regularLabor += Math.Min(8, laborHours);
                            overtimeLabor += Math.Max(0, laborHours - 8);

                            regularTravel += Math.Min(8, travelHours);
                            overtimeTravel += Math.Max(0, travelHours - 8);
                        }
                        else if (isSaturday) // Saturday
                        {
                            // All hours at overtime rate
                            overtimeLabor += laborHours;
                            overtimeTravel += travelHours;
                        }
                        else if (isSunday) // Sunday
                        {
                            // All hours at premium rate
                            premiumLabor += laborHours;
                            premiumTravel += travelHours;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error calculating hours for day {i + 1}: {ex.Message}");
                }
            }
        }

        // Helper to get Excel column letter from column index
        private string GetExcelColumnLetter(int columnNumber)
        {
            string columnName = "";

            while (columnNumber > 0)
            {
                int remainder = (columnNumber - 1) % 26;
                char columnLetter = (char)(65 + remainder);
                columnName = columnLetter + columnName;
                columnNumber = (columnNumber - 1) / 26;
            }

            return columnName;
        }






        private void ResetForm()
        {
            // Reset numeric inputs
            ((NumericUpDown)Controls.Find("numDaysOnSite", true)[0]).Value = 1;
            ((NumericUpDown)Controls.Find("numHoursPerDay", true)[0]).Value = 10;
            ((NumericUpDown)Controls.Find("numDiscount", true)[0]).Value = 0;
            ((NumericUpDown)Controls.Find("numTravelDistance", true)[0]).Value = 0;
            ((NumericUpDown)Controls.Find("numTravelTime", true)[0]).Value = 0;
            ((NumericUpDown)Controls.Find("numDailyTravelDistance", true)[0]).Value = 0;
            ((NumericUpDown)Controls.Find("numDailyTravelTime", true)[0]).Value = 0;

            // Use current rate sheet values instead of zeros
            ((NumericUpDown)Controls.Find("numFlightCost", true)[0]).Value = currentRateSheet.FlightCost;
            ((NumericUpDown)Controls.Find("numRentalCarCost", true)[0]).Value = currentRateSheet.RentalCarRate;
            ((NumericUpDown)Controls.Find("numHotelCost", true)[0]).Value = currentRateSheet.HotelCost;
            ((NumericUpDown)Controls.Find("numMileageRate", true)[0]).Value = currentRateSheet.MileageRate;
            ((NumericUpDown)Controls.Find("numPerDiem", true)[0]).Value = currentRateSheet.PerDiemRate;

            // Reset checkboxes
            ((CheckBox)Controls.Find("chkHoldoverDay", true)[0]).Checked = false;
            ((CheckBox)Controls.Find("chkEmergency", true)[0]).Checked = false;
            ((CheckBox)Controls.Find("chkSeparateTravelTo", true)[0]).Checked = false;
            ((CheckBox)Controls.Find("chkSeparateTravelFrom", true)[0]).Checked = false;
            ((CheckBox)Controls.Find("chkRentalCar", true)[0]).Checked = false;

            // Reset results
            ((Label)Controls.Find("lblLabourHoursValue", true)[0]).Text = "0";
            ((Label)Controls.Find("lblTravelHoursValue", true)[0]).Text = "0";
            ((Label)Controls.Find("lblLabourCostValue", true)[0]).Text = "$0.00";
            ((Label)Controls.Find("lblTravelCostValue", true)[0]).Text = "$0.00";
            ((Label)Controls.Find("lblExpensesCostValue", true)[0]).Text = "$0.00";
            ((Label)Controls.Find("lblGrandTotal", true)[0]).Text = "$0.00";

            TextBox txtTechnician = GetControlSafely<TextBox>("txtTechnician");
            if (txtTechnician != null)
                txtTechnician.Text = "Technician";

            DateTimePicker dtpStartDate = GetControlSafely<DateTimePicker>("dtpStartDate");
            if (dtpStartDate != null)
            {
                dtpStartDate.Checked = false;
                dtpStartDate.CustomFormat = " ";
            }

            DateTimePicker dtpEndDate = GetControlSafely<DateTimePicker>("dtpEndDate");
            if (dtpEndDate != null)
            {
                dtpEndDate.Checked = false;
                dtpEndDate.CustomFormat = " ";
            }

            CheckBox chkHotel = GetControlSafely<CheckBox>("chkHotel");
            if (chkHotel != null)
                chkHotel.Checked = false;

            // Enable days on site and start day controls
            EnableDaysOnSiteControl(true);
            EnableStartDayControl(true);

            // Clear datagrid
            DataGridView dataGridViewDays = (DataGridView)Controls.Find("dataGridViewDays", true)[0];
            dataGridViewDays.Rows.Clear();

            // Update day panels
            UpdateDayPanels();

            // Recalculate with reset values
            CalculateAndDisplayResults();
        }
    }
}