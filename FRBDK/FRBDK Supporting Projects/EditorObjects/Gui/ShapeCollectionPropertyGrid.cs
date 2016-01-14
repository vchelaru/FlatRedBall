using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Graphics;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Graphics.Model;
using FlatRedBall.Math.Geometry;

#if FRB_XNA
using Keys = Microsoft.Xna.Framework.Input.Keys;
#elif FRB_MDX
using Keys = Microsoft.DirectX.DirectInput.Key;
#endif

namespace EditorObjects.Gui
{
    public class ShapeCollectionPropertyGrid : PropertyGrid<ShapeCollection>
    {
        #region Fields

        ListDisplayWindow mAxisAlignedRectangles;
        ListDisplayWindow mAxisAlignedCubes;
        ListDisplayWindow mPolygons;
        ListDisplayWindow mSpheres;
        ListDisplayWindow mCircles;

        #endregion

        #region Properties

        public AxisAlignedRectangle CurrentAxisAlignedRectangle
        {
            get { return mAxisAlignedRectangles.GetFirstHighlightedObject() as AxisAlignedRectangle; }
            set 
            {
                if (value != CurrentAxisAlignedRectangle)
                {
                    mAxisAlignedRectangles.HighlightObjectNoCall(value, false);

                    if (value != null)
                    {
                        SelectCategory("AxisAlignedRectangles");
                    }
                }
            } 
        }

        public AxisAlignedCube CurrentAxisAlignedCube
        {
            get { return mAxisAlignedCubes.GetFirstHighlightedObject() as AxisAlignedCube;}
            set 
            {
                if (value != CurrentAxisAlignedCube)
                {
                    mAxisAlignedCubes.HighlightObjectNoCall(value, false);
                    

                    if (value != null)
                    {
                        SelectCategory("AxisAlignedCubes");
                    }
                }
            } 
        }

        public Circle CurrentCircle
        {
            get { return mCircles.GetFirstHighlightedObject() as Circle; }
            set 
            {
                if (value != CurrentCircle)
                {
                    

                    mCircles.HighlightObjectNoCall(value, false);

                    if (value != null)
                    {
                        SelectCategory("Circles");
                    }
                }
            }
        }

        public Sphere CurrentSphere
        {
            get { return mSpheres.GetFirstHighlightedObject() as Sphere; }
            set 
            {
                if (value != CurrentSphere)
                {
                    mSpheres.HighlightObjectNoCall(value, false);

                    if (value != null)
                    {
                        SelectCategory("Spheres");
                    }
                }
            }
        }

        public Polygon CurrentPolygon
        {
            get { return mPolygons.GetFirstHighlightedObject() as Polygon; }
            set 
            {
                if (value != CurrentPolygon)
                {
                    mPolygons.HighlightObjectNoCall(value, false);
                    if (value != null)
                    {
                        SelectCategory("Polygons");
                    }
                }
            }
        }

        public bool ShowPropertyGridOnStrongSelectAxisAlignedRectangle
        {            
            get { return mAxisAlignedRectangles.ShowPropertyGridOnStrongSelect;}
            set { mAxisAlignedRectangles.ShowPropertyGridOnStrongSelect = value;}          
        }

        public bool ShowPropertyGridOnStrongSelectAxisAlignedCube
        {
            get { return mAxisAlignedCubes.ShowPropertyGridOnStrongSelect; }
            set { mAxisAlignedCubes.ShowPropertyGridOnStrongSelect = value; }
        }

        public bool ShowPropertyGridOnStrongSelectCircle
        {
            get { return mCircles.ShowPropertyGridOnStrongSelect; }
            set { mCircles.ShowPropertyGridOnStrongSelect = value; }
        }

        public bool ShowPropertyGridOnStrongSelectSphere
        {
            get { return mSpheres.ShowPropertyGridOnStrongSelect; }
            set { mSpheres.ShowPropertyGridOnStrongSelect = value; }
        }

        public bool ShowPropertyGridOnStrongSelectPolygon
        {
            get { return mPolygons.ShowPropertyGridOnStrongSelect; }
            set { mPolygons.ShowPropertyGridOnStrongSelect = value; }
        }

        public override List<FlatRedBall.Instructions.InstructionList> UndoInstructions
        {
            set
            {
                base.UndoInstructions = value;

                mAxisAlignedRectangles.UndoInstructions = value;
                mAxisAlignedCubes.UndoInstructions = value;
                mCircles.UndoInstructions = value;
                mSpheres.UndoInstructions = value;
                mPolygons.UndoInstructions = value;
            }
        }

        #endregion

        #region Events

        public event GuiMessage AxisAlignedRectangleSelected;
        public event GuiMessage AxisAlignedCubeSelected;
        public event GuiMessage CircleSelected;
        public event GuiMessage SphereSelected;
        public event GuiMessage PolygonSelected;

        #endregion

        #region Event Methods

        private void AxisAlignedRectangleListBoxClick(Window callingWindow)
        {
            if (AxisAlignedRectangleSelected != null)
            {
                AxisAlignedRectangleSelected(this);
            }
        }

        private void AxisAlignedCubeListBoxClick(Window callingWindow)
        {
            if (AxisAlignedCubeSelected != null)
            {
                AxisAlignedCubeSelected(this);
            }
        }

        private void CircleListBoxClick(Window callingWindow)
        {
            if (CircleSelected != null)
            {
                CircleSelected(this);
            }
        }

        private void SphereListBoxClick(Window callingWindow)
        {
            if (SphereSelected != null)
            {
                SphereSelected(this);
            }
        }

        private void PolygonListBoxClick(Window callingWindow)
        {
            if (PolygonSelected != null)
            {
                PolygonSelected(this);
            }
        }

        #endregion

        #region Methods

        #region Constructor

        public ShapeCollectionPropertyGrid(Cursor cursor)
            : base(cursor)
        {
            #region Exclude/include members and create categories

            ExcludeAllMembers();

            IncludeMember("AxisAlignedRectangles", "AxisAlignedRectangles");
            IncludeMember("AxisAlignedCubes", "AxisAlignedCubes");
            IncludeMember("Circles", "Circles");
            IncludeMember("Spheres", "Spheres");
            IncludeMember("Polygons", "Polygons");

            RemoveCategory("Uncategorized");

            #endregion

            CreateListDisplayWindows();

            SetPropertyGridTypeAssociations();
        }

        #endregion

        public void AddIgnoredKey(Keys keyToIgnore)
        {
            mAxisAlignedRectangles.IgnoredKeys.Add(keyToIgnore);
            mAxisAlignedCubes.IgnoredKeys.Add(keyToIgnore);
            mPolygons.IgnoredKeys.Add(keyToIgnore);
            mSpheres.IgnoredKeys.Add(keyToIgnore);
            mCircles.IgnoredKeys.Add(keyToIgnore);

        }

        private void CreateListDisplayWindows()
        {
            float listDisplayWindowScaleX = 14;
            float listDisplayWindowScaleY = 16;

            #region AxisAlignedRectangle PropertyGrid Initialization
            mAxisAlignedRectangles = new ListDisplayWindow(this.mCursor);
            mAxisAlignedRectangles.ScaleX = listDisplayWindowScaleX;
            mAxisAlignedRectangles.ScaleY = listDisplayWindowScaleY;
            mAxisAlignedRectangles.ListBox.Highlight += AxisAlignedRectangleListBoxClick;
            mAxisAlignedRectangles.ConsiderAttachments = true;
            SetMemberDisplayName("AxisAlignedRectangles", "");
            #endregion

            #region AxisAlignedCube PropertyGrid Initialization
            mAxisAlignedCubes = new ListDisplayWindow(this.mCursor);
            mAxisAlignedCubes.ScaleX = listDisplayWindowScaleX;
            mAxisAlignedCubes.ScaleY = listDisplayWindowScaleY;
            mAxisAlignedCubes.ListBox.Highlight += AxisAlignedCubeListBoxClick;
            mAxisAlignedCubes.ConsiderAttachments = true;
            SetMemberDisplayName("AxisAlignedCubes", "");
            #endregion

            #region Circle PropertyGrid Initialization
            mCircles = new ListDisplayWindow(this.mCursor);
            mCircles.ScaleX = listDisplayWindowScaleX;
            mCircles.ScaleY = listDisplayWindowScaleY;
            mCircles.ListBox.Highlight += CircleListBoxClick;
            mCircles.ConsiderAttachments = true;
            SetMemberDisplayName("Circles", "");
            #endregion

            #region Sphere PropertyGrid Initialization
            mSpheres = new ListDisplayWindow(this.mCursor);
            mSpheres.ScaleX = listDisplayWindowScaleX;
            mSpheres.ScaleY = listDisplayWindowScaleY;
            mSpheres.ListBox.Highlight += SphereListBoxClick;
            mSpheres.ConsiderAttachments = true;
            SetMemberDisplayName("Spheres", "");
            #endregion

            #region Polygon PropertyGrid Initialization
            mPolygons = new ListDisplayWindow(this.mCursor);
            mPolygons.ScaleX = listDisplayWindowScaleX;
            mPolygons.ScaleY = listDisplayWindowScaleY;
            mPolygons.ListBox.Highlight += PolygonListBoxClick;
            mPolygons.ConsiderAttachments = true;
            SetMemberDisplayName("Polygons", "");
            #endregion

            #region Replace the UI elements with the newly-created ListDisplayWindows
            // Call ReplaceMemberUIElement last
            // because it calls UpdateDisplayedProperties
            // which calls UpdateToList on all ListDisplayWindows.
            // If all aren't created before UpdateToList is called,
            // then we'll get a NullReferenceException.
            this.ReplaceMemberUIElement("Spheres", mSpheres);
            this.ReplaceMemberUIElement("Circles", mCircles);
            this.ReplaceMemberUIElement("AxisAlignedCubes", mAxisAlignedCubes);
            this.ReplaceMemberUIElement("AxisAlignedRectangles", mAxisAlignedRectangles);
            this.ReplaceMemberUIElement("Polygons", mPolygons);
            #endregion
        }


        private void SetPropertyGridTypeAssociations()
        {
            SetPropertyGridTypeAssociation(typeof(AxisAlignedRectangle), typeof(AxisAlignedRectanglePropertyGrid));
            SetPropertyGridTypeAssociation(typeof(AxisAlignedCube), typeof(AxisAlignedCubePropertyGrid));
            SetPropertyGridTypeAssociation(typeof(Circle), typeof(CirclePropertyGrid));
            SetPropertyGridTypeAssociation(typeof(Sphere), typeof(SpherePropertyGrid));
            SetPropertyGridTypeAssociation(typeof(Polygon), typeof(PolygonPropertyGrid));
        }


        public override void UpdateDisplayedProperties()
        {
            base.UpdateDisplayedProperties();

            #region Update invisible ListDisplayWindows.  See inside for explanation of why we're doing this.
            // The ListDisplayWindows for 
            // each shape type are used to
            // create PropertyGrids when the
            // user selects a shape.  To do this
            // the ListDisplayWindows must have their
            // lists updated so that every object has a
            // CollapseItem.  Therefore, we need to make
            // sure that all ListDisplayWindows are updated.
            // The visible ones will be updated automatically
            // by this, so we only need to worry about invisible
            // ones.

            // Since Circle is created last, test against that
            if (mCircles != null && ObjectDisplaying != null)
            {
                if (mAxisAlignedRectangles.Visible == false)
                {
                    mAxisAlignedRectangles.ListShowing = ObjectDisplaying.AxisAlignedRectangles;
                }

                if (mAxisAlignedCubes.Visible == false)
                {
                    mAxisAlignedCubes.ListShowing = ObjectDisplaying.AxisAlignedCubes;
                }

                if (mPolygons.Visible == false)
                {
                    mPolygons.ListShowing = ObjectDisplaying.Polygons;
                }

                if (mSpheres.Visible == false)
                {
                    mSpheres.ListShowing = ObjectDisplaying.Spheres;
                }

                if (mCircles.Visible == false)
                {
                    mCircles.ListShowing = ObjectDisplaying.Circles;
                }
            }
            #endregion

        }


        #endregion
    }
}
