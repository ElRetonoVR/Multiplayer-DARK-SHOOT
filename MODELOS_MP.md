# Modelos multijugador DarkShot (Player_CDC + kit de laboratorio)

Generados con `BlenderSource/scripts/make_mp.py` (usa `common.py` del proyecto, con las rutas de salida adaptadas a `/home/claude/out`; helpers y materiales nuevos en `mpcommon.py`, modelos en `player.py` y `labkit.py`). Exportador identico al de `common.py::export_fbx` (FBX_SCALE_ALL, forward -Z, up Y, flat shading, sin rig ni animacion). Estilo low-poly facetado.

## Convenciones
- Escala 1 unidad = 1 m. Re-importados en bpy: todas las dimensiones coinciden con lo pedido.
- Modelado en Blender con Z arriba y el frente en -Y (igual que Bloom_Face). Con la exportacion del proyecto eso llega a Unity como **+Y arriba, +Z = frente**. Las dimensiones de abajo estan en ejes de **Unity** (X ancho, Y alto, Z fondo).
- Los materiales del FBX solo llevan color base. **La emision hay que ponerla en Unity** (columna "Emision"). `M_LabGlass` debe ser un material transparente en URP (alpha ~0.3-0.4); en el FBX es opaco.
- `M_BloomSkin`, `M_BloomVeins`, `M_Branch` se llaman igual que los existentes (mismo hex), para que Unity reutilice los suyos.
- Todos son meshes cerrados con normales hacia fuera; sin colliders (los colliders siguen siendo los cubos de la escena).

## Materiales nuevos
| Material | Color base | Emision (HDR) | Rugosidad | Uso |
|---|---|---|---|---|
| M_AvatarSuit | #3C4640 | no | 0.85 | traje gris verdoso oscuro |
| M_AvatarVisor | #0B1218 | no | 0.08 (brillante) | visor |
| M_AvatarGear | #16181B | no | 0.7 | correas, chaleco, guantes, respirador |
| M_AvatarAccent | #E6F0EC (blanco grisaceo) | #E6F0EC x ~2.5 (recolorable por jugador) | 0.4 | franjas emisivas |
| M_LabMetal | #3A4350 | no | 0.45 | metal azulado frio |
| M_LabPanel | #4A5758 | no | 0.7 | paneles gris verdoso |
| M_LabPipe | #2A3037 | no | 0.5 | tuberias, patas, marcos oscuros |
| M_LabFloor | #2E3539 | no | 0.8 | centro de la losa |
| M_LabFloorEdge | #1F2427 | no | 0.6 | borde biselado de la losa |
| M_LabGlass | #6FA3AA | no (alpha 0.35 en Unity) | 0.05 | cristal tanques/ventanas/tubos |
| M_LabLight | #FFE2A8 | #FFE2A8 x 5 | 0.4 | lampara |
| M_LabLED | #FF3A22 | #FF3A22 x 4 | 0.4 | luces de emergencia rojas |
| M_LabLEDGreen | #3CCB78 | #3CCB78 x 3 | 0.4 | LEDs verde clinico (extra, no pedido) |
| M_LabLiquid | #4FD08A | #4FD08A x 1.2 | 0.2 | liquido de tanques/tubos (extra) |
| M_LabHazard | #8C6A1C | no | 0.6 | franjas/etiquetas de aviso ambar (extra) |

Reutilizados: M_BloomSkin (#C9B8A8), M_BloomVeins (#5A1E2A, emision 0.35), M_Branch (#1E1418).

## Player_CDC.fbx  (2268 tris de 3000)
4 objetos separados, sin padre comun (hijos directos del root del FBX). **Cada malla esta modelada alrededor de su propio origen** (el origen del objeto = su pivote). La posicion de cada objeto dentro del FBX es una pose de referencia ensamblada (en Unity): puedes ignorarla o usarla de preview; las mallas no dependen de ella.

| Objeto | Tris | Materiales (orden) | Pivote | Caja local (Unity, m) x / y / z |
|---|---|---|---|---|
| Avatar_Head | 780 | M_AvatarSuit, M_AvatarVisor, M_AvatarAccent, M_AvatarGear | centro de los ojos (camara) | x +-0.139 (casco 0.256 + franja), y -0.15..+0.22 (antena), z -0.185..+0.168 |
| Avatar_Trunk | 684 | M_AvatarSuit, M_AvatarGear, M_AvatarAccent | base del cuello, centro superior | x +-0.247, y -0.75..+0.085 (cuello sobresale 8 cm), z -0.247..+0.19 (mochila detras) |
| Avatar_LeftHand | 402 | M_AvatarGear, M_AvatarAccent, M_AvatarSuit | punto de agarre del mando | x -0.044..+0.053, y +-0.055, z -0.234..+0.044 |
| Avatar_RightHand | 402 | idem | idem | x -0.055..+0.043, y +-0.055, z -0.234..+0.042 |

- Todos miran +Z. Lado derecho del personaje = +X de Unity.
- Pose de referencia (Unity): Head (0, 1.70, +0.07); Trunk (0, 1.51, 0); LeftHand (-0.30, 1.20, +0.12); RightHand (+0.30, 1.20, +0.12). Relacion cabeza-tronco recomendada: pivote de ojos ~0.19 m por encima y ~0.07 m por delante del pivote del tronco (el cuello del tronco llega a +0.085 y el anillo del casco baja a -0.15 desde los ojos, solapan sin hueco).
- Manos: puño con los 4 dedos envolviendo un mango vertical (eje Y de Unity que pasa por el pivote) y el pulgar apuntando hacia delante (+Z); las articulaciones/nudillos miran a +Z, el antebrazo (~0.15 m visibles + puño) sale hacia atras (-Z) con ligera subida. **Hueco libre para el mando/pistola**: cilindro vertical de radio ~0.024 m centrado en el pivote, de y -0.04 a +0.04, delante de la palma. La palma esta ~0.03 m detras del pivote. Si la empuñadura de la pistola es mas gruesa, recorta con el offset del grip (se solapara un poco con el guante, no es visible).
- Acentos (M_AvatarAccent): franja del visor que rodea el casco, raya de corona, franja bajo el visor, cintas en hombreras, 2 lineas verticales en el pecho, linea y banda en la espalda, hebilla, aros en los tanques de la mochila, aros de muñeca y linea en el dorso de la mano.
- El casco propio no se debe renderizar para el jugador local (la camara esta dentro del visor).
- Hombros: muñones de manga cortos (sin brazos). Sin piernas (solo hasta la cintura, -0.75 m).

## Kit de laboratorio
Pivote = centro de la base (Y=0 en el suelo) salvo indicacion. Dimensiones (x ancho / y alto / z fondo):

| FBX | Tris | Presup. | Dimensiones | Pivote | Materiales |
|---|---|---|---|---|---|
| Lab_WallPanel | 380 | 400 | 1.00 / 2.93 / 0.12 | centro base | M_LabPanel, M_LabMetal, M_LabPipe, M_LabLED, M_BloomSkin, M_BloomVeins |
| Lab_WallEnd | 96 | 120 | 0.20 / 2.93 / 0.20 | centro base | M_LabPanel, M_LabMetal, M_LabLED |
| Lab_Block | 1496 | 2500 | 3.17 / 3.00 / 2.98 | centro base | M_LabMetal, M_LabHazard, M_LabPanel, M_LabPipe, M_LabLED, M_LabGlass, M_LabLiquid, M_BloomSkin, M_LabLEDGreen, M_BloomVeins, M_Branch |
| Lab_Platform | 780 | 800 | 5.00 / 0.50 / 5.00 | centro base | M_LabMetal, M_LabPipe, M_LabPanel, M_LabLED, M_LabLEDGreen |
| Lab_Crate | 168 | 200 | 0.80 / 0.80 / 0.80 | centro base | M_LabPanel, M_LabMetal, M_LabHazard, M_LabPipe, M_LabLED, M_BloomSkin |
| Lab_Table | 440 | 600 | 1.60 / 0.88 / 0.90 (mesa 0.70 + tubos hasta 0.88) | centro base | M_LabMetal, M_LabPipe, M_LabPanel, M_LabGlass, M_LabLiquid, M_BloomSkin, M_LabLED |
| Lab_ServerRack | 460 | 500 | 0.60 / 2.00 / 0.80 | centro base | M_LabMetal, M_LabPipe, M_LabPanel, M_LabLED, M_LabLEDGreen |
| Lab_CeilingLamp | 188 | 200 | 0.60 / 0.40 / 0.60 | **arriba (punto de colgado)**; el cuerpo cuelga hacia -Y (0 a -0.40) | M_LabPipe, M_LabPanel, M_LabMetal, M_LabLight |
| Lab_FloorTile | 18 | 20 | 2.00 / 0.05 / 2.00 | centro en planta; **la cara superior esta en Y=0** y la losa baja a -0.05 | M_LabFloorEdge, M_LabFloor |

Notas por pieza:
- **Lab_WallPanel**: detalle en ambas caras. Cara -Z (frente): tuberia redonda horizontal con 2 abrazaderas, paneles en relieve, 2 bultos organicos + venas, luz roja de emergencia. Cara +Z: conducto rectangular con abrazaderas, otros 2 bultos + venas. Las tuberias/conducto cruzan de borde a borde (continuas al repetirse). Rodapie y cornisa de metal. Hay una ranura de 2 cm entre paneles (el nucleo mide 0.99 de ancho, rodapie/cornisa 1.00) que da junta visible. Escala X 0.8-1.25 solo deforma los bultos un poco (aceptable) y la pieza esta pensada para escalar solo en X. El mesh respeta 0.12 de grosor (sin salientes).
- **Lab_Block**: camara de cuarentena con 4 tanques criogenicos en las esquinas (cristal + liquido verde emisivo), ventanas frontal/trasera con espécimen (M_BloomSkin) dentro, escotilla con rueda en +X, panel de control en -X, tuberias en los laterales, techo con ranuras y baliza roja. Visible por los 4 lados. Los tanques usan cristal + liquido interior: M_LabGlass debe ser transparente para verlo.
- **Lab_Platform**: borde con 12 luces (rojo/verde), plataforma con 12 nervios radiales (relieve 4 cm, cima Y=0.50) y tapa central. Tapa superior plana Y=0.46; los nervios son decorativos.
- **Lab_Crate**: etiqueta de aviso ambar al frente (-Z), 2 cierres, luz roja y un bulto organico en la cara +X.
- **Lab_Table**: soporte con 6 tubos de ensayo (cristal + liquido), 2 vasos, bandeja con muestra organica, estante inferior.
- **Lab_ServerRack**: 8 unidades en la cara frontal (-Z) con LEDs verde/rojo emisivos, asa, luz roja de estado; la cara trasera tiene rejillas y cables.
- **Lab_CeilingLamp**: 4 cables hacia el punto de colgado, caja con rejilla de 4x4 celdas, panel emisivo M_LabLight por debajo.

## Cambios / decisiones / riesgos
- La orientacion "frente +Z de Unity" y la mano espejo estan comprobadas con el flujo de `common.py` (Blender -Y frontal -> FBX +Z). No he podido abrir Unity: verifica que Avatar_RightHand cae en +X y que el pulgar apunta a +Z. Si saliera espejado, invertir el signo X al instanciar.
- `Lab_Block`: interprete "3.17 x 3 x 2.98" como X=3.17, Y(alto)=3.00, Z=2.98. Si el alto real es 2.98 y el fondo 3.0, basta con rotar/escalar ~0.7%.
- `Lab_FloorTile` y `Lab_CeilingLamp`: los pivotes estan en la cara superior y arriba respectivamente (no en el centro de volumen) para que se coloquen a Y=0 y en el techo directamente.
- `Lab_Table` mide 0.88 de alto contando los tubos (el tablero esta a 0.70).
- Con el exportador del proyecto (sin `bake_space_transform`) los objetos pueden venir con una rotacion de -90 grados en X en el nodo FBX; Unity la compensa en la importacion (igual que BatteryStation). Dejar "Bake Axis Conversion" desactivado, como en los modelos existentes.
- El FBX no lleva emision: sin configurar la emision en Unity (M_AvatarAccent, M_LabLight, M_LabLED, M_LabLEDGreen, M_LabLiquid) las piezas se veran apagadas.
- Presupuestos: todos cumplen (ver tabla). Triangulos totales Player_CDC = 2268.
- Overdraw: M_LabGlass transparente en Quest puede costar; Lab_Block usa 4 tanques + 2 ventanas. Si hay problemas, hacerlo opaco o cutout.
- Los modelos no tienen UVs ni texturas (materiales planos, igual que el resto del proyecto); si se quiere lightmapping habra que desplegar UVs en Unity (Generate Lightmap UVs).

## Renders (BlenderSource/renders)
Player_CDC_front/34/back/head_front/head_34, Player_CDC_hands_34/side, Player_CDC_handR_a/b/c, y por cada Lab_*: `_front`+`_34` (WallPanel tambien `_back`; FloorTile `_top`+`_34`).

## Archivos
Assets/DarkShot/Models/*.fbx (10), BlenderSource/blend/*.blend (10), BlenderSource/scripts/{common,mpcommon,player,labkit,make_mp,verify_mp,render,render_mp}.py. Regenerar todo: `python3 make_mp.py`; verificar: `python3 verify_mp.py`; render: `python3 render_mp.py Lab_Block Player_CDC ...`.
