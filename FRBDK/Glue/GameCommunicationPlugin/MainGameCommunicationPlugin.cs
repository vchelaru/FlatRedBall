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

        #endregion

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
            _gameCommunicationManager = new GameConnectionManager(ReactToPluginEvent);
            _gameCommunicationManager.Port = 8888;
            _gameCommunicationManager.OnPacketReceived += HandleOnPacketReceived;
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

                case "GameCommunication_Send_OldDTO":
                    _gameCommunicationManager.SendItem(new GameConnectionManager.Packet
                    {
                        PacketType = "OldDTO",
                        Payload = payload
                    });

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
                case "GameCommunication_Send_OldDTO":
                    var response = await _gameCommunicationManager.SendItemWithResponse(new GameConnectionManager.Packet
                    {
                        PacketType = "OldDTO",
                        Payload = payload
                    });

                    var returnPacket = response.Data;

                    if(returnPacket?.PacketType == "OldDTO" && returnPacket?.Payload != "{\"Commands\":[]}")
                        Debug.WriteLine($"{returnPacket.PacketType}, {returnPacket.Payload}");

                    return JsonConvert.SerializeObject(new GeneralResponse<string>
                    {
                        Succeeded = returnPacket != null,
                        Data = returnPacket?.Payload,
                        Message = response.Message
                    });

                case "GameCommunication_SetPrimarySettings":
                    var sPayload = JObject.Parse(payload);

                    _gameCommunicationManager.Port = sPayload.ContainsKey("PortNumber") ? sPayload.Value<int>("PortNumber") : 8888;
                    _gameCommunicationManager.DoConnections = sPayload.ContainsKey("IsGlueControlManagerGenerationEnabled") ? sPayload.Value<bool>("IsGlueControlManagerGenerationEnabled") : false;
                    game1GlueCommunicationGenerator.PortNumber = _gameCommunicationManager.Port;
                    game1GlueCommunicationGenerator.IsGameCommunicationEnabled = _gameCommunicationManager.DoConnections;

                    return "";

            }

            return null;
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
