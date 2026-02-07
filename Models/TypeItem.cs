// TypeItem.cs
using System.ComponentModel;
using Autodesk.Revit.DB;

namespace TypeManagerPro.Models
{
    /// <summary>
    /// Represents a single element type with its properties and rename preview
    /// </summary>
    public class TypeItem : INotifyPropertyChanged
    {
        #region Private Fields

        private bool _isSelected;
        private string _newName;
        private bool _hasError;
        private string _errorMessage;

        #endregion

        #region Public Properties

        /// <summary>
        /// The Revit ElementType
        /// </summary>
        public ElementType RevitType { get; set; }

        /// <summary>
        /// Original type name
        /// </summary>
        public string OriginalName { get; set; }

        /// <summary>
        /// Category name (e.g., "Walls", "Doors")
        /// </summary>
        public string CategoryName { get; set; }

        /// <summary>
        /// Family name
        /// </summary>
        public string FamilyName { get; set; }

        /// <summary>
        /// Is this type selected for renaming
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        /// <summary>
        /// New name after applying operations
        /// </summary>
        public string NewName
        {
            get => _newName;
            set
            {
                _newName = value;
                OnPropertyChanged(nameof(NewName));
                OnPropertyChanged(nameof(WillChange));
            }
        }

        /// <summary>
        /// Will this type's name change
        /// </summary>
        public bool WillChange => IsSelected && !string.IsNullOrEmpty(NewName) && NewName != OriginalName;

        /// <summary>
        /// Has validation error (duplicate name, invalid characters, etc.)
        /// </summary>
        public bool HasError
        {
            get => _hasError;
            set
            {
                if (_hasError != value)
                {
                    _hasError = value;
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        /// <summary>
        /// Error message if HasError is true
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged(nameof(ErrorMessage));
                }
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new TypeItem from a Revit ElementType
        /// </summary>
        public TypeItem(ElementType elementType)
        {
            RevitType = elementType;
            OriginalName = elementType.Name;
            FamilyName = elementType.FamilyName;

            // Get category name
            if (elementType.Category != null)
            {
                CategoryName = elementType.Category.Name;
            }

            // Initially not selected
            IsSelected = false;
            NewName = OriginalName;
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Validates the new name
        /// </summary>
        public bool Validate(System.Collections.Generic.List<string> existingNames)
        {
            HasError = false;
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(NewName))
            {
                HasError = true;
                ErrorMessage = "Name cannot be empty";
                return false;
            }

            // Check for invalid characters
            char[] invalidChars = { '\\', '/', ':', ';', '<', '>', '?', '`', '|', '*', '"' };
            if (NewName.IndexOfAny(invalidChars) >= 0)
            {
                HasError = true;
                ErrorMessage = "Contains invalid characters";
                return false;
            }

            // Check for duplicate names (only if name changed)
            if (WillChange && existingNames.Contains(NewName))
            {
                HasError = true;
                ErrorMessage = "Duplicate name";
                return false;
            }

            return true;
        }

        #endregion
    }
}