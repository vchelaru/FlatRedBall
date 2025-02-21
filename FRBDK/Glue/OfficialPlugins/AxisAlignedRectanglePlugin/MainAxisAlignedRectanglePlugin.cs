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
        IElement element, NamedObjectSave nos, ReferencedFileSave rfs, string layerName)
    {
        if(string.IsNullOrEmpty(layerName))
        {
            return $"FlatRedBall.Math.Geometry.ShapeManager.AddAxisAlignedRectangle({nos.InstanceName});";
        }
        else
        {
            var visibleVariable = nos.GetCustomVariable("Visible")?.Value;

            var isVisible = visibleVariable == null || visibleVariable is true;
            string toReturn = "";
            if(isVisible)
            {
                // Adding unlayered flips visible to true. Adding layered doesn't, so we have to do
                // it explicitly here:
                toReturn += $"{nos.InstanceName}.Visible = true;";
            }
            toReturn += $"FlatRedBall.Math.Geometry.ShapeManager.AddToLayer({nos.InstanceName}, {layerName});";
            return toReturn;
        }
    }
}
