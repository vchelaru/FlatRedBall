using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Instructions;
#if FRB_MDX
using Microsoft.DirectX.Direct3D;
using FlatRedBall;

using FlatRedBall.Graphics;
using FlatRedBall.Content.Instructions;
#endif

namespace EditorObjects.Gui
{
    public class EmissionSettingsPropertyGrid : PropertyGrid<EmissionSettings>
    {
        #region fields
        private bool initialized = false;
        private InstructionBlueprintListPropertyGrid mInstructionPropertyGrid;

        List<string> mVelocityProperties = new List<string>();

        Dictionary<RangeType, List<string>> mVelocityPropertiesByRangeType = new Dictionary<RangeType, List<string>>();

        RangeType mLastVelocityRangeTypeSet;

        #endregion

        #region Properties
        public InstructionBlueprintListPropertyGrid InstructionDisplayWindow
        {
            get { return mInstructionPropertyGrid; }
        }
        #endregion

        #region Events

        public void AddInstructionBlueprint(Window callingWindow)
        {
            ListDisplayWindow listWindow = (GetUIElementForMember("Instructions") as InstructionBlueprintListPropertyGrid).ListDisplayWindow;
            List<InstructionBlueprint> tempList = listWindow.ListShowing as List<InstructionBlueprint>;
            tempList[tempList.Count - 1].TargetType = typeof(FlatRedBall.Sprite);
        }

        private void InitializeInstructionGrid(Window callingWindow)
        {
            if (!initialized)
            {
                InstructionBlueprintListPropertyGrid grid = ((InstructionBlueprintListPropertyGrid)GetUIElementForMember("Instructions"));
                grid.ListDisplayWindow.EnableRemovingFromList();
                grid.ListDisplayWindow.EnableAddingToList(typeof(InstructionBlueprint));


                grid.ListDisplayWindow.ShowPropertyGridOnStrongSelect = true;
                initialized = true;
            }
        }


        private void ChangeVelocityRangeType(Window callingWindow)
        {
            if (SelectedObject != null)
            {
                UpdateVelocityWindowInclusion(SelectedObject.VelocityRangeType);
            }
        }

        private void PropertiesUpdated(Window callingWindow)
        {
            if (SelectedObject != null && SelectedObject.VelocityRangeType != mLastVelocityRangeTypeSet)
            {
                UpdateVelocityWindowInclusion(SelectedObject.VelocityRangeType);
            }

        }

        #endregion

        #region Methods
        public EmissionSettingsPropertyGrid(Cursor cursor) : base(cursor)
        {
            #region Set "this" properties

            this.AfterUpdateDisplayedProperties += PropertiesUpdated;

            #endregion

            #region Include/Categorize members


            IncludeMember("VelocityRangeType", "Velocity");
            IncludeMember("RadialVelocity", "Velocity");
            IncludeMember("RadialVelocityRange", "Velocity");
            IncludeMember("XVelocity", "Velocity");
            IncludeMember("YVelocity", "Velocity");
            IncludeMember("ZVelocity", "Velocity");
            IncludeMember("XVelocityRange", "Velocity");
            IncludeMember("YVelocityRange", "Velocity");
            IncludeMember("ZVelocityRange", "Velocity");
            IncludeMember("WedgeAngle", "Velocity");
            IncludeMember("WedgeSpread", "Velocity");

            IncludeMember("ScaleX", "Scale");
            IncludeMember("ScaleY", "Scale");
            IncludeMember("ScaleXRange", "Scale");
            IncludeMember("ScaleYRange", "Scale");
            IncludeMember("ScaleXVelocity", "Scale");
            IncludeMember("ScaleYVelocity", "Scale");
            IncludeMember("ScaleXVelocityRange", "Scale");
            IncludeMember("ScaleYVelocityRange", "Scale");
            IncludeMember("MatchScaleXToY", "Scale");

            IncludeMember("XAcceleration", "Acceleration");
            IncludeMember("YAcceleration", "Acceleration");
            IncludeMember("ZAcceleration", "Acceleration");
            IncludeMember("XAccelerationRange", "Acceleration");
            IncludeMember("YAccelerationRange", "Acceleration");
            IncludeMember("ZAccelerationRange", "Acceleration");
            IncludeMember("Drag", "Acceleration");

            IncludeMember("Alpha", "Color/Fade");
            IncludeMember("Red", "Color/Fade");
            IncludeMember("Green", "Color/Fade");
            IncludeMember("Blue", "Color/Fade");

            ((UpDown)GetUIElementForMember("Alpha")).MinValue = 0;
            ((UpDown)GetUIElementForMember("Alpha")).MaxValue = 255;
            ((UpDown)GetUIElementForMember("Red")).MinValue = 0;
            ((UpDown)GetUIElementForMember("Green")).MinValue = 0;
            ((UpDown)GetUIElementForMember("Blue")).MinValue = 0;


            IncludeMember("AlphaRate", "Color/Fade");
            IncludeMember("RedRate", "Color/Fade");
            IncludeMember("GreenRate", "Color/Fade");
            IncludeMember("BlueRate", "Color/Fade");
            IncludeMember("BlendOperation", "Color/Fade");
            IncludeMember("ColorOperation", "Color/Fade");

            IncludeMember("Instructions", "Instructions");

            #endregion

            #region Set the event changed for the VelocityRangeType property

            //mVelocityWindows.Add(GetUIElementForMember("VelocityRangeType"));
            mVelocityProperties.Add("RadialVelocity");
            mVelocityProperties.Add("RadialVelocityRange");
            mVelocityProperties.Add("XVelocity");
            mVelocityProperties.Add("YVelocity");
            mVelocityProperties.Add("ZVelocity");
            mVelocityProperties.Add("XVelocityRange");
            mVelocityProperties.Add("YVelocityRange");
            mVelocityProperties.Add("ZVelocityRange");
            mVelocityProperties.Add("WedgeAngle");
            mVelocityProperties.Add("WedgeSpread");

            List<string> properties = new List<string>();
            properties.Add("XVelocity");
            properties.Add("YVelocity");
            properties.Add("ZVelocity");
            properties.Add("XVelocityRange");
            properties.Add("YVelocityRange");
            properties.Add("ZVelocityRange");
            mVelocityPropertiesByRangeType.Add(RangeType.Component, properties);

            properties = new List<string>();
            properties.Add("WedgeAngle");
            properties.Add("WedgeSpread");
            properties.Add("RadialVelocity");
            properties.Add("RadialVelocityRange");
            mVelocityPropertiesByRangeType.Add(RangeType.Cone, properties);

            properties = new List<string>();
            properties.Add("RadialVelocity");
            properties.Add("RadialVelocityRange");
            mVelocityPropertiesByRangeType.Add(RangeType.Radial, properties);

            properties = new List<string>();
            properties.Add("RadialVelocity");
            properties.Add("RadialVelocityRange");
            mVelocityPropertiesByRangeType.Add(RangeType.Spherical, properties);

            properties = new List<string>();
            properties.Add("WedgeAngle");
            properties.Add("WedgeSpread");
            properties.Add("RadialVelocity");
            properties.Add("RadialVelocityRange");
            mVelocityPropertiesByRangeType.Add(RangeType.Wedge, properties);

            SetMemberChangeEvent("VelocityRangeType", ChangeVelocityRangeType);

            #endregion

            #region Create the "Instructions" UI element

            SetMemberDisplayName("Instructions", "");

            mInstructionPropertyGrid = new InstructionBlueprintListPropertyGrid(GuiManager.Cursor);
            mInstructionPropertyGrid.HasMoveBar = false;
            AfterUpdateDisplayedProperties += new GuiMessage(InitializeInstructionGrid);

            ReplaceMemberUIElement("Instructions", mInstructionPropertyGrid);

            mInstructionPropertyGrid.ListDisplayWindow.AfterAddItem += new GuiMessage(AddInstructionBlueprint);
            mInstructionPropertyGrid.ListDisplayWindow.ScaleX = 18;
            mInstructionPropertyGrid.ScaleX = 23;

            #endregion

            #region If not in FRB XNA, remove elements not supported in FRB XNA

#if !FRB_XNA
            // Only do this for non FRB_XNA
            ComboBox colorOperationComboBox = GetUIElementForMember("ColorOperation") as ComboBox;


            for (int i = colorOperationComboBox.Count - 1; i > -1; i--)
            {
                TextureOperation textureOperation =
                    ((TextureOperation)colorOperationComboBox[i].ReferenceObject);

                if (!FlatRedBall.Graphics.GraphicalEnumerations.IsTextureOperationSupportedInFrbXna(
                    textureOperation))
                {
                    colorOperationComboBox.RemoveAt(i);
                }
            }
#endif
            #endregion

            ExcludeMember("AnimationChain");
            ExcludeMember("Animate");
        }


        private void UpdateVelocityWindowInclusion(RangeType velocityRangeType)
        {
            mLastVelocityRangeTypeSet = velocityRangeType;
            for (int i = 0; i < mVelocityProperties.Count; i++)
            {
                ExcludeMember(mVelocityProperties[i]);
            }

            List<string> propertiesToInclude = mVelocityPropertiesByRangeType[velocityRangeType];

            for (int i = 0; i < propertiesToInclude.Count; i++)
            {
                const bool callUpdateDisplayedProperties = false;

                IncludeMember(propertiesToInclude[i], "Velocity", callUpdateDisplayedProperties);
            }

        }
        #endregion

    }
}
