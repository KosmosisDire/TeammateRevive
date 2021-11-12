using System.Linq;
using RoR2;
using TeammateRevive.Players;
using TeammateRevive.Skull;
using UnityEngine.Networking;

namespace TeammateRevive.Debug.Monitor
{
    public class InsideSkullMonitor : BaseMonitor<string>
    {
        protected override void InitData()
        {
            DeadPlayerSkull.GlobalOnDestroy += OnSkullDestroy;
            foreach (var player in NetworkUser.readOnlyInstancesList)
            {
                UpdateOrCreateRow(player.userName);
            }
        }

        private void OnSkullDestroy(DeadPlayerSkull obj)
        {
            UpdateOrCreateRow(obj.PlayerName, "");
        }

        protected override void UpdateData()
        {
            for (var index = 0; index < SkullTracker.instance.skulls.ToArray().Length; index++)
            {
                var skull = SkullTracker.instance.skulls.ToArray()[index];
                UpdateOrCreateRow(skull.PlayerName, Format(skull));
            }
        }

        string Format(DeadPlayerSkull skull)
        {
            var names = NetworkUser.readOnlyInstancesList
                .Where(p => skull.insidePlayerIDs.Contains(p.GetCurrentBody()?.netId ?? NetworkInstanceId.Invalid))
                .Select(p => p.userName)
                .ToArray();
            var ns = names.Any() ? string.Join(", ", names) : "<no one>";
            return $"<indent=5%><color=\"red\"><size=70%>Skull<color=\"white\"> <size=100%>{skull.PlayerName}: ({skull.progress:P1}/{skull.fractionPerSecond:F2}) {skull.insidePlayerIDs.Count} inside: {ns}";
        }
    }
}