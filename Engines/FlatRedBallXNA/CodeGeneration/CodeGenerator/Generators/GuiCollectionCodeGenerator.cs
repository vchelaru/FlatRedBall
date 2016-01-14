using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.CodeGeneration;
using FlatRedBall.Gui;
#if FRB_XNA
using Microsoft.Xna.Framework;
using FlatRedBall.Instructions.Reflection;
#elif FRB_MDX
using Microsoft.DirectX;
#endif

namespace CodeGenerator.Generators
{
    public static class GuiCollectionCodeGenerator
    {
        public static void CreateGuiSaveClasses()
        {
            List<SaveClassOptions> optionList = new List<SaveClassOptions>();

            #region Window
            SaveClassOptions options = new SaveClassOptions(typeof(Window));

            options.MembersToExclude.Add("SpriteFrame");
            options.MembersToExclude.Add("CurrentChain");
            //options.MembersToExclude.Add("Parent");
            //options.MembersToExclude.Add("FloatingParent");
            options.MembersToExclude.Add("NextInTabSequence");
            options.MembersToExclude.Add("GuiManagerDrawn");

            // not supported at the time of this writing
            options.MembersToExclude.Add("AnimationSpeed");
            options.MembersToExclude.Add("AnimationChains");
            options.MembersToExclude.Add("CurrentChainIndex");
            options.MembersToExclude.Add("CurrentChainName");
            options.MembersToExclude.Add("CurrentFrameIndex");
            options.MembersToExclude.Add("UseAnimationRelativePosition");
            /////////////////////////////////////////////
            options.MembersToMakeOptional.Add("Visible");

            /////////////////////////////////////////////
            options.MembersToExcludeFromToRuntime.Add("Parent");
            options.MembersToExcludeFromToRuntime.Add("FloatingParent");

            options.NewFieldsInSaveOnly.Add("public static bool ApplyVisible = true;");

            options.NewFieldsInSaveOnly.Add("public static bool ApplyMinimumScales = true;");

            options.UseCommonNamedTypes = true;

            options.MethodCode.Add(
                "FloatingChildren",
                new MethodCode(
                    "List<Window> windowList",
                    "throw new NotImplementedException();"));

            options.MethodCode.Add(
                "Children",
                new MethodCode(
                    "List<Window> windowList",
                    "throw new NotImplementedException();"));

            optionList.Add(options);

            #endregion

            #region CollapseWindow

            SaveClassOptions collapseWindowOptions = new SaveClassOptions(typeof(CollapseWindow));
            optionList.Add(collapseWindowOptions);

            #endregion

            #region Button

            SaveClassOptions buttonOptions = new SaveClassOptions(typeof(Button));
            optionList.Add(buttonOptions);

            #endregion

            #region ListBoxBase

            SaveClassOptions listBoxBaseOptions = new SaveClassOptions(typeof(ListBoxBase));

            listBoxBaseOptions.MembersToExclude.Add("Font");
            listBoxBaseOptions.MembersToExclude.Add("HighlightBar");
            listBoxBaseOptions.MembersToExclude.Add("NextInTabSequence");
            listBoxBaseOptions.MembersToExclude.Add("Items");

            optionList.Add(listBoxBaseOptions);

            #endregion

            #region ListBox

            optionList.Add(new SaveClassOptions(typeof(ListBox)));

            #endregion

            #region CollapseListBox

            optionList.Add(new SaveClassOptions(typeof(CollapseListBox)));

            #endregion

            #region ColorDisplay


            SaveClassOptions colorDisplaySaveClassOptions = new SaveClassOptions(typeof(ColorDisplay));

            colorDisplaySaveClassOptions.MembersToExclude.Add("ValueChanged");
            colorDisplaySaveClassOptions.MembersToExclude.Add("ColorValue");
            optionList.Add(colorDisplaySaveClassOptions);

            #endregion

            #region ComboBox

            SaveClassOptions comboBoxSaveClassOptions = new SaveClassOptions(typeof(ComboBox));

            comboBoxSaveClassOptions.MembersToExclude.Add("SelectedObject");

            optionList.Add(comboBoxSaveClassOptions);

            #endregion

            #region TimeLine

            optionList.Add(new SaveClassOptions(typeof(TimeLine)));

            #endregion

            #region MarkerTimeLine

            SaveClassOptions markerTimeLineSaveClassOptions = new SaveClassOptions(typeof(MarkerTimeLine));

            markerTimeLineSaveClassOptions.MembersToExclude.Add("MarkerClicked");

            optionList.Add(markerTimeLineSaveClassOptions);

            #endregion

            #region ScrollBar

            optionList.Add(new SaveClassOptions(typeof(ScrollBar)));

            #endregion

            #region TextBox

            SaveClassOptions textBoxSaveClassOptions = new SaveClassOptions(typeof(TextBox));
            textBoxSaveClassOptions.MembersToExclude.Add("CursorSprite");
            textBoxSaveClassOptions.MembersToExclude.Add("Font");
            optionList.Add(textBoxSaveClassOptions);

            #endregion

            #region TextDisplay

            optionList.Add(new SaveClassOptions(typeof(TextDisplay)));

            #endregion

            #region ToggleButton

            SaveClassOptions toggleButtonSaveClassOptions = 
                new SaveClassOptions(typeof(ToggleButton));

            toggleButtonSaveClassOptions.MembersToExclude.Add("radioGroup");

            optionList.Add(toggleButtonSaveClassOptions);

            #endregion

            #region UpDown
            SaveClassOptions upDownSaveClassOptions = new SaveClassOptions(typeof(UpDown));
            upDownSaveClassOptions.MembersToExclude.Add("textBox");
            upDownSaveClassOptions.MembersToExclude.Add("UpDownButton");
            optionList.Add(upDownSaveClassOptions);

            #endregion

            #region Vector3Display

            SaveClassOptions vector3DisplaySaveClassOptions = new SaveClassOptions(typeof(Vector3Display));
            vector3DisplaySaveClassOptions.MembersToExclude.Add("ValueChanged");
            vector3DisplaySaveClassOptions.MembersToExclude.Add("AfterValueChanged");
            optionList.Add(vector3DisplaySaveClassOptions);

            #endregion

            // When adding new types, be sure to update the WindowSaveCollection.FromRuntime method!

            foreach (SaveClassOptions sco in optionList)
            {
                sco.Namespace = "FlatRedBall.Content.Gui";
                sco.TypesToNotFullyQualify.Add(typeof(Vector3).FullName);
                sco.TypesToNotFullyQualify.Add(typeof(Vector2).FullName);
                sco.TypesToNotFullyQualify.Add(typeof(Microsoft.Xna.Framework.Graphics.Texture2D).FullName);
            }

            SaveClassCodeGenerator sccg = new SaveClassCodeGenerator();
            bool isTesting = false;
            if (isTesting)
            {
                sccg.CreateSaveClasses(optionList, @"V:\FlatRedBall\Content\GuiTEST");
            }
            else
            {
                sccg.CreateSaveClasses(optionList, @"V:\FlatRedBall\Content\Gui");
            }
        }
    }
}
