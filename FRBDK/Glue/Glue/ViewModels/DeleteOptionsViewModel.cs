using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.SaveClasses;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace GlueFormsCore.ViewModels
{
    /// <summary>
    /// What should happen to the files that a delete leaves behind. Ordered so the least destructive
    /// option is first; <see cref="RemoveAndDelete"/> sends files to the recycle bin rather than
    /// erasing them.
    /// </summary>
    public enum FileDeleteAction
    {
        /// <summary>
        /// Leave the files in the game project untouched.
        /// </summary>
        Nothing,
        /// <summary>
        /// Remove the files from the .csproj but leave them on disk.
        /// </summary>
        RemoveFromProject,
        /// <summary>
        /// Remove the files from the .csproj and move them to the recycle bin.
        /// </summary>
        RemoveAndDelete
    }

    /// <summary>
    /// One optional, user-toggleable part of a delete - "reset the inheritance for DerivedScreen",
    /// "delete the Gum screen GameScreenGum". Plugins add their own through
    /// <c>PluginManager.FillDeleteOptions</c> so every question a delete needs to ask lands in the same
    /// dialog instead of in a popup of its own.
    /// </summary>
    public class DeleteOptionViewModel : ViewModel
    {
        public string Display
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        /// <summary>
        /// Identifies the option to whoever added it - the derived ScreenSave for an inheritance-reset
        /// option, a plugin-defined key for a plugin option. Read back with
        /// <see cref="DeleteOptionsViewModel.IsOptionChecked"/> after the dialog closes.
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// Files that only become deletable if this option is checked - the Gum screen's generated and
        /// custom code files, for instance. They appear in <see cref="DeleteOptionsViewModel.FilesToRemove"/>
        /// while the option is checked and disappear when it is unchecked, so the file list the user
        /// approves always matches the options they picked.
        /// </summary>
        public List<string> AdditionalFilesToRemove { get; } = new List<string>();
    }

    /// <summary>
    /// Everything a single delete needs to ask the user, in one object: what is being deleted, what else
    /// goes with it, which optional parts to include, and what to do with the leftover files.
    ///
    /// Built by <see cref="FlatRedBall.Glue.Elements.DeletionPlanner"/> without touching any UI, shown by
    /// <c>DialogService.ShowDelete</c>, and then read back by the code that performs the delete. That split
    /// is the point: deleting a Screen used to ask up to four separate questions from four different places
    /// part-way through the delete itself (see GitHub issue #429).
    /// </summary>
    public class DeleteOptionsViewModel : ViewModel
    {
        /// <summary>
        /// The element this delete is for. Null for deletes that aren't element deletes.
        /// </summary>
        public GlueElement Element { get; set; }

        public string Message
        {
            get => Get<string>();
            set => Set(value);
        }

        /// <summary>
        /// Things which are removed as an unavoidable consequence of this delete - objects referencing the
        /// deleted entity, variables tied to a deleted state. Informational; the user cannot opt out.
        /// </summary>
        public ObservableCollection<string> ObjectsToRemove { get; } = new ObservableCollection<string>();

        [DependsOn(nameof(ObjectsToRemove))]
        public Visibility ObjectsToRemoveVisibility => (ObjectsToRemove.Count > 0).ToVisibility();

        /// <summary>
        /// Consequences of the delete the user can neither opt out of nor act on here - an object defined by
        /// a base element that will be left referencing a file that no longer exists, for instance. These
        /// used to be their own message box raised part-way through the delete.
        /// </summary>
        public ObservableCollection<string> Warnings { get; } = new ObservableCollection<string>();

        [DependsOn(nameof(Warnings))]
        public Visibility WarningsVisibility => (Warnings.Count > 0).ToVisibility();

        public ObservableCollection<DeleteOptionViewModel> Options { get; } = new ObservableCollection<DeleteOptionViewModel>();

        [DependsOn(nameof(Options))]
        public Visibility OptionsVisibility => (Options.Count > 0).ToVisibility();

        /// <summary>
        /// The files this delete would leave orphaned, kept in sync with which <see cref="Options"/> are
        /// checked. Never assign to this - add to <see cref="AlwaysRemovedFiles"/> or to an option's
        /// <see cref="DeleteOptionViewModel.AdditionalFilesToRemove"/>. These are the paths acted on;
        /// <see cref="FilesToRemoveDisplay"/> is what the user sees.
        /// </summary>
        public ObservableCollection<string> FilesToRemove { get; } = new ObservableCollection<string>();

        /// <summary>
        /// <see cref="FilesToRemove"/> shortened to paths relative to <see cref="ProjectRootForDisplay"/>.
        /// A delete's file list is assembled from several sources that each spell a path their own way, so
        /// showing them raw mixed separators and mixed absolute with relative in the same list.
        /// </summary>
        public ObservableCollection<string> FilesToRemoveDisplay { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Trimmed off the front of each file for display. Null leaves paths at full length.
        /// </summary>
        public string ProjectRootForDisplay { get; set; }

        [DependsOn(nameof(FilesToRemove))]
        public Visibility FilesToRemoveVisibility => (FilesToRemove.Count > 0).ToVisibility();

        /// <summary>
        /// Files removed no matter which options the user picks - the deleted element's own code and
        /// JSON files.
        /// </summary>
        public List<string> AlwaysRemovedFiles { get; } = new List<string>();

        public FileDeleteAction FileAction
        {
            get => Get<FileDeleteAction>();
            set => Set(value);
        }

        #region Radio button bindings for FileAction

        public bool IsDoNothingWithFilesChecked
        {
            get => FileAction == FileDeleteAction.Nothing;
            set { if (value) FileAction = FileDeleteAction.Nothing; }
        }

        public bool IsRemoveFilesFromProjectChecked
        {
            get => FileAction == FileDeleteAction.RemoveFromProject;
            set { if (value) FileAction = FileDeleteAction.RemoveFromProject; }
        }

        public bool IsRemoveAndDeleteFilesChecked
        {
            get => FileAction == FileDeleteAction.RemoveAndDelete;
            set { if (value) FileAction = FileDeleteAction.RemoveAndDelete; }
        }

        #endregion

        public DeleteOptionsViewModel()
        {
            // Default to the action that leaves the project in the state the user asked for: the listed
            // files are the deleted element's own code and content, which nothing references any more.
            // They go to the recycle bin, not to nowhere.
            FileAction = FileDeleteAction.RemoveAndDelete;

            ObjectsToRemove.CollectionChanged += (_, __) => NotifyPropertyChanged(nameof(ObjectsToRemove));
            Warnings.CollectionChanged += (_, __) => NotifyPropertyChanged(nameof(Warnings));
            FilesToRemove.CollectionChanged += (_, __) => NotifyPropertyChanged(nameof(FilesToRemove));
            Options.CollectionChanged += HandleOptionsChanged;
        }

        protected override void NotifyPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            base.NotifyPropertyChanged(propertyName);

            if (propertyName == nameof(FileAction))
            {
                base.NotifyPropertyChanged(nameof(IsDoNothingWithFilesChecked));
                base.NotifyPropertyChanged(nameof(IsRemoveFilesFromProjectChecked));
                base.NotifyPropertyChanged(nameof(IsRemoveAndDeleteFilesChecked));
            }
        }

        /// <summary>
        /// Adds a checkbox to the dialog. Checked by default, because every one of these replaced a
        /// prompt whose default answer was the action itself.
        /// </summary>
        public DeleteOptionViewModel AddOption(string display, object tag = null, bool isChecked = true)
        {
            var option = new DeleteOptionViewModel
            {
                Display = display,
                Tag = tag,
                IsChecked = isChecked
            };
            Options.Add(option);
            return option;
        }

        /// <summary>
        /// Whether the user left the option with this tag checked. Returns false when no option has the
        /// tag, so a plugin that didn't add an option never acts on one.
        /// </summary>
        public bool IsOptionChecked(object tag) =>
            Options.Any(item => Equals(item.Tag, tag) && item.IsChecked);

        public IEnumerable<DeleteOptionViewModel> CheckedOptions => Options.Where(item => item.IsChecked);

        /// <summary>
        /// Rebuilds <see cref="FilesToRemove"/> from <see cref="AlwaysRemovedFiles"/> plus the files of
        /// every checked option, de-duplicated. Called automatically whenever an option is toggled.
        /// </summary>
        public void RefreshFilesToRemove()
        {
            var desired = AlwaysRemovedFiles
                .Concat(CheckedOptions.SelectMany(item => item.AdditionalFilesToRemove))
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Replace('\\', '/'))
                .Distinct(System.StringComparer.OrdinalIgnoreCase)
                .ToList();

            FilesToRemove.Clear();
            FilesToRemoveDisplay.Clear();
            foreach (var file in desired)
            {
                FilesToRemove.Add(file);
                FilesToRemoveDisplay.Add(ToDisplayPath(file, ProjectRootForDisplay));
            }
        }

        /// <summary>
        /// Shortens a path to one relative to <paramref name="root"/>, with forward slashes. A file
        /// outside the project still comes back relative (a "../" chain) rather than as a full path, so
        /// every row in the list reads the same way.
        /// </summary>
        public static string ToDisplayPath(string file, string root)
        {
            if (string.IsNullOrWhiteSpace(file) || string.IsNullOrWhiteSpace(root))
            {
                return file;
            }

            var normalizedRoot = root.Replace('\\', '/');
            if (!normalizedRoot.EndsWith("/"))
            {
                normalizedRoot += "/";
            }

            if (file.StartsWith(normalizedRoot, System.StringComparison.OrdinalIgnoreCase))
            {
                return file.Substring(normalizedRoot.Length);
            }

            // Different drive, no shared root - MakeRelative can't express it, so it hands the path back
            // unchanged rather than producing something nonsensical.
            return FlatRedBall.IO.FileManager.MakeRelative(file, normalizedRoot).Replace('\\', '/');
        }

        private void HandleOptionsChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.OldItems != null)
            {
                foreach (DeleteOptionViewModel option in args.OldItems)
                {
                    option.PropertyChanged -= HandleOptionPropertyChanged;
                }
            }
            if (args.NewItems != null)
            {
                foreach (DeleteOptionViewModel option in args.NewItems)
                {
                    option.PropertyChanged += HandleOptionPropertyChanged;
                }
            }

            NotifyPropertyChanged(nameof(Options));
            RefreshFilesToRemove();
        }

        private void HandleOptionPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(DeleteOptionViewModel.IsChecked))
            {
                RefreshFilesToRemove();
            }
        }
    }
}
