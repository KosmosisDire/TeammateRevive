using R2API.Networking;
using R2API.Networking.Interfaces;
using UnityEngine.Networking;

namespace TeammateRevival.RevivalStrategies
{
    public class DebugNetworkMessage : INetMessage
    {
        private string messageType;
        private MainTeammateRevival Plugin => MainTeammateRevival.instance;

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
            switch (this.messageType)
            {
                case "SpawnSkull":
                    SpawnSkull();
                    break;
            }
        }

        public void SpawnSkull()
        {
            if (!Plugin.DeadPlayers.Contains(Plugin.AlivePlayers[0]))
            {
                Plugin.DeadPlayers.Add(Plugin.AlivePlayers[0]);
            }

            Plugin.RevivalStrategy.ServerSpawnSkull(Plugin.AllPlayers[0]);
        }
    }
}