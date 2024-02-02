using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.CodeGeneration.Game1;
using GameCommunicationPlugin.Common;

namespace GameCommunicationPlugin.CodeGeneration
{
    public class Game1GlueCommunicationGenerator : Game1CodeGenerator
    {

        public Game1GlueCommunicationGenerator(bool isGameCommunicationEnabled, int portNumber)
        {
            IsGameCommunicationEnabled = isGameCommunicationEnabled;
            PortNumber = portNumber;
        }

        public bool GenerateConnectionOnlyInDebug { get; set; } = true;
        public bool IsGameCommunicationEnabled { get; set; }
        public int PortNumber { get; set; }
        public override void GenerateClassScope(ICodeBlock codeBlock)
        {
            if(IsGameCommunicationEnabled)
            {
                AddIfDebug(codeBlock);
                codeBlock.Line("GlueCommunication.GameConnectionManager gameConnectionManager;");
                EndIfDebug(codeBlock);
            }
        }

        private void AddIfDebug(ICodeBlock codeBlock)
        {
            if (GenerateConnectionOnlyInDebug)
            {
                codeBlock.Line("#if DEBUG");
            }
        }


        private void EndIfDebug(ICodeBlock codeBlock)
        {
            if (GenerateConnectionOnlyInDebug)
            {
                codeBlock.Line("#endif");
            }
        }

        public override void GenerateInitialize(ICodeBlock codeBlock)
        {
            GenerateGameCommunicationInitialize(codeBlock);
        }

        private void GenerateGameCommunicationInitialize(ICodeBlock codeBlock)
        {
            if (IsGameCommunicationEnabled)
            {
                codeBlock.Line("System.AppDomain currentDomain = System.AppDomain.CurrentDomain;");
                codeBlock.Line("currentDomain.AssemblyResolve += (s, e) =>");
                codeBlock.Line("{");
                codeBlock.Line("    // Get just the name of assmebly");
                codeBlock.Line("    // Aseembly name excluding version and other metadata");
                codeBlock.Line("    string name = e.Name.Contains(\", \") ? e.Name.Substring(0, e.Name.IndexOf(\", \")) : e.Name;");
                codeBlock.Line("");
                codeBlock.Line("    if (name == \"Newtonsoft.Json\")");
                codeBlock.Line("    {");
                codeBlock.Line("        // Load whatever version available");
                codeBlock.Line("        return System.Reflection.Assembly.Load(name);");
                codeBlock.Line("    }");
                codeBlock.Line("");
                codeBlock.Line("    return null;");
                codeBlock.Line("};");
                codeBlock.Line();

                AddIfDebug(codeBlock);
                codeBlock.Line($"gameConnectionManager = new GlueCommunication.GameConnectionManager({PortNumber});");
                codeBlock.Line("gameConnectionManager.OnPacketReceived += async (packet) =>");
                codeBlock.Line("{");
                codeBlock.Line("    if (packet.Packet.PacketType == \"OldDTO\" && glueControlManager != null)");
                codeBlock.Line("    {");
                codeBlock.Line("        var returnValue = await glueControlManager?.ProcessMessage(packet.Packet.Payload);");
                codeBlock.Line("");
                codeBlock.Line("        gameConnectionManager.SendItem(new GlueCommunication.GameConnectionManager.Packet");
                codeBlock.Line("        {");
                codeBlock.Line("            PacketType = \"OldDTO\",");
                codeBlock.Line("            Payload = returnValue,");
                codeBlock.Line("            InResponseTo = packet.Packet.Id");
                codeBlock.Line("        });");
                codeBlock.Line("    }");
                codeBlock.Line("};");
                codeBlock.Line("this.Exiting += (not, used) => gameConnectionManager.Dispose();");
                EndIfDebug(codeBlock);
            }
        }
    }
}
