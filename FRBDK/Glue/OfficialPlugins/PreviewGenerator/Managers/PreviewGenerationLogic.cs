using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OfficialPlugins.PreviewGenerator.Managers
{
    public static class PreviewGenerationLogic
    {
        public static ImageSource GetImageSourceForSelection(NamedObjectSave namedObjectSave, GlueElement element, StateSave state)
        {
            if (namedObjectSave == null && element != null)
            {
                namedObjectSave = element.NamedObjects.FirstOrDefault(item => item.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.Sprite);
            }

            string textureName, achxName, chainName;
            GetVariablesForCreatingPreview(namedObjectSave, element, state, out textureName, out achxName, out chainName);

            FilePath textureFilePath;
            int? left, top, width, height;
            textureName = GetCoordinates(element, textureName, achxName, chainName, out textureFilePath, out left, out top, out width, out height);

            if (textureFilePath == null && !string.IsNullOrEmpty(textureName))
            {
                var rfs = element.GetReferencedFileSaveRecursively(textureName);

                if (rfs != null)
                {
                    textureFilePath = GlueCommands.Self.GetAbsoluteFilePath(rfs);
                }
            }

            ImageSource imageSource = null;

            if (textureFilePath?.Exists() == true)
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.UriSource = new Uri(textureFilePath.FullPath, UriKind.Relative);
                bitmapImage.EndInit();

                imageSource = bitmapImage;

                if (left != null)
                {
                    var croppedBitmap = new CroppedBitmap();
                    croppedBitmap.BeginInit();
                    croppedBitmap.SourceRect = new Int32Rect(left.Value, top.Value, width.Value, height.Value);
                    croppedBitmap.Source = bitmapImage;
                    croppedBitmap.EndInit();

                    imageSource = croppedBitmap;
                }

            }

            return imageSource;
        }

        private static void GetVariablesForCreatingPreview(NamedObjectSave namedObjectSave, GlueElement element, StateSave state, out string textureName, out string achxName, out string chainName)
        {
            textureName = null;
            achxName = null;
            chainName = null;
            if (namedObjectSave?.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.Sprite)
            {
                if (state != null)
                {
                    textureName = GetEffectiveValue(state, namedObjectSave, "Texture", element);
                    achxName = GetEffectiveValue(state, namedObjectSave, "AnimationChains", element);
                    chainName = GetEffectiveValue(state, namedObjectSave, "CurrentChainName", element);
                }

                if (string.IsNullOrEmpty(textureName))
                {
                    textureName = namedObjectSave.GetCustomVariable("Texture")?.Value as string;
                }

                if (string.IsNullOrEmpty(achxName))
                {
                    achxName = namedObjectSave.GetCustomVariable("AnimationChains")?.Value as string;
                }

                if (string.IsNullOrEmpty(chainName))
                {
                    chainName = namedObjectSave.GetCustomVariable("CurrentChainName")?.Value as string;
                }
            }
        }

        private static string GetEffectiveValue(StateSave state, NamedObjectSave namedObjectSave, string variableName, GlueElement owner)
        {
            var variable = owner.CustomVariables.FirstOrDefault(item => item.SourceObject == namedObjectSave.InstanceName && item.SourceObjectProperty == variableName);

            string valueToReturn = null;

            if (variable != null)
            {
                var matchingInstruction = state.InstructionSaves.FirstOrDefault(item => item.Member == variable.Name);

                if (matchingInstruction != null)
                {
                    valueToReturn = matchingInstruction.Value as string;
                }
            }
            return valueToReturn;
        }

        private static string GetCoordinates(GlueElement element, string textureName, string achxName, string chainName, out FilePath textureFilePath, out int? left, out int? top, out int? width, out int? height)
        {
            textureFilePath = null;
            left = null;
            top = null;
            width = null;
            height = null;
            if (!string.IsNullOrEmpty(achxName))
            {
                var rfs = element.GetReferencedFileSaveRecursively(achxName);
                var achxFullPath = GlueCommands.Self.GetAbsoluteFilePath(rfs);

                if (achxFullPath.Exists())
                {
                    var animationChainListSave = AnimationChainListSave.FromFile(achxFullPath.FullPath);

                    AnimationChainSave animationChainSave = null;
                    animationChainSave = animationChainListSave.AnimationChains.FirstOrDefault(item => item.Name == chainName);
                    if (animationChainSave == null)
                    {
                        animationChainSave = animationChainListSave.AnimationChains.FirstOrDefault();
                    }

                    var frame = animationChainSave.Frames.FirstOrDefault();
                    left = (int)frame?.LeftCoordinate;
                    width = (int)frame?.RightCoordinate - left;
                    top = (int)frame?.TopCoordinate;
                    height = (int)frame?.BottomCoordinate - top;

                    textureName = frame.TextureName;

                    textureFilePath = achxFullPath.GetDirectoryContainingThis() + textureName;

                }
            }

            return textureName;
        }

    }
}
