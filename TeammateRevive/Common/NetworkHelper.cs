using UnityEngine.Networking;

namespace TeammateRevive.Common
{
    public static class NetworkHelper
    {
        public static bool IsServer => !IsClient();
        
        public static bool IsClient()
        {
            if (RoR2.RoR2Application.isInSinglePlayer || !NetworkServer.active)
            {
                return true;
            }

            return false;
        }
    }
}