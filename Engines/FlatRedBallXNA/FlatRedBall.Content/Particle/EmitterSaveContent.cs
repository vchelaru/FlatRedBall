using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Content.Scene;
using FlatRedBall.Content.Particle;
using System.Xml.Serialization;

namespace FlatRedBall.Content.Particle
{             
    [XmlRoot("EmitterSave")]
    public class EmitterSaveContent : EmitterSaveBase
    {

        public EmitterSaveContent() : base() { }

    }
}
