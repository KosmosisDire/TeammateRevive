using TMPro;
using UnityEngine;

namespace TeammateRevive.RevivalStrategies.ReduceMaxHp.ProgressBar
{
    public class ProgressBarScript : MonoBehaviour
    {
        private RectTransform rectTransform;
        private RectTransform barTransform;

        public float Fraction;

        public float mod = 0.007f;
        public bool ShouldMock = false;
        private TextMeshProUGUI text;

        public bool IsShown
        {
            get => this.gameObject.activeSelf;
            set => this.gameObject.SetActive(value);
        }

        public void SetText(string textValue) => this.text.SetText(textValue);

        void Awake()
        {
            this.rectTransform = GetComponent<RectTransform>();
            this.barTransform = (RectTransform)this.rectTransform.Find("Bar").transform;
            this.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Screen.width / 3);
            this.text = this.transform.Find("Text").GetComponent<TextMeshProUGUI>();
        }

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            if (ShouldMock) {
                this.Fraction += this.mod;
            
                if (this.Fraction < 0)
                {
                    this.Fraction = 0;
                    this.mod = -this.mod;
                }
                else if (this.Fraction > 1)
                {
                    this.Fraction = 1;
                    this.mod = -this.mod;
                }
            }
        
            var delta = this.barTransform.sizeDelta;
            this.barTransform.sizeDelta = new Vector2(this.rectTransform.rect.width * Fraction, delta.y);
        }
    }

}