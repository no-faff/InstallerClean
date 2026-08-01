# InstallerClean in Tiếng Việt (Vietnamese)

The text of InstallerClean's interface and command-line tool in English on the left, with the Vietnamese translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Vietnamese can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.vi.resx`](../../src/InstallerClean.Core/Resources/Strings.vi.resx), so do not edit it by hand. The Vietnamese translation itself lives in [`gen-strings-vi.mjs`](../../scripts/translations/gen-strings-vi.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | Tiếng Việt |
| --- | --- |
| InstallerClean | InstallerClean |
| About | Giới thiệu |
| Registered files that should not be deleted | Tệp đã đăng ký, không nên xóa |
| Unneeded files that are safe to delete | Tệp không cần thiết, có thể xóa an toàn |

## Section headings

| English | Tiếng Việt |
| --- | --- |
| PRODUCTS | SẢN PHẨM |
| PATCHES | BẢN VÁ |
| PRODUCT DETAILS | CHI TIẾT SẢN PHẨM |
| BACKUP FOLDER | BACKUP FOLDER |
| SAY THANKS | LỜI CẢM ƠN |

## Buttons and actions

| English | Tiếng Việt |
| --- | --- |
| _About | _Giới thiệu |
| Copy | Sao chép |
| Cut | Cắt |
| Paste | Dán |
| Select all | Chọn tất cả |
| _Browse... | _Duyệt... |
| _Cancel | _Hủy |
| Check for _updates | _Kiểm tra cập nhật |
| _Close | Đón_g |
| _Delete permanently | _Xóa vĩnh viễn |
| _Done | _Xong |
| Details | Chi tiết |
| _Buy me a cuppa | _Mời tôi một ly cà phê |
| Leave a _star on GitHub | Gắn _sao trên GitHub |
| Apache 2.0 licence | Giấy phép Apache 2.0 |
| _Move | _Chuyển |
| Path to folder if you move rather than delete. | Path to folder if you move rather than delete. |
| Open _release page | _Mở trang phát hành |
| _Re-scan | _Quét lại |
| _Scan again | Quét _lại |
| Send report | Gửi báo cáo |
| _Send | _Gửi |

## About window

| English | Tiếng Việt |
| --- | --- |
| Guide and FAQ | Hướng dẫn và câu hỏi thường gặp |
| Report a problem | Báo cáo vấn đề |
| Check for updates automatically | Tự động kiểm tra cập nhật |

## Field labels

| English | Tiếng Việt |
| --- | --- |
| Reason | Lý do |
| Author | Tác giả |
| Application | Ứng dụng |
| Title | Tiêu đề |
| Subject | Chủ đề |
| Keywords | Từ khóa |
| Signing certificate | Chứng chỉ ký |
| File size | Kích thước tệp |
| Comment | Chú thích |
| Product name | Tên sản phẩm |
| File | Tệp |
| Size | Kích thước |
| Patches | Bản vá |
| (unknown) | (không rõ) |
| (patches only) | (chỉ bản vá) |
| missing | thiếu |

## Status and progress

| English | Tiếng Việt |
| --- | --- |
| Scanning... | Đang quét... |
| Cancelling... | Đang hủy... |
| Starting scan... | Đang bắt đầu quét... |
| Asking Windows about installed software... | Đang hỏi Windows về phần mềm đã cài... |
| Scanning installer cache folder... | Đang quét thư mục bộ nhớ đệm trình cài đặt... |
| Enumerating installed products... | Đang liệt kê các sản phẩm đã cài... |
| Checking registry for additional packages... | Đang kiểm tra sổ đăng ký để tìm các gói bổ sung... |
| Found {0} registered {1}. | Đã tìm thấy {0} {1} đã đăng ký. |
| Scan complete ({0}) | Quét xong ({0}) |
| Scanning local packages... | Đang quét các gói cục bộ... |
| Found {0} {1} you can safely delete. | Đã tìm thấy {0} {1} bạn có thể xóa an toàn. |
| Preparing destination folder... | Đang chuẩn bị thư mục đích... |
| Moving files... | Moving files... |
| Deleting files... | Deleting files... |
| Move cancelled. {0} of {1} {2} processed. | Đã hủy chuyển. Đã xử lý {0}/{1} {2}. |
| Delete cancelled. {0} of {1} {2} processed. | Đã hủy xóa. Đã xử lý {0}/{1} {2}. |
| Move failed ({0}). Details in {1}. | Chuyển thất bại ({0}). Chi tiết trong {1}. |
| Move failed ({0}). The crash log could not be written. | Chuyển thất bại ({0}). Không thể ghi nhật ký sự cố. |
| Delete failed ({0}). Details in {1}. | Xóa thất bại ({0}). Chi tiết trong {1}. |
| Delete failed ({0}). The crash log could not be written. | Xóa thất bại ({0}). Không thể ghi nhật ký sự cố. |
| Access denied. Windows refused the scan. | Truy cập bị từ chối. Windows đã từ chối lần quét. |
| Scan failed: couldn't read the Windows Installer records. | Quét thất bại: không thể đọc các bản ghi Windows Installer. |
| Scan cancelled. | Đã hủy quét. |
| Ready | Sẵn sàng |
| Scan failed ({0}). Details in {1}. | Quét thất bại ({0}). Chi tiết trong {1}. |
| Scan failed ({0}). The crash log could not be written. | Quét thất bại ({0}). Không thể ghi nhật ký sự cố. |

## Main screen text

| English | Tiếng Việt |
| --- | --- |
| Any unneeded files below are [safe to delete]. | Any unneeded files below are [safe to delete]. |
| They sit in {InstallerFolder}, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | Chúng nằm trong {InstallerFolder}, còn sót lại khi một chương trình bị gỡ cài đặt ({0}), khi một bản vá mới hơn thay thế một bản ({1}) hoặc khi nhà phát hành thu hồi nó ({2}). InstallerClean chỉ liệt kê những tệp mà chính Windows báo là đã dùng xong. |
| Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored. | Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored. |
| Nothing scanned yet. | Chưa quét gì cả. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Nhấn Quét lại để tìm trong {InstallerFolder} những tệp cài đặt mà không chương trình nào còn cần. |
| These files can't be cleaned up right now. | Hiện chưa thể dọn những tệp này. |
| Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. |
| Select a file to view details. | Chọn một tệp để xem chi tiết. |
| Select a product to view details. | Chọn một sản phẩm để xem chi tiết. |
| No metadata available. | Không có siêu dữ liệu. |
| This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. | This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. |
| The README [explains this folder], and how to recover a file, in Microsoft's own words. | README [giải thích thư mục này], và cách khôi phục một tệp, bằng chính lời của Microsoft. |
| (none) | (không có) |

## Reasons a file is unneeded

| English | Tiếng Việt |
| --- | --- |
| Orphaned | Bị bỏ lại |
| Superseded | Bị thay thế |
| Obsoleted | Đã lỗi thời |

## Completion screen

| English | Tiếng Việt |
| --- | --- |
| All clean | Đã sạch |
| Nothing to clean up in {InstallerFolder} | Không còn gì để dọn trong {InstallerFolder} |
| Scanned {0} {1} in {2} | Đã quét {0} {1} trong {2} |
| The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). |
| The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). |
| The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). |
| The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). |
| {0} freed | Đã giải phóng {0} |
| {0} moved | Đã chuyển {0} |
| Nothing was moved | Không có tệp nào được chuyển |
| Nothing was deleted | Không có tệp nào bị xóa |
| {0} of {1} could not be moved. | Không thể chuyển {0} trong số {1} tệp. |
| {0} of {1} could not be moved. | Không thể chuyển {0} trong số {1} tệp. |
| {0} of {1} could not be deleted. | Không thể xóa {0} trong số {1} tệp. |
| {0} of {1} could not be deleted. | Không thể xóa {0} trong số {1} tệp. |
| {0} {1} moved to: {2} | Đã chuyển {0} {1} tới: {2} |
| {0} {1} moved to: {2} | Đã chuyển {0} {1} tới: {2} |
| {0} {1} kept in place, because a program went back to needing what the scan flagged. | Đã giữ nguyên {0} {1} vì sau lần quét, một chương trình lại cần đến chúng. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read when the check was repeated. | Đã giữ nguyên {0} {1} vì khi kiểm tra lại, không thể đọc đầy đủ các bản ghi Windows Installer. |
| Moved {0} of {1} {2} before you cancelled. | Đã chuyển {0}/{1} {2} trước khi bạn hủy. |
| Permanently deleted {0} of {1} {2} before you cancelled. | Đã xóa vĩnh viễn {0}/{1} {2} trước khi bạn hủy. |
| {0} {1} permanently deleted | {0} {1} permanently deleted |
| {0} {1} permanently deleted | {0} {1} permanently deleted |
| Glad to help. There's a tip jar if you're feeling kind. | Rất vui vì đã giúp được. Nếu bạn có lòng, một ly cà phê cũng quý. |

## Summaries and counts

| English | Tiếng Việt |
| --- | --- |
| {0} file still needed | {0} tệp vẫn cần thiết |
| {0} files still needed | {0} tệp vẫn cần thiết |
| {0} unneeded file to clean up | {0} tệp không cần thiết để dọn |
| {0} unneeded files to clean up | {0} tệp không cần thiết để dọn |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | {0} tệp đã đăng ký bị thiếu (không phải do InstallerClean xóa). Hiện chưa có vấn đề, nhưng một lần sửa chữa, cập nhật hoặc gỡ cài đặt chương trình liên quan trong tương lai có thể thất bại. Mở Chi tiết để biết cần làm gì. |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | {0} tệp đã đăng ký bị thiếu (không phải do InstallerClean xóa). Hiện chưa có vấn đề, nhưng một lần sửa chữa, cập nhật hoặc gỡ cài đặt chương trình liên quan trong tương lai có thể thất bại. Mở Chi tiết để biết cần làm gì. |
| {0} installed program could not be read during this scan, so superseded patches have been kept. Orphaned files are not affected. | Không thể đọc {0} chương trình đã cài trong lần quét này, nên các bản vá bị thay thế đã được giữ lại. Các tệp bị bỏ lại không bị ảnh hưởng. |
| {0} installed programs could not be read during this scan, so superseded patches have been kept. Orphaned files are not affected. | Không thể đọc {0} chương trình đã cài trong lần quét này, nên các bản vá bị thay thế đã được giữ lại. Các tệp bị bỏ lại không bị ảnh hưởng. |
| {0} of {1} {2} | {0}/{1} {2} |
| {0} orphaned, {1} superseded, {2} obsoleted ({3}) | {0} bị bỏ lại, {1} bị thay thế, {2} đã lỗi thời ({3}) |
| {0} registered file that is still needed ({1}) | {0} tệp đã đăng ký vẫn cần thiết ({1}) |
| {0} registered files that are still needed ({1}) | {0} tệp đã đăng ký vẫn cần thiết ({1}) |

## Confirmation dialogs

| English | Tiếng Việt |
| --- | --- |
| Move {0} {1} ({2})? | Chuyển {0} {1} ({2})? |
| Files will be moved to: | Các tệp sẽ được chuyển tới: |
| Delete {0} {1} ({2})? | Xóa {0} {1} ({2})? |
| This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. | This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. |
| Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. | Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. |

## Error messages

| English | Tiếng Việt |
| --- | --- |
| Access denied | Truy cập bị từ chối |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows đã từ chối quyền truy cập của InstallerClean, nên nó đã dừng lại. Không có gì bị xóa.<br><br>InstallerClean vốn đã chạy với quyền quản trị viên, nên khởi động lại theo cách đó cũng không giúp được gì. Windows không nói gì thêm về thứ đã từ chối quyền truy cập, nên không có gì cụ thể để thử. |
| Couldn't read the Windows Installer records | Không thể đọc các bản ghi Windows Installer |
| Scan failed | Quét thất bại |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed. | Các bản ghi Windows Installer trả về hoàn toàn trống: không một chương trình đã cài hay bản cập nhật nào nhận là chủ của một tệp cài đặt trong bộ nhớ đệm. Điều đó không xảy ra trên một máy hoạt động bình thường (ngay cả một bản Windows vừa cài cũng có vài tệp như vậy), nên hoặc các bản ghi đã hỏng, hoặc không đọc được, và một lần quét tin vào câu trả lời này sẽ nhầm lẫn coi mọi tệp trong {InstallerFolder} là bị bỏ lại. Thay vào đó InstallerClean đã dừng. Không có gì bị xóa. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer không cho phép InstallerClean liệt kê những gì đã được cài. InstallerClean vốn đã chạy với quyền quản trị viên, nên chạy lại với quyền quản trị viên cũng không thay đổi được gì. Không có danh sách đó thì không có cách nào an toàn để biết tệp nào trong bộ nhớ đệm vẫn còn cần, nên InstallerClean đã dừng. Không có gì bị xóa. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer không thể đưa cho InstallerClean một danh sách chương trình đã cài đọc được: {0} mục liên tiếp trả về không đọc được (mã lỗi cuối {1}). Thay vì làm việc với một danh sách chỉ đọc được một phần, InstallerClean đã dừng. Không có gì bị xóa. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer chưa bao giờ báo hiệu kết thúc danh sách chương trình đã cài: InstallerClean đã bỏ cuộc sau {0} mục (mã lỗi cuối {1}). Không thể tin một danh sách không có điểm dừng, nên InstallerClean đã dừng. Không có gì bị xóa. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer chưa bao giờ báo hiệu kết thúc danh sách bản vá của một chương trình: InstallerClean đã bỏ cuộc sau {0} mục (mã lỗi cuối {1}). Không thể tin một danh sách không có điểm dừng, nên InstallerClean đã dừng. Không có gì bị xóa. |
| InstallerClean couldn't square this scan with the Windows Installer records: every file Windows still lists as needed is missing from {InstallerFolder}, while the files actually in the folder match nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. | InstallerClean không thể khớp lần quét này với các bản ghi Windows Installer: mọi tệp mà Windows vẫn liệt kê là cần thiết đều không có trong {InstallerFolder}, trong khi các tệp thực sự nằm trong thư mục lại không khớp với bản ghi nào. Không có máy thật nào trông như vậy, nên điều này cho thấy có vấn đề khi đọc các bản ghi, chứ không phải là các tệp bạn có thể xóa an toàn. Chưa có gì được đưa ra để dọn dẹp và không có gì bị xóa. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean không đọc được đủ các bản ghi Windows Installer để chắc chắn thứ gì vẫn còn cần: danh sách chương trình đã cài trả về thiếu, và việc đọc chính các bản ghi đó trực tiếp từ sổ đăng ký cũng gặp lỗi. Một tệp có thể trông như bị bỏ lại chỉ vì bản ghi nêu tên nó nằm trong số những bản ghi không đọc được, nên InstallerClean đã dừng. Không có gì bị xóa. |
| Invalid destination | Đích không hợp lệ |
| Could not write to destination | Không thể ghi vào đích |
| Move failed | Chuyển thất bại |
| Delete failed | Xóa thất bại |
| Setting not saved | Không lưu được cài đặt |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | Không thể lưu thay đổi. Lần chạy tiếp theo, InstallerClean sẽ quay lại cài đặt trước đó. |
| The destination cannot be inside the Windows Installer folder. | Đích không thể nằm bên trong thư mục Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | Đích {0} nằm dưới một thư mục hệ thống của Windows. Hãy chọn một đường dẫn ngoài %SystemRoot%, %ProgramFiles% và %ProgramData%. |
| Not enough space | Không đủ dung lượng |
| Not enough space at {0}<br><br>Required: {1}<br>Available: {2} | Không đủ dung lượng tại {0}<br><br>Cần: {1}<br>Còn trống: {2} |
| You don't have permission to write to {0}.<br>Try a folder in your user profile or on a drive you own. | Bạn không có quyền ghi vào {0}.<br>Hãy thử một thư mục trong hồ sơ người dùng của bạn hoặc trên một ổ đĩa bạn sở hữu. |
| The path {0} is too long for Windows. Pick a shorter path. | Đường dẫn {0} quá dài đối với Windows. Hãy chọn một đường dẫn ngắn hơn. |
| The folder {0} does not exist and could not be created. Check the drive letter or network path. | Thư mục {0} không tồn tại và không thể tạo được. Hãy kiểm tra ký tự ổ đĩa hoặc đường dẫn mạng. |
| Windows cannot write to {0}.<br>Details in {1}. | Windows không thể ghi vào {0}.<br>Chi tiết trong {1}. |
| Windows cannot write to {0}. The crash log could not be written. | Windows không thể ghi vào {0}. Không thể ghi nhật ký sự cố. |
| Cannot write to {0}.<br>Details in {1}. | Không thể ghi vào {0}.<br>Chi tiết trong {1}. |
| Cannot write to {0}. The crash log could not be written. | Không thể ghi vào {0}. Không thể ghi nhật ký sự cố. |
| File no longer exists. | Tệp không còn tồn tại. |
| Source file is a symlink or junction; refused for safety. | Tệp nguồn là một symlink hoặc junction; bị từ chối vì lý do an toàn. |
| This file is not directly inside the Windows Installer folder; refused for safety. | Tệp này không nằm trực tiếp trong thư mục Windows Installer; bị từ chối vì lý do an toàn. |
| Windows refused access to this file; it was left in place. | Windows từ chối truy cập tệp này; tệp được giữ nguyên tại chỗ. |
| Windows refused access to these files; they were left in place. | Windows từ chối truy cập các tệp này; các tệp được giữ nguyên tại chỗ. |
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. |
| Windows reported a file error; the file was left in place. | Windows báo một lỗi tệp; tệp được giữ nguyên tại chỗ. |
| Windows reported file errors; these files were left in place. | Windows báo lỗi tệp; các tệp này được giữ nguyên tại chỗ. |
| Something went wrong with this file; it was left in place. | Đã có trục trặc với tệp này; tệp được giữ nguyên tại chỗ. |
| Something went wrong with these files; they were left in place. | Đã có trục trặc với các tệp này; các tệp được giữ nguyên tại chỗ. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Từ chối chuyển tệp vào thư mục Windows Installer (đích: {0}). |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} |
| The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. |
| Cannot write to {0}. | Không thể ghi vào {0}. |
| Could not find a unique filename for '{0}' after 10,000 attempts. | Không tìm được tên tệp duy nhất cho '{0}' sau 10.000 lần thử. |

## Update check

| English | Tiếng Việt |
| --- | --- |
| Check for updates | Kiểm tra cập nhật |
| Checking... | Đang kiểm tra... |
| Up to date. | Đã cập nhật. |
| Version {0} is available. | Phiên bản {0} đã có. |
| Update available | Có bản cập nhật |
| You're running version {0}.<br>Version {1} is available. | Bạn đang dùng phiên bản {0}.<br>Phiên bản {1} đã có. |
| Couldn't reach GitHub. Check your internet connection and try again. | Không thể kết nối tới GitHub. Hãy kiểm tra kết nối internet và thử lại. |
| GitHub returned an error response. Try again in a few minutes. | GitHub trả về phản hồi lỗi. Hãy thử lại sau vài phút. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | Phản hồi của GitHub không chứa bản phát hành nào nhận ra được. Hãy thử lại sau, hoặc mở thẳng trang phát hành. |
| The check timed out. Your connection to GitHub may be slow; try again. | Quá thời gian kiểm tra. Kết nối của bạn tới GitHub có thể chậm; hãy thử lại. |
| The check failed for an unknown reason. Details are in crash.log if you need to report it. | Việc kiểm tra thất bại vì một lý do không xác định. Chi tiết nằm trong crash.log nếu bạn cần báo cáo. |

## Opening links in your browser

| English | Tiếng Việt |
| --- | --- |
| Couldn't open your browser | Không thể mở trình duyệt của bạn |
| InstallerClean couldn't open your browser. The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | InstallerClean không thể mở trình duyệt của bạn. Liên kết đã được sao chép vào bảng tạm, nên bạn có thể tự dán nó vào:<br><br>{0} |
| InstallerClean couldn't open your browser, and couldn't copy the link to your clipboard either. The link is:<br><br>{0} | InstallerClean không thể mở trình duyệt của bạn, và cũng không thể sao chép liên kết vào bảng tạm. Liên kết là:<br><br>{0} |

## Sending the summary

| English | Tiếng Việt |
| --- | --- |
| Sending... | Đang gửi... |
| Thanks! Report sent. | Cảm ơn! Đã gửi báo cáo. |
| Sending failed. Try again later. | Gửi thất bại. Hãy thử lại sau. |
| No report to send. | Không có báo cáo để gửi. |
| Send this? | Gửi cái này? |
| It goes to nofaff.netlify.app/api/result-log. Nothing identifies you or your machine; it just lets me know InstallerClean's working and [how much space people are freeing]. | Nó được gửi tới nofaff.netlify.app/api/result-log. Không có gì nhận dạng bạn hay máy của bạn; nó chỉ cho tôi biết InstallerClean có hoạt động không và [mọi người đang giải phóng được bao nhiêu dung lượng]. |

## Startup and crashes

| English | Tiếng Việt |
| --- | --- |
| InstallerClean | InstallerClean |
| InstallerClean is already running. | InstallerClean đang chạy rồi. |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | Đã xảy ra lỗi không mong muốn và InstallerClean cần đóng lại.<br><br>{0}<br><br>Chi tiết đã được ghi vào:<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | Đã xảy ra lỗi không mong muốn và InstallerClean cần đóng lại.<br><br>{0}<br><br>Không thể ghi nhật ký sự cố. |
| Startup error | Lỗi khởi động |
| Failed to start ({0}). Details written to:<br>{1} | Khởi động thất bại ({0}). Chi tiết đã được ghi vào:<br>{1} |
| Failed to start ({0}). The crash log could not be written. | Khởi động thất bại ({0}). Không thể ghi nhật ký sự cố. |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> |

## Tooltips (hover text)

| English | Tiếng Việt |
| --- | --- |
| It's thirsty work! | Làm việc này khát nước lắm! |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Đã yêu cầu hủy. InstallerClean đang chờ bước hiện tại tới điểm dừng. Việc này có thể mất vài giây khi I/O nặng hoặc khi đang gọi cơ sở dữ liệu MSI. |
| Close | Đóng |
| A star helps other people find it. | Một ngôi sao giúp người khác tìm thấy InstallerClean. |
| Minimise | Thu nhỏ |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | Tùy bạn, nhưng rất được trân trọng. Gửi một bản tóm tắt ẩn danh chỉ để cho tôi biết nó có hoạt động không và mọi người đang giải phóng được bao nhiêu dung lượng. Màn hình tiếp theo cho bạn xem những gì sẽ được gửi trước khi bạn xác nhận. |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | Tùy bạn, nhưng rất được trân trọng. Gửi một bản tóm tắt ẩn danh chỉ để cho tôi biết nó có hoạt động không. Màn hình tiếp theo cho bạn xem những gì sẽ được gửi trước khi bạn xác nhận. |
| Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. | Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. |
| Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. | Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. | Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. |
| Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. | Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Tên chủ thể từ chứng chỉ Authenticode được nhúng. Chưa xác minh chuỗi. |
| Change language. The program will restart. | Thay đổi ngôn ngữ. Chương trình sẽ khởi động lại. |

## Screen reader labels

| English | Tiếng Việt |
| --- | --- |
| Donate | Ủng hộ |
| Buy me a cuppa | Mời tôi một ly cà phê |
| Cancel operation | Hủy thao tác |
| Cancel scan | Hủy quét |
| Cancel startup scan | Hủy quét khi khởi động |
| Close | Đóng |
| Close window | Đóng cửa sổ |
| Close result and return to main window | Đóng kết quả và quay lại cửa sổ chính |
| Leave a star on github | Gắn sao trên github |
| Minimise | Thu nhỏ |
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Delete permanently removes the unneeded files. Cancel closes without deleting. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Chuyển sẽ đặt các tệp không cần thiết vào thư mục đích đã chọn. Hủy sẽ để chúng nguyên chỗ cũ. |
| Say thanks | Lời cảm ơn |
| Send posts the report shown to No Faff. Cancel sends nothing. | Gửi sẽ đăng báo cáo hiển thị tới No Faff. Hủy sẽ không gửi gì. |
| Check for updates | Kiểm tra cập nhật |
| Checks github's releases page for a newer version. | Kiểm tra trang phát hành của github xem có phiên bản mới hơn không. |
| Opens the readme on github in your browser. | Mở readme trên github trong trình duyệt của bạn. |
| Opens the issue tracker on github.com in your browser. | Mở trình theo dõi vấn đề (Issues) trên github.com trong trình duyệt của bạn. |
| If ticked, InstallerClean checks github for a newer version when you run it. | Nếu được đánh dấu, InstallerClean sẽ kiểm tra github xem có phiên bản mới hơn không khi bạn chạy nó. |
| Open the release page to download the newer version, or cancel to keep the current version. | Mở trang phát hành để tải phiên bản mới hơn, hoặc hủy để giữ phiên bản hiện tại. |
| Opens the licence file on github.com in your browser. | Mở tệp giấy phép trên github.com trong trình duyệt của bạn. |
| Backup folder | Backup folder |
| Products | Sản phẩm |
| Patches | Bản vá |
| Product details | Chi tiết sản phẩm |
| Backup folder | Backup folder |
| Operation progress | Tiến trình thao tác |
| Scan {InstallerFolder} again | Quét lại {InstallerFolder} |
| Scanning progress | Tiến trình quét |
| Startup scan progress | Tiến trình quét khi khởi động |
| Details, unneeded files | Chi tiết, tệp không cần thiết |
| Available for cleanup. | Có thể dọn dẹp. |
| Details, registered files | Chi tiết, tệp đã đăng ký |
| Read-only inventory. | Danh sách chỉ đọc. |
| Sorted by {0}, ascending | Đã sắp xếp theo {0}, tăng dần |
| Sorted by {0}, descending | Đã sắp xếp theo {0}, giảm dần |
| Scan results | Kết quả quét |
| Result details | Chi tiết kết quả |
| File details | Chi tiết tệp |
| Dialog text | Nội dung hộp thoại |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | Các tệp không thể xử lý |
| Explains this folder, and how to recover a file, in the README | Giải thích thư mục này, và cách khôi phục một tệp, trong README |
| Report preview | Xem trước báo cáo |
| Change language | Thay đổi ngôn ngữ |
| The program will restart. | Chương trình sẽ khởi động lại. |

## File picker

| English | Tiếng Việt |
| --- | --- |
| Choose destination folder for moved files | Chọn thư mục đích cho các tệp đã chuyển |

## Version

| English | Tiếng Việt |
| --- | --- |
| Version {0} | Phiên bản {0} |

## Word forms (singular and plural)

| English | Tiếng Việt |
| --- | --- |
| file | tệp |
| files | tệp |
| error | lỗi |
| errors | lỗi |
| package | gói |
| packages | gói |
| product | sản phẩm |
| products | sản phẩm |
| patch | bản vá |
| patches | bản vá |

## Sizes and times

| English | Tiếng Việt |
| --- | --- |
| {0:F2} GB | {0:F2} GB |
| {0:F1} MB | {0:F1} MB |
| {0:F1} KB | {0:F1} KB |
| {0} B | {0} B |
| {0:F0}ms | {0:F0}ms |
| {0:F1}s | {0:F1}s |
| less than a second | chưa đến một giây |
| {0:F1} seconds | {0:F1} giây |

## Command-line tool (installerclean-cli)

| English | Tiếng Việt |
| --- | --- |
| Unknown argument: '{0}' | Đối số không xác định: '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Lỗi: có thêm đối số không mong đợi '{0}'. Nếu thư mục đích của bạn có dấu cách, hãy đặt cả đường dẫn trong dấu ngoặc kép: /m "D:\My Backup" |
| Cancelling... | Đang hủy... |
| Cancelled. | Đã hủy. |
| Error: {0}. Details written to {1}. | Lỗi: {0}. Chi tiết đã ghi vào {1}. |
| Error: {0}. The crash log could not be written. | Lỗi: {0}. Không thể ghi nhật ký sự cố. |
| Scanning {InstallerFolder}... | Đang quét {InstallerFolder}... |
| Found {0} {1} to clean up ({2}). | Đã tìm thấy {0} {1} để dọn ({2}). |
| Nothing to do. | Không có gì để làm. |
| Deleting {0} {1}... | Đang xóa {0} {1}... |
| Permanently deleted {0} {1}. | Permanently deleted {0} {1}. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Lỗi: chưa chỉ định đích để chuyển. Dùng /m ĐƯỜNG_DẪN. (Mặc định đặt trong GUI là theo từng người dùng và không áp dụng cho các lần chạy theo lịch hoặc bằng tài khoản dịch vụ.) |
| Error: destination cannot be inside the Windows Installer folder. | Lỗi: đích không thể nằm bên trong thư mục Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Lỗi: đích phải là một đường dẫn đầy đủ. Nhận được: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | Lỗi: đích {0} nằm dưới một thư mục hệ thống của Windows. Hãy chọn một đường dẫn ngoài %SystemRoot%, %ProgramFiles% và %ProgramData%. |
| Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. |
| Moving {0} {1} to {2}... | Đang chuyển {0} {1} tới {2}... |
| Moved {0} {1}. | Đã chuyển {0} {1}. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Một tiến trình InstallerClean khác đang giữ khóa một-thực-thể (GUI hoặc một lần chạy CLI khác). Mã thoát 75 (tạm thời); có thể thử lại sau. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Lưu ý: ghi vào Nhật ký sự kiện thất bại. Hãy kiểm tra quyền của nhật ký Ứng dụng hoặc Chính sách nhóm. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - dọn dẹp {InstallerFolder} |
| Usage: | Cách dùng: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help        Hiển thị trợ giúp (cũng nhận /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version     In ra phiên bản (cũng nhận -v) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         Scan only - list unneeded files |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         Delete unneeded files permanently |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         Move to the saved backup folder |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m ĐƯỜNG_DẪN  Chuyển tới đường dẫn được chỉ định |
| installerclean-cli is a real console process and blocks the prompt | installerclean-cli là một tiến trình console thật và chặn dấu nhắc |
| until it finishes; redirect or pipe its output as you would any | cho đến khi xong; hãy chuyển hướng hoặc nối ống đầu ra của nó như |
| other console exe. The GUI lives in InstallerClean.exe alongside it. | với mọi tệp console khác. GUI nằm trong InstallerClean.exe cùng chỗ. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. |
| Exit codes: | Mã thoát: |
|   0   success: every flagged file was processed |   0   thành công: mọi tệp được đánh dấu đều đã được xử lý |
|   1   failure: nothing processed (bad args, scan failed, all files failed) |   1   thất bại: không xử lý được gì (đối số, quét hoặc mọi tệp) |
|   2   partial: some files processed, some failed |   2   một phần: một số tệp được xử lý, một số thất bại |
|   75  transient: a temporary condition blocked the run (see the message) |   75  tạm thời: một điều kiện tạm thời đã chặn lần chạy (xem thông báo) |
|   130 cancelled (Ctrl+C) |   130 đã hủy (Ctrl+C) |
