using FlatRedBall.Glue.Plugins;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace OfficialPlugins.PostProcessingPlugin
{
    [Export(typeof(PluginBase))]
    public class MainPostProcessingPlugin : PluginBase
    {
        public override string FriendlyName => "Post Processing Plugin";

        public override void StartUp()
        {
            // todo - add ATIs here:

            var testSave = new PostProcessSave();
            testSave.FxFiles.Add("test.fx");
            testSave.CodeFile = "test.cs";

            var serialized = JsonConvert.SerializeObject(testSave);

            this.AddAssetTypeInfo(AssetTypeInfoManager.PostProcessAti);

            AssignEvents();
        }

        private void AssignEvents()
        {
            this.FillWithReferencedFiles += HandleFillWithReferencedFiles;
        }

        private GeneralResponse HandleFillWithReferencedFiles(FlatRedBall.IO.FilePath path, List<FlatRedBall.IO.FilePath> list)
        {
            /////////////////Early Out///////////////////////
            if(path.Extension != "post")
            {
                return GeneralResponse.SuccessfulResponse;
            }
            if(!path.Exists())
            {
                return GeneralResponse.UnsuccessfulWith($"Could not find file {path}");
            }
            //////////////End Early Out/////////////////////

            var fileContents = System.IO.File.ReadAllText(path.FullPath);
            var deserialized = JsonConvert.DeserializeObject<PostProcessSave>(fileContents);

            var directory = path.GetDirectoryContainingThis().FullPath;

            foreach(var fxFile in deserialized.FxFiles)
            {
                list.Add(directory + fxFile);
            }

            return GeneralResponse.SuccessfulResponse;
        }
    }
}
