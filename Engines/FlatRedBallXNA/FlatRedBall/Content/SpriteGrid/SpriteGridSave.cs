using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.Xml.Serialization;

using FlatRedBall.ManagedSpriteGroups;

using FlatRedBall.Content.Scene;
using FlatRedBall.Attributes;

using FlatRedBall.IO;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework.Graphics;
using FlatRedBall.Graphics.Texture;

namespace FlatRedBall.Content.SpriteGrid
{
    #region ReferencedAnimationChain struct

    public struct ReferencedAnimationChain
    {
        public string AnimationChainListFileName;
        public string AnimationChainName;

        public static ReferencedAnimationChain FromAnimationChain(FlatRedBall.Graphics.Animation.AnimationChain animationChain)
        {
            ReferencedAnimationChain rac = new ReferencedAnimationChain();
            // Vic says:  This used to set the AnimationChainListFileName as a relative
            // name, but that shouldn't be done until the file is saved.
            rac.AnimationChainListFileName = animationChain.ParentAchxFileName;
            rac.AnimationChainName = animationChain.Name;

            return rac;
        }
    }

    #endregion


    public class SpriteGridSave : SpriteGridSaveBase<SpriteSave>
    {
        #region Fields

#if FRB_XNA && !MONODROID
        [ExternalInstance]
        [XmlIgnore]
        public Texture2D mTextureInstance;
        [ExternalInstanceList]
        [XmlIgnore]
        public Texture2D[][] mTextureGridInstance;
#endif
        #endregion

        #region Properties

        [XmlIgnore]
        public float XLeftFilledBound
        {
            get
            {
                return FlatRedBall.ManagedSpriteGroups.SpriteGrid.GetLeftFilledBound(
                    XLeftBound, 
                    GridSpacing, 
                    Blueprint.ScaleX, 
                    Blueprint.X);
            }
        }

        [XmlIgnore]
        public float XRightFilledBound
        {
            get
            {
                return FlatRedBall.ManagedSpriteGroups.SpriteGrid.GetRightFilledBound(
                    XRightBound, 
                    GridSpacing, 
                    Blueprint.ScaleX, 
                    Blueprint.X);
            }
        }

        [XmlIgnore]
        public float YTopFilledBound
        {
            get
            {
                return FlatRedBall.ManagedSpriteGroups.SpriteGrid.GetTopFilledBound(
                    YTopBound, GridSpacing, Blueprint.ScaleY, Blueprint.Y);
            }
        }

        [XmlIgnore]
        public float YBottomFilledBound
        {
            get
            {
                return FlatRedBall.ManagedSpriteGroups.SpriteGrid.GetBottomFilledBound(
                    YBottomBound, GridSpacing, Blueprint.ScaleY, Blueprint.Y);
            }
        }

        /*
        [XmlIgnore]
        public List<string> ReferencedTextures
        {
            get
            {
                List<string> textures = new List<string>();

                foreach (StringArray stringArray in GridTextures)
                    foreach (string s in stringArray)
                        textures.Add(s);

                return textures;
            }
        }
        */

        #endregion

        #region Methods

        #region Constructor

        public SpriteGridSave()
            : base()
        { }

        #endregion

        #region Public Methods

        public FlatRedBall.ManagedSpriteGroups.SpriteGrid ToSpriteGrid(Camera camera, string contentManagerName) 
           // where SpriteGridType : FlatRedBall.ManagedSpriteGroups.SpriteGrid, new()
        {

            var plane = GetPlaneFromAxis();

            // Since the TextureGrid is passed as an argument in the constructor of the SpriteGrid, it
            // needs to be created before the SpriteGrid is initialized

            TextureGrid<Texture2D> textureGrid = CreateTextureGrid(contentManagerName);
            
            var spriteGrid = new FlatRedBall.ManagedSpriteGroups.SpriteGrid(
                camera, plane, this.Blueprint.ToSprite(contentManagerName), textureGrid);

            CreateAnimationChainGrid(contentManagerName, spriteGrid);

            CreateDisplayRegionGrid(spriteGrid);

            spriteGrid.Name = this.Name;
            
            SetBoundsValues(spriteGrid);

            spriteGrid.GridSpacing = GridSpacing;

            SetOrderingMode(spriteGrid);

            spriteGrid.CreatesAutomaticallyUpdatedSprites = CreatesAutomaticallyUpdatedSprites;
            spriteGrid.CreatesParticleSprites = CreatesParticleSprites;
            //spriteGrid.DrawDefaultTileNoPopulate = DrawDefaultTile;  

            return spriteGrid;
            // do we need this line?
            // lastBlueprintPosition = Blueprint.ToVector3();
        }

        private void SetOrderingMode(FlatRedBall.ManagedSpriteGroups.SpriteGrid spriteGrid)
        {
#if FRB_MDX
            spriteGrid.Blueprint.mOrdered = this.Blueprint.Ordered;
#endif
            if (this.Blueprint.Ordered)
            {
                spriteGrid.OrderingMode = FlatRedBall.Graphics.OrderingMode.DistanceFromCamera;
            }
            else
            {
                spriteGrid.OrderingMode = FlatRedBall.Graphics.OrderingMode.ZBuffered;
            }
        }

        private void SetBoundsValues(FlatRedBall.ManagedSpriteGroups.SpriteGrid spriteGrid)
        {
            spriteGrid.XLeftBound = XLeftBound;
            spriteGrid.XRightBound = XRightBound;
            spriteGrid.YTopBound = YTopBound;
            spriteGrid.YBottomBound = YBottomBound;
            spriteGrid.ZCloseBound = ZCloseBound;
            spriteGrid.ZFarBound = ZFarBound;
        }

        private void CreateDisplayRegionGrid(FlatRedBall.ManagedSpriteGroups.SpriteGrid spriteGrid)
        {
            #region Create the DisplayRegionGrid

            if (DisplayRegionGridSave != null)
            {

                TextureGrid<FloatRectangle?> displayRegionGrid =
                    DisplayRegionGridSave.ToDisplayRegionGrid(GridSpacing);
                if (displayRegionGrid.BaseTexture == null)
                {
                    displayRegionGrid.BaseTexture = new FloatRectangle(
                        spriteGrid.Blueprint.TopTextureCoordinate,
                        spriteGrid.Blueprint.BottomTextureCoordinate,
                        spriteGrid.Blueprint.LeftTextureCoordinate,
                        spriteGrid.Blueprint.RightTextureCoordinate);
                }
                spriteGrid.DisplayRegionGrid = displayRegionGrid;
            }
            #endregion
        }

        private void CreateAnimationChainGrid(string contentManagerName, FlatRedBall.ManagedSpriteGroups.SpriteGrid spriteGrid)
        {
            #region Create the AnimationChainGrid
            if (AnimationChainGridSave != null)
            {
                TextureGrid<FlatRedBall.Graphics.Animation.AnimationChain> animationChainGrid =
                    AnimationChainGridSave.ToAnimationChainGrid(contentManagerName, GridSpacing);

                spriteGrid.AnimationChainGrid = animationChainGrid;
            }
            #endregion
        }

        private TextureGrid<Texture2D> CreateTextureGrid(string contentManagerName)
        {
            #region Create the TextureGrid

            TextureGrid<Texture2D> textureGrid = new TextureGrid<Texture2D>();


            textureGrid.FirstPaintedX = FirstPaintedX;

            textureGrid.LastPaintedX = new List<float>(); // this is set when the textures are copied over

            textureGrid.FirstPaintedY = FirstPaintedY;
            if (GridTexturesArray != null) // should never be null
                textureGrid.LastPaintedY = textureGrid.FirstPaintedY + (this.GridTexturesArray.Length - 1) * GridSpacing;
            else
                throw new NullReferenceException("GridTexturesArray in the SpriteGridSave is null.  Cannot create SpriteGrid.");

            textureGrid.GridSpacingX = GridSpacing;
            textureGrid.GridSpacingY = GridSpacing;

            int yOn = 0;

#if FRB_XNA && !MONODROID
            #region Set the base texture instance

            if (this.mTextureInstance != null)
            {
                textureGrid.BaseTexture = this.mTextureInstance;
            }
            else if (!string.IsNullOrEmpty(this.BaseTexture))
            {
                textureGrid.BaseTexture = FlatRedBallServices.Load<Texture2D>(this.BaseTexture, contentManagerName);
            }
            #endregion


            if (this.mTextureGridInstance != null)
            {
                foreach (Texture2D[] ta in this.mTextureGridInstance)
                {
                    List<Texture2D> textureList = new List<Texture2D>(ta);
                    textureGrid.Textures.Add(textureList);


                    textureGrid.LastPaintedX.Add(textureGrid.FirstPaintedX[yOn] + GridSpacing * (ta.Length - 1));

                    yOn++;
                }
            }
            else
#endif
            {

                foreach (string[] sa in this.GridTexturesArray)
                {
                    List<Texture2D> textureList = new List<Texture2D>();
                    textureGrid.Textures.Add(textureList);

                    textureGrid.LastPaintedX.Add(textureGrid.FirstPaintedX[yOn] + GridSpacing * (sa.Length - 1));

                    foreach (string s in sa)
                    {
                        if (!string.IsNullOrEmpty(s))
                        {
                            Texture2D textureToAdd = FlatRedBallServices.Load<Texture2D>(s, contentManagerName);

                            textureList.Add(textureToAdd);
                        }
                        else
                        {
                            textureList.Add(null);

                        }
                    }

                    yOn++;
                }
            }
            #endregion
            return textureGrid;
        }

        private FlatRedBall.ManagedSpriteGroups.SpriteGrid.Plane GetPlaneFromAxis()
        {
            FlatRedBall.ManagedSpriteGroups.SpriteGrid.Plane plane;

            if (this.Axis == 'y')
                // for now we just use an XY plane - need to add support for XZ and eventually YZ planes
                plane = FlatRedBall.ManagedSpriteGroups.SpriteGrid.Plane.XY;
            else
                plane = FlatRedBall.ManagedSpriteGroups.SpriteGrid.Plane.XZ;
            return plane;
        }

        public static SpriteGridSave FromSpriteGrid(FlatRedBall.ManagedSpriteGroups.SpriteGrid spriteGrid)
        {
            SpriteGridSave spriteGridSave = new SpriteGridSave();

            if(spriteGrid.Name != null)
                spriteGridSave.Name = spriteGrid.Name;

            spriteGridSave.Blueprint = SpriteSave.FromSprite(spriteGrid.Blueprint);

            #region Set the GridPlane (Axis in SpriteSave)

            if (spriteGrid.GridPlane == FlatRedBall.ManagedSpriteGroups.SpriteGrid.Plane.XY)
            {
                spriteGridSave.Axis = 'y';
            }
            else
            {
                spriteGridSave.Axis = 'z';
            }

            #endregion

            // Create new lists here instead of just assigning them (like we used to do) because
            // the SpriteGridSave will make modifications to these to make file sizes smaller.  If
            // we don't make new lists, then the SpriteGridSave will make modifications to the original
            // SpriteGrid which causes problems.
            spriteGridSave.FirstPaintedX = new List<float>(spriteGrid.TextureGrid.FirstPaintedX);
            spriteGridSave.FirstPaintedY = spriteGrid.TextureGrid.FirstPaintedY;



            spriteGridSave.GridSpacing = spriteGrid.GridSpacing;

            // Right now there's no sprite type.  Should there be Ordered/Unordered?

            #region set the bounds
            spriteGridSave.XRightBound = spriteGrid.XRightBound;
            spriteGridSave.XLeftBound = spriteGrid.XLeftBound;
            spriteGridSave.YTopBound = spriteGrid.YTopBound;
            spriteGridSave.YBottomBound = spriteGrid.YBottomBound;
            spriteGridSave.ZCloseBound = spriteGrid.ZCloseBound;
            spriteGridSave.ZFarBound = spriteGrid.ZFarBound;
            #endregion


            #region fill up the strings for the FRBTextures

            if (spriteGrid.TextureGrid.BaseTexture != null)
            {
                spriteGridSave.BaseTexture = spriteGrid.TextureGrid.BaseTexture.SourceFile();
            }
            else
            {
                spriteGridSave.BaseTexture = "";

            }

            #region Chop off the bottom (or near in XZ)

            int numberToChopOffBottom = 0;

            float yValue = spriteGrid.TextureGrid.FirstPaintedY;
            float bottomBound = spriteGrid.YBottomBound;
            if (spriteGrid.GridPlane == FlatRedBall.ManagedSpriteGroups.SpriteGrid.Plane.XZ)
            {
                bottomBound = spriteGrid.ZCloseBound;
            }

            // This will usually include one extra row beyond the bounds, but that's ok because
            // it's possible that someone creates a really weird grid with overlapping Sprites.
            while (yValue + spriteGrid.GridSpacing/2.0f < bottomBound)
            {
                numberToChopOffBottom++;
                yValue += spriteGrid.GridSpacing;
                spriteGridSave.FirstPaintedY += spriteGrid.GridSpacing;

                if (spriteGridSave.FirstPaintedX.Count == 0)
                {
                    // we're done here, so exit out
                    break;
                }
                else
                {
                    spriteGridSave.FirstPaintedX.RemoveAt(0);
                }
            }
            #endregion

            #region Chop off the top (or far in XZ)

            int numberToChopOffTop = 0;
            yValue = spriteGrid.TextureGrid.FirstPaintedY + spriteGrid.TextureGrid.FirstPaintedX.Count * spriteGrid.GridSpacing;
            float topBound = spriteGrid.YTopBound;
            if (spriteGrid.GridPlane == FlatRedBall.ManagedSpriteGroups.SpriteGrid.Plane.XZ)
            {
                topBound = spriteGrid.ZFarBound;
            }

            while (yValue - spriteGrid.GridSpacing / 2.0f > topBound)
            {
                numberToChopOffTop++;
                yValue -= spriteGrid.GridSpacing;

                if (spriteGridSave.FirstPaintedX.Count == 0)
                {
                    break;
                }
                else
                {
                    spriteGridSave.FirstPaintedX.RemoveAt(spriteGridSave.FirstPaintedX.Count - 1);
                }
            }

            #endregion

            int numberOfGridTextures = spriteGrid.TextureGrid.Textures.Count - numberToChopOffBottom - numberToChopOffTop;
            numberOfGridTextures = System.Math.Max(numberOfGridTextures, 0); // don't let it be negative

            spriteGridSave.GridTexturesArray = 
                new string[numberOfGridTextures][];
            
            //GridTextures = new StringArrayArray();
            //GridTexturesArray = new string[spriteGrid.textureGrid.textures.Count][];

            TextureGrid<Texture2D> textureGrid = spriteGrid.TextureGrid;

            for (int i = numberToChopOffBottom; i < textureGrid.Textures.Count - numberToChopOffTop; i++)
            {

                int numberToChopOffLeft = 0;
                int numberToChopOffRight = 0;

                float xValue = textureGrid.FirstPaintedX[i];

                while (xValue + spriteGrid.GridSpacingX / 2.0f < spriteGrid.XLeftBound)
                {
                    xValue += spriteGrid.GridSpacingX;
                    numberToChopOffLeft++;
                    spriteGridSave.FirstPaintedX[i - numberToChopOffBottom] += spriteGrid.GridSpacingX;
                }

                xValue = textureGrid.LastPaintedX[i];

                while (xValue - spriteGrid.GridSpacingX / 2.0f > spriteGrid.XRightBound)
                {
                    xValue -= spriteGrid.GridSpacingX;
                    numberToChopOffRight++;
                }

                int numberOfTexturesInRow = textureGrid.Textures[i].Count - numberToChopOffLeft - numberToChopOffRight;
                numberOfTexturesInRow = System.Math.Max(0, numberOfTexturesInRow);

                spriteGridSave.GridTexturesArray[i - numberToChopOffBottom] =
                    new string[numberOfTexturesInRow];

                for (int j = numberToChopOffLeft; j < textureGrid.Textures[i].Count - numberToChopOffRight; j++)
                {
                    if (textureGrid.Textures[i][j] != null)
                    {
                        spriteGridSave.GridTexturesArray[i - numberToChopOffBottom][j - numberToChopOffLeft] =
                            textureGrid.Textures[i][j].SourceFile();
                    }
                    else
                    {
                        spriteGridSave.GridTexturesArray[i - numberToChopOffBottom][j - numberToChopOffLeft] = null;
                    }
                }
            }
            #endregion

            spriteGridSave.AnimationChainGridSave = AnimationChainGridSave.FromAnimationChainGrid(
                spriteGrid.AnimationChainGrid);

            spriteGridSave.DisplayRegionGridSave = DisplayRegionGridSave.FromDisplayRegionGrid(
                spriteGrid.DisplayRegionGrid);

#if FRB_XNA
            spriteGridSave.Blueprint.Ordered = spriteGrid.OrderingMode == FlatRedBall.Graphics.OrderingMode.DistanceFromCamera;
#else
            spriteGridSave.Blueprint.Ordered = spriteGrid.Blueprint.mOrdered;
#endif
            spriteGridSave.CreatesAutomaticallyUpdatedSprites = spriteGrid.CreatesAutomaticallyUpdatedSprites;
            spriteGridSave.CreatesParticleSprites = spriteGrid.CreatesParticleSprites;
            //spriteGridSave.DrawDefaultTile = spriteGrid.DrawDefaultTile;
            //spriteGridSave.DrawableBatch = spriteGrid.DrawableBatch;

            spriteGridSave.CropOutOfBoundsPaintedSprites();

            return spriteGridSave;
        }

        public void ReplaceTexture(string oldTexture, string newTexture)
        {
            if (this.BaseTexture == oldTexture)
            {
                this.BaseTexture = newTexture;
            }

            if (this.Blueprint.Texture == oldTexture)
            {
                this.Blueprint.Texture = newTexture;
            }

            for (int y = 0; y < GridTexturesArray.Length; y++)
            {
                for (int x = 0; x < GridTexturesArray[y].Length; x++)
                {
                    if (GridTexturesArray[y][x] == oldTexture)
                    {
                        GridTexturesArray[y][x] = newTexture;
                    }
                }
            }
        }

#if !FRB_MDX
        internal static SpriteGridSave FromXElement(System.Xml.Linq.XElement element)
        {
            SpriteGridSave sgs = new SpriteGridSave();

            foreach (var subElement in element.Elements())
            {
                switch (subElement.Name.LocalName)
                {
                    case "AnimationChainGridSave":
                        sgs.AnimationChainGridSave = AnimationChainGridSave.FromXElement(subElement);
                        break;
                    case "Axis":
                        sgs.Axis = SpriteEditorScene.AsChar(subElement);
                        break;
                    case "Blueprint":
                        sgs.Blueprint = SpriteSave.FromXElement(subElement);
                        break;
                    case "BaseTexture":
                        sgs.BaseTexture = (string)subElement.Value;
                        break;
                    case "CreatesAutomaticallyUpdatedSprites":
                        sgs.CreatesAutomaticallyUpdatedSprites = SpriteEditorScene.AsBool(subElement);
                        break;
                    case "CreatesParticleSprites":
                        sgs.CreatesParticleSprites = SpriteEditorScene.AsBool(subElement);
                        break;
                    case "DisplayRegionGridSave":
                        sgs.DisplayRegionGridSave = DisplayRegionGridSave.FromXElement(subElement);
                        break;
                    case "DrawableBatch":
                        sgs.DrawableBatch = SpriteEditorScene.AsBool(subElement);
                        break;
                    case "DrawDefaultTile":
                        sgs.DrawDefaultTile = SpriteEditorScene.AsBool(subElement);
                        break;
                    case "FirstPaintedX":
                        sgs.FirstPaintedX = SpriteEditorScene.AsFloatList(subElement);
                        break;
                    case "FirstPaintedY":
                        sgs.FirstPaintedY = SceneSave.AsFloat(subElement);
                        break;
                    case "GridSpacing":
                        sgs.GridSpacing = SceneSave.AsFloat(subElement);
                        break;
                    case "GridTexturesArray":
                        sgs.GridTexturesArray = SpriteEditorScene.AsStringArrayArray(subElement);
                        break;
                    case "Name":
                        sgs.Name = subElement.Value;
                        break;
                    case "OrderingMode":
                        sgs.OrderingMode = (Graphics.OrderingMode)Enum.Parse(typeof(Graphics.OrderingMode), subElement.Value, true );
                        break;
                    case "XLeftBound":
                        sgs.XLeftBound = SceneSave.AsFloat(subElement);
                        break;
                    case "XRightBound":
                        sgs.XRightBound = SceneSave.AsFloat(subElement);
                        break;
                    case "YBottomBound":
                        sgs.YBottomBound = SceneSave.AsFloat(subElement);
                        break;
                    case "YTopBound":
                        sgs.YTopBound = SceneSave.AsFloat(subElement);
                        break;
                    case "ZCloseBound":
                        sgs.ZCloseBound = SceneSave.AsFloat(subElement);
                        break;
                    case "ZFarBound":
                        sgs.ZFarBound = SceneSave.AsFloat(subElement);
                        break;
                    default:
                        throw new NotImplementedException("Node not understood: " + subElement.Name.LocalName);
                }

            }


            return sgs;
        }
#endif

        #endregion

        #region Private Methods

        private void CropOutOfBoundsPaintedSprites()
        {
 
        }

        #endregion

        #endregion
    }
}
