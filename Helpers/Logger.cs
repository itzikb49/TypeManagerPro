// Logger.cs
using System;
using System.IO;
using System.Text;

namespace TypeManagerPro.Helpers
{
    /// <summary>
    /// Logger for Type Manager Pro with separate log files per category
    /// </summary>
    public static class Logger
    {
        #region Constants

        private static readonly string LogDirectory;
        private static readonly object LockObject = new object();

        #endregion

        #region Log Categories (Files)

        public enum LogCategory
        {
            Main,       // TypeManagerPro-Main.log
            Rename,     // TypeManagerPro-Rename.log
            License,    // TypeManagerPro-License.log
            Analytics,  // TypeManagerPro-Analytics.log
            Excel,      // TypeManagerPro-Excel.log
            Errors      // TypeManagerPro-Errors.log
        }

        #endregion

        #region Constructor

        static Logger()
        {
            // Fixed logs folder: C:\ProgramData\IB-BIM\TypeManagerPro\Logs
            LogDirectory = @"C:\ProgramData\IB-BIM\TypeManagerPro\Logs";

            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }
            }
            catch 
            {
                // If can't create in ProgramData, fallback to AppData
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                LogDirectory = Path.Combine(appData, "IB-BIM", "TypeManagerPro", "Logs");

                try
                {
                    if (!Directory.Exists(LogDirectory))
                    {
                        Directory.CreateDirectory(LogDirectory);
                    }
                }
                catch
                {
                    // Last resort: temp folder
                    LogDirectory = Path.Combine(Path.GetTempPath(), "IB-BIM", "TypeManagerPro", "Logs");
                    Directory.CreateDirectory(LogDirectory);
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Logs an Info message
        /// </summary>
        public static void Info(LogCategory category, string message, string details = null)
        {
            Log(category, "INFO", message, details);
        }

        /// <summary>
        /// Logs a Warning message
        /// </summary>
        public static void Warning(LogCategory category, string message, string details = null)
        {
            Log(category, "WARN", message, details);

            // Also log to Errors file
            if (category != LogCategory.Errors)
            {
                Log(LogCategory.Errors, "WARN", $"[{category}] {message}", details);
            }
        }

        /// <summary>
        /// Logs an Error message
        /// </summary>
        public static void Error(LogCategory category, string message, Exception ex = null)
        {
            string details = ex != null ? GetExceptionDetails(ex) : null;
            Log(category, "ERROR", message, details);

            // Also log to Errors file
            if (category != LogCategory.Errors)
            {
                Log(LogCategory.Errors, "ERROR", $"[{category}] {message}", details);
            }
        }

        /// <summary>
        /// Logs a Debug message (only in Debug builds)
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        public static void Debug(LogCategory category, string message, string details = null)
        {
            Log(category, "DEBUG", message, details);
        }

        /// <summary>
        /// Logs method entry (for debugging)
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        public static void MethodEntry(LogCategory category, string className, string methodName)
        {
            Log(category, "ENTRY", $"{className}.{methodName}()");
        }

        /// <summary>
        /// Logs method exit (for debugging)
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        public static void MethodExit(LogCategory category, string className, string methodName)
        {
            Log(category, "EXIT", $"{className}.{methodName}()");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Main logging method - SYNCHRONOUS to ensure logs are written immediately
        /// This prevents log loss in case of crashes or async operations
        /// </summary>
        private static void Log(LogCategory category, string level, string message, string details = null)
        {
            try
            {
                string logFile = GetLogFilePath(category);
                string logEntry = FormatLogEntry(level, message, details);

                // SYNCHRONOUS write with lock - ensures logs are written immediately
                // Even if app crashes or async operations fail, logs will be persisted
                lock (LockObject)
                {
                    // Using AppendAllText for immediate flush to disk
                    File.AppendAllText(logFile, logEntry, Encoding.UTF8);
                }

                // Also write to Debug output
                System.Diagnostics.Debug.WriteLine(logEntry.TrimEnd());
            }
            catch
            {
                // Silent fail - don't want logging to crash the app
                // Could write to Event Log here as last resort if needed
            }
        }

        /// <summary>
        /// Gets log file path for category
        /// </summary>
        private static string GetLogFilePath(LogCategory category)
        {
            string fileName = $"TypeManagerPro-{category}.log";
            return Path.Combine(LogDirectory, fileName);
        }

        /// <summary>
        /// Formats log entry
        /// </summary>
        private static string FormatLogEntry(string level, string message, string details = null)
        {
            var sb = new StringBuilder();

            // Timestamp | Level | Message
            sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level,-5}] {message}");

            // Details (if provided)
            if (!string.IsNullOrEmpty(details))
            {
                sb.AppendLine($"  Details: {details}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets exception details including inner exceptions
        /// </summary>
        private static string GetExceptionDetails(Exception ex)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Exception: {ex.GetType().Name}");
            sb.AppendLine($"Message: {ex.Message}");

            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                sb.AppendLine($"StackTrace: {ex.StackTrace}");
            }

            // Inner exception
            if (ex.InnerException != null)
            {
                sb.AppendLine("Inner Exception:");
                sb.Append(GetExceptionDetails(ex.InnerException));
            }

            return sb.ToString();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the logger - should be called at application startup
        /// </summary>
        public static void Initialize(string appName = "Type Manager Pro", string version = null)
        {
            try
            {
                // Write session separator
                WriteSeparator(LogCategory.Main);

                // Log application startup
                Info(LogCategory.Main, "=".PadRight(80, '='));
                Info(LogCategory.Main, $"{appName} - SESSION STARTED");
                Info(LogCategory.Main, "=".PadRight(80, '='));

                // Log version
                if (!string.IsNullOrEmpty(version))
                {
                    Info(LogCategory.Main, $"Version: {version}");
                }

                // Log environment info
                Info(LogCategory.Main, $"Date/Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Info(LogCategory.Main, $"OS: {Environment.OSVersion}");
                Info(LogCategory.Main, $"Machine: {Environment.MachineName}");
                Info(LogCategory.Main, $"User: {Environment.UserName}");
                Info(LogCategory.Main, $"CLR Version: {Environment.Version}");
                Info(LogCategory.Main, $"Log Directory: {LogDirectory}");

                // Log Revit info (will be added by caller)

                Info(LogCategory.Main, "-".PadRight(80, '-'));

                // Clean old logs
                CleanOldLogs();

                Info(LogCategory.Main, "Logger initialized successfully");
            }
            catch (Exception ex)
            {
                // If initialization fails, log to Errors at least
                Error(LogCategory.Errors, "Logger initialization failed", ex);
            }
        }

        /// <summary>
        /// Logs Revit-specific information
        /// </summary>
        public static void LogRevitInfo(string revitVersion, string documentName = null)
        {
            try
            {
                Info(LogCategory.Main, $"Revit Version: {revitVersion}");

                if (!string.IsNullOrEmpty(documentName))
                {
                    Info(LogCategory.Main, $"Document: {documentName}");
                }
            }
            catch (Exception ex)
            {
                Error(LogCategory.Main, "Failed to log Revit info", ex);
            }
        }

        /// <summary>
        /// Logs application shutdown
        /// </summary>
        public static void Shutdown()
        {
            try
            {
                Info(LogCategory.Main, "-".PadRight(80, '-'));
                Info(LogCategory.Main, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - SESSION ENDED");
                Info(LogCategory.Main, "=".PadRight(80, '='));
                WriteSeparator(LogCategory.Main);
            }
            catch
            {
                // Silent fail
            }
        }

        /// <summary>
        /// Writes a separator line to log
        /// </summary>
        private static void WriteSeparator(LogCategory category)
        {
            try
            {
                string logFile = GetLogFilePath(category);
                string separator = Environment.NewLine + Environment.NewLine;

                lock (LockObject)
                {
                    File.AppendAllText(logFile, separator, Encoding.UTF8);
                }
            }
            catch { }
        }

        #endregion

        #region Maintenance Methods

        /// <summary>
        /// Cleans old log files (older than specified days)
        /// </summary>
        public static void CleanOldLogs(int daysToKeep = 30)
        {
            try
            {
                Logger.Info(LogCategory.Main, $"Cleaning logs older than {daysToKeep} days");

                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                var logFiles = Directory.GetFiles(LogDirectory, "*.log");

                int deletedCount = 0;
                foreach (var file in logFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < cutoffDate)
                    {
                        File.Delete(file);
                        deletedCount++;
                    }
                }

                Logger.Info(LogCategory.Main, $"Cleaned {deletedCount} old log files");
            }
            catch (Exception ex)
            {
                Logger.Error(LogCategory.Main, "Failed to clean old logs", ex);
            }
        }

        /// <summary>
        /// Gets current log directory path
        /// </summary>
        public static string GetLogDirectory()
        {
            return LogDirectory;
        }

        #endregion
    }
}