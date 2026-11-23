# Graph Report - darkshoot  (2026-11-23)

## Corpus Check
- Corpus is ~14,391 words - fits in a single context window. You may not need a graph.

## Summary
- 290 nodes · 564 edges · 11 communities (8 shown, 3 thin omitted)
- Extraction: 88% EXTRACTED · 12% INFERRED · 0% AMBIGUOUS · INFERRED: 65 edges (avg confidence: 0.87)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- Multijugador en red (PUN)
- Jugador y combate en Arena
- Herramienta DarkShotSetup
- Archivos de scripts
- IA y animación del Bloom
- GDD Laser Project (PvP)
- Guía Photon VR
- Arma láser multijugador
- Estación de baterías
- Tipos de modelo (DarkShotSetup)
- Documento DARK SHOT

## God Nodes (most connected - your core abstractions)
1. `DarkShotSetup` - 35 edges
2. `GunSingle` - 18 edges
3. `Laser Project (laser tag PvP multijugador)` - 18 edges
4. `Gun` - 17 edges
5. `PlayerHealthSolo` - 17 edges
6. `SpawnEnemy` - 16 edges
7. `JumpscareBloom` - 15 edges
8. `NetworkManager` - 15 edges
9. `BloomAnimator` - 14 edges
10. `EnemyFollow` - 14 edges

## Surprising Connections (you probably didn't know these)
- `DarkShotSetup` --implements--> `Ramas (Branches)`  [INFERRED]
  Assets/DarkShot/Editor/DarkShotSetup.cs → docs/DARK_GDD.md
- `Gun` --implements--> `Arma láser sin munición física, consume energía`  [INFERRED]
  Assets/Scripts/Multiplayer/Gun.cs → docs/GDD_UTR_2026.md
- `Gun` --implements--> `Linterna que se debilita con cada disparo`  [INFERRED]
  Assets/Scripts/Multiplayer/Gun.cs → docs/GDD_UTR_2026.md
- `Gun` --conceptually_related_to--> `Niveles de brillo: 100% / 50% / 10% / 0%`  [INFERRED]
  Assets/Scripts/Multiplayer/Gun.cs → docs/GDD_UTR_2026.md
- `Gun` --conceptually_related_to--> `Prefab PlayerVR en Photon/PhotonUnityNetworking/Resources`  [INFERRED]
  Assets/Scripts/Multiplayer/Gun.cs → docs/Guia_Multijugador_VR.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Ciclo de vida del Bloom en Arena** — assets_scripts_singleplayer_spawnenemy_spawnenemy, assets_scripts_singleplayer_enemynav_enemyfollow, assets_darkshot_scripts_bloomanimator_bloomanimator, assets_scripts_singleplayer_health_health, assets_scripts_singleplayer_playerhealthsolo_playerhealthsolo, assets_scripts_singleplayer_jumpscarebloom_jumpscarebloom, assets_darkshot_editor_darkshotsetup_darkshotsetup [INFERRED 0.95]
- **Jugador de red con Photon PUN 2 (SampleScene)** — assets_scripts_networkmanager_networkmanager, assets_scripts_multiplayer_vrlocaltonetworkmapper_vrlocaltonetworkmapper, assets_scripts_multiplayer_gun_gun, assets_scripts_multiplayer_playerhealth_playerhealth, docs_guia_multijugador_vr_prefab_playervr [INFERRED 0.95]
- **Luz y batería como recurso (disparar o ver)** — assets_scripts_singleplayer_gunsingle_gunsingle, assets_scripts_multiplayer_gun_gun, assets_scripts_singleplayer_batterystationsingle_batterystationsingle, docs_dark_gdd_bateria_5_por_disparo, docs_gdd_utr_2026_linterna_se_debilita [INFERRED 0.85]

## Communities (11 total, 3 thin omitted)

### Community 0 - "Multijugador en red (PUN)"
Cohesion: 0.06
Nodes (39): ChangeUI, LeaveRoom, PlayerHealth, VRLocalToNetworkMapper, NetworkManager, Escena Lobby (Assets/Scenes/Lobby.unity), Escena SampleScene (Assets/Scenes/SampleScene.unity), Agarrar baterías con la mano libre (+31 more)

### Community 1 - "Jugador y combate en Arena"
Cohesion: 0.09
Nodes (21): GunSingle, Health, JumpscareBloom, PlayerHealthSolo, ScoreSingle, TimerScript, Ataque principal: jumpscare de la cara del Bloom, Batería: −5% por disparo (+13 more)

### Community 3 - "Archivos de scripts"
Cohesion: 0.07
Nodes (6): CambiarEscenaMult, PlayerVR, Arena (modo de un jugador, GDD de DARK SHOT), Escenas del proyecto y scripts que usan, Lobby, SampleScene (multijugador PvP, GDD de Laser Project)

### Community 4 - "IA y animación del Bloom"
Cohesion: 0.11
Nodes (9): BloomAnimator, EnemyFollow, SpawnEnemy, El Bloom aparece cada 6 / 5 / 3 s, El Bloom golpea solo junto al jugador, El Bloom siempre ve al jugador y lo persigue, Enemigos infinitos, The Bloom (enemigo) (+1 more)

### Community 5 - "GDD Laser Project (PvP)"
Cohesion: 0.08
Nodes (25): **10. Unique value proposition**, **1st month: Basic prototype**, **2. Key Concept (Elevator Pitch)**, **2nd month: playable content**, **3. Principal Mechanics**, **3rd month: Multiplayer**, **4. Progression & goals**, **4th month: Polishing & launch** (+17 more)

### Community 6 - "Guía Photon VR"
Cohesion: 0.08
Nodes (24): Bibliografía, **C#**, **Cliente**, **Cliente-servidor**, **Cliente y servidor**, **Conceptos Clave**, **¿Cómo funciona?**, ***Fundamentos Previos*** (+16 more)

### Community 7 - "Arma láser multijugador"
Cohesion: 0.18
Nodes (3): Gun, Arma láser sin munición física, consume energía, Niveles de brillo: 100% / 50% / 10% / 0%

### Community 9 - "Tipos de modelo (DarkShotSetup)"
Cohesion: 0.50
Nodes (4): TipoModelo, ConAnimacion, Estatico, Riggeado

## Knowledge Gaps
- **11 isolated node(s):** `ConAnimacion`, `Estatico`, `Riggeado`, `Historia: equipo del CDC en un laboratorio subterráneo infectado`, `Agarrar baterías con la mano libre` (+6 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 82 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **3 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `DarkShotSetup` connect `Herramienta DarkShotSetup` to `Tipos de modelo (DarkShotSetup)`, `Archivos de scripts`, `IA y animación del Bloom`, `Jugador y combate en Arena`?**
  _High betweenness centrality (0.155) - this node is a cross-community bridge._
- **Are the 4 inferred relationships involving `DarkShotSetup` (e.g. with `Ataque principal: jumpscare de la cara del Bloom` and `Ramas (Branches)`) actually correct?**
  _`DarkShotSetup` has 4 INFERRED edges - model-reasoned connections that need verification._
- **What connects `ConAnimacion`, `Estatico`, `Riggeado` to the rest of the system?**
  _11 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Multijugador en red (PUN)` be split into smaller, more focused modules?**
  _Cohesion score 0.059932659932659935 - nodes in this community are weakly interconnected._
- **Why does `VRLocalToNetworkMapper` connect `Multijugador en red (PUN)` to `Jugador y combate en Arena`, `Archivos de scripts`?**
  _High betweenness centrality (0.109) - this node is a cross-community bridge._
- **Are the 3 inferred relationships involving `GunSingle` (e.g. with `Batería: −5% por disparo` and `Sin batería el jugador queda ciego`) actually correct?**
  _`GunSingle` has 3 INFERRED edges - model-reasoned connections that need verification._
- **Should `Jugador y combate en Arena` be split into smaller, more focused modules?**
  _Cohesion score 0.08773784355179703 - nodes in this community are weakly interconnected._