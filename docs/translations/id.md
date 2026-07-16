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
| Check for _updates | Periksa _pembaruan |
| _Close | _Tutup |
| _Delete | _Hapus |
| _Delete permanently | _Hapus permanen |
| _Done | _Selesai |
| Details | Detail |
| _Buy me a cuppa | Traktir _kopi |
| Leave a _star on GitHub | Beri _bintang di GitHub |
| MIT licence | Lisensi MIT |
| _Move | _Pindahkan |
| _Move instead | _Pindahkan saja |
| Path to folder if you Move instead of Delete | Jalur folder jika Anda memilih Pindahkan, bukan Hapus |
| Open _release page | Buka halaman _rilis |
| _Re-scan | Pindai _ulang |
| _Scan again | Pindai _lagi |
| Send report | Kirim laporan |
| _Send | _Kirim |

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
| Moving {0} {1}... | Memindahkan {0} {1}... |
| Deleting {0} {1}... | Menghapus {0} {1}... |
| Move cancelled. {0} of {1} {2} processed. | Pemindahan dibatalkan. {0} dari {1} {2} diproses. |
| Delete cancelled. {0} of {1} {2} processed. | Penghapusan dibatalkan. {0} dari {1} {2} diproses. |
| Move failed ({0}). Details in {1}. | Pemindahan gagal ({0}). Detail di {1}. |
| Move failed ({0}). The crash log could not be written. | Pemindahan gagal ({0}). Log kerusakan tidak bisa ditulis. |
| Delete failed ({0}). Details in {1}. | Penghapusan gagal ({0}). Detail di {1}. |
| Delete failed ({0}). The crash log could not be written. | Penghapusan gagal ({0}). Log kerusakan tidak bisa ditulis. |
| Access denied. Windows refused the scan. | Akses ditolak. Windows menolak pemindaian. |
| Scan failed: installer database unavailable. | Pemindaian gagal: basis data penginstal tidak tersedia. |
| Scan cancelled. | Pemindaian dibatalkan. |
| Ready | Siap |
| Scan failed ({0}). Details in {1}. | Pemindaian gagal ({0}). Detail di {1}. |
| Scan failed ({0}). The crash log could not be written. | Pemindaian gagal ({0}). Log kerusakan tidak bisa ditulis. |

## Main screen text

| English | Bahasa Indonesia |
| --- | --- |
| The unneeded files below are safe to delete. | File yang tidak diperlukan di bawah ini aman dihapus. |
| They sit in C:\Windows\Installer, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | File-file ini berada di C:\Windows\Installer, tertinggal saat sebuah program dihapus instalasinya ({0}), patch yang lebih baru menggantikan yang lama ({1}), atau penerbitnya menariknya ({2}). InstallerClean hanya pernah mencantumkan file yang Windows sendiri laporkan sudah tidak terpakai. |
| Delete them to the Recycle Bin, or use Move instead if you'd rather keep a copy. | Hapus ke Keranjang Sampah, atau gunakan Pindahkan sebagai gantinya jika Anda lebih suka menyimpan salinan. |
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
| Copy them back if anything breaks ([it won't!]). | Salin kembali jika ada yang rusak ([tidak akan!]). |
| Until then, you can restore them if anything breaks ([it won't!]). | Sampai saat itu, Anda bisa memulihkannya jika ada yang rusak ([tidak akan!]). |
| Empty it to actually reclaim the space. | Kosongkan Keranjang Sampah untuk benar-benar membebaskan ruang. |
| {0} freed | {0} dikosongkan |
| {0} cleaned up | {0} dibersihkan |
| {0} moved | {0} dipindahkan |
| {0} moved, some files could not be processed | {0} dipindahkan, sebagian file tidak dapat diproses |
| {0} freed, some files could not be processed | {0} dikosongkan, sebagian file tidak dapat diproses |
| {0} cleaned up, some files could not be processed | {0} dibersihkan, sebagian file tidak dapat diproses |
| {0} {1} moved to: {2} | {0} {1} dipindahkan ke: {2} |
| {0} {1} moved to: {2} | {0} {1} dipindahkan ke: {2} |
| {0} {1} moved to: {2}. {3} {4} | {0} {1} dipindahkan ke: {2}. {3} {4} |
| {0} {1} moved to: {2}. {3} {4} | {0} {1} dipindahkan ke: {2}. {3} {4} |
| {0} {1} moved to the Recycle Bin | {0} {1} dipindahkan ke Keranjang Sampah |
| {0} {1} moved to the Recycle Bin | {0} {1} dipindahkan ke Keranjang Sampah |
| {0} {1} moved to the Recycle Bin. {2} {3} | {0} {1} dipindahkan ke Keranjang Sampah. {2} {3} |
| {0} {1} moved to the Recycle Bin. {2} {3} | {0} {1} dipindahkan ke Keranjang Sampah. {2} {3} |
| {0} {1} kept in place, needed again by a program since the scan. | {0} {1} dibiarkan di tempatnya, kembali diperlukan sebuah program sejak pemindaian. |
| Moved {0} of {1} {2} before you cancelled. | {0} dari {1} {2} dipindahkan sebelum Anda membatalkan. |
| Moved {0} of {1} {2} to the Recycle Bin before you cancelled. | {0} dari {1} {2} dipindahkan ke Keranjang Sampah sebelum Anda membatalkan. |
| Permanently deleted {0} of {1} {2} before you cancelled. | {0} dari {1} {2} dihapus permanen sebelum Anda membatalkan. |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. | {0} {1} dihapus permanen. File tidak masuk ke Keranjang Sampah. |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. | {0} {1} dihapus permanen. File tidak masuk ke Keranjang Sampah. |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. {2} {3} | {0} {1} dihapus permanen. File tidak masuk ke Keranjang Sampah. {2} {3} |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. {2} {3} | {0} {1} dihapus permanen. File tidak masuk ke Keranjang Sampah. {2} {3} |
| That's fine, it was safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Tidak masalah, file itu memang aman dihapus. InstallerClean hanya membersihkan file yang Windows laporkan sudah tidak terpakai, tidak pernah file yang masih diperlukan sebuah program. Pada kemungkinan kecil suatu penghapusan membuat sebuah program tidak bisa diperbaiki, diperbarui, atau dihapus instalasinya, memasangnya ulang dari pembuatnya biasanya memulihkan file tersebut, meski Microsoft tidak menjaminnya. |
| That's fine, they were safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Tidak masalah, file itu memang aman dihapus. InstallerClean hanya membersihkan file yang Windows laporkan sudah tidak terpakai, tidak pernah file yang masih diperlukan sebuah program. Pada kemungkinan kecil suatu penghapusan membuat sebuah program tidak bisa diperbaiki, diperbarui, atau dihapus instalasinya, memasangnya ulang dari pembuatnya biasanya memulihkan file tersebut, meski Microsoft tidak menjaminnya. |

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
| Windows still lists {0} old patch whose file is already gone from disk. That's harmless, and there's nothing you need to do. | Windows masih mendaftarkan {0} patch lama yang file-nya sudah tidak ada di disk. Itu tidak berbahaya, dan tidak ada yang perlu Anda lakukan. |
| Windows still lists {0} old patches whose files are already gone from disk. That's harmless, and there's nothing you need to do. | Windows masih mendaftarkan {0} patch lama yang file-nya sudah tidak ada di disk. Itu tidak berbahaya, dan tidak ada yang perlu Anda lakukan. |
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
| Windows refused InstallerClean access. InstallerClean is already running as administrator, so starting it again that way won't help.<br><br>That leaves two likely causes: security software is holding C:\Windows\Installer, or the folder's permissions have been changed. Pausing the security software and trying again is the quickest one to rule out. | Windows menolak akses InstallerClean. InstallerClean sudah berjalan sebagai administrator, jadi menjalankannya lagi dengan cara itu tidak akan membantu.<br><br>Tinggal dua kemungkinan penyebabnya: perangkat lunak keamanan sedang menahan C:\Windows\Installer, atau izin folder itu telah diubah. Menjeda perangkat lunak keamanan lalu mencoba lagi adalah cara tercepat untuk menyingkirkan salah satu kemungkinan itu. |
| Installer database unavailable | Basis data penginstal tidak tersedia |
| Scan failed | Pemindaian gagal |
| The Windows Installer database appears to be empty or inaccessible. This is unusual even on a fresh Windows install and typically means the database is corrupt or a third-party tool has cleared it. Running 'sfc /scannow' from an elevated prompt usually repairs it. | Basis data Windows Installer tampak kosong atau tidak bisa diakses. Ini tidak biasa bahkan pada Windows yang baru dipasang dan biasanya berarti basis data rusak atau alat pihak ketiga telah mengosongkannya. Menjalankan 'sfc /scannow' dari prompt dengan hak administrator biasanya memperbaikinya. |
| Windows Installer refused to list the installed products, and InstallerClean is already running as administrator, so running it again won't help. The permissions on Windows's own installer records may have been changed, or security software may be blocking them. Running 'sfc /scannow' from an elevated prompt is worth a try. | Windows Installer menolak menampilkan daftar produk yang terpasang, dan InstallerClean sudah berjalan sebagai administrator, jadi menjalankannya lagi tidak akan membantu. Izin pada catatan penginstal milik Windows sendiri mungkin telah diubah, atau perangkat lunak keamanan mungkin memblokirnya. Menjalankan 'sfc /scannow' dari prompt dengan hak administrator layak dicoba. |
| Windows Installer refused to list products after {0} consecutive failures (last error code {1}). Try restarting Windows, or run 'sfc /scannow' from an elevated prompt. | Windows Installer menolak menampilkan daftar produk setelah {0} kali gagal berturut-turut (kode kesalahan terakhir {1}). Coba mulai ulang Windows, atau jalankan 'sfc /scannow' dari prompt dengan hak administrator. |
| Windows Installer refused to list a product's patches after {0} consecutive failures (last error code {1}). Try restarting Windows, or run 'sfc /scannow' from an elevated prompt. | Windows Installer menolak menampilkan daftar patch untuk sebuah produk setelah {0} kali gagal berturut-turut (kode kesalahan terakhir {1}). Coba mulai ulang Windows, atau jalankan 'sfc /scannow' dari prompt dengan hak administrator. |
| InstallerClean couldn't cross-check this scan against Windows: everything Windows still lists is missing from the cache folder, while the files in the folder match nothing Windows knows about. That points to a problem reading the installer records rather than to files you can safely remove, so nothing has been offered for cleanup. Restarting Windows and scanning again usually clears it. | InstallerClean tidak bisa memeriksa silang pemindaian ini dengan Windows: semua yang masih didaftarkan Windows tidak ada di folder cache, sementara file-file di folder itu tidak cocok dengan apa pun yang Windows ketahui. Itu menunjukkan adanya masalah dalam membaca catatan penginstal, bukan file yang aman Anda hapus, jadi tidak ada yang ditawarkan untuk dibersihkan. Memulai ulang Windows lalu memindai lagi biasanya mengatasinya. |
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
| This file is not inside the Windows Installer folder; refused for safety. | File ini tidak berada di dalam folder Windows Installer; ditolak demi keamanan. |
| Access denied. | Akses ditolak. |
| The operation failed. Try again or restart Windows. | Operasi gagal. Coba lagi atau mulai ulang Windows. |
| Unknown error. | Kesalahan tidak diketahui. |
| Couldn't move this file to the Recycle Bin (error {0}). It may be locked, in use or blocked by Windows. Use the Move button instead. | Tidak bisa memindahkan file ini ke Keranjang Sampah (kesalahan {0}). File mungkin terkunci, sedang digunakan, atau diblokir Windows. Gunakan tombol Pindahkan sebagai gantinya. |
| Windows blocked access to this file, even with administrator rights (error {0}). It is usually an ownership or permissions lock. Use the Move button instead. | Windows memblokir akses ke file ini, bahkan dengan hak administrator (kesalahan {0}). Biasanya ini karena masalah kepemilikan atau izin. Gunakan tombol Pindahkan sebagai gantinya. |
| This file is open or locked by another program (error {0}). Close that program, or whatever is scanning it, then try again, or use the Move button instead. | File ini sedang dibuka atau dikunci oleh program lain (kesalahan {0}). Tutup program itu, atau apa pun yang sedang memindainya, lalu coba lagi, atau gunakan tombol Pindahkan sebagai gantinya. |
| The file was permanently deleted because it could not be moved to the Recycle Bin. | File dihapus permanen karena tidak bisa dipindahkan ke Keranjang Sampah. |
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
| Donate | Donasi |
| It's thirsty work! | Membuat haus! |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Pembatalan diminta. InstallerClean sedang menunggu langkah yang berjalan mencapai titik berhenti. Ini bisa memakan waktu beberapa detik saat I/O berat atau panggilan basis data MSI. |
| Close | Tutup |
| Leave a star on GitHub, report an Issue or post in Discussions. Any feedback welcome. | Beri bintang di GitHub, laporkan Issue, atau tulis di Discussions. Masukan apa pun diterima. |
| or report an Issue or post in Discussions. Any feedback welcome. | atau laporkan Issue, atau tulis di Discussions. Masukan apa pun diterima. |
| Minimise | Kecilkan |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | Terserah Anda, tapi sangat dihargai. Mengirim ringkasan anonim yang sekadar memberi tahu saya apakah aplikasi berfungsi dan berapa banyak ruang yang dikosongkan orang-orang. Layar berikutnya memperlihatkan apa yang akan dikirim sebelum Anda mengonfirmasi. |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | Terserah Anda, tapi sangat dihargai. Mengirim ringkasan anonim yang sekadar memberi tahu saya apakah aplikasi berfungsi. Layar berikutnya memperlihatkan apa yang akan dikirim sebelum Anda mengonfirmasi. |
| Move the unneeded files to the Move location. | Pindahkan file yang tidak diperlukan ke lokasi pemindahan. |
| Move the unneeded files to the Move location. Choose one first. | Pindahkan file yang tidak diperlukan ke lokasi pemindahan. Pilih lokasinya dulu. |
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
| Leave a star on GitHub | Beri bintang di GitHub |
| Leave a star on GitHub (About window) | Beri bintang di GitHub (jendela Tentang) |
| Minimise | Kecilkan |
| Move all unneeded installer files to the chosen destination folder | Pindahkan semua file penginstal yang tidak diperlukan ke folder tujuan yang dipilih |
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
| Open the release page to download the newer version, or cancel to keep the current version. | Buka halaman rilis untuk mengunduh versi yang lebih baru, atau batalkan untuk tetap memakai versi saat ini. |
| MIT licence | Lisensi MIT |
| Opens the licence file on github.com in your browser. | Membuka file lisensi di github.com melalui peramban Anda. |
| Move location | Lokasi pemindahan |
| Products | Produk |
| Patches | Patch |
| Product details | Detail produk |
| Move destination folder | Folder tujuan pemindahan |
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
| Result log preview | Pratinjau log hasil |
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
