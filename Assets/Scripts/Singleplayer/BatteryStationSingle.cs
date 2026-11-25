using UnityEngine;

// Estacion de baterias del single player (Arena).
// Antes sumaba una bateria invisible e infinita cada vez que el jugador tocaba el trigger.
// Ahora muestra pilas sobre la estacion, como el generador del multijugador:
//   - Una pila al empezar y otra cada 'intervalo' segundos, hasta 'maxPilas' a la vez.
//   - Se recoge con el grip izquierdo con la mano izquierda a 'distanciaRecoger' o menos (igual que en multijugador).
//   - Cada pila recogida suma 1 bateria a GunSingle (se recarga con B, como siempre).
// 'prefabPila' lo asigna el menu DarkShot/11 (Assets/DarkShot/Prefabs/Battery.prefab); si falta se usa una capsula verde.
public class BatteryStationSingle : MonoBehaviour
{
    [Header("Pilas sobre la estacion")]
    public GameObject prefabPila;
    public int maxPilas = 3;
    public float intervalo = 20f;
    public bool pilaAlEmpezar = true;
    public float separacion = 0.2f;        // entre pilas, a lo ancho de la estacion
    public float alturaSobreEstacion = 0.12f;

    [Header("Recoger")]
    public float distanciaRecoger = 0.25f; // mano izquierda
    public AudioSource sonidoRecoger;      // opcional

    GameObject[] pilas;
    Vector3[] ranuras;
    float proximo;
    Transform manoIzquierda;
    bool gripAntes;

    void Start()
    {
        pilas = new GameObject[Mathf.Max(1, maxPilas)];
        CalcularRanuras();
        if (pilaAlEmpezar) Generar();
        proximo = Time.time + intervalo;
    }

    void Update()
    {
        if (Time.time >= proximo)
        {
            proximo = Time.time + intervalo;
            Generar();
        }

        // Giro lento para que se note que se pueden tomar
        for (int i = 0; i < pilas.Length; i++)
        {
            if (pilas[i] != null) pilas[i].transform.Rotate(0f, 60f * Time.deltaTime, 0f, Space.World);
        }

        if (manoIzquierda == null)
        {
            OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
            if (rig != null) manoIzquierda = rig.leftHandAnchor;
            if (manoIzquierda == null) return;
        }

        // Al apretar el grip (no mientras se mantiene) se toma la pila mas cercana a la mano
        bool grip = OVRInput.Get(OVRInput.RawButton.LHandTrigger);
        bool apreto = grip && !gripAntes;
        gripAntes = grip;
        if (!apreto) return;

        int mejor = -1;
        float mejorDist = distanciaRecoger;
        for (int i = 0; i < pilas.Length; i++)
        {
            if (pilas[i] == null) continue;
            float d = Vector3.Distance(manoIzquierda.position, pilas[i].transform.position);
            if (d <= mejorDist) { mejorDist = d; mejor = i; }
        }
        if (mejor >= 0) Recoger(mejor);
    }

    void Generar()
    {
        for (int i = 0; i < pilas.Length; i++)
        {
            if (pilas[i] != null) continue;
            pilas[i] = CrearPila(ranuras[i]);
            return;
        }
    }

    GameObject CrearPila(Vector3 posicion)
    {
        GameObject p;
        if (prefabPila != null)
        {
            p = Instantiate(prefabPila, posicion, prefabPila.transform.rotation);
        }
        else
        {
            p = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            p.transform.position = posicion;
            p.transform.localScale = new Vector3(0.07f, 0.06f, 0.07f);
            Renderer r = p.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0f, 0.97f, 0.22f); // verde bateria
        }
        p.name = "PilaEstacion";
        // Solo visual: sin colliders para no estorbar al jugador ni al laser
        foreach (Collider c in p.GetComponentsInChildren<Collider>(true)) c.enabled = false;

        // Se apoya sobre la estacion: la base del modelo en la altura de la ranura
        Bounds b;
        if (Caja(p, out b)) p.transform.position += new Vector3(0f, posicion.y - b.min.y, 0f);
        return p;
    }

    void Recoger(int indice)
    {
        GameObject p = pilas[indice];
        pilas[indice] = null;
        if (p != null) Destroy(p);

        GunSingle gun = FindFirstObjectByType<GunSingle>();
        if (gun != null)
        {
            gun.batteries += 1;
            if (gun.ammoText != null) gun.ammoText.text = "Ammo: " + gun.ammoCapacity + "/" + gun.batteries;
        }
        if (sonidoRecoger != null) sonidoRecoger.Play();
        OVRInput.SetControllerVibration(0.5f, 0.4f, OVRInput.Controller.LTouch);
        Invoke(nameof(PararVibracion), 0.12f);

        // La siguiente pila tarda un intervalo completo desde que se recoge una
        if (Time.time > proximo - intervalo * 0.5f) proximo = Time.time + intervalo;
    }

    void PararVibracion()
    {
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
    }

    // Ranuras en fila sobre la parte de arriba de la estacion (modelo incluido)
    void CalcularRanuras()
    {
        ranuras = new Vector3[pilas.Length];
        Bounds b;
        Vector3 centro = transform.position;
        float arriba = transform.position.y + alturaSobreEstacion;
        if (Caja(gameObject, out b))
        {
            centro = b.center;
            arriba = b.max.y + 0.01f;
        }
        for (int i = 0; i < ranuras.Length; i++)
        {
            float x = (i - (ranuras.Length - 1) * 0.5f) * separacion;
            ranuras[i] = new Vector3(centro.x, arriba, centro.z) + transform.right * x;
        }
    }

    static bool Caja(GameObject g, out Bounds b)
    {
        b = new Bounds();
        bool hay = false;
        foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled) continue;
            if (!hay) { b = r.bounds; hay = true; }
            else b.Encapsulate(r.bounds);
        }
        return hay;
    }

    // Se conserva por compatibilidad (por si algun evento la llama): suma una bateria directamente
    public void RechargeGun()
    {
        GunSingle gun = FindFirstObjectByType<GunSingle>();
        if (gun != null) gun.batteries += 1;
    }
}
