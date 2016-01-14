using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.Graphics.Particle;

namespace EditorObjects.Gui
{
    public class EmitterPropertyGrid : PropertyGrid<Emitter>
    {
        SpritePropertyGrid mSpritePropertyGrid;

        #region Properties

        public override Emitter SelectedObject
        {
            get
            {
                return base.SelectedObject;
            }
            set
            {
                base.SelectedObject = value;

                if (SelectedObject != null)
                {
                    this.mSpritePropertyGrid.SelectedObject = SelectedObject.ParticleBlueprint;
                    this.SelectCategory(this.SelectedCategory);
                }

                this.Visible = SelectedObject != null;
            }
        }

        #endregion

        #region Event Methods

        private void ScaleEmitterClick(Window callingWindow)
        {
            TextInputWindow tiw = GuiManager.ShowTextInputWindow(
                "Enter amount to scale by", "Scale Emitter");

            tiw.Format = TextBox.FormatTypes.Decimal;

            tiw.OkClick += ScaleEmitterOk;
        }

        private void ScaleEmitterOk(Window callingWindow)
        {
            TextInputWindow tiw = (TextInputWindow)callingWindow;

            try
            {
                float scaleAmount = float.Parse(tiw.Text);
                ScaleEmitter(scaleAmount);
            }
            catch
            {
                GuiManager.ShowMessageBox("Invalid scaling value", "Error");
            }
        }

        #endregion

        #region Methods

        #region Constructor

        public EmitterPropertyGrid(Cursor cursor)
            : base(cursor)
        {
            #region Set "this" properties
            MinimumScaleY = 10;
            this.Name = "Emitter Properties";


            SelectedObject = null;

            Visible = false;


            #endregion

            ExcludeAllMembers();

            #region Simple includes and categories

            IncludeMember("ParentVelocityChangesEmissionVelocity", "Attachment");
            IncludeMember("ParentRotationChangesPosition", "Attachment");
            IncludeMember("RelativeX", "Attachment");
            IncludeMember("RelativeY", "Attachment");
            IncludeMember("RelativeZ", "Attachment");
            IncludeMember("RelativeRotationX", "Attachment");
            IncludeMember("RelativeRotationY", "Attachment");
            IncludeMember("RelativeRotationZ", "Attachment");

            IncludeMember("X", "Basic");
            IncludeMember("Y", "Basic");
            IncludeMember("Z", "Basic");

            IncludeMember("RotationX", "Basic");
            IncludeMember("RotationY", "Basic");
            IncludeMember("RotationZ", "Basic");

            IncludeMember("Name", "Basic");

			IncludeMember("RotationChangesParticleRotation", "Basic");
			IncludeMember("RotationChangesParticleAcceleration", "Basic");

            IncludeMember("EmissionSettings", "Basic");

            IncludeMember("Texture", "Texture");

            IncludeMember("RemovalEvent", "Removal");
            IncludeMember("SecondsLasting", "Removal");

            IncludeMember("AreaEmission", "Emission Area");

            IncludeMember("ScaleX", "Emission Area");
            IncludeMember("ScaleY", "Emission Area");
            IncludeMember("ScaleZ", "Emission Area");


            IncludeMember("SecondFrequency", "Frequency");
            IncludeMember("NumberPerEmission", "Frequency");
            IncludeMember("TimedEmission", "Frequency");

            IncludeMember("BoundedEmission", "Boundary");


            #endregion

            #region Set value minimums and maximums

            UpDown upDown = GetUIElementForMember("ScaleX") as UpDown;
            upDown.MinValue = 0;

            upDown = GetUIElementForMember("ScaleY") as UpDown;
            upDown.MinValue = 0;

            upDown = GetUIElementForMember("ScaleZ") as UpDown;
            upDown.MinValue = 0;

            #endregion

            #region Create the AreaEmissionType ComboBox

            ComboBox emissionComboBox = new ComboBox(GuiManager.Cursor);
            emissionComboBox.AddItem("Point");
            emissionComboBox.AddItem("Rectangle");
            emissionComboBox.AddItem("Cube");
            emissionComboBox.ScaleX = 4;

            ReplaceMemberUIElement("AreaEmissionType", emissionComboBox);

            #endregion

            #region Create the ScaleEmitter ComboBox



            Button scaleEmitterButton = new Button(this.mCursor);
            scaleEmitterButton.Text = "Scale Emitter";
            scaleEmitterButton.ScaleX = 6;
            scaleEmitterButton.ScaleY = 1.5f;
            scaleEmitterButton.Click += ScaleEmitterClick;

            this.AddWindow(scaleEmitterButton, "Actions");

            #endregion

            mSpritePropertyGrid = new SpritePropertyGrid(cursor);
            mSpritePropertyGrid.HasMoveBar = false;
            mSpritePropertyGrid.MakeVisibleOnSpriteSet = false;
            this.AddWindow(mSpritePropertyGrid, "Blueprint");
            mSpritePropertyGrid.Visible = false;



            RemoveCategory("Uncategorized");

            SelectCategory("Basic");
            KeepInScreen();

            // This needs to be last
            X = 20.4f;
            Y = 15.7f;
            this.MinimumScaleY = 12;

            PropertyGrid.SetPropertyGridTypeAssociation(typeof(EmissionSettings),
                typeof(EmissionSettingsPropertyGrid));


        }

        #endregion

        #region Private Methods

        private void ScaleEmitter(float scaleAmount)
        {
            // scale all of the values:

            Emitter e = SelectedObject;

            if (e.EmissionBoundary != null)
            {
                e.EmissionBoundary.ScaleBy(scaleAmount);
            }

            e.ScaleX *= scaleAmount;
            e.ScaleY *= scaleAmount;
            e.ScaleZ *= scaleAmount;

            e.EmissionSettings.RadialVelocity *= scaleAmount;
            e.EmissionSettings.RadialVelocityRange *= scaleAmount;

            e.EmissionSettings.ScaleX *= scaleAmount;
            e.EmissionSettings.ScaleXRange *= scaleAmount;
            e.EmissionSettings.ScaleXVelocity *= scaleAmount;
            e.EmissionSettings.ScaleXVelocityRange *= scaleAmount;

            e.EmissionSettings.ScaleY *= scaleAmount;
            e.EmissionSettings.ScaleYRange *= scaleAmount;
            e.EmissionSettings.ScaleYVelocity *= scaleAmount;
            e.EmissionSettings.ScaleYVelocityRange *= scaleAmount;

            // do we scale acceleration by the same amount?
            e.EmissionSettings.XAcceleration *= scaleAmount;
            e.EmissionSettings.XAccelerationRange *= scaleAmount;

            e.EmissionSettings.XVelocity *= scaleAmount;
            e.EmissionSettings.XVelocityRange *= scaleAmount;

            e.EmissionSettings.YAcceleration *= scaleAmount;
            e.EmissionSettings.YAccelerationRange *= scaleAmount;

            e.EmissionSettings.YVelocity *= scaleAmount;
            e.EmissionSettings.YVelocityRange *= scaleAmount;

            e.EmissionSettings.ZAcceleration *= scaleAmount;
            e.EmissionSettings.ZAccelerationRange *= scaleAmount;

            e.EmissionSettings.ZVelocity *= scaleAmount;
            e.EmissionSettings.ZVelocityRange *= scaleAmount;
        }

        #endregion



        #endregion
    }
}
