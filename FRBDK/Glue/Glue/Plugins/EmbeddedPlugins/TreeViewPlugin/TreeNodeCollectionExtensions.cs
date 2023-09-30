using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Windows.Controls;

namespace FlatRedBall.Glue.FormHelpers
{
    public static class TreeNodeCollectionExtensions
    {
        public static void SortByTextConsideringDirectories(this ItemCollection treeNodeCollection, bool recursive = false)
        {
            int lastObjectExclusive = treeNodeCollection.Count;
            for (int i = 0 + 1; i < lastObjectExclusive; i++)
            {
                TreeViewItem first = treeNodeCollection[i] as TreeViewItem;
                TreeViewItem second = treeNodeCollection[i - 1] as TreeViewItem;
                if (TreeNodeComparer(first, second) < 0)
                {
                    if (i == 1)
                    {
                        var treeNode = treeNodeCollection[i];
                        treeNodeCollection.RemoveAt(i);

                        treeNodeCollection.Insert(0, treeNode);
                        continue;
                    }

                    for (var whereObjectBelongs = i - 2; whereObjectBelongs > -1; whereObjectBelongs--)
                    {
                        second = treeNodeCollection[whereObjectBelongs] as TreeViewItem;
                        if (TreeNodeComparer(treeNodeCollection[i] as TreeViewItem, second) >= 0)
                        {
                            var treeNode = treeNodeCollection[i];

                            treeNodeCollection.RemoveAt(i);
                            treeNodeCollection.Insert(whereObjectBelongs + 1, treeNode);
                            break;
                        }

                        if (whereObjectBelongs == 0 && TreeNodeComparer(treeNodeCollection[i] as TreeViewItem, treeNodeCollection[0] as TreeViewItem) < 0)
                        {
                            var treeNode = treeNodeCollection[i];
                            treeNodeCollection.RemoveAt(i);
                            treeNodeCollection.Insert(0, treeNode);
                            break;
                        }
                    }
                }
            }

            if (recursive)
            {
                foreach (TreeViewItem node in treeNodeCollection)
                {
                    if (node.Items.Count > 0) // IsDirectoryNode();
                    {
                        SortByTextConsideringDirectories(node.Items, recursive);
                    }
                }
            }
        }

        [DllImport("Shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern int StrCmpLogicalW(string x, string y);


        private static int TreeNodeComparer(ItemsControl first, ItemsControl second)
        {
            bool isFirstDirectory = first.Items.Count > 0; // IsDirectoryNode();
            bool isSecondDirectory = second.Items.Count > 0;

            return isFirstDirectory switch
            {
                true when !isSecondDirectory => -1,
                false when isSecondDirectory => 1,
                _ => StrCmpLogicalW(first.Name, second.Name)
            };

            //return first.Text.CompareTo(second.Text);
            // This will put Level9 before Level10
        }
    }
}
