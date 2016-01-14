using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Graphics.Particle;
using System.ComponentModel;
using ParticleEditorControls.TypeConverters;
using FlatRedBall.Graphics;
using FlatRedBall.IO;
using FlatRedBall.Glue.Controls;
using System.Windows.Forms;
using ParticleEditorControls.Managers;

namespace ParticleEditorControls.PropertyGrids
{
    public class EmissionSettingsSaveDisplayer : PropertyGridDisplayer
    {
        public override object Instance
        {
            get
            {
                return base.Instance;
            }
            set
            {
                UpdateIncludeAndExclude(value as EmissionSettingsSave);
                base.Instance = value;
                UpdateCategories();
            }
        }

        public EmissionSettingsSave SettingsSaveInstance
        {
            get
            {
                return Instance as EmissionSettingsSave;
            }
        }

        private void UpdateCategories()
        {
            // I'm going to set everything that isn't Velocity below.  Leftovers will go under Velocity
            SetAllPropertyCategory("Velocity");

            // TODO: this should be a file browse dialog
            SetCategory("AnimationChains", "Texture and Animation");

            // TODO: this should be a dropdown if AnimationChains is populated
            SetCategory("CurrentChainName", "Texture and Animation");

            SetCategory("Animate", "Texture and Animation");

            SetCategory("Texture", "Texture and Animation");


            SetCategory("Billboarded", "Rotation");
            foreach (var property in NativePropertyGridMembers.Where(property => property.Name.Contains("Rotation")))
            {
                SetCategory(property.Name, "Rotation");
            }

            foreach (var property in NativePropertyGridMembers.Where(property => property.Name.Contains("Acceleration")))
            {
                SetCategory(property.Name, "Acceleration");
            }
            SetCategory("Drag", "Acceleration");


            SetCategory("ColorOperation", "Color");
            SetCategory("TintRed", "Color");
            SetCategory("TintRedRate", "Color");
            SetCategory("TintGreen", "Color");
            SetCategory("TintGreenRate", "Color");
            SetCategory("TintBlue", "Color");
            SetCategory("TintBlueRate", "Color");

            foreach (var property in NativePropertyGridMembers.Where(property => property.Name.Contains("Scale")))
            {
                SetCategory(property.Name, "Scale");
            }

            SetCategory("Fade", "Transparency");
            SetCategory("FadeRate", "Transparency");
            SetCategory("BlendOperation", "Transparency");


        }

        private void UpdateIncludeAndExclude(EmissionSettingsSave emissionSettings)
        {
            ResetToDefault();

            ExcludeMember("Instructions");

            if (emissionSettings != null)
            {

                var rangeType = emissionSettings.VelocityRangeType;
                if (rangeType != RangeType.Component)
                {
                    ExcludeMember("XVelocity");
                    ExcludeMember("YVelocity");
                    ExcludeMember("ZVelocity");
                    ExcludeMember("XVelocityRange");
                    ExcludeMember("YVelocityRange");
                    ExcludeMember("ZVelocityRange");
                }
                if (rangeType != RangeType.Cone &&
                    rangeType != RangeType.Wedge)
                {
                    ExcludeMember("WedgeAngle");
                    ExcludeMember("WedgeSpread");
                }

                if (rangeType == RangeType.Component)
                {
                    ExcludeMember("RadialVelocity");
                    ExcludeMember("RadialVelocityRange");
                }


                EnumToString blendEnumToString = new EnumToString();
                blendEnumToString.EnumType = typeof(BlendOperation);

                IncludeMember(
                    "BlendOperation",
                    containingType: typeof(EmissionSettingsSave),
                    typeConverter: blendEnumToString);

                EnumToString colorEnumToString = new EnumToString();
                colorEnumToString.EnumType = typeof(ColorOperation);

                IncludeMember(
                    "ColorOperation",
                    containingType: typeof(EmissionSettingsSave),
                    typeConverter: colorEnumToString);


                IncludeMember(
                    "Texture",
                    typeof(string),
                    HandleTextureChanged,
                    () =>
                    {
                        return SettingsSaveInstance.Texture;
                    },
                    null,
                    new Attribute[]
                        {
                            PropertyGridDisplayer.FileWindowAttribute, CategoryAttribute("Texture and Animation")[0]
                        }
                );

                IncludeMember(
                    "AnimationChains",
                    typeof(string),
                    HandleAniimationPathChanged,
                    () =>
                    {
                        return SettingsSaveInstance.AnimationChains;
                    },
                null,
                new Attribute[]
                {
                    PropertyGridDisplayer.FileWindowAttribute, CategoryAttribute("Texture and Animation")[0]
                }
                );
            }
        }

        protected override void HandlePropertyValueChanged(object s, System.Windows.Forms.PropertyValueChangedEventArgs e)
        {
            string name = e.ChangedItem.Label;

            if (name == "VelocityRangeType")
            {
                Instance = Instance;
                PropertyGrid.Refresh();
            }
        }

        void HandleAniimationPathChanged(object sender, MemberChangeArgs memberChangeArgs)
        {
            string fullFileName = memberChangeArgs.Value as string;
            string emitterDirectory = FileManager.GetDirectory(ProjectManager.Self.FileName);

            if (FileManager.IsRelative(fullFileName))
            {
                // This file is relative.
                // This means that the user
                // typed in a value rather than
                // using the file window.  We should
                // assume that the file is relative to
                // the emitter.
                fullFileName = emitterDirectory + fullFileName;
            }
            string relativeFileName = FileManager.MakeRelative(fullFileName, emitterDirectory);
            SettingsSaveInstance.AnimationChains = relativeFileName;
        }

        void HandleTextureChanged(object sender, MemberChangeArgs memberChangeArgs)
        {
            // Justin has reported a bug here.  I don't know what is going on, so I'm going to spit out the error to a message box
            try
            {
                string fullFileName = memberChangeArgs.Value as string;
                string emitterDirectory = FileManager.GetDirectory(ProjectManager.Self.FileName);

                if (FileManager.IsRelative(fullFileName))
                {
                    // This file is relative.
                    // This means that the user
                    // typed in a value rather than
                    // using the file window.  We should
                    // assume that the file is relative to
                    // the emitter.
                    fullFileName = emitterDirectory + fullFileName;
                }

                if (!FileManager.IsRelativeTo(fullFileName, emitterDirectory))
                {
                    MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
                    mbmb.MessageText = "The selected file:\n\n" + fullFileName + "\n\nis not relative to the Emitter file.  What would you like to do?";

                    mbmb.AddButton("Copy the file to the same folder as the Emitter file", System.Windows.Forms.DialogResult.Yes);
                    mbmb.AddButton("Keep the file where it is (this may limit the portability of the Emitter file)", System.Windows.Forms.DialogResult.No);

                    DialogResult result = mbmb.ShowDialog();

                    if (result == DialogResult.Yes)
                    {
                        string destination = emitterDirectory + FileManager.RemovePath(fullFileName);

                        try
                        {
                            System.IO.File.Copy(fullFileName, destination, true);
                            fullFileName = destination;
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("Could not copy the file:\n" + e);
                        }

                    }
                }

                string relativeFileName = FileManager.MakeRelative(fullFileName, emitterDirectory);

                SettingsSaveInstance.Texture = relativeFileName;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());

            }
        }
    }
}
