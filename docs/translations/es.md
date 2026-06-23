# InstallerClean UI in Español (Spanish)

The text of InstallerClean's interface in English on the left, with the Spanish translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Spanish can read through the translation and flag anything that doesn't read well. See [Can you help translate the GUI?](../../README.es.md#can-you-help-translate-the-gui) for how to suggest a change, whether an issue or a pull request.

A few lines (the app name, version and file-size formats) are meant to stay the same in every language, so leave those as they are. The translation file itself is [`Strings.es.resx`](../../src/InstallerClean.Core/Resources/Strings.es.resx). This page is generated from it by `scripts/gen-translation-table.mjs`, so do not edit it by hand.

## Window titles

| English | Español |
| --- | --- |
| InstallerClean | InstallerClean |
| About | Acerca de |
| Registered files that should not be deleted | Archivos registrados que no deberían eliminarse |
| Unneeded files that are safe to delete | Archivos innecesarios que puedes eliminar sin riesgo |
| Confirm move | Confirmar movimiento |
| Confirm delete | Confirmar eliminación |
| Recycle Bin unavailable | Papelera de reciclaje no disponible |

## Section headings

| English | Español |
| --- | --- |
| PRODUCTS | PRODUCTOS |
| PATCHES | PARCHES |
| PRODUCT DETAILS | DETALLES DEL PRODUCTO |
| MOVE LOCATION | UBICACIÓN DE DESTINO |
| SAY THANKS | DAR LAS GRACIAS |

## Buttons and actions

| English | Español |
| --- | --- |
| _About | _Acerca de |
| Copy | Copiar |
| Cut | Cortar |
| Paste | Pegar |
| Select all | Seleccionar todo |
| _Browse... | E_xaminar... |
| _Cancel | _Cancelar |
| Check for _updates | Buscar _actualizaciones |
| _Close | _Cerrar |
| _Delete | _Eliminar |
| _Delete permanently | _Eliminar definitivamente |
| _Done | _Listo |
| Details | Detalles |
| _Buy me a cuppa | _Invítame a un café |
| Leave a _star on GitHub | Deja una e_strella en GitHub |
| MIT licence | Licencia MIT |
| _Move | _Mover |
| _Move instead | _Mover en su lugar |
| Path to folder if you Move instead of Delete | Ruta de la carpeta si eliges Mover en lugar de Eliminar |
| Open _release page | Abrir la página de la _versión |
| _Re-scan | _Volver a analizar |
| _Scan again | Analizar de _nuevo |
| Send summary | Enviar resumen |
| _Send | _Enviar |

## Field labels

| English | Español |
| --- | --- |
| Reason | Motivo |
| Author | Autor |
| Application | Aplicación |
| Title | Título |
| Subject | Asunto |
| Keywords | Palabras clave |
| Signing certificate | Certificado de firma |
| File size | Tamaño del archivo |
| Comment | Comentario |
| Product name | Nombre del producto |
| File | Archivo |
| Size | Tamaño |
| Patches | Parches |
| (unknown) | (desconocido) |
| (patches only) | (solo parches) |
| missing | ausente |

## Status and progress

| English | Español |
| --- | --- |
| Scanning... | Analizando... |
| Cancelling... | Cancelando... |
| Starting scan... | Iniciando el análisis... |
| Asking Windows about installed software... | Consultando a Windows el software instalado... |
| Scanning installer cache folder... | Analizando la carpeta de la caché de instalación... |
| Enumerating installed products... | Enumerando los productos instalados... |
| Checking registry for additional packages... | Comprobando el registro en busca de paquetes adicionales... |
| Found {0} registered {1}. | Se encontraron {0} {1} registrados. |
| Scan complete ({0}) | Análisis completado ({0}) |
| Scanning local packages... | Analizando los paquetes locales... |
| Found {0} {1} to clean up. | {0} {1} para limpiar. |
| Preparing destination folder... | Preparando la carpeta de destino... |
| Moving {0} {1}... | Moviendo {0} {1}... |
| Deleting {0} {1}... | Eliminando {0} {1}... |
| Move cancelled. {0} of {1} {2} processed. | Movimiento cancelado tras procesar {0} de {1} {2}. |
| Delete cancelled. {0} of {1} {2} processed. | Eliminación cancelada tras procesar {0} de {1} {2}. |
| Move failed ({0}). Details in {1}. | Movimiento fallido ({0}). Detalles en {1}. |
| Move failed ({0}). The crash log could not be written. | Movimiento fallido ({0}). No se pudo escribir el archivo crash.log. |
| Delete failed ({0}). Details in {1}. | Eliminación fallida ({0}). Detalles en {1}. |
| Delete failed ({0}). The crash log could not be written. | Eliminación fallida ({0}). No se pudo escribir el archivo crash.log. |
| Access denied. Run as administrator. | Acceso denegado. Ejecuta como administrador. |
| Scan failed: installer database unavailable. | Análisis fallido: base de datos del instalador no disponible. |
| Scan cancelled. | Análisis cancelado. |
| Done | Listo |
| Scan failed ({0}). Details in {1}. | Análisis fallido ({0}). Detalles en {1}. |
| Scan failed ({0}). The crash log could not be written. | Análisis fallido ({0}). No se pudo escribir el archivo crash.log. |

## Main screen text

| English | Español |
| --- | --- |
| The unneeded files below are safe to delete. | Los archivos innecesarios de abajo se pueden eliminar sin riesgo. |
| They sit in C:\Windows\Installer, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | Están en C:\Windows\Installer, donde quedaron cuando se desinstaló un programa ({0}), un parche más reciente sustituyó a otro ({1}) o el fabricante lo retiró ({2}). InstallerClean solo enumera archivos que el propio Windows da por terminados. |
| Delete them to the Recycle Bin, or Move them elsewhere first if you'd rather keep a copy. | Elimínalos y se enviarán a la Papelera de reciclaje, o muévelos primero a otro sitio si prefieres conservar una copia. |
| Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch the installer cache while it's changing. Once it's done, Re-scan and they come back. | Ahora mismo algo está usando Windows Installer, normalmente una actualización de Windows o un programa instalándose en segundo plano. Mover y Eliminar quedan en pausa mientras eso ocurre, de modo que InstallerClean no toca la caché de instalación mientras está cambiando. Cuando termine, vuelve a analizar y volverán a estar disponibles. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | Hay una transacción anterior de Windows Installer suspendida en este equipo. Reanuda o revierte esa instalación (o reinicia Windows) antes de limpiar la caché. |
| Windows has a file rename queued for the next restart that affects the Installer cache. Restart Windows before cleaning. | Windows tiene en cola, para el próximo reinicio, el cambio de nombre de un archivo que afecta a la caché de instalación. Reinicia Windows antes de limpiar. |
| Select a file to view details. | Selecciona un archivo para ver sus detalles. |
| Select a product to view details. | Selecciona un producto para ver sus detalles. |
| No metadata available. | No hay metadatos disponibles. |
| This installer file has been deleted. InstallerClean didn't do it, it never removes a file a program still needs; something else deleted this one before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. | Este archivo de instalación se ha eliminado. No ha sido InstallerClean: nunca quita un archivo que un programa todavía necesita; algo más lo eliminó antes de que ejecutaras InstallerClean.<br><br>Por ahora no causa ningún problema, y no lo hará hasta el día en que intentes reparar, actualizar o desinstalar el programa al que pertenece. Ese paso puede fallar entonces, porque Windows busca este archivo y no está.<br><br>Para intentar arreglarlo, descarga el instalador de ese programa desde su fabricante y ejecútalo sobre tu copia existente (no desinstales primero: desinstalar es, en sí mismo, un paso que necesita este archivo). Usa la versión que tienes instalada si puedes conseguirla, ya que Windows podría rechazar una distinta. Esto suele restaurar el archivo, y tu configuración normalmente queda intacta, pero Microsoft no lo garantiza: su propio último recurso es reinstalar el programa, o el propio Windows. |
| The README [explains this folder], and how to recover a file, in Microsoft's own words. | El README [explica esta carpeta], y cómo recuperar un archivo, con las propias palabras de Microsoft. |
| (none) | (ninguno) |

## Reasons a file is unneeded

| English | Español |
| --- | --- |
| Orphaned | Huérfano |
| Superseded | Sustituido |
| Obsoleted | Obsoleto |

## Completion screen

| English | Español |
| --- | --- |
| All clean | Todo limpio |
| Nothing to clean up in C:\Windows\Installer | Nada que limpiar en C:\Windows\Installer |
| Scanned {0} {1} in {2} | Análisis de {0} {1} en {2} |
| Copy them back if anything stops working | Vuelve a copiarlos a su sitio si algo deja de funcionar |
| Restore them from the Recycle Bin if anything stops working | Restáuralos desde la Papelera de reciclaje si algo deja de funcionar |
| {0} freed | {0} liberados |
| {0} moved | {0} movidos |
| {0} moved, some files could not be processed | {0} movidos, no se pudieron procesar algunos archivos |
| {0} freed, some files could not be processed | {0} liberados, no se pudieron procesar algunos archivos |
| {0} {1} moved to {2} | {0} {1} en {2} |
| {0} {1} moved to {2}. {3} {4} | {0} {1} en {2}. {3} {4} |
| {0} {1} sent to the Recycle Bin | {0} {1} en la Papelera de reciclaje |
| {0} {1} sent to the Recycle Bin. {2} {3} | {0} {1} en la Papelera de reciclaje. {2} {3} |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. | {0} {1} eliminado definitivamente. No fue a la Papelera de reciclaje. |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. | {0} {1} eliminados definitivamente. No fueron a la Papelera de reciclaje. |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. {2} {3} | {0} {1} eliminado definitivamente. No fue a la Papelera de reciclaje. {2} {3} |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. {2} {3} | {0} {1} eliminados definitivamente. No fueron a la Papelera de reciclaje. {2} {3} |
| That's fine, it was safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | No pasa nada, se podía eliminar sin riesgo. InstallerClean solo elimina los archivos que Windows da por terminados, nunca uno que un programa todavía necesita. En el caso improbable de que una eliminación llegara a dejar un programa sin poder repararse, actualizarse o desinstalarse, reinstalarlo desde su fabricante suele restaurar el archivo, aunque Microsoft no lo garantiza. |
| That's fine, they were safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | No pasa nada, se podían eliminar sin riesgo. InstallerClean solo elimina los archivos que Windows da por terminados, nunca uno que un programa todavía necesita. En el caso improbable de que una eliminación llegara a dejar un programa sin poder repararse, actualizarse o desinstalarse, reinstalarlo desde su fabricante suele restaurar el archivo, aunque Microsoft no lo garantiza. |

## Recycle Bin unavailable

| English | Español |
| --- | --- |
| The Recycle Bin isn't available for this drive | La Papelera de reciclaje no está disponible para esta unidad |
| So this {1} ({2}) hasn't been deleted. You can move it somewhere safe, or delete it permanently. | Así que este {1} ({2}) no se ha eliminado. Puedes moverlo a un lugar seguro, o eliminarlo definitivamente. |
| So these {0} {1} ({2}) haven't been deleted. You can move them somewhere safe, or delete them permanently. | Así que estos {0} {1} ({2}) no se han eliminado. Puedes moverlos a un lugar seguro, o eliminarlos definitivamente. |
| Deleting it is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Eliminarlo es seguro. InstallerClean solo elimina los archivos que Windows da por terminados, nunca uno que un programa todavía necesita, y la Papelera de reciclaje es solo una protección adicional. En el caso improbable de que una eliminación llegara a dejar un programa sin poder repararse, actualizarse o desinstalarse, reinstalarlo desde su fabricante suele restaurar el archivo, aunque Microsoft no lo garantiza. |
| Deleting them is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Eliminarlos es seguro. InstallerClean solo elimina los archivos que Windows da por terminados, nunca uno que un programa todavía necesita, y la Papelera de reciclaje es solo una protección adicional. En el caso improbable de que una eliminación llegara a dejar un programa sin poder repararse, actualizarse o desinstalarse, reinstalarlo desde su fabricante suele restaurar el archivo, aunque Microsoft no lo garantiza. |

## Summaries and counts

| English | Español |
| --- | --- |
| {0} file still needed | {0} archivo aún necesario |
| {0} files still needed | {0} archivos aún necesarios |
| {0} unneeded file to clean up | {0} archivo innecesario para limpiar |
| {0} unneeded files to clean up | {0} archivos innecesarios para limpiar |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | Falta {0} archivo registrado (no lo ha eliminado InstallerClean). Por ahora sin problemas, pero en el futuro una reparación, actualización o desinstalación de ese programa podría fallar. Abre Detalles para saber qué hacer. |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | Faltan {0} archivos registrados (no los ha eliminado InstallerClean). Por ahora sin problemas, pero en el futuro una reparación, actualización o desinstalación de esos programas podría fallar. Abre Detalles para saber qué hacer. |
| {0} stale MSI entry detected (file already gone from disk; InstallerClean doesn't unregister it). | Detectada {0} entrada MSI obsoleta (el archivo ya no está en el disco; InstallerClean no la quita del registro). |
| {0} stale MSI entries detected (files already gone from disk; InstallerClean doesn't unregister them). | Detectadas {0} entradas MSI obsoletas (los archivos ya no están en el disco; InstallerClean no las quita del registro). |
| {0} of {1} {2} | {0} de {1} {2} |
| {0} unneeded {1} ({2}) | {0} {1} para limpiar ({2}) |
| {0} registered {1} ({2}) | {0} {1} registrados ({2}) |

## Confirmation dialogs

| English | Español |
| --- | --- |
| Move {0} {1} ({2})? | ¿Mover {0} {1} ({2})? |
| Files will be moved to {0}. | Los archivos se moverán a {0}. |
| Delete {0} {1} ({2})? | ¿Eliminar {0} {1} ({2})? |
| Files will be sent to the Recycle Bin. If you'd like backup copies, use Move instead. | Los archivos se enviarán a la Papelera de reciclaje. Si quieres copias de seguridad, usa Mover en su lugar. |

## Error messages

| English | Español |
| --- | --- |
| Administrator rights required | Se requieren permisos de administrador |
| This app requires administrator privileges.<br><br>Please right-click and choose 'Run as administrator'. | Esta aplicación requiere privilegios de administrador.<br><br>Haz clic con el botón derecho y elige 'Ejecutar como administrador'. |
| Installer database unavailable | Base de datos del instalador no disponible |
| Scan failed | Análisis fallido |
| The Windows Installer database appears to be empty or inaccessible. This is unusual even on a fresh Windows install and typically means the database is corrupt or a third-party tool has cleared it. Running 'sfc /scannow' from an elevated prompt usually repairs it. | La base de datos de Windows Installer parece estar vacía o inaccesible. Es algo inusual incluso en una instalación de Windows recién hecha y normalmente significa que la base de datos está dañada o que una herramienta de terceros la ha vaciado. Ejecutar 'sfc /scannow' desde un símbolo del sistema con privilegios elevados suele repararla. |
| Access denied enumerating installed products. Run as administrator. | Acceso denegado al enumerar los productos instalados. Ejecuta como administrador. |
| Windows Installer refused to list products after {0} consecutive failures (last error code {1}). Try restarting Windows, or run 'sfc /scannow' from an elevated prompt. | Windows Installer se negó a enumerar los productos tras {0} errores consecutivos (último código de error {1}). Prueba a reiniciar Windows, o ejecuta 'sfc /scannow' desde un símbolo del sistema con privilegios elevados. |
| Invalid destination | Destino no válido |
| Could not write to destination | No se pudo escribir en el destino |
| Move failed | Movimiento fallido |
| Delete failed | Eliminación fallida |
| The destination cannot be inside the Windows Installer folder. | El destino no puede estar dentro de la carpeta de Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | El destino {0} se encuentra dentro de una carpeta de sistema de Windows. Elige una ruta fuera de %SystemRoot%, %ProgramFiles% y %ProgramData%. |
| Not enough space | Espacio insuficiente |
| Not enough space at {0}<br><br>Required: {1}<br>Available: {2} | Espacio insuficiente en {0}<br><br>Necesario: {1}<br>Disponible: {2} |
| You don't have permission to write to {0}.<br>Try a folder in your user profile or on a drive you own. | No tienes permiso para escribir en {0}.<br>Prueba con una carpeta de tu perfil de usuario o en una unidad de tu propiedad. |
| The path {0} is too long for Windows. Pick a shorter path. | La ruta {0} es demasiado larga para Windows. Elige una ruta más corta. |
| The folder {0} does not exist and could not be created. Check the drive letter or network path. | La carpeta {0} no existe y no se pudo crear. Comprueba la letra de la unidad o la ruta de red. |
| Windows cannot write to {0}.<br>Details in {1}. | Windows no puede escribir en {0}.<br>Detalles en {1}. |
| Windows cannot write to {0}. The crash log could not be written. | Windows no puede escribir en {0}. No se pudo escribir el archivo crash.log. |
| Cannot write to {0}.<br>Details in {1}. | No se puede escribir en {0}.<br>Detalles en {1}. |
| Cannot write to {0}. The crash log could not be written. | No se puede escribir en {0}. No se pudo escribir el archivo crash.log. |
| File no longer exists. | El archivo ya no existe. |
| Source file is a symlink or junction; refused for safety. | El archivo de origen es un enlace simbólico o un punto de unión; rechazado por seguridad. |
| Access denied. | Acceso denegado. |
| The operation failed. Try again or restart Windows. | La operación falló. Inténtalo de nuevo o reinicia Windows. |
| Unknown error. | Error desconocido. |
| Couldn't send this file to the Recycle Bin (error {0}). It may be locked, in use or blocked by Windows. Move it instead. | No se pudo enviar este archivo a la Papelera de reciclaje (error {0}). Puede estar bloqueado, en uso o impedido por Windows. Muévelo en su lugar. |
| Windows blocked access to this file, even with administrator rights (error {0}). It is usually an ownership or permissions lock. Move it instead. | Windows bloqueó el acceso a este archivo, incluso con permisos de administrador (error {0}). Suele ser un bloqueo de propiedad o de permisos. Muévelo en su lugar. |
| This file is open or locked by another program (error {0}). Close that program, or whatever is scanning it, then try again, or Move it instead. | Este archivo está abierto o bloqueado por otro programa (error {0}). Cierra ese programa, o lo que sea que lo esté analizando, y vuelve a intentarlo, o muévelo en su lugar. |
| The file was permanently deleted because it could not be sent to the Recycle Bin. | El archivo se eliminó definitivamente porque no se pudo enviar a la Papelera de reciclaje. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Se rechaza mover archivos a la carpeta de Windows Installer (destino: {0}). |
| Destination must be a fully qualified path (relative paths resolve against the process current directory and are unsafe under elevation): {0} | El destino debe ser una ruta completa (las rutas relativas se resuelven respecto al directorio actual del proceso y no son seguras con privilegios elevados): {0} |
| Destination folder canonical path changed mid-batch: {0} | La ruta canónica de la carpeta de destino cambió durante la operación: {0} |
| Cannot write to {0}. | No se puede escribir en {0}. |
| Could not find a unique filename for '{0}' after 10,000 attempts. | No se pudo encontrar un nombre de archivo único para '{0}' tras 10.000 intentos. |

## Update check

| English | Español |
| --- | --- |
| Check for updates | Buscar actualizaciones |
| Checking... | Buscando... |
| Up to date. | Estás al día. |
| Update available | Actualización disponible |
| You're running version {0}.<br>Version {1} is available. | Estás usando la versión {0}.<br>Está disponible la versión {1}. |
| Couldn't reach GitHub. Check your internet connection and try again. | No se pudo conectar con GitHub. Comprueba tu conexión a internet y vuelve a intentarlo. |
| GitHub returned an error response. The releases API may be rate-limited; try again in a few minutes. | GitHub devolvió una respuesta de error. La API de versiones puede tener un límite de frecuencia; vuelve a intentarlo en unos minutos. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | La respuesta de GitHub no contenía una versión reconocible. Vuelve a intentarlo más tarde, o abre directamente la página de versiones. |
| The check timed out. Your connection to GitHub may be slow; try again. | Se agotó el tiempo de espera de la comprobación. Tu conexión con GitHub puede ser lenta; vuelve a intentarlo. |
| The check failed for an unknown reason. Details are in crash.log if you need to report it. | La comprobación falló por un motivo desconocido. Los detalles están en crash.log por si necesitas informar del problema. |

## Opening links in your browser

| English | Español |
| --- | --- |
| Couldn't open your browser | No se pudo abrir el navegador |
| The link couldn't be opened in your normal-user browser. The URL has been copied to your clipboard so you can open it manually:<br><br>{0} | No se pudo abrir el enlace en el navegador de tu usuario normal. La URL se ha copiado al portapapeles para que puedas abrirla manualmente:<br><br>{0} |
| The link couldn't be opened in your normal-user browser, and copying it to the clipboard also failed. The URL is:<br><br>{0} | No se pudo abrir el enlace en el navegador de tu usuario normal, y tampoco se pudo copiar al portapapeles. La URL es:<br><br>{0} |

## Sending the summary

| English | Español |
| --- | --- |
| Sending... | Enviando... |
| Thanks! Summary sent. | ¡Gracias! Resumen enviado. |
| Sending failed. Try again later. | Envío fallido. Inténtalo de nuevo más tarde. |
| No summary to send. | No hay ningún resumen que enviar. |
| Send this to No Faff? | ¿Enviar esto a No Faff? |
| Nothing identifies you or your machine; it just lets me know the app is working and how much space people are freeing. It goes to nofaff.netlify.app/api/result-log. | Nada te identifica a ti ni a tu equipo; solo sirve para que yo sepa que la aplicación funciona y cuánto espacio libera la gente. Se envía a nofaff.netlify.app/api/result-log. |

## Startup and crashes

| English | Español |
| --- | --- |
| InstallerClean | InstallerClean |
| InstallerClean is already running. | InstallerClean ya se está ejecutando. |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | Se produjo un error inesperado e InstallerClean debe cerrarse.<br><br>{0}<br><br>Detalles guardados en:<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | Se produjo un error inesperado e InstallerClean debe cerrarse.<br><br>{0}<br><br>No se pudo escribir el archivo crash.log. |
| Startup error | Error de inicio |
| Failed to start ({0}). Details written to:<br>{1} | No se pudo iniciar ({0}). Detalles guardados en:<br>{1} |
| Failed to start ({0}). The crash log could not be written. | No se pudo iniciar ({0}). No se pudo escribir el archivo crash.log. |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Redact both classes of detail before attaching this file to a<br># public bug report.<br> | # crash.log registra las excepciones no controladas de InstallerClean.<br># Con privilegios elevados, los mensajes de excepción del framework<br># pueden incluir rutas de archivos de la sesión en curso (incluidos<br># los perfiles de otros usuarios enumerados por las consultas de<br># Windows Installer). Los mensajes de error de red de la comprobación<br># de actualizaciones o del envío del registro de resultados pueden<br># incluir la URL de destino y la dirección IP / proxy resuelta.<br># Elimina ambos tipos de detalle antes de adjuntar este archivo a<br># un informe de error público.<br> |

## Tooltips (hover text)

| English | Español |
| --- | --- |
| If it helped, buy me a cup of tea. | Si te ha servido, invítame a un café. |
| It's thirsty work! | ¡Esto da sed! |
| Cancellation requested. The app is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Cancelación solicitada. La aplicación está esperando a que el paso en curso llegue a un punto en el que pueda detenerse. Puede tardar unos segundos durante operaciones intensas de entrada/salida o una llamada a la base de datos MSI. |
| Close | Cerrar |
| Leave a star on GitHub, report an Issue or post in Discussions. Any feedback welcome. | Deja una estrella en GitHub, informa de un problema (Issue) o escribe en Discussions. Cualquier comentario es bienvenido. |
| or report an Issue or post in Discussions. Any feedback welcome. | o informa de un problema (Issue) o escribe en Discussions. Cualquier comentario es bienvenido. |
| Minimise | Minimizar |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | Tú decides, pero se agradece. Envía un resumen anónimo que solo sirve para que yo sepa si funciona y cuánto espacio libera la gente. La pantalla siguiente te muestra lo que se enviará antes de confirmar. |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | Tú decides, pero se agradece. Envía un resumen anónimo que solo sirve para que yo sepa si funciona. La pantalla siguiente te muestra lo que se enviará antes de confirmar. |
| Move the unneeded files to the Move location. | Mueve los archivos innecesarios a la ubicación de destino. |
| Move the unneeded files to the Move location. Choose one first. | Mueve los archivos innecesarios a la ubicación de destino. Elige una primero. |
| Send the unneeded files to the Recycle Bin. | Envía los archivos innecesarios a la Papelera de reciclaje. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Nombre del firmante del certificado Authenticode incorporado. La cadena no está verificada. |
| Change the display language. InstallerClean restarts to apply it. | Cambia el idioma de visualización. InstallerClean se reiniciará para aplicarlo. |

## Screen reader labels

| English | Español |
| --- | --- |
| Buy me a cup of tea | Invítame a una taza de café |
| Buy me a cuppa (About window) | Invítame a un café (ventana Acerca de) |
| Cancel operation | Cancelar la operación |
| Cancel scan | Cancelar el análisis |
| Cancel startup scan | Cancelar el análisis de inicio |
| Close | Cerrar |
| Close window | Cerrar la ventana |
| Close result and return to main window | Cerrar el resultado y volver a la ventana principal |
| Leave a star on GitHub | Deja una estrella en GitHub |
| Leave a star on GitHub (About window) | Deja una estrella en GitHub (ventana Acerca de) |
| Minimise | Minimizar |
| Move all unneeded installer files to the chosen destination folder | Mover todos los archivos de instalación innecesarios a la carpeta de destino elegida |
| Send all unneeded installer files to the Recycle Bin | Enviar a la Papelera de reciclaje todos los archivos de instalación innecesarios |
| Delete sends the unneeded files to the Recycle Bin. Cancel closes without deleting. | Eliminar envía los archivos innecesarios a la Papelera de reciclaje. Cancelar cierra sin eliminar. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Mover coloca los archivos innecesarios en la carpeta de destino elegida. Cancelar los deja donde están. |
| Choose how to handle the unneeded files: move them somewhere safe, delete them permanently or cancel. | Elige qué hacer con los archivos innecesarios: moverlos a un lugar seguro, eliminarlos definitivamente o cancelar. |
| Move the unneeded files to a folder you choose | Mover los archivos innecesarios a una carpeta que tú elijas |
| Delete the unneeded files permanently because the Recycle Bin is unavailable for this drive | Eliminar definitivamente los archivos innecesarios porque la Papelera de reciclaje no está disponible para esta unidad |
| Posts to nofaff.netlify.app. Counts and labels only. You will see the exact payload before sending. | Se envía a nofaff.netlify.app. Solo recuentos y etiquetas. Verás exactamente lo que se envía antes de enviarlo. |
| Say thanks | Dar las gracias |
| Send posts the summary shown to No Faff. Cancel sends nothing. | Enviar transmite a No Faff el resumen mostrado. Cancelar no envía nada. |
| Check for updates | Buscar actualizaciones |
| Checks the GitHub releases API over HTTPS for a newer version. | Consulta la API de versiones de GitHub por HTTPS para ver si hay una versión más reciente. |
| Open the release page to download the newer version, or cancel to keep the current version. | Abre la página de la versión para descargar la más reciente, o cancela para conservar la actual. |
| MIT licence | Licencia MIT |
| Opens the licence file on github.com in your browser. | Abre el archivo de la licencia en github.com en tu navegador. |
| Move location | Ubicación de destino |
| Products | Productos |
| Patches | Parches |
| Product details | Detalles del producto |
| Move destination folder | Carpeta de destino |
| Operation progress | Progreso de la operación |
| Scan C:\Windows\Installer again | Volver a analizar C:\Windows\Installer |
| Scanning progress | Progreso del análisis |
| Startup scan progress | Progreso del análisis de inicio |
| Details, unneeded files | Detalles, archivos innecesarios |
| Available for cleanup. | Disponibles para limpiar. |
| Details, registered files | Detalles, archivos registrados |
| Read-only inventory. | Inventario de solo lectura. |
| Sorted by {0}, ascending | Ordenado por {0}, ascendente |
| Sorted by {0}, descending | Ordenado por {0}, descendente |
| Scan results | Resultados del análisis |
| Result details | Detalles del resultado |
| File details | Detalles del archivo |
| Dialog text | Texto del cuadro de diálogo |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | Archivos que no se pudieron procesar |
| Explains this folder, and how to recover a file, in the README | Explica esta carpeta, y cómo recuperar un archivo, en el README |
| Result log preview | Vista previa del registro de resultados |
| Change the display language | Cambiar el idioma de visualización |
| InstallerClean restarts to apply it. | InstallerClean se reiniciará para aplicarlo. |

## File picker

| English | Español |
| --- | --- |
| Choose destination folder for moved files | Elige la carpeta de destino para los archivos movidos |

## Version

| English | Español |
| --- | --- |
| Version {0} | Versión {0} |

## Word forms (singular and plural)

| English | Español |
| --- | --- |
| file | archivo |
| files | archivos |
| error | error |
| errors | errores |
| package | paquete |
| packages | paquetes |
| product | producto |
| products | productos |
| patch | parche |
| patches | parches |

## Sizes and times

| English | Español |
| --- | --- |
| {0:F2} GB | {0:F2} GB |
| {0:F1} MB | {0:F1} MB |
| {0:F1} KB | {0:F1} KB |
| {0} B | {0} B |
| {0:F0}ms | {0:F0}ms |
| {0:F1}s | {0:F1}s |
| less than a second | menos de un segundo |
| {0:F1} seconds | {0:F1} segundos |
