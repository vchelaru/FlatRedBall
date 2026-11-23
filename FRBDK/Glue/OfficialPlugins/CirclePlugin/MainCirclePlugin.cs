using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.Common;

namespace OfficialPlugins.CirclePlugin;
[Export(typeof(PluginBase))]
public class MainCirclePlugin : PluginBase
{
    public override void StartUp()
    {
        AdjustAssetTypeInfo();
    }

    private void AdjustAssetTypeInfo()
    {
        var ati = AvailableAssetTypes.CommonAtis.Circle;
        ati.AddToManagersFunc += HandleAddToManagers;
    }

    private string HandleAddToManagers(
    IElement element, NamedObjectSave nos, ReferencedFileSave rfs, string layerName) =>
        ShapeAddToManagersCodeGenerator.Generate(element, nos, layerName, "AddCircle");
}
