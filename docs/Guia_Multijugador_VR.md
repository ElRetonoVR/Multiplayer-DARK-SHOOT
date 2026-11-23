# Guía para crear una experiencia multijugador VR (Photon PUN 2)

> Texto extraído de `Guia_para_crear_una_experiencia_multijugador_VR.docx` (Javier Esteban Ochoa Raygoza). Las imágenes no se incluyen.


By: Javier Esteban Ochoa Raygoza  
4 de abril de 2026

Índice

[**Introducción 3**](#introducción)

[**Fundamentos Previos 4**](#fundamentos-previos)

> [Unity 4](#unity)
>
> [C# 4](#c)
>
> [scripts 5](#scripts)
>
> [Variables y tipos de datos 5](#variables-y-tipos-de-datos)
>
> [Realidad Extendida 6](#realidad-extendida)
>
> [Realidad Virtual 6](#realidad-virtual)
>
> [Realidad Aumentada 6](#realidad-aumentada)
>
> [Realidad Mixta 7](#realidad-mixta)

[**Networking 8**](#networking)

> [Multijugador 8](#multijugador)
>
> [¿Cómo funciona? 8](#cómo-funciona)
>
> [Conceptos Clave 9](#conceptos-clave)
>
> [Cliente y servidor 9](#cliente-y-servidor)
>
> [Cliente 9](#cliente)
>
> [Servidor 9](#servidor)
>
> [Modelos principales 9](#modelos-principales)
>
> [Peer-to-peer (P2P) 9](#peer-to-peer-p2p)
>
> [Cliente-servidor 9](#cliente-servidor)
>
> [Remote Procedure Call (RPC) 10](#remote-procedure-call-rpc)
>
> [Problemas típicos 10](#problemas-típicos)
>
> [Latencia 10](#latencia)
>
> [Sincronización 10](#sincronización)
>
> [La 8va capa del modelo OSI 10](#la-8va-capa-del-modelo-osi)
>
> [Photon Engine 11](#photon-engine)
>
> [Photon PUN 2 11](#photon-pun-2)

[**Configuración del entorno 12**](#configuración-del-entorno)

> [Configuración Realidad Extendida 12](#configuración-realidad-extendida)
>
> [Configuración Multijugador 23](#configuración-multijugador)
>
> [Resultados 42](#resultados)

[**Bibliografía 43**](#bibliografía)

# ***Introducción***

El desarrollo de experiencias multijugador en realidad virtual puede ser complejo, ya que combina la interacción inmersiva de la VR con la sincronización de múltiples jugadores en tiempo real.

Esta guía está pensada como un tutorial paso a paso para desarrolladores que deseen crear una experiencia multijugador en realidad virtual y sus variantes, utilizando el motor gráfico Unity y un kit de desarrollo de software (SDK) como Photon Engine. Está especialmente orientada a quienes ya cuentan con conocimientos básicos en programación con C# y desarrollo dentro de Unity, así como nociones fundamentales de realidad virtual.

Al finalizar esta guía, el lector será capaz de desarrollar un juego multijugador en realidad virtual, logrando conectar múltiples jugadores en una misma sala, sincronizar avatares VR (como la cabeza y las manos) y comprender los principales retos que implica la integración de realidad virtual con sistemas de red en tiempo real.

Al finalizar, tendrás un proyecto funcional de un juego multijugador en realidad virtual, además de una base sólida para entender y resolver problemas comunes al trabajar con networking en VR.

# ***Fundamentos Previos***

## **Unity**

Unity es un motor de desarrollo de videojuegos en 2D y 3D, utilizado para crear juegos para PC, móvil, consolas y realidad virtual/aumentada. Permite diseñar, programar con C# y renderizar los juegos en tiempo real, siendo ideal tanto para principiantes como profesionales.

La ventaja que cuenta Unity es que cuenta con una comunidad muy grande, por lo que se cuenta con un gran ecosistema para cada caso de uso, como bien se dice en la página oficial de Unity “Cualquiera puede crear con Unity” ya que cuenta con un sistema de programación orientada a objetos, teniendo un sistema y una interfaz demasiado fácil de utilizar con la simplicidad de arrastrar y soltar.

Las principales competencias de Unity son Unreal Engine y Godot Engine, el primero está enfocado en gráficos fotorealistas de alto rendimiento e impacto, siendo un competidor en los aspectos de juego triple A y juegos en 3D, por otra parte, Godot Engine está más caracterizado en los juegos Indies y en 2D gracias a su motor ligero y rápido.

Unity lleva siendo el estándar para la implementación de juegos de realidad virtual y aumentada, contando con una integración directa de OpenXR, siendo una API para desarrollar aplicaciones de VR, AR y MR (Realidad Mixta), y con un catálogo grande de assets listos para su implementación en la tienda de Unity, logrando el equilibrio perfecto de fácil de aprender y desarrollo rápido, que a diferencia de sus competidores directos, estos tienen más problemas y limitaciones para este tipo de juegos.

### **C#**

C# (C Sharp) es el lenguaje de programación fundamental que se utiliza para programar en Unity, no es necesario aprenderlo para utilizar el motor gráfico, sin embargo si se requiere tener conocimientos de esta misma para ser mejor un desarrollador en Unity.

Los conceptos básicos de C# en Unity incluyen el uso de los scripts, variables y métodos fundamentales como lo son los siguientes:

#### **scripts**

Los scripts son archivos C# que se añaden a los GameObjects (objetos) en la escena para darles comportamiento, heredan de la clase MonoBehaviour siendo la clase fundamental para los scripts de Unity, Heredar de ella permite controlar la lógica del juego y los eventos en el motor, obteniendo acceso a las propiedades de los objetos y sus ciclos de vida como lo son los siguientes:

| **Ciclo de vida** | **¿Qué hace?**                                                      | **Ejemplo**                                                                   |
|-------------------|---------------------------------------------------------------------|-------------------------------------------------------------------------------|
| Start()           | Se ejecuta una sola vez al iniciar el juego o al activar el objeto. | Cuando requieres inicializar las variables al inicio del juego.               |
| Update()          | Se ejecuta en cada frame mientras el objeto esté activo.            | Siendo utilizado normalmente para la lógica de movimiento, como la velocidad. |

#### **Variables y tipos de datos**

Las variables en Unity son contenedores para almacenar datos como la velocidad, vida, texto, en scripts C#, normalmente siendo modificables desde el inspector, aunque se pueden declarar public para observarlas desde el inspector o private para ocultarlas.

<table>
<colgroup>
<col style="width: 33%" />
<col style="width: 33%" />
<col style="width: 33%" />
</colgroup>
<thead>
<tr class="header">
<th><strong>Variable / tipo de dato</strong></th>
<th><strong>¿Qué hace?</strong></th>
<th><strong>Ejemplo</strong></th>
</tr>
<tr class="odd">
<th>int</th>
<th>Números enteros</th>
<th>Almacenar el daño de un personaje con un int: 10.</th>
</tr>
<tr class="header">
<th>string</th>
<th>Texto</th>
<th>Mostrar un cartel que diga “Hola mundo”</th>
</tr>
<tr class="odd">
<th>float</th>
<th>Números decimales</th>
<th><p>La velocidad de una objeto en el juego con un float: 10.5f,</p>
<p>la f al final es obligatorio</p></th>
</tr>
<tr class="header">
<th>bool</th>
<th>Valores verdadero/falso</th>
<th>Una puerta que solo se abre si el bool es true y se cierra si el bool es false</th>
</tr>
<tr class="odd">
<th>Vectores</th>
<th><p>Estructura para almacenar coordenadas x, y, z.</p>
<p>Vector2, Vector3</p></th>
<th>Normalmente utilizado para modificar los valores de rotación, posición y escalas de un GameObject</th>
</tr>
</thead>
<tbody>
</tbody>
</table>

## **Realidad Extendida**

La Realidad Extendida (XR) es un término que engloba las tecnologías que combinan el mundo físico con entornos digitales para crear experiencias interactivas. Estas tecnologías pueden utilizar dispositivos como visores, gafas o teléfonos móviles, y tienen aplicaciones en sectores como la industria, la educación, la medicina y el entretenimiento.

XR incluye la realidad virtual (VR), la realidad aumentada (AR) y la realidad mixta (MR), cada una con distintos niveles de inmersión e interacción entre el entorno real y el virtual.

### **Realidad Virtual**

La Realidad Virtual (VR) es una tecnología que sumerge completamente al usuario en un entorno digital generado por computadora, bloqueando la percepción del mundo real mediante el uso de visores y sensores de movimiento.

La interacción se realiza mediante controles o seguimiento de manos, permitiendo al usuario desplazarse e interactuar dentro del entorno virtual.

### **Realidad Aumentada**

La Realidad Aumentada (AR) superpone elementos digitales sobre el entorno real a través de dispositivos como teléfonos móviles o gafas especiales.

A diferencia de la VR, la AR no reemplaza el entorno físico, sino que lo complementa con información o elementos virtuales en tiempo real.

### **Realidad Mixta**

La Realidad Mixta (MR) combina características de la realidad virtual y la realidad aumentada, permitiendo que los objetos virtuales no solo se integren en el entorno real, sino que también interactúen con él en tiempo real.

Esto significa que los elementos digitales pueden responder a superficies, posiciones y objetos del mundo físico, generando una experiencia más avanzada e interactiva.

La diferencia principal entre estas tecnologías radica en el nivel de inmersión: VR sustituye completamente el entorno real, AR lo complementa, y MR lo integra con interacción directa.

En esta guía se trabajará principalmente con realidad virtual, utilizando Unity y herramientas de interacción XR, aunque los conceptos presentados pueden aplicarse a otras tecnologías dentro de la realidad extendida. Después de todo, no existe una limitante para utilizar una herramienta.

# ***Networking***

El networking en videojuegos es, en pocas palabras, todo lo que permite que varios jugadores compartan una misma experiencia en tiempo real a través de una red (generalmente internet). Es la base de cualquier juego multijugador.

## **Multijugador**

El multijugador es una modalidad de juego donde dos o más personas logran interactuar simultáneamente dentro de la misma partida, puede ser de forma presencial (local) dentro de un mismo dispositivo, o en línea a través de internet, cada usuario con su propio dispositivo, el multijugador permite competir o colaborar en mundos virtuales, ya sea en equipo o de forma individual.

## **¿Cómo funciona?**

Cuando juegas un juego multijugador, no importa el genero, tu dispositivo no está ejecutando el dispositivo de forma aislada, sino que envía la información, como lo son la posición del jugador o sus acciones, y también recibe la información, como lo son la posición de otros jugadores, sus acciones e incluso eventos del mundo; todo ocurriendo en cuestión de milisegundos y de forma constante, lo que permite la interacción en tiempo real en los jugadores.

La siguiente figura muestra cómo los jugadores envían información al servidor, y este se encarga de distribuir el estado actualizado a todos los clientes.

## **Conceptos Clave**

### **Cliente y servidor**

En juegos multijugador por lo general se utiliza un servidor central como autoridad para gestionar el estado del juego y la lógica, mientras que los clientes a través de sus dispositivos envían entradas, como lo movimientos o sus acciones, y la renderización de los gráficos; y es el servidor quien valida esas acciones para sincronizar la misma información a todos los jugadores.

#### **Cliente**

Es el software en el ordenador/consola del jugador. Su función es captar la entrada (teclado/ratón), renderizar los gráficos y mostrar el mundo del juego. Envía comandos al servidor y recibe actualizaciones de estado.

#### **Servidor**

Es la máquina o software que ejecuta la lógica principal del juego. Recibe las acciones de los clientes, valida si son posibles (por ejemplo, si un jugador no atraviesa paredes) y envía el estado actualizado del juego de vuelta a los clientes.

### **Modelos principales**

#### **Peer-to-peer (P2P)**

Es una arquitectura descentralizada donde los jugadores se conectan directamente entre sí sin un servidor central, compartiendo datos de red (movimiento, disparos) directamente. Suele funcionar generalmente cuando un jugador actúa como el servidor “host” y los demás jugadores se conectan a él.

Aunque suele tener la ventaja de tener menores costos en la infraestructura para los desarrolladores y que varios jugadores utilicen este modelo para partidas rápidas o cooperativas con sus amigos, si el “host” llega a abandonar la partida, la partida suele interrumpirse, además de que cuenta con que el “host” tenga ventaja de red y pueda facilitarse con el uso de trampas.

#### **Cliente-servidor**

Es el modelo más usado en la actualidad ya que los servidores son cruciales en juegos multijugador para manejar la persistencia del mundo y la interacción sincrónica entre usuarios. Conectando múltiples jugadores (clientes) a una máquina central (servidor).

A diferencia de del Peer-to-peer, en esta arquitectura es muy difícil hacer trampa, ya que el servidor es la autoridad máxima el cual procesa toda la lógica y el estado del mundo

### **Remote Procedure Call (RPC)**

La llamada a procedimiento remoto (en inglés, Remote Procedure Call, RPC) es un programa que utiliza una computadora para ejecutar código en otra máquina remota sin tener que preocuparse por las comunicaciones entre ambas, de forma que parezca que se ejecuta en local

Se usa para eventos específicos y no continuos, como disparar, abrir puertas, chatear o activar efectos visuales.

### **Problemas típicos**

#### **Latencia**

La latencia es el tiempo de retraso que tardan los datos a viajar desde el dispositivo al servidor, lo que se le considera como lag, principalmente causada por la distancia que se tiene con el servidor, una conexión inestable a internet o estar conectado por wifi y no por cable, gracias a la latencia es que se puede apreciar a jugadores “teletransportandose” entre la escena.

#### **Sincronización**

La sincronización se le conoce como el proceso en el que todos los jugadores vean la misma cosa, por ejemplo, si un jugador mueve un objeto y los demás jugadores ven como ese mismo objeto es manipulado por el jugador que lo mueve, logrando manipular el estado del juego como lo son la posición, acciones y demás.

La sincronización está relacionada a la latencia, ya que esta es la que mide en cuánto tiempo se va a terminar realizando la acción, por lo que si se tiene una latencia alta entonces va a haber una desincronización entre los jugadores.

#### **La 8va capa del modelo OSI**

Con la octava capa del modelo OSI me refiero a todos los problemas que puedan ser causados por el usuario, como lo es el troleo, siendo la acción de jugadores que se dedican a molestar a otros solo por diversión, arruinando la experiencia de los demás.

De igual forma si el videojuego multijugador cuenta con chat de voz o chat de texto, es común encontrarse a personas que realizan ciberacoso, diciendo insultos, intimidando e incluso amenazando por mensaje o texto.

## **Photon Engine**

Photon Engine es un conjunto de servicios y herramientas diseñados para implementar funcionalidades multijugador en aplicaciones y videojuegos. Permite sincronizar jugadores, objetos y eventos en tiempo real a través de internet sin necesidad de desarrollar un servidor desde cero.

Photon utiliza un modelo basado en la nube, lo que facilita la conexión entre usuarios y la gestión de salas (rooms), haciendo posible crear experiencias multijugador de forma rápida y escalable.

### **Photon PUN 2**

Photon PUN 2 (Photon Unity Networking 2) es una de las soluciones más utilizadas dentro de Photon para Unity. Está diseñado para ser fácil de usar e integrarse directamente con el flujo de trabajo del motor. Permite crear y unirse a salas, sincronizar jugadores y objetos, enviar eventos entre usuarios, manejar instancias en red y más.

Es ideal para prototipos, proyectos educativos y experiencias multijugador de complejidad media.

Photon también cuenta con otras soluciones como Photon Fusion, la cual está orientado a juegos más rápidos y modernos, con mejor rendimiento y menor latencia, Photon Quantum diseñado para juegos competitivos con lógica determinista, como MOBAs o juegos de estrategia.

En esta guía se utilizará Photon PUN 2 debido a su facilidad de uso e integración con Unity, lo que lo convierte en una opción ideal para aprender los fundamentos del desarrollo multijugador en experiencias de realidad virtual. Siendo gratuito hasta 20 usuarios simultáneos y facilitando la exportación a múltiples plataformas.

| **Ventajas de Photon**                  | **Desventajas de Photon**                                                          |
|-----------------------------------------|------------------------------------------------------------------------------------|
| No requiere crear un servidor propio    | Plan gratuito limitado para los usuarios                                           |
| Fácil de integrar con Unity             | Dependencia de servicios externos                                                  |
| Escalable mediante servicios en la nube | PUN 2 está siendo reemplazado gradualmente por soluciones más modernas como Fusion |
| Amplia documentación y comunidad        | Menor control comparado con servidores dedicados                                   |
| Ideal para VR Multijugador              | Otras herramientas como Netcode ofrecen mejor control total.                       |

# ***Configuración del entorno***

## **Configuración Realidad Extendida**

Para esta configuración vamos a utilizar los Building Blocks de Meta Quest, son herramientas preconfiguradas para desarrolladores en Unity que permite implementar las funciones avanzadas de la realidad extendida rápidamente sin programar desde cero.

Para instalarlo vamos a crear un nuevo proyecto en Unity utilizando el template Universal 3D URP, procura utilizar una versión LTS la cual significa Long Term Support (LTS) siendo de las versiones más estables:

Una vez el proyecto se haya creado verás la interfaz del motor gráfico, para continuar con la configuración del sistema es importante modificar el perfil del proyecto, por lo que en las pestañas de arriba dirígete a file → Build profiles o bien presionar la combinación de letras ctrl + shift + b

Se abrirá la siguiente pestaña, verás múltiples opciones como Windows, Android e incluso consolas como PlayStation 5, estos son archivos assets que contienen configuraciones de construccion especificas, permitiendo definir perfiles únicos para todas las opciones que se pueden visualizar, en este caso apretaremos el botón que dice Add Build Profile, para tener un mejor control sobre nuestro perfil:

Se abrirá la siguiente pestaña, en este caso utilizaremos la opción que dice Meta Quest para poder trabajar con el perfil y configuraciones de la misma, una vez abierta la opción para el Meta Quest, navegar hasta abajo con el scroll del mouse o bien con la barra navegadora del lado derecho y seleccionar el botón que dice Select All, esto para utilizar todos los paquetes que el perfil permite, pueden cambiar el nombre que gusten y terminamos dándole click a Add Build Profile que está del lado inferior derecho:

Una vez instalado la pestaña de Build Profile probablemente se vea de la siguiente manera, simplemente dale click al botón Enable Platform:

Una vez termine la instalación del perfil y el perfil que creamos marque como activo, podemos cerrar la pestaña de Build Profiles, probablemente te salga una pestaña de arreglar algunos problemas, no te preocupes por eso justo íbamos a ese paso, entramos a la opción de Project Settings que se encuentra en la pestaña de arriba Edit → Project Settings:

Se abrirá la siguiente pestaña con múltiples opciones del proyecto, viaja hasta la opción de XR Plug-in Management y verás dos opciones, una con el símbolo de pc y la otra con los símbolos de meta y android, es importante que en las dos opciones tengas las siguientes casillas marcadas que dicen OpenXR y Meta XR feature group:

Es muy probable que se te quiera instalar un programa llamado Meta XR Simulator, si cuentas con unos cascos Meta no es necesario que lo instales, caso contrario hazlo, para esta guía como contamos con cascos Meta Quest 3 no lo instalaremos.

Una vez hecho esto volvemos a abrir Project Settings y entramos a la opción de Meta XR, donde se darán cuenta que hay más errores, hay que presionar fix all y apply all para solucionarlos:

Una vez realizado todo los pasos anteriores, ahora si podemos pasar a la construcción de la escena, primero que nada es importante que borremos la Main Camera, ya que esta ya no nos va a servir puesto que utilizaremos los Building Blocks de Meta y en este ya se encuentra nuestra Main Camera configurada. Para esta guía simplemente voy a poner una base sencilla donde el jugador podrá estar y un objeto la cual el jugador podrá agarrar:

Como pueden apreciar en la imagen, borramos Main Camera, cree un suelo y un cubo para agarrar, es importante que vean que cree una carpeta para poner los materiales que le dieron color a mis objetos en escena, es importante clasificar todo por carpetas para tener todo mejor ordenado, más que nada por las buena prácticas y para no perderse a la hora de encontrar un asset que ocupamos más adelante.

En las pestañas de arriba verás una opción nueva que dice Meta, dentro de ella seleccionamos la opción tools y nos dirigimos a Building Blocks:

Dentro de Building Blocks encontraremos la siguiente pestaña donde se encuentran todas las configuraciones ya creadas previamente por el equipo de Meta, de esta manera crearemos de forma más fácil nuestros juegos VR:

Dentro de los building blocks hay una opción que dice All Building Blocks, o en el mismo buscador podemos buscar los bloques que ocupamos, en este caso seleccionaremos el building block llamado Camera Rig, la cual será nuestra cámara previamente configurada para que el jugador pueda observar el mundo con los cascos

Una vez le dimos al click de añadir veran que en la escena ya se coloco una camara, de igual forma seleccionaremos los Building Blocks Controller Tracking e Interaction Rig, esto nos permitirá poder movernos a través de la plataforma:

De esta forma si colocamos nuestros cascos y entramos a la computadora con ellos y dándole play al juego, podrán notar que ya podrían caminar y dar vuelta por todo el “mapa” que tenemos en la escena, sin embargo aún no podremos interactuar con el cubo verde, para ésto tenemos que volver a abrir los Building Blocks y arrastrar la opción Grab Interaction a nuestro cubo verde, de esta forma podrán agarrar el cubo verde y empezar a jugar con el:

Si vuelven a probar el juego veran que ahora si podran manipular el cubo, sin embargo se mantendrá en el aire todo el tiempo ya que eso se tiene que configurar con las propiedades de Rigidbody, sin embargo esto queda a decisión de cada uno:

Ya con esto tenemos configurado por completo la parte de Realidad Mixta, utilizamos los Building Blocks para facilitarnos con las configuraciones de la realidad virtual, te recomiendo experimentar un poco más para que te acostumbres con las opciones que brinda unity junto con los Building Blocks

## **Configuración Multijugador**

Para esta configuración primero que nada es necesario entrar a la página de Photon Engine [<u>https://www.photonengine.com/</u>](https://www.photonengine.com/), dentro de esta página es importante loguearnos para poder utilizar sus servicios

Seguimos todos los pasos para crear una cuenta en photon, terminando de configurar la cuenta a través de un correo electrónico que nos llegara a la cuenta a la cual registramos, una vez realizando todos los pasos nos logueamos en photon engine, veremos una opción que dice create new app, le picaremos ahi:

Entrando en esa opción tendrás el siguiente “formulario”, para esta guía utilizaremos la opción de Multiplayer Game en Select Application Type, en Select Photon SDK como ya mencionamos con anterioridad, seleccionaremos la opción Pun, y ya por ultimo ponemos el nombre que deseen, una vez terminando de llenar los campos le damos a create:

Una vez creado te mandara a la siguiente pestaña, te recomiendo darle una ojeada para tener una idea de sus limitaciones, por el momento lo único que nos importa ahora es el campo de App ID, ya que lo ocuparemos más adelante:

De regreso a Unity tendremos que descargar la API para utilizar el multiplayer, para esto tenemos que viajar a la asset store de Unity, la forma más fácil de llegar ahí es en las pestañas de arriba seleccionar windows → Package Management → Asset Store:

Una vez abierta la página de la Asset Store lo único que hay que hacer es primero que nada loguearse:

Una vez logueado es importante que en el buscador de la asset store busquen PUN 2 FREE

Una vez seleccionado veran que les manda a una nueva pagina, ahi seleccionan el boton que dice Open in Unity y simplemente aceptan todas las alertas que les sueltan:

Les regresara a Unity abriendo la pestaña Package Manager con un My assets y descargamos el asset ya mencionado:

Terminando de descargarla y aceptar las alertas es muy probable que te salte la siguiente pestaña, dentro de esa pestaña colocaremos la App ID que habíamos guardado previamente y apretamos a la opcion que dice Setup Project:

Con eso nos avisará que la instalación y la sincronización con nuestro “servidor” ya estará listo, y ya podríamos pasar a la instalación y configuración del Photon para el multijugador.

En nuestra escena creamos un objeto vacío NetworkManager que será el encargado de tener los scripts para la conexión multijugador:

A su vez en assets creamos un MonoBehaviour Script al que le llamaremos de igual manera NetworkManager, dentro de la misma colocaremos el siguiente script, donde estamos primero llamando las librerías de Photon, de igual forma notaran que el MonoBehaviour pasó a ser MonoBehaviourPunCallBacks, esto para llamar las clases desde ahí, contamos con una lista donde estaríamos guardando todos los SpawnPoints, despues en start() inicializamos el PhotonNetwork para poder unirnos a un sala, en caso de que no exista una sala es gracias al JoinOrCreateRoom() en el que la creamos, una vez creado llamamos a spawnear lo que es PhotonNetwork.Instantiate(

"PlayerVR",

spawnPoint.position,

spawnPoint.rotation

);

Con esto se hace spawn a un prefab llamado PlayerVR en la posicion y rotacion de los spawnPoint, pero ya llegaremos a eso, este script lo ponemos dentro del objeto que creamos hace poco llamado NetworkManager:

Para los SpawnPoints creamos un objeto vacío llamado SpawnPoints o como gusten, dentro de este objeto creamos todos los Spawn que queramos, en mi caso yo nada más seleccione dos, y los arrastramos dentro del inspector del NetworkManager:

De esta forma ya nada mas faltaria crear al jugador, creamos un objeto vacio llamado PlayerVR, ojo, se tiene que llamar igual que como inicializamos el spawn del jugador, como en el script tenemos en el PhotonNetwork.Instantiate el “PlayerVR” se tiene que llamar de la misma manera.

Dentro del objeto llamado PlayerVR creamos un avatar super sencillo, esto para diferenciar y saber que hay jugadores dentro de la sala, yo lo hice de la siguiente manera puedes copiarlo sin problema alguno:

#  

Realizado eso, seleccionamos el resto de los objetos del avatar y volvemos a buscar los scripts, pero solo buscamos el Photon Transform View, ya que el Photon View solo debería estar en el objeto padre, hay que asegurarnos que todos tengan las casillas de Position y Rotation activas:

Para sincronizar el Building Block al avatar es necesario utilizar el siguiente script, crearemos un script llamado VRLocalToNetworkMapper, con este script lo que haremos es sincronizar el BuildingBlock que está en la escena con el avatar:

Colocaremos este script dentro del objeto PlayerVR, y solo añadiremos los objetos creados dentro del avatar head con head, left hand con left hand y right hand con right hand:

En la parte donde dice Local (scene) no pondremos nada, ya que nos vamos a dirigir a la carpeta llamada Photon → PhotonUnityNetworking → Resources:

Seleccionaremos todo el objeto de PlayerVR y lo arrastraremos hasta la carpeta que acabamos de abrir, de esta forma crearemos un prefab, y como Photon solo puede crear cosas en prefab dentro de la carpeta de Resources, lo mantendremos ahi:

Una vez creado el prefab y que esté en Resources, es importante lo siguiente, Borrar el Player VR en la escena pero mantener intacta lo que es el Building Block, de esta forma cada jugador que se conecte entonces podrá tener su propio avatar:

En el objeto del cubo pondremos el componente de Photon Transform View y Photon View para que los demás jugadores vean cómo interactúan con el objeto:

Ya con todo esto el multijugador VR quedó hecho, para probarlo bien pueden hacerlo desde el editor dándole al play y veran como se crea un avatar lo que significa que es un jugador, o hacerle build and run desde edit → build profiles:

Volvemos a Build Profiles a nuestro perfil activo y en Build And Run le damos click para hacer el build a los cascos y tener el apk en nuestros metas, de esta forma podemos probarlo con editor + build:

##  

## **Resultados**

Realizando todos los pasos anteriores tendremos una conexión multijugador en VR funcional, ya es tu turno de crear tu futuro juego exitosos, como pueden observar en la imagen ya contamos con dos jugadores en nuestra sala, el primero probándolo desde el editor y el segundo probándolo desde un build en los cascos, con esto ya podremos crear varias salas con más jugadores.

Recuerda que no hay límites para la programación ya que **si lo puedes imaginar, lo puedes programar**, con esta guía me despido y les deseo la mejor de las suertes para sus futuros proyectos:

# Bibliografía

- *Unity: Desarrolla, implementa y crece \| El motor de juegos líder en el mundo*. (s. f.). Unity. [<u>https://unity.com/es</u>](https://unity.com/es)

- colaboradores de Wikipedia. (2025, 30 junio). *Realidad extendida*. Wikipedia, la Enciclopedia Libre. [<u>https://es.wikipedia.org/wiki/Realidad_extendida</u>](https://es.wikipedia.org/wiki/Realidad_extendida)

- colaboradores de Wikipedia. (2026, 10 abril). *Realidad virtual*. Wikipedia, la Enciclopedia Libre. [<u>https://es.wikipedia.org/wiki/Realidad_virtual</u>](https://es.wikipedia.org/wiki/Realidad_virtual)

- colaboradores de Wikipedia. (2026b, abril 8). *Realidad aumentada*. Wikipedia, la Enciclopedia Libre. [<u>https://es.wikipedia.org/wiki/Realidad_aumentada</u>](https://es.wikipedia.org/wiki/Realidad_aumentada)

- colaboradores de Wikipedia. (2025b, diciembre 28). *Realidad mixta*. Wikipedia, la Enciclopedia Libre. [<u>https://es.wikipedia.org/wiki/Realidad_mixta</u>](https://es.wikipedia.org/wiki/Realidad_mixta)

- colaboradores de Wikipedia. (2026a, marzo 5). *Peer-to-peer*. Wikipedia, la Enciclopedia Libre. [<u>https://es.wikipedia.org/wiki/Peer-to-peer</u>](https://es.wikipedia.org/wiki/Peer-to-peer)

- *Remote Procedure Calls in Unreal Engine \| Unreal Engine 5.7 Documentation \| Epic Developer Community*. (s. f.). Epic Games Developer. [<u>https://dev.epicgames.com/documentation/unreal-engine/remote-procedure-calls-in-unreal-engine?lang=en-US</u>](https://dev.epicgames.com/documentation/unreal-engine/remote-procedure-calls-in-unreal-engine?lang=en-US)

- colaboradores de Wikipedia. (2026e, abril 13). *Llamada a procedimiento remoto*. Wikipedia, la Enciclopedia Libre. [<u>https://es.wikipedia.org/wiki/Llamada_a_procedimiento_remoto</u>](https://es.wikipedia.org/wiki/Llamada_a_procedimiento_remoto)

- *Multiplayer game development made easy \| Photon Engine*. (s. f.). [<u>https://www.photonengine.com/</u>](https://www.photonengine.com/)
