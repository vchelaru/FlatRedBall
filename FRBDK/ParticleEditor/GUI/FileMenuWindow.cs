using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

using FlatRedBall;

using FlatRedBall.Content;
using FlatRedBall.Content.Particle;


using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Particle;

using FlatRedBall.Gui;

using FlatRedBall.Instructions;

using FlatRedBall.IO;

using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Content.Scene;




namespace ParticleEditor.GUI
{
	/// <summary>
	/// Summary description for FileMenuWindow.
	/// </summary>
	public static class FileMenuWindow
	{
		static SpriteList blueprintSprites = new SpriteList();

		#region Methods

		#region load
		public static void LoadEmittersClick(FlatRedBall.Gui.Window callingWindow)
		{
			FileWindow tempWindow = GuiManager.AddFileWindow();
            tempWindow.Filter = "XML Emitter (*.emix)|*.emix";
            tempWindow.CurrentFileType = "emix";
			tempWindow.OkClick += new GuiMessage(LoadEmitterOK);
		}


		public static void LoadEmitterOK(FlatRedBall.Gui.Window callingWindow)
		{
			string fileName = ((FileWindow)callingWindow).Results[0];

            AppCommands.Self.File.LoadEmitters(fileName);

		}


		public static void LoadScnxButtonClick(FlatRedBall.Gui.Window callingWindow)
		{
			FileWindow tempWindow = GuiManager.AddFileWindow();
			tempWindow.SetFileType("scnx");
			tempWindow.OkClick += new GuiMessage(LoadScnxFileWindowOK);
		}


		public static void LoadScnxFileWindowOK(FlatRedBall.Gui.Window callingWindow)
		{
			EditorData.LoadScene(((FileWindow)callingWindow).Results[0]);
		}


		public static void AttemptEmitterAttachment(string scnFileName)
		{
            if (EditorData.lastLoadedFile != null)
			{
                for (int i = EditorData.lastLoadedFile.emitters.Count - 1; i > -1; i--)
				{
                    if (EditorData.lastLoadedFile.emitters[i].ParentSpriteName != null)
					{
                        Sprite tempParentSprite = EditorData.Scene.Sprites.FindWithNameContaining(
                            EditorData.lastLoadedFile.emitters[i].ParentSpriteName);

						if(tempParentSprite != null)
						{
                            EditorData.Emitters.FindWithNameContaining(EditorData.lastLoadedFile.emitters[i].Name).AttachTo(tempParentSprite, false);
                            EditorData.lastLoadedFile.emitters.RemoveAt(i);
						}
					}
					else
                        EditorData.lastLoadedFile.emitters.RemoveAt(i);
				}
                if (EditorData.lastLoadedFile.emitters.Count == 0)
                    EditorData.lastLoadedFile = null;


				List<string> particlesAttached = new List<string>();
				List<string> spritesAttachedTo = new List<string>();

                foreach (Emitter e in EditorData.Emitters)
				{
					if(e.Parent != null)
					{
						particlesAttached.Add(e.Name);
						spritesAttachedTo.Add(e.Parent.Name);
					}
				}

				string message;

				if(particlesAttached.Count == 0)
					message = "No particles were attached to the loaded scene.\n";
				else
				{
					message = "The following particles were attached to parent Sprites: \n\n";
					for(int i = 0; i < particlesAttached.Count; i++)
					{
						message += particlesAttached[i] + " attached to " + spritesAttachedTo[i] + "\n";
					}
				}

                if (EditorData.lastLoadedFile == null)
				{
					message += "\nAll attachments made successfully!";
				}
				else
				{
					message += "\nThe following Emitters could not find the appropriate parent Sprites: \n\n";
                    foreach (EmitterSave es in EditorData.lastLoadedFile.emitters)
					{
						message += es.Name + " could not find " + es.ParentSpriteName + "\n";
					}
				}

				if(scnFileName != "")
					GuiManager.ShowMessageBox(message, FlatRedBall.IO.FileManager.MakeRelative(scnFileName) + " loaded");
				else
					GuiManager.ShowMessageBox(message, "");

			}


		}


		#region emi with attachments loaded, but no Sprites in scene messages
		
		public static void ForgetAttachmentInfo(Window callingWindow)
		{
            EditorData.lastLoadedFile = null;
			callingWindow.Parent.CloseWindow();
		}

		public static void RememberAttachmentInfo(Window callingWindow)
		{
            callingWindow.Parent.CloseWindow();
		}

		public static void AbsRelToZero(Window callingWindow)
		{
            foreach (Emitter e in EditorData.Emitters)
			{
				e.X = e.RelativeX = 0;
				e.Y = e.RelativeY = 0; 
				e.Z = e.RelativeZ = 0;
			}
            callingWindow.Parent.CloseWindow();
		}

		public static void AbsToZeroKeepRel(Window callingWindow)
		{
            foreach (Emitter e in EditorData.Emitters)
			{
				e.X = 0;
				e.Y = 0;
				e.Z = 0;
			}
            callingWindow.Parent.CloseWindow();
		}
		public static void KeepAbsRel(Window callingWindow)
		{
            callingWindow.Parent.CloseWindow();
		}
		
		public static void ManuallyLoadScn(Window callingWindow)
		{
			LoadScnxButtonClick(callingWindow);
            callingWindow.Parent.CloseWindow();
		}	
		
		public static void AutoSearchScn(Window callingWindow)
		{
            callingWindow.Parent.CloseWindow();
			List<string> allFiles =  FileManager.GetAllFilesInDirectory(System.IO.Directory.GetCurrentDirectory());
			
			for(int i = allFiles.Count - 1; i > -1; i--)
			{
				if(FlatRedBall.IO.FileManager.GetExtension(allFiles[i]) != "scnx")
					allFiles.RemoveAt(i);
			}


            //throw new NotImplementedException("Feature not implemented.  Complain on FlatRedBall forums");

            

			foreach(string scnFile in allFiles)
			{
                SpriteEditorScene ses = SpriteEditorScene.FromFile(scnFile);

                foreach(SpriteSave ss in ses.SpriteList)
				{

                    for (int i = EditorData.lastLoadedFile.emitters.Count - 1; i > -1; i--)
					{
                        if (EditorData.lastLoadedFile.emitters[i].ParentSpriteName != null)
						{
                            if (ss.Name == EditorData.lastLoadedFile.emitters[i].ParentSpriteName)
							{
                                EditorData.LoadScene(scnFile);

								return;
							}
						}

					}
				}
			}
             
		}
		#endregion

		#endregion
		
		public static void NewWorkspace(FlatRedBall.Gui.Window callingWindow)
		{
			// TODO:  Support unloading Scene;

            EditorData.CreateNewWorkspace();
		}

		#endregion

	}
}
