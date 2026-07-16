# InstallerClean in Español (Spanish)

The text of InstallerClean's interface and command-line tool in English on the left, with the Spanish translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Spanish can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. The translation file itself is [`Strings.es.resx`](../../src/InstallerClean.Core/Resources/Strings.es.resx). This page is generated from it by `scripts/gen-translation-table.mjs`, so do not edit it by hand.

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
| Send report | Enviar informe |
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
| Found {0} {1} you can safely delete. | Se encontraron {0} {1} que puedes eliminar sin riesgo. |
| Preparing destination folder... | Preparando la carpeta de destino... |
| Moving {0} {1}... | Moviendo {0} {1}... |
| Deleting {0} {1}... | Eliminando {0} {1}... |
| Move cancelled. {0} of {1} {2} processed. | Movimiento cancelado tras procesar {0} de {1} {2}. |
| Delete cancelled. {0} of {1} {2} processed. | Eliminación cancelada tras procesar {0} de {1} {2}. |
| Move failed ({0}). Details in {1}. | Movimiento fallido ({0}). Detalles en {1}. |
| Move failed ({0}). The crash log could not be written. | Movimiento fallido ({0}). No se pudo escribir el archivo crash.log. |
| Delete failed ({0}). Details in {1}. | Eliminación fallida ({0}). Detalles en {1}. |
| Delete failed ({0}). The crash log could not be written. | Eliminación fallida ({0}). No se pudo escribir el archivo crash.log. |
| Access denied. Windows refused the scan. | Acceso denegado. Windows rechazó el análisis. |
| Scan failed: installer database unavailable. | Análisis fallido: base de datos del instalador no disponible. |
| Scan cancelled. | Análisis cancelado. |
| Ready | Listo |
| Scan failed ({0}). Details in {1}. | Análisis fallido ({0}). Detalles en {1}. |
| Scan failed ({0}). The crash log could not be written. | Análisis fallido ({0}). No se pudo escribir el archivo crash.log. |

## Main screen text

| English | Español |
| --- | --- |
| The unneeded files below are safe to delete. | Los archivos innecesarios de abajo se pueden eliminar sin riesgo. |
| They sit in C:\Windows\Installer, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | Están en C:\Windows\Installer, donde quedaron cuando se desinstaló un programa ({0}), un parche más reciente sustituyó a otro ({1}) o el fabricante lo retiró ({2}). InstallerClean solo enumera archivos que el propio Windows da por terminados. |
| Delete them to the Recycle Bin, or use Move instead if you'd rather keep a copy. | Elimínalos y se enviarán a la Papelera de reciclaje, o usa Mover en su lugar si prefieres conservar una copia. |
| Nothing scanned yet. | Aún no se ha analizado nada. |
| Press Re-scan to look through C:\Windows\Installer for installer files that no program still needs. | Pulsa Volver a analizar para revisar C:\Windows\Installer en busca de archivos de instalación que ya no necesita ningún programa. |
| These files can't be cleaned up right now. | Estos archivos no se pueden limpiar ahora mismo. |
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
| Copy them back if anything breaks ([it won't!]). | Vuelve a copiarlos a su sitio si algo falla ([¡no fallará!]). |
| Until then, you can restore them if anything breaks ([it won't!]). | Hasta entonces, puedes restaurarlos si algo falla ([¡no fallará!]). |
| Empty it to actually reclaim the space. | Vacíala para liberar el espacio de verdad. |
| {0} freed | {0} liberados |
| {0} cleaned up | {0} limpiados |
| {0} moved | {0} movidos |
| {0} moved, some files could not be processed | {0} movidos, no se pudieron procesar algunos archivos |
| {0} freed, some files could not be processed | {0} liberados, no se pudieron procesar algunos archivos |
| {0} cleaned up, some files could not be processed | {0} limpiados, no se pudieron procesar algunos archivos |
| {0} {1} moved to: {2} | {0} {1} en: {2} |
| {0} {1} moved to: {2} | {0} {1} en: {2} |
| {0} {1} moved to: {2}. {3} {4} | {0} {1} en: {2}. {3} {4} |
| {0} {1} moved to: {2}. {3} {4} | {0} {1} en: {2}. {3} {4} |
| {0} {1} moved to the Recycle Bin | {0} {1} en la Papelera de reciclaje |
| {0} {1} moved to the Recycle Bin | {0} {1} en la Papelera de reciclaje |
| {0} {1} moved to the Recycle Bin. {2} {3} | {0} {1} en la Papelera de reciclaje. {2} {3} |
| {0} {1} moved to the Recycle Bin. {2} {3} | {0} {1} en la Papelera de reciclaje. {2} {3} |
| {0} {1} kept in place, because a program started needing them again after the scan. | {0} {1} conservados en su sitio: un programa ha vuelto a necesitarlos después del análisis. |
| Moved {0} of {1} {2} before you cancelled. | Cancelaste tras mover {0} de {1} {2}. |
| Moved {0} of {1} {2} to the Recycle Bin before you cancelled. | Cancelaste tras mover {0} de {1} {2} a la Papelera de reciclaje. |
| Permanently deleted {0} of {1} {2} before you cancelled. | Cancelaste tras eliminar definitivamente {0} de {1} {2}. |
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
| Windows still lists {0} old patch whose file is already gone from disk. That's harmless, and there's nothing you need to do. | Windows todavía tiene registrado {0} parche antiguo cuyo archivo ya no está en el disco. No supone ningún problema y no tienes que hacer nada. |
| Windows still lists {0} old patches whose files are already gone from disk. That's harmless, and there's nothing you need to do. | Windows todavía tiene registrados {0} parches antiguos cuyos archivos ya no están en el disco. No supone ningún problema y no tienes que hacer nada. |
| {0} of {1} {2} | {0} de {1} {2} |
| {0} orphaned, {1} superseded, {2} obsoleted ({3}) | {0} huérfanos, {1} sustituidos, {2} obsoletos ({3}) |
| {0} registered file that is still needed ({1}) | {0} archivo registrado aún necesario ({1}) |
| {0} registered files that are still needed ({1}) | {0} archivos registrados aún necesarios ({1}) |

## Confirmation dialogs

| English | Español |
| --- | --- |
| Move {0} {1} ({2})? | ¿Mover {0} {1} ({2})? |
| Files will be moved to: | Los archivos se moverán a: |
| Delete {0} {1} ({2})? | ¿Eliminar {0} {1} ({2})? |
| Files will be moved to the Recycle Bin. If you'd like backup copies, use the Move button instead. | Los archivos se moverán a la Papelera de reciclaje. Si quieres copias de seguridad, usa el botón Mover en su lugar. |
| This folder is on the same drive, so the move won't free any space by itself. You'll get the space back when you delete the files from it, or you can pick a folder on another drive instead. | Esta carpeta está en la misma unidad, así que mover los archivos no liberará espacio por sí solo. Recuperarás el espacio cuando elimines los archivos de ahí, o puedes elegir una carpeta en otra unidad. |

## Error messages

| English | Español |
| --- | --- |
| Access denied | Acceso denegado |
| Windows refused InstallerClean access. InstallerClean is already running as administrator, so starting it again that way won't help.<br><br>That leaves two likely causes: security software is holding C:\Windows\Installer, or the folder's permissions have been changed. Pausing the security software and trying again is the quickest one to rule out. | Windows le negó el acceso a InstallerClean. InstallerClean ya se está ejecutando como administrador, así que volver a iniciarlo de esa forma no servirá de nada.<br><br>Quedan dos causas probables: algún software de seguridad está bloqueando C:\Windows\Installer, o se han cambiado los permisos de la carpeta. Pausar el software de seguridad y volver a intentarlo es lo más rápido de descartar. |
| Installer database unavailable | Base de datos del instalador no disponible |
| Scan failed | Análisis fallido |
| The Windows Installer database appears to be empty or inaccessible. This is unusual even on a fresh Windows install and typically means the database is corrupt or a third-party tool has cleared it. Running 'sfc /scannow' from an elevated prompt usually repairs it. | La base de datos de Windows Installer parece estar vacía o inaccesible. Es algo inusual incluso en una instalación de Windows recién hecha y normalmente significa que la base de datos está dañada o que una herramienta de terceros la ha vaciado. Ejecutar 'sfc /scannow' desde un símbolo del sistema con privilegios elevados suele repararla. |
| Windows Installer refused to list the installed products, and InstallerClean is already running as administrator, so running it again won't help. The permissions on the Windows Installer records may have been changed, or security software may be blocking them. Running 'sfc /scannow' from an elevated prompt is worth a try. | Windows Installer se negó a enumerar los productos instalados, e InstallerClean ya se está ejecutando como administrador, así que volver a ejecutarlo no servirá de nada. Puede que se hayan cambiado los permisos de los registros de Windows Installer, o que algún software de seguridad los esté bloqueando. Vale la pena probar a ejecutar 'sfc /scannow' desde un símbolo del sistema con privilegios elevados. |
| Windows Installer refused to list products after {0} consecutive failures (last error code {1}). Try restarting Windows, or run 'sfc /scannow' from an elevated prompt. | Windows Installer se negó a enumerar los productos tras {0} errores consecutivos (último código de error {1}). Prueba a reiniciar Windows, o ejecuta 'sfc /scannow' desde un símbolo del sistema con privilegios elevados. |
| Windows Installer refused to list a product's patches after {0} consecutive failures (last error code {1}). Try restarting Windows, or run 'sfc /scannow' from an elevated prompt. | Windows Installer se negó a enumerar los parches de un producto tras {0} errores consecutivos (último código de error {1}). Prueba a reiniciar Windows, o ejecuta 'sfc /scannow' desde un símbolo del sistema con privilegios elevados. |
| InstallerClean couldn't cross-check this scan against Windows: everything Windows still lists is missing from the cache folder, while the files in the folder match nothing Windows knows about. That points to a problem reading the installer records rather than to files you can safely remove, so nothing has been offered for cleanup. Restarting Windows and scanning again usually clears it. | InstallerClean no pudo contrastar este análisis con Windows: todo lo que Windows sigue teniendo registrado falta en la carpeta de la caché, mientras que los archivos de la carpeta no coinciden con nada que Windows conozca. Eso apunta a un problema al leer los registros de instalación, más que a archivos que puedas eliminar sin riesgo, así que no se ha ofrecido nada para limpiar. Reiniciar Windows y volver a analizar suele solucionarlo. |
| Invalid destination | Destino no válido |
| Could not write to destination | No se pudo escribir en el destino |
| Move failed | Movimiento fallido |
| Delete failed | Eliminación fallida |
| The destination cannot be inside the Windows Installer folder. | El destino no puede estar dentro de la carpeta de Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | El destino {0} se encuentra dentro de una carpeta del sistema de Windows. Elige una ruta fuera de %SystemRoot%, %ProgramFiles% y %ProgramData%. |
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
| This file is not inside the Windows Installer folder; refused for safety. | Este archivo no está dentro de la carpeta de Windows Installer; rechazado por seguridad. |
| Access denied. | Acceso denegado. |
| The operation failed. Try again or restart Windows. | La operación falló. Inténtalo de nuevo o reinicia Windows. |
| Unknown error. | Error desconocido. |
| Couldn't move this file to the Recycle Bin (error {0}). It may be locked, in use or blocked by Windows. Use the Move button instead. | No se pudo mover este archivo a la Papelera de reciclaje (error {0}). Puede estar bloqueado, en uso o impedido por Windows. Usa el botón Mover en su lugar. |
| Windows blocked access to this file, even with administrator rights (error {0}). It is usually an ownership or permissions lock. Use the Move button instead. | Windows bloqueó el acceso a este archivo, incluso con permisos de administrador (error {0}). Suele ser un bloqueo de propiedad o de permisos. Usa el botón Mover en su lugar. |
| This file is open or locked by another program (error {0}). Close that program, or whatever is scanning it, then try again, or use the Move button instead. | Este archivo está abierto o bloqueado por otro programa (error {0}). Cierra ese programa, o lo que sea que lo esté analizando, y vuelve a intentarlo, o usa el botón Mover en su lugar. |
| The file was permanently deleted because it could not be moved to the Recycle Bin. | El archivo se eliminó definitivamente porque no se pudo mover a la Papelera de reciclaje. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Se rechaza mover archivos a la carpeta de Windows Installer (destino: {0}). |
| The Move location needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | La ubicación de destino tiene que ser una ruta completa a una carpeta, que empiece por una letra de unidad o un recurso compartido de red (por ejemplo D:\Backup o \\server\backup). InstallerClean no puede usar esta: {0} |
| The Move location changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | La ubicación de destino cambió mientras se movían los archivos (algo sustituyó o redirigió la carpeta), así que InstallerClean se detuvo en lugar de escribir en el lugar equivocado. Comprueba {0}, luego vuelve a analizar e inténtalo de nuevo. |
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
| InstallerClean couldn't open your browser. The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | InstallerClean no pudo abrir tu navegador. El enlace está en el portapapeles, así que puedes pegarlo tú mismo:<br><br>{0} |
| InstallerClean couldn't open your browser, and couldn't copy the link to your clipboard either. The link is:<br><br>{0} | InstallerClean no pudo abrir tu navegador, y tampoco pudo copiar el enlace al portapapeles. El enlace es:<br><br>{0} |

## Sending the summary

| English | Español |
| --- | --- |
| Sending... | Enviando... |
| Thanks! Report sent. | ¡Gracias! Informe enviado. |
| Sending failed. Try again later. | Envío fallido. Inténtalo de nuevo más tarde. |
| No report to send. | No hay ningún informe que enviar. |
| Send this? | ¿Enviar esto? |
| It goes to nofaff.netlify.app/api/result-log. Nothing identifies you or your machine; it just lets me know InstallerClean's working and [how much space people are freeing]. | Se envía a nofaff.netlify.app/api/result-log. Nada te identifica a ti ni a tu equipo; solo sirve para que yo sepa que InstallerClean funciona y [cuánto espacio libera la gente]. |

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
| Donate | Donar |
| It's thirsty work! | ¡Esto da sed! |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Cancelación solicitada. InstallerClean está esperando a que el paso en curso llegue a un punto en el que pueda detenerse. Puede tardar unos segundos durante operaciones intensas de entrada/salida o una llamada a la base de datos MSI. |
| Close | Cerrar |
| Leave a star on GitHub, report an Issue or post in Discussions. Any feedback welcome. | Deja una estrella en GitHub, informa de un problema (Issue) o escribe en Discussions. Cualquier comentario es bienvenido. |
| or report an Issue or post in Discussions. Any feedback welcome. | o informa de un problema (Issue) o escribe en Discussions. Cualquier comentario es bienvenido. |
| Minimise | Minimizar |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | Tú decides, pero se agradece. Envía un resumen anónimo que solo sirve para que yo sepa si funciona y cuánto espacio libera la gente. La pantalla siguiente te muestra lo que se enviará antes de confirmar. |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | Tú decides, pero se agradece. Envía un resumen anónimo que solo sirve para que yo sepa si funciona. La pantalla siguiente te muestra lo que se enviará antes de confirmar. |
| Move the unneeded files to the Move location. | Mueve los archivos innecesarios a la ubicación de destino. |
| Move the unneeded files to the Move location. Choose one first. | Mueve los archivos innecesarios a la ubicación de destino. Elige una primero. |
| Move the unneeded files to the Recycle Bin. | Mueve los archivos innecesarios a la Papelera de reciclaje. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Nombre del firmante del certificado Authenticode incorporado. La cadena no está verificada. |
| Change language. The program will restart. | Cambia el idioma. El programa se reiniciará. |

## Screen reader labels

| English | Español |
| --- | --- |
| Donate | Donar |
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
| Move all unneeded installer files to the Recycle Bin | Mover todos los archivos de instalación innecesarios a la Papelera de reciclaje |
| Delete moves the unneeded files to the Recycle Bin. Cancel closes without deleting. | Eliminar mueve los archivos innecesarios a la Papelera de reciclaje. Cancelar cierra sin eliminar. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Mover coloca los archivos innecesarios en la carpeta de destino elegida. Cancelar los deja donde están. |
| Choose how to handle the unneeded files: move them somewhere safe, delete them permanently or cancel. | Elige qué hacer con los archivos innecesarios: moverlos a un lugar seguro, eliminarlos definitivamente o cancelar. |
| Move the unneeded files to a folder you choose | Mover los archivos innecesarios a una carpeta que tú elijas |
| Delete the unneeded files permanently because the Recycle Bin is unavailable for this drive | Eliminar definitivamente los archivos innecesarios porque la Papelera de reciclaje no está disponible para esta unidad |
| Posts to nofaff.netlify.app. Counts and labels only. You will see the exact payload before sending. | Se envía a nofaff.netlify.app. Solo recuentos y etiquetas. Verás exactamente lo que se envía antes de enviarlo. |
| Say thanks | Dar las gracias |
| Send posts the report shown to No Faff. Cancel sends nothing. | Enviar transmite a No Faff el informe mostrado. Cancelar no envía nada. |
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
| Change language | Cambiar el idioma |
| The program will restart. | El programa se reiniciará. |

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

## Command-line tool (installerclean-cli)

| English | Español |
| --- | --- |
| Unknown argument: '{0}' | Argumento desconocido: '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Error: argumento extra inesperado '{0}'. Si la carpeta de destino tiene un espacio, escribe toda la ruta entre comillas: /m "D:\My Backup" |
| Cancelling... | Cancelando... |
| Cancelled. | Operación cancelada. |
| Error: {0}. Details written to {1}. | Error: {0}. Detalles guardados en {1}. |
| Error: {0}. The crash log could not be written. | Error: {0}. No se pudo escribir el archivo crash.log. |
| Scanning C:\Windows\Installer... | Analizando C:\Windows\Installer... |
| Found {0} {1} to clean up ({2}). | Encontrados {0} {1} para limpiar ({2}). |
| Nothing to do. | No hay nada que hacer. |
| Deleting {0} {1}... | Eliminando {0} {1}... |
| Deleted {0} {1}. | Eliminados {0} {1}. |
| Error: the Recycle Bin is unavailable for this volume, so nothing was deleted. Use /m to move the files instead, or re-enable the Recycle Bin and run again. | Error: la Papelera de reciclaje no está disponible para este volumen, así que no se eliminó nada. Usa /m para mover los archivos en su lugar, o vuelve a activar la Papelera de reciclaje y ejecuta de nuevo. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Error: no se ha especificado un destino para mover. Usa /m RUTA. (Una ubicación predeterminada configurada en la GUI se guarda por usuario y no se aplica a las ejecuciones programadas ni a las de cuenta de servicio.) |
| Error: destination cannot be inside the Windows Installer folder. | Error: el destino no puede estar dentro de la carpeta de Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Error: el destino debe ser una ruta completa. Recibido: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | Error: el destino {0} se encuentra dentro de una carpeta del sistema de Windows. Elige una ruta fuera de %SystemRoot%, %ProgramFiles% y %ProgramData%. |
| Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are blocked while that runs. Try again once it finishes. | Error: ahora mismo algo está usando Windows Installer, normalmente una actualización de Windows o un programa instalándose en segundo plano. Mover y Eliminar están bloqueados mientras eso ocurre. Vuelve a intentarlo cuando termine. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | Error: hay una transacción anterior de Windows Installer suspendida en este equipo. Reanuda o revierte esa instalación (o reinicia Windows) antes de limpiar la caché. |
| Error: a queued post-reboot file operation targets the Installer cache ({0}). Restart Windows to complete that operation before cleaning. | Error: una operación de archivo en cola para después del reinicio afecta a la caché de instalación ({0}). Reinicia Windows para completar esa operación antes de limpiar. |
| Moving {0} {1} to {2}... | Moviendo {0} {1} a {2}... |
| Moved {0} {1}. | Movidos {0} {1}. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Otro proceso de InstallerClean mantiene el bloqueo de instancia única (la GUI u otra ejecución de la CLI). Código de salida 75 (transitorio); es seguro reintentar más tarde. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Nota: error al escribir en el registro de eventos. Comprueba los permisos del registro Aplicación o las directivas de grupo. |
| InstallerClean - clean up C:\Windows\Installer | InstallerClean - limpiar C:\Windows\Installer |
| Usage: | Uso: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     Muestra esta ayuda (acepta también /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  Muestra la versión (acepta también -v) |
|   installerclean-cli /s         Scan only - list removable files |   installerclean-cli /s         Solo análisis - enumera los archivos innecesarios |
|   installerclean-cli /d         Delete removable files (Recycle Bin) |   installerclean-cli /d         Elimina los archivos innecesarios (Papelera de reciclaje) |
|   installerclean-cli /m         Move to saved default location |   installerclean-cli /m         Mueve a la ubicación predeterminada guardada |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m RUTA    Mueve a la ruta especificada |
| installerclean-cli is a real console process and blocks the prompt | installerclean-cli es un verdadero proceso de consola y bloquea el |
| until it finishes; redirect or pipe its output as you would any | símbolo del sistema hasta que termina; redirige o canaliza su salida como |
| other console exe. The GUI lives in InstallerClean.exe alongside it. | con cualquier otro ejecutable de consola. La GUI está en InstallerClean.exe. |
| The saved default is per-user; scheduled or SYSTEM runs need /m PATH. | La ubicación predeterminada guardada es por usuario; las ejecuciones programadas o con la cuenta SYSTEM necesitan /m RUTA. |
| Exit codes: | Códigos de salida: |
|   0   success: every flagged file was processed |   0   correcto: se procesó cada archivo señalado |
|   1   failure: nothing processed (bad args, scan failed, all files failed) |   1   error: no se procesó nada (argumentos incorrectos, análisis fallido, todos los archivos fallaron) |
|   2   partial: some files processed, some failed |   2   parcial: algunos archivos procesados, otros fallaron |
|   75  transient: a temporary condition blocked the run (see the message) |   75  transitorio: una condición temporal bloqueó la ejecución (consulta el mensaje) |
|   130 cancelled (Ctrl+C) |   130 cancelado (Ctrl+C) |
