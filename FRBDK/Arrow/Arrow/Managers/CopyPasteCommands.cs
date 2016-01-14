using FlatRedBall.Arrow.ViewModels;
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.Content.Scene;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.IO;
using FlatRedBall.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace FlatRedBall.Arrow.Managers
{
    public class CopyPasteCommands
    {
        public void AddToClipboard(ArrowInstanceGeneralVm instanceVm)
        {
            try
            {
                string dataType = typeof(ArrowInstanceGeneralVm).Name + instanceVm.Model.GetType().Name;
                Clipboard.SetData(dataType, SerializeToString(instanceVm.Model));
            }
            catch
            {
                // do nothing?
            }
        }

        public bool TryPasteInstance()
        {
            bool wasPasted = false;

            string instanceType = GetInstanceType();

            var currentArrowElement = ArrowState.Self.CurrentArrowElementSave;

            if (currentArrowElement != null && !string.IsNullOrEmpty(instanceType))
            {
                string xmlSerializedString = 
                    Clipboard.GetData(typeof(ArrowInstanceGeneralVm).Name + instanceType) as string;

                object newObject = CreateAndAddInstanceFromClipboard(instanceType, currentArrowElement, xmlSerializedString);

                ArrowCommands.Self.Add.MakeNewObjectUnique(currentArrowElement, newObject);

                ArrowCommands.Self.File.SaveProject();
                ArrowCommands.Self.File.GenerateGlux();

                ArrowCommands.Self.UpdateToSelectedElement();

                ArrowState.Self.CurrentInstance = newObject;

                //if (ArrowState.Self.CurrentArrowElementSave != null)
                //{
                //    object clonedToPaste = FileManager.CloneObject<object>( instanceVm.GeneralInstance);
                //    wasPasted = true;
                //}
            }

            return wasPasted;

        }

        private static object CreateAndAddInstanceFromClipboard(string instanceType, DataTypes.ArrowElementSave currentArrowElement, string xmlSerializedString)
        {
            object newObject = null;

            if (instanceType == typeof(AxisAlignedRectangleSave).Name)
            {

                AxisAlignedRectangleSave aars = FileManager.XmlDeserializeFromString<AxisAlignedRectangleSave>(
                    xmlSerializedString);

                currentArrowElement.Rectangles.Add(aars);
                newObject = aars;
            }
            else if (instanceType == typeof(CircleSave).Name)
            {

                CircleSave circleSave = FileManager.XmlDeserializeFromString<CircleSave>(
                    xmlSerializedString);

                currentArrowElement.Circles.Add(circleSave);
                newObject = circleSave;

            }
            else if (instanceType == typeof(SpriteSave).Name)
            {

                SpriteSave spriteSave = FileManager.XmlDeserializeFromString<SpriteSave>(
                    xmlSerializedString);

                currentArrowElement.Sprites.Add(spriteSave);
                newObject = spriteSave;
            }

            return newObject;
        }

        public string GetInstanceType()
        {
            if (Clipboard.ContainsData(typeof(ArrowInstanceGeneralVm).Name + typeof(CircleSave).Name))
            {
                return typeof(CircleSave).Name;
            }
            else if (Clipboard.ContainsData(typeof(ArrowInstanceGeneralVm).Name + typeof(AxisAlignedRectangleSave).Name))
            {
                return typeof(AxisAlignedRectangleSave).Name;
            }
            else if (Clipboard.ContainsData(typeof(ArrowInstanceGeneralVm).Name + typeof(SpriteSave).Name))
            {
                return typeof(SpriteSave).Name;
            }
            return null;
        }

        public string SerializeToString(object toSerialize)
        {
            string toReturn = null;
            if (toSerialize is AxisAlignedRectangleSave)
            {
                FileManager.XmlSerialize(toSerialize as AxisAlignedRectangleSave, out toReturn);
            }
            else if (toSerialize is CircleSave)
            {
                FileManager.XmlSerialize(toSerialize as CircleSave, out toReturn);
            }

            else if (toSerialize is SpriteSave)
            {
                FileManager.XmlSerialize(toSerialize as SpriteSave, out toReturn);
            }

            else
            {
                throw new NotImplementedException("Need to add support for type " + toSerialize.GetType());
            }
            return toReturn;
        }


    }
}
