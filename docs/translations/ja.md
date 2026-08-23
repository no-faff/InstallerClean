# InstallerClean in 日本語 (Japanese)

The text of InstallerClean's interface and command-line tool in English on the left, with the Japanese translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Japanese can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.ja.resx`](../../src/InstallerClean.Core/Resources/Strings.ja.resx), so do not edit it by hand. The Japanese translation itself lives in [`gen-strings-ja.mjs`](../../scripts/translations/gen-strings-ja.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | 日本語 |
| --- | --- |
| InstallerClean | InstallerClean |
| About | InstallerClean について |
| Files left alone | そのままにしたファイル |
| Unneeded files that are safe to delete | 削除しても安全な不要ファイル |

## Section headings

| English | 日本語 |
| --- | --- |
| PATCHES | パッチ |
| PRODUCT DETAILS | 製品詳細 |
| BACKUP FOLDER | 移動先フォルダー |
| SAY THANKS | 謝意を伝える |

## Buttons and actions

| English | 日本語 |
| --- | --- |
| _About | InstallerClean について(_A) |
| Copy | コピー |
| Cut | 切り取り |
| Paste | 貼り付け |
| Select all | すべて選択 |
| _Browse... | 参照(_B)... |
| _Cancel | キャンセル(_C) |
| Check for _updates | 更新の確認(_U) |
| _Close | 閉じる(_C) |
| _Delete permanently | 完全に削除(_D) |
| _Done | 完了(_D) |
| Details | 詳細 |
| _Buy me a cuppa | コーヒーを一杯おごる(_B) |
| Leave a _star on GitHub | GitHubでスターを付ける(_S) |
| Apache 2.0 licence | Apache 2.0 ライセンス |
| _Move | 移動(_M) |
| Path to folder if you move rather than delete. | 削除ではなく移動する場合のフォルダーのパス。 |
| Open _release page | リリースページを開く(_R) |
| _Re-scan | 再スキャン(_R) |
| _Scan again | 再スキャン(_S) |
| Send report | レポートを送信 |
| _Send | 送信(_S) |

## About window

| English | 日本語 |
| --- | --- |
| Guide and FAQ | ガイドとよくある質問 |
| Report a problem | 問題を報告 |
| Check for updates automatically | 更新を自動的に確認する |

## Field labels

| English | 日本語 |
| --- | --- |
| Reason | 理由 |
| Author | 作成者 |
| Application | アプリケーション |
| Title | タイトル |
| Subject | 件名 |
| Keywords | キーワード |
| Signing certificate | 署名証明書 |
| File size | ファイルサイズ |
| Comment | コメント |
| Product name | 製品名 |
| File | ファイル |
| Size | サイズ |
| Patches | パッチ |
| (unknown) | (不明) |
| (patches only) | (パッチのみ) |
| missing | 見つかりません |

## Status and progress

| English | 日本語 |
| --- | --- |
| Scanning... | スキャン中... |
| Cancelling... | キャンセル中... |
| Starting scan... | スキャンを開始しています... |
| Asking Windows about installed software... | Windowsにインストール済みソフトウェアについて問い合わせ中... |
| Scanning installer cache folder... | インストーラーキャッシュフォルダーをスキャン中... |
| Enumerating installed products... | インストール済み製品を列挙中... |
| Checking registry for additional packages... | 追加パッケージがないかレジストリをチェック中... |
| Found {0} registered {1}. | {0} 個の登録済み {1} が見つかりました。 |
| Scan complete ({0}) | スキャン完了 ({0}) |
| Scanning local packages... | ローカルパッケージをスキャン中... |
| Found {0} {1} you can safely delete. | 安全に削除できる {0} 個の {1} が見つかりました。 |
| Preparing destination folder... | 移動先フォルダーを準備中... |
| Moving unneeded files... | 不要ファイルを移動しています... |
| Deleting unneeded files... | 不要ファイルを削除しています... |
| Move cancelled. {0} of {1} {2} processed. | 移動がキャンセルされました。{1} 個中 {0} 個の {2} を処理しました。 |
| Delete cancelled. {0} of {1} {2} processed. | 削除がキャンセルされました。{1} 個中 {0} 個の {2} を処理しました。 |
| Move failed ({0}). Details in {1}. | 移動に失敗しました ({0})。詳細は{1}をご覧ください。 |
| Move failed ({0}). The crash log could not be written. | 移動に失敗しました ({0})。クラッシュログを書き込めませんでした。 |
| Delete failed ({0}). Details in {1}. | 削除に失敗しました ({0})。詳細は{1}をご覧ください。 |
| Delete failed ({0}). The crash log could not be written. | 削除に失敗しました ({0})。クラッシュログを書き込めませんでした。 |
| Access denied. Windows refused the scan. | アクセスが拒否されました。Windows がスキャンを拒否しました。 |
| Scan failed: couldn't read the Windows Installer records. | スキャン失敗：Windows Installer の登録情報を読み取れませんでした。 |
| Scan cancelled. | スキャンがキャンセルされました。 |
| Ready | 準備完了 |
| Scan failed ({0}). Details in {1}. | スキャンに失敗しました ({0})。詳細は {1} をご覧ください。 |
| Scan failed ({0}). The crash log could not be written. | スキャンに失敗しました ({0})。クラッシュログを書き込めませんでした。 |

## Main screen text

| English | 日本語 |
| --- | --- |
| Any unneeded files below are [safe to delete]. | 下にある不要ファイルはいずれも[安全に削除できます]。 |
| They sit in {InstallerFolder}. InstallerClean asks Windows about every installed program: a file is listed when no program claims it ({0}), or when a newer patch has replaced it and no program could roll back to it ({1}). | これらは {InstallerFolder} にあります。InstallerClean はインストール済みのすべてのプログラムについて Windows に問い合わせます。どのプログラムもそのファイルを自分のものだと示さない場合({0})、または新しいパッチが置き換えていてどのプログラムもそこへ戻れない場合({1})に、そのファイルが一覧に載ります。 |
| Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update, repair and uninstall as normal. Putting them back into {InstallerFolder} restores everything. Or delete them permanently now. | 選んだ移動先フォルダーへ移し、プログラムがこれまでどおり更新・修復・アンインストールできることを確かめてから、そのフォルダーを削除してください。{InstallerFolder} へ戻せばすべて元どおりになります。あるいは今すぐ完全に削除してください。 |
| Nothing scanned yet. | まだ何もスキャンしていません。 |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | 「再スキャン」を押すと、{InstallerFolder} を調べて、どのプログラムも必要としなくなったインストーラーファイルを探します。 |
| These files can't be cleaned up right now. | これらのファイルは今はクリーンアップできません。 |
| Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | 現在、Windows Update やバックグラウンドでインストール中のプログラムなど、何かが Windows Installer を使用しています。その間は移動と削除が一時停止し、InstallerClean は変更中の {InstallerFolder} に触れません。終わったら再スキャンすれば、どちらも使えるようになります。 |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | このコンピューターには、中断されたままの以前の Windows Installer トランザクションがあります。{InstallerFolder} をクリーンアップする前に、そのインストールを再開するかロールバックしてください(または Windows を再起動してください)。 |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows は次回の再起動時に実行するファイル名の変更をキューに入れており、それが {InstallerFolder} に影響します。クリーンアップする前に Windows を再起動してください。 |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer で処理が進行中のため、移動と削除は一時停止しています。InstallerClean は変更中の {InstallerFolder} には触れません。終わったら再スキャンすれば、どちらも使えるようになります。 |
| Select a file to view details. | ファイルを選択して詳細を表示します。 |
| Select a product to view details. | 製品を選択して詳細を表示します。 |
| No metadata available. | メタデータはありません。 |
| This installer file is missing. It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it, and its own last resort is reinstalling the program. | このインストーラーファイルが見つかりません。今のところ問題はなく、このファイルが属するプログラムを修復・更新・アンインストールしようとする日までは問題になりません。その時点で、Windows がこのファイルを探しても見つからないため、その手順が失敗することがあります。<br><br>直すには、そのプログラムのインストーラーを提供元からダウンロードし、既存のインストールに上書きする形で実行してください(先にアンインストールしないでください。アンインストール自体がこのファイルを必要とする手順です)。入手できるなら、インストール済みのものと同じバージョンを使ってください。別のバージョンは Windows に拒否されることがあります。これでファイルが復元され、設定はそのまま残るはずですが、Microsoft はそれを保証しておらず、Microsoft 自身の最終手段はプログラムの再インストールです。 |
| The README [explains this folder], and how to recover a file, in Microsoft's own words. | README には、[このフォルダー]について、Microsoft自身の言葉で説明されており、ファイルの回復方法も記載されています。 |
| (none) | (なし) |

## Reasons a file is unneeded

| English | 日本語 |
| --- | --- |
| Orphaned | 孤立 |
| Superseded | 置換済み |
| Obsoleted | 廃止 |

## Completion screen

| English | 日本語 |
| --- | --- |
| All clean | すべてクリーン |
| Nothing removed | 何も取り除かれませんでした |
| Nothing to clean up in {InstallerFolder} | {InstallerFolder} にクリーンアップするものはありません |
| Scanned {0} {1} in {2} | {0} 個の {1} を {2} でスキャンしました |
| Nothing offered on this PC | この PC では何も提示されませんでした |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({1}) it might otherwise have offered. | InstallerClean は、キャッシュ内のどのファイルがここにインストールされたプログラムのものかを確実には判断できなかったため、提示できたはずの 1 個のファイル({1})をそのままにしました。 |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back all {0} files ({1}) it might otherwise have offered. | InstallerClean は、キャッシュ内のどのファイルがここにインストールされたプログラムのものかを確実には判断できなかったため、提示できたはずの {0} 個のファイル({1})をすべてそのままにしました。 |
| The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | そのフォルダー内のファイルは[安全に取り除けます]ので、いつでもフォルダーを削除してかまいません。それまでは、万一プログラムが必要とした場合に {InstallerFolder} へ戻せます(可能性は極めて低いです)。 |
| The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | そのフォルダー内のファイルは[安全に取り除けます]ので、いつでもフォルダーを削除してかまいません。それまでは、万一プログラムがどれかを必要とした場合に {InstallerFolder} へ戻せます(可能性は極めて低いです)。 |
| The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | そのフォルダー内のファイルは[安全に取り除けます]ので、実際に容量を取り戻したくなったら、いつでもフォルダーを削除するか別のドライブへ移動してください。それまでは、万一プログラムが必要とした場合に {InstallerFolder} へ戻せます(可能性は極めて低いです)。 |
| The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | そのフォルダー内のファイルは[安全に取り除けます]ので、実際に容量を取り戻したくなったら、いつでもフォルダーを削除するか別のドライブへ移動してください。それまでは、万一プログラムがどれかを必要とした場合に {InstallerFolder} へ戻せます(可能性は極めて低いです)。 |
| {0} freed | {0} 解放 |
| {0} moved | {0} 移動 |
| Nothing was moved | 何も移動されませんでした |
| Nothing was deleted | 何も削除されませんでした |
| {0} of {1} could not be moved. | {1} 個中 {0} 個のファイルを移動できませんでした。 |
| {0} of {1} could not be moved. | {1} 個中 {0} 個のファイルを移動できませんでした。 |
| {0} of {1} could not be deleted. | {1} 個中 {0} 個のファイルを削除できませんでした。 |
| {0} of {1} could not be deleted. | {1} 個中 {0} 個のファイルを削除できませんでした。 |
| {0} {1} moved to: {2} | {0} 個の {1} を次の場所に移動しました：{2} |
| {0} {1} moved to: {2} | {0} 個の {1} を次の場所に移動しました：{2} |
| {0} {1} kept in place, because the records now claim what the scan flagged. | {0} 個の {1} をそのままにしました。登録情報が、スキャンで印を付けたものを現在は自分のものだと示しているためです。 |
| {0} {1} kept in place, because the Windows Installer records had changed by the final check. | {0} 個の {1} をそのままにしました。最終確認の時点で Windows Installer の登録情報が変わっていたためです。 |
| {0} {1} kept in place, because the Windows Installer records could not be fully read in the final check. | {0} 個の {1} をそのままにしました。最終確認で Windows Installer の登録情報をすべて読み取れなかったためです。 |
| {0} {1} kept in place, because by the final check InstallerClean could not be certain which cached files belong to the programs installed here. | {0} 個の {1} をそのままにしました。最終確認の時点まで、InstallerClean はキャッシュ内のどのファイルがここにインストールされたプログラムのものかを確実には判断できなかったためです。 |
| {0} {1} kept in place, because Windows has a record of the program named inside. | {0} 個の {1} をそのままにしました。ファイル内に記されたプログラムの登録情報が Windows にあるためです。 |
| {0} {1} kept in place, because InstallerClean couldn't find a program named inside. | {0} 個の {1} をそのままにしました。InstallerClean がファイル内にプログラム名を見つけられなかったためです。 |
| Moved {0} of {1} {2} before you cancelled. | キャンセルするまでに、{1} 個中 {0} 個の {2} を移動しました。 |
| Permanently deleted {0} of {1} {2} before you cancelled. | キャンセルするまでに、{1} 個中 {0} 個の {2} を完全に削除しました。 |
| {0} {1} permanently deleted | {0} 個の {1} を完全に削除しました |
| {0} {1} permanently deleted | {0} 個の {1} を完全に削除しました |
| Glad to help. There's a tip jar if you're feeling kind. | お役に立てて何よりです。お心づけをいただけたら幸いです。 |

## Summaries and counts

| English | 日本語 |
| --- | --- |
| {0} file left alone | {0} 個のファイルをそのままにしました |
| {0} files left alone | {0} 個のファイルをそのままにしました |
| {0} unneeded file to clean up | クリーンアップ対象の不要ファイルが {0} 個 |
| {0} unneeded files to clean up | クリーンアップ対象の不要ファイルが {0} 個 |
| Windows has a record for {0} file that isn't in {InstallerFolder}: {1}. It causes no trouble day to day, but a repair, update or uninstall can fail on it. Open Details for what to do. | {InstallerFolder} にない {0} 個のファイルについて、Windows に登録情報があります：{1}。日常的には支障ありませんが、修復・更新・アンインストールがこれで失敗することがあります。どうすればよいかは「詳細」を開いてください。 |
| Windows has records for {0} files that aren't in {InstallerFolder}: {1}. They cause no trouble day to day, but a repair, update or uninstall can fail on them. Open Details for what to do. | {InstallerFolder} にない {0} 個のファイルについて、Windows に登録情報があります：{1}。日常的には支障ありませんが、修復・更新・アンインストールがこれで失敗することがあります。どうすればよいかは「詳細」を開いてください。 |
| {0} other program | 他に {0} 個のプログラム |
| {0} other programs | 他に {0} 個のプログラム |
| {0} file with no program named in the records | 登録情報にプログラム名がない {0} 個のファイル |
| {0} files with no program named in the records | 登録情報にプログラム名がない {0} 個のファイル |
| On this PC InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file rather than listing it. | この PC では、InstallerClean はキャッシュ内のどのファイルがここにインストールされたプログラムのものかを確実には判断できなかったため、唯一のファイルを一覧に載せずそのままにしました。 |
| On this PC InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} rather than listing them. | この PC では、InstallerClean はキャッシュ内のどのファイルがここにインストールされたプログラムのものかを確実には判断できなかったため、{0} 個の {1} を一覧に載せずそのままにしました。 |
| InstallerClean couldn't match up everything in the Windows records, so it didn't read all of them. The unneeded files above are unaffected, but what it says about files missing from {InstallerFolder} may be short of the full picture. Re-scan to try again. | InstallerClean は Windows の登録情報にあるものすべてを突き合わせられなかったため、そのすべては読み取っていません。上の不要ファイルは影響を受けませんが、{InstallerFolder} にないファイルについての記載は全体を示していないことがあります。再スキャンしてもう一度お試しください。 |
| {0} of {1} {2} | {2} {1} 個中 {0} 個 |
| {0} unneeded {1} ({2}) | {0} 個の不要な {1} ({2}) |
| {0} file left alone ({1}) | {0} 個のファイルをそのままにしました ({1}) |
| {0} files left alone ({1}) | {0} 個のファイルをそのままにしました ({1}) |

## Confirmation dialogs

| English | 日本語 |
| --- | --- |
| Move {0} {1} ({2})? | {0} 個の {1} ({2})を移動しますか？ |
| Move to: | 移動先： |
| Delete {0} {1} ({2})? | {0} 個の {1} ({2})を削除しますか？ |
| This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. | このファイルは完全に削除されます。[安全に削除できます]が、バックアップが欲しい場合は代わりに「移動」ボタンを使ってください。 |
| Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. | これらのファイルは完全に削除されます。[安全に削除できます]が、バックアップが欲しい場合は代わりに「移動」ボタンを使ってください。 |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | そのフォルダーは同じドライブにあるため、削除するまで容量は戻りません。すぐに容量が必要な場合は、別のドライブのフォルダーを選んでください。 |

## Error messages

| English | 日本語 |
| --- | --- |
| Access denied | アクセスが拒否されました |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows が InstallerClean のアクセスを拒否したため、処理を中止しました。何も削除していません。<br><br>InstallerClean はすでに管理者として実行されていたため、同じように起動し直しても解決しません。Windows は何がアクセスを拒否したのかそれ以上説明しないため、具体的に試せることはありません。 |
| Couldn't read the Windows Installer records | Windows Installer の登録情報を読み取れませんでした |
| Scan failed | スキャンに失敗しました |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed. | Windows Installer の登録情報が完全に空の状態で返されました。インストール済みのプログラムも更新プログラムも、キャッシュされたインストーラーファイルを一つも要求していません。正常に動作しているコンピューターでは起こらないこと (インストール直後の Windows にも該当するファイルはあります) なので、登録情報が破損しているか、読み取れなかったかのいずれかです。この答えを信じたスキャンは、{InstallerFolder} 内のすべてのファイルを誤って孤立と判定してしまいます。InstallerClean はそうせずに中止しました。何も削除していません。 |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer が InstallerClean にインストール済みの一覧表示を許可しませんでした。InstallerClean はすでに管理者として実行されていたため、管理者として実行し直しても何も変わりません。この一覧がなければ、キャッシュされたどのファイルがまだ必要なのかを安全に判断する方法はないため、InstallerClean は中止しました。何も削除していません。 |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer は InstallerClean に、読み取り可能なインストール済みプログラムの一覧を渡せませんでした。{0} 件の項目が連続して読み取り不能で返されました (最後のエラーコード{1})。一部しか読めていない一覧を使うのではなく、InstallerClean は中止しました。何も削除していません。 |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer がインストール済みプログラムの一覧の終わりを最後まで知らせませんでした。InstallerClean は {0} 件で打ち切りました (最後のエラーコード{1})。終わりのない一覧は信頼できないため、InstallerClean は中止しました。何も削除していません。 |
| Windows Installer never signalled the end of one program's patch list: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer があるプログラムのパッチ一覧の終わりを最後まで知らせませんでした。InstallerClean は {0} 件で打ち切りました (最後のエラーコード{1})。終わりのない一覧は信頼できないため、InstallerClean は中止しました。何も削除していません。 |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean は Windows Installer の登録情報を {InstallerFolder} の内容と突き合わせられませんでした。登録情報が指しているもののほとんどがそこになく、そこにあるもののほとんどがどの登録情報にも名指しされていないため、どのファイルについても不要であることを示せませんでした。何も提示されず、何も取り除かれていません。 |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean は Windows Installer の登録情報を {InstallerFolder} の内容と突き合わせられませんでした。フォルダーにはファイルがありますが、その中のどれかを指す登録情報が一つもないため、どのファイルについても不要であることを示せませんでした。何も提示されず、何も取り除かれていません。 |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean は、何がまだ必要かを確かめられるだけの Windows Installer の登録情報を読み取れませんでした。インストール済みプログラムの一覧が不足した状態で返され、同じ登録情報をレジストリから直接読み取る方法でもエラーが発生しました。あるファイルを指し示す登録情報が読み取れなかったものの一つだったというだけで、そのファイルが孤立しているように見えてしまうことがあるため、InstallerClean は中止しました。何も削除していません。 |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean は {InstallerFolder} の実際のパスを Windows に解決させられなかったため、どのファイルについてもその中にあることを示せず、クリーンアップの対象として提示されたものはありません。今回のスキャンで何も見つからなかったのは、フォルダーがきれいだからではなく、その確認が失敗したためです。何も取り除かれていません。 |
| Nothing was deleted | 何も削除されませんでした |
| Nothing was moved | 何も移動されませんでした |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | 二つのプログラムが同時にインストール済みソフトウェアを変更しないよう Windows Installer が使うロックを InstallerClean が取得できなかったため、途中でファイルが必要になる可能性を排除できず、何も削除していません。もう一度お試しください。繰り返す場合は Windows を再起動してください。 |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. Try again, and restart Windows if it keeps happening. | 二つのプログラムが同時にインストール済みソフトウェアを変更しないよう Windows Installer が使うロックを InstallerClean が取得できなかったため、途中でファイルが必要になる可能性を排除できず、何も移動していません。もう一度お試しください。繰り返す場合は Windows を再起動してください。 |
| Invalid destination | 無効な移動先 |
| Could not write to destination | 移動先に書き込めませんでした |
| Move failed | 移動に失敗しました |
| Delete failed | 削除に失敗しました |
| Setting not saved | 設定の保存に失敗しました |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | 変更を保存できませんでした。次回の起動時に、InstallerClean は以前の設定に戻ります。 |
| The destination cannot be inside the Windows Installer folder. | 移動先を Windows Installer フォルダー内にすることはできません。 |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | 移動先 {0} は Windows のシステムフォルダー配下に解決されます。%SystemRoot%、%ProgramFiles%、%ProgramFiles(x86)%、%ProgramData% の外にあるパスを選んでください。 |
| Not enough space | 空き容量が不足しています |
| Not enough space at {0}<br><br>Required: {1}<br>Available: {2} | {0} の空き容量が不足しています<br><br>必要：{1}<br>利用可能：{2} |
| You don't have permission to write to {0}.<br>Try a folder in your user profile or on a drive you own. | {0} に書き込む権限がありません。<br>ユーザープロファイル内または自分が所有するドライブ上のフォルダーを試してください。 |
| The path {0} is too long for Windows. Pick a shorter path. | パス{0} は Windows にとって長すぎます。より短いパスを選択してください。 |
| The folder {0} does not exist and could not be created. Check the drive letter or network path. | フォルダー {0} が存在せず、作成できませんでした。ドライブ文字またはネットワークパスを確認してください。 |
| Windows cannot write to {0}.<br>Details in {1}. | Windows は {0} に書き込めません。<br>詳細は{1}をご覧ください。 |
| Windows cannot write to {0}. The crash log could not be written. | Windows は {0} に書き込めません。クラッシュログを書き込めませんでした。 |
| Cannot write to {0}.<br>Details in {1}. | {0} に書き込めません。<br>詳細は{1}をご覧ください。 |
| Cannot write to {0}. The crash log could not be written. | {0} に書き込めません。クラッシュログを書き込めませんでした。 |
| File no longer exists. | ファイルはもう存在しません。 |
| Source file is a symlink or junction; refused for safety. | ソースファイルはシンボリックリンクまたはジャンクションです。安全のために拒否されました。 |
| This file is not directly inside the Windows Installer folder; refused for safety. | このファイルは Windows Installer フォルダーの直下にありません。安全のために拒否されました。 |
| Windows refused access to this file; it was left in place. | Windows がこのファイルへのアクセスを拒否しました。ファイルはそのままにしてあります。 |
| Windows refused access to these files; they were left in place. | Windows がこれらのファイルへのアクセスを拒否しました。ファイルはそのままにしてあります。 |
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | このファイルは別のプログラムによって開かれているかロックされているため、今は取り除けません。そのままにしてあります。後でもう一度お試しください。 |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | これらのファイルは別のプログラムによって開かれているかロックされているため、今は取り除けません。そのままにしてあります。後でもう一度お試しください。 |
| Windows reported a file error; the file was left in place. | Windows がファイルエラーを報告しました。ファイルはそのままにしてあります。 |
| Windows reported file errors; these files were left in place. | Windows がファイルエラーを報告しました。これらのファイルはそのままにしてあります。 |
| Something went wrong with this file; it was left in place. | このファイルで何らかの問題が発生しました。ファイルはそのままにしてあります。 |
| Something went wrong with these files; they were left in place. | これらのファイルで何らかの問題が発生しました。ファイルはそのままにしてあります。 |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Windows Installerフォルダー内へのファイル移動を拒否します (移動先：{0})。 |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | 移動先フォルダーは、ドライブ文字またはネットワーク共有で始まる、フォルダーへの完全なパスである必要があります(例：D:\Backup、または \\server\backup)。InstallerClean はこれを使えません：{0} |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | InstallerClean は移動先フォルダーを確認できなくなったため、誤った場所に書き込まずに停止しました。{0} を確認してから、再スキャンしてもう一度お試しください。 |
| Cannot write to {0}. | {0} に書き込めません。 |
| Could not find a unique filename for '{0}' after 10,000 attempts. | 10,000回の試行後も'{0}'の一意のファイル名が見つかりませんでした。 |

## Update check

| English | 日本語 |
| --- | --- |
| Check for updates | 更新の確認 |
| Checking... | 確認中... |
| Up to date. | 最新です。 |
| Version {0} is available. | バージョン {0} が利用可能です。 |
| Update available | 更新があります |
| You're running version {0}.<br>Version {1} is available. | バージョン {0} を実行しています。<br>バージョン {1} が利用可能です。 |
| Couldn't reach GitHub. Check your internet connection and try again. | GitHub に到達できませんでした。インターネット接続を確認して再試行してください。 |
| GitHub returned an error response. Try again in a few minutes. | GitHub がエラーレスポンスを返しました。数分待ってから再試行してください。 |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | GitHub のレスポンスに認識可能なリリースが含まれていませんでした。後でもう一度試すか、リリースページを直接開いてください。 |
| The check timed out. Your connection to GitHub may be slow; try again. | 確認がタイムアウトしました。GitHub への接続が遅い可能性があります。再試行してください。 |
| The check failed for an unknown reason. Details are in crash.log if you need to report it. | 不明な理由で確認に失敗しました。報告が必要な場合は詳細が crash.log にあります。 |

## Opening links in your browser

| English | 日本語 |
| --- | --- |
| Couldn't open your browser | ブラウザを開けませんでした |
| InstallerClean couldn't open your browser. The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | InstallerClean はブラウザを開けませんでした。リンクはクリップボードにコピーしてあるので、ご自分で貼り付けられます：<br><br>{0} |
| InstallerClean couldn't open your browser, and couldn't copy the link to your clipboard either. The link is:<br><br>{0} | InstallerClean はブラウザを開けず、リンクをクリップボードにコピーすることもできませんでした。リンクはこちらです：<br><br>{0} |

## Sending the summary

| English | 日本語 |
| --- | --- |
| Sending... | 送信中... |
| Thanks! Report sent. | ありがとうございます！レポートを送信しました。 |
| Sending failed. Try again later. | 送信に失敗しました。後でもう一度試してください。 |
| No report to send. | 送信するレポートがありません。 |
| Send this? | これを送信しますか？ |
| It goes to nofaff.netlify.app/api/result-log. Nothing identifies you or your machine; it just lets me know InstallerClean's working and [how much space people are freeing]. | 送信先は nofaff.netlify.app/api/result-log です。あなたやあなたのマシンを特定するものは何もありません。InstallerClean が動作していることと、[どれだけの容量が解放されているか]を知るためのものです。 |

## Startup and crashes

| English | 日本語 |
| --- | --- |
| InstallerClean | InstallerClean |
| InstallerClean is already running. | InstallerClean は既に実行中です。 |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | 予期しないエラーが発生し、InstallerClean を終了する必要があります。<br><br>{0}<br><br>詳細は以下に書き込まれました：<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | 予期しないエラーが発生し、InstallerClean を終了する必要があります。<br><br>{0}<br><br>クラッシュログを書き込めませんでした。 |
| Startup error | 起動エラー |
| Failed to start ({0}). Details written to:<br>{1} | 起動に失敗しました ({0})。詳細は以下に書き込まれました：<br>{1} |
| Failed to start ({0}). The crash log could not be written. | 起動に失敗しました ({0})。クラッシュログを書き込めませんでした。 |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log には InstallerClean の未処理例外が記録されます。<br># 昇格した状態では、フレームワークの例外メッセージに実行中セッションの<br># ファイルパスが含まれることがあります(Windows Installer のクエリが<br># 列挙した他のユーザーのプロファイルを含む)。更新確認や結果ログの送信で<br># のネットワーク障害メッセージには、宛先 URL や解決された IP アドレス・<br># プロキシアドレスが含まれることがあります。読み取れない Windows<br># Installer の登録情報に関する項目には、Windows アカウントの SID<br># (S-1-5-21-...) やインストール済みソフトウェアの製品コードが含まれる<br># ことがあります。<br># このファイルを公開のバグ報告に添付する前に、三種類すべてを削除して<br># ください。<br> |

## Tooltips (hover text)

| English | 日本語 |
| --- | --- |
| It's thirsty work! | 喉が渇く仕事です！ |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | キャンセルが要求されました。InstallerClean は現在のステップが停止可能なポイントに達するのを待っています。大量の I/O または MSI データベース呼び出し中は数秒かかることがあります。 |
| Close | 閉じる |
| A star helps other people find it. | スターを付けると、InstallerClean を見つけてもらいやすくなります。 |
| Minimise | 最小化 |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | 任意ですが、歓迎します。匿名の要約を送信するもので、正常に動作しているか、どれだけの容量が解放されているかを知るためのものです。次の画面で送信前に送信内容を確認できます。 |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | 任意ですが、歓迎します。匿名の要約を送信するもので、正常に動作しているかを知るためのものです。次の画面で送信前に送信内容を確認できます。 |
| Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. | 不要ファイルを移動先フォルダーへ移します。どれも必要とされていないと納得できたら、そのフォルダーを削除してください。 |
| Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. | 不要ファイルを移動先フォルダーへ移します。フォルダーはこの後で選びます。どれも必要とされていないと納得できたら、そのフォルダーを削除してください。 |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. | 不要ファイルを移動先フォルダーへ移します。そのフォルダーは同じドライブにあるため、削除するか別のドライブへ移すまで容量は戻りません。どれも必要とされていないと納得できたら、そうしてください。 |
| Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. | 不要ファイルを完全に削除します。安全に取り除け、容量はすぐに戻ります。 |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | 埋め込まれた Authenticode 証明書のサブジェクト名です。チェーン検証はされていません。 |
| Change language. The program will restart. | 言語を変更します。プログラムが再起動します。 |

## Screen reader labels

| English | 日本語 |
| --- | --- |
| Donate | 寄付 |
| Buy me a cuppa | コーヒーを一杯おごる |
| Cancel operation | 操作をキャンセル |
| Cancel scan | スキャンをキャンセル |
| Cancel startup scan | 起動時スキャンをキャンセル |
| Close | 閉じる |
| Close window | ウィンドウを閉じる |
| Close result and return to main window | 結果を閉じてメインウィンドウに戻る |
| Leave a star on github | github でスターを付ける |
| Minimise | 最小化 |
| Delete permanently removes the unneeded files. Cancel closes without deleting. | 完全に削除すると不要ファイルが取り除かれます。キャンセルすると何も削除せずに閉じます。 |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | 移動を実行すると不要ファイルが選択した移動先フォルダーに移動されます。キャンセルはそのままの場所に残します。 |
| Say thanks | 謝意を伝える |
| Send posts the report shown to No Faff. Cancel sends nothing. | 送信を実行すると表示されたレポートが No Faff に投稿されます。キャンセルは何も送信しません。 |
| Check for updates | 更新の確認 |
| Checks github's releases page for a newer version. | github のリリースページで新しいバージョンがあるかどうかを確認します。 |
| Opens the readme on github in your browser. | ブラウザで github の readme を開きます。 |
| Opens the issue tracker on github.com in your browser. | ブラウザで github.com の Issue トラッカーを開きます。 |
| If ticked, InstallerClean checks github for a newer version when you run it. | チェックを入れると、InstallerClean は起動時に github で新しいバージョンを確認します。 |
| Open the release page to download the newer version, or cancel to keep the current version. | リリースページを開いて新しいバージョンをダウンロードするか、キャンセルして現在のバージョンを維持します。 |
| Opens the licence file on github.com in your browser. | ブラウザで github.com のライセンスファイルを開きます。 |
| Backup folder | 移動先フォルダー |
| Patches | パッチ |
| Product details | 製品詳細 |
| Backup folder | 移動先フォルダー |
| Operation progress | 操作の進捗 |
| Scan {InstallerFolder} again | {InstallerFolder} を再スキャン |
| Scanning progress | スキャンの進捗 |
| Startup scan progress | 起動時スキャンの進捗 |
| Details, unneeded files | 詳細、不要ファイル |
| Available for cleanup. | クリーンアップ可能です。 |
| Details, files left alone | 詳細、そのままにしたファイル |
| Read-only inventory. | 読み取り専用のインベントリです。 |
| Sorted by {0}, ascending | {0} で昇順にソート |
| Sorted by {0}, descending | {0} で降順にソート |
| Scan results | スキャン結果 |
| Result details | 結果の詳細 |
| File details | ファイルの詳細 |
| Product details | 製品の詳細 |
| Dialog text | ダイアログテキスト |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | 処理できなかったファイル |
| Explains this folder, and how to recover a file, in the README | このフォルダーとファイルの回復方法を README で説明しています |
| Report preview | レポートのプレビュー |
| Change language | 言語を変更 |
| The program will restart. | プログラムが再起動します。 |

## File picker

| English | 日本語 |
| --- | --- |
| Choose destination folder for moved files | 移動ファイルの移動先フォルダーを選択 |

## Version

| English | 日本語 |
| --- | --- |
| Version {0} | バージョン {0} |

## Word forms (singular and plural)

| English | 日本語 |
| --- | --- |
| file | ファイル |
| files | ファイル |
| error | エラー |
| errors | エラー |
| package | パッケージ |
| packages | パッケージ |
| product | 製品 |
| products | 製品 |
| patch | パッチ |
| patches | パッチ |

## Sizes and times

| English | 日本語 |
| --- | --- |
| ,  | 、 |
| {0:F2} GB | {0:F2} GB |
| {0:F1} MB | {0:F1} MB |
| {0:F1} KB | {0:F1} KB |
| {0} B | {0} B |
| {0:F0}ms | {0:F0}ms |
| {0:F1}s | {0:F1}s |
| less than a second | 1秒未満 |
| {0:F1} seconds | {0:F1}秒 |

## Command-line tool (installerclean-cli)

| English | 日本語 |
| --- | --- |
| Error: unknown argument '{0}' | エラー：不明な引数 '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | エラー：予期しない余分な引数 '{0}' があります。移動先フォルダーにスペースが含まれる場合は、パス全体を引用符で囲んでください：/m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | エラー：予期しない余分な引数 '{0}'。/s と /d は他の引数を取らず、1 回の実行で使えるフラグは 1 つだけです。 |
| Cancelling... | キャンセル中... |
| Cancelled. | キャンセルされました。 |
| Error: unexpected failure ({0}). Details written to {1}. | エラー：予期しない障害 ({0})。詳細を {1} に書き込みました。 |
| Error: unexpected failure ({0}). The crash log could not be written. | エラー：予期しない障害 ({0})。クラッシュログを書き込めませんでした。 |
| Scanning {InstallerFolder}... | {InstallerFolder} をスキャン中... |
| Found {0} unneeded {1} to clean up ({2}). | クリーンアップ対象の不要な {1} が {0} 個見つかりました ({2})。 |
| Found no unneeded files. | 不要なファイルは見つかりませんでした。 |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it held back the one file ({2}) it might otherwise have offered. | InstallerClean は、キャッシュ内のどのファイルがここにインストールされたプログラムのものかを確実には判断できなかったため、提示できたはずの 1 個のファイル({2})をそのままにしました。 |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it held back all {0} {1} ({2}) it might otherwise have offered. | InstallerClean は、キャッシュ内のどのファイルがここにインストールされたプログラムのものかを確実には判断できなかったため、提示できたはずの {0} 個の {1}({2})をすべてそのままにしました。 |
| Windows has a record for {0} file that is not in {InstallerFolder}: {1}. It causes no trouble day to day, but a repair, update or uninstall can fail on it. Running that program's installer again, preferably the same version, usually restores the file. | {InstallerFolder} にない {0} 個のファイルについて、Windows に登録情報があります：{1}。日常的には支障ありませんが、修復・更新・アンインストールがこれで失敗することがあります。そのプログラムのインストーラーを、できれば同じバージョンで実行し直すと、たいていファイルが復元されます。 |
| Windows has records for {0} files that are not in {InstallerFolder}: {1}. They cause no trouble day to day, but a repair, update or uninstall can fail on them. Running each program's installer again, preferably the same version, usually restores the files. | {InstallerFolder} にない {0} 個のファイルについて、Windows に登録情報があります：{1}。日常的には支障ありませんが、修復・更新・アンインストールがこれで失敗することがあります。各プログラムのインストーラーを、できれば同じバージョンで実行し直すと、たいていファイルが復元されます。 |
| InstallerClean couldn't match up everything in the Windows records, so it didn't read all of them. What it found is unaffected, but what it says about files missing from {InstallerFolder} may be short of the full picture. Running it again may pick up more. | InstallerClean は Windows の登録情報にあるものすべてを突き合わせられなかったため、そのすべては読み取っていません。見つかったものは影響を受けませんが、{InstallerFolder} にないファイルについての記載は全体を示していないことがあります。もう一度実行すると、さらに見つかることがあります。 |
| Deleting {0} unneeded {1}... | {0} 個の不要な {1} を削除しています... |
| Permanently deleted {0} unneeded {1}. | {0} 個の不要な {1} を完全に削除しました。 |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | エラー：移動先が指定されていません。/m PATH を使用してください (GUI で設定したデフォルトはユーザーごとのもので、スケジュール実行やサービスアカウントでの実行には適用されません)。 |
| Error: destination cannot be inside the Windows Installer folder. | エラー：移動先を Windows Installer フォルダー内にすることはできません。 |
| Error: destination must be a fully qualified path. Got: {0} | エラー：移動先は完全修飾パスである必要があります。指定されたもの：{0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | エラー：移動先 {0} は Windows のシステムフォルダー配下に解決されます。%SystemRoot%、%ProgramFiles%、%ProgramFiles(x86)%、%ProgramData% の外にあるパスを選んでください。 |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | エラー：{0} の空き容量が不足しています。これらのファイルの移動には {1} が必要ですが、空きは {2} です。何も移動されていません。 |
| Error: something is using Windows Installer right now, such as a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | エラー：現在、Windows Update やバックグラウンドでインストール中のプログラムなど、何かが Windows Installer を使用しています。その間 /m と /d はブロックされます。終わってからもう一度お試しください。 |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | エラー：このコンピューターには、中断されたままの以前の Windows Installer トランザクションがあります。{InstallerFolder} をクリーンアップする前に、そのインストールを再開するかロールバックしてください(または Windows を再起動してください)。 |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | エラー：再起動後に実行するためキューに入れられたファイル操作が {InstallerFolder} を対象にしています ({0})。クリーンアップする前に Windows を再起動して、その操作を完了させてください。 |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | エラー：Windows Installer で処理が進行中のため、/m と /d はブロックされます。InstallerClean は変更中の {InstallerFolder} には触れません。終わってからもう一度お試しください。 |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | エラー：二つのプログラムが同時にインストール済みソフトウェアを変更しないようにする Windows Installer のロックを InstallerClean が取得できなかったため、途中でファイルが必要になる可能性を排除できませんでした。何も削除されていません。もう一度お試しください。繰り返す場合は Windows を再起動してください。 |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. Try again, and restart Windows if it keeps happening. | エラー：二つのプログラムが同時にインストール済みソフトウェアを変更しないようにする Windows Installer のロックを InstallerClean が取得できなかったため、途中でファイルが必要になる可能性を排除できませんでした。何も移動されていません。もう一度お試しください。繰り返す場合は Windows を再起動してください。 |
| Moving {0} unneeded {1} to {2}... | {0} 個の不要な {1} を {2} へ移動しています... |
| Moved {0} unneeded {1}. | {0} 個の不要な {1} を移動しました。 |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then run the command again. | InstallerClean は移動先フォルダーを確認できなくなったため、誤った場所に書き込まずに停止しました。{0} を確認してから、コマンドをもう一度実行してください。 |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | 別の InstallerClean プロセスが単一インスタンスロックを保持しています (GUIまたは別のCLI実行)。終了コード75 (一時的)。後で再試行しても安全です。 |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | 注意：イベントログの書き込みに失敗しました。Application ログのアクセス許可またはグループポリシーを確認してください。 |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - {InstallerFolder} をクリーンアップ |
| Removes cached .msi and .msp files that no installed program still needs. | どのインストール済みプログラムも必要としない .msi/.msp を削除します。 |
| Needs an elevated (administrator) prompt; Windows will not start it. | 管理者権限のプロンプトが必要です。Windows はそれ以外では起動しません。 |
| Usage: | 使用方法： |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     このヘルプを表示 (/?、-hも受け付けます) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  バージョンを表示 (-vも受け付けます) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         スキャンのみ - 不要ファイルを一覧 |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         不要ファイルを完全に削除 |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         保存済みの移動先フォルダーへ移動 |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m PATH    指定されたパスに移動 |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli は終了までプロンプトを占有するため、スクリプトや<br>スケジュールされたタスクが完了を待てます。 |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | このフォルダーはユーザーごとに保存され、予約実行には /m PATH が必要です。 |
| Exit codes: | 終了コード： |
|   0   success: the run did what it was asked and nothing failed |   0   成功：求められた処理を行い、失敗は何もなかった |
|   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |   1   失敗：何も処理されなかった (引数や移動先の誤り、<br>       スキャンの失敗、または全ファイルの失敗) |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   一部：一部は処理され、一部は処理されず (失敗または Ctrl+C) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  一時的：一時的な状態が実行をブロックしました (メッセージを参照) |
|   130 cancelled (Ctrl+C) |   130 キャンセル (Ctrl+C) |
