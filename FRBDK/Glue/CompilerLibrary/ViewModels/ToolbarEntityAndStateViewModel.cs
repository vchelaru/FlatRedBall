using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using CompilerLibrary.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static System.Windows.Forms.AxHost;

namespace CompilerLibrary.ViewModels
{
    public class ToolbarEntityAndStateViewModel : ViewModel
    {
        #region Events and related

        public ICommand ClickedCommand { get; private set; } 
        public ICommand RemoveFromToolbarCommand { get; private set; }

        public ICommand ForceRefreshPreviewCommand { get; private set; }
        public ICommand ViewInExplorerCommand { get;private set; }
        public ICommand SelectPreviewFileCommand { get; private set; }
        public ICommand SelectPreviewFromEntityCommand { get; private set; }

        private Func<string, string, Task<string>> _pluginAction;

        void RaiseClicked() => Clicked?.Invoke();
        void RaiseRemoveFromToolbar() => RemovedFromToolbar?.Invoke();
        void RaiseForceRefreshPreview() => ForceRefreshPreview?.Invoke();
        void RaiseViewInExplorerCommand() => ViewInExplorer?.Invoke();
        void RaiseSelectPreviewFile() => SelectPreviewFile?.Invoke();
        void RaiseSelectPreviewFromEntity() => SelectPreviewFromEntity?.Invoke();

        public event Action Clicked;
        public event Action RemovedFromToolbar;
        public event Action ForceRefreshPreview;
        public event Action DragLeave;
        public event Action ViewInExplorer;
        public event Action SelectPreviewFile;
        public event Action SelectPreviewFromEntity;

        #endregion

        #region Fields/Properties

        public NamedObjectSave NamedObjectSave { get; set; }

        public ImageSource ImageSource
        {
            get => Get<ImageSource>();
            set => Set(value) ;
        }

        public string CustomPreviewLocation
        {
            get => Get<string>();
            set => Set(value);
        }

        [DependsOn(nameof(NamedObjectSave))]
        public string TooltipText
        {
            get
            {
                var toReturn = NamedObjectSave.SourceClassType;
                if(toReturn?.StartsWith(@"Entities\", StringComparison.OrdinalIgnoreCase) ?? false)
                {
                    toReturn = toReturn[@"Entities\".Length..];
                }

                foreach(var variable in ExceptXYZ( NamedObjectSave.InstructionSaves))
                {
                    toReturn += $"\n{variable.Member} = {variable.Value}";
                }

                // NamedObjects can have any number of properties assigned. Previously
                // we would use state but...what should we do now? We could randomly loop through
                // variables, limiting to something like 5? Or would that be too noisy?
                return toReturn;
            }
        }

        [DependsOn(nameof(CustomPreviewLocation))]
        public string RefreshPreviewText
        {
            get
            {
                if(string.IsNullOrEmpty(CustomPreviewLocation))
                {
                    return "Refresh Preview";
                }
                else
                {
                    return $"Revert to default preview for {NamedObjectSave.SourceClassType}";
                }
            }
        }

        #endregion

        CustomVariableInNamedObject[] ExceptXYZ(List<CustomVariableInNamedObject> variables) => variables
            .Where(item => item.Member != "X" && item.Member != "Y" && item.Member != "Z")
            .OrderBy(item => item.Member)
            .ToArray();

        public ToolbarEntityAndStateViewModel(Func<string, string, Task<string>> pluginAction)
        {
            ClickedCommand = new Command(RaiseClicked);
            RemoveFromToolbarCommand = new Command(RaiseRemoveFromToolbar);
            ForceRefreshPreviewCommand = new Command(RaiseForceRefreshPreview);
            ViewInExplorerCommand = new Command(RaiseViewInExplorerCommand);
            SelectPreviewFileCommand = new Command(RaiseSelectPreviewFile);
            SelectPreviewFromEntityCommand = new Command(RaiseSelectPreviewFromEntity);
            _pluginAction = pluginAction;
        }

        public void ApplyTo(ToolbarModel toolbarModel)
        {
            toolbarModel.CustomPreviewLocation = CustomPreviewLocation;
            toolbarModel.NamedObject = NamedObjectSave.Clone();
        }

        internal void SetFrom(ToolbarModel item)
        {
            this.CustomPreviewLocation = item.CustomPreviewLocation;
            this.NamedObjectSave = item.NamedObject.Clone();

        }

        public void HandleDragLeave() => DragLeave?.Invoke();
    }
}
