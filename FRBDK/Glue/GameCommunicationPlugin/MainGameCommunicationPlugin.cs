using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;
using GameCommunicationPlugin.Common;
using GameJsonCommunicationPlugin.Common;
using Newtonsoft.Json;
using GameCommunicationPlugin.GlueControl.CodeGeneration;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using GameCommunicationPlugin.CodeGeneration;
using EmbeddedCodeManager = GameCommunicationPlugin.CodeGeneration.EmbeddedCodeManager;
using ToolsUtilities;

namespace GameCommunicationPlugin
{
    [Export(typeof(PluginBase))]
    public class MainGameCommunicationPlugin : PluginBase
    {
        #region Fields/Properties

        private GameConnectionManager _gameCommunicationManager;
        private Game1GlueCommunicationGenerator game1GlueCommunicationGenerator;

        public override string FriendlyName => "Game Communication Plugin";

        public override Version Version => new Version(1, 0);

        public static MainGameCommunicationPlugin Self { get; private set; }

        #endregion

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            ReactToLoadedGlux -= HandleGluxLoaded;
            _gameCommunicationManager.OnPacketReceived -= HandleOnPacketReceived;
            _gameCommunicationManager.Dispose();
            _gameCommunicationManager = null;
            GameConnectionManager.Self = null;
            return true;
        }

        public override void StartUp()
        {
            Self = this;
            _gameCommunicationManager = new GameConnectionManager(ReactToPluginEvent);
            _gameCommunicationManager.Port = 8888;
            _gameCommunicationManager.OnPacketReceived += HandleOnPacketReceived;
            GameConnectionManager.Self = _gameCommunicationManager;

            ReactToLoadedGlux += HandleGluxLoaded;

            game1GlueCommunicationGenerator = new Game1GlueCommunicationGenerator(true, 8888);
            RegisterCodeGenerator(game1GlueCommunicationGenerator);

            //Task.Run(() =>
            //{
            //    while (true)
            //    {
            //        Thread.Sleep(500);
            //        _gameCommunicationManager?.SendItem(new GameConnectionManager.Packet
            //        {
            //            PacketType = "Test",
            //            Payload = DateTime.Now.ToLongTimeString(),
            //        });
            //    }
            //});
        }

        public override void HandleEvent(string eventName, string payload)
        {
            base.HandleEvent(eventName, payload);

            switch(eventName)
            {
                case "GameCommunication_SendPacket":
                    _gameCommunicationManager.SendItem(JsonConvert.DeserializeObject<GameConnectionManager.Packet>(payload));

                    break;
            }
        }

        protected override async Task<string> HandleEventWithReturnImplementation(string eventName, string payload)
        {
            switch (eventName)
            {
                case "GameCommunication_SendPacket":
                    var returnValue = await _gameCommunicationManager.SendItemWithResponse(JsonConvert.DeserializeObject<GameConnectionManager.Packet>(payload));

                    return returnValue.Data?.Payload;
            }

            return null;
        }

        public void SetPrimarySettings(int portNumber, bool doConnections)
        {
            _gameCommunicationManager.Port = portNumber;
            _gameCommunicationManager.DoConnections = doConnections;
            game1GlueCommunicationGenerator.PortNumber = _gameCommunicationManager.Port;
            game1GlueCommunicationGenerator.IsGameCommunicationEnabled = _gameCommunicationManager.DoConnections;
        }

        private void HandleOnPacketReceived(GameConnectionManager.PacketReceivedArgs packetReceivedArgs)
        {
            if(!string.IsNullOrEmpty(packetReceivedArgs.Packet.Payload))
            {
                ReactToPluginEvent($"GameCommunicationPlugin_PacketReceived_{packetReceivedArgs.Packet.PacketType}", packetReceivedArgs.Packet.Payload);
                Debug.WriteLine($"Packet Type: {packetReceivedArgs.Packet.PacketType}, Payload: {packetReceivedArgs.Packet.Payload}");
            }
        }

        private void HandleGluxLoaded()
        {
            if (GameCommunicationHelper.IsFrbNewEnough())
            {
                EmbeddedCodeManager.Embed(new System.Collections.Generic.List<string>
                {
                    "GameConnectionManager.cs"
                });
            }
        }
    }
}
