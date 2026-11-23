using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.Common;

namespace OfficialPlugins.AxisAlignedRectanglePlugin;

[Export(typeof(PluginBase))]
public class MainAxisAlignedRectanglePlugin : PluginBase
{
    public override void StartUp()
    {
        AdjustAssetTypeInfo();
    }

    private void AdjustAssetTypeInfo()
    {
        var ati = AvailableAssetTypes.CommonAtis.AxisAlignedRectangle;

        ati.VariableDefinitions.Add(new VariableDefinition
        {
            Name = "XVelocity",
            Type = "float",
            IsVariableVisibleInEditor = (_, _) => false
        });
        ati.VariableDefinitions.Add(new VariableDefinition
        {
            Name = "YVelocity",
            Type = "float",
            IsVariableVisibleInEditor = (_, _) => false
        });
        ati.VariableDefinitions.Add(new VariableDefinition
        {
            Name = "ZVelocity",
            Type = "float",
            IsVariableVisibleInEditor = (_, _) => false
        });

        ati.AddToManagersFunc += HandleAddAxisAlignedRectangleToManagers;


    }

    private string HandleAddAxisAlignedRectangleToManagers(
        IElement element, NamedObjectSave nos, ReferencedFileSave rfs, string layerName) =>
        ShapeAddToManagersCodeGenerator.Generate(element, nos, layerName, "AddAxisAlignedRectangle");
}
