using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

// MODO ESCRITORIO (solo para pruebas sin visor, p. ej. un jugador virtual de Multiplayer Play Mode).
// Se crea solo al arrancar el juego. Las teclas del Lobby funcionan siempre; moverse/disparar y la altura de la
// camara solo sin visor activo (XRSettings.isDeviceActive == false) o con F2 (forzar modo escritorio).
//
// Teclas:
//   Lobby:   M = abrir panel de salas   J = unirse a la primera sala   C = crear sala Todos vs todos
//            E = crear sala Equipos   O = crear sala Cooperativo
//   Espera:  K = listo (como A)   Z = Rojo (como X)   X = Azul (como Y)   (solo en Esperando)
//   Partida: W/A/S/D = moverse   Flechas izquierda/derecha = girar   Flechas arriba/abajo = mirar arriba/abajo
//            Espacio = disparar   R = recargar   G = recoger bateria cercana (1.5 m)   L = salir al Lobby
// Sin visor, la cabeza y las manos del OVRCameraRig se colocan a altura de persona (1.6 m) para que el
// avatar remoto se vea de pie y se le pueda disparar.
[DefaultExecutionOrder(-100)] // antes de VRLocalToNetworkMapper (que copia las anclas en su LateUpdate)
public class DesktopDebugControls : MonoBehaviour
{
    const float AlturaOjos = 1.6f;
    const float VelocidadMovimiento = 2.5f;
    const float VelocidadGiro = 90f;

    static DesktopDebugControls instancia;
    float pitch;
    bool forzarEscritorio; // F2: forzar modo escritorio aunque Unity crea que hay visor (p. ej. Quest conectado por USB)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Instalar()
    {
        if (instancia != null) return;
        GameObject go = new GameObject("DesktopDebugControls");
        DontDestroyOnLoad(go);
        instancia = go.AddComponent<DesktopDebugControls>();
    }

    bool HayVisor
    {
        get { return XRSettings.isDeviceActive && !forzarEscritorio; }
    }

    void Update()
    {
        // Las teclas funcionan siempre (con el visor puesto nadie usa el teclado)
        Keyboard k = Keyboard.current;
        if (k == null) return;
        if (k.f2Key.wasPressedThisFrame)
        {
            forzarEscritorio = !forzarEscritorio;
            Debug.Log("[DarkShot] Modo escritorio forzado: " + forzarEscritorio);
        }

        // ---- Lobby
        RoomBrowser rb = FindFirstObjectByType<RoomBrowser>();
        if (rb != null)
        {
            if (k.mKey.wasPressedThisFrame) rb.AbrirPanel();
            if (k.jKey.wasPressedThisFrame) rb.UnirsePrimeraSala();
            if (k.cKey.wasPressedThisFrame) rb.CrearSalaFFA();
            if (k.eKey.wasPressedThisFrame) rb.CrearSalaEquipos();
            if (k.oKey.wasPressedThisFrame) rb.CrearSalaCoop();
            if (k.vKey.wasPressedThisFrame) rb.AlternarCaminando();
        }

        // ---- Sala de espera: listo y equipo (funcionan con o sin visor)
        MatchManager mm = MatchManager.Instancia;
        if (mm != null && PhotonNetwork.InRoom)
        {
            if (k.kKey.wasPressedThisFrame) mm.AlternarListoLocal();
            if (k.zKey.wasPressedThisFrame) mm.ElegirEquipoLocal(MatchProps.EquipoRojo);
            if (k.xKey.wasPressedThisFrame) mm.ElegirEquipoLocal(MatchProps.EquipoAzul);
        }

        // ---- Partida (movimiento solo sin visor: con visor se camina con el joystick)
        if (!PhotonNetwork.InRoom || HayVisor) return;
        OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig == null) return;

        float giro = 0f;
        if (k.leftArrowKey.isPressed) giro -= 1f;
        if (k.rightArrowKey.isPressed) giro += 1f;
        rig.transform.Rotate(0f, giro * VelocidadGiro * Time.deltaTime, 0f, Space.World);

        if (k.upArrowKey.isPressed) pitch -= 45f * Time.deltaTime;
        if (k.downArrowKey.isPressed) pitch += 45f * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, -60f, 60f);

        Vector3 mov = Vector3.zero;
        if (k.wKey.isPressed) mov += rig.transform.forward;
        if (k.sKey.isPressed) mov -= rig.transform.forward;
        if (k.dKey.isPressed) mov += rig.transform.right;
        if (k.aKey.isPressed) mov -= rig.transform.right;
        mov.y = 0f;
        if (mov.sqrMagnitude > 0.001f) rig.transform.position += mov.normalized * VelocidadMovimiento * Time.deltaTime;

        if (Gun.Local != null)
        {
            if (k.spaceKey.wasPressedThisFrame) Gun.Local.DispararExterno();
            if (k.rKey.wasPressedThisFrame) Gun.Local.RecargarExterno();
        }

        if (k.gKey.wasPressedThisFrame)
        {
            BatteryPickup cercana = null;
            float mejor = 1.5f;
            for (int i = 0; i < BatteryPickup.Activas.Count; i++)
            {
                BatteryPickup b = BatteryPickup.Activas[i];
                if (b == null) continue;
                Vector3 d = b.transform.position - rig.transform.position;
                d.y = 0f;
                if (d.magnitude < mejor) { mejor = d.magnitude; cercana = b; }
            }
            if (cercana != null) cercana.PedirRecoger();
        }

        if (k.lKey.wasPressedThisFrame) NetworkManager.SalirDeLaSala();
    }

    // Anclas a altura de persona (sin visor el tracking las deja en el piso)
    void LateUpdate()
    {
        if (HayVisor) return;
        OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig == null || rig.centerEyeAnchor == null) return;

        Quaternion mirada = Quaternion.Euler(pitch, 0f, 0f);
        rig.centerEyeAnchor.localPosition = new Vector3(0f, AlturaOjos, 0f);
        rig.centerEyeAnchor.localRotation = mirada;
        if (rig.leftHandAnchor != null)
        {
            rig.leftHandAnchor.localPosition = new Vector3(-0.25f, 1.2f, 0.35f);
            rig.leftHandAnchor.localRotation = Quaternion.identity;
        }
        if (rig.rightHandAnchor != null)
        {
            // La pistola apunta hacia donde mira la cabeza
            rig.rightHandAnchor.localPosition = new Vector3(0.25f, 1.25f, 0.35f);
            rig.rightHandAnchor.localRotation = mirada;
        }
    }
}
