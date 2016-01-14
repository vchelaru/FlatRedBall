using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Settings;
using FlatRedBall.Glue.Events;
using FlatRedBall.Instructions.Reflection;

namespace FlatRedBall.Glue.FormHelpers
{
    public class ParameterPropertyDescriptor : PropertyDescriptor
    {
        Type mParameterType;

		TypeConverter mTypeConverter;

		public object Owner
		{
			get;
			set;
		}

        public ParameterPropertyDescriptor(string name, Type type, Attribute[] attrs)
            : base(name, attrs)
        {
			mParameterType = type;
        }

		public TypeConverter TypeConverter
		{
			get { return Converter; }
			set { mTypeConverter = value; }
		}

        public override TypeConverter Converter
        {
            get
            {
				if (mTypeConverter == null)
				{
					if (mParameterType == typeof(bool))
					{
						return TypeDescriptor.GetConverter(typeof(bool));
					}
					else
					{
						return TypeDescriptor.GetConverter(typeof(string));
					}
				}
				else
				{
					return mTypeConverter;
				}
            }
        }

        public override bool CanResetValue(object component)
        {
            return false;
        }

        public override Type ComponentType
        {
            get 
            {

                return mParameterType;
                // add more things here
				//return typeof(string);
            
            }
        }

        public override object GetValue(object component)
        {
			if (Owner != null)
			{
				if (Owner is FileAssociationSettings)
				{
					return ((FileAssociationSettings)Owner).GetApplicationForExtension(this.Name);
				}
				else
				{
					return null;
				}
            }


            #region StateSave

            if (EditorLogic.CurrentStateSave != null)
            {
                return EditorLogic.CurrentStateSave.GetValue(this.Name);

            }

            #endregion

            #region NamedObjectSave

            else if (EditorLogic.CurrentNamedObject != null)
            {
                NamedObjectSave asNamedObject = EditorLogic.CurrentNamedObject;

                if (this.Attributes.Count > 0 && this.Attributes[0] is CategoryAttribute)
                {
                    string category = ((CategoryAttribute)this.Attributes[0]).Category;

                    switch (category)
                    {
                        case "Active Events":

                            EventSave eventSave = asNamedObject.GetEvent(Name);
                            if (eventSave != null)
                            {
                                return eventSave.InstanceMethod;
                            }
                            else
                            {
                                return "";
                            }
                            //break;
                        case "Unused Events":
                            return "";
                            //break;
                        case "Custom Variable Set Events":
                            return EditorLogic.CurrentNamedObject.GetCustomVariable(this.Name.Replace(" Set", "")).EventOnSet;
                            //break;
                        default:
                            return EditorLogic.CurrentNamedObject.GetPropertyValue(this.Name);

                    }
                }
                else
                {
                    return EditorLogic.CurrentNamedObject.GetPropertyValue(this.Name);
                }
            }
            #endregion



            #region CustomVariable

            else if (EditorLogic.CurrentCustomVariable != null)
            {
                if (EditorLogic.CurrentEntitySave != null)
                {
                    return EditorLogic.CurrentEntitySave.GetPropertyValue(EditorLogic.CurrentCustomVariable.Name);
                }
                else
                {
                    return EditorLogic.CurrentScreenSave.GetPropertyValue(EditorLogic.CurrentCustomVariable.Name);
                }

            }

            #endregion

            #region ScreenSave

            else if (EditorLogic.CurrentScreenSave != null)
            {
                if (this.Attributes.Count > 0 && this.Attributes[0] is CategoryAttribute)
                {
                    string category = ((CategoryAttribute)this.Attributes[0]).Category;

                    switch (category)
                    {
                        case "Active Events":
                            EventResponseSave eventSave = EditorLogic.CurrentScreenSave.GetEvent(Name);
                            //if (eventSave != null)
                            //{
                            //    return eventSave.InstanceMethod;
                            //}
                            //else
                            {
                                return "";
                            }
                        case "Unused Events":
                            return "";
                        default:
                            return "";
                    }
                }
                return "";
            }

            #endregion

            #region EntitySave

            else if (EditorLogic.CurrentEntitySave != null)
            {
                return EditorLogic.CurrentEntitySave.GetPropertyValue(this.Name);

            }

            #endregion

            else
            {
                return null;
            }
        }

        public override bool IsReadOnly
        {
            get { return false; }
        }

        public override Type PropertyType
        {
            get 
			{
				if (mParameterType == typeof(bool))
				{
					return typeof(bool);
				}
				else
				{
					return typeof(string);
				}
			}
        }

        public override void ResetValue(object component)
        {
            
            // do nothing here
        }

        public override void SetValue(object component, object value)
		{
            value = ConvertValueToType(value);

            if (Owner != null)
			{
				if (Owner is FileAssociationSettings)
				{
					((FileAssociationSettings)Owner).SetApplicationForExtension(this.Name, (string)value);
				}
			}

            else if (EditorLogic.CurrentStateSave != null)
            {
                EditorLogic.CurrentStateSave.SetValue(this.Name, value);
            }

            else if (EditorLogic.CurrentNamedObject != null)
            {
                NamedObjectSave namedObject = EditorLogic.CurrentNamedObject;

                //if (this.Attributes.Count > 0 && this.Attributes[0] is CategoryAttribute)
                //{
                //    string category = ((CategoryAttribute)this.Attributes[0]).Category;

                //    switch (category)
                //    {
                //        case "Active Events":
                //        case "Unused Events":

                //            SetEventValue(category, namedObject, (string)value);
                //            break;
                //        case "Custom Variable Set Events":

                //            if ((string)value == "<NONE>")
                //            {
                //                namedObject.GetCustomVariable(Name.Replace(" Set", "")).EventOnSet = "";
                //            }
                //            else
                //            {

                //                namedObject.GetCustomVariable(Name.Replace(" Set", "")).EventOnSet = (string)value;
                //            }
                //            break;
                //        default:
                //            EditorLogic.CurrentNamedObject.SetPropertyValue(Name, value);
                //            break;
                //    }
                //}
                //else
                {
                    EditorLogic.CurrentNamedObject.SetPropertyValue(Name, value);
                }


                // Set the value here.


            }
            else if (EditorLogic.CurrentCustomVariable != null)
            {
                if (EditorLogic.CurrentEntitySave != null)
                {
                    EditorLogic.CurrentEntitySave.SetPropertyValue(EditorLogic.CurrentCustomVariable.Name, value);
                }
                else
                {
                    EditorLogic.CurrentScreenSave.SetPropertyValue(EditorLogic.CurrentCustomVariable.Name, value);

                }
            }
            // Do CurrentScreens and CurrentEntities AFTER named objects.
            else if (EditorLogic.CurrentEntitySave != null)
            {
                EditorLogic.CurrentEntitySave.SetPropertyValue(Name, value);
            }
            else if (EditorLogic.CurrentScreenSave != null)
            {
                if (this.Attributes.Count > 0 && this.Attributes[0] is CategoryAttribute)
                {
                    SetEventValue(((CategoryAttribute)this.Attributes[0]).Category, EditorLogic.CurrentScreenSave, (string)value);

                }
            }
        }

        private object ConvertValueToType(object value)
        {

            Type type = mParameterType;


            value = PropertyValuePair.ConvertStringToType((string)value, type);
            return value;
        }


        private void SetEventValue(string categoryString, IEventContainer eventContainer, string value)
        {
            switch (categoryString)
            {
                case "Active Events":

                    EventResponseSave eventToModify = null;

                    for (int i = eventContainer.Events.Count - 1; i > -1; i--)
                    {
                        if (eventContainer.Events[i].EventName == this.Name)
                        {
                            eventToModify = eventContainer.Events[i];
                            break;
                        }
                    }

                    if (eventToModify == null)
                    {
                        throw new Exception("Could not find an event by the name of " + Name);
                    }
                    else
                    {
                        string valueAsString = value;

                        if (string.IsNullOrEmpty(valueAsString) || valueAsString == "<NONE>")
                        {
                            eventContainer.Events.Remove(eventToModify);
                        }
                        else
                        {
                            //eventToModify.InstanceMethod = valueAsString;
                        }
                    }
                    //EventSave eventSave = EditorLogic.Current
                    break;
                case "Unused Events":

                    //EventSave eventSave = EventManager.AllEvents[Name];

                    //eventSave.InstanceMethod = value;

                    //EditorLogic.CurrentEventContainer.Events.Add(eventSave);
                    break;
            }
        }

        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }
    }
}
