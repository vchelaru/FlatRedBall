using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace FlatRedBall.Graphics.Animation
{
    public class AnimationController
    {
        #region Fields/Properties

        public IAnimatable AnimatedObject { get; set; }

        /// <summary>
        /// The AnimationLayers which will be checked when Activity is called (usually every frame). The first layers in the list have the lowest priority, so 
        /// aniatmions should be added in order of least -> most.
        /// </summary>
        public ObservableCollection<AnimationLayer> Layers
        {
            get; private set;
        } = new ObservableCollection<AnimationLayer>();

        public string Name { get; set; }

        #endregion

        public AnimationController()
        {
            Layers.CollectionChanged += CollectionChanged;
        }

        public AnimationController(IAnimatable animatable)
        {
            Layers.CollectionChanged += CollectionChanged;
            this.AnimatedObject = animatable;
        }

        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch(e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    (e.NewItems[0] as AnimationLayer).Container = this;
                    break;
                case NotifyCollectionChangedAction.Remove:
                    var layer = e.NewItems[0] as AnimationLayer;
                    if (layer.Container == this)
                    {
                        layer.Container = null;
                    }

                    break;
            }
        }

        public bool HasPriority(AnimationLayer layer)
        {
            for(int i = Layers.Count - 1; i > - 1; i--)
            {
                var layerAtI = Layers[i];

                if(layerAtI == layer)
                {
                    return !string.IsNullOrEmpty(layerAtI.CurrentChainName);
                }
                else if(!string.IsNullOrEmpty(layerAtI.CurrentChainName))
                {
                    return false;
                }
            }

            return false;
        }

        public void Activity()
        {
            string animationToSet = null;

            for(int i = Layers.Count-1; i > -1; i--)
            {
                var layer = Layers[i];

                layer.Activity();

                animationToSet = layer.CurrentChainName;

                if(!string.IsNullOrEmpty(animationToSet))
                {
                    break;
                }
            }

            AnimatedObject?.PlayAnimation(animationToSet);
        }

        /// <summary>
        /// Instantiates a new layer and adds it to the Layers collection.
        /// </summary>
        /// <returns>The newly-created Layer.</returns>
        public AnimationLayer AddLayer()
        {
            var layer = new AnimationLayer();
            this.Layers.Add(layer);
            return layer;
        }
    }

}
