using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;
using GameJsonCommunicationPlugin.Common;
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
        public override string FriendlyName => "Game Communication Plugin";

        public override Version Version => new Version(1, 0);

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            GameConnectionManager.Instance.OnPacketReceived -= HandleOnPacketReceived;

            return true;
        }

        public override void StartUp()
        {
            GameConnectionManager.Instance.OnPacketReceived += HandleOnPacketReceived;

            Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(500);
                    GameConnectionManager.Instance.SendItem(new GameConnectionManager.Packet
                    {
                        PacketType = "Test",
                        Payload = DateTime.Now.ToLongTimeString(),
                    });
                }
            });
        }

        private void HandleOnPacketReceived(GameConnectionManager.PacketReceivedArgs packetReceivedArgs)
        {
            ReactToPluginEvent($"GameCommunicationPlugin_PacketReceived_{packetReceivedArgs.Packet.PacketType}", packetReceivedArgs.Packet.Payload);
            Debug.WriteLine($"Packet Type: {packetReceivedArgs.Packet.PacketType}, Payload: {packetReceivedArgs.Packet.Payload}");
        }
    }
}
