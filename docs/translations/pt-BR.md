# InstallerClean in Português (Brasil) (Brazilian Portuguese)

The text of InstallerClean's interface and command-line tool in English on the left, with the Brazilian Portuguese translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Brazilian Portuguese can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.pt-BR.resx`](../../src/InstallerClean.Core/Resources/Strings.pt-BR.resx), so do not edit it by hand. The Brazilian Portuguese translation itself lives in [`gen-strings-pt-BR.mjs`](../../scripts/translations/gen-strings-pt-BR.mjs).

Where Brazilian Portuguese needs a different form of a line for some counts, the form is in a row of its own directly under the line, marked _(one form)_, _(few form)_ or _(many form)_. The app shows it in place of that line at the counts it is written for.

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | Português (Brasil) |
| --- | --- |
| InstallerClean | InstallerClean |
| About | Sobre |
| Files left alone | Arquivos deixados de lado |
| Unneeded files that are safe to delete | Arquivos desnecessários que podem ser excluídos com segurança |
| The anonymous report | O relatório anônimo |
| InstallerClean couldn't read the report. | O InstallerClean não conseguiu ler o relatório. |
| Third-party notices | Avisos de terceiros |

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
| Carry on _without it | Continuar _sem esperar |
| Check for _updates | Verificar _atualizações |
| _Close | _Fechar |
| _Close without tipping | _Fechar sem dar gorjeta |
| _Delete permanently | _Excluir permanentemente |
| _Done | _Concluído |
| Details | Detalhes |
| _Leave a tip | Deixar uma _gorjeta |
| Leave a tip | Deixar uma gorjeta |
| _Leave a tip | Deixar uma _gorjeta |
| Leave a _star on GitHub | _Deixe uma estrela no GitHub |
| Apache 2.0 licence | Licença Apache 2.0 |
| _Move | _Mover |
| Path to folder if you move rather than delete. | Caminho da pasta se você mover em vez de excluir. |
| _Re-scan | _Reanalisar |

## About window

| English | Português (Brasil) |
| --- | --- |
| Guide and FAQ | Guia e perguntas frequentes |
| What's new in this version | Novidades desta versão |
| Report a problem | Relatar um problema |
| Third-party notices | Avisos de terceiros |
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
| Checking registry for additional packages... | Verificando o registro em busca de pacotes adicionais... |
| Matching files against installed programs... | Comparando os arquivos com os programas instalados... |
| {0} of {1} | {0} de {1} |
| Checking remaining files... | Verificando os arquivos restantes... |
| Waiting for drive {0} to respond... | Aguardando a unidade {0} responder... |
| Waiting for {0} to respond... | Aguardando {0} responder... |
| Any file InstallerClean can't check without it will be left alone. | Qualquer arquivo cuja verificação dependa disso será deixado de lado. |
| Scan complete ({0}) | Análise concluída ({0}) |
| Found {0} {1} you can safely delete. | Encontrados {0} {1} que você pode excluir com segurança. |
| Preparing destination folder... | Preparando a pasta de destino... |
| Moving unneeded files... | Movendo arquivos desnecessários... |
| Deleting unneeded files... | Excluindo arquivos desnecessários... |
| Waiting for Windows Installer to finish... | Waiting for Windows Installer to finish... |
| Waiting for an installation that has not finished. If nothing is being installed, cancel and restart Windows. | Waiting for an installation that has not finished. If nothing is being installed, cancel and restart Windows. |
| Checking the remaining files again... | Checking the remaining files again... |
| Move cancelled. {0} of {1} {2} processed. | Movimentação cancelada após processar {0} de {1} {2}. |
| Delete cancelled. {0} of {1} {2} processed. | Exclusão cancelada após processar {0} de {1} {2}. |
| {0}. Details are in {1}. | {0}. Detalhes em {1}. |
| {0}. The crash log could not be written. | {0}. Não foi possível gravar o crash.log. |
| {0}. Details are in {1}. | {0}. Detalhes em {1}. |
| {0}. The crash log could not be written. | {0}. Não foi possível gravar o crash.log. |
| Access denied. Windows refused the scan. | Acesso negado. O Windows recusou a análise. |
| Scan stopped. | Análise interrompida. |
| Scan cancelled. | Análise cancelada. |
| Ready | Pronto |
| Scan failed ({0}). Details in {1}. | Falha na análise ({0}). Detalhes em {1}. |
| Scan failed ({0}). The crash log could not be written. | Falha na análise ({0}). Não foi possível gravar o crash.log. |

## Main screen text

| English | Português (Brasil) |
| --- | --- |
| Any unneeded files below are safe to delete. | Qualquer arquivo desnecessário abaixo pode ser excluído com segurança. |
| They sit in {InstallerFolder}. InstallerClean asks Windows about every installed program: a file is listed as safe to delete when no program claims it ({0}), or when a newer patch has replaced it and no program could roll back to it ({1}). | Eles ficam em {InstallerFolder}. O InstallerClean pergunta ao Windows sobre cada programa instalado: um arquivo entra na lista dos que podem ser excluídos com segurança quando nenhum programa o reivindica ({0}), ou quando um patch mais novo o substituiu e nenhum programa poderia voltar a ele ({1}). |
| Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update and uninstall as normal. Putting them back into {InstallerFolder} restores everything. Or delete them permanently now. | Mova-os para uma pasta de backup que você escolher e depois exclua essa pasta quando estiver convencido de que seus programas ainda atualizam e desinstalam normalmente. Colocá-los de volta em {InstallerFolder} restaura tudo. Ou exclua-os permanentemente agora. |
| Nothing scanned yet. | Nada foi analisado ainda. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Clique em Reanalisar para procurar em {InstallerFolder} arquivos de instalação que nenhum programa ainda precisa. |
| Nothing to show until the next scan. | Nada para mostrar até a próxima análise. |
| The scan that runs after a Move or Delete didn't finish, so there's no list to show. Press Re-scan to scan {InstallerFolder} again. | A análise feita depois de Mover ou Excluir não terminou, então não há nenhuma lista para mostrar. Clique em Reanalisar para analisar {InstallerFolder} de novo. |
| What stopped it is recorded in {0}. | O que a interrompeu fica registrado em {0}. |
| These files can't be cleaned up right now. | Estes arquivos não podem ser limpos agora. |
| Something is using Windows Installer right now, such as Windows Update or a program being installed or removed. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} until it has finished. Then Re-scan and they come back. | Algo está usando o Windows Installer neste momento, como o Windows Update ou um programa sendo instalado ou removido. Mover e Excluir ficam pausados enquanto isso acontece, para que o InstallerClean não toque em {InstallerFolder} até isso terminar. Depois, clique em Reanalisar e eles voltam. |
| Windows refused InstallerClean permission to check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows. InstallerClean will check again the next time you start it. If Windows still refuses, this isn't a machine InstallerClean can clean. | O Windows negou ao InstallerClean permissão para verificar se o Windows Installer está ocupado, então Mover e Excluir ficam pausados e o InstallerClean não vai tocar em {InstallerFolder}. Reinicie o Windows. Na próxima vez que você iniciar o InstallerClean, ele vai verificar de novo. Se o Windows continuar negando, este não é um computador que o InstallerClean possa limpar. |
| InstallerClean couldn't check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows. InstallerClean will check again the next time you start it. If it still can't check, this isn't a machine InstallerClean can clean. | O InstallerClean não conseguiu verificar se o Windows Installer está ocupado, então Mover e Excluir ficam pausados e o InstallerClean não vai tocar em {InstallerFolder}. Reinicie o Windows. Na próxima vez que você iniciar o InstallerClean, ele vai verificar de novo. Se ele continuar sem conseguir verificar, este não é um computador que o InstallerClean possa limpar. |
| InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}). Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows. InstallerClean will check again the next time you start it. If it still can't check, this isn't a machine InstallerClean can clean. | O InstallerClean não conseguiu verificar se o Windows Installer está ocupado, porque o Windows deu um erro: {0} (erro {1}). Mover e Excluir ficam pausados e o InstallerClean não vai tocar em {InstallerFolder}. Reinicie o Windows. Na próxima vez que você iniciar o InstallerClean, ele vai verificar de novo. Se ele continuar sem conseguir verificar, este não é um computador que o InstallerClean possa limpar. |
| Windows Installer is still running, as it does for a few minutes after a program has been installed, updated or removed, and for as long as an installer's window is left open. Move and Delete are paused until it has stopped, so InstallerClean won't touch {InstallerFolder}. Then Re-scan and they come back. | Windows Installer is still running, as it does for a few minutes after a program has been installed, updated or removed, and for as long as an installer's window is left open. Move and Delete are paused until it has stopped, so InstallerClean won't touch {InstallerFolder}. Then Re-scan and they come back. |
| An earlier Windows Installer installation hasn't finished, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows. An installation waiting for a restart carries on after it, and InstallerClean will check again the next time you start it. If this message comes back, run the setup of the program that was being installed or removed again, and Windows Installer will offer to undo the installation it didn't finish. | Uma instalação anterior do Windows Installer não terminou, então Mover e Excluir ficam pausados e o InstallerClean não vai tocar em {InstallerFolder}. Reinicie o Windows. Uma instalação que aguarda uma reinicialização continua após a reinicialização, e na próxima vez que você iniciar o InstallerClean, ele vai verificar de novo. Se esta mensagem voltar, execute de novo o instalador do programa que estava sendo instalado ou removido, e o Windows Installer vai oferecer a opção de desfazer as alterações feitas pela instalação que não foi concluída. |
| Windows has a change to {InstallerFolder} waiting for the next restart, so Move and Delete are paused. Restart Windows. InstallerClean will check again the next time you start it. | O Windows tem uma alteração em {InstallerFolder} aguardando a próxima reinicialização, então Mover e Excluir ficam pausados. Reinicie o Windows. Na próxima vez que você iniciar o InstallerClean, ele vai verificar de novo. |
| Windows has a file change waiting for the next restart, and InstallerClean can't tell which files it affects, so it can't rule out that they're in {InstallerFolder}. Move and Delete are paused. Restart Windows. InstallerClean will check again the next time you start it. | O Windows tem uma alteração de arquivos aguardando a próxima reinicialização, e o InstallerClean não consegue saber quais arquivos ela afeta, então não dá para descartar que estejam em {InstallerFolder}. Mover e Excluir ficam pausados. Reinicie o Windows. Na próxima vez que você iniciar o InstallerClean, ele vai verificar de novo. |
| InstallerClean couldn't read one of the Windows settings it checks before touching {InstallerFolder}, so it can't tell whether Windows Installer is busy or has work waiting for a restart. Move and Delete are paused. Restart Windows. InstallerClean will check again the next time you start it. If the setting still won't read, this isn't a machine InstallerClean can clean. | O InstallerClean não conseguiu ler uma das configurações do Windows que verifica antes de tocar em {InstallerFolder}, então não dá para saber se o Windows Installer está ocupado ou tem trabalho aguardando uma reinicialização. Mover e Excluir ficam pausados. Reinicie o Windows. Na próxima vez que você iniciar o InstallerClean, ele vai verificar de novo. Se a configuração continuar ilegível, este não é um computador que o InstallerClean possa limpar. |
| Windows Installer has something unfinished, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Once it has cleared, Re-scan and they come back. | O Windows Installer tem algo não concluído, então Mover e Excluir ficam pausados e o InstallerClean não vai tocar em {InstallerFolder}. Quando isso se resolver, clique em Reanalisar e eles voltam. |
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
| No files can be deleted | Nenhum arquivo pode ser excluído |
| One file ({2}) was left alone because Windows shows it changed after the scan started. If something was installing or updating, scan again once it has finished. | Um arquivo ({2}) foi deixado de lado porque o Windows indica que ele mudou depois que a análise começou. Se algo estava sendo instalado ou atualizado, faça uma nova análise quando terminar. |
| {0} {1} ({2}) were left alone because Windows shows they changed after the scan started. If something was installing or updating, scan again once it has finished. | {0} {1} ({2}) foram deixados de lado porque o Windows indica que eles mudaram depois que a análise começou. Se algo estava sendo instalado ou atualizado, faça uma nova análise quando terminar. |
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
| {0} file left alone. The scan listed it as safe to delete, but the final check before moving or deleting couldn't confirm it. | {0} arquivo deixado de lado. A análise o incluiu na lista dos que podem ser excluídos com segurança, mas a verificação final antes de mover ou excluir não pôde confirmar isso. |
| {0} files left alone. The scan listed them as safe to delete, but the final check before moving or deleting couldn't confirm it. | {0} arquivos deixados de lado. A análise os incluiu na lista dos que podem ser excluídos com segurança, mas a verificação final antes de mover ou excluir não pôde confirmar isso. |
| Moved {0} of {1} {2} to {3} before you cancelled. | Movimentação cancelada após mover {0} de {1} {2} para {3}. |
| Permanently deleted {0} of {1} {2} before you cancelled. | Exclusão permanente cancelada após remover {0} de {1} {2}. |
| It's simple to undo. Move them back into {InstallerFolder} and everything will be back to how it was. | É simples de desfazer. Mova-os de volta para {InstallerFolder} e tudo voltará a ser como era. |
| {0} {1} permanently deleted | {0} {1} excluído permanentemente |
| {0} {1} permanently deleted | {0} {1} excluídos permanentemente |
| Any tip is much appreciated | Qualquer gorjeta é muito bem-vinda |
| Send _anonymous report | Enviar relatório _anônimo |
| The report adds your result to [this chart] on GitHub, which shows how much space people are reclaiming with InstallerClean. It also holds these technical details, to help me spot any bugs: | O relatório adiciona o seu resultado a [este gráfico] no GitHub, que mostra quanto espaço as pessoas estão recuperando com o InstallerClean. Ele também contém estes detalhes técnicos, que me ajudam a encontrar bugs: |
| How much space was freed, and how many files were moved or deleted | Quanto espaço foi liberado e quantos arquivos foram movidos ou excluídos |
| Whether a Move went to the same drive, another drive, a removable drive or a network share | Se Mover levou os arquivos para a mesma unidade, outra unidade, uma unidade removível ou um compartilhamento de rede |
| How long the scan and the Move or Delete took | Quanto tempo levaram a análise e Mover ou Excluir |
| How many installer files there are of each kind, and how much space they take | Quantos arquivos de instalação há de cada tipo e quanto espaço ocupam |
| How many files were left alone, and why | Quantos arquivos foram deixados de lado e por quê |
| How often InstallerClean waited for a slow drive or network share, and how many it stopped waiting for | Quantas vezes o InstallerClean esperou por uma unidade ou um compartilhamento de rede lentos, e por quantos parou de esperar |
| How many programs and patches Windows has a record of | De quantos programas e patches o Windows tem registro |
| Whether Windows still makes old-style short file names | Se o Windows ainda cria nomes de arquivo curtos no estilo antigo |
| Whether Windows is 10 or 11 and x64 or Arm, and which language and country or region it's set to | Se o Windows é 10 ou 11 e x64 ou Arm, e em que idioma e país ou região está configurado |
| InstallerClean's version, how you got it (setup, portable, Scoop, the command-line tool on its own, built from source, or not known), whether the run was in InstallerClean or the command-line tool, and the language it was showing | A versão do InstallerClean, como você o obteve (setup, portable, Scoop, a ferramenta de linha de comando sozinha, compilado a partir do código-fonte ou desconhecido), se a execução foi no InstallerClean ou na ferramenta de linha de comando, e o idioma em que ele estava sendo exibido |
| Counts of anything that went wrong | Contagens de qualquer coisa que tenha dado errado |
| A random code made for this report, so that it's only counted once if it's sent more than once | Um código aleatório criado para este relatório, para que ele seja contado só uma vez se for enviado mais de uma vez |
| Apart from that code, each of these is a number or a label. Nothing in the report names you, your PC, your files or your programs, and I never see your IP address. | Tirando esse código, cada um desses itens é um número ou um rótulo. Nada no relatório identifica você, seu PC, seus arquivos ou seus programas, e eu nunca vejo seu endereço IP. |
| See exactly what's sent | Ver exatamente o que é enviado |

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
| InstallerClean stopped waiting for drive {0}, so it left alone any file it couldn't check without it. Re-scan once it's responding normally. | O InstallerClean parou de esperar pela unidade {0}, então deixou de lado qualquer arquivo que não conseguisse verificar sem essa unidade. Clique em Reanalisar quando ela voltar a responder normalmente. |
| InstallerClean stopped waiting for {0}, so it left alone any file it couldn't check without it. Re-scan once it's responding normally. | O InstallerClean parou de esperar por {0}, então deixou de lado qualquer arquivo que não conseguisse verificar sem esse local. Clique em Reanalisar quando o acesso voltar ao normal. |
| InstallerClean stopped waiting for more than one drive or share ({0}), so it left alone any file it couldn't check without them. Re-scan once they're responding normally. | O InstallerClean parou de esperar por mais de uma unidade ou compartilhamento ({0}), então deixou de lado qualquer arquivo que não conseguisse verificar sem eles. Clique em Reanalisar quando voltarem a responder normalmente. |
| Windows Installer was busy when the scan started, for example with Windows Update or a program being installed or removed, so InstallerClean left alone the files it can't check while it's busy. Re-scan once it has finished. | O Windows Installer estava ocupado quando a análise começou, por exemplo com o Windows Update ou um programa sendo instalado ou removido, então o InstallerClean deixou de lado os arquivos que não consegue verificar enquanto o Windows Installer estiver ocupado. Clique em Reanalisar quando o Windows Installer terminar. |
| An earlier Windows Installer installation hadn't finished when the scan started, so InstallerClean left alone the files it can't check until it has. Restart Windows. An installation waiting for a restart carries on after it, and InstallerClean will check again the next time you start it. If this message comes back, run the setup of the program that was being installed or removed again, and Windows Installer will offer to undo the installation it didn't finish. | Uma instalação anterior do Windows Installer não tinha terminado quando a análise começou, então o InstallerClean deixou de lado os arquivos que não consegue verificar até que essa instalação termine. Reinicie o Windows. Uma instalação que aguarda uma reinicialização continua após a reinicialização, e na próxima vez que você iniciar o InstallerClean, ele vai verificar de novo. Se esta mensagem voltar, execute de novo o instalador do programa que estava sendo instalado ou removido, e o Windows Installer vai oferecer a opção de desfazer as alterações feitas pela instalação que não foi concluída. |
| Windows had a change to {InstallerFolder} waiting for the next restart when the scan started, so InstallerClean left alone the files it can't check until that change is made. Restart Windows. InstallerClean will check again the next time you start it. | O Windows tinha uma alteração em {InstallerFolder} aguardando a próxima reinicialização quando a análise começou, então o InstallerClean deixou de lado os arquivos que não consegue verificar até que essa alteração seja aplicada. Reinicie o Windows. Na próxima vez que você iniciar o InstallerClean, ele vai verificar de novo. |
| Windows had a file change waiting for the next restart when the scan started, and InstallerClean couldn't tell which files it affects, so it left alone the files it can't check until that change is made. Restart Windows. InstallerClean will check again the next time you start it. | O Windows tinha uma alteração de arquivos aguardando a próxima reinicialização quando a análise começou, e o InstallerClean não conseguiu saber quais arquivos ela afeta, então deixou de lado os arquivos que não consegue verificar até que essa alteração seja aplicada. Reinicie o Windows. Na próxima vez que você iniciar o InstallerClean, ele vai verificar de novo. |
| Windows refused InstallerClean permission to check whether Windows Installer was busy when the scan started, so it left alone the files it can't check while Windows Installer is busy. Restart Windows. InstallerClean will check again the next time you start it. | O Windows negou ao InstallerClean permissão para verificar se o Windows Installer estava ocupado quando a análise começou, então o InstallerClean deixou de lado os arquivos que não consegue verificar enquanto o Windows Installer estiver ocupado. Reinicie o Windows. Na próxima vez que você iniciar o InstallerClean, ele vai verificar de novo. |
| InstallerClean couldn't read a Windows setting it checks, so it couldn't tell whether Windows Installer was busy or had work waiting for a restart when the scan started. It left alone the files it can't check while Windows Installer is busy. Restart Windows. InstallerClean will check again the next time you start it. | O InstallerClean não conseguiu ler uma configuração do Windows que ele verifica, então não conseguiu saber se o Windows Installer estava ocupado ou tinha trabalho aguardando uma reinicialização quando a análise começou. Ele deixou de lado os arquivos que não consegue verificar enquanto o Windows Installer estiver ocupado. Reinicie o Windows. Na próxima vez que você iniciar o InstallerClean, ele vai verificar de novo. |
| InstallerClean couldn't check whether Windows Installer was busy when the scan started, so it left alone the files it can't check while Windows Installer is busy. Restart Windows. InstallerClean will check again the next time you start it. | O InstallerClean não conseguiu verificar se o Windows Installer estava ocupado quando a análise começou, então deixou de lado os arquivos que não consegue verificar enquanto o Windows Installer estiver ocupado. Reinicie o Windows. Na próxima vez que você iniciar o InstallerClean, ele vai verificar de novo. |
| InstallerClean couldn't check whether Windows Installer was busy when the scan started. Windows gave an error: {0} (error {1}). It left alone the files it can't check while Windows Installer is busy. Restart Windows. InstallerClean will check again the next time you start it. | O InstallerClean não conseguiu verificar se o Windows Installer estava ocupado quando a análise começou. O Windows deu um erro: {0} (erro {1}). O InstallerClean deixou de lado os arquivos que não consegue verificar enquanto o Windows Installer estiver ocupado. Reinicie o Windows. Na próxima vez que você iniciar o InstallerClean, ele vai verificar de novo. |
| Windows Installer had something unfinished when the scan started, so InstallerClean left alone the files it can't check until it has finished. Re-scan once it has finished. | O Windows Installer tinha algo não concluído quando a análise começou, então o InstallerClean deixou de lado os arquivos que não consegue verificar até que isso termine. Clique em Reanalisar quando isso terminar. |
| This PC's clock went back or lost time while the scan was running, so InstallerClean couldn't tell which files had just been added, and left alone every file that could have been. Re-scan. | O relógio deste PC voltou atrás ou atrasou durante a análise, então o InstallerClean não conseguiu saber quais arquivos tinham acabado de ser adicionados e deixou de lado todos os que podem ser um deles. Clique em Reanalisar. |
| A scan has put back into {InstallerFolder} files an earlier Move or Delete had left in InstallerClean's own folder there: {0} | A scan has put back into {InstallerFolder} files an earlier Move or Delete had left in InstallerClean's own folder there: {0} |
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
| InstallerClean stopped | O InstallerClean parou |
| Scan failed | Falha na análise |
| Nothing has been removed. | Nada foi removido. |
| No file has been listed as safe to delete and nothing has been removed. | Nenhum arquivo foi incluído na lista dos que podem ser excluídos com segurança e nada foi removido. |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. | Os registros do Windows Installer voltaram completamente vazios: nenhum programa instalado e nenhuma atualização reivindica um arquivo de instalação em cache. Isso não acontece em uma máquina que funciona (até uma instalação nova do Windows tem alguns), então ou os registros estão danificados ou não puderam ser lidos, e uma análise que acreditasse nessa resposta chamaria erroneamente de órfão cada arquivo em {InstallerFolder}. Em vez disso, o InstallerClean parou. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. | O Windows Installer não deixou o InstallerClean listar o que está instalado. O InstallerClean já estava em execução como administrador, então executá-lo de novo como administrador não muda nada. Sem essa lista não há como saber com segurança quais arquivos em cache ainda são necessários, então o InstallerClean parou. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: it read {1} {2}, then an entry came back unreadable (error code {0}). Rather than work from a part-read list, InstallerClean stopped. | O Windows Installer não conseguiu dar ao InstallerClean uma lista legível dos programas instalados: ele leu {1} {2} e depois uma entrada voltou ilegível (código de erro {0}). Em vez de trabalhar com uma lista lida pela metade, o InstallerClean parou. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. | O Windows Installer nunca sinalizou o fim da lista de programas instalados: o InstallerClean leu {2} {3} e depois desistiu após {0} entradas (último código de erro {1}). Não dá para confiar em uma lista sem fim, então o InstallerClean parou. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. | O Windows Installer nunca sinalizou o fim da lista de patches de um programa: o InstallerClean leu {2} {3} e depois desistiu após {0} entradas (último código de erro {1}). Não dá para confiar em uma lista sem fim, então o InstallerClean parou. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. | O InstallerClean não conseguiu casar os registros do Windows Installer com o conteúdo de {InstallerFolder}. Quase nada do que os registros apontam está de fato lá, e quase nada do que está lá é nomeado por algum registro, então não foi possível mostrar que algum arquivo fosse desnecessário. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. | O InstallerClean não conseguiu casar os registros do Windows Installer com o conteúdo de {InstallerFolder}. A pasta tem arquivos, mas nenhum registro aponta para nada lá dentro, então não foi possível mostrar que algum arquivo fosse desnecessário. |
| InstallerClean can't scan {InstallerFolder} on this PC. Windows reports that folder as {0}, and InstallerClean only scans an Installer folder that Windows reports at its own path. | O InstallerClean não pode analisar {InstallerFolder} neste PC. O Windows informa essa pasta como {0}, e o InstallerClean só analisa uma pasta Installer que o Windows informe em seu próprio caminho. |
| Windows reports that {InstallerFolder} doesn't exist on this PC, so InstallerClean stopped. | O Windows informa que {InstallerFolder} não existe neste PC, então o InstallerClean parou. |
| Windows reported an error when InstallerClean tried to list the files in {InstallerFolder}, so it couldn't get a complete list and stopped. | O Windows informou um erro quando o InstallerClean tentou listar os arquivos em {InstallerFolder}, então ele não conseguiu obter uma lista completa e parou. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. | O InstallerClean não conseguiu ler o suficiente dos registros do Windows Installer para ter certeza do que ainda é necessário: a lista de programas instalados voltou incompleta, e ler esses mesmos registros direto do registro do Windows também deu erros. Um arquivo poderia parecer órfão só porque o registro que o nomeia era um dos ilegíveis, então o InstallerClean parou. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was listed as safe to delete. This scan found nothing because that check failed, not because the folder is clean. | O InstallerClean não conseguiu que o Windows resolvesse o caminho verdadeiro de {InstallerFolder}, então não foi possível mostrar que algum arquivo estivesse dentro dela e nenhum foi incluído na lista dos que podem ser excluídos com segurança. Esta análise não encontrou nada porque essa verificação falhou, não porque a pasta esteja limpa. |
| Invalid destination | Destino inválido |
| Move stopped | Movimentação interrompida |
| Delete stopped | Delete stopped |
| InstallerClean stopped because Windows Installer's records kept changing while it checked the files, which happens while programs are being installed or updated. Files already moved or deleted stay so. The rest were not touched. | InstallerClean stopped because Windows Installer's records kept changing while it checked the files, which happens while programs are being installed or updated. Files already moved or deleted stay so. The rest were not touched. |
| InstallerClean stopped because Windows would not let it watch the registry keys where Windows Installer keeps its records, so it cannot tell whether they change while it works. Files already moved or deleted stay so. The rest were not touched. | InstallerClean stopped because Windows would not let it watch the registry keys where Windows Installer keeps its records, so it cannot tell whether they change while it works. Files already moved or deleted stay so. The rest were not touched. |
| InstallerClean could not make its own folder, {0}, inside the Installer folder (Windows error {1}), so it did not move or delete anything. | InstallerClean could not make its own folder, {0}, inside the Installer folder (Windows error {1}), so it did not move or delete anything. |
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
| This file could not be put back in its place. It is whole, in {0}. | This file could not be put back in its place. It is whole, in {0}. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Recusando mover arquivos para a pasta do Windows Installer (destino: {0}). |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | A pasta de backup precisa ser um caminho completo até uma pasta, começando por uma letra de unidade ou um compartilhamento de rede (por exemplo D:\Backup, ou \\servidor\backup). O InstallerClean não pode usar esta: {0} |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then Re-scan and try again. | O InstallerClean não pôde mais confirmar a pasta de backup, então parou. Verifique {0}, depois Reanalisar e tente de novo. |
| Cannot write to {0}. | Não é possível gravar em {0}. |
| You can save files in {0} but not open them again, so InstallerClean won't move files there. | Você pode salvar arquivos em {0}, mas não pode abri-los de novo, então o InstallerClean não vai mover arquivos para lá. |
| A file called '{0}' is already in the backup folder. | Já existe um arquivo chamado '{0}' na pasta de backup. |

## Update check

| English | Português (Brasil) |
| --- | --- |
| Check for updates | Verificar atualizações |
| Checking... | Verificando... |
| Up to date. | Tudo atualizado. |
| InstallerClean {0} is available. You have {1}. | O InstallerClean {0} está disponível. Você tem a versão {1}. |
| It's strongly recommended to use the latest version. | É altamente recomendável usar a versão mais recente. |
| Download it from GitHub | Baixe no GitHub |
| Download .NET Framework 4.8 from Microsoft | Baixe o .NET Framework 4.8 no site da Microsoft |
| Update now | Atualizar agora |
| This copy was installed by Scoop. To update it, close InstallerClean and run: | Esta cópia foi instalada pelo Scoop. Para atualizá-la, feche o InstallerClean e execute: |
| This copy was installed by Scoop for all users. To update it, close InstallerClean, open a terminal as administrator and run: | Esta cópia foi instalada pelo Scoop para todos os usuários. Para atualizá-la, feche o InstallerClean, abra um terminal como administrador e execute: |
| It needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then open InstallerClean to update. | Esta versão precisa do .NET Framework 4.8. Instale-o pelo site da Microsoft, reinicie o PC e depois abra o InstallerClean para atualizar. |
| InstallerClean {0} can't be installed automatically. | O InstallerClean {0} não pode ser instalado automaticamente. |
| InstallerClean can't tell whether its setup installed it, so it can't update itself. | O InstallerClean não consegue saber se foi instalado pelo próprio instalador, então não pode se atualizar. |
| Copied | Copiado |
| Couldn't copy it. Select the command and copy it yourself. | Não foi possível copiar. Selecione o comando e copie-o manualmente. |
| _Continue without updating | _Continuar sem atualizar |
| Downloading InstallerClean {0}... | Baixando o InstallerClean {0}... |
| Checking the download... | Verificando o download... |
| The update couldn't be downloaded. | Não foi possível baixar a atualização. |
| The update couldn't be installed. | Não foi possível instalar a atualização. |
| InstallerClean has been updated to {0}. | O InstallerClean foi atualizado para a versão {0}. |
| What's new | Novidades |
| Couldn't reach GitHub. Check your internet connection and try again. | Não foi possível acessar o GitHub. Verifique a sua conexão com a internet e tente de novo. |
| The update check received an error instead of GitHub's releases page. Try again later, or open [the releases page] in your browser. | A verificação de atualizações recebeu um erro em vez da página de versões do GitHub. Tente de novo mais tarde, ou abra [a página de versões] no navegador. |
| The update check couldn't tell which version is the newest from GitHub's answer. Try again later, or open [the releases page] in your browser. | A verificação de atualizações não conseguiu saber, pela resposta do GitHub, qual é a versão mais recente. Tente de novo mais tarde, ou abra [a página de versões] no navegador. |
| The check timed out. Your connection to GitHub may be slow; try again. | A verificação expirou. A sua conexão com o GitHub pode estar lenta; tente de novo. |
| The check failed for an unknown reason. Details are in {0} if you need to report it. | A verificação falhou por um motivo desconhecido. Os detalhes estão em {0}, se você precisar relatar o problema. |
| The check failed for an unknown reason. The crash log could not be written. | A verificação falhou por um motivo desconhecido. Não foi possível gravar o crash.log. |

## Opening links in your browser

| English | Português (Brasil) |
| --- | --- |
| Couldn't open your browser | Não foi possível abrir o navegador |
| The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | O link está na área de transferência, então você pode colá-lo:<br><br>{0} |
| InstallerClean couldn't copy the link to your clipboard either, so here it is:<br><br>{0} | O InstallerClean também não conseguiu copiar o link para a área de transferência, então aqui está:<br><br>{0} |

## Startup and crashes

| English | Português (Brasil) |
| --- | --- |
| InstallerClean | InstallerClean |
| It's already running. | Já está em execução. |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | Ocorreu um erro inesperado e o InstallerClean precisa fechar.<br><br>{0}<br><br>Detalhes gravados em:<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | Ocorreu um erro inesperado e o InstallerClean precisa fechar.<br><br>{0}<br><br>Não foi possível gravar o crash.log. |
| Startup error | Erro de inicialização |
| InstallerClean won't start because there is a file called {0} beside it, and InstallerClean doesn't come with one. Delete that file, then open InstallerClean again.<br><br>{1} | O InstallerClean não vai iniciar porque há um arquivo chamado {0} ao lado dele, e o InstallerClean não vem com nenhum. Exclua esse arquivo e abra o InstallerClean novamente.<br><br>{1} |
| InstallerClean needs Windows 10 version 1607 or later, or Windows 11. | O InstallerClean precisa do Windows 10 versão 1607 ou posterior, ou do Windows 11. |
| InstallerClean needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then open InstallerClean again. | O InstallerClean precisa do .NET Framework 4.8. Instale-o pelo site da Microsoft, reinicie o PC e depois abra o InstallerClean de novo. |
| Failed to start ({0}). Details written to:<br>{1} | Falha ao iniciar ({0}). Detalhes gravados em:<br>{1} |
| Failed to start ({0}). The crash log could not be written. | Falha ao iniciar ({0}). Não foi possível gravar o crash.log. |
| Administrator rights needed | São necessários privilégios de administrador |
| InstallerClean needs administrator rights to see every program installed on this PC, and it isn't running as administrator, so it won't scan. Nothing has been changed.<br><br>Run InstallerClean again as administrator. | O InstallerClean precisa de privilégios de administrador para ver todos os programas instalados neste PC e não está sendo executado como administrador, então não vai fazer a análise. Nada foi alterado.<br><br>Execute o InstallerClean novamente como administrador. |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # O crash.log registra exceções não tratadas do InstallerClean.<br># Com privilégios elevados, as mensagens de exceção do framework podem<br># incluir caminhos de arquivo da sessão em execução (inclusive perfis<br># de outros usuários enumerados pelas consultas do Windows Installer).<br># Mensagens de falha de rede da verificação de atualizações ou do envio<br># do log de resultados podem incluir a URL de destino e o IP ou proxy<br># resolvido. Entradas sobre registros ilegíveis do Windows Installer<br># podem incluir um SID de conta do Windows (S-1-5-21-...) e os códigos<br># de produto do software instalado.<br># Remova os três tipos de dado antes de anexar este arquivo a um<br># relatório de erro público.<br> |

## Tooltips (hover text)

| English | Português (Brasil) |
| --- | --- |
| Any tip is much appreciated | Qualquer gorjeta é muito bem-vinda |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Cancelamento solicitado. O InstallerClean está esperando o passo atual chegar a um ponto em que possa parar. Isso pode levar alguns segundos durante operações intensas de E/S ou uma chamada ao banco de dados MSI. |
| Close | Fechar |
| I really should charge for InstallerClean - because it's been a huge amount of work - but I want to keep it free so it helps as many people as possible. Your donation makes it worthwhile and is very much appreciated. | Eu realmente deveria cobrar pelo InstallerClean, porque deu um trabalho enorme, mas quero mantê-lo gratuito para que ajude o maior número possível de pessoas. Sua doação faz o esforço valer a pena, e eu agradeço muito. |
| A star helps other people find it | Uma estrela ajuda outras pessoas a encontrar o InstallerClean |
| Minimise | Minimizar |
| Move the unneeded files to the backup folder | Move os arquivos desnecessários para a pasta de backup |
| Move the unneeded files to a backup folder. You'll choose it next. | Move os arquivos desnecessários para uma pasta de backup. Você a escolhe em seguida. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder. | Move os arquivos desnecessários para a pasta de backup. Ela está na mesma unidade, então você só recupera o espaço depois de excluir essa pasta. |
| Delete the unneeded files permanently. Use Move instead if you'd like a chance to satisfy yourself all is well. | Exclui permanentemente os arquivos desnecessários. Use Mover se quiser a chance de se convencer de que está tudo bem. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Nome do titular do certificado Authenticode incorporado. Cadeia não verificada. |
| Change language. The program will restart. | Alterar idioma. O programa será reiniciado. |

## Screen reader labels

| English | Português (Brasil) |
| --- | --- |
| Leave a tip | Deixar uma gorjeta |
| Leave a tip | Deixar uma gorjeta |
| Leave a tip | Deixar uma gorjeta |
| Cancel operation | Cancelar a operação |
| Cancel scan | Cancelar a análise |
| Cancel startup scan | Cancelar a análise inicial |
| Carry on without it, drive {0} | Continuar sem esperar, unidade {0} |
| Carry on without it, {0} | Continuar sem esperar, {0} |
| Close | Fechar |
| Close window | Fechar a janela |
| Close result and return to main window | Fechar o resultado e voltar para a janela principal |
| Leave a star on github | Deixe uma estrela no github |
| Minimise | Minimizar |
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Excluir permanentemente remove os arquivos desnecessários. Cancelar fecha sem excluir nada. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Mover coloca os arquivos desnecessários na pasta de destino escolhida. Cancelar os deixa onde estão. |
| Say thanks | Agradeça |
| Check for updates | Verificar atualizações |
| Checks github's releases page for a newer version. | Consulta a página de versões do github em busca de uma versão mais recente. |
| Opens the readme on github in your browser. | Abre o readme no github no seu navegador. |
| Opens this version's release page on github in your browser. | Abre a página desta versão no github no seu navegador. |
| Opens the issue tracker on github.com in your browser. | Abre o rastreador de problemas (Issues) em github.com no seu navegador. |
| Shows the licences of the fonts, icons and code InstallerClean includes. | Mostra as licenças das fontes, dos ícones e do código que o InstallerClean inclui. |
| If ticked, InstallerClean checks github for a newer version when you run it. | Se marcada, o InstallerClean consulta o github em busca de uma versão mais recente quando você o executa. |
| Download it from github | Baixe no github |
| Opens InstallerClean's releases on github in your browser, newest first. | Abre as versões do InstallerClean no github no seu navegador, da mais recente para a mais antiga. |
| Download .NET Framework 4.8 from Microsoft | Baixe o .NET Framework 4.8 no site da Microsoft |
| Copy the command | Copiar o comando |
| Scoop's update command | Comando de atualização do Scoop |
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
| Waiting for a drive or share | Aguardando uma unidade ou compartilhamento |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | Arquivos que não puderam ser processados |
| Explains this folder, and how to recover a file, in the README | Explica esta pasta, e como recuperar um arquivo, no README |
| {0} Press Alt+{1} to carry on without it. {2} | {0} Pressione Alt+{1} para continuar sem esperar. {2} |
| What the report holds | O que o relatório contém |
| What the report holds | O que o relatório contém |
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
| drive {0} | unidade {0} |
| .  | .  |
| {0} TB | {0} TB |
| {0} GB | {0} GB |
| {0} MB | {0} MB |
| {0} KB | {0} KB |
| {0} B | {0} B |
| {0:F0}ms | {0:F0}ms |
| {0:F1}s | {0:F1}s |
| less than a second | menos de um segundo |
| {0:F1} seconds | {0:F1} segundos |

## Command-line tool (installerclean-cli)

| English | Português (Brasil) |
| --- | --- |
| InstallerClean needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then run InstallerClean again. | O InstallerClean precisa do .NET Framework 4.8. Instale-o pelo site da Microsoft, reinicie o PC e depois execute o InstallerClean de novo. |
| Error: unknown argument '{0}' | Erro: argumento desconhecido '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Erro: argumento extra inesperado '{0}'. Se a sua pasta de destino tiver um espaço no nome, coloque aspas em todo o caminho: /m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | Erro: argumento extra inesperado '{0}'. /s e /d não aceitam mais argumentos, e só se pode usar um sinalizador por execução. |
| Cancelling... | Cancelando... |
| Cancelled. | Cancelado. |
| Error: unexpected failure ({0}). Details written to {1}. | Erro: falha inesperada ({0}). Detalhes gravados em {1}. |
| Error: unexpected failure ({0}). The crash log could not be written. | Erro: falha inesperada ({0}). Não foi possível gravar o log de falhas. |
| Scanning {InstallerFolder}... | Analisando {InstallerFolder}... |
| This scan put back into {InstallerFolder} files an earlier Move or Delete had left in InstallerClean's own folder there: {0} | This scan put back into {InstallerFolder} files an earlier Move or Delete had left in InstallerClean's own folder there: {0} |
| Found {0} unneeded {1} to clean up ({2}). | Foram encontrados {0} {1} desnecessários para limpar ({2}). |
| Found {0} unneeded {1} to clean up ({2}).<br>_(one form)_ | Foi encontrado {0} {1} desnecessário para limpar ({2}). |
| No unneeded files identified. | Nenhum arquivo desnecessário identificado. |
| Windows has a record for {0} file that is not in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. To put the file back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | O Windows tem um registro de {0} arquivo que não está em {InstallerFolder}: {1}. No dia a dia não causa problema, mas uma atualização ou desinstalação desse programa pode falhar. Para recolocar o arquivo, você precisa do instalador da versão que já tem. Consiga-o com o fabricante do programa e execute-o por cima da sua cópia atual. Uma versão mais nova não serve: ela teria que remover primeiro a que você tem, e é justamente essa etapa que precisa deste arquivo. Desinstalar primeiro também não funciona, pelo mesmo motivo. Isso deve restaurar o arquivo e deixar suas configurações intactas, mas a Microsoft não garante. |
| Windows has records for {0} files that are not in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. To put a file back, you need the installer for the version you already have of that program. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs the file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | O Windows tem registros de {0} arquivos que não estão em {InstallerFolder}: {1}. No dia a dia não causam problema, mas uma atualização ou desinstalação desses programas pode falhar. Para recolocar um arquivo, você precisa do instalador da versão desse programa que já tem. Consiga-o com o fabricante do programa e execute-o por cima da sua cópia atual. Uma versão mais nova não serve: ela teria que remover primeiro a que você tem, e é justamente essa etapa que precisa do arquivo. Desinstalar primeiro também não funciona, pelo mesmo motivo. Isso deve restaurar o arquivo e deixar suas configurações intactas, mas a Microsoft não garante. |
| InstallerClean stopped waiting for drive {0}, so it left alone any file it couldn't check without it. Once it's responding normally, run the command again. | O InstallerClean parou de esperar pela unidade {0}, então deixou de lado qualquer arquivo que não conseguisse verificar sem essa unidade. Quando ela voltar a responder normalmente, execute o comando de novo. |
| InstallerClean stopped waiting for {0}, so it left alone any file it couldn't check without it. Once it's responding normally, run the command again. | O InstallerClean parou de esperar por {0}, então deixou de lado qualquer arquivo que não conseguisse verificar sem esse local. Quando o acesso voltar ao normal, execute o comando de novo. |
| InstallerClean stopped waiting for more than one drive or share ({0}), so it left alone any file it couldn't check without them. Once they're responding normally, run the command again. | O InstallerClean parou de esperar por mais de uma unidade ou compartilhamento ({0}), então deixou de lado qualquer arquivo que não conseguisse verificar sem eles. Quando voltarem a responder normalmente, execute o comando de novo. |
| Deleting {0} unneeded {1}... | Excluindo {0} {1} desnecessários... |
| Deleting {0} unneeded {1}...<br>_(one form)_ | Excluindo {0} {1} desnecessário... |
| Permanently deleted {0} unneeded {1}. | Foram excluídos permanentemente {0} {1} desnecessários. |
| Permanently deleted {0} unneeded {1}.<br>_(one form)_ | Foi excluído permanentemente {0} {1} desnecessário. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Erro: nenhum destino de movimentação especificado. Use /m CAMINHO. (Um padrão definido na GUI é por usuário e não se aplica a execuções agendadas ou em contas de serviço.) |
| Error: destination cannot be inside the Windows Installer folder. | Erro: o destino não pode estar dentro da pasta do Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Erro: o destino deve ser um caminho totalmente qualificado. Recebido: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Erro: o destino {0} é resolvido dentro de uma pasta de sistema do Windows. Escolha um caminho fora de %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% e %ProgramData%. |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | Erro: espaço insuficiente em {0}. Mover estes arquivos precisa de {1} e há {2} livres. Nada foi movido. |
| Error: something is using Windows Installer right now, such as Windows Update or a program being installed or removed. /m and /d are blocked while that runs. Try again once it has finished. | Erro: algo está usando o Windows Installer neste momento, como o Windows Update ou um programa sendo instalado ou removido. /m e /d ficam bloqueados enquanto isso acontece. Tente de novo quando terminar. |
| Error: an earlier Windows Installer installation hasn't finished. /m and /d are blocked, so InstallerClean won't touch {InstallerFolder}. Restart Windows, then run the command again. An installation waiting for a restart carries on after it. If this message comes back, run the setup of the program that was being installed or removed again, and Windows Installer will offer to undo the installation it didn't finish. | Erro: uma instalação anterior do Windows Installer não terminou. /m e /d ficam bloqueados, então o InstallerClean não vai tocar em {InstallerFolder}. Reinicie o Windows e depois execute o comando de novo. Uma instalação que aguarda uma reinicialização continua após a reinicialização. Se esta mensagem voltar, execute de novo o instalador do programa que estava sendo instalado ou removido, e o Windows Installer vai oferecer a opção de desfazer as alterações feitas pela instalação que não foi concluída. |
| Error: Windows has a change to {InstallerFolder} waiting for the next restart ({0}). /m and /d are blocked. Restart Windows, then try again. | Erro: o Windows tem uma alteração em {InstallerFolder} aguardando a próxima reinicialização ({0}). /m e /d ficam bloqueados. Reinicie o Windows e tente de novo. |
| Error: Windows has a file change waiting for the next restart, and InstallerClean can't tell which files it affects, so it can't rule out that they're in {InstallerFolder}. /m and /d are blocked. Restart Windows, then try again. | Erro: o Windows tem uma alteração de arquivos aguardando a próxima reinicialização, e o InstallerClean não consegue saber quais arquivos ela afeta, então não dá para descartar que estejam em {InstallerFolder}. /m e /d ficam bloqueados. Reinicie o Windows e tente de novo. |
| Error: InstallerClean couldn't read one of the registry values it checks before touching {InstallerFolder}, so it can't tell whether Windows Installer is busy or has work waiting for a restart. /m and /d are blocked. Restart Windows and try again. If the read still fails, this isn't a machine InstallerClean can clean. | Erro: o InstallerClean não conseguiu ler um dos valores de registro que verifica antes de tocar em {InstallerFolder}, então não dá para saber se o Windows Installer está ocupado ou tem trabalho aguardando uma reinicialização. /m e /d ficam bloqueados. Reinicie o Windows e tente de novo. Se a leitura continuar falhando, este não é um computador que o InstallerClean possa limpar. |
| Error: InstallerClean couldn't check whether Windows Installer is busy, so it can't rule out an installation in progress. /m and /d are blocked. Restart Windows and try again. If the check still fails, this isn't a machine InstallerClean can clean. | Erro: o InstallerClean não conseguiu verificar se o Windows Installer está ocupado, então não dá para descartar uma instalação em andamento. /m e /d estão bloqueados. Reinicie o Windows e tente de novo. Se a verificação continuar falhando, este não é um computador que o InstallerClean possa limpar. |
| Error: InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}), so it can't rule out an installation in progress. /m and /d are blocked. Restart Windows and try again. If the check still fails, this isn't a machine InstallerClean can clean. | Erro: o InstallerClean não conseguiu verificar se o Windows Installer está ocupado, porque o Windows deu um erro: {0} (erro {1}), então não dá para descartar uma instalação em andamento. /m e /d estão bloqueados. Reinicie o Windows e tente de novo. Se a verificação continuar falhando, este não é um computador que o InstallerClean possa limpar. |
| Error: Windows Installer is still running, as it does for a few minutes after a program has been installed, updated or removed, and for as long as an installer's window is left open. /m and /d are blocked until it has stopped. Try again in a few minutes. | Error: Windows Installer is still running, as it does for a few minutes after a program has been installed, updated or removed, and for as long as an installer's window is left open. /m and /d are blocked until it has stopped. Try again in a few minutes. |
| Error: Windows Installer has something unfinished, so /m and /d are blocked and InstallerClean won't touch {InstallerFolder}. Try again once it has cleared. | Erro: o Windows Installer tem algo não concluído, então /m e /d ficam bloqueados e o InstallerClean não vai tocar em {InstallerFolder}. Tente de novo quando isso se resolver. |
| Waiting for Windows Installer to finish. InstallerClean carries on by itself when it has. | Waiting for Windows Installer to finish. InstallerClean carries on by itself when it has. |
| Checking the remaining files again. | Checking the remaining files again. |
| Error: Windows Installer stayed busy for 30 minutes, so InstallerClean stopped. Files already moved or deleted stay so. The rest were not touched. Run the command again later to carry on. | Error: Windows Installer stayed busy for 30 minutes, so InstallerClean stopped. Files already moved or deleted stay so. The rest were not touched. Run the command again later to carry on. |
| Error: Windows Installer's records kept changing while InstallerClean checked the files, which happens while programs are being installed or updated, so it stopped. Files already moved or deleted stay so. The rest were not touched. Try again once the installations have finished. | Error: Windows Installer's records kept changing while InstallerClean checked the files, which happens while programs are being installed or updated, so it stopped. Files already moved or deleted stay so. The rest were not touched. Try again once the installations have finished. |
| Error: Windows would not let InstallerClean watch the registry keys where Windows Installer keeps its records, so it cannot tell whether they change while it works, and it stopped. Files already moved or deleted stay so. The rest were not touched. | Error: Windows would not let InstallerClean watch the registry keys where Windows Installer keeps its records, so it cannot tell whether they change while it works, and it stopped. Files already moved or deleted stay so. The rest were not touched. |
| Error: InstallerClean could not make its own folder, {0}, inside {InstallerFolder} (Windows error {1}), so it did not move or delete anything. | Error: InstallerClean could not make its own folder, {0}, inside {InstallerFolder} (Windows error {1}), so it did not move or delete anything. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it stopped. Files already moved or deleted stay so. The rest were not touched. | Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it stopped. Files already moved or deleted stay so. The rest were not touched. |
| Windows Installer was busy when the scan started, for example with Windows Update or a program being installed or removed, so InstallerClean left alone the files it can't check while it's busy. Run the command again once it has finished. | O Windows Installer estava ocupado quando a análise começou, por exemplo com o Windows Update ou um programa sendo instalado ou removido, então o InstallerClean deixou de lado os arquivos que não consegue verificar enquanto o Windows Installer estiver ocupado. Execute o comando de novo quando o Windows Installer terminar. |
| An earlier Windows Installer installation hadn't finished when the scan started, so InstallerClean left alone the files it can't check until it has. Restart Windows, then run the command again. An installation waiting for a restart carries on after it. If this message comes back, run the setup of the program that was being installed or removed again, and Windows Installer will offer to undo the installation it didn't finish. | Uma instalação anterior do Windows Installer não tinha terminado quando a análise começou, então o InstallerClean deixou de lado os arquivos que não consegue verificar até que essa instalação termine. Reinicie o Windows e depois execute o comando de novo. Uma instalação que aguarda uma reinicialização continua após a reinicialização. Se esta mensagem voltar, execute de novo o instalador do programa que estava sendo instalado ou removido, e o Windows Installer vai oferecer a opção de desfazer as alterações feitas pela instalação que não foi concluída. |
| Windows had a change to {InstallerFolder} waiting for the next restart when the scan started, so InstallerClean left alone the files it can't check until that change is made. Restart Windows, then run the command again. | O Windows tinha uma alteração em {InstallerFolder} aguardando a próxima reinicialização quando a análise começou, então o InstallerClean deixou de lado os arquivos que não consegue verificar até que essa alteração seja aplicada. Reinicie o Windows e depois execute o comando de novo. |
| Windows had a file change waiting for the next restart when the scan started, and InstallerClean couldn't tell which files it affects, so it left alone the files it can't check until that change is made. Restart Windows, then run the command again. | O Windows tinha uma alteração de arquivos aguardando a próxima reinicialização quando a análise começou, e o InstallerClean não conseguiu saber quais arquivos ela afeta, então deixou de lado os arquivos que não consegue verificar até que essa alteração seja aplicada. Reinicie o Windows e depois execute o comando de novo. |
| Windows refused InstallerClean permission to check whether Windows Installer was busy when the scan started, so it left alone the files it can't check while Windows Installer is busy. Restart Windows, then run the command again. | O Windows negou ao InstallerClean permissão para verificar se o Windows Installer estava ocupado quando a análise começou, então o InstallerClean deixou de lado os arquivos que não consegue verificar enquanto o Windows Installer estiver ocupado. Reinicie o Windows e depois execute o comando de novo. |
| InstallerClean couldn't read a Windows setting it checks, so it couldn't tell whether Windows Installer was busy or had work waiting for a restart when the scan started. It left alone the files it can't check while Windows Installer is busy. Restart Windows, then run the command again. | O InstallerClean não conseguiu ler uma configuração do Windows que ele verifica, então não conseguiu saber se o Windows Installer estava ocupado ou tinha trabalho aguardando uma reinicialização quando a análise começou. Ele deixou de lado os arquivos que não consegue verificar enquanto o Windows Installer estiver ocupado. Reinicie o Windows e depois execute o comando de novo. |
| InstallerClean couldn't check whether Windows Installer was busy when the scan started, so it left alone the files it can't check while Windows Installer is busy. Restart Windows, then run the command again. | O InstallerClean não conseguiu verificar se o Windows Installer estava ocupado quando a análise começou, então deixou de lado os arquivos que não consegue verificar enquanto o Windows Installer estiver ocupado. Reinicie o Windows e depois execute o comando de novo. |
| InstallerClean couldn't check whether Windows Installer was busy when the scan started. Windows gave an error: {0} (error {1}). It left alone the files it can't check while Windows Installer is busy. Restart Windows, then run the command again. | O InstallerClean não conseguiu verificar se o Windows Installer estava ocupado quando a análise começou. O Windows deu um erro: {0} (erro {1}). O InstallerClean deixou de lado os arquivos que não consegue verificar enquanto o Windows Installer estiver ocupado. Reinicie o Windows e depois execute o comando de novo. |
| Windows Installer had something unfinished when the scan started, so InstallerClean left alone the files it can't check until it has finished. Run the command again once it has finished. | O Windows Installer tinha algo não concluído quando a análise começou, então o InstallerClean deixou de lado os arquivos que não consegue verificar até que isso termine. Execute o comando de novo quando isso terminar. |
| This PC's clock went back or lost time while the scan was running, so InstallerClean couldn't tell which files had just been added, and left alone every file that could have been. Run the command again. | O relógio deste PC voltou atrás ou atrasou durante a análise, então o InstallerClean não conseguiu saber quais arquivos tinham acabado de ser adicionados e deixou de lado todos os que podem ser um deles. Execute o comando de novo. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. | Erro: o Windows negou ao InstallerClean permissão para verificar se o Windows Installer estava ocupado, então não deu para descartar que um arquivo viesse a ser necessário no meio do caminho. Nada foi excluído. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. | Erro: o Windows negou ao InstallerClean permissão para verificar se o Windows Installer estava ocupado, então não deu para descartar que um arquivo viesse a ser necessário no meio do caminho. Nada foi movido. |
| Moving {0} unneeded {1} to {2}... | Movendo {0} {1} desnecessários para {2}... |
| Moving {0} unneeded {1} to {2}...<br>_(one form)_ | Movendo {0} {1} desnecessário para {2}... |
| Moved {0} unneeded {1}. | Foram movidos {0} {1} desnecessários. |
| Moved {0} unneeded {1}.<br>_(one form)_ | Foi movido {0} {1} desnecessário. |
| Check that your programs still update and uninstall as normal, then delete {0}. | Confirme que seus programas ainda atualizam e desinstalam normalmente e depois exclua {0}. |
| InstallerClean is free. If it helped, a tip is much appreciated: {0} | O InstallerClean é gratuito. Se ele ajudou, uma gorjeta é muito bem-vinda: {0} |
| Update now? It's strongly recommended to use the latest version. [{0}/{1}] | Atualizar agora? É altamente recomendável usar a versão mais recente. [{0}/{1}] |
| Y | S |
| N | N |
| No answer after a minute, so InstallerClean updates without asking, as it does when nobody is there to answer. | Sem resposta após um minuto, então o InstallerClean se atualiza sem perguntar, como quando não há ninguém para responder. |
| Send an anonymous report of this run? It is saved at {0}. Type ? to see what it holds. [{1}/{2}/?] | Enviar um relatório anônimo desta execução? Ele está salvo em {0}. Digite ? para ver o que ele contém. [{1}/{2}/?] |
| the chart at {0} | este gráfico em {0} |
| The report adds your result to {0}, which shows how much space people are reclaiming with InstallerClean. It also holds these technical details, to help me spot any bugs: | O relatório adiciona o seu resultado a {0}, que mostra quanto espaço as pessoas estão recuperando com o InstallerClean. Ele também contém estes detalhes técnicos, que me ajudam a encontrar bugs: |
| The full report is printed below. | O relatório completo é exibido abaixo. |
| Report sent. Thank you. | Relatório enviado. Obrigado. |
| Report not sent. | Relatório não enviado. |
| No answer after a minute. | Sem resposta após um minuto. |
| Cancelled while sending the report. It may have arrived anyway. | Cancelado durante o envio do relatório. Ele pode ter chegado mesmo assim. |
| The report could not be sent. It is kept in {0}. | Não foi possível enviar o relatório. Ele está guardado em {0}. |
| Sending a report left waiting by an earlier run. | Enviando um relatório pendente desde uma execução anterior. |
| The new version will scan and list what it finds, without deleting anything. To delete, run /d again afterwards. | A nova versão vai analisar e listar o que encontrar, sem excluir nada. Para excluir, execute /d novamente depois. |
| The new version will scan and list what it finds, without moving anything. To move, run /m again afterwards. | A nova versão vai analisar e listar o que encontrar, sem mover nada. Para mover, execute /m novamente depois. |
| InstallerClean is in use, so it can't update itself now. | O InstallerClean está em uso, então não pode se atualizar agora. |
| Running {0} with InstallerClean {1} instead. | Em vez disso, {0} será executado com o InstallerClean {1}. |
| The newer version couldn't be run. | Não foi possível executar a versão mais recente. |
| Running {0} with InstallerClean {1}. It takes this copy's place once the run ends. | {0} será executado com o InstallerClean {1}. Ao final da execução, ele substituirá esta cópia. |
| {0}: {1} | {0}: {1} |
| It needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then run InstallerClean again to update. | Esta versão precisa do .NET Framework 4.8. Instale-o pelo site da Microsoft, reinicie o PC e depois execute o InstallerClean de novo para atualizar. |
| It's simple to undo. Move them back from {0} into {InstallerFolder} and everything will be back to how it was. | É simples de desfazer. Mova-os de volta de {0} para {InstallerFolder} e tudo voltará a ser como era. |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then run the command again. | O InstallerClean não pôde mais confirmar a pasta de backup, então parou. Verifique {0} e execute o comando de novo. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Outro processo do InstallerClean mantém o bloqueio de instância única (a GUI ou outra execução da CLI). Código de saída 75 (transitório); seguro tentar novamente mais tarde. |
| Stopped after deleting {0} of {1} {2}: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 2 (partial). | Interrompido após excluir {0} de {1} {2}: o InstallerClean não podia mais ter certeza de que nenhum outro InstallerClean estava agindo. Execute o mesmo comando novamente. Código de saída 2 (parcial). |
| Stopped after moving {0} of {1} {2} to {3}: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 2 (partial). | Interrompido após mover {0} de {1} {2} para {3}: o InstallerClean não podia mais ter certeza de que nenhum outro InstallerClean estava agindo. Execute o mesmo comando novamente. Código de saída 2 (parcial). |
| Stopped before deleting any file: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 75 (transient). | Interrompido antes de excluir qualquer arquivo: o InstallerClean não podia mais ter certeza de que nenhum outro InstallerClean estava agindo. Execute o mesmo comando novamente. Código de saída 75 (transitório). |
| Stopped before moving any file: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 75 (transient). | Interrompido antes de mover qualquer arquivo: o InstallerClean não podia mais ter certeza de que nenhum outro InstallerClean estava agindo. Execute o mesmo comando novamente. Código de saída 75 (transitório). |
| InstallerClean needs administrator rights to see every program installed on this PC, and it isn't running as administrator, so it won't scan. Nothing has been changed. Run it again from an administrator prompt. | O InstallerClean precisa de privilégios de administrador para ver todos os programas instalados neste PC e não está sendo executado como administrador, então não vai fazer a análise. Nada foi alterado. Execute-o novamente a partir de um prompt como administrador. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Observação: falha ao gravar no Log de Eventos. Verifique as permissões do log de Aplicativo ou a Política de Grupo. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - limpeza de {InstallerFolder} |
| Removes cached .msi and .msp files that no installed program still needs. | Remove arquivos .msi/.msp em cache que nenhum programa instalado precisa. |
| Needs an elevated (administrator) prompt and won't scan without one. | Exige um prompt como administrador; sem ele, não faz a análise. |
| Usage: | Uso: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help      Mostra esta ajuda (aceita também /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version   Mostra a versão (aceita também -v) |
|   installerclean-cli --notices  Print the third-party notices |   installerclean-cli --notices   Mostra os avisos de terceiros |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s          Somente analisar - lista os supérfluos |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d          Exclui permanentemente os supérfluos |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m          Move para a pasta de backup salva |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m CAMINHO  Move para o caminho especificado |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | O installerclean-cli bloqueia o prompt até terminar, para que um script<br>ou uma tarefa agendada possa esperar por ele. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | Pasta salva por usuário; execuções agendadas ou SYSTEM: /m CAMINHO. |
| Before it scans, it checks for a newer version of InstallerClean, unless<br>"Check for updates automatically" is turned off for the account it runs<br>as. Where its window is on screen and nothing is piped into or out of it,<br>it asks before updating, and updates if nobody answers within a minute.<br>Anywhere else, such as a scheduled task set to run whether or not anyone<br>is logged on, it updates without asking.<br>Add /noupdate to skip the check. | Antes de analisar, ele verifica se há uma versão mais recente do<br>InstallerClean, a menos que a opção “Verificar atualizações<br>automaticamente” esteja desativada para a conta em que ele é executado.<br>Quando a janela dele está na tela e sua entrada e saída não estão<br>redirecionadas, ele pergunta antes de atualizar, e atualiza se ninguém<br>responder em um minuto. Em qualquer outro caso, como uma tarefa agendada<br>configurada para rodar com ou sem usuário conectado, ele atualiza sem<br>perguntar. Adicione /noupdate para pular a verificação. |
| Exit codes: | Códigos de saída: |
|   0   success: the run did what it was asked |   0   êxito: a execução fez o que foi pedido |
|   1   nothing processed: the run failed or was refused |   1   nada processado: a execução falhou ou foi recusada |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   parcial: alguns processados, outros não (falha ou Ctrl+C) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  transitório: algo temporário bloqueou a execução (veja a mensagem) |
|   130 cancelled (Ctrl+C) |   130 cancelado (Ctrl+C) |
