using FlatRedBall.Glue.Elements;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.EffectPlugin.Managers
{
    public static class AssetTypeInfoManager
    {
        static AssetTypeInfo fxbEffectAssetTypeInfo;
        public static AssetTypeInfo FxbEffectAssetTypeInfo =>
            fxbEffectAssetTypeInfo = fxbEffectAssetTypeInfo ?? CreateFxbEffectAssetTypeInfo();



        private static AssetTypeInfo CreateFxbEffectAssetTypeInfo()
        {
            var originalEffect = AvailableAssetTypes.Self.AllAssetTypes.FirstOrDefault(item => 
                item.Extension == "fx" && 
                item.QualifiedRuntimeTypeName.QualifiedType == "Microsoft.Xna.Framework.Graphics.Effect");

            var fbxEffect = FileManager.CloneObject(originalEffect);

            fbxEffect.FriendlyName = "Effect (.fxb)";
            fbxEffect.Extension = "fxb";
            fbxEffect.MustBeAddedToContentPipeline = false;
            fbxEffect.CanBeAddedToContentPipeline = false;
            fbxEffect.ContentImporter = null;
            fbxEffect.ContentProcessor = null;

            return fbxEffect;
        }

        public static bool IsFx(AssetTypeInfo ati) => ati?.Extension == "fx";
        internal static void Initialize()
        {
            var fxAti = AvailableAssetTypes.Self.AllAssetTypes.First(item => IsFx(item));
            fxAti.HideFromNewFileWindow = false;

        }
    }
}
