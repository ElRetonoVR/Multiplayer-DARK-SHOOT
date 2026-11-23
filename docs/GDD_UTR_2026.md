# Laser Project — GDD multijugador v1.2 (GDD_UTR_2026)

> Texto extraído de `GDD_UTR_2026.docx`. Las imágenes no se incluyen.

**Game Design Document**

**Laser Project**

**Document version 1.2**

> **Team:**  
> - Ian Abel Zavala Saucedo
>
> \- Javier Esteban Ochoa Raygoza
>
> \- Daniel Gibran Mireles Mora
>
> \- Ahtziri Itzel Segovia Monroy
>
> **Publication Date:**
>
> 01/07/2026  
> **1. Basic Project Information**

\- **Genre:** VR & Cooperative/competitive Shooter.

\- **Devices:** Meta Quest 3.

\- **Modality:** Local Multiplayer (2-4 jugadores).

\- **Estimated duration per game:** 5 -10 minutes.

\- **Graphics engine:** Unity (URP).

\- **Required team equipment:** 5-6 workers.

\- **Timeline:** 4 months.

##  **2. Key Concept (Elevator Pitch)**

Fast-paced laser tag-style VR game played in complete darkness, where the only source of light is the flashlight under your weapon. Shooting consumes the flashlight's energy, creating moments of tension between attacking and seeing. Defeated enemies drop batteries to restore visibility.

##  **3. Principal Mechanics**

###  **Light System/Resources:**

\- **Lantern Mechanics:** The light gets weaker every shot.

\- **Battery recharge:** Defeated enemies can drop 1-3 batteries.

\- **Levels of bright/battery:** 100% (normal), 50% (weak), 10% (almost off), 0% (total dark).

###  **Fight:**

\- **Laser Gun:** No physics ammo, but consumes energy.

\- **Punctuation system:** Scoreboard counting hits, kills, and the score

\- **Haptic Feedback:** Vibrations when shooting and being hit.

### **Movement & VR’s controllers:**

\- **Transportation:** Smooth movement and run.

\- **Interaction:** Grab batteries with your free hands.

\- **UI in game:** Gun Energy Indicator.

##  **4. Progression & goals**

###  **Playing modes:**

\- **All PvP:** Free-for-all (2-4 players).

\- **Team PvP:** 2 teams fighting for the best score.

###  **Replayable elements:**

\- As time goes by, rechargeable batteries for lasers appear in the center.

\- Final results board.

##  **5. Art &Visual Style**

### **Art directions:**

\- **Style:** Low-poly optimized for VR.

\- **Palet:** Dominant darkness with neons (flashlights/effects).

\- **Environment:** Principal arena Futuristic and possibly an abandoned industrial facility.

##  **Priority Assets:**

1\. Weapon with integrated flashlight.

2\. Enemy models.

3\. Batteries.

4\. Particle effects (shots, impacts).

##  **6. Sound & Audio**

###  **Key elements:**

\- **Spatial Audio:** Footsteps, battery recharge, enemy breath

\- **Music:** Tense atmosphere, dynamic intensity.

\- **Feedback auditivo:** Flickering flashlight, shots, and alerts.

##  **7. Development Plan (4 months)**

### **1st month: Basic prototype**

\- Main mechanics working (flashlight that consumes power).

\- Movement and shooting in VR.

\- Basic battery collection system.

\- 1 test room.

### **2nd month: playable content**

\- Scoreboard system.

\- Basic UI (GEI).

###  **3rd month: Multiplayer**

\- Networking (Photon or Netcode).

\- Mechanics balance.

\- Sounds & basic VFX.

###  **4th month: Polishing & launch**

\- Optimization for Quest.

\- Menus and settings.

\- Testing and bug fixing.

\- Final build.

##  **8. Risks & Mitigation**

###  **Identified risks:**

**- Motion sickness:** Mitigate with movement options.

**- Complex networking:** Use established solutions.

**- VR optimization:** Keep polygon count low.

**- Limited time:** Reduced scope, minimum viable mechanics.

##  **9. Tech Requirements**

###  **Min:**

\- 90 FPS.

\- Parties of 4 players.

\- \< 2GB memory.

###  **Technologies:**

\- Meta SDK

\- Unity XR Interaction Toolkit

\- Solution networking (Photon, Netcode)

\- Oculus Integration SDK

##  

##  **10. Unique value proposition**

**1. Strategic tension:** Shoot vs sight.

**2. Forced cooperation:** On occasions you’ll need allies to avoid being blinded.

**3. Quick access:** Short games, ideal for VR.

4\. **Simple but deep:** Easy to learn, hard to master.

*\*Document for revision and approval - Date: \_\_\_\_\_\_\_\_\_\_\_\_\_\_\**
