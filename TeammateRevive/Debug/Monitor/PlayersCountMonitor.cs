using TeammateRevive.Common;
using TeammateRevive.Players;

namespace TeammateRevive.Debug.Monitor
{
    public class PlayersCountMonitor : BaseMonitor<string>
    {
        private RowHandle row;

        protected override void InitData()
        {
            this.row = this.Script.CreateRow("");
        }

        protected override void UpdateData()
        {
            if (NetworkHelper.IsClient()) return;
            var t = PlayersTracker.instance;
            this.row.SetText($"<indent=5%>Alive: {t.Alive.Count}, Dead: {t.Dead.Count}, All: {t.Alive.Count}");
        }
    }
}