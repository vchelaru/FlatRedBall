using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;

namespace DialogTreePluginCore.Managers
{
    public class AssetTypeInfoManager : Singleton<AssetTypeInfoManager>
    {
        private AssetTypeInfo mJsonType;
        public AssetTypeInfo JsonAti
        {
            get
            {
                if (mJsonType == null)
                {
                    mJsonType = new AssetTypeInfo();
                    mJsonType.FriendlyName = "Raw Dialog Tree Json (.json)";
                    mJsonType.QualifiedRuntimeTypeName = new PlatformSpecificType();
                    mJsonType.QualifiedRuntimeTypeName.QualifiedType = "DialogTreePlugin.SaveClasses.DialogTreeRaw.RootObject";

                    mJsonType.QualifiedSaveTypeName = null;
                    mJsonType.Extension = "json";
                    mJsonType.AddToManagersMethod = new List<string>();
                    mJsonType.CustomLoadFunc = GetRawDialogTreeLoadCode;
                    mJsonType.DestroyMethod = null;
                    mJsonType.SupportsMakeOneWay = false;
                    mJsonType.ShouldAttach = false;
                    mJsonType.MustBeAddedToContentPipeline = false;
                    mJsonType.CanBeCloned = false;
                    mJsonType.HasCursorIsOn = false;
                    mJsonType.HasVisibleProperty = false;
                    mJsonType.CanIgnorePausing = false;
                    mJsonType.HideFromNewFileWindow = false;

                    mJsonType.CustomReloadFunc = CustomReloadFunc;
                }

                return mJsonType;
            }
        }

        private string CustomReloadFunc(IElement container, NamedObjectSave nos, 
            ReferencedFileSave file, string contentManagerName)
        {
            // same as normal load
            return GetRawDialogTreeLoadCode(container, nos, file, contentManagerName);
        }

        private string GetRawDialogTreeLoadCode(IElement element, NamedObjectSave namedObject, ReferencedFileSave referencedFile, string contentManager)
        {

            var fileName = ReferencedFileSaveCodeGenerator.GetFileToLoadForRfs(referencedFile, referencedFile.GetAssetTypeInfo());

            return $"{referencedFile.GetInstanceName()} = DialogTreePlugin.SaveClasses.DialogTreeRaw.RootObject.FromJson(\"{fileName}\");";
        }

        private AssetTypeInfo mGlsnType;
        public AssetTypeInfo GlsnAti
        {
            get
            {
                if (mGlsnType == null)
                {
                    mGlsnType = new AssetTypeInfo();
                    mGlsnType.FriendlyName = "Dialog Tree Json (.glsn)";
                    mGlsnType.QualifiedRuntimeTypeName = new PlatformSpecificType();

                    mGlsnType.QualifiedSaveTypeName = null;
                    mGlsnType.Extension = "glsn";
                    mGlsnType.AddToManagersMethod = new List<string>();
                    mGlsnType.CustomLoadMethod = null;
                    mGlsnType.DestroyMethod = null;
                    mGlsnType.SupportsMakeOneWay = false;
                    mGlsnType.ShouldAttach = false;
                    mGlsnType.MustBeAddedToContentPipeline = false;
                    mGlsnType.CanBeCloned = false;
                    mGlsnType.HasCursorIsOn = false;
                    mGlsnType.HasVisibleProperty = false;
                    mGlsnType.CanIgnorePausing = false;
                    mGlsnType.HideFromNewFileWindow = false;
                }

                return mGlsnType;
            }
        }
    }
}
