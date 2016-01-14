using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.IO;
#if FRB_MDX

#else
using Microsoft.Xna.Framework.Graphics;
#endif

namespace FlatRedBall.Graphics.Texture
{
    public class TextureAtlas
    {
        public Texture2D Texture
        {
            get;
            set;
        }

        public List<AtlasEntry> Entries
        {
            get;
            set;
        }



        public TextureAtlas()
        {
            Entries = new List<AtlasEntry>();
        }

        public AtlasEntry GetEntryFor(string fileName)
        {
            string thisDirectory = FileManager.GetDirectory(
                this.Texture.Name, RelativeType.Absolute);

            if (FileManager.IsRelative(fileName))
            {
                fileName = FileManager.RelativeDirectory + fileName;
            }

            fileName = FileManager.MakeRelative(fileName, thisDirectory);

            return Entries.FirstOrDefault(item => item.OriginalName.ToLower() == fileName);


        }




        public void UpdateContainedEntries()
        {
            foreach (var item in Entries)
            {
                item.ParentHeight = this.Texture.Height;
                item.ParentWidth = this.Texture.Width;
            }
        }
    }
}
