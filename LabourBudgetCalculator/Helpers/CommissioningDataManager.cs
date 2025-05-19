using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace LabourBudgetCalculator.Helpers
{
    public class CommissioningDataManager
    {
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

        private string _projectFilePath;
        private readonly string _projectsDirectory;
        private static string _projectsListFile;

        public CommissioningDataManager()
        {
            _projectsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LabourBudgetCalculator", "Projects");

            // Set the file for the projects list
            _projectsListFile = Path.Combine(_projectsDirectory, "ProjectsList.xml");

            // Ensure the directory exists
            if (!Directory.Exists(_projectsDirectory))
            {
                Directory.CreateDirectory(_projectsDirectory);
            }
        }

        public CommissioningProject LoadProject(string projectId)
        {
            _projectFilePath = Path.Combine(_projectsDirectory, $"{projectId}.xml");

            if (File.Exists(_projectFilePath))
            {
                try
                {
                    using (var reader = new StreamReader(_projectFilePath))
                    {
                        var serializer = new XmlSerializer(typeof(CommissioningProject));
                        _currentProject = (CommissioningProject)serializer.Deserialize(reader);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading project: {ex.Message}");
                    _currentProject = new CommissioningProject
                    {
                        ProjectID = projectId,
                        ProjectName = "New Project",
                        ProjectDate = DateTime.Now
                    };
                }
            }
            else
            {
                _currentProject = new CommissioningProject
                {
                    ProjectID = projectId,
                    ProjectName = "New Project",
                    ProjectDate = DateTime.Now
                };
            }

            return _currentProject;
        }

        public void SaveCurrentProject()
        {
            if (_currentProject == null)
                return;

            SaveProject(_currentProject);
        }

        public static void SaveProject(CommissioningProject project)
        {
            if (project == null)
                return;

            string projectsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LabourBudgetCalculator", "Projects");

            if (!Directory.Exists(projectsDirectory))
            {
                Directory.CreateDirectory(projectsDirectory);
            }

            string filePath = Path.Combine(projectsDirectory, $"{project.ProjectID}.xml");

            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    var serializer = new XmlSerializer(typeof(CommissioningProject));
                    serializer.Serialize(writer, project);
                }

                // Reset dirty flag after successful save
                project.IsDirty = false;
                foreach (var resource in project.Resources)
                {
                    resource.IsDirty = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving project: {ex.Message}");
            }
        }

        // Method to load all projects
        public static List<CommissioningProject> LoadProjects()
        {
            List<CommissioningProject> projects = new List<CommissioningProject>();

            try
            {
                string projectsDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "LabourBudgetCalculator", "Projects");

                string projectsListFile = Path.Combine(projectsDirectory, "ProjectsList.xml");

                if (File.Exists(projectsListFile))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<CommissioningProject>));
                    using (FileStream fs = new FileStream(projectsListFile, FileMode.Open))
                    {
                        projects = (List<CommissioningProject>)serializer.Deserialize(fs);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading projects list: {ex.Message}");
            }

            return projects;
        }

        // Method to save all projects
        public static void SaveProjects(List<CommissioningProject> projects)
        {
            if (projects == null) return;

            try
            {
                string projectsDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "LabourBudgetCalculator", "Projects");

                if (!Directory.Exists(projectsDirectory))
                {
                    Directory.CreateDirectory(projectsDirectory);
                }

                string projectsListFile = Path.Combine(projectsDirectory, "ProjectsList.xml");

                XmlSerializer serializer = new XmlSerializer(typeof(List<CommissioningProject>));
                using (FileStream fs = new FileStream(projectsListFile, FileMode.Create))
                {
                    serializer.Serialize(fs, projects);
                }

                // Also save each individual project file
                foreach (var project in projects)
                {
                    SaveProject(project);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving projects list: {ex.Message}");
            }
        }
    }
}