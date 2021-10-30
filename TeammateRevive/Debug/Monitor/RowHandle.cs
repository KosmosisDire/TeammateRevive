using TMPro;

namespace TeammateRevive.Debug.Monitor
{
    public class RowHandle
    {
        public readonly TextMeshProUGUI text;
        private readonly DebugMonitorPanelScript script;

        public RowHandle(TextMeshProUGUI text, DebugMonitorPanelScript script)
        {
            this.text = text;
            this.script = script;
        }
        
        public void SetText(string text)
        {
            this.text.SetText(text);
        }

        public void Remove()
        {
            this.script.RemoveByHandle(this);
        }
    }
}