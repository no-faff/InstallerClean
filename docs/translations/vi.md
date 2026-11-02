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
| The anonymous report | Báo cáo ẩn danh |
| InstallerClean couldn't read the report. | InstallerClean không đọc được báo cáo. |

## Section headings

| English | Tiếng Việt |
| --- | --- |
| PATCHES | BẢN VÁ |
| PRODUCT DETAILS | CHI TIẾT SẢN PHẨM |
| BACKUP FOLDER | THƯ MỤC SAO LƯU |
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
| Carry on _without it | Tiếp tục mà _không chờ |
| Check for _updates | _Kiểm tra cập nhật |
| _Close | Đón_g |
| _Close without donating | Đóng mà _không ủng hộ |
| _Delete permanently | _Xóa vĩnh viễn |
| _Done | _Xong |
| Details | Chi tiết |
| _Donate | Ủng _hộ |
| _Donate $3 | Ủng _hộ 3 USD |
| Donate | Ủng hộ |
| Leave a _star on GitHub | Gắn _sao trên GitHub |
| Apache 2.0 licence | Giấy phép Apache 2.0 |
| _Move | _Chuyển |
| Path to folder if you move rather than delete. | Đường dẫn thư mục nếu bạn chuyển thay vì xóa. |
| _Re-scan | _Quét lại |

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
| (no program) | (không có chương trình) |
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
| Checking registry for additional packages... | Đang kiểm tra sổ đăng ký để tìm các gói bổ sung... |
| Matching files against installed programs... | Đang đối chiếu các tệp với các chương trình đã cài... |
| {0} of {1} | {0}/{1} |
| Checking remaining files... | Đang kiểm tra các tệp còn lại... |
| Waiting for drive {0} to respond... | Đang chờ ổ đĩa {0} phản hồi... |
| Waiting for {0} to respond... | Đang chờ {0} phản hồi... |
| Any file still to be checked against it will be left alone. | Mọi tệp còn cần đối chiếu với nó sẽ được để nguyên. |
| Scan complete ({0}) | Quét xong ({0}) |
| Found {0} {1} you can safely delete. | Đã tìm thấy {0} {1} bạn có thể xóa an toàn. |
| Preparing destination folder... | Đang chuẩn bị thư mục đích... |
| Moving unneeded files... | Đang chuyển các tệp không cần thiết... |
| Deleting unneeded files... | Đang xóa các tệp không cần thiết... |
| Move cancelled. {0} of {1} {2} processed. | Đã hủy chuyển. Đã xử lý {0}/{1} {2}. |
| Delete cancelled. {0} of {1} {2} processed. | Đã hủy xóa. Đã xử lý {0}/{1} {2}. |
| {0}. Details are in {1}. | {0}. Chi tiết trong {1}. |
| {0}. The crash log could not be written. | {0}. Không thể ghi nhật ký sự cố. |
| {0}. Details are in {1}. | {0}. Chi tiết trong {1}. |
| {0}. The crash log could not be written. | {0}. Không thể ghi nhật ký sự cố. |
| Access denied. Windows refused the scan. | Truy cập bị từ chối. Windows đã từ chối lần quét. |
| Scan stopped. | Đã dừng quét. |
| Scan cancelled. | Đã hủy quét. |
| Ready | Sẵn sàng |
| Scan failed ({0}). Details in {1}. | Quét thất bại ({0}). Chi tiết trong {1}. |
| Scan failed ({0}). The crash log could not be written. | Quét thất bại ({0}). Không thể ghi nhật ký sự cố. |

## Main screen text

| English | Tiếng Việt |
| --- | --- |
| Any unneeded files below are safe to delete. | Mọi tệp không cần thiết bên dưới đều có thể xóa an toàn. |
| They sit in {InstallerFolder}. InstallerClean asks Windows about every installed program: a file is listed when no program claims it ({0}), or when a newer patch has replaced it and no program could roll back to it ({1}). | Chúng nằm trong {InstallerFolder}. InstallerClean hỏi Windows về từng chương trình đã cài: một tệp được liệt kê khi không chương trình nào nhận nó ({0}), hoặc khi một bản vá mới hơn đã thay thế nó và không chương trình nào có thể quay lại dùng nó ({1}). |
| Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update and uninstall as normal. Putting them back into {InstallerFolder} restores everything. Or delete them permanently now. | Hãy chuyển chúng vào một thư mục sao lưu do bạn chọn, rồi xóa thư mục đó khi bạn đã yên tâm rằng các chương trình của mình vẫn cập nhật và gỡ cài đặt bình thường. Đưa chúng trở lại {InstallerFolder} sẽ khôi phục mọi thứ. Hoặc xóa vĩnh viễn ngay bây giờ. |
| Nothing scanned yet. | Chưa quét gì cả. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Nhấn Quét lại để tìm trong {InstallerFolder} những tệp cài đặt mà không chương trình nào còn cần. |
| Nothing to show until the next scan. | Không có gì để hiển thị cho đến lần quét tiếp theo. |
| The scan that runs after a Move or Delete didn't finish, so there's no list to show. Press Re-scan to scan {InstallerFolder} again. | Lần quét chạy sau Chuyển hoặc Xóa đã không hoàn tất, nên không có danh sách nào để hiển thị. Nhấn Quét lại để quét {InstallerFolder} một lần nữa. |
| What stopped it is recorded in {0}. | Nguyên nhân dừng quét được ghi lại trong {0}. |
| These files can't be cleaned up right now. | Hiện chưa thể dọn những tệp này. |
| Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Có thứ gì đó đang dùng Windows Installer ngay lúc này, chẳng hạn một bản cập nhật Windows hoặc một chương trình đang cài trong nền. Chuyển và Xóa tạm dừng trong lúc đó, để InstallerClean không đụng vào {InstallerFolder} khi thư mục đang thay đổi. Xong rồi thì quét lại, hai nút sẽ trở lại. |
| Windows refused InstallerClean permission to check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If Windows still refuses, this isn't a machine InstallerClean can clean. | Windows đã từ chối cho InstallerClean quyền kiểm tra xem Windows Installer có đang bận không, nên Chuyển và Xóa tạm dừng và InstallerClean sẽ không đụng vào {InstallerFolder}. Hãy khởi động lại Windows rồi quét lại. Nếu Windows vẫn từ chối thì đây không phải máy mà InstallerClean có thể dọn. |
| InstallerClean couldn't check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If it still can't check, this isn't a machine InstallerClean can clean. | InstallerClean không kiểm tra được Windows Installer có đang bận không, nên Chuyển và Xóa tạm dừng và InstallerClean sẽ không đụng vào {InstallerFolder}. Hãy khởi động lại Windows rồi quét lại. Nếu vẫn không kiểm tra được thì đây không phải máy mà InstallerClean có thể dọn. |
| InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}). Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If it still can't check, this isn't a machine InstallerClean can clean. | InstallerClean không kiểm tra được Windows Installer có đang bận không, vì Windows báo lỗi: {0} (lỗi {1}). Chuyển và Xóa tạm dừng và InstallerClean sẽ không đụng vào {InstallerFolder}. Hãy khởi động lại Windows rồi quét lại. Nếu vẫn không kiểm tra được thì đây không phải máy mà InstallerClean có thể dọn. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Máy này có một giao dịch Windows Installer trước đó đang bị treo. Hãy tiếp tục hoặc hoàn tác lần cài đặt ấy (hoặc khởi động lại Windows) trước khi dọn {InstallerFolder}. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows đã xếp hàng một thao tác đổi tên tệp cho lần khởi động tới, có ảnh hưởng tới {InstallerFolder}. Hãy khởi động lại Windows trước khi dọn. |
| A file operation is queued for the next restart and InstallerClean can't tell which files it names, so it can't rule out that they're in {InstallerFolder}. Restart Windows before cleaning. | Có một thao tác tệp đang xếp hàng chờ lần khởi động lại tới và InstallerClean không biết được thao tác đó nêu tên những tệp nào, nên không thể loại trừ khả năng chúng nằm trong {InstallerFolder}. Hãy khởi động lại Windows trước khi dọn dẹp. |
| InstallerClean couldn't read one of the Windows settings it checks before touching {InstallerFolder}, so it can't tell whether an installer operation is running or waiting for a restart. Restart Windows and Re-scan. If the setting still won't read, this isn't a machine InstallerClean can clean. | InstallerClean không đọc được một trong các thiết lập Windows mà nó kiểm tra trước khi đụng vào {InstallerFolder}, nên không biết được có thao tác cài đặt nào đang chạy hay đang chờ khởi động lại không. Hãy khởi động lại Windows rồi quét lại. Nếu thiết lập đó vẫn không đọc được thì đây không phải máy mà InstallerClean có thể dọn. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer đang có việc dở dang, nên Chuyển và Xóa tạm dừng. InstallerClean sẽ không đụng vào {InstallerFolder} khi thư mục đang thay đổi. Xong rồi thì quét lại, hai nút sẽ trở lại. |
| Select a file to view details. | Chọn một tệp để xem chi tiết. |
| Select a product to view details. | Chọn một sản phẩm để xem chi tiết. |
| No metadata available. | Không có siêu dữ liệu. |
| This installer file is missing. It causes no trouble now, and won't until the day you try to update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To put it back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Thiếu tệp cài đặt này. Hiện tại nó không gây rắc rối gì, và sẽ không gây rắc rối cho tới ngày bạn thử cập nhật hoặc gỡ cài đặt chương trình mà nó thuộc về. Khi đó bước này có thể thất bại, vì Windows tìm tệp này mà không thấy.<br><br>Để đưa nó trở lại, bạn cần bộ cài của đúng phiên bản bạn đang có. Hãy lấy từ nhà sản xuất chương trình và chạy đè lên bản đang cài. Phiên bản mới hơn không dùng được: nó phải gỡ bản bạn đang có trước, và chính bước đó mới cần tệp này. Gỡ cài đặt trước cũng không được, vì cùng lý do. Việc này sẽ khôi phục tệp và giữ nguyên các thiết lập của bạn, nhưng Microsoft không bảo đảm điều đó. |
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
| Nothing offered on this PC | Không có gì được đề xuất trên máy này |
| One file ({2}) was held back because Windows shows it was changed after InstallerClean started scanning. If a program was installing or updating, scan again once it has finished and InstallerClean will check it again. | Một tệp ({2}) đã bị giữ lại vì Windows cho thấy nó đã bị thay đổi sau khi InstallerClean bắt đầu quét. Nếu khi đó có chương trình đang cài đặt hoặc cập nhật, hãy quét lại khi việc đó xong và InstallerClean sẽ kiểm tra lại tệp này. |
| {0} {1} ({2}) were held back because Windows shows they were changed after InstallerClean started scanning. If a program was installing or updating, scan again once it has finished and InstallerClean will check them again. | {0} {1} ({2}) đã bị giữ lại vì Windows cho thấy chúng đã bị thay đổi sau khi InstallerClean bắt đầu quét. Nếu khi đó có chương trình đang cài đặt hoặc cập nhật, hãy quét lại khi việc đó xong và InstallerClean sẽ kiểm tra lại chúng. |
| Delete that folder when you're satisfied all is well. | Hãy xóa thư mục đó khi bạn đã yên tâm rằng mọi thứ đều ổn. |
| Delete that folder when you're satisfied all is well. You won't actually reclaim the space until you do. | Hãy xóa thư mục đó khi bạn đã yên tâm rằng mọi thứ đều ổn. Chỉ khi đó dung lượng mới thực sự được giải phóng. |
| {0} freed | Đã giải phóng {0} |
| {0} moved | Đã chuyển {0} |
| Nothing was moved | Không có tệp nào được chuyển |
| Nothing was deleted | Không có tệp nào bị xóa |
| {0} file could not be moved. | Không thể chuyển {0} tệp. |
| {0} files could not be moved. | Không thể chuyển {0} tệp. |
| {0} file could not be deleted. | Không thể xóa {0} tệp. |
| {0} files could not be deleted. | Không thể xóa {0} tệp. |
| {0} {1} moved to: {2} | Đã chuyển {0} {1} tới: {2} |
| {0} {1} moved to: {2} | Đã chuyển {0} {1} tới: {2} |
| {0} file held back. The scan said it was unneeded. The final check couldn't confirm that. | Đã giữ lại {0} tệp. Lần quét cho rằng nó không cần thiết. Lần kiểm tra cuối không xác nhận được điều đó. |
| {0} files held back. The scan said these were unneeded. The final check couldn't confirm that. | Đã giữ lại {0} tệp. Lần quét cho rằng chúng không cần thiết. Lần kiểm tra cuối không xác nhận được điều đó. |
| {0} {1} kept in place, because Windows has a record of the program named inside. | Đã giữ nguyên {0} {1}, vì Windows có bản ghi về chương trình được nêu tên bên trong. |
| {0} {1} kept in place, because InstallerClean couldn't find a program named inside. | Đã giữ nguyên {0} {1}, vì InstallerClean không tìm thấy tên chương trình nào bên trong. |
| Moved {0} of {1} {2} to {3} before you cancelled. | Đã chuyển {0}/{1} {2} tới {3} trước khi bạn hủy. |
| Permanently deleted {0} of {1} {2} before you cancelled. | Đã xóa vĩnh viễn {0}/{1} {2} trước khi bạn hủy. |
| It's simple to undo. Move them back into {InstallerFolder} and everything will be back to how it was. | Hoàn tác rất đơn giản. Hãy chuyển chúng trở lại {InstallerFolder} và mọi thứ sẽ như cũ. |
| {0} {1} permanently deleted | Đã xóa vĩnh viễn {0} {1} |
| {0} {1} permanently deleted | Đã xóa vĩnh viễn {0} {1} |
| Any small donation is much appreciated | Mọi đóng góp dù nhỏ đều rất được trân trọng |
| Send _anonymous report | _Gửi báo cáo ẩn danh |
| The report adds your result to [this chart] on GitHub, which shows how much space people are reclaiming with InstallerClean. It also holds these technical details, to help me spot any bugs: | Báo cáo sẽ thêm kết quả của bạn vào [biểu đồ này] trên GitHub, cho thấy mọi người đang giải phóng được bao nhiêu dung lượng với InstallerClean. Báo cáo cũng chứa các chi tiết kỹ thuật sau để giúp tôi phát hiện lỗi: |
| How much space was freed, and how many files were moved or deleted | Đã giải phóng bao nhiêu dung lượng, và đã chuyển hoặc xóa bao nhiêu tệp |
| Whether a Move went to the same drive, another drive, a removable drive or a network share | Thao tác Chuyển đã đưa tệp đến cùng ổ đĩa, ổ đĩa khác, ổ đĩa di động hay chia sẻ mạng |
| How long the scan and the Move or Delete took | Lần quét và thao tác Chuyển hoặc Xóa mất bao lâu |
| How many installer files there are of each kind, and how much space they take | Có bao nhiêu tệp cài đặt mỗi loại, và chúng chiếm bao nhiêu dung lượng |
| How many files were left alone, and why | Có bao nhiêu tệp được để nguyên, và vì sao |
| How often InstallerClean waited for a slow drive or network share, and how many it stopped waiting for | InstallerClean đã chờ ổ đĩa hoặc chia sẻ mạng chậm bao nhiêu lần, và đã ngừng chờ bao nhiêu trong số đó |
| How many programs and patches Windows has a record of | Windows có bản ghi về bao nhiêu chương trình và bản vá |
| Whether Windows still makes old-style short file names | Windows có còn tạo tên tệp ngắn kiểu cũ hay không |
| Whether Windows is 10 or 11 and x64 or Arm, and which language and country or region it's set to | Windows là bản 10 hay 11, x64 hay Arm, và đang đặt ngôn ngữ cùng quốc gia hoặc khu vực nào |
| InstallerClean's version, how you got it (setup, portable, slim, Scoop, the command-line tool on its own, built from source, or not known), whether the report was sent from InstallerClean or the command-line tool, and the language it was showing | Phiên bản InstallerClean, cách bạn có được nó (setup, portable, slim, Scoop, công cụ dòng lệnh dùng riêng, tự biên dịch từ mã nguồn, hoặc không rõ), báo cáo được gửi từ InstallerClean hay từ công cụ dòng lệnh, và ngôn ngữ nó đang hiển thị |
| Counts of anything that went wrong | Số lần xảy ra bất kỳ sự cố nào |
| Each of these is a number or a label. Nothing in the report names you, your PC, your files or your programs, and I never see your IP address. | Mỗi mục này là một con số hoặc một nhãn. Không có gì trong báo cáo nêu tên bạn, máy tính, tệp hay chương trình của bạn, và tôi không bao giờ thấy địa chỉ IP của bạn. |
| See exactly what's sent | Xem chính xác những gì được gửi |

## Summaries and counts

| English | Tiếng Việt |
| --- | --- |
| {0} file left alone | {0} tệp được để nguyên |
| {0} files left alone | {0} tệp được để nguyên |
| {0} unneeded file to clean up | {0} tệp không cần thiết để dọn |
| {0} unneeded files to clean up | {0} tệp không cần thiết để dọn |
| Windows has a record for {0} file that isn't in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. Open Details for what to do. | Windows có hồ sơ về {0} tệp không nằm trong {InstallerFolder}: {1}. Trong sử dụng hằng ngày điều này không gây rắc rối, nhưng việc cập nhật hoặc gỡ cài đặt chương trình đó có thể thất bại. Hãy mở Chi tiết để biết cần làm gì. |
| Windows has records for {0} files that aren't in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. Open Details for what to do. | Windows có hồ sơ về {0} tệp không nằm trong {InstallerFolder}: {1}. Trong sử dụng hằng ngày điều này không gây rắc rối, nhưng việc cập nhật hoặc gỡ cài đặt các chương trình đó có thể thất bại. Hãy mở Chi tiết để biết cần làm gì. |
| {0} other program | {0} chương trình khác |
| {0} other programs | {0} chương trình khác |
| {0} file with no program named in the records | {0} tệp không có chương trình nào được nêu tên trong bản ghi |
| {0} files with no program named in the records | {0} tệp không có chương trình nào được nêu tên trong bản ghi |
| InstallerClean carried on without drive {0} and left alone any file still to be checked against it. Once it's responding normally, Re-scan. | InstallerClean đã tiếp tục mà không có ổ đĩa {0} và để nguyên mọi tệp còn cần đối chiếu với nó. Khi ổ đĩa phản hồi bình thường trở lại, hãy nhấn Quét lại. |
| InstallerClean carried on without {0} and left alone any file still to be checked against it. Once it's responding normally, Re-scan. | InstallerClean đã tiếp tục mà không có {0} và để nguyên mọi tệp còn cần đối chiếu với nó. Khi nó phản hồi bình thường trở lại, hãy nhấn Quét lại. |
| InstallerClean carried on without more than one drive or share ({0}) and left alone any file still to be checked against one of them. Once they're responding normally, Re-scan. | InstallerClean đã tiếp tục mà không có nhiều ổ đĩa hoặc chia sẻ mạng ({0}) và để nguyên mọi tệp còn cần đối chiếu với một trong số đó. Khi chúng phản hồi bình thường trở lại, hãy nhấn Quét lại. |
| Something was using Windows Installer when the scan started, such as a Windows Update or a program installing, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan once that has finished. | Có thứ gì đó đang dùng Windows Installer khi bắt đầu quét, chẳng hạn một bản cập nhật Windows hoặc một chương trình đang cài, nên InstallerClean đã giữ lại các tệp trong {InstallerFolder} không được nêu tên trong bản ghi của bất kỳ chương trình đã cài nào, thay vì đề xuất chúng. Hãy nhấn Quét lại khi việc đó xong. |
| A Windows Installer transaction was suspended on this machine when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Resume or roll back that install, or restart Windows, then Re-scan. | Có một giao dịch Windows Installer đang bị treo trên máy này khi bắt đầu quét, nên InstallerClean đã giữ lại các tệp trong {InstallerFolder} không được nêu tên trong bản ghi của bất kỳ chương trình đã cài nào, thay vì đề xuất chúng. Hãy tiếp tục hoặc hoàn tác lần cài đặt ấy, hoặc khởi động lại Windows, rồi nhấn Quét lại. |
| When the scan started, Windows had a file rename queued for the next restart that affects {InstallerFolder}, so InstallerClean held back the files there that no installed program's records name, rather than offering them. Restart Windows, then Re-scan. | Khi bắt đầu quét, Windows đã xếp hàng một thao tác đổi tên tệp cho lần khởi động tới, có ảnh hưởng tới {InstallerFolder}, nên InstallerClean đã giữ lại các tệp ở đó không được nêu tên trong bản ghi của bất kỳ chương trình đã cài nào, thay vì đề xuất chúng. Hãy khởi động lại Windows rồi nhấn Quét lại. |
| When the scan started, a file operation was queued for the next restart and InstallerClean couldn't tell which files it names, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows, then Re-scan. | Khi bắt đầu quét, có một thao tác tệp đang xếp hàng chờ lần khởi động lại tới và InstallerClean không biết được thao tác đó nêu tên những tệp nào, nên InstallerClean đã giữ lại các tệp trong {InstallerFolder} không được nêu tên trong bản ghi của bất kỳ chương trình đã cài nào, thay vì đề xuất chúng. Hãy khởi động lại Windows rồi nhấn Quét lại. |
| When the scan started, Windows refused InstallerClean permission to check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Khi bắt đầu quét, Windows đã từ chối cho InstallerClean quyền kiểm tra xem Windows Installer có đang bận không, nên InstallerClean đã giữ lại các tệp trong {InstallerFolder} không được nêu tên trong bản ghi của bất kỳ chương trình đã cài nào, thay vì đề xuất chúng. Hãy khởi động lại Windows rồi nhấn Quét lại. |
| When the scan started, InstallerClean couldn't read one of the Windows settings it checks, so it couldn't tell whether an installer operation was running or waiting for a restart, and it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Khi bắt đầu quét, InstallerClean không đọc được một trong các thiết lập Windows mà nó kiểm tra, nên không biết được có thao tác cài đặt nào đang chạy hay đang chờ khởi động lại không, và vì vậy InstallerClean đã giữ lại các tệp trong {InstallerFolder} không được nêu tên trong bản ghi của bất kỳ chương trình đã cài nào, thay vì đề xuất chúng. Hãy khởi động lại Windows rồi nhấn Quét lại. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Khi bắt đầu quét, InstallerClean không kiểm tra được Windows Installer có đang bận không, nên InstallerClean đã giữ lại các tệp trong {InstallerFolder} không được nêu tên trong bản ghi của bất kỳ chương trình đã cài nào, thay vì đề xuất chúng. Hãy khởi động lại Windows rồi nhấn Quét lại. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, because Windows gave an error: {0} (error {1}). So it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Khi bắt đầu quét, InstallerClean không kiểm tra được Windows Installer có đang bận không, vì Windows báo lỗi: {0} (lỗi {1}). Vì vậy InstallerClean đã giữ lại các tệp trong {InstallerFolder} không được nêu tên trong bản ghi của bất kỳ chương trình đã cài nào, thay vì đề xuất chúng. Hãy khởi động lại Windows rồi nhấn Quét lại. |
| Windows Installer had something in progress or waiting when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan once that has cleared. | Windows Installer có việc gì đó đang chạy hoặc đang chờ khi bắt đầu quét, nên InstallerClean đã giữ lại các tệp trong {InstallerFolder} không được nêu tên trong bản ghi của bất kỳ chương trình đã cài nào, thay vì đề xuất chúng. Hãy nhấn Quét lại khi việc đó đã xong xuôi. |
| This PC's clock went back or lost time while the scan was running, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan. | Đồng hồ của PC này đã lùi lại hoặc chạy chậm đi trong lúc quét, nên InstallerClean đã giữ lại các tệp trong {InstallerFolder} không được nêu tên trong bản ghi của bất kỳ chương trình đã cài nào, thay vì đề xuất chúng. Hãy nhấn Quét lại. |
| {0} of {1} {2} | {0}/{1} {2} |
| {0} unneeded {1} ({2}) | {0} {1} không cần thiết ({2}) |
| {0} file left alone ({1}) | {0} tệp được để nguyên ({1}) |
| {0} files left alone ({1}) | {0} tệp được để nguyên ({1}) |
| {0} missing | {0} thiếu |
| {0} missing | {0} thiếu |

## Confirmation dialogs

| English | Tiếng Việt |
| --- | --- |
| Move {0} {1} ({2})? | Chuyển {0} {1} ({2})? |
| This file will be moved to: | Tệp này sẽ được chuyển tới: |
| These files will be moved to: | Các tệp này sẽ được chuyển tới: |
| Delete {0} {1} ({2})? | Xóa {0} {1} ({2})? |
| This file will be deleted permanently. It's safe to do but if you'd like a backup, use Move instead. | Tệp này sẽ bị xóa vĩnh viễn. Việc này an toàn, nhưng nếu bạn muốn có bản sao lưu thì hãy dùng Chuyển. |
| These files will be deleted permanently. It's safe to do but if you'd like a backup, use Move instead. | Các tệp này sẽ bị xóa vĩnh viễn. Việc này an toàn, nhưng nếu bạn muốn có bản sao lưu thì hãy dùng Chuyển. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | Thư mục đó nằm trên cùng một ổ đĩa, nên dung lượng chưa trở lại cho tới khi bạn xóa nó. Hãy chọn một thư mục trên ổ đĩa khác nếu bạn muốn có dung lượng ngay. |

## Error messages

| English | Tiếng Việt |
| --- | --- |
| This is also recorded in {0}. | Điều này cũng được ghi lại trong {0}. |
| Access denied | Truy cập bị từ chối |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows đã từ chối quyền truy cập của InstallerClean, nên nó đã dừng lại. Không có gì bị xóa.<br><br>InstallerClean vốn đã chạy với quyền quản trị viên, nên khởi động lại theo cách đó cũng không giúp được gì. Windows không nói gì thêm về thứ đã từ chối quyền truy cập, nên không có gì cụ thể để thử. |
| InstallerClean stopped | InstallerClean đã dừng |
| Scan failed | Quét thất bại |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed. | Các bản ghi Windows Installer trả về hoàn toàn trống: không một chương trình đã cài hay bản cập nhật nào nhận là chủ của một tệp cài đặt trong bộ nhớ đệm. Điều đó không xảy ra trên một máy hoạt động bình thường (ngay cả một bản Windows vừa cài cũng có vài tệp như vậy), nên hoặc các bản ghi đã hỏng, hoặc không đọc được, và một lần quét tin vào câu trả lời này sẽ nhầm lẫn coi mọi tệp trong {InstallerFolder} là bị bỏ lại. Thay vào đó InstallerClean đã dừng. Không có gì bị xóa. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer không cho phép InstallerClean liệt kê những gì đã được cài. InstallerClean vốn đã chạy với quyền quản trị viên, nên chạy lại với quyền quản trị viên cũng không thay đổi được gì. Không có danh sách đó thì không có cách nào an toàn để biết tệp nào trong bộ nhớ đệm vẫn còn cần, nên InstallerClean đã dừng. Không có gì bị xóa. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: it read {1} {2}, then an entry came back unreadable (error code {0}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer không thể đưa cho InstallerClean một danh sách chương trình đã cài đọc được: nó đã đọc {1} {2}, rồi một mục trả về không đọc được (mã lỗi {0}). Thay vì làm việc với một danh sách chỉ đọc được một phần, InstallerClean đã dừng. Không có gì bị xóa. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer chưa bao giờ báo hiệu kết thúc danh sách chương trình đã cài: InstallerClean đã đọc {2} {3}, rồi bỏ cuộc sau {0} mục (mã lỗi cuối {1}). Không thể tin một danh sách không có điểm dừng, nên InstallerClean đã dừng. Không có gì bị xóa. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer chưa bao giờ báo hiệu kết thúc danh sách bản vá của một chương trình: InstallerClean đã đọc {2} {3}, rồi bỏ cuộc sau {0} mục (mã lỗi cuối {1}). Không thể tin một danh sách không có điểm dừng, nên InstallerClean đã dừng. Không có gì bị xóa. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean không khớp được các bản ghi Windows Installer với nội dung trong {InstallerFolder}. Gần như không có thứ gì các bản ghi trỏ tới thực sự nằm ở đó, và gần như không có thứ gì ở đó được bản ghi nào nêu tên, nên không tệp nào có thể được chứng tỏ là không cần thiết. Không có gì được đề xuất và không có gì bị bỏ đi. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean không khớp được các bản ghi Windows Installer với nội dung trong {InstallerFolder}. Thư mục có tệp bên trong, nhưng không một bản ghi nào trỏ tới bất cứ thứ gì trong đó, nên không tệp nào có thể được chứng tỏ là không cần thiết. Không có gì được đề xuất và không có gì bị bỏ đi. |
| InstallerClean can't scan {InstallerFolder} on this PC. Windows reports that folder as {0}, and InstallerClean only scans an Installer folder that Windows reports at its own path. Nothing has been offered and nothing has been removed. | InstallerClean không thể quét {InstallerFolder} trên máy này. Windows báo thư mục đó là {0}, và InstallerClean chỉ quét một thư mục Installer mà Windows báo đúng ở đường dẫn của chính nó. Không có gì được đề xuất và không có gì bị bỏ đi. |
| Windows reports that {InstallerFolder} doesn't exist on this PC, so InstallerClean stopped. Nothing has been offered and nothing has been removed. | Windows báo rằng {InstallerFolder} không tồn tại trên máy này, nên InstallerClean đã dừng lại. Không có gì được đề xuất và không có gì bị bỏ đi. |
| Windows reported an error when InstallerClean tried to list the files in {InstallerFolder}, so it couldn't get a complete list and stopped. Nothing has been offered and nothing has been removed. | Windows đã báo lỗi khi InstallerClean cố liệt kê các tệp trong {InstallerFolder}, nên InstallerClean không lấy được danh sách đầy đủ và đã dừng lại. Không có gì được đề xuất và không có gì bị bỏ đi. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean không đọc được đủ các bản ghi Windows Installer để chắc chắn thứ gì vẫn còn cần: danh sách chương trình đã cài trả về thiếu, và việc đọc chính các bản ghi đó trực tiếp từ sổ đăng ký cũng gặp lỗi. Một tệp có thể trông như bị bỏ lại chỉ vì bản ghi nêu tên nó nằm trong số những bản ghi không đọc được, nên InstallerClean đã dừng. Không có gì bị xóa. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean không khiến được Windows phân giải đường dẫn thật của {InstallerFolder}, nên không tệp nào có thể được chứng tỏ là nằm bên trong và không tệp nào được đề xuất để dọn. Lần quét này không tìm thấy gì vì phép kiểm tra ấy thất bại, chứ không phải vì thư mục đã sạch. Không có gì bị bỏ đi. |
| Nothing was deleted | Không có tệp nào bị xóa |
| Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. | Windows đã từ chối cho InstallerClean quyền kiểm tra xem Windows Installer có đang bận không, nên nó không thể loại trừ khả năng một tệp trở nên cần thiết giữa chừng, và không có gì bị xóa. |
| Nothing was moved | Không có tệp nào được chuyển |
| Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. | Windows đã từ chối cho InstallerClean quyền kiểm tra xem Windows Installer có đang bận không, nên nó không thể loại trừ khả năng một tệp trở nên cần thiết giữa chừng, và không có gì được chuyển. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | InstallerClean không lấy được khóa mà Windows Installer dùng để ngăn hai chương trình cùng lúc thay đổi phần mềm đã cài, nên không thể loại trừ khả năng một tệp trở nên cần thiết giữa chừng, và không có gì bị xóa. Hãy thử lại, và khởi động lại Windows nếu việc này cứ tiếp diễn. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. Try again, and restart Windows if it keeps happening. | InstallerClean không lấy được khóa mà Windows Installer dùng để ngăn hai chương trình cùng lúc thay đổi phần mềm đã cài, nên không thể loại trừ khả năng một tệp trở nên cần thiết giữa chừng, và không có gì được chuyển. Hãy thử lại, và khởi động lại Windows nếu việc này cứ tiếp diễn. |
| Invalid destination | Đích không hợp lệ |
| Move stopped | Đã dừng chuyển |
| Couldn't use that backup folder | Không thể dùng thư mục sao lưu đó |
| Move failed | Chuyển thất bại |
| Delete failed | Xóa thất bại |
| Setting not saved | Không lưu được cài đặt |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | Không thể lưu thay đổi. Lần chạy tiếp theo, InstallerClean sẽ quay lại cài đặt trước đó. |
| The destination cannot be inside the Windows Installer folder. | Đích không thể nằm bên trong thư mục Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Đích {0} phân giải vào bên trong một thư mục hệ thống của Windows. Hãy chọn một đường dẫn ngoài %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% và %ProgramData%. |
| Not enough space | Không đủ dung lượng |
| There isn't room at {0}<br><br>Required: {1}<br>Available: {2} | Không còn đủ chỗ tại {0}<br><br>Cần: {1}<br>Còn trống: {2} |
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
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | Thư mục sao lưu phải là một đường dẫn đầy đủ tới một thư mục, bắt đầu bằng ký tự ổ đĩa hoặc một chia sẻ mạng (ví dụ D:\Backup, hoặc \\server\backup). InstallerClean không dùng được cái này: {0} |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then Re-scan and try again. | InstallerClean không còn xác nhận được thư mục sao lưu, nên đã dừng lại. Hãy kiểm tra {0}, rồi Quét lại và thử lần nữa. |
| Cannot write to {0}. | Không thể ghi vào {0}. |
| You can save files in {0} but not open them again, so InstallerClean won't move files there. | Bạn có thể lưu tệp vào {0} nhưng không thể mở lại chúng, nên InstallerClean sẽ không chuyển tệp vào đó. |
| A file called '{0}' is already in the backup folder. | Đã có một tệp tên '{0}' trong thư mục sao lưu. |

## Update check

| English | Tiếng Việt |
| --- | --- |
| Check for updates | Kiểm tra cập nhật |
| Checking... | Đang kiểm tra... |
| Up to date. | Đã cập nhật. |
| InstallerClean {0} is available. | Đã có InstallerClean {0}. |
| It's strongly recommended to use the latest version. | Chúng tôi đặc biệt khuyên bạn dùng phiên bản mới nhất. |
| Download it from GitHub | Tải về từ GitHub |
| Download .NET Framework 4.8 from Microsoft | Tải .NET Framework 4.8 về từ Microsoft |
| Update now | Cập nhật ngay |
| This copy was installed by Scoop. To update it, close InstallerClean and run: | Bản này được cài bằng Scoop. Để cập nhật, hãy đóng InstallerClean rồi chạy: |
| This copy was installed by Scoop for all users. To update it, close InstallerClean, open a terminal as administrator and run: | Bản này được cài bằng Scoop cho tất cả người dùng. Để cập nhật, hãy đóng InstallerClean, mở terminal với quyền quản trị viên rồi chạy: |
| It needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then open InstallerClean to update. | Phiên bản này cần .NET Framework 4.8. Hãy cài đặt nó từ Microsoft, khởi động lại PC, rồi mở InstallerClean để cập nhật. |
| InstallerClean {0} can't be installed automatically. | Không thể tự động cài đặt InstallerClean {0}. |
| InstallerClean can't tell whether its setup installed it, so it can't update itself. | InstallerClean không thể xác định liệu nó có được cài bằng trình cài đặt của chính nó hay không, nên không thể tự cập nhật. |
| Copied | Đã sao chép |
| Couldn't copy it. Select the command and copy it yourself. | Không thể sao chép. Hãy chọn lệnh rồi tự sao chép. |
| _Continue without updating | _Tiếp tục mà không cập nhật |
| Downloading InstallerClean {0}... | Đang tải về InstallerClean {0}... |
| Checking the download... | Đang kiểm tra bản tải về... |
| The update couldn't be downloaded. | Không thể tải về bản cập nhật. |
| The update couldn't be installed. | Không thể cài đặt bản cập nhật. |
| Couldn't reach GitHub. Check your internet connection and try again. | Không thể kết nối tới GitHub. Hãy kiểm tra kết nối internet và thử lại. |
| GitHub returned an error response. Try again in a few minutes. | GitHub trả về phản hồi lỗi. Hãy thử lại sau vài phút. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | Phản hồi của GitHub không chứa bản phát hành nào nhận ra được. Hãy thử lại sau, hoặc mở thẳng trang phát hành. |
| The check timed out. Your connection to GitHub may be slow; try again. | Quá thời gian kiểm tra. Kết nối của bạn tới GitHub có thể chậm; hãy thử lại. |
| The check failed for an unknown reason. Details are in {0} if you need to report it. | Việc kiểm tra thất bại vì một lý do không xác định. Chi tiết nằm trong {0} nếu bạn cần báo cáo. |
| The check failed for an unknown reason. The crash log could not be written. | Việc kiểm tra thất bại vì một lý do không xác định. Không thể ghi nhật ký sự cố. |

## Opening links in your browser

| English | Tiếng Việt |
| --- | --- |
| Couldn't open your browser | Không thể mở trình duyệt của bạn |
| The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | Liên kết đã nằm trong bảng nhớ tạm, nên bạn có thể tự dán:<br><br>{0} |
| InstallerClean couldn't copy the link to your clipboard either, so here it is:<br><br>{0} | InstallerClean cũng không sao chép được liên kết vào bảng nhớ tạm, đây là liên kết:<br><br>{0} |

## Startup and crashes

| English | Tiếng Việt |
| --- | --- |
| InstallerClean | InstallerClean |
| It's already running. | Đang chạy rồi. |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | Đã xảy ra lỗi không mong muốn và InstallerClean cần đóng lại.<br><br>{0}<br><br>Chi tiết đã được ghi vào:<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | Đã xảy ra lỗi không mong muốn và InstallerClean cần đóng lại.<br><br>{0}<br><br>Không thể ghi nhật ký sự cố. |
| Startup error | Lỗi khởi động |
| Failed to start ({0}). Details written to:<br>{1} | Khởi động thất bại ({0}). Chi tiết đã được ghi vào:<br>{1} |
| Failed to start ({0}). The crash log could not be written. | Khởi động thất bại ({0}). Không thể ghi nhật ký sự cố. |
| Administrator rights needed | Cần quyền quản trị viên |
| InstallerClean needs administrator rights to see every program installed on this PC, and it isn't running as administrator, so it won't scan. Nothing has been changed.<br><br>Run InstallerClean again as administrator. | InstallerClean cần quyền quản trị viên để thấy mọi chương trình được cài trên máy này, nhưng hiện không chạy với tư cách quản trị viên, nên sẽ không quét. Không có gì bị thay đổi.<br><br>Hãy chạy lại InstallerClean với tư cách quản trị viên. |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log ghi lại các ngoại lệ chưa xử lý của InstallerClean.<br># Khi chạy với quyền nâng cao, thông báo ngoại lệ của framework có thể<br># chứa đường dẫn tệp trong phiên đang chạy (kể cả hồ sơ của người dùng<br># khác do các truy vấn Windows Installer liệt kê). Thông báo lỗi mạng<br># từ việc kiểm tra cập nhật hoặc gửi nhật ký kết quả có thể chứa URL<br># đích và địa chỉ IP hoặc proxy đã phân giải. Các mục về bản ghi<br># Windows Installer không đọc được có thể chứa SID tài khoản Windows<br># (S-1-5-21-...) và mã sản phẩm của phần mềm đã cài.<br># Hãy xóa cả ba loại thông tin này trước khi đính kèm tệp này vào một<br># báo cáo lỗi công khai.<br> |

## Tooltips (hover text)

| English | Tiếng Việt |
| --- | --- |
| Any small donation is much appreciated | Mọi đóng góp dù nhỏ đều rất được trân trọng |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Đã yêu cầu hủy. InstallerClean đang chờ bước hiện tại tới điểm dừng. Việc này có thể mất vài giây khi I/O nặng hoặc khi đang gọi cơ sở dữ liệu MSI. |
| Close | Đóng |
| I really should charge for InstallerClean - because it's been a huge amount of work - but I want to keep it free so it helps as many people as possible. Your donation makes it worthwhile and is very much appreciated. | Lẽ ra tôi nên thu phí InstallerClean, vì nó đã tốn rất nhiều công sức, nhưng tôi muốn giữ nó miễn phí để giúp được càng nhiều người càng tốt. Sự ủng hộ của bạn khiến mọi công sức trở nên xứng đáng và tôi rất trân trọng điều đó. |
| A star helps other people find it | Một ngôi sao giúp người khác tìm thấy InstallerClean |
| Minimise | Thu nhỏ |
| Move the unneeded files to the backup folder | Chuyển các tệp không cần thiết vào thư mục sao lưu |
| Move the unneeded files to a backup folder. You'll choose it next. | Chuyển các tệp không cần thiết vào một thư mục sao lưu. Bạn sẽ chọn thư mục ngay sau đây. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder. | Chuyển các tệp không cần thiết vào thư mục sao lưu. Thư mục đó nằm trên cùng ổ đĩa, nên dung lượng chỉ được giải phóng sau khi bạn xóa thư mục đó. |
| Delete the unneeded files permanently. Use Move instead if you'd like a chance to satisfy yourself all is well. | Xóa vĩnh viễn các tệp không cần thiết. Hãy dùng Chuyển nếu bạn muốn có cơ hội tự mình yên tâm rằng mọi thứ đều ổn. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Tên chủ thể từ chứng chỉ Authenticode được nhúng. Chưa xác minh chuỗi. |
| Change language. The program will restart. | Thay đổi ngôn ngữ. Chương trình sẽ khởi động lại. |

## Screen reader labels

| English | Tiếng Việt |
| --- | --- |
| Donate | Ủng hộ |
| Donate $3 | Ủng hộ 3 USD |
| Donate | Ủng hộ |
| Cancel operation | Hủy thao tác |
| Cancel scan | Hủy quét |
| Cancel startup scan | Hủy quét khi khởi động |
| Carry on without it, drive {0} | Tiếp tục mà không chờ, ổ đĩa {0} |
| Carry on without it, {0} | Tiếp tục mà không chờ, {0} |
| Close | Đóng |
| Close window | Đóng cửa sổ |
| Close result and return to main window | Đóng kết quả và quay lại cửa sổ chính |
| Leave a star on github | Gắn sao trên github |
| Minimise | Thu nhỏ |
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Xóa vĩnh viễn sẽ bỏ đi các tệp không cần thiết. Hủy sẽ đóng lại mà không xóa gì. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Chuyển sẽ đặt các tệp không cần thiết vào thư mục đích đã chọn. Hủy sẽ để chúng nguyên chỗ cũ. |
| Say thanks | Lời cảm ơn |
| Check for updates | Kiểm tra cập nhật |
| Checks github's releases page for a newer version. | Kiểm tra trang phát hành của github xem có phiên bản mới hơn không. |
| Opens the readme on github in your browser. | Mở readme trên github trong trình duyệt của bạn. |
| Opens the issue tracker on github.com in your browser. | Mở trình theo dõi vấn đề (Issues) trên github.com trong trình duyệt của bạn. |
| If ticked, InstallerClean checks github for a newer version when you run it. | Nếu được đánh dấu, InstallerClean sẽ kiểm tra github xem có phiên bản mới hơn không khi bạn chạy nó. |
| Download it from github | Tải về từ github |
| Download .NET Framework 4.8 from Microsoft | Tải .NET Framework 4.8 về từ Microsoft |
| Copy the command | Sao chép lệnh |
| Scoop's update command | Lệnh cập nhật của Scoop |
| Opens the licence file on github.com in your browser. | Mở tệp giấy phép trên github.com trong trình duyệt của bạn. |
| Backup folder | Thư mục sao lưu |
| Patches | Bản vá |
| Product details | Chi tiết sản phẩm |
| Backup folder | Thư mục sao lưu |
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
| Waiting for a drive or share | Đang chờ ổ đĩa hoặc chia sẻ mạng |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | Các tệp không thể xử lý |
| Explains this folder, and how to recover a file, in the README | Giải thích thư mục này, và cách khôi phục một tệp, trong README |
| {0} Press Alt+{1} to carry on without it. {2} | {0} Nhấn Alt+{1} để tiếp tục mà không chờ. {2} |
| What the report holds | Báo cáo chứa những gì |
| What the report holds | Báo cáo chứa những gì |
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
| drive {0} | ổ đĩa {0} |
| .  | .  |
| {0} TB | {0} TB |
| {0} GB | {0} GB |
| {0} MB | {0} MB |
| {0} KB | {0} KB |
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
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({2}) rather than offering it. | InstallerClean không thể chắc chắn những tệp nào trong bộ nhớ đệm thuộc về các chương trình đã cài ở đây, nên đã giữ lại tệp duy nhất ({2}) thay vì đề xuất nó. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} ({2}) rather than offering them. | InstallerClean không thể chắc chắn những tệp nào trong bộ nhớ đệm thuộc về các chương trình đã cài ở đây, nên đã giữ lại {0} {1} ({2}) thay vì đề xuất chúng. |
| InstallerClean couldn't establish that one file is unneeded, so it has held it back ({2}) rather than offering it. | InstallerClean không thể xác định rằng một tệp là không cần thiết, nên đã giữ lại tệp đó ({2}) thay vì đề xuất nó. |
| InstallerClean couldn't establish that {0} {1} are unneeded, so it has held them back ({2}) rather than offering them. | InstallerClean không thể xác định rằng {0} {1} là không cần thiết, nên đã giữ lại các tệp đó ({2}) thay vì đề xuất chúng. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({2}) rather than offering it. | InstallerClean không thể chắc chắn những tệp nào trong bộ nhớ đệm thuộc về các chương trình đã cài ở đây, nên đã giữ lại tệp duy nhất ({2}) thay vì đề xuất nó. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} ({2}) rather than offering them. | InstallerClean không thể chắc chắn những tệp nào trong bộ nhớ đệm thuộc về các chương trình đã cài ở đây, nên đã giữ lại {0} {1} ({2}) thay vì đề xuất chúng. |
| InstallerClean couldn't establish that one file is unneeded, so it has held it back ({2}) rather than offering it. | InstallerClean không thể xác định rằng một tệp là không cần thiết, nên đã giữ lại tệp đó ({2}) thay vì đề xuất nó. |
| InstallerClean couldn't establish that {0} {1} are unneeded, so it has held them back ({2}) rather than offering them. | InstallerClean không thể xác định rằng {0} {1} là không cần thiết, nên đã giữ lại các tệp đó ({2}) thay vì đề xuất chúng. |
| Why it couldn't be certain: | Vì sao không thể chắc chắn: |
|   A file path in Windows Installer's own records wouldn't resolve, so nothing could be matched to it. |   Một đường dẫn tệp trong chính hồ sơ của Windows Installer không phân giải được, nên không thể đối chiếu gì với nó. |
|   A file Windows has a record of couldn't be identified, so it couldn't be matched to what's in the folder. |   Không nhận dạng được một tệp mà Windows có hồ sơ, nên không thể đối chiếu nó với những gì có trong thư mục. |
|   A program may be installed more than once on this PC, and the records can't say which copy a file belongs to. |   Một chương trình có thể đã được cài nhiều hơn một lần trên máy này, và hồ sơ không cho biết một tệp thuộc về bản sao nào. |
|   Part of Windows Installer's records wouldn't read, so a file could belong to a program recorded there. |   Một phần hồ sơ của Windows Installer không đọc được, nên một tệp có thể thuộc về một chương trình được ghi ở đó. |
|   A file in the folder couldn't be identified, so it couldn't be matched against the records. |   Không nhận dạng được một tệp trong thư mục, nên không thể đối chiếu nó với hồ sơ. |
|   Either a file wouldn't say which program it belongs to, or Windows wouldn't give a clear answer about that program. |   Hoặc một tệp không cho biết nó thuộc về chương trình nào, hoặc Windows không trả lời rõ ràng về chương trình đó. |
|   A check on which programs the files belong to gave answers that didn't line up with the files it was handed. |   Một lần kiểm tra xem các tệp thuộc về những chương trình nào đã trả về các câu trả lời không khớp với những tệp được giao cho nó. |
|   Either a patch file wouldn't say which patch it is, or it wouldn't say which programs it's for, or Windows wouldn't say in full which programs have that patch. |   Hoặc một tệp bản vá không cho biết nó là bản vá nào, hoặc không cho biết nó dành cho những chương trình nào, hoặc Windows không cho biết đầy đủ những chương trình nào có bản vá đó. |
| Windows has a record for {0} file that is not in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. To put the file back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Windows có hồ sơ về {0} tệp không nằm trong {InstallerFolder}: {1}. Trong sử dụng hằng ngày điều này không gây rắc rối, nhưng việc cập nhật hoặc gỡ cài đặt chương trình đó có thể thất bại. Để đưa tệp trở lại, bạn cần bộ cài của đúng phiên bản bạn đang có. Hãy lấy từ nhà sản xuất chương trình và chạy đè lên bản đang cài. Phiên bản mới hơn không dùng được: nó phải gỡ bản bạn đang có trước, và chính bước đó mới cần tệp này. Gỡ cài đặt trước cũng không được, vì cùng lý do. Việc này sẽ khôi phục tệp và giữ nguyên các thiết lập của bạn, nhưng Microsoft không bảo đảm điều đó. |
| Windows has records for {0} files that are not in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. To put a file back, you need the installer for the version you already have of that program. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs the file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Windows có hồ sơ về {0} tệp không nằm trong {InstallerFolder}: {1}. Trong sử dụng hằng ngày điều này không gây rắc rối, nhưng việc cập nhật hoặc gỡ cài đặt các chương trình đó có thể thất bại. Để đưa một tệp trở lại, bạn cần bộ cài của đúng phiên bản chương trình đó mà bạn đang có. Hãy lấy từ nhà sản xuất chương trình và chạy đè lên bản đang cài. Phiên bản mới hơn không dùng được: nó phải gỡ bản bạn đang có trước, và chính bước đó mới cần tệp đó. Gỡ cài đặt trước cũng không được, vì cùng lý do. Việc này sẽ khôi phục tệp và giữ nguyên các thiết lập của bạn, nhưng Microsoft không bảo đảm điều đó. |
| InstallerClean couldn't be certain that the one superseded file is no longer needed, so it has held it back. | InstallerClean không thể chắc chắn rằng tệp bị thay thế duy nhất đó không còn cần đến nữa, nên đã giữ lại nó. |
| InstallerClean couldn't be certain that {0} superseded files are no longer needed, so it has held them back. | InstallerClean không thể chắc chắn rằng {0} tệp bị thay thế không còn cần đến nữa, nên đã giữ lại chúng. |
| InstallerClean stopped waiting for drive {0} and left alone any file still to be checked against it. Once it's responding normally, run the command again. | InstallerClean đã ngừng chờ ổ đĩa {0} và để nguyên mọi tệp còn cần đối chiếu với nó. Khi ổ đĩa phản hồi bình thường trở lại, hãy chạy lại lệnh. |
| InstallerClean stopped waiting for {0} and left alone any file still to be checked against it. Once it's responding normally, run the command again. | InstallerClean đã ngừng chờ {0} và để nguyên mọi tệp còn cần đối chiếu với nó. Khi nó phản hồi bình thường trở lại, hãy chạy lại lệnh. |
| InstallerClean stopped waiting for more than one drive or share ({0}) and left alone any file still to be checked against one of them. Once they're responding normally, run the command again. | InstallerClean đã ngừng chờ nhiều ổ đĩa hoặc chia sẻ mạng ({0}) và để nguyên mọi tệp còn cần đối chiếu với một trong số đó. Khi chúng phản hồi bình thường trở lại, hãy chạy lại lệnh. |
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
| Error: a file operation is queued for the next restart and InstallerClean can't tell which files it names, so it can't rule out {InstallerFolder}. Restart Windows before cleaning. | Lỗi: có một thao tác tệp đang xếp hàng chờ lần khởi động lại tới và InstallerClean không biết được thao tác đó nêu tên những tệp nào, nên không thể loại trừ {InstallerFolder}. Hãy khởi động lại Windows trước khi dọn dẹp. |
| Error: InstallerClean couldn't read one of the registry values it checks before touching {InstallerFolder}, so it can't rule out a Windows Installer operation in flight or queued for the next restart. /m and /d are blocked. Restart Windows and try again. If the read still fails, this isn't a machine InstallerClean can clean. | Lỗi: InstallerClean không đọc được một trong các giá trị registry mà nó kiểm tra trước khi đụng vào {InstallerFolder}, nên không thể loại trừ một thao tác Windows Installer đang chạy hoặc đang xếp hàng chờ lần khởi động lại tới. /m và /d bị chặn. Hãy khởi động lại Windows rồi thử lại. Nếu vẫn không đọc được thì đây không phải máy mà InstallerClean có thể dọn. |
| Error: InstallerClean couldn't check whether Windows Installer is busy, so it can't rule out an installation in progress. /m and /d are blocked. Restart Windows and try again. If the check still fails, this isn't a machine InstallerClean can clean. | Lỗi: InstallerClean không kiểm tra được Windows Installer có đang bận không, nên không thể loại trừ một lượt cài đặt đang diễn ra. /m và /d bị chặn. Hãy khởi động lại Windows rồi thử lại. Nếu vẫn không kiểm tra được thì đây không phải máy mà InstallerClean có thể dọn. |
| Error: InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}), so it can't rule out an installation in progress. /m and /d are blocked. Restart Windows and try again. If the check still fails, this isn't a machine InstallerClean can clean. | Lỗi: InstallerClean không kiểm tra được Windows Installer có đang bận không, vì Windows báo lỗi: {0} (lỗi {1}), nên không thể loại trừ một lượt cài đặt đang diễn ra. /m và /d bị chặn. Hãy khởi động lại Windows rồi thử lại. Nếu vẫn không kiểm tra được thì đây không phải máy mà InstallerClean có thể dọn. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Lỗi: Windows Installer đang có việc dở dang, nên /m và /d bị chặn. InstallerClean sẽ không đụng vào {InstallerFolder} khi thư mục đang thay đổi. Hãy thử lại khi xong. |
| Something was using Windows Installer when the scan started, such as a Windows Update or a program installing, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again once that has finished. | Có thứ gì đó đang dùng Windows Installer khi bắt đầu quét, chẳng hạn một bản cập nhật Windows hoặc một chương trình đang cài, nên InstallerClean đã giữ lại các tệp trong {InstallerFolder} không được nêu tên trong bản ghi của bất kỳ chương trình đã cài nào, thay vì đề xuất chúng. Hãy chạy lại lệnh khi việc đó xong. |
| A Windows Installer transaction was suspended on this machine when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Resume or roll back that install, or restart Windows, then run it again. | Có một giao dịch Windows Installer đang bị treo trên máy này khi bắt đầu quét, nên InstallerClean đã giữ lại các tệp trong {InstallerFolder} không được nêu tên trong bản ghi của bất kỳ chương trình đã cài nào, thay vì đề xuất chúng. Hãy tiếp tục hoặc hoàn tác lần cài đặt ấy, hoặc khởi động lại Windows, rồi chạy lại lệnh. |
| When the scan started, Windows had a file rename queued for the next restart that affects {InstallerFolder}, so InstallerClean held back the files there that no installed program's records name, rather than offering them. Restart Windows, then run it again. | Khi bắt đầu quét, Windows đã xếp hàng một thao tác đổi tên tệp cho lần khởi động tới, có ảnh hưởng tới {InstallerFolder}, nên InstallerClean đã giữ lại các tệp ở đó không được nêu tên trong bản ghi của bất kỳ chương trình đã cài nào, thay vì đề xuất chúng. Hãy khởi động lại Windows rồi chạy lại lệnh. |
| When the scan started, a file operation was queued for the next restart and InstallerClean couldn't tell which files it names, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows, then run it again. | Khi bắt đầu quét, có một thao tác tệp đang xếp hàng chờ lần khởi động lại tới và InstallerClean không biết được thao tác đó nêu tên những tệp nào, nên InstallerClean đã giữ lại các tệp trong {InstallerFolder} không được nêu tên trong bản ghi của bất kỳ chương trình đã cài nào, thay vì đề xuất chúng. Hãy khởi động lại Windows rồi chạy lại lệnh. |
| When the scan started, Windows refused InstallerClean permission to check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Khi bắt đầu quét, Windows đã từ chối cho InstallerClean quyền kiểm tra xem Windows Installer có đang bận không, nên InstallerClean đã giữ lại các tệp trong {InstallerFolder} không được nêu tên trong bản ghi của bất kỳ chương trình đã cài nào, thay vì đề xuất chúng. Hãy khởi động lại Windows rồi chạy lại lệnh. |
| When the scan started, InstallerClean couldn't read one of the Windows settings it checks, so it couldn't tell whether an installer operation was running or waiting for a restart, and it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Khi bắt đầu quét, InstallerClean không đọc được một trong các thiết lập Windows mà nó kiểm tra, nên không biết được có thao tác cài đặt nào đang chạy hay đang chờ khởi động lại không, và vì vậy InstallerClean đã giữ lại các tệp trong {InstallerFolder} không được nêu tên trong bản ghi của bất kỳ chương trình đã cài nào, thay vì đề xuất chúng. Hãy khởi động lại Windows rồi chạy lại lệnh. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Khi bắt đầu quét, InstallerClean không kiểm tra được Windows Installer có đang bận không, nên InstallerClean đã giữ lại các tệp trong {InstallerFolder} không được nêu tên trong bản ghi của bất kỳ chương trình đã cài nào, thay vì đề xuất chúng. Hãy khởi động lại Windows rồi chạy lại lệnh. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, because Windows gave an error: {0} (error {1}). So it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Khi bắt đầu quét, InstallerClean không kiểm tra được Windows Installer có đang bận không, vì Windows báo lỗi: {0} (lỗi {1}). Vì vậy InstallerClean đã giữ lại các tệp trong {InstallerFolder} không được nêu tên trong bản ghi của bất kỳ chương trình đã cài nào, thay vì đề xuất chúng. Hãy khởi động lại Windows rồi chạy lại lệnh. |
| Windows Installer had something in progress or waiting when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again once that has cleared. | Windows Installer có việc gì đó đang chạy hoặc đang chờ khi bắt đầu quét, nên InstallerClean đã giữ lại các tệp trong {InstallerFolder} không được nêu tên trong bản ghi của bất kỳ chương trình đã cài nào, thay vì đề xuất chúng. Hãy chạy lại lệnh khi việc đó đã xong xuôi. |
| This PC's clock went back or lost time while the scan was running, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again. | Đồng hồ của PC này đã lùi lại hoặc chạy chậm đi trong lúc quét, nên InstallerClean đã giữ lại các tệp trong {InstallerFolder} không được nêu tên trong bản ghi của bất kỳ chương trình đã cài nào, thay vì đề xuất chúng. Hãy chạy lại lệnh. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | Lỗi: InstallerClean không lấy được khóa Windows Installer vốn ngăn hai chương trình cùng lúc thay đổi phần mềm đã cài, nên không thể loại trừ khả năng một tệp trở nên cần thiết giữa chừng. Không có gì bị xóa. Hãy thử lại, và khởi động lại Windows nếu việc này cứ tiếp diễn. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. Try again, and restart Windows if it keeps happening. | Lỗi: InstallerClean không lấy được khóa Windows Installer vốn ngăn hai chương trình cùng lúc thay đổi phần mềm đã cài, nên không thể loại trừ khả năng một tệp trở nên cần thiết giữa chừng. Không có gì được chuyển. Hãy thử lại, và khởi động lại Windows nếu việc này cứ tiếp diễn. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. | Lỗi: Windows đã từ chối cho InstallerClean quyền kiểm tra xem Windows Installer có đang bận không, nên nó không thể loại trừ khả năng một tệp trở nên cần thiết giữa chừng. Không có gì bị xóa. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. | Lỗi: Windows đã từ chối cho InstallerClean quyền kiểm tra xem Windows Installer có đang bận không, nên nó không thể loại trừ khả năng một tệp trở nên cần thiết giữa chừng. Không có gì được chuyển. |
| Moving {0} unneeded {1} to {2}... | Đang chuyển {0} {1} không cần thiết tới {2}... |
| Moved {0} unneeded {1}. | Đã chuyển {0} {1} không cần thiết. |
| Check that your programs still update and uninstall as normal, then delete {0}. | Hãy kiểm tra xem các chương trình của bạn vẫn cập nhật và gỡ cài đặt bình thường, rồi xóa {0}. |
| InstallerClean is free. If it helped, please donate $3: {0} | InstallerClean miễn phí. Nếu nó có ích, hãy ủng hộ 3 USD: {0} |
| Update now? It's strongly recommended to use the latest version. [{0}/{1}] | Cập nhật ngay bây giờ? Chúng tôi đặc biệt khuyên bạn dùng phiên bản mới nhất. [{0}/{1}] |
| Y | C |
| N | K |
| No answer after a minute, so InstallerClean updates without asking, as it does when nobody is there to answer. | Không có câu trả lời sau một phút, nên InstallerClean cập nhật mà không hỏi, như khi không có ai ở đó để trả lời. |
| Send an anonymous report of this run? It is saved at {0}. Type ? to see what it holds. [{1}/{2}/?] | Gửi báo cáo ẩn danh về lần chạy này? Báo cáo được lưu tại {0}. Nhập ? để xem nội dung. [{1}/{2}/?] |
| the chart at {0} | biểu đồ tại {0} |
| The report adds your result to {0}, which shows how much space people are reclaiming with InstallerClean. It also holds these technical details, to help me spot any bugs: | Báo cáo sẽ thêm kết quả của bạn vào {0}, cho thấy mọi người đang giải phóng được bao nhiêu dung lượng với InstallerClean. Báo cáo cũng chứa các chi tiết kỹ thuật sau để giúp tôi phát hiện lỗi: |
| The full report is printed below. | Toàn bộ báo cáo được in bên dưới. |
| Report sent. Thank you. | Đã gửi báo cáo. Cảm ơn bạn. |
| Report not sent. | Chưa gửi báo cáo. |
| No answer after a minute. | Không có câu trả lời sau một phút. |
| The report could not be sent. It is kept in {0}. | Không thể gửi báo cáo. Báo cáo được lưu giữ tại {0}. |
| Sending a report left waiting by an earlier run. | Đang gửi một báo cáo còn chờ từ một lần chạy trước đó. |
| The new version will scan and list what it finds, without deleting anything. To delete, run /d again afterwards. | Phiên bản mới sẽ quét và liệt kê những gì tìm thấy, không xóa gì cả. Để xóa, hãy chạy lại /d sau đó. |
| The new version will scan and list what it finds, without moving anything. To move, run /m again afterwards. | Phiên bản mới sẽ quét và liệt kê những gì tìm thấy, không di chuyển gì cả. Để di chuyển, hãy chạy lại /m sau đó. |
| InstallerClean is in use, so it can't update itself now. | InstallerClean đang được sử dụng nên không thể tự cập nhật lúc này. |
| Running {0} with InstallerClean {1} instead. | Thay vào đó, chạy {0} bằng InstallerClean {1}. |
| The newer version couldn't be run. | Không thể chạy phiên bản mới hơn. |
| Running {0} with InstallerClean {1}. It takes this copy's place once the run ends. | Chạy {0} bằng InstallerClean {1}. Khi chạy xong, phiên bản này sẽ thay thế bản hiện tại. |
| {0}: {1} | {0}: {1} |
| It needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then run InstallerClean again to update. | Phiên bản này cần .NET Framework 4.8. Hãy cài đặt nó từ Microsoft, khởi động lại PC, rồi chạy lại InstallerClean để cập nhật. |
| It's simple to undo. Move them back from {0} into {InstallerFolder} and everything will be back to how it was. | Hoàn tác rất đơn giản. Hãy chuyển chúng từ {0} trở lại {InstallerFolder} và mọi thứ sẽ như cũ. |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then run the command again. | InstallerClean không còn xác nhận được thư mục sao lưu, nên đã dừng lại. Hãy kiểm tra {0}, rồi chạy lại lệnh. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Một tiến trình InstallerClean khác đang giữ khóa một-thực-thể (GUI hoặc một lần chạy CLI khác). Mã thoát 75 (tạm thời); có thể thử lại sau. |
| Stopped after deleting {0} of {1} {2}: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 2 (partial). | Đã dừng sau khi xóa {0}/{1} {2}: InstallerClean không còn chắc chắn được rằng không có InstallerClean nào khác đang hoạt động. Hãy chạy lại cùng lệnh đó. Mã thoát 2 (một phần). |
| Stopped after moving {0} of {1} {2} to {3}: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 2 (partial). | Đã dừng sau khi chuyển {0}/{1} {2} đến {3}: InstallerClean không còn chắc chắn được rằng không có InstallerClean nào khác đang hoạt động. Hãy chạy lại cùng lệnh đó. Mã thoát 2 (một phần). |
| Stopped before deleting any file: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 75 (transient). | Đã dừng trước khi xóa bất kỳ tệp nào: InstallerClean không còn chắc chắn được rằng không có InstallerClean nào khác đang hoạt động. Hãy chạy lại cùng lệnh đó. Mã thoát 75 (tạm thời). |
| Stopped before moving any file: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 75 (transient). | Đã dừng trước khi di chuyển bất kỳ tệp nào: InstallerClean không còn chắc chắn được rằng không có InstallerClean nào khác đang hoạt động. Hãy chạy lại cùng lệnh đó. Mã thoát 75 (tạm thời). |
| InstallerClean needs administrator rights to see every program installed on this PC, and it isn't running as administrator, so it won't scan. Nothing has been changed. Run it again from an administrator prompt. | InstallerClean cần quyền quản trị viên để thấy mọi chương trình được cài trên máy này, nhưng hiện không chạy với tư cách quản trị viên, nên sẽ không quét. Không có gì bị thay đổi. Hãy chạy lại từ dấu nhắc quản trị viên. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Lưu ý: ghi vào Nhật ký sự kiện thất bại. Hãy kiểm tra quyền của nhật ký Ứng dụng hoặc Chính sách nhóm. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - dọn dẹp {InstallerFolder} |
| Removes cached .msi and .msp files that no installed program still needs. | Bỏ các tệp .msi và .msp trong bộ đệm mà không chương trình đã cài nào cần. |
| Needs an elevated (administrator) prompt and won't scan without one. | Cần dấu nhắc quản trị viên; nếu không sẽ không quét. |
| Usage: | Cách dùng: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help        Hiển thị trợ giúp (cũng nhận /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version     In ra phiên bản (cũng nhận -v) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s            Chỉ quét - liệt kê tệp không cần |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d            Xóa vĩnh viễn tệp không cần thiết |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m            Chuyển tới thư mục sao lưu |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m ĐƯỜNG_DẪN  Chuyển tới đường dẫn được chỉ định |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli giữ dấu nhắc cho tới khi xong, để một tập lệnh hoặc<br>một tác vụ theo lịch có thể chờ nó. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | Thư mục theo người dùng; tác vụ theo lịch hoặc SYSTEM: /m ĐƯỜNG_DẪN. |
| Before it scans, it checks for a newer version of InstallerClean.<br>When you run it yourself, it asks before updating, and updates if nobody<br>answers within a minute. When it runs on its own, in a scheduled task or<br>a script, it updates without asking.<br>Add /noupdate to skip the check. | Trước khi quét, công cụ kiểm tra xem có phiên bản InstallerClean mới hơn<br>không. Khi bạn tự chạy, nó hỏi trước khi cập nhật, và cập nhật nếu không<br>ai trả lời trong một phút. Khi chạy tự động, trong một tác vụ theo lịch<br>hoặc một tập lệnh, nó cập nhật mà không hỏi. Thêm /noupdate để bỏ qua<br>bước kiểm tra. |
| Exit codes: | Mã thoát: |
|   0   success: the run did what it was asked |   0   thành công: đã làm đúng việc được yêu cầu |
|   1   nothing processed: the run failed or was refused |   1   không xử lý gì: lần chạy đã thất bại hoặc bị từ chối |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   một phần: xử lý được một phần (một lỗi hoặc một Ctrl+C) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  tạm thời: một điều kiện tạm thời đã chặn lần chạy (xem thông báo) |
|   130 cancelled (Ctrl+C) |   130 đã hủy (Ctrl+C) |
