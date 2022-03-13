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
        public static void SavePreview(BitmapSource bitmapSource, GlueElement glueElement, StateSave stateSave)
        {
            var filePath = GlueCommands.Self.GluxCommands.GetPreviewLocation(glueElement, stateSave);

            var directoryToCreate = filePath.GetDirectoryContainingThis().FullPath;
            System.IO.Directory.CreateDirectory(directoryToCreate);

            using (var fileStream = new FileStream(filePath.FullPath, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(fileStream);
            }
        }

    }
}
