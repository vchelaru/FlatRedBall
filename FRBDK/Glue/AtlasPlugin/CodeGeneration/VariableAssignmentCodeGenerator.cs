using AtlasPlugin.Managers;
using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasPlugin.CodeGeneration
{
    static class VariableAssignmentCodeGenerator
    {
        public static void HandleWriteInstanceVariableAssignment(NamedObjectSave instance, ICodeBlock code, InstructionSave variable)
        {
            var shouldHandle = variable.Member == "AtlasedTexture" && instance.SourceClassType == "Sprite";

            if(shouldHandle)
            {
                var memberName = instance.InstanceName;

                // The code should look something like:
                // 
                // SpriteInstance.AtlasedTexture = FlatRedBall.Graphics.Texture.AtlasLoader.LoadAtlasedTexture("asdf");
                //
                // But I still need to make the AtlasLoader keep track of all loaded assets, and I need to have a "priority" system

                code = code.Block();
                {
                    // eventually this might exist
                    //var atlas = instance.GetInstructionFromMember("");
                    // for now we assume that the texture packer project is in global content
                    var rfs = GlueState.Self.CurrentGlueProject.GlobalFiles.FirstOrDefault(item =>
                    {
                        var ati = item.GetAssetTypeInfo();
                        if(ati != null)
                        {
                            return ati == AtiManager.Self.TextureAtlasAti;
                        }

                        return false;
                    });

                    if (rfs != null)
                    {
                        var atlas = $"{GlueState.Self.ProjectNamespace}.GlobalContent.{rfs.GetInstanceName()}";
                        code.Line($"var atlas = {atlas};");

                        code.Line($"{memberName}.AtlasedTexture = atlas.Sprite(\"{variable.Value}\");");
                    }
                }
            }
        }

    }
}
