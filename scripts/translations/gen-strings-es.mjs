#!/usr/bin/env node
// Spanish (es) satellite generator for InstallerClean. Same proven pattern as
// gen-strings-de.mjs: reads the neutral Strings.resx as the structural base,
// strips ONLY the machine-contract Cli.* keys, replaces every other key's
// <value> from MAP, appends the satellite-only .One overrides, writes LF/UTF-8
// and self-verifies against the neutral.
//
// Spanish plural class: One only at n==1, else Other (same selector as de/it).
// Spanish past participles DO inflect for number (encontrado/encontrados,
// eliminado/eliminados, movido/movidos), so the three CLI completion lines carry
// .One overrides; the gerund progress lines (Moviendo/Eliminando) do not inflect
// and need none. Status.RegisteredPackagesFound also overrides for the
// registrado/registrados adjective agreement. The four held-back sentences carried
// .One overrides too, for the conservado/conservados participle, and went with the
// sentences themselves when the 3.0.0 round replaced all four with one
// Completion.HeldBack pair. The
// three Completion.*CancelledSummary lines deliberately do NOT: their form is
// picked by the total while the leading count varies, so they use the
// count-invariant "Cancelaste tras <infinitive>" frame that reads right at any count.
//
// MAP, OVERRIDES and ALSO_KEEP are derived byte-for-byte from the committed
// Strings.es.resx, so regenerating reproduces it: \\ is one backslash (paths),
// \n is a real newline (multi-line values), {0}/{1} are .NET placeholders, and
// &#10; is the XML entity written literally where the neutral uses it.
import { readFileSync, writeFileSync } from 'node:fs';

const dir = 'src/InstallerClean.Core/Resources';
const BASE = `${dir}/Strings.resx`;            // English source (the "neutral")
const OUT = `${dir}/Strings.es.resx`;

// Universal keeps: keys whose value is the same in every language, the brand names
// and the pure-placeholder announcement string. Their still-English value is NOT a
// miss. Explicit by KEY on purpose: a future brand key then defaults to "flag until
// someone adds it here", never silently passes. Do NOT translate these values. Do
// NOT edit this list per language.
//
// The four size suffixes and the two elapsed suffixes do not belong in this list,
// because they are not universal: French writes Go/Mo/Ko/o, Russian and Ukrainian
// write ГБ/МБ/КБ/Б and мс/с. Those three carry real
// values in their MAP; the languages that do abbreviate as English does keep them in
// ALSO_KEEP, which is the per-language list. Display.ListSeparator is the same shape,
// for the same reason.
const KEEP_ENGLISH = new Set([
  'Window.Main.Title',
  'Startup.AlreadyRunningTitle',
  'Startup.UnhandledTitle',
  'Automation.ScanResultAnnouncement',
]);

// Per-language keeps: Spanish words byte-identical to the English source that are
// the correct, natural Spanish term ("error" = error), so the still-"English"
// value is not a miss.
const ALSO_KEEP = [
  'Plural.Error.Singular',
  // The list separator Spanish uses is the one English uses. A punctuation
  // mark rather than a word, so there is nothing to translate and nothing to
  // get wrong; only ja and zh-Hans differ, taking the ideographic comma.
  'Display.ListSeparator',       // ", "
  // The size and elapsed unit suffixes. Spanish abbreviates them exactly as
  // English does, so there is nothing to translate and nothing to get wrong.
  // A per-language keep rather than a universal one because fr, ru and uk do
  // NOT: French takes Go/Mo/Ko/o, Russian and Ukrainian take ГБ/МБ/КБ/Б and
  // мс/с, and all three carry real values in their MAP.
  'Display.Size.GB',           // {0:F2} GB
  'Display.Size.MB',           // {0:F1} MB
  'Display.Size.KB',           // {0:F1} KB
  'Display.Size.B',            // {0} B
  'Display.Elapsed.Ms',        // {0:F0}ms
  'Display.Elapsed.S',         // {0:F1}s
];

// Satellite-only .One override(s). NOT in the neutral; appended before </root>.
// check-resx-parity.mjs allows each because its base key is in the neutral.
const OVERRIDES = {
  'Status.RegisteredPackagesFound.One': `Se encontró {0} {1} registrado.`,
  'Cli.FoundOrphans.One': `Se encontró {0} {1} innecesario para limpiar ({2}).`,
  'Cli.DeletingFiles.One': `Eliminando {0} {1} innecesario...`,
  'Cli.DeletedFiles.One': `Se eliminó definitivamente {0} {1} innecesario.`,
  'Cli.MovingFiles.One': `Moviendo {0} {1} innecesario a {2}...`,
  'Cli.MovedFiles.One': `Se movió {0} {1} innecesario.`,
  // Completion.ReverifyIdentityUnreadable.One was added and removed again in the 3.0.0 round. Its base is
  // one of the two retired identity causes: no code reads it, so nothing passes
  // the prefix to Pluralise and the override could never be selected.
  // CountedStringTests.Every_satellite_override_belongs_to_a_counted_prefix is
  // what says so. The base string itself stays translated, which is the point of
  // keeping those two keys at all.
};

const MAP = {
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `Acerca de`,
  'Window.Registered.Title': `Archivos que se han dejado en paz`,
  'Window.Orphaned.Title': `Archivos innecesarios que puedes eliminar sin riesgo`,
  // Section.Registered.Products and Automation.Section.Products are deliberately
  // not here. They left the neutral resx when the registered-files window stopped
  // having a products group of its own, so a value for either would be
  // regenerated into a key the app cannot use.
  'Section.Registered.Patches': `PARCHES`,
  'Section.Registered.Details': `DETALLES DEL PRODUCTO`,
  'Section.Backup.Folder': `CARPETA DE COPIA DE SEGURIDAD`,
  'Section.SayThanks': `DAR LAS GRACIAS`,
  'Field.Reason': `Motivo`,
  'Field.Author': `Autor`,
  'Field.Application': `Aplicación`,
  'Field.Title': `Título`,
  'Field.Subject': `Asunto`,
  'Field.Keywords': `Palabras clave`,
  'Field.SigningCertificate': `Certificado de firma`,
  'Field.FileSize': `Tamaño del archivo`,
  'Field.Comment': `Comentario`,
  'Field.ProductName': `Nombre del producto`,
  'Field.File': `Archivo`,
  'Field.Size': `Tamaño`,
  'Field.Patches': `Parches`,
  'Field.UnknownProductName': `(desconocido)`,
  'Field.PatchesOnly': `(solo parches)`,
  'Field.Missing': `ausente`,
  'Action.About': `_Acerca de`,
  'Action.Copy': `Copiar`,
  'Action.Cut': `Cortar`,
  'Action.Paste': `Pegar`,
  'Action.SelectAll': `Seleccionar todo`,
  'Action.Browse': `E_xaminar...`,
  'Action.Cancel': `_Cancelar`,
  'Action.CheckForUpdates': `_Buscar actualizaciones`,
  'Action.Close': `_Cerrar`,
  'Action.DeletePermanently': `_Eliminar definitivamente`,
  'Action.Done': `_Listo`,
  'Action.Details': `Detalles`,
  'Action.BuyMeACuppa': `_Invítame a un café`,
  'Action.LeaveStarOnGitHub': `Deja una e_strella en GitHub`,
  'Action.Licence': `Licencia Apache 2.0`,
  'Action.Move': `_Mover`,
  'Action.BackupFolderPlaceholder': `Ruta a la carpeta si mueves en lugar de eliminar.`,
  'Action.OpenReleasePage': `Abrir la página de la _versión`,
  'Action.Rescan': `_Volver a analizar`,
  'Action.ScanAgain': `Analizar de _nuevo`,
  'Action.SendResultLog': `Enviar informe`,
  'Action.SendResultLogConfirm': `_Enviar`,
  'Automation.BuyMeACuppa': `Donar`,
  'Automation.BuyMeACuppa.About': `Invítame a un café`,
  'Automation.CancelOperation': `Cancelar la operación`,
  'Automation.CancelScan': `Cancelar el análisis`,
  'Automation.CancelStartupScan': `Cancelar el análisis de inicio`,
  'Automation.Close': `Cerrar`,
  'Automation.CloseWindow': `Cerrar la ventana`,
  'Automation.CloseResult': `Cerrar el resultado y volver a la ventana principal`,
  'Automation.LeaveStarOnGitHub.About': `Deja una estrella en github`,
  'Automation.Minimise': `Minimizar`,
  'Automation.ConfirmDelete': `Eliminar definitivamente quita los archivos innecesarios. Cancelar cierra sin eliminar nada.`,
  'Automation.ConfirmMove': `Mover coloca los archivos innecesarios en la carpeta de destino elegida. Cancelar los deja donde están.`,
  'Automation.SayThanks': `Dar las gracias`,
  'Automation.ConfirmSendResultLog': `Enviar transmite a No Faff el informe mostrado. Cancelar no envía nada.`,
  'Automation.CheckForUpdates': `Buscar actualizaciones`,
  'Automation.CheckForUpdates.HelpText': `Consulta la página de versiones de github en busca de una versión más reciente.`,
  'Automation.UpdateAvailable.HelpText': `Abre la página de la versión para descargar la más reciente, o cancela para conservar la actual.`,
  'Automation.Licence.HelpText': `Abre el archivo de la licencia en github.com en tu navegador.`,
  'Automation.Section.BackupFolder': `Carpeta de copia de seguridad`,
  'Automation.Section.Patches': `Parches`,
  'Automation.Section.ProductDetails': `Detalles del producto`,
  'Automation.BackupFolder': `Carpeta de copia de seguridad`,
  'Automation.OperationProgress': `Progreso de la operación`,
  'Automation.RescanInstaller': `Volver a analizar {InstallerFolder}`,
  'Automation.ScanningProgress': `Progreso del análisis`,
  'Automation.StartupScanProgress': `Progreso del análisis de inicio`,
  'Automation.ViewOrphanedFiles': `Detalles, archivos innecesarios`,
  'Automation.ViewOrphanedFiles.HelpText': `Disponibles para limpiar.`,
  'Automation.ViewRegisteredFiles': `Detalles, archivos que se han dejado en paz`,
  'Automation.ViewRegisteredFiles.HelpText': `Inventario de solo lectura.`,
  'Automation.SortStatus.Ascending': `Ordenado por {0}, ascendente`,
  'Automation.SortStatus.Descending': `Ordenado por {0}, descendente`,
  'Automation.Scroll.ScanResults': `Resultados del análisis`,
  'Automation.Scroll.ResultDetails': `Detalles del resultado`,
  'Automation.Scroll.FileDetails': `Detalles del archivo`,
  'Automation.Scroll.DialogBody': `Texto del cuadro de diálogo`,
  'Automation.ScanResultAnnouncement': `{0} ({1})`,
  'Automation.CompletionErrors': `Archivos que no se pudieron procesar`,
  'Automation.RegisteredMissingSeeAlso': `Explica esta carpeta, y cómo recuperar un archivo, en el README`,
  'Tooltip.BuyMeACuppa.About': `¡Esto da sed!`,
  'Tooltip.CancellingPending': `Cancelación solicitada. InstallerClean está esperando a que el paso en curso llegue a un punto en el que pueda detenerse. Puede tardar unos segundos durante operaciones intensas de entrada/salida o una llamada a la base de datos MSI.`,
  'Tooltip.Close': `Cerrar`,
  'Tooltip.LeaveStarOnGitHub.About': `Una estrella ayuda a otras personas a encontrar InstallerClean.`,
  'Tooltip.Minimise': `Minimizar`,
  'Tooltip.SendResultLog': `Tú decides, pero se agradece. Envía un resumen anónimo que solo sirve para que yo sepa si funciona y cuánto espacio libera la gente. La pantalla siguiente te muestra lo que se enviará antes de confirmar.`,
  'Tooltip.SendResultLog.NothingFound': `Tú decides, pero se agradece. Envía un resumen anónimo que solo sirve para que yo sepa si funciona. La pantalla siguiente te muestra lo que se enviará antes de confirmar.`,
  'Tooltip.Move': `Mueve los archivos innecesarios a la carpeta de copia de seguridad.`,
  'Tooltip.MoveNeedsDestination': `Mueve los archivos innecesarios a una carpeta de copia de seguridad. La elegirás a continuación.`,
  'Tooltip.Delete': `Elimina definitivamente los archivos innecesarios. Usa Mover en su lugar si quieres la oportunidad de convencerte de que todo va bien.`,
  'Tooltip.SigningCertificate': `Nombre del firmante del certificado Authenticode incorporado. La cadena no está verificada.`,
  'Body.MainExplanation.Lead': `Cualquier archivo innecesario de los de abajo se puede [eliminar sin riesgo].`,
  'Body.MainExplanation.Why': `Están en {InstallerFolder}. InstallerClean pregunta a Windows por cada programa instalado: un archivo aparece en la lista cuando ningún programa lo reclama ({0}), o cuando un parche más nuevo lo ha sustituido y ningún programa podría volver atrás hasta él ({1}).`,
  'Body.MainExplanation.Action': `Muévelos a una carpeta de copia de seguridad que elijas y luego elimina esa carpeta cuando estés convencido de que tus programas siguen actualizándose y desinstalándose con normalidad. Devolverlos a {InstallerFolder} lo restaura todo. O elimínalos definitivamente ahora.`,
  'Body.PendingReboot.MsiExecuteMutex': `Algo está usando Windows Installer en este momento, como una actualización de Windows o un programa instalándose en segundo plano. Mover y Eliminar están en pausa mientras eso ocurre, así que InstallerClean no tocará {InstallerFolder} mientras cambia. Cuando termine, vuelve a analizar y estarán de nuevo disponibles.`,
  'Body.PendingReboot.InstallerInProgress': `En este equipo hay una transacción anterior de Windows Installer en suspenso. Reanuda o deshaz esa instalación (o reinicia Windows) antes de limpiar {InstallerFolder}.`,
  'Body.PendingReboot.PendingRenameInCache': `Windows tiene en cola para el próximo reinicio un cambio de nombre de archivo que afecta a {InstallerFolder}. Reinicia Windows antes de limpiar.`,
  'Body.NoFileSelected': `Selecciona un archivo para ver sus detalles.`,
  'Body.NoProductSelected': `Selecciona un producto para ver sus detalles.`,
  'Body.NoMetadata': `No hay metadatos disponibles.`,
  'Body.RegisteredMissingFromDisk': `Falta este archivo de instalación. Ahora mismo no causa ningún problema, y no lo causará hasta el día en que intentes actualizar o desinstalar el programa al que pertenece. Ese paso puede fallar entonces, porque Windows busca este archivo y no está.\n\nPara reponerlo, necesitas el instalador de la versión que ya tienes. Consíguelo del fabricante del programa y ejecútalo sobre tu copia actual. Una versión más nueva no sirve: tendría que quitar primero la que tienes, y ese es justo el paso que necesita este archivo. Desinstalar primero tampoco funciona, por la misma razón. Esto debería restaurar el archivo y dejar tu configuración intacta, pero Microsoft no lo garantiza.`,
  'Body.RegisteredMissingFromDisk.SeeAlso': `El README [explica esta carpeta], y cómo recuperar un archivo, con las propias palabras de Microsoft.`,
  'Body.NoPatches': `(ninguno)`,
  'Reason.Orphaned': `Huérfano`,
  'Reason.Superseded': `Sustituido`,
  'Reason.Obsoleted': `Obsoleto`,
  'Status.Scanning': `Analizando...`,
  'Status.Cancelling': `Cancelando...`,
  'Status.StartingScan': `Iniciando el análisis...`,
  'Status.QueryingApi': `Consultando a Windows el software instalado...`,
  'Status.ScanningCache': `Analizando la carpeta de la caché de instalación...`,
  'Status.EnumeratingProducts': `Enumerando los productos instalados...`,
  'Status.CheckingRegistry': `Comprobando el registro en busca de paquetes adicionales...`,
  'Status.RegisteredPackagesFound': `Se encontraron {0} {1} registrados.`,
  'Status.ScanComplete': `Análisis completado ({0})`,
  'Status.FoundProducts': `Analizando los paquetes locales...`,
  'Status.FoundUnused': `Se encontraron {0} {1} que puedes eliminar sin riesgo.`,
  'Status.PreparingDestination': `Preparando la carpeta de destino...`,
  'Status.Moving': `Moviendo archivos innecesarios...`,
  'Status.Deleting': `Eliminando archivos innecesarios...`,
  'Status.MoveCancelled.Partial': `Movimiento cancelado tras procesar {0} de {1} {2}.`,
  'Status.DeleteCancelled.Partial': `Eliminación cancelada tras procesar {0} de {1} {2}.`,
  'Status.MoveFailed': `Movimiento fallido ({0}). Detalles en {1}.`,
  'Status.MoveFailed.NoLog': `Movimiento fallido ({0}). No se pudo escribir el archivo crash.log.`,
  'Status.DeleteFailed': `Eliminación fallida ({0}). Detalles en {1}.`,
  'Status.DeleteFailed.NoLog': `Eliminación fallida ({0}). No se pudo escribir el archivo crash.log.`,
  'Status.ScanAccessDenied': `Acceso denegado. Windows rechazó el análisis.`,
  'Status.ScanFailedDb': `Análisis fallido: no se pudieron leer los registros de Windows Installer.`,
  'Status.ScanCancelled': `Análisis cancelado.`,
  'Status.Done': `Listo`,
  'Status.ScanFailedDetails': `Análisis fallido ({0}). Detalles en {1}.`,
  'Status.ScanFailedDetails.NoLog': `Análisis fallido ({0}). No se pudo escribir el archivo crash.log.`,
  'Completion.AllClean': `Todo limpio`,
  'Completion.NothingToCleanUp': `Nada que limpiar en {InstallerFolder}`,
  'Completion.NothingToCleanUpReceipt': `Análisis de {0} {1} en {2}`,
  'Completion.Freed': `{0} liberados`,
  'Completion.Moved': `{0} movidos`,
  // Heading for the outcome where the operation acted on no file at all, then
  // the failure count line beneath it: 0 = files that failed, 1 = files tried
  // (succeeded + failed). The count selecting the form is {0}; the noun belongs
  // to {1}, so treat the two independently.
  'Completion.NothingMoved': `No se movió nada`,
  'Completion.NothingDeleted': `No se eliminó nada`,
  'Completion.FailedCount.Singular': `No se pudo mover {0} archivo de {1}.`,
  'Completion.FailedCount.Plural': `No se pudieron mover {0} archivos de {1}.`,
  'Completion.FailedCountDelete.Singular': `No se pudo eliminar {0} archivo de {1}.`,
  'Completion.FailedCountDelete.Plural': `No se pudieron eliminar {0} archivos de {1}.`,
  'Completion.MoveSummary.Singular': `{0} {1} movido a: {2}`,
  'Completion.MoveSummary.Plural': `{0} {1} movidos a: {2}`,
  'Completion.PermanentDeleteSummary.Singular': `{0} {1} eliminado definitivamente`,
  'Completion.PermanentDeleteSummary.Plural': `{0} {1} eliminados definitivamente`,
  'Summary.RegisteredStillUsed.Singular': `{0} archivo dejado en paz`,
  'Summary.RegisteredStillUsed.Plural': `{0} archivos dejados en paz`,
  'Summary.OrphanedToCleanUp.Singular': `{0} archivo innecesario para limpiar`,
  'Summary.OrphanedToCleanUp.Plural': `{0} archivos innecesarios para limpiar`,
  'Summary.NothingListed.Singular': `En este PC InstallerClean no pudo determinar con certeza qué archivos almacenados en caché pertenecen a los programas instalados aquí, así que ha retenido el único archivo en lugar de mostrarlo en la lista.`,
  'Summary.NothingListed.Plural': `En este PC InstallerClean no pudo determinar con certeza qué archivos almacenados en caché pertenecen a los programas instalados aquí, así que ha retenido {0} {1} en lugar de mostrarlos en la lista.`,
  'Summary.MissingFromDisk.Singular': `Windows tiene un registro de {0} archivo que no está en {InstallerFolder}: {1}. En el día a día no causa problemas, pero una actualización o desinstalación de ese programa puede fallar. Abre Detalles para saber qué hacer.`,
  'Summary.MissingFromDisk.Plural': `Windows tiene registros de {0} archivos que no están en {InstallerFolder}: {1}. En el día a día no causan problemas, pero una actualización o desinstalación de esos programas puede fallar. Abre Detalles para saber qué hacer.`,
  'Summary.MissingFromDisk.OtherPrograms.Singular': `{0} programa más`,
  'Summary.MissingFromDisk.OtherPrograms.Plural': `{0} programas más`,
  'Summary.MissingFromDisk.Unnamed.Singular': `{0} archivo sin ningún programa nombrado en los registros`,
  'Summary.MissingFromDisk.Unnamed.Plural': `{0} archivos sin ningún programa nombrado en los registros`,
  'Summary.OperationFiles': `{0} de {1} {2}`,
  'Summary.OrphanedWindow': `{0} {1} para limpiar ({2})`,
  'Summary.RegisteredWindow.Singular': `{0} archivo dejado en paz ({1})`,
  'Summary.RegisteredWindow.Plural': `{0} archivos dejados en paz ({1})`,
  'Confirm.MoveTitle': `¿Mover {0} {1} ({2})?`,
  'Confirm.DeleteTitle': `¿Eliminar {0} {1} ({2})?`,
  'Error.AdminRequiredTitle': `Acceso denegado`,
  'Error.AdminRequiredBody': `Windows le negó el acceso a InstallerClean, así que se detuvo. No se ha eliminado nada.\n\nInstallerClean ya se estaba ejecutando como administrador, así que volver a iniciarlo de esa forma no servirá de nada. Windows no dice nada más sobre qué denegó el acceso, así que no hay nada concreto que puedas probar.`,
  'Error.InstallerDbUnavailableTitle': `No se pudieron leer los registros de Windows Installer`,
  'Error.ScanFailedTitle': `Análisis fallido`,
  'Error.InstallerDbEmpty': `Los registros de Windows Installer llegaron completamente vacíos: ni un solo programa instalado ni una sola actualización reclama un archivo de instalación en caché. Eso no ocurre en un equipo que funciona (incluso una instalación nueva de Windows tiene alguno), así que o los registros están dañados o no se pudieron leer, y un análisis que se creyera esta respuesta marcaría por error como huérfano cada archivo de {InstallerFolder}. InstallerClean se detuvo en lugar de eso. No se ha eliminado nada.`,
  'Error.MsiAccessDenied': `Windows Installer no dejó que InstallerClean enumerara lo que hay instalado. InstallerClean ya se estaba ejecutando como administrador, así que volver a ejecutarlo como administrador no cambiará nada. Sin esa lista no hay forma segura de saber qué archivos en caché siguen haciendo falta, así que InstallerClean se detuvo. No se ha eliminado nada.`,
  'Error.MsiNonSuccess': `Windows Installer no pudo darle a InstallerClean una lista legible de los programas instalados: leyó {2} {3} y luego {0} entradas seguidas llegaron ilegibles (último código de error {1}). En lugar de trabajar con una lista leída a medias, InstallerClean se detuvo. No se ha eliminado nada.`,
  'Error.InvalidDestinationTitle': `Destino no válido`,
  'Error.DestinationWriteFailedTitle': `No se pudo escribir en el destino`,
  'Error.MoveFailedTitle': `Movimiento fallido`,
  'Error.DeleteFailedTitle': `Eliminación fallida`,
  'Error.SettingNotSavedTitle': `Ajuste no guardado`,
  'Error.SettingNotSavedBody': `No se pudo guardar el cambio. La próxima vez que se inicie, InstallerClean volverá al ajuste anterior.`,
  'Error.DestinationInsideInstaller': `El destino no puede estar dentro de la carpeta de Windows Installer.`,
  'Error.DestinationInSystemFolder': `El destino {0} se resuelve dentro de una carpeta del sistema de Windows. Elige una ruta fuera de %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% y %ProgramData%.`,
  'Error.NotEnoughSpaceTitle': `Espacio insuficiente`,
  'Error.NotEnoughSpaceBody': `Espacio insuficiente en {0}\n\nNecesario: {1}\nDisponible: {2}`,
  'Error.AccessDeniedDestination': `No tienes permiso para escribir en {0}.\nPrueba con una carpeta de tu perfil de usuario o en una unidad de tu propiedad.`,
  'Error.PathTooLong': `La ruta {0} es demasiado larga para Windows. Elige una ruta más corta.`,
  'Error.DestinationMissing': `La carpeta {0} no existe y no se pudo crear. Comprueba la letra de la unidad o la ruta de red.`,
  'Error.IOWriteDestination': `Windows no puede escribir en {0}.\nDetalles en {1}.`,
  'Error.IOWriteDestination.NoLog': `Windows no puede escribir en {0}. No se pudo escribir el archivo crash.log.`,
  'Error.WriteDestination': `No se puede escribir en {0}.\nDetalles en {1}.`,
  'Error.WriteDestination.NoLog': `No se puede escribir en {0}. No se pudo escribir el archivo crash.log.`,
  'Error.MissingSourceFile': `El archivo ya no existe.`,
  'Error.SourceIsReparsePoint': `El archivo de origen es un enlace simbólico o un punto de unión; rechazado por seguridad.`,
  // Singular = the sentence for ONE file, which the CLI prints after "filename: ";
  // plural = the heading the completion overlay puts over a list of filenames.
  'Error.AccessDenied.Singular': `Windows denegó el acceso a este archivo; se dejó donde estaba.`,
  'Error.AccessDenied.Plural': `Windows denegó el acceso a estos archivos; se dejaron donde estaban.`,
  'Error.FileInUse.Singular': `Este archivo está abierto o bloqueado por otro programa, así que ahora mismo nada puede quitarlo. Se ha dejado en su sitio; inténtalo más tarde.`,
  'Error.FileInUse.Plural': `Estos archivos están abiertos o bloqueados por otro programa, así que ahora mismo nada puede quitarlos. Se han dejado en su sitio; inténtalo más tarde.`,
  'Error.IOFailure.Singular': `Windows informó de un error de archivo; el archivo se dejó donde estaba.`,
  'Error.IOFailure.Plural': `Windows informó de errores de archivo; estos archivos se dejaron donde estaban.`,
  'Error.UnknownError.Singular': `Algo salió mal con este archivo; se dejó donde estaba.`,
  'Error.UnknownError.Plural': `Algo salió mal con estos archivos; se dejaron donde estaban.`,
  'Error.MoveIntoInstaller': `Se rechaza mover archivos a la carpeta de Windows Installer (destino: {0}).`,
  'Error.DestinationNotFullyQualified': `La carpeta de copia de seguridad tiene que ser una ruta completa a una carpeta, empezando por una letra de unidad o un recurso compartido de red (por ejemplo D:\\Backup, o \\\\servidor\\backup). InstallerClean no puede usar esta: {0}`,
  'BrowserLaunch.FailedTitle': `No se pudo abrir el navegador`,
  'UpdateCheck.Title': `Buscar actualizaciones`,
  'UpdateCheck.Status.Checking': `Buscando...`,
  'UpdateCheck.Status.UpToDate': `Estás al día.`,
  'UpdateCheck.UpdateAvailable.Title': `Actualización disponible`,
  'UpdateCheck.UpdateAvailable.Body': `Estás usando la versión {0}.&#10;Está disponible la versión {1}.`,
  'UpdateCheck.Failed.NetworkUnavailable': `No se pudo conectar con GitHub. Comprueba tu conexión a internet y vuelve a intentarlo.`,
  'UpdateCheck.Failed.ServerError': `GitHub devolvió una respuesta de error. Vuelve a intentarlo en unos minutos.`,
  'UpdateCheck.Failed.ResponseParseError': `La respuesta de GitHub no contenía una versión reconocible. Vuelve a intentarlo más tarde, o abre directamente la página de versiones.`,
  'UpdateCheck.Failed.Timeout': `Se agotó el tiempo de espera de la comprobación. Tu conexión con GitHub puede ser lenta; vuelve a intentarlo.`,
  'UpdateCheck.Failed.Unknown': `La comprobación falló por un motivo desconocido. Los detalles están en crash.log por si necesitas informar del problema.`,
  'BrowserLaunch.ClipboardOk': `The link is on your clipboard, so you can paste it in yourself:&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean couldn't copy the link to your clipboard either, so here it is:&#10;&#10;{0}`,
  'Error.DestinationChangedMidBatch': `InstallerClean ya no pudo confirmar la carpeta de copia de seguridad, así que se detuvo en lugar de escribir en el sitio equivocado. Comprueba {0}, luego Volver a analizar e inténtalo de nuevo.`,
  'Error.CannotWriteFolder': `No se puede escribir en {0}.`,
  'Error.DestinationCollision': `Ya hay un archivo llamado '{0}' en la carpeta de copia de seguridad.`,
  'ResultLog.Sending': `Enviando...`,
  'ResultLog.Sent': `¡Gracias! Informe enviado.`,
  'ResultLog.Failed': `Envío fallido. Inténtalo de nuevo más tarde.`,
  'ResultLog.NothingToSend': `No hay ningún informe que enviar.`,
  'ConfirmSendResultLog.Title': `¿Enviar esto?`,
  'ConfirmSendResultLog.Reassurance': `Se envía a nofaff.netlify.app/api/result-log. Nada te identifica a ti ni a tu equipo; solo sirve para que yo sepa que InstallerClean funciona y [cuánto espacio libera la gente].`,
  'Automation.ResultLogPreview': `Vista previa del informe`,
  'Startup.AlreadyRunningTitle': `InstallerClean`,
  'Startup.AlreadyRunningBody': `InstallerClean ya se está ejecutando.`,
  'Startup.UnhandledTitle': `InstallerClean`,
  'Startup.UnhandledBody': `Se produjo un error inesperado e InstallerClean debe cerrarse.\n\n{0}\n\nDetalles guardados en:\n{1}`,
  'Startup.UnhandledBody.NoLog': `Se produjo un error inesperado e InstallerClean debe cerrarse.\n\n{0}\n\nNo se pudo escribir el archivo crash.log.`,
  'Startup.ErrorTitle': `Error de inicio`,
  'Startup.FailedToStart': `No se pudo iniciar ({0}). Detalles guardados en:\n{1}`,
  'Startup.FailedToStart.NoLog': `No se pudo iniciar ({0}). No se pudo escribir el archivo crash.log.`,
  'FilePicker.ChooseDestinationTitle': `Elige la carpeta de destino para los archivos movidos`,
  'Version.Display': `Versión {0}`,
  'Plural.File.Singular': `archivo`,
  'Plural.File.Plural': `archivos`,
  'Plural.Error.Singular': `error`,
  'Plural.Error.Plural': `errores`,
  'Plural.Package.Singular': `paquete`,
  'Plural.Package.Plural': `paquetes`,
  'Plural.Product.Singular': `producto`,
  'Plural.Product.Plural': `productos`,
  'Plural.Patch.Singular': `parche`,
  'Plural.Patch.Plural': `parches`,
  'Display.Size.GB': `{0:F2} GB`,
  'Display.Size.MB': `{0:F1} MB`,
  'Display.Size.KB': `{0:F1} KB`,
  'Display.Size.B': `{0} B`,
  'Display.Elapsed.Ms': `{0:F0}ms`,
  'Display.Elapsed.S': `{0:F1}s`,
  'Display.ListSeparator': `, `,
  'Display.ElapsedLong.LessThanASecond': `menos de un segundo`,
  'Display.ElapsedLong.Seconds': `{0:F1} segundos`,
  'Cli.UnknownArgument': `Error: argumento desconocido '{0}'`,
  'Cli.Cancelling': `Cancelando...`,
  'Cli.Cancelled': `Operación cancelada.`,
  'Cli.GenericError': `Error: fallo inesperado ({0}). Detalles escritos en {1}.`,
  'Cli.GenericError.NoLog': `Error: fallo inesperado ({0}). No se pudo escribir el registro de fallos.`,
  'Cli.ScanningInstaller': `Analizando {InstallerFolder}...`,
  'Cli.FoundOrphans': `Se encontraron {0} {1} innecesarios para limpiar ({2}).`,
  'Cli.DeletingFiles': `Eliminando {0} {1} innecesarios...`,
  'Cli.DeletedFiles': `Se eliminaron definitivamente {0} {1} innecesarios.`,
  'Cli.NoMoveDestination': `Error: no se ha especificado un destino para mover. Usa /m RUTA. (Una ubicación predeterminada configurada en la GUI se guarda por usuario y no se aplica a las ejecuciones programadas ni a las de cuenta de servicio.)`,
  'Cli.MoveDestinationInsideInstaller': `Error: el destino no puede estar dentro de la carpeta de Windows Installer.`,
  'Cli.MoveDestinationRelative': `Error: el destino debe ser una ruta completa. Recibido: {0}`,
  'Cli.MoveDestinationInSystemFolder': `Error: el destino {0} se resuelve dentro de una carpeta del sistema de Windows. Elige una ruta fuera de %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% y %ProgramData%.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Error: algo está usando Windows Installer en este momento, como una actualización de Windows o un programa instalándose en segundo plano. /m y /d están bloqueados mientras eso ocurre. Inténtalo de nuevo cuando termine.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Error: en este equipo hay una transacción anterior de Windows Installer en suspenso. Reanuda o deshaz esa instalación (o reinicia Windows) antes de limpiar {InstallerFolder}.`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Error: una operación de archivo en cola tras el reinicio afecta a {InstallerFolder} ({0}). Reinicia Windows para completar esa operación antes de limpiar.`,
  'Cli.MovingFiles': `Moviendo {0} {1} innecesarios a {2}...`,
  'Cli.MovedFiles': `Se movieron {0} {1} innecesarios.`,
  'Cli.MutexBlocked': `Otro proceso de InstallerClean mantiene el bloqueo de instancia única (la GUI u otra ejecución de la CLI). Código de salida 75 (transitorio); es seguro reintentar más tarde.`,
  'Cli.EventLogUnavailable': `Nota: error al escribir en el registro de eventos. Comprueba los permisos del registro Aplicación o las directivas de grupo.`,
  'CrashLog.PrivacyHeader': `# crash.log recoge las excepciones no controladas de InstallerClean.\n# Con permisos elevados, los mensajes de excepción del framework\n# pueden incluir rutas de archivo de la sesión en curso (incluidos\n# perfiles de otros usuarios enumerados por las consultas de Windows\n# Installer). Los mensajes de fallo de red de la comprobación de\n# actualizaciones o del envío del registro de resultados pueden\n# incluir la URL de destino y la IP o el proxy resueltos. Las\n# entradas sobre registros ilegibles de Windows Installer pueden\n# incluir un SID de cuenta de Windows (S-1-5-21-...) y los códigos\n# de producto del software instalado.\n# Quita los tres tipos de dato antes de adjuntar este archivo a un\n# informe de error público.\n`,
  'Cli.Help.Header': `InstallerClean - limpiar {InstallerFolder}`,
  'Cli.Help.Usage': `Uso:`,
  'Cli.Help.Help': `  installerclean-cli --help     Muestra esta ayuda (acepta también /?, -h)`,
  'Cli.Help.Version': `  installerclean-cli --version  Muestra la versión (acepta también -v)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s         Solo analizar - lista los innecesarios`,
  'Cli.Help.Delete': `  installerclean-cli /d         Elimina definitivamente los innecesarios`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m         Mueve a la carpeta de copia de seguridad`,
  'Cli.Help.MovePath': `  installerclean-cli /m RUTA    Mueve a la ruta especificada`,
  'Cli.Help.NoteLine1': `installerclean-cli bloquea el símbolo del sistema hasta terminar, para&#10;que un script o una tarea programada pueda esperarlo.`,
  'Cli.Help.ExitCodesHeader': `Códigos de salida:`,
  'Cli.Help.ExitCodeOk': `  0   correcto: hizo lo que se le pidió y nada falló`,
  'Cli.Help.ExitCodeError': `  1   fallo: no se procesó nada (argumentos o destino incorrectos,&#10;       análisis fallido o todos los archivos fallaron)`,
  'Cli.Help.ExitCodePartial': `  2   parcial: unos procesados y otros no (un fallo o un Ctrl+C)`,
  'Cli.Help.ExitCodeTransient': `  75  transitorio: algo temporal bloqueó la ejecución (ver el mensaje)`,
  'Cli.Help.ExitCodeCancelled': `  130 cancelado (Ctrl+C)`,
  'Tooltip.ChangeLanguage': `Cambia el idioma. El programa se reiniciará.`,
  'Automation.ChangeLanguage': `Cambiar el idioma`,
  'Automation.ChangeLanguage.HelpText': `El programa se reiniciará.`,
  'Body.NotScanned.Lead': `Aún no se ha analizado nada.`,
  'Body.NotScanned.Why': `Pulsa Volver a analizar para revisar {InstallerFolder} en busca de archivos de instalación que ya no necesita ningún programa.`,
  'Confirm.MoveSameDrive': `Esa carpeta está en la misma unidad, así que el espacio no volverá hasta que la elimines. Elige una carpeta en otra unidad si quieres el espacio de inmediato.`,
  'Error.ScanCorrelationFailed': `InstallerClean no pudo hacer coincidir los registros de Windows Installer con el contenido de {InstallerFolder}. Casi nada de lo que señalan los registros está realmente ahí, y casi nada de lo que hay ahí lo nombra ningún registro, así que no se pudo demostrar que ningún archivo fuera innecesario. No se ha ofrecido nada y no se ha quitado nada.`,
  'Error.CandidateOutsideCache': `Este archivo no está directamente dentro de la carpeta de Windows Installer; rechazado por seguridad.`,
  'Completion.MoveCancelledSummary': `Cancelaste tras mover {0} de {1} {2}.`,
  'Completion.PermanentDeleteCancelledSummary': `Cancelaste tras eliminar definitivamente {0} de {1} {2}.`,
  'Body.PendingReboot.Lead': `Estos archivos no se pueden limpiar ahora mismo.`,
  'Cli.TooManyArguments': `Error: argumento extra inesperado '{0}'. Si la carpeta de destino tiene un espacio, escribe toda la ruta entre comillas: /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `Carpeta por usuario; ejecuciones programadas o SYSTEM: /m RUTA.`,
  'Error.ScanRecordsUnreadable': `InstallerClean no pudo leer lo suficiente de los registros de Windows Installer para estar seguro de qué sigue haciendo falta: la lista de programas instalados llegó incompleta, y leer esos mismos registros directamente desde el registro de Windows también dio errores. Un archivo podría parecer huérfano solo porque el registro que lo nombra era uno de los ilegibles, así que InstallerClean se detuvo. No se ha eliminado nada.`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer nunca señaló el final de la lista de programas instalados: InstallerClean leyó {2} {3} y luego se rindió tras {0} entradas (último código de error {1}). De una lista sin final no hay que fiarse, así que InstallerClean se detuvo. No se ha eliminado nada.`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer nunca señaló el final de la lista de parches de un programa: InstallerClean leyó {2} {3} y luego se rindió tras {0} entradas (último código de error {1}). De una lista sin final no hay que fiarse, así que InstallerClean se detuvo. No se ha eliminado nada.`,
  'UpdateCheck.Status.UpdateAvailable': `Está disponible la versión {0}.`,
  'Completion.DonateAsk': `Me alegro de haber ayudado. Aquí tienes el bote de propinas, si te nace del corazón.`,
  'About.Link.Guide': `Guía y preguntas frecuentes`,
  'About.Link.ReportProblem': `Informar de un problema`,
  'About.AutoUpdateCheck': `Buscar actualizaciones automáticamente`,
  'Automation.About.Guide.HelpText': `Abre el readme en github en tu navegador.`,
  'Automation.About.ReportProblem.HelpText': `Abre el rastreador de problemas (Issues) en github.com en tu navegador.`,
  'Automation.AutoUpdateCheck.HelpText': `Si está marcada, InstallerClean busca en github una versión más reciente cuando lo ejecutas.`,
  'Tooltip.MoveSameDrive': `Mueve los archivos innecesarios a la carpeta de copia de seguridad. Está en la misma unidad, así que no recuperarás el espacio hasta que elimines esa carpeta.`,
  'Confirm.DeletePermanently.Singular': `Este archivo se eliminará definitivamente. Es seguro hacerlo, pero si quieres una copia, usa Mover en su lugar.`,
  'Confirm.DeletePermanently.Plural': `Estos archivos se eliminarán definitivamente. Es seguro hacerlo, pero si quieres una copia, usa Mover en su lugar.`,
  'Error.ScanCacheRootUnresolved': `InstallerClean no consiguió que Windows resolviera la ruta real de {InstallerFolder}, así que no se pudo demostrar que ningún archivo estuviera dentro y no se ofreció ninguno para limpiar. Este análisis no encontró nada porque esa comprobación falló, no porque la carpeta esté limpia. No se ha quitado nada.`,
  'Automation.Scroll.ProductDetails': `Detalles del producto`,
  'Body.PendingReboot.Other': `Windows Installer tiene algo en curso, así que Mover y Eliminar están en pausa. InstallerClean no tocará {InstallerFolder} mientras cambia. Cuando termine, vuelve a analizar y estarán de nuevo disponibles.`,
  'Cli.TooManyArgumentsNoPath': `Error: argumento extra inesperado '{0}'. /s y /d no admiten más argumentos, y solo se puede usar un modificador por ejecución.`,
  'Cli.MissingFromDisk.Singular': `Windows tiene un registro de {0} archivo que no está en {InstallerFolder}: {1}. En el día a día no causa problemas, pero una actualización o desinstalación de ese programa puede fallar. Para reponer el archivo, necesitas el instalador de la versión que ya tienes. Consíguelo del fabricante del programa y ejecútalo sobre tu copia actual. Una versión más nueva no sirve: tendría que quitar primero la que tienes, y ese es justo el paso que necesita este archivo. Desinstalar primero tampoco funciona, por la misma razón. Esto debería restaurar el archivo y dejar tu configuración intacta, pero Microsoft no lo garantiza.`,
  'Cli.MissingFromDisk.Plural': `Windows tiene registros de {0} archivos que no están en {InstallerFolder}: {1}. En el día a día no causan problemas, pero una actualización o desinstalación de esos programas puede fallar. Para reponer un archivo, necesitas el instalador de la versión que ya tienes de ese programa. Consíguelo del fabricante del programa y ejecútalo sobre tu copia actual. Una versión más nueva no sirve: tendría que quitar primero la que tienes, y ese es justo el paso que necesita el archivo. Desinstalar primero tampoco funciona, por la misma razón. Esto debería restaurar el archivo y dejar tu configuración intacta, pero Microsoft no lo garantiza.`,
  'Cli.MoveNotEnoughSpace': `Error: espacio insuficiente en {0}. Mover estos archivos necesita {1} y hay {2} libres. No se ha movido nada.`,
  'Cli.PendingRebootBlocked.Other': `Error: Windows Installer tiene algo en curso, así que /m y /d están bloqueados. InstallerClean no tocará {InstallerFolder} mientras cambia. Inténtalo de nuevo cuando termine.`,
  'Cli.FoundNoOrphans': `No se encontraron archivos innecesarios.`,
  'Cli.NothingOffered.Singular': `InstallerClean no pudo determinar con certeza qué archivos almacenados en caché pertenecen a los programas instalados aquí, así que ha retenido el único archivo ({2}) que podría haber ofrecido.`,
  'Cli.NothingOffered.Plural': `InstallerClean no pudo determinar con certeza qué archivos almacenados en caché pertenecen a los programas instalados aquí, así que ha retenido los {0} {1} ({2}) que podría haber ofrecido.`,
  'Cli.DestinationChangedMidBatch': `InstallerClean ya no pudo confirmar la carpeta de copia de seguridad, así que se detuvo en lugar de escribir en el sitio equivocado. Comprueba {0} y vuelve a ejecutar el comando.`,
  'Cli.Help.Summary': `Quita archivos .msi/.msp en caché que ningún programa instalado necesita.`,
  'Cli.Help.Elevation': `Requiere símbolo del sistema como administrador; Windows no lo iniciará.`,
  'Error.InstallerLockUnavailableTitle': `No se eliminó nada`,
  'Error.MoveInstallerLockUnavailableTitle': `No se movió nada`,
  'Error.InstallerLockUnavailable': `InstallerClean no pudo tomar el bloqueo que usa Windows Installer para impedir que dos programas cambien el software instalado a la vez, así que no pudo descartar que un archivo pasara a ser necesario a mitad de camino, y no se ha eliminado nada. Inténtalo de nuevo, y reinicia Windows si sigue ocurriendo.`,
  'Error.MoveInstallerLockUnavailable': `InstallerClean no pudo tomar el bloqueo que usa Windows Installer para impedir que dos programas cambien el software instalado a la vez, así que no pudo descartar que un archivo pasara a ser necesario a mitad de camino, y no se ha movido nada. Inténtalo de nuevo, y reinicia Windows si sigue ocurriendo.`,
  'Cli.InstallerLockUnavailable': `Error: InstallerClean no pudo tomar el bloqueo de Windows Installer que impide que dos programas cambien el software instalado a la vez, así que no pudo descartar que un archivo pasara a ser necesario a mitad de camino. No se ha eliminado nada. Inténtalo de nuevo, y reinicia Windows si sigue ocurriendo.`,
  'Cli.MoveInstallerLockUnavailable': `Error: InstallerClean no pudo tomar el bloqueo de Windows Installer que impide que dos programas cambien el software instalado a la vez, así que no pudo descartar que un archivo pasara a ser necesario a mitad de camino. No se ha movido nada. Inténtalo de nuevo, y reinicia Windows si sigue ocurriendo.`,
  'Completion.ReverifyIdentityClaimed': `{0} {1} conservados en su sitio, porque Windows tiene un registro del programa que se nombra dentro.`,
  'Completion.ReverifyIdentityUnreadable': `{0} {1} conservados en su sitio, porque InstallerClean no encontró ningún programa nombrado dentro.`,
  'Error.ScanNoRegisteredFileInFolder': `InstallerClean no pudo hacer coincidir los registros de Windows Installer con el contenido de {InstallerFolder}. La carpeta tiene archivos, pero ni un solo registro señala nada de lo que hay ahí, así que no se pudo demostrar que ningún archivo fuera innecesario. No se ha ofrecido nada y no se ha quitado nada.`,
  'Completion.NothingOffered': `No se ofreció nada en este PC`,
  'Completion.NothingOfferedBody.Singular': `InstallerClean no pudo determinar con certeza qué archivos almacenados en caché pertenecen a los programas instalados aquí, así que ha retenido el único archivo ({2}) que podría haber ofrecido.`,
  'Completion.NothingOfferedBody.Plural': `InstallerClean no pudo determinar con certeza qué archivos almacenados en caché pertenecen a los programas instalados aquí, así que ha retenido los {0} {1} ({2}) que podría haber ofrecido.`,
  'Summary.SupersededHeldBack.Singular': `En este PC InstallerClean no pudo determinar con certeza que el único archivo sustituido ya no haga falta, así que lo ha retenido.`,
  'Summary.SupersededHeldBack.Plural': `En este PC InstallerClean no pudo determinar con certeza que {0} archivos sustituidos ya no hagan falta, así que los ha retenido.`,
  'Cli.SupersededHeldBack.Singular': `En este PC InstallerClean no pudo determinar con certeza que el único archivo sustituido ya no haga falta, así que lo ha retenido.`,
  'Cli.SupersededHeldBack.Plural': `En este PC InstallerClean no pudo determinar con certeza que {0} archivos sustituidos ya no hagan falta, así que los ha retenido.`,
  'Completion.HeldBack.Singular': `{0} archivo retenido. El análisis dijo que no era necesario. La comprobación final no estuvo de acuerdo.`,
  'Completion.HeldBack.Plural': `{0} archivos retenidos. El análisis dijo que no eran necesarios. La comprobación final no estuvo de acuerdo.`,
  'Body.PendingReboot.PendingRenameUnresolved': `Hay una operación de archivos en cola para el próximo reinicio e InstallerClean no puede saber qué archivos nombra, así que no puede descartar que estén en {InstallerFolder}. Reinicia Windows antes de limpiar.`,
  'Completion.MoveRestoreHint': `Elimina esa carpeta cuando estés convencido de que todo va bien.`,
  'Completion.MoveRestoreHintSameDrive': `Elimina esa carpeta cuando estés convencido de que todo va bien. Hasta entonces no recuperarás el espacio.`,
  'Confirm.MoveDestination.Singular': `Este archivo se moverá a:`,
  'Confirm.MoveDestination.Plural': `Estos archivos se moverán a:`,
  'Cli.NothingListed.Singular': `En este PC InstallerClean no pudo tener la certeza de qué archivos en caché pertenecen a los programas instalados aquí, así que ha retenido el único archivo ({2}) en lugar de listarlo.`,
  'Cli.NothingListed.Plural': `En este PC InstallerClean no pudo tener la certeza de qué archivos en caché pertenecen a los programas instalados aquí, así que ha retenido {0} {1} ({2}) en lugar de listarlos.`,
  'Cli.WithheldReasons.Header': `Por qué no pudo tener la certeza:`,
  'Cli.WithheldReasons.RecordedPath': `  Una ruta de archivo de los propios registros de Windows Installer no se pudo resolver.`,
  'Cli.WithheldReasons.FileIdentity': `  La identidad de un archivo nombrado en los registros de Windows Installer no se pudo leer.`,
  'Cli.WithheldReasons.SecondInstance': `  Puede que un programa esté instalado más de una vez en este PC.`,
  'Cli.PendingRebootBlocked.PendingRenameUnresolved': `Error: hay una operación de archivos en cola para el próximo reinicio e InstallerClean no puede saber qué archivos nombra, así que no puede descartar {InstallerFolder}. Reinicia Windows antes de limpiar.`,
  'Cli.MoveRestoreHint': `Comprueba que tus programas siguen actualizándose y desinstalándose con normalidad, y luego elimina {0}.`,
  'Error.ScanStoppedDetails': `Esto también queda registrado en {0}.`,
};

// PARSE CONTROL. About the READING and not about the content, and it exits 2,
// which is a code no ordinary run of this generator can produce: a generator is
// red by intent for the whole gap between a string landing in English and its
// translation round, so its verdict lines and its exit 0 are load-bearing in
// ci.yml and are deliberately untouched here. This says something different from
// "the translation is not done". It says the file could not be read.
//
// BOTH LEGS. raw === 0 catches a file that declares no entry at all, which the
// equality cannot see on its own because 0 === 0 holds. parsed !== raw catches
// entries the reader dropped, which one <comment> moved above its <value> does to
// any regex wanting <value> on the same whitespace run, and the Visual Studio resx
// editor writes that shape. Counted with <data\b so a tab after the tag name is
// not read as an empty file, and neither figure is written down, so a string added
// to the resx cannot make this go stale.
//
// WHY IT IS HERE WHEN THE SELF-CHECK BELOW ALREADY REDDENS. The self-check reaches
// the right verdict through what it happens to compare, not through knowing it
// read anything: with the neutral's attribute order changed, this generator wrote a
// 389-entry file and its own self-check parsed THREE entries out of it, said
// GENERATION HAS ISSUES, and was right for a reason with nothing to do with the
// truth. A tool reasoning over three entries of a 389-entry artefact should say so.
const parseControl = (where, xml, parsed) => {
  const raw = (xml.match(/<data\b/g) || []).length;
  if (raw !== 0 && parsed === raw) return;
  console.error(`PARSE CONTROL FAILED for ${where}: ${raw} '<data' occurrence(s), ${parsed} parsed.`);
  console.error('Refusing to report on a file this generator cannot show it read.');
  process.exit(2);
};

let text = readFileSync(BASE, 'utf8');
// The transform below reaches every entry through '<data name="', one space and no
// \s+, which is NOT the spelling the self-check's parse() uses further down. A
// control that exercises a pattern the reader does not use proves the file has
// structure and proves nothing about whether this reader can reach it, so the
// source is controlled in its own spelling before a single value is replaced.
parseControl(BASE, text,
  [...text.matchAll(/<data name="([^"]+)"[^>]*>\s*<value>/g)].length);

// Remove ONLY the machine-contract Cli.* <data> elements BY NAME (the
// Cli.EventLog* set bar Cli.EventLogUnavailable). The human Cli keys stay and
// are translated from MAP. Same predicate as scripts/check-resx-parity.mjs.
const isMachineCliKey = (k) =>
  k.startsWith('Cli.') && k.includes('EventLog') && k !== 'Cli.EventLogUnavailable';
let cliMachineRemoved = 0;
text = text.replace(/[^\S\n]*<data name="(Cli\.[^"]*)"[\s\S]*?<\/data>\n?/g,
  (m, name) => { if (isMachineCliKey(name)) { cliMachineRemoved++; return ''; } return m; });

// Replace each key's inner <value> from MAP. The closing quote anchors the name,
// so Status.MoveFailed never matches Status.MoveFailed.NoLog. A function
// replacement keeps $-sequences in a value from being read as backreferences.
const escRe = (s) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
const notApplied = [];
for (const [key, val] of Object.entries(MAP)) {
  const re = new RegExp('(<data name="' + escRe(key) + '"[^>]*>\\s*<value>)([\\s\\S]*?)(</value>)');
  let applied = false;
  text = text.replace(re, (m, p1, p2, p3) => { applied = true; return p1 + val + p3; });
  if (!applied) notApplied.push(key);
}

// Append the satellite-only .One override <data> elements before </root>.
const overrideBlock = Object.entries(OVERRIDES)
  .map(([k, v]) => `  <data name="${k}" xml:space="preserve"><value>${v}</value></data>`)
  .join('\n') + '\n';
text = text.replace('</root>', overrideBlock + '</root>');

// Normalise to LF with exactly one trailing newline.
text = text.replace(/\r\n/g, '\n');
if (!text.endsWith('\n')) text += '\n';

writeFileSync(OUT, text, 'utf8');

// ---------------- self-check the written file against the neutral ----------------
const placeholders = (s) => new Set([...s.matchAll(/\{(\d+)(?::[^}]*)?\}/g)].map((p) => p[1]));
const parse = (xml, where) => {
  const map = new Map();
  const re = /<data\s+name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)<\/value>/g;
  let m;
  while ((m = re.exec(xml)) !== null) map.set(m[1], m[2]);
  parseControl(where, xml, map.size);
  return map;
};
const neutral = parse(readFileSync(BASE, 'utf8'), BASE);
// Derived, never pinned: the machine set grows whenever the command line
// gains an event-log string, and a literal here would fail every generator
// at once while asserting nothing about what was actually stripped.
const cliMachineExpected = [...neutral.keys()].filter(isMachineCliKey).length;
const written = readFileSync(OUT, 'utf8');
const output = parse(written, OUT);
// Required = the non-Cli keys plus the human-facing Cli keys.
const neutralRequired = [...neutral.keys()].filter((k) => !isMachineCliKey(k));

// The output also carries the satellite-only .One override key(s). Each must be
// present and share its base key's placeholder set (base = the .Plural sibling if
// it exists, else the flat key itself; mirrors check-resx-parity.mjs).
const overrideKeys = Object.keys(OVERRIDES);
const overrideMissing = overrideKeys.filter((k) => !output.has(k));
const overrideArityMismatch = overrideKeys.filter((k) => {
  if (!output.has(k)) return true;
  const base = k.replace(/\.(?:One|Few|Many)$/, '');
  const ref = neutral.has(`${base}.Plural`) ? `${base}.Plural` : base;
  if (!neutral.has(ref)) return true;
  const a = placeholders(neutral.get(ref)), b = placeholders(output.get(k));
  return a.size !== b.size || [...a].some((i) => !b.has(i));
});

const missingFromMap = neutralRequired.filter((k) => !(k in MAP));
const strayMapKeys = Object.keys(MAP).filter((k) => !neutral.has(k));
const machineLeaked = [...output.keys()].filter(isMachineCliKey);

// The one human-facing Cli.EventLog* key, asserted present rather than left to
// the counts: a predicate that stopped discriminating it takes it out of the
// output AND out of the required set, so every figure above still agrees. The
// MAP substitution notices today only through the order the two run in.
const humanCliStripped = !output.has('Cli.EventLogUnavailable');
const missingFromOutput = neutralRequired.filter((k) => !output.has(k));
const arityMismatch = neutralRequired.filter((k) => {
  if (!output.has(k)) return false; // already counted by missingFromOutput
  const a = placeholders(neutral.get(k)), b = placeholders(output.get(k));
  return a.size !== b.size || [...a].some((i) => !b.has(i));
});
const crlf = (written.match(/\r/g) || []).length;

// Untranslated-phrase gate (KEY-based, HARD): a value still byte-identical to the
// English neutral is a miss, UNLESS its key is a universal keep or in ALSO_KEEP.
const alsoKeep = new Set(ALSO_KEEP);
const untranslated = neutralRequired.filter((k) =>
  output.has(k) && output.get(k) === neutral.get(k) && !KEEP_ENGLISH.has(k) && !alsoKeep.has(k));

// Breakdown computed, never pinned: the non-Cli and human-Cli totals both grow with
// every string the app gains, and a hardcoded pair goes stale silently while the
// checked figure beside it stays right.
const nonCliRequired = neutralRequired.filter((k) => !k.startsWith('Cli.')).length;
console.log('translatable <data> in output:', output.size,
  '(expect', neutralRequired.length + overrideKeys.length,
  '=', nonCliRequired, 'non-Cli +', neutralRequired.length - nonCliRequired, 'Cli +',
  overrideKeys.length, 'override)');
console.log('machine Cli <data> removed:', cliMachineRemoved, `(expect ${cliMachineExpected})`);
console.log('MAP entries:', Object.keys(MAP).length, '| override keys:', overrideKeys.length, '| CRLF:', crlf, '(expect 0)');

// ALSO_KEEP audit roster, so a lazy "force it green" dump is visible at a glance.
if (alsoKeep.size) {
  console.log('ALSO_KEEP (' + alsoKeep.size + '), kept identical to English:');
  for (const k of alsoKeep) {
    const v = output.get(k);
    const words = v == null ? 0 : v.replace(/\{\d+(?::[^}]*)?\}/g, ' ').trim().split(/\s+/).filter(Boolean).length;
    const suspicious = v != null && (words > 2 || v.length > 24);
    console.log('   ' + (suspicious ? '!! suspicious (longer than a word or two) ' : '') + k + ' = ' + JSON.stringify(v));
  }
}

if (notApplied.length) console.log('!! value not applied (regex miss):', notApplied);
if (missingFromMap.length) console.log('!! in neutral but missing from MAP:', missingFromMap);
if (strayMapKeys.length) console.log('!! in MAP but not in neutral:', strayMapKeys);
if (missingFromOutput.length) console.log('!! required key missing from output:', missingFromOutput);
if (arityMismatch.length) console.log('!! placeholder arity differs from neutral:', arityMismatch);
if (machineLeaked.length) console.log('!! machine Cli keys leaked into output:', machineLeaked);
if (humanCliStripped) console.log('!! Cli.EventLogUnavailable stripped: that key is human-facing and must stay');
if (overrideMissing.length) console.log('!! override key missing from output:', overrideMissing);
if (overrideArityMismatch.length) console.log('!! override arity differs from its base key:', overrideArityMismatch);
if (untranslated.length) {
  const show = untranslated.slice(0, 40).join(', ');
  console.log('!! still English (untranslated), ' + untranslated.length + ': ' + show +
    (untranslated.length > 40 ? ', ...and ' + (untranslated.length - 40) + ' more' : ''));
  if (untranslated.length > 50)
    console.log('   (that is most of the file: this is the untranslated template. Translate the MAP values, then a real miss is listed on its own.)');
}

const structuralOk = !notApplied.length && !missingFromMap.length && !strayMapKeys.length &&
  !missingFromOutput.length && !arityMismatch.length && !machineLeaked.length &&
  !humanCliStripped &&
  !overrideMissing.length && !overrideArityMismatch.length &&
  output.size === neutralRequired.length + overrideKeys.length && cliMachineRemoved === cliMachineExpected && crlf === 0;
const ok = structuralOk && !untranslated.length;
console.log(ok ? '\nGENERATION OK' : '\nGENERATION HAS ISSUES (see above)');
