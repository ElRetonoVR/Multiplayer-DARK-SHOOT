# Escenas del proyecto y scripts que usan

> Índice generado a partir de los archivos `Assets/Scenes/*.unity` y de los prefabs que esas escenas instancian, el 23/11/2026.

## Arena (modo de un jugador, GDD de DARK SHOT)

Ruta: `Assets/Scenes/Arena.unity`

| Objeto en la escena | Script |
|---|---|
| player | PlayerHealthSolo |
| gun | GunSingle |
| Controller | SpawnEnemy, ScoreSingle |
| Timer Script | TimerScript |
| Station | BatteryStationSingle |
| JumpscareBloom | JumpscareBloom |
| Branches | (decoración creada por DarkShotSetup, menú 3) |

- SpawnEnemy instancia el pool de Blooms a partir de `variantesEnemigo`: Bloom_Base, Bloom_LabCoat y Bloom_Engineer (prefabs creados por DarkShotSetup, menú 1, a partir de `Assets/Prefab/Capsule.prefab`).
- Cada Bloom lleva EnemyFollow (enemyNav.cs), Health y BloomAnimator.
- DarkShotSetup (menú 2) configura esta escena: asigna las variantes a SpawnEnemy, la cara Bloom_Face y el audio a JumpscareBloom, y el modelo de la estación.

## SampleScene (multijugador PvP, GDD de Laser Project)

Ruta: `Assets/Scenes/SampleScene.unity`

| Objeto en la escena | Script |
|---|---|
| NetworkManager | NetworkManager, LeaveRoom |
| UI Script | ChangeUI |

- NetworkManager instancia con Photon el prefab `PlayerVR` (`Assets/Photon/PhotonUnityNetworking/Resources/PlayerVR.prefab`), que lleva Gun, PlayerHealth y VRLocalToNetworkMapper.

## Lobby

Ruta: `Assets/Scenes/Lobby.unity`. Escena de entrada; TimerScript regresa aquí al terminar la partida de Arena y LeaveRoom al salir de la sala.
