#!/usr/bin/env node
// Japanese (ja) satellite generator for InstallerClean.
//
// PROVENANCE: the Japanese translation was written from scratch by the native
// speaker coolvitto (PR #41), a contributor's gift and the strongest provenance
// of any satellite. This generator's MAP holds coolvitto's wording verbatim;
// regenerating reproduces Strings.ja.resx. Treat the MAP values as the native
// author's work, not a machine draft.
//
// Structure: UNLIKE the other satellites, ja carries the machine-contract
// Cli.EventLog* keys, which coolvitto translated along with everything else and
// which the other fourteen omit. They are forced to English at the emit site at
// runtime (MachineContract.cs), so a translated copy is inert rather than wrong,
// and keeping his work costs nothing. Japanese has no count plurals, so there
// are no satellite-only overrides.
//
// The file is a MIXTURE on purpose and must not be harmonised in either
// direction: most machine keys stay because they are a contributor's
// translation, and the named few in STRIPPED below go because they are not.
// Count that set there rather than trusting a figure here, which goes stale the
// next time one earns its place. Two ways a key earns it. Some outlived their
// English: Cli.EventLogDeleteSummary sat here saying the files had been sent to
// the Recycle Bin after the bin went, and Cli.EventLogScanNoOrphans said
// "Scan mode" after the entry stopped calling every run a scan. Both are
// fossils no user can reach and no gate can measure, the still-English gate
// skipping machine keys by contract and nothing else comparing them. The rest
// arrived after his PR and sit in English inside a Japanese file; translating
// them would buy correct Japanese that can never be read. Any machine key that
// outlives its English or postdates his work joins them.
import { readFileSync, writeFileSync } from 'node:fs';

const dir = 'src/InstallerClean.Core/Resources';
const BASE = `${dir}/Strings.resx`;            // English source (the "neutral")
const OUT = `${dir}/Strings.ja.resx`;

// Universal keeps: keys whose value is the same in every language, the brand names
// and the pure-placeholder announcement string. Their still-English value is NOT a
// miss. Explicit by KEY on purpose: a future brand key then defaults to "flag until
// someone adds it here", never silently passes. Do NOT translate these values. Do
// NOT edit this list per language.
//
// The four size suffixes and the two elapsed suffixes were in this list until
// 2026-08-26 and do not belong in it, because they are not universal: French writes
// Go/Mo/Ko/o, Russian and Ukrainian write ГБ/МБ/КБ/Б and мс/с. Those three carry real
// values in their MAP; the languages that do abbreviate as English does keep them in
// ALSO_KEEP, which is the per-language list. Display.ListSeparator is the same shape,
// for the same reason.
const KEEP_ENGLISH = new Set([
  'Window.Main.Title',
  'Startup.AlreadyRunningTitle',
  'Startup.UnhandledTitle',
  'Automation.ScanResultAnnouncement',
]);

// Per-language keeps: Japanese values byte-identical to English (genuine
// single-token matches, not misses). The self-check prints these so the keep
// stays honest.
const ALSO_KEEP = [
  // The size and elapsed unit suffixes. Japanese abbreviates them exactly as
  // English does, so there is nothing to translate and nothing to get wrong.
  // A per-language keep rather than a universal one because fr, ru and uk do
  // NOT: French takes Go/Mo/Ko/o, Russian and Ukrainian take ГБ/МБ/КБ/Б and
  // мс/с, and all three carry real values in their MAP.
  'Display.Size.GB',           // {0:F2} GB
  'Display.Size.MB',           // {0:F1} MB
  'Display.Size.KB',           // {0:F1} KB
  'Display.Size.B',            // {0} B
  'Display.Elapsed.Ms',        // {0:F0}ms
  'Display.Elapsed.S',         // {0:F1}s

  // The Application event log is written in English whatever the interface
  // language is, so this label stays as it is.
  'Cli.EventLogReason.PendingRenameUnresolved',
];

const MAP = {
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `InstallerClean について`,
  'Window.Registered.Title': `そのままにしたファイル`,
  'Window.Orphaned.Title': `削除しても安全な不要ファイル`,
  // Section.Registered.Products and Automation.Section.Products are deliberately
  // not here. They left the neutral resx when the registered-files window stopped
  // having a products group of its own, so a value for either would be
  // regenerated into a key the app cannot use.
  'Section.Registered.Patches': `パッチ`,
  'Section.Registered.Details': `製品詳細`,
  'Section.Backup.Folder': `バックアップフォルダー`,
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
  'Action.BackupFolderPlaceholder': `削除ではなく移動する場合のフォルダーのパス。`,
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
  'Automation.ConfirmDelete': `完全に削除すると不要ファイルが取り除かれます。キャンセルすると何も削除せずに閉じます。`,
  'Automation.ConfirmMove': `移動を実行すると不要ファイルが選択した移動先フォルダーに移動されます。キャンセルはそのままの場所に残します。`,
  'Automation.SayThanks': `謝意を伝える`,
  'Automation.ConfirmSendResultLog': `送信を実行すると表示されたレポートが No Faff に投稿されます。キャンセルは何も送信しません。`,
  'Automation.CheckForUpdates': `更新の確認`,
  'Automation.CheckForUpdates.HelpText': `github のリリースページで新しいバージョンがあるかどうかを確認します。`,
  'Automation.UpdateAvailable.HelpText': `リリースページを開いて新しいバージョンをダウンロードするか、キャンセルして現在のバージョンを維持します。`,
  'Automation.Licence.HelpText': `ブラウザで github.com のライセンスファイルを開きます。`,
  'Automation.Section.BackupFolder': `バックアップフォルダー`,
  'Automation.Section.Patches': `パッチ`,
  'Automation.Section.ProductDetails': `製品詳細`,
  'Automation.BackupFolder': `バックアップフォルダー`,
  'Automation.OperationProgress': `操作の進捗`,
  'Automation.RescanInstaller': `{InstallerFolder} を再スキャン`,
  'Automation.ScanningProgress': `スキャンの進捗`,
  'Automation.StartupScanProgress': `起動時スキャンの進捗`,
  'Automation.ViewOrphanedFiles': `詳細、不要ファイル`,
  'Automation.ViewOrphanedFiles.HelpText': `クリーンアップ可能です。`,
  'Automation.ViewRegisteredFiles': `詳細、そのままにしたファイル`,
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
  'Tooltip.Move': `不要ファイルをバックアップフォルダーへ移します。`,
  'Tooltip.MoveNeedsDestination': `不要ファイルをバックアップフォルダーへ移します。フォルダーはこの後で選びます。`,
  'Tooltip.Delete': `不要ファイルを完全に削除します。問題ないか自分で確かめたい場合は、代わりに「移動」を使ってください。`,
  'Tooltip.SigningCertificate': `埋め込まれた Authenticode 証明書のサブジェクト名です。チェーン検証はされていません。`,
  'Body.MainExplanation.Lead': `下にある不要ファイルはいずれも[安全に削除できます]。`,
  'Body.MainExplanation.Why': `これらは {InstallerFolder} にあります。InstallerClean はインストール済みのすべてのプログラムについて Windows に問い合わせます。どのプログラムもそのファイルを自分のものだと示さない場合({0})、または新しいパッチが置き換えていてどのプログラムもそこへ戻れない場合({1})に、そのファイルが一覧に載ります。`,
  'Body.MainExplanation.Action': `選んだバックアップフォルダーへ移動し、プログラムがこれまでどおり更新およびアンインストールできると納得できたら、そのフォルダーを削除してください。{InstallerFolder} に戻せばすべて元どおりになります。または、今すぐ完全に削除することもできます。`,
  'Body.PendingReboot.MsiExecuteMutex': `現在、Windows Update やバックグラウンドでインストール中のプログラムなど、何かが Windows Installer を使用しています。その間は移動と削除が一時停止し、InstallerClean は変更中の {InstallerFolder} に触れません。終わったら再スキャンすれば、どちらも使えるようになります。`,
  'Body.PendingReboot.InstallerInProgress': `このコンピューターには、中断されたままの以前の Windows Installer トランザクションがあります。{InstallerFolder} をクリーンアップする前に、そのインストールを再開するかロールバックしてください(または Windows を再起動してください)。`,
  'Body.PendingReboot.PendingRenameInCache': `Windows は次回の再起動時に実行するファイル名の変更をキューに入れており、それが {InstallerFolder} に影響します。クリーンアップする前に Windows を再起動してください。`,
  'Body.NoFileSelected': `ファイルを選択して詳細を表示します。`,
  'Body.NoProductSelected': `製品を選択して詳細を表示します。`,
  'Body.NoMetadata': `メタデータはありません。`,
  'Body.RegisteredMissingFromDisk': `このインストーラーファイルが見つかりません。今すぐ困ることはなく、そのファイルが属するプログラムを更新またはアンインストールしようとする日まで問題は起きません。その際、Windows がこのファイルを探しても見つからないため、処理が失敗することがあります。\n\n元に戻すには、今お使いのバージョンのインストーラーが必要です。プログラムの提供元から入手し、既存のインストールに上書きして実行してください。新しいバージョンでは代用できません。新しいバージョンはまず今のものを削除する必要があり、その手順こそがこのファイルを必要とするからです。先にアンインストールする方法も、同じ理由でうまくいきません。これでファイルが復元され、設定はそのまま残るはずですが、Microsoft が保証しているわけではありません。`,
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
  'Status.Moving': `不要ファイルを移動しています...`,
  'Status.Deleting': `不要ファイルを削除しています...`,
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
  'Completion.PermanentDeleteSummary.Singular': `{0} 個の {1} を完全に削除しました`,
  'Completion.PermanentDeleteSummary.Plural': `{0} 個の {1} を完全に削除しました`,
  'Summary.RegisteredStillUsed.Singular': `{0} 個のファイルをそのままにしました`,
  'Summary.RegisteredStillUsed.Plural': `{0} 個のファイルをそのままにしました`,
  'Summary.OrphanedToCleanUp.Singular': `クリーンアップ対象の不要ファイルが {0} 個`,
  'Summary.OrphanedToCleanUp.Plural': `クリーンアップ対象の不要ファイルが {0} 個`,
  'Summary.NothingListed.Singular': `この PC では、InstallerClean はキャッシュ内のどのファイルがここにインストールされたプログラムのものかを確実には判断できなかったため、唯一のファイルを一覧に載せずそのままにしました。`,
  'Summary.NothingListed.Plural': `この PC では、InstallerClean はキャッシュ内のどのファイルがここにインストールされたプログラムのものかを確実には判断できなかったため、{0} 個の {1} を一覧に載せずそのままにしました。`,
  'Summary.MissingFromDisk.Singular': `Windows には、{InstallerFolder} にない {0} 件のファイルの記録があります: {1}。普段の使用では問題ありませんが、そのプログラムの更新やアンインストールが失敗することがあります。対処方法は「詳細」を開いてください。`,
  'Summary.MissingFromDisk.Plural': `Windows には、{InstallerFolder} にない {0} 件のファイルの記録があります: {1}。普段の使用では問題ありませんが、それらのプログラムの更新やアンインストールが失敗することがあります。対処方法は「詳細」を開いてください。`,
  'Summary.MissingFromDisk.OtherPrograms.Singular': `他に {0} 個のプログラム`,
  'Summary.MissingFromDisk.OtherPrograms.Plural': `他に {0} 個のプログラム`,
  'Summary.MissingFromDisk.Unnamed.Singular': `登録情報にプログラム名がない {0} 個のファイル`,
  'Summary.MissingFromDisk.Unnamed.Plural': `登録情報にプログラム名がない {0} 個のファイル`,
  'Summary.OperationFiles': `{2} {1} 個中 {0} 個`,
  'Summary.OrphanedWindow': `{0} 個の不要な {1} ({2})`,
  'Summary.RegisteredWindow.Singular': `{0} 個のファイルをそのままにしました ({1})`,
  'Summary.RegisteredWindow.Plural': `{0} 個のファイルをそのままにしました ({1})`,
  'Confirm.MoveTitle': `{0} 個の {1} ({2})を移動しますか？`,
  'Confirm.DeleteTitle': `{0} 個の {1} ({2})を削除しますか？`,
  'Error.AdminRequiredTitle': `アクセスが拒否されました`,
  'Error.AdminRequiredBody': `Windows が InstallerClean のアクセスを拒否したため、処理を中止しました。何も削除していません。\n\nInstallerClean はすでに管理者として実行されていたため、同じように起動し直しても解決しません。Windows は何がアクセスを拒否したのかそれ以上説明しないため、具体的に試せることはありません。`,
  'Error.InstallerDbUnavailableTitle': `Windows Installer の登録情報を読み取れませんでした`,
  'Error.ScanFailedTitle': `スキャンに失敗しました`,
  'Error.InstallerDbEmpty': `Windows Installer の登録情報が完全に空の状態で返されました。インストール済みのプログラムも更新プログラムも、キャッシュされたインストーラーファイルを一つも要求していません。正常に動作しているコンピューターでは起こらないこと (インストール直後の Windows にも該当するファイルはあります) なので、登録情報が破損しているか、読み取れなかったかのいずれかです。この答えを信じたスキャンは、{InstallerFolder} 内のすべてのファイルを誤って孤立と判定してしまいます。InstallerClean はそうせずに中止しました。何も削除していません。`,
  'Error.MsiAccessDenied': `Windows Installer が InstallerClean にインストール済みの一覧表示を許可しませんでした。InstallerClean はすでに管理者として実行されていたため、管理者として実行し直しても何も変わりません。この一覧がなければ、キャッシュされたどのファイルがまだ必要なのかを安全に判断する方法はないため、InstallerClean は中止しました。何も削除していません。`,
  'Error.MsiNonSuccess': `Windows Installer は InstallerClean に、読み取り可能なインストール済みプログラムの一覧を渡せませんでした。{2} {3} を読み取ったのち、{0} 件の項目が連続して読み取り不能で返されました (最後のエラーコード{1})。一部しか読めていない一覧を使うのではなく、InstallerClean は中止しました。何も削除していません。`,
  'Error.InvalidDestinationTitle': `無効な移動先`,
  'Error.DestinationWriteFailedTitle': `移動先に書き込めませんでした`,
  'Error.MoveFailedTitle': `移動に失敗しました`,
  'Error.DeleteFailedTitle': `削除に失敗しました`,
  'Error.SettingNotSavedTitle': `設定の保存に失敗しました`,
  'Error.SettingNotSavedBody': `変更を保存できませんでした。次回の起動時に、InstallerClean は以前の設定に戻ります。`,
  'Error.DestinationInsideInstaller': `移動先を Windows Installer フォルダー内にすることはできません。`,
  'Error.DestinationInSystemFolder': `移動先 {0} は Windows のシステムフォルダー配下に解決されます。%SystemRoot%、%ProgramFiles%、%ProgramFiles(x86)%、%ProgramData% の外にあるパスを選んでください。`,
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
  'Error.FileInUse.Singular': `このファイルは別のプログラムによって開かれているかロックされているため、今は取り除けません。そのままにしてあります。後でもう一度お試しください。`,
  'Error.FileInUse.Plural': `これらのファイルは別のプログラムによって開かれているかロックされているため、今は取り除けません。そのままにしてあります。後でもう一度お試しください。`,
  'Error.IOFailure.Singular': `Windows がファイルエラーを報告しました。ファイルはそのままにしてあります。`,
  'Error.IOFailure.Plural': `Windows がファイルエラーを報告しました。これらのファイルはそのままにしてあります。`,
  'Error.UnknownError.Singular': `このファイルで何らかの問題が発生しました。ファイルはそのままにしてあります。`,
  'Error.UnknownError.Plural': `これらのファイルで何らかの問題が発生しました。ファイルはそのままにしてあります。`,
  'Error.MoveIntoInstaller': `Windows Installerフォルダー内へのファイル移動を拒否します (移動先：{0})。`,
  'Error.DestinationNotFullyQualified': `バックアップフォルダーは、ドライブ文字またはネットワーク共有で始まる、フォルダーへの完全なパスである必要があります(例：D:\\Backup、または \\\\server\\backup)。InstallerClean はこれを使えません：{0}`,
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
  'Error.DestinationChangedMidBatch': `InstallerClean はバックアップフォルダーを確認できなくなったため、誤った場所に書き込まずに停止しました。{0} を確認してから、再スキャンしてもう一度お試しください。`,
  'Error.CannotWriteFolder': `{0} に書き込めません。`,
  'Error.DestinationCollision': `'{0}' という名前のファイルはすでにバックアップフォルダーにあります。`,
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
  'Display.ListSeparator': `、`,
  'Display.ElapsedLong.LessThanASecond': `1秒未満`,
  'Display.ElapsedLong.Seconds': `{0:F1}秒`,
  'Cli.UnknownArgument': `エラー：不明な引数 '{0}'`,
  'Cli.Cancelling': `キャンセル中...`,
  'Cli.Cancelled': `キャンセルされました。`,
  'Cli.GenericError': `エラー：予期しない障害 ({0})。詳細を {1} に書き込みました。`,
  'Cli.GenericError.NoLog': `エラー：予期しない障害 ({0})。クラッシュログを書き込めませんでした。`,
  'Cli.ScanningInstaller': `{InstallerFolder} をスキャン中...`,
  'Cli.FoundOrphans': `クリーンアップ対象の不要な {1} が {0} 個見つかりました ({2})。`,
  'Cli.EventLogScanFound': `スキャンモード ({0})：{1} 個の不要な {2} が見つかりました ({3})。アクションは実行されていません。`,
  'Cli.DeletingFiles': `{0} 個の不要な {1} を削除しています...`,
  'Cli.DeletedFiles': `{0} 個の不要な {1} を完全に削除しました。`,
  'Cli.NoMoveDestination': `エラー：移動先が指定されていません。/m PATH を使用してください (GUI で設定したデフォルトはユーザーごとのもので、スケジュール実行やサービスアカウントでの実行には適用されません)。`,
  'Cli.EventLogMoveNoDestination': `{0}モードは中止されました：移動先が指定されていません。`,
  'Cli.MoveDestinationInsideInstaller': `エラー：移動先を Windows Installer フォルダー内にすることはできません。`,
  'Cli.MoveDestinationRelative': `エラー：移動先は完全修飾パスである必要があります。指定されたもの：{0}`,
  'Cli.MoveDestinationInSystemFolder': `エラー：移動先 {0} は Windows のシステムフォルダー配下に解決されます。%SystemRoot%、%ProgramFiles%、%ProgramFiles(x86)%、%ProgramData% の外にあるパスを選んでください。`,
  'Cli.EventLogMoveDestinationInsideInstaller': `{0}モードは中止されました：移動先 {1} はC:\\Windows\\Installer 内にあります。`,
  'Cli.EventLogMoveDestinationRelative': `{0}モードは中止されました：移動先 {1} は完全修飾パスではありません。`,
  'Cli.EventLogMoveDestinationInSystemFolder': `{0}モードは中止されました：移動先 {1} は Windows システムフォルダー内に解決されます。`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `エラー：現在、Windows Update やバックグラウンドでインストール中のプログラムなど、何かが Windows Installer を使用しています。その間 /m と /d はブロックされます。終わってからもう一度お試しください。`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `エラー：このコンピューターには、中断されたままの以前の Windows Installer トランザクションがあります。{InstallerFolder} をクリーンアップする前に、そのインストールを再開するかロールバックしてください(または Windows を再起動してください)。`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `エラー：再起動後に実行するためキューに入れられたファイル操作が {InstallerFolder} を対象にしています ({0})。クリーンアップする前に Windows を再起動して、その操作を完了させてください。`,
  'Cli.EventLogReason.MsiExecuteMutex': `Windows Installer ミューテックスが保持されています`,
  'Cli.EventLogReason.InstallerInProgress': `インストーラートランザクションが進行中`,
  'Cli.EventLogReason.PendingRenameInCache': `キューに入れられた再起動後のファイル名変更がインストーラーキャッシュを対象としています`,
  'Cli.EventLogPendingRebootBlocked': `{0}モードは中止されました：保留中の再起動が検出されました。理由：{1}。{2}`,
  'Cli.MovingFiles': `{0} 個の不要な {1} を {2} へ移動しています...`,
  'Cli.MovedFiles': `{0} 個の不要な {1} を移動しました。`,
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
  'Cli.EventLogUnavailable': `注意：イベントログの書き込みに失敗しました。Application ログのアクセス許可またはグループポリシーを確認してください。`,
  'CrashLog.PrivacyHeader': `# crash.log には InstallerClean の未処理例外が記録されます。\n# 昇格した状態では、フレームワークの例外メッセージに実行中セッションの\n# ファイルパスが含まれることがあります(Windows Installer のクエリが\n# 列挙した他のユーザーのプロファイルを含む)。更新確認や結果ログの送信で\n# のネットワーク障害メッセージには、宛先 URL や解決された IP アドレス・\n# プロキシアドレスが含まれることがあります。読み取れない Windows\n# Installer の登録情報に関する項目には、Windows アカウントの SID\n# (S-1-5-21-...) やインストール済みソフトウェアの製品コードが含まれる\n# ことがあります。\n# このファイルを公開のバグ報告に添付する前に、三種類すべてを削除して\n# ください。\n`,
  'Cli.Help.Header': `InstallerClean - {InstallerFolder} をクリーンアップ`,
  'Cli.Help.Usage': `使用方法：`,
  'Cli.Help.Help': `  installerclean-cli --help     このヘルプを表示 (/?、-hも受け付けます)`,
  'Cli.Help.Version': `  installerclean-cli --version  バージョンを表示 (-vも受け付けます)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s         スキャンのみ - 不要ファイルを一覧`,
  'Cli.Help.Delete': `  installerclean-cli /d         不要ファイルを完全に削除`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m         保存済みのバックアップフォルダーへ移動`,
  'Cli.Help.MovePath': `  installerclean-cli /m PATH    指定されたパスに移動`,
  'Cli.Help.NoteLine1': `installerclean-cli は終了までプロンプトを占有するため、スクリプトや&#10;スケジュールされたタスクが完了を待てます。`,
  'Cli.Help.ExitCodesHeader': `終了コード：`,
  'Cli.Help.ExitCodeOk': `  0   成功：求められた処理を行い、失敗は何もなかった`,
  'Cli.Help.ExitCodeError': `  1   失敗：何も処理されなかった (引数や移動先の誤り、&#10;       スキャンの失敗、または全ファイルの失敗)`,
  'Cli.Help.ExitCodePartial': `  2   一部：一部は処理され、一部は処理されず (失敗または Ctrl+C)`,
  'Cli.Help.ExitCodeTransient': `  75  一時的：一時的な状態が実行をブロックしました (メッセージを参照)`,
  'Cli.Help.ExitCodeCancelled': `  130 キャンセル (Ctrl+C)`,
  'Tooltip.ChangeLanguage': `言語を変更します。プログラムが再起動します。`,
  'Automation.ChangeLanguage': `言語を変更`,
  'Automation.ChangeLanguage.HelpText': `プログラムが再起動します。`,
  'Body.NotScanned.Lead': `まだ何もスキャンしていません。`,
  'Body.NotScanned.Why': `「再スキャン」を押すと、{InstallerFolder} を調べて、どのプログラムも必要としなくなったインストーラーファイルを探します。`,
  'Confirm.MoveSameDrive': `そのフォルダーは同じドライブにあるため、削除するまで容量は戻りません。すぐに容量が必要な場合は、別のドライブのフォルダーを選んでください。`,
  'Error.ScanCorrelationFailed': `InstallerClean は Windows Installer の登録情報を {InstallerFolder} の内容と突き合わせられませんでした。登録情報が指しているもののほとんどがそこになく、そこにあるもののほとんどがどの登録情報にも名指しされていないため、どのファイルについても不要であることを示せませんでした。何も提示されず、何も取り除かれていません。`,
  'Error.CandidateOutsideCache': `このファイルは Windows Installer フォルダーの直下にありません。安全のために拒否されました。`,
  'Completion.MoveCancelledSummary': `キャンセルするまでに、{1} 個中 {0} 個の {2} を移動しました。`,
  'Completion.PermanentDeleteCancelledSummary': `キャンセルするまでに、{1} 個中 {0} 個の {2} を完全に削除しました。`,
  'Body.PendingReboot.Lead': `これらのファイルは今はクリーンアップできません。`,
  'Cli.TooManyArguments': `エラー：予期しない余分な引数 '{0}' があります。移動先フォルダーにスペースが含まれる場合は、パス全体を引用符で囲んでください：/m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `フォルダーはユーザーごと。予約実行や SYSTEM には /m PATH が必要。`,
  'Error.ScanRecordsUnreadable': `InstallerClean は、何がまだ必要かを確かめられるだけの Windows Installer の登録情報を読み取れませんでした。インストール済みプログラムの一覧が不足した状態で返され、同じ登録情報をレジストリから直接読み取る方法でもエラーが発生しました。あるファイルを指し示す登録情報が読み取れなかったものの一つだったというだけで、そのファイルが孤立しているように見えてしまうことがあるため、InstallerClean は中止しました。何も削除していません。`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer がインストール済みプログラムの一覧の終わりを最後まで知らせませんでした。InstallerClean は {2} {3} を読み取ったのち、{0} 件で打ち切りました (最後のエラーコード{1})。終わりのない一覧は信頼できないため、InstallerClean は中止しました。何も削除していません。`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer がひとつのプログラムのパッチ一覧の終わりを最後まで知らせませんでした。InstallerClean は {2} {3} を読み取ったのち、{0} 件で打ち切りました (最後のエラーコード{1})。終わりのない一覧は信頼できないため、InstallerClean は中止しました。何も削除していません。`,
  'UpdateCheck.Status.UpdateAvailable': `バージョン {0} が利用可能です。`,
  'Completion.DonateAsk': `お役に立てて何よりです。お心づけをいただけたら幸いです。`,
  'About.Link.Guide': `ガイドとよくある質問`,
  'About.Link.ReportProblem': `問題を報告`,
  'About.AutoUpdateCheck': `更新を自動的に確認する`,
  'Automation.About.Guide.HelpText': `ブラウザで github の readme を開きます。`,
  'Automation.About.ReportProblem.HelpText': `ブラウザで github.com の Issue トラッカーを開きます。`,
  'Automation.AutoUpdateCheck.HelpText': `チェックを入れると、InstallerClean は起動時に github で新しいバージョンを確認します。`,
  'Tooltip.MoveSameDrive': `不要ファイルをバックアップフォルダーへ移します。同じドライブ上にあるため、そのフォルダーを削除するまで空き容量は戻りません。`,
  'Confirm.DeletePermanently.Singular': `このファイルは完全に削除されます。安全な操作ですが、バックアップが欲しい場合は代わりに「移動」を使ってください。`,
  'Confirm.DeletePermanently.Plural': `これらのファイルは完全に削除されます。安全な操作ですが、バックアップが欲しい場合は代わりに「移動」を使ってください。`,
  'Error.ScanCacheRootUnresolved': `InstallerClean は {InstallerFolder} の実際のパスを Windows に解決させられなかったため、どのファイルについてもその中にあることを示せず、クリーンアップの対象として提示されたものはありません。今回のスキャンで何も見つからなかったのは、フォルダーがきれいだからではなく、その確認が失敗したためです。何も取り除かれていません。`,
  'Automation.Scroll.ProductDetails': `製品の詳細`,
  'Body.PendingReboot.Other': `Windows Installer で処理が進行中のため、移動と削除は一時停止しています。InstallerClean は変更中の {InstallerFolder} には触れません。終わったら再スキャンすれば、どちらも使えるようになります。`,
  'Cli.TooManyArgumentsNoPath': `エラー：予期しない余分な引数 '{0}'。/s と /d は他の引数を取らず、1 回の実行で使えるフラグは 1 つだけです。`,
  'Cli.MissingFromDisk.Singular': `Windows には、{InstallerFolder} にない {0} 件のファイルの記録があります: {1}。普段の使用では問題ありませんが、そのプログラムの更新やアンインストールが失敗することがあります。ファイルを元に戻すには、今お使いのバージョンのインストーラーが必要です。プログラムの提供元から入手し、既存のインストールに上書きして実行してください。新しいバージョンでは代用できません。新しいバージョンはまず今のものを削除する必要があり、その手順こそがこのファイルを必要とするからです。先にアンインストールする方法も、同じ理由でうまくいきません。これでファイルが復元され、設定はそのまま残るはずですが、Microsoft が保証しているわけではありません。`,
  'Cli.MissingFromDisk.Plural': `Windows には、{InstallerFolder} にない {0} 件のファイルの記録があります: {1}。普段の使用では問題ありませんが、それらのプログラムの更新やアンインストールが失敗することがあります。ファイルを元に戻すには、そのプログラムの今お使いのバージョンのインストーラーが必要です。プログラムの提供元から入手し、既存のインストールに上書きして実行してください。新しいバージョンでは代用できません。新しいバージョンはまず今のものを削除する必要があり、その手順こそがこのファイルを必要とするからです。先にアンインストールする方法も、同じ理由でうまくいきません。これでファイルが復元され、設定はそのまま残るはずですが、Microsoft が保証しているわけではありません。`,
  'Cli.MoveNotEnoughSpace': `エラー：{0} の空き容量が不足しています。これらのファイルの移動には {1} が必要ですが、空きは {2} です。何も移動されていません。`,
  'Cli.PendingRebootBlocked.Other': `エラー：Windows Installer で処理が進行中のため、/m と /d はブロックされます。InstallerClean は変更中の {InstallerFolder} には触れません。終わってからもう一度お試しください。`,
  'Cli.FoundNoOrphans': `不要なファイルは見つかりませんでした。`,
  'Cli.NothingOffered.Singular': `InstallerClean は、キャッシュ内のどのファイルがここにインストールされたプログラムのものかを確実には判断できなかったため、提示できたはずの 1 個のファイル({2})をそのままにしました。`,
  'Cli.NothingOffered.Plural': `InstallerClean は、キャッシュ内のどのファイルがここにインストールされたプログラムのものかを確実には判断できなかったため、提示できたはずの {0} 個の {1}({2})をすべてそのままにしました。`,
  'Cli.DestinationChangedMidBatch': `InstallerClean はバックアップフォルダーを確認できなくなったため、誤った場所に書き込まずに停止しました。{0} を確認してから、コマンドをもう一度実行してください。`,
  'Cli.Help.Summary': `どのインストール済みプログラムも必要としない .msi/.msp を削除します。`,
  'Cli.Help.Elevation': `管理者権限のプロンプトが必要です。Windows はそれ以外では起動しません。`,
  'Error.InstallerLockUnavailableTitle': `何も削除されませんでした`,
  'Error.MoveInstallerLockUnavailableTitle': `何も移動されませんでした`,
  'Error.InstallerLockUnavailable': `二つのプログラムが同時にインストール済みソフトウェアを変更しないよう Windows Installer が使うロックを InstallerClean が取得できなかったため、途中でファイルが必要になる可能性を排除できず、何も削除していません。もう一度お試しください。繰り返す場合は Windows を再起動してください。`,
  'Error.MoveInstallerLockUnavailable': `二つのプログラムが同時にインストール済みソフトウェアを変更しないよう Windows Installer が使うロックを InstallerClean が取得できなかったため、途中でファイルが必要になる可能性を排除できず、何も移動していません。もう一度お試しください。繰り返す場合は Windows を再起動してください。`,
  'Cli.InstallerLockUnavailable': `エラー：二つのプログラムが同時にインストール済みソフトウェアを変更しないようにする Windows Installer のロックを InstallerClean が取得できなかったため、途中でファイルが必要になる可能性を排除できませんでした。何も削除されていません。もう一度お試しください。繰り返す場合は Windows を再起動してください。`,
  'Cli.MoveInstallerLockUnavailable': `エラー：二つのプログラムが同時にインストール済みソフトウェアを変更しないようにする Windows Installer のロックを InstallerClean が取得できなかったため、途中でファイルが必要になる可能性を排除できませんでした。何も移動されていません。もう一度お試しください。繰り返す場合は Windows を再起動してください。`,
  'Completion.ReverifyIdentityClaimed': `{0} 個の {1} をそのままにしました。ファイル内に記されたプログラムの登録情報が Windows にあるためです。`,
  'Completion.ReverifyIdentityUnreadable': `{0} 個の {1} をそのままにしました。InstallerClean がファイル内にプログラム名を見つけられなかったためです。`,
  'Error.ScanNoRegisteredFileInFolder': `InstallerClean は Windows Installer の登録情報を {InstallerFolder} の内容と突き合わせられませんでした。フォルダーにはファイルがありますが、その中のどれかを指す登録情報が一つもないため、どのファイルについても不要であることを示せませんでした。何も提示されず、何も取り除かれていません。`,
  'Completion.NothingOffered': `この PC では何も提示されませんでした`,
  'Completion.NothingOfferedBody.Singular': `InstallerClean は、キャッシュ内のどのファイルがここにインストールされたプログラムのものかを確実には判断できなかったため、提示できたはずの 1 個のファイル({2})をそのままにしました。`,
  'Completion.NothingOfferedBody.Plural': `InstallerClean は、キャッシュ内のどのファイルがここにインストールされたプログラムのものかを確実には判断できなかったため、提示できたはずの {0} 個の{1}({2})をすべてそのままにしました。`,
  'Summary.SupersededHeldBack.Singular': `この PC では、InstallerClean は唯一の置換済みファイルがもう不要だと確実には判断できなかったため、そのファイルをそのまま残しました。`,
  'Summary.SupersededHeldBack.Plural': `この PC では、InstallerClean は {0} 個の置換済みファイルがもう不要だと確実には判断できなかったため、それらのファイルをそのまま残しました。`,
  'Cli.SupersededHeldBack.Singular': `この PC では、InstallerClean は唯一の置換済みファイルがもう不要だと確実には判断できなかったため、そのファイルをそのまま残しました。`,
  'Cli.SupersededHeldBack.Plural': `この PC では、InstallerClean は {0} 個の置換済みファイルがもう不要だと確実には判断できなかったため、それらのファイルをそのまま残しました。`,
  'Completion.HeldBack.Singular': `{0} 件のファイルを保留しました。スキャンでは不要と判断されましたが、最終チェックの判断は違いました。`,
  'Completion.HeldBack.Plural': `{0} 件のファイルを保留しました。スキャンでは不要と判断されましたが、最終チェックの判断は違いました。`,
  'Body.PendingReboot.PendingRenameUnresolved': `次回の再起動に向けてファイル操作が予約されていますが、InstallerClean にはそれがどのファイルを指しているか分からないため、{InstallerFolder} 内のファイルでないとは言い切れません。クリーンアップの前に Windows を再起動してください。`,
  'Completion.MoveRestoreHint': `問題ないと納得できたら、そのフォルダーを削除してください。`,
  'Completion.MoveRestoreHintSameDrive': `問題ないと納得できたら、そのフォルダーを削除してください。削除するまで空き容量は実際には戻りません。`,
  'Confirm.MoveDestination.Singular': `このファイルの移動先:`,
  'Confirm.MoveDestination.Plural': `これらのファイルの移動先:`,
  'Cli.NothingListed.Singular': `この PC では、キャッシュ内のどのファイルがここにインストールされたプログラムのものか InstallerClean が確信を持てなかったため、その 1 件のファイル ({2}) を一覧に載せず保留しました。`,
  'Cli.NothingListed.Plural': `この PC では、キャッシュ内のどのファイルがここにインストールされたプログラムのものか InstallerClean が確信を持てなかったため、{0} {1} ({2}) を一覧に載せず保留しました。`,
  'Cli.WithheldReasons.Header': `確信を持てなかった理由:`,
  'Cli.WithheldReasons.RecordedPath': `  Windows Installer 自身の記録にあるファイルパスを解決できませんでした。`,
  'Cli.WithheldReasons.FileIdentity': `  Windows Installer の記録に挙げられたファイルの識別情報を読み取れませんでした。`,
  'Cli.WithheldReasons.SecondInstance': `  この PC には同じプログラムが複数回インストールされている可能性があります。`,
  'Cli.PendingRebootBlocked.PendingRenameUnresolved': `エラー: 次回の再起動に向けてファイル操作が予約されていますが、InstallerClean にはそれがどのファイルを指しているか分からないため、{InstallerFolder} を除外できません。クリーンアップの前に Windows を再起動してください。`,
  'Cli.MoveRestoreHint': `プログラムがこれまでどおり更新およびアンインストールできることを確認してから、{0} を削除してください。`,
  'Error.ScanStoppedDetails': `これは {0} にも記録されます。`,
  'Cli.EventLogReason.PendingRenameUnresolved': `queued post-reboot file rename could not be resolved`,
};

// PARSE CONTROL. About the READING and not about the content, and it exits 2,
// which is a code no ordinary run of this generator can produce: a generator is
// red by intent for the whole gap between a string landing in English and its
// translation round, so its verdict lines and its exit 0 are load-bearing in
// ci.yml and are deliberately untouched here. This says something different from
// "the translation is not done". It says the file could not be read.
//
// BOTH LEGS. raw === 0 catches a file that declares no entry at all, which the
// equality cannot see on its own because 0 === 0 holds. parsed !== raw catches
// entries the reader dropped, which one <comment> moved above its <value> does to
// any regex wanting <value> on the same whitespace run, and the Visual Studio resx
// editor writes that shape. Counted with <data\b so a tab after the tag name is
// not read as an empty file, and neither figure is written down, so a string added
// to the resx cannot make this go stale.
//
// WHY IT IS HERE WHEN THE SELF-CHECK BELOW ALREADY REDDENS. The self-check reaches
// the right verdict through what it happens to compare, not through knowing it
// read anything: with the neutral's attribute order changed, this generator wrote a
// 389-entry file and its own self-check parsed THREE entries out of it, said
// GENERATION HAS ISSUES, and was right for a reason with nothing to do with the
// truth. A tool reasoning over three entries of a 389-entry artefact should say so.
const parseControl = (where, xml, parsed) => {
  const raw = (xml.match(/<data\b/g) || []).length;
  if (raw !== 0 && parsed === raw) return;
  console.error(`PARSE CONTROL FAILED for ${where}: ${raw} '<data' occurrence(s), ${parsed} parsed.`);
  console.error('Refusing to report on a file this generator cannot show it read.');
  process.exit(2);
};

let text = readFileSync(BASE, 'utf8');
// The transform below reaches every entry through '<data name="', one space and no
// \s+, which is NOT the spelling the self-check's parse() uses further down. A
// control that exercises a pattern the reader does not use proves the file has
// structure and proves nothing about whether this reader can reach it, so the
// source is controlled in its own spelling before a single value is replaced.
parseControl(BASE, text,
  [...text.matchAll(/<data name="([^"]+)"[^>]*>\s*<value>/g)].length);

// The stripped keys, by name (see the header). Everything else, coolvitto's
// machine Cli values included, stays and is translated from MAP. A named set
// rather than a predicate, because no predicate can separate these from the
// ones that stay: the difference is who wrote them, which is not a property of
// the key.
//
// That rule is what puts a machine key added later in here rather than in the
// MAP. Nobody wrote a Japanese line for it, and nobody should: MachineContract
// forces en-GB at every Cli.EventLog* emit site, so a translated value is never
// reached, and stripping the key leaves the lookup falling through to the
// neutral, which is the English the Application channel is grepped for anyway.
const STRIPPED = new Set([
  'Cli.EventLogDeleteSummary',
  'Cli.EventLogScanNoOrphans',
  'Cli.EventLogScanWithheld',
  'Cli.EventLogMissingFromDisk',
  'Cli.EventLogMoveNotEnoughSpace',
  'Cli.EventLogMoveAborted',
  'Cli.EventLogInstallerLockUnavailable',
  // Added in the 3.0.0 round, by the rule in the paragraph above: both are
  // machine Cli keys that postdate coolvitto's PR, so nobody wrote a Japanese
  // line for them and nobody should.
  'Cli.EventLogNothingOffered',
  'Cli.EventLogNothingOfferedNotice',
]);
let stripped = 0;
text = text.replace(/[^\S\n]*<data name="(Cli\.[^"]*)"[\s\S]*?<\/data>\n?/g,
  (m, name) => { if (STRIPPED.has(name)) { stripped++; return ''; } return m; });

// Replace each key's inner <value> from MAP.
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
const placeholders = (s) => new Set([...s.matchAll(/\{(\d+)(?::[^}]*)?\}/g)].map((p) => p[1]));
const parse = (xml, where) => {
  const map = new Map();
  const re = /<data\s+name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)<\/value>/g;
  let m;
  while ((m = re.exec(xml)) !== null) map.set(m[1], m[2]);
  parseControl(where, xml, map.size);
  return map;
};
const neutral = parse(readFileSync(BASE, 'utf8'), BASE);
const written = readFileSync(OUT, 'utf8');
const output = parse(written, OUT);
// ja ships every neutral key, machine Cli included, bar the one stripped above.
const neutralRequired = [...neutral.keys()].filter((k) => !STRIPPED.has(k));
const strippedLeaked = [...STRIPPED].filter((k) => output.has(k));

// The one human-facing Cli.EventLog* key, asserted present rather than left to
// the counts: a hand edit adding it to the named set above takes it out of the
// output AND out of the required set, so every figure above still agrees. The
// MAP substitution notices today only through the order the two run in.
const humanCliStripped = !output.has('Cli.EventLogUnavailable');

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
console.log('machine Cli <data> removed:', stripped, `(expect ${STRIPPED.size}:`, [...STRIPPED].join(', ') + ')');

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
if (strippedLeaked.length) console.log('!! still in the output; the strip regex missed them:', strippedLeaked);
if (humanCliStripped) console.log('!! Cli.EventLogUnavailable stripped: that key is human-facing and must stay');

const structuralOk = !notApplied.length && !missingFromMap.length && !strayMapKeys.length &&
  !missingFromOutput.length && !arityMismatch.length && !strippedLeaked.length &&
  !humanCliStripped && stripped === STRIPPED.size &&
  output.size === neutralRequired.length && crlf === 0;
const ok = structuralOk && !untranslated.length;
console.log(ok ? '\nGENERATION OK' : '\nGENERATION HAS ISSUES (see above)');
