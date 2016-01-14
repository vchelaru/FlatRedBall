using System;
using System.Collections.Generic;

using FlatRedBall;


using FlatRedBall.Gui;


using FlatRedBall.IO;

using FlatRedBall.ManagedSpriteGroups;


using FlatRedBall.Content.Instructions;
using FlatRedBall.Instructions;


using InstructionEditor;

using System.IO;
using System.Globalization;
using FlatRedBall.Graphics.Model;
using FlatRedBall.Graphics;
using FlatRedBall.Utilities;
using FlatRedBall.Instructions.ScriptedAnimations;




namespace InstructionEditor.Gui
{
	/// <summary>
	/// Summary description for FileMenuMessages.
	/// </summary>
	public class FileMenuMessages
	{
		#region Fields

		static InstructionSetSaveList mInstructionSetSaveList;

		public static Camera camera;

		#endregion

        #region Properties

        public static InstructionSetSaveList InstructionSetSaveList
        {
            get { return mInstructionSetSaveList; }
        }

        #endregion

        #region Methods


        static void CreateActiveSpritesFromIss()
		{

            /*
			#region loop through all PositionInstructions in iss creating Sprites and adding instructions to them
			foreach(PositionInstruction i in InstructionSet.ia)
			{
                IESprite sprite = (IESprite)(EditorData.activeSprites.FindByName((string)(i.PositionedObject.Name)));
				if(sprite != null)
				{

					// TODO:  test i to see which type of instruction it is.  If it's a position instruction, add it as below, otherwise
					// add it to the appropriate array.  Even better, write an AddKeyInstruction method which adds the instruction to the
					// appropriate array.
					sprite.positionInstructions.Add(i);
                    i.PositionedObject = sprite;
				}
				else
				{
					Sprite spriteToAdd = (Sprite)(GuiData.ListBoxWindow.BlueprintSpriteListBox.GetObject((string)(i.blueprintName)));


					IESprite tempSprite = new IESprite();

					tempSprite.SetFromRegSprite(spriteToAdd);

					tempSprite.Name = (string)(i.ReferencedObjectName);

					EditorData.AddSprite(tempSprite);

					// TODO:  test i to see which type of instruction it is.  If it's a position instruction, add it as below, otherwise
					// add it to the appropriate array.  Even better, write an AddKeyInstruction method which adds the instruction to the
					// appropriate array.
					tempSprite.positionInstructions.Add(i);
					i.PositionedObject = tempSprite;
				}
			}

			#endregion

			foreach(InstructionList fia in InstructionSet.formations)
			{
				GuiData.ListBoxWindow.InstructionSetListBox.AddItem( fia.Name, fia);
			}


			for(int i = 0; i < InstructionSet.spriteNames.Count; i++)
			{
				((IESprite)EditorData.activeSprites.FindByName(InstructionSet.spriteNames[i])).groupNumber = 
					InstructionSet.spriteGroups[i];

				EditorData.groupNumber = Math.Max( EditorData.groupNumber, InstructionSet.spriteGroups[i] + 1);
			}

			InstructionSet = null;

			EditorData.SelectSprite(null);
            */
		}


		public static void NewSet(Window callingWindow)
		{
            mInstructionSetSaveList = null;

			// clear the blueprint Sprites
            foreach (Sprite s in EditorData.ActiveSprites)
				SpriteManager.RemoveSprite(s);

            Sprite cameraSprite = null;

            if (EditorData.ActiveSprites.Count != 0)
            {
                cameraSprite = EditorData.ActiveSprites[0];
                EditorData.ActiveSprites.Clear();
                EditorData.ActiveSprites.AddOneWay(cameraSprite);
            }

            EditorData.EditorLogic.SelectObject((Sprite)null, EditorData.EditorLogic.CurrentSprites);

			GuiData.ListBoxWindow.InstructionSetListBox.Clear();
		}


		#region Load

        static void CancelLoadingScene(Window callingWindow)
        {
            Window parentWindow = callingWindow.Parent;
            GuiManager.RemoveWindow(callingWindow);
        }

		public static void LoadActiveSceneClick(Window callingWindow)
		{
			FileWindow tempFileWindow = GuiManager.AddFileWindow();
			tempFileWindow.SetFileType("scnx");
			tempFileWindow.OkClick += new GuiMessage(LoadActiveSceneFileOk);

		}
	

		public static void LoadActiveSceneFileOk(Window callingWindow)
		{
            MultiButtonMessageBox mbmb = GuiManager.AddMultiButtonMessageBox();
            mbmb.Name = ((FileWindow)callingWindow).Results[0];
            mbmb.Text = "Would you like to load " + ((FileWindow)callingWindow).Results[0] +
                " as a Blueprint or Blocking Scene?";

            mbmb.AddButton("Blueprint Scene", new GuiMessage(LoadActiveBlueprintScene));
            mbmb.AddButton("Blocking Scene", new GuiMessage(LoadActiveBlockingScene));
            mbmb.AddButton("Cancel", CancelLoadingScene);

            mbmb.ScaleX = 9;

        }

        public static void LoadActiveBlueprintScene(Window callingWindow)
        {
            /*
			// store the loaded blueprint Sprites here
            // All Sprites will be loaded through the SpriteArray's Load method.
            // This method is preferred because the SpriteManager methods for loading
            // add the Sprites to the scene, and they shouldn't be added yet.
			SpriteList tempArray = new SpriteList();


            SpriteManager.LoadScn(callingWindow.parentWindow.Name, tempArray, null,
                FlatRedBallServices.Random);

			foreach(Sprite sprite in tempArray)
			{
				GuiData.ListBoxWindow.BlueprintSpriteListBox.AddItem(sprite.Name, sprite);	
//				GuiData.propertyWindow.blueprintComboBox.AddItem(sprite.Name, sprite);
			}
			
			if(InstructionSetSave != null)
			{
				CreateActiveSpritesFromIss();
			}

			TimeLineMessages.ToStartPushed(null);
             */
             
        }

        #region XML Docs
        /// <summary>
        /// Loads a blocking Scene using the argument FileWindow's result argument.
        /// This is used as a file window ok button event.
        /// </summary>
        /// <param name="callingWindow">Reference to the FileWindow.</param>
        #endregion
        public static void LoadActiveBlockingScene(Window callingWindow)
        {
            LoadActiveBlockingSceneFromFile(
                callingWindow.Parent.Name);
        }


        static bool LoadActiveBlockingSceneFromFile(string fileName)
        {
            // It's likely the scene is relative to the InstructionSet.  Therefore, load using this relative scene


            if(string.IsNullOrEmpty(LastLoadInformation.LastInstructionSetLoaded) == false)
            {
                FileManager.RelativeDirectory =
                    FileManager.GetDirectory(LastLoadInformation.LastInstructionSetLoaded);
            }

            if (FileManager.FileExists(fileName))
            {

                FlatRedBall.Content.SpriteEditorScene ses = FlatRedBall.Content.SpriteEditorScene.FromFile(
                    fileName);

                ses.AllowLoadingModelsFromFile = true;


                EditorData.BlockingScene = ses.ToScene(EditorData.ContentManagerName);

                return true;
            }
            else
            {
                return false;
            }


        }


		public static void LoadInactiveSceneClick(Window callingWindow)
		{
			FileWindow tempFileWindow = GuiManager.AddFileWindow();
			tempFileWindow.SetFileType("scnx");
			tempFileWindow.OkClick += new GuiMessage(LoadInactiveSceneFileOk);
		}

		public static void LoadInactiveSceneFileOk(Window callingWindow)
		{
            FlatRedBall.Content.SpriteEditorScene ses = FlatRedBall.Content.SpriteEditorScene.FromFile(
                ((FileWindow)callingWindow).Results[0]);

            EditorData.InactiveScene = ses.ToScene(EditorData.ContentManagerName);
            SpriteManager.AddScene(EditorData.InactiveScene);
		}

		public static void LoadSetClick(Window callingWindow)
		{
			FileWindow fileWindow = GuiManager.AddFileWindow();
			fileWindow.SetFileType("istx");
			fileWindow.OkClick += new GuiMessage(LoadSetFileWindowOk);
		}

		public static void LoadSetFileWindowOk(Window callingWindow)
        {
            string fileName = ((FileWindow)callingWindow).Results[0];

            #region Load the file to the mInstructionSetSaveList field - it may be stored off until the .scnx is found
            LastLoadInformation.LastInstructionSetLoaded = fileName;
            mInstructionSetSaveList = InstructionSetSaveList.FromFile(LastLoadInformation.LastInstructionSetLoaded);
            #endregion

            #region Load the properties file (.iepsx) if it exists

            string iepsxFileName = FileManager.RemoveExtension(fileName) + ".iepsx";

            if (FileManager.FileExists(iepsxFileName))
            {
                InstructionEditorPropertiesSave iepsx = InstructionEditorPropertiesSave.FromFile(iepsxFileName);
                iepsx.ApplyToEditor();

            }


            #endregion

            // See if there is already a Blocking scene loaded
            if (EditorData.BlockingScene == null || string.IsNullOrEmpty(EditorData.BlockingScene.Name))
            {
                OkCancelWindow okCancelWindow =
                    GuiManager.ShowOkCancelWindow("There is no blocking scene currently loaded.  Attempt to load the scene " +
                    "referenced by the loaded Instruction Set?", "No Scene Loaded");
                okCancelWindow.OkClick += LoadBlockingFromLoadedInstructionSet;

            }
            else
            {
                AddCurrentlyLoadedInstructionSet();
            }

            /*

			#region blueprint Sprites already loaded
			if(GuiData.ListBoxWindow.BlueprintSpriteListBox.Count != 0)
			{
				CreateActiveSpritesFromIss();
				TimeLineMessages.ToStartPushed(null);

			}
			#endregion
		
				#region blueprint Sprites not already loaded
			else
			{
				MultiButtonMessageBox mbmb = GuiManager.AddMultiButtonMessageBox();
				mbmb.Name = "No Blueprints Loaded";
				mbmb.Text = ((FileWindow)callingWindow).result[0] + " instruction set requires active Sprites to be loaded.  What would you like to do?";

				mbmb.AddButton("Manually search for .scnx file.", new GuiMessage(LoadActiveSceneClick));

//				mbmb.AddButton("Automatically search for .scn with Sprites matching instructions.", new GuiMessage(AutoSearchScn));
			}
			#endregion
             */
        }

        static void AddCurrentlyLoadedInstructionSet()
        {
            #region Add the per-object InstructionSets

            // When the InstructionSet is created from the InstructionSetSave, a scene must be passed
            // so that the instructions can be hooked up correctly.
            List<InstructionSet> instructionSetList = InstructionSetSaveList.ToInstructionSetList(EditorData.BlockingScene);


            for (int i = 0; i < InstructionSetSaveList.InstructionSetSaves.Count; i++)
            {
                string objectName = InstructionSetSaveList.InstructionSetSaves[i].Target;

                INameable target = EditorData.BlockingScene.FindByName(objectName);

                if (target == null)
                {
                    //EditorData.GlobalInstructionSets.Add(instructionSetList[i]);
                }
                else
                {
                    // The dictionary is already populated with the instruction sets
                    EditorData.ObjectInstructionSets[target] =  instructionSetList[i];
                }

            }

            #endregion

            #region Add the AnimationSequences

            EditorData.GlobalInstructionSets = InstructionSetSaveList.ToAnimationSequenceList(instructionSetList);

            #endregion


        }

        static void LoadBlockingFromLoadedInstructionSet(Window callingWindow)
        {

            bool loadSuccessful = 
                LoadActiveBlockingSceneFromFile(mInstructionSetSaveList.SceneFileName);

            if (loadSuccessful)
            {
                AddCurrentlyLoadedInstructionSet();
                GuiManager.ShowMessageBox("Loading referenced .scnx successful.", "Load Successful");
            }
            else
            {
                GuiManager.ShowMessageBox("Could not load referenced .scnx file.", "Load Failed");
            }

        }


		#endregion


		#region Save


		public static void SaveSetClick(Window callingWindow)
		{
            GuiData.UsedPropertySelectionWindow.Visible = true;
            GuiData.UsedPropertySelectionWindow.OkButton.Click += SaveSetPropertySelectionOk;
        }


        static void SaveSetPropertySelectionOk(Window callingWindow)
        {
			FileWindow fileWindow = GuiManager.AddFileWindow();
			fileWindow.SetToSave();
			fileWindow.SetFileType("istx");
			fileWindow.OkClick += new GuiMessage(SaveSetFileWindowOk);
		}

		
		public static void SaveSetFileWindowOk(Window callingWindow)
        {
            string fileName = ((FileWindow)callingWindow).Results[0];

            #region Save the istx
            InstructionSetSaveList instructionSetSaveList = new FlatRedBall.Content.Instructions.InstructionSetSaveList();

            if (EditorData.BlockingScene == null)
            {
                GuiManager.ShowMessageBox("There is no scene loaded.  A scene must be loaded for the .istx to reference.", "Error saving");
            }
            else
            {
                foreach (AnimationSequence animationSequence in EditorData.GlobalInstructionSets)
                {
                    AnimationSequenceSave sequenceSave = AnimationSequenceSave.FromAnimationSequence(animationSequence);

                    instructionSetSaveList.AnimationSequenceSaves.Add(sequenceSave);
                    //AddSetToSave(instructionSet, instructionSetSaveList, "");
                    // TODO:
                }
                foreach (KeyValuePair<INameable, InstructionSet> kvp in EditorData.ObjectInstructionSets)
                {
                    AddSetToSave(kvp.Value, instructionSetSaveList, kvp.Key.Name);
                }

                instructionSetSaveList.SceneFileName = EditorData.BlockingScene.Name;
                instructionSetSaveList.Save(fileName);
            }

            #endregion

            #region Save the iepsx file

            fileName = FileManager.RemoveExtension(fileName) + ".iepsx";

            InstructionEditorPropertiesSave ieps = InstructionEditorPropertiesSave.FromEditorData();
            ieps.Save(fileName);

            #endregion

        }


        private static void AddSetToSave(InstructionSet instructionSet, InstructionSetSaveList instructionSetSaveList, 
            string targetName)
        {
            // This following members be used as a buffer for holding the lists that will be saved.
            // In the following loop the code will only copy over instructions that set properties
            // which are included in EditorData.SavedMembers
            List<InstructionList> temporaryListList = new List<InstructionList>();
            InstructionList temporaryList = new InstructionList();

            InstructionSetSave instructionSetSave = new InstructionSetSave();
            
            foreach (KeyframeList keyframeList in instructionSet)
            {
                temporaryListList = new List<InstructionList>();

                foreach (InstructionList instructionList in keyframeList)
                {
                    temporaryList = new InstructionList();
                    temporaryList.Name = instructionList.Name;

                    foreach (Instruction instruction in instructionList)
                    {
                        // Assume that all instructions are GenericInstructions
                        GenericInstruction asGenericInstruction = instruction as GenericInstruction;

                        bool toAdd = false;

                        if (asGenericInstruction.Target is PositionedModel)
                        {
                            toAdd = EditorData.CurrentPositionedModelMembersWatching.Contains(asGenericInstruction.Member);
                        }
                        else if (asGenericInstruction.Target is Sprite)
                        {
                            toAdd = EditorData.CurrentSpriteMembersWatching.Contains(asGenericInstruction.Member);
                        }
                        else if (asGenericInstruction.Target is SpriteFrame)
                        {
                            toAdd = EditorData.CurrentSpriteFrameMembersWatching.Contains(asGenericInstruction.Member);
                        }
                        else if (asGenericInstruction.Target is Text)
                        {
                            toAdd = EditorData.CurrentTextMembersWatching.Contains(asGenericInstruction.Member);
                        }

                        if (toAdd)
                        {
                            // this instruction is one we want to save
                            temporaryList.Add(instruction);
                        }
                    }

                    if (temporaryList.Count != 0)
                    {
                        temporaryListList.Add(temporaryList);
                    }
                }

                if (temporaryListList.Count != 0)
                {
                    instructionSetSave.AddInstructions(temporaryListList, keyframeList.Name);
                }
            }
            if (instructionSetSave.Instructions.Count != 0)
            {
                instructionSetSave.Target = targetName;
                instructionSetSaveList.InstructionSetSaves.Add(instructionSetSave);
            }
        }

		#endregion

		#endregion

	}
}
