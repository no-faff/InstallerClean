#!/usr/bin/env node
// Japanese (ja) satellite generator for InstallerClean.
//
// PROVENANCE: the Japanese translation was written from scratch by the native
// speaker coolvitto (PR #41), a contributor's gift and the strongest provenance
// of any satellite. This generator's MAP holds coolvitto's wording verbatim;
// regenerating reproduces Strings.ja.resx. Treat the MAP values as the native
// author's work, not a machine draft.
//
// Structure: UNLIKE the other satellites, ja carries the full neutral key set,
// including the 21 machine-contract Cli.EventLog* keys (coolvitto translated
// those too). They are forced to English at the emit site at runtime
// (MachineContract.cs), so carrying translated copies is harmless. This
// generator therefore does NOT strip the machine keys. Japanese has no count
// plurals, so there are no satellite-only overrides.
import { readFileSync, writeFileSync } from 'node:fs';

const dir = 'src/InstallerClean.Core/Resources';
const BASE = `${dir}/Strings.resx`;            // English source (the "neutral")
const OUT = `${dir}/Strings.ja.resx`;

// Universal keeps (brand names, pure-placeholder, size/elapsed formats).
const KEEP_ENGLISH = new Set([
  'Window.Main.Title',
  'Startup.AlreadyRunningTitle',
  'Startup.UnhandledTitle',
  'Automation.ScanResultAnnouncement',
  'Display.Size.GB',
  'Display.Size.MB',
  'Display.Size.KB',
  'Display.Size.B',
  'Display.Elapsed.Ms',
  'Display.Elapsed.S',
]);

// Per-language keeps: Japanese values byte-identical to English (genuine
// single-token matches, not misses). The self-check prints these so the keep
// stays honest.
const ALSO_KEEP = [];

const MAP = {
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `InstallerClean について`,
  'Window.Registered.Title': `削除すべきでない登録済みファイル`,
  'Window.Orphaned.Title': `削除しても安全な不要ファイル`,
  'Section.Registered.Products': `製品`,
  'Section.Registered.Patches': `パッチ`,
  'Section.Registered.Details': `製品詳細`,
  'Section.Move.Location': `移動先`,
  'Section.SayThanks': `謝意を伝える`,
  'Field.Reason': `理由`,
  'Field.Author': `作成者`,
  'Field.Application': `アプリケーション`,
  'Field.Title': `タイトル`,
  'Field.Subject': `件名`,
  'Field.Keywords': `キーワード`,
  'Field.SigningCertificate': `署名証明書`,
  'Field.FileSize': `ファイルサイズ`,
  'Field.Comment': `コメント`,
  'Field.ProductName': `製品名`,
  'Field.File': `ファイル`,
  'Field.Size': `サイズ`,
  'Field.Patches': `パッチ`,
  'Field.UnknownProductName': `(不明)`,
  'Field.PatchesOnly': `(パッチのみ)`,
  'Field.Missing': `見つかりません`,
  'Action.About': `InstallerClean について(_A)`,
  'Action.Copy': `コピー`,
  'Action.Cut': `切り取り`,
  'Action.Paste': `貼り付け`,
  'Action.SelectAll': `すべて選択`,
  'Action.Browse': `参照(_B)...`,
  'Action.Cancel': `キャンセル(_C)`,
  'Action.CheckForUpdates': `更新の確認(_U)`,
  'Action.Close': `閉じる(_C)`,
  'Action.DeletePermanently': `完全に削除(_D)`,
  'Action.Done': `完了(_D)`,
  'Action.Details': `詳細`,
  'Action.BuyMeACuppa': `コーヒーを一杯おごる(_B)`,
  'Action.LeaveStarOnGitHub': `GitHubでスターを付ける(_S)`,
  'Action.Licence': `Apache 2.0 ライセンス`,
  'Action.Move': `移動(_M)`,
  'Action.MoveDestinationPlaceholder': `削除せずに移動する場合のフォルダーパス`,
  'Action.OpenReleasePage': `リリースページを開く(_R)`,
  'Action.Rescan': `再スキャン(_R)`,
  'Action.ScanAgain': `再スキャン(_S)`,
  'Action.SendResultLog': `レポートを送信`,
  'Action.SendResultLogConfirm': `送信(_S)`,
  'Automation.BuyMeACuppa': `寄付`,
  'Automation.BuyMeACuppa.About': `コーヒーを一杯おごる`,
  'Automation.CancelOperation': `操作をキャンセル`,
  'Automation.CancelScan': `スキャンをキャンセル`,
  'Automation.CancelStartupScan': `起動時スキャンをキャンセル`,
  'Automation.Close': `閉じる`,
  'Automation.CloseWindow': `ウィンドウを閉じる`,
  'Automation.CloseResult': `結果を閉じてメインウィンドウに戻る`,
  'Automation.LeaveStarOnGitHub.About': `github でスターを付ける`,
  'Automation.Minimise': `最小化`,
  'Automation.ConfirmDelete': `削除を実行すると不要ファイルがごみ箱に移動されます。キャンセルは削除せずに閉じます。`,
  'Automation.ConfirmMove': `移動を実行すると不要ファイルが選択した移動先フォルダーに移動されます。キャンセルはそのままの場所に残します。`,
  'Automation.SayThanks': `謝意を伝える`,
  'Automation.ConfirmSendResultLog': `送信を実行すると表示されたレポートが No Faff に投稿されます。キャンセルは何も送信しません。`,
  'Automation.CheckForUpdates': `更新の確認`,
  'Automation.CheckForUpdates.HelpText': `github のリリースページで新しいバージョンがあるかどうかを確認します。`,
  'Automation.UpdateAvailable.HelpText': `リリースページを開いて新しいバージョンをダウンロードするか、キャンセルして現在のバージョンを維持します。`,
  'Automation.Licence.HelpText': `ブラウザで github.com のライセンスファイルを開きます。`,
  'Automation.Section.MoveLocation': `移動先`,
  'Automation.Section.Products': `製品`,
  'Automation.Section.Patches': `パッチ`,
  'Automation.Section.ProductDetails': `製品詳細`,
  'Automation.MoveDestinationFolder': `移動先`,
  'Automation.OperationProgress': `操作の進捗`,
  'Automation.RescanInstaller': `{InstallerFolder} を再スキャン`,
  'Automation.ScanningProgress': `スキャンの進捗`,
  'Automation.StartupScanProgress': `起動時スキャンの進捗`,
  'Automation.ViewOrphanedFiles': `詳細、不要ファイル`,
  'Automation.ViewOrphanedFiles.HelpText': `クリーンアップ可能です。`,
  'Automation.ViewRegisteredFiles': `詳細、登録済みファイル`,
  'Automation.ViewRegisteredFiles.HelpText': `読み取り専用のインベントリです。`,
  'Automation.SortStatus.Ascending': `{0} で昇順にソート`,
  'Automation.SortStatus.Descending': `{0} で降順にソート`,
  'Automation.Scroll.ScanResults': `スキャン結果`,
  'Automation.Scroll.ResultDetails': `結果の詳細`,
  'Automation.Scroll.FileDetails': `ファイルの詳細`,
  'Automation.Scroll.DialogBody': `ダイアログテキスト`,
  'Automation.ScanResultAnnouncement': `{0} ({1})`,
  'Automation.CompletionErrors': `処理できなかったファイル`,
  'Automation.RegisteredMissingSeeAlso': `このフォルダーとファイルの回復方法を README で説明しています`,
  'Tooltip.BuyMeACuppa.About': `喉が渇く仕事です！`,
  'Tooltip.CancellingPending': `キャンセルが要求されました。InstallerClean は現在のステップが停止可能なポイントに達するのを待っています。大量の I/O または MSI データベース呼び出し中は数秒かかることがあります。`,
  'Tooltip.Close': `閉じる`,
  'Tooltip.LeaveStarOnGitHub.About': `スターを付けると、InstallerClean を見つけてもらいやすくなります。`,
  'Tooltip.Minimise': `最小化`,
  'Tooltip.SendResultLog': `任意ですが、歓迎します。匿名の要約を送信するもので、正常に動作しているか、どれだけの容量が解放されているかを知るためのものです。次の画面で送信前に送信内容を確認できます。`,
  'Tooltip.SendResultLog.NothingFound': `任意ですが、歓迎します。匿名の要約を送信するもので、正常に動作しているかを知るためのものです。次の画面で送信前に送信内容を確認できます。`,
  'Tooltip.Move': `不要ファイルを移動先に移動します。`,
  'Tooltip.MoveNeedsDestination': `不要ファイルを安全な場所に移動します。フォルダーはこの後選択します。`,
  'Tooltip.Delete': `不要ファイルをごみ箱に移動します。`,
  'Tooltip.SigningCertificate': `埋め込まれた Authenticode 証明書のサブジェクト名です。チェーン検証はされていません。`,
  'Body.MainExplanation.Lead': `以下の不要ファイルは削除しても安全です。`,
  'Body.MainExplanation.Why': `これらのファイルは{InstallerFolder}にあり、プログラムがアンインストールされたとき ({0})、新しいパッチが置き換えたとき ({1})、または公開元が撤回したとき ({2})に取り残されます。InstallerClean は、Windows 自身が不要と報告したファイルのみをリストアップします。`,
  'Body.MainExplanation.Action': `ごみ箱に削除するか、バックアップコピーを残すには代わりに「移動」を使用してください。ファイルを{InstallerFolder}に戻せば、完全に元どおりになります。`,
  'Body.PendingReboot.MsiExecuteMutex': `現在 Windows Installer を使用しているものがあります。通常は Windows Update またはバックグラウンドでインストール中のプログラムです。その実行中は移動と削除が一時停止され、InstallerClean は変更中のインストーラーキャッシュに触れません。完了したら、再スキャンすると復元されます。`,
  'Body.PendingReboot.InstallerInProgress': `このマシンで以前の Windows Installer トランザクションが中断されています。キャッシュをクリーンアップする前に、そのインストールを再開またはロールバックするか (または Windows を再起動してください)。`,
  'Body.PendingReboot.PendingRenameInCache': `Windows はインストーラーキャッシュに影響するファイル名の変更を次の再起動のためにキューに入れています。クリーンアップする前に Windows を再起動してください。`,
  'Body.NoFileSelected': `ファイルを選択して詳細を表示します。`,
  'Body.NoProductSelected': `製品を選択して詳細を表示します。`,
  'Body.NoMetadata': `メタデータはありません。`,
  'Body.RegisteredMissingFromDisk': `このインストーラーファイルは削除されています。InstallerClean が削除したのではありません。プログラムがまだ必要とするファイルを削除することは決してありません。何か別のものが、InstallerClean を実行する前にこのファイルを削除しました。&#10;&#10;今は問題を引き起こしませんが、それが属するプログラムの修復、更新、またはアンインストールを試みるまでは問題になりません。その時、Windowsがこのファイルを探して見つからないため、そのステップは失敗する可能性があります。&#10;&#10;修正を試みるには、そのプログラムのインストーラーをメーカーからダウンロードし、既存のコピーに上書き実行してください (最初にアンインストールしないでください。アンインストール自体がこのファイルを必要とするステップです)。可能であればインストールされているバージョンを使用してください。Windows は異なるバージョンを拒否する可能性があります。これにより通常ファイルは復元され、設定は通常影響を受けませんが、Microsoft はそれを保証しておらず、最終手段はプログラム自体または Windows の再インストールです。`,
  'Body.RegisteredMissingFromDisk.SeeAlso': `README には、[このフォルダー]について、Microsoft自身の言葉で説明されており、ファイルの回復方法も記載されています。`,
  'Body.NoPatches': `(なし)`,
  'Reason.Orphaned': `孤立`,
  'Reason.Superseded': `置換済み`,
  'Reason.Obsoleted': `廃止`,
  'Status.Scanning': `スキャン中...`,
  'Status.Cancelling': `キャンセル中...`,
  'Status.StartingScan': `スキャンを開始しています...`,
  'Status.QueryingApi': `Windowsにインストール済みソフトウェアについて問い合わせ中...`,
  'Status.ScanningCache': `インストーラーキャッシュフォルダーをスキャン中...`,
  'Status.EnumeratingProducts': `インストール済み製品を列挙中...`,
  'Status.CheckingRegistry': `追加パッケージがないかレジストリをチェック中...`,
  'Status.RegisteredPackagesFound': `{0} 個の登録済み {1} が見つかりました。`,
  'Status.ScanComplete': `スキャン完了 ({0})`,
  'Status.FoundProducts': `ローカルパッケージをスキャン中...`,
  'Status.FoundUnused': `安全に削除できる {0} 個の {1} が見つかりました。`,
  'Status.PreparingDestination': `移動先フォルダーを準備中...`,
  'Status.Moving': `{0} 個の {1} を移動中...`,
  'Status.Deleting': `{0} 個の {1} を削除中...`,
  'Status.MoveCancelled.Partial': `移動がキャンセルされました。{1} 個中 {0} 個の {2} を処理しました。`,
  'Status.DeleteCancelled.Partial': `削除がキャンセルされました。{1} 個中 {0} 個の {2} を処理しました。`,
  'Status.MoveFailed': `移動に失敗しました ({0})。詳細は{1}をご覧ください。`,
  'Status.MoveFailed.NoLog': `移動に失敗しました ({0})。クラッシュログを書き込めませんでした。`,
  'Status.DeleteFailed': `削除に失敗しました ({0})。詳細は{1}をご覧ください。`,
  'Status.DeleteFailed.NoLog': `削除に失敗しました ({0})。クラッシュログを書き込めませんでした。`,
  'Status.ScanAccessDenied': `アクセスが拒否されました。Windows がスキャンを拒否しました。`,
  'Status.ScanFailedDb': `スキャン失敗：Windows Installer の登録情報を読み取れませんでした。`,
  'Status.ScanCancelled': `スキャンがキャンセルされました。`,
  'Status.Done': `準備完了`,
  'Status.ScanFailedDetails': `スキャンに失敗しました ({0})。詳細は {1} をご覧ください。`,
  'Status.ScanFailedDetails.NoLog': `スキャンに失敗しました ({0})。クラッシュログを書き込めませんでした。`,
  'Completion.AllClean': `すべてクリーン`,
  'Completion.NothingToCleanUp': `{InstallerFolder} にクリーンアップするものはありません`,
  'Completion.NothingToCleanUpReceipt': `{0} 個の {1} を {2} でスキャンしました`,
  'Completion.Freed': `{0} 解放`,
  'Completion.Moved': `{0} 移動`,
  // Heading for the outcome where the operation acted on no file at all, then
  // the failure count line beneath it: 0 = files that failed, 1 = files tried
  // (succeeded + failed). The count selecting the form is {0}; the noun belongs
  // to {1}, so treat the two independently.
  'Completion.NothingMoved': `何も移動されませんでした`,
  'Completion.NothingDeleted': `何も削除されませんでした`,
  'Completion.FailedCount.Singular': `{1} 個中 {0} 個のファイルを移動できませんでした。`,
  'Completion.FailedCount.Plural': `{1} 個中 {0} 個のファイルを移動できませんでした。`,
  'Completion.FailedCountDelete.Singular': `{1} 個中 {0} 個のファイルを削除できませんでした。`,
  'Completion.FailedCountDelete.Plural': `{1} 個中 {0} 個のファイルを削除できませんでした。`,
  'Completion.MoveSummary.Singular': `{0} 個の {1} を次の場所に移動しました：{2}`,
  'Completion.MoveSummary.Plural': `{0} 個の {1} を次の場所に移動しました：{2}`,
  'Completion.PermanentDeleteSummary.Singular': `{0} 個の {1} を完全に削除しました。ごみ箱には送られていません。`,
  'Completion.PermanentDeleteSummary.Plural': `{0} 個の {1} を完全に削除しました。ごみ箱には送られていません。`,
  'Summary.RegisteredStillUsed.Singular': `まだ必要なファイルが {0} 個`,
  'Summary.RegisteredStillUsed.Plural': `まだ必要なファイルが {0} 個`,
  'Summary.OrphanedToCleanUp.Singular': `クリーンアップ対象の不要ファイルが {0} 個`,
  'Summary.OrphanedToCleanUp.Plural': `クリーンアップ対象の不要ファイルが {0} 個`,
  'Summary.MissingFromDisk.Singular': `{0} 個の登録済みファイルが見つかりません (InstallerClean によって削除されたものではありません)。現時点では問題ありませんが、そのプログラムの将来の修復、更新、またはアンインストールが失敗する可能性があります。対処方法は詳細を開いてください。`,
  'Summary.MissingFromDisk.Plural': `{0} 個の登録済みファイルが見つかりません (InstallerClean によって削除されたものではありません)。現時点では問題ありませんが、それらのプログラムの将来の修復、更新、またはアンインストールが失敗する可能性があります。対処方法は詳細を開いてください。`,
  'Summary.OperationFiles': `{2} {1} 個中 {0} 個`,
  'Summary.OrphanedWindow': `孤立 {0}、置換済み {1}、廃止 {2}({3})`,
  'Summary.RegisteredWindow.Singular': `まだ必要な登録ファイルが {0} 個({1})`,
  'Summary.RegisteredWindow.Plural': `まだ必要な登録ファイルが {0} 個({1})`,
  'Confirm.MoveTitle': `{0} 個の {1} ({2})を移動しますか？`,
  'Confirm.MoveDestination': `ファイルは次の場所に移動されます：`,
  'Confirm.DeleteTitle': `{0} 個の {1} ({2})を削除しますか？`,
  'Error.AdminRequiredTitle': `アクセスが拒否されました`,
  'Error.AdminRequiredBody': `Windows が InstallerClean のアクセスを拒否したため、処理を中止しました。何も削除していません。\n\nInstallerClean はすでに管理者として実行されていたため、同じように起動し直しても解決しません。Windows は何がアクセスを拒否したのかそれ以上説明しないため、具体的に試せることはありません。`,
  'Error.InstallerDbUnavailableTitle': `Windows Installer の登録情報を読み取れませんでした`,
  'Error.ScanFailedTitle': `スキャンに失敗しました`,
  'Error.InstallerDbEmpty': `Windows Installer の登録情報が完全に空の状態で返されました。インストール済みのプログラムも更新プログラムも、キャッシュされたインストーラーファイルを一つも要求していません。正常に動作しているコンピューターでは起こらないこと (インストール直後の Windows にも該当するファイルはあります) なので、登録情報が破損しているか、読み取れなかったかのいずれかです。この答えを信じたスキャンは、{InstallerFolder} 内のすべてのファイルを誤って孤立と判定してしまいます。InstallerClean はそうせずに中止しました。何も削除していません。`,
  'Error.MsiAccessDenied': `Windows Installer が InstallerClean にインストール済みの一覧表示を許可しませんでした。InstallerClean はすでに管理者として実行されていたため、管理者として実行し直しても何も変わりません。この一覧がなければ、キャッシュされたどのファイルがまだ必要なのかを安全に判断する方法はないため、InstallerClean は中止しました。何も削除していません。`,
  'Error.MsiNonSuccess': `Windows Installer は InstallerClean に、読み取り可能なインストール済みプログラムの一覧を渡せませんでした。{0} 件の項目が連続して読み取り不能で返されました (最後のエラーコード{1})。一部しか読めていない一覧を使うのではなく、InstallerClean は中止しました。何も削除していません。`,
  'Error.InvalidDestinationTitle': `無効な移動先`,
  'Error.DestinationWriteFailedTitle': `移動先に書き込めませんでした`,
  'Error.MoveFailedTitle': `移動に失敗しました`,
  'Error.DeleteFailedTitle': `削除に失敗しました`,
  'Error.SettingNotSavedTitle': `設定の保存に失敗しました`,
  'Error.SettingNotSavedBody': `変更を保存できませんでした。次回の起動時に、InstallerClean は以前の設定に戻ります。`,
  'Error.DestinationInsideInstaller': `移動先を Windows Installer フォルダー内にすることはできません。`,
  'Error.DestinationInSystemFolder': `移動先 {0} は Windows システムフォルダー下に解決されます。%SystemRoot%、%ProgramFiles%、%ProgramData% 以外のパスを選択してください。`,
  'Error.NotEnoughSpaceTitle': `空き容量が不足しています`,
  'Error.NotEnoughSpaceBody': `{0} の空き容量が不足しています\n\n必要：{1}\n利用可能：{2}`,
  'Error.AccessDeniedDestination': `{0} に書き込む権限がありません。\nユーザープロファイル内または自分が所有するドライブ上のフォルダーを試してください。`,
  'Error.PathTooLong': `パス{0} は Windows にとって長すぎます。より短いパスを選択してください。`,
  'Error.DestinationMissing': `フォルダー {0} が存在せず、作成できませんでした。ドライブ文字またはネットワークパスを確認してください。`,
  'Error.IOWriteDestination': `Windows は {0} に書き込めません。\n詳細は{1}をご覧ください。`,
  'Error.IOWriteDestination.NoLog': `Windows は {0} に書き込めません。クラッシュログを書き込めませんでした。`,
  'Error.WriteDestination': `{0} に書き込めません。\n詳細は{1}をご覧ください。`,
  'Error.WriteDestination.NoLog': `{0} に書き込めません。クラッシュログを書き込めませんでした。`,
  'Error.MissingSourceFile': `ファイルはもう存在しません。`,
  'Error.SourceIsReparsePoint': `ソースファイルはシンボリックリンクまたはジャンクションです。安全のために拒否されました。`,
  // Singular = the sentence for ONE file, which the CLI prints after "filename: ";
  // plural = the heading the completion overlay puts over a list of filenames.
  'Error.AccessDenied.Singular': `Windows がこのファイルへのアクセスを拒否しました。ファイルはそのままにしてあります。`,
  'Error.AccessDenied.Plural': `Windows がこれらのファイルへのアクセスを拒否しました。ファイルはそのままにしてあります。`,
  'Error.FileInUse.Singular': `このファイルは別のプログラムによって開かれているか、ロックされています。そのため今はどうやっても移動できません。ファイルはそのままにしてあります。しばらくしてからもう一度お試しください。`,
  'Error.FileInUse.Plural': `これらのファイルは別のプログラムによって開かれているか、ロックされています。そのため今はどうやっても移動できません。ファイルはそのままにしてあります。しばらくしてからもう一度お試しください。`,
  'Error.IOFailure.Singular': `Windows がファイルエラーを報告しました。ファイルはそのままにしてあります。`,
  'Error.IOFailure.Plural': `Windows がファイルエラーを報告しました。これらのファイルはそのままにしてあります。`,
  'Error.UnknownError.Singular': `このファイルで何らかの問題が発生しました。ファイルはそのままにしてあります。`,
  'Error.UnknownError.Plural': `これらのファイルで何らかの問題が発生しました。ファイルはそのままにしてあります。`,
  'Error.MoveIntoInstaller': `Windows Installerフォルダー内へのファイル移動を拒否します (移動先：{0})。`,
  'Error.DestinationNotFullyQualified': `移動先には、ドライブ文字またはネットワーク共有から始まる、フォルダーへの完全なパスを指定してください (例：D:\\Backup、\\\\server\\backup)。InstallerClean はこのパスを使用できません：{0}`,
  'BrowserLaunch.FailedTitle': `ブラウザを開けませんでした`,
  'UpdateCheck.Title': `更新の確認`,
  'UpdateCheck.Status.Checking': `確認中...`,
  'UpdateCheck.Status.UpToDate': `最新です。`,
  'UpdateCheck.UpdateAvailable.Title': `更新があります`,
  'UpdateCheck.UpdateAvailable.Body': `バージョン {0} を実行しています。&#10;バージョン {1} が利用可能です。`,
  'UpdateCheck.Failed.NetworkUnavailable': `GitHub に到達できませんでした。インターネット接続を確認して再試行してください。`,
  'UpdateCheck.Failed.ServerError': `GitHub がエラーレスポンスを返しました。数分待ってから再試行してください。`,
  'UpdateCheck.Failed.ResponseParseError': `GitHub のレスポンスに認識可能なリリースが含まれていませんでした。後でもう一度試すか、リリースページを直接開いてください。`,
  'UpdateCheck.Failed.Timeout': `確認がタイムアウトしました。GitHub への接続が遅い可能性があります。再試行してください。`,
  'UpdateCheck.Failed.Unknown': `不明な理由で確認に失敗しました。報告が必要な場合は詳細が crash.log にあります。`,
  'BrowserLaunch.ClipboardOk': `InstallerClean はブラウザを開けませんでした。リンクはクリップボードにコピーしてあるので、ご自分で貼り付けられます：&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean はブラウザを開けず、リンクをクリップボードにコピーすることもできませんでした。リンクはこちらです：&#10;&#10;{0}`,
  'Error.DestinationChangedMidBatch': `ファイルの移動中に移動先が変更されました (フォルダーが置き換えられたか、リダイレクトされました)。誤った場所に書き込まないよう、InstallerClean は処理を停止しました。{0} を確認してから、再スキャンしてもう一度お試しください。`,
  'Error.CannotWriteFolder': `{0} に書き込めません。`,
  'Error.NoUniqueFilename': `10,000回の試行後も'{0}'の一意のファイル名が見つかりませんでした。`,
  'ResultLog.Sending': `送信中...`,
  'ResultLog.Sent': `ありがとうございます！レポートを送信しました。`,
  'ResultLog.Failed': `送信に失敗しました。後でもう一度試してください。`,
  'ResultLog.NothingToSend': `送信するレポートがありません。`,
  'ConfirmSendResultLog.Title': `これを送信しますか？`,
  'ConfirmSendResultLog.Reassurance': `送信先は nofaff.netlify.app/api/result-log です。あなたやあなたのマシンを特定するものは何もありません。InstallerClean が動作していることと、[どれだけの容量が解放されているか]を知るためのものです。`,
  'Automation.ResultLogPreview': `レポートのプレビュー`,
  'Startup.AlreadyRunningTitle': `InstallerClean`,
  'Startup.AlreadyRunningBody': `InstallerClean は既に実行中です。`,
  'Startup.UnhandledTitle': `InstallerClean`,
  'Startup.UnhandledBody': `予期しないエラーが発生し、InstallerClean を終了する必要があります。\n\n{0}\n\n詳細は以下に書き込まれました：\n{1}`,
  'Startup.UnhandledBody.NoLog': `予期しないエラーが発生し、InstallerClean を終了する必要があります。\n\n{0}\n\nクラッシュログを書き込めませんでした。`,
  'Startup.ErrorTitle': `起動エラー`,
  'Startup.FailedToStart': `起動に失敗しました ({0})。詳細は以下に書き込まれました：\n{1}`,
  'Startup.FailedToStart.NoLog': `起動に失敗しました ({0})。クラッシュログを書き込めませんでした。`,
  'FilePicker.ChooseDestinationTitle': `移動ファイルの移動先フォルダーを選択`,
  'Version.Display': `バージョン {0}`,
  'Plural.File.Singular': `ファイル`,
  'Plural.File.Plural': `ファイル`,
  'Plural.Error.Singular': `エラー`,
  'Plural.Error.Plural': `エラー`,
  'Plural.Package.Singular': `パッケージ`,
  'Plural.Package.Plural': `パッケージ`,
  'Plural.Product.Singular': `製品`,
  'Plural.Product.Plural': `製品`,
  'Plural.Patch.Singular': `パッチ`,
  'Plural.Patch.Plural': `パッチ`,
  'Display.Size.GB': `{0:F2} GB`,
  'Display.Size.MB': `{0:F1} MB`,
  'Display.Size.KB': `{0:F1} KB`,
  'Display.Size.B': `{0} B`,
  'Display.Elapsed.Ms': `{0:F0}ms`,
  'Display.Elapsed.S': `{0:F1}s`,
  'Display.ElapsedLong.LessThanASecond': `1秒未満`,
  'Display.ElapsedLong.Seconds': `{0:F1}秒`,
  'Cli.UnknownArgument': `不明な引数：'{0}'`,
  'Cli.Cancelling': `キャンセル中...`,
  'Cli.Cancelled': `キャンセルされました。`,
  'Cli.GenericError': `エラー：{0}。詳細は {1} に書き込まれました。`,
  'Cli.GenericError.NoLog': `エラー：{0}。クラッシュログを書き込めませんでした。`,
  'Cli.ScanningInstaller': `{InstallerFolder} をスキャン中...`,
  'Cli.FoundOrphans': `クリーンアップ対象の {0} 個の {1} が見つかりました ({2})。`,
  'Cli.NothingToDo': `実行するものはありません。`,
  'Cli.EventLogScanNoOrphans': `スキャンモード ({0})：不要ファイルなし。インストーラーデータベースには {1} 個の登録済み {2} があります。`,
  'Cli.EventLogScanFound': `スキャンモード ({0})：{1} 個の不要な {2} が見つかりました ({3})。アクションは実行されていません。`,
  'Cli.DeletingFiles': `{0} 個の {1} を削除中...`,
  'Cli.DeletedFiles': `{0} 個の {1} を削除しました。`,
  'Cli.EventLogDeleteSummary': `{0}モード：{2} 個中 {1} 個の {3} をごみ箱に送信、{4}を解放、{5}個の{6}。`,
  'Cli.NoMoveDestination': `エラー：移動先が指定されていません。/m PATH を使用してください (GUI で設定したデフォルトはユーザーごとのもので、スケジュール実行やサービスアカウントでの実行には適用されません)。`,
  'Cli.EventLogMoveNoDestination': `{0}モードは中止されました：移動先が指定されていません。`,
  'Cli.MoveDestinationInsideInstaller': `エラー：移動先を Windows Installer フォルダー内にすることはできません。`,
  'Cli.MoveDestinationRelative': `エラー：移動先は完全修飾パスである必要があります。指定されたもの：{0}`,
  'Cli.MoveDestinationInSystemFolder': `エラー：移動先 {0} は Windows システムフォルダー下に解決されます。%SystemRoot%、%ProgramFiles%、%ProgramData%以外のパスを選択してください。`,
  'Cli.EventLogMoveDestinationInsideInstaller': `{0}モードは中止されました：移動先 {1} はC:\\Windows\\Installer 内にあります。`,
  'Cli.EventLogMoveDestinationRelative': `{0}モードは中止されました：移動先 {1} は完全修飾パスではありません。`,
  'Cli.EventLogMoveDestinationInSystemFolder': `{0}モードは中止されました：移動先 {1} は Windows システムフォルダー内に解決されます。`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `エラー：現在 Windows Installer を使用しているものがあります。通常は Windows Update またはバックグラウンドでインストール中のプログラムです。その実行中は移動と削除がブロックされます。完了したら再試行してください。`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `エラー：このマシンで以前の Windows Installer トランザクションが中断されています。キャッシュをクリーンアップする前に、そのインストールを再開またはロールバックするか (または Windows を再起動してください)。`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `エラー：キューに入れられた再起動後のファイル操作がインストーラーキャッシュ ({0})を対象としています。クリーンアップ前にその操作を完了するために Windows を再起動してください。`,
  'Cli.EventLogReason.MsiExecuteMutex': `Windows Installer ミューテックスが保持されています`,
  'Cli.EventLogReason.InstallerInProgress': `インストーラートランザクションが進行中`,
  'Cli.EventLogReason.PendingRenameInCache': `キューに入れられた再起動後のファイル名変更がインストーラーキャッシュを対象としています`,
  'Cli.EventLogPendingRebootBlocked': `{0}モードは中止されました：保留中の再起動が検出されました。理由：{1}。{2}`,
  'Cli.MovingFiles': `{0} 個の {1} を {2} に移動中...`,
  'Cli.MovedFiles': `{0} 個の {1} を移動しました。`,
  'Cli.EventLogMoveSummary': `{0}モード：{2} 個中 {1} 個の {3} を {4} に移動、{5} を移動、{6} 個の {7}。`,
  'Cli.EventLogCancelledPartial': `{0}モードが Ctrl+C で中断されました：キャンセル前に {2} 個中 {1} 個の {3} を処理しました。ファイルごとの詳細は進捗出力を参照してください。`,
  'Cli.EventLogCancelledNoWork': `{0}モードが作業実行前に Ctrl+C で中断されました。アクションは実行されていません。`,
  'Cli.MutexBlocked': `別の InstallerClean プロセスが単一インスタンスロックを保持しています (GUIまたは別のCLI実行)。終了コード75 (一時的)。後で再試行しても安全です。`,
  'Cli.EventLogMutexBlocked': `{0}モードはスキップされました：GUI または別の CLI 実行が既に単一インスタンスミューテックスを保持しています。`,
  'Cli.EventLogBadArguments': `実行が中止されました：認識できない、または不正な引数'{0}'。アクションは実行されていません。`,
  'Cli.EventLogNoArguments': `実行が中止されました：引数が指定されていません。アクションは実行されていません。`,
  'Cli.EventLogValidationFailed': `{0}モードが失敗しました：{1}`,
  'Cli.EventLogHardError': `{0}モードが失敗しました：{1}。詳細は {2} をご覧ください。`,
  'Cli.EventLogHardError.NoLog': `{0}モードが失敗しました：{1}。クラッシュログを書き込めませんでした。`,
  'Cli.EventLogUnavailable': `注意：イベントログの書き込みに失敗しました。Application チャネルのアクセス許可またはグループポリシーを確認してください。`,
  'CrashLog.PrivacyHeader': `# crash.log は InstallerClean からの未処理例外をキャプチャします。\n# 昇格時には、フレームワークの例外メッセージに実行中のセッションの\n# ファイルパス (Windows Installerクエリによって列挙された他のユーザーの\n# プロファイルを含む)が含まれる場合があります。\n# 更新チェックまたは結果ログPOSTからのネットワーク障害メッセージには、\n# 宛先URLおよび解決されたIP/プロキシアドレスが含まれる場合があります。\n# このファイルを公開バグレポートに添付する前に、両方のクラスの詳細を\n# 編集してください。\n`,
  'Cli.Help.Header': `InstallerClean - {InstallerFolder} をクリーンアップ`,
  'Cli.Help.Usage': `使用方法：`,
  'Cli.Help.Help': `  installerclean-cli --help     このヘルプを表示 (/?、-hも受け付けます)`,
  'Cli.Help.Version': `  installerclean-cli --version  バージョンを表示 (-vも受け付けます)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s         スキャンのみ - 削除可能なファイルの一覧`,
  'Cli.Help.Delete': `  installerclean-cli /d         削除可能なファイルを削除 (ごみ箱)`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m         保存されたデフォルトの場所に移動`,
  'Cli.Help.MovePath': `  installerclean-cli /m PATH    指定されたパスに移動`,
  'Cli.Help.NoteLine1': `installerclean-cliは実際のコンソールプロセスであり、完了するまで`,
  'Cli.Help.NoteLine2': `プロンプトをブロックします。他のコンソール exe と同様に出力をリダイレクト`,
  'Cli.Help.NoteLine3': `またはパイプできます。GUI は同じ場所にある InstallerClean.exe にあります。`,
  'Cli.Help.ExitCodesHeader': `終了コード：`,
  'Cli.Help.ExitCodeOk': `  0   成功：フラグが立てられたすべてのファイルが処理されました`,
  'Cli.Help.ExitCodeError': `  1   失敗：処理なし (不正な引数、スキャン失敗、全ファイル失敗)`,
  'Cli.Help.ExitCodePartial': `  2   部分完了：一部のファイルは処理され、一部は失敗しました`,
  'Cli.Help.ExitCodeTransient': `  75  一時的：一時的な状態が実行をブロックしました (メッセージを参照)`,
  'Cli.Help.ExitCodeCancelled': `  130 キャンセル (Ctrl+C)`,
  'Tooltip.ChangeLanguage': `言語を変更します。プログラムが再起動します。`,
  'Automation.ChangeLanguage': `言語を変更`,
  'Automation.ChangeLanguage.HelpText': `プログラムが再起動します。`,
  'Body.NotScanned.Lead': `まだ何もスキャンしていません。`,
  'Body.NotScanned.Why': `「再スキャン」を押すと、{InstallerFolder} を調べて、どのプログラムも必要としなくなったインストーラーファイルを探します。`,
  'Confirm.MoveSameDrive': `このフォルダーは同じドライブ上にあるため、移動しただけでは空き容量は増えません。移動先からファイルを削除すれば、その分の容量が空きます。または、別のドライブのフォルダーを選ぶこともできます。`,
  'Error.ScanCorrelationFailed': `InstallerClean は今回のスキャン結果を Windows Installer の登録情報と突き合わせられませんでした。Windows がまだ必要として挙げているファイルはすべて {InstallerFolder} に見当たらず、一方でフォルダーに実際にあるファイルはどの登録情報とも一致しません。実在するコンピューターがこのような状態になることはないため、これは安全に削除できるファイルがあるということではなく、登録情報の読み取りに問題があることを示しています。クリーンアップ対象は何も表示しておらず、何も削除していません。`,
  'Error.CandidateOutsideCache': `このファイルは Windows Installer フォルダーの直下にありません。安全のために拒否されました。`,
  'Completion.ReverifySkipped': `{0} 個の {1} はそのまま残しました。スキャン後にプログラムが再び必要としたためです。`,
  'Completion.MoveCancelledSummary': `キャンセルするまでに、{1} 個中 {0} 個の {2} を移動しました。`,
  'Completion.PermanentDeleteCancelledSummary': `キャンセルするまでに、{1} 個中 {0} 個の {2} を完全に削除しました。`,
  'Body.PendingReboot.Lead': `これらのファイルは今はクリーンアップできません。`,
  'Cli.TooManyArguments': `エラー：予期しない余分な引数 '{0}' があります。移動先フォルダーにスペースが含まれる場合は、パス全体を引用符で囲んでください：/m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `保存先はユーザー別です。スケジュール実行や SYSTEM は /m PATH が必要です。`,
  'Completion.ReverifyIncomplete': `{0} 個の {1} はそのまま残しました。確認をやり直した際に Windows Installer の登録情報を完全に読み取れなかったためです。`,
  'Summary.ProgramsUnreadable.Singular': `今回のスキャンでインストール済みプログラム {0} 個を読み取れなかったため、置換済みのパッチは残してあります。孤立ファイルには影響しません。`,
  'Summary.ProgramsUnreadable.Plural': `今回のスキャンでインストール済みプログラム {0} 個を読み取れなかったため、置換済みのパッチは残してあります。孤立ファイルには影響しません。`,
  'Error.ScanRecordsUnreadable': `InstallerClean は、何がまだ必要かを確かめられるだけの Windows Installer の登録情報を読み取れませんでした。インストール済みプログラムの一覧が不足した状態で返され、同じ登録情報をレジストリから直接読み取る方法でもエラーが発生しました。あるファイルを指し示す登録情報が読み取れなかったものの一つだったというだけで、そのファイルが孤立しているように見えてしまうことがあるため、InstallerClean は中止しました。何も削除していません。`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer がインストール済みプログラムの一覧の終わりを最後まで知らせませんでした。InstallerClean は {0} 件で打ち切りました (最後のエラーコード{1})。終わりのない一覧は信頼できないため、InstallerClean は中止しました。何も削除していません。`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer があるプログラムのパッチ一覧の終わりを最後まで知らせませんでした。InstallerClean は {0} 件で打ち切りました (最後のエラーコード{1})。終わりのない一覧は信頼できないため、InstallerClean は中止しました。何も削除していません。`,
  'UpdateCheck.Status.UpdateAvailable': `バージョン {0} が利用可能です。`,
  'Completion.DonateAsk': `お役に立てて何よりです。お心づけをいただけたら幸いです。`,
  'About.Link.Guide': `ガイドとよくある質問`,
  'About.Link.ReportProblem': `問題を報告`,
  'About.AutoUpdateCheck': `更新を自動的に確認する`,
  'Automation.About.Guide.HelpText': `ブラウザで github の readme を開きます。`,
  'Automation.About.ReportProblem.HelpText': `ブラウザで github.com の Issue トラッカーを開きます。`,
  'Automation.AutoUpdateCheck.HelpText': `チェックを入れると、InstallerClean は起動時に github で新しいバージョンを確認します。`,
};

let text = readFileSync(BASE, 'utf8');

// Replace each key's inner <value> from MAP. No machine-key removal: ja keeps
// the full set, so every neutral key (machine Cli included) is translated here.
const escRe = (s) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
const notApplied = [];
for (const [key, val] of Object.entries(MAP)) {
  const re = new RegExp('(<data name="' + escRe(key) + '"[^>]*>\\s*<value>)([\\s\\S]*?)(</value>)');
  let applied = false;
  text = text.replace(re, (m, p1, p2, p3) => { applied = true; return p1 + val + p3; });
  if (!applied) notApplied.push(key);
}

text = text.replace(/\r\n/g, '\n');
if (!text.endsWith('\n')) text += '\n';
writeFileSync(OUT, text, 'utf8');

// ---------------- self-check the written file against the neutral ----------------
const isMachineCliKey = (k) =>
  k.startsWith('Cli.') && k.includes('EventLog') && k !== 'Cli.EventLogUnavailable';
const placeholders = (s) => new Set([...s.matchAll(/\{(\d+)(?::[^}]*)?\}/g)].map((p) => p[1]));
const parse = (xml) => {
  const map = new Map();
  const re = /<data\s+name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)<\/value>/g;
  let m;
  while ((m = re.exec(xml)) !== null) map.set(m[1], m[2]);
  return map;
};
const neutral = parse(readFileSync(BASE, 'utf8'));
const written = readFileSync(OUT, 'utf8');
const output = parse(written);
// ja ships every neutral key, machine Cli included.
const neutralRequired = [...neutral.keys()];

const missingFromMap = neutralRequired.filter((k) => !(k in MAP));
const strayMapKeys = Object.keys(MAP).filter((k) => !neutral.has(k));
const missingFromOutput = neutralRequired.filter((k) => !output.has(k));
const arityMismatch = neutralRequired.filter((k) => {
  if (!output.has(k)) return false;
  const a = placeholders(neutral.get(k)), b = placeholders(output.get(k));
  return a.size !== b.size || [...a].some((i) => !b.has(i));
});
const crlf = (written.match(/\r/g) || []).length;

const alsoKeep = new Set(ALSO_KEEP);
const untranslated = neutralRequired.filter((k) =>
  output.has(k) && output.get(k) === neutral.get(k) && !KEEP_ENGLISH.has(k) && !alsoKeep.has(k));

// Breakdown computed, never pinned: the non-Cli and human-Cli totals both grow with
// every string the app gains, and a hardcoded pair goes stale silently while the
// checked figure beside it stays right.
const nonCliRequired = neutralRequired.filter((k) => !k.startsWith('Cli.')).length;
console.log('translatable <data> in output:', output.size,
  '(expect', neutralRequired.length,
  '=', nonCliRequired, 'non-Cli +', neutralRequired.length - nonCliRequired, 'Cli)');
console.log('MAP entries:', Object.keys(MAP).length, '| CRLF:', crlf, '(expect 0)');

if (alsoKeep.size) {
  console.log('ALSO_KEEP (' + alsoKeep.size + '), kept identical to English:');
  for (const k of alsoKeep) console.log('   ' + k + ' = ' + JSON.stringify(output.get(k)));
}
if (notApplied.length) console.log('!! value not applied (regex miss):', notApplied);
if (missingFromMap.length) console.log('!! in neutral but missing from MAP:', missingFromMap);
if (strayMapKeys.length) console.log('!! in MAP but not in neutral:', strayMapKeys);
if (missingFromOutput.length) console.log('!! required key missing from output:', missingFromOutput);
if (arityMismatch.length) console.log('!! placeholder arity differs from neutral:', arityMismatch);
if (untranslated.length) console.log('!! still English (untranslated), ' + untranslated.length + ': ' + untranslated.slice(0, 40).join(', '));

const structuralOk = !notApplied.length && !missingFromMap.length && !strayMapKeys.length &&
  !missingFromOutput.length && !arityMismatch.length &&
  output.size === neutralRequired.length && crlf === 0;
const ok = structuralOk && !untranslated.length;
console.log(ok ? '\nGENERATION OK' : '\nGENERATION HAS ISSUES (see above)');
