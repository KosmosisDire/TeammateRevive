using RoR2;

namespace TeammateRevive.Players
{
    public static class PlayerExtensions
    {
        public static void GiveItem(this Player player, ItemIndex index, int count = 1)
        {
            player.master.master.inventory.GiveItem(index, count);
        }
        
        public static void RemoveItem(this Player player, ItemIndex index, int count = 1)
        {
            player.master.master.inventory.RemoveItem(index, count);
        }

        public static int ItemCount(this Player player, ItemIndex index)
        {
            return player.master.master.inventory.GetItemCount(index);
        }
    }
}