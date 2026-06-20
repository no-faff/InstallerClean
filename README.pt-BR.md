<p align="center">
  <a href="README.md">English</a> · <a href="README.zh-CN.md">简体中文</a> · <a href="README.es.md">Español</a> · <a href="README.ja.md">日本語</a> · <strong>Português (BR)</strong> · <a href="README.ru.md">Русский</a> · <a href="README.fr.md">Français</a> · <a href="README.it.md">Italiano</a>
</p>

<p align="center"><em>Esta página está traduzida, mas a interface do aplicativo está atualmente apenas em inglês.</em></p>

<p align="center">
  <img src="docs/icon.png" width="280" alt="InstallerClean">
</p>

<p align="center"><em>🎶 What's my line? I'm happy <a href="https://www.youtube.com/watch?v=HM-jHhUZfFI">cleaning Windows</a></em></p>

<h1 align="center">InstallerClean</h1>

<p align="center"><strong>Uma ferramenta de código aberto para limpar com segurança o <code>C:\Windows\Installer</code>, a pasta oculta do Windows que consome silenciosamente o seu espaço em disco.</strong></p>

<p align="center"><em>Use uma vez. Talvez libere um espaço. Pode jogar fora.</em></p>

<p align="center">
  <a href="LICENSE"><img src="https://img.shields.io/badge/licence-MIT-blue.svg" alt="Licença: MIT"></a>
  <a href="https://dotnet.microsoft.com/download/dotnet/10.0"><img src="https://img.shields.io/badge/.NET-10.0-purple.svg" alt=".NET 10"></a>
  <a href="https://github.com/no-faff/InstallerClean/actions/workflows/ci.yml"><img src="https://github.com/no-faff/InstallerClean/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases"><img src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4.svg" alt="Windows 10/11"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases/latest"><img src="https://img.shields.io/badge/release-v1.9.1-blue" alt="Versão do GitHub"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases"><img src="https://img.shields.io/badge/downloads-22k-brightgreen" alt="Total de downloads"></a>
</p>

![Captura de tela do InstallerClean após uma limpeza bem-sucedida: 1,28 GB liberados, 69 arquivos enviados para a Lixeira](docs/screenshots/06-freed-success-done.webp)

- **O que faz:** O InstallerClean faz uma coisa só: remove arquivos desnecessários de `C:\Windows\Installer`, uma pasta oculta que o Windows nunca limpa. Depois de uma análise quase instantânea, ele te diz se você tem algum, mostra mais detalhes para os curiosos e deixa você excluí-los para liberar espaço no disco C:. Você usa uma vez e segue em frente.
- **Quanto espaço:** Os relatórios (opcionais) enviados até agora mostram que <!-- reports-freedpct-start -->41%<!-- reports-freedpct-end --> das máquinas tinham arquivos desnecessários para limpar. Dessas, a mediana liberada é <!-- reports-median-start -->22 GB<!-- reports-median-end -->. Algumas liberaram centenas de GB. Para mim, foram 1,28 GB. As outras <!-- reports-nothingpct-start -->59%<!-- reports-nothingpct-end --> não acharam nada para remover, o que só significa que a pasta Installer delas já estava limpa. Mais detalhes nas [Perguntas frequentes](#perguntas-frequentes) abaixo.
- **É seguro:** Sim. Ele pergunta à própria API do Windows Installer quais arquivos ainda são necessários e só lista aqueles que o Windows informa ter terminado de usar. É de código aberto (MIT) e não pergunta nada sobre você: sem conta, sem anúncios, sem rastreamento, sem telemetria, nada rodando em segundo plano. Ele nunca se conecta à internet sozinho.
- **Como obter:** [Baixe a versão mais recente](../../releases/latest). Execute; passe [pelo aviso do Windows](#unknown-publisher) e [pelo prompt de administrador](#admin). Exclua os arquivos desnecessários. Pronto.

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

Existe uma pasta oculta em todo PC com Windows chamada `C:\Windows\Installer`. Toda vez que você instala um programa que usa o sistema Windows Installer, ou aplica um patch ao Microsoft Office, Adobe Acrobat, Visual Studio ou a qualquer outro aplicativo baseado em `.msi`, uma cópia desse instalador ou desse arquivo de patch `.msp` vai parar nessa pasta, e fica lá.

Quando você desinstala o programa, os arquivos ficam. Quando um patch mais novo substitui um antigo, os dois ficam. O Windows nunca os limpa. A Limpeza de Disco não toca neles. O DISM cuida de outra pasta, completamente diferente. Com o tempo, a pasta cresce: 1 GB, 5 GB, 20 GB, 50 GB. Em máquinas com muito programa baseado em MSI (o Acrobat é um culpado frequente), ela pode [passar de 100 GB](https://www.reddit.com/r/sysadmin/comments/1oxcrmh/acrobat_filling_up_the_cwindowsinstaller_folder/).

Não são arquivos temporários que voltam sozinhos. São peso morto de verdade: instaladores antigos de programas que você desinstalou anos atrás e patches que já foram substituídos várias vezes. Uma vez removidos, não voltam.

**Se você procura um jeito fácil de liberar espaço em disco no Windows, essa pasta é um bom lugar para começar.** O InstallerClean encontra os arquivos desnecessários e os remove com segurança.

## A busca por ajuda

Se você já procurou ajuda com essa pasta, provavelmente sabe como é. Alguém com 180 GB em `C:\Windows\Installer` pergunta como limpá-la. [Mandam rodar a Limpeza de Disco](https://learn.microsoft.com/en-us/answers/questions/4238108/windows-installer-folder-has-occupied-180gb). A pessoa tenta. Ela libera 600 MB, nenhum deles dessa pasta (porque a Limpeza de Disco não toca em `C:\Windows\Installer`). E o tópico morre.

> *"Todos os tópicos que encontrei tendem a recomendar as mesmas coisas, que não resolvem o problema, e depois morrem."*
>
> [ksparks519, r/Windows10](https://www.reddit.com/r/Windows10/comments/1bt8c5p/anyone_ever_figure_out_giant_installer_folders/) (traduzido do inglês)

Ou então mandam não mexer nela de jeito nenhum. Em um tópico, disseram a alguém com uma pasta Installer de 60 GB para [não mexer nisso](https://www.reddit.com/r/techsupport/comments/1hw4suq/my_windows_installer_folder_is_like_60gb_so_i/). Quando essa pessoa perguntou o que deveria fazer no lugar, a resposta foi: *"Acabei de te dizer."*

O conselho padrão confunde apagar arquivos a esmo (o que é de fato perigoso) com remover arquivos que o próprio Windows declara não precisar mais (o que não é). O InstallerClean faz a segunda coisa.

## O que ele faz

1. **Analisa** o `C:\Windows\Installer` em busca de arquivos `.msi` e `.msp`
2. **Consulta** a API do Windows Installer para descobrir quais arquivos ainda estão registrados
3. **Mostra** quanto você pode liberar e quanto ainda é necessário, com janelas de detalhes opcionais que listam cada arquivo
4. **Remove** os arquivos desnecessários: exclui para a Lixeira ou move para uma pasta que você escolher

## Capturas de tela

<p>
  <img src="docs/screenshots/01-initial-scan.webp" alt="Tela de abertura com o logo do InstallerClean enquanto a análise é executada" width="900"><br>
  <em>Análise inicial. Muito rápida.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/02-main-window.webp" alt="Janela principal mostrando 120 arquivos ainda necessários (2,83 GB) e 69 arquivos desnecessários para limpar (1,28 GB), com uma caixa de local de destino e os botões Excluir e Mover" width="900"><br>
  <em>Resultados: quanto ainda é necessário, quanto é removível.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/03-details-registered.webp" alt="Janela de arquivos registrados listando os produtos instalados, com os detalhes do banco de dados do instalador para o produto selecionado" width="900"><br>
  <em>Detalhes dos arquivos ainda necessários, com os metadados lidos do banco de dados do instalador.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/04-details-safe-to-delete.webp" alt="Janela de arquivos desnecessários listando os arquivos .msi removíveis ordenados por tamanho, com o motivo de cada um ser removível e os detalhes do arquivo selecionado" width="900"><br>
  <em>Detalhes dos arquivos que não são mais necessários.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/05-delete-dialogue.webp" alt="Confirmação de exclusão perguntando se deve excluir 69 arquivos (1,28 GB), avisando que os arquivos serão enviados para a Lixeira" width="900"><br>
  <em>Confirmação antes de cada ação. Excluir envia para a Lixeira; Mover coloca os arquivos onde você quiser.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/06-freed-success-done.webp" alt="Sobreposição de sucesso mostrando 1,28 GB liberados, com 69 arquivos enviados para a Lixeira" width="900"><br>
  <em>Depois de uma exclusão bem-sucedida.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/07-scanned-again-all-clean.webp" alt="Sobreposição de tudo limpo após uma nova análise: nada para limpar em C:\Windows\Installer" width="900"><br>
  <em>Depois de uma nova análise. Nada mais para limpar.</em>
  <br><br>
</p>

## Como funciona

O InstallerClean identifica três tipos de arquivos desnecessários.

**Arquivos órfãos** são os instaladores `.msi` (e quaisquer patches `.msp`) deixados para trás depois que você desinstala um programa. O Windows não os referencia mais, mas os arquivos continuam na pasta ocupando espaço.

**Patches substituídos** são patches `.msp` antigos que foram trocados por outros mais novos. O Windows os marca como substituídos no próprio banco de dados, mas nunca os exclui. Fornecedores que lançam patches com frequência (Acrobat, Office, ferramentas de desenvolvimento grandes) acumulam patches substituídos indefinidamente.

**Patches obsoletos** são patches `.msp` que o fabricante retirou ou descontinuou em vez de substituir por uma versão mais nova. O Windows registra esse estado também e, da mesma forma, deixa o arquivo na pasta.

Para encontrá-los, o InstallerClean chama a interface COM do Windows Installer diretamente, via P/Invoke:

- `MsiEnumProductsEx` para enumerar cada produto instalado
- `MsiEnumPatchesEx` para encontrar todos os patches registrados de cada produto
- `MsiGetPatchInfoEx` para ler o estado de cada patch (aplicado, substituído ou obsoleto)

Qualquer arquivo `.msi` ou `.msp` em `C:\Windows\Installer` que não seja reivindicado por um produto registrado é órfão e marcado como removível. O mesmo vale para qualquer patch que o banco de dados marque como substituído ou obsoleto e que não seja necessário para a desinstalação.

Se a API retornar dados incompletos (raro, mas pode acontecer com um estado do instalador corrompido), o aplicativo recorre à leitura do registro. Essa alternativa só adiciona arquivos ao conjunto "ainda necessários", nunca ao conjunto "removíveis".

Depois que um Mover ou Excluir é concluído, as subpastas vazias dentro de `C:\Windows\Installer` (os diretórios que o cache deixa para trás quando o conteúdo some) são removidas na mesma passagem.

## É seguro?

Sim. O InstallerClean consulta o mesmo banco de dados da API do Windows Installer que o próprio Windows usa para controlar o que está instalado. Se o Windows diz que um arquivo não é mais necessário, o aplicativo acredita; ele não fica adivinhando a partir de nomes de arquivo ou datas.

**Sobre Excluir e Mover.** Os arquivos que o InstallerClean exclui podem ser excluídos permanentemente sem risco. **Excluir** envia os arquivos para a Lixeira (você será avisado se ela não estiver disponível); você recupera o espaço no seu disco C: quando esvazia a Lixeira.

Ainda assim, você não precisa acreditar na minha palavra de que os arquivos podem ser excluídos sem risco. Enquanto eles estão na Lixeira, você tem a chance de verificar se os aplicativos que usam essa pasta, Office, Acrobat, Visual Studio e afins, continuam atualizando e desinstalando sem problemas. Se algo quebrar (não vai!), restaure os arquivos pela Lixeira para resolver. Para ter ainda mais segurança, você pode usar **Mover** em vez disso, para deixar os arquivos em uma pasta que você escolher (obviamente, escolha uma pasta em outra partição ou unidade se o que você quer é liberar espaço em C:). Basta copiar os arquivos de volta para `C:\Windows\Installer` para deixar tudo como estava (mas você não vai precisar!).

Se o Windows Installer estiver gravando no cache naquele momento, tiver uma transação anterior suspensa ou tiver um renomeamento pós-reinicialização na fila apontando para o cache, Mover e Excluir ficam desativados e o motivo específico é exibido.

Os serviços de análise, consulta, movimentação, exclusão, configurações e reinicialização pendente são cobertos por uma suíte de testes automatizados que roda a cada commit (veja o selo de CI acima).

**Verificando o binário.** O InstallerClean não é assinado, então você não precisa confiar de olhos fechados:

- Os hashes SHA-256 de cada versão estão listados na [página de versões](../../releases/latest).
- VirusTotal: limpo em todos os mecanismos. Há links ativos nas notas de cada versão para você verificar de novo.
- O código-fonte está em [github.com/no-faff/InstallerClean](https://github.com/no-faff/InstallerClean), e a CI compila e testa cada commit (veja o selo verde de CI acima).
- <!-- downloads-start -->22k<!-- downloads-end --> downloads entre o GitHub, o MajorGeeks e a Softpedia.
- O [MajorGeeks](https://www.majorgeeks.com/files/details/installerclean.html) testa cada envio em uma máquina virtual e só publica se passar na avaliação deles.
- A [Softpedia](https://www.softpedia.com/get/System/Hard-Disk-Utils/InstallerClean.shtml) testa cada versão em busca de vírus, spyware e adware.

<a href="https://www.softpedia.com/get/System/Hard-Disk-Utils/InstallerClean.shtml"><img src="docs/badges/softpedia-100-free2.webp" alt="Certificado 100% limpo pela Softpedia" width="190"></a>

<a id="recovery"></a>
## Se você estiver mesmo com um arquivo faltando em `C:\Windows\Installer`

O InstallerClean só remove arquivos que o próprio Windows informa não serem mais necessários, então ele nunca pode ser o motivo de um arquivo estar faltando. Mas se um já tiver sumido, o InstallerClean detecta e sinaliza. Veja como resolver.

Baixe o instalador desse programa no site do fabricante e execute-o por cima da sua instalação atual; não desinstale antes. Use a versão que você tem agora, se possível, porque o Windows pode recusar uma diferente. Isso normalmente recoloca o arquivo e deixa as suas configurações intactas. Analise de novo no InstallerClean e o aviso terá sumido, se tiver funcionado.

Isso normalmente funciona. O que vem a seguir é o relato mais completo da própria Microsoft: os detalhes oficiais e os casos mais difíceis, para quando não for tão simples. Nada disso é causado pelo InstallerClean, e eu não tenho como melhorar a orientação da Microsoft, então só estou repassando.

<details>
<summary>A posição mais completa da Microsoft</summary>

*As citações da Microsoft a seguir estão no original em inglês.*

Orientação completa: [Restore missing Windows Installer cache files](https://learn.microsoft.com/en-us/troubleshoot/windows-client/application-management/missing-windows-installer-cache).

*Pode não aparecer de imediato:*
> "If the installer cache is compromised, you may not immediately see problems until you take an action such as uninstalling, repairing, or updating a product."

*Os arquivos são únicos por máquina, então você não pode copiar um de outro PC:*
> "Missing files cannot be copied between computers because the files are unique."

*Você também não consegue restaurar só o arquivo de um backup:*
> "To restore the missing files, a full system state restoration is required. It is not possible to replace only the missing files from a previous backup."

*A recuperação recomendada, e os seus limites diretos:*
> "If application files are missing from the Windows Installer Cache, ask the vendor or support team for the application about the missing files. You must follow the procedures or steps recommended by the application vendor to restore the files. In some cases, you may have to rebuild the operating system and reinstall the application to fix the problem."
>
> "Windows support engineers cannot help you recover missing application files from the Windows Installer cache."

*Por que a mesma versão importa:*
> "The upgrade cannot be installed by the Windows Installer service because the program to be upgraded may be missing, or the upgrade may update a different version of the program."

</details>

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
- Ele só se conecta à internet quando você manda: uma verificação manual de atualizações; o resumo anônimo opcional (só para eu saber que está funcionando); e links para a documentação no GitHub e para uma página de doação, que abrem no seu navegador se você optar por clicar.
- Sem barras de ferramentas, sem software empacotado, sem adware.

## Perguntas frequentes

**Vou realmente liberar vários GB de espaço?** Depende da sua máquina. Uma instalação limpa do Windows 11 sem programas extras não tem nada para remover. Uma estação de trabalho de desenvolvimento usada há muito tempo, ou qualquer máquina com muito programa baseado em MSI (Acrobat, Office, LibreOffice, ferramentas de desenvolvimento grandes), pode ter dezenas de GB. De um jeito ou de outro, você vê exatamente quanto no momento em que executa.

<!-- reports-stats-start (generated by non-repo-files/refresh-reports-table.mjs; do not hand-edit between these markers) -->
Entre os 83 relatórios que as pessoas tiveram a gentileza de enviar (obrigado 🙏) desde que a v1.8.0 adicionou a opção:

| Resultado | Proporção | Menor | Mediana | Maior |
|---|---|---|---|---|
| Nada para remover | 59% | - | - | - |
| Espaço liberado | 41% | 0,2 GB | 22 GB | 327 GB |
<!-- reports-stats-end -->

<details>
<summary>Esses relatórios vêm do botão opcional "Enviar resumo". Veja o que você verá antes de qualquer coisa ser enviada.</summary>

![Caixa de diálogo de confirmação intitulada "Enviar isto para o No Faff?" mostrando o relatório completo que seria enviado: versão do aplicativo, versão do Windows, contagens da análise, arquivos processados e bytes liberados, sem nenhum caminho de arquivo, nome ou ID de máquina, e uma observação de que nada identifica você ou a sua máquina, apenas se o aplicativo funcionou e quanto espaço foi liberado, com os botões Cancelar e Enviar.](docs/screenshots/optional-send-summary-confirmation-dialogue.webp)

</details>

<a id="admin"></a>

**Por que ele pede Administrador?** O `C:\Windows\Installer` é restrito a administradores. Ler a pasta, consultar o banco de dados do Installer e mover ou excluir arquivos exigem isso, então o aplicativo precisa rodar como administrador.

<a id="unknown-publisher"></a>

**Por que o Windows diz "Editor desconhecido"?** Porque o InstallerClean não tem assinatura de código. Um certificado de assinatura custa dinheiro todo ano, e eu prefiro manter o aplicativo gratuito a pagar por um. Então, quando você o executa, o Windows SmartScreen mostra "O Windows protegeu o seu PC". Clique em **Mais informações** e depois em **Executar assim mesmo**. Pode fazer sem medo: o código-fonte é público, e cada versão tem links do VirusTotal e hashes SHA-256 que você pode conferir antes.

**Posso desfazer uma exclusão?** Em geral, sim. Quando a Lixeira está disponível para a unidade, Excluir manda os arquivos para lá e você pode restaurá-los pela Lixeira. Se a Lixeira não estiver disponível, o aplicativo nunca exclui de vez por conta própria (veja [É seguro?](#é-seguro)). E se você preferir ter uma volta sob o seu controle, Mover coloca os arquivos em uma pasta que você escolher; exclua de lá quando estiver satisfeito.

**O Windows vai reclamar se eu remover esses arquivos?** Não. O InstallerClean só remove os arquivos que o próprio Windows informa ter terminado de usar, então nada do que ele remove é necessário para reparar, atualizar ou desinstalar um programa. Se um arquivo necessário acabar sumindo de `C:\Windows\Installer` por algum outro meio, veja [Se você estiver mesmo com um arquivo faltando em C:\Windows\Installer](#recovery).

**Por que não usar `Win32_Product` (WMI)?** [O `Win32_Product` dispara operações de reparo do MSI em cada produto durante a enumeração](https://gregramsey.net/2012/02/20/win32_product-is-evil/), o que pode levar minutos e sobrecarregar o disco. O InstallerClean chama a API COM do Windows Installer diretamente, sem efeitos colaterais.

**Por que não simplesmente um script PowerShell?** Um script curto que chama `MsiEnumPatchesEx` já basta para *listar* os patches, mas as partes que sustentam o InstallerClean são justamente as que um script passa por cima: a classificação órfão x substituído, a alternativa via registro que só adiciona arquivos ao conjunto "ainda necessários" (nunca ao de "removíveis"), o bloqueio por reinicialização pendente, a rede de segurança do Mover-para-outro-lugar, o progresso por arquivo com cancelamento e o padrão de Lixeira-em-vez-de-exclusão-permanente. Os casos extremos em máquinas reais com muito MSI (registros corrompidos, junções dentro do cache, produtos em `HKU\.DEFAULT`, transações do Installer suspensas) são fáceis de tratar errado em um script improvisado. O `installerclean-cli` é a versão sem interface, caso o que você queira seja scripting.

**Funciona no Windows 7 ou 8?** Não testado e não suportado. O alvo é o Windows 10 e o 11.

**Serve para RMM / implantação em massa?** Sim. A CLI sai com códigos distintos por resultado (0 sucesso, 2 parcial, 1 falha total, 75 transitório, 130 para um Ctrl+C antes de qualquer arquivo ser processado; um Ctrl+C no meio do lote sai com 2, já que houve trabalho concluído), então uma tarefa agendada pode tentar de novo no 75 sem confundi-lo com uma falha total. Ela grava um resumo de cada execução no log de eventos do Aplicativo e respeita o mesmo mutex de instância única que a interface gráfica. O setup também instala de forma silenciosa com as opções padrão do Inno Setup (`/SILENT` ou `/VERYSILENT`); a execução pós-instalação é pulada em instalações silenciosas. Veja a seção Linha de comando.

## Download

Três builds, escolha um:

- **Setup** (`InstallerClean-setup.exe`): um instalador comum do Windows com o runtime do .NET 10 embutido. Adiciona um atalho no menu Iniciar e desinstala sem deixar resíduos. Fica guardadinho nos Programas, fácil de achar daqui a seis meses.
- **Portable** (`InstallerClean-portable.exe`): um único exe autônomo com o runtime embutido. Sem instalação, sem desinstalador. Execute, use, apague. Execute de novo quando quiser.
- **CLI** (`installerclean-cli.exe`): a versão de linha de comando sozinha, um único exe autônomo. Sem instalação, sem deixar nada na máquina depois. Largue num cliente, rode uma análise ou uma limpeza, apague. Feito para scripting, tarefas agendadas e implantação em massa, quando você quer as operações sem um aplicativo de desktop no cliente. Veja [Linha de comando](#linha-de-comando) para os argumentos e códigos de saída.

Baixe na [página de versões](../../releases/latest) e execute. Ele não é assinado, então o Windows mostra um aviso de "editor desconhecido"; as [Perguntas frequentes](#unknown-publisher) explicam o que você vai ver e por que é seguro.

O aplicativo analisa automaticamente ao iniciar. Veja os resultados e clique em **Excluir** ou **Mover**.

Ou instale pelo [winget](https://learn.microsoft.com/windows/package-manager/winget/):

```
winget install NoFaff.InstallerClean
```

Ou instale pelo [Scoop](https://scoop.sh):

```
scoop bucket add no-faff https://github.com/no-faff/scoop-bucket
scoop install installerclean
```

## Comparado ao PatchCleaner

Se você já procurou por essa pasta antes, a ferramenta que você provavelmente encontrou é o [PatchCleaner](https://www.homedev.com.au/free/patchcleaner). Ele continua firme, mas eu fiz o InstallerClean porque o PatchCleaner tem código fechado, não recebe atualização desde março de 2016 e, por padrão, não mexe em produtos Adobe. A verificação de órfãos dele sinalizava os patches da Adobe por engano, e removê-los quebrava as atualizações da Adobe, então ele deixa todos os arquivos da Adobe em paz, a menos que você desligue o filtro. Nas máquinas onde a Adobe é a maior responsável, isso é a maior parte do espaço:

> *"Baixei o PatchCleaner para excluir os arquivos .msp órfãos, mas aparentemente isso só liberaria 250 MB de espaço. 29 GB dos arquivos estão 'excluídos por filtros', então o PatchCleaner não parece ajudar."*
>
> HeatherBunny1111, [r/techsupport](https://www.reddit.com/r/techsupport/comments/1qc4tcf/how_to_delete_msp_files_safely/) (traduzido do inglês)

O InstallerClean lê os próprios registros de patch do Windows Installer, então consegue identificar quais patches da Adobe estão de fato substituídos e limpar esses com segurança, sem nenhum filtro geral. Veja como os dois se comparam:

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
| Segurança ao excluir | Lixeira. Se ela não estiver disponível, ele pergunta: mover ou excluir permanentemente | Permanente, sem Lixeira |

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

`/d` e `/m` analisam e depois agem. `/d` envia os arquivos removíveis para a Lixeira. `/m` os move para uma pasta (ou a que você especificar na linha de comando, ou a padrão salva pela interface gráfica). Códigos de saída: `0` sucesso total, `2` parcial (alguns arquivos deram certo, outros falharam), `1` falha total (a análise falhou, argumentos inválidos ou todos os arquivos do lote falharam), `75` uma condição transitória bloqueou a execução (a mensagem exibida explica qual e se tentar de novo vai ajudar), `130` para um Ctrl+C antes de qualquer arquivo ser processado (um Ctrl+C no meio do lote sai com `2`, parcial, já que houve trabalho concluído).

Toda a saída da CLI, incluindo as mensagens de erro e de diagnóstico, vai para o stdout; não há um fluxo stderr separado. O código de saída é o sinal legível por máquina (e a entrada no log de eventos do Aplicativo de cada execução o reflete), então um script deve se basear no código de saída em vez de analisar o texto, e `installerclean-cli /s > audit.txt` captura a execução inteira, incluindo qualquer linha de erro.

Os três exigem um prompt de comando elevado (administrador). Se a Diretiva de Grupo bloquear o prompt de elevação do UAC, o processo se recusa a iniciar e o Windows retorna o erro 740 para o shell pai (`$LASTEXITCODE = 740` no PowerShell). `taskkill /pid <pid>` não dispara um cancelamento gracioso; o mutex de instância única é recuperado na próxima execução pelo caminho do AbandonedMutexException.

Observação: a saída da própria CLI está em inglês. As descrições acima correspondem às opções disponíveis.

### Por que `installerclean-cli` e não `installerclean.exe`?

O `InstallerClean.exe` é a interface gráfica WPF; ela não responde a argumentos de linha de comando. O `installerclean-cli.exe` é um executável de console separado, entregue no mesmo diretório de instalação, que expõe as mesmas operações de análise / movimentação / exclusão para o PowerShell, o cmd e tarefas agendadas. Como é um processo de console de verdade, ele bloqueia o prompt até terminar; redirecione ou canalize a saída dele como faria com qualquer outro exe de console.

O download portable contém apenas o exe da interface gráfica. Se você quer a linha de comando sem a interface, baixe o `installerclean-cli.exe` na [página de versões](../../releases/latest) e execute-o diretamente. O setup também o instala ao lado da interface gráfica.

## Requisitos

- Windows 10 (versão 1607 / build 14393 ou posterior, a mais antiga compatível com o runtime do .NET 10) ou Windows 11
- Privilégios de administrador (`C:\Windows\Installer` é restrito a administradores)

Veja [Download](#download) para as opções setup, portable e CLI.

## Compilar a partir do código-fonte

```
git clone https://github.com/no-faff/InstallerClean.git
cd InstallerClean
dotnet build src/InstallerClean.sln
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
