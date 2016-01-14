using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;

using FlatRedBall.ManagedSpriteGroups;

using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;

using EditorObjects;
using EditorObjects.Undo;
using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Model;
using FlatRedBall.Gui;
#if FRB_XNA
using Microsoft.Xna.Framework.Input;
using EditorObjects.Undo.PropertyComparers;

#elif FRB_MDX
using Keys = Microsoft.DirectX.DirectInput.Key;
using EditorObjects.Undo.PropertyComparers;


#endif

namespace EditorObjects
{
    public static class UndoManager
    {
        #region Fields

        static List<InstructionList> mInstructions = new List<InstructionList>();

        static InstructionList mUndosAddedThisFrame = new InstructionList();

        static Dictionary<Type, PropertyComparer> 
            mPropertyComparers = new Dictionary<Type, PropertyComparer>();

        static ListDisplayWindow mInstructionListDisplayWindow;

        #endregion

        #region Properties

        public static List<InstructionList> Instructions
        {
            get { return mInstructions; }
        }

        #endregion

        #region Methods

        #region Constructor

        static UndoManager()
        {
			// These should be moved into their own constructors that the user can call depending on what undos they need

            #region Set the members to watch for Sprites
            PropertyComparer<Sprite> spritePropertyComparer =
                new PropertyComparer<Sprite>();
            mPropertyComparers.Add(typeof(Sprite), spritePropertyComparer);

            spritePropertyComparer.AddMemberWatching<float>("X");
            spritePropertyComparer.AddMemberWatching<float>("Y");
            spritePropertyComparer.AddMemberWatching<float>("Z");

            spritePropertyComparer.AddMemberWatching<float>("ScaleX");
            spritePropertyComparer.AddMemberWatching<float>("ScaleY");

            spritePropertyComparer.AddMemberWatching<float>("RotationX");
            spritePropertyComparer.AddMemberWatching<float>("RotationY");
            spritePropertyComparer.AddMemberWatching<float>("RotationZ");

            spritePropertyComparer.AddMemberWatching<float>("RelativeX");
            spritePropertyComparer.AddMemberWatching<float>("RelativeY");
            spritePropertyComparer.AddMemberWatching<float>("RelativeZ");

            spritePropertyComparer.AddMemberWatching<float>("RelativeRotationX");
            spritePropertyComparer.AddMemberWatching<float>("RelativeRotationY");
            spritePropertyComparer.AddMemberWatching<float>("RelativeRotationZ");

            #endregion

            #region Set the members to watch for SpriteFrames
            PropertyComparer<SpriteFrame> spriteFramePropertyComparer =
                new PropertyComparer<SpriteFrame>();
            mPropertyComparers.Add(typeof(SpriteFrame), spriteFramePropertyComparer);

            spriteFramePropertyComparer.AddMemberWatching<float>("X");
            spriteFramePropertyComparer.AddMemberWatching<float>("Y");
            spriteFramePropertyComparer.AddMemberWatching<float>("Z");

            spriteFramePropertyComparer.AddMemberWatching<float>("ScaleX");
            spriteFramePropertyComparer.AddMemberWatching<float>("ScaleY");

            spriteFramePropertyComparer.AddMemberWatching<float>("RotationX");
            spriteFramePropertyComparer.AddMemberWatching<float>("RotationY");
            spriteFramePropertyComparer.AddMemberWatching<float>("RotationZ");

            spriteFramePropertyComparer.AddMemberWatching<float>("RelativeX");
            spriteFramePropertyComparer.AddMemberWatching<float>("RelativeY");
            spriteFramePropertyComparer.AddMemberWatching<float>("RelativeZ");

            spriteFramePropertyComparer.AddMemberWatching<float>("RelativeRotationX");
            spriteFramePropertyComparer.AddMemberWatching<float>("RelativeRotationY");
            spriteFramePropertyComparer.AddMemberWatching<float>("RelativeRotationZ");

            #endregion

            #region Set the members to watch for Polygons

            PolygonPropertyComparer polygonPropertyComparer = new PolygonPropertyComparer();
            mPropertyComparers.Add(typeof(Polygon), polygonPropertyComparer);

            polygonPropertyComparer.AddMemberWatching<float>("X");
            polygonPropertyComparer.AddMemberWatching<float>("Y");
            polygonPropertyComparer.AddMemberWatching<float>("Z");

            polygonPropertyComparer.AddMemberWatching<float>("RotationZ");

            #endregion

            #region Set the members to watch for AxisAlignedRectangles

            PropertyComparer<AxisAlignedRectangle> axisAlignedRectanglePropertyComparer =
                new PropertyComparer<AxisAlignedRectangle>();
            mPropertyComparers.Add(typeof(AxisAlignedRectangle), axisAlignedRectanglePropertyComparer);

            axisAlignedRectanglePropertyComparer.AddMemberWatching<float>("X");
            axisAlignedRectanglePropertyComparer.AddMemberWatching<float>("Y");
            axisAlignedRectanglePropertyComparer.AddMemberWatching<float>("Z");

            axisAlignedRectanglePropertyComparer.AddMemberWatching<float>("ScaleX");
            axisAlignedRectanglePropertyComparer.AddMemberWatching<float>("ScaleY");

            #endregion

            #region Set the members to watch for Circles

            PropertyComparer<Circle> circlePropertyComparer = new PropertyComparer<Circle>();
            mPropertyComparers.Add(typeof(Circle), circlePropertyComparer);

            circlePropertyComparer.AddMemberWatching<float>("X");
            circlePropertyComparer.AddMemberWatching<float>("Y");
            circlePropertyComparer.AddMemberWatching<float>("Z");

            circlePropertyComparer.AddMemberWatching<float>("Radius");

            #endregion

            #region Set the members to watch for Spheres

            PropertyComparer<Sphere> spherePropertyComparer =
                new PropertyComparer<Sphere>();
            mPropertyComparers.Add(typeof(Sphere), spherePropertyComparer);

            spherePropertyComparer.AddMemberWatching<float>("X");
            spherePropertyComparer.AddMemberWatching<float>("Y");
            spherePropertyComparer.AddMemberWatching<float>("Z");

            spherePropertyComparer.AddMemberWatching<float>("Radius");

            #endregion

            #region Set the members to watch for Texts
            PropertyComparer<Text> textPropertyComparer =
                new PropertyComparer<Text>();
            mPropertyComparers.Add(typeof(Text), textPropertyComparer);

            textPropertyComparer.AddMemberWatching<float>("X");
            textPropertyComparer.AddMemberWatching<float>("Y");
            textPropertyComparer.AddMemberWatching<float>("Z");

            textPropertyComparer.AddMemberWatching<float>("RotationZ");
            #endregion

            #region Set the members to watch for PositionedModels
            PropertyComparer<PositionedModel> positionedModelPropertyComparer =
                new PropertyComparer<PositionedModel>();
            mPropertyComparers.Add(typeof(PositionedModel), positionedModelPropertyComparer);

            positionedModelPropertyComparer.AddMemberWatching<float>("X");
            positionedModelPropertyComparer.AddMemberWatching<float>("Y");
            positionedModelPropertyComparer.AddMemberWatching<float>("Z");

            positionedModelPropertyComparer.AddMemberWatching<float>("RotationZ");

            #endregion

            #region Set the members to watch for SpriteGrids

            mPropertyComparers.Add(typeof(SpriteGrid), 
                new SpriteGridPropertyComparer());

            #endregion
        }

        #endregion

        #region Public Methods

        public static void AddToThisFramesUndo(Instruction instructionToAdd)
        {
            mUndosAddedThisFrame.Add(instructionToAdd);
        }


        public static void AddToWatch<T>(T objectToWatch) where T : new()
        {
            PropertyComparer<T> propertyComparer =
                mPropertyComparers[typeof(T)] as PropertyComparer<T>;

            if (objectToWatch == null)
            {
                throw new NullReferenceException("Can't add a null object to the UndoManager's watch");
            }

            if (propertyComparer.Contains(objectToWatch) == false)
            {
                propertyComparer.AddObject(objectToWatch, new T());
            }
        }


		public static void AddAxisAlignedCubePropertyComparer()
		{
			PropertyComparer<AxisAlignedCube> axisAlignedCubePropertyComparer =
				new PropertyComparer<AxisAlignedCube>();
			mPropertyComparers.Add(typeof(AxisAlignedCube), axisAlignedCubePropertyComparer);

			axisAlignedCubePropertyComparer.AddMemberWatching<float>("X");
			axisAlignedCubePropertyComparer.AddMemberWatching<float>("Y");
			axisAlignedCubePropertyComparer.AddMemberWatching<float>("Z");

			axisAlignedCubePropertyComparer.AddMemberWatching<float>("ScaleX");
			axisAlignedCubePropertyComparer.AddMemberWatching<float>("ScaleY");
			axisAlignedCubePropertyComparer.AddMemberWatching<float>("ScaleZ");
		}


        public static void ClearObjectsWatching<T>() where T : new()
        {
            PropertyComparer<T> propertyComparer =
                mPropertyComparers[typeof(T)] as PropertyComparer<T>;
            propertyComparer.ClearObjects();
        }


        public static void EndOfFrameActivity()
        {
            UpdateListDisplayWindow();

            #region Perform Undos if pushed Control+Z

            if (InputManager.Keyboard.ControlZPushed() &&
                mInstructions.Count != 0)
            {
                InstructionList instructionList = mInstructions[mInstructions.Count - 1];

                for (int i = 0; i < instructionList.Count; i++)
                {
                    Instruction instruction = instructionList[i];
                    
                    // See if the instruction is one that has an associated delegate
                    if (instruction is GenericInstruction)
                    {
                        GenericInstruction asGenericInstruction = instruction as GenericInstruction;

                        Type targetType = asGenericInstruction.Target.GetType();

                        PropertyComparer propertyComparerForType = null;

                        if(mPropertyComparers.ContainsKey(targetType))
                        {
                            // There is a PropertyComparer for this exact type
                            propertyComparerForType = mPropertyComparers[targetType];
                        }
                        else
                        {
                            // There isn't a PropertyComparer for this exact type, so climb up the inheritance tree
                            foreach (PropertyComparer pc in mPropertyComparers.Values)
                            {
                                if (pc.GenericType.IsAssignableFrom(targetType))
                                {
                                    propertyComparerForType = pc;
                                    break;
                                }
                            }
                        }

                        // If there's no PropertyComparer, then the app might be a UI-only app.  If that's
                        // the case, we don't want to run afterUpdateDelegates
                        if (propertyComparerForType != null)
                        {

                            AfterUpdateDelegate afterUpdateDelegate =
                                propertyComparerForType.GetAfterUpdateDelegateForMember(asGenericInstruction.Member);

                            if (afterUpdateDelegate != null)
                            {
                                afterUpdateDelegate(asGenericInstruction.Target);
                            }
                        }
                    }
                    
                    instruction.Execute();

                }
                mInstructions.RemoveAt(mInstructions.Count - 1);
            }

            #endregion

            // Vic says that this will break if undos are added through this and through
            // the property comparers in the same frame.
            if (mUndosAddedThisFrame.Count != 0)
            {
                mInstructions.Add(mUndosAddedThisFrame);

                mUndosAddedThisFrame = new InstructionList();
            }
        }


        public static bool HasPropertyComparerForType(Type type)
        {
            return mPropertyComparers.ContainsKey(type);
        }


        public static void RecordUndos<T>() where T : new()
        {
            RecordUndos<T>(true);
        }


        public static void RecordUndos<T>(bool createNewList) where T : new()
        {            
            PropertyComparer<T> propertyComparer =
                mPropertyComparers[typeof(T)] as PropertyComparer<T>;

            propertyComparer.GetAllChangedMemberInstructions(mInstructions, createNewList);
        }


        public static void SetAfterUpdateDelegate(Type type, string member, AfterUpdateDelegate afterUpdateDelegate)
        {
            PropertyComparer propertyComparer =
                mPropertyComparers[type];

            propertyComparer.SetAfterUpdateDelegateForMember(member, afterUpdateDelegate);

        }


        public static void SetPropertyComparer(Type type, PropertyComparer propertyComparer)
        {
            if (mPropertyComparers.ContainsKey(type))
            {
                mPropertyComparers.Remove(type);
            }

            mPropertyComparers.Add(type, propertyComparer);
        }


        public static void ShowListDisplayWindow()
        {
            ShowListDisplayWindow(null);
        }

        /// <summary>
        /// Shows a ListDisplayWindow with the undos in the UndoManager.  This is provided as a convenience function so it 
        /// can be used in MenuStrips.
        /// </summary>
        /// <param name="callingWindow">This property will not be used - it's here to match the GuiMessage delegate.</param>
        public static void ShowListDisplayWindow(Window callingWindow)
        {
            #region If the ListDisplayWindow hasn't been created yet, create it

            if (mInstructionListDisplayWindow == null)
            {
                mInstructionListDisplayWindow = new ListDisplayWindow(GuiManager.Cursor);
                
                mInstructionListDisplayWindow.ListShowing = mInstructions;
                mInstructionListDisplayWindow.HasMoveBar = true;
                mInstructionListDisplayWindow.HasCloseButton = true;
                mInstructionListDisplayWindow.Resizable = true;
                mInstructionListDisplayWindow.ScaleX = 9;
                mInstructionListDisplayWindow.X = SpriteManager.Camera.XEdge * 2 - mInstructionListDisplayWindow.ScaleX;
                mInstructionListDisplayWindow.Name = "Undos";

                GuiManager.AddWindow(mInstructionListDisplayWindow);
            }

            #endregion

            #region Show and bring the ListDisplayWindow to the top

            mInstructionListDisplayWindow.Visible = true;
            GuiManager.BringToFront(mInstructionListDisplayWindow);

            #endregion
        }


        public static new string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("Undos: ").Append(mInstructions.Count.ToString());

            return stringBuilder.ToString();
        }

        #endregion

        #region Private Methods

        private static void UpdateListDisplayWindow()
        {
            if (mInstructionListDisplayWindow != null)
            {
                mInstructionListDisplayWindow.UpdateToList();
            }
        }

        #endregion

        #endregion
    }
}
