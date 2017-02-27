using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace FlatRedBall.AnimationEditorForms.Data
{
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
    }
}
