# InstallerClean in Português (Brasil) (Brazilian Portuguese)

The text of InstallerClean's interface and command-line tool in English on the left, with the Brazilian Portuguese translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Brazilian Portuguese can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. The translation file itself is [`Strings.pt-BR.resx`](../../src/InstallerClean.Core/Resources/Strings.pt-BR.resx). This page is generated from it by `scripts/gen-translation-table.mjs`, so do not edit it by hand.

## Window titles

| English | Português (Brasil) |
| --- | --- |
| InstallerClean | InstallerClean |
| About | Sobre |
| Registered files that should not be deleted | Arquivos registrados que não deveriam ser excluídos |
| Unneeded files that are safe to delete | Arquivos desnecessários que podem ser excluídos com segurança |
| Confirm move | Confirmar movimentação |
| Confirm delete | Confirmar exclusão |
| Recycle Bin unavailable | Lixeira indisponível |

## Section headings

| English | Português (Brasil) |
| --- | --- |
| PRODUCTS | PRODUTOS |
| PATCHES | PATCHES |
| PRODUCT DETAILS | DETALHES DO PRODUTO |
| MOVE LOCATION | LOCAL DE DESTINO |
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
| _Delete | _Excluir |
| _Delete permanently | _Excluir permanentemente |
| _Done | _Concluído |
| Details | Detalhes |
| _Buy me a cuppa | Me paga um _café |
| Leave a _star on GitHub | _Deixe uma estrela no GitHub |
| Apache 2.0 licence | Licença Apache 2.0 |
| _Move | _Mover |
| _Move instead | _Mover em vez disso |
| Path to folder if you Move instead of Delete | Caminho da pasta, se você Mover em vez de Excluir |
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
| Checking the Recycle Bin... | Verificando a Lixeira... |
| Moving {0} {1}... | Movendo {0} {1}... |
| Deleting {0} {1}... | Excluindo {0} {1}... |
| Move cancelled. {0} of {1} {2} processed. | Movimentação cancelada após processar {0} de {1} {2}. |
| Delete cancelled. {0} of {1} {2} processed. | Exclusão cancelada após processar {0} de {1} {2}. |
| Move failed ({0}). Details in {1}. | Falha na movimentação ({0}). Detalhes em {1}. |
| Move failed ({0}). The crash log could not be written. | Falha na movimentação ({0}). Não foi possível gravar o crash.log. |
| Delete failed ({0}). Details in {1}. | Falha na exclusão ({0}). Detalhes em {1}. |
| Delete failed ({0}). The crash log could not be written. | Falha na exclusão ({0}). Não foi possível gravar o crash.log. |
| Access denied. Windows refused the scan. | Acesso negado. O Windows recusou a análise. |
| Scan failed: couldn't read the Windows Installer records. | Falha na análise: não foi possível ler os registros do Windows Installer. |
| Scan cancelled. | Análise cancelada. |
| Ready | Pronto |
| Scan failed ({0}). Details in {1}. | Falha na análise ({0}). Detalhes em {1}. |
| Scan failed ({0}). The crash log could not be written. | Falha na análise ({0}). Não foi possível gravar o crash.log. |

## Main screen text

| English | Português (Brasil) |
| --- | --- |
| Any unneeded files below are safe to delete. | Quaisquer arquivos desnecessários abaixo podem ser excluídos com segurança. |
| They sit in C:\Windows\Installer, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | Eles ficam em C:\Windows\Installer, deixados para trás quando um programa foi desinstalado ({0}), um patch mais recente substituiu outro ({1}) ou o fabricante o retirou ({2}). O InstallerClean só lista arquivos que o próprio Windows informa ter terminado de usar. |
| Delete them to the Recycle Bin, or use Move instead to keep a backup. Putting the files back in C:\Windows\Installer returns you to exactly where you started. | Exclua-os para a Lixeira, ou use Mover em vez disso para manter uma cópia de backup. Colocar os arquivos de volta em C:\Windows\Installer deixa tudo exatamente como estava. |
| Nothing scanned yet. | Nada foi analisado ainda. |
| Press Re-scan to look through C:\Windows\Installer for installer files that no program still needs. | Clique em Reanalisar para procurar em C:\Windows\Installer arquivos de instalação que nenhum programa ainda precisa. |
| These files can't be cleaned up right now. | Estes arquivos não podem ser limpos agora. |
| Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch the installer cache while it's changing. Once it's done, Re-scan and they come back. | Algo está usando o Windows Installer agora, normalmente uma atualização do Windows ou um programa se instalando em segundo plano. Mover e Excluir ficam pausados enquanto isso acontece, então o InstallerClean não mexe no cache de instalação enquanto ele está mudando. Quando terminar, analise de novo e eles voltam. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | Uma transação anterior do Windows Installer está suspensa nesta máquina. Retome ou reverta essa instalação (ou reinicie o Windows) antes de limpar o cache. |
| Windows has a file rename queued for the next restart that affects the Installer cache. Restart Windows before cleaning. | O Windows tem uma renomeação de arquivo na fila para a próxima reinicialização que afeta o cache do Installer. Reinicie o Windows antes de limpar. |
| Select a file to view details. | Selecione um arquivo para ver os detalhes. |
| Select a product to view details. | Selecione um produto para ver os detalhes. |
| No metadata available. | Nenhum metadado disponível. |
| This installer file has been deleted. InstallerClean didn't do it, it never removes a file a program still needs; something else deleted this one before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. | Este arquivo de instalação foi excluído. Não foi o InstallerClean: ele nunca remove um arquivo que um programa ainda precisa; outra coisa excluiu este antes de você executar o InstallerClean.<br><br>Por enquanto não causa nenhum problema, e não vai causar até o dia em que você tentar reparar, atualizar ou desinstalar o programa ao qual ele pertence. Esse passo pode falhar então, porque o Windows procura este arquivo e ele não está lá.<br><br>Para tentar resolver, baixe o instalador desse programa no site do fabricante e execute-o por cima da sua cópia atual (não desinstale antes: desinstalar é, em si, um passo que precisa deste arquivo). Use a versão que você tem instalada, se conseguir, porque o Windows pode recusar uma diferente. Isso normalmente recoloca o arquivo, e as suas configurações em geral ficam intactas, mas a Microsoft não garante: o último recurso dela é reinstalar o programa, ou o próprio Windows. |
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
| Nothing to clean up in C:\Windows\Installer | Nada para limpar em C:\Windows\Installer |
| Scanned {0} {1} in {2} | Análise de {0} {1} em {2} |
| Copy them back to C:\Windows\Installer if anything ever breaks ([extremely unlikely]). | Copie-os de volta para C:\Windows\Installer se algum dia algo quebrar ([extremamente improvável]). |
| Until then, you can restore them if anything ever breaks ([extremely unlikely]). | Até lá, você pode restaurá-los se algum dia algo quebrar ([extremamente improvável]). |
| Empty it to actually reclaim the space. | Esvazie a Lixeira para realmente recuperar o espaço. |
| {0} freed | {0} liberados |
| {0} cleaned up | {0} limpos |
| {0} moved | {0} movidos |
| Nothing was moved | Nada foi movido |
| Nothing was deleted | Nada foi excluído |
| {0} of {1} could not be moved. | {0} arquivo de {1} não pôde ser movido. |
| {0} of {1} could not be moved. | {0} arquivos de {1} não puderam ser movidos. |
| {0} of {1} could not be deleted. | {0} arquivo de {1} não pôde ser excluído. |
| {0} of {1} could not be deleted. | {0} arquivos de {1} não puderam ser excluídos. |
| {0} {1} moved to: {2} | {0} {1} movido para: {2} |
| {0} {1} moved to: {2} | {0} {1} movidos para: {2} |
| {0} {1} moved to the Recycle Bin | {0} {1} movido para a Lixeira |
| {0} {1} moved to the Recycle Bin | {0} {1} movidos para a Lixeira |
| {0} {1} kept in place, because a program started needing them again after the scan. | {0} {1} mantidos no lugar: um programa voltou a precisar deles depois da análise. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read when the check was repeated. | {0} {1} mantidos no lugar: não foi possível ler por completo os registros do Windows Installer quando a verificação foi repetida. |
| Moved {0} of {1} {2} before you cancelled. | Movimentação cancelada após mover {0} de {1} {2}. |
| Moved {0} of {1} {2} to the Recycle Bin before you cancelled. | Exclusão cancelada após mover {0} de {1} {2} para a Lixeira. |
| Permanently deleted {0} of {1} {2} before you cancelled. | Exclusão permanente cancelada após remover {0} de {1} {2}. |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. | {0} {1} excluído permanentemente. Não foi para a Lixeira. |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. | {0} {1} excluídos permanentemente. Não foram para a Lixeira. |
| That's fine, it was safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Tudo bem, era seguro remover. O InstallerClean só remove os arquivos que o Windows informa ter terminado de usar, nunca um que um programa ainda precisa. No caso improvável de uma exclusão deixar um programa sem conseguir reparar, atualizar ou desinstalar, reinstalá-lo pelo site do fabricante normalmente recoloca o arquivo, embora a Microsoft não garanta. |
| That's fine, they were safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Tudo bem, eram seguros para remover. O InstallerClean só remove os arquivos que o Windows informa ter terminado de usar, nunca um que um programa ainda precisa. No caso improvável de uma exclusão deixar um programa sem conseguir reparar, atualizar ou desinstalar, reinstalá-lo pelo site do fabricante normalmente recoloca o arquivo, embora a Microsoft não garanta. |
| Glad to help. There's a tip jar if you're feeling kind. | Que bom que ajudou. A caixinha está aqui, se vier do coração. |

## Recycle Bin unavailable

| English | Português (Brasil) |
| --- | --- |
| The Recycle Bin isn't available for this drive | A Lixeira não está disponível para esta unidade |
| So this {1} ({2}) hasn't been deleted. You can move it somewhere safe, or delete it permanently. | Então este {1} ({2}) não foi excluído. Você pode movê-lo para um lugar seguro, ou excluí-lo permanentemente. |
| So these {0} {1} ({2}) haven't been deleted. You can move them somewhere safe, or delete them permanently. | Então estes {0} {1} ({2}) não foram excluídos. Você pode movê-los para um lugar seguro, ou excluí-los permanentemente. |
| Deleting it is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Excluí-lo é seguro. O InstallerClean só remove os arquivos que o Windows informa ter terminado de usar, nunca um que um programa ainda precisa, e a Lixeira é apenas uma proteção extra. No caso improvável de uma exclusão deixar um programa sem conseguir reparar, atualizar ou desinstalar, reinstalá-lo pelo site do fabricante normalmente recoloca o arquivo, embora a Microsoft não garanta. |
| Deleting them is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Excluí-los é seguro. O InstallerClean só remove os arquivos que o Windows informa ter terminado de usar, nunca um que um programa ainda precisa, e a Lixeira é apenas uma proteção extra. No caso improvável de uma exclusão deixar um programa sem conseguir reparar, atualizar ou desinstalar, reinstalá-lo pelo site do fabricante normalmente recoloca o arquivo, embora a Microsoft não garanta. |

## Summaries and counts

| English | Português (Brasil) |
| --- | --- |
| {0} file still needed | {0} arquivo ainda necessário |
| {0} files still needed | {0} arquivos ainda necessários |
| {0} unneeded file to clean up | {0} arquivo desnecessário para limpar |
| {0} unneeded files to clean up | {0} arquivos desnecessários para limpar |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | {0} arquivo registrado está ausente (não foi excluído pelo InstallerClean). Sem problema agora, mas uma futura reparação, atualização ou desinstalação desse programa pode falhar. Abra Detalhes para saber o que fazer. |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | {0} arquivos registrados estão ausentes (não foram excluídos pelo InstallerClean). Sem problema agora, mas uma futura reparação, atualização ou desinstalação desses programas pode falhar. Abra Detalhes para saber o que fazer. |
| {0} installed program could not be read during this scan, so superseded patches have been kept. Orphaned files are not affected. | {0} programa instalado não pôde ser lido nesta análise, então os patches substituídos foram mantidos. Os arquivos órfãos não são afetados. |
| {0} installed programs could not be read during this scan, so superseded patches have been kept. Orphaned files are not affected. | {0} programas instalados não puderam ser lidos nesta análise, então os patches substituídos foram mantidos. Os arquivos órfãos não são afetados. |
| {0} of {1} {2} | {0} de {1} {2} |
| {0} orphaned, {1} superseded, {2} obsoleted ({3}) | {0} órfãos, {1} substituídos, {2} obsoletos ({3}) |
| {0} registered file that is still needed ({1}) | {0} arquivo registrado que ainda é necessário ({1}) |
| {0} registered files that are still needed ({1}) | {0} arquivos registrados que ainda são necessários ({1}) |

## Confirmation dialogs

| English | Português (Brasil) |
| --- | --- |
| Move {0} {1} ({2})? | Mover {0} {1} ({2})? |
| Files will be moved to: | Os arquivos serão movidos para: |
| Delete {0} {1} ({2})? | Excluir {0} {1} ({2})? |
| Files will be moved to the Recycle Bin. If you'd like backup copies, use the Move button instead. | Os arquivos serão movidos para a Lixeira. Se quiser cópias de backup, use o botão Mover em vez disso. |
| This folder is on the same drive, so the move won't free any space by itself. You'll get the space back when you delete the files from it, or you can pick a folder on another drive instead. | Esta pasta está na mesma unidade, então mover não vai liberar espaço por si só. Você recupera o espaço quando excluir os arquivos de lá, ou pode escolher uma pasta em outra unidade. |

## Error messages

| English | Português (Brasil) |
| --- | --- |
| Access denied | Acesso negado |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | O Windows negou acesso ao InstallerClean, que por isso parou. Nada foi removido.<br><br>O InstallerClean já estava em execução como administrador, então iniciá-lo de novo dessa forma não vai ajudar. O Windows não diz mais nada sobre o que negou o acesso, então não há nada específico para tentar. |
| Couldn't read the Windows Installer records | Não foi possível ler os registros do Windows Installer |
| Scan failed | Falha na análise |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in C:\Windows\Installer orphaned. InstallerClean stopped instead. Nothing has been removed. | Os registros do Windows Installer voltaram completamente vazios: nenhum programa instalado e nenhuma atualização reivindica um arquivo de instalação em cache. Isso não acontece em uma máquina que funciona (até uma instalação nova do Windows tem alguns), então ou os registros estão danificados ou não puderam ser lidos, e uma análise que acreditasse nessa resposta chamaria erroneamente de órfão cada arquivo em C:\Windows\Installer. Em vez disso, o InstallerClean parou. Nada foi removido. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | O Windows Installer não deixou o InstallerClean listar o que está instalado. O InstallerClean já estava em execução como administrador, então executá-lo de novo como administrador não muda nada. Sem essa lista não há como saber com segurança quais arquivos em cache ainda são necessários, então o InstallerClean parou. Nada foi removido. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | O Windows Installer não conseguiu dar ao InstallerClean uma lista legível dos programas instalados: {0} entradas seguidas voltaram ilegíveis (último código de erro {1}). Em vez de trabalhar com uma lista lida pela metade, o InstallerClean parou. Nada foi removido. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | O Windows Installer nunca sinalizou o fim da lista de programas instalados: o InstallerClean desistiu depois de {0} entradas (último código de erro {1}). Não dá para confiar em uma lista sem fim, então o InstallerClean parou. Nada foi removido. |
| Windows Installer couldn't give InstallerClean a readable list of one program's patches: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | O Windows Installer não conseguiu dar ao InstallerClean uma lista legível dos patches de um programa: {0} entradas seguidas voltaram ilegíveis (último código de erro {1}). Em vez de trabalhar com uma lista lida pela metade, o InstallerClean parou. Nada foi removido. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | O Windows Installer nunca sinalizou o fim da lista de patches de um programa: o InstallerClean desistiu depois de {0} entradas (último código de erro {1}). Não dá para confiar em uma lista sem fim, então o InstallerClean parou. Nada foi removido. |
| InstallerClean couldn't square this scan with the Windows Installer records: every file Windows still lists as needed is missing from C:\Windows\Installer, while the files actually in the folder match nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. | O InstallerClean não conseguiu conciliar esta análise com os registros do Windows Installer: todo arquivo que o Windows ainda lista como necessário está ausente de C:\Windows\Installer, enquanto os arquivos que de fato estão na pasta não correspondem a nenhum registro. Nenhuma máquina real se parece com isso, então isso aponta para um problema na leitura dos registros, e não para arquivos que você possa remover com segurança. Nada foi oferecido para limpeza e nada foi removido. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | O InstallerClean não conseguiu ler o suficiente dos registros do Windows Installer para ter certeza do que ainda é necessário: a lista de programas instalados voltou incompleta, e ler esses mesmos registros direto do registro do Windows também deu erros. Um arquivo poderia parecer órfão só porque o registro que o nomeia era um dos ilegíveis, então o InstallerClean parou. Nada foi removido. |
| Invalid destination | Destino inválido |
| Could not write to destination | Não foi possível gravar no destino |
| Move failed | Falha na movimentação |
| Delete failed | Falha na exclusão |
| Setting not saved | Configuração não salva |
| The change could not be saved. The app will go back to the previous setting next time it starts. | Não foi possível salvar a alteração. Na próxima vez que for iniciado, o InstallerClean voltará à configuração anterior. |
| The destination cannot be inside the Windows Installer folder. | O destino não pode estar dentro da pasta do Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | O destino {0} fica dentro de uma pasta de sistema do Windows. Escolha um caminho fora de %SystemRoot%, %ProgramFiles% e %ProgramData%. |
| Not enough space | Espaço insuficiente |
| Not enough space at {0}<br><br>Required: {1}<br>Available: {2} | Espaço insuficiente em {0}<br><br>Necessário: {1}<br>Disponível: {2} |
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
| This file is open or locked by another program, so nothing can move it just now. It was left in place; try again later. | Este arquivo está aberto ou bloqueado por outro programa, então nada consegue movê-lo agora. Ele foi deixado onde estava; tente de novo mais tarde. |
| These files are open or locked by another program, so nothing can move them just now. They were left in place; try again later. | Estes arquivos estão abertos ou bloqueados por outro programa, então nada consegue movê-los agora. Eles foram deixados onde estavam; tente de novo mais tarde. |
| Windows reported a file error; the file was left in place. | O Windows relatou um erro de arquivo; o arquivo foi deixado onde estava. |
| Windows reported file errors; these files were left in place. | O Windows relatou erros de arquivo; estes arquivos foram deixados onde estavam. |
| Something went wrong with this file; it was left in place. | Algo deu errado com este arquivo; ele foi deixado onde estava. |
| Something went wrong with these files; they were left in place. | Algo deu errado com estes arquivos; eles foram deixados onde estavam. |
| Couldn't move this file to the Recycle Bin (error {0}), and InstallerClean can't tell you why from that code. The file was left in place. Try the Move button instead, since it doesn't use the Recycle Bin. | Não foi possível mover este arquivo para a Lixeira (erro {0}), e por esse código o InstallerClean não tem como dizer o motivo. O arquivo foi deixado onde estava. Tente o botão Mover, que não usa a Lixeira. |
| Windows refused access even with administrator rights (error {0}), and InstallerClean can't tell whether the problem is the file or the Recycle Bin. The file was left in place. The Move button will work if it's the Recycle Bin, but not if it's the file. | O Windows negou o acesso mesmo com direitos de administrador (erro {0}), e o InstallerClean não tem como saber se o problema é o arquivo ou a Lixeira. O arquivo foi deixado onde estava. O botão Mover vai funcionar se o problema for a Lixeira, mas não se for o arquivo. |
| This file is open or locked by another program (error {0}), so nothing can remove it just now. It was left in place; try again later. | Este arquivo está aberto ou bloqueado por outro programa (erro {0}), então nada consegue removê-lo agora. Ele foi deixado onde estava; tente de novo mais tarde. |
| Windows deleted this file outright rather than moving it to the Recycle Bin. InstallerClean asked for the Recycle Bin, and Windows did this instead. The file is gone. | O Windows excluiu este arquivo de vez, em vez de movê-lo para a Lixeira. O InstallerClean pediu a Lixeira, e o Windows fez diferente. O arquivo se foi. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Recusando mover arquivos para a pasta do Windows Installer (destino: {0}). |
| The Move location needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | O local de destino precisa ser um caminho completo até uma pasta, começando com uma letra de unidade ou um compartilhamento de rede (por exemplo D:\Backup, ou \\server\backup). O InstallerClean não pode usar este: {0} |
| The Move location changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | O local de destino mudou enquanto os arquivos estavam sendo movidos (algo substituiu ou redirecionou a pasta), então o InstallerClean parou em vez de gravar no lugar errado. Verifique {0}, depois clique em Reanalisar e tente de novo. |
| Cannot write to {0}. | Não é possível gravar em {0}. |
| Could not find a unique filename for '{0}' after 10,000 attempts. | Não foi possível encontrar um nome de arquivo único para '{0}' após 10.000 tentativas. |

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
| GitHub returned an error response. The releases API may be rate-limited; try again in a few minutes. | O GitHub retornou uma resposta de erro. A API de versões pode estar com limite de requisições; tente de novo em alguns minutos. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | A resposta do GitHub não continha uma versão reconhecível. Tente de novo mais tarde, ou abra diretamente a página de versões. |
| The check timed out. Your connection to GitHub may be slow; try again. | A verificação expirou. A sua conexão com o GitHub pode estar lenta; tente de novo. |
| The check failed for an unknown reason. Details are in crash.log if you need to report it. | A verificação falhou por um motivo desconhecido. Os detalhes estão no crash.log, se você precisar relatar o problema. |

## Opening links in your browser

| English | Português (Brasil) |
| --- | --- |
| Couldn't open your browser | Não foi possível abrir o navegador |
| InstallerClean couldn't open your browser. The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | O InstallerClean não conseguiu abrir o seu navegador. O link está na área de transferência, então você mesmo pode colá-lo:<br><br>{0} |
| InstallerClean couldn't open your browser, and couldn't copy the link to your clipboard either. The link is:<br><br>{0} | O InstallerClean não conseguiu abrir o seu navegador, e também não conseguiu copiar o link para a área de transferência. O link é:<br><br>{0} |

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
| InstallerClean is already running. | O InstallerClean já está em execução. |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | Ocorreu um erro inesperado e o InstallerClean precisa fechar.<br><br>{0}<br><br>Detalhes gravados em:<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | Ocorreu um erro inesperado e o InstallerClean precisa fechar.<br><br>{0}<br><br>Não foi possível gravar o crash.log. |
| Startup error | Erro de inicialização |
| Failed to start ({0}). Details written to:<br>{1} | Falha ao iniciar ({0}). Detalhes gravados em:<br>{1} |
| Failed to start ({0}). The crash log could not be written. | Falha ao iniciar ({0}). Não foi possível gravar o crash.log. |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Redact both classes of detail before attaching this file to a<br># public bug report.<br> | # O crash.log captura exceções não tratadas do InstallerClean.<br># Sob elevação, as mensagens de exceção do framework podem incluir<br># caminhos de arquivo da sessão em execução (incluindo perfis de<br># outros usuários enumerados pelas consultas do Windows Installer).<br># As mensagens de falha de rede da verificação de atualizações ou do<br># envio do relatório de resultados podem incluir a URL de destino e o<br># endereço IP / proxy resolvido. Remova os dois tipos de detalhe antes<br># de anexar este arquivo a um relatório de bug público.<br> |

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
| Move the unneeded files to the Move location. | Mover os arquivos desnecessários para o local de destino. |
| Move the unneeded files somewhere safe. You'll choose the folder next. | Mover os arquivos desnecessários para um lugar seguro. Você escolherá a pasta em seguida. |
| Move the unneeded files to the Recycle Bin. | Mover os arquivos desnecessários para a Lixeira. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Nome do titular do certificado Authenticode incorporado. Cadeia não verificada. |
| Change language. The program will restart. | Alterar idioma. O programa será reiniciado. |

## Screen reader labels

| English | Português (Brasil) |
| --- | --- |
| Donate | Doar |
| Buy me a cuppa (About window) | Me paga um café (janela Sobre) |
| Cancel operation | Cancelar a operação |
| Cancel scan | Cancelar a análise |
| Cancel startup scan | Cancelar a análise inicial |
| Close | Fechar |
| Close window | Fechar a janela |
| Close result and return to main window | Fechar o resultado e voltar para a janela principal |
| Leave a star on GitHub (About window) | Deixe uma estrela no GitHub (janela Sobre) |
| Minimise | Minimizar |
| Move all unneeded installer files to the Move location | Mover todos os arquivos de instalação desnecessários para o local de destino |
| Move all unneeded installer files to the Recycle Bin | Mover todos os arquivos de instalação desnecessários para a Lixeira |
| Delete moves the unneeded files to the Recycle Bin. Cancel closes without deleting. | Excluir move os arquivos desnecessários para a Lixeira. Cancelar fecha sem excluir. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Mover coloca os arquivos desnecessários na pasta de destino escolhida. Cancelar os deixa onde estão. |
| Choose how to handle the unneeded files: move them somewhere safe, delete them permanently or cancel. | Escolha o que fazer com os arquivos desnecessários: movê-los para um lugar seguro, excluí-los permanentemente ou cancelar. |
| Move the unneeded files to a folder you choose | Mover os arquivos desnecessários para uma pasta que você escolher |
| Delete the unneeded files permanently because the Recycle Bin is unavailable for this drive | Excluir permanentemente os arquivos desnecessários porque a Lixeira está indisponível para esta unidade |
| Posts to nofaff.netlify.app. Counts and labels only. You will see the exact payload before sending. | Envia para nofaff.netlify.app. Apenas contagens e rótulos. Você verá exatamente o que será enviado antes do envio. |
| Say thanks | Agradecer |
| Send posts the report shown to No Faff. Cancel sends nothing. | Enviar transmite ao No Faff o relatório exibido. Cancelar não envia nada. |
| Check for updates | Verificar atualizações |
| Checks the GitHub releases API over HTTPS for a newer version. | Consulta a API de versões do GitHub via HTTPS em busca de uma versão mais recente. |
| Opens the guide (README) on github.com in your browser. | Abre o guia (README) em github.com no seu navegador. |
| Opens the issue tracker on github.com in your browser. | Abre o rastreador de problemas (Issues) em github.com no seu navegador. |
| When ticked, InstallerClean checks GitHub for a newer version when you run it. | Se marcada, o InstallerClean consulta o GitHub em busca de uma versão mais recente sempre que você o executa. |
| Open the release page to download the newer version, or cancel to keep the current version. | Abra a página da versão para baixar a versão mais recente, ou cancele para manter a versão atual. |
| Apache 2.0 licence | Licença Apache 2.0 |
| Opens the licence file on github.com in your browser. | Abre o arquivo da licença em github.com no seu navegador. |
| Move location | Local de destino |
| Products | Produtos |
| Patches | Patches |
| Product details | Detalhes do produto |
| Move location | Local de destino |
| Operation progress | Progresso da operação |
| Scan C:\Windows\Installer again | Analisar C:\Windows\Installer novamente |
| Scanning progress | Progresso da análise |
| Startup scan progress | Progresso da análise inicial |
| Details, unneeded files | Detalhes, arquivos desnecessários |
| Available for cleanup. | Disponíveis para limpeza. |
| Details, registered files | Detalhes, arquivos registrados |
| Read-only inventory. | Inventário somente leitura. |
| Sorted by {0}, ascending | Ordenado por {0}, crescente |
| Sorted by {0}, descending | Ordenado por {0}, decrescente |
| Scan results | Resultados da análise |
| Result details | Detalhes do resultado |
| File details | Detalhes do arquivo |
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
| Unknown argument: '{0}' | Argumento desconhecido: '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Erro: argumento extra inesperado '{0}'. Se a sua pasta de destino tiver um espaço no nome, coloque aspas em todo o caminho: /m "D:\My Backup" |
| Cancelling... | Cancelando... |
| Cancelled. | Cancelado. |
| Error: {0}. Details written to {1}. | Erro: {0}. Detalhes gravados em {1}. |
| Error: {0}. The crash log could not be written. | Erro: {0}. Não foi possível gravar o crash.log. |
| Scanning C:\Windows\Installer... | Analisando C:\Windows\Installer... |
| Found {0} {1} to clean up ({2}). | Encontrados {0} {1} para limpar ({2}). |
| Nothing to do. | Nada a fazer. |
| Deleting {0} {1}... | Excluindo {0} {1}... |
| Deleted {0} {1}. | Excluídos {0} {1}. |
| Error: the Recycle Bin is unavailable for this volume, so nothing was deleted. Use /m to move the files instead, or re-enable the Recycle Bin and run again. | Erro: a Lixeira está indisponível para este volume, então nada foi excluído. Use /m para mover os arquivos, ou reative a Lixeira e execute novamente. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Erro: nenhum destino de movimentação especificado. Use /m CAMINHO. (Um padrão definido na GUI é por usuário e não se aplica a execuções agendadas ou em contas de serviço.) |
| Error: destination cannot be inside the Windows Installer folder. | Erro: o destino não pode estar dentro da pasta do Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Erro: o destino deve ser um caminho totalmente qualificado. Recebido: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | Erro: o destino {0} fica dentro de uma pasta de sistema do Windows. Escolha um caminho fora de %SystemRoot%, %ProgramFiles% e %ProgramData%. |
| Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are blocked while that runs. Try again once it finishes. | Erro: algo está usando o Windows Installer agora, normalmente uma atualização do Windows ou um programa se instalando em segundo plano. Mover e Excluir ficam bloqueados enquanto isso acontece. Tente novamente quando terminar. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | Erro: uma transação anterior do Windows Installer está suspensa nesta máquina. Retome ou reverta essa instalação (ou reinicie o Windows) antes de limpar o cache. |
| Error: a queued post-reboot file operation targets the Installer cache ({0}). Restart Windows to complete that operation before cleaning. | Erro: uma operação de arquivo em fila para a próxima reinicialização afeta o cache do Installer ({0}). Reinicie o Windows para concluir essa operação antes de limpar. |
| Moving {0} {1} to {2}... | Movendo {0} {1} para {2}... |
| Moved {0} {1}. | Movidos {0} {1}. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Outro processo do InstallerClean mantém o bloqueio de instância única (a GUI ou outra execução da CLI). Código de saída 75 (transitório); seguro tentar novamente mais tarde. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Observação: falha ao gravar no Log de Eventos. Verifique as permissões do log de Aplicativo ou a Diretiva de Grupo. |
| InstallerClean - clean up C:\Windows\Installer | InstallerClean - limpeza de C:\Windows\Installer |
| Usage: | Uso: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help      Mostra esta ajuda (aceita também /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version   Mostra a versão (aceita também -v) |
|   installerclean-cli /s         Scan only - list removable files |   installerclean-cli /s          Apenas análise - lista os arquivos desnecessários |
|   installerclean-cli /d         Delete removable files (Recycle Bin) |   installerclean-cli /d          Exclui os arquivos desnecessários (Lixeira) |
|   installerclean-cli /m         Move to saved default location |   installerclean-cli /m          Move para o local padrão salvo |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m CAMINHO  Move para o caminho especificado |
| installerclean-cli is a real console process and blocks the prompt | installerclean-cli é um processo de console real e bloqueia o prompt |
| until it finishes; redirect or pipe its output as you would any | até terminar; redirecione ou encaminhe a saída por pipe como faria |
| other console exe. The GUI lives in InstallerClean.exe alongside it. | com qualquer outro executável de console. A GUI fica no InstallerClean.exe. |
| The saved default is per-user; scheduled or SYSTEM runs need /m PATH. | O padrão salvo é por usuário; execuções agendadas ou como SYSTEM exigem /m CAMINHO. |
| Exit codes: | Códigos de saída: |
|   0   success: every flagged file was processed |   0   sucesso: todos os arquivos sinalizados foram processados |
|   1   failure: nothing processed (bad args, scan failed, all files failed) |   1   falha: nada processado (argumentos inválidos, falha na análise, todos os arquivos falharam) |
|   2   partial: some files processed, some failed |   2   parcial: alguns arquivos processados, outros falharam |
|   75  transient: a temporary condition blocked the run (see the message) |   75  transitório: uma condição temporária bloqueou a execução (veja a mensagem) |
|   130 cancelled (Ctrl+C) |   130 cancelado (Ctrl+C) |
