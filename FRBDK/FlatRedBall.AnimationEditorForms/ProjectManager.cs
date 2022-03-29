using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.AnimationEditorForms.Data;
using FlatRedBall.IO;
using RenderingLibrary.Content;
using FlatRedBall.AnimationEditorForms.Converters;
using FilePath = ToolsUtilities.FilePath;
using FlatRedBall.Glue.SaveClasses;
using System.Xml.Linq;
using System.Diagnostics;

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

        public AnimationChainListSave AnimationChainListSave { get; set; }

        public TileMapInformationList TileMapInformationList
        {
            get => mTileMapInformationList;
            set => mTileMapInformationList = value;
        }

        public FilePath[] ReferencedPngs
        {
            get; private set;
        } = new FilePath[0];
        

        public string FileName { get; set; }

        #endregion


        internal void LoadAnimationChain(FilePath fileName)
        {
            // Reset all textures
            LoaderManager.Self.CacheTextures = false;
            LoaderManager.Self.CacheTextures = true;

            if(fileName.Exists())
            {
                AnimationChainListSave acls = null;

                acls = AnimationChainListSave.FromFile(fileName.FullPath);


                AnimationChainListSave = acls;

                FileName = fileName.FullPath;

                TryLoadProjectFile(fileName.GetDirectoryContainingThis() + acls.ProjectFile);



                // The app prefers UV coordinates....I think?
                if (acls.CoordinateType == Graphics.TextureCoordinateType.Pixel)
                {
                    acls.ConvertToUvCoordinates();
                }
            }
        }

        private void TryLoadProjectFile(FilePath projectFile)
        {
            if(projectFile?.Exists() == true)
            {
                // assume content folder. I suppose Android would be different? Would need to modify this if so...
                var projectDirectory = projectFile.GetDirectoryContainingThis() + "Content/";

                var files = new HashSet<FilePath>();
                void AddRfs(XElement referencedFiles)
                {
                    if(referencedFiles != null)
                    {
                        foreach (var file in referencedFiles.Elements())
                        {
                            var nameDescendant = file.Elements("Name").FirstOrDefault();
                            var name = nameDescendant.Value;
                            if(FileManager.GetExtension(name) == "png")
                            {
                                files.Add(projectDirectory + name);
                            }
                        }
                    }
                }


                // We can't do this because GlueProjectSave depends on MonoGame, and AnimationEditor uses XNA
                //GlueProject = FileManager.XmlDeserialize<GlueProjectSave>(projectFile.FullPath);
                XElement xElement = null;
                try
                {
                    xElement = XElement.Load(projectFile.FullPath);
                }
                catch
                {
                    // no biggie, just don't load it.
                    // Update October 14, 2021
                    // This is now broken due to the change from
                    // GLUX to GLUJ. need to address this eventually...
                }

                if(xElement != null)
                {
                    var screens = xElement.Elements("Screens").FirstOrDefault();
                    if(screens != null)
                    {
                        foreach(var screen in screens.Elements())
                        {
                            var referencedFiles = screen.Elements("ReferencedFiles").FirstOrDefault();

                            AddRfs(referencedFiles);
                        }
                    }
                    var entities = xElement.Elements("Entities").FirstOrDefault();
                    if(entities != null)
                    {
                        foreach(var entity in entities.Elements())
                        {
                            var referencedFiles = entity.Elements("ReferencedFiles").FirstOrDefault();
                            AddRfs(referencedFiles);
                        }
                    }
                    var globalFiles = xElement.Elements("GlobalFiles").FirstOrDefault();
                    AddRfs(globalFiles);

                    ReferencedPngs = files.ToArray();
                }
                else
                {
                    // we don't have a .glux, or we have a .gluj which we currently can't parse. We could just get
                    // all .png files relative to the project directory, though
                    ReferencedPngs = FileManager
                        .GetAllFilesInDirectory(projectDirectory, "png")
                        .Select(item => new FilePath(item))
                        .ToArray();
                }

            }
            else
            {
                ReferencedPngs = new FilePath[0];
            }
        }

        internal void LoadTileMapInformation(string fileName)
        {
            TileMapInformationList infoList = FileManager.XmlDeserialize<TileMapInformationList>(fileName);

            TileMapInformationList = infoList;

        }
    }
}
