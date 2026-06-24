# InstallerClean UI in Português (Brasil) (Brazilian Portuguese)

The text of InstallerClean's interface in English on the left, with the Brazilian Portuguese translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Brazilian Portuguese can read through the translation and flag anything that doesn't read well. See [Can you help translate the GUI?](../../README.pt-BR.md#can-you-help-translate-the-gui) for how to suggest a change, whether an issue or a pull request.

A few lines (the app name, version and file-size formats) are meant to stay the same in every language, so leave those as they are. The translation file itself is [`Strings.pt-BR.resx`](../../src/InstallerClean.Core/Resources/Strings.pt-BR.resx). This page is generated from it by `scripts/gen-translation-table.mjs`, so do not edit it by hand.

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
| MIT licence | Licença MIT |
| _Move | _Mover |
| _Move instead | _Mover em vez disso |
| Path to folder if you Move instead of Delete | Caminho da pasta, se você Mover em vez de Excluir |
| Open _release page | Abrir a página da _versão |
| _Re-scan | _Reanalisar |
| _Scan again | Analisar de _novo |
| Send report | Enviar relatório |
| _Send | _Enviar |

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
| Found {0} {1} to clean up. | {0} {1} para limpar. |
| Preparing destination folder... | Preparando a pasta de destino... |
| Moving {0} {1}... | Movendo {0} {1}... |
| Deleting {0} {1}... | Excluindo {0} {1}... |
| Move cancelled. {0} of {1} {2} processed. | Movimentação cancelada após processar {0} de {1} {2}. |
| Delete cancelled. {0} of {1} {2} processed. | Exclusão cancelada após processar {0} de {1} {2}. |
| Move failed ({0}). Details in {1}. | Falha na movimentação ({0}). Detalhes em {1}. |
| Move failed ({0}). The crash log could not be written. | Falha na movimentação ({0}). Não foi possível gravar o crash.log. |
| Delete failed ({0}). Details in {1}. | Falha na exclusão ({0}). Detalhes em {1}. |
| Delete failed ({0}). The crash log could not be written. | Falha na exclusão ({0}). Não foi possível gravar o crash.log. |
| Access denied. Run as administrator. | Acesso negado. Execute como administrador. |
| Scan failed: installer database unavailable. | Falha na análise: banco de dados do instalador indisponível. |
| Scan cancelled. | Análise cancelada. |
| Done | Concluído |
| Scan failed ({0}). Details in {1}. | Falha na análise ({0}). Detalhes em {1}. |
| Scan failed ({0}). The crash log could not be written. | Falha na análise ({0}). Não foi possível gravar o crash.log. |

## Main screen text

| English | Português (Brasil) |
| --- | --- |
| The unneeded files below are safe to delete. | Os arquivos desnecessários abaixo podem ser excluídos com segurança. |
| They sit in C:\Windows\Installer, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | Eles ficam em C:\Windows\Installer, deixados para trás quando um programa foi desinstalado ({0}), um patch mais recente substituiu outro ({1}) ou o fabricante o retirou ({2}). O InstallerClean só lista arquivos que o próprio Windows informa ter terminado de usar. |
| Delete them to the Recycle Bin, or Move them elsewhere first if you'd rather keep a copy. | Exclua-os para a Lixeira, ou Mova-os para outro lugar antes, se preferir manter uma cópia. |
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
| Copy them back if anything stops working | Copie-os de volta se algo parar de funcionar |
| Restore them from the Recycle Bin if anything stops working | Restaure-os pela Lixeira se algo parar de funcionar |
| {0} freed | {0} liberados |
| {0} moved | {0} movidos |
| {0} moved, some files could not be processed | {0} movidos, alguns arquivos não puderam ser processados |
| {0} freed, some files could not be processed | {0} liberados, alguns arquivos não puderam ser processados |
| {0} {1} moved to {2} | {0} {1} em {2} |
| {0} {1} moved to {2}. {3} {4} | {0} {1} em {2}. {3} {4} |
| {0} {1} sent to the Recycle Bin | {0} {1} na Lixeira |
| {0} {1} sent to the Recycle Bin. {2} {3} | {0} {1} na Lixeira. {2} {3} |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. | {0} {1} excluído permanentemente. Não foi para a Lixeira. |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. | {0} {1} excluídos permanentemente. Não foram para a Lixeira. |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. {2} {3} | {0} {1} excluído permanentemente. Não foi para a Lixeira. {2} {3} |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. {2} {3} | {0} {1} excluídos permanentemente. Não foram para a Lixeira. {2} {3} |
| That's fine, it was safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Tudo bem, era seguro remover. O InstallerClean só remove os arquivos que o Windows informa ter terminado de usar, nunca um que um programa ainda precisa. No caso improvável de uma exclusão deixar um programa sem conseguir reparar, atualizar ou desinstalar, reinstalá-lo a partir do fabricante normalmente recoloca o arquivo, embora a Microsoft não garanta. |
| That's fine, they were safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Tudo bem, eram seguros para remover. O InstallerClean só remove os arquivos que o Windows informa ter terminado de usar, nunca um que um programa ainda precisa. No caso improvável de uma exclusão deixar um programa sem conseguir reparar, atualizar ou desinstalar, reinstalá-lo a partir do fabricante normalmente recoloca o arquivo, embora a Microsoft não garanta. |

## Recycle Bin unavailable

| English | Português (Brasil) |
| --- | --- |
| The Recycle Bin isn't available for this drive | A Lixeira não está disponível para esta unidade |
| So this {1} ({2}) hasn't been deleted. You can move it somewhere safe, or delete it permanently. | Então este {1} ({2}) não foi excluído. Você pode movê-lo para um lugar seguro, ou excluí-lo permanentemente. |
| So these {0} {1} ({2}) haven't been deleted. You can move them somewhere safe, or delete them permanently. | Então estes {0} {1} ({2}) não foram excluídos. Você pode movê-los para um lugar seguro, ou excluí-los permanentemente. |
| Deleting it is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Excluí-lo é seguro. O InstallerClean só remove os arquivos que o Windows informa ter terminado de usar, nunca um que um programa ainda precisa, e a Lixeira é apenas uma proteção extra. No caso improvável de uma exclusão deixar um programa sem conseguir reparar, atualizar ou desinstalar, reinstalá-lo a partir do fabricante normalmente recoloca o arquivo, embora a Microsoft não garanta. |
| Deleting them is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Excluí-los é seguro. O InstallerClean só remove os arquivos que o Windows informa ter terminado de usar, nunca um que um programa ainda precisa, e a Lixeira é apenas uma proteção extra. No caso improvável de uma exclusão deixar um programa sem conseguir reparar, atualizar ou desinstalar, reinstalá-lo a partir do fabricante normalmente recoloca o arquivo, embora a Microsoft não garanta. |

## Summaries and counts

| English | Português (Brasil) |
| --- | --- |
| {0} file still needed | {0} arquivo ainda necessário |
| {0} files still needed | {0} arquivos ainda necessários |
| {0} unneeded file to clean up | {0} arquivo desnecessário para limpar |
| {0} unneeded files to clean up | {0} arquivos desnecessários para limpar |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | {0} arquivo registrado está ausente (não foi excluído pelo InstallerClean). Sem problema agora, mas uma futura reparação, atualização ou desinstalação desse programa pode falhar. Abra Detalhes para saber o que fazer. |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | {0} arquivos registrados estão ausentes (não foram excluídos pelo InstallerClean). Sem problema agora, mas uma futura reparação, atualização ou desinstalação desses programas pode falhar. Abra Detalhes para saber o que fazer. |
| {0} stale MSI entry detected (file already gone from disk; InstallerClean doesn't unregister it). | {0} entrada MSI obsoleta detectada (o arquivo já sumiu do disco; o InstallerClean não a remove do registro). |
| {0} stale MSI entries detected (files already gone from disk; InstallerClean doesn't unregister them). | {0} entradas MSI obsoletas detectadas (os arquivos já sumiram do disco; o InstallerClean não as remove do registro). |
| {0} of {1} {2} | {0} de {1} {2} |
| {0} unneeded {1} ({2}) | {0} {1} para limpar ({2}) |
| {0} registered {1} ({2}) | {0} {1} registrados ({2}) |

## Confirmation dialogs

| English | Português (Brasil) |
| --- | --- |
| Move {0} {1} ({2})? | Mover {0} {1} ({2})? |
| Files will be moved to {0}. | Os arquivos serão movidos para {0}. |
| Delete {0} {1} ({2})? | Excluir {0} {1} ({2})? |
| Files will be sent to the Recycle Bin. If you'd like backup copies, use Move instead. | Os arquivos serão enviados para a Lixeira. Se quiser cópias de backup, use Mover em vez disso. |

## Error messages

| English | Português (Brasil) |
| --- | --- |
| Administrator rights required | Direitos de administrador necessários |
| InstallerClean requires administrator privileges.<br><br>Please right-click and choose 'Run as administrator'. | O InstallerClean requer privilégios de administrador.<br><br>Clique com o botão direito e escolha 'Executar como administrador'. |
| Installer database unavailable | Banco de dados do instalador indisponível |
| Scan failed | Falha na análise |
| The Windows Installer database appears to be empty or inaccessible. This is unusual even on a fresh Windows install and typically means the database is corrupt or a third-party tool has cleared it. Running 'sfc /scannow' from an elevated prompt usually repairs it. | O banco de dados do Windows Installer parece estar vazio ou inacessível. Isso é incomum mesmo em uma instalação nova do Windows e normalmente significa que o banco de dados está corrompido ou que uma ferramenta de terceiros o esvaziou. Executar 'sfc /scannow' em um prompt elevado costuma repará-lo. |
| Access denied enumerating installed products. Run as administrator. | Acesso negado ao enumerar os produtos instalados. Execute como administrador. |
| Windows Installer refused to list products after {0} consecutive failures (last error code {1}). Try restarting Windows, or run 'sfc /scannow' from an elevated prompt. | O Windows Installer recusou-se a listar os produtos após {0} falhas consecutivas (último código de erro {1}). Tente reiniciar o Windows, ou execute 'sfc /scannow' em um prompt elevado. |
| Invalid destination | Destino inválido |
| Could not write to destination | Não foi possível gravar no destino |
| Move failed | Falha na movimentação |
| Delete failed | Falha na exclusão |
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
| Access denied. | Acesso negado. |
| The operation failed. Try again or restart Windows. | A operação falhou. Tente de novo ou reinicie o Windows. |
| Unknown error. | Erro desconhecido. |
| Couldn't send this file to the Recycle Bin (error {0}). It may be locked, in use or blocked by Windows. Move it instead. | Não foi possível enviar este arquivo para a Lixeira (erro {0}). Ele pode estar bloqueado, em uso ou impedido pelo Windows. Mova-o em vez disso. |
| Windows blocked access to this file, even with administrator rights (error {0}). It is usually an ownership or permissions lock. Move it instead. | O Windows bloqueou o acesso a este arquivo, mesmo com direitos de administrador (erro {0}). Normalmente é um bloqueio de propriedade ou de permissões. Mova-o em vez disso. |
| This file is open or locked by another program (error {0}). Close that program, or whatever is scanning it, then try again, or Move it instead. | Este arquivo está aberto ou bloqueado por outro programa (erro {0}). Feche esse programa, ou o que estiver analisando o arquivo, e tente de novo, ou mova-o em vez disso. |
| The file was permanently deleted because it could not be sent to the Recycle Bin. | O arquivo foi excluído permanentemente porque não foi possível enviá-lo para a Lixeira. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Recusando mover arquivos para a pasta do Windows Installer (destino: {0}). |
| Destination must be a fully qualified path (relative paths resolve against the process current directory and are unsafe under elevation): {0} | O destino deve ser um caminho totalmente qualificado (caminhos relativos são resolvidos com base no diretório atual do processo e não são seguros sob elevação): {0} |
| Destination folder canonical path changed mid-batch: {0} | O caminho canônico da pasta de destino mudou durante a operação: {0} |
| Cannot write to {0}. | Não é possível gravar em {0}. |
| Could not find a unique filename for '{0}' after 10,000 attempts. | Não foi possível encontrar um nome de arquivo único para '{0}' após 10.000 tentativas. |

## Update check

| English | Português (Brasil) |
| --- | --- |
| Check for updates | Verificar atualizações |
| Checking... | Verificando... |
| Up to date. | Tudo atualizado. |
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
| The link couldn't be opened in your normal-user browser. The URL has been copied to your clipboard so you can open it manually:<br><br>{0} | Não foi possível abrir o link no navegador do seu usuário comum. A URL foi copiada para a área de transferência, para que você possa abri-la manualmente:<br><br>{0} |
| The link couldn't be opened in your normal-user browser, and copying it to the clipboard also failed. The URL is:<br><br>{0} | Não foi possível abrir o link no navegador do seu usuário comum, e copiá-la para a área de transferência também falhou. A URL é:<br><br>{0} |

## Sending the summary

| English | Português (Brasil) |
| --- | --- |
| Sending... | Enviando... |
| Thanks! Report sent. | Obrigado! Relatório enviado. |
| Sending failed. Try again later. | Falha no envio. Tente de novo mais tarde. |
| No report to send. | Nenhum relatório para enviar. |
| Send this to No Faff? | Enviar isto para o No Faff? |
| Nothing identifies you or your machine; it just lets me know InstallerClean's working and how much space people are freeing. It goes to nofaff.netlify.app/api/result-log. | Nada identifica você ou a sua máquina; só me diz que o InstallerClean está funcionando e quanto espaço as pessoas estão liberando. Vai para nofaff.netlify.app/api/result-log. |

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
| If it helped, buy me a cup of tea. | Se ajudou, me paga um café. |
| It's thirsty work! | É trabalho que dá sede! |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Cancelamento solicitado. O InstallerClean está esperando o passo atual chegar a um ponto em que possa parar. Isso pode levar alguns segundos durante operações intensas de E/S ou uma chamada ao banco de dados MSI. |
| Close | Fechar |
| Leave a star on GitHub, report an Issue or post in Discussions. Any feedback welcome. | Deixe uma estrela no GitHub, abra uma Issue ou escreva nas Discussions. Qualquer feedback é bem-vindo. |
| or report an Issue or post in Discussions. Any feedback welcome. | ou abra uma Issue ou escreva nas Discussions. Qualquer feedback é bem-vindo. |
| Minimise | Minimizar |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | Você decide, mas eu agradeço. Envia um resumo anônimo que só me diz se está funcionando e quanto espaço as pessoas estão liberando. A próxima tela mostra o que será enviado antes de você confirmar. |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | Você decide, mas eu agradeço. Envia um resumo anônimo que só me diz se está funcionando. A próxima tela mostra o que será enviado antes de você confirmar. |
| Move the unneeded files to the Move location. | Mover os arquivos desnecessários para o local de destino. |
| Move the unneeded files to the Move location. Choose one first. | Mover os arquivos desnecessários para o local de destino. Escolha um primeiro. |
| Send the unneeded files to the Recycle Bin. | Enviar os arquivos desnecessários para a Lixeira. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Nome do titular do certificado Authenticode incorporado. Cadeia não verificada. |
| Change language. The program will restart. | Alterar idioma. O programa será reiniciado. |

## Screen reader labels

| English | Português (Brasil) |
| --- | --- |
| Buy me a cup of tea | Me paga uma xícara de café |
| Buy me a cuppa (About window) | Me paga um café (janela Sobre) |
| Cancel operation | Cancelar a operação |
| Cancel scan | Cancelar a análise |
| Cancel startup scan | Cancelar a análise inicial |
| Close | Fechar |
| Close window | Fechar a janela |
| Close result and return to main window | Fechar o resultado e voltar para a janela principal |
| Leave a star on GitHub | Deixe uma estrela no GitHub |
| Leave a star on GitHub (About window) | Deixe uma estrela no GitHub (janela Sobre) |
| Minimise | Minimizar |
| Move all unneeded installer files to the chosen destination folder | Mover todos os arquivos de instalação desnecessários para a pasta de destino escolhida |
| Send all unneeded installer files to the Recycle Bin | Enviar para a Lixeira todos os arquivos de instalação desnecessários |
| Delete sends the unneeded files to the Recycle Bin. Cancel closes without deleting. | Excluir envia os arquivos desnecessários para a Lixeira. Cancelar fecha sem excluir. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Mover coloca os arquivos desnecessários na pasta de destino escolhida. Cancelar os deixa onde estão. |
| Choose how to handle the unneeded files: move them somewhere safe, delete them permanently or cancel. | Escolha o que fazer com os arquivos desnecessários: movê-los para um lugar seguro, excluí-los permanentemente ou cancelar. |
| Move the unneeded files to a folder you choose | Mover os arquivos desnecessários para uma pasta que você escolher |
| Delete the unneeded files permanently because the Recycle Bin is unavailable for this drive | Excluir permanentemente os arquivos desnecessários porque a Lixeira está indisponível para esta unidade |
| Posts to nofaff.netlify.app. Counts and labels only. You will see the exact payload before sending. | Envia para nofaff.netlify.app. Apenas contagens e rótulos. Você verá exatamente o que será enviado antes do envio. |
| Say thanks | Agradecer |
| Send posts the report shown to No Faff. Cancel sends nothing. | Enviar transmite ao No Faff o relatório exibido. Cancelar não envia nada. |
| Check for updates | Verificar atualizações |
| Checks the GitHub releases API over HTTPS for a newer version. | Consulta a API de versões do GitHub via HTTPS em busca de uma versão mais recente. |
| Open the release page to download the newer version, or cancel to keep the current version. | Abra a página da versão para baixar a versão mais recente, ou cancele para manter a versão atual. |
| MIT licence | Licença MIT |
| Opens the licence file on github.com in your browser. | Abre o arquivo da licença em github.com no seu navegador. |
| Move location | Local de destino |
| Products | Produtos |
| Patches | Patches |
| Product details | Detalhes do produto |
| Move destination folder | Pasta de destino |
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
| Result log preview | Visualização do relatório de resultados |
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
