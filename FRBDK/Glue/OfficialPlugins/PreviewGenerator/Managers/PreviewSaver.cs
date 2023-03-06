using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;

namespace OfficialPlugins.PreviewGenerator.Managers
{
    public static class PreviewSaver
    {
        public static void SavePreview(BitmapSource bitmapSource, GlueElement glueElement, StateSave stateSave, FilePath forcedLocation = null)
        {
            if(bitmapSource == null)
            {
                throw new ArgumentNullException(nameof(bitmapSource));
            }
            var filePath = forcedLocation ?? GlueCommands.Self.GluxCommands.GetPreviewLocation(glueElement, stateSave);

            var directoryToCreate = filePath.GetDirectoryContainingThis().FullPath;
            System.IO.Directory.CreateDirectory(directoryToCreate);

            if(filePath.Exists())
            {
                System.IO.File.Delete(filePath.FullPath); 
            }

            using (var fileStream = new FileStream(filePath.FullPath, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(fileStream);
            }
        }

    }
}
