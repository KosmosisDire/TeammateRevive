using On.RoR2;

namespace TeammateRevive.Content
{
    public abstract class ContentBase
    {
        public abstract void Init();

        public virtual void OnItemsAvailable()
        {
        }

        public virtual void OnBuffsAvailable()
        {
        }
    }
}