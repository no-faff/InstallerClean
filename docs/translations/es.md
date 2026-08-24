# InstallerClean in Español (Spanish)

The text of InstallerClean's interface and command-line tool in English on the left, with the Spanish translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Spanish can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.es.resx`](../../src/InstallerClean.Core/Resources/Strings.es.resx), so do not edit it by hand. The Spanish translation itself lives in [`gen-strings-es.mjs`](../../scripts/translations/gen-strings-es.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | Español |
| --- | --- |
| InstallerClean | InstallerClean |
| About | Acerca de |
| Files left alone | Archivos que se han dejado en paz |
| Unneeded files that are safe to delete | Archivos innecesarios que puedes eliminar sin riesgo |

## Section headings

| English | Español |
| --- | --- |
| PATCHES | PARCHES |
| PRODUCT DETAILS | DETALLES DEL PRODUCTO |
| BACKUP FOLDER | CARPETA DE DESTINO |
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
| Path to folder if you move rather than delete. | Ruta a la carpeta si mueves en lugar de eliminar. |
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
| Moving unneeded files... | Moviendo archivos innecesarios... |
| Deleting unneeded files... | Eliminando archivos innecesarios... |
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
| Any unneeded files below are [safe to delete]. | Cualquier archivo innecesario de los de abajo se puede [eliminar sin riesgo]. |
| They sit in {InstallerFolder}. InstallerClean asks Windows about every installed program: a file is listed when no program claims it ({0}), or when a newer patch has replaced it and no program could roll back to it ({1}). | Están en {InstallerFolder}. InstallerClean pregunta a Windows por cada programa instalado: un archivo aparece en la lista cuando ningún programa lo reclama ({0}), o cuando un parche más nuevo lo ha sustituido y ningún programa podría volver atrás hasta él ({1}). |
| Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update, repair and uninstall as normal. Putting them back into {InstallerFolder} restores everything. Or delete them permanently now. | Muévelos a una carpeta de destino que elijas y elimina esa carpeta cuando compruebes que tus programas se siguen actualizando, reparando y desinstalando con normalidad. Devolverlos a {InstallerFolder} lo restaura todo. O elimínalos definitivamente ahora. |
| Nothing scanned yet. | Aún no se ha analizado nada. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Pulsa Volver a analizar para revisar {InstallerFolder} en busca de archivos de instalación que ya no necesita ningún programa. |
| These files can't be cleaned up right now. | Estos archivos no se pueden limpiar ahora mismo. |
| Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Algo está usando Windows Installer en este momento, como una actualización de Windows o un programa instalándose en segundo plano. Mover y Eliminar están en pausa mientras eso ocurre, así que InstallerClean no tocará {InstallerFolder} mientras cambia. Cuando termine, vuelve a analizar y estarán de nuevo disponibles. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | En este equipo hay una transacción anterior de Windows Installer en suspenso. Reanuda o deshaz esa instalación (o reinicia Windows) antes de limpiar {InstallerFolder}. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows tiene en cola para el próximo reinicio un cambio de nombre de archivo que afecta a {InstallerFolder}. Reinicia Windows antes de limpiar. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer tiene algo en curso, así que Mover y Eliminar están en pausa. InstallerClean no tocará {InstallerFolder} mientras cambia. Cuando termine, vuelve a analizar y estarán de nuevo disponibles. |
| Select a file to view details. | Selecciona un archivo para ver sus detalles. |
| Select a product to view details. | Selecciona un producto para ver sus detalles. |
| No metadata available. | No hay metadatos disponibles. |
| This installer file is missing. It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it, and its own last resort is reinstalling the program. | Este archivo de instalación falta. Ahora no causa ningún problema, y no lo hará hasta el día en que intentes reparar, actualizar o desinstalar el programa al que pertenece. Ese paso puede fallar entonces, porque Windows busca este archivo y no está.<br><br>Para intentar arreglarlo, descarga el instalador de ese programa de su fabricante y ejecútalo sobre la copia que ya tienes (no desinstales primero: desinstalar es en sí mismo un paso que necesita este archivo). Usa la versión que tienes instalada si puedes conseguirla, ya que Windows puede rechazar otra distinta. Esto debería restaurar el archivo y dejar tus ajustes intactos, pero Microsoft no lo garantiza, y su propio último recurso es reinstalar el programa. |
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
| Nothing removed | No se quitó nada |
| Nothing to clean up in {InstallerFolder} | Nada que limpiar en {InstallerFolder} |
| Scanned {0} {1} in {2} | Análisis de {0} {1} en {2} |
| Nothing offered on this PC | No se ofreció nada en este PC |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({1}) it might otherwise have offered. | InstallerClean no pudo determinar con certeza qué archivos almacenados en caché pertenecen a los programas instalados aquí, así que ha retenido el único archivo ({1}) que podría haber ofrecido. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back all {0} files ({1}) it might otherwise have offered. | InstallerClean no pudo determinar con certeza qué archivos almacenados en caché pertenecen a los programas instalados aquí, así que ha retenido los {0} archivos ({1}) que podría haber ofrecido. |
| The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | El archivo de esa carpeta se puede [quitar sin riesgo], así que elimina la carpeta cuando quieras. Hasta entonces, puedes devolverlo a {InstallerFolder} si algún programa resulta necesitarlo (extremadamente improbable). |
| The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | Los archivos de esa carpeta se pueden [quitar sin riesgo], así que elimínala cuando quieras. Hasta entonces, puedes devolverlos a {InstallerFolder} si algún programa resulta necesitar alguno (extremadamente improbable). |
| The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | El archivo de esa carpeta se puede [quitar sin riesgo], así que elimina la carpeta o muévela a otra unidad cuando quieras recuperar el espacio de verdad. Hasta entonces, puedes devolverlo a {InstallerFolder} si algún programa resulta necesitarlo (extremadamente improbable). |
| The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | Los archivos de esa carpeta se pueden [quitar sin riesgo], así que elimínala o muévela a otra unidad cuando quieras recuperar el espacio de verdad. Hasta entonces, puedes devolverlos a {InstallerFolder} si algún programa resulta necesitar alguno (extremadamente improbable). |
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
| {0} {1} kept in place, because the records now claim what the scan flagged. | {0} {1} conservados en su sitio, porque los registros ahora reclaman lo que el análisis había marcado. |
| {0} {1} kept in place, because the Windows Installer records had changed by the final check. | {0} {1} conservados en su sitio, porque los registros de Windows Installer habían cambiado para la comprobación final. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read in the final check. | {0} {1} conservados en su sitio, porque los registros de Windows Installer no se pudieron leer por completo en la comprobación final. |
| {0} {1} kept in place, because by the final check InstallerClean could not be certain which cached files belong to the programs installed here. | {0} {1} conservados en su sitio, porque para la comprobación final InstallerClean no pudo determinar con certeza qué archivos almacenados en caché pertenecen a los programas instalados aquí. |
| {0} {1} kept in place, because Windows has a record of the program named inside. | {0} {1} conservados en su sitio, porque Windows tiene un registro del programa que se nombra dentro. |
| {0} {1} kept in place, because InstallerClean couldn't find a program named inside. | {0} {1} conservados en su sitio, porque InstallerClean no encontró ningún programa nombrado dentro. |
| Moved {0} of {1} {2} before you cancelled. | Cancelaste tras mover {0} de {1} {2}. |
| Permanently deleted {0} of {1} {2} before you cancelled. | Cancelaste tras eliminar definitivamente {0} de {1} {2}. |
| {0} {1} permanently deleted | {0} {1} eliminado definitivamente |
| {0} {1} permanently deleted | {0} {1} eliminados definitivamente |
| Glad to help. There's a tip jar if you're feeling kind. | Me alegro de haber ayudado. Aquí tienes el bote de propinas, si te nace del corazón. |

## Summaries and counts

| English | Español |
| --- | --- |
| {0} file left alone | {0} archivo dejado en paz |
| {0} files left alone | {0} archivos dejados en paz |
| {0} unneeded file to clean up | {0} archivo innecesario para limpiar |
| {0} unneeded files to clean up | {0} archivos innecesarios para limpiar |
| Windows has a record for {0} file that isn't in {InstallerFolder}: {1}. It causes no trouble day to day, but a repair, update or uninstall can fail on it. Open Details for what to do. | Windows tiene un registro de {0} archivo que no está en {InstallerFolder}: {1}. En el día a día no causa problemas, pero una reparación, una actualización o una desinstalación pueden fallar por él. Abre Detalles para saber qué hacer. |
| Windows has records for {0} files that aren't in {InstallerFolder}: {1}. They cause no trouble day to day, but a repair, update or uninstall can fail on them. Open Details for what to do. | Windows tiene registros de {0} archivos que no están en {InstallerFolder}: {1}. En el día a día no causan problemas, pero una reparación, una actualización o una desinstalación pueden fallar por ellos. Abre Detalles para saber qué hacer. |
| {0} other program | {0} programa más |
| {0} other programs | {0} programas más |
| {0} file with no program named in the records | {0} archivo sin ningún programa nombrado en los registros |
| {0} files with no program named in the records | {0} archivos sin ningún programa nombrado en los registros |
| On this PC InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file rather than listing it. | En este PC InstallerClean no pudo determinar con certeza qué archivos almacenados en caché pertenecen a los programas instalados aquí, así que ha retenido el único archivo en lugar de mostrarlo en la lista. |
| On this PC InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} rather than listing them. | En este PC InstallerClean no pudo determinar con certeza qué archivos almacenados en caché pertenecen a los programas instalados aquí, así que ha retenido {0} {1} en lugar de mostrarlos en la lista. |
| InstallerClean couldn't match up everything in the Windows records, so it didn't read all of them. The unneeded files above are unaffected, but what it says about files missing from {InstallerFolder} may be short of the full picture. Re-scan to try again. | InstallerClean no pudo hacer coincidir todo lo que hay en los registros de Windows, así que no los leyó todos. Los archivos innecesarios de arriba no se ven afectados, pero lo que dice sobre archivos que faltan en {InstallerFolder} puede quedarse corto. Vuelve a analizar para intentarlo de nuevo. |
| {0} of {1} {2} | {0} de {1} {2} |
| {0} unneeded {1} ({2}) | {0} {1} para limpiar ({2}) |
| {0} file left alone ({1}) | {0} archivo dejado en paz ({1}) |
| {0} files left alone ({1}) | {0} archivos dejados en paz ({1}) |

## Confirmation dialogs

| English | Español |
| --- | --- |
| Move {0} {1} ({2})? | ¿Mover {0} {1} ({2})? |
| Move to: | Mover a: |
| Delete {0} {1} ({2})? | ¿Eliminar {0} {1} ({2})? |
| This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. | Este archivo se eliminará definitivamente. Se puede [eliminar sin riesgo], pero si quieres una copia, usa el botón Mover en su lugar. |
| Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. | Los archivos se eliminarán definitivamente. Se pueden [eliminar sin riesgo], pero si quieres una copia, usa el botón Mover en su lugar. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | Esa carpeta está en la misma unidad, así que el espacio no volverá hasta que la elimines. Elige una carpeta en otra unidad si quieres el espacio de inmediato. |

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
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean no pudo hacer coincidir los registros de Windows Installer con el contenido de {InstallerFolder}. Casi nada de lo que señalan los registros está realmente ahí, y casi nada de lo que hay ahí lo nombra ningún registro, así que no se pudo demostrar que ningún archivo fuera innecesario. No se ha ofrecido nada y no se ha quitado nada. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean no pudo hacer coincidir los registros de Windows Installer con el contenido de {InstallerFolder}. La carpeta tiene archivos, pero ni un solo registro señala nada de lo que hay ahí, así que no se pudo demostrar que ningún archivo fuera innecesario. No se ha ofrecido nada y no se ha quitado nada. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean no pudo leer lo suficiente de los registros de Windows Installer para estar seguro de qué sigue haciendo falta: la lista de programas instalados llegó incompleta, y leer esos mismos registros directamente desde el registro de Windows también dio errores. Un archivo podría parecer huérfano solo porque el registro que lo nombra era uno de los ilegibles, así que InstallerClean se detuvo. No se ha eliminado nada. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean no consiguió que Windows resolviera la ruta real de {InstallerFolder}, así que no se pudo demostrar que ningún archivo estuviera dentro y no se ofreció ninguno para limpiar. Este análisis no encontró nada porque esa comprobación falló, no porque la carpeta esté limpia. No se ha quitado nada. |
| Nothing was deleted | No se eliminó nada |
| Nothing was moved | No se movió nada |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | InstallerClean no pudo tomar el bloqueo que usa Windows Installer para impedir que dos programas cambien el software instalado a la vez, así que no pudo descartar que un archivo pasara a ser necesario a mitad de camino, y no se ha eliminado nada. Inténtalo de nuevo, y reinicia Windows si sigue ocurriendo. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. Try again, and restart Windows if it keeps happening. | InstallerClean no pudo tomar el bloqueo que usa Windows Installer para impedir que dos programas cambien el software instalado a la vez, así que no pudo descartar que un archivo pasara a ser necesario a mitad de camino, y no se ha movido nada. Inténtalo de nuevo, y reinicia Windows si sigue ocurriendo. |
| Invalid destination | Destino no válido |
| Could not write to destination | No se pudo escribir en el destino |
| Move failed | Movimiento fallido |
| Delete failed | Eliminación fallida |
| Setting not saved | Ajuste no guardado |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | No se pudo guardar el cambio. La próxima vez que se inicie, InstallerClean volverá al ajuste anterior. |
| The destination cannot be inside the Windows Installer folder. | El destino no puede estar dentro de la carpeta de Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | El destino {0} se resuelve dentro de una carpeta del sistema de Windows. Elige una ruta fuera de %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% y %ProgramData%. |
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
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | Este archivo está abierto o bloqueado por otro programa, así que ahora mismo nada puede quitarlo. Se ha dejado en su sitio; inténtalo más tarde. |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | Estos archivos están abiertos o bloqueados por otro programa, así que ahora mismo nada puede quitarlos. Se han dejado en su sitio; inténtalo más tarde. |
| Windows reported a file error; the file was left in place. | Windows informó de un error de archivo; el archivo se dejó donde estaba. |
| Windows reported file errors; these files were left in place. | Windows informó de errores de archivo; estos archivos se dejaron donde estaban. |
| Something went wrong with this file; it was left in place. | Algo salió mal con este archivo; se dejó donde estaba. |
| Something went wrong with these files; they were left in place. | Algo salió mal con estos archivos; se dejaron donde estaban. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Se rechaza mover archivos a la carpeta de Windows Installer (destino: {0}). |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | La carpeta de destino tiene que ser una ruta completa a una carpeta, empezando por una letra de unidad o un recurso compartido de red (por ejemplo D:\Backup, o \\servidor\backup). InstallerClean no puede usar esta: {0} |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | InstallerClean ya no pudo confirmar la carpeta de destino, así que se detuvo en lugar de escribir en el sitio equivocado. Comprueba {0}, luego Volver a analizar e inténtalo de nuevo. |
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
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log recoge las excepciones no controladas de InstallerClean.<br># Con permisos elevados, los mensajes de excepción del framework<br># pueden incluir rutas de archivo de la sesión en curso (incluidos<br># perfiles de otros usuarios enumerados por las consultas de Windows<br># Installer). Los mensajes de fallo de red de la comprobación de<br># actualizaciones o del envío del registro de resultados pueden<br># incluir la URL de destino y la IP o el proxy resueltos. Las<br># entradas sobre registros ilegibles de Windows Installer pueden<br># incluir un SID de cuenta de Windows (S-1-5-21-...) y los códigos<br># de producto del software instalado.<br># Quita los tres tipos de dato antes de adjuntar este archivo a un<br># informe de error público.<br> |

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
| Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. | Mueve los archivos innecesarios a la carpeta de destino. Elimina esa carpeta cuando estés convencido de que nada los necesita. |
| Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. | Mueve los archivos innecesarios a una carpeta de destino. La elegirás a continuación. Elimina esa carpeta cuando estés convencido de que nada los necesita. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. | Mueve los archivos innecesarios a la carpeta de destino. Está en la misma unidad, así que no recuperarás el espacio hasta que elimines esa carpeta o la muevas a otra unidad. Puedes hacerlo cuando estés convencido de que nada los necesita. |
| Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. | Elimina definitivamente los archivos innecesarios. Se pueden quitar sin riesgo, y recuperarás el espacio de inmediato. |
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
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Eliminar definitivamente quita los archivos innecesarios. Cancelar cierra sin eliminar nada. |
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
| Backup folder | Carpeta de destino |
| Patches | Parches |
| Product details | Detalles del producto |
| Backup folder | Carpeta de destino |
| Operation progress | Progreso de la operación |
| Scan {InstallerFolder} again | Volver a analizar {InstallerFolder} |
| Scanning progress | Progreso del análisis |
| Startup scan progress | Progreso del análisis de inicio |
| Details, unneeded files | Detalles, archivos innecesarios |
| Available for cleanup. | Disponibles para limpiar. |
| Details, files left alone | Detalles, archivos que se han dejado en paz |
| Read-only inventory. | Inventario de solo lectura. |
| Sorted by {0}, ascending | Ordenado por {0}, ascendente |
| Sorted by {0}, descending | Ordenado por {0}, descendente |
| Scan results | Resultados del análisis |
| Result details | Detalles del resultado |
| File details | Detalles del archivo |
| Product details | Detalles del producto |
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
| ,  | ,  |
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
| Error: unknown argument '{0}' | Error: argumento desconocido '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Error: argumento extra inesperado '{0}'. Si la carpeta de destino tiene un espacio, escribe toda la ruta entre comillas: /m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | Error: argumento extra inesperado '{0}'. /s y /d no admiten más argumentos, y solo se puede usar un modificador por ejecución. |
| Cancelling... | Cancelando... |
| Cancelled. | Operación cancelada. |
| Error: unexpected failure ({0}). Details written to {1}. | Error: fallo inesperado ({0}). Detalles escritos en {1}. |
| Error: unexpected failure ({0}). The crash log could not be written. | Error: fallo inesperado ({0}). No se pudo escribir el registro de fallos. |
| Scanning {InstallerFolder}... | Analizando {InstallerFolder}... |
| Found {0} unneeded {1} to clean up ({2}). | Se encontraron {0} {1} innecesarios para limpiar ({2}). |
| Found no unneeded files. | No se encontraron archivos innecesarios. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it held back the one file ({2}) it might otherwise have offered. | InstallerClean no pudo determinar con certeza qué archivos almacenados en caché pertenecen a los programas instalados aquí, así que ha retenido el único archivo ({2}) que podría haber ofrecido. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it held back all {0} {1} ({2}) it might otherwise have offered. | InstallerClean no pudo determinar con certeza qué archivos almacenados en caché pertenecen a los programas instalados aquí, así que ha retenido los {0} {1} ({2}) que podría haber ofrecido. |
| Windows has a record for {0} file that is not in {InstallerFolder}: {1}. It causes no trouble day to day, but a repair, update or uninstall can fail on it. Running that program's installer again, preferably the same version, usually restores the file. | Windows tiene un registro de {0} archivo que no está en {InstallerFolder}: {1}. En el día a día no causa problemas, pero una reparación, una actualización o una desinstalación pueden fallar por él. Volver a ejecutar el instalador de ese programa, preferiblemente la misma versión, suele restaurar el archivo. |
| Windows has records for {0} files that are not in {InstallerFolder}: {1}. They cause no trouble day to day, but a repair, update or uninstall can fail on them. Running each program's installer again, preferably the same version, usually restores the files. | Windows tiene registros de {0} archivos que no están en {InstallerFolder}: {1}. En el día a día no causan problemas, pero una reparación, una actualización o una desinstalación pueden fallar por ellos. Volver a ejecutar el instalador de cada programa, preferiblemente la misma versión, suele restaurar los archivos. |
| InstallerClean couldn't match up everything in the Windows records, so it didn't read all of them. What it found is unaffected, but what it says about files missing from {InstallerFolder} may be short of the full picture. Running it again may pick up more. | InstallerClean no pudo hacer coincidir todo lo que hay en los registros de Windows, así que no los leyó todos. Lo que encontró no se ve afectado, pero lo que dice sobre archivos que faltan en {InstallerFolder} puede quedarse corto. Volver a ejecutarlo puede detectar más. |
| Deleting {0} unneeded {1}... | Eliminando {0} {1} innecesarios... |
| Permanently deleted {0} unneeded {1}. | Se eliminaron definitivamente {0} {1} innecesarios. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Error: no se ha especificado un destino para mover. Usa /m RUTA. (Una ubicación predeterminada configurada en la GUI se guarda por usuario y no se aplica a las ejecuciones programadas ni a las de cuenta de servicio.) |
| Error: destination cannot be inside the Windows Installer folder. | Error: el destino no puede estar dentro de la carpeta de Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Error: el destino debe ser una ruta completa. Recibido: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Error: el destino {0} se resuelve dentro de una carpeta del sistema de Windows. Elige una ruta fuera de %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% y %ProgramData%. |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | Error: espacio insuficiente en {0}. Mover estos archivos necesita {1} y hay {2} libres. No se ha movido nada. |
| Error: something is using Windows Installer right now, such as a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | Error: algo está usando Windows Installer en este momento, como una actualización de Windows o un programa instalándose en segundo plano. /m y /d están bloqueados mientras eso ocurre. Inténtalo de nuevo cuando termine. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Error: en este equipo hay una transacción anterior de Windows Installer en suspenso. Reanuda o deshaz esa instalación (o reinicia Windows) antes de limpiar {InstallerFolder}. |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | Error: una operación de archivo en cola tras el reinicio afecta a {InstallerFolder} ({0}). Reinicia Windows para completar esa operación antes de limpiar. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Error: Windows Installer tiene algo en curso, así que /m y /d están bloqueados. InstallerClean no tocará {InstallerFolder} mientras cambia. Inténtalo de nuevo cuando termine. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | Error: InstallerClean no pudo tomar el bloqueo de Windows Installer que impide que dos programas cambien el software instalado a la vez, así que no pudo descartar que un archivo pasara a ser necesario a mitad de camino. No se ha eliminado nada. Inténtalo de nuevo, y reinicia Windows si sigue ocurriendo. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. Try again, and restart Windows if it keeps happening. | Error: InstallerClean no pudo tomar el bloqueo de Windows Installer que impide que dos programas cambien el software instalado a la vez, así que no pudo descartar que un archivo pasara a ser necesario a mitad de camino. No se ha movido nada. Inténtalo de nuevo, y reinicia Windows si sigue ocurriendo. |
| Moving {0} unneeded {1} to {2}... | Moviendo {0} {1} innecesarios a {2}... |
| Moved {0} unneeded {1}. | Se movieron {0} {1} innecesarios. |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then run the command again. | InstallerClean ya no pudo confirmar la carpeta de destino, así que se detuvo en lugar de escribir en el sitio equivocado. Comprueba {0} y vuelve a ejecutar el comando. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Otro proceso de InstallerClean mantiene el bloqueo de instancia única (la GUI u otra ejecución de la CLI). Código de salida 75 (transitorio); es seguro reintentar más tarde. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Nota: error al escribir en el registro de eventos. Comprueba los permisos del registro Aplicación o las directivas de grupo. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - limpiar {InstallerFolder} |
| Removes cached .msi and .msp files that no installed program still needs. | Quita archivos .msi/.msp en caché que ningún programa instalado necesita. |
| Needs an elevated (administrator) prompt; Windows will not start it. | Requiere símbolo del sistema como administrador; Windows no lo iniciará. |
| Usage: | Uso: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     Muestra esta ayuda (acepta también /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  Muestra la versión (acepta también -v) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         Solo analizar - lista los innecesarios |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         Elimina definitivamente los innecesarios |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         Mueve a la carpeta de destino guardada |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m RUTA    Mueve a la ruta especificada |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli bloquea el símbolo del sistema hasta terminar, para<br>que un script o una tarea programada pueda esperarlo. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | Carpeta por usuario; ejecuciones programadas o SYSTEM: /m RUTA. |
| Exit codes: | Códigos de salida: |
|   0   success: the run did what it was asked and nothing failed |   0   correcto: hizo lo que se le pidió y nada falló |
|   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |   1   fallo: no se procesó nada (argumentos o destino incorrectos,<br>       análisis fallido o todos los archivos fallaron) |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   parcial: unos procesados y otros no (un fallo o un Ctrl+C) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  transitorio: algo temporal bloqueó la ejecución (ver el mensaje) |
|   130 cancelled (Ctrl+C) |   130 cancelado (Ctrl+C) |
