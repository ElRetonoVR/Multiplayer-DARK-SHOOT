using System.Collections.Generic;
using System.IO;
using Photon.Pun;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Herramienta de editor del MULTIJUGADOR de DARK SHOT (continua la numeracion de DarkShotSetup).
//   DarkShot/4. Importar modelos multijugador
//   DarkShot/5. Configurar PlayerVR (avatar y pistola)
//   DarkShot/6. Configurar SampleScene (laboratorio, partida y generador de baterias)
//   DarkShot/7. Configurar Lobby (salas, con un boton de crear por modo)
//   DarkShot/8. Configurar Cooperativo (Blooms en red y NavMesh)
// Todos los pasos son idempotentes y hacen copia de seguridad (carpeta Backups/ fuera de Assets) antes de
// tocar escenas o prefabs. Si falta un modelo, se avisa y se salta lo que depende de el.
public static class DarkShotMultiplayerSetup
{
    // ---------------------------------------------------------------- Rutas
    const string Raiz = "Assets/DarkShot";
    const string DirModelos = Raiz + "/Models";
    const string DirMateriales = Raiz + "/Materials";
    const string DirPrefabsMP = Raiz + "/Prefabs/Multiplayer";

    const string RutaPlayerFbx = DirModelos + "/Player_CDC.fbx";
    const string RutaEstacionFbx = DirModelos + "/BatteryStation.fbx";
    const string RutaBateriaFbx = DirModelos + "/Battery.fbx";
    const string RutaPrefabBateria = "Assets/Photon/PhotonUnityNetworking/Resources/BatteryPickup.prefab";
    const string RutaPistolaFbx = "Assets/Materials/Pistola Dual Texturas.fbx";
    const string GuidPistola = "52df7b9d7c5b77f49927537484b04df5";
    const string GuidSonidoDisparo = "b2f7e17c403a65144bda4dfc59e6c0ad"; // el mismo clip que usa la pistola de Arena
    const string GuidFuenteTmp = "8f586378b4e144a9851e7b34d9b748ee";     // la fuente que usan los botones del Lobby

    const string RutaPrefabPlayer = "Assets/Photon/PhotonUnityNetworking/Resources/PlayerVR.prefab";
    const string RutaSampleScene = "Assets/Scenes/SampleScene.unity";
    const string RutaLobby = "Assets/Scenes/Lobby.unity";

    static readonly string[] PiezasLab =
    {
        "Lab_WallPanel", "Lab_WallEnd", "Lab_Block", "Lab_Platform", "Lab_Crate",
        "Lab_Table", "Lab_ServerRack", "Lab_CeilingLamp", "Lab_FloorTile"
    };

    static readonly string[] PartesAvatar = { "Avatar_Head", "Avatar_Trunk", "Avatar_LeftHand", "Avatar_RightHand" };

    // Medidas nominales del kit (contrato del modelador)
    const float AltoPanel = 2.93f;
    static readonly Vector3 TamanoBloque = new Vector3(3.17f, 3.0f, 2.98f);
    const float DiametroPlataforma = 5f;
    const float AlturaPlataforma = 0.5f;
    const float LadoLosa = 2f;
    const int MaxLosas = 120; // si el piso necesita mas losas se usa solo el material M_LabFloor

    // Estaticos para batching/occlusion; sin NavigationStatic ni ContributeGI (no obligan a re-hornear luces)
    const StaticEditorFlags FlagsLab =
        StaticEditorFlags.BatchingStatic |
        StaticEditorFlags.OccluderStatic |
        StaticEditorFlags.OccludeeStatic |
        StaticEditorFlags.ReflectionProbeStatic;

    // =====================================================================
    //  MENU 4: Importar modelos multijugador
    // =====================================================================
    [MenuItem("DarkShot/4. Importar modelos multijugador")]
    public static void ImportarModelosMultijugador()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("DarkShot", "Sal del modo Play antes de ejecutar este paso.", "OK");
            return;
        }

        List<string> hecho = new List<string>();
        List<string> avisos = new List<string>();

        try
        {
            Log("Paso 4: importar modelos multijugador...");
            AssetDatabase.Refresh();

            AsegurarCarpeta(DirModelos);
            AsegurarCarpeta(DirMateriales);
            AsegurarCarpeta(DirPrefabsMP);

            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null)
            {
                EditorUtility.DisplayDialog("DarkShot", "No se encontro el shader 'Universal Render Pipeline/Lit'. Revisa que URP este instalado y activo.", "OK");
                return;
            }

            // 1. Importadores + materiales (por nombre de material importado)
            List<string> rutas = new List<string>();
            rutas.Add(RutaPlayerFbx);
            foreach (string pieza in PiezasLab) rutas.Add(DirModelos + "/" + pieza + ".fbx");

            int importados = 0;
            int materialesNuevos = 0;
            foreach (string ruta in rutas)
            {
                ModelImporter imp = AssetImporter.GetAtPath(ruta) as ModelImporter;
                if (imp == null)
                {
                    avisos.Add("No se encontro " + ruta + " (copia el FBX ahi y vuelve a correr el paso 4).");
                    continue;
                }
                materialesNuevos += ConfigurarImportador(ruta, imp, lit, avisos);
                importados++;
            }
            hecho.Add("FBX configurados: " + importados + " de " + rutas.Count + " (materiales URP Lit nuevos: " + materialesNuevos + ")");

            // 2. Comprobar las partes del avatar
            GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPlayerFbx);
            if (player != null)
            {
                foreach (string parte in PartesAvatar)
                {
                    if (BuscarPorNombre(player.transform, parte) == null)
                        avisos.Add("Player_CDC.fbx no tiene el hijo '" + parte + "'.");
                }
            }

            // 3. Prefabs del kit (sin colliders, estaticos)
            int prefabs = 0;
            foreach (string pieza in PiezasLab)
            {
                string ruta = DirModelos + "/" + pieza + ".fbx";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(ruta) == null) continue;
                if (ConstruirPrefabLab(pieza, ruta, avisos)) prefabs++;
            }
            hecho.Add("Prefabs del laboratorio en " + DirPrefabsMP + ": " + prefabs);

            // 4. Prefab de red de la bateria recogible (debe estar en una carpeta Resources para PhotonNetwork.InstantiateRoomObject)
            if (ConstruirPrefabBateria(avisos)) hecho.Add("Prefab de red BatteryPickup: " + RutaPrefabBateria);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        catch (System.Exception e)
        {
            Debug.LogError("[DarkShot] Error en el paso 4: " + e);
            EditorUtility.DisplayDialog("DarkShot", "El paso 4 fallo: " + e.Message + "\n\nRevisa la consola para ver el detalle.", "OK");
            return;
        }

        Resumen("Paso 4", hecho, avisos, "Siguiente: DarkShot/5. Configurar PlayerVR (avatar y pistola)");
    }

    // Configura el ModelImporter y remapea los materiales por nombre. Devuelve cuantos materiales se crearon.
    static int ConfigurarImportador(string ruta, ModelImporter imp, Shader lit, List<string> avisos)
    {
        imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
        imp.animationType = ModelImporterAnimationType.None;
        imp.importAnimation = false;
        imp.bakeAxisConversion = false; // igual que los modelos existentes (contrato del modelador)

        // Quitar remapeos de materiales anteriores para volver a ver los materiales embebidos y sus nombres
        List<AssetImporter.SourceAssetIdentifier> previos = new List<AssetImporter.SourceAssetIdentifier>();
        foreach (KeyValuePair<AssetImporter.SourceAssetIdentifier, Object> kv in imp.GetExternalObjectMap())
        {
            if (kv.Key.type == typeof(Material)) previos.Add(kv.Key);
        }
        foreach (AssetImporter.SourceAssetIdentifier id in previos) imp.RemoveRemap(id);
        imp.SaveAndReimport();

        HashSet<string> nombres = new HashSet<string>();
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(ruta))
        {
            Material m = o as Material;
            if (m != null) nombres.Add(m.name);
        }
        if (nombres.Count == 0)
        {
            avisos.Add(ruta + ": no se encontraron materiales embebidos (se deja el importador estandar).");
            return 0;
        }

        // Re-obtener el importador tras el reimport
        ModelImporter imp2 = AssetImporter.GetAtPath(ruta) as ModelImporter;
        if (imp2 == null) return 0;

        int creados = 0;
        foreach (string nombre in nombres)
        {
            string rutaMat = DirMateriales + "/" + LimpiarNombreArchivo(nombre) + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(rutaMat);
            if (mat == null)
            {
                mat = CrearMaterialGenerico(nombre, rutaMat, lit);
                creados++;
            }
            else if (BuscarDefMaterial(nombre) != null)
            {
                AplicarDefMaterial(mat, BuscarDefMaterial(nombre)); // valores del contrato (MODELOS_MP.md)
                EditorUtility.SetDirty(mat);
            }
            AssetImporter.SourceAssetIdentifier id = new AssetImporter.SourceAssetIdentifier(typeof(Material), nombre);
            imp2.RemoveRemap(id);
            imp2.AddRemap(id, mat);
        }
        imp2.SaveAndReimport();
        return creados;
    }

    // ---- Materiales del contrato MODELOS_MP.md (color base, emision HDR, rugosidad). Los M_Bloom*/M_Branch ya existen y se reutilizan.
    class DefMat
    {
        public string nombre, hex, hexEmision;
        public float multEmision, rugosidad, alfa;
        public DefMat(string nombre, string hex, float rugosidad, string hexEmision = null, float multEmision = 0f, float alfa = 1f)
        {
            this.nombre = nombre; this.hex = hex; this.rugosidad = rugosidad;
            this.hexEmision = hexEmision; this.multEmision = multEmision; this.alfa = alfa;
        }
    }

    static readonly DefMat[] DefMateriales =
    {
        new DefMat("M_AvatarSuit",    "#3C4640", 0.85f),
        new DefMat("M_AvatarVisor",   "#0B1218", 0.08f),
        new DefMat("M_AvatarGear",    "#16181B", 0.70f),
        new DefMat("M_AvatarAccent",  "#E6F0EC", 0.40f, "#E6F0EC", 2.5f),
        new DefMat("M_LabMetal",      "#3A4350", 0.45f),
        new DefMat("M_LabPanel",      "#4A5758", 0.70f),
        new DefMat("M_LabPipe",       "#2A3037", 0.50f),
        new DefMat("M_LabFloor",      "#2E3539", 0.80f),
        new DefMat("M_LabFloorEdge",  "#1F2427", 0.60f),
        new DefMat("M_LabGlass",      "#6FA3AA", 0.05f, null, 0f, 0.35f),
        new DefMat("M_LabLight",      "#FFE2A8", 0.40f, "#FFE2A8", 5f),
        new DefMat("M_LabLED",        "#FF3A22", 0.40f, "#FF3A22", 4f),
        new DefMat("M_LabLEDGreen",   "#3CCB78", 0.40f, "#3CCB78", 3f),
        new DefMat("M_LabLiquid",     "#4FD08A", 0.20f, "#4FD08A", 1.2f),
        new DefMat("M_LabHazard",     "#8C6A1C", 0.60f)
    };

    static DefMat BuscarDefMaterial(string nombre)
    {
        foreach (DefMat d in DefMateriales)
        {
            if (d.nombre == nombre) return d;
        }
        return null;
    }

    static void AplicarDefMaterial(Material m, DefMat d)
    {
        Color baseColor;
        if (!ColorUtility.TryParseHtmlString(d.hex, out baseColor)) baseColor = Color.gray;
        baseColor.a = d.alfa;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", baseColor);
        if (m.HasProperty("_Color")) m.SetColor("_Color", baseColor);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 1f - d.rugosidad);

        if (d.hexEmision != null)
        {
            Color emision;
            if (!ColorUtility.TryParseHtmlString(d.hexEmision, out emision)) emision = Color.white;
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emision * d.multEmision);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        }
        else
        {
            m.DisableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }

        if (d.alfa < 1f)
        {
            // Surface Type = Transparent (URP Lit), mezcla alfa normal
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);
            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }

    // Material URP Lit nuevo: usa el contrato si el nombre esta en la tabla; si no, colores genericos por nombre
    // (gris por defecto; emision si el nombre contiene Light/LED/Accent/Glow)
    static Material CrearMaterialGenerico(string nombre, string ruta, Shader lit)
    {
        Material m = new Material(lit);
        DefMat def = BuscarDefMaterial(nombre);
        if (def != null)
        {
            AplicarDefMaterial(m, def);
            AssetDatabase.CreateAsset(m, ruta);
            return m;
        }

        string hex = "#808080";
        float suavidad = 0.4f;
        string hexEmision = null;
        float intensidadEmision = 0f;
        if (nombre.Contains("Accent")) { hex = "#1AD6FF"; hexEmision = hex; intensidadEmision = 2.5f; }
        else if (nombre.Contains("LED")) { hex = "#FF3030"; hexEmision = hex; intensidadEmision = 3f; }
        else if (nombre.Contains("Glow")) { hex = "#3CFF9A"; hexEmision = hex; intensidadEmision = 2.5f; }
        else if (nombre.Contains("Light")) { hex = "#FFF2D6"; hexEmision = hex; intensidadEmision = 3f; }

        AplicarDefMaterial(m, new DefMat(nombre, hex, 1f - suavidad, hexEmision, intensidadEmision));
        AssetDatabase.CreateAsset(m, ruta);
        return m;
    }

    static bool ConstruirPrefabLab(string nombre, string rutaFbx, List<string> avisos)
    {
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(rutaFbx);
        if (fbx == null) return false;

        UnityEngine.SceneManagement.Scene vista = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject raiz = new GameObject(nombre);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(raiz, vista);

            GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(fbx, raiz.transform);
            modelo.name = "Model";
            modelo.transform.localPosition = Vector3.zero;
            // Se conservan la rotacion y la escala del FBX: en los modelos de una sola malla el giro de -90 en X
            // del exportador de Blender esta en la raiz; ponerlo en cero dejaba el modelo acostado.

            // Sin colliders: el kit es solo visual
            foreach (Collider c in modelo.GetComponentsInChildren<Collider>(true)) c.enabled = false;

            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, FlagsLab);
            }

            string ruta = DirPrefabsMP + "/" + nombre + ".prefab";
            bool ok;
            PrefabUtility.SaveAsPrefabAsset(raiz, ruta, out ok);
            if (!ok) avisos.Add("No se pudo guardar el prefab " + ruta);
            return ok;
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(vista);
        }
    }

    // Bateria recogible: modelo Battery.fbx + PhotonView + BatteryPickup + BoxCollider trigger
    static bool ConstruirPrefabBateria(List<string> avisos)
    {
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(RutaBateriaFbx);
        if (fbx == null)
        {
            avisos.Add("Falta " + RutaBateriaFbx + " (se crea con el menu 1): no se creo el prefab BatteryPickup.");
            return false;
        }

        UnityEngine.SceneManagement.Scene vista = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject raiz = new GameObject("BatteryPickup");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(raiz, vista);

            GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(fbx, raiz.transform);
            modelo.name = "Model";
            modelo.transform.localPosition = Vector3.zero;
            // Se conservan la rotacion y la escala del FBX: en los modelos de una sola malla el giro de -90 en X
            // del exportador de Blender esta en la raiz; ponerlo en cero dejaba el modelo acostado.
            foreach (Collider c in modelo.GetComponentsInChildren<Collider>(true)) c.enabled = false;

            // Caja trigger del tamano del modelo (minimo 0.3 m para que sea facil agarrarla)
            Bounds b;
            Vector3 centro = Vector3.zero;
            Vector3 tam = new Vector3(0.3f, 0.3f, 0.3f);
            if (BoundsDe(raiz, out b))
            {
                centro = raiz.transform.InverseTransformPoint(b.center);
                tam = new Vector3(Mathf.Max(0.3f, b.size.x), Mathf.Max(0.3f, b.size.y), Mathf.Max(0.3f, b.size.z));
            }
            BoxCollider caja = raiz.AddComponent<BoxCollider>();
            caja.isTrigger = true;
            caja.center = centro;
            caja.size = tam;

            PhotonView pv = raiz.AddComponent<PhotonView>();
            pv.Synchronization = ViewSynchronization.Off;
            pv.ObservedComponents = new List<Component>();

            BatteryPickup bp = raiz.AddComponent<BatteryPickup>();
            bp.modelo = modelo.transform;

            bool ok;
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabBateria, out ok);
            if (!ok) avisos.Add("No se pudo guardar " + RutaPrefabBateria);
            return ok;
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(vista);
        }
    }

    // =====================================================================
    //  MENU 5: Configurar PlayerVR (avatar y pistola)
    // =====================================================================
    [MenuItem("DarkShot/5. Configurar PlayerVR (avatar y pistola)")]
    public static void ConfigurarPlayerVR()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("DarkShot", "Sal del modo Play antes de ejecutar este paso.", "OK");
            return;
        }
        if (!File.Exists(RutaAbsoluta(RutaPrefabPlayer)))
        {
            EditorUtility.DisplayDialog("DarkShot", "No se encontro " + RutaPrefabPlayer, "OK");
            return;
        }

        List<string> hecho = new List<string>();
        List<string> avisos = new List<string>();
        GameObject raiz = null;

        try
        {
            Log("Paso 5: configurando " + RutaPrefabPlayer + "...");
            string respaldo = Respaldar(RutaPrefabPlayer);
            hecho.Add("Copia de seguridad: " + respaldo);

            raiz = PrefabUtility.LoadPrefabContents(RutaPrefabPlayer);

            Transform remoteRig = raiz.transform.Find("RemoteRig");
            Transform cabeza = remoteRig != null ? remoteRig.Find("Head") : null;
            Transform tronco = remoteRig != null ? remoteRig.Find("Trunk") : null;
            if (tronco == null && cabeza != null) tronco = cabeza.Find("Trunk"); // primera corrida: Trunk es hijo de Head
            Transform izq = remoteRig != null ? remoteRig.Find("LeftHand") : null;
            Transform der = remoteRig != null ? remoteRig.Find("RightHand") : null;

            if (remoteRig == null || cabeza == null || tronco == null || izq == null || der == null)
            {
                EditorUtility.DisplayDialog("DarkShot", "El prefab PlayerVR no tiene la estructura esperada (RemoteRig > Head, Trunk, LeftHand, RightHand). No se modifico nada.", "OK");
                return;
            }

            PhotonView pv = raiz.GetComponent<PhotonView>();

            // ---- Trunk fuera de Head, bajo RemoteRig, sin sincronizacion de red (lo mueve AvatarVisual en cada cliente)
            if (tronco.parent != remoteRig) tronco.SetParent(remoteRig, false);
            PhotonTransformView ptvTronco = tronco.GetComponent<PhotonTransformView>();
            if (ptvTronco != null)
            {
                if (pv != null && pv.ObservedComponents != null) pv.ObservedComponents.Remove(ptvTronco);
                Object.DestroyImmediate(ptvTronco);
            }
            if (pv != null && pv.ObservedComponents != null)
            {
                pv.ObservedComponents.RemoveAll(c => c == null);
                EditorUtility.SetDirty(pv);
            }
            hecho.Add("Trunk movido bajo RemoteRig y quitado de los observados del PhotonView");

            // ---- Escala 1 y primitivas ocultas
            Transform[] partes = { cabeza, tronco, izq, der };
            foreach (Transform t in partes)
            {
                t.localScale = Vector3.one;
                MeshRenderer mr = t.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false; // se desactiva, no se borra
            }
            tronco.localPosition = Vector3.zero;
            tronco.localRotation = Quaternion.identity;
            hecho.Add("Escala 1 en Head/Trunk/LeftHand/RightHand y primitivas ocultas");

            // ---- Colliders a tamano real + tag Player
            SoloCollider(cabeza, typeof(SphereCollider));
            SphereCollider esfera = cabeza.GetComponent<SphereCollider>();
            if (esfera == null) esfera = cabeza.gameObject.AddComponent<SphereCollider>();
            esfera.radius = 0.15f;
            esfera.center = new Vector3(0f, 0.02f, 0f);
            esfera.isTrigger = false;

            SoloCollider(tronco, typeof(CapsuleCollider));
            CapsuleCollider capsula = tronco.GetComponent<CapsuleCollider>();
            if (capsula == null) capsula = tronco.gameObject.AddComponent<CapsuleCollider>();
            capsula.direction = 1; // eje Y
            capsula.radius = 0.22f;
            capsula.height = 0.75f;
            capsula.center = new Vector3(0f, -0.375f, 0f); // cuelga bajo la base del cuello (pivote del tronco)
            capsula.isTrigger = false;

            Transform[] manos = { izq, der };
            foreach (Transform mano in manos)
            {
                SoloCollider(mano, typeof(BoxCollider));
                BoxCollider caja = mano.GetComponent<BoxCollider>();
                if (caja == null) caja = mano.gameObject.AddComponent<BoxCollider>();
                caja.size = new Vector3(0.1f, 0.1f, 0.12f);
                caja.center = new Vector3(0f, 0f, 0.04f);
                caja.isTrigger = false;
            }
            foreach (Transform t in partes) t.gameObject.tag = "Player";
            hecho.Add("Colliders: cabeza esfera r0.15, tronco capsula r0.22 h0.75, manos caja 0.1 (tag Player)");

            // ---- Modelos del avatar (copia de mallas del FBX, pivotes en la posicion de cada parte)
            GameObject fbxAvatar = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPlayerFbx);
            if (fbxAvatar == null)
            {
                avisos.Add("Falta " + RutaPlayerFbx + ": solo se configuro la pistola y los colliders. Corre el paso 4 con el FBX en su sitio y vuelve a correr el 5.");
            }
            else
            {
                int puestos = 0;
                if (PonerParteAvatar(fbxAvatar, "Avatar_Head", cabeza, avisos)) puestos++;
                if (PonerParteAvatar(fbxAvatar, "Avatar_Trunk", tronco, avisos)) puestos++;
                if (PonerParteAvatar(fbxAvatar, "Avatar_LeftHand", izq, avisos)) puestos++;
                if (PonerParteAvatar(fbxAvatar, "Avatar_RightHand", der, avisos)) puestos++;
                hecho.Add("Partes del avatar colocadas: " + puestos + " de 4");
            }

            // ---- Pistola bajo RightHand
            string rutaPistola = AssetDatabase.GUIDToAssetPath(GuidPistola);
            if (string.IsNullOrEmpty(rutaPistola)) rutaPistola = RutaPistolaFbx;
            GameObject fbxPistola = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPistola);

            Vector3 posPistola = new Vector3(0f, 0f, 0.1943f);
            const float escalaPistola = 0.08f;
            Transform pistola = null;
            for (int i = 0; i < der.childCount; i++)
            {
                if (der.GetChild(i).name.StartsWith("Pistola")) { pistola = der.GetChild(i); break; }
            }
            if (pistola == null && fbxPistola != null)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(fbxPistola, der);
                if (inst != null) pistola = inst.transform;
            }
            if (pistola == null)
            {
                avisos.Add("No se encontro la pistola (" + rutaPistola + "): no se coloco el modelo.");
            }
            else
            {
                pistola.localPosition = posPistola;
                pistola.localRotation = Quaternion.identity;
                pistola.localScale = Vector3.one * escalaPistola;
                foreach (Collider c in pistola.GetComponentsInChildren<Collider>(true)) c.enabled = false;
                hecho.Add("Pistola bajo RightHand: pos (0,0,0.1943), escala 0.08");
            }

            // ---- FirePoint (la geometria es la de Arena: (0,0.91,0.11) dentro del modelo a escala 0.08)
            Transform fp = der.Find("FirePoint");
            if (fp == null)
            {
                GameObject nuevo = new GameObject("FirePoint");
                nuevo.transform.SetParent(der, false);
                fp = nuevo.transform;
            }
            fp.localPosition = posPistola + new Vector3(0f, 0.91f, 0.11f) * escalaPistola;
            fp.localRotation = Quaternion.identity;
            fp.localScale = Vector3.one;

            // ---- Gun: referencias y sonido
            Gun gun = der.GetComponent<Gun>();
            if (gun == null)
            {
                avisos.Add("RightHand no tiene el componente Gun.");
            }
            else
            {
                gun.firePoint = fp;
                gun.gunLight = der.GetComponent<Light>();
                if (gun.gunLight == null) avisos.Add("RightHand no tiene Light: la pistola no iluminara.");

                AudioSource audio = der.GetComponent<AudioSource>();
                if (audio == null) audio = der.gameObject.AddComponent<AudioSource>();
                audio.playOnAwake = false;
                audio.spatialBlend = 1f;
                audio.volume = 0.6f;
                audio.maxDistance = 30f;
                string rutaClip = AssetDatabase.GUIDToAssetPath(GuidSonidoDisparo);
                AudioClip clip = string.IsNullOrEmpty(rutaClip) ? null : AssetDatabase.LoadAssetAtPath<AudioClip>(rutaClip);
                if (clip != null) audio.clip = clip;
                else avisos.Add("No se encontro el clip de disparo de Arena: asigna uno al AudioSource de RightHand si quieres sonido.");
                gun.sonidoDisparo = audio;

                EditorUtility.SetDirty(gun);
                hecho.Add("Gun: firePoint, gunLight y AudioSource asignados");
            }

            // ---- AvatarVisual en la raiz
            AvatarVisual av = raiz.GetComponent<AvatarVisual>();
            if (av == null) av = raiz.AddComponent<AvatarVisual>();
            av.cabeza = cabeza;
            av.tronco = tronco;
            EditorUtility.SetDirty(av);
            hecho.Add("AvatarVisual agregado a la raiz del prefab");

            bool ok;
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabPlayer, out ok);
            if (ok) hecho.Add("Prefab guardado: " + RutaPrefabPlayer);
            else avisos.Add("No se pudo guardar el prefab " + RutaPrefabPlayer);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[DarkShot] Error en el paso 5: " + e);
            EditorUtility.DisplayDialog("DarkShot", "El paso 5 fallo: " + e.Message + "\n\nRevisa la consola. Hay copia del prefab en la carpeta Backups/.", "OK");
            return;
        }
        finally
        {
            if (raiz != null) PrefabUtility.UnloadPrefabContents(raiz);
        }

        AssetDatabase.SaveAssets();
        Resumen("Paso 5", hecho, avisos, "Siguiente: DarkShot/6. Configurar SampleScene (laboratorio y partida)");
    }

    // Deja en el objeto solo colliders del tipo indicado
    static void SoloCollider(Transform t, System.Type tipo)
    {
        Collider[] todos = t.GetComponents<Collider>();
        for (int i = 0; i < todos.Length; i++)
        {
            if (todos[i].GetType() != tipo) Object.DestroyImmediate(todos[i]);
        }
    }

    // Copia la malla de una parte del FBX (con sus hijos) como objetos normales bajo 'destino'.
    // Se copia en vez de instanciar el FBX 4 veces: cada parte queda con su pivote exactamente en el
    // transform de destino. Posicion y rotacion quedan a cero (la pose ensamblada del FBX se descarta); se conserva la escala.
    static bool PonerParteAvatar(GameObject fbx, string nombreParte, Transform destino, List<string> avisos)
    {
        // Quitar copias de corridas anteriores
        for (int i = destino.childCount - 1; i >= 0; i--)
        {
            Transform hijo = destino.GetChild(i);
            if (hijo.name == nombreParte) Object.DestroyImmediate(hijo.gameObject);
        }

        Transform origen = BuscarPorNombre(fbx.transform, nombreParte);
        if (origen == null)
        {
            avisos.Add("Player_CDC.fbx no tiene '" + nombreParte + "': esa parte no se coloco.");
            return false;
        }
        if (origen.GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
        {
            avisos.Add(nombreParte + " usa SkinnedMeshRenderer: el menu solo copia mallas estaticas (MeshFilter).");
        }
        CopiarNodo(origen, destino, true);
        return true;
    }

    static void CopiarNodo(Transform origen, Transform padre, bool esRaiz)
    {
        GameObject go = new GameObject(origen.name);
        go.transform.SetParent(padre, false);
        if (esRaiz)
        {
            // Contrato: cada parte con su pivote en el ancla (la POSICION de la pose del FBX se descarta).
            // La ROTACION del nodo SI se conserva: el exportador de Blender deja -90 en X por la conversion de ejes
            // (igual que en los Blooms); ponerla a cero dejaria la malla acostada. La raiz del FBX esta en identidad.
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Inverse(origen.root.rotation) * origen.rotation;
            go.transform.localScale = origen.lossyScale;
        }
        else
        {
            go.transform.localPosition = origen.localPosition;
            go.transform.localRotation = origen.localRotation;
            go.transform.localScale = origen.localScale;
        }

        MeshFilter mf = origen.GetComponent<MeshFilter>();
        MeshRenderer mr = origen.GetComponent<MeshRenderer>();
        if (mf != null && mf.sharedMesh != null && mr != null)
        {
            MeshFilter nmf = go.AddComponent<MeshFilter>();
            nmf.sharedMesh = mf.sharedMesh;
            MeshRenderer nmr = go.AddComponent<MeshRenderer>();
            nmr.sharedMaterials = mr.sharedMaterials;
            nmr.shadowCastingMode = mr.shadowCastingMode;
            nmr.receiveShadows = mr.receiveShadows;
        }

        for (int i = 0; i < origen.childCount; i++) CopiarNodo(origen.GetChild(i), go.transform, false);
    }

    // =====================================================================
    //  MENU 6: Configurar SampleScene (laboratorio y partida)
    // =====================================================================
    [MenuItem("DarkShot/6. Configurar SampleScene (laboratorio y partida)")]
    public static void ConfigurarSampleScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("DarkShot", "Sal del modo Play antes de ejecutar este paso.", "OK");
            return;
        }
        if (!File.Exists(RutaAbsoluta(RutaSampleScene)))
        {
            EditorUtility.DisplayDialog("DarkShot", "No se encontro " + RutaSampleScene, "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        List<string> hecho = new List<string>();
        List<string> avisos = new List<string>();

        try
        {
            Log("Paso 6: configurando " + RutaSampleScene + "...");
            hecho.Add("Copia de seguridad: " + Respaldar(RutaSampleScene));

            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            if (escena.path != RutaSampleScene) escena = EditorSceneManager.OpenScene(RutaSampleScene, OpenSceneMode.Single);

            // ---- Referencias de la escena
            Transform environment = BuscarEnEscena(escena, "Environment");
            Transform walls = environment != null ? environment.Find("Walls") : null;
            Transform obstacles = environment != null ? environment.Find("Obstacles") : null;
            Transform plano = environment != null ? environment.Find("Plane") : null;
            Transform techo = environment != null ? environment.Find("Ceiling") : null;
            Transform lounge = BuscarEnEscena(escena, "Losers Lounge");
            if (environment == null) avisos.Add("No se encontro 'Environment': no se viste el mapa.");

            // ---- Medidas del mapa
            Bounds mapa = new Bounds(Vector3.zero, new Vector3(36f, 0.1f, 24f));
            float suelo = 0f;
            if (plano != null)
            {
                Renderer rp = plano.GetComponent<Renderer>();
                Collider cp = plano.GetComponent<Collider>();
                if (rp != null) mapa = rp.bounds;
                else if (cp != null) mapa = cp.bounds;
                suelo = mapa.max.y;
            }
            float alturaTecho = techo != null ? techo.position.y : 2.85f;
            Physics.SyncTransforms();

            // ---- Limpiar lo generado en corridas anteriores
            DestruirPorNombre(escena, "LabDressing");
            DestruirPorNombre(escena, "BatteryGeneratorMP");
            LimpiarSpawnsGenerados(environment);

            // ---- Prefabs del kit
            GameObject pPanel = CargarPrefabLab("Lab_WallPanel");
            GameObject pExtremo = CargarPrefabLab("Lab_WallEnd");
            GameObject pBloque = CargarPrefabLab("Lab_Block");
            GameObject pPlataforma = CargarPrefabLab("Lab_Platform");
            GameObject pLosa = CargarPrefabLab("Lab_FloorTile");
            GameObject pLampara = CargarPrefabLab("Lab_CeilingLamp");
            GameObject[] pProps =
            {
                CargarPrefabLab("Lab_Crate"), CargarPrefabLab("Lab_Table"), CargarPrefabLab("Lab_ServerRack")
            };
            if (pPanel == null) avisos.Add("Falta el prefab Lab_WallPanel: las paredes originales NO se ocultan. Corre el paso 4 con los FBX y vuelve a correr el 6.");

            GameObject dressing = new GameObject("LabDressing");
            Transform gParedes = NuevoGrupo(dressing.transform, "Paredes");
            Transform gPiso = NuevoGrupo(dressing.transform, "Piso");
            Transform gProps = NuevoGrupo(dressing.transform, "Props");
            Transform gLamparas = NuevoGrupo(dressing.transform, "Lamparas");

            // ---- 1. Spawns (antes de poner nada que bloquee la fisica de consulta)
            List<Transform> spawns = new List<Transform>();
            Transform spawnRaiz = environment != null ? environment.Find("SpawnPoint") : null;
            if (spawnRaiz != null)
            {
                foreach (Transform hijo in spawnRaiz)
                {
                    if (hijo.name.StartsWith("Spawn") && !hijo.name.StartsWith("SpawnMP_")) spawns.Add(hijo);
                }
            }
            else
            {
                GameObject sr = new GameObject("SpawnPoint");
                if (environment != null) sr.transform.SetParent(environment, false);
                spawnRaiz = sr.transform;
            }

            List<Vector3> libres1 = CeldasLibres(mapa, suelo, 1f, 2f, 1.0f);
            List<Vector3> posSpawns = new List<Vector3>();
            foreach (Transform s in spawns) posSpawns.Add(s.position);
            int faltan = Mathf.Max(0, 8 - spawns.Count);
            List<Vector3> nuevos = ElegirDispersos(libres1, posSpawns, faltan, 0f, mapa.center);
            for (int i = 0; i < nuevos.Count; i++)
            {
                GameObject go = new GameObject("SpawnMP_" + (i + 1));
                go.transform.SetParent(spawnRaiz, false);
                Vector3 p = new Vector3(nuevos[i].x, suelo, nuevos[i].z);
                Vector3 haciaCentro = new Vector3(mapa.center.x - p.x, 0f, mapa.center.z - p.z);
                Quaternion rot = haciaCentro.sqrMagnitude > 0.01f ? Quaternion.LookRotation(haciaCentro.normalized, Vector3.up) : Quaternion.identity;
                go.transform.SetPositionAndRotation(p, rot);
                spawns.Add(go.transform);
                posSpawns.Add(p);
            }
            hecho.Add("Spawns: " + spawns.Count + " (" + nuevos.Count + " nuevos 'SpawnMP_*', a >= 1 m de cualquier collider)");
            if (spawns.Count < 8) avisos.Add("Solo se encontraron " + spawns.Count + " spawns libres (se pedian 8).");

            NetworkManager nm = Object.FindFirstObjectByType<NetworkManager>(FindObjectsInactive.Include);
            if (nm == null)
            {
                avisos.Add("No se encontro el NetworkManager en la escena: asigna los spawns a mano.");
            }
            else
            {
                SerializedObject so = new SerializedObject(nm);
                SerializedProperty arr = so.FindProperty("spawnPoints");
                arr.arraySize = spawns.Count;
                for (int i = 0; i < spawns.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = spawns[i];
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(nm);
                hecho.Add("NetworkManager.spawnPoints: " + spawns.Count + " asignados");
            }

            // ---- 2. Centro: plataforma + BatteryStation (decoracion) + BatterySpawner (genera baterias en red)
            Transform cilindroCentro = BuscarEnEscenaFueraDe(escena, "Cylinder", lounge);
            CrearGeneradorCentral(mapa, suelo, cilindroCentro, pPlataforma, gPiso, posSpawns, hecho, avisos);

            // ---- 3. Vestir paredes y obstaculos delgados
            int paneles = 0, paredesVestidas = 0, sinVestir = 0;
            Transform bloqueCentral = null;
            if (pPanel != null)
            {
                List<Transform> candidatos = new List<Transform>();
                if (walls != null) foreach (Transform t in walls) candidatos.Add(t);
                if (obstacles != null) foreach (Transform t in obstacles) candidatos.Add(t);
                foreach (Transform t in candidatos)
                {
                    int n = VestirPared(t, pPanel, pExtremo, gParedes);
                    if (n > 0) { paredesVestidas++; paneles += n; }
                    else
                    {
                        sinVestir++;
                        if (t.name == "Cube" && bloqueCentral == null) bloqueCentral = t;
                    }
                }
                hecho.Add("Paredes vestidas: " + paredesVestidas + " (" + paneles + " paneles Lab_WallPanel + extremos)");
                if (sinVestir > 1) Log("Objetos no delgados sin vestir (se dejan como estan): " + (sinVestir - (bloqueCentral != null ? 1 : 0)));
            }

            // ---- 4. Bloque central (Cube) y plataforma (Cylinder)
            if (bloqueCentral == null && obstacles != null) bloqueCentral = obstacles.Find("Cube");
            if (bloqueCentral != null && pBloque != null)
            {
                MeshRenderer mrc = bloqueCentral.GetComponent<MeshRenderer>();
                GameObject b = ColocarEnCaja(pBloque, gPiso, bloqueCentral.position, bloqueCentral.rotation, bloqueCentral.lossyScale, "Lab_Block");
                if (b != null && mrc != null)
                {
                    mrc.enabled = false;
                    hecho.Add("Cube central reemplazado por Lab_Block (collider original intacto)");
                }
            }
            else if (pBloque != null) avisos.Add("No se encontro el 'Cube' central: no se coloco Lab_Block.");

            Transform cilindro = cilindroCentro;
            if (cilindro != null && pPlataforma != null)
            {
                MeshRenderer mrc = cilindro.GetComponent<MeshRenderer>();
                Vector3 s = cilindro.lossyScale; // el cilindro de Unity mide 1 x 2 x 1 con escala (d, h/2, d)
                Vector3 tam = new Vector3(s.x, s.y * 2f, s.z);
                if (Mathf.Abs(tam.x - DiametroPlataforma) > 0.01f || Mathf.Abs(tam.y - AlturaPlataforma) > 0.01f)
                    Log("Cylinder: medidas " + tam + " (el kit mide " + DiametroPlataforma + " x " + AlturaPlataforma + ")");
                GameObject pl = ColocarEnCaja(pPlataforma, gPiso, cilindro.position, cilindro.rotation, tam, "Lab_Platform");
                if (pl != null && mrc != null) mrc.enabled = false;
                hecho.Add("Cylinder reemplazado por Lab_Platform");
            }
            else if (cilindro == null) Log("No hay 'Cylinder' en el mapa (el de la Losers Lounge no cuenta): la plataforma se coloca en el centro libre mas cercano.");

            // ---- 5. Piso y techo
            Material matPiso = AssetDatabase.LoadAssetAtPath<Material>(DirMateriales + "/M_LabFloor.mat");
            if (plano != null)
            {
                int columnas = Mathf.CeilToInt(mapa.size.x / LadoLosa);
                int filas = Mathf.CeilToInt(mapa.size.z / LadoLosa);
                if (pLosa != null && columnas * filas <= MaxLosas)
                {
                    Renderer rp = plano.GetComponent<Renderer>();
                    for (int i = 0; i < columnas; i++)
                        for (int j = 0; j < filas; j++)
                        {
                            GameObject l = Instanciar(pLosa, gPiso);
                            l.transform.position = new Vector3(mapa.min.x + (i + 0.5f) * LadoLosa, suelo + 0.005f, mapa.min.z + (j + 0.5f) * LadoLosa);
                            l.transform.rotation = Quaternion.identity;
                        }
                    if (rp != null) rp.enabled = false;
                    hecho.Add("Piso: " + (columnas * filas) + " losas Lab_FloorTile");
                }
                else if (matPiso != null)
                {
                    Renderer rp = plano.GetComponent<Renderer>();
                    if (rp != null)
                    {
                        rp.sharedMaterial = matPiso;
                        EditorUtility.SetDirty(rp);
                    }
                    hecho.Add("Piso: material M_LabFloor aplicado al Plane (" + (columnas * filas) + " losas serian demasiadas para el Quest)");
                }
                else avisos.Add("Piso sin cambios: falta el material M_LabFloor (corre el paso 4).");
            }
            if (techo != null)
            {
                Material matTecho = AssetDatabase.LoadAssetAtPath<Material>(DirMateriales + "/M_LabMetal.mat");
                Renderer rt = techo.GetComponent<Renderer>();
                if (matTecho != null && rt != null)
                {
                    rt.sharedMaterial = matTecho;
                    EditorUtility.SetDirty(rt);
                }

                // El techo es un Plane de una sola cara: desde abajo no se ve y asoma el cielo (rompe la oscuridad).
                // Se agrega una copia volteada solo visual (sin collider) bajo LabDressing.
                MeshFilter mfTecho = techo.GetComponent<MeshFilter>();
                if (mfTecho != null && mfTecho.sharedMesh != null && rt != null)
                {
                    GameObject cara = new GameObject("TechoCaraInferior");
                    cara.transform.SetParent(gPiso, false);
                    cara.transform.SetPositionAndRotation(techo.position - Vector3.up * 0.01f, techo.rotation * Quaternion.Euler(180f, 0f, 0f));
                    cara.transform.localScale = techo.lossyScale;
                    cara.AddComponent<MeshFilter>().sharedMesh = mfTecho.sharedMesh;
                    MeshRenderer mrCara = cara.AddComponent<MeshRenderer>();
                    mrCara.sharedMaterial = matTecho != null ? matTecho : rt.sharedMaterial;
                    mrCara.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    GameObjectUtility.SetStaticEditorFlags(cara, FlagsLab);
                    hecho.Add("Techo: cara inferior agregada (antes desde el piso se veia el cielo)");
                }
            }

            // ---- 6. Lamparas (4, para el rendimiento del Quest) con UNA luz puntual cada una
            if (pLampara != null)
            {
                List<Vector3> libresL = CeldasLibres(mapa, suelo, 1f, 1.5f, 0.6f);
                List<Vector3> posLamparas = ElegirDispersos(libresL, new List<Vector3>(), 4, 0f, mapa.center);
                Color[] colores = { new Color(1f, 0.15f, 0.1f), new Color(1f, 0.6f, 0.15f) };
                for (int i = 0; i < posLamparas.Count; i++)
                {
                    GameObject lamp = Instanciar(pLampara, gLamparas);
                    lamp.transform.position = new Vector3(posLamparas[i].x, alturaTecho, posLamparas[i].z);
                    lamp.transform.rotation = Quaternion.identity;
                    lamp.name = "Lab_CeilingLamp_" + (i + 1);

                    GameObject luzGo = new GameObject("Luz");
                    luzGo.transform.SetParent(lamp.transform, false);
                    luzGo.transform.localPosition = new Vector3(0f, -0.4f, 0f);
                    Light luz = luzGo.AddComponent<Light>();
                    luz.type = LightType.Point;
                    luz.color = colores[i % colores.Length];
                    luz.intensity = 5f;
                    luz.range = 7f;
                    luz.shadows = LightShadows.None;
                }
                hecho.Add("Lamparas: " + posLamparas.Count + " con una luz puntual roja/ambar (intensidad 5, rango 7, sin sombras)");
            }
            else avisos.Add("Falta el prefab Lab_CeilingLamp: no se colocaron lamparas.");

            // ---- 7. Decoracion (props) en espacios abiertos
            int props = ColocarProps(pProps, gProps, mapa, suelo, posSpawns, avisos);
            if (props > 0) hecho.Add("Decoracion: " + props + " props (con BoxCollider para frenar el laser)");

            // ---- 8. Oscuridad: la Losers Lounge ya no se usa (sus luces dejan de iluminar el mapa)
            if (lounge != null)
            {
                lounge.gameObject.SetActive(false);
                hecho.Add("Losers Lounge desactivada (no borrada): sus luces (Point 150 y Directional) ya no iluminan el mapa");
            }
            else
            {
                avisos.Add("No se encontro 'Losers Lounge'. Si sigues viendo el mapa iluminado, baja o desactiva la Point Light de intensidad 150 y la Directional Light.");
            }

            // ---- 9. MatchManager
            ConfigurarMatchManager(escena, hecho, avisos);

            // ---- 10. HUD de partida en el CenterEyeAnchor
            ConfigurarHud(escena, hecho, avisos);

            EditorSceneManager.MarkSceneDirty(escena);
            if (EditorSceneManager.SaveScene(escena)) hecho.Add("Escena guardada: " + RutaSampleScene);
            else avisos.Add("No se pudo guardar la escena automaticamente: guardala con Ctrl+S.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[DarkShot] Error en el paso 6: " + e);
            EditorUtility.DisplayDialog("DarkShot", "El paso 6 fallo: " + e.Message + "\n\nRevisa la consola. Hay copia de la escena en la carpeta Backups/.", "OK");
            return;
        }

        Resumen("Paso 6", hecho, avisos, "Siguiente: DarkShot/7. Configurar Lobby (salas)");
    }

    // ------------------------------------------------------------ Paredes
    // Pared delgada (escala Z <= 0.2, o X <= 0.2 si esta de lado): oculta su MeshRenderer y la cubre con
    // N = round(largo) paneles a lo largo de su eje, mas un poste Lab_WallEnd en cada extremo.
    // El collider original NO se toca (el laser y la fisica siguen igual). Devuelve cuantos paneles puso.
    static int VestirPared(Transform pared, GameObject panel, GameObject extremo, Transform padreGrupo)
    {
        MeshRenderer mr = pared.GetComponent<MeshRenderer>();
        if (mr == null) return 0;

        Vector3 s = pared.lossyScale;
        Vector3 eje;
        float largo;
        Quaternion rot;
        if (s.z <= 0.2f && s.x > s.z)
        {
            eje = pared.right;
            largo = s.x;
            rot = pared.rotation;
        }
        else if (s.x <= 0.2f && s.z > s.x)
        {
            eje = pared.forward;
            largo = s.z;
            rot = pared.rotation * Quaternion.Euler(0f, 90f, 0f); // el X del panel queda sobre el eje largo de la pared
        }
        else
        {
            return 0;
        }

        float alto = s.y;
        int n = Mathf.Max(1, Mathf.RoundToInt(largo));
        float ancho = largo / n;
        Vector3 baseCentro = pared.position - pared.up * (alto * 0.5f);

        GameObject grupo = new GameObject(pared.name);
        grupo.transform.SetParent(padreGrupo, false);

        for (int i = 0; i < n; i++)
        {
            float d = -largo * 0.5f + (i + 0.5f) * ancho;
            GameObject p = Instanciar(panel, grupo.transform);
            p.transform.SetPositionAndRotation(baseCentro + eje * d, rot);
            p.transform.localScale = new Vector3(ancho, alto / AltoPanel, 1f);
        }

        if (extremo != null)
        {
            for (int k = -1; k <= 1; k += 2)
            {
                GameObject e = Instanciar(extremo, grupo.transform);
                e.transform.SetPositionAndRotation(baseCentro + eje * (k * largo * 0.5f), rot);
                e.transform.localScale = new Vector3(1f, alto / AltoPanel, 1f);
            }
        }

        mr.enabled = false;
        return n;
    }

    // Coloca un modelo para que su caja (medida en el editor) coincida con la caja destino (centro, rotacion, tamano).
    // No supone donde esta el pivote del FBX.
    static GameObject ColocarEnCaja(GameObject prefab, Transform padre, Vector3 centro, Quaternion rot, Vector3 tamano, string nombre)
    {
        GameObject inst = Instanciar(prefab, padre);
        inst.name = nombre;
        inst.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        inst.transform.localScale = Vector3.one;

        Bounds b;
        if (!BoundsDe(inst, out b) || b.size.x < 0.0001f || b.size.y < 0.0001f || b.size.z < 0.0001f)
        {
            Object.DestroyImmediate(inst);
            return null;
        }

        Vector3 escala = new Vector3(tamano.x / b.size.x, tamano.y / b.size.y, tamano.z / b.size.z);
        inst.transform.localScale = escala;
        Vector3 desfase = Vector3.Scale(b.center, escala); // centro del modelo respecto al pivote, ya escalado
        inst.transform.SetPositionAndRotation(centro - rot * desfase, rot);
        return inst;
    }

    // ------------------------------------------------------------ Generador central
    // Plataforma cilindrica en el centro con el modelo BatteryStation como decoracion y el BatterySpawner encima.
    // Devuelve la posicion del generador (sobre la plataforma).
    static Vector3 CrearGeneradorCentral(Bounds mapa, float suelo, Transform cilindro, GameObject pPlataforma, Transform padrePlataforma,
        List<Vector3> ocupados, List<string> hecho, List<string> avisos)
    {
        Vector3 centro;
        float alturaPlataforma = 0f;

        if (cilindro != null)
        {
            // El Cylinder del mapa ya marca el centro (su Lab_Platform lo pone el paso 4 de este menu)
            centro = new Vector3(cilindro.position.x, suelo, cilindro.position.z);
            alturaPlataforma = AlturaPlataforma;
        }
        else
        {
            // Sin Cylinder: el punto libre mas cercano al centro donde quepa la plataforma (D 5 m + margen)
            centro = new Vector3(mapa.center.x, suelo, mapa.center.z);
            bool hallado = false;
            float mejorD = float.MaxValue;
            for (float x = mapa.min.x + 3f; x <= mapa.max.x - 3f; x += 1f)
            {
                for (float z = mapa.min.z + 3f; z <= mapa.max.z - 3f; z += 1f)
                {
                    float dx = x - mapa.center.x, dz = z - mapa.center.z;
                    float d = dx * dx + dz * dz;
                    if (d >= mejorD) continue;
                    Vector3 cc = new Vector3(x, suelo + 1.5f, z);
                    Vector3 mitad = new Vector3(DiametroPlataforma * 0.5f + 0.5f, 1.2f, DiametroPlataforma * 0.5f + 0.5f);
                    if (Physics.CheckBox(cc, mitad, Quaternion.identity, Physics.AllLayers, QueryTriggerInteraction.Ignore)) continue;
                    mejorD = d;
                    centro = new Vector3(x, suelo, z);
                    hallado = true;
                }
            }
            if (!hallado) avisos.Add("No hay espacio libre de 5 m para la plataforma central: el generador queda en el centro del mapa (puede solaparse con paredes).");
            else if (pPlataforma != null)
            {
                                alturaPlataforma = AlturaPlataforma;
                GameObject pl = Instanciar(pPlataforma, padrePlataforma);
                pl.name = "Lab_Platform";
                pl.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                Bounds b;
                if (BoundsDe(pl, out b))
                {
                    // base de la plataforma a ras de piso, centrada en 'centro'
                    pl.transform.position = new Vector3(centro.x - b.center.x, suelo - b.min.y, centro.z - b.center.z);
                }
            }
        }

        GameObject raiz = new GameObject("BatteryGeneratorMP");
        raiz.transform.position = new Vector3(centro.x, suelo + alturaPlataforma, centro.z);

        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(RutaEstacionFbx);
        if (fbx != null)
        {
            GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(fbx, raiz.transform);
            modelo.name = "BatteryStation_Model";
            modelo.transform.localPosition = Vector3.zero;
            // Se conservan la rotacion y la escala del FBX: en los modelos de una sola malla el giro de -90 en X
            // del exportador de Blender esta en la raiz; ponerlo en cero dejaba el modelo acostado.
            foreach (Collider c in modelo.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        }
        else avisos.Add("Falta " + RutaEstacionFbx + ": el generador central se crea sin modelo.");

        BatterySpawner spawner = raiz.AddComponent<BatterySpawner>(); // intervalo 20 s, maximo 3 vivas, solo en Jugando
        EditorUtility.SetDirty(spawner);

        ocupados.Add(centro);
        hecho.Add("Generador central de baterias en " + raiz.transform.position + " (BatteryStation como decoracion + BatterySpawner)");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabBateria) == null)
            avisos.Add("Falta el prefab " + RutaPrefabBateria + ": corre el paso 4 (necesita Battery.fbx) o el spawner no podra generar baterias.");
        return centro;
    }

    // ------------------------------------------------------------ Props
    static int ColocarProps(GameObject[] prefabs, Transform padre, Bounds mapa, float suelo, List<Vector3> ocupados, List<string> avisos)
    {
        bool hayAlguno = false;
        foreach (GameObject p in prefabs) if (p != null) hayAlguno = true;
        if (!hayAlguno)
        {
            avisos.Add("Faltan los prefabs de decoracion (Lab_Crate/Lab_Table/Lab_ServerRack): no se coloco decoracion.");
            return 0;
        }

        // Medir cada prop una vez (caja en reposo)
        Vector3[] tamanos = new Vector3[prefabs.Length];
        for (int i = 0; i < prefabs.Length; i++)
        {
            if (prefabs[i] == null) continue;
            GameObject temp = Instanciar(prefabs[i], padre);
            temp.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Bounds b;
            tamanos[i] = BoundsDe(temp, out b) ? b.size : Vector3.one;
            Object.DestroyImmediate(temp);
        }

        List<Vector3> celdas = CeldasLibres(mapa, suelo, 1f, 2f, 1.2f);
        System.Random azar = new System.Random(4242);
        for (int i = celdas.Count - 1; i > 0; i--)
        {
            int j = azar.Next(i + 1);
            Vector3 tmp = celdas[i]; celdas[i] = celdas[j]; celdas[j] = tmp;
        }

        const int objetivo = 18; // mas obstaculos para cubrirse (pedido del usuario 23/11)
        int colocados = 0;
        int tipo = 0;
        foreach (Vector3 c in celdas)
        {
            if (colocados >= objetivo) break;

            // siguiente tipo disponible
            int intentos = 0;
            while (prefabs[tipo % prefabs.Length] == null && intentos++ < prefabs.Length) tipo++;
            int idx = tipo % prefabs.Length;
            if (prefabs[idx] == null) break;

            // lejos de spawns, estaciones y otros props
            bool cerca = false;
            foreach (Vector3 o in ocupados)
            {
                float dx = c.x - o.x, dz = c.z - o.z;
                if (dx * dx + dz * dz < 6.25f) { cerca = true; break; }
            }
            if (cerca) continue;

            float yaw = azar.Next(4) * 90f;
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 tam = tamanos[idx];
            bool girado = Mathf.Approximately(yaw % 180f, 90f);
            float fx = girado ? tam.z : tam.x;
            float fz = girado ? tam.x : tam.z;

            // CheckBox: la huella del prop + 0.8 m de margen por lado debe estar libre (no tapa pasillos)
            Vector3 centroCaja = new Vector3(c.x, suelo + 1.0f, c.z);
            Vector3 mitad = new Vector3(fx * 0.5f + 0.8f, 0.9f, fz * 0.5f + 0.8f);
            if (Physics.CheckBox(centroCaja, mitad, Quaternion.identity, Physics.AllLayers, QueryTriggerInteraction.Ignore)) continue;

            GameObject inst = Instanciar(prefabs[idx], padre);
            inst.transform.SetPositionAndRotation(Vector3.zero, rot);
            Bounds bb;
            if (!BoundsDe(inst, out bb)) { Object.DestroyImmediate(inst); continue; }
            inst.transform.position = new Vector3(c.x - bb.center.x, suelo - bb.min.y, c.z - bb.center.z);

            Bounds bf;
            if (BoundsDe(inst, out bf))
            {
                BoxCollider caja = inst.AddComponent<BoxCollider>();
                caja.center = inst.transform.InverseTransformPoint(bf.center);
                Vector3 sz = inst.transform.InverseTransformVector(bf.size);
                caja.size = new Vector3(Mathf.Abs(sz.x), Mathf.Abs(sz.y), Mathf.Abs(sz.z));
            }
            Physics.SyncTransforms();

            ocupados.Add(c);
            colocados++;
            tipo++;
        }
        return colocados;
    }

    // ------------------------------------------------------------ Partida y HUD
    static void ConfigurarMatchManager(UnityEngine.SceneManagement.Scene escena, List<string> hecho, List<string> avisos)
    {
        MatchManager mm = Object.FindFirstObjectByType<MatchManager>(FindObjectsInactive.Include);
        GameObject go;
        if (mm == null)
        {
            go = new GameObject("MatchManager");
            mm = go.AddComponent<MatchManager>();
        }
        else
        {
            go = mm.gameObject;
        }

        // PhotonView de escena (viewID de escena unico en la escena, < 1000)
        PhotonView pv = go.GetComponent<PhotonView>();
        if (pv == null) pv = go.AddComponent<PhotonView>();
        pv.Synchronization = ViewSynchronization.Off;
        if (pv.ObservedComponents == null) pv.ObservedComponents = new List<Component>();
        if (pv.sceneViewId <= 0)
        {
            int max = 0;
            foreach (PhotonView otro in Object.FindObjectsByType<PhotonView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (otro != pv && otro.sceneViewId > max) max = otro.sceneViewId;
            }
            pv.sceneViewId = max + 1;
        }
        EditorUtility.SetDirty(pv);
        EditorUtility.SetDirty(mm);
        hecho.Add("MatchManager con PhotonView de escena (sceneViewId " + pv.sceneViewId + ")");
    }

    static void ConfigurarHud(UnityEngine.SceneManagement.Scene escena, List<string> hecho, List<string> avisos)
    {
        Transform ojo = BuscarEnEscena(escena, "CenterEyeAnchor");
        if (ojo == null)
        {
            avisos.Add("No se encontro CenterEyeAnchor: no se creo el HUD de partida.");
            return;
        }

        Transform viejo = ojo.Find("MatchHUD");
        if (viejo != null) Object.DestroyImmediate(viejo.gameObject);

        // Canvas world-space propio, hijo de la camara (el canvas "UI" existente es Screen Space - Camera y su tamano
        // depende de la resolucion del visor; con uno propio el tamano en metros es fijo: 1 m delante, 1000 x 560 px a 0.001)
        GameObject hudGo = new GameObject("MatchHUD", typeof(RectTransform), typeof(Canvas));
        hudGo.transform.SetParent(ojo, false);
        RectTransform rt = hudGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1000f, 560f);
        // A 0.5 m (no 1 m) para que una pared cercana no tape el HUD; 1000 x 560 px a 0.0004 = 0.4 x 0.22 m (~44 grados)
        rt.localPosition = new Vector3(0f, 0f, 0.5f);
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one * 0.0004f;
        Canvas canvas = hudGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        TMP_FontAsset fuente = CargarFuenteTmp();

        TMP_Text estado = CrearTexto(rt, "Estado", fuente, 54f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(800f, 70f), Color.white);
        TMP_Text bateria = CrearTexto(rt, "Bateria", fuente, 34f, TextAlignmentOptions.BottomLeft,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 20f), new Vector2(520f, 50f), Color.white);
        TMP_Text kills = CrearTexto(rt, "Kills", fuente, 34f, TextAlignmentOptions.BottomRight,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(520f, 50f), Color.white);
        TMP_Text invencible = CrearTexto(rt, "Invencible", fuente, 44f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(600f, 60f), new Color(0.2f, 0.9f, 1f));
        TMP_Text marcador = CrearTexto(rt, "Marcador", fuente, 34f, TextAlignmentOptions.TopLeft,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(760f, 420f), Color.white);

        MatchHUD hud = hudGo.AddComponent<MatchHUD>();
        hud.textoEstado = estado;
        hud.textoBateria = bateria;
        hud.textoKills = kills;
        hud.textoInvencible = invencible;
        hud.textoMarcador = marcador;
        EditorUtility.SetDirty(hud);

        hecho.Add("HUD de partida: canvas 'MatchHUD' (world-space, 0.5 m delante) bajo CenterEyeAnchor con 5 textos TMP");
    }

    static TMP_Text CrearTexto(RectTransform padre, string nombre, TMP_FontAsset fuente, float tamano, TextAlignmentOptions alineacion,
        Vector2 anclaMin, Vector2 anclaMax, Vector2 pivote, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = anclaMin;
        r.anchorMax = anclaMax;
        r.pivot = pivote;
        r.anchoredPosition = pos;
        r.sizeDelta = size;

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (fuente != null) t.font = fuente;
        t.fontSize = tamano;
        t.alignment = alineacion;
        t.color = color;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.richText = true;
        t.raycastTarget = false;
        t.text = "";
        return t;
    }

    static TMP_FontAsset CargarFuenteTmp()
    {
        string ruta = AssetDatabase.GUIDToAssetPath(GuidFuenteTmp);
        if (string.IsNullOrEmpty(ruta)) return null;
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ruta);
    }

    // =====================================================================
    //  MENU 7: Configurar Lobby (salas)
    // =====================================================================
    [MenuItem("DarkShot/7. Configurar Lobby (salas)")]
    public static void ConfigurarLobby()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("DarkShot", "Sal del modo Play antes de ejecutar este paso.", "OK");
            return;
        }
        if (!File.Exists(RutaAbsoluta(RutaLobby)))
        {
            EditorUtility.DisplayDialog("DarkShot", "No se encontro " + RutaLobby, "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        List<string> hecho = new List<string>();
        List<string> avisos = new List<string>();

        try
        {
            Log("Paso 7: configurando " + RutaLobby + "...");
            hecho.Add("Copia de seguridad: " + Respaldar(RutaLobby));

            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            if (escena.path != RutaLobby) escena = EditorSceneManager.OpenScene(RutaLobby, OpenSceneMode.Single);

            // ---- Menu / botones existentes
            Transform menu = null;
            foreach (Transform t in TodosLosTransforms(escena))
            {
                if (t.name == "Menu" && t.GetComponent<Canvas>() != null) { menu = t; break; }
            }
            Transform botones = menu != null ? menu.Find("Buttons") : null;
            Transform multi = botones != null ? botones.Find("MultiBtn") : null;
            if (menu == null || botones == null || multi == null)
            {
                EditorUtility.DisplayDialog("DarkShot", "No se encontro UI/Menu/Buttons/MultiBtn en el Lobby. No se modifico nada.", "OK");
                return;
            }
            RectTransform menuRt = (RectTransform)menu;
            Button multiBtn = multi.GetComponent<Button>();
            TMP_Text multiTexto = multi.GetComponentInChildren<TMP_Text>(true);

            // ---- Limpiar lo generado antes
            Transform viejo = menu.Find("RoomPanel");
            if (viejo != null) Object.DestroyImmediate(viejo.gameObject);
            DestruirPorNombre(escena, "RoomBrowser");

            // ---- Panel
            GameObject panelGo = new GameObject("RoomPanel", typeof(RectTransform));
            panelGo.transform.SetParent(menuRt, false);
            RectTransform panelRt = (RectTransform)panelGo.transform;
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            panelRt.localScale = Vector3.one;
            panelRt.localPosition = new Vector3(panelRt.localPosition.x, panelRt.localPosition.y, 0f);

            GameObject fondoGo = new GameObject("Fondo", typeof(RectTransform));
            fondoGo.transform.SetParent(panelRt, false);
            RectTransform fondoRt = (RectTransform)fondoGo.transform;
            fondoRt.anchorMin = Vector2.zero;
            fondoRt.anchorMax = Vector2.one;
            fondoRt.offsetMin = Vector2.zero;
            fondoRt.offsetMax = Vector2.zero;
            Image fondo = fondoGo.AddComponent<Image>();
            fondo.color = new Color(0.03f, 0.03f, 0.05f, 0.95f);
            fondo.raycastTarget = true;

            TMP_FontAsset fuente = multiTexto != null ? multiTexto.font : CargarFuenteTmp();

            TMP_Text titulo = CrearTextoLobby(panelRt, "Titulo", fuente, 9f, new Vector2(0f, 43f), new Vector2(120f, 10f), Color.white);
            titulo.text = "Salas";
            TMP_Text estado = CrearTextoLobby(panelRt, "Estado", fuente, 5.5f, new Vector2(0f, 34f), new Vector2(180f, 8f), new Color(0.75f, 0.9f, 1f));
            estado.text = "";

            // ---- 6 botones de sala (clon de MultiBtn: heredan estilo y la interaccion por rayo)
            Button[] botonesSala = new Button[6];
            for (int i = 0; i < 6; i++)
            {
                int col = i % 2;
                int fila = i / 2;
                Vector2 pos = new Vector2(col == 0 ? -47f : 47f, 21f - fila * 14f);
                botonesSala[i] = ClonarBoton(multi.gameObject, panelRt, "Sala" + (i + 1), pos, new Vector2(92f, 12f), "Sala " + (i + 1), 4.5f);
                botonesSala[i].gameObject.SetActive(false);
            }
            // Panel de 193.4 x 100: las 6 salas ocupan y = 27..-13; los tres botones de modo van en y = -25 (alto 14, de -18 a -32)
            // y Volver en y = -42 (alto 12, de -36 a -48), sin tapar la lista.
            Button botonCrearFFA = ClonarBoton(multi.gameObject, panelRt, "CrearFFABtn", new Vector2(-62f, -25f), new Vector2(58f, 14f), "Todos vs todos", 5.5f);
            Button botonCrearEquipos = ClonarBoton(multi.gameObject, panelRt, "CrearEquiposBtn", new Vector2(0f, -25f), new Vector2(58f, 14f), "Equipos", 5.5f);
            Button botonCrearCoop = ClonarBoton(multi.gameObject, panelRt, "CrearCoopBtn", new Vector2(62f, -25f), new Vector2(58f, 14f), "Cooperativo", 5.5f);
            Button botonVolver = ClonarBoton(multi.gameObject, panelRt, "VolverBtn", new Vector2(0f, -42f), new Vector2(52f, 12f), "Volver", 6f);
            hecho.Add("RoomPanel: titulo, estado, 6 botones de sala, 3 botones de crear sala por modo y Volver (clonados de MultiBtn)");

            // ---- RoomBrowser (objeto raiz, siempre activo)
            GameObject rbGo = new GameObject("RoomBrowser");
            RoomBrowser rb = rbGo.AddComponent<RoomBrowser>();
            rb.panel = panelGo;
            rb.menuPrincipal = botones.gameObject;
            rb.textoEstado = estado;
            rb.botonesSala = botonesSala;
            rb.botonCrear = null; // reemplazado por los tres botones de modo
            rb.botonCrearFFA = botonCrearFFA;
            rb.botonCrearEquipos = botonCrearEquipos;
            rb.botonCrearCoop = botonCrearCoop;
            rb.botonVolver = botonVolver;
            rb.escenaPartida = "SampleScene";
            EditorUtility.SetDirty(rb);

            // ---- MultiBtn: abrir el panel en vez de cargar SampleScene
            for (int i = multiBtn.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                UnityEventTools.RemovePersistentListener(multiBtn.onClick, i);
            }
            UnityEventTools.AddPersistentListener(multiBtn.onClick, rb.AbrirPanel);
            EditorUtility.SetDirty(multiBtn);
            hecho.Add("MultiBtn ahora llama a RoomBrowser.AbrirPanel (ya no carga SampleScene directo)");

            panelGo.SetActive(false);

            EditorSceneManager.MarkSceneDirty(escena);
            if (EditorSceneManager.SaveScene(escena)) hecho.Add("Escena guardada: " + RutaLobby);
            else avisos.Add("No se pudo guardar la escena automaticamente: guardala con Ctrl+S.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[DarkShot] Error en el paso 7: " + e);
            EditorUtility.DisplayDialog("DarkShot", "El paso 7 fallo: " + e.Message + "\n\nRevisa la consola. Hay copia de la escena en la carpeta Backups/.", "OK");
            return;
        }

        Resumen("Paso 7", hecho, avisos, "Prueba: abre Lobby, Play, Multiplayer > Todos vs todos / Equipos / Cooperativo (o abre SampleScene directo para probar solo).");
    }

    static Button ClonarBoton(GameObject original, RectTransform padre, string nombre, Vector2 pos, Vector2 size, string texto, float tamanoFuente)
    {
        GameObject clon = (GameObject)Object.Instantiate(original, padre, false);
        clon.name = nombre;
        clon.SetActive(true);

        RectTransform r = (RectTransform)clon.transform;
        r.anchorMin = new Vector2(0.5f, 0.5f);
        r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        r.localScale = Vector3.one;

        Button b = clon.GetComponent<Button>();
        // El clon trae el onClick persistente de MultiBtn (cargar escena): se quita
        for (int i = b.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            UnityEventTools.RemovePersistentListener(b.onClick, i);
        }

        TMP_Text t = clon.GetComponentInChildren<TMP_Text>(true);
        if (t != null)
        {
            t.text = texto;
            t.fontSize = tamanoFuente;
            t.enableAutoSizing = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
        }
        return b;
    }

    static TMP_Text CrearTextoLobby(RectTransform padre, string nombre, TMP_FontAsset fuente, float tamano, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        RectTransform r = (RectTransform)go.transform;
        r.anchorMin = new Vector2(0.5f, 0.5f);
        r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (fuente != null) t.font = fuente;
        t.fontSize = tamano;
        t.alignment = TextAlignmentOptions.Center;
        t.color = color;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        return t;
    }

    // =====================================================================
    //  MENU 8: Configurar Cooperativo (Blooms en red y NavMesh)
    // =====================================================================
    static readonly string[] VariantesBloom = { "Base", "LabCoat", "Engineer" };
    const string DirResourcesPhoton = "Assets/Photon/PhotonUnityNetworking/Resources";
    const string DirNavMesh = "Assets/Scenes/SampleScene";
    const string RutaNavMeshData = DirNavMesh + "/NavMesh-BloomNet.asset";
    const float AlturaMaximaNavMesh = 2.5f; // todo lo que empiece por encima (techo, lamparas) queda fuera del NavMesh

    [MenuItem("DarkShot/8. Configurar Cooperativo (Blooms en red y NavMesh)")]
    public static void ConfigurarCooperativo()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("DarkShot", "Sal del modo Play antes de ejecutar este paso.", "OK");
            return;
        }
        if (!File.Exists(RutaAbsoluta(RutaSampleScene)))
        {
            EditorUtility.DisplayDialog("DarkShot", "No se encontro " + RutaSampleScene, "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        List<string> hecho = new List<string>();
        List<string> avisos = new List<string>();

        try
        {
            Log("Paso 8: configurando el modo Cooperativo...");

            // ---- 1. Prefabs BloomNet_*
            AsegurarCarpeta(DirResourcesPhoton);
            for (int i = 0; i < VariantesBloom.Length; i++)
            {
                CrearPrefabBloomNet(VariantesBloom[i], hecho, avisos);
            }

            // ---- 2. Escena: BloomSpawner + NavMeshSurface
            hecho.Add("Copia de seguridad: " + Respaldar(RutaSampleScene));
            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            if (escena.path != RutaSampleScene) escena = EditorSceneManager.OpenScene(RutaSampleScene, OpenSceneMode.Single);

            // BloomSpawner (idempotente)
            BloomSpawner spawner = Object.FindFirstObjectByType<BloomSpawner>(FindObjectsInactive.Include);
            if (spawner == null)
            {
                GameObject go = new GameObject("BloomSpawner");
                spawner = go.AddComponent<BloomSpawner>();
            }
            EditorUtility.SetDirty(spawner);
            hecho.Add("BloomSpawner en la escena (usa los spawns del NetworkManager; cada 6/5/3 s, maximo 15 vivos)");

            ConfigurarNavMesh(escena, hecho, avisos);
            ConfigurarSustoCoop(escena, hecho, avisos);

            EditorSceneManager.MarkSceneDirty(escena);
            if (EditorSceneManager.SaveScene(escena)) hecho.Add("Escena guardada: " + RutaSampleScene);
            else avisos.Add("No se pudo guardar la escena automaticamente: guardala con Ctrl+S.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[DarkShot] Error en el paso 8: " + e);
            EditorUtility.DisplayDialog("DarkShot", "El paso 8 fallo: " + e.Message + "\n\nRevisa la consola. Hay copia de la escena en la carpeta Backups/.", "OK");
            return;
        }

        Resumen("Paso 8", hecho, avisos, "Prueba: Lobby > Cooperativo, marca listo (A / K) y dispara a los Blooms. Revisa el NavMesh con la ventana Navigation (Show NavMesh).");
    }

    // Susto del Bloom en SampleScene: mismo JumpscareBloom de Arena (cara Bloom_Face + Bloom_Breath), lo activa
    // PlayerHealth en el cliente golpeado por un Bloom. Idempotente: se recrea el objeto "JumpscareBloom".
    static void ConfigurarSustoCoop(UnityEngine.SceneManagement.Scene escena, List<string> hecho, List<string> avisos)
    {
        DestruirPorNombre(escena, "JumpscareBloom");

        GameObject go = new GameObject("JumpscareBloom");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, escena);
        JumpscareBloom jb = go.AddComponent<JumpscareBloom>();

        AudioSource fuente = go.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = false;
        fuente.spatialBlend = 0f; // pegado al oido
        fuente.volume = 1f;
        fuente.clip = BuscarClipRespiracion();
        jb.sonidoSusto = fuente;
        if (fuente.clip == null) avisos.Add("Susto sin sonido: falta Assets/DarkShot/Audio/Bloom_Breath.*");

        Transform ojo = BuscarEnEscena(escena, "CenterEyeAnchor");
        if (ojo != null) jb.cabezaJugador = ojo;
        else avisos.Add("No se encontro CenterEyeAnchor: el susto usara Camera.main.");

        GameObject prefabCara = AssetDatabase.LoadAssetAtPath<GameObject>(Raiz + "/Prefabs/Bloom_Face.prefab");
        if (prefabCara != null)
        {
            GameObject cara = (GameObject)PrefabUtility.InstantiatePrefab(prefabCara, go.transform);
            cara.name = "Bloom_Face";
            cara.transform.localPosition = Vector3.zero;
            foreach (Collider c in cara.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            cara.SetActive(false);
            jb.caraBloom = cara;
        }
        else avisos.Add("Falta " + Raiz + "/Prefabs/Bloom_Face.prefab (menu 1): el susto no muestra la cara.");

        EditorUtility.SetDirty(jb);
        hecho.Add("Susto del Bloom en SampleScene (JumpscareBloom + Bloom_Face + Bloom_Breath): se activa al recibir un golpe de Bloom");
    }

    static AudioClip BuscarClipRespiracion()
    {
        foreach (string guid in AssetDatabase.FindAssets("Bloom_Breath t:AudioClip", new[] { Raiz + "/Audio" }))
        {
            AudioClip c = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
            if (c != null) return c;
        }
        return null;
    }

    // Copia Bloom_<variante>.prefab a Resources como BloomNet_<variante>.prefab, quita la logica de un jugador y agrega la red
    static void CrearPrefabBloomNet(string variante, List<string> hecho, List<string> avisos)
    {
        string origen = Raiz + "/Prefabs/Bloom_" + variante + ".prefab";
        string destino = DirResourcesPhoton + "/BloomNet_" + variante + ".prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(origen) == null)
        {
            avisos.Add("No se encontro " + origen + ": no se creo BloomNet_" + variante + ".");
            return;
        }

        // Se parte siempre de una copia limpia del original (idempotente)
        if (AssetDatabase.LoadAssetAtPath<GameObject>(destino) != null) AssetDatabase.DeleteAsset(destino);
        if (!AssetDatabase.CopyAsset(origen, destino))
        {
            avisos.Add("No se pudo copiar " + origen + " a " + destino);
            return;
        }

        GameObject raiz = PrefabUtility.LoadPrefabContents(destino);
        try
        {
            // Componentes sueltos: EnemyFollow (la clase vive en enemyNav.cs y puede salir como Missing Script) y Health
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (PrefabUtility.GetNearestPrefabInstanceRoot(t.gameObject) != null) continue; // dentro del FBX anidado no se toca
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            }
            Component[] comps = raiz.GetComponents<Component>();
            for (int i = 0; i < comps.Length; i++)
            {
                if (comps[i] == null) continue;
                string n = comps[i].GetType().Name;
                if (n == "EnemyFollow" || n == "Health") Object.DestroyImmediate(comps[i], true);
            }

            // Red: PhotonView + PhotonTransformView (posicion y rotacion) + BloomNet
            PhotonView pv = raiz.GetComponent<PhotonView>();
            if (pv == null) pv = raiz.AddComponent<PhotonView>();
            PhotonTransformView ptv = raiz.GetComponent<PhotonTransformView>();
            if (ptv == null) ptv = raiz.AddComponent<PhotonTransformView>();
            ptv.m_SynchronizePosition = true;
            ptv.m_SynchronizeRotation = true;
            ptv.m_SynchronizeScale = false;
            pv.Synchronization = ViewSynchronization.UnreliableOnChange;
            pv.ObservedComponents = new List<Component>();
            pv.ObservedComponents.Add(ptv);

            if (raiz.GetComponent<BloomNet>() == null) raiz.AddComponent<BloomNet>();
            raiz.tag = "Enemy";

            // Respiracion 3D en bucle (Assets/DarkShot/Audio/Bloom_Breath.*), se oye a pocos metros en la oscuridad
            AudioClip clipRespiracion = BuscarClipRespiracion();
            if (clipRespiracion != null)
            {
                AudioSource fuente = raiz.GetComponent<AudioSource>();
                if (fuente == null) fuente = raiz.AddComponent<AudioSource>();
                fuente.clip = clipRespiracion;
                fuente.loop = true;
                fuente.playOnAwake = false; // BloomNet lo arranca con un desfase al azar
                fuente.spatialBlend = 1f;
                fuente.rolloffMode = AudioRolloffMode.Linear;
                fuente.minDistance = 1f;
                fuente.maxDistance = 9f;
                fuente.volume = 0.7f;
                fuente.dopplerLevel = 0f;
            }
            else avisos.Add("No se encontro Assets/DarkShot/Audio/Bloom_Breath.*: los Blooms no respiran.");

            if (raiz.GetComponent<UnityEngine.AI.NavMeshAgent>() == null) avisos.Add("BloomNet_" + variante + " no tiene NavMeshAgent.");
            if (raiz.GetComponent<Collider>() == null) avisos.Add("BloomNet_" + variante + " no tiene collider: los disparos no lo golpearian.");

            PrefabUtility.SaveAsPrefabAsset(raiz, destino);
            hecho.Add("Prefab " + destino + " (sin EnemyFollow/Health; PhotonView + PhotonTransformView + BloomNet)");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    // NavMeshSurface en la escena con la geometria de colliders; techo y todo lo que este por encima de 2.5 m se ignora
    static void ConfigurarNavMesh(UnityEngine.SceneManagement.Scene escena, List<string> hecho, List<string> avisos)
    {
        // Altura del piso: el spawn mas bajo (los spawns estan en el piso)
        float piso = 0f;
        NetworkManager nm = Object.FindFirstObjectByType<NetworkManager>(FindObjectsInactive.Include);
        if (nm != null && nm.spawnPoints != null && nm.spawnPoints.Length > 0)
        {
            piso = float.MaxValue;
            for (int i = 0; i < nm.spawnPoints.Length; i++)
            {
                if (nm.spawnPoints[i] != null && nm.spawnPoints[i].position.y < piso) piso = nm.spawnPoints[i].position.y;
            }
            if (piso == float.MaxValue) piso = 0f;
        }
        else
        {
            avisos.Add("No se encontro NetworkManager con spawns: se asume el piso en y = 0.");
        }
        float limite = piso + AlturaMaximaNavMesh;

        // Ignorar: objetos llamados Ceiling/Techo y cualquier collider que empiece por encima del limite
        int ignorados = 0;
        foreach (Transform t in TodosLosTransforms(escena))
        {
            string n = t.name.ToLowerInvariant();
            bool porNombre = n == "ceiling" || n.StartsWith("techo");
            bool porAltura = false;
            Collider c = t.GetComponent<Collider>();
            if (c != null && c.enabled && c.bounds.min.y > limite) porAltura = true;
            if (!porNombre && !porAltura) continue;

            Unity.AI.Navigation.NavMeshModifier mod = t.GetComponent<Unity.AI.Navigation.NavMeshModifier>();
            if (mod == null) mod = t.gameObject.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
            mod.ignoreFromBuild = true;
            mod.applyToChildren = true;
            EditorUtility.SetDirty(mod);
            ignorados++;
        }
        hecho.Add("NavMeshModifier (Ignore From Build) en " + ignorados + " objetos: techo y todo lo que empieza por encima de " + limite.ToString("0.0") + " m");

        // Surface (idempotente: se reutiliza el objeto si ya existe)
        Unity.AI.Navigation.NavMeshSurface surface = Object.FindFirstObjectByType<Unity.AI.Navigation.NavMeshSurface>(FindObjectsInactive.Include);
        if (surface == null)
        {
            GameObject go = new GameObject("NavMeshSurfaceCoop");
            surface = go.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
        }
        surface.collectObjects = Unity.AI.Navigation.CollectObjects.All;
        surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders; // los paneles del laboratorio son solo visuales
        surface.layerMask = ~0;
        surface.defaultArea = 0;

        // Hornear y guardar los datos como asset junto a la escena (si no, se pierden al recargar)
        AsegurarCarpeta(DirNavMesh);
        surface.RemoveData();
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RutaNavMeshData) != null) AssetDatabase.DeleteAsset(RutaNavMeshData);
        surface.BuildNavMesh();
        if (surface.navMeshData == null)
        {
            avisos.Add("El horneado del NavMesh no genero datos: revisa que haya colliders de piso en la escena.");
        }
        else
        {
            AssetDatabase.CreateAsset(surface.navMeshData, RutaNavMeshData);
            surface.navMeshData = AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(RutaNavMeshData);
            AssetDatabase.SaveAssets();
            hecho.Add("NavMeshSurface horneado (colliders fisicos) y guardado en " + RutaNavMeshData);
        }
        EditorUtility.SetDirty(surface);
    }

    // =====================================================================
    //  Utilidades de escena
    // =====================================================================
    static GameObject CargarPrefabLab(string nombre)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(DirPrefabsMP + "/" + nombre + ".prefab");
    }

    static GameObject Instanciar(GameObject prefab, Transform padre)
    {
        return (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
    }

    static Transform NuevoGrupo(Transform padre, string nombre)
    {
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(padre, false);
        return g.transform;
    }

    static IEnumerable<Transform> TodosLosTransforms(UnityEngine.SceneManagement.Scene escena)
    {
        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true)) yield return t;
        }
    }

    static Transform BuscarEnEscena(UnityEngine.SceneManagement.Scene escena, string nombre)
    {
        foreach (Transform t in TodosLosTransforms(escena))
        {
            if (t.name == nombre) return t;
        }
        return null;
    }

    // Primer objeto con ese nombre que NO cuelga de 'excluir'
    static Transform BuscarEnEscenaFueraDe(UnityEngine.SceneManagement.Scene escena, string nombre, Transform excluir)
    {
        foreach (Transform t in TodosLosTransforms(escena))
        {
            if (t.name != nombre) continue;
            if (excluir != null && (t == excluir || t.IsChildOf(excluir))) continue;
            return t;
        }
        return null;
    }

    static void DestruirPorNombre(UnityEngine.SceneManagement.Scene escena, string nombre)
    {
        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            if (raiz.name == nombre) Object.DestroyImmediate(raiz);
        }
    }

    static void LimpiarSpawnsGenerados(Transform environment)
    {
        if (environment == null) return;
        Transform raiz = environment.Find("SpawnPoint");
        if (raiz == null) return;
        for (int i = raiz.childCount - 1; i >= 0; i--)
        {
            if (raiz.GetChild(i).name.StartsWith("SpawnMP_")) Object.DestroyImmediate(raiz.GetChild(i).gameObject);
        }
    }

    static Transform BuscarPorNombre(Transform raiz, string nombre)
    {
        foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == nombre) return t;
        }
        return null;
    }

    static bool BoundsDe(GameObject go, out Bounds b)
    {
        b = new Bounds(go.transform.position, Vector3.zero);
        bool hay = false;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
        {
            if (!hay) { b = r.bounds; hay = true; }
            else b.Encapsulate(r.bounds);
        }
        return hay;
    }

    // Puntos del piso (rejilla de 'paso' m) con un volumen libre de colliders: esfera de 'radioLibre' m a 1.3 m de altura
    static List<Vector3> CeldasLibres(Bounds mapa, float suelo, float paso, float margen, float radioLibre)
    {
        List<Vector3> r = new List<Vector3>();
        for (float x = mapa.min.x + margen; x <= mapa.max.x - margen; x += paso)
        {
            for (float z = mapa.min.z + margen; z <= mapa.max.z - margen; z += paso)
            {
                Vector3 centro = new Vector3(x, suelo + 1.3f, z);
                if (!Physics.CheckSphere(centro, radioLibre, Physics.AllLayers, QueryTriggerInteraction.Ignore))
                {
                    r.Add(new Vector3(x, suelo, z));
                }
            }
        }
        return r;
    }

    // Muestreo por "punto mas lejano": cada punto nuevo maximiza su distancia al mas cercano de los ya elegidos
    static List<Vector3> ElegirDispersos(List<Vector3> candidatos, List<Vector3> existentes, int cuantos, float distanciaMinima, Vector3 centroMapa)
    {
        List<Vector3> elegidos = new List<Vector3>();
        List<Vector3> referencia = new List<Vector3>(existentes);
        for (int n = 0; n < cuantos; n++)
        {
            int mejor = -1;
            float mejorD = -1f;
            for (int i = 0; i < candidatos.Count; i++)
            {
                Vector3 c = candidatos[i];
                float d;
                if (referencia.Count == 0)
                {
                    float dx0 = c.x - centroMapa.x, dz0 = c.z - centroMapa.z;
                    d = Mathf.Sqrt(dx0 * dx0 + dz0 * dz0);
                }
                else
                {
                    d = float.MaxValue;
                    for (int j = 0; j < referencia.Count; j++)
                    {
                        float dx = c.x - referencia[j].x, dz = c.z - referencia[j].z;
                        float dd = Mathf.Sqrt(dx * dx + dz * dz);
                        if (dd < d) d = dd;
                    }
                }
                if (d < distanciaMinima) continue;
                if (d > mejorD) { mejorD = d; mejor = i; }
            }
            if (mejor < 0) break;
            elegidos.Add(candidatos[mejor]);
            referencia.Add(candidatos[mejor]);
        }
        return elegidos;
    }

    // ------------------------------------------------------------ Utilidades generales
    static string RutaAbsoluta(string rutaAssets)
    {
        string proyecto = Path.GetDirectoryName(Application.dataPath);
        return Path.Combine(proyecto, rutaAssets);
    }

    // Copia el archivo a Backups/ (en la raiz del proyecto, fuera de Assets) con fecha y hora
    static string Respaldar(string rutaAssets)
    {
        string proyecto = Path.GetDirectoryName(Application.dataPath);
        string carpeta = Path.Combine(proyecto, "Backups");
        Directory.CreateDirectory(carpeta);
        string nombre = Path.GetFileNameWithoutExtension(rutaAssets) + "_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + Path.GetExtension(rutaAssets);
        string destino = Path.Combine(carpeta, nombre);
        File.Copy(RutaAbsoluta(rutaAssets), destino, true);
        return destino;
    }

    static string LimpiarNombreArchivo(string nombre)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) nombre = nombre.Replace(c, '_');
        return nombre;
    }

    static void AsegurarCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        string padre = Path.GetDirectoryName(ruta).Replace('\\', '/');
        string nombre = Path.GetFileName(ruta);
        AsegurarCarpeta(padre);
        AssetDatabase.CreateFolder(padre, nombre);
    }

    static void Resumen(string titulo, List<string> hecho, List<string> avisos, string siguiente)
    {
        foreach (string h in hecho) Log("OK: " + h);
        foreach (string a in avisos) Warn(a);

        string msg = titulo + " terminado.\n\nHecho:\n- " + string.Join("\n- ", hecho.ToArray());
        if (avisos.Count > 0)
        {
            msg += "\n\nAvisos (" + avisos.Count + "):\n- " + string.Join("\n- ", avisos.ToArray());
        }
        msg += "\n\n" + siguiente;
        EditorUtility.DisplayDialog("DarkShot - " + titulo, msg, "OK");
    }

    static void Log(string m) { Debug.Log("[DarkShot] " + m); }
    static void Warn(string m) { Debug.LogWarning("[DarkShot] " + m); }
}
