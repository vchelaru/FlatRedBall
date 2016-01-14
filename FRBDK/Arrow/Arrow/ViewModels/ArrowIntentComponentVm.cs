using FlatRedBall.Arrow.DataTypes;
using FlatRedBall.Arrow.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace FlatRedBall.Arrow.ViewModels
{
    public class ArrowIntentComponentVm : IViewModel<ArrowIntentComponentSave>, INotifyPropertyChanged
    {
        #region Fields
        string mRequiredExtension;

        ArrowIntentComponentSave mModel;


        #endregion

        #region Properties

        public ArrowIntentComponentSave Model
        {
            get
            {
                return mModel;
            }
            set
            {
                mModel = value;
                Refresh();
            }
        }
        
        [Category("Glue Object Characteristics")]
        public string RequiredName
        {
            get { return Model.RequiredName; }
            set
            {
                Model.RequiredName = value;
                NotifyPropertyChanged("RequiredName");
                NotifyPropertyChanged("UiDisplayName");
            }
        }

        [Category("Glue Object Characteristics")]
        public GlueItemType GlueItemType
        {
            get { return Model.GlueItemType; }
            set
            {
                Model.GlueItemType = value;

                NotifyPropertyChanged("GlueItemType");
            }
        }


        [Category("File")]
        public CharacteristicRequirement IsFileRequirement
        {
            get { return Model.IsFileRequirement; }
            set
            {
                Model.IsFileRequirement = value;
                NotifyPropertyChanged("IsFileRequirement");
            }
        }

        [Category("File")]
        public string RequiredExtension
        {
            get
            {
                return Model.RequiredExtension;
            }
            set
            {
                Model.RequiredExtension = value;
                NotifyPropertyChanged("RequiredExtension");
            }
        }



        [Category("File")]
        public bool LoadedOnlyWhenReferenced
        {
            get
            {
                return Model.LoadedOnlyWhenReferenced;
            }
            set
            {
                Model.LoadedOnlyWhenReferenced = value;
                NotifyPropertyChanged("LoadedOnlyWhenReferenced");
            }
        }

        public string UiDisplayName
        {
            get
            {
                return ToString();
            }
        }


        #endregion

        #region Events

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Methods

        public void Refresh()
        {

        }

        void NotifyPropertyChanged(string name)
        {
            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }

            ArrowCommands.Self.File.GenerateGlux(true);
            ArrowCommands.Self.File.SaveProject();
        }

        public override string ToString()
        {
            if (Model == null)
            {
                return null;
            }
            else
            {
                string name;
                if (!string.IsNullOrEmpty(Model.RequiredName))
                {
                    name = Model.RequiredName;
                }
                else
                {
                    name = "<No name specified>";
                }
                return name + " (" + Model.GlueItemType + ")";
            }
        }

        #endregion
    }
}
