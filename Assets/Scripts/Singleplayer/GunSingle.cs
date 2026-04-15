using UnityEngine;
using UnityEngine.UI;

public class GunSingle : MonoBehaviour
{
    public OVRInput.RawButton shootButton;

    [Header("Light Settings")]
    public Light gunLight;

    [Header("Gun Settings")]
    public float range = 100f; // Alcance del disparo
    public int damage = 1; // Daño que inflige el disparo
    public int ammoCapacity = 20; // Capacidad de munición
    public int batteries = 1; // Cantidad de baterías para recargar
    public AudioSource gunSound; // Sonido del disparo

    [Header("References")]
    public Transform firePoint;
    public LineRenderer linePrefab;

    [Header("Visuals")]
    public float lineDuration = 0.05f;
    public Text ammoText; // Texto para mostrar la munición restante

    private void Start()
    {
        gunLight = GetComponent<Light>();

    }

    void Update()
    {
        if (OVRInput.GetDown(shootButton))
        {
            if (ammoCapacity > 0)
            {
                Shoot();
            }
        }
        if (OVRInput.GetDown(OVRInput.RawButton.B))
        {
            if (batteries > 0)
            {
                reloadGun();
            }
        }
    }

    void Shoot()
    {
        gunSound.pitch = Random.Range(0.7f, 1.5f);
        gunSound.PlayOneShot(gunSound.clip, 0.7f);
        Ray ray = new Ray(firePoint.position, firePoint.forward);
        RaycastHit hit;

        Vector3 endPoint = firePoint.position + firePoint.forward * range;

        if (Physics.Raycast(ray, out hit, range))
        {
            endPoint = hit.point;

            // Damage (optional)
            Health target = hit.collider.GetComponent<Health>();
            if (target != null)
            {
                target.TakeDamage(damage);
            }
        }

        // ?? Instantiate LineRenderer
        LineRenderer line = Instantiate(linePrefab);
        line.SetPosition(0, firePoint.position);
        line.SetPosition(1, endPoint);

        Destroy(line.gameObject, lineDuration);
        ammoCapacity--;
        gunLight.intensity-=1.9f;
        gunLight.range-=0.5f;
        ammoText.text = "Ammo: " + ammoCapacity + "/" + batteries;

    }
    void reloadGun()
    {
        ammoCapacity = 20;
        batteries --;
        gunLight.intensity = 40;
        gunLight.range = 15;
        ammoText.text = "Ammo: " + ammoCapacity + "/" + batteries;
    }
}
