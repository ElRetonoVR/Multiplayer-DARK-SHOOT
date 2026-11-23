# DARK SHOT - Cambios para Unity (modo un jugador, escena Arena)

Todo lo de esta carpeta replica rutas de Unity: copia el contenido de `Assets/` encima del `Assets/` del proyecto.
No se tocaron `SampleScene`, `GunSingle`, `Capsule.prefab` ni ningun script de multijugador.
No hay Unity en la maquina donde se escribio: **nada ha sido compilado ni ejecutado** (ver Riesgos).

## Archivos

Nuevos
- `Assets/DarkShot/Editor/DarkShotSetup.cs` - menus `DarkShot/1`, `/2` y `/3`.
- `Assets/DarkShot/Scripts/BloomAnimator.cs` - lee `NavMeshAgent.velocity.magnitude` -> parametro `Speed`; expone `PlayAttack()` (trigger `Attack`). No falla si no hay Animator/controller.

Modificados (copia completa en la misma ruta espejo)
- `Assets/Scripts/Singleplayer/enemyNav.cs` (clase `EnemyFollow`)
  - Campos nuevos: `distanciaAtaque = 1.8`, `cooldownAtaque = 1.2`.
  - Nuevo `IntentarAtaque()`: `GetComponentInChildren<BloomAnimator>(true)` (cacheado) y llama `PlayAttack()` si existe; si no existe no hace nada.
  - Se llama desde un `OnCollisionEnter` nuevo (contra tag `Player`) y desde `Update` cuando el jugador esta a <= `distanciaAtaque` (en plano XZ). El dano NO cambia: sigue aplicandolo `PlayerHealthSolo`.
  - Nota: `enemyNav` no golpeaba por si mismo (el golpe es la colision que detecta `PlayerHealthSolo`), por eso el ataque visual se dispara por colision + cercania.
- `Assets/Scripts/Singleplayer/SpawnEnemy.cs`
  - Campo nuevo `public GameObject[] variantesEnemigo`. Al llenar el pool se elige una variante al azar por enemigo (nuevo `ElegirPrefab()`). Si el arreglo esta vacio se usa `enemyPrefab` como antes (compatibilidad total). Si `ElegirPrefab()` devuelve null se salta ese enemigo.
- `JumpscareBloom.cs`: **no se modifico**. La cara se asigna al campo `caraBloom` existente; la luz de la cara es hija de ella, asi que el propio script ya no la apaga durante el susto.

## Que hace cada menu

**DarkShot/1. Importar y crear prefabs** (idempotente)
1. Crea materiales URP Lit en `Assets/DarkShot/Materials/` (colores/emision del contrato) y los remapea por nombre en cada FBX (`AddRemap`; nombres de material tomados de Blender).
2. Configura los ModelImporter: Bloom_Base = Generic + avatar propio, clips `Idle`/`Walk` con loop y `Attack` sin loop (busca takes cuyo nombre termine en eso, p. ej. `BloomRig|Walk`, y los renombra limpio); Bloom_LabCoat y Bloom_Engineer = Generic copiando el avatar de Bloom_Base, sin clips; el resto sin rig ni animacion.
3. Crea `Assets/DarkShot/Animation/Bloom.controller` (Idle/Walk/Attack; `Speed` float, `Attack` trigger; Idle<->Walk con umbral 0.1; AnyState->Attack; Attack->Idle).
4. Crea prefabs en `Assets/DarkShot/Prefabs/`:
   - `Bloom_Base`, `Bloom_LabCoat`, `Bloom_Engineer`: salen de una instancia desempaquetada de `Capsule.prefab` (conservan NavMeshAgent, Rigidbody kinematico, CapsuleCollider, tag `Enemy`, layer, `EnemyFollow`, `Health` con health=10 y sus valores). Se quita el MeshFilter/MeshRenderer de la capsula, escala 1, modelo como hijo `Model` con Animator + `BloomAnimator` en la raiz.
     - Ajustes de geometria: collider capsula radio 0.5 / alto 1.0 / centro (0,0.5,0); NavMeshAgent baseOffset 0, height 1, radius 0.5 (velocidad, aceleracion, stoppingDistance etc. sin cambios). El radio 0.5 equivale al contacto del Capsule original (radio 1 x escala 0.5), para no romper el golpe al jugador.
   - `Bloom_Face`: modelo + una luz puntual propia (en oscuridad el URP Lit se veria negro).
   - `Battery`, `BatteryStation` (BoxCollider trigger 1.2x1x1.2 + `BatteryStationSingle`), `Branch_Wall_A`, `Branch_Wall_B`, `Branch_Floor` (sin colliders; static flags: Batching, Occluder, Occludee, ReflectionProbe; sin Navigation ni ContributeGI a proposito).
5. Muestra resumen y avisos (FBX faltantes, clips no encontrados, materiales sin equivalente) en consola y dialogo. Si falta algun FBX salta lo que depende de el.

**DarkShot/2. Configurar escena Arena** (idempotente; abre Arena si no esta abierta y la guarda)
- `SpawnEnemy`: llena `variantesEnemigo` con los 3 Bloom y apunta `enemyPrefab` a Bloom_Base.
- `JumpscareBloom`: borra el Quad "CaraBloom (provisional)" (solo si cuelga del mismo objeto), instancia `Bloom_Face` como hijo (desactivada, sin collider) y la asigna a `caraBloom`. La posicion/rotacion en runtime ya la fija el script (a `distanciaCara` de la cabeza, +Z del objeto hacia la camara), asi que la posicion local en el editor no importa.
- Audio: si existe `Assets/DarkShot/Audio/Bloom_Breath*` (wav/mp3/ogg) lo asigna al AudioSource `sonidoSusto`; si no, solo avisa en consola. Si el AudioSource ya tenia otro clip ajeno, no lo pisa.
- Estacion de baterias: conserva el GameObject "Station" con `BatteryStationSingle` y su BoxCollider trigger; oculta (desactiva el MeshRenderer) el cilindro aplastado y agrega como hijo el FBX `BatteryStation` (`BatteryStation_Model`), compensando la escala aplastada del padre y apoyandolo en la base del cilindro. No usa el prefab `BatteryStation` en la escena a proposito: duplicaria `BatteryStationSingle` y daria 2 baterias por entrada.
- `MarkSceneDirty` + `SaveScene`.

**DarkShot/3. Colocar ramas de ejemplo** (con Arena abierta)
- Calcula el centro de los colliders de `Walls`, lanza 24 rayos horizontales, y coloca hasta 8 ramas de pared (alternando A/B, orientadas con la normal, altura entre 0.9 y 2 m sobre el suelo, separadas >= 3.5 m; minimo 6 si es posible) y 2 de suelo, todas bajo el objeto `Branches`. Ignora obstaculos que se interpongan en el rayo. Para quitarlas: borrar `Branches` (o Ctrl+Z). No guarda la escena: Ctrl+S si te gusta.

## Pasos para el usuario
1. Copia los FBX a `Assets/DarkShot/Models/` (nombres exactos del contrato) y este `Assets/` encima del del proyecto. Abre Unity 6000.3.3f1 y espera a que compile (revisa la consola por errores).
2. Menu `DarkShot/1. Importar y crear prefabs` y lee el dialogo de avisos. Si lo corres antes de tener todos los FBX no pasa nada: vuelve a correrlo cuando lleguen.
3. Menu `DarkShot/2. Configurar escena Arena`.
4. (Opcional) Pon `Bloom_Breath.wav` (o similar) en `Assets/DarkShot/Audio/` y vuelve a correr el 2.
5. (Opcional) Menu `DarkShot/3. Colocar ramas de ejemplo`.
6. Prueba en Quest: que los Bloom caminen (Walk), ataquen al llegar, mueran con 10 de vida, el susto muestre la cara de frente y la estacion recargue.

## Riesgos / no verificado sin compilar
- Todo el C# esta sin compilar. Los puntos mas sensibles de API: `ModelImporter.materialImportMode/materialLocation/materialName` (confirmados en la doc de Unity 6), `PrefabUtility.InstantiatePrefab(obj, Scene)` + `UnpackPrefabInstance` dentro de una escena de preview, y `AnimatorController` por codigo. Si hay un error de compilacion en la consola, mandame el texto.
- **Orientacion de la cara**: `JumpscareBloom` apunta el +Z del objeto hacia la camara. Con una cara que "mira a +Z" eso la deja de frente, por eso `GiroYCara = 0` (constante al inicio de `DarkShotSetup.cs`). Si en pantalla ves la nuca, cambia a 180 y vuelve a correr el 1 y el 2 (o gira a mano el hijo `Modelo` de `Bloom_Face`). Pendiente de confirmar con el FBX real.
- A 0.35 m una cabeza de ~0.5 m ocupa casi todo el campo de vision (efecto deseado, pero si se ve cortada sube `distanciaCara` en el inspector, p. ej. 0.5-0.6).
- Los nombres de material dependen de lo que exporte Blender; hay un respaldo por nombre (contiene `M_...`), y se avisa de los que no coincidan. Si un FBX no trae `Idle/Walk/Attack` el controller queda con estados vacios hasta que se re-corra el paso 1.
- El estado Attack->Idle usa exit time 0.9; durante ~0.8 s de ataque el Bloom no pasa a Walk. Ajustable en el controller.
- Orientacion de las ramas de pared (se asume frente +Z y espalda en el origen) y el origen del FBX de la estacion en su base: no verificable sin los modelos; ajusta los prefabs si salen girados o flotando.
- Con `stoppingDistance = 1.5` el Bloom se detiene a esa distancia del jugador; el golpe sigue dependiendo de la colision como antes (comportamiento original, no cambiado).
- El pool de SpawnEnemy instancia en el origen y luego desactiva (comportamiento original): puede aparecer algun aviso de NavMeshAgent al inicio.
- Las ramas/estacion no estan marcadas como ContributeGI: si la escena usa lightmaps horneados no aportaran ni recibiran luz horneada.

## Revisión del líder (Opus)
- `DarkShotSetup.cs`: los FBX se importan con `materialLocation = InPrefab` (con `External` Unity ignora los `AddRemap` y no aplicaría los materiales URP).
- `DarkShotSetup.cs`: la cara del susto se aleja 0.12 m dentro del prefab `Bloom_Face` (`OffsetCara`). Si no se aleja, la lengua queda pegada a los ojos y la cámara la corta. Si se ve muy lejos o muy cerca, ajusta `OffsetCara` o `distanciaCara` en `JumpscareBloom`.
- Fuentes de Blender en `BlenderSource/` (fuera de `Assets/` para que Unity no intente importar los .blend). Los scripts se corren con `python3` y el módulo `bpy` 5.3.
- `SpawnEnemy.cs` (corrección del 23/11/2026): los Blooms aparecían encima del techo. Los puntos de spawn están a ~2.6 m de altura y el techo (`Ceiling`) también está horneado en el NavMesh, así que el agente se pegaba a la superficie más cercana, que era el techo. Ahora cada Bloom busca el piso con un raycast hacia abajo desde el punto de spawn y se coloca con `NavMesh.SamplePosition` + `agent.Warp`. Se ajusta con `distanciaBusquedaPiso` y `radioBusquedaNavMesh`. Como alternativa, se puede excluir el techo del NavMesh con un `NavMeshModifier` (Ignore From Build) y volver a hornear.
