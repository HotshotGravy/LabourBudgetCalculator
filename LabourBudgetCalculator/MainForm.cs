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
            this.MinimumSize = new Size(1000, 750);

            // Make the form resizable
            this.FormBorderStyle = FormBorderStyle.Sizable;

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
            Label lblTravelDistance = new Label { Text = "Travel Distance to Site Area or Airport:", Location = new Point(20, 90), AutoSize = true };
            NumericUpDown numTravelDistance = new NumericUpDown
            {
                Name = "numTravelDistance",
                Location = new Point(255, 87),
                Size = new Size(75, 25),
                Maximum = 10000
            };
            Label lblTravelDistanceUnit = new Label { Text = "Miles", Location = new Point(335, 90), AutoSize = true };

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
            Label lblTravelTimeUnit = new Label { Text = "Hours", Location = new Point(335, 120), AutoSize = true };

            // Daily Travel Distance
            Label lblDailyTravelDistance = new Label { Text = "Daily Travel Distance (Round Trip):", Location = new Point(20, 150), AutoSize = true };

            NumericUpDown numDailyTravelDistance = new NumericUpDown
            {
                Name = "numDailyTravelDistance",
                Location = new Point(255, 147),
                Size = new Size(75, 25),
                Maximum = 1000,
                Increment = 30m
            };
            Label lblDailyTravelDistanceUnit = new Label { Text = "Miles", Location = new Point(335, 150), AutoSize = true };

            // Daily Travel Time
            Label lblDailyTravelTime = new Label { Text = "Daily Travel Time (Round Trip):", Location = new Point(20, 180), AutoSize = true };
            NumericUpDown numDailyTravelTime = new NumericUpDown
            {
                Name = "numDailyTravelTime",
                Location = new Point(255, 177),
                Size = new Size(75, 25),
                Maximum = 24,
                Increment = 0.5m,   // Add this line
                DecimalPlaces = 1
            };
            Label lblDailyTravelTimeUnit = new Label { Text = "Hours", Location = new Point(335, 180), AutoSize = true };

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
                Location = new Point(25, 486),
                Size = new Size(420, 260)
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
            Label lblFlightCostUnit = new Label { Text = "USD", Location = new Point(255, 30), AutoSize = true };

            // Rental Car
            CheckBox chkRentalCar = new CheckBox { Name = "chkRentalCar", Text = "Rental Car", Location = new Point(20, 60), AutoSize = true };
            Label lblDailyCost = new Label { Text = "Daily Cost:", Location = new Point(110, 60), AutoSize = true };
            NumericUpDown numRentalCarCost = new NumericUpDown
            {
                Name = "numRentalCarCost",
                Location = new Point(170, 57),
                Size = new Size(80, 25),
                Maximum = 500
            };
            Label lblRentalCarCostUnit = new Label { Text = "USD", Location = new Point(255, 60), AutoSize = true };

            // Hotel Cost
            Label lblHotelCost = new Label { Text = "Hotel Cost:", Location = new Point(20, 90), AutoSize = true };
            NumericUpDown numHotelCost = new NumericUpDown
            {
                Name = "numHotelCost",
                Location = new Point(170, 87),
                Size = new Size(80, 25),
                Maximum = 1000
            };
            Label lblHotelCostUnit = new Label { Text = "per Night", Location = new Point(255, 90), AutoSize = true };

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
            Label lblMileageRateUnit = new Label { Text = "per Mile", Location = new Point(255, 120), AutoSize = true };

            // Per Diem
            Label lblPerDiem = new Label { Text = "Per Diem:", Location = new Point(20, 150), AutoSize = true };
            NumericUpDown numPerDiem = new NumericUpDown
            {
                Name = "numPerDiem",
                Location = new Point(170, 147),
                Size = new Size(80, 25),
                Maximum = 500
            };
            Label lblPerDiemUnit = new Label { Text = "per Day", Location = new Point(255, 150), AutoSize = true };

            // Setup Rate Sheets button
            Button btnSetup = new Button
            {
                Name = "btnSetup",
                Text = "Setup Rate Sheets",
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

            btnDarkMode = new Button
            {
                Name = "btnDarkMode",
                Text = "",
                Location = new Point(290, 190), // Position it right after Reset button
                Size = new Size(7, 25),
                BackColor = Color.DarkGray
            };

            btnDarkMode.Click += (sender, e) => ToggleDarkMode();




            // Add controls to groupBoxExpenses including btnReset
            groupBoxExpenses.Controls.AddRange(new Control[] {
        lblFlightCost, numFlightCost, lblFlightCostUnit,
        chkRentalCar, lblDailyCost, numRentalCarCost, lblRentalCarCostUnit,
        lblHotelCost, numHotelCost, lblHotelCostUnit,
        lblMileageRate, numMileageRate, lblMileageRateUnit,
        lblPerDiem, numPerDiem, lblPerDiemUnit,
        btnSetup, btnReset, btnDarkMode 
    });
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
                Size = new Size(880, 350), // Reduced height for better visibility
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
                Location = new Point(120, 290),
                Size = new Size(40, 20),
                Font = new Font(this.Font, FontStyle.Bold)
            };
            groupBoxResults.Controls.Add(lblTotalDaysValue);

            Label lblTotalDays = new Label
            {
                Text = "Total Days:",
                Location = new Point(40, 290),
                AutoSize = true
            };
            groupBoxResults.Controls.Add(lblTotalDays);

            // Grand Total panel
            Panel grandTotalPanel = new Panel
            {
                Name = "grandTotalPanel",
                Size = new Size(260, 100),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.WhiteSmoke,
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
                ForeColor = Color.DarkBlue
            };
            grandTotalPanel.Controls.Add(lblGrandTotalSection);

            // Grand total value
            lblGrandTotal = new Label
            {
                Name = "lblGrandTotal",
                Text = "$0.00",
                Location = new Point(20, 50),
                Size = new Size(220, 40),
                Font = new Font(this.Font.FontFamily, 20, FontStyle.Bold)
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
                Color weekendColor = isDarkMode ? Color.LightPink : Color.Gray;
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
                for (int i = 0; i < totalDays; i++)
                {
                    try
                    {
                        Panel dayPanel = (Panel)Controls.Find($"dayPanel{i + 1}", true)[0];
                        Label dayOfWeekLabel = (Label)dayPanel.Controls.Find($"dayOfWeekLabel{i + 1}", false)[0];
                        Label laborHoursLabel = (Label)dayPanel.Controls.Find($"laborHoursLabel{i + 1}", false)[0];
                        Label travelHoursLabel = (Label)dayPanel.Controls.Find($"travelHoursLabel{i + 1}", false)[0];

                        // Make label backgrounds transparent
                        dayOfWeekLabel.BackColor = Color.Transparent;
                        laborHoursLabel.BackColor = Color.Transparent;
                        travelHoursLabel.BackColor = Color.Transparent;

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

                        // Set other labels' text color
                        laborHoursLabel.ForeColor = isDarkMode ? Color.White : SystemColors.ControlText;
                        travelHoursLabel.ForeColor = isDarkMode ? Color.White : SystemColors.ControlText;

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
                            travelHours = dailyTravelTime;

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
                // Show premium rates for all categories
                txtRegularLabour.Text = currentRateSheet.PremiumLabourRate.ToString("F2");
                txtOvertimeLabour.Text = currentRateSheet.PremiumLabourRate.ToString("F2");
                txtPremiumLabour.Text = currentRateSheet.PremiumLabourRate.ToString("F2");
                txtRegularTravel.Text = currentRateSheet.PremiumTravelRate.ToString("F2");
                txtOvertimeTravel.Text = currentRateSheet.PremiumTravelRate.ToString("F2");
                txtPremiumTravel.Text = currentRateSheet.PremiumTravelRate.ToString("F2");

                // Set text color to red for ALL textboxes
                foreach (TextBox textBox in rateTextBoxes)
                {
                    // Make sure ReadOnly is true so ForeColor will work
                    textBox.ReadOnly = true;
                    textBox.BackColor = SystemColors.Window; // Keep background white
                    textBox.ForeColor = Color.Red;
                }
            }
            else
            {
                // Show normal rates
                txtRegularLabour.Text = currentRateSheet.RegularLabourRate.ToString("F2");
                txtOvertimeLabour.Text = currentRateSheet.OvertimeLabourRate.ToString("F2");
                txtPremiumLabour.Text = currentRateSheet.PremiumLabourRate.ToString("F2");
                txtRegularTravel.Text = currentRateSheet.RegularTravelRate.ToString("F2");
                txtOvertimeTravel.Text = currentRateSheet.OvertimeTravelRate.ToString("F2");
                txtPremiumTravel.Text = currentRateSheet.PremiumTravelRate.ToString("F2");

                // Reset text color to default for ALL textboxes
                foreach (TextBox textBox in rateTextBoxes)
                {
                    // Keep ReadOnly property
                    textBox.ReadOnly = true;
                    textBox.BackColor = SystemColors.Window; // Keep background white
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

            numDiscount.ValueChanged += (s, e) => CalculateAndDisplayResults();
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

            numFlightCost.ValueChanged += (s, e) => CalculateAndDisplayResults();
            chkRentalCar.CheckedChanged += (s, e) => CalculateAndDisplayResults();
            numRentalCarCost.ValueChanged += (s, e) => CalculateAndDisplayResults();
            numHotelCost.ValueChanged += (s, e) => CalculateAndDisplayResults();
            numMileageRate.ValueChanged += (s, e) => CalculateAndDisplayResults();
            numPerDiem.ValueChanged += (s, e) => CalculateAndDisplayResults();
        }
        private void AdjustLayoutForCurrentSize()
        {
            try
            {
                // Find the results group box safely
                GroupBox groupBoxResults = null;
                foreach (Control c in this.Controls)
                {
                    if (c is GroupBox && c.Name == "groupBoxResults")
                    {
                        groupBoxResults = c as GroupBox;
                        break;
                    }
                }

                if (groupBoxResults == null)
                    return;

                // Adjust height more conservatively
                groupBoxResults.Height = Math.Min(450, this.ClientSize.Height - groupBoxResults.Location.Y - 20);

                // Find the grand total panel more safely
                Panel grandTotalPanel = null;
                foreach (Control c in groupBoxResults.Controls)
                {
                    if (c is Panel && c.Controls.Count > 0)
                    {
                        foreach (Control panelControl in c.Controls)
                        {
                            if (panelControl is Label && panelControl.Name == "lblGrandTotal")
                            {
                                grandTotalPanel = c as Panel;
                                break;
                            }
                        }
                        if (grandTotalPanel != null) break;
                    }
                }

                if (grandTotalPanel != null)
                {
                    // Use more conservative positioning
                    int newX = Math.Max(groupBoxResults.Width - grandTotalPanel.Width - 20, 400);
                    int newY = Math.Min(40, groupBoxResults.Height / 5);
                    grandTotalPanel.Location = new Point(newX, newY);

                    // Find the Reset button safely
                    Button btnReset = null;
                    foreach (Control c in groupBoxResults.Controls)
                    {
                        if (c is Button && c.Name == "btnReset")
                        {
                            btnReset = c as Button;
                            break;
                        }
                    }

                    // Position Reset button if found
                    if (btnReset != null)
                    {
                        btnReset.Location = new Point(
                            grandTotalPanel.Location.X + (grandTotalPanel.Width - btnReset.Width) / 2,
                            grandTotalPanel.Location.Y + grandTotalPanel.Height + 10);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"Error adjusting layout: {ex.Message}");
            }
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
                        if (dayPanel.BackColor == Color.LightGreen) // Holdover day
                        {
                            // Holdover days are 8 hours at regular rate no matter what day of week
                            regularLaborHours = laborHours;
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
                                // Regular hours (max 8), then overtime
                                regularLaborHours = Math.Min(8, laborHours);
                                overtimeLaborHours = Math.Max(0, laborHours - 8);

                                // For travel hours - apply same logic
                                regularTravelHours = Math.Min(8, travelHours);
                                overtimeTravelHours = Math.Max(0, travelHours - 8);
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

                        // Hotel cost - applied to all days except the last
                        if (i < totalDays - 1) // No hotel on last day
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

                        // Mileage calculation - the key fix
                        if (isFirstDay || isLastDay)
                        {
                            // First day or last day mileage - always apply if travel distance is specified
                            // (regardless of travel method or rental car)
                            if (travelDistanceToSite > 0)
                            {
                                dayMileage = mileageRate * travelDistanceToSite;
                            }
                        }
                        else if (travelMethod == "Driving" && !rentalCarChecked)
                        {
                            // For middle days, only apply mileage if:
                            // 1. Travel method is Driving AND
                            // 2. Not using a rental car
                            dayMileage = mileageRate * dailyTravelDistance;
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

                // Apply discount if any
                NumericUpDown numDiscount = (NumericUpDown)Controls.Find("numDiscount", true)[0];
                decimal discountPercent = numDiscount.Value;
                if (discountPercent > 0)
                {
                    decimal discountMultiplier = (1 - (discountPercent / 100));
                    totalLaborCost *= discountMultiplier;
                    totalTravelCost *= discountMultiplier;
                }

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
                }
                else if (control.Name == "lblGrandTotalSection")
                {
                    control.ForeColor = Color.LightSkyBlue; // Highlight heading in dark mode
                }
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
                }
                else if (control.Name == "lblGrandTotalSection")
                {
                    control.ForeColor = Color.DarkBlue; // Original color
                }
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
                                    dayOfWeekLabel.ForeColor = isDarkMode ? Color.LightPink : Color.Gray;
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
                        if (c is Label && c.Text.Contains("Travel Distance to Site Area"))
                        {
                            // Apply tooltip to travel distance label
                            toolTip.SetToolTip(c,
                                "This is usually for separate travel days, and includes distance to car rental location. " +
                                "If there is no separate travel day, this distance will supersede the daily travel distance for that day");

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
                        Value = laborHours
                    };

                    Label lblTravel = new Label { Text = "Travel Hours:", Location = new Point(20, 110), AutoSize = true };
                    NumericUpDown numTravel = new NumericUpDown
                    {
                        Location = new Point(150, 108),
                        Size = new Size(80, 25),
                        DecimalPlaces = 1,
                        Minimum = 0,
                        Maximum = 24,
                        Value = travelHours
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
                            clickedPanel.BackColor = Color.LightBlue;
                        else if (selectedType == "Travel Day")
                            clickedPanel.BackColor = Color.Yellow;
                        else if (selectedType == "Holdover Day")
                            clickedPanel.BackColor = Color.LightGreen;
                        else if (selectedType == "Nil")
                            clickedPanel.BackColor = SystemColors.Control;

                        // Mark as manually set
                        clickedPanel.Tag = "manual";

                        // Recalculate totals
                        CalculateAndDisplayResults();
                    }
                }
            }
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
            ((NumericUpDown)Controls.Find("numFlightCost", true)[0]).Value = 0;
            ((NumericUpDown)Controls.Find("numRentalCarCost", true)[0]).Value = 0;

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