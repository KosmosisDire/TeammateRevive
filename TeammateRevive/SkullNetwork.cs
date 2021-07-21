using RoR2;
using TeammateRevival;
using UnityEngine;
using UnityEngine.Networking;



public class SkullNetwork : NetworkBehaviour
{
    [SyncVar]
    public static float amount;
    [SyncVar]
    public float r;
    [SyncVar]
    public float g;
    [SyncVar]
    public float b;
    [SyncVar]
    public float intensity;

    public class SyncListUInt64 : SyncListStruct<System.UInt64>
    {
    }

    [SyncVar]
    public SyncListUInt64 insidePlayerIDs = new SyncListUInt64();


    
    public void SetColor(float _r, float _g, float _b, float _intensity)
    {
        r = _r;
        g = _g;
        b = _b;
        intensity = _intensity;
    }

    public Color GetColor()
    {
        return new Color(r, g, b);
        
    }

    public void Start()
    {
        SetColor(1, 0, 0, 1);
    }

    public void SetLighting()
    {
        transform.GetChild(0).GetComponentInChildren<Light>(false).color = GetColor();
        transform.GetChild(0).GetComponentInChildren<Light>(false).intensity = intensity;
    }

    
    public void DamageNumbers()
    {
        if (DamageNumberManager.instance == null) return;

        if (insidePlayerIDs.Count > 0)
        {
            foreach (var playerID in insidePlayerIDs)
            {
                foreach (var localPlayer in NetworkUser.instancesList)
                {
                    if (playerID == localPlayer._id.value)
                    {
                        //show damage numbers
                        if (Random.Range(0f, 100f) < 10f)
                            DamageNumberManager.instance.SpawnDamageNumber(10, transform.position, false, TeamIndex.Player, DamageColorIndex.Bleed);
                    }
                }
            }

            if (Random.Range(0f, 100f) < 10f)
                DamageNumberManager.instance.SpawnDamageNumber(amount * 10 + Random.Range(-1, 2), transform.position, false, TeamIndex.Player, DamageColorIndex.Heal);
        }
    }

    public void Update()
    {
        SetLighting();
        DamageNumbers();

        if (NetworkServer.active)
        {
            SetColor(Random.Range(0f, 1), Random.Range(0f, 1), Random.Range(0f, 1), intensity);
        }
    }
}
