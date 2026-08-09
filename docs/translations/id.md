# InstallerClean in Bahasa Indonesia (Indonesian)

The text of InstallerClean's interface and command-line tool in English on the left, with the Indonesian translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Indonesian can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.id.resx`](../../src/InstallerClean.Core/Resources/Strings.id.resx), so do not edit it by hand. The Indonesian translation itself lives in [`gen-strings-id.mjs`](../../scripts/translations/gen-strings-id.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | Bahasa Indonesia |
| --- | --- |
| InstallerClean | InstallerClean |
| About | Tentang |
| Registered files that should not be deleted | File terdaftar yang sebaiknya tidak dihapus |
| Unneeded files that are safe to delete | File tidak diperlukan yang aman dihapus |

## Section headings

| English | Bahasa Indonesia |
| --- | --- |
| PRODUCTS | PRODUK |
| PATCHES | PATCH |
| PRODUCT DETAILS | DETAIL PRODUK |
| BACKUP FOLDER | BACKUP FOLDER |
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
| _Delete permanently | _Hapus permanen |
| _Done | _Selesai |
| Details | Detail |
| _Buy me a cuppa | Traktir saya secangkir _kopi |
| Leave a _star on GitHub | Beri _bintang di GitHub |
| Apache 2.0 licence | Lisensi Apache 2.0 |
| _Move | _Pindahkan |
| Path to folder if you move rather than delete. | Path to folder if you move rather than delete. |
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
| Moving unneeded files... | Moving unneeded files... |
| Deleting unneeded files... | Deleting unneeded files... |
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
| Any unneeded files below are [safe to delete]. | Any unneeded files below are [safe to delete]. |
| They sit in {InstallerFolder}, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | File-file ini berada di {InstallerFolder}, tertinggal saat sebuah program dihapus instalasinya ({0}), patch yang lebih baru menggantikan yang lama ({1}), atau penerbitnya menariknya ({2}). InstallerClean hanya pernah mencantumkan file yang Windows sendiri laporkan sudah tidak terpakai. |
| Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored. | Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored. |
| Nothing scanned yet. | Belum ada yang dipindai. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Tekan Pindai ulang untuk menelusuri {InstallerFolder} mencari file penginstal yang tidak lagi diperlukan program mana pun. |
| These files can't be cleaned up right now. | File-file ini tidak bisa dibersihkan sekarang. |
| Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. |
| Select a file to view details. | Pilih file untuk melihat detail. |
| Select a product to view details. | Pilih produk untuk melihat detail. |
| No metadata available. | Tidak ada metadata yang tersedia. |
| This installer file is missing. It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it, and its own last resort is reinstalling the program. | This installer file is missing. It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it, and its own last resort is reinstalling the program. |
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
| Nothing removed | Nothing removed |
| Nothing to clean up in {InstallerFolder} | Tidak ada yang perlu dibersihkan di {InstallerFolder} |
| Scanned {0} {1} in {2} | {0} {1} dipindai dalam {2} |
| The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). |
| The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). |
| The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). |
| The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). |
| {0} freed | {0} dikosongkan |
| {0} moved | {0} dipindahkan |
| Nothing was moved | Tidak ada yang dipindahkan |
| Nothing was deleted | Tidak ada yang dihapus |
| {0} of {1} could not be moved. | {0} dari {1} file tidak bisa dipindahkan. |
| {0} of {1} could not be moved. | {0} dari {1} file tidak bisa dipindahkan. |
| {0} of {1} could not be deleted. | {0} dari {1} file tidak bisa dihapus. |
| {0} of {1} could not be deleted. | {0} dari {1} file tidak bisa dihapus. |
| {0} {1} moved to: {2} | {0} {1} dipindahkan ke: {2} |
| {0} {1} moved to: {2} | {0} {1} dipindahkan ke: {2} |
| {0} {1} kept in place, because the records now claim what the scan flagged. | {0} {1} kept in place, because the records now claim what the scan flagged. |
| {0} {1} kept in place, because the Windows Installer records had changed by the final check. | {0} {1} kept in place, because the Windows Installer records had changed by the final check. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read in the final check. | {0} {1} kept in place, because the Windows Installer records could not be fully read in the final check. |
| {0} {1} kept in place, because Windows has a record of the program named inside. | {0} {1} kept in place, because Windows has a record of the program named inside. |
| {0} {1} kept in place, because InstallerClean couldn't find a program named inside. | {0} {1} kept in place, because InstallerClean couldn't find a program named inside. |
| Moved {0} of {1} {2} before you cancelled. | {0} dari {1} {2} dipindahkan sebelum Anda membatalkan. |
| Permanently deleted {0} of {1} {2} before you cancelled. | {0} dari {1} {2} dihapus permanen sebelum Anda membatalkan. |
| {0} {1} permanently deleted | {0} {1} permanently deleted |
| {0} {1} permanently deleted | {0} {1} permanently deleted |
| Glad to help. There's a tip jar if you're feeling kind. | Senang bisa membantu. Kalau Anda berbaik hati, secangkir kopi sangat saya hargai. |

## Summaries and counts

| English | Bahasa Indonesia |
| --- | --- |
| {0} file left alone | {0} file left alone |
| {0} files left alone | {0} files left alone |
| {0} unneeded file to clean up | {0} file tidak diperlukan untuk dibersihkan |
| {0} unneeded files to clean up | {0} file tidak diperlukan untuk dibersihkan |
| {0} registered file is missing. No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | {0} registered file is missing. No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. |
| {0} registered files are missing. No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | {0} registered files are missing. No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. |
| InstallerClean couldn't match up everything in the Windows records, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. | InstallerClean couldn't match up everything in the Windows records, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. |
| {0} of {1} {2} | {0} dari {1} {2} |
| {0} orphaned, {1} superseded, {2} obsoleted ({3}) | {0} terisolasi, {1} digantikan, {2} usang ({3}) |
| {0} registered file left alone ({1}) | {0} registered file left alone ({1}) |
| {0} registered files left alone ({1}) | {0} registered files left alone ({1}) |

## Confirmation dialogs

| English | Bahasa Indonesia |
| --- | --- |
| Move {0} {1} ({2})? | Pindahkan {0} {1} ({2})? |
| Move to: | Move to: |
| Delete {0} {1} ({2})? | Hapus {0} {1} ({2})? |
| This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. | This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. |
| Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. | Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. |

## Error messages

| English | Bahasa Indonesia |
| --- | --- |
| Access denied | Akses ditolak |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows menolak akses untuk InstallerClean, jadi prosesnya dihentikan. Tidak ada yang dihapus.<br><br>InstallerClean sudah berjalan sebagai administrator, jadi menjalankannya lagi dengan cara itu tidak akan membantu. Windows tidak menjelaskan lebih jauh apa yang menolak akses, jadi tidak ada hal khusus yang bisa dicoba. |
| Couldn't read the Windows Installer records | Catatan Windows Installer tidak bisa dibaca |
| Scan failed | Pemindaian gagal |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed. | Catatan Windows Installer kembali sepenuhnya kosong: tidak satu pun program terpasang atau pembaruan yang mengklaim file pemasang di cache. Itu tidak terjadi pada komputer yang berfungsi (bahkan pemasangan Windows yang baru pun punya beberapa), jadi catatannya rusak atau tidak bisa dibaca, dan pemindaian yang memercayai jawaban ini akan keliru menyebut setiap file di {InstallerFolder} terisolasi. InstallerClean berhenti sebagai gantinya. Tidak ada yang dihapus. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer tidak mengizinkan InstallerClean menampilkan daftar apa saja yang terpasang. InstallerClean sudah berjalan sebagai administrator, jadi menjalankannya lagi sebagai administrator tidak akan mengubah apa pun. Tanpa daftar itu tidak ada cara yang aman untuk mengetahui file cache mana yang masih diperlukan, jadi InstallerClean berhenti. Tidak ada yang dihapus. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer tidak bisa memberi InstallerClean daftar program terpasang yang terbaca: {0} entri berturut-turut kembali tidak terbaca (kode kesalahan terakhir {1}). Alih-alih bekerja dengan daftar yang hanya terbaca sebagian, InstallerClean berhenti. Tidak ada yang dihapus. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer tidak pernah menandai akhir daftar program terpasang: InstallerClean menyerah setelah {0} entri (kode kesalahan terakhir {1}). Daftar yang tidak berujung tidak bisa dipercaya, jadi InstallerClean berhenti. Tidak ada yang dihapus. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer tidak pernah menandai akhir daftar patch sebuah program: InstallerClean menyerah setelah {0} entri (kode kesalahan terakhir {1}). Daftar yang tidak berujung tidak bisa dipercaya, jadi InstallerClean berhenti. Tidak ada yang dihapus. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean tidak bisa membaca cukup banyak catatan Windows Installer untuk memastikan apa yang masih diperlukan: daftar program terpasang kembali tidak lengkap, dan membaca catatan yang sama langsung dari registri juga menemui kesalahan. Sebuah file bisa tampak terisolasi hanya karena catatan yang menyebutkannya termasuk yang tidak terbaca, jadi InstallerClean berhenti. Tidak ada yang dihapus. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. |
| Nothing was deleted | Tidak ada yang dihapus |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. |
| Invalid destination | Tujuan tidak valid |
| Could not write to destination | Tidak bisa menulis ke tujuan |
| Move failed | Pemindahan gagal |
| Delete failed | Penghapusan gagal |
| Setting not saved | Pengaturan tidak tersimpan |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | Perubahan tidak dapat disimpan. Saat berikutnya dijalankan, InstallerClean akan kembali ke pengaturan sebelumnya. |
| The destination cannot be inside the Windows Installer folder. | Tujuan tidak boleh berada di dalam folder Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. |
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
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. |
| Windows reported a file error; the file was left in place. | Windows melaporkan kesalahan file; file dibiarkan di tempatnya. |
| Windows reported file errors; these files were left in place. | Windows melaporkan kesalahan file; file-file ini dibiarkan di tempatnya. |
| Something went wrong with this file; it was left in place. | Ada yang tidak beres dengan file ini; file dibiarkan di tempatnya. |
| Something went wrong with these files; they were left in place. | Ada yang tidak beres dengan file-file ini; semuanya dibiarkan di tempatnya. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Menolak memindahkan file ke dalam folder Windows Installer (tujuan: {0}). |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. |
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
| GitHub returned an error response. Try again in a few minutes. | GitHub mengembalikan respons kesalahan. Coba lagi dalam beberapa menit. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | Respons GitHub tidak memuat rilis yang dikenali. Coba lagi nanti, atau buka halaman rilis langsung. |
| The check timed out. Your connection to GitHub may be slow; try again. | Pemeriksaan kehabisan waktu. Koneksi Anda ke GitHub mungkin lambat; coba lagi. |
| The check failed for an unknown reason. Details are in crash.log if you need to report it. | Pemeriksaan gagal karena alasan yang tidak diketahui. Detailnya ada di crash.log jika Anda perlu melaporkannya. |

## Opening links in your browser

| English | Bahasa Indonesia |
| --- | --- |
| Couldn't open your browser | Tidak bisa membuka browser Anda |
| InstallerClean couldn't open your browser. The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | InstallerClean tidak bisa membuka browser Anda. Tautannya sudah ada di clipboard, jadi Anda bisa menempelkannya sendiri:<br><br>{0} |
| InstallerClean couldn't open your browser, and couldn't copy the link to your clipboard either. The link is:<br><br>{0} | InstallerClean tidak bisa membuka browser Anda, dan juga tidak bisa menyalin tautan ke clipboard. Tautannya:<br><br>{0} |

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
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> |

## Tooltips (hover text)

| English | Bahasa Indonesia |
| --- | --- |
| It's thirsty work! | Membuat haus! |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Pembatalan diminta. InstallerClean sedang menunggu langkah yang berjalan mencapai titik berhenti. Ini bisa memakan waktu beberapa detik saat I/O berat atau panggilan basis data MSI. |
| Close | Tutup |
| A star helps other people find it. | Bintang membantu orang lain menemukan InstallerClean. |
| Minimise | Kecilkan |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | Terserah Anda, tapi sangat dihargai. Mengirim ringkasan anonim yang sekadar memberi tahu saya apakah aplikasi berfungsi dan berapa banyak ruang yang dikosongkan orang-orang. Layar berikutnya memperlihatkan apa yang akan dikirim sebelum Anda mengonfirmasi. |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | Terserah Anda, tapi sangat dihargai. Mengirim ringkasan anonim yang sekadar memberi tahu saya apakah aplikasi berfungsi. Layar berikutnya memperlihatkan apa yang akan dikirim sebelum Anda mengonfirmasi. |
| Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. | Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. |
| Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. | Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. | Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. |
| Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. | Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Nama subjek dari sertifikat Authenticode yang disematkan. Rantai sertifikat tidak diverifikasi. |
| Change language. The program will restart. | Ganti bahasa. Program akan dimulai ulang. |

## Screen reader labels

| English | Bahasa Indonesia |
| --- | --- |
| Donate | Donasi |
| Buy me a cuppa | Traktir saya secangkir kopi |
| Cancel operation | Batal, operasi |
| Cancel scan | Batal, pemindaian |
| Cancel startup scan | Batal, pemindaian awal |
| Close | Tutup |
| Close window | Tutup jendela |
| Close result and return to main window | Tutup hasil dan kembali ke jendela utama |
| Leave a star on github | Beri bintang di github |
| Minimise | Kecilkan |
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Delete permanently removes the unneeded files. Cancel closes without deleting. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Pindahkan menaruh file yang tidak diperlukan di folder tujuan yang dipilih. Batal membiarkannya di tempatnya. |
| Say thanks | Ucapkan terima kasih |
| Send posts the report shown to No Faff. Cancel sends nothing. | Kirim mengirimkan laporan yang ditampilkan ke No Faff. Batal tidak mengirim apa pun. |
| Check for updates | Periksa pembaruan |
| Checks github's releases page for a newer version. | Memeriksa halaman rilis github untuk mencari versi yang lebih baru. |
| Opens the readme on github in your browser. | Membuka readme di github melalui browser Anda. |
| Opens the issue tracker on github.com in your browser. | Membuka pelacak masalah (Issues) di github.com melalui browser Anda. |
| If ticked, InstallerClean checks github for a newer version when you run it. | Jika dicentang, InstallerClean memeriksa apakah ada versi yang lebih baru di github saat Anda menjalankannya. |
| Open the release page to download the newer version, or cancel to keep the current version. | Buka halaman rilis untuk mengunduh versi yang lebih baru, atau batalkan untuk tetap memakai versi saat ini. |
| Opens the licence file on github.com in your browser. | Membuka file lisensi di github.com melalui browser Anda. |
| Backup folder | Backup folder |
| Products | Produk |
| Patches | Patch |
| Product details | Detail produk |
| Backup folder | Backup folder |
| Operation progress | Kemajuan operasi |
| Scan {InstallerFolder} again | Pindai ulang {InstallerFolder} |
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
| Product details | Product details |
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
| Error: unknown argument '{0}' | Error: unknown argument '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Kesalahan: argumen tambahan yang tidak terduga '{0}'. Jika folder pemindahan Anda mengandung spasi, apit seluruh jalur dengan tanda kutip: /m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. |
| Cancelling... | Membatalkan... |
| Cancelled. | Dibatalkan. |
| Error: unexpected failure ({0}). Details written to {1}. | Error: unexpected failure ({0}). Details written to {1}. |
| Error: unexpected failure ({0}). The crash log could not be written. | Error: unexpected failure ({0}). The crash log could not be written. |
| Scanning {InstallerFolder}... | Memindai {InstallerFolder}... |
| Found {0} unneeded {1} to clean up ({2}). | Found {0} unneeded {1} to clean up ({2}). |
| Found no unneeded files. | Found no unneeded files. |
| {0} registered file is missing from {InstallerFolder}. No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, preferably the same version, should restore it. | {0} registered file is missing from {InstallerFolder}. No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, preferably the same version, should restore it. |
| {0} registered files are missing from {InstallerFolder}. No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, preferably the same version, should restore them. | {0} registered files are missing from {InstallerFolder}. No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, preferably the same version, should restore them. |
| InstallerClean couldn't match up everything in the Windows records, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. | InstallerClean couldn't match up everything in the Windows records, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. |
| Deleting {0} unneeded {1}... | Deleting {0} unneeded {1}... |
| Permanently deleted {0} unneeded {1}. | Permanently deleted {0} unneeded {1}. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Kesalahan: tujuan pemindahan tidak ditentukan. Gunakan /m JALUR. (Default yang diatur di GUI bersifat per-pengguna dan tidak berlaku untuk tugas terjadwal atau proses akun layanan.) |
| Error: destination cannot be inside the Windows Installer folder. | Kesalahan: tujuan tidak boleh berada di dalam folder Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Kesalahan: tujuan harus berupa jalur absolut lengkap. Diterima: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. |
| Error: something is using Windows Installer right now, such as a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | Error: something is using Windows Installer right now, such as a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. |
| Moving {0} unneeded {1} to {2}... | Moving {0} unneeded {1} to {2}... |
| Moved {0} unneeded {1}. | Moved {0} unneeded {1}. |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then run the command again. | InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then run the command again. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Proses InstallerClean lain memegang kunci instans-tunggal (GUI atau proses CLI lain). Kode keluar 75 (sementara); aman untuk dicoba lagi nanti. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Catatan: penulisan ke Log Peristiwa gagal. Periksa izin log Aplikasi atau Kebijakan Grup. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - pembersihan {InstallerFolder} |
| Removes cached .msi and .msp files that no installed program still needs. | Removes cached .msi and .msp files that no installed program still needs. |
| Needs an elevated (administrator) prompt; Windows will not start it. | Needs an elevated (administrator) prompt; Windows will not start it. |
| Usage: | Penggunaan: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     Tampilkan bantuan ini (juga /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  Cetak versi (juga -v) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         Scan only - list unneeded files |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         Delete unneeded files permanently |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         Move to the saved backup folder |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m JALUR   Pindahkan ke jalur yang ditentukan |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. |
| Exit codes: | Kode keluar: |
|   0   success: the run finished with nothing left to do |   0   success: the run finished with nothing left to do |
|   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   partial: some processed, some not (a failure or a Ctrl+C part way) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  sementara: kondisi sementara memblokir proses (lihat pesannya) |
|   130 cancelled (Ctrl+C) |   130 dibatalkan (Ctrl+C) |
