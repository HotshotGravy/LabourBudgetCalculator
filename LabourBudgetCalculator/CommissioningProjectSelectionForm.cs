



using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
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
                // Load existing commissioning projects from XML file
                commissioningProjects = CommissioningDataManager.LoadProjects();
                cboProjects.Items.Clear();

                foreach (var project in commissioningProjects)
                {
                    cboProjects.Items.Add(new ComboBoxItem(project.ProjectID, project.ProjectName));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading projects: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnOpenProject_Click(object sender, EventArgs e)
        {
            if (cboProjects.SelectedItem != null)
            {
                ComboBoxItem selectedProject = (ComboBoxItem)cboProjects.SelectedItem;

                // Find the selected project
                CommissioningProject project = commissioningProjects.Find(p => p.ProjectID == selectedProject.Value);

                if (project != null)
                {
                    // Open the data entry form
                    this.Hide();
                    CommissioningDataEntryForm dataEntryForm = new CommissioningDataEntryForm(project);
                    dataEntryForm.ShowDialog();
                    this.Close();
                }
            }
            else
            {
                MessageBox.Show("Please select a project first.", "Selection Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                // Check if project ID already exists
                if (commissioningProjects.Exists(p => p.ProjectID == txtNewProjectID.Text.Trim()))
                {
                    MessageBox.Show("A project with this ID already exists. Please use a different ID.", "Duplicate ID",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Create new project
                var newProject = new CommissioningProject
                {
                    ProjectID = txtNewProjectID.Text.Trim(),
                    ProjectName = txtNewProjectName.Text.Trim(),
                    InitialEstimate = (decimal)nudEstimateAmount.Value,
                    CreatedDate = DateTime.Now
                };

                // Add to list and save
                commissioningProjects.Add(newProject);
                CommissioningDataManager.SaveProjects(commissioningProjects);

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
            WelcomeForm welcomeForm = new WelcomeForm();
            welcomeForm.Show();
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