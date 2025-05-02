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

namespace LabourBudgetCalculator
{
    public partial class MainForm : Form
    {
        private List<RateSheet> rateSheets;
        private RateSheet currentRateSheet;

        // Add these at the class level    
        private Label lblGrandTotalSection;
        private Label lblGrandTotal;
        private DataGridView dataGridViewDays;
        private Label lblTotalDays, lblTotalDaysValue, lblLabourSection, lblLabourHours, lblLabourHoursValue;
        private Label lblLabourCost, lblLabourCostValue, lblTravelSection, lblTravelHours, lblTravelHoursValue;
        private Label lblTravelCost, lblTravelCostValue, lblExpensesSection, lblExpensesCost, lblExpensesCostValue;
        // private Label lblGrandTotalSection, lblGrandTotal;

        public MainForm()
        {
            InitializeComponent();  
            this.Size = new Size(1438, 920);
            this.Text = "Time & Expense Calculator";
            this.StartPosition = FormStartPosition.CenterScreen;

            SetupUI();
            
            LoadData();
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

            // Setup event handlers
            SetupEventHandlers();
        }

        private void CreateRatesSection()
        {
            GroupBox groupBoxRates = new GroupBox
            {
                Name = "groupBoxRates",
                Text = "Rates",
                Location = new Point(25, 25),  // More spacing
                Size = new Size(420, 260)   // Changed from 200 to 220
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
            Label lblPercentage = new Label { Text = "%", Location = new Point(165, 50), AutoSize = true };

            // Emergency checkbox
            CheckBox chkEmergency = new CheckBox
            {
                Name = "chkEmergency",
                Text = "Emergency",
                Location = new Point(20, 75),
                AutoSize = true
            };

            // Add labels and textboxes for rates
            Label lblRegularLabour = new Label { Text = "Regular Labour", Location = new Point(20, 100), AutoSize = true };
            Label lblOvertimeLabour = new Label { Text = "Overtime Labour", Location = new Point(20, 125), AutoSize = true };
            Label lblPremiumLabour = new Label { Text = "Premium Labour", Location = new Point(20, 150), AutoSize = true };
            Label lblRegularTravel = new Label { Text = "Regular Travel", Location = new Point(20, 175), AutoSize = true };
            Label lblOvertimeTravel = new Label { Text = "Overtime Travel", Location = new Point(20, 200), AutoSize = true };
            Label lblPremiumTravel = new Label { Text = "Premium Travel", Location = new Point(20, 225), AutoSize = true };

            TextBox txtRegularLabour = new TextBox { Name = "txtRegularLabour", Location = new Point(120, 97), Size = new Size(60, 25), ReadOnly = true };
            TextBox txtOvertimeLabour = new TextBox { Name = "txtOvertimeLabour", Location = new Point(120, 122), Size = new Size(60, 25), ReadOnly = true };
            TextBox txtPremiumLabour = new TextBox { Name = "txtPremiumLabour", Location = new Point(120, 147), Size = new Size(60, 25), ReadOnly = true };
            TextBox txtRegularTravel = new TextBox { Name = "txtRegularTravel", Location = new Point(120, 172), Size = new Size(60, 25), ReadOnly = true };
            TextBox txtOvertimeTravel = new TextBox { Name = "txtOvertimeTravel", Location = new Point(120, 197), Size = new Size(60, 25), ReadOnly = true };
            TextBox txtPremiumTravel = new TextBox { Name = "txtPremiumTravel", Location = new Point(120, 222), Size = new Size(60, 25), ReadOnly = true };

            Label lblHr1 = new Label { Text = "/hr.", Location = new Point(181, 100), AutoSize = true };
            Label lblHr2 = new Label { Text = "/hr.", Location = new Point(181, 125), AutoSize = true };
            Label lblHr3 = new Label { Text = "/hr.", Location = new Point(181, 150), AutoSize = true };
            Label lblHr4 = new Label { Text = "/hr.", Location = new Point(181, 175), AutoSize = true };
            Label lblHr5 = new Label { Text = "/hr.", Location = new Point(181, 200), AutoSize = true };
            Label lblHr6 = new Label { Text = "/hr.", Location = new Point(181, 225), AutoSize = true };

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
                Location = new Point(25, 300),
                Size = new Size(420, 260)
            };
            this.Controls.Add(groupBoxTravel);

            // Separate Travel Day checkboxes
            Label lblSeparateTravel = new Label { Text = "Separate Travel Day", Location = new Point(20, 30), AutoSize = true };
            CheckBox chkSeparateTravelTo = new CheckBox { Name = "chkSeparateTravelTo", Text = "To", Location = new Point(220, 30), AutoSize = true };
            CheckBox chkSeparateTravelFrom = new CheckBox { Name = "chkSeparateTravelFrom", Text = "From", Location = new Point(260, 30), AutoSize = true };

            // Travel Method
            Label lblTravelMethod = new Label { Text = "Travel Method to Site Area:", Location = new Point(20, 60), AutoSize = true };
            ComboBox comboBoxTravelMethod = new ComboBox
            {
                Name = "comboBoxTravelMethod",
                Location = new Point(180, 57),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Travel Distance
            Label lblTravelDistance = new Label { Text = "Travel Distance to Site Area or Airport:", Location = new Point(20, 90), AutoSize = true };
            NumericUpDown numTravelDistance = new NumericUpDown
            {
                Name = "numTravelDistance",
                Location = new Point(250, 87),
                Size = new Size(80, 25),
                Maximum = 10000
            };
            Label lblTravelDistanceUnit = new Label { Text = "Miles", Location = new Point(335, 90), AutoSize = true };

            // Total Travel Time
            Label lblTravelTime = new Label { Text = "Total Travel Time to Site Area (Including Flight):", Location = new Point(20, 120), AutoSize = true };
            NumericUpDown numTravelTime = new NumericUpDown
            {
                Name = "numTravelTime",
                Location = new Point(250, 117),
                Size = new Size(80, 25),
                Maximum = 48,
                Increment = 0.5m,
                DecimalPlaces = 1
            };
            Label lblTravelTimeUnit = new Label { Text = "Hours", Location = new Point(335, 120), AutoSize = true };

            // Daily Travel Distance
            Label lblDailyTravelDistance = new Label { Text = "Daily Travel Distance:", Location = new Point(20, 150), AutoSize = true };

            NumericUpDown numDailyTravelDistance = new NumericUpDown
            {
                Name = "numDailyTravelDistance",
                Location = new Point(250, 147),
                Size = new Size(80, 25),
                Maximum = 1000
            };
            Label lblDailyTravelDistanceUnit = new Label { Text = "Miles", Location = new Point(335, 150), AutoSize = true };

            // Daily Travel Time
            Label lblDailyTravelTime = new Label { Text = "Daily Travel Time:", Location = new Point(20, 180), AutoSize = true };
            NumericUpDown numDailyTravelTime = new NumericUpDown
            {
                Name = "numDailyTravelTime",
                Location = new Point(250, 177),
                Size = new Size(80, 25),
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
                Location = new Point(25, 580),
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
            Label lblDailyCost = new Label { Text = "Daily Cost:", Location = new Point(150, 60), AutoSize = true };
            NumericUpDown numRentalCarCost = new NumericUpDown
            {
                Name = "numRentalCarCost",
                Location = new Point(220, 57),
                Size = new Size(80, 25),
                Maximum = 500
            };
            Label lblRentalCarCostUnit = new Label { Text = "USD", Location = new Point(305, 60), AutoSize = true };

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

            // Add controls to groupBoxExpenses
            groupBoxExpenses.Controls.AddRange(new Control[] {
                lblFlightCost, numFlightCost, lblFlightCostUnit,
                chkRentalCar, lblDailyCost, numRentalCarCost, lblRentalCarCostUnit,
                lblHotelCost, numHotelCost, lblHotelCostUnit,
                lblMileageRate, numMileageRate, lblMileageRateUnit,
                lblPerDiem, numPerDiem, lblPerDiemUnit,
                btnSetup
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
                Location = new Point(460, 445), // Adjusted from 450 to 445 to match new schedule position
                Size = new Size(880, 440)
            };
            this.Controls.Add(groupBoxResults);

            // DataGridView for day details
            dataGridViewDays = new DataGridView
            {
                Name = "dataGridViewDays",
                Location = new Point(20, 30),
                Size = new Size(760, 160),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true
            };
            groupBoxResults.Controls.Add(dataGridViewDays);

            // Total Days
            lblTotalDays = new Label
            {
                Text = "Total Days:",
                Location = new Point(20, 200),
                AutoSize = true
            };
            lblTotalDaysValue = new Label
            {
                Name = "lblTotalDaysValue",
                Text = "0",
                Location = new Point(150, 200),
                Size = new Size(40, 20),
                Font = new Font(this.Font, FontStyle.Bold)
            };

            // Labour section
            lblLabourSection = new Label
            {
                Text = "Labour",
                Location = new Point(20, 225),
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            lblLabourHours = new Label
            {
                Text = "Total Hours:",
                Location = new Point(40, 250),
                AutoSize = true
            };
            lblLabourHoursValue = new Label
            {
                Name = "lblLabourHoursValue",
                Text = "0",
                Location = new Point(150, 250),
                Size = new Size(40, 20)
            };
            lblLabourCost = new Label
            {
                Text = "Total Cost:",
                Location = new Point(250, 250),
                AutoSize = true
            };
            lblLabourCostValue = new Label
            {
                Name = "lblLabourCostValue",
                Text = "$0.00",
                Location = new Point(350, 250),
                Size = new Size(100, 20)
            };

            // Travel section
            lblTravelSection = new Label
            {
                Text = "Travel",
                Location = new Point(20, 275),
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            lblTravelHours = new Label
            {
                Text = "Total Hours:",
                Location = new Point(40, 300),
                AutoSize = true
            };
            lblTravelHoursValue = new Label
            {
                Name = "lblTravelHoursValue",
                Text = "0",
                Location = new Point(150, 300),
                Size = new Size(40, 20)
            };
            lblTravelCost = new Label
            {
                Text = "Total Cost:",
                Location = new Point(250, 300),
                AutoSize = true
            };
            lblTravelCostValue = new Label
            {
                Name = "lblTravelCostValue",
                Text = "$0.00",
                Location = new Point(350, 300),
                Size = new Size(100, 20)
            };

            // Expenses section (adjusted positions)
            lblExpensesSection = new Label
            {
                Text = "Expenses (Cost + 10%, not incl. per Diem)",
                Location = new Point(20, 325),  // Lowered Y position
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            lblExpensesCost = new Label
            {
                Text = "Total Cost:",
                Location = new Point(250, 350),  // Lowered Y position
                AutoSize = true
            };
            lblExpensesCostValue = new Label
            {
                Name = "lblExpensesCostValue",
                Text = "$0.00",
                Location = new Point(350, 350),  // Lowered Y position
                Size = new Size(100, 20)
            };

            // Grand total (adjusted positions)
            lblGrandTotalSection = new Label
            {
                Text = "Grand total",
                Location = new Point(500, 300),  // Moved down
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            lblGrandTotal = new Label
            {
                Name = "lblGrandTotal",
                Text = "$0.00",
                Location = new Point(500, 330),  // Moved down
                Size = new Size(150, 30),
                Font = new Font(this.Font.FontFamily, 16, FontStyle.Bold)
            };

            // Add all controls to groupBoxResults
            groupBoxResults.Controls.AddRange(new Control[] {
        lblTotalDays, lblTotalDaysValue,
        lblLabourSection, lblLabourHours, lblLabourHoursValue, lblLabourCost, lblLabourCostValue,
        lblTravelSection, lblTravelHours, lblTravelHoursValue, lblTravelCost, lblTravelCostValue,
        lblExpensesSection, lblExpensesCost, lblExpensesCostValue,
        lblGrandTotalSection, lblGrandTotal
    });


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
                Color weekendColor = Color.Gray;
                Font weekdayFont = new Font(this.Font, FontStyle.Regular);
                Font weekendFont = new Font(this.Font, FontStyle.Bold);

                // Reset all panels first (only those that are not manually set)
                for (int i = 0; i < 14; i++)
                {
                    Panel dayPanel = (Panel)Controls.Find($"dayPanel{i + 1}", true)[0];
                    if (dayPanel.Tag is int) // Not manually set yet
                    {
                        dayPanel.Tag = "auto"; // Mark as auto-assigned
                    }
                }

                // Update each day panel
                for (int i = 0; i < 14; i++)
                {
                    try
                    {
                        Panel dayPanel = (Panel)Controls.Find($"dayPanel{i + 1}", true)[0];
                        Label dayOfWeekLabel = (Label)dayPanel.Controls.Find($"dayOfWeekLabel{i + 1}", false)[0];
                        Label laborHoursLabel = (Label)dayPanel.Controls.Find($"laborHoursLabel{i + 1}", false)[0];
                        Label travelHoursLabel = (Label)dayPanel.Controls.Find($"travelHoursLabel{i + 1}", false)[0];

                        // If this day is beyond our project, reset and skip
                        if (i >= totalDays)
                        {
                            dayPanel.BackColor = SystemColors.Control;
                            laborHoursLabel.Text = "Labor: 0 hrs";
                            laborHoursLabel.Tag = "0";
                            travelHoursLabel.Text = "Travel: 0 hrs";
                            travelHoursLabel.Tag = "0";
                            dayOfWeekLabel.ForeColor = SystemColors.ControlText;
                            dayOfWeekLabel.Font = weekdayFont;
                            continue;
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
                            dayOfWeekLabel.ForeColor = SystemColors.ControlText;
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
                            dayPanel.BackColor = Color.Yellow;
                            travelHours = travelTimeToSite;
                            laborHours = 0;
                        }
                        else if (chkSeparateTravelFrom.Checked && i == totalDays - 1)
                        {
                            // Travel From day
                            dayPanel.BackColor = Color.Yellow;
                            travelHours = travelTimeToSite;
                            laborHours = 0;
                        }
                        else if (holdoverDayCheckBox.Checked && dayIndex == holdoverDayOfWeek)
                        {
                            // Holdover day - 8 hours at regular rate
                            dayPanel.BackColor = Color.LightGreen;
                            laborHours = 8; // Per requirements, holdover days get 8 hours at regular rate
                            travelHours = 0;
                        }
                        else
                        {
                            // Regular work day
                            dayPanel.BackColor = Color.LightBlue;
                            travelHours = dailyTravelTime;
                            laborHours = hoursPerDay;
                        }

                        // Update hour labels
                        laborHoursLabel.Text = $"Labor: {laborHours} hrs";
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
        private void SetupEventHandlers()
        {
            // Rate sheet selection change
            ComboBox comboBoxRateSheet = (ComboBox)Controls.Find("comboBoxRateSheet", true)[0];
            comboBoxRateSheet.SelectedIndexChanged += (sender, e) =>
            {
                if (comboBoxRateSheet.SelectedIndex >= 0 && comboBoxRateSheet.SelectedIndex < rateSheets.Count)
                {
                    currentRateSheet = rateSheets[comboBoxRateSheet.SelectedIndex];
                    DisplayRateSheet();
                    CalculateAndDisplayResults(); // Auto-calculate on change
                }
            };

            // Holdover day checkbox - initial setup
            CheckBox holdoverDayCheckBox = (CheckBox)Controls.Find("chkHoldoverDay", true)[0];
            ComboBox holdoverDayComboBox = (ComboBox)Controls.Find("comboBoxHoldoverDay", true)[0];
            holdoverDayCheckBox.CheckedChanged += (sender, e) =>
            {
                holdoverDayComboBox.Enabled = holdoverDayCheckBox.Checked;
                CalculateAndDisplayResults(); // Auto-calculate on change
                UpdateDayPanels(); // Update schedule display
            };

            // Start day change
            ComboBox comboBoxStartDay = (ComboBox)Controls.Find("comboBoxStartDay", true)[0];
            comboBoxStartDay.SelectedIndexChanged += (sender, e) =>
            {
                UpdateDayPanels();
                CalculateAndDisplayResults(); // Auto-calculate on change
            };

            // Setup button
            Button btnSetup = (Button)Controls.Find("btnSetup", true)[0];
            btnSetup.Click += (sender, e) =>
            {
                using (var setupForm = new SetupForm(rateSheets))
                {
                    if (setupForm.ShowDialog() == DialogResult.OK)
                    {
                        rateSheets = setupForm.UpdatedRateSheets;

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
            };

            // Additional event handlers for schedule updates
            NumericUpDown numDaysOnSite = (NumericUpDown)Controls.Find("numDaysOnSite", true)[0];
            numDaysOnSite.ValueChanged += (s, e) => UpdateDayPanels();

            NumericUpDown numHoursPerDay = (NumericUpDown)Controls.Find("numHoursPerDay", true)[0];
            numHoursPerDay.ValueChanged += (s, e) => UpdateDayPanels();

            // Already set up the holdoverDayCheckBox event handler above
            holdoverDayComboBox.SelectedIndexChanged += (s, e) => UpdateDayPanels();

            CheckBox chkSeparateTravelTo = (CheckBox)Controls.Find("chkSeparateTravelTo", true)[0];
            chkSeparateTravelTo.CheckedChanged += (s, e) => {
                UpdateDayPanels();
                CalculateAndDisplayResults();
            };

            CheckBox chkSeparateTravelFrom = (CheckBox)Controls.Find("chkSeparateTravelFrom", true)[0];
            chkSeparateTravelFrom.CheckedChanged += (s, e) => {
                UpdateDayPanels();
                CalculateAndDisplayResults();
            };

            NumericUpDown numTravelTime = (NumericUpDown)Controls.Find("numTravelTime", true)[0];
            numTravelTime.ValueChanged += (s, e) => UpdateDayPanels();

            NumericUpDown numDailyTravelTime = (NumericUpDown)Controls.Find("numDailyTravelTime", true)[0];
            numDailyTravelTime.ValueChanged += (s, e) => UpdateDayPanels();

            // Set up change events for all inputs to auto-calculate
            SetupAutoCalculateEvents();
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
            numFlightCost.Value = currentRateSheet.FlightCost;  // Add this line
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

                        // Hotel cost
                        decimal hotelCost = ((NumericUpDown)Controls.Find("numHotelCost", true)[0]).Value;
                        if (i < totalDays - 1) // No hotel on last day
                        {
                            dayHotel = hotelCost;
                        }

                        // Mileage
                        decimal mileageRate = ((NumericUpDown)Controls.Find("numMileageRate", true)[0]).Value;
                        string travelMethod = "Driving"; // Default value
                        ComboBox comboBoxTravelMethod = GetControlSafely<ComboBox>("comboBoxTravelMethod");
                        if (comboBoxTravelMethod != null && comboBoxTravelMethod.SelectedItem != null)
                        {
                            travelMethod = comboBoxTravelMethod.SelectedItem.ToString();
                        }

                        if (dayPanel.BackColor == Color.Yellow) // Travel day
                        {
                            if (travelMethod == "Driving")
                            {
                                decimal travelDistance = ((NumericUpDown)Controls.Find("numTravelDistance", true)[0]).Value;
                                dayMileage = mileageRate * travelDistance;
                            }
                            else if (travelMethod == "Flight")
                            {
                                dayFlight = ((NumericUpDown)Controls.Find("numFlightCost", true)[0]).Value;
                            }
                        }
                        else if (dayPanel.BackColor == Color.LightBlue) // Work day
                        {
                            // Check if rental car is checked
                            bool rentalCarChecked = false;
                            CheckBox chkRentalCar = GetControlSafely<CheckBox>("chkRentalCar");
                            if (chkRentalCar != null)
                                rentalCarChecked = chkRentalCar.Checked;

                            if (!rentalCarChecked) // Only apply mileage if rental car is NOT checked
                            {
                                decimal dailyDistance = GetNumericValueSafely("numDailyTravelDistance", 0);
                                dayMileage = mileageRate * dailyDistance;
                            }
                            // If rental car is checked, dayMileage remains 0
                        }

                        // Rental car
                        if (((CheckBox)Controls.Find("chkRentalCar", true)[0]).Checked)
                        {
                            dayRental = ((NumericUpDown)Controls.Find("numRentalCarCost", true)[0]).Value;
                        }

                        // Per diem
                        if (laborHours > 0 || travelHours > 0) // If there's any work or travel
                        {
                            dayPerDiem = ((NumericUpDown)Controls.Find("numPerDiem", true)[0]).Value;
                        }

                        // Track totals for expenses and per diem
                        totalExpenses += dayMileage + dayHotel + dayRental + dayFlight;
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

                // Add 10% to expenses
                totalExpenses *= 1.1m;

                // Update expenses display
                Label lblExpensesCostValue = (Label)Controls.Find("lblExpensesCostValue", true)[0];
                lblExpensesCostValue.Text = totalExpenses.ToString("C2");

                // Grand total
                decimal grandTotal = totalLaborCost + totalTravelCost + totalExpenses + totalPerDiem;

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

                    comboDayType.Items.AddRange(new string[] { "Work Day", "Travel Day", "Holdover Day" });
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