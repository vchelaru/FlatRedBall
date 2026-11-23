using FlatRedBall.Glue.EditorProperties;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.FormHelpers.PropertyGrids
{
    /// <summary>
    /// Shows a selected screen in the WinForms Properties grid. Which properties exist, which can change and the values
    /// offered come from <see cref="ScreenSavePropertyModel"/>; this renders them.
    /// </summary>
    public class ScreenSavePropertyGridDisplayer : ElementPropertyGridDisplayer
    {
        readonly ScreenSavePropertyModel model;

        public ScreenSavePropertyGridDisplayer(ScreenSavePropertyModel model)
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
                if (value is ScreenSave screen)
                {
                    EditorPropertyGridMapping.Apply(this, screen, model.GetProperties(screen));
                }
                base.Instance = value;
                SetAfterMemberChangedEvents();
            }
        }
    }
}
