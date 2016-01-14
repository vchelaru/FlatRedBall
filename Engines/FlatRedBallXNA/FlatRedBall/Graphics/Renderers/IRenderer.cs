using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

// THESE ARE ALL STILL IN-PROCESS CODE.  THEY WILL NOT AFFECT THE BUILD IN ANY WAY

namespace FlatRedBall.Graphics.Renderers
{
    #region XML Docs
    /// <summary>
    /// The possible ways to sort objects which should be sorted (Sprites, Texts, IDrawableBatches).
    /// </summary>
    #endregion
    public enum SortMode
    {
        None,
        Z,
        DistanceFromCamera,
        DistanceAlongForwardVector
    }

    #region XML Docs
    /// <summary>
    /// Renders objects
    /// </summary>
    #endregion
    public interface IRenderer
    {
        #region Properties

        #region XML Docs
        /// <summary>
        /// Whether or not the objects in this renderer are drawn in order with order renderers.
        /// </summary>
        #endregion
        bool DrawSorted { get; }

        #region XML Docs
        /// <summary>
        /// Gets or sets the current sorting mode.
        /// </summary>
        #endregion
        SortMode SortMode { get; set; }

        #endregion

        #region Methods

        #region XML Docs
        /// <summary>
        /// Prepares objects on the specified camera for drawing before beginning the drawing loop.
        /// </summary>
        /// <param name="camera">The camera to prepare.</param>
        #endregion
        void Prepare(Camera camera);

#if !SILVERLIGHT
        #region XML Docs
        /// <summary>
        /// Sets up render states for this renderer.  Called when the last renderer to draw was not
        /// this renderer.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="renderMode"></param>
        #endregion
        void SetDeviceSettings(Camera camera, RenderMode renderMode);

        #region XML Docs
        /// <summary>
        /// Draws objects on the specified layer.  For renderers that DrawSorted, this should draw the
        /// next object on the layer, and GetNextObjectDepth should now return the following object.
        /// </summary>
        /// <param name="camera">The camera to draw.</param>
        /// <param name="layer">The layer to draw.</param>
        /// <param name="renderMode"></param>
        #endregion
        void Draw(Camera camera, Layer layer, RenderMode renderMode);
#endif

        #region XML Docs
        /// <summary>
        /// Returns the depth of the next object to draw on this layer for the current sort mode.
        /// </summary>
        /// <param name="camera">The Camera currently being drawn.</param>
        /// <param name="layer">The layer currently being drawn.</param>
        /// <returns>The depth of the next object on this layer for the current sort mode.</returns>
        #endregion
        float GetNextObjectDepth(Camera camera, Layer layer);

        #region XML Docs
        /// <summary>
        /// Returns true if this layer hasn't drawn all of its objects for this Renderer.
        /// </summary>
        /// <param name="camera">The Camera to check</param>
        /// <param name="layer">The layer to check.</param>
        /// <returns>Whether or not the renderer has drawn all objects on the layer.</returns>
        #endregion
        bool HasObjectsLeftToDraw(Camera camera, Layer layer);

        #region XML Docs
        /// <summary>
        /// Called to remove layer information from a renderer (for renderers that store
        /// layer information).
        /// </summary>
        /// <param name="camera">The Camera to move the Layer from</param>
        /// <param name="layer">The layer to remove</param>
        #endregion
        void RemoveLayer(Camera camera, Layer layer);

        #endregion
    }
}
