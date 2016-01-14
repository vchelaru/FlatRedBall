using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Reflection;
using FlatRedBall.Content.SpriteFrame;
using System.Globalization;

namespace EditorObjects.Cleaners
{
    public static class GeneralCleaner
    {
        static Type floatType = typeof(float);
        static Type boolType = typeof(bool);
        static Type stringType = typeof(string);

        public static void CleanNode(XmlNode node, object saveObject, Type saveType)
        {

            for (int i = node.ChildNodes.Count - 1; i > -1; i--)
            {
                XmlNode childNode = node.ChildNodes[i];
                string memberName = childNode.Name;

                FieldInfo fieldInfo = saveType.GetField(memberName);
                PropertyInfo propertyInfo = saveType.GetProperty(memberName);

                #region There is a field Info

                if (fieldInfo != null || propertyInfo != null)
                {
                    Type memberType = null;

                    if (fieldInfo != null)
                    {
                        memberType = fieldInfo.FieldType;
                    }
                    else
                    {
                        memberType = propertyInfo.PropertyType;
                    }

                    if (memberType == floatType)
                    {
                        float defaultValue = 0;

                        if (fieldInfo != null)
                        {
                            defaultValue = (float)fieldInfo.GetValue(saveObject);
                        }
                        else if (propertyInfo != null)
                        {
                            defaultValue = (float)propertyInfo.GetValue(saveObject, null);
                        }

                        if (childNode.InnerText == "INF")
                        {
                            if (float.IsInfinity(defaultValue))
                            {
                                node.RemoveChild(childNode);
                            }
                        }
                        else if (defaultValue == float.Parse(childNode.InnerText, CultureInfo.InvariantCulture))
                        {
                            //remove
                            node.RemoveChild(childNode);
                        }
                    }

                    else if (memberType == stringType)
                    {
                        string defaultValue = null;
                        if (fieldInfo != null)
                        {
                            defaultValue = (string)fieldInfo.GetValue(saveObject);
                        }
                        else if (propertyInfo != null)
                        {
                            defaultValue = (string)propertyInfo.GetValue(saveObject, null);
                        }

                        if (defaultValue == childNode.InnerText)
                        {
                            //remove
                            node.RemoveChild(childNode);
                        }
                    }

                    else if (memberType == boolType)
                    {
                        bool defaultValue = false;

                        if (fieldInfo != null)
                        {
                            defaultValue = (bool)fieldInfo.GetValue(saveObject);
                        }
                        else if (propertyInfo != null)
                        {
                            defaultValue = (bool)propertyInfo.GetValue(saveObject, null);
                        }
                        
                        if (defaultValue == bool.Parse(childNode.InnerText))
                        {
                            //remove
                            node.RemoveChild(childNode);
                        }
                    }
                    else
                    {
                        if (saveObject is SpriteFrameSave && childNode.Name == "ParentSprite")
                        {
                            CleanNode(childNode, ScnxCleaner.SpriteSave, ScnxCleaner.SpriteSaveType);
                        }
                        else if (saveObject is FlatRedBall.Content.Particle.EmitterSave && childNode.Name == "ParticleBlueprint")
                        {
                            CleanNode(childNode, ScnxCleaner.SpriteSave, ScnxCleaner.SpriteSaveType);
                        }
                        else if (saveObject is FlatRedBall.Content.Particle.EmitterSave && childNode.Name == "EmissionSettings")
                        {
                            CleanNode(childNode, EmixCleaner.EmissionSettingsSave, EmixCleaner.EmissionSettingsSaveType);
                        }
                        else if (saveObject is FlatRedBall.Content.Particle.EmitterSave && childNode.Name == "EmissionBoundary")
                        {
                            CleanNode(childNode, ShcxCleaner.PolygonSave, ShcxCleaner.PolygonSaveType);
                        }
                        else if (saveObject is FlatRedBall.Content.Polygon.PolygonSave && childNode.Name == "Points")
                        {
                            CleanNode(childNode, ShcxCleaner.Point, ShcxCleaner.PointType);
                        }
                        else if (saveObject is FlatRedBall.Graphics.Particle.EmissionSettingsSave && childNode.Name == "Instructions")
                        {
                            continue;
                        }
                        else
                        {
                            string valueAsString = "";

                            if (fieldInfo != null)
                            {
                                valueAsString = fieldInfo.GetValue(saveObject).ToString();
                            }
                            else
                            {
                                valueAsString = propertyInfo.GetValue(saveObject, null).ToString();
                            }
                            if (valueAsString == childNode.InnerText)
                            {
                                node.RemoveChild(childNode);
                            }
                        }

                    }

                }
                #endregion
            }

        }



    }
}
