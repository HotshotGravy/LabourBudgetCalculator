using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Xml;
using System.IO;
using System.Windows.Forms;

using LabourBudgetCalculator.Models;
using LabourBudgetCalculator.Helpers;

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
        private Button btnViewResults;
        private CommissioningResultsWindow _resultsWindow;
        private Timer scheduleUpdateTimer;
        private CommissioningResource pendingUpdateResource;
        private TabPage pendingUpdateTabPage;
        private Timer redrawDelayTimer;

        // Add these properties near the top of the CommissioningDataEntryForm class
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

        private Button btnBackToProjects;
        private Button btnReset;

        public CommissioningDataEntryForm(CommissioningProject project)
        {
            InitializeComponent();
            currentProject = project ?? throw new ArgumentNullException(nameof(project));
            if (currentProject.Resources == null) currentProject.Resources = new List<CommissioningResource>();

            CommissioningDataManager.Instance.SetCurrentProject(this.currentProject);

            SetupForm();
            LoadData();
            SetupControls();
            // Load saved dark mode preference and apply theme AFTER controls are created
            LoadDarkModePreference();
            SetupAutoSave();
            SetupScheduleUpdateTimer();
        }

        private void EnableDoubleBuffering()
        {
            // Enable double buffering for the entire form
            this.DoubleBuffered = true;

            // Enable double buffering for the tab control
            if (tabResources != null)
            {
                typeof(TabControl).GetProperty("DoubleBuffered",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)
                    .SetValue(tabResources, true, null);
            }
        }

        private void UpdateDayPanelColors(Panel dayPanel, ResourceDayData dayData, CommissioningResource resource)
        {
            if (dayPanel == null || dayData == null || resource == null) return;

            try
            {
                // Determine day type based on the data and resource configuration
                DayType dayType = DetermineDayType(dayData, resource);

                // Apply color based on day type and current theme
                Color panelColor = GetColorForDayType(dayType);
                dayPanel.BackColor = panelColor;

                // Update all labels in the panel to have transparent backgrounds and proper text color
                foreach (Control control in dayPanel.Controls)
                {
                    if (control is Label label)
                    {
                        label.BackColor = Color.Transparent;
                        label.ForeColor = isDarkMode ? Color.White : SystemColors.ControlText;

                        // Special handling for weekend days
                        if (IsWeekendDay(dayData.Date))
                        {
                            label.ForeColor = isDarkMode ? Color.Yellow : Color.Gray;
                            if (label.Name.Contains("dayOfWeek"))
                            {
                                label.Font = new Font(label.Font, FontStyle.Bold);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating day panel colors: {ex.Message}");
            }
        }

        private DayType DetermineDayType(ResourceDayData dayData, CommissioningResource resource)
        {
            // Check for manual override first
            if (dayData.ManualDayType.HasValue)
            {
                return dayData.ManualDayType.Value;
            }

            // Check if it's a travel day (separate travel to/from site)
            if (IsTravelDay(dayData, resource))
            {
                return DayType.Travel;
            }

            // Check if there are any hours planned for this day
            decimal totalHours = dayData.GetPlannedLabourHoursTotal() + dayData.GetPlannedTravelHoursTotal();
            if (totalHours <= 0)
            {
                return DayType.Nil;
            }

            // Default to work day
            return DayType.Work;
        }

        private bool IsTravelDay(ResourceDayData dayData, CommissioningResource resource)
        {
            // Get the resource's daily data ordered by date
            var orderedDays = resource.DailyData.Values.OrderBy(d => d.Date).ToList();
            if (!orderedDays.Any()) return false;

            // Check if this is the first day and separate travel TO is enabled
            if (resource.SeparateTravelTo && dayData.Date.Date == orderedDays.First().Date.Date)
            {
                // First day with separate travel TO - should be travel day
                return dayData.GetPlannedTravelHoursTotal() > 0 && dayData.GetPlannedLabourHoursTotal() == 0;
            }

            // Check if this is the last day and separate travel FROM is enabled
            if (resource.SeparateTravelFrom && dayData.Date.Date == orderedDays.Last().Date.Date)
            {
                // Last day with separate travel FROM - should be travel day
                return dayData.GetPlannedTravelHoursTotal() > 0 && dayData.GetPlannedLabourHoursTotal() == 0;
            }

            return false;
        }

        private bool IsHoldoverDay(ResourceDayData dayData, CommissioningResource resource)
        {
            // Holdover days are typically weekend days with 8 hours at regular rate
            DayOfWeek dayOfWeek = dayData.Date.DayOfWeek;
            bool isWeekend = (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday);

            if (isWeekend)
            {
                // Check if it has exactly 8 hours of labour with no overtime/premium
                return dayData.PlannedRegularLabourHours == 8 &&
                       dayData.PlannedOvertimeLabourHours == 0 &&
                       dayData.PlannedPremiumLabourHours == 0;
            }

            return false;
        }

        private bool IsWeekendDay(DateTime date)
        {
            DayOfWeek dayOfWeek = date.DayOfWeek;
            return dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday;
        }

        private Color GetColorForDayType(DayType dayType)
        {
            switch (dayType)
            {
                case DayType.Travel:
                    return isDarkMode ? darkModeTravelDay : lightTravelDay;
                case DayType.Work:
                    return isDarkMode ? darkModeWorkDay : lightWorkDay;
                case DayType.Holdover:
                    return isDarkMode ? darkModeHoldoverDay : lightHoldoverDay;
                case DayType.Nil:
                default:
                    return isDarkMode ? Color.FromArgb(60, 60, 60) : Color.LightGray;
            }
        }

        private void UpdateAllDayPanelsInSchedule(Panel schedulePanel, CommissioningResource resource)
        {
            if (schedulePanel == null || resource?.DailyData == null) return;

            // Find all day panels in the schedule
            foreach (Control control in schedulePanel.Controls)
            {
                if (control is FlowLayoutPanel weekPanel)
                {
                    foreach (Control dayControl in weekPanel.Controls)
                    {
                        if (dayControl is Panel dayPanel && dayPanel.Name.StartsWith("dayPanel_"))
                        {
                            // Extract date from panel name
                            string datePart = dayPanel.Name.Replace("dayPanel_", "");
                            if (DateTime.TryParseExact(datePart, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime panelDate))
                            {
                                // Find matching day data
                                ResourceDayData dayData = resource.DailyData.Values.FirstOrDefault(d => d.Date.Date == panelDate.Date);
                                if (dayData != null)
                                {
                                    // Update colors for this day panel
                                    UpdateDayPanelColors(dayPanel, dayData, resource);
                                }
                            }
                        }
                    }
                }
            }
        }
        private void SetupForm()
        {
            this.Text = $"Commissioning Data Entry - {currentProject.ProjectName}";
            this.Size = new Size(1450, 950);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            this.AutoScroll = true;
        }

        private void LoadData()
        {
            try
            {
                rateSheets = DataManager.LoadRateSheets();
                if (rateSheets == null || !rateSheets.Any())
                {
                    MessageBox.Show("No rate sheets found. A default rate sheet will be used.", "Rate Sheet Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    rateSheets = new List<RateSheet> { new RateSheet("Default (Fallback)") { RegularLabourRate = 100 } };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading rate sheets: {ex.Message}", "Load Data Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                rateSheets = new List<RateSheet> { new RateSheet("Error Fallback") };
            }
        }

        private void SetupControls()
        {
            tabResources = new TabControl { Location = new Point(12, 12), Size = new Size(this.ClientSize.Width - 24, this.ClientSize.Height - 80), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            this.Controls.Add(tabResources);
            btnAddResource = new Button { Text = "Add Resource", Size = new Size(120, 30), Location = new Point(12, this.ClientSize.Height - 55), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            btnAddResource.Click += BtnAddResource_Click; this.Controls.Add(btnAddResource);
            btnDeleteResource = new Button { Text = "Delete Resource", Size = new Size(120, 30), Location = new Point(btnAddResource.Right + 6, btnAddResource.Top), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            btnDeleteResource.Click += BtnDeleteResource_Click; this.Controls.Add(btnDeleteResource);
            btnViewResults = new Button { Text = "View Results", Size = new Size(120, 30), Location = new Point(btnDeleteResource.Right + 6, btnDeleteResource.Top), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            btnViewResults.Click += BtnViewResults_Click; this.Controls.Add(btnViewResults);

            btnBackToProjects = new Button
            {
                Name = "btnBackToProjects",
                Text = "Back to Projects",
                Size = new Size(120, 30),
                Location = new Point(btnViewResults.Right + 6, btnViewResults.Top),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnBackToProjects.Click += BtnBackToProjects_Click;
            this.Controls.Add(btnBackToProjects);

            btnReset = new Button
            {
                Name = "btnReset",
                Text = "Reset",
                Size = new Size(120, 30),
                Location = new Point(btnBackToProjects.Right + 6, btnBackToProjects.Top),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnReset.Click += (sender, e) => ResetForm();
            this.Controls.Add(btnReset);

            btnDarkMode = new Button
            {
                Name = "btnDarkMode",
                Text = "Toggle Dark Mode",
                Size = new Size(120, 30),
                Location = new Point(btnReset.Right + 6, btnReset.Top),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnDarkMode.Click += (sender, e) => ToggleDarkMode();
            this.Controls.Add(btnDarkMode);

            // Store light theme colors for later use
            lightBackColor = this.BackColor;
            lightTextColor = this.ForeColor;
            lightControlBackColor = SystemColors.Control;
            lightPanelBackColor = SystemColors.Control;
            lightGridBackColor = SystemColors.Window;

            // REMOVED: Load saved dark mode preference (now done in constructor)
            // LoadDarkModePreference();

            this.Resize += (s, a) => {
                btnAddResource.Top = this.ClientSize.Height - 55;
                btnDeleteResource.Top = btnAddResource.Top;
                btnViewResults.Top = btnAddResource.Top;
                btnBackToProjects.Top = btnAddResource.Top; // Add this line
            };

            if (currentProject.Resources != null && currentProject.Resources.Any())
            {
                foreach (var resource in currentProject.Resources)
                {
                    PopulateResourceRatesFromSheetName(resource);
                    if (resource.DailyData == null || !resource.DailyData.Any() || resource.IsDirty) resource.InitializeFromSchedule();
                    CreateResourceTab(resource);
                }
            }
            else { AddNewResource(); }
            if (tabResources.TabPages.Count > 0) tabResources.SelectedIndex = 0;
        }



        private void BtnBackToProjects_Click(object sender, EventArgs e)
        {
            // Check if there are unsaved changes
            bool hasUnsavedChanges = currentProject.IsDirty ||
                                    (currentProject.Resources != null && currentProject.Resources.Any(r => r.IsDirty));

            if (hasUnsavedChanges)
            {
                DialogResult result = MessageBox.Show(
                    "You have unsaved changes. Do you want to save before going back to project selection?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Cancel)
                {
                    return; // User cancelled, stay on current form
                }
                else if (result == DialogResult.Yes)
                {
                    // Save before leaving
                    SaveProjectData(true);
                }
                // If No, proceed without saving
            }

            try
            {
                // Stop timers before closing
                autoSaveTimer?.Stop();
                scheduleUpdateTimer?.Stop();

                // Close results window if open
                _resultsWindow?.Close();

                // Hide current form
                this.Hide();

                // Show the project selection form
                CommissioningProjectSelectionForm projectSelectionForm = new CommissioningProjectSelectionForm();
                projectSelectionForm.ShowDialog();

                // Close current form after project selection form closes
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error returning to project selection: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ToggleDarkMode()
        {
            isDarkMode = !isDarkMode;
            ApplyTheme();
            SaveDarkModePreference();

            // Update results window if open
            if (_resultsWindow is CommissioningResultsWindow resultsWindow)
            {
               // resultsWindow.SetDarkMode(isDarkMode);
            }
        }

        private void ApplyTheme()
        {
            if (isDarkMode)
            {
                // Apply dark theme
                this.BackColor = darkBackColor;
                this.ForeColor = darkTextColor;

                // Update button appearance to indicate it's in dark mode
                if (btnDarkMode != null)
                {
                    btnDarkMode.BackColor = darkButtonBackColor;
                    btnDarkMode.ForeColor = darkButtonForeColor;
                }

                // Apply dark theme to all group boxes
                foreach (Control control in this.Controls)
                {
                    if (control is GroupBox)
                    {
                        ApplyDarkThemeToControl(control);
                    }
                }

                // Apply dark theme to tab control and all tab pages
                if (tabResources != null)
                {
                    ApplyDarkThemeToControl(tabResources);
                    foreach (TabPage tabPage in tabResources.TabPages)
                    {
                        ApplyDarkThemeToControl(tabPage);
                    }
                }

                // Update day panels with dark theme colors
                UpdateDayPanelsTheme();
            }
            else
            {
                // Restore light theme
                this.BackColor = lightBackColor;
                this.ForeColor = lightTextColor;

                // Update button appearance to indicate it's in light mode
                if (btnDarkMode != null)
                {
                    btnDarkMode.BackColor = SystemColors.Control;
                    btnDarkMode.ForeColor = SystemColors.ControlText;
                }

                // Restore light theme to all group boxes
                foreach (Control control in this.Controls)
                {
                    if (control is GroupBox)
                    {
                        ApplyLightThemeToControl(control);
                    }
                }

                // Restore light theme to tab control and all tab pages
                if (tabResources != null)
                {
                    ApplyLightThemeToControl(tabResources);
                    foreach (TabPage tabPage in tabResources.TabPages)
                    {
                        ApplyLightThemeToControl(tabPage);
                    }
                }

                // Update day panels with light theme colors
                UpdateDayPanelsTheme();
            }
        }

        // Add recursive methods to apply themes to controls
        private void ApplyDarkThemeToControl(Control control)
        {
            control.BackColor = darkControlBackColor;
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
            else if (control is DateTimePicker)
            {
                DateTimePicker dateTimePicker = (DateTimePicker)control;
                dateTimePicker.BackColor = darkPanelBackColor;
                dateTimePicker.ForeColor = Color.White;
                dateTimePicker.CalendarForeColor = Color.White;
                dateTimePicker.CalendarMonthBackground = darkGridBackColor;
            }
            else if (control is TabControl)
            {
                control.BackColor = darkControlBackColor;
                control.ForeColor = Color.White;
            }
            else if (control is TabPage)
            {
                control.BackColor = darkControlBackColor;
                control.ForeColor = Color.White;
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
            else if (control is DateTimePicker)
            {
                DateTimePicker dateTimePicker = (DateTimePicker)control;
                dateTimePicker.BackColor = SystemColors.Window;
                dateTimePicker.ForeColor = SystemColors.WindowText;
                dateTimePicker.CalendarForeColor = SystemColors.WindowText;
                dateTimePicker.CalendarMonthBackground = SystemColors.Window;
            }
            else if (control is TabControl)
            {
                control.BackColor = SystemColors.Control;
                control.ForeColor = SystemColors.ControlText;
            }
            else if (control is TabPage)
            {
                control.BackColor = SystemColors.Control;
                control.ForeColor = SystemColors.ControlText;
            }

            // Recursively apply to child controls
            foreach (Control child in control.Controls)
            {
                ApplyLightThemeToControl(child);
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
                    "CommissioningSettings.xml");

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
                    "CommissioningSettings.xml");

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

        private void SetupScheduleUpdateTimer()
        {
            if (scheduleUpdateTimer != null)
            {
                scheduleUpdateTimer.Stop();
                scheduleUpdateTimer.Dispose();
            }

            scheduleUpdateTimer = new Timer();
            scheduleUpdateTimer.Interval = 1000; // Increased from 500ms to 1000ms for better performance
            scheduleUpdateTimer.Tick += ScheduleUpdateTimer_Tick;
        }

        private void ScheduleUpdateTimer_Tick(object sender, EventArgs e)
        {
            scheduleUpdateTimer.Stop();

            if (pendingUpdateResource != null && pendingUpdateTabPage != null)
            {
                // Only regenerate if the resource structure has changed
                if (pendingUpdateResource.IsDirty)
                {
                    pendingUpdateResource.InitializeFromSchedule();
                    RegenerateSchedule(pendingUpdateTabPage, pendingUpdateResource);
                }

                // Clear pending updates
                pendingUpdateResource = null;
                pendingUpdateTabPage = null;
            }
        }

        private void QueueScheduleUpdate(TabPage tabPage, CommissioningResource resource)
        {
            // Only queue updates for structural changes, not simple value changes
            if (resource == null || tabPage == null) return;

            // Initialize timer if it's null
            if (scheduleUpdateTimer == null)
            {
                SetupScheduleUpdateTimer();
            }

            // Stop any existing timer
            scheduleUpdateTimer.Stop();

            // Store the pending update
            pendingUpdateTabPage = tabPage;
            pendingUpdateResource = resource;

            // Start the timer
            scheduleUpdateTimer.Start();
        }

        private void CreateResourceTab(CommissioningResource resource)
        {
            TabPage tabPage = new TabPage(string.IsNullOrWhiteSpace(resource.TechnicianName) ? "New Resource" : resource.TechnicianName)
            {
                AutoScroll = true
            };

            // Set up the Tag structure immediately with the dictionary
            var tagData = new Dictionary<string, object>();
            tagData["resource"] = resource;
            tabPage.Tag = tagData;

            CreateResourceFormContent(tabPage, resource);
            PopulateTabDropdowns(tabPage, resource);
            LoadResourceDataIntoControls(resource, tabPage);

            // Add the reset button to the tab
            AddResetButton(tabPage, resource);

            // Add the travel time/distance linking
            SetupTravelTimeDistanceLink(tabPage, resource);

            tabResources.TabPages.Add(tabPage);
        }

        private void CreateResourceFormContent(TabPage tabPage, CommissioningResource resource)
        {
            CreateRatesSection(tabPage, resource);
            CreateDaysSection(tabPage, resource);
            CreateResourceInfoSection(tabPage, resource);
            CreateTravelSection(tabPage, resource);
            CreateExpensesSection(tabPage, resource);
            CreateScheduleSection(tabPage, resource);
        }

        private void PopulateResourceRatesFromSheetName(CommissioningResource resource)
        {
            if (resource == null || string.IsNullOrEmpty(resource.RateSheetName) || rateSheets == null || !rateSheets.Any()) return;
            RateSheet selectedSheet = rateSheets.FirstOrDefault(rs => rs.Name == resource.RateSheetName);
            if (selectedSheet != null)
            {
                resource.RegularLabourRate = selectedSheet.RegularLabourRate; resource.OvertimeLabourRate = selectedSheet.OvertimeLabourRate; resource.PremiumLabourRate = selectedSheet.PremiumLabourRate;
                resource.RegularTravelRate = selectedSheet.RegularTravelRate; resource.OvertimeTravelRate = selectedSheet.OvertimeTravelRate; resource.PremiumTravelRate = selectedSheet.PremiumTravelRate;

                // Only update from sheet if current resource value is default (0) or matches previous sheet's default
                // This allows user overrides in the expense section to persist if the rate sheet changes.
                if (resource.HotelRate == 0 || (resource.HotelRate != 0 && resource.HotelRate == GetOldSheetHotelCost(resource.RateSheetName))) resource.HotelRate = selectedSheet.HotelCost;
                if (resource.PerDiemRate == 0 || (resource.PerDiemRate != 0 && resource.PerDiemRate == GetOldSheetPerDiem(resource.RateSheetName))) resource.PerDiemRate = selectedSheet.PerDiemRate;
                if (resource.MileageRate == 0 || (resource.MileageRate != 0 && resource.MileageRate == GetOldSheetMileageRate(resource.RateSheetName))) resource.MileageRate = selectedSheet.MileageRate;
                if (resource.RentalCarRate == 0 || (resource.RentalCarRate != 0 && resource.RentalCarRate == GetOldSheetRentalCarRate(resource.RateSheetName))) resource.RentalCarRate = selectedSheet.RentalCarRate;
                if (resource.FlightCost == 0 || (resource.FlightCost != 0 && resource.FlightCost == GetOldSheetFlightCost(resource.RateSheetName))) resource.FlightCost = selectedSheet.FlightCost;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Warning: Rate sheet '{resource.RateSheetName}' not found for resource '{resource.TechnicianName}'. Rates not populated from sheet.");
            }
        }
        // Helper methods to get old sheet defaults (stubs, implement if needed for complex override logic)
        private decimal GetOldSheetHotelCost(string oldSheetName) { return rateSheets.FirstOrDefault(rs => rs.Name == oldSheetName)?.HotelCost ?? 0; }
        private decimal GetOldSheetPerDiem(string oldSheetName) { return rateSheets.FirstOrDefault(rs => rs.Name == oldSheetName)?.PerDiemRate ?? 0; }
        private decimal GetOldSheetMileageRate(string oldSheetName) { return rateSheets.FirstOrDefault(rs => rs.Name == oldSheetName)?.MileageRate ?? 0; }
        private decimal GetOldSheetRentalCarRate(string oldSheetName) { return rateSheets.FirstOrDefault(rs => rs.Name == oldSheetName)?.RentalCarRate ?? 0; }
        private decimal GetOldSheetFlightCost(string oldSheetName) { return rateSheets.FirstOrDefault(rs => rs.Name == oldSheetName)?.FlightCost ?? 0; }


        private void UpdateRatesOnResourceAndDisplay(TabPage tabPage, CommissioningResource resource)
        {
            if (resource == null || tabPage == null || rateSheets == null || !rateSheets.Any()) return;
            var comboRateSheet = FindControlInTab<ComboBox>(tabPage, "comboBoxRateSheet");
            var numDiscount = FindControlInTab<NumericUpDown>(tabPage, "numDiscount");
            var chkEmergency = FindControlInTab<CheckBox>(tabPage, "chkEmergency");

            string oldRateSheetName = resource.RateSheetName; // Store old name for expense override logic

            if (comboRateSheet.SelectedIndex >= 0 && comboRateSheet.SelectedIndex < rateSheets.Count)
            {
                RateSheet selectedSheet = rateSheets[comboRateSheet.SelectedIndex];
                resource.RateSheetName = selectedSheet.Name;
                PopulateResourceRatesFromSheetName(resource); // This copies all rates and default expenses
            }
            resource.DiscountPercent = numDiscount?.Value ?? 0;
            resource.IsEmergency = chkEmergency?.Checked ?? false;
            UpdateRateDisplayTextBoxes(tabPage, resource);
            resource.IsDirty = true; currentProject.IsDirty = true;
        }

        private void UpdateRateDisplayTextBoxes(TabPage tabPage, CommissioningResource resource)
        {
            if (resource == null) return;

            decimal discountFactor = 1 - (resource.DiscountPercent / 100m);

            decimal regLab = resource.RegularLabourRate * discountFactor;
            decimal otLab = resource.OvertimeLabourRate * discountFactor;
            decimal premLab = resource.PremiumLabourRate * discountFactor;

            decimal regTrav = resource.RegularTravelRate * discountFactor;
            decimal otTrav = resource.OvertimeTravelRate * discountFactor;
            decimal premTrav = resource.PremiumTravelRate * discountFactor;

            if (resource.IsEmergency)
            {
                regLab = premLab;
                otLab = premLab;
                regTrav = premTrav;
                otTrav = premTrav;
            }

            FindControlInTab<TextBox>(tabPage, "txtRegularLabour")?.SetValue(t => t.Text = regLab.ToString("F2"));
            FindControlInTab<TextBox>(tabPage, "txtOvertimeLabour")?.SetValue(t => t.Text = otLab.ToString("F2"));
            FindControlInTab<TextBox>(tabPage, "txtPremiumLabour")?.SetValue(t => t.Text = premLab.ToString("F2"));

            FindControlInTab<TextBox>(tabPage, "txtRegularTravel")?.SetValue(t => t.Text = regTrav.ToString("F2"));
            FindControlInTab<TextBox>(tabPage, "txtOvertimeTravel")?.SetValue(t => t.Text = otTrav.ToString("F2"));
            FindControlInTab<TextBox>(tabPage, "txtPremiumTravel")?.SetValue(t => t.Text = premTrav.ToString("F2"));
        }

        private void CreateRatesSection(TabPage tabPage, CommissioningResource resource)
        {
            var g = new GroupBox
            {
                Name = "groupBoxRates",
                Text = "Rates & Discounts",
                Location = new Point(10, 10),
                Size = new Size(420, 180)
            };
            tabPage.Controls.Add(g);

            var cb = new ComboBox
            {
                Name = "comboBoxRateSheet",
                Location = new Point(120, 22),
                Size = new Size(200, 21),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cb.SelectedIndexChanged += (s, e) =>
            {
                UpdateRatesOnResourceAndDisplay(tabPage, resource);
                UpdateResourceFromUI(resource);
            };

            var nd = new NumericUpDown
            {
                Name = "numDiscount",
                Location = new Point(120, 52),
                Size = new Size(70, 20),
                Maximum = 100,
                Minimum = 0,
                DecimalPlaces = 2,
                Value = resource.DiscountPercent
            };
            nd.ValueChanged += (s, e) =>
            {
                UpdateRatesOnResourceAndDisplay(tabPage, resource);
                UpdateResourceFromUI(resource);
            };

            var ce = new CheckBox
            {
                Name = "chkEmergency",
                Text = "Emergency Rates",
                Location = new Point(240, 52),
                AutoSize = true,
                Checked = resource.IsEmergency
            };
            ce.CheckedChanged += (s, e) =>
            {
                UpdateRatesOnResourceAndDisplay(tabPage, resource);
                UpdateResourceFromUI(resource);
            };

            g.Controls.AddRange(new Control[] {
        new Label { Text = "Rate Sheet:", Location = new Point(15, 25), AutoSize = true },
        cb,
        new Label { Text = "Discount:", Location = new Point(15, 55), AutoSize = true },
        nd,
        new Label { Text = "%", Location = new Point(195, 55), AutoSize = true },
        ce
    });
            CreateRateDisplayLabels(g);
        }

        private void CreateRateDisplayLabels(GroupBox pg)
        {
            int yS = 90, xL1 = 15, xT1 = 120, xU1 = 185;
            int xL2 = 220, xT2 = 305, xU2 = 370;

            TextBox tRL = new TextBox { Name = "txtRegularLabour", Location = new Point(xT1, yS - 3), Size = new Size(60, 20), ReadOnly = true, TabStop = false };
            TextBox tOL = new TextBox { Name = "txtOvertimeLabour", Location = new Point(xT1, yS + 22), Size = new Size(60, 20), ReadOnly = true, TabStop = false };
            TextBox tPL = new TextBox { Name = "txtPremiumLabour", Location = new Point(xT1, yS + 47), Size = new Size(60, 20), ReadOnly = true, TabStop = false };

            TextBox tRT = new TextBox { Name = "txtRegularTravel", Location = new Point(xT2, yS - 3), Size = new Size(60, 20), ReadOnly = true, TabStop = false };
            TextBox tOT = new TextBox { Name = "txtOvertimeTravel", Location = new Point(xT2, yS + 22), Size = new Size(60, 20), ReadOnly = true, TabStop = false };
            TextBox tPT = new TextBox { Name = "txtPremiumTravel", Location = new Point(xT2, yS + 47), Size = new Size(60, 20), ReadOnly = true, TabStop = false };

            pg.Controls.AddRange(new Control[] {
        new Label { Text = "Regular Labour:", Location = new Point(xL1, yS), AutoSize = true }, tRL, new Label { Text = "/hr", Location = new Point(xU1, yS), AutoSize = true },
        new Label { Text = "Overtime Labour:", Location = new Point(xL1, yS + 25), AutoSize = true }, tOL, new Label { Text = "/hr", Location = new Point(xU1, yS + 25), AutoSize = true },
        new Label { Text = "Premium Labour:", Location = new Point(xL1, yS + 50), AutoSize = true }, tPL, new Label { Text = "/hr", Location = new Point(xU1, yS + 50), AutoSize = true },
        new Label { Text = "Regular Travel:", Location = new Point(xL2, yS), AutoSize = true }, tRT, new Label { Text = "/hr", Location = new Point(xU2, yS), AutoSize = true },
        new Label { Text = "Overtime Travel:", Location = new Point(xL2, yS + 25), AutoSize = true }, tOT, new Label { Text = "/hr", Location = new Point(xU2, yS + 25), AutoSize = true },
        new Label { Text = "Premium Travel:", Location = new Point(xL2, yS + 50), AutoSize = true }, tPT, new Label { Text = "/hr", Location = new Point(xU2, yS + 50), AutoSize = true }
    });
        }

        private void CreateDaysSection(TabPage tabPage, CommissioningResource resource)
        {
            var g = new GroupBox
            {
                Name = "groupBoxDays",
                Text = "Schedule Configuration",
                Location = new Point(440, 10),
                Size = new Size(420, 180)
            };
            tabPage.Controls.Add(g);

            NumericUpDown nDS = new NumericUpDown
            {
                Name = "numDaysOnSite",
                Location = new Point(170, 22),
                Size = new Size(60, 20),
                Minimum = 0,
                Maximum = 365,
                Value = resource.DaysOnSite
            };
            nDS.ValueChanged += (s, e) => { UpdateResourceFromUI(resource); resource.InitializeFromSchedule(); RegenerateSchedule(tabPage, resource); };

            NumericUpDown nHPD = new NumericUpDown
            {
                Name = "numHoursPerDay",
                Location = new Point(170, 52),
                Size = new Size(60, 20),
                Minimum = 0,
                Maximum = 24,
                Value = resource.HoursPerDay,
                DecimalPlaces = 1,
                Increment = 0.5m
            };
            nHPD.ValueChanged -= (s, e) => { UpdateResourceFromUI(resource); resource.InitializeFromSchedule(); RegenerateSchedule(tabPage, resource); };
            object nHPD_OriginalValue = nHPD.Value;
            nHPD.Enter += (s, e) => nHPD_OriginalValue = nHPD.Value;
            nHPD.Leave += (s, e) => {
                if (!nHPD.Value.Equals(nHPD_OriginalValue)) {
                    UpdateResourceFromUI(resource);
                    resource.InitializeFromSchedule();
                    RegenerateSchedule(tabPage, resource);
                }
            };
            nHPD.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) {
                    e.SuppressKeyPress = true;
                    this.SelectNextControl(nHPD, true, true, true, true);
                }
            };

            NumericUpDown nLD = new NumericUpDown
            {
                Name = "numLunchDuration",
                Location = new Point(170, 142),
                Size = new Size(60, 20),
                Minimum = 0,
                Maximum = 4,
                Value = resource.LunchDuration,
                DecimalPlaces = 1,
                Increment = 0.5m
            };
            nLD.ValueChanged += (s, e) => { UpdateResourceFromUI(resource); QueueScheduleUpdate(tabPage, resource); };

            // Restore the declarations for dtp and cbST before g.Controls.AddRange in CreateDaysSection
            DateTimePicker dtp = new DateTimePicker
            {
                Name = "dtpResourceStartDate",
                Location = new Point(170, 82),
                Size = new Size(120, 21),
                Value = resource.StartDate,
                Format = DateTimePickerFormat.Short
            };
            dtp.ValueChanged += (s, e) => { UpdateResourceFromUI(resource); resource.InitializeFromSchedule(); RegenerateSchedule(tabPage, resource); };

            ComboBox cbST = new ComboBox
            {
                Name = "comboBoxStartTime",
                Location = new Point(170, 112),
                Size = new Size(100, 21),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbST.SelectedIndexChanged += (s, e) => { UpdateResourceFromUI(resource); QueueScheduleUpdate(tabPage, resource); };

            g.Controls.AddRange(new Control[] {
        new Label { Text = "Work Days on Site:", Location = new Point(15, 25), AutoSize = true }, nDS,
        new Label { Text = "Default Work Hours/Day:", Location = new Point(15, 55), AutoSize = true }, nHPD,
        new Label { Text = "Resource Start Date:", Location = new Point(15, 85), AutoSize = true }, dtp,
        new Label { Text = "Default Daily Start Time:", Location = new Point(15, 115), AutoSize = true }, cbST,
        new Label { Text = "Lunch Duration (hours):", Location = new Point(15, 145), AutoSize = true }, nLD
    });
        }

        private void CreateTravelSection(TabPage tabPage, CommissioningResource resource)
        {
            var g = new GroupBox
            {
                Name = "groupBoxTravel",
                Text = "Travel",
                Location = new Point(10, 200),
                Size = new Size(420, 230)
            };
            tabPage.Controls.Add(g);
            int y = 25;

            CheckBox cSTT = new CheckBox { Name = "chkSeparateTravelTo", Text = "To", Location = new Point(220, y - 2), AutoSize = true, Checked = resource.SeparateTravelTo };
            CheckBox cSTF = new CheckBox { Name = "chkSeparateTravelFrom", Text = "From", Location = new Point(300, y - 2), AutoSize = true, Checked = resource.SeparateTravelFrom };
            cSTT.CheckedChanged += (s, e) => { UpdateResourceFromUI(resource); QueueScheduleUpdate(tabPage, resource); };
            cSTF.CheckedChanged += (s, e) => { UpdateResourceFromUI(resource); QueueScheduleUpdate(tabPage, resource); };
            y += 30;

            ComboBox cbTM = new ComboBox { Name = "comboBoxTravelMethod", Location = new Point(290, y - 3), Size = new Size(60, 21), DropDownStyle = ComboBoxStyle.DropDownList };
            cbTM.SelectedIndexChanged += (s, e) => UpdateResourceFromUI(resource);
            y += 30;

            NumericUpDown nTD = new NumericUpDown { Name = "numTravelDistance", Location = new Point(290, y - 3), Size = new Size(75, 20), Maximum = 10000, Value = resource.TravelDistance };
            nTD.ValueChanged += (s, e) => UpdateResourceFromUI(resource);
            y += 30;

            NumericUpDown nTT = new NumericUpDown { Name = "numTravelTime", Location = new Point(290, y - 3), Size = new Size(75, 20), Maximum = 48, Increment = 0.5m, DecimalPlaces = 1, Value = resource.TravelTime };
            nTT.ValueChanged += (s, e) => UpdateResourceFromUI(resource);
            nTT.Leave += (s, e) => {
                UpdateResourceFromUI(resource);
                if (redrawDelayTimer == null) {
                    redrawDelayTimer = new Timer { Interval = 400 };
                    redrawDelayTimer.Tick += (sender2, e2) => {
                        redrawDelayTimer.Stop();
                        resource.InitializeFromSchedule();
                        RegenerateSchedule(tabPage, resource);
                    };
                }
                redrawDelayTimer.Stop();
                redrawDelayTimer.Start();
            };
            nTT.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) {
                    e.SuppressKeyPress = true;
                    this.SelectNextControl(nTT, true, true, true, true);
                }
            };
            y += 30;

            // Restore the declarations for nDTD and nDTT before the g.Controls.AddRange call in CreateTravelSection
            NumericUpDown nDTD = new NumericUpDown { Name = "numDailyTravelDistance", Location = new Point(290, y - 3), Size = new Size(75, 20), Maximum = 1000, Increment = 15, Value = resource.DailyTravelDistance };
            nDTD.ValueChanged -= (s, e) => UpdateResourceFromUI(resource); // Remove if present
            object nDTD_OriginalValue = nDTD.Value;
            nDTD.Enter += (s, e) => nDTD_OriginalValue = nDTD.Value;
            nDTD.Leave += (s, e) => {
                if (!nDTD.Value.Equals(nDTD_OriginalValue)) {
                    UpdateResourceFromUI(resource);
                    resource.InitializeFromSchedule();
                    RegenerateSchedule(tabPage, resource);
                }
            };
            nDTD.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) {
                    e.SuppressKeyPress = true;
                    this.SelectNextControl(nDTD, true, true, true, true);
                }
            };
            y += 30;

            NumericUpDown nDTT = new NumericUpDown { Name = "numDailyTravelTime", Location = new Point(290, y - 3), Size = new Size(75, 20), Maximum = 24, Increment = 0.25m, DecimalPlaces = 2, Value = resource.DailyTravelTime };
            nDTT.ValueChanged -= (s, e) => UpdateResourceFromUI(resource); // Remove if present
            object nDTT_OriginalValue = nDTT.Value;
            nDTT.Enter += (s, e) => nDTT_OriginalValue = nDTT.Value;
            nDTT.Leave += (s, e) => {
                if (!nDTT.Value.Equals(nDTT_OriginalValue)) {
                    UpdateResourceFromUI(resource);
                    resource.InitializeFromSchedule();
                    RegenerateSchedule(tabPage, resource);
                }
            };
            nDTT.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) {
                    e.SuppressKeyPress = true;
                    this.SelectNextControl(nDTT, true, true, true, true);
                }
            };
            y += 30;

            g.Controls.AddRange(new Control[] {
        new Label { Text = "Separate Travel Day", Location = new Point(15, 25), AutoSize = true }, cSTT, cSTF,
        new Label { Text = "Travel Method to Site Area:", Location = new Point(15, 55), AutoSize = true }, cbTM,
        new Label { Text = "Driving Distance to Site Area (First and Last Days Only):", Location = new Point(15, 85), AutoSize = true }, nTD, new Label { Text = "miles/km", Location = new Point(370, 85), AutoSize = true },
        new Label { Text = "Total Travel Time to Site Area (Inlcuding Flight):", Location = new Point(15, 115), AutoSize = true }, nTT, new Label { Text = "hours", Location = new Point(370, 115), AutoSize = true },
        new Label { Text = "Daily Travel Distance (One Way):", Location = new Point(15, 145), AutoSize = true }, nDTD, new Label { Text = "miles/km", Location = new Point(370, 145), AutoSize = true },
        new Label { Text = "Daily Travel Time (One Way):", Location = new Point(15, 175), AutoSize = true }, nDTT, new Label { Text = "hours", Location = new Point(370, 175), AutoSize = true }
    });
        }

        private void CreateExpensesSection(TabPage tabPage, CommissioningResource resource)
        {
            var g = new GroupBox
            {
                Name = "groupBoxExpenses",
                Text = "Expenses",
                Location = new Point(10, 440),
                Size = new Size(420, 220)
            };
            tabPage.Controls.Add(g);
            int y = 25;

            // Flight Cost (Round Trip)
            Label lblFlightCost = new Label
            {
                Text = "Flight Cost (Round Trip):",
                Location = new Point(15, 25),
                AutoSize = true
            };

            NumericUpDown nFC = new NumericUpDown
            {
                Name = "numFlightCost",
                Location = new Point(190, y - 3),
                Size = new Size(90, 20),
                Maximum = 10000,
                DecimalPlaces = 2,
                Value = resource.FlightCost
            };
            nFC.ValueChanged += (s, e) => UpdateResourceFromUI(resource);
            y += 30;

            // Rental Car
            CheckBox cRCR = new CheckBox
            {
                Name = "chkRentalCarRequired",
                Text = "Rental Car",
                Location = new Point(15, y - 2),
                AutoSize = true,
                Checked = resource.RentalCarRequired
            };

            NumericUpDown nRCR = new NumericUpDown
            {
                Name = "numRentalCarRate",
                Location = new Point(190, y - 3),
                Size = new Size(90, 20),
                Maximum = 500,
                DecimalPlaces = 2,
                Value = resource.RentalCarRate
            };

            Label lblRentalPerDay = new Label
            {
                Text = "per day",
                Location = new Point(285, 55),
                AutoSize = true
            };

            cRCR.CheckedChanged += (s, e) => UpdateResourceFromUI(resource);
            nRCR.ValueChanged += (s, e) => UpdateResourceFromUI(resource);
            y += 30;

            // Hotel
            CheckBox cHR = new CheckBox
            {
                Name = "chkHotelRequired",
                Text = "Hotel",
                Location = new Point(15, y - 2),
                AutoSize = true,
                Checked = resource.HotelRequired
            };

            NumericUpDown nHR = new NumericUpDown
            {
                Name = "numHotelRate",
                Location = new Point(190, y - 3),
                Size = new Size(90, 20),
                Maximum = 1000,
                DecimalPlaces = 2,
                Value = resource.HotelRate
            };

            Label lblHotelPerNight = new Label
            {
                Text = "per night",
                Location = new Point(285, 85),
                AutoSize = true
            };

            cHR.CheckedChanged += (s, e) => UpdateResourceFromUI(resource);
            nHR.ValueChanged += (s, e) => UpdateResourceFromUI(resource);
            y += 30;

            // Mileage Rate
            Label lblMileageRate = new Label
            {
                Text = "Mileage Rate:",
                Location = new Point(15, 115),
                AutoSize = true
            };

            NumericUpDown nMR = new NumericUpDown
            {
                Name = "numMileageRate",
                Location = new Point(190, y - 3),
                Size = new Size(90, 20),
                Maximum = 2,
                DecimalPlaces = 2,
                Increment = 0.01m,
                Value = resource.MileageRate
            };

            Label lblMileagePerUnit = new Label
            {
                Text = "per mile/km",
                Location = new Point(285, 115),
                AutoSize = true
            };

            nMR.ValueChanged += (s, e) => UpdateResourceFromUI(resource);
            y += 30;

            // Per Diem
            Label lblPerDiem = new Label
            {
                Text = "Per Diem:",
                Location = new Point(15, 145),
                AutoSize = true
            };

            NumericUpDown nPDR = new NumericUpDown
            {
                Name = "numPerDiemRate",
                Location = new Point(190, y - 3),
                Size = new Size(90, 20),
                Maximum = 500,
                DecimalPlaces = 2,
                Value = resource.PerDiemRate
            };

            Label lblPerDiemPerDay = new Label
            {
                Text = "per day",
                Location = new Point(285, 145),
                AutoSize = true
            };

            nPDR.ValueChanged += (s, e) => UpdateResourceFromUI(resource);
            y += 30;

            // Other Expenses (commented out in original code)
            NumericUpDown nOE = new NumericUpDown
            {
                Name = "numOtherExpenses",
                Location = new Point(190, y - 3),
                Size = new Size(90, 20),
                Maximum = 20000,
                DecimalPlaces = 2,
                Value = resource.OtherExpenses
            };
            nOE.ValueChanged += (s, e) => UpdateResourceFromUI(resource);

            // Add all controls to the group box
            g.Controls.AddRange(new Control[] {
       lblFlightCost, nFC,
       cRCR, nRCR, lblRentalPerDay,
       cHR, nHR, lblHotelPerNight,
       lblMileageRate, nMR, lblMileagePerUnit,
       lblPerDiem, nPDR, lblPerDiemPerDay,
       // new Label { Text = "Other Fixed Expenses:", Location = new Point(15, 175), AutoSize = true }, nOE
   });
        }
        private void CreateScheduleSection(TabPage tabPage, CommissioningResource resource)
        {
            var gS = new GroupBox
            {
                Name = "groupBoxSchedule",
                Text = "Daily Schedule Plan",
                Location = new Point(440, 200),
                Size = new Size(750, Math.Max(460, tabPage.ClientSize.Height - 210 - SystemInformation.HorizontalScrollBarHeight)),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            tabPage.Controls.Add(gS);

            var pS = new Panel
            {
                Name = "panelSchedule",
                Location = new Point(10, 20),
                Size = new Size(gS.ClientSize.Width - 20, gS.ClientSize.Height - 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true,
                BorderStyle = BorderStyle.Fixed3D
            };
            gS.Controls.Add(pS);

            GenerateCalendarLayout(pS, resource);
        }

        private void CreateResourceInfoSection(TabPage tabPage, CommissioningResource resource)
        {
            var g = new GroupBox
            {
                Name = "groupBoxResourceInfo",
                Text = "Resource Information",
                Location = new Point(870, 10),
                Size = new Size(320, 180)
            };
            tabPage.Controls.Add(g);

            // Technician Name
            Label lblTechName = new Label
            {
                Text = "Technician Name:",
                Location = new Point(15, 30),
                AutoSize = true
            };

            TextBox txtTechName = new TextBox
            {
                Name = "txtTechnicianName",
                Location = new Point(15, 55),
                Size = new Size(290, 20),
                Text = resource.TechnicianName ?? ""
            };

            // Wire up the event to update both the resource and tab title
            txtTechName.TextChanged += (s, e) =>
            {
                if (resource != null)
                {
                    resource.TechnicianName = txtTechName.Text;
                    tabPage.Text = string.IsNullOrWhiteSpace(txtTechName.Text) ? "New Resource" : txtTechName.Text;
                    resource.IsDirty = true;
                    currentProject.IsDirty = true;
                }
            };

            g.Controls.AddRange(new Control[] { lblTechName, txtTechName });
        }
        private void RegenerateSchedule(TabPage tabPage, CommissioningResource resource)
        {
            var groupBoxSchedule = FindControlInTab<GroupBox>(tabPage, "groupBoxSchedule");
            if (groupBoxSchedule == null) return;
            var panelSchedule = FindControlByName<Panel>(groupBoxSchedule, "panelSchedule");
            if (panelSchedule == null) return;
            resource.InitializeFromSchedule();
            GenerateCalendarLayout(panelSchedule, resource);
        }

        private void UpdateExistingDayPanels(Panel schedulePanel, CommissioningResource resource)
        {
            if (resource.DailyData == null || !resource.DailyData.Any()) return;

            // Update each existing day panel with new data
            foreach (Control control in schedulePanel.Controls)
            {
                if (control is FlowLayoutPanel weekPanel)
                {
                    foreach (Control dayControl in weekPanel.Controls)
                    {
                        if (dayControl is Panel dayBox && dayBox.Tag is DateTime date)
                        {
                            // Find the corresponding day data
                            var dayData = resource.DailyData.Values.FirstOrDefault(d => d.Date.Date == date.Date);
                            if (dayData != null)
                            {
                                // Find the day key
                                var dayKey = resource.DailyData.FirstOrDefault(kvp => kvp.Value == dayData).Key;
                                if (dayKey != 0) // Valid key found
                                {
                                    UpdateDayPanelContent(dayBox, dayKey, dayData, resource);
                                    UpdateDayPanelColors(dayBox, dayData, resource);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void UpdateDayPanelContent(Panel dayBox, int dayKey, ResourceDayData dayData, CommissioningResource resource)
        {
            // Update the existing controls instead of recreating them
            foreach (Control control in dayBox.Controls)
            {
                if (control is ComboBox comboStart && control.Name == $"comboPlannedStart_{dayKey}")
                {
                    var currentValue = ConvertTo12Hour(dayData.PlannedStartTime);
                    if (comboStart.SelectedItem?.ToString() != currentValue)
                    {
                        comboStart.SelectedItem = currentValue;
                    }
                }
                else if (control is NumericUpDown numHours && control.Name == $"numPlannedHours_{dayKey}")
                {
                    var currentValue = dayData.GetPlannedLabourHoursTotal();
                    if (numHours.Value != currentValue)
                    {
                        numHours.Value = currentValue;
                    }
                }
                else if (control is NumericUpDown numTravel && control.Name == $"numPlannedTravel_{dayKey}")
                {
                    var currentValue = dayData.GetPlannedTravelHoursTotal();
                    if (numTravel.Value != currentValue)
                    {
                        numTravel.Value = currentValue;
                    }
                }
            }
        }

        private void GenerateCalendarLayout(Panel parentPanel, CommissioningResource resource)
        {
            parentPanel.SuspendLayout();
            parentPanel.Controls.Clear();

            if (resource.DailyData == null || !resource.DailyData.Any()) resource.InitializeFromSchedule();
            if (resource.DailyData == null || !resource.DailyData.Any()) { parentPanel.ResumeLayout(false); return; }

            var orderedDailyDataEntries = resource.DailyData.OrderBy(kvp => kvp.Value.Date).ToList();
            if (!orderedDailyDataEntries.Any()) { parentPanel.ResumeLayout(false); return; }

            DateTime minDateInSchedule = orderedDailyDataEntries.First().Value.Date;
            DateTime maxDateInSchedule = orderedDailyDataEntries.Last().Value.Date;
            DateTime firstCalendarDisplaySunday = minDateInSchedule.AddDays(-(int)minDateInSchedule.DayOfWeek);
            DateTime lastCalendarDisplaySaturday = maxDateInSchedule.AddDays(6 - (int)maxDateInSchedule.DayOfWeek);
            int totalWeeksToDisplay = (int)Math.Ceiling((lastCalendarDisplaySaturday - firstCalendarDisplaySunday).TotalDays / 7.0);
            if (totalWeeksToDisplay == 0 && orderedDailyDataEntries.Any()) totalWeeksToDisplay = 1;

            int availableWidth = parentPanel.ClientSize.Width - (parentPanel.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0) - 5;
            int margin = 3, dayBoxWidth = 105, dayBoxHeight = 105, weekHeaderHeight = 25, currentY = margin;

            for (int weekNum = 0; weekNum < totalWeeksToDisplay; weekNum++)
            {
                DateTime currentWeekSunday = firstCalendarDisplaySunday.AddDays(weekNum * 7);
                Label lblWeek = new Label { Text = $"Week of {currentWeekSunday:MMM dd, yyyy}", Location = new Point(margin, currentY), Font = new Font(this.Font.FontFamily, 9, FontStyle.Bold), AutoSize = true };
                parentPanel.Controls.Add(lblWeek); currentY += weekHeaderHeight;
                FlowLayoutPanel weekDaysPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Location = new Point(margin, currentY), Size = new Size((dayBoxWidth + margin) * 7, dayBoxHeight + margin), WrapContents = false };

                for (int dayOfWeekIdx = 0; dayOfWeekIdx < 7; dayOfWeekIdx++)
                {
                    DateTime calendarDateForBox = currentWeekSunday.AddDays(dayOfWeekIdx);
                    ResourceDayData dayEntryForBox = resource.DailyData.Values.FirstOrDefault(d => d.Date.Date == calendarDateForBox.Date);
                    int dayKeyForBox = -99;
                    if (dayEntryForBox != null)
                    {
                        var kvp = resource.DailyData.FirstOrDefault(pair => pair.Value == dayEntryForBox);
                        if (!kvp.Equals(default(KeyValuePair<int, ResourceDayData>)))
                            dayKeyForBox = kvp.Key;
                    }

                    Panel dayBox = new Panel { Name = $"dayBox_{calendarDateForBox:yyyyMMdd}", Size = new Size(dayBoxWidth, dayBoxHeight), BorderStyle = BorderStyle.FixedSingle, Tag = calendarDateForBox, Margin = new Padding(0, 0, margin, margin) };
                    Label lblDateOnly = new Label { Text = $"{calendarDateForBox:ddd, MMM dd}", Location = new Point(3, 3), AutoSize = true }; dayBox.Controls.Add(lblDateOnly);

                    // Add right-click context menu for manual day type selection
                    if (dayEntryForBox != null && dayKeyForBox != -99)
                    {
                        ContextMenuStrip dayTypeMenu = new ContextMenuStrip();
                        dayTypeMenu.Items.Add("Work Day", null, (s, e) => SetManualDayTypeAndUpdate(dayEntryForBox, resource, dayKeyForBox, DayType.Work));
                        dayTypeMenu.Items.Add("Travel Day", null, (s, e) => SetManualDayTypeAndUpdate(dayEntryForBox, resource, dayKeyForBox, DayType.Travel));
                        dayTypeMenu.Items.Add("Holdover", null, (s, e) => SetManualDayTypeAndUpdate(dayEntryForBox, resource, dayKeyForBox, DayType.Holdover));
                        dayTypeMenu.Items.Add("No Activity", null, (s, e) => SetManualDayTypeAndUpdate(dayEntryForBox, resource, dayKeyForBox, DayType.Nil));
                        dayTypeMenu.Items.Add(new ToolStripSeparator());
                        dayTypeMenu.Items.Add("Auto (Reset)", null, (s, e) => SetManualDayTypeAndUpdate(dayEntryForBox, resource, dayKeyForBox, null));
                        dayBox.MouseUp += (s, e) => {
                            if (e.Button == MouseButtons.Right)
                            {
                                dayTypeMenu.Show(dayBox, e.Location);
                            }
                        };
                    }

                    if (dayEntryForBox != null && dayKeyForBox != -99)
                    {
                        dayBox.BackColor = SystemColors.Window; lblDateOnly.Font = new Font(dayBox.Font, FontStyle.Bold);
                        CreateDayContent_Planning(dayBox, dayKeyForBox, dayEntryForBox, resource);
                        UpdateDayPanelColors(dayBox, dayEntryForBox, resource);
                    }
                    else { dayBox.BackColor = Color.FromArgb(80, 80, 80); lblDateOnly.ForeColor = SystemColors.GrayText; }
                    weekDaysPanel.Controls.Add(dayBox);
                }
                parentPanel.Controls.Add(weekDaysPanel); currentY += dayBoxHeight + margin + 5;
            }
            parentPanel.ResumeLayout(false);
        }

        // Helper to set manual day type and update planned values/UI
        private void SetManualDayTypeAndUpdate(ResourceDayData dayData, CommissioningResource resource, int dayKey, DayType? manualType)
        {
            dayData.ManualDayType = manualType;
            switch (manualType)
            {
                case DayType.Work:
                    dayData.PlannedRegularLabourHours = resource.HoursPerDay;
                    dayData.PlannedOvertimeLabourHours = 0;
                    dayData.PlannedPremiumLabourHours = 0;
                    dayData.PlannedRegularTravelHours = 0;
                    dayData.PlannedOvertimeTravelHours = 0;
                    dayData.PlannedPremiumTravelHours = 0;
                    dayData.PlannedStartTime = resource.DefaultStartTime;
                    dayData.PlannedEndTime = null;
                    break;
                case DayType.Travel:
                    dayData.PlannedRegularLabourHours = 0;
                    dayData.PlannedOvertimeLabourHours = 0;
                    dayData.PlannedPremiumLabourHours = 0;
                    dayData.PlannedRegularTravelHours = resource.TravelTime;
                    dayData.PlannedOvertimeTravelHours = 0;
                    dayData.PlannedPremiumTravelHours = 0;
                    dayData.PlannedStartTime = resource.DefaultStartTime;
                    dayData.PlannedEndTime = null;
                    break;
                case DayType.Holdover:
                    dayData.PlannedRegularLabourHours = 8;
                    dayData.PlannedOvertimeLabourHours = 0;
                    dayData.PlannedPremiumLabourHours = 0;
                    dayData.PlannedRegularTravelHours = 0;
                    dayData.PlannedOvertimeTravelHours = 0;
                    dayData.PlannedPremiumTravelHours = 0;
                    dayData.PlannedStartTime = null;
                    dayData.PlannedEndTime = null;
                    break;
                case DayType.Nil:
                    dayData.PlannedRegularLabourHours = 0;
                    dayData.PlannedOvertimeLabourHours = 0;
                    dayData.PlannedPremiumLabourHours = 0;
                    dayData.PlannedRegularTravelHours = 0;
                    dayData.PlannedOvertimeTravelHours = 0;
                    dayData.PlannedPremiumTravelHours = 0;
                    dayData.PlannedStartTime = null;
                    dayData.PlannedEndTime = null;
                    break;
                case null:
                    // Auto: clear manual override, re-initialize from schedule for this day
                    dayData.ManualDayType = null;
                    resource.InitializeFromSchedule();
                    break;
            }
            // Mark dirty and update UI
            resource.IsDirty = true;
            currentProject.IsDirty = true;
            // Redraw the schedule for the current tab
            RegenerateSchedule(tabResources.SelectedTab, resource);
        }

        private void CreateDayContent_Planning(Panel dayBox, int dayKey, ResourceDayData dayDataEntry, CommissioningResource resource)
        {
            var dayType = DetermineDayType(dayDataEntry, resource);
            if (dayType == DayType.Nil)
            {
                // No controls for No Activity day
                return;
            }
            int yPos = 28; int xLabel = 5; int xControl = 35; int controlWidth = dayBox.ClientSize.Width - xControl - 5;

            if (dayType == DayType.Holdover)
            {
                // Only show a disabled Hours control set to 8
                Label lblHoldoverHours = new Label { Text = "Hours", Location = new Point(xLabel, yPos), AutoSize = true, Font = new Font(this.Font.FontFamily, 7) };
                NumericUpDown numHoldoverHours = new NumericUpDown { Name = $"numPlannedHours_{dayKey}", Location = new Point(45, yPos - 2), Size = new Size(50, 18), Minimum = 0, Maximum = 24, DecimalPlaces = 1, Increment = 0.5m, Value = 8, Tag = dayDataEntry, Font = new Font(this.Font.FontFamily, 7), Enabled = false };
                dayBox.Controls.AddRange(new Control[] { lblHoldoverHours, numHoldoverHours });
                return;
            }

            Label lblPlannedStart = new Label { Text = "Start", Location = new Point(xLabel, yPos), Size = new Size(30, 12), Font = new Font(this.Font.FontFamily, 7) };
            ComboBox comboPlannedStart = new ComboBox { Name = $"comboPlannedStart_{dayKey}", Location = new Point(xControl, yPos - 2), Size = new Size(Math.Max(65, controlWidth), 21), DropDownStyle = ComboBoxStyle.DropDownList, Tag = dayDataEntry, Font = new Font(this.Font.FontFamily, 7) };
            PopulateTime12HourCombo(comboPlannedStart);
            comboPlannedStart.SelectedItem = ConvertTo12Hour(dayDataEntry.PlannedStartTime);
            comboPlannedStart.SelectedIndexChanged -= (s, e) => DailySchedulePlanned_Changed(s, e, resource, dayKey); // Remove if present
            string comboPlannedStart_OriginalValue = comboPlannedStart.SelectedItem?.ToString();
            comboPlannedStart.Enter += (s, e) => comboPlannedStart_OriginalValue = comboPlannedStart.SelectedItem?.ToString();
            comboPlannedStart.Leave += (s, e) => {
                if (comboPlannedStart.SelectedItem?.ToString() != comboPlannedStart_OriginalValue) {
                    DailySchedulePlanned_Changed(comboPlannedStart, e, resource, dayKey);
                    RegenerateSchedule(tabResources.SelectedTab, resource);
                }
            };
            comboPlannedStart.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) {
                    e.SuppressKeyPress = true;
                    this.SelectNextControl(comboPlannedStart, true, true, true, true);
                }
            };
            yPos += 23;

            Label lblPlannedHours = new Label { Text = "Hours", Location = new Point(xLabel, yPos), AutoSize = true, Font = new Font(this.Font.FontFamily, 7) };
            NumericUpDown numPlannedHours = new NumericUpDown { Name = $"numPlannedHours_{dayKey}", Location = new Point(45, yPos - 2), Size = new Size(50, 18), Minimum = 0, Maximum = 24, DecimalPlaces = 1, Increment = 0.5m, Value = dayDataEntry.GetPlannedLabourHoursTotal(), Tag = dayDataEntry, Font = new Font(this.Font.FontFamily, 7) };
            numPlannedHours.ValueChanged -= (s, e) => DailySchedulePlanned_Changed(s, e, resource, dayKey); // Remove if present
            numPlannedHours.Leave += (s, e) => {
                DailySchedulePlanned_Changed(numPlannedHours, e, resource, dayKey);
                if (redrawDelayTimer == null) {
                    redrawDelayTimer = new Timer { Interval = 400 };
                    redrawDelayTimer.Tick += (sender2, e2) => {
                        redrawDelayTimer.Stop();
                        RegenerateSchedule(tabResources.SelectedTab, resource);
                    };
                }
                redrawDelayTimer.Stop();
                redrawDelayTimer.Start();
            };
            numPlannedHours.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) {
                    e.SuppressKeyPress = true;
                    this.SelectNextControl(numPlannedHours, true, true, true, true);
                }
            };
            yPos += 23;

            Label lblPlannedTravel = new Label { Text = "Travel", Location = new Point(xLabel, yPos), AutoSize = true, Font = new Font(this.Font.FontFamily, 7) };
            NumericUpDown numPlannedTravel = new NumericUpDown { Name = $"numPlannedTravel_{dayKey}", Location = new Point(45, yPos - 2), Size = new Size(50, 18), Minimum = 0, Maximum = 24, DecimalPlaces = 1, Increment = 0.5m, Value = dayDataEntry.GetPlannedTravelHoursTotal(), Tag = dayDataEntry, Font = new Font(this.Font.FontFamily, 7) };
            numPlannedTravel.ValueChanged -= (s, e) => DailySchedulePlanned_Changed(s, e, resource, dayKey); // Remove if present
            numPlannedTravel.Leave += (s, e) => {
                DailySchedulePlanned_Changed(numPlannedTravel, e, resource, dayKey);
                if (redrawDelayTimer == null) {
                    redrawDelayTimer = new Timer { Interval = 400 };
                    redrawDelayTimer.Tick += (sender2, e2) => {
                        redrawDelayTimer.Stop();
                        RegenerateSchedule(tabResources.SelectedTab, resource);
                    };
                }
                redrawDelayTimer.Stop();
                redrawDelayTimer.Start();
            };
            numPlannedTravel.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) {
                    e.SuppressKeyPress = true;
                    this.SelectNextControl(numPlannedTravel, true, true, true, true);
                }
            };

            dayBox.Controls.AddRange(new Control[] { lblPlannedStart, comboPlannedStart, lblPlannedHours, numPlannedHours, lblPlannedTravel, numPlannedTravel });
        }

        private void DailySchedulePlanned_Changed(object sender, EventArgs e, CommissioningResource resource, int dayKey)
        {
            Control control = sender as Control;
            if (resource != null && resource.DailyData.TryGetValue(dayKey, out ResourceDayData dayDataEntry))
            {
                bool changed = false;
                
                if (sender is ComboBox comboStart && comboStart.SelectedItem != null)
                {
                    string newTime = ConvertTo24Hour(comboStart.SelectedItem.ToString());
                    if (dayDataEntry.PlannedStartTime != newTime) 
                    { 
                        dayDataEntry.PlannedStartTime = newTime; 
                        changed = true; 
                    }
                }
                else if (sender is NumericUpDown numInput)
                {
                    if (control.Name.StartsWith("numPlannedHours"))
                    {
                        if (dayDataEntry.PlannedRegularLabourHours != numInput.Value)
                        {
                            dayDataEntry.PlannedRegularLabourHours = numInput.Value;
                            dayDataEntry.PlannedOvertimeLabourHours = 0;
                            dayDataEntry.PlannedPremiumLabourHours = 0;
                            changed = true;
                        }
                    }
                    else if (control.Name.StartsWith("numPlannedTravel"))
                    {
                        if (dayDataEntry.PlannedRegularTravelHours != numInput.Value)
                        {
                            dayDataEntry.PlannedRegularTravelHours = numInput.Value;
                            dayDataEntry.PlannedOvertimeTravelHours = 0;
                            dayDataEntry.PlannedPremiumTravelHours = 0;
                            changed = true;
                        }
                    }
                }

                if (changed)
                {
                    resource.IsDirty = true;
                    currentProject.IsDirty = true;
                    
                    // Only queue a schedule update if we actually need to recalculate
                    // For simple value changes, we don't need to rebuild the entire schedule
                    // The changes are already reflected in the UI controls
                }
            }
            else 
            { 
                System.Diagnostics.Debug.WriteLine($"DailySchedulePlanned_Changed: Could not find resource or dayKey {dayKey}."); 
            }
        }

        // Helper method to get resource from tab page with new Tag structure
        private CommissioningResource GetResourceFromTabPage(TabPage tabPage)
        {
            if (tabPage.Tag is CommissioningResource resource)
            {
                return resource;
            }
            else if (tabPage.Tag is Dictionary<string, object> tagDict && tagDict.ContainsKey("resource"))
            {
                return tagDict["resource"] as CommissioningResource;
            }
            return null;
        }

        private void PopulateTime12HourCombo(ComboBox combo) { combo.Items.Clear(); for (int h = 0; h < 24; h++) for (int m = 0; m < 60; m += 30) combo.Items.Add(new DateTime(2000, 1, 1, h, m, 0).ToString("h:mm tt", System.Globalization.CultureInfo.InvariantCulture)); }
        private string ConvertTo12Hour(string time24) { if (DateTime.TryParseExact(time24, "HH:mm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime dt)) return dt.ToString("h:mm tt", System.Globalization.CultureInfo.InvariantCulture); return new DateTime(2000, 1, 1, 7, 0, 0).ToString("h:mm tt"); }
        private string ConvertTo24Hour(string time12) { if (DateTime.TryParse(time12, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime dt)) return dt.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture); return "07:00"; }
        private DateTime GetNextOccurrence(DateTime fromDate, string dayOfWeekName) { if (!Enum.TryParse(dayOfWeekName, true, out DayOfWeek targetDay)) targetDay = DayOfWeek.Monday; int daysToAdd = ((int)targetDay - (int)fromDate.DayOfWeek + 7) % 7; return fromDate.AddDays(daysToAdd); }

        private void PopulateTabDropdowns(TabPage tabPage, CommissioningResource resource)
        { /* ... same as corrected_csharp_code_v6 ... */
            if (resource == null || tabPage == null) return;
            var cRS = FindControlInTab<ComboBox>(tabPage, "comboBoxRateSheet"); if (cRS != null && rateSheets != null && rateSheets.Any()) { cRS.Items.Clear(); foreach (var rs in rateSheets) cRS.Items.Add(rs.Name); if (!string.IsNullOrEmpty(resource.RateSheetName) && cRS.Items.Contains(resource.RateSheetName)) cRS.SelectedItem = resource.RateSheetName; else if (cRS.Items.Count > 0) cRS.SelectedIndex = 0; }
            var dtp = FindControlInTab<DateTimePicker>(tabPage, "dtpResourceStartDate"); if (dtp != null) dtp.Value = resource.StartDate;
            var cST = FindControlInTab<ComboBox>(tabPage, "comboBoxStartTime"); if (cST != null) { PopulateTime12HourCombo(cST); if (!string.IsNullOrEmpty(resource.DefaultStartTime)) cST.SelectedItem = ConvertTo12Hour(resource.DefaultStartTime); else cST.SelectedItem = ConvertTo12Hour("07:00"); }
            var nLD = FindControlInTab<NumericUpDown>(tabPage, "numLunchDuration"); if (nLD != null) nLD.Value = resource.LunchDuration;
            var cTM = FindControlInTab<ComboBox>(tabPage, "comboBoxTravelMethod"); if (cTM != null) { cTM.Items.Clear(); cTM.Items.AddRange(new string[] { "Driving", "Flight" }); if (!string.IsNullOrEmpty(resource.TravelMethod) && cTM.Items.Contains(resource.TravelMethod)) cTM.SelectedItem = resource.TravelMethod; else cTM.SelectedIndex = 0; }
        }
        private T FindControlInTab<T>(TabPage tabPage, string controlName) where T : Control => FindControlByName<T>(tabPage, controlName);
        private T FindControlByName<T>(Control parent, string name) where T : Control { if (parent == null) return null; foreach (Control c in parent.Controls) { if (c.Name == name && c is T typedControl) return typedControl; var foundChild = FindControlByName<T>(c, name); if (foundChild != null) return foundChild; } return null; }
        private void LoadResourceDataIntoControls(CommissioningResource resource, TabPage tabPage)
        {
            if (resource == null || tabPage == null) return;
            
            // Only initialize schedule if needed
            if (resource.DailyData == null || !resource.DailyData.Any() || resource.IsDirty) 
            {
                resource.InitializeFromSchedule();
            }
            
            // Load data into controls
            FindControlInTab<NumericUpDown>(tabPage, "numDiscount")?.SetValue(c => c.Value = resource.DiscountPercent);
            FindControlInTab<CheckBox>(tabPage, "chkEmergency")?.SetValue(c => c.Checked = resource.IsEmergency);
            UpdateRateDisplayTextBoxes(tabPage, resource);
            
            FindControlInTab<NumericUpDown>(tabPage, "numDaysOnSite")?.SetValue(c => c.Value = Math.Max(0, resource.DaysOnSite));
            FindControlInTab<NumericUpDown>(tabPage, "numHoursPerDay")?.SetValue(c => c.Value = Math.Max(0, resource.HoursPerDay));
            
            FindControlInTab<CheckBox>(tabPage, "chkSeparateTravelTo")?.SetValue(c => c.Checked = resource.SeparateTravelTo);
            FindControlInTab<CheckBox>(tabPage, "chkSeparateTravelFrom")?.SetValue(c => c.Checked = resource.SeparateTravelFrom);
            FindControlInTab<NumericUpDown>(tabPage, "numTravelDistance")?.SetValue(c => c.Value = resource.TravelDistance);
            FindControlInTab<NumericUpDown>(tabPage, "numTravelTime")?.SetValue(c => c.Value = resource.TravelTime);
            FindControlInTab<NumericUpDown>(tabPage, "numDailyTravelDistance")?.SetValue(c => c.Value = resource.DailyTravelDistance);
            FindControlInTab<NumericUpDown>(tabPage, "numDailyTravelTime")?.SetValue(c => c.Value = resource.DailyTravelTime);
            
            FindControlInTab<NumericUpDown>(tabPage, "numFlightCost")?.SetValue(c => c.Value = resource.FlightCost);
            FindControlInTab<CheckBox>(tabPage, "chkRentalCarRequired")?.SetValue(c => c.Checked = resource.RentalCarRequired);
            FindControlInTab<NumericUpDown>(tabPage, "numRentalCarRate")?.SetValue(c => c.Value = resource.RentalCarRate);
            FindControlInTab<CheckBox>(tabPage, "chkHotelRequired")?.SetValue(c => c.Checked = resource.HotelRequired);
            FindControlInTab<NumericUpDown>(tabPage, "numHotelRate")?.SetValue(c => c.Value = resource.HotelRate);
            FindControlInTab<NumericUpDown>(tabPage, "numMileageRate")?.SetValue(c => c.Value = resource.MileageRate);
            FindControlInTab<NumericUpDown>(tabPage, "numPerDiemRate")?.SetValue(c => c.Value = resource.PerDiemRate);
            FindControlInTab<NumericUpDown>(tabPage, "numOtherExpenses")?.SetValue(c => c.Value = resource.OtherExpenses);
            
            FindControlInTab<TextBox>(tabPage, "txtTechnicianName")?.SetValue(c => c.Text = resource.TechnicianName ?? "");
            
            // Only queue schedule update if the resource structure has actually changed
            if (resource.IsDirty)
            {
                QueueScheduleUpdate(tabPage, resource);
            }
        }
        private void BtnAddResource_Click(object sender, EventArgs e) => AddNewResource();
        private void AddNewResource()
        {
            var nr = new CommissioningResource();

            // Set default technician name
            nr.TechnicianName = $"Resource {currentProject.Resources.Count + 1}";

            if (rateSheets != null && rateSheets.Any())
            {
                nr.RateSheetName = rateSheets.First().Name;
                PopulateResourceRatesFromSheetName(nr);
            }
            nr.InitializeFromSchedule();
            currentProject.Resources.Add(nr);
            CreateResourceTab(nr);
            if (tabResources.TabPages.Count > 0)
                tabResources.SelectedTab = tabResources.TabPages[tabResources.TabPages.Count - 1];
            currentProject.IsDirty = true;
        }
        private void BtnDeleteResource_Click(object sender, EventArgs e)
        {
            if (tabResources.SelectedTab == null || tabResources.TabPages.Count <= 1)
            {
                MessageBox.Show("At least one resource is required or no resource is selected.", "Cannot Delete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show("Are you sure you want to delete this resource?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                // Use the helper method instead of direct Tag access
                CommissioningResource rtr = GetResourceFromTabPage(tabResources.SelectedTab);
                if (rtr != null)
                {
                    currentProject.Resources.Remove(rtr);
                }
                tabResources.TabPages.Remove(tabResources.SelectedTab);
                currentProject.IsDirty = true;
            }
        }


        private void SyncExpenseProperties()
        {
            foreach (TabPage tab in tabResources.TabPages)
            {
                if (tab.Tag is CommissioningResource resource)
                {
                    // Find the expense controls in this tab
                    CheckBox chkRentalCar = FindControlInTab<CheckBox>(tab, "chkRentalCar");
                    CheckBox chkHotel = FindControlInTab<CheckBox>(tab, "chkHotel");
                    ComboBox comboTravelMethod = FindControlInTab<ComboBox>(tab, "comboBoxTravelMethod");

                    // Update the resource properties directly
                    if (chkRentalCar != null)
                        resource.RentalCarRequired = chkRentalCar.Checked;

                    if (chkHotel != null)
                        resource.HotelRequired = chkHotel.Checked;

                    if (comboTravelMethod != null && comboTravelMethod.SelectedItem != null)
                        resource.TravelMethod = comboTravelMethod.SelectedItem.ToString();

                    System.Diagnostics.Debug.WriteLine($"Synced expense properties for {resource.TechnicianName}: " +
                        $"Hotel={resource.HotelRequired}, Rental={resource.RentalCarRequired}, Travel={resource.TravelMethod}");
                }
            }
        }


        private void UpdateResourceFromUI(CommissioningResource resource)
        {
            if (resource == null) return;

            // Use the helper method instead of direct Tag comparison
            TabPage ct = tabResources.TabPages.Cast<TabPage>().FirstOrDefault(t => GetResourceFromTabPage(t) == resource);
            if (ct == null) return;

            bool spc = false;

            // Days and schedule configuration
            var nDS = FindControlInTab<NumericUpDown>(ct, "numDaysOnSite");
            if (nDS != null && resource.DaysOnSite != (int)nDS.Value)
            {
                resource.DaysOnSite = (int)nDS.Value;
                spc = true;
            }

            var nHPD = FindControlInTab<NumericUpDown>(ct, "numHoursPerDay");
            if (nHPD != null && resource.HoursPerDay != nHPD.Value)
            {
                resource.HoursPerDay = nHPD.Value;
                spc = true;
            }

            var dtp = FindControlInTab<DateTimePicker>(ct, "dtpResourceStartDate");
            if (dtp != null && resource.StartDate.Date != dtp.Value.Date)
            {
                resource.StartDate = dtp.Value.Date;
                spc = true;
            }

            var cbST = FindControlInTab<ComboBox>(ct, "comboBoxStartTime");
            if (cbST?.SelectedItem != null && resource.DefaultStartTime != ConvertTo24Hour(cbST.SelectedItem.ToString()))
            {
                resource.DefaultStartTime = ConvertTo24Hour(cbST.SelectedItem.ToString());
                spc = true;
            }

            var nLD = FindControlInTab<NumericUpDown>(ct, "numLunchDuration");
            if (nLD != null && resource.LunchDuration != nLD.Value)
            {
                resource.LunchDuration = nLD.Value;
                spc = true;
            }

            var cSTT = FindControlInTab<CheckBox>(ct, "chkSeparateTravelTo");
            if (cSTT != null && resource.SeparateTravelTo != cSTT.Checked)
            {
                resource.SeparateTravelTo = cSTT.Checked;
                spc = true;
            }

            var cSTF = FindControlInTab<CheckBox>(ct, "chkSeparateTravelFrom");
            if (cSTF != null && resource.SeparateTravelFrom != cSTF.Checked)
            {
                resource.SeparateTravelFrom = cSTF.Checked;
                spc = true;
            }

            var txtTechName = FindControlInTab<TextBox>(ct, "txtTechnicianName");
            if (txtTechName != null && resource.TechnicianName != txtTechName.Text)
            {
                resource.TechnicianName = txtTechName.Text;
                ct.Text = string.IsNullOrWhiteSpace(txtTechName.Text) ? "New Resource" : txtTechName.Text;
            }

            // Expense properties
            var cRCR = FindControlInTab<CheckBox>(ct, "chkRentalCarRequired");
            if (cRCR != null)
            {
                resource.RentalCarRequired = cRCR.Checked;
            }

            var nRCR = FindControlInTab<NumericUpDown>(ct, "numRentalCarRate");
            if (nRCR != null)
            {
                resource.RentalCarRate = nRCR.Value;
            }

            var cHR = FindControlInTab<CheckBox>(ct, "chkHotelRequired");
            if (cHR != null)
            {
                resource.HotelRequired = cHR.Checked;
            }

            var nHR = FindControlInTab<NumericUpDown>(ct, "numHotelRate");
            if (nHR != null)
            {
                resource.HotelRate = nHR.Value;
            }

            var nFC = FindControlInTab<NumericUpDown>(ct, "numFlightCost");
            if (nFC != null)
            {
                resource.FlightCost = nFC.Value;
            }

            var nMR = FindControlInTab<NumericUpDown>(ct, "numMileageRate");
            if (nMR != null)
            {
                resource.MileageRate = nMR.Value;
            }

            var nPDR = FindControlInTab<NumericUpDown>(ct, "numPerDiemRate");
            if (nPDR != null)
            {
                resource.PerDiemRate = nPDR.Value;
            }

            var nOE = FindControlInTab<NumericUpDown>(ct, "numOtherExpenses");
            if (nOE != null)
            {
                resource.OtherExpenses = nOE.Value;
            }

            // Travel settings
            var cbTM = FindControlInTab<ComboBox>(ct, "comboBoxTravelMethod");
            if (cbTM?.SelectedItem != null)
            {
                resource.TravelMethod = cbTM.SelectedItem.ToString();
            }

            var nTD = FindControlInTab<NumericUpDown>(ct, "numTravelDistance");
            if (nTD != null)
            {
                resource.TravelDistance = nTD.Value;
            }

            var nTT = FindControlInTab<NumericUpDown>(ct, "numTravelTime");
            if (nTT != null)
            {
                resource.TravelTime = nTT.Value;
            }

            var nDTD = FindControlInTab<NumericUpDown>(ct, "numDailyTravelDistance");
            if (nDTD != null)
            {
                resource.DailyTravelDistance = nDTD.Value;
            }

            var nDTT = FindControlInTab<NumericUpDown>(ct, "numDailyTravelTime");
            if (nDTT != null)
            {
                resource.DailyTravelTime = nDTT.Value;
            }

            // If schedule parameters changed, regenerate the schedule
            if (spc)
            {
                QueueScheduleUpdate(ct, resource);
            }

            resource.IsDirty = true;
            currentProject.IsDirty = true;
        }
        private void SetupAutoSave() { /* ... same as corrected_csharp_code_v6 ... */ autoSaveTimer = new Timer { Interval = 15000 }; autoSaveTimer.Tick += (s, e) => SaveProjectData(false); autoSaveTimer.Start(); }
        private void SaveProjectData(bool showUserMessage)
        { /* ... same as corrected_csharp_code_v6 ... */
            try { if (tabResources.SelectedTab?.Tag is CommissioningResource cr) UpdateResourceFromUI(cr); foreach (var res in currentProject.Resources) if (res.IsDirty || res.DailyData == null || !res.DailyData.Any() || res.DailyData.Values.Any(d => d.Date == DateTime.MinValue)) res.InitializeFromSchedule(); currentProject.CalculateTotals(); CommissioningDataManager.Instance.SaveCurrentProject(); if (showUserMessage) { } } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error saving: {ex.Message}"); if (showUserMessage) MessageBox.Show($"Failed to save: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }


        // Add this method to CommissioningDataEntryForm
        private void ResetResource(CommissioningResource resource)
        {
            if (resource == null) return;

            // Confirm with the user
            DialogResult result = MessageBox.Show(
                $"Are you sure you want to reset all values for {resource.TechnicianName} to default?",
                "Confirm Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            // Use the helper method instead of direct Tag comparison
            TabPage tab = tabResources.TabPages.Cast<TabPage>().FirstOrDefault(t => GetResourceFromTabPage(t) == resource);
            if (tab == null) return;

            // Get the current rate sheet for defaults
            ComboBox comboRateSheet = FindControlInTab<ComboBox>(tab, "comboBoxRateSheet");
            string rateSheetName = null;
            if (comboRateSheet?.SelectedItem != null)
            {
                rateSheetName = comboRateSheet.SelectedItem.ToString();
            }

            RateSheet selectedRateSheet = null;
            if (!string.IsNullOrEmpty(rateSheetName))
            {
                selectedRateSheet = rateSheets.FirstOrDefault(rs => rs.Name == rateSheetName);
            }

            // Reset basic properties to defaults - these are directly applied to controls
            var nDS = FindControlInTab<NumericUpDown>(tab, "numDaysOnSite");
            if (nDS != null) nDS.Value = 1;

            var nHPD = FindControlInTab<NumericUpDown>(tab, "numHoursPerDay");
            if (nHPD != null) nHPD.Value = 8m;

            var dtp = FindControlInTab<DateTimePicker>(tab, "dtpResourceStartDate");
            if (dtp != null) dtp.Value = DateTime.Today.AddDays(1);

            var cbST = FindControlInTab<ComboBox>(tab, "comboBoxStartTime");
            if (cbST != null && cbST.Items.Count > 0)
            {
                // Find "7:00 AM" in the combo box items
                for (int i = 0; i < cbST.Items.Count; i++)
                {
                    if (cbST.Items[i].ToString().Contains("7:00"))
                    {
                        cbST.SelectedIndex = i;
                        break;
                    }
                }
            }

            var nLD = FindControlInTab<NumericUpDown>(tab, "numLunchDuration");
            if (nLD != null) nLD.Value = 0.5m;

            var numDiscount = FindControlInTab<NumericUpDown>(tab, "numDiscount");
            if (numDiscount != null) numDiscount.Value = 0;

            var chkEmergency = FindControlInTab<CheckBox>(tab, "chkEmergency");
            if (chkEmergency != null) chkEmergency.Checked = false;

            // Reset travel properties
            var cSTT = FindControlInTab<CheckBox>(tab, "chkSeparateTravelTo");
            if (cSTT != null) cSTT.Checked = false;

            var cSTF = FindControlInTab<CheckBox>(tab, "chkSeparateTravelFrom");
            if (cSTF != null) cSTF.Checked = false;

            var cbTM = FindControlInTab<ComboBox>(tab, "comboBoxTravelMethod");
            if (cbTM != null && cbTM.Items.Count > 0)
            {
                for (int i = 0; i < cbTM.Items.Count; i++)
                {
                    if (cbTM.Items[i].ToString() == "Driving")
                    {
                        cbTM.SelectedIndex = i;
                        break;
                    }
                }
            }

            var nTD = FindControlInTab<NumericUpDown>(tab, "numTravelDistance");
            if (nTD != null) nTD.Value = 0;

            var nTT = FindControlInTab<NumericUpDown>(tab, "numTravelTime");
            if (nTT != null) nTT.Value = 0;

            var nDTD = FindControlInTab<NumericUpDown>(tab, "numDailyTravelDistance");
            if (nDTD != null) nDTD.Value = 0;

            var nDTT = FindControlInTab<NumericUpDown>(tab, "numDailyTravelTime");
            if (nDTT != null) nDTT.Value = 0;

            // Reset expense properties
            var nFC = FindControlInTab<NumericUpDown>(tab, "numFlightCost");
            if (nFC != null) nFC.Value = selectedRateSheet?.FlightCost ?? 0;

            var cRCR = FindControlInTab<CheckBox>(tab, "chkRentalCarRequired");
            if (cRCR != null) cRCR.Checked = false;

            var nRCR = FindControlInTab<NumericUpDown>(tab, "numRentalCarRate");
            if (nRCR != null) nRCR.Value = selectedRateSheet?.RentalCarRate ?? 0;

            var cHR = FindControlInTab<CheckBox>(tab, "chkHotelRequired");
            if (cHR != null) cHR.Checked = false;

            var nHR = FindControlInTab<NumericUpDown>(tab, "numHotelRate");
            if (nHR != null) nHR.Value = selectedRateSheet?.HotelCost ?? 0;

            var nMR = FindControlInTab<NumericUpDown>(tab, "numMileageRate");
            if (nMR != null) nMR.Value = selectedRateSheet?.MileageRate ?? 0;

            var nPDR = FindControlInTab<NumericUpDown>(tab, "numPerDiemRate");
            if (nPDR != null) nPDR.Value = selectedRateSheet?.PerDiemRate ?? 0;

            var nOE = FindControlInTab<NumericUpDown>(tab, "numOtherExpenses");
            if (nOE != null) nOE.Value = 0;

            // Explicitly update the resource object from the UI controls
            UpdateResourceFromUI(resource);

            // Regenerate schedule with new values
            resource.InitializeFromSchedule();
            RegenerateSchedule(tab, resource);

            MessageBox.Show(
                $"Values for {resource.TechnicianName} have been reset to default.",
                "Reset Complete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // Add this method to add the reset button to each resource tab
        private void AddResetButton(TabPage tabPage, CommissioningResource resource)
        {
            // Find a good location - perhaps in the same area as the rate sheet controls
            GroupBox ratesGroup = FindControlInTab<GroupBox>(tabPage, "groupBoxRates");
            if (ratesGroup == null) return;

            Button btnViewResults = FindControlInTab<Button>(tabPage, "btnViewResults");
            if (btnViewResults == null)
            {
                // If not found by name, try to find it among the parent controls
                btnViewResults = this.Controls.OfType<Button>().FirstOrDefault(b => b.Text == "View Results");
            }

            // Create the button next to View Results
            Button btnReset = new Button
            {
                Name = "btnReset",
                Text = "Reset Values",
                Location = btnViewResults != null
                    ? new Point(btnViewResults.Right + 6, btnViewResults.Top)
                    : new Point(12, this.ClientSize.Height - 55),
                Size = new Size(100, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };



            // Add the click handler
            btnReset.Click += (sender, e) => ResetResource(resource);

            // Add to the tab
            tabPage.Controls.Add(btnReset);
        }




        private void SetupTravelTimeDistanceLink(TabPage tabPage, CommissioningResource resource)
        {
            // Get the numeric up down controls for this tab
            NumericUpDown numDailyTravelTime = FindControlInTab<NumericUpDown>(tabPage, "numDailyTravelTime");
            NumericUpDown numDailyTravelDistance = FindControlInTab<NumericUpDown>(tabPage, "numDailyTravelDistance");

            if (numDailyTravelTime == null || numDailyTravelDistance == null)
                return;

            // Create a unique flag for this tab to prevent recursive updates
            string flagKey = $"isUpdating_{tabPage.Name}";

            // Store the flag in the tab's Tag property as a dictionary
            if (tabPage.Tag == null || !(tabPage.Tag is Dictionary<string, object>))
            {
                var tagData = new Dictionary<string, object>();
                if (tabPage.Tag is CommissioningResource)
                {
                    tagData["resource"] = tabPage.Tag;
                }
                tagData[flagKey] = false;
                tabPage.Tag = tagData;
            }
            else if (tabPage.Tag is Dictionary<string, object> tagDict)
            {
                tagDict[flagKey] = false;
            }

            // Add event handler for travel time changes
            numDailyTravelTime.ValueChanged += (sender, e) =>
            {
                var tagDict = tabPage.Tag as Dictionary<string, object>;
                bool isUpdating = tagDict != null && tagDict.ContainsKey(flagKey) && (bool)tagDict[flagKey];

                if (!isUpdating)
                {
                    tagDict[flagKey] = true;
                    // Convert time to distance (0.5 hours = 30 miles, which is 60 miles per hour)
                    numDailyTravelDistance.Value = numDailyTravelTime.Value * 60;
                    tagDict[flagKey] = false;

                    // Update the resource and regenerate schedule
                    UpdateResourceFromUI(resource);
                    QueueScheduleUpdate(tabPage, resource);
                }
            };

            // Add event handler for travel distance changes
            numDailyTravelDistance.ValueChanged += (sender, e) =>
            {
                var tagDict = tabPage.Tag as Dictionary<string, object>;
                bool isUpdating = tagDict != null && tagDict.ContainsKey(flagKey) && (bool)tagDict[flagKey];

                if (!isUpdating)
                {
                    tagDict[flagKey] = true;
                    // Convert distance to time (30 miles = 0.5 hours)
                    numDailyTravelTime.Value = numDailyTravelDistance.Value / 60;
                    tagDict[flagKey] = false;

                    // Update the resource and regenerate schedule
                    UpdateResourceFromUI(resource);
                    QueueScheduleUpdate(tabPage, resource);
                }
            };
        }

        private void UpdateDayPanelsTheme()
        {
            // This method is called when dark mode is toggled
            // We need to update all existing day panels with the new theme colors

            foreach (TabPage tabPage in tabResources.TabPages)
            {
                CommissioningResource resource = GetResourceFromTabPage(tabPage);
                if (resource == null) continue;

                // Find the schedule panel
                var schedulePanel = FindControlInTab<Panel>(tabPage, "panelSchedule");
                if (schedulePanel == null) continue;

                // Update all day panels in this schedule
                UpdateAllDayPanelsInSchedule(schedulePanel, resource);
            }
        }


        private void BtnViewResults_Click(object sender, EventArgs e)
        {
            // Save current project data
            SaveProjectData(false);

            // Sync expense properties from UI to resource objects
            SyncExpenseProperties();

            // Verify we have a project
            if (currentProject == null)
            {
                MessageBox.Show(
                    "No project data.",
                    "No Project",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Create or update the results window
            if (_resultsWindow == null || _resultsWindow.IsDisposed)
            {
                // Create a new results window
                _resultsWindow = new CommissioningResultsWindow(currentProject);
                _resultsWindow.Show(this);
            }
            else
            {
                // Update the existing results window
                _resultsWindow.UpdateResults(currentProject);
                _resultsWindow.BringToFront();
            }
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            autoSaveTimer?.Stop();
            autoSaveTimer?.Dispose();
            scheduleUpdateTimer?.Stop();        // ADD THIS LINE
            scheduleUpdateTimer?.Dispose();     // ADD THIS LINE

            DialogResult c = MessageBox.Show("Save changes before closing?", "Confirm Close", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (c == DialogResult.Yes) SaveProjectData(true);
            else if (c == DialogResult.Cancel)
            {
                e.Cancel = true;
                autoSaveTimer?.Start();
                scheduleUpdateTimer?.Start();   // ADD THIS LINE
                return;
            }
            _resultsWindow?.Close();
            base.OnFormClosing(e);
        }

        private void ResetForm()
        {
            DialogResult result = MessageBox.Show(
                "Are you sure you want to reset all values to default?",
                "Confirm Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
                return;

            foreach (TabPage tab in tabResources.TabPages)
            {
                var resource = GetResourceFromTabPage(tab);
                if (resource == null) continue;

                // Reset all user-editable controls in the tab
                FindControlInTab<NumericUpDown>(tab, "numDaysOnSite")?.SetValue(c => c.Value = 1);
                FindControlInTab<NumericUpDown>(tab, "numHoursPerDay")?.SetValue(c => c.Value = 8);
                FindControlInTab<NumericUpDown>(tab, "numDiscount")?.SetValue(c => c.Value = 0);
                FindControlInTab<NumericUpDown>(tab, "numLunchDuration")?.SetValue(c => c.Value = 0.5m);
                FindControlInTab<NumericUpDown>(tab, "numTravelDistance")?.SetValue(c => c.Value = 0);
                FindControlInTab<NumericUpDown>(tab, "numTravelTime")?.SetValue(c => c.Value = 0);
                FindControlInTab<NumericUpDown>(tab, "numDailyTravelDistance")?.SetValue(c => c.Value = 0);
                FindControlInTab<NumericUpDown>(tab, "numDailyTravelTime")?.SetValue(c => c.Value = 0);
                FindControlInTab<NumericUpDown>(tab, "numFlightCost")?.SetValue(c => c.Value = resource.FlightCost);
                FindControlInTab<NumericUpDown>(tab, "numRentalCarRate")?.SetValue(c => c.Value = resource.RentalCarRate);
                FindControlInTab<NumericUpDown>(tab, "numHotelRate")?.SetValue(c => c.Value = resource.HotelRate);
                FindControlInTab<NumericUpDown>(tab, "numMileageRate")?.SetValue(c => c.Value = resource.MileageRate);
                FindControlInTab<NumericUpDown>(tab, "numPerDiemRate")?.SetValue(c => c.Value = resource.PerDiemRate);
                FindControlInTab<NumericUpDown>(tab, "numOtherExpenses")?.SetValue(c => c.Value = 0);

                FindControlInTab<CheckBox>(tab, "chkEmergency")?.SetValue(c => c.Checked = false);
                FindControlInTab<CheckBox>(tab, "chkSeparateTravelTo")?.SetValue(c => c.Checked = false);
                FindControlInTab<CheckBox>(tab, "chkSeparateTravelFrom")?.SetValue(c => c.Checked = false);
                FindControlInTab<CheckBox>(tab, "chkRentalCarRequired")?.SetValue(c => c.Checked = false);
                FindControlInTab<CheckBox>(tab, "chkHotelRequired")?.SetValue(c => c.Checked = false);

                // Set StartDate to next Monday from today
                var dtp = FindControlInTab<DateTimePicker>(tab, "dtpResourceStartDate");
                if (dtp != null)
                {
                    int daysUntilMonday = ((int)DayOfWeek.Monday - (int)DateTime.Today.DayOfWeek + 7) % 7;
                    dtp.Value = DateTime.Today.AddDays(daysUntilMonday == 0 ? 7 : daysUntilMonday);
                }

                // Reset ComboBoxes to first/default value
                var cbRateSheet = FindControlInTab<ComboBox>(tab, "comboBoxRateSheet");
                if (cbRateSheet != null && cbRateSheet.Items.Count > 0) cbRateSheet.SelectedIndex = 0;
                var cbStartTime = FindControlInTab<ComboBox>(tab, "comboBoxStartTime");
                if (cbStartTime != null && cbStartTime.Items.Count > 0) cbStartTime.SelectedIndex = 0;
                var cbTravelMethod = FindControlInTab<ComboBox>(tab, "comboBoxTravelMethod");
                if (cbTravelMethod != null && cbTravelMethod.Items.Count > 0) cbTravelMethod.SelectedIndex = 0;

                // Reset technician name if present
                var txtTechnician = FindControlInTab<TextBox>(tab, "txtTechnicianName");
                if (txtTechnician != null) txtTechnician.Text = "Technician";

                // Clear all manual day type overrides
                if (resource.DailyData != null)
                {
                    foreach (var day in resource.DailyData.Values)
                    {
                        day.ManualDayType = null;
                    }
                }

                // Update resource object from UI controls
                UpdateResourceFromUI(resource);
                // Regenerate schedule
                resource.InitializeFromSchedule();
                resource.IsDirty = false;
                RegenerateSchedule(tab, resource);
            }
            currentProject.IsDirty = true;
            MessageBox.Show("All values have been reset to default.", "Reset Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
        public static class ControlExtensions { public static void SetValue<T>(this T control, Action<T> action) where T : Control { if (control != null) action(control); } }
}

