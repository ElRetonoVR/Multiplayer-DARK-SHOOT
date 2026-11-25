using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu 13: deja el mapa multijugador (SampleScene, laboratorio) del tamano del mapa del single player (Arena)
// y crea los puntos de origen del modo caminando en SampleScene y ArenaCoop.
//   1. Encoge Environment (piso, techo, paredes, obstaculos y spawns originales) en X y Z alrededor del centro del piso,
//      con el mismo factor en los dos ejes: las paredes delgadas se acortan pero conservan su grosor y su altura.
//      Si el piso ya mide como Arena, no vuelve a encoger (el menu se puede repetir).
//   2. La musica de fondo que estaba en LabDressing se pasa a un objeto raiz "MusicaFondo" (el menu 6 borra LabDressing).
//   3. Corre el menu 6 (vuelve a vestir el laboratorio, spawns, generador, lamparas, props y HUD).
//   4. Crea "OrigenesCaminando" (4 puntos lo mas separados posible, todos mirando a +Z) y los asigna al NetworkManager,
//      en SampleScene y en ArenaCoop.
// Despues: correr el menu 7 (boton Caminando en el Lobby) y el 9 (estilo de menus y HUD).
public static class DarkShotModoCaminando
{
    const string RutaSampleScene = "Assets/Scenes/SampleScene.unity";
    const string RutaArenaCoop = "Assets/Scenes/ArenaCoop.unity";

    // Interior del mapa del single player (Plane de Arena): 23.28 x 15.90 m
    const float ArenaAncho = 23.28f;
    const float ArenaLargo = 15.90f;
    const float GrosorMaximoPared = 0.3f; // ejes mas delgados que esto no se encogen
    const int NumeroOrigenes = 4;
    const float GiroOrigenes = 0f; // 0 = mirando a +Z (pared del frente); 90 = +X, 180 = -Z, -90 = -X

    [MenuItem("DarkShot/13. Mapa MP del tamano del single y origenes caminando")]
    public static void Ejecutar()
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
            // ---- 1 y 2. Encoger el laboratorio y salvar la musica
            hecho.Add("Copia de seguridad: " + Respaldar(RutaSampleScene));
            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.OpenScene(RutaSampleScene, OpenSceneMode.Single);
            EncogerMapa(escena, hecho, avisos);
            SalvarMusica(escena, hecho);
            EditorSceneManager.MarkSceneDirty(escena);
            if (!EditorSceneManager.SaveScene(escena)) throw new System.Exception("No se pudo guardar SampleScene");

            // ---- 3. Vestir de nuevo el laboratorio (menu 6; muestra su propio resumen)
            DarkShotMultiplayerSetup.ConfigurarSampleScene();
            hecho.Add("Menu 6 ejecutado (laboratorio, spawns, generador, lamparas, props y HUD rehechos)");

            // ---- 4. Origenes del modo caminando
            escena = EditorSceneManager.GetActiveScene();
            if (escena.path != RutaSampleScene) escena = EditorSceneManager.OpenScene(RutaSampleScene, OpenSceneMode.Single);
            CrearOrigenes(escena, "SampleScene", hecho, avisos);

            if (File.Exists(RutaAbsoluta(RutaArenaCoop)))
            {
                hecho.Add("Copia de seguridad: " + Respaldar(RutaArenaCoop));
                UnityEngine.SceneManagement.Scene coop = EditorSceneManager.OpenScene(RutaArenaCoop, OpenSceneMode.Single);
                CrearOrigenes(coop, "ArenaCoop", hecho, avisos);
                EditorSceneManager.OpenScene(RutaSampleScene, OpenSceneMode.Single);
            }
            else avisos.Add("No se encontro " + RutaArenaCoop + ": el Cooperativo usara 4 spawns separados como origenes.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[DarkShot] Error en el paso 13: " + e);
            EditorUtility.DisplayDialog("DarkShot", "El paso 13 fallo: " + e.Message + "\n\nRevisa la consola. Hay copias de las escenas en la carpeta Backups/.", "OK");
            return;
        }

        string texto = "Hecho:\n- " + string.Join("\n- ", hecho);
        if (avisos.Count > 0) texto += "\n\nAvisos:\n- " + string.Join("\n- ", avisos);
        texto += "\n\nSiguiente: DarkShot/7. Configurar Lobby y luego DarkShot/9. Estilo de menus.";
        Debug.Log("[DarkShot] Paso 13\n" + texto);
        EditorUtility.DisplayDialog("DarkShot - Paso 13", texto, "OK");
    }

    // =====================================================================
    //  MENU 16: interior de los mapas a una medida exacta (de cara interior a cara interior de las paredes)
    // =====================================================================
    const float AnchoInterior = 20.10f; // X
    const float LargoInterior = 13.75f; // Z

    [MenuItem("DarkShot/16. Interior de los mapas a la medida (20.1 x 13.75 m)")]
    public static void AjustarInterior()
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
            hecho.Add("Copia: " + Respaldar(RutaArena));
            UnityEngine.SceneManagement.Scene arena = EditorSceneManager.OpenScene(RutaArena, OpenSceneMode.Single);
            AjustarAMedida(arena, "Arena", "Maps/Walls", GruposArena, SueltosArena, hecho);
            HornearNavMesh(arena, "Arena", "Maps/Plane", null, hecho, avisos);
            Guardar(arena, "Arena", hecho, avisos);

            if (File.Exists(RutaAbsoluta(RutaArenaCoop)))
            {
                hecho.Add("Copia: " + Respaldar(RutaArenaCoop));
                UnityEngine.SceneManagement.Scene coop = EditorSceneManager.OpenScene(RutaArenaCoop, OpenSceneMode.Single);
                AjustarAMedida(coop, "ArenaCoop", "Maps/Walls", GruposCoop, SueltosCoop, hecho);
                HornearNavMesh(coop, "ArenaCoop", "Maps/Plane", "Assets/Scenes/ArenaCoop/NavMesh-Plane.asset", hecho, avisos);
                Guardar(coop, "ArenaCoop", hecho, avisos);
                CrearOrigenes(coop, "ArenaCoop", hecho, avisos);
            }

            hecho.Add("Copia: " + Respaldar(RutaSampleScene));
            UnityEngine.SceneManagement.Scene sample = EditorSceneManager.OpenScene(RutaSampleScene, OpenSceneMode.Single);
            AjustarAMedida(sample, "SampleScene", "Environment/Walls", GruposSample, SueltosSample, hecho);
            SalvarMusica(sample, hecho);
            Guardar(sample, "SampleScene", hecho, avisos);
            DarkShotMultiplayerSetup.ConfigurarSampleScene();
            hecho.Add("Menu 6 ejecutado (laboratorio vestido de nuevo)");
            sample = EditorSceneManager.GetActiveScene();
            if (sample.path != RutaSampleScene) sample = EditorSceneManager.OpenScene(RutaSampleScene, OpenSceneMode.Single);
            CrearOrigenes(sample, "SampleScene", hecho, avisos);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[DarkShot] Error en el paso 16: " + e);
            EditorUtility.DisplayDialog("DarkShot", "El paso 16 fallo: " + e.Message + "\n\nRevisa la consola. Hay copias de las escenas en Backups/.", "OK");
            return;
        }

        string texto = "Hecho:\n- " + string.Join("\n- ", hecho);
        if (avisos.Count > 0) texto += "\n\nAvisos:\n- " + string.Join("\n- ", avisos);
        texto += "\n\nSiguiente: DarkShot/9. Estilo de menus (el menu 6 rehizo el HUD de la partida).";
        Debug.Log("[DarkShot] Paso 16\n" + texto);
        EditorUtility.DisplayDialog("DarkShot - Paso 16", texto, "OK");
    }

    // Escala el mapa alrededor del centro del interior hasta que las caras interiores de las paredes midan
    // AnchoInterior x LargoInterior (unas pocas pasadas porque las paredes gruesas tambien cambian un poco).
    static void AjustarAMedida(UnityEngine.SceneManagement.Scene escena, string nombre, string rutaParedes, string[] grupos, string[] sueltos,
        List<string> hecho)
    {
        float izq, der, atr, fre;
        if (!MedirInterior(escena, rutaParedes, out izq, out der, out atr, out fre))
            throw new System.Exception(nombre + ": no se pudieron medir las paredes en " + rutaParedes);
        string antes = (der - izq).ToString("F2") + " x " + (fre - atr).ToString("F2");

        List<Transform> items = ItemsDelMapa(escena, grupos, sueltos);
        for (int pasada = 0; pasada < 5; pasada++)
        {
            float sx = AnchoInterior / (der - izq);
            float sz = LargoInterior / (fre - atr);
            if (Mathf.Abs(sx - 1f) < 0.0002f && Mathf.Abs(sz - 1f) < 0.0002f) break;
            Vector3 c = new Vector3((izq + der) * 0.5f, 0f, (atr + fre) * 0.5f);
            foreach (Transform t in items)
            {
                if (!AjustarCaja(t, c, sx, sz))
                {
                    Vector3 p = t.position;
                    t.position = new Vector3(c.x + (p.x - c.x) * sx, p.y, c.z + (p.z - c.z) * sz);
                }
                EditorUtility.SetDirty(t);
            }
            Physics.SyncTransforms();
            MedirInterior(escena, rutaParedes, out izq, out der, out atr, out fre);
        }
        hecho.Add(nombre + ": interior de " + antes + " a " + (der - izq).ToString("F2") + " x " + (fre - atr).ToString("F2") + " m");
    }

    static List<Transform> ItemsDelMapa(UnityEngine.SceneManagement.Scene escena, string[] grupos, string[] sueltos)
    {
        List<Transform> items = new List<Transform>();
        foreach (string g in grupos)
        {
            Transform t = BuscarRuta(escena, g);
            if (t != null) foreach (Transform h in t) items.Add(h);
        }
        foreach (string r in sueltos)
        {
            Transform t = BuscarRuta(escena, r);
            if (t != null) items.Add(t);
        }
        return items;
    }

    // Caras interiores de las 4 paredes exteriores (paredes con el lado largo en Z = izquierda/derecha; en X = atras/frente)
    static bool MedirInterior(UnityEngine.SceneManagement.Scene escena, string rutaParedes, out float izq, out float der, out float atr, out float fre)
    {
        izq = float.MinValue; der = float.MaxValue; atr = float.MinValue; fre = float.MaxValue;
        Transform paredes = BuscarRuta(escena, rutaParedes);
        if (paredes == null) return false;
        foreach (Transform w in paredes)
        {
            Vector3 ax = w.TransformVector(Vector3.right) * 0.5f;
            Vector3 az = w.TransformVector(Vector3.forward) * 0.5f;
            float hx = Mathf.Abs(ax.x) + Mathf.Abs(az.x);
            float hz = Mathf.Abs(ax.z) + Mathf.Abs(az.z);
            Vector3 c = w.position;
            if (hx < hz)
            {
                if (c.x < 0f) izq = Mathf.Max(izq, c.x + hx); else der = Mathf.Min(der, c.x - hx);
            }
            else
            {
                if (c.z < 0f) atr = Mathf.Max(atr, c.z + hz); else fre = Mathf.Min(fre, c.z - hz);
            }
        }
        return izq > float.MinValue && der < float.MaxValue && atr > float.MinValue && fre < float.MaxValue;
    }

    // =====================================================================
    //  MENU 15: girar los origenes existentes para que miren derecho a la pared del frente (+Z)
    // =====================================================================
    [MenuItem("DarkShot/15. Origenes caminando mirando al frente")]
    public static void GirarOrigenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("DarkShot", "Sal del modo Play antes de ejecutar este paso.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        List<string> lineas = new List<string>();
        foreach (string ruta in new[] { RutaSampleScene, RutaArenaCoop })
        {
            if (!File.Exists(RutaAbsoluta(ruta))) continue;
            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
            Transform grupo = BuscarRaiz(escena, "OrigenesCaminando");
            if (grupo == null) { lineas.Add(escena.name + ": no hay OrigenesCaminando (corre el menu 13)"); continue; }
            foreach (Transform o in grupo)
            {
                o.rotation = Quaternion.Euler(0f, GiroOrigenes, 0f);
                EditorUtility.SetDirty(o);
                lineas.Add(escena.name + " " + o.name + ": x " + o.position.x.ToString("F2") + ", z " + o.position.z.ToString("F2"));
            }
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
        }
        string texto = "Origenes mirando a +Z (pared del frente):\n- " + string.Join("\n- ", lineas);
        Debug.Log("[DarkShot] Paso 15\n" + texto);
        EditorUtility.DisplayDialog("DarkShot - Paso 15", texto, "OK");
    }

    // =====================================================================
    //  MENU 14: quitar 1 m de ancho y 1 m de largo a los mapas (Arena, ArenaCoop y SampleScene)
    // =====================================================================
    const string RutaArena = "Assets/Scenes/Arena.unity";
    const float MetrosMenos = 1f;

    // Que se ajusta en cada escena: grupos (se ajusta cada hijo directo) y objetos sueltos (solo se mueven)
    static readonly string[] GruposArena = { "Maps/Walls", "Maps/Obstacles", "Maps/BatteryStations", "Branches", "SpawnPoints" };
    static readonly string[] SueltosArena = { "Maps/Plane", "Maps/Ceiling", "[BuildingBlock] Camera Rig" };
    static readonly string[] GruposCoop = { "Maps/Walls", "Maps/Obstacles", "Maps/BatteryStations", "Branches", "SpawnBlooms", "SpawnsCoop" };
    static readonly string[] SueltosCoop = { "Maps/Plane", "Maps/Ceiling", "BatteryGeneratorMP" };
    static readonly string[] GruposSample = { "Environment/Walls", "Environment/Obstacles", "Environment/SpawnPoint" };
    static readonly string[] SueltosSample = { "Environment/Plane", "Environment/Ceiling" };

    [MenuItem("DarkShot/14. Quitar 1 m de ancho y largo a los mapas")]
    public static void QuitarUnMetro()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("DarkShot", "Sal del modo Play antes de ejecutar este paso.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("DarkShot", "Se quitara 1 m de ancho y 1 m de largo a Arena, ArenaCoop y SampleScene (paredes, obstaculos, ramas, spawns y piso). Se guardan copias en Backups/. Cada vez que lo corras se quita otro metro.\n\nContinuar?", "Si", "Cancelar")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        List<string> hecho = new List<string>();
        List<string> avisos = new List<string>();
        try
        {
            // ---- Arena (single player)
            hecho.Add("Copia: " + Respaldar(RutaArena));
            UnityEngine.SceneManagement.Scene arena = EditorSceneManager.OpenScene(RutaArena, OpenSceneMode.Single);
            AchicarEscena(arena, "Arena", "Maps/Plane", GruposArena, SueltosArena, hecho, avisos);
            HornearNavMesh(arena, "Arena", "Maps/Plane", null, hecho, avisos);
            Guardar(arena, "Arena", hecho, avisos);

            // ---- ArenaCoop (cooperativo, mismo mapa que Arena) con su propio NavMesh
            if (File.Exists(RutaAbsoluta(RutaArenaCoop)))
            {
                hecho.Add("Copia: " + Respaldar(RutaArenaCoop));
                UnityEngine.SceneManagement.Scene coop = EditorSceneManager.OpenScene(RutaArenaCoop, OpenSceneMode.Single);
                AchicarEscena(coop, "ArenaCoop", "Maps/Plane", GruposCoop, SueltosCoop, hecho, avisos);
                HornearNavMesh(coop, "ArenaCoop", "Maps/Plane", "Assets/Scenes/ArenaCoop/NavMesh-Plane.asset", hecho, avisos);
                Guardar(coop, "ArenaCoop", hecho, avisos);
                CrearOrigenes(coop, "ArenaCoop", hecho, avisos);
            }

            // ---- SampleScene (laboratorio): se ajusta el mapa base y el menu 6 lo vuelve a vestir
            hecho.Add("Copia: " + Respaldar(RutaSampleScene));
            UnityEngine.SceneManagement.Scene sample = EditorSceneManager.OpenScene(RutaSampleScene, OpenSceneMode.Single);
            AchicarEscena(sample, "SampleScene", "Environment/Plane", GruposSample, SueltosSample, hecho, avisos);
            SalvarMusica(sample, hecho);
            Guardar(sample, "SampleScene", hecho, avisos);
            DarkShotMultiplayerSetup.ConfigurarSampleScene();
            hecho.Add("Menu 6 ejecutado (laboratorio vestido de nuevo)");
            sample = EditorSceneManager.GetActiveScene();
            if (sample.path != RutaSampleScene) sample = EditorSceneManager.OpenScene(RutaSampleScene, OpenSceneMode.Single);
            CrearOrigenes(sample, "SampleScene", hecho, avisos);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[DarkShot] Error en el paso 14: " + e);
            EditorUtility.DisplayDialog("DarkShot", "El paso 14 fallo: " + e.Message + "\n\nRevisa la consola. Hay copias de las escenas en Backups/.", "OK");
            return;
        }

        string texto = "Hecho:\n- " + string.Join("\n- ", hecho);
        if (avisos.Count > 0) texto += "\n\nAvisos:\n- " + string.Join("\n- ", avisos);
        texto += "\n\nSiguiente: DarkShot/9. Estilo de menus (el menu 6 rehizo el HUD de la partida).";
        Debug.Log("[DarkShot] Paso 14\n" + texto);
        EditorUtility.DisplayDialog("DarkShot - Paso 14", texto, "OK");
    }

    static void Guardar(UnityEngine.SceneManagement.Scene escena, string nombre, List<string> hecho, List<string> avisos)
    {
        EditorSceneManager.MarkSceneDirty(escena);
        if (EditorSceneManager.SaveScene(escena)) hecho.Add(nombre + " guardada");
        else avisos.Add("No se pudo guardar " + nombre + ": guardala con Ctrl+S.");
    }

    // Ajusta el mapa para que el piso mida 1 m menos de ancho (X) y de largo (Z), alrededor del centro del piso.
    // Las paredes y obstaculos delgados se acortan sin cambiar su grosor; los demas objetos solo se mueven.
    static void AchicarEscena(UnityEngine.SceneManagement.Scene escena, string nombre, string rutaPiso, string[] grupos, string[] sueltos,
        List<string> hecho, List<string> avisos)
    {
        Transform piso = BuscarRuta(escena, rutaPiso);
        if (piso == null) throw new System.Exception(nombre + ": no se encontro " + rutaPiso);
        Renderer rp = piso.GetComponent<Renderer>();
        Collider cp = piso.GetComponent<Collider>();
        Bounds b = rp != null ? rp.bounds : cp.bounds;
        if (b.size.x <= MetrosMenos + 2f || b.size.z <= MetrosMenos + 2f) throw new System.Exception(nombre + ": el piso es demasiado chico");

        float sx = (b.size.x - MetrosMenos) / b.size.x;
        float sz = (b.size.z - MetrosMenos) / b.size.z;
        Vector3 c = b.center;

        List<Transform> items = new List<Transform>();
        foreach (string g in grupos)
        {
            Transform t = BuscarRuta(escena, g);
            if (t == null) { avisos.Add(nombre + ": no se encontro " + g + " (se omite)"); continue; }
            foreach (Transform h in t) items.Add(h);
        }
        foreach (string r in sueltos)
        {
            Transform t = BuscarRuta(escena, r);
            if (t == null) { avisos.Add(nombre + ": no se encontro " + r + " (se omite)"); continue; }
            items.Add(t);
        }

        int cajas = 0, movidos = 0;
        foreach (Transform t in items)
        {
            if (AjustarCaja(t, c, sx, sz)) cajas++;
            else
            {
                Vector3 p = t.position;
                t.position = new Vector3(c.x + (p.x - c.x) * sx, p.y, c.z + (p.z - c.z) * sz);
                movidos++;
            }
            EditorUtility.SetDirty(t);
        }
        Physics.SyncTransforms();
        Bounds nb = rp != null ? rp.bounds : cp.bounds;
        hecho.Add(nombre + ": piso de " + b.size.x.ToString("F2") + " x " + b.size.z.ToString("F2") + " a " + nb.size.x.ToString("F2") + " x "
            + nb.size.z.ToString("F2") + " m (" + cajas + " paredes/obstaculos ajustados, " + movidos + " objetos movidos)");
    }

    // Caja (Cube o Plane con Renderer/Collider propio): mueve el centro y escala sus ejes horizontales.
    // Delgada (un eje <= 0.3 m): el eje largo se transforma como segmento (largo y angulo nuevos) y el grosor se conserva.
    // Gruesa: cada eje se escala con el factor del eje del mundo al que apunta.
    static bool AjustarCaja(Transform t, Vector3 c, float sx, float sz)
    {
        // Solo las primitivas del mapa (Cube / Plane); los modelos (ramas, estacion) solo se mueven
        MeshFilter mf = t.GetComponent<MeshFilter>();
        string malla = mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : "";
        if (malla != "Cube" && malla != "Plane") return false;
        bool esPlano = malla == "Plane";
        float unidad = esPlano ? 10f : 1f; // el Plane de Unity mide 10 x 10

        Vector3 ax = t.TransformVector(Vector3.right) * unidad;   // eje X local en el mundo (largo completo)
        Vector3 az = t.TransformVector(Vector3.forward) * unidad; // eje Z local en el mundo
        ax.y = 0f; az.y = 0f;

        Vector3 p = t.position;
        t.position = new Vector3(c.x + (p.x - c.x) * sx, p.y, c.z + (p.z - c.z) * sz);

        float lx = ax.magnitude, lz = az.magnitude;
        Vector3 ls = t.localScale;
        bool delgada = !esPlano && Mathf.Min(lx, lz) <= GrosorMaximoPared && Mathf.Max(lx, lz) > GrosorMaximoPared;
        if (delgada)
        {
            bool largoEsX = lx >= lz;
            Vector3 largo = largoEsX ? ax : az;
            Vector3 nuevo = new Vector3(largo.x * sx, 0f, largo.z * sz);
            float giro = Vector3.SignedAngle(largo, nuevo, Vector3.up);
            t.rotation = Quaternion.AngleAxis(giro, Vector3.up) * t.rotation;
            float f = nuevo.magnitude / largo.magnitude;
            if (largoEsX) ls.x *= f; else ls.z *= f;
        }
        else
        {
            // factor por eje segun hacia donde apunta en el mundo (para cajas giradas se interpola)
            float fx = lx > 0.0001f ? new Vector3(ax.x * sx, 0f, ax.z * sz).magnitude / lx : 1f;
            float fz = lz > 0.0001f ? new Vector3(az.x * sx, 0f, az.z * sz).magnitude / lz : 1f;
            ls.x *= fx;
            ls.z *= fz;
        }
        t.localScale = ls;
        return true;
    }

    // Vuelve a hornear el NavMeshSurface del piso (los Blooms caminan sobre el). rutaNueva: guardar en un asset propio
    // (ArenaCoop compartia el de Arena); null = mismo archivo que tenia.
    static void HornearNavMesh(UnityEngine.SceneManagement.Scene escena, string nombre, string rutaPiso, string rutaNueva,
        List<string> hecho, List<string> avisos)
    {
        Transform piso = BuscarRuta(escena, rutaPiso);
        Unity.AI.Navigation.NavMeshSurface surface = piso != null ? piso.GetComponent<Unity.AI.Navigation.NavMeshSurface>() : null;
        if (surface == null)
        {
            avisos.Add(nombre + ": el piso no tiene NavMeshSurface; hornea el NavMesh a mano si los Blooms no caminan bien.");
            return;
        }
        string ruta = rutaNueva;
        if (string.IsNullOrEmpty(ruta))
        {
            ruta = surface.navMeshData != null ? AssetDatabase.GetAssetPath(surface.navMeshData) : "";
            if (string.IsNullOrEmpty(ruta)) ruta = Path.GetDirectoryName(escena.path).Replace('\\', '/') + "/" + escena.name + "/NavMesh-Plane.asset";
        }
        string carpeta = Path.GetDirectoryName(ruta).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(carpeta))
        {
            string padre = Path.GetDirectoryName(carpeta).Replace('\\', '/');
            AssetDatabase.CreateFolder(padre, Path.GetFileName(carpeta));
        }

        surface.RemoveData();
        surface.navMeshData = null;
        if (AssetDatabase.LoadAssetAtPath<Object>(ruta) != null) AssetDatabase.DeleteAsset(ruta);
        surface.BuildNavMesh();
        if (surface.navMeshData == null)
        {
            avisos.Add(nombre + ": el NavMesh salio vacio; revisalo en la ventana Navigation.");
            return;
        }
        AssetDatabase.CreateAsset(surface.navMeshData, ruta);
        surface.navMeshData = AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(ruta);
        surface.AddData();
        EditorUtility.SetDirty(surface);
        AssetDatabase.SaveAssets();
        hecho.Add(nombre + ": NavMesh horneado de nuevo en " + ruta);
    }

    static Transform BuscarRuta(UnityEngine.SceneManagement.Scene escena, string ruta)
    {
        string[] partes = ruta.Split('/');
        Transform t = BuscarRaiz(escena, partes[0]);
        for (int i = 1; i < partes.Length && t != null; i++) t = t.Find(partes[i]);
        return t;
    }

    // ------------------------------------------------------------ Encoger
    static void EncogerMapa(UnityEngine.SceneManagement.Scene escena, List<string> hecho, List<string> avisos)
    {
        Transform environment = BuscarRaiz(escena, "Environment");
        if (environment == null) throw new System.Exception("No se encontro 'Environment' en SampleScene");
        Transform plano = environment.Find("Plane");
        if (plano == null) throw new System.Exception("No se encontro Environment/Plane");
        Renderer rp = plano.GetComponent<Renderer>();
        Collider cp = plano.GetComponent<Collider>();
        Bounds b = rp != null ? rp.bounds : cp.bounds;

        if (b.size.x <= ArenaAncho + 0.5f && b.size.z <= ArenaLargo + 0.5f)
        {
            hecho.Add("El piso ya mide " + b.size.x.ToString("F1") + " x " + b.size.z.ToString("F1") + " m: no se vuelve a encoger");
            return;
        }

        float s = Mathf.Min(ArenaAncho / b.size.x, ArenaLargo / b.size.z);
        Vector3 c = b.center;
        int piezas = 0;

        foreach (Transform hijo in environment)
        {
            if (hijo.name == "OrigenesCaminando") continue;
            bool grupo = hijo.GetComponent<Renderer>() == null && hijo.GetComponent<Collider>() == null && hijo.childCount > 0;
            if (grupo)
            {
                foreach (Transform t in hijo)
                {
                    if (t.name.StartsWith("SpawnMP_")) continue; // los rehace el menu 6
                    Encoger(t, c, s);
                    piezas++;
                }
            }
            else
            {
                Encoger(hijo, c, s);
                piezas++;
            }
        }
        Physics.SyncTransforms();
        Bounds nb = rp != null ? rp.bounds : cp.bounds;
        hecho.Add("Laboratorio encogido x" + s.ToString("F3") + ": de " + b.size.x.ToString("F1") + " x " + b.size.z.ToString("F1")
            + " m a " + nb.size.x.ToString("F1") + " x " + nb.size.z.ToString("F1") + " m (" + piezas + " piezas; altura sin cambios)");
    }

    // Mueve la pieza hacia el centro y acorta sus ejes horizontales largos (el grosor de las paredes se conserva).
    // Las piezas del mapa solo estan giradas en Y, asi que los ejes locales X y Z son horizontales.
    static void Encoger(Transform t, Vector3 centro, float s)
    {
        Vector3 p = t.position;
        t.position = new Vector3(centro.x + (p.x - centro.x) * s, p.y, centro.z + (p.z - centro.z) * s);
        EditorUtility.SetDirty(t);

        // Solo las piezas con geometria cambian de tamano (los spawns y grupos vacios solo se mueven)
        if (t.GetComponent<Renderer>() == null && t.GetComponent<Collider>() == null) return;
        Vector3 ls = t.localScale;
        Vector3 mundo = t.lossyScale;
        if (Mathf.Abs(mundo.x) > GrosorMaximoPared) ls.x *= s;
        if (Mathf.Abs(mundo.z) > GrosorMaximoPared) ls.z *= s;
        t.localScale = ls;
    }

    // ------------------------------------------------------------ Musica
    static void SalvarMusica(UnityEngine.SceneManagement.Scene escena, List<string> hecho)
    {
        Transform dressing = BuscarRaiz(escena, "LabDressing");
        if (dressing == null) return;
        AudioSource[] fuentes = dressing.GetComponents<AudioSource>();
        if (fuentes.Length == 0) return;

        Transform destino = BuscarRaiz(escena, "MusicaFondo");
        GameObject go = destino != null ? destino.gameObject : new GameObject("MusicaFondo");
        foreach (AudioSource a in fuentes)
        {
            if (UnityEditorInternal.ComponentUtility.CopyComponent(a) && UnityEditorInternal.ComponentUtility.PasteComponentAsNew(go))
                Object.DestroyImmediate(a);
        }
        EditorUtility.SetDirty(go);
        hecho.Add("Musica de fondo movida de LabDressing a 'MusicaFondo' (el menu 6 borra LabDressing)");
    }

    // ------------------------------------------------------------ Origenes
    static void CrearOrigenes(UnityEngine.SceneManagement.Scene escena, string nombreEscena, List<string> hecho, List<string> avisos)
    {
        NetworkManager nm = Object.FindFirstObjectByType<NetworkManager>(FindObjectsInactive.Include);
        if (nm == null)
        {
            avisos.Add(nombreEscena + ": no hay NetworkManager, no se crearon origenes.");
            return;
        }
        List<Transform> spawns = new List<Transform>();
        if (nm.spawnPoints != null) foreach (Transform t in nm.spawnPoints) if (t != null) spawns.Add(t);
        if (spawns.Count == 0)
        {
            avisos.Add(nombreEscena + ": el NetworkManager no tiene spawns, no se crearon origenes.");
            return;
        }

        // Muestreo del mas lejano: empieza por el de menor x
        List<Transform> elegidos = new List<Transform>();
        Transform primero = spawns[0];
        foreach (Transform t in spawns) if (t.position.x < primero.position.x) primero = t;
        elegidos.Add(primero);
        while (elegidos.Count < NumeroOrigenes && elegidos.Count < spawns.Count)
        {
            Transform mejor = null;
            float mejorD = -1f;
            foreach (Transform t in spawns)
            {
                if (elegidos.Contains(t)) continue;
                float minima = float.MaxValue;
                foreach (Transform e in elegidos)
                {
                    Vector3 d = t.position - e.position;
                    d.y = 0f;
                    minima = Mathf.Min(minima, d.magnitude);
                }
                if (minima > mejorD) { mejorD = minima; mejor = t; }
            }
            if (mejor == null) break;
            elegidos.Add(mejor);
        }

        string nombreGrupo = "OrigenesCaminando";
        GameObject viejo = null;
        foreach (GameObject r in escena.GetRootGameObjects())
        {
            if (r.name == nombreGrupo) viejo = r;
            Transform dentro = r.transform.Find(nombreGrupo);
            if (dentro != null) viejo = dentro.gameObject;
        }
        if (viejo != null) Object.DestroyImmediate(viejo);

        GameObject grupo = new GameObject(nombreGrupo);
        Transform[] origenes = new Transform[elegidos.Count];
        List<string> posiciones = new List<string>();
        for (int i = 0; i < elegidos.Count; i++)
        {
            GameObject o = new GameObject("Origen_" + (i + 1));
            o.transform.SetParent(grupo.transform, false);
            Vector3 p = elegidos[i].position;
            // Todos miran derecho hacia la pared del frente (+Z): en la sala real todos ven la misma pared
            o.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, GiroOrigenes, 0f));
            origenes[i] = o.transform;
            posiciones.Add("(" + p.x.ToString("F1") + ", " + p.z.ToString("F1") + ")");
        }

        SerializedObject so = new SerializedObject(nm);
        SerializedProperty arr = so.FindProperty("origenesCaminando");
        arr.arraySize = origenes.Length;
        for (int i = 0; i < origenes.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = origenes[i];
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(nm);

        EditorSceneManager.MarkSceneDirty(escena);
        if (EditorSceneManager.SaveScene(escena))
            hecho.Add(nombreEscena + ": " + origenes.Length + " origenes caminando en x,z " + string.Join(" ", posiciones) + " (escena guardada)");
        else avisos.Add(nombreEscena + ": no se pudo guardar, guardala con Ctrl+S.");
    }

    // ------------------------------------------------------------ Utilidades
    static Transform BuscarRaiz(UnityEngine.SceneManagement.Scene escena, string nombre)
    {
        foreach (GameObject r in escena.GetRootGameObjects()) if (r.name == nombre) return r.transform;
        return null;
    }

    static string RutaAbsoluta(string rutaAssets)
    {
        return Path.Combine(Path.GetDirectoryName(Application.dataPath), rutaAssets);
    }

    static string Respaldar(string rutaAssets)
    {
        string carpeta = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Backups");
        Directory.CreateDirectory(carpeta);
        string nombre = Path.GetFileNameWithoutExtension(rutaAssets) + "_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + Path.GetExtension(rutaAssets);
        string destino = Path.Combine(carpeta, nombre);
        File.Copy(RutaAbsoluta(rutaAssets), destino, true);
        return destino;
    }
}
