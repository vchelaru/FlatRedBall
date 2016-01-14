using EditorObjects.Parsing;
using FlatRedBall.Content;
using FlatRedBall.Content.AI.Pathfinding;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.Content.Math.Splines;
using FlatRedBall.Content.Particle;
using FlatRedBall.Content.Scene;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.FileParsingPlugins
{
    [Export(typeof(PluginBase))]
    public class StandardFileParsingPlugin : EmbeddedPlugin
    {
        public override void StartUp()
        {
            this.TryAddContainedObjects += HandleTryAddContainedObjects;
        }

        private bool HandleTryAddContainedObjects(string absoluteFile, List<string> availableObjects)
        {
            bool toReturn = ContentParser.GetNamedObjectsIn(absoluteFile, availableObjects);

            return toReturn;
        }
    }
}
