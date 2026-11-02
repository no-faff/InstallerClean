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
| The anonymous report | El informe anónimo |
| InstallerClean couldn't read the report. | InstallerClean no pudo leer el informe. |

## Section headings

| English | Español |
| --- | --- |
| PATCHES | PARCHES |
| PRODUCT DETAILS | DETALLES DEL PRODUCTO |
| BACKUP FOLDER | CARPETA DE COPIA DE SEGURIDAD |
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
| Carry on _without it | Continuar _sin esperar |
| Check for _updates | _Buscar actualizaciones |
| _Close | _Cerrar |
| _Close without donating | _Cerrar sin donar |
| _Delete permanently | _Eliminar definitivamente |
| _Done | _Listo |
| Details | Detalles |
| _Donate | _Donar |
| _Donate $3 | _Donar 3 $ |
| Donate | Donar |
| Leave a _star on GitHub | Deja una e_strella en GitHub |
| Apache 2.0 licence | Licencia Apache 2.0 |
| _Move | _Mover |
| Path to folder if you move rather than delete. | Ruta a la carpeta si mueves en lugar de eliminar. |
| _Re-scan | _Volver a analizar |

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
| (no program) | (sin programa) |
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
| Checking registry for additional packages... | Comprobando el registro en busca de paquetes adicionales... |
| Matching files against installed programs... | Comparando los archivos con los programas instalados... |
| {0} of {1} | {0} de {1} |
| Checking remaining files... | Comprobando los archivos restantes... |
| Waiting for drive {0} to respond... | Esperando a que responda la unidad {0}... |
| Waiting for {0} to respond... | Esperando a que responda {0}... |
| Any file still to be checked against it will be left alone. | Cualquier archivo que aún deba cotejarse con lo que hay allí se dejará en paz. |
| Scan complete ({0}) | Análisis completado ({0}) |
| Found {0} {1} you can safely delete. | Se encontraron {0} {1} que puedes eliminar sin riesgo. |
| Preparing destination folder... | Preparando la carpeta de destino... |
| Moving unneeded files... | Moviendo archivos innecesarios... |
| Deleting unneeded files... | Eliminando archivos innecesarios... |
| Move cancelled. {0} of {1} {2} processed. | Movimiento cancelado tras procesar {0} de {1} {2}. |
| Delete cancelled. {0} of {1} {2} processed. | Eliminación cancelada tras procesar {0} de {1} {2}. |
| {0}. Details are in {1}. | {0}. Detalles en {1}. |
| {0}. The crash log could not be written. | {0}. No se pudo escribir el archivo crash.log. |
| {0}. Details are in {1}. | {0}. Detalles en {1}. |
| {0}. The crash log could not be written. | {0}. No se pudo escribir el archivo crash.log. |
| Access denied. Windows refused the scan. | Acceso denegado. Windows rechazó el análisis. |
| Scan stopped. | Análisis detenido. |
| Scan cancelled. | Análisis cancelado. |
| Ready | Listo |
| Scan failed ({0}). Details in {1}. | Análisis fallido ({0}). Detalles en {1}. |
| Scan failed ({0}). The crash log could not be written. | Análisis fallido ({0}). No se pudo escribir el archivo crash.log. |

## Main screen text

| English | Español |
| --- | --- |
| Any unneeded files below are safe to delete. | Cualquier archivo innecesario de los de abajo se puede eliminar sin riesgo. |
| They sit in {InstallerFolder}. InstallerClean asks Windows about every installed program: a file is listed when no program claims it ({0}), or when a newer patch has replaced it and no program could roll back to it ({1}). | Están en {InstallerFolder}. InstallerClean pregunta a Windows por cada programa instalado: un archivo aparece en la lista cuando ningún programa lo reclama ({0}), o cuando un parche más nuevo lo ha sustituido y ningún programa podría volver atrás hasta él ({1}). |
| Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update and uninstall as normal. Putting them back into {InstallerFolder} restores everything. Or delete them permanently now. | Muévelos a una carpeta de copia de seguridad que elijas y luego elimina esa carpeta cuando estés convencido de que tus programas siguen actualizándose y desinstalándose con normalidad. Devolverlos a {InstallerFolder} lo restaura todo. O elimínalos definitivamente ahora. |
| Nothing scanned yet. | Aún no se ha analizado nada. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Pulsa Volver a analizar para revisar {InstallerFolder} en busca de archivos de instalación que ya no necesita ningún programa. |
| Nothing to show until the next scan. | Nada que mostrar hasta el próximo análisis. |
| The scan that runs after a Move or Delete didn't finish, so there's no list to show. Press Re-scan to scan {InstallerFolder} again. | El análisis que se ejecuta después de Mover o Eliminar no terminó, así que no hay ninguna lista que mostrar. Pulsa Volver a analizar para analizar {InstallerFolder} otra vez. |
| What stopped it is recorded in {0}. | Lo que lo detuvo queda registrado en {0}. |
| These files can't be cleaned up right now. | Estos archivos no se pueden limpiar ahora mismo. |
| Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Algo está usando Windows Installer en este momento, como una actualización de Windows o un programa instalándose en segundo plano. Mover y Eliminar están en pausa mientras eso ocurre, así que InstallerClean no tocará {InstallerFolder} mientras cambia. Cuando termine, vuelve a analizar y estarán de nuevo disponibles. |
| Windows refused InstallerClean permission to check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If Windows still refuses, this isn't a machine InstallerClean can clean. | Windows le negó a InstallerClean el permiso para comprobar si Windows Installer está ocupado, así que Mover y Eliminar están en pausa e InstallerClean no tocará {InstallerFolder}. Reinicia Windows y usa Volver a analizar. Si Windows sigue negándolo, este no es un equipo que InstallerClean pueda limpiar. |
| InstallerClean couldn't check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If it still can't check, this isn't a machine InstallerClean can clean. | InstallerClean no pudo comprobar si Windows Installer está ocupado, así que Mover y Eliminar están en pausa e InstallerClean no tocará {InstallerFolder}. Reinicia Windows y usa Volver a analizar. Si sigue sin poder comprobarlo, este no es un equipo que InstallerClean pueda limpiar. |
| InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}). Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If it still can't check, this isn't a machine InstallerClean can clean. | InstallerClean no pudo comprobar si Windows Installer está ocupado, porque Windows dio un error: {0} (error {1}). Mover y Eliminar están en pausa e InstallerClean no tocará {InstallerFolder}. Reinicia Windows y usa Volver a analizar. Si sigue sin poder comprobarlo, este no es un equipo que InstallerClean pueda limpiar. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | En este equipo hay una transacción anterior de Windows Installer en suspenso. Reanuda o deshaz esa instalación (o reinicia Windows) antes de limpiar {InstallerFolder}. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows tiene en cola para el próximo reinicio un cambio de nombre de archivo que afecta a {InstallerFolder}. Reinicia Windows antes de limpiar. |
| A file operation is queued for the next restart and InstallerClean can't tell which files it names, so it can't rule out that they're in {InstallerFolder}. Restart Windows before cleaning. | Hay una operación de archivos en cola para el próximo reinicio e InstallerClean no puede saber qué archivos nombra, así que no puede descartar que estén en {InstallerFolder}. Reinicia Windows antes de limpiar. |
| InstallerClean couldn't read one of the Windows settings it checks before touching {InstallerFolder}, so it can't tell whether an installer operation is running or waiting for a restart. Restart Windows and Re-scan. If the setting still won't read, this isn't a machine InstallerClean can clean. | InstallerClean no pudo leer una de las opciones de Windows que comprueba antes de tocar {InstallerFolder}, así que no puede saber si hay una operación de instalación en curso o esperando a un reinicio. Reinicia Windows y usa Volver a analizar. Si la opción sigue sin poder leerse, este no es un equipo que InstallerClean pueda limpiar. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer tiene algo en curso, así que Mover y Eliminar están en pausa. InstallerClean no tocará {InstallerFolder} mientras cambia. Cuando termine, vuelve a analizar y estarán de nuevo disponibles. |
| Select a file to view details. | Selecciona un archivo para ver sus detalles. |
| Select a product to view details. | Selecciona un producto para ver sus detalles. |
| No metadata available. | No hay metadatos disponibles. |
| This installer file is missing. It causes no trouble now, and won't until the day you try to update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To put it back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Falta este archivo de instalación. Ahora mismo no causa ningún problema, y no lo causará hasta el día en que intentes actualizar o desinstalar el programa al que pertenece. Ese paso puede fallar entonces, porque Windows busca este archivo y no está.<br><br>Para reponerlo, necesitas el instalador de la versión que ya tienes. Consíguelo del fabricante del programa y ejecútalo sobre tu copia actual. Una versión más nueva no sirve: tendría que quitar primero la que tienes, y ese es justo el paso que necesita este archivo. Desinstalar primero tampoco funciona, por la misma razón. Esto debería restaurar el archivo y dejar tu configuración intacta, pero Microsoft no lo garantiza. |
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
| Nothing offered on this PC | No se ofreció nada en este PC |
| One file ({2}) was held back because Windows shows it was changed after InstallerClean started scanning. If a program was installing or updating, scan again once it has finished and InstallerClean will check it again. | Se ha retenido un archivo ({2}) porque Windows indica que se modificó después de que InstallerClean empezara a analizar. Si un programa se estaba instalando o actualizando, vuelve a analizar cuando haya terminado e InstallerClean volverá a comprobarlo. |
| {0} {1} ({2}) were held back because Windows shows they were changed after InstallerClean started scanning. If a program was installing or updating, scan again once it has finished and InstallerClean will check them again. | Se han retenido {0} {1} ({2}) porque Windows indica que se modificaron después de que InstallerClean empezara a analizar. Si un programa se estaba instalando o actualizando, vuelve a analizar cuando haya terminado e InstallerClean volverá a comprobarlos. |
| Delete that folder when you're satisfied all is well. | Elimina esa carpeta cuando estés convencido de que todo va bien. |
| Delete that folder when you're satisfied all is well. You won't actually reclaim the space until you do. | Elimina esa carpeta cuando estés convencido de que todo va bien. Hasta entonces no recuperarás el espacio. |
| {0} freed | {0} liberados |
| {0} moved | {0} movidos |
| Nothing was moved | No se movió nada |
| Nothing was deleted | No se eliminó nada |
| {0} file could not be moved. | No se pudo mover {0} archivo. |
| {0} files could not be moved. | No se pudieron mover {0} archivos. |
| {0} file could not be deleted. | No se pudo eliminar {0} archivo. |
| {0} files could not be deleted. | No se pudieron eliminar {0} archivos. |
| {0} {1} moved to: {2} | {0} {1} movido a: {2} |
| {0} {1} moved to: {2} | {0} {1} movidos a: {2} |
| {0} file held back. The scan said it was unneeded. The final check couldn't confirm that. | {0} archivo retenido. El análisis dijo que no era necesario. La comprobación final no pudo confirmarlo. |
| {0} files held back. The scan said these were unneeded. The final check couldn't confirm that. | {0} archivos retenidos. El análisis dijo que no eran necesarios. La comprobación final no pudo confirmarlo. |
| {0} {1} kept in place, because Windows has a record of the program named inside. | {0} {1} conservados en su sitio, porque Windows tiene un registro del programa que se nombra dentro. |
| {0} {1} kept in place, because InstallerClean couldn't find a program named inside. | {0} {1} conservados en su sitio, porque InstallerClean no encontró ningún programa nombrado dentro. |
| Moved {0} of {1} {2} to {3} before you cancelled. | Cancelaste tras mover {0} de {1} {2} a {3}. |
| Permanently deleted {0} of {1} {2} before you cancelled. | Cancelaste tras eliminar definitivamente {0} de {1} {2}. |
| It's simple to undo. Move them back into {InstallerFolder} and everything will be back to how it was. | Deshacerlo es sencillo. Muévelos de vuelta a {InstallerFolder} y todo quedará como estaba. |
| {0} {1} permanently deleted | {0} {1} eliminado definitivamente |
| {0} {1} permanently deleted | {0} {1} eliminados definitivamente |
| Any small donation is much appreciated | Cualquier donación, por pequeña que sea, se agradece mucho |
| Send _anonymous report | Enviar informe _anónimo |
| The report adds your result to [this chart] on GitHub, which shows how much space people are reclaiming with InstallerClean. It also holds these technical details, to help me spot any bugs: | El informe añade tu resultado a [este gráfico] en GitHub, que muestra cuánto espacio están recuperando las personas con InstallerClean. También incluye estos datos técnicos, que me ayudan a detectar errores: |
| How much space was freed, and how many files were moved or deleted | Cuánto espacio se liberó y cuántos archivos se movieron o eliminaron |
| Whether a Move went to the same drive, another drive, a removable drive or a network share | Si Mover llevó los archivos a la misma unidad, a otra unidad, a una unidad extraíble o a un recurso compartido de red |
| How long the scan and the Move or Delete took | Cuánto tardaron el análisis y Mover o Eliminar |
| How many installer files there are of each kind, and how much space they take | Cuántos archivos de instalación hay de cada tipo y cuánto espacio ocupan |
| How many files were left alone, and why | Cuántos archivos se dejaron en paz y por qué |
| How often InstallerClean waited for a slow drive or network share, and how many it stopped waiting for | Cuántas veces InstallerClean esperó a una unidad o un recurso compartido de red lentos, y a cuántos dejó de esperar |
| How many programs and patches Windows has a record of | De cuántos programas y parches tiene registro Windows |
| Whether Windows still makes old-style short file names | Si Windows sigue creando nombres de archivo cortos de estilo antiguo |
| Whether Windows is 10 or 11 and x64 or Arm, and which language and country or region it's set to | Si Windows es 10 u 11 y x64 o Arm, y en qué idioma y país o región está configurado |
| InstallerClean's version, how you got it (setup, portable, slim, Scoop, the command-line tool on its own, built from source, or not known), whether the report was sent from InstallerClean or the command-line tool, and the language it was showing | La versión de InstallerClean, cómo lo obtuviste (setup, portable, slim, Scoop, la herramienta de línea de comandos por sí sola, compilado desde el código fuente o desconocido), si el informe se envió desde InstallerClean o desde la herramienta de línea de comandos, y el idioma en que se mostraba |
| Counts of anything that went wrong | Recuentos de cualquier cosa que haya fallado |
| Each of these is a number or a label. Nothing in the report names you, your PC, your files or your programs, and I never see your IP address. | Cada uno de estos datos es un número o una etiqueta. Nada del informe te identifica a ti, a tu PC, tus archivos ni tus programas, y nunca veo tu dirección IP. |
| See exactly what's sent | Ver exactamente qué se envía |

## Summaries and counts

| English | Español |
| --- | --- |
| {0} file left alone | {0} archivo dejado en paz |
| {0} files left alone | {0} archivos dejados en paz |
| {0} unneeded file to clean up | {0} archivo innecesario para limpiar |
| {0} unneeded files to clean up | {0} archivos innecesarios para limpiar |
| Windows has a record for {0} file that isn't in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. Open Details for what to do. | Windows tiene un registro de {0} archivo que no está en {InstallerFolder}: {1}. En el día a día no causa problemas, pero una actualización o desinstalación de ese programa puede fallar. Abre Detalles para saber qué hacer. |
| Windows has records for {0} files that aren't in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. Open Details for what to do. | Windows tiene registros de {0} archivos que no están en {InstallerFolder}: {1}. En el día a día no causan problemas, pero una actualización o desinstalación de esos programas puede fallar. Abre Detalles para saber qué hacer. |
| {0} other program | {0} programa más |
| {0} other programs | {0} programas más |
| {0} file with no program named in the records | {0} archivo sin ningún programa nombrado en los registros |
| {0} files with no program named in the records | {0} archivos sin ningún programa nombrado en los registros |
| InstallerClean carried on without drive {0} and left alone any file still to be checked against it. Once it's responding normally, Re-scan. | InstallerClean continuó sin la unidad {0} y dejó en paz cualquier archivo que aún debía cotejarse con ella. Cuando vuelva a responder con normalidad, pulsa Volver a analizar. |
| InstallerClean carried on without {0} and left alone any file still to be checked against it. Once it's responding normally, Re-scan. | InstallerClean continuó sin {0} y dejó en paz cualquier archivo que aún debía cotejarse con lo que hay allí. Cuando vuelva a responder con normalidad, pulsa Volver a analizar. |
| InstallerClean carried on without more than one drive or share ({0}) and left alone any file still to be checked against one of them. Once they're responding normally, Re-scan. | InstallerClean continuó sin más de una unidad o recurso compartido ({0}) y dejó en paz cualquier archivo que aún debía cotejarse con alguno de ellos. Cuando vuelvan a responder con normalidad, pulsa Volver a analizar. |
| Something was using Windows Installer when the scan started, such as a Windows Update or a program installing, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan once that has finished. | Algo estaba usando Windows Installer cuando empezó el análisis, como una actualización de Windows o un programa instalándose, así que InstallerClean retuvo los archivos de {InstallerFolder} que no aparecen nombrados en los registros de ningún programa instalado, en lugar de ofrecerlos. Pulsa Volver a analizar cuando eso haya terminado. |
| A Windows Installer transaction was suspended on this machine when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Resume or roll back that install, or restart Windows, then Re-scan. | Había una transacción de Windows Installer en suspenso en este equipo cuando empezó el análisis, así que InstallerClean retuvo los archivos de {InstallerFolder} que no aparecen nombrados en los registros de ningún programa instalado, en lugar de ofrecerlos. Reanuda o deshaz esa instalación, o reinicia Windows, y luego pulsa Volver a analizar. |
| When the scan started, Windows had a file rename queued for the next restart that affects {InstallerFolder}, so InstallerClean held back the files there that no installed program's records name, rather than offering them. Restart Windows, then Re-scan. | Cuando empezó el análisis, Windows tenía en cola para el próximo reinicio un cambio de nombre de archivo que afecta a {InstallerFolder}, así que InstallerClean retuvo los archivos de esa carpeta que no aparecen nombrados en los registros de ningún programa instalado, en lugar de ofrecerlos. Reinicia Windows y luego pulsa Volver a analizar. |
| When the scan started, a file operation was queued for the next restart and InstallerClean couldn't tell which files it names, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows, then Re-scan. | Cuando empezó el análisis, había una operación de archivos en cola para el próximo reinicio e InstallerClean no pudo saber qué archivos nombra, así que retuvo los archivos de {InstallerFolder} que no aparecen nombrados en los registros de ningún programa instalado, en lugar de ofrecerlos. Reinicia Windows y luego pulsa Volver a analizar. |
| When the scan started, Windows refused InstallerClean permission to check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Cuando empezó el análisis, Windows le negó a InstallerClean el permiso para comprobar si Windows Installer estaba ocupado, así que retuvo los archivos de {InstallerFolder} que no aparecen nombrados en los registros de ningún programa instalado, en lugar de ofrecerlos. Reinicia Windows y pulsa Volver a analizar. |
| When the scan started, InstallerClean couldn't read one of the Windows settings it checks, so it couldn't tell whether an installer operation was running or waiting for a restart, and it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Cuando empezó el análisis, InstallerClean no pudo leer una de las opciones de Windows que comprueba, así que no pudo saber si había una operación de instalación en curso o esperando a un reinicio, y retuvo los archivos de {InstallerFolder} que no aparecen nombrados en los registros de ningún programa instalado, en lugar de ofrecerlos. Reinicia Windows y pulsa Volver a analizar. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Cuando empezó el análisis, InstallerClean no pudo comprobar si Windows Installer estaba ocupado, así que retuvo los archivos de {InstallerFolder} que no aparecen nombrados en los registros de ningún programa instalado, en lugar de ofrecerlos. Reinicia Windows y pulsa Volver a analizar. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, because Windows gave an error: {0} (error {1}). So it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Cuando empezó el análisis, InstallerClean no pudo comprobar si Windows Installer estaba ocupado, porque Windows dio un error: {0} (error {1}). Así que retuvo los archivos de {InstallerFolder} que no aparecen nombrados en los registros de ningún programa instalado, en lugar de ofrecerlos. Reinicia Windows y pulsa Volver a analizar. |
| Windows Installer had something in progress or waiting when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan once that has cleared. | Windows Installer tenía algo en curso o pendiente cuando empezó el análisis, así que InstallerClean retuvo los archivos de {InstallerFolder} que no aparecen nombrados en los registros de ningún programa instalado, en lugar de ofrecerlos. Pulsa Volver a analizar cuando eso se haya resuelto. |
| This PC's clock went back or lost time while the scan was running, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan. | El reloj de este equipo retrocedió o se atrasó mientras se ejecutaba el análisis, así que InstallerClean retuvo los archivos de {InstallerFolder} que no aparecen nombrados en los registros de ningún programa instalado, en lugar de ofrecerlos. Pulsa Volver a analizar. |
| {0} of {1} {2} | {0} de {1} {2} |
| {0} unneeded {1} ({2}) | {0} {1} para limpiar ({2}) |
| {0} file left alone ({1}) | {0} archivo dejado en paz ({1}) |
| {0} files left alone ({1}) | {0} archivos dejados en paz ({1}) |
| {0} missing | {0} ausente |
| {0} missing | {0} ausentes |

## Confirmation dialogs

| English | Español |
| --- | --- |
| Move {0} {1} ({2})? | ¿Mover {0} {1} ({2})? |
| This file will be moved to: | Este archivo se moverá a: |
| These files will be moved to: | Estos archivos se moverán a: |
| Delete {0} {1} ({2})? | ¿Eliminar {0} {1} ({2})? |
| This file will be deleted permanently. It's safe to do but if you'd like a backup, use Move instead. | Este archivo se eliminará definitivamente. Es seguro hacerlo, pero si quieres una copia, usa Mover en su lugar. |
| These files will be deleted permanently. It's safe to do but if you'd like a backup, use Move instead. | Estos archivos se eliminarán definitivamente. Es seguro hacerlo, pero si quieres una copia, usa Mover en su lugar. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | Esa carpeta está en la misma unidad, así que el espacio no volverá hasta que la elimines. Elige una carpeta en otra unidad si quieres el espacio de inmediato. |

## Error messages

| English | Español |
| --- | --- |
| This is also recorded in {0}. | Esto también queda registrado en {0}. |
| Access denied | Acceso denegado |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows le negó el acceso a InstallerClean, así que se detuvo. No se ha eliminado nada.<br><br>InstallerClean ya se estaba ejecutando como administrador, así que volver a iniciarlo de esa forma no servirá de nada. Windows no dice nada más sobre qué denegó el acceso, así que no hay nada concreto que puedas probar. |
| InstallerClean stopped | InstallerClean se detuvo |
| Scan failed | Análisis fallido |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed. | Los registros de Windows Installer llegaron completamente vacíos: ni un solo programa instalado ni una sola actualización reclama un archivo de instalación en caché. Eso no ocurre en un equipo que funciona (incluso una instalación nueva de Windows tiene alguno), así que o los registros están dañados o no se pudieron leer, y un análisis que se creyera esta respuesta marcaría por error como huérfano cada archivo de {InstallerFolder}. InstallerClean se detuvo en lugar de eso. No se ha eliminado nada. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer no dejó que InstallerClean enumerara lo que hay instalado. InstallerClean ya se estaba ejecutando como administrador, así que volver a ejecutarlo como administrador no cambiará nada. Sin esa lista no hay forma segura de saber qué archivos en caché siguen haciendo falta, así que InstallerClean se detuvo. No se ha eliminado nada. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: it read {1} {2}, then an entry came back unreadable (error code {0}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer no pudo darle a InstallerClean una lista legible de los programas instalados: leyó {1} {2} y luego una entrada llegó ilegible (código de error {0}). En lugar de trabajar con una lista leída a medias, InstallerClean se detuvo. No se ha eliminado nada. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer nunca señaló el final de la lista de programas instalados: InstallerClean leyó {2} {3} y luego se rindió tras {0} entradas (último código de error {1}). De una lista sin final no hay que fiarse, así que InstallerClean se detuvo. No se ha eliminado nada. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer nunca señaló el final de la lista de parches de un programa: InstallerClean leyó {2} {3} y luego se rindió tras {0} entradas (último código de error {1}). De una lista sin final no hay que fiarse, así que InstallerClean se detuvo. No se ha eliminado nada. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean no pudo hacer coincidir los registros de Windows Installer con el contenido de {InstallerFolder}. Casi nada de lo que señalan los registros está realmente ahí, y casi nada de lo que hay ahí lo nombra ningún registro, así que no se pudo demostrar que ningún archivo fuera innecesario. No se ha ofrecido nada y no se ha quitado nada. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean no pudo hacer coincidir los registros de Windows Installer con el contenido de {InstallerFolder}. La carpeta tiene archivos, pero ni un solo registro señala nada de lo que hay ahí, así que no se pudo demostrar que ningún archivo fuera innecesario. No se ha ofrecido nada y no se ha quitado nada. |
| InstallerClean can't scan {InstallerFolder} on this PC. Windows reports that folder as {0}, and InstallerClean only scans an Installer folder that Windows reports at its own path. Nothing has been offered and nothing has been removed. | InstallerClean no puede analizar {InstallerFolder} en este PC. Windows sitúa esa carpeta en {0}, e InstallerClean solo analiza una carpeta Installer que Windows sitúe en su propia ruta. No se ha ofrecido nada y no se ha quitado nada. |
| Windows reports that {InstallerFolder} doesn't exist on this PC, so InstallerClean stopped. Nothing has been offered and nothing has been removed. | Windows indica que {InstallerFolder} no existe en este PC, así que InstallerClean se detuvo. No se ha ofrecido nada y no se ha quitado nada. |
| Windows reported an error when InstallerClean tried to list the files in {InstallerFolder}, so it couldn't get a complete list and stopped. Nothing has been offered and nothing has been removed. | Windows devolvió un error cuando InstallerClean intentó listar los archivos de {InstallerFolder}, así que no pudo obtener una lista completa y se detuvo. No se ha ofrecido nada y no se ha quitado nada. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean no pudo leer lo suficiente de los registros de Windows Installer para estar seguro de qué sigue haciendo falta: la lista de programas instalados llegó incompleta, y leer esos mismos registros directamente desde el registro de Windows también dio errores. Un archivo podría parecer huérfano solo porque el registro que lo nombra era uno de los ilegibles, así que InstallerClean se detuvo. No se ha eliminado nada. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean no consiguió que Windows resolviera la ruta real de {InstallerFolder}, así que no se pudo demostrar que ningún archivo estuviera dentro y no se ofreció ninguno para limpiar. Este análisis no encontró nada porque esa comprobación falló, no porque la carpeta esté limpia. No se ha quitado nada. |
| Nothing was deleted | No se eliminó nada |
| Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. | Windows le negó a InstallerClean el permiso para comprobar si Windows Installer estaba ocupado, así que no pudo descartar que un archivo hiciera falta a mitad del proceso, y no se ha eliminado nada. |
| Nothing was moved | No se movió nada |
| Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. | Windows le negó a InstallerClean el permiso para comprobar si Windows Installer estaba ocupado, así que no pudo descartar que un archivo hiciera falta a mitad del proceso, y no se ha movido nada. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | InstallerClean no pudo tomar el bloqueo que usa Windows Installer para impedir que dos programas cambien el software instalado a la vez, así que no pudo descartar que un archivo pasara a ser necesario a mitad de camino, y no se ha eliminado nada. Inténtalo de nuevo, y reinicia Windows si sigue ocurriendo. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. Try again, and restart Windows if it keeps happening. | InstallerClean no pudo tomar el bloqueo que usa Windows Installer para impedir que dos programas cambien el software instalado a la vez, así que no pudo descartar que un archivo pasara a ser necesario a mitad de camino, y no se ha movido nada. Inténtalo de nuevo, y reinicia Windows si sigue ocurriendo. |
| Invalid destination | Destino no válido |
| Move stopped | Movimiento detenido |
| Couldn't use that backup folder | No se pudo usar esa carpeta de copia de seguridad |
| Move failed | Movimiento fallido |
| Delete failed | Eliminación fallida |
| Setting not saved | Ajuste no guardado |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | No se pudo guardar el cambio. La próxima vez que se inicie, InstallerClean volverá al ajuste anterior. |
| The destination cannot be inside the Windows Installer folder. | El destino no puede estar dentro de la carpeta de Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | El destino {0} se resuelve dentro de una carpeta del sistema de Windows. Elige una ruta fuera de %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% y %ProgramData%. |
| Not enough space | Espacio insuficiente |
| There isn't room at {0}<br><br>Required: {1}<br>Available: {2} | No hay sitio suficiente en {0}<br><br>Necesario: {1}<br>Disponible: {2} |
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
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | La carpeta de copia de seguridad tiene que ser una ruta completa a una carpeta, empezando por una letra de unidad o un recurso compartido de red (por ejemplo D:\Backup, o \\servidor\backup). InstallerClean no puede usar esta: {0} |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then Re-scan and try again. | InstallerClean ya no pudo confirmar la carpeta de copia de seguridad, así que se detuvo. Comprueba {0}, luego Volver a analizar e inténtalo de nuevo. |
| Cannot write to {0}. | No se puede escribir en {0}. |
| You can save files in {0} but not open them again, so InstallerClean won't move files there. | Puedes guardar archivos en {0}, pero no volver a abrirlos, así que InstallerClean no moverá archivos allí. |
| A file called '{0}' is already in the backup folder. | Ya hay un archivo llamado '{0}' en la carpeta de copia de seguridad. |

## Update check

| English | Español |
| --- | --- |
| Check for updates | Buscar actualizaciones |
| Checking... | Buscando... |
| Up to date. | Estás al día. |
| InstallerClean {0} is available. | Está disponible InstallerClean {0}. |
| It's strongly recommended to use the latest version. | Se recomienda encarecidamente usar la versión más reciente. |
| Download it from GitHub | Descárgalo de GitHub |
| Download .NET Framework 4.8 from Microsoft | Descarga .NET Framework 4.8 de Microsoft |
| Update now | Actualizar ahora |
| This copy was installed by Scoop. To update it, close InstallerClean and run: | Esta copia se instaló con Scoop. Para actualizarla, cierra InstallerClean y ejecuta: |
| This copy was installed by Scoop for all users. To update it, close InstallerClean, open a terminal as administrator and run: | Esta copia se instaló con Scoop para todos los usuarios. Para actualizarla, cierra InstallerClean, abre un terminal como administrador y ejecuta: |
| It needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then open InstallerClean to update. | Esta versión necesita .NET Framework 4.8. Instálalo desde Microsoft, reinicia el PC y luego abre InstallerClean para actualizar. |
| InstallerClean {0} can't be installed automatically. | InstallerClean {0} no se puede instalar automáticamente. |
| InstallerClean can't tell whether its setup installed it, so it can't update itself. | InstallerClean no puede saber si lo instaló su programa de instalación, así que no puede actualizarse. |
| Copied | Copiado |
| Couldn't copy it. Select the command and copy it yourself. | No se pudo copiar. Selecciona el comando y cópialo a mano. |
| _Continue without updating | _Continuar sin actualizar |
| Downloading InstallerClean {0}... | Descargando InstallerClean {0}... |
| Checking the download... | Comprobando la descarga... |
| The update couldn't be downloaded. | No se pudo descargar la actualización. |
| The update couldn't be installed. | No se pudo instalar la actualización. |
| Couldn't reach GitHub. Check your internet connection and try again. | No se pudo conectar con GitHub. Comprueba tu conexión a internet y vuelve a intentarlo. |
| GitHub returned an error response. Try again in a few minutes. | GitHub devolvió una respuesta de error. Vuelve a intentarlo en unos minutos. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | La respuesta de GitHub no contenía una versión reconocible. Vuelve a intentarlo más tarde, o abre directamente la página de versiones. |
| The check timed out. Your connection to GitHub may be slow; try again. | Se agotó el tiempo de espera de la comprobación. Tu conexión con GitHub puede ser lenta; vuelve a intentarlo. |
| The check failed for an unknown reason. Details are in {0} if you need to report it. | La comprobación falló por un motivo desconocido. Los detalles están en {0} por si necesitas informar del problema. |
| The check failed for an unknown reason. The crash log could not be written. | La comprobación falló por un motivo desconocido. No se pudo escribir el archivo crash.log. |

## Opening links in your browser

| English | Español |
| --- | --- |
| Couldn't open your browser | No se pudo abrir el navegador |
| The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | El enlace está en el portapapeles, así que puedes pegarlo tú mismo:<br><br>{0} |
| InstallerClean couldn't copy the link to your clipboard either, so here it is:<br><br>{0} | InstallerClean tampoco pudo copiar el enlace al portapapeles, así que aquí está:<br><br>{0} |

## Startup and crashes

| English | Español |
| --- | --- |
| InstallerClean | InstallerClean |
| It's already running. | Ya se está ejecutando. |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | Se produjo un error inesperado e InstallerClean debe cerrarse.<br><br>{0}<br><br>Detalles guardados en:<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | Se produjo un error inesperado e InstallerClean debe cerrarse.<br><br>{0}<br><br>No se pudo escribir el archivo crash.log. |
| Startup error | Error de inicio |
| Failed to start ({0}). Details written to:<br>{1} | No se pudo iniciar ({0}). Detalles guardados en:<br>{1} |
| Failed to start ({0}). The crash log could not be written. | No se pudo iniciar ({0}). No se pudo escribir el archivo crash.log. |
| Administrator rights needed | Se necesitan permisos de administrador |
| InstallerClean needs administrator rights to see every program installed on this PC, and it isn't running as administrator, so it won't scan. Nothing has been changed.<br><br>Run InstallerClean again as administrator. | InstallerClean necesita permisos de administrador para ver todos los programas instalados en este PC, y no se está ejecutando como administrador, así que no hará el análisis. No se ha cambiado nada.<br><br>Vuelve a ejecutar InstallerClean como administrador. |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log recoge las excepciones no controladas de InstallerClean.<br># Con permisos elevados, los mensajes de excepción del framework<br># pueden incluir rutas de archivo de la sesión en curso (incluidos<br># perfiles de otros usuarios enumerados por las consultas de Windows<br># Installer). Los mensajes de fallo de red de la comprobación de<br># actualizaciones o del envío del registro de resultados pueden<br># incluir la URL de destino y la IP o el proxy resueltos. Las<br># entradas sobre registros ilegibles de Windows Installer pueden<br># incluir un SID de cuenta de Windows (S-1-5-21-...) y los códigos<br># de producto del software instalado.<br># Quita los tres tipos de dato antes de adjuntar este archivo a un<br># informe de error público.<br> |

## Tooltips (hover text)

| English | Español |
| --- | --- |
| Any small donation is much appreciated | Cualquier donación, por pequeña que sea, se agradece mucho |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Cancelación solicitada. InstallerClean está esperando a que el paso en curso llegue a un punto en el que pueda detenerse. Puede tardar unos segundos durante operaciones intensas de entrada/salida o una llamada a la base de datos MSI. |
| Close | Cerrar |
| I really should charge for InstallerClean - because it's been a huge amount of work - but I want to keep it free so it helps as many people as possible. Your donation makes it worthwhile and is very much appreciated. | La verdad es que debería cobrar por InstallerClean, porque me ha llevado muchísimo trabajo, pero quiero que siga siendo gratis para que ayude a cuanta más gente mejor. Tu donativo hace que valga la pena y lo agradezco muchísimo. |
| A star helps other people find it | Una estrella ayuda a otras personas a encontrar InstallerClean |
| Minimise | Minimizar |
| Move the unneeded files to the backup folder | Mueve los archivos innecesarios a la carpeta de copia de seguridad |
| Move the unneeded files to a backup folder. You'll choose it next. | Mueve los archivos innecesarios a una carpeta de copia de seguridad. La elegirás a continuación. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder. | Mueve los archivos innecesarios a la carpeta de copia de seguridad. Está en la misma unidad, así que no recuperarás el espacio hasta que elimines esa carpeta. |
| Delete the unneeded files permanently. Use Move instead if you'd like a chance to satisfy yourself all is well. | Elimina definitivamente los archivos innecesarios. Usa Mover en su lugar si quieres la oportunidad de convencerte de que todo va bien. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Nombre del firmante del certificado Authenticode incorporado. La cadena no está verificada. |
| Change language. The program will restart. | Cambia el idioma. El programa se reiniciará. |

## Screen reader labels

| English | Español |
| --- | --- |
| Donate | Donar |
| Donate $3 | Donar 3 $ |
| Donate | Donar |
| Cancel operation | Cancelar la operación |
| Cancel scan | Cancelar el análisis |
| Cancel startup scan | Cancelar el análisis de inicio |
| Carry on without it, drive {0} | Continuar sin esperar, unidad {0} |
| Carry on without it, {0} | Continuar sin esperar, {0} |
| Close | Cerrar |
| Close window | Cerrar la ventana |
| Close result and return to main window | Cerrar el resultado y volver a la ventana principal |
| Leave a star on github | Deja una estrella en github |
| Minimise | Minimizar |
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Eliminar definitivamente quita los archivos innecesarios. Cancelar cierra sin eliminar nada. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Mover coloca los archivos innecesarios en la carpeta de destino elegida. Cancelar los deja donde están. |
| Say thanks | Dar las gracias |
| Check for updates | Buscar actualizaciones |
| Checks github's releases page for a newer version. | Consulta la página de versiones de github en busca de una versión más reciente. |
| Opens the readme on github in your browser. | Abre el readme en github en tu navegador. |
| Opens the issue tracker on github.com in your browser. | Abre el rastreador de problemas (Issues) en github.com en tu navegador. |
| If ticked, InstallerClean checks github for a newer version when you run it. | Si está marcada, InstallerClean busca en github una versión más reciente cuando lo ejecutas. |
| Download it from github | Descárgalo de github |
| Download .NET Framework 4.8 from Microsoft | Descarga .NET Framework 4.8 de Microsoft |
| Copy the command | Copiar el comando |
| Scoop's update command | Comando de actualización de Scoop |
| Opens the licence file on github.com in your browser. | Abre el archivo de la licencia en github.com en tu navegador. |
| Backup folder | Carpeta de copia de seguridad |
| Patches | Parches |
| Product details | Detalles del producto |
| Backup folder | Carpeta de copia de seguridad |
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
| Waiting for a drive or share | Esperando a una unidad o recurso compartido |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | Archivos que no se pudieron procesar |
| Explains this folder, and how to recover a file, in the README | Explica esta carpeta, y cómo recuperar un archivo, en el README |
| {0} Press Alt+{1} to carry on without it. {2} | {0} Pulsa Alt+{1} para continuar sin esperar. {2} |
| What the report holds | Qué contiene el informe |
| What the report holds | Qué contiene el informe |
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
| drive {0} | unidad {0} |
| .  | .  |
| {0} TB | {0} TB |
| {0} GB | {0} GB |
| {0} MB | {0} MB |
| {0} KB | {0} KB |
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
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({2}) rather than offering it. | InstallerClean no pudo determinar con certeza qué archivos en caché pertenecen a los programas instalados aquí, así que ha retenido el único archivo ({2}) en lugar de ofrecerlo. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} ({2}) rather than offering them. | InstallerClean no pudo determinar con certeza qué archivos en caché pertenecen a los programas instalados aquí, así que ha retenido {0} {1} ({2}) en lugar de ofrecerlos. |
| InstallerClean couldn't establish that one file is unneeded, so it has held it back ({2}) rather than offering it. | InstallerClean no pudo establecer que un archivo ya no fuera necesario, así que lo ha retenido ({2}) en lugar de ofrecerlo. |
| InstallerClean couldn't establish that {0} {1} are unneeded, so it has held them back ({2}) rather than offering them. | InstallerClean no pudo establecer que {0} {1} ya no fueran necesarios, así que los ha retenido ({2}) en lugar de ofrecerlos. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({2}) rather than offering it. | InstallerClean no pudo determinar con certeza qué archivos en caché pertenecen a los programas instalados aquí, así que ha retenido el único archivo ({2}) en lugar de ofrecerlo. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} ({2}) rather than offering them. | InstallerClean no pudo determinar con certeza qué archivos en caché pertenecen a los programas instalados aquí, así que ha retenido {0} {1} ({2}) en lugar de ofrecerlos. |
| InstallerClean couldn't establish that one file is unneeded, so it has held it back ({2}) rather than offering it. | InstallerClean no pudo establecer que un archivo ya no fuera necesario, así que lo ha retenido ({2}) en lugar de ofrecerlo. |
| InstallerClean couldn't establish that {0} {1} are unneeded, so it has held them back ({2}) rather than offering them. | InstallerClean no pudo establecer que {0} {1} ya no fueran necesarios, así que los ha retenido ({2}) en lugar de ofrecerlos. |
| Why it couldn't be certain: | Por qué no pudo tener la certeza: |
|   A file path in Windows Installer's own records wouldn't resolve, so nothing could be matched to it. |   Una ruta de archivo de los propios registros de Windows Installer no se pudo resolver, así que no se le pudo cotejar nada. |
|   A file Windows has a record of couldn't be identified, so it couldn't be matched to what's in the folder. |   No se pudo identificar un archivo del que Windows tiene registro, así que no se pudo cotejar con lo que hay en la carpeta. |
|   A program may be installed more than once on this PC, and the records can't say which copy a file belongs to. |   Puede que un programa esté instalado más de una vez en este PC, y los registros no pueden decir a qué copia pertenece un archivo. |
|   Part of Windows Installer's records wouldn't read, so a file could belong to a program recorded there. |   Una parte de los registros de Windows Installer no se pudo leer, así que un archivo podría pertenecer a un programa registrado allí. |
|   A file in the folder couldn't be identified, so it couldn't be matched against the records. |   No se pudo identificar un archivo de la carpeta, así que no se pudo cotejar con los registros. |
|   Either a file wouldn't say which program it belongs to, or Windows wouldn't give a clear answer about that program. |   O bien un archivo no indicó a qué programa pertenece, o bien Windows no dio una respuesta clara sobre ese programa. |
|   A check on which programs the files belong to gave answers that didn't line up with the files it was handed. |   Una comprobación de a qué programas pertenecen los archivos dio respuestas que no encajaban con los archivos que se le pasaron. |
|   Either a patch file wouldn't say which patch it is, or it wouldn't say which programs it's for, or Windows wouldn't say in full which programs have that patch. |   O bien un archivo de parche no indicó de qué parche se trata, o bien no indicó a qué programas está destinado, o bien Windows no indicó de forma completa qué programas tienen ese parche. |
| Windows has a record for {0} file that is not in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. To put the file back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Windows tiene un registro de {0} archivo que no está en {InstallerFolder}: {1}. En el día a día no causa problemas, pero una actualización o desinstalación de ese programa puede fallar. Para reponer el archivo, necesitas el instalador de la versión que ya tienes. Consíguelo del fabricante del programa y ejecútalo sobre tu copia actual. Una versión más nueva no sirve: tendría que quitar primero la que tienes, y ese es justo el paso que necesita este archivo. Desinstalar primero tampoco funciona, por la misma razón. Esto debería restaurar el archivo y dejar tu configuración intacta, pero Microsoft no lo garantiza. |
| Windows has records for {0} files that are not in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. To put a file back, you need the installer for the version you already have of that program. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs the file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Windows tiene registros de {0} archivos que no están en {InstallerFolder}: {1}. En el día a día no causan problemas, pero una actualización o desinstalación de esos programas puede fallar. Para reponer un archivo, necesitas el instalador de la versión que ya tienes de ese programa. Consíguelo del fabricante del programa y ejecútalo sobre tu copia actual. Una versión más nueva no sirve: tendría que quitar primero la que tienes, y ese es justo el paso que necesita el archivo. Desinstalar primero tampoco funciona, por la misma razón. Esto debería restaurar el archivo y dejar tu configuración intacta, pero Microsoft no lo garantiza. |
| InstallerClean couldn't be certain that the one superseded file is no longer needed, so it has held it back. | InstallerClean no pudo determinar con certeza que el único archivo sustituido ya no haga falta, así que lo ha retenido. |
| InstallerClean couldn't be certain that {0} superseded files are no longer needed, so it has held them back. | InstallerClean no pudo determinar con certeza que {0} archivos sustituidos ya no hagan falta, así que los ha retenido. |
| InstallerClean stopped waiting for drive {0} and left alone any file still to be checked against it. Once it's responding normally, run the command again. | InstallerClean dejó de esperar a la unidad {0} y dejó en paz cualquier archivo que aún debía cotejarse con ella. Cuando vuelva a responder con normalidad, vuelve a ejecutar el comando. |
| InstallerClean stopped waiting for {0} and left alone any file still to be checked against it. Once it's responding normally, run the command again. | InstallerClean dejó de esperar a {0} y dejó en paz cualquier archivo que aún debía cotejarse con lo que hay allí. Cuando vuelva a responder con normalidad, vuelve a ejecutar el comando. |
| InstallerClean stopped waiting for more than one drive or share ({0}) and left alone any file still to be checked against one of them. Once they're responding normally, run the command again. | InstallerClean dejó de esperar a más de una unidad o recurso compartido ({0}) y dejó en paz cualquier archivo que aún debía cotejarse con alguno de ellos. Cuando vuelvan a responder con normalidad, vuelve a ejecutar el comando. |
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
| Error: a file operation is queued for the next restart and InstallerClean can't tell which files it names, so it can't rule out {InstallerFolder}. Restart Windows before cleaning. | Error: hay una operación de archivos en cola para el próximo reinicio e InstallerClean no puede saber qué archivos nombra, así que no puede descartar {InstallerFolder}. Reinicia Windows antes de limpiar. |
| Error: InstallerClean couldn't read one of the registry values it checks before touching {InstallerFolder}, so it can't rule out a Windows Installer operation in flight or queued for the next restart. /m and /d are blocked. Restart Windows and try again. If the read still fails, this isn't a machine InstallerClean can clean. | Error: InstallerClean no pudo leer uno de los valores del registro que comprueba antes de tocar {InstallerFolder}, así que no puede descartar una operación de Windows Installer en curso o en cola para el próximo reinicio. /m y /d están bloqueados. Reinicia Windows y vuelve a intentarlo. Si la lectura sigue fallando, este no es un equipo que InstallerClean pueda limpiar. |
| Error: InstallerClean couldn't check whether Windows Installer is busy, so it can't rule out an installation in progress. /m and /d are blocked. Restart Windows and try again. If the check still fails, this isn't a machine InstallerClean can clean. | Error: InstallerClean no pudo comprobar si Windows Installer está ocupado, así que no puede descartar una instalación en curso. /m y /d están bloqueados. Reinicia Windows y vuelve a intentarlo. Si la comprobación sigue fallando, este no es un equipo que InstallerClean pueda limpiar. |
| Error: InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}), so it can't rule out an installation in progress. /m and /d are blocked. Restart Windows and try again. If the check still fails, this isn't a machine InstallerClean can clean. | Error: InstallerClean no pudo comprobar si Windows Installer está ocupado, porque Windows dio un error: {0} (error {1}), así que no puede descartar una instalación en curso. /m y /d están bloqueados. Reinicia Windows y vuelve a intentarlo. Si la comprobación sigue fallando, este no es un equipo que InstallerClean pueda limpiar. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Error: Windows Installer tiene algo en curso, así que /m y /d están bloqueados. InstallerClean no tocará {InstallerFolder} mientras cambia. Inténtalo de nuevo cuando termine. |
| Something was using Windows Installer when the scan started, such as a Windows Update or a program installing, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again once that has finished. | Algo estaba usando Windows Installer cuando empezó el análisis, como una actualización de Windows o un programa instalándose, así que InstallerClean retuvo los archivos de {InstallerFolder} que no aparecen nombrados en los registros de ningún programa instalado, en lugar de ofrecerlos. Vuelve a ejecutar el comando cuando eso haya terminado. |
| A Windows Installer transaction was suspended on this machine when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Resume or roll back that install, or restart Windows, then run it again. | Había una transacción de Windows Installer en suspenso en este equipo cuando empezó el análisis, así que InstallerClean retuvo los archivos de {InstallerFolder} que no aparecen nombrados en los registros de ningún programa instalado, en lugar de ofrecerlos. Reanuda o deshaz esa instalación, o reinicia Windows, y luego vuelve a ejecutar el comando. |
| When the scan started, Windows had a file rename queued for the next restart that affects {InstallerFolder}, so InstallerClean held back the files there that no installed program's records name, rather than offering them. Restart Windows, then run it again. | Cuando empezó el análisis, Windows tenía en cola para el próximo reinicio un cambio de nombre de archivo que afecta a {InstallerFolder}, así que InstallerClean retuvo los archivos de esa carpeta que no aparecen nombrados en los registros de ningún programa instalado, en lugar de ofrecerlos. Reinicia Windows y luego vuelve a ejecutar el comando. |
| When the scan started, a file operation was queued for the next restart and InstallerClean couldn't tell which files it names, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows, then run it again. | Cuando empezó el análisis, había una operación de archivos en cola para el próximo reinicio e InstallerClean no pudo saber qué archivos nombra, así que retuvo los archivos de {InstallerFolder} que no aparecen nombrados en los registros de ningún programa instalado, en lugar de ofrecerlos. Reinicia Windows y luego vuelve a ejecutar el comando. |
| When the scan started, Windows refused InstallerClean permission to check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Cuando empezó el análisis, Windows le negó a InstallerClean el permiso para comprobar si Windows Installer estaba ocupado, así que retuvo los archivos de {InstallerFolder} que no aparecen nombrados en los registros de ningún programa instalado, en lugar de ofrecerlos. Reinicia Windows y vuelve a ejecutar el comando. |
| When the scan started, InstallerClean couldn't read one of the Windows settings it checks, so it couldn't tell whether an installer operation was running or waiting for a restart, and it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Cuando empezó el análisis, InstallerClean no pudo leer una de las opciones de Windows que comprueba, así que no pudo saber si había una operación de instalación en curso o esperando a un reinicio, y retuvo los archivos de {InstallerFolder} que no aparecen nombrados en los registros de ningún programa instalado, en lugar de ofrecerlos. Reinicia Windows y vuelve a ejecutar el comando. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Cuando empezó el análisis, InstallerClean no pudo comprobar si Windows Installer estaba ocupado, así que retuvo los archivos de {InstallerFolder} que no aparecen nombrados en los registros de ningún programa instalado, en lugar de ofrecerlos. Reinicia Windows y vuelve a ejecutar el comando. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, because Windows gave an error: {0} (error {1}). So it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Cuando empezó el análisis, InstallerClean no pudo comprobar si Windows Installer estaba ocupado, porque Windows dio un error: {0} (error {1}). Así que retuvo los archivos de {InstallerFolder} que no aparecen nombrados en los registros de ningún programa instalado, en lugar de ofrecerlos. Reinicia Windows y vuelve a ejecutar el comando. |
| Windows Installer had something in progress or waiting when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again once that has cleared. | Windows Installer tenía algo en curso o pendiente cuando empezó el análisis, así que InstallerClean retuvo los archivos de {InstallerFolder} que no aparecen nombrados en los registros de ningún programa instalado, en lugar de ofrecerlos. Vuelve a ejecutar el comando cuando eso se haya resuelto. |
| This PC's clock went back or lost time while the scan was running, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again. | El reloj de este equipo retrocedió o se atrasó mientras se ejecutaba el análisis, así que InstallerClean retuvo los archivos de {InstallerFolder} que no aparecen nombrados en los registros de ningún programa instalado, en lugar de ofrecerlos. Vuelve a ejecutar el comando. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | Error: InstallerClean no pudo tomar el bloqueo de Windows Installer que impide que dos programas cambien el software instalado a la vez, así que no pudo descartar que un archivo pasara a ser necesario a mitad de camino. No se ha eliminado nada. Inténtalo de nuevo, y reinicia Windows si sigue ocurriendo. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. Try again, and restart Windows if it keeps happening. | Error: InstallerClean no pudo tomar el bloqueo de Windows Installer que impide que dos programas cambien el software instalado a la vez, así que no pudo descartar que un archivo pasara a ser necesario a mitad de camino. No se ha movido nada. Inténtalo de nuevo, y reinicia Windows si sigue ocurriendo. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. | Error: Windows le negó a InstallerClean el permiso para comprobar si Windows Installer estaba ocupado, así que no pudo descartar que un archivo hiciera falta a mitad del proceso. No se ha eliminado nada. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. | Error: Windows le negó a InstallerClean el permiso para comprobar si Windows Installer estaba ocupado, así que no pudo descartar que un archivo hiciera falta a mitad del proceso. No se ha movido nada. |
| Moving {0} unneeded {1} to {2}... | Moviendo {0} {1} innecesarios a {2}... |
| Moved {0} unneeded {1}. | Se movieron {0} {1} innecesarios. |
| Check that your programs still update and uninstall as normal, then delete {0}. | Comprueba que tus programas siguen actualizándose y desinstalándose con normalidad, y luego elimina {0}. |
| InstallerClean is free. If it helped, please donate $3: {0} | InstallerClean es gratis. Si te ha ayudado, dona 3 $, por favor: {0} |
| Update now? It's strongly recommended to use the latest version. [{0}/{1}] | ¿Actualizar ahora? Se recomienda encarecidamente usar la versión más reciente. [{0}/{1}] |
| Y | S |
| N | N |
| No answer after a minute, so InstallerClean updates without asking, as it does when nobody is there to answer. | Sin respuesta tras un minuto, así que InstallerClean se actualiza sin preguntar, como cuando no hay nadie para responder. |
| Send an anonymous report of this run? It is saved at {0}. Type ? to see what it holds. [{1}/{2}/?] | ¿Enviar un informe anónimo de esta ejecución? Está guardado en {0}. Escribe ? para ver qué contiene. [{1}/{2}/?] |
| the chart at {0} | este gráfico en {0} |
| The report adds your result to {0}, which shows how much space people are reclaiming with InstallerClean. It also holds these technical details, to help me spot any bugs: | El informe añade tu resultado a {0}, que muestra cuánto espacio están recuperando las personas con InstallerClean. También incluye estos datos técnicos, que me ayudan a detectar errores: |
| The full report is printed below. | El informe completo se muestra a continuación. |
| Report sent. Thank you. | Informe enviado. Gracias. |
| Report not sent. | Informe no enviado. |
| No answer after a minute. | Sin respuesta tras un minuto. |
| The report could not be sent. It is kept in {0}. | No se pudo enviar el informe. Se conserva en {0}. |
| Sending a report left waiting by an earlier run. | Se envía un informe que esperaba desde una ejecución anterior. |
| The new version will scan and list what it finds, without deleting anything. To delete, run /d again afterwards. | La nueva versión analizará y mostrará lo que encuentre, sin eliminar nada. Para eliminar, vuelve a ejecutar /d después. |
| The new version will scan and list what it finds, without moving anything. To move, run /m again afterwards. | La nueva versión analizará y mostrará lo que encuentre, sin mover nada. Para mover, vuelve a ejecutar /m después. |
| InstallerClean is in use, so it can't update itself now. | InstallerClean está en uso, así que no puede actualizarse ahora. |
| Running {0} with InstallerClean {1} instead. | En su lugar, se ejecuta {0} con InstallerClean {1}. |
| The newer version couldn't be run. | No se pudo ejecutar la versión más reciente. |
| Running {0} with InstallerClean {1}. It takes this copy's place once the run ends. | Se ejecuta {0} con InstallerClean {1}. Ocupará el lugar de esta copia cuando termine la ejecución. |
| {0}: {1} | {0}: {1} |
| It needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then run InstallerClean again to update. | Esta versión necesita .NET Framework 4.8. Instálalo desde Microsoft, reinicia el PC y luego vuelve a ejecutar InstallerClean para actualizar. |
| It's simple to undo. Move them back from {0} into {InstallerFolder} and everything will be back to how it was. | Deshacerlo es sencillo. Muévelos de vuelta desde {0} a {InstallerFolder} y todo quedará como estaba. |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then run the command again. | InstallerClean ya no pudo confirmar la carpeta de copia de seguridad, así que se detuvo. Comprueba {0} y vuelve a ejecutar el comando. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Otro proceso de InstallerClean mantiene el bloqueo de instancia única (la GUI u otra ejecución de la CLI). Código de salida 75 (transitorio); es seguro reintentar más tarde. |
| Stopped after deleting {0} of {1} {2}: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 2 (partial). | Detenido tras eliminar {0} de {1} {2}: InstallerClean ya no podía tener la certeza de que ningún otro InstallerClean estuviera actuando. Vuelve a ejecutar el mismo comando. Código de salida 2 (parcial). |
| Stopped after moving {0} of {1} {2} to {3}: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 2 (partial). | Detenido tras mover {0} de {1} {2} a {3}: InstallerClean ya no podía tener la certeza de que ningún otro InstallerClean estuviera actuando. Vuelve a ejecutar el mismo comando. Código de salida 2 (parcial). |
| Stopped before deleting any file: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 75 (transient). | Detenido antes de eliminar ningún archivo: InstallerClean ya no podía tener la certeza de que ningún otro InstallerClean estuviera actuando. Vuelve a ejecutar el mismo comando. Código de salida 75 (transitorio). |
| Stopped before moving any file: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 75 (transient). | Detenido antes de mover ningún archivo: InstallerClean ya no podía tener la certeza de que ningún otro InstallerClean estuviera actuando. Vuelve a ejecutar el mismo comando. Código de salida 75 (transitorio). |
| InstallerClean needs administrator rights to see every program installed on this PC, and it isn't running as administrator, so it won't scan. Nothing has been changed. Run it again from an administrator prompt. | InstallerClean necesita permisos de administrador para ver todos los programas instalados en este PC, y no se está ejecutando como administrador, así que no hará el análisis. No se ha cambiado nada. Vuelve a ejecutarlo desde un símbolo del sistema como administrador. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Nota: error al escribir en el registro de eventos. Comprueba los permisos del registro Aplicación o las directivas de grupo. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - limpiar {InstallerFolder} |
| Removes cached .msi and .msp files that no installed program still needs. | Quita archivos .msi/.msp en caché que ningún programa instalado necesita. |
| Needs an elevated (administrator) prompt and won't scan without one. | Requiere símbolo del sistema como administrador; sin él no analiza. |
| Usage: | Uso: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     Muestra esta ayuda (acepta también /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  Muestra la versión (acepta también -v) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         Solo analizar - lista los innecesarios |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         Elimina definitivamente los innecesarios |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         Mueve a la carpeta de copia de seguridad |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m RUTA    Mueve a la ruta especificada |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli bloquea el símbolo del sistema hasta terminar,<br>para que un script o una tarea programada pueda esperarlo. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | Carpeta por usuario; ejecuciones programadas o SYSTEM: /m RUTA. |
| Before it scans, it checks for a newer version of InstallerClean.<br>When you run it yourself, it asks before updating, and updates if nobody<br>answers within a minute. When it runs on its own, in a scheduled task or<br>a script, it updates without asking.<br>Add /noupdate to skip the check. | Antes de analizar, comprueba si hay una versión más reciente de<br>InstallerClean. Cuando lo ejecutas tú, pregunta antes de actualizar, y se<br>actualiza si nadie responde en un minuto. Cuando se ejecuta solo, en una<br>tarea programada o un script, se actualiza sin preguntar. Añade /noupdate<br>para omitir la comprobación. |
| Exit codes: | Códigos de salida: |
|   0   success: the run did what it was asked |   0   correcto: hizo lo que se le pidió |
|   1   nothing processed: the run failed or was refused |   1   nada procesado: la ejecución falló o fue rechazada |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   parcial: unos procesados y otros no (un fallo o un Ctrl+C) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  transitorio: algo temporal bloqueó la ejecución (ver el mensaje) |
|   130 cancelled (Ctrl+C) |   130 cancelado (Ctrl+C) |
