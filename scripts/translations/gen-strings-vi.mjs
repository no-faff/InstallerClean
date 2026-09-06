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

// Per-language keeps: empty for Vietnamese, which translates every translatable
// token (patch -> bản vá), so nothing beyond KEEP_ENGLISH stays English.
const ALSO_KEEP = [
  // The list separator Vietnamese uses is the one English uses. A punctuation
  // mark rather than a word, so there is nothing to translate and nothing to
  // get wrong; only ja and zh-Hans differ, taking the ideographic comma.
  'Display.ListSeparator',       // ", "
  // The size and elapsed unit suffixes. Vietnamese abbreviates them exactly as
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

// Satellite-only plural overrides: empty. Vietnamese takes PluralCategory.Other
// at every count, so the neutral's one/other pair covers every form the UI needs.
const OVERRIDES = {};

const MAP = {
  // Window titles
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `Giới thiệu`,
  'Window.Registered.Title': `Tệp được để nguyên`,
  'Window.Orphaned.Title': `Tệp không cần thiết, có thể xóa an toàn`,

  // Section headings
  // Section.Registered.Products and Automation.Section.Products are deliberately
  // not here. They left the neutral resx when the registered-files window stopped
  // having a products group of its own, so a value for either would be
  // regenerated into a key the app cannot use.
  'Section.Registered.Patches': `BẢN VÁ`,
  'Section.Registered.Details': `CHI TIẾT SẢN PHẨM`,
  'Section.Backup.Folder': `THƯ MỤC SAO LƯU`,
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
  'Action.BackupFolderPlaceholder': `Đường dẫn thư mục nếu bạn chuyển thay vì xóa.`,
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
  'Automation.ConfirmDelete': `Xóa vĩnh viễn sẽ bỏ đi các tệp không cần thiết. Hủy sẽ đóng lại mà không xóa gì.`,
  'Automation.ConfirmMove': `Chuyển sẽ đặt các tệp không cần thiết vào thư mục đích đã chọn. Hủy sẽ để chúng nguyên chỗ cũ.`,
  'Automation.SayThanks': `Lời cảm ơn`,
  'Automation.ConfirmSendResultLog': `Gửi sẽ đăng báo cáo hiển thị tới No Faff. Hủy sẽ không gửi gì.`,
  'Automation.CheckForUpdates': `Kiểm tra cập nhật`,
  'Automation.CheckForUpdates.HelpText': `Kiểm tra trang phát hành của github xem có phiên bản mới hơn không.`,
  'Automation.UpdateAvailable.HelpText': `Mở trang phát hành để tải phiên bản mới hơn, hoặc hủy để giữ phiên bản hiện tại.`,
  'Automation.Licence.HelpText': `Mở tệp giấy phép trên github.com trong trình duyệt của bạn.`,
  'Automation.Section.BackupFolder': `Thư mục sao lưu`,
  'Automation.Section.Patches': `Bản vá`,
  'Automation.Section.ProductDetails': `Chi tiết sản phẩm`,
  'Automation.BackupFolder': `Thư mục sao lưu`,
  'Automation.OperationProgress': `Tiến trình thao tác`,
  'Automation.RescanInstaller': `Quét lại {InstallerFolder}`,
  'Automation.ScanningProgress': `Tiến trình quét`,
  'Automation.StartupScanProgress': `Tiến trình quét khi khởi động`,
  'Automation.ViewOrphanedFiles': `Chi tiết, tệp không cần thiết`,
  'Automation.ViewOrphanedFiles.HelpText': `Có thể dọn dẹp.`,
  'Automation.ViewRegisteredFiles': `Chi tiết, tệp được để nguyên`,
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
  'Tooltip.Move': `Chuyển các tệp không cần thiết vào thư mục sao lưu.`,
  'Tooltip.MoveNeedsDestination': `Chuyển các tệp không cần thiết vào một thư mục sao lưu. Bạn sẽ chọn thư mục ngay sau đây.`,
  'Tooltip.Delete': `Xóa vĩnh viễn các tệp không cần thiết. Hãy dùng Chuyển nếu bạn muốn có cơ hội tự mình yên tâm rằng mọi thứ đều ổn.`,
  'Tooltip.SigningCertificate': `Tên chủ thể từ chứng chỉ Authenticode được nhúng. Chưa xác minh chuỗi.`,

  // Body copy
  'Body.MainExplanation.Lead': `Mọi tệp không cần thiết bên dưới đều [có thể xóa an toàn].`,
  'Body.MainExplanation.Why': `Chúng nằm trong {InstallerFolder}. InstallerClean hỏi Windows về từng chương trình đã cài: một tệp được liệt kê khi không chương trình nào nhận nó ({0}), hoặc khi một bản vá mới hơn đã thay thế nó và không chương trình nào có thể quay lại dùng nó ({1}).`,
  'Body.MainExplanation.Action': `Hãy chuyển chúng vào một thư mục sao lưu do bạn chọn, rồi xóa thư mục đó khi bạn đã yên tâm rằng các chương trình của mình vẫn cập nhật và gỡ cài đặt bình thường. Đưa chúng trở lại {InstallerFolder} sẽ khôi phục mọi thứ. Hoặc xóa vĩnh viễn ngay bây giờ.`,
  'Body.PendingReboot.MsiExecuteMutex': `Có thứ gì đó đang dùng Windows Installer ngay lúc này, chẳng hạn một bản cập nhật Windows hoặc một chương trình đang cài trong nền. Chuyển và Xóa tạm dừng trong lúc đó, để InstallerClean không đụng vào {InstallerFolder} khi thư mục đang thay đổi. Xong rồi thì quét lại, hai nút sẽ trở lại.`,
  'Body.PendingReboot.InstallerInProgress': `Máy này có một giao dịch Windows Installer trước đó đang bị treo. Hãy tiếp tục hoặc hoàn tác lần cài đặt ấy (hoặc khởi động lại Windows) trước khi dọn {InstallerFolder}.`,
  'Body.PendingReboot.PendingRenameInCache': `Windows đã xếp hàng một thao tác đổi tên tệp cho lần khởi động tới, có ảnh hưởng tới {InstallerFolder}. Hãy khởi động lại Windows trước khi dọn.`,
  'Body.NoFileSelected': `Chọn một tệp để xem chi tiết.`,
  'Body.NoProductSelected': `Chọn một sản phẩm để xem chi tiết.`,
  'Body.NoMetadata': `Không có siêu dữ liệu.`,
  'Body.RegisteredMissingFromDisk': `Thiếu tệp cài đặt này. Hiện tại nó không gây rắc rối gì, và sẽ không gây rắc rối cho tới ngày bạn thử cập nhật hoặc gỡ cài đặt chương trình mà nó thuộc về. Khi đó bước này có thể thất bại, vì Windows tìm tệp này mà không thấy.\n\nĐể đưa nó trở lại, bạn cần bộ cài của đúng phiên bản bạn đang có. Hãy lấy từ nhà sản xuất chương trình và chạy đè lên bản đang cài. Phiên bản mới hơn không dùng được: nó phải gỡ bản bạn đang có trước, và chính bước đó mới cần tệp này. Gỡ cài đặt trước cũng không được, vì cùng lý do. Việc này sẽ khôi phục tệp và giữ nguyên các thiết lập của bạn, nhưng Microsoft không bảo đảm điều đó.`,
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
  'Status.Moving': `Đang chuyển các tệp không cần thiết...`,
  'Status.Deleting': `Đang xóa các tệp không cần thiết...`,
  'Status.MoveCancelled.Partial': `Đã hủy chuyển. Đã xử lý {0}/{1} {2}.`,
  'Status.DeleteCancelled.Partial': `Đã hủy xóa. Đã xử lý {0}/{1} {2}.`,
  'Status.MoveFailed': `{0}. Chi tiết trong {1}.`,
  'Status.MoveFailed.NoLog': `{0}. Không thể ghi nhật ký sự cố.`,
  'Status.DeleteFailed': `{0}. Chi tiết trong {1}.`,
  'Status.DeleteFailed.NoLog': `{0}. Không thể ghi nhật ký sự cố.`,
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
  'Completion.PermanentDeleteSummary.Singular': `Đã xóa vĩnh viễn {0} {1}`,
  'Completion.PermanentDeleteSummary.Plural': `Đã xóa vĩnh viễn {0} {1}`,

  // Summaries
  'Summary.RegisteredStillUsed.Singular': `{0} tệp được để nguyên`,
  'Summary.RegisteredStillUsed.Plural': `{0} tệp được để nguyên`,
  'Summary.OrphanedToCleanUp.Singular': `{0} tệp không cần thiết để dọn`,
  'Summary.OrphanedToCleanUp.Plural': `{0} tệp không cần thiết để dọn`,
  'Summary.NothingListed.Singular': `InstallerClean không thể chắc chắn những tệp nào trong bộ nhớ đệm thuộc về các chương trình đã cài ở đây, nên đã giữ lại tệp duy nhất đó thay vì đề xuất nó.`,
  'Summary.NothingListed.Plural': `InstallerClean không thể chắc chắn những tệp nào trong bộ nhớ đệm thuộc về các chương trình đã cài ở đây, nên đã giữ lại {0} {1} thay vì đề xuất chúng.`,
  'Summary.MissingFromDisk.Singular': `Windows có hồ sơ về {0} tệp không nằm trong {InstallerFolder}: {1}. Trong sử dụng hằng ngày điều này không gây rắc rối, nhưng việc cập nhật hoặc gỡ cài đặt chương trình đó có thể thất bại. Hãy mở Chi tiết để biết cần làm gì.`,
  'Summary.MissingFromDisk.Plural': `Windows có hồ sơ về {0} tệp không nằm trong {InstallerFolder}: {1}. Trong sử dụng hằng ngày điều này không gây rắc rối, nhưng việc cập nhật hoặc gỡ cài đặt các chương trình đó có thể thất bại. Hãy mở Chi tiết để biết cần làm gì.`,
  'Summary.MissingFromDisk.OtherPrograms.Singular': `{0} chương trình khác`,
  'Summary.MissingFromDisk.OtherPrograms.Plural': `{0} chương trình khác`,
  'Summary.MissingFromDisk.Unnamed.Singular': `{0} tệp không có chương trình nào được nêu tên trong bản ghi`,
  'Summary.MissingFromDisk.Unnamed.Plural': `{0} tệp không có chương trình nào được nêu tên trong bản ghi`,

  // 0 = current file count, 1 = total count, 2 = pluralised noun.
  'Summary.OperationFiles': `{0}/{1} {2}`,

  // Orphaned-window footer: unneeded files split into the three removable causes
  // (true orphans, superseded patches, obsoleted patches). 0 = orphaned count,
  // 1 = superseded count, 2 = obsoleted count, 3 = size display. No trailing
  // noun, so it agrees at any count.
  'Summary.OrphanedWindow': `{0} {1} không cần thiết ({2})`,

  // Registered-window footer, split singular/plural so the noun and verb agree at
  // one file ("file ... is" vs "files ... are"). 0 = count, 1 = size display.
  'Summary.RegisteredWindow.Singular': `{0} tệp được để nguyên ({1})`,
  'Summary.RegisteredWindow.Plural': `{0} tệp được để nguyên ({1})`,

  // Confirmation dialogs

  // 0 = file count, 1 = pluralised "file"/"files", 2 = size display
  'Confirm.MoveTitle': `Chuyển {0} {1} ({2})?`,

  'Confirm.DeleteTitle': `Xóa {0} {1} ({2})?`,

  // Error messages
  'Error.AdminRequiredTitle': `Truy cập bị từ chối`,
  'Error.AdminRequiredBody': `Windows đã từ chối quyền truy cập của InstallerClean, nên nó đã dừng lại. Không có gì bị xóa.\n\nInstallerClean vốn đã chạy với quyền quản trị viên, nên khởi động lại theo cách đó cũng không giúp được gì. Windows không nói gì thêm về thứ đã từ chối quyền truy cập, nên không có gì cụ thể để thử.`,
  'Error.InstallerDbUnavailableTitle': `Không thể đọc các bản ghi Windows Installer`,
  'Error.ScanFailedTitle': `Quét thất bại`,
  'Error.InstallerDbEmpty': `Các bản ghi Windows Installer trả về hoàn toàn trống: không một chương trình đã cài hay bản cập nhật nào nhận là chủ của một tệp cài đặt trong bộ nhớ đệm. Điều đó không xảy ra trên một máy hoạt động bình thường (ngay cả một bản Windows vừa cài cũng có vài tệp như vậy), nên hoặc các bản ghi đã hỏng, hoặc không đọc được, và một lần quét tin vào câu trả lời này sẽ nhầm lẫn coi mọi tệp trong {InstallerFolder} là bị bỏ lại. Thay vào đó InstallerClean đã dừng. Không có gì bị xóa.`,
  'Error.MsiAccessDenied': `Windows Installer không cho phép InstallerClean liệt kê những gì đã được cài. InstallerClean vốn đã chạy với quyền quản trị viên, nên chạy lại với quyền quản trị viên cũng không thay đổi được gì. Không có danh sách đó thì không có cách nào an toàn để biết tệp nào trong bộ nhớ đệm vẫn còn cần, nên InstallerClean đã dừng. Không có gì bị xóa.`,
  'Error.MsiNonSuccess': `Windows Installer không thể đưa cho InstallerClean một danh sách chương trình đã cài đọc được: nó đã đọc {2} {3}, rồi {0} mục liên tiếp trả về không đọc được (mã lỗi cuối {1}). Thay vì làm việc với một danh sách chỉ đọc được một phần, InstallerClean đã dừng. Không có gì bị xóa.`,
  'Error.InvalidDestinationTitle': `Đích không hợp lệ`,
  'Error.DestinationWriteFailedTitle': `Không thể ghi vào đích`,
  'Error.MoveFailedTitle': `Chuyển thất bại`,
  'Error.DeleteFailedTitle': `Xóa thất bại`,
  'Error.SettingNotSavedTitle': `Không lưu được cài đặt`,
  'Error.SettingNotSavedBody': `Không thể lưu thay đổi. Lần chạy tiếp theo, InstallerClean sẽ quay lại cài đặt trước đó.`,
  'Error.DestinationInsideInstaller': `Đích không thể nằm bên trong thư mục Windows Installer.`,

  // 0 = the destination path the user typed
  'Error.DestinationInSystemFolder': `Đích {0} phân giải vào bên trong một thư mục hệ thống của Windows. Hãy chọn một đường dẫn ngoài %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% và %ProgramData%.`,
  'Error.NotEnoughSpaceTitle': `Không đủ dung lượng`,

  // 0 = destination, 1 = required size, 2 = available size
  'Error.NotEnoughSpaceBody': `Không còn đủ chỗ tại {0}\n\nCần: {1}\nCòn trống: {2}`,

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
  'Error.FileInUse.Singular': `Tệp này đang được một chương trình khác mở hoặc khóa, nên lúc này không gì bỏ đi được. Nó đã được để nguyên; hãy thử lại sau.`,
  'Error.FileInUse.Plural': `Các tệp này đang được một chương trình khác mở hoặc khóa, nên lúc này không gì bỏ đi được. Chúng đã được để nguyên; hãy thử lại sau.`,
  'Error.IOFailure.Singular': `Windows báo một lỗi tệp; tệp được giữ nguyên tại chỗ.`,
  'Error.IOFailure.Plural': `Windows báo lỗi tệp; các tệp này được giữ nguyên tại chỗ.`,
  'Error.UnknownError.Singular': `Đã có trục trặc với tệp này; tệp được giữ nguyên tại chỗ.`,
  'Error.UnknownError.Plural': `Đã có trục trặc với các tệp này; các tệp được giữ nguyên tại chỗ.`,

  // 0 = shell error code

  // 0 = destination
  'Error.MoveIntoInstaller': `Từ chối chuyển tệp vào thư mục Windows Installer (đích: {0}).`,

  // 0 = the relative path the caller passed
  'Error.DestinationNotFullyQualified': `Thư mục sao lưu phải là một đường dẫn đầy đủ tới một thư mục, bắt đầu bằng ký tự ổ đĩa hoặc một chia sẻ mạng (ví dụ D:\\Backup, hoặc \\\\server\\backup). InstallerClean không dùng được cái này: {0}`,
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
  'UpdateCheck.Failed.Unknown': `Việc kiểm tra thất bại vì một lý do không xác định. Chi tiết nằm trong {0} nếu bạn cần báo cáo.`,

  // 0 = the URL the user was trying to reach
  'BrowserLaunch.ClipboardOk': `The link is on your clipboard, so you can paste it in yourself:&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean couldn't copy the link to your clipboard either, so here it is:&#10;&#10;{0}`,

  // 0 = the destination folder whose canonical path changed mid-batch
  'Error.DestinationChangedMidBatch': `InstallerClean không còn xác nhận được thư mục sao lưu, nên đã dừng lại. Hãy kiểm tra {0}, rồi Quét lại và thử lần nữa.`,
  // 0 = folder, 1 = inner exception message
  'Error.CannotWriteFolder': `Không thể ghi vào {0}.`,

  // 0 = file name
  'Error.DestinationCollision': `Đã có một tệp tên '{0}' trong thư mục sao lưu.`,

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
  'Startup.AlreadyRunningBody': `Đang chạy rồi.`,
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
  'Display.ListSeparator': `, `,
  'Display.ElapsedLong.LessThanASecond': `chưa đến một giây`,
  'Display.ElapsedLong.Seconds': `{0:F1} giây`,
  'CrashLog.PrivacyHeader': `# crash.log ghi lại các ngoại lệ chưa xử lý của InstallerClean.\n# Khi chạy với quyền nâng cao, thông báo ngoại lệ của framework có thể\n# chứa đường dẫn tệp trong phiên đang chạy (kể cả hồ sơ của người dùng\n# khác do các truy vấn Windows Installer liệt kê). Thông báo lỗi mạng\n# từ việc kiểm tra cập nhật hoặc gửi nhật ký kết quả có thể chứa URL\n# đích và địa chỉ IP hoặc proxy đã phân giải. Các mục về bản ghi\n# Windows Installer không đọc được có thể chứa SID tài khoản Windows\n# (S-1-5-21-...) và mã sản phẩm của phần mềm đã cài.\n# Hãy xóa cả ba loại thông tin này trước khi đính kèm tệp này vào một\n# báo cáo lỗi công khai.\n`,
  'Tooltip.ChangeLanguage': `Thay đổi ngôn ngữ. Chương trình sẽ khởi động lại.`,
  'Automation.ChangeLanguage': `Thay đổi ngôn ngữ`,
  'Automation.ChangeLanguage.HelpText': `Chương trình sẽ khởi động lại.`,

  // Command-line tool (installerclean-cli): the HUMAN-facing Cli.* keys.
  'Cli.UnknownArgument': `Lỗi: đối số không rõ '{0}'`,
  'Cli.Cancelling': `Đang hủy...`,
  'Cli.Cancelled': `Đã hủy.`,
  'Cli.GenericError': `Lỗi: sự cố không mong đợi ({0}). Chi tiết đã ghi vào {1}.`,
  'Cli.GenericError.NoLog': `Lỗi: sự cố không mong đợi ({0}). Không ghi được nhật ký sự cố.`,
  'Cli.ScanningInstaller': `Đang quét {InstallerFolder}...`,
  'Cli.FoundOrphans': `Đã tìm thấy {0} {1} không cần thiết để dọn ({2}).`,
  'Cli.DeletingFiles': `Đang xóa {0} {1} không cần thiết...`,
  'Cli.DeletedFiles': `Đã xóa vĩnh viễn {0} {1} không cần thiết.`,
  'Cli.NoMoveDestination': `Lỗi: chưa chỉ định đích để chuyển. Dùng /m ĐƯỜNG_DẪN. (Mặc định đặt trong GUI là theo từng người dùng và không áp dụng cho các lần chạy theo lịch hoặc bằng tài khoản dịch vụ.)`,
  'Cli.MoveDestinationInsideInstaller': `Lỗi: đích không thể nằm bên trong thư mục Windows Installer.`,
  'Cli.MoveDestinationRelative': `Lỗi: đích phải là một đường dẫn đầy đủ. Nhận được: {0}`,
  'Cli.MoveDestinationInSystemFolder': `Lỗi: đích {0} phân giải vào bên trong một thư mục hệ thống của Windows. Hãy chọn một đường dẫn ngoài %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% và %ProgramData%.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Lỗi: có thứ gì đó đang dùng Windows Installer ngay lúc này, chẳng hạn một bản cập nhật Windows hoặc một chương trình đang cài trong nền. /m và /d bị chặn trong lúc đó. Hãy thử lại khi xong.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Lỗi: máy này có một giao dịch Windows Installer trước đó đang bị treo. Hãy tiếp tục hoặc hoàn tác lần cài đặt ấy (hoặc khởi động lại Windows) trước khi dọn {InstallerFolder}.`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Lỗi: một thao tác tệp đã xếp hàng sau khi khởi động lại có nhắm vào {InstallerFolder} ({0}). Hãy khởi động lại Windows để hoàn tất thao tác đó trước khi dọn.`,
  'Cli.MovingFiles': `Đang chuyển {0} {1} không cần thiết tới {2}...`,
  'Cli.MovedFiles': `Đã chuyển {0} {1} không cần thiết.`,
  'Cli.MutexBlocked': `Một tiến trình InstallerClean khác đang giữ khóa một-thực-thể (GUI hoặc một lần chạy CLI khác). Mã thoát 75 (tạm thời); có thể thử lại sau.`,
  'Cli.EventLogUnavailable': `Lưu ý: ghi vào Nhật ký sự kiện thất bại. Hãy kiểm tra quyền của nhật ký Ứng dụng hoặc Chính sách nhóm.`,
  'Cli.Help.Header': `InstallerClean - dọn dẹp {InstallerFolder}`,
  'Cli.Help.Usage': `Cách dùng:`,
  'Cli.Help.Help': `  installerclean-cli --help        Hiển thị trợ giúp (cũng nhận /?, -h)`,
  'Cli.Help.Version': `  installerclean-cli --version     In ra phiên bản (cũng nhận -v)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s            Chỉ quét - liệt kê tệp không cần`,
  'Cli.Help.Delete': `  installerclean-cli /d            Xóa vĩnh viễn tệp không cần thiết`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m            Chuyển tới thư mục sao lưu`,
  'Cli.Help.MovePath': `  installerclean-cli /m ĐƯỜNG_DẪN  Chuyển tới đường dẫn được chỉ định`,
  'Cli.Help.NoteLine1': `installerclean-cli giữ dấu nhắc cho tới khi xong, để một tập lệnh hoặc&#10;một tác vụ theo lịch có thể chờ nó.`,
  'Cli.Help.ExitCodesHeader': `Mã thoát:`,
  'Cli.Help.ExitCodeOk': `  0   thành công: đã làm đúng việc được yêu cầu, không có gì hỏng`,
  'Cli.Help.ExitCodeError': `  1   thất bại: không xử lý gì (đối số hoặc đích sai, quét thất bại&#10;       hoặc mọi tệp đều lỗi)`,
  'Cli.Help.ExitCodePartial': `  2   một phần: xử lý được một phần (một lỗi hoặc một Ctrl+C)`,
  'Cli.Help.ExitCodeTransient': `  75  tạm thời: một điều kiện tạm thời đã chặn lần chạy (xem thông báo)`,
  'Cli.Help.ExitCodeCancelled': `  130 đã hủy (Ctrl+C)`,
  'Body.NotScanned.Lead': `Chưa quét gì cả.`,
  'Body.NotScanned.Why': `Nhấn Quét lại để tìm trong {InstallerFolder} những tệp cài đặt mà không chương trình nào còn cần.`,
  'Confirm.MoveSameDrive': `Thư mục đó nằm trên cùng một ổ đĩa, nên dung lượng chưa trở lại cho tới khi bạn xóa nó. Hãy chọn một thư mục trên ổ đĩa khác nếu bạn muốn có dung lượng ngay.`,
  'Error.ScanCorrelationFailed': `InstallerClean không khớp được các bản ghi Windows Installer với nội dung trong {InstallerFolder}. Gần như không có thứ gì các bản ghi trỏ tới thực sự nằm ở đó, và gần như không có thứ gì ở đó được bản ghi nào nêu tên, nên không tệp nào có thể được chứng tỏ là không cần thiết. Không có gì được đề xuất và không có gì bị bỏ đi.`,
  'Error.CandidateOutsideCache': `Tệp này không nằm trực tiếp trong thư mục Windows Installer; bị từ chối vì lý do an toàn.`,
  'Completion.MoveCancelledSummary': `Đã chuyển {0}/{1} {2} trước khi bạn hủy.`,
  'Completion.PermanentDeleteCancelledSummary': `Đã xóa vĩnh viễn {0}/{1} {2} trước khi bạn hủy.`,
  'Body.PendingReboot.Lead': `Hiện chưa thể dọn những tệp này.`,
  'Cli.TooManyArguments': `Lỗi: có thêm đối số không mong đợi '{0}'. Nếu thư mục đích của bạn có dấu cách, hãy đặt cả đường dẫn trong dấu ngoặc kép: /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `Thư mục theo người dùng; tác vụ theo lịch hoặc SYSTEM: /m ĐƯỜNG_DẪN.`,
  'Error.ScanRecordsUnreadable': `InstallerClean không đọc được đủ các bản ghi Windows Installer để chắc chắn thứ gì vẫn còn cần: danh sách chương trình đã cài trả về thiếu, và việc đọc chính các bản ghi đó trực tiếp từ sổ đăng ký cũng gặp lỗi. Một tệp có thể trông như bị bỏ lại chỉ vì bản ghi nêu tên nó nằm trong số những bản ghi không đọc được, nên InstallerClean đã dừng. Không có gì bị xóa.`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer chưa bao giờ báo hiệu kết thúc danh sách chương trình đã cài: InstallerClean đã đọc {2} {3}, rồi bỏ cuộc sau {0} mục (mã lỗi cuối {1}). Không thể tin một danh sách không có điểm dừng, nên InstallerClean đã dừng. Không có gì bị xóa.`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer chưa bao giờ báo hiệu kết thúc danh sách bản vá của một chương trình: InstallerClean đã đọc {2} {3}, rồi bỏ cuộc sau {0} mục (mã lỗi cuối {1}). Không thể tin một danh sách không có điểm dừng, nên InstallerClean đã dừng. Không có gì bị xóa.`,
  'UpdateCheck.Status.UpdateAvailable': `Phiên bản {0} đã có.`,
  'Completion.DonateAsk': `Rất vui vì đã giúp được. Nếu bạn có lòng, một ly cà phê cũng quý.`,
  'About.Link.Guide': `Hướng dẫn và câu hỏi thường gặp`,
  'About.Link.ReportProblem': `Báo cáo vấn đề`,
  'About.AutoUpdateCheck': `Tự động kiểm tra cập nhật`,
  'Automation.About.Guide.HelpText': `Mở readme trên github trong trình duyệt của bạn.`,
  'Automation.About.ReportProblem.HelpText': `Mở trình theo dõi vấn đề (Issues) trên github.com trong trình duyệt của bạn.`,
  'Automation.AutoUpdateCheck.HelpText': `Nếu được đánh dấu, InstallerClean sẽ kiểm tra github xem có phiên bản mới hơn không khi bạn chạy nó.`,
  'Tooltip.MoveSameDrive': `Chuyển các tệp không cần thiết vào thư mục sao lưu. Thư mục đó nằm trên cùng ổ đĩa, nên dung lượng chỉ được giải phóng sau khi bạn xóa thư mục đó.`,
  'Confirm.DeletePermanently.Singular': `Tệp này sẽ bị xóa vĩnh viễn. Việc này an toàn, nhưng nếu bạn muốn có bản sao lưu thì hãy dùng Chuyển.`,
  'Confirm.DeletePermanently.Plural': `Các tệp này sẽ bị xóa vĩnh viễn. Việc này an toàn, nhưng nếu bạn muốn có bản sao lưu thì hãy dùng Chuyển.`,
  'Error.ScanCacheRootUnresolved': `InstallerClean không khiến được Windows phân giải đường dẫn thật của {InstallerFolder}, nên không tệp nào có thể được chứng tỏ là nằm bên trong và không tệp nào được đề xuất để dọn. Lần quét này không tìm thấy gì vì phép kiểm tra ấy thất bại, chứ không phải vì thư mục đã sạch. Không có gì bị bỏ đi.`,
  'Automation.Scroll.ProductDetails': `Chi tiết sản phẩm`,
  'Body.PendingReboot.Other': `Windows Installer đang có việc dở dang, nên Chuyển và Xóa tạm dừng. InstallerClean sẽ không đụng vào {InstallerFolder} khi thư mục đang thay đổi. Xong rồi thì quét lại, hai nút sẽ trở lại.`,
  'Cli.TooManyArgumentsNoPath': `Lỗi: đối số thừa không mong đợi '{0}'. /s và /d không nhận thêm đối số nào, và mỗi lần chạy chỉ dùng được một cờ.`,
  'Cli.MissingFromDisk.Singular': `Windows có hồ sơ về {0} tệp không nằm trong {InstallerFolder}: {1}. Trong sử dụng hằng ngày điều này không gây rắc rối, nhưng việc cập nhật hoặc gỡ cài đặt chương trình đó có thể thất bại. Để đưa tệp trở lại, bạn cần bộ cài của đúng phiên bản bạn đang có. Hãy lấy từ nhà sản xuất chương trình và chạy đè lên bản đang cài. Phiên bản mới hơn không dùng được: nó phải gỡ bản bạn đang có trước, và chính bước đó mới cần tệp này. Gỡ cài đặt trước cũng không được, vì cùng lý do. Việc này sẽ khôi phục tệp và giữ nguyên các thiết lập của bạn, nhưng Microsoft không bảo đảm điều đó.`,
  'Cli.MissingFromDisk.Plural': `Windows có hồ sơ về {0} tệp không nằm trong {InstallerFolder}: {1}. Trong sử dụng hằng ngày điều này không gây rắc rối, nhưng việc cập nhật hoặc gỡ cài đặt các chương trình đó có thể thất bại. Để đưa một tệp trở lại, bạn cần bộ cài của đúng phiên bản chương trình đó mà bạn đang có. Hãy lấy từ nhà sản xuất chương trình và chạy đè lên bản đang cài. Phiên bản mới hơn không dùng được: nó phải gỡ bản bạn đang có trước, và chính bước đó mới cần tệp đó. Gỡ cài đặt trước cũng không được, vì cùng lý do. Việc này sẽ khôi phục tệp và giữ nguyên các thiết lập của bạn, nhưng Microsoft không bảo đảm điều đó.`,
  'Cli.MoveNotEnoughSpace': `Lỗi: không đủ dung lượng tại {0}. Chuyển các tệp này cần {1} mà chỉ còn trống {2}. Không có gì được chuyển.`,
  'Cli.PendingRebootBlocked.Other': `Lỗi: Windows Installer đang có việc dở dang, nên /m và /d bị chặn. InstallerClean sẽ không đụng vào {InstallerFolder} khi thư mục đang thay đổi. Hãy thử lại khi xong.`,
  'Cli.FoundNoOrphans': `Không tìm thấy tệp không cần thiết nào.`,
  'Cli.NothingOffered.Singular': `InstallerClean không thể chắc chắn những tệp nào trong bộ nhớ đệm thuộc về các chương trình đã cài ở đây, nên đã giữ lại tệp duy nhất ({2}) thay vì đề xuất nó.`,
  'Cli.NothingOffered.Plural': `InstallerClean không thể chắc chắn những tệp nào trong bộ nhớ đệm thuộc về các chương trình đã cài ở đây, nên đã giữ lại toàn bộ {0} {1} ({2}) thay vì đề xuất chúng.`,
  'Cli.DestinationChangedMidBatch': `InstallerClean không còn xác nhận được thư mục sao lưu, nên đã dừng lại. Hãy kiểm tra {0}, rồi chạy lại lệnh.`,
  'Cli.Help.Summary': `Bỏ các tệp .msi và .msp trong bộ đệm mà không chương trình đã cài nào cần.`,
  'Cli.Help.Elevation': `Cần dấu nhắc quản trị viên; nếu không Windows sẽ không khởi chạy.`,
  'Error.InstallerLockUnavailableTitle': `Không có tệp nào bị xóa`,
  'Error.MoveInstallerLockUnavailableTitle': `Không có tệp nào được chuyển`,
  'Error.InstallerLockUnavailable': `InstallerClean không lấy được khóa mà Windows Installer dùng để ngăn hai chương trình cùng lúc thay đổi phần mềm đã cài, nên không thể loại trừ khả năng một tệp trở nên cần thiết giữa chừng, và không có gì bị xóa. Hãy thử lại, và khởi động lại Windows nếu việc này cứ tiếp diễn.`,
  'Error.MoveInstallerLockUnavailable': `InstallerClean không lấy được khóa mà Windows Installer dùng để ngăn hai chương trình cùng lúc thay đổi phần mềm đã cài, nên không thể loại trừ khả năng một tệp trở nên cần thiết giữa chừng, và không có gì được chuyển. Hãy thử lại, và khởi động lại Windows nếu việc này cứ tiếp diễn.`,
  'Cli.InstallerLockUnavailable': `Lỗi: InstallerClean không lấy được khóa Windows Installer vốn ngăn hai chương trình cùng lúc thay đổi phần mềm đã cài, nên không thể loại trừ khả năng một tệp trở nên cần thiết giữa chừng. Không có gì bị xóa. Hãy thử lại, và khởi động lại Windows nếu việc này cứ tiếp diễn.`,
  'Cli.MoveInstallerLockUnavailable': `Lỗi: InstallerClean không lấy được khóa Windows Installer vốn ngăn hai chương trình cùng lúc thay đổi phần mềm đã cài, nên không thể loại trừ khả năng một tệp trở nên cần thiết giữa chừng. Không có gì được chuyển. Hãy thử lại, và khởi động lại Windows nếu việc này cứ tiếp diễn.`,
  'Completion.ReverifyIdentityClaimed': `Đã giữ nguyên {0} {1}, vì Windows có bản ghi về chương trình được nêu tên bên trong.`,
  'Completion.ReverifyIdentityUnreadable': `Đã giữ nguyên {0} {1}, vì InstallerClean không tìm thấy tên chương trình nào bên trong.`,
  'Error.ScanNoRegisteredFileInFolder': `InstallerClean không khớp được các bản ghi Windows Installer với nội dung trong {InstallerFolder}. Thư mục có tệp bên trong, nhưng không một bản ghi nào trỏ tới bất cứ thứ gì trong đó, nên không tệp nào có thể được chứng tỏ là không cần thiết. Không có gì được đề xuất và không có gì bị bỏ đi.`,
  'Completion.NothingOffered': `Không có gì được đề xuất trên máy này`,
  'Completion.NothingOfferedBody.Singular': `InstallerClean không thể chắc chắn những tệp nào trong bộ nhớ đệm thuộc về các chương trình đã cài ở đây, nên đã giữ lại tệp duy nhất ({2}) thay vì đề xuất nó.`,
  'Completion.NothingOfferedBody.Plural': `InstallerClean không thể chắc chắn những tệp nào trong bộ nhớ đệm thuộc về các chương trình đã cài ở đây, nên đã giữ lại toàn bộ {0} {1} ({2}) thay vì đề xuất chúng.`,
  'Summary.SupersededHeldBack.Singular': `InstallerClean không thể chắc chắn rằng tệp bị thay thế duy nhất đó không còn cần đến nữa, nên đã giữ lại nó.`,
  'Summary.SupersededHeldBack.Plural': `InstallerClean không thể chắc chắn rằng {0} tệp bị thay thế không còn cần đến nữa, nên đã giữ lại chúng.`,
  'Cli.SupersededHeldBack.Singular': `InstallerClean không thể chắc chắn rằng tệp bị thay thế duy nhất đó không còn cần đến nữa, nên đã giữ lại nó.`,
  'Cli.SupersededHeldBack.Plural': `InstallerClean không thể chắc chắn rằng {0} tệp bị thay thế không còn cần đến nữa, nên đã giữ lại chúng.`,
  'Completion.HeldBack.Singular': `Đã giữ lại {0} tệp. Lần quét cho rằng nó không cần thiết. Lần kiểm tra cuối không xác nhận được điều đó.`,
  'Completion.HeldBack.Plural': `Đã giữ lại {0} tệp. Lần quét cho rằng chúng không cần thiết. Lần kiểm tra cuối không xác nhận được điều đó.`,
  'Body.PendingReboot.PendingRenameUnresolved': `Có một thao tác tệp đang xếp hàng chờ lần khởi động lại tới và InstallerClean không biết được thao tác đó nêu tên những tệp nào, nên không thể loại trừ khả năng chúng nằm trong {InstallerFolder}. Hãy khởi động lại Windows trước khi dọn dẹp.`,
  'Completion.MoveRestoreHint': `Hãy xóa thư mục đó khi bạn đã yên tâm rằng mọi thứ đều ổn.`,
  'Completion.MoveRestoreHintSameDrive': `Hãy xóa thư mục đó khi bạn đã yên tâm rằng mọi thứ đều ổn. Chỉ khi đó dung lượng mới thực sự được giải phóng.`,
  'Confirm.MoveDestination.Singular': `Tệp này sẽ được chuyển tới:`,
  'Confirm.MoveDestination.Plural': `Các tệp này sẽ được chuyển tới:`,
  'Cli.NothingListed.Singular': `InstallerClean không thể chắc chắn những tệp nào trong bộ nhớ đệm thuộc về các chương trình đã cài ở đây, nên đã giữ lại tệp duy nhất ({2}) thay vì đề xuất nó.`,
  'Cli.NothingListed.Plural': `InstallerClean không thể chắc chắn những tệp nào trong bộ nhớ đệm thuộc về các chương trình đã cài ở đây, nên đã giữ lại {0} {1} ({2}) thay vì đề xuất chúng.`,
  'Cli.WithheldReasons.Header': `Vì sao không thể chắc chắn:`,
  'Cli.WithheldReasons.RecordedPath': `  Một đường dẫn tệp trong chính hồ sơ của Windows Installer không phân giải được, nên không thể đối chiếu gì với nó.`,
  'Cli.WithheldReasons.FileIdentity': `  Không nhận dạng được một tệp mà Windows có hồ sơ, nên không thể đối chiếu nó với những gì có trong thư mục.`,
  'Cli.WithheldReasons.SecondInstance': `  Một chương trình có thể đã được cài nhiều hơn một lần trên máy này, và hồ sơ không cho biết một tệp thuộc về bản sao nào.`,
  'Cli.PendingRebootBlocked.PendingRenameUnresolved': `Lỗi: có một thao tác tệp đang xếp hàng chờ lần khởi động lại tới và InstallerClean không biết được thao tác đó nêu tên những tệp nào, nên không thể loại trừ {InstallerFolder}. Hãy khởi động lại Windows trước khi dọn dẹp.`,
  'Cli.MoveRestoreHint': `Hãy kiểm tra xem các chương trình của bạn vẫn cập nhật và gỡ cài đặt bình thường, rồi xóa {0}.`,
  'Error.ScanStoppedDetails': `Điều này cũng được ghi lại trong {0}.`,
  'Cli.NothingListedPerFile.Singular': `InstallerClean không chắc chắn về một trong những tệp trong bộ nhớ đệm mà nó tìm thấy, nên đã giữ lại chính tệp đó ({2}) thay vì đề xuất nó.`,
  'Cli.NothingListedPerFile.Plural': `InstallerClean không chắc chắn về một số tệp trong bộ nhớ đệm mà nó tìm thấy, nên đã giữ lại {0} {1} ({2}) thay vì đề xuất chúng.`,
  'Cli.NothingOfferedPerFile.Singular': `InstallerClean không thể xác định rằng tệp trong bộ nhớ đệm mà nó tìm thấy là không cần thiết, nên đã giữ lại đúng tệp đó ({2}) thay vì đề xuất nó.`,
  'Cli.NothingOfferedPerFile.Plural': `InstallerClean không thể xác định rằng bất kỳ tệp nào trong bộ nhớ đệm mà nó tìm thấy là không cần thiết, nên đã giữ lại toàn bộ {0} {1} ({2}) thay vì đề xuất chúng.`,
  'Completion.NothingOfferedPerFileBody.Singular': `InstallerClean không thể xác định rằng tệp trong bộ nhớ đệm mà nó tìm thấy là không cần thiết, nên đã giữ lại đúng tệp đó ({2}) thay vì đề xuất nó.`,
  'Completion.NothingOfferedPerFileBody.Plural': `InstallerClean không thể xác định rằng bất kỳ tệp nào trong bộ nhớ đệm mà nó tìm thấy là không cần thiết, nên đã giữ lại toàn bộ {0} {1} ({2}) thay vì đề xuất chúng.`,
  'Summary.NothingListedPerFile.Singular': `InstallerClean không chắc chắn về một trong những tệp trong bộ nhớ đệm mà nó tìm thấy, nên đã giữ lại nó thay vì đề xuất.`,
  'Summary.NothingListedPerFile.Plural': `InstallerClean không chắc chắn về một số tệp trong bộ nhớ đệm mà nó tìm thấy, nên đã giữ lại {0} {1} thay vì đề xuất chúng.`,
  'Cli.WithheldReasons.CandidateIdentity': `  Không nhận dạng được một tệp trong thư mục, nên không thể đối chiếu nó với hồ sơ.`,
  'Cli.WithheldReasons.DeclaredProductInstalled': `  Một tệp khai rằng nó thuộc về một chương trình vẫn còn được cài, nên có thể vẫn còn cần đến.`,
  'Cli.WithheldReasons.DeclaredProductUnestablished': `  Hoặc một tệp không cho biết nó thuộc về chương trình nào, hoặc Windows không trả lời về chương trình đó.`,
  'Cli.WithheldReasons.ScreenUnanswered': `  Một lần kiểm tra xem các tệp thuộc về những chương trình nào đã trả về các câu trả lời không khớp với những tệp được giao cho nó.`,
  'Body.PendingReboot.RegistryCheckUnreadable': `InstallerClean couldn't read one of the Windows settings it checks before touching {InstallerFolder}, so it can't tell whether an installer operation is running or waiting for a restart. Restart Windows and Re-scan. If the setting still won't read, this isn't a machine InstallerClean can clean.`,
  'Cli.InstallerLockAccessRefused': `Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted.`,
  'Cli.MoveCancelledRestoreHint': `It's simple to undo. Move them back from {0} into {InstallerFolder} and everything will be back to how it was.`,
  'Cli.MoveInstallerLockAccessRefused': `Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved.`,
  'Cli.PendingRebootBlocked.RegistryCheckUnreadable': `Error: InstallerClean couldn't read one of the registry values it checks before touching {InstallerFolder}, so it can't rule out a Windows Installer operation in flight or queued for the next restart. /m and /d are blocked. Restart Windows and try again. If the read still fails, this isn't a machine InstallerClean can clean.`,
  'Completion.MoveCancelledRestoreHint': `It's simple to undo. Move them back into {InstallerFolder} and everything will be back to how it was.`,
  'Error.InstallerLockAccessRefused': `Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted.`,
  'Error.MoveInstallerLockAccessRefused': `Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved.`,
  'Error.MoveStoppedTitle': `Move stopped`,
  'Field.NoNamedProduct': `(no program)`,
  'Summary.RegisteredWindow.Missing.Plural': `{0} missing`,
  'Summary.RegisteredWindow.Missing.Singular': `{0} missing`,
  'UpdateCheck.Failed.Unknown.NoLog': `The check failed for an unknown reason. The crash log could not be written.`,
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
