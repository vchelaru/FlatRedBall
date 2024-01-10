using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.ViewModels;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TiledPluginCore.Controls
{
    #region TmxReferenceViewModel Class
    public class TmxReferenceViewModel : ViewModel
    {
        public FilePath FilePath
        {
            get => Get<FilePath>();
            set => Set(value);
        }

        [DependsOn(nameof(FilePath))]
        public string Display => FilePath.NoPath;

        public bool IsSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public override string ToString() => Display;

        public TmxReferenceViewModel(FilePath filePath) => FilePath = filePath;
    }

    #endregion

    public class TiledToolbarViewModel : ViewModel, ISearchBarViewModel
    {
        public List<TmxReferenceViewModel> AllTmxFiles { get; set; } = new List<TmxReferenceViewModel>();

        public ObservableCollection<TmxReferenceViewModel> AvailableTmxFiles
        {
            get; set;
        } = new ObservableCollection<TmxReferenceViewModel>();

        public string SearchBoxText
        {
            get => Get<string>();
            set
            {
                if (Set(value))
                {
                    RefreshAvailableTmxFiles();
                }
            }
        }

        public bool IsSearchBoxFocused
        {
            get => Get<bool>();
            set => Set(value);
        }


        [DependsOn(nameof(SearchBoxText))]
        public Visibility SearchButtonVisibility => (!string.IsNullOrEmpty(SearchBoxText)).ToVisibility();

        public Visibility TipsVisibility => Visibility.Collapsed;

        [DependsOn(nameof(IsSearchBoxFocused))]
        [DependsOn(nameof(SearchBoxText))]
        public Visibility SearchPlaceholderVisibility =>
            (IsSearchBoxFocused == false && string.IsNullOrWhiteSpace(SearchBoxText)).ToVisibility();

        public string FilterResultsInfo => null;


        public void RefreshAvailableTmxFiles()
        {
            var searchTextToLowerInvariant = SearchBoxText?.ToLowerInvariant();
            AvailableTmxFiles.Clear();

            foreach (var item in AllTmxFiles)
            {
                var shouldInclude =
                    string.IsNullOrWhiteSpace(searchTextToLowerInvariant) ||
                    item.Display.ToLowerInvariant().Contains(searchTextToLowerInvariant);
                if (shouldInclude)
                {
                    AvailableTmxFiles.Add(item);
                }
            }
        }
    }

    /// <summary>
    /// Interaction logic for TiledToolbar.xaml
    /// </summary>
    public partial class TiledToolbar : UserControl
    {
        public event EventHandler Opened;

        TiledToolbarViewModel ViewModel => DataContext as TiledToolbarViewModel;

        public TiledToolbar()
        {
            InitializeComponent();
            DataContext = new TiledToolbarViewModel();
        }

        private void HandleButtonClick(object sender, RoutedEventArgs e)
        {
            var assocation = FileAssociation.GetExecFileAssociatedToExtension(".tmx");

            if(!string.IsNullOrEmpty(assocation))
            {
                var startInfo = new ProcessStartInfo();
                startInfo.FileName = assocation;
                startInfo.UseShellExecute = true;

                System.Diagnostics.Process.Start(startInfo);
            }
        }

        private void HandleOpened(object sender, RoutedEventArgs e)
        {
            Opened?.Invoke(this, null);
        }

        [DllImport("Shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern int StrCmpLogicalW(string x, string y);


        // Vic asks - why use a list of MenuItems rather than a ListBox...because we didn't want
        // the list box to keep its selection??
        // Update - a ListBox does not scroll with the mousewheel whereas an ItemCollectionView does
        internal void FillDropdown(List<ReferencedFileSave> availableTmxFiles)
        {
            ViewModel.AllTmxFiles.Clear();

            var sorted = availableTmxFiles;
            //.OrderBy(item => new FilePath(item.Name).NoPath.ToLowerInvariant());
            sorted.Sort((a,b) => 
                StrCmpLogicalW(new FilePath(a.Name).NoPath,
                               new FilePath(b.Name).NoPath));


            HashSet<FilePath> alreadyAdded = new HashSet<FilePath>(); // prevents duplicates:

            foreach (var rfs in sorted)
            {
                var fullFilePath = new FilePath(GlueCommands.Self.GetAbsoluteFileName(rfs));

                if(!alreadyAdded.Contains(fullFilePath))
                {
                    alreadyAdded.Add(fullFilePath);
                    ViewModel.AllTmxFiles.Add(new TmxReferenceViewModel(fullFilePath));
                }

            }

            ViewModel.RefreshAvailableTmxFiles();

        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var vm = (sender as MenuItem).DataContext as TmxReferenceViewModel;
            SelectTmxReference(vm);
        }

        private static void SelectTmxReference(TmxReferenceViewModel vm)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = vm.FilePath.FullPath;
            startInfo.UseShellExecute = true;

            System.Diagnostics.Process.Start(startInfo);
        }

        private void SearchBar_ClearSearchButtonClicked()
        {
            ViewModel.SearchBoxText = null;
            DropDownButton.IsOpen = true;
        }

        private void DropDownButton_Closed(object sender, RoutedEventArgs e)
        {
            ViewModel.SearchBoxText = null;
        }

        private void SearchBar_ArrowKeyPushed(Key key)
        {
            if (key == Key.Up)
            {
                var highlighted = ViewModel.AvailableTmxFiles.FirstOrDefault(item => item.IsSelected);
                if (highlighted != null)
                {
                    var index = ViewModel.AvailableTmxFiles.IndexOf(highlighted);

                    if (index > 0)
                    {
                        highlighted.IsSelected = false;

                        ViewModel.AvailableTmxFiles[index - 1].IsSelected = true;
                    }
                }
                else
                {
                    var toSelect = ViewModel.AvailableTmxFiles.FirstOrDefault();
                    if (toSelect != null)
                    {
                        toSelect.IsSelected = true;
                    }
                }
            }
            else if (key == Key.Down)
            {
                var highlighted = ViewModel.AvailableTmxFiles.FirstOrDefault(item => item.IsSelected);
                if (highlighted != null)
                {
                    var index = ViewModel.AvailableTmxFiles.IndexOf(highlighted);

                    if (index < ViewModel.AvailableTmxFiles.Count - 1)
                    {
                        highlighted.IsSelected = false;

                        ViewModel.AvailableTmxFiles[index + 1].IsSelected = true;
                    }
                }
                else
                {
                    var toSelect = ViewModel.AvailableTmxFiles.FirstOrDefault();
                    if (toSelect != null)
                    {
                        toSelect.IsSelected = true;
                    }
                }
            }
        }

        private void SearchBar_EnterPressed()
        {
            var highlighted = ViewModel.AvailableTmxFiles.FirstOrDefault(item => item.IsSelected);
            if(highlighted != null)
            {
                SelectTmxReference(highlighted);
            }
        }

        internal void HighlightFirstItem()
        {
            if(ViewModel.AvailableTmxFiles.Count > 0)
            {
                ViewModel.AvailableTmxFiles[0].IsSelected = true;
            }
        }
    }

    // pulled from:
    // https://stackoverflow.com/questions/770023/how-do-i-get-file-type-information-based-on-extension-not-mime-in-c-sharp

    /// <summary>
    /// Usage:  string executablePath = FileAssociation.GetExecFileAssociatedToExtension(pathExtension, "open");
    /// </summary>
    public static class FileAssociation
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ext"></param>
        /// <param name="verb"></param>
        /// <returns>Return null if not found</returns>
        public static string GetExecFileAssociatedToExtension(string ext, string verb = null)
        {
            if (ext[0] != '.')
            {
                ext = "." + ext;
            }

            string executablePath = FileExtentionInfo(AssocStr.Executable, ext, verb); // Will only work for 'open' verb
            if (string.IsNullOrEmpty(executablePath))
            {
                executablePath = FileExtentionInfo(AssocStr.Command, ext, verb); // required to find command of any other verb than 'open'

                // Extract only the path
                if (!string.IsNullOrEmpty(executablePath) && executablePath.Length > 1)
                {
                    if (executablePath[0] == '"')
                    {
                        executablePath = executablePath.Split('\"')[1];
                    }
                    else if (executablePath[0] == '\'')
                    {
                        executablePath = executablePath.Split('\'')[1];
                    }
                }
            }

            // Ensure to not return the default OpenWith.exe associated executable in Windows 8 or higher
            if (!string.IsNullOrEmpty(executablePath) && File.Exists(executablePath) &&
                !executablePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                if (executablePath.EndsWith("openwith.exe", StringComparison.OrdinalIgnoreCase))
                {
                    return null; // 'OpenWith.exe' is th windows 8 or higher default for unknown extensions. I don't want to have it as associted file
                }
                return executablePath;
            }
            return executablePath;
        }

        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern uint AssocQueryString(AssocF flags, AssocStr str, string pszAssoc, string pszExtra, [Out] StringBuilder pszOut, [In][Out] ref uint pcchOut);

        private static string FileExtentionInfo(AssocStr assocStr, string doctype, string verb)
        {
            uint pcchOut = 0;
            AssocQueryString(AssocF.Verify, assocStr, doctype, verb, null, ref pcchOut);

            Debug.Assert(pcchOut != 0);
            if (pcchOut == 0)
            {
                return "";
            }

            StringBuilder pszOut = new StringBuilder((int)pcchOut);
            AssocQueryString(AssocF.Verify, assocStr, doctype, verb, pszOut, ref pcchOut);
            return pszOut.ToString();
        }

        [Flags]
        public enum AssocF
        {
            Init_NoRemapCLSID = 0x1,
            Init_ByExeName = 0x2,
            Open_ByExeName = 0x2,
            Init_DefaultToStar = 0x4,
            Init_DefaultToFolder = 0x8,
            NoUserSettings = 0x10,
            NoTruncate = 0x20,
            Verify = 0x40,
            RemapRunDll = 0x80,
            NoFixUps = 0x100,
            IgnoreBaseClass = 0x200
        }

        public enum AssocStr
        {
            Command = 1,
            Executable,
            FriendlyDocName,
            FriendlyAppName,
            NoOpen,
            ShellNewValue,
            DDECommand,
            DDEIfExec,
            DDEApplication,
            DDETopic
        }



    }

}
