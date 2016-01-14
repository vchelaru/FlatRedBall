using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Content.Particle;
using FlatRedBall;
using FlatRedBall.IO;
using FlatRedBall.Math;
using EditorObjects.EditorSettings;
using FlatRedBall.Gui;
using ParticleEditor.GUI;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Content.Scene;

namespace ParticleEditor.Managers
{
    public class FileCommands
    {
        public string CurrentEmixFileName
        {
            get;
            set;
        }

        public void SaveEmitters(PositionedObjectList<Emitter> emitters, string fileName)
        {


            EmitterSaveList emitterSaveList = EmitterSaveList.FromEmitterList(EditorData.Emitters);

            emitterSaveList.Save(fileName);

#if FRB_MDX


            FlatRedBallServices.Owner.Text = "ParticleEditor - Currently editing " + fileName;
#else
            FlatRedBallServices.Game.Window.Title = "ParticleEditor - Currently editing " + fileName;
#endif
            fileName = FileManager.RemoveExtension(fileName);
            EditorData.CurrentEmixFileName = fileName;


            EmitterEditorSettingsSave settings = new EmitterEditorSettingsSave();
            settings.Camera = CameraSave.FromCamera(Camera.Main);

            if (Camera.Main.Orthogonal && Camera.Main.OrthogonalHeight == Camera.Main.DestinationRectangle.Height)
            {
                settings.Camera.OrthogonalWidth = -1;
                settings.Camera.OrthogonalHeight = -1;
            }


            settings.Save(FileManager.RemoveExtension( fileName ) + ".ess");

        }



        public void LoadEmitters(string fileName)
        {
            #region Clear all Emitters in memory
            while (AppState.Self.Emitters.Count != 0)
            {
                SpriteManager.RemoveEmitter(AppState.Self.Emitters[0]);
            }

            AppState.Self.CurrentEmitter = null;

            #endregion

            #region Load the Emitters and add them to the SpriteManager

            EmitterSaveList emitterSaveList = EmitterSaveList.FromFile(fileName);

            AppState.Self.Emitters = emitterSaveList.ToEmitterList(AppState.Self.PermanentContentManager);

            foreach (Emitter emitter in AppState.Self.Emitters)
            {
                SpriteManager.AddEmitter(emitter);
                ShapeManager.AddPolygon(emitter.EmissionBoundary);
            }

            CurrentEmixFileName = FileManager.RemoveExtension(fileName);
            #endregion

            bool haveAttachments = false;

#if FRB_MDX
            FlatRedBallServices.Owner.Text = "ParticleEditor - Currently editing " + CurrentEmixFileName;
#else
            FlatRedBallServices.Game.Window.Title = "ParticleEditor - Currently editing " + CurrentEmixFileName;

#endif

            for (int i = 0; i < AppState.Self.Emitters.Count; i++)
            {
                if (emitterSaveList.emitters[i].ParentSpriteName != null)
                {
                    // see if the emitter exists in the gameData.emitterArray and set the attachments.  If not, then
                    // we need to set haveAttachments to true, indicating there are attachments to .scn Sprites
                    Emitter e = AppState.Self.Emitters.FindWithNameContaining(emitterSaveList.emitters[i].ParentSpriteName);
                    if (e != null)
                    {

                        AppState.Self.Emitters[i].AttachTo(e, false);

                    }
                    else
                    {
                        haveAttachments = true;
                    }

                }
            }


            // TODO:  Handle when the ParticleEditor can't find attachments.

            if (haveAttachments)
            {
                EditorData.lastLoadedFile = emitterSaveList;

                if (EditorData.Scene == null || EditorData.Scene.Sprites.Count == 0)
                {
                    MultiButtonMessageBox mbmb = GuiManager.AddMultiButtonMessageBox();
                    mbmb.Name = ".emi attachments found";
                    mbmb.Text = fileName + " has one or more attachments.  There are no " +
                        "Sprites loaded.  What would you like to do with the attachment information?";

                    mbmb.ScaleX = 15;

                    mbmb.AddButton("Forget all attachment information.", new GuiMessage(FileMenuWindow.ForgetAttachmentInfo));
                    mbmb.AddButton("Remember attachment information, I will load a .scnx file later.", new GuiMessage(FileMenuWindow.RememberAttachmentInfo));
                    mbmb.AddButton("Manually search for .scnx file now.", new GuiMessage(FileMenuWindow.LoadScnxButtonClick));

                    mbmb.AddButton("Automatically search for .scnx with Sprites matching attachments.", new GuiMessage(FileMenuWindow.AutoSearchScn));


                }
                else
                {
                    FileMenuWindow.AttemptEmitterAttachment("");


                }
            }

            string settingsFileName = FileManager.RemoveExtension( fileName ) + ".ess";

            bool doesSettingsFileExist = System.IO.File.Exists(settingsFileName);

            if (doesSettingsFileExist)
            {
                EmitterEditorSettingsSave settings = EmitterEditorSettingsSave.Load(settingsFileName);

                settings.Camera.SetCamera(Camera.Main);

                if (settings.Camera.OrthogonalHeight < 0)
                {
                    Camera.Main.UsePixelCoordinates();
                }
                else
                {
                    Camera.Main.FixAspectRatioYConstant();
                }
            }

        }


    }
}
