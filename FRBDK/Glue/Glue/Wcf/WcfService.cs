using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueWcfServices;

namespace Glue.Wcf
{
    public class WcfService : IWcfService
    {
               //void SelectNamedObject(string elementName, string namedObjectName);
        public void SelectNamedObject(string elementName, string namedObjectName)
        {
            IElement container = ObjectFinder.Self.GetIElement(elementName);
            if (container != null)
            {
                NamedObjectSave nos = container.GetNamedObject(namedObjectName);
                if (nos != null)
                {
                    GlueCommands.Self.TreeNodeCommands.SelectTreeNode(nos);
                }
            }
        }

        public void SelectElement(string elementName)
        {
            IElement element = ObjectFinder.Self.GetIElement(elementName);
            if (element != null)
            {
                GlueCommands.Self.TreeNodeCommands.SelectTreeNode(element);

            }
        }

        public void PrintOutput(string output)
        {
            GlueCommands.Self.PrintOutput(output);
        }
    }
}
