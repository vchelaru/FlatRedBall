namespace System.ComponentModel
{
    // Summary:
    //     Specifies the name of the category in which to group the property or event
    //     when displayed in a System.Windows.Forms.PropertyGrid control set to Categorized
    //     mode.
    [AttributeUsage(AttributeTargets.All)]
    public class CategoryAttribute : Attribute
    {
        public CategoryAttribute() { }
        //
        // Summary:
        //     Initializes a new instance of the System.ComponentModel.CategoryAttribute
        //     class using the specified category name.
        //
        // Parameters:
        //   category:
        //     The name of the category.
        public CategoryAttribute(string category) { }
    }
}
