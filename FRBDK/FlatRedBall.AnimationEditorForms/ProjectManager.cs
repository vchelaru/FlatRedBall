using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.AnimationEditorForms.Data;
using FlatRedBall.IO;
using RenderingLibrary.Content;
using FlatRedBall.AnimationEditorForms.Converters;

namespace FlatRedBall.AnimationEditorForms
{
    public class ProjectManager
    {
        #region Fields

        static ProjectManager mSelf;

        static TileMapInformationList mTileMapInformationList = new TileMapInformationList();

        #endregion

        #region Properties

        public static ProjectManager Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new ProjectManager();
                }
                return mSelf;
            }
        }

        public AnimationChainListSave AnimationChainListSave
        {
            get;
            set;
        }

        public TileMapInformationList TileMapInformationList
        {
            get
            {
                return mTileMapInformationList;
            }
            set
            {
                mTileMapInformationList = value;
            }
        }

        public string FileName
        {
            get;
            set;
        }

        #endregion


        internal void LoadAnimationChain(string fileName)
        {
            // Reset all textures
            LoaderManager.Self.CacheTextures = false;
            LoaderManager.Self.CacheTextures = true;

            AnimationChainListSave acls = AnimationChainListSave.FromFile(fileName);


            AnimationChainListSave = acls;

            FileName = fileName;

            //Now just convert back to pixel when saving out
            if (acls.CoordinateType == Graphics.TextureCoordinateType.Pixel)
            {
                acls.ConvertToUvCoordinates();
            }

        }

        


        internal void LoadTileMapInformation(string fileName)
        {
            TileMapInformationList infoList = FileManager.XmlDeserialize<TileMapInformationList>(fileName);

            TileMapInformationList = infoList;

        }
    }
}
