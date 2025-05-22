using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LabourBudgetCalculator.Helpers
{
    public class CommissioningDataManager
    {
        // --- Singleton Implementation ---
        private static CommissioningDataManager _instance;
        public static CommissioningDataManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new CommissioningDataManager();
                return _instance;
            }
        }

        private CommissioningProject _currentProject;
        public CommissioningProject CurrentProject => _currentProject;

        private readonly string _projectsDirectory;
        private readonly string _backupDirectory;

        public CommissioningDataManager()
        {
            _projectsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LabourBudgetCalculator", "Projects");

            _backupDirectory = Path.Combine(_projectsDirectory, "Backups");

            // Ensure the directories exist
            if (!Directory.Exists(_projectsDirectory))
            {
                Directory.CreateDirectory(_projectsDirectory);
            }

            if (!Directory.Exists(_backupDirectory))
            {
                Directory.CreateDirectory(_backupDirectory);
            }
        }

        /// <summary>
        /// Sets the currently active project for the manager.
        /// </summary>
        public void SetCurrentProject(CommissioningProject project)
        {
            _currentProject = project;
        }

        /// <summary>
        /// Gets a project by ID. If it's the CurrentProject, returns that, otherwise tries to load it.
        /// </summary>
        public CommissioningProject GetProjectById(string projectId)
        {
            if (string.IsNullOrEmpty(projectId)) return null;

            if (_currentProject != null && _currentProject.ProjectID == projectId)
            {
                return _currentProject;
            }
            // If not the current one, try to load it
            return LoadProject(projectId);
        }

        /// <summary>
        /// Loads a project by its ID and sets it as the CurrentProject.
        /// Includes improved error handling and recovery.
        /// </summary>
        public CommissioningProject LoadProject(string projectId)
        {
            if (string.IsNullOrEmpty(projectId))
            {
                // Create a new default project if no ID is provided
                _currentProject = CreateNewProject();
                System.Diagnostics.Debug.WriteLine($"LoadProject: No projectId provided, created new project with ID: {_currentProject.ProjectID}");
                return _currentProject;
            }

            string projectFilePath = Path.Combine(_projectsDirectory, $"{projectId}.xml");

            if (File.Exists(projectFilePath))
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"Attempting to load project from {projectFilePath}");

                    // Try to load the project
                    _currentProject = DeserializeProject(projectFilePath);
                    if (_currentProject != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Project '{_currentProject.ProjectName}' loaded successfully from {projectFilePath}");

                        // Validate and ensure all resources and daily data are properly initialized
                        ValidateProject(_currentProject);
                        return _currentProject;
                    }
                }
                catch (Exception ex)
                {
                    // Log detailed error information
                    System.Diagnostics.Debug.WriteLine($"XML Loading failed for {projectFilePath}:");
                    System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Exception Type: {ex.GetType().Name}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    }

                    // Create a backup of the corrupted file
                    string backupPath = CreateBackup(projectFilePath);
                    System.Diagnostics.Debug.WriteLine($"Created backup at: {backupPath}");

                    // For now, continue with creating a new project instead of showing error to user
                    System.Diagnostics.Debug.WriteLine("Continuing with new project creation due to XML loading error");
                }
            }

            // If we get here, either the file doesn't exist or loading failed
            _currentProject = CreateNewProject(projectId);
            return _currentProject;
        }

        /// <summary>
        /// Creates a new project with optional specific ID
        /// </summary>
        private CommissioningProject CreateNewProject(string projectId = null)
        {
            var project = new CommissioningProject
            {
                ProjectID = projectId ?? Guid.NewGuid().ToString(),
                ProjectName = projectId == null ? "New Project" : $"New Project {projectId.Substring(0, Math.Min(5, projectId.Length))}",
                ProjectDate = DateTime.Now,
                Resources = new List<CommissioningResource>()
            };
            SetCurrentProject(project);
            return project;
        }

        /// <summary>
        /// Deserializes a project from XML with improved error handling
        /// </summary>
        private CommissioningProject DeserializeProject(string filePath)
        {
            try
            {
                // First try normal deserialization
                using (var reader = new StreamReader(filePath))
                {
                    var serializer = new XmlSerializer(typeof(CommissioningProject));
                    var project = (CommissioningProject)serializer.Deserialize(reader);
                    return project;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Standard deserialization failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Exception type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }

                // If that fails, try reading the XML and see if we need to repair it
                string xml = File.ReadAllText(filePath);

                // Check for common XML issues and try to fix them
                if (string.IsNullOrWhiteSpace(xml))
                {
                    throw new Exception("Project file is empty.");
                }

                // If we can't repair, re-throw the original exception
                throw;
            }
        }

        /// <summary>
        /// Creates a backup of a file before attempting repairs
        /// </summary>
        private string CreateBackup(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return null;

                string fileName = Path.GetFileName(filePath);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupPath = Path.Combine(_backupDirectory, $"{timestamp}_{fileName}");

                File.Copy(filePath, backupPath, true);
                return backupPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create backup: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Validates and ensures a project has all required data structures initialized
        /// </summary>
        private void ValidateProject(CommissioningProject project)
        {
            if (project == null) return;

            // Ensure project has Resources collection
            if (project.Resources == null)
            {
                project.Resources = new List<CommissioningResource>();
            }

            // Validate each resource
            foreach (var resource in project.Resources)
            {
                // Ensure resource has a valid ID
                if (string.IsNullOrEmpty(resource.ResourceID))
                {
                    resource.ResourceID = Guid.NewGuid().ToString();
                }

                // Ensure DailyData is initialized
                if (resource.DailyData == null)
                {
                    resource.DailyData = new Dictionary<int, Models.ResourceDayData>();
                    resource.IsDirty = true; // Mark for re-initialization
                }

                // Initialize resources with no daily data
                if (resource.DailyData.Count == 0 || resource.IsDirty)
                {
                    resource.InitializeFromSchedule();
                }
            }

            // Recalculate project totals
            project.CalculateTotals();
        }

        /// <summary>
        /// Saves the project currently held in the CurrentProject property.
        /// </summary>
        public void SaveCurrentProject()
        {
            if (_currentProject == null)
            {
                System.Diagnostics.Debug.WriteLine("SaveCurrentProject called, but CurrentProject is null.");
                return;
            }
            SaveProject(_currentProject);
        }

        /// <summary>
        /// Saves the given CommissioningProject object to an XML file with improved error handling.
        /// </summary>
        public void SaveProject(CommissioningProject project)
        {
            if (project == null || string.IsNullOrEmpty(project.ProjectID))
            {
                System.Diagnostics.Debug.WriteLine("SaveProject called with null project or null/empty ProjectID.");
                return;
            }

            string filePath = Path.Combine(_projectsDirectory, $"{project.ProjectID}.xml");
            string tempFilePath = filePath + ".temp";

            try
            {
                // Validate before saving
                ValidateProject(project);

                // Save to a temporary file first
                using (var writer = new StreamWriter(tempFilePath))
                {
                    var serializer = new XmlSerializer(typeof(CommissioningProject));
                    serializer.Serialize(writer, project);
                }

                // Create a backup of the existing file if it exists
                if (File.Exists(filePath))
                {
                    CreateBackup(filePath);
                }

                // Replace the original file with the temp file
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                File.Move(tempFilePath, filePath);

                System.Diagnostics.Debug.WriteLine($"Project '{project.ProjectName}' saved to {filePath}");

                // Reset dirty flags
                project.IsDirty = false;
                if (project.Resources != null)
                {
                    foreach (var resource in project.Resources)
                    {
                        resource.IsDirty = false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving project '{project.ProjectName}' to '{filePath}': {ex.Message}");

                // Clean up temp file if it exists
                try
                {
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }
                }
                catch { /* Ignore cleanup errors */ }

                MessageBox.Show(
                    $"There was an error saving the project file:\n\n{ex.Message}",
                    "Project Save Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Loads all project summaries from the Projects directory.
        /// </summary>
        public List<CommissioningProject> LoadProjects()
        {
            List<CommissioningProject> projects = new List<CommissioningProject>();

            if (Directory.Exists(_projectsDirectory))
            {
                foreach (string file in Directory.GetFiles(_projectsDirectory, "*.xml"))
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"LoadProjects: Processing file {file}");
                        string projectId = Path.GetFileNameWithoutExtension(file);

                        // Only load basic project info (not the full project with resources)
                        CommissioningProject projectSummary = LoadProjectSummary(file);
                        if (projectSummary != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"LoadProjects: Successfully loaded summary for {projectSummary.ProjectName}");
                            projects.Add(projectSummary);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"LoadProjects: LoadProjectSummary returned null for {file}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"LoadProjects: Error loading project file '{file}': {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"LoadProjects: Exception type: {ex.GetType().Name}");
                        if (ex.InnerException != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"LoadProjects: Inner exception: {ex.InnerException.Message}");
                        }
                    }
                }
            }
            return projects;
        }

        /// <summary>
        /// Loads just the summary data for a project, without loading all resources and daily data
        /// </summary>
        private CommissioningProject LoadProjectSummary(string filePath)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"LoadProjectSummary: Attempting to load {filePath}");

                using (var reader = new StreamReader(filePath))
                {
                    var serializer = new XmlSerializer(typeof(CommissioningProject));
                    var project = (CommissioningProject)serializer.Deserialize(reader);

                    System.Diagnostics.Debug.WriteLine($"LoadProjectSummary: Successfully deserialized {project?.ProjectName}");

                    // Clear resource data to save memory - we just need the project metadata
                    if (project.Resources != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"LoadProjectSummary: Clearing {project.Resources.Count} resources to save memory");
                        project.Resources.Clear();
                    }

                    return project;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadProjectSummary: Failed to load {filePath}");
                System.Diagnostics.Debug.WriteLine($"LoadProjectSummary: Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"LoadProjectSummary: Exception type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"LoadProjectSummary: Inner exception: {ex.InnerException.Message}");
                }
                return null;
            }
        }
    }
}