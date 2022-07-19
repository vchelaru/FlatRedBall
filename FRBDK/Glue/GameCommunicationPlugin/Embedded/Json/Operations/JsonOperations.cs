namespace GlueCommunication.Json.Operations
{
    internal class JsonOperations
    {
        private static JsonOperations _instance;

        public static JsonOperations Self {  get { return _instance; } }

        static JsonOperations()
        {
            _instance = new JsonOperations();
        }

        public JsonOperations()
        {
            GluxCommands = new GluxCommands();
        }

        public GluxCommands GluxCommands { get; set; }
    }
}
