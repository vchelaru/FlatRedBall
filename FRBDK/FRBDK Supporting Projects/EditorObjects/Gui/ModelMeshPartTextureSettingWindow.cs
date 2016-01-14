#if !FRB_MDX

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.Content.Model.Helpers;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using FlatRedBall;
using FlatRedBall.Graphics.Model;
using FlatRedBall.Graphics;

namespace EditorObjects.Gui
{
    public class ModelMeshPartTextureSettingWindow : Window, IObjectDisplayer<CustomModel>
    {
        #region Fields

        ComboBox mComboBox;
        Button mTextureDisplayButton;

        Button mSetAllToDefaultButton;
        Button mSetAllToNullButton;

        TextDisplay mCustomOrDefault;

        CustomModel mCustomModel;



        #endregion

        #region Properties

        public PositionedModel ContainingModel
        {
            get;
            set;
        }

        public CustomModel ObjectDisplaying
        {
            get
            {
                return mCustomModel;
            }
            set
            {
                mCustomModel = value;

                UpdateUiToObject();
            }
        }

        public object ObjectDisplayingAsObject
        {
            get
            {
                return ObjectDisplaying;
            }
            set
            {

                ObjectDisplaying = (CustomModel)value;
            }
        }
        #endregion

        #region Event Methods

        void MeshPartSelected(Window callingWindow)
        {
            ModelMeshPart mmp = mComboBox.SelectedObject as ModelMeshPart;



            UpdateButtonTexture(mmp);

            UpdateDefaultOrCustom();
        }

        private void UpdateButtonTexture(ModelMeshPart mmp)
        {
            if (mmp != null)
            {
                Texture2D textureToSet = mmp.Texture;

                if (ContainingModel.RenderOverrides.ContainsKey(mmp))
                {
                    if (ContainingModel.RenderOverrides[mmp].Count != 0)
                    {

                        textureToSet = ContainingModel.RenderOverrides[mmp][0].AlternateTexture;
                    }
                }



                if (ContainingModel != null)
                {
                    mTextureDisplayButton.SetOverlayTextures(
                        textureToSet,
                        textureToSet);

                }
            }
        }

        void OnTextureButtonClick(Window callingWindow)
        {
            
            FileWindow fileWindow = GuiManager.AddFileWindow();
            fileWindow.SetFileType(FileWindow.FileWindowTypes.Graphics);
            fileWindow.OkClick += new GuiMessage(OnTextureLoadOk);

        }

        void OnTextureLoadOk(Window callingWindow)
        {
            string fileName = ((FileWindow)callingWindow).Results[0];

            Texture2D texture = FlatRedBallServices.Load<Texture2D>(fileName);

            mTextureDisplayButton.SetOverlayTextures(
                texture,
                texture);

            SetTextureOnCurrentModelMeshPart(texture);
        }

        private void SetTextureOnCurrentModelMeshPart(Texture2D texture)
        {
            if (mComboBox.SelectedObject != null)
            {
                ModelMeshPart mmp = mComboBox.SelectedObject as ModelMeshPart;


                if (!ContainingModel.RenderOverrides.ContainsKey(mmp))
                {
                    ContainingModel.RenderOverrides.Add(mmp, new List<RenderOverrides>());
                }

                List<RenderOverrides> overrides = ContainingModel.RenderOverrides[mmp];
                if (overrides.Count == 0)
                {
                    overrides.Add(new RenderOverrides());
                }

                overrides[0].AlternateTexture = texture;

                overrides[0].AllowNullTexture = texture == null;


                UpdateDefaultOrCustom();
            }
        }

        void ButtonSecondaryClick(Window callingWindow)
        {
            ListBox listBox = GuiManager.AddPerishableListBox();

            listBox.AddItem("Set to default");
            listBox.AddItem("Set to null");
            listBox.ScrollBarVisible = false;
            listBox.SetScaleToContents(0);

            listBox.HighlightOnRollOver = true;

            listBox.Click += new GuiMessage(RightClickMenuClick);

            GuiManager.PositionTopLeftToCursor(listBox);

        }

        void RightClickMenuClick(Window callingWindow)
        {
            List<string> a = ((ListBox)callingWindow).GetHighlighted();

            ModelMeshPart mmp = mComboBox.SelectedObject as ModelMeshPart;

            if (a.Count != 0)
            {

                switch (a[0])
                {
                    case "Set to default":
                        if (ContainingModel.RenderOverrides.ContainsKey(mmp))
                        {
                            if (ContainingModel.RenderOverrides[mmp].Count != 0)
                            {
                                ContainingModel.RenderOverrides[mmp].Clear();
                            }
                        }


                        break;

                    case "Set to null":
                        SetTextureOnCurrentModelMeshPart(null);
                        break;
                }
            }

            UpdateButtonTexture(mmp);
            UpdateDefaultOrCustom();
            callingWindow.Visible = false;
        }

        void OnSetAllToDefaultClick(Window callingWindow)
        {
            if (ObjectDisplaying != null)
            {
                foreach (ModelMesh modelMesh in ObjectDisplaying.Meshes)
                {
                    foreach (ModelMeshPart part in modelMesh.MeshParts)
                    {
                        if (ContainingModel.RenderOverrides.ContainsKey(part))
                        {
                            ContainingModel.RenderOverrides[part].Clear();
                        }                       
                    }

                }

                ModelMeshPart mmp = mComboBox.SelectedObject as ModelMeshPart;
                UpdateButtonTexture(mmp);
                UpdateDefaultOrCustom();
            }
        }

        void OnSetAllToNullClick(Window callingWindow)
        {
            if (ObjectDisplaying != null)
            {
                foreach (ModelMesh modelMesh in ObjectDisplaying.Meshes)
                {
                    foreach (ModelMeshPart part in modelMesh.MeshParts)
                    {

                        if (!ContainingModel.RenderOverrides.ContainsKey(part))
                        {
                            ContainingModel.RenderOverrides.Add(part, new List<RenderOverrides>());
                        }

                        List<RenderOverrides> overrides = ContainingModel.RenderOverrides[part];
                        if (overrides.Count == 0)
                        {
                            overrides.Add(new RenderOverrides());
                        }

                        overrides[0].AlternateTexture = null;

                        overrides[0].AllowNullTexture = true;
                    }
                }
                ModelMeshPart mmp = mComboBox.SelectedObject as ModelMeshPart;
                UpdateButtonTexture(mmp);
                UpdateDefaultOrCustom();

            }
        }

        #endregion

        #region Methods

        public ModelMeshPartTextureSettingWindow(Cursor cursor)
            : base(cursor)
        {
            this.ScaleX = 7;
            this.ScaleY = 11f;

            #region Create the Combo Box
            mComboBox = new ComboBox(cursor);
            this.AddWindow(mComboBox);
            mComboBox.ScaleX = 6.5f;
            mComboBox.Y = 2;
            mComboBox.ItemClick += new GuiMessage(MeshPartSelected);

            #endregion

            #region Texture Display Button

            mTextureDisplayButton = new Button(cursor);
            this.AddWindow(mTextureDisplayButton);
            mTextureDisplayButton.ScaleX = 5;
            mTextureDisplayButton.ScaleY = 5;
            mTextureDisplayButton.Y = mComboBox.Y + 1.5f + mTextureDisplayButton.ScaleY;

            mTextureDisplayButton.Click += new GuiMessage(OnTextureButtonClick);
            mTextureDisplayButton.SecondaryClick += new GuiMessage(ButtonSecondaryClick);

            #endregion

            #region Create the Custom or Default Label

            mCustomOrDefault = new TextDisplay(cursor);
            this.AddWindow(mCustomOrDefault);
            mCustomOrDefault.Y = mTextureDisplayButton.Y + 1.5f + mTextureDisplayButton.ScaleY;
            mCustomOrDefault.Text = "Default";
            mCustomOrDefault.X = 1;

            #endregion

            #region Set the AllToDefault Button

            mSetAllToDefaultButton = new Button(cursor);
            this.AddWindow(mSetAllToDefaultButton);

            mSetAllToDefaultButton.Text = "Set all default";
            mSetAllToDefaultButton.ScaleX = ScaleX - .5f;
            mSetAllToDefaultButton.ScaleY = 1.3f;
            mSetAllToDefaultButton.Y = mCustomOrDefault.Y + 1.5f + mSetAllToDefaultButton.ScaleY;
            mSetAllToDefaultButton.Click += new GuiMessage(OnSetAllToDefaultClick);

            #endregion

            mSetAllToNullButton = new Button(cursor);
            this.AddWindow(mSetAllToNullButton);

            mSetAllToNullButton.Text = "Set all null";
            mSetAllToNullButton.ScaleX = ScaleX - .5f;
            mSetAllToNullButton.ScaleY = 1.3f;
            mSetAllToNullButton.Y = mSetAllToDefaultButton.Y + 1.5f + mSetAllToNullButton.ScaleY;
            mSetAllToNullButton.Click += new GuiMessage(OnSetAllToNullClick);

        }

       

        public void UpdateToObject()
        {
            

        }

        private void UpdateUiToObject()
        {

                mComboBox.Clear();

                int index = 0;

                if (mCustomModel != null)
                {
                    foreach (ModelMesh modelMesh in mCustomModel.Meshes)
                    {
                        foreach (ModelMeshPart modelMeshPart in modelMesh.MeshParts)
                        {
                            mComboBox.AddItem("Part " + index.ToString(), modelMeshPart);
                            index++;
                        }
                    }
                }
        }

        private void UpdateDefaultOrCustom()
        {

            ModelMeshPart mmp = null;

            if (mComboBox != null)
            {
                mmp = mComboBox.SelectedObject as ModelMeshPart;
            }

            if (mmp == null)
            {
                mCustomOrDefault.Text = "";
            }
            else
            {
                Texture2D textureToSet = mmp.Texture;

                bool wasSet = false;

                if (ContainingModel.RenderOverrides.ContainsKey(mmp))
                {
                    if (ContainingModel.RenderOverrides[mmp].Count != 0)
                    {
                        wasSet = true;
                        mCustomOrDefault.Text = "Custom";
                    }
                }
                
                if(!wasSet)
                {
                    mCustomOrDefault.Text = "Default";
                }
            }
        }

        #endregion

    }
}
#endif