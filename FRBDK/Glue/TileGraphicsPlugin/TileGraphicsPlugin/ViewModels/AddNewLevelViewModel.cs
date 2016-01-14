using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace TileGraphicsPlugin.ViewModels
{
    public class AddNewLevelViewModel : INotifyPropertyChanged
    {
        #region Fields

        ObservableCollection<string> mAvailableSharedFiles;

        // We need something selected:
        bool mCreateSamplePlatformer = true;
        bool mCreateShareTilesetWith;
        bool mCreateEmptyLevel;

        int mIndividualTileWidth = 16;
        int mIndividualTileHeight = 16;

        #endregion

        #region Properties

        public string Name
        {
            get;
            set;
        }

        public bool CreateSamplePlatformer 
        {
            get { return mCreateSamplePlatformer; }
            set 
            { 
                mCreateSamplePlatformer = value;

                OnPropertyChanged("CreateSamplePlatformer");
                OnPropertyChanged("TileMapListBoxVisibility");
                OnPropertyChanged("TileWidthHeightVisibility");
                
            }
        }

        public bool CreateShareTilesetWith 
        {
            get { return mCreateShareTilesetWith; }
            set 
            { 
                mCreateShareTilesetWith = value;
                OnPropertyChanged("CreateShareTilesetWith");
                OnPropertyChanged("TileMapListBoxVisibility");
                OnPropertyChanged("TileWidthHeightVisibility");

            }
        }

        public bool CreateEmptyLevel 
        {
            get { return mCreateEmptyLevel; }
            set 
            { 
                mCreateEmptyLevel = value;
                OnPropertyChanged("CreateEmptyLevel");
                OnPropertyChanged("TileMapListBoxVisibility");
                OnPropertyChanged("TileWidthHeightVisibility");

            }
        }

        public int IndividualTileWidth
        {
            get { return mIndividualTileWidth; }
            set
            {
                mIndividualTileWidth = value;
                OnPropertyChanged("IndividualTileWidth");
            }
        }

        public int IndividualTileHeight
        {
            get { return mIndividualTileHeight; }
            set
            {
                mIndividualTileHeight = value;
                OnPropertyChanged("IndividualTileHeight");
            }
        }

        public Visibility TileMapListBoxVisibility
        {
            get
            {
                if (CreateShareTilesetWith)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        public Visibility TileWidthHeightVisibility
        {
            get
            {
                if(CreateEmptyLevel)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }
        // finish here setting TileMapListBoxVisibility

        public ObservableCollection<string> AvailableSharedFiles
        {
            get { return mAvailableSharedFiles; }
            set
            {
                mAvailableSharedFiles = value;
            }
        }

        public string SelectedSharedFile
        {
            get;
            set;
        }

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Methods

        public AddNewLevelViewModel()
        {
            AvailableSharedFiles = new ObservableCollection<string>();

            Name = "Level1";
        }

        void OnPropertyChanged(string propertyName)
        {
            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        internal void MakeNameUnique(List<FlatRedBall.Glue.SaveClasses.ReferencedFileSave> list)
        {
            while(list.Any(item=> FileManager.RemovePath(FileManager.RemoveExtension( item.Name ))== this.Name))
            {
                this.Name = FlatRedBall.Utilities.StringFunctions.IncrementNumberAtEnd(this.Name);
            }
        }

        #endregion

    }
}
