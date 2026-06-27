# InstallerClean in Tiếng Việt (Vietnamese)

The text of InstallerClean's interface and command-line tool in English on the left, with the Vietnamese translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Vietnamese can read through the translation and flag anything that doesn't read well. See [Can you help translate InstallerClean?](../../README.vi.md#can-you-help-translate-installerclean) for how to suggest a change, whether an issue or a pull request.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. The translation file itself is [`Strings.vi.resx`](../../src/InstallerClean.Core/Resources/Strings.vi.resx). This page is generated from it by `scripts/gen-translation-table.mjs`, so do not edit it by hand.

## Window titles

| English | Tiếng Việt |
| --- | --- |
| InstallerClean | InstallerClean |
| About | Giới thiệu |
| Registered files that should not be deleted | Tệp đã đăng ký, không nên xóa |
| Unneeded files that are safe to delete | Tệp không cần thiết, có thể xóa an toàn |
| Confirm move | Xác nhận chuyển |
| Confirm delete | Xác nhận xóa |
| Recycle Bin unavailable | Thùng rác không khả dụng |

## Section headings

| English | Tiếng Việt |
| --- | --- |
| PRODUCTS | SẢN PHẨM |
| PATCHES | BẢN VÁ |
| PRODUCT DETAILS | CHI TIẾT SẢN PHẨM |
| MOVE LOCATION | NƠI CHUYỂN ĐẾN |
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
| Check for _updates | Kiểm tra _cập nhật |
| _Close | Đón_g |
| _Delete | _Xóa |
| _Delete permanently | _Xóa vĩnh viễn |
| _Done | _Xong |
| Details | Chi tiết |
| _Buy me a cuppa | _Mời tôi một ly cà phê |
| Leave a _star on GitHub | Gắn _sao trên GitHub |
| MIT licence | Giấy phép MIT |
| _Move | _Chuyển |
| _Move instead | _Chuyển thay vào đó |
| Path to folder if you Move instead of Delete | Đường dẫn thư mục nếu bạn Chuyển thay vì Xóa |
| Open _release page | _Mở trang phát hành |
| _Re-scan | _Quét lại |
| _Scan again | _Quét lại |
| Send report | Gửi báo cáo |
| _Send | _Gửi |

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
| Checking registry for additional packages... | Đang kiểm tra registry để tìm các gói bổ sung... |
| Found {0} registered {1}. | Đã tìm thấy {0} {1} đã đăng ký. |
| Scan complete ({0}) | Quét xong ({0}) |
| Scanning local packages... | Đang quét các gói cục bộ... |
| Found {0} {1} you can safely delete. | Đã tìm thấy {0} {1} bạn có thể xóa an toàn. |
| Preparing destination folder... | Đang chuẩn bị thư mục đích... |
| Moving {0} {1}... | Đang chuyển {0} {1}... |
| Deleting {0} {1}... | Đang xóa {0} {1}... |
| Move cancelled. {0} of {1} {2} processed. | Đã hủy chuyển. Đã xử lý {0}/{1} {2}. |
| Delete cancelled. {0} of {1} {2} processed. | Đã hủy xóa. Đã xử lý {0}/{1} {2}. |
| Move failed ({0}). Details in {1}. | Chuyển thất bại ({0}). Chi tiết trong {1}. |
| Move failed ({0}). The crash log could not be written. | Chuyển thất bại ({0}). Không thể ghi nhật ký sự cố. |
| Delete failed ({0}). Details in {1}. | Xóa thất bại ({0}). Chi tiết trong {1}. |
| Delete failed ({0}). The crash log could not be written. | Xóa thất bại ({0}). Không thể ghi nhật ký sự cố. |
| Access denied. Run as administrator. | Truy cập bị từ chối. Hãy chạy với quyền quản trị viên. |
| Scan failed: installer database unavailable. | Quét thất bại: cơ sở dữ liệu trình cài đặt không khả dụng. |
| Scan cancelled. | Đã hủy quét. |
| Ready | Sẵn sàng |
| Scan failed ({0}). Details in {1}. | Quét thất bại ({0}). Chi tiết trong {1}. |
| Scan failed ({0}). The crash log could not be written. | Quét thất bại ({0}). Không thể ghi nhật ký sự cố. |

## Main screen text

| English | Tiếng Việt |
| --- | --- |
| The unneeded files below are safe to delete. | Các tệp không cần thiết bên dưới có thể xóa an toàn. |
| They sit in C:\Windows\Installer, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | Chúng nằm trong C:\Windows\Installer, còn sót lại khi một chương trình bị gỡ cài đặt ({0}), khi một bản vá mới hơn thay thế một bản ({1}) hoặc khi nhà phát hành thu hồi nó ({2}). InstallerClean chỉ liệt kê những tệp mà chính Windows báo là đã dùng xong. |
| Delete them to the Recycle Bin, or Move them elsewhere first if you'd rather keep a copy. | Xóa chúng vào Thùng rác, hoặc Chuyển chúng đi nơi khác trước nếu bạn muốn giữ một bản sao. |
| Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch the installer cache while it's changing. Once it's done, Re-scan and they come back. | Có thứ gì đó đang dùng Windows Installer ngay lúc này, thường là Windows Update hoặc một chương trình đang cài đặt ở chế độ nền. Chuyển và Xóa được tạm dừng trong khi việc đó chạy, nên InstallerClean sẽ không đụng tới bộ nhớ đệm trình cài đặt khi nó đang thay đổi. Khi việc đó xong, hãy Quét lại và chúng sẽ quay lại. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | Một giao dịch Windows Installer trước đó đang bị tạm dừng trên máy này. Hãy tiếp tục hoặc hoàn tác lần cài đặt đó (hoặc khởi động lại Windows) trước khi dọn bộ nhớ đệm. |
| Windows has a file rename queued for the next restart that affects the Installer cache. Restart Windows before cleaning. | Windows có một thao tác đổi tên tệp được xếp hàng cho lần khởi động lại tới, ảnh hưởng tới bộ nhớ đệm Installer. Hãy khởi động lại Windows trước khi dọn. |
| Select a file to view details. | Chọn một tệp để xem chi tiết. |
| Select a product to view details. | Chọn một sản phẩm để xem chi tiết. |
| No metadata available. | Không có siêu dữ liệu. |
| This installer file has been deleted. InstallerClean didn't do it, it never removes a file a program still needs; something else deleted this one before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. | Tệp cài đặt này đã bị xóa. InstallerClean không làm việc đó, nó không bao giờ xóa một tệp mà chương trình vẫn cần; thứ gì đó khác đã xóa tệp này trước khi bạn chạy InstallerClean.<br><br>Bây giờ nó không gây rắc rối gì, và sẽ không gây rắc rối cho tới ngày bạn cố sửa chữa, cập nhật hoặc gỡ cài đặt chương trình mà nó thuộc về. Khi đó bước này có thể thất bại, vì Windows tìm tệp này mà không thấy.<br><br>Để thử khắc phục, hãy tải trình cài đặt của chương trình đó từ nhà sản xuất và chạy nó đè lên bản hiện có của bạn (đừng gỡ cài đặt trước, gỡ cài đặt bản thân nó là một bước cần tới tệp này). Hãy dùng đúng phiên bản bạn đã cài nếu có thể, vì Windows có thể từ chối một phiên bản khác. Cách này thường khôi phục được tệp, và cài đặt của bạn thường không bị động đến, nhưng Microsoft không bảo đảm điều đó, phương án cuối cùng của họ là cài đặt lại chương trình, hoặc chính Windows. |
| The README [explains this folder], and how to recover a file, in Microsoft's own words. | README [giải thích thư mục này], và cách khôi phục một tệp, bằng chính lời của Microsoft. |
| (none) | (không có) |

## Reasons a file is unneeded

| English | Tiếng Việt |
| --- | --- |
| Orphaned | Mồ côi |
| Superseded | Bị thay thế |
| Obsoleted | Đã lỗi thời |

## Completion screen

| English | Tiếng Việt |
| --- | --- |
| All clean | Đã sạch |
| Nothing to clean up in C:\Windows\Installer | Không còn gì để dọn trong C:\Windows\Installer |
| Scanned {0} {1} in {2} | Đã quét {0} {1} trong {2} |
| Copy them back if anything stops working | Chép chúng trở lại nếu có gì ngừng hoạt động |
| Restore them from the Recycle Bin if anything stops working | Khôi phục chúng từ Thùng rác nếu có gì ngừng hoạt động |
| {0} freed | Đã giải phóng {0} |
| {0} moved | Đã chuyển {0} |
| {0} moved, some files could not be processed | Đã chuyển {0}, một số tệp không thể xử lý |
| {0} freed, some files could not be processed | Đã giải phóng {0}, một số tệp không thể xử lý |
| {0} {1} moved to {2} | Đã chuyển {0} {1} tới {2} |
| {0} {1} moved to {2}. {3} {4} | Đã chuyển {0} {1} tới {2}. {3} {4} |
| {0} {1} sent to the Recycle Bin | Đã gửi {0} {1} vào Thùng rác |
| {0} {1} sent to the Recycle Bin. {2} {3} | Đã gửi {0} {1} vào Thùng rác. {2} {3} |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. | Đã xóa vĩnh viễn {0} {1}, không qua Thùng rác. |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. | Đã xóa vĩnh viễn {0} {1}, không qua Thùng rác. |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. {2} {3} | Đã xóa vĩnh viễn {0} {1}, không qua Thùng rác. {2} {3} |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. {2} {3} | Đã xóa vĩnh viễn {0} {1}, không qua Thùng rác. {2} {3} |
| That's fine, it was safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Không sao, những tệp đó đều an toàn để loại bỏ. InstallerClean chỉ dọn những tệp Windows báo là đã dùng xong, không bao giờ là tệp mà chương trình vẫn cần. Trong trường hợp hiếm hoi một lần xóa từng khiến một chương trình không thể sửa chữa, cập nhật hoặc gỡ cài đặt, việc cài đặt lại chương trình đó từ nhà sản xuất thường khôi phục được tệp, dù Microsoft không bảo đảm điều đó. |
| That's fine, they were safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Không sao, những tệp đó đều an toàn để loại bỏ. InstallerClean chỉ dọn những tệp Windows báo là đã dùng xong, không bao giờ là tệp mà chương trình vẫn cần. Trong trường hợp hiếm hoi một lần xóa từng khiến một chương trình không thể sửa chữa, cập nhật hoặc gỡ cài đặt, việc cài đặt lại chương trình đó từ nhà sản xuất thường khôi phục được tệp, dù Microsoft không bảo đảm điều đó. |

## Recycle Bin unavailable

| English | Tiếng Việt |
| --- | --- |
| The Recycle Bin isn't available for this drive | Thùng rác không khả dụng cho ổ đĩa này |
| So this {1} ({2}) hasn't been deleted. You can move it somewhere safe, or delete it permanently. | Vậy nên {1} này ({2}) chưa bị xóa. Bạn có thể chuyển nó tới nơi an toàn, hoặc xóa vĩnh viễn. |
| So these {0} {1} ({2}) haven't been deleted. You can move them somewhere safe, or delete them permanently. | Vậy nên {0} {1} này ({2}) chưa bị xóa. Bạn có thể chuyển chúng tới nơi an toàn, hoặc xóa vĩnh viễn. |
| Deleting it is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Việc xóa là an toàn. InstallerClean chỉ dọn những tệp Windows báo là đã dùng xong, không bao giờ là tệp mà chương trình vẫn cần, và Thùng rác chỉ là một lớp bảo vệ thêm. Trong trường hợp hiếm hoi một lần xóa từng khiến một chương trình không thể sửa chữa, cập nhật hoặc gỡ cài đặt, việc cài đặt lại chương trình đó từ nhà sản xuất thường khôi phục được tệp, dù Microsoft không bảo đảm điều đó. |
| Deleting them is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Việc xóa là an toàn. InstallerClean chỉ dọn những tệp Windows báo là đã dùng xong, không bao giờ là tệp mà chương trình vẫn cần, và Thùng rác chỉ là một lớp bảo vệ thêm. Trong trường hợp hiếm hoi một lần xóa từng khiến một chương trình không thể sửa chữa, cập nhật hoặc gỡ cài đặt, việc cài đặt lại chương trình đó từ nhà sản xuất thường khôi phục được tệp, dù Microsoft không bảo đảm điều đó. |

## Summaries and counts

| English | Tiếng Việt |
| --- | --- |
| {0} file still needed | {0} tệp vẫn cần thiết |
| {0} files still needed | {0} tệp vẫn cần thiết |
| {0} unneeded file to clean up | {0} tệp không cần thiết để dọn |
| {0} unneeded files to clean up | {0} tệp không cần thiết để dọn |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | {0} tệp đã đăng ký bị thiếu (không phải do InstallerClean xóa). Hiện chưa có vấn đề, nhưng một lần sửa chữa, cập nhật hoặc gỡ cài đặt chương trình liên quan trong tương lai có thể thất bại. Mở Chi tiết để biết cần làm gì. |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | {0} tệp đã đăng ký bị thiếu (không phải do InstallerClean xóa). Hiện chưa có vấn đề, nhưng một lần sửa chữa, cập nhật hoặc gỡ cài đặt chương trình liên quan trong tương lai có thể thất bại. Mở Chi tiết để biết cần làm gì. |
| {0} stale MSI entry detected (file already gone from disk; InstallerClean doesn't unregister it). | Phát hiện {0} mục MSI cũ (tệp đã biến mất khỏi đĩa; InstallerClean không hủy đăng ký các mục này). |
| {0} stale MSI entries detected (files already gone from disk; InstallerClean doesn't unregister them). | Phát hiện {0} mục MSI cũ (tệp đã biến mất khỏi đĩa; InstallerClean không hủy đăng ký các mục này). |
| {0} of {1} {2} | {0}/{1} {2} |
| {0} orphaned, {1} superseded, {2} obsoleted ({3}) | {0} mồ côi, {1} bị thay thế, {2} đã lỗi thời ({3}) |
| {0} registered file that is still needed ({1}) | {0} tệp đã đăng ký vẫn cần thiết ({1}) |
| {0} registered files that are still needed ({1}) | {0} tệp đã đăng ký vẫn cần thiết ({1}) |

## Confirmation dialogs

| English | Tiếng Việt |
| --- | --- |
| Move {0} {1} ({2})? | Chuyển {0} {1} ({2})? |
| Files will be moved to {0}. | Các tệp sẽ được chuyển tới {0}. |
| Delete {0} {1} ({2})? | Xóa {0} {1} ({2})? |
| Files will be sent to the Recycle Bin. If you'd like backup copies, use Move instead. | Các tệp sẽ được gửi vào Thùng rác. Nếu bạn muốn có bản sao lưu, hãy dùng Chuyển thay vào đó. |

## Error messages

| English | Tiếng Việt |
| --- | --- |
| Administrator rights required | Cần quyền quản trị viên |
| InstallerClean requires administrator privileges.<br><br>Please right-click and choose 'Run as administrator'. | InstallerClean cần quyền quản trị viên.<br><br>Hãy nhấp chuột phải và chọn 'Chạy với tư cách quản trị viên'. |
| Installer database unavailable | Cơ sở dữ liệu trình cài đặt không khả dụng |
| Scan failed | Quét thất bại |
| The Windows Installer database appears to be empty or inaccessible. This is unusual even on a fresh Windows install and typically means the database is corrupt or a third-party tool has cleared it. Running 'sfc /scannow' from an elevated prompt usually repairs it. | Cơ sở dữ liệu Windows Installer có vẻ trống hoặc không truy cập được. Điều này bất thường ngay cả trên một bản Windows mới cài và thường có nghĩa là cơ sở dữ liệu bị hỏng hoặc một công cụ của bên thứ ba đã xóa nó. Chạy 'sfc /scannow' từ một dấu nhắc có quyền nâng cao thường sửa được. |
| Access denied enumerating installed products. Run as administrator. | Truy cập bị từ chối khi liệt kê các sản phẩm đã cài. Hãy chạy với quyền quản trị viên. |
| Windows Installer refused to list products after {0} consecutive failures (last error code {1}). Try restarting Windows, or run 'sfc /scannow' from an elevated prompt. | Windows Installer từ chối liệt kê sản phẩm sau {0} lần thất bại liên tiếp (mã lỗi cuối {1}). Hãy thử khởi động lại Windows, hoặc chạy 'sfc /scannow' từ một dấu nhắc có quyền nâng cao. |
| Invalid destination | Đích không hợp lệ |
| Could not write to destination | Không thể ghi vào đích |
| Move failed | Chuyển thất bại |
| Delete failed | Xóa thất bại |
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
| Access denied. | Truy cập bị từ chối. |
| The operation failed. Try again or restart Windows. | Thao tác thất bại. Hãy thử lại hoặc khởi động lại Windows. |
| Unknown error. | Lỗi không xác định. |
| Couldn't send this file to the Recycle Bin (error {0}). It may be locked, in use or blocked by Windows. Move it instead. | Không thể gửi tệp này vào Thùng rác (lỗi {0}). Nó có thể đang bị khóa, đang được dùng hoặc bị Windows chặn. Hãy Chuyển nó thay vào đó. |
| Windows blocked access to this file, even with administrator rights (error {0}). It is usually an ownership or permissions lock. Move it instead. | Windows đã chặn truy cập tệp này, ngay cả với quyền quản trị viên (lỗi {0}). Đây thường là khóa quyền sở hữu hoặc quyền truy cập. Hãy Chuyển nó thay vào đó. |
| This file is open or locked by another program (error {0}). Close that program, or whatever is scanning it, then try again, or Move it instead. | Tệp này đang được mở hoặc bị khóa bởi một chương trình khác (lỗi {0}). Hãy đóng chương trình đó, hoặc thứ gì đang quét nó, rồi thử lại, hoặc Chuyển nó thay vào đó. |
| The file was permanently deleted because it could not be sent to the Recycle Bin. | Tệp đã bị xóa vĩnh viễn vì không thể gửi vào Thùng rác. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Từ chối chuyển tệp vào thư mục Windows Installer (đích: {0}). |
| Destination must be a fully qualified path (relative paths resolve against the process current directory and are unsafe under elevation): {0} | Đích phải là một đường dẫn đầy đủ (đường dẫn tương đối được phân giải theo thư mục hiện tại của tiến trình và không an toàn khi chạy nâng quyền): {0} |
| Destination folder canonical path changed mid-batch: {0} | Đường dẫn chuẩn của thư mục đích đã thay đổi giữa chừng: {0} |
| Cannot write to {0}. | Không thể ghi vào {0}. |
| Could not find a unique filename for '{0}' after 10,000 attempts. | Không tìm được tên tệp duy nhất cho '{0}' sau 10.000 lần thử. |

## Update check

| English | Tiếng Việt |
| --- | --- |
| Check for updates | Kiểm tra cập nhật |
| Checking... | Đang kiểm tra... |
| Up to date. | Đã cập nhật. |
| Update available | Có bản cập nhật |
| You're running version {0}.<br>Version {1} is available. | Bạn đang dùng phiên bản {0}.<br>Phiên bản {1} đã có. |
| Couldn't reach GitHub. Check your internet connection and try again. | Không thể kết nối tới GitHub. Hãy kiểm tra kết nối internet và thử lại. |
| GitHub returned an error response. The releases API may be rate-limited; try again in a few minutes. | GitHub trả về phản hồi lỗi. API phát hành có thể đang bị giới hạn tần suất; hãy thử lại sau vài phút. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | Phản hồi của GitHub không chứa bản phát hành nào nhận ra được. Hãy thử lại sau, hoặc mở thẳng trang phát hành. |
| The check timed out. Your connection to GitHub may be slow; try again. | Quá thời gian kiểm tra. Kết nối của bạn tới GitHub có thể chậm; hãy thử lại. |
| The check failed for an unknown reason. Details are in crash.log if you need to report it. | Việc kiểm tra thất bại vì một lý do không xác định. Chi tiết nằm trong crash.log nếu bạn cần báo cáo. |

## Opening links in your browser

| English | Tiếng Việt |
| --- | --- |
| Couldn't open your browser | Không thể mở trình duyệt của bạn |
| The link couldn't be opened in your normal-user browser. The URL has been copied to your clipboard so you can open it manually:<br><br>{0} | Không thể mở liên kết trong trình duyệt người dùng thường của bạn. URL đã được chép vào bộ nhớ tạm để bạn mở thủ công:<br><br>{0} |
| The link couldn't be opened in your normal-user browser, and copying it to the clipboard also failed. The URL is:<br><br>{0} | Không thể mở liên kết trong trình duyệt người dùng thường của bạn, và việc chép nó vào bộ nhớ tạm cũng thất bại. URL là:<br><br>{0} |

## Sending the summary

| English | Tiếng Việt |
| --- | --- |
| Sending... | Đang gửi... |
| Thanks! Report sent. | Cảm ơn! Đã gửi báo cáo. |
| Sending failed. Try again later. | Gửi thất bại. Hãy thử lại sau. |
| No report to send. | Không có báo cáo để gửi. |
| Send this to No Faff? | Gửi cái này tới No Faff? |
| Nothing identifies you or your machine; it just lets me know InstallerClean's working and how much space people are freeing. It goes to nofaff.netlify.app/api/result-log. | Không có gì nhận dạng bạn hay máy của bạn; nó chỉ cho tôi biết InstallerClean có hoạt động không và mọi người đang giải phóng được bao nhiêu dung lượng. Nó được gửi tới nofaff.netlify.app/api/result-log. |

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
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Redact both classes of detail before attaching this file to a<br># public bug report.<br> | # crash.log ghi lại các ngoại lệ chưa được xử lý từ InstallerClean.<br># Khi chạy nâng quyền, thông báo ngoại lệ của framework có thể chứa<br># đường dẫn tệp từ phiên đang chạy (bao gồm hồ sơ của những người<br># dùng khác do truy vấn Windows Installer liệt kê). Thông báo lỗi<br># mạng từ lần kiểm tra cập nhật hoặc POST nhật ký kết quả có thể<br># chứa URL đích và địa chỉ IP / proxy đã phân giải. Hãy che cả hai<br># loại chi tiết này trước khi đính kèm tệp này vào một báo cáo lỗi<br># công khai.<br> |

## Tooltips (hover text)

| English | Tiếng Việt |
| --- | --- |
| If it helped, buy me a cup of tea. | Nếu thấy hữu ích, hãy mời tôi một ly cà phê. |
| It's thirsty work! | Việc này khát nước lắm! |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Đã yêu cầu hủy. InstallerClean đang chờ bước hiện tại tới điểm dừng. Việc này có thể mất vài giây khi I/O nặng hoặc khi đang gọi cơ sở dữ liệu MSI. |
| Close | Đóng |
| Leave a star on GitHub, report an Issue or post in Discussions. Any feedback welcome. | Gắn sao trên GitHub, báo một Issue hoặc đăng trong Discussions. Mọi phản hồi đều được hoan nghênh. |
| or report an Issue or post in Discussions. Any feedback welcome. | hoặc báo một Issue hay đăng trong Discussions. Mọi phản hồi đều được hoan nghênh. |
| Minimise | Thu nhỏ |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | Tùy bạn, nhưng rất được trân trọng. Gửi một bản tóm tắt ẩn danh chỉ để cho tôi biết nó có hoạt động không và mọi người đang giải phóng được bao nhiêu dung lượng. Màn hình tiếp theo cho bạn xem những gì sẽ được gửi trước khi bạn xác nhận. |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | Tùy bạn, nhưng rất được trân trọng. Gửi một bản tóm tắt ẩn danh chỉ để cho tôi biết nó có hoạt động không. Màn hình tiếp theo cho bạn xem những gì sẽ được gửi trước khi bạn xác nhận. |
| Move the unneeded files to the Move location. | Chuyển các tệp không cần thiết tới Nơi chuyển đến. |
| Move the unneeded files to the Move location. Choose one first. | Chuyển các tệp không cần thiết tới Nơi chuyển đến. Hãy chọn một nơi trước. |
| Send the unneeded files to the Recycle Bin. | Gửi các tệp không cần thiết vào Thùng rác. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Tên chủ thể từ chứng chỉ Authenticode được nhúng. Chưa xác minh chuỗi. |
| Change language. The program will restart. | Thay đổi ngôn ngữ. Chương trình sẽ khởi động lại. |

## Screen reader labels

| English | Tiếng Việt |
| --- | --- |
| Buy me a cup of tea | Mời tôi một ly cà phê |
| Buy me a cuppa (About window) | Mời tôi một ly cà phê (cửa sổ Giới thiệu) |
| Cancel operation | Hủy thao tác |
| Cancel scan | Hủy quét |
| Cancel startup scan | Hủy quét khi khởi động |
| Close | Đóng |
| Close window | Đóng cửa sổ |
| Close result and return to main window | Đóng kết quả và quay lại cửa sổ chính |
| Leave a star on GitHub | Gắn sao trên GitHub |
| Leave a star on GitHub (About window) | Gắn sao trên GitHub (cửa sổ Giới thiệu) |
| Minimise | Thu nhỏ |
| Move all unneeded installer files to the chosen destination folder | Chuyển tất cả các tệp cài đặt không cần thiết tới thư mục đích đã chọn |
| Send all unneeded installer files to the Recycle Bin | Gửi tất cả các tệp cài đặt không cần thiết vào Thùng rác |
| Delete sends the unneeded files to the Recycle Bin. Cancel closes without deleting. | Xóa sẽ gửi các tệp không cần thiết vào Thùng rác. Hủy sẽ đóng lại mà không xóa. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Chuyển sẽ đặt các tệp không cần thiết vào thư mục đích đã chọn. Hủy sẽ để chúng nguyên chỗ cũ. |
| Choose how to handle the unneeded files: move them somewhere safe, delete them permanently or cancel. | Chọn cách xử lý các tệp không cần thiết: chuyển chúng tới nơi an toàn, xóa vĩnh viễn hoặc hủy. |
| Move the unneeded files to a folder you choose | Chuyển các tệp không cần thiết tới một thư mục bạn chọn |
| Delete the unneeded files permanently because the Recycle Bin is unavailable for this drive | Xóa vĩnh viễn các tệp không cần thiết vì Thùng rác không khả dụng cho ổ đĩa này |
| Posts to nofaff.netlify.app. Counts and labels only. You will see the exact payload before sending. | Gửi tới nofaff.netlify.app. Chỉ gồm số liệu và nhãn. Bạn sẽ thấy nội dung chính xác trước khi gửi. |
| Say thanks | Gửi lời cảm ơn |
| Send posts the report shown to No Faff. Cancel sends nothing. | Gửi sẽ đăng báo cáo hiển thị tới No Faff. Hủy sẽ không gửi gì. |
| Check for updates | Kiểm tra cập nhật |
| Checks the GitHub releases API over HTTPS for a newer version. | Kiểm tra API phát hành của GitHub qua HTTPS để tìm phiên bản mới hơn. |
| Open the release page to download the newer version, or cancel to keep the current version. | Mở trang phát hành để tải phiên bản mới hơn, hoặc hủy để giữ phiên bản hiện tại. |
| MIT licence | Giấy phép MIT |
| Opens the licence file on github.com in your browser. | Mở tệp giấy phép trên github.com trong trình duyệt của bạn. |
| Move location | Nơi chuyển đến |
| Products | Sản phẩm |
| Patches | Bản vá |
| Product details | Chi tiết sản phẩm |
| Move destination folder | Thư mục đích để chuyển đến |
| Operation progress | Tiến trình thao tác |
| Scan C:\Windows\Installer again | Quét lại C:\Windows\Installer |
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
| Result log preview | Xem trước nhật ký kết quả |
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
| Cancelling... | Đang hủy... |
| Cancelled. | Đã hủy. |
| Error: {0}. Details written to {1}. | Lỗi: {0}. Chi tiết đã ghi vào {1}. |
| Error: {0}. The crash log could not be written. | Lỗi: {0}. Không thể ghi nhật ký sự cố. |
| Scanning C:\Windows\Installer... | Đang quét C:\Windows\Installer... |
| Found {0} {1} to clean up ({2}). | Đã tìm thấy {0} {1} để dọn ({2}). |
| Nothing to do. | Không có gì để làm. |
| Deleting {0} {1}... | Đang xóa {0} {1}... |
| Deleted {0} {1}. | Đã xóa {0} {1}. |
| Error: the Recycle Bin is unavailable for this volume, so nothing was deleted. Use /m to move the files instead, or re-enable the Recycle Bin and run again. | Lỗi: Thùng rác không khả dụng cho ổ đĩa này, nên không có gì bị xóa. Hãy dùng /m để chuyển các tệp thay vào đó, hoặc bật lại Thùng rác rồi chạy lại. |
| Error: no move destination specified. Use /m PATH or set a default in the GUI. | Lỗi: chưa chỉ định đích để chuyển. Hãy dùng /m ĐƯỜNG_DẪN hoặc đặt một mặc định trong GUI. |
| Error: destination cannot be inside the Windows Installer folder. | Lỗi: đích không thể nằm bên trong thư mục Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Lỗi: đích phải là một đường dẫn đầy đủ. Nhận được: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | Lỗi: đích {0} nằm dưới một thư mục hệ thống của Windows. Hãy chọn một đường dẫn ngoài %SystemRoot%, %ProgramFiles% và %ProgramData%. |
| Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are blocked while that runs. Try again once it finishes. | Lỗi: có thứ gì đó đang dùng Windows Installer ngay lúc này, thường là Windows Update hoặc một chương trình đang cài đặt ở chế độ nền. Chuyển và Xóa bị chặn trong khi việc đó chạy. Hãy thử lại khi nó xong. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | Lỗi: một giao dịch Windows Installer trước đó đang bị tạm dừng trên máy này. Hãy tiếp tục hoặc hoàn tác lần cài đặt đó (hoặc khởi động lại Windows) trước khi dọn bộ nhớ đệm. |
| Error: a queued post-reboot file operation targets the Installer cache ({0}). Restart Windows to complete that operation before cleaning. | Lỗi: một thao tác trên tệp được xếp hàng cho lần khởi động lại tới nhắm vào bộ nhớ đệm Installer ({0}). Hãy khởi động lại Windows để hoàn tất thao tác đó trước khi dọn. |
| Moving {0} {1} to {2}... | Đang chuyển {0} {1} tới {2}... |
| Moved {0} {1}. | Đã chuyển {0} {1}. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Một tiến trình InstallerClean khác đang giữ khóa một-thực-thể (GUI hoặc một lần chạy CLI khác). Mã thoát 75 (tạm thời); có thể thử lại sau. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Lưu ý: ghi vào Nhật ký sự kiện thất bại. Hãy kiểm tra quyền của nhật ký Ứng dụng hoặc Chính sách nhóm. |
| InstallerClean - clean up C:\Windows\Installer | InstallerClean - dọn dẹp C:\Windows\Installer |
| Usage: | Cách dùng: |
|   installerclean-cli --help   Show this help (also accepts /?, -h) |   installerclean-cli --help        Hiển thị trợ giúp này (cũng nhận /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version     In ra phiên bản (cũng nhận -v) |
|   installerclean-cli /s       Scan only - list removable files |   installerclean-cli /s            Chỉ quét - liệt kê các tệp không cần thiết |
|   installerclean-cli /d       Delete removable files (Recycle Bin) |   installerclean-cli /d            Xóa các tệp không cần thiết (Thùng rác) |
|   installerclean-cli /m       Move to saved default location |   installerclean-cli /m            Chuyển tới vị trí mặc định đã lưu |
|   installerclean-cli /m PATH  Move to specified path |   installerclean-cli /m ĐƯỜNG_DẪN  Chuyển tới đường dẫn được chỉ định |
| installerclean-cli is a real console process and blocks the prompt | installerclean-cli là một tiến trình console thật và chặn dấu nhắc |
| until it finishes; redirect or pipe its output as you would any | cho đến khi xong; hãy chuyển hướng hoặc nối ống đầu ra của nó như |
| other console exe. The GUI lives in InstallerClean.exe alongside it. | với mọi tệp console khác. GUI nằm trong InstallerClean.exe cùng chỗ. |
| Exit codes: | Mã thoát: |
|   0   success: every flagged file was processed |   0   thành công: mọi tệp được đánh dấu đều đã được xử lý |
|   1   failure: nothing processed (bad args, scan failed, all files failed) |   1   thất bại: không xử lý được gì (sai đối số, quét thất bại, mọi tệp đều thất bại) |
|   2   partial: some files processed, some failed |   2   một phần: một số tệp được xử lý, một số thất bại |
|   75  transient: a temporary condition blocked the run (see the message) |   75  tạm thời: một điều kiện tạm thời đã chặn lần chạy (xem thông báo) |
|   130 cancelled (Ctrl+C) |   130 đã hủy (Ctrl+C) |
