using FlatRedBall.Content.Scene;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FlatRedBall.Arrow.DataTypes
{
    [XmlRoot("ArrowProject")]
    public class ArrowProjectSave
    {
        [XmlElement("Element")]
        public ObservableCollection<ArrowElementSave> Elements
        {
            get;
            set;
        }

        [XmlElement("Resolution")]
        public ResolutionSettings ResolutionSettings
        {
            get;
            set;
        }

        [XmlElement("Camera")]
        public CameraSave CameraSave
        {
            get;
            set;
        }

        [XmlElement("Intent")]
        public ObservableCollection<ArrowIntentSave> Intents
        {
            get;
            set;
        }

        public ArrowProjectSave()
        {
            Elements = new ObservableCollection<ArrowElementSave>();
            Intents = new ObservableCollection<ArrowIntentSave>();
            ResolutionSettings = new ResolutionSettings();

            ResolutionSettings.Width = 800;
            ResolutionSettings.Height = 480;

            CameraSave = new CameraSave();
            CameraSave.Orthogonal = true;
            CameraSave.OrthogonalWidth = ResolutionSettings.Width;
            CameraSave.OrthogonalHeight = ResolutionSettings.Height;
        }

        public bool ShouldSerializeElements()
        {

            return Elements != null && Elements.Count != 0;
        }

    }
}
