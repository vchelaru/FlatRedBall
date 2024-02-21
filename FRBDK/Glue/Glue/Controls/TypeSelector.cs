using System;
using System.Windows;
using System.Windows.Controls;

namespace FlatRedBall.Glue.Controls;

public class TypeSelector : DataTemplateSelector
{
    #region Properties
    public DataTemplate? BoolDataTemplate
    {
        get;
        set;
    }

    public DataTemplate? EnumDataTemplate
    {
        get;
        set;
    }

    public DataTemplate? IntegralDataTemplate
    {
        get;
        set;
    }
    #endregion

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        if (item is PropertyGridRow rowVm)
        {
            if (rowVm.Value is Enum && EnumDataTemplate != null)
            {
                return EnumDataTemplate;
            }

            if (rowVm.Value is bool && BoolDataTemplate != null)
            {
                return BoolDataTemplate;
            }
            if (IntegralDataTemplate != null)
            {
                return IntegralDataTemplate;
            }
            throw new MemberAccessException("No data template set.");
        }

        if (IntegralDataTemplate != null)
        {
            return IntegralDataTemplate;
        }

        throw new MemberAccessException("No data template set.");
    }
}