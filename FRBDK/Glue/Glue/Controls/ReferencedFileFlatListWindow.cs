using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;

namespace FlatRedBall.Glue.Controls
{
    public partial class ReferencedFileFlatListWindow : Form
    {
        ToolStripMenuItem mMoveToTop;
        ToolStripMenuItem mMoveToBottom;

        ToolStripMenuItem mMoveUp;
        ToolStripMenuItem mMoveDown;

        List<ReferencedFileSave> mReferencedFiles;

        public ReferencedFileFlatListWindow()
        {
            InitializeComponent();

            mMoveToTop = new ToolStripMenuItem("^^ Move To Top");
            mMoveToTop.Click += new System.EventHandler(MoveToTopClick);

            mMoveUp = new ToolStripMenuItem("^ Move Up");
            mMoveUp.Click += new System.EventHandler(MoveUpClick);

            mMoveDown = new ToolStripMenuItem("v Move Down");
            mMoveDown.Click += new System.EventHandler(MoveDownClick);

            mMoveToBottom = new ToolStripMenuItem("vv Move To Bottom");
            mMoveToBottom.Click += new System.EventHandler(MoveToBottomClick);

        }

        public void PopulateFrom(List<ReferencedFileSave> referencedFileList)
        {
            ListBox.Items.Clear();

            mReferencedFiles = referencedFileList;

            foreach (ReferencedFileSave rfs in mReferencedFiles)
            {
                ListBox.Items.Add(rfs);
            }
        }

        private void ListBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                ListBox.SelectedIndex = ListBox.IndexFromPoint(e.Location);

                if (ListBox.SelectedIndex != -1)
                {
                    ContextMenuStrip contextMenuStrip = new ContextMenuStrip();

                    contextMenuStrip.Items.Add(mMoveToTop);
                    contextMenuStrip.Items.Add(mMoveUp);
                    contextMenuStrip.Items.Add(mMoveDown);
                    contextMenuStrip.Items.Add(mMoveToBottom);

                    Point point = new Point(
                        System.Windows.Forms.Cursor.Position.X,
                        System.Windows.Forms.Cursor.Position.Y);

                    contextMenuStrip.Show(point);
                }
            }
            else if(this.ListBox.SelectedItem != null)
            {
                // Code from:
                // http://stackoverflow.com/questions/805165/reorder-a-winforms-listbox-using-drag-and-drop
                this.ListBox.DoDragDrop(this.ListBox.SelectedItem, DragDropEffects.Move);
            }
        }

        #region Right-click move helpers

        private void MoveToTopClick(object sender, EventArgs e)
        {
            ReferencedFileSave objectToRemove = ListBox.SelectedItem as ReferencedFileSave;

            int index = mReferencedFiles.IndexOf(objectToRemove);
            if (index > 0)
            {
                mReferencedFiles.Remove(objectToRemove);
                mReferencedFiles.Insert(0, objectToRemove);
            }

            PostMoveActivity();

        }

        private void MoveUpClick(object sender, EventArgs e)
        {
            ReferencedFileSave objectToRemove = ListBox.SelectedItem as ReferencedFileSave;

            int index = mReferencedFiles.IndexOf(objectToRemove);

            if (index > 0)
            {
                mReferencedFiles.Remove(objectToRemove);

                mReferencedFiles.Insert(index - 1, objectToRemove);
            }

            PostMoveActivity();
        }



        private void MoveDownClick(object sender, EventArgs e)
        {
            ReferencedFileSave objectToRemove = ListBox.SelectedItem as ReferencedFileSave;

            int index = mReferencedFiles.IndexOf(objectToRemove);

            if (index < mReferencedFiles.Count - 1)
            {
                mReferencedFiles.Remove(objectToRemove);

                mReferencedFiles.Insert(index + 1, objectToRemove);
            }

            PostMoveActivity();
        }

        private void MoveToBottomClick(object sender, EventArgs e)
        {
            ReferencedFileSave objectToRemove = ListBox.SelectedItem as ReferencedFileSave;

            int index = mReferencedFiles.IndexOf(objectToRemove);

            if (index < mReferencedFiles.Count - 1)
            {
                mReferencedFiles.Remove(objectToRemove);


                mReferencedFiles.Insert(mReferencedFiles.Count, objectToRemove);
            }

            PostMoveActivity();
        }

        #endregion

        private void PostMoveActivity()
        {
            PopulateFrom(this.mReferencedFiles);

            GlobalContentCodeGenerator.UpdateLoadGlobalContentCode();

            GluxCommands.Self.SaveProjectAndElements();
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ListBox_DragDrop(object sender, DragEventArgs e)
        {
            Point point = ListBox.PointToClient(new Point(e.X, e.Y));
            int index = this.ListBox.IndexFromPoint(point);
            if (index < 0) index = this.ListBox.Items.Count - 1;
            ReferencedFileSave data = e.Data.GetData(typeof(ReferencedFileSave)) as ReferencedFileSave;
            this.ListBox.Items.Remove(data);
            mReferencedFiles.Remove(data);
            this.ListBox.Items.Insert(index, data);
            mReferencedFiles.Insert(index, data);

            GlobalContentCodeGenerator.UpdateLoadGlobalContentCode();

            GluxCommands.Self.SaveProjectAndElements();
        }

        private void ListBox_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }
    }
}
