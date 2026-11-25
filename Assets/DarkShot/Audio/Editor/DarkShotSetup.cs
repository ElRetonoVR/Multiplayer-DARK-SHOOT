using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Herramienta de editor de DARK SHOT.
//   DarkShot/1. Importar y crear prefabs
//   DarkShot/2. Configurar escena Arena
//   DarkShot/3. Colocar ramas de ejemplo
// Todos los pasos son idempotentes: se pueden correr varias veces sin duplicar nada.
public static class DarkShotSetup
{
    // ---------------------------------------------------------------- Rutas
    const string Raiz = "Assets/DarkShot";
    const string DirModelos = Raiz + "/Models";
    const string DirMateriales = Raiz + "/Materials";
    const string DirAnimacion = Raiz + "/Animation";
    const string DirPrefabs = Raiz + "/Prefabs";
    const string DirAudio = Raiz + "/Audio";

    const string FbxBase = DirModelos + "/Bloom_Base.fbx";
    const string FbxLabCoat = DirModelos + "/Bloom_LabCoat.fbx";
    const string FbxEngineer = DirModelos + "/Bloom_Engineer.fbx";
    const string FbxFace = DirModelos + "/Bloom_Face.fbx";
    const string FbxBattery = DirModelos + "/Battery.fbx";
    const string FbxStation = DirModelos + "/BatteryStation.fbx";
    const string FbxBranchWallA = DirModelos + "/Branch_Wall_A.fbx";
    const string FbxBranchWallB = DirModelos + "/Branch_Wall_B.fbx";
    const string FbxBranchFloor = DirModelos + "/Branch_Floor.fbx";

    const string RutaControlador = DirAnimacion + "/Bloom.controller";
    const string RutaCapsule = "Assets/Prefab/Capsule.prefab";
    const string RutaArena = "Assets/Scenes/Arena.unity";

    const string NombreModeloEstacion = "BatteryStation_Model";
    const string NombreCara = "Bloom_Face";

    // Si al activarse el susto ves la nuca de la cara en vez del rostro, cambia a 180.
    // (El script JumpscareBloom orienta el +Z del objeto hacia la camara.)
    const float GiroYCara = 0f;
    const float OffsetCara = 0.12f;

    // Ramas: estaticas para batching/occlusion, pero NO NavigationStatic (no deben tocar el NavMesh)
    // ni ContributeGI (no obligan a re-hornear luces).
    const StaticEditorFlags FlagsRamas =
        StaticEditorFlags.BatchingStatic |
        StaticEditorFlags.OccluderStatic |
        StaticEditorFlags.OccludeeStatic |
        StaticEditorFlags.ReflectionProbeStatic;

    static readonly string[] ClipsBloom = { "Idle", "Walk", "Attack" };

    // ----------------------------------------------------------- Materiales
    class DefMaterial
    {
        public string nombre;
        public string hex;
        public float metalico;
        public float suavidad;
        public string hexEmision;     // null = sin emision
        public float intensidadEmision;

        public DefMaterial(string nombre, string hex, float metalico, float suavidad, string hexEmision = null, float intensidadEmision = 0f)
        {
            this.nombre = nombre;
            this.hex = hex;
            this.metalico = metalico;
            this.suavidad = suavidad;
            this.hexEmision = hexEmision;
            this.intensidadEmision = intensidadEmision;
        }
    }

    static readonly DefMaterial[] DefMateriales =
    {
        new DefMaterial("M_BloomSkin",     "#C9B8A8", 0f,   0.35f),
        new DefMaterial("M_BloomVeins",    "#5A1E2A", 0f,   0.30f, "#5A1E2A", 0.8f),   // emisivo tenue
        new DefMaterial("M_BloomEyes",     "#E8E4D0", 0f,   0.75f),
        new DefMaterial("M_BloomMouth",    "#3A0A10", 0f,   0.55f),
        new DefMaterial("M_Coat",          "#D8D8D0", 0f,   0.15f),
        new DefMaterial("M_EngineerSuit",  "#C25A1A", 0f,   0.20f),
        new DefMaterial("M_Helmet",        "#D9B21E", 0.1f, 0.50f),
        new DefMaterial("M_Battery",       "#2A2E33", 0.6f, 0.50f),
        new DefMaterial("M_BatteryGlow",   "#3CFF9A", 0f,   0.50f, "#3CFF9A", 2.5f),   // emisivo fuerte
        new DefMaterial("M_Station",       "#3B3F44", 0.5f, 0.45f),
        new DefMaterial("M_Branch",        "#1E1418", 0f,   0.25f),
        new DefMaterial("M_BranchBulb",    "#7A2A3A", 0f,   0.40f, "#7A2A3A", 0.7f),   // emisivo tenue
    };

    enum TipoModelo { ConAnimacion, Riggeado, Estatico }

    // =====================================================================
    //  MENU 1: Importar y crear prefabs
    // =====================================================================
    [MenuItem("DarkShot/1. Importar y crear prefabs")]
    public static void ImportarYCrearPrefabs()
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
            Log("Paso 1: importar modelos y crear prefabs...");
            AssetDatabase.Refresh();

            AsegurarCarpeta(DirModelos);
            AsegurarCarpeta(DirMateriales);
            AsegurarCarpeta(DirAnimacion);
            AsegurarCarpeta(DirPrefabs);
            AsegurarCarpeta(DirAudio);

            // 1. Materiales URP Lit
            Dictionary<string, Material> mats = CrearMateriales(avisos);
            if (mats == null)
            {
                EditorUtility.DisplayDialog("DarkShot", "No se encontro el shader 'Universal Render Pipeline/Lit'. Revisa que URP este instalado y activo.", "OK");
                return;
            }
            hecho.Add("Materiales URP Lit: " + mats.Count + " en " + DirMateriales);

            // 2. Importadores (Bloom_Base primero: de el sale el Avatar)
            bool hayBase = ConfigurarModelo(FbxBase, TipoModelo.ConAnimacion, mats, null, avisos);
            Avatar avatarBase = hayBase ? CargarAvatar(FbxBase) : null;
            if (hayBase && avatarBase == null)
            {
                avisos.Add("Bloom_Base.fbx no genero un Avatar. Revisa que tenga el armature 'BloomRig'.");
            }

            bool hayLabCoat = ConfigurarModelo(FbxLabCoat, TipoModelo.Riggeado, mats, avatarBase, avisos);
            bool hayEngineer = ConfigurarModelo(FbxEngineer, TipoModelo.Riggeado, mats, avatarBase, avisos);
            bool hayFace = ConfigurarModelo(FbxFace, TipoModelo.Estatico, mats, null, avisos);
            bool hayBattery = ConfigurarModelo(FbxBattery, TipoModelo.Estatico, mats, null, avisos);
            bool hayStation = ConfigurarModelo(FbxStation, TipoModelo.Estatico, mats, null, avisos);
            bool hayWallA = ConfigurarModelo(FbxBranchWallA, TipoModelo.Estatico, mats, null, avisos);
            bool hayWallB = ConfigurarModelo(FbxBranchWallB, TipoModelo.Estatico, mats, null, avisos);
            bool hayFloor = ConfigurarModelo(FbxBranchFloor, TipoModelo.Estatico, mats, null, avisos);
            hecho.Add("ModelImporter configurados (Generic, clips Idle/Walk loop, Attack sin loop, materiales remapeados)");

            // 3. Animator Controller
            AnimatorController controlador = null;
            if (hayBase)
            {
                controlador = CrearControlador(avisos);
                hecho.Add("Animator Controller: " + RutaControlador);
            }

            // 4. Prefabs
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaCapsule) == null)
            {
                avisos.Add("No existe " + RutaCapsule + ": no se pueden crear los prefabs de Bloom.");
            }
            else if (controlador != null)
            {
                if (hayBase && ConstruirBloom("Bloom_Base", FbxBase, controlador, avatarBase, mats, avisos)) hecho.Add("Prefab Bloom_Base");
                if (hayLabCoat && ConstruirBloom("Bloom_LabCoat", FbxLabCoat, controlador, avatarBase, mats, avisos)) hecho.Add("Prefab Bloom_LabCoat");
                if (hayEngineer && ConstruirBloom("Bloom_Engineer", FbxEngineer, controlador, avatarBase, mats, avisos)) hecho.Add("Prefab Bloom_Engineer");
            }

            if (hayFace && ConstruirCara(mats, avisos)) hecho.Add("Prefab Bloom_Face (modelo + luz propia para verse a oscuras)");
            if (hayBattery && ConstruirSimple("Battery", FbxBattery, mats, false, null, avisos)) hecho.Add("Prefab Battery");
            if (hayStation && ConstruirSimple("BatteryStation", FbxStation, mats, false, ConfigurarEstacion, avisos)) hecho.Add("Prefab BatteryStation (trigger + BatteryStationSingle)");
            if (hayWallA && ConstruirSimple("Branch_Wall_A", FbxBranchWallA, mats, true, null, avisos)) hecho.Add("Prefab Branch_Wall_A");
            if (hayWallB && ConstruirSimple("Branch_Wall_B", FbxBranchWallB, mats, true, null, avisos)) hecho.Add("Prefab Branch_Wall_B");
            if (hayFloor && ConstruirSimple("Branch_Floor", FbxBranchFloor, mats, true, null, avisos)) hecho.Add("Prefab Branch_Floor");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        catch (System.Exception e)
        {
            Debug.LogError("[DarkShot] Error en el paso 1: " + e);
            EditorUtility.DisplayDialog("DarkShot", "El paso 1 fallo: " + e.Message + "\n\nRevisa la consola para ver el detalle.", "OK");
            return;
        }

        foreach (string h in hecho) Log("OK: " + h);
        foreach (string a in avisos) Warn(a);

        string msg = "Paso 1 terminado.\n\nCreado/actualizado:\n- " + string.Join("\n- ", hecho.ToArray());
        if (avisos.Count > 0)
        {
            msg += "\n\nAvisos (" + avisos.Count + "):\n- " + string.Join("\n- ", avisos.ToArray());
        }
        msg += "\n\nSiguiente: DarkShot/2. Configurar escena Arena";
        EditorUtility.DisplayDialog("DarkShot - Paso 1", msg, "OK");
    }

    // ------------------------------------------------------------ Materiales
    static Dictionary<string, Material> CrearMateriales(List<string> avisos)
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) return null;

        Dictionary<string, Material> resultado = new Dictionary<string, Material>();
        foreach (DefMaterial d in DefMateriales)
        {
            string ruta = DirMateriales + "/" + d.nombre + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(lit);
                AssetDatabase.CreateAsset(m, ruta);
            }
            else if (m.shader != lit)
            {
                m.shader = lit;
            }

            Color baseColor = ParsearColor(d.hex);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", baseColor);
            if (m.HasProperty("_Color")) m.SetColor("_Color", baseColor);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", d.metalico);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", d.suavidad);

            if (d.hexEmision != null)
            {
                Color emision = ParsearColor(d.hexEmision) * d.intensidadEmision;
                m.EnableKeyword("_EMISSION");
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emision);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }
            else
            {
                m.DisableKeyword("_EMISSION");
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }

            EditorUtility.SetDirty(m);
            resultado[d.nombre] = m;
        }
        AssetDatabase.SaveAssets();
        return resultado;
    }

    static Dictionary<string, Material> CargarMateriales()
    {
        Dictionary<string, Material> resultado = new Dictionary<string, Material>();
        foreach (DefMaterial d in DefMateriales)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(DirMateriales + "/" + d.nombre + ".mat");
            if (m != null) resultado[d.nombre] = m;
        }
        return resultado;
    }

    static Color ParsearColor(string hex)
    {
        Color c;
        if (ColorUtility.TryParseHtmlString(hex, out c)) return c;
        return Color.magenta;
    }

    // ------------------------------------------------------------- Importador
    // Devuelve false si el FBX no existe.
    static bool ConfigurarModelo(string ruta, TipoModelo tipo, Dictionary<string, Material> mats, Avatar avatarBase, List<string> avisos)
    {
        ModelImporter imp = AssetImporter.GetAtPath(ruta) as ModelImporter;
        if (imp == null)
        {
            avisos.Add("No se encontro " + ruta + " (copia el FBX ahi y vuelve a correr el paso 1).");
            return false;
        }

        // Materiales: nombres de Blender tal cual y remapeados a los materiales URP
        imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        // InPrefab (materiales embebidos) es el modo en el que Unity respeta los AddRemap;
        // con External los remapeos se ignoran y se extraen copias a una carpeta Materials.
        imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
        foreach (KeyValuePair<string, Material> kv in mats)
        {
            AssetImporter.SourceAssetIdentifier id = new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key);
            imp.RemoveRemap(id);
            imp.AddRemap(id, kv.Value);
        }

        switch (tipo)
        {
            case TipoModelo.ConAnimacion:
                imp.animationType = ModelImporterAnimationType.Generic;
                imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                imp.importAnimation = true;
                ConfigurarClips(imp, ruta, avisos);
                break;

            case TipoModelo.Riggeado:
                imp.animationType = ModelImporterAnimationType.Generic;
                imp.importAnimation = false;
                if (avatarBase != null)
                {
                    imp.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                    imp.sourceAvatar = avatarBase;
                }
                else
                {
                    imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    avisos.Add(ruta + ": no hay Avatar de Bloom_Base; se genero uno propio.");
                }
                break;

            default:
                imp.animationType = ModelImporterAnimationType.None;
                imp.importAnimation = false;
                break;
        }

        imp.SaveAndReimport();
        return true;
    }

    static void ConfigurarClips(ModelImporter imp, string ruta, List<string> avisos)
    {
        ModelImporterClipAnimation[] porDefecto = imp.defaultClipAnimations;
        List<ModelImporterClipAnimation> lista = new List<ModelImporterClipAnimation>();
        HashSet<string> usados = new HashSet<string>();

        foreach (ModelImporterClipAnimation clip in porDefecto)
        {
            string canon = NombreCanonico(clip.name);
            if (canon == null) canon = NombreCanonico(clip.takeName);
            if (canon == null || usados.Contains(canon)) continue;

            usados.Add(canon);
            clip.name = canon;
            bool loop = canon != "Attack";
            clip.loopTime = loop;
            clip.loopPose = loop;
            lista.Add(clip);
        }

        foreach (string esperado in ClipsBloom)
        {
            if (!usados.Contains(esperado)) avisos.Add(ruta + ": no se encontro el clip '" + esperado + "' (se busca un take cuyo nombre termine en eso).");
        }

        if (lista.Count > 0)
        {
            imp.clipAnimations = lista.ToArray();
        }
    }

    // "BloomRig|Walk" -> "Walk"; null si no es uno de los clips esperados
    static string NombreCanonico(string nombre)
    {
        if (string.IsNullOrEmpty(nombre)) return null;
        foreach (string c in ClipsBloom)
        {
            if (nombre.EndsWith(c, System.StringComparison.OrdinalIgnoreCase)) return c;
        }
        return null;
    }

    static Avatar CargarAvatar(string ruta)
    {
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(ruta))
        {
            Avatar a = o as Avatar;
            if (a != null) return a;
        }
        return null;
    }

    // ------------------------------------------------------ Animator Controller
    static AnimatorController CrearControlador(List<string> avisos)
    {
        AnimationClip idle = null, walk = null, attack = null;
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(FbxBase))
        {
            AnimationClip c = o as AnimationClip;
            if (c == null || c.name.StartsWith("__preview__")) continue;
            string canon = NombreCanonico(c.name);
            if (canon == "Idle" && idle == null) idle = c;
            else if (canon == "Walk" && walk == null) walk = c;
            else if (canon == "Attack" && attack == null) attack = c;
        }
        if (idle == null) avisos.Add("Bloom_Base.fbx no tiene clip Idle: el estado Idle quedo sin animacion.");
        if (walk == null) avisos.Add("Bloom_Base.fbx no tiene clip Walk: el estado Walk quedo sin animacion.");
        if (attack == null) avisos.Add("Bloom_Base.fbx no tiene clip Attack: el estado Attack quedo sin animacion.");

        AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador);
        if (ctrl == null)
        {
            ctrl = AnimatorController.CreateAnimatorControllerAtPath(RutaControlador);
        }

        // Limpiar lo anterior para que sea idempotente (se conserva el mismo asset y GUID)
        for (int i = ctrl.parameters.Length - 1; i >= 0; i--)
        {
            ctrl.RemoveParameter(i);
        }
        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        foreach (ChildAnimatorState cs in sm.states)
        {
            sm.RemoveState(cs.state);
        }
        foreach (AnimatorStateTransition t in sm.anyStateTransitions)
        {
            sm.RemoveAnyStateTransition(t);
        }

        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

        AnimatorState sIdle = sm.AddState("Idle", new Vector3(300f, 100f, 0f));
        AnimatorState sWalk = sm.AddState("Walk", new Vector3(300f, 200f, 0f));
        AnimatorState sAttack = sm.AddState("Attack", new Vector3(560f, 150f, 0f));
        sIdle.motion = idle;
        sWalk.motion = walk;
        sAttack.motion = attack;
        sm.defaultState = sIdle;

        AnimatorStateTransition idleAWalk = sIdle.AddTransition(sWalk);
        idleAWalk.hasExitTime = false;
        idleAWalk.hasFixedDuration = true;
        idleAWalk.duration = 0.1f;
        idleAWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        AnimatorStateTransition walkAIdle = sWalk.AddTransition(sIdle);
        walkAIdle.hasExitTime = false;
        walkAIdle.hasFixedDuration = true;
        walkAIdle.duration = 0.1f;
        walkAIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        AnimatorStateTransition anyAAttack = sm.AddAnyStateTransition(sAttack);
        anyAAttack.hasExitTime = false;
        anyAAttack.hasFixedDuration = true;
        anyAAttack.duration = 0.05f;
        anyAAttack.canTransitionToSelf = false;
        anyAAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

        AnimatorStateTransition attackAIdle = sAttack.AddTransition(sIdle);
        attackAIdle.hasExitTime = true;
        attackAIdle.exitTime = 0.9f;
        attackAIdle.hasFixedDuration = true;
        attackAIdle.duration = 0.1f;

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        return ctrl;
    }

    // ------------------------------------------------------------------ Prefabs
    // Bloom: parte del Capsule.prefab original (instancia desempaquetada) para conservar
    // NavMeshAgent, Rigidbody, CapsuleCollider, tag, layer, enemyNav y Health con sus valores.
    static bool ConstruirBloom(string nombre, string rutaFbx, AnimatorController ctrl, Avatar avatar, Dictionary<string, Material> mats, List<string> avisos)
    {
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(rutaFbx);
        GameObject capsule = AssetDatabase.LoadAssetAtPath<GameObject>(RutaCapsule);
        if (fbx == null || capsule == null)
        {
            avisos.Add("No se pudo crear " + nombre + " (falta " + (fbx == null ? rutaFbx : RutaCapsule) + ").");
            return false;
        }

        Scene vista = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject raiz = (GameObject)PrefabUtility.InstantiatePrefab(capsule, vista);
            PrefabUtility.UnpackPrefabInstance(raiz, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            raiz.name = nombre;
            raiz.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            raiz.transform.localScale = Vector3.one;

            // Quitar la capsula visual provisional (el renderer depende del filter: primero el renderer)
            MeshRenderer mr = raiz.GetComponent<MeshRenderer>();
            if (mr != null) Object.DestroyImmediate(mr);
            MeshFilter mf = raiz.GetComponent<MeshFilter>();
            if (mf != null) Object.DestroyImmediate(mf);

            // Collider ~1 m de alto, origen en los pies. El radio se queda en 0.5 m (mismo ancho de contacto
            // que el original: el Capsule era radio 1 * escala 0.5) para que el golpe al jugador siga funcionando.
            CapsuleCollider col = raiz.GetComponent<CapsuleCollider>();
            if (col == null) col = raiz.AddComponent<CapsuleCollider>();
            col.direction = 1;
            col.radius = 0.5f;
            col.height = 1.0f;
            col.center = new Vector3(0f, 0.5f, 0f);

            // El NavMeshAgent se queda igual (velocidad, aceleracion, stopping distance...),
            // salvo la geometria: origen en los pies, 1 m de alto.
            NavMeshAgent agente = raiz.GetComponent<NavMeshAgent>();
            if (agente == null) agente = raiz.AddComponent<NavMeshAgent>();
            agente.baseOffset = 0f;
            agente.height = 1.0f;
            agente.radius = 0.5f;

            // Modelo como hijo con Animator
            GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(fbx, raiz.transform);
            modelo.name = "Model";
            modelo.transform.localPosition = Vector3.zero;
            // Se conserva la rotacion de la raiz del FBX: el exportador de Blender deja -90 en X ahi (modelos de una malla).
            modelo.transform.localRotation = fbx.transform.localRotation;
            modelo.transform.localScale = Vector3.one;
            foreach (Collider c in modelo.GetComponentsInChildren<Collider>(true)) c.enabled = false;

            Animator anim = modelo.GetComponent<Animator>();
            if (anim == null) anim = modelo.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;
            if (avatar != null) anim.avatar = avatar;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            AplicarMateriales(modelo, mats, nombre, avisos);

            BloomAnimator ba = raiz.GetComponent<BloomAnimator>();
            if (ba == null) ba = raiz.AddComponent<BloomAnimator>();
            ba.animator = anim;
            ba.agent = agente;

            return GuardarPrefab(raiz, nombre, avisos);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(vista);
        }
    }

    // Cara del susto: modelo + luz puntual propia (JumpscareBloom apaga todas las demas luces).
    static bool ConstruirCara(Dictionary<string, Material> mats, List<string> avisos)
    {
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(FbxFace);
        if (fbx == null)
        {
            avisos.Add("No se pudo crear Bloom_Face (falta " + FbxFace + ").");
            return false;
        }

        Scene vista = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject raiz = new GameObject(NombreCara);
            SceneManager.MoveGameObjectToScene(raiz, vista);

            GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(fbx, raiz.transform);
            modelo.name = "Modelo";
            // El rostro sobresale ~0.27 m (y la lengua ~0.35 m) por delante del origen de Bloom_Face.fbx.
            // JumpscareBloom pone la raiz a 0.35 m de los ojos, asi que se aleja el modelo 0.12 m:
            // la cara queda a ~0.20 m y la punta de la lengua a ~0.12 m, sin cortarse con el near plane.
            modelo.transform.localPosition = new Vector3(0f, 0f, -OffsetCara);
            // Cara: Se conserva la rotacion de la raiz del FBX: el exportador de Blender deja -90 en X ahi (modelos de una malla).
            modelo.transform.localRotation = Quaternion.Euler(0f, GiroYCara, 0f) * fbx.transform.localRotation;
            modelo.transform.localScale = Vector3.one;

            foreach (Collider c in modelo.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            foreach (Renderer r in modelo.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            AplicarMateriales(modelo, mats, NombreCara, avisos);

            // Luz propia: el +Z de la raiz apunta hacia la camara, la luz se pone un poco delante de la cara
            GameObject luzObj = new GameObject("LuzCara");
            luzObj.transform.SetParent(raiz.transform, false);
            luzObj.transform.localPosition = new Vector3(0f, 0.1f, 0.3f);
            Light luz = luzObj.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = new Color(1f, 0.82f, 0.75f);
            luz.intensity = 1.2f; // a 0.35 m de los ojos, 3 dejaba la piel palida quemada en blanco
            luz.range = 1.5f;
            luz.shadows = LightShadows.None;

            return GuardarPrefab(raiz, NombreCara, avisos);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(vista);
        }
    }

    // Prefab simple: raiz vacia + modelo FBX como hijo "Model". 'extra' puede agregar componentes a la raiz.
    static bool ConstruirSimple(string nombre, string rutaFbx, Dictionary<string, Material> mats, bool estatico, System.Action<GameObject> extra, List<string> avisos)
    {
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(rutaFbx);
        if (fbx == null)
        {
            avisos.Add("No se pudo crear " + nombre + " (falta " + rutaFbx + ").");
            return false;
        }

        Scene vista = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject raiz = new GameObject(nombre);
            SceneManager.MoveGameObjectToScene(raiz, vista);

            GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(fbx, raiz.transform);
            modelo.name = "Model";
            modelo.transform.localPosition = Vector3.zero;
            // Se conserva la rotacion de la raiz del FBX: el exportador de Blender deja -90 en X ahi (modelos de una malla).
            modelo.transform.localRotation = fbx.transform.localRotation;
            modelo.transform.localScale = Vector3.one;

            // Sin colliders en el modelo (las ramas son solo decoracion)
            foreach (Collider c in modelo.GetComponentsInChildren<Collider>(true)) c.enabled = false;

            AplicarMateriales(modelo, mats, nombre, avisos);

            if (extra != null) extra(raiz);

            if (estatico)
            {
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, FlagsRamas);
                }
            }

            return GuardarPrefab(raiz, nombre, avisos);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(vista);
        }
    }

    // Misma logica que la estacion actual de la escena: trigger + BatteryStationSingle
    static void ConfigurarEstacion(GameObject raiz)
    {
        if (raiz.GetComponent<BatteryStationSingle>() == null) raiz.AddComponent<BatteryStationSingle>();
        BoxCollider caja = raiz.GetComponent<BoxCollider>();
        if (caja == null) caja = raiz.AddComponent<BoxCollider>();
        caja.isTrigger = true;
        caja.size = new Vector3(1.2f, 1.0f, 1.2f);
        caja.center = new Vector3(0f, 0.5f, 0f);
    }

    static bool GuardarPrefab(GameObject raiz, string nombre, List<string> avisos)
    {
        string ruta = DirPrefabs + "/" + nombre + ".prefab";
        bool ok;
        PrefabUtility.SaveAsPrefabAsset(raiz, ruta, out ok);
        if (!ok)
        {
            avisos.Add("No se pudo guardar el prefab " + ruta);
            return false;
        }
        return true;
    }

    // Respaldo del remapeo del importador: si algun renderer quedo con un material que no es el URP
    // (por ejemplo "Bloom_Base-M_BloomSkin" o "M_BloomSkin.001"), se reemplaza por nombre.
    static void AplicarMateriales(GameObject modelo, Dictionary<string, Material> mats, string contexto, List<string> avisos)
    {
        HashSet<string> desconocidos = new HashSet<string>();
        foreach (Renderer r in modelo.GetComponentsInChildren<Renderer>(true))
        {
            Material[] actuales = r.sharedMaterials;
            bool cambio = false;
            for (int i = 0; i < actuales.Length; i++)
            {
                Material m = actuales[i];
                if (m == null) continue;
                Material destino = BuscarMaterial(m.name, mats);
                if (destino == null)
                {
                    desconocidos.Add(m.name);
                }
                else if (destino != m)
                {
                    actuales[i] = destino;
                    cambio = true;
                }
            }
            if (cambio) r.sharedMaterials = actuales;
        }
        if (desconocidos.Count > 0)
        {
            string[] arr = new string[desconocidos.Count];
            desconocidos.CopyTo(arr);
            avisos.Add(contexto + ": materiales sin equivalente en la lista (se dejaron como vienen): " + string.Join(", ", arr));
        }
    }

    // El nombre mas largo contenido gana (M_BatteryGlow antes que M_Battery)
    static Material BuscarMaterial(string nombre, Dictionary<string, Material> mats)
    {
        Material mejor = null;
        int largo = -1;
        foreach (KeyValuePair<string, Material> kv in mats)
        {
            if (nombre.Contains(kv.Key) && kv.Key.Length > largo)
            {
                mejor = kv.Value;
                largo = kv.Key.Length;
            }
        }
        return mejor;
    }

    // =====================================================================
    //  MENU 2: Configurar escena Arena
    // =====================================================================
    [MenuItem("DarkShot/2. Configurar escena Arena")]
    public static void ConfigurarEscenaArena()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("DarkShot", "Sal del modo Play antes de ejecutar este paso.", "OK");
            return;
        }
        if (!File.Exists(RutaArena))
        {
            EditorUtility.DisplayDialog("DarkShot", "No se encontro " + RutaArena, "OK");
            return;
        }

        GameObject pBase = AssetDatabase.LoadAssetAtPath<GameObject>(DirPrefabs + "/Bloom_Base.prefab");
        GameObject pLab = AssetDatabase.LoadAssetAtPath<GameObject>(DirPrefabs + "/Bloom_LabCoat.prefab");
        GameObject pEng = AssetDatabase.LoadAssetAtPath<GameObject>(DirPrefabs + "/Bloom_Engineer.prefab");
        GameObject pCara = AssetDatabase.LoadAssetAtPath<GameObject>(DirPrefabs + "/" + NombreCara + ".prefab");
        GameObject fbxEstacion = AssetDatabase.LoadAssetAtPath<GameObject>(FbxStation);

        List<GameObject> variantes = new List<GameObject>();
        if (pBase != null) variantes.Add(pBase);
        if (pLab != null) variantes.Add(pLab);
        if (pEng != null) variantes.Add(pEng);
        if (variantes.Count == 0)
        {
            EditorUtility.DisplayDialog("DarkShot", "No hay prefabs de Bloom. Corre primero 'DarkShot/1. Importar y crear prefabs'.", "OK");
            return;
        }

        Scene escena = EditorSceneManager.GetActiveScene();
        if (escena.path != RutaArena)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            escena = EditorSceneManager.OpenScene(RutaArena, OpenSceneMode.Single);
        }

        List<string> hecho = new List<string>();
        List<string> avisos = new List<string>();

        try
        {
            Log("Paso 2: configurando " + RutaArena + "...");

            // ---- SpawnEnemy
            SpawnEnemy spawn = Object.FindFirstObjectByType<SpawnEnemy>(FindObjectsInactive.Include);
            if (spawn == null)
            {
                avisos.Add("No se encontro SpawnEnemy en la escena.");
            }
            else
            {
                SerializedObject so = new SerializedObject(spawn);
                SerializedProperty arr = so.FindProperty("variantesEnemigo");
                SerializedProperty prefabClasico = so.FindProperty("enemyPrefab");
                if (arr == null)
                {
                    avisos.Add("SpawnEnemy no tiene el campo 'variantesEnemigo': copia el SpawnEnemy.cs modificado y vuelve a correr el paso 2.");
                }
                else
                {
                    arr.arraySize = variantes.Count;
                    for (int i = 0; i < variantes.Count; i++)
                    {
                        arr.GetArrayElementAtIndex(i).objectReferenceValue = variantes[i];
                    }
                    // enemyPrefab tambien apunta a un Bloom, por si alguien vacia el arreglo
                    if (prefabClasico != null && pBase != null) prefabClasico.objectReferenceValue = pBase;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(spawn);
                    hecho.Add("SpawnEnemy: " + variantes.Count + " variantes de Bloom asignadas");
                }
            }

            // ---- JumpscareBloom
            JumpscareBloom jb = Object.FindFirstObjectByType<JumpscareBloom>(FindObjectsInactive.Include);
            if (jb == null)
            {
                avisos.Add("No se encontro JumpscareBloom en la escena.");
            }
            else
            {
                ConfigurarSusto(jb, pCara, hecho, avisos);
            }

            // ---- Estaciones de baterias
            if (fbxEstacion == null)
            {
                avisos.Add("Falta " + FbxStation + ": no se cambio el modelo de la estacion (corre el paso 1 con los FBX en su sitio).");
            }
            else
            {
                BatteryStationSingle[] estaciones = Object.FindObjectsByType<BatteryStationSingle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Dictionary<string, Material> mats = CargarMateriales();
                int n = 0;
                foreach (BatteryStationSingle est in estaciones)
                {
                    if (ReemplazarModeloEstacion(est, fbxEstacion, mats, avisos)) n++;
                }
                if (estaciones.Length == 0) avisos.Add("No se encontro ninguna BatteryStationSingle en la escena.");
                else hecho.Add("Estaciones de baterias con modelo nuevo: " + n + " de " + estaciones.Length);
            }

            EditorSceneManager.MarkSceneDirty(escena);
            bool guardada = EditorSceneManager.SaveScene(escena);
            if (guardada) hecho.Add("Escena guardada: " + RutaArena);
            else avisos.Add("No se pudo guardar la escena automaticamente: guardala con Ctrl+S.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[DarkShot] Error en el paso 2: " + e);
            EditorUtility.DisplayDialog("DarkShot", "El paso 2 fallo: " + e.Message + "\n\nRevisa la consola para ver el detalle.", "OK");
            return;
        }

        foreach (string h in hecho) Log("OK: " + h);
        foreach (string a in avisos) Warn(a);

        string msg = "Paso 2 terminado.\n\nHecho:\n- " + string.Join("\n- ", hecho.ToArray());
        if (avisos.Count > 0)
        {
            msg += "\n\nAvisos (" + avisos.Count + "):\n- " + string.Join("\n- ", avisos.ToArray());
        }
        msg += "\n\nOpcional: DarkShot/3. Colocar ramas de ejemplo";
        EditorUtility.DisplayDialog("DarkShot - Paso 2", msg, "OK");
    }

    static void ConfigurarSusto(JumpscareBloom jb, GameObject prefabCara, List<string> hecho, List<string> avisos)
    {
        if (prefabCara == null)
        {
            avisos.Add("No existe el prefab Bloom_Face: no se reemplazo el Quad provisional del susto.");
        }
        else
        {
            // Quitar el Quad provisional (o una cara de una corrida anterior), solo si cuelgan de este objeto
            List<GameObject> viejos = new List<GameObject>();
            if (jb.caraBloom != null && jb.caraBloom.transform.IsChildOf(jb.transform) && jb.caraBloom != jb.gameObject)
            {
                viejos.Add(jb.caraBloom);
            }
            foreach (Transform hijo in jb.transform)
            {
                if ((hijo.name == NombreCara || hijo.name.StartsWith("CaraBloom")) && !viejos.Contains(hijo.gameObject))
                {
                    viejos.Add(hijo.gameObject);
                }
            }
            foreach (GameObject v in viejos)
            {
                Undo.DestroyObjectImmediate(v);
            }

            GameObject cara = (GameObject)PrefabUtility.InstantiatePrefab(prefabCara, jb.transform);
            Undo.RegisterCreatedObjectUndo(cara, "DarkShot cara");
            cara.name = NombreCara;
            cara.transform.localPosition = Vector3.zero;
            cara.transform.localRotation = Quaternion.identity;
            cara.transform.localScale = Vector3.one;
            foreach (Collider c in cara.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            cara.SetActive(false);

            SerializedObject so = new SerializedObject(jb);
            SerializedProperty p = so.FindProperty("caraBloom");
            p.objectReferenceValue = cara;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(jb);
            hecho.Add("JumpscareBloom: Quad provisional reemplazado por Bloom_Face (desactivada)");
        }

        // Audio del susto: Assets/DarkShot/Audio/Bloom_Breath*
        AudioClip clip = BuscarAudioSusto();
        if (jb.sonidoSusto == null)
        {
            avisos.Add("JumpscareBloom no tiene AudioSource asignado en 'sonidoSusto'.");
        }
        else if (clip == null)
        {
            avisos.Add("No hay audio del susto: pon un archivo 'Bloom_Breath*' (wav/mp3/ogg) en " + DirAudio + " y vuelve a correr el paso 2.");
        }
        else
        {
            AudioSource fuente = jb.sonidoSusto;
            bool clipAjeno = fuente.clip != null && !fuente.clip.name.StartsWith("Bloom_Breath");
            if (clipAjeno)
            {
                avisos.Add("El AudioSource del susto ya tenia otro clip (" + fuente.clip.name + "); no se cambio.");
            }
            else
            {
                Undo.RecordObject(fuente, "DarkShot audio susto");
                fuente.clip = clip;
                fuente.playOnAwake = false;
                EditorUtility.SetDirty(fuente);
                hecho.Add("Audio del susto asignado: " + clip.name);
            }
        }
    }

    static AudioClip BuscarAudioSusto()
    {
        if (!AssetDatabase.IsValidFolder(DirAudio)) return null;
        string[] guids = AssetDatabase.FindAssets("Bloom_Breath t:AudioClip", new[] { DirAudio });
        foreach (string guid in guids)
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
            if (clip != null) return clip;
        }
        return null;
    }

    // Mantiene el GameObject de la estacion (BatteryStationSingle + trigger) y solo cambia lo visual.
    static bool ReemplazarModeloEstacion(BatteryStationSingle est, GameObject fbx, Dictionary<string, Material> mats, List<string> avisos)
    {
        Transform t = est.transform;

        // Quitar el modelo de una corrida anterior
        List<GameObject> previos = new List<GameObject>();
        foreach (Transform hijo in t)
        {
            if (hijo.name == NombreModeloEstacion) previos.Add(hijo.gameObject);
        }
        foreach (GameObject p in previos) Undo.DestroyObjectImmediate(p);

        // Base del modelo viejo (cilindro aplastado) para apoyar el nuevo en el mismo sitio
        Vector3 pie = t.position;
        MeshRenderer viejo = est.GetComponent<MeshRenderer>();
        if (viejo != null)
        {
            Bounds b = viejo.bounds;
            pie = new Vector3(b.center.x, b.min.y, b.center.z);
        }

        GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(fbx, t);
        if (modelo == null)
        {
            avisos.Add("No se pudo instanciar " + FbxStation + " en " + est.name);
            return false;
        }
        Undo.RegisterCreatedObjectUndo(modelo, "DarkShot estacion");
        modelo.name = NombreModeloEstacion;
        // Se conserva la rotacion de la raiz del FBX: el exportador de Blender deja -90 en X ahi (modelos de una malla).
        modelo.transform.localRotation = fbx.transform.localRotation;
        modelo.transform.localScale = Vector3.one;
        // La estacion original esta aplastada (escala Y ~0.03): se compensa para que el modelo salga 1:1
        Vector3 s = modelo.transform.lossyScale;
        modelo.transform.localScale = new Vector3(
            s.x > 0.0001f ? 1f / s.x : 1f,
            s.y > 0.0001f ? 1f / s.y : 1f,
            s.z > 0.0001f ? 1f / s.z : 1f);
        modelo.transform.position = pie;
        foreach (Collider c in modelo.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        AplicarMateriales(modelo, mats, "BatteryStation (escena)", avisos);

        if (viejo != null && viejo.enabled)
        {
            Undo.RecordObject(viejo, "DarkShot estacion");
            viejo.enabled = false; // se oculta el cilindro viejo; el collider trigger y el script quedan intactos
        }
        EditorUtility.SetDirty(est.gameObject);
        return true;
    }

    // =====================================================================
    //  MENU 3: Colocar ramas de ejemplo
    // =====================================================================
    [MenuItem("DarkShot/3. Colocar ramas de ejemplo")]
    public static void ColocarRamasDeEjemplo()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("DarkShot", "Sal del modo Play antes de ejecutar este paso.", "OK");
            return;
        }

        Scene escena = EditorSceneManager.GetActiveScene();
        if (escena.path != RutaArena)
        {
            EditorUtility.DisplayDialog("DarkShot", "Abre la escena Arena (" + RutaArena + ") y vuelve a ejecutar este paso.", "OK");
            return;
        }

        GameObject pA = AssetDatabase.LoadAssetAtPath<GameObject>(DirPrefabs + "/Branch_Wall_A.prefab");
        GameObject pB = AssetDatabase.LoadAssetAtPath<GameObject>(DirPrefabs + "/Branch_Wall_B.prefab");
        GameObject pSuelo = AssetDatabase.LoadAssetAtPath<GameObject>(DirPrefabs + "/Branch_Floor.prefab");
        if (pA == null || pB == null || pSuelo == null)
        {
            EditorUtility.DisplayDialog("DarkShot", "Faltan los prefabs de ramas. Corre primero 'DarkShot/1. Importar y crear prefabs'.", "OK");
            return;
        }

        GameObject paredes = GameObject.Find("Walls");
        if (paredes == null)
        {
            EditorUtility.DisplayDialog("DarkShot", "No se encontro el objeto 'Walls' en la escena.", "OK");
            return;
        }
        Collider[] cols = paredes.GetComponentsInChildren<Collider>();
        if (cols.Length == 0)
        {
            EditorUtility.DisplayDialog("DarkShot", "'Walls' no tiene colliders: no se pueden hacer raycasts.", "OK");
            return;
        }

        Physics.SyncTransforms();
        Bounds limites = cols[0].bounds;
        for (int i = 1; i < cols.Length; i++) limites.Encapsulate(cols[i].bounds);
        Vector3 origen = new Vector3(limites.center.x, Mathf.Lerp(limites.min.y, limites.max.y, 0.5f), limites.center.z);

        // Borrar "Branches" anterior (idempotente)
        for (int k = 0; k < 5; k++)
        {
            GameObject anterior = GameObject.Find("Branches");
            if (anterior == null) break;
            Undo.DestroyObjectImmediate(anterior);
        }

        GameObject padre = new GameObject("Branches");
        Undo.RegisterCreatedObjectUndo(padre, "DarkShot ramas");

        System.Random azar = new System.Random(1234);
        List<Vector3> puntos = new List<Vector3>();
        List<Vector3> normales = new List<Vector3>();
        int objetivo = 8;
        const int rayos = 24;

        for (int pasada = 0; pasada < 2 && puntos.Count < 6; pasada++)
        {
            float separacion = pasada == 0 ? 3.5f : 1.8f;
            for (int i = 0; i < rayos && puntos.Count < objetivo; i++)
            {
                float ang = (i * 360f / rayos + 7.5f) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang));

                RaycastHit[] golpes = Physics.RaycastAll(origen, dir, 300f, ~0, QueryTriggerInteraction.Ignore);
                System.Array.Sort(golpes, (a, b) => a.distance.CompareTo(b.distance));

                bool hay = false;
                RaycastHit elegido = new RaycastHit();
                foreach (RaycastHit g in golpes)
                {
                    if (!g.collider.transform.IsChildOf(paredes.transform)) continue; // ignora obstaculos del medio
                    elegido = g;
                    hay = true;
                    break;
                }
                if (!hay) continue;
                if (Mathf.Abs(elegido.normal.y) > 0.5f) continue;   // solo superficies verticales
                if (elegido.distance < 2f) continue;

                bool lejos = true;
                foreach (Vector3 p in puntos)
                {
                    if (Vector3.Distance(p, elegido.point) < separacion) { lejos = false; break; }
                }
                if (!lejos) continue;

                puntos.Add(elegido.point);
                normales.Add(elegido.normal);
            }
        }

        if (puntos.Count == 0)
        {
            Undo.DestroyObjectImmediate(padre);
            EditorUtility.DisplayDialog("DarkShot", "Los raycasts no encontraron paredes en 'Walls'. No se coloco nada.", "OK");
            return;
        }

        int colocadas = 0;
        for (int i = 0; i < puntos.Count; i++)
        {
            Vector3 n = normales[i];
            Vector3 pos = puntos[i] + n * 0.02f;
            pos.y = AlturaSuelo(puntos[i] + n * 0.5f, origen.y, limites.min.y) + 0.9f + (float)azar.NextDouble() * 1.1f;

            GameObject prefab = (i % 2 == 0) ? pA : pB;
            GameObject rama = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre.transform);
            Undo.RegisterCreatedObjectUndo(rama, "DarkShot rama");
            rama.transform.position = pos;
            rama.transform.rotation = Quaternion.LookRotation(n, Vector3.up); // se asume que la rama "mira" a +Z y su espalda es el origen
            colocadas++;
        }

        // Dos ramas de suelo cerca de las paredes colocadas
        int deSuelo = Mathf.Min(2, puntos.Count);
        for (int k = 0; k < deSuelo; k++)
        {
            int idx = (k * puntos.Count) / 2;
            Vector3 sobre = puntos[idx] + normales[idx] * 1.0f;
            float y = AlturaSuelo(sobre, origen.y, limites.min.y);
            GameObject rama = (GameObject)PrefabUtility.InstantiatePrefab(pSuelo, padre.transform);
            Undo.RegisterCreatedObjectUndo(rama, "DarkShot rama suelo");
            rama.transform.position = new Vector3(sobre.x, y, sobre.z);
            rama.transform.rotation = Quaternion.Euler(0f, (float)azar.NextDouble() * 360f, 0f);
            colocadas++;
        }

        EditorSceneManager.MarkSceneDirty(escena);
        Log("Paso 3: " + colocadas + " ramas colocadas bajo el objeto 'Branches'. Para quitarlas borra ese objeto (o Ctrl+Z).");
        EditorUtility.DisplayDialog("DarkShot - Paso 3",
            colocadas + " ramas colocadas bajo el objeto 'Branches'.\n\nPara quitarlas borra ese objeto. La escena quedo marcada como modificada: guardala con Ctrl+S si te gusta el resultado.",
            "OK");
    }

    // Altura del suelo bajo un punto (raycast hacia abajo); si no hay nada usa el valor de respaldo
    static float AlturaSuelo(Vector3 xz, float alturaOrigen, float respaldo)
    {
        Vector3 desde = new Vector3(xz.x, alturaOrigen, xz.z);
        RaycastHit golpe;
        if (Physics.Raycast(desde, Vector3.down, out golpe, 100f, ~0, QueryTriggerInteraction.Ignore))
        {
            return golpe.point.y;
        }
        return respaldo;
    }

    // ------------------------------------------------------------ Utilidades
    static void AsegurarCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        string padre = Path.GetDirectoryName(ruta).Replace('\\', '/');
        string nombre = Path.GetFileName(ruta);
        AsegurarCarpeta(padre);
        AssetDatabase.CreateFolder(padre, nombre);
    }

    static void Log(string m) { Debug.Log("[DarkShot] " + m); }
    static void Warn(string m) { Debug.LogWarning("[DarkShot] " + m); }
}
