using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;
using GameCommunicationPlugin.Common;
using GameJsonCommunicationPlugin.Common;
using Newtonsoft.Json;
using OfficialPlugins.Compiler.CodeGeneration;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace GameCommunicationPlugin
{
    [Export(typeof(PluginBase))]
    public class MainGameCommunicationPlugin : PluginBase
    {
        private GameConnectionManager _gameCommunicationManager;

        public override string FriendlyName => "Game Communication Plugin";

        public override Version Version => new Version(1, 0);

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            ReactToLoadedGlux -= HandleGluxLoaded;
            _gameCommunicationManager.OnPacketReceived -= HandleOnPacketReceived;
            _gameCommunicationManager.Dispose();
            _gameCommunicationManager = null;

            return true;
        }

        public override void StartUp()
        {
            _gameCommunicationManager = new GameConnectionManager();
            _gameCommunicationManager.Port = 8888;
            _gameCommunicationManager.OnPacketReceived += HandleOnPacketReceived;
            ReactToLoadedGlux += HandleGluxLoaded;

            Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(500);
                    _gameCommunicationManager?.SendItem(new GameConnectionManager.Packet
                    {
                        PacketType = "Test",
                        Payload = DateTime.Now.ToLongTimeString(),
                    });
                }
            });
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

        private void HandleOnPacketReceived(GameConnectionManager.PacketReceivedArgs packetReceivedArgs)
        {
            ReactToPluginEvent($"GameCommunicationPlugin_PacketReceived_{packetReceivedArgs.Packet.PacketType}", packetReceivedArgs.Packet.Payload);
            Debug.WriteLine($"Packet Type: {packetReceivedArgs.Packet.PacketType}, Payload: {packetReceivedArgs.Packet.Payload}");
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
