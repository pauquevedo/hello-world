# Dragon Masters 🐉🏹

Juego de lanzamiento por turnos al estilo *arquero vs. arquero*, hecho en **Unity para iOS** con todos los sprites de **Brand 01**.

## Cómo se juega

- **Arrastra hacia atrás** (desde cualquier punto de la pantalla) para apuntar: cuanto más arrastras, más fuerza. Ves el ángulo, el % de potencia y el principio de la trayectoria.
- Mientras apuntas **solo te ves a ti**: el enemigo está fuera de cámara.
- Al **soltar**, la cámara **corre siguiendo el arma** hasta el otro lado, donde está el enemigo.
- Luego responde el enemigo: la cámara se queda con él, lanza, y la cámara vuelve volando hacia ti siguiendo su arma.
- **Headshot** = doble daño. Los aciertos dan monedas; ganar el combate da un bonus.
- Tu tiro anterior queda marcado con puntos fantasma para que puedas corregir.
- 9 enemigos (slime → Rey Sapo), cada uno con su escenario; al terminarlos empieza otra ronda más difícil.
- 18 dragones jugables (9 disfraces + 9 elementales), cada uno con su arma. Se desbloquean con monedas.

## Qué sprites del Brand 01 se usan y dónde

| Hoja | Uso en el juego |
|---|---|
| `dragon_costumes`, `dragon_elements` | Los 18 dragones jugables |
| `dragon_mascot` | Mascota del menú y icono de la app |
| `dragon_expressions` | Pantallas de victoria/derrota y pausa |
| `enemies` | Los 9 rivales |
| `weapons` | Armas que se lanzan (espada, hacha, martillo, lanza, bastón, escudo) y cañones de decorado |
| `items_collectibles` | Monedas, corazón de vida, cofre, llave de bloqueado y objetos lanzables (poción, gema, estrella, regalo…) |
| `effects_vfx` | Impactos, explosiones, confeti, nubes y proyectiles mágicos (rayo, fuego, remolino, salpicadura) |
| `buildings`, `environment_props` | Escenarios de fondo, plataformas de roca |
| `vehicles_boats` | Barcos en el mar y dirigibles en el cielo |
| `characters_npcs` | Público animando al fondo |
| `ui_buttons` | Todos los botones, marcos y paneles |

Las hojas originales están en `/assets/sprites`. `Tools/slice_sprites.py` las recorta (3×3), quita el fondo blanco y las guarda en `Assets/Resources/Sprites/<hoja>/<nombre>.png`. Si cambias una hoja, vuelve a ejecutar:

```bash
pip install numpy scipy pillow
python3 DragonMasters/Tools/slice_sprites.py
```

## Abrir y probar

1. Instala **Unity 6 (6000.0 LTS)** desde Unity Hub con el módulo **iOS Build Support**.
2. Unity Hub → *Add project from disk* → elige la carpeta `DragonMasters`.
3. Al abrirse por primera vez se crea sola la escena `Assets/Scenes/Main.unity` y se configuran los ajustes de iOS (horizontal, IL2CPP/ARM64, iOS 13+, icono). Si no, usa el menú **Dragon Masters → 1. Configurar proyecto**.
4. Pon la vista *Game* en una resolución horizontal (p. ej. 2532×1170) y dale a **Play**. En el editor se apunta con el ratón.

## Generar la app para iPhone/iPad

1. Menú **Dragon Masters → 2. Generar proyecto Xcode (iOS)** → crea `Builds/iOS`.
2. Abre `Builds/iOS/Unity-iPhone.xcodeproj` en Xcode (en un Mac).
3. *Signing & Capabilities* → elige tu Team (bundle id: `com.brand01.dragonmasters`, cámbialo si quieres).
4. Conecta el iPhone y dale a **Run**.

También desde terminal (Mac):

```bash
/Applications/Unity/Hub/Editor/6000.0.47f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath DragonMasters -buildTarget iOS -executeMethod DragonMasters.EditorTools.DragonMastersSetup.BuildIOS
```

## Estructura del código (`Assets/Scripts/Runtime`)

Todo se monta por código al arrancar, no hay prefabs que configurar:

- `GameManager` – arranque, cámara, interfaz y cambio de pantallas.
- `Battle` – turnos, apuntado por arrastre, IA enemiga, encuadres y barridos de cámara, HUD, pausa.
- `Projectile` – física balística propia y predicción de trayectoria.
- `Fighter` – luchadores con cajas de cabeza/cuerpo, reacción a golpes y caída.
- `CameraRig` – seguimiento, barrido "corriendo" de un lado a otro y temblor.
- `Scenery` – cielo, nubes, colinas, mar con barcos, edificios, público y props con parallax.
- `Catalog` – dragones, armas, enemigos y escenarios (aquí se ajusta el equilibrio).
- `Screens` – menú, selección de dragón y resultados. `Sfx` – sonidos sintetizados. `Save` – progreso.
