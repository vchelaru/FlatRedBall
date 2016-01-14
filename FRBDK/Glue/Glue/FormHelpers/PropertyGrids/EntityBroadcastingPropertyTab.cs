using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms.Design;
using System.ComponentModel;
using System.Reflection;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Glue.GuiDisplay;

namespace FlatRedBall.Glue.FormHelpers.PropertyGrids
{
    class UnassignedEventPropertyDescriptor : PropertyDescriptor
    {

        public UnassignedEventPropertyDescriptor(string name)
            : base(name, null)
        {

        }

        public override bool CanResetValue(object component)
        {
            return true;
        }

        public override Type ComponentType
        {
            get { throw new NotImplementedException(); }
        }

        public override object GetValue(object component)
        {
            throw new NotImplementedException();
        }

        public override bool IsReadOnly
        {
            get { return false; }
        }

        public override Type PropertyType
        {
            get 
            {
                return typeof(string);
            }
        }

        public override void ResetValue(object component)
        {
            throw new NotImplementedException();
        }

        public override void SetValue(object component, object value)
        {

            throw new NotImplementedException();
        }

        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }
    }


    public class EntityBroadcastingPropertyTab : PropertyTab
    {
        public override PropertyDescriptorCollection GetProperties(object component, Attribute[] attributes)
        {
            List<PropertyDescriptor> propertiesToReturn = new List<PropertyDescriptor>();
            IEventContainer asEventContainer = component as IEventContainer;

            PropertyInfo[] properties = component.GetType().GetProperties(
                                            BindingFlags.Instance | BindingFlags.Public);

            #region Get the events defined by the class

            PropertyDescriptorCollection propertyDescriptions = null;

            bool isStatic = EditorLogic.CurrentNamedObject == null ;

            if (isStatic)
            {

                propertyDescriptions = TypeDescriptor.GetProperties(
                    component, new Attribute[] { new BroadcastAttribute(BroadcastStaticOrInstance.Internal)});
            }
            else
            {
                propertyDescriptions = TypeDescriptor.GetProperties(
                    component, new Attribute[] { new BroadcastAttribute(BroadcastStaticOrInstance.Instance), new CategoryAttribute("Active Events") });


            }
            #endregion
            
            BroadcastAutofillTypeConverter autofillConverter = 
                new BroadcastAutofillTypeConverter();


            if (asEventContainer != null)
            {
                #region Add active events which come from the events CSV

                foreach (EventResponseSave es in asEventContainer.Events)
                {
                    propertyDescriptions = PropertyDescriptorHelper.AddProperty(
                        propertyDescriptions, es.EventName, typeof(string),
                        autofillConverter,
                        new Attribute[] { new CategoryAttribute("Active Events") });
                }

                #endregion

                #region Add Unused Events

                foreach (EventSave es in EventManager.AllEvents.Values)
                {
                    string name = es.EventName;

                    bool doesNamedObjectUseThis = false;

                    foreach (EventResponseSave usedEvent in asEventContainer.Events)
                    {
                        if (usedEvent.EventName == name)
                        {
                            doesNamedObjectUseThis = true;
                            break;
                        }
                    
                    }

                    if (!doesNamedObjectUseThis)
                    {

                        propertyDescriptions = PropertyDescriptorHelper.AddProperty(
                            propertyDescriptions, es.EventName, typeof(string),
                            new FlatRedBall.Glue.FormHelpers.StringConverters.BroadcastAutofillTypeConverter(),
                            new Attribute[] { new CategoryAttribute("Unused Events") });
                    }
                }

                #endregion
            }


            NamedObjectSave asNamedObject = component as NamedObjectSave;

            if (asNamedObject != null)
            {
                Attribute[] customVariableEvents = new Attribute[]{new CategoryAttribute("Custom Variable Set Events")};

                foreach (CustomVariableInNamedObject cv in asNamedObject.InstructionSaves)
                {
                    propertyDescriptions = PropertyDescriptorHelper.AddProperty(
                        propertyDescriptions, cv.Member + " Set", typeof(string),
                        autofillConverter,
                        customVariableEvents);
                }

                #region Remove the ClickEvent if it's not an IClickable

                if (asNamedObject.SourceType != SourceType.Entity)
                {
                    propertyDescriptions = PropertyDescriptorHelper.RemoveProperty(
                        propertyDescriptions, "ClickEvent");
                }
                else if (!string.IsNullOrEmpty(asNamedObject.SourceClassType))
                {

                    EntitySave entitySave = Glue.Elements.ObjectFinder.Self.GetEntitySave(asNamedObject.SourceClassType);

                    if (entitySave != null && entitySave.ImplementsIClickable == false)
                    {
                        propertyDescriptions = PropertyDescriptorHelper.RemoveProperty(
                            propertyDescriptions, "ClickEvent");
                    }
                }

                #endregion
            }



            return propertyDescriptions;

            ////foreach (PropertyDescriptor propertyDescriptor in propertyDescriptions)
            ////{
            ////    propertiesToReturn.Add(propertyDescriptions);
            ////}

            //foreach (PropertyInfo property in properties)
            //{
            //    //object[] propertyAttributes = property.GetCustomAttributes(false);

            //    //foreach (object o in propertyAttributes)
            //    {
            //        //if (o is BroadcastAttribute)
            //        {
            //            propertiesToReturn.Add(propertyDescriptions[property.Name]);
            //        }
            //    }
            //}

            //PropertyDescriptorCollection toReturn = new PropertyDescriptorCollection(
            //                            propertiesToReturn.ToArray());

            //return toReturn;
        }

        public override string TabName
        {
            get { return "Broadcasting"; }
        }



        public override System.Drawing.Bitmap Bitmap
        {
            get
            {
                return Resources.Resource1.broadcastFRB;
            }
        }
    }
}
