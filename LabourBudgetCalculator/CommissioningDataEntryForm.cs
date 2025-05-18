using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TimeExpenseCalculator.Helpers;
using TimeExpenseCalculator.Models;

namespace LabourBudgetCalculator
{
    public partial class CommissioningDataEntryForm : Form
    {
        private CommissioningProject currentProject;
        private List<RateSheet> rateSheets;
        private TabControl tabResources;
        private Button btnAddResource;
        private Button btnDeleteResource;
        private Timer autoSaveTimer;

        private CommissioningResultsWindow _resultsWindow;
        private CommissioningProject _project;

        private GroupBox groupBoxRates;
        private GroupBox groupBoxDays;
        private GroupBox groupBoxTravel;
        private GroupBox groupBoxExpenses;
        private GroupBox groupBoxSchedule;

        public CommissioningDataEntryForm(CommissioningProject project)
        {
            InitializeComponent();
            currentProject = project ?? throw new ArgumentNullException(nameof(project));
            SetupForm();
            SetupControls();
            LoadData();
            SetupAutoSave();
        }

        private void SetupForm()
        {
            this.Text = $"Commissioning Data Entry - {currentProject.ProjectName}";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
        }

        private void SetupControls()
        {
            // Create tab control for resources
            tabResources = new TabControl();
            tabResources.Location = new Point(12, 12);
            tabResources.Size = new Size(1360, 800);
            tabResources.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(tabResources);

            // Add resource management buttons
            btnAddResource = new Button();
            btnAddResource.Text = "Add Resource";
            btnAddResource.Size = new Size(100, 30);
            btnAddResource.Location = new Point(12, 820);
            btnAddResource.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnAddResource.Click += BtnAddResource_Click;
            this.Controls.Add(btnAddResource);

            btnDeleteResource = new Button();
            btnDeleteResource.Text = "Delete Resource";
            btnDeleteResource.Size = new Size(100, 30);
            btnDeleteResource.Location = new Point(120, 820);
            btnDeleteResource.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnDeleteResource.Click += BtnDeleteResource_Click;
            this.Controls.Add(btnDeleteResource);

            // If no resources exist, create the first one
            if (currentProject.Resources.Count == 0)
            {
                AddNewResource();
            }
            else
            {
                // Load existing resources
                foreach (var resource in currentProject.Resources)
                {
                    CreateResourceTab(resource);
                }
            }
        }

        private void CreateResourceTab(CommissioningResource resource)
        {
            // Create new tab page
            TabPage tabPage = new TabPage($"Resource {tabResources.TabPages.Count + 1}");
            tabPage.Tag = resource;

            // Create form content similar to MainForm but for commissioning
            CreateResourceFormContent(tabPage, resource);

            // Populate dropdowns for this tab
            PopulateTabDropdowns(tabPage);

            // Load resource data if it exists
            LoadResourceData(resource, tabPage);

            tabResources.TabPages.Add(tabPage);
        }

        private void CreateResourceFormContent(TabPage tabPage, CommissioningResource resource)
        {
            // This will create the main sections for each resource
            CreateRatesSection(tabPage, resource);
            CreateDaysSection(tabPage, resource);
            CreateTravelSection(tabPage, resource);
            CreateExpensesSection(tabPage, resource);
            CreateScheduleSection(tabPage, resource);
        }

        private void UpdateRateDisplay(TabPage tabPage)
        {
            var comboRateSheet = FindControlInTab<ComboBox>(tabPage, "comboBoxRateSheet");
            var numDiscount = FindControlInTab<NumericUpDown>(tabPage, "numDiscount");
            var chkEmergency = FindControlInTab<CheckBox>(tabPage, "chkEmergency");

            if (comboRateSheet == null || comboRateSheet.SelectedIndex < 0) return;

            var selectedRateSheet = rateSheets[comboRateSheet.SelectedIndex];
            bool isEmergency = chkEmergency?.Checked ?? false;
            decimal discountPercent = numDiscount?.Value ?? 0;
            decimal discountMultiplier = 1 - (discountPercent / 100);

            // Update rate display fields
            var txtRegularLabour = FindControlInTab<TextBox>(tabPage, "txtRegularLabour");
            var txtOvertimeLabour = FindControlInTab<TextBox>(tabPage, "txtOvertimeLabour");
            var txtPremiumLabour = FindControlInTab<TextBox>(tabPage, "txtPremiumLabour");
            var txtRegularTravel = FindControlInTab<TextBox>(tabPage, "txtRegularTravel");
            var txtOvertimeTravel = FindControlInTab<TextBox>(tabPage, "txtOvertimeTravel");
            var txtPremiumTravel = FindControlInTab<TextBox>(tabPage, "txtPremiumTravel");

            if (isEmergency)
            {
                // Emergency rates - all premium
                decimal premiumLabour = selectedRateSheet.PremiumLabourRate * discountMultiplier;
                decimal premiumTravel = selectedRateSheet.PremiumTravelRate * discountMultiplier;

                if (txtRegularLabour != null) txtRegularLabour.Text = premiumLabour.ToString("F2");
                if (txtOvertimeLabour != null) txtOvertimeLabour.Text = premiumLabour.ToString("F2");
                if (txtPremiumLabour != null) txtPremiumLabour.Text = premiumLabour.ToString("F2");
                if (txtRegularTravel != null) txtRegularTravel.Text = premiumTravel.ToString("F2");
                if (txtOvertimeTravel != null) txtOvertimeTravel.Text = premiumTravel.ToString("F2");
                if (txtPremiumTravel != null) txtPremiumTravel.Text = premiumTravel.ToString("F2");
            }
            else
            {
                // Normal rates
                if (txtRegularLabour != null) txtRegularLabour.Text = (selectedRateSheet.RegularLabourRate * discountMultiplier).ToString("F2");
                if (txtOvertimeLabour != null) txtOvertimeLabour.Text = (selectedRateSheet.OvertimeLabourRate * discountMultiplier).ToString("F2");
                if (txtPremiumLabour != null) txtPremiumLabour.Text = (selectedRateSheet.PremiumLabourRate * discountMultiplier).ToString("F2");
                if (txtRegularTravel != null) txtRegularTravel.Text = (selectedRateSheet.RegularTravelRate * discountMultiplier).ToString("F2");
                if (txtOvertimeTravel != null) txtOvertimeTravel.Text = (selectedRateSheet.OvertimeTravelRate * discountMultiplier).ToString("F2");
                if (txtPremiumTravel != null) txtPremiumTravel.Text = (selectedRateSheet.PremiumTravelRate * discountMultiplier).ToString("F2");
            }
        }

        private void CreateRatesSection(TabPage tabPage, CommissioningResource resource)
        {
            groupBoxRates = new GroupBox
            {
                Name = "groupBoxRates",
                Text = "Rates",
                Location = new Point(25, 25),
                Size = new Size(420, 180)
            };
            tabPage.Controls.Add(groupBoxRates);

            // Add label and combobox for rate sheet selection
            Label lblRateSheet = new Label { Text = "Rate Sheet:", Location = new Point(20, 23), AutoSize = true };
            var comboBoxRateSheet = new ComboBox
            {
                Name = "comboBoxRateSheet",
                Location = new Point(120, 20),
                Size = new Size(180, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            comboBoxRateSheet.SelectedIndexChanged += (s, e) => {
                UpdateRateDisplay(tabPage);
                UpdateResourceFromUI(resource);
            };

            // Discount field
            Label lblDiscount = new Label { Text = "Discount", Location = new Point(20, 50), AutoSize = true };
            var numDiscount = new NumericUpDown
            {
                Name = "numDiscount",
                Location = new Point(120, 48),
                Size = new Size(60, 25),
                Maximum = 100,
                Minimum = 0,
                Value = 0
            };
            numDiscount.ValueChanged += (s, e) => {
                UpdateRateDisplay(tabPage);
                UpdateResourceFromUI(resource);
            };
            Label lblPercentage = new Label { Text = "%", Location = new Point(181, 50), AutoSize = true };

            // Emergency checkbox
            var chkEmergency = new CheckBox
            {
                Name = "chkEmergency",
                Text = "Emergency",
                Location = new Point(220, 50),
                AutoSize = true
            };
            chkEmergency.CheckedChanged += (s, e) => {
                UpdateRateDisplay(tabPage);
                UpdateResourceFromUI(resource);
            };

            // Add rate display labels (similar to MainForm)
            CreateRateDisplayLabels(groupBoxRates);

            groupBoxRates.Controls.AddRange(new Control[] {
                lblRateSheet, comboBoxRateSheet,
                lblDiscount, numDiscount, lblPercentage,
                chkEmergency
            });
        }

        private void CreateRateDisplayLabels(GroupBox groupBox)
        {
            // Similar to MainForm rate display
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

            groupBox.Controls.AddRange(new Control[] {
                lblRegularLabour, txtRegularLabour, lblHr1,
                lblOvertimeLabour, txtOvertimeLabour, lblHr2,
                lblPremiumLabour, txtPremiumLabour, lblHr3,
                lblRegularTravel, txtRegularTravel, lblHr4,
                lblOvertimeTravel, txtOvertimeTravel, lblHr5,
                lblPremiumTravel, txtPremiumTravel, lblHr6
            });
        }

        // Update the event handlers in CreateDaysSection to regenerate schedule when values change
        private void CreateDaysSection(TabPage tabPage, CommissioningResource resource)
        {
            groupBoxDays = new GroupBox
            {
                Name = "groupBoxDays",
                Text = "Days Configuration",
                Location = new Point(460, 25),
                Size = new Size(420, 180)
            };
            tabPage.Controls.Add(groupBoxDays);

            // Days on Site
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
                Maximum = 60, // Increased for commissioning projects
                Value = 1
            };
            numDaysOnSite.ValueChanged += (s, e) => {
                UpdateResourceFromUI(resource);
                RegenerateSchedule(tabPage, resource); // Add this line
            };

            // Hours per Day
            Label lblHoursPerDay = new Label
            {
                Text = "Default Hours per Day:",
                Location = new Point(190, 30),
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
            numHoursPerDay.ValueChanged += (s, e) => {
                UpdateResourceFromUI(resource);
                RegenerateSchedule(tabPage, resource); // Add this line
            };

            // Start Day
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
            comboBoxStartDay.SelectedIndexChanged += (s, e) => {
                UpdateResourceFromUI(resource);
                RegenerateSchedule(tabPage, resource); // Add this line
            };

            // Default Start Time (new for commissioning)
            Label lblDefaultStartTime = new Label
            {
                Text = "Default Start Time:",
                Location = new Point(20, 105),
                AutoSize = true
            };

            ComboBox comboBoxStartTime = new ComboBox
            {
                Name = "comboBoxStartTime",
                Location = new Point(140, 102),
                Size = new Size(80, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            comboBoxStartTime.SelectedIndexChanged += (s, e) => {
                UpdateResourceFromUI(resource);
                RegenerateSchedule(tabPage, resource); // Add this line
            };

            // Lunch Duration (new for commissioning)
            Label lblLunchDuration = new Label
            {
                Text = "Lunch Duration:",
                Location = new Point(20, 140),
                AutoSize = true
            };

            ComboBox comboBoxLunchDuration = new ComboBox
            {
                Name = "comboBoxLunchDuration",
                Location = new Point(140, 137),
                Size = new Size(80, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            comboBoxLunchDuration.SelectedIndexChanged += (s, e) => {
                UpdateResourceFromUI(resource);
                RegenerateSchedule(tabPage, resource); // Add this line
            };

            groupBoxDays.Controls.AddRange(new Control[] {
        lblDaysOnSite, numDaysOnSite,
        lblHoursPerDay, numHoursPerDay,
        lblStartDay, comboBoxStartDay,
        lblDefaultStartTime, comboBoxStartTime,
        lblLunchDuration, comboBoxLunchDuration
    });
        }

        // Add this helper method to regenerate the schedule when configuration changes
        private void RegenerateSchedule(TabPage tabPage, CommissioningResource resource)
        {
            var schedulePanel = FindControlInTab<Panel>(tabPage, "panelSchedule");
            if (schedulePanel != null)
            {
                GenerateCalendarLayout(schedulePanel, resource);
            }
        }

        private void CreateTravelSection(TabPage tabPage, CommissioningResource resource)
        {
            groupBoxTravel = new GroupBox
            {
                Name = "groupBoxTravel",
                Text = "Travel Options",
                Location = new Point(25, 215),
                Size = new Size(420, 260)
            };
            tabPage.Controls.Add(groupBoxTravel);

            // Separate Travel Day checkboxes
            Label lblSeparateTravel = new Label { Text = "Separate Travel Day", Location = new Point(20, 30), AutoSize = true };
            CheckBox chkSeparateTravelTo = new CheckBox { Name = "chkSeparateTravelTo", Text = "To", Location = new Point(255, 30), AutoSize = true };
            CheckBox chkSeparateTravelFrom = new CheckBox { Name = "chkSeparateTravelFrom", Text = "From", Location = new Point(295, 30), AutoSize = true };

            chkSeparateTravelTo.CheckedChanged += (s, e) => UpdateResourceFromUI(resource);
            chkSeparateTravelFrom.CheckedChanged += (s, e) => UpdateResourceFromUI(resource);

            // Travel Method
            Label lblTravelMethod = new Label { Text = "Travel Method to Site Area:", Location = new Point(20, 60), AutoSize = true };
            ComboBox comboBoxTravelMethod = new ComboBox
            {
                Name = "comboBoxTravelMethod",
                Location = new Point(255, 57),
                Size = new Size(75, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            comboBoxTravelMethod.SelectedIndexChanged += (s, e) => UpdateResourceFromUI(resource);

            // Travel Distance
            Label lblTravelDistance = new Label { Text = "Driving Distance (First and Last Days Only):", Location = new Point(20, 90), AutoSize = true };
            NumericUpDown numTravelDistance = new NumericUpDown
            {
                Name = "numTravelDistance",
                Location = new Point(255, 87),
                Size = new Size(75, 25),
                Maximum = 10000
            };
            numTravelDistance.ValueChanged += (s, e) => UpdateResourceFromUI(resource);
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
            numTravelTime.ValueChanged += (s, e) => UpdateResourceFromUI(resource);
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
            numDailyTravelDistance.ValueChanged += (s, e) => UpdateResourceFromUI(resource);
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
            numDailyTravelTime.ValueChanged += (s, e) => UpdateResourceFromUI(resource);
            Label lblDailyTravelTimeUnit = new Label { Text = "hours", Location = new Point(335, 180), AutoSize = true };

            groupBoxTravel.Controls.AddRange(new Control[] {
                lblSeparateTravel, chkSeparateTravelTo, chkSeparateTravelFrom,
                lblTravelMethod, comboBoxTravelMethod,
                lblTravelDistance, numTravelDistance, lblTravelDistanceUnit,
                lblTravelTime, numTravelTime, lblTravelTimeUnit,
                lblDailyTravelDistance, numDailyTravelDistance, lblDailyTravelDistanceUnit,
                lblDailyTravelTime, numDailyTravelTime, lblDailyTravelTimeUnit
            });
        }

        private void CreateExpensesSection(TabPage tabPage, CommissioningResource resource)
        {
            groupBoxExpenses = new GroupBox
            {
                Name = "groupBoxExpenses",
                Text = "Expenses",
                Location = new Point(25, 485),
                Size = new Size(420, 250)
            };
            tabPage.Controls.Add(groupBoxExpenses);

            // Flight Cost
            Label lblFlightCost = new Label { Text = "Flight Cost (One Way):", Location = new Point(20, 30), AutoSize = true };
            NumericUpDown numFlightCost = new NumericUpDown
            {
                Name = "numFlightCost",
                Location = new Point(170, 27),
                Size = new Size(80, 25),
                Maximum = 10000
            };
            numFlightCost.ValueChanged += (s, e) => UpdateResourceFromUI(resource);

            // Rental Car
            CheckBox chkRentalCar = new CheckBox { Name = "chkRentalCar", Text = "Rental Car", Location = new Point(20, 60), AutoSize = true };
            chkRentalCar.CheckedChanged += (s, e) => UpdateResourceFromUI(resource);
            NumericUpDown numRentalCarCost = new NumericUpDown
            {
                Name = "numRentalCarCost",
                Location = new Point(170, 57),
                Size = new Size(80, 25),
                Maximum = 500
            };
            numRentalCarCost.ValueChanged += (s, e) => UpdateResourceFromUI(resource);
            Label lblRentalCarUnit = new Label { Text = "per day", Location = new Point(255, 60), AutoSize = true };

            // Hotel
            CheckBox chkHotel = new CheckBox
            {
                Name = "chkHotel",
                Text = "Hotel",
                Location = new Point(20, 90),
                AutoSize = true
            };
            chkHotel.CheckedChanged += (s, e) => UpdateResourceFromUI(resource);

            NumericUpDown numHotelCost = new NumericUpDown
            {
                Name = "numHotelCost",
                Location = new Point(170, 87),
                Size = new Size(80, 25),
                Maximum = 1000
            };
            numHotelCost.ValueChanged += (s, e) => UpdateResourceFromUI(resource);
            Label lblHotelUnit = new Label { Text = "per night", Location = new Point(255, 90), AutoSize = true };

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
            numMileageRate.ValueChanged += (s, e) => UpdateResourceFromUI(resource);
            Label lblMileageUnit = new Label { Text = "per mile / km", Location = new Point(255, 120), AutoSize = true };

            // Per Diem
            Label lblPerDiem = new Label { Text = "Per Diem:", Location = new Point(20, 150), AutoSize = true };
            NumericUpDown numPerDiem = new NumericUpDown
            {
                Name = "numPerDiem",
                Location = new Point(170, 147),
                Size = new Size(80, 25),
                Maximum = 500
            };
            numPerDiem.ValueChanged += (s, e) => UpdateResourceFromUI(resource);
            Label lblPerDiemUnit = new Label { Text = "per day", Location = new Point(255, 150), AutoSize = true };

            groupBoxExpenses.Controls.AddRange(new Control[] {
                lblFlightCost, numFlightCost,
                chkRentalCar, numRentalCarCost, lblRentalCarUnit,
                chkHotel, numHotelCost, lblHotelUnit,
                lblMileageRate, numMileageRate, lblMileageUnit,
                lblPerDiem, numPerDiem, lblPerDiemUnit
            });
        }


        private void CreateScheduleSection(TabPage tabPage, CommissioningResource resource)
        {
            groupBoxSchedule = new GroupBox
            {
                Name = "groupBoxSchedule",
                Text = "Schedule",
                Location = new Point(460, 215),
                Size = new Size(880, 470)
            };
            tabPage.Controls.Add(groupBoxSchedule);

            // Create a scroll panel for the calendar
            var panelSchedule = new Panel
            {
                Name = "panelSchedule",
                Location = new Point(10, 20),
                Size = new Size(860, 440),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            groupBoxSchedule.Controls.Add(panelSchedule);

            // Generate initial calendar when section is created
            GenerateCalendarLayout(panelSchedule, resource);
        }

        private void GenerateCalendarLayout(Panel parentPanel, CommissioningResource resource)
        {
            // Clear existing controls
            parentPanel.Controls.Clear();

            // Get the current tab page from the resource
            TabPage currentTab = null;
            foreach (TabPage tab in tabResources.TabPages)
            {
                if (tab.Tag == resource)
                {
                    currentTab = tab;
                    break;
                }
            }

            if (currentTab == null) return;

 .Controls.Clear();

            // Get the current tab page from the resource
            TabPage currentTab = null;
            foreach (TabPage tab in tabResources.TabPages)
            {
                if (tab.Tag == resource)
                {
                    currentTab = tab;
                    break;
                }
            }

            if (currentTab == null) return;

            // Get configuration values
            var numDaysOnSite = FindControlInTab<NumericUpDown>(currentTab, "numDaysOnSite");
            var comboStartDay = FindControlInTab<ComboBox>(currentTab, "comboBoxStartDay");
            var comboStartTime = FindControlInTab<ComboBox>(currentTab, "comboBoxStartTime");
            var comboLunchDuration = FindControlInTab<ComboBox>(currentTab, "comboBoxLunchDuration");
            var numHoursPerDay = FindControlInTab<NumericUpDown>(currentTab, "numHoursPerDay");

            if (numDaysOnSite == null || comboStartDay == null) return;

            int daysOnSite = (int)numDaysOnSite.Value;
            string startDayText = comboStartDay.SelectedItem?.ToString() ?? "Monday";
            string defaultStartTime = comboStartTime?.SelectedItem?.ToString() ?? "07:00";
            decimal defaultHoursPerDay = numHoursPerDay?.Value ?? 10;

            // Parse lunch duration
            string lunchDurationText = comboLunchDuration?.SelectedItem?.ToString() ?? "0.5 hours";
            decimal lunchHours = 0.5m;
            if (lunchDurationText.Contains("0.0")) lunchHours = 0m;
            else if (lunchDurationText.Contains("1.0")) lunchHours = 1m;

            // Calculate project start date
            DateTime projectStartDate = GetNextOccurrence(DateTime.Today, startDayText);
            DateTime projectEndDate = projectStartDate.AddDays(daysOnSite - 1);

            // Calculate the first Sunday to show (start of week containing project start)
            DateTime firstSunday = projectStartDate.AddDays(-(int)projectStartDate.DayOfWeek);

            // Calculate the last Sunday to show (start of week containing project end)
            DateTime lastSunday = projectEndDate.AddDays(-(int)projectEndDate.DayOfWeek);

            // Calculate total weeks needed
            int totalWeeks = (int)((lastSunday - firstSunday).TotalDays / 7) + 1;

            // Create calendar layout
            int availableWidth = 850; // Total width minus margins
            int margin = 5;
            int dayBoxWidth = (availableWidth - (6 * margin)) / 7; // 7 days per week
            int dayBoxHeight = 100; // Reduced height
            int weekHeight = dayBoxHeight + 30; // Extra height for week label

            for (int week = 0; week < totalWeeks; week++)
            {
                // Week label
                DateTime weekStart = firstSunday.AddDays(week * 7);
                var lblWeek = new Label
                {
                    Text = $"Week of {weekStart.ToString("MMM dd, yyyy")}",
                    Location = new Point(5, week * weekHeight + 5),
                    Font = new Font("Microsoft Sans Serif", 9, FontStyle.Bold),
                    AutoSize = true
                };
                parentPanel.Controls.Add(lblWeek);

                // Create 7 day boxes for the week
                for (int dayOfWeek = 0; dayOfWeek < 7; dayOfWeek++)
                {
                    DateTime currentDate = weekStart.AddDays(dayOfWeek);
                    int dayBoxX = 5 + dayOfWeek * (dayBoxWidth + margin);
                    int dayBoxY = week * weekHeight + 25;

                    // Determine if this day is part of the project
                    bool isProjectDay = currentDate >= projectStartDate && currentDate <= projectEndDate;
                    bool isBeforeProject = currentDate < projectStartDate;

                    // Create day box
                    var dayBox = new Panel
                    {
                        Name = $"dayBox_{week}_{dayOfWeek}",
                        Location = new Point(dayBoxX, dayBoxY),
                        Size = new Size(dayBoxWidth, dayBoxHeight),
                        BorderStyle = BorderStyle.FixedSingle,
                        BackColor = isProjectDay ? Color.White : (isBeforeProject ? Color.LightGray : Color.White)
                    };

                    if (isProjectDay)
                    {
                        // Create day content
                        CreateDayContent(dayBox, currentDate, resource, defaultStartTime, defaultHoursPerDay + lunchHours);
                    }
                    else
                    {
                        // Show just the date for non-project days
                        var lblDate = new Label
                        {
                            Text = $"{currentDate.ToString("MMM dd")}\n{currentDate.DayOfWeek.ToString().Substring(0, 3)}",
                            Location = new Point(5, 5),
                            AutoSize = true,
                            ForeColor = Color.Gray
                        };
                        dayBox.Controls.Add(lblDate);
                    }

                    parentPanel.Controls.Add(dayBox);
                }
            }
        }

        private void CreateDayContent(Panel dayBox, DateTime date, CommissioningResource resource, string defaultStartTime, decimal defaultTotalHours)
        {
            // Date and day label
            var lblDate = new Label
            {
                Text = $"{date.ToString("MMM dd")} - {date.DayOfWeek.ToString().Substring(0, 3)}",
                Location = new Point(5, 5),
                AutoSize = true,
                Font = new Font("Microsoft Sans Serif", 8, FontStyle.Bold)
            };

            // Start time combo
            var lblStartLabel = new Label
            {
                Text = "Start:",
                Location = new Point(5, 25),
                AutoSize = true,
                Font = new Font("Microsoft Sans Serif", 8)
            };

            var comboStart = new ComboBox
            {
                Name = $"comboStart_{date.ToString("yyyyMMdd")}",
                Size = new Size(70, 20),
                Location = new Point(40, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft Sans Serif", 8)
            };
            PopulateTime12HourCombo(comboStart);
            comboStart.SelectedItem = ConvertTo12Hour(defaultStartTime);

            // End time combo
            string defaultEndTime = CalculateEndTime(defaultStartTime, defaultTotalHours);
            var lblEndLabel = new Label
            {
                Text = "End:",
                Location = new Point(5, 45),
                AutoSize = true,
                Font = new Font("Microsoft Sans Serif", 8)
            };

            var comboEnd = new ComboBox
            {
                Name = $"comboEnd_{date.ToString("yyyyMMdd")}",
                Size = new Size(70, 20),
                Location = new Point(40, 43),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft Sans Serif", 8)
            };
            PopulateTime12HourCombo(comboEnd);
            comboEnd.SelectedItem = ConvertTo12Hour(defaultEndTime);

            // Travel time combo
            var lblTravelLabel = new Label
            {
                Text = "Travel:",
                Location = new Point(5, 65),
                AutoSize = true,
                Font = new Font("Microsoft Sans Serif", 8)
            };

            var comboTravel = new ComboBox
            {
                Name = $"comboTravel_{date.ToString("yyyyMMdd")}",
                Size = new Size(55, 20),
                Location = new Point(50, 63),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft Sans Serif", 8)
            };
            PopulateTravelCombo(comboTravel);
            comboTravel.SelectedItem = "1.5";

            // Set up change events
            comboStart.SelectedIndexChanged += (s, e) => UpdateResourceFromUI(resource);
            comboEnd.SelectedIndexChanged += (s, e) => UpdateResourceFromUI(resource);
            comboTravel.SelectedIndexChanged += (s, e) => UpdateResourceFromUI(resource);

            // Add all to day box
            dayBox.Controls.AddRange(new Control[] {
                lblDate, lblStartLabel, comboStart, lblEndLabel, comboEnd, lblTravelLabel, comboTravel
            });
        }

        private void PopulateTime12HourCombo(ComboBox combo)
        {
            combo.Items.Clear();
            for (int hour = 0; hour < 24; hour++)
            {
                // Add hours with AM/PM format
                combo.Items.Add(ConvertTo12Hour($"{hour:D2}:00"));
                combo.Items.Add(ConvertTo12Hour($"{hour:D2}:30"));
            }
        }

        private string ConvertTo12Hour(string time24)
        {
            if (string.IsNullOrEmpty(time24)) return "7:00 AM";

            string[] parts = time24.Split(':');
            if (parts.Length != 2) return "7:00 AM";

            int hour = int.Parse(parts[0]);
            int minute = int.Parse(parts[1]);

            string period = hour >= 12 ? "PM" : "AM";
            if (hour == 0) hour = 12;
            else if (hour > 12) hour -= 12;

            return $"{hour}:{minute:D2} {period}";
        }

        private string ConvertTo24Hour(string time12)
        {
            if (string.IsNullOrEmpty(time12)) return "07:00";

            string[] parts = time12.Split(' ');
            if (parts.Length != 2) return "07:00";

            string[] timeParts = parts[0].Split(':');
            if (timeParts.Length != 2) return "07:00";

            int hour = int.Parse(timeParts[0]);
            int minute = int.Parse(timeParts[1]);
            string period = parts[1];

            if (period == "PM" && hour != 12) hour += 12;
            else if (period == "AM" && hour == 12) hour = 0;

            return $"{hour:D2}:{minute:D2}";
        }

        private void PopulateTravelCombo(ComboBox combo)
        {
            combo.Items.Clear();
            for (decimal hours = 0; hours <= 12; hours += 0.5m)
            {
                combo.Items.Add(hours.ToString("0.0"));
            }
        }

        private string CalculateEndTime(string startTime, decimal totalHours)
        {
            if (string.IsNullOrEmpty(startTime)) return "17:00";

            string[] parts = startTime.Split(':');
            if (parts.Length != 2) return "17:00";

            int startHour = int.Parse(parts[0]);
            int startMinute = int.Parse(parts[1]);

            decimal startDecimal = startHour + (startMinute / 60m);
            decimal endDecimal = startDecimal + totalHours;

            // Handle overflow past 24 hours
            if (endDecimal >= 24) endDecimal -= 24;

            int endHour = (int)endDecimal;
            int endMinute = (int)((endDecimal - endHour) * 60);

            // Round to nearest 30 minutes
            if (endMinute > 0 && endMinute < 30) endMinute = 30;
            else if (endMinute > 30) { endMinute = 0; endHour++; }

            if (endHour >= 24) endHour = 0;

            return $"{endHour:D2}:{endMinute:D2}";
        }

        private DateTime GetNextOccurrence(DateTime startDate, string dayOfWeek)
        {
            DayOfWeek targetDay;
            if (!Enum.TryParse(dayOfWeek, out targetDay))
                targetDay = DayOfWeek.Monday;

            int daysUntilTarget = ((int)targetDay - (int)startDate.DayOfWeek + 7) % 7;
            if (daysUntilTarget == 0) daysUntilTarget = 7; // If today is the target day, move to next week

            return startDate.AddDays(daysUntilTarget);
        }

        private void LoadData()
        {
            try
            {
                // Load rate sheets first
                rateSheets = DataManager.LoadRateSheets();

                // Check if rate sheets loaded successfully
                if (rateSheets == null || rateSheets.Count == 0)
                {
                    MessageBox.Show("No rate sheets found. Creating a default rate sheet.", "Warning",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    rateSheets = new List<RateSheet> { CreateDefaultRateSheet() };
                }

                // Populate all dropdown lists for all tabs
                PopulateDropDownLists();

                // Load values into the first tab
                if (tabResources.TabPages.Count > 0 && currentProject.Resources.Count > 0)
                {
                    LoadResourceData(currentProject.Resources[0], tabResources.TabPages[0]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private RateSheet CreateDefaultRateSheet()
        {
            return new RateSheet("Default")
            {
                RegularLabourRate = 155,
                OvertimeLabourRate = 232.5m,
                PremiumLabourRate = 310,
                RegularTravelRate = 124,
                OvertimeTravelRate = 186,
                PremiumTravelRate = 248,
                HotelCost = 170,
                PerDiemRate = 80,
                MileageRate = 0.70m,
                RentalCarRate = 120,
                FlightCost = 400
            };
        }

        private void PopulateDropDownLists()
        {
            // Populate all tabs with dropdown options
            foreach (TabPage tabPage in tabResources.TabPages)
            {
                PopulateTabDropdowns(tabPage);
            }
        }

        private void PopulateTabDropdowns(TabPage tabPage)
        {
            try
            {
                // Rate sheets
                var comboRateSheet = FindControlInTab<ComboBox>(tabPage, "comboBoxRateSheet");
                if (comboRateSheet != null && rateSheets != null)
                {
                    comboRateSheet.Items.Clear();
                    foreach (var rateSheet in rateSheets)
                    {
                        comboRateSheet.Items.Add(rateSheet.Name);
                    }
                    if (comboRateSheet.Items.Count > 0)
                        comboRateSheet.SelectedIndex = 0;
                }

                // Days of week
                var comboStartDay = FindControlInTab<ComboBox>(tabPage, "comboBoxStartDay");
                if (comboStartDay != null)
                {
                    comboStartDay.Items.Clear();
                    string[] daysOfWeek = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
                    comboStartDay.Items.AddRange(daysOfWeek);
                    comboStartDay.SelectedIndex = 0; // Monday
                }

                // Start times (30-minute increments)
                var comboStartTime = FindControlInTab<ComboBox>(tabPage, "comboBoxStartTime");
                if (comboStartTime != null)
                {
                    comboStartTime.Items.Clear();
                    for (int hour = 0; hour < 24; hour++)
                    {
                        comboStartTime.Items.Add($"{hour:D2}:00");
                        comboStartTime.Items.Add($"{hour:D2}:30");
                    }
                    // Set default to 7:00 AM
                    comboStartTime.SelectedItem = "07:00";
                }

                // Lunch duration
                var comboLunchDuration = FindControlInTab<ComboBox>(tabPage, "comboBoxLunchDuration");
                if (comboLunchDuration != null)
                {
                    comboLunchDuration.Items.Clear();
                    comboLunchDuration.Items.AddRange(new[] { "0.0 hours", "0.5 hours", "1.0 hours" });
                    comboLunchDuration.SelectedIndex = 1; // 0.5 hours default
                }

                // Travel methods
                var comboTravelMethod = FindControlInTab<ComboBox>(tabPage, "comboBoxTravelMethod");
                if (comboTravelMethod != null)
                {
                    comboTravelMethod.Items.Clear();
                    comboTravelMethod.Items.AddRange(new[] { "Driving", "Flight" });
                    comboTravelMethod.SelectedIndex = 0; // Driving default
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error populating tab dropdowns: {ex.Message}");
            }
        }

        private T FindControlInTab<T>(TabPage tabPage, string controlName) where T : Control
        {
            return FindControlByName<T>(tabPage, controlName);
        }

        private T FindControlByName<T>(Control parent, string name) where T : Control
        {
            foreach (Control control in parent.Controls)
            {
                if (control.Name == name && control is T)
                    return (T)control;

                var found = FindControlByName<T>(control, name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private void LoadResourceData(CommissioningResource resource, TabPage tabPage)
        {
            // Load resource data into the tab's controls
            if (resource == null) return;

            // Update rates display when rate sheet changes
            UpdateRateDisplay(tabPage);
        }

        private void BtnAddResource_Click(object sender, EventArgs e)
        {
            AddNewResource();
        }

        private void AddNewResource()
        {
            // Create new resource
            var newResource = new CommissioningResource
            {
                ResourceID = Guid.NewGuid().ToString(),
                TechnicianName = $"Resource {currentProject.Resources.Count + 1}"
            };

            // Copy settings from first resource if it exists
            if (currentProject.Resources.Count > 0)
            {
                CopyResourceSettings(currentProject.Resources[0], newResource);
            }

            currentProject.Resources.Add(newResource);
            CreateResourceTab(newResource);

            // Select the new tab
            tabResources.SelectedIndex = tabResources.TabPages.Count - 1;
        }

        private void CopyResourceSettings(CommissioningResource source, CommissioningResource target)
        {
            // Copy common settings from first resource to new resource
            target.RegularLabourRate = source.RegularLabourRate;
            target.OvertimeLabourRate = source.OvertimeLabourRate;
            target.PremiumLabourRate = source.PremiumLabourRate;
            target.RegularTravelRate = source.RegularTravelRate;
            target.OvertimeTravelRate = source.OvertimeTravelRate;
            target.PremiumTravelRate = source.PremiumTravelRate;
            target.TravelMethod = source.TravelMethod;
            target.MileageRate = source.MileageRate;
            target.PerDiemRate = source.PerDiemRate;
            // etc.
        }

        private void BtnDeleteResource_Click(object sender, EventArgs e)
        {
            if (tabResources.TabPages.Count > 1) // Keep at least one resource
            {
                var result = MessageBox.Show(
                    "Are you sure you want to delete this resource?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    var selectedTab = tabResources.SelectedTab;
                    var resource = selectedTab.Tag as CommissioningResource;

                    currentProject.Resources.Remove(resource);
                    tabResources.TabPages.Remove(selectedTab);

                    // Renumber tabs
                    for (int i = 0; i < tabResources.TabPages.Count; i++)
                    {
                        tabResources.TabPages[i].Text = $"Resource {i + 1}";
                    }
                }
            }
            else
            {
                MessageBox.Show("At least one resource is required.", "Cannot Delete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UpdateResourceFromUI(CommissioningResource resource)
        {
            if (resource == null) return;

            // Get the current tab page
            TabPage currentTab = null;
            foreach (TabPage tab in tabResources.TabPages)
            {
                if (tab.Tag == resource)
                {
                    currentTab = tab;
                    break;
                }
            }

            if (currentTab == null) return;

            try
            {
                // Get values from UI controls

                // Rates
                var comboRateSheet = FindControlInTab<ComboBox>(currentTab, "comboBoxRateSheet");
                var txtRegularLabour = FindControlInTab<TextBox>(currentTab, "txtRegularLabour");
                var txtOvertimeLabour = FindControlInTab<TextBox>(currentTab, "txtOvertimeLabour");
                var txtPremiumLabour = FindControlInTab<TextBox>(currentTab, "txtPremiumLabour");
                var txtRegularTravel = FindControlInTab<TextBox>(currentTab, "txtRegularTravel");
                var txtOvertimeTravel = FindControlInTab<TextBox>(currentTab, "txtOvertimeTravel");
                var txtPremiumTravel = FindControlInTab<TextBox>(currentTab, "txtPremiumTravel");

                // Update resource rates
                if (txtRegularLabour != null) decimal.TryParse(txtRegularLabour.Text, out resource.RegularLabourRate);
                if (txtOvertimeLabour != null) decimal.TryParse(txtOvertimeLabour.Text, out resource.OvertimeLabourRate);
                if (txtPremiumLabour != null) decimal.TryParse(txtPremiumLabour.Text, out resource.PremiumLabourRate);
                if (txtRegularTravel != null) decimal.TryParse(txtRegularTravel.Text, out resource.RegularTravelRate);
                if (txtOvertimeTravel != null) decimal.TryParse(txtOvertimeTravel.Text, out resource.OvertimeTravelRate);
                if (txtPremiumTravel != null) decimal.TryParse(txtPremiumTravel.Text, out resource.PremiumTravelRate);

                // Days configuration
                var numDaysOnSite = FindControlInTab<NumericUpDown>(currentTab, "numDaysOnSite");
                var numHoursPerDay = FindControlInTab<NumericUpDown>(currentTab, "numHoursPerDay");
                var comboStartDay = FindControlInTab<ComboBox>(currentTab, "comboBoxStartDay");
                var comboStartTime = FindControlInTab<ComboBox>(currentTab, "comboBoxStartTime");
                var comboLunchDuration = FindControlInTab<ComboBox>(currentTab, "comboBoxLunchDuration");

                if (numDaysOnSite != null) resource.DaysOnSite = (int)numDaysOnSite.Value;
                if (numHoursPerDay != null) resource.HoursPerDay = numHoursPerDay.Value;
                if (comboStartTime != null) resource.DefaultStartTime = comboStartTime.SelectedItem?.ToString() ?? "07:00";

                // Parse lunch duration
                if (comboLunchDuration != null)
                {
                    string lunchDurationText = comboLunchDuration.SelectedItem?.ToString() ?? "0.5 hours";
                    if (lunchDurationText.Contains("0.0")) resource.LunchDuration = 0m;
                    else if (lunchDurationText.Contains("1.0")) resource.LunchDuration = 1m;
                    else resource.LunchDuration = 0.5m;
                }

                // Calculate start date based on selected day of week
                if (comboStartDay != null)
                {
                    string startDayText = comboStartDay.SelectedItem?.ToString() ?? "Monday";
                    resource.StartDate = GetNextOccurrence(DateTime.Today, startDayText);
                }

                // Travel options
                var chkSeparateTravelTo = FindControlInTab<CheckBox>(currentTab, "chkSeparateTravelTo");
                var chkSeparateTravelFrom = FindControlInTab<CheckBox>(currentTab, "chkSeparateTravelFrom");
                var comboTravelMethod = FindControlInTab<ComboBox>(currentTab, "comboBoxTravelMethod");
                var numMileageRate = FindControlInTab<NumericUpDown>(currentTab, "numMileageRate");
                var numPerDiem = FindControlInTab<NumericUpDown>(currentTab, "numPerDiem");

                if (chkSeparateTravelTo != null) resource.SeparateTravelTo = chkSeparateTravelTo.Checked;
                if (chkSeparateTravelFrom != null) resource.SeparateTravelFrom = chkSeparateTravelFrom.Checked;
                if (comboTravelMethod != null) resource.TravelMethod = comboTravelMethod.SelectedItem?.ToString() ?? "Driving";
                if (numMileageRate != null) resource.MileageRate = numMileageRate.Value;
                if (numPerDiem != null) resource.PerDiemRate = numPerDiem.Value;

                // Mark resource as dirty to trigger recalculation
                resource.IsDirty = true;

                // Regenerate resource schedule if needed
                if (resource.DailyData == null || resource.DailyData.Count == 0)
                {
                    resource.InitializeFromSchedule();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating resource from UI: {ex.Message}");
            }
        }

        private void SetupAutoSave()
        {
            autoSaveTimer = new Timer();
            autoSaveTimer.Interval = 3000; // 3 seconds
            autoSaveTimer.Tick += (s, e) => SaveProject();
            autoSaveTimer.Start();
        }

        private void SaveProject()
        {
            try
            {
                // Apply any pending changes
                if (tabResources.SelectedTab != null)
                {
                    var resource = tabResources.SelectedTab.Tag as CommissioningResource;
                    if (resource != null)
                    {
                        UpdateResourceFromUI(resource);
                    }
                }

                // Save the project
                CommissioningDataManager.SaveProject(currentProject);
            }
            catch (Exception ex)
            {
                // Log error but don't show to user repeatedly
                System.Diagnostics.Debug.WriteLine($"Auto-save error: {ex.Message}");
            }
        }

        private void ViewResultsButton_Click(object sender, EventArgs e)
        {
            // Apply any pending changes
            ApplyPendingChanges();

            // Check if project exists
            if (currentProject == null)
            {
                MessageBox.Show("No project loaded.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Create and show the results window if not already open
            if (_resultsWindow == null || _resultsWindow.IsDisposed)
            {
                _resultsWindow = new CommissioningResultsWindow(currentProject);
                _resultsWindow.Show();
            }
            else
            {
                // If results window already exists, bring it to front
                _resultsWindow.BringToFront();
            }
        }

        private void ApplyPendingChanges()
        {
            // Save the current resource being edited
            if (tabResources.SelectedTab != null)
            {
                var resource = tabResources.SelectedTab.Tag as CommissioningResource;
                if (resource != null)
                {
                    UpdateResourceFromUI(resource);
                }
            }

            // Make sure all resources have initialized daily data
            foreach (var resource in currentProject.Resources)
            {
                if (resource.DailyData == null || resource.DailyData.Count == 0)
                {
                    resource.InitializeFromSchedule();
                }
            }

            // Recalculate project totals
            currentProject.CalculateTotals();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Save project when closing
            SaveProject();

            // Close results window if open
            if (_resultsWindow != null && !_resultsWindow.IsDisposed)
            {
                _resultsWindow.Close();
            }

            autoSaveTimer?.Stop();
            base.OnFormClosing(e);
        }
    }
}