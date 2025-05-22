using System;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace LabourBudgetCalculator
{
    class Program
    {
        // Path constants to ensure consistency
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LabourBudgetCalculator");
        private static readonly string ProjectsFolder = Path.Combine(AppDataFolder, "Projects");
        private static readonly string BackupsFolder = Path.Combine(ProjectsFolder, "Backups");
        private static readonly string LogsFolder = Path.Combine(AppDataFolder, "Logs");

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Set up global exception handling
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            try
            {
                // Ensure all required directories exist
                EnsureDirectoriesExist();

                // Set up logging
                SetupLogging();

                // Fix XML project files if needed
                // FixCorruptedProjectFiles();

                // Start the application with your existing project selection form
                // Check if we should start with the welcome form or go directly to project selection
                if (File.Exists(Path.Combine(AppDataFolder, "directload.cfg")))
                {
                    Application.Run(new CommissioningProjectSelectionForm());
                }
                else
                {
                    // Assume your application starts with a WelcomeForm
                    Application.Run(new WelcomeForm());
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
                MessageBox.Show($"Fatal error starting application: {ex.Message}\n\n{ex.StackTrace}",
                    "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void EnsureDirectoriesExist()
        {
            // Create all necessary directories
            Directory.CreateDirectory(AppDataFolder);
            Directory.CreateDirectory(ProjectsFolder);
            Directory.CreateDirectory(BackupsFolder);
            Directory.CreateDirectory(LogsFolder);
        }

        private static void SetupLogging()
        {
            // Create a log file for this session
            string logFilePath = Path.Combine(LogsFolder, $"AppLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

            try
            {
                File.WriteAllText(logFilePath, $"Application started at {DateTime.Now}\r\n");
                File.AppendAllText(logFilePath, $"Operating System: {Environment.OSVersion}\r\n");
                File.AppendAllText(logFilePath, $".NET Version: {Environment.Version}\r\n");
                File.AppendAllText(logFilePath, $"Machine Name: {Environment.MachineName}\r\n");
                File.AppendAllText(logFilePath, $"User Name: {Environment.UserName}\r\n");
                File.AppendAllText(logFilePath, $"--------------------------------------------------------------\r\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize logging: {ex.Message}");
                // If logging fails, we don't want to crash the app
            }
        }

        private static void FixCorruptedProjectFiles()
        {
            try
            {
                // Get all XML files
                string[] xmlFiles = Directory.GetFiles(ProjectsFolder, "*.xml");

                if (xmlFiles.Length == 0)
                {
                    return; // No files to fix
                }

                // Check for empty or corrupted files
                int fixedCount = 0;
                foreach (string file in xmlFiles)
                {
                    try
                    {
                        FileInfo fileInfo = new FileInfo(file);

                        // Skip very small files that are likely empty or corrupted
                        if (fileInfo.Length < 100)
                        {
                            BackupAndDeleteCorruptedFile(file, "too_small");
                            fixedCount++;
                            continue;
                        }

                        // Try to read the file to check if it's valid XML
                        string content = File.ReadAllText(file);

                        // Basic check for valid XML structure
                        if (!content.StartsWith("<?xml") || !content.Contains("<CommissioningProject>"))
                        {
                            BackupAndDeleteCorruptedFile(file, "invalid_format");
                            fixedCount++;
                            continue;
                        }

                        // Try to validate deeper by attempting to deserialize (but don't actually use the result)
        //                try
        //                {
        //                    // This will throw an exception if the XML is malformed or doesn't match the class structure
        //                   using (XmlReader reader = XmlReader.Create(new StringReader(content)))
        //                    {
        //                        var serializer = new XmlSerializer(typeof(CommissioningProject));
        //                        serializer.Deserialize(reader);
        //                    }
        //                }
         //               catch (Exception ex)
         //               {
         //                   // Deserialization failed, file is likely corrupted
         //                   System.Diagnostics.Debug.WriteLine($"XML validation failed for {file}: {ex.Message}");
         //                   BackupAndDeleteCorruptedFile(file, "deserialization_error");
         //                   fixedCount++;
         //               }
                    }
                    catch (Exception ex)
                    {
                        // If we can't even check the file, it's definitely corrupted
                        System.Diagnostics.Debug.WriteLine($"Error checking file {file}: {ex.Message}");
                        try
                        {
                            BackupAndDeleteCorruptedFile(file, "check_error");
                            fixedCount++;
                        }
                        catch
                        {
                            // If the backup fails, log the error but continue
                            System.Diagnostics.Debug.WriteLine($"Failed to back up corrupted file {file}");
                        }
                    }
                }

                // Notify the user if we fixed any files
                if (fixedCount > 0)
                {
                    MessageBox.Show(
                        $"{fixedCount} corrupted project files were detected and backed up. " +
                        $"New project files will be created as needed.\n\n" +
                        $"Backups can be found in:\n{BackupsFolder}",
                        "Project Files Fixed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                // If the entire fix process fails, log but continue
                System.Diagnostics.Debug.WriteLine($"Error in FixCorruptedProjectFiles: {ex.Message}");
                LogException(ex);
            }
        }

        private static void BackupAndDeleteCorruptedFile(string filePath, string reason)
        {
            try
            {
                string fileName = Path.GetFileName(filePath);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupPath = Path.Combine(BackupsFolder, $"{Path.GetFileNameWithoutExtension(filePath)}_{reason}_{timestamp}.xml");

                // Create a backup
                File.Copy(filePath, backupPath, true);

                // Log the backup
                System.Diagnostics.Debug.WriteLine($"Backed up corrupted file {filePath} to {backupPath}");

                // Delete the corrupted file
                File.Delete(filePath);

                // Log what we did
                LogCorruptedFileAction(filePath, backupPath, reason);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error backing up file {filePath}: {ex.Message}");
                throw; // Rethrow to be handled by the caller
            }
        }

        private static void LogCorruptedFileAction(string originalFile, string backupPath, string reason)
        {
            try
            {
                string logFilePath = Path.Combine(LogsFolder, "CorruptedFiles.log");
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Corrupted file: {originalFile}\r\n" +
                                   $"Reason: {reason}\r\n" +
                                   $"Backup created at: {backupPath}\r\n" +
                                   $"--------------------------------------------------------------\r\n";

                File.AppendAllText(logFilePath, logEntry);
            }
            catch
            {
                // If logging fails, there's not much we can do
            }
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            // Log the exception
            LogException(e.Exception);

            // Show a friendly message to the user
            ShowExceptionMessage(e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Log the exception
            LogException(e.ExceptionObject as Exception);

            // Show a friendly message if possible
            if (e.ExceptionObject is Exception ex)
            {
                ShowExceptionMessage(ex);
            }
            else
            {
                MessageBox.Show(
                    "A serious error occurred in the application. The application will now close.",
                    "Fatal Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static void LogException(Exception ex)
        {
            if (ex == null) return;

            try
            {
                string logFilePath = Path.Combine(LogsFolder, $"ErrorLog_{DateTime.Now:yyyyMMdd}.txt");

                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error: {ex.Message}\r\n" +
                                   $"Stack Trace:\r\n{ex.StackTrace}\r\n" +
                                   $"Inner Exception: {ex.InnerException?.Message}\r\n" +
                                   $"--------------------------------------------------------------\r\n";

                // Append to the log file
                File.AppendAllText(logFilePath, logEntry);
            }
            catch
            {
                // If logging fails, there's not much we can do
            }
        }

        private static void ShowExceptionMessage(Exception ex)
        {
            string message = "An error occurred in the application:";

            if (ex is XmlException ||
                ex is InvalidOperationException && ex.Message.Contains("XML"))
            {
                message = "An error occurred while processing project data. This may be due to corrupted XML files.\n\n" +
                          "The application will try to repair the issue. If the problem persists, please contact support.";
            }

            MessageBox.Show(
                $"{message}\n\n{ex.Message}",
                "Application Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}