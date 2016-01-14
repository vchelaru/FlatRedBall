using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using FlatRedBall.Arrow.DataTypes;
using FlatRedBall.Arrow.Gui;
using FlatRedBall.Arrow.Verification;
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.Content.Scene;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Utilities;
using System.Windows.Controls;

namespace FlatRedBall.Arrow.Managers
{
    public class AddCommands
    {

        public ArrowElementSave Element()
        {
            TextInputWindow tiw = new TextInputWindow();

            tiw.Text = "Enter new element name:";

            List<string> intentNames = new List<string>();

            const string noIntent = "<NO INTENT>";

            intentNames.Add(noIntent);
            foreach (var item in ArrowState.Self.CurrentArrowProject.Intents)
            {
                intentNames.Add(item.Name);
            }



            var treeView = tiw.AddTreeView(intentNames);


            var result = tiw.ShowDialog();

            if (result.HasValue && result.Value)
            {

                ArrowElementSave toReturn = new ArrowElementSave();
                toReturn.Name = tiw.Result;
                ArrowProjectSave projectToAddTo = ArrowState.Self.CurrentArrowProject;

                if (treeView.SelectedItem as string != noIntent)
                {
                    toReturn.Intent = treeView.SelectedItem as string;

                    ArrowIntentSave intent = new ArrowIntentSave();
                    IntentManager.Self.AddRequirementsForIntent(toReturn, intent);
                }

                projectToAddTo.Elements.Add(toReturn);

                ArrowCommands.Self.File.SaveProject();
                ArrowCommands.Self.File.GenerateGlux();
                ArrowState.Self.CurrentArrowProjectVm.Refresh();

                return toReturn;
            }
            else
            {
                return null;
            }
        }

        internal void Sprite()
        {
            if (ArrowState.Self.CurrentArrowElementSave != null)
            {
                
                TextInputWindow tiw = new TextInputWindow();

                tiw.Text = "Enter new Sprite name:";
                tiw.Result = "Sprite";
                var result = tiw.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    bool isInvalid = CheckAndShowMessageIfInvalid(tiw.Result);

                    if (!isInvalid)
                    {
                        SpriteSave spriteSave = new SpriteSave();
                        spriteSave.ScaleX = 16;
                        spriteSave.ScaleY = 16;
                        spriteSave.Name = tiw.Result;
                        spriteSave.ColorOperation = "Color";

                        spriteSave.TintRed = 255;
                        spriteSave.TintGreen = 255;

                        ArrowState.Self.CurrentArrowElementSave.Sprites.Add(spriteSave);

                        AfterAddLogic(ArrowState.Self.CurrentArrowElementSave, spriteSave);
                    }
                }
            }
        }

        internal void Circle()
        {
            if (ArrowState.Self.CurrentArrowElementSave != null)
            {

                TextInputWindow tiw = new TextInputWindow();

                tiw.Text = "Enter new Circle name:";
                tiw.Result = "Circle";

                var result = tiw.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    bool isInvalid = CheckAndShowMessageIfInvalid(tiw.Result);

                    if (!isInvalid)
                    {
                        CircleSave circleSave = new CircleSave();
                        circleSave.Radius = 16;
                        circleSave.Name = tiw.Result;

                        ArrowState.Self.CurrentArrowElementSave.Circles.Add(circleSave);

                        AfterAddLogic(ArrowState.Self.CurrentArrowElementSave, circleSave);
                    }
                }
            }

        }

        internal void Rectangle()
        {
            if (ArrowState.Self.CurrentArrowElementSave != null)
            {

                TextInputWindow tiw = new TextInputWindow();

                tiw.Text = "Enter new Rectangle name:";
                tiw.Result = "Rectangle";
                var result = tiw.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    bool isInvalid = CheckAndShowMessageIfInvalid(tiw.Result);

                    if (!isInvalid)
                    {
                        AxisAlignedRectangleSave rectangleSave = new AxisAlignedRectangleSave();
                        rectangleSave.ScaleX = 16;
                        rectangleSave.ScaleY = 16;
                        rectangleSave.Name = tiw.Result;

                        ArrowState.Self.CurrentArrowElementSave.Rectangles.Add(rectangleSave);

                        AfterAddLogic(ArrowState.Self.CurrentArrowElementSave, rectangleSave);
                    }
                }
            }

        }

        internal void NewFile()
        {
            if (ArrowState.Self.CurrentArrowElementSave != null)
            {
                TextInputWindow tiw = new TextInputWindow();

                tiw.Text = "Enter new File name:";



                List<string> toAddToTreeView = new List<string>();
                toAddToTreeView.Add("Scene (.scnx)");
                tiw.AddTreeView(toAddToTreeView);

                var result = tiw.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    SceneSave sceneSave = new SceneSave();
                //    bool isInvalid = CheckAndShowMessageIfInvalid(tiw.Result);

                //    if (!isInvalid)
                //    {
                //        SpriteSave spriteSave = new SpriteSave();
                //        spriteSave.ScaleX = 16;
                //        spriteSave.ScaleY = 16;
                //        spriteSave.Name = tiw.Result;
                //        spriteSave.ColorOperation = "Color";

                //        spriteSave.TintRed = 255;
                //        spriteSave.TintGreen = 255;

                //        ArrowState.Self.CurrentArrowElementSave.Sprites.Add(spriteSave);

                //        AfterAddLogic(ArrowState.Self.CurrentArrowElementSave, spriteSave);
                //    }
                }
            }
        }

        internal void ElementInstance()
        {
            if (ArrowState.Self.CurrentArrowElementSave != null)
            {
                //Show a text input window for the name, but add a combo box so the user can select the type
                TextInputWindow tiw = new TextInputWindow();

                TreeView treeView = new TreeView();
                treeView.HorizontalAlignment = HorizontalAlignment.Stretch;
                treeView.VerticalAlignment = VerticalAlignment.Top;
                treeView.Height = 80;
                treeView.Margin = new Thickness(3);

                List<ArrowElementSave> toAddToTreeView = new List<ArrowElementSave>();

                foreach (var elementSave in ArrowState.Self.CurrentArrowProject.Elements)
                {
                    if (elementSave != null && elementSave != ArrowState.Self.CurrentArrowElementSave)
                    {
                        toAddToTreeView.Add(elementSave);
                    }
                }

                treeView.ItemsSource = toAddToTreeView;

                tiw.AddControl(treeView);



                bool? result = tiw.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    ArrowElementSave typeToAdd = treeView.SelectedItem as ArrowElementSave;

                    string name = tiw.Result;

                    ElementInstance(name, typeToAdd);
                }
                //tiw.AddControl
            }
        }


        private bool CheckAndShowMessageIfInvalid(string name)
        {
            string whyIsnt;
            if (!NameVerifier.Self.IsInstanceNameValid(name, out whyIsnt))
            {
                MessageBox.Show(whyIsnt);
            }

            return string.IsNullOrEmpty(whyIsnt) == false;
        }


        private void AfterAddLogic(ArrowElementSave arrowElement, object newObject)
        {
            MakeNewObjectUnique(arrowElement, newObject);
            ArrowCommands.Self.File.SaveProject();
            ArrowCommands.Self.File.GenerateGlux();

            ArrowState.Self.CurrentArrowElementVm.Refresh();

            ArrowCommands.Self.UpdateToSelectedElement();
            //ArrowCommands.Self.GuiCommands.RefreshSingleElementTreeView();
        }


        public void MakeNewObjectUnique(DataTypes.ArrowElementSave container, object newObject)
        {
            string name = LateBinder.GetValueStatic(newObject, "Name") as string;

            List<string> allNames = new List<string>();
            foreach (var item in container.AllInstances)
            {
                if (item != newObject)
                {
                    allNames.Add(LateBinder.GetValueStatic(item, "Name") as string);
                }
            }

            name = StringFunctions.MakeStringUnique(name, allNames);

            LateBinder.SetValueStatic(newObject, "Name", name);
        }




        public ArrowElementInstance ElementInstance(string name, ArrowElementSave typeToAdd)
        {
            ArrowElementInstance instance = new ArrowElementInstance();
            instance.Name = name;
            instance.Type = typeToAdd.Name;

            ArrowState.Self.CurrentArrowElementSave.ElementInstances.Add(instance);

            ArrowCommands.Self.File.SaveProject();
            ArrowCommands.Self.File.GenerateGlux();

            ArrowCommands.Self.UpdateToSelectedElement();

            ArrowState.Self.CurrentInstance = instance;

            return instance;
        }

    }
}
