using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;

namespace FlatRedBall.Glue.Plugins.Interfaces
{
    public interface INewFile : IPlugin
    {
        void AddNewFileOptions(NewFileWindow newFileWindow);
        bool CreateNewFile(AssetTypeInfo assetTypeInfo, object extraData, string directory, string name, out string resultingName);
        void ReactToNewFile(ReferencedFileSave newFile);
    }
}
