// Logger.cs - FIXED: Clears log file on each run
using System;
using System.IO;

namespace TypeManagerPro.Helpers
{
    /// <summary>
    /// Simple file logger for debugging and error tracking
    /// Log file is cleared on each application start
    /// </summary>
    public static class Logger
    {
        private static string _logPath;
        private static StreamWriter _logWriter;
        private static readonly object _lock = new object();

        public enum LogCategory
        {
            Main,
            General,
            Rename,
            Import,
            Export,
            Validation,
            UI
        }

        /// <summary>
        /// Initializes the logger with a fresh log file
        /// </summary>
        public static void Initialize()
        {
            Initialize(null, null);
        }


        /// <summary>
        /// Initializes the logger with a fresh log file (with app name and version)
        /// </summary>
        public static void Initialize(string appName, string version)
        {
            try
            {
                // Keep the original ProgramData location
                string logFolder = @"C:\ProgramData\IB-BIM\TypeManagerPro\Logs";

                if (!Directory.Exists(logFolder))
                {
                    Directory.CreateDirectory(logFolder);
                }

                // Same log file name - overwrites on each run
                _logPath = Path.Combine(logFolder, "TypeManagerPro.log");

                // append: false = overwrite the file each time
                _logWriter = new StreamWriter(_logPath, append: false);
                _logWriter.AutoFlush = true;

                Info(LogCategory.General, $"=== Type Manager Pro Log Started - {DateTime.Now} ===");
                if (!string.IsNullOrEmpty(appName))
                {
                    Info(LogCategory.General, $"Application: {appName} v{version}");
                }
                Info(LogCategory.General, $"Log file: {_logPath}");
            }
            catch (Exception ex)
            {
                // If logging fails, silently continue
                System.Diagnostics.Debug.WriteLine($"Logger initialization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Closes the log file
        /// </summary>
        public static void Close()
        {
            try
            {
                if (_logWriter != null)
                {
                    Info(LogCategory.General, "=== Log Closed ===");
                    _logWriter.Close();
                    _logWriter.Dispose();
                    _logWriter = null;
                }
            }
            catch { }
        }

        /// <summary>
        /// Alias for Close() - for backward compatibility
        /// </summary>
        public static void Shutdown()
        {
            Close();
        }


        /// <summary>
        /// Logs Revit version information
        /// </summary>
        public static void LogRevitInfo(Autodesk.Revit.ApplicationServices.Application app)
        {
            try
            {
                if (app != null)
                {
                    Info(LogCategory.General, $"Revit Version: {app.VersionName} ({app.VersionBuild})");
                    Info(LogCategory.General, $"Revit Language: {app.Language}");
                }
            }
            catch (Exception ex)
            {
                Warning(LogCategory.General, "Could not log Revit info", ex.Message);
            }
        }

        public static void LogRevitInfo( Autodesk.Revit.ApplicationServices.ControlledApplication app)
        {
            try
            {
                if (app != null)
                {
                    Info(LogCategory.General, $"Revit Version: {app.VersionName}");
                    Info(LogCategory.General, $"Revit Language: {app.Language}");
                }
            }
            catch (Exception ex)
            {
                Warning(LogCategory.General, "Could not log Revit info", ex.Message);
            }
        }

        /// <summary>
        /// Logs an informational message
        /// </summary>
        public static void Info(LogCategory category, string message)
        {
            WriteLog("INFO", category, message);
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        public static void Warning(LogCategory category, string message, string details = null)
        {
            WriteLog("WARN", category, message + (details != null ? $" - {details}" : ""));
        }

        /// <summary>
        /// Logs an error message
        /// </summary>
        public static void Error(LogCategory category, string message, Exception ex = null)
        {
            string errorMsg = message;
            if (ex != null)
            {
                errorMsg += $" | Exception: {ex.GetType().Name} - {ex.Message}";
                if (ex.StackTrace != null)
                {
                    errorMsg += $"\nStack: {ex.StackTrace}";
                }
            }
            WriteLog("ERROR", category, errorMsg);
        }

        /// <summary>
        /// Logs a debug message
        /// </summary>
        public static void Debug(LogCategory category, string message)
        {
#if DEBUG
            WriteLog("DEBUG", category, message);
#endif
        }

        /// <summary>
        /// Logs a method entry
        /// </summary>
        public static void MethodEntry(LogCategory category, string className, string methodName)
        {
#if DEBUG
            WriteLog("DEBUG", category, $"→ {className}.{methodName}()");
#endif
        }

        /// <summary>
        /// Logs a method exit
        /// </summary>
        public static void MethodExit(LogCategory category, string className, string methodName)
        {
#if DEBUG
            WriteLog("DEBUG", category, $"← {className}.{methodName}()");
#endif
        }

        /// <summary>
        /// Core logging method
        /// </summary>
        private static void WriteLog(string level, LogCategory category, string message)
        {
            if (_logWriter == null)
            {
                return;
            }

            try
            {
                lock (_lock)
                {
                    string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    string logEntry = $"[{timestamp}] [{level,-5}] [{category,-12}] {message}";
                    _logWriter.WriteLine(logEntry);
                }
            }
            catch
            {
                // If logging fails, silently continue
            }
        }

        /// <summary>
        /// Gets the path to the current log file
        /// </summary>
        public static string GetLogPath()
        {
            return _logPath;
        }
    }
}