using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using Gum.DataTypes;
using GumPlugin.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.IO;

namespace GumPlugin.Managers
{
    public class AssetTypeInfoManager : Singleton<AssetTypeInfoManager>
    {
        #region Fields

        AssetTypeInfo mComponentAti;
        AssetTypeInfo mScreenIdbAti;
        AssetTypeInfo screenAti;
        AssetTypeInfo mGraphicalUiElementAti;
        AssetTypeInfo mGumxAti;


        public List<AssetTypeInfo> AssetTypesForThisProject { get; private set; } = new List<FlatRedBall.Glue.Elements.AssetTypeInfo>();


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
                if (screenAti == null)
                {
                    screenAti = new AssetTypeInfo();
                    screenAti.FriendlyName = "Gum Screen (.gusx)";
                    screenAti.QualifiedRuntimeTypeName = new PlatformSpecificType()
                    {
                        QualifiedType = "Gum.Wireframe.GraphicalUiElement"
                    };


                    screenAti.QualifiedSaveTypeName = "Gum.Data.ScreenSave";
                    screenAti.Extension = "gusx";
                    screenAti.AddToManagersMethod.Add("this.AddToManagers();" +
                        "FlatRedBall.FlatRedBallServices.GraphicsOptions.SizeOrOrientationChanged += RefreshLayoutInternal");

                    //screenAti.CustomLoadMethod =
                    //    "FlatRedBall.Gum.GumIdb.UpdateDisplayToMainFrbCamera();
                    //     {THIS} = GumRuntime.ElementSaveExtensions.CreateGueForElement( Gum.Managers.ObjectFinder.Self.GetScreen(FlatRedBall.IO.FileManager.RemoveExtension(FlatRedBall.IO.FileManager.RemovePath(\"{FILE_NAME}\"))), true)";

                    screenAti.CustomLoadFunc = (element, nos, rfs, contentManagerName) =>
                    {
                        var toReturn = "FlatRedBall.Gum.GumIdb.UpdateDisplayToMainFrbCamera();";

                        string strippedName = GetStrippedScreenName(rfs);

                        toReturn +=
                            $"{rfs.GetInstanceName()} = GumRuntime.ElementSaveExtensions.CreateGueForElement(Gum.Managers.ObjectFinder.Self.GetScreen(\"{strippedName}\"), true);";

                        return toReturn;
                    };


                    screenAti.DestroyMethod = "this.RemoveFromManagers();" +
                        "FlatRedBall.FlatRedBallServices.GraphicsOptions.SizeOrOrientationChanged -= RefreshLayoutInternal";
                    screenAti.SupportsMakeOneWay = false;
                    screenAti.ShouldAttach = false;
                    screenAti.MustBeAddedToContentPipeline = false;
                    screenAti.CanBeCloned = false;
                    screenAti.HasCursorIsOn = false;
                    screenAti.HasVisibleProperty = false;

                    screenAti.CanIgnorePausing = false;
                    screenAti.FindByNameSyntax = "GetGraphicalUiElementByName(\"OBJECTNAME\")";
                    screenAti.ShouldAttach = false;
                    screenAti.HideFromNewFileWindow = true;
                }

                return screenAti;
            }
        }

        private static string GetStrippedScreenName(ReferencedFileSave rfs)
        {
            var fileName = rfs.Name;

            var gumxRfs = GumProjectManager.Self.GetRfsForGumProject();
            var gumRfsfolder = FileManager.GetDirectory(gumxRfs.Name, RelativeType.Relative);
            

            var gumScreensFolder = $"{gumRfsfolder}screens/".ToLowerInvariant();

            var strippedName = fileName;
            if (fileName.ToLowerInvariant().StartsWith(gumScreensFolder))
            {
                strippedName = fileName.Substring(gumScreensFolder.Length);
            }

            strippedName = FileManager.RemoveExtension(strippedName);
            return strippedName;
        }

        public AssetTypeInfo ScreenIdbAti
        {
            get
            {
                if (mScreenIdbAti == null)
                {
                    mScreenIdbAti = new AssetTypeInfo();
                    mScreenIdbAti.FriendlyName = "Gum Screen (.gusx)";
                    mScreenIdbAti.QualifiedRuntimeTypeName = new PlatformSpecificType()
                    {
                        QualifiedType = "FlatRedBall.Gum.GumIdb"
                    };

                    mScreenIdbAti.QualifiedSaveTypeName = "Gum.Data.ScreenSave";
                    mScreenIdbAti.Extension = "gusx";
                    mScreenIdbAti.AddToManagersMethod.Add("this.InstanceInitialize(); FlatRedBall.FlatRedBallServices.GraphicsOptions.SizeOrOrientationChanged += this.HandleResolutionChanged");
                    mScreenIdbAti.CustomLoadMethod =
                        // February 11, 2019
                        // We don't use content
                        // managers to load gum, 
                        // but doing a null check
                        // effectively prevents the
                        // content from being re-loaded.
                        // This is a bug that was discovered
                        // during a monthly when a derived screen
                        // was loading its screen 2 times.
                        "if({THIS} == null)\n" +
                        "{{\n" +
                        "Gum.Wireframe.GraphicalUiElement.IsAllLayoutSuspended = true;\n" +
                        "{THIS} = new FlatRedBall.Gum.GumIdb(); \n" +
                        "{THIS}.LoadFromFile(\"{FILE_NAME}\");  {THIS}.AssignReferences();" +

                        "Gum.Wireframe.GraphicalUiElement.IsAllLayoutSuspended = false;\n" +
                        "{THIS}.Element.UpdateLayout();\n" +
                        // HACK!  There's a bug in the UpdateLayout that makes a single UpdateLayout not work right - a 2nd one fixes it. I'm adding this in temporarily until
                        // I have time to dig into the UpdateLayout to see why it's properly positioning all children.
                        "{THIS}.Element.UpdateLayout();\n" +
                        "}}";

                    
                    mScreenIdbAti.DestroyMethod = "FlatRedBall.SpriteManager.RemoveDrawableBatch(this); FlatRedBall.FlatRedBallServices.GraphicsOptions.SizeOrOrientationChanged -= this.HandleResolutionChanged";
                    mScreenIdbAti.SupportsMakeOneWay = false;
                    mScreenIdbAti.ShouldAttach = false;
                    mScreenIdbAti.MustBeAddedToContentPipeline = false;
                    mScreenIdbAti.CanBeCloned = false;
                    mScreenIdbAti.HasCursorIsOn = false;
                    mScreenIdbAti.HasVisibleProperty = false;
                    mScreenIdbAti.CanIgnorePausing = false;
                    mScreenIdbAti.FindByNameSyntax = "GetGraphicalUiElementByName(\"OBJECTNAME\")";
                    mScreenIdbAti.HideFromNewFileWindow = true;
                }

                return mScreenIdbAti;
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
                    // Actually, we need to use the overload so that components which are part of FRB entities get added to the right layer:
                    //mGraphicalUiElementAti.AddToManagersMethod.Add("this.AddToManagers()");
                    // Update October 12, 2017 - since this can be added to layers, need to
                    // use the LayeredAddToManagers instead of AddToManagers:
                    // Update March 28, 2018 - We now use custom code for this using the GetAddToManagersFunc
                    //mGraphicalUiElementAti.LayeredAddToManagersMethod.Add(
                    //    "this.AddToManagers(RenderingLibrary.SystemManagers.Default, " +
                    //    "System.Linq.Enumerable.FirstOrDefault(FlatRedBall.Gum.GumIdb.AllGumLayersOnFrbLayer(mLayer)))");
                    //mGraphicalUiElementAti.AddToManagersMethod.Add("this.AddToManagers()");
                    mGraphicalUiElementAti.AddToManagersFunc = GueDerivingClassCodeGenerator.Self.GetAddToManagersFunc;



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

        AssetTypeInfo GumxAti
        {
            get
            {
                if(mGumxAti == null)
                {
                    mGumxAti = new AssetTypeInfo();
                    mGumxAti.FriendlyName = "Gum Project (.gumx)";
                    mGumxAti.QualifiedRuntimeTypeName = new PlatformSpecificType()
                    {
                        QualifiedType = "FlatRedBall.Gum.GumIdb"
                    };
                    mGumxAti.QualifiedSaveTypeName = "Gum.Data.ProjectSave";
                    mGumxAti.Extension = "gumx";
                    mGumxAti.CustomLoadFunc = GetGumxLoadCode;


                    mGumxAti.SupportsMakeOneWay = false;
                    mGumxAti.ShouldAttach = false;
                    mGumxAti.MustBeAddedToContentPipeline = false;
                    mGumxAti.CanBeCloned = false;
                    mGumxAti.HasCursorIsOn = false;
                    mGumxAti.HasVisibleProperty = false;
                    mGumxAti.CanIgnorePausing = false;

                    // don't let users add this:
                    mGumxAti.HideFromNewFileWindow = true;

                    mGumxAti.CanBeObject = false;

                }

                return mGumxAti;
            }
        }

        string GetGumxLoadCode(IElement element, NamedObjectSave nos, ReferencedFileSave rfs, string contentManagerName)
        {
            string fileNameToLoad = ReferencedFileSaveCodeGenerator.GetFileToLoadForRfs(rfs, mGumxAti);

            string toReturn = $"FlatRedBall.Gum.GumIdb.StaticInitialize(\"{fileNameToLoad}\"); " +
                        "FlatRedBall.Gum.GumIdbExtensions.RegisterTypes();  " +
                        "FlatRedBall.Gui.GuiManager.BringsClickedWindowsToFront = false;";

            var displaySettings = GlueState.Self.CurrentGlueProject?.DisplaySettings;

            if(displaySettings != null)
            {
                if(displaySettings.FixedAspectRatio == false || displaySettings.AspectRatioHeight == 0)
                {
                    toReturn += "FlatRedBall.Gum.GumIdb.FixedCanvasAspectRatio = null;";
                }
                else
                {
                    var aspectRatio = displaySettings.AspectRatioWidth / displaySettings.AspectRatioHeight;
                    toReturn += $"FlatRedBall.Gum.GumIdb.FixedCanvasAspectRatio = {displaySettings.AspectRatioWidth}/{displaySettings.AspectRatioHeight}m;";
                }
            }

            // As of August 19, 2019 Glue handles this in the camera code, so this isn't needed anymore
            //toReturn +=
            //            "FlatRedBall.FlatRedBallServices.GraphicsOptions.SizeOrOrientationChanged += (not, used) => { FlatRedBall.Gum.GumIdb.UpdateDisplayToMainFrbCamera(); };"
            //            ;

            var gumxRfs = GumProjectManager.Self.GetRfsForGumProject();

            bool shouldShowOutlines = false;

            if (gumxRfs != null)
            {
                shouldShowOutlines = gumxRfs.Properties.GetValue<bool>("ShowDottedOutlines");
            }

            string valueAsString = shouldShowOutlines.ToString().ToLowerInvariant();

            toReturn += 
                $"Gum.Wireframe.GraphicalUiElement.ShowLineRectangles = {valueAsString};";

            return toReturn;
        }

        public void AddCommonAtis()
        {
            AddIfNotPresent(GumxAti);

            AddIfNotPresent(ComponentAti);

            AddIfNotPresent(ScreenAti);
            AddIfNotPresent(ScreenIdbAti);

            AddIfNotPresent(GraphicalUiElementAti);
        }



        public void AddIfNotPresent(AssetTypeInfo ati)
        {
            var alreadyAdded = AvailableAssetTypes.Self.AllAssetTypes
                .Any(item => 
                    item.FriendlyName == ati.FriendlyName && 
                    item.QualifiedRuntimeTypeName.QualifiedType == ati.QualifiedRuntimeTypeName.QualifiedType);
            if (alreadyAdded == false)
            {
                AvailableAssetTypes.Self.AddAssetType(ati);
            }
        }

        public void RefreshProjectSpecificAtis()
        {
            var list = GetAtisForDerivedGues();

            AssetTypesForThisProject.Clear();
            AssetTypesForThisProject.AddRange(list);

            foreach (var item in list)
            {
                AddIfNotPresent(item);
            }
        }

        public List<AssetTypeInfo> GetAtisForDerivedGues()
        {
            List<AssetTypeInfo> assetTypeInfos = new List<AssetTypeInfo>();

            var allElements = AppState.Self.AllLoadedElements.ToList();

            foreach (var element in allElements)
            {
                if (GueDerivingClassCodeGenerator.Self.ShouldGenerateRuntimeFor(element))
                {
                    AssetTypeInfo newAti = GetAtiFor(element);
                    assetTypeInfos.Add(newAti);
                }
            }

            return assetTypeInfos;
        }

        private AssetTypeInfo GetAtiFor(ElementSave element)
        {
            if(element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }


            AssetTypeInfo newAti = FlatRedBall.IO.FileManager.CloneObject<AssetTypeInfo>(GraphicalUiElementAti);
            newAti.AddToManagersFunc = GraphicalUiElementAti.AddToManagersFunc;

            newAti.QualifiedRuntimeTypeName = new PlatformSpecificType()
            {
                QualifiedType = GueDerivingClassCodeGenerator.Self.GetQualifiedRuntimeTypeFor(element)
            };


            if (element is ComponentSave)
            {
                newAti.Extension = GumProjectSave.ComponentExtension;
                newAti.CustomLoadMethod = ComponentAti.CustomLoadMethod + " as " + 
                    GueDerivingClassCodeGenerator.Self.GetQualifiedRuntimeTypeFor(element);
            }
            if(element is Gum.DataTypes.ScreenSave)
            {
                var qualifiedName = GueDerivingClassCodeGenerator.Self.GetQualifiedRuntimeTypeFor(element);

                //newAti.CustomLoadMethod =  
                //    // Now the camera setup code handles this, so we don't have to:
                //    //"FlatRedBall.Gum.GumIdb.UpdateDisplayToMainFrbCamera();" +
                //    $"{{THIS}} = ({qualifiedName})GumRuntime.ElementSaveExtensions.CreateGueForElement( " +
                //        "Gum.Managers.ObjectFinder.Self.GetScreen(" +
                //            "FlatRedBall.IO.FileManager.RemoveExtension(FlatRedBall.IO.FileManager.RemovePath(\"{FILE_NAME}\"))), true)";

                newAti.CustomLoadFunc = (element, nos, rfs, contentManagerName) =>
                {
                    var fileName = rfs.Name;

                    string strippedName = GetStrippedScreenName(rfs);

                    var toReturn =
                        $"{rfs.GetInstanceName()} = ({qualifiedName})GumRuntime.ElementSaveExtensions.CreateGueForElement(Gum.Managers.ObjectFinder.Self.GetScreen(\"{strippedName}\"), true);";

                    return toReturn;
                };

                newAti.AddToManagersFunc = null;
                newAti.AddToManagersMethod.Clear();
                newAti.AddToManagersMethod.AddRange(screenAti.AddToManagersMethod);

                newAti.DestroyMethod = screenAti.DestroyMethod;
            }
            string unqualifiedName = element.Name + "Runtime";
            newAti.FriendlyName = unqualifiedName;
            newAti.ConstructorFunc = GetGumElementConstructorFunct;



            newAti.FindByNameSyntax = "GetGraphicalUiElementByName(\"OBJECTNAME\") as " +
                newAti.QualifiedRuntimeTypeName.QualifiedType;

            newAti.ExtraVariablesPattern = "";

            // 9/24/2014
            // Jesse was getting
            // the plugin to crash
            // on this line of code
            // with a NullReferenceException.
            // I'm going to wrap this in if-s to be sure it's safe.
            // 7/19/2018
            // Turns out this can crash if the lement's DefaultState is null
            // This happens if the backing file (like gutx) is not on disk. 
            // Let's put a warning
            if(element != null && element.DefaultState == null)
            {
                GlueCommands.Self.PrintError("Could not find default state for Gum element " + element.Name + ". This can happen if the file is missing on disk");
            }

            if (element != null & element.DefaultState != null)
            {
                var states = new List<Gum.DataTypes.Variables.StateSave>();
                
                states.Add(element.DefaultState);

                var parentElement = Gum.Managers.ObjectFinder.Self.GetElementSave(element.BaseType);
                while(parentElement != null)
                {
                    states.Add(parentElement.DefaultState);
                    parentElement = Gum.Managers.ObjectFinder.Self.GetElementSave(parentElement.BaseType);
                }

                foreach (var state in states)
                {
                    var variablesForState = state.Variables.Where(item => !string.IsNullOrEmpty(item.ExposedAsName) || string.IsNullOrEmpty(item.SourceObject)).ToArray();

                    foreach (var variable in variablesForState)
                    {
                        string variableName = (variable.ExposedAsName ?? variable.Name).Replace(" ", "");

                        var hasAlreadyBeenAdded = newAti.VariableDefinitions.Any(item => item.Name == variableName);

                        if(!hasAlreadyBeenAdded)
                        {

                            var variableDefinition = new VariableDefinition();
                            variableDefinition.Category = variable.Category;
                            variableDefinition.DefaultValue = variable.Value?.ToString();
                            variableDefinition.Name = variableName; // gum variables can have spaces, but Glue variables can't

                            variableDefinition.Type = QualifyGumVariableType(variable, element);

                            ChangePositionUnitTypes(variableDefinition);

                            newAti.VariableDefinitions.Add(variableDefinition);

                        }
                    }
                }
            }

            return newAti;
        }

        private void ChangePositionUnitTypes(VariableDefinition variableDefinition)
        {
            var isPositionUnitVariable =
                variableDefinition.Name == "XUnits" || variableDefinition.Name == "YUnits";

            var isPositionUnitsType = variableDefinition.Type == "Gum.Managers.PositionUnitType";
            if (isPositionUnitVariable && isPositionUnitsType)
            {
                variableDefinition.Type = "Gum.Converters.GeneralUnitType";
            }
        }

        private static string QualifyGumVariableType(Gum.DataTypes.Variables.VariableSave variable, ElementSave container)
        {
            // If it is a state:
            var isState = variable.IsState(container);
            // todo: for now we'll return the unqualified name for the state, but eventually we may want to qualify it:
            if(isState)
            {
                return variable.Type;
            }
            else
            {
                switch(variable.Type)
                {
                    case "int":
                    case "bool":
                    case "string":
                    case "float":
                        return variable.Type;
                    case "HorizontalAlignment":
                        return "RenderingLibrary.Graphics.HorizontalAlignment";
                    case "VerticalAlignment":
                        return "RenderingLibrary.Graphics.VerticalAlignment";
                    case "PositionUnitType":
                        return "Gum.Managers.PositionUnitType";
                    case "Blend":
                        return "Gum.RenderingLibrary.Blend";
                    case "DimensionUnitType":
                        return "Gum.DataTypes.DimensionUnitType";
                    case "ChildrenLayout":
                        return "Gum.Managers.ChildrenLayout";
                    case "TextureAddress":
                        return "Gum.Managers.TextureAddress";
                    case "GeneralUnitType":
                        return "Gum.Converters.GeneralUnitType";
                }

                return variable.Type;
            }
        }

        private string GetGumElementConstructorFunct(IElement glueContainerElement, NamedObjectSave namedObject, ReferencedFileSave assetTypeInfo)
        {
            var fieldName = namedObject.FieldName;

            var ati = namedObject.GetAssetTypeInfo();

            return 
            "{" + 
                $"var oldLayoutSuspended = global::Gum.Wireframe.GraphicalUiElement.IsAllLayoutSuspended; " + 
                $"global::Gum.Wireframe.GraphicalUiElement.IsAllLayoutSuspended = true; " +
                $"{fieldName} = new {ati.QualifiedRuntimeTypeName.QualifiedType}();" +
                $"global::Gum.Wireframe.GraphicalUiElement.IsAllLayoutSuspended = oldLayoutSuspended; " +
                $"{fieldName}.UpdateLayout();" +
            "}";

        }

        public void UnloadProjectSpecificAtis()
        {

            foreach(var item in AssetTypesForThisProject)
            {
                AvailableAssetTypes.Self.RemoveAssetType(item);

            }

            AssetTypesForThisProject.Clear();
        }

    }
}
