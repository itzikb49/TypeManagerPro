// RevitCompatibilityExtensions.cs
// Compatibility layer for Revit 2023-2026+
// Handles ElementId API changes between versions

using Autodesk.Revit.DB;
// using System;
using System.Reflection;

namespace TypeManagerPro.Helpers
{
    /// <summary>
    /// Extension Methods to support different Revit versions (2023-2026+)
    /// Uses Reflection for full compile-time compatibility
    /// </summary>
    public static class RevitCompatibilityExtensions
    {
        // ════════════════════════════════════════════════════════════════
        // ElementId Value Extraction
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns the integer value of ElementId - compatible with all Revit versions
        /// </summary>
        public static int GetIdValue(this ElementId elementId)
        {
            if (elementId == null)
                return -1;

            try
            {
                // Try Value property first (Revit 2024+)
                var valueProperty = typeof(ElementId).GetProperty("Value");
                if (valueProperty != null)
                {
                    var value = valueProperty.GetValue(elementId);
                    return (int)(long)value;
                }
            }
            catch { }

            try
            {
                // Fallback to IntegerValue (Revit 2023)
                var intProperty = typeof(ElementId).GetProperty("IntegerValue");
                if (intProperty != null)
                {
                    return (int)intProperty.GetValue(elementId);
                }
            }
            catch { }

            return -1;
        }

        /// <summary>
        /// Returns the value of ElementId as long
        /// </summary>
        public static long GetIdValueLong(this ElementId elementId)
        {
            if (elementId == null)
                return -1;

            try
            {
                var valueProperty = typeof(ElementId).GetProperty("Value");
                if (valueProperty != null)
                {
                    return (long)valueProperty.GetValue(elementId);
                }
            }
            catch { }

            try
            {
                var intProperty = typeof(ElementId).GetProperty("IntegerValue");
                if (intProperty != null)
                {
                    return (int)intProperty.GetValue(elementId);
                }
            }
            catch { }

            return -1;
        }

        /// <summary>
        /// Checks if ElementId represents a BuiltIn parameter
        /// </summary>
        public static bool IsBuiltInId(this ElementId elementId)
        {
            return elementId != null && elementId.GetIdValue() < 0;
        }

        /// <summary>
        /// Checks if ElementId is valid
        /// </summary>
        public static bool IsValidId(this ElementId elementId)
        {
            if (elementId == null)
                return false;

            if (elementId == ElementId.InvalidElementId)
                return false;

            return elementId.GetIdValue() != -1;
        }

        // ════════════════════════════════════════════════════════════════
        // ElementId Creation
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Creates ElementId from int value - compatible with all versions
        /// </summary>
        public static ElementId ToElementId(this int value)
        {
            return ToElementId((long)value);
        }

        /// <summary>
        /// Creates ElementId from long value - compatible with all versions
        /// </summary>
        public static ElementId ToElementId(this long value)
        {
            try
            {
                // Try ElementId(long) constructor first (Revit 2024+)
                var longConstructor = typeof(ElementId).GetConstructor(new[] { typeof(long) });
                if (longConstructor != null)
                {
                    return (ElementId)longConstructor.Invoke(new object[] { value });
                }
            }
            catch { }

            try
            {
                // Fallback to ElementId(int) constructor (Revit 2023)
                var intConstructor = typeof(ElementId).GetConstructor(new[] { typeof(int) });
                if (intConstructor != null)
                {
                    return (ElementId)intConstructor.Invoke(new object[] { (int)value });
                }
            }
            catch { }

            // Last resort - return InvalidElementId
            return ElementId.InvalidElementId;
        }

        // ════════════════════════════════════════════════════════════════
        // ElementId Comparison
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Compares two ElementIds for equality
        /// </summary>
        public static bool IsEqual(this ElementId id1, ElementId id2)
        {
            if (id1 == null && id2 == null)
                return true;

            if (id1 == null || id2 == null)
                return false;

            return id1.Equals(id2);
        }

        // ════════════════════════════════════════════════════════════════
        // Revit Version Detection
        // ════════════════════════════════════════════════════════════════

        private static int? _cachedRevitVersion = null;

        /// <summary>
        /// Gets the current Revit version year (e.g., 2023, 2024, 2025)
        /// </summary>
        public static int GetRevitVersion()
        {
            if (_cachedRevitVersion.HasValue)
                return _cachedRevitVersion.Value;

            try
            {
                // Get version from RevitAPI assembly
                var assembly = typeof(ElementId).Assembly;
                var version = assembly.GetName().Version;

                // Major version = last 2 digits of year
                // Example: 23 = 2023, 24 = 2024, etc.
                int year = 2000 + version.Major;
                _cachedRevitVersion = year;
                return year;
            }
            catch
            {
                // Default to 2023 if detection fails
                _cachedRevitVersion = 2023;
                return 2023;
            }
        }

        /// <summary>
        /// Checks if current Revit version is 2024 or later
        /// </summary>
        public static bool IsRevit2024OrLater()
        {
            return GetRevitVersion() >= 2024;
        }

        // ════════════════════════════════════════════════════════════════
        // Element Type Name Helpers
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Safely gets the name of an ElementType
        /// </summary>
        public static string GetTypeName(this ElementType elementType)
        {
            if (elementType == null)
                return string.Empty;

            try
            {
                return elementType.Name ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Safely sets the name of an ElementType
        /// </summary>
        public static bool TrySetTypeName(this ElementType elementType, string newName)
        {
            if (elementType == null || string.IsNullOrEmpty(newName))
                return false;

            try
            {
                elementType.Name = newName;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if an ElementType's name can be changed
        /// </summary>
        public static bool CanRename(this ElementType elementType)
        {
            if (elementType == null)
                return false;

            try
            {
                var param = elementType.get_Parameter(BuiltInParameter.SYMBOL_NAME_PARAM);
                return param != null && !param.IsReadOnly;
            }
            catch
            {
                return false;
            }
        }

        // ════════════════════════════════════════════════════════════════
        // Transaction Helpers
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Safely gets the document from an element
        /// </summary>
        public static Document GetDocumentSafe(this Element element)
        {
            try
            {
                return element?.Document;
            }
            catch
            {
                return null;
            }
        }
    }
}