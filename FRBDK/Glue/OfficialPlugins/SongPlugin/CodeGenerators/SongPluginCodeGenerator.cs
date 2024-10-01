using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.SongPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.SongPlugin.CodeGenerators
{
    public class SongPluginCodeGenerator : ElementComponentCodeGenerator
    {
        public override ICodeBlock GeneratePostInitialize(ICodeBlock codeBlock, GlueElement element)
        {
            var playingSong = element.ReferencedFiles
                .FirstOrDefault(item =>
                {
                    return 
                        item.Properties.GetValue<bool>(nameof(ReferencedFileSave.LoadedOnlyWhenReferenced)) == false &&
                        item.GetAssetTypeInfo()?.QualifiedRuntimeTypeName.QualifiedType == "Microsoft.Xna.Framework.Media.Song";
                });

            if(playingSong != null)
            {
                int volume = playingSong.Properties.GetValue<int>(nameof(MainSongControlViewModel.Volume));
                bool shouldLoop = playingSong.Properties.GetValue<bool>(nameof(MainSongControlViewModel.ShouldLoopSong));
                bool shouldSetVolume = playingSong.Properties.GetValue<bool>(nameof(MainSongControlViewModel.IsSetVolumeChecked));

                if(shouldSetVolume)
                {
                    var dividedVolume = (volume / 100.0f).ToString(CultureInfo.InvariantCulture);
                    codeBlock.Line($"Microsoft.Xna.Framework.Media.MediaPlayer.Volume = {dividedVolume}f;");
                }

                var isRepeatingString = shouldLoop.ToString().ToLowerInvariant();
                codeBlock.Line($"Microsoft.Xna.Framework.Media.MediaPlayer.IsRepeating = {isRepeatingString};");
            }

            return codeBlock;
        }
    }
}
