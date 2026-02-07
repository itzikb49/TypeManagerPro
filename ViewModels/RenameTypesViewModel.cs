// RenameTypesViewModel.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Autodesk.Revit.DB;
using TypeManagerPro.Models;
using TypeManagerPro.Helpers;

namespace TypeManagerPro.ViewModels
{
    /// <summary>
    /// ViewModel for Rename Types tab - handles all logic and data binding
    /// </summary>
    public class RenameTypesViewModel : INotifyPropertyChanged
    {
        #region Private Fields

        private readonly Document _document;
        private ObservableCollection<TypeItem> _allTypes;
        private ObservableCollection<TypeItem> _filteredTypes;
        private string _selectedCategory;
        private string _searchText;
        private string _findText;
        private string _replaceText;
        private string _prefixText;
        private string _suffixText;
        private int _totalCount;
        private int _selectedCount;
        private int _willChangeCount;
        private bool _ignoreCase;
        private bool _useRegex;

        #endregion

        #region Public Properties

        /// <summary>
        /// All types (unfiltered)
        /// </summary>
        public ObservableCollection<TypeItem> AllTypes
        {
            get => _allTypes;
            set
            {
                _allTypes = value;
                OnPropertyChanged(nameof(AllTypes));
            }
        }

        /// <summary>
        /// Available categories (loaded dynamically from document)
        /// </summary>
        public ObservableCollection<string> AvailableCategories { get; set; }

        /// <summary>
        /// Filtered types (what's displayed)
        /// </summary>
        public ObservableCollection<TypeItem> FilteredTypes
        {
            get => _filteredTypes;
            set
            {
                _filteredTypes = value;
                OnPropertyChanged(nameof(FilteredTypes));
            }
        }

        /// <summary>
        /// Selected category (e.g., "Walls")
        /// </summary>
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory != value)
                {
                    _selectedCategory = value;
                    OnPropertyChanged(nameof(SelectedCategory));
                    LoadTypesByCategory();
                }
            }
        }

        /// <summary>
        /// Search text for filtering
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    ApplyFilters();
                }
            }
        }

        /// <summary>
        /// Find text for Find/Replace operation
        /// </summary>
        public string FindText
        {
            get => _findText;
            set
            {
                if (_findText != value)
                {
                    _findText = value;
                    OnPropertyChanged(nameof(FindText));
                    UpdateAllPreviews();
                }
            }
        }

        /// <summary>
        /// Replace text for Find/Replace operation
        /// </summary>
        public string ReplaceText
        {
            get => _replaceText;
            set
            {
                if (_replaceText != value)
                {
                    _replaceText = value;
                    OnPropertyChanged(nameof(ReplaceText));
                    UpdateAllPreviews();
                }
            }
        }

        /// <summary>
        /// Prefix to add
        /// </summary>
        public string PrefixText
        {
            get => _prefixText;
            set
            {
                if (_prefixText != value)
                {
                    _prefixText = value;
                    OnPropertyChanged(nameof(PrefixText));
                    UpdateAllPreviews();
                }
            }
        }

        /// <summary>
        /// Suffix to add
        /// </summary>
        public string SuffixText
        {
            get => _suffixText;
            set
            {
                if (_suffixText != value)
                {
                    _suffixText = value;
                    OnPropertyChanged(nameof(SuffixText));
                    UpdateAllPreviews();
                }
            }
        }

        /// <summary>
        /// Total types count
        /// </summary>
        public int TotalCount
        {
            get => _totalCount;
            set
            {
                _totalCount = value;
                OnPropertyChanged(nameof(TotalCount));
            }
        }

        /// <summary>
        /// Selected types count
        /// </summary>
        public int SelectedCount
        {
            get => _selectedCount;
            set
            {
                _selectedCount = value;
                OnPropertyChanged(nameof(SelectedCount));
            }
        }

        /// <summary>
        /// Will change count
        /// </summary>
        public int WillChangeCount
        {
            get => _willChangeCount;
            set
            {
                _willChangeCount = value;
                OnPropertyChanged(nameof(WillChangeCount));
                OnPropertyChanged(nameof(ExecuteButtonColor));
            }
        }

        /// <summary>
        /// Execute button color - ירוד כשיש שינויים, אדום כשין
        /// </summary>
        public string ExecuteButtonColor => _willChangeCount > 0 ? "#27AE60" : "#E74C3C";

        /// <summary>
        /// Ignore case when searching
        /// </summary>
        public bool IgnoreCase
        {
            get => _ignoreCase;
            set
            {
                if (_ignoreCase != value)
                {
                    _ignoreCase = value;
                    OnPropertyChanged(nameof(IgnoreCase));
                    UpdateAllPreviews();
                }
            }
        }

        /// <summary>
        /// Use Regex (Wild Cards)
        /// </summary>
        public bool UseRegex
        {
            get => _useRegex;
            set
            {
                if (_useRegex != value)
                {
                    _useRegex = value;
                    OnPropertyChanged(nameof(UseRegex));
                    UpdateAllPreviews();
                }
            }
        }

        #endregion

        #region Commands

        public ICommand SelectAllCommand { get; }
        public ICommand DeselectAllCommand { get; }
        public ICommand ExecuteCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand ExportCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new RenameTypesViewModel
        /// </summary>
        public RenameTypesViewModel(Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));

            // Initialize collections
            AllTypes = new ObservableCollection<TypeItem>();
            FilteredTypes = new ObservableCollection<TypeItem>();
            AvailableCategories = new ObservableCollection<string>();

            // Initialize commands
            SelectAllCommand = new RelayCommand(SelectAll);
            DeselectAllCommand = new RelayCommand(DeselectAll);
            ExecuteCommand = new RelayCommand(Execute, CanExecute);
            ImportCommand = new RelayCommand(Import);
            ExportCommand = new RelayCommand(Export, CanExport);

            // Load available categories from document
            LoadAvailableCategories();

            // Set default category (first one if available)
            if (AvailableCategories.Count > 0)
            {
                // Try to find Walls, otherwise use first
                SelectedCategory = AvailableCategories.Contains("Walls")
                    ? "Walls"
                    : AvailableCategories[0];
            }
        }

        #endregion

        #region Load Categories

        /// <summary>
        /// Loads available categories from document
        /// </summary>
        private void LoadAvailableCategories()
        {
            Logger.Info(Logger.LogCategory.Rename, "Loading available categories");

            try
            {
                var categories = RenameHelper.GetAvailableCategories(_document);

                AvailableCategories.Clear();
                foreach (var category in categories)
                {
                    AvailableCategories.Add(category);
                }

                Logger.Info(Logger.LogCategory.Rename,
                    $"Loaded {AvailableCategories.Count} categories");
            }
            catch (Exception ex)
            {
                Logger.Error(Logger.LogCategory.Rename,
                    "Failed to load categories", ex);
            }
        }

        #endregion

        #region Load Types

        /// <summary>
        /// Loads types by selected category
        /// </summary>
        private void LoadTypesByCategory()
        {
            Logger.MethodEntry(Logger.LogCategory.Rename, "RenameTypesViewModel", "LoadTypesByCategory");
            Logger.Info(Logger.LogCategory.Rename, $"Loading types for category: {SelectedCategory}");

            try
            {
                AllTypes.Clear();
                FilteredTypes.Clear();

                if (string.IsNullOrEmpty(SelectedCategory))
                {
                    Logger.Warning(Logger.LogCategory.Rename, "No category selected");
                    return;
                }

                // Get types using dynamic category name lookup
                var types = RenameHelper.GetTypesByCategoryName(_document, SelectedCategory);
                Logger.Info(Logger.LogCategory.Rename, $"Found {types.Count} types");

                foreach (var type in types)
                {
                    var typeItem = new TypeItem(type);

                    // DEBUG: כתוב כל שם בדיוק כמו שהוא כולל כל תו
                    Logger.Info(Logger.LogCategory.Rename,
                        $"Type loaded: [{typeItem.OriginalName}] Length={typeItem.OriginalName.Length} " +
                        $"Chars=[{string.Join(",", typeItem.OriginalName.ToCharArray().Select(c => $"'{c}'({(int)c})"))}]");

                    // Subscribe to property changes
                    typeItem.PropertyChanged += TypeItem_PropertyChanged;

                    AllTypes.Add(typeItem);
                    FilteredTypes.Add(typeItem);
                }

                UpdateStatistics();
                Logger.Info(Logger.LogCategory.Rename, "Types loaded successfully");

                // עדכן Preview לפי Find/Replace הנוכחי
                UpdateAllPreviews();
            }
            catch (Exception ex)
            {
                Logger.Error(Logger.LogCategory.Rename, "Failed to load types", ex);
            }
            finally
            {
                Logger.MethodExit(Logger.LogCategory.Rename, "RenameTypesViewModel", "LoadTypesByCategory");
            }
        }

        /// <summary>
        /// Handles property changes on TypeItems
        /// </summary>
        private void TypeItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TypeItem.IsSelected) ||
                e.PropertyName == nameof(TypeItem.WillChange))
            {
                UpdateStatistics();
            }
        }

        #endregion

        #region Filters

        /// <summary>
        /// Applies search filter
        /// </summary>
        private void ApplyFilters()
        {
            FilteredTypes.Clear();

            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? AllTypes.ToList()
                : RenameHelper.FilterBySearch(AllTypes.ToList(), SearchText, t => t.OriginalName);

            foreach (var item in filtered)
            {
                FilteredTypes.Add(item);
            }

            UpdateStatistics();
        }

        #endregion

        #region Preview Updates

        /// <summary>
        /// Updates preview for all types
        /// </summary>
        private void UpdateAllPreviews()
        {
            foreach (var typeItem in AllTypes)
            {
                UpdatePreview(typeItem);
            }

            ValidateAllNames();
            UpdateStatistics();
        }

        /// <summary>
        /// Updates preview for a single type
        /// </summary>
        private void UpdatePreview(TypeItem typeItem)
        {
            if (typeItem == null) return;

            string newName = RenameHelper.ApplyAllOperations(
                typeItem.OriginalName,
                FindText,
                ReplaceText,
                PrefixText,
                SuffixText,
                IgnoreCase,
                UseRegex);

            // DEBUG: כתוב כל שינוי בדיוק
            if (newName != typeItem.OriginalName)
            {
                Logger.Info(Logger.LogCategory.Rename,
                    $"Preview: [{typeItem.OriginalName}] → [{newName}] | Find=[{FindText}] Replace=[{ReplaceText}] IgnoreCase={IgnoreCase}");
            }

            typeItem.NewName = newName;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validates all type names
        /// </summary>
        private void ValidateAllNames()
        {
            // Get all existing names (excluding the type being validated)
            var allNewNames = AllTypes
                .Where(t => t.IsSelected && t.WillChange)
                .Select(t => t.NewName)
                .ToList();

            foreach (var typeItem in AllTypes)
            {
                if (!typeItem.IsSelected || !typeItem.WillChange)
                {
                    typeItem.HasError = false;
                    typeItem.ErrorMessage = null;
                    continue;
                }

                // Get existing names excluding this type
                var otherNames = allNewNames
                    .Where(n => n != typeItem.NewName)
                    .ToList();

                // Add names from non-selected types
                otherNames.AddRange(AllTypes
                    .Where(t => !t.IsSelected || !t.WillChange)
                    .Select(t => t.OriginalName));

                typeItem.Validate(otherNames);
            }
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Updates statistics
        /// </summary>
        private void UpdateStatistics()
        {
            var (total, selected, willChange) = RenameHelper.CalculateStatistics(
                FilteredTypes.ToList(),
                t => t.IsSelected,
                t => t.WillChange);

            TotalCount = total;
            SelectedCount = selected;
            WillChangeCount = willChange;
        }

        #endregion

        #region Commands Implementation

        /// <summary>
        /// Select all types
        /// </summary>
        private void SelectAll()
        {
            foreach (var typeItem in FilteredTypes)
            {
                typeItem.IsSelected = true;
            }
        }

        /// <summary>
        /// Deselect all types
        /// </summary>
        private void DeselectAll()
        {
            foreach (var typeItem in FilteredTypes)
            {
                typeItem.IsSelected = false;
            }
        }

        /// <summary>
        /// Can execute rename operation
        /// </summary>
        private bool CanExecute()
        {
            // Must have selected types with changes
            if (WillChangeCount == 0)
                return false;

            // Must not have any errors
            return !AllTypes.Any(t => t.IsSelected && t.HasError);
        }

        /// <summary>
        /// Executes rename operation
        /// </summary>
        private void Execute()
        {
            Logger.MethodEntry(Logger.LogCategory.Rename, "RenameTypesViewModel", "Execute");

            var typesToRename = AllTypes
                .Where(t => t.IsSelected && t.WillChange && !t.HasError)
                .ToList();

            Logger.Info(Logger.LogCategory.Rename,
                $"Starting rename operation for {typesToRename.Count} types");

            if (typesToRename.Count == 0)
            {
                Logger.Warning(Logger.LogCategory.Rename, "No types to rename");
                return;
            }

            // Check if document is workshared and if central is accessible
            bool isWorkshared = _document.IsWorkshared;
            Logger.Info(Logger.LogCategory.Rename, $"Document is workshared: {isWorkshared}");

            if (isWorkshared && !_document.IsDetached)
            {
                // Document is workshared AND not detached - check if central is accessible
                var centralPath = _document.GetWorksharingCentralModelPath();
                bool isCentralAccessible = centralPath != null;

                Logger.Info(Logger.LogCategory.Rename,
                    $"Central model accessible: {isCentralAccessible}, IsDetached: {_document.IsDetached}");

                if (!isCentralAccessible)
                {
                    Logger.Warning(Logger.LogCategory.Rename,
                        "Document is workshared but central is inaccessible");

                    System.Windows.MessageBox.Show(
                        "This document is a Workshared file but the Central Model is not accessible.\n\n" +
                        "Types in workshared documents can only be modified when connected to the Central Model.\n\n" +
                        "To rename types:\n" +
                        "• Open the file directly from the Central Model location, OR\n" +
                        "• Open as 'Detach from Central' to work independently",
                        "Central Model Inaccessible",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);

                    Logger.MethodExit(Logger.LogCategory.Rename, "RenameTypesViewModel", "Execute");
                    return;
                }
            }

            // If document is detached, it's now a standalone file - safe to modify
            bool treatAsStandalone = !isWorkshared || _document.IsDetached;
            Logger.Info(Logger.LogCategory.Rename, $"Treat as standalone: {treatAsStandalone}");

            int renamed = 0;
            int failed = 0;
            int skipped = 0;
            var failedTypes = new System.Text.StringBuilder();
            var skippedTypes = new System.Text.StringBuilder();

            using (Transaction trans = new Transaction(_document, "Rename Types"))
            {
                trans.Start();
                Logger.Info(Logger.LogCategory.Rename, "Transaction started");

                foreach (var typeItem in typesToRename)
                {
                    try
                    {
                        // For workshared documents that are NOT detached, check ownership
                        if (isWorkshared && !_document.IsDetached)
                        {
                            var checkoutStatus = WorksharingUtils.GetCheckoutStatus(_document, typeItem.RevitType.Id);

                            if (checkoutStatus == CheckoutStatus.OwnedByOtherUser)
                            {
                                skipped++;
                                Logger.Warning(Logger.LogCategory.Rename,
                                    $"Skipped (owned by other user): '{typeItem.OriginalName}'");
                                skippedTypes.AppendLine($"• {typeItem.OriginalName} (Owned by another user)");
                                continue;
                            }

                            if (checkoutStatus == CheckoutStatus.NotOwned)
                            {
                                // Try to request ownership
                                try
                                {
                                    WorksharingUtils.CheckoutElements(_document,
                                        new List<ElementId> { typeItem.RevitType.Id });
                                    Logger.Info(Logger.LogCategory.Rename,
                                        $"Checked out element: '{typeItem.OriginalName}'");
                                }
                                catch (Exception checkoutEx)
                                {
                                    skipped++;
                                    Logger.Warning(Logger.LogCategory.Rename,
                                        $"Skipped (cannot checkout): '{typeItem.OriginalName}'", checkoutEx.Message);
                                    skippedTypes.AppendLine($"• {typeItem.OriginalName} (Cannot checkout)");
                                    continue;
                                }
                            }
                        }

                        Logger.Debug(Logger.LogCategory.Rename,
                            $"Renaming: '{typeItem.OriginalName}' -> '{typeItem.NewName}'");

                        typeItem.RevitType.Name = typeItem.NewName;
                        renamed++;

                        Logger.Info(Logger.LogCategory.Rename,
                            $"Successfully renamed: '{typeItem.OriginalName}' -> '{typeItem.NewName}'");
                    }
                    catch (Autodesk.Revit.Exceptions.ArgumentException ex)
                    {
                        failed++;
                        Logger.Error(Logger.LogCategory.Rename,
                            $"Failed to rename: '{typeItem.OriginalName}' - Invalid name", ex);
                        failedTypes.AppendLine($"• {typeItem.OriginalName} (Invalid name)");
                    }
                    catch (Autodesk.Revit.Exceptions.InvalidOperationException ex)
                    {
                        failed++;
                        Logger.Error(Logger.LogCategory.Rename,
                            $"Failed to rename: '{typeItem.OriginalName}' - Cannot modify", ex);
                        failedTypes.AppendLine($"• {typeItem.OriginalName} (Cannot modify)");
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        Logger.Error(Logger.LogCategory.Rename,
                            $"Failed to rename: '{typeItem.OriginalName}'", ex);
                        failedTypes.AppendLine($"• {typeItem.OriginalName} ({ex.Message})");
                    }
                }

                trans.Commit();
                Logger.Info(Logger.LogCategory.Rename, "Transaction committed");
            }

            Logger.Info(Logger.LogCategory.Rename,
                $"Rename operation completed. Success: {renamed}, Failed: {failed}, Skipped: {skipped}");

            // Show detailed results
            string message = $"✓ Successfully renamed: {renamed}\n";
            if (skipped > 0)
                message += $"⊘ Skipped (no permission): {skipped}\n";
            if (failed > 0)
                message += $"✗ Failed: {failed}\n";

            if (skippedTypes.Length > 0)
            {
                message += "\nSkipped types (need permission):\n" + skippedTypes.ToString();
            }

            if (failedTypes.Length > 0)
            {
                message += "\nFailed types:\n" + failedTypes.ToString();
            }

            System.Windows.MessageBox.Show(
                message,
                "Rename Complete",
                System.Windows.MessageBoxButton.OK,
                failed > 0 || skipped > 0
                    ? System.Windows.MessageBoxImage.Warning
                    : System.Windows.MessageBoxImage.Information);

            // Reload types
            LoadTypesByCategory();
            Logger.MethodExit(Logger.LogCategory.Rename, "RenameTypesViewModel", "Execute");
        }

        /// <summary>
        /// Can export
        /// </summary>
        private bool CanExport()
        {
            return AllTypes.Count > 0;
        }

        /// <summary>
        /// Export to Excel (placeholder)
        /// </summary>
        private void Export()
        {
            // TODO: Implement Excel export
            System.Windows.MessageBox.Show(
                "Excel export will be implemented here",
                "Export",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        /// <summary>
        /// Import from Excel (placeholder)
        /// </summary>
        private void Import()
        {
            // TODO: Implement Excel import
            System.Windows.MessageBox.Show(
                "Excel import will be implemented here",
                "Import",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    #region RelayCommand Helper Class

    /// <summary>
    /// Simple ICommand implementation
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }
    }

    #endregion
}