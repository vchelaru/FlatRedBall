using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall;
using FlatRedBall.Glue.GuiDisplay;

namespace GlueView.Forms.PropertyGrids
{
    public class CameraDisplayer : PropertyGridDisplayer
    {
        public override object Instance
        {
            get
            {
                return base.Instance;
            }
            set
            {
                mInstance = value;
                UpdateDisplayedProperties(value as Camera);
                base.Instance = value;

                UpdateSets();
            }
        }

        private void UpdateSets()
        {
            GetPropertyGridMember("Orthogonal").AfterMemberChange += HandleOrthogonalMemberChange;
        }

        void HandleOrthogonalMemberChange(object sender, MemberChangeArgs args)
        {
            // We need to lose focus so we can update the UI immediately
            this.PropertyGrid.Enabled = false;
            UpdateDisplayedProperties(mInstance as Camera);
            UpdateSets();
            this.PropertyGrid.Enabled = true;
        }

        private void UpdateDisplayedProperties(Camera camera)
        {
            ExcludeAllMembers();

            // For some reason categorizing the properties didn't work here.  I can't
            // get them to categorize, but that's okay, I'll eventually replace this with
            // the wpf grid.
            IncludeMember("X");
            IncludeMember("Y");
            IncludeMember("Z");



            IncludeMember("Orthogonal");

            if (camera.Orthogonal)
            {
                IncludeMember("OrthogonalWidth");
                IncludeMember("OrthogonalHeight");
            }
            else
            {
                IncludeMember("FieldOfView");

                IncludeMember("RotationX");
                IncludeMember("RotationY");
                IncludeMember("RotationZ");
                
            }


            IncludeMember("BackgroundColor");

            IncludeMember("Filtering", typeof(bool), OnFilteringChange, GetFiltering);


        }


        object GetFiltering()
        {
            return FlatRedBallServices.GraphicsOptions.TextureFilter == Microsoft.Xna.Framework.Graphics.TextureFilter.Linear;
        }


        void OnFilteringChange(object sender, MemberChangeArgs args)
        {
            bool newValue = (bool)args.Value;

            if (newValue)
            {
                FlatRedBallServices.GraphicsOptions.TextureFilter = Microsoft.Xna.Framework.Graphics.TextureFilter.Linear;
            }
            else
            {
                FlatRedBallServices.GraphicsOptions.TextureFilter = Microsoft.Xna.Framework.Graphics.TextureFilter.Point;
            }
        }

    }
}
