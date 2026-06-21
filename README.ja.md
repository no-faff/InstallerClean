<p align="center">
  <a href="README.md">English</a> · <a href="README.zh-CN.md">简体中文</a> · <a href="README.es.md">Español</a> · <strong>日本語</strong> · <a href="README.pt-BR.md">Português (BR)</a> · <a href="README.ru.md">Русский</a> · <a href="README.fr.md">Français</a> · <a href="README.it.md">Italiano</a>
</p>

<p align="center"><em>このページは翻訳版ですが、アプリの画面表示は現在のところ英語のみです。</em></p>

<p align="center">
  <img src="docs/icon.png" width="280" alt="InstallerClean">
</p>

<p align="center"><em>🎶 What's my line? I'm happy <a href="https://www.youtube.com/watch?v=HM-jHhUZfFI">cleaning Windows</a></em></p>

<h1 align="center">InstallerClean</h1>

<p align="center"><strong><code>C:\Windows\Installer</code>、つまり気づかないうちにディスク容量を食いつぶしていく Windows の隠しフォルダーを、安全にクリーンアップするためのオープンソースツールです。</strong></p>

<p align="center"><em>一度使う。少し容量が空くかも。あとは捨てる。</em></p>

<p align="center">
  <a href="LICENSE"><img src="https://img.shields.io/badge/licence-MIT-blue.svg" alt="ライセンス: MIT"></a>
  <a href="https://dotnet.microsoft.com/download/dotnet/10.0"><img src="https://img.shields.io/badge/.NET-10.0-purple.svg" alt=".NET 10"></a>
  <a href="https://github.com/no-faff/InstallerClean/actions/workflows/ci.yml"><img src="https://github.com/no-faff/InstallerClean/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases"><img src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4.svg" alt="Windows 10/11"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases/latest"><img src="https://img.shields.io/badge/release-v1.9.1-blue" alt="GitHub リリース"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases"><img src="https://img.shields.io/badge/downloads-22k-brightgreen" alt="総ダウンロード数"></a>
</p>

![クリーンアップ成功後の InstallerClean の画面：1.28 GB を解放し、69 個のファイルをごみ箱へ送ったところ](docs/screenshots/06-freed-success-done.webp)

- **概要：** InstallerClean がすることは 1 つだけです。Windows が一切クリーンアップしない隠しフォルダー `C:\Windows\Installer` から、不要なファイルを取り除きます。ほぼ一瞬で終わるスキャンのあと、不要なファイルがあるかどうかを知らせ、詳しく見たい人にはさらに詳細を示し、それらを削除して C: ドライブの空き容量を増やせるようにします。一度使ったら、それで終わりです。
- **どれくらい空くか：** これまでに（任意で）送られてきたレポートでは、<!-- reports-freedpct-start -->42%<!-- reports-freedpct-end --> のマシンに掃除できる不要なファイルがありました。そのうち、解放できた容量の中央値は <!-- reports-median-start -->22 GB<!-- reports-median-end --> です。数百 GB を片づけた例もいくつかありました。私の場合は 1.28 GB でした。残りの <!-- reports-nothingpct-start -->58%<!-- reports-nothingpct-end --> は削除するものが見つからず、これは単に Installer フォルダーがすでにきれいだったというだけのことです。詳しくは下の [よくある質問](#よくある質問) をご覧ください。
- **安全性：** はい。どのファイルがまだ必要かを Windows インストーラー API 自身に問い合わせ、Windows が「用済み」と報告したファイルだけを一覧に出します。オープンソース（MIT）で、あなたについて何も尋ねません。アカウントも、広告も、追跡も、テレメトリもなく、バックグラウンドで動くものもありません。自分から勝手にインターネットへ接続することもありません。
- **入手方法：** [最新リリースをダウンロード](../../releases/latest)してください。実行し、[Windows の警告](#unknown-publisher)と[管理者権限の確認](#admin)をクリックして進みます。不要なファイルがあれば削除します。これで完了です。

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

どの Windows PC にも `C:\Windows\Installer` という隠しフォルダーがあります。Windows インストーラーの仕組みを使うソフトウェアをインストールしたり、Microsoft Office、Adobe Acrobat、Visual Studio などの `.msi` ベースのアプリケーションにパッチを適用したりするたびに、そのインストーラーや `.msp` パッチファイルのコピーがこのフォルダーに入り、そのまま残り続けます。

ソフトウェアをアンインストールしても、ファイルは残ります。新しいパッチが古いパッチを置き換えても、両方とも残ります。Windows がそれらをクリーンアップすることはありません。ディスク クリーンアップも手をつけません。DISM はまったく別のフォルダー用のツールです。時間とともに、フォルダーは膨らんでいきます。1 GB、5 GB、20 GB、50 GB と。MSI を多用するソフトウェアが入った環境（Acrobat が代表的な原因です）では、[100 GB を超える](https://www.reddit.com/r/sysadmin/comments/1oxcrmh/acrobat_filling_up_the_cwindowsinstaller_folder/)こともあります。

これらは、ひとりでに戻ってくるような一時ファイルではありません。正真正銘のお荷物です。何年も前にアンインストールしたソフトの古いインストーラーや、すでに何度も置き換えられたパッチなどです。一度消してしまえば、戻ってくることはありません。

**Windows でディスク容量を手軽に空けたいなら、このフォルダーは手始めにちょうどよい場所です。** InstallerClean は不要なファイルを見つけ出し、安全に削除します。

## 助けを求めて

このフォルダーについて一度でも調べたことがあるなら、お決まりの流れをご存じでしょう。`C:\Windows\Installer` に 180 GB を抱えた誰かが、掃除の仕方を尋ねる。[ディスク クリーンアップを使うよう言われる](https://learn.microsoft.com/en-us/answers/questions/4238108/windows-installer-folder-has-occupied-180gb)。試してみる。600 MB は片づくものの、そのフォルダーからは 1 バイトも減らない（ディスク クリーンアップは `C:\Windows\Installer` に手を触れないからです）。そしてスレッドは静かになります。

> *「見つかるスレッドはどれも、問題を解決しない同じ対処法ばかりを勧めてきて、そのまま書き込みが途絶えてしまうんです。」*
>
> [ksparks519, r/Windows10](https://www.reddit.com/r/Windows10/comments/1bt8c5p/anyone_ever_figure_out_giant_installer_folders/)（英語原文からの翻訳）

あるいは、まったく触るなと言われることもあります。あるスレッドでは、60 GB の Installer フォルダーを抱えた人が[「いじるな」](https://www.reddit.com/r/techsupport/comments/1hw4suq/my_windows_installer_folder_is_like_60gb_so_i/)と言われました。では代わりにどうすればいいのかと尋ねると、返ってきた答えは「さっき言っただろう」でした。

世間で言われる定番のアドバイスは、ファイルを手当たり次第に削除すること（これは本当に危険です）と、Windows 自身がもう不要だと言っているファイルを削除すること（こちらは危険ではありません）を混同しています。InstallerClean が行うのは後者です。

## 何をするのか

1. `C:\Windows\Installer` 内の `.msi` と `.msp` ファイルを**スキャン**します。
2. Windows インストーラー API に**問い合わせ**て、どのファイルがまだ登録されているかを確認します。
3. 解放できる容量と、まだ必要な容量を**表示**します（すべてのファイルを一覧する詳細ウィンドウも、必要に応じて開けます）。
4. 不要なファイルを**削除**します。ごみ箱へ送るか、選んだフォルダーへ移動します。

## スクリーンショット

<p>
  <img src="docs/screenshots/01-initial-scan.webp" alt="スキャン実行中、InstallerClean のロゴが表示されたスプラッシュ画面" width="900"><br>
  <em>最初のスキャン。とても高速です。</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/02-main-window.webp" alt="まだ必要な 120 個のファイル（2.83 GB）と、クリーンアップ対象の不要な 69 個のファイル（1.28 GB）を示すメイン画面。移動先の入力欄と、「削除」「移動」ボタンがある" width="900"><br>
  <em>結果の画面。どれだけがまだ必要で、どれだけが削除できるかがわかります。</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/03-details-registered.webp" alt="インストール済み製品を一覧表示し、選択した製品のインストーラーデータベースの詳細を示す登録済みファイルのウィンドウ" width="900"><br>
  <em>まだ必要なファイルの詳細。インストーラーデータベースから読み取ったメタデータ付きです。</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/04-details-safe-to-delete.webp" alt="削除可能な .msi ファイルをサイズ順に並べ、それぞれが削除可能な理由と、選択したファイルの詳細を示す不要なファイルのウィンドウ" width="900"><br>
  <em>もう必要のないファイルの詳細。</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/05-delete-dialogue.webp" alt="69 個のファイル（1.28 GB）の削除を確認し、ファイルがごみ箱へ送られることを伝える削除確認ダイアログ" width="900"><br>
  <em>どちらの操作の前にも確認があります。「削除」はごみ箱へ送り、「移動」はファイルをお好みの場所に置きます。</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/06-freed-success-done.webp" alt="1.28 GB を解放し、69 個のファイルをごみ箱へ送ったことを示す成功画面" width="900"><br>
  <em>削除が成功した後の画面。</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/07-scanned-again-all-clean.webp" alt="再スキャン後の「すべてクリーン」画面。C:\Windows\Installer にクリーンアップするものはありません" width="900"><br>
  <em>再スキャン後。もうクリーンアップするものはありません。</em>
  <br><br>
</p>

## 仕組み

InstallerClean は、不要なファイルを 3 種類に分けて見つけ出します。

**孤立したファイル**とは、ソフトウェアをアンインストールした後に取り残された `.msi` インストーラー（および `.msp` パッチ）です。Windows はもう参照していませんが、ファイルはフォルダーに居座って容量を占有し続けます。

**置き換えられたパッチ**とは、より新しいパッチに取って代わられた古い `.msp` パッチです。Windows は自身のデータベースで「置き換え済み」と記録しますが、削除はしません。頻繁にパッチを配布するベンダー（Acrobat、Office、大規模な開発ツールなど）では、置き換えられたパッチが際限なくたまっていきます。

**廃止されたパッチ**とは、発行元が新しいバージョンで置き換えるのではなく、取り下げたり非推奨にしたりした `.msp` パッチです。Windows はその状態も記録しますが、やはりファイルはフォルダーに残したままにします。

これらを見つけるために、InstallerClean は P/Invoke を介して Windows インストーラーの COM インターフェイスを直接呼び出します。

- `MsiEnumProductsEx`：インストール済みのすべての製品を列挙します
- `MsiEnumPatchesEx`：各製品に登録されているすべてのパッチを見つけます
- `MsiGetPatchInfoEx`：パッチの状態（適用済み、置き換え済み、または廃止済み）を読み取ります

`C:\Windows\Installer` 内の `.msi` または `.msp` ファイルのうち、登録済みのどの製品からも要求されていないものは孤立したファイルであり、削除可能なものとして示されます。データベースが「置き換え済み」または「廃止済み」と記録し、かつアンインストールに不要なパッチも同様です。

API が不完全なデータを返した場合（まれですが、インストーラーの状態が壊れていると起こり得ます）、アプリはレジストリの読み取りにフォールバックします。このフォールバックは「まだ必要」の側にだけファイルを追加し、「削除可能」の側に加えることは決してありません。

「移動」または「削除」が完了すると、`C:\Windows\Installer` 内の空になったサブフォルダー（中身がなくなった後にキャッシュが残す空ディレクトリ）も、同じ処理の中で削除されます。

## 安全ですか？

はい。InstallerClean は、Windows 自身が「何がインストールされているか」を管理するのに使っているのと同じ Windows インストーラー API のデータベースに問い合わせます。Windows がもう不要だと言うファイルなら、アプリはそれを信頼します。ファイル名や日付から推測することはありません。

**「削除」と「移動」について。** InstallerClean が削除するファイルは、完全に削除してしまっても問題ありません。「削除」を選ぶと、それらはごみ箱へ送られます（ごみ箱が使えない場合は警告が表示されます）。ごみ箱を空にすると、その分の容量が C: ドライブに戻ります。

とはいえ、ファイルが削除しても安全だという点を、私の言葉だけで信じる必要はありません。ファイルがごみ箱に入っている間に、このフォルダーを使うアプリ（Office、Acrobat、Visual Studio など）が、引き続き問題なく更新・アンインストールできることを確かめられます。もし何かが壊れていたら（そんなことはありませんが！）、ごみ箱からファイルを復元すれば直せます。より万全を期すなら、代わりに「移動」を使い、ファイルを自分で選んだフォルダーに置いておくこともできます（C: ドライブの容量を空けたいのであれば、当然ながら別のパーティションやドライブにあるフォルダーを選んでください）。元どおりに戻したくなったら、ファイルを `C:\Windows\Installer` にコピーして戻すだけです（とはいえ、その必要はないはずですが！）。

Windows インストーラーが今まさにキャッシュへ書き込んでいる、前回のトランザクションが中断されている、またはキャッシュを対象とする再起動後のリネームが予約されている場合、「移動」と「削除」は無効になり、具体的な理由が表示されます。

スキャン、クエリ、移動、削除、設定、再起動保留チェックの各サービスは、コミットのたびに実行される自動テストでカバーされています（上の CI バッジを参照）。

**バイナリの検証について。** InstallerClean には署名がありませんが、鵜呑みにする必要はありません。

- 各リリースの SHA-256 ハッシュは[リリースページ](../../releases/latest)に掲載しています。
- VirusTotal：すべてのエンジンでクリーンです。各リリースのノートにライブリンクを掲載しているので、ご自身で再確認できます。
- ソースコードは [github.com/no-faff/InstallerClean](https://github.com/no-faff/InstallerClean) にあり、CI が毎回のコミットをビルド・テストしています（上の緑色の CI バッジを参照）。
- GitHub、MajorGeeks、Softpedia を合わせて <!-- downloads-start -->22k<!-- downloads-end --> 回ダウンロードされています。
- [MajorGeeks](https://www.majorgeeks.com/files/details/installerclean.html) は各提出物を仮想マシンでテストし、審査を通過したものだけを掲載します。
- [Softpedia](https://www.softpedia.com/get/System/Hard-Disk-Utils/InstallerClean.shtml) は各リリースをウイルス・スパイウェア・アドウェアについて検査しています。

<a href="https://www.softpedia.com/get/System/Hard-Disk-Utils/InstallerClean.shtml"><img src="docs/badges/softpedia-100-free2.webp" alt="Softpedia 認証済み 100% クリーン" width="190"></a>

<a id="recovery"></a>
## 万一 `C:\Windows\Installer` のファイルが失われてしまったら

InstallerClean が削除するのは、Windows 自身がもう不要だと報告したファイルだけなので、ファイルが失われる原因になることは決してありません。とはいえ、すでにどれかが失われている場合、InstallerClean はそれを見つけて警告します。その対処法は次のとおりです。

該当するプログラムのインストーラーを提供元からダウンロードし、既存のインストールに上書きする形で実行してください。先にアンインストールしてはいけません。可能なら、今使っているのと同じバージョンを使ってください。Windows は別のバージョンを受け付けないことがあるからです。これで通常はファイルが元に戻り、設定もそのまま残ります。InstallerClean で再スキャンして、うまくいっていれば警告は消えています。

これでたいていは解決します。以下は Microsoft 自身による、より詳しい説明です。公式の詳細と、そう簡単にはいかない難しいケースについて述べています。いずれも InstallerClean が原因ではなく、私が Microsoft の案内より良いものを示すこともできないので、そのままお伝えします。

<details>
<summary>Microsoft のより詳しい見解</summary>

*以下の Microsoft の引用は、英語の原文のまま掲載しています。*

詳しい案内の全文：[Restore missing Windows Installer cache files](https://learn.microsoft.com/en-us/troubleshoot/windows-client/application-management/missing-windows-installer-cache)。

*すぐには表面化しないことがあります：*
> "If the installer cache is compromised, you may not immediately see problems until you take an action such as uninstalling, repairing, or updating a product."

*ファイルはマシンごとに固有なので、別の PC からコピーすることはできません：*
> "Missing files cannot be copied between computers because the files are unique."

*バックアップから、その失われたファイルだけを取り出すこともできません：*
> "To restore the missing files, a full system state restoration is required. It is not possible to replace only the missing files from a previous backup."

*推奨される復旧方法と、その率直な限界：*
> "If application files are missing from the Windows Installer Cache, ask the vendor or support team for the application about the missing files. You must follow the procedures or steps recommended by the application vendor to restore the files. In some cases, you may have to rebuild the operating system and reinstall the application to fix the problem."
>
> "Windows support engineers cannot help you recover missing application files from the Windows Installer cache."

*同じバージョンが重要な理由：*
> "The upgrade cannot be installed by the Windows Installer service because the program to be upgraded may be missing, or the upgrade may update a different version of the program."

</details>

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
- インターネットに接続するのは、あなたが指示したときだけです。手動の更新チェック、任意の匿名サマリー（アプリがちゃんと動いていると私に知らせるためだけのもの）、そして GitHub のドキュメントや寄付ページへのリンク（クリックを選んだ場合にブラウザーで開きます）です。
- ツールバーも、バンドルされたソフトウェアも、アドウェアもありません。

## よくある質問

**本当に数 GB も空きますか？** お使いのマシン次第です。追加ソフトを入れていないまっさらな Windows 11 では、削除するものは何もありません。長く使い込んだ開発用ワークステーションや、MSI ベースのソフトを多く入れたマシン（Acrobat、Office、LibreOffice、大規模な開発ツールなど）では、数十 GB になることもあります。いずれにせよ、実行した瞬間に、どれだけ空くかが正確にわかります。

<!-- reports-stats-start (generated by non-repo-files/refresh-reports-table.mjs; do not hand-edit between these markers) -->
v1.8.0 でこのオプションが追加されて以来、ご厚意で送っていただいた 84 件のレポート（ありがとうございます🙏）を集計すると、次のようになりました。

| 結果 | 割合 | 最小 | 中央値 | 最大 |
|---|---|---|---|---|
| 削除対象なし | 58% | - | - | - |
| 容量を解放 | 42% | 0.1 GB | 22 GB | 327 GB |
<!-- reports-stats-end -->

<details>
<summary>こうしたレポートは、任意で押せる「概要を送信」ボタンから送られます。送信前に表示される内容は次のとおりです。</summary>

![「No Faff に送信しますか？」と題された確認ダイアログ。送信される内容の全体が表示されている。アプリのバージョン、Windows のバージョン、スキャンの各件数、処理したファイル数、解放したバイト数。ファイルパスや名前、マシン ID は含まれず、あなたやマシンを特定するものは一切なく、アプリが動作したかどうかと解放した容量だけだという注記、そして「キャンセル」と「送信」のボタンがある。](docs/screenshots/optional-send-summary-confirmation-dialogue.webp)

</details>

<a id="admin"></a>

**なぜ管理者権限が必要なのですか？** `C:\Windows\Installer` は管理者だけがアクセスできるよう制限されています。フォルダーの読み取り、インストーラーデータベースへの問い合わせ、ファイルの移動や削除には、いずれも管理者権限が必要です。そのため、アプリは管理者として実行する必要があります。

<a id="unknown-publisher"></a>

**なぜ Windows は「不明な発行元」と表示するのですか？** InstallerClean がコード署名されていないからです。署名証明書は毎年費用がかかるため、お金を払うよりアプリを無料のままにしておきたいのです。そのため、実行すると Windows SmartScreen が「WindowsによってPCが保護されました」と表示します。**詳細情報**をクリックし、**実行**をクリックしてください。これは安全です。ソースコードは公開されており、各リリースには事前に確認できる VirusTotal リンクと SHA-256 ハッシュが付いています。

**削除を取り消せますか？** たいていは可能です。ドライブでごみ箱が使える場合、「削除」はファイルをごみ箱へ送るので、そこから復元できます。ごみ箱が使えない場合でも、アプリが自分の判断でファイルを完全に消すことはありません（[安全ですか？](#安全ですか)を参照）。それに、自分で管理できる戻し方がほしいなら、「移動」を使ってファイルを選んだフォルダーに置き、納得できたらそこから削除できます。

**これらのファイルを削除すると Windows が文句を言いませんか？** 言いません。InstallerClean が削除するのは Windows 自身が「用済み」と報告したファイルだけなので、削除するものの中に、プログラムの修復・更新・アンインストールに必要なものは含まれません。もし別の手段で必要なファイルが `C:\Windows\Installer` から消えてしまった場合は、[万一 C:\Windows\Installer のファイルが失われてしまったら](#recovery)をご覧ください。

**なぜ `Win32_Product`（WMI）を使わないのですか？** [`Win32_Product` は列挙の途中で製品ごとに MSI の修復処理を引き起こし](https://gregramsey.net/2012/02/20/win32_product-is-evil/)、数分かかったり、ディスクに大きな負荷をかけたりすることがあります。InstallerClean は Windows インストーラーの COM API を直接呼び出すので、副作用がありません。

**いっそ PowerShell スクリプトでよいのでは？** `MsiEnumPatchesEx` を呼び出す短いスクリプトでも、パッチを*一覧表示する*だけなら十分です。しかし InstallerClean の屋台骨となっているのは、スクリプトが見落としがちな部分です。孤立か置き換え済みかの分類、「まだ必要」の側にだけファイルを追加する（決して「削除可能」には加えない）レジストリフォールバック、再起動保留時のブロック、別の場所へ「移動」する安全網、キャンセル可能なファイルごとの進捗表示、そして完全削除ではなくごみ箱を既定とする動作です。MSI を本当に多用しているマシンでの特殊なケース（壊れた登録情報、キャッシュ内のジャンクション、`HKU\.DEFAULT` 内の製品、中断された Installer トランザクションなど）は、その場限りのスクリプトでは扱いを誤りやすいものです。スクリプトでの利用が目的なら、`installerclean-cli` がその無人実行向けの顔になります。

**Windows 7 や 8 で動きますか？** テストしておらず、サポート対象外です。対象は Windows 10 と 11 です。

**RMM や大規模展開に向いていますか？** はい。CLI は結果に応じて異なる終了コードを返します（0 = 成功、2 = 部分的成功、1 = 重大な失敗、75 = 一時的な状態、130 = ファイルを 1 つも処理する前の Ctrl+C。バッチの途中で Ctrl+C が入った場合は、すでに作業が行われているため 2 = 部分的成功になります）。そのため、スケジュールされたタスクは 75 のときだけ再試行でき、重大な失敗と混同せずに済みます。実行ごとの概要をアプリケーションイベントログに書き込み、GUI と同じ単一インスタンスのミューテックスを尊重します。setup は、Inno Setup の標準スイッチ（`/SILENT` または `/VERYSILENT`）でサイレントインストールにも対応します。サイレントインストールでは、インストール後の自動起動は行われません。詳しくはコマンドラインのセクションをご覧ください。

## ダウンロード

3 つのビルドがあります。お好みのものを選んでください。

- **Setup**（`InstallerClean-setup.exe`）：.NET 10 ランタイムを同梱した、ごく普通の Windows インストーラーです。スタートメニューに項目を追加し、きれいにアンインストールできます。「プログラム」の一覧に収まるので、半年後でも見つけやすいはずです。
- **Portable**（`InstallerClean-portable.exe`）：ランタイムを同梱した、単一の自己完結型 exe です。インストール不要、アンインストーラーもありません。実行して、使って、消すだけ。必要になればいつでもまた実行できます。
- **CLI**（`installerclean-cli.exe`）：コマンドライン版を単体にしたもので、単一の自己完結型 exe です。インストール不要で、使用後はマシンに何も残りません。クライアント機に置いて、スキャンやクリーンアップを実行し、消すだけ。スクリプト、スケジュールされたタスク、大規模展開のために作られており、クライアントにデスクトップアプリを置かずに操作だけを行いたい場合に向いています。引数と終了コードについては[コマンドライン](#コマンドライン)を参照してください。

[リリースページ](../../releases/latest)からダウンロードして、実行してください。署名がないため、Windows は「不明な発行元」という警告を表示します。何が表示され、なぜ安全なのかは、[よくある質問](#unknown-publisher)で説明しています。

アプリは起動時に自動でスキャンします。結果を確認したら、**削除**または**移動**をクリックしてください。

または [winget](https://learn.microsoft.com/windows/package-manager/winget/) でインストールできます。

```
winget install NoFaff.InstallerClean
```

または [Scoop](https://scoop.sh) でインストールできます。

```
scoop bucket add no-faff https://github.com/no-faff/scoop-bucket
scoop install installerclean
```

## PatchCleaner との比較

このフォルダーについて以前に調べたことがあるなら、たどり着く可能性が最も高いツールは [PatchCleaner](https://www.homedev.com.au/free/patchcleaner) でしょう。今も健在ですが、私が InstallerClean を作ったのは、PatchCleaner がクローズドソースで、2016 年 3 月以降は更新されておらず、さらに既定では Adobe 製品に手を触れないからです。PatchCleaner の孤立判定は Adobe のパッチを誤って検出してしまい、それらを削除すると Adobe の更新が壊れていました。そのため、フィルターをオフにしない限り、Adobe のファイルにはいっさい手を出しません。Adobe が最大の容量の食い手になっているマシンでは、それが空き容量の大半を占めます。

> *「孤立した .msp ファイルを削除しようと PatchCleaner をダウンロードしたのですが、どうやらこれでは 250 MB しか空かないようです。ファイルのうち 29 GB が『フィルターで除外』されているので、PatchCleaner は役に立たないようです。」*
>
> HeatherBunny1111, [r/techsupport](https://www.reddit.com/r/techsupport/comments/1qc4tcf/how_to_delete_msp_files_safely/)（英語原文からの翻訳）

InstallerClean は Windows インストーラー自身のパッチ記録を読み取るので、Adobe のどのパッチが本当に置き換え済みなのかを見分け、一律のフィルターを使わずに安全に削除できます。両者を比べると、次のようになります。

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
| 削除時の安全性 | ごみ箱へ送ります。使えない場合は、別の場所へ移動するか完全に削除するかを尋ねます | 完全削除、ごみ箱なし |

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

`/d` と `/m` は、スキャンしてから実行します。`/d` は削除可能なファイルをごみ箱へ送ります。`/m` はそれらをフォルダーへ移動します（コマンドラインで指定したフォルダー、または GUI で保存した既定の場所のいずれか）。終了コードは次のとおりです。`0` = 完全な成功、`2` = 部分的な成功（一部のファイルは成功し、一部は失敗）、`1` = 全面的な失敗（スキャンの失敗、引数の誤り、またはバッチ内のすべてのファイルが失敗）、`75` = 実行を妨げた一時的な状態（表示されるメッセージが、どの状態か、再試行が有効かどうかを説明します）、`130` = ファイルを 1 つも処理する前の Ctrl+C（バッチの途中で Ctrl+C が入った場合は、すでに作業が行われているため `2`（部分的な成功）になります）。

CLI の出力は、エラーや診断のメッセージも含めて、すべて標準出力（stdout）に送られます。標準エラー出力（stderr）は別に用意していません。終了コードが機械可読の信号であり（実行ごとのアプリケーションイベントログの記録もこれと一致します）、スクリプトはテキストを解析するのではなく終了コードで判定すべきです。また、`installerclean-cli /s > audit.txt` とすれば、エラー行も含めて実行全体をキャプチャできます。

3 つのいずれも、管理者権限で昇格したコマンドプロンプトが必要です。グループポリシーが UAC の昇格プロンプトをブロックしている場合、プロセスは起動を拒否し、Windows は親シェルにエラー 740 を返します（PowerShell では `$LASTEXITCODE = 740`）。`taskkill /pid <pid>` では正常なキャンセルは行われません。単一インスタンスのミューテックスは、次回の実行時に AbandonedMutexException の経路を通じて回復されます。

なお、CLI 自体の出力は英語です。上記の説明は、利用できるオプションに対応しています。

### なぜ `installerclean.exe` ではなく `installerclean-cli` なのか

`InstallerClean.exe` は WPF の GUI で、コマンドライン引数には反応しません。`installerclean-cli.exe` はそれとは別のコンソール実行ファイルで、同じインストールディレクトリに同梱され、同じスキャン / 移動 / 削除の操作を PowerShell、cmd、スケジュールタスクに公開します。本物のコンソールプロセスなので、終了するまでプロンプトをブロックします。出力のリダイレクトやパイプは、ほかのコンソール exe と同じように行えます。

portable のダウンロードには、GUI の exe だけが含まれます。GUI なしでコマンドラインだけがほしい場合は、[リリースページ](../../releases/latest)から `installerclean-cli.exe` をダウンロードして、直接実行してください。setup では、GUI と並べてこれもインストールされます。

## 動作要件

- Windows 10（バージョン 1607 / ビルド 14393 以降。.NET 10 ランタイムが対応する最も古いバージョンです）または Windows 11
- 管理者権限（`C:\Windows\Installer` は管理者専用です）

setup、portable、CLI の各ビルドの選択肢については[ダウンロード](#ダウンロード)をご覧ください。

## ソースからのビルド

```
git clone https://github.com/no-faff/InstallerClean.git
cd InstallerClean
dotnet build src/InstallerClean.sln
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
