<p align="center">
  <a href="README.md">English</a> · <a href="README.zh-CN.md">简体中文</a> · <strong>Español</strong> · <a href="README.ja.md">日本語</a> · <a href="README.pt-BR.md">Português (BR)</a> · <a href="README.ru.md">Русский</a> · <a href="README.fr.md">Français</a>
</p>

<p align="center"><em>Esta página está traducida, pero la interfaz de la aplicación está actualmente solo en inglés.</em></p>

<p align="center">
  <img src="docs/icon.png" width="280" alt="InstallerClean">
</p>

<p align="center"><em>🎶 What's my line? I'm happy <a href="https://www.youtube.com/watch?v=HM-jHhUZfFI">cleaning Windows</a></em></p>

<h1 align="center">InstallerClean</h1>

<p align="center"><strong>Un reemplazo moderno y de código abierto para <a href="https://www.homedev.com.au/free/patchcleaner">PatchCleaner</a>. Limpia con seguridad <code>C:\Windows\Installer</code>, la carpeta oculta de Windows que se va comiendo tu espacio en disco sin que te des cuenta.</strong></p>

<p align="center">
  <a href="LICENSE"><img src="https://img.shields.io/badge/licence-MIT-blue.svg" alt="Licencia: MIT"></a>
  <a href="https://dotnet.microsoft.com/download/dotnet/10.0"><img src="https://img.shields.io/badge/.NET-10.0-purple.svg" alt=".NET 10"></a>
  <a href="https://github.com/no-faff/InstallerClean/actions/workflows/ci.yml"><img src="https://github.com/no-faff/InstallerClean/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases"><img src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4.svg" alt="Windows 10/11"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases/latest"><img src="https://img.shields.io/github/v/release/no-faff/InstallerClean" alt="Versión de GitHub"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases"><img src="https://img.shields.io/github/downloads/no-faff/InstallerClean/total?cacheSeconds=300" alt="Descargas totales"></a>
</p>

![Captura de pantalla de InstallerClean tras limpiar con éxito: 965 MB liberados, 68 archivos eliminados](docs/screenshots/04d-deleted-freed-success.webp)

- **Qué hace:** Encuentra y elimina archivos innecesarios de `C:\Windows\Installer`, la carpeta oculta que Windows nunca limpia.
- **Cuánto espacio:** Depende de tu software. En mi equipo fue de casi 1 GB. Un usuario de InstallerClean [informó de](https://github.com/no-faff/InstallerClean/issues/12#issuecomment-4395580816) 25 GB. Con Adobe Acrobat puede superar los 100 GB. Podría no ser nada en absoluto. La cuestión es que es rápido y no cuesta nada; todo lo que se pueda eliminar desaparecerá.
- **¿Es seguro?** Sí. Solo elimina los archivos que el propio Windows declara que ya no necesita. Eliminar envía los archivos a la Papelera de reciclaje y nunca borra nada de forma permanente sin preguntar. Mover te permite guardarlos en un lugar seguro.
- **Cómo obtenerlo:** [Descarga la última versión](../../releases/latest), ejecútala, listo.

---

## La carpeta de la que nadie te habla

En todo PC con Windows existe una carpeta oculta llamada `C:\Windows\Installer`. Cada vez que instalas software que usa el sistema Windows Installer, o aplicas un parche a Microsoft Office, Adobe Acrobat, Visual Studio o cualquier otra aplicación basada en `.msi`, una copia de ese instalador o de ese archivo de parche `.msp` va a parar a esta carpeta. Y allí se queda.

Cuando desinstalas el software, los archivos siguen ahí. Cuando un parche nuevo sustituye a uno antiguo, los dos siguen ahí. Windows nunca los limpia. El Liberador de espacio en disco no los toca. DISM se ocupa de otra carpeta distinta. Con los años, la carpeta crece: 10 GB, 30 GB, 50 GB. En equipos con mucho software basado en MSI (Acrobat es un sospechoso habitual), puede [superar los 100 GB](https://www.reddit.com/r/sysadmin/comments/1oxcrmh/acrobat_filling_up_the_cwindowsinstaller_folder/).

No son archivos temporales que reaparezcan en cuanto cierras una herramienta de limpieza. Son peso muerto de verdad: instaladores antiguos de software que desinstalaste hace años y parches que ya se han sustituido tres veces. Una vez fuera, no vuelven.

**Si buscas una manera sencilla de liberar espacio en disco en Windows, esta carpeta es uno de los mejores sitios por donde empezar.** InstallerClean encuentra los archivos innecesarios y los elimina con seguridad.

[PatchCleaner](https://www.homedev.com.au/free/patchcleaner) ha sido la herramienta de referencia para esto, pero no se actualiza desde marzo de 2016 y es de código cerrado. InstallerClean es una alternativa de código abierto, con detección de parches sustituidos (que captura los parches de Acrobat que PatchCleaner excluye) y una interfaz moderna.

## La búsqueda de ayuda

Si alguna vez has buscado ayuda con esta carpeta, ya sabes cómo va la cosa. Alguien pregunta cómo limpiarla. Le dicen que ejecute el Liberador de espacio en disco. Lo prueba. Libera [600 MB de una carpeta de 180 GB](https://learn.microsoft.com/en-us/answers/questions/4238108/windows-installer-folder-has-occupied-180gb). El hilo se apaga.

> *«Todos los hilos que he encontrado suelen recomendar las mismas cosas, que no resuelven el problema, y luego mueren.»*
>
> ksparks519, r/Windows10 (traducido del inglés)

O bien le dicen que ni la toque. En un hilo, a alguien con una carpeta Installer de 60 GB le dijeron que [«no la toques»](https://www.reddit.com/r/techsupport/comments/1hw4suq/my_windows_installer_folder_is_like_60gb_so_i/). Cuando preguntó qué debía hacer en su lugar, la respuesta fue: *«Acabo de decírtelo.»*

El consejo habitual confunde borrar archivos al azar (lo cual sí es peligroso) con eliminar archivos que el propio Windows da por innecesarios (lo cual no lo es). InstallerClean hace lo segundo.

Si ya has buscado ayuda con esto antes, probablemente ya hayas encontrado [PatchCleaner](https://www.homedev.com.au/free/patchcleaner) de [John Crawford](https://www.homedev.com.au/). Es una aplicación estupenda. La descargué e hizo exactamente lo que prometía: liberar muchísimo espacio. Lo único que no gestiona son los parches de Adobe; los excluye por defecto, y en equipos donde Adobe es el mayor responsable, quedan atrás muchos archivos que se podrían eliminar:

> *«He descargado PatchCleaner para borrar los archivos `.msp` huérfanos... 29 GB de los archivos están "excluidos por filtros", así que PatchCleaner no parece servir de ayuda.»*
>
> HeatherBunny1111, [r/techsupport](https://www.reddit.com/r/techsupport/comments/1qc4tcf/how_to_delete_msp_files_safely/) (traducido del inglés)

InstallerClean detecta qué parches han sido sustituidos por actualizaciones más recientes y los marca como eliminables, incluidos los parches de Acrobat que PatchCleaner excluye.

## Qué hace

1. **Analiza** `C:\Windows\Installer` en busca de archivos `.msi` y `.msp`
2. **Consulta** la API de Windows Installer para identificar qué archivos siguen registrados
3. **Muestra** lo que hace falta y lo que no, con sus tamaños
4. **Elimina** los archivos innecesarios: los envía a la Papelera de reciclaje (si no está disponible para la unidad, la aplicación pregunta antes de cualquier borrado permanente) o los mueve a una carpeta que tú elijas

Sin actividad de red automática. Dos botones opcionales hacen una sola llamada HTTPS, y solo al pulsarlos: **Buscar actualizaciones** en Acerca de, y **Enviar resumen** en la pantalla final. Encontrarás todos los detalles en [Lo que no hace](#lo-que-no-hace), más abajo.

## Capturas de pantalla

<p>
  <img src="docs/screenshots/01-initial-scan.webp" alt="Pantalla de bienvenida con el análisis en curso, tras encontrar 68 archivos para limpiar" width="900"><br>
  <em>Análisis inicial. Es muy rápido.</em>
</p>

<p>
  <img src="docs/screenshots/02-main-window.webp" alt="Ventana principal con 116 archivos aún en uso y 68 archivos para limpiar" width="900"><br>
  <em>Resultados: cuánto se está usando y cuánto se puede eliminar.</em>
</p>

<p>
  <img src="docs/screenshots/03a-details-registered.webp" alt="Ventana de archivos registrados con los productos instalados y sus metadatos de la base de datos del instalador" width="900"><br>
  <em>Los archivos aún en uso, con los metadatos leídos de la base de datos del instalador.</em>
</p>

<p>
  <img src="docs/screenshots/03b-details-unused.webp" alt="Ventana de archivos no utilizados con los archivos .msi eliminables y sus motivos" width="900"><br>
  <em>Los archivos que ya no hacen falta.</em>
</p>

<p>
  <img src="docs/screenshots/04b-Delete-dialogue.webp" alt="Diálogo de confirmación de eliminación que indica que 68 archivos (965 MB) irán a la Papelera de reciclaje" width="900"><br>
  <em>Confirmación antes de cada acción. Eliminar envía a la Papelera de reciclaje; Mover coloca los archivos donde tú elijas.</em>
</p>

<p>
  <img src="docs/screenshots/04d-deleted-freed-success.webp" alt="Superposición de éxito que muestra 965 MB liberados tras una eliminación, con 68 archivos enviados a la Papelera de reciclaje" width="900"><br>
  <em>Tras una eliminación correcta.</em>
</p>

<p>
  <img src="docs/screenshots/06a-scanned-again-all-clean.webp" alt="Superposición de «todo limpio» que aparece cuando no queda nada que eliminar en un análisis posterior" width="900"><br>
  <em>Tras un nuevo análisis. No queda nada que limpiar.</em>
</p>

## Cómo funciona

InstallerClean identifica dos tipos de archivos innecesarios.

**Los archivos huérfanos** son instaladores y parches que quedan tras desinstalar un programa. Windows ya no los referencia, pero siguen en la carpeta ocupando espacio.

**Los parches sustituidos** son parches `.msp` antiguos que han sido reemplazados por otros más nuevos. Windows los marca como sustituidos en su propia base de datos, pero nunca los borra. Los proveedores que publican parches con frecuencia (Acrobat, Office, grandes herramientas de desarrollo) van acumulando parches sustituidos de forma indefinida.

Para encontrarlos, InstallerClean llama directamente a la interfaz COM de Windows Installer mediante P/Invoke:

- `MsiEnumProductsEx` para enumerar todos los productos instalados
- `MsiEnumPatchesEx` para encontrar todos los parches registrados de cada producto
- `MsiGetPatchInfoEx` para leer el estado de cada parche (aplicado, sustituido u obsoleto)

Todo archivo `.msi` o `.msp` de `C:\Windows\Installer` que no pertenezca a ningún producto registrado es huérfano. Todo parche marcado como sustituido y que no haga falta para la desinstalación se marca como eliminable.

Si la API devuelve datos incompletos (algo raro, pero que puede ocurrir si el estado del instalador está dañado), la aplicación recurre a leer el registro. Esa lectura de reserva solo añade archivos al conjunto de «aún necesarios», nunca al de «eliminables».

Tras completar un Mover o un Eliminar, las subcarpetas vacías que haya dentro de `C:\Windows\Installer` (los directorios que la caché deja atrás cuando su contenido desaparece) se podan en la misma pasada. Los puntos de reanálisis (reparse points) se omiten durante la poda para que un punto de unión (junction) creado dentro de la caché no pueda redirigir la limpieza fuera de ella.

## ¿Es seguro?

Sí. InstallerClean consulta la misma base de datos que el propio Windows usa para llevar el control de lo que está instalado. Si Windows dice que un archivo ya no hace falta, la aplicación se fía; no adivina a partir de nombres de archivo ni fechas.

**Dentro de la aplicación.** Eliminar envía los archivos a la Papelera de reciclaje. Si la Papelera no está disponible para esa unidad (desactivada para la unidad, llena o dañada), InstallerClean no borra los archivos para siempre en silencio. Se detiene y te deja elegir: moverlos a un lugar seguro, borrarlos de forma permanente o cancelar. Los archivos solo se borran de forma permanente si lo eliges explícitamente. Mover es la opción aún más segura: coloca los archivos en una carpeta que tú elijas, para que puedas conservarlos hasta estar seguro de que nada se ha roto. Nada se toca hasta que confirmas. Si Windows Installer está escribiendo en la caché en ese momento, tiene una transacción anterior suspendida o tiene un renombrado pendiente tras reiniciar que apunta a la caché, Mover y Eliminar quedan desactivados y se muestra el motivo concreto. Los servicios de análisis, consulta, movimiento, eliminación, configuración y comprobación de reinicio pendiente están cubiertos por una batería de pruebas automatizadas que se ejecuta en cada commit (consulta la insignia de CI más arriba).

**Verificación del binario.** InstallerClean no está firmado. Los certificados de firma de código cuestan dinero todos los años y prefiero mantener el proyecto gratuito, abierto y financiado por donaciones.

- Los hashes SHA-256 de cada versión están listados en la [página de versiones](../../releases/latest).
- Se publican enlaces a VirusTotal de las variantes setup, portable, slim y CLI con cada versión.
- El código fuente está en [github.com/no-faff/InstallerClean](https://github.com/no-faff/InstallerClean) y la CI compila y prueba cada commit (consulta la insignia verde de CI más arriba).
- [Softpedia](https://www.softpedia.com/get/System/Hard-Disk-Utils/InstallerClean.shtml) analiza cada versión en busca de virus, spyware y adware.
- [MajorGeeks](https://www.majorgeeks.com/files/details/installerclean.html) prueba cada envío en una máquina virtual y solo lo publica si pasa su revisión.

<a href="https://www.softpedia.com/get/System/Hard-Disk-Utils/InstallerClean.shtml"><img src="docs/badges/softpedia-100-free2.webp" alt="Certificado 100% limpio por Softpedia" width="190"></a>

VirusTotal: limpio en todos los motores. Hay enlaces en vivo en las notas de cada versión para que puedas volver a comprobarlo.

## Si falta un archivo necesario

InstallerClean solo elimina los archivos que Windows da por terminados, así que no puede dejar a un programa sin posibilidad de reparación, actualización o desinstalación. Quitar archivos de `C:\Windows\Installer` a mano, o con una herramienta que no consulte primero la base de datos del instalador, es otra cosa, y es la razón por la que el consejo habitual es no tocar la carpeta. Ese consejo es acertado, hasta cierto punto. Aquí tienes el panorama completo, y qué hacer si un archivo necesario ya ha desaparecido.

<details>
<summary><strong>Acerca de <code>C:\Windows\Installer</code>, y cómo recuperar un archivo que falta</strong></summary>

<br>

*Las citas de Microsoft que aparecen a continuación se reproducen en su versión original en inglés.*

`C:\Windows\Installer` es la caché de Windows Installer. Cuando instalas un programa basado en MSI o aplicas un parche, Windows guarda aquí una copia del instalador y anota, para cada producto, el archivo que espera encontrar más adelante. Estos archivos no se usan mientras el programa funciona; se usan cuando Windows lo repara, lo actualiza o lo desinstala. Borra uno que un programa todavía necesita y nada se rompe de inmediato; es justo por eso por lo que resulta fácil borrarlos sin consecuencias aparentes y solo tener problemas meses después. Microsoft lo expresa así:

> "If the installer cache is compromised, you may not immediately see problems until you take an action such as uninstalling, repairing, or updating a product."

La recuperación no es sencilla, y Microsoft es franco al respecto:

> "If application files are missing from the Windows Installer Cache, ask the vendor or support team for the application about the missing files. You must follow the procedures or steps recommended by the application vendor to restore the files. In some cases, you may have to rebuild the operating system and reinstall the application to fix the problem."

> "Windows support engineers cannot help you recover missing application files from the Windows Installer cache."

Tampoco puedes tomar prestado el archivo de otra máquina:

> "Missing files cannot be copied between computers because the files are unique."

En la práctica, la solución que suele funcionar es descargar el instalador del programa afectado desde su fabricante y ejecutarlo sobre tu instalación existente. No desinstales primero: desinstalar es en sí mismo uno de los pasos que necesita el archivo que falta. Usa la versión que tienes instalada actualmente si todavía puedes conseguirla, porque Windows puede rechazar una distinta:

> "The upgrade cannot be installed by the Windows Installer service because the program to be upgraded may be missing, or the upgrade may update a different version of the program."

Esto normalmente restaura el archivo y deja tu configuración intacta, pero Microsoft no lo garantiza, y su último recurso documentado es reinstalar el programa, o reconstruir Windows. Esa es la posición oficial, contada tal como la encuentro. No la he causado yo y no puedo mejorar las propias indicaciones de Microsoft; me limito a decirte cuál es.

Nada de esto puede ocurrir por culpa de InstallerClean. Solo elimina archivos que el propio Windows da por innecesarios, de modo que el archivo que una futura reparación, actualización o desinstalación irá a buscar nunca es uno que haya tocado. Las indicaciones de Microsoft están en [Restore missing Windows Installer cache files](https://learn.microsoft.com/en-us/troubleshoot/windows-client/application-management/missing-windows-installer-cache).

</details>

## Accesibilidad

InstallerClean está pensado para ser plenamente utilizable con el teclado y con un lector de pantalla.

- **Totalmente operable con el teclado.** El tabulador llega a cada control, y las columnas de las ventanas de detalle se ordenan con el teclado: aquí nada necesita ratón. El foco del teclado permanece visible dondequiera que esté.
- **Narrador y Acceso por voz.** Cada control está etiquetado, y la palabra visible en un botón es exactamente la que lo activa por voz. Cuando termina una operación de mover o eliminar, el resultado se anuncia en voz alta.
- **Hecho para leerse.** El texto cumple el contraste WCAG AA en todo el tema oscuro.

Si algo aquí te estorba, [abre un issue](../../issues). Los problemas de accesibilidad son bugs, no casos límite.

## Lo que no hace

- WinSxS (`C:\Windows\WinSxS`) es una carpeta distinta con reglas distintas. Para esa, ejecuta `Dism /Online /Cleanup-Image /StartComponentCleanup` desde un símbolo del sistema elevado.
- Sin servicio en segundo plano, sin tarea programada, sin limpieza automática. La aplicación se ejecuta cuando tú la inicias.
- El acceso al registro es de solo lectura. La aplicación consulta la base de datos de Windows Installer; no la modifica.
- Sin telemetría automática, sin red en segundo plano. La aplicación no hace ninguna llamada de red hasta que pulses uno de los dos botones. **Buscar actualizaciones** en Acerca de consulta la API pública de releases de GitHub al pulsarlo y te dice si tienes la última versión (un solo GET HTTPS, cadena identificadora `InstallerClean/<version>`). **Enviar resumen** en la pantalla final lee `%LOCALAPPDATA%\NoFaff\InstallerClean\last-run.json` y lo envía mediante POST HTTPS a un endpoint de No Faff para que yo pueda ver si la ejecución funcionó. El JSON contiene únicamente contadores y etiquetas categóricas: ninguna ruta de archivo, ningún nombre de usuario, ningún identificador de equipo, ninguna hora del día. Al pulsar se abre una ventana de confirmación que muestra el JSON exacto que se va a enviar; revísalo ahí y pulsa Enviar para confirmar, o Cancelar para echarte atrás. Una vez por equipo: tras un envío correcto el botón queda oculto para siempre; si el primer intento falla con un error transitorio, la próxima sesión vuelve a preguntar.
- Sin extras incluidos. Sin barras de herramientas, sin ofertas de terceros, sin que intenten venderte nada.
- El único permiso que se pide, más allá de iniciar la aplicación, es el de Administrador, necesario porque a `C:\Windows\Installer` solo pueden acceder los administradores.

## Preguntas frecuentes

**¿Realmente voy a liberar GB de espacio?** Depende de tu equipo. Una instalación limpia de Windows 11 sin software adicional no tiene nada que eliminar. Una estación de trabajo de desarrollo de larga vida, o cualquier equipo con mucho software basado en MSI (Acrobat, Office, LibreOffice, grandes herramientas de desarrollo), puede tener decenas de GB. Ejecuta `installerclean-cli /s` para ver exactamente qué se eliminaría antes de comprometerte.

<!-- reports-stats-start (generated by non-repo-files/refresh-reports-table.mjs; do not hand-edit between these markers) -->
De los 68 informes que me han enviado amablemente (gracias 🙏) desde que la v1.8.0 añadió la opción:

| Resultado | Proporción | Mínimo | Mediana | Máximo |
|---|---|---|---|---|
| Nada que eliminar | 68% | - | - | - |
| Espacio liberado | 32% | 0.2 GB | 21 GB | 327 GB |
<!-- reports-stats-end -->

**¿Por qué pide Administrador?** `C:\Windows\Installer` es propiedad de SYSTEM y está restringido solo a administradores. Leer la carpeta, consultar la API de la base de datos del Installer y mover o eliminar archivos requieren elevación. No existe ninguna vía en modo usuario.

**¿Puedo deshacer una eliminación?** Normalmente, sí. Cuando la Papelera está disponible para la unidad, Eliminar envía los archivos ahí y puedes restaurarlos desde la Papelera. Si no está disponible, la aplicación nunca borra para siempre por su cuenta (consulta [¿Es seguro?](#es-seguro)). Para una red de seguridad que tú controlas, usa Mover para colocar los archivos en una carpeta a tu elección y verifica que nada se rompa antes de borrarlos desde ahí.

**¿Va a quejarse Windows si quito estos archivos?** No. InstallerClean solo elimina los archivos que el propio Windows da por terminados, así que nada de lo que elimina hace falta para reparar, actualizar o desinstalar un programa. Si un archivo necesario llega a desaparecer de `C:\Windows\Installer` por algún otro medio, consulta [Si falta un archivo necesario](#si-falta-un-archivo-necesario).

**¿Por qué no `Win32_Product` (WMI)?** [`Win32_Product` desencadena operaciones de reparación de MSI en cada producto durante la enumeración](https://gregramsey.net/2012/02/20/win32_product-is-evil/), lo cual puede tardar minutos y cargar mucho el disco. InstallerClean llama a la API COM de Windows Installer directamente, sin efectos colaterales.

**¿Por qué no simplemente un script de PowerShell?** Un script corto que llame a `MsiEnumPatchesEx` basta para *listar* parches, pero las partes que sostienen InstallerClean son las que un script pasa por alto: la clasificación de huérfano frente a sustituido, la lectura de reserva del registro que solo añade archivos al conjunto de «aún necesarios» (nunca al de «eliminables»), el bloqueo por reinicio pendiente, la red de seguridad de mover a otra ubicación, el progreso por archivo con cancelación y el uso de la Papelera de reciclaje en lugar del borrado permanente por defecto. Los casos límite en equipos reales con mucho MSI (registros de productos dañados, uniones dentro de la caché, productos en `HKU\.DEFAULT`, transacciones del Installer suspendidas) son fáciles de gestionar mal en un script improvisado. La `installerclean-cli` es la cara sin interfaz si lo que buscas es scripting.

**¿Funciona en Windows 7 u 8?** Sin probar y sin soporte. Está pensado para Windows 10 y 11.

**¿Sirve para RMM o despliegue masivo?** Sí. La CLI sale con códigos distintos por resultado (0 éxito, 2 parcial, 1 fallo total, 75 transitorio, 130 Ctrl+C), de modo que una tarea programada puede reintentar en 75 sin confundirlo con un fallo total. Escribe un resumen por ejecución en el registro de eventos de Aplicación y respeta el mismo mutex de instancia única que la interfaz gráfica. Consulta la sección Línea de comandos.

## Descarga

Cuatro variantes, elige una:

- **Setup** (`InstallerClean-setup.exe`): un instalador clásico de Windows con el runtime de .NET 10 incluido. Añade una entrada en el menú Inicio y se desinstala sin dejar rastro. Bien guardado en Programas, fácil de encontrar dentro de seis meses.
- **Portable** (`InstallerClean-portable.exe`): un único exe autónomo con el runtime incluido. Sin instalación, sin desinstalador. Ejecútalo, úsalo, bórralo. Vuelve a ejecutarlo cuando quieras.
- **Slim** (`InstallerClean-slim.exe`): la descarga más pequeña. Requiere tener ya instalado el [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (ya lo tienes si tu Visual Studio está al día).
- **CLI** (`installerclean-cli.exe`): la versión de línea de comandos por sí sola, un único exe autónomo. Sin instalación, sin dejar nada en la máquina después. Déjalo en un equipo cliente, ejecuta un análisis o una limpieza, y bórralo. Pensado para scripting, tareas programadas y despliegue masivo, cuando quieres las operaciones sin una aplicación de escritorio en el cliente. Consulta [Línea de comandos](#línea-de-comandos) para los argumentos y los códigos de salida.

Descárgala desde la [página de versiones](../../releases/latest) y ejecútala. Windows SmartScreen dirá «Editor desconocido». Pulsa **Más información** y luego **Ejecutar de todas formas**. Es normal en software de código abierto sin firmar.

La aplicación analiza automáticamente al arrancar. Revisa los resultados y pulsa **Eliminar** o **Mover**.

O instálalo con [Scoop](https://scoop.sh):

```
scoop bucket add no-faff https://github.com/no-faff/scoop-bucket
scoop install installerclean
```

## Comparativa con PatchCleaner

| | **InstallerClean** | **PatchCleaner** |
|---|---|---|
| Última actualización | 2026 (activo) | 3 de marzo de 2016 |
| Código fuente | Código abierto (MIT) | Código cerrado |
| Runtime | .NET 10 (autónomo) | .NET + VBScript |
| API | Windows Installer COM (en proceso) | Windows Installer COM (fuera de proceso, mediante VBScript) |
| Detección de parches sustituidos | Sí | No |
| Gestión de Adobe | Detecta los parches sustituidos | Excluye por defecto |
| Interfaz | Tema oscuro (WPF) | Windows Forms |
| Recopilación de datos | Ninguna | Ninguna |
| Seguridad al eliminar | Papelera de reciclaje, nunca un borrado permanente silencioso | Permanente, sin Papelera |

> **Nota sobre `Win32_Product`:** El enfoque común pero defectuoso para listar productos instalados es `Win32_Product` (WMI), que [desencadena operaciones de reparación de MSI](https://gregramsey.net/2012/02/20/win32_product-is-evil/) en cada producto durante la enumeración. Tanto InstallerClean como PatchCleaner lo evitan. Ambos usan la interfaz COM de Windows Installer. El nombre de archivo `WMIProducts.vbs` del script de PatchCleaner resulta engañoso; el script usa COM de MSI, no WMI.

[Ultra Virus Killer (UVK)](https://www.carifred.com/uvk/) también ofrece limpieza del Installer como parte de su módulo System Booster, pero es una herramienta de pago (15-25 USD) y la limpieza es una pequeña función dentro de una aplicación mucho mayor. InstallerClean es gratuito, especializado y de código abierto.

Los limpiadores generalistas como [CCleaner](https://www.ccleaner.com/) y [BleachBit](https://www.bleachbit.org/) no tocan `C:\Windows\Installer`. La carpeta necesita consultas a la API de Windows Installer para distinguir los paquetes registrados de los que no se usan, y un limpiador genérico que se limitara a recorrer el árbol de archivos podría romper aplicaciones instaladas. InstallerClean es la herramienta a la que recurrir cuando esa es precisamente la carpeta que quieres limpiar.

## Línea de comandos

InstallerClean admite el funcionamiento sin interfaz, para scripting y administración de sistemas:

```
Uso:
  installerclean-cli --help   Muestra esta ayuda (también acepta /?, -h)
  installerclean-cli --version  Muestra la versión (también acepta -v)
  installerclean-cli /s       Solo análisis, lista los archivos eliminables
  installerclean-cli /d       Elimina los archivos (Papelera de reciclaje)
  installerclean-cli /m       Mueve a la ubicación por defecto guardada
  installerclean-cli /m PATH  Mueve a la ruta indicada
```

Para abrir la interfaz gráfica, ejecuta `InstallerClean.exe` (o usa el acceso directo del menú Inicio si lo instalaste con Setup).

Ejecutado sin argumentos, o con una opción no reconocida, `installerclean-cli` muestra esta ayuda y sale con el código `1`, de modo que una tarea programada que pierda su opción falla de forma visible en lugar de tener éxito en silencio sin hacer nada. Un `--help`, `/?` o `-h` explícito muestra la misma ayuda y sale con el código `0`.

`/s` es una ejecución en seco: analiza, enumera lo que eliminaría con nombres y tamaños, y sale. Útil para auditar antes de limpiar. El código de salida es `0` si el análisis tiene éxito, `1` si falla y `130` con Ctrl+C. Todos los archivos están en `C:\Windows\Installer`.

`/d` y `/m` analizan y luego actúan. `/d` envía los archivos eliminables a la Papelera de reciclaje. `/m` los mueve a una carpeta (la que indiques en la línea de comandos, o la guardada por defecto desde la interfaz gráfica). Códigos de salida: `0` éxito completo, `2` parcial (algunos archivos correctos, otros fallidos), `1` fallo total (análisis fallido, argumentos incorrectos o todos los archivos del lote han fallado), `75` una condición transitoria bloqueó la ejecución (el mensaje mostrado indica cuál y si reintentar servirá de algo), `130` Ctrl+C.

Toda la salida de la CLI, incluidos los mensajes de error y de diagnóstico, va a stdout; no hay un flujo stderr aparte. El código de salida es la señal legible por máquina (y la entrada por ejecución en el registro de eventos de Aplicación lo refleja), así que un script debería basarse en el código de salida en lugar de analizar el texto, y `installerclean-cli /s > audit.txt` captura toda la ejecución, incluida cualquier línea de error.

Las tres requieren un símbolo del sistema elevado (administrador). Si una directiva de grupo bloquea el aviso de elevación de UAC, el proceso se niega a iniciarse y Windows devuelve el error 740 al shell que lo invocó (`$LASTEXITCODE = 740` en PowerShell). `taskkill /pid <pid>` no provoca una cancelación controlada; el mutex de instancia única se recupera en la siguiente ejecución mediante la vía AbandonedMutexException.

Nota: la salida de la propia CLI está en inglés. Las descripciones anteriores corresponden a las opciones disponibles.

### ¿Por qué `installerclean-cli` y no `installerclean.exe`?

`InstallerClean.exe` es la interfaz gráfica WPF; no responde a argumentos de línea de comandos. `installerclean-cli.exe` es un ejecutable de consola aparte que se incluye en el mismo directorio de instalación y expone las mismas operaciones de análisis / movimiento / eliminación a PowerShell, cmd y tareas programadas. Como es un proceso de consola real, bloquea la consola hasta que termina; redirige o canaliza su salida igual que con cualquier otro exe de consola.

Las descargas portable y slim solo contienen el exe de la interfaz gráfica. Si quieres la línea de comandos sin la interfaz, descarga `installerclean-cli.exe` desde la [página de versiones](../../releases/latest) y ejecútalo directamente. El programa de instalación también lo instala junto a la interfaz gráfica.

## Requisitos

- Windows 10 u 11
- Privilegios de administrador (a `C:\Windows\Installer` solo pueden acceder los administradores)

Consulta [Descarga](#descarga) para ver las variantes setup, portable, slim y CLI.

## Compilar desde el código fuente

```
git clone https://github.com/no-faff/InstallerClean.git
cd InstallerClean
dotnet build src/InstallerClean/InstallerClean.csproj
```

Ejecutar las pruebas:

```
dotnet test src/InstallerClean.Tests/
```

## Contribuir

¿Has encontrado un bug o tienes una sugerencia? [Abre un issue](../../issues) o inicia una [discusión](../../discussions). Las pull requests son bienvenidas. Ejecuta `dotnet test` antes de enviar.

## Apoyar el proyecto

Si InstallerClean te ha sido útil, considera [apoyar a No Faff](https://nofaff.netlify.app/support) o dejar una estrella en GitHub.

## Historial de estrellas

<a href="https://www.star-history.com/?repos=no-faff%2FInstallerClean&type=date&legend=top-left">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/chart?repos=no-faff/InstallerClean&type=date&theme=dark&legend=top-left" />
   <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/chart?repos=no-faff/InstallerClean&type=date&legend=top-left" />
   <img alt="Gráfico del historial de estrellas" src="https://api.star-history.com/chart?repos=no-faff/InstallerClean&type=date&legend=top-left" />
 </picture>
</a>

## Licencia

[MIT](LICENSE)

---

🎶 [George Formby - When I'm Cleaning Windows](https://www.youtube.com/watch?v=sfmAeijj5cM). ¡A disfrutarla!
