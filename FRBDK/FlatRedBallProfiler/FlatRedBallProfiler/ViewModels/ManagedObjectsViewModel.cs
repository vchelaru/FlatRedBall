using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Gui;
using FlatRedBall.Instructions;

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


        EntityViewModel EntitiesForSprites
        {
            get;
            set;
        }

        EntityViewModel EntitiesForShapes
        {
            get;
            set;
        }

        EntityViewModel EntitiesForTexts
        {
            get;
            set;
        }

        EntityViewModel CategorizedEntities
        {
            get;
            set;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();

        public ManagedObjectsViewModel()
        {
            // todo: Move this to an Init method so the constructor isn't so heavy
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

            ProfilerCommands.Self.AddManagedObjectsCategory("Sprites", EntitiesForSprites.GetStrings);
            ProfilerCommands.Self.AddManagedObjectsCategory("Shapes", EntitiesForShapes.GetStrings);
            ProfilerCommands.Self.AddManagedObjectsCategory("Texts", EntitiesForTexts.GetStrings);
            ProfilerCommands.Self.AddManagedObjectsCategory("Entities", CategorizedEntities.GetStrings);

            ProfilerCommands.Self.AddManagedObjectsCategory("Windows (FlatRedBall)", GetWindowList);
            ProfilerCommands.Self.AddManagedObjectsCategory("Instructions", GetInstructions);

        }

        private IEnumerable<string> GetInstructions()
        {
            foreach(var item in InstructionManager.Instructions)
            {
                string toReturn = null;
                if(item.Target != null)
                {
                    toReturn = item.Target.ToString();

                    if(string.IsNullOrEmpty(toReturn))
                    {
                        toReturn = item.Target.GetType().Name;
                    }
                }

                if(string.IsNullOrEmpty(toReturn))
                {
                    toReturn = item.ToString();
                }

                yield return toReturn;
            }
        }

        private IEnumerable<string> GetWindowList()
        {
            foreach(var item in GuiManager.Windows)
            {
                yield return item.GetType().Name;
            }

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
            
        }
    }
}
