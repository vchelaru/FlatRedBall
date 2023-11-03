using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace FlatRedBall.Content.Scene
{
    [XmlRoot("SpriteEditorScene")]
    public partial class SceneSave :
#pragma warning disable CS0618 // Type or member is obsolete
        SpriteEditorScene
#pragma warning restore CS0618 // Type or member is obsolete
    {
        internal static int AsInt(System.Xml.Linq.XElement element)
        {
            return int.Parse(element.Value, CultureInfo.InvariantCulture);
        }

        internal static float AsFloat(System.Xml.Linq.XElement subElement)
        {
            if (subElement.Value == "INF")
            {
                return float.PositiveInfinity;
            }
            else
            {
                return float.Parse(subElement.Value, CultureInfo.InvariantCulture);
            }
        }

        public static void ValidateDependencies(List<SpriteSave> spriteSaves,
            SpriteList spritesToValidate)
        {
            foreach (SpriteSave ss in spriteSaves)
                if (ss.Parent != string.Empty)
                    spritesToValidate.FindByName(ss.Name).AttachTo(
                        spritesToValidate.FindByName(ss.Parent), false);

        }
    }
}
