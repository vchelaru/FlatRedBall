using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Content.Particle;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Instructions.Reflection;
using ParticleEditorControls.TypeConverters;
using FlatRedBall.IO;
using System.Windows.Forms;
using FlatRedBall.Glue.Controls;

namespace ParticleEditorControls.PropertyGrids
{
    class EmitterSavePropertyGridDisplayer : PropertyGridDisplayer
    {
        public override object Instance
        {
            get
            {
                return base.Instance;
            }
            set
            {
                UpdateIncludeAndExclude();
                base.Instance = value;

                CategorizeProperties();
            }
        }

        public EmitterSave EmitterSaveInstance
        {
            get
            {
                return Instance as EmitterSave;
            }
        }

        private void CategorizeProperties()
        {
            SetAllPropertyCategory("Emitter Values");

            SetCategory("X", "Position");
            SetCategory("Y", "Position");
            SetCategory("Z", "Position");

            SetCategory("RotationX", "Rotation");
            SetCategory("RotationY", "Rotation");
            SetCategory("RotationZ", "Rotation");

            SetCategory("ScaleX", "Emission Area");
            SetCategory("ScaleY", "Emission Area");
            SetCategory("ScaleZ", "Emission Area");
            SetCategory("AreaEmissionType", "Emission Area");

            SetCategory("RemovalEvent", "Removal");
            SetCategory("SecondsLasting", "Removal");

        }

        private void UpdateIncludeAndExclude()
        {
            ExcludeMember("AssetsRelativeToFile");
            ExcludeMember("EmissionBoundary");
            ExcludeMember("EmissionSettings");
            ExcludeMember("FileName");
            ExcludeMember("ParentSpriteName");
            ExcludeMember("ParticleBlueprint");

            ExcludeMember("RelativeX");
            ExcludeMember("RelativeY");
            ExcludeMember("RelativeZ");




            EnumToString areaEmissionToString = new EnumToString();
            areaEmissionToString.EnumType = typeof(FlatRedBall.Graphics.Particle.Emitter.AreaEmissionType);

            IncludeMember(
                "AreaEmissionType",
                containingType: typeof(EmitterSave),
                typeConverter: areaEmissionToString);


            EnumToString removalEventToString = new EnumToString();
            removalEventToString.EnumType = typeof(FlatRedBall.Graphics.Particle.Emitter.RemovalEventType);
            //EmitterSave es;

            IncludeMember(
                "RemovalEvent",
                containingType: typeof(EmitterSave),
                typeConverter: removalEventToString);
        }
    }
}
