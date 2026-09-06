# InstallerClean in Português (Brasil) (Brazilian Portuguese)

The text of InstallerClean's interface and command-line tool in English on the left, with the Brazilian Portuguese translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Brazilian Portuguese can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.pt-BR.resx`](../../src/InstallerClean.Core/Resources/Strings.pt-BR.resx), so do not edit it by hand. The Brazilian Portuguese translation itself lives in [`gen-strings-pt-BR.mjs`](../../scripts/translations/gen-strings-pt-BR.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | Português (Brasil) |
| --- | --- |
| InstallerClean | InstallerClean |
| About | Sobre |
| Files left alone | Arquivos deixados de lado |
| Unneeded files that are safe to delete | Arquivos desnecessários que podem ser excluídos com segurança |

## Section headings

| English | Português (Brasil) |
| --- | --- |
| PATCHES | PATCHES |
| PRODUCT DETAILS | DETALHES DO PRODUTO |
| BACKUP FOLDER | PASTA DE BACKUP |
| SAY THANKS | AGRADEÇA |

## Buttons and actions

| English | Português (Brasil) |
| --- | --- |
| _About | _Sobre |
| Copy | Copiar |
| Cut | Recortar |
| Paste | Colar |
| Select all | Selecionar tudo |
| _Browse... | _Procurar... |
| _Cancel | _Cancelar |
| Check for _updates | Verificar _atualizações |
| _Close | _Fechar |
| _Delete permanently | _Excluir permanentemente |
| _Done | _Concluído |
| Details | Detalhes |
| _Buy me a cuppa | Me paga um _café |
| Leave a _star on GitHub | _Deixe uma estrela no GitHub |
| Apache 2.0 licence | Licença Apache 2.0 |
| _Move | _Mover |
| Path to folder if you move rather than delete. | Caminho da pasta se você mover em vez de excluir. |
| Open _release page | Abrir a página da _versão |
| _Re-scan | _Reanalisar |
| _Scan again | Analisar de _novo |
| Send report | Enviar relatório |
| _Send | _Enviar |

## About window

| English | Português (Brasil) |
| --- | --- |
| Guide and FAQ | Guia e perguntas frequentes |
| Report a problem | Relatar um problema |
| Check for updates automatically | Verificar atualizações automaticamente |

## Field labels

| English | Português (Brasil) |
| --- | --- |
| Reason | Motivo |
| Author | Autor |
| Application | Aplicativo |
| Title | Título |
| Subject | Assunto |
| Keywords | Palavras-chave |
| Signing certificate | Certificado de assinatura |
| File size | Tamanho do arquivo |
| Comment | Comentário |
| Product name | Nome do produto |
| File | Arquivo |
| Size | Tamanho |
| Patches | Patches |
| (unknown) | (desconhecido) |
| (no program) | (sem programa) |
| (patches only) | (apenas patches) |
| missing | ausente |

## Status and progress

| English | Português (Brasil) |
| --- | --- |
| Scanning... | Analisando... |
| Cancelling... | Cancelando... |
| Starting scan... | Iniciando a análise... |
| Asking Windows about installed software... | Consultando o Windows sobre o software instalado... |
| Scanning installer cache folder... | Analisando a pasta do cache de instalação... |
| Enumerating installed products... | Enumerando os produtos instalados... |
| Checking registry for additional packages... | Verificando o registro em busca de pacotes adicionais... |
| Found {0} registered {1}. | Foram encontrados {0} {1} registrados. |
| Scan complete ({0}) | Análise concluída ({0}) |
| Scanning local packages... | Analisando os pacotes locais... |
| Found {0} {1} you can safely delete. | Encontrados {0} {1} que você pode excluir com segurança. |
| Preparing destination folder... | Preparando a pasta de destino... |
| Moving unneeded files... | Movendo arquivos desnecessários... |
| Deleting unneeded files... | Excluindo arquivos desnecessários... |
| Move cancelled. {0} of {1} {2} processed. | Movimentação cancelada após processar {0} de {1} {2}. |
| Delete cancelled. {0} of {1} {2} processed. | Exclusão cancelada após processar {0} de {1} {2}. |
| {0}. Details are in {1}. | {0}. Detalhes em {1}. |
| {0}. The crash log could not be written. | {0}. Não foi possível gravar o crash.log. |
| {0}. Details are in {1}. | {0}. Detalhes em {1}. |
| {0}. The crash log could not be written. | {0}. Não foi possível gravar o crash.log. |
| Access denied. Windows refused the scan. | Acesso negado. O Windows recusou a análise. |
| Scan failed: couldn't read the Windows Installer records. | Falha na análise: não foi possível ler os registros do Windows Installer. |
| Scan cancelled. | Análise cancelada. |
| Ready | Pronto |
| Scan failed ({0}). Details in {1}. | Falha na análise ({0}). Detalhes em {1}. |
| Scan failed ({0}). The crash log could not be written. | Falha na análise ({0}). Não foi possível gravar o crash.log. |

## Main screen text

| English | Português (Brasil) |
| --- | --- |
| Any unneeded files below are [safe to delete]. | Qualquer arquivo desnecessário abaixo pode ser [excluído com segurança]. |
| They sit in {InstallerFolder}. InstallerClean asks Windows about every installed program: a file is listed when no program claims it ({0}), or when a newer patch has replaced it and no program could roll back to it ({1}). | Eles ficam em {InstallerFolder}. O InstallerClean pergunta ao Windows sobre cada programa instalado: um arquivo entra na lista quando nenhum programa o reivindica ({0}), ou quando um patch mais novo o substituiu e nenhum programa poderia voltar a ele ({1}). |
| Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update and uninstall as normal. Putting them back into {InstallerFolder} restores everything. Or delete them permanently now. | Mova-os para uma pasta de backup que você escolher e depois exclua essa pasta quando estiver convencido de que seus programas ainda atualizam e desinstalam normalmente. Colocá-los de volta em {InstallerFolder} restaura tudo. Ou exclua-os permanentemente agora. |
| Nothing scanned yet. | Nada foi analisado ainda. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Clique em Reanalisar para procurar em {InstallerFolder} arquivos de instalação que nenhum programa ainda precisa. |
| These files can't be cleaned up right now. | Estes arquivos não podem ser limpos agora. |
| Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Algo está usando o Windows Installer neste momento, como uma atualização do Windows ou um programa instalando em segundo plano. Mover e Excluir ficam pausados enquanto isso acontece, para que o InstallerClean não toque em {InstallerFolder} enquanto ela muda. Quando terminar, reanalise e eles voltam. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Há uma transação anterior do Windows Installer suspensa nesta máquina. Retome ou reverta essa instalação (ou reinicie o Windows) antes de limpar {InstallerFolder}. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | O Windows tem na fila para a próxima reinicialização uma renomeação de arquivo que afeta {InstallerFolder}. Reinicie o Windows antes de limpar. |
| A file operation is queued for the next restart and InstallerClean can't tell which files it names, so it can't rule out that they're in {InstallerFolder}. Restart Windows before cleaning. | Há uma operação de arquivos na fila para a próxima reinicialização e o InstallerClean não consegue saber quais arquivos ela nomeia, então não dá para descartar que estejam em {InstallerFolder}. Reinicie o Windows antes de limpar. |
| InstallerClean couldn't read one of the Windows settings it checks before touching {InstallerFolder}, so it can't tell whether an installer operation is running or waiting for a restart. Restart Windows and Re-scan. If the setting still won't read, this isn't a machine InstallerClean can clean. | O InstallerClean não conseguiu ler uma das configurações do Windows que verifica antes de tocar em {InstallerFolder}, então não dá para saber se há uma operação de instalação em andamento ou aguardando uma reinicialização. Reinicie o Windows e use Reanalisar. Se a configuração continuar ilegível, este não é um computador que o InstallerClean possa limpar. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | O Windows Installer tem algo em andamento, então Mover e Excluir ficam pausados. O InstallerClean não vai tocar em {InstallerFolder} enquanto ela muda. Quando terminar, reanalise e eles voltam. |
| Select a file to view details. | Selecione um arquivo para ver os detalhes. |
| Select a product to view details. | Selecione um produto para ver os detalhes. |
| No metadata available. | Nenhum metadado disponível. |
| This installer file is missing. It causes no trouble now, and won't until the day you try to update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To put it back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Este arquivo de instalação está faltando. Agora não causa problema nenhum, e não vai causar até o dia em que você tentar atualizar ou desinstalar o programa a que ele pertence. Essa etapa pode falhar então, porque o Windows procura este arquivo e ele não está lá.<br><br>Para recolocá-lo, você precisa do instalador da versão que já tem. Consiga-o com o fabricante do programa e execute-o por cima da sua cópia atual. Uma versão mais nova não serve: ela teria que remover primeiro a que você tem, e é justamente essa etapa que precisa deste arquivo. Desinstalar primeiro também não funciona, pelo mesmo motivo. Isso deve restaurar o arquivo e deixar suas configurações intactas, mas a Microsoft não garante. |
| The README [explains this folder], and how to recover a file, in Microsoft's own words. | O README [explica esta pasta], e como recuperar um arquivo, com as próprias palavras da Microsoft. |
| (none) | (nenhum) |

## Reasons a file is unneeded

| English | Português (Brasil) |
| --- | --- |
| Orphaned | Órfão |
| Superseded | Substituído |
| Obsoleted | Obsoleto |

## Completion screen

| English | Português (Brasil) |
| --- | --- |
| All clean | Tudo limpo |
| Nothing to clean up in {InstallerFolder} | Nada para limpar em {InstallerFolder} |
| Scanned {0} {1} in {2} | Análise de {0} {1} em {2} |
| Nothing offered on this PC | Nada oferecido neste PC |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({2}) rather than offering it. | O InstallerClean não conseguiu ter certeza de quais arquivos em cache pertencem aos programas instalados aqui, então reteve o único arquivo ({2}) em vez de oferecê-lo. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back all {0} {1} ({2}) rather than offering them. | O InstallerClean não conseguiu ter certeza de quais arquivos em cache pertencem aos programas instalados aqui, então reteve todos os {0} {1} ({2}) em vez de oferecê-los. |
| InstallerClean couldn't establish that the cached file it found is unneeded, so it has held back the one file ({2}) rather than offering it. | O InstallerClean não conseguiu estabelecer que o arquivo em cache que encontrou não é necessário, então reteve esse único arquivo ({2}) em vez de oferecê-lo. |
| InstallerClean couldn't establish that any of the cached files it found are unneeded, so it has held back all {0} {1} ({2}) rather than offering them. | O InstallerClean não conseguiu estabelecer de nenhum dos arquivos em cache que encontrou que não seja necessário, então reteve todos os {0} {1} ({2}) em vez de oferecê-los. |
| Delete that folder when you're satisfied all is well. | Exclua essa pasta quando estiver convencido de que está tudo bem. |
| Delete that folder when you're satisfied all is well. You won't actually reclaim the space until you do. | Exclua essa pasta quando estiver convencido de que está tudo bem. Só então o espaço é realmente recuperado. |
| {0} freed | {0} liberados |
| {0} moved | {0} movidos |
| Nothing was moved | Nada foi movido |
| Nothing was deleted | Nada foi excluído |
| {0} file could not be moved. | {0} arquivo não pôde ser movido. |
| {0} files could not be moved. | {0} arquivos não puderam ser movidos. |
| {0} file could not be deleted. | {0} arquivo não pôde ser excluído. |
| {0} files could not be deleted. | {0} arquivos não puderam ser excluídos. |
| {0} {1} moved to: {2} | {0} {1} movido para: {2} |
| {0} {1} moved to: {2} | {0} {1} movidos para: {2} |
| {0} file held back. The scan said it was unneeded. The final check couldn't confirm that. | {0} arquivo retido. A análise disse que ele não era necessário. A verificação final não pôde confirmar isso. |
| {0} files held back. The scan said these were unneeded. The final check couldn't confirm that. | {0} arquivos retidos. A análise disse que eles não eram necessários. A verificação final não pôde confirmar isso. |
| {0} {1} kept in place, because Windows has a record of the program named inside. | {0} {1} mantidos no lugar, porque o Windows tem um registro do programa nomeado lá dentro. |
| {0} {1} kept in place, because InstallerClean couldn't find a program named inside. | {0} {1} mantidos no lugar, porque o InstallerClean não encontrou nenhum programa nomeado lá dentro. |
| Moved {0} of {1} {2} to {3} before you cancelled. | Movimentação cancelada após mover {0} de {1} {2} para {3}. |
| Permanently deleted {0} of {1} {2} before you cancelled. | Exclusão permanente cancelada após remover {0} de {1} {2}. |
| It's simple to undo. Move them back into {InstallerFolder} and everything will be back to how it was. | É simples de desfazer. Mova-os de volta para {InstallerFolder} e tudo voltará a ser como era. |
| {0} {1} permanently deleted | {0} {1} excluído permanentemente |
| {0} {1} permanently deleted | {0} {1} excluídos permanentemente |
| Glad to help. There's a tip jar if you're feeling kind. | Que bom que ajudou. A caixinha está aqui, se vier do coração. |

## Summaries and counts

| English | Português (Brasil) |
| --- | --- |
| {0} file left alone | {0} arquivo deixado de lado |
| {0} files left alone | {0} arquivos deixados de lado |
| {0} unneeded file to clean up | {0} arquivo desnecessário para limpar |
| {0} unneeded files to clean up | {0} arquivos desnecessários para limpar |
| Windows has a record for {0} file that isn't in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. Open Details for what to do. | O Windows tem um registro de {0} arquivo que não está em {InstallerFolder}: {1}. No dia a dia não causa problema, mas uma atualização ou desinstalação desse programa pode falhar. Abra Detalhes para saber o que fazer. |
| Windows has records for {0} files that aren't in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. Open Details for what to do. | O Windows tem registros de {0} arquivos que não estão em {InstallerFolder}: {1}. No dia a dia não causam problema, mas uma atualização ou desinstalação desses programas pode falhar. Abra Detalhes para saber o que fazer. |
| {0} other program | {0} outro programa |
| {0} other programs | {0} outros programas |
| {0} file with no program named in the records | {0} arquivo sem nenhum programa nomeado nos registros |
| {0} files with no program named in the records | {0} arquivos sem nenhum programa nomeado nos registros |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file rather than offering it. | O InstallerClean não conseguiu ter certeza de quais arquivos em cache pertencem aos programas instalados aqui, então reteve o único arquivo em vez de oferecê-lo. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} rather than offering them. | O InstallerClean não conseguiu ter certeza de quais arquivos em cache pertencem aos programas instalados aqui, então reteve {0} {1} em vez de oferecê-los. |
| InstallerClean couldn't be certain about one of the cached files it found, so it has held that one back rather than offering it. | O InstallerClean não teve certeza sobre um dos arquivos em cache que encontrou, então o reteve em vez de oferecê-lo. |
| InstallerClean couldn't be certain about some of the cached files it found, so it has held back {0} {1} rather than offering them. | O InstallerClean não teve certeza sobre alguns dos arquivos em cache que encontrou, então reteve {0} {1} em vez de oferecê-los. |
| InstallerClean couldn't be certain that the one superseded file is no longer needed, so it has held it back. | O InstallerClean não conseguiu ter certeza de que o único arquivo substituído não é mais necessário, então o reteve. |
| InstallerClean couldn't be certain that {0} superseded files are no longer needed, so it has held them back. | O InstallerClean não conseguiu ter certeza de que {0} arquivos substituídos não são mais necessários, então os reteve. |
| {0} of {1} {2} | {0} de {1} {2} |
| {0} unneeded {1} ({2}) | {0} {1} para limpar ({2}) |
| {0} file left alone ({1}) | {0} arquivo deixado de lado ({1}) |
| {0} files left alone ({1}) | {0} arquivos deixados de lado ({1}) |
| {0} missing | {0} ausente |
| {0} missing | {0} ausentes |

## Confirmation dialogs

| English | Português (Brasil) |
| --- | --- |
| Move {0} {1} ({2})? | Mover {0} {1} ({2})? |
| This file will be moved to: | Este arquivo será movido para: |
| These files will be moved to: | Estes arquivos serão movidos para: |
| Delete {0} {1} ({2})? | Excluir {0} {1} ({2})? |
| This file will be deleted permanently. It's safe to do but if you'd like a backup, use Move instead. | Este arquivo será excluído permanentemente. É seguro fazer isso, mas se você quiser uma cópia, use Mover. |
| These files will be deleted permanently. It's safe to do but if you'd like a backup, use Move instead. | Estes arquivos serão excluídos permanentemente. É seguro fazer isso, mas se você quiser uma cópia, use Mover. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | Essa pasta está na mesma unidade, então o espaço não volta enquanto você não excluí-la. Escolha uma pasta em outra unidade se quiser o espaço na hora. |

## Error messages

| English | Português (Brasil) |
| --- | --- |
| This is also recorded in {0}. | Isso também fica registrado em {0}. |
| Access denied | Acesso negado |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | O Windows negou acesso ao InstallerClean, que por isso parou. Nada foi removido.<br><br>O InstallerClean já estava em execução como administrador, então iniciá-lo de novo dessa forma não vai ajudar. O Windows não diz mais nada sobre o que negou o acesso, então não há nada específico para tentar. |
| Couldn't read the Windows Installer records | Não foi possível ler os registros do Windows Installer |
| Scan failed | Falha na análise |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed. | Os registros do Windows Installer voltaram completamente vazios: nenhum programa instalado e nenhuma atualização reivindica um arquivo de instalação em cache. Isso não acontece em uma máquina que funciona (até uma instalação nova do Windows tem alguns), então ou os registros estão danificados ou não puderam ser lidos, e uma análise que acreditasse nessa resposta chamaria erroneamente de órfão cada arquivo em {InstallerFolder}. Em vez disso, o InstallerClean parou. Nada foi removido. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | O Windows Installer não deixou o InstallerClean listar o que está instalado. O InstallerClean já estava em execução como administrador, então executá-lo de novo como administrador não muda nada. Sem essa lista não há como saber com segurança quais arquivos em cache ainda são necessários, então o InstallerClean parou. Nada foi removido. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: it read {2} {3}, then {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | O Windows Installer não conseguiu dar ao InstallerClean uma lista legível dos programas instalados: ele leu {2} {3} e depois {0} entradas seguidas voltaram ilegíveis (último código de erro {1}). Em vez de trabalhar com uma lista lida pela metade, o InstallerClean parou. Nada foi removido. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | O Windows Installer nunca sinalizou o fim da lista de programas instalados: o InstallerClean leu {2} {3} e depois desistiu após {0} entradas (último código de erro {1}). Não dá para confiar em uma lista sem fim, então o InstallerClean parou. Nada foi removido. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | O Windows Installer nunca sinalizou o fim da lista de patches de um programa: o InstallerClean leu {2} {3} e depois desistiu após {0} entradas (último código de erro {1}). Não dá para confiar em uma lista sem fim, então o InstallerClean parou. Nada foi removido. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | O InstallerClean não conseguiu casar os registros do Windows Installer com o conteúdo de {InstallerFolder}. Quase nada do que os registros apontam está de fato lá, e quase nada do que está lá é nomeado por algum registro, então não foi possível mostrar que algum arquivo fosse desnecessário. Nada foi oferecido e nada foi removido. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | O InstallerClean não conseguiu casar os registros do Windows Installer com o conteúdo de {InstallerFolder}. A pasta tem arquivos, mas nenhum registro aponta para nada lá dentro, então não foi possível mostrar que algum arquivo fosse desnecessário. Nada foi oferecido e nada foi removido. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | O InstallerClean não conseguiu ler o suficiente dos registros do Windows Installer para ter certeza do que ainda é necessário: a lista de programas instalados voltou incompleta, e ler esses mesmos registros direto do registro do Windows também deu erros. Um arquivo poderia parecer órfão só porque o registro que o nomeia era um dos ilegíveis, então o InstallerClean parou. Nada foi removido. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | O InstallerClean não conseguiu que o Windows resolvesse o caminho verdadeiro de {InstallerFolder}, então não foi possível mostrar que algum arquivo estivesse dentro dela e nenhum foi oferecido para limpeza. Esta análise não encontrou nada porque essa verificação falhou, não porque a pasta esteja limpa. Nada foi removido. |
| Nothing was deleted | Nada foi excluído |
| Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. | O Windows negou ao InstallerClean permissão para verificar se o Windows Installer estava ocupado, então não deu para descartar que um arquivo viesse a ser necessário no meio do caminho, e nada foi excluído. |
| Nothing was moved | Nada foi movido |
| Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. | O Windows negou ao InstallerClean permissão para verificar se o Windows Installer estava ocupado, então não deu para descartar que um arquivo viesse a ser necessário no meio do caminho, e nada foi movido. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | O InstallerClean não conseguiu obter o bloqueio que o Windows Installer usa para impedir que dois programas alterem software instalado ao mesmo tempo, então não pôde descartar que um arquivo se tornasse necessário no meio do caminho, e nada foi excluído. Tente de novo, e reinicie o Windows se continuar acontecendo. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. Try again, and restart Windows if it keeps happening. | O InstallerClean não conseguiu obter o bloqueio que o Windows Installer usa para impedir que dois programas alterem software instalado ao mesmo tempo, então não pôde descartar que um arquivo se tornasse necessário no meio do caminho, e nada foi movido. Tente de novo, e reinicie o Windows se continuar acontecendo. |
| Invalid destination | Destino inválido |
| Move stopped | Movimentação interrompida |
| Couldn't use that backup folder | Não foi possível usar essa pasta de backup |
| Move failed | Falha na movimentação |
| Delete failed | Falha na exclusão |
| Setting not saved | Configuração não salva |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | Não foi possível salvar a alteração. Na próxima vez que for iniciado, o InstallerClean voltará à configuração anterior. |
| The destination cannot be inside the Windows Installer folder. | O destino não pode estar dentro da pasta do Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | O destino {0} é resolvido dentro de uma pasta de sistema do Windows. Escolha um caminho fora de %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% e %ProgramData%. |
| Not enough space | Espaço insuficiente |
| There isn't room at {0}<br><br>Required: {1}<br>Available: {2} | Não cabe em {0}<br><br>Necessário: {1}<br>Disponível: {2} |
| You don't have permission to write to {0}.<br>Try a folder in your user profile or on a drive you own. | Você não tem permissão para gravar em {0}.<br>Tente uma pasta no seu perfil de usuário ou em uma unidade sua. |
| The path {0} is too long for Windows. Pick a shorter path. | O caminho {0} é longo demais para o Windows. Escolha um caminho mais curto. |
| The folder {0} does not exist and could not be created. Check the drive letter or network path. | A pasta {0} não existe e não foi possível criá-la. Verifique a letra da unidade ou o caminho de rede. |
| Windows cannot write to {0}.<br>Details in {1}. | O Windows não consegue gravar em {0}.<br>Detalhes em {1}. |
| Windows cannot write to {0}. The crash log could not be written. | O Windows não consegue gravar em {0}. Não foi possível gravar o crash.log. |
| Cannot write to {0}.<br>Details in {1}. | Não é possível gravar em {0}.<br>Detalhes em {1}. |
| Cannot write to {0}. The crash log could not be written. | Não é possível gravar em {0}. Não foi possível gravar o crash.log. |
| File no longer exists. | O arquivo não existe mais. |
| Source file is a symlink or junction; refused for safety. | O arquivo de origem é um link simbólico ou junção; recusado por segurança. |
| This file is not directly inside the Windows Installer folder; refused for safety. | Este arquivo não está diretamente dentro da pasta do Windows Installer; recusado por segurança. |
| Windows refused access to this file; it was left in place. | O Windows negou o acesso a este arquivo; ele foi deixado onde estava. |
| Windows refused access to these files; they were left in place. | O Windows negou o acesso a estes arquivos; eles foram deixados onde estavam. |
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | Este arquivo está aberto ou bloqueado por outro programa, então nada consegue removê-lo agora. Ele foi deixado no lugar; tente mais tarde. |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | Estes arquivos estão abertos ou bloqueados por outro programa, então nada consegue removê-los agora. Eles foram deixados no lugar; tente mais tarde. |
| Windows reported a file error; the file was left in place. | O Windows relatou um erro de arquivo; o arquivo foi deixado onde estava. |
| Windows reported file errors; these files were left in place. | O Windows relatou erros de arquivo; estes arquivos foram deixados onde estavam. |
| Something went wrong with this file; it was left in place. | Algo deu errado com este arquivo; ele foi deixado onde estava. |
| Something went wrong with these files; they were left in place. | Algo deu errado com estes arquivos; eles foram deixados onde estavam. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Recusando mover arquivos para a pasta do Windows Installer (destino: {0}). |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | A pasta de backup precisa ser um caminho completo até uma pasta, começando por uma letra de unidade ou um compartilhamento de rede (por exemplo D:\Backup, ou \\servidor\backup). O InstallerClean não pode usar esta: {0} |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then Re-scan and try again. | O InstallerClean não pôde mais confirmar a pasta de backup, então parou. Verifique {0}, depois Reanalisar e tente de novo. |
| Cannot write to {0}. | Não é possível gravar em {0}. |
| A file called '{0}' is already in the backup folder. | Já existe um arquivo chamado '{0}' na pasta de backup. |

## Update check

| English | Português (Brasil) |
| --- | --- |
| Check for updates | Verificar atualizações |
| Checking... | Verificando... |
| Up to date. | Tudo atualizado. |
| Version {0} is available. | A versão {0} está disponível. |
| Update available | Atualização disponível |
| You're running version {0}.<br>Version {1} is available. | Você está usando a versão {0}.<br>A versão {1} está disponível. |
| Couldn't reach GitHub. Check your internet connection and try again. | Não foi possível acessar o GitHub. Verifique a sua conexão com a internet e tente de novo. |
| GitHub returned an error response. Try again in a few minutes. | O GitHub retornou uma resposta de erro. Tente de novo em alguns minutos. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | A resposta do GitHub não continha uma versão reconhecível. Tente de novo mais tarde, ou abra diretamente a página de versões. |
| The check timed out. Your connection to GitHub may be slow; try again. | A verificação expirou. A sua conexão com o GitHub pode estar lenta; tente de novo. |
| The check failed for an unknown reason. Details are in {0} if you need to report it. | A verificação falhou por um motivo desconhecido. Os detalhes estão em {0}, se você precisar relatar o problema. |
| The check failed for an unknown reason. The crash log could not be written. | A verificação falhou por um motivo desconhecido. Não foi possível gravar o crash.log. |

## Opening links in your browser

| English | Português (Brasil) |
| --- | --- |
| Couldn't open your browser | Não foi possível abrir o navegador |
| The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | O link está na área de transferência, então você pode colá-lo:<br><br>{0} |
| InstallerClean couldn't copy the link to your clipboard either, so here it is:<br><br>{0} | O InstallerClean também não conseguiu copiar o link para a área de transferência, então aqui está:<br><br>{0} |

## Sending the summary

| English | Português (Brasil) |
| --- | --- |
| Sending... | Enviando... |
| Thanks! Report sent. | Obrigado! Relatório enviado. |
| Sending failed. Try again later. | Falha no envio. Tente de novo mais tarde. |
| No report to send. | Nenhum relatório para enviar. |
| Send this? | Enviar isto? |
| It goes to nofaff.netlify.app/api/result-log. Nothing identifies you or your machine; it just lets me know InstallerClean's working and [how much space people are freeing]. | Vai para nofaff.netlify.app/api/result-log. Nada identifica você ou a sua máquina; só me diz que o InstallerClean está funcionando e [quanto espaço as pessoas estão liberando]. |

## Startup and crashes

| English | Português (Brasil) |
| --- | --- |
| InstallerClean | InstallerClean |
| It's already running. | Já está em execução. |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | Ocorreu um erro inesperado e o InstallerClean precisa fechar.<br><br>{0}<br><br>Detalhes gravados em:<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | Ocorreu um erro inesperado e o InstallerClean precisa fechar.<br><br>{0}<br><br>Não foi possível gravar o crash.log. |
| Startup error | Erro de inicialização |
| Failed to start ({0}). Details written to:<br>{1} | Falha ao iniciar ({0}). Detalhes gravados em:<br>{1} |
| Failed to start ({0}). The crash log could not be written. | Falha ao iniciar ({0}). Não foi possível gravar o crash.log. |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # O crash.log registra exceções não tratadas do InstallerClean.<br># Com privilégios elevados, as mensagens de exceção do framework podem<br># incluir caminhos de arquivo da sessão em execução (inclusive perfis<br># de outros usuários enumerados pelas consultas do Windows Installer).<br># Mensagens de falha de rede da verificação de atualizações ou do envio<br># do log de resultados podem incluir a URL de destino e o IP ou proxy<br># resolvido. Entradas sobre registros ilegíveis do Windows Installer<br># podem incluir um SID de conta do Windows (S-1-5-21-...) e os códigos<br># de produto do software instalado.<br># Remova os três tipos de dado antes de anexar este arquivo a um<br># relatório de erro público.<br> |

## Tooltips (hover text)

| English | Português (Brasil) |
| --- | --- |
| It's thirsty work! | É trabalho que dá sede! |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Cancelamento solicitado. O InstallerClean está esperando o passo atual chegar a um ponto em que possa parar. Isso pode levar alguns segundos durante operações intensas de E/S ou uma chamada ao banco de dados MSI. |
| Close | Fechar |
| A star helps other people find it. | Uma estrela ajuda outras pessoas a encontrar o InstallerClean. |
| Minimise | Minimizar |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | Você decide, mas eu agradeço. Envia um resumo anônimo que só me diz se está funcionando e quanto espaço as pessoas estão liberando. A próxima tela mostra o que será enviado antes de você confirmar. |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | Você decide, mas eu agradeço. Envia um resumo anônimo que só me diz se está funcionando. A próxima tela mostra o que será enviado antes de você confirmar. |
| Move the unneeded files to the backup folder. | Move os arquivos desnecessários para a pasta de backup. |
| Move the unneeded files to a backup folder. You'll choose it next. | Move os arquivos desnecessários para uma pasta de backup. Você a escolhe em seguida. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder. | Move os arquivos desnecessários para a pasta de backup. Ela está na mesma unidade, então você só recupera o espaço depois de excluir essa pasta. |
| Delete the unneeded files permanently. Use Move instead if you'd like a chance to satisfy yourself all is well. | Exclui permanentemente os arquivos desnecessários. Use Mover se quiser a chance de se convencer de que está tudo bem. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Nome do titular do certificado Authenticode incorporado. Cadeia não verificada. |
| Change language. The program will restart. | Alterar idioma. O programa será reiniciado. |

## Screen reader labels

| English | Português (Brasil) |
| --- | --- |
| Donate | Doar |
| Buy me a cuppa | Me paga um café |
| Cancel operation | Cancelar a operação |
| Cancel scan | Cancelar a análise |
| Cancel startup scan | Cancelar a análise inicial |
| Close | Fechar |
| Close window | Fechar a janela |
| Close result and return to main window | Fechar o resultado e voltar para a janela principal |
| Leave a star on github | Deixe uma estrela no github |
| Minimise | Minimizar |
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Excluir permanentemente remove os arquivos desnecessários. Cancelar fecha sem excluir nada. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Mover coloca os arquivos desnecessários na pasta de destino escolhida. Cancelar os deixa onde estão. |
| Say thanks | Agradeça |
| Send posts the report shown to No Faff. Cancel sends nothing. | Enviar transmite ao No Faff o relatório exibido. Cancelar não envia nada. |
| Check for updates | Verificar atualizações |
| Checks github's releases page for a newer version. | Consulta a página de versões do github em busca de uma versão mais recente. |
| Opens the readme on github in your browser. | Abre o readme no github no seu navegador. |
| Opens the issue tracker on github.com in your browser. | Abre o rastreador de problemas (Issues) em github.com no seu navegador. |
| If ticked, InstallerClean checks github for a newer version when you run it. | Se marcada, o InstallerClean consulta o github em busca de uma versão mais recente quando você o executa. |
| Open the release page to download the newer version, or cancel to keep the current version. | Abra a página da versão para baixar a versão mais recente, ou cancele para manter a versão atual. |
| Opens the licence file on github.com in your browser. | Abre o arquivo da licença em github.com no seu navegador. |
| Backup folder | Pasta de backup |
| Patches | Patches |
| Product details | Detalhes do produto |
| Backup folder | Pasta de backup |
| Operation progress | Progresso da operação |
| Scan {InstallerFolder} again | Analisar {InstallerFolder} novamente |
| Scanning progress | Progresso da análise |
| Startup scan progress | Progresso da análise inicial |
| Details, unneeded files | Detalhes, arquivos desnecessários |
| Available for cleanup. | Disponíveis para limpeza. |
| Details, files left alone | Detalhes, arquivos deixados de lado |
| Read-only inventory. | Inventário somente leitura. |
| Sorted by {0}, ascending | Classificado por {0}, crescente |
| Sorted by {0}, descending | Classificado por {0}, decrescente |
| Scan results | Resultados da análise |
| Result details | Detalhes do resultado |
| File details | Detalhes do arquivo |
| Product details | Detalhes do produto |
| Dialog text | Texto da caixa de diálogo |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | Arquivos que não puderam ser processados |
| Explains this folder, and how to recover a file, in the README | Explica esta pasta, e como recuperar um arquivo, no README |
| Report preview | Visualização do relatório |
| Change language | Alterar idioma |
| The program will restart. | O programa será reiniciado. |

## File picker

| English | Português (Brasil) |
| --- | --- |
| Choose destination folder for moved files | Escolha a pasta de destino para os arquivos movidos |

## Version

| English | Português (Brasil) |
| --- | --- |
| Version {0} | Versão {0} |

## Word forms (singular and plural)

| English | Português (Brasil) |
| --- | --- |
| file | arquivo |
| files | arquivos |
| error | erro |
| errors | erros |
| package | pacote |
| packages | pacotes |
| product | produto |
| products | produtos |
| patch | patch |
| patches | patches |

## Sizes and times

| English | Português (Brasil) |
| --- | --- |
| ,  | ,  |
| {0:F2} GB | {0:F2} GB |
| {0:F1} MB | {0:F1} MB |
| {0:F1} KB | {0:F1} KB |
| {0} B | {0} B |
| {0:F0}ms | {0:F0}ms |
| {0:F1}s | {0:F1}s |
| less than a second | menos de um segundo |
| {0:F1} seconds | {0:F1} segundos |

## Command-line tool (installerclean-cli)

| English | Português (Brasil) |
| --- | --- |
| Error: unknown argument '{0}' | Erro: argumento desconhecido '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Erro: argumento extra inesperado '{0}'. Se a sua pasta de destino tiver um espaço no nome, coloque aspas em todo o caminho: /m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | Erro: argumento extra inesperado '{0}'. /s e /d não aceitam mais argumentos, e só se pode usar um sinalizador por execução. |
| Cancelling... | Cancelando... |
| Cancelled. | Cancelado. |
| Error: unexpected failure ({0}). Details written to {1}. | Erro: falha inesperada ({0}). Detalhes gravados em {1}. |
| Error: unexpected failure ({0}). The crash log could not be written. | Erro: falha inesperada ({0}). Não foi possível gravar o log de falhas. |
| Scanning {InstallerFolder}... | Analisando {InstallerFolder}... |
| Found {0} unneeded {1} to clean up ({2}). | Foram encontrados {0} {1} desnecessários para limpar ({2}). |
| Found no unneeded files. | Nenhum arquivo desnecessário encontrado. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({2}) rather than offering it. | O InstallerClean não conseguiu ter certeza de quais arquivos em cache pertencem aos programas instalados aqui, então reteve o único arquivo ({2}) em vez de oferecê-lo. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back all {0} {1} ({2}) rather than offering them. | O InstallerClean não conseguiu ter certeza de quais arquivos em cache pertencem aos programas instalados aqui, então reteve todos os {0} {1} ({2}) em vez de oferecê-los. |
| InstallerClean couldn't establish that the cached file it found is unneeded, so it has held back the one file ({2}) rather than offering it. | O InstallerClean não conseguiu estabelecer que o arquivo em cache que encontrou não é necessário, então reteve esse único arquivo ({2}) em vez de oferecê-lo. |
| InstallerClean couldn't establish that any of the cached files it found are unneeded, so it has held back all {0} {1} ({2}) rather than offering them. | O InstallerClean não conseguiu estabelecer de nenhum dos arquivos em cache que encontrou que não seja necessário, então reteve todos os {0} {1} ({2}) em vez de oferecê-los. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({2}) rather than offering it. | O InstallerClean não conseguiu ter certeza de quais arquivos em cache pertencem aos programas instalados aqui, então reteve o único arquivo ({2}) em vez de oferecê-lo. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} ({2}) rather than offering them. | O InstallerClean não conseguiu ter certeza de quais arquivos em cache pertencem aos programas instalados aqui, então reteve {0} {1} ({2}) em vez de oferecê-los. |
| InstallerClean couldn't be certain about one of the cached files it found, so it has held that one back ({2}) rather than offering it. | O InstallerClean não teve certeza sobre um dos arquivos em cache que encontrou, então reteve esse ({2}) em vez de oferecê-lo. |
| InstallerClean couldn't be certain about some of the cached files it found, so it has held back {0} {1} ({2}) rather than offering them. | O InstallerClean não teve certeza sobre alguns dos arquivos em cache que encontrou, então reteve {0} {1} ({2}) em vez de oferecê-los. |
| Why it couldn't be certain: | Por que não foi possível ter certeza: |
|   A file path in Windows Installer's own records wouldn't resolve, so nothing could be matched to it. |   Um caminho de arquivo nos próprios registros do Windows Installer não pôde ser resolvido, então nada pôde ser associado a ele. |
|   A file Windows has a record of couldn't be identified, so it couldn't be matched to what's in the folder. |   Não foi possível identificar um arquivo do qual o Windows tem registro, então não foi possível compará-lo com o que há na pasta. |
|   A program may be installed more than once on this PC, and the records can't say which copy a file belongs to. |   Um programa pode estar instalado mais de uma vez neste PC, e os registros não conseguem dizer a qual cópia um arquivo pertence. |
|   A file in the folder couldn't be identified, so it couldn't be matched against the records. |   Não foi possível identificar um arquivo da pasta, então não foi possível compará-lo com os registros. |
|   A file says it belongs to a program that is still installed, so it may still be needed. |   Um arquivo diz pertencer a um programa que ainda está instalado, então ainda pode ser necessário. |
|   Either a file wouldn't say which program it belongs to, or Windows wouldn't answer about that program. |   Ou um arquivo não informou a qual programa pertence, ou o Windows não respondeu sobre esse programa. |
|   A check on which programs the files belong to gave answers that didn't line up with the files it was handed. |   Uma verificação de a quais programas os arquivos pertencem deu respostas que não correspondiam aos arquivos que lhe foram entregues. |
| Windows has a record for {0} file that is not in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. To put the file back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | O Windows tem um registro de {0} arquivo que não está em {InstallerFolder}: {1}. No dia a dia não causa problema, mas uma atualização ou desinstalação desse programa pode falhar. Para recolocar o arquivo, você precisa do instalador da versão que já tem. Consiga-o com o fabricante do programa e execute-o por cima da sua cópia atual. Uma versão mais nova não serve: ela teria que remover primeiro a que você tem, e é justamente essa etapa que precisa deste arquivo. Desinstalar primeiro também não funciona, pelo mesmo motivo. Isso deve restaurar o arquivo e deixar suas configurações intactas, mas a Microsoft não garante. |
| Windows has records for {0} files that are not in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. To put a file back, you need the installer for the version you already have of that program. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs the file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | O Windows tem registros de {0} arquivos que não estão em {InstallerFolder}: {1}. No dia a dia não causam problema, mas uma atualização ou desinstalação desses programas pode falhar. Para recolocar um arquivo, você precisa do instalador da versão desse programa que já tem. Consiga-o com o fabricante do programa e execute-o por cima da sua cópia atual. Uma versão mais nova não serve: ela teria que remover primeiro a que você tem, e é justamente essa etapa que precisa do arquivo. Desinstalar primeiro também não funciona, pelo mesmo motivo. Isso deve restaurar o arquivo e deixar suas configurações intactas, mas a Microsoft não garante. |
| InstallerClean couldn't be certain that the one superseded file is no longer needed, so it has held it back. | O InstallerClean não conseguiu ter certeza de que o único arquivo substituído não é mais necessário, então o reteve. |
| InstallerClean couldn't be certain that {0} superseded files are no longer needed, so it has held them back. | O InstallerClean não conseguiu ter certeza de que {0} arquivos substituídos não são mais necessários, então os reteve. |
| Deleting {0} unneeded {1}... | Excluindo {0} {1} desnecessários... |
| Permanently deleted {0} unneeded {1}. | Foram excluídos permanentemente {0} {1} desnecessários. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Erro: nenhum destino de movimentação especificado. Use /m CAMINHO. (Um padrão definido na GUI é por usuário e não se aplica a execuções agendadas ou em contas de serviço.) |
| Error: destination cannot be inside the Windows Installer folder. | Erro: o destino não pode estar dentro da pasta do Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Erro: o destino deve ser um caminho totalmente qualificado. Recebido: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Erro: o destino {0} é resolvido dentro de uma pasta de sistema do Windows. Escolha um caminho fora de %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% e %ProgramData%. |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | Erro: espaço insuficiente em {0}. Mover estes arquivos precisa de {1} e há {2} livres. Nada foi movido. |
| Error: something is using Windows Installer right now, such as a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | Erro: algo está usando o Windows Installer neste momento, como uma atualização do Windows ou um programa instalando em segundo plano. /m e /d ficam bloqueados enquanto isso acontece. Tente de novo quando terminar. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Erro: há uma transação anterior do Windows Installer suspensa nesta máquina. Retome ou reverta essa instalação (ou reinicie o Windows) antes de limpar {InstallerFolder}. |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | Erro: uma operação de arquivo na fila para depois da reinicialização atinge {InstallerFolder} ({0}). Reinicie o Windows para concluir essa operação antes de limpar. |
| Error: a file operation is queued for the next restart and InstallerClean can't tell which files it names, so it can't rule out {InstallerFolder}. Restart Windows before cleaning. | Erro: há uma operação de arquivos na fila para a próxima reinicialização e o InstallerClean não consegue saber quais arquivos ela nomeia, então não dá para descartar {InstallerFolder}. Reinicie o Windows antes de limpar. |
| Error: InstallerClean couldn't read one of the registry values it checks before touching {InstallerFolder}, so it can't rule out a Windows Installer operation in flight or queued for the next restart. /m and /d are blocked. Restart Windows and try again. If the read still fails, this isn't a machine InstallerClean can clean. | Erro: o InstallerClean não conseguiu ler um dos valores de registro que verifica antes de tocar em {InstallerFolder}, então não dá para descartar uma operação do Windows Installer em andamento ou na fila para a próxima reinicialização. /m e /d estão bloqueados. Reinicie o Windows e tente de novo. Se a leitura continuar falhando, este não é um computador que o InstallerClean possa limpar. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Erro: o Windows Installer tem algo em andamento, então /m e /d ficam bloqueados. O InstallerClean não vai tocar em {InstallerFolder} enquanto ela muda. Tente de novo quando terminar. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | Erro: o InstallerClean não conseguiu obter o bloqueio do Windows Installer que impede que dois programas alterem software instalado ao mesmo tempo, então não pôde descartar que um arquivo se tornasse necessário no meio do caminho. Nada foi excluído. Tente de novo, e reinicie o Windows se continuar acontecendo. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. Try again, and restart Windows if it keeps happening. | Erro: o InstallerClean não conseguiu obter o bloqueio do Windows Installer que impede que dois programas alterem software instalado ao mesmo tempo, então não pôde descartar que um arquivo se tornasse necessário no meio do caminho. Nada foi movido. Tente de novo, e reinicie o Windows se continuar acontecendo. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. | Erro: o Windows negou ao InstallerClean permissão para verificar se o Windows Installer estava ocupado, então não deu para descartar que um arquivo viesse a ser necessário no meio do caminho. Nada foi excluído. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. | Erro: o Windows negou ao InstallerClean permissão para verificar se o Windows Installer estava ocupado, então não deu para descartar que um arquivo viesse a ser necessário no meio do caminho. Nada foi movido. |
| Moving {0} unneeded {1} to {2}... | Movendo {0} {1} desnecessários para {2}... |
| Moved {0} unneeded {1}. | Foram movidos {0} {1} desnecessários. |
| Check that your programs still update and uninstall as normal, then delete {0}. | Confirme que seus programas ainda atualizam e desinstalam normalmente e depois exclua {0}. |
| It's simple to undo. Move them back from {0} into {InstallerFolder} and everything will be back to how it was. | É simples de desfazer. Mova-os de volta de {0} para {InstallerFolder} e tudo voltará a ser como era. |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then run the command again. | O InstallerClean não pôde mais confirmar a pasta de backup, então parou. Verifique {0} e execute o comando de novo. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Outro processo do InstallerClean mantém o bloqueio de instância única (a GUI ou outra execução da CLI). Código de saída 75 (transitório); seguro tentar novamente mais tarde. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Observação: falha ao gravar no Log de Eventos. Verifique as permissões do log de Aplicativo ou a Diretiva de Grupo. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - limpeza de {InstallerFolder} |
| Removes cached .msi and .msp files that no installed program still needs. | Remove arquivos .msi/.msp em cache que nenhum programa instalado precisa. |
| Needs an elevated (administrator) prompt; Windows will not start it. | Exige um prompt como administrador; o Windows não vai iniciá-lo. |
| Usage: | Uso: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help      Mostra esta ajuda (aceita também /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version   Mostra a versão (aceita também -v) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s          Somente analisar - lista os supérfluos |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d          Exclui permanentemente os supérfluos |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m          Move para a pasta de backup salva |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m CAMINHO  Move para o caminho especificado |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | O installerclean-cli bloqueia o prompt até terminar, para que um script<br>ou uma tarefa agendada possa esperar por ele. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | Pasta salva por usuário; execuções agendadas ou SYSTEM: /m CAMINHO. |
| Exit codes: | Códigos de saída: |
|   0   success: the run did what it was asked and nothing failed |   0   êxito: a execução fez o que foi pedido e nada falhou |
|   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |   1   falha: nada processado (argumentos ou destino inválidos, uma<br>       análise com falha ou todos os arquivos com falha) |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   parcial: alguns processados, outros não (falha ou Ctrl+C) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  transitório: algo temporário bloqueou a execução (veja a mensagem) |
|   130 cancelled (Ctrl+C) |   130 cancelado (Ctrl+C) |
