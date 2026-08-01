#!/usr/bin/env node
// Brazilian Portuguese (pt-BR) satellite generator for InstallerClean. Forked from
// gen-strings-template.mjs and reconciled to the CLI-aware pattern (model:
// gen-strings-de.mjs). It works FROM THE ENGLISH SOURCE (Strings.resx): it reads
// the neutral as the structural base, strips ONLY the 20 machine-contract Cli.*
// keys, replaces the inner <value> of every other key (the non-Cli set plus the human Cli set)
// from the MAP, appends the satellite-only .One override(s), writes LF/UTF-8 and
// self-verifies against the neutral.
//
// pt-BR plural class: CategoryFor returns One at n==0 and n==1, else Other (the
// fr/pt branch). Past participles inflect for number, so the three CLI completion
// lines take a singular .One override (Cli.FoundOrphans / DeletedFiles / MovedFiles:
// Encontrado/Excluido/Movido vs the plural base), as does the attributive
// Status.RegisteredPackagesFound. The count-bearing PROGRESS lines (Cli/Status
// Deleting/Moving) are gerunds, invariant, so they need no override.
//
// MAP escaping (template literals): \\ is one backslash (the paths), \n is a real
// newline (the multi-line values), {0}/{1} are .NET placeholders left verbatim,
// and &#10; is written literally where the neutral uses the XML entity.
import { readFileSync, writeFileSync } from 'node:fs';

const dir = 'src/InstallerClean.Core/Resources';
const BASE = `${dir}/Strings.resx`;            // English source (the "neutral")
const OUT = 'src/InstallerClean.Core/Resources/Strings.pt-BR.resx';

// Universal keeps: keys whose value is the same in every language (brand names,
// the pure-placeholder string, the size/elapsed format strings). Their still-
// English value is NOT a miss. Do NOT translate these values. Do NOT edit per language.
const KEEP_ENGLISH = new Set([
  'Window.Main.Title',                 // InstallerClean
  'Startup.AlreadyRunningTitle',       // InstallerClean
  'Startup.UnhandledTitle',            // InstallerClean
  'Automation.ScanResultAnnouncement', // {0} ({1})
  'Display.Size.GB',                   // {0:F2} GB
  'Display.Size.MB',                   // {0:F1} MB
  'Display.Size.KB',                   // {0:F1} KB
  'Display.Size.B',                    // {0} B
  'Display.Elapsed.Ms',                // {0:F0}ms
  'Display.Elapsed.S',                 // {0:F1}s
]);

// Per-language keeps: keys pt-BR deliberately keeps identical to English. The
// "patch" loanword is standard Brazilian tech usage (README.pt-BR keeps it
// throughout, e.g. "Patches substituídos"), so the patch family stays English.
// All single short tokens; the self-check prints this roster to keep it honest.
const ALSO_KEEP = [
  'Plural.Patch.Singular',       // patch
  'Plural.Patch.Plural',         // patches
  'Field.Patches',               // Patches
  'Section.Registered.Patches',  // PATCHES
  'Automation.Section.Patches',  // Patches
];

const MAP = {
  // Window titles
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `Sobre`,
  'Window.Registered.Title': `Arquivos registrados que não deveriam ser excluídos`,
  'Window.Orphaned.Title': `Arquivos desnecessários que podem ser excluídos com segurança`,

  // Section headings
  'Section.Registered.Products': `PRODUTOS`,
  'Section.Registered.Patches': `PATCHES`,
  'Section.Registered.Details': `DETALHES DO PRODUTO`,
  'Section.Backup.Folder': `BACKUP FOLDER`,
  'Section.SayThanks': `AGRADEÇA`,

  // Field labels (used in detail panels)
  'Field.Reason': `Motivo`,
  'Field.Author': `Autor`,
  'Field.Application': `Aplicativo`,
  'Field.Title': `Título`,
  'Field.Subject': `Assunto`,
  'Field.Keywords': `Palavras-chave`,
  'Field.SigningCertificate': `Certificado de assinatura`,
  'Field.FileSize': `Tamanho do arquivo`,
  'Field.Comment': `Comentário`,
  'Field.ProductName': `Nome do produto`,
  'Field.File': `Arquivo`,
  'Field.Size': `Tamanho`,
  'Field.Patches': `Patches`,

  // Placeholder shown for a registered package whose API ProductName is empty.
  'Field.UnknownProductName': `(desconhecido)`,
  'Field.PatchesOnly': `(apenas patches)`,
  'Field.Missing': `ausente`,

  // Actions (button labels; underscore prefixes are WPF mnemonics)
  'Action.About': `_Sobre`,
  'Action.Copy': `Copiar`,
  'Action.Cut': `Recortar`,
  'Action.Paste': `Colar`,
  'Action.SelectAll': `Selecionar tudo`,
  'Action.Browse': `_Procurar...`,
  'Action.Cancel': `_Cancelar`,
  'Action.CheckForUpdates': `Verificar _atualizações`,
  'Action.Close': `_Fechar`,
  'Action.DeletePermanently': `_Excluir permanentemente`,
  'Action.Done': `_Concluído`,
  'Action.Details': `Detalhes`,
  'Action.BuyMeACuppa': `Me paga um _café`,
  'Action.LeaveStarOnGitHub': `_Deixe uma estrela no GitHub`,
  'Action.Licence': `Licença Apache 2.0`,
  'Action.Move': `_Mover`,
  'Action.BackupFolderPlaceholder': `Path to folder if you move rather than delete.`,
  'Action.OpenReleasePage': `Abrir a página da _versão`,
  'Action.Rescan': `_Reanalisar`,
  'Action.ScanAgain': `Analisar de _novo`,
  'Action.SendResultLog': `Enviar relatório`,
  'Action.SendResultLogConfirm': `_Enviar`,

  // Automation names (screen reader / accessibility)
  'Automation.BuyMeACuppa': `Doar`,
  'Automation.BuyMeACuppa.About': `Me paga um café`,
  'Automation.CancelOperation': `Cancelar a operação`,
  'Automation.CancelScan': `Cancelar a análise`,
  'Automation.CancelStartupScan': `Cancelar a análise inicial`,
  'Automation.Close': `Fechar`,
  'Automation.CloseWindow': `Fechar a janela`,
  'Automation.CloseResult': `Fechar o resultado e voltar para a janela principal`,
  'Automation.LeaveStarOnGitHub.About': `Deixe uma estrela no github`,
  'Automation.Minimise': `Minimizar`,
  'Automation.ConfirmDelete': `Delete permanently removes the unneeded files. Cancel closes without deleting.`,
  'Automation.ConfirmMove': `Mover coloca os arquivos desnecessários na pasta de destino escolhida. Cancelar os deixa onde estão.`,
  'Automation.SayThanks': `Agradeça`,
  'Automation.ConfirmSendResultLog': `Enviar transmite ao No Faff o relatório exibido. Cancelar não envia nada.`,
  'Automation.CheckForUpdates': `Verificar atualizações`,
  'Automation.CheckForUpdates.HelpText': `Consulta a página de versões do github em busca de uma versão mais recente.`,
  'Automation.UpdateAvailable.HelpText': `Abra a página da versão para baixar a versão mais recente, ou cancele para manter a versão atual.`,
  'Automation.Licence.HelpText': `Abre o arquivo da licença em github.com no seu navegador.`,
  'Automation.Section.BackupFolder': `Backup folder`,
  'Automation.Section.Products': `Produtos`,
  'Automation.Section.Patches': `Patches`,
  'Automation.Section.ProductDetails': `Detalhes do produto`,
  'Automation.BackupFolder': `Backup folder`,
  'Automation.OperationProgress': `Progresso da operação`,
  'Automation.RescanInstaller': `Analisar {InstallerFolder} novamente`,
  'Automation.ScanningProgress': `Progresso da análise`,
  'Automation.StartupScanProgress': `Progresso da análise inicial`,
  'Automation.ViewOrphanedFiles': `Detalhes, arquivos desnecessários`,
  'Automation.ViewOrphanedFiles.HelpText': `Disponíveis para limpeza.`,
  'Automation.ViewRegisteredFiles': `Detalhes, arquivos registrados`,
  'Automation.ViewRegisteredFiles.HelpText': `Inventário somente leitura.`,
  'Automation.SortStatus.Ascending': `Classificado por {0}, crescente`,
  'Automation.SortStatus.Descending': `Classificado por {0}, decrescente`,
  'Automation.Scroll.ScanResults': `Resultados da análise`,
  'Automation.Scroll.ResultDetails': `Detalhes do resultado`,
  'Automation.Scroll.FileDetails': `Detalhes do arquivo`,
  'Automation.Scroll.DialogBody': `Texto da caixa de diálogo`,
  'Automation.ScanResultAnnouncement': `{0} ({1})`,
  'Automation.CompletionErrors': `Arquivos que não puderam ser processados`,
  'Automation.RegisteredMissingSeeAlso': `Explica esta pasta, e como recuperar um arquivo, no README`,

  // Tooltips
  'Tooltip.BuyMeACuppa.About': `É trabalho que dá sede!`,
  'Tooltip.CancellingPending': `Cancelamento solicitado. O InstallerClean está esperando o passo atual chegar a um ponto em que possa parar. Isso pode levar alguns segundos durante operações intensas de E/S ou uma chamada ao banco de dados MSI.`,
  'Tooltip.Close': `Fechar`,
  'Tooltip.LeaveStarOnGitHub.About': `Uma estrela ajuda outras pessoas a encontrar o InstallerClean.`,
  'Tooltip.Minimise': `Minimizar`,
  'Tooltip.SendResultLog': `Você decide, mas eu agradeço. Envia um resumo anônimo que só me diz se está funcionando e quanto espaço as pessoas estão liberando. A próxima tela mostra o que será enviado antes de você confirmar.`,
  'Tooltip.SendResultLog.NothingFound': `Você decide, mas eu agradeço. Envia um resumo anônimo que só me diz se está funcionando. A próxima tela mostra o que será enviado antes de você confirmar.`,
  'Tooltip.Move': `Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them.`,
  'Tooltip.MoveNeedsDestination': `Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them.`,
  'Tooltip.Delete': `Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away.`,
  'Tooltip.SigningCertificate': `Nome do titular do certificado Authenticode incorporado. Cadeia não verificada.`,

  // Body copy
  'Body.MainExplanation.Lead': `Any unneeded files below are [safe to delete].`,
  'Body.MainExplanation.Why': `Eles ficam em {InstallerFolder}, deixados para trás quando um programa foi desinstalado ({0}), um patch mais recente substituiu outro ({1}) ou o fabricante o retirou ({2}). O InstallerClean só lista arquivos que o próprio Windows informa ter terminado de usar.`,
  'Body.MainExplanation.Action': `Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored.`,
  'Body.PendingReboot.MsiExecuteMutex': `Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back.`,
  'Body.PendingReboot.InstallerInProgress': `A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}.`,
  'Body.PendingReboot.PendingRenameInCache': `Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning.`,
  'Body.NoFileSelected': `Selecione um arquivo para ver os detalhes.`,
  'Body.NoProductSelected': `Selecione um produto para ver os detalhes.`,
  'Body.NoMetadata': `Nenhum metadado disponível.`,
  'Body.RegisteredMissingFromDisk': `This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.&#10;&#10;It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.&#10;&#10;To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself.`,
  'Body.RegisteredMissingFromDisk.SeeAlso': `O README [explica esta pasta], e como recuperar um arquivo, com as próprias palavras da Microsoft.`,
  'Body.NoPatches': `(nenhum)`,

  // Reasons (Reason column in the orphaned-files list)
  'Reason.Orphaned': `Órfão`,
  'Reason.Superseded': `Substituído`,
  'Reason.Obsoleted': `Obsoleto`,

  // Status / progress text
  'Status.Scanning': `Analisando...`,
  'Status.Cancelling': `Cancelando...`,
  'Status.StartingScan': `Iniciando a análise...`,
  'Status.QueryingApi': `Consultando o Windows sobre o software instalado...`,
  'Status.ScanningCache': `Analisando a pasta do cache de instalação...`,
  'Status.EnumeratingProducts': `Enumerando os produtos instalados...`,
  'Status.CheckingRegistry': `Verificando o registro em busca de pacotes adicionais...`,

  // 0 = registered package count, 1 = pluralised "package"/"packages"
  'Status.RegisteredPackagesFound': `Foram encontrados {0} {1} registrados.`,

  // 0 = elapsed time text (e.g. "1.2s")
  'Status.ScanComplete': `Análise concluída ({0})`,
  'Status.FoundProducts': `Analisando os pacotes locais...`,

  // 0 = file count, 1 = pluralised noun ("file"/"files")
  'Status.FoundUnused': `Encontrados {0} {1} que você pode excluir com segurança.`,
  'Status.PreparingDestination': `Preparando a pasta de destino...`,

  // 0 = file count, 1 = pluralised noun
  'Status.Moving': `Moving files...`,
  'Status.Deleting': `Deleting files...`,
  'Status.MoveCancelled.Partial': `Movimentação cancelada após processar {0} de {1} {2}.`,
  'Status.DeleteCancelled.Partial': `Exclusão cancelada após processar {0} de {1} {2}.`,
  'Status.MoveFailed': `Falha na movimentação ({0}). Detalhes em {1}.`,
  'Status.MoveFailed.NoLog': `Falha na movimentação ({0}). Não foi possível gravar o crash.log.`,
  'Status.DeleteFailed': `Falha na exclusão ({0}). Detalhes em {1}.`,
  'Status.DeleteFailed.NoLog': `Falha na exclusão ({0}). Não foi possível gravar o crash.log.`,
  'Status.ScanAccessDenied': `Acesso negado. O Windows recusou a análise.`,
  'Status.ScanFailedDb': `Falha na análise: não foi possível ler os registros do Windows Installer.`,
  'Status.ScanCancelled': `Análise cancelada.`,
  'Status.Done': `Pronto`,
  'Status.ScanFailedDetails': `Falha na análise ({0}). Detalhes em {1}.`,
  'Status.ScanFailedDetails.NoLog': `Falha na análise ({0}). Não foi possível gravar o crash.log.`,

  // Completion screen
  'Completion.AllClean': `Tudo limpo`,
  'Completion.NothingToCleanUp': `Nada para limpar em {InstallerFolder}`,
  'Completion.NothingToCleanUpReceipt': `Análise de {0} {1} em {2}`,

  // 0 = size freed (e.g. "120.5 MB")
  'Completion.Freed': `{0} liberados`,
  'Completion.Moved': `{0} movidos`,
  // Heading for the outcome where the operation acted on no file at all, then
  // the failure count line beneath it: 0 = files that failed, 1 = files tried
  // (succeeded + failed). The count selecting the form is {0}; the noun belongs
  // to {1}, so treat the two independently.
  'Completion.NothingMoved': `Nada foi movido`,
  'Completion.NothingDeleted': `Nada foi excluído`,
  'Completion.FailedCount.Singular': `{0} arquivo de {1} não pôde ser movido.`,
  'Completion.FailedCount.Plural': `{0} arquivos de {1} não puderam ser movidos.`,
  'Completion.FailedCountDelete.Singular': `{0} arquivo de {1} não pôde ser excluído.`,
  'Completion.FailedCountDelete.Plural': `{0} arquivos de {1} não puderam ser excluídos.`,

  // 0 = moved count, 1 = pluralised noun, 2 = destination path
  'Completion.MoveSummary.Singular': `{0} {1} movido para: {2}`,
  'Completion.MoveSummary.Plural': `{0} {1} movidos para: {2}`,

  // 0 = deleted count, 1 = pluralised noun

  // 0 = deleted count, 1 = pluralised noun
  'Completion.PermanentDeleteSummary.Singular': `{0} {1} permanently deleted`,
  'Completion.PermanentDeleteSummary.Plural': `{0} {1} permanently deleted`,

  // Summaries
  'Summary.RegisteredStillUsed.Singular': `{0} arquivo ainda necessário`,
  'Summary.RegisteredStillUsed.Plural': `{0} arquivos ainda necessários`,
  'Summary.OrphanedToCleanUp.Singular': `{0} arquivo desnecessário para limpar`,
  'Summary.OrphanedToCleanUp.Plural': `{0} arquivos desnecessários para limpar`,
  'Summary.MissingFromDisk.Singular': `{0} arquivo registrado está ausente (não foi excluído pelo InstallerClean). Sem problema agora, mas uma futura reparação, atualização ou desinstalação desse programa pode falhar. Abra Detalhes para saber o que fazer.`,
  'Summary.MissingFromDisk.Plural': `{0} arquivos registrados estão ausentes (não foram excluídos pelo InstallerClean). Sem problema agora, mas uma futura reparação, atualização ou desinstalação desses programas pode falhar. Abra Detalhes para saber o que fazer.`,

  // 0 = current file count, 1 = total count, 2 = pluralised noun.
  'Summary.OperationFiles': `{0} de {1} {2}`,

  // 0 = orphaned count, 1 = superseded count, 2 = obsoleted count, 3 = size display
  'Summary.OrphanedWindow': `{0} órfãos, {1} substituídos, {2} obsoletos ({3})`,
  // 0 = count, 1 = size display. Singular/plural split so the noun and verb agree.
  'Summary.RegisteredWindow.Singular': `{0} arquivo registrado que ainda é necessário ({1})`,
  'Summary.RegisteredWindow.Plural': `{0} arquivos registrados que ainda são necessários ({1})`,

  // Confirmation dialogs

  // 0 = file count, 1 = pluralised "file"/"files", 2 = size display
  'Confirm.MoveTitle': `Mover {0} {1} ({2})?`,

  // 0 = destination path
  'Confirm.MoveDestination': `Os arquivos serão movidos para:`,
  'Confirm.DeleteTitle': `Excluir {0} {1} ({2})?`,

  // Error messages
  'Error.AdminRequiredTitle': `Acesso negado`,
  'Error.AdminRequiredBody': `O Windows negou acesso ao InstallerClean, que por isso parou. Nada foi removido.\n\nO InstallerClean já estava em execução como administrador, então iniciá-lo de novo dessa forma não vai ajudar. O Windows não diz mais nada sobre o que negou o acesso, então não há nada específico para tentar.`,
  'Error.InstallerDbUnavailableTitle': `Não foi possível ler os registros do Windows Installer`,
  'Error.ScanFailedTitle': `Falha na análise`,
  'Error.InstallerDbEmpty': `Os registros do Windows Installer voltaram completamente vazios: nenhum programa instalado e nenhuma atualização reivindica um arquivo de instalação em cache. Isso não acontece em uma máquina que funciona (até uma instalação nova do Windows tem alguns), então ou os registros estão danificados ou não puderam ser lidos, e uma análise que acreditasse nessa resposta chamaria erroneamente de órfão cada arquivo em {InstallerFolder}. Em vez disso, o InstallerClean parou. Nada foi removido.`,
  'Error.MsiAccessDenied': `O Windows Installer não deixou o InstallerClean listar o que está instalado. O InstallerClean já estava em execução como administrador, então executá-lo de novo como administrador não muda nada. Sem essa lista não há como saber com segurança quais arquivos em cache ainda são necessários, então o InstallerClean parou. Nada foi removido.`,
  'Error.MsiNonSuccess': `O Windows Installer não conseguiu dar ao InstallerClean uma lista legível dos programas instalados: {0} entradas seguidas voltaram ilegíveis (último código de erro {1}). Em vez de trabalhar com uma lista lida pela metade, o InstallerClean parou. Nada foi removido.`,
  'Error.InvalidDestinationTitle': `Destino inválido`,
  'Error.DestinationWriteFailedTitle': `Não foi possível gravar no destino`,
  'Error.MoveFailedTitle': `Falha na movimentação`,
  'Error.DeleteFailedTitle': `Falha na exclusão`,
  'Error.SettingNotSavedTitle': `Configuração não salva`,
  'Error.SettingNotSavedBody': `Não foi possível salvar a alteração. Na próxima vez que for iniciado, o InstallerClean voltará à configuração anterior.`,
  'Error.DestinationInsideInstaller': `O destino não pode estar dentro da pasta do Windows Installer.`,

  // 0 = the destination path the user typed
  'Error.DestinationInSystemFolder': `O destino {0} fica dentro de uma pasta de sistema do Windows. Escolha um caminho fora de %SystemRoot%, %ProgramFiles% e %ProgramData%.`,
  'Error.NotEnoughSpaceTitle': `Espaço insuficiente`,

  // 0 = destination, 1 = required size, 2 = available size
  'Error.NotEnoughSpaceBody': `Espaço insuficiente em {0}\n\nNecessário: {1}\nDisponível: {2}`,

  // 0 = destination
  'Error.AccessDeniedDestination': `Você não tem permissão para gravar em {0}.\nTente uma pasta no seu perfil de usuário ou em uma unidade sua.`,
  'Error.PathTooLong': `O caminho {0} é longo demais para o Windows. Escolha um caminho mais curto.`,
  'Error.DestinationMissing': `A pasta {0} não existe e não foi possível criá-la. Verifique a letra da unidade ou o caminho de rede.`,
  'Error.IOWriteDestination': `O Windows não consegue gravar em {0}.\nDetalhes em {1}.`,
  'Error.IOWriteDestination.NoLog': `O Windows não consegue gravar em {0}. Não foi possível gravar o crash.log.`,
  'Error.WriteDestination': `Não é possível gravar em {0}.\nDetalhes em {1}.`,
  'Error.WriteDestination.NoLog': `Não é possível gravar em {0}. Não foi possível gravar o crash.log.`,
  'Error.MissingSourceFile': `O arquivo não existe mais.`,
  'Error.SourceIsReparsePoint': `O arquivo de origem é um link simbólico ou junção; recusado por segurança.`,
  // Singular = the sentence for ONE file, which the CLI prints after "filename: ";
  // plural = the heading the completion overlay puts over a list of filenames.
  'Error.AccessDenied.Singular': `O Windows negou o acesso a este arquivo; ele foi deixado onde estava.`,
  'Error.AccessDenied.Plural': `O Windows negou o acesso a estes arquivos; eles foram deixados onde estavam.`,
  'Error.FileInUse.Singular': `This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later.`,
  'Error.FileInUse.Plural': `These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later.`,
  'Error.IOFailure.Singular': `O Windows relatou um erro de arquivo; o arquivo foi deixado onde estava.`,
  'Error.IOFailure.Plural': `O Windows relatou erros de arquivo; estes arquivos foram deixados onde estavam.`,
  'Error.UnknownError.Singular': `Algo deu errado com este arquivo; ele foi deixado onde estava.`,
  'Error.UnknownError.Plural': `Algo deu errado com estes arquivos; eles foram deixados onde estavam.`,

  // 0 = shell error code

  // 0 = destination
  'Error.MoveIntoInstaller': `Recusando mover arquivos para a pasta do Windows Installer (destino: {0}).`,

  // 0 = the relative path the caller passed
  'Error.DestinationNotFullyQualified': `The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\\Backup, or \\\\server\\backup). InstallerClean can't use this one: {0}`,
  'BrowserLaunch.FailedTitle': `Não foi possível abrir o navegador`,
  'UpdateCheck.Title': `Verificar atualizações`,
  'UpdateCheck.Status.Checking': `Verificando...`,
  'UpdateCheck.Status.UpToDate': `Tudo atualizado.`,
  'UpdateCheck.UpdateAvailable.Title': `Atualização disponível`,

  // 0 = installed version, 1 = latest version on GitHub
  'UpdateCheck.UpdateAvailable.Body': `Você está usando a versão {0}.&#10;A versão {1} está disponível.`,
  'UpdateCheck.Failed.NetworkUnavailable': `Não foi possível acessar o GitHub. Verifique a sua conexão com a internet e tente de novo.`,
  'UpdateCheck.Failed.ServerError': `O GitHub retornou uma resposta de erro. Tente de novo em alguns minutos.`,
  'UpdateCheck.Failed.ResponseParseError': `A resposta do GitHub não continha uma versão reconhecível. Tente de novo mais tarde, ou abra diretamente a página de versões.`,
  'UpdateCheck.Failed.Timeout': `A verificação expirou. A sua conexão com o GitHub pode estar lenta; tente de novo.`,
  'UpdateCheck.Failed.Unknown': `A verificação falhou por um motivo desconhecido. Os detalhes estão no crash.log, se você precisar relatar o problema.`,

  // 0 = the URL the user was trying to reach
  'BrowserLaunch.ClipboardOk': `O InstallerClean não conseguiu abrir o seu navegador. O link está na área de transferência, então você mesmo pode colá-lo:&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `O InstallerClean não conseguiu abrir o seu navegador, e também não conseguiu copiar o link para a área de transferência. O link é:&#10;&#10;{0}`,

  // 0 = the destination folder whose canonical path changed mid-batch
  'Error.DestinationChangedMidBatch': `The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again.`,

  // 0 = folder, 1 = inner exception message
  'Error.CannotWriteFolder': `Não é possível gravar em {0}.`,

  // 0 = file name
  'Error.NoUniqueFilename': `Não foi possível encontrar um nome de arquivo único para '{0}' após 10.000 tentativas.`,

  // Result log (post-cleanup diagnostic send)
  'ResultLog.Sending': `Enviando...`,
  'ResultLog.Sent': `Obrigado! Relatório enviado.`,
  'ResultLog.Failed': `Falha no envio. Tente de novo mais tarde.`,
  'ResultLog.NothingToSend': `Nenhum relatório para enviar.`,
  'ConfirmSendResultLog.Title': `Enviar isto?`,
  'ConfirmSendResultLog.Reassurance': `Vai para nofaff.netlify.app/api/result-log. Nada identifica você ou a sua máquina; só me diz que o InstallerClean está funcionando e [quanto espaço as pessoas estão liberando].`,
  'Automation.ResultLogPreview': `Visualização do relatório`,

  // Single instance / startup / crash
  'Startup.AlreadyRunningTitle': `InstallerClean`,
  'Startup.AlreadyRunningBody': `O InstallerClean já está em execução.`,
  'Startup.UnhandledTitle': `InstallerClean`,
  'Startup.UnhandledBody': `Ocorreu um erro inesperado e o InstallerClean precisa fechar.\n\n{0}\n\nDetalhes gravados em:\n{1}`,
  'Startup.UnhandledBody.NoLog': `Ocorreu um erro inesperado e o InstallerClean precisa fechar.\n\n{0}\n\nNão foi possível gravar o crash.log.`,
  'Startup.ErrorTitle': `Erro de inicialização`,
  'Startup.FailedToStart': `Falha ao iniciar ({0}). Detalhes gravados em:\n{1}`,
  'Startup.FailedToStart.NoLog': `Falha ao iniciar ({0}). Não foi possível gravar o crash.log.`,

  // File picker
  'FilePicker.ChooseDestinationTitle': `Escolha a pasta de destino para os arquivos movidos`,

  // Version display

  // 0 = major.minor.patch (e.g. "1.5.4")
  'Version.Display': `Versão {0}`,
  'Plural.File.Singular': `arquivo`,
  'Plural.File.Plural': `arquivos`,
  'Plural.Error.Singular': `erro`,
  'Plural.Error.Plural': `erros`,
  'Plural.Package.Singular': `pacote`,
  'Plural.Package.Plural': `pacotes`,
  'Plural.Product.Singular': `produto`,
  'Plural.Product.Plural': `produtos`,
  'Plural.Patch.Singular': `patch`,
  'Plural.Patch.Plural': `patches`,
  'Display.Size.GB': `{0:F2} GB`,
  'Display.Size.MB': `{0:F1} MB`,
  'Display.Size.KB': `{0:F1} KB`,
  'Display.Size.B': `{0} B`,
  'Display.Elapsed.Ms': `{0:F0}ms`,
  'Display.Elapsed.S': `{0:F1}s`,
  'Display.ElapsedLong.LessThanASecond': `menos de um segundo`,
  'Display.ElapsedLong.Seconds': `{0:F1} segundos`,
  'CrashLog.PrivacyHeader': `# crash.log captures unhandled exceptions from InstallerClean.\n# Under elevation the framework's exception messages can include\n# file paths from the running session (including other users'\n# profiles enumerated by Windows Installer queries). Network-\n# failure messages from the update check or result-log POST can\n# include the destination URL and the resolved IP / proxy address.\n# Entries about unreadable Windows Installer records can include a\n# Windows account SID (S-1-5-21-...) and the product codes of\n# installed software.\n# Redact all three classes of detail before attaching this file to\n# a public bug report.\n`,
  'Tooltip.ChangeLanguage': `Alterar idioma. O programa será reiniciado.`,
  'Automation.ChangeLanguage': `Alterar idioma`,
  'Automation.ChangeLanguage.HelpText': `O programa será reiniciado.`,

  // Command-line tool (installerclean-cli): the HUMAN-facing Cli.* keys.
  // The 20 machine-contract Cli.EventLog* keys (bar EventLogUnavailable) are
  // stripped and stay English at the emit site; they are not in this MAP.
  'Cli.UnknownArgument': `Argumento desconhecido: '{0}'`,
  'Cli.Cancelling': `Cancelando...`,
  'Cli.Cancelled': `Cancelado.`,
  'Cli.GenericError': `Erro: {0}. Detalhes gravados em {1}.`,
  'Cli.GenericError.NoLog': `Erro: {0}. Não foi possível gravar o crash.log.`,
  'Cli.ScanningInstaller': `Analisando {InstallerFolder}...`,
  'Cli.FoundOrphans': `Encontrados {0} {1} para limpar ({2}).`,
  'Cli.NothingToDo': `Nada a fazer.`,
  'Cli.DeletingFiles': `Excluindo {0} {1}...`,
  'Cli.DeletedFiles': `Permanently deleted {0} {1}.`,
  'Cli.NoMoveDestination': `Erro: nenhum destino de movimentação especificado. Use /m CAMINHO. (Um padrão definido na GUI é por usuário e não se aplica a execuções agendadas ou em contas de serviço.)`,
  'Cli.MoveDestinationInsideInstaller': `Erro: o destino não pode estar dentro da pasta do Windows Installer.`,
  'Cli.MoveDestinationRelative': `Erro: o destino deve ser um caminho totalmente qualificado. Recebido: {0}`,
  'Cli.MoveDestinationInSystemFolder': `Erro: o destino {0} fica dentro de uma pasta de sistema do Windows. Escolha um caminho fora de %SystemRoot%, %ProgramFiles% e %ProgramData%.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}.`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning.`,
  'Cli.MovingFiles': `Movendo {0} {1} para {2}...`,
  'Cli.MovedFiles': `Movidos {0} {1}.`,
  'Cli.MutexBlocked': `Outro processo do InstallerClean mantém o bloqueio de instância única (a GUI ou outra execução da CLI). Código de saída 75 (transitório); seguro tentar novamente mais tarde.`,
  'Cli.EventLogUnavailable': `Observação: falha ao gravar no Log de Eventos. Verifique as permissões do log de Aplicativo ou a Diretiva de Grupo.`,
  'Cli.Help.Header': `InstallerClean - limpeza de {InstallerFolder}`,
  'Cli.Help.Usage': `Uso:`,
  'Cli.Help.Help': `  installerclean-cli --help      Mostra esta ajuda (aceita também /?, -h)`,
  'Cli.Help.Version': `  installerclean-cli --version   Mostra a versão (aceita também -v)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s         Scan only - list unneeded files`,
  'Cli.Help.Delete': `  installerclean-cli /d         Delete unneeded files permanently`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m         Move to the saved backup folder`,
  'Cli.Help.MovePath': `  installerclean-cli /m CAMINHO  Move para o caminho especificado`,
  'Cli.Help.NoteLine1': `installerclean-cli é um processo de console real e bloqueia o prompt`,
  'Cli.Help.NoteLine2': `até terminar; redirecione ou encaminhe a saída por pipe como faria`,
  'Cli.Help.NoteLine3': `com qualquer outro executável de console. A GUI é InstallerClean.exe.`,
  'Cli.Help.ExitCodesHeader': `Códigos de saída:`,
  'Cli.Help.ExitCodeOk': `  0   sucesso: todos os arquivos sinalizados foram processados`,
  'Cli.Help.ExitCodeError': `  1   falha: nada processado (argumentos, análise ou todos os arquivos)`,
  'Cli.Help.ExitCodePartial': `  2   parcial: alguns arquivos processados, outros falharam`,
  'Cli.Help.ExitCodeTransient': `  75  transitório: algo temporário bloqueou a execução (veja a mensagem)`,
  'Cli.Help.ExitCodeCancelled': `  130 cancelado (Ctrl+C)`,
  'Body.NotScanned.Lead': `Nada foi analisado ainda.`,
  'Body.NotScanned.Why': `Clique em Reanalisar para procurar em {InstallerFolder} arquivos de instalação que nenhum programa ainda precisa.`,
  'Confirm.MoveSameDrive': `That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away.`,
  'Error.ScanCorrelationFailed': `O InstallerClean não conseguiu conciliar esta análise com os registros do Windows Installer: todo arquivo que o Windows ainda lista como necessário está ausente de {InstallerFolder}, enquanto os arquivos que de fato estão na pasta não correspondem a nenhum registro. Nenhuma máquina real se parece com isso, então isso aponta para um problema na leitura dos registros, e não para arquivos que você possa remover com segurança. Nada foi oferecido para limpeza e nada foi removido.`,
  'Error.CandidateOutsideCache': `Este arquivo não está diretamente dentro da pasta do Windows Installer; recusado por segurança.`,
  'Completion.ReverifySkipped': `{0} {1} mantidos no lugar: um programa voltou a precisar deles depois da análise.`,
  'Completion.MoveCancelledSummary': `Movimentação cancelada após mover {0} de {1} {2}.`,
  'Completion.PermanentDeleteCancelledSummary': `Exclusão permanente cancelada após remover {0} de {1} {2}.`,
  'Body.PendingReboot.Lead': `Estes arquivos não podem ser limpos agora.`,
  'Cli.TooManyArguments': `Erro: argumento extra inesperado '{0}'. Se a sua pasta de destino tiver um espaço no nome, coloque aspas em todo o caminho: /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `O padrão salvo é por usuário; execuções agendadas ou SYSTEM: /m CAMINHO.`,
  'Completion.ReverifyIncomplete': `{0} {1} mantidos no lugar: não foi possível ler por completo os registros do Windows Installer quando a verificação foi repetida.`,
  'Summary.ProgramsUnreadable.Singular': `{0} programa instalado não pôde ser lido nesta análise, então os patches substituídos foram mantidos. Os arquivos órfãos não são afetados.`,
  'Summary.ProgramsUnreadable.Plural': `{0} programas instalados não puderam ser lidos nesta análise, então os patches substituídos foram mantidos. Os arquivos órfãos não são afetados.`,
  'Error.ScanRecordsUnreadable': `O InstallerClean não conseguiu ler o suficiente dos registros do Windows Installer para ter certeza do que ainda é necessário: a lista de programas instalados voltou incompleta, e ler esses mesmos registros direto do registro do Windows também deu erros. Um arquivo poderia parecer órfão só porque o registro que o nomeia era um dos ilegíveis, então o InstallerClean parou. Nada foi removido.`,
  'Error.MsiEnumerationNeverEnded': `O Windows Installer nunca sinalizou o fim da lista de programas instalados: o InstallerClean desistiu depois de {0} entradas (último código de erro {1}). Não dá para confiar em uma lista sem fim, então o InstallerClean parou. Nada foi removido.`,
  'Error.MsiPatchEnumerationNeverEnded': `O Windows Installer nunca sinalizou o fim da lista de patches de um programa: o InstallerClean desistiu depois de {0} entradas (último código de erro {1}). Não dá para confiar em uma lista sem fim, então o InstallerClean parou. Nada foi removido.`,
  'UpdateCheck.Status.UpdateAvailable': `A versão {0} está disponível.`,
  'Completion.DonateAsk': `Que bom que ajudou. A caixinha está aqui, se vier do coração.`,
  'About.Link.Guide': `Guia e perguntas frequentes`,
  'About.Link.ReportProblem': `Relatar um problema`,
  'About.AutoUpdateCheck': `Verificar atualizações automaticamente`,
  'Automation.About.Guide.HelpText': `Abre o readme no github no seu navegador.`,
  'Automation.About.ReportProblem.HelpText': `Abre o rastreador de problemas (Issues) em github.com no seu navegador.`,
  'Automation.AutoUpdateCheck.HelpText': `Se marcada, o InstallerClean consulta o github em busca de uma versão mais recente quando você o executa.`,
  'Tooltip.MoveSameDrive': `Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them.`,
  'Completion.MoveRestoreHint.Singular': `The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely).`,
  'Completion.MoveRestoreHint.Plural': `The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely).`,
  'Completion.MoveRestoreHintSameDrive.Singular': `The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely).`,
  'Completion.MoveRestoreHintSameDrive.Plural': `The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely).`,
  'Confirm.DeletePermanently.Singular': `This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead.`,
  'Confirm.DeletePermanently.Plural': `Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead.`,
};

// Satellite-only .One override(s). NOT in the neutral; appended before </root>.
// check-resx-parity.mjs allows each because its base (the .Plural sibling, or the
// flat key itself) is in the neutral, and the value's {N} set matches the base's.
// pt counts 0 and 1 as "one" (CategoryFor's fr/pt branch), so .One fires at 0 and 1.
// The three Cli completion lines inflect the past participle at one; the attributive
// Status.RegisteredPackagesFound agrees too. Progress gerunds (Deleting/Moving) do not.
// Completion.ReverifySkipped is chosen on the very count it prints, so its participle
// (mantido/mantidos) and the dele/deles pronoun both take a singular .One. The three
// *CancelledSummary keys do NOT get an override: their form is selected on the total
// ({1}) but any participle would have to agree with the acted-on count ({0}), a
// different number, so they use a count-invariant "X cancelada apos <infinitive>
// {0} de {1} {2}" that reads correctly at every count.
const OVERRIDES = {
  'Status.RegisteredPackagesFound.One': `Foi encontrado {0} {1} registrado.`,
  'Cli.FoundOrphans.One': `Encontrado {0} {1} para limpar ({2}).`,
  'Cli.DeletedFiles.One': `Excluído {0} {1}.`,
  'Cli.MovedFiles.One': `Movido {0} {1}.`,
  'Completion.ReverifySkipped.One': `{0} {1} mantido no lugar: um programa voltou a precisar dele depois da análise.`,
  // Participle agreement only: "mantido" for a single file.
  'Completion.ReverifyIncomplete.One': `{0} {1} mantido no lugar: não foi possível ler por completo os registros do Windows Installer quando a verificação foi repetida.`,
};

let text = readFileSync(BASE, 'utf8');

// Remove ONLY the 20 machine-contract Cli.* <data> elements BY NAME (the
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
console.log('machine Cli <data> removed:', cliMachineRemoved, '(expect 20)');
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
  !overrideMissing.length && !overrideArityMismatch.length &&
  output.size === neutralRequired.length + overrideKeys.length && cliMachineRemoved === 20 && crlf === 0;
const ok = structuralOk && !untranslated.length;
console.log(ok ? '\nGENERATION OK' : '\nGENERATION HAS ISSUES (see above)');
