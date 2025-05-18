using System;
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

        public CommissioningDataManager()
        {
            _projectsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LabourBudgetCalculator", "Projects");

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
    }
}