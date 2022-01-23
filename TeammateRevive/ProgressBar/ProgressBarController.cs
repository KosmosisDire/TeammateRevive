using System.Reflection;
using R2API;
using TeammateRevive.Logging;
using TMPro;
using UnityEngine;

namespace TeammateRevive.ProgressBar
{
    /// <summary>
    /// Controls adding and updating progress bar.
    /// </summary>
    public class ProgressBarController
    {
        private const string DefaultName = "Player";
        
        private GameObject progressBarPrefab;
        private ProgressBarScript progressBarScript;
        private TextMeshProUGUI textComponent;

        private string currentName = DefaultName;
        private readonly CharArrayBuilder charArrayBuilder;

        public bool IsShown => this.progressBarScript is { IsShown: true };

        public ProgressBarController()
        {
            Log.Debug("Init ResurrectController");
            InitProgressBar();
            On.RoR2.UI.HUD.Awake += HUDOnAwake;
            
            // NOTE: this string splitting is required so class can internally keep track of individual parts and update them efficiently
            this.charArrayBuilder = new CharArrayBuilder("Reviving ", DefaultName, " (", "000.0", "%)...");
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
            progressBar.transform.SetParent(hud.mainContainer.transform);
            this.progressBarScript = progressBar.AddComponent<ProgressBarScript>();
            this.progressBarScript.IsShown = false;

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

        public void UpdateText(string name, float progress = 0)
        {
            name = string.IsNullOrEmpty(name) ? DefaultName : name;
            if (this.currentName != name)
            {
                this.charArrayBuilder.UpdatePart(1, name);
                this.currentName = name;
            }
            this.charArrayBuilder.SetPaddedPercentagePart(3, progress);
            this.textComponent.SetCharArray(this.charArrayBuilder.Buffer, 0, this.charArrayBuilder.Length);
        }
    }
}