
using RoR2;
using TeammateRevive.Resources;
using UnityEngine;
using UnityEngine.Networking;

namespace TeammateRevive.Revive.Shrine
{
    public class ShrineBehaviour : NetworkBehaviour
    {
        public void Setup()
        {
            this.gameObject.AddComponent<ShrineInteraction>();
            this.gameObject.AddComponent<MeshCollider>().sharedMesh = AddedAssets.ColliderMesh;
        }

        private void Start()
        {
            if (!this.gameObject.GetComponent<EntityLocator>())
            {
                this.gameObject.AddComponent<EntityLocator>().entity = this.gameObject;
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!this.gameObject.GetComponent<EntityLocator>())
            {
                this.gameObject.AddComponent<EntityLocator>().entity = this.gameObject;
            }
        }
    }
}