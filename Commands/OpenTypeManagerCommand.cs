using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using TypeManagerPro.Helpers;
using TypeManagerPro.Views;

namespace TypeManagerPro.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class OpenTypeManagerCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                // Log command execution
                Logger.Info(Logger.LogCategory.Main, "Command: OpenTypeManager executed");

                // Get the current document
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc?.Document;

                // Check if document is open
                if (doc == null)
                {
                    Logger.Warning(Logger.LogCategory.Main, "No document open");
                    TaskDialog.Show("Type Manager Pro",
                        "Please open a Revit project first.");
                    return Result.Failed;
                }

                Logger.Info(Logger.LogCategory.Main, $"Opening window for document: {doc.Title}");

                // Open the main window with document
                MainWindow window = new MainWindow(doc);  // ✅ העבר את doc!
                window.ShowDialog();

                Logger.Info(Logger.LogCategory.Main, "Command completed successfully");
                return Result.Succeeded;
            }
            catch (System.Exception ex)
            {
                Logger.Error(Logger.LogCategory.Main, "Command execution failed", ex);  // ✅ Logger במקום message
                message = ex.Message;
                TaskDialog.Show("Error",
                    $"An error occurred:\n{ex.Message}");
                return Result.Failed;
            }
        }
    }
}