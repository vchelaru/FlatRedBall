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

        public bool IsGameCommunicationEnabled { get; set; }
        public int PortNumber { get; set; }
        public override void GenerateClassScope(ICodeBlock codeBlock)
        {
            if(IsGameCommunicationEnabled)
            {
                codeBlock.Line("GlueCommunication.GameConnectionManager gameConnectionManager;");
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

                //if(GameCommunicationHelper.IsFrbUsesJson())
                //{
                //    codeBlock.Line("GlueCommunication.GameConnectionManager.CanUseJsonManager = true;");
                //    codeBlock.Line("var gjmInstance = GlueCommunication.Json.GlueJsonManager.Instance;");
                //    codeBlock.Line("gameConnectionManager.OnPacketReceived += async (packet) =>");
                //    codeBlock.Line("{");
                //    codeBlock.Line("    if (packet.Packet.PacketType == \"JsonUpdate\")");
                //    codeBlock.Line("    {");
                //    codeBlock.Line("        await gjmInstance.ProcessUpdatePacket(packet.Packet);");
                //    codeBlock.Line("    }");
                //    codeBlock.Line("};");
                //    //codeBlock.Line("gjmInstance.HandleUpdatedSelection += async (dto) => await glueControlManager.ProcessMessage(dto);");
                //    //codeBlock.Line("gjmInstance.SendPacket += (packet) => gameConnectionManager.SendItem(packet);");
                //    //codeBlock.Line("gjmInstance.SendPacketWithResponse += (packet) => { return gameConnectionManager.SendItemWithResponse(packet); };");
                //}

                //Test Block
                //codeBlock.Line("System.Threading.Tasks.Task.Run(() =>");
                //codeBlock.Line("{");
                //codeBlock.Line("    while (true)");
                //codeBlock.Line("    {");
                //codeBlock.Line("        System.Threading.Thread.Sleep(500);");
                //codeBlock.Line("        gameConnectionManager.SendItem(new GlueCommunication.GameConnectionManager.Packet");
                //codeBlock.Line("        {");
                //codeBlock.Line("            PacketType = \"Test\",");
                //codeBlock.Line("            Payload = System.DateTime.Now.ToLongTimeString(),");
                //codeBlock.Line("        });");
                //codeBlock.Line("    }");
                //codeBlock.Line("});");
            }
        }
    }
}
