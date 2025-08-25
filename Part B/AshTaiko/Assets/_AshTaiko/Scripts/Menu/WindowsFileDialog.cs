using System;
using System.Runtime.InteropServices;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AshTaiko.Menu
{
    /// <summary>
    /// Provides native Windows file dialog functionality for song importing.
    /// This class uses Win32 API calls to show native file selection dialogs.
    /// </summary>
    public static class WindowsFileDialog
    {
        #region Win32 API Imports
        
        [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr GetOpenFileName(ref OpenFileName ofn);
        
        [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr GetSaveFileName(ref OpenFileName ofn);
        
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct OpenFileName
        {
            public int lStructSize;
            public IntPtr hwndOwner;
            public IntPtr hInstance;
            public string lpstrFilter;
            public string lpstrCustomFilter;
            public int nMaxCustFilter;
            public int nFilterIndex;
            public string lpstrFile;
            public int nMaxFile;
            public string lpstrFileTitle;
            public int nMaxFileTitle;
            public string lpstrInitialDir;
            public string lpstrTitle;
            public int Flags;
            public short nFileOffset;
            public short nFileExtension;
            public string lpstrDefExt;
            public IntPtr lCustData;
            public IntPtr lpfnHook;
            public string lpTemplateName;
            public IntPtr pvReserved;
            public int dwReserved;
            public int FlagsEx;
        }
        
        #endregion
        
        #region Constants
        
        private const int OFN_PATHMUSTEXIST = 0x00000800;
        private const int OFN_FILEMUSTEXIST = 0x00001000;
        private const int OFN_HIDEREADONLY = 0x00000004;
        private const int OFN_OVERWRITEPROMPT = 0x00000002;
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Shows Windows runtime file dialog using Win32 API.
        /// </summary>
        private static string ShowWindowsFileDialog(string title, string filter, string initialDirectory, string defaultExtension)
        {
            try
            {
                OpenFileName ofn = new OpenFileName();
                ofn.lStructSize = Marshal.SizeOf(ofn);
                ofn.lpstrTitle = title;
                
                // Fix the filter string format for Windows
                string fixedFilter = FixFilterStringForWindows(filter);
                ofn.lpstrFilter = fixedFilter;
                
                ofn.lpstrInitialDir = initialDirectory;
                
                // Windows file dialog expects extension with dot (e.g., ".osz")
                string cleanExtension = defaultExtension.TrimStart('.');
                ofn.lpstrDefExt = "." + cleanExtension;
                
                ofn.Flags = OFN_PATHMUSTEXIST | OFN_FILEMUSTEXIST | OFN_HIDEREADONLY;
                ofn.nFilterIndex = 1;
                
                // Allocate buffer for file path
                string filePath = new string('\0', 260);
                ofn.lpstrFile = filePath;
                ofn.nMaxFile = filePath.Length;
                
                IntPtr result = GetOpenFileName(ref ofn);
                
                if (result != IntPtr.Zero)
                {
                    // Extract the selected file path
                    int nullIndex = ofn.lpstrFile.IndexOf('\0');
                    if (nullIndex > 0)
                    {
                        return ofn.lpstrFile.Substring(0, nullIndex);
                    }
                    return ofn.lpstrFile;
                }
                
                return string.Empty;
            }
            catch (Exception e)
            {
                Debug.LogError($"Windows file dialog error: {e.Message}");
                return string.Empty;
            }
        }
        
        /// <summary>
        /// Shows Unity Editor file dialog using EditorUtility.
        /// </summary>
        private static string ShowEditorFileDialog(string title, string filter, string initialDirectory, string defaultExtension)
        {
            #if UNITY_EDITOR
            try
            {
                // Use the defaultExtension directly - it should already be clean (e.g., "osz" not "*.osz")
                string extension = defaultExtension;
                
                // Use Unity's EditorUtility for file selection
                // Note: EditorUtility.OpenFilePanel expects extension without dot, which is what we have
                string filePath = EditorUtility.OpenFilePanel(title, "", extension);
                return string.IsNullOrEmpty(filePath) ? string.Empty : filePath;
            }
            catch (Exception e)
            {
                Debug.LogError($"Editor file dialog error: {e.Message}");
                return string.Empty;
            }
            #else
            return string.Empty;
            #endif
        }
        
        #endregion
        
        /// <summary>
        /// Fixes the filter string format for Windows file dialogs.
        /// Windows expects a specific format with null terminators.
        /// </summary>
        private static string FixFilterStringForWindows(string filter)
        {
            if (string.IsNullOrEmpty(filter))
                return filter;
            
            // Windows file dialog filter format: "Description\0*.ext\0All Files\0*.*\0"
            // Replace | with null terminators
            string fixedFilter = filter.Replace("|", "\0") + "\0";
            
            return fixedFilter;
        }
        
        /// <summary>
        /// Shows a file open dialog for selecting files to import.
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="filter">File filter string (e.g., "OSZ files (*.osz)|*.osz|All files (*.*)|*.*")</param>
        /// <param name="initialDirectory">Starting directory for the dialog</param>
        /// <param name="defaultExtension">Default file extension</param>
        /// <returns>Selected file path or empty string if cancelled</returns>
        public static string ShowOpenFileDialog(string title, string filter, string initialDirectory, string defaultExtension = "")
        {
            #if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            // Runtime Windows file dialog
            return ShowWindowsFileDialog(title, filter, initialDirectory, defaultExtension);
            #elif UNITY_EDITOR
            // Unity Editor file dialog
            return ShowEditorFileDialog(title, filter, initialDirectory, defaultExtension);
            #else
            // Fallback for non-Windows platforms
            Debug.LogWarning("File dialog not available on this platform");
            return string.Empty;
            #endif
        }
        
        /// <summary>
        /// Shows a file save dialog for saving files.
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="filter">File filter string</param>
        /// <param name="initialDirectory">Starting directory for the dialog</param>
        /// <param name="defaultFileName">Default filename</param>
        /// <param name="defaultExtension">Default file extension</param>
        /// <returns>Selected file path or empty string if cancelled</returns>
        public static string ShowSaveFileDialog(string title, string filter, string initialDirectory, string defaultFileName, string defaultExtension = "")
        {
            #if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            try
            {
                OpenFileName ofn = new OpenFileName();
                ofn.lStructSize = Marshal.SizeOf(ofn);
                ofn.lpstrTitle = title;
                
                // Fix the filter string format for Windows
                string fixedFilter = FixFilterStringForWindows(filter);
                ofn.lpstrFilter = fixedFilter;
                
                ofn.lpstrInitialDir = initialDirectory;
                ofn.lpstrFile = defaultFileName;
                ofn.nMaxFile = defaultFileName.Length;
                
                // Windows file dialog expects extension with dot (e.g., ".osz")
                string cleanExtension = defaultExtension.TrimStart('.');
                ofn.lpstrDefExt = "." + cleanExtension;
                ofn.Flags = OFN_PATHMUSTEXIST | OFN_OVERWRITEPROMPT | OFN_HIDEREADONLY;
                ofn.nFilterIndex = 1;
                
                IntPtr result = GetSaveFileName(ref ofn);
                
                if (result != IntPtr.Zero)
                {
                    return ofn.lpstrFile;
                }
                
                return string.Empty;
            }
            catch (Exception e)
            {
                Debug.LogError($"Windows save file dialog error: {e.Message}");
                return string.Empty;
            }
            #else
            // Fallback for non-Windows platforms or editor
            Debug.LogWarning("Windows file dialog not available on this platform");
            return string.Empty;
            #endif
        }
        
        /// <summary>
        /// Creates a filter string for file dialogs.
        /// </summary>
        /// <param name="description">File type description</param>
        /// <param name="extension">File extension (without dot)</param>
        /// <returns>Formatted filter string</returns>
        public static string CreateFilterString(string description, string extension)
        {
            // Clean the extension to ensure it doesn't have a dot
            string cleanExtension = extension.TrimStart('.');
            
            // Create a Windows-compatible filter string
            // Format: "Description (*.ext)|*.ext|All files (*.*)|*.*"
            string filter = $"{description} (*.{cleanExtension})|*.{cleanExtension}|All files (*.*)|*.*";
            
            return filter;
        }
    }
}
