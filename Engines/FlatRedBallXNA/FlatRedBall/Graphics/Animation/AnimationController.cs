using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace FlatRedBall.Graphics.Animation
{
    public class AnimationController
    {
        internal IAnimationChainAnimatable AnimatedObject { get; set; }

        public ObservableCollection<AnimationLayer> Layers
        {
            get; private set;
        } = new ObservableCollection<AnimationLayer>();


        public AnimationController(IAnimationChainAnimatable animatable)
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
                    return true;
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

            AnimatedObject.CurrentChainName = animationToSet;
        }
    }



}
