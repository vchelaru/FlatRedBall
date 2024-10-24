using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Math;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall
{
    public class SpriteList : PositionedObjectList<Sprite>, IEquatable<SpriteList>
    {
        #region Properties

        public float Alpha
        {
            set
            {
                for (int i = 0; i < this.Count; i++)
                {
                    Sprite sprite = this[i];

                    sprite.Alpha = value;
                }
            }
        }

        public bool Visible
        {
            set
            {
                for (int i = 0; i < this.Count; i++)
                {
                    Sprite sprite = this[i];

                    sprite.Visible = value;
                }
            }
        }
        #endregion

        #region Methods

        #region Constructors

        public SpriteList() : base() { }
        public SpriteList(int capacity) : base(capacity) { }

        #endregion

        #region Public Methods

        public SpriteList FindSpritesWithNameContaining(string stringToSearchFor)
        {
            SpriteList oneWayList = new SpriteList();

            for (int i = 0; i < this.Count; i++)
            {
                Sprite sprite = this[i];
                string name = sprite.Name;

                if (name.Contains(stringToSearchFor))
                    oneWayList.AddOneWay(sprite);
            }

            return oneWayList;
        }


        public Sprite FindSpriteWithTexture(Texture2D texture)
        {
            for (int i = 0; i < Count; i++)
            {
                Sprite sprite = this[i];
                if (sprite.Texture == texture)
                {
                    return sprite;
                }
            }
            return null;
        }

        #region XML Docs
        /// <summary>
        /// Returns a one-way SpriteList containing all Sprites in this SpriteList which reference the texture argument.
        /// </summary>
        /// <param name="texture">The texture to match against.</param>
        /// <returns>SpriteList containing Sprites with matching textures.</returns>
        #endregion
        public SpriteList FindSpritesWithTexture(Texture2D texture)
        {
            SpriteList spriteListToReturn = new SpriteList();

            for (int i = 0; i < Count; i++)
            {
                Sprite sprite = this[i];
                if (sprite.Texture == texture)
                {
                    spriteListToReturn.AddOneWay(sprite);
                }
            }
            return spriteListToReturn;
        }


        public Sprite FindUnrotatedSpriteAt(float x, float y)
        {
            for (int i = 0; i < Count; i++)
            {
                if (x > (this[i]).Position.X - (this[i]).ScaleX && x < (this[i]).Position.X + (this[i]).ScaleX &&
                    y > (this[i]).Position.Y - (this[i]).ScaleY && y < (this[i]).Position.Y + (this[i]).ScaleY)
                {
                    return this[i];
                }
            }
            return null;
        }


        public int GetNumberOfSpritesInCameraView(int startIndex, int range)
        {
            int numberToReturn = 0;
            for (int i = startIndex; i < startIndex + range; i++)
            {
                Sprite sprite = this[i];
                if (sprite.mInCameraView)
                    numberToReturn++;
            }

            return numberToReturn;
        }


        public List<int> GetTextureBreaks()
        {
            List<int> textureBreaks = new List<int>();

            if (Count == 0 || Count == 1)
                return textureBreaks;

            for (int i = 1; i < Count; i++)
            {
                Sprite sprite = this[i];
                Sprite lastSprite = this[i - 1];

                if (sprite.Texture != lastSprite.Texture)
                    textureBreaks.Add(i);
            }
            return textureBreaks;
        }


        #region Sorting and ordering

        List<List<Sprite>> sSpriteListList = new List<List<Sprite>>();


        public void SortTextureInsertion()
        {
            sSortTextureDictionary.Clear();

            foreach (List<Sprite> spriteList in sSpriteListList)
            {
                spriteList.Clear();
            }

            int textureID = 1; // start at 1, null is 0

            for (int i = 0; i < this.mInternalList.Count; i++)
            {
                int idToAddAt = 0;

                if (mInternalList[i].Texture != null)
                {
                    if (!sSortTextureDictionary.ContainsKey(mInternalList[i].Texture))
                    {
                        sSortTextureDictionary.Add(mInternalList[i].Texture, textureID);
                        idToAddAt = textureID;
                        textureID++;
                    }
                    else
                    {
                        idToAddAt = sSortTextureDictionary[mInternalList[i].Texture];
                    }
                }

                while (sSpriteListList.Count <= idToAddAt)
                {
                    sSpriteListList.Add(new List<Sprite>());
                }

                sSpriteListList[idToAddAt].Add(mInternalList[i]);
            }

            // Now we can clear the mInternalList and add them according to what's in the list
			
            mInternalList.Clear();
            foreach (List<Sprite> spriteList in sSpriteListList)
            {
				if ( spriteList.Count != 0 )
					for ( int i = 0; i < spriteList.Count; i++ )
						mInternalList.Add(spriteList[i]);
					//mInternalList.AddRange(spriteList);
            }
        }

        static Dictionary<Texture2D, int> sSortTextureDictionary = new Dictionary<Texture2D, int>(500);

        #region XML Docs
        /// <summary>
        /// Sorts a sub-array of the SpriteArray by their Texture.
        /// </summary>
        /// <param name="firstSprite">Index of the first Sprite, inclusive.</param>
        /// <param name="lastSpriteExclusive">Index of the last Sprite, exclusive.</param>
        #endregion
        public void SortTextureInsertion(int firstSprite, int lastSpriteExclusive)
        {
            int nextKey = 0;

            int i = 0;
            sSortTextureDictionary.Clear();

            try
            {            
                for (i = firstSprite; i < lastSpriteExclusive; i++)
                {
                    Sprite sprite = this[i];
                    if (sprite.Texture != null)
                    {
                        if (sSortTextureDictionary.ContainsKey(sprite.Texture) == false)
                        {
                            sSortTextureDictionary.Add(sprite.Texture, nextKey);
                            nextKey++;
                        }
                    }
                }
            }
            catch(ArgumentNullException e)
            {
                throw new NullReferenceException("Texture sorting failed due to Sprite " + i + " having a null Texture", e);
            }

            int numSorting = lastSpriteExclusive - firstSprite;
            // Biggest first
            if (numSorting == 1 || numSorting == 0)
                return;

            int whereSpriteBelongs;
            
            for (i = firstSprite + 1; i < lastSpriteExclusive; i++)
            {
                Sprite spriteBeforeI = this[i - 1];
                Sprite spriteAtI = this[i];
                Sprite spriteAt0 = this[firstSprite];


                if (GetTextureIndex(spriteBeforeI) > GetTextureIndex(spriteAtI))
                {
                    if (i == 1)
                    {
                        base.Insert(0, spriteAtI);
                        base.RemoveAtOneWay(i + 1);
                        continue;
                    }

                    for (whereSpriteBelongs = i - 2; whereSpriteBelongs > firstSprite - 1; whereSpriteBelongs--)
                    {
                        Sprite spriteAtWhereSpriteBelongs = this[whereSpriteBelongs];

                        if (GetTextureIndex(spriteAtWhereSpriteBelongs) <= GetTextureIndex(spriteAtI))
                        {
                            base.Insert(whereSpriteBelongs + 1, spriteAtI);
                            base.RemoveAtOneWay(i + 1);
                            break;
                        }
                        else if (whereSpriteBelongs == firstSprite &&
                            GetTextureIndex(spriteAt0) > GetTextureIndex(spriteAtI))
                        {
                            base.Insert(firstSprite, spriteAtI);
                            base.RemoveAtOneWay(i + 1);
                            break;
                        }
                    }
                }
            }
        }

        int GetTextureIndex(Sprite sprite)
        {
            if (sprite.Texture == null)
            {
                return -1;
            }
            else
            {
                return sSortTextureDictionary[sprite.Texture];
            }
        }

        public void SortTextureOnZBreaks()
        {
            List<int> zBreaks = GetZBreaks();

            zBreaks.Insert(0, 0);
            zBreaks.Add(Count);

            for (int i = 0; i < zBreaks.Count - 1; i++)
            {
                SortTextureInsertion(zBreaks[i], zBreaks[i + 1]);
            }

        }

        #endregion


        public override string ToString()
        {
            return base.ToString();
        }

        #endregion

        #endregion

        #region IEquatable<SpriteList> Members

        bool IEquatable<SpriteList>.Equals(SpriteList other)
        {
            return this == other;
        }

        #endregion
    }
}
