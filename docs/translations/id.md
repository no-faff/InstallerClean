# InstallerClean in Bahasa Indonesia (Indonesian)

The text of InstallerClean's interface and command-line tool in English on the left, with the Indonesian translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Indonesian can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. The translation file itself is [`Strings.id.resx`](../../src/InstallerClean.Core/Resources/Strings.id.resx). This page is generated from it by `scripts/gen-translation-table.mjs`, so do not edit it by hand.

## Window titles

| English | Bahasa Indonesia |
| --- | --- |
| InstallerClean | InstallerClean |
| About | Tentang |
| Registered files that should not be deleted | File terdaftar yang sebaiknya tidak dihapus |
| Unneeded files that are safe to delete | File tidak diperlukan yang aman dihapus |
| Confirm move | Konfirmasi pemindahan |
| Confirm delete | Konfirmasi penghapusan |
| Recycle Bin unavailable | Keranjang Sampah tidak tersedia |

## Section headings

| English | Bahasa Indonesia |
| --- | --- |
| PRODUCTS | PRODUK |
| PATCHES | PATCH |
| PRODUCT DETAILS | DETAIL PRODUK |
| MOVE LOCATION | LOKASI PEMINDAHAN |
| SAY THANKS | UCAPKAN TERIMA KASIH |

## Buttons and actions

| English | Bahasa Indonesia |
| --- | --- |
| _About | _Tentang |
| Copy | Salin |
| Cut | Potong |
| Paste | Tempel |
| Select all | Pilih semua |
| _Browse... | Te_lusuri... |
| _Cancel | _Batal |
| Check for _updates | Periksa pem_baruan |
| _Close | _Tutup |
| _Delete | _Hapus |
| _Delete permanently | _Hapus permanen |
| _Done | _Selesai |
| Details | Detail |
| _Buy me a cuppa | Traktir _kopi |
| Leave a _star on GitHub | Beri _bintang di GitHub |
| Apache 2.0 licence | Lisensi Apache 2.0 |
| _Move | _Pindahkan |
| _Move instead | _Pindahkan saja |
| Path to folder if you Move instead of Delete | Jalur folder jika Anda memilih Pindahkan, bukan Hapus |
| Open _release page | Buka halaman _rilis |
| _Re-scan | Pindai _ulang |
| _Scan again | Pindai _lagi |
| Send report | Kirim laporan |
| _Send | _Kirim |

## About window

| English | Bahasa Indonesia |
| --- | --- |
| Guide and FAQ | Panduan dan FAQ |
| Report a problem | Laporkan masalah |
| Check for updates automatically | Periksa pembaruan secara otomatis |

## Field labels

| English | Bahasa Indonesia |
| --- | --- |
| Reason | Alasan |
| Author | Pembuat |
| Application | Aplikasi |
| Title | Judul |
| Subject | Subjek |
| Keywords | Kata kunci |
| Signing certificate | Sertifikat penandatanganan |
| File size | Ukuran file |
| Comment | Komentar |
| Product name | Nama produk |
| File | File |
| Size | Ukuran |
| Patches | Patch |
| (unknown) | (tidak diketahui) |
| (patches only) | (patch saja) |
| missing | hilang |

## Status and progress

| English | Bahasa Indonesia |
| --- | --- |
| Scanning... | Memindai... |
| Cancelling... | Membatalkan... |
| Starting scan... | Memulai pemindaian... |
| Asking Windows about installed software... | Menanyai Windows tentang perangkat lunak yang terpasang... |
| Scanning installer cache folder... | Memindai folder cache penginstal... |
| Enumerating installed products... | Mendata produk yang terpasang... |
| Checking registry for additional packages... | Memeriksa registri untuk paket tambahan... |
| Found {0} registered {1}. | Ditemukan {0} {1} terdaftar. |
| Scan complete ({0}) | Pemindaian selesai ({0}) |
| Scanning local packages... | Memindai paket lokal... |
| Found {0} {1} you can safely delete. | Ditemukan {0} {1} yang aman Anda hapus. |
| Preparing destination folder... | Menyiapkan folder tujuan... |
| Checking the Recycle Bin... | Memeriksa Keranjang Sampah... |
| Moving {0} {1}... | Memindahkan {0} {1}... |
| Deleting {0} {1}... | Menghapus {0} {1}... |
| Move cancelled. {0} of {1} {2} processed. | Pemindahan dibatalkan. {0} dari {1} {2} diproses. |
| Delete cancelled. {0} of {1} {2} processed. | Penghapusan dibatalkan. {0} dari {1} {2} diproses. |
| Move failed ({0}). Details in {1}. | Pemindahan gagal ({0}). Detail di {1}. |
| Move failed ({0}). The crash log could not be written. | Pemindahan gagal ({0}). Log kerusakan tidak bisa ditulis. |
| Delete failed ({0}). Details in {1}. | Penghapusan gagal ({0}). Detail di {1}. |
| Delete failed ({0}). The crash log could not be written. | Penghapusan gagal ({0}). Log kerusakan tidak bisa ditulis. |
| Access denied. Windows refused the scan. | Akses ditolak. Windows menolak pemindaian. |
| Scan failed: couldn't read the Windows Installer records. | Pemindaian gagal: catatan Windows Installer tidak bisa dibaca. |
| Scan cancelled. | Pemindaian dibatalkan. |
| Ready | Siap |
| Scan failed ({0}). Details in {1}. | Pemindaian gagal ({0}). Detail di {1}. |
| Scan failed ({0}). The crash log could not be written. | Pemindaian gagal ({0}). Log kerusakan tidak bisa ditulis. |

## Main screen text

| English | Bahasa Indonesia |
| --- | --- |
| Any unneeded files below are safe to delete. | File yang tidak diperlukan di bawah ini aman dihapus. |
| They sit in C:\Windows\Installer, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | File-file ini berada di C:\Windows\Installer, tertinggal saat sebuah program dihapus instalasinya ({0}), patch yang lebih baru menggantikan yang lama ({1}), atau penerbitnya menariknya ({2}). InstallerClean hanya pernah mencantumkan file yang Windows sendiri laporkan sudah tidak terpakai. |
| Delete them to the Recycle Bin, or use Move instead to keep a backup. Putting the files back in C:\Windows\Installer returns you to exactly where you started. | Hapus ke Keranjang Sampah, atau gunakan Pindahkan sebagai gantinya untuk menyimpan salinan cadangan. Mengembalikan file ke C:\Windows\Installer akan membuat semuanya persis seperti semula. |
| Nothing scanned yet. | Belum ada yang dipindai. |
| Press Re-scan to look through C:\Windows\Installer for installer files that no program still needs. | Tekan Pindai ulang untuk menelusuri C:\Windows\Installer mencari file penginstal yang tidak lagi diperlukan program mana pun. |
| These files can't be cleaned up right now. | File-file ini tidak bisa dibersihkan sekarang. |
| Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch the installer cache while it's changing. Once it's done, Re-scan and they come back. | Ada sesuatu yang sedang menggunakan Windows Installer saat ini, biasanya Windows Update atau program yang memasang di latar belakang. Pindahkan dan Hapus dijeda selama itu berjalan, sehingga InstallerClean tidak menyentuh cache penginstal saat sedang berubah. Setelah selesai, Pindai ulang dan keduanya kembali. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | Ada transaksi Windows Installer sebelumnya yang ditangguhkan di komputer ini. Lanjutkan atau batalkan instalasi itu (atau mulai ulang Windows) sebelum membersihkan cache. |
| Windows has a file rename queued for the next restart that affects the Installer cache. Restart Windows before cleaning. | Windows memiliki antrean penggantian nama file untuk mulai ulang berikutnya yang memengaruhi cache Installer. Mulai ulang Windows sebelum membersihkan. |
| Select a file to view details. | Pilih file untuk melihat detail. |
| Select a product to view details. | Pilih produk untuk melihat detail. |
| No metadata available. | Tidak ada metadata yang tersedia. |
| This installer file has been deleted. InstallerClean didn't do it, it never removes a file a program still needs; something else deleted this one before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. | File penginstal ini telah dihapus. InstallerClean tidak melakukannya, aplikasi ini tidak pernah menghapus file yang masih diperlukan sebuah program; sesuatu yang lain menghapus file ini sebelum Anda menjalankan InstallerClean.<br><br>File ini tidak menimbulkan masalah sekarang, dan tidak akan menimbulkannya sampai suatu hari Anda mencoba memperbaiki, memperbarui, atau menghapus instalasi program pemiliknya. Langkah itu kemudian bisa gagal, karena Windows mencari file ini dan file-nya tidak ada.<br><br>Untuk mencoba memperbaikinya, unduh penginstal program tersebut dari pembuatnya dan jalankan di atas salinan yang sudah ada (jangan menghapus instalasi terlebih dahulu, penghapusan instalasi sendiri adalah langkah yang memerlukan file ini). Gunakan versi yang Anda pasang jika bisa mendapatkannya, karena Windows mungkin menolak versi yang berbeda. Cara ini biasanya memulihkan file, dan pengaturan Anda umumnya tetap utuh, tetapi Microsoft tidak menjaminnya, langkah terakhir Microsoft sendiri adalah memasang ulang program itu, atau Windows itu sendiri. |
| The README [explains this folder], and how to recover a file, in Microsoft's own words. | README [menjelaskan folder ini], dan cara memulihkan file, dengan kata-kata Microsoft sendiri. |
| (none) | (tidak ada) |

## Reasons a file is unneeded

| English | Bahasa Indonesia |
| --- | --- |
| Orphaned | Terisolasi |
| Superseded | Digantikan |
| Obsoleted | Usang |

## Completion screen

| English | Bahasa Indonesia |
| --- | --- |
| All clean | Semua bersih |
| Nothing to clean up in C:\Windows\Installer | Tidak ada yang perlu dibersihkan di C:\Windows\Installer |
| Scanned {0} {1} in {2} | {0} {1} dipindai dalam {2} |
| Copy them back to C:\Windows\Installer if anything ever breaks ([extremely unlikely]). | Salin kembali ke C:\Windows\Installer jika suatu saat ada yang rusak ([kemungkinannya sangat kecil]). |
| Until then, you can restore them if anything ever breaks ([extremely unlikely]). | Sampai saat itu, Anda bisa memulihkannya jika suatu saat ada yang rusak ([kemungkinannya sangat kecil]). |
| Empty it to actually reclaim the space. | Kosongkan Keranjang Sampah untuk benar-benar membebaskan ruang. |
| {0} freed | {0} dikosongkan |
| {0} cleaned up | {0} dibersihkan |
| {0} moved | {0} dipindahkan |
| Nothing was moved | Tidak ada yang dipindahkan |
| Nothing was deleted | Tidak ada yang dihapus |
| {0} of {1} could not be moved. | {0} dari {1} file tidak bisa dipindahkan. |
| {0} of {1} could not be moved. | {0} dari {1} file tidak bisa dipindahkan. |
| {0} of {1} could not be deleted. | {0} dari {1} file tidak bisa dihapus. |
| {0} of {1} could not be deleted. | {0} dari {1} file tidak bisa dihapus. |
| {0} {1} moved to: {2} | {0} {1} dipindahkan ke: {2} |
| {0} {1} moved to: {2} | {0} {1} dipindahkan ke: {2} |
| {0} {1} moved to the Recycle Bin | {0} {1} dipindahkan ke Keranjang Sampah |
| {0} {1} moved to the Recycle Bin | {0} {1} dipindahkan ke Keranjang Sampah |
| {0} {1} kept in place, because a program started needing them again after the scan. | {0} {1} dibiarkan di tempatnya, karena sebuah program kembali membutuhkannya setelah pemindaian. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read when the check was repeated. | {0} {1} dibiarkan di tempatnya, karena catatan Windows Installer tidak dapat dibaca sepenuhnya saat pemeriksaan diulang. |
| Moved {0} of {1} {2} before you cancelled. | {0} dari {1} {2} dipindahkan sebelum Anda membatalkan. |
| Moved {0} of {1} {2} to the Recycle Bin before you cancelled. | {0} dari {1} {2} dipindahkan ke Keranjang Sampah sebelum Anda membatalkan. |
| Permanently deleted {0} of {1} {2} before you cancelled. | {0} dari {1} {2} dihapus permanen sebelum Anda membatalkan. |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. | {0} {1} dihapus permanen. File tidak masuk ke Keranjang Sampah. |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. | {0} {1} dihapus permanen. File tidak masuk ke Keranjang Sampah. |
| That's fine, it was safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Tidak masalah, file itu memang aman dihapus. InstallerClean hanya membersihkan file yang Windows laporkan sudah tidak terpakai, tidak pernah file yang masih diperlukan sebuah program. Pada kemungkinan kecil suatu penghapusan membuat sebuah program tidak bisa diperbaiki, diperbarui, atau dihapus instalasinya, memasangnya ulang dari pembuatnya biasanya memulihkan file tersebut, meski Microsoft tidak menjaminnya. |
| That's fine, they were safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Tidak masalah, file itu memang aman dihapus. InstallerClean hanya membersihkan file yang Windows laporkan sudah tidak terpakai, tidak pernah file yang masih diperlukan sebuah program. Pada kemungkinan kecil suatu penghapusan membuat sebuah program tidak bisa diperbaiki, diperbarui, atau dihapus instalasinya, memasangnya ulang dari pembuatnya biasanya memulihkan file tersebut, meski Microsoft tidak menjaminnya. |
| Glad to help. There's a tip jar if you're feeling kind. | Senang bisa membantu. Kalau Anda berbaik hati, secangkir kopi sangat saya hargai. |

## Recycle Bin unavailable

| English | Bahasa Indonesia |
| --- | --- |
| The Recycle Bin isn't available for this drive | Keranjang Sampah tidak tersedia untuk drive ini |
| So this {1} ({2}) hasn't been deleted. You can move it somewhere safe, or delete it permanently. | Jadi {1} ini ({2}) belum dihapus. Anda bisa memindahkannya ke tempat aman, atau menghapusnya permanen. |
| So these {0} {1} ({2}) haven't been deleted. You can move them somewhere safe, or delete them permanently. | Jadi {0} {1} ini ({2}) belum dihapus. Anda bisa memindahkannya ke tempat aman, atau menghapusnya permanen. |
| Deleting it is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Menghapusnya aman. InstallerClean hanya membersihkan file yang Windows laporkan sudah tidak terpakai, tidak pernah file yang masih diperlukan sebuah program, dan Keranjang Sampah hanyalah pengaman tambahan. Pada kemungkinan kecil suatu penghapusan membuat sebuah program tidak bisa diperbaiki, diperbarui, atau dihapus instalasinya, memasangnya ulang dari pembuatnya biasanya memulihkan file tersebut, meski Microsoft tidak menjaminnya. |
| Deleting them is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Menghapusnya aman. InstallerClean hanya membersihkan file yang Windows laporkan sudah tidak terpakai, tidak pernah file yang masih diperlukan sebuah program, dan Keranjang Sampah hanyalah pengaman tambahan. Pada kemungkinan kecil suatu penghapusan membuat sebuah program tidak bisa diperbaiki, diperbarui, atau dihapus instalasinya, memasangnya ulang dari pembuatnya biasanya memulihkan file tersebut, meski Microsoft tidak menjaminnya. |

## Summaries and counts

| English | Bahasa Indonesia |
| --- | --- |
| {0} file still needed | {0} file masih diperlukan |
| {0} files still needed | {0} file masih diperlukan |
| {0} unneeded file to clean up | {0} file tidak diperlukan untuk dibersihkan |
| {0} unneeded files to clean up | {0} file tidak diperlukan untuk dibersihkan |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | {0} file terdaftar hilang (bukan dihapus oleh InstallerClean). Tidak masalah sekarang, tetapi perbaikan, pembaruan, atau penghapusan instalasi program itu di kemudian hari bisa gagal. Buka Detail untuk tahu apa yang harus dilakukan. |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | {0} file terdaftar hilang (bukan dihapus oleh InstallerClean). Tidak masalah sekarang, tetapi perbaikan, pembaruan, atau penghapusan instalasi program-program itu di kemudian hari bisa gagal. Buka Detail untuk tahu apa yang harus dilakukan. |
| {0} installed program could not be read during this scan, so superseded patches have been kept. Orphaned files are not affected. | {0} program terpasang tidak dapat dibaca selama pemindaian ini, jadi patch yang digantikan tetap dipertahankan. File yang terisolasi tidak terpengaruh. |
| {0} installed programs could not be read during this scan, so superseded patches have been kept. Orphaned files are not affected. | {0} program terpasang tidak dapat dibaca selama pemindaian ini, jadi patch yang digantikan tetap dipertahankan. File yang terisolasi tidak terpengaruh. |
| {0} of {1} {2} | {0} dari {1} {2} |
| {0} orphaned, {1} superseded, {2} obsoleted ({3}) | {0} terisolasi, {1} digantikan, {2} usang ({3}) |
| {0} registered file that is still needed ({1}) | {0} file terdaftar yang masih diperlukan ({1}) |
| {0} registered files that are still needed ({1}) | {0} file terdaftar yang masih diperlukan ({1}) |

## Confirmation dialogs

| English | Bahasa Indonesia |
| --- | --- |
| Move {0} {1} ({2})? | Pindahkan {0} {1} ({2})? |
| Files will be moved to: | File akan dipindahkan ke: |
| Delete {0} {1} ({2})? | Hapus {0} {1} ({2})? |
| Files will be moved to the Recycle Bin. If you'd like backup copies, use the Move button instead. | File akan dipindahkan ke Keranjang Sampah. Jika Anda ingin salinan cadangan, gunakan tombol Pindahkan sebagai gantinya. |
| This folder is on the same drive, so the move won't free any space by itself. You'll get the space back when you delete the files from it, or you can pick a folder on another drive instead. | Folder ini berada di drive yang sama, jadi pemindahan itu sendiri tidak akan mengosongkan ruang apa pun. Ruangnya akan kembali saat Anda menghapus file di dalamnya, atau Anda bisa memilih folder di drive lain. |

## Error messages

| English | Bahasa Indonesia |
| --- | --- |
| Access denied | Akses ditolak |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows menolak akses untuk InstallerClean, jadi prosesnya dihentikan. Tidak ada yang dihapus.<br><br>InstallerClean sudah berjalan sebagai administrator, jadi menjalankannya lagi dengan cara itu tidak akan membantu. Windows tidak menjelaskan lebih jauh apa yang menolak akses, jadi tidak ada hal khusus yang bisa dicoba. |
| Couldn't read the Windows Installer records | Catatan Windows Installer tidak bisa dibaca |
| Scan failed | Pemindaian gagal |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in C:\Windows\Installer orphaned. InstallerClean stopped instead. Nothing has been removed. | Catatan Windows Installer kembali sepenuhnya kosong: tidak satu pun program terpasang atau pembaruan yang mengklaim file pemasang di cache. Itu tidak terjadi pada komputer yang berfungsi (bahkan pemasangan Windows yang baru pun punya beberapa), jadi catatannya rusak atau tidak bisa dibaca, dan pemindaian yang memercayai jawaban ini akan keliru menyebut setiap file di C:\Windows\Installer terisolasi. InstallerClean berhenti sebagai gantinya. Tidak ada yang dihapus. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer tidak mengizinkan InstallerClean menampilkan daftar apa saja yang terpasang. InstallerClean sudah berjalan sebagai administrator, jadi menjalankannya lagi sebagai administrator tidak akan mengubah apa pun. Tanpa daftar itu tidak ada cara yang aman untuk mengetahui file cache mana yang masih diperlukan, jadi InstallerClean berhenti. Tidak ada yang dihapus. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer tidak bisa memberi InstallerClean daftar program terpasang yang terbaca: {0} entri berturut-turut kembali tidak terbaca (kode kesalahan terakhir {1}). Alih-alih bekerja dengan daftar yang hanya terbaca sebagian, InstallerClean berhenti. Tidak ada yang dihapus. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer tidak pernah menandai akhir daftar program terpasang: InstallerClean menyerah setelah {0} entri (kode kesalahan terakhir {1}). Daftar yang tidak berujung tidak bisa dipercaya, jadi InstallerClean berhenti. Tidak ada yang dihapus. |
| Windows Installer couldn't give InstallerClean a readable list of one program's patches: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer tidak bisa memberi InstallerClean daftar patch sebuah program yang terbaca: {0} entri berturut-turut kembali tidak terbaca (kode kesalahan terakhir {1}). Alih-alih bekerja dengan daftar yang hanya terbaca sebagian, InstallerClean berhenti. Tidak ada yang dihapus. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer tidak pernah menandai akhir daftar patch sebuah program: InstallerClean menyerah setelah {0} entri (kode kesalahan terakhir {1}). Daftar yang tidak berujung tidak bisa dipercaya, jadi InstallerClean berhenti. Tidak ada yang dihapus. |
| InstallerClean couldn't square this scan with the Windows Installer records: every file Windows still lists as needed is missing from C:\Windows\Installer, while the files actually in the folder match nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. | InstallerClean tidak bisa mencocokkan pemindaian ini dengan catatan Windows Installer: setiap file yang masih didaftarkan Windows sebagai diperlukan tidak ada di C:\Windows\Installer, sementara file yang benar-benar ada di folder itu tidak cocok dengan catatan mana pun. Tidak ada komputer nyata yang seperti itu, jadi ini menunjukkan masalah dalam membaca catatan, bukan file yang aman Anda hapus. Tidak ada yang ditawarkan untuk dibersihkan dan tidak ada yang dihapus. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean tidak bisa membaca cukup banyak catatan Windows Installer untuk memastikan apa yang masih diperlukan: daftar program terpasang kembali tidak lengkap, dan membaca catatan yang sama langsung dari registri juga menemui kesalahan. Sebuah file bisa tampak terisolasi hanya karena catatan yang menyebutkannya termasuk yang tidak terbaca, jadi InstallerClean berhenti. Tidak ada yang dihapus. |
| Invalid destination | Tujuan tidak valid |
| Could not write to destination | Tidak bisa menulis ke tujuan |
| Move failed | Pemindahan gagal |
| Delete failed | Penghapusan gagal |
| The destination cannot be inside the Windows Installer folder. | Tujuan tidak boleh berada di dalam folder Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | Tujuan {0} mengarah ke dalam folder sistem Windows. Pilih jalur di luar %SystemRoot%, %ProgramFiles%, dan %ProgramData%. |
| Not enough space | Ruang tidak cukup |
| Not enough space at {0}<br><br>Required: {1}<br>Available: {2} | Ruang tidak cukup di {0}<br><br>Diperlukan: {1}<br>Tersedia: {2} |
| You don't have permission to write to {0}.<br>Try a folder in your user profile or on a drive you own. | Anda tidak punya izin untuk menulis ke {0}.<br>Coba folder di profil pengguna Anda atau di drive milik Anda sendiri. |
| The path {0} is too long for Windows. Pick a shorter path. | Jalur {0} terlalu panjang untuk Windows. Pilih jalur yang lebih pendek. |
| The folder {0} does not exist and could not be created. Check the drive letter or network path. | Folder {0} tidak ada dan tidak bisa dibuat. Periksa huruf drive atau jalur jaringan. |
| Windows cannot write to {0}.<br>Details in {1}. | Windows tidak bisa menulis ke {0}.<br>Detail di {1}. |
| Windows cannot write to {0}. The crash log could not be written. | Windows tidak bisa menulis ke {0}. Log kerusakan tidak bisa ditulis. |
| Cannot write to {0}.<br>Details in {1}. | Tidak bisa menulis ke {0}.<br>Detail di {1}. |
| Cannot write to {0}. The crash log could not be written. | Tidak bisa menulis ke {0}. Log kerusakan tidak bisa ditulis. |
| File no longer exists. | File sudah tidak ada lagi. |
| Source file is a symlink or junction; refused for safety. | File sumber adalah symlink atau junction; ditolak demi keamanan. |
| This file is not directly inside the Windows Installer folder; refused for safety. | File ini tidak berada langsung di dalam folder Windows Installer; ditolak demi keamanan. |
| Windows refused access to this file; it was left in place. | Windows menolak akses ke file ini; file dibiarkan di tempatnya. |
| Windows refused access to these files; they were left in place. | Windows menolak akses ke file-file ini; semuanya dibiarkan di tempatnya. |
| This file is open or locked by another program, so nothing can move it just now. It was left in place; try again later. | File ini sedang dibuka atau dikunci oleh program lain, jadi saat ini tidak ada yang bisa memindahkannya. File dibiarkan di tempatnya; coba lagi nanti. |
| These files are open or locked by another program, so nothing can move them just now. They were left in place; try again later. | File-file ini sedang dibuka atau dikunci oleh program lain, jadi saat ini tidak ada yang bisa memindahkannya. Semuanya dibiarkan di tempatnya; coba lagi nanti. |
| Windows reported a file error; the file was left in place. | Windows melaporkan kesalahan file; file dibiarkan di tempatnya. |
| Windows reported file errors; these files were left in place. | Windows melaporkan kesalahan file; file-file ini dibiarkan di tempatnya. |
| Something went wrong with this file; it was left in place. | Ada yang tidak beres dengan file ini; file dibiarkan di tempatnya. |
| Something went wrong with these files; they were left in place. | Ada yang tidak beres dengan file-file ini; semuanya dibiarkan di tempatnya. |
| Couldn't move this file to the Recycle Bin (error {0}), and InstallerClean can't tell you why from that code. The file was left in place. Try the Move button instead, since it doesn't use the Recycle Bin. | Tidak bisa memindahkan file ini ke Keranjang Sampah (kesalahan {0}), dan dari kode itu InstallerClean tidak bisa memberi tahu Anda alasannya. File dibiarkan di tempatnya. Coba tombol Pindahkan saja, karena tombol itu tidak memakai Keranjang Sampah. |
| Windows refused access even with administrator rights (error {0}), and InstallerClean can't tell whether the problem is the file or the Recycle Bin. The file was left in place. The Move button will work if it's the Recycle Bin, but not if it's the file. | Windows menolak akses bahkan dengan hak administrator (kesalahan {0}), dan InstallerClean tidak bisa memastikan apakah masalahnya ada pada file atau pada Keranjang Sampah. File dibiarkan di tempatnya. Tombol Pindahkan akan berhasil jika masalahnya Keranjang Sampah, tetapi tidak jika masalahnya file itu sendiri. |
| This file is open or locked by another program (error {0}), so nothing can remove it just now. It was left in place; try again later. | File ini sedang dibuka atau dikunci oleh program lain (kesalahan {0}), jadi saat ini tidak ada yang bisa menghapusnya. File dibiarkan di tempatnya; coba lagi nanti. |
| Windows deleted this file outright rather than moving it to the Recycle Bin. InstallerClean asked for the Recycle Bin, and Windows did this instead. The file is gone. | Windows menghapus file ini secara permanen alih-alih memindahkannya ke Keranjang Sampah. InstallerClean meminta Keranjang Sampah, dan Windows justru melakukan ini. File itu sudah hilang. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Menolak memindahkan file ke dalam folder Windows Installer (tujuan: {0}). |
| The Move location needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | Lokasi pemindahan harus berupa jalur lengkap ke sebuah folder, yang dimulai dengan huruf drive atau jalur jaringan (misalnya D:\Backup, atau \\server\backup). InstallerClean tidak bisa memakai yang ini: {0} |
| The Move location changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | Lokasi pemindahan berubah saat file sedang dipindahkan (ada sesuatu yang mengganti atau mengalihkan folder itu), jadi InstallerClean berhenti daripada menulis ke tempat yang salah. Periksa {0}, lalu Pindai ulang dan coba lagi. |
| Cannot write to {0}. | Tidak bisa menulis ke {0}. |
| Could not find a unique filename for '{0}' after 10,000 attempts. | Tidak bisa menemukan nama file yang unik untuk '{0}' setelah 10.000 percobaan. |

## Update check

| English | Bahasa Indonesia |
| --- | --- |
| Check for updates | Periksa pembaruan |
| Checking... | Memeriksa... |
| Up to date. | Sudah versi terbaru. |
| Version {0} is available. | Versi {0} tersedia. |
| Update available | Pembaruan tersedia |
| You're running version {0}.<br>Version {1} is available. | Anda menjalankan versi {0}.<br>Versi {1} tersedia. |
| Couldn't reach GitHub. Check your internet connection and try again. | Tidak bisa menjangkau GitHub. Periksa koneksi internet Anda dan coba lagi. |
| GitHub returned an error response. The releases API may be rate-limited; try again in a few minutes. | GitHub mengembalikan respons kesalahan. API rilis mungkin terkena batas laju; coba lagi beberapa menit lagi. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | Respons GitHub tidak memuat rilis yang dikenali. Coba lagi nanti, atau buka halaman rilis langsung. |
| The check timed out. Your connection to GitHub may be slow; try again. | Pemeriksaan kehabisan waktu. Koneksi Anda ke GitHub mungkin lambat; coba lagi. |
| The check failed for an unknown reason. Details are in crash.log if you need to report it. | Pemeriksaan gagal karena alasan yang tidak diketahui. Detailnya ada di crash.log jika Anda perlu melaporkannya. |

## Opening links in your browser

| English | Bahasa Indonesia |
| --- | --- |
| Couldn't open your browser | Tidak bisa membuka peramban Anda |
| InstallerClean couldn't open your browser. The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | InstallerClean tidak bisa membuka peramban Anda. Tautannya sudah ada di papan klip, jadi Anda bisa menempelkannya sendiri:<br><br>{0} |
| InstallerClean couldn't open your browser, and couldn't copy the link to your clipboard either. The link is:<br><br>{0} | InstallerClean tidak bisa membuka peramban Anda, dan juga tidak bisa menyalin tautan ke papan klip. Tautannya:<br><br>{0} |

## Sending the summary

| English | Bahasa Indonesia |
| --- | --- |
| Sending... | Mengirim... |
| Thanks! Report sent. | Terima kasih! Laporan terkirim. |
| Sending failed. Try again later. | Pengiriman gagal. Coba lagi nanti. |
| No report to send. | Tidak ada laporan untuk dikirim. |
| Send this? | Kirim ini? |
| It goes to nofaff.netlify.app/api/result-log. Nothing identifies you or your machine; it just lets me know InstallerClean's working and [how much space people are freeing]. | Dikirim ke nofaff.netlify.app/api/result-log. Tidak ada yang mengidentifikasi Anda atau komputer Anda; ini hanya memberi tahu saya bahwa InstallerClean berfungsi dan [berapa banyak ruang yang dikosongkan orang-orang]. |

## Startup and crashes

| English | Bahasa Indonesia |
| --- | --- |
| InstallerClean | InstallerClean |
| InstallerClean is already running. | InstallerClean sudah berjalan. |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | Terjadi kesalahan tak terduga dan InstallerClean perlu ditutup.<br><br>{0}<br><br>Detail ditulis ke:<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | Terjadi kesalahan tak terduga dan InstallerClean perlu ditutup.<br><br>{0}<br><br>Log kerusakan tidak bisa ditulis. |
| Startup error | Kesalahan saat memulai |
| Failed to start ({0}). Details written to:<br>{1} | Gagal memulai ({0}). Detail ditulis ke:<br>{1} |
| Failed to start ({0}). The crash log could not be written. | Gagal memulai ({0}). Log kerusakan tidak bisa ditulis. |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Redact both classes of detail before attaching this file to a<br># public bug report.<br> | # crash.log menangkap pengecualian tak tertangani dari InstallerClean.<br># Dalam mode dengan hak akses tinggi, pesan pengecualian framework bisa<br># memuat jalur file dari sesi yang berjalan (termasuk profil pengguna<br># lain yang didata oleh kueri Windows Installer). Pesan kegagalan<br># jaringan dari pemeriksaan pembaruan atau POST log hasil bisa memuat<br># URL tujuan dan alamat IP / proksi yang teresolusi. Hapus kedua jenis<br># detail ini sebelum melampirkan file ini ke laporan bug publik.<br> |

## Tooltips (hover text)

| English | Bahasa Indonesia |
| --- | --- |
| It's thirsty work! | Membuat haus! |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Pembatalan diminta. InstallerClean sedang menunggu langkah yang berjalan mencapai titik berhenti. Ini bisa memakan waktu beberapa detik saat I/O berat atau panggilan basis data MSI. |
| Close | Tutup |
| A GitHub star helps other people find it. | Bintang di GitHub membantu orang lain menemukan InstallerClean. |
| Minimise | Kecilkan |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | Terserah Anda, tapi sangat dihargai. Mengirim ringkasan anonim yang sekadar memberi tahu saya apakah aplikasi berfungsi dan berapa banyak ruang yang dikosongkan orang-orang. Layar berikutnya memperlihatkan apa yang akan dikirim sebelum Anda mengonfirmasi. |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | Terserah Anda, tapi sangat dihargai. Mengirim ringkasan anonim yang sekadar memberi tahu saya apakah aplikasi berfungsi. Layar berikutnya memperlihatkan apa yang akan dikirim sebelum Anda mengonfirmasi. |
| Move the unneeded files to the Move location. | Pindahkan file yang tidak diperlukan ke lokasi pemindahan. |
| Move the unneeded files somewhere safe. You'll choose the folder next. | Pindahkan file yang tidak diperlukan ke tempat aman. Anda akan memilih foldernya setelah ini. |
| Move the unneeded files to the Recycle Bin. | Pindahkan file yang tidak diperlukan ke Keranjang Sampah. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Nama subjek dari sertifikat Authenticode yang disematkan. Rantai sertifikat tidak diverifikasi. |
| Change language. The program will restart. | Ganti bahasa. Program akan dimulai ulang. |

## Screen reader labels

| English | Bahasa Indonesia |
| --- | --- |
| Donate | Donasi |
| Buy me a cuppa (About window) | Traktir saya secangkir kopi (jendela Tentang) |
| Cancel operation | Batalkan operasi |
| Cancel scan | Batalkan pemindaian |
| Cancel startup scan | Batalkan pemindaian awal |
| Close | Tutup |
| Close window | Tutup jendela |
| Close result and return to main window | Tutup hasil dan kembali ke jendela utama |
| Leave a star on GitHub (About window) | Beri bintang di GitHub (jendela Tentang) |
| Minimise | Kecilkan |
| Move all unneeded installer files to the Move location | Pindahkan semua file penginstal yang tidak diperlukan ke lokasi pemindahan |
| Move all unneeded installer files to the Recycle Bin | Pindahkan semua file penginstal yang tidak diperlukan ke Keranjang Sampah |
| Delete moves the unneeded files to the Recycle Bin. Cancel closes without deleting. | Hapus memindahkan file yang tidak diperlukan ke Keranjang Sampah. Batal menutup tanpa menghapus. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Pindahkan menaruh file yang tidak diperlukan di folder tujuan yang dipilih. Batal membiarkannya di tempatnya. |
| Choose how to handle the unneeded files: move them somewhere safe, delete them permanently or cancel. | Pilih cara menangani file yang tidak diperlukan: pindahkan ke tempat aman, hapus permanen, atau batalkan. |
| Move the unneeded files to a folder you choose | Pindahkan file yang tidak diperlukan ke folder pilihan Anda |
| Delete the unneeded files permanently because the Recycle Bin is unavailable for this drive | Hapus permanen file yang tidak diperlukan karena Keranjang Sampah tidak tersedia untuk drive ini |
| Posts to nofaff.netlify.app. Counts and labels only. You will see the exact payload before sending. | Dikirim ke nofaff.netlify.app. Hanya jumlah dan label. Anda akan melihat data persis yang akan dikirim sebelum mengirim. |
| Say thanks | Ucapkan terima kasih |
| Send posts the report shown to No Faff. Cancel sends nothing. | Kirim mengirimkan laporan yang ditampilkan ke No Faff. Batal tidak mengirim apa pun. |
| Check for updates | Periksa pembaruan |
| Checks the GitHub releases API over HTTPS for a newer version. | Memeriksa API rilis GitHub melalui HTTPS untuk versi yang lebih baru. |
| Opens the guide (README) on github.com in your browser. | Membuka panduan (README) di github.com melalui peramban Anda. |
| Opens the issue tracker on github.com in your browser. | Membuka pelacak masalah (Issues) di github.com melalui peramban Anda. |
| When ticked, InstallerClean checks GitHub for a newer version when you run it. | Jika dicentang, InstallerClean memeriksa GitHub untuk versi yang lebih baru setiap kali Anda menjalankannya. |
| Open the release page to download the newer version, or cancel to keep the current version. | Buka halaman rilis untuk mengunduh versi yang lebih baru, atau batalkan untuk tetap memakai versi saat ini. |
| Apache 2.0 licence | Lisensi Apache 2.0 |
| Opens the licence file on github.com in your browser. | Membuka file lisensi di github.com melalui peramban Anda. |
| Move location | Lokasi pemindahan |
| Products | Produk |
| Patches | Patch |
| Product details | Detail produk |
| Move location | Lokasi pemindahan |
| Operation progress | Kemajuan operasi |
| Scan C:\Windows\Installer again | Pindai ulang C:\Windows\Installer |
| Scanning progress | Kemajuan pemindaian |
| Startup scan progress | Kemajuan pemindaian awal |
| Details, unneeded files | Detail, file yang tidak diperlukan |
| Available for cleanup. | Tersedia untuk dibersihkan. |
| Details, registered files | Detail, file terdaftar |
| Read-only inventory. | Daftar baca-saja. |
| Sorted by {0}, ascending | Diurutkan berdasarkan {0}, menaik |
| Sorted by {0}, descending | Diurutkan berdasarkan {0}, menurun |
| Scan results | Hasil pemindaian |
| Result details | Detail hasil |
| File details | Detail file |
| Dialog text | Teks dialog |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | File yang tidak dapat diproses |
| Explains this folder, and how to recover a file, in the README | Menjelaskan folder ini, dan cara memulihkan file, di README |
| Report preview | Pratinjau laporan |
| Change language | Ganti bahasa |
| The program will restart. | Program akan dimulai ulang. |

## File picker

| English | Bahasa Indonesia |
| --- | --- |
| Choose destination folder for moved files | Pilih folder tujuan untuk file yang dipindahkan |

## Version

| English | Bahasa Indonesia |
| --- | --- |
| Version {0} | Versi {0} |

## Word forms (singular and plural)

| English | Bahasa Indonesia |
| --- | --- |
| file | file |
| files | file |
| error | kesalahan |
| errors | kesalahan |
| package | paket |
| packages | paket |
| product | produk |
| products | produk |
| patch | patch |
| patches | patch |

## Sizes and times

| English | Bahasa Indonesia |
| --- | --- |
| {0:F2} GB | {0:F2} GB |
| {0:F1} MB | {0:F1} MB |
| {0:F1} KB | {0:F1} KB |
| {0} B | {0} B |
| {0:F0}ms | {0:F0}ms |
| {0:F1}s | {0:F1}s |
| less than a second | kurang dari satu detik |
| {0:F1} seconds | {0:F1} detik |

## Command-line tool (installerclean-cli)

| English | Bahasa Indonesia |
| --- | --- |
| Unknown argument: '{0}' | Argumen tidak dikenal: '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Kesalahan: argumen tambahan yang tidak terduga '{0}'. Jika folder pemindahan Anda mengandung spasi, apit seluruh jalur dengan tanda kutip: /m "D:\My Backup" |
| Cancelling... | Membatalkan... |
| Cancelled. | Dibatalkan. |
| Error: {0}. Details written to {1}. | Kesalahan: {0}. Detail ditulis ke {1}. |
| Error: {0}. The crash log could not be written. | Kesalahan: {0}. Log kerusakan tidak bisa ditulis. |
| Scanning C:\Windows\Installer... | Memindai C:\Windows\Installer... |
| Found {0} {1} to clean up ({2}). | Ditemukan {0} {1} untuk dibersihkan ({2}). |
| Nothing to do. | Tidak ada yang perlu dilakukan. |
| Deleting {0} {1}... | Menghapus {0} {1}... |
| Deleted {0} {1}. | {0} {1} dihapus. |
| Error: the Recycle Bin is unavailable for this volume, so nothing was deleted. Use /m to move the files instead, or re-enable the Recycle Bin and run again. | Kesalahan: Keranjang Sampah tidak tersedia untuk volume ini, jadi tidak ada yang dihapus. Gunakan /m untuk memindahkan file, atau aktifkan kembali Keranjang Sampah dan jalankan lagi. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Kesalahan: tujuan pemindahan tidak ditentukan. Gunakan /m JALUR. (Default yang diatur di GUI bersifat per-pengguna dan tidak berlaku untuk tugas terjadwal atau proses akun layanan.) |
| Error: destination cannot be inside the Windows Installer folder. | Kesalahan: tujuan tidak boleh berada di dalam folder Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Kesalahan: tujuan harus berupa jalur absolut lengkap. Diterima: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | Kesalahan: tujuan {0} mengarah ke dalam folder sistem Windows. Pilih jalur di luar %SystemRoot%, %ProgramFiles%, dan %ProgramData%. |
| Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are blocked while that runs. Try again once it finishes. | Kesalahan: ada sesuatu yang sedang menggunakan Windows Installer saat ini, biasanya Windows Update atau program yang memasang di latar belakang. Pindahkan dan Hapus diblokir selama itu berjalan. Coba lagi setelah selesai. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | Kesalahan: ada transaksi Windows Installer sebelumnya yang ditangguhkan di komputer ini. Lanjutkan atau batalkan instalasi itu (atau mulai ulang Windows) sebelum membersihkan cache. |
| Error: a queued post-reboot file operation targets the Installer cache ({0}). Restart Windows to complete that operation before cleaning. | Kesalahan: operasi file yang diantrekan setelah mulai ulang menyasar cache Installer ({0}). Mulai ulang Windows untuk menyelesaikan operasi itu sebelum membersihkan. |
| Moving {0} {1} to {2}... | Memindahkan {0} {1} ke {2}... |
| Moved {0} {1}. | {0} {1} dipindahkan. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Proses InstallerClean lain memegang kunci instans-tunggal (GUI atau proses CLI lain). Kode keluar 75 (sementara); aman untuk dicoba lagi nanti. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Catatan: penulisan ke Log Peristiwa gagal. Periksa izin log Aplikasi atau Kebijakan Grup. |
| InstallerClean - clean up C:\Windows\Installer | InstallerClean - pembersihan C:\Windows\Installer |
| Usage: | Penggunaan: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     Tampilkan bantuan ini (juga menerima /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  Cetak versi (juga menerima -v) |
|   installerclean-cli /s         Scan only - list removable files |   installerclean-cli /s         Pindai saja - daftar file tidak diperlukan |
|   installerclean-cli /d         Delete removable files (Recycle Bin) |   installerclean-cli /d         Hapus file tidak diperlukan (Keranjang Sampah) |
|   installerclean-cli /m         Move to saved default location |   installerclean-cli /m         Pindahkan ke lokasi default tersimpan |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m JALUR   Pindahkan ke jalur yang ditentukan |
| installerclean-cli is a real console process and blocks the prompt | installerclean-cli adalah proses konsol sungguhan dan memblokir prompt |
| until it finishes; redirect or pipe its output as you would any | sampai selesai; alihkan atau salurkan keluarannya seperti |
| other console exe. The GUI lives in InstallerClean.exe alongside it. | file exe konsol lainnya. GUI ada di InstallerClean.exe di sebelahnya. |
| The saved default is per-user; scheduled or SYSTEM runs need /m PATH. | Lokasi default tersimpan bersifat per-pengguna; tugas terjadwal atau proses SYSTEM memerlukan /m JALUR. |
| Exit codes: | Kode keluar: |
|   0   success: every flagged file was processed |   0   berhasil: setiap file yang ditandai telah diproses |
|   1   failure: nothing processed (bad args, scan failed, all files failed) |   1   gagal: tidak ada yang diproses (argumen salah, pemindaian gagal, semua file gagal) |
|   2   partial: some files processed, some failed |   2   sebagian: sebagian file diproses, sebagian gagal |
|   75  transient: a temporary condition blocked the run (see the message) |   75  sementara: kondisi sementara memblokir proses (lihat pesannya) |
|   130 cancelled (Ctrl+C) |   130 dibatalkan (Ctrl+C) |
