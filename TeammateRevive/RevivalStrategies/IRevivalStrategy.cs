namespace TeammateRevival.RevivalStrategies
{
    public interface IRevivalStrategy
    {
        void Init();
        
        DeadPlayerSkull ServerSpawnSkull(Player player);

        void OnClientSkullSpawned(DeadPlayerSkull skull);

        void Update();

        void Revive(Player dead);
    }
}