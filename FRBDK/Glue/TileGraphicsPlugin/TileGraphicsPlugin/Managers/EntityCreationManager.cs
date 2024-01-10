using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TileGraphicsPlugin.ViewModels;
using TileGraphicsPlugin.Views;

namespace TileGraphicsPlugin.Managers
{
    public class EntityCreationManager : Singleton<EntityCreationManager>
    {
        public const string CreateEntitiesInGeneratedCodePropertyName = "CreateEntitiesInGeneratedCode";

        TiledMapEntityCreationViewModel viewModel;

        bool ReactingToChangedProperties = true;

        //public void AddEntityCreationView(TmxEditorControl control)
        //{
        //    var entitiesView = new TiledMapEntityCreationView();
        //    entitiesView.ViewTiledObjectXmlClicked += HandleViewTiledObjectXmlClicked;
        //    if(viewModel == null)
        //    {
        //        viewModel = new TiledMapEntityCreationViewModel();
        //    }
        //    viewModel.PropertyChanged += HandleEntitiesTabPropertyChanged;
        //    entitiesView.DataContext = viewModel;
        //    control.AddTab("Entities", entitiesView);
        //}

        private void HandleViewTiledObjectXmlClicked(object sender, EventArgs e)
        {
            var fileLocation = TiledObjectTypeCreator.GetTiledObjectTypeFileName();

            var locationToShow = $"\"{fileLocation.FullPath.Replace("/", "\\")}\"";

            System.Diagnostics.Process.Start("explorer.exe", "/select," + locationToShow);
        }

        private void HandleEntitiesTabPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(ReactingToChangedProperties)
            {
                switch (e.PropertyName)
                {
                    case nameof(TiledMapEntityCreationViewModel.CreateEntitiesInGeneratedCode):

                        var currentRfs = GlueState.Self.CurrentReferencedFileSave;

                        if (currentRfs != null)
                        {
                            currentRfs.SetProperty(CreateEntitiesInGeneratedCodePropertyName, true);

                            GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
                            GlueCommands.Self.GluxCommands.SaveProjectAndElements();
                        }

                        break;
                }
            }
        }

        internal void ReactToRfsSelected(ReferencedFileSave rfs)
        {
            ReactingToChangedProperties = false;

            if(GlueState.Self.CurrentReferencedFileSave != null)
            {
                if (viewModel == null)
                {
                    viewModel = new TiledMapEntityCreationViewModel();
                }

                viewModel.CreateEntitiesInGeneratedCode = rfs.GetProperty<bool>(
                    CreateEntitiesInGeneratedCodePropertyName);


            }

            ReactingToChangedProperties = true;

        }
    }
}
