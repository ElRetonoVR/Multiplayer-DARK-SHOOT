using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// MENU 9: estilo de los menus con la paleta de la presentacion DARK (DARK.pdf):
//   fondo negro con gris humo, titulos rojo sangre, rojo neon, cian del laser tag y verde de bateria.
// Lobby: fondo oscuro con borde rojo, titulo "DARK SHOT" con brillo rojo, frase del juego, "LASER TAG VR" en cian
//        y todos los botones oscuros con borde de neon (rojo = modos de terror, cian = modos arcade).
// SampleScene: colores de los textos del HUD de la partida.
// No cambia la logica: solo colores, bordes y textos decorativos. Se puede correr varias veces.
public static class DarkShotMenuStyle
{
    // ---- Paleta (muestreada de DARK.pdf)
    static readonly Color Negro       = Hex("#0B0B0B");
    static readonly Color Humo        = Hex("#1C1C1C");
    static readonly Color RojoSangre  = Hex("#9E0000");
    static readonly Color RojoNeon    = Hex("#F80000");
    static readonly Color RojoOscuro  = Hex("#4A0000");
    static readonly Color Cian        = Hex("#10A0B8");
    static readonly Color CianOscuro  = Hex("#063E48");
    static readonly Color VerdeBateria = Hex("#00F838");
    static readonly Color Texto       = Hex("#EDEDED");
    static readonly Color TextoGris   = Hex("#9A9A9A");

    const string RutaLobby = "Assets/Scenes/Lobby.unity";
    const string RutaSampleScene = "Assets/Scenes/SampleScene.unity";
    const string RutaMaterialTitulo = "Assets/DarkShot/Materials/TMP_TituloDark.mat";

    // Botones con acento rojo (terror); el resto usa cian (arcade)
    static readonly HashSet<string> BotonesRojos = new HashSet<string> { "SingleBtn", "CrearCoopBtn" };
    static readonly HashSet<string> BotonesGrises = new HashSet<string> { "VolverBtn" };

    [MenuItem("DarkShot/9. Estilo de menus (paleta DARK)")]
    public static void AplicarEstilo()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("DarkShot", "Sal del modo Play antes de ejecutar este paso.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        List<string> hecho = new List<string>();
        List<string> avisos = new List<string>();
        try
        {
            EstilizarLobby(hecho, avisos);
            EstilizarHud(hecho, avisos);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[DarkShot] Error en el paso 9: " + e);
            EditorUtility.DisplayDialog("DarkShot", "El paso 9 fallo: " + e.Message + "\n\nRevisa la consola. Hay copias de las escenas en la carpeta Backups/.", "OK");
            return;
        }

        string texto = "Hecho:\n- " + string.Join("\n- ", hecho);
        if (avisos.Count > 0) texto += "\n\nAvisos:\n- " + string.Join("\n- ", avisos);
        Debug.Log("[DarkShot] Paso 9\n" + texto);
        EditorUtility.DisplayDialog("DarkShot - Paso 9", texto, "OK");
    }

    // =====================================================================
    //  Lobby
    // =====================================================================
    static void EstilizarLobby(List<string> hecho, List<string> avisos)
    {
        if (!File.Exists(RutaAbsoluta(RutaLobby))) { avisos.Add("No se encontro " + RutaLobby); return; }
        hecho.Add("Copia de seguridad: " + Respaldar(RutaLobby));
        UnityEngine.SceneManagement.Scene escena = EditorSceneManager.OpenScene(RutaLobby, OpenSceneMode.Single);

        Canvas menu = null;
        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            foreach (Canvas c in raiz.GetComponentsInChildren<Canvas>(true))
            {
                if (c.name == "Menu") { menu = c; break; }
            }
            if (menu != null) break;
        }
        if (menu == null) { avisos.Add("No se encontro el Canvas 'Menu' en el Lobby: no se cambio."); return; }
        RectTransform menuRt = (RectTransform)menu.transform;

        TMP_FontAsset fuente = null;
        TMP_Text cualquiera = menu.GetComponentInChildren<TMP_Text>(true);
        if (cualquiera != null) fuente = cualquiera.font;

        // ---- Fondo del menu principal (detras de todo; no bloquea el rayo)
        Image fondo = BuscarOCrear<Image>(menuRt, "FondoMenu");
        RectTransform fondoRt = (RectTransform)fondo.transform;
        Estirar(fondoRt);
        fondoRt.SetAsFirstSibling();
        fondo.sprite = null;
        fondo.color = Negro; // opaco: con transparencia el borde (Outline) se veia por detras y tenia de rojo el fondo
        fondo.raycastTarget = false;
        PonerBorde(fondo.gameObject, RojoSangre, 0.8f);

        // Franja de humo detras del titulo
        Image franja = BuscarOCrear<Image>(menuRt, "FranjaTitulo");
        RectTransform franjaRt = (RectTransform)franja.transform;
        Centrar(franjaRt, new Vector2(0f, 20f), new Vector2(193.4f, 42f));
        franjaRt.SetSiblingIndex(1);
        franja.sprite = null;
        franja.color = Humo;
        franja.raycastTarget = false;

        // ---- Titulo, frase y subtitulo (detras de los botones y del panel de salas)
        TMP_Text titulo = Texto3(menuRt, "TituloDark", fuente, "DARK SHOT", 26f, new Vector2(0f, 26f), new Vector2(190f, 30f), RojoSangre, FontStyles.Bold);
        titulo.characterSpacing = 6f;
        Material brillo = MaterialTitulo(titulo);
        if (brillo != null) titulo.fontSharedMaterial = brillo;
        titulo.transform.SetSiblingIndex(2);

        TMP_Text frase = Texto3(menuRt, "FraseDark", fuente, "\"IT DOESN'T CHASE YOU TO KILL YOU. IT CHASES YOU TO GROW.\"", 4f,
            new Vector2(0f, 7f), new Vector2(190f, 6f), RojoNeon, FontStyles.Italic);
        frase.transform.SetSiblingIndex(3);

        TMP_Text sub = Texto3(menuRt, "SubtituloDark", fuente, "LASER TAG VR", 6f, new Vector2(0f, -12f), new Vector2(120f, 8f), Cian, FontStyles.Bold);
        sub.characterSpacing = 12f;
        sub.transform.SetSiblingIndex(4);
        hecho.Add("Lobby: fondo negro con borde rojo, titulo DARK SHOT con brillo rojo, frase del juego y LASER TAG VR en cian");

        // ---- Botones (menu principal, salas, crear y volver)
        int botones = 0;
        foreach (Button b in menuRt.GetComponentsInChildren<Button>(true))
        {
            Color acento = BotonesRojos.Contains(b.name) ? RojoNeon : BotonesGrises.Contains(b.name) ? TextoGris : Cian;
            Color oscuro = BotonesRojos.Contains(b.name) ? RojoOscuro : BotonesGrises.Contains(b.name) ? Humo : CianOscuro;
            EstilizarBoton(b, acento, oscuro);
            botones++;
        }
        hecho.Add("Lobby: " + botones + " botones oscuros con borde de neon (rojo: Singleplayer y Cooperativo; cian: el resto; Volver gris)");

        // ---- Panel de salas
        Transform panel = menuRt.Find("RoomPanel");
        if (panel != null)
        {
            Transform f = panel.Find("Fondo");
            Image fp = f != null ? f.GetComponent<Image>() : null;
            if (fp != null)
            {
                fp.color = Negro;
                PonerBorde(fp.gameObject, RojoSangre, 0.8f);
            }
            Transform t = panel.Find("Titulo");
            TMP_Text tt = t != null ? t.GetComponent<TMP_Text>() : null;
            if (tt != null)
            {
                tt.text = "SALAS";
                tt.color = RojoSangre;
                tt.fontStyle = FontStyles.Bold;
                tt.characterSpacing = 6f;
                if (brillo != null) tt.fontSharedMaterial = brillo;
            }
            Transform e = panel.Find("Estado");
            TMP_Text te = e != null ? e.GetComponent<TMP_Text>() : null;
            if (te != null) te.color = TextoGris;
            hecho.Add("Panel de salas: fondo negro con borde rojo, titulo SALAS en rojo, estado en gris");
        }
        else avisos.Add("No se encontro RoomPanel (corre el menu 7 antes).");

        EditorSceneManager.MarkSceneDirty(escena);
        if (EditorSceneManager.SaveScene(escena)) hecho.Add("Escena guardada: " + RutaLobby);
        else avisos.Add("No se pudo guardar el Lobby: guardalo con Ctrl+S.");
    }

    static void EstilizarBoton(Button b, Color acento, Color oscuro)
    {
        Image img = b.GetComponent<Image>();
        if (img != null)
        {
            img.color = Color.white; // el tinte lo pone el ColorBlock
            EditorUtility.SetDirty(img);
            PonerBorde(b.gameObject, acento, 0.5f);
        }

        ColorBlock cb = b.colors;
        cb.normalColor = Humo;
        cb.highlightedColor = oscuro;
        cb.pressedColor = acento;
        cb.selectedColor = Humo;
        cb.disabledColor = new Color(Humo.r, Humo.g, Humo.b, 0.4f);
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.08f;
        b.colors = cb;
        b.transition = Selectable.Transition.ColorTint;
        b.targetGraphic = img;
        EditorUtility.SetDirty(b);

        foreach (TMP_Text t in b.GetComponentsInChildren<TMP_Text>(true))
        {
            t.color = Texto;
            t.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            t.characterSpacing = 0f;
            // En mayusculas "SINGLEPLAYER" no cabia y se partia: una linea, y se achica solo si no cabe
            if (!t.enableAutoSizing || t.fontSizeMax < t.fontSize) t.fontSizeMax = t.fontSize;
            t.fontSizeMin = 2f;
            t.enableAutoSizing = true;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.margin = new Vector4(2f, 0f, 2f, 0f);
            EditorUtility.SetDirty(t);
        }
    }

    // =====================================================================
    //  HUD de la partida (SampleScene)
    // =====================================================================
    static void EstilizarHud(List<string> hecho, List<string> avisos)
    {
        if (!File.Exists(RutaAbsoluta(RutaSampleScene))) { avisos.Add("No se encontro " + RutaSampleScene); return; }
        hecho.Add("Copia de seguridad: " + Respaldar(RutaSampleScene));
        UnityEngine.SceneManagement.Scene escena = EditorSceneManager.OpenScene(RutaSampleScene, OpenSceneMode.Single);

        MatchHUD hud = null;
        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            hud = raiz.GetComponentInChildren<MatchHUD>(true);
            if (hud != null) break;
        }
        if (hud == null) { avisos.Add("No se encontro MatchHUD en SampleScene (corre el menu 6 antes)."); return; }

        Colorear(hud.textoEstado, Texto);
        Colorear(hud.textoBateria, VerdeBateria);
        Colorear(hud.textoKills, Cian);
        Colorear(hud.textoInvencible, Cian);
        Colorear(hud.textoMarcador, Texto);
        hecho.Add("HUD: estado y marcador en blanco, bateria en verde, puntos e invencible en cian (bateria baja y 'no listo' en rojo neon)");

        EditorSceneManager.MarkSceneDirty(escena);
        if (EditorSceneManager.SaveScene(escena)) hecho.Add("Escena guardada: " + RutaSampleScene);
        else avisos.Add("No se pudo guardar SampleScene: guardala con Ctrl+S.");

        // Se vuelve a dejar abierto el Lobby (la escena de inicio)
        EditorSceneManager.OpenScene(RutaLobby, OpenSceneMode.Single);
    }

    static void Colorear(TMP_Text t, Color c)
    {
        if (t == null) return;
        t.color = c;
        EditorUtility.SetDirty(t);
    }

    // =====================================================================
    //  Utilidades
    // =====================================================================
    static Material MaterialTitulo(TMP_Text t)
    {
        if (t.font == null || t.font.material == null) return null;
        Material m = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialTitulo);
        bool nuevo = m == null;
        if (nuevo) m = new Material(t.font.material);
        else m.CopyPropertiesFromMaterial(t.font.material);
        m.name = "TMP_TituloDark";
        m.shaderKeywords = t.font.material.shaderKeywords;
        m.EnableKeyword("UNDERLAY_ON");
        if (m.HasProperty("_UnderlayColor")) m.SetColor("_UnderlayColor", new Color(RojoNeon.r, RojoNeon.g, RojoNeon.b, 0.55f));
        if (m.HasProperty("_UnderlaySoftness")) m.SetFloat("_UnderlaySoftness", 0.7f);
        if (m.HasProperty("_UnderlayDilate")) m.SetFloat("_UnderlayDilate", 0.35f);
        if (m.HasProperty("_UnderlayOffsetX")) m.SetFloat("_UnderlayOffsetX", 0f);
        if (m.HasProperty("_UnderlayOffsetY")) m.SetFloat("_UnderlayOffsetY", 0f);
        if (nuevo)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RutaAbsoluta(RutaMaterialTitulo)));
            AssetDatabase.CreateAsset(m, RutaMaterialTitulo);
        }
        else EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();
        return m;
    }

    static void PonerBorde(GameObject go, Color c, float grosor)
    {
        Outline o = go.GetComponent<Outline>();
        if (o == null) o = go.AddComponent<Outline>();
        o.effectColor = c;
        o.effectDistance = new Vector2(grosor, -grosor);
        o.useGraphicAlpha = false;
        EditorUtility.SetDirty(o);
    }

    static TMP_Text Texto3(RectTransform padre, string nombre, TMP_FontAsset fuente, string texto, float tamano, Vector2 pos, Vector2 size, Color color, FontStyles estilo)
    {
        Transform t = padre.Find(nombre);
        GameObject go = t != null ? t.gameObject : new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        Centrar((RectTransform)go.transform, pos, size);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();
        if (fuente != null) tmp.font = fuente;
        tmp.text = texto;
        tmp.fontSize = tamano;
        tmp.fontStyle = estilo;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        EditorUtility.SetDirty(tmp);
        return tmp;
    }

    static T BuscarOCrear<T>(RectTransform padre, string nombre) where T : Component
    {
        Transform t = padre.Find(nombre);
        GameObject go = t != null ? t.gameObject : new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        T c = go.GetComponent<T>();
        if (c == null) c = go.AddComponent<T>();
        return c;
    }

    static void Estirar(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
        r.localScale = Vector3.one;
        r.localPosition = new Vector3(r.localPosition.x, r.localPosition.y, 0f);
    }

    static void Centrar(RectTransform r, Vector2 pos, Vector2 size)
    {
        r.anchorMin = new Vector2(0.5f, 0.5f);
        r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        r.localScale = Vector3.one;
        r.localPosition = new Vector3(r.localPosition.x, r.localPosition.y, 0f);
    }

    static Color Hex(string hex)
    {
        Color c;
        ColorUtility.TryParseHtmlString(hex, out c);
        return c;
    }

    static string RutaAbsoluta(string rutaAssets)
    {
        return Path.Combine(Path.GetDirectoryName(Application.dataPath), rutaAssets);
    }

    static string Respaldar(string rutaAssets)
    {
        string carpeta = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Backups");
        Directory.CreateDirectory(carpeta);
        string destino = Path.Combine(carpeta, Path.GetFileNameWithoutExtension(rutaAssets) + "_" +
            System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_estilo" + Path.GetExtension(rutaAssets));
        File.Copy(RutaAbsoluta(rutaAssets), destino, true);
        return destino;
    }
}
