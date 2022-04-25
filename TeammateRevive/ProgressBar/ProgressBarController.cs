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
        RoR2.UI.HUD hudRef;

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
        
        Rect healthbarRect;
        Rect barRootRect;
        RectTransform rt;

        void UpdatePositionAndSize()
        {
            
            Vector2 parentSize = rt.GetParentSize();

            float width = parentSize.x * 0.8f;
            float height = healthbarRect.height;
            //use law of sines to get the depth of the bar after 6 degrees of rotation
            float depthOffset = (barRootRect.width * 1.2f)/Mathf.Sin(90 * Mathf.Deg2Rad) * Mathf.Sin(6 * Mathf.Deg2Rad);

            rt.SetSizeInPixels(width, height);
            rt.SetBottomLeftOffset(parentSize.x/2 - width/2, 0);
            rt.localScale = Vector3.one;
            rt.localPosition = rt.localPosition.SetZ(depthOffset);
        }

        public void Hide()
        {
            currentName = DefaultName;
            progressBar.GetComponent<CanvasGroup>().alpha = 0;
            showing = false;
        }

        public void Show()
        {
            UpdatePositionAndSize();
            showing = true;
            progressBar.GetComponent<CanvasGroup>().alpha = 1;
        }

        private void AttachProgressBar(RoR2.UI.HUD hud)
        {
            Log.Debug("AttachProgressBar");

            hudRef = hud;
            
            progressBar = CustomResources.progressBarPrefab.InstantiateClone("Revival Progress Bar").GetComponent<Slider>();
            progressBar.transform.SetParent(hud.mainUIPanel.transform.Find("SpringCanvas/BottomCenterCluster"));

            healthbarRect = hudRef.mainUIPanel.transform.Find("SpringCanvas/BottomLeftCluster/BarRoots/HealthbarRoot").GetComponent<RectTransform>().rect;
            barRootRect = hudRef.mainUIPanel.transform.Find("SpringCanvas/BottomLeftCluster/BarRoots").GetComponent<RectTransform>().rect;
            rt = progressBar.GetComponent<RectTransform>();

            textComponent = progressBar.GetComponentInChildren<TextMeshProUGUI>();
            textComponent.font = RoR2.UI.HGTextMeshProUGUI.defaultLanguageFont;
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