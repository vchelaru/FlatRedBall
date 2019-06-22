using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TmxEditor;
using TmxEditor.Controllers;

namespace TileGraphicsPlugin.Managers
{
    public class EntityListManager : FlatRedBall.Glue.Managers.Singleton<EntityListManager>
    {
        private void SetupEntityListInScreen(EntitySave entity, ScreenSave screen)
        {
            if (entity != null && screen != null)
            {
                var hasList =
                    screen.AllNamedObjects.Any(o => o.ClassType == "PositionedObjectList<" + entity.ClassName + ">");

                if (!hasList)
                {
                    screen.NamedObjects.Add(new NamedObjectSave
                    {
                        AddToManagers = true,
                        SourceClassGenericType = entity.Name,
                        SourceClassType = "PositionedObjectList<T>",
                        InstanceName = entity.ClassName + "List",
                        SourceType = SourceType.FlatRedBallType
                    });
                }
            }
        }

        public void OnEntityAssociationsChanged(object sender, EventArgs eventArgs)
        {
            var currentTile = TilesetController.Self.CurrentTilesetTile;

            var property =
                TilesetController.Self.GetExistingProperty(TilesetController.EntityToCreatePropertyName, currentTile);

            if (property == null)
            {
                return;
            }

            var entityToCreate = property.value;
            if (entityToCreate == "None")
            {
                return;
            }
            var entity = GlueState.Self.CurrentGlueProject.Entities.FirstOrDefault(e => e.Name == entityToCreate);
            var screen = GlueState.Self.CurrentScreenSave;

            SetupEntityAssociation(entity);
            SetupEntityListInScreen(entity, screen);
            SetupFileAssociations(screen);

            TileGraphicsPluginClass.ExecuteFinalGlueCommands(entity);
        }

        private void SetupFileAssociations(ScreenSave screen)
        {
            var fileSaves =
                screen.GetAllReferencedFileSavesRecursively().ToList();

            var tmxCSVExists =
                fileSaves.Any(
                    f => AppState.Self.TmxFileName.EndsWith(f.SourceFile, StringComparison.CurrentCultureIgnoreCase) &&
                         f.Name.EndsWith(".csv", StringComparison.CurrentCultureIgnoreCase));

            if (!tmxCSVExists)
            {
                var otherfile =
                    fileSaves.First(
                        f =>
                            AppState.Self.TmxFileName.EndsWith(f.SourceFile, StringComparison.CurrentCultureIgnoreCase) &&
                            f.Name.EndsWith(".tilb"));

                var file = new ReferencedFileSave
                {
                    CreatesDictionary = true,
                    SourceFile = otherfile.SourceFile,
                    Name = otherfile.Name.Replace(".tilb", "Properties.csv"),
                    BuildTool = BuildToolAssociationManager.Self.TmxToCsv.ToString(),
                    IsSharedStatic = true
                };
                file.DestroyOnUnload = false;
                

                string fileName = FlatRedBall.Glue.ProjectManager.ContentDirectory + file.SourceFile;

                var errorMessage = BuildToolAssociationManager.Self.TmxToCsv.PerformBuildOn(
                    fileName , 
                    FlatRedBall.Glue.ProjectManager.ContentDirectory + file.Name, null, FlatRedBall.Glue.Plugins.PluginManager.ReceiveOutput,
                    PluginManager.ReceiveError);

                if (string.IsNullOrWhiteSpace(errorMessage))
                {
                    var tileMapInfoCustomClass =
                        GlueState.Self.CurrentGlueProject.CustomClasses.FirstOrDefault(c => c.Name == "TileMapInfo");
                    if (tileMapInfoCustomClass != null)
                    {
                        tileMapInfoCustomClass.CsvFilesUsingThis.Add(file.Name);
                        screen.ReferencedFiles.Add(file);

                    }
                }
            }
        }



        private void SetupEntityAssociation(EntitySave entity)
        {
            if (entity != null)
            {
                entity.CreatedByOtherEntities = true;
            }
        }
    }
}
