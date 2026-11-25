# DARK SHOT - Multijugador (Photon PUN 2.52, Unity 6000.3.3f1)

Todo replica rutas de Unity: copia el contenido de `Assets/` encima del `Assets/` del proyecto.
**Nada se compilo ni se ejecuto** (no hay Unity donde se escribio): ver "Riesgos / no verificado".
Reglas aplicadas: `SPEC_MP.md` incluida la seccion "Cambios por el GDD Laser Project v1.2" (4 jugadores, luz por niveles,
baterias recogibles en red, marcador h/k/d/s, vibracion, audio 3D opcional).

## Archivos

Modificados (misma ruta, misma clase)
- `Assets/Scripts/NetworkManager.cs` - instancia al jugador en `Start` si ya esta en sala; si no, conecta y `JoinOrCreateRoom("RoomVR", MaxPlayers 4)` (pruebas en editor). `AutomaticallySyncScene = true`. `ObtenerMejorSpawn(Vector3?)` (spawn mas lejano de los demas) y `TeletransportarRig(Transform)` (pone la CABEZA sobre el spawn). Si sale de la sala o se desconecta, vuelve al Lobby. `SalirDeLaSala()` / `CargarEscena()` estaticos con guarda anti doble carga.
- `Assets/Scripts/LeaveRoom.cs` - ademas del boton, `OVRInput.Button.Start` (menu del control izquierdo) sale al Lobby.
- `Assets/Scripts/ChangeUI.cs` - a prueba de referencias nulas y sin reactivar imagenes si no cambian.
- `Assets/Scripts/Multiplayer/Gun.cs` - bateria %, reserva (inicial 1, max 3), recarga con B, luz por niveles (100% normal, 50% debil, 10% casi apagada con parpadeo, 0% apagada), RaycastAll ordenado que ignora los colliders del propio `PlayerHealth` y los triggers, dano por RPC con el ActorNumber del atacante, linea laser por RPC con puntos calculados por el tirador, el % de bateria viaja en el RPC (todos ven la luz de cada jugador), vibracion RTouch 0.05 s, audio 3D opcional.
  Campos serializados que se CONSERVAN: `shootButton, gunLight, range, damage, firePoint, linePrefab, lineDuration`. Se QUITAN `ammoCapacity` y `batteries` (los reemplazan `BateriaPct` y `Reserva`; Unity los ignora al cargar el prefab).
- `Assets/Scripts/Multiplayer/PlayerHealth.cs` - un solo `[PunRPC] TakeDamage(int damage, int attackerActor)`. 5 s invencible tras reaparecer, curacion 20/s tras 8 s sin dano, muerte -> `d`+1, RPC al atacante (impacto +10, kill +100), pide al Master soltar 1-3 baterias, reaparece en el spawn mas lejano moviendo el OVRCameraRig, vida y bateria al 100%. RPC a todos para parpadear el acento mientras es invencible. Vibracion en ambas manos 0.2 s al recibir impacto. `LoserLounge` queda como campo sin uso.

Nuevos (en `Assets/DarkShot/Scripts/Multiplayer/`)
- `MatchProps.cs` - enum `EstadoPartida` y claves/ayudas de propiedades (`ms`, `t0`; jugador `h`,`k`,`d`,`s`).
- `MatchManager.cs` - Esperando (<2) -> Cuenta 10 s -> Jugando `duracionPartida` (180, editable) -> Fin 10 s -> todos al Lobby. Estado en propiedades de sala escritas solo por el Master; se lee cada frame (`unchecked(ServerTimestamp - t0)`). Reinicia h/k/d/s al entrar en Esperando/Cuenta; al empezar el combate todos reaparecen; al Fin el master pone `IsOpen=false`. Tambien aloja los RPC de baterias (`RPC_SoltarBaterias`, `RPC_SumarBateria`) porque tiene PhotonView de escena.
- `MatchHUD.cs` - textos TMP del jugador local: estado/tiempo, bateria % y reserva, PUNTOS/K/D/H, INVENCIBLE y marcador final (Score, K, D, H) ordenado por score, kills y menos muertes.
- `RoomBrowser.cs` - Lobby: conecta, `JoinLobby`, diccionario de salas desde `OnRoomListUpdate`, 6 botones, Crear (`Sala-####`, 4 jugadores), Volver (desconecta). El master hace `LoadLevel("SampleScene")`.
- `AvatarVisual.cs` - oculta cabeza/tronco propios, tinta `M_AvatarAccent` por ActorNumber (8 colores, MaterialPropertyBlock), tronco bajo la cabeza girando solo en Y (LateUpdate, no se sincroniza), `SetBlink(bool)`.
- `BatteryPickup.cs` - bateria de red: mano izquierda a <= 0.25 m + grip izquierdo (`RawButton.LHandTrigger`) -> RPC al Master -> valida -> `PhotonNetwork.Destroy` y RPC al jugador (+1 reserva, max 3; con 3 ni se pide).
- `BatterySpawner.cs` - solo el Master, solo en `Jugando`: una bateria cada 20 s, max 3 vivas del spawner (`InstantiateRoomObject`). Si cambia el Master, el nuevo continua (su temporizador arranca de cero).
- `VibracionVR.cs` - `OVRInput.SetControllerVibration` con apagado por corrutina.
- (`BatteryStationNet` NO se entrega: el GDD nuevo lo reemplaza por baterias recogibles.)

Editor: `Assets/DarkShot/Editor/DarkShotMultiplayerSetup.cs` (no toca `DarkShotSetup.cs`).

## Menus (orden 4 -> 5 -> 6 -> 7; todos idempotentes, con resumen en dialogo)
- **4. Importar modelos multijugador**: usa el contrato `MODELOS_MP.md` (hex, emision HDR y rugosidad exactos; `M_LabGlass` transparente alfa 0.35; los `M_Bloom*`/`M_Branch` existentes se reutilizan; reaplica los valores del contrato aunque el .mat ya exista), `bakeAxisConversion = false`. Configura `Player_CDC.fbx` y los `Lab_*`; por cada material importado crea `Assets/DarkShot/Materials/<nombre>.mat` (URP Lit, gris por defecto; emision si el nombre contiene Light/LED/Accent/Glow) **solo si no existe** (no pisa tus ajustes) y lo remapea; prefabs `Lab_*` en `Assets/DarkShot/Prefabs/Multiplayer/` (sin colliders, estaticos); crea `Assets/Photon/PhotonUnityNetworking/Resources/BatteryPickup.prefab` (de `Battery.fbx` + PhotonView + `BatteryPickup` + BoxCollider trigger). Avisa de lo que falte.
- **5. Configurar PlayerVR**: copia en `Backups/` y edita el prefab: escala 1, Trunk bajo RemoteRig y fuera de los observados, primitivas con MeshRenderer apagado, colliders reales (esfera r0.15, capsula r0.22 h0.75, cajas 0.1; tag Player, tambien RightHand), mallas `Avatar_*` copiadas del FBX, pistola en RightHand (pos 0,0,0.1943 escala 0.08), `FirePoint`, `Gun.firePoint/gunLight`, AudioSource de disparo (clip de Arena si se encuentra) y `AvatarVisual`. Sin `Player_CDC.fbx` solo hace pistola y colliders y avisa.
- **6. Configurar SampleScene**: copia en `Backups/`; 8 spawns (los 2 existentes + `SpawnMP_*` en celdas libres a >= 1 m de colliders) asignados al NetworkManager; viste el mapa bajo `LabDressing` (paneles `Lab_WallPanel` por pared delgada, `Lab_WallEnd` en extremos; `Cube` -> `Lab_Block`; piso con `M_LabFloor` porque 280 losas serian demasiadas); 4 lamparas con una luz puntual cada una, sin sombras; 8 props con BoxCollider; **generador central** `BatteryGeneratorMP` (BatteryStation como decoracion + `BatterySpawner`) sobre la plataforma; `MatchManager` con PhotonView de escena; HUD (canvas world-space `MatchHUD` a 0.5 m bajo CenterEyeAnchor); la `Losers Lounge` se DESACTIVA (no se borra) y con ella sus luces (Point 150 y Directional), asi el mapa queda oscuro.
- **7. Configurar Lobby**: copia en `Backups/`; `RoomPanel` (clonando `MultiBtn`: titulo, estado, 6 salas, Crear, Volver) + objeto `RoomBrowser`; `MultiBtn` pasa a llamar `RoomBrowser.AbrirPanel` (se quitan sus llamadas persistentes a `CambiarEscena`).

## Pasos para el usuario
1. Copia `Assets/` encima del proyecto y espera a que compile; revisa la consola (pega aqui cualquier error).
2. Pon los FBX del modelador en `Assets/DarkShot/Models/` y corre el menu 4, luego 5, 6 y 7 en ese orden (puedes repetirlos).
3. Build Settings: Lobby, SampleScene, Arena (ya estan). Photon AppId ya configurado.
4. Probar solo en editor: abre `SampleScene` y Play (entra a la sala "RoomVR"; se puede disparar durante la espera).
5. Probar con 2 jugadores: editor + Quest (o dos builds). En ambos abre `Lobby`; uno crea sala, el otro la ve en la lista y entra. Con 2 jugadores la cuenta regresiva de 10 s empieza sola; partida de 180 s; marcador 10 s; vuelta al Lobby.
6. Controles: gatillo derecho dispara, B recarga, grip izquierdo (con la mano a <= 25 cm de una bateria) recoge, Menu izquierdo sale al Lobby.

## Riesgos / no verificado sin compilar
- **Nada compilado.** Los puntos de API mas sensibles: `ModelImporter.GetExternalObjectMap/RemoveRemap/AddRemap` (menu 4, 2 reimports por FBX), `PrefabUtility.LoadPrefabContents` + editar una instancia anidada de FBX (la pistola) en el menu 5, `TextWrappingModes` de TMP (Unity 6), `UnityEventTools.AddPersistentListener`, `OVRInput.SetControllerVibration` / `RawButton.LHandTrigger`, y `PhotonView.sceneViewId`/`Synchronization` asignados por codigo.
- **HUD**: es un canvas propio world-space (el `UI` existente es Screen Space-Camera y su tamano depende del visor). Con paredes a menos de 0.5 m el texto queda detras de la pared. Tamanos de fuente a ojo.
- **Avatar**: las partes se COPIAN del FBX (MeshFilter/MeshRenderer); si el modelador entrega SkinnedMeshRenderer o rotaciones raras en los nodos, saldran mal (el menu avisa solo de lo primero). `offsetCuello` (0.19 m abajo, 0.07 m atras, del contrato) y la caja de las manos son estimados. Cada parte se coloca con pos/rot a cero bajo su ancla; no verificado que Avatar_RightHand caiga en +X (si sale espejado, invertir X).
- **Spawn inicial**: al entrar, los avatares de los demas pueden no estar instanciados aun, asi que el primer spawn se reparte por ActorNumber en vez de "el mas lejano". Dos reapariciones simultaneas pueden coincidir.
- **Master**: la deteccion de estado no depende de callbacks (lee propiedades cada frame). `SetCustomProperties` aplica local al instante; si no lo hiciera, el master reescribiria el estado una vez mas (inofensivo).
- **Baterias**: si dos jugadores piden la misma bateria el Master concede una; si la reserva llega a 3 entre el pedido y la respuesta, la bateria se pierde. El RPC de suma va por el PhotonView de `MatchManager`, que debe existir (lo crea el menu 6). Las baterias sueltas al morir quedan en el mapa hasta que alguien las recoja (tope total 12).
- **Rendimiento**: 4 lamparas + hasta 4 luces de pistola (spot) en URP; sin sombras. ~300 paneles bajo static batching: revisa draw calls en Quest 3 y, si hace falta, baja los paneles (p. ej. N = largo/2) o activa GPU instancing. El `Cylinder` del mapa solo existe dentro de la Losers Lounge: la plataforma central se coloca en el centro libre mas cercano (si hay 5 m libres).
- `Gun.sonidoDisparo` y los clips (recarga, recoger, parpadeo, bateria baja, impacto, muerte) quedan sin asignar salvo el de disparo; sin clip no falla.
- No se creo capa `PlayerAvatar`: no hizo falta (el raycast distingue lo propio por `PlayerHealth`). El avatar propio no usa sombra para evitar su propia cabeza porque esta oculta.
- Las baterias de red usan `InstantiateRoomObject` (el Master es el dueno); si el Master se va, PUN las traspasa al nuevo Master.
- El `Lobby` no usa `PhotonNetwork.NickName`: el marcador muestra "Jugador N".

## Revisión del líder (Opus, 23/11/2026)
- `PlayerHealth.cs`: se quitó la sobrecarga vieja `TakeDamage(int)`. Dos RPC con el mismo nombre son un riesgo en PUN y ya nadie la llamaba.
- `MatchManager.cs`: al empezar el combate todos reaparecen **sin** invencibilidad. En el GDD los 5 s son solo tras morir.
- `DarkShotMultiplayerSetup.cs` (menú 5): las partes del avatar conservan la **rotación** del nodo del FBX y solo se descarta la posición. El exportador de Blender (`common.py`, sin `bake_space_transform`) deja -90° en X en cada nodo, igual que en los Blooms; con rotación cero el avatar saldría acostado. Si aun así sale girado, revisa el hijo `Avatar_*` bajo Head/Trunk/LeftHand/RightHand.
- `DarkShotMultiplayerSetup.cs` (menú 6): el HUD pasa de 1 m a 0.5 m (escala 0.0004) para que una pared cercana no lo tape. Ocupa el mismo ángulo de visión.
- Revisado: el botón Multiplayer del Lobby es un `UnityEngine.UI.Button` con `onClick` persistente, así que el menú 7 lo redirige bien; `PhotonView.sceneViewId` es público en PUN 2.52.

## Prueba en el editor (Opus, 23/11/2026 14:30–14:40)
- Compiló sin errores; los menús 4, 5, 6 y 7 corrieron completos.
  - Menú 4: 10 FBX, 15 materiales, 9 prefabs y `BatteryPickup`.
  - Menú 5: avatar 4 de 4 y pistola. El nodo `Avatar_Head` trae -90° en X, como se esperaba, y el casco queda derecho.
  - Menú 6: 8 spawns y 36 paredes con 296 paneles. El generador de baterías quedó en (1, 0.5, 10.3), el espacio libre de 6 m más cercano al centro.
  - Menú 7: panel de salas. Se revisó el diseño en el editor.
- Play en `SampleScene` sin visor: conecta a Photon, entra a `RoomVR` y crea `PlayerVR(Clone)`. Cero errores; las advertencias son del SDK de Meta porque no había visor conectado.
- `NetworkManager.OnDisconnected` ya no intenta cargar el Lobby al cerrar la app o detener Play (`ApplicationQuit`).
- Falta: probar con el visor puesto (disparos, HUD, baterías, lista de salas) y con dos jugadores.
- Prueba con el visor por Quest Link (23/11/2026, 14:41–14:43, un jugador). Todo funcionó y la consola no tuvo errores:
  - El Lobby abrió el panel de salas, se conectó a Photon ("Sin salas, crea una") y creó la sala con Crear sala.
  - Cargó SampleScene y el HUD mostró batería, reserva y puntos.
  - Se probaron el disparo (batería al 90%) y la recarga con B (batería al 100%).
  - El botón Menú regresó al Lobby.
  - El usuario confirmó que el mapa, el HUD y las manos con la pistola se veían bien.
- Falta: la prueba con dos jugadores (impactos, kills, baterías al morir, generador central y marcador final).

## Correcciones y prueba con dos jugadores (23/11/2026, 15:00–15:17)
- **Paredes y muebles acostados:** el menú 4 reseteaba la rotación de la raíz del FBX y en los modelos de una sola malla ahí va el giro de -90° de Blender. Ahora se conserva la rotación y la escala del FBX (prefabs `Lab_*`, `BatteryPickup` y la estación del generador). Eso también arregla el "piso irregular": los muebles acostados tenían colliders bajos con los que se tropezaba al caminar.
- **Techo:** se agrega `TechoCaraInferior`, una copia volteada solo visual, porque el Plane del techo no se veía desde abajo y asomaba el cielo.
- **Más obstáculos:** hasta 18 props con collider en lugar de 8, separados al menos 2.5 m entre sí.
- **Modo escritorio para pruebas** (`DesktopDebugControls`):
  - Lobby: M abre el panel de salas, J se une a la primera sala, C crea una sala.
  - Partida: WASD mueve, las flechas giran y miran, Espacio dispara, R recarga, G recoge una batería, L sale al Lobby.
  - F2 fuerza el modo escritorio aunque haya visor.
  - Sin visor, la cabeza queda a 1.6 m.
- **Aviso:** con el Quest conectado por USB y el APK corriendo, darle Play en el editor puede trabar Unity, porque intenta abrir Quest Link. Se soluciona desconectando el cable.
- **Prueba:** el usuario con el APK en el Quest y Claude desde el editor en modo escritorio, en la sala Sala-1841.
  - Funcionó en los dos sentidos: impactos, kill (150 puntos = 5 impactos + 1 kill), muerte, reaparición en otro spawn, aviso "INVENCIBLE", 3 baterías soltadas al morir y regreso de los dos al Lobby al terminar.
  - El mapa ya se ve como laboratorio: pasillos, lámparas rojas y ámbar, venas de la infección y obstáculos de pie.

## v2: Listo, Equipos y Cooperativo (23/11/2026)
Spec: seccion "v2 (23/11/2026 15:25)" de `SPEC_MP.md`. **Nada compilado ni ejecutado** (ver "Riesgos / no verificado" de esta seccion).

### Archivos
Modificados
- `Assets/DarkShot/Scripts/Multiplayer/MatchProps.cs` - enum `ModoPartida`; claves de sala `md` y de jugador `r` (listo), `t` (equipo 0/1/2), `b` (Blooms); utilidades (`ModoActual`, `EstaListo`, `GetEquipo`, `SonAliados`, `ColorDeEquipo`, `PuntosEquipo`, `BloomsTotales`, `LimpiarListoYEquipoLocal`...). `ReiniciarLocal` tambien pone `b` en 0.
- `MatchManager.cs` - condiciones de inicio por modo (todos listos + FFA 2+, Equipos 1+ por equipo y todos con equipo, Coop 1+); vuelve a Esperando si alguien se desmarca, entra o sale; entrada del mando (A listo, X Rojo, Y Azul, solo en Esperando) y metodos publicos `AlternarListoLocal` / `ElegirEquipoLocal` (los usa el modo escritorio); `RPC_BloomEliminado`; limpia `r` al Fin y `r`/`t` al salir de la sala o al entrar a una nueva. No hay `LobbyEsperaInput.cs`: la entrada vive en `MatchManager`.
- `MatchHUD.cs` - en Esperando/Cuenta muestra en el cuadro central el modo, la lista de jugadores (LISTO / no listo, equipo) y las instrucciones; marcador final por modo (FFA igual que antes; Equipos con puntos por equipo, ganador o empate y lista agrupada por equipo; Coop con Blooms eliminados). Sin simbolos Unicode (se usa texto de color en vez de check/cruz).
- `AvatarVisual.cs` - color por equipo en modo Equipos (revisa cada 0.25 s); FFA y Coop siguen con la paleta por jugador.
- `Assets/Scripts/Multiplayer/Gun.cs` - sin dano a jugadores en Coop, sin fuego amigo en Equipos (el laser se ve, no se manda RPC); si el raycast golpea un `BloomNet` llama `PedirDano`.
- `Assets/Scripts/Multiplayer/PlayerHealth.cs` - `TakeDamage` ignora dano de jugadores en Coop y de aliados en Equipos (los Blooms usan `attackerActor = -2` y siempre danan); `ReaparecerAlInicioDePartida()`.
- `Assets/Scripts/NetworkManager.cs` - spawns por mitad segun equipo (Rojo x < promedio de los spawns, Azul x >), huir de enemigos (Equipos) o de Blooms (Coop); `ObtenerSpawnInicialPartida()` reparte por orden dentro del equipo al empezar; la sala de pruebas se crea con `md = 0`.
- `RoomBrowser.cs` - `CrearSala(int modo)`, `CrearSalaFFA/Equipos/Coop()` y `CrearSala()` (= FFA); campos `botonCrearFFA/Equipos/Coop`; publica `md` en el lobby; la lista muestra "Sala-1841 - Equipos - 2/4".
- `DesktopDebugControls.cs` - Lobby: C = FFA, E = Equipos, O = Cooperativo, J = unirse, M = panel. Espera: K = listo, Z = Rojo, X = Azul.
- `Assets/DarkShot/Editor/DarkShotMultiplayerSetup.cs` - menu 7 con tres botones de modo (en lugar de Crear) y nuevo menu 8.
Nuevos
- `BloomNet.cs` y `BloomSpawner.cs` (`Assets/DarkShot/Scripts/Multiplayer/`).

### Menus (orden 4 -> 5 -> 6 -> 7 -> 8)
- **Volver a correr: 7** (botones de modo del Lobby) y **8** (nuevo). **No hace falta** volver a correr 4, 5 ni 6: el avatar, el HUD, el `MatchManager` y su PhotonView no cambiaron.
- **8. Configurar Cooperativo**: crea `BloomNet_Base|LabCoat|Engineer` en `Assets/Photon/PhotonUnityNetworking/Resources/` (copia de `Bloom_*.prefab` sin `EnemyFollow` ni `Health` y sin scripts perdidos; PhotonView + PhotonTransformView + `BloomNet`); en `SampleScene` agrega `BloomSpawner` y `NavMeshSurfaceCoop` (colliders fisicos), pone `NavMeshModifier` Ignore From Build al `Ceiling`/`Techo*` y a todo collider que empiece a mas de 2.5 m sobre el piso, hornea y guarda los datos en `Assets/Scenes/SampleScene/NavMesh-BloomNet.asset`; copia de seguridad de la escena en `Backups/`.

### Como probar (un visor + el editor en modo escritorio)
Cada paso se hace en los dos lados: visor = A / X / Y; editor = K / Z / X (con el cursor en la ventana Game). Espacio NO se usa en el Lobby.
1. **Todos vs todos**: en el Lobby, visor: boton "Todos vs todos"; editor: M y luego J. Los dos marcan listo (A / K). Al estar los dos listos empieza la cuenta de 10 s. Probar desmarcar durante la cuenta (A / K): vuelve a Esperando.
2. **Equipos**: visor: boton "Equipos" (editor: E si crea el editor). Cada uno elige equipo (visor X o Y; editor Z o X), luego listo. Con los dos en el mismo equipo no arranca ("Hace falta al menos 1 jugador en cada equipo"). Con equipos distintos: aparecen en mitades distintas, el acento del avatar es rojo/azul y se puede danar al rival. Para probar fuego amigo hace falta un tercer jugador en el mismo equipo (o dos instancias en el mismo equipo y comprobar que el disparo no baja vida).
3. **Cooperativo**: visor: boton "Cooperativo" (editor: O). Con 1 jugador listo ya empieza. Aparecen Blooms cada 6 s (luego 5 y 3), te persiguen y atacan a 1.6 m (20 de dano). Un disparo los elimina (+100 puntos, contador de Blooms en el HUD y en el marcador). Los disparos entre jugadores no dan dano.
4. Con la sala llena de Blooms (15) revisar FPS en el Quest.

### Decisiones y diferencias respecto a la spec
- La sala se **cierra al empezar el combate** (`IsOpen = false` en Jugando, ya no solo al Fin) para que nadie entre sin equipo.
- En la cuenta regresiva A solo sirve para **desmarcar** (si no, la regla "alguien se desmarca durante la cuenta" seria imposible, porque X/Y/A solo funcionan en Esperando).
- En Equipos hace falta tener equipo para marcar listo.
- Los equipos y `r` se limpian al salir de la sala (no al Fin) porque el marcador final se agrupa por equipo.
- Los Blooms los destruye el Master si el estado deja de ser Jugando. Aparece 1 Bloom por intervalo (no oleadas).
- `JumpscareBloom` NO se integro: apaga todas las luces y pausa el audio, y necesita objetos de escena (cara, sonido). Solo hay vibracion y dano.
- `BloomAnimator` se deja desactivado en todos los clientes (lee `agent.velocity`); `BloomNet` calcula `Speed` con el desplazamiento y usa `PlayAttack()`.

### Riesgos / no verificado (v2)
- **Nada compilado.** Puntos mas sensibles: en el menu 8 `GameObjectUtility.RemoveMonoBehavioursWithMissingScript` sobre un prefab cargado, la API de `Unity.AI.Navigation` 2.0.9 (`collectObjects`, `useGeometry`, `defaultArea`, `BuildNavMesh()` y guardar `navMeshData` con `CreateAsset`), los campos publicos `m_SynchronizePosition/Rotation/Scale` de `PhotonTransformView`, y `Hashtable` con `CustomRoomPropertiesForLobby`.
- El horneado con `PhysicsColliders` depende de que los colliders del suelo existan (el `Plane`). Si el NavMesh sale vacio o con huecos hay que revisar a mano en la ventana Navigation; el limite de 2.5 m usa el spawn mas bajo como piso.
- `BloomNet`: el agente nace en el Master y en los clientes se apaga; si el prefab tiene root motion en el Animator del FBX, el Bloom podria deslizarse o desincronizarse. Cambio de Master con Blooms vivos: no probado. El Bloom no esquiva a otros Blooms mas alla del avoidance de Unity.
- Los Blooms detectan "jugador vivo" sin vida real (la vida solo existe en el dueno): atacan siempre al mas cercano; la invencibilidad tras morir los frena por `TakeDamage`.
- Tras reaparecer en Coop, el jugador puede quedar cerca de Blooms si no hay spawn libre lejos.
- `PhotonView` de los Blooms: `Synchronization = UnreliableOnChange` y estos prefabs deben estar en `Resources` con el nombre exacto; si PUN no los encuentra, `InstantiateRoomObject` lanza error en consola.
- En Equipos dos companeros reaparecen en spawns repartidos por orden al empezar, pero tras morir pueden coincidir (igual que en v1). Si todos los spawns tuvieran la misma x, ambos equipos usan todos.
- Sin simbolos de check/cruz: se usan los textos "LISTO" / "no listo" (la fuente TMP del proyecto puede no tener esos glifos).
- Texto de las salas del Lobby con fuente 4.5 y ancho 92: "Sala-1841 - Todos vs todos - 2/4" puede quedar justo; si se corta, bajar el tamano en el menu 7.
- Los tres botones de modo (y Volver) estan a y = -25 / -42 en un panel de 100 de alto; la lista de 6 salas llega a y = -13. No verificado visualmente.

## Modo caminando y mapa del tamaño del single (25/11/2026)
- **Opción al crear sala:** botón "Caminando: SI/NO" en el panel de salas (se recuerda en cada visor; en modo escritorio, V). Se guarda en la propiedad de sala `cm` y la lista muestra "(caminando)". El HUD de espera muestra CAMINANDO.
- **En modo caminando:**
  - Al entrar a la escena, cada jugador se coloca en su **punto de origen** (`NetworkManager.origenesCaminando`, uno distinto por jugador según su orden en la sala).
  - Después **no se le vuelve a mover**: ni al empezar el combate ni al reaparecer tras morir. Recupera vida, batería e invencibilidad donde está parado.
- **Menú 13** (`DarkShotModoCaminando.cs`): encoge el laboratorio de SampleScene ×0.582 (de 40 × 27.3 m a 23.3 × 15.9 m, como Arena). Las paredes se acortan sin cambiar su grosor ni su altura. Después corre el menú 6 y crea `OrigenesCaminando` (4 puntos lo más separados posible, mirando al centro) en SampleScene y ArenaCoop.
  - La música de fondo que estaba en `LabDressing` pasó al objeto raíz `MusicaFondo`, porque el menú 6 borra `LabDressing`.
  - El generador de baterías quedó en el centro (0, 0, 0) sin plataforma, porque ya no hay 5 m libres.
- Orígenes en SampleScene (x, z): (−9.6, 1.1), (9.4, −6.0), (1.4, 5.1), (1.4, −5.0).
- Orígenes en ArenaCoop (x, z): (−10.6, −4.0), (10.4, 1.1), (−1.6, 6.1), (1.4, −7.0).
- Se volvieron a correr los menús 7 y 9 (botón nuevo en el Lobby y su estilo).
- **Probado en el editor:**
  - Lobby → Caminando: SI → Todos vs todos.
  - SampleScene carga, el HUD dice CAMINANDO y el rig queda en el Origen_1.
  - Consola sin errores.
- **Falta:** probar con dos visores (que nadie se mueva al morir) y en el Cooperativo.
- **Para pruebas sin Lobby:** `NetworkManager.pruebasCaminando` crea la sala de pruebas en modo caminando.

## 1 m menos de ancho y largo y sin joystick en modo caminando (25/11/2026, 14:30)
- **Menú 14** ("Quitar 1 m de ancho y largo a los mapas"):
  - Arena, ArenaCoop y SampleScene pasan de 23.3 × 15.9 a **22.3 × 14.9 m**, alrededor del centro del piso. La altura no cambia.
  - Paredes y obstáculos delgados conservan su ángulo relativo y su grosor; ramas, estación, spawns y rig solo se mueven.
  - Se volvió a hornear el NavMesh de Arena. ArenaCoop tiene ahora su propio NavMesh (`Assets/Scenes/ArenaCoop/NavMesh-Plane.asset`); antes compartía el de Arena.
  - El laboratorio se vistió de nuevo (menú 6) y se rehicieron los orígenes. Se corrió el menú 9.
  - Cada vez que se corre quita otro metro; hay copias en `Backups/`.
- Orígenes en SampleScene (x, z): (−8.3, −6.4), (7.1, 3.7), (−4.1, 4.6), (0.9, −4.5).
- Orígenes en ArenaCoop (x, z): (−10.2, −3.7), (9.9, 1.0), (−1.6, 5.7), (1.3, −6.5).
- **Modo caminando sin joystick:** al entrar a la escena, `NetworkManager.DesactivarLocomocion()` apaga el objeto `Locomotor` del bloque OVRInteractionComprehensive y todos los componentes `Oculus.Interaction.Locomotion` (deslizarse, girar con el stick, teletransporte).
  - Probado en el editor: en una sala caminando el `Locomotor` queda inactivo, sin errores en la consola.
  - El single player no cambia: ahí el joystick sigue activo.

## Interior de 21.1 × 14.75 m y orígenes mirando al frente (25/11/2026, 14:55)
- **Menú 15:** los orígenes del modo caminando miran todos a +Z, la pared del frente. Antes miraban al centro.
- **Menú 16:** deja el interior de Arena, ArenaCoop y SampleScene en **21.1 × 14.75 m**, medidos de cara interior a cara interior de las paredes.
  - Se volvió a hornear el NavMesh de Arena y ArenaCoop, se vistió de nuevo el laboratorio y se rehicieron los orígenes.
  - Después se corrió el menú 9.
  - Los pasillos siguen conectados con al menos 1 m de paso.
- Orígenes del laboratorio (distancia a la pared izquierda / a la del frente): 1: 2.57 / 13.78; 2: 17.32 / 3.72; 3: 6.89 / 2.82; 4: 18.89 / 12.82.
- Orígenes del Cooperativo (izquierda / frente): 1: 0.87 / 11.07; 2: 19.93 / 6.42; 3: 9.04 / 1.76; 4: 11.76 / 13.86.
