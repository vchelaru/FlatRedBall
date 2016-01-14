using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Gui;
using System.Reflection;
using FlatRedBall.IO;
using System.Collections;

namespace XmlObjectEditor.Gui
{
    public static class GuiData
    {
        #region Fields

        static ListDisplayWindow mAssembliesListDisplayWindow;
        static ListDisplayWindow mAssemblyTypeListDisplayWindow;

        static Type mTypeOfObjectToSerialize;
        static object mObjectToSerialize = null;

        // Since the type isn't known for the PropertyGrid, the only way to 
        // change the reference when loading an object is to recreate the PropertyGrid.
        // When this is done the old PropertyGrid must be destroyed and the new one must
        // be created.  To do this the old PropertyGrid reference must be held.
        static PropertyGrid mObjectPropertyGrid;
        // If the object is an IList, then use a ListDisplayWindow.
        static ListDisplayWindow mObjectListDisplayWindow;

        #endregion

        #region Event Methods

        private static void CreateObjectOfSelectedType(Window callingWindow)
        {
            CollapseListBox collapseListBox = callingWindow as CollapseListBox;

            Type type = collapseListBox.GetFirstHighlightedObject() as Type;

            mObjectToSerialize = Activator.CreateInstance(type);
            mTypeOfObjectToSerialize = type;

            if (PropertyGrid.IsIEnumerable(type))
            {
                mObjectListDisplayWindow = CreateListDisplayWindowForObject(mObjectToSerialize);
            }
            else
            {
                mObjectPropertyGrid = CreatePropertyGridForObject(mObjectToSerialize);
            }

        }

        private static void ClickShowAssemblyTypes(Window callingWindow)
        {
            mAssemblyTypeListDisplayWindow.Visible = true;
            GuiManager.BringToFront(mAssemblyTypeListDisplayWindow);

            mAssemblyTypeListDisplayWindow.ListShowing = EditorData.EditorLogic.CurrentAssembly.GetTypes();

        }

        private static PropertyGrid CreatePropertyGridForObject(object objectToCreateGridFor)
        {

            object[] arguments = new object[]{
                        GuiManager.Cursor,
                        objectToCreateGridFor
                };

            Type t = typeof(PropertyGrid<>).MakeGenericType(mTypeOfObjectToSerialize);
            object obj = Activator.CreateInstance(t, arguments);

            PropertyGrid propertyGrid = obj as PropertyGrid;
            GuiManager.AddWindow(propertyGrid);
            propertyGrid.HasCloseButton = true;
            propertyGrid.Closing += GuiManager.RemoveWindow;

            Button saveButton = new Button(GuiManager.Cursor);
            saveButton.Text = "Save Object as XML";
            saveButton.ScaleX = 9f;
            saveButton.Click += XmlSeralizeObject;
            propertyGrid.AddWindow(saveButton);

            Button loadButton = new Button(GuiManager.Cursor);
            loadButton.Text = "Load Object from XML";
            loadButton.ScaleX = 9f;
            loadButton.Click += XmlDeserializeObject;
            propertyGrid.AddWindow(loadButton);

            return propertyGrid;
        }

        private static ListDisplayWindow CreateListDisplayWindowForObject(object objectToCreateWindowFor)
        {
            ListDisplayWindow listDisplayWindow = new ListDisplayWindow(GuiManager.Cursor);
            GuiManager.AddWindow(listDisplayWindow);
            listDisplayWindow.HasMoveBar = true;
            listDisplayWindow.HasCloseButton = true;
            listDisplayWindow.Resizable = true;
            listDisplayWindow.ListShowing = mObjectToSerialize as IEnumerable;
            listDisplayWindow.Closing += GuiManager.RemoveWindow;

//            listDisplayWindow.EnableAddingToList();

            Button saveButton = new Button(GuiManager.Cursor);
            saveButton.Text = "Save Object as XML";
            saveButton.ScaleX = 9f;
            saveButton.Click += XmlSeralizeObject;
            listDisplayWindow.AddWindow(saveButton);

            Button loadButton = new Button(GuiManager.Cursor);
            loadButton.Text = "Load Object from XML";
            loadButton.ScaleX = 9f;
            loadButton.Click += XmlDeserializeObject;
            listDisplayWindow.AddWindow(loadButton);



            listDisplayWindow.MinimumScaleX = 10;

            return listDisplayWindow;
        }

        static void HighlightNewAssembly(Window callingWindow)
        {
            EditorData.EditorLogic.CurrentAssembly = mAssembliesListDisplayWindow.GetFirstHighlightedObject() as Assembly;
        }

        private static void LoadAssembly(Window callingWindow)
        {
            FileWindow fileWindow = GuiManager.AddFileWindow();
            fileWindow.SetToLoad();
            fileWindow.Filter = 
                "Executable (*.exe)|*.exe|DLL (*.dll)|*.dll";

            fileWindow.OkClick += LoadAssemblyOk;
        }

        private static void LoadAssemblyOk(Window callingWindow)
        {
            EditorData.LoadAssembly(((FileWindow)callingWindow).Results[0]);
        }

        private static void ShowAssemblyPropertyGrid(Window callingWindow)
        {
            PropertyGrid<Assembly> propertyGrid = callingWindow as PropertyGrid<Assembly>;

            //EditorData.EditorLogic.CurrentAssembly = propertyGrid.SelectedObject;

            Button showTypes = new Button(GuiManager.Cursor);
            showTypes.Text = "Show Types";
            showTypes.ScaleX = 5;
            showTypes.Click += ClickShowAssemblyTypes;

            propertyGrid.AddWindow(showTypes);
        }

        private static void XmlDeserializeObject(Window callingWindow)
        {
            FileWindow fileWindow = GuiManager.AddFileWindow();
            fileWindow.SetToLoad();
            fileWindow.SetFileType("xml");

            fileWindow.OkClick += XmlDeserializeObjectOk;
        }

        private static void XmlDeserializeObjectOk(Window callingWindow)
        {
            mObjectToSerialize = FileManager.XmlDeserialize(mTypeOfObjectToSerialize,
                ((FileWindow)callingWindow).Results[0]);

            if (mObjectPropertyGrid != null)
                GuiManager.RemoveWindow(mObjectPropertyGrid);

            PropertyGrid newPropertyGrid = CreatePropertyGridForObject(mObjectToSerialize);

            newPropertyGrid.X = mObjectPropertyGrid.X;
            newPropertyGrid.Y = mObjectPropertyGrid.Y;

            mObjectPropertyGrid = newPropertyGrid;
        }

        private static void XmlSeralizeObject(Window callingWindow)
        {
            FileWindow fileWindow = GuiManager.AddFileWindow();
            fileWindow.SetToSave();
            fileWindow.SetFileType("xml");

            fileWindow.OkClick += XmlSerializeObjectOk;

        }

        private static void XmlSerializeObjectOk(Window callingWindow)
        {
            FileManager.XmlSerialize(
                mTypeOfObjectToSerialize,
                mObjectToSerialize,
                ((FileWindow)callingWindow).Results[0]);

        }

        #endregion

        #region Methods

        #region Public Methods

        public static void Initialize()
        {
            CreateMenuStrip();

            CreateListDisplayWindows();

            CreatePropertyGrids();
        }

        public static void Update()
        {
            mAssembliesListDisplayWindow.UpdateToList();

            if (mObjectListDisplayWindow != null)
            {
                mObjectListDisplayWindow.UpdateToList();
            }
            
            if(mObjectPropertyGrid != null)
            {
                mObjectPropertyGrid.UpdateDisplayedProperties();
            }

        }

        #endregion

        #region Private Methods


        private static void CreateListDisplayWindows()
        {
            mAssembliesListDisplayWindow = new ListDisplayWindow(GuiManager.Cursor);
            GuiManager.AddWindow(mAssembliesListDisplayWindow);
            mAssembliesListDisplayWindow.ScaleX = 10;
            mAssembliesListDisplayWindow.ScaleY = 12;
            mAssembliesListDisplayWindow.HasMoveBar = true;
            mAssembliesListDisplayWindow.X = mAssembliesListDisplayWindow.ScaleX;
            mAssembliesListDisplayWindow.Y = mAssembliesListDisplayWindow.ScaleY + 6;
            mAssembliesListDisplayWindow.Resizable = true;
            mAssembliesListDisplayWindow.ShowPropertyGridOnStrongSelect = true;
            mAssembliesListDisplayWindow.ListBox.Highlight += HighlightNewAssembly;
            mAssembliesListDisplayWindow.ListShowing = EditorData.Assemblies;


            mAssemblyTypeListDisplayWindow = new ListDisplayWindow(GuiManager.Cursor);
            mAssemblyTypeListDisplayWindow.Visible = false;
            GuiManager.AddWindow(mAssemblyTypeListDisplayWindow);
            mAssemblyTypeListDisplayWindow.ScaleX = 10;
            mAssemblyTypeListDisplayWindow.ScaleY = 12;
            mAssemblyTypeListDisplayWindow.HasMoveBar = true;
            mAssemblyTypeListDisplayWindow.X = mAssembliesListDisplayWindow.ScaleX;
            mAssemblyTypeListDisplayWindow.Y = 26;
            mAssemblyTypeListDisplayWindow.Resizable = true;
            mAssemblyTypeListDisplayWindow.ListBox.StrongSelect += CreateObjectOfSelectedType;
            mAssemblyTypeListDisplayWindow.HasCloseButton = true;

        }


        private static void CreateMenuStrip()
        {
            MenuStrip menuStrip = GuiManager.AddMenuStrip();


            MenuItem menuItem = menuStrip.AddItem("File");

            menuItem.AddItem("Load Assembly").Click += LoadAssembly;

        }


        private static void CreatePropertyGrids()
        {
            PropertyGrid.SetNewWindowEvent(typeof(Assembly), ShowAssemblyPropertyGrid);
        }

        #endregion

        #endregion
    }
}
