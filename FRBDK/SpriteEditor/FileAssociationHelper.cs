using System;
using Microsoft.Win32;
using System.Security.Permissions;

namespace SpriteEditor
{
    /// <exception cref="System.Security.SecurityException">A <see cref="System.Security.SecurityException"/> 
    /// will be thrown if the currently logged in user does not have unrestricted Registry 
    /// and Reflection permissions.</exception>
    /// <remarks>Currently requires unrestricted Registry and Reflection permissions.  
    /// Have not fully investigated whether you really need unrestricted access to the registry
    /// to create/edit the required registry keys (in HKEY_CLASSES_ROOT), or
    /// if there is a way to do the association as a limited user. In
    /// the mean time, we will err on the side of caution.</remarks>
    [ReflectionPermission(SecurityAction.Demand, Unrestricted = true),
    RegistryPermission(SecurityAction.Demand, Unrestricted = true)]
    internal sealed class FileAssociationHelper
    {
        #region Fields

        private string _x10AssociationKeyName;
        private string _x10 = string.Empty;
        private string _currentEditor = string.Empty;
        private string _currentOpener = string.Empty;
        private bool _isOpener;
        private bool _isEditor;
        private readonly string _assemblyCommand;

        #endregion

        #region Constructors

        public FileAssociationHelper(string extension)
            : this()
        {
            Extension = extension;
        }

        /// <summary>
        /// Until the <see cref="Exception"/> property is set, the <see cref="IsEditor"/>,
        /// <see cref="IsOpener"/>, <see cref="CurrentEditor"/>, and <see cref="CurrentOpener"/>
        /// properties will be initialized to default values.
        /// </summary>
        public FileAssociationHelper()
        {
            _assemblyCommand = GetAssemblyCommand();
        }
        #endregion

        #region Properties

        /// <summary>
        /// The extension to check/associate with the current executable.
        /// </summary>
        /// <remarks>Must start with a '.'</remarks>
        public string Extension
        {
            get { return _x10; }
            set
            {
                if (value.IndexOf('.') != 0)
                {
                    throw new ArgumentException("Extension must start with a '.'", "Extension");
                }

                _x10 = value;
                _x10AssociationKeyName = value.Substring(1, value.Length-1) + "_file";
                CheckStatus();
            }
        }

        public string CurrentEditor
        {
            get { return _currentEditor; }
        }

        public string CurrentOpener
        {
            get { return _currentOpener; }
        }

        public bool IsOpener
        {
            get { return _isOpener; }
        }

        public bool IsEditor
        {
            get { return _isEditor; }
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Refreshes the <see cref="IsEditor"/>,
        /// <see cref="IsOpener"/>, <see cref="CurrentEditor"/>, and <see cref="CurrentOpener"/>
        /// properties by checking the Registry.
        /// </summary>
        public void CheckStatus()
        {
            RegistryKey x10RegKey = Registry.ClassesRoot.OpenSubKey(_x10);
            if (x10RegKey == null)
            {
                _isOpener = false;
                _isEditor = false;
                _currentOpener = string.Empty;
                _currentEditor = string.Empty;
                return;
            }

            string keyValue = x10RegKey.GetValue(string.Empty).ToString();

            RegistryKey editorKey = GetAssociationKey(keyValue, @"\shell\edit\command");
            Refresh(editorKey, ref _isEditor, ref _currentEditor);
            RegistryKey openerKey = GetAssociationKey(keyValue, @"\shell\open\command");
            Refresh(openerKey, ref _isOpener, ref _currentOpener);
        }

        /// <summary>
        /// Associates the currently running executable with the file <see cref="Extension"/>
        /// </summary>
        public void Associate()
        {
            if (!string.IsNullOrEmpty(_x10) && (!this._isEditor || !this._isOpener))
            {
                RegistryKey x10RegKey = GetCreateSubKey(Registry.ClassesRoot, _x10, _x10AssociationKeyName);
                _x10AssociationKeyName = x10RegKey.GetValue(string.Empty).ToString();

                RegistryKey x10AssociationSubKey = GetCreateSubKey(Registry.ClassesRoot, _x10AssociationKeyName);
                RegistryKey shell = GetCreateSubKey(x10AssociationSubKey, "shell");

                if (!_isEditor)
                {
                    RegistryKey edit = GetCreateSubKey(shell, "edit");
                    RegistryKey editCommand = GetCreateSubKey(edit, "command");
                    editCommand.SetValue(string.Empty, _assemblyCommand);
                }

                if (!_isOpener)
                {
                    RegistryKey open = GetCreateSubKey(shell, "open");
                    RegistryKey openCommand = GetCreateSubKey(open, "command");
                    openCommand.SetValue(string.Empty, _assemblyCommand);
                }

                this.CheckStatus();
            }
        }
        #endregion

        #region Private Methods

        private void Refresh(RegistryKey key, ref bool _isExe, ref string _currentExe)
        {
            _isExe = CheckAssociation(key);
            _currentExe = key.GetValue(string.Empty).ToString();
        }

        private bool CheckAssociation(RegistryKey associationKey)
        {
            string currentExe = associationKey.GetValue(string.Empty).ToString();
            return currentExe.Equals(_assemblyCommand);
        }

        private static string GetAssemblyCommand()
        {
            string runningexe = System.Reflection.Assembly.GetEntryAssembly().CodeBase.Replace("/",@"\");
            return runningexe.Substring(8, runningexe.Length - 8) + " %1";
        }

        private static RegistryKey GetAssociationKey(string subKey, string associationKey)
        {
            return Registry.ClassesRoot.OpenSubKey(subKey + associationKey);
        }

        private static RegistryKey GetCreateSubKey(RegistryKey parentKey, string subkeyPath)
        {
            RegistryKey subKey = parentKey.OpenSubKey(subkeyPath, true);
            if (subKey == null)
            {
                subKey = parentKey.CreateSubKey(subkeyPath);
            }
            return subKey;
        }

        private static RegistryKey GetCreateSubKey(RegistryKey parentKey, string subkeyPath, string valueIfNull)
        {
            RegistryKey subKey = parentKey.OpenSubKey(subkeyPath, true);
            if (subKey == null)
            {
                subKey = parentKey.CreateSubKey(subkeyPath);
                subKey.SetValue(string.Empty, valueIfNull);
            }
            return subKey;
        }
        #endregion
    }
}
