using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Content.Scene;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall;
using FlatRedBall.IO;

namespace GlueView.SaveClasses
{
    #region ElementCameraSave class

    public class ElementCameraSave
    {
        public CameraSave CameraSave;
        public string ElementName;
    }

    #endregion

    public class GlueViewSettings
    {
        #region Fields

        public List<string> CollapsedPlugins { get; set; } = new List<string>();

        public List<ElementCameraSave> ElementCameraSaves = new List<ElementCameraSave>();

        #endregion

        #region Properties

        public ElementCameraSave this[string elementName]
        {
            get
            {
                for (int i = ElementCameraSaves.Count - 1; i > -1; i--)
                {
                    ElementCameraSave ecs = ElementCameraSaves[i];

                    if (ecs.ElementName == elementName)
                    {
                        return ecs;
                    }
                }

                return null;
            }
        }

        #endregion

        public void SetElementCameraSave(IElement element, Camera camera)
        {
            CameraSave cameraSave = CameraSave.FromCamera(camera, false);

            ElementCameraSave ecs = this[element.Name];

            if (ecs == null)
            {
                ecs = new ElementCameraSave();
                ecs.ElementName = element.Name;
                ElementCameraSaves.Add(ecs);
            }

            ecs.CameraSave = cameraSave;
        }

        #region XML Docs
        /// <summary>
        /// Sets the argument Camera to the saved Camera values associated with the argument IElement,
        /// or does nothing if there is not an entry for the IElement.
        /// </summary>
        /// <param name="element">The element to use as the key when searching for Camera values.</param>
        /// <param name="camera">The camera to assign.</param>
        /// <returns>Whether properties on the Camera were set.  Properties will not be set if there isn't an entry for the given element.</returns>
        #endregion
        public bool SetCameraToSavedValues(IElement element, Camera camera)
        {
            ElementCameraSave ecs = this[element.Name];
            bool returnValue = false;
            if (ecs != null)
            {
                ecs.CameraSave.SetCamera(SpriteManager.Camera);
                returnValue = true;
            }

            return returnValue;
        }

        public static GlueViewSettings Load(string fileName)
        {
            return FileManager.XmlDeserialize<GlueViewSettings>(fileName);
        }

        public void Save(string fileName)
        {
            FileManager.XmlSerialize(this, fileName);
        }
    }
}
