// RenameTypesView.xaml.cs
using System.Windows.Controls;
using Autodesk.Revit.DB;
using TypeManagerPro.ViewModels;

namespace TypeManagerPro.Views
{
    /// <summary>
    /// Interaction logic for RenameTypesView.xaml
    /// Code-Behind is kept minimal - most logic is in ViewModel
    /// </summary>
    public partial class RenameTypesView : UserControl
    {
        private RenameTypesViewModel _viewModel;

        /// <summary>
        /// Default constructor (for XAML designer)
        /// </summary>
        public RenameTypesView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Constructor with Document (used at runtime)
        /// </summary>
        public RenameTypesView(Document document) : this()
        {
            if (document != null)
            {
                _viewModel = new RenameTypesViewModel(document);
                this.DataContext = _viewModel;
            }
        }

        /// <summary>
        /// Sets the ViewModel (alternative to constructor)
        /// </summary>
        public void SetViewModel(RenameTypesViewModel viewModel)
        {
            _viewModel = viewModel;
            this.DataContext = _viewModel;
        }
    }
}