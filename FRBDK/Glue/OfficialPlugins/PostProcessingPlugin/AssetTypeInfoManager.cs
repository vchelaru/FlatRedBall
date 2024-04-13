using FlatRedBall.Glue.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.PostProcessingPlugin
{
    public class AssetTypeInfoManager
    {
        static AssetTypeInfo postProcessAti;
        public static AssetTypeInfo PostProcessAti
        {
            get
            {
                if(postProcessAti == null)
                {
                    postProcessAti = CreatePostProcessAti();
                }
                return postProcessAti;
            }
        }

        private static AssetTypeInfo CreatePostProcessAti()
        {
            var ati = new AssetTypeInfo();
            ati.Extension = "post";
            ati.CanBeObject = true;
            ati.AddToManagersFunc = (element, nos, rfs, layer) =>
            {
                if(nos != null)
                {
                    throw new NotImplementedException("Vic, fill this in!");
                }
                else
                {
                    var instanceName = rfs.CachedInstanceName;
                    
                    return $"global::FlatRedBall.Graphics.Renderer.GlobalPostProcesses.Add({instanceName});";
                }
            };


            return ati;
        }
    }
}
