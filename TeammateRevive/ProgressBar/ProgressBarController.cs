using System.Reflection;
using R2API;
using TeammateRevive.Logging;
using TMPro;
using UnityEngine;

namespace TeammateRevive.ProgressBar
{
    public class ProgressBarController
    {
        private GameObject progressBarPrefab;
        private ProgressBarScript progressBarScript;
        private TextMeshProUGUI textComponent;

        public bool IsShown => this.progressBarScript is { IsShown: true };

        public ProgressBarController()
        {
            Log.Debug("Init ResurrectController");
            InitProgressBar();
            On.RoR2.UI.HUD.Awake += HUDOnAwake;
        }

        private void InitProgressBar()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TeammateRevive.Resources.reducehp");
            var bundle = AssetBundle.LoadFromStream(stream);

            this.progressBarPrefab = bundle.LoadAsset<GameObject>("Assets/ProgressBar/ProgressBar.prefab");

            bundle.Unload(false);
        }

        public void SetFraction(float fraction)
        {
            if (this.progressBarScript == null) return;
            if (fraction == 0)
            {
                this.progressBarScript.IsShown = false;
                return;
            }

            if (!this.progressBarScript.IsShown)
            {
                this.progressBarScript.IsShown = true;
            }

            this.progressBarScript.Fraction = fraction > 1 ? 1 : fraction;
        }

        public void SetColor(Color color)
        {
            this.progressBarScript.barImage.color = color;
        }

        public void Hide()
        {
            if (this.progressBarScript == null) return;

            this.progressBarScript.Fraction = 0;
            if (this.progressBarScript.IsShown) this.progressBarScript.IsShown = false;
            this.currentName = null;
        }

        private void AttachProgressBar(RoR2.UI.HUD hud)
        {
            Log.Debug("AttachProgressBar");
            var progressBar = this.progressBarPrefab.InstantiateClone("Progress Bar");
            this.progressBarScript = progressBar.AddComponent<ProgressBarScript>();
            progressBar.transform.SetParent(hud.mainContainer.transform);
            this.progressBarScript.IsShown = false;
            
            var transform = progressBar.GetComponent<RectTransform>();
            transform.anchorMax = new Vector2(.9f, .68f);
            transform.anchorMin = new Vector2(.6f, .66f);

            var textObj = progressBar.transform.Find("Text");
            this.textComponent = textObj.GetComponent<TextMeshProUGUI>();
            this.textComponent.font = RoR2.UI.HGTextMeshProUGUI.defaultLanguageFont;
        }

        private void HUDOnAwake(On.RoR2.UI.HUD.orig_Awake orig, RoR2.UI.HUD self)
        {
            Log.Debug("HUDOnAwake");
            orig(self);

            AttachProgressBar(self);
        }

        private string currentName = "Player";

        public void SetUser(string name)
        {
            name ??= "Player";
            if (this.currentName == name) return;
            this.textComponent.text = $"Reviving {name}...";
            this.currentName = name;
        }
    }
}