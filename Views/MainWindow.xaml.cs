// MainWindow.xaml.cs
using System;
using System.Reflection;
using System.Windows;
using Autodesk.Revit.DB;

namespace TypeManagerPro.Views
{
    /// <summary>
    /// Main window for Type Manager Pro application
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Document _document;

        /// <summary>
        /// Constructor with Revit Document
        /// </summary>
        public MainWindow(Document document)
        {
            InitializeComponent();

            _document = document ?? throw new ArgumentNullException(nameof(document));

            LoadVersion();

            // Initialize tabs after window is fully loaded
            this.Loaded += MainWindow_Loaded;
        }

        /// <summary>
        /// Window loaded event - ensures all XAML elements are ready
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeTabs();
        }

        /// <summary>
        /// Loads version number from assembly
        /// </summary>
        private void LoadVersion()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;

                if (this.FindName("VersionText") is System.Windows.Controls.TextBlock versionTextBlock)
                {
                    versionTextBlock.Text = $"Ver. {version.Major}.{version.Minor}.{version.Build}";
                }
            }
            catch { }
        }

        /// <summary>
        /// Initializes the tabs with Document
        /// </summary>
        private void InitializeTabs()
        {
            // Set ViewModel for RenameTypesView
            if (this.FindName("RenameTypesViewControl") is RenameTypesView renameView)
            {
                var viewModel = new ViewModels.RenameTypesViewModel(_document);
                renameView.SetViewModel(viewModel);
            }
        }

        /// <summary>
        /// Help button click handler
        /// </summary>
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://itzikb49.github.io/IB-BIM-TypeManagerPro-Docs/UserGuide.html",
                    UseShellExecute = true
                });
            }
            catch
            {
                MessageBox.Show("Could not open help documentation.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Close button click handler
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}