using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using Gum.DataTypes;
using GumPlugin.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GumPlugin.Managers
{
    public class AssetTypeInfoManager : Singleton<AssetTypeInfoManager>
    {
        #region Fields

        AssetTypeInfo mComponentAti;
        AssetTypeInfo mScreenAti;
        AssetTypeInfo mGraphicalUiElementAti;

        List<AssetTypeInfo> mAssetTypesForThisProject = new List<FlatRedBall.Glue.Elements.AssetTypeInfo>();

        #endregion
        

        // Victor Chelaru 
        // March 10, 2015
        // I think initially
        // I wanted to have all
        // components use this ATI,
        // but it turns out that the 
        // Gum plugin dynamically creates
        // ATI's for all components, so we
        // don't need a base ComponentAti.
        // Update March 11, 2015
        // But the user may add a
        // .gucx file to their project,
        // and in that case the .gucx needs
        // to be loaded, so we're going to have
        // to support that here:
        AssetTypeInfo ComponentAti
        {
            get
            {
                if (mComponentAti == null)
                {
                    mComponentAti = new AssetTypeInfo();
                    mComponentAti.FriendlyName = "Gum Component (.gucx)";
                    mComponentAti.QualifiedRuntimeTypeName = new PlatformSpecificType()
                    {
                        QualifiedType = "Gum.Wireframe.GraphicalUiElement"
                    };

                    
                    mComponentAti.QualifiedSaveTypeName = "Gum.Data.ComponentSave";
                    mComponentAti.Extension = "gucx";
                    mComponentAti.AddToManagersMethod.Add("this.AddToManagers()");
                    mComponentAti.CustomLoadMethod =
                        "{THIS} = GumRuntime.ElementSaveExtensions.CreateGueForElement( Gum.Managers.ObjectFinder.Self.GetComponent(FlatRedBall.IO.FileManager.RemoveExtension(FlatRedBall.IO.FileManager.RemovePath(\"{FILE_NAME}\"))), true)";
                    mComponentAti.DestroyMethod = "this.RemoveFromManagers()";
                    mComponentAti.SupportsMakeOneWay = false;
                    mComponentAti.ShouldAttach = false;
                    mComponentAti.MustBeAddedToContentPipeline = false;
                    mComponentAti.CanBeCloned = false;
                    mComponentAti.HasCursorIsOn = false;
                    mComponentAti.HasVisibleProperty = false;
                    mComponentAti.CanIgnorePausing = false;
                    mComponentAti.FindByNameSyntax = "GetGraphicalUiElementByName(\"OBJECTNAME\")";
                    mComponentAti.ShouldAttach = false;
                    mComponentAti.HideFromNewFileWindow = true;
                }

                return mComponentAti;
            }
        }

        AssetTypeInfo ScreenAti
        {
            get
            {
                if (mScreenAti == null)
                {
                    mScreenAti = new AssetTypeInfo();
                    mScreenAti.FriendlyName = "Gum Screen (.gusx)";
                    mScreenAti.QualifiedRuntimeTypeName = new PlatformSpecificType()
                    {
                        QualifiedType = "FlatRedBall.Gum.GumIdb"
                    };

                    mScreenAti.QualifiedSaveTypeName = "Gum.Data.ScreenSave";
                    mScreenAti.Extension = "gusx";
                    mScreenAti.AddToManagersMethod.Add("this.InstanceInitialize()");
                    mScreenAti.CustomLoadMethod =
                        "Gum.Wireframe.GraphicalUiElement.IsAllLayoutSuspended = true;  {THIS} = new FlatRedBall.Gum.GumIdb();  {THIS}.LoadFromFile(\"{FILE_NAME}\");  {THIS}.AssignReferences();" +

                        // HACK!  There's a bug in the UpdateLayout that makes a single UpdateLayout not work right - a 2nd one fixes it. I'm adding this in temporarily until
                        // I have time to dig into the UpdateLayout to see why it's properly positioning all children.
                        "Gum.Wireframe.GraphicalUiElement.IsAllLayoutSuspended = false; {THIS}.Element.UpdateLayout(); {THIS}.Element.UpdateLayout();";


                    mScreenAti.DestroyMethod = "this.Element.RemoveFromManagers()";
                    mScreenAti.SupportsMakeOneWay = false;
                    mScreenAti.ShouldAttach = false;
                    mScreenAti.MustBeAddedToContentPipeline = false;
                    mScreenAti.CanBeCloned = false;
                    mScreenAti.HasCursorIsOn = false;
                    mScreenAti.HasVisibleProperty = false;
                    mScreenAti.CanIgnorePausing = false;
                    mScreenAti.FindByNameSyntax = "GetGraphicalUiElementByName(\"OBJECTNAME\")";
                    mScreenAti.HideFromNewFileWindow = true;
                }

                return mScreenAti;
            }
        }

        AssetTypeInfo GraphicalUiElementAti
        {
            get
            {
                if (mGraphicalUiElementAti == null)
                {
                    mGraphicalUiElementAti = new AssetTypeInfo();
                    mGraphicalUiElementAti.FriendlyName = "GraphicalUiElement";
                    mGraphicalUiElementAti.QualifiedRuntimeTypeName = new PlatformSpecificType()
                    {
                        QualifiedType = "Gum.Wireframe.GraphicalUiElement"
                    };

                    //gumComponentAti.QualifiedSaveTypeName = "Gum.Wireframe.GraphicalUiElement";
                    //gumComponentAti.Extension = "gucx";

                    // mManagers doesn't exist in context, so we should just use the no-arg version
                    //graphicalUiElement.AddToManagersMethod.Add("this.AddToManagers(mManagers, null)");
                    mGraphicalUiElementAti.AddToManagersMethod.Add("this.AddToManagers()");

                    //gumComponentAti.CustomLoadMethod = "{THIS} = new GumIdb();  {THIS}.LoadFromFile(\"{FILE_NAME}\");";
                    mGraphicalUiElementAti.CanBeCloned = false;
                    mGraphicalUiElementAti.CanIgnorePausing = false;
                    mGraphicalUiElementAti.CanBeObject = true;

                    mGraphicalUiElementAti.DestroyMethod = "this.RemoveFromManagers()";
                    mGraphicalUiElementAti.SupportsMakeOneWay = false;
                    mGraphicalUiElementAti.ShouldAttach = false;
                    mGraphicalUiElementAti.MustBeAddedToContentPipeline = false;
                    mGraphicalUiElementAti.HasCursorIsOn = false;
                    mGraphicalUiElementAti.HasVisibleProperty = false;
                    mGraphicalUiElementAti.FindByNameSyntax = "GetGraphicalUiElementByName(\"OBJECTNAME\")";
                    mGraphicalUiElementAti.ExtraVariablesPattern = "float X; float Y; float Width; float Height; bool Visible";
                    mGraphicalUiElementAti.HideFromNewFileWindow = true;
                }

                return mGraphicalUiElementAti;
            }
        }

        public void AddCommonAtis()
        {
            AddGumxAti();

            AddIfNotPresent(ComponentAti);

            AddIfNotPresent(ScreenAti);

            AddIfNotPresent(GraphicalUiElementAti);

        }



        private void AddGumxAti()
        {
            AssetTypeInfo gumProjectAti = new AssetTypeInfo();
            gumProjectAti.FriendlyName = "Gum Project (.gumx)";
            gumProjectAti.QualifiedRuntimeTypeName = new PlatformSpecificType()
            {
                QualifiedType = "FlatRedBall.Gum.GumIdb"
            };
            gumProjectAti.QualifiedSaveTypeName = "Gum.Data.ProjectSave";
            gumProjectAti.Extension = "gumx";
            gumProjectAti.CustomLoadMethod = "FlatRedBall.Gum.GumIdb.StaticInitialize(\"{FILE_NAME}\"); " +
                "FlatRedBall.Gum.GumIdb.RegisterTypes();  " + 
                "FlatRedBall.Gui.GuiManager.BringsClickedWindowsToFront = false;";
            gumProjectAti.SupportsMakeOneWay = false;
            gumProjectAti.ShouldAttach = false;
            gumProjectAti.MustBeAddedToContentPipeline = false;
            gumProjectAti.CanBeCloned = false;
            gumProjectAti.HasCursorIsOn = false;
            gumProjectAti.HasVisibleProperty = false;
            gumProjectAti.CanIgnorePausing = false;

            // don't let users add this:
            gumProjectAti.HideFromNewFileWindow = true;

            gumProjectAti.CanBeObject = false;

            AddIfNotPresent(gumProjectAti);
        }

        public void AddIfNotPresent(AssetTypeInfo ati)
        {
            if (AvailableAssetTypes.Self.AllAssetTypes.Any(item => item.FriendlyName == ati.FriendlyName) == false)
            {
                AvailableAssetTypes.Self.AddAssetType(ati);
            }
        }

        public void AddProjectSpecificAtis()
        {
            var list = GetAtisForDerivedGues();

            mAssetTypesForThisProject.AddRange(list);

            foreach (var item in list)
            {
                AddIfNotPresent(item);
            }
        }

        public string GetQualifiedRuntimeFor(string elementName)
        {
            string unqualifiedName = elementName + "Runtime";
            return GueDerivingClassCodeGenerator.Self.GueRuntimeNamespace + "." + unqualifiedName;
        }

        public List<AssetTypeInfo> GetAtisForDerivedGues()
        {
            List<AssetTypeInfo> assetTypeInfos = new List<AssetTypeInfo>();

            foreach (var element in AppState.Self.AllLoadedElements)
            {
                if (GueDerivingClassCodeGenerator.Self.ShouldGenerateRuntimeFor(element))
                {
                    AssetTypeInfo newAti = FlatRedBall.IO.FileManager.CloneObject<AssetTypeInfo>(GraphicalUiElementAti);

                    
                    newAti.QualifiedRuntimeTypeName = new PlatformSpecificType()
                    {
                        QualifiedType = GetQualifiedRuntimeFor(element.Name)
                    };

                    if(element is ComponentSave)
                    {
                        newAti.Extension = GumProjectSave.ComponentExtension;
                        newAti.CustomLoadMethod = ComponentAti.CustomLoadMethod + " as " + GetQualifiedRuntimeFor(element.Name);
                    }
                    string unqualifiedName = element.Name + "Runtime";
                    newAti.FriendlyName = unqualifiedName;



                    newAti.FindByNameSyntax = "GetGraphicalUiElementByName(\"OBJECTNAME\") as " +
                        newAti.QualifiedRuntimeTypeName.QualifiedType ;

                    assetTypeInfos.Add(newAti);

                    if (newAti.ExtraVariablesPattern.EndsWith(";") == false)
                    {
                        newAti.ExtraVariablesPattern += ";";
                    }

                    // 9/24/2014
                    // Jesse was getting
                    // the plugin to crash
                    // on this line of code
                    // with a NullReferenceException.
                    // I'm going to wrap this in if-s to be sure it's safe.
                    if (element != null && element.DefaultState != null && element.DefaultState.Variables != null)
                    {
                        foreach (var variable in element.DefaultState.Variables.Where(item => !string.IsNullOrEmpty(item.ExposedAsName)))
                        {
                            newAti.ExtraVariablesPattern += variable.Type + " " + variable.ExposedAsName + ";";
                        }
                    }
                }
            }

            return assetTypeInfos;
        }

        public void UnloadProjectSpecificAtis()
        {

            foreach(var item in mAssetTypesForThisProject)
            {
                AvailableAssetTypes.Self.RemoveAssetType(item);

            }

            mAssetTypesForThisProject.Clear();
        }

    }
}
