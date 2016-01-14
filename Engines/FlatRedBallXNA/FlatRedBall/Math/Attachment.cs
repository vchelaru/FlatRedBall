using System;
using System.Collections.Generic;
using System.Text;

#if FRB_XNA || SILVERLIGHT || WINDOWS_PHONE
using Microsoft.Xna.Framework;
#elif FRB_MDX
using Microsoft.DirectX;
#endif
using FlatRedBall.Utilities;

namespace FlatRedBall.Math
{
    public class Attachment : INameable
    {
        #region Fields

        public Vector3 RelativePosition;

        private Matrix mRelativeRotationMatrix;

        private PositionedObject mParent;

        private string mName;

        #endregion

        #region Properties

        public string Name
        {
            get { return mName; }
            set { mName = value; }
        }

        public PositionedObject Parent
        {
            get { return mParent; }
            set { mParent = value; }
        }

        public Matrix RelativeRotationMatrix
        {   // This is a property in case individual rotation components
            // are going to be added later.
            get { return mRelativeRotationMatrix; }
            set { mRelativeRotationMatrix = value; }
        }

        #endregion

        #region Methods

        public void ApplyTo(PositionedObject positionedObject)
        {
            positionedObject.AttachTo(Parent, false);
            positionedObject.RelativeRotationMatrix = RelativeRotationMatrix;
            positionedObject.RelativePosition = RelativePosition;
        }

        public static Attachment FromAttachment(PositionedObject child)
        {
            if (child.Parent == null)
            {
                throw new ArgumentException("The argument child must have a parent to create an Attachment instance.");
            }

            Attachment attachment = new Attachment();
            attachment.Parent = child.Parent;
            attachment.RelativePosition = child.RelativePosition;
            attachment.RelativeRotationMatrix = child.RelativeRotationMatrix;

            return attachment;
        }



        #endregion
    }
}
