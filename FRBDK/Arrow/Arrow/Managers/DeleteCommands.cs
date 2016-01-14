using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using FlatRedBall.Arrow.DataTypes;
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.Content.Scene;
using FlatRedBall.Content.SpriteFrame;
using FlatRedBall.Arrow.GlueView;

namespace FlatRedBall.Arrow.Managers
{
    public class DeleteCommands
    {
        #region Fields

        MenuItem mMenuItem;

        #endregion

        public void Initialize(MenuItem deleteMenuItem)
        {
            mMenuItem = deleteMenuItem;

        }

        public void PopulateDeleteMenuFromArrowState()
        {
            mMenuItem.Items.Clear();

            if (ArrowState.Self.CurrentArrowElementSave != null)
            {
                // Add delete element save
                MenuItem menuItem = new MenuItem();
                mMenuItem.Items.Add(menuItem);

                menuItem.Header = ArrowState.Self.CurrentArrowElementSave.ToString();

                menuItem.Click += HandleDeleteElementClick;
            }

            if (ArrowState.Self.CurrentInstance != null)
            {
                // Add delete intance
                MenuItem menuItem = new MenuItem();
                mMenuItem.Items.Add(menuItem);
                if (string.IsNullOrEmpty(ArrowState.Self.CurrentInstanceName))
                {
                    menuItem.Header = "Unnamed Item";
                }
                else
                {
                    menuItem.Header = ArrowState.Self.CurrentInstanceName;
                }
                menuItem.Click += HandleDeleteInstanceClick;
            }

        }

        private void HandleDeleteElementClick(object sender, System.Windows.RoutedEventArgs e)
        {
            DeleteCurrentElement();
        }


        private void HandleDeleteInstanceClick(object sender, System.Windows.RoutedEventArgs e)
        {
            DeleteCurrentInstance();


        }


        public void DeleteCurrentElement()
        {
            ArrowProjectSave arrowProject = ArrowState.Self.CurrentArrowProject;

            ArrowElementSave element = ArrowState.Self.CurrentArrowElementSave;

            ///////////////////Early Out///////////////////////
            if (arrowProject == null || element == null)
            {
                return;
            }
            /////////////////End Early Out/////////////////////

            arrowProject.Elements.Remove(element);

            ArrowCommands.Self.File.GenerateGlux();
            ArrowCommands.Self.File.SaveProject();

            //ArrowCommands.Self.GuiCommands.RefreshSingleElementTreeView();
            ArrowCommands.Self.View.RefreshToCurrentElement();

            ArrowState.Self.CurrentArrowProjectVm.Refresh();
        }

        public void DeleteCurrentInstance()
        {
            ArrowProjectSave arrowProject = ArrowState.Self.CurrentArrowProject;

            ArrowElementSave element = ArrowState.Self.CurrentArrowElementSave;

            object instance = ArrowState.Self.CurrentInstance;

            ///////////////////Early Out///////////////////////
            if (arrowProject == null || instance == null)
            {
                return;
            }
            /////////////////End Early Out/////////////////////

            if (instance is SpriteSave)
            {
                element.Sprites.Remove(instance as SpriteSave);
            }
            else if (instance is CircleSave)
            {
                element.Circles.Remove(instance as CircleSave);
            }
            else if (instance is AxisAlignedRectangleSave)
            {
                element.Rectangles.Remove(instance as AxisAlignedRectangleSave);
            }
            else if (instance is SpriteFrameSave)
            {
                element.SpriteFrameSaves.Remove(instance as SpriteFrameSave);
            }
            else if (instance is ArrowElementInstance)
            {
                element.ElementInstances.Remove(instance as ArrowElementInstance);
            }
            else
            {
                throw new NotImplementedException("Removal of the type " + instance.GetType().Name + " needs to be implemented");
            }

            ArrowCommands.Self.File.GenerateGlux();
            ArrowCommands.Self.File.SaveProject();

            ArrowCommands.Self.UpdateToSelectedElement();
        }
    }
}
