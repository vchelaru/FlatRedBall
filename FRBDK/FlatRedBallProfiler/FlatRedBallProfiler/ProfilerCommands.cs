using FlatRedBallProfiler.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace FlatRedBallProfiler
{
    public class ProfilerCommands : Singleton<ProfilerCommands>
    {

        System.Windows.Forms.TabControl masterTabControl;
        TreeView managedObjectsTreeView;
        List<ManagedObjectsCategory> managedObjectsCategory = new List<ManagedObjectsCategory>();

        internal void RegisterTabControl(System.Windows.Forms.TabControl tabControl)
        {
            masterTabControl = tabControl;
        }


        public void AddControl(Control control, string tabName)
        {
            System.Windows.Forms.TabPage tabPage = new System.Windows.Forms.TabPage();
            tabPage.Text = tabName;
            

            var host = new System.Windows.Forms.Integration.ElementHost();
            host.Dock = System.Windows.Forms.DockStyle.Fill;
            host.Child = control;


            tabPage.Controls.Add(host);
            masterTabControl.Controls.Add(tabPage);
        }

        internal void RegisterManagedObjectsTreeView(TreeView treeView)
        {
            managedObjectsTreeView = treeView;
        }

        public void AddManagedObjectsCategory(string categoryName, Func<IEnumerable<string>> getParentNames)
        {
            managedObjectsCategory.Add(
                new ManagedObjectsCategory
                {
                    CategoryName = categoryName,
                    GetParentNamesFunc = getParentNames
                });
        }

        // Do we want this to be public at some point?
        internal void RefreshManagedObjects()
        {
            managedObjectsTreeView.Items.Clear();


            // This is a little expensive given we do GetParentNamesFunc multiple times, but it's also really easy 
            // to code so I'm going to live with the perf issues for now:
            foreach (var category in managedObjectsCategory.OrderByDescending(item=>item.GetParentNamesFunc().Count()))
            {

                TreeViewItem newItem = new TreeViewItem();
                var parentNames = category.GetParentNamesFunc().ToList();
                newItem.Header = $"{category.CategoryName} ({parentNames.Count})" ;


                var tuples = parentNames
                    .Distinct()
                    .Select(name =>
                    {
                        var count = parentNames.Count(item2 => item2 == name);
                        Tuple<string, int> tuple = new Tuple<string, int>(name, count);

                        return tuple;
                    })
                    .OrderByDescending(item=>item.Item2);

                foreach (var tuple in tuples)
                {
                    newItem.Items.Add(tuple.Item2.ToString() + " " + tuple.Item1);
                }

                managedObjectsTreeView.Items.Add(newItem);
            }
        }

    }
}
