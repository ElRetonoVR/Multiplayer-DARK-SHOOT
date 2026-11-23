using System.Collections;
using UnityEngine;

// Ataque principal del Bloom (GDD): cuando el enemigo alcanza al jugador
// la pantalla se oscurece, se corta el sonido ambiente y su cara aparece muy cerca.
//
// Configuracion en la escena:
// 1. Agrega este componente a cualquier objeto (por ejemplo, al jugador).
// 2. En "caraBloom" arrastra un objeto DESACTIVADO con la cara del Bloom
//    (un modelo o un Quad con la textura de la cara). Usa un material Unlit o
//    con emision, porque la escena se oscurece durante el susto.
// 3. En "sonidoSusto" arrastra un AudioSource con el sonido humedo / respiracion.
// 4. "cabezaJugador" puede quedar vacio: se usa la camara principal (CenterEyeAnchor).
public class JumpscareBloom : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject caraBloom;          // Cara del Bloom, empieza desactivada
    public AudioSource sonidoSusto;       // Sonido humedo / respiracion
    public Transform cabezaJugador;       // Si queda vacio se usa Camera.main

    [Header("Ajustes")]
    public float duracion = 1.2f;         // Segundos que dura el susto
    public float distanciaCara = 0.35f;   // Que tan cerca de los ojos aparece la cara (metros)
    public float tiempoEntreSustos = 4f;  // Evita que se repita con cada golpe seguido

    private bool activo = false;
    private float ultimoSusto = -999f;

    void Start()
    {
        if (cabezaJugador == null && Camera.main != null)
        {
            cabezaJugador = Camera.main.transform;
        }

        if (caraBloom != null)
        {
            caraBloom.SetActive(false);
        }

        if (sonidoSusto != null)
        {
            sonidoSusto.ignoreListenerPause = true; // Debe sonar aunque el resto del audio este pausado
        }
    }

    public void Activar()
    {
        if (activo || Time.time - ultimoSusto < tiempoEntreSustos) return;
        StartCoroutine(Susto());
    }

    IEnumerator Susto()
    {
        activo = true;
        ultimoSusto = Time.time;

        // 1. Oscurecer: apaga las luces de la escena (menos las que tenga la cara) y la luz ambiente
        Light[] luces = FindObjectsByType<Light>(FindObjectsSortMode.None);
        bool[] estabanEncendidas = new bool[luces.Length];
        for (int i = 0; i < luces.Length; i++)
        {
            estabanEncendidas[i] = luces[i].enabled;
            bool esDeLaCara = caraBloom != null && luces[i].transform.IsChildOf(caraBloom.transform);
            if (!esDeLaCara) luces[i].enabled = false;
        }
        Color ambienteAnterior = RenderSettings.ambientLight;
        float intensidadAmbienteAnterior = RenderSettings.ambientIntensity;
        RenderSettings.ambientLight = Color.black;
        RenderSettings.ambientIntensity = 0f;

        // 2. Cortar el sonido ambiente
        AudioListener.pause = true;

        // 3. Cara del Bloom muy cerca, mirando al jugador
        if (caraBloom != null && cabezaJugador != null)
        {
            caraBloom.transform.position = cabezaJugador.position + cabezaJugador.forward * distanciaCara;
            caraBloom.transform.rotation = Quaternion.LookRotation(cabezaJugador.position - caraBloom.transform.position);
            caraBloom.SetActive(true);
        }

        if (sonidoSusto != null)
        {
            sonidoSusto.Play();
        }

        // Mantiene la cara frente a los ojos aunque el jugador gire la cabeza
        float t = 0f;
        while (t < duracion)
        {
            if (caraBloom != null && cabezaJugador != null)
            {
                caraBloom.transform.position = cabezaJugador.position + cabezaJugador.forward * distanciaCara;
                caraBloom.transform.rotation = Quaternion.LookRotation(cabezaJugador.position - caraBloom.transform.position);
            }
            t += Time.deltaTime;
            yield return null;
        }

        // 4. Regresar todo a la normalidad
        if (caraBloom != null)
        {
            caraBloom.SetActive(false);
        }

        AudioListener.pause = false;
        RenderSettings.ambientLight = ambienteAnterior;
        RenderSettings.ambientIntensity = intensidadAmbienteAnterior;
        for (int i = 0; i < luces.Length; i++)
        {
            if (luces[i] != null) luces[i].enabled = estabanEncendidas[i];
        }

        activo = false;
    }
}
