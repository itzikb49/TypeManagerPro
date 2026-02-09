// RenameHelper.cs
using System;
using System.Collections.Generic;
using System.Linq;
// using System.Text.RegularExpressions;
using Autodesk.Revit.DB;

namespace TypeManagerPro.Helpers
{
    /// <summary>
    /// Helper class for rename operations, calculations, and validations
    /// </summary>
    public static class RenameHelper
    {
        #region Get Types from Document

        /// <summary>
        /// Gets all element types from the document by category
        /// </summary>
        public static List<ElementType> GetTypesByCategory(Document doc, BuiltInCategory category)
        {
            if (doc == null) return new List<ElementType>();

            var collector = new FilteredElementCollector(doc)
                .OfClass(typeof(ElementType))
                .OfCategory(category);

            return collector
                .Cast<ElementType>()
                .Where(t => t != null && !string.IsNullOrEmpty(t.Name))
                .OrderBy(t => t.Name)
                .ToList();
        }

        /// <summary>
        /// Gets all element types from the document (all categories)
        /// </summary>
        public static List<ElementType> GetAllTypes(Document doc)
        {
            if (doc == null) return new List<ElementType>();

            var collector = new FilteredElementCollector(doc)
                .OfClass(typeof(ElementType));

            return collector
                .Cast<ElementType>()
                .Where(t => t != null &&
                           t.Category != null &&
                           !string.IsNullOrEmpty(t.Name))
                .OrderBy(t => t.Category.Name)
                .ThenBy(t => t.Name)
                .ToList();
        }

        #endregion

        #region Get Categories Dynamically

        /// <summary>
        /// Gets all categories that have element types in the document
        /// </summary>
        public static List<string> GetAvailableCategories(Document doc)
        {
            if (doc == null) return new List<string>();

            try
            {
                var categories = new HashSet<string>();

                // Get all element types
                var collector = new FilteredElementCollector(doc)
                    .OfClass(typeof(ElementType));

                foreach (ElementType type in collector)
                {
                    if (type.Category != null && !string.IsNullOrEmpty(type.Category.Name))
                    {
                        categories.Add(type.Category.Name);
                    }
                }

                // Return sorted list
                return categories.OrderBy(c => c).ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Gets all element types by category name (dynamic lookup)
        /// </summary>
        public static List<ElementType> GetTypesByCategoryName(Document doc, string categoryName)
        {
            if (doc == null || string.IsNullOrEmpty(categoryName))
                return new List<ElementType>();

            try
            {
                var collector = new FilteredElementCollector(doc)
                    .OfClass(typeof(ElementType));

                return collector
                    .Cast<ElementType>()
                    .Where(t => t != null &&
                               t.Category != null &&
                               t.Category.Name == categoryName &&
                               !string.IsNullOrEmpty(t.Name))
                    .OrderBy(t => t.Name)
                    .ToList();
            }
            catch
            {
                return new List<ElementType>();
            }
        }

        #endregion

        #region Name Operations

        /// <summary>
        /// Applies Find/Replace operation to a name
        /// </summary>
        public static string ApplyFindReplace(string originalName, string find, string replace,
            bool ignoreCase = false, bool useRegex = false)
        {
            if (string.IsNullOrEmpty(originalName) || string.IsNullOrEmpty(find))
                return originalName;

            try
            {
                // Auto-detect Wild Cards (? or *)
                bool hasWildCards = find.Contains('?') || find.Contains('*');

                if (hasWildCards)
                {
                    // Convert Wild Cards to Regex:
                    // 1. Escape all Regex special chars first
                    // 2. Then convert ? → . and * → .*
                    string regexPattern = System.Text.RegularExpressions.Regex.Escape(find);
                    regexPattern = regexPattern.Replace("\\?", ".").Replace("\\*", ".*");

                    var options = ignoreCase
                        ? System.Text.RegularExpressions.RegexOptions.IgnoreCase
                        : System.Text.RegularExpressions.RegexOptions.None;

                    return System.Text.RegularExpressions.Regex.Replace(
                        originalName, regexPattern, replace ?? string.Empty, options);
                }
                else if (ignoreCase)
                {
                    // Case-insensitive simple replace
                    int index = originalName.IndexOf(find, StringComparison.OrdinalIgnoreCase);
                    if (index < 0) return originalName;

                    string result = originalName;
                    while (index >= 0)
                    {
                        result = result.Substring(0, index) + (replace ?? string.Empty) + result.Substring(index + find.Length);
                        index = result.IndexOf(find, index + (replace ?? string.Empty).Length, StringComparison.OrdinalIgnoreCase);
                    }
                    return result;
                }
                else
                {
                    // Default: exact match
                    return originalName.Replace(find, replace ?? string.Empty);
                }
            }
            catch (Exception ex)
            {
                // Invalid pattern
                Logger.Error(Logger.LogCategory.Rename, $"Invalid pattern: '{find}'", ex);
                return originalName;
            }
        }

        /// <summary>
        /// Applies Prefix to a name
        /// </summary>
        public static string ApplyPrefix(string originalName, string prefix)
        {
            if (string.IsNullOrEmpty(originalName) || string.IsNullOrEmpty(prefix))
                return originalName;

            // Don't add prefix if it already exists
            if (originalName.StartsWith(prefix))
                return originalName;

            return prefix + originalName;
        }

        /// <summary>
        /// Applies Suffix to a name
        /// </summary>
        public static string ApplySuffix(string originalName, string suffix)
        {
            if (string.IsNullOrEmpty(originalName) || string.IsNullOrEmpty(suffix))
                return originalName;

            // Don't add suffix if it already exists
            if (originalName.EndsWith(suffix))
                return originalName;

            return originalName + suffix;
        }

        /// <summary>
        /// Applies all rename operations in order: Find/Replace, then Prefix, then Suffix
        /// </summary>
        public static string ApplyAllOperations(string originalName, string find, string replace,
            string prefix, string suffix, bool ignoreCase = false, bool useRegex = false)
        {
            string result = originalName;

            // Step 1: Find/Replace (with IgnoreCase / Regex support)
            if (!string.IsNullOrEmpty(find))
            {
                result = ApplyFindReplace(result, find, replace, ignoreCase, useRegex);
            }

            // Step 2: Prefix
            if (!string.IsNullOrEmpty(prefix))
            {
                result = ApplyPrefix(result, prefix);
            }

            // Step 3: Suffix
            if (!string.IsNullOrEmpty(suffix))
            {
                result = ApplySuffix(result, suffix);
            }

            // Return result without trimming - display names exactly as requested

            return result;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Invalid characters for Revit element names
        /// </summary>
        public static readonly char[] InvalidCharacters =
            { '\\', '/', ':', ';', '<', '>', '?', '`', '|', '*', '"' };

        /// <summary>
        /// Checks if a name contains invalid characters
        /// </summary>
        public static bool ContainsInvalidCharacters(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            return name.IndexOfAny(InvalidCharacters) >= 0;
        }

        /// <summary>
        /// Gets list of invalid characters found in a name
        /// </summary>
        public static List<char> GetInvalidCharacters(string name)
        {
            if (string.IsNullOrEmpty(name))
                return new List<char>();

            return name.Where(c => InvalidCharacters.Contains(c)).Distinct().ToList();
        }

        /// <summary>
        /// Validates a type name
        /// </summary>
        public static (bool IsValid, string ErrorMessage) ValidateName(string name,
            List<string> existingNames = null)
        {
            // Check for empty name
            if (string.IsNullOrWhiteSpace(name))
            {
                return (false, "Name cannot be empty");
            }

            // Check for invalid characters
            if (ContainsInvalidCharacters(name))
            {
                var invalidChars = GetInvalidCharacters(name);
                var charList = string.Join(", ", invalidChars.Select(c => $"'{c}'"));
                return (false, $"Contains invalid characters: {charList}");
            }

            // Check for duplicates
            if (existingNames != null && existingNames.Contains(name))
            {
                return (false, "Name already exists");
            }

            return (true, null);
        }

        #endregion

        #region Filtering and Search

        /// <summary>
        /// Filters types by search text (case-insensitive)
        /// </summary>
        public static List<T> FilterBySearch<T>(List<T> items, string searchText,
            Func<T, string> getNameFunc)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return items;

            searchText = searchText.ToLower();

            return items.Where(item =>
            {
                var name = getNameFunc(item);
                return !string.IsNullOrEmpty(name) && name.ToLower().Contains(searchText);
            }).ToList();
        }

        #endregion

        #region Category Helpers

        /// <summary>
        /// Gets BuiltInCategory from category name
        /// </summary>
        public static BuiltInCategory? GetBuiltInCategory(string categoryName)
        {
            var mapping = new Dictionary<string, BuiltInCategory>
            {
                { "Walls", BuiltInCategory.OST_Walls },
                { "Doors", BuiltInCategory.OST_Doors },
                { "Windows", BuiltInCategory.OST_Windows },
                { "Floors", BuiltInCategory.OST_Floors },
                { "Ceilings", BuiltInCategory.OST_Ceilings },
                { "Roofs", BuiltInCategory.OST_Roofs },
                { "Structural Framing", BuiltInCategory.OST_StructuralFraming },
                { "Structural Columns", BuiltInCategory.OST_StructuralColumns },
                { "Pipes", BuiltInCategory.OST_PipeCurves },
                { "Ducts", BuiltInCategory.OST_DuctCurves },
                { "Cable Trays", BuiltInCategory.OST_CableTray },
                { "Conduits", BuiltInCategory.OST_Conduit }
            };

            return mapping.ContainsKey(categoryName) ? mapping[categoryName] : (BuiltInCategory?)null;
        }

        /// <summary>
        /// Gets display name for category (with emoji)
        /// </summary>
        public static string GetCategoryDisplayName(BuiltInCategory category)
        {
            var mapping = new Dictionary<BuiltInCategory, string>
            {
                { BuiltInCategory.OST_Walls, "🧱 Walls" },
                { BuiltInCategory.OST_Doors, "🚪 Doors" },
                { BuiltInCategory.OST_Windows, "🪟 Windows" },
                { BuiltInCategory.OST_Floors, "⬛ Floors" },
                { BuiltInCategory.OST_Ceilings, "⬜ Ceilings" },
                { BuiltInCategory.OST_Roofs, "🏠 Roofs" },
                { BuiltInCategory.OST_StructuralFraming, "🔩 Structural Framing" },
                { BuiltInCategory.OST_StructuralColumns, "🏛️ Structural Columns" },
                { BuiltInCategory.OST_PipeCurves, "🚰 Pipes" },
                { BuiltInCategory.OST_DuctCurves, "🌬️ Ducts" },
                { BuiltInCategory.OST_CableTray, "⚡ Cable Trays" },
                { BuiltInCategory.OST_Conduit, "💡 Conduits" }
            };

            return mapping.ContainsKey(category) ? mapping[category] : category.ToString();
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Calculates rename statistics
        /// </summary>
        public static (int Total, int Selected, int WillChange) CalculateStatistics<T>(
            List<T> items,
            Func<T, bool> isSelectedFunc,
            Func<T, bool> willChangeFunc)
        {
            int total = items.Count;
            int selected = items.Count(isSelectedFunc);
            int willChange = items.Count(i => isSelectedFunc(i) && willChangeFunc(i));

            return (total, selected, willChange);
        }

        #endregion

        #region Excel Import/Export

        /// <summary>
        /// Prepares data for Excel export
        /// </summary>
        public static List<Dictionary<string, object>> PrepareExportData<T>(
            List<T> items,
            Func<T, string> getOriginalNameFunc,
            Func<T, string> getNewNameFunc,
            Func<T, string> getCategoryFunc,
            Func<T, string> getFamilyFunc)
        {
            var result = new List<Dictionary<string, object>>();

            foreach (var item in items)
            {
                var row = new Dictionary<string, object>
                {
                    { "Category", getCategoryFunc(item) },
                    { "Family", getFamilyFunc(item) },
                    { "Original Name", getOriginalNameFunc(item) },
                    { "New Name", getNewNameFunc(item) }
                };
                result.Add(row);
            }

            return result;
        }

        #endregion
    }
}