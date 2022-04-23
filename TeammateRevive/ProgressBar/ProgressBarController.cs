using System.Reflection;
using R2API;
using TeammateRevive.Logging;
using TeammateRevive.Resources;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TeammateRevive.ProgressBar
{
    /// <summary>
    /// Controls adding and updating progress bar.
    /// </summary>
    public class ProgressBarController
    {
        private const string DefaultName = "Player";
        private TextMeshProUGUI textComponent;
        private string currentName = DefaultName;
        private readonly CharArrayBuilder charArrayBuilder;
        public bool showing = false;
        Slider progressBar;
        public float progress;

        public ProgressBarController()
        {
            Log.Debug("Init ResurrectController");
            On.RoR2.UI.HUD.Awake += HUDOnAwake;
            // NOTE: this string splitting is required so class can internally keep track of individual parts and update them efficiently
            charArrayBuilder = new CharArrayBuilder("Reviving ", DefaultName, " (", "000.0", "%)...");
        }

        public void UpdateText(string name, float progress = 0)
        {
            name = string.IsNullOrEmpty(name) ? DefaultName : name;

            if (currentName != name)
            {
                charArrayBuilder.UpdatePart(1, name);
                currentName = name;
            }

            charArrayBuilder.SetPaddedPercentagePart(3, progress);
            textComponent.SetCharArray(charArrayBuilder.Buffer, 0, charArrayBuilder.Length);

            Show();
        }

        public void SetFraction(float fraction)
        {
            if(progressBar == null) return;
            progressBar.value = fraction;
            progress = fraction;
            Show();
        }

        public void SetColor(Color color)
        {
            if(progressBar == null) return;
            progressBar.fillRect.GetComponent<Image>().color = color;
            Show();
        }

        public void Hide()
        {
            currentName = DefaultName;
            progressBar.GetComponent<CanvasGroup>().alpha = 0;
            showing = false;
        }

        public void Show()
        {
            showing = true;
            progressBar.GetComponent<CanvasGroup>().alpha = 1;
        }

        private void AttachProgressBar(RoR2.UI.HUD hud)
        {
            Log.Debug("AttachProgressBar");
            
            progressBar = CustomResources.progressBarPrefab.InstantiateClone("Revival Progress Bar").GetComponent<Slider>();
            progressBar.transform.SetParent(hud.mainContainer.transform);

            textComponent = progressBar.GetComponentInChildren<TextMeshProUGUI>();
            textComponent.font = RoR2.UI.HGTextMeshProUGUI.defaultLanguageFont;

            RectTransform rt = progressBar.GetComponent<RectTransform>();
            rt.SetSizeInPixels(Screen.width/3f, Screen.height/30f);
            rt.SetBottomLeftOffset(Screen.width/3f, Screen.height/25f);
            Hide();
        }

        private void HUDOnAwake(On.RoR2.UI.HUD.orig_Awake orig, RoR2.UI.HUD self)
        {
            Log.Debug("HUDOnAwake");
            orig(self);

            AttachProgressBar(self);
        }
    }
}