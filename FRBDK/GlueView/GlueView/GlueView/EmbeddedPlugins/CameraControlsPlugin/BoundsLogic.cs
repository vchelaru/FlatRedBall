using EditorObjects;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Math.Geometry;
using GlueView.Facades;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueView.EmbeddedPlugins.CameraControlsPlugin
{
    class BoundsLogic
    {
        AxisAlignedRectangle boundsRectangle;

        Line originX;
        Line originY;

        LineGrid lineGrid;

        bool showOrigin;
        public bool ShowOrigin
        {
            get { return showOrigin; }
            set
            {
                showOrigin = value;

                RefreshOrigin();
            }
        }

        bool showGrid;
        public bool ShowGrid
        {
            get { return showGrid; }
            set
            {
                showGrid = value;

                RefreshGrid();
            }
        }

        int cellSize;
        public int CellSize
        {
            get { return cellSize; }
            set
            {
                cellSize = value;
                RefreshGrid();
            }
        }

        internal void HandleElementLoaded()
        {
            RefreshAll();
        }

        private void RefreshAll()
        {
            RefreshBounds();

            RefreshOrigin();
        }

        private void RefreshOrigin()
        {
            if (showOrigin && originX == null)
            {
                originX = ShapeManager.AddLine();
                originX.SetFromAbsoluteEndpoints(
                    new Vector3(10000, 0, 0),
                    new Vector3(-10000, 0, 0));

                originY = ShapeManager.AddLine();
                originY.SetFromAbsoluteEndpoints(
                    new Vector3(0, 10000, 0),
                    new Vector3(0, -10000, 0));
            }

            if(originX != null)
            {
                originX.Visible = ShowOrigin;
                originY.Visible = ShowOrigin;
            }
        }

        private void RefreshGrid()
        {
            if(ShowGrid && lineGrid == null)
            {
                lineGrid = new LineGrid();
            }

            if(lineGrid != null)
            {
                lineGrid.Visible = ShowGrid;
            }

            if(lineGrid?.Visible == true)
            {
                lineGrid.DistanceBetweenLines = cellSize;
                lineGrid.NumberOfHorizontalLines = 41;
                lineGrid.NumberOfVerticalLines = 41;
            }
        }

        private void RefreshBounds()
        {
            if (GlueViewState.Self.CurrentGlueProject != null)
            {
                if (boundsRectangle == null)
                {
                    boundsRectangle = ShapeManager.AddAxisAlignedRectangle();
                }

                var glueProject = GlueViewState.Self.CurrentGlueProject;

                if (glueProject.DisplaySettings != null)
                {
                    if (glueProject.DisplaySettings.Is2D)
                    {
                        boundsRectangle.Width = glueProject.DisplaySettings.ResolutionWidth;
                        boundsRectangle.Height = glueProject.DisplaySettings.ResolutionHeight;
                    }
                }
                else
                {
                    boundsRectangle.Width = glueProject.OrthogonalWidth;
                    boundsRectangle.Height = glueProject.OrthogonalHeight;
                }

                var currentElement = GlueViewState.Self.CurrentElement;
                var recursiveNamedObjects = currentElement.GetAllNamedObjectsRecurisvely();

                // see if the current element has a camera and if it's offset...
                var cameraNoses = recursiveNamedObjects
                    .Where(item =>
                        item.SourceType == FlatRedBall.Glue.SaveClasses.SourceType.FlatRedBallType &&
                        item.SourceClassType == "Camera");

                foreach(var cameraNos in cameraNoses)
                {
                    var xAsObject = cameraNos.GetPropertyValue("X");
                    var yAsObject = cameraNos.GetPropertyValue("Y");

                    if(xAsObject is float)
                    {
                        boundsRectangle.X = (float)xAsObject;
                    }
                    if(yAsObject is float)
                    {
                        boundsRectangle.Y = (float)yAsObject;
                    }
                }
            }
        }
    }
}
