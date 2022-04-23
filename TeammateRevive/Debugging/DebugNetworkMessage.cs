using System;
using R2API.Networking;
using R2API.Networking.Interfaces;
using R2API.Utils;
using UnityEngine.Networking;

namespace TeammateRevive.Debugging
{
    public class DebugNetworkMessage : INetMessage
    {
        private string messageType;

        public DebugNetworkMessage()
        {
        }

        public static void SendToServer(string type)
        {
            new DebugNetworkMessage(type).Send(NetworkDestination.Server);
        }

        public DebugNetworkMessage(string messageType)
        {
            this.messageType = messageType;
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(this.messageType);
        }

        public void Deserialize(NetworkReader reader)
        {
            this.messageType = reader.ReadString();
        }

        public void OnReceived()
        {
            typeof(DebugHelper).GetMethod(this.messageType)?.Invoke(null, Array.Empty<object>());
        }

        public void SpawnTotem()
        {
            DebugHelper.SpawnTotemForFirstPlayer();
        }
    }
}