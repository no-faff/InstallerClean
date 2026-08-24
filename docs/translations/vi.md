# InstallerClean in Tiếng Việt (Vietnamese)

The text of InstallerClean's interface and command-line tool in English on the left, with the Vietnamese translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Vietnamese can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.vi.resx`](../../src/InstallerClean.Core/Resources/Strings.vi.resx), so do not edit it by hand. The Vietnamese translation itself lives in [`gen-strings-vi.mjs`](../../scripts/translations/gen-strings-vi.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | Tiếng Việt |
| --- | --- |
| InstallerClean | InstallerClean |
| About | Giới thiệu |
| Files left alone | Tệp được để nguyên |
| Unneeded files that are safe to delete | Tệp không cần thiết, có thể xóa an toàn |

## Section headings

| English | Tiếng Việt |
| --- | --- |
| PATCHES | BẢN VÁ |
| PRODUCT DETAILS | CHI TIẾT SẢN PHẨM |
| BACKUP FOLDER | THƯ MỤC ĐÍCH |
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
| Path to folder if you move rather than delete. | Đường dẫn thư mục nếu bạn chuyển thay vì xóa. |
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
| Moving unneeded files... | Đang chuyển các tệp không cần thiết... |
| Deleting unneeded files... | Đang xóa các tệp không cần thiết... |
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
| Any unneeded files below are [safe to delete]. | Mọi tệp không cần thiết bên dưới đều [có thể xóa an toàn]. |
| They sit in {InstallerFolder}. InstallerClean asks Windows about every installed program: a file is listed when no program claims it ({0}), or when a newer patch has replaced it and no program could roll back to it ({1}). | Chúng nằm trong {InstallerFolder}. InstallerClean hỏi Windows về từng chương trình đã cài: một tệp được liệt kê khi không chương trình nào nhận nó ({0}), hoặc khi một bản vá mới hơn đã thay thế nó và không chương trình nào có thể quay lại dùng nó ({1}). |
| Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update, repair and uninstall as normal. Putting them back into {InstallerFolder} restores everything. Or delete them permanently now. | Hãy chuyển chúng vào một thư mục đích do bạn chọn, rồi xóa thư mục đó khi bạn thấy các chương trình của mình vẫn cập nhật, sửa chữa và gỡ cài đặt như thường. Đặt chúng trở lại {InstallerFolder} sẽ khôi phục mọi thứ. Hoặc xóa vĩnh viễn ngay bây giờ. |
| Nothing scanned yet. | Chưa quét gì cả. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Nhấn Quét lại để tìm trong {InstallerFolder} những tệp cài đặt mà không chương trình nào còn cần. |
| These files can't be cleaned up right now. | Hiện chưa thể dọn những tệp này. |
| Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Có thứ gì đó đang dùng Windows Installer ngay lúc này, chẳng hạn một bản cập nhật Windows hoặc một chương trình đang cài trong nền. Chuyển và Xóa tạm dừng trong lúc đó, để InstallerClean không đụng vào {InstallerFolder} khi thư mục đang thay đổi. Xong rồi thì quét lại, hai nút sẽ trở lại. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Máy này có một giao dịch Windows Installer trước đó đang bị treo. Hãy tiếp tục hoặc hoàn tác lần cài đặt ấy (hoặc khởi động lại Windows) trước khi dọn {InstallerFolder}. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows đã xếp hàng một thao tác đổi tên tệp cho lần khởi động tới, có ảnh hưởng tới {InstallerFolder}. Hãy khởi động lại Windows trước khi dọn. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer đang có việc dở dang, nên Chuyển và Xóa tạm dừng. InstallerClean sẽ không đụng vào {InstallerFolder} khi thư mục đang thay đổi. Xong rồi thì quét lại, hai nút sẽ trở lại. |
| Select a file to view details. | Chọn một tệp để xem chi tiết. |
| Select a product to view details. | Chọn một sản phẩm để xem chi tiết. |
| No metadata available. | Không có siêu dữ liệu. |
| This installer file is missing. It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it, and its own last resort is reinstalling the program. | Tệp cài đặt này bị thiếu. Hiện giờ nó không gây rắc rối gì, và sẽ không gây cho tới ngày bạn thử sửa chữa, cập nhật hoặc gỡ cài đặt chương trình mà nó thuộc về. Bước đó khi ấy có thể thất bại, vì Windows tìm tệp này mà không thấy.<br><br>Để thử khắc phục, hãy tải bộ cài của chương trình đó từ nhà sản xuất và chạy đè lên bản bạn đang có (đừng gỡ cài đặt trước: việc gỡ cài đặt tự nó cũng là một bước cần tệp này). Nếu lấy được, hãy dùng đúng phiên bản bạn đã cài, vì Windows có thể từ chối một phiên bản khác. Cách này sẽ khôi phục được tệp và không đụng tới cài đặt của bạn, nhưng Microsoft không đảm bảo điều đó, và cách cuối cùng của chính họ là cài lại chương trình. |
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
| Nothing removed | Không có gì bị bỏ đi |
| Nothing to clean up in {InstallerFolder} | Không còn gì để dọn trong {InstallerFolder} |
| Scanned {0} {1} in {2} | Đã quét {0} {1} trong {2} |
| Nothing offered on this PC | Không có gì được đề xuất trên máy này |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({1}) it might otherwise have offered. | InstallerClean không thể chắc chắn những tệp nào trong bộ nhớ đệm thuộc về các chương trình đã cài ở đây, nên đã giữ lại tệp duy nhất ({1}) mà lẽ ra nó có thể đề xuất. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back all {0} files ({1}) it might otherwise have offered. | InstallerClean không thể chắc chắn những tệp nào trong bộ nhớ đệm thuộc về các chương trình đã cài ở đây, nên đã giữ lại toàn bộ {0} tệp ({1}) mà lẽ ra nó có thể đề xuất. |
| The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | Tệp trong thư mục đó [có thể bỏ đi an toàn], nên bạn cứ xóa thư mục bất cứ lúc nào. Cho tới lúc ấy, bạn có thể đặt nó trở lại {InstallerFolder} nếu hóa ra có chương trình nào cần đến (cực kỳ khó xảy ra). |
| The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | Các tệp trong thư mục đó [có thể bỏ đi an toàn], nên bạn cứ xóa thư mục bất cứ lúc nào. Cho tới lúc ấy, bạn có thể đặt chúng trở lại {InstallerFolder} nếu hóa ra có chương trình nào cần đến một tệp trong số đó (cực kỳ khó xảy ra). |
| The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | Tệp trong thư mục đó [có thể bỏ đi an toàn], nên khi nào bạn thực sự muốn lấy lại dung lượng thì cứ xóa thư mục hoặc chuyển nó sang ổ đĩa khác. Cho tới lúc ấy, bạn có thể đặt nó trở lại {InstallerFolder} nếu hóa ra có chương trình nào cần đến (cực kỳ khó xảy ra). |
| The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | Các tệp trong thư mục đó [có thể bỏ đi an toàn], nên khi nào bạn thực sự muốn lấy lại dung lượng thì cứ xóa thư mục hoặc chuyển nó sang ổ đĩa khác. Cho tới lúc ấy, bạn có thể đặt chúng trở lại {InstallerFolder} nếu hóa ra có chương trình nào cần đến một tệp trong số đó (cực kỳ khó xảy ra). |
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
| {0} {1} kept in place, because the records now claim what the scan flagged. | Đã giữ nguyên {0} {1}, vì các bản ghi giờ đây nhận phần mà lần quét đã đánh dấu. |
| {0} {1} kept in place, because the Windows Installer records had changed by the final check. | Đã giữ nguyên {0} {1}, vì tới lần kiểm tra cuối, các bản ghi Windows Installer đã thay đổi. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read in the final check. | Đã giữ nguyên {0} {1}, vì ở lần kiểm tra cuối, không đọc được đầy đủ các bản ghi Windows Installer. |
| {0} {1} kept in place, because by the final check InstallerClean could not be certain which cached files belong to the programs installed here. | Đã giữ nguyên {0} {1}, vì cho tới lần kiểm tra cuối, InstallerClean không thể chắc chắn những tệp nào trong bộ nhớ đệm thuộc về các chương trình đã cài ở đây. |
| {0} {1} kept in place, because Windows has a record of the program named inside. | Đã giữ nguyên {0} {1}, vì Windows có bản ghi về chương trình được nêu tên bên trong. |
| {0} {1} kept in place, because InstallerClean couldn't find a program named inside. | Đã giữ nguyên {0} {1}, vì InstallerClean không tìm thấy tên chương trình nào bên trong. |
| Moved {0} of {1} {2} before you cancelled. | Đã chuyển {0}/{1} {2} trước khi bạn hủy. |
| Permanently deleted {0} of {1} {2} before you cancelled. | Đã xóa vĩnh viễn {0}/{1} {2} trước khi bạn hủy. |
| {0} {1} permanently deleted | Đã xóa vĩnh viễn {0} {1} |
| {0} {1} permanently deleted | Đã xóa vĩnh viễn {0} {1} |
| Glad to help. There's a tip jar if you're feeling kind. | Rất vui vì đã giúp được. Nếu bạn có lòng, một ly cà phê cũng quý. |

## Summaries and counts

| English | Tiếng Việt |
| --- | --- |
| {0} file left alone | {0} tệp được để nguyên |
| {0} files left alone | {0} tệp được để nguyên |
| {0} unneeded file to clean up | {0} tệp không cần thiết để dọn |
| {0} unneeded files to clean up | {0} tệp không cần thiết để dọn |
| Windows has a record for {0} file that isn't in {InstallerFolder}: {1}. It causes no trouble day to day, but a repair, update or uninstall can fail on it. Open Details for what to do. | Windows có bản ghi cho {0} tệp không nằm trong {InstallerFolder}: {1}. Hằng ngày điều này không gây rắc rối, nhưng một lần sửa chữa, cập nhật hoặc gỡ cài đặt có thể thất bại vì nó. Hãy mở Chi tiết để biết phải làm gì. |
| Windows has records for {0} files that aren't in {InstallerFolder}: {1}. They cause no trouble day to day, but a repair, update or uninstall can fail on them. Open Details for what to do. | Windows có bản ghi cho {0} tệp không nằm trong {InstallerFolder}: {1}. Hằng ngày điều này không gây rắc rối, nhưng một lần sửa chữa, cập nhật hoặc gỡ cài đặt có thể thất bại vì chúng. Hãy mở Chi tiết để biết phải làm gì. |
| {0} other program | {0} chương trình khác |
| {0} other programs | {0} chương trình khác |
| {0} file with no program named in the records | {0} tệp không có chương trình nào được nêu tên trong bản ghi |
| {0} files with no program named in the records | {0} tệp không có chương trình nào được nêu tên trong bản ghi |
| On this PC InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file rather than listing it. | Trên máy này, InstallerClean không thể chắc chắn những tệp nào trong bộ nhớ đệm thuộc về các chương trình đã cài ở đây, nên đã giữ lại tệp duy nhất thay vì liệt kê nó. |
| On this PC InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} rather than listing them. | Trên máy này, InstallerClean không thể chắc chắn những tệp nào trong bộ nhớ đệm thuộc về các chương trình đã cài ở đây, nên đã giữ lại {0} {1} thay vì liệt kê chúng. |
| InstallerClean couldn't match up everything in the Windows records, so it didn't read all of them. The unneeded files above are unaffected, but what it says about files missing from {InstallerFolder} may be short of the full picture. Re-scan to try again. | InstallerClean không khớp được hết mọi thứ trong các bản ghi Windows, nên đã không đọc hết chúng. Các tệp không cần thiết ở trên không bị ảnh hưởng, nhưng những gì nói về các tệp thiếu khỏi {InstallerFolder} có thể chưa đầy đủ. Hãy quét lại để thử lần nữa. |
| {0} of {1} {2} | {0}/{1} {2} |
| {0} unneeded {1} ({2}) | {0} {1} không cần thiết ({2}) |
| {0} file left alone ({1}) | {0} tệp được để nguyên ({1}) |
| {0} files left alone ({1}) | {0} tệp được để nguyên ({1}) |

## Confirmation dialogs

| English | Tiếng Việt |
| --- | --- |
| Move {0} {1} ({2})? | Chuyển {0} {1} ({2})? |
| Move to: | Chuyển tới: |
| Delete {0} {1} ({2})? | Xóa {0} {1} ({2})? |
| This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. | Tệp này sẽ bị xóa vĩnh viễn. Nó [có thể xóa an toàn], nhưng nếu bạn muốn có bản sao lưu thì hãy dùng nút Chuyển. |
| Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. | Các tệp này sẽ bị xóa vĩnh viễn. Chúng [có thể xóa an toàn], nhưng nếu bạn muốn có bản sao lưu thì hãy dùng nút Chuyển. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | Thư mục đó nằm trên cùng một ổ đĩa, nên dung lượng chưa trở lại cho tới khi bạn xóa nó. Hãy chọn một thư mục trên ổ đĩa khác nếu bạn muốn có dung lượng ngay. |

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
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean không khớp được các bản ghi Windows Installer với nội dung trong {InstallerFolder}. Gần như không có thứ gì các bản ghi trỏ tới thực sự nằm ở đó, và gần như không có thứ gì ở đó được bản ghi nào nêu tên, nên không tệp nào có thể được chứng tỏ là không cần thiết. Không có gì được đề xuất và không có gì bị bỏ đi. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean không khớp được các bản ghi Windows Installer với nội dung trong {InstallerFolder}. Thư mục có tệp bên trong, nhưng không một bản ghi nào trỏ tới bất cứ thứ gì trong đó, nên không tệp nào có thể được chứng tỏ là không cần thiết. Không có gì được đề xuất và không có gì bị bỏ đi. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean không đọc được đủ các bản ghi Windows Installer để chắc chắn thứ gì vẫn còn cần: danh sách chương trình đã cài trả về thiếu, và việc đọc chính các bản ghi đó trực tiếp từ sổ đăng ký cũng gặp lỗi. Một tệp có thể trông như bị bỏ lại chỉ vì bản ghi nêu tên nó nằm trong số những bản ghi không đọc được, nên InstallerClean đã dừng. Không có gì bị xóa. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean không khiến được Windows phân giải đường dẫn thật của {InstallerFolder}, nên không tệp nào có thể được chứng tỏ là nằm bên trong và không tệp nào được đề xuất để dọn. Lần quét này không tìm thấy gì vì phép kiểm tra ấy thất bại, chứ không phải vì thư mục đã sạch. Không có gì bị bỏ đi. |
| Nothing was deleted | Không có tệp nào bị xóa |
| Nothing was moved | Không có tệp nào được chuyển |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | InstallerClean không lấy được khóa mà Windows Installer dùng để ngăn hai chương trình cùng lúc thay đổi phần mềm đã cài, nên không thể loại trừ khả năng một tệp trở nên cần thiết giữa chừng, và không có gì bị xóa. Hãy thử lại, và khởi động lại Windows nếu việc này cứ tiếp diễn. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. Try again, and restart Windows if it keeps happening. | InstallerClean không lấy được khóa mà Windows Installer dùng để ngăn hai chương trình cùng lúc thay đổi phần mềm đã cài, nên không thể loại trừ khả năng một tệp trở nên cần thiết giữa chừng, và không có gì được chuyển. Hãy thử lại, và khởi động lại Windows nếu việc này cứ tiếp diễn. |
| Invalid destination | Đích không hợp lệ |
| Could not write to destination | Không thể ghi vào đích |
| Move failed | Chuyển thất bại |
| Delete failed | Xóa thất bại |
| Setting not saved | Không lưu được cài đặt |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | Không thể lưu thay đổi. Lần chạy tiếp theo, InstallerClean sẽ quay lại cài đặt trước đó. |
| The destination cannot be inside the Windows Installer folder. | Đích không thể nằm bên trong thư mục Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Đích {0} phân giải vào bên trong một thư mục hệ thống của Windows. Hãy chọn một đường dẫn ngoài %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% và %ProgramData%. |
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
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | Tệp này đang được một chương trình khác mở hoặc khóa, nên lúc này không gì bỏ đi được. Nó đã được để nguyên; hãy thử lại sau. |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | Các tệp này đang được một chương trình khác mở hoặc khóa, nên lúc này không gì bỏ đi được. Chúng đã được để nguyên; hãy thử lại sau. |
| Windows reported a file error; the file was left in place. | Windows báo một lỗi tệp; tệp được giữ nguyên tại chỗ. |
| Windows reported file errors; these files were left in place. | Windows báo lỗi tệp; các tệp này được giữ nguyên tại chỗ. |
| Something went wrong with this file; it was left in place. | Đã có trục trặc với tệp này; tệp được giữ nguyên tại chỗ. |
| Something went wrong with these files; they were left in place. | Đã có trục trặc với các tệp này; các tệp được giữ nguyên tại chỗ. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Từ chối chuyển tệp vào thư mục Windows Installer (đích: {0}). |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | Thư mục đích phải là một đường dẫn đầy đủ tới một thư mục, bắt đầu bằng ký tự ổ đĩa hoặc một chia sẻ mạng (ví dụ D:\Backup, hoặc \\server\backup). InstallerClean không dùng được cái này: {0} |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | InstallerClean không còn xác nhận được thư mục đích, nên đã dừng lại thay vì ghi nhầm chỗ. Hãy kiểm tra {0}, rồi Quét lại và thử lần nữa. |
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
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log ghi lại các ngoại lệ chưa xử lý của InstallerClean.<br># Khi chạy với quyền nâng cao, thông báo ngoại lệ của framework có thể<br># chứa đường dẫn tệp trong phiên đang chạy (kể cả hồ sơ của người dùng<br># khác do các truy vấn Windows Installer liệt kê). Thông báo lỗi mạng<br># từ việc kiểm tra cập nhật hoặc gửi nhật ký kết quả có thể chứa URL<br># đích và địa chỉ IP hoặc proxy đã phân giải. Các mục về bản ghi<br># Windows Installer không đọc được có thể chứa SID tài khoản Windows<br># (S-1-5-21-...) và mã sản phẩm của phần mềm đã cài.<br># Hãy xóa cả ba loại thông tin này trước khi đính kèm tệp này vào một<br># báo cáo lỗi công khai.<br> |

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
| Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. | Chuyển các tệp không cần thiết vào thư mục đích. Hãy xóa thư mục đó khi bạn đã yên tâm rằng không gì cần đến chúng. |
| Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. | Chuyển các tệp không cần thiết vào một thư mục đích. Bạn sẽ chọn thư mục ngay sau đây. Hãy xóa thư mục đó khi bạn đã yên tâm rằng không gì cần đến chúng. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. | Chuyển các tệp không cần thiết vào thư mục đích. Thư mục đó nằm trên cùng một ổ đĩa, nên bạn chỉ lấy lại dung lượng sau khi xóa nó hoặc chuyển nó sang ổ đĩa khác. Bạn có thể làm vậy khi đã yên tâm rằng không gì cần đến chúng. |
| Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. | Xóa vĩnh viễn các tệp không cần thiết. Chúng có thể bỏ đi an toàn, và bạn lấy lại dung lượng ngay lập tức. |
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
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Xóa vĩnh viễn sẽ bỏ đi các tệp không cần thiết. Hủy sẽ đóng lại mà không xóa gì. |
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
| Backup folder | Thư mục đích |
| Patches | Bản vá |
| Product details | Chi tiết sản phẩm |
| Backup folder | Thư mục đích |
| Operation progress | Tiến trình thao tác |
| Scan {InstallerFolder} again | Quét lại {InstallerFolder} |
| Scanning progress | Tiến trình quét |
| Startup scan progress | Tiến trình quét khi khởi động |
| Details, unneeded files | Chi tiết, tệp không cần thiết |
| Available for cleanup. | Có thể dọn dẹp. |
| Details, files left alone | Chi tiết, tệp được để nguyên |
| Read-only inventory. | Danh sách chỉ đọc. |
| Sorted by {0}, ascending | Đã sắp xếp theo {0}, tăng dần |
| Sorted by {0}, descending | Đã sắp xếp theo {0}, giảm dần |
| Scan results | Kết quả quét |
| Result details | Chi tiết kết quả |
| File details | Chi tiết tệp |
| Product details | Chi tiết sản phẩm |
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
| ,  | ,  |
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
| Error: unknown argument '{0}' | Lỗi: đối số không rõ '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Lỗi: có thêm đối số không mong đợi '{0}'. Nếu thư mục đích của bạn có dấu cách, hãy đặt cả đường dẫn trong dấu ngoặc kép: /m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | Lỗi: đối số thừa không mong đợi '{0}'. /s và /d không nhận thêm đối số nào, và mỗi lần chạy chỉ dùng được một cờ. |
| Cancelling... | Đang hủy... |
| Cancelled. | Đã hủy. |
| Error: unexpected failure ({0}). Details written to {1}. | Lỗi: sự cố không mong đợi ({0}). Chi tiết đã ghi vào {1}. |
| Error: unexpected failure ({0}). The crash log could not be written. | Lỗi: sự cố không mong đợi ({0}). Không ghi được nhật ký sự cố. |
| Scanning {InstallerFolder}... | Đang quét {InstallerFolder}... |
| Found {0} unneeded {1} to clean up ({2}). | Đã tìm thấy {0} {1} không cần thiết để dọn ({2}). |
| Found no unneeded files. | Không tìm thấy tệp không cần thiết nào. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it held back the one file ({2}) it might otherwise have offered. | InstallerClean không thể chắc chắn những tệp nào trong bộ nhớ đệm thuộc về các chương trình đã cài ở đây, nên đã giữ lại tệp duy nhất ({2}) mà lẽ ra nó có thể đề xuất. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it held back all {0} {1} ({2}) it might otherwise have offered. | InstallerClean không thể chắc chắn những tệp nào trong bộ nhớ đệm thuộc về các chương trình đã cài ở đây, nên đã giữ lại toàn bộ {0} {1} ({2}) mà lẽ ra nó có thể đề xuất. |
| Windows has a record for {0} file that is not in {InstallerFolder}: {1}. It causes no trouble day to day, but a repair, update or uninstall can fail on it. Running that program's installer again, preferably the same version, usually restores the file. | Windows có bản ghi cho {0} tệp không nằm trong {InstallerFolder}: {1}. Hằng ngày điều này không gây rắc rối, nhưng một lần sửa chữa, cập nhật hoặc gỡ cài đặt có thể thất bại vì nó. Chạy lại bộ cài của chương trình đó, tốt nhất là đúng phiên bản, thường sẽ khôi phục tệp. |
| Windows has records for {0} files that are not in {InstallerFolder}: {1}. They cause no trouble day to day, but a repair, update or uninstall can fail on them. Running each program's installer again, preferably the same version, usually restores the files. | Windows có bản ghi cho {0} tệp không nằm trong {InstallerFolder}: {1}. Hằng ngày điều này không gây rắc rối, nhưng một lần sửa chữa, cập nhật hoặc gỡ cài đặt có thể thất bại vì chúng. Chạy lại bộ cài của từng chương trình, tốt nhất là đúng phiên bản, thường sẽ khôi phục các tệp. |
| InstallerClean couldn't match up everything in the Windows records, so it didn't read all of them. What it found is unaffected, but what it says about files missing from {InstallerFolder} may be short of the full picture. Running it again may pick up more. | InstallerClean không khớp được hết mọi thứ trong các bản ghi Windows, nên đã không đọc hết chúng. Những gì tìm được không bị ảnh hưởng, nhưng những gì nói về các tệp thiếu khỏi {InstallerFolder} có thể chưa đầy đủ. Chạy lại có thể tìm thấy thêm. |
| Deleting {0} unneeded {1}... | Đang xóa {0} {1} không cần thiết... |
| Permanently deleted {0} unneeded {1}. | Đã xóa vĩnh viễn {0} {1} không cần thiết. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Lỗi: chưa chỉ định đích để chuyển. Dùng /m ĐƯỜNG_DẪN. (Mặc định đặt trong GUI là theo từng người dùng và không áp dụng cho các lần chạy theo lịch hoặc bằng tài khoản dịch vụ.) |
| Error: destination cannot be inside the Windows Installer folder. | Lỗi: đích không thể nằm bên trong thư mục Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Lỗi: đích phải là một đường dẫn đầy đủ. Nhận được: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Lỗi: đích {0} phân giải vào bên trong một thư mục hệ thống của Windows. Hãy chọn một đường dẫn ngoài %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% và %ProgramData%. |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | Lỗi: không đủ dung lượng tại {0}. Chuyển các tệp này cần {1} mà chỉ còn trống {2}. Không có gì được chuyển. |
| Error: something is using Windows Installer right now, such as a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | Lỗi: có thứ gì đó đang dùng Windows Installer ngay lúc này, chẳng hạn một bản cập nhật Windows hoặc một chương trình đang cài trong nền. /m và /d bị chặn trong lúc đó. Hãy thử lại khi xong. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Lỗi: máy này có một giao dịch Windows Installer trước đó đang bị treo. Hãy tiếp tục hoặc hoàn tác lần cài đặt ấy (hoặc khởi động lại Windows) trước khi dọn {InstallerFolder}. |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | Lỗi: một thao tác tệp đã xếp hàng sau khi khởi động lại có nhắm vào {InstallerFolder} ({0}). Hãy khởi động lại Windows để hoàn tất thao tác đó trước khi dọn. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Lỗi: Windows Installer đang có việc dở dang, nên /m và /d bị chặn. InstallerClean sẽ không đụng vào {InstallerFolder} khi thư mục đang thay đổi. Hãy thử lại khi xong. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | Lỗi: InstallerClean không lấy được khóa Windows Installer vốn ngăn hai chương trình cùng lúc thay đổi phần mềm đã cài, nên không thể loại trừ khả năng một tệp trở nên cần thiết giữa chừng. Không có gì bị xóa. Hãy thử lại, và khởi động lại Windows nếu việc này cứ tiếp diễn. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. Try again, and restart Windows if it keeps happening. | Lỗi: InstallerClean không lấy được khóa Windows Installer vốn ngăn hai chương trình cùng lúc thay đổi phần mềm đã cài, nên không thể loại trừ khả năng một tệp trở nên cần thiết giữa chừng. Không có gì được chuyển. Hãy thử lại, và khởi động lại Windows nếu việc này cứ tiếp diễn. |
| Moving {0} unneeded {1} to {2}... | Đang chuyển {0} {1} không cần thiết tới {2}... |
| Moved {0} unneeded {1}. | Đã chuyển {0} {1} không cần thiết. |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then run the command again. | InstallerClean không còn xác nhận được thư mục đích, nên đã dừng lại thay vì ghi nhầm chỗ. Hãy kiểm tra {0}, rồi chạy lại lệnh. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Một tiến trình InstallerClean khác đang giữ khóa một-thực-thể (GUI hoặc một lần chạy CLI khác). Mã thoát 75 (tạm thời); có thể thử lại sau. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Lưu ý: ghi vào Nhật ký sự kiện thất bại. Hãy kiểm tra quyền của nhật ký Ứng dụng hoặc Chính sách nhóm. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - dọn dẹp {InstallerFolder} |
| Removes cached .msi and .msp files that no installed program still needs. | Bỏ các tệp .msi và .msp trong bộ đệm mà không chương trình đã cài nào cần. |
| Needs an elevated (administrator) prompt; Windows will not start it. | Cần dấu nhắc quản trị viên; nếu không Windows sẽ không khởi chạy. |
| Usage: | Cách dùng: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help        Hiển thị trợ giúp (cũng nhận /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version     In ra phiên bản (cũng nhận -v) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s            Chỉ quét - liệt kê tệp không cần |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d            Xóa vĩnh viễn tệp không cần thiết |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m            Chuyển tới thư mục đích đã lưu |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m ĐƯỜNG_DẪN  Chuyển tới đường dẫn được chỉ định |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli giữ dấu nhắc cho tới khi xong, để một tập lệnh hoặc<br>một tác vụ theo lịch có thể chờ nó. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | Thư mục theo người dùng; tác vụ theo lịch hoặc SYSTEM: /m ĐƯỜNG_DẪN. |
| Exit codes: | Mã thoát: |
|   0   success: the run did what it was asked and nothing failed |   0   thành công: đã làm đúng việc được yêu cầu, không có gì hỏng |
|   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |   1   thất bại: không xử lý gì (đối số hoặc đích sai, quét thất bại<br>       hoặc mọi tệp đều lỗi) |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   một phần: xử lý được một phần (một lỗi hoặc một Ctrl+C) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  tạm thời: một điều kiện tạm thời đã chặn lần chạy (xem thông báo) |
|   130 cancelled (Ctrl+C) |   130 đã hủy (Ctrl+C) |
