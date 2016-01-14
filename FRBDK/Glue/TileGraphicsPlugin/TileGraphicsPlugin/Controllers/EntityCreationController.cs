using FlatRedBall.Content.Scene;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TileGraphicsPlugin.Controls;
using TmxEditor;
using TmxEditor.Controllers;

namespace TileGraphicsPlugin.Controllers
{
    public class EntityCreationController : FlatRedBall.Glue.Managers.Singleton<EntityCreationController>
    {
        int TileWidth
        {
            get
            {
                return AppState.Self.CurrentTileset.Tilewidth;
            }
        }

        int TileHeight
        {
            get
            {
                return AppState.Self.CurrentTileset.Tileheight;
            }
        }

        public void HandleCreateEntityClick(object sender, EventArgs e)
        {
            if (AppState.Self.CurrentMapTilesetTile == null)
            {
                MessageBox.Show("You must first select a tile");
                return;
            }

            TextInputWindow tiw;
            ControlForAddingCollision collisionControl;
            DialogResult result;
            AskToCreateEntity(out tiw, out collisionControl, out result);

            if (result == DialogResult.OK)
            {
                string entityName = tiw.Result;

                string whyItIsntValid;
                if (NameVerifier.IsEntityNameValid(entityName, null, out whyItIsntValid) == false)
                {
                    MessageBox.Show(whyItIsntValid);
                }
                else
                {
                    var newEntity = GlueCommands.Self.GluxCommands.EntityCommands.AddEntity(
                        entityName, is2D: true);
                    // Give it a factory so that instances can automatically be added
                    newEntity.CreatedByOtherEntities = true;
                    
                    ReferencedFileSave rfs = CreateReferencedFileSave();
                    newEntity.ReferencedFiles.Add(rfs);

                    NamedObjectSave nos = CreateSpriteNamedObject(rfs);
                    newEntity.NamedObjects.Add(nos);

                    if (collisionControl.HasCollision)
                    {
                        NamedObjectSave collisionNos = CreateCollisionNamedObject(
                            collisionControl.CircleSelected, collisionControl.RectangleSelected);
                        newEntity.NamedObjects.Add(collisionNos);
                    }

                    AddNamePropertyToTile(entityName);

                    GlueCommands.Self.GluxCommands.SaveGlux();
                    GlueCommands.Self.UpdateCommands.Update(newEntity);

                    TileGraphicsPluginClass.Self.SaveTiledMapSave(changeType:TmxEditor.Events.ChangeType.Tileset);
                    TileGraphicsPluginClass.Self.UpdateTilesetDisplay();
                }
            }
        }

        private static void AskToCreateEntity(out TextInputWindow tiw, out ControlForAddingCollision collisionControl, out DialogResult result)
        {
            tiw = new TextInputWindow();
            tiw.DisplayText = "Enter entity name:";

            collisionControl = new ControlForAddingCollision();

            tiw.AddControl(collisionControl);

            result = tiw.ShowDialog();
        }

        private void AddNamePropertyToTile(string entityName)
        {
            TMXGlueLib.property property = null;


            property = AppState.Self.CurrentMapTilesetTile.properties.FirstOrDefault(item => item.StrippedNameLower == "name");
            if(property == null)
            {
                property = TilesetController.Self.AddProperty(AppState.Self.CurrentMapTilesetTile, "Name", "string");
            }

            property.value = entityName;
        }

        private ReferencedFileSave CreateReferencedFileSave()
        {
            ReferencedFileSave rfs = new ReferencedFileSave();
            string entireFileName = null;

            var currentTileset = AppState.Self.CurrentTileset;
            
            if(currentTileset.IsShared)
            {
                var tsxFile = FileManager.RemoveDotDotSlash( AppState.Self.TmxFolder + currentTileset.Source);

                var external = FileManager.XmlDeserialize<ExternalTileSet>(tsxFile);

                entireFileName = FileManager.RemoveDotDotSlash(
                    FileManager.GetDirectory(tsxFile) + external.Images[0].Source);

            }
            else
            {
                entireFileName =
                    AppState.Self.TmxFolder +
                    currentTileset.Images[0].sourceFileName;
            }

            // Make this relative to the content directory
            string relativeToContent = FileManager.MakeRelative(entireFileName,
                GlueState.Self.ContentDirectory);
            rfs.Name = relativeToContent;
            rfs.RuntimeType = "Microsoft.Xna.Framework.Graphics.Texture2D";
            return rfs;
        }

        private NamedObjectSave CreateSpriteNamedObject(ReferencedFileSave rfs)
        {
            SpriteSave spriteSave = new SpriteSave();

            int imageHeight = AppState.Self.CurrentTileset.Images[0].height;
            int imageWidth = AppState.Self.CurrentTileset.Images[0].width;

            var currentTileset = AppState.Self.CurrentTileset;
            var id = (uint)(AppState.Self.CurrentMapTilesetTile.id + currentTileset.Firstgid);


            AppState.Self.CurrentTiledMapSave.SetSpriteTextureCoordinates(
                id,
                spriteSave,
                currentTileset);

            NamedObjectSave nos = new NamedObjectSave();
            nos.SourceType = SourceType.FlatRedBallType;
            nos.SourceClassType = "Sprite";
            nos.InstanceName = "Sprite";

            nos.InstructionSaves.Add(new CustomVariableInNamedObject() { Member = "Texture", Type="Texture2D", Value = rfs.GetInstanceName() });

            nos.InstructionSaves.Add(new CustomVariableInNamedObject() { Member = "TextureScale", Value = 1.0f });


            nos.InstructionSaves.Add(new CustomVariableInNamedObject() { Member = "TopTexturePixel", Value = spriteSave.TopTextureCoordinate * (float)imageHeight });

            nos.InstructionSaves.Add(new CustomVariableInNamedObject() { Member = "BottomTexturePixel", Value = spriteSave.BottomTextureCoordinate * (float)imageHeight });

            nos.InstructionSaves.Add(new CustomVariableInNamedObject() { Member = "LeftTexturePixel", Value = spriteSave.LeftTextureCoordinate * (float)imageWidth });

            nos.InstructionSaves.Add(new CustomVariableInNamedObject() { Member = "RightTexturePixel", Value = spriteSave.RightTextureCoordinate * (float)imageWidth });

            return nos;
        }

        private NamedObjectSave CreateCollisionNamedObject(bool isCircle, bool isRectangle)
        {
            NamedObjectSave nos = new NamedObjectSave();

            nos.SourceType = SourceType.FlatRedBallType;

            if (isCircle)
            {
                nos.SourceClassType = "Circle";
                nos.InstructionSaves.Add(new CustomVariableInNamedObject() { Member = "Radius", Value = TileWidth/2.0f });
            }
            else if(isRectangle)
            {
                nos.SourceClassType = "AxisAlignedRectangle";
                nos.InstructionSaves.Add(new CustomVariableInNamedObject() { Member = "Width", Value = (float)TileWidth });
                nos.InstructionSaves.Add(new CustomVariableInNamedObject() { Member = "Height", Value = (float)TileHeight });
            }

            nos.InstanceName = "Collision";
            nos.HasPublicProperty = true;

            return nos;
        }
    }
}
