#!/usr/bin/env node
// Brazilian Portuguese (pt-BR) satellite generator for InstallerClean. Forked from
// gen-strings-template.mjs and reconciled to the CLI-aware pattern (model:
// gen-strings-de.mjs). It works FROM THE ENGLISH SOURCE (Strings.resx): it reads
// the neutral as the structural base, strips ONLY the machine-contract Cli.*
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

// Universal keeps: keys whose value is the same in every language, the brand names
// and the pure-placeholder announcement string. Their still-English value is NOT a
// miss. Explicit by KEY on purpose: a future brand key then defaults to "flag until
// someone adds it here", never silently passes. Do NOT translate these values. Do
// NOT edit this list per language.
//
// The four size suffixes and the two elapsed suffixes were in this list until
// 2026-08-26 and do not belong in it, because they are not universal: French writes
// Go/Mo/Ko/o, Russian and Ukrainian write ГБ/МБ/КБ/Б and мс/с. Those three carry real
// values in their MAP; the languages that do abbreviate as English does keep them in
// ALSO_KEEP, which is the per-language list. Display.ListSeparator is the same shape,
// for the same reason.
const KEEP_ENGLISH = new Set([
  'Window.Main.Title',                 // InstallerClean
  'Startup.AlreadyRunningTitle',       // InstallerClean
  'Startup.UnhandledTitle',            // InstallerClean
  'Automation.ScanResultAnnouncement', // {0} ({1})
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
  // The list separator Portuguese uses is the one English uses. A punctuation
  // mark rather than a word, so there is nothing to translate and nothing to
  // get wrong; only ja and zh-Hans differ, taking the ideographic comma.
  'Display.ListSeparator',       // ", "
  // The size and elapsed unit suffixes. Brazilian Portuguese abbreviates them exactly as
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

const MAP = {
  // Window titles
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `Sobre`,
  'Window.Registered.Title': `Arquivos deixados de lado`,
  'Window.Orphaned.Title': `Arquivos desnecessários que podem ser excluídos com segurança`,

  // Section headings
  // Section.Registered.Products and Automation.Section.Products were removed from
  // this map on 2026-08-21. They left the neutral resx at f49b795b, when the
  // registered-files window stopped having a products group of its own, and stayed
  // here and in all fifteen satellites, so every round regenerated two keys the app
  // cannot use and check-resx-parity reported them as strays in every language.
  'Section.Registered.Patches': `PATCHES`,
  'Section.Registered.Details': `DETALHES DO PRODUTO`,
  'Section.Backup.Folder': `PASTA DE BACKUP`,
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
  'Action.BackupFolderPlaceholder': `Caminho da pasta se você mover em vez de excluir.`,
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
  'Automation.ConfirmDelete': `Excluir permanentemente remove os arquivos desnecessários. Cancelar fecha sem excluir nada.`,
  'Automation.ConfirmMove': `Mover coloca os arquivos desnecessários na pasta de destino escolhida. Cancelar os deixa onde estão.`,
  'Automation.SayThanks': `Agradeça`,
  'Automation.ConfirmSendResultLog': `Enviar transmite ao No Faff o relatório exibido. Cancelar não envia nada.`,
  'Automation.CheckForUpdates': `Verificar atualizações`,
  'Automation.CheckForUpdates.HelpText': `Consulta a página de versões do github em busca de uma versão mais recente.`,
  'Automation.UpdateAvailable.HelpText': `Abra a página da versão para baixar a versão mais recente, ou cancele para manter a versão atual.`,
  'Automation.Licence.HelpText': `Abre o arquivo da licença em github.com no seu navegador.`,
  'Automation.Section.BackupFolder': `Pasta de backup`,
  'Automation.Section.Patches': `Patches`,
  'Automation.Section.ProductDetails': `Detalhes do produto`,
  'Automation.BackupFolder': `Pasta de backup`,
  'Automation.OperationProgress': `Progresso da operação`,
  'Automation.RescanInstaller': `Analisar {InstallerFolder} novamente`,
  'Automation.ScanningProgress': `Progresso da análise`,
  'Automation.StartupScanProgress': `Progresso da análise inicial`,
  'Automation.ViewOrphanedFiles': `Detalhes, arquivos desnecessários`,
  'Automation.ViewOrphanedFiles.HelpText': `Disponíveis para limpeza.`,
  'Automation.ViewRegisteredFiles': `Detalhes, arquivos deixados de lado`,
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
  'Tooltip.Move': `Move os arquivos desnecessários para a pasta de backup. Exclua essa pasta quando estiver convencido de que nada precisa deles.`,
  'Tooltip.MoveNeedsDestination': `Move os arquivos desnecessários para uma pasta de backup. Você a escolhe em seguida. Exclua essa pasta quando estiver convencido de que nada precisa deles.`,
  'Tooltip.Delete': `Delete the unneeded files permanently. They're safe to delete, and you'll reclaim the space straight away.`,
  'Tooltip.SigningCertificate': `Nome do titular do certificado Authenticode incorporado. Cadeia não verificada.`,

  // Body copy
  'Body.MainExplanation.Lead': `Qualquer arquivo desnecessário abaixo pode ser [excluído com segurança].`,
  'Body.MainExplanation.Why': `Eles ficam em {InstallerFolder}. O InstallerClean pergunta ao Windows sobre cada programa instalado: um arquivo entra na lista quando nenhum programa o reivindica ({0}), ou quando um patch mais novo o substituiu e nenhum programa poderia voltar a ele ({1}).`,
  'Body.MainExplanation.Action': `Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update and uninstall as normal. If anything does go wrong, put them back into {InstallerFolder} under the names they had. Or delete them permanently now.`,
  'Body.PendingReboot.MsiExecuteMutex': `Algo está usando o Windows Installer neste momento, como uma atualização do Windows ou um programa instalando em segundo plano. Mover e Excluir ficam pausados enquanto isso acontece, para que o InstallerClean não toque em {InstallerFolder} enquanto ela muda. Quando terminar, reanalise e eles voltam.`,
  'Body.PendingReboot.InstallerInProgress': `Há uma transação anterior do Windows Installer suspensa nesta máquina. Retome ou reverta essa instalação (ou reinicie o Windows) antes de limpar {InstallerFolder}.`,
  'Body.PendingReboot.PendingRenameInCache': `O Windows tem na fila para a próxima reinicialização uma renomeação de arquivo que afeta {InstallerFolder}. Reinicie o Windows antes de limpar.`,
  'Body.NoFileSelected': `Selecione um arquivo para ver os detalhes.`,
  'Body.NoProductSelected': `Selecione um produto para ver os detalhes.`,
  'Body.NoMetadata': `Nenhum metadado disponível.`,
  'Body.RegisteredMissingFromDisk': `This installer file is missing. It causes no trouble now, and won't until the day you try to update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.\n\nTo put it back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it.`,
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
  'Status.Moving': `Movendo arquivos desnecessários...`,
  'Status.Deleting': `Excluindo arquivos desnecessários...`,
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
  'Completion.PermanentDeleteSummary.Singular': `{0} {1} excluído permanentemente`,
  'Completion.PermanentDeleteSummary.Plural': `{0} {1} excluídos permanentemente`,

  // Summaries
  'Summary.RegisteredStillUsed.Singular': `{0} arquivo deixado de lado`,
  'Summary.RegisteredStillUsed.Plural': `{0} arquivos deixados de lado`,
  'Summary.OrphanedToCleanUp.Singular': `{0} arquivo desnecessário para limpar`,
  'Summary.OrphanedToCleanUp.Plural': `{0} arquivos desnecessários para limpar`,
  'Summary.NothingListed.Singular': `Neste PC o InstallerClean não conseguiu ter certeza de quais arquivos em cache pertencem aos programas instalados aqui, então reteve o único arquivo em vez de listá-lo.`,
  'Summary.NothingListed.Plural': `Neste PC o InstallerClean não conseguiu ter certeza de quais arquivos em cache pertencem aos programas instalados aqui, então reteve {0} {1} em vez de listá-los.`,
  'Summary.MissingFromDisk.Singular': `Windows has a record for {0} file that isn't in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. Open Details for what to do.`,
  'Summary.MissingFromDisk.Plural': `Windows has records for {0} files that aren't in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. Open Details for what to do.`,
  'Summary.MissingFromDisk.OtherPrograms.Singular': `{0} outro programa`,
  'Summary.MissingFromDisk.OtherPrograms.Plural': `{0} outros programas`,
  'Summary.MissingFromDisk.Unnamed.Singular': `{0} arquivo sem nenhum programa nomeado nos registros`,
  'Summary.MissingFromDisk.Unnamed.Plural': `{0} arquivos sem nenhum programa nomeado nos registros`,

  // 0 = current file count, 1 = total count, 2 = pluralised noun.
  'Summary.OperationFiles': `{0} de {1} {2}`,

  // 0 = orphaned count, 1 = superseded count, 2 = obsoleted count, 3 = size display
  'Summary.OrphanedWindow': `{0} {1} para limpar ({2})`,
  // 0 = count, 1 = size display. Singular/plural split so the noun and verb agree.
  'Summary.RegisteredWindow.Singular': `{0} arquivo deixado de lado ({1})`,
  'Summary.RegisteredWindow.Plural': `{0} arquivos deixados de lado ({1})`,

  // Confirmation dialogs

  // 0 = file count, 1 = pluralised "file"/"files", 2 = size display
  'Confirm.MoveTitle': `Mover {0} {1} ({2})?`,

  // 0 = destination path
  'Confirm.MoveDestination': `Mover para:`,
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
  'Error.DestinationInSystemFolder': `O destino {0} é resolvido dentro de uma pasta de sistema do Windows. Escolha um caminho fora de %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% e %ProgramData%.`,
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
  'Error.FileInUse.Singular': `Este arquivo está aberto ou bloqueado por outro programa, então nada consegue removê-lo agora. Ele foi deixado no lugar; tente mais tarde.`,
  'Error.FileInUse.Plural': `Estes arquivos estão abertos ou bloqueados por outro programa, então nada consegue removê-los agora. Eles foram deixados no lugar; tente mais tarde.`,
  'Error.IOFailure.Singular': `O Windows relatou um erro de arquivo; o arquivo foi deixado onde estava.`,
  'Error.IOFailure.Plural': `O Windows relatou erros de arquivo; estes arquivos foram deixados onde estavam.`,
  'Error.UnknownError.Singular': `Algo deu errado com este arquivo; ele foi deixado onde estava.`,
  'Error.UnknownError.Plural': `Algo deu errado com estes arquivos; eles foram deixados onde estavam.`,

  // 0 = shell error code

  // 0 = destination
  'Error.MoveIntoInstaller': `Recusando mover arquivos para a pasta do Windows Installer (destino: {0}).`,

  // 0 = the relative path the caller passed
  'Error.DestinationNotFullyQualified': `A pasta de backup precisa ser um caminho completo até uma pasta, começando por uma letra de unidade ou um compartilhamento de rede (por exemplo D:\\Backup, ou \\\\servidor\\backup). O InstallerClean não pode usar esta: {0}`,
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
  'Error.DestinationChangedMidBatch': `O InstallerClean não pôde mais confirmar a pasta de backup, então parou em vez de gravar no lugar errado. Verifique {0}, depois Reanalisar e tente de novo.`,
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
  'Display.ListSeparator': `, `,
  'Display.ElapsedLong.LessThanASecond': `menos de um segundo`,
  'Display.ElapsedLong.Seconds': `{0:F1} segundos`,
  'CrashLog.PrivacyHeader': `# O crash.log registra exceções não tratadas do InstallerClean.\n# Com privilégios elevados, as mensagens de exceção do framework podem\n# incluir caminhos de arquivo da sessão em execução (inclusive perfis\n# de outros usuários enumerados pelas consultas do Windows Installer).\n# Mensagens de falha de rede da verificação de atualizações ou do envio\n# do log de resultados podem incluir a URL de destino e o IP ou proxy\n# resolvido. Entradas sobre registros ilegíveis do Windows Installer\n# podem incluir um SID de conta do Windows (S-1-5-21-...) e os códigos\n# de produto do software instalado.\n# Remova os três tipos de dado antes de anexar este arquivo a um\n# relatório de erro público.\n`,
  'Tooltip.ChangeLanguage': `Alterar idioma. O programa será reiniciado.`,
  'Automation.ChangeLanguage': `Alterar idioma`,
  'Automation.ChangeLanguage.HelpText': `O programa será reiniciado.`,

  // Command-line tool (installerclean-cli): the HUMAN-facing Cli.* keys.
  // The machine-contract Cli.EventLog* keys (bar EventLogUnavailable) are
  // stripped and stay English at the emit site; they are not in this MAP.
  'Cli.UnknownArgument': `Erro: argumento desconhecido '{0}'`,
  'Cli.Cancelling': `Cancelando...`,
  'Cli.Cancelled': `Cancelado.`,
  'Cli.GenericError': `Erro: falha inesperada ({0}). Detalhes gravados em {1}.`,
  'Cli.GenericError.NoLog': `Erro: falha inesperada ({0}). Não foi possível gravar o log de falhas.`,
  'Cli.ScanningInstaller': `Analisando {InstallerFolder}...`,
  'Cli.FoundOrphans': `Foram encontrados {0} {1} desnecessários para limpar ({2}).`,
  'Cli.DeletingFiles': `Excluindo {0} {1} desnecessários...`,
  'Cli.DeletedFiles': `Foram excluídos permanentemente {0} {1} desnecessários.`,
  'Cli.NoMoveDestination': `Erro: nenhum destino de movimentação especificado. Use /m CAMINHO. (Um padrão definido na GUI é por usuário e não se aplica a execuções agendadas ou em contas de serviço.)`,
  'Cli.MoveDestinationInsideInstaller': `Erro: o destino não pode estar dentro da pasta do Windows Installer.`,
  'Cli.MoveDestinationRelative': `Erro: o destino deve ser um caminho totalmente qualificado. Recebido: {0}`,
  'Cli.MoveDestinationInSystemFolder': `Erro: o destino {0} é resolvido dentro de uma pasta de sistema do Windows. Escolha um caminho fora de %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% e %ProgramData%.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Erro: algo está usando o Windows Installer neste momento, como uma atualização do Windows ou um programa instalando em segundo plano. /m e /d ficam bloqueados enquanto isso acontece. Tente de novo quando terminar.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Erro: há uma transação anterior do Windows Installer suspensa nesta máquina. Retome ou reverta essa instalação (ou reinicie o Windows) antes de limpar {InstallerFolder}.`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Erro: uma operação de arquivo na fila para depois da reinicialização atinge {InstallerFolder} ({0}). Reinicie o Windows para concluir essa operação antes de limpar.`,
  'Cli.MovingFiles': `Movendo {0} {1} desnecessários para {2}...`,
  'Cli.MovedFiles': `Foram movidos {0} {1} desnecessários.`,
  'Cli.MutexBlocked': `Outro processo do InstallerClean mantém o bloqueio de instância única (a GUI ou outra execução da CLI). Código de saída 75 (transitório); seguro tentar novamente mais tarde.`,
  'Cli.EventLogUnavailable': `Observação: falha ao gravar no Log de Eventos. Verifique as permissões do log de Aplicativo ou a Diretiva de Grupo.`,
  'Cli.Help.Header': `InstallerClean - limpeza de {InstallerFolder}`,
  'Cli.Help.Usage': `Uso:`,
  'Cli.Help.Help': `  installerclean-cli --help      Mostra esta ajuda (aceita também /?, -h)`,
  'Cli.Help.Version': `  installerclean-cli --version   Mostra a versão (aceita também -v)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s          Somente analisar - lista os supérfluos`,
  'Cli.Help.Delete': `  installerclean-cli /d          Exclui permanentemente os supérfluos`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m          Move para a pasta de backup salva`,
  'Cli.Help.MovePath': `  installerclean-cli /m CAMINHO  Move para o caminho especificado`,
  'Cli.Help.NoteLine1': `O installerclean-cli bloqueia o prompt até terminar, para que um script&#10;ou uma tarefa agendada possa esperar por ele.`,
  'Cli.Help.ExitCodesHeader': `Códigos de saída:`,
  'Cli.Help.ExitCodeOk': `  0   êxito: a execução fez o que foi pedido e nada falhou`,
  'Cli.Help.ExitCodeError': `  1   falha: nada processado (argumentos ou destino inválidos, uma&#10;       análise com falha ou todos os arquivos com falha)`,
  'Cli.Help.ExitCodePartial': `  2   parcial: alguns processados, outros não (falha ou Ctrl+C)`,
  'Cli.Help.ExitCodeTransient': `  75  transitório: algo temporário bloqueou a execução (veja a mensagem)`,
  'Cli.Help.ExitCodeCancelled': `  130 cancelado (Ctrl+C)`,
  'Body.NotScanned.Lead': `Nada foi analisado ainda.`,
  'Body.NotScanned.Why': `Clique em Reanalisar para procurar em {InstallerFolder} arquivos de instalação que nenhum programa ainda precisa.`,
  'Confirm.MoveSameDrive': `Essa pasta está na mesma unidade, então o espaço não volta enquanto você não excluí-la. Escolha uma pasta em outra unidade se quiser o espaço na hora.`,
  'Error.ScanCorrelationFailed': `O InstallerClean não conseguiu casar os registros do Windows Installer com o conteúdo de {InstallerFolder}. Quase nada do que os registros apontam está de fato lá, e quase nada do que está lá é nomeado por algum registro, então não foi possível mostrar que algum arquivo fosse desnecessário. Nada foi oferecido e nada foi removido.`,
  'Error.CandidateOutsideCache': `Este arquivo não está diretamente dentro da pasta do Windows Installer; recusado por segurança.`,
  'Completion.ReverifySkipped': `{0} {1} mantidos no lugar, porque os registros agora reivindicam o que a análise havia sinalizado.`,
  'Completion.MoveCancelledSummary': `Movimentação cancelada após mover {0} de {1} {2}.`,
  'Completion.PermanentDeleteCancelledSummary': `Exclusão permanente cancelada após remover {0} de {1} {2}.`,
  'Body.PendingReboot.Lead': `Estes arquivos não podem ser limpos agora.`,
  'Cli.TooManyArguments': `Erro: argumento extra inesperado '{0}'. Se a sua pasta de destino tiver um espaço no nome, coloque aspas em todo o caminho: /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `Pasta salva por usuário; execuções agendadas ou SYSTEM: /m CAMINHO.`,
  'Completion.ReverifyIncomplete': `{0} {1} mantidos no lugar, porque os registros do Windows Installer não puderam ser lidos por completo na verificação final.`,
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
  'Tooltip.MoveSameDrive': `Move os arquivos desnecessários para a pasta de backup. Ela está na mesma unidade, então você só recupera o espaço quando excluir essa pasta ou movê-la para outra unidade. Pode fazer isso quando estiver convencido de que nada precisa deles.`,
  'Completion.MoveRestoreHint.Singular': `The file in that folder is [safe to delete], so remove the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely).`,
  'Completion.MoveRestoreHint.Plural': `The files in that folder are [safe to delete], so remove it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely).`,
  'Completion.MoveRestoreHintSameDrive.Singular': `The file in that folder is [safe to delete], so remove the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely).`,
  'Completion.MoveRestoreHintSameDrive.Plural': `The files in that folder are [safe to delete], so remove it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely).`,
  'Confirm.DeletePermanently.Singular': `Este arquivo será excluído permanentemente. Ele é [seguro para excluir], mas se você quiser uma cópia, use o botão Mover.`,
  'Confirm.DeletePermanently.Plural': `These files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead.`,
  'Error.ScanCacheRootUnresolved': `O InstallerClean não conseguiu que o Windows resolvesse o caminho verdadeiro de {InstallerFolder}, então não foi possível mostrar que algum arquivo estivesse dentro dela e nenhum foi oferecido para limpeza. Esta análise não encontrou nada porque essa verificação falhou, não porque a pasta esteja limpa. Nada foi removido.`,
  'Automation.Scroll.ProductDetails': `Detalhes do produto`,
  'Body.PendingReboot.Other': `O Windows Installer tem algo em andamento, então Mover e Excluir ficam pausados. O InstallerClean não vai tocar em {InstallerFolder} enquanto ela muda. Quando terminar, reanalise e eles voltam.`,
  'Cli.TooManyArgumentsNoPath': `Erro: argumento extra inesperado '{0}'. /s e /d não aceitam mais argumentos, e só se pode usar um sinalizador por execução.`,
  'Cli.MissingFromDisk.Singular': `Windows has a record for {0} file that is not in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. To put the file back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This usually restores the file, but Microsoft doesn't guarantee it.`,
  'Cli.MissingFromDisk.Plural': `Windows has records for {0} files that are not in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. To put a file back, you need the installer for the version you already have of that program. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs the file. Uninstalling first won't work either, for the same reason. This usually restores the file, but Microsoft doesn't guarantee it.`,
  'Cli.MoveNotEnoughSpace': `Erro: espaço insuficiente em {0}. Mover estes arquivos precisa de {1} e há {2} livres. Nada foi movido.`,
  'Cli.PendingRebootBlocked.Other': `Erro: o Windows Installer tem algo em andamento, então /m e /d ficam bloqueados. O InstallerClean não vai tocar em {InstallerFolder} enquanto ela muda. Tente de novo quando terminar.`,
  'Cli.FoundNoOrphans': `Nenhum arquivo desnecessário encontrado.`,
  'Cli.NothingOffered.Singular': `O InstallerClean não conseguiu ter certeza de quais arquivos em cache pertencem aos programas instalados aqui, então reteve o único arquivo ({2}) que poderia ter oferecido.`,
  'Cli.NothingOffered.Plural': `O InstallerClean não conseguiu ter certeza de quais arquivos em cache pertencem aos programas instalados aqui, então reteve todos os {0} {1} ({2}) que poderia ter oferecido.`,
  'Cli.DestinationChangedMidBatch': `O InstallerClean não pôde mais confirmar a pasta de backup, então parou em vez de gravar no lugar errado. Verifique {0} e execute o comando de novo.`,
  'Cli.Help.Summary': `Remove arquivos .msi/.msp em cache que nenhum programa instalado precisa.`,
  'Cli.Help.Elevation': `Exige um prompt como administrador; o Windows não vai iniciá-lo.`,
  'Error.InstallerLockUnavailableTitle': `Nada foi excluído`,
  'Error.MoveInstallerLockUnavailableTitle': `Nada foi movido`,
  'Error.InstallerLockUnavailable': `O InstallerClean não conseguiu obter o bloqueio que o Windows Installer usa para impedir que dois programas alterem software instalado ao mesmo tempo, então não pôde descartar que um arquivo se tornasse necessário no meio do caminho, e nada foi excluído. Tente de novo, e reinicie o Windows se continuar acontecendo.`,
  'Error.MoveInstallerLockUnavailable': `O InstallerClean não conseguiu obter o bloqueio que o Windows Installer usa para impedir que dois programas alterem software instalado ao mesmo tempo, então não pôde descartar que um arquivo se tornasse necessário no meio do caminho, e nada foi movido. Tente de novo, e reinicie o Windows se continuar acontecendo.`,
  'Cli.InstallerLockUnavailable': `Erro: o InstallerClean não conseguiu obter o bloqueio do Windows Installer que impede que dois programas alterem software instalado ao mesmo tempo, então não pôde descartar que um arquivo se tornasse necessário no meio do caminho. Nada foi excluído. Tente de novo, e reinicie o Windows se continuar acontecendo.`,
  'Cli.MoveInstallerLockUnavailable': `Erro: o InstallerClean não conseguiu obter o bloqueio do Windows Installer que impede que dois programas alterem software instalado ao mesmo tempo, então não pôde descartar que um arquivo se tornasse necessário no meio do caminho. Nada foi movido. Tente de novo, e reinicie o Windows se continuar acontecendo.`,
  'Completion.ReverifyRecordsChanged': `{0} {1} mantidos no lugar, porque os registros do Windows Installer haviam mudado até a verificação final.`,
  'Completion.ReverifyIdentityClaimed': `{0} {1} mantidos no lugar, porque o Windows tem um registro do programa nomeado lá dentro.`,
  'Completion.ReverifyIdentityUnreadable': `{0} {1} mantidos no lugar, porque o InstallerClean não encontrou nenhum programa nomeado lá dentro.`,
  'Completion.ReverifyOwnershipUnestablished': `{0} {1} mantidos no lugar, porque até a verificação final o InstallerClean não conseguiu ter certeza de quais arquivos em cache pertencem aos programas instalados aqui.`,
  'Completion.NothingRemoved': `Nada foi removido`,
  'Error.ScanNoRegisteredFileInFolder': `O InstallerClean não conseguiu casar os registros do Windows Installer com o conteúdo de {InstallerFolder}. A pasta tem arquivos, mas nenhum registro aponta para nada lá dentro, então não foi possível mostrar que algum arquivo fosse desnecessário. Nada foi oferecido e nada foi removido.`,
  'Completion.NothingOffered': `Nada oferecido neste PC`,
  'Completion.NothingOfferedBody.Singular': `O InstallerClean não conseguiu ter certeza de quais arquivos em cache pertencem aos programas instalados aqui, então reteve o único arquivo ({2}) que poderia ter oferecido.`,
  'Completion.NothingOfferedBody.Plural': `O InstallerClean não conseguiu ter certeza de quais arquivos em cache pertencem aos programas instalados aqui, então reteve todos os {0} {1} ({2}) que poderia ter oferecido.`,
  'Summary.SupersededHeldBack.Singular': `On this PC InstallerClean couldn't be certain that the one superseded file is no longer needed, so it has held it back.`,
  'Summary.SupersededHeldBack.Plural': `On this PC InstallerClean couldn't be certain that {0} superseded files are no longer needed, so it has held them back.`,
  'Cli.SupersededHeldBack.Singular': `On this PC InstallerClean couldn't be certain that the one superseded file is no longer needed, so it has held it back.`,
  'Cli.SupersededHeldBack.Plural': `On this PC InstallerClean couldn't be certain that {0} superseded files are no longer needed, so it has held them back.`,
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
  'Cli.FoundOrphans.One': `Foi encontrado {0} {1} desnecessário para limpar ({2}).`,
  'Cli.DeletingFiles.One': `Excluindo {0} {1} desnecessário...`,
  'Cli.DeletedFiles.One': `Foi excluído permanentemente {0} {1} desnecessário.`,
  'Cli.MovingFiles.One': `Movendo {0} {1} desnecessário para {2}...`,
  'Cli.MovedFiles.One': `Foi movido {0} {1} desnecessário.`,
  'Completion.ReverifySkipped.One': `{0} {1} mantido no lugar, porque os registros agora reivindicam o que a análise havia sinalizado.`,
  'Completion.ReverifyRecordsChanged.One': `{0} {1} mantido no lugar, porque os registros do Windows Installer haviam mudado até a verificação final.`,
  // Participle agreement only: "mantido" for a single file.
  'Completion.ReverifyIncomplete.One': `{0} {1} mantido no lugar, porque os registros do Windows Installer não puderam ser lidos por completo na verificação final.`,
  'Completion.ReverifyOwnershipUnestablished.One': `{0} {1} mantido no lugar, porque até a verificação final o InstallerClean não conseguiu ter certeza de quais arquivos em cache pertencem aos programas instalados aqui.`,
  // Completion.ReverifyIdentityUnreadable.One was added and removed again in the 3.0.0 round. Its base is
  // one of the two retired identity causes: no code reads it, so nothing passes
  // the prefix to Pluralise and the override could never be selected.
  // CountedStringTests.Every_satellite_override_belongs_to_a_counted_prefix is
  // what says so. The base string itself stays translated, which is the point of
  // keeping those two keys at all.
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
