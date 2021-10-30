using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TeammateRevive.Debug.Monitor
{
    public abstract class BaseMonitor<TKey> : MonoBehaviour
    {
        protected DebugMonitorPanelScript Script;
        protected readonly Dictionary<TKey, RowHandle> Dictionary = new();

        void Awake()
        {
            this.Script = this.gameObject.GetComponent<DebugMonitorPanelScript>();
            InitData();
        }

        public RowHandle UpdateOrCreateRow(TKey key, string text = "")
        {
            if (!this.Dictionary.TryGetValue(key, out var handle))
            {
                handle = this.Script.CreateRow(text);
                this.Dictionary[key] = handle;
            }

            if (!string.IsNullOrEmpty(text))
            {
                handle.SetText(text);
            }
            return handle;
        }

        protected void RemoveRow(TKey key)
        {
            if (this.Dictionary.TryGetValue(key, out var handle))
            {
                handle.Remove();
                this.Dictionary.Remove(key);
            }
        }
        
        public void ClearAll()
        {
            foreach (var key in this.Dictionary.Keys.ToArray())
            {
                RemoveRow(key);
            }
        }
        
        void OnDestroy()
        {
            ClearAll();
        }

        protected virtual void InitData()
        {
            
        }

        protected abstract void UpdateData();
        
        public void Update()
        {
            if (this.Script.gameObject.activeSelf && RunTracker.instance.IsStarted)
            {
                this.UpdateData();
            }
        }
        
    }
}