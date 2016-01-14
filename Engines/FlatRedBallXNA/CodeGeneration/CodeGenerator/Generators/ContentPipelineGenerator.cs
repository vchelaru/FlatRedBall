using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Content.Scene;
using System.Reflection;
using FlatRedBall.IO;
using System.Diagnostics;
using System.Xml.Serialization;
using FlatRedBall.Attributes;
using FlatRedBall.Content;
using FlatRedBall.Content.SpriteGrid;
using FlatRedBall.Content.Model;
using FlatRedBall.Content.SpriteFrame;
using FlatRedBall.Content.Saves;
using FlatRedBall.Content.Lighting;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.Animation3D;
using FlatRedBall.Content.Model.Animation;

namespace CodeGenerator.Generators
{
    public struct ReaderWriter
    {
        public string Reader;
        public string Writer;
        public ReaderWriter(string reader, string writer)
        {
            Reader = reader;
            Writer = writer;
        }
    }
        

    public static class ContentPipelineGenerator
    {
        static Dictionary<string, string> mTypesUsingObjectReaderWriter = new Dictionary<string, string>();

        public static void CreateContentPipelineFiles()
        {
            // We don't want to associate AnimationChainListSave because that's an ignored type in the SpriteSave.
            // This just serves as an example if we do want to add types here.
            //mTypeContentPipelineAssociation.Add("AnimationChainListSave", new ReaderWriter("AnimationChainListReader", "AnimationChainArrayWriter"));
            mTypesUsingObjectReaderWriter.Add("SpriteSaveContent", typeof(SpriteSave).FullName);
            mTypesUsingObjectReaderWriter.Add("SpriteGridSaveContent", typeof(SpriteGridSave).FullName);
            mTypesUsingObjectReaderWriter.Add("PositionedModelSaveContent", typeof(PositionedModelSave).FullName);
            mTypesUsingObjectReaderWriter.Add("SpriteFrameSaveContent", typeof(SpriteFrameSave).FullName);
            mTypesUsingObjectReaderWriter.Add("TextSaveContent", typeof(TextSave).FullName);
            mTypesUsingObjectReaderWriter.Add("LightSave", typeof(LightSave).FullName);
            mTypesUsingObjectReaderWriter.Add("AnimationChainSaveContent", typeof(AnimationChainSave).FullName);
            mTypesUsingObjectReaderWriter.Add("AnimationFrameSaveContent", typeof(AnimationFrameSave).FullName);
            mTypesUsingObjectReaderWriter.Add("SourceReferencingFile", typeof(SourceReferencingFile).FullName);
            mTypesUsingObjectReaderWriter.Add("ModelMeshSave", typeof(ModelMeshSave).FullName);
            mTypesUsingObjectReaderWriter.Add("Animation3DInstanceSave", typeof(Animation3DInstanceSave).FullName);


            CreateReadersAndWriters(typeof(SpriteSave), typeof(SpriteSaveContent), false);
            CreateReadersAndWriters(typeof(SpriteEditorScene), typeof(SpriteEditorSceneContent), false);
            CreateReadersAndWriters(typeof(TextSave), typeof(TextSaveContent), false);
            CreateReadersAndWriters(typeof(AnimationChainListSave), typeof(AnimationChainListSaveContent), false);
            CreateReadersAndWriters(typeof(AnimationChainSave), typeof(AnimationChainSaveContent), false);
            CreateReadersAndWriters(typeof(AnimationFrameSave), typeof(AnimationFrameSaveContent), false);
            CreateReadersAndWriters(typeof(PositionedModelSave), typeof(PositionedModelSaveContent), false);
            CreateReadersAndWriters(typeof(Animation3DListSave), typeof(Animation3DListSaveContent), false);
            CreateReadersAndWriters(typeof(SourceReferencingFile), typeof(SourceReferencingFile), false);
            CreateReadersAndWriters(typeof(Animation3DInstanceSave), typeof(Animation3DInstanceSave), true);
        }

        private static void CreateReadersAndWriters(Type runtimeType, Type pipelineType, bool openGeneratedCode)
        {
            StringBuilder writerStringBuilder = new StringBuilder() ;
            StringBuilder readerStringBuilder = new StringBuilder() ;


            writerStringBuilder.AppendLine("{");
            readerStringBuilder.AppendLine("{");

            readerStringBuilder.AppendLine(runtimeType.FullName + " newObject = new " + runtimeType.FullName + "();");

            CreateReadersAndWritersForType(pipelineType, runtimeType, readerStringBuilder, writerStringBuilder);

            readerStringBuilder.AppendLine("return newObject;");

            writerStringBuilder.AppendLine("}");
            readerStringBuilder.AppendLine("}");

            string writerFile = FileManager.UserApplicationDataForThisApplication + runtimeType.Name + "Writer.txt";
            string readerFile = FileManager.UserApplicationDataForThisApplication + runtimeType.Name + "Reader.txt";


            FileManager.SaveText(writerStringBuilder.ToString(), writerFile);
            FileManager.SaveText(readerStringBuilder.ToString(), readerFile);

            if (openGeneratedCode)
            {
                Process.Start(writerFile);
                Process.Start(readerFile);
            }
        }

        private static void CreateReadersAndWritersForType(Type pipelineType, Type runtimeType, StringBuilder readerStringBuilder, StringBuilder writerStringBuilder)
        {
            FieldInfo[] fields = pipelineType.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (FieldInfo field in fields)
            {
                string memberName = field.Name;
                    
                Type memberType = field.FieldType;

                GenerateCodeForMemberInfo(runtimeType, readerStringBuilder, writerStringBuilder, field, memberName, memberType);
            }

            PropertyInfo[] properties = pipelineType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                string memberName = property.Name;

                Type memberType = property.PropertyType;

                GenerateCodeForMemberInfo(runtimeType, readerStringBuilder, writerStringBuilder, property, memberName, memberType);

            }

        }

        private static void GenerateCodeForMemberInfo(Type runtimeType, StringBuilder readerStringBuilder, StringBuilder writerStringBuilder, MemberInfo member, string memberName, Type memberType)
        {
            if (IsInstanceMember(member))
            {
                CreateInstanceMemberCode(member, memberType, runtimeType, readerStringBuilder, writerStringBuilder);
            }
            else if (IsXmlIgnored(member)) // check this *after* checking the InstanceMember
            {
                //continue;
            }

            else if (IsExternalInstance(member))
            {
                // We skip ExternalInstances, the 
                //continue;
            }
            else
            {
                GenerateNormalReadWrite(readerStringBuilder, writerStringBuilder, memberName, memberType);
            }
        }

        private static void GenerateNormalReadWrite(StringBuilder readerStringBuilder, StringBuilder writerStringBuilder, string memberName, Type memberType)
        {
            if (memberType.IsPrimitive || memberType == typeof(decimal))
            {
                writerStringBuilder.AppendLine(string.Format("output.Write(value.{0});", memberName));
                readerStringBuilder.AppendLine(string.Format("newObject.{0} = input.Read{2}();", memberName, memberType, ReadTypeAsString(memberType)));
            }
            else if (memberType == typeof(string))
            {
                writerStringBuilder.AppendLine(string.Format("if(value.{0} != null)", memberName));
                writerStringBuilder.AppendLine(string.Format("\toutput.Write(value.{0});", memberName));
                writerStringBuilder.AppendLine(string.Format("else", memberName));
                writerStringBuilder.AppendLine("\toutput.Write(\"\");");

                readerStringBuilder.AppendLine(string.Format("newObject.{0} = input.ReadString();", memberName));
            }
            else if (mTypesUsingObjectReaderWriter.ContainsKey(memberType.Name))
            {
                writerStringBuilder.AppendLine(string.Format("ObjectWriter.WriteObject(output, value.{0});", memberName));
                readerStringBuilder.AppendLine(string.Format("newObject.{0} = ObjectReader.ReadObject<{1}>(input);", memberName, memberType));
            }
            else if (memberType.IsEnum)
            {
                writerStringBuilder.AppendLine(string.Format("output.Write(System.Convert.ToInt32(value.{0}));", memberName));
                readerStringBuilder.AppendLine(string.Format("newObject.{0} = ({1})Enum.ToObject(typeof({1}), (int)input.ReadInt32());", memberName, memberType));
            }
            else if (memberType.GetInterface("IList") != null)
            {
                WriteListCode(memberType, memberName, writerStringBuilder, readerStringBuilder);
            }
        }

        private static void CreateInstanceMemberCode(MemberInfo field, Type memberType, Type runtimeType, StringBuilder readerStringBuilder, StringBuilder writerStringBuilder)
        {
            InstanceMember instanceMemberAttribute = null;

            var attributes = field.GetCustomAttributes(true);

            foreach (object attribute in attributes)
            {
                if (attribute is InstanceMember)
                {
                    instanceMemberAttribute = ((InstanceMember)attribute);

                    break;
                }
            }

            string instanceMember = instanceMemberAttribute.memberName;
            FieldInfo runtimeInstanceFieldInfo = runtimeType.GetField(instanceMember, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (memberType.IsArray)
            {
                Type elementType = memberType.GetElementType().GetGenericArguments()[0];
                Type runtimeInstanceType = runtimeInstanceFieldInfo.FieldType.GetElementType();
                
                writerStringBuilder.AppendLine(string.Format("output.Write(value.{0} != null);", field.Name));
                writerStringBuilder.AppendLine(string.Format("if(value.{0} != null)", field.Name));
                writerStringBuilder.AppendLine("{");
                writerStringBuilder.AppendLine(string.Format("\toutput.Write(value.{0}.Length);", field.Name));
                writerStringBuilder.AppendLine(string.Format("\tforeach(Microsoft.Xna.Framework.Content.Pipeline.ExternalReference<{0}> instance in value.{1})", elementType, field.Name));
                writerStringBuilder.AppendLine("\t{");
                writerStringBuilder.AppendLine("\t\toutput.Write( instance != null );");
                writerStringBuilder.AppendLine("\t\tif( instance != null )");
                //writerStringBuilder.AppendLine(string.Format("\toutput.WriteExternalReference(value.{0});", field.Name));
                writerStringBuilder.AppendLine("\t\t\toutput.WriteExternalReference(instance);");
                writerStringBuilder.AppendLine("\t}");
                writerStringBuilder.AppendLine("}");

                readerStringBuilder.AppendLine("if(input.ReadBoolean() )");
                readerStringBuilder.AppendLine("{");
                readerStringBuilder.AppendLine(string.Format("\tint {0}Length = input.ReadInt32();", instanceMember));
                //readerStringBuilder.AppendLine(string.Format("\tnewObject.{0} = input.ReadExternalReference<{1}>();", instanceMember, runtimeInstanceFieldInfo.FieldType));  // Replace this
                readerStringBuilder.AppendLine(string.Format("\tnewObject.{0} = new {1}[{0}Length];", instanceMember, runtimeInstanceType));
                readerStringBuilder.AppendLine(string.Format("\tfor(int i = 0; i < {0}Length; i++)", instanceMember));
                readerStringBuilder.AppendLine("\t{");
                readerStringBuilder.AppendLine("\t\tif( input.ReadBoolean() )");
                readerStringBuilder.AppendLine(string.Format("\t\t\tnewObject.{0}[i] = input.ReadExternalReference<{1}>();", instanceMember, runtimeInstanceType));
                readerStringBuilder.AppendLine("\t}");
                readerStringBuilder.AppendLine("}");
            }
            else
            {



                writerStringBuilder.AppendLine(string.Format("output.Write(value.{0} != null);", field.Name));
                writerStringBuilder.AppendLine(string.Format("if(value.{0} != null)", field.Name));
                writerStringBuilder.AppendLine(string.Format("\toutput.WriteExternalReference(value.{0});", field.Name));

                readerStringBuilder.AppendLine(string.Format("if( input.ReadBoolean() )", field.Name));
                readerStringBuilder.AppendLine(string.Format("\tnewObject.{0} = input.ReadExternalReference<{1}>();", instanceMember, runtimeInstanceFieldInfo.FieldType));  // Replace this
            }
        }

        private static string ReadTypeAsString(Type type)
        {
            if (type == typeof(bool))
            {
                return "Boolean";
            }
            else if (type == typeof(byte))
            {
                return "Byte";
            }
            else if (type == typeof(char))
            {
                return "Char";
            }
            else if (type == typeof(Decimal))
            {
                return "Decimal";
            }
            else if (type == typeof(double))
            {
                return "Double";
            }
            else if (type == typeof(Int16))
            {
                return "Int16";
            }
            else if (type == typeof(Int32))
            {
                return "Int32";
            }
            else if (type == typeof(Int64))
            {
                return "Int64";
            }
            else if (type == typeof(sbyte))
            {
                return "SByte";
            }
            else if (type == typeof(Single))
            {
                return "Single";
            }
            else if (type == typeof(String))
            {
                return "String";
            }
            else if (type == typeof(UInt16))
            {
                return "UInt16";
            }
            else if(type == typeof(UInt32))
            {
                return "UInt32";
            }
            else if (type == typeof(UInt64))
            {
                return "UInt64";
            }

            throw new Exception();
        }

        static bool IsInstanceMember(MemberInfo memberInfo)
        {
            var attributes = memberInfo.GetCustomAttributes(true);

            foreach (object attribute in attributes)
            {
                if (attribute is InstanceMember)
                {
                    string memberName = ((InstanceMember)attribute).memberName;

                    return true;
                }
            }
            return false;
        }

        static bool IsXmlIgnored(MemberInfo memberInfo)
        {
            var attributes = memberInfo.GetCustomAttributes(true);

            foreach (object attribute in attributes)
            {
                if (attribute is XmlIgnoreAttribute)
                {
                    return true;
                }
            }
            return false;
        }

        static bool IsExternalInstance(MemberInfo memberInfo)
        {
            var attributes = memberInfo.GetCustomAttributes(true);

            foreach(object attribute in attributes)
            {
                // Since we don't have the right libs linked, we just use the type
                // LIES, we actually do, but this works too
                if (attributes.GetType().Name == "ExternalInstance")
                {
                    return true;
                }
            }



            return false;

        }

        static void WriteListCode(Type memberType, string memberName, StringBuilder writerStringBuilder, StringBuilder readerStringBuilder)
        {
            string contentType = memberType.GetGenericArguments()[0].Name;

            string contentTypeFullName = memberType.GetGenericArguments()[0].FullName;

            if (mTypesUsingObjectReaderWriter.ContainsKey(contentType))
            {

                writerStringBuilder.AppendLine(string.Format("output.Write(value.{0}.Count);", memberName));
                writerStringBuilder.AppendLine(string.Format("for (int i = 0; i < value.{0}.Count; i++)", memberName));
                writerStringBuilder.AppendLine(string.Format("\tObjectWriter.WriteObject(output, value.{0}[i]);", memberName));




                readerStringBuilder.AppendLine(string.Format("int {0}Count = input.ReadInt32();", memberName));
                readerStringBuilder.AppendLine(string.Format("for (int i = 0; i < {0}Count; i++)", memberName));
                readerStringBuilder.AppendLine(string.Format("\tnewObject.{0}.Add( ObjectReader.ReadObject<{1}>(input));", memberName, mTypesUsingObjectReaderWriter[contentType]));
            }
            else if (contentType == "String")
            {
                writerStringBuilder.AppendLine(string.Format("output.Write(value.{0}.Count);", memberName));
                writerStringBuilder.AppendLine(string.Format("for (int i = 0; i < value.{0}.Count; i++)", memberName));
                writerStringBuilder.AppendLine("{");
                writerStringBuilder.AppendLine(string.Format("\toutput.Write(value.{0}[i] != null);", memberName));
                writerStringBuilder.AppendLine(string.Format("\tif(value.{0}[i] != null)", memberName));
                writerStringBuilder.AppendLine(string.Format("\t\toutput.Write(value.{0}[i]);", memberName));
                writerStringBuilder.AppendLine("}");



                readerStringBuilder.AppendLine(string.Format("int {0}Count = input.ReadInt32();", memberName));
                readerStringBuilder.AppendLine(string.Format("for (int i = 0; i < {0}Count; i++)", memberName));
                readerStringBuilder.AppendLine("{");
                readerStringBuilder.AppendLine(string.Format("\tif(input.ReadBoolean())"));
                readerStringBuilder.AppendLine(string.Format("\t\tnewObject.{0}.Add( input.ReadString() );", memberName));
                readerStringBuilder.AppendLine("}");
            }
            else
            {
                throw new NotImplementedException("Encounterd a list of " + contentType + " and this type isn't defined in mTypesUsingObjectReaderWriter");
            }
            // coninue onward here
        }
    }
}
