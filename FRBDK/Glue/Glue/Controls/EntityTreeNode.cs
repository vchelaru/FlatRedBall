using System.Collections.Generic;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.IO;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Utilities;

namespace FlatRedBall.Glue.Controls
{
    public class EntityTreeNode : BaseElementTreeNode<EntitySave>
    {
        #region Properties

        public EntitySave EntitySave
        {
            get => mSaveObject as EntitySave; 
            set { mSaveObject = value; this.Tag = mSaveObject; }
        }


        #endregion  

        #region Methods

        public EntityTreeNode(string text) : base(text)
        {
            ImageKey = "entity.png";
            SelectedImageKey = "entity.png";
        }      

        #endregion

    }
}
