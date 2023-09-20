using Microsoft.Xna.Framework;

namespace FlatRedBall.Graphics.Texture
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using FlatRedBall.Content;
    using FlatRedBall.IO;
    using FlatRedBall.Graphics.Texture;
#if NETFX_CORE
    using System.Threading.Tasks;
#endif


    public class AtlasLoader
    {
        static List<Atlas> loadedAtlases = new List<Atlas>();


#if IOS
        private readonly bool supportRetina;
#endif

        public AtlasLoader()
        {
#if IOS
            this.supportRetina = UIKit.UIScreen.MainScreen.Scale == 2.0f;
#endif
        }
        


        public static Atlas Load(string atlasResource, string contentManagerName)
        {
            var contentManager = FlatRedBall.FlatRedBallServices.GetContentManagerByName(contentManagerName);
            // See if this thing is already loaded:
            bool isAlreadyLoaded = contentManager.IsAssetLoadedByName<Atlas>(atlasResource);

            if (isAlreadyLoaded)
            {
                return contentManager.GetDisposable<Atlas>(atlasResource);
            }
            else
            {
                string absoluteFileName = FileManager.MakeAbsolute(atlasResource);


                var directory = FileManager.GetDirectory(atlasResource, RelativeType.Relative);

                var texture = contentManager.Load<Microsoft.Xna.Framework.Graphics.Texture2D>(Path.ChangeExtension(atlasResource, "png"));
                
                var dataFileLines = ReadDataFile(atlasResource);

                var atlas = new Atlas();

                foreach(var cols in dataFileLines
                    .Where(item=>!string.IsNullOrWhiteSpace(item) && !item.StartsWith("#"))
                    .Select(item=>item.Split(';'))
                    )
                {
                    if (cols.Length != 10)
                    {
                        throw new InvalidDataException("Incorrect format data in spritesheet data file");
                    }

                    var isRotated = int.Parse(cols[1]) == 1;
                    var slashIndex = cols[0].IndexOf("/", StringComparison.Ordinal);
                    var name = cols[0].Substring(slashIndex + 1).ToLowerInvariant();

                    var sourceRectangle = new Microsoft.Xna.Framework.Rectangle(
                        int.Parse(cols[2]),
                        int.Parse(cols[3]),
                        int.Parse(cols[4]),
                        int.Parse(cols[5]));
                    var size = new Microsoft.Xna.Framework.Vector2(
                        int.Parse(cols[6]),
                        int.Parse(cols[7]));
                    var pivotPoint = new Microsoft.Xna.Framework.Vector2(
                        float.Parse(cols[8]),
                        float.Parse(cols[9]));
                    var sprite = new AtlasedTexture(name, texture, sourceRectangle, size, pivotPoint, isRotated);

                    atlas.Add(name, sprite);
                }

                contentManager.AddDisposable(atlasResource, atlas);

                loadedAtlases.Add(atlas);

                return atlas;
            }
        }

        public static AtlasedTexture LoadAtlasedTexture(string textureName, bool ignoreCase = false)
        {
            foreach(var atlas in loadedAtlases)
            {
                var nameWithoutExtension = FlatRedBall.IO.FileManager.RemoveExtension(textureName);

                if (atlas.Contains(nameWithoutExtension, ignoreCase))
                {
                    return atlas.Sprite(nameWithoutExtension, ignoreCase);
                }
            }
            return null;
        }

        public static AtlasedTexture LoadAtlasedTextureByFileName(string fileName)
        {
            var fileNameToAtlasName = ConvertFileNameToAtlasName(fileName);

            return LoadAtlasedTexture(fileNameToAtlasName, ignoreCase:true);
        }

        private static string ConvertFileNameToAtlasName(string fileName)
        {
            var directoryToBeRelativeTo = FileManager.DefaultRelativeDirectory + @"Content/";

            var relativeToContent = FileManager.MakeRelative(fileName, directoryToBeRelativeTo).ToLowerInvariant() ;

            return relativeToContent;
        }

        private static void ClearDisposedAtlases()
        {
            for(int i = loadedAtlases.Count - 1; i > -1; i--)
            {
                if(loadedAtlases[i].IsDisposed)
                {
                    loadedAtlases.RemoveAt(i);
                }
            }
        }

#if __IOS__
        private static string[] ReadDataFile(string dataFile) 
        {
            var input = dataFile;

            //if (this.supportRetina)
            //{
            //    var dataFile2x = Path.Combine (Path.GetDirectoryName (dataFile),
            //        Path.GetFileNameWithoutExtension (dataFile)
            //        + "@2x" + Path.GetExtension (dataFile));

            //    if (File.Exists (dataFile2x))
            //    {
            //        input = dataFile2x;
            //    }
            //}

            return File.ReadAllLines (input);
        }
#elif NETFX_CORE
        private static string[] ReadDataFile(string dataFile)
        {
            var dataFileLines = ReadDataFileLines(dataFile);

            return dataFileLines.Result.ToArray();
        }

        private static async Task<string[]> ReadDataFileLines(string dataFile)
        {
            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;

            var file = await folder.GetFileAsync(dataFile).AsTask().ConfigureAwait(false);
            var fileContents = await Windows.Storage.FileIO.ReadLinesAsync(file).AsTask().ConfigureAwait(false);

            return fileContents.ToArray();
        }
#elif __ANDROID__
		private static string[] ReadDataFile(string dataFile) {
			using(var ms = new MemoryStream()) {
				using (var s = Game.Activity.Assets.Open (dataFile)) {
					s.CopyTo (ms);
					return System.Text.Encoding.Default.GetString (ms.ToArray()).Split (new char[] { '\n'});
				}
			}
		}
#else
        private static string[] ReadDataFile(string dataFile) 
        {
            var fileName = dataFile;
            if(FileManager.IsRelative(fileName))
            {
                fileName = FileManager.RelativeDirectory + fileName;
            }
            return File.ReadAllLines(fileName);
        }
#endif
    }
}