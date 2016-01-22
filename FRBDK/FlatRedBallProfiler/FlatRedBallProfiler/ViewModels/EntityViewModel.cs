using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using FlatRedBall;

namespace FlatRedBallProfiler.ViewModels
{
    public enum CategorizationType
    {
        Parent,
        Type
    }

    public class EntityViewModel : ViewModel
    {
        public CategorizationType CategorizationType
        {
            get;
            set;
        }

        public string CategoryName
        {
            get;
            set;
        }

        List<PositionedObject> instances = new List<PositionedObject>();

        public EntityViewModel()
        {
        }

        public void Clear()
        {
            instances.Clear();
        }
        
        public void Add(PositionedObject objectToAdd)
        {
            instances.Add(objectToAdd);
        }
            
        public IEnumerable<string> GetStrings()
        { 
            string key = null;

            foreach (var instance in instances)
            {
                if (CategorizationType == ViewModels.CategorizationType.Parent)
                {
                    if (instance.Parent == null)
                    {
                        key = "<No Parent>";
                    }
                    else
                    {
                        key = instance.Parent.GetType().Name;
                    }
                }
                else if (CategorizationType == ViewModels.CategorizationType.Type)
                {
                    key = instance.GetType().Name;
                }
                yield return key;
            }

        }

        public void Refresh()
        {
            RaisePropertyChanged("ObjectsUsingThis");
            RaisePropertyChanged("Title");
            
        }



    }
}
