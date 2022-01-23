namespace TeammateRevive.Content
{
    public abstract class ContentBase
    {
        public static bool ContentInited = false;
        
        protected ContentBase()
        {
            On.RoR2.ItemCatalog.Init += OnItemsOnInit;
            On.RoR2.BuffCatalog.Init += BuffCatalogOnInit;
        }

        public abstract void Init();

        protected virtual void OnItemsAvailable()
        {
        }

        protected virtual void OnBuffsAvailable()
        {
        }

        private void BuffCatalogOnInit(On.RoR2.BuffCatalog.orig_Init orig)
        {
            orig();
            OnBuffsAvailable();
        }
        
        private void OnItemsOnInit(On.RoR2.ItemCatalog.orig_Init orig)
        {
            orig();
            OnItemsAvailable();
        }
    }
}