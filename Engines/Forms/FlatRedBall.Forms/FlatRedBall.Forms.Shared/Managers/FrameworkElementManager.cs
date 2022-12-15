using FlatRedBall.Forms.Controls;
using FlatRedBall.Managers;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Forms.Managers
{
    public class FrameworkElementManager : IManager
    {
        static FrameworkElementManager self;
        public static FrameworkElementManager Self => self;
        static FrameworkElementManager()
        {
            self = new FrameworkElementManager();
        }
        List<FrameworkElement> elements = new List<FrameworkElement>();

        public void AddFrameworkElement(FrameworkElement frameworkElement)
        {
            elements.Add(frameworkElement);
        }

        public void RemoveFrameworkElement(FrameworkElement frameworkElement)
        {
            if(elements.Contains(frameworkElement))
            {
                elements.Remove(frameworkElement);
            }
        }

        public void Update()
        {
            foreach(var frameworkElement in elements)
            {
                frameworkElement.Activity();
            }
        }

        public void UpdateDependencies()
        {
        }
    }
}
