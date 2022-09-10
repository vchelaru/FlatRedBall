using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace FlatRedBall.Glue.FormHelpers
{
    public static class TreeNodeCollectionExtensions
    {
        public static void SortByTextConsideringDirectories(this TreeNodeCollection treeNodeCollection, bool recursive = false)
        {
            int lastObjectExclusive = treeNodeCollection.Count;
            int whereObjectBelongs;
            for (int i = 0 + 1; i < lastObjectExclusive; i++)
            {
                TreeNode first = treeNodeCollection[i];
                TreeNode second = treeNodeCollection[i - 1];
                if (TreeNodeComparer(first, second) < 0)
                {
                    if (i == 1)
                    {
                        TreeNode treeNode = treeNodeCollection[i];
                        treeNodeCollection.RemoveAt(i);

                        treeNodeCollection.Insert(0, treeNode);
                        continue;
                    }

                    for (whereObjectBelongs = i - 2; whereObjectBelongs > -1; whereObjectBelongs--)
                    {
                        second = treeNodeCollection[whereObjectBelongs];
                        if (TreeNodeComparer(treeNodeCollection[i], second) >= 0)
                        {
                            TreeNode treeNode = treeNodeCollection[i];

                            treeNodeCollection.RemoveAt(i);
                            treeNodeCollection.Insert(whereObjectBelongs + 1, treeNode);
                            break;
                        }
                        else if (whereObjectBelongs == 0 && TreeNodeComparer(treeNodeCollection[i], treeNodeCollection[0]) < 0)
                        {
                            TreeNode treeNode = treeNodeCollection[i];
                            treeNodeCollection.RemoveAt(i);
                            treeNodeCollection.Insert(0, treeNode);
                            break;
                        }
                    }
                }
            }

            if (recursive)
            {
                foreach (TreeNode node in treeNodeCollection)
                {
                    if (node.IsDirectoryNode())
                    {
                        SortByTextConsideringDirectories(node.Nodes, recursive);
                    }
                }
            }

        }

        [DllImport("Shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern int StrCmpLogicalW(string x, string y);

        //public override int Compare(string x, string y)
        //{
        //    return StrCmpLogicalW(x, y);
        //}

        private static int TreeNodeComparer(TreeNode first, TreeNode second)
        {
            bool isFirstDirectory = first.IsDirectoryNode();
            bool isSecondDirectory = second.IsDirectoryNode();

            if (isFirstDirectory && !isSecondDirectory)
            {
                return -1;
            }
            else if (!isFirstDirectory && isSecondDirectory)
            {
                return 1;
            }
            else
            {

                //return first.Text.CompareTo(second.Text);
                // This will put Level9 before Level10
                return StrCmpLogicalW(first.Text, second.Text);
            }
        }

        public static bool ContainsText(this TreeNodeCollection treeNodeCollection, string textToSearchFor)
        {
            foreach (TreeNode treeNode in treeNodeCollection)
            {
                if (treeNode.Text == textToSearchFor)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
