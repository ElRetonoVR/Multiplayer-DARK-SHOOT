using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TimerScript : MonoBehaviour
{
    public float timer = 180f; // 3 minutos, segun el GDD
    public Text Contador;
    [Tooltip("Segundos que se muestra la pantalla de puntuacion antes de volver al menu")]
    public float segundosPantallaFinal = 12f;
    [Tooltip("Distancia (m) frente al jugador a la que aparece el panel de resultados")]
    public float distanciaPanel = 1.6f;
    private bool terminado = false;
    private float tiempoPantallaFinal = 0f;
    private bool botonSoltado = false;

    public void Update()
    {
        if (terminado)
        {
            ActualizarPantallaFinal();
            return;
        }

        timer -= Time.deltaTime;
        Contador.text = "Time: " + Mathf.Max(timer, 0f).ToString("F0");

        if (timer <= 0)
        {
            terminado = true; // Muestra los resultados una sola vez
            MostrarPantallaFinal();
        }
    }

    // Congela la partida y muestra la puntuacion en un panel frente al jugador
    void MostrarPantallaFinal()
    {
        ScoreSingle scoreSingle = FindAnyObjectByType<ScoreSingle>();
        int puntos = scoreSingle != null ? scoreSingle.GetScore() : 0;

        Time.timeScale = 0f; // Detiene enemigos y apariciones

        Camera cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();

        GameObject canvasGO = new GameObject("PantallaFinal");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform rt = canvas.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(800f, 500f);
        rt.localScale = Vector3.one * 0.002f;

        if (cam != null)
        {
            // Frente a la vista del jugador, sin inclinarse con la cabeza
            Vector3 adelante = cam.transform.forward;
            adelante.y = 0f;
            if (adelante.sqrMagnitude < 0.001f) adelante = Vector3.forward;
            adelante.Normalize();
            rt.position = cam.transform.position + adelante * distanciaPanel;
            rt.rotation = Quaternion.LookRotation(adelante, Vector3.up);
        }

        // Fondo oscuro
        GameObject fondo = new GameObject("Fondo");
        fondo.transform.SetParent(canvasGO.transform, false);
        Image img = fondo.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.85f);
        RectTransform fr = img.rectTransform;
        fr.anchorMin = Vector2.zero;
        fr.anchorMax = Vector2.one;
        fr.offsetMin = Vector2.zero;
        fr.offsetMax = Vector2.zero;

        Font fuente = Contador != null ? Contador.font : null;
        if (fuente == null) fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        CrearTexto(canvasGO.transform, fuente, "Titulo", "TIEMPO TERMINADO", 56, new Vector2(0f, 150f), new Color(1f, 0.25f, 0.25f));
        CrearTexto(canvasGO.transform, fuente, "Puntos", "Puntuacion: " + puntos, 80, new Vector2(0f, 20f), Color.white);
        CrearTexto(canvasGO.transform, fuente, "Ayuda", "Pulsa un boton del control para volver al menu", 30, new Vector2(0f, -150f), new Color(0.8f, 0.8f, 0.8f));
    }

    static void CrearTexto(Transform padre, Font fuente, string nombre, string contenido, int tamano, Vector2 posicion, Color color)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        Text t = go.AddComponent<Text>();
        t.font = fuente;
        t.text = contenido;
        t.fontSize = tamano;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform r = t.rectTransform;
        r.sizeDelta = new Vector2(760f, 120f);
        r.anchoredPosition = posicion;
    }

    // Espera unos segundos (o un boton del control) y vuelve al menu
    void ActualizarPantallaFinal()
    {
        tiempoPantallaFinal += Time.unscaledDeltaTime;

        bool pulsado = BotonDeControlPulsado();
        if (!pulsado) botonSoltado = true; // evita que un disparo en curso salte la pantalla

        if (tiempoPantallaFinal >= segundosPantallaFinal || (tiempoPantallaFinal > 1f && botonSoltado && pulsado))
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
            enabled = false; // no volver a cargar la escena
        }
    }

    static readonly List<UnityEngine.XR.InputDevice> dispositivos = new List<UnityEngine.XR.InputDevice>();

    static bool BotonDeControlPulsado()
    {
        dispositivos.Clear();
        UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(UnityEngine.XR.InputDeviceCharacteristics.Controller, dispositivos);
        foreach (UnityEngine.XR.InputDevice d in dispositivos)
        {
            bool b;
            if (d.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out b) && b) return true;
            if (d.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out b) && b) return true;
            if (d.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out b) && b) return true;
        }
        return false;
    }
}
