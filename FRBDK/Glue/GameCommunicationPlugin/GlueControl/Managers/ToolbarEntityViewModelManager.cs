using CompilerLibrary.ViewModels;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.ViewModels;
using FlatRedBall.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using GameCommunicationPlugin.Dtos;
using System.Windows.Controls;
using static System.Windows.Forms.AxHost;
using FlatRedBall.Utilities;

namespace GameCommunicationPlugin.GlueControl.Managers
{
    static class ToolbarEntityViewModelManager
    {
        public static CompilerViewModel CompilerViewModel { get; set; }
        public static Func<string, string, Task<string>> ReactToPluginEventWithReturn { get; set; }
        public static Action SaveCompilerSettingsModel;
        public static CustomVariableInNamedObject[] ExceptXYZ(List<CustomVariableInNamedObject> variables) => variables
            .Where(item => item.Member != "X" && item.Member != "Y" && item.Member != "Z")
            .OrderBy(item => item.Member)
            .ToArray();

        public static ToolbarEntityAndStateViewModel CreateNewViewModel(NamedObjectSave namedObject,
            string customPreviewLocation = null)
        {
            var newViewModel = new ToolbarEntityAndStateViewModel(ReactToPluginEventWithReturn);
            newViewModel.CustomPreviewLocation = customPreviewLocation;
            newViewModel.NamedObjectSave = namedObject.Clone();
            newViewModel.Clicked += async () =>
            {
                var canEdit = CompilerViewModel.IsRunning && CompilerViewModel.IsEditChecked;
                if (!canEdit)
                {
                    return;
                }

                var element = GlueState.Self.CurrentElement;

                NamedObjectSave newNos = null;

                if (element != null)
                {
                    var addObjectViewModel = new AddObjectViewModel();
                    addObjectViewModel.SourceType = SourceType.Entity;
                    var entitySave = ObjectFinder.Self.GetEntitySave(namedObject.SourceClassType);

                    addObjectViewModel.SelectedEntitySave = entitySave;

                    var listToAddTo = ObjectFinder.Self.GetDefaultListToContain(entitySave.Name, element);

                    newNos = await GlueCommands.Self.GluxCommands.AddNewNamedObjectToAsync(
                        addObjectViewModel,
                        element,
                        listToAddTo);

                    var variablesToAssign = ExceptXYZ(namedObject.InstructionSaves);
                    if (variablesToAssign.Length > 0)
                    {
                        List<NosVariableAssignment> assignments = new List<NosVariableAssignment>();

                        foreach (var variable in variablesToAssign)
                        {
                            var assignment = new NosVariableAssignment();
                            assignment.NamedObjectSave = newNos;
                            assignment.VariableName = variable.Member;
                            assignment.Value = variable.Value;

                            assignments.Add(assignment);
                        }

                        await GlueCommands.Self.GluxCommands.SetVariableOnList(
                            assignments);

                    }
                }
            };
            newViewModel.RemovedFromToolbar += () =>
            {
                CompilerViewModel.ToolbarEntitiesAndStates.Remove(newViewModel);
            };
            newViewModel.ForceRefreshPreview += () =>
            {
                newViewModel.CustomPreviewLocation = null;
                SetSourceFromElementAndState(newViewModel, force: true);
            };
            newViewModel.SelectPreviewFile += () =>
            {
                var element = ObjectFinder.Self.GetElement(namedObject);

                var openFileDialog = new System.Windows.Forms.OpenFileDialog();
                openFileDialog.Filter = "*.png|*.png";
                openFileDialog.InitialDirectory =
                    GlueCommands.Self.GluxCommands.GetPreviewLocation(element)
                        .GetDirectoryContainingThis()
                        .FullPath
                        .Replace("/", "\\");
                var result = openFileDialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    FilePath value = openFileDialog.FileName;

                    // make this relative to the gluj
                    var relativePath = value.RelativeTo(GlueState.Self.CurrentGlueProjectDirectory);

                    newViewModel.CustomPreviewLocation = relativePath;
                    SetSourceFromElementAndState(newViewModel);
                    SaveCompilerSettingsModel();
                }
            };
            newViewModel.SelectPreviewFromEntity += () => HandleSelectPreviewFromEntity(newViewModel);
            newViewModel.ViewInExplorer += () =>
            {
                GlueCommands.Self.FileCommands.ViewInExplorer(GetPreviewPathFor(newViewModel));
            };
            newViewModel.DragLeave += () =>
            {
                if (GlueState.Self.DraggedTreeNode == null)
                {
                    var element = ObjectFinder.Self.GetElement(namedObject);

                    // Simulate having grabbed the tree node
                    var tag = element;
                    var treeNode = GlueState.Self.Find.TreeNodeByTag(tag);
                    GlueState.Self.DraggedTreeNode = treeNode;
                }

            };

            SetSourceFromElementAndState(newViewModel, force: true);

            return newViewModel;
        }

        static FilePath GetPreviewPathFor(ToolbarEntityAndStateViewModel model)
        {
            FilePath filePath = null;

            if(!string.IsNullOrEmpty(model.CustomPreviewLocation))
            {
                filePath = GlueState.Self.CurrentGlueProjectDirectory + model.CustomPreviewLocation;
            }
            else
            {
                var element = ObjectFinder.Self.GetElement(model.NamedObjectSave);
                if(element != null)
                {
                    filePath = GlueCommands.Self.GluxCommands.GetPreviewLocation(element, null);
                }
            }

            return filePath;
        }

        private static async void HandleSelectPreviewFromEntity(ToolbarEntityAndStateViewModel viewModel)
        {
            var window = new ListBoxWindowWpf();

            window.Message = "Select an entity. A custom PNG will be created for this named object";

            var label = new Label();
            label.Content = "File Name:";
            window.AddControl(label);

            var textBox = new TextBox();

            var element = ObjectFinder.Self.GetEntitySave( viewModel.NamedObjectSave);

            var previewDirectory = GlueCommands.Self.GluxCommands.GetPreviewLocation(element, stateSave: null)
                .GetDirectoryContainingThis();

            var name = element.GetStrippedName() + ".preview";

            while(System.IO.File.Exists(previewDirectory + name + ".png"))
            {
                var withoutPreview = name.Substring(name.IndexOf("."));
                withoutPreview = StringFunctions.IncrementNumberAtEnd(withoutPreview);
                name = withoutPreview + ".preview";
            }


            textBox.Text = name;



            window.AddControl(textBox);

            foreach (var entity in GlueState.Self.CurrentGlueProject.Entities)
            {
                window.AddItem(entity.Name);
            }

            var result = window.ShowDialog();

            if (result == true)
            {
                var selectedEntityName = window.SelectedListBoxItem as string;

                FilePath imageFilePath = previewDirectory + 
                    textBox.Text + ".png";

                var dto = new GeneratePreviewDto
                {
                    ImageFilePath = imageFilePath.FullPath,
                    Element = selectedEntityName,
                };
                var json = JsonConvert.SerializeObject(dto);
                await ReactToPluginEventWithReturn("PreviewGenerator_SaveImageSourceForSelection", json);

                ApplyImageToViewModel(imageFilePath, viewModel);

                viewModel.CustomPreviewLocation =  imageFilePath.RelativeTo(GlueState.Self.CurrentGlueProjectDirectory);
                SaveCompilerSettingsModel();

            }
        }


        /// <summary>
        /// Sets the ImageSource according to a preview generated based on the Element and its State.
        /// This method will generate a cached PNG on disk if it doesn't already exist, or if force is true.
        /// </summary>
        /// <param name="force">Whether to force generate the preview PNG. If true, then the PNG
        /// will be generated even if it already exists.</param>
        static async void SetSourceFromElementAndState(ToolbarEntityAndStateViewModel viewModel, bool force = false)
        {
            FlatRedBall.IO.FilePath imageFilePath = null;
            if (string.IsNullOrEmpty(viewModel.CustomPreviewLocation))
            {
                var element = ObjectFinder.Self.GetElement(viewModel.NamedObjectSave.SourceClassType);
                // for now don't do any states...
                StateSave state = null;
                imageFilePath = GlueCommands.Self.GluxCommands.GetPreviewLocation(element, stateSave: null);

                if (!imageFilePath.Exists() || force)
                {
                    StateSaveCategory category = null;
                    if (state != null)
                    {
                        category = ObjectFinder.Self.GetStateSaveCategory(state);
                    }

                    var dto = new GeneratePreviewDto
                    {
                        ImageFilePath = imageFilePath.FullPath,
                        //NamedObjectSave = (Guid?)null,
                        Element = element?.Name,
                        CategoryName = category?.Name,
                        State = state?.Name
                    };

                    var json = JsonConvert.SerializeObject(dto);

                    var result = await ReactToPluginEventWithReturn("PreviewGenerator_SaveImageSourceForSelection", json);
                }
            }
            else
            {
                imageFilePath = GlueState.Self.CurrentGlueProjectDirectory + viewModel.CustomPreviewLocation;
            }

            ApplyImageToViewModel(imageFilePath, viewModel);
        }

        private static void ApplyImageToViewModel(FilePath imageFilePath, ToolbarEntityAndStateViewModel viewModel)
        {
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
                    viewModel.ImageSource = bitmap;
                }
            }
        }

        internal static void ReactToFileChanged(string fileName)
        {
            foreach(var item in CompilerViewModel.ToolbarEntitiesAndStates)
            {
                var previewFilePath = GetPreviewPathFor(item);

                if(previewFilePath == fileName)
                {
                    ApplyImageToViewModel(fileName, item);
                }
            }
        }
    }
}
