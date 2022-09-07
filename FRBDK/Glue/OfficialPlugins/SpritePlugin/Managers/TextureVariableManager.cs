using FlatRedBall;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.SpritePlugin.Managers
{
    class TextureVariableManager : Singleton<TextureVariableManager>
    {
        internal async void HandleChange(VariableChangeArguments variable)
        {
            var didChangeSpriteTexture =
                variable.NamedObject?.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.Sprite &&
                variable.ChangedMember == nameof(Sprite.Texture);

            var shouldAskToNullTextureCoordinates = false;
            var nos = variable.NamedObject;
            if (didChangeSpriteTexture)
            {
                var nosContainer =
                    ObjectFinder.Self.GetElementContaining(variable.NamedObject);

                var textureValue = ObjectFinder.Self.GetValueRecursively(
                    nos,
                    nosContainer,
                    nameof(Sprite.Texture)) as string;

                var isTextureNull = string.IsNullOrEmpty(textureValue);

                if (isTextureNull)
                {
                    var hasTextureValuesSet =
                        nos.GetCustomVariable(nameof(Sprite.LeftTexturePixel))?.Value != null ||
                        nos.GetCustomVariable(nameof(Sprite.RightTexturePixel))?.Value != null ||
                        nos.GetCustomVariable(nameof(Sprite.TopTexturePixel))?.Value != null ||
                        nos.GetCustomVariable(nameof(Sprite.BottomTexturePixel))?.Value != null;

                    shouldAskToNullTextureCoordinates = hasTextureValuesSet;
                }
            }

            if (shouldAskToNullTextureCoordinates)
            {

                var message = "Sprites with a null texture will cause a crash if they set texture coordinates. Clear texture coordinates?";

                var result = GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(message);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    await TaskManager.Self.AddAsync(() =>
                    {
                        nos.InstructionSaves.RemoveAll(item =>
                            item.Member == nameof(Sprite.LeftTexturePixel) ||
                            item.Member == nameof(Sprite.RightTexturePixel) ||
                            item.Member == nameof(Sprite.TopTexturePixel) ||
                            item.Member == nameof(Sprite.BottomTexturePixel));

                        var element = ObjectFinder.Self.GetElementContaining(nos);

                        if (element != null)
                        {
                            GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
                            GlueCommands.Self.GluxCommands.SaveGlux();

                            GlueCommands.Self.RefreshCommands.RefreshVariables();
                        }
                    }, $"Removing Texture Coords from {nos}");
                }
            }
        }

    }
}
