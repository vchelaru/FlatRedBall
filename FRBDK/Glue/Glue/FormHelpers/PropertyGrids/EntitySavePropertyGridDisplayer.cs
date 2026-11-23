using FlatRedBall.Glue.EditorProperties;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.FormHelpers.PropertyGrids
{
    /// <summary>
    /// Shows a selected entity in the WinForms Properties grid. Which properties exist, which can change and the values
    /// offered come from <see cref="EntitySavePropertyModel"/>; this renders them.
    /// </summary>
    public class EntitySavePropertyGridDisplayer : ElementPropertyGridDisplayer
    {
        readonly EntitySavePropertyModel model;

        public EntitySavePropertyGridDisplayer(EntitySavePropertyModel model)
        {
            this.model = model;
        }

        public override object Instance
        {
            get
            {
                return base.Instance;
            }
            set
            {
                if (value is EntitySave entity)
                {
                    EditorPropertyGridMapping.Apply(this, entity, model.GetProperties(entity));
                }
                base.Instance = value;
                SetAfterMemberChangedEvents();
            }
        }
    }
}
