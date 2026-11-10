# MochiMochi VRC

Organizá y explorá tu biblioteca local de assets de Unity y VRChat desde una aplicación de escritorio para Windows.

MochiMochi VRC combina C# y Windows Forms con una interfaz Vue que se muestra dentro de WebView2.

## Funciones

- Catálogo con miniaturas y vistas de tarjetas o lista.
- Búsqueda y filtros por categoría, avatar y etiquetas.
- Orden por nombre o tamaño, con paginación.
- Favoritos y configuración guardados localmente.
- Consulta de archivos y apertura de carpetas desde Windows.
- Apertura de archivos comprimidos con la aplicación asociada.
- Carga de productos con varios archivos, portada y compatibilidad.
- Etiqueta R18 y difuminado de las portadas correspondientes.
- Lectura de la biblioteca al iniciar y al actualizar manualmente.

## Requisitos

Para ejecutar la aplicación publicada:

- Windows x64.
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/).

Para compilar:

- SDK de .NET 8.0.422, indicado en global.json.
- Node.js 22.12 o posterior de la rama 22, con npm.
- Visual Studio con la carga de trabajo **Desarrollo de escritorio de .NET**, si vas a usar el IDE.

El perfil de publicación Windows incluye el runtime de .NET.

## Descargar la aplicación

Descargá la versión portable para Windows x64 desde [GitHub Releases](https://github.com/KHiga007/MochiMochi-VRC/releases/latest).

## Licencia

El código del proyecto se distribuye bajo la [licencia MIT](LICENSE), con copyright de KHiga007.

Las dependencias y fuentes conservan sus respectivas licencias, disponibles en [ui/public/licenses](ui/public/licenses) y [ui/public/fonts](ui/public/fonts). Los assets que cada usuario agrega a su biblioteca se rigen por las licencias de sus autores.

