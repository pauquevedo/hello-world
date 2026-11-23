# Sprites

Hojas de sprites del juego (generadas con Gemini). Cada hoja es un PNG con una
cuadrícula de 3×3 elementos sobre fondo blanco (aún no recortados ni con
transparencia), salvo `dragon_mascot.png`, que es un solo personaje.

Origen: carpeta **"Brand 01"** de Google Drive.

| Archivo | Contenido (por filas, de izquierda a derecha) | Tamaño | Nombre original en Drive |
|---|---|---|---|
| `dragon_mascot.png` | Dragón naranja protagonista, saludando (personaje único) | 1792×2388 | Gemini_Generated_Image_4n5g8x4n5g8x4n5g.png |
| `dragon_expressions.png` | Dragón: saludando, señalando, celebrando / triste, durmiendo, sorprendido / pensando, enfadado, guiñando | 2048×2048 | Gemini_Generated_Image_aab8edaab8edaab8.png |
| `dragon_costumes.png` | Dragón disfrazado: pirata, aviador, cyber / rey, chef, astronauta / ninja, caballero, mago | 2048×2048 | Gemini_Generated_Image_8h9qr88h9qr88h9q.png |
| `dragon_elements.png` | Dragones: agua, planta, estrella / hielo, piedra, rayo / rojo adulto, anciano, cría en huevo | 2048×2048 | Gemini_Generated_Image_qqy5uiqqy5uiqqy5.png |
| `characters_npcs.png` | Aventurero, caballero, pirata / mago, princesa, herrero / mercader, aviadora, granjero | 2048×2048 | Gemini_Generated_Image_f9s5iff9s5iff9s5.png |
| `enemies.png` | Slime, seta, fantasma / esqueleto pirata, mapache ladrón, robot / nube de tormenta, gólem, rey sapo | 2048×2048 | Gemini_Generated_Image_rtw1uwrtw1uwrtw1.png |
| `items_collectibles.png` | Moneda, montón de monedas, gema / estrella, corazón, poción / llave, cofre del tesoro, regalo | 2048×2048 | Gemini_Generated_Image_5o3vw55o3vw55o3v.png |
| `weapons.png` | Espada, escudo, arco y flecha / hacha, martillo, bastón mágico / cañón, tirachinas, lanza | 2048×2048 | Gemini_Generated_Image_m9d71vm9d71vm9d7.png |
| `buildings.png` | Casa, torre, castillo / molino, tienda/mercado, taberna / herrería, faro, cueva con huevos | 2048×2048 | Gemini_Generated_Image_l51vyil51vyil51v.png |
| `environment_props.png` | Árbol frutal, pino, palmera / rocas, arbusto con flores, valla / puente, pozo, barril y caja | 2048×2048 | Gemini_Generated_Image_myip3gmyip3gmyip.png |
| `vehicles_boats.png` | Bote de remos, barco pirata, galeón / velero, balsa, submarino / dirigible, barco de vapor, drakkar vikingo | 2048×2048 | Gemini_Generated_Image_882p8n882p8n882p.png |
| `effects_vfx.png` | Explosión, destello de estrella, nube de humo / fuego, impacto, confeti / remolino mágico, salpicadura, rayo | 2048×2048 | Gemini_Generated_Image_khdv3pkhdv3pkhdv.png |
| `ui_buttons.png` | Botones vacíos naranja, verde azulado, morado / play, pausa, ajustes / inicio, sonido, marco de panel | 2048×2048 | Gemini_Generated_Image_9cvy6f9cvy6f9cvy.png |

## Sprites recortados

Los sprites individuales, ya con fondo transparente, están en
`DragonRunner/Assets/Resources/Sprites/<hoja>/<nombre>.png`. Se regeneran con:

```bash
python3 tools/slice_sprites.py assets/sprites DragonRunner/Assets/Resources/Sprites
```

(requiere `numpy`, `scipy` y `Pillow`).
