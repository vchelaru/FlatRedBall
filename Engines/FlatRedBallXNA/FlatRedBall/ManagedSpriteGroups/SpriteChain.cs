
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

using FlatRedBall.Graphics;
using FlatRedBall.Math.Geometry;

using Vector3 = Microsoft.Xna.Framework.Vector3;
using FlatRedBall.Input;

using Matrix = Microsoft.Xna.Framework.Matrix;
using System.Collections.ObjectModel;

namespace FlatRedBall.ManagedSpriteGroups
{
    public class SpriteChain : IColorable
    {
        #region Fields

        //////////////// OLD VALUES ////////////////////
        public PositionedObject head;
        public bool headEndVisible;
        public float headTv;
        private Vector3 lastPlaced;
        private double lastPlacement;
        private float minimumPlacementDistance;
        private float minimumPlacementDistanceSquared;
        private float placementFrequency;

        public bool stretchTexture;
        public float stretchTv;
        private Texture2D texture;
        private float mWidth = 1;
        public float xOffset;
        public float yOffset;
        ////////////// End of OLD VALUES //////////////////


        string mName;

        private List<Vector3> mPoints;
        private ReadOnlyCollection<Vector3> mPointsReadOnly;

        float mBlueRate;
        float mRedRate;
        float mGreenRate;
        float mAlphaRate;

        SpriteList mSpriteList = new SpriteList();

        ColorOperation mColorOperation = ColorOperation.Texture;

        float mRed;
        float mGreen;
        float mBlue;
        float mAlpha = FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue;

        BlendOperation mBlendOperation = BlendOperation.Regular;

        #endregion

        #region Properties

        public Texture2D Texture
        {
            get { return texture; }
            set { texture = value; }
        }

        public string Name
        {
            get { return mName; }
            set
            {
                mName = value;
                mSpriteList.Name = "SpriteChain List " + mName;
            }
        }

        public IList<Vector3> Points
        {
            get
            {
                return mPointsReadOnly;
            }
            set
            {
                // Get the texture of the last Sprite so that we can reuse it when we recreate everything
                Texture2D textureToUse = null;
                if (mSpriteList.Count != 0)
                {
                    textureToUse = mSpriteList.Last.Texture;
                }

                SetPoints(value);
            }
        }

        public float Width
        {
            get
            {
                return this.mWidth;
            }
            set
            {
                this.mWidth = value;
            }
        }

        public Layer Layer
        {
            get;
            set;
        }

        #region IColorable

        #region XML Docs
        /// <summary>
        /// The color operation to use when applying the Red, Green, and Blue properties.
        /// </summary>
        #endregion
        public ColorOperation ColorOperation
        {
            get { return mColorOperation; }
            set
            {
                mColorOperation = value;

                for (int i = 0; i < mSpriteList.Count; i++)
                {
                    mSpriteList[i].ColorOperation = mColorOperation;
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// Controls how the SpriteChain will blend with objects behind it when drawn.
        /// </summary>
        #endregion
        public BlendOperation BlendOperation
        {
            get
            {
                return mBlendOperation;
            }
            set
            {
                mBlendOperation = value;

                for (int i = 0; i < mSpriteList.Count; i++)
                {
                    mSpriteList[i].BlendOperation = mBlendOperation;
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// The red component to use when applying the ColorOperation.
        /// </summary>
        #endregion
        public float Red
        {
            get { return mRed; }
            set
            {
                mRed = value;

                for (int i = 0; i < mSpriteList.Count; i++)
                {
                    mSpriteList[i].Red = value;
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// The green component to use when applying the ColorOperation.
        /// </summary>
        #endregion
        public float Green
        {
            get { return mGreen; }
            set
            {
                mGreen = value;

                for (int i = 0; i < mSpriteList.Count; i++)
                {
                    mSpriteList[i].Green = value;
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// The blue component to use when applying the ColorOperation.
        /// </summary>
        #endregion
        public float Blue
        {
            get { return mBlue; }
            set
            {
                mBlue = value;

                for (int i = 0; i < mSpriteList.Count; i++)
                {
                    mSpriteList[i].Blue = value;
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// The alpha (opacity) to use when rendering the SpriteChain.
        /// </summary>
        #endregion
        public float Alpha
        {
            get { return mAlpha; }
            set
            {
                mAlpha = value;

                for (int i = 0; i < mSpriteList.Count; i++)
                {
                    mSpriteList[i].Alpha = value;
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// The rate of change (in units per second) to apply to the Red property.
        /// </summary>
        #endregion
        public float RedRate
        {
            get
            {
                throw new InvalidOperationException("This value is read-only");
            }
            set
            {
                mRedRate = value;

                for (int i = 0; i < mSpriteList.Count; i++)
                {
                    mSpriteList[i].RedRate = mRedRate;
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// The rate of change (in units per second) to apply to the Green property.
        /// </summary>
        #endregion
        public float GreenRate
        {
            get
            {
                throw new InvalidOperationException("This value is read-only");
            }
            set
            {
                mGreenRate = value;

                for (int i = 0; i < mSpriteList.Count; i++)
                {
                    mSpriteList[i].GreenRate = mGreenRate;
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// The rate of change (in units per second) to apply to the Glue property.
        /// </summary>
        #endregion
        public float BlueRate
        {
            get
            {
                throw new InvalidOperationException("This value is read-only");
            }
            set
            {
                mBlueRate = value;

                for (int i = 0; i < mSpriteList.Count; i++)
                {
                    mSpriteList[i].BlueRate = mBlueRate;
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// The rate of change (in units per second) to apply to the Alpha property.
        /// </summary>
        #endregion
        public float AlphaRate
        {
            get
            {
                throw new InvalidOperationException("This value is read-only");
            }
            set
            {
                mAlphaRate = value;

                for (int i = 0; i < mSpriteList.Count; i++)
                {
                    mSpriteList[i].AlphaRate = mAlphaRate;
                }
            }
        }

        #endregion

        #region IVisible
        bool mVisible;
        public bool Visible
        {
            get { return mVisible; }
            set
            {
                mVisible = value;
                for (int i = 0; i < mSpriteList.Count; i++)
                    mSpriteList[i].Visible = mVisible;
            }
        }
        #endregion

        public bool CreatesParticleSprite
        {
            get;
            set;
        }

        public bool CreatesManuallyUpdatedSprites
        {
            get;
            set;
        }

        #endregion

        #region Methods


        #region Constructor

        public SpriteChain()// : base(sprMan)
        {
            mVisible = true;
            mSpriteList.Name = "SpriteChain List";
            this.mPoints = new List<Vector3>();
            mPointsReadOnly = new ReadOnlyCollection<Vector3>(mPoints);

            this.headEndVisible = true;

            this.stretchTv = 0.5f;
        }

        #endregion

        #region Public Methods

        public Sprite AddPoint(Vector3 position, Texture2D texture)
        {
            return AddPoint(position.X, position.Y, position.Z, texture);
        }

        public Sprite AddPoint(float x, float y, float z, Texture2D texture)
        {
            Sprite newSprite = null;
            this.lastPlaced.X = x;
            this.lastPlaced.Y = y;
            this.lastPlaced.Z = z;

            if (this.mPoints.Count == 0)
            {
                this.mPoints.Add(new Vector3(x, y, z));
            }
            else
            {
                newSprite = CreateSprite(texture);
                newSprite.Z = z;
                this.mPoints.Add(new Vector3(x, y, z));

                this.UpdateLink(this.mPoints.Count - 3);
                this.UpdateLink(this.mPoints.Count - 2);
                if (this.stretchTexture)
                {
                    mSpriteList[0].Vertices[0].TextureCoordinate.Y = this.stretchTv;
                    mSpriteList[0].Vertices[1].TextureCoordinate.Y = this.stretchTv;
                }

                if (this.mPoints.Count > 1)
                {
                    if (this.stretchTexture)
                    {
                        newSprite.Vertices[2].TextureCoordinate.Y = this.stretchTv;
                        newSprite.Vertices[3].TextureCoordinate.Y = this.stretchTv;
                        (mSpriteList[this.mPoints.Count - 3]).Vertices[0].TextureCoordinate.Y = this.stretchTv;
                        (mSpriteList[this.mPoints.Count - 3]).Vertices[1].TextureCoordinate.Y = this.stretchTv;
                        if (!this.headEndVisible)
                        {
                            (mSpriteList[this.mPoints.Count - 3]).Visible = true;
                        }

                        SetConnectionTextureY(this.mPoints.Count - 1, this.headTv);
                    }
                }
            }
            return newSprite;
        }

        public void Destroy()
        {
            SpriteManager.RemoveSpriteList(mSpriteList);

            mPoints.Clear();
        }

        public List<SpriteVertex> GetConnection(int i)
        {
            List<SpriteVertex> spriteVertexList = new List<SpriteVertex>();

            if (i > 0)
            {
                spriteVertexList.Add((mSpriteList[i - 1]).Vertices[0]);
                spriteVertexList.Add((mSpriteList[i - 1]).Vertices[1]);
            }
            if (i < mSpriteList.Count)
            {
                spriteVertexList.Add(mSpriteList[i].Vertices[2]);
                spriteVertexList.Add(mSpriteList[i].Vertices[3]);
            }
            return spriteVertexList;
        }

        public void Manage()
        {
            if ((this.mPoints.Count >= 1) && (this.head != null))
            {
                Vector3 vector;
                if ((this.head != null) && (this.mPoints.Count > 1))
                {
                    vector = (Vector3)this.mPoints[this.mPoints.Count - 2];
                }
                else
                {
                    vector = (Vector3)this.mPoints[this.mPoints.Count - 1];
                }
                if ((TimeManager.SecondsSince(this.lastPlacement) > this.placementFrequency) && (new Vector3((this.head.X + this.xOffset) - this.lastPlaced.X, (this.head.Y + this.yOffset) - this.lastPlaced.Y, 0f).Length() > this.minimumPlacementDistance))
                {
                    double num = 0.0;
                    Sprite head = this.head as Sprite;
                    if ((head != null) && head.KeepTrackOfReal)
                    {
                        num = System.Math.Atan2((double)head.RealVelocity.Y, (double)head.RealVelocity.X);
                    }
                    else
                    {
                        num = System.Math.Atan2((double)this.head.RealVelocity.Y, (double)this.head.RealVelocity.X);
                    }
                    float xOffset = this.xOffset;
                    float yOffset = this.yOffset;

                    Math.MathFunctions.RotatePointAroundPoint(
                        0,
                        0,
                        ref xOffset,
                        ref yOffset,
                        (float)num);

                    vector.X = this.head.X + xOffset;
                    vector.Y = this.head.Y + yOffset;
                    vector.Z = this.head.Z;
                    if (this.mPoints.Count > 1)
                    {
                        this.mPoints[this.mPoints.Count - 1] = this.lastPlaced;
                    }
                    this.AddPoint(this.head.X, this.head.Y, this.head.Z, this.texture);
                    this.lastPlacement = TimeManager.CurrentTime;
                }
                if ((this.head != null) && (this.mPoints.Count > 1))
                {
                    vector.X = this.head.X;
                    vector.Y = this.head.Y;
                    vector.Z = this.head.Z;
                    this.mPoints[this.mPoints.Count - 1] = vector;
                    this.UpdateLink(this.mPoints.Count - 2);
                }
            }
        }

        public void SetPointAtIndex(int index, float x, float y, float z)
        {

            mPoints[index] = new Vector3(x, y, z);

            // June 24, 2011
            // Not sure why but
            // the first call to
            // UpdateLink (which passes
            // index) was not here before.
            // It doesn't seem to work without
            // it so I'm making that change now.
            // Adding this seems to fix an issue
            // discovered while writing a tutorial
            // for SetPointAtIndex.  Will return here
            // if there are problems later.
            UpdateLink(index);
            UpdateLink(index - 1);
            UpdateLink(index - 2);
        }


        #endregion


        private void SetPoints(IList<Vector3> points)
        {
            int desiredSpriteCount = points.Count - 1;

            while (desiredSpriteCount > mSpriteList.Count)
            {
                CreateSprite(texture);
            }
            while (mSpriteList.Count > 0 && desiredSpriteCount < mSpriteList.Count)
            {
                SpriteManager.RemoveSprite(mSpriteList.Last);
            }

            mPoints.Clear();
            for (int i = 0; i < points.Count; i++)
            {
                mPoints.Add(points[i]);
            }

            for (int i = 0; i < mSpriteList.Count; i++)
            {
                UpdateLink(i);
            }
        }

        internal void RemoveSprite(Sprite spriteToRemove)
        {
            this.mPoints.RemoveAt(mSpriteList.IndexOf(spriteToRemove));
        }

        private Sprite CreateSprite(Texture2D texture)
        {
            Sprite returnSprite = null;

            if (Layer == null)
            {
                if (CreatesParticleSprite)
                {
                    returnSprite = SpriteManager.AddParticleSprite(texture);
                }
                else
                {
                    returnSprite = SpriteManager.AddSprite(texture);
                }
            }
            else
            {
                if (CreatesParticleSprite)
                {
                    returnSprite = SpriteManager.AddParticleSprite(texture);
                    SpriteManager.AddToLayer(returnSprite, Layer);
                }
                else
                {
                    returnSprite = SpriteManager.AddSprite(texture, Layer);
                }
            }

            if (CreatesManuallyUpdatedSprites)
            {
                SpriteManager.ConvertToManuallyUpdated(returnSprite);
            }

            returnSprite.ColorOperation = mColorOperation;
            returnSprite.BlendOperation = mBlendOperation;

            returnSprite.Red = mRed;
            returnSprite.Green = mGreen;
            returnSprite.Blue = mBlue;

            returnSprite.Alpha = mAlpha;

            returnSprite.Visible = Visible;

            mSpriteList.Add(returnSprite);

            return returnSprite;
        }

        public void RemoveSpriteAtHead()
        {
            this.mPoints.RemoveAt(this.mPoints.Count - 1);
            SpriteManager.RemoveSprite(mSpriteList.Last);
        }

        public void SetConnectionTextureY(int index, float textureY)
        {
            if (index > 0)
            {
                mSpriteList[index - 1].Vertices[0].TextureCoordinate.Y = textureY;
                mSpriteList[index - 1].Vertices[1].TextureCoordinate.Y = textureY;
            }
            if (index < mSpriteList.Count)
            {
                mSpriteList[index].Vertices[2].TextureCoordinate.Y = textureY;
                mSpriteList[index].Vertices[3].TextureCoordinate.Y = textureY;
            }

        }

        public void SetHead(PositionedObject headPO, Texture2D texture, float placementFrequency, float minimumPlacementDistance)
        {
            this.head = headPO;
            this.placementFrequency = placementFrequency;
            this.minimumPlacementDistance = minimumPlacementDistance;
            this.minimumPlacementDistanceSquared = minimumPlacementDistance * minimumPlacementDistance;
            this.texture = texture;
            if (this.mPoints.Count < 2)
            {
                this.AddPoint(this.head.X, this.head.Y, this.head.Z, texture);
                this.lastPlacement = TimeManager.CurrentTime;
            }
        }

        Segment GetHorizontalSegmentAt(int index)
        {
            Vector3 pointBefore = this.mPoints[index];
            Vector3 pointAfter = this.mPoints[index + 1];
            Segment connectingSegment = new Segment(pointBefore, pointAfter);

            return connectingSegment;
        }

        void GetSegmentsAboveAndBelow(int index, out Segment aboveSegment, out Segment belowSegment)
        {
            Segment segment = GetHorizontalSegmentAt(index);

            segment.ScaleBy(2); // so they intersect

            Vector3 normalVector = segment.Point2.ToVector3() - segment.Point1.ToVector3();
            normalVector = new Vector3(-normalVector.Y, normalVector.X, 0);
            normalVector.Normalize();
            normalVector = normalVector * Width / 2.0f;

            aboveSegment = segment;
            aboveSegment.MoveBy(normalVector.X, normalVector.Y);

            belowSegment = segment;
            belowSegment.MoveBy(-normalVector.X, -normalVector.Y);
        }

        private void UpdateLink(int num)
        {


            if (((num >= 0) && (num <= (mSpriteList.Count - 1))) && (this.mPoints.Count >= 2))
            {
                Vector3 absoluteTopLeft = new Vector3();
                Vector3 absoluteTopRight = new Vector3();
                Vector3 absoluteBottomLeft = new Vector3();
                Vector3 absoluteBottomRight = new Vector3();

                Sprite sprite = mSpriteList[num];

                Segment horizontalConnectingSegment = GetHorizontalSegmentAt(num);

                float oldZ = sprite.Z;

                sprite.Position = (horizontalConnectingSegment.Point1.ToVector3() + horizontalConnectingSegment.Point2.ToVector3()) / 2.0f;

                sprite.Z = oldZ;

                horizontalConnectingSegment.ScaleBy(2);

                Vector3 normalVector = horizontalConnectingSegment.Point2.ToVector3() - horizontalConnectingSegment.Point1.ToVector3();
                normalVector = new Vector3(-normalVector.Y, normalVector.X, 0);
                normalVector.Normalize();
                normalVector *= Width / 2.0f;

                #region Get the left side

                if (num == 0)
                {

                    absoluteTopLeft = mPoints[num] + normalVector;
                    absoluteBottomLeft = mPoints[num] - normalVector;

                }
                else
                {
                    Segment thisAboveSegment = horizontalConnectingSegment;
                    thisAboveSegment.MoveBy(normalVector.X, normalVector.Y);
                    thisAboveSegment.ScaleBy(10);

                    Segment thisBelowSegment = horizontalConnectingSegment;
                    thisBelowSegment.MoveBy(-normalVector.X, -normalVector.Y);
                    thisBelowSegment.ScaleBy(10);

                    Segment leftAboveSegment;
                    Segment leftBelowSegment;

                    GetSegmentsAboveAndBelow(num - 1, out leftAboveSegment, out leftBelowSegment);
                    leftAboveSegment.ScaleBy(10);
                    leftBelowSegment.ScaleBy(10);

                    Point point;

                    thisAboveSegment.IntersectionPoint(ref leftAboveSegment, out point);
                    if (double.IsNaN(point.X) || double.IsNaN(point.Y))
                    {
                        absoluteTopLeft = mPoints[num] + normalVector;
                    }
                    else
                    {
                        absoluteTopLeft.X = (float)point.X;
                        absoluteTopLeft.Y = (float)point.Y;
                    }

                    thisBelowSegment.IntersectionPoint(ref leftBelowSegment, out point);


                    if (double.IsNaN(point.X) || double.IsNaN(point.Y))
                    {
                        absoluteBottomLeft = mPoints[num] - normalVector;
                    }
                    else
                    {
                        absoluteBottomLeft.X = (float)point.X;
                        absoluteBottomLeft.Y = (float)point.Y;
                    }
                }
                #endregion

                if (num + 1 == mPoints.Count - 1)
                {
                    absoluteTopRight = mPoints[num + 1] + normalVector;
                    absoluteBottomRight = mPoints[num + 1] - normalVector;
                }
                else
                {
                    Segment thisAboveSegment = horizontalConnectingSegment;
                    thisAboveSegment.MoveBy(normalVector.X, normalVector.Y);
                    thisAboveSegment.ScaleBy(10);

                    Segment thisBelowSegment = horizontalConnectingSegment;
                    thisBelowSegment.MoveBy(-normalVector.X, -normalVector.Y);
                    thisBelowSegment.ScaleBy(10);

                    Segment rightAboveSegment;
                    Segment rightBelowSegment;

                    GetSegmentsAboveAndBelow(num + 1, out rightAboveSegment, out rightBelowSegment);

                    rightAboveSegment.ScaleBy(10);
                    rightBelowSegment.ScaleBy(10);

                    Point point;
                    thisAboveSegment.IntersectionPoint(ref rightAboveSegment, out point);

                    if (double.IsNaN(point.X) || double.IsNaN(point.Y))
                    {
                        // this likely occurs because the slope for the endpoints of this Sprite and the slope
                        // for the endpoints of the next Sprite are equal, so parallel segments never intersect
                        absoluteTopRight = mPoints[num + 1] + normalVector;
                    }
                    else
                    {

                        absoluteTopRight.X = (float)point.X;
                        absoluteTopRight.Y = (float)point.Y;
                    }

                    thisBelowSegment.IntersectionPoint(ref rightBelowSegment, out point);


                    if (double.IsNaN(point.X) || double.IsNaN(point.Y))
                    {
                        // this likely occurs because the slope for the endpoints of this Sprite and the slope
                        // for the endpoints of the next Sprite are equal, so parallel segments never intersect
                        absoluteBottomRight = mPoints[num + 1] - normalVector;
                    }
                    else
                    {
                        absoluteBottomRight.X = (float)point.X;
                        absoluteBottomRight.Y = (float)point.Y;
                    }

                }

                Vector3 tempVector = absoluteTopLeft - sprite.Position;
                sprite.Vertices[0].Scale = new Microsoft.Xna.Framework.Vector2(tempVector.X, tempVector.Y);

                tempVector = absoluteTopRight - sprite.Position;
                sprite.Vertices[1].Scale = new Microsoft.Xna.Framework.Vector2(tempVector.X, tempVector.Y);

                tempVector = absoluteBottomRight - sprite.Position;
                sprite.Vertices[2].Scale = new Microsoft.Xna.Framework.Vector2(tempVector.X, tempVector.Y);

                tempVector = absoluteBottomLeft - sprite.Position;
                sprite.Vertices[3].Scale = new Microsoft.Xna.Framework.Vector2(tempVector.X, tempVector.Y);


                SpriteManager.ManualUpdate(sprite);
                /*
                Vector3 vector;
                Vector3 vector2;
                Sprite sprite;
                if (num < 2)
                {
                    vector = (Vector3) this.points[0];
                    vector2 = (Vector3) this.points[1];
                    sprite = mSpriteList[0];
                    Vector3 vector3 = new Vector3(vector2.X - vector.X, vector2.Y - vector.Y, 0f);
                    Matrix sourceMatrix = Matrix.CreateRotationZ(-1.570796f);

                    vector3 = Vector3.Transform(vector3, sourceMatrix);

                    vector3.Normalize();
                    vector3 = (Vector3) (vector3 * this.Width);
                    if (vector3.Length() != 0f)
                    {
                        sprite.Vertices[0].Position = vector2 - vector3;
                        sprite.Vertices[1].Position = vector2 + vector3;
                        sprite.Vertices[2].Position = vector + vector3;
                        sprite.Vertices[3].Position = vector - vector3;
                    }
                    else
                    {
                        sprite.Vertices[0].Position = vector;
                        sprite.Vertices[1].Position = vector;
                        sprite.Vertices[2].Position = vector;
                        sprite.Vertices[3].Position = vector;
                    }
                }
                else if (num == (this.points.Count - 1))
                {
                    Point point;
                    Point point2;
                    vector = (Vector3) this.points[num - 2];
                    vector2 = (Vector3) this.points[num - 1];
                    Vector3 vector4 = (Vector3) this.points[num];
                    sprite = mSpriteList[num - 1];
                    Sprite sprite2 = mSpriteList[num - 2];
                    Vector3 vector5 = vector - vector2;
                    Vector3 vector6 = vector4 - vector2;
                    double num2 = System.Math.Atan2((double) vector6.Y, (double) vector6.X);
                    double num3 = System.Math.Atan2((double) vector5.Y, (double) vector5.X);
                    double d = (num2 + num3) / 2.0;
                    Vector3 vector7 = new Vector3((float) System.Math.Cos(d), (float) System.Math.Sin(d), 0f);

                    vector5 = FlatRedBall.Math.MathFunctions.TransformVector(vector5,  Matrix.CreateRotationZ(1.570796f));

                    vector6 = FlatRedBall.Math.MathFunctions.TransformVector(vector6,  Matrix.CreateRotationZ(-1.570796f));

                    vector5.Normalize();
                    vector5 = (Vector3) (vector5 * this.Width);
                    vector6.Normalize();
                    vector6 = (Vector3) (vector6 * this.Width);
                    vector7.Normalize();
                    if (num3 > num2)
                    {
                        vector7 = (Vector3) (vector7 * -1f);
                    }
                    vector7 = (Vector3) (vector7 * this.Width);
                    Segment line = new Segment(
                        new Point((double) (vector.X + vector5.X), (double) (vector.Y + vector5.Y)), new Point((double) (vector2.X + vector5.X), (double) (vector2.Y + vector5.Y)));
                    Segment line2 = new Segment(
                        new Point((double) (vector.X - vector5.X), (double) (vector.Y - vector5.Y)), new Point((double) (vector2.X - vector5.X), (double) (vector2.Y - vector5.Y)));
                    Segment line3 = new Segment(
                        new Point((double) (vector2.X + vector6.X), (double) (vector2.Y + vector6.Y)), new Point((double) (vector4.X + vector6.X), (double) (vector4.Y + vector6.Y)));
                    Segment line4 = new Segment(
                        new Point((double) (vector2.X - vector6.X), (double) (vector2.Y - vector6.Y)), new Point((double) (vector4.X - vector6.X), (double) (vector4.Y - vector6.Y)));


                    if (System.Math.Abs((double)(System.Math.Abs((double)(num2 - num3)) - 3.1415926535897931)) < 0.0010000000474974513)
                    {
                        point = new Point((double) (vector2.X + vector7.X), (double) (vector2.Y + vector7.Y));
                        point2 = new Point((double) (vector2.X - vector7.X), (double) (vector2.Y - vector7.Y));
                    }
                    else
                    {
                        line.IntersectionPoint(ref line3, out point);
                        line2.IntersectionPoint(ref line4, out point2);
                    }
                    Vector3 vector8 = new Vector3(vector4.X - vector6.X, vector4.Y - vector6.Y, 0f);
                    Vector3 vector9 = new Vector3(vector4.X + vector6.X, vector4.Y + vector6.Y, 0f);
                    sprite.Vertices[0].Position = vector8;
                    sprite.Vertices[1].Position = vector9;
                    sprite.Vertices[2].Position.X = (float)point.X;
                    sprite.Vertices[2].Position.Y = (float)point.Y;
                    sprite.Vertices[3].Position.X = (float)point2.X;
                    sprite.Vertices[3].Position.Y = (float)point2.Y;
                    sprite2.Vertices[0].Position = sprite.Vertices[3].Position;
                    sprite2.Vertices[1].Position = sprite.Vertices[2].Position;
                }
                 * 
                 */
            }
        }

        #endregion
    }
}