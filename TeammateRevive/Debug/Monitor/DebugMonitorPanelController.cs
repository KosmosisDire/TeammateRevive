using System;
using System.Collections.Generic;
using On.RoR2.UI;
using TeammateRevive.Common;
using TeammateRevive.Logging;
using TeammateRevive.Players;
using UnityEngine;

namespace TeammateRevive.Debug.Monitor
{
    public class DebugMonitorPanelController
    {
        public DebugMonitorPanelScript script;

        private readonly List<Type> watchers = new();
        private RoR2.UI.HUD hud;
        private int counter = 0;

        public DebugMonitorPanelController()
        {
            On.RoR2.UI.HUD.Awake += HUDOnAwake;
            PlayersTracker.instance.OnSetupFinished += InitIfReady;
        }

        private void InitIfReady()
        {
            // we need both player and hun initialization to be finished
            if (++this.counter == 2 || (this.counter == 1 && NetworkHelper.IsClient()))
            {
                Init();
            }
        }

        void Init()
        {
            var obj = new GameObject("DebugMonitor");
            var rectTransform = obj.AddComponent<RectTransform>();
            this.script = obj.AddComponent<DebugMonitorPanelScript>();
            
            obj.transform.SetParent(this.hud.mainContainer.transform);

            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(.4f, 1f);
            rectTransform.anchoredPosition = Vector2.zero;

            foreach (var watcherType in this.watchers)
            {
                this.script.gameObject.AddComponent(watcherType);
            }
        }

        public void AddWatcher<T>() where T : Component
        {
            this.watchers.Add(typeof(T));
        }

        private void HUDOnAwake(HUD.orig_Awake orig, RoR2.UI.HUD hud)
        {
            Log.DebugMethod("DebugMonitor");
            orig(hud);
            this.hud = hud;
            InitIfReady();
        }
    }
}

