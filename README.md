# Captura Flotante

La miniatura flotante de capturas de macOS, ahora en Windows.

Cuando sacas una captura (por ejemplo con **Win + Shift + S**), aparece una miniatura en la esquina inferior derecha. La puedes **arrastrar** a cualquier chat, correo, carpeta o app, y la imagen te sigue por la pantalla mientras la mueves, igual que en la Mac.

> [!IMPORTANT]
> **Desactiva las notificaciones de la Herramienta Recortes**, o no vas a ver la miniatura: la notificación de Windows aparece en la misma esquina y la tapa.
>
> **Configuración → Sistema → Notificaciones → Herramienta Recortes → Desactivado**
>
> La captura se sigue copiando y guardando normal; solo deja de salir el aviso.

## Características

- Miniatura flotante que entra deslizándose desde el borde.
- Arrastrar y soltar en cualquier app (se suelta como archivo PNG).
- Animación de arrastre: una copia de la miniatura sigue al mouse.
- Si la sueltas donde no corresponde, vuelve sola a su lugar.
- Clic = abrir la imagen · Clic derecho = cerrar.
- Se cierra sola a los 8 segundos (no mientras tienes el mouse encima).
- Las capturas se guardan en `Imágenes\Capturas`.
- Funciona con varios monitores (aparece en la pantalla donde está tu mouse).
- Un solo archivo `.exe`, sin instalador y sin dependencias: usa .NET Framework 4, que ya viene con Windows 10 y 11.

## Instalación

1. Descarga `CapturaFlotante.exe` desde [Releases](../../releases/latest).
2. Guárdalo en una carpeta fija (por ejemplo `C:\Herramientas\`) y ábrelo con doble clic.
3. Aparece un ícono azul en la bandeja del sistema (junto al reloj). Haz clic derecho en él y activa **Iniciar con Windows**.

Windows SmartScreen puede advertir que el programa es "desconocido" porque no está firmado. Haz clic en **Más información → Ejecutar de todas formas**. El código fuente completo está en este repositorio.

4. **No olvides desactivar las notificaciones de la Herramienta Recortes** (ver el aviso de arriba).

## Cómo funciona

El programa no reemplaza la Herramienta Recortes. Escucha el portapapeles de Windows y, cada vez que llega una imagen, la guarda como PNG y muestra la miniatura. Por eso funciona con Win + Shift + S, Impr Pant o cualquier herramienta que copie la captura al portapapeles.

Esto también significa que si copias una imagen desde el navegador, aparece la miniatura.

## Compilar desde el código

No hace falta instalar nada. En Windows, clona el repositorio y ejecuta:

```
build.cmd
```

Esto usa el compilador `csc.exe` que trae Windows y genera `CapturaFlotante.exe`. Cada versión publicada en Releases se compila automáticamente con GitHub Actions a partir de este mismo código.

## Licencia

[MIT](LICENSE): libre para usar, modificar y compartir.
