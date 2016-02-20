using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Content.ContentLoaders
{
    public class SongLoader : IContentLoader<Song>
    {
        public Song Load(string absoluteFileName)
        {

            Song song;

#if ANDROID
	        if(absoluteFileName.StartsWith("./"))
	        {
		        absoluteFileName = absoluteFileName.Substring(2);
	        }
#endif

            var uri = new Uri(absoluteFileName, UriKind.Relative);
            song = Song.FromUri(absoluteFileName, uri);

#if ANDROID
            var songType = song.GetType();

            var fields = songType.GetField("assetUri",
                                        System.Reflection.BindingFlags.NonPublic |
                                        System.Reflection.BindingFlags.Instance);
			Android.Net.Uri androidUri = Android.Net.Uri.Parse(absoluteFileName);
            fields.SetValue(song, androidUri);
#endif
            return song;

        }
    }
}
