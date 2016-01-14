using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FlatRedBall.Utilities;
using FlatRedBall.Content.Scene;
using FlatRedBall.IO;
using System.Globalization;

namespace FlatRedBall.Content.Xml
{
    public struct XmlLine
    {
        public string Name;
        public string Value;
        public bool IsClosing;

        public XmlLine(string line)
        {
            if (line.Contains("</"))
            {
                string trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("</"))
                {
                    IsClosing = true;
                    Name = trimmedLine.Substring(2, trimmedLine.IndexOf(">") - 2);
                    Value = "";
                }
                else
                {
                    IsClosing = false;
                    // We got something like <X>0</X>
                    Name = trimmedLine.Substring(1, trimmedLine.IndexOf(">") - 1);

                    int firstClosingBracket = trimmedLine.IndexOf(">");
                    int secondOpeningBracket = trimmedLine.IndexOf("<", firstClosingBracket);



                    Value = trimmedLine.Substring(firstClosingBracket + 1, secondOpeningBracket - firstClosingBracket - 1);
                }
            }
            else
            {
                IsClosing = false;
                Name = line.Trim();
                Name = Name.Substring(1, Name.Length - 2);

                if (Name.Contains(" "))
                {
                    Name = Name.Substring(0, Name.IndexOf(" "));
                }

                Value = "";
            }
        }

        public override string ToString()
        {
            return string.Format("{0} = {1}", Name, Value);
        }

    }


    public delegate bool TypeBasedShouldRemoveLine(XmlLine line);

    public static class XmlCleaner
    {
        static Stack<TypeBasedShouldRemoveLine> TypeBasedRemovals = new Stack<TypeBasedShouldRemoveLine>();
        static Stack<Type> TypeStack = new Stack<Type>();
        static Stack<object> ObjectStack = new Stack<object>();

        static StringBuilder stringBuilder = new StringBuilder();

        public static void CleanXml(string fileName)
        {
            stringBuilder.Remove(0, stringBuilder.Length);

            using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				using (System.IO.StreamReader sr = new StreamReader(fileStream))
				{
                    while (!sr.EndOfStream)
                    {
                        string lineAsString = sr.ReadLine();
                        XmlLine line = new XmlLine(lineAsString);

                        if (!ShouldRemoveLine(line))
                        {
                            stringBuilder.AppendLine(lineAsString);
                        }
                    }

					sr.Close();
				}
			}

            string newFileName = FileManager.RemoveExtension(fileName) + "Clean" + "." + FileManager.GetExtension(fileName);

            FileManager.SaveText(stringBuilder.ToString(), newFileName);

        }

        private static bool ShouldRemoveLine(XmlLine line)
        {
            bool returnValue = true;

            if (line.IsClosing)
            {
                //if (TypeStack.Count == 1)
                //{
                //    int m = 3;
                //}
                PopLayer();
                returnValue = false;
            }

            else if (line.Name.StartsWith("?xml"))
            {
                returnValue = false;
            }
            else
            {

                switch (line.Name)
                {
                    case "SpriteEditorScene":
                        PushNewLayer(ShouldRemoveLineSpriteEditorScene, typeof(SpriteEditorScene), new SpriteEditorScene());

                        returnValue = false;
                        break;
                    default:
                        if (TypeBasedRemovals.Count == 0)
                        {
                            returnValue = false;
                        }
                        else
                        {
                            returnValue = TypeBasedRemovals.Peek()(line);
                        }
                        break;

                }

            }

            return returnValue;
        }

        private static bool ShouldRemoveLineSpriteEditorScene(XmlLine line)
        {
            bool returnValue = false;


            switch (line.Name)
            {
                case "Sprite":
                    PushNewLayer(ShouldRemoveLineGeneral, typeof(SpriteSave), new SpriteSave());

                    returnValue = false;
                    break;
                case "Camera":
                    PushNewLayer(ShouldRemoveLineGeneral, typeof(CameraSave), new CameraSave());

                    break;
                
            }

            return returnValue;
        }


        private static bool ShouldRemoveLineGeneral(XmlLine line)
        {
            Type currentType = TypeStack.Peek();

            if (currentType.GetField(line.Name) != null)
            {
                object value = currentType.GetField(line.Name).GetValue(ObjectStack.Peek());

                if (value is float)
                {
                    return (float)value == float.Parse(line.Value, CultureInfo.InvariantCulture);
                }
                else if (value is string)
                {
                    return (string)value == line.Value;
                }
                else if (value is bool)
                {
                    return (bool)value == bool.Parse(line.Value.ToLower());
                }
                else if (value is int)
                {
                    return (int)value == int.Parse(line.Value);
                }
                else if (value is Microsoft.Xna.Framework.Graphics.TextureAddressMode)
                {
                    return (Microsoft.Xna.Framework.Graphics.TextureAddressMode)value ==
                        (Microsoft.Xna.Framework.Graphics.TextureAddressMode)Enum.Parse(
                        typeof(Microsoft.Xna.Framework.Graphics.TextureAddressMode),
                            line.Value, true);

                }
            }
            

            return false;
        }


        static void PushNewLayer(TypeBasedShouldRemoveLine method, Type typeOfObject, object objectInstance)
        {
            TypeBasedRemovals.Push(method);
            TypeStack.Push(typeOfObject);
            ObjectStack.Push(objectInstance);
        }

        static void PopLayer()
        {
            TypeBasedRemovals.Pop();
            TypeStack.Pop();
            ObjectStack.Pop();
        }
    }
}
