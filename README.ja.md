<p align="center">
  <a href="README.md">English</a> · <a href="README.zh-CN.md">简体中文</a> · <a href="README.es.md">Español</a> · <strong>日本語</strong> · <a href="README.pt-BR.md">Português (BR)</a> · <a href="README.ru.md">Русский</a> · <a href="README.fr.md">Français</a>
</p>

<p align="center"><em>このページは翻訳版ですが、アプリの画面表示は現在のところ英語のみです。</em></p>

<p align="center">
  <img src="docs/icon.png" width="280" alt="InstallerClean">
</p>

<p align="center"><em>🎶 What's my line? I'm happy <a href="https://www.youtube.com/watch?v=HM-jHhUZfFI">cleaning Windows</a></em></p>

<h1 align="center">InstallerClean</h1>

<p align="center"><strong><a href="https://www.homedev.com.au/free/patchcleaner">PatchCleaner</a> に代わる、オープンソースのツールです。気づかないうちにディスク容量を食いつぶしていく Windows の隠しフォルダー <code>C:\Windows\Installer</code> を、安全にクリーンアップできます。</strong></p>

<p align="center"><em>一度使う。少し容量が空くかも。あとは捨てる。</em></p>

<p align="center">
  <a href="LICENSE"><img src="https://img.shields.io/badge/licence-MIT-blue.svg" alt="ライセンス: MIT"></a>
  <a href="https://dotnet.microsoft.com/download/dotnet/10.0"><img src="https://img.shields.io/badge/.NET-10.0-purple.svg" alt=".NET 10"></a>
  <a href="https://github.com/no-faff/InstallerClean/actions/workflows/ci.yml"><img src="https://github.com/no-faff/InstallerClean/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases"><img src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4.svg" alt="Windows 10/11"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases/latest"><img src="https://img.shields.io/github/v/release/no-faff/InstallerClean" alt="GitHub リリース"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases"><img src="https://img.shields.io/github/downloads/no-faff/InstallerClean/total?cacheSeconds=300" alt="総ダウンロード数"></a>
</p>

![クリーンアップ成功後の InstallerClean の画面：965 MB を解放し、68 個のファイルを削除](docs/screenshots/04d-deleted-freed-success.webp)

- **概要：** `C:\Windows\Installer`、つまり Windows が一切クリーンアップしない隠しフォルダーから、不要なファイルを見つけて削除します。
- **どれくらい空くか：** お使いのソフトウェアによります。私の環境では 1 GB 弱でした。ある InstallerClean ユーザーは 25 GB だったと[報告しています](https://github.com/no-faff/InstallerClean/issues/12#issuecomment-4395580816)。Adobe Acrobat が入っていると 100 GB を超えることもあります。まったくのゼロかもしれません。要は、手早く無料で試せて、消せるものはすべて消える、ということです。
- **安全性：** はい。Windows 自身が「もう不要」と判断したファイルだけを削除します。「削除」を選ぶとファイルはごみ箱に送られ、確認なしに完全削除することは一切ありません。「移動」を使えば、安全な場所にファイルを残しておけます。
- **入手方法：** [最新リリースをダウンロード](../../releases/latest)して、実行すれば完了です。

## 目次

- [誰も教えてくれないフォルダー](#誰も教えてくれないフォルダー)
- [助けを求めて](#助けを求めて)
- [何をするのか](#何をするのか)
- [スクリーンショット](#スクリーンショット)
- [仕組み](#仕組み)
- [安全ですか？](#安全ですか)
- [万一 C:\Windows\Installer のファイルが失われてしまったら](#recovery)
- [アクセシビリティ](#アクセシビリティ)
- [このアプリがしないこと](#このアプリがしないこと)
- [よくある質問](#よくある質問)
- [ダウンロード](#ダウンロード)
- [PatchCleaner との比較](#patchcleaner-との比較)
- [コマンドライン](#コマンドライン)
- [動作要件](#動作要件)
- [ソースからのビルド](#ソースからのビルド)
- [貢献](#貢献)
- [プロジェクトを応援する](#プロジェクトを応援する)
- [スター履歴](#スター履歴)
- [ライセンス](#ライセンス)

---

## 誰も教えてくれないフォルダー

どの Windows PC にも `C:\Windows\Installer` という隠しフォルダーがあります。Windows インストーラーの仕組みを使うソフトウェアをインストールしたり、Microsoft Office、Adobe Acrobat、Visual Studio などの `.msi` ベースのアプリケーションにパッチを適用したりするたびに、そのインストーラーや `.msp` パッチファイルのコピーがこのフォルダーに入ります。そして、そのまま残り続けます。

ソフトウェアをアンインストールしても、ファイルは残ります。新しいパッチが古いパッチを置き換えても、両方とも残ります。Windows がそれらをクリーンアップすることはありません。ディスク クリーンアップも手をつけません。DISM はまったく別のフォルダー用のツールです。年月とともに、フォルダーは膨らんでいきます。10 GB、30 GB、50 GB と。MSI を多用するソフトウェアが入った環境（Acrobat が代表的な原因です）では、[100 GB を超える](https://www.reddit.com/r/sysadmin/comments/1oxcrmh/acrobat_filling_up_the_cwindowsinstaller_folder/)こともあります。

これらは、クリーンアップツールを閉じた途端にまた作り直されるような一時ファイルではありません。正真正銘のお荷物です。何年も前にアンインストールしたソフトの古いインストーラーや、すでに 3 回も置き換えられたパッチなどです。一度消してしまえば、戻ってくることはありません。

**Windows でディスク容量を手軽に空けたいなら、このフォルダーは最初に手をつけるべき場所のひとつです。** InstallerClean は不要なファイルを見つけ出し、安全に削除します。

この用途では [PatchCleaner](https://www.homedev.com.au/free/patchcleaner) が長らく定番のツールでしたが、2016 年 3 月以降は更新されておらず、ソースも非公開です。InstallerClean はそのオープンソースの代替で、置き換えられたパッチの検出（PatchCleaner が除外してしまう Acrobat のパッチも見つけ出します）と、モダンな UI を備えています。

## 助けを求めて

このフォルダーについて一度でも調べたことがあるなら、お決まりの流れをご存じでしょう。誰かが掃除の仕方を尋ね、ディスク クリーンアップを使うよう言われ、試してみる。すると [180 GB のフォルダーのうち 600 MB しか解放されない](https://learn.microsoft.com/en-us/answers/questions/4238108/windows-installer-folder-has-occupied-180gb)。そしてスレッドは静かになります。

> *「見つかるスレッドはどれも、問題を解決しない同じ対処法ばかりを勧めてきて、そのまま書き込みが途絶えてしまうんです。」*
>
> ksparks519, r/Windows10（英語原文からの翻訳）

あるいは、まったく触るなと言われることもあります。あるスレッドでは、60 GB の Installer フォルダーを抱えた人が[「いじるな」](https://www.reddit.com/r/techsupport/comments/1hw4suq/my_windows_installer_folder_is_like_60gb_so_i/)と言われました。では代わりにどうすればいいのかと尋ねると、返ってきた答えは「さっき言っただろう」でした。

世間で言われる定番のアドバイスは、ファイルを手当たり次第に削除すること（これは本当に危険です）と、Windows 自身がもう不要だと言っているファイルを削除すること（こちらは危険ではありません）を混同しています。InstallerClean が行うのは後者です。

以前にこの件で調べたことがあるなら、おそらく [John Crawford](https://www.homedev.com.au/) 氏の [PatchCleaner](https://www.homedev.com.au/free/patchcleaner) はすでに見つけているはずです。素晴らしいアプリです。私もダウンロードしてみましたが、うたい文句どおり、大量の空き容量を確保してくれました。唯一手が届かないのが Adobe のパッチです。デフォルトで除外される設定になっているため、Adobe が最大の元凶になっている環境では、削除できるはずのファイルが大量に取り残されてしまいます。

> *「孤立した .msp ファイルを削除しようと PatchCleaner をダウンロードしたのですが……ファイルのうち 29 GB が『フィルターで除外』されてしまっていて、PatchCleaner では役に立たないようです。」*
>
> HeatherBunny1111, [r/techsupport](https://www.reddit.com/r/techsupport/comments/1qc4tcf/how_to_delete_msp_files_safely/)（英語原文からの翻訳）

InstallerClean は、より新しい更新によって置き換えられたパッチを検出し、削除可能なものとして示します。PatchCleaner が除外する Acrobat のパッチも対象です。

## 何をするのか

1. `C:\Windows\Installer` 内の `.msi` と `.msp` ファイルを**スキャン**します。
2. Windows インストーラー API に**問い合わせ**て、どのファイルがまだ登録されているかを確認します。
3. 必要なファイルと不要なファイルを、サイズとともに**表示**します。
4. 不要なファイルを**削除**します。ごみ箱へ送るか（ドライブでごみ箱が使えない場合は、完全削除の前に必ず確認します）、選んだフォルダーへ移動します。

自動的なネットワーク通信は一切ありません。ユーザーが任意で押す 2 つのボタンだけが、クリックしたときに HTTPS 通信を 1 回だけ行います。「バージョン情報」画面の**更新を確認**ボタンと、完了画面の**概要を送信**ボタンです。詳しくは下記の[このアプリがしないこと](#このアプリがしないこと)をご覧ください。

## スクリーンショット

<p>
  <img src="docs/screenshots/01-initial-scan.webp" alt="スキャン中のスプラッシュ画面。クリーンアップ対象として 68 個のファイルが見つかっている" width="900"><br>
  <em>最初のスキャン。とても高速です。</em>
</p>

<p>
  <img src="docs/screenshots/02-main-window.webp" alt="116 個のファイルが使用中、68 個がクリーンアップ対象であることを示すメイン画面" width="900"><br>
  <em>結果の画面。どれだけ使用中で、どれだけ削除できるかがわかります。</em>
</p>

<p>
  <img src="docs/screenshots/03a-details-registered.webp" alt="インストール済み製品と、そのインストーラーデータベースのメタデータを一覧表示する登録済みファイルのウィンドウ" width="900"><br>
  <em>まだ使用中のファイル。インストーラーデータベースから読み取ったメタデータ付きです。</em>
</p>

<p>
  <img src="docs/screenshots/03b-details-unused.webp" alt="削除可能な .msi ファイルとその理由を一覧表示する不要なファイルのウィンドウ" width="900"><br>
  <em>もう必要のないファイル。</em>
</p>

<p>
  <img src="docs/screenshots/04b-Delete-dialogue.webp" alt="68 個のファイル（965 MB）がごみ箱に送られることを示す削除確認ダイアログ" width="900"><br>
  <em>どちらの操作の前にも確認があります。「削除」はごみ箱へ送り、「移動」はファイルをお好みの場所に置きます。</em>
</p>

<p>
  <img src="docs/screenshots/04d-deleted-freed-success.webp" alt="削除操作後に 965 MB を解放し、68 個のファイルをごみ箱へ送ったことを示す成功画面" width="900"><br>
  <em>削除が成功した後の画面。</em>
</p>

<p>
  <img src="docs/screenshots/06a-scanned-again-all-clean.webp" alt="再スキャンで削除対象がなかったときに表示される「すべてクリーン」の画面" width="900"><br>
  <em>再スキャン後。もうクリーンアップするものはありません。</em>
</p>

## 仕組み

InstallerClean は、不要なファイルを 2 種類に分けて見つけ出します。

**孤立したファイル**とは、ソフトウェアをアンインストールした後に取り残されたインストーラーやパッチです。Windows はもう参照していませんが、ファイルはフォルダーに居座って容量を占有し続けます。

**置き換えられたパッチ**とは、より新しいパッチに取って代わられた古い `.msp` パッチです。Windows は自身のデータベースで「置き換え済み」と記録しますが、削除はしません。頻繁にパッチを配布するベンダー（Acrobat、Office、大規模な開発ツールなど）では、置き換えられたパッチが際限なくたまっていきます。

これらを見つけるために、InstallerClean は P/Invoke を介して Windows インストーラーの COM インターフェイスを直接呼び出します。

- `MsiEnumProductsEx`：インストール済みのすべての製品を列挙します
- `MsiEnumPatchesEx`：各製品に登録されているすべてのパッチを見つけます
- `MsiGetPatchInfoEx`：パッチの状態（適用済み、置き換え済み、または廃止済み）を読み取ります

`C:\Windows\Installer` 内の `.msi` または `.msp` ファイルのうち、登録済みのどの製品からも要求されていないものが孤立したファイルです。「置き換え済み」と記録され、かつアンインストールに不要なパッチは、削除可能なものとして示されます。

API が不完全なデータを返した場合（まれですが、インストーラーの状態が壊れていると起こり得ます）、アプリはレジストリの読み取りにフォールバックします。このフォールバックは「まだ必要」の側にだけファイルを追加し、「削除可能」の側に加えることは決してありません。

「移動」または「削除」が完了すると、`C:\Windows\Installer` 内の空になったサブフォルダー（中身がなくなった後にキャッシュが残す空ディレクトリ）も、同じ処理の中で削除されます。この整理の際、再解析ポイント（reparse point）はスキップされます。これにより、キャッシュ内に仕掛けられたジャンクションが、クリーンアップをキャッシュの外へ向けてしまうのを防ぎます。

## 安全ですか？

はい。InstallerClean は、Windows 自身が「何がインストールされているか」を管理するのに使っているのと同じデータベースに問い合わせます。Windows がもう不要だと言うファイルなら、アプリはそれを信頼します。ファイル名や日付から推測することはありません。

**アプリの中での動作。** 「削除」を選ぶと、ファイルはごみ箱に送られます。そのドライブでごみ箱が使えない場合（ドライブで無効にされている、いっぱいになっている、または壊れている場合）でも、InstallerClean が黙ってファイルを完全に消すことはありません。いったん処理を止めて、選択肢を示します。安全な場所へ移動する、完全に削除する、またはキャンセルする、のいずれかです。ファイルが完全に削除されるのは、あなたが明示的にそれを選んだときだけです。「移動」はさらに安全な選択肢です。選んだフォルダーにファイルを置くので、何も問題が起きていないと確認できるまで、手元に残しておけます。確認するまで、何も手を付けません。Windows インストーラーが今まさにキャッシュへ書き込んでいる、前回のトランザクションが中断されている、またはキャッシュを対象とする再起動後のリネームが予約されている場合、「移動」と「削除」は無効になり、具体的な理由が表示されます。スキャン、クエリ、移動、削除、設定、再起動保留チェックの各サービスは、コミットのたびに実行される自動テストでカバーされています（上の CI バッジを参照）。

**バイナリの検証について。** InstallerClean には署名がありません。コード署名証明書は毎年費用がかかるため、このプロジェクトは無料・オープン・寄付で支える形のままにしておきたいのです。

- 各リリースの SHA-256 ハッシュは[リリースページ](../../releases/latest)に掲載しています。
- setup、portable、slim、CLI の各ビルドの VirusTotal リンクは、リリースごとに公開しています。
- ソースコードは [github.com/no-faff/InstallerClean](https://github.com/no-faff/InstallerClean) にあり、CI が毎回のコミットをビルド・テストしています（上の緑色の CI バッジを参照）。
- [MajorGeeks](https://www.majorgeeks.com/files/details/installerclean.html) は各提出物を仮想マシンでテストし、審査を通過したものだけを掲載します。
- [Softpedia](https://www.softpedia.com/get/System/Hard-Disk-Utils/InstallerClean.shtml) は各リリースをウイルス・スパイウェア・アドウェアについて検査しています。

<a href="https://www.softpedia.com/get/System/Hard-Disk-Utils/InstallerClean.shtml"><img src="docs/badges/softpedia-100-free2.webp" alt="Softpedia 認証済み 100% クリーン" width="190"></a>

VirusTotal：すべてのエンジンでクリーンです。各リリースのノートにライブリンクを掲載しているので、ご自身で再確認できます。

<a id="recovery"></a>
## 万一 `C:\Windows\Installer` のファイルが失われてしまったら

InstallerClean が消すのは、Windows が「もう用済み」と報告したファイルだけです。ですから、プログラムを修復・更新・アンインストールできなくしてしまうことはありません。`C:\Windows\Installer` から手作業で、あるいはインストーラーデータベースを先に確認しないツールでファイルを削除するのは、まったく別の話です。これこそが、「このフォルダーには触れるな」という定番のアドバイスの理由です。そのアドバイスはたいてい正しいのですが、InstallerClean を使うなら当てはまりません。ここでは、より詳しい全体像と、必要なファイルがすでに失われてしまった場合の対処法を説明します。

### `C:\Windows\Installer` について、そして失われたファイルの復旧方法

*以下の Microsoft の引用は、英語の原文のまま掲載しています。*

`C:\Windows\Installer` は Windows インストーラーのキャッシュです。MSI ベースのプログラムをインストールしたり、パッチを適用したりすると、Windows はここにインストーラーのコピーを保管し、製品ごとに「後で必要になるはずのファイル」を記録します。これらのファイルは、プログラムの実行中には使われません。Windows がそのプログラムを修復・更新・アンインストールするときに使われます。プログラムがまだ必要としているファイルを削除しても、すぐには何も壊れません。だからこそ、削除しても一見問題なく済んでしまい、何か月も経ってから初めてトラブルに見舞われる、ということが起こりやすいのです。Microsoft は次のように述べています。

> "If the installer cache is compromised, you may not immediately see problems until you take an action such as uninstalling, repairing, or updating a product."

復旧は簡単ではなく、Microsoft もそれを率直に認めています。

> "If application files are missing from the Windows Installer Cache, ask the vendor or support team for the application about the missing files. You must follow the procedures or steps recommended by the application vendor to restore the files. In some cases, you may have to rebuild the operating system and reinstall the application to fix the problem."

> "Windows support engineers cannot help you recover missing application files from the Windows Installer cache."

別のマシンからファイルを借りてくることもできません。

> "Missing files cannot be copied between computers because the files are unique."

実際のところ、たいていうまくいく対処法は、問題のプログラムのインストーラーを提供元からダウンロードし、既存のインストールに上書きする形で実行することです。先にアンインストールしてはいけません。アンインストール自体が、失われたファイルを必要とする操作のひとつだからです。今インストールされているのと同じバージョンを、まだ入手できるなら使ってください。Windows は別のバージョンを受け付けないことがあるからです。

> "The upgrade cannot be installed by the Windows Installer service because the program to be upgraded may be missing, or the upgrade may update a different version of the program."

これで通常はファイルが復元され、設定もそのまま残ります。ただし Microsoft が保証しているわけではなく、ドキュメントに記された最終手段は、プログラムの再インストール、あるいは Windows の再構築です。これが公式の見解で、私が見つけたとおりにお伝えしています。これは私が引き起こしたものではありませんし、Microsoft 自身の案内より良いものを示すこともできません。ただ、事実をお伝えしているだけです。

こうしたことが InstallerClean のせいで起こることはありません。InstallerClean が削除するのは、Windows 自身がもう不要だと報告したファイルだけなので、将来の修復・更新・アンインストールが探しに行くファイルが、InstallerClean の触れたものであることは決してありません。Microsoft の案内は [Restore missing Windows Installer cache files](https://learn.microsoft.com/en-us/troubleshoot/windows-client/application-management/missing-windows-installer-cache) にあります。

## アクセシビリティ

InstallerClean は、キーボードだけでも、スクリーンリーダーと併用しても、完全に操作できるように作られています。

- **すべてキーボードで操作可能。** Tab キーですべてのコントロールに到達でき、詳細ウィンドウの列もキーボードで並べ替えられます。ここではマウスを必要とする操作はありません。キーボードフォーカスは、どこにあっても常に見えるようになっています。
- **ナレーターと音声アクセス。** すべてのコントロールにラベルが付いており、ボタンに表示されている言葉が、そのまま音声で操作するときの言葉になります。「移動」や「削除」が完了すると、その結果が読み上げられます。
- **読みやすさを重視。** ダークテーマ全体を通じて、テキストは WCAG AA のコントラスト基準を満たしています。

もし使いづらい点があれば、[Issue を立ててください](../../issues)。アクセシビリティの問題は、些細な例外ではなくバグだと考えています。

## このアプリがしないこと

- WinSxS（`C:\Windows\WinSxS`）は、別のルールが適用される別のフォルダーです。そちらには、管理者権限のプロンプトで `Dism /Online /Cleanup-Image /StartComponentCleanup` を実行してください。
- バックグラウンドサービスも、スケジュールされたタスクも、自動クリーンアップもありません。アプリは、あなたが起動したときにだけ動きます。
- レジストリは読み取り専用です。アプリは Windows インストーラーのデータベースに問い合わせるだけで、変更はしません。
- 自動的なテレメトリも、バックグラウンドのネットワーク通信もありません。アプリは、2 つのボタンのいずれかをクリックするまで、ネットワーク通信を一切行いません。「バージョン情報」画面の**更新を確認**ボタンは、クリックすると GitHub の公開リリース API に問い合わせ、最新版かどうかを知らせます（HTTPS GET を 1 回、識別文字列は `InstallerClean/<version>`）。完了画面の**概要を送信**ボタンは、`%LOCALAPPDATA%\NoFaff\InstallerClean\last-run.json` を読み取り、それを HTTPS POST で No Faff のエンドポイントへ送信します。これは、実行がうまくいったかどうかを私が把握するためです。この JSON に含まれるのは件数と分類ラベルだけで、ファイルパス、ユーザー名、マシンの識別子、時刻などは一切含まれません。クリックすると、送信しようとしている JSON の中身をそのまま表示する確認ウィンドウが開きます。そこで内容を確認し、「送信」を押せば確定、「キャンセル」を押せば取りやめです。1 台につき 1 回だけ：送信に成功するとボタンは以後ずっと非表示になります。最初の試行が一時的なエラーで失敗した場合は、次回のセッションで再び確認します。
- 余計なおまけは同梱しません。ツールバーも、サードパーティの広告も、有料版へのしつこい勧誘もありません。
- 起動以外に求める権限は管理者権限だけです。これは `C:\Windows\Installer` が管理者専用だからです。

## よくある質問

**本当に数 GB も空きますか？** お使いのマシン次第です。追加ソフトを入れていないまっさらな Windows 11 では、削除するものは何もありません。長く使い込んだ開発用ワークステーションや、MSI ベースのソフトを多く入れたマシン（Acrobat、Office、LibreOffice、大規模な開発ツールなど）では、数十 GB になることもあります。実際に削除する前に何が削除対象になるかを確認するには、`installerclean-cli /s` を実行してください。

<!-- reports-stats-start (generated by non-repo-files/refresh-reports-table.mjs; do not hand-edit between these markers) -->
v1.8.0 でこのオプションが追加されて以来、ご厚意で送っていただいた 68 件のレポート（ありがとうございます🙏）を集計すると、次のようになりました。

| 結果 | 割合 | 最小 | 中央値 | 最大 |
|---|---|---|---|---|
| 削除対象なし | 68% | - | - | - |
| 容量を解放 | 32% | 0.2 GB | 21 GB | 327 GB |
<!-- reports-stats-end -->

<details>
<summary>レポートはこんな感じです</summary>

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

含まれるのは件数と分類ラベルだけです。ファイルパス、ユーザー名、マシンの識別子は含まれません。

</details>

**なぜ管理者権限が必要なのですか？** `C:\Windows\Installer` は SYSTEM が所有しており、管理者だけがアクセスできるよう厳しく制限されています。フォルダーの読み取り、インストーラーデータベースに問い合わせる API の呼び出し、ファイルの移動や削除は、いずれも権限の昇格を必要とします。ユーザーモードでの経路は存在しません。

**削除を取り消せますか？** たいていは可能です。ドライブでごみ箱が使える場合、「削除」はファイルをごみ箱へ送るので、そこから復元できます。ごみ箱が使えない場合でも、アプリが自分の判断でファイルを完全に消すことはありません（[安全ですか？](#安全ですか)を参照）。自分で管理できる安全網がほしいときは、「移動」を使ってファイルを好きなフォルダーに置き、問題が起きないことを確認してから、そこで削除してください。

**これらのファイルを削除すると Windows が文句を言いませんか？** 言いません。InstallerClean が削除するのは Windows 自身が「用済み」と報告したファイルだけなので、削除するものの中に、プログラムの修復・更新・アンインストールに必要なものは含まれません。もし別の手段で必要なファイルが `C:\Windows\Installer` から消えてしまった場合は、[万一 C:\Windows\Installer のファイルが失われてしまったら](#recovery)をご覧ください。

**なぜ `Win32_Product`（WMI）を使わないのですか？** [`Win32_Product` は列挙の途中で製品ごとに MSI の修復処理を引き起こし](https://gregramsey.net/2012/02/20/win32_product-is-evil/)、数分かかったり、ディスクに大きな負荷をかけたりすることがあります。InstallerClean は Windows インストーラーの COM API を直接呼び出すので、副作用がありません。

**いっそ PowerShell スクリプトでよいのでは？** `MsiEnumPatchesEx` を呼び出す短いスクリプトでも、パッチを*一覧表示する*だけなら十分です。しかし InstallerClean の屋台骨となっているのは、スクリプトが見落としがちな部分です。孤立か置き換え済みかの分類、「まだ必要」の側にだけファイルを追加する（決して「削除可能」には加えない）レジストリフォールバック、再起動保留時のブロック、別の場所へ「移動」する安全網、キャンセル可能なファイルごとの進捗表示、そして完全削除ではなくごみ箱を既定とする動作です。MSI を本当に多用しているマシンでの特殊なケース（壊れた登録情報、キャッシュ内のジャンクション、`HKU\.DEFAULT` 内の製品、中断された Installer トランザクションなど）は、その場限りのスクリプトでは扱いを誤りやすいものです。スクリプトでの利用が目的なら、`installerclean-cli` がその無人実行向けの顔になります。

**Windows 7 や 8 で動きますか？** テストしておらず、サポート対象外です。対象は Windows 10 と 11 です。

**RMM や大規模展開に向いていますか？** はい。CLI は結果に応じて異なる終了コードを返します（0 = 成功、2 = 部分的成功、1 = 重大な失敗、75 = 一時的な状態、130 = Ctrl+C）。そのため、スケジュールされたタスクは 75 のときだけ再試行でき、重大な失敗と混同せずに済みます。実行ごとの概要をアプリケーションイベントログに書き込み、GUI と同じ単一インスタンスのミューテックスを尊重します。詳しくはコマンドラインのセクションをご覧ください。

## ダウンロード

4 つのビルドがあります。お好みのものを選んでください。

- **Setup**（`InstallerClean-setup.exe`）：.NET 10 ランタイムを同梱した、ごく普通の Windows インストーラーです。スタートメニューに項目を追加し、きれいにアンインストールできます。「プログラム」の一覧に収まるので、半年後でも見つけやすいはずです。
- **Portable**（`InstallerClean-portable.exe`）：ランタイムを同梱した、単一の自己完結型 exe です。インストール不要、アンインストーラーもありません。実行して、使って、消すだけ。必要になればいつでもまた実行できます。
- **Slim**（`InstallerClean-slim.exe`）：最も小さいダウンロードです。[.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) が事前にインストールされている必要があります（最新の Visual Studio が入っていれば、すでに入っています）。
- **CLI**（`installerclean-cli.exe`）：コマンドライン版を単体にしたもので、単一の自己完結型 exe です。インストール不要で、使用後はマシンに何も残りません。クライアント機に置いて、スキャンやクリーンアップを実行し、消すだけ。スクリプト、スケジュールされたタスク、大規模展開のために作られており、クライアントにデスクトップアプリを置かずに操作だけを行いたい場合に向いています。引数と終了コードについては[コマンドライン](#コマンドライン)を参照してください。

[リリースページ](../../releases/latest)からダウンロードして、実行してください。Windows SmartScreen が「不明な発行元」と表示します。**詳細情報**をクリックし、**実行**をクリックしてください。署名のないオープンソースソフトウェアでは、これは正常な動作です。

アプリは起動時に自動でスキャンします。結果を確認したら、**削除**または**移動**をクリックしてください。

または [Scoop](https://scoop.sh) でインストールできます。

```
scoop bucket add no-faff https://github.com/no-faff/scoop-bucket
scoop install installerclean
```

## PatchCleaner との比較

| | **InstallerClean** | **PatchCleaner** |
|---|---|---|
| 最終更新 | 2026 年（活発に更新中） | 2016 年 3 月 3 日 |
| ソースコード | オープンソース（MIT） | クローズドソース |
| ランタイム | .NET 10（自己完結型） | .NET + VBScript |
| API | Windows インストーラー COM（プロセス内） | Windows インストーラー COM（VBScript 経由のプロセス外） |
| 置き換えられたパッチの検出 | あり | なし |
| Adobe への対応 | 置き換えられたパッチを検出 | 既定で除外 |
| UI | ダークテーマ（WPF） | Windows Forms |
| データ収集 | なし | なし |
| 削除時の安全性 | ごみ箱へ送り、黙って完全削除することはない | 完全削除、ごみ箱なし |

> **`Win32_Product` についての補足：** インストール済み製品を一覧表示する方法として広く使われているものの、問題があるのが `Win32_Product`（WMI）です。これは列挙の途中で[製品ごとに MSI の修復処理を引き起こします](https://gregramsey.net/2012/02/20/win32_product-is-evil/)。InstallerClean も PatchCleaner も、これを避けています。どちらも Windows インストーラーの COM インターフェイスを使います。PatchCleaner のスクリプトにある `WMIProducts.vbs` というファイル名は紛らわしいのですが、このスクリプトが使っているのは WMI ではなく MSI COM です。

[Ultra Virus Killer (UVK)](https://www.carifred.com/uvk/) も、System Booster モジュールの一部として Installer のクリーンアップ機能を提供していますが、有料ツール（15〜25 ドル）であり、クリーンアップは、はるかに大きなアプリケーションの中の小さな機能のひとつにすぎません。InstallerClean は無料で、用途を絞った、オープンソースのツールです。

[CCleaner](https://www.ccleaner.com/) や [BleachBit](https://www.bleachbit.org/) のような汎用のシステムクリーナーは、`C:\Windows\Installer` には手を出しません。このフォルダーでは、登録済みのパッケージと不要なものを見分けるために Windows インストーラー API への問い合わせが必要で、ファイルツリーをただたどるだけの汎用クリーナーでは、インストール済みのアプリを壊しかねないからです。まさにこのフォルダーをきれいにしたい、というときに手に取るべきツールが InstallerClean です。

## コマンドライン

InstallerClean は、スクリプトやシステム管理での利用に向けて、無人（ヘッドレス）実行に対応しています。

```
使い方：
  installerclean-cli --help   このヘルプを表示（/?、-h も使用可）
  installerclean-cli --version  バージョンを表示（-v も使用可）
  installerclean-cli /s       スキャンのみ。削除可能なファイルを一覧表示
  installerclean-cli /d       削除可能なファイルを削除（ごみ箱）
  installerclean-cli /m       保存された既定の場所へ移動
  installerclean-cli /m PATH  指定したパスへ移動
```

GUI を起動するには、`InstallerClean.exe` を実行してください（または、setup でインストールした場合はスタートメニューのショートカットを使ってください）。

引数なしで、あるいは認識できないフラグを付けて実行すると、`installerclean-cli` はこの使い方を表示してコード `1` で終了します。そのため、フラグを取りこぼしたスケジュールタスクは、何もしないまま黙って成功するのではなく、はっきりと失敗します。明示的に `--help`、`/?`、`-h` を指定した場合は、同じ使い方を表示してコード `0` で終了します。

`/s` はドライラン（試し実行）です。スキャンを行い、削除するであろうファイルの名前とサイズを一覧表示して、終了します。クリーンアップ前の確認に便利です。終了コードは、スキャン成功で `0`、スキャン失敗で `1`、Ctrl+C で `130` です。ファイルはすべて `C:\Windows\Installer` 内にあります。

`/d` と `/m` は、スキャンしてから実行します。`/d` は削除可能なファイルをごみ箱へ送ります。`/m` はそれらをフォルダーへ移動します（コマンドラインで指定したフォルダー、または GUI で保存した既定の場所のいずれか）。終了コードは次のとおりです。`0` = 完全な成功、`2` = 部分的な成功（一部のファイルは成功し、一部は失敗）、`1` = 全面的な失敗（スキャンの失敗、引数の誤り、またはバッチ内のすべてのファイルが失敗）、`75` = 実行を妨げた一時的な状態（表示されるメッセージが、どの状態か、再試行が有効かどうかを説明します）、`130` = Ctrl+C。

CLI の出力は、エラーや診断のメッセージも含めて、すべて標準出力（stdout）に送られます。標準エラー出力（stderr）は別に用意していません。終了コードが機械可読の信号であり（実行ごとのアプリケーションイベントログの記録もこれと一致します）、スクリプトはテキストを解析するのではなく終了コードで判定すべきです。また、`installerclean-cli /s > audit.txt` とすれば、エラー行も含めて実行全体をキャプチャできます。

3 つのいずれも、管理者権限で昇格したコマンドプロンプトが必要です。グループポリシーが UAC の昇格プロンプトをブロックしている場合、プロセスは起動を拒否し、Windows は親シェルにエラー 740 を返します（PowerShell では `$LASTEXITCODE = 740`）。`taskkill /pid <pid>` では正常なキャンセルは行われません。単一インスタンスのミューテックスは、次回の実行時に AbandonedMutexException の経路を通じて回復されます。

なお、CLI 自体の出力は英語です。上記の説明は、利用できるオプションに対応しています。

### なぜ `installerclean.exe` ではなく `installerclean-cli` なのか

`InstallerClean.exe` は WPF の GUI で、コマンドライン引数には反応しません。`installerclean-cli.exe` はそれとは別のコンソール実行ファイルで、同じインストールディレクトリに同梱され、同じスキャン / 移動 / 削除の操作を PowerShell、cmd、スケジュールタスクに公開します。本物のコンソールプロセスなので、終了するまでプロンプトをブロックします。出力のリダイレクトやパイプは、ほかのコンソール exe と同じように行えます。

portable と slim のダウンロードには、GUI の exe だけが含まれます。GUI なしでコマンドラインだけがほしい場合は、[リリースページ](../../releases/latest)から `installerclean-cli.exe` をダウンロードして、直接実行してください。setup では、GUI と並べてこれもインストールされます。

## 動作要件

- Windows 10 または 11
- 管理者権限（`C:\Windows\Installer` は管理者専用です）

setup、portable、slim、CLI の各ビルドの選択肢については[ダウンロード](#ダウンロード)をご覧ください。

## ソースからのビルド

```
git clone https://github.com/no-faff/InstallerClean.git
cd InstallerClean
dotnet build src/InstallerClean/InstallerClean.csproj
```

テストを実行する場合：

```
dotnet test src/InstallerClean.Tests/
```

## 貢献

バグを見つけた、あるいは提案があるという場合は、[Issue を立てる](../../issues)か、[ディスカッション](../../discussions)を始めてください。プルリクエストも歓迎します。送信する前に `dotnet test` を実行してください。

## プロジェクトを応援する

InstallerClean が役に立ったなら、[No Faff を応援](https://nofaff.netlify.app/support)していただくか、GitHub でスターを付けていただけるとうれしいです。

## スター履歴

<a href="https://www.star-history.com/?repos=no-faff%2FInstallerClean&type=date&legend=top-left">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/chart?repos=no-faff/InstallerClean&type=date&theme=dark&legend=top-left" />
   <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/chart?repos=no-faff/InstallerClean&type=date&legend=top-left" />
   <img alt="スター履歴グラフ" src="https://api.star-history.com/chart?repos=no-faff/InstallerClean&type=date&legend=top-left" />
 </picture>
</a>

## ライセンス

[MIT](LICENSE)

---

🎶 [George Formby - When I'm Cleaning Windows](https://www.youtube.com/watch?v=sfmAeijj5cM). ぜひどうぞ
