# InstallerClean in 日本語 (Japanese)

The text of InstallerClean's interface and command-line tool in English on the left, with the Japanese translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Japanese can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. The translation file itself is [`Strings.ja.resx`](../../src/InstallerClean.Core/Resources/Strings.ja.resx). This page is generated from it by `scripts/gen-translation-table.mjs`, so do not edit it by hand.

## Window titles

| English | 日本語 |
| --- | --- |
| InstallerClean | InstallerClean |
| About | InstallerClean について |
| Registered files that should not be deleted | 削除すべきでない登録済みファイル |
| Unneeded files that are safe to delete | 削除しても安全な不要ファイル |
| Confirm move | 移動の確認 |
| Confirm delete | 削除の確認 |
| Recycle Bin unavailable | ごみ箱が利用できません |

## Section headings

| English | 日本語 |
| --- | --- |
| PRODUCTS | 製品 |
| PATCHES | パッチ |
| PRODUCT DETAILS | 製品詳細 |
| MOVE LOCATION | 移動先 |
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
| _Delete | 削除(_D) |
| _Delete permanently | 完全に削除(_D) |
| _Done | 完了(_D) |
| Details | 詳細 |
| _Buy me a cuppa | コーヒーをおごる(_B) |
| Leave a _star on GitHub | GitHubでスターを付ける(_S) |
| MIT licence | MITライセンス |
| _Move | 移動(_M) |
| _Move instead | 代わりに移動(_M) |
| Path to folder if you Move instead of Delete | 削除せずに移動する場合のフォルダーパス |
| Open _release page | リリースページを開く(_R) |
| _Re-scan | 再スキャン(_R) |
| _Scan again | 再スキャン(_S) |
| Send report | レポートを送信 |
| _Send | 送信(_S) |

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
| Moving {0} {1}... | {0} 個の {1} を移動中... |
| Deleting {0} {1}... | {0} 個の {1} を削除中... |
| Move cancelled. {0} of {1} {2} processed. | 移動がキャンセルされました。{1} 個中 {0} 個の {2} を処理しました。 |
| Delete cancelled. {0} of {1} {2} processed. | 削除がキャンセルされました。{1} 個中 {0} 個の {2} を処理しました。 |
| Move failed ({0}). Details in {1}. | 移動に失敗しました ({0})。詳細は{1}をご覧ください。 |
| Move failed ({0}). The crash log could not be written. | 移動に失敗しました ({0})。クラッシュログを書き込めませんでした。 |
| Delete failed ({0}). Details in {1}. | 削除に失敗しました ({0})。詳細は{1}をご覧ください。 |
| Delete failed ({0}). The crash log could not be written. | 削除に失敗しました ({0})。クラッシュログを書き込めませんでした。 |
| Access denied. Windows refused the scan. | アクセスが拒否されました。Windows がスキャンを拒否しました。 |
| Scan failed: installer database unavailable. | スキャンに失敗しました：インストーラーデータベースが利用できません。 |
| Scan cancelled. | スキャンがキャンセルされました。 |
| Ready | 準備完了 |
| Scan failed ({0}). Details in {1}. | スキャンに失敗しました ({0})。詳細は {1} をご覧ください。 |
| Scan failed ({0}). The crash log could not be written. | スキャンに失敗しました ({0})。クラッシュログを書き込めませんでした。 |

## Main screen text

| English | 日本語 |
| --- | --- |
| The unneeded files below are safe to delete. | 以下の不要ファイルは削除しても安全です。 |
| They sit in C:\Windows\Installer, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | これらのファイルはC:\Windows\Installerにあり、プログラムがアンインストールされたとき ({0})、新しいパッチが置き換えたとき ({1})、または公開元が撤回したとき ({2})に取り残されます。InstallerClean は、Windows 自身が不要と報告したファイルのみをリストアップします。 |
| Delete them to the Recycle Bin, or use Move instead if you'd rather keep a copy. | ごみ箱に削除するか、コピーを保持したい場合は代わりに「移動」を使用してください。 |
| Nothing scanned yet. | まだ何もスキャンしていません。 |
| Press Re-scan to look through C:\Windows\Installer for installer files that no program still needs. | 「再スキャン」を押すと、C:\Windows\Installer を調べて、どのプログラムも必要としなくなったインストーラーファイルを探します。 |
| These files can't be cleaned up right now. | これらのファイルは今はクリーンアップできません。 |
| Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch the installer cache while it's changing. Once it's done, Re-scan and they come back. | 現在 Windows Installer を使用しているものがあります。通常は Windows Update またはバックグラウンドでインストール中のプログラムです。その実行中は移動と削除が一時停止され、InstallerClean は変更中のインストーラーキャッシュに触れません。完了したら、再スキャンすると復元されます。 |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | このマシンで以前の Windows Installer トランザクションが中断されています。キャッシュをクリーンアップする前に、そのインストールを再開またはロールバックするか (または Windows を再起動してください)。 |
| Windows has a file rename queued for the next restart that affects the Installer cache. Restart Windows before cleaning. | Windows はインストーラーキャッシュに影響するファイル名の変更を次の再起動のためにキューに入れています。クリーンアップする前に Windows を再起動してください。 |
| Select a file to view details. | ファイルを選択して詳細を表示します。 |
| Select a product to view details. | 製品を選択して詳細を表示します。 |
| No metadata available. | メタデータはありません。 |
| This installer file has been deleted. InstallerClean didn't do it, it never removes a file a program still needs; something else deleted this one before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. | このインストーラーファイルは削除されています。InstallerClean が削除したのではありません。プログラムがまだ必要とするファイルを削除することは決してありません。何か別のものが、InstallerClean を実行する前にこのファイルを削除しました。<br><br>今は問題を引き起こしませんが、それが属するプログラムの修復、更新、またはアンインストールを試みるまでは問題になりません。その時、Windowsがこのファイルを探して見つからないため、そのステップは失敗する可能性があります。<br><br>修正を試みるには、そのプログラムのインストーラーをメーカーからダウンロードし、既存のコピーに上書き実行してください (最初にアンインストールしないでください。アンインストール自体がこのファイルを必要とするステップです)。可能であればインストールされているバージョンを使用してください。Windows は異なるバージョンを拒否する可能性があります。これにより通常ファイルは復元され、設定は通常影響を受けませんが、Microsoft はそれを保証しておらず、最終手段はプログラム自体または Windows の再インストールです。 |
| The README [explains this folder], and how to recover a file, in Microsoft's own words. | README には、このフォルダーについて、[Microsoft自身の言葉で]説明されており、ファイルの回復方法も記載されています。 |
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
| Nothing to clean up in C:\Windows\Installer | C:\Windows\Installer にクリーンアップするものはありません |
| Scanned {0} {1} in {2} | {0} 個の {1} を {2} でスキャンしました |
| Copy them back if anything breaks ([it won't!]). | 何かが動作しなくなったらコピーを戻してください ([そんなことはありませんが！])。 |
| Until then, you can restore them if anything breaks ([it won't!]). | それまでは、何かが動作しなくなったら復元できます ([そんなことはありませんが！])。 |
| Empty it to actually reclaim the space. | ごみ箱を空にすると、実際に空き容量が増えます。 |
| {0} freed | {0} 解放 |
| {0} cleaned up | {0} クリーンアップ |
| {0} moved | {0} 移動 |
| {0} moved, some files could not be processed | {0} 移動、一部のファイルは処理できませんでした |
| {0} freed, some files could not be processed | {0} 解放、一部のファイルは処理できませんでした |
| {0} cleaned up, some files could not be processed | {0} クリーンアップ、一部のファイルは処理できませんでした |
| {0} {1} moved to: {2} | {0} 個の {1} を次の場所に移動しました：{2} |
| {0} {1} moved to: {2} | {0} 個の {1} を次の場所に移動しました：{2} |
| {0} {1} moved to: {2}. {3} {4} | {0} 個の {1} を次の場所に移動しました：{2}。{3} 個の {4} |
| {0} {1} moved to: {2}. {3} {4} | {0} 個の {1} を次の場所に移動しました：{2}。{3} 個の {4} |
| {0} {1} moved to the Recycle Bin | {0} 個の {1} をごみ箱に移動しました |
| {0} {1} moved to the Recycle Bin | {0} 個の {1} をごみ箱に移動しました |
| {0} {1} moved to the Recycle Bin. {2} {3} | {0} 個の {1} をごみ箱に移動しました。{2} 個の {3} |
| {0} {1} moved to the Recycle Bin. {2} {3} | {0} 個の {1} をごみ箱に移動しました。{2} 個の {3} |
| {0} {1} kept in place, because a program started needing them again after the scan. | {0} 個の {1} はそのまま残しました。スキャン後にプログラムが再び必要としたためです。 |
| Moved {0} of {1} {2} before you cancelled. | キャンセルするまでに、{1} 個中 {0} 個の {2} を移動しました。 |
| Moved {0} of {1} {2} to the Recycle Bin before you cancelled. | キャンセルするまでに、{1} 個中 {0} 個の {2} をごみ箱に移動しました。 |
| Permanently deleted {0} of {1} {2} before you cancelled. | キャンセルするまでに、{1} 個中 {0} 個の {2} を完全に削除しました。 |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. | {0} 個の {1} を完全に削除しました。ごみ箱には送られていません。 |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. | {0} 個の {1} を完全に削除しました。ごみ箱には送られていません。 |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. {2} {3} | {0} 個の {1} を完全に削除しました。ごみ箱には送られていません。{2} 個の {3} |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. {2} {3} | {0} 個の {1} を完全に削除しました。ごみ箱には送られていません。{2} 個の {3} |
| That's fine, it was safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | 問題ありません。削除しても安全でした。InstallerCleanはWindowsが不要と報告したファイルのみを削除し、プログラムがまだ必要とするファイルを削除することは決してありません。万が一、削除によってプログラムが修復、更新、またはアンインストールできなくなった場合でも、メーカーから再インストールすることで通常はファイルが復元されます (ただしMicrosoftは保証していません)。 |
| That's fine, they were safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | 問題ありません。削除しても安全でした。InstallerClean は Windows が不要と報告したファイルのみを削除し、プログラムがまだ必要とするファイルを削除することは決してありません。万が一、削除によってプログラムが修復、更新、またはアンインストールできなくなった場合でも、メーカーから再インストールすることで通常はファイルが復元されます (ただし Microsoft は保証していません)。 |

## Recycle Bin unavailable

| English | 日本語 |
| --- | --- |
| The Recycle Bin isn't available for this drive | このドライブではごみ箱が利用できません |
| So this {1} ({2}) hasn't been deleted. You can move it somewhere safe, or delete it permanently. | そのため、この {1} ({2})は削除されていません。安全な場所に移動するか、完全に削除することができます。 |
| So these {0} {1} ({2}) haven't been deleted. You can move them somewhere safe, or delete them permanently. | そのため、これらの {0} 個の {1} ({2}) は削除されていません。安全な場所に移動するか、完全に削除することができます。 |
| Deleting it is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | 削除は安全です。InstallerClean は Windows が不要と報告したファイルのみを削除し、プログラムがまだ必要とするファイルを削除することは決してありません。ごみ箱は単なる追加の安全対策です。万が一、削除によってプログラムが修復、更新、またはアンインストールできなくなった場合でも、メーカーから再インストールすることで通常はファイルが復元されます (ただし Microsoft は保証していません)。 |
| Deleting them is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | 削除は安全です。InstallerClean は Windows が不要と報告したファイルのみを削除し、プログラムがまだ必要とするファイルを削除することは決してありません。ごみ箱は単なる追加の安全対策です。万が一、削除によってプログラムが修復、更新、またはアンインストールできなくなった場合でも、メーカーから再インストールすることで通常はファイルが復元されます (ただし Microsoft は保証していません)。 |

## Summaries and counts

| English | 日本語 |
| --- | --- |
| {0} file still needed | まだ必要なファイルが {0} 個 |
| {0} files still needed | まだ必要なファイルが {0} 個 |
| {0} unneeded file to clean up | クリーンアップ対象の不要ファイルが {0} 個 |
| {0} unneeded files to clean up | クリーンアップ対象の不要ファイルが {0} 個 |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | {0} 個の登録済みファイルが見つかりません (InstallerClean によって削除されたものではありません)。現時点では問題ありませんが、そのプログラムの将来の修復、更新、またはアンインストールが失敗する可能性があります。対処方法は詳細を開いてください。 |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | {0} 個の登録済みファイルが見つかりません (InstallerClean によって削除されたものではありません)。現時点では問題ありませんが、それらのプログラムの将来の修復、更新、またはアンインストールが失敗する可能性があります。対処方法は詳細を開いてください。 |
| Windows still lists {0} old patch whose file is already gone from disk. That's harmless, and there's nothing you need to do. | Windows の一覧には、ファイルがすでにディスクから消えている古いパッチが {0} 個、まだ残っています。害はなく、特に何もする必要はありません。 |
| Windows still lists {0} old patches whose files are already gone from disk. That's harmless, and there's nothing you need to do. | Windows の一覧には、ファイルがすでにディスクから消えている古いパッチが {0} 個、まだ残っています。害はなく、特に何もする必要はありません。 |
| {0} of {1} {2} | {2} {1} 個中 {0} 個 |
| {0} orphaned, {1} superseded, {2} obsoleted ({3}) | 孤立 {0}、置換済み {1}、廃止 {2}({3}) |
| {0} registered file that is still needed ({1}) | まだ必要な登録ファイルが {0} 個({1}) |
| {0} registered files that are still needed ({1}) | まだ必要な登録ファイルが {0} 個({1}) |

## Confirmation dialogs

| English | 日本語 |
| --- | --- |
| Move {0} {1} ({2})? | {0} 個の {1} ({2})を移動しますか？ |
| Files will be moved to: | ファイルは次の場所に移動されます： |
| Delete {0} {1} ({2})? | {0} 個の {1} ({2})を削除しますか？ |
| Files will be moved to the Recycle Bin. If you'd like backup copies, use the Move button instead. | ファイルはごみ箱に移動されます。バックアップコピーが必要な場合は、代わりに「移動」ボタンを使用してください。 |
| This folder is on the same drive, so the move won't free any space by itself. You'll get the space back when you delete the files from it, or you can pick a folder on another drive instead. | このフォルダーは同じドライブ上にあるため、移動しただけでは空き容量は増えません。移動先からファイルを削除すれば、その分の容量が空きます。または、別のドライブのフォルダーを選ぶこともできます。 |

## Error messages

| English | 日本語 |
| --- | --- |
| Access denied | アクセスが拒否されました |
| Windows refused InstallerClean access. InstallerClean is already running as administrator, so starting it again that way won't help.<br><br>That leaves two likely causes: security software is holding C:\Windows\Installer, or the folder's permissions have been changed. Pausing the security software and trying again is the quickest one to rule out. | Windows が InstallerClean によるアクセスを拒否しました。InstallerClean はすでに管理者として実行されているため、もう一度同じ方法で起動し直しても解決しません。<br><br>そうなると、考えられる原因は2つに絞られます。セキュリティソフトが C:\Windows\Installer をロックしているか、フォルダーのアクセス許可が変更されたかのどちらかです。まずはセキュリティソフトを一時停止して再試行するのが、いちばん手早く原因を切り分けられる方法です。 |
| Installer database unavailable | インストーラーデータベースが利用できません |
| Scan failed | スキャンに失敗しました |
| The Windows Installer database appears to be empty or inaccessible. This is unusual even on a fresh Windows install and typically means the database is corrupt or a third-party tool has cleared it. Running 'sfc /scannow' from an elevated prompt usually repairs it. | Windows Installer データベースが空であるか、アクセスできないようです。これは新しい Windows インストールでも異常であり、通常はデータベースが破損しているか、サードパーティツールが消去したことを意味します。昇格したプロンプトで'sfc /scannow'を実行すると、通常は修復されます。 |
| Windows Installer refused to list the installed products, and InstallerClean is already running as administrator, so running it again won't help. The permissions on the Windows Installer records may have been changed, or security software may be blocking them. Running 'sfc /scannow' from an elevated prompt is worth a try. | Windows Installer がインストール済み製品の一覧表示を拒否しました。InstallerClean はすでに管理者として実行されているため、もう一度実行しても解決しません。Windows Installer の登録情報のアクセス許可が変更されたか、セキュリティソフトがブロックしている可能性があります。昇格したプロンプトで'sfc /scannow'を実行してみる価値はあります。 |
| Windows Installer refused to list products after {0} consecutive failures (last error code {1}). Try restarting Windows, or run 'sfc /scannow' from an elevated prompt. | Windows Installerは、{0} 回の連続失敗の後も製品の一覧表示を拒否しました (最後のエラーコード{1})。Windows を再起動するか、昇格したプロンプトで'sfc /scannow'を実行してみてください。 |
| Windows Installer refused to list a product's patches after {0} consecutive failures (last error code {1}). Try restarting Windows, or run 'sfc /scannow' from an elevated prompt. | Windows Installerは、{0} 回の連続失敗の後も製品のパッチの一覧表示を拒否しました (最後のエラーコード{1})。Windows を再起動するか、昇格したプロンプトで'sfc /scannow'を実行してみてください。 |
| InstallerClean couldn't cross-check this scan against Windows: everything Windows still lists is missing from the cache folder, while the files in the folder match nothing Windows knows about. That points to a problem reading the installer records rather than to files you can safely remove, so nothing has been offered for cleanup. Restarting Windows and scanning again usually clears it. | InstallerClean は今回のスキャン結果を Windows と照合できませんでした。Windows が今も一覧に挙げているファイルはすべてキャッシュフォルダーに見当たらず、一方でフォルダー内のファイルは Windows が把握しているものと一つも一致しません。これは、安全に削除できるファイルがあるというよりも、インストーラー登録情報の読み取りに問題があることを示しています。そのため、クリーンアップ対象は何も表示していません。Windows を再起動してからもう一度スキャンすると、通常は解消します。 |
| Invalid destination | 無効な移動先 |
| Could not write to destination | 移動先に書き込めませんでした |
| Move failed | 移動に失敗しました |
| Delete failed | 削除に失敗しました |
| The destination cannot be inside the Windows Installer folder. | 移動先を Windows Installer フォルダー内にすることはできません。 |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | 移動先 {0} は Windows システムフォルダー下に解決されます。%SystemRoot%、%ProgramFiles%、%ProgramData% 以外のパスを選択してください。 |
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
| This file is not inside the Windows Installer folder; refused for safety. | このファイルは Windows Installer フォルダー内にありません。安全のために拒否されました。 |
| Access denied. | アクセスが拒否されました。 |
| The operation failed. Try again or restart Windows. | 操作に失敗しました。もう一度試すか、Windows を再起動してください。 |
| Unknown error. | 不明なエラー。 |
| Couldn't move this file to the Recycle Bin (error {0}). It may be locked, in use or blocked by Windows. Use the Move button instead. | このファイルをごみ箱に移動できませんでした (エラー{0})。ロックされているか、使用中か、Windows によってブロックされている可能性があります。代わりに「移動」ボタンを使用してください。 |
| Windows blocked access to this file, even with administrator rights (error {0}). It is usually an ownership or permissions lock. Use the Move button instead. | Windows は管理者権限でもこのファイルへのアクセスをブロックしました (エラー{0})。通常は所有権またはアクセス許可のロックです。代わりに「移動」ボタンを使用してください。 |
| This file is open or locked by another program (error {0}). Close that program, or whatever is scanning it, then try again, or use the Move button instead. | このファイルは別のプログラムによって開かれているかロックされています (エラー{0})。そのプログラムまたはスキャンしているものを閉じてから再試行するか、代わりに「移動」ボタンを使用してください。 |
| The file was permanently deleted because it could not be moved to the Recycle Bin. | ごみ箱に移動できなかったため、ファイルは完全に削除されました。 |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Windows Installerフォルダー内へのファイル移動を拒否します (移動先：{0})。 |
| The Move location needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | 移動先には、ドライブ文字またはネットワーク共有から始まる、フォルダーへの完全なパスを指定してください (例：D:\Backup、\\server\backup)。InstallerClean はこのパスを使用できません：{0} |
| The Move location changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | ファイルの移動中に移動先が変更されました (フォルダーが置き換えられたか、リダイレクトされました)。誤った場所に書き込まないよう、InstallerClean は処理を停止しました。{0} を確認してから、再スキャンしてもう一度お試しください。 |
| Cannot write to {0}. | {0} に書き込めません。 |
| Could not find a unique filename for '{0}' after 10,000 attempts. | 10,000回の試行後も'{0}'の一意のファイル名が見つかりませんでした。 |

## Update check

| English | 日本語 |
| --- | --- |
| Check for updates | 更新の確認 |
| Checking... | 確認中... |
| Up to date. | 最新です。 |
| Update available | 更新があります |
| You're running version {0}.<br>Version {1} is available. | バージョン {0} を実行しています。<br>バージョン {1} が利用可能です。 |
| Couldn't reach GitHub. Check your internet connection and try again. | GitHub に到達できませんでした。インターネット接続を確認して再試行してください。 |
| GitHub returned an error response. The releases API may be rate-limited; try again in a few minutes. | GitHub がエラーレスポンスを返しました。リリース API がレート制限されている可能性があります。数分待ってから再試行してください。 |
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
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Redact both classes of detail before attaching this file to a<br># public bug report.<br> | # crash.log は InstallerClean からの未処理例外をキャプチャします。<br># 昇格時には、フレームワークの例外メッセージに実行中のセッションの<br># ファイルパス (Windows Installerクエリによって列挙された他のユーザーの<br># プロファイルを含む)が含まれる場合があります。<br># 更新チェックまたは結果ログPOSTからのネットワーク障害メッセージには、<br># 宛先URLおよび解決されたIP/プロキシアドレスが含まれる場合があります。<br># このファイルを公開バグレポートに添付する前に、両方のクラスの詳細を<br># 編集してください。<br> |

## Tooltips (hover text)

| English | 日本語 |
| --- | --- |
| Donate | 寄付 |
| It's thirsty work! | 喉が渇く仕事です！ |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | キャンセルが要求されました。InstallerClean は現在のステップが停止可能なポイントに達するのを待っています。大量の I/O または MSI データベース呼び出し中は数秒かかることがあります。 |
| Close | 閉じる |
| Leave a star on GitHub, report an Issue or post in Discussions. Any feedback welcome. | GitHub でスターを付けたり、問題を報告したり、ディスカッションに投稿したりしてください。あらゆるフィードバックを歓迎します。 |
| or report an Issue or post in Discussions. Any feedback welcome. | または問題を報告するか、ディスカッションに投稿してください。あらゆるフィードバックを歓迎します。 |
| Minimise | 最小化 |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | 任意ですが、歓迎します。匿名の要約を送信するもので、正常に動作しているか、どれだけの容量が解放されているかを知るためのものです。次の画面で送信前に送信内容を確認できます。 |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | 任意ですが、歓迎します。匿名の要約を送信するもので、正常に動作しているかを知るためのものです。次の画面で送信前に送信内容を確認できます。 |
| Move the unneeded files to the Move location. | 不要ファイルを移動先に移動します。 |
| Move the unneeded files to the Move location. Choose one first. | 不要ファイルを移動先に移動します。先に移動先を選択してください。 |
| Move the unneeded files to the Recycle Bin. | 不要ファイルをごみ箱に移動します。 |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | 埋め込まれた Authenticode 証明書のサブジェクト名です。チェーン検証はされていません。 |
| Change language. The program will restart. | 言語を変更します。プログラムが再起動します。 |

## Screen reader labels

| English | 日本語 |
| --- | --- |
| Donate | 寄付 |
| Buy me a cuppa (About window) | 一杯おごる (バージョン情報ウィンドウ) |
| Cancel operation | 操作をキャンセル |
| Cancel scan | スキャンをキャンセル |
| Cancel startup scan | 起動時スキャンをキャンセル |
| Close | 閉じる |
| Close window | ウィンドウを閉じる |
| Close result and return to main window | 結果を閉じてメインウィンドウに戻る |
| Leave a star on GitHub | GitHub でスターを付ける |
| Leave a star on GitHub (About window) | GitHub でスターを付ける (バージョン情報ウィンドウ) |
| Minimise | 最小化 |
| Move all unneeded installer files to the chosen destination folder | 不要なインストーラーファイルをすべて、選択した移動先フォルダーに移動 |
| Move all unneeded installer files to the Recycle Bin | 不要なインストーラーファイルをすべてごみ箱に移動 |
| Delete moves the unneeded files to the Recycle Bin. Cancel closes without deleting. | 削除を実行すると不要ファイルがごみ箱に移動されます。キャンセルは削除せずに閉じます。 |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | 移動を実行すると不要ファイルが選択した移動先フォルダーに移動されます。キャンセルはそのままの場所に残します。 |
| Choose how to handle the unneeded files: move them somewhere safe, delete them permanently or cancel. | 不要ファイルの処理方法を選択：安全な場所に移動するか、完全に削除するか、キャンセルするか。 |
| Move the unneeded files to a folder you choose | 不要ファイルを選択したフォルダーに移動 |
| Delete the unneeded files permanently because the Recycle Bin is unavailable for this drive | このドライブではごみ箱が利用できないため、不要ファイルを完全に削除 |
| Posts to nofaff.netlify.app. Counts and labels only. You will see the exact payload before sending. | nofaff.netlify.app に投稿します。カウントとラベルのみです。送信前に正確なペイロードを確認できます。 |
| Say thanks | 謝意を伝える |
| Send posts the report shown to No Faff. Cancel sends nothing. | 送信を実行すると表示されたレポートが No Faff に投稿されます。キャンセルは何も送信しません。 |
| Check for updates | 更新の確認 |
| Checks the GitHub releases API over HTTPS for a newer version. | HTTPS 経由で GitHub リリース API をチェックし、新しいバージョンがないか確認します。 |
| Open the release page to download the newer version, or cancel to keep the current version. | リリースページを開いて新しいバージョンをダウンロードするか、キャンセルして現在のバージョンを維持します。 |
| MIT licence | MIT ライセンス |
| Opens the licence file on github.com in your browser. | ブラウザで github.com のライセンスファイルを開きます。 |
| Move location | 移動先 |
| Products | 製品 |
| Patches | パッチ |
| Product details | 製品詳細 |
| Move destination folder | 移動先フォルダー |
| Operation progress | 操作の進捗 |
| Scan C:\Windows\Installer again | C:\Windows\Installer を再スキャン |
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
| Dialog text | ダイアログテキスト |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | 処理できなかったファイル |
| Explains this folder, and how to recover a file, in the README | このフォルダーとファイルの回復方法を README で説明しています |
| Result log preview | 結果ログのプレビュー |
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
| Unknown argument: '{0}' | 不明な引数：'{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | エラー：予期しない余分な引数 '{0}' があります。移動先フォルダーにスペースが含まれる場合は、パス全体を引用符で囲んでください：/m "D:\My Backup" |
| Cancelling... | キャンセル中... |
| Cancelled. | キャンセルされました。 |
| Error: {0}. Details written to {1}. | エラー：{0}。詳細は {1} に書き込まれました。 |
| Error: {0}. The crash log could not be written. | エラー：{0}。クラッシュログを書き込めませんでした。 |
| Scanning C:\Windows\Installer... | C:\Windows\Installer をスキャン中... |
| Found {0} {1} to clean up ({2}). | クリーンアップ対象の {0} 個の {1} が見つかりました ({2})。 |
| Nothing to do. | 実行するものはありません。 |
| Deleting {0} {1}... | {0} 個の {1} を削除中... |
| Deleted {0} {1}. | {0} 個の {1} を削除しました。 |
| Error: the Recycle Bin is unavailable for this volume, so nothing was deleted. Use /m to move the files instead, or re-enable the Recycle Bin and run again. | エラー：このボリュームではごみ箱が利用できないため、何も削除されませんでした。代わりに/mを使用してファイルを移動するか、ごみ箱を再度有効にして再実行してください。 |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | エラー：移動先が指定されていません。/m PATH を使用してください (GUI で設定したデフォルトはユーザーごとのもので、スケジュール実行やサービスアカウントでの実行には適用されません)。 |
| Error: destination cannot be inside the Windows Installer folder. | エラー：移動先を Windows Installer フォルダー内にすることはできません。 |
| Error: destination must be a fully qualified path. Got: {0} | エラー：移動先は完全修飾パスである必要があります。指定されたもの：{0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | エラー：移動先 {0} は Windows システムフォルダー下に解決されます。%SystemRoot%、%ProgramFiles%、%ProgramData%以外のパスを選択してください。 |
| Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are blocked while that runs. Try again once it finishes. | エラー：現在 Windows Installer を使用しているものがあります。通常は Windows Update またはバックグラウンドでインストール中のプログラムです。その実行中は移動と削除がブロックされます。完了したら再試行してください。 |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | エラー：このマシンで以前の Windows Installer トランザクションが中断されています。キャッシュをクリーンアップする前に、そのインストールを再開またはロールバックするか (または Windows を再起動してください)。 |
| Error: a queued post-reboot file operation targets the Installer cache ({0}). Restart Windows to complete that operation before cleaning. | エラー：キューに入れられた再起動後のファイル操作がインストーラーキャッシュ ({0})を対象としています。クリーンアップ前にその操作を完了するために Windows を再起動してください。 |
| Moving {0} {1} to {2}... | {0} 個の {1} を {2} に移動中... |
| Moved {0} {1}. | {0} 個の {1} を移動しました。 |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | 別の InstallerClean プロセスが単一インスタンスロックを保持しています (GUIまたは別のCLI実行)。終了コード75 (一時的)。後で再試行しても安全です。 |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | 注意：イベントログの書き込みに失敗しました。Application チャネルのアクセス許可またはグループポリシーを確認してください。 |
| InstallerClean - clean up C:\Windows\Installer | InstallerClean - C:\Windows\Installer をクリーンアップ |
| Usage: | 使用方法： |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     このヘルプを表示 (/?、-hも受け付けます) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  バージョンを表示 (-vも受け付けます) |
|   installerclean-cli /s         Scan only - list removable files |   installerclean-cli /s         スキャンのみ - 削除可能なファイルを一覧表示 |
|   installerclean-cli /d         Delete removable files (Recycle Bin) |   installerclean-cli /d         削除可能なファイルを削除 (ごみ箱) |
|   installerclean-cli /m         Move to saved default location |   installerclean-cli /m         保存されたデフォルトの場所に移動 |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m PATH    指定されたパスに移動 |
| installerclean-cli is a real console process and blocks the prompt | installerclean-cliは実際のコンソールプロセスであり、完了するまで |
| until it finishes; redirect or pipe its output as you would any | プロンプトをブロックします。他のコンソール exe と同様に、出力をリダイレクト |
| other console exe. The GUI lives in InstallerClean.exe alongside it. | またはパイプできます。GUI は同じ場所にある InstallerClean.exe にあります。 |
| The saved default is per-user; scheduled or SYSTEM runs need /m PATH. | 保存されたデフォルトはユーザーごとの設定です。スケジュール実行や SYSTEM 実行では /m PATH が必要です。 |
| Exit codes: | 終了コード： |
|   0   success: every flagged file was processed |   0   成功：フラグが立てられたすべてのファイルが処理されました |
|   1   failure: nothing processed (bad args, scan failed, all files failed) |   1   失敗：何も処理されませんでした (不正な引数、スキャン失敗、すべてのファイルが失敗) |
|   2   partial: some files processed, some failed |   2   部分完了：一部のファイルは処理され、一部は失敗しました |
|   75  transient: a temporary condition blocked the run (see the message) |   75  一時的：一時的な状態が実行をブロックしました (メッセージを参照) |
|   130 cancelled (Ctrl+C) |   130 キャンセル (Ctrl+C) |
