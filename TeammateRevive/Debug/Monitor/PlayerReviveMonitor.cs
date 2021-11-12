using System.Linq;
using TeammateRevive.Common;
using TeammateRevive.Players;
using UnityEngine;

namespace TeammateRevive.Debug.Monitor
{
    public class PlayerReviveMonitor : BaseMonitor<Player>
    {
        protected override void InitData()
        {
            if (PlayersTracker.instance.Setup)
            {
                InitAllPlayers();
            }
            else
            {
                PlayersTracker.instance.OnSetupFinished += InitAllPlayers;
            }
        }

        void InitAllPlayers()
        {
            ClearAll();
            UpdateData();
        }

        protected override void UpdateData()
        {
            if (NetworkHelper.IsClient()) return;
            
            foreach (var player in PlayersTracker.instance.All)
            {
                if (!this.Dictionary.TryGetValue(player, out var handle))
                {
                    handle = this.Script.CreateRow("");
                    this.Dictionary[player] = handle;
                }
                this.Dictionary[player].SetText(FormatPlayer(player));
            }
        }

        string FormatPlayer(Player player)
        {
            if (player.reviveLinks.Count == 0)
            {
                return "";
            }
            var links = string.Join(" ",
                player.reviveLinks.Select(kv => $"{kv.Key.networkUser.userName}({(kv.Value - Time.time):F0}s)")
            );
            return $"<indent=5%>{player.networkUser.userName}:: {links}";
        }
    }
}