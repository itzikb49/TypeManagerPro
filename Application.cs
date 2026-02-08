// Application.cs
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using TypeManagerPro.Helpers;

namespace TypeManagerPro
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Application : IExternalApplication
    {
        // נתיב מרכזי ללוגים
        public static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "IB-BIM",
            "TypeManagerPro",
            "Logs"
        );

        public static bool EnableLogging = true;

        /*
        /// <summary>
        /// מחיקת קבצי לוג - מתבצעת בתחילת כל הרצה
        /// </summary>
        public static void ClearLogFiles()
        {
            try
            {
                // בדוק אם התיקייה קיימת, אם לא - צור אותה
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                    return;
                }

                string[] logFiles = {
                    Path.Combine(LogDirectory, "TypeManagerPro_Debug.log"),
                    Path.Combine(LogDirectory, "Startup_Debug.log"),
                    Path.Combine(LogDirectory, "UI_Debug.log")
                };

                foreach (string logFile in logFiles)
                {
                    if (File.Exists(logFile))
                    {
                        try
                        {
                            File.Delete(logFile);
                        }
                        catch
                        {
                            // אם הקובץ נעול, נסה לנקות את תוכנו
                            try
                            {
                                File.WriteAllText(logFile, string.Empty);
                            }
                            catch
                            {
                                // Silent fail
                            }
                        }
                    }
                }
            }
            catch
            {
                // Silent fail
            }
        }
        */

        // Application.cs - OnStartup מתוקן

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // ========================================
                // Initialize Logger - FIRST THING!
                // ========================================
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                string versionString = $"{version.Major}.{version.Minor}.{version.Build}";

                Logger.Initialize("Type Manager Pro", versionString);
                Logger.LogRevitInfo(application.ControlledApplication);

                Logger.Info(Logger.LogCategory.Main, "=== Type Manager Pro Startup ===");
                Logger.Info(Logger.LogCategory.Main, $"Assembly: {assembly.Location}");

                // ========================================
                // Create Ribbon Tab and Panel
                // ========================================
                string tabName = "IB-BIM Tools";

                // יצירת הטאב אם הוא לא קיים
                try
                {
                    application.CreateRibbonTab(tabName);
                    Logger.Info(Logger.LogCategory.Main, $"Created tab: {tabName}");
                }
                catch
                {
                    Logger.Info(Logger.LogCategory.Main, $"Tab '{tabName}' already exists");
                }

                // יצירת הפאנל תחת הטאב
                RibbonPanel panel = application.CreateRibbonPanel(tabName, "Type Management");
                Logger.Info(Logger.LogCategory.Main, "Created panel: Type Management");

                // הוספת הכפתור
                AddPushButtonTypeManager(panel);

                Logger.Info(Logger.LogCategory.Main, "Type Manager Pro startup completed successfully");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.Error(Logger.LogCategory.Main, "Startup failed", ex);
                TaskDialog.Show("Error", $"Failed to create ribbon: {ex.Message}");
                return Result.Failed;
            }
        }


        public Result OnShutdown(UIControlledApplication application)
        {
            try
            {
                Logger.Info(Logger.LogCategory.Main, "Application shutting down");

                // Cleanup code here if needed...

                Logger.Shutdown();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.Error(Logger.LogCategory.Main, "Shutdown failed", ex);
                return Result.Failed;
            }
        }

        private void AddPushButtonTypeManager(RibbonPanel panel)
        {
            try
            {
                WriteLog("AddPushButtonTypeManager started");

                string path = Assembly.GetExecutingAssembly().Location;
                WriteLog($"Assembly path: {path}");

                PushButtonData buttonData = new PushButtonData(
                    "TypeManagerPro",
                    "Type\nManager Pro",
                    path,
                    "TypeManagerPro.Commands.OpenTypeManagerCommand"
                );
                WriteLog("PushButtonData created");

                // Load the icon (32x32 for large button)
                BitmapSource iconLarge = GetEmbeddedImage("TypeManagerPro.Resources.type_manager_32.png");

                if (iconLarge != null)
                {
                    WriteLog("Icon loaded successfully, setting to button");
                    buttonData.LargeImage = iconLarge;
                }
                else
                {
                    WriteLog("Icon is null - button will have no image");
                }

                PushButton pushButton = panel.AddItem(buttonData) as PushButton;
                WriteLog($"PushButton added to panel: {pushButton != null}");

                // Tooltip and description
                pushButton.ToolTip = "Complete Type Management Solution";
                pushButton.LongDescription = "Rename multiple element types using Find/Replace or Prefix/Suffix. " +
                                            "Replace one type with another across all instances. " +
                                            "Support for 25+ categories with live preview.";

                // F1 Help - Autodesk Store requirement
                // יש לעדכן URL לאחר פרסום הדוקומנטציה
                ContextualHelp help = new ContextualHelp(
                    ContextualHelpType.Url,
                    "https://itzikb49.github.io/IB-BIM-TypeManagerPro-Docs/UserGuide.html"
                );
                pushButton.SetContextualHelp(help);

                WriteLog("AddPushButtonTypeManager completed successfully");
            }
            catch (Exception ex)
            {
                WriteLog($"Exception in AddPushButtonTypeManager: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private static BitmapSource GetEmbeddedImage(string name)
        {
            try
            {
                WriteLog($"GetEmbeddedImage called with: {name}");

                Assembly assembly = Assembly.GetExecutingAssembly();

                // רשום את כל המשאבים
                string[] resources = assembly.GetManifestResourceNames();
                WriteLog($"Available resources: {string.Join(", ", resources)}");

                Stream stream = assembly.GetManifestResourceStream(name);

                if (stream != null)
                {
                    WriteLog($"Stream found for: {name}");
                    BitmapSource image = BitmapFrame.Create(stream);
                    WriteLog($"Image loaded successfully! Size: {image.PixelWidth}x{image.PixelHeight}");
                    return image;
                }
                else
                {
                    WriteLog($"Stream is null for: {name}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                WriteLog($"Exception in GetEmbeddedImage: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        // מתודה לכתיבת לוג
        /// <summary>
        /// Legacy WriteLog - redirects to Logger
        /// </summary>
        private static void WriteLog(string message)
        {
            Logger.Info(Logger.LogCategory.Main, message);
        }
    }
}