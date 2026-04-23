using System.Collections.Generic;
using System.Xml.Serialization;

namespace AnimationEditor.Core.Data
{
    public class TextureSettings
    {
    }

    public class AnimationChainSettingSave
    {
        public string Name { get; set; }
        public UnitType UnitType { get; set; }
    }

    public class AESettingsSave
    {
        public float OffsetMultiplier = 1;

        [XmlElement("HorizontalGuide")]
        public List<float> HorizontalGuides = new List<float>();

        [XmlElement("VerticalGuide")]
        public List<float> VerticalGuides = new List<float>();

        [XmlElement("Texture")]
        public List<TextureSettings> TextureSettings = new List<TextureSettings>();

        [XmlElement("ExpandedNode")]
        public List<string> ExpandedNodes { get; set; } = new List<string>();

        public List<AnimationChainSettingSave> AnimationChainSettings = new List<AnimationChainSettingSave>();

        public UnitType UnitType { get; set; }
        public bool SnapToGrid { get; set; }
        public int GridSize { get; set; } = 16;
    }
}
