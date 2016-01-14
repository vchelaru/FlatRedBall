using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Model;

using EditorObjects.SaveClasses;

#if !FRB_MDX
using FlatRedBall.Graphics.Animation3D;
#endif
using FlatRedBall.IO;

namespace EditorObjects.Gui
{
    public class PositionedModelPropertyGrid : PropertyGrid<PositionedModel>
    {
        #region Fields

        ComboBox mCurrentAnimationComboBox;

        Button mAddAnimationButton;
        Button mRemoveAllAnimationDataButton;

        ListDisplayWindow mAnimationListDisplayWindow;

        FileTextBox mHierarchyFileTextBox;


        #endregion

        #region Properties

        public static List<BuildToolAssociation> BuildToolAssociationList
        {
            get;
            set;
        }


        public override PositionedModel SelectedObject
        {
            get
            {
                return base.SelectedObject;
            }
            set
            {
                base.SelectedObject = value;



                if (!Visible && (SelectedObject != null))
                {
                    GuiManager.BringToFront(this);

                }
                Visible = (SelectedObject != null);

            }
        }

        #endregion    
    
        #region Event Methods

        void AddAnimationClick(Window callingWindow)
        {
            FileWindow fileWindow = GuiManager.AddFileWindow();

            List<string> fileTypes = new List<string>(2);
            fileTypes.Add("waa"); 
            fileTypes.Add("wam");


            fileWindow.SetFileType(fileTypes);

            fileWindow.SetToLoad();
            fileWindow.OkClick += new GuiMessage(AddAnimationOK);
        }

        void AddAnimationOK(Window callingWindow)
        {
#if !FRB_MDX
            string sourceFile = ((FileWindow)callingWindow).Results[0];
            string destinationFile = "";
            string extension = FileManager.GetExtension(sourceFile);

            BuildToolAssociation toolForExtension = GetToolForExtension(extension);

            if (toolForExtension != null)
            {
                destinationFile =
                    FileManager.UserApplicationDataForThisApplication +
                    FileManager.RemovePath(FileManager.RemoveExtension(sourceFile)) + "." + toolForExtension.DestinationFileType;

                toolForExtension.PerformBuildOn(sourceFile, destinationFile, null, null, null);
            }
            else
            {
                destinationFile = sourceFile;
            }


            mAnimationListDisplayWindow.UpdateToList();
#endif
        }
#if FRB_XNA
        void RemoveAllAnimationButtonClick(Window callingWindow)
        {
            OkCancelWindow window = GuiManager.ShowOkCancelWindow("Are you sure you want to remove all animation information from this model?" + 
                "All objects attached to any joints will be detached.", "Remove anim info");
            window.OkText = "Yes";
            window.CancelText = "No";

            window.OkClick += new GuiMessage(OnRemoveAllAnimationOk);
        }


        void OnRemoveAllAnimationOk(Window callingWindow)
        {
            SelectedObject.Animate = false;

            for (int i = SelectedObject.Children.Count - 1; i > -1; i--)
            {
                PositionedObject child = SelectedObject.Children[i];

                if (!string.IsNullOrEmpty(child.ParentBone))
                {
                    child.Detach();
                }
            }
        }
#endif

        void ShowAnimationPropertyGrid(Window callingWindow)
        {
            GuiManager.ObjectDisplayManager.GetObjectDisplayerForObject(mAnimationListDisplayWindow.GetFirstHighlightedObject());
        }

        void StrongSelectAnimation(Window callingWindow)
        {
        }


        void LoadSkeleton(Window callingWindow)
        {
#if !FRB_MDX
            FileTextBox fileTextBox = callingWindow as FileTextBox;

            string sourceFile = fileTextBox.Text;
            string destinationFile = "";

            string extension = FileManager.GetExtension(sourceFile);
            
            BuildToolAssociation toolForExtension = GetToolForExtension(extension);
            
            if (toolForExtension != null)
            {
                destinationFile =
                    FileManager.UserApplicationDataForThisApplication +
                    FileManager.RemovePath(FileManager.RemoveExtension(sourceFile)) + "." + toolForExtension.DestinationFileType;

                toolForExtension.PerformBuildOn(sourceFile, destinationFile, null, null, null);
            }
            else
            {
                destinationFile = sourceFile;
            }

#endif
        }

        #endregion

        #region Methods

        #region Constructor

        public PositionedModelPropertyGrid(Cursor cursor)
            : base(cursor)
        {
            ExcludeAllMembers();

            #region Include Basic Members

            IncludeMember("X", "Basic");
            IncludeMember("Y", "Basic");
            IncludeMember("Z", "Basic");



            IncludeMember("RotationX", "Basic");
            IncludeMember("RotationY", "Basic");
            IncludeMember("RotationZ", "Basic");

//            IncludeMember("Visible", "Basic");
            IncludeMember("CursorSelectable", "Basic");

            IncludeMember("Name", "Basic");

            IncludeMember("IsAutomaticallyUpdated", "Basic");

            IncludeMember("Visible", "Basic");

            #endregion

            #region Include "Scale" members

            IncludeMember("ScaleX", "Scale");
            IncludeMember("ScaleY", "Scale");
            IncludeMember("ScaleZ", "Scale");

#if !FRB_MDX
            IncludeMember("FlipX", "Scale");
            IncludeMember("FlipY", "Scale");
            IncludeMember("FlipZ", "Scale");
#endif

            #endregion

            #region Include "Rendering" members

            IncludeMember("FaceCullMode", "Rendering");

            #endregion

            #region Include Relative Members

            IncludeMember("RelativeX", "Relative");
            IncludeMember("RelativeY", "Relative");
            IncludeMember("RelativeZ", "Relative");

            IncludeMember("RelativeRotationX", "Relative");
            IncludeMember("RelativeRotationY", "Relative");
            IncludeMember("RelativeRotationZ", "Relative");

            #endregion

            #region Include Animation Members

#if FRB_XNA
            //IncludeMember("HasAnimation", "Animation");
            //IncludeMember("Animate", "Animation");
            //IncludeMember("CurrentAnimation", "Animation");

            //mCurrentAnimationComboBox = new ComboBox(GuiManager.Cursor);
            //mCurrentAnimationComboBox.ScaleX = 6;
            //ReplaceMemberUIElement("CurrentAnimation", mCurrentAnimationComboBox);

            mHierarchyFileTextBox = new FileTextBox(GuiManager.Cursor);
            mHierarchyFileTextBox.ScaleY = 1.5f;
            mHierarchyFileTextBox.ScaleX = 15;
            List<string> fileTypes = new List<string>();
            fileTypes.Add("wbi");
            fileTypes.Add("whe");
            mHierarchyFileTextBox.SetFileType(fileTypes);
            this.AddWindow(mHierarchyFileTextBox, "Animation");
            mHierarchyFileTextBox.FileSelect += new GuiMessage(LoadSkeleton);




            mAnimationListDisplayWindow = new ListDisplayWindow(cursor);
            mAnimationListDisplayWindow.ScaleX = 15;
            mAnimationListDisplayWindow.ScaleY = 15;
            mAnimationListDisplayWindow.ListBox.StrongSelect += new GuiMessage(StrongSelectAnimation);
            mAnimationListDisplayWindow.ListBox.Highlight += new GuiMessage(ShowAnimationPropertyGrid);
            this.AddWindow(mAnimationListDisplayWindow, "Animation");



            mAddAnimationButton = new Button(GuiManager.Cursor);
            mAddAnimationButton.Text = "Add Animation";
            mAddAnimationButton.ScaleX = 8.5f;
            mAddAnimationButton.ScaleY = 1.5f;
            this.AddWindow(mAddAnimationButton, "Animation");
            mAddAnimationButton.Click += new GuiMessage(AddAnimationClick);

            mRemoveAllAnimationDataButton = new Button(cursor);
            mRemoveAllAnimationDataButton.Text = "Remove All Anim Data";
            mRemoveAllAnimationDataButton.ScaleX = 8.5f;
            mRemoveAllAnimationDataButton.ScaleY = 1.5f;
            this.AddWindow(mRemoveAllAnimationDataButton, "Animation");
            mRemoveAllAnimationDataButton.Click += new GuiMessage(RemoveAllAnimationButtonClick);
            
            
            mAnimationListDisplayWindow.EnableRemovingFromList();


#endif
            #endregion



            #region Remove Uncategorized and set default category
            RemoveCategory("Uncategorized");

            SelectCategory("Basic");
            #endregion

#if FRB_XNA
            PropertyGrid.SetPropertyGridTypeAssociation(typeof(Animation3DInstance), typeof(Animation3DInstancePropertyGrid));
#endif

            ShowIColorableProperties();

            this.X = this.ScaleX;
            this.Y = this.ScaleY + Window.MoveBarHeight + 3;
        }



        #endregion

        #region Public Methods

        public void ShowIColorableProperties()
        {
            IncludeMember("ColorOperation", "Color");
            IncludeMember("Red", "Color");
            IncludeMember("Green", "Color");
            IncludeMember("Blue", "Color");

        }


        private BuildToolAssociation GetToolForExtension(string extension)
        {
            if (BuildToolAssociationList == null)
            {
                return null;
            }

            foreach (BuildToolAssociation bta in BuildToolAssociationList)
            {
                if (bta.SourceFileType == extension)
                {
                    return bta;
                }
            }

            return null;
        }

        public override void UpdateDisplayedProperties()
        {
            base.UpdateDisplayedProperties();

#if !FRB_MDX

#endif
        }

        #endregion

        #endregion
    }
}
