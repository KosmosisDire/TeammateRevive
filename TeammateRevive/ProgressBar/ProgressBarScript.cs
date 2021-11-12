using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TeammateRevive.ProgressBar
{
    public class ProgressBarScript : MonoBehaviour
    {
        private RectTransform rectTransform;
        private RectTransform barTransform;
        public Image barImage;

        public float Fraction;
        private TextMeshProUGUI text;

        private bool isDestroyed;

        public bool IsShown
        {
            get => !this.isDestroyed && this.gameObject.activeSelf;
            set
            {
                if (!this.isDestroyed)
                {
                    this.gameObject.SetActive(value);
                }
            }
        }

        public void SetText(string textValue) => this.text.SetText(textValue);

        void Awake()
        {
            this.rectTransform = GetComponent<RectTransform>();
            this.barTransform = (RectTransform)this.rectTransform.Find("Bar").transform;
            this.barImage = this.barTransform.GetComponent<Image>();
            
            // it works, don't ask me how - layout is working weirdly in RoR2...
            rectTransform.anchorMin = new Vector2(0.55f, .58f);
            rectTransform.anchorMax = new Vector2(0.53f, .6f);
            this.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Screen.width / 3f);
            this.text = this.transform.Find("Text").GetComponent<TextMeshProUGUI>();
        }

        private void OnDestroy()
        {
            this.isDestroyed = true;
        }

        // Update is called once per frame
        void Update()
        {
            var delta = this.barTransform.sizeDelta;
            this.barTransform.sizeDelta = new Vector2(this.rectTransform.rect.width * this.Fraction, delta.y);
        }
    }

}