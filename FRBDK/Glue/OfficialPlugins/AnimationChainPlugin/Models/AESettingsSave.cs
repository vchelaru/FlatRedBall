using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace FlatRedBall.AnimationEditorForms.Data;

public class AESettingsSave
{
    public float OffsetMultiplier = 1;

    public List<string> ExpandedNodes { get; set; } = new List<string>();

    public bool SnapToGrid { get; set; }
    public int GridSize { get; set; } = 16;
}
