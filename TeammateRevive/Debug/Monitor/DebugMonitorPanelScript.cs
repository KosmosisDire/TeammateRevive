using System.Collections;
using System.Collections.Generic;
using TeammateRevive.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TeammateRevive.Debug.Monitor
{
    public class DebugMonitorPanelScript : MonoBehaviour
    {
        private List<TextMeshProUGUI> rows = new();
        private VerticalLayoutGroup @group;

        void Awake()
        {
        
            this.group = this.gameObject.AddComponent<VerticalLayoutGroup>();
            this.@group.childForceExpandHeight = false;
            var image = this.gameObject.AddComponent<Image>();
            image.color = new Color(.5f, .5f, .5f, 0.05f);
            image.raycastTarget = false;
        }

        public RowHandle CreateRow(string text)
        {
            var idx = AddRow(text);
            return new RowHandle(this.rows[idx], this);
        }

        public void RemoveByHandle(RowHandle handle)
        {
            this.rows.Remove(handle.text);
            Destroy(handle.text.gameObject);
        }

        public string GetTextAt(int idx)
        {
            if (this.rows.Count > idx)
            {
                return this.rows[idx].text;
            }

            return null;
        }

        public void SetTextAt(int idx, string text)
        {
            if (this.rows.Count <= idx)
            {
                return;
            }

            this.rows[idx].SetText(text);
        }

        public void RemoveRowAt(int idx)
        {
            if (this.rows.Count <= idx)
            {
                return;
            }
        
            Destroy(this.rows[idx].gameObject);
            this.rows.RemoveAt(idx);
        }

        public int AddRow(string text)
        {
            var row = AddTextRow(text);
            this.rows.Add(row.GetComponent<TextMeshProUGUI>());
            return this.rows.Count - 1;
        }

        public int RowsCount => this.rows.Count;

        public void Clear()
        {
            var i = 0;
            while (this.RowsCount > 0)
            {
                RemoveRowAt(i++);
            }
        }

        private GameObject AddTextRow(string text)
        {
            var obj = new GameObject($"Text Row: {text}");
            obj.AddComponent<RectTransform>();
            var component = obj.AddComponent<TextMeshProUGUI>();
            component.text = text;
        
            obj.transform.SetParent(this.transform);

            return obj;
        }
    }
}