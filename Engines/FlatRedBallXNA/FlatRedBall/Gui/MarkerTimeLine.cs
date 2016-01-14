using System;
using System.Collections.Generic;

using System.Collections;
using FlatRedBall.Utilities;
#if FRB_MDX
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Texture2D = FlatRedBall.Texture2D;


#else
using Microsoft.Xna.Framework.Graphics;
#endif

namespace FlatRedBall.Gui
{
	/// <summary>
	/// Summary description for MarkerTimeLine.
	/// </summary>
	public class MarkerTimeLine : TimeLine
	{
		#region Fields
		List<Marker> markerArray;

		#region XML Docs
		/// <summary>
		/// Tells which marker was clicked on.
		/// </summary>
		/// <remarks>
        /// This variable is only to be used in the MarkerClick event.  
		/// </remarks>
		#endregion
		public Marker MarkerClicked;

        private Marker mMarkerPushed;
		
        #endregion

        #region Events

        #region XML Docs
        /// <summary>
        /// Event raised when the user clicks on a Marker.
        /// </summary>
        #endregion
        public event GuiMessage MarkerClick = null;

        #endregion

        #region Event Methods

        private void TimeLineClicked(Window callingWindow)
        {
            foreach (Marker m in markerArray)
            {
                float markerPosition = (float)GetPosOnBar(m.Value);

                if (mCursor.XForUI > markerPosition - m.ScaleX && mCursor.XForUI < markerPosition + m.ScaleX)
                {
                    this.CurrentValue = m.Value;
                    MarkerClicked = m;
                    if (MarkerClick != null)
                        MarkerClick(this);

                    break;
                }
            }

            this.MarkerClicked = null;
        }

        private void TimeLineDragged(Window callingWindow)
        {
            if (mMarkerPushed != null)
            {
                mMarkerPushed.Value = PositionToValueAbsolute((float)(mCursor.XForUI));

                if (mMarkerPushed.ReferenceObject is ITimed)
                {
                    ((ITimed)mMarkerPushed.ReferenceObject).Time = mMarkerPushed.Value;
                }
            }
        }

        private void TimeLinePushed(Window callingWindow)
        {
            double value = PositionToValueAbsolute((float)(mCursor.XForUI));

            foreach (Marker m in markerArray)
            {
                if (value > m.Value - m.ScaleX && value < m.Value + m.ScaleX)
                {
                    mMarkerPushed = m;
                }
            }
        }


        #endregion

        #region Methods

        #region Constructor

        #region XML Docs
        /// <summary>
        /// Instantiates a new MarkerTimeLine which will interact with the argument Cursor.
        /// </summary>
        /// <param name="cursor">The Cursor that the MarkerTimeLine will interact with.</param>
        #endregion
        public MarkerTimeLine(Cursor cursor) : base(cursor)
		{
			markerArray = new List<Marker>();
                
			this.Click += TimeLineClicked;
            this.Dragging += TimeLineDragged;
            this.Push += TimeLinePushed;

        }

        #endregion

        #region Public Methods

        #region XML Docs
        /// <summary>
        /// Adds a Marker which will be positioned at the time specified
        /// by the "value" argument referencing the referenceObject argument.
        /// </summary>
        /// <param name="value">The time at which to place the new Marker.</param>
        /// <param name="referenceObject">The object that the Marker will reference - can be null.</param>
        /// <returns>The newly-created Marker.</returns>
        #endregion
        public Marker AddMarker(double value, object referenceObject)
		{
            return AddMarker(value, referenceObject, null);
        }

        #region XML Docs
        /// <summary>
        /// Adds a custom-textured Marker which will be positioned at the time specified
        /// by the "value" argument referencing the referenceObject argument.
        /// </summary>
        /// <param name="value">The time at which to place the new Marker.</param>
        /// <param name="referenceObject">The object that the Marker will reference - can be null.</param>
        /// <param name="textureToUse">The new Marker's texture - will use the default texture if null.</param>
        /// <returns>The newly-created Marker.</returns>
        #endregion
        public Marker AddMarker(double value, object referenceObject, Texture2D textureToUse)
        {
			Marker marker = new Marker();
			marker.Value = value;
			marker.ReferenceObject = referenceObject;
            marker.Texture = textureToUse; // if the marker has a null texture then the default rendering is used.

			markerArray.Add(marker);

            return marker;
        }

        #region XML Docs
        /// <summary>
        /// Adds markers according to the argument List of ITimed objects.
        /// </summary>
        /// <typeparam name="T">The type of object being added.</typeparam>
        /// <param name="timedList">The list of ITimed objects.</param>
        #endregion
        public void AddMarkers<T>(List<T> timedList) where T : FlatRedBall.Utilities.ITimed
        {
            foreach (ITimed timed in timedList)
            {
                AddMarker(timed.Time, timed);
            }
        }

        #region XML Docs
        /// <summary>
        /// Clears all Markers.
        /// </summary>
        #endregion
        public void ClearMarkers()
		{
			markerArray.Clear();
        }

        #region XML Docs
        /// <summary>
        /// Returns the first Marker found with its ReferenceObject property
        /// matching the argument referenceObject or null if no matchers are found.
        /// </summary>
        /// <param name="referenceObject">The referenceObject to search for.</param>
        /// <returns>The Marker found or null if none are found.</returns>
        #endregion
        public Marker FindMarkerByReferenceObject(object referenceObject)
        {
            foreach (Marker marker in markerArray)
                if (marker.ReferenceObject == referenceObject)
                    return marker;

            return null;

        }

        #region XML Docs
        /// <summary>
        /// Finds the Marker that references the argument referenceObject
        /// and changes its Value property (usually it's time) to the argument
        /// newValue.
        /// </summary>
        /// <param name="referenceObject">The object to search for.</param>
        /// <param name="newValue">The new value to set.</param>
        /// <returns>The index of the new Marker after the value change.</returns>
        #endregion
        public int MoveMarker(object referenceObject, double newValue)
        {
            FindMarkerByReferenceObject(referenceObject).Value = newValue;

            // returns the new index of marker

            int index = 0;
            for (int i = 0; i < markerArray.Count; i++)
                if (((Marker)(markerArray[i])).Value < newValue)
                    index++;
            return index;
        }

        #region XML Docs
        /// <summary>
        /// Removes the Marker which references the argument referenceObject.
        /// </summary>
        /// <param name="referenceObject">The object to search for.</param>
        #endregion
        public void RemoveMarker(object referenceObject)
        {
            markerArray.Remove(FindMarkerByReferenceObject(referenceObject));
        }

        #endregion

        #region Internal Methods

        internal override void DrawSelfAndChildren(Camera camera)
        {
            #region Draw the base
            base.DrawSelfAndChildren(camera);
            #endregion

            #region Prepare variables for the loop
            float yToUse = mWorldUnitY;

			float xToUse;

			StaticVertices[0].Position.Z = StaticVertices[1].Position.Z = StaticVertices[2].Position.Z = 
                StaticVertices[3].Position.Z = StaticVertices[4].Position.Z = StaticVertices[5].Position.Z = 
                camera.Z + FlatRedBall.Math.MathFunctions.ForwardVector3.Z * 100;

			StaticVertices[0].Color = StaticVertices[1].Color = StaticVertices[2].Color = StaticVertices[3].Color = StaticVertices[4].Color = StaticVertices[5].Color = mColor;
            #endregion

            foreach (Marker marker in markerArray)
			{
				if(marker.Value >= Start && marker.Value <= Start + ValueWidth)
                {
                    #region If the marker is using the default texture, set the texture coordinates appropriately
                    if (marker.Texture == null)
                    {
					    StaticVertices[0].TextureCoordinate.X = 0;
					    StaticVertices[0].TextureCoordinate.Y = .875f;

					    StaticVertices[1].TextureCoordinate.X = 0;	
					    StaticVertices[1].TextureCoordinate.Y = .876f;

                        StaticVertices[2].TextureCoordinate.X = 0;	
					    StaticVertices[2].TextureCoordinate.Y = .875f;

                        StaticVertices[5].TextureCoordinate.X = .001f;
                        StaticVertices[5].TextureCoordinate.Y = .876f;
                    }
                    #endregion

                    #region Else, the marker is using a custom texture, so add a texture switch and set the coordinates appropriately
                    else
                    {
                        GuiManager.AddTextureSwitch(marker.Texture);

					    StaticVertices[0].TextureCoordinate.X = 0;
					    StaticVertices[0].TextureCoordinate.Y = 1;

					    StaticVertices[1].TextureCoordinate.X = 0;	
					    StaticVertices[1].TextureCoordinate.Y = 0;

                        StaticVertices[2].TextureCoordinate.X = 1;	
					    StaticVertices[2].TextureCoordinate.Y = 0;

                        StaticVertices[5].TextureCoordinate.X = 1f;
                        StaticVertices[5].TextureCoordinate.Y = 1;
                    }
                    #endregion

                    xToUse = 
                        (float)GetPosOnBar(marker.Value);

					StaticVertices[0].Position.X = xToUse - marker.ScaleX;
                    StaticVertices[0].Position.Y = yToUse - marker.ScaleY;

                    StaticVertices[1].Position.X = xToUse - marker.ScaleX;
                    StaticVertices[1].Position.Y = yToUse + marker.ScaleY;

                    StaticVertices[2].Position.X = xToUse + marker.ScaleX;
                    StaticVertices[2].Position.Y = yToUse + marker.ScaleY;

					StaticVertices[3] = StaticVertices[0];
					StaticVertices[4] = StaticVertices[2];

                    StaticVertices[5].Position.X = xToUse + marker.ScaleX;
                    StaticVertices[5].Position.Y = yToUse - marker.ScaleY;

                    GuiManager.WriteVerts(StaticVertices);

				}
			}

		}


        internal override int GetNumberOfVerticesToDraw()
		{
			int i = 0;

			foreach(Marker marker in markerArray)
			{
				if(marker.Value >= Start && marker.Value <= Start + ValueWidth)
				{
					i += 6;
				}

			}

			return base.GetNumberOfVerticesToDraw() + i;
        }


        public override void TestCollision(Cursor cursor)
        {
            // If the mouse button is not down, then this should
            // be set to false BEFORE the GUI events are raised.
            if (cursor.PrimaryDown == false)
            {
                mMarkerPushed = null;
            }

            base.TestCollision(cursor);
        }

        #endregion	
	
		#endregion

	}
}
