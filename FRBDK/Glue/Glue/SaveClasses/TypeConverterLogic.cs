using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.SaveClasses
{
    /// <summary>
    /// Provides common methods for the System.ComponentModel.TypeConverter used to handle the apperance of
    /// variables in the PropertyGrid. This is still used by the WPF plugins for things like options.
    /// </summary>
    public static class TypeConverterLogic
    {
        public static TypeConverter GetTypeConverter(NamedObjectSave instance, GlueElement container, string memberName, Type memberType, string customTypeName,
            VariableDefinition variableDefinition)
        {
            var toReturn = PluginManager.GetTypeConverter(
                 container, instance, memberType, memberName, customTypeName);

            if (variableDefinition?.ForcedOptions?.Count > 0)
            {
                var converter = new DelegateBasedTypeConverter();
                converter.CustomDelegate = () =>
                {
                    var list = new List<string>();
                    list.AddRange(variableDefinition.ForcedOptions);
                    return list;
                };
                return converter;
            }
            else if (variableDefinition?.CustomGetForcedOptionFunc != null)
            {
                var converter = new DelegateBasedTypeConverter();
                converter.CustomDelegate = () =>
                {
                    var list = new List<string>();
                    list.AddRange(variableDefinition.CustomGetForcedOptionFunc(container, instance, null));
                    return list;
                };
                return converter;
            }

            return toReturn;
        }

    }
}
