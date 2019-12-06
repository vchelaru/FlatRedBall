using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArticyDraftPlugin.Managers
{
    public class AssetTypeInfoManager : Singleton<AssetTypeInfoManager>
    {
        AssetTypeInfo mArticyDraftXmlAti;

        public AssetTypeInfo ArticyDraftXmlAti
        {
            get
            {
                if(mArticyDraftXmlAti == null)
                {
                    mArticyDraftXmlAti = CreateArticyDraftXmlAti();
                }

                return mArticyDraftXmlAti;
            }
        }

        private AssetTypeInfo CreateArticyDraftXmlAti()
        {
            var toReturn = new AssetTypeInfo();

            toReturn.FriendlyName = "Articy:Draft XML File";
            toReturn.QualifiedRuntimeTypeName = new PlatformSpecificType();
            toReturn.QualifiedRuntimeTypeName.QualifiedType = "ArticyDraft.GameDialogue";

            // todo  continue here maybe

            return toReturn;
        }
    }
}
