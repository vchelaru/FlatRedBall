using AnimationEditor.Core.Data;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FilePath = FlatRedBall.IO.FilePath;

namespace AnimationEditor.Core
{
    public class ProjectManager : Singleton<ProjectManager>
    {
        static TileMapInformationList mTileMapInformationList = new TileMapInformationList();

        public AnimationChainListSave AnimationChainListSave { get; set; }

        public TileMapInformationList TileMapInformationList
        {
            get => mTileMapInformationList;
            set => mTileMapInformationList = value;
        }

        public FilePath[] ReferencedPngs { get; private set; } = new FilePath[0];

        public string FileName { get; set; }

        internal void LoadAnimationChain(FilePath fileName)
        {
            if (fileName.Exists())
            {
                var acls = AnimationChainListSave.FromFile(fileName.FullPath);

                AddShapeCollectionsToFrames(acls);

                AnimationChainListSave = acls;
                FileName = fileName.FullPath;

                TryLoadProjectFile(fileName.GetDirectoryContainingThis() + acls.ProjectFile);

                // Note: pixel→UV coordinate conversion requires texture dimensions; deferred
                // to the rendering layer (WireframeControl) once textures are loaded.
            }
        }

        private void AddShapeCollectionsToFrames(AnimationChainListSave acls)
        {
            foreach (var chain in acls.AnimationChains)
            {
                foreach (var frame in chain.Frames)
                {
                    frame.ShapeCollectionSave ??= new FlatRedBall.Content.Math.Geometry.ShapeCollectionSave();
                }
            }
        }

        private void TryLoadProjectFile(FilePath projectFile)
        {
            if (projectFile?.Exists() != true)
            {
                ReferencedPngs = new FilePath[0];
                return;
            }

            // Assume content folder; adjust for Android if needed
            var projectDirectory = projectFile.GetDirectoryContainingThis() + "Content/";

            var files = new HashSet<FilePath>();

            void AddRfs(XElement referencedFiles)
            {
                if (referencedFiles != null)
                {
                    foreach (var file in referencedFiles.Elements())
                    {
                        var nameDescendant = file.Elements("Name").FirstOrDefault();
                        if (nameDescendant != null)
                        {
                            var name = nameDescendant.Value;
                            if (FileManager.GetExtension(name) == "png")
                            {
                                files.Add(projectDirectory + name);
                            }
                        }
                    }
                }
            }

            XElement xElement = null;
            try
            {
                xElement = XElement.Load(projectFile.FullPath);
            }
            catch
            {
                // Could not load — possibly a .gluj format we can't parse yet
            }

            if (xElement != null)
            {
                var screens = xElement.Elements("Screens").FirstOrDefault();
                if (screens != null)
                {
                    foreach (var screen in screens.Elements())
                    {
                        AddRfs(screen.Elements("ReferencedFiles").FirstOrDefault());
                    }
                }

                var entities = xElement.Elements("Entities").FirstOrDefault();
                if (entities != null)
                {
                    foreach (var entity in entities.Elements())
                    {
                        AddRfs(entity.Elements("ReferencedFiles").FirstOrDefault());
                    }
                }

                AddRfs(xElement.Elements("GlobalFiles").FirstOrDefault());

                ReferencedPngs = files.ToArray();
            }
            else
            {
                // No parseable project file — fall back to all .png files relative to project dir
                ReferencedPngs = FileManager
                    .GetAllFilesInDirectory(projectDirectory, "png")
                    .Select(item => new FilePath(item))
                    .ToArray();
            }
        }

        internal void LoadTileMapInformation(string fileName)
        {
            TileMapInformationList = FileManager.XmlDeserialize<TileMapInformationList>(fileName);
        }
    }
}
