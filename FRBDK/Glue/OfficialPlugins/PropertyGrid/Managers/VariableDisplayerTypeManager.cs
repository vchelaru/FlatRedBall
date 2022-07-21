using FlatRedBall.Glue.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using WpfDataUi.Controls;

namespace OfficialPlugins.PropertyGrid.Managers
{
    internal static class VariableDisplayerTypeManager
    {
        public static Dictionary<string, Type> TypeNameToTypeAssociations { get; private set; } = new Dictionary<string, Type>();

        public static void FillTypeNameAssociations()
        {
            // which assemblies do we look in?
            var wpfDataUiAssembly = typeof(CheckBoxDisplay).Assembly;
            var userControl = typeof(UserControl);

            var types = wpfDataUiAssembly.GetTypes();

            foreach (var type in types)
            {
                if (userControl.IsAssignableFrom(type))
                {
                    TypeNameToTypeAssociations[type.FullName] = type;
                    // add unqualified ...is this a good idea?
                    TypeNameToTypeAssociations[type.Name] = type;
                }
            }
        }
    }
}
