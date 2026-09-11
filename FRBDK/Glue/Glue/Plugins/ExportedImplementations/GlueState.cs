using System.Collections.Generic;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using System.Windows.Forms;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Data;
using FlatRedBall.Glue.Managers;
using Glue;
using FlatRedBall.IO;
using FlatRedBall.Glue.Errors;
using System.Linq;
using FlatRedBall.Glue.IO;
using GlueFormsCore.Plugins.EmbeddedPlugins.ExplorerTabPlugin;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Navigation;
using FlatRedBall.Glue.Tiled;
using GlueFormsCore.ViewModels;
using Microsoft.Build.Evaluation;
using Mono.Cecil;


namespace FlatRedBall.Glue.Plugins.ExportedImplementations
{
    #region GlueStateSnapshot

    public class GlueStateSnapshot
    {
        public ITreeNode CurrentTreeNode
        {
            get => CurrentTreeNodes.FirstOrDefault();
            set
            {
                if(value == null)
                {
                    CurrentTreeNodes.Clear();
                }
                else if(CurrentTreeNodes.Count != 1 || CurrentTreeNodes[0] != value)
                {
                    CurrentTreeNodes.Clear();

                    CurrentTreeNodes.Add(value);
                }
            }
        }
        public List<ITreeNode> CurrentTreeNodes = new List<ITreeNode>();

        /// <summary>
        /// The selected objects themselves (an element, a NamedObjectSave, a state, ...), in selection
        /// order. This is the selection; CurrentTreeNodes is only the tree view's rendering of it and
        /// can be shorter (or empty, with no tree). An entry is null for a tagless tree node.
        /// </summary>
        public List<object> SelectedTags = new List<object>();

        public GlueElement CurrentElement;
        public EntitySave CurrentEntitySave;
        public ScreenSave CurrentScreenSave;
        public ReferencedFileSave CurrentReferencedFileSave;
        public NamedObjectSave CurrentNamedObjectSave
        {
            get => CurrentNamedObjectSaves.FirstOrDefault();
            set
            {
                if(value == null)
                {
                    CurrentNamedObjectSaves.Clear();
                }
                else if(CurrentNamedObjectSaves.Count != 1 || CurrentNamedObjectSaves[0] != value)
                {
                    CurrentNamedObjectSaves.Clear();

                    CurrentNamedObjectSaves.Add(value);
                }
            }
        }
        public List<NamedObjectSave> CurrentNamedObjectSaves = new List<NamedObjectSave>();
        public StateSave CurrentStateSave;
        public StateSaveCategory CurrentStateSaveCategory;
        public CustomVariable CurrentCustomVariable;
        public EventResponseSave CurrentEventResponseSave;
        public int? SelectedSubIndex;

    }

    #endregion

    public class GlueState : IGlueState
    {
        #region Current Selection Properties

        public ITreeNode CurrentTreeNode
        {
            get => snapshot.CurrentTreeNode;
            set
            {
                UpdateToSetTreeNode(value, recordState:true);
            }
        }

        public IReadOnlyList<ITreeNode> CurrentTreeNodes
        {
            get => snapshot.CurrentTreeNodes;
            set
            {
                UpdateToSetTreeNode(value, recordState: true);
            }
        }

        // Every Current* setter below selects by model object. The selection lives here, not in the
        // tree view: a tree node (when a tree exists) is looked up so the tree view and other
        // ITreeNode-based plugins can follow, but the snapshot is derived from the object itself, so
        // selection behaves the same with no tree at all (unit tests). See GitHub issue #2268.

        public GlueElement CurrentElement
        {
            get => snapshot.CurrentElement;
            set => SelectTag(value);
        }

        public EntitySave CurrentEntitySave
        {
            get => snapshot.CurrentEntitySave;
            set => CurrentElement = value;
        }

        public ScreenSave CurrentScreenSave
        {
            get => snapshot.CurrentScreenSave;
            set => CurrentElement = value;
        }

        public ReferencedFileSave CurrentReferencedFileSave
        {
            get => snapshot.CurrentReferencedFileSave;
            set => SelectTag(value);
        }

        public NamedObjectSave CurrentNamedObjectSave
        {
            get => snapshot.CurrentNamedObjectSave;
            set => SelectTag(value);
        }

        public IReadOnlyList<NamedObjectSave> CurrentNamedObjectSaves
        {
            get => snapshot.CurrentNamedObjectSaves;
            set => SelectTags(value ?? new List<NamedObjectSave>());
        }

        public StateSave CurrentStateSave
        {
            get => snapshot.CurrentStateSave;
            set => SelectTag(value);
        }

        public StateSaveCategory CurrentStateSaveCategory
        {
            get => snapshot.CurrentStateSaveCategory;
            set => SelectTag(value);
        }

        public CustomVariable CurrentCustomVariable
        {
            get => snapshot.CurrentCustomVariable;
            set => SelectTag(value);
        }

        public EventResponseSave CurrentEventResponseSave
        {
            get => snapshot.CurrentEventResponseSave;
            set => SelectTag(value);
        }

        public string[] CurrentFocusedTabs
        {
            get
            {
                string GetFocusFor(TabContainerViewModel tabContainerVm)
                {
                    foreach(var tabPage in tabContainerVm.Tabs)
                    {
                        if(tabPage.IsSelected)
                        {
                            return tabPage.Title;
                        }
                    }
                    return null;
                }

                List<string> listToReturn = new List<string>();

                GlueCommands.Self.DoOnUiThread(() =>
                {
                    void AddIfNotNull(string value) 
                    {
                        if(value != null)
                        {
                            listToReturn.Add(value);
                        }
                    };

                    AddIfNotNull(GetFocusFor(PluginManager.TabControlViewModel.TopTabItems));
                    AddIfNotNull(GetFocusFor(PluginManager.TabControlViewModel.BottomTabItems));
                    AddIfNotNull(GetFocusFor(PluginManager.TabControlViewModel.LeftTabItems));
                    AddIfNotNull(GetFocusFor(PluginManager.TabControlViewModel.RightTabItems));
                    AddIfNotNull(GetFocusFor(PluginManager.TabControlViewModel.CenterTabItems));

                });

                return listToReturn.ToArray();
            }
        }

        public int? SelectedSubIndex
        {
            get => snapshot.SelectedSubIndex;
            set
            {
                if(snapshot.SelectedSubIndex != value)
                {
                    snapshot.SelectedSubIndex = value;
                    PluginManager.ReactToSelectedSubIndexChanged(snapshot.SelectedSubIndex);
                }
            }
        }

        public List<ProjectBase> SyncedProjects { get; private set; } = new List<ProjectBase>();
        IEnumerable<ProjectBase> IGlueState.SyncedProjects
        {
            get => SyncedProjects;
        }

        #endregion

        #region Project Properties

        public string ContentDirectory
        {
            get
            {
                return CurrentMainProject?.GetAbsoluteContentFolder();
            }
        }

        public FilePath ContentDirectoryPath => ContentDirectory != null
            ? new FilePath(ContentDirectory) : null;

        /// <summary>
        /// Returns the current Glue code project file name
        /// </summary>
        public FilePath CurrentCodeProjectFileName
        {
            get; private set;
        }

        /// <summary>
        /// The directory holding the .gluj, which is what every element's .glsj/.glej and the folders
        /// beside them are relative to.
        /// </summary>
        /// <remarks>
        /// Taken from the .gluj rather than the .csproj. Those are the same directory for a project
        /// Glue maintains, which is why deriving it from the .csproj went unnoticed - but a project
        /// that keeps its Glue files in their own folder resolved every element path against the wrong
        /// root, and the symptom was quiet: "View in Explorer" opened the desktop, because Explorer
        /// falls back to that when handed a path that does not exist.
        /// </remarks>
        public string CurrentGlueProjectDirectory
        {
            get
            {
                // Falls back to the code project's directory once the project has closed.
                // CurrentCodeProjectFileName is deliberately kept after that point for tasks still
                // draining, while GlueProjectFileName goes null with CurrentMainProject - so without
                // this, closing a project turns a path those tasks used into a null reference.
                return GlueProjectFileName?.GetDirectoryContainingThis().FullPath
                    ?? CurrentCodeProjectFileName?.GetDirectoryContainingThis().FullPath;
            }
        }


        VisualStudioProject _currentMainProject;
        public VisualStudioProject CurrentMainProject
        {
            get => _currentMainProject;
            set
            {
                _currentMainProject = value;
                // This preserves the old file name even after we exit in case there are extra tasks running
                if (value != null)
                {
                    CurrentCodeProjectFileName = value.FullFileName;
                }
                // Invalidate - Runner re-reads this every few seconds while polling for the game
                // process, so it can't re-walk the solution and every referenced .csproj each time.
                _currentFrb2LauncherProjectFileNameCache = null;
                _hasCachedCurrentFrb2LauncherProjectFileName = false;
            }
        }

        public VisualStudioProject CurrentMainContentProject { get { return ProjectManager.ContentProject; } }

        public FilePath CurrentSlnFileName => SlnFileForProject(CurrentMainProject);

        bool _hasCachedCurrentFrb2LauncherProjectFileName;
        FilePath _currentFrb2LauncherProjectFileNameCache;

        /// <summary>
        /// The FRB2 launcher project (Desktop today) that actually builds/runs
        /// <see cref="CurrentMainProject"/> - Glue edits Common, but Common alone has no
        /// <c>OutputType</c> and nothing to run (#2188). Null for non-FRB2 projects, or when the
        /// launcher can't be found or is ambiguous - see
        /// <see cref="VSHelpers.ProjectFileResolver.FindFrb2LauncherProject"/>.
        /// </summary>
        /// <remarks>
        /// Cached per <see cref="CurrentMainProject"/>: Runner polls this every few seconds while
        /// watching for the game process, and re-parsing the solution plus every referenced .csproj
        /// that often would be wasteful disk I/O for a value that cannot change without a project
        /// reload.
        /// </remarks>
        public FilePath CurrentFrb2LauncherProjectFileName
        {
            get
            {
                if (_hasCachedCurrentFrb2LauncherProjectFileName)
                {
                    return _currentFrb2LauncherProjectFileNameCache;
                }

                FilePath result = null;

                if (CurrentMainProject is Frb2Project && CurrentSlnFileName != null)
                {
                    var launcherPath = VSHelpers.ProjectFileResolver.FindFrb2LauncherProject(
                        CurrentSlnFileName.FullPath, CurrentMainProject.FullFileName.FullPath);

                    result = launcherPath == null ? null : new FilePath(launcherPath);
                }

                _currentFrb2LauncherProjectFileNameCache = result;
                _hasCachedCurrentFrb2LauncherProjectFileName = true;

                return result;
            }
        }

        public FilePath SlnFileForProject(VisualStudioProject vsproject)
        {
            if (vsproject == null)
            {
                return null;
            }
            else
            {
                var csprojLocation = vsproject.FullFileName;
                return VSHelpers.ProjectSyncer.LocateSolution(csprojLocation);
            }
        }

        public FilePath GlueExeDirectory
        {
            get
            {
                return System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\";
            }
        }

        public string ProjectNamespace
        {
            get
            {
                return ProjectManager.ProjectNamespace;
            }

        }

        /// <summary>
        /// The file name of the GLUX
        /// </summary>
        public FilePath GlueProjectFileName
        {
            get
            {
                if (CurrentMainProject == null)
                {
                    return null;
                }
                else
                {
                    var extension =
                        CurrentGlueProject?.FileVersion >= (int)GlueProjectSave.GluxVersions.GlueSavedToJson
                            ? ".gluj"
                            : ".glux";

                    var withoutExtension = CurrentMainProject.FullFileName.RemoveExtension();

                    var subdirectory = CurrentMainProject.GlueProjectSubdirectory;
                    if (string.IsNullOrEmpty(subdirectory))
                    {
                        return withoutExtension + extension;
                    }

                    var nameOnly = FileManager.RemovePath(withoutExtension.FullPath);

                    // FRB2's Common project is named "<ProjectName>.Common.csproj" (paralleling
                    // .Desktop/.Android/...), but there's only one .gluj for the whole game, so drop
                    // the suffix rather than naming it "<ProjectName>.Common.gluj".
                    if (CurrentMainProject is Frb2Project && nameOnly.EndsWith(".Common"))
                    {
                        nameOnly = nameOnly.Substring(0, nameOnly.Length - ".Common".Length);
                    }

                    // Same file name, different folder - see ProjectBase.GlueProjectSubdirectory. The
                    // Screens/Entities JSON follows automatically: every writer derives its directory
                    // from this path.
                    return CurrentMainProject.Directory + subdirectory + nameOnly + extension;
                }
            }

        }

        public string ProjectSpecificSettingsFolder
        {
            get
            {
                // Deliberately follows the .gluj rather than the .csproj: everything Glue authors has to
                // sit under one folder, so deleting that folder removes every trace of the editor from
                // the project. These settings getting copied to output along with it is harmless and is
                // the accepted cost of that.
                var projectDirectory = GlueProjectFileName.GetDirectoryContainingThis();

                return projectDirectory.FullPath + "GlueSettings/";
            }
        }

        public FilePath ProjectSpecificSettingsPath => new FilePath(ProjectSpecificSettingsFolder);


        public GlueProjectSave CurrentGlueProject => ObjectFinder.Self.GlueProject; 

        public PluginSettings CurrentPluginSettings => ProjectManager.PluginSettings;

        public bool IsProjectLoaded(VisualStudioProject project)
        {
            return CurrentMainProject == project || SyncedProjects.Contains(project);
        }

        /// <summary>
        /// The global glue settings for the current user, not tied to a particular project.
        /// </summary>
        public GlueSettingsSave GlueSettingsSave
        {
            get => ProjectManager.GlueSettingsSave;
            set => ProjectManager.GlueSettingsSave = value;
        }

        public int? EngineDllSyntaxVersion
        {
            get
            {
                // for now we'll use the main project, but eventually we may want to include synced projects too:
                var project = GlueState.Self.CurrentMainProject;
                if (project == null)
                {
                    return null;
                }
                var referenceItems = project.EvaluatedItems.Where(item =>
                {
                    return item.ItemType == "PackageReference" && item.EvaluatedInclude.StartsWith("FlatRedBall");
                });

                foreach (var item in referenceItems)
                {
                    var path = GetFilePathFor(item);

                    if (path != null)
                    {
                        var module = ModuleDefinition.ReadModule(path.FullPath);
                        var frbServicesType = module.Types.FirstOrDefault(item => item.FullName == "FlatRedBall.FlatRedBallServices");
                        foreach (var attribute in frbServicesType.CustomAttributes)
                        {
                            if (attribute.AttributeType.Name == "SyntaxVersionAttribute" && attribute.Fields.Count > 0)
                            {
                                var version = int.Parse(attribute.Fields[0].Argument.Value.ToString());
                                return version;
                            }
                        }
                    }

                }
                return null;
            }
        }

        private static FilePath GetFilePathFor(ProjectItem item)
        {
            string packageName = item.EvaluatedInclude;
            string packageVersion = item.Metadata.FirstOrDefault(item => item.Name == "Version")?.EvaluatedValue;

            var userName = System.Environment.UserName;


            if (userName != null)
            {
                string[] searchPaths = {
                    @"C:\Program Files\dotnet\packs",
                    $@"C:\Users\{userName}\.nuget\packages"
                };

                foreach (string path in searchPaths)
                {
                    string fullPath = System.IO.Path.Combine(path, $"{packageName}", $"{packageVersion}", $"{packageName}.{packageVersion}.nupkg");
                    if (System.IO.File.Exists(fullPath))
                    {
                        var directory = FileManager.GetDirectory(fullPath);
                        // find a .dll with matching file
                        var allFiles = FlatRedBall.IO.FileManager.GetAllFilesInDirectory(directory, "dll");
                        foreach (var file in allFiles)
                        {
                            if (file.Contains($"{packageName}.dll"))
                            {
                                return file;
                            }
                        }
                    }
                }
            }
            return null;
        }

        #endregion

        #region Sub-containers and Self

        static GlueState mSelf;
        public static GlueState Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new GlueState();
                }
                return mSelf;
            }
        }
        public IFindManager Find
        {
            get;
            set;
        }
        public States.Clipboard Clipboard
        {
            get;
            private set;
        }

        public TiledCache TiledCache { get; private set; } = new TiledCache();

        #endregion

        #region Properties

        ITreeNode? draggedTreeNode;
        public ITreeNode? DraggedTreeNode 
        {
            get => draggedTreeNode;
            set
            {
                if(value != draggedTreeNode)
                {
                    if(draggedTreeNode != null)
                    {
                        PluginManager.ReactToGrabbedTreeNodeChanged(draggedTreeNode, TreeNodeAction.Released);
                    }
                    draggedTreeNode = value;
                    if(draggedTreeNode == null)
                    {
                        //GlueCommands.Self.PrintOutput("Released node");
                    }
                    else
                    {
                        if (value != null)
                        {
                            PluginManager.ReactToGrabbedTreeNodeChanged(draggedTreeNode, TreeNodeAction.Grabbed);
                        }

                    }
                }
            }
        }

        GlueStateSnapshot snapshot = new GlueStateSnapshot();

        public ErrorListViewModel ErrorList { get; private set; } = new ErrorListViewModel();

        public static object ErrorListSyncLock = new object();

        public bool IsReferencingFrbSource
        {
            get
            {
                if(CurrentMainProject == null)
                {
                    return false;
                }
                else
                {
                    if(CurrentMainProject is MonoGameDesktopGlBaseProject)
                    {
                        // todo - handle different types of projects
                        string projectReferenceName;
                        if(CurrentMainProject.DotNetVersion.Major >= 6)
                        {
                            projectReferenceName = "FlatRedBallDesktopGLNet6";
                        }
                        else
                        {
                            projectReferenceName = "FlatRedBallDesktopGL";
                        }
                        return CurrentMainProject.HasProjectReference(projectReferenceName);

                    }
                    return false;
                }
            }
        }

        #endregion

        public GlueState()
        {
            // find will be assigned by plugins
            Clipboard = new States.Clipboard();

            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(
                ErrorList.Errors, ErrorListSyncLock);
        }

        /// <summary>
        /// Returns all loaded IDE projects, including the main project and all synced projects.
        /// </summary>
        /// <returns></returns>
        public List<ProjectBase> GetProjects()
        {
            var list = new List<ProjectBase>();

            list.Add(GlueState.Self.CurrentMainProject);

            list.AddRange(ProjectManager.SyncedProjects);

            return list;
        }

        public void SetCurrentTreeNode(ITreeNode treeNode, bool recordState) => UpdateToSetTreeNode(treeNode, recordState);

        private void UpdateToSetTreeNode(ITreeNode value, bool recordState)
        {
            if(value != null)
            {
                UpdateToSetTreeNode(new List<ITreeNode> { value }, recordState);
            }
            else
            {
                UpdateToSetTreeNode(new List<ITreeNode> (), recordState);
            }
        }

        /// <summary>
        /// Selection reported by the tree view (or anything else holding real tree nodes). The nodes'
        /// tags are the selected objects; a node with no tag (a folder, a root node) selects only the
        /// node.
        /// </summary>
        private void UpdateToSetTreeNode(IReadOnlyList<ITreeNode> value, bool recordState)
        {
            var tags = value.Select(item => item.Tag).ToList();
            UpdateSelection(tags, value, recordState);
        }

        private void SelectTag(object tag) => SelectTags(tag == null ? new List<object>() : new List<object> { tag });

        /// <summary>
        /// Selection by model object. Objects that don't belong to the loaded project (a NamedObjectSave
        /// in no element, say - a selection reported by the running game that no longer matches anything
        /// loaded, GitHub issue #2149) are dropped, the same as when the tree view had no node for them,
        /// so an unresolvable selection reads as "nothing selected" rather than a half-updated snapshot.
        /// </summary>
        private void SelectTags(IEnumerable<object> tags)
        {
            var selectable = tags.Where(IsSelectable).ToList();

            var treeNodes = selectable
                .Select(tag => Find?.TreeNodeByTag(tag))
                .Where(node => node != null)
                .ToList();

            UpdateSelection(selectable, treeNodes, recordState: true);
        }

        private static bool IsSelectable(object tag)
        {
            switch (tag)
            {
                case null: return false;
                case GlueElement: return true;
                case ReferencedFileSave: return true;
                case NamedObjectSave nos: return nos.GetContainer() != null;
                case StateSave state: return GetElementContaining(state) != null;
                case StateSaveCategory category: return GetElementContaining(category) != null;
                case CustomVariable variable: return GetElementContaining(variable) != null;
                case EventResponseSave ers: return GetElementContaining(ers) != null;
                default: return true;
            }
        }

        private void UpdateSelection(IReadOnlyList<object> tags, IReadOnlyList<ITreeNode> treeNodes, bool recordState)
        {
            var isSame = snapshot.SelectedTags.SequenceEqual(tags) && snapshot.CurrentTreeNodes.SequenceEqual(treeNodes);

            // Push to the stack for history before taking a snapshot, so that the "old" one is pushed
            if (!isSame && snapshot.CurrentTreeNode != null && recordState)
            {
                // todo - need to support multi select
                TreeNodeStackManager.Self.Push(snapshot.CurrentTreeNode);
            }

            // Snapshot should come first so everyone can update to the snapshot
            TakeSnapshot(tags, treeNodes);

            // If we don't check for isSame, then selecting the same tree node will result in double-selects in the game.
            if(!isSame)
            {
                PluginManager.ReactToItemsSelected(treeNodes.ToList());
            }
        }


        public IEnumerable<ReferencedFileSave> GetAllReferencedFiles()
        {
            return ObjectFinder.Self.GetAllReferencedFiles();
        }

        /// <summary>
        /// Derives every Current* value from the selected objects. Only a tagless tree node (a folder or
        /// root node) needs the tree: its element is whichever element node it sits under.
        /// </summary>
        void TakeSnapshot(IReadOnlyList<object> tags, IReadOnlyList<ITreeNode> treeNodes)
        {
            var first = tags.FirstOrDefault();

            snapshot.SelectedTags = tags.ToList();
            snapshot.CurrentTreeNodes = treeNodes.ToList();
            snapshot.CurrentElement = first != null
                ? GetElementFor(first)
                : treeNodes.FirstOrDefault()?.GetContainingElementTreeNode()?.Tag as GlueElement;
            snapshot.CurrentEntitySave = snapshot.CurrentElement as EntitySave;
            snapshot.CurrentScreenSave = snapshot.CurrentElement as ScreenSave;
            snapshot.CurrentReferencedFileSave = first as ReferencedFileSave;
            snapshot.CurrentNamedObjectSaves = tags.OfType<NamedObjectSave>().ToList();
            snapshot.CurrentStateSave = first as StateSave;
            snapshot.CurrentStateSaveCategory = first switch
            {
                StateSaveCategory category => category,
                StateSave state => GetCategoryContaining(state),
                _ => null
            };
            snapshot.CurrentCustomVariable = first as CustomVariable;
            snapshot.CurrentEventResponseSave = first as EventResponseSave;
            snapshot.SelectedSubIndex = null;
        }

        private static GlueElement GetElementFor(object tag)
        {
            switch (tag)
            {
                case GlueElement element: return element;
                case NamedObjectSave nos: return nos.GetContainer();
                case ReferencedFileSave rfs: return rfs.GetContainer();
                case StateSave state: return GetElementContaining(state);
                case StateSaveCategory category: return GetElementContaining(category);
                case CustomVariable variable: return GetElementContaining(variable);
                case EventResponseSave ers: return GetElementContaining(ers);
                default: return null;
            }
        }

        // ObjectFinder's lookups assume a loaded project; with none, nothing is contained anywhere.
        private static GlueElement GetElementContaining(StateSave state) =>
            ObjectFinder.Self.GlueProject == null ? null : ObjectFinder.Self.GetElementContaining(state);
        private static GlueElement GetElementContaining(StateSaveCategory category) =>
            ObjectFinder.Self.GlueProject == null ? null : ObjectFinder.Self.GetElementContaining(category);
        private static GlueElement GetElementContaining(CustomVariable variable) =>
            ObjectFinder.Self.GlueProject == null ? null : ObjectFinder.Self.GetElementContaining(variable);
        private static StateSaveCategory GetCategoryContaining(StateSave state) =>
            ObjectFinder.Self.GlueProject == null ? null : ObjectFinder.Self.GetStateSaveCategory(state);
        private static GlueElement GetElementContaining(EventResponseSave ers) =>
            ObjectFinder.Self.GlueProject == null ? null : ObjectFinder.Self.GetElementContaining(ers);
    }
}
