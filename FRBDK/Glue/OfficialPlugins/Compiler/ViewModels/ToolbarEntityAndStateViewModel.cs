using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.Compiler.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OfficialPlugins.Compiler.ViewModels
{
    public class ToolbarEntityAndStateViewModel : ViewModel
    {
        public ICommand ClickedCommand { get; private set; } 
        public ICommand RemoveFromToolbarCommand { get; private set; }

        public ICommand ForceRefreshPreviewCommand { get; private set; }

        void RaiseClicked() => Clicked?.Invoke();
        void RaiseRemoveFromToolbar() => RemovedFromToolbar?.Invoke();
        void RaiseForceRefreshPreview() => ForceRefreshPreview?.Invoke();

        public event Action Clicked;
        public event Action RemovedFromToolbar;
        public event Action ForceRefreshPreview;

        public ToolbarEntityAndStateViewModel()
        {
            ClickedCommand = new Command(RaiseClicked);
            RemoveFromToolbarCommand = new Command(RaiseRemoveFromToolbar);
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

        internal void ApplyTo(ToolbarEntityAndState toolbarModel)
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
        internal void SetSourceFromElementAndState(bool force = false)
        {
            var imageFilePath = GlueCommands.Self.GluxCommands.GetPreviewLocation(GlueElement, StateSave);

            if (!imageFilePath.Exists() || force)
            {
                var image = PreviewGenerator.Managers.PreviewGenerationLogic.GetImageSourceForSelection(null, GlueElement, StateSave);
                PreviewGenerator.Managers.PreviewSaver.SavePreview(image as BitmapSource, GlueElement, StateSave);
            }

            if (imageFilePath.Exists())
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.UriSource = new Uri(imageFilePath.FullPath, UriKind.Relative);
                bitmapImage.EndInit();
                ImageSource = bitmapImage;
            }


        }
    }
}
