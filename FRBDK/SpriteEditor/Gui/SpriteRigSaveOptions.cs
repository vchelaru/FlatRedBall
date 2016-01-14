using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Collections;
using FlatRedBall.IO;
using FlatRedBall.ManagedSpriteGroups;


namespace SpriteEditor.Gui
{
    public class SpriteRigSaveOptions : Window
    {
        #region Fields

        private ComboBox bodyAvailableTextures;
        private TextBox bodyNameToInclude;
        public SpriteList bodySprites;
        private ComboBox bodySpriteSelectionMethod;
        private Button cancelButton;
        private ComboBox jointAvailableTextures;
        private TextBox jointNameToInclude;
        public SpriteList joints;
        private ComboBox jointSpriteSelectionMethod;
        public ToggleButton jointsVisible;
        private GuiMessages messages;
        private Button okButton;
        public List<PoseChain> poseChains;
        public Sprite root;
        private ComboBox rootSpriteComboBox;
        public ToggleButton rootVisible;
        private ComboBox sceneOrGroup;

        #endregion


        #region Delegates

        private void jointSpriteSelectionMethodClicked(Window callingWindow)
        {
            if (((ComboBox)callingWindow).Text == "Name Includes")
            {
                this.jointAvailableTextures.Visible = false;
                this.jointNameToInclude.Visible = true;
            }
            else if (((ComboBox)callingWindow).Text == "By Texture")
            {
                this.jointAvailableTextures.Visible = true;
                this.jointNameToInclude.Visible = false;
                this.fillAvailableTextures(this.jointAvailableTextures);
            }
            else
            {
                this.jointAvailableTextures.Visible = false;
                this.jointNameToInclude.Visible = false;
            }
        }

        private void bodySpriteSelectionMethodClicked(Window callingWindow)
        {
            if (((ComboBox)callingWindow).Text == "Name Includes")
            {
                this.bodyAvailableTextures.Visible = false;
                this.bodyNameToInclude.Visible = true;
            }
            else if (((ComboBox)callingWindow).Text == "By Texture")
            {
                this.bodyAvailableTextures.Visible = true;
                this.bodyNameToInclude.Visible = false;
                this.fillAvailableTextures(this.bodyAvailableTextures);
            }
            else
            {
                this.bodyAvailableTextures.Visible = false;
                this.bodyNameToInclude.Visible = false;
            }
        }

        private void saveButtonClick(Window callingWindow)
        {
            // If a user thinks he's funny and selects AllNotJoint for BodySprites
            // and AllNotBodySprites for Joints, let the user know he has to
            // specify some criteria.
            if (this.bodySpriteSelectionMethod.Text == "All Not Joint" &&
                this.jointSpriteSelectionMethod.Text == "All Not Body")
            {
                GuiManager.ShowMessageBox(
                    "Cannot select All Not Body for Joint Selection when " +
                    "All Not Joint is selected for Body Sprite Selection.  " +
                    "Change one of the criteria and try to save again.", "Selection Error");
                return;

            }



            double error = 0;
            SpriteList possibleSprites = new SpriteList();

            if (this.sceneOrGroup.Text == "Entire Scene")
            {
                possibleSprites = GameData.Scene.Sprites;
            }
            else if (GameData.EditorLogic.CurrentSprites.Count != 0)
            {
                ((Sprite)GameData.EditorLogic.CurrentSprites[0].TopParent).GetAllDescendantsOneWay(possibleSprites);
            }

            #region Get rid of the Axes from the possibleSprites

            if (possibleSprites.Contains(GameData.EditorLogic.EditAxes.xAxis))
            {
                possibleSprites.Remove(GameData.EditorLogic.EditAxes.xAxis);
            }
            if (possibleSprites.Contains(GameData.EditorLogic.EditAxes.yAxis))
            {
                possibleSprites.Remove(GameData.EditorLogic.EditAxes.yAxis);
            }
            if (possibleSprites.Contains(GameData.EditorLogic.EditAxes.zAxis))
            {
                possibleSprites.Remove(GameData.EditorLogic.EditAxes.zAxis);
            }
            if (possibleSprites.Contains(GameData.EditorLogic.EditAxes.xRot))
            {
                possibleSprites.Remove(GameData.EditorLogic.EditAxes.xRot);
            }
            if (possibleSprites.Contains(GameData.EditorLogic.EditAxes.yRot))
            {
                possibleSprites.Remove(GameData.EditorLogic.EditAxes.yRot);
            }
            if (possibleSprites.Contains(GameData.EditorLogic.EditAxes.xScale))
            {
                possibleSprites.Remove(GameData.EditorLogic.EditAxes.xScale);
            }
            if (possibleSprites.Contains(GameData.EditorLogic.EditAxes.yScale))
            {
                possibleSprites.Remove(GameData.EditorLogic.EditAxes.yScale);
            }
            if (possibleSprites.Contains(GameData.EditorLogic.EditAxes.origin))
            {
                possibleSprites.Remove(GameData.EditorLogic.EditAxes.origin);
            }
            #endregion

            if (this.bodySpriteSelectionMethod.Text == "Name Includes")
            {
                this.bodySprites = possibleSprites.FindSpritesWithNameContaining(this.bodyNameToInclude.Text);
            }
            else if (this.bodySpriteSelectionMethod.Text == "By Texture")
            {
                this.bodySprites = possibleSprites.FindSpritesWithTexture(
                    FlatRedBallServices.Load<Texture2D>(this.bodyAvailableTextures.Text, GameData.SceneContentManager));
            }
            else if (this.bodySpriteSelectionMethod.Text == "All")
            {
                this.bodySprites = possibleSprites;
            }

            if (this.jointSpriteSelectionMethod.Text == "Name Includes")
            {
                this.joints = possibleSprites.FindSpritesWithNameContaining(this.jointNameToInclude.Text);
            }
            else if (this.jointSpriteSelectionMethod.Text == "By Texture")
            {
                this.joints = possibleSprites.FindSpritesWithTexture(
                    FlatRedBallServices.Load<Texture2D>(this.jointAvailableTextures.Text, GameData.SceneContentManager));
            }
            else if (this.jointSpriteSelectionMethod.Text == "All")
            {
                this.joints = possibleSprites;
            }

            try
            {
                if (bodySpriteSelectionMethod == null) System.Windows.Forms.MessageBox.Show("a");

                if (this.bodySpriteSelectionMethod.Text == "All Not Joint")
                {
                    this.bodySprites = new SpriteList();

                    if (possibleSprites == null) System.Windows.Forms.MessageBox.Show("b");


                    foreach (Sprite s in possibleSprites)
                    {
                        if (this == null) System.Windows.Forms.MessageBox.Show("c");
                        if (this.joints == null) System.Windows.Forms.MessageBox.Show("d");
                        if (this.bodySprites == null) System.Windows.Forms.MessageBox.Show("e");



                        if (!this.joints.Contains(s))
                        {
                            this.bodySprites.Add(s);
                        }
                    }
                }
            }
            catch { error = 3.3; }
                if (this.jointSpriteSelectionMethod.Text == "All Not Body")
                {
                    this.joints = new SpriteList();
                    foreach (Sprite s in possibleSprites)
                    {
                        if (!this.bodySprites.Contains(s))
                        {
                            this.joints.Add(s);
                        }
                    }
                }

                if (this.rootSpriteComboBox.Text != "<No Root>")
                {
                    this.root = possibleSprites.FindByName(this.rootSpriteComboBox.Text);
                }
                else
                {
                    this.root = null;
                }




            FileButtonWindow.spriteRigOptionsOK(this);
            this.Visible = false;

        }


        #endregion

        // Methods
        public SpriteRigSaveOptions(GuiMessages messages, Cursor cursor)
            : base(cursor)
        {
            this.messages = messages;
            GuiManager.AddWindow(this);
            this.ScaleX = 13f;
            this.ScaleY = 17f;
            base.HasMoveBar = true;
            base.mName = "SpriteRig Options";
            base.HasCloseButton = true;

            TextDisplay tempTextDisplay = new TextDisplay(mCursor);
            AddWindow(tempTextDisplay);
            tempTextDisplay.Text = "Include:";
            tempTextDisplay.SetPositionTL(0.2f, 1.5f);

            this.sceneOrGroup = new ComboBox(mCursor);
            AddWindow(sceneOrGroup);
            this.sceneOrGroup.ScaleX = 8f;
            this.sceneOrGroup.SetPositionTL(10f, 3.5f);
            this.sceneOrGroup.Text = "Entire Scene";
            this.sceneOrGroup.AddItem("Current Group");
            this.sceneOrGroup.AddItem("Entire Scene");

            tempTextDisplay = new TextDisplay(mCursor);
            AddWindow(tempTextDisplay);
            tempTextDisplay.Text = "Body Sprite Selection Includes:";
            tempTextDisplay.SetPositionTL(0.2f, 6f);

            this.bodySpriteSelectionMethod = new ComboBox(mCursor);
            AddWindow(bodySpriteSelectionMethod);
            this.bodySpriteSelectionMethod.ScaleX = 8f;
            this.bodySpriteSelectionMethod.SetPositionTL(10f, 8.5f);
            this.bodySpriteSelectionMethod.Text = "Name Includes";
            this.bodySpriteSelectionMethod.AddItem("Name Includes");
            this.bodySpriteSelectionMethod.AddItem("By Texture");
            this.bodySpriteSelectionMethod.AddItem("All Not Joint");
            this.bodySpriteSelectionMethod.AddItem("All");
            this.bodySpriteSelectionMethod.ItemClick += new GuiMessage(this.bodySpriteSelectionMethodClicked);

            this.bodyAvailableTextures = new ComboBox(mCursor);
            AddWindow(bodyAvailableTextures);
            this.bodyAvailableTextures.ScaleX = 8f;
            this.bodyAvailableTextures.SetPositionTL(10f, 10.5f);
            this.bodyAvailableTextures.Visible = false;

            this.bodyNameToInclude = new TextBox(mCursor);
            AddWindow(bodyNameToInclude);
            this.bodyNameToInclude.ScaleX = 8f;
            this.bodyNameToInclude.SetPositionTL(10f, 10.5f);

            tempTextDisplay = new TextDisplay(mCursor);
            AddWindow(tempTextDisplay);
            tempTextDisplay.Text = "Joint Sprite Selection Includes:";
            tempTextDisplay.SetPositionTL(0.2f, 13f);

            this.jointSpriteSelectionMethod = new ComboBox(mCursor);
            AddWindow(jointSpriteSelectionMethod);
            this.jointSpriteSelectionMethod.ScaleX = 8f;
            this.jointSpriteSelectionMethod.SetPositionTL(10f, 15.5f);
            this.jointSpriteSelectionMethod.Text = "Name Includes";
            this.jointSpriteSelectionMethod.AddItem("Name Includes");
            this.jointSpriteSelectionMethod.AddItem("By Texture");
            this.jointSpriteSelectionMethod.AddItem("All Not Body");
            this.jointSpriteSelectionMethod.AddItem("All");
            this.jointSpriteSelectionMethod.ItemClick += new GuiMessage(this.jointSpriteSelectionMethodClicked);

            this.jointAvailableTextures = new ComboBox(mCursor);
            AddWindow(jointAvailableTextures);
            this.jointAvailableTextures.ScaleX = 8f;
            this.jointAvailableTextures.SetPositionTL(10f, 18f);
            this.jointAvailableTextures.Visible = false;

            this.jointNameToInclude = new TextBox(mCursor);
            AddWindow(jointNameToInclude);
            this.jointNameToInclude.ScaleX = 8f;
            this.jointNameToInclude.SetPositionTL(10f, 18f);

            tempTextDisplay = new TextDisplay(mCursor);
            AddWindow(tempTextDisplay);
            tempTextDisplay.Text = "Root Sprite:";
            tempTextDisplay.SetPositionTL(0.2f, 20);

            this.rootSpriteComboBox = new ComboBox(mCursor);
            AddWindow(rootSpriteComboBox);
            this.rootSpriteComboBox.ScaleX = 8f;
            this.rootSpriteComboBox.SetPositionTL(10f, 22.5f);
            this.rootSpriteComboBox.Text = "<No Root>";

            tempTextDisplay = new TextDisplay(mCursor);
            AddWindow(tempTextDisplay);
            tempTextDisplay.Text = "Sprite Visibility:";
            tempTextDisplay.SetPositionTL(0.2f, 25f);

            this.jointsVisible = new ToggleButton(mCursor);
            AddWindow(jointsVisible);
            this.jointsVisible.SetPositionTL(11f, 27f);
            this.jointsVisible.SetText("Joints Not Visible", "Joints Visible");
            this.jointsVisible.ScaleX = 7.5f;

            this.rootVisible = new ToggleButton(mCursor);
            AddWindow(rootVisible);
            this.rootVisible.SetPositionTL(11f, 29f);
            this.rootVisible.SetText("Root Not Visible", "Root Visible");
            this.rootVisible.ScaleX = 7.5f;

            this.okButton = new Button(mCursor);
            AddWindow(okButton);
            this.okButton.Text = "Save";
            this.okButton.ScaleX = 4.5f;
            this.okButton.ScaleY = 1.3f;
            this.okButton.SetPositionTL(5f, 32f);
            this.okButton.Click += new GuiMessage(this.saveButtonClick);

            this.cancelButton = new Button(mCursor);
            AddWindow(cancelButton);
            this.cancelButton.Text = "Cancel";
            this.cancelButton.ScaleX = 4.5f;
            this.cancelButton.ScaleY = 1.3f;
            this.cancelButton.SetPositionTL(16f, 32f);
            this.cancelButton.Click += new GuiMessage(this.cancelButtonClick);

            this.Visible = false;
        }

        private void cancelButtonClick(Window callingWindow)
        {
            this.Visible = false;
        }

        private void fillAvailableSprites(ComboBox comboBox)
        {
            comboBox.Clear();
            SpriteList possibleSprites = new SpriteList();
            if (this.sceneOrGroup.Text == "Entire Scene")
            {
                possibleSprites = GameData.Scene.Sprites;
            }
            else if (GameData.EditorLogic.CurrentSprites.Count != 0)
            {
                ((Sprite)GameData.EditorLogic.CurrentSprites[0].TopParent).GetAllDescendantsOneWay(possibleSprites);
            }
            comboBox.AddItem("<No Root>");
            foreach (Sprite s in possibleSprites)
            {
                comboBox.AddItem(s.Name);
            }
        }

        private void fillAvailableTextures(ComboBox comboBox)
        {
            comboBox.Clear();
            SpriteList possibleSprites = new SpriteList();
            if (this.sceneOrGroup.Text == "Entire Scene")
            {
                possibleSprites = GameData.Scene.Sprites;
            }
            else if (GameData.EditorLogic.CurrentSprites.Count != 0)
            {
                ((Sprite)GameData.EditorLogic.CurrentSprites[0].TopParent).GetAllDescendantsOneWay(possibleSprites);
            }
            List<Texture2D> ta = new List<Texture2D>();
            foreach (Sprite s in possibleSprites)
            {
                if (!ta.Contains(s.Texture))
                {
                    ta.Add(s.Texture);
                }
            }
            foreach (Texture2D t in ta)
            {
                comboBox.AddItem(FileManager.MakeRelative(t.Name, FileManager.RelativeDirectory));
            }
        }

        public void FillComboBoxes()
        {
            SpriteList possibleSprites = new SpriteList();
            if (this.sceneOrGroup.Text == "Entire Scene")
            {
                possibleSprites = GameData.Scene.Sprites;
            }
            else if (GameData.EditorLogic.CurrentSprites.Count != 0)
            {
                ((Sprite)GameData.EditorLogic.CurrentSprites[0].TopParent).GetAllDescendantsOneWay(possibleSprites);
            }
            this.rootSpriteComboBox.AddItem("<No Root>");
            foreach (Sprite s in possibleSprites)
            {
                this.rootSpriteComboBox.AddItem(s.Name);
            }
            Sprite defaultRoot = possibleSprites.FindByName("root1");
            if (defaultRoot == null)
            {
                defaultRoot = possibleSprites.FindByName("root");
            }
            if (defaultRoot == null)
            {
                defaultRoot = possibleSprites.FindByName("Root");
            }
            if (defaultRoot == null)
            {
                defaultRoot = possibleSprites.FindByName("Root1");
            }
            if (defaultRoot == null)
            {
                defaultRoot = possibleSprites.FindWithNameContaining("root");
            }
            if (defaultRoot == null)
            {
                defaultRoot = possibleSprites.FindWithNameContaining("Root");
            }
            if (defaultRoot != null)
            {
                this.rootSpriteComboBox.Text = defaultRoot.Name;
            }
            else
            {
                this.rootSpriteComboBox.Text = "<No Root>";
            }
        }
    }


}
