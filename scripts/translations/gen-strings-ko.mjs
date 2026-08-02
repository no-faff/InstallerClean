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

// Universal keeps: keys whose value is the same in every language (brand names,
// the pure-placeholder string, the size/elapsed format strings). Their still-
// English value is NOT a miss. Do NOT translate these values. Do NOT edit this
// list per language.
const KEEP_ENGLISH = new Set([
  'Window.Main.Title',                 // InstallerClean
  'Startup.AlreadyRunningTitle',       // InstallerClean
  'Startup.UnhandledTitle',            // InstallerClean
  'Automation.ScanResultAnnouncement', // {0} ({1})
  'Display.Size.GB',                   // {0:F2} GB
  'Display.Size.MB',                   // {0:F1} MB
  'Display.Size.KB',                   // {0:F1} KB
  'Display.Size.B',                    // {0} B
  'Display.Elapsed.Ms',                // {0:F0}ms
  'Display.Elapsed.S',                 // {0:F1}s
]);

// Per-language keeps: empty for Korean. 패치 is a Hangul translation of "patch",
// not an English keep.
const ALSO_KEEP = [];

const MAP = {
  // Window titles
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `정보`,
  'Window.Registered.Title': `삭제하지 말아야 할 등록된 파일`,
  'Window.Orphaned.Title': `안전하게 삭제할 수 있는 불필요한 파일`,

  // Section headings
  'Section.Registered.Products': `제품`,
  'Section.Registered.Patches': `패치`,
  'Section.Registered.Details': `제품 세부 정보`,
  'Section.Backup.Folder': `BACKUP FOLDER`,
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
  'Action.BackupFolderPlaceholder': `Path to folder if you move rather than delete.`,
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
  'Automation.ConfirmDelete': `Delete permanently removes the unneeded files. Cancel closes without deleting.`,
  'Automation.ConfirmMove': `이동하면 불필요한 파일이 선택한 대상 폴더에 들어갑니다. 취소하면 파일은 있던 자리에 그대로 남습니다.`,
  'Automation.SayThanks': `감사 인사`,
  'Automation.ConfirmSendResultLog': `보내기를 누르면 표시된 보고서가 No Faff에 전송됩니다. 취소하면 아무것도 보내지 않습니다.`,
  'Automation.CheckForUpdates': `업데이트 확인`,
  'Automation.CheckForUpdates.HelpText': `github의 릴리스 페이지에서 새 버전이 있는지 확인합니다.`,
  'Automation.UpdateAvailable.HelpText': `새 버전을 내려받으려면 릴리스 페이지를 열고, 현재 버전을 유지하려면 취소하세요.`,
  'Automation.Licence.HelpText': `브라우저에서 github.com의 라이선스 파일을 엽니다.`,
  'Automation.Section.BackupFolder': `Backup folder`,
  'Automation.Section.Products': `제품`,
  'Automation.Section.Patches': `패치`,
  'Automation.Section.ProductDetails': `제품 세부 정보`,
  'Automation.BackupFolder': `Backup folder`,
  'Automation.OperationProgress': `작업 진행 상황`,
  'Automation.RescanInstaller': `{InstallerFolder} 다시 검사`,
  'Automation.ScanningProgress': `검사 진행 상황`,
  'Automation.StartupScanProgress': `시작 검사 진행 상황`,
  'Automation.ViewOrphanedFiles': `세부 정보, 불필요한 파일`,
  'Automation.ViewOrphanedFiles.HelpText': `정리할 수 있습니다.`,
  'Automation.ViewRegisteredFiles': `세부 정보, 등록된 파일`,
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
  'Tooltip.Move': `Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them.`,
  'Tooltip.MoveNeedsDestination': `Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them.`,
  'Tooltip.Delete': `Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away.`,
  'Tooltip.SigningCertificate': `내장된 Authenticode 인증서의 주체 이름입니다. 인증서 체인은 검증하지 않았습니다.`,

  // Body copy
  'Body.MainExplanation.Lead': `Any unneeded files below are [safe to delete].`,
  'Body.MainExplanation.Why': `이 파일들은 {InstallerFolder}에 있으며, 프로그램을 제거했거나({0}), 새 패치가 옛 패치를 대체했거나({1}), 게시자가 철회했을 때({2}) 남겨진 것입니다. InstallerClean은 Windows 자체가 다 썼다고 보고하는 파일만 나열합니다.`,
  'Body.MainExplanation.Action': `Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored.`,
  'Body.PendingReboot.MsiExecuteMutex': `Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back.`,
  'Body.PendingReboot.InstallerInProgress': `A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}.`,
  'Body.PendingReboot.PendingRenameInCache': `Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning.`,
  'Body.NoFileSelected': `세부 정보를 보려면 파일을 선택하세요.`,
  'Body.NoProductSelected': `세부 정보를 보려면 제품을 선택하세요.`,
  'Body.NoMetadata': `사용할 수 있는 메타데이터가 없습니다.`,
  'Body.RegisteredMissingFromDisk': `This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.&#10;&#10;It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.&#10;&#10;To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself.`,
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
  'Status.Moving': `Moving unneeded files...`,
  'Status.Deleting': `Deleting unneeded files...`,
  'Status.MoveCancelled.Partial': `이동이 취소되었습니다. {2} {1}개 중 {0}개를 처리했습니다.`,
  'Status.DeleteCancelled.Partial': `삭제가 취소되었습니다. {2} {1}개 중 {0}개를 처리했습니다.`,
  'Status.MoveFailed': `이동 실패 ({0}). 자세한 내용은 {1}에 있습니다.`,
  'Status.MoveFailed.NoLog': `이동 실패 ({0}). 크래시 로그를 기록할 수 없었습니다.`,
  'Status.DeleteFailed': `삭제 실패 ({0}). 자세한 내용은 {1}에 있습니다.`,
  'Status.DeleteFailed.NoLog': `삭제 실패 ({0}). 크래시 로그를 기록할 수 없었습니다.`,
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
  'Completion.PermanentDeleteSummary.Singular': `{0} {1} permanently deleted`,
  'Completion.PermanentDeleteSummary.Plural': `{0} {1} permanently deleted`,

  // Summaries
  'Summary.RegisteredStillUsed.Singular': `아직 필요한 파일 {0}개`,
  'Summary.RegisteredStillUsed.Plural': `아직 필요한 파일 {0}개`,
  'Summary.OrphanedToCleanUp.Singular': `정리할 불필요한 파일 {0}개`,
  'Summary.OrphanedToCleanUp.Plural': `정리할 불필요한 파일 {0}개`,
  'Summary.MissingFromDisk.Singular': `등록된 파일 {0}개가 누락되었습니다(InstallerClean이 삭제한 것은 아닙니다). 지금은 문제가 없지만, 나중에 해당 프로그램을 복구, 업데이트 또는 제거할 때 실패할 수 있습니다. 어떻게 해야 할지는 세부 정보를 여세요.`,
  'Summary.MissingFromDisk.Plural': `등록된 파일 {0}개가 누락되었습니다(InstallerClean이 삭제한 것은 아닙니다). 지금은 문제가 없지만, 나중에 해당 프로그램들을 복구, 업데이트 또는 제거할 때 실패할 수 있습니다. 어떻게 해야 할지는 세부 정보를 여세요.`,

  // 0 = current file count, 1 = total count, 2 = pluralised noun.
  'Summary.OperationFiles': `{2} {1}개 중 {0}개`,

  // Orphaned-window footer. 0 = orphaned count, 1 = superseded count,
  // 2 = obsoleted count, 3 = size display.
  'Summary.OrphanedWindow': `고립됨 {0}개, 대체됨 {1}개, 폐기됨 {2}개 ({3})`,

  // Registered-window footer. 0 = count, 1 = size display.
  'Summary.RegisteredWindow.Singular': `아직 필요한 등록된 파일 {0}개 ({1})`,
  'Summary.RegisteredWindow.Plural': `아직 필요한 등록된 파일 {0}개 ({1})`,

  // Confirmation dialogs

  // 0 = file count, 1 = pluralised "file"/"files", 2 = size display
  'Confirm.MoveTitle': `{1} {0}개를 이동하시겠습니까? ({2})`,

  // 0 = destination path
  'Confirm.MoveDestination': `파일이 다음 위치로 이동됩니다:`,
  'Confirm.DeleteTitle': `{1} {0}개를 삭제하시겠습니까? ({2})`,

  // Error messages
  'Error.AdminRequiredTitle': `액세스 거부됨`,
  'Error.AdminRequiredBody': `Windows가 InstallerClean의 접근을 거부해서 작업을 멈췄습니다. 아무것도 제거되지 않았습니다.\n\nInstallerClean은 이미 관리자 권한으로 실행 중이었으므로 그런 식으로 다시 시작해도 도움이 되지 않습니다. Windows는 무엇이 접근을 거부했는지 더 이상 알려주지 않으므로 구체적으로 시도해 볼 것이 없습니다.`,
  'Error.InstallerDbUnavailableTitle': `Windows Installer 기록을 읽을 수 없습니다`,
  'Error.ScanFailedTitle': `검사 실패`,
  'Error.InstallerDbEmpty': `Windows Installer 기록이 완전히 비어서 돌아왔습니다. 설치된 프로그램도, 업데이트도 캐시된 설치 파일을 하나도 요구하지 않습니다. 정상적으로 작동하는 컴퓨터에서는 이런 일이 없으므로(갓 설치한 Windows에도 그런 파일이 있습니다) 기록이 손상되었거나 읽을 수 없었던 것이고, 이 답을 그대로 믿은 검사는 {InstallerFolder}의 모든 파일을 잘못 고립된 것으로 판단했을 것입니다. InstallerClean은 그러지 않고 멈췄습니다. 아무것도 제거되지 않았습니다.`,
  'Error.MsiAccessDenied': `Windows Installer가 InstallerClean에게 설치된 항목의 목록 표시를 허용하지 않았습니다. InstallerClean은 이미 관리자 권한으로 실행 중이었으므로 관리자 권한으로 다시 실행해도 달라지는 것이 없습니다. 그 목록이 없으면 캐시된 파일 중 어느 것이 아직 필요한지 안전하게 알아낼 방법이 없으므로 InstallerClean은 멈췄습니다. 아무것도 제거되지 않았습니다.`,
  'Error.MsiNonSuccess': `Windows Installer가 InstallerClean에게 읽을 수 있는 설치된 프로그램 목록을 주지 못했습니다. {0}개 항목이 연속으로 읽을 수 없는 상태로 돌아왔습니다(마지막 오류 코드 {1}). 일부만 읽은 목록으로 작업하는 대신 InstallerClean은 멈췄습니다. 아무것도 제거되지 않았습니다.`,
  'Error.InvalidDestinationTitle': `잘못된 대상`,
  'Error.DestinationWriteFailedTitle': `대상에 쓸 수 없음`,
  'Error.MoveFailedTitle': `이동 실패`,
  'Error.DeleteFailedTitle': `삭제 실패`,
  'Error.SettingNotSavedTitle': `설정 저장 실패`,
  'Error.SettingNotSavedBody': `변경 내용을 저장하지 못했습니다. 다음에 실행할 때 InstallerClean은 이전 설정으로 돌아갑니다.`,
  'Error.DestinationInsideInstaller': `대상은 Windows Installer 폴더 안에 있을 수 없습니다.`,

  // 0 = the destination path the user typed
  'Error.DestinationInSystemFolder': `지정한 대상이 Windows 시스템 폴더 아래로 해석됩니다 ({0}). %SystemRoot%, %ProgramFiles%, %ProgramData% 밖의 경로를 선택하세요.`,
  'Error.NotEnoughSpaceTitle': `공간 부족`,

  // 0 = destination, 1 = required size, 2 = available size
  'Error.NotEnoughSpaceBody': `{0}에 공간이 부족합니다\n\n필요: {1}\n사용 가능: {2}`,

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
  'Error.FileInUse.Singular': `This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later.`,
  'Error.FileInUse.Plural': `These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later.`,
  'Error.IOFailure.Singular': `Windows가 파일 오류를 알렸습니다. 파일은 그대로 두었습니다.`,
  'Error.IOFailure.Plural': `Windows가 파일 오류를 알렸습니다. 이 파일들은 그대로 두었습니다.`,
  'Error.UnknownError.Singular': `이 파일에서 문제가 발생했습니다. 파일은 그대로 두었습니다.`,
  'Error.UnknownError.Plural': `이 파일들에서 문제가 발생했습니다. 파일은 그대로 두었습니다.`,

  // 0 = shell error code

  // 0 = destination
  'Error.MoveIntoInstaller': `파일을 Windows Installer 폴더로 이동하는 것을 거부합니다(대상: {0}).`,

  // 0 = the relative path the caller passed
  'Error.DestinationNotFullyQualified': `The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\\Backup, or \\\\server\\backup). InstallerClean can't use this one: {0}`,
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
  'BrowserLaunch.ClipboardOk': `InstallerClean이 브라우저를 열지 못했습니다. 링크를 클립보드에 복사해 두었으니 직접 붙여넣으시면 됩니다:&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean이 브라우저를 열지 못했고, 링크를 클립보드에 복사하지도 못했습니다. 링크는 다음과 같습니다:&#10;&#10;{0}`,

  // 0 = the destination folder whose canonical path changed mid-batch
  'Error.DestinationChangedMidBatch': `The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again.`,

  // 0 = folder, 1 = inner exception message
  'Error.CannotWriteFolder': `{0}에 쓸 수 없습니다.`,

  // 0 = file name
  'Error.NoUniqueFilename': `10,000번 시도한 후에도 '{0}'에 대한 고유한 파일 이름을 찾을 수 없었습니다.`,

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
  'Startup.AlreadyRunningBody': `InstallerClean이 이미 실행 중입니다.`,
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
  'Display.ElapsedLong.LessThanASecond': `1초 미만`,
  'Display.ElapsedLong.Seconds': `{0:F1}초`,
  'CrashLog.PrivacyHeader': `# crash.log captures unhandled exceptions from InstallerClean.\n# Under elevation the framework's exception messages can include\n# file paths from the running session (including other users'\n# profiles enumerated by Windows Installer queries). Network-\n# failure messages from the update check or result-log POST can\n# include the destination URL and the resolved IP / proxy address.\n# Entries about unreadable Windows Installer records can include a\n# Windows account SID (S-1-5-21-...) and the product codes of\n# installed software.\n# Redact all three classes of detail before attaching this file to\n# a public bug report.\n`,
  'Tooltip.ChangeLanguage': `언어를 변경합니다. 프로그램이 다시 시작됩니다.`,
  'Automation.ChangeLanguage': `언어 변경`,
  'Automation.ChangeLanguage.HelpText': `프로그램이 다시 시작됩니다.`,

  // Command-line tool (installerclean-cli): the HUMAN-facing Cli.* keys.
  // Descriptions translated; command tokens, flags, the {InstallerFolder} token
  // and the exit-code numbers verbatim; leading spaces kept (the screen is
  // column-aligned for a monospace terminal); PATH metavariable -> 경로.
  'Cli.UnknownArgument': `알 수 없는 인수: '{0}'`,
  'Cli.Cancelling': `취소 중...`,
  'Cli.Cancelled': `취소되었습니다.`,
  'Cli.GenericError': `오류: {0}. 자세한 내용을 {1}에 기록했습니다.`,
  'Cli.GenericError.NoLog': `오류: {0}. 크래시 로그를 기록할 수 없었습니다.`,
  'Cli.ScanningInstaller': `{InstallerFolder} 검사 중...`,
  'Cli.FoundOrphans': `정리할 {1} {0}개를 찾았습니다 ({2}).`,
  'Cli.NothingToDo': `수행할 작업이 없습니다.`,
  'Cli.DeletingFiles': `{1} {0}개를 삭제하는 중...`,
  'Cli.DeletedFiles': `Permanently deleted {0} unneeded {1}.`,
  'Cli.NoMoveDestination': `오류: 이동 대상이 지정되지 않았습니다. /m 경로를 사용하세요. (GUI에서 설정한 기본값은 사용자별로 저장되므로, 예약된 작업이나 서비스 계정 실행에는 적용되지 않습니다.)`,
  'Cli.MoveDestinationInsideInstaller': `오류: 대상은 Windows Installer 폴더 안에 있을 수 없습니다.`,
  'Cli.MoveDestinationRelative': `오류: 대상은 정규화된 전체 경로여야 합니다. 입력값: {0}`,
  'Cli.MoveDestinationInSystemFolder': `오류: 지정한 대상이 Windows 시스템 폴더 아래로 해석됩니다 ({0}). %SystemRoot%, %ProgramFiles%, %ProgramData% 밖의 경로를 선택하세요.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}.`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning.`,
  'Cli.MovingFiles': `{1} {0}개를 {2}(으)로 이동하는 중...`,
  'Cli.MovedFiles': `{1} {0}개를 이동했습니다.`,
  'Cli.MutexBlocked': `다른 InstallerClean 프로세스가 단일 인스턴스 잠금을 보유하고 있습니다(GUI 또는 다른 CLI 실행). 종료 코드 75(일시적); 나중에 다시 시도해도 안전합니다.`,
  'Cli.EventLogUnavailable': `참고: 이벤트 로그 쓰기에 실패했습니다. 응용 프로그램 로그 권한 또는 그룹 정책을 확인하세요.`,
  'Cli.Help.Header': `InstallerClean - {InstallerFolder} 정리`,
  'Cli.Help.Usage': `사용법:`,
  'Cli.Help.Help': `  installerclean-cli --help     이 도움말 표시 (/?, -h도 사용 가능)`,
  'Cli.Help.Version': `  installerclean-cli --version  버전 출력 (-v도 사용 가능)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s         Scan only - list unneeded files`,
  'Cli.Help.Delete': `  installerclean-cli /d         Delete unneeded files permanently`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m         Move to the saved backup folder`,
  'Cli.Help.MovePath': `  installerclean-cli /m 경로    지정한 경로로 이동`,
  'Cli.Help.NoteLine1': `installerclean-cli는 실제 콘솔 프로세스로, 명령 프롬프트를 점유하며,`,
  'Cli.Help.NoteLine2': `실행이 끝날 때까지 기다립니다. 다른 콘솔 앱처럼 출력을 리디렉션하거나`,
  'Cli.Help.NoteLine3': `파이프로 넘길 수 있습니다. GUI는 옆의 InstallerClean.exe입니다.`,
  'Cli.Help.ExitCodesHeader': `종료 코드:`,
  'Cli.Help.ExitCodeOk': `  0   성공: 표시된 모든 파일을 처리함`,
  'Cli.Help.ExitCodeError': `  1   실패: 처리된 파일 없음 (잘못된 인수, 검사 실패, 모든 파일 실패)`,
  'Cli.Help.ExitCodePartial': `  2   부분 처리: 일부 파일은 처리됨, 일부는 실패`,
  'Cli.Help.ExitCodeTransient': `  75  일시적: 일시적인 상황으로 실행이 차단됨 (메시지 참고)`,
  'Cli.Help.ExitCodeCancelled': `  130 취소됨 (Ctrl+C)`,
  'Body.NotScanned.Lead': `아직 검사하지 않았습니다.`,
  'Body.NotScanned.Why': `다시 검사를 눌러 {InstallerFolder}에서 더 이상 어떤 프로그램도 필요로 하지 않는 설치 관리자 파일을 찾아보세요.`,
  'Confirm.MoveSameDrive': `That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away.`,
  'Error.ScanCorrelationFailed': `InstallerClean couldn't square this scan with the Windows Installer records: nearly every file Windows still lists as needed is missing from {InstallerFolder}, while what is actually in the folder matches almost nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed.`,
  'Error.CandidateOutsideCache': `이 파일은 Windows Installer 폴더 바로 아래에 있지 않습니다. 안전을 위해 거부했습니다.`,
  'Completion.ReverifySkipped': `{1} {0}개는 검사 이후 어떤 프로그램이 다시 필요로 하게 되어 그대로 두었습니다.`,
  'Completion.MoveCancelledSummary': `취소하기 전까지 {2} {1}개 중 {0}개를 이동했습니다.`,
  'Completion.PermanentDeleteCancelledSummary': `취소하기 전까지 {2} {1}개 중 {0}개를 영구 삭제했습니다.`,
  'Body.PendingReboot.Lead': `지금은 이 파일들을 정리할 수 없습니다.`,
  'Cli.TooManyArguments': `오류: 예상치 못한 추가 인수 '{0}'. 이동 폴더 경로에 공백이 있으면 전체 경로를 큰따옴표로 묶으세요: /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `That folder is saved per-user; scheduled or SYSTEM runs need /m PATH.`,
  'Completion.ReverifyIncomplete': `{1} {0}개는 확인을 다시 했을 때 Windows Installer 기록을 완전히 읽을 수 없어 그대로 두었습니다.`,
  'Summary.ProgramsUnreadable.Singular': `Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again.`,
  'Summary.ProgramsUnreadable.Plural': `Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again.`,
  'Error.ScanRecordsUnreadable': `InstallerClean이 무엇이 아직 필요한지 확신할 만큼 Windows Installer 기록을 읽지 못했습니다. 설치된 프로그램 목록이 일부 빠진 채로 돌아왔고, 같은 기록을 레지스트리에서 직접 읽는 것도 오류를 만났습니다. 어떤 파일을 가리키는 기록이 읽을 수 없는 것 중 하나였다는 이유만으로 그 파일이 고립된 것처럼 보일 수 있으므로 InstallerClean은 멈췄습니다. 아무것도 제거되지 않았습니다.`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer가 설치된 프로그램 목록의 끝을 끝내 알리지 않았습니다. InstallerClean은 {0}개 항목에서 포기했습니다(마지막 오류 코드 {1}). 끝이 없는 목록은 믿을 수 없으므로 InstallerClean은 멈췄습니다. 아무것도 제거되지 않았습니다.`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer가 한 프로그램의 패치 목록의 끝을 끝내 알리지 않았습니다. InstallerClean은 {0}개 항목에서 포기했습니다(마지막 오류 코드 {1}). 끝이 없는 목록은 믿을 수 없으므로 InstallerClean은 멈췄습니다. 아무것도 제거되지 않았습니다.`,
  'UpdateCheck.Status.UpdateAvailable': `{0} 버전을 사용할 수 있습니다.`,
  'Completion.DonateAsk': `도움이 되어 기쁩니다. 너그러운 마음이 있으시면 작은 성의도 반갑습니다.`,
  'About.Link.Guide': `안내서 및 자주 묻는 질문`,
  'About.Link.ReportProblem': `문제 신고`,
  'About.AutoUpdateCheck': `자동으로 업데이트 확인`,
  'Automation.About.Guide.HelpText': `브라우저에서 github의 readme를 엽니다.`,
  'Automation.About.ReportProblem.HelpText': `브라우저에서 github.com의 이슈 트래커를 엽니다.`,
  'Automation.AutoUpdateCheck.HelpText': `선택하면 InstallerClean이 실행할 때 github에서 새 버전이 있는지 확인합니다.`,
  'Tooltip.MoveSameDrive': `Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them.`,
  'Completion.MoveRestoreHint.Singular': `The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely).`,
  'Completion.MoveRestoreHint.Plural': `The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely).`,
  'Completion.MoveRestoreHintSameDrive.Singular': `The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely).`,
  'Completion.MoveRestoreHintSameDrive.Plural': `The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely).`,
  'Confirm.DeletePermanently.Singular': `This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead.`,
  'Confirm.DeletePermanently.Plural': `Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead.`,
  'Error.ScanCacheRootUnresolved': `InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed.`,
  'Automation.Scroll.ProductDetails': `Product details`,
  'Body.PendingReboot.Other': `Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back.`,
  'Cli.TooManyArgumentsNoPath': `Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run.`,
  'Cli.MissingFromDisk.Singular': `{0} registered file is missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, the same version, usually restores it.`,
  'Cli.MissingFromDisk.Plural': `{0} registered files are missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, the same version, usually restores them.`,
  'Cli.MoveNotEnoughSpace': `Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved.`,
  'Cli.PendingRebootBlocked.Other': `Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes.`,
};

let text = readFileSync(BASE, 'utf8');

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
const parse = (xml) => {
  const map = new Map();
  const re = /<data\s+name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)<\/value>/g;
  let m;
  while ((m = re.exec(xml)) !== null) map.set(m[1], m[2]);
  return map;
};
const neutral = parse(readFileSync(BASE, 'utf8'));
// Derived, never pinned: the machine set grows whenever the command line
// gains an event-log string, and a literal here would fail every generator
// at once while asserting nothing about what was actually stripped.
const cliMachineExpected = [...neutral.keys()].filter(isMachineCliKey).length;
const written = readFileSync(OUT, 'utf8');
const output = parse(written);
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
