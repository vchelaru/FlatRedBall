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
        Dictionary<string, int> childCountByType;

        public string Title
        {
            get
            {
                int count = this.childCountByType.Sum(item=>item.Value);
                return CategoryName + " " + count;
            }
        }

        public IEnumerable<PositionedObjectViewModel> ObjectsUsingThis
        {
            get
            {
                foreach(var kvp in this.childCountByType.OrderByDescending(item=>item.Value))
                {
                    var toReturn = new PositionedObjectViewModel();

                    
                    toReturn.Name = kvp.Value + " " + kvp.Key;
                    
                    yield return toReturn;
                }
            }
        }

        public EntityViewModel()
        {
            childCountByType = new Dictionary<string, int>();
        }

        public void Clear()
        {
            childCountByType.Clear();
        }

        public void Add(PositionedObject objectToAdd)
        {
            
            
            string key = null;

            if (CategorizationType == ViewModels.CategorizationType.Parent)
            {
                if (objectToAdd.Parent == null)
                {
                    key = "<No Parent>";
                }
                else
                {
                    key = objectToAdd.Parent.GetType().Name;
                }
            }
            else if (CategorizationType == ViewModels.CategorizationType.Type)
            {
                key = objectToAdd.GetType().Name;
            }

            if(childCountByType.ContainsKey(key) == false)
            {
                childCountByType[key] = 0;
            }

            childCountByType[key]++;

        }

        public void Refresh()
        {
            RaisePropertyChanged("ObjectsUsingThis");
            RaisePropertyChanged("Title");
            
        }



    }
}
