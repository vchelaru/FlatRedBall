using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.GuiDisplay;
using TMXGlueLib;


namespace TmxEditor.PropertyGridDisplayers
{
    public class MapLayerDisplayer : PropertyGridDisplayer
    {
        #region Fields

        public override object Instance
        {
            get
            {
                return base.Instance;
            }
            set
            {
                base.Instance = value;
                UpdateDisplayedProperties();

                base.PropertyGrid.Refresh();
            }
        }

        MapLayer MapLayerInstance
        {
            get
            {
                return base.Instance as MapLayer;
            }
        }

        #endregion

        #region Properties

        public property CurrentLayerProperty
        {
            get
            {
                if (PropertyGrid.SelectedGridItem == null)
                {
                    return null;
                }
                else
                {
                    string name = PropertyGrid.SelectedGridItem.Label;
                    if (MapLayerInstance != null)
                    {
                        var property = DisplayerExtensionMethods.GetPropertyByName(name, MapLayerInstance.properties);
                        return property;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        #endregion

        public void UpdateDisplayedProperties()
        {
            ExcludeAllMembers();

            this.RefreshOnTimer = false;
            

            this.DisplayProperties(MapLayerInstance.properties);
        }
    }
}
