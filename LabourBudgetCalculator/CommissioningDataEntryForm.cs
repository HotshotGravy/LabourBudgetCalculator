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
        private ComboBox comboBoxRateSheet;
        private NumericUpDown numDiscount;
        private CheckBox chkEmergency;
        private Timer autoSaveTimer;

        // Current resource controls
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
            comboBoxRateSheet = new ComboBox
            {
                Name = "comboBoxRateSheet",
                Location = new Point(120, 20),
                Size = new Size(180, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            comboBoxRateSheet.SelectedIndexChanged += (s, e) => UpdateResourceFromUI(resource);

            // Discount field
            Label lblDiscount = new Label { Text = "Discount", Location = new Point(20, 50), AutoSize = true };
            numDiscount = new NumericUpDown
            {
                Name = "numDiscount",
                Location = new Point(120, 48),
                Size = new Size(60, 25),
                Maximum = 100,
                Minimum = 0,
                Value = 0
            };
            numDiscount.ValueChanged += (s, e) => UpdateResourceFromUI(resource);
            Label lblPercentage = new Label { Text = "%", Location = new Point(181, 50), AutoSize = true };

            // Emergency checkbox
            chkEmergency = new CheckBox
            {
                Name = "chkEmergency",
                Text = "Emergency",
                Location = new Point(220, 50),
                AutoSize = true
            };
            chkEmergency.CheckedChanged += (s, e) => UpdateResourceFromUI(resource);

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
            numDaysOnSite.ValueChanged += (s, e) => UpdateResourceFromUI(resource);

            // Hours per Day
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
            numHoursPerDay.ValueChanged += (s, e) => UpdateResourceFromUI(resource);

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
            comboBoxStartDay.SelectedIndexChanged += (s, e) => UpdateResourceFromUI(resource);

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
            comboBoxStartTime.SelectedIndexChanged += (s, e) => UpdateResourceFromUI(resource);

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
            comboBoxLunchDuration.SelectedIndexChanged += (s, e) => UpdateResourceFromUI(resource);

            groupBoxDays.Controls.AddRange(new Control[] {
                lblDaysOnSite, numDaysOnSite,
                lblHoursPerDay, numHoursPerDay,
                lblStartDay, comboBoxStartDay,
                lblDefaultStartTime, comboBoxStartTime,
                lblLunchDuration, comboBoxLunchDuration
            });
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

            // Similar travel controls as MainForm but bound to resource
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

            // Add all other travel controls similar to MainForm...
            groupBoxTravel.Controls.AddRange(new Control[] {
                lblSeparateTravel, chkSeparateTravelTo, chkSeparateTravelFrom,
                lblTravelMethod, comboBoxTravelMethod
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

            // Similar expense controls as MainForm but bound to resource
            // Flight Cost, Hotel, Rental Car, etc.
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

            // Create schedule grid or day panels here
            // This will show the dates, start/end times, and calculated hours
        }

        private void LoadData()
        {
            // Load rate sheets
            rateSheets = DataManager.LoadRateSheets();

            // Initialize combo boxes
            LoadDayOfWeekOptions();
            LoadTimeOptions();
            LoadLunchOptions();
            LoadTravelMethods();
        }

        private void LoadDayOfWeekOptions()
        {
            string[] daysOfWeek = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
            // Add to all start day combo boxes in all tabs
        }

        private void LoadTimeOptions()
        {
            // Create time options in 30-minute increments
            var times = new List<string>();
            for (int hour = 0; hour < 24; hour++)
            {
                times.Add($"{hour:D2}:00");
                times.Add($"{hour:D2}:30");
            }
            // Add to all time combo boxes
        }

        private void LoadLunchOptions()
        {
            var lunchOptions = new[] { "0.0 hours", "0.5 hours", "1.0 hours" };
            // Add to all lunch duration combo boxes
        }

        private void LoadTravelMethods()
        {
            var travelMethods = new[] { "Driving", "Flight" };
            // Add to all travel method combo boxes
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
            // Update resource object from UI controls
            // This will be called whenever a control value changes
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
                CommissioningDataManager.SaveProject(currentProject);
            }
            catch (Exception ex)
            {
                // Log error but don't show to user repeatedly
                System.Diagnostics.Debug.WriteLine($"Auto-save error: {ex.Message}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Save project when closing
            SaveProject();
            autoSaveTimer?.Stop();
            base.OnFormClosing(e);
        }
    }
}