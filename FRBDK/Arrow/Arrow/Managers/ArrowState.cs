using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using FlatRedBall.Arrow.DataTypes;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Instructions.Reflection;
using Arrow;
using System.ComponentModel;
using FlatRedBall.Arrow.ViewModels;
using FlatRedBall.Arrow.GlueView;

namespace FlatRedBall.Arrow.Managers
{
    public class ArrowState : Singleton<ArrowState>, INotifyPropertyChanged
    {
        #region Fields

        ArrowProjectSave mCurrentArrowProject;
        ItemsControl mTreeView;
        TreeView mSingleElementTreeView;
        GlueProjectSave mCurrentGlueProjectSave;

        ArrowElementSave mCurrentElementSave;

        MainWindow mMainWindow;

        #endregion

        #region Properties

        public ArrowProjectSave CurrentArrowProject 
        {
            get { return mCurrentArrowProject; }
            set
            {
                mCurrentArrowProject = value;

                CurrentArrowProjectVm.Model = mCurrentArrowProject;

                mMainWindow.DataContext = mCurrentArrowProject;

                ArrowCommands.Self.GuiCommands.RefreshAll();

                BoundsManager.Self.UpdateTo(mCurrentArrowProject.CameraSave);

                RaisePropertyChanged("CurrentArrowProject");
            }
        }
        public ArrowProjectVm CurrentArrowProjectVm
        {
            get;
            private set;
        }


        public ArrowElementSave CurrentArrowElementSave 
        {
            get
            {

                var currentElementVm = CurrentArrowElementVm;
                if (currentElementVm == null)
                {
                    return null;
                }
                else
                {
                    return currentElementVm.Model;
                }
            }
            set
            {
                foreach (var elementVm in CurrentArrowProjectVm.Elements)
                {
                    elementVm.IsSelected = elementVm.Model == value;
                }
                RaiseCurrentArrowElementChangedEvent();
            }
        }
        public ArrowElementVm CurrentArrowElementVm
        {
            get
            {
                foreach (var elementVm in CurrentArrowProjectVm.Elements)
                {
                    if (elementVm.IsSelected)
                    {
                        return elementVm;
                    }
                }
                return null;
            }
        }

        public object CurrentInstance
        {
            get
            {
                ArrowInstanceGeneralVm generalVm = (ArrowInstanceGeneralVm)mSingleElementTreeView.SelectedItem;
                if(generalVm == null)
                {
                    return null;
                }
                else
                {
                    return generalVm.Model;
                }
            }
            set
            {
                foreach (var item in CurrentArrowElementVm.AllInstances)
                {
                    item.IsSelected = item.Model == value;
                }
            }
        }
        public ArrowInstanceGeneralVm CurrentInstanceVm
        {
            get
            {
                var elementVm = CurrentArrowElementVm;
                if (elementVm != null)
                {
                    foreach (var item in elementVm.AllInstances)
                    {
                        if (item.IsSelected)
                        {
                            return item;
                        }
                    }
                }
                return null;
            }
            set
            {
                var elementVm = CurrentArrowElementVm;
                if (elementVm != null)
                {
                    foreach (var item in elementVm.AllInstances)
                    {
                        item.IsSelected = item == value;
                    }
                }
            }
        }

        public GlueProjectSave CurrentGlueProjectSave
        {
            get { return mCurrentGlueProjectSave; }
            set
            {
                mCurrentGlueProjectSave = value;
                ObjectFinder.Self.GlueProject = value;

                if (mCurrentGlueProjectSave != null)
                {
                    foreach (var screen in mCurrentGlueProjectSave.Screens)
                    {
                        foreach (var nos in screen.AllNamedObjects)
                        {
                            nos.UpdateCustomProperties();
                        }
                    }
                    foreach (var entity in mCurrentGlueProjectSave.Entities)
                    {
                        foreach (var nos in entity.AllNamedObjects)
                        {
                            nos.UpdateCustomProperties();
                        }
                    }
                }
            }
        }

        public IElement CurrentGlueElement
        {
            get
            {
                var currentElement = ArrowState.Self.CurrentArrowElementSave;

                if (currentElement != null)
                {
                    string elementName = currentElement.Name;

                    string prefix;

                    IElement glueElement = null;

                    if (currentElement.ElementType == DataTypes.ElementType.Screen)
                    {
                        prefix = "Screens/";
                        glueElement = ArrowState.Self.CurrentGlueProjectSave.Screens.FirstOrDefault(item => item.Name == prefix + elementName);
                    }
                    else
                    {
                        prefix = "Entities/";
                        glueElement = ArrowState.Self.CurrentGlueProjectSave.Entities.FirstOrDefault(item => item.Name == prefix + elementName);

                    }
                    return glueElement;
                }
                return null;
            }
        }

        public NamedObjectSave CurrentNamedObjectSave
        {
            get
            {
                if (CurrentGlueElement != null && CurrentInstance != null)
                {
                    string currentInstanceName = CurrentInstanceName;
                    return CurrentGlueElement.NamedObjects.FirstOrDefault(item => item.InstanceName == currentInstanceName);
                }
                return null;
            }
        }

        public string CurrentGluxFileLocation
        {
            get;
            set;
        }





        public ElementRuntime CurrentElementRuntime
        {
            get
            {
                return GluxManager.CurrentElement;
            }
        }

        public ElementRuntime CurrentContainedElementRuntime
        {
            get
            {
                if (CurrentElementRuntime != null && CurrentInstance != null)
                {
                    return RelationshipManager.Self.ElementRuntimeForArrowInstance(
                        CurrentInstance, CurrentElementRuntime);
                }
                return null;
            }
            set
            {
                if (value != null)
                {
                    CurrentInstanceVm = CurrentArrowElementVm.AllInstances.FirstOrDefault(item => item.Name == value.Name);
                }
                else
                {
                    CurrentInstanceVm = null;
                }
            }
        }

        public string CurrentInstanceName
        {
            get
            {
                object instance = CurrentInstance;
                if (instance != null)
                {
                    return LateBinder.GetInstance(instance.GetType()).GetValue(instance, "Name") as string;
                }
                return null;
            }
        }

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Methods

        public void Initialize(MainWindow mainWindow, ItemsControl allElementsTreeView, TreeView singleElementTreeView)
        {
            CurrentArrowProjectVm = new ArrowProjectVm();
            
            mMainWindow = mainWindow;
            mTreeView = allElementsTreeView;
            mSingleElementTreeView = singleElementTreeView;
        }


        public void RaiseCurrentArrowElementChangedEvent()
        {
            RaisePropertyChanged("CurrentArrowElementVm");
        }

        void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));

            }
        }

        #endregion

    }
}
