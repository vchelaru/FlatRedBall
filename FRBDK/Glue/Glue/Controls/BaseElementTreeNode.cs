using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.IO;
using Glue;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Content.Instructions;
using System.Drawing;
using FlatRedBall.IO.Csv;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using EditorObjects.Parsing;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Events;
using FlatRedBall.Localization;
using FlatRedBall.Utilities;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Performance.Measurement;
using FlatRedBall.Glue.IO;

namespace FlatRedBall.Glue.Controls
{
    public enum CodeArea
    {
        Initialize,
        AddToManagers
    }


    #region BaseElementTreeNode (not generic)

    public abstract class BaseElementTreeNode : TreeNode
    {
        #region Fields

        protected TreeNode mCodeTreeNode;

        protected StateListTreeNode mStateListTreeNode;

        public const bool UseIcons = true; 
        
        protected NamedObjectListTreeNode mObjectsTreeNode;


        protected TreeNode mVariablesTreeNode;

        protected TreeNode mEventsTreeNode;

        protected ReferencedFileListTreeNode mFilesTreeNode;

        protected IElement mSaveObject;


        #endregion

        public IElement SaveObject
        {
            get { return mSaveObject; }
        }

        public ReferencedFileListTreeNode FilesTreeNode
        {
            get
            {
                return mFilesTreeNode;
            }
        }

        #region Methods

        public BaseElementTreeNode(string text)
            : base(text)
        {

        }


        public static bool IsOnOwnLayer(IElement element)
        {
            if (element is EntitySave)
            {
                // The AddToManagers for EntitySaves takes a layer.  We should always
                // use this argument, but make sure all methods that take layered arguments
                // can work with null
                return true;

            }
            else
            {
                return (element as ScreenSave).IsOnOwnLayer;
            }
        }


        public abstract IElement SaveObjectAsElement
        {
            get;
            set;
        }

        public void UpdateReferencedTreeNodes()
        {
            ElementViewWindow.SuppressSelectionEvents = true;

            const int numberOfTimesToAllowFailures = 5;

            int numberOfFailures = 0;
            Exception lastException = null;

            while (numberOfFailures < numberOfTimesToAllowFailures)
            {
                try
                {
                    Section.GetAndStartContextAndTime("Text");

                    #region Set this Text
                    if (Text != FileManager.RemovePath(SaveObject.Name))
                    {
                        this.Text = FileManager.RemovePath(SaveObject.Name);
                    }
                    #endregion

                    Section.EndContextAndTime();
                    Section.GetAndStartContextAndTime("UpdateToReferencedFiles");

                    mFilesTreeNode.UpdateToReferencedFiles(mSaveObject.ReferencedFiles, SaveObject);


                    Section.EndContextAndTime();
                    Section.GetAndStartContextAndTime("UpdateToNamedObjectSaves");

                    mObjectsTreeNode.UpdateToNamedObjectSaves(mSaveObject.NamedObjects);


                    Section.EndContextAndTime();
                    Section.GetAndStartContextAndTime("UpdateToStates");

                    mStateListTreeNode.UpdateToStates(mSaveObject.States, mSaveObject.StateCategoryList);

                    Section.EndContextAndTime();
                    Section.GetAndStartContextAndTime("UpdateVariablesTreeNode");

                    UpdateVariablesTreeNode();


                    Section.EndContextAndTime();
                    Section.GetAndStartContextAndTime("UpdateEventsTreeNode");

                    UpdateEventsTreeNode();


                    Section.EndContextAndTime();
                    Section.GetAndStartContextAndTime("UpdateCodeTreeNodes");

                    UpdateCodeTreeNodes();


                    Section.EndContextAndTime();
                    Section.GetAndStartContextAndTime("SaveProjects");

                    Section.EndContextAndTime();

                    break;
                }
                catch (Exception e)
                {
                    lastException = e;
                    System.Threading.Thread.Sleep(20);
                    numberOfFailures++;
                }
            }

            if (numberOfFailures >= numberOfTimesToAllowFailures)
            {
                int m = 3;
            }

            ElementViewWindow.SuppressSelectionEvents = false;

        }

        private void UpdateVariablesTreeNode()
        {
            Section.GetAndStartContextAndTime("Add nodes");
            while (this.mVariablesTreeNode.Nodes.Count < mSaveObject.CustomVariables.Count)
            {
                int indexToAddAt = mVariablesTreeNode.Nodes.Count;
                TreeNode treeNode = mVariablesTreeNode.Nodes.Add(GetDisplayTextForCustomVariable(mSaveObject.CustomVariables[indexToAddAt]));

                if (UseIcons)
                {
                    treeNode.ImageKey = "variable.png";
                    treeNode.SelectedImageKey = "variable.png";
                }
            }

            Section.EndContextAndTime();

            Section.GetAndStartContextAndTime("Remove nodes");

            while (this.mVariablesTreeNode.Nodes.Count > mSaveObject.CustomVariables.Count)
            {
                mVariablesTreeNode.Nodes.RemoveAt(mVariablesTreeNode.Nodes.Count - 1);

            }

            Section.EndContextAndTime();

            Section.GetAndStartContextAndTime("Modify nodes");

            for (int i = 0; i < mSaveObject.CustomVariables.Count; i++)
            {
                Section.GetAndStartContextAndTime("Set Tag");

                TreeNode treeNode = mVariablesTreeNode.Nodes[i];

                CustomVariable customVariable = mSaveObject.CustomVariables[i];

                if (treeNode.Tag != customVariable)
                {
                    treeNode.Tag = customVariable;
                }

                Section.EndContextAndTime();

                Section.GetAndStartContextAndTime("Get Text to set");
                string textToSet = GetDisplayTextForCustomVariable(customVariable);
                Section.EndContextAndTime();

                Section.GetAndStartContextAndTime("Set Text");
                if (treeNode.Text != textToSet)
                {
                    treeNode.Text = textToSet;
                }

                // Vic says - no need to support disabled custom variables
                //if (mSaveObject.NamedObjects[i].IsDisabled)
                //{
                //    treeNode.ForeColor = DisabledColor;
                //}
                Section.EndContextAndTime();

                Section.GetAndStartContextAndTime("Set Color");
                Color colorToSet;
                if (customVariable.SetByDerived)
                {
                    colorToSet = ElementViewWindow.SetByDerivedColor;
                }
                else if (customVariable.DefinedByBase)
                {
                    colorToSet = ElementViewWindow.DefinedByBaseColor;
                }
                else if (!string.IsNullOrEmpty(customVariable.SourceObject) && mSaveObject.GetNamedObjectRecursively(customVariable.SourceObject) == null)
                {
                    colorToSet = ElementViewWindow.MissingObjectColor;
                }
                else
                {
                    colorToSet = Color.White;
                }

                if (treeNode.ForeColor != colorToSet)
                {
                    treeNode.ForeColor = colorToSet;
                }
                Section.EndContextAndTime();
            }


            Section.EndContextAndTime();
        }


        public abstract void RefreshStateCategoryUi(StateSaveCategory category);

        //public abstract void GenerateCode();

        private static string GetDisplayTextForCustomVariable(CustomVariable customVariable)
        {
            if (string.IsNullOrEmpty(customVariable.OverridingPropertyType))
            {
                return
                    customVariable.Name + " (" + customVariable.Type + ")";
            }
            else
            {
                return
                    customVariable.Name + " (" + customVariable.Type + " as " + customVariable.OverridingPropertyType + ")";
            }
        }

        private void UpdateEventsTreeNode()
        {
            while (this.mEventsTreeNode.Nodes.Count < mSaveObject.Events.Count)
            {
                int indexAddingAt = mEventsTreeNode.Nodes.Count;

                TreeNode newNode = mEventsTreeNode.Nodes.Add(mSaveObject.Events[indexAddingAt].EventName);
                newNode.ImageKey = "edit_code.png";
                newNode.SelectedImageKey = "edit_code.png";
            }

            while (this.mEventsTreeNode.Nodes.Count > mSaveObject.Events.Count)
            {
                mEventsTreeNode.Nodes.RemoveAt(mEventsTreeNode.Nodes.Count - 1);

            }

            for (int i = 0; i < mSaveObject.Events.Count; i++)
            {
                TreeNode treeNode = mEventsTreeNode.Nodes[i];

                EventResponseSave eventSave = mSaveObject.Events[i];

                if (treeNode.Tag != eventSave)
                {
                    treeNode.Tag = eventSave;
                }


                string textToSet = eventSave.EventName;

                if (treeNode.Text != textToSet)
                {
                    treeNode.Text = textToSet;
                }
            }

        }

        public void UpdateCodeTreeNodes()
        {
            mCodeTreeNode.Nodes.Clear();

            List<string> files = CodeWriter.GetAllCodeFilesFor(SaveObject);

            foreach (string file in files)
            {
                // See if there is already a tree node for this
                TreeNode foundTreeNode = null;
                string text = FileManager.MakeRelative(file);
                foreach (TreeNode treeNode in mCodeTreeNode.Nodes)
                {
                    if (treeNode.Text == text)
                    {
                        foundTreeNode = treeNode;
                        break;
                    }
                }

                if (foundTreeNode == null)
                {
                    TreeNode treeNode = new TreeNode(text);
                    if (UseIcons)
                    {
                        treeNode.SelectedImageKey = "code.png";
                        treeNode.ImageKey = "code.png";
                    }
                    mCodeTreeNode.Nodes.Add(treeNode);
                }
            }
        }

        public static IElement GetElementIfCustomVariableIsVariableState(CustomVariable customVariable, IElement saveObject)
        {

            if (customVariable.GetIsVariableState() && string.IsNullOrEmpty(customVariable.SourceObject))
            {
                return saveObject;
            }
            else
            {

                NamedObjectSave sourceNamedObjectSave = saveObject.GetNamedObjectRecursively(customVariable.SourceObject);

                if (sourceNamedObjectSave != null)
                {
                    EntitySave sourceEntitySave = ObjectFinder.Self.GetEntitySave(sourceNamedObjectSave.SourceClassType);

                    if (sourceEntitySave != null &&
                        ((sourceEntitySave.States.Count != 0 && customVariable.SourceObjectProperty == "CurrentState") ||
                        sourceEntitySave.StateCategoryList.ContainsCategoryName(customVariable.Type))
                        )
                    {
                        return sourceEntitySave;
                    }
                    else if (sourceEntitySave == null)
                    {
                        ScreenSave sourceScreenSave = ObjectFinder.Self.GetScreenSave(sourceNamedObjectSave.SourceClassType);

                        if (sourceScreenSave != null && sourceScreenSave.States.Count != 0 && customVariable.SourceObjectProperty == "CurrentState")
                        {
                            return sourceScreenSave;
                        }

                    }
                }
                return null;
            }
        }

        public TreeNode GetTreeNodeFor(NamedObjectSave namedObjectSave)
        {
            return mObjectsTreeNode.GetTreeNodeFor(namedObjectSave);
        }

        public TreeNode GetTreeNodeFor(ReferencedFileSave referencedFileSave)
        {
            return mFilesTreeNode.GetTreeNodeFor(referencedFileSave);
        }

        public TreeNode GetTreeNodeFor(CustomVariable variable)
        {
            foreach (TreeNode treeNode in this.mVariablesTreeNode.Nodes)
            {
                if (treeNode.Tag == variable)
                {
                    return treeNode;
                }
            }
            return null;
        }

        public TreeNode GetTreeNodeFor(EventSave eventSave)
        {
            foreach (TreeNode treeNode in this.mVariablesTreeNode.Nodes)
            {
                if (treeNode.Tag == eventSave)
                {
                    return treeNode;
                }
            }
            return null;
        }

        public TreeNode GetTreeNodeFor(EventResponseSave eventResponse)
        {
            foreach (TreeNode treeNode in this.mEventsTreeNode.Nodes)
            {
                if (treeNode.Tag == eventResponse)
                {
                    return treeNode;
                }
            }
            return null;
        }

        #endregion
    }

    #endregion

    public abstract class BaseElementTreeNode<T> : BaseElementTreeNode where T : IElement
    {

        #region Fields

        protected TreeNode mCodeFile;

        protected TreeNode mGeneratedCodeFile;

        #endregion

        #region Properties

        public string CodeFile
        {
            get { return this.mSaveObject.Name + ".cs"; }
            set
            {
                if (mCodeFile == null)
                {
                    mCodeFile = new TreeNode(value);
                    mCodeTreeNode.Nodes.Add(mCodeFile);
                }
                if (mCodeFile.Text != value)
                {
                    mCodeFile.Text = value;
                }
            }
        }
        

        public string GeneratedCodeFile
        {
            get
            {
                string returnValue = this.mSaveObject.Name + ".Generated.cs";
                return returnValue;
                //return mGeneratedCodeFile.Text;
            }
            set
            {
                if (mGeneratedCodeFile == null)
                {
                    mGeneratedCodeFile = new TreeNode(value);

                    ElementViewWindow.Invoke((MethodInvoker)(() =>
                    {
                        mCodeTreeNode.Nodes.Add(mGeneratedCodeFile);
                    }));
                }
                else if (mGeneratedCodeFile.Text != value)
                {

                    ElementViewWindow.Invoke((MethodInvoker)(() =>
                    {
                        mGeneratedCodeFile.Text = value;
                    }));
                }
            }
        }


        public override IElement SaveObjectAsElement
        {
            get { return mSaveObject; }
            set { mSaveObject = (T)(object)value; Tag = value; }
        }

        #endregion

        #region Methods

        #region Constructor

        public BaseElementTreeNode(string text) : base(text)
        {


            mFilesTreeNode = new ReferencedFileListTreeNode("Files");
            if (BaseElementTreeNode.UseIcons)
            {
                mFilesTreeNode.ImageKey = "master_file.png";
                mFilesTreeNode.SelectedImageKey = "master_file.png";
            }
            this.Nodes.Add(mFilesTreeNode);

            mObjectsTreeNode = new NamedObjectListTreeNode("Objects");
            if (BaseElementTreeNode.UseIcons)
            {
                mObjectsTreeNode.ImageKey = "master_object.png";
                mObjectsTreeNode.SelectedImageKey = "master_object.png";
            }
            this.Nodes.Add(mObjectsTreeNode);

            mVariablesTreeNode = new TreeNode("Variables");
            if (BaseElementTreeNode.UseIcons)
            {
                mVariablesTreeNode.ImageKey = "master_variables.png";
                mVariablesTreeNode.SelectedImageKey = "master_variables.png";                
            }
            this.Nodes.Add(mVariablesTreeNode);

            mStateListTreeNode = new StateListTreeNode("States");
            if (BaseElementTreeNode.UseIcons)
            {
                mStateListTreeNode.ImageKey = "master_states.png";
                mStateListTreeNode.SelectedImageKey = "master_states.png";                
            }
            this.Nodes.Add(mStateListTreeNode);


            mEventsTreeNode = new TreeNode("Events");
            if (BaseElementTreeNode.UseIcons)
            {
                mEventsTreeNode.ImageKey = "master_code.png";
                mEventsTreeNode.SelectedImageKey = "master_code.png";                
            }
            this.Nodes.Add(mEventsTreeNode);

           
            mCodeTreeNode = new TreeNode("Code");
            if (BaseElementTreeNode.UseIcons)
            {
                mCodeTreeNode.ImageKey = "master_code.png";
                mCodeTreeNode.SelectedImageKey = "master_code.png";
            }
            this.Nodes.Add(mCodeTreeNode);


        }

        #endregion

        #region Public Methods
        

        public override void RefreshStateCategoryUi(StateSaveCategory category)
        {
            mStateListTreeNode.UpdateToStateCategory(category);
        }


        #endregion


        #region Private Methods
                

        private TreeNode GetTreeNodeForCustomVariable(CustomVariable customVariable)
        {
            foreach (TreeNode node in this.mVariablesTreeNode.Nodes)
            {
                if (node.Tag == customVariable)
                {
                    return node;
                }
            }

            return null;

        }

        #endregion

        #endregion

    }
}
