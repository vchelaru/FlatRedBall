using System;
using System.Collections.Generic;
using System.Text;
using InstructionEditor.Gui;
using FlatRedBall;
using InstructionEditor.Collections;
using EditorObjects;
using FlatRedBall.Instructions;
using FlatRedBall.Input;
using FlatRedBall.Gui;
using Microsoft.Xna.Framework.Graphics;
using FlatRedBall.ManagedSpriteGroups;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using FlatRedBall.Graphics.Model;
using ToolTemplate;
using FlatRedBall.Graphics;
using FlatRedBall.Content;
using FlatRedBall.IO;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Utilities;
using FlatRedBall.Instructions.ScriptedAnimations;


namespace InstructionEditor
{
    #region XML Docs
    /// <summary>
    /// Stores all data that the tool will edit.
    /// </summary>
    /// <remarks>
    /// Examples include Scenes, Polygon Lists, PositionedModel Lists, or any other
    /// custom data that you may edit.
    /// </remarks>
    #endregion
    public static class EditorData
    {
        #region Fields

        private static EditorLogic mEditorLogic = new EditorLogic();

        public const string ContentManagerName = "Tool ContentManager";

        static Scene mBlockingScene;
        static Scene mInactiveScene;

        static List<string> mAllSpriteMembersWatching = new List<string>();
        static List<string> mAllSpriteFrameMembersWatching = new List<string>();
        static List<string> mAllPositionedModelMembersWatching = new List<string>();
        static List<string> mAllTextMembersWatching = new List<string>();

        static List<string> mCurrentSpriteMembersWatching = new List<string>();
        static List<string> mCurrentSpriteFrameMembersWatching = new List<string>();
        static List<string> mCurrentPositionedModelMembersWatching = new List<string>();
        static List<string> mCurrentTextMembersWatching = new List<string>();

        static List<string> mSavedMembers = new List<string>();

        static Camera mSceneCamera = new Camera(ContentManagerName);
        static EditorObjects.CameraBounds mCameraBounds;

        public static SpriteList ActiveSprites; // one way array, as the Sprites will be removed from the SpriteManager often when rewinding
        public static FormationArray formationArray;


        public static Sprite currentKeyframeSprite;
        //public static Instruction currentKeyframe;

        public static InstructionPlayer instructionPlayer;

        public static int groupNumber = 0;

        static RectangleSelector rectangleSelector;

        static List<AnimationSequence> mGlobalInstructionSets;
        static Dictionary<INameable, InstructionSet> mObjectInstructionSets = new Dictionary<INameable, InstructionSet>();

        static EditorOptions mEditorOptions = new EditorOptions();


        #endregion

        #region Properties

        public static List<string> AllPositionedModelMembersWatching
        {
            get { return mAllPositionedModelMembersWatching; }
        }

        public static List<string> AllSpriteFrameMembersWatching
        {
            get { return mAllSpriteFrameMembersWatching; }

        }        
        
        public static List<string> AllSpriteMembersWatching
        {
            get { return mAllSpriteMembersWatching; }
        }

        public static List<string> AllTextMembersWatching
        {
            get { return mAllTextMembersWatching; }
        }

        public static Scene BlockingScene
        {
            get { return mBlockingScene; }
            set 
            {
                // Clear out the old blocking scene
                mBlockingScene.RemoveFromManagers();
                mBlockingScene.Clear();

                mBlockingScene = value;
                mBlockingScene.AddToManagers();
            }
        }

        public static List<string> CurrentPositionedModelMembersWatching
        {
            get { return mCurrentPositionedModelMembersWatching; }
        }

        public static List<string> CurrentSpriteFrameMembersWatching
        {
            get { return mCurrentSpriteFrameMembersWatching; }

        }

        public static List<string> CurrentSpriteMembersWatching
        {
            get { return mCurrentSpriteMembersWatching; }
        }

        public static List<string> CurrentTextMembersWatching
        {
            get { return mCurrentTextMembersWatching; }
        }

        public static EditorLogic EditorLogic
        {
            get { return mEditorLogic; }
        }

        public static EditorOptions EditorOptions
        {
            get { return mEditorOptions; }
        }

        public static Scene InactiveScene
        {
            get { return mInactiveScene; }
            set { mInactiveScene = value; }
        }

        public static List<AnimationSequence> GlobalInstructionSets
        {
            get { return mGlobalInstructionSets; }
            set { mGlobalInstructionSets = value; }
        }

        public static Dictionary<INameable, InstructionSet> ObjectInstructionSets
        {
            get { return mObjectInstructionSets; }
        }

        public static Camera SceneCamera
        {
            get { return mSceneCamera; }
        }

        #endregion

        #region Methods

        #region Initialization Methods

        static EditorData()
        {
            // Force the UI to be created

            mEditorLogic = new EditorLogic();

            mGlobalInstructionSets = new List<AnimationSequence>();

            SpriteManager.Camera.Z = 40.0f;
            mCameraBounds = new CameraBounds(mSceneCamera);

            mBlockingScene = new Scene();

            #region create the spriteArrays, InstructionGroupArray, and SpriteGridArray
            ActiveSprites = new SpriteList();
            formationArray = new FormationArray();
            #endregion

            // Add the properties that are going to be watched for InstructionSets
            CreateMembersWatching();

            instructionPlayer = new InstructionPlayer();

            //= SpriteManager.AddSprite("genGfx/targetBox.bmp", sprMan.AddLayer());
            //			currentSpriteMarker.fade = 150;
            //			currentSpriteMarker.visible = false;


            rectangleSelector = new RectangleSelector();

        }

        private static void CreateMembersWatching()
        {
            #region Sprite members watching

            mAllSpriteMembersWatching.Add("X");
            mAllSpriteMembersWatching.Add("Y");
            mAllSpriteMembersWatching.Add("Z");

            mAllSpriteMembersWatching.Add("RelativeX");
            mAllSpriteMembersWatching.Add("RelativeY");
            mAllSpriteMembersWatching.Add("RelativeZ");

            mAllSpriteMembersWatching.Add("ScaleX");
            mAllSpriteMembersWatching.Add("ScaleY");

            mAllSpriteMembersWatching.Add("RotationX");
            mAllSpriteMembersWatching.Add("RotationY");
            mAllSpriteMembersWatching.Add("RotationZ");

            mAllSpriteMembersWatching.Add("RelativeRotationX");
            mAllSpriteMembersWatching.Add("RelativeRotationY");
            mAllSpriteMembersWatching.Add("RelativeRotationZ");

            mAllSpriteMembersWatching.Add("ColorOperation");

            mAllSpriteMembersWatching.Add("Alpha");
            mAllSpriteMembersWatching.Add("Red");
            mAllSpriteMembersWatching.Add("Green");
            mAllSpriteMembersWatching.Add("Blue");

            mAllSpriteMembersWatching.Add("Visible");

            mAllSpriteMembersWatching.Add("CurrentChainName");
            mAllSpriteMembersWatching.Add("Animate");
            mAllSpriteMembersWatching.Add("AnimationSpeed");

            #endregion

            #region SpriteFrame members watching


            mAllSpriteFrameMembersWatching.Add("X");
            mAllSpriteFrameMembersWatching.Add("Y");
            mAllSpriteFrameMembersWatching.Add("Z");

            mAllSpriteFrameMembersWatching.Add("RelativeX");
            mAllSpriteFrameMembersWatching.Add("RelativeY");
            mAllSpriteFrameMembersWatching.Add("RelativeZ");

            mAllSpriteFrameMembersWatching.Add("ScaleX");
            mAllSpriteFrameMembersWatching.Add("ScaleY");

            mAllSpriteFrameMembersWatching.Add("RotationX");
            mAllSpriteFrameMembersWatching.Add("RotationY");
            mAllSpriteFrameMembersWatching.Add("RotationZ");

            mAllSpriteFrameMembersWatching.Add("RelativeRotationX");
            mAllSpriteFrameMembersWatching.Add("RelativeRotationY");
            mAllSpriteFrameMembersWatching.Add("RelativeRotationZ");
            
            mAllSpriteFrameMembersWatching.Add("ColorOperation");

            mAllSpriteFrameMembersWatching.Add("Alpha");
            mAllSpriteFrameMembersWatching.Add("Red");
            mAllSpriteFrameMembersWatching.Add("Green");
            mAllSpriteFrameMembersWatching.Add("Blue");

            mAllSpriteFrameMembersWatching.Add("Visible");
            #endregion

            #region PositionedModel members watching
            mAllPositionedModelMembersWatching.Add("X");
            mAllPositionedModelMembersWatching.Add("Y");
            mAllPositionedModelMembersWatching.Add("Z");

            mAllPositionedModelMembersWatching.Add("RelativeX");
            mAllPositionedModelMembersWatching.Add("RelativeY");
            mAllPositionedModelMembersWatching.Add("RelativeZ");

            mAllPositionedModelMembersWatching.Add("ScaleX");
            mAllPositionedModelMembersWatching.Add("ScaleY");
            mAllPositionedModelMembersWatching.Add("ScaleZ");

            mAllPositionedModelMembersWatching.Add("RotationX");
            mAllPositionedModelMembersWatching.Add("RotationY");
            mAllPositionedModelMembersWatching.Add("RotationZ");

            mAllPositionedModelMembersWatching.Add("RelativeRotationX");
            mAllPositionedModelMembersWatching.Add("RelativeRotationY");
            mAllPositionedModelMembersWatching.Add("RelativeRotationZ");

            mAllPositionedModelMembersWatching.Add("Visible");

            mAllPositionedModelMembersWatching.Add("Animate");
            mAllPositionedModelMembersWatching.Add("CurrentAnimation");

            #endregion

            #region Text members watching
            mAllTextMembersWatching.Add("X");
            mAllTextMembersWatching.Add("Y");
            mAllTextMembersWatching.Add("Z");

            mAllTextMembersWatching.Add("RelativeX");
            mAllTextMembersWatching.Add("RelativeY");
            mAllTextMembersWatching.Add("RelativeZ");
            
            mAllTextMembersWatching.Add("RotationX");
            mAllTextMembersWatching.Add("RotationY");
            mAllTextMembersWatching.Add("RotationZ");

            mAllTextMembersWatching.Add("RelativeRotationX");
            mAllTextMembersWatching.Add("RelativeRotationY");
            mAllTextMembersWatching.Add("RelativeRotationZ");

            mAllTextMembersWatching.Add("ColorOperation");

            mAllTextMembersWatching.Add("Alpha");
            mAllTextMembersWatching.Add("Red");
            mAllTextMembersWatching.Add("Green");
            mAllTextMembersWatching.Add("Blue");

            mAllTextMembersWatching.Add("Visible");
            mAllTextMembersWatching.Add("DisplayText");

            #endregion
        }

        #endregion

        #region Public Methods

        public static void Update()
        {
            mEditorLogic.Update();

            MouseShortcuts();

            instructionPlayer.Activity();

            UpdateUI();

            UpdateObjectInstructionSets();// this might get expensive.  Profile if problems occur

            ManageSpriteGrids();
        }


        public static void AdjustMovementPath()
        {
            // this is called either when Key.I is released or when releasing the mouse button when I is down
            //if(oldSpritePosition != null)
            //{
            //    float differenceX = mEditorLogic.CurrentSprites[0].X - oldSpritePosition.X;
            //    float differenceY = mEditorLogic.CurrentSprites[0].Y - oldSpritePosition.Y;
            //    float differenceZ = mEditorLogic.CurrentSprites[0].Z - oldSpritePosition.Z;

            //    foreach (Sprite s in mEditorLogic.CurrentSprites)
            //    {
            //        foreach(PositionInstruction i in ((IESprite)s).positionInstructions)
            //        {
            //            i.X += differenceX;
            //            i.Y += differenceY;
            //            i.Z += differenceZ;
            //        }
            //    }
            //    oldSpritePosition = null;


            //}

        }

        static List<string> mEmptyList = new List<string>();
        public static void AddInstructionsToList(InstructionList instructionList, double timeToExecute)
        {
            AddInstructionsToList(instructionList, timeToExecute, mEmptyList);
        }

        public static void AddInstructionsToList(InstructionList instructionList, double timeToExecute, List<string> membersToIgnore)
        {
            if (GuiData.TimeLineWindow.InstructionMode == InstructionMode.All)
            {
                #region Loop through all Sprites and create instructions for them

                foreach (Sprite sprite in BlockingScene.Sprites)
                {
                    InstructionRecorder.RecordInstructions(instructionList, timeToExecute, membersToIgnore, sprite);
                }

                #endregion

                #region Loop through all SpriteFrames and create instructions for them
                foreach (SpriteFrame spriteFrame in BlockingScene.SpriteFrames)
                {
                    InstructionRecorder.RecordInstructions(instructionList, timeToExecute, membersToIgnore, spriteFrame);
                }
                #endregion

                #region Loop through all PositionedModels and create Instructions for them

                foreach (PositionedModel positionedModel in mBlockingScene.PositionedModels)
                {
                    InstructionRecorder.RecordInstructions(instructionList, timeToExecute, membersToIgnore, positionedModel);
                }

                #endregion

                #region Loop through all Texts and create Instructions for them

                foreach (Text text in BlockingScene.Texts)
                {
                    InstructionRecorder.RecordInstructions(instructionList, timeToExecute, membersToIgnore, text);
                }

                #endregion
            }
            else if (GuiData.TimeLineWindow.InstructionMode == InstructionMode.Current)
            {
                if (EditorData.EditorLogic.CurrentSprites.Count != 0)
                {
                    InstructionRecorder.RecordInstructions(instructionList, timeToExecute, membersToIgnore, 
                        EditorData.EditorLogic.CurrentSprites[0]);
                }
                else if (EditorData.EditorLogic.CurrentSpriteFrames.Count != 0)
                {
                    InstructionRecorder.RecordInstructions(instructionList, timeToExecute, membersToIgnore, 
                        EditorData.EditorLogic.CurrentSpriteFrames[0]);
                }
                else if (EditorData.EditorLogic.CurrentPositionedModels.Count != 0)
                {
                    InstructionRecorder.RecordInstructions(instructionList, timeToExecute, membersToIgnore,
                        EditorData.EditorLogic.CurrentPositionedModels[0]);
                }
                else if (EditorData.EditorLogic.CurrentTexts.Count != 0)
                {
                    InstructionRecorder.RecordInstructions(instructionList, timeToExecute, membersToIgnore,
                        EditorData.EditorLogic.CurrentTexts[0]);

                }

            }

        }


        public static void AddSprite(string textureOrAnimationFile)
        {
            Sprite newSprite = null;

            if (FileManager.GetExtension(textureOrAnimationFile) == "achx")
            {
                AnimationChainListSave achs = AnimationChainListSave.FromFile(textureOrAnimationFile);

                newSprite = SpriteManager.AddSprite(
                    achs.ToAnimationChainList(ContentManagerName));
                mBlockingScene.Sprites.Add(newSprite);

                newSprite.Name = FileManager.RemovePath(FileManager.RemoveExtension(textureOrAnimationFile));
            }
            else
            {
                newSprite = SpriteManager.AddSprite(textureOrAnimationFile, ContentManagerName);
                mBlockingScene.Sprites.Add(newSprite);

                newSprite.Name = FileManager.RemovePath(FileManager.RemoveExtension(textureOrAnimationFile));

            }

            SetNewlyCreatedSpriteProperties(newSprite);

        }


        public static void AddText()
        {
            Text text = TextManager.AddText("Text");
            mBlockingScene.Texts.Add(text);
            text.Name = "Text";
            StringFunctions.MakeNameUnique<Text>(text, mBlockingScene.Texts);
        }

        #region XML Docs
        /// <summary>
        /// Exports the code that can be executed instead of the global instruction set
        /// </summary>
        /// <returns>The string containing the source code.</returns>
        #endregion
        public static string GetStringForInstructions()
        {
            StringBuilder stringBuilder = new StringBuilder();

            InstructionList temporaryInstructionList = new InstructionList();


            #region Create an array of doubles for times

            //stringBuilder.AppendLine("static double[][] KeyframeTimes = new double[][]{");

            //foreach (InstructionSet instructionSet in EditorData.GlobalInstructionSets)
            //{
            //    for (int keyframeListIndex = 0; keyframeListIndex < instructionSet.Count; keyframeListIndex++)
            //    {
            //        KeyframeList keyframeList = mGlobalInstructionSet[keyframeListIndex];

            //        stringBuilder.AppendLine("\tnew double[]{");
            //        for (int i = 0; i < keyframeList.Count; i++)
            //        {
            //            if (i == keyframeList.Count - 1)
            //            {
            //                stringBuilder.AppendLine("\t\t" + keyframeList[i][0].TimeToExecute);
            //            }
            //            else
            //            {
            //                stringBuilder.AppendLine("\t\t" + keyframeList[i][0].TimeToExecute + ",");
            //            }
            //        }

            //        if (keyframeListIndex == mGlobalInstructionSet.Count - 1)
            //        {
            //            stringBuilder.AppendLine("\t}");

            //        }
            //        else
            //        {
            //            stringBuilder.AppendLine("\t},");

            //        }
            //    }
            //}
            #endregion
            stringBuilder.AppendLine("};");

            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();

            stringBuilder.AppendLine("public void SetCurrentFrame()");
            stringBuilder.AppendLine("{");
            
            stringBuilder.AppendLine("\tswitch( mCurrentAnimation )");
            stringBuilder.AppendLine("\t{");

            //for(int keyframeIndex = 0; keyframeIndex < mGlobalInstructionSet.Count; keyframeIndex++)
            //{
            //    KeyframeList keyframeList = mGlobalInstructionSet[keyframeIndex];

            //    stringBuilder.AppendLine("\t\tcase " + keyframeIndex + ":");

            //    stringBuilder.AppendLine("\t\tswitch( mCurrentKeyframe )");
            //    stringBuilder.AppendLine("\t\t{");


            //    for(int i= 0; i < keyframeList.Count; i++)
            //    {

            //        temporaryInstructionList.Clear();

            //        temporaryInstructionList.AddRange( keyframeList[i] );

            //        temporaryInstructionList.AddRange( mGlobalInstructionSet[keyframeIndex].CreateVelocityListAtIndex(i) );




            //        stringBuilder.AppendLine("\t\t\tcase " + i + ":");

            //        foreach (GenericInstruction instruction in temporaryInstructionList)
            //        {
            //            #region Get the name of the object and stuff it in objectName
            //            string objectName = "object";

            //            if(instruction.Target is Sprite)
            //            {
            //                int index = mBlockingScene.Sprites.IndexOf(instruction.Target as Sprite);
            //                objectName = "mSprites[" + index + "].";
            //            }


            //            if (instruction.Target is SpriteFrame)
            //            {
            //                int index = mBlockingScene.SpriteFrames.IndexOf(instruction.Target as SpriteFrame);
            //                objectName = "mSpriteFrames[" + index + "].";
            //            }


            //            if (instruction.Target is Text)
            //            {
            //                int index = mBlockingScene.Texts.IndexOf(instruction.Target as Text);
            //                objectName = "mTexts[" + index + "].";
            //            }


            //            if (instruction.Target is PositionedModel)
            //            {
            //                int index = mBlockingScene.PositionedModels.IndexOf(instruction.Target as PositionedModel);
            //                objectName = "mPositionedModels[" + index + "].";
            //            }
            //            #endregion

            //            string memberValueAsString = instruction.MemberValueAsString;

            //            if (instruction.MemberValueAsObject is float)
            //            {
            //                memberValueAsString += "f";
            //            }
            //            else if (instruction.MemberValueAsObject != null && instruction.MemberValueAsObject.GetType().IsEnum)
            //            {
            //                memberValueAsString =
            //                    instruction.MemberValueAsObject.GetType().FullName + "." + memberValueAsString;
            //            }

            //            stringBuilder.AppendLine("\t\t\t\t" + objectName + instruction.Member + " = " +
            //                memberValueAsString + ";");

            //        }

            //        stringBuilder.AppendLine("\t\t\t\tbreak;");
            //    }

            //    stringBuilder.AppendLine("\t\t}");
            //    stringBuilder.AppendLine("\t\tbreak;");

            //}
            stringBuilder.AppendLine("\t}");

            stringBuilder.AppendLine("}");


            return stringBuilder.ToString();
        }


        public static bool MouseControlOverKeyframes()
        {
            if (mEditorLogic.CurrentSprites.Count == 0) return false;

            //if(GuiManager.CursorObjectGrabbed != null && 
            //    GuiManager.CursorObjectGrabbed is Sprite && ((Sprite)(GuiManager.CursorObjectGrabbed)).type == "positionKeyframe")
            //    GuiData.propertyWindow.UpdateKeyframeValues();

            #region find which keyframe the cursor is over giving the currentKeyframe more importance
            Sprite tempSpriteOver = null;
            if (currentKeyframeSprite != null && GuiManager.Cursor.IsOn(currentKeyframeSprite))
            {
                // not sure what this does, will probably get replaced
                //                tempSpriteOver = ((IESprite)mEditorLogic.CurrentSprites[0]).movementPath.positionInstructions[((IESprite)mEditorLogic.CurrentSprites[0]).positionInstructions.IndexOf(currentKeyframe)];

            }
            else
            {
                //foreach (Sprite s in ((IESprite)mEditorLogic.CurrentSprites[0]).movementPath.positionInstructions)
                //{
                //    if (GuiManager.Cursor.IsOn(s))
                //    {
                //        tempSpriteOver = s;
                //        break;
                //    }
                //}
            }
            #endregion


            #region primaryPush - grabbing and setting a currentKeyframe
            if (GuiManager.Cursor.PrimaryPush || GuiManager.Cursor.SecondaryPush)
            {
                GuiManager.Cursor.SetObjectRelativePosition(currentKeyframeSprite);
                GuiManager.Cursor.ObjectGrabbed = currentKeyframeSprite;
            }

            return (tempSpriteOver != null);

            #endregion


        }


        public static void MouseShortcuts()
        {
            Cursor cursor = GuiManager.Cursor;

            if (cursor.WindowOver != null || GuiManager.DominantWindowActive)
            {
                return;
            }

            EditorObjects.CameraMethods.MouseCameraControl(SpriteManager.Camera);


            /*
        else if(mouseButtons[1] != 0x00 && mouseButtons[2] != 0x00)
        {
            GuiManager.CursorStaticPosition = true;

            GuiData.timeLineWindow.timeLine.CurrentValue += GuiManager.CursorXVelocity * GuiData.timeLineWindow.timeLine.valueWidth / GuiData.timeLineWindow.timeLine.ScaleX;
            //		GuiData.timeLineWindow.currentTimeTextBox.Text = GuiData.timeLineWindow.timeLine.currentValue.ToString();
            GuiData.timeLineWindow.timeLine.CallOnGUIChange();
        }
        */


            /* // no idea what this is.
             * 
            if ((mouseButtons[2] != 0 && mouse.ButtonReleased(FlatRedBall.Input.Mouse.MouseButtons.RightButton)) ||
                (mouseButtons[1] != 0 && mouse.ButtonReleased(FlatRedBall.Input.Mouse.MouseButtons.MiddleButton)) ||
				(inpMan.MouseClick(1) && inpMan.MouseClick(2) ))
			{
				GuiManager.CursorstaticPosition = false;
			}
			*/
        }


        public static void SaveActiveScene(string fileName)
        {
            SpriteEditorScene sceneSave = SpriteEditorScene.FromScene(mBlockingScene);

            sceneSave.Save(fileName);

            mBlockingScene.Name = fileName;
        }


        public static void SetTime(double timeToSet)
        {
            if (mEditorLogic.CurrentKeyframeList != null &&
                InputManager.Keyboard.KeyDown(Keys.H) == false // H holds the layout so the user can change time without moving things.
                )
            {
                mEditorLogic.CurrentKeyframeList.SetState(timeToSet, false);
            }

            //if(currentSprites.Count != 0)
            //	GuiData.propertyWindow.UpdateSpriteValues();
        }


        public static void SpriteGrabbedLogic()
        {
            // this method will move the sprite grabbed.  This includes both keyrames and active sprites.

            if (GuiManager.Cursor.ObjectGrabbed == null) return;

            Sprite sprite = GuiManager.Cursor.ObjectGrabbed as Sprite;

            #region move button is pressed

            if (GuiData.ToolsWindow.MoveButton.IsPressed)
            {
                if (GuiManager.Cursor.PrimaryDown)
                {
                    Vector3 positionBeforeMouseMove = sprite.Position;

                    float x = 0;
                    float y = 0;

                    GuiManager.Cursor.GetCursorPositionForSprite(ref x, ref y, sprite.Z);

                    if (mEditorLogic.CurrentSprites.Contains(sprite))
                    {	// dragging a current Sprite, so loop through all current Sprites and move them appropriately
                        foreach (Sprite s in mEditorLogic.CurrentSprites)
                        {
                            //MoveSpriteBy(s as IESprite, x - positionBeforeMouseMove.X,
                            //                            y - positionBeforeMouseMove.Y,
                            //                            0);
                        }
                    }

                }
                else if (GuiManager.Cursor.SecondaryDown)
                {
                    GuiManager.Cursor.StaticPosition = true;
                    sprite.Z += GuiManager.Cursor.YVelocity;

                }

                if (GuiManager.Cursor.SecondaryClick)
                    GuiManager.Cursor.StaticPosition = false;
            }



            #endregion

        }


        #endregion

        #region Private Methods


        static void ManageSpriteGrids()
        {
            if (mInactiveScene != null)
            {
                foreach (SpriteGrid spriteGrid in mInactiveScene.SpriteGrids)
                {
                    spriteGrid.Manage();
                }
            }

            if (mBlockingScene != null)
            {
                foreach (SpriteGrid spriteGrid in mBlockingScene.SpriteGrids)
                {
                    spriteGrid.Manage();
                }
            }
        }


        static void SetNewlyCreatedSpriteProperties(Sprite newSprite)
        {
            StringFunctions.MakeNameUnique<Sprite>(newSprite, mBlockingScene.Sprites);
            newSprite.X = SpriteManager.Camera.X;
            newSprite.Y = SpriteManager.Camera.Y;

            if (SpriteManager.Camera.Orthogonal && newSprite.Texture != null)
            {
                newSprite.ScaleX = newSprite.Texture.Width / 2.0f;
                newSprite.ScaleY = newSprite.Texture.Height / 2.0f;
            }
        }

        
        static List<Sprite> sSpritesToRemove = new List<Sprite>();
        static List<Text> sTextsToRemove = new List<Text>();
        static void UpdateObjectInstructionSets()
        {
            #region Sprites

            foreach (Sprite sprite in mBlockingScene.Sprites)
            {
                if (mObjectInstructionSets.ContainsKey(sprite) == false)
                {
                    mObjectInstructionSets.Add(sprite, new InstructionSet());
                }
            }

            sSpritesToRemove.Clear();
            // Use a temporary list to store off the Sprites that should be removed.
            foreach (KeyValuePair<INameable, InstructionSet> kvp in mObjectInstructionSets)
            {
                if (kvp.Key is Sprite && mBlockingScene.Sprites.Contains((kvp.Key as Sprite)) == false)
                {
                    sSpritesToRemove.Add(kvp.Key as Sprite);
                }
            }
            for (int i = 0; i < sSpritesToRemove.Count; i++)
            {
                mObjectInstructionSets.Remove(sSpritesToRemove[i]);
            }

            #endregion

            #region Texts

            foreach (Text text in mBlockingScene.Texts)
            {
                if (mObjectInstructionSets.ContainsKey(text) == false)
                {
                    mObjectInstructionSets.Add(text, new InstructionSet());
                }
            }

            // Use a temporary list to store off the Texts that should be removed.
            sTextsToRemove.Clear();
            foreach (KeyValuePair<INameable, InstructionSet> kvp in mObjectInstructionSets)
            {
                if (kvp.Key is Text && mBlockingScene.Texts.Contains((kvp.Key as Text)) == false)
                {
                    sTextsToRemove.Add(kvp.Key as Text);
                }
            }
            for (int i = 0; i < sTextsToRemove.Count; i++)
            {
                mObjectInstructionSets.Remove(sTextsToRemove[i]);
            }


            #endregion
        }


        static void UpdateUI()
        {

            GuiData.Update();

            mCameraBounds.UpdateBounds(0);
        }

        #endregion

        #endregion
    }

}
