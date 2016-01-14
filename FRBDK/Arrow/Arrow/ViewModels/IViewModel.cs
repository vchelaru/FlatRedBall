using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace FlatRedBall.Arrow.ViewModels
{
    public interface IViewModel<T> : INotifyPropertyChanged
    {
        T Model { get; set; }
    }

    public static class IViewModelExtensionMethods
    {
        public static void Match<ViewModelType, ModelType>(this ObservableCollection<ViewModelType> list, IEnumerable<ModelType> models) where ViewModelType : IViewModel<ModelType>, new()
        {
            List<ModelType> tempList = new List<ModelType>();
            tempList.AddRange(models);
            list.Match(tempList);

        }


        public static void Match<ViewModelType, ModelType>(this ObservableCollection<ViewModelType> list, ICollection<ModelType> models) where ViewModelType : IViewModel<ModelType>, new()
        {
            for (int i = list.Count - 1; i > -1; i--)
            {
                if (models.Contains(list[i].Model) == false)
                {
                    list.RemoveAt(i);
                }
            }


            int index = 0;
            foreach (ModelType model in models)
            {
                var existingViewModel = list.FirstOrDefault(item => item.Model.Equals(model));
                if (existingViewModel == null)
                {
                    list.Insert(index, new ViewModelType() { Model = model });
                }
                else
                {
                    int existingIndex = list.IndexOf(existingViewModel);
                    if (existingIndex != index)
                    {
                        list.Move(existingIndex, index);
                    }
                }

                index++;
            }

        }

    }
}
