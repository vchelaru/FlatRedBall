using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.CodeGeneration.Game1;

namespace OfficialPlugins.Compiler.CodeGeneration
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
                codeBlock.Line($"gameConnectionManager = new GlueCommunication.GameConnectionManager({PortNumber});");
                codeBlock.Line("this.Exiting += (not, used) => gameConnectionManager.Dispose();");

                //Test Block
                codeBlock.Line("System.Threading.Tasks.Task.Run(() =>");
                codeBlock.Line("{");
                codeBlock.Line("    while (true)");
                codeBlock.Line("    {");
                codeBlock.Line("        System.Threading.Thread.Sleep(500);");
                codeBlock.Line("        gameConnectionManager.SendItem(new GlueCommunication.GameConnectionManager.Packet");
                codeBlock.Line("        {");
                codeBlock.Line("            PacketType = \"Test\",");
                codeBlock.Line("            Payload = System.DateTime.Now.ToLongTimeString(),");
                codeBlock.Line("        });");
                codeBlock.Line("    }");
                codeBlock.Line("});");
            }
        }
    }
}
