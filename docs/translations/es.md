# InstallerClean in Español (Spanish)

The text of InstallerClean's interface and command-line tool in English on the left, with the Spanish translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Spanish can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.es.resx`](../../src/InstallerClean.Core/Resources/Strings.es.resx), so do not edit it by hand. The Spanish translation itself lives in [`gen-strings-es.mjs`](../../scripts/translations/gen-strings-es.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | Español |
| --- | --- |
| InstallerClean | InstallerClean |
| About | Acerca de |
| Registered files that should not be deleted | Archivos registrados que no deberían eliminarse |
| Unneeded files that are safe to delete | Archivos innecesarios que puedes eliminar sin riesgo |

## Section headings

| English | Español |
| --- | --- |
| PRODUCTS | PRODUCTOS |
| PATCHES | PARCHES |
| PRODUCT DETAILS | DETALLES DEL PRODUCTO |
| BACKUP FOLDER | BACKUP FOLDER |
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
| Check for _updates | _Buscar actualizaciones |
| _Close | _Cerrar |
| _Delete permanently | _Eliminar definitivamente |
| _Done | _Listo |
| Details | Detalles |
| _Buy me a cuppa | _Invítame a un café |
| Leave a _star on GitHub | Deja una e_strella en GitHub |
| Apache 2.0 licence | Licencia Apache 2.0 |
| _Move | _Mover |
| Path to folder if you move rather than delete. | Path to folder if you move rather than delete. |
| Open _release page | Abrir la página de la _versión |
| _Re-scan | _Volver a analizar |
| _Scan again | Analizar de _nuevo |
| Send report | Enviar informe |
| _Send | _Enviar |

## About window

| English | Español |
| --- | --- |
| Guide and FAQ | Guía y preguntas frecuentes |
| Report a problem | Informar de un problema |
| Check for updates automatically | Buscar actualizaciones automáticamente |

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
| Moving unneeded files... | Moving unneeded files... |
| Deleting unneeded files... | Deleting unneeded files... |
| Move cancelled. {0} of {1} {2} processed. | Movimiento cancelado tras procesar {0} de {1} {2}. |
| Delete cancelled. {0} of {1} {2} processed. | Eliminación cancelada tras procesar {0} de {1} {2}. |
| Move failed ({0}). Details in {1}. | Movimiento fallido ({0}). Detalles en {1}. |
| Move failed ({0}). The crash log could not be written. | Movimiento fallido ({0}). No se pudo escribir el archivo crash.log. |
| Delete failed ({0}). Details in {1}. | Eliminación fallida ({0}). Detalles en {1}. |
| Delete failed ({0}). The crash log could not be written. | Eliminación fallida ({0}). No se pudo escribir el archivo crash.log. |
| Access denied. Windows refused the scan. | Acceso denegado. Windows rechazó el análisis. |
| Scan failed: couldn't read the Windows Installer records. | Análisis fallido: no se pudieron leer los registros de Windows Installer. |
| Scan cancelled. | Análisis cancelado. |
| Ready | Listo |
| Scan failed ({0}). Details in {1}. | Análisis fallido ({0}). Detalles en {1}. |
| Scan failed ({0}). The crash log could not be written. | Análisis fallido ({0}). No se pudo escribir el archivo crash.log. |

## Main screen text

| English | Español |
| --- | --- |
| Any unneeded files below are [safe to delete]. | Any unneeded files below are [safe to delete]. |
| They sit in {InstallerFolder}, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | Están en {InstallerFolder}, donde quedaron cuando se desinstaló un programa ({0}), un parche más reciente sustituyó a otro ({1}) o el editor lo retiró ({2}). InstallerClean solo enumera archivos que el propio Windows da por terminados. |
| Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored. | Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored. |
| Nothing scanned yet. | Aún no se ha analizado nada. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Pulsa Volver a analizar para revisar {InstallerFolder} en busca de archivos de instalación que ya no necesita ningún programa. |
| These files can't be cleaned up right now. | Estos archivos no se pueden limpiar ahora mismo. |
| Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. |
| Select a file to view details. | Selecciona un archivo para ver sus detalles. |
| Select a product to view details. | Selecciona un producto para ver sus detalles. |
| No metadata available. | No hay metadatos disponibles. |
| This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. | This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. |
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
| Nothing to clean up in {InstallerFolder} | Nada que limpiar en {InstallerFolder} |
| Scanned {0} {1} in {2} | Análisis de {0} {1} en {2} |
| The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). |
| The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). |
| The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). |
| The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). |
| {0} freed | {0} liberados |
| {0} moved | {0} movidos |
| Nothing was moved | No se movió nada |
| Nothing was deleted | No se eliminó nada |
| {0} of {1} could not be moved. | No se pudo mover {0} archivo de {1}. |
| {0} of {1} could not be moved. | No se pudieron mover {0} archivos de {1}. |
| {0} of {1} could not be deleted. | No se pudo eliminar {0} archivo de {1}. |
| {0} of {1} could not be deleted. | No se pudieron eliminar {0} archivos de {1}. |
| {0} {1} moved to: {2} | {0} {1} en: {2} |
| {0} {1} moved to: {2} | {0} {1} en: {2} |
| {0} {1} kept in place, because a program went back to needing what the scan flagged. | {0} {1} conservados en su sitio: un programa ha vuelto a necesitarlos después del análisis. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read when the check was repeated. | {0} {1} conservados en su sitio: los registros de Windows Installer no se han podido leer por completo al repetir la comprobación. |
| Moved {0} of {1} {2} before you cancelled. | Cancelaste tras mover {0} de {1} {2}. |
| Permanently deleted {0} of {1} {2} before you cancelled. | Cancelaste tras eliminar definitivamente {0} de {1} {2}. |
| {0} {1} permanently deleted | {0} {1} permanently deleted |
| {0} {1} permanently deleted | {0} {1} permanently deleted |
| Glad to help. There's a tip jar if you're feeling kind. | Me alegro de haber ayudado. Aquí tienes el bote de propinas, si te nace del corazón. |

## Summaries and counts

| English | Español |
| --- | --- |
| {0} file still needed | {0} archivo aún necesario |
| {0} files still needed | {0} archivos aún necesarios |
| {0} unneeded file to clean up | {0} archivo innecesario para limpiar |
| {0} unneeded files to clean up | {0} archivos innecesarios para limpiar |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | Falta {0} archivo registrado (no lo ha eliminado InstallerClean). Por ahora sin problemas, pero en el futuro una reparación, actualización o desinstalación de ese programa podría fallar. Abre Detalles para saber qué hacer. |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | Faltan {0} archivos registrados (no los ha eliminado InstallerClean). Por ahora sin problemas, pero en el futuro una reparación, actualización o desinstalación de esos programas podría fallar. Abre Detalles para saber qué hacer. |
| Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. | Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. |
| Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. | Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. |
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
| This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. | This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. |
| Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. | Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. |

## Error messages

| English | Español |
| --- | --- |
| Access denied | Acceso denegado |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows le negó el acceso a InstallerClean, así que se detuvo. No se ha eliminado nada.<br><br>InstallerClean ya se estaba ejecutando como administrador, así que volver a iniciarlo de esa forma no servirá de nada. Windows no dice nada más sobre qué denegó el acceso, así que no hay nada concreto que puedas probar. |
| Couldn't read the Windows Installer records | No se pudieron leer los registros de Windows Installer |
| Scan failed | Análisis fallido |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed. | Los registros de Windows Installer llegaron completamente vacíos: ni un solo programa instalado ni una sola actualización reclama un archivo de instalación en caché. Eso no ocurre en un equipo que funciona (incluso una instalación nueva de Windows tiene alguno), así que o los registros están dañados o no se pudieron leer, y un análisis que se creyera esta respuesta marcaría por error como huérfano cada archivo de {InstallerFolder}. InstallerClean se detuvo en lugar de eso. No se ha eliminado nada. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer no dejó que InstallerClean enumerara lo que hay instalado. InstallerClean ya se estaba ejecutando como administrador, así que volver a ejecutarlo como administrador no cambiará nada. Sin esa lista no hay forma segura de saber qué archivos en caché siguen haciendo falta, así que InstallerClean se detuvo. No se ha eliminado nada. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer no pudo darle a InstallerClean una lista legible de los programas instalados: {0} entradas seguidas llegaron ilegibles (último código de error {1}). En lugar de trabajar con una lista leída a medias, InstallerClean se detuvo. No se ha eliminado nada. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer nunca señaló el final de la lista de programas instalados: InstallerClean se rindió tras {0} entradas (último código de error {1}). De una lista sin final no hay que fiarse, así que InstallerClean se detuvo. No se ha eliminado nada. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer nunca señaló el final de la lista de parches de un programa: InstallerClean se rindió tras {0} entradas (último código de error {1}). De una lista sin final no hay que fiarse, así que InstallerClean se detuvo. No se ha eliminado nada. |
| InstallerClean couldn't square this scan with the Windows Installer records: nearly every file Windows still lists as needed is missing from {InstallerFolder}, while what is actually in the folder matches almost nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. | InstallerClean couldn't square this scan with the Windows Installer records: nearly every file Windows still lists as needed is missing from {InstallerFolder}, while what is actually in the folder matches almost nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean no pudo leer lo suficiente de los registros de Windows Installer para estar seguro de qué sigue haciendo falta: la lista de programas instalados llegó incompleta, y leer esos mismos registros directamente desde el registro de Windows también dio errores. Un archivo podría parecer huérfano solo porque el registro que lo nombra era uno de los ilegibles, así que InstallerClean se detuvo. No se ha eliminado nada. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. |
| Nothing was deleted | No se eliminó nada |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. |
| Invalid destination | Destino no válido |
| Could not write to destination | No se pudo escribir en el destino |
| Move failed | Movimiento fallido |
| Delete failed | Eliminación fallida |
| Setting not saved | Ajuste no guardado |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | No se pudo guardar el cambio. La próxima vez que se inicie, InstallerClean volverá al ajuste anterior. |
| The destination cannot be inside the Windows Installer folder. | El destino no puede estar dentro de la carpeta de Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. |
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
| This file is not directly inside the Windows Installer folder; refused for safety. | Este archivo no está directamente dentro de la carpeta de Windows Installer; rechazado por seguridad. |
| Windows refused access to this file; it was left in place. | Windows denegó el acceso a este archivo; se dejó donde estaba. |
| Windows refused access to these files; they were left in place. | Windows denegó el acceso a estos archivos; se dejaron donde estaban. |
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. |
| Windows reported a file error; the file was left in place. | Windows informó de un error de archivo; el archivo se dejó donde estaba. |
| Windows reported file errors; these files were left in place. | Windows informó de errores de archivo; estos archivos se dejaron donde estaban. |
| Something went wrong with this file; it was left in place. | Algo salió mal con este archivo; se dejó donde estaba. |
| Something went wrong with these files; they were left in place. | Algo salió mal con estos archivos; se dejaron donde estaban. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Se rechaza mover archivos a la carpeta de Windows Installer (destino: {0}). |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} |
| The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. |
| Cannot write to {0}. | No se puede escribir en {0}. |
| Could not find a unique filename for '{0}' after 10,000 attempts. | No se pudo encontrar un nombre de archivo único para '{0}' tras 10.000 intentos. |

## Update check

| English | Español |
| --- | --- |
| Check for updates | Buscar actualizaciones |
| Checking... | Buscando... |
| Up to date. | Estás al día. |
| Version {0} is available. | Está disponible la versión {0}. |
| Update available | Actualización disponible |
| You're running version {0}.<br>Version {1} is available. | Estás usando la versión {0}.<br>Está disponible la versión {1}. |
| Couldn't reach GitHub. Check your internet connection and try again. | No se pudo conectar con GitHub. Comprueba tu conexión a internet y vuelve a intentarlo. |
| GitHub returned an error response. Try again in a few minutes. | GitHub devolvió una respuesta de error. Vuelve a intentarlo en unos minutos. |
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
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> |

## Tooltips (hover text)

| English | Español |
| --- | --- |
| It's thirsty work! | ¡Esto da sed! |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Cancelación solicitada. InstallerClean está esperando a que el paso en curso llegue a un punto en el que pueda detenerse. Puede tardar unos segundos durante operaciones intensas de entrada/salida o una llamada a la base de datos MSI. |
| Close | Cerrar |
| A star helps other people find it. | Una estrella ayuda a otras personas a encontrar InstallerClean. |
| Minimise | Minimizar |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | Tú decides, pero se agradece. Envía un resumen anónimo que solo sirve para que yo sepa si funciona y cuánto espacio libera la gente. La pantalla siguiente te muestra lo que se enviará antes de confirmar. |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | Tú decides, pero se agradece. Envía un resumen anónimo que solo sirve para que yo sepa si funciona. La pantalla siguiente te muestra lo que se enviará antes de confirmar. |
| Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. | Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. |
| Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. | Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. | Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. |
| Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. | Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Nombre del firmante del certificado Authenticode incorporado. La cadena no está verificada. |
| Change language. The program will restart. | Cambia el idioma. El programa se reiniciará. |

## Screen reader labels

| English | Español |
| --- | --- |
| Donate | Donar |
| Buy me a cuppa | Invítame a un café |
| Cancel operation | Cancelar la operación |
| Cancel scan | Cancelar el análisis |
| Cancel startup scan | Cancelar el análisis de inicio |
| Close | Cerrar |
| Close window | Cerrar la ventana |
| Close result and return to main window | Cerrar el resultado y volver a la ventana principal |
| Leave a star on github | Deja una estrella en github |
| Minimise | Minimizar |
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Delete permanently removes the unneeded files. Cancel closes without deleting. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Mover coloca los archivos innecesarios en la carpeta de destino elegida. Cancelar los deja donde están. |
| Say thanks | Dar las gracias |
| Send posts the report shown to No Faff. Cancel sends nothing. | Enviar transmite a No Faff el informe mostrado. Cancelar no envía nada. |
| Check for updates | Buscar actualizaciones |
| Checks github's releases page for a newer version. | Consulta la página de versiones de github en busca de una versión más reciente. |
| Opens the readme on github in your browser. | Abre el readme en github en tu navegador. |
| Opens the issue tracker on github.com in your browser. | Abre el rastreador de problemas (Issues) en github.com en tu navegador. |
| If ticked, InstallerClean checks github for a newer version when you run it. | Si está marcada, InstallerClean busca en github una versión más reciente cuando lo ejecutas. |
| Open the release page to download the newer version, or cancel to keep the current version. | Abre la página de la versión para descargar la más reciente, o cancela para conservar la actual. |
| Opens the licence file on github.com in your browser. | Abre el archivo de la licencia en github.com en tu navegador. |
| Backup folder | Backup folder |
| Products | Productos |
| Patches | Parches |
| Product details | Detalles del producto |
| Backup folder | Backup folder |
| Operation progress | Progreso de la operación |
| Scan {InstallerFolder} again | Volver a analizar {InstallerFolder} |
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
| Product details | Product details |
| Dialog text | Texto del cuadro de diálogo |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | Archivos que no se pudieron procesar |
| Explains this folder, and how to recover a file, in the README | Explica esta carpeta, y cómo recuperar un archivo, en el README |
| Report preview | Vista previa del informe |
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
| Error: unknown argument '{0}' | Error: unknown argument '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Error: argumento extra inesperado '{0}'. Si la carpeta de destino tiene un espacio, escribe toda la ruta entre comillas: /m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. |
| Cancelling... | Cancelando... |
| Cancelled. | Operación cancelada. |
| Error: unexpected failure ({0}). Details written to {1}. | Error: unexpected failure ({0}). Details written to {1}. |
| Error: unexpected failure ({0}). The crash log could not be written. | Error: unexpected failure ({0}). The crash log could not be written. |
| Scanning {InstallerFolder}... | Analizando {InstallerFolder}... |
| Found {0} unneeded {1} to clean up ({2}). | Found {0} unneeded {1} to clean up ({2}). |
| Found no unneeded files. | Found no unneeded files. |
| {0} registered file is missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, the same version, usually restores it. | {0} registered file is missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, the same version, usually restores it. |
| {0} registered files are missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, the same version, usually restores them. | {0} registered files are missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, the same version, usually restores them. |
| Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. | Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. |
| Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. | Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. |
| Deleting {0} unneeded {1}... | Deleting {0} unneeded {1}... |
| Permanently deleted {0} unneeded {1}. | Permanently deleted {0} unneeded {1}. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Error: no se ha especificado un destino para mover. Usa /m RUTA. (Una ubicación predeterminada configurada en la GUI se guarda por usuario y no se aplica a las ejecuciones programadas ni a las de cuenta de servicio.) |
| Error: destination cannot be inside the Windows Installer folder. | Error: el destino no puede estar dentro de la carpeta de Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Error: el destino debe ser una ruta completa. Recibido: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. |
| Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. |
| Moving {0} unneeded {1} to {2}... | Moving {0} unneeded {1} to {2}... |
| Moved {0} unneeded {1}. | Moved {0} unneeded {1}. |
| The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then run the command again. | The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then run the command again. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Otro proceso de InstallerClean mantiene el bloqueo de instancia única (la GUI u otra ejecución de la CLI). Código de salida 75 (transitorio); es seguro reintentar más tarde. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Nota: error al escribir en el registro de eventos. Comprueba los permisos del registro Aplicación o las directivas de grupo. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - limpiar {InstallerFolder} |
| Removes cached .msi and .msp files that no installed program still needs. | Removes cached .msi and .msp files that no installed program still needs. |
| Needs an elevated (administrator) prompt; Windows will not start it. | Needs an elevated (administrator) prompt; Windows will not start it. |
| Usage: | Uso: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     Muestra esta ayuda (acepta también /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  Muestra la versión (acepta también -v) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         Scan only - list unneeded files |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         Delete unneeded files permanently |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         Move to the saved backup folder |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m RUTA    Mueve a la ruta especificada |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. |
| Exit codes: | Códigos de salida: |
|   0   success: the run finished with nothing left to do |   0   success: the run finished with nothing left to do |
|   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   partial: some processed, some not (a failure or a Ctrl+C part way) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  transitorio: algo temporal bloqueó la ejecución (ver el mensaje) |
|   130 cancelled (Ctrl+C) |   130 cancelado (Ctrl+C) |
