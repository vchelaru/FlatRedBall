using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace OfficialPlugins.VariableDisplay
{
    class RefreshLogic
    {
        static int RefreshesToSkip = 0;

        internal static void RefreshGrid(DataUiGrid grid = null)
        {
            grid = grid ?? MainPropertyGridPlugin.VariableGrid.DataUiGrid;
            if (RefreshesToSkip > 0)
            {
                RefreshesToSkip--;
            }
            else
            {
                grid.Refresh();
            }

            foreach (var category in grid.Categories)
            {
                List<InstanceMember> membersToRefresh = new List<InstanceMember>();

                foreach (var instanceMember in category.Members)
                {
                    var dataGridItem = instanceMember as DataGridItem;

                    if(dataGridItem != null)
                    {
                        // Not sure why we check if the instanceMember has non-0 count for options.
                        // It could have had 0 before, but after a refresh, it may now have options.
                        // Update August 16, 2022
                        // If an item has options, let's refresh them even if there is no TypeConverter:

                        bool shouldRefresh = instanceMember.CustomOptions?.Count > 0 ||
                            dataGridItem.TypeConverter != null;

                        if (shouldRefresh)
                        {
                            dataGridItem.RefreshOptions();
                            membersToRefresh.Add(instanceMember);
                        }
                    }
                }

                bool shouldSort = membersToRefresh.Count != 0;

                foreach (var item in membersToRefresh)
                {
                    var index = category.Members.IndexOf(item);
                    category.Members.Remove(item);
                    category.Members.Insert(index, item);
                }
            }
        }

        internal static void IgnoreNextRefresh()
        {
            RefreshesToSkip++;
        }
    }
}
