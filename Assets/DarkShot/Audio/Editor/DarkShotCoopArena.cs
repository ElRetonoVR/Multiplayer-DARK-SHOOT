using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// MENU 10: crea la escena ArenaCoop = el modo Cooperativo en el mapa del single player.
//   - Parte de una copia de SampleScene (red, MatchManager, HUD, BloomSpawner, susto, generador de baterias).
//   - Quita el mapa del laboratorio (Environment, LabDressing, NavMesh del laboratorio, Losers Lounge).
//   - Trae de Arena el mapa (Maps, con su musica de fondo), las ramas, el escenario y los spawns de enemigos,
//     y copia su iluminacion oscura (ambiente, niebla, fondo de camara).
//   - Quita los scripts del single player de lo que trae, pone el generador de baterias sobre la estacion de
//     Arena y crea 8 spawns de jugador en el piso libre.
//   - Agrega ArenaCoop a Build Settings. Arena y SampleScene NO se modifican.
// Se puede correr de nuevo (rehace ArenaCoop desde cero, con copia en Backups/).
public static class DarkShotCoopArena
{
    const string RutaSample = "Assets/Scenes/SampleScene.unity";
    const string RutaArena = "Assets/Scenes/Arena.unity";
    const string RutaCoop = "Assets/Scenes/ArenaCoop.unity";
    const int SpawnsJugador = 8;

    // Raices de SampleScene que son del mapa del laboratorio
    static readonly string[] QuitarDeSample = { "Environment", "LabDressing", "NavMeshSurfaceCoop", "Losers Lounge" };
    // Raices de Arena que son del single player (no se traen)
    static readonly HashSet<string> NoTraerDeArena = new HashSet<string>
    {
        "Controller", "[BuildingBlock] Camera Rig", "JumpscareBloom", "EventSystem", "Global Volume", "Directional Light"
    };
    // Scripts del single player que se quitan de lo que se trae
    static readonly HashSet<string> ScriptsSingle = new HashSet<string>
    {
        "BatteryStationSingle", "SpawnEnemy", "TimerScript", "ScoreSingle", "GunSingle", "PlayerHealthSolo", "Health",
        "EnemyFollow", "enemyNav", "BloomAnimator", "JumpscareBloom", "ChangeUI"
    };

    [MenuItem("DarkShot/10. Crear escena Cooperativo (mapa Arena)")]
    public static void CrearEscenaCoop()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("DarkShot", "Sal del modo Play antes de ejecutar este paso.", "OK");
            return;
        }
        if (!File.Exists(Abs(RutaSample)) || !File.Exists(Abs(RutaArena)))
        {
            EditorUtility.DisplayDialog("DarkShot", "Faltan " + RutaSample + " o " + RutaArena + ".", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        List<string> hecho = new List<string>();
        List<string> avisos = new List<string>();
        try
        {
            // ---- 1. Copia de SampleScene
            if (File.Exists(Abs(RutaCoop)))
            {
                hecho.Add("Copia de seguridad: " + Respaldar(RutaCoop));
                AssetDatabase.DeleteAsset(RutaCoop);
            }
            if (!AssetDatabase.CopyAsset(RutaSample, RutaCoop)) throw new System.Exception("No se pudo copiar SampleScene a " + RutaCoop);
            AssetDatabase.Refresh();

            Scene coop = EditorSceneManager.OpenScene(RutaCoop, OpenSceneMode.Single);
            int quitados = 0;
            foreach (string n in QuitarDeSample)
            {
                GameObject g = Raiz(coop, n);
                if (g != null) { Object.DestroyImmediate(g); quitados++; }
            }
            hecho.Add("Copia de SampleScene sin el mapa del laboratorio (" + quitados + " objetos raiz quitados)");

            // ---- 2. Traer el mapa de Arena
            Scene arena = EditorSceneManager.OpenScene(RutaArena, OpenSceneMode.Additive);

            // Iluminacion de Arena (RenderSettings es de la escena activa)
            SceneManager.SetActiveScene(arena);
            Luz luz = Luz.Leer();
            Camera camArena = CamaraDe(arena);
            CameraClearFlags flags = camArena != null ? camArena.clearFlags : CameraClearFlags.SolidColor;
            Color fondoCam = camArena != null ? camArena.backgroundColor : Color.black;
            SceneManager.SetActiveScene(coop);
            luz.Aplicar();
            Camera camCoop = CamaraDe(coop);
            if (camCoop != null)
            {
                camCoop.clearFlags = flags;
                camCoop.backgroundColor = fondoCam;
                EditorUtility.SetDirty(camCoop);
            }
            hecho.Add("Iluminacion de Arena copiada (ambiente, niebla, reflejos y fondo de camara)");

            List<string> traidos = new List<string>();
            foreach (GameObject g in arena.GetRootGameObjects())
            {
                if (NoTraerDeArena.Contains(g.name)) continue;
                SceneManager.MoveGameObjectToScene(g, coop);
                traidos.Add(g.name);
            }
            EditorSceneManager.CloseScene(arena, true); // sin guardar: Arena queda igual
            hecho.Add("Traido de Arena: " + string.Join(", ", traidos.ToArray()));

            // ---- 3. Quitar scripts del single player de lo traido
            int scripts = 0;
            foreach (string n in traidos)
            {
                GameObject g = Raiz(coop, n);
                if (g == null) continue;
                foreach (MonoBehaviour mb in g.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb != null && ScriptsSingle.Contains(mb.GetType().Name)) { Object.DestroyImmediate(mb); scripts++; }
                }
                foreach (Transform t in g.GetComponentsInChildren<Transform>(true))
                    scripts += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            }
            hecho.Add("Scripts del single player quitados del mapa: " + scripts);

            // ---- 4. Spawns de Blooms = los de enemigos de Arena
            BloomSpawner bs = Object.FindFirstObjectByType<BloomSpawner>(FindObjectsInactive.Include);
            GameObject spawnsArena = Raiz(coop, "SpawnPoints");
            if (spawnsArena != null)
            {
                spawnsArena.name = "SpawnBlooms";
                List<Transform> lista = new List<Transform>();
                foreach (Transform t in spawnsArena.transform) lista.Add(t);
                if (bs != null && lista.Count > 0)
                {
                    SerializedObject so = new SerializedObject(bs);
                    SerializedProperty arr = so.FindProperty("puntosDeSpawn");
                    if (arr != null)
                    {
                        arr.arraySize = lista.Count;
                        for (int i = 0; i < lista.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = lista[i];
                        hecho.Add("BloomSpawner usa los " + lista.Count + " spawns de enemigos de Arena");
                    }
                    else avisos.Add("BloomSpawner no tiene 'puntosDeSpawn': usara los spawns de jugador.");
                    // Ritmo del single player (SpawnEnemy): la copia de SampleScene traia el tope viejo de 15
                    SerializedProperty max = so.FindProperty("maxVivos");
                    if (max != null) max.intValue = 30;
                    so.ApplyModifiedProperties();
                    hecho.Add("BloomSpawner: oleadas de 1 / 2 / 3 Blooms cada 6 / 5 / 3 s, hasta 30 vivos (como el single player)");
                }
            }
            else avisos.Add("Arena no tiene 'SpawnPoints': los Blooms usaran los spawns de jugador.");
            if (bs == null) avisos.Add("No hay BloomSpawner (corre el menu 8 en SampleScene y vuelve a correr este).");

            Physics.SyncTransforms();

            // ---- 5. Generador de baterias sobre la estacion de Arena
            GameObject gen = Raiz(coop, "BatteryGeneratorMP");
            Transform estacion = BuscarPorNombre(coop, "Station");
            if (gen != null)
            {
                for (int i = gen.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(gen.transform.GetChild(i).gameObject);
                BatterySpawner sp = gen.GetComponent<BatterySpawner>();
                float altura = sp != null ? sp.alturaAparicion : 1.1f;
                Vector3 arriba;
                Bounds b;
                if (estacion != null && Caja(estacion.gameObject, out b)) arriba = new Vector3(b.center.x, b.max.y + 0.25f, b.center.z);
                else arriba = CentroDelPiso(coop) + Vector3.up * 1.2f;
                gen.transform.position = arriba - Vector3.up * altura;
                hecho.Add(estacion != null ? "Generador de baterias sobre la estacion de Arena" : "Generador de baterias en el centro del mapa (no se encontro 'Station')");
            }
            else avisos.Add("No hay BatteryGeneratorMP: en el cooperativo no apareceran baterias en el mapa.");

            // ---- 6. Spawns de jugador en el piso libre
            NetworkManager nm = Object.FindFirstObjectByType<NetworkManager>(FindObjectsInactive.Include);
            List<Transform> spawns = CrearSpawns(coop, spawnsArena, avisos);
            if (nm != null && spawns.Count > 0)
            {
                SerializedObject so = new SerializedObject(nm);
                SerializedProperty arr = so.FindProperty("spawnPoints");
                arr.arraySize = spawns.Count;
                for (int i = 0; i < spawns.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = spawns[i];
                so.ApplyModifiedProperties();
                hecho.Add("Spawns de jugador: " + spawns.Count + " en el piso libre de Arena");
            }

            // ---- 7. Guardar y Build Settings
            EditorSceneManager.MarkSceneDirty(coop);
            if (!EditorSceneManager.SaveScene(coop)) avisos.Add("No se pudo guardar " + RutaCoop + ": guardala con Ctrl+S.");
            else hecho.Add("Escena guardada: " + RutaCoop);

            List<EditorBuildSettingsScene> escenas = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool esta = false;
            foreach (EditorBuildSettingsScene e in escenas) if (e.path == RutaCoop) { e.enabled = true; esta = true; }
            if (!esta) escenas.Add(new EditorBuildSettingsScene(RutaCoop, true));
            EditorBuildSettings.scenes = escenas.ToArray();
            hecho.Add(esta ? "ArenaCoop ya estaba en Build Settings" : "ArenaCoop agregada a Build Settings");

            EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity", OpenSceneMode.Single);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[DarkShot] Error en el paso 10: " + e);
            EditorUtility.DisplayDialog("DarkShot", "El paso 10 fallo: " + e.Message + "\n\nArena y SampleScene no se modificaron. Revisa la consola.", "OK");
            return;
        }

        string texto = "Hecho:\n- " + string.Join("\n- ", hecho.ToArray());
        if (avisos.Count > 0) texto += "\n\nAvisos:\n- " + string.Join("\n- ", avisos.ToArray());
        Debug.Log("[DarkShot] Paso 10\n" + texto);
        EditorUtility.DisplayDialog("DarkShot - Paso 10", texto, "OK");
    }

    // =====================================================================
    //  MENU 11: pilas visibles en la estacion del single player (Arena)
    // =====================================================================
    const string RutaPrefabPila = "Assets/DarkShot/Prefabs/Battery.prefab";

    [MenuItem("DarkShot/11. Pilas en la estacion del single player")]
    public static void PilasEnEstacion()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("DarkShot", "Sal del modo Play antes de ejecutar este paso.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        GameObject pila = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabPila);
        string resumen;
        try
        {
            string copia = Respaldar(RutaArena);
            Scene arena = EditorSceneManager.OpenScene(RutaArena, OpenSceneMode.Single);
            BatteryStationSingle[] estaciones = Object.FindObjectsByType<BatteryStationSingle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (BatteryStationSingle e in estaciones)
            {
                SerializedObject so = new SerializedObject(e);
                so.FindProperty("prefabPila").objectReferenceValue = pila;
                so.ApplyModifiedProperties();
            }
            EditorSceneManager.MarkSceneDirty(arena);
            bool guardada = EditorSceneManager.SaveScene(arena);
            resumen = "Copia de seguridad: " + copia + "\n" +
                      "Estaciones configuradas: " + estaciones.Length + "\n" +
                      (pila != null ? "Modelo de pila: " + RutaPrefabPila : "AVISO: falta " + RutaPrefabPila + " (corre el menu 1); se usara una capsula verde") + "\n" +
                      (guardada ? "Escena guardada: " + RutaArena : "No se pudo guardar Arena: guardala con Ctrl+S") + "\n\n" +
                      "En el juego: una pila al empezar y otra cada 20 s (maximo 3) sobre la estacion; se toman con el grip izquierdo.";
            if (estaciones.Length == 0) resumen += "\n\nAVISO: no hay ninguna BatteryStationSingle en Arena.";
            EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity", OpenSceneMode.Single);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[DarkShot] Error en el paso 11: " + e);
            EditorUtility.DisplayDialog("DarkShot", "El paso 11 fallo: " + e.Message, "OK");
            return;
        }
        Debug.Log("[DarkShot] Paso 11\n" + resumen);
        EditorUtility.DisplayDialog("DarkShot - Paso 11", resumen, "OK");
    }

    // =====================================================================
    //  MENU 12: guantes del avatar en las manos del single player (Arena)
    // =====================================================================
    const string RutaPlayerVR = "Assets/Photon/PhotonUnityNetworking/Resources/PlayerVR.prefab";

    [MenuItem("DarkShot/12. Guantes en el single player")]
    public static void GuantesEnSingle()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("DarkShot", "Sal del modo Play antes de ejecutar este paso.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPlayerVR);
        Transform guanteIzq = player != null ? BuscarHijo(player.transform, "Avatar_LeftHand") : null;
        Transform guanteDer = player != null ? BuscarHijo(player.transform, "Avatar_RightHand") : null;
        if (guanteIzq == null || guanteDer == null)
        {
            EditorUtility.DisplayDialog("DarkShot", "No se encontraron Avatar_LeftHand / Avatar_RightHand en " + RutaPlayerVR + " (corre el menu 5).", "OK");
            return;
        }

        string resumen;
        try
        {
            string copia = Respaldar(RutaArena);
            Scene arena = EditorSceneManager.OpenScene(RutaArena, OpenSceneMode.Single);
            GameObject rig = Raiz(arena, "[BuildingBlock] Camera Rig");
            Transform anclaIzq = rig != null ? BuscarHijo(rig.transform, "LeftHandAnchor") : BuscarPorNombre(arena, "LeftHandAnchor");
            Transform anclaDer = rig != null ? BuscarHijo(rig.transform, "RightHandAnchor") : BuscarPorNombre(arena, "RightHandAnchor");
            if (anclaIzq == null || anclaDer == null) throw new System.Exception("No se encontro el Camera Rig con LeftHandAnchor y RightHandAnchor en Arena.");

            PonerGuante(guanteIzq, anclaIzq, "Guante_Izquierdo");
            PonerGuante(guanteDer, anclaDer, "Guante_Derecho");

            EditorSceneManager.MarkSceneDirty(arena);
            bool guardada = EditorSceneManager.SaveScene(arena);
            resumen = "Copia de seguridad: " + copia + "\n" +
                      "Guante izquierdo en LeftHandAnchor y guante derecho en RightHandAnchor (con la pistola), " +
                      "con la misma posicion que en el jugador del multijugador.\n" +
                      (guardada ? "Escena guardada: " + RutaArena : "No se pudo guardar Arena: guardala con Ctrl+S");
            EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity", OpenSceneMode.Single);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[DarkShot] Error en el paso 12: " + e);
            EditorUtility.DisplayDialog("DarkShot", "El paso 12 fallo: " + e.Message, "OK");
            return;
        }
        Debug.Log("[DarkShot] Paso 12\n" + resumen);
        EditorUtility.DisplayDialog("DarkShot - Paso 12", resumen, "OK");
    }

    // Copia solo lo visual (malla y materiales) del guante del prefab; se puede correr varias veces
    static void PonerGuante(Transform origen, Transform ancla, string nombre)
    {
        Transform viejo = ancla.Find(nombre);
        if (viejo != null) Object.DestroyImmediate(viejo.gameObject);

        GameObject g = Object.Instantiate(origen.gameObject, ancla, false);
        g.name = nombre;
        g.transform.localPosition = origen.localPosition;
        g.transform.localRotation = origen.localRotation;
        g.transform.localScale = origen.localScale;
        foreach (Collider c in g.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        foreach (MonoBehaviour mb in g.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(mb);
        foreach (Renderer r in g.GetComponentsInChildren<Renderer>(true))
        {
            r.enabled = true;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        g.SetActive(true);
    }

    static Transform BuscarHijo(Transform raiz, string nombre)
    {
        foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            if (t.name == nombre) return t;
        return null;
    }

    // ------------------------------------------------------------ Spawns de jugador
    // Rejilla de 1 m sobre el piso: punto libre si cabe una capsula de persona (r 0.45, 0.2..1.8 m), esta a >= 2.5 m
    // de los spawns de Blooms y el rayo hacia abajo pega en el piso. Luego se eligen 8 bien separados
    // (el primero el mas alejado del centro, cada siguiente el mas lejano de los ya elegidos).
    static List<Transform> CrearSpawns(Scene escena, GameObject spawnsBlooms, List<string> avisos)
    {
        List<Transform> res = new List<Transform>();
        Bounds piso;
        float suelo;
        if (!PisoDe(escena, out piso, out suelo))
        {
            avisos.Add("No se encontro el piso (Plane) de Arena: asigna los spawns de jugador a mano.");
            return res;
        }

        List<Vector3> evitar = new List<Vector3>();
        if (spawnsBlooms != null) foreach (Transform t in spawnsBlooms.transform) evitar.Add(t.position);

        List<Vector3> libres = new List<Vector3>();
        for (float x = piso.min.x + 1f; x <= piso.max.x - 1f; x += 1f)
            for (float z = piso.min.z + 1f; z <= piso.max.z - 1f; z += 1f)
            {
                Vector3 p = new Vector3(x, suelo, z);
                if (Physics.CheckCapsule(p + Vector3.up * 0.65f, p + Vector3.up * 1.8f, 0.45f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                RaycastHit hit;
                if (!Physics.Raycast(p + Vector3.up * 1.5f, Vector3.down, out hit, 2.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                if (hit.point.y > suelo + 0.15f) continue; // encima de un obstaculo bajo
                bool cerca = false;
                foreach (Vector3 e in evitar) { Vector3 d = e - p; d.y = 0f; if (d.magnitude < 2.5f) { cerca = true; break; } }
                if (cerca) continue;
                libres.Add(new Vector3(x, hit.point.y, z));
            }
        if (libres.Count == 0)
        {
            avisos.Add("No se encontro piso libre para los spawns de jugador.");
            return res;
        }

        List<Vector3> elegidos = new List<Vector3>();
        Vector3 c = piso.center;
        Vector3 primero = libres[0];
        float mejor = -1f;
        foreach (Vector3 p in libres) { float d = (p - c).sqrMagnitude; if (d > mejor) { mejor = d; primero = p; } }
        elegidos.Add(primero);
        while (elegidos.Count < SpawnsJugador && elegidos.Count < libres.Count)
        {
            Vector3 sig = libres[0];
            float lejos = -1f;
            foreach (Vector3 p in libres)
            {
                float min = float.MaxValue;
                foreach (Vector3 q in elegidos) min = Mathf.Min(min, (p - q).sqrMagnitude);
                if (min > lejos) { lejos = min; sig = p; }
            }
            if (lejos < 1f) break;
            elegidos.Add(sig);
        }

        GameObject grupo = new GameObject("SpawnsCoop");
        SceneManager.MoveGameObjectToScene(grupo, escena);
        for (int i = 0; i < elegidos.Count; i++)
        {
            GameObject s = new GameObject("SpawnMP_" + (i + 1));
            s.transform.SetParent(grupo.transform, false);
            s.transform.position = elegidos[i];
            Vector3 mira = c - elegidos[i];
            mira.y = 0f;
            if (mira.sqrMagnitude > 0.01f) s.transform.rotation = Quaternion.LookRotation(mira); // mirando al centro
            res.Add(s.transform);
        }
        if (res.Count < SpawnsJugador) avisos.Add("Solo se encontraron " + res.Count + " spawns de jugador libres.");
        return res;
    }

    static bool PisoDe(Scene escena, out Bounds b, out float suelo)
    {
        b = new Bounds();
        suelo = 0f;
        Transform plano = BuscarPorNombre(escena, "Plane");
        if (plano == null) return false;
        Collider col = plano.GetComponent<Collider>();
        Renderer r = plano.GetComponent<Renderer>();
        if (col != null) b = col.bounds;
        else if (r != null) b = r.bounds;
        else return false;
        suelo = b.max.y;
        return true;
    }

    static Vector3 CentroDelPiso(Scene escena)
    {
        Bounds b;
        float s;
        return PisoDe(escena, out b, out s) ? new Vector3(b.center.x, s, b.center.z) : Vector3.zero;
    }

    // ------------------------------------------------------------ Iluminacion
    struct Luz
    {
        UnityEngine.Rendering.AmbientMode modo;
        Color cielo, ecuador, suelo, ambiente;
        float intensidad, reflejos, densidad, inicio, fin;
        bool niebla;
        Color colorNiebla;
        FogMode modoNiebla;
        Material skybox;
        UnityEngine.Rendering.DefaultReflectionMode modoReflejo;

        public static Luz Leer()
        {
            Luz l = new Luz();
            l.modo = RenderSettings.ambientMode;
            l.cielo = RenderSettings.ambientSkyColor;
            l.ecuador = RenderSettings.ambientEquatorColor;
            l.suelo = RenderSettings.ambientGroundColor;
            l.ambiente = RenderSettings.ambientLight;
            l.intensidad = RenderSettings.ambientIntensity;
            l.reflejos = RenderSettings.reflectionIntensity;
            l.modoReflejo = RenderSettings.defaultReflectionMode;
            l.niebla = RenderSettings.fog;
            l.colorNiebla = RenderSettings.fogColor;
            l.modoNiebla = RenderSettings.fogMode;
            l.densidad = RenderSettings.fogDensity;
            l.inicio = RenderSettings.fogStartDistance;
            l.fin = RenderSettings.fogEndDistance;
            l.skybox = RenderSettings.skybox;
            return l;
        }

        public void Aplicar()
        {
            RenderSettings.ambientMode = modo;
            RenderSettings.ambientSkyColor = cielo;
            RenderSettings.ambientEquatorColor = ecuador;
            RenderSettings.ambientGroundColor = suelo;
            if (modo == UnityEngine.Rendering.AmbientMode.Flat) RenderSettings.ambientLight = ambiente;
            RenderSettings.ambientIntensity = intensidad;
            RenderSettings.reflectionIntensity = reflejos;
            RenderSettings.defaultReflectionMode = modoReflejo;
            RenderSettings.fog = niebla;
            RenderSettings.fogColor = colorNiebla;
            RenderSettings.fogMode = modoNiebla;
            RenderSettings.fogDensity = densidad;
            RenderSettings.fogStartDistance = inicio;
            RenderSettings.fogEndDistance = fin;
            RenderSettings.skybox = skybox;
        }
    }

    // ------------------------------------------------------------ Utilidades
    static Camera CamaraDe(Scene escena)
    {
        foreach (GameObject g in escena.GetRootGameObjects())
            foreach (Camera c in g.GetComponentsInChildren<Camera>(true))
                if (c.name == "CenterEyeAnchor") return c;
        return null;
    }

    static GameObject Raiz(Scene escena, string nombre)
    {
        foreach (GameObject g in escena.GetRootGameObjects()) if (g.name == nombre) return g;
        return null;
    }

    static Transform BuscarPorNombre(Scene escena, string nombre)
    {
        foreach (GameObject g in escena.GetRootGameObjects())
            foreach (Transform t in g.GetComponentsInChildren<Transform>(true))
                if (t.name == nombre) return t;
        return null;
    }

    static bool Caja(GameObject g, out Bounds b)
    {
        b = new Bounds();
        bool hay = false;
        foreach (Renderer r in g.GetComponentsInChildren<Renderer>(true))
        {
            if (!hay) { b = r.bounds; hay = true; }
            else b.Encapsulate(r.bounds);
        }
        if (!hay)
        {
            foreach (Collider c in g.GetComponentsInChildren<Collider>(true))
            {
                if (!hay) { b = c.bounds; hay = true; }
                else b.Encapsulate(c.bounds);
            }
        }
        return hay;
    }

    static string Abs(string rutaAssets)
    {
        return Path.Combine(Path.GetDirectoryName(Application.dataPath), rutaAssets);
    }

    static string Respaldar(string rutaAssets)
    {
        string carpeta = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Backups");
        Directory.CreateDirectory(carpeta);
        string destino = Path.Combine(carpeta, Path.GetFileNameWithoutExtension(rutaAssets) + "_" +
            System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + Path.GetExtension(rutaAssets));
        File.Copy(Abs(rutaAssets), destino, true);
        return destino;
    }
}
