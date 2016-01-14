using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Graphics;
using FlatRedBall;
using FlatRedBall.Instructions.Reflection;

namespace ParticleEditor.Entities
{
    public class EmissionAreaVisibleRepresentation
    {
        #region Fields

        Dictionary<Emitter.AreaEmissionType, object> mVisibleRepresentations = new Dictionary<Emitter.AreaEmissionType, object>();

        PositionedObject mCurrentVisibleRepresentation = null;

        #endregion

        #region Methods

        public EmissionAreaVisibleRepresentation()
        {
            mVisibleRepresentations.Add(Emitter.AreaEmissionType.Cube, new AxisAlignedCube());
            mVisibleRepresentations.Add(Emitter.AreaEmissionType.Point, Polygon.CreateEquilateral(1, 0, 0));
            mVisibleRepresentations.Add(Emitter.AreaEmissionType.Rectangle, new AxisAlignedRectangle());
        }

        public void UpdateToEmitter(Emitter currentEmitter)
        {
            #region Update All Visibility

            if (currentEmitter == null)
            {
                if (mCurrentVisibleRepresentation != null)
                {
                    MakeAllInvisible();


                    mCurrentVisibleRepresentation = null;

                }
            }
            else
            {
                object shapeThatShouldBeVisible = mVisibleRepresentations[currentEmitter.AreaEmission];

                if (shapeThatShouldBeVisible != ((object)mCurrentVisibleRepresentation))
                {
                    MakeAllInvisible();

                    object visibleRepresentation = mVisibleRepresentations[currentEmitter.AreaEmission];

                    LateBinder.GetInstance(visibleRepresentation.GetType()).SetValue(visibleRepresentation, "Visible", true);


                    mCurrentVisibleRepresentation = mVisibleRepresentations[currentEmitter.AreaEmission] as PositionedObject;
                }
            }

            #endregion

            #region Update the current Shape's properties

            if (mCurrentVisibleRepresentation != null)
            {
                switch (currentEmitter.AreaEmission)
                {
                    case Emitter.AreaEmissionType.Point:
                        // We do nothing here since we're not showing anything
                        break;
                    case Emitter.AreaEmissionType.Cube:
                        AxisAlignedCube asAxisAlignedCube = mCurrentVisibleRepresentation as AxisAlignedCube;

                        asAxisAlignedCube.ScaleX = currentEmitter.ScaleX;
                        asAxisAlignedCube.ScaleY = currentEmitter.ScaleY;
                        asAxisAlignedCube.ScaleZ = currentEmitter.ScaleZ;

                        asAxisAlignedCube.Position = currentEmitter.Position;

                        break;
                    case Emitter.AreaEmissionType.Rectangle:
                        AxisAlignedRectangle asAxisAlignedRectangle = mCurrentVisibleRepresentation as AxisAlignedRectangle;
                        asAxisAlignedRectangle.ScaleX = currentEmitter.ScaleX;
                        asAxisAlignedRectangle.ScaleY = currentEmitter.ScaleY;
                        asAxisAlignedRectangle.Position = currentEmitter.Position;

                        break;
                }

            }

            #endregion
        }

        private void MakeAllInvisible()
        {
            foreach (object visibleObject in mVisibleRepresentations.Values)
            {
                LateBinder.GetInstance(visibleObject.GetType()).SetValue(visibleObject, "Visible", false);
            }
        }


        #endregion
    }
}
