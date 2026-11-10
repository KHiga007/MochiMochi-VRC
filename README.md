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

La publicación del script incluye el runtime de .NET.

## Descargar y compilar

~~~powershell
git clone https://github.com/KHiga007/MochiMochi-VRC.git
cd MochiMochi-VRC
.\scripts\Preparar-App.ps1
& '.\dist\VRC Shelf.exe'
~~~

El script instala las dependencias de Vue, compila la interfaz, ejecuta sus pruebas y publica la aplicación. Cerrá la app antes de instalar una nueva compilación.

Para trasladar la aplicación a otra carpeta, copiá el contenido completo de dist.

## Abrir en Visual Studio

1. Abrí **MochiMochi VRC.sln**.
2. Seleccioná **VrcShelf.Desktop** como proyecto de inicio.
3. Presioná **F5**.

La compilación desde Visual Studio prepara la interfaz Vue. El Explorador de soluciones incluye el código C#, la interfaz y las pruebas.

## Configurar la biblioteca

En Configuración, elegí la carpeta raíz de tus assets. También podés establecer la variable de entorno VRC_LIBRARY_ROOT antes de iniciar la aplicación.

Los ajustes y favoritos de escritorio se guardan en:

~~~text
%LOCALAPPDATA%\VrcShelfDesktop
~~~

La biblioteca utiliza carpetas por categoría y puede leer los metadatos de 99_Catalogo. El catálogo se actualiza al iniciar la app o al solicitar una actualización.

## Estructura del proyecto

| Carpeta o archivo | Responsabilidad |
| --- | --- |
| Models | Catálogo, persistencia y operaciones con archivos |
| Views | Ventana nativa e integración con WebView2 |
| Controllers | Solicitudes de la interfaz hacia las funciones de escritorio |
| ui/src | Componentes, estilos y lógica de Vue |
| ui/public | Fuentes y avisos de terceros |
| ui/tests | Pruebas de la interfaz y de integración |
| ui/server | Servidor utilizado por las pruebas de integración |
| Tests | Comprobaciones de C# |
| scripts | Compilación y publicación local |
| Program.cs | Punto de entrada de la aplicación |

## Pruebas

Comprobaciones nativas:

~~~powershell
dotnet run --project .\Tests\Checks.csproj -c Release
~~~

Pruebas de Vue e integración:

~~~powershell
cd ui
npm ci
npm run build
npm test
~~~

## Licencia

El código del proyecto se distribuye bajo la [licencia MIT](LICENSE), con copyright de KHiga007.

Las dependencias y fuentes conservan sus respectivas licencias, disponibles en [ui/public/licenses](ui/public/licenses) y [ui/public/fonts](ui/public/fonts). Los assets que cada usuario agrega a su biblioteca se rigen por las licencias de sus autores.
