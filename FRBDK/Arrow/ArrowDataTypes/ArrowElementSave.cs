using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.Content.Saves;
using FlatRedBall.Content.Scene;
using FlatRedBall.Content.SpriteFrame;
using System.Collections.ObjectModel;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace FlatRedBall.Arrow.DataTypes
{
    #region ElementType Enum
    public enum ElementType
    {
        Entity,
        Screen
    }
    #endregion


    [XmlRoot("Element")]
    public class ArrowElementSave : INotifyPropertyChanged
    {
        #region Fields

        public ElementType ElementType;

        public string Name;

        public string Intent;

        #endregion

        #region Contained "Instances"

        // I want to have a simpler XML structure which could potentially
        // be written by hand, so I'm going to embed the objects right in the
        // element.
        [XmlElement("Sprite")]
        public ObservableCollection<SpriteSave> Sprites { get; set; }
        public bool ShouldSerializeSprites()
        {
            return Sprites != null && Sprites.Count != 0;
        }

        [XmlElement("Circle")]
        public ObservableCollection<CircleSave> Circles { get; set; }
        public bool ShouldSerializeCircles()
        {
            return Circles != null && Circles.Count != 0;
        }

        [XmlElement("Rectangle")]
        public ObservableCollection<AxisAlignedRectangleSave> Rectangles { get; set; }
        public bool ShouldSerializeRectangles()
        {
            return Rectangles != null && Rectangles.Count != 0;
        }


        [XmlElement("SpriteFrame")]
        public ObservableCollection<SpriteFrameSave> SpriteFrameSaves { get; set; }
        public bool ShouldSerializeSpriteFrameSaves()
        {
            return SpriteFrameSaves != null && SpriteFrameSaves.Count != 0;
        }

        [XmlElement("Text")]
        public ObservableCollection<TextSave> Texts { get; set; }
        public bool ShouldSerializeTexts()
        {
            return Texts != null && Texts.Count != 0;
        }


        [XmlElement("ElementInstance")]
        public ObservableCollection<ArrowElementInstance> ElementInstances { get; set; }
        public bool ShouldSerializeElementInstances()
        {
            return ElementInstances != null && ElementInstances.Count != 0;
        }

        List<IEnumerable> mListOfLists = new List<IEnumerable>();

        // If adding anything here, modify the AllInstances property
        // Also, add to the DeleteCommands' HandleDeleteInstanceClick

        public IEnumerable<object> AllInstances
        {
            get
            {
                foreach (IEnumerable enumerable in mListOfLists)
                {
                    foreach (var item in enumerable)
                    {
                        yield return item;
                    }
                }

            }
        }


        #endregion

        [XmlElement("File")]
        public List<ArrowReferencedFileSave> Files
        {
            get;
            set;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ArrowElementSave()
        {
            Sprites = new ObservableCollection<SpriteSave>();
            Circles = new ObservableCollection<CircleSave>();
            Rectangles = new ObservableCollection<AxisAlignedRectangleSave>();
            SpriteFrameSaves = new ObservableCollection<SpriteFrameSave>();
            Texts = new ObservableCollection<TextSave>();
            ElementInstances = new ObservableCollection<ArrowElementInstance>();
            Files = new List<ArrowReferencedFileSave>();

            mListOfLists.Add(Sprites);
            mListOfLists.Add(Circles);
            mListOfLists.Add(Rectangles);
            mListOfLists.Add(SpriteFrameSaves);
            mListOfLists.Add(Texts);
            mListOfLists.Add(ElementInstances);

            foreach (IEnumerable enumerable in mListOfLists)
            {
                INotifyCollectionChanged asCollectionChanged = enumerable as INotifyCollectionChanged;

                asCollectionChanged.CollectionChanged += HandleCollectionChanged;
            }
        }

        void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RaisePropertyChanged("AllInstances");
        }

        public override string ToString()
        {
            return Name;
        }

        void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
