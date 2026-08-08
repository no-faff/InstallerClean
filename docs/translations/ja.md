# InstallerClean in 日本語 (Japanese)

The text of InstallerClean's interface and command-line tool in English on the left, with the Japanese translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Japanese can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.ja.resx`](../../src/InstallerClean.Core/Resources/Strings.ja.resx), so do not edit it by hand. The Japanese translation itself lives in [`gen-strings-ja.mjs`](../../scripts/translations/gen-strings-ja.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | 日本語 |
| --- | --- |
| InstallerClean | InstallerClean |
| About | InstallerClean について |
| Registered files that should not be deleted | 削除すべきでない登録済みファイル |
| Unneeded files that are safe to delete | 削除しても安全な不要ファイル |

## Section headings

| English | 日本語 |
| --- | --- |
| PRODUCTS | 製品 |
| PATCHES | パッチ |
| PRODUCT DETAILS | 製品詳細 |
| BACKUP FOLDER | BACKUP FOLDER |
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
| Path to folder if you move rather than delete. | Path to folder if you move rather than delete. |
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
| Moving unneeded files... | Moving unneeded files... |
| Deleting unneeded files... | Deleting unneeded files... |
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
| Any unneeded files below are [safe to delete]. | Any unneeded files below are [safe to delete]. |
| They sit in {InstallerFolder}, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | これらのファイルは{InstallerFolder}にあり、プログラムがアンインストールされたとき ({0})、新しいパッチが置き換えたとき ({1})、または公開元が撤回したとき ({2})に取り残されます。InstallerClean は、Windows 自身が不要と報告したファイルのみをリストアップします。 |
| Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored. | Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored. |
| Nothing scanned yet. | まだ何もスキャンしていません。 |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | 「再スキャン」を押すと、{InstallerFolder} を調べて、どのプログラムも必要としなくなったインストーラーファイルを探します。 |
| These files can't be cleaned up right now. | これらのファイルは今はクリーンアップできません。 |
| Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. |
| Select a file to view details. | ファイルを選択して詳細を表示します。 |
| Select a product to view details. | 製品を選択して詳細を表示します。 |
| No metadata available. | メタデータはありません。 |
| This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. | This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. |
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
| Nothing removed | Nothing removed |
| Nothing to clean up in {InstallerFolder} | {InstallerFolder} にクリーンアップするものはありません |
| Scanned {0} {1} in {2} | {0} 個の {1} を {2} でスキャンしました |
| The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). |
| The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). |
| The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). |
| The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). |
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
| {0} {1} kept in place, because the records now claim what the scan flagged. | {0} {1} kept in place, because the records now claim what the scan flagged. |
| {0} {1} kept in place, because the Windows Installer records had changed by the final check. | {0} {1} kept in place, because the Windows Installer records had changed by the final check. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read in the final check. | {0} {1} kept in place, because the Windows Installer records could not be fully read in the final check. |
| {0} {1} kept in place, because Windows has a record of the program named inside. | {0} {1} kept in place, because Windows has a record of the program named inside. |
| {0} {1} kept in place, because InstallerClean couldn't find a program named inside. | {0} {1} kept in place, because InstallerClean couldn't find a program named inside. |
| Moved {0} of {1} {2} before you cancelled. | キャンセルするまでに、{1} 個中 {0} 個の {2} を移動しました。 |
| Permanently deleted {0} of {1} {2} before you cancelled. | キャンセルするまでに、{1} 個中 {0} 個の {2} を完全に削除しました。 |
| {0} {1} permanently deleted | {0} {1} permanently deleted |
| {0} {1} permanently deleted | {0} {1} permanently deleted |
| Glad to help. There's a tip jar if you're feeling kind. | お役に立てて何よりです。お心づけをいただけたら幸いです。 |

## Summaries and counts

| English | 日本語 |
| --- | --- |
| {0} file left alone | {0} file left alone |
| {0} files left alone | {0} files left alone |
| {0} unneeded file to clean up | クリーンアップ対象の不要ファイルが {0} 個 |
| {0} unneeded files to clean up | クリーンアップ対象の不要ファイルが {0} 個 |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | {0} 個の登録済みファイルが見つかりません (InstallerClean によって削除されたものではありません)。現時点では問題ありませんが、そのプログラムの将来の修復、更新、またはアンインストールが失敗する可能性があります。対処方法は詳細を開いてください。 |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | {0} 個の登録済みファイルが見つかりません (InstallerClean によって削除されたものではありません)。現時点では問題ありませんが、それらのプログラムの将来の修復、更新、またはアンインストールが失敗する可能性があります。対処方法は詳細を開いてください。 |
| InstallerClean couldn't match up everything in the Windows records, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. | InstallerClean couldn't match up everything in the Windows records, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. |
| {0} of {1} {2} | {2} {1} 個中 {0} 個 |
| {0} orphaned, {1} superseded, {2} obsoleted ({3}) | 孤立 {0}、置換済み {1}、廃止 {2}({3}) |
| {0} registered file left alone ({1}) | {0} registered file left alone ({1}) |
| {0} registered files left alone ({1}) | {0} registered files left alone ({1}) |

## Confirmation dialogs

| English | 日本語 |
| --- | --- |
| Move {0} {1} ({2})? | {0} 個の {1} ({2})を移動しますか？ |
| Files will be moved to: | ファイルは次の場所に移動されます： |
| Delete {0} {1} ({2})? | {0} 個の {1} ({2})を削除しますか？ |
| This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. | This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. |
| Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. | Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. |

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
| InstallerClean couldn't square this scan with the Windows Installer records: nearly every file Windows still lists as needed is missing from {InstallerFolder}, while what is actually in the folder matches almost nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. | InstallerClean couldn't square this scan with the Windows Installer records: nearly every file Windows still lists as needed is missing from {InstallerFolder}, while what is actually in the folder matches almost nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean は、何がまだ必要かを確かめられるだけの Windows Installer の登録情報を読み取れませんでした。インストール済みプログラムの一覧が不足した状態で返され、同じ登録情報をレジストリから直接読み取る方法でもエラーが発生しました。あるファイルを指し示す登録情報が読み取れなかったものの一つだったというだけで、そのファイルが孤立しているように見えてしまうことがあるため、InstallerClean は中止しました。何も削除していません。 |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. |
| Nothing was deleted | 何も削除されませんでした |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. |
| Invalid destination | 無効な移動先 |
| Could not write to destination | 移動先に書き込めませんでした |
| Move failed | 移動に失敗しました |
| Delete failed | 削除に失敗しました |
| Setting not saved | 設定の保存に失敗しました |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | 変更を保存できませんでした。次回の起動時に、InstallerClean は以前の設定に戻ります。 |
| The destination cannot be inside the Windows Installer folder. | 移動先を Windows Installer フォルダー内にすることはできません。 |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. |
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
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. |
| Windows reported a file error; the file was left in place. | Windows がファイルエラーを報告しました。ファイルはそのままにしてあります。 |
| Windows reported file errors; these files were left in place. | Windows がファイルエラーを報告しました。これらのファイルはそのままにしてあります。 |
| Something went wrong with this file; it was left in place. | このファイルで何らかの問題が発生しました。ファイルはそのままにしてあります。 |
| Something went wrong with these files; they were left in place. | これらのファイルで何らかの問題が発生しました。ファイルはそのままにしてあります。 |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Windows Installerフォルダー内へのファイル移動を拒否します (移動先：{0})。 |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. |
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
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> |

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
| Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. | Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. |
| Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. | Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. | Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. |
| Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. | Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. |
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
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Delete permanently removes the unneeded files. Cancel closes without deleting. |
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
| Backup folder | Backup folder |
| Products | 製品 |
| Patches | パッチ |
| Product details | 製品詳細 |
| Backup folder | Backup folder |
| Operation progress | 操作の進捗 |
| Scan {InstallerFolder} again | {InstallerFolder} を再スキャン |
| Scanning progress | スキャンの進捗 |
| Startup scan progress | 起動時スキャンの進捗 |
| Details, unneeded files | 詳細、不要ファイル |
| Available for cleanup. | クリーンアップ可能です。 |
| Details, registered files | 詳細、登録済みファイル |
| Read-only inventory. | 読み取り専用のインベントリです。 |
| Sorted by {0}, ascending | {0} で昇順にソート |
| Sorted by {0}, descending | {0} で降順にソート |
| Scan results | スキャン結果 |
| Result details | 結果の詳細 |
| File details | ファイルの詳細 |
| Product details | Product details |
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
| Error: unknown argument '{0}' | Error: unknown argument '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | エラー：予期しない余分な引数 '{0}' があります。移動先フォルダーにスペースが含まれる場合は、パス全体を引用符で囲んでください：/m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. |
| Cancelling... | キャンセル中... |
| Cancelled. | キャンセルされました。 |
| Error: unexpected failure ({0}). Details written to {1}. | Error: unexpected failure ({0}). Details written to {1}. |
| Error: unexpected failure ({0}). The crash log could not be written. | Error: unexpected failure ({0}). The crash log could not be written. |
| Scanning {InstallerFolder}... | {InstallerFolder} をスキャン中... |
| Found {0} unneeded {1} to clean up ({2}). | Found {0} unneeded {1} to clean up ({2}). |
| Found no unneeded files. | Found no unneeded files. |
| {0} registered file is missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, the same version, usually restores it. | {0} registered file is missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, the same version, usually restores it. |
| {0} registered files are missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, the same version, usually restores them. | {0} registered files are missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, the same version, usually restores them. |
| InstallerClean couldn't match up everything in the Windows records, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. | InstallerClean couldn't match up everything in the Windows records, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. |
| Deleting {0} unneeded {1}... | Deleting {0} unneeded {1}... |
| Permanently deleted {0} unneeded {1}. | Permanently deleted {0} unneeded {1}. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | エラー：移動先が指定されていません。/m PATH を使用してください (GUI で設定したデフォルトはユーザーごとのもので、スケジュール実行やサービスアカウントでの実行には適用されません)。 |
| Error: destination cannot be inside the Windows Installer folder. | エラー：移動先を Windows Installer フォルダー内にすることはできません。 |
| Error: destination must be a fully qualified path. Got: {0} | エラー：移動先は完全修飾パスである必要があります。指定されたもの：{0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. |
| Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. |
| Moving {0} unneeded {1} to {2}... | Moving {0} unneeded {1} to {2}... |
| Moved {0} unneeded {1}. | Moved {0} unneeded {1}. |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then run the command again. | InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then run the command again. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | 別の InstallerClean プロセスが単一インスタンスロックを保持しています (GUIまたは別のCLI実行)。終了コード75 (一時的)。後で再試行しても安全です。 |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | 注意：イベントログの書き込みに失敗しました。Application チャネルのアクセス許可またはグループポリシーを確認してください。 |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - {InstallerFolder} をクリーンアップ |
| Removes cached .msi and .msp files that no installed program still needs. | Removes cached .msi and .msp files that no installed program still needs. |
| Needs an elevated (administrator) prompt; Windows will not start it. | Needs an elevated (administrator) prompt; Windows will not start it. |
| Usage: | 使用方法： |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     このヘルプを表示 (/?、-hも受け付けます) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  バージョンを表示 (-vも受け付けます) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         Scan only - list unneeded files |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         Delete unneeded files permanently |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         Move to the saved backup folder |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m PATH    指定されたパスに移動 |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. |
| Exit codes: | 終了コード： |
|   0   success: the run finished with nothing left to do |   0   success: the run finished with nothing left to do |
|   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   partial: some processed, some not (a failure or a Ctrl+C part way) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  一時的：一時的な状態が実行をブロックしました (メッセージを参照) |
|   130 cancelled (Ctrl+C) |   130 キャンセル (Ctrl+C) |
