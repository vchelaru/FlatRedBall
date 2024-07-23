using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace FlatRedBall.Forms
{
    public class VisualTemplate
    {
        Func<object, GraphicalUiElement> creationFunc;


        public VisualTemplate(Type type)
        {
#if DEBUG
            if (typeof(GraphicalUiElement).IsAssignableFrom(type) == false)
            {
                throw new ArgumentException(
                    $"The type {type} must be derived from GraphicalUiElement (Gum Runtime usually)");
            }
#endif
            var constructor = type.GetConstructor(Type.EmptyTypes);

#if DEBUG
            if (constructor == null)
            {
                throw new ArgumentException(
                    $"The type {type} must have a constructor with no arguments");
            }
#endif

            Initialize((throwaway) => constructor.Invoke(null) as GraphicalUiElement);
        }

        /// <summary>
        /// Creates a new VisualTemplate with a Func which creates a new GraphicalUiElement.
        /// </summary>
        /// <param name="creationFunc">The Func to call when CreateContent is called.</param>
        public VisualTemplate(Func<GraphicalUiElement> creationFunc)
        {
            Initialize((throwaway) => creationFunc());
        }

        /// <summary>
        /// Instantiates a new VisualTemplate with the provided creation function.
        /// </summary>
        /// <param name="creationFunc">The creation function which takes a ViewModel and returns a new GraphicalUiElement.</param>
        public VisualTemplate(Func<object, GraphicalUiElement> creationFunc)
        {
            Initialize(creationFunc);
        }


        private void Initialize(Func<object, GraphicalUiElement> creationFunc)
        {
            this.creationFunc = creationFunc;
        }

        /// <summary>
        /// Invokes the constructor for the GraphicalUiElement type and returns the result.
        /// </summary>
        /// <param name="bindingContext">The binding context to pass in to the newly-created GraphicalUiElement</param>
        /// <returns>The new GraphicalUiElement instance</returns>
        public GraphicalUiElement CreateContent(object bindingContext)
        {
            return creationFunc(bindingContext);
        }
    }
}
