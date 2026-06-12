<p align="center">
  <a href="README.md">English</a> · <a href="README.zh-CN.md">简体中文</a> · <a href="README.es.md">Español</a> · <a href="README.ja.md">日本語</a> · <strong>Português (BR)</strong> · <a href="README.ru.md">Русский</a> · <a href="README.fr.md">Français</a>
</p>

<p align="center"><em>Esta página está traduzida, mas a interface do aplicativo está atualmente apenas em inglês.</em></p>

<p align="center">
  <img src="docs/icon.png" width="280" alt="InstallerClean">
</p>

<p align="center"><em>🎶 What's my line? I'm happy <a href="https://www.youtube.com/watch?v=HM-jHhUZfFI">cleaning Windows</a></em></p>

<h1 align="center">InstallerClean</h1>

<p align="center"><strong>Uma alternativa de código aberto ao <a href="https://www.homedev.com.au/free/patchcleaner">PatchCleaner</a>. Limpe com segurança o <code>C:\Windows\Installer</code>, a pasta oculta do Windows que consome silenciosamente o seu espaço em disco.</strong></p>

<p align="center"><em>Use uma vez. Talvez libere um espaço. Pode jogar fora.</em></p>

<p align="center">
  <a href="LICENSE"><img src="https://img.shields.io/badge/licence-MIT-blue.svg" alt="Licença: MIT"></a>
  <a href="https://dotnet.microsoft.com/download/dotnet/10.0"><img src="https://img.shields.io/badge/.NET-10.0-purple.svg" alt=".NET 10"></a>
  <a href="https://github.com/no-faff/InstallerClean/actions/workflows/ci.yml"><img src="https://github.com/no-faff/InstallerClean/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases"><img src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4.svg" alt="Windows 10/11"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases/latest"><img src="https://img.shields.io/github/v/release/no-faff/InstallerClean" alt="Versão do GitHub"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases"><img src="https://img.shields.io/github/downloads/no-faff/InstallerClean/total?cacheSeconds=300" alt="Total de downloads"></a>
</p>

![Captura de tela do InstallerClean após uma limpeza bem-sucedida: 965 MB liberados, 68 arquivos excluídos](docs/screenshots/04d-deleted-freed-success.webp)

- **O que faz:** Encontra e remove arquivos desnecessários de `C:\Windows\Installer`, a pasta oculta que o Windows nunca limpa.
- **Quanto espaço:** Depende dos seus programas. Na minha máquina, deu pouco menos de 1 GB. Um usuário do InstallerClean [relatou](https://github.com/no-faff/InstallerClean/issues/12#issuecomment-4395580816) 25 GB. Com o Adobe Acrobat, pode passar de 100 GB. Pode ser que não dê nada. O importante é que é rápido e não custa nada: tudo o que puder ser removido vai embora.
- **É seguro:** Sim. Só remove os arquivos que o próprio Windows declara não precisar mais. Excluir manda os arquivos para a Lixeira e nunca exclui nada de forma permanente sem perguntar. Mover deixa você guardá-los em um lugar seguro.
- **Como obter:** [Baixe a versão mais recente](../../releases/latest), execute e pronto.

## Conteúdo

- [A pasta que ninguém te conta](#a-pasta-que-ninguém-te-conta)
- [A busca por ajuda](#a-busca-por-ajuda)
- [O que ele faz](#o-que-ele-faz)
- [Capturas de tela](#capturas-de-tela)
- [Como funciona](#como-funciona)
- [É seguro?](#é-seguro)
- [Se você estiver mesmo com um arquivo faltando em C:\Windows\Installer](#recovery)
- [Acessibilidade](#acessibilidade)
- [O que ele não faz](#o-que-ele-não-faz)
- [Perguntas frequentes](#perguntas-frequentes)
- [Download](#download)
- [Comparado ao PatchCleaner](#comparado-ao-patchcleaner)
- [Linha de comando](#linha-de-comando)
- [Requisitos](#requisitos)
- [Compilar a partir do código-fonte](#compilar-a-partir-do-código-fonte)
- [Contribuir](#contribuir)
- [Apoie o projeto](#apoie-o-projeto)
- [Histórico de estrelas](#histórico-de-estrelas)
- [Licença](#licença)

---

## A pasta que ninguém te conta

Existe uma pasta oculta em todo PC com Windows chamada `C:\Windows\Installer`. Toda vez que você instala um programa que usa o sistema Windows Installer, ou aplica um patch ao Microsoft Office, Adobe Acrobat, Visual Studio ou a qualquer outro aplicativo baseado em `.msi`, uma cópia desse instalador ou desse arquivo de patch `.msp` vai parar nessa pasta. E fica lá.

Quando você desinstala o programa, os arquivos ficam. Quando um patch mais novo substitui um antigo, os dois ficam. O Windows nunca os limpa. A Limpeza de Disco não toca neles. O DISM cuida de outra pasta, completamente diferente. Com os anos, a pasta cresce: 10 GB, 30 GB, 50 GB. Em máquinas com muito programa baseado em MSI (o Acrobat é um culpado frequente), ela pode [passar de 100 GB](https://www.reddit.com/r/sysadmin/comments/1oxcrmh/acrobat_filling_up_the_cwindowsinstaller_folder/).

Não são arquivos temporários que reaparecem assim que você fecha uma ferramenta de limpeza. São peso morto de verdade: instaladores antigos de programas que você desinstalou anos atrás e patches que já foram substituídos três vezes. Uma vez removidos, não voltam.

**Se você procura um jeito fácil de liberar espaço em disco no Windows, essa pasta é um dos melhores lugares para começar.** O InstallerClean encontra os arquivos desnecessários e os remove com segurança.

[PatchCleaner](https://www.homedev.com.au/free/patchcleaner) sempre foi a ferramenta de referência para isso, mas não recebe atualizações desde março de 2016 e tem código fechado. O InstallerClean é uma alternativa de código aberto, com detecção de patches substituídos (que pega os patches do Acrobat que o PatchCleaner exclui) e uma interface moderna.

## A busca por ajuda

Se você já procurou ajuda com essa pasta, sabe como é. Alguém pergunta como limpá-la. Mandam rodar a Limpeza de Disco. A pessoa tenta. Libera [600 MB de uma pasta de 180 GB](https://learn.microsoft.com/en-us/answers/questions/4238108/windows-installer-folder-has-occupied-180gb). E o tópico morre.

> *"Todos os tópicos que encontrei tendem a recomendar as mesmas coisas, que não resolvem o problema, e depois morrem."*
>
> ksparks519, r/Windows10 (traduzido do inglês)

Ou então mandam não mexer nela de jeito nenhum. Em um tópico, disseram a alguém com uma pasta Installer de 60 GB para [não mexer nisso](https://www.reddit.com/r/techsupport/comments/1hw4suq/my_windows_installer_folder_is_like_60gb_so_i/). Quando essa pessoa perguntou o que deveria fazer no lugar, a resposta foi: *"Acabei de te dizer."*

O conselho padrão confunde apagar arquivos a esmo (o que é de fato perigoso) com remover arquivos que o próprio Windows declara não precisar mais (o que não é). O InstallerClean faz a segunda coisa.

Se você já procurou ajuda com isso antes, provavelmente já encontrou o [PatchCleaner](https://www.homedev.com.au/free/patchcleaner) de [John Crawford](https://www.homedev.com.au/). É um aplicativo fantástico. Eu baixei e ele fez exatamente o que prometia: liberou um monte de espaço. A única coisa que ele não trata são os patches da Adobe; ele os exclui por padrão e, em máquinas onde a Adobe é a maior culpada, muitos arquivos removíveis acabam ficando para trás:

> *"Baixei o PatchCleaner para excluir os arquivos .msp órfãos... 29 GB dos arquivos estão 'excluídos por filtros', então o PatchCleaner não parece ajudar."*
>
> HeatherBunny1111, [r/techsupport](https://www.reddit.com/r/techsupport/comments/1qc4tcf/how_to_delete_msp_files_safely/) (traduzido do inglês)

O InstallerClean detecta quais patches foram substituídos por atualizações mais recentes e os marca como removíveis, incluindo os patches do Acrobat que o PatchCleaner exclui.

## O que ele faz

1. **Analisa** o `C:\Windows\Installer` em busca de arquivos `.msi` e `.msp`
2. **Consulta** a API do Windows Installer para descobrir quais arquivos ainda estão registrados
3. **Mostra** o que é necessário e o que não é, com os tamanhos
4. **Remove** os arquivos desnecessários: exclui para a Lixeira (se ela não estiver disponível para a unidade, o aplicativo pergunta antes de qualquer exclusão permanente) ou move para uma pasta que você escolher

Nenhuma atividade de rede automática. Dois botões opcionais fazem uma única chamada HTTPS quando clicados: **Verificar atualizações**, em Sobre, e **Enviar resumo**, na tela de conclusão. Veja [O que ele não faz](#o-que-ele-não-faz) mais abaixo para todos os detalhes.

## Capturas de tela

<p>
  <img src="docs/screenshots/01-initial-scan.webp" alt="Tela de abertura mostrando a análise em andamento, com 68 arquivos encontrados para limpar" width="900"><br>
  <em>Análise inicial. Muito rápida.</em>
</p>

<p>
  <img src="docs/screenshots/02-main-window.webp" alt="Janela principal mostrando 116 arquivos ainda em uso e 68 arquivos para limpar" width="900"><br>
  <em>Resultados: quanto está em uso, quanto é removível.</em>
</p>

<p>
  <img src="docs/screenshots/03a-details-registered.webp" alt="Janela de arquivos registrados listando os produtos instalados e seus metadados do banco de dados do instalador" width="900"><br>
  <em>Os arquivos ainda em uso, com os metadados lidos do banco de dados do instalador.</em>
</p>

<p>
  <img src="docs/screenshots/03b-details-unused.webp" alt="Janela de arquivos desnecessários listando os arquivos .msi removíveis com os motivos" width="900"><br>
  <em>Os arquivos que não são mais necessários.</em>
</p>

<p>
  <img src="docs/screenshots/04b-Delete-dialogue.webp" alt="Caixa de diálogo de confirmação de exclusão mostrando que 68 arquivos (965 MB) irão para a Lixeira" width="900"><br>
  <em>Confirmação antes de cada ação. Excluir envia para a Lixeira; Mover coloca os arquivos onde você quiser.</em>
</p>

<p>
  <img src="docs/screenshots/04d-deleted-freed-success.webp" alt="Sobreposição de sucesso mostrando 965 MB liberados após uma exclusão, com 68 arquivos enviados para a Lixeira" width="900"><br>
  <em>Depois de uma exclusão bem-sucedida.</em>
</p>

<p>
  <img src="docs/screenshots/06a-scanned-again-all-clean.webp" alt="Sobreposição de tudo limpo exibida quando não há mais nada para remover em uma nova análise" width="900"><br>
  <em>Depois de uma nova análise. Nada mais para limpar.</em>
</p>

## Como funciona

O InstallerClean identifica dois tipos de arquivos desnecessários.

**Arquivos órfãos** são instaladores e patches deixados para trás depois que você desinstala um programa. O Windows não os referencia mais, mas os arquivos continuam na pasta ocupando espaço.

**Patches substituídos** são patches `.msp` antigos que foram trocados por outros mais novos. O Windows os marca como substituídos no próprio banco de dados, mas nunca os exclui. Fornecedores que lançam patches com frequência (Acrobat, Office, ferramentas de desenvolvimento grandes) acumulam patches substituídos indefinidamente.

Para encontrá-los, o InstallerClean chama a interface COM do Windows Installer diretamente, via P/Invoke:

- `MsiEnumProductsEx` para enumerar cada produto instalado
- `MsiEnumPatchesEx` para encontrar todos os patches registrados de cada produto
- `MsiGetPatchInfoEx` para ler o estado de cada patch (aplicado, substituído ou obsoleto)

Qualquer arquivo `.msi` ou `.msp` em `C:\Windows\Installer` que não seja reivindicado por um produto registrado é órfão. Qualquer patch marcado como substituído e não necessário para a desinstalação é marcado como removível.

Se a API retornar dados incompletos (raro, mas pode acontecer com um estado do instalador corrompido), o aplicativo recorre à leitura do registro. Essa alternativa só adiciona arquivos ao conjunto "ainda necessários", nunca ao conjunto "removíveis".

Depois que um Mover ou Excluir é concluído, as subpastas vazias dentro de `C:\Windows\Installer` (os diretórios que o cache deixa para trás quando o conteúdo some) são removidas na mesma passagem. Os pontos de nova análise (reparse points) são ignorados durante essa limpeza, para que uma junção plantada dentro do cache não consiga redirecionar a limpeza para fora dele.

## É seguro?

Sim. O InstallerClean consulta o mesmo banco de dados que o próprio Windows usa para controlar o que está instalado. Se o Windows diz que um arquivo não é mais necessário, o aplicativo acredita; ele não fica adivinhando a partir de nomes de arquivo ou datas.

**No aplicativo.** Excluir envia os arquivos para a Lixeira. Se a Lixeira não estiver disponível para aquela unidade (foi desativada para a unidade, ou está cheia ou danificada), o InstallerClean não exclui os arquivos de vez em silêncio. Ele para e deixa você escolher: movê-los para um lugar seguro, excluí-los permanentemente ou cancelar. Os arquivos só são excluídos permanentemente se você escolher isso explicitamente. Mover é a opção ainda mais segura: coloca os arquivos em uma pasta que você escolher, para que você possa guardá-los até ter certeza de que nada deu errado. Nada é tocado até você confirmar. Se o Windows Installer estiver gravando no cache naquele momento, tiver uma transação anterior suspensa ou tiver um renomeamento pós-reinicialização na fila apontando para o cache, Mover e Excluir ficam desativados e o motivo específico é exibido. Os serviços de análise, consulta, movimentação, exclusão, configurações e reinicialização pendente são cobertos por uma suíte de testes automatizados que roda a cada commit (veja o selo de CI acima).

**Verificando o binário.** O InstallerClean não é assinado. Certificados de assinatura de código custam dinheiro todo ano, e eu prefiro manter o projeto gratuito, aberto e financiado por doações.

- Os hashes SHA-256 de cada versão estão listados na [página de versões](../../releases/latest).
- Links do VirusTotal para os builds setup, portable, slim e CLI são publicados a cada versão.
- O código-fonte está em [github.com/no-faff/InstallerClean](https://github.com/no-faff/InstallerClean), e a CI compila e testa cada commit (veja o selo verde de CI acima).
- O [MajorGeeks](https://www.majorgeeks.com/files/details/installerclean.html) testa cada envio em uma máquina virtual e só publica se passar na avaliação deles.
- A [Softpedia](https://www.softpedia.com/get/System/Hard-Disk-Utils/InstallerClean.shtml) testa cada versão em busca de vírus, spyware e adware.

<a href="https://www.softpedia.com/get/System/Hard-Disk-Utils/InstallerClean.shtml"><img src="docs/badges/softpedia-100-free2.webp" alt="Certificado 100% limpo pela Softpedia" width="190"></a>

VirusTotal: limpo em todos os mecanismos. Há links ativos nas notas de cada versão para você verificar de novo.

<a id="recovery"></a>
## Se você estiver mesmo com um arquivo faltando em `C:\Windows\Installer`

O InstallerClean só remove os arquivos que o Windows informa ter terminado de usar, então ele não tem como deixar um programa sem condição de ser reparado, atualizado ou desinstalado. Remover arquivos de `C:\Windows\Installer` na mão, ou com uma ferramenta que não consulta o banco de dados do instalador antes, é outra história, e é por isso que o conselho padrão é não mexer na pasta. Esse conselho costuma estar certo, mas não se você usa o InstallerClean. Aqui está o quadro completo, e o que fazer se um arquivo necessário já tiver sumido.

### Sobre o `C:\Windows\Installer` e como recuperar um arquivo perdido

*As citações da Microsoft abaixo são reproduzidas no original em inglês.*

`C:\Windows\Installer` é o cache do Windows Installer. Quando você instala um programa baseado em MSI ou aplica um patch, o Windows guarda aqui uma cópia do instalador e anota, para cada produto, o arquivo que espera encontrar mais tarde. Esses arquivos não são usados enquanto o programa roda; são usados quando o Windows o repara, atualiza ou desinstala. Apague um de que um programa ainda precisa e nada quebra na hora, e é justamente por isso que é fácil apagá-los sem consequência aparente e só ter problema meses depois. A Microsoft coloca assim:

> "If the installer cache is compromised, you may not immediately see problems until you take an action such as uninstalling, repairing, or updating a product."

A recuperação não é nada simples, e a Microsoft é franca quanto a isso:

> "If application files are missing from the Windows Installer Cache, ask the vendor or support team for the application about the missing files. You must follow the procedures or steps recommended by the application vendor to restore the files. In some cases, you may have to rebuild the operating system and reinstall the application to fix the problem."

> "Windows support engineers cannot help you recover missing application files from the Windows Installer cache."

E você também não pode pegar o arquivo emprestado de outra máquina:

> "Missing files cannot be copied between computers because the files are unique."

Na prática, a solução que costuma funcionar é baixar o instalador do programa afetado no site do fabricante e executá-lo por cima da sua instalação atual. Não desinstale antes: desinstalar é, em si, uma das etapas que precisam do arquivo que falta. Use a versão que você tem instalada no momento, se ainda conseguir obtê-la, porque o Windows pode rejeitar uma diferente:

> "The upgrade cannot be installed by the Windows Installer service because the program to be upgraded may be missing, or the upgrade may update a different version of the program."

Isso normalmente restaura o arquivo e deixa as suas configurações intactas, mas a Microsoft não garante, e o último recurso documentado dela é reinstalar o programa, ou reconstruir o Windows. Essa é a posição oficial, relatada exatamente como a encontro. Não fui eu que causei isso e não tenho como melhorar a própria orientação da Microsoft; só estou te dizendo como é.

Nada disso pode acontecer por causa do InstallerClean. Ele só remove os arquivos que o próprio Windows informa não serem mais necessários, de modo que o arquivo que um futuro reparo, atualização ou desinstalação for procurar nunca é um dos que ele tocou. A orientação da Microsoft está em [Restore missing Windows Installer cache files](https://learn.microsoft.com/en-us/troubleshoot/windows-client/application-management/missing-windows-installer-cache).

## Acessibilidade

O InstallerClean foi feito para ser totalmente utilizável pelo teclado e com leitor de tela.

- **Operável inteiramente pelo teclado.** O Tab alcança todos os controles, e as colunas das janelas de detalhes são ordenadas pelo teclado, então nada aqui precisa de mouse. O foco do teclado fica sempre visível onde quer que esteja.
- **Narrador e Acesso por Voz.** Todos os controles têm rótulo, e a palavra visível em um botão é a palavra que o aciona por voz. Quando um Mover ou Excluir termina, o resultado é lido em voz alta.
- **Feito para ser lido.** O texto atende ao contraste WCAG AA em todo o tema escuro.

Se algo aqui atrapalhar você, [abra uma issue](../../issues). Problemas de acessibilidade são bugs, não casos isolados.

## O que ele não faz

- O WinSxS (`C:\Windows\WinSxS`) é uma pasta diferente, com regras diferentes. Para essa, rode `Dism /Online /Cleanup-Image /StartComponentCleanup` em um prompt elevado.
- Sem serviço em segundo plano, sem tarefa agendada, sem limpeza automática. O aplicativo roda quando você o abre.
- O registro é acessado apenas para leitura. O aplicativo consulta o banco de dados do Windows Installer; não o modifica.
- Sem telemetria automática, sem rede em segundo plano. O aplicativo não faz nenhuma chamada de rede até você clicar em um dos dois botões. **Verificar atualizações**, em Sobre, consulta a API pública de versões do GitHub no momento do clique e diz se você está com a versão mais recente (um único GET HTTPS, string de identificação `InstallerClean/<version>`). **Enviar resumo**, na tela de conclusão, lê o `%LOCALAPPDATA%\NoFaff\InstallerClean\last-run.json` e o envia por POST HTTPS a um endpoint do No Faff, para que eu possa ver se a execução funcionou. O JSON contém apenas contadores e rótulos categóricos: nenhum caminho de arquivo, nenhum nome de usuário, nenhum identificador de máquina, nenhum horário. Clicar abre uma janela de confirmação mostrando o JSON exato que será enviado; revise ali e aperte Enviar para confirmar, ou Cancelar para desistir. Uma vez por máquina: depois de um envio bem-sucedido, o botão fica oculto para sempre; se a primeira tentativa falhar com um erro transitório, a próxima sessão pergunta de novo.
- Sem extras empacotados. Sem barras de ferramentas, sem ofertas de terceiros, sem upsells.
- A única permissão pedida além de abrir o programa é a de Administrador, necessária porque `C:\Windows\Installer` é restrito a administradores.

## Perguntas frequentes

**Vou realmente liberar vários GB de espaço?** Depende da sua máquina. Uma instalação limpa do Windows 11 sem programas extras não tem nada para remover. Uma estação de trabalho de desenvolvimento usada há muito tempo, ou qualquer máquina com muito programa baseado em MSI (Acrobat, Office, LibreOffice, ferramentas de desenvolvimento grandes), pode ter dezenas de GB. Rode `installerclean-cli /s` para ver exatamente o que seria removido antes de se comprometer.

<!-- reports-stats-start (generated by non-repo-files/refresh-reports-table.mjs; do not hand-edit between these markers) -->
Entre os 74 relatórios que as pessoas tiveram a gentileza de enviar (obrigado 🙏) desde que a v1.8.0 adicionou a opção:

| Resultado | Proporção | Menor | Mediana | Maior |
|---|---|---|---|---|
| Nada para remover | 65% | - | - | - |
| Espaço liberado | 35% | 0,2 GB | 22 GB | 327 GB |
<!-- reports-stats-end -->

<details>
<summary>Veja como é um relatório</summary>

```json
{
  "schemaVersion": 3,
  "app": { "version": "1.9.0" },
  "os": "Windows 11 (X64)",
  "scan": {
    "durationMs": 1820,
    "registeredCount": 148,
    "orphanedCount": 40,
    "supersededCount": 25,
    "obsoletedCount": 5,
    "missingFromDiskCount": 0,
    "pendingReboot": "clean"
  },
  "operation": {
    "kind": "delete",
    "outcome": "complete",
    "filesProcessed": 70,
    "filesFailed": 0,
    "bytesFreed": 22548578304,
    "errors": [],
    "moveDestinationKind": null
  }
}
```

Ele carrega apenas contadores e rótulos categóricos: nenhum caminho de arquivo, nenhum nome de usuário, nenhum identificador de máquina.

</details>

**Por que ele pede Administrador?** O `C:\Windows\Installer` pertence ao SYSTEM e é restrito apenas a administradores. Ler a pasta, escrever na API de consulta do banco de dados do Installer e mover ou excluir arquivos exigem elevação. Não há caminho em modo de usuário.

**Posso desfazer uma exclusão?** Em geral, sim. Quando a Lixeira está disponível para a unidade, Excluir manda os arquivos para lá e você pode restaurá-los pela Lixeira. Se a Lixeira não estiver disponível, o aplicativo nunca exclui de vez por conta própria (veja [É seguro?](#é-seguro)). Para uma rede de segurança que você controla, use Mover para colocar os arquivos em uma pasta que você escolher e confirme que nada quebrou antes de excluí-los de lá.

**O Windows vai reclamar se eu remover esses arquivos?** Não. O InstallerClean só remove os arquivos que o próprio Windows informa ter terminado de usar, então nada do que ele remove é necessário para reparar, atualizar ou desinstalar um programa. Se um arquivo necessário acabar sumindo de `C:\Windows\Installer` por algum outro meio, veja [Se você estiver mesmo com um arquivo faltando em C:\Windows\Installer](#recovery).

**Por que não usar `Win32_Product` (WMI)?** [O `Win32_Product` dispara operações de reparo do MSI em cada produto durante a enumeração](https://gregramsey.net/2012/02/20/win32_product-is-evil/), o que pode levar minutos e sobrecarregar o disco. O InstallerClean chama a API COM do Windows Installer diretamente, sem efeitos colaterais.

**Por que não simplesmente um script PowerShell?** Um script curto que chama `MsiEnumPatchesEx` já basta para *listar* os patches, mas as partes que sustentam o InstallerClean são justamente as que um script passa por cima: a classificação órfão x substituído, a alternativa via registro que só adiciona arquivos ao conjunto "ainda necessários" (nunca ao de "removíveis"), o bloqueio por reinicialização pendente, a rede de segurança do Mover-para-outro-lugar, o progresso por arquivo com cancelamento e o padrão de Lixeira-em-vez-de-exclusão-permanente. Os casos extremos em máquinas reais com muito MSI (registros corrompidos, junções dentro do cache, produtos em `HKU\.DEFAULT`, transações do Installer suspensas) são fáceis de tratar errado em um script improvisado. O `installerclean-cli` é a versão sem interface, caso o que você queira seja scripting.

**Funciona no Windows 7 ou 8?** Não testado e não suportado. O alvo é o Windows 10 e o 11.

**Serve para RMM / implantação em massa?** Sim. A CLI sai com códigos distintos por resultado (0 sucesso, 2 parcial, 1 falha total, 75 transitório, 130 Ctrl+C), então uma tarefa agendada pode tentar de novo no 75 sem confundi-lo com uma falha total. Ela grava um resumo de cada execução no log de eventos do Aplicativo e respeita o mesmo mutex de instância única que a interface gráfica. Veja a seção Linha de comando.

## Download

Quatro builds, escolha um:

- **Setup** (`InstallerClean-setup.exe`): um instalador comum do Windows com o runtime do .NET 10 embutido. Adiciona um atalho no menu Iniciar e desinstala sem deixar resíduos. Fica guardadinho nos Programas, fácil de achar daqui a seis meses.
- **Portable** (`InstallerClean-portable.exe`): um único exe autônomo com o runtime embutido. Sem instalação, sem desinstalador. Execute, use, apague. Execute de novo quando quiser.
- **Slim** (`InstallerClean-slim.exe`): o menor download. Exige que o [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) já esteja instalado (o que você tem se estiver com um Visual Studio atualizado).
- **CLI** (`installerclean-cli.exe`): a versão de linha de comando sozinha, um único exe autônomo. Sem instalação, sem deixar nada na máquina depois. Largue num cliente, rode uma análise ou uma limpeza, apague. Feito para scripting, tarefas agendadas e implantação em massa, quando você quer as operações sem um aplicativo de desktop no cliente. Veja [Linha de comando](#linha-de-comando) para os argumentos e códigos de saída.

Baixe na [página de versões](../../releases/latest) e execute. O Windows SmartScreen vai dizer "Editor desconhecido". Clique em **Mais informações** e depois em **Executar assim mesmo**. Isso é normal para software de código aberto sem assinatura.

O aplicativo analisa automaticamente ao iniciar. Veja os resultados e clique em **Excluir** ou **Mover**.

Ou instale pelo [Scoop](https://scoop.sh):

```
scoop bucket add no-faff https://github.com/no-faff/scoop-bucket
scoop install installerclean
```

## Comparado ao PatchCleaner

| | **InstallerClean** | **PatchCleaner** |
|---|---|---|
| Última atualização | 2026 (ativo) | 3 de março de 2016 |
| Código-fonte | Código aberto (MIT) | Fechado |
| Runtime | .NET 10 (autônomo) | .NET + VBScript |
| API | Windows Installer COM (em processo) | Windows Installer COM (fora do processo, via VBScript) |
| Detecção de patches substituídos | Sim | Não |
| Tratamento do Adobe | Detecta os patches substituídos | Exclui por padrão |
| Interface | Tema escuro (WPF) | Windows Forms |
| Coleta de dados | Nenhuma | Nenhuma |
| Segurança ao excluir | Lixeira, nunca uma exclusão permanente silenciosa | Permanente, sem Lixeira |

> **Uma observação sobre o `Win32_Product`:** A abordagem comum, mas problemática, para listar produtos instalados é o `Win32_Product` (WMI), que [dispara operações de reparo do MSI](https://gregramsey.net/2012/02/20/win32_product-is-evil/) em cada produto durante a enumeração. Tanto o InstallerClean quanto o PatchCleaner evitam isso. Os dois usam a interface COM do Windows Installer. O nome de arquivo `WMIProducts.vbs` no script do PatchCleaner é enganoso; o script usa COM do MSI, não WMI.

O [Ultra Virus Killer (UVK)](https://www.carifred.com/uvk/) também oferece limpeza do Installer como parte do seu módulo System Booster, mas é uma ferramenta paga (US$ 15-25) e a limpeza é um pequeno recurso dentro de um aplicativo bem maior. O InstallerClean é gratuito, focado e de código aberto.

Limpadores de sistema genéricos como o [CCleaner](https://www.ccleaner.com/) e o [BleachBit](https://www.bleachbit.org/) não tocam no `C:\Windows\Installer`. A pasta precisa de consultas à API do Windows Installer para distinguir os pacotes registrados dos desnecessários, e um limpador genérico que apenas percorresse a árvore de arquivos poderia quebrar aplicativos instalados. O InstallerClean é a ferramenta certa quando essa é exatamente a pasta que você quer limpar.

## Linha de comando

O InstallerClean oferece operação sem interface gráfica para uso em scripts e administração de sistemas:

```
Uso:
  installerclean-cli --help   Mostra esta ajuda (também aceita /?, -h)
  installerclean-cli --version  Mostra a versão (também aceita -v)
  installerclean-cli /s       Apenas análise, lista os arquivos removíveis
  installerclean-cli /d       Exclui os arquivos removíveis (Lixeira)
  installerclean-cli /m       Move para o local padrão salvo
  installerclean-cli /m PATH  Move para o caminho especificado
```

Para abrir a interface gráfica, execute `InstallerClean.exe` (ou use o atalho do menu Iniciar, se você instalou pelo setup).

Executado sem argumento, ou com uma opção não reconhecida, o `installerclean-cli` mostra esta ajuda e sai com o código `1`, para que uma tarefa agendada que perca a opção falhe de forma visível em vez de ter sucesso silencioso sem fazer nada. Um `--help`, `/?` ou `-h` explícito mostra a mesma ajuda e sai com o código `0`.

`/s` é uma simulação: analisa, lista o que seria removido com nomes e tamanhos, e sai. Útil para auditar antes de limpar. O código de saída é `0` se a análise for bem-sucedida, `1` se ela falhar e `130` em caso de Ctrl+C. Todos os arquivos estão em `C:\Windows\Installer`.

`/d` e `/m` analisam e depois agem. `/d` envia os arquivos removíveis para a Lixeira. `/m` os move para uma pasta (ou a que você especificar na linha de comando, ou a padrão salva pela interface gráfica). Códigos de saída: `0` sucesso total, `2` parcial (alguns arquivos deram certo, outros falharam), `1` falha total (a análise falhou, argumentos inválidos ou todos os arquivos do lote falharam), `75` uma condição transitória bloqueou a execução (a mensagem exibida explica qual e se tentar de novo vai ajudar), `130` Ctrl+C.

Toda a saída da CLI, incluindo as mensagens de erro e de diagnóstico, vai para o stdout; não há um fluxo stderr separado. O código de saída é o sinal legível por máquina (e a entrada no log de eventos do Aplicativo de cada execução o reflete), então um script deve se basear no código de saída em vez de analisar o texto, e `installerclean-cli /s > audit.txt` captura a execução inteira, incluindo qualquer linha de erro.

Os três exigem um prompt de comando elevado (administrador). Se a Diretiva de Grupo bloquear o prompt de elevação do UAC, o processo se recusa a iniciar e o Windows retorna o erro 740 para o shell pai (`$LASTEXITCODE = 740` no PowerShell). `taskkill /pid <pid>` não dispara um cancelamento gracioso; o mutex de instância única é recuperado na próxima execução pelo caminho do AbandonedMutexException.

Observação: a saída da própria CLI está em inglês. As descrições acima correspondem às opções disponíveis.

### Por que `installerclean-cli` e não `installerclean.exe`?

O `InstallerClean.exe` é a interface gráfica WPF; ela não responde a argumentos de linha de comando. O `installerclean-cli.exe` é um executável de console separado, entregue no mesmo diretório de instalação, que expõe as mesmas operações de análise / movimentação / exclusão para o PowerShell, o cmd e tarefas agendadas. Como é um processo de console de verdade, ele bloqueia o prompt até terminar; redirecione ou canalize a saída dele como faria com qualquer outro exe de console.

Os downloads portable e slim contêm apenas o exe da interface gráfica. Se você quer a linha de comando sem a interface, baixe o `installerclean-cli.exe` na [página de versões](../../releases/latest) e execute-o diretamente. O setup também o instala ao lado da interface gráfica.

## Requisitos

- Windows 10 ou 11
- Privilégios de administrador (`C:\Windows\Installer` é restrito a administradores)

Veja [Download](#download) para as opções setup, portable, slim e CLI.

## Compilar a partir do código-fonte

```
git clone https://github.com/no-faff/InstallerClean.git
cd InstallerClean
dotnet build src/InstallerClean/InstallerClean.csproj
```

Rodar os testes:

```
dotnet test src/InstallerClean.Tests/
```

## Contribuir

Encontrou um bug ou tem uma sugestão? [Abra uma issue](../../issues) ou comece uma [discussão](../../discussions). Pull requests são bem-vindas. Por favor, rode `dotnet test` antes de enviar.

## Apoie o projeto

Se o InstallerClean te ajudou, considere [apoiar o No Faff](https://nofaff.netlify.app/support) ou deixar uma estrela no GitHub.

## Histórico de estrelas

<a href="https://www.star-history.com/?repos=no-faff%2FInstallerClean&type=date&legend=top-left">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/chart?repos=no-faff/InstallerClean&type=date&theme=dark&legend=top-left" />
   <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/chart?repos=no-faff/InstallerClean&type=date&legend=top-left" />
   <img alt="Gráfico do histórico de estrelas" src="https://api.star-history.com/chart?repos=no-faff/InstallerClean&type=date&legend=top-left" />
 </picture>
</a>

## Licença

[MIT](LICENSE)

---

🎶 [George Formby - When I'm Cleaning Windows](https://www.youtube.com/watch?v=sfmAeijj5cM). Aproveite!
