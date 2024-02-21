using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Design;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms.Design;
using System.Windows.Forms;
using FlatRedBall.Glue.SaveClasses;
using System.Collections;
using System.Reflection;
using System.IO;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Glue.Plugins.ExportedImplementations;


using FlatRedBall.Glue.SaveClasses.Helpers;

namespace FlatRedBall.Glue.Controls.PropertyGridControls
{
    public class StateValueEditor : UITypeEditor
    {
        const string CanInterpolateFile = @"Content\Icons\CanInterpolate.bmp";
        const string CantInterpolateFile = @"Content\Icons\CantInterpolate.bmp";
        const string NeedsInterpolationVariable = @"Content\Icons\NeedsInterpolateVariable.bmp";

        Image mCanInterpolate = Resources.Resource1.CanInterpolate;
        Image mCantInterpolate = Resources.Resource1.CantInterpolate;
        Image mNeedsVelocityVariable = Resources.Resource1.NeedsInterpolateVariable;

        public override object EditValue(
            ITypeDescriptorContext context,
            IServiceProvider provider,
            object value)
        {
            IWindowsFormsEditorService editorService = null;

            if (provider != null)
            {
                editorService =
                    provider.GetService(
                    typeof(IWindowsFormsEditorService))
                    as IWindowsFormsEditorService;
            }



            if (editorService != null)
            {
                ListBox listBox = new ListBox();



                if (context != null)
                {
                    ICollection collection = context.PropertyDescriptor.Converter.GetStandardValues();

                    foreach (object item in collection)
                    {
                        listBox.Items.Add(item);
                    }

                }
                editorService.DropDownControl(listBox);
            }

            return value;
        }

        // This method indicates to the design environment that
        // the type editor will paint additional content in the
        // LightShape entry in the PropertyGrid.
        public override bool GetPaintValueSupported(
            ITypeDescriptorContext context)
        {
            FlatRedBall.Glue.SaveClasses.StateSave stateSave = 
                ((StateSavePropertyGridDisplayer)context.Instance).Instance as StateSave;

            
            //if (context.PropertyDescriptor.)
            //{
            //    int m = 3;
            //}
            return mPaintIcon;
        }

        bool mPaintIcon = true;

        // This method paints a graphical representation of the 
        // selected value of the LightShpae property.
        public override void PaintValue(PaintValueEventArgs e)
        {
            if (e.Bounds.Left == 1 && e.Bounds.Top == 1)
            {


                if (e.Context.Instance is StateSavePropertyGridDisplayer)
                {
                    FlatRedBall.Glue.SaveClasses.StateSave stateSave =
                        ((StateSavePropertyGridDisplayer)e.Context.Instance).Instance as StateSave;

                    PropertyInfo[] properties = e.Context.GetType().GetProperties();

                    PropertyInfo info = e.Context.GetType().GetProperty("PropertyName");



                    string variableName = (string)info.GetValue(e.Context, null);

                    CustomVariable variable = GlueState.Self.CurrentElement.GetCustomVariable(variableName);
                    if (variable != null)
                    {


                        InterpolationCharacteristic interpolationCharacteristic =
                            CustomVariableHelper.GetInterpolationCharacteristic(variable, GlueState.Self.CurrentElement);

                        Image bitmap = null;
                        if (interpolationCharacteristic == InterpolationCharacteristic.CanInterpolate ||
                            (interpolationCharacteristic == InterpolationCharacteristic.NeedsVelocityVariable && variable.HasAccompanyingVelocityProperty))
                        {
                            bitmap = mCanInterpolate;
                        }
                        else if (interpolationCharacteristic == InterpolationCharacteristic.NeedsVelocityVariable)
                        {
                            bitmap = mNeedsVelocityVariable;
                        }
                        else
                        {
                            bitmap = mCantInterpolate;
                        }

                        e.Graphics.DrawImage(bitmap, e.Bounds.Left, e.Bounds.Top, bitmap.Width, bitmap.Height);

                    }
                }
            }
            else
            {
                //e.Graphics.DrawEllipse(Pens.Yellow, e.Bounds);
            }
        }

        
    }
}
