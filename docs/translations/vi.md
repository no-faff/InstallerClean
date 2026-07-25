# InstallerClean in Tiếng Việt (Vietnamese)

The text of InstallerClean's interface and command-line tool in English on the left, with the Vietnamese translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Vietnamese can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

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
| Check for _updates | _Kiểm tra cập nhật |
| _Close | Đón_g |
| _Delete | _Xóa |
| _Delete permanently | _Xóa vĩnh viễn |
| _Done | _Xong |
| Details | Chi tiết |
| _Buy me a cuppa | _Mời tôi một ly cà phê |
| Leave a _star on GitHub | Gắn _sao trên GitHub |
| Apache 2.0 licence | Giấy phép Apache 2.0 |
| _Move | _Chuyển |
| _Move instead | _Chuyển thay vào đó |
| Path to folder if you Move instead of Delete | Đường dẫn thư mục nếu bạn Chuyển thay vì Xóa |
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
| Checking registry for additional packages... | Đang kiểm tra registry để tìm các gói bổ sung... |
| Found {0} registered {1}. | Đã tìm thấy {0} {1} đã đăng ký. |
| Scan complete ({0}) | Quét xong ({0}) |
| Scanning local packages... | Đang quét các gói cục bộ... |
| Found {0} {1} you can safely delete. | Đã tìm thấy {0} {1} bạn có thể xóa an toàn. |
| Preparing destination folder... | Đang chuẩn bị thư mục đích... |
| Checking the Recycle Bin... | Đang kiểm tra Thùng rác... |
| Moving {0} {1}... | Đang chuyển {0} {1}... |
| Deleting {0} {1}... | Đang xóa {0} {1}... |
| Move cancelled. {0} of {1} {2} processed. | Đã hủy chuyển. Đã xử lý {0}/{1} {2}. |
| Delete cancelled. {0} of {1} {2} processed. | Đã hủy xóa. Đã xử lý {0}/{1} {2}. |
| Move failed ({0}). Details in {1}. | Chuyển thất bại ({0}). Chi tiết trong {1}. |
| Move failed ({0}). The crash log could not be written. | Chuyển thất bại ({0}). Không thể ghi nhật ký sự cố. |
| Delete failed ({0}). Details in {1}. | Xóa thất bại ({0}). Chi tiết trong {1}. |
| Delete failed ({0}). The crash log could not be written. | Xóa thất bại ({0}). Không thể ghi nhật ký sự cố. |
| Access denied. Windows refused the scan. | Truy cập bị từ chối. Windows đã từ chối lần quét. |
| Scan failed: couldn't read the Windows Installer records. | Quét thất bại: không thể đọc các bản ghi trình cài đặt của Windows. |
| Scan cancelled. | Đã hủy quét. |
| Ready | Sẵn sàng |
| Scan failed ({0}). Details in {1}. | Quét thất bại ({0}). Chi tiết trong {1}. |
| Scan failed ({0}). The crash log could not be written. | Quét thất bại ({0}). Không thể ghi nhật ký sự cố. |

## Main screen text

| English | Tiếng Việt |
| --- | --- |
| Any unneeded files below are safe to delete. | Mọi tệp không cần thiết bên dưới đều có thể xóa an toàn. |
| They sit in C:\Windows\Installer, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | Chúng nằm trong C:\Windows\Installer, còn sót lại khi một chương trình bị gỡ cài đặt ({0}), khi một bản vá mới hơn thay thế một bản ({1}) hoặc khi nhà phát hành thu hồi nó ({2}). InstallerClean chỉ liệt kê những tệp mà chính Windows báo là đã dùng xong. |
| Delete them to the Recycle Bin, or use Move instead to keep a backup. Putting the files back in C:\Windows\Installer returns you to exactly where you started. | Xóa chúng vào Thùng rác, hoặc dùng Chuyển thay vào đó để giữ một bản sao lưu. Đặt các tệp trở lại C:\Windows\Installer sẽ đưa mọi thứ về đúng như lúc ban đầu. |
| Nothing scanned yet. | Chưa quét gì cả. |
| Press Re-scan to look through C:\Windows\Installer for installer files that no program still needs. | Nhấn Quét lại để tìm trong C:\Windows\Installer những tệp cài đặt mà không chương trình nào còn cần. |
| These files can't be cleaned up right now. | Hiện chưa thể dọn những tệp này. |
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
| Orphaned | Bị bỏ lại |
| Superseded | Bị thay thế |
| Obsoleted | Đã lỗi thời |

## Completion screen

| English | Tiếng Việt |
| --- | --- |
| All clean | Đã sạch |
| Nothing to clean up in C:\Windows\Installer | Không còn gì để dọn trong C:\Windows\Installer |
| Scanned {0} {1} in {2} | Đã quét {0} {1} trong {2} |
| Copy them back to C:\Windows\Installer if anything ever breaks ([extremely unlikely]). | Sao chép chúng trở lại C:\Windows\Installer nếu chẳng may có gì trục trặc ([cực kỳ khó xảy ra]). |
| Until then, you can restore them if anything ever breaks ([extremely unlikely]). | Cho đến lúc đó, bạn có thể khôi phục chúng nếu chẳng may có gì trục trặc ([cực kỳ khó xảy ra]). |
| Empty it to actually reclaim the space. | Dọn sạch Thùng rác để thực sự lấy lại dung lượng. |
| {0} freed | Đã giải phóng {0} |
| {0} cleaned up | Đã dọn {0} |
| {0} moved | Đã chuyển {0} |
| Nothing was moved | Không có tệp nào được chuyển |
| Nothing was deleted | Không có tệp nào bị xóa |
| {0} of {1} could not be moved. | Không thể chuyển {0} trong số {1} tệp. |
| {0} of {1} could not be moved. | Không thể chuyển {0} trong số {1} tệp. |
| {0} of {1} could not be deleted. | Không thể xóa {0} trong số {1} tệp. |
| {0} of {1} could not be deleted. | Không thể xóa {0} trong số {1} tệp. |
| {0} {1} moved to: {2} | Đã chuyển {0} {1} tới: {2} |
| {0} {1} moved to: {2} | Đã chuyển {0} {1} tới: {2} |
| {0} {1} moved to the Recycle Bin | Đã di chuyển {0} {1} vào Thùng rác |
| {0} {1} moved to the Recycle Bin | Đã di chuyển {0} {1} vào Thùng rác |
| {0} {1} kept in place, because a program started needing them again after the scan. | Đã giữ nguyên {0} {1} vì sau lần quét, một chương trình lại cần đến chúng. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read when the check was repeated. | Đã giữ nguyên {0} {1} vì khi kiểm tra lại, không thể đọc đầy đủ các bản ghi trình cài đặt của Windows. |
| Moved {0} of {1} {2} before you cancelled. | Đã chuyển {0}/{1} {2} trước khi bạn hủy. |
| Moved {0} of {1} {2} to the Recycle Bin before you cancelled. | Đã di chuyển {0}/{1} {2} vào Thùng rác trước khi bạn hủy. |
| Permanently deleted {0} of {1} {2} before you cancelled. | Đã xóa vĩnh viễn {0}/{1} {2} trước khi bạn hủy. |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. | Đã xóa vĩnh viễn {0} {1}, không qua Thùng rác. |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. | Đã xóa vĩnh viễn {0} {1}, không qua Thùng rác. |
| That's fine, it was safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Không sao, những tệp đó đều an toàn để loại bỏ. InstallerClean chỉ dọn những tệp Windows báo là đã dùng xong, không bao giờ là tệp mà chương trình vẫn cần. Trong trường hợp hiếm hoi một lần xóa từng khiến một chương trình không thể sửa chữa, cập nhật hoặc gỡ cài đặt, việc cài đặt lại chương trình đó từ nhà sản xuất thường khôi phục được tệp, dù Microsoft không bảo đảm điều đó. |
| That's fine, they were safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Không sao, những tệp đó đều an toàn để loại bỏ. InstallerClean chỉ dọn những tệp Windows báo là đã dùng xong, không bao giờ là tệp mà chương trình vẫn cần. Trong trường hợp hiếm hoi một lần xóa từng khiến một chương trình không thể sửa chữa, cập nhật hoặc gỡ cài đặt, việc cài đặt lại chương trình đó từ nhà sản xuất thường khôi phục được tệp, dù Microsoft không bảo đảm điều đó. |
| Glad to help. There's a tip jar if you're feeling kind. | Rất vui vì đã giúp được. Nếu bạn có lòng, một ly cà phê cũng quý. |

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
| Files will be moved to the Recycle Bin. If you'd like backup copies, use the Move button instead. | Các tệp sẽ được di chuyển vào Thùng rác. Nếu bạn muốn có bản sao lưu, hãy dùng nút Chuyển thay vào đó. |
| This folder is on the same drive, so the move won't free any space by itself. You'll get the space back when you delete the files from it, or you can pick a folder on another drive instead. | Thư mục này nằm trên cùng ổ đĩa, nên bản thân việc chuyển sẽ không giải phóng dung lượng nào. Bạn sẽ lấy lại dung lượng khi xóa các tệp khỏi đó, hoặc bạn có thể chọn một thư mục trên ổ đĩa khác thay vào đó. |

## Error messages

| English | Tiếng Việt |
| --- | --- |
| Access denied | Truy cập bị từ chối |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows đã từ chối quyền truy cập của InstallerClean, nên nó đã dừng lại. Không có gì bị xóa.<br><br>InstallerClean vốn đã chạy với quyền quản trị viên, nên khởi động lại theo cách đó cũng không giúp được gì. Windows không nói gì thêm về thứ đã từ chối quyền truy cập, nên không có gì cụ thể để thử. |
| Couldn't read the Windows Installer records | Không thể đọc các bản ghi trình cài đặt của Windows |
| Scan failed | Quét thất bại |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in C:\Windows\Installer orphaned. InstallerClean stopped instead. Nothing has been removed. | Các bản ghi trình cài đặt của Windows trả về hoàn toàn trống: không một chương trình đã cài hay bản cập nhật nào nhận là chủ của một tệp cài đặt trong bộ nhớ đệm. Điều đó không xảy ra trên một máy hoạt động bình thường (ngay cả một bản Windows vừa cài cũng có vài tệp như vậy), nên hoặc các bản ghi đã hỏng, hoặc không đọc được, và một lần quét tin vào câu trả lời này sẽ nhầm lẫn coi mọi tệp trong C:\Windows\Installer là bị bỏ lại. Thay vào đó InstallerClean đã dừng. Không có gì bị xóa. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer không cho phép InstallerClean liệt kê những gì đã được cài. InstallerClean vốn đã chạy với quyền quản trị viên, nên chạy lại với quyền quản trị viên cũng không thay đổi được gì. Không có danh sách đó thì không có cách nào an toàn để biết tệp nào trong bộ nhớ đệm vẫn còn cần, nên InstallerClean đã dừng. Không có gì bị xóa. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer không thể đưa cho InstallerClean một danh sách chương trình đã cài đọc được: {0} mục liên tiếp trả về không đọc được (mã lỗi cuối {1}). Thay vì làm việc với một danh sách chỉ đọc được một phần, InstallerClean đã dừng. Không có gì bị xóa. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer chưa bao giờ báo hiệu kết thúc danh sách chương trình đã cài: InstallerClean đã bỏ cuộc sau {0} mục (mã lỗi cuối {1}). Không thể tin một danh sách không có điểm dừng, nên InstallerClean đã dừng. Không có gì bị xóa. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer chưa bao giờ báo hiệu kết thúc danh sách bản vá của một chương trình: InstallerClean đã bỏ cuộc sau {0} mục (mã lỗi cuối {1}). Không thể tin một danh sách không có điểm dừng, nên InstallerClean đã dừng. Không có gì bị xóa. |
| InstallerClean couldn't square this scan with the Windows Installer records: every file Windows still lists as needed is missing from C:\Windows\Installer, while the files actually in the folder match nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. | InstallerClean không thể khớp lần quét này với các bản ghi trình cài đặt của Windows: mọi tệp mà Windows vẫn liệt kê là cần thiết đều không có trong C:\Windows\Installer, trong khi các tệp thực sự nằm trong thư mục lại không khớp với bản ghi nào. Không có máy thật nào trông như vậy, nên điều này cho thấy có vấn đề khi đọc các bản ghi, chứ không phải là các tệp bạn có thể xóa an toàn. Chưa có gì được đưa ra để dọn dẹp và không có gì bị xóa. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean không đọc được đủ các bản ghi trình cài đặt của Windows để chắc chắn thứ gì vẫn còn cần: danh sách chương trình đã cài trả về thiếu, và việc đọc chính các bản ghi đó trực tiếp từ registry cũng gặp lỗi. Một tệp có thể trông như bị bỏ lại chỉ vì bản ghi nêu tên nó nằm trong số những bản ghi không đọc được, nên InstallerClean đã dừng. Không có gì bị xóa. |
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
| This file is open or locked by another program, so nothing can move it just now. It was left in place; try again later. | Tệp này đang được mở hoặc bị khóa bởi một chương trình khác, nên hiện không gì có thể chuyển nó. Tệp được giữ nguyên tại chỗ; hãy thử lại sau. |
| These files are open or locked by another program, so nothing can move them just now. They were left in place; try again later. | Các tệp này đang được mở hoặc bị khóa bởi một chương trình khác, nên hiện không gì có thể chuyển chúng. Các tệp được giữ nguyên tại chỗ; hãy thử lại sau. |
| Windows reported a file error; the file was left in place. | Windows báo một lỗi tệp; tệp được giữ nguyên tại chỗ. |
| Windows reported file errors; these files were left in place. | Windows báo lỗi tệp; các tệp này được giữ nguyên tại chỗ. |
| Something went wrong with this file; it was left in place. | Đã có trục trặc với tệp này; tệp được giữ nguyên tại chỗ. |
| Something went wrong with these files; they were left in place. | Đã có trục trặc với các tệp này; các tệp được giữ nguyên tại chỗ. |
| Couldn't move this file to the Recycle Bin (error {0}), and InstallerClean can't tell you why from that code. The file was left in place. Try the Move button instead, since it doesn't use the Recycle Bin. | Không thể chuyển tệp này vào Thùng rác (lỗi {0}), và từ mã đó InstallerClean không thể cho bạn biết lý do. Tệp được giữ nguyên tại chỗ. Hãy thử nút Chuyển, vì nút đó không dùng Thùng rác. |
| Windows refused access even with administrator rights (error {0}), and InstallerClean can't tell whether the problem is the file or the Recycle Bin. The file was left in place. The Move button will work if it's the Recycle Bin, but not if it's the file. | Windows đã từ chối truy cập ngay cả với quyền quản trị viên (lỗi {0}), và InstallerClean không thể biết vấn đề nằm ở tệp hay ở Thùng rác. Tệp được giữ nguyên tại chỗ. Nút Chuyển sẽ dùng được nếu vấn đề là Thùng rác, nhưng không dùng được nếu vấn đề là chính tệp đó. |
| This file is open or locked by another program (error {0}), so nothing can remove it just now. It was left in place; try again later. | Tệp này đang được mở hoặc bị khóa bởi một chương trình khác (lỗi {0}), nên hiện không gì có thể xóa nó. Tệp được giữ nguyên tại chỗ; hãy thử lại sau. |
| Windows deleted this file outright rather than moving it to the Recycle Bin. InstallerClean asked for the Recycle Bin, and Windows did this instead. The file is gone. | Windows đã xóa thẳng tệp này thay vì chuyển nó vào Thùng rác. InstallerClean đã yêu cầu Thùng rác, còn Windows thì làm khác. Tệp không còn nữa. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Từ chối chuyển tệp vào thư mục Windows Installer (đích: {0}). |
| The Move location needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | Nơi chuyển đến cần là đường dẫn đầy đủ tới một thư mục, bắt đầu bằng ký tự ổ đĩa hoặc một thư mục mạng dùng chung (ví dụ D:\Backup, hoặc \\server\backup). InstallerClean không dùng được đường dẫn này: {0} |
| The Move location changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | Nơi chuyển đến đã thay đổi trong khi các tệp đang được chuyển (thứ gì đó đã thay thế hoặc chuyển hướng thư mục), nên InstallerClean đã dừng lại thay vì ghi vào nhầm chỗ. Hãy kiểm tra {0}, rồi Quét lại và thử lại. |
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
| InstallerClean couldn't open your browser. The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | InstallerClean không thể mở trình duyệt của bạn. Liên kết đã được sao chép vào bảng nhớ tạm, nên bạn có thể tự dán nó vào:<br><br>{0} |
| InstallerClean couldn't open your browser, and couldn't copy the link to your clipboard either. The link is:<br><br>{0} | InstallerClean không thể mở trình duyệt của bạn, và cũng không thể sao chép liên kết vào bảng nhớ tạm. Liên kết là:<br><br>{0} |

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
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Redact both classes of detail before attaching this file to a<br># public bug report.<br> | # crash.log ghi lại các ngoại lệ chưa được xử lý từ InstallerClean.<br># Khi chạy nâng quyền, thông báo ngoại lệ của framework có thể chứa<br># đường dẫn tệp từ phiên đang chạy (bao gồm hồ sơ của những người<br># dùng khác do truy vấn Windows Installer liệt kê). Thông báo lỗi<br># mạng từ lần kiểm tra cập nhật hoặc POST nhật ký kết quả có thể<br># chứa URL đích và địa chỉ IP / proxy đã phân giải. Hãy che cả hai<br># loại chi tiết này trước khi đính kèm tệp này vào một báo cáo lỗi<br># công khai.<br> |

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
| Move the unneeded files to the Move location. | Chuyển các tệp không cần thiết tới Nơi chuyển đến. |
| Move the unneeded files somewhere safe. You'll choose the folder next. | Chuyển các tệp không cần thiết tới nơi an toàn. Bạn sẽ chọn thư mục ở bước tiếp theo. |
| Move the unneeded files to the Recycle Bin. | Di chuyển các tệp không cần thiết vào Thùng rác. |
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
| Delete moves the unneeded files to the Recycle Bin. Cancel closes without deleting. | Xóa sẽ di chuyển các tệp không cần thiết vào Thùng rác. Hủy sẽ đóng lại mà không xóa. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Chuyển sẽ đặt các tệp không cần thiết vào thư mục đích đã chọn. Hủy sẽ để chúng nguyên chỗ cũ. |
| Choose how to handle the unneeded files: move them somewhere safe, delete them permanently or cancel. | Chọn cách xử lý các tệp không cần thiết: chuyển chúng tới nơi an toàn, xóa vĩnh viễn hoặc hủy. |
| Move the unneeded files to a folder you choose | Chuyển các tệp không cần thiết tới một thư mục bạn chọn |
| Delete the unneeded files permanently because the Recycle Bin is unavailable for this drive | Xóa vĩnh viễn các tệp không cần thiết vì Thùng rác không khả dụng cho ổ đĩa này |
| Say thanks | Gửi lời cảm ơn |
| Send posts the report shown to No Faff. Cancel sends nothing. | Gửi sẽ đăng báo cáo hiển thị tới No Faff. Hủy sẽ không gửi gì. |
| Check for updates | Kiểm tra cập nhật |
| Checks github's releases page for a newer version. | Kiểm tra trang phát hành của github xem có phiên bản mới hơn không. |
| Opens the readme on github in your browser. | Mở readme trên github trong trình duyệt của bạn. |
| Opens the issue tracker on github.com in your browser. | Mở trình theo dõi vấn đề (Issues) trên github.com trong trình duyệt của bạn. |
| If ticked, InstallerClean checks github for a newer version when you run it. | Nếu được đánh dấu, InstallerClean sẽ kiểm tra github xem có phiên bản mới hơn không khi bạn chạy nó. |
| Open the release page to download the newer version, or cancel to keep the current version. | Mở trang phát hành để tải phiên bản mới hơn, hoặc hủy để giữ phiên bản hiện tại. |
| Opens the licence file on github.com in your browser. | Mở tệp giấy phép trên github.com trong trình duyệt của bạn. |
| Move location | Nơi chuyển đến |
| Products | Sản phẩm |
| Patches | Bản vá |
| Product details | Chi tiết sản phẩm |
| Move location | Nơi chuyển đến |
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
| Scanning C:\Windows\Installer... | Đang quét C:\Windows\Installer... |
| Found {0} {1} to clean up ({2}). | Đã tìm thấy {0} {1} để dọn ({2}). |
| Nothing to do. | Không có gì để làm. |
| Deleting {0} {1}... | Đang xóa {0} {1}... |
| Deleted {0} {1}. | Đã xóa {0} {1}. |
| Error: the Recycle Bin is unavailable for this volume, so nothing was deleted. Use /m to move the files instead, or re-enable the Recycle Bin and run again. | Lỗi: Thùng rác không khả dụng cho ổ đĩa này, nên không có gì bị xóa. Hãy dùng /m để chuyển các tệp thay vào đó, hoặc bật lại Thùng rác rồi chạy lại. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Lỗi: chưa chỉ định đích để chuyển. Dùng /m ĐƯỜNG_DẪN. (Mặc định đặt trong GUI là theo từng người dùng và không áp dụng cho các lần chạy theo lịch hoặc bằng tài khoản dịch vụ.) |
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
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help        Hiển thị trợ giúp này (cũng nhận /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version     In ra phiên bản (cũng nhận -v) |
|   installerclean-cli /s         Scan only - list removable files |   installerclean-cli /s            Chỉ quét - liệt kê các tệp không cần thiết |
|   installerclean-cli /d         Delete removable files (Recycle Bin) |   installerclean-cli /d            Xóa các tệp không cần thiết (Thùng rác) |
|   installerclean-cli /m         Move to saved default location |   installerclean-cli /m            Chuyển tới vị trí mặc định đã lưu |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m ĐƯỜNG_DẪN  Chuyển tới đường dẫn được chỉ định |
| installerclean-cli is a real console process and blocks the prompt | installerclean-cli là một tiến trình console thật và chặn dấu nhắc |
| until it finishes; redirect or pipe its output as you would any | cho đến khi xong; hãy chuyển hướng hoặc nối ống đầu ra của nó như |
| other console exe. The GUI lives in InstallerClean.exe alongside it. | với mọi tệp console khác. GUI nằm trong InstallerClean.exe cùng chỗ. |
| The saved default is per-user; scheduled or SYSTEM runs need /m PATH. | Mặc định đã lưu là theo từng người dùng; các lần chạy theo lịch hoặc bằng tài khoản SYSTEM cần /m ĐƯỜNG_DẪN. |
| Exit codes: | Mã thoát: |
|   0   success: every flagged file was processed |   0   thành công: mọi tệp được đánh dấu đều đã được xử lý |
|   1   failure: nothing processed (bad args, scan failed, all files failed) |   1   thất bại: không xử lý được gì (sai đối số, quét thất bại, mọi tệp đều thất bại) |
|   2   partial: some files processed, some failed |   2   một phần: một số tệp được xử lý, một số thất bại |
|   75  transient: a temporary condition blocked the run (see the message) |   75  tạm thời: một điều kiện tạm thời đã chặn lần chạy (xem thông báo) |
|   130 cancelled (Ctrl+C) |   130 đã hủy (Ctrl+C) |
