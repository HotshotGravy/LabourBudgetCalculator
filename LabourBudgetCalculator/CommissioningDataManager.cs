using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace LabourBudgetCalculator
{
    public static class CommissioningDataManager
    {
        private static string dataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TimeExpenseCalculator");
        private static string projectsFile = Path.Combine(dataPath, "CommissioningProjects.xml");

        static CommissioningDataManager()
        {
            // Create directory if it doesn't exist
            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }
        }

        public static void SaveProjects(List<CommissioningProject> projects)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<CommissioningProject>));
                using (FileStream fs = new FileStream(projectsFile, FileMode.Create))
                {
                    serializer.Serialize(fs, projects);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving commissioning projects: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static List<CommissioningProject> LoadProjects()
        {
            List<CommissioningProject> projects = new List<CommissioningProject>();

            if (File.Exists(projectsFile))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<CommissioningProject>));
                    using (FileStream fs = new FileStream(projectsFile, FileMode.Open))
                    {
                        projects = (List<CommissioningProject>)serializer.Deserialize(fs);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading commissioning projects: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            return projects;
        }

        public static CommissioningProject GetProjectByID(string projectID)
        {
            var projects = LoadProjects();
            return projects.Find(p => p.ProjectID == projectID);
        }

        public static void SaveProject(CommissioningProject project)
        {
            var projects = LoadProjects();
            var existingIndex = projects.FindIndex(p => p.ProjectID == project.ProjectID);

            if (existingIndex >= 0)
            {
                projects[existingIndex] = project;
            }
            else
            {
                projects.Add(project);
            }

            SaveProjects(projects);
        }
    }
}