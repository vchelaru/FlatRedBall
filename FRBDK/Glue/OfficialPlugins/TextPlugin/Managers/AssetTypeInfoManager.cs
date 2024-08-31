using FlatRedBall.Glue.Elements;
using FlatRedBall.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.TextPlugin.Managers
{
    internal class AssetTypeInfoManager
    {
        internal static void HandleStartup()
        {
            AddSpacingVariable();
            AddNewLineDistanceVariable();
        }

        private static void AddSpacingVariable()
        {
            var ati = AvailableAssetTypes.CommonAtis.Text;

            var variableDefinition = new VariableDefinition
            {
                Name = nameof(Text.Spacing),
                Type = "float",
                DefaultValue = "1",
                Category = "Size",
            };

            variableDefinition.SubtextFunc = (element, variable) =>
            {
                return "Returns the spacing between characters. This should only be modified if not using TextureScale";
            };

            ati.VariableDefinitions.Add(variableDefinition);
        }

        private static void AddNewLineDistanceVariable()
        {
            var ati = AvailableAssetTypes.CommonAtis.Text;


            var variableDefinition = new VariableDefinition
            {
                Name = nameof(Text.NewLineDistance),
                Type = "float",
                DefaultValue = "1.4",
                Category = "Size",
            };

            variableDefinition.SubtextFunc = (element, variable) =>
            {
                return "Returns the distance between lines of text. This should only be modified if not using TextureScale";
            };

            ati.VariableDefinitions.Add(variableDefinition);

        }
    }
}
