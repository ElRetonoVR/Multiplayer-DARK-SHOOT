using UnityEngine;

public class BatteryStationSingle : MonoBehaviour
{


    // Update is called once per frame
    void Update()
    {
        
    }
    public void RechargeGun()
    {
        GunSingle gun = FindFirstObjectByType<GunSingle>();
        if (gun != null)
        {
            gun.batteries += 1; // Aumenta la cantidad de baterías disponibles
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            RechargeGun();
        }
    }
}
