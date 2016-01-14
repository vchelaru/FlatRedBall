using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Math.Geometry;

namespace FlatRedBallProfiler.ViewModels
{
    public class ManagedObjectsViewModel : INotifyPropertyChanged
    {
        public string Summary
        {
            get
            {
                return FlatRedBall.Debugging.Debugger.GetAutomaticallyUpdatedObjectInformation();
            }
        }


        public EntityViewModel EntitiesForSprites
        {
            get;
            private set;
        }

        public EntityViewModel EntitiesForShapes
        {
            get;
            private set;
        }

        public EntityViewModel EntitiesForTexts
        {
            get;
            private set;
        }

        public EntityViewModel CategorizedEntities
        {
            get;
            private set;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();

        public ManagedObjectsViewModel()
        {
            dispatcherTimer.Tick += new EventHandler(Refresh);
            dispatcherTimer.Interval = new TimeSpan(0,0,1);
            dispatcherTimer.Start();

            EntitiesForSprites = new EntityViewModel { CategoryName = "Sprites" };
            EntitiesForShapes = new EntityViewModel { CategoryName = "Shapes" };
            EntitiesForTexts = new EntityViewModel { CategoryName = "Texts" };
            CategorizedEntities = new EntityViewModel 
            { 
                CategoryName = "Entities",
                CategorizationType = CategorizationType.Type
            
            };
        }

        private void Refresh(object sender, EventArgs e)
        {
            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("Summary"));
            }
        }


        internal void ManualRefresh()
        {
            EntitiesForSprites.Clear();
            EntitiesForShapes.Clear();
            EntitiesForTexts.Clear();
            CategorizedEntities.Clear();

            foreach(var item in SpriteManager.AutomaticallyUpdatedSprites)
            {
                EntitiesForSprites.Add(item);
            }

            foreach(var item in ShapeManager.AutomaticallyUpdatedShapes)
            {
                EntitiesForShapes.Add(item);
            }

            foreach(var item in TextManager.AutomaticallyUpdatedTexts)
            {
                EntitiesForTexts.Add(item);
            }

            foreach(var item in SpriteManager.ManagedPositionedObjects)
            {
                CategorizedEntities.Add(item);
            }


            EntitiesForSprites.Refresh();
            EntitiesForShapes.Refresh();
            EntitiesForTexts.Refresh();
            CategorizedEntities.Refresh();
        }
    }
}
