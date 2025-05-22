using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using LabourBudgetCalculator.Helpers;

namespace LabourBudgetCalculator
{
    public partial class CommissioningProjectSelectionForm : Form
    {
        private ComboBox cboProjects;
        private TextBox txtNewProjectID;
        private TextBox txtNewProjectName;
        private NumericUpDown nudEstimateAmount;
        private Button btnOpenProject;
        private Button btnCreateProject;
        private Button btnBack;
        private Button btnDeleteProject;
        private Label lblProjects;
        private Label lblNewProjectID;
        private Label lblNewProjectName;
        private Label lblEstimateAmount;
        private Panel pnlExistingProject;
        private Panel pnlNewProject;
        private List<CommissioningProject> commissioningProjects;

        public CommissioningProjectSelectionForm()
        {
            InitializeComponent();
            SetupForm();
            SetupControls();
            LoadProjects();
        }

        private void SetupForm()
        {
            this.Text = "Commissioning Project Selection";
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
        }

        private void SetupControls()
        {
            // Title
            Label lblTitle = new Label();
            lblTitle.Text = "Select or Create Commissioning Project";
            lblTitle.Font = new Font(this.Font.FontFamily, 14, FontStyle.Bold);
            lblTitle.Size = new Size(550, 40);
            lblTitle.Location = new Point(25, 20);
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblTitle);

            // Panel for existing projects
            pnlExistingProject = new Panel();
            pnlExistingProject.Size = new Size(550, 120);
            pnlExistingProject.Location = new Point(25, 70);
            pnlExistingProject.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(pnlExistingProject);

            // Label for projects list
            lblProjects = new Label();
            lblProjects.Text = "Select Existing Project:";
            lblProjects.Location = new Point(10, 10);
            lblProjects.AutoSize = true;
            pnlExistingProject.Controls.Add(lblProjects);

            // Combo box for projects
            cboProjects = new ComboBox();
            cboProjects.Size = new Size(400, 25);
            cboProjects.Location = new Point(10, 40);
            cboProjects.DropDownStyle = ComboBoxStyle.DropDownList;
            pnlExistingProject.Controls.Add(cboProjects);

            // Open project button
            btnOpenProject = new Button();
            btnOpenProject.Text = "Open Project";
            btnOpenProject.Size = new Size(120, 25);
            btnOpenProject.Location = new Point(10, 75);
            btnOpenProject.Click += BtnOpenProject_Click;
            pnlExistingProject.Controls.Add(btnOpenProject);

            // Delete project button
            btnDeleteProject = new Button();
            btnDeleteProject.Text = "Delete Project";
            btnDeleteProject.Size = new Size(120, 25);
            btnDeleteProject.Location = new Point(140, 75);
            btnDeleteProject.Click += BtnDeleteProject_Click;
            pnlExistingProject.Controls.Add(btnDeleteProject);

            // Panel for new project
            pnlNewProject = new Panel();
            pnlNewProject.Size = new Size(550, 150);
            pnlNewProject.Location = new Point(25, 210);
            pnlNewProject.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(pnlNewProject);

            // New project ID
            lblNewProjectID = new Label();
            lblNewProjectID.Text = "New Project ID:";
            lblNewProjectID.Location = new Point(10, 10);
            lblNewProjectID.AutoSize = true;
            pnlNewProject.Controls.Add(lblNewProjectID);

            txtNewProjectID = new TextBox();
            txtNewProjectID.Size = new Size(150, 25);
            txtNewProjectID.Location = new Point(120, 8);
            pnlNewProject.Controls.Add(txtNewProjectID);

            // Generate ID button
            Button btnGenerateID = new Button();
            btnGenerateID.Text = "Generate ID";
            btnGenerateID.Size = new Size(100, 23);
            btnGenerateID.Location = new Point(280, 8);
            btnGenerateID.Click += (s, e) => { txtNewProjectID.Text = Guid.NewGuid().ToString().Substring(0, 8); };
            pnlNewProject.Controls.Add(btnGenerateID);

            // New project name
            lblNewProjectName = new Label();
            lblNewProjectName.Text = "Project Name:";
            lblNewProjectName.Location = new Point(10, 45);
            lblNewProjectName.AutoSize = true;
            pnlNewProject.Controls.Add(lblNewProjectName);

            txtNewProjectName = new TextBox();
            txtNewProjectName.Size = new Size(400, 25);
            txtNewProjectName.Location = new Point(120, 43);
            pnlNewProject.Controls.Add(txtNewProjectName);

            // Estimate amount
            lblEstimateAmount = new Label();
            lblEstimateAmount.Text = "Initial Estimate ($):";
            lblEstimateAmount.Location = new Point(10, 80);
            lblEstimateAmount.AutoSize = true;
            pnlNewProject.Controls.Add(lblEstimateAmount);

            nudEstimateAmount = new NumericUpDown();
            nudEstimateAmount.Size = new Size(150, 25);
            nudEstimateAmount.Location = new Point(120, 78);
            nudEstimateAmount.Minimum = 0;
            nudEstimateAmount.Maximum = 999999999;
            nudEstimateAmount.DecimalPlaces = 2;
            nudEstimateAmount.ThousandsSeparator = true;
            pnlNewProject.Controls.Add(nudEstimateAmount);

            // Create project button
            btnCreateProject = new Button();
            btnCreateProject.Text = "Create Project";
            btnCreateProject.Size = new Size(120, 25);
            btnCreateProject.Location = new Point(10, 115);
            btnCreateProject.Click += BtnCreateProject_Click;
            pnlNewProject.Controls.Add(btnCreateProject);

            // Back button
            btnBack = new Button();
            btnBack.Text = "Back";
            btnBack.Size = new Size(80, 30);
            btnBack.Location = new Point(25, 365);
            btnBack.Click += BtnBack_Click;
            this.Controls.Add(btnBack);
        }

        private void LoadProjects()
        {
            try
            {
                // Use the improved data manager to load projects
                commissioningProjects = CommissioningDataManager.Instance.LoadProjects();
                cboProjects.Items.Clear();

                if (commissioningProjects.Count == 0)
                {
                    // Add a placeholder item if no projects exist
                    cboProjects.Items.Add(new ComboBoxItem("", "No projects found. Create a new project below."));
                    cboProjects.SelectedIndex = 0;
                    btnOpenProject.Enabled = false;
                    btnDeleteProject.Enabled = false;
                }
                else
                {
                    // Add projects to the combobox
                    foreach (var project in commissioningProjects)
                    {
                        string displayText = $"{project.ProjectName}";
                        if (!string.IsNullOrEmpty(project.ClientName))
                            displayText += $" - {project.ClientName}";

                        cboProjects.Items.Add(new ComboBoxItem(project.ProjectID, displayText));
                    }

                    // Select the first project by default
                    if (cboProjects.Items.Count > 0)
                    {
                        cboProjects.SelectedIndex = 0;
                        btnOpenProject.Enabled = true;
                        btnDeleteProject.Enabled = true;
                    }
                }

                // Generate a default project ID for new projects
                if (string.IsNullOrEmpty(txtNewProjectID.Text))
                {
                    txtNewProjectID.Text = Guid.NewGuid().ToString().Substring(0, 8);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading projects: {ex.Message}\n\nThe list will be empty. You can create a new project below.",
                    "Error Loading Projects", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                commissioningProjects = new List<CommissioningProject>();
                cboProjects.Items.Add(new ComboBoxItem("", "Error loading projects. Create a new project below."));
                cboProjects.SelectedIndex = 0;
                btnOpenProject.Enabled = false;
                btnDeleteProject.Enabled = false;
            }
        }

        private void BtnOpenProject_Click(object sender, EventArgs e)
        {
            if (cboProjects.SelectedItem == null || string.IsNullOrEmpty(((ComboBoxItem)cboProjects.SelectedItem).Value))
            {
                MessageBox.Show("Please select a valid project first.", "Selection Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                ComboBoxItem selectedItem = (ComboBoxItem)cboProjects.SelectedItem;
                string projectId = selectedItem.Value;

                // Use the improved data manager to load the project
                CommissioningProject project = CommissioningDataManager.Instance.LoadProject(projectId);

                if (project != null)
                {
                    // Open the data entry form with the selected project
                    this.Hide();
                    CommissioningDataEntryForm dataEntryForm = new CommissioningDataEntryForm(project);
                    dataEntryForm.ShowDialog();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Failed to load the selected project. The project file may be corrupted or missing.",
                        "Project Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    // Refresh the project list
                    LoadProjects();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening project: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDeleteProject_Click(object sender, EventArgs e)
        {
            if (cboProjects.SelectedItem == null || string.IsNullOrEmpty(((ComboBoxItem)cboProjects.SelectedItem).Value))
            {
                MessageBox.Show("Please select a valid project first.", "Selection Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                ComboBoxItem selectedItem = (ComboBoxItem)cboProjects.SelectedItem;
                string projectId = selectedItem.Value;
                string projectName = selectedItem.Text;

                // Confirm deletion
                DialogResult result = MessageBox.Show(
                    $"Are you sure you want to delete the project '{projectName}'?\n\nThis action cannot be undone.",
                    "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    // Create a backup before deleting
                    string projectsDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "LabourBudgetCalculator", "Projects");

                    string projectFilePath = Path.Combine(projectsDir, $"{projectId}.xml");

                    if (File.Exists(projectFilePath))
                    {
                        // Create backup directory if it doesn't exist
                        string backupDir = Path.Combine(projectsDir, "Backups");
                        if (!Directory.Exists(backupDir))
                        {
                            Directory.CreateDirectory(backupDir);
                        }

                        // Create backup with timestamp
                        string backupPath = Path.Combine(backupDir,
                            $"{projectId}_deleted_{DateTime.Now:yyyyMMdd_HHmmss}.xml");

                        File.Copy(projectFilePath, backupPath);

                        // Delete the file
                        File.Delete(projectFilePath);

                        MessageBox.Show(
                            $"Project '{projectName}' has been deleted.\nA backup has been created at:\n{backupPath}",
                            "Project Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Refresh the project list
                        LoadProjects();
                    }
                    else
                    {
                        MessageBox.Show(
                            "The project file could not be found. It may have been deleted already.",
                            "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                        // Refresh the project list anyway
                        LoadProjects();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting project: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCreateProject_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNewProjectID.Text) ||
                string.IsNullOrWhiteSpace(txtNewProjectName.Text))
            {
                MessageBox.Show("Please enter both Project ID and Project Name.", "Input Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string projectId = txtNewProjectID.Text.Trim();

                // Check if project ID already exists
                if (commissioningProjects.Exists(p => p.ProjectID == projectId))
                {
                    MessageBox.Show("A project with this ID already exists. Please use a different ID.", "Duplicate ID",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Create new project with the improved structure
                var newProject = new CommissioningProject
                {
                    ProjectID = projectId,
                    ProjectName = txtNewProjectName.Text.Trim(),
                    InitialEstimate = nudEstimateAmount.Value,
                    ProjectDate = DateTime.Now,
                    Resources = new List<CommissioningResource>()
                };

                // Save the new project using the improved data manager
                CommissioningDataManager.Instance.SetCurrentProject(newProject);
                CommissioningDataManager.Instance.SaveCurrentProject();

                // Open the data entry form with the new project
                this.Hide();
                CommissioningDataEntryForm dataEntryForm = new CommissioningDataEntryForm(newProject);
                dataEntryForm.ShowDialog();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating project: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            this.Close();

            try
            {
                // Show the welcome form
                WelcomeForm welcomeForm = new WelcomeForm();
                welcomeForm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error returning to welcome form: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }
    }

    // Helper class for ComboBox items
    public class ComboBoxItem
    {
        public string Value { get; set; }
        public string Text { get; set; }

        public ComboBoxItem(string value, string text)
        {
            Value = value;
            Text = text;
        }

        public override string ToString()
        {
            return Text;
        }
    }
}