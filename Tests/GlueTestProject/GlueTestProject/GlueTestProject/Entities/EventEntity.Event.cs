using System;
using FlatRedBall;
using FlatRedBall.Input;
using Microsoft.Xna.Framework.Graphics;
using GlueTestProject.Entities;
using GlueTestProject.Screens;
using System.Collections.Generic;
namespace GlueTestProject.Entities
{
	public partial class EventEntity
	{
        void OnAfterCircleXSet (object sender, EventArgs e)
        {
            if(Circle.Parent != null)
            {
            	this.Y = Circle.RelativeX;
            }
            else
            {
            	this.Y = Circle.X;
            }
            
        }
        void OnAfterCurrentStateSet (object sender, EventArgs e)
        {
            this.X = 5;
        }
        void OnListObjectCollectionChanged (object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (this.ListObject.Count != 0)
            {
                // We set .5 to red and then test it in CustomInitialize
                this.ListObject.Last.Red = .5f;
            }
        }
        void OnIncompleteDefinitionListCollectionChanged (object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            
        }
        void OnCustomEvent1 (FlatRedBall.Gui.IWindow window)
        {
            
        }
                void OnFloatActionEvent (float value)
        {
            
        }
        void OnIEnumerableEvent (IEnumerable<int> value)
        {
            
        }
        void OnAfterInnerWidthSet (object sender, EventArgs e)
        {
            this.LeftRect.X = -(this.LeftRect.ScaleX + this.InnerWidth/2.0f);
            this.RightRect.X = this.RightRect.ScaleX + this.InnerWidth/2.0f;
        }
        void OnInitialized (object sender, EventArgs e)
        {
            
        }
        void OnInitialize (object sender, EventArgs e)
        {
            
        }
        void OnInitializeEvent (object sender, EventArgs e)
        {
            VariableToBeSetByInitializedEvent = 4;
            
        }
        void OnBackPushed ()
        {
            
        }
        void OnAfterExposedInDerivedSet (object sender, EventArgs e)
        {
            
        }

	}
}
