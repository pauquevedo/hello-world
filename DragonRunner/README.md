# Dragon Runner

Runner infinito 2D para Unity con el dragón naranja y los sprites de **Brand 01**.

El dragón corre solo y el mundo va cada vez más rápido:

- **Tocar / clic / espacio**: saltar.
- **Tocar otra vez en el aire**: segundo salto (aleteo).
- **Mantener pulsado mientras cae**: planear con las alas (se pone el disfraz de aviador).
- **Caer encima** de slimes, setas, mapaches, robots, esqueletos o gólems los elimina (+25 puntos y rebote).
- Rocas, barriles, vallas, pozos, fantasmas y nubes de tormenta te quitan un corazón (tienes 3).
- Monedas: +10 puntos. Gema: +100. Corazón: recuperas una vida.
- Puntuación = metros + monedas × 10 + enemigos pisados × 25. El récord se guarda en el dispositivo.
- **Esc / P** o el botón de pausa: pausa.

## Cómo abrirlo en Unity (5 minutos)

1. En **Unity Hub** → *New project* → plantilla **Universal 2D** (Unity 6). Nómbralo `Dragon Runner`.
2. Copia el contenido de esta carpeta `DragonRunner/Assets/` dentro de la carpeta `Assets/` del proyecto nuevo
   (se añaden `Scripts/` y `Resources/Sprites/`).
3. Vuelve a Unity, espera a que importe y pulsa **Play** en la escena `SampleScene`.

No hay que montar nada en la escena: `GameBootstrap` crea cámara, luz, decorado, dragón e interfaz al darle a Play.

Para móvil: *File → Build Profiles* → Android o iOS (instala el módulo desde Unity Hub si no lo tienes).

## Estructura

| Archivo | Qué hace |
|---|---|
| `Scripts/GameBootstrap.cs` | Arranca el juego automáticamente en cualquier escena. |
| `Scripts/GameManager.cs` | Estados (portada, partida, pausa, game over), velocidad, generación de obstáculos/monedas, colisiones, decorado con parallax, sonidos. |
| `Scripts/DragonPlayer.cs` | Salto, aleteo, planeo, animación por rebote y caras según lo que pasa. |
| `Scripts/Scroller.cs` | Todo lo que se mueve hacia la izquierda. |
| `Scripts/GameUI.cs` | Portada, marcador, pausa y game over con los botones de Brand 01 (uGUI). |
| `Scripts/TapInput.cs` | Teclado, ratón y táctil; funciona con el Input System nuevo y el antiguo. |
| `Scripts/FxBurst.cs` | Efectos (estrellas, confeti, impacto, humo). |
| `Scripts/Art.cs` | Carga de sprites y sonidos generados por código. |
| `Resources/Sprites/` | 109 sprites recortados con fondo transparente (generados con `tools/slice_sprites.py`). |

Los valores de juego (velocidad, gravedad, salto, vidas…) están como constantes al principio de
`GameManager.cs` y `DragonPlayer.cs`.

## Pendiente / ideas

- Sonidos y música de verdad (ahora son pitidos generados por código).
- Animación de correr: Brand 01 no trae frames de carrera, así que el dragón bota.
- Tienda de disfraces con las monedas (los 9 disfraces de `dragon_costumes` ya están recortados).
- Escenarios por zonas (mar con barcos, castillo…) usando `vehicles_boats` y `buildings`.
