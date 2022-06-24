using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.PropertyGrid.Managers
{
    public static class PreferredDisplayerManager
    {
        static Dictionary<string, Type> typeDictionary;
        static PreferredDisplayerManager()
        {
            typeDictionary = new Dictionary<string, Type>();
        }

        public Type GetPreferredDisplayerType(string name)
        {

        }
    }
}
