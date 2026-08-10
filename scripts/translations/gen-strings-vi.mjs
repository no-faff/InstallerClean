#!/usr/bin/env node
// Vietnamese (vi) satellite generator for InstallerClean. Copied from
// gen-strings-template.mjs; only OUT and the MAP values differ. Works FROM THE
// ENGLISH SOURCE (Strings.resx): replaces each key's inner <value>, strips the
// machine-contract Cli.EventLog* keys, keeps the human Cli keys, and
// self-verifies against the neutral. Output is LF, UTF-8. See the template for
// the whole of how the body works.
//
// Vietnamese plural rule (DisplayHelpers.CategoryFor, case "vi"): PluralCategory
// .Other at every count, Vietnamese nouns carrying no number inflection. So
// there are NO .One/.Few/.Many override keys, and each Plural.* pair carries the
// same noun on both members.
//
// MAP escaping (template literals): \\ is one backslash (the paths), \n is a real
// newline (the multi-line values), {0}/{1} are .NET placeholders left verbatim,
// and &#10; is written literally where the neutral uses the XML entity.
import { readFileSync, writeFileSync } from 'node:fs';

const dir = 'src/InstallerClean.Core/Resources';
const BASE = `${dir}/Strings.resx`;            // English source (the "neutral")
const OUT = `${dir}/Strings.vi.resx`;

// Universal keeps: keys whose value is the same in every language (brand names,
// the pure-placeholder string, the size/elapsed format strings). Their still-
// English value is NOT a miss. Explicit by KEY on purpose: a future brand/format
// key then defaults to "flag until someone adds it here", never silently passes.
// Do NOT translate these values. Do NOT edit this list per language.
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

// Per-language keeps: empty for Vietnamese, which translates every translatable
// token (patch -> bản vá), so nothing beyond KEEP_ENGLISH stays English.
const ALSO_KEEP = [];

// Satellite-only plural overrides: empty. Vietnamese takes PluralCategory.Other
// at every count, so the neutral's one/other pair covers every form the UI needs.
const OVERRIDES = {};

const MAP = {
  // Window titles
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `Giới thiệu`,
  'Window.Registered.Title': `Tệp đã đăng ký, không nên xóa`,
  'Window.Orphaned.Title': `Tệp không cần thiết, có thể xóa an toàn`,

  // Section headings
  'Section.Registered.Products': `SẢN PHẨM`,
  'Section.Registered.Patches': `BẢN VÁ`,
  'Section.Registered.Details': `CHI TIẾT SẢN PHẨM`,
  'Section.Backup.Folder': `BACKUP FOLDER`,
  'Section.SayThanks': `LỜI CẢM ƠN`,

  // Field labels (used in detail panels)
  'Field.Reason': `Lý do`,
  'Field.Author': `Tác giả`,
  'Field.Application': `Ứng dụng`,
  'Field.Title': `Tiêu đề`,
  'Field.Subject': `Chủ đề`,
  'Field.Keywords': `Từ khóa`,
  'Field.SigningCertificate': `Chứng chỉ ký`,
  'Field.FileSize': `Kích thước tệp`,
  'Field.Comment': `Chú thích`,
  'Field.ProductName': `Tên sản phẩm`,
  'Field.File': `Tệp`,
  'Field.Size': `Kích thước`,
  'Field.Patches': `Bản vá`,

  // Placeholder shown for a registered package whose API ProductName is empty.
  'Field.UnknownProductName': `(không rõ)`,
  'Field.PatchesOnly': `(chỉ bản vá)`,
  'Field.Missing': `thiếu`,

  // Actions (button labels; underscore prefixes are WPF mnemonics)
  'Action.About': `_Giới thiệu`,
  'Action.Copy': `Sao chép`,
  'Action.Cut': `Cắt`,
  'Action.Paste': `Dán`,
  'Action.SelectAll': `Chọn tất cả`,
  'Action.Browse': `_Duyệt...`,
  'Action.Cancel': `_Hủy`,
  'Action.CheckForUpdates': `_Kiểm tra cập nhật`,
  'Action.Close': `Đón_g`,
  'Action.DeletePermanently': `_Xóa vĩnh viễn`,
  'Action.Done': `_Xong`,
  'Action.Details': `Chi tiết`,
  'Action.BuyMeACuppa': `_Mời tôi một ly cà phê`,
  'Action.LeaveStarOnGitHub': `Gắn _sao trên GitHub`,
  'Action.Licence': `Giấy phép Apache 2.0`,
  'Action.Move': `_Chuyển`,
  'Action.BackupFolderPlaceholder': `Path to folder if you move rather than delete.`,
  'Action.OpenReleasePage': `_Mở trang phát hành`,
  'Action.Rescan': `_Quét lại`,
  'Action.ScanAgain': `Quét _lại`,
  'Action.SendResultLog': `Gửi báo cáo`,
  'Action.SendResultLogConfirm': `_Gửi`,

  // Automation names (screen reader / accessibility)
  'Automation.BuyMeACuppa': `Ủng hộ`,
  'Automation.BuyMeACuppa.About': `Mời tôi một ly cà phê`,
  'Automation.CancelOperation': `Hủy thao tác`,
  'Automation.CancelScan': `Hủy quét`,
  'Automation.CancelStartupScan': `Hủy quét khi khởi động`,
  'Automation.Close': `Đóng`,
  'Automation.CloseWindow': `Đóng cửa sổ`,
  'Automation.CloseResult': `Đóng kết quả và quay lại cửa sổ chính`,
  'Automation.LeaveStarOnGitHub.About': `Gắn sao trên github`,
  'Automation.Minimise': `Thu nhỏ`,
  'Automation.ConfirmDelete': `Delete permanently removes the unneeded files. Cancel closes without deleting.`,
  'Automation.ConfirmMove': `Chuyển sẽ đặt các tệp không cần thiết vào thư mục đích đã chọn. Hủy sẽ để chúng nguyên chỗ cũ.`,
  'Automation.SayThanks': `Lời cảm ơn`,
  'Automation.ConfirmSendResultLog': `Gửi sẽ đăng báo cáo hiển thị tới No Faff. Hủy sẽ không gửi gì.`,
  'Automation.CheckForUpdates': `Kiểm tra cập nhật`,
  'Automation.CheckForUpdates.HelpText': `Kiểm tra trang phát hành của github xem có phiên bản mới hơn không.`,
  'Automation.UpdateAvailable.HelpText': `Mở trang phát hành để tải phiên bản mới hơn, hoặc hủy để giữ phiên bản hiện tại.`,
  'Automation.Licence.HelpText': `Mở tệp giấy phép trên github.com trong trình duyệt của bạn.`,
  'Automation.Section.BackupFolder': `Backup folder`,
  'Automation.Section.Products': `Sản phẩm`,
  'Automation.Section.Patches': `Bản vá`,
  'Automation.Section.ProductDetails': `Chi tiết sản phẩm`,
  'Automation.BackupFolder': `Backup folder`,
  'Automation.OperationProgress': `Tiến trình thao tác`,
  'Automation.RescanInstaller': `Quét lại {InstallerFolder}`,
  'Automation.ScanningProgress': `Tiến trình quét`,
  'Automation.StartupScanProgress': `Tiến trình quét khi khởi động`,
  'Automation.ViewOrphanedFiles': `Chi tiết, tệp không cần thiết`,
  'Automation.ViewOrphanedFiles.HelpText': `Có thể dọn dẹp.`,
  'Automation.ViewRegisteredFiles': `Chi tiết, tệp đã đăng ký`,
  'Automation.ViewRegisteredFiles.HelpText': `Danh sách chỉ đọc.`,
  'Automation.SortStatus.Ascending': `Đã sắp xếp theo {0}, tăng dần`,
  'Automation.SortStatus.Descending': `Đã sắp xếp theo {0}, giảm dần`,
  'Automation.Scroll.ScanResults': `Kết quả quét`,
  'Automation.Scroll.ResultDetails': `Chi tiết kết quả`,
  'Automation.Scroll.FileDetails': `Chi tiết tệp`,
  'Automation.Scroll.DialogBody': `Nội dung hộp thoại`,
  'Automation.ScanResultAnnouncement': `{0} ({1})`,
  'Automation.CompletionErrors': `Các tệp không thể xử lý`,
  'Automation.RegisteredMissingSeeAlso': `Giải thích thư mục này, và cách khôi phục một tệp, trong README`,

  // Tooltips
  'Tooltip.BuyMeACuppa.About': `Làm việc này khát nước lắm!`,
  'Tooltip.CancellingPending': `Đã yêu cầu hủy. InstallerClean đang chờ bước hiện tại tới điểm dừng. Việc này có thể mất vài giây khi I/O nặng hoặc khi đang gọi cơ sở dữ liệu MSI.`,
  'Tooltip.Close': `Đóng`,
  'Tooltip.LeaveStarOnGitHub.About': `Một ngôi sao giúp người khác tìm thấy InstallerClean.`,
  'Tooltip.Minimise': `Thu nhỏ`,
  'Tooltip.SendResultLog': `Tùy bạn, nhưng rất được trân trọng. Gửi một bản tóm tắt ẩn danh chỉ để cho tôi biết nó có hoạt động không và mọi người đang giải phóng được bao nhiêu dung lượng. Màn hình tiếp theo cho bạn xem những gì sẽ được gửi trước khi bạn xác nhận.`,
  'Tooltip.SendResultLog.NothingFound': `Tùy bạn, nhưng rất được trân trọng. Gửi một bản tóm tắt ẩn danh chỉ để cho tôi biết nó có hoạt động không. Màn hình tiếp theo cho bạn xem những gì sẽ được gửi trước khi bạn xác nhận.`,
  'Tooltip.Move': `Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them.`,
  'Tooltip.MoveNeedsDestination': `Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them.`,
  'Tooltip.Delete': `Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away.`,
  'Tooltip.SigningCertificate': `Tên chủ thể từ chứng chỉ Authenticode được nhúng. Chưa xác minh chuỗi.`,

  // Body copy
  'Body.MainExplanation.Lead': `Any unneeded files below are [safe to delete].`,
  'Body.MainExplanation.Why': `Chúng nằm trong {InstallerFolder}, còn sót lại khi một chương trình bị gỡ cài đặt ({0}), khi một bản vá mới hơn thay thế một bản ({1}) hoặc khi nhà phát hành thu hồi nó ({2}). InstallerClean chỉ liệt kê những tệp mà chính Windows báo là đã dùng xong.`,
  'Body.MainExplanation.Action': `Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored.`,
  'Body.PendingReboot.MsiExecuteMutex': `Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back.`,
  'Body.PendingReboot.InstallerInProgress': `A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}.`,
  'Body.PendingReboot.PendingRenameInCache': `Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning.`,
  'Body.NoFileSelected': `Chọn một tệp để xem chi tiết.`,
  'Body.NoProductSelected': `Chọn một sản phẩm để xem chi tiết.`,
  'Body.NoMetadata': `Không có siêu dữ liệu.`,
  'Body.RegisteredMissingFromDisk': `This installer file is missing. It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.&#10;&#10;To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it, and its own last resort is reinstalling the program.`,
  'Body.RegisteredMissingFromDisk.SeeAlso': `README [giải thích thư mục này], và cách khôi phục một tệp, bằng chính lời của Microsoft.`,
  'Body.NoPatches': `(không có)`,

  // Reasons (Reason column in the orphaned-files list)
  'Reason.Orphaned': `Bị bỏ lại`,
  'Reason.Superseded': `Bị thay thế`,
  'Reason.Obsoleted': `Đã lỗi thời`,

  // Status / progress text
  'Status.Scanning': `Đang quét...`,
  'Status.Cancelling': `Đang hủy...`,
  'Status.StartingScan': `Đang bắt đầu quét...`,
  'Status.QueryingApi': `Đang hỏi Windows về phần mềm đã cài...`,
  'Status.ScanningCache': `Đang quét thư mục bộ nhớ đệm trình cài đặt...`,
  'Status.EnumeratingProducts': `Đang liệt kê các sản phẩm đã cài...`,
  'Status.CheckingRegistry': `Đang kiểm tra sổ đăng ký để tìm các gói bổ sung...`,

  // 0 = registered package count, 1 = pluralised "package"/"packages"
  'Status.RegisteredPackagesFound': `Đã tìm thấy {0} {1} đã đăng ký.`,

  // 0 = elapsed time text (e.g. "1.2s")
  'Status.ScanComplete': `Quét xong ({0})`,
  'Status.FoundProducts': `Đang quét các gói cục bộ...`,

  // 0 = file count, 1 = pluralised noun ("file"/"files")
  'Status.FoundUnused': `Đã tìm thấy {0} {1} bạn có thể xóa an toàn.`,
  'Status.PreparingDestination': `Đang chuẩn bị thư mục đích...`,

  // 0 = file count, 1 = pluralised noun
  'Status.Moving': `Moving unneeded files...`,
  'Status.Deleting': `Deleting unneeded files...`,
  'Status.MoveCancelled.Partial': `Đã hủy chuyển. Đã xử lý {0}/{1} {2}.`,
  'Status.DeleteCancelled.Partial': `Đã hủy xóa. Đã xử lý {0}/{1} {2}.`,
  'Status.MoveFailed': `Chuyển thất bại ({0}). Chi tiết trong {1}.`,
  'Status.MoveFailed.NoLog': `Chuyển thất bại ({0}). Không thể ghi nhật ký sự cố.`,
  'Status.DeleteFailed': `Xóa thất bại ({0}). Chi tiết trong {1}.`,
  'Status.DeleteFailed.NoLog': `Xóa thất bại ({0}). Không thể ghi nhật ký sự cố.`,
  'Status.ScanAccessDenied': `Truy cập bị từ chối. Windows đã từ chối lần quét.`,
  'Status.ScanFailedDb': `Quét thất bại: không thể đọc các bản ghi Windows Installer.`,
  'Status.ScanCancelled': `Đã hủy quét.`,
  'Status.Done': `Sẵn sàng`,
  'Status.ScanFailedDetails': `Quét thất bại ({0}). Chi tiết trong {1}.`,
  'Status.ScanFailedDetails.NoLog': `Quét thất bại ({0}). Không thể ghi nhật ký sự cố.`,

  // Completion screen
  'Completion.AllClean': `Đã sạch`,
  'Completion.NothingToCleanUp': `Không còn gì để dọn trong {InstallerFolder}`,
  'Completion.NothingToCleanUpReceipt': `Đã quét {0} {1} trong {2}`,

  // 0 = size freed (e.g. "120.5 MB")
  'Completion.Freed': `Đã giải phóng {0}`,
  'Completion.Moved': `Đã chuyển {0}`,
  // Heading for the outcome where the operation acted on no file at all, then
  // the failure count line beneath it: 0 = files that failed, 1 = files tried
  // (succeeded + failed). The count selecting the form is {0}; the noun belongs
  // to {1}, so treat the two independently.
  'Completion.NothingMoved': `Không có tệp nào được chuyển`,
  'Completion.NothingDeleted': `Không có tệp nào bị xóa`,
  'Completion.FailedCount.Singular': `Không thể chuyển {0} trong số {1} tệp.`,
  'Completion.FailedCount.Plural': `Không thể chuyển {0} trong số {1} tệp.`,
  'Completion.FailedCountDelete.Singular': `Không thể xóa {0} trong số {1} tệp.`,
  'Completion.FailedCountDelete.Plural': `Không thể xóa {0} trong số {1} tệp.`,

  // 0 = moved count, 1 = pluralised noun, 2 = destination path
  'Completion.MoveSummary.Singular': `Đã chuyển {0} {1} tới: {2}`,
  'Completion.MoveSummary.Plural': `Đã chuyển {0} {1} tới: {2}`,

  // 0 = deleted count, 1 = pluralised noun

  // 0 = deleted count, 1 = pluralised noun
  'Completion.PermanentDeleteSummary.Singular': `{0} {1} permanently deleted`,
  'Completion.PermanentDeleteSummary.Plural': `{0} {1} permanently deleted`,

  // Summaries
  'Summary.RegisteredStillUsed.Singular': `{0} file left alone`,
  'Summary.RegisteredStillUsed.Plural': `{0} files left alone`,
  'Summary.OrphanedToCleanUp.Singular': `{0} tệp không cần thiết để dọn`,
  'Summary.OrphanedToCleanUp.Plural': `{0} tệp không cần thiết để dọn`,
  'Summary.MissingFromDisk.Singular': `{0} registered file is missing. No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do.`,
  'Summary.MissingFromDisk.Plural': `{0} registered files are missing. No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do.`,

  // 0 = current file count, 1 = total count, 2 = pluralised noun.
  'Summary.OperationFiles': `{0}/{1} {2}`,

  // Orphaned-window footer: unneeded files split into the three removable causes
  // (true orphans, superseded patches, obsoleted patches). 0 = orphaned count,
  // 1 = superseded count, 2 = obsoleted count, 3 = size display. No trailing
  // noun, so it agrees at any count.
  'Summary.OrphanedWindow': `{0} bị bỏ lại, {1} bị thay thế, {2} đã lỗi thời ({3})`,

  // Registered-window footer, split singular/plural so the noun and verb agree at
  // one file ("file ... is" vs "files ... are"). 0 = count, 1 = size display.
  'Summary.RegisteredWindow.Singular': `{0} registered file left alone ({1})`,
  'Summary.RegisteredWindow.Plural': `{0} registered files left alone ({1})`,

  // Confirmation dialogs

  // 0 = file count, 1 = pluralised "file"/"files", 2 = size display
  'Confirm.MoveTitle': `Chuyển {0} {1} ({2})?`,

  // 0 = destination path
  'Confirm.MoveDestination': `Move to:`,
  'Confirm.DeleteTitle': `Xóa {0} {1} ({2})?`,

  // Error messages
  'Error.AdminRequiredTitle': `Truy cập bị từ chối`,
  'Error.AdminRequiredBody': `Windows đã từ chối quyền truy cập của InstallerClean, nên nó đã dừng lại. Không có gì bị xóa.\n\nInstallerClean vốn đã chạy với quyền quản trị viên, nên khởi động lại theo cách đó cũng không giúp được gì. Windows không nói gì thêm về thứ đã từ chối quyền truy cập, nên không có gì cụ thể để thử.`,
  'Error.InstallerDbUnavailableTitle': `Không thể đọc các bản ghi Windows Installer`,
  'Error.ScanFailedTitle': `Quét thất bại`,
  'Error.InstallerDbEmpty': `Các bản ghi Windows Installer trả về hoàn toàn trống: không một chương trình đã cài hay bản cập nhật nào nhận là chủ của một tệp cài đặt trong bộ nhớ đệm. Điều đó không xảy ra trên một máy hoạt động bình thường (ngay cả một bản Windows vừa cài cũng có vài tệp như vậy), nên hoặc các bản ghi đã hỏng, hoặc không đọc được, và một lần quét tin vào câu trả lời này sẽ nhầm lẫn coi mọi tệp trong {InstallerFolder} là bị bỏ lại. Thay vào đó InstallerClean đã dừng. Không có gì bị xóa.`,
  'Error.MsiAccessDenied': `Windows Installer không cho phép InstallerClean liệt kê những gì đã được cài. InstallerClean vốn đã chạy với quyền quản trị viên, nên chạy lại với quyền quản trị viên cũng không thay đổi được gì. Không có danh sách đó thì không có cách nào an toàn để biết tệp nào trong bộ nhớ đệm vẫn còn cần, nên InstallerClean đã dừng. Không có gì bị xóa.`,
  'Error.MsiNonSuccess': `Windows Installer không thể đưa cho InstallerClean một danh sách chương trình đã cài đọc được: {0} mục liên tiếp trả về không đọc được (mã lỗi cuối {1}). Thay vì làm việc với một danh sách chỉ đọc được một phần, InstallerClean đã dừng. Không có gì bị xóa.`,
  'Error.InvalidDestinationTitle': `Đích không hợp lệ`,
  'Error.DestinationWriteFailedTitle': `Không thể ghi vào đích`,
  'Error.MoveFailedTitle': `Chuyển thất bại`,
  'Error.DeleteFailedTitle': `Xóa thất bại`,
  'Error.SettingNotSavedTitle': `Không lưu được cài đặt`,
  'Error.SettingNotSavedBody': `Không thể lưu thay đổi. Lần chạy tiếp theo, InstallerClean sẽ quay lại cài đặt trước đó.`,
  'Error.DestinationInsideInstaller': `Đích không thể nằm bên trong thư mục Windows Installer.`,

  // 0 = the destination path the user typed
  'Error.DestinationInSystemFolder': `The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%.`,
  'Error.NotEnoughSpaceTitle': `Không đủ dung lượng`,

  // 0 = destination, 1 = required size, 2 = available size
  'Error.NotEnoughSpaceBody': `Không đủ dung lượng tại {0}\n\nCần: {1}\nCòn trống: {2}`,

  // 0 = destination
  'Error.AccessDeniedDestination': `Bạn không có quyền ghi vào {0}.\nHãy thử một thư mục trong hồ sơ người dùng của bạn hoặc trên một ổ đĩa bạn sở hữu.`,
  'Error.PathTooLong': `Đường dẫn {0} quá dài đối với Windows. Hãy chọn một đường dẫn ngắn hơn.`,
  'Error.DestinationMissing': `Thư mục {0} không tồn tại và không thể tạo được. Hãy kiểm tra ký tự ổ đĩa hoặc đường dẫn mạng.`,
  'Error.IOWriteDestination': `Windows không thể ghi vào {0}.\nChi tiết trong {1}.`,
  'Error.IOWriteDestination.NoLog': `Windows không thể ghi vào {0}. Không thể ghi nhật ký sự cố.`,
  'Error.WriteDestination': `Không thể ghi vào {0}.\nChi tiết trong {1}.`,
  'Error.WriteDestination.NoLog': `Không thể ghi vào {0}. Không thể ghi nhật ký sự cố.`,
  'Error.MissingSourceFile': `Tệp không còn tồn tại.`,
  'Error.SourceIsReparsePoint': `Tệp nguồn là một symlink hoặc junction; bị từ chối vì lý do an toàn.`,
  // Singular = the sentence for ONE file, which the CLI prints after "filename: ";
  // plural = the heading the completion overlay puts over a list of filenames.
  'Error.AccessDenied.Singular': `Windows từ chối truy cập tệp này; tệp được giữ nguyên tại chỗ.`,
  'Error.AccessDenied.Plural': `Windows từ chối truy cập các tệp này; các tệp được giữ nguyên tại chỗ.`,
  'Error.FileInUse.Singular': `This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later.`,
  'Error.FileInUse.Plural': `These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later.`,
  'Error.IOFailure.Singular': `Windows báo một lỗi tệp; tệp được giữ nguyên tại chỗ.`,
  'Error.IOFailure.Plural': `Windows báo lỗi tệp; các tệp này được giữ nguyên tại chỗ.`,
  'Error.UnknownError.Singular': `Đã có trục trặc với tệp này; tệp được giữ nguyên tại chỗ.`,
  'Error.UnknownError.Plural': `Đã có trục trặc với các tệp này; các tệp được giữ nguyên tại chỗ.`,

  // 0 = shell error code

  // 0 = destination
  'Error.MoveIntoInstaller': `Từ chối chuyển tệp vào thư mục Windows Installer (đích: {0}).`,

  // 0 = the relative path the caller passed
  'Error.DestinationNotFullyQualified': `The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\\Backup, or \\\\server\\backup). InstallerClean can't use this one: {0}`,
  'BrowserLaunch.FailedTitle': `Không thể mở trình duyệt của bạn`,
  'UpdateCheck.Title': `Kiểm tra cập nhật`,
  'UpdateCheck.Status.Checking': `Đang kiểm tra...`,
  'UpdateCheck.Status.UpToDate': `Đã cập nhật.`,
  'UpdateCheck.UpdateAvailable.Title': `Có bản cập nhật`,

  // 0 = installed version, 1 = latest version on GitHub
  'UpdateCheck.UpdateAvailable.Body': `Bạn đang dùng phiên bản {0}.&#10;Phiên bản {1} đã có.`,
  'UpdateCheck.Failed.NetworkUnavailable': `Không thể kết nối tới GitHub. Hãy kiểm tra kết nối internet và thử lại.`,
  'UpdateCheck.Failed.ServerError': `GitHub trả về phản hồi lỗi. Hãy thử lại sau vài phút.`,
  'UpdateCheck.Failed.ResponseParseError': `Phản hồi của GitHub không chứa bản phát hành nào nhận ra được. Hãy thử lại sau, hoặc mở thẳng trang phát hành.`,
  'UpdateCheck.Failed.Timeout': `Quá thời gian kiểm tra. Kết nối của bạn tới GitHub có thể chậm; hãy thử lại.`,
  'UpdateCheck.Failed.Unknown': `Việc kiểm tra thất bại vì một lý do không xác định. Chi tiết nằm trong crash.log nếu bạn cần báo cáo.`,

  // 0 = the URL the user was trying to reach
  'BrowserLaunch.ClipboardOk': `InstallerClean không thể mở trình duyệt của bạn. Liên kết đã được sao chép vào bảng tạm, nên bạn có thể tự dán nó vào:&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean không thể mở trình duyệt của bạn, và cũng không thể sao chép liên kết vào bảng tạm. Liên kết là:&#10;&#10;{0}`,

  // 0 = the destination folder whose canonical path changed mid-batch
  'Error.DestinationChangedMidBatch': `InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then Re-scan and try again.`,

  // 0 = folder, 1 = inner exception message
  'Error.CannotWriteFolder': `Không thể ghi vào {0}.`,

  // 0 = file name
  'Error.NoUniqueFilename': `Không tìm được tên tệp duy nhất cho '{0}' sau 10.000 lần thử.`,

  // Result log (post-cleanup diagnostic send)
  'ResultLog.Sending': `Đang gửi...`,
  'ResultLog.Sent': `Cảm ơn! Đã gửi báo cáo.`,
  'ResultLog.Failed': `Gửi thất bại. Hãy thử lại sau.`,
  'ResultLog.NothingToSend': `Không có báo cáo để gửi.`,
  'ConfirmSendResultLog.Title': `Gửi cái này?`,
  'ConfirmSendResultLog.Reassurance': `Nó được gửi tới nofaff.netlify.app/api/result-log. Không có gì nhận dạng bạn hay máy của bạn; nó chỉ cho tôi biết InstallerClean có hoạt động không và [mọi người đang giải phóng được bao nhiêu dung lượng].`,
  'Automation.ResultLogPreview': `Xem trước báo cáo`,

  // Single instance / startup / crash
  'Startup.AlreadyRunningTitle': `InstallerClean`,
  'Startup.AlreadyRunningBody': `InstallerClean đang chạy rồi.`,
  'Startup.UnhandledTitle': `InstallerClean`,
  'Startup.UnhandledBody': `Đã xảy ra lỗi không mong muốn và InstallerClean cần đóng lại.\n\n{0}\n\nChi tiết đã được ghi vào:\n{1}`,
  'Startup.UnhandledBody.NoLog': `Đã xảy ra lỗi không mong muốn và InstallerClean cần đóng lại.\n\n{0}\n\nKhông thể ghi nhật ký sự cố.`,
  'Startup.ErrorTitle': `Lỗi khởi động`,
  'Startup.FailedToStart': `Khởi động thất bại ({0}). Chi tiết đã được ghi vào:\n{1}`,
  'Startup.FailedToStart.NoLog': `Khởi động thất bại ({0}). Không thể ghi nhật ký sự cố.`,

  // File picker
  'FilePicker.ChooseDestinationTitle': `Chọn thư mục đích cho các tệp đã chuyển`,

  // Version display

  // 0 = major.minor.patch (e.g. "1.5.4")
  'Version.Display': `Phiên bản {0}`,
  'Plural.File.Singular': `tệp`,
  'Plural.File.Plural': `tệp`,
  'Plural.Error.Singular': `lỗi`,
  'Plural.Error.Plural': `lỗi`,
  'Plural.Package.Singular': `gói`,
  'Plural.Package.Plural': `gói`,
  'Plural.Product.Singular': `sản phẩm`,
  'Plural.Product.Plural': `sản phẩm`,
  'Plural.Patch.Singular': `bản vá`,
  'Plural.Patch.Plural': `bản vá`,
  'Display.Size.GB': `{0:F2} GB`,
  'Display.Size.MB': `{0:F1} MB`,
  'Display.Size.KB': `{0:F1} KB`,
  'Display.Size.B': `{0} B`,
  'Display.Elapsed.Ms': `{0:F0}ms`,
  'Display.Elapsed.S': `{0:F1}s`,
  'Display.ElapsedLong.LessThanASecond': `chưa đến một giây`,
  'Display.ElapsedLong.Seconds': `{0:F1} giây`,
  'CrashLog.PrivacyHeader': `# crash.log captures unhandled exceptions from InstallerClean.\n# Under elevation the framework's exception messages can include\n# file paths from the running session (including other users'\n# profiles enumerated by Windows Installer queries). Network-\n# failure messages from the update check or result-log POST can\n# include the destination URL and the resolved IP / proxy address.\n# Entries about unreadable Windows Installer records can include a\n# Windows account SID (S-1-5-21-...) and the product codes of\n# installed software.\n# Redact all three classes of detail before attaching this file to\n# a public bug report.\n`,
  'Tooltip.ChangeLanguage': `Thay đổi ngôn ngữ. Chương trình sẽ khởi động lại.`,
  'Automation.ChangeLanguage': `Thay đổi ngôn ngữ`,
  'Automation.ChangeLanguage.HelpText': `Chương trình sẽ khởi động lại.`,

  // Command-line tool (installerclean-cli): the HUMAN-facing Cli.* keys.
  'Cli.UnknownArgument': `Error: unknown argument '{0}'`,
  'Cli.Cancelling': `Đang hủy...`,
  'Cli.Cancelled': `Đã hủy.`,
  'Cli.GenericError': `Error: unexpected failure ({0}). Details written to {1}.`,
  'Cli.GenericError.NoLog': `Error: unexpected failure ({0}). The crash log could not be written.`,
  'Cli.ScanningInstaller': `Đang quét {InstallerFolder}...`,
  'Cli.FoundOrphans': `Found {0} unneeded {1} to clean up ({2}).`,
  'Cli.DeletingFiles': `Deleting {0} unneeded {1}...`,
  'Cli.DeletedFiles': `Permanently deleted {0} unneeded {1}.`,
  'Cli.NoMoveDestination': `Lỗi: chưa chỉ định đích để chuyển. Dùng /m ĐƯỜNG_DẪN. (Mặc định đặt trong GUI là theo từng người dùng và không áp dụng cho các lần chạy theo lịch hoặc bằng tài khoản dịch vụ.)`,
  'Cli.MoveDestinationInsideInstaller': `Lỗi: đích không thể nằm bên trong thư mục Windows Installer.`,
  'Cli.MoveDestinationRelative': `Lỗi: đích phải là một đường dẫn đầy đủ. Nhận được: {0}`,
  'Cli.MoveDestinationInSystemFolder': `Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Error: something is using Windows Installer right now, such as a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}.`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning.`,
  'Cli.MovingFiles': `Moving {0} unneeded {1} to {2}...`,
  'Cli.MovedFiles': `Moved {0} unneeded {1}.`,
  'Cli.MutexBlocked': `Một tiến trình InstallerClean khác đang giữ khóa một-thực-thể (GUI hoặc một lần chạy CLI khác). Mã thoát 75 (tạm thời); có thể thử lại sau.`,
  'Cli.EventLogUnavailable': `Lưu ý: ghi vào Nhật ký sự kiện thất bại. Hãy kiểm tra quyền của nhật ký Ứng dụng hoặc Chính sách nhóm.`,
  'Cli.Help.Header': `InstallerClean - dọn dẹp {InstallerFolder}`,
  'Cli.Help.Usage': `Cách dùng:`,
  'Cli.Help.Help': `  installerclean-cli --help        Hiển thị trợ giúp (cũng nhận /?, -h)`,
  'Cli.Help.Version': `  installerclean-cli --version     In ra phiên bản (cũng nhận -v)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s         Scan only - list unneeded files`,
  'Cli.Help.Delete': `  installerclean-cli /d         Delete unneeded files permanently`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m         Move to the saved backup folder`,
  'Cli.Help.MovePath': `  installerclean-cli /m ĐƯỜNG_DẪN  Chuyển tới đường dẫn được chỉ định`,
  'Cli.Help.NoteLine1': `installerclean-cli blocks the prompt until it finishes, so a script or&#10;scheduled task can wait on it.`,
  'Cli.Help.ExitCodesHeader': `Mã thoát:`,
  'Cli.Help.ExitCodeOk': `  0   success: the run finished with nothing left to do`,
  'Cli.Help.ExitCodeError': `  1   failure: nothing processed (bad arguments, a bad destination, a&#10;       failed scan or every file failed)`,
  'Cli.Help.ExitCodePartial': `  2   partial: some processed, some not (a failure or a Ctrl+C part way)`,
  'Cli.Help.ExitCodeTransient': `  75  tạm thời: một điều kiện tạm thời đã chặn lần chạy (xem thông báo)`,
  'Cli.Help.ExitCodeCancelled': `  130 đã hủy (Ctrl+C)`,
  'Body.NotScanned.Lead': `Chưa quét gì cả.`,
  'Body.NotScanned.Why': `Nhấn Quét lại để tìm trong {InstallerFolder} những tệp cài đặt mà không chương trình nào còn cần.`,
  'Confirm.MoveSameDrive': `That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away.`,
  'Error.ScanCorrelationFailed': `InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed.`,
  'Error.CandidateOutsideCache': `Tệp này không nằm trực tiếp trong thư mục Windows Installer; bị từ chối vì lý do an toàn.`,
  'Completion.ReverifySkipped': `{0} {1} kept in place, because the records now claim what the scan flagged.`,
  'Completion.MoveCancelledSummary': `Đã chuyển {0}/{1} {2} trước khi bạn hủy.`,
  'Completion.PermanentDeleteCancelledSummary': `Đã xóa vĩnh viễn {0}/{1} {2} trước khi bạn hủy.`,
  'Body.PendingReboot.Lead': `Hiện chưa thể dọn những tệp này.`,
  'Cli.TooManyArguments': `Lỗi: có thêm đối số không mong đợi '{0}'. Nếu thư mục đích của bạn có dấu cách, hãy đặt cả đường dẫn trong dấu ngoặc kép: /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `That folder is saved per-user; scheduled or SYSTEM runs need /m PATH.`,
  'Completion.ReverifyIncomplete': `{0} {1} kept in place, because the Windows Installer records could not be fully read in the final check.`,
  'Error.ScanRecordsUnreadable': `InstallerClean không đọc được đủ các bản ghi Windows Installer để chắc chắn thứ gì vẫn còn cần: danh sách chương trình đã cài trả về thiếu, và việc đọc chính các bản ghi đó trực tiếp từ sổ đăng ký cũng gặp lỗi. Một tệp có thể trông như bị bỏ lại chỉ vì bản ghi nêu tên nó nằm trong số những bản ghi không đọc được, nên InstallerClean đã dừng. Không có gì bị xóa.`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer chưa bao giờ báo hiệu kết thúc danh sách chương trình đã cài: InstallerClean đã bỏ cuộc sau {0} mục (mã lỗi cuối {1}). Không thể tin một danh sách không có điểm dừng, nên InstallerClean đã dừng. Không có gì bị xóa.`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer chưa bao giờ báo hiệu kết thúc danh sách bản vá của một chương trình: InstallerClean đã bỏ cuộc sau {0} mục (mã lỗi cuối {1}). Không thể tin một danh sách không có điểm dừng, nên InstallerClean đã dừng. Không có gì bị xóa.`,
  'UpdateCheck.Status.UpdateAvailable': `Phiên bản {0} đã có.`,
  'Completion.DonateAsk': `Rất vui vì đã giúp được. Nếu bạn có lòng, một ly cà phê cũng quý.`,
  'About.Link.Guide': `Hướng dẫn và câu hỏi thường gặp`,
  'About.Link.ReportProblem': `Báo cáo vấn đề`,
  'About.AutoUpdateCheck': `Tự động kiểm tra cập nhật`,
  'Automation.About.Guide.HelpText': `Mở readme trên github trong trình duyệt của bạn.`,
  'Automation.About.ReportProblem.HelpText': `Mở trình theo dõi vấn đề (Issues) trên github.com trong trình duyệt của bạn.`,
  'Automation.AutoUpdateCheck.HelpText': `Nếu được đánh dấu, InstallerClean sẽ kiểm tra github xem có phiên bản mới hơn không khi bạn chạy nó.`,
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
  'Cli.MissingFromDisk.Singular': `{0} registered file is missing from {InstallerFolder}. No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, preferably the same version, should restore it.`,
  'Cli.MissingFromDisk.Plural': `{0} registered files are missing from {InstallerFolder}. No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, preferably the same version, should restore them.`,
  'Cli.MoveNotEnoughSpace': `Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved.`,
  'Cli.PendingRebootBlocked.Other': `Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes.`,
  'Cli.FoundNoOrphans': `Found no unneeded files.`,
  'Cli.DestinationChangedMidBatch': `InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then run the command again.`,
  'Cli.Help.Summary': `Removes cached .msi and .msp files that no installed program still needs.`,
  'Cli.Help.Elevation': `Needs an elevated (administrator) prompt; Windows will not start it.`,
  'Error.InstallerLockUnavailableTitle': `Không có tệp nào bị xóa`,
  'Error.MoveInstallerLockUnavailableTitle': `Nothing was moved`,
  'Error.InstallerLockUnavailable': `InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening.`,
  'Error.MoveInstallerLockUnavailable': `InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. Try again, and restart Windows if it keeps happening.`,
  'Cli.InstallerLockUnavailable': `Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening.`,
  'Cli.MoveInstallerLockUnavailable': `Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. Try again, and restart Windows if it keeps happening.`,
  'Completion.ReverifyRecordsChanged': `{0} {1} kept in place, because the Windows Installer records had changed by the final check.`,
  'Summary.RecordsNotMatched': `InstallerClean couldn't match up everything in the Windows records, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again.`,
  'Cli.RecordsNotMatched': `InstallerClean couldn't match up everything in the Windows records, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up.`,
  'Completion.ReverifyIdentityClaimed': `{0} {1} kept in place, because Windows has a record of the program named inside.`,
  'Completion.ReverifyIdentityUnreadable': `{0} {1} kept in place, because InstallerClean couldn't find a program named inside.`,
  'Completion.NothingRemoved': `Nothing removed`,
  'Error.ScanNoRegisteredFileInFolder': `InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed.`,
};

let text = readFileSync(BASE, 'utf8');

// Remove ONLY the machine-contract Cli.* <data> elements BY NAME (the
// Cli.EventLog* set bar Cli.EventLogUnavailable): each is matched non-greedy to
// its own </data>. The human-facing Cli keys are KEPT, and their value is
// replaced from the MAP like any other key. Same predicate as
// scripts/check-resx-parity.mjs. The section comments left orphaned by a removed
// machine key (<!-- CLI output -->, the per-machine-key placeholder notes) are
// left in place deliberately: removing them needs fragile anchors that name
// specific keys, the exact step that broke before. They are harmless XML
// comments. Do NOT reintroduce comment surgery to "tidy" them.
const isMachineCliKey = (k) =>
  k.startsWith('Cli.') && k.includes('EventLog') && k !== 'Cli.EventLogUnavailable';
let cliMachineRemoved = 0;
text = text.replace(/[^\S\n]*<data name="(Cli\.[^"]*)"[\s\S]*?<\/data>\n?/g,
  (m, name) => { if (isMachineCliKey(name)) { cliMachineRemoved++; return ''; } return m; });

// Replace each key's inner <value> from MAP. The capture keeps the <data> tag,
// its attributes and the whitespace before <value>; any <comment> child and the
// </data> close sit outside the match. The closing quote anchors the name, so
// Status.MoveFailed never matches Status.MoveFailed.NoLog. A function replacement
// keeps $-sequences in a value from being read as backreferences.
const escRe = (s) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
const notApplied = [];
for (const [key, val] of Object.entries(MAP)) {
  const re = new RegExp('(<data name="' + escRe(key) + '"[^>]*>\\s*<value>)([\\s\\S]*?)(</value>)');
  let applied = false;
  text = text.replace(re, (m, p1, p2, p3) => { applied = true; return p1 + val + p3; });
  if (!applied) notApplied.push(key);
}

// Append the satellite-only override <data> elements before </root>. Values carry
// no XML-special characters (same as the MAP). Empty OVERRIDES means no block, so
// the output is byte-identical to a no-override language (e.g. Korean).
const overrideBlock = Object.entries(OVERRIDES)
  .map(([k, v]) => `  <data name="${k}" xml:space="preserve"><value>${v}</value></data>`)
  .join('\n');
if (overrideBlock) text = text.replace('</root>', overrideBlock + '\n</root>');

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
// Required = everything a satellite must carry: the non-Cli keys plus the
// human-facing Cli keys. The machine Cli keys are the complement; they must be
// absent from the output (isMachineCliKey is defined up in the strip section).
const neutralRequired = [...neutral.keys()].filter((k) => !isMachineCliKey(k));

// Satellite-only overrides: present, and each sharing its base key's {N} set
// (base = the <Prefix>.Plural sibling if the neutral has one, else the flat key).
const overrideKeys = Object.keys(OVERRIDES);
const overrideMissing = overrideKeys.filter((k) => !output.has(k));
const overrideArityMismatch = overrideKeys.filter((k) => {
  if (!output.has(k)) return true;
  const base = k.replace(/\.(?:One|Few|Many)$/, '');
  const ref = neutral.has(`${base}.Plural`) ? `${base}.Plural` : base;
  if (!neutral.has(ref)) return true; // base must exist in the neutral
  const a = placeholders(neutral.get(ref)), b = placeholders(output.get(k));
  return a.size !== b.size || [...a].some((i) => !b.has(i));
});

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
  if (!output.has(k)) return false; // already counted by missingFromOutput
  const a = placeholders(neutral.get(k)), b = placeholders(output.get(k));
  return a.size !== b.size || [...a].some((i) => !b.has(i));
});
const crlf = (written.match(/\r/g) || []).length;

// Untranslated-phrase gate (KEY-based, HARD): a value still byte-identical to the
// English neutral is a miss, UNLESS its key is a universal keep or in ALSO_KEEP.
const alsoKeep = new Set(ALSO_KEEP);
const untranslated = neutralRequired.filter((k) =>
  output.has(k) && output.get(k) === neutral.get(k) && !KEEP_ENGLISH.has(k) && !alsoKeep.has(k));

// Breakdown computed, never pinned: the non-Cli and human-Cli totals both grow with
// every string the app gains, and a hardcoded pair goes stale silently while the
// checked figure beside it stays right.
const nonCliRequired = neutralRequired.filter((k) => !k.startsWith('Cli.')).length;
console.log('translatable <data> in output:', output.size,
  '(expect', neutralRequired.length + overrideKeys.length,
  '=', nonCliRequired, 'non-Cli +', neutralRequired.length - nonCliRequired, 'Cli +',
  overrideKeys.length, 'override)');
console.log('machine Cli <data> removed:', cliMachineRemoved, `(expect ${cliMachineExpected})`);
console.log('MAP entries:', Object.keys(MAP).length, '| override keys:', overrideKeys.length, '| CRLF:', crlf, '(expect 0)');

// ALSO_KEEP audit roster, so a lazy "force it green" dump is visible at a glance.
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
if (overrideMissing.length) console.log('!! override key missing from output:', overrideMissing);
if (overrideArityMismatch.length) console.log('!! override arity differs from its base key:', overrideArityMismatch);
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
  !overrideMissing.length && !overrideArityMismatch.length &&
  output.size === neutralRequired.length + overrideKeys.length && cliMachineRemoved === cliMachineExpected && crlf === 0;
const ok = structuralOk && !untranslated.length;
console.log(ok ? '\nGENERATION OK' : '\nGENERATION HAS ISSUES (see above)');
