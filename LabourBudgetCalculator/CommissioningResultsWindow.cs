using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Text;
using LabourBudgetCalculator.Helpers;

namespace LabourBudgetCalculator
{

   

    public partial class CommissioningResultsWindow : Form
    {
        // Reference to the project
        private CommissioningProject _project;

        // UI Components
        private TableLayoutPanel _summaryPanel;
        private Panel _resourcesContainer;
        private Timer _refreshTimer;
        private Timer _dataRefreshTimer;
        private Button _saveButton;
        private Button _exportButton;

        // Dictionary to keep track of editable cells
        private Dictionary<string, TextBox> _editableCells = new Dictionary<string, TextBox>();

        // Constructor
        public CommissioningResultsWindow(CommissioningProject project)
        {
            InitializeComponent();

            _project = project ?? throw new ArgumentNullException(nameof(project));

            // Configure form
            this.Text = $"Results - {_project.ProjectName}";
            this.Size = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Create UI
            SetupUI();

            // Set up auto-refresh timer
            _refreshTimer = new Timer
            {
                Interval = 2000, // Refresh every 2 seconds
                Enabled = true
            };
            _refreshTimer.Tick += (sender, e) => RefreshDisplay();

            // Set up data refresh timer
            _dataRefreshTimer = new Timer
            {
                Interval = 3000, // Check for updates every 3 seconds
                Enabled = true
            };
            _dataRefreshTimer.Tick += (sender, e) => ReloadProjectData();

            // Initial load
            RefreshDisplay();
        }
        private void ReloadProjectData()
        {
            try
            {
                // Only reload if we have a valid project
                if (_project != null && !string.IsNullOrEmpty(_project.ProjectID))
                {
                    // Get the current project from the data manager
                    var currentProject = CommissioningDataManager.Instance.CurrentProject;

                    // If the data manager has the same project ID and it exists
                    if (currentProject != null && currentProject.ProjectID == _project.ProjectID)
                    {
                        // Update our reference to use the current project from the manager
                        _project = currentProject;
                        _project.IsDirty = true; // Force a refresh
                        RefreshDisplay();
                    }
                    else
                    {
                        // Otherwise, try to load it from disk
                        var updatedProject = CommissioningDataManager.Instance.LoadProject(_project.ProjectID);
                        if (updatedProject != null)
                        {
                            _project = updatedProject;
                            _project.IsDirty = true; // Force a refresh
                            RefreshDisplay();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reloading project data: {ex.Message}");
            }
        }

        // Modify your existing OnFormClosing method
  
        private void CommissioningResultsWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Stop the timer when form closes
            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
                _refreshTimer.Dispose();
            }
        }

        private void SetupUI()
        {
            // Main container
            var mainContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                Padding = new Padding(10)
            };

            mainContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 80)); // Summary panel
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Resources panel
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Button panel

            // 1. Summary panel (Project totals)
            _summaryPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 5,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
                Margin = new Padding(0, 0, 0, 10)
            };

            // Equal width columns
            for (int i = 0; i < 5; i++)
            {
                _summaryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            }

            // Add headers
            AddHeaderCell(_summaryPanel, "Quoted", 0, 0);
            AddHeaderCell(_summaryPanel, "Planned", 0, 1);
            AddHeaderCell(_summaryPanel, "Current", 0, 2);
            AddHeaderCell(_summaryPanel, "Forecast", 0, 3);
            AddHeaderCell(_summaryPanel, "Delta", 0, 4);

            // Add value cells (will be populated in RefreshDisplay)
            for (int i = 0; i < 5; i++)
            {
                AddValueCell(_summaryPanel, "$0.00", 1, i);
            }

            // 2. Resources scroll panel
            _resourcesContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };

            // 3. Button panel
            var buttonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 10, 0, 0)
            };

            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));

            _saveButton = new Button
            {
                Text = "Save Changes",
                Dock = DockStyle.Fill,
                Margin = new Padding(5)
            };
            _saveButton.Click += SaveButton_Click;

            _exportButton = new Button
            {
                Text = "Export to Excel",
                Dock = DockStyle.Fill,
                Margin = new Padding(5)
            };
            _exportButton.Click += ExportButton_Click;

            var closeButton = new Button
            {
                Text = "Close",
                Dock = DockStyle.Fill,
                Margin = new Padding(5)
            };
            closeButton.Click += (sender, e) => this.Close();



            buttonPanel.Controls.Add(_saveButton, 0, 0);
            buttonPanel.Controls.Add(_exportButton, 1, 0);
            buttonPanel.Controls.Add(closeButton, 2, 0);

            // Add controls to main container
            mainContainer.Controls.Add(_summaryPanel, 0, 0);
            mainContainer.Controls.Add(_resourcesContainer, 0, 1);
            mainContainer.Controls.Add(buttonPanel, 0, 2);

            // Add main container to form
            this.Controls.Add(mainContainer);
        }

        private Label AddHeaderCell(TableLayoutPanel panel, string text, int row, int col)
        {
            var label = new Label
            {
                Text = text,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font(Font.FontFamily, 10, FontStyle.Bold),
                BackColor = Color.LightGray
            };

            panel.Controls.Add(label, col, row);
            return label;
        }

        private Label AddValueCell(TableLayoutPanel panel, string text, int row, int col, bool highlightNegative = false)
        {
            var label = new Label
            {
                Text = text,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font(Font.FontFamily, 10, FontStyle.Regular)
            };

            // Highlight negative values in red if requested
            if (highlightNegative && text.Contains("-"))
            {
                label.ForeColor = Color.Red;
            }

            panel.Controls.Add(label, col, row);
            return label;
        }

        // Add missing method for day data cells
        private void AddDayDataCells(TableLayoutPanel grid, CommissioningResource resource, int day, int dataType,
            int colOffset, int rowOffset, decimal plannedValue, decimal actualValue,
            Dictionary<string, string> cellValues, bool isMoney)
        {
            // Unique cell identifiers
            string plannedId = $"p_{resource.ResourceID}_{day}_{dataType}";
            string actualId = $"a_{resource.ResourceID}_{day}_{dataType}";

            // Format values appropriately
            string plannedText = isMoney ? $"${plannedValue:F2}" : $"{plannedValue:F1}";

            // Use stored value if it exists, otherwise use default
            string actualText;
            if (cellValues.ContainsKey(actualId))
            {
                actualText = cellValues[actualId];
            }
            else
            {
                actualText = isMoney ? $"${actualValue:F2}" : $"{actualValue:F1}";
            }

            // Calculate delta
            decimal delta = actualValue - plannedValue;
            string deltaText = isMoney ? $"${delta:F2}" : $"{delta:F1}";

            // Create planned cell (read-only)
            var plannedLabel = new Label
            {
                Text = plannedText,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font(Font.FontFamily, 8, FontStyle.Regular)
            };

            // Create actual cell (editable)
            var actualTextBox = new TextBox
            {
                Text = actualText.Replace("$", ""),
                TextAlign = HorizontalAlignment.Center,
                Dock = DockStyle.Fill,
                Font = new Font(Font.FontFamily, 8, FontStyle.Regular),
                Tag = new CellData { Resource = resource, Day = day, DataType = dataType }
            };
            actualTextBox.TextChanged += TextBox_TextChanged;
            actualTextBox.Leave += TextBox_Leave;

            // Add to tracked cells
            _editableCells[actualId] = actualTextBox;

            // Create delta cell (calculated)
            var deltaLabel = new Label
            {
                Text = deltaText,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font(Font.FontFamily, 8, FontStyle.Regular),
                ForeColor = delta > 0 ? Color.Red : SystemColors.ControlText
            };

            // Add cells to grid
            grid.Controls.Add(plannedLabel, colOffset, rowOffset);
            grid.Controls.Add(actualTextBox, colOffset + 1, rowOffset);
            grid.Controls.Add(deltaLabel, colOffset + 2, rowOffset);
        }

        // Add missing method for money cells
        private void AddMoneyCell(TableLayoutPanel grid, int col, int row, decimal value, bool highlightNegative)
        {
            var label = new Label
            {
                Text = $"${value:F2}",
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font(Font.FontFamily, 8, FontStyle.Regular)
            };

            if (highlightNegative && value < 0)
            {
                label.ForeColor = Color.Red;
            }

            grid.Controls.Add(label, col, row);
        }

       
        private void EnsureProjectData()
        {
            if (_project == null || _project.Resources == null)
                return;

            // Make sure all resources have daily data
            foreach (var resource in _project.Resources)
            {
                if (resource.DailyData == null || resource.DailyData.Count == 0)
                {
                    if (resource.DaysOnSite <= 0)
                        resource.DaysOnSite = 5; // Default to 5 days

                    resource.InitializeFromSchedule();
                }
            }
        }


        private void RefreshDisplay()
        {
            System.Diagnostics.Debug.WriteLine($"RefreshDisplay called - Project: {_project?.ProjectName}, Resources: {_project?.Resources?.Count}");
            if (_project?.Resources != null)
            {
                foreach (var resource in _project.Resources)
                {
                    System.Diagnostics.Debug.WriteLine($"Resource: {resource.TechnicianName}, DailyData count: {resource.DailyData?.Count}");
                }
            }

            EnsureProjectData();


            // Recalculate project totals

            if (_project.IsDirty)
            {
                _project.CalculateTotals();
            }

            // Update summary panel
            UpdateSummaryPanel();

            // Update resource grid
            UpdateResourceGrid();
        }

        private void UpdateSummaryPanel()
        {
            // Clear any existing value cells
            for (int i = 0; i < 5; i++)
            {
                if (_summaryPanel.GetControlFromPosition(i, 1) is Label label)
                {
                    // Quoted
                    if (i == 0)
                    {
                        label.Text = $"${_project.InitialEstimate:N2}";
                    }
                    // Planned
                    else if (i == 1)
                    {
                        label.Text = $"${_project.PlannedTotal:N2}";
                    }
                    // Current
                    else if (i == 2)
                    {
                        label.Text = $"${_project.CurrentTotal:N2}";
                    }
                    // Forecast
                    else if (i == 3)
                    {
                        label.Text = $"${_project.ForecastTotal:N2}";

                        // Red if over quoted
                        if (_project.ForecastTotal > _project.InitialEstimate)
                        {
                            label.ForeColor = Color.Red;
                        }
                        else
                        {
                            label.ForeColor = SystemColors.ControlText;
                        }
                    }
                    // Delta (Forecast - Quoted)
                    else if (i == 4)
                    {
                        decimal delta = _project.ForecastTotal - _project.InitialEstimate;
                        label.Text = $"${delta:N2}";

                        // Red if negative
                        if (delta > 0)
                        {
                            label.ForeColor = Color.Red;
                        }
                        else
                        {
                            label.ForeColor = SystemColors.ControlText;
                        }
                    }
                }
            }
        }

        private void UpdateResourceGrid()
        {

            if (_project == null || _project.Resources == null || _project.Resources.Count == 0)
            {
                _resourcesContainer.Controls.Clear();

                var noDataLabel = new Label
                {
                    Text = "No resources to display. Add resources in the data entry form.",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font(Font.FontFamily, 10)
                };

                _resourcesContainer.Controls.Add(noDataLabel);
                return;
            }
            // Store scroll position to restore after rebuild
            Point scrollPosition = _resourcesContainer.AutoScrollPosition;
            scrollPosition = new Point(Math.Abs(scrollPosition.X), Math.Abs(scrollPosition.Y));

            // Suspend layout to avoid flickering
            _resourcesContainer.SuspendLayout();

            // Preserve existing cell values before clearing
            Dictionary<string, string> cellValues = new Dictionary<string, string>();
            foreach (var key in _editableCells.Keys)
            {
                if (_editableCells[key].Modified)
                {
                    cellValues[key] = _editableCells[key].Text;
                }
            }

            // Clear current controls
            _resourcesContainer.Controls.Clear();
            _editableCells.Clear();

            // Create new resource panels
            var resourcesPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                Padding = new Padding(0)
            };

            // Add each resource
            foreach (var resource in _project.Resources)
                try
                {
                    var resourcePanel = CreateResourcePanel(resource, cellValues);
                    resourcesPanel.Controls.Add(resourcePanel);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error creating resource panel: {ex.Message}");
                }

            // Add the flow panel to the container
            _resourcesContainer.Controls.Add(resourcesPanel);

            // Resume layout and restore scroll position
            _resourcesContainer.ResumeLayout();
            _resourcesContainer.AutoScrollPosition = scrollPosition;
        }

        private Panel CreateResourcePanel(CommissioningResource resource, Dictionary<string, string> cellValues)
        {
            var panel = new Panel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 20),
                Width = _resourcesContainer.Width - 25 // Allow for scrollbar
            };

            // Resource header
            var headerPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 30,
                ColumnCount = 1,
                RowCount = 1,
                BackColor = Color.DarkGray,
                ForeColor = Color.White,
                Margin = new Padding(0, 0, 0, 5)
            };

            headerPanel.Controls.Add(new Label
            {
                Text = $"Resource: {resource.TechnicianName}",
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Font = new Font(Font.FontFamily, 10, FontStyle.Bold),
                Padding = new Padding(10, 0, 0, 0)
            }, 0, 0);

            panel.Controls.Add(headerPanel);

            // Resource data grid
            var grid = CreateResourceGrid(resource, cellValues);
            grid.Dock = DockStyle.Top;
            grid.Location = new Point(0, headerPanel.Height + 5);

            panel.Controls.Add(grid);

            return panel;
        }

        private TableLayoutPanel CreateResourceGrid(CommissioningResource resource, Dictionary<string, string> cellValues)
        {
            System.Diagnostics.Debug.WriteLine($"Creating grid for {resource.TechnicianName}, DailyData count: {resource.DailyData?.Count}");

            // Find the min and max day indices to display
            int minDay = resource.DailyData.Keys.Count > 0 ? resource.DailyData.Keys.Min() : 0;
            int maxDay = resource.DailyData.Keys.Count > 0 ? resource.DailyData.Keys.Max() : 6;

            // Calculate grid dimensions
            int columnCount = 4 + ((maxDay - minDay + 1) * 3); // 4 label columns + (days * 3)
            int rowCount = 16; // Header + 11 data rows + 2 subtotal rows + 2 header rows

            // Create the grid
            var grid = new TableLayoutPanel
            {
                RowCount = rowCount,
                ColumnCount = columnCount,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0)
            };

            // Configure columns
            int[] fixedColumnWidths = { 120, 80, 80, 80 }; // Category, Planned, Actual, Delta
            int dayBoxWidth = 120; // Width for each day's column group

            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, fixedColumnWidths[0])); // Category

            // Add date header area (spanning columns)
            for (int day = minDay; day <= maxDay; day++)
            {
                int dateColIndex = 1 + (day - minDay) * 3;

                // Add three columns for each day (Planned, Actual, Delta)
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, dayBoxWidth / 3));
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, dayBoxWidth / 3));
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, dayBoxWidth / 3));

                // Get the date for this day
                DateTime date = resource.StartDate.AddDays(day);
                string dateText = date.ToString("MMM dd - ddd");

                // Date header spanning 3 columns (Planned, Actual, Delta)
                var dateLabel = new Label
                {
                    Text = dateText,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    Font = new Font(Font.FontFamily, 9, FontStyle.Bold),
                    BackColor = Color.LightGray
                };

                grid.Controls.Add(dateLabel, dateColIndex, 0);
                grid.SetColumnSpan(dateLabel, 3);

                // Sub-headers for Planned, Actual, Delta
                string[] subHeaders = { "Planned", "Actual", "Delta" };
                for (int i = 0; i < 3; i++)
                {
                    var subHeaderLabel = new Label
                    {
                        Text = subHeaders[i],
                        TextAlign = ContentAlignment.MiddleCenter,
                        Dock = DockStyle.Fill,
                        Font = new Font(Font.FontFamily, 8, FontStyle.Regular),
                        BackColor = Color.LightGray
                    };

                    grid.Controls.Add(subHeaderLabel, dateColIndex + i, 1);
                }
            }

            // Add totals columns at the end
            int totalsColStart = 1 + (maxDay - minDay + 1) * 3;
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, fixedColumnWidths[1])); // Planned Total
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, fixedColumnWidths[2])); // Actual Total
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, fixedColumnWidths[3])); // Delta Total

            // Empty cell for the category header
            grid.Controls.Add(new Label { Text = "", BackColor = Color.LightGray }, 0, 0);
            grid.SetRowSpan(grid.GetControlFromPosition(0, 0), 2);

            // Totals headers
            var totalsHeaderLabel = new Label
            {
                Text = "Totals",
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font(Font.FontFamily, 9, FontStyle.Bold),
                BackColor = Color.LightGray
            };

            grid.Controls.Add(totalsHeaderLabel, totalsColStart, 0);
            grid.SetColumnSpan(totalsHeaderLabel, 3);

            // Sub-headers for Totals
            string[] totalSubHeaders = { "Planned", "Actual", "Delta" };
            for (int i = 0; i < 3; i++)
            {
                var subHeaderLabel = new Label
                {
                    Text = totalSubHeaders[i],
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    Font = new Font(Font.FontFamily, 8, FontStyle.Regular),
                    BackColor = Color.LightGray
                };

                grid.Controls.Add(subHeaderLabel, totalsColStart + i, 1);
            }

            // Row headers
            string[] rowHeaders = {
                "Service (Reg)",
                "Service (OT)",
                "Service (Premium)",
                "Travel (Reg)",
                "Travel (OT)",
                "Travel (Premium)",
                "Subtotals - Charges",
                "Mileage",
                "Per Diem",
                "Flight",
                "Car Rental",
                "Hotel",
                "Subtotals - Expenses",
                "Totals"
            };

            for (int i = 0; i < rowHeaders.Length; i++)
            {
                var rowLabel = new Label
                {
                    Text = rowHeaders[i],
                    TextAlign = ContentAlignment.MiddleLeft,
                    Dock = DockStyle.Fill,
                    Font = new Font(Font.FontFamily, 8, i == 6 || i == 12 || i == 13 ? FontStyle.Bold : FontStyle.Regular),
                    Padding = new Padding(5, 0, 0, 0),
                    BackColor = i == 6 || i == 12 || i == 13 ? Color.LightGray : SystemColors.Control
                };

                grid.Controls.Add(rowLabel, 0, i + 2);
            }

            // Populate day columns with data
            for (int day = minDay; day <= maxDay; day++)
            {
                var dayData = resource.DailyData.ContainsKey(day) ? resource.DailyData[day] : new ResourceDayData();
                int colOffset = 1 + (day - minDay) * 3;

                // Labor hours rows
                AddDayDataCells(grid, resource, day, 0, colOffset, 2,
                    dayData.PlannedRegularLabourHours,
                    dayData.ActualRegularLabourHours,
                    cellValues, false);

                AddDayDataCells(grid, resource, day, 1, colOffset, 3,
                    dayData.PlannedOvertimeLabourHours,
                    dayData.ActualOvertimeLabourHours,
                    cellValues, false);

                AddDayDataCells(grid, resource, day, 2, colOffset, 4,
                    dayData.PlannedPremiumLabourHours,
                    dayData.ActualPremiumLabourHours,
                    cellValues, false);

                // Travel hours rows
                AddDayDataCells(grid, resource, day, 3, colOffset, 5,
                    dayData.PlannedRegularTravelHours,
                    dayData.ActualRegularTravelHours,
                    cellValues, false);

                AddDayDataCells(grid, resource, day, 4, colOffset, 6,
                    dayData.PlannedOvertimeTravelHours,
                    dayData.ActualOvertimeTravelHours,
                    cellValues, false);

                AddDayDataCells(grid, resource, day, 5, colOffset, 7,
                    dayData.PlannedPremiumTravelHours,
                    dayData.ActualPremiumTravelHours,
                    cellValues, false);

                // Calculate subtotal for this day's service and travel charges
                decimal plannedServiceTravel =
                    (dayData.PlannedRegularLabourHours * resource.RegularLabourRate) +
                    (dayData.PlannedOvertimeLabourHours * resource.OvertimeLabourRate) +
                    (dayData.PlannedPremiumLabourHours * resource.PremiumLabourRate) +
                    (dayData.PlannedRegularTravelHours * resource.RegularTravelRate) +
                    (dayData.PlannedOvertimeTravelHours * resource.OvertimeTravelRate) +
                    (dayData.PlannedPremiumTravelHours * resource.PremiumTravelRate);

                decimal actualServiceTravel =
                    (dayData.ActualRegularLabourHours * resource.RegularLabourRate) +
                    (dayData.ActualOvertimeLabourHours * resource.OvertimeLabourRate) +
                    (dayData.ActualPremiumLabourHours * resource.PremiumLabourRate) +
                    (dayData.ActualRegularTravelHours * resource.RegularTravelRate) +
                    (dayData.ActualOvertimeTravelHours * resource.OvertimeTravelRate) +
                    (dayData.ActualPremiumTravelHours * resource.PremiumTravelRate);

                decimal deltaServiceTravel = actualServiceTravel - plannedServiceTravel;

                // Add service/travel subtotal row
                AddMoneyCell(grid, colOffset, 8, plannedServiceTravel, false);
                AddMoneyCell(grid, colOffset + 1, 8, actualServiceTravel, false);
                AddMoneyCell(grid, colOffset + 2, 8, deltaServiceTravel, true);

                // Expense rows
                AddDayDataCells(grid, resource, day, 6, colOffset, 9,
                    dayData.PlannedMileageCost,
                    dayData.ActualMileageCost,
                    cellValues, true);

                AddDayDataCells(grid, resource, day, 7, colOffset, 10,
                    dayData.PlannedPerDiemCost,
                    dayData.ActualPerDiemCost,
                    cellValues, true);

                AddDayDataCells(grid, resource, day, 8, colOffset, 11,
                    dayData.PlannedFlightCost,
                    dayData.ActualFlightCost,
                    cellValues, true);

                AddDayDataCells(grid, resource, day, 9, colOffset, 12,
                    dayData.PlannedRentalCarCost,
                    dayData.ActualRentalCarCost,
                    cellValues, true);

                AddDayDataCells(grid, resource, day, 10, colOffset, 13,
                    dayData.PlannedHotelCost,
                    dayData.ActualHotelCost,
                    cellValues, true);

                // Calculate subtotal for this day's expenses
                decimal plannedExpenses =
                    dayData.PlannedMileageCost +
                    dayData.PlannedPerDiemCost +
                    dayData.PlannedFlightCost +
                    dayData.PlannedRentalCarCost +
                    dayData.PlannedHotelCost;

                decimal actualExpenses =
                    dayData.ActualMileageCost +
                    dayData.ActualPerDiemCost +
                    dayData.ActualFlightCost +
                    dayData.ActualRentalCarCost +
                    dayData.ActualHotelCost;

                decimal deltaExpenses = actualExpenses - plannedExpenses;

                // Add expenses subtotal row
                AddMoneyCell(grid, colOffset, 14, plannedExpenses, false);
                AddMoneyCell(grid, colOffset + 1, 14, actualExpenses, false);
                AddMoneyCell(grid, colOffset + 2, 14, deltaExpenses, true);

                // Day total
                decimal plannedTotal = plannedServiceTravel + plannedExpenses;
                decimal actualTotal = actualServiceTravel + actualExpenses;
                decimal deltaTotal = actualTotal - plannedTotal;

                // Add day total row
                AddMoneyCell(grid, colOffset, 15, plannedTotal, false);
                AddMoneyCell(grid, colOffset + 1, 15, actualTotal, false);
                AddMoneyCell(grid, colOffset + 2, 15, deltaTotal, true);
            }

            // Add resource totals columns
            CalculateAndAddResourceTotals(grid, resource, minDay, maxDay, totalsColStart);

            return grid;
        }

        private void CalculateAndAddResourceTotals(TableLayoutPanel grid, CommissioningResource resource,
            int minDay, int maxDay, int totalCol)
        {
            // Calculate resource totals
            decimal plannedRegularLabourHours = 0;
            decimal plannedOvertimeLabourHours = 0;
            decimal plannedPremiumLabourHours = 0;
            decimal plannedRegularTravelHours = 0;
            decimal plannedOvertimeTravelHours = 0;
            decimal plannedPremiumTravelHours = 0;

            decimal actualRegularLabourHours = 0;
            decimal actualOvertimeLabourHours = 0;
            decimal actualPremiumLabourHours = 0;
            decimal actualRegularTravelHours = 0;
            decimal actualOvertimeTravelHours = 0;
            decimal actualPremiumTravelHours = 0;

            decimal plannedMileageCost = 0;
            decimal plannedPerDiemCost = 0;
            decimal plannedFlightCost = 0;
            decimal plannedRentalCarCost = 0;
            decimal plannedHotelCost = 0;

            decimal actualMileageCost = 0;
            decimal actualPerDiemCost = 0;
            decimal actualFlightCost = 0;
            decimal actualRentalCarCost = 0;
            decimal actualHotelCost = 0;

            foreach (var entry in resource.DailyData)
            {
                var dayData = entry.Value;

                // Labor hours
                plannedRegularLabourHours += dayData.PlannedRegularLabourHours;
                plannedOvertimeLabourHours += dayData.PlannedOvertimeLabourHours;
                plannedPremiumLabourHours += dayData.PlannedPremiumLabourHours;

                actualRegularLabourHours += dayData.ActualRegularLabourHours;
                actualOvertimeLabourHours += dayData.ActualOvertimeLabourHours;
                actualPremiumLabourHours += dayData.ActualPremiumLabourHours;

                // Travel hours
                plannedRegularTravelHours += dayData.PlannedRegularTravelHours;
                plannedOvertimeTravelHours += dayData.PlannedOvertimeTravelHours;
                plannedPremiumTravelHours += dayData.PlannedPremiumTravelHours;

                actualRegularTravelHours += dayData.ActualRegularTravelHours;
                actualOvertimeTravelHours += dayData.ActualOvertimeTravelHours;
                actualPremiumTravelHours += dayData.ActualPremiumTravelHours;

                // Expenses
                plannedMileageCost += dayData.PlannedMileageCost;
                plannedPerDiemCost += dayData.PlannedPerDiemCost;
                plannedFlightCost += dayData.PlannedFlightCost;
                plannedRentalCarCost += dayData.PlannedRentalCarCost;
                plannedHotelCost += dayData.PlannedHotelCost;

                actualMileageCost += dayData.ActualMileageCost;
                actualPerDiemCost += dayData.ActualPerDiemCost;
                actualFlightCost += dayData.ActualFlightCost;
                actualRentalCarCost += dayData.ActualRentalCarCost;
                actualHotelCost += dayData.ActualHotelCost;
            }

            // Add totals to grid
            // Labor hours
            AddValueCell(grid, plannedRegularLabourHours.ToString("F1"), totalCol, 2);
            AddValueCell(grid, actualRegularLabourHours.ToString("F1"), totalCol + 1, 2);
            AddValueCell(grid, (actualRegularLabourHours - plannedRegularLabourHours).ToString("F1"), totalCol + 2, 2);

            AddValueCell(grid, plannedOvertimeLabourHours.ToString("F1"), totalCol, 3);
            AddValueCell(grid, actualOvertimeLabourHours.ToString("F1"), totalCol + 1, 3);
            AddValueCell(grid, (actualOvertimeLabourHours - plannedOvertimeLabourHours).ToString("F1"), totalCol + 2, 3);

            AddValueCell(grid, plannedPremiumLabourHours.ToString("F1"), totalCol, 4);
            AddValueCell(grid, actualPremiumLabourHours.ToString("F1"), totalCol + 1, 4);
            AddValueCell(grid, (actualPremiumLabourHours - plannedPremiumLabourHours).ToString("F1"), totalCol + 2, 4);

            // Travel hours
            AddValueCell(grid, plannedRegularTravelHours.ToString("F1"), totalCol, 5);
            AddValueCell(grid, actualRegularTravelHours.ToString("F1"), totalCol + 1, 5);
            AddValueCell(grid, (actualRegularTravelHours - plannedRegularTravelHours).ToString("F1"), totalCol + 2, 5);

            AddValueCell(grid, plannedOvertimeTravelHours.ToString("F1"), totalCol, 6);
            AddValueCell(grid, actualOvertimeTravelHours.ToString("F1"), totalCol + 1, 6);
            AddValueCell(grid, (actualOvertimeTravelHours - plannedOvertimeTravelHours).ToString("F1"), totalCol + 2, 6);

            AddValueCell(grid, plannedPremiumTravelHours.ToString("F1"), totalCol, 7);
            AddValueCell(grid, actualPremiumTravelHours.ToString("F1"), totalCol + 1, 7);
            AddValueCell(grid, (actualPremiumTravelHours - plannedPremiumTravelHours).ToString("F1"), totalCol + 2, 7);

            // Calculate service/travel subtotals
            decimal plannedServiceTravel =
                (plannedRegularLabourHours * resource.RegularLabourRate) +
                (plannedOvertimeLabourHours * resource.OvertimeLabourRate) +
                (plannedPremiumLabourHours * resource.PremiumLabourRate) +
                (plannedRegularTravelHours * resource.RegularTravelRate) +
                (plannedOvertimeTravelHours * resource.OvertimeTravelRate) +
                (plannedPremiumTravelHours * resource.PremiumTravelRate);

            decimal actualServiceTravel =
                (actualRegularLabourHours * resource.RegularLabourRate) +
                (actualOvertimeLabourHours * resource.OvertimeLabourRate) +
                (actualPremiumLabourHours * resource.PremiumLabourRate) +
                (actualRegularTravelHours * resource.RegularTravelRate) +
                (actualOvertimeTravelHours * resource.OvertimeTravelRate) +
                (actualPremiumTravelHours * resource.PremiumTravelRate);

            decimal deltaServiceTravel = actualServiceTravel - plannedServiceTravel;

            // Add service/travel subtotal row
            AddMoneyCell(grid, totalCol, 8, plannedServiceTravel, false);
            AddMoneyCell(grid, totalCol + 1, 8, actualServiceTravel, false);
            AddMoneyCell(grid, totalCol + 2, 8, deltaServiceTravel, true);

            // Expenses
            AddValueCell(grid, $"${plannedMileageCost:F2}", totalCol, 9);
            AddValueCell(grid, $"${actualMileageCost:F2}", totalCol + 1, 9);
            AddValueCell(grid, $"${actualMileageCost - plannedMileageCost:F2}", totalCol + 2, 9);

            AddValueCell(grid, $"${plannedPerDiemCost:F2}", totalCol, 10);
            AddValueCell(grid, $"${actualPerDiemCost:F2}", totalCol + 1, 10);
            AddValueCell(grid, $"${actualPerDiemCost - plannedPerDiemCost:F2}", totalCol + 2, 10);

            AddValueCell(grid, $"${plannedFlightCost:F2}", totalCol, 11);
            AddValueCell(grid, $"${actualFlightCost:F2}", totalCol + 1, 11);
            AddValueCell(grid, $"${actualFlightCost - plannedFlightCost:F2}", totalCol + 2, 11);

            AddValueCell(grid, $"${plannedRentalCarCost:F2}", totalCol, 12);
            AddValueCell(grid, $"${actualRentalCarCost:F2}", totalCol + 1, 12);
            AddValueCell(grid, $"${actualRentalCarCost - plannedRentalCarCost:F2}", totalCol + 2, 12);

            AddValueCell(grid, $"${plannedHotelCost:F2}", totalCol, 13);
            AddValueCell(grid, $"${actualHotelCost:F2}", totalCol + 1, 13);
            AddValueCell(grid, $"${actualHotelCost - plannedHotelCost:F2}", totalCol + 2, 13);

            // Calculate expenses subtotal
            decimal plannedExpenses =
                plannedMileageCost +
                plannedPerDiemCost +
                plannedFlightCost +
                plannedRentalCarCost +
                plannedHotelCost;

            decimal actualExpenses =
                actualMileageCost +
                actualPerDiemCost +
                actualFlightCost +
                actualRentalCarCost +
                actualHotelCost;

            decimal deltaExpenses = actualExpenses - plannedExpenses;

            // Add expenses subtotal row
            AddMoneyCell(grid, totalCol, 14, plannedExpenses, false);
            AddMoneyCell(grid, totalCol + 1, 14, actualExpenses, false);
            AddMoneyCell(grid, totalCol + 2, 14, deltaExpenses, true);

            // Calculate totals
            decimal plannedTotal = plannedServiceTravel + plannedExpenses;
            decimal actualTotal = actualServiceTravel + actualExpenses;
            decimal deltaTotal = actualTotal - plannedTotal;

            // Add totals row
            AddMoneyCell(grid, totalCol, 15, plannedTotal, false);
            AddMoneyCell(grid, totalCol + 1, 15, actualTotal, false);
            AddMoneyCell(grid, totalCol + 2, 15, deltaTotal, true);
        }

        // Helper class to track cell data
        private class CellData
        {
            public CommissioningResource Resource { get; set; }
            public int Day { get; set; }
            public int DataType { get; set; }
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            // Validate input
            TextBox textBox = sender as TextBox;
            if (textBox == null) return;

            // Allow only numeric input
            string text = textBox.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            // Regular expression for decimal number
            if (!System.Text.RegularExpressions.Regex.IsMatch(text, @"^\$?[0-9]*\.?[0-9]*$"))
            {
                // Invalid input - revert to last valid
                textBox.Text = text.Substring(0, text.Length - 1);
                textBox.SelectionStart = textBox.Text.Length;
            }
        }

        private void TextBox_Leave(object sender, EventArgs e)
        {
            // Update model when focus leaves cell
            TextBox textBox = sender as TextBox;
            if (textBox == null || !(textBox.Tag is CellData cellData)) return;

            var resource = cellData.Resource;
            int day = cellData.Day;
            int dataType = cellData.DataType;

            if (!resource.DailyData.ContainsKey(day))
            {
                resource.DailyData[day] = new ResourceDayData();
            }

            var dayData = resource.DailyData[day];

            // Parse value
            decimal value = 0;
            if (decimal.TryParse(textBox.Text.Replace("$", ""), out decimal parsedValue))
            {
                value = parsedValue;
            }

            // Update appropriate field based on data type
            switch (dataType)
            {
                case 0: // Regular Labor Hours
                    dayData.ActualRegularLabourHours = value;
                    break;
                case 1: // Overtime Labor Hours
                    dayData.ActualOvertimeLabourHours = value;
                    break;
                case 2: // Premium Labor Hours
                    dayData.ActualPremiumLabourHours = value;
                    break;
                case 3: // Regular Travel Hours
                    dayData.ActualRegularTravelHours = value;
                    break;
                case 4: // Overtime Travel Hours
                    dayData.ActualOvertimeTravelHours = value;
                    break;
                case 5: // Premium Travel Hours
                    dayData.ActualPremiumTravelHours = value;
                    break;
                case 6: // Mileage Cost
                    dayData.ActualMileageCost = value;
                    break;
                case 7: // Per Diem Cost
                    dayData.ActualPerDiemCost = value;
                    break;
                case 8: // Flight Cost
                    dayData.ActualFlightCost = value;
                    break;
                case 9: // Rental Car Cost
                    dayData.ActualRentalCarCost = value;
                    break;
                case 10: // Hotel Cost
                    dayData.ActualHotelCost = value;
                    break;
            }

            // Mark as dirty to recalculate totals
            resource.IsDirty = true;
            _project.IsDirty = true;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            // Apply all pending changes
            foreach (TextBox textBox in _editableCells.Values)
            {
                // Simulate leaving the cell to apply changes
                TextBox_Leave(textBox, EventArgs.Empty);
            }

            // Save the project
            CommissioningDataManager.Instance.SaveCurrentProject();

            MessageBox.Show("Changes saved successfully.", "Save Complete",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            // Create export dialog
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                DefaultExt = "csv",
                Title = "Export Results"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // Export to CSV
                    ExportToCSV(saveDialog.FileName);

                    MessageBox.Show("Export completed successfully.", "Export Complete",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting results: {ex.Message}", "Export Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExportToCSV(string fileName)
        {
            using (StreamWriter writer = new StreamWriter(fileName))
            {
                // Write header
                writer.WriteLine("Commissioning Project Results");
                writer.WriteLine($"Project: {_project.ProjectName}");
                writer.WriteLine($"Date: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                writer.WriteLine();

                // Write summary
                writer.WriteLine("Project Summary");
                writer.WriteLine("Quoted,Planned,Current,Forecast,Delta");
                writer.WriteLine($"{_project.InitialEstimate:F2},{_project.PlannedTotal:F2},{_project.CurrentTotal:F2},{_project.ForecastTotal:F2},{(_project.ForecastTotal - _project.InitialEstimate):F2}");
                writer.WriteLine();

                // Write each resource
                foreach (var resource in _project.Resources)
                {
                    writer.WriteLine($"Resource: {resource.TechnicianName}");

                    // Find the min and max day indices
                    int minDay = resource.DailyData.Keys.Count > 0 ? resource.DailyData.Keys.Min() : 0;
                    int maxDay = resource.DailyData.Keys.Count > 0 ? resource.DailyData.Keys.Max() : 6;

                    // Write day headers
                    writer.Write("Category,");
                    for (int day = minDay; day <= maxDay; day++)
                    {
                        DateTime date = resource.StartDate.AddDays(day);
                        writer.Write($"{date.ToString("MMM dd")}_Planned,{date.ToString("MMM dd")}_Actual,{date.ToString("MMM dd")}_Delta,");
                    }
                    writer.WriteLine("Total_Planned,Total_Actual,Total_Delta");

                    // Write labor hours
                    WriteCSVRow(writer, "Service (Reg)", resource, minDay, maxDay,
                        (d) => d.PlannedRegularLabourHours,
                        (d) => d.ActualRegularLabourHours, false);

                    WriteCSVRow(writer, "Service (OT)", resource, minDay, maxDay,
                        (d) => d.PlannedOvertimeLabourHours,
                        (d) => d.ActualOvertimeLabourHours, false);

                    WriteCSVRow(writer, "Service (Premium)", resource, minDay, maxDay,
                        (d) => d.PlannedPremiumLabourHours,
                        (d) => d.ActualPremiumLabourHours, false);

                    WriteCSVRow(writer, "Travel (Reg)", resource, minDay, maxDay,
                        (d) => d.PlannedRegularTravelHours,
                        (d) => d.ActualRegularTravelHours, false);

                    WriteCSVRow(writer, "Travel (OT)", resource, minDay, maxDay,
                        (d) => d.PlannedOvertimeTravelHours,
                        (d) => d.ActualOvertimeTravelHours, false);

                    WriteCSVRow(writer, "Travel (Premium)", resource, minDay, maxDay,
                        (d) => d.PlannedPremiumTravelHours,
                        (d) => d.ActualPremiumTravelHours, false);

                    // Write service/travel subtotal
                    WriteServiceTravelSubtotal(writer, resource, minDay, maxDay);

                    // Write expenses
                    WriteCSVRow(writer, "Mileage", resource, minDay, maxDay,
                        (d) => d.PlannedMileageCost,
                        (d) => d.ActualMileageCost, true);

                    WriteCSVRow(writer, "Per Diem", resource, minDay, maxDay,
                        (d) => d.PlannedPerDiemCost,
                        (d) => d.ActualPerDiemCost, true);

                    WriteCSVRow(writer, "Flight", resource, minDay, maxDay,
                        (d) => d.PlannedFlightCost,
                        (d) => d.ActualFlightCost, true);

                    WriteCSVRow(writer, "Car Rental", resource, minDay, maxDay,
                        (d) => d.PlannedRentalCarCost,
                        (d) => d.ActualRentalCarCost, true);

                    WriteCSVRow(writer, "Hotel", resource, minDay, maxDay,
                        (d) => d.PlannedHotelCost,
                        (d) => d.ActualHotelCost, true);

                    // Write expenses subtotal
                    WriteExpensesSubtotal(writer, resource, minDay, maxDay);

                    // Write totals
                    WriteTotals(writer, resource, minDay, maxDay);

                    writer.WriteLine();
                }
            }
        }

        private void WriteCSVRow(StreamWriter writer, string rowName, CommissioningResource resource,
            int minDay, int maxDay, Func<ResourceDayData, decimal> plannedSelector,
            Func<ResourceDayData, decimal> actualSelector, bool isMoney)
        {
            writer.Write($"{rowName},");

            decimal plannedTotal = 0;
            decimal actualTotal = 0;

            for (int day = minDay; day <= maxDay; day++)
            {
                var dayData = resource.DailyData.ContainsKey(day) ? resource.DailyData[day] : new ResourceDayData();

                decimal planned = plannedSelector(dayData);
                decimal actual = actualSelector(dayData);
                decimal delta = actual - planned;

                plannedTotal += planned;
                actualTotal += actual;

                if (isMoney)
                {
                    writer.Write($"${planned:F2},${actual:F2},${delta:F2},");
                }
                else
                {
                    writer.Write($"{planned:F1},{actual:F1},{delta:F1},");
                }
            }

            decimal totalDelta = actualTotal - plannedTotal;

            if (isMoney)
            {
                writer.WriteLine($"${plannedTotal:F2},${actualTotal:F2},${totalDelta:F2}");
            }
            else
            {
                writer.WriteLine($"{plannedTotal:F1},{actualTotal:F1},{totalDelta:F1}");
            }
        }

        private void WriteServiceTravelSubtotal(StreamWriter writer, CommissioningResource resource, int minDay, int maxDay)
        {
            writer.Write("Subtotals - Charges,");

            decimal plannedTotal = 0;
            decimal actualTotal = 0;

            for (int day = minDay; day <= maxDay; day++)
            {
                var dayData = resource.DailyData.ContainsKey(day) ? resource.DailyData[day] : new ResourceDayData();

                decimal planned =
                    (dayData.PlannedRegularLabourHours * resource.RegularLabourRate) +
                    (dayData.PlannedOvertimeLabourHours * resource.OvertimeLabourRate) +
                    (dayData.PlannedPremiumLabourHours * resource.PremiumLabourRate) +
                    (dayData.PlannedRegularTravelHours * resource.RegularTravelRate) +
                    (dayData.PlannedOvertimeTravelHours * resource.OvertimeTravelRate) +
                    (dayData.PlannedPremiumTravelHours * resource.PremiumTravelRate);

                decimal actual =
                    (dayData.ActualRegularLabourHours * resource.RegularLabourRate) +
                    (dayData.ActualOvertimeLabourHours * resource.OvertimeLabourRate) +
                    (dayData.ActualPremiumLabourHours * resource.PremiumLabourRate) +
                    (dayData.ActualRegularTravelHours * resource.RegularTravelRate) +
                    (dayData.ActualOvertimeTravelHours * resource.OvertimeTravelRate) +
                    (dayData.ActualPremiumTravelHours * resource.PremiumTravelRate);

                decimal delta = actual - planned;

                plannedTotal += planned;
                actualTotal += actual;

                writer.Write($"${planned:F2},${actual:F2},${delta:F2},");
            }

            decimal totalDelta = actualTotal - plannedTotal;

            writer.WriteLine($"${plannedTotal:F2},${actualTotal:F2},${totalDelta:F2}");
        }

        private void WriteExpensesSubtotal(StreamWriter writer, CommissioningResource resource, int minDay, int maxDay)
        {
            writer.Write("Subtotals - Expenses,");

            decimal plannedTotal = 0;
            decimal actualTotal = 0;

            for (int day = minDay; day <= maxDay; day++)
            {
                var dayData = resource.DailyData.ContainsKey(day) ? resource.DailyData[day] : new ResourceDayData();

                decimal planned =
                    dayData.PlannedMileageCost +
                    dayData.PlannedPerDiemCost +
                    dayData.PlannedFlightCost +
                    dayData.PlannedRentalCarCost +
                    dayData.PlannedHotelCost;

                decimal actual =
                    dayData.ActualMileageCost +
                    dayData.ActualPerDiemCost +
                    dayData.ActualFlightCost +
                    dayData.ActualRentalCarCost +
                    dayData.ActualHotelCost;

                decimal delta = actual - planned;

                plannedTotal += planned;
                actualTotal += actual;

                writer.Write($"${planned:F2},${actual:F2},${delta:F2},");
            }

            decimal totalDelta = actualTotal - plannedTotal;

            writer.WriteLine($"${plannedTotal:F2},${actualTotal:F2},${totalDelta:F2}");
        }

        private void WriteTotals(StreamWriter writer, CommissioningResource resource, int minDay, int maxDay)
        {
            writer.Write("Totals,");

            decimal plannedTotal = 0;
            decimal actualTotal = 0;

            for (int day = minDay; day <= maxDay; day++)
            {
                var dayData = resource.DailyData.ContainsKey(day) ? resource.DailyData[day] : new ResourceDayData();

                decimal plannedServiceTravel =
                    (dayData.PlannedRegularLabourHours * resource.RegularLabourRate) +
                    (dayData.PlannedOvertimeLabourHours * resource.OvertimeLabourRate) +
                    (dayData.PlannedPremiumLabourHours * resource.PremiumLabourRate) +
                    (dayData.PlannedRegularTravelHours * resource.RegularTravelRate) +
                    (dayData.PlannedOvertimeTravelHours * resource.OvertimeTravelRate) +
                    (dayData.PlannedPremiumTravelHours * resource.PremiumTravelRate);

                decimal plannedExpenses =
                    dayData.PlannedMileageCost +
                    dayData.PlannedPerDiemCost +
                    dayData.PlannedFlightCost +
                    dayData.PlannedRentalCarCost +
                    dayData.PlannedHotelCost;

                decimal actualServiceTravel =
                    (dayData.ActualRegularLabourHours * resource.RegularLabourRate) +
                    (dayData.ActualOvertimeLabourHours * resource.OvertimeLabourRate) +
                    (dayData.ActualPremiumLabourHours * resource.PremiumLabourRate) +
                    (dayData.ActualRegularTravelHours * resource.RegularTravelRate) +
                    (dayData.ActualOvertimeTravelHours * resource.OvertimeTravelRate) +
                    (dayData.ActualPremiumTravelHours * resource.PremiumTravelRate);

                decimal actualExpenses =
                    dayData.ActualMileageCost +
                    dayData.ActualPerDiemCost +
                    dayData.ActualFlightCost +
                    dayData.ActualRentalCarCost +
                    dayData.ActualHotelCost;

                decimal planned = plannedServiceTravel + plannedExpenses;
                decimal actual = actualServiceTravel + actualExpenses;
                decimal delta = actual - planned;

                plannedTotal += planned;
                actualTotal += actual;

                writer.Write($"${planned:F2},${actual:F2},${delta:F2},");
            }

            decimal totalDelta = actualTotal - plannedTotal;

            writer.WriteLine($"${plannedTotal:F2},${actualTotal:F2},${totalDelta:F2}");
        }

        // Fix for the OnFormClosing method - correct signature
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Save project when closing
            SaveProject();

            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
                _refreshTimer.Dispose();
            }

            base.OnFormClosing(e);
        }

        private void SaveProject()
        {
            try
            {
                // Apply all pending changes
                foreach (TextBox textBox in _editableCells.Values)
                {
                    // Simulate leaving the cell to apply changes
                    TextBox_Leave(textBox, EventArgs.Empty);
                }

                // Save the project
                CommissioningDataManager.Instance.SaveCurrentProject();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving project: {ex.Message}");
            }
        }
    }
}