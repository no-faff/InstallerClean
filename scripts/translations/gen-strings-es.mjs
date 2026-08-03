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
// registrado/registrados adjective agreement. Completion.ReverifySkipped carries
// one too (conservado/conservados plus the necesitarlo/necesitarlos clitic): its
// count drives both the noun and the .One selection, so the singular agrees. The
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

// Universal keeps: values identical in every language. Do NOT edit per language.
const KEEP_ENGLISH = new Set([
  'Window.Main.Title',
  'Startup.AlreadyRunningTitle',
  'Startup.UnhandledTitle',
  'Automation.ScanResultAnnouncement',
  'Display.Size.GB',
  'Display.Size.MB',
  'Display.Size.KB',
  'Display.Size.B',
  'Display.Elapsed.Ms',
  'Display.Elapsed.S',
]);

// Per-language keeps: Spanish words byte-identical to the English source that are
// the correct, natural Spanish term ("error" = error), so the still-"English"
// value is not a miss.
const ALSO_KEEP = [
  'Plural.Error.Singular',
];

// Satellite-only .One override(s). NOT in the neutral; appended before </root>.
// check-resx-parity.mjs allows each because its base key is in the neutral.
const OVERRIDES = {
  'Status.RegisteredPackagesFound.One': `Se encontró {0} {1} registrado.`,
  'Cli.FoundOrphans.One': `Found {0} unneeded {1} to clean up ({2}).`,
  'Cli.DeletedFiles.One': `Permanently deleted {0} unneeded {1}.`,
  'Cli.MovedFiles.One': `Moved {0} unneeded {1}.`,
  'Completion.ReverifySkipped.One': `{0} {1} conservado en su sitio: un programa ha vuelto a necesitarlo después del análisis.`,
  // Same participle agreement as ReverifySkipped.One: "conservado" for a
  // single file. The reason clause names no file, so nothing else inflects.
  'Completion.ReverifyIncomplete.One': `{0} {1} conservado en su sitio: los registros de Windows Installer no se han podido leer por completo al repetir la comprobación.`,
};

const MAP = {
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `Acerca de`,
  'Window.Registered.Title': `Archivos registrados que no deberían eliminarse`,
  'Window.Orphaned.Title': `Archivos innecesarios que puedes eliminar sin riesgo`,
  'Section.Registered.Products': `PRODUCTOS`,
  'Section.Registered.Patches': `PARCHES`,
  'Section.Registered.Details': `DETALLES DEL PRODUCTO`,
  'Section.Backup.Folder': `BACKUP FOLDER`,
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
  'Action.BackupFolderPlaceholder': `Path to folder if you move rather than delete.`,
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
  'Automation.ConfirmDelete': `Delete permanently removes the unneeded files. Cancel closes without deleting.`,
  'Automation.ConfirmMove': `Mover coloca los archivos innecesarios en la carpeta de destino elegida. Cancelar los deja donde están.`,
  'Automation.SayThanks': `Dar las gracias`,
  'Automation.ConfirmSendResultLog': `Enviar transmite a No Faff el informe mostrado. Cancelar no envía nada.`,
  'Automation.CheckForUpdates': `Buscar actualizaciones`,
  'Automation.CheckForUpdates.HelpText': `Consulta la página de versiones de github en busca de una versión más reciente.`,
  'Automation.UpdateAvailable.HelpText': `Abre la página de la versión para descargar la más reciente, o cancela para conservar la actual.`,
  'Automation.Licence.HelpText': `Abre el archivo de la licencia en github.com en tu navegador.`,
  'Automation.Section.BackupFolder': `Backup folder`,
  'Automation.Section.Products': `Productos`,
  'Automation.Section.Patches': `Parches`,
  'Automation.Section.ProductDetails': `Detalles del producto`,
  'Automation.BackupFolder': `Backup folder`,
  'Automation.OperationProgress': `Progreso de la operación`,
  'Automation.RescanInstaller': `Volver a analizar {InstallerFolder}`,
  'Automation.ScanningProgress': `Progreso del análisis`,
  'Automation.StartupScanProgress': `Progreso del análisis de inicio`,
  'Automation.ViewOrphanedFiles': `Detalles, archivos innecesarios`,
  'Automation.ViewOrphanedFiles.HelpText': `Disponibles para limpiar.`,
  'Automation.ViewRegisteredFiles': `Detalles, archivos registrados`,
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
  'Tooltip.Move': `Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them.`,
  'Tooltip.MoveNeedsDestination': `Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them.`,
  'Tooltip.Delete': `Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away.`,
  'Tooltip.SigningCertificate': `Nombre del firmante del certificado Authenticode incorporado. La cadena no está verificada.`,
  'Body.MainExplanation.Lead': `Any unneeded files below are [safe to delete].`,
  'Body.MainExplanation.Why': `Están en {InstallerFolder}, donde quedaron cuando se desinstaló un programa ({0}), un parche más reciente sustituyó a otro ({1}) o el editor lo retiró ({2}). InstallerClean solo enumera archivos que el propio Windows da por terminados.`,
  'Body.MainExplanation.Action': `Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored.`,
  'Body.PendingReboot.MsiExecuteMutex': `Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back.`,
  'Body.PendingReboot.InstallerInProgress': `A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}.`,
  'Body.PendingReboot.PendingRenameInCache': `Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning.`,
  'Body.NoFileSelected': `Selecciona un archivo para ver sus detalles.`,
  'Body.NoProductSelected': `Selecciona un producto para ver sus detalles.`,
  'Body.NoMetadata': `No hay metadatos disponibles.`,
  'Body.RegisteredMissingFromDisk': `This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.&#10;&#10;It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.&#10;&#10;To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself.`,
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
  'Status.Moving': `Moving unneeded files...`,
  'Status.Deleting': `Deleting unneeded files...`,
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
  'Completion.MoveSummary.Singular': `{0} {1} en: {2}`,
  'Completion.MoveSummary.Plural': `{0} {1} en: {2}`,
  'Completion.PermanentDeleteSummary.Singular': `{0} {1} permanently deleted`,
  'Completion.PermanentDeleteSummary.Plural': `{0} {1} permanently deleted`,
  'Summary.RegisteredStillUsed.Singular': `{0} archivo aún necesario`,
  'Summary.RegisteredStillUsed.Plural': `{0} archivos aún necesarios`,
  'Summary.OrphanedToCleanUp.Singular': `{0} archivo innecesario para limpiar`,
  'Summary.OrphanedToCleanUp.Plural': `{0} archivos innecesarios para limpiar`,
  'Summary.MissingFromDisk.Singular': `Falta {0} archivo registrado (no lo ha eliminado InstallerClean). Por ahora sin problemas, pero en el futuro una reparación, actualización o desinstalación de ese programa podría fallar. Abre Detalles para saber qué hacer.`,
  'Summary.MissingFromDisk.Plural': `Faltan {0} archivos registrados (no los ha eliminado InstallerClean). Por ahora sin problemas, pero en el futuro una reparación, actualización o desinstalación de esos programas podría fallar. Abre Detalles para saber qué hacer.`,
  'Summary.OperationFiles': `{0} de {1} {2}`,
  'Summary.OrphanedWindow': `{0} huérfanos, {1} sustituidos, {2} obsoletos ({3})`,
  'Summary.RegisteredWindow.Singular': `{0} archivo registrado aún necesario ({1})`,
  'Summary.RegisteredWindow.Plural': `{0} archivos registrados aún necesarios ({1})`,
  'Confirm.MoveTitle': `¿Mover {0} {1} ({2})?`,
  'Confirm.MoveDestination': `Los archivos se moverán a:`,
  'Confirm.DeleteTitle': `¿Eliminar {0} {1} ({2})?`,
  'Error.AdminRequiredTitle': `Acceso denegado`,
  'Error.AdminRequiredBody': `Windows le negó el acceso a InstallerClean, así que se detuvo. No se ha eliminado nada.\n\nInstallerClean ya se estaba ejecutando como administrador, así que volver a iniciarlo de esa forma no servirá de nada. Windows no dice nada más sobre qué denegó el acceso, así que no hay nada concreto que puedas probar.`,
  'Error.InstallerDbUnavailableTitle': `No se pudieron leer los registros de Windows Installer`,
  'Error.ScanFailedTitle': `Análisis fallido`,
  'Error.InstallerDbEmpty': `Los registros de Windows Installer llegaron completamente vacíos: ni un solo programa instalado ni una sola actualización reclama un archivo de instalación en caché. Eso no ocurre en un equipo que funciona (incluso una instalación nueva de Windows tiene alguno), así que o los registros están dañados o no se pudieron leer, y un análisis que se creyera esta respuesta marcaría por error como huérfano cada archivo de {InstallerFolder}. InstallerClean se detuvo en lugar de eso. No se ha eliminado nada.`,
  'Error.MsiAccessDenied': `Windows Installer no dejó que InstallerClean enumerara lo que hay instalado. InstallerClean ya se estaba ejecutando como administrador, así que volver a ejecutarlo como administrador no cambiará nada. Sin esa lista no hay forma segura de saber qué archivos en caché siguen haciendo falta, así que InstallerClean se detuvo. No se ha eliminado nada.`,
  'Error.MsiNonSuccess': `Windows Installer no pudo darle a InstallerClean una lista legible de los programas instalados: {0} entradas seguidas llegaron ilegibles (último código de error {1}). En lugar de trabajar con una lista leída a medias, InstallerClean se detuvo. No se ha eliminado nada.`,
  'Error.InvalidDestinationTitle': `Destino no válido`,
  'Error.DestinationWriteFailedTitle': `No se pudo escribir en el destino`,
  'Error.MoveFailedTitle': `Movimiento fallido`,
  'Error.DeleteFailedTitle': `Eliminación fallida`,
  'Error.SettingNotSavedTitle': `Ajuste no guardado`,
  'Error.SettingNotSavedBody': `No se pudo guardar el cambio. La próxima vez que se inicie, InstallerClean volverá al ajuste anterior.`,
  'Error.DestinationInsideInstaller': `El destino no puede estar dentro de la carpeta de Windows Installer.`,
  'Error.DestinationInSystemFolder': `The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%.`,
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
  'Error.FileInUse.Singular': `This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later.`,
  'Error.FileInUse.Plural': `These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later.`,
  'Error.IOFailure.Singular': `Windows informó de un error de archivo; el archivo se dejó donde estaba.`,
  'Error.IOFailure.Plural': `Windows informó de errores de archivo; estos archivos se dejaron donde estaban.`,
  'Error.UnknownError.Singular': `Algo salió mal con este archivo; se dejó donde estaba.`,
  'Error.UnknownError.Plural': `Algo salió mal con estos archivos; se dejaron donde estaban.`,
  'Error.MoveIntoInstaller': `Se rechaza mover archivos a la carpeta de Windows Installer (destino: {0}).`,
  'Error.DestinationNotFullyQualified': `The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\\Backup, or \\\\server\\backup). InstallerClean can't use this one: {0}`,
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
  'BrowserLaunch.ClipboardOk': `InstallerClean no pudo abrir tu navegador. El enlace está en el portapapeles, así que puedes pegarlo tú mismo:&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean no pudo abrir tu navegador, y tampoco pudo copiar el enlace al portapapeles. El enlace es:&#10;&#10;{0}`,
  'Error.DestinationChangedMidBatch': `The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again.`,
  'Error.CannotWriteFolder': `No se puede escribir en {0}.`,
  'Error.NoUniqueFilename': `No se pudo encontrar un nombre de archivo único para '{0}' tras 10.000 intentos.`,
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
  'Display.ElapsedLong.LessThanASecond': `menos de un segundo`,
  'Display.ElapsedLong.Seconds': `{0:F1} segundos`,
  'Cli.UnknownArgument': `Error: unknown argument '{0}'`,
  'Cli.Cancelling': `Cancelando...`,
  'Cli.Cancelled': `Operación cancelada.`,
  'Cli.GenericError': `Error: unexpected failure ({0}). Details written to {1}.`,
  'Cli.GenericError.NoLog': `Error: unexpected failure ({0}). The crash log could not be written.`,
  'Cli.ScanningInstaller': `Analizando {InstallerFolder}...`,
  'Cli.FoundOrphans': `Found {0} unneeded {1} to clean up ({2}).`,
  'Cli.DeletingFiles': `Deleting {0} unneeded {1}...`,
  'Cli.DeletedFiles': `Permanently deleted {0} unneeded {1}.`,
  'Cli.NoMoveDestination': `Error: no se ha especificado un destino para mover. Usa /m RUTA. (Una ubicación predeterminada configurada en la GUI se guarda por usuario y no se aplica a las ejecuciones programadas ni a las de cuenta de servicio.)`,
  'Cli.MoveDestinationInsideInstaller': `Error: el destino no puede estar dentro de la carpeta de Windows Installer.`,
  'Cli.MoveDestinationRelative': `Error: el destino debe ser una ruta completa. Recibido: {0}`,
  'Cli.MoveDestinationInSystemFolder': `Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}.`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning.`,
  'Cli.MovingFiles': `Moving {0} unneeded {1} to {2}...`,
  'Cli.MovedFiles': `Moved {0} unneeded {1}.`,
  'Cli.MutexBlocked': `Otro proceso de InstallerClean mantiene el bloqueo de instancia única (la GUI u otra ejecución de la CLI). Código de salida 75 (transitorio); es seguro reintentar más tarde.`,
  'Cli.EventLogUnavailable': `Nota: error al escribir en el registro de eventos. Comprueba los permisos del registro Aplicación o las directivas de grupo.`,
  'CrashLog.PrivacyHeader': `# crash.log captures unhandled exceptions from InstallerClean.\n# Under elevation the framework's exception messages can include\n# file paths from the running session (including other users'\n# profiles enumerated by Windows Installer queries). Network-\n# failure messages from the update check or result-log POST can\n# include the destination URL and the resolved IP / proxy address.\n# Entries about unreadable Windows Installer records can include a\n# Windows account SID (S-1-5-21-...) and the product codes of\n# installed software.\n# Redact all three classes of detail before attaching this file to\n# a public bug report.\n`,
  'Cli.Help.Header': `InstallerClean - limpiar {InstallerFolder}`,
  'Cli.Help.Usage': `Uso:`,
  'Cli.Help.Help': `  installerclean-cli --help     Muestra esta ayuda (acepta también /?, -h)`,
  'Cli.Help.Version': `  installerclean-cli --version  Muestra la versión (acepta también -v)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s         Scan only - list unneeded files`,
  'Cli.Help.Delete': `  installerclean-cli /d         Delete unneeded files permanently`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m         Move to the saved backup folder`,
  'Cli.Help.MovePath': `  installerclean-cli /m RUTA    Mueve a la ruta especificada`,
  'Cli.Help.NoteLine1': `installerclean-cli blocks the prompt until it finishes, so a script or&#10;scheduled task can wait on it.`,
  'Cli.Help.ExitCodesHeader': `Códigos de salida:`,
  'Cli.Help.ExitCodeOk': `  0   success: the run finished with nothing left to do`,
  'Cli.Help.ExitCodeError': `  1   failure: nothing processed (bad arguments, a bad destination, a&#10;       failed scan or every file failed)`,
  'Cli.Help.ExitCodePartial': `  2   partial: some processed, some not (a failure or a Ctrl+C part way)`,
  'Cli.Help.ExitCodeTransient': `  75  transitorio: algo temporal bloqueó la ejecución (ver el mensaje)`,
  'Cli.Help.ExitCodeCancelled': `  130 cancelado (Ctrl+C)`,
  'Tooltip.ChangeLanguage': `Cambia el idioma. El programa se reiniciará.`,
  'Automation.ChangeLanguage': `Cambiar el idioma`,
  'Automation.ChangeLanguage.HelpText': `El programa se reiniciará.`,
  'Body.NotScanned.Lead': `Aún no se ha analizado nada.`,
  'Body.NotScanned.Why': `Pulsa Volver a analizar para revisar {InstallerFolder} en busca de archivos de instalación que ya no necesita ningún programa.`,
  'Confirm.MoveSameDrive': `That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away.`,
  'Error.ScanCorrelationFailed': `InstallerClean couldn't square this scan with the Windows Installer records: nearly every file Windows still lists as needed is missing from {InstallerFolder}, while what is actually in the folder matches almost nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed.`,
  'Error.CandidateOutsideCache': `Este archivo no está directamente dentro de la carpeta de Windows Installer; rechazado por seguridad.`,
  'Completion.ReverifySkipped': `{0} {1} conservados en su sitio: un programa ha vuelto a necesitarlos después del análisis.`,
  'Completion.MoveCancelledSummary': `Cancelaste tras mover {0} de {1} {2}.`,
  'Completion.PermanentDeleteCancelledSummary': `Cancelaste tras eliminar definitivamente {0} de {1} {2}.`,
  'Body.PendingReboot.Lead': `Estos archivos no se pueden limpiar ahora mismo.`,
  'Cli.TooManyArguments': `Error: argumento extra inesperado '{0}'. Si la carpeta de destino tiene un espacio, escribe toda la ruta entre comillas: /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `That folder is saved per-user; scheduled or SYSTEM runs need /m PATH.`,
  'Completion.ReverifyIncomplete': `{0} {1} conservados en su sitio: los registros de Windows Installer no se han podido leer por completo al repetir la comprobación.`,
  'Summary.ProgramsUnreadable.Singular': `Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again.`,
  'Summary.ProgramsUnreadable.Plural': `Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again.`,
  'Error.ScanRecordsUnreadable': `InstallerClean no pudo leer lo suficiente de los registros de Windows Installer para estar seguro de qué sigue haciendo falta: la lista de programas instalados llegó incompleta, y leer esos mismos registros directamente desde el registro de Windows también dio errores. Un archivo podría parecer huérfano solo porque el registro que lo nombra era uno de los ilegibles, así que InstallerClean se detuvo. No se ha eliminado nada.`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer nunca señaló el final de la lista de programas instalados: InstallerClean se rindió tras {0} entradas (último código de error {1}). De una lista sin final no hay que fiarse, así que InstallerClean se detuvo. No se ha eliminado nada.`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer nunca señaló el final de la lista de parches de un programa: InstallerClean se rindió tras {0} entradas (último código de error {1}). De una lista sin final no hay que fiarse, así que InstallerClean se detuvo. No se ha eliminado nada.`,
  'UpdateCheck.Status.UpdateAvailable': `Está disponible la versión {0}.`,
  'Completion.DonateAsk': `Me alegro de haber ayudado. Aquí tienes el bote de propinas, si te nace del corazón.`,
  'About.Link.Guide': `Guía y preguntas frecuentes`,
  'About.Link.ReportProblem': `Informar de un problema`,
  'About.AutoUpdateCheck': `Buscar actualizaciones automáticamente`,
  'Automation.About.Guide.HelpText': `Abre el readme en github en tu navegador.`,
  'Automation.About.ReportProblem.HelpText': `Abre el rastreador de problemas (Issues) en github.com en tu navegador.`,
  'Automation.AutoUpdateCheck.HelpText': `Si está marcada, InstallerClean busca en github una versión más reciente cuando lo ejecutas.`,
  'Tooltip.MoveSameDrive': `Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them.`,
  'Completion.MoveRestoreHint.Singular': `The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely).`,
  'Completion.MoveRestoreHint.Plural': `The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely).`,
  'Completion.MoveRestoreHintSameDrive.Singular': `The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely).`,
  'Completion.MoveRestoreHintSameDrive.Plural': `The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely).`,
  'Confirm.DeletePermanently.Singular': `This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead.`,
  'Confirm.DeletePermanently.Plural': `Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead.`,
  'Error.ScanCacheRootUnresolved': `InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed.`,
  'Automation.Scroll.ProductDetails': `Product details`,
  'Body.PendingReboot.Other': `Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back.`,
  'Cli.TooManyArgumentsNoPath': `Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run.`,
  'Cli.MissingFromDisk.Singular': `{0} registered file is missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, the same version, usually restores it.`,
  'Cli.MissingFromDisk.Plural': `{0} registered files are missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, the same version, usually restores them.`,
  'Cli.MoveNotEnoughSpace': `Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved.`,
  'Cli.PendingRebootBlocked.Other': `Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes.`,
  'Cli.FoundNoOrphans': `Found no unneeded files.`,
  'Cli.DestinationChangedMidBatch': `The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then run the command again.`,
  'Cli.ProgramsUnreadable.Singular': `Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up.`,
  'Cli.ProgramsUnreadable.Plural': `Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up.`,
  'Cli.Help.Summary': `Removes cached .msi and .msp files that no installed program still needs.`,
  'Cli.Help.Elevation': `Needs an elevated (administrator) prompt; Windows will not start it.`,
  'Error.InstallerLockUnavailable': `InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening.`,
  'Cli.InstallerLockUnavailable': `Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening.`,
};

let text = readFileSync(BASE, 'utf8');

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
const parse = (xml) => {
  const map = new Map();
  const re = /<data\s+name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)<\/value>/g;
  let m;
  while ((m = re.exec(xml)) !== null) map.set(m[1], m[2]);
  return map;
};
const neutral = parse(readFileSync(BASE, 'utf8'));
// Derived, never pinned: the machine set grows whenever the command line
// gains an event-log string, and a literal here would fail every generator
// at once while asserting nothing about what was actually stripped.
const cliMachineExpected = [...neutral.keys()].filter(isMachineCliKey).length;
const written = readFileSync(OUT, 'utf8');
const output = parse(written);
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
