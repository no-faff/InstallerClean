#!/usr/bin/env node
// Korean (ko) satellite generator for InstallerClean. Copied from
// gen-strings-template.mjs; only OUT and the MAP values differ. Works FROM THE
// ENGLISH SOURCE (Strings.resx): replaces each key's inner <value>, strips the
// machine-contract Cli.EventLog* keys, keeps the human Cli keys, and
// self-verifies against the neutral. Output is LF, UTF-8.
//
// Korean plural rule (DisplayHelpers.CategoryFor, case "ko"): PluralCategory
// .Other at every count. Korean nouns do not inflect for number, so there are
// NO .One/.Few/.Many override keys, and the Plural.* pairs are identical
// (both 파일 etc). The hardcoded .Singular/.Plural sentence pairs are
// translated on both members and come out identical.
//
// Register: 합니다체 (formal-polite) throughout, matching README.ko.md and the
// Windows Korean UI convention; warmth carried by word choice. Platform terms
// sourced from Windows: About = 정보, Event Log / Application log / Group
// Policy = 이벤트 로그 / 응용 프로그램 로그 / 그룹 정책.
import { readFileSync, writeFileSync } from 'node:fs';

const dir = 'src/InstallerClean.Core/Resources';
const BASE = `${dir}/Strings.resx`;            // English source (the "neutral")
const OUT = `${dir}/Strings.ko.resx`;

// Universal keeps: keys whose value is the same in every language, the brand names
// and the pure-placeholder announcement string. Their still-English value is NOT a
// miss. Explicit by KEY on purpose: a future brand key then defaults to "flag until
// someone adds it here", never silently passes. Do NOT translate these values. Do
// NOT edit this list per language.
//
// The four size suffixes and the two elapsed suffixes do not belong in this list,
// because they are not universal: French writes Go/Mo/Ko/o, Russian and Ukrainian
// write ГБ/МБ/КБ/Б and мс/с. Those three carry real
// values in their MAP; the languages that do abbreviate as English does keep them in
// ALSO_KEEP, which is the per-language list. Display.ListSeparator is the same shape,
// for the same reason.
const KEEP_ENGLISH = new Set([
  'Window.Main.Title',                 // InstallerClean
  'Startup.AlreadyRunningTitle',       // InstallerClean
  'Startup.UnhandledTitle',            // InstallerClean
  'Automation.ScanResultAnnouncement', // {0} ({1})
]);

// Per-language keeps: empty for Korean. 패치 is a Hangul translation of "patch",
// not an English keep.
const ALSO_KEEP = [
  // The list separator Korean uses is the one English uses. A punctuation
  // mark rather than a word, so there is nothing to translate and nothing to
  // get wrong; only ja and zh-Hans differ, taking the ideographic comma.
  'Display.ListSeparator',       // ", "
  // The size and elapsed unit suffixes. Korean abbreviates them exactly as
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
];

const MAP = {
  // Window titles
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `정보`,
  'Window.Registered.Title': `그대로 둔 파일`,
  'Window.Orphaned.Title': `안전하게 삭제할 수 있는 불필요한 파일`,

  // Section headings
  // Section.Registered.Products and Automation.Section.Products are deliberately
  // not here. They left the neutral resx when the registered-files window stopped
  // having a products group of its own, so a value for either would be
  // regenerated into a key the app cannot use.
  'Section.Registered.Patches': `패치`,
  'Section.Registered.Details': `제품 세부 정보`,
  'Section.Backup.Folder': `백업 폴더`,
  'Section.SayThanks': `감사 인사`,

  // Field labels (used in detail panels)
  'Field.Reason': `이유`,
  'Field.Author': `작성자`,
  'Field.Application': `애플리케이션`,
  'Field.Title': `제목`,
  'Field.Subject': `주제`,
  'Field.Keywords': `키워드`,
  'Field.SigningCertificate': `서명 인증서`,
  'Field.FileSize': `파일 크기`,
  'Field.Comment': `설명`,
  'Field.ProductName': `제품 이름`,
  'Field.File': `파일`,
  'Field.Size': `크기`,
  'Field.Patches': `패치`,

  // Placeholder shown for a registered package whose API ProductName is empty.
  'Field.UnknownProductName': `(알 수 없음)`,
  'Field.PatchesOnly': `(패치 전용)`,
  'Field.Missing': `누락`,

  // Actions (button labels; underscore prefixes are WPF mnemonics)
  'Action.About': `정보(_A)`,
  'Action.Copy': `복사`,
  'Action.Cut': `잘라내기`,
  'Action.Paste': `붙여넣기`,
  'Action.SelectAll': `모두 선택`,
  'Action.Browse': `찾아보기(_B)...`,
  'Action.Cancel': `취소(_C)`,
  'Action.CheckForUpdates': `업데이트 확인(_U)`,
  'Action.Close': `닫기(_C)`,
  'Action.DeletePermanently': `영구 삭제(_D)`,
  'Action.Done': `완료(_D)`,
  'Action.Details': `세부 정보`,
  'Action.BuyMeACuppa': `커피 한 잔 사주기(_B)`,
  'Action.LeaveStarOnGitHub': `GitHub에 별 남기기(_S)`,
  'Action.Licence': `Apache 2.0 라이선스`,
  'Action.Move': `이동(_M)`,
  'Action.BackupFolderPlaceholder': `삭제하지 않고 이동할 경우 사용할 폴더 경로입니다.`,
  'Action.OpenReleasePage': `릴리스 페이지 열기(_R)`,
  'Action.Rescan': `다시 검사(_R)`,
  'Action.ScanAgain': `다시 검사(_S)`,
  'Action.SendResultLog': `보고서 보내기`,
  'Action.SendResultLogConfirm': `보내기(_S)`,

  // Automation names (screen reader / accessibility)
  'Automation.BuyMeACuppa': `후원`,
  'Automation.BuyMeACuppa.About': `커피 한 잔 사주기`,
  'Automation.CancelOperation': `작업 취소`,
  'Automation.CancelScan': `검사 취소`,
  'Automation.CancelStartupScan': `시작 검사 취소`,
  'Automation.Close': `닫기`,
  'Automation.CloseWindow': `창 닫기`,
  'Automation.CloseResult': `결과를 닫고 메인 창으로 돌아가기`,
  'Automation.LeaveStarOnGitHub.About': `github에 별 남기기`,
  'Automation.Minimise': `최소화`,
  'Automation.ConfirmDelete': `영구 삭제하면 불필요한 파일이 제거됩니다. 취소하면 아무것도 삭제하지 않고 닫습니다.`,
  'Automation.ConfirmMove': `이동하면 불필요한 파일이 선택한 대상 폴더에 들어갑니다. 취소하면 파일은 있던 자리에 그대로 남습니다.`,
  'Automation.SayThanks': `감사 인사`,
  'Automation.ConfirmSendResultLog': `보내기를 누르면 표시된 보고서가 No Faff에 전송됩니다. 취소하면 아무것도 보내지 않습니다.`,
  'Automation.CheckForUpdates': `업데이트 확인`,
  'Automation.CheckForUpdates.HelpText': `github의 릴리스 페이지에서 새 버전이 있는지 확인합니다.`,
  'Automation.UpdateAvailable.HelpText': `새 버전을 내려받으려면 릴리스 페이지를 열고, 현재 버전을 유지하려면 취소하세요.`,
  'Automation.Licence.HelpText': `브라우저에서 github.com의 라이선스 파일을 엽니다.`,
  'Automation.Section.BackupFolder': `백업 폴더`,
  'Automation.Section.Patches': `패치`,
  'Automation.Section.ProductDetails': `제품 세부 정보`,
  'Automation.BackupFolder': `백업 폴더`,
  'Automation.OperationProgress': `작업 진행 상황`,
  'Automation.RescanInstaller': `{InstallerFolder} 다시 검사`,
  'Automation.ScanningProgress': `검사 진행 상황`,
  'Automation.StartupScanProgress': `시작 검사 진행 상황`,
  'Automation.ViewOrphanedFiles': `세부 정보, 불필요한 파일`,
  'Automation.ViewOrphanedFiles.HelpText': `정리할 수 있습니다.`,
  'Automation.ViewRegisteredFiles': `세부 정보, 그대로 둔 파일`,
  'Automation.ViewRegisteredFiles.HelpText': `읽기 전용 목록입니다.`,
  'Automation.SortStatus.Ascending': `{0} 기준 오름차순 정렬됨`,
  'Automation.SortStatus.Descending': `{0} 기준 내림차순 정렬됨`,
  'Automation.Scroll.ScanResults': `검사 결과`,
  'Automation.Scroll.ResultDetails': `결과 세부 정보`,
  'Automation.Scroll.FileDetails': `파일 세부 정보`,
  'Automation.Scroll.DialogBody': `대화 상자 텍스트`,
  'Automation.ScanResultAnnouncement': `{0} ({1})`,
  'Automation.CompletionErrors': `처리할 수 없는 파일`,
  'Automation.RegisteredMissingSeeAlso': `이 폴더에 대한 설명과 파일 복구 방법을 README에서 안내`,

  // Tooltips
  'Tooltip.BuyMeACuppa.About': `이게 생각보다 목마른 일입니다!`,
  'Tooltip.CancellingPending': `취소가 요청되었습니다. InstallerClean이 현재 단계가 멈출 수 있는 지점에 이를 때까지 기다리고 있습니다. I/O가 많거나 MSI 데이터베이스를 호출하는 동안에는 몇 초 걸릴 수 있습니다.`,
  'Tooltip.Close': `닫기`,
  'Tooltip.LeaveStarOnGitHub.About': `별 하나가 다른 사람들이 InstallerClean을 찾는 데 도움이 됩니다.`,
  'Tooltip.Minimise': `최소화`,
  'Tooltip.SendResultLog': `보내실지는 자유지만 보내 주시면 정말 감사합니다. 익명 요약을 전송하며, 이는 프로그램이 잘 작동하는지와 사람들이 공간을 얼마나 확보하고 있는지 제가 알 수 있게 해 줍니다. 다음 화면에서 확인 전에 보낼 내용을 미리 볼 수 있습니다.`,
  'Tooltip.SendResultLog.NothingFound': `보내실지는 자유지만 보내 주시면 정말 감사합니다. 익명 요약을 전송하며, 이는 프로그램이 잘 작동하는지 제가 알 수 있게 해 줍니다. 다음 화면에서 확인 전에 보낼 내용을 미리 볼 수 있습니다.`,
  'Tooltip.Move': `불필요한 파일을 백업 폴더로 옮깁니다.`,
  'Tooltip.MoveNeedsDestination': `불필요한 파일을 백업 폴더로 옮깁니다. 폴더는 곧이어 선택하게 됩니다.`,
  'Tooltip.Delete': `불필요한 파일을 영구히 삭제합니다. 모든 것이 괜찮은지 직접 확인해 보고 싶으면 대신 이동을 사용하세요.`,
  'Tooltip.SigningCertificate': `내장된 Authenticode 인증서의 주체 이름입니다. 인증서 체인은 검증하지 않았습니다.`,

  // Body copy
  'Body.MainExplanation.Lead': `아래에 있는 불필요한 파일은 모두 [안전하게 삭제할 수 있습니다].`,
  'Body.MainExplanation.Why': `이 파일들은 {InstallerFolder}에 있습니다. InstallerClean은 설치된 모든 프로그램에 대해 Windows에 문의합니다. 어떤 프로그램도 자기 것이라고 하지 않거나({0}), 더 새로운 패치가 그 파일을 대체했고 어떤 프로그램도 그 파일로 되돌아갈 수 없을 때({1}) 목록에 오릅니다.`,
  'Body.MainExplanation.Action': `직접 고른 백업 폴더로 옮긴 다음, 프로그램이 여전히 정상적으로 업데이트되고 제거되는지 확인되면 그 폴더를 삭제하세요. {InstallerFolder}에 다시 넣으면 모두 원래대로 돌아갑니다. 아니면 지금 영구히 삭제하세요.`,
  'Body.PendingReboot.MsiExecuteMutex': `지금 무언가가 Windows Installer를 사용하고 있습니다. Windows 업데이트이거나 백그라운드에서 설치 중인 프로그램일 수 있습니다. 그동안 이동과 삭제는 일시 중지되어, InstallerClean이 변경 중인 {InstallerFolder}를 건드리지 않습니다. 끝나면 다시 검사하면 두 기능이 돌아옵니다.`,
  'Body.PendingReboot.InstallerInProgress': `이 컴퓨터에 이전 Windows Installer 트랜잭션이 중단된 채 남아 있습니다. {InstallerFolder}를 정리하기 전에 그 설치를 계속하거나 되돌리세요(또는 Windows를 다시 시작하세요).`,
  'Body.PendingReboot.PendingRenameInCache': `Windows가 다음 재시작 때 처리할 파일 이름 변경을 대기열에 넣어 두었고, 그 대상이 {InstallerFolder}입니다. 정리하기 전에 Windows를 다시 시작하세요.`,
  'Body.NoFileSelected': `세부 정보를 보려면 파일을 선택하세요.`,
  'Body.NoProductSelected': `세부 정보를 보려면 제품을 선택하세요.`,
  'Body.NoMetadata': `사용할 수 있는 메타데이터가 없습니다.`,
  'Body.RegisteredMissingFromDisk': `이 설치 파일이 없습니다. 지금은 아무 문제도 일으키지 않으며, 이 파일이 속한 프로그램을 업데이트하거나 제거하려는 날이 오기 전까지는 문제가 없습니다. 그때 Windows가 이 파일을 찾지 못해 그 단계가 실패할 수 있습니다.\n\n되돌리려면 지금 사용 중인 버전의 설치 프로그램이 필요합니다. 프로그램 제작사에서 구해 기존 설치본 위에 실행하세요. 더 새 버전으로는 되지 않습니다. 새 버전은 먼저 지금 있는 것을 제거해야 하는데, 바로 그 단계에 이 파일이 필요하기 때문입니다. 먼저 제거하는 방법도 같은 이유로 되지 않습니다. 이렇게 하면 파일이 복원되고 설정은 그대로 남아야 하지만, Microsoft가 보장하지는 않습니다.`,
  'Body.RegisteredMissingFromDisk.SeeAlso': `README에는 Microsoft의 표현 그대로 [이 폴더에 대한 설명]과 파일을 복구하는 방법이 담겨 있습니다.`,
  'Body.NoPatches': `(없음)`,

  // Reasons (Reason column in the orphaned-files list)
  'Reason.Orphaned': `고립됨`,
  'Reason.Superseded': `대체됨`,
  'Reason.Obsoleted': `폐기됨`,

  // Status / progress text
  'Status.Scanning': `검사 중...`,
  'Status.Cancelling': `취소 중...`,
  'Status.StartingScan': `검사를 시작하는 중...`,
  'Status.QueryingApi': `설치된 소프트웨어 정보를 Windows에 조회하는 중...`,
  'Status.ScanningCache': `설치 관리자 캐시 폴더를 검사하는 중...`,
  'Status.EnumeratingProducts': `설치된 제품을 열거하는 중...`,
  'Status.CheckingRegistry': `레지스트리에서 추가 패키지를 확인하는 중...`,

  // 0 = registered package count, 1 = pluralised "package"/"packages"
  'Status.RegisteredPackagesFound': `등록된 {1} {0}개를 찾았습니다.`,

  // 0 = elapsed time text (e.g. "1.2s")
  'Status.ScanComplete': `검사 완료 ({0})`,
  'Status.FoundProducts': `로컬 패키지를 검사하는 중...`,

  // 0 = file count, 1 = pluralised noun ("file"/"files")
  'Status.FoundUnused': `안전하게 삭제할 수 있는 {1} {0}개를 찾았습니다.`,
  'Status.PreparingDestination': `대상 폴더를 준비하는 중...`,

  // 0 = file count, 1 = pluralised noun
  'Status.Moving': `불필요한 파일 이동 중...`,
  'Status.Deleting': `불필요한 파일 삭제 중...`,
  'Status.MoveCancelled.Partial': `이동이 취소되었습니다. {2} {1}개 중 {0}개를 처리했습니다.`,
  'Status.DeleteCancelled.Partial': `삭제가 취소되었습니다. {2} {1}개 중 {0}개를 처리했습니다.`,
  'Status.MoveFailed': `{0}. 자세한 내용은 {1}에 있습니다.`,
  'Status.MoveFailed.NoLog': `{0}. 크래시 로그를 기록할 수 없었습니다.`,
  'Status.DeleteFailed': `{0}. 자세한 내용은 {1}에 있습니다.`,
  'Status.DeleteFailed.NoLog': `{0}. 크래시 로그를 기록할 수 없었습니다.`,
  'Status.ScanAccessDenied': `액세스가 거부되었습니다. Windows가 검사를 거부했습니다.`,
  'Status.ScanFailedDb': `검사 실패: Windows Installer 기록을 읽을 수 없습니다.`,
  'Status.ScanCancelled': `검사가 취소되었습니다.`,
  'Status.Done': `준비됨`,
  'Status.ScanFailedDetails': `검사 실패 ({0}). 자세한 내용은 {1}에 있습니다.`,
  'Status.ScanFailedDetails.NoLog': `검사 실패 ({0}). 크래시 로그를 기록할 수 없었습니다.`,

  // Completion screen
  'Completion.AllClean': `모두 깨끗합니다`,
  'Completion.NothingToCleanUp': `{InstallerFolder}에 정리할 것이 없습니다`,
  'Completion.NothingToCleanUpReceipt': `{1} {0}개 검사, {2} 소요`,

  // 0 = size freed (e.g. "120.5 MB")
  'Completion.Freed': `{0} 확보`,
  'Completion.Moved': `{0} 이동`,
  // Heading for the outcome where the operation acted on no file at all, then
  // the failure count line beneath it: 0 = files that failed, 1 = files tried
  // (succeeded + failed). The count selecting the form is {0}; the noun belongs
  // to {1}, so treat the two independently.
  'Completion.NothingMoved': `이동된 파일 없음`,
  'Completion.NothingDeleted': `삭제된 파일 없음`,
  'Completion.FailedCount.Singular': `파일 {1}개 중 {0}개를 이동하지 못했습니다.`,
  'Completion.FailedCount.Plural': `파일 {1}개 중 {0}개를 이동하지 못했습니다.`,
  'Completion.FailedCountDelete.Singular': `파일 {1}개 중 {0}개를 삭제하지 못했습니다.`,
  'Completion.FailedCountDelete.Plural': `파일 {1}개 중 {0}개를 삭제하지 못했습니다.`,

  // 0 = moved count, 1 = pluralised noun, 2 = destination path
  'Completion.MoveSummary.Singular': `{1} {0}개를 다음 위치로 이동함: {2}`,
  'Completion.MoveSummary.Plural': `{1} {0}개를 다음 위치로 이동함: {2}`,

  // 0 = deleted count, 1 = pluralised noun

  // 0 = deleted count, 1 = pluralised noun
  'Completion.PermanentDeleteSummary.Singular': `{1} {0}개 영구 삭제됨`,
  'Completion.PermanentDeleteSummary.Plural': `{1} {0}개 영구 삭제됨`,

  // Summaries
  'Summary.RegisteredStillUsed.Singular': `파일 {0}개 그대로 둠`,
  'Summary.RegisteredStillUsed.Plural': `파일 {0}개 그대로 둠`,
  'Summary.OrphanedToCleanUp.Singular': `정리할 불필요한 파일 {0}개`,
  'Summary.OrphanedToCleanUp.Plural': `정리할 불필요한 파일 {0}개`,
  'Summary.NothingListed.Singular': `InstallerClean이 캐시에 있는 어떤 파일이 여기 설치된 프로그램에 속하는지 확실히 알 수 없어서, 파일 하나를 제시하지 않고 보류했습니다.`,
  'Summary.NothingListed.Plural': `InstallerClean이 캐시에 있는 어떤 파일이 여기 설치된 프로그램에 속하는지 확실히 알 수 없어서, {1} {0}개를 제시하지 않고 보류했습니다.`,
  'Summary.MissingFromDisk.Singular': `Windows에 {InstallerFolder}에 없는 파일 {0}개의 기록이 있습니다: {1}. 평소에는 문제가 없지만 그 프로그램의 업데이트나 제거가 실패할 수 있습니다. 어떻게 할지는 세부 정보를 여세요.`,
  'Summary.MissingFromDisk.Plural': `Windows에 {InstallerFolder}에 없는 파일 {0}개의 기록이 있습니다: {1}. 평소에는 문제가 없지만 그 프로그램들의 업데이트나 제거가 실패할 수 있습니다. 어떻게 할지는 세부 정보를 여세요.`,
  'Summary.MissingFromDisk.OtherPrograms.Singular': `다른 프로그램 {0}개`,
  'Summary.MissingFromDisk.OtherPrograms.Plural': `다른 프로그램 {0}개`,
  'Summary.MissingFromDisk.Unnamed.Singular': `기록에 프로그램 이름이 없는 파일 {0}개`,
  'Summary.MissingFromDisk.Unnamed.Plural': `기록에 프로그램 이름이 없는 파일 {0}개`,

  // 0 = current file count, 1 = total count, 2 = pluralised noun.
  'Summary.OperationFiles': `{2} {1}개 중 {0}개`,

  // Orphaned-window footer. 0 = orphaned count, 1 = superseded count,
  // 2 = obsoleted count, 3 = size display.
  'Summary.OrphanedWindow': `불필요한 {1} {0}개 ({2})`,

  // Registered-window footer. 0 = count, 1 = size display.
  'Summary.RegisteredWindow.Singular': `파일 {0}개 그대로 둠 ({1})`,
  'Summary.RegisteredWindow.Plural': `파일 {0}개 그대로 둠 ({1})`,

  // Confirmation dialogs

  // 0 = file count, 1 = pluralised "file"/"files", 2 = size display
  'Confirm.MoveTitle': `{1} {0}개를 이동하시겠습니까? ({2})`,

  'Confirm.DeleteTitle': `{1} {0}개를 삭제하시겠습니까? ({2})`,

  // Error messages
  'Error.AdminRequiredTitle': `액세스 거부됨`,
  'Error.AdminRequiredBody': `Windows가 InstallerClean의 접근을 거부해서 작업을 멈췄습니다. 아무것도 제거되지 않았습니다.\n\nInstallerClean은 이미 관리자 권한으로 실행 중이었으므로 그런 식으로 다시 시작해도 도움이 되지 않습니다. Windows는 무엇이 접근을 거부했는지 더 이상 알려주지 않으므로 구체적으로 시도해 볼 것이 없습니다.`,
  'Error.InstallerDbUnavailableTitle': `Windows Installer 기록을 읽을 수 없습니다`,
  'Error.ScanFailedTitle': `검사 실패`,
  'Error.InstallerDbEmpty': `Windows Installer 기록이 완전히 비어서 돌아왔습니다. 설치된 프로그램도, 업데이트도 캐시된 설치 파일을 하나도 요구하지 않습니다. 정상적으로 작동하는 컴퓨터에서는 이런 일이 없으므로(갓 설치한 Windows에도 그런 파일이 있습니다) 기록이 손상되었거나 읽을 수 없었던 것이고, 이 답을 그대로 믿은 검사는 {InstallerFolder}의 모든 파일을 잘못 고립된 것으로 판단했을 것입니다. InstallerClean은 그러지 않고 멈췄습니다. 아무것도 제거되지 않았습니다.`,
  'Error.MsiAccessDenied': `Windows Installer가 InstallerClean에게 설치된 항목의 목록 표시를 허용하지 않았습니다. InstallerClean은 이미 관리자 권한으로 실행 중이었으므로 관리자 권한으로 다시 실행해도 달라지는 것이 없습니다. 그 목록이 없으면 캐시된 파일 중 어느 것이 아직 필요한지 안전하게 알아낼 방법이 없으므로 InstallerClean은 멈췄습니다. 아무것도 제거되지 않았습니다.`,
  'Error.MsiNonSuccess': `Windows Installer가 InstallerClean에게 읽을 수 있는 설치된 프로그램 목록을 주지 못했습니다. {2} {3}을(를) 읽은 다음 {0}개 항목이 연속으로 읽을 수 없는 상태로 돌아왔습니다(마지막 오류 코드 {1}). 일부만 읽은 목록으로 작업하는 대신 InstallerClean은 멈췄습니다. 아무것도 제거되지 않았습니다.`,
  'Error.InvalidDestinationTitle': `잘못된 대상`,
  'Error.DestinationWriteFailedTitle': `대상에 쓸 수 없음`,
  'Error.MoveFailedTitle': `이동 실패`,
  'Error.DeleteFailedTitle': `삭제 실패`,
  'Error.SettingNotSavedTitle': `설정 저장 실패`,
  'Error.SettingNotSavedBody': `변경 내용을 저장하지 못했습니다. 다음에 실행할 때 InstallerClean은 이전 설정으로 돌아갑니다.`,
  'Error.DestinationInsideInstaller': `대상은 Windows Installer 폴더 안에 있을 수 없습니다.`,

  // 0 = the destination path the user typed
  'Error.DestinationInSystemFolder': `대상 {0}이(가) Windows 시스템 폴더 아래를 가리킵니다. %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)%, %ProgramData% 바깥의 경로를 선택하세요.`,
  'Error.NotEnoughSpaceTitle': `공간 부족`,

  // 0 = destination, 1 = required size, 2 = available size
  'Error.NotEnoughSpaceBody': `{0}에 자리가 부족합니다\n\n필요: {1}\n사용 가능: {2}`,

  // 0 = destination
  'Error.AccessDeniedDestination': `{0}에 쓸 권한이 없습니다.\n사용자 프로필 안의 폴더나 본인 소유의 드라이브를 사용해 보세요.`,
  'Error.PathTooLong': `Windows가 처리하기에는 경로가 너무 깁니다 ({0}). 더 짧은 경로를 선택하세요.`,
  'Error.DestinationMissing': `폴더가 존재하지 않으며 만들 수도 없습니다 ({0}). 드라이브 문자나 네트워크 경로를 확인하세요.`,
  'Error.IOWriteDestination': `Windows가 {0}에 쓸 수 없습니다.\n자세한 내용은 {1}에 있습니다.`,
  'Error.IOWriteDestination.NoLog': `Windows가 {0}에 쓸 수 없습니다. 크래시 로그를 기록할 수 없었습니다.`,
  'Error.WriteDestination': `{0}에 쓸 수 없습니다.\n자세한 내용은 {1}에 있습니다.`,
  'Error.WriteDestination.NoLog': `{0}에 쓸 수 없습니다. 크래시 로그를 기록할 수 없었습니다.`,
  'Error.MissingSourceFile': `파일이 더 이상 존재하지 않습니다.`,
  'Error.SourceIsReparsePoint': `원본 파일이 심볼릭 링크 또는 정션입니다. 안전을 위해 거부했습니다.`,
  // Singular = the sentence for ONE file, which the CLI prints after "filename: ";
  // plural = the heading the completion overlay puts over a list of filenames.
  'Error.AccessDenied.Singular': `Windows가 이 파일에 대한 접근을 거부했습니다. 파일은 그대로 두었습니다.`,
  'Error.AccessDenied.Plural': `Windows가 이 파일들에 대한 접근을 거부했습니다. 파일은 그대로 두었습니다.`,
  'Error.FileInUse.Singular': `이 파일은 다른 프로그램이 열어 두었거나 잠가 두어서 지금은 어떤 것도 제거할 수 없습니다. 파일은 그대로 두었습니다. 나중에 다시 시도하세요.`,
  'Error.FileInUse.Plural': `이 파일들은 다른 프로그램이 열어 두었거나 잠가 두어서 지금은 어떤 것도 제거할 수 없습니다. 파일들은 그대로 두었습니다. 나중에 다시 시도하세요.`,
  'Error.IOFailure.Singular': `Windows가 파일 오류를 알렸습니다. 파일은 그대로 두었습니다.`,
  'Error.IOFailure.Plural': `Windows가 파일 오류를 알렸습니다. 이 파일들은 그대로 두었습니다.`,
  'Error.UnknownError.Singular': `이 파일에서 문제가 발생했습니다. 파일은 그대로 두었습니다.`,
  'Error.UnknownError.Plural': `이 파일들에서 문제가 발생했습니다. 파일은 그대로 두었습니다.`,

  // 0 = shell error code

  // 0 = destination
  'Error.MoveIntoInstaller': `파일을 Windows Installer 폴더로 이동하는 것을 거부합니다(대상: {0}).`,

  // 0 = the relative path the caller passed
  'Error.DestinationNotFullyQualified': `백업 폴더는 드라이브 문자나 네트워크 공유로 시작하는 폴더의 전체 경로여야 합니다(예: D:\\Backup 또는 \\\\server\\backup). InstallerClean은 이 경로를 사용할 수 없습니다: {0}`,
  'BrowserLaunch.FailedTitle': `브라우저를 열 수 없음`,
  'UpdateCheck.Title': `업데이트 확인`,
  'UpdateCheck.Status.Checking': `확인 중...`,
  'UpdateCheck.Status.UpToDate': `최신 버전입니다.`,
  'UpdateCheck.UpdateAvailable.Title': `업데이트 사용 가능`,

  // 0 = installed version, 1 = latest version on GitHub
  'UpdateCheck.UpdateAvailable.Body': `현재 {0} 버전을 사용하고 있습니다.&#10;{1} 버전을 사용할 수 있습니다.`,
  'UpdateCheck.Failed.NetworkUnavailable': `GitHub에 연결할 수 없습니다. 인터넷 연결을 확인하고 다시 시도하세요.`,
  'UpdateCheck.Failed.ServerError': `GitHub가 오류 응답을 반환했습니다. 몇 분 후에 다시 시도하세요.`,
  'UpdateCheck.Failed.ResponseParseError': `GitHub의 응답에 인식할 수 있는 릴리스가 없습니다. 나중에 다시 시도하거나, 릴리스 페이지를 직접 여세요.`,
  'UpdateCheck.Failed.Timeout': `확인 시간이 초과되었습니다. GitHub와의 연결이 느릴 수 있으니 다시 시도하세요.`,
  'UpdateCheck.Failed.Unknown': `알 수 없는 이유로 확인에 실패했습니다. 신고가 필요하면 자세한 내용이 crash.log에 있습니다.`,

  // 0 = the URL the user was trying to reach
  'BrowserLaunch.ClipboardOk': `The link is on your clipboard, so you can paste it in yourself:&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean couldn't copy the link to your clipboard either, so here it is:&#10;&#10;{0}`,

  // 0 = the destination folder whose canonical path changed mid-batch
  'Error.DestinationChangedMidBatch': `InstallerClean이 백업 폴더를 더 이상 확인할 수 없어서, 중단했습니다. {0}을(를) 확인한 다음 다시 검사하고 다시 시도하세요.`,
  // 0 = folder, 1 = inner exception message
  'Error.CannotWriteFolder': `{0}에 쓸 수 없습니다.`,

  // 0 = file name
  'Error.DestinationCollision': `'{0}'(이)라는 이름의 파일이 이미 백업 폴더에 있습니다.`,

  // Result log (post-cleanup diagnostic send)
  'ResultLog.Sending': `보내는 중...`,
  'ResultLog.Sent': `감사합니다! 보고서를 보냈습니다.`,
  'ResultLog.Failed': `보내기에 실패했습니다. 나중에 다시 시도하세요.`,
  'ResultLog.NothingToSend': `보낼 보고서가 없습니다.`,
  'ConfirmSendResultLog.Title': `이 내용을 보내시겠습니까?`,
  'ConfirmSendResultLog.Reassurance': `nofaff.netlify.app/api/result-log으로 전송됩니다. 사용자나 사용자의 컴퓨터를 식별할 수 있는 내용은 전혀 없습니다. 그저 InstallerClean이 잘 작동하는지와 [사람들이 공간을 얼마나 확보하고 있는지] 알 수 있게 해 줄 뿐입니다.`,
  'Automation.ResultLogPreview': `보고서 미리 보기`,

  // Single instance / startup / crash
  'Startup.AlreadyRunningTitle': `InstallerClean`,
  'Startup.AlreadyRunningBody': `이미 실행 중입니다.`,
  'Startup.UnhandledTitle': `InstallerClean`,
  'Startup.UnhandledBody': `예기치 않은 오류가 발생하여 InstallerClean을 닫아야 합니다.\n\n{0}\n\n자세한 내용을 기록한 위치:\n{1}`,
  'Startup.UnhandledBody.NoLog': `예기치 않은 오류가 발생하여 InstallerClean을 닫아야 합니다.\n\n{0}\n\n크래시 로그를 기록할 수 없었습니다.`,
  'Startup.ErrorTitle': `시작 오류`,
  'Startup.FailedToStart': `시작하지 못했습니다 ({0}). 자세한 내용을 기록한 위치:\n{1}`,
  'Startup.FailedToStart.NoLog': `시작하지 못했습니다 ({0}). 크래시 로그를 기록할 수 없었습니다.`,

  // File picker
  'FilePicker.ChooseDestinationTitle': `이동할 파일의 대상 폴더 선택`,

  // Version display

  // 0 = major.minor.patch (e.g. "1.5.4")
  'Version.Display': `버전 {0}`,
  'Plural.File.Singular': `파일`,
  'Plural.File.Plural': `파일`,
  'Plural.Error.Singular': `오류`,
  'Plural.Error.Plural': `오류`,
  'Plural.Package.Singular': `패키지`,
  'Plural.Package.Plural': `패키지`,
  'Plural.Product.Singular': `제품`,
  'Plural.Product.Plural': `제품`,
  'Plural.Patch.Singular': `패치`,
  'Plural.Patch.Plural': `패치`,
  'Display.Size.GB': `{0:F2} GB`,
  'Display.Size.MB': `{0:F1} MB`,
  'Display.Size.KB': `{0:F1} KB`,
  'Display.Size.B': `{0} B`,
  'Display.Elapsed.Ms': `{0:F0}ms`,
  'Display.Elapsed.S': `{0:F1}s`,
  'Display.ListSeparator': `, `,
  'Display.ElapsedLong.LessThanASecond': `1초 미만`,
  'Display.ElapsedLong.Seconds': `{0:F1}초`,
  'CrashLog.PrivacyHeader': `# crash.log에는 InstallerClean의 처리되지 않은 예외가 기록됩니다.\n# 권한이 상승된 상태에서는 프레임워크의 예외 메시지에 실행 중인\n# 세션의 파일 경로가 포함될 수 있습니다(Windows Installer 쿼리가\n# 열거한 다른 사용자의 프로필 포함). 업데이트 확인이나 결과 로그\n# 전송의 네트워크 실패 메시지에는 대상 URL과 확인된 IP 또는 프록시\n# 주소가 포함될 수 있습니다. 읽을 수 없는 Windows Installer 기록에\n# 대한 항목에는 Windows 계정 SID(S-1-5-21-...)와 설치된 소프트웨어의\n# 제품 코드가 포함될 수 있습니다.\n# 이 파일을 공개 버그 신고에 첨부하기 전에 세 가지 정보를 모두\n# 지우세요.\n`,
  'Tooltip.ChangeLanguage': `언어를 변경합니다. 프로그램이 다시 시작됩니다.`,
  'Automation.ChangeLanguage': `언어 변경`,
  'Automation.ChangeLanguage.HelpText': `프로그램이 다시 시작됩니다.`,

  // Command-line tool (installerclean-cli): the HUMAN-facing Cli.* keys.
  // Descriptions translated; command tokens, flags, the {InstallerFolder} token
  // and the exit-code numbers verbatim; leading spaces kept (the screen is
  // column-aligned for a monospace terminal); PATH metavariable -> 경로.
  'Cli.UnknownArgument': `오류: 알 수 없는 인수 '{0}'`,
  'Cli.Cancelling': `취소 중...`,
  'Cli.Cancelled': `취소되었습니다.`,
  'Cli.GenericError': `오류: 예상치 못한 실패({0}). 자세한 내용을 {1}에 기록했습니다.`,
  'Cli.GenericError.NoLog': `오류: 예상치 못한 실패({0}). 크래시 로그를 기록하지 못했습니다.`,
  'Cli.ScanningInstaller': `{InstallerFolder} 검사 중...`,
  'Cli.FoundOrphans': `정리할 불필요한 {1} {0}개를 찾았습니다 ({2}).`,
  'Cli.DeletingFiles': `불필요한 {1} {0}개 삭제 중...`,
  'Cli.DeletedFiles': `불필요한 {1} {0}개를 영구 삭제했습니다.`,
  'Cli.NoMoveDestination': `오류: 이동 대상이 지정되지 않았습니다. /m 경로를 사용하세요. (GUI에서 설정한 기본값은 사용자별로 저장되므로, 예약된 작업이나 서비스 계정 실행에는 적용되지 않습니다.)`,
  'Cli.MoveDestinationInsideInstaller': `오류: 대상은 Windows Installer 폴더 안에 있을 수 없습니다.`,
  'Cli.MoveDestinationRelative': `오류: 대상은 정규화된 전체 경로여야 합니다. 입력값: {0}`,
  'Cli.MoveDestinationInSystemFolder': `오류: 대상 {0}이(가) Windows 시스템 폴더 아래를 가리킵니다. %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)%, %ProgramData% 바깥의 경로를 선택하세요.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `오류: 지금 무언가가 Windows Installer를 사용하고 있습니다. Windows 업데이트이거나 백그라운드에서 설치 중인 프로그램일 수 있습니다. 그동안 /m과 /d는 차단됩니다. 끝나면 다시 시도하세요.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `오류: 이 컴퓨터에 이전 Windows Installer 트랜잭션이 중단된 채 남아 있습니다. {InstallerFolder}를 정리하기 전에 그 설치를 계속하거나 되돌리세요(또는 Windows를 다시 시작하세요).`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `오류: 재시작 후에 처리하도록 대기열에 든 파일 작업이 {InstallerFolder}를 대상으로 합니다({0}). 정리하기 전에 Windows를 다시 시작해 그 작업을 끝내세요.`,
  'Cli.MovingFiles': `불필요한 {1} {0}개를 {2}(으)로 이동 중...`,
  'Cli.MovedFiles': `불필요한 {1} {0}개를 이동했습니다.`,
  'Cli.MutexBlocked': `다른 InstallerClean 프로세스가 단일 인스턴스 잠금을 보유하고 있습니다(GUI 또는 다른 CLI 실행). 종료 코드 75(일시적); 나중에 다시 시도해도 안전합니다.`,
  'Cli.EventLogUnavailable': `참고: 이벤트 로그 쓰기에 실패했습니다. 응용 프로그램 로그 권한 또는 그룹 정책을 확인하세요.`,
  'Cli.Help.Header': `InstallerClean - {InstallerFolder} 정리`,
  'Cli.Help.Usage': `사용법:`,
  'Cli.Help.Help': `  installerclean-cli --help     이 도움말 표시 (/?, -h도 사용 가능)`,
  'Cli.Help.Version': `  installerclean-cli --version  버전 출력 (-v도 사용 가능)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s         검사만 - 불필요한 파일 나열`,
  'Cli.Help.Delete': `  installerclean-cli /d         불필요한 파일 영구 삭제`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m         저장된 백업 폴더로 이동`,
  'Cli.Help.MovePath': `  installerclean-cli /m 경로    지정한 경로로 이동`,
  'Cli.Help.NoteLine1': `installerclean-cli는 끝날 때까지 프롬프트를 붙잡고 있으므로 스크립트나&#10;예약 작업이 이를 기다릴 수 있습니다.`,
  'Cli.Help.ExitCodesHeader': `종료 코드:`,
  'Cli.Help.ExitCodeOk': `  0   성공: 요청한 일을 했고 실패한 것이 없음`,
  'Cli.Help.ExitCodeError': `  1   실패: 아무것도 처리되지 않음 (잘못된 인수나 대상,&#10;       검사 실패 또는 모든 파일 실패)`,
  'Cli.Help.ExitCodePartial': `  2   부분: 일부는 처리되고 일부는 안 됨 (실패 또는 Ctrl+C)`,
  'Cli.Help.ExitCodeTransient': `  75  일시적: 일시적인 상황으로 실행이 차단됨 (메시지 참고)`,
  'Cli.Help.ExitCodeCancelled': `  130 취소됨 (Ctrl+C)`,
  'Body.NotScanned.Lead': `아직 검사하지 않았습니다.`,
  'Body.NotScanned.Why': `다시 검사를 눌러 {InstallerFolder}에서 더 이상 어떤 프로그램도 필요로 하지 않는 설치 관리자 파일을 찾아보세요.`,
  'Confirm.MoveSameDrive': `그 폴더는 같은 드라이브에 있어서, 폴더를 삭제하기 전까지는 공간이 돌아오지 않습니다. 공간을 바로 확보하려면 다른 드라이브의 폴더를 선택하세요.`,
  'Error.ScanCorrelationFailed': `InstallerClean이 Windows Installer 기록을 {InstallerFolder}의 내용과 대조하지 못했습니다. 기록이 가리키는 것 중 실제로 그곳에 있는 것이 거의 없고, 그곳에 있는 것 중 어떤 기록에도 이름이 없는 것이 거의 전부여서, 어떤 파일도 불필요하다고 밝힐 수 없었습니다. 아무것도 제시하지 않았고 아무것도 제거하지 않았습니다.`,
  'Error.CandidateOutsideCache': `이 파일은 Windows Installer 폴더 바로 아래에 있지 않습니다. 안전을 위해 거부했습니다.`,
  'Completion.MoveCancelledSummary': `취소하기 전까지 {2} {1}개 중 {0}개를 이동했습니다.`,
  'Completion.PermanentDeleteCancelledSummary': `취소하기 전까지 {2} {1}개 중 {0}개를 영구 삭제했습니다.`,
  'Body.PendingReboot.Lead': `지금은 이 파일들을 정리할 수 없습니다.`,
  'Cli.TooManyArguments': `오류: 예상치 못한 추가 인수 '{0}'. 대상 폴더 경로에 공백이 있으면 전체 경로를 큰따옴표로 묶으세요: /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `폴더는 사용자별 저장. 예약 또는 SYSTEM 실행에는 /m 경로 필요.`,
  'Error.ScanRecordsUnreadable': `InstallerClean이 무엇이 아직 필요한지 확신할 만큼 Windows Installer 기록을 읽지 못했습니다. 설치된 프로그램 목록이 일부 빠진 채로 돌아왔고, 같은 기록을 레지스트리에서 직접 읽는 것도 오류를 만났습니다. 어떤 파일을 가리키는 기록이 읽을 수 없는 것 중 하나였다는 이유만으로 그 파일이 고립된 것처럼 보일 수 있으므로 InstallerClean은 멈췄습니다. 아무것도 제거되지 않았습니다.`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer가 설치된 프로그램 목록의 끝을 끝내 알리지 않았습니다. InstallerClean은 {2} {3}을(를) 읽은 다음 {0}개 항목에서 포기했습니다(마지막 오류 코드 {1}). 끝이 없는 목록은 믿을 수 없으므로 InstallerClean은 멈췄습니다. 아무것도 제거되지 않았습니다.`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer가 한 프로그램의 패치 목록의 끝을 끝내 알리지 않았습니다. InstallerClean은 {2} {3}을(를) 읽은 다음 {0}개 항목에서 포기했습니다(마지막 오류 코드 {1}). 끝이 없는 목록은 믿을 수 없으므로 InstallerClean은 멈췄습니다. 아무것도 제거되지 않았습니다.`,
  'UpdateCheck.Status.UpdateAvailable': `{0} 버전을 사용할 수 있습니다.`,
  'Completion.DonateAsk': `도움이 되어 기쁩니다. 너그러운 마음이 있으시면 작은 성의도 반갑습니다.`,
  'About.Link.Guide': `안내서 및 자주 묻는 질문`,
  'About.Link.ReportProblem': `문제 신고`,
  'About.AutoUpdateCheck': `자동으로 업데이트 확인`,
  'Automation.About.Guide.HelpText': `브라우저에서 github의 readme를 엽니다.`,
  'Automation.About.ReportProblem.HelpText': `브라우저에서 github.com의 이슈 트래커를 엽니다.`,
  'Automation.AutoUpdateCheck.HelpText': `선택하면 InstallerClean이 실행할 때 github에서 새 버전이 있는지 확인합니다.`,
  'Tooltip.MoveSameDrive': `불필요한 파일을 백업 폴더로 옮깁니다. 같은 드라이브에 있으므로 그 폴더를 삭제하기 전에는 공간이 확보되지 않습니다.`,
  'Confirm.DeletePermanently.Singular': `이 파일은 영구히 삭제됩니다. 안전한 작업이지만 백업을 원하시면 대신 이동을 사용하세요.`,
  'Confirm.DeletePermanently.Plural': `이 파일들은 영구히 삭제됩니다. 안전한 작업이지만 백업을 원하시면 대신 이동을 사용하세요.`,
  'Error.ScanCacheRootUnresolved': `InstallerClean이 Windows로부터 {InstallerFolder}의 실제 경로를 확인받지 못해서, 어떤 파일도 그 안에 있다고 밝힐 수 없었고 정리 대상으로 제시된 파일도 없습니다. 이번 검사가 아무것도 찾지 못한 것은 폴더가 깨끗해서가 아니라 그 확인이 실패했기 때문입니다. 아무것도 제거하지 않았습니다.`,
  'Automation.Scroll.ProductDetails': `제품 세부 정보`,
  'Body.PendingReboot.Other': `Windows Installer가 진행 중인 작업이 있어 이동과 삭제가 일시 중지되었습니다. InstallerClean은 변경 중인 {InstallerFolder}를 건드리지 않습니다. 끝나면 다시 검사하면 두 기능이 돌아옵니다.`,
  'Cli.TooManyArgumentsNoPath': `오류: 예상치 못한 추가 인수 '{0}'. /s와 /d는 다른 인수를 받지 않으며, 한 번 실행에 플래그는 하나만 쓸 수 있습니다.`,
  'Cli.MissingFromDisk.Singular': `Windows에 {InstallerFolder}에 없는 파일 {0}개의 기록이 있습니다: {1}. 평소에는 문제가 없지만 그 프로그램의 업데이트나 제거가 실패할 수 있습니다. 파일을 되돌리려면 지금 사용 중인 버전의 설치 프로그램이 필요합니다. 프로그램 제작사에서 구해 기존 설치본 위에 실행하세요. 더 새 버전으로는 되지 않습니다. 새 버전은 먼저 지금 있는 것을 제거해야 하는데, 바로 그 단계에 이 파일이 필요하기 때문입니다. 먼저 제거하는 방법도 같은 이유로 되지 않습니다. 이렇게 하면 파일이 복원되고 설정은 그대로 남아야 하지만, Microsoft가 보장하지는 않습니다.`,
  'Cli.MissingFromDisk.Plural': `Windows에 {InstallerFolder}에 없는 파일 {0}개의 기록이 있습니다: {1}. 평소에는 문제가 없지만 그 프로그램들의 업데이트나 제거가 실패할 수 있습니다. 파일을 되돌리려면 그 프로그램의 지금 사용 중인 버전의 설치 프로그램이 필요합니다. 프로그램 제작사에서 구해 기존 설치본 위에 실행하세요. 더 새 버전으로는 되지 않습니다. 새 버전은 먼저 지금 있는 것을 제거해야 하는데, 바로 그 단계에 그 파일이 필요하기 때문입니다. 먼저 제거하는 방법도 같은 이유로 되지 않습니다. 이렇게 하면 파일이 복원되고 설정은 그대로 남아야 하지만, Microsoft가 보장하지는 않습니다.`,
  'Cli.MoveNotEnoughSpace': `오류: {0}에 공간이 부족합니다. 이 파일들을 옮기려면 {1}이(가) 필요한데 {2}만 남아 있습니다. 아무것도 이동하지 않았습니다.`,
  'Cli.PendingRebootBlocked.Other': `오류: Windows Installer가 진행 중인 작업이 있어 /m과 /d가 차단되었습니다. InstallerClean은 변경 중인 {InstallerFolder}를 건드리지 않습니다. 끝나면 다시 시도하세요.`,
  'Cli.FoundNoOrphans': `불필요한 파일을 찾지 못했습니다.`,
  'Cli.NothingOffered.Singular': `InstallerClean이 캐시에 있는 어떤 파일이 여기 설치된 프로그램에 속하는지 확실히 알 수 없어서, 파일 하나({2})를 제시하지 않고 보류했습니다.`,
  'Cli.NothingOffered.Plural': `InstallerClean이 캐시에 있는 어떤 파일이 여기 설치된 프로그램에 속하는지 확실히 알 수 없어서, {1} {0}개({2}) 전부를 제시하지 않고 보류했습니다.`,
  'Cli.DestinationChangedMidBatch': `InstallerClean이 백업 폴더를 더 이상 확인할 수 없어서, 중단했습니다. {0}을(를) 확인한 다음 명령을 다시 실행하세요.`,
  'Cli.Help.Summary': `설치된 어떤 프로그램도 더는 필요로 하지 않는 .msi/.msp 파일을 제거합니다.`,
  'Cli.Help.Elevation': `관리자 명령 프롬프트가 필요하며, 아니면 Windows가 실행하지 않습니다.`,
  'Error.InstallerLockUnavailableTitle': `삭제된 파일 없음`,
  'Error.MoveInstallerLockUnavailableTitle': `이동된 파일 없음`,
  'Error.InstallerLockUnavailable': `두 프로그램이 설치된 소프트웨어를 동시에 변경하지 못하도록 Windows Installer가 사용하는 잠금을 InstallerClean이 가져오지 못해서, 작업 도중에 어떤 파일이 필요해지지 않는다고 확신할 수 없었고 아무것도 삭제하지 않았습니다. 다시 시도해 보시고, 계속 이러면 Windows를 다시 시작하세요.`,
  'Error.MoveInstallerLockUnavailable': `두 프로그램이 설치된 소프트웨어를 동시에 변경하지 못하도록 Windows Installer가 사용하는 잠금을 InstallerClean이 가져오지 못해서, 작업 도중에 어떤 파일이 필요해지지 않는다고 확신할 수 없었고 아무것도 이동하지 않았습니다. 다시 시도해 보시고, 계속 이러면 Windows를 다시 시작하세요.`,
  'Cli.InstallerLockUnavailable': `오류: 두 프로그램이 설치된 소프트웨어를 동시에 변경하지 못하도록 하는 Windows Installer 잠금을 InstallerClean이 가져오지 못해서, 작업 도중에 어떤 파일이 필요해지지 않는다고 확신할 수 없었습니다. 아무것도 삭제하지 않았습니다. 다시 시도해 보시고, 계속 이러면 Windows를 다시 시작하세요.`,
  'Cli.MoveInstallerLockUnavailable': `오류: 두 프로그램이 설치된 소프트웨어를 동시에 변경하지 못하도록 하는 Windows Installer 잠금을 InstallerClean이 가져오지 못해서, 작업 도중에 어떤 파일이 필요해지지 않는다고 확신할 수 없었습니다. 아무것도 이동하지 않았습니다. 다시 시도해 보시고, 계속 이러면 Windows를 다시 시작하세요.`,
  'Completion.ReverifyIdentityClaimed': `{1} {0}개를 그대로 두었습니다. 파일 안에 이름이 적힌 프로그램의 기록을 Windows가 가지고 있기 때문입니다.`,
  'Completion.ReverifyIdentityUnreadable': `{1} {0}개를 그대로 두었습니다. InstallerClean이 파일 안에서 프로그램 이름을 찾지 못했기 때문입니다.`,
  'Error.ScanNoRegisteredFileInFolder': `InstallerClean이 Windows Installer 기록을 {InstallerFolder}의 내용과 대조하지 못했습니다. 폴더에 파일은 있지만 그 안의 어떤 것도 가리키는 기록이 하나도 없어서, 어떤 파일도 불필요하다고 밝힐 수 없었습니다. 아무것도 제시하지 않았고 아무것도 제거하지 않았습니다.`,
  'Completion.NothingOffered': `이 PC에서는 아무것도 제시하지 않았습니다`,
  'Completion.NothingOfferedBody.Singular': `InstallerClean이 캐시에 있는 어떤 파일이 여기 설치된 프로그램에 속하는지 확실히 알 수 없어서, 파일 하나({2})를 제시하지 않고 보류했습니다.`,
  'Completion.NothingOfferedBody.Plural': `InstallerClean이 캐시에 있는 어떤 파일이 여기 설치된 프로그램에 속하는지 확실히 알 수 없어서, {1} {0}개({2}) 전부를 제시하지 않고 보류했습니다.`,
  'Summary.SupersededHeldBack.Singular': `InstallerClean이 대체된 그 파일 하나가 더 이상 필요하지 않다고 확실히 알 수 없어서, 그 파일을 보류했습니다.`,
  'Summary.SupersededHeldBack.Plural': `InstallerClean이 대체된 파일 {0}개가 더 이상 필요하지 않다고 확실히 알 수 없어서, 그 파일들을 보류했습니다.`,
  'Cli.SupersededHeldBack.Singular': `InstallerClean이 대체된 그 파일 하나가 더 이상 필요하지 않다고 확실히 알 수 없어서, 그 파일을 보류했습니다.`,
  'Cli.SupersededHeldBack.Plural': `InstallerClean이 대체된 파일 {0}개가 더 이상 필요하지 않다고 확실히 알 수 없어서, 그 파일들을 보류했습니다.`,
  'Completion.HeldBack.Singular': `{0}개 파일을 보류했습니다. 검사는 필요 없다고 했지만, 최종 확인은 그것을 확인해 주지 못했습니다.`,
  'Completion.HeldBack.Plural': `{0}개 파일을 보류했습니다. 검사는 필요 없다고 했지만, 최종 확인은 그것을 확인해 주지 못했습니다.`,
  'Body.PendingReboot.PendingRenameUnresolved': `다음 재시작을 위해 예약된 파일 작업이 있는데 InstallerClean은 그 작업이 어떤 파일을 가리키는지 알 수 없으므로, 그 파일들이 {InstallerFolder}에 있지 않다고 단정할 수 없습니다. 정리하기 전에 Windows를 다시 시작하세요.`,
  'Completion.MoveRestoreHint': `모든 것이 괜찮다고 확신하게 되면 그 폴더를 삭제하세요.`,
  'Completion.MoveRestoreHintSameDrive': `모든 것이 괜찮다고 확신하게 되면 그 폴더를 삭제하세요. 그때까지는 공간이 실제로 확보되지 않습니다.`,
  'Confirm.MoveDestination.Singular': `이 파일을 다음 위치로 옮깁니다:`,
  'Confirm.MoveDestination.Plural': `이 파일들을 다음 위치로 옮깁니다:`,
  'Cli.NothingListed.Singular': `InstallerClean이 캐시에 있는 어떤 파일이 여기 설치된 프로그램에 속하는지 확실히 알 수 없어서, 파일 하나({2})를 제시하지 않고 보류했습니다.`,
  'Cli.NothingListed.Plural': `InstallerClean이 캐시에 있는 어떤 파일이 여기 설치된 프로그램에 속하는지 확실히 알 수 없어서, {1} {0}개({2})를 제시하지 않고 보류했습니다.`,
  'Cli.WithheldReasons.Header': `확신할 수 없었던 이유:`,
  'Cli.WithheldReasons.RecordedPath': `  Windows Installer 자체 기록에 있는 파일 경로를 확인할 수 없어서, 그 경로에 아무것도 대조할 수 없었습니다.`,
  'Cli.WithheldReasons.FileIdentity': `  Windows에 기록이 있는 파일 하나의 신원을 확인할 수 없어서, 폴더에 있는 것과 대조할 수 없었습니다.`,
  'Cli.WithheldReasons.SecondInstance': `  이 PC에 같은 프로그램이 두 번 이상 설치되어 있을 수 있고, 기록만으로는 파일이 어느 사본에 속하는지 알 수 없습니다.`,
  'Cli.PendingRebootBlocked.PendingRenameUnresolved': `오류: 다음 재시작을 위해 예약된 파일 작업이 있는데 InstallerClean은 그 작업이 어떤 파일을 가리키는지 알 수 없으므로, {InstallerFolder}를 배제할 수 없습니다. 정리하기 전에 Windows를 다시 시작하세요.`,
  'Cli.MoveRestoreHint': `프로그램이 여전히 정상적으로 업데이트되고 제거되는지 확인한 다음 {0}을(를) 삭제하세요.`,
  'Error.ScanStoppedDetails': `이 내용은 {0}에도 기록됩니다.`,
  'Cli.NothingListedPerFile.Singular': `InstallerClean이 찾은 캐시 파일 중 하나에 대해 확신할 수 없어서, 그 파일({2})을 제시하지 않고 보류했습니다.`,
  'Cli.NothingListedPerFile.Plural': `InstallerClean이 찾은 캐시 파일 중 일부에 대해 확신할 수 없어서, {1} {0}개({2})를 제시하지 않고 보류했습니다.`,
  'Cli.NothingOfferedPerFile.Singular': `InstallerClean이 찾은 캐시 파일이 필요 없다는 것을 확인하지 못해서, 그 파일 하나({2})를 제시하지 않고 보류했습니다.`,
  'Cli.NothingOfferedPerFile.Plural': `InstallerClean이 찾은 캐시 파일 중 어느 것도 필요 없다는 것을 확인하지 못해서, {1} {0}개({2}) 전부를 제시하지 않고 보류했습니다.`,
  'Completion.NothingOfferedPerFileBody.Singular': `InstallerClean이 찾은 캐시 파일이 필요 없다는 것을 확인하지 못해서, 그 파일 하나({2})를 제시하지 않고 보류했습니다.`,
  'Completion.NothingOfferedPerFileBody.Plural': `InstallerClean이 찾은 캐시 파일 중 어느 것도 필요 없다는 것을 확인하지 못해서, {1} {0}개({2}) 전부를 제시하지 않고 보류했습니다.`,
  'Summary.NothingListedPerFile.Singular': `InstallerClean이 찾은 캐시 파일 중 하나에 대해 확신할 수 없어서, 그 파일을 제시하지 않고 보류했습니다.`,
  'Summary.NothingListedPerFile.Plural': `InstallerClean이 찾은 캐시 파일 중 일부에 대해 확신할 수 없어서, {1} {0}개를 제시하지 않고 보류했습니다.`,
  'Cli.WithheldReasons.CandidateIdentity': `  폴더에 있는 파일 하나의 신원을 확인할 수 없어서, 기록과 대조할 수 없었습니다.`,
  'Cli.WithheldReasons.DeclaredProductInstalled': `  어떤 파일이 아직 설치되어 있는 프로그램에 속한다고 밝히고 있어서, 아직 필요할 수 있습니다.`,
  'Cli.WithheldReasons.DeclaredProductUnestablished': `  어떤 파일이 어느 프로그램에 속하는지 밝히지 않았거나, Windows가 그 프로그램에 대해 답하지 않았습니다.`,
  'Cli.WithheldReasons.ScreenUnanswered': `  파일이 어느 프로그램에 속하는지 확인하는 검사가, 건네받은 파일과 맞지 않는 답을 내놓았습니다.`,
  'Body.PendingReboot.RegistryCheckUnreadable': `InstallerClean이 {InstallerFolder}를 건드리기 전에 확인하는 Windows 설정 중 하나를 읽을 수 없어서, 설치 작업이 실행 중인지 재시작을 기다리는지 알 수 없습니다. Windows를 다시 시작한 뒤 다시 검사하세요. 그래도 설정을 읽을 수 없다면, 이 PC는 InstallerClean이 정리할 수 있는 PC가 아닙니다.`,
  'Cli.InstallerLockAccessRefused': `오류: Windows가 InstallerClean에 Windows Installer가 사용 중인지 확인할 권한을 주지 않아, 도중에 파일이 필요해질 가능성을 배제할 수 없었습니다. 아무것도 삭제되지 않았습니다.`,
  'Cli.MoveCancelledRestoreHint': `되돌리기는 간단합니다. {0}에서 {InstallerFolder}로 다시 옮기면 모든 것이 원래대로 돌아갑니다.`,
  'Cli.MoveInstallerLockAccessRefused': `오류: Windows가 InstallerClean에 Windows Installer가 사용 중인지 확인할 권한을 주지 않아, 도중에 파일이 필요해질 가능성을 배제할 수 없었습니다. 아무것도 이동되지 않았습니다.`,
  'Cli.PendingRebootBlocked.RegistryCheckUnreadable': `오류: InstallerClean이 {InstallerFolder}를 건드리기 전에 확인하는 레지스트리 값 중 하나를 읽을 수 없어서, 진행 중이거나 다음 재시작을 위해 예약된 Windows Installer 작업을 배제할 수 없습니다. /m과 /d가 차단되었습니다. Windows를 다시 시작한 뒤 다시 시도하세요. 그래도 읽기에 실패하면, 이 PC는 InstallerClean이 정리할 수 있는 PC가 아닙니다.`,
  'Completion.MoveCancelledRestoreHint': `되돌리기는 간단합니다. {InstallerFolder}로 다시 옮기면 모든 것이 원래대로 돌아갑니다.`,
  'Error.InstallerLockAccessRefused': `Windows가 InstallerClean에 Windows Installer가 사용 중인지 확인할 권한을 주지 않아, 도중에 파일이 필요해질 가능성을 배제할 수 없었고, 아무것도 삭제되지 않았습니다.`,
  'Error.MoveInstallerLockAccessRefused': `Windows가 InstallerClean에 Windows Installer가 사용 중인지 확인할 권한을 주지 않아, 도중에 파일이 필요해질 가능성을 배제할 수 없었고, 아무것도 이동되지 않았습니다.`,
  'Error.MoveStoppedTitle': `이동 중지됨`,
  'Field.NoNamedProduct': `(프로그램 없음)`,
  'Summary.RegisteredWindow.Missing.Plural': `{0}개 누락`,
  'Summary.RegisteredWindow.Missing.Singular': `{0}개 누락`,
  'UpdateCheck.Failed.Unknown.NoLog': `알 수 없는 이유로 확인에 실패했습니다. 크래시 로그를 기록할 수 없었습니다.`,
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

// Remove ONLY the machine-contract Cli.* <data> elements BY NAME (the
// Cli.EventLog* set bar Cli.EventLogUnavailable).
const isMachineCliKey = (k) =>
  k.startsWith('Cli.') && k.includes('EventLog') && k !== 'Cli.EventLogUnavailable';
let cliMachineRemoved = 0;
text = text.replace(/[^\S\n]*<data name="(Cli\.[^"]*)"[\s\S]*?<\/data>\n?/g,
  (m, name) => { if (isMachineCliKey(name)) { cliMachineRemoved++; return ''; } return m; });

// Replace each key's inner <value> from MAP.
const escRe = (s) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
const notApplied = [];
for (const [key, val] of Object.entries(MAP)) {
  const re = new RegExp('(<data name="' + escRe(key) + '"[^>]*>\\s*<value>)([\\s\\S]*?)(</value>)');
  let applied = false;
  text = text.replace(re, (m, p1, p2, p3) => { applied = true; return p1 + val + p3; });
  if (!applied) notApplied.push(key);
}

// Normalise to LF with exactly one trailing newline.
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
// Derived, never pinned: the machine set grows whenever the command line
// gains an event-log string, and a literal here would fail every generator
// at once while asserting nothing about what was actually stripped.
const cliMachineExpected = [...neutral.keys()].filter(isMachineCliKey).length;
const written = readFileSync(OUT, 'utf8');
const output = parse(written, OUT);
const neutralRequired = [...neutral.keys()].filter((k) => !isMachineCliKey(k));

const missingFromMap = neutralRequired.filter((k) => !(k in MAP));
const strayMapKeys = Object.keys(MAP).filter((k) => !neutral.has(k));
const machineLeaked = [...output.keys()].filter(isMachineCliKey);

// The one human-facing Cli.EventLog* key, asserted present rather than left to
// the counts: a predicate that stopped discriminating it takes it out of the
// output AND out of the required set, so every figure above still agrees. The
// MAP substitution notices today only through the order the two run in.
const humanCliStripped = !output.has('Cli.EventLogUnavailable');
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
console.log('machine Cli <data> removed:', cliMachineRemoved, `(expect ${cliMachineExpected})`);
console.log('MAP entries:', Object.keys(MAP).length, '| CRLF:', crlf, '(expect 0)');

if (alsoKeep.size) {
  console.log('ALSO_KEEP (' + alsoKeep.size + '), kept identical to English:');
  for (const k of alsoKeep) {
    const v = output.get(k);
    const words = v == null ? 0 : v.replace(/\{\d+(?::[^}]*)?\}/g, ' ').trim().split(/\s+/).filter(Boolean).length;
    const suspicious = v != null && (words > 2 || v.length > 24);
    console.log('   ' + (suspicious ? '!! suspicious (longer than a word or two) ' : '') + k + ' = ' + JSON.stringify(v));
  }
}

if (notApplied.length) console.log('!! value not applied (regex miss):', notApplied);
if (missingFromMap.length) console.log('!! in neutral but missing from MAP:', missingFromMap);
if (strayMapKeys.length) console.log('!! in MAP but not in neutral:', strayMapKeys);
if (missingFromOutput.length) console.log('!! required key missing from output:', missingFromOutput);
if (arityMismatch.length) console.log('!! placeholder arity differs from neutral:', arityMismatch);
if (machineLeaked.length) console.log('!! machine Cli keys leaked into output:', machineLeaked);
if (humanCliStripped) console.log('!! Cli.EventLogUnavailable stripped: that key is human-facing and must stay');
if (untranslated.length) {
  const show = untranslated.slice(0, 40).join(', ');
  console.log('!! still English (untranslated), ' + untranslated.length + ': ' + show +
    (untranslated.length > 40 ? ', ...and ' + (untranslated.length - 40) + ' more' : ''));
  if (untranslated.length > 50)
    console.log('   (that is most of the file: this is the untranslated template. Translate the MAP values, then a real miss is listed on its own.)');
}

const structuralOk = !notApplied.length && !missingFromMap.length && !strayMapKeys.length &&
  !missingFromOutput.length && !arityMismatch.length && !machineLeaked.length &&
  !humanCliStripped &&
  output.size === neutralRequired.length && cliMachineRemoved === cliMachineExpected && crlf === 0;
const ok = structuralOk && !untranslated.length;
console.log(ok ? '\nGENERATION OK' : '\nGENERATION HAS ISSUES (see above)');
