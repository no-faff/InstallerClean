<p align="center">
  <a href="README.md">English</a> · <a href="README.zh-CN.md">简体中文</a> · <a href="README.ru.md">Русский</a> · <strong>Español</strong> · <a href="README.pt-BR.md">Português (BR)</a> · <a href="README.ja.md">日本語</a> · <a href="README.ko.md">한국어</a> · <a href="README.fr.md">Français</a> · <a href="README.de.md">Deutsch</a> · <a href="README.it.md">Italiano</a>
</p>

<p align="center"><em>Esta página está traducida, pero la interfaz de la aplicación está actualmente solo en inglés.</em></p>

<p align="center">
  <img src="docs/icon.png" width="280" alt="InstallerClean">
</p>

<p align="center"><em>🎶 What's my line? I'm happy <a href="https://www.youtube.com/watch?v=HM-jHhUZfFI">cleaning Windows</a></em></p>

<h1 align="center">InstallerClean</h1>

<p align="center"><strong>Una herramienta de código abierto para limpiar con seguridad <code>C:\Windows\Installer</code>, la carpeta oculta de Windows que se va comiendo tu espacio en disco sin que te des cuenta.</strong></p>

<p align="center"><em>Úsala una vez. Quizá liberes algo de espacio. Tírala.</em></p>

<p align="center">
  <a href="LICENSE"><img src="https://img.shields.io/badge/licence-MIT-blue.svg" alt="Licencia: MIT"></a>
  <a href="https://dotnet.microsoft.com/download/dotnet/10.0"><img src="https://img.shields.io/badge/.NET-10.0-purple.svg" alt=".NET 10"></a>
  <a href="https://github.com/no-faff/InstallerClean/actions/workflows/ci.yml"><img src="https://github.com/no-faff/InstallerClean/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases"><img src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4.svg" alt="Windows 10/11"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases/latest"><img src="https://img.shields.io/badge/release-v1.9.2-blue" alt="Versión de GitHub"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases"><img src="https://img.shields.io/badge/downloads-25k-brightgreen" alt="Descargas totales"></a>
</p>

![Captura de pantalla de InstallerClean tras limpiar con éxito: 1,28 GB liberados, 69 archivos enviados a la Papelera de reciclaje](docs/screenshots/06-freed-success-done.webp)

- **Qué hace:** InstallerClean hace una sola cosa: elimina archivos innecesarios de `C:\Windows\Installer`, una carpeta oculta que Windows nunca limpia. Tras un análisis casi instantáneo te dice si tienes alguno, muestra más detalle para los curiosos y te deja eliminarlos para liberar espacio en tu unidad C:. Lo usas una vez y a otra cosa.
- **Cuánto espacio:** Los informes (opcionales) enviados hasta ahora muestran que el <!-- reports-freedpct-start -->40 %<!-- reports-freedpct-end --> de los equipos tenían archivos innecesarios que limpiar. De esos, la mediana liberada es de <!-- reports-median-start -->23 GB<!-- reports-median-end -->. Unos pocos liberaron cientos de GB. En mi caso fue de 1,28 GB. El otro <!-- reports-nothingpct-start -->60 %<!-- reports-nothingpct-end --> no encontró nada que eliminar, lo que solo significa que su carpeta Installer ya estaba limpia. Más detalle en las [Preguntas frecuentes](#preguntas-frecuentes) más abajo.
- **¿Es seguro?** Sí. Le pregunta a la propia API de Windows Installer qué archivos siguen haciendo falta y solo enumera los que Windows da por terminados. Es de código abierto (MIT) y no pregunta nada sobre ti: sin cuenta, sin anuncios, sin seguimiento, sin telemetría, nada corriendo en segundo plano. Nunca se conecta a internet por su cuenta.
- **Cómo obtenerlo:** [Descarga la última versión](../../releases/latest). Ejecútala; pasa [el aviso de Windows](#unknown-publisher) y [la solicitud de permisos de administrador](#admin). Elimina los archivos innecesarios. Listo.

## Contenido

- [La carpeta de la que nadie te habla](#la-carpeta-de-la-que-nadie-te-habla)
- [La búsqueda de ayuda](#la-búsqueda-de-ayuda)
- [Qué hace](#qué-hace)
- [Capturas de pantalla](#capturas-de-pantalla)
- [Cómo funciona](#cómo-funciona)
- [¿Es seguro?](#es-seguro)
- [Si te llega a faltar un archivo de C:\Windows\Installer](#recovery)
- [Accesibilidad](#accesibilidad)
- [Lo que no hace](#lo-que-no-hace)
- [Preguntas frecuentes](#preguntas-frecuentes)
- [Descarga](#descarga)
- [Comparativa con PatchCleaner](#comparativa-con-patchcleaner)
- [Línea de comandos](#línea-de-comandos)
- [Requisitos](#requisitos)
- [Compilar desde el código fuente](#compilar-desde-el-código-fuente)
- [Contribuir](#contribuir)
- [Apoyar el proyecto](#apoyar-el-proyecto)
- [Historial de estrellas](#historial-de-estrellas)
- [Licencia](#licencia)

---

## La carpeta de la que nadie te habla

En todo PC con Windows existe una carpeta oculta llamada `C:\Windows\Installer`. Cada vez que instalas software que usa el sistema Windows Installer, o aplicas un parche a Microsoft Office, Adobe Acrobat, Visual Studio o cualquier otra aplicación basada en `.msi`, una copia de ese instalador o de ese archivo de parche `.msp` va a parar a esta carpeta, y allí se queda.

Cuando desinstalas el software, los archivos siguen ahí. Cuando un parche nuevo sustituye a uno antiguo, los dos siguen ahí. Windows nunca los limpia. El Liberador de espacio en disco no los toca. DISM se ocupa de otra carpeta distinta. Con el tiempo, la carpeta crece: 1 GB, 5 GB, 20 GB, 50 GB. En equipos con mucho software basado en MSI (Acrobat es un sospechoso habitual), puede [superar los 100 GB](https://www.reddit.com/r/sysadmin/comments/1oxcrmh/acrobat_filling_up_the_cwindowsinstaller_folder/).

No son archivos temporales que vuelvan por su cuenta. Son peso muerto de verdad: instaladores antiguos de software que desinstalaste hace años y parches que se han sustituido varias veces. Una vez fuera, no vuelven.

**Si buscas una manera sencilla de liberar espacio en disco en Windows, esta carpeta es un buen sitio por donde empezar.** InstallerClean encuentra los archivos innecesarios y los elimina con seguridad.

## La búsqueda de ayuda

Si alguna vez has buscado ayuda con esta carpeta, seguramente ya sabes cómo va la cosa. Alguien con 180 GB en `C:\Windows\Installer` pregunta cómo limpiarla. Le [dicen que ejecute el Liberador de espacio en disco](https://learn.microsoft.com/en-us/answers/questions/4238108/windows-installer-folder-has-occupied-180gb). Lo prueba. Libera 600 MB, ninguno de ellos de esa carpeta (porque el Liberador de espacio en disco no toca `C:\Windows\Installer`). El hilo se apaga.

> *«Todos los hilos que he encontrado suelen recomendar las mismas cosas, que no resuelven el problema, y luego mueren.»*
>
> [ksparks519, r/Windows10](https://www.reddit.com/r/Windows10/comments/1bt8c5p/anyone_ever_figure_out_giant_installer_folders/) (traducido del inglés)

O bien le dicen que ni la toque. En un hilo, a alguien con una carpeta Installer de 60 GB le dijeron que [«no la toques»](https://www.reddit.com/r/techsupport/comments/1hw4suq/my_windows_installer_folder_is_like_60gb_so_i/). Cuando preguntó qué debía hacer en su lugar, la respuesta fue: *«Acabo de decírtelo.»*

El consejo habitual confunde borrar archivos al azar (lo cual sí es peligroso) con eliminar archivos que el propio Windows da por innecesarios (lo cual no lo es). InstallerClean hace lo segundo.

## Qué hace

1. **Analiza** `C:\Windows\Installer` en busca de archivos `.msi` y `.msp`
2. **Consulta** la API de Windows Installer para identificar qué archivos siguen registrados
3. **Muestra** cuánto puedes liberar y cuánto sigue haciendo falta, con ventanas de detalle opcionales que enumeran cada archivo
4. **Elimina** los archivos innecesarios: los envía a la Papelera de reciclaje, o los mueve a una carpeta que tú elijas

## Capturas de pantalla

<p>
  <img src="docs/screenshots/01-initial-scan.webp" alt="Pantalla de bienvenida con el logotipo de InstallerClean mientras se ejecuta el análisis" width="900"><br>
  <em>Análisis inicial. Es muy rápido.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/02-main-window.webp" alt="Ventana principal con 120 archivos que siguen haciendo falta (2,83 GB) y 69 archivos innecesarios para limpiar (1,28 GB), con un cuadro de ubicación para mover y los botones Eliminar y Mover" width="900"><br>
  <em>Resultados: cuánto sigue haciendo falta y cuánto se puede eliminar.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/03-details-registered.webp" alt="Ventana de archivos registrados con los productos instalados y los detalles de la base de datos del instalador para el producto seleccionado" width="900"><br>
  <em>Detalle de los archivos que siguen haciendo falta, con los metadatos leídos de la base de datos del instalador.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/04-details-safe-to-delete.webp" alt="Ventana de archivos innecesarios con los archivos .msi eliminables ordenados por tamaño, el motivo por el que cada uno es eliminable y los detalles del archivo seleccionado" width="900"><br>
  <em>Detalle de los archivos que ya no hacen falta.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/05-delete-dialogue.webp" alt="Confirmación de eliminación que pregunta si eliminar 69 archivos (1,28 GB) e indica que los archivos se enviarán a la Papelera de reciclaje" width="900"><br>
  <em>Confirmación antes de cada acción. Eliminar envía a la Papelera de reciclaje; Mover coloca los archivos donde tú elijas.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/06-freed-success-done.webp" alt="Superposición de éxito que muestra 1,28 GB liberados, con 69 archivos enviados a la Papelera de reciclaje" width="900"><br>
  <em>Tras una eliminación correcta.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/07-scanned-again-all-clean.webp" alt="Superposición de «todo limpio» tras un nuevo análisis: no queda nada que limpiar en C:\Windows\Installer" width="900"><br>
  <em>Tras un nuevo análisis. No queda nada que limpiar.</em>
  <br><br>
</p>

## Cómo funciona

InstallerClean identifica tres tipos de archivos innecesarios.

**Los archivos huérfanos** son los instaladores `.msi` (y cualquier parche `.msp`) que quedan tras desinstalar un programa. Windows ya no los referencia, pero siguen en la carpeta ocupando espacio.

**Los parches sustituidos** son parches `.msp` antiguos que han sido reemplazados por otros más nuevos. Windows los marca como sustituidos en su propia base de datos, pero nunca los borra. Los proveedores que publican parches con frecuencia (Acrobat, Office, grandes herramientas de desarrollo) van acumulando parches sustituidos de forma indefinida.

**Los parches obsoletos** son parches `.msp` que el fabricante ha retirado o dado de baja en lugar de reemplazarlos por una versión más reciente. Windows también registra ese estado y, de igual modo, deja el archivo en la carpeta.

Para encontrarlos, InstallerClean llama directamente a la interfaz COM de Windows Installer mediante P/Invoke:

- `MsiEnumProductsEx` para enumerar todos los productos instalados
- `MsiEnumPatchesEx` para encontrar todos los parches registrados de cada producto
- `MsiGetPatchInfoEx` para leer el estado de cada parche (aplicado, sustituido u obsoleto)

Todo archivo `.msi` o `.msp` de `C:\Windows\Installer` que no pertenezca a ningún producto registrado es huérfano y se marca como eliminable. Lo mismo ocurre con cualquier parche que la base de datos marque como sustituido u obsoleto y que no haga falta para la desinstalación.

Si la API devuelve datos incompletos (algo raro, pero que puede ocurrir si el estado del instalador está dañado), la aplicación recurre a leer el registro. Esa lectura de reserva solo añade archivos al conjunto de «aún necesarios», nunca al de «eliminables».

Tras completar un Mover o un Eliminar, las subcarpetas vacías que haya dentro de `C:\Windows\Installer` (los directorios que la caché deja atrás cuando su contenido desaparece) se podan en la misma pasada.

## ¿Es seguro?

Sí. InstallerClean consulta la misma base de datos de la API de Windows Installer que el propio Windows usa para llevar el control de lo que está instalado. Si Windows dice que un archivo ya no hace falta, la aplicación se fía; no adivina a partir de nombres de archivo ni fechas.

**Sobre Eliminar y Mover.** Los archivos que InstallerClean elimina se pueden borrar de forma permanente sin riesgo. **Eliminar** los envía a la Papelera de reciclaje (se te avisará si no está disponible); recuperas el espacio en tu unidad C: cuando vacías la Papelera de reciclaje.

Aun así, no tienes que fiarte de mi palabra de que se pueden borrar sin riesgo. Mientras están en la Papelera de reciclaje, tienes ocasión de comprobar que las aplicaciones que usan esta carpeta (Office, Acrobat, Visual Studio y similares) siguen actualizándose y desinstalándose sin problemas. Si algo falla (¡no fallará!), restaura los archivos desde la Papelera de reciclaje para arreglarlo. Para mayor seguridad todavía, puedes usar **Mover** en su lugar, para dejar los archivos en una carpeta que tú elijas (obviamente, elige una carpeta en otra partición o unidad si lo que buscas es liberar espacio en C:). Solo tienes que volver a copiar los archivos a `C:\Windows\Installer` para dejar las cosas como estaban (¡pero no te hará falta!).

Si Windows Installer está escribiendo en la caché en ese momento, tiene una transacción anterior suspendida o tiene un renombrado pendiente tras reiniciar que apunta a la caché, Mover y Eliminar quedan desactivados y se muestra el motivo concreto.

Los servicios de análisis, consulta, movimiento, eliminación, configuración y comprobación de reinicio pendiente están cubiertos por una batería de pruebas automatizadas que se ejecuta en cada commit (consulta la insignia de CI más arriba).

**Verificación del binario.** InstallerClean no está firmado, así que no tienes que fiarte sin más:

- Los hashes SHA-256 de cada versión están listados en la [página de versiones](../../releases/latest).
- VirusTotal: limpio en todos los motores. Hay enlaces en vivo en las notas de cada versión para que puedas volver a comprobarlo.
- El código fuente está en [github.com/no-faff/InstallerClean](https://github.com/no-faff/InstallerClean) y la CI compila y prueba cada commit (consulta la insignia verde de CI más arriba).
- <!-- downloads-start -->25k<!-- downloads-end --> descargas entre GitHub, MajorGeeks y Softpedia.
- [MajorGeeks](https://www.majorgeeks.com/files/details/installerclean.html) prueba cada envío en una máquina virtual y solo lo publica si pasa su revisión.
- [Softpedia](https://www.softpedia.com/get/System/Hard-Disk-Utils/InstallerClean.shtml) analiza cada versión en busca de virus, spyware y adware.

<a href="https://www.softpedia.com/get/System/Hard-Disk-Utils/InstallerClean.shtml"><img src="docs/badges/softpedia-100-free2.webp" alt="Certificado 100 % limpio por Softpedia" width="190"></a>

<a id="recovery"></a>
## Si te llega a faltar un archivo de `C:\Windows\Installer`

InstallerClean solo elimina los archivos que el propio Windows da por innecesarios, así que nunca puede ser la causa de que falte un archivo. Pero si ya ha desaparecido alguno, InstallerClean lo detecta y lo señala. Aquí tienes la solución.

Descarga el instalador de ese programa desde su fabricante y ejecútalo sobre tu instalación existente; no desinstales primero. Usa la versión que tienes ahora si puedes, porque Windows puede rechazar una distinta. Eso normalmente devuelve el archivo a su sitio y deja tu configuración intacta. Vuelve a analizar en InstallerClean y, si ha funcionado, el aviso habrá desaparecido.

Eso suele funcionar. Lo que sigue es la versión más completa de la propia Microsoft: el detalle oficial, y los casos más difíciles para cuando no es tan sencillo. Nada de esto tiene que ver con InstallerClean, y no puedo mejorar las indicaciones de Microsoft, así que me limito a transmitírtelas.

<details>
<summary>La posición más completa de Microsoft</summary>

*Las citas de Microsoft que aparecen a continuación se reproducen en su versión original en inglés.*

Guía completa: [Restore missing Windows Installer cache files](https://learn.microsoft.com/en-us/troubleshoot/windows-client/application-management/missing-windows-installer-cache).

*Puede que no se manifieste de inmediato:*
> "If the installer cache is compromised, you may not immediately see problems until you take an action such as uninstalling, repairing, or updating a product."

*Los archivos son únicos en cada equipo, así que no puedes copiar uno desde otro PC:*
> "Missing files cannot be copied between computers because the files are unique."

*Tampoco puedes recuperar solo el archivo de una copia de seguridad:*
> "To restore the missing files, a full system state restoration is required. It is not possible to replace only the missing files from a previous backup."

*La recuperación recomendada, y sus límites sin rodeos:*
> "If application files are missing from the Windows Installer Cache, ask the vendor or support team for the application about the missing files. You must follow the procedures or steps recommended by the application vendor to restore the files. In some cases, you may have to rebuild the operating system and reinstall the application to fix the problem."
>
> "Windows support engineers cannot help you recover missing application files from the Windows Installer cache."

*Por qué importa usar la misma versión:*
> "The upgrade cannot be installed by the Windows Installer service because the program to be upgraded may be missing, or the upgrade may update a different version of the program."

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
- Solo se conecta a internet cuando se lo pides: una comprobación de actualizaciones manual; el resumen anónimo opcional (solo para que yo sepa que funciona); y enlaces a la documentación de GitHub y a una página de donaciones, que se abren en tu navegador si decides pulsarlos.
- Sin barras de herramientas, sin software incluido, sin adware.

## Preguntas frecuentes

**¿Realmente voy a liberar GB de espacio?** Depende de tu equipo. Una instalación limpia de Windows 11 sin software adicional no tiene nada que eliminar. Una estación de trabajo de desarrollo de larga vida, o cualquier equipo con mucho software basado en MSI (Acrobat, Office, LibreOffice, grandes herramientas de desarrollo), puede tener decenas de GB. En cualquier caso, verás exactamente cuánto en cuanto lo ejecutes.

<!-- reports-stats-start (generated by non-repo-files/refresh-reports-table.mjs; do not hand-edit between these markers) -->
De los 93 informes que me han enviado amablemente (gracias 🙏) desde que la v1.8.0 añadió la opción:

| Resultado | Proporción | Mínimo | Mediana | Máximo |
|---|---|---|---|---|
| Nada que eliminar | 60 % | - | - | - |
| Espacio liberado | 40 % | 0,1 GB | 23 GB | 327 GB |
<!-- reports-stats-end -->

<details>
<summary>Esos informes provienen del botón opcional «Enviar resumen». Esto es lo que verás antes de que se envíe nada.</summary>

![Diálogo de confirmación titulado «¿Enviar esto a No Faff?» que muestra el informe completo que se enviaría: la versión de la aplicación, la versión de Windows, los recuentos del análisis, los archivos procesados y los bytes liberados, sin rutas de archivo, sin nombres ni identificadores de equipo, y con una nota de que nada te identifica a ti ni a tu equipo, solo si la aplicación funcionó y cuánto espacio se liberó, con los botones Cancelar y Enviar.](docs/screenshots/optional-send-summary-confirmation-dialogue.webp)

</details>

<a id="admin"></a>

**¿Por qué pide Administrador?** `C:\Windows\Installer` está restringido a los administradores. Leer la carpeta, consultar la base de datos del Installer y mover o eliminar archivos lo requieren, así que la aplicación tiene que ejecutarse como administrador.

<a id="unknown-publisher"></a>

**¿Por qué dice Windows «Editor desconocido»?** Porque InstallerClean no está firmado digitalmente. Un certificado de firma cuesta dinero todos los años, y prefiero mantener la aplicación gratuita antes que pagar por uno. Así que, al ejecutarla, Windows SmartScreen muestra «Windows protegió su PC». Pulsa **Más información** y luego **Ejecutar de todas formas**. Hacerlo es seguro: el código fuente es público, y cada versión incluye enlaces a VirusTotal y hashes SHA-256 que puedes comprobar antes.

**¿Puedo deshacer una eliminación?** Normalmente, sí. Cuando la Papelera está disponible para la unidad, Eliminar envía los archivos ahí y puedes restaurarlos desde la Papelera. Si no está disponible, la aplicación nunca borra para siempre por su cuenta (consulta [¿Es seguro?](#es-seguro)). Y si prefieres tener una vía de vuelta que tú controlas, Mover coloca los archivos en una carpeta que tú elijas; bórralos de ahí cuando te quedes tranquilo.

**¿Va a quejarse Windows si quito estos archivos?** No. InstallerClean solo elimina los archivos que el propio Windows da por terminados, así que nada de lo que elimina hace falta para reparar, actualizar o desinstalar un programa. Si un archivo necesario llega a desaparecer de `C:\Windows\Installer` por algún otro medio, consulta [Si te llega a faltar un archivo de C:\Windows\Installer](#recovery).

**¿Por qué no `Win32_Product` (WMI)?** [`Win32_Product` desencadena operaciones de reparación de MSI en cada producto durante la enumeración](https://gregramsey.net/2012/02/20/win32_product-is-evil/), lo cual puede tardar minutos y cargar mucho el disco. InstallerClean llama a la API COM de Windows Installer directamente, sin efectos colaterales.

**¿Por qué no simplemente un script de PowerShell?** Un script corto que llame a `MsiEnumPatchesEx` basta para *listar* parches, pero las partes que sostienen InstallerClean son las que un script pasa por alto: la clasificación de huérfano frente a sustituido, la lectura de reserva del registro que solo añade archivos al conjunto de «aún necesarios» (nunca al de «eliminables»), el bloqueo por reinicio pendiente, la red de seguridad de mover a otra ubicación, el progreso por archivo con cancelación y el uso de la Papelera de reciclaje en lugar del borrado permanente por defecto. Los casos límite en equipos reales con mucho MSI (registros de productos dañados, uniones dentro de la caché, productos en `HKU\.DEFAULT`, transacciones del Installer suspendidas) son fáciles de gestionar mal en un script improvisado. La `installerclean-cli` es la cara sin interfaz si lo que buscas es scripting.

**¿Funciona en Windows 7 u 8?** Sin probar y sin soporte. Está pensado para Windows 10 y 11.

**¿Sirve para RMM o despliegue masivo?** Sí. La CLI sale con códigos distintos por resultado (0 éxito, 2 parcial, 1 fallo total, 75 transitorio, 130 para un Ctrl+C antes de procesar ningún archivo; un Ctrl+C que cae a mitad del lote sale con 2, parcial, porque ya se había hecho trabajo), de modo que una tarea programada puede reintentar en 75 sin confundirlo con un fallo total. Escribe un resumen por ejecución en el registro de eventos de Aplicación y respeta el mismo mutex de instancia única que la interfaz gráfica. El programa de instalación también se instala en silencio con los modificadores estándar de Inno Setup (`/SILENT` o `/VERYSILENT`); el lanzamiento posterior a la instalación se omite en las instalaciones silenciosas. Consulta la sección Línea de comandos.

## Descarga

Tres variantes, elige una:

- **Setup** (`InstallerClean-setup.exe`): un instalador clásico de Windows con el runtime de .NET 10 incluido. Añade una entrada en el menú Inicio y se desinstala sin dejar rastro. Bien guardado en Programas, fácil de encontrar dentro de seis meses.
- **Portable** (`InstallerClean-portable.exe`): un único exe autónomo con el runtime incluido. Sin instalación, sin desinstalador. Ejecútalo, úsalo, bórralo. Vuelve a ejecutarlo cuando quieras.
- **CLI** (`installerclean-cli.exe`): la versión de línea de comandos por sí sola, un único exe autónomo. Sin instalación, sin dejar nada en la máquina después. Déjalo en un equipo cliente, ejecuta un análisis o una limpieza, y bórralo. Pensado para scripting, tareas programadas y despliegue masivo, cuando quieres las operaciones sin una aplicación de escritorio en el cliente. Consulta [Línea de comandos](#línea-de-comandos) para los argumentos y los códigos de salida.

Descárgala desde la [página de versiones](../../releases/latest) y ejecútala. No está firmada, así que Windows muestra un aviso de «editor desconocido»; las [Preguntas frecuentes](#unknown-publisher) explican lo que verás y por qué es seguro.

La aplicación analiza automáticamente al arrancar. Revisa los resultados y pulsa **Eliminar** o **Mover**.

O instálalo con [winget](https://learn.microsoft.com/windows/package-manager/winget/):

```
winget install NoFaff.InstallerClean
```

O instálalo con [Scoop](https://scoop.sh):

```
scoop bucket add no-faff https://github.com/no-faff/scoop-bucket
scoop install installerclean
```

## Comparativa con PatchCleaner

Si ya has buscado esta carpeta antes, la herramienta que con más probabilidad habrás encontrado es [PatchCleaner](https://www.homedev.com.au/free/patchcleaner). Sigue funcionando bien, pero hice InstallerClean porque PatchCleaner es de código cerrado, no se actualiza desde marzo de 2016 y, por defecto, no toca los productos de Adobe. Su comprobación de huérfanos marcaba por error los parches de Adobe, y quitarlos rompía las actualizaciones de Adobe, así que deja en paz todos los archivos de Adobe a menos que desactives el filtro. En los equipos donde Adobe es el mayor responsable, ahí está la mayor parte del espacio:

> *«He descargado PatchCleaner para borrar los archivos `.msp` huérfanos, pero al parecer esto solo liberaría 250 MB de espacio. 29 GB de los archivos están "excluidos por filtros", así que PatchCleaner no parece servir de ayuda.»*
>
> HeatherBunny1111, [r/techsupport](https://www.reddit.com/r/techsupport/comments/1qc4tcf/how_to_delete_msp_files_safely/) (traducido del inglés)

InstallerClean lee los propios registros de parches de Windows Installer, así que puede distinguir qué parches de Adobe están realmente sustituidos y eliminarlos con seguridad, sin un filtro general. Así es como se comparan las dos:

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
| Seguridad al eliminar | Papelera de reciclaje. Si no está disponible, pregunta: mover en su lugar o borrar de forma permanente | Permanente, sin Papelera |

> **Nota sobre `Win32_Product`:** El enfoque común pero defectuoso para listar productos instalados es `Win32_Product` (WMI), que [desencadena operaciones de reparación de MSI](https://gregramsey.net/2012/02/20/win32_product-is-evil/) en cada producto durante la enumeración. Tanto InstallerClean como PatchCleaner lo evitan. Ambos usan la interfaz COM de Windows Installer. El nombre de archivo `WMIProducts.vbs` del script de PatchCleaner resulta engañoso; el script usa COM de MSI, no WMI.

[Ultra Virus Killer (UVK)](https://www.carifred.com/uvk/) también ofrece limpieza del Installer como parte de su módulo System Booster, pero es una herramienta de pago (15-25 USD) y la limpieza es una pequeña función dentro de una aplicación mucho mayor. InstallerClean es gratuito, especializado y de código abierto.

Los limpiadores generalistas como [CCleaner](https://www.ccleaner.com/) y [BleachBit](https://www.bleachbit.org/) no tocan `C:\Windows\Installer`. La carpeta necesita consultas a la API de Windows Installer para distinguir los paquetes registrados de los innecesarios, y un limpiador genérico que se limitara a recorrer el árbol de archivos podría romper aplicaciones instaladas. InstallerClean es la herramienta a la que recurrir cuando esa es precisamente la carpeta que quieres limpiar.

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

`/d` y `/m` analizan y luego actúan. `/d` envía los archivos eliminables a la Papelera de reciclaje. `/m` los mueve a una carpeta (la que indiques en la línea de comandos, o la guardada por defecto desde la interfaz gráfica). Códigos de salida: `0` éxito completo, `2` parcial (algunos archivos correctos, otros fallidos), `1` fallo total (análisis fallido, argumentos incorrectos o todos los archivos del lote han fallado), `75` una condición transitoria bloqueó la ejecución (el mensaje mostrado indica cuál y si reintentar servirá de algo), `130` para un Ctrl+C antes de procesar ningún archivo (un Ctrl+C que cae a mitad del lote sale con `2`, parcial, porque ya se había hecho trabajo).

Toda la salida de la CLI, incluidos los mensajes de error y de diagnóstico, va a stdout; no hay un flujo stderr aparte. El código de salida es la señal legible por máquina (y la entrada por ejecución en el registro de eventos de Aplicación lo refleja), así que un script debería basarse en el código de salida en lugar de analizar el texto, y `installerclean-cli /s > audit.txt` captura toda la ejecución, incluida cualquier línea de error.

Las tres requieren un símbolo del sistema elevado (administrador). Si una directiva de grupo bloquea el aviso de elevación de UAC, el proceso se niega a iniciarse y Windows devuelve el error 740 al shell que lo invocó (`$LASTEXITCODE = 740` en PowerShell). `taskkill /pid <pid>` no provoca una cancelación controlada; el mutex de instancia única se recupera en la siguiente ejecución mediante la vía AbandonedMutexException.

Nota: la salida de la propia CLI está en inglés. Las descripciones anteriores corresponden a las opciones disponibles.

### ¿Por qué `installerclean-cli` y no `installerclean.exe`?

`InstallerClean.exe` es la interfaz gráfica WPF; no responde a argumentos de línea de comandos. `installerclean-cli.exe` es un ejecutable de consola aparte que se incluye en el mismo directorio de instalación y expone las mismas operaciones de análisis / movimiento / eliminación a PowerShell, cmd y tareas programadas. Como es un proceso de consola real, bloquea la consola hasta que termina; redirige o canaliza su salida igual que con cualquier otro exe de consola.

La descarga portable solo contiene el exe de la interfaz gráfica. Si quieres la línea de comandos sin la interfaz, descarga `installerclean-cli.exe` desde la [página de versiones](../../releases/latest) y ejecútalo directamente. El programa de instalación también lo instala junto a la interfaz gráfica.

## Requisitos

- Windows 10 (versión 1607 / compilación 14393 o posterior, la más antigua que admite el runtime de .NET 10) o Windows 11
- Privilegios de administrador (a `C:\Windows\Installer` solo pueden acceder los administradores)

Consulta [Descarga](#descarga) para ver las variantes setup, portable y CLI.

## Compilar desde el código fuente

```
git clone https://github.com/no-faff/InstallerClean.git
cd InstallerClean
dotnet build src/InstallerClean.sln
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
