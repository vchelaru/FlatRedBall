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

        public GlueElement GlueElement { get; set; }
        public StateSave StateSave { get; set; }
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
                var entityName = GlueElement.Name;
                if(entityName?.StartsWith("Entities\\") == true)
                {
                    entityName = entityName.Substring("Entities\\".Length);
                }
                if(StateSave == null)
                {
                    return entityName;
                }
                else
                {
                    return $"{StateSave.Name}\n({entityName})";
                }
            }
        }

        public void ApplyTo(ToolbarEntityAndState toolbarModel)
        {

            StateSaveCategory category = null;
            if(StateSave != null)
            {
                category = ObjectFinder.Self.GetStateSaveCategory(StateSave);
            }
            toolbarModel.CategoryName = category?.Name;
            toolbarModel.StateName = StateSave?.Name;
            toolbarModel.EntityName = GlueElement?.Name;
        }

        internal void SetFrom(ToolbarEntityAndState item)
        {
            GlueElement = ObjectFinder.Self.GetElement(item.EntityName) as EntitySave;
            if(!string.IsNullOrEmpty(item.StateName))
            {
                StateSaveCategory category = null;
                if(item.CategoryName != null)
                {
                    category = GlueElement?.GetStateCategory(item.CategoryName);
                }

                StateSave = category?.GetState(item.StateName) ??
                    GlueElement?.States.FirstOrDefault(possibleState => possibleState.Name == item.StateName);
            }
        }

        /// <summary>
        /// Sets the ImageSource according to a preview generated based on the Element and its State.
        /// This method will generate a cached PNG on disk if it doesn't already exist, or if force is true.
        /// </summary>
        /// <param name="force">Whether to force generate the preview PNG. If true, then the PNG
        /// will be generated even if it already exists.</param>
        public void SetSourceFromElementAndState(bool force = false)
        {
            var imageFilePath = GlueCommands.Self.GluxCommands.GetPreviewLocation(GlueElement, StateSave);

            if (!imageFilePath.Exists() || force)
            {
                var geId = Guid.NewGuid();
                var ssId = Guid.NewGuid();

                _pluginStorge.TryAdd(geId, GlueElement);
                _pluginStorge.TryAdd(ssId, StateSave);

                var result = _pluginAction("PreviewGenerator_SaveImageSourceForSelection", JsonConvert.SerializeObject(new
                {
                    ImageFilePath = imageFilePath,
                    NamedObjectSave = (Guid?)null,
                    Element = geId,
                    State = ssId
                })).Result;
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
