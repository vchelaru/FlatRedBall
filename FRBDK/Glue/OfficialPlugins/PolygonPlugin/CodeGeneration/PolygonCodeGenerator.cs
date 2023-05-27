using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.PolygonPlugin.CodeGeneration
{
    internal class PolygonCodeGenerator : ElementComponentCodeGenerator
    {
        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, IElement element)
        {
            foreach(var variable in element.CustomVariables)
            {
                if(variable.SourceObject != null)
                {
                    var nos = element.GetNamedObjectRecursively(variable.SourceObject);

                    if(nos?.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.Polygon && variable.SourceObjectProperty == "Points")
                    {
                        var propBlock = codeBlock.Property($"public IList<FlatRedBall.Math.Geometry.Point>", $"{variable.Name}");
                        propBlock.Get().Line($"return {nos.InstanceName}.Points;");
                        propBlock.Set().Line($"{nos.InstanceName}.Points = value;");
                        //codeBlock.Line($"public FlatRedBall.Math.Geometry.Polygon {variable.Name};");
                    }
                }
            }
            //foreach(var instance in element.NamedObjects)
            //{
            //    var ati = instance.GetAssetTypeInfo();

            //    //if(instance.BaseType == "Polygon")
            //    //{
            //    //    codeBlock.Line($"public FlatRedBall.Math.Geometry.Polygon {instance.FieldName};");
            //    //}
            //}

            return base.GenerateFields(codeBlock, element);
        }
    }
}
