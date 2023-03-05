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
        public ICommand ClickedCommand { get; private set; } 
        public ICommand RemoveFromToolbarCommand { get; private set; }

        public ICommand ForceRefreshPreviewCommand { get; private set; }
        public ICommand ViewInExplorerCommand { get;private set; }

        private Func<string, string, Task<string>> _pluginAction;
        private ConcurrentDictionary<Guid, object> _pluginStorge;

        void RaiseClicked() => Clicked?.Invoke();
        void RaiseRemoveFromToolbar() => RemovedFromToolbar?.Invoke();
        void RaiseForceRefreshPreview() => ForceRefreshPreview?.Invoke();
        void RaiseViewInExplorerCommand() => ViewInExplorer?.Invoke();

        public event Action Clicked;
        public event Action RemovedFromToolbar;
        public event Action ForceRefreshPreview;
        public event Action DragLeave;
        public event Action ViewInExplorer;

        public ToolbarEntityAndStateViewModel(Func<string, string, Task<string>> pluginAction, ConcurrentDictionary<Guid, object> pluginStorage)
        {
            ClickedCommand = new Command(RaiseClicked);
            RemoveFromToolbarCommand = new Command(RaiseRemoveFromToolbar);
            ForceRefreshPreviewCommand = new Command(RaiseForceRefreshPreview);
            ViewInExplorerCommand = new Command(RaiseViewInExplorerCommand);
            _pluginAction = pluginAction;
            _pluginStorge = pluginStorage;
        }

        public NamedObjectSave NamedObjectSave { get; set; }

        public ImageSource ImageSource
        {
            get => Get<ImageSource>();
            set => Set(value) ;
        }

        [DependsOn(nameof(StateSave))]
        [DependsOn(nameof(GlueElement))]
        public string TooltipText
        {
            get
            {
                var entityName = NamedObjectSave.SourceClassType;
                if(entityName?.StartsWith("Entities\\") == true)
                {
                    entityName = entityName.Substring("Entities\\".Length);
                }

                // NamedObjects can have any number of properties assigned. Previously
                // we would use state but...what should we do now? We could randomly loop through
                // variables, limiting to something like 5? Or would that be too noisy?
                return entityName;
            }
        }

        public void ApplyTo(ToolbarModel toolbarModel)
        {
            toolbarModel.NamedObject = NamedObjectSave.Clone();
        }

        internal void SetFrom(ToolbarModel item)
        {
            this.NamedObjectSave = item.NamedObject.Clone();
        }

        /// <summary>
        /// Sets the ImageSource according to a preview generated based on the Element and its State.
        /// This method will generate a cached PNG on disk if it doesn't already exist, or if force is true.
        /// </summary>
        /// <param name="force">Whether to force generate the preview PNG. If true, then the PNG
        /// will be generated even if it already exists.</param>
        public async void SetSourceFromElementAndState(bool force = false)
        {
            var element = ObjectFinder.Self.GetElement(NamedObjectSave.SourceClassType);
            // for now don't do any states...
            StateSave state = null;
            var imageFilePath = GlueCommands.Self.GluxCommands.GetPreviewLocation(element, stateSave:null);

            if (!imageFilePath.Exists() || force)
            {
                StateSaveCategory category = null;
                if(state != null)
                {
                    category = ObjectFinder.Self.GetStateSaveCategory(state);
                }
                
                var dto = new
                {
                    ImageFilePath = imageFilePath.FullPath,
                    //NamedObjectSave = (Guid?)null,
                    Element = element?.Name,
                    CategoryName = category?.Name,
                    State = state?.Name
                };

                var json = JsonConvert.SerializeObject(dto);

                var result = await _pluginAction("PreviewGenerator_SaveImageSourceForSelection", json);
            }

            if (imageFilePath.Exists())
            {
                // Loading PNGs in WPF sucks
                // This code works, but it holds
                // on to the file path, so that 
                // saving the file after it's loaded
                // doesn't work:
                //var bitmapImage = new BitmapImage(new Uri(imageFilePath.FullPath));
                //ImageSource = bitmapImage;

                // If I use "OnLoad" cache, it works, but it never re-loads...
                //var bitmapImage = new BitmapImage();
                //bitmapImage.BeginInit();
                //// OnLoad means we can't refresh this image if the user makes changes to the object
                ////bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                ////bitmapImage.CacheOption = BitmapCacheOption.None;
                ////bitmapImage.CacheOption = BitmapCacheOption.OnDemand;
                //bitmapImage.UriSource = new Uri(imageFilePath.FullPath, UriKind.Relative);
                //bitmapImage.EndInit();

                // Big thanks to Thraka who helped me figure this out. Using my own stream allows the file
                // to be loaded AND allows it to be re-loaded
                using (var stream = System.IO.File.OpenRead(imageFilePath.FullPath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    ImageSource = bitmap;
                }
            }
        }

        public void HandleDragLeave() => DragLeave?.Invoke();
    }
}
