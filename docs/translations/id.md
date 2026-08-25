# InstallerClean in Bahasa Indonesia (Indonesian)

The text of InstallerClean's interface and command-line tool in English on the left, with the Indonesian translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Indonesian can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.id.resx`](../../src/InstallerClean.Core/Resources/Strings.id.resx), so do not edit it by hand. The Indonesian translation itself lives in [`gen-strings-id.mjs`](../../scripts/translations/gen-strings-id.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | Bahasa Indonesia |
| --- | --- |
| InstallerClean | InstallerClean |
| About | Tentang |
| Files left alone | File yang dibiarkan apa adanya |
| Unneeded files that are safe to delete | File tidak diperlukan yang aman dihapus |

## Section headings

| English | Bahasa Indonesia |
| --- | --- |
| PATCHES | PATCH |
| PRODUCT DETAILS | DETAIL PRODUK |
| BACKUP FOLDER | FOLDER TUJUAN |
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
| Path to folder if you move rather than delete. | Jalur ke folder jika Anda memindahkan, bukan menghapus. |
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
| Moving unneeded files... | Memindahkan file yang tidak diperlukan... |
| Deleting unneeded files... | Menghapus file yang tidak diperlukan... |
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
| Any unneeded files below are [safe to delete]. | File apa pun yang tidak diperlukan di bawah ini [aman dihapus]. |
| They sit in {InstallerFolder}. InstallerClean asks Windows about every installed program: a file is listed when no program claims it ({0}), or when a newer patch has replaced it and no program could roll back to it ({1}). | File-file itu ada di {InstallerFolder}. InstallerClean menanyakan setiap program yang terpasang kepada Windows: sebuah file masuk daftar jika tidak ada program yang mengakuinya ({0}), atau jika sebuah patch yang lebih baru telah menggantikannya dan tidak ada program yang bisa kembali kepadanya ({1}). |
| Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update, repair and uninstall as normal. Putting them back into {InstallerFolder} restores everything. Or delete them permanently now. | Pindahkan ke folder tujuan pilihan Anda, lalu hapus folder itu setelah Anda yakin program-program Anda tetap bisa diperbarui, diperbaiki dan dicopot seperti biasa. Mengembalikannya ke {InstallerFolder} memulihkan semuanya. Atau hapus permanen sekarang juga. |
| Nothing scanned yet. | Belum ada yang dipindai. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Tekan Pindai ulang untuk menelusuri {InstallerFolder} mencari file penginstal yang tidak lagi diperlukan program mana pun. |
| These files can't be cleaned up right now. | File-file ini tidak bisa dibersihkan sekarang. |
| Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Ada yang sedang memakai Windows Installer saat ini, misalnya pembaruan Windows atau program yang memasang diri di latar belakang. Pindahkan dan Hapus dijeda selama itu berjalan, sehingga InstallerClean tidak menyentuh {InstallerFolder} selagi berubah. Setelah selesai, pindai ulang dan keduanya kembali aktif. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Ada transaksi Windows Installer sebelumnya yang tertunda di mesin ini. Lanjutkan atau batalkan pemasangan itu (atau mulai ulang Windows) sebelum membersihkan {InstallerFolder}. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows mengantrekan penggantian nama file untuk mulai ulang berikutnya yang memengaruhi {InstallerFolder}. Mulai ulang Windows sebelum membersihkan. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer sedang mengerjakan sesuatu, jadi Pindahkan dan Hapus dijeda. InstallerClean tidak akan menyentuh {InstallerFolder} selagi berubah. Setelah selesai, pindai ulang dan keduanya kembali aktif. |
| Select a file to view details. | Pilih file untuk melihat detail. |
| Select a product to view details. | Pilih produk untuk melihat detail. |
| No metadata available. | Tidak ada metadata yang tersedia. |
| This installer file is missing. It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it, and its own last resort is reinstalling the program. | File penginstal ini hilang. Sekarang ini tidak menimbulkan masalah, dan tidak akan sampai hari ketika Anda mencoba memperbaiki, memperbarui atau mencopot program pemiliknya. Langkah itu bisa gagal, karena Windows mencari file ini dan file itu tidak ada.<br><br>Untuk mencoba memperbaikinya, unduh penginstal program itu dari pembuatnya dan jalankan di atas salinan yang sudah ada (jangan copot dulu: mencopot sendiri adalah langkah yang membutuhkan file ini). Gunakan versi yang terpasang jika Anda bisa mendapatkannya, karena Windows mungkin menolak versi lain. Ini seharusnya memulihkan file dan membiarkan pengaturan Anda utuh, tetapi Microsoft tidak menjaminnya, dan jalan terakhir mereka sendiri adalah memasang ulang program itu. |
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
| Nothing removed | Tidak ada yang disingkirkan |
| Nothing to clean up in {InstallerFolder} | Tidak ada yang perlu dibersihkan di {InstallerFolder} |
| Scanned {0} {1} in {2} | {0} {1} dipindai dalam {2} |
| Nothing offered on this PC | Tidak ada yang ditawarkan di PC ini |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({2}) it might otherwise have offered. | InstallerClean tidak bisa memastikan file mana dalam cache yang menjadi milik program-program yang terpasang di sini, jadi satu-satunya file ({2}) yang mungkin ditawarkannya ditahan. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back all {0} {1} ({2}) it might otherwise have offered. | InstallerClean tidak bisa memastikan file mana dalam cache yang menjadi milik program-program yang terpasang di sini, jadi seluruh {0} {1} ({2}) yang mungkin ditawarkannya ditahan. |
| The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | File di folder itu [aman disingkirkan], jadi hapus foldernya kapan saja Anda mau. Sampai saat itu, Anda bisa mengembalikannya ke {InstallerFolder} kalau suatu program ternyata membutuhkannya (sangat kecil kemungkinannya). |
| The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | File di folder itu [aman disingkirkan], jadi hapus foldernya kapan saja Anda mau. Sampai saat itu, Anda bisa mengembalikannya ke {InstallerFolder} kalau suatu program ternyata membutuhkan salah satunya (sangat kecil kemungkinannya). |
| The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | File di folder itu [aman disingkirkan], jadi hapus foldernya atau pindahkan ke drive lain kapan pun Anda benar-benar ingin mendapatkan kembali ruangnya. Sampai saat itu, Anda bisa mengembalikannya ke {InstallerFolder} kalau suatu program ternyata membutuhkannya (sangat kecil kemungkinannya). |
| The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | File di folder itu [aman disingkirkan], jadi hapus foldernya atau pindahkan ke drive lain kapan pun Anda benar-benar ingin mendapatkan kembali ruangnya. Sampai saat itu, Anda bisa mengembalikannya ke {InstallerFolder} kalau suatu program ternyata membutuhkan salah satunya (sangat kecil kemungkinannya). |
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
| {0} {1} kept in place, because the records now claim what the scan flagged. | {0} {1} dibiarkan di tempatnya, karena catatan kini mengakui apa yang ditandai pemindaian. |
| {0} {1} kept in place, because the Windows Installer records had changed by the final check. | {0} {1} dibiarkan di tempatnya, karena catatan Windows Installer sudah berubah pada pemeriksaan terakhir. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read in the final check. | {0} {1} dibiarkan di tempatnya, karena catatan Windows Installer tidak bisa dibaca seluruhnya pada pemeriksaan terakhir. |
| {0} {1} kept in place, because by the final check InstallerClean could not be certain which cached files belong to the programs installed here. | {0} {1} dibiarkan di tempatnya, karena sampai pemeriksaan terakhir InstallerClean tidak bisa memastikan file mana dalam cache yang menjadi milik program-program yang terpasang di sini. |
| {0} {1} kept in place, because Windows has a record of the program named inside. | {0} {1} dibiarkan di tempatnya, karena Windows punya catatan tentang program yang disebutkan di dalamnya. |
| {0} {1} kept in place, because InstallerClean couldn't find a program named inside. | {0} {1} dibiarkan di tempatnya, karena InstallerClean tidak menemukan nama program di dalamnya. |
| Moved {0} of {1} {2} before you cancelled. | {0} dari {1} {2} dipindahkan sebelum Anda membatalkan. |
| Permanently deleted {0} of {1} {2} before you cancelled. | {0} dari {1} {2} dihapus permanen sebelum Anda membatalkan. |
| {0} {1} permanently deleted | {0} {1} dihapus permanen |
| {0} {1} permanently deleted | {0} {1} dihapus permanen |
| Glad to help. There's a tip jar if you're feeling kind. | Senang bisa membantu. Kalau Anda berbaik hati, secangkir kopi sangat saya hargai. |

## Summaries and counts

| English | Bahasa Indonesia |
| --- | --- |
| {0} file left alone | {0} file dibiarkan apa adanya |
| {0} files left alone | {0} file dibiarkan apa adanya |
| {0} unneeded file to clean up | {0} file tidak diperlukan untuk dibersihkan |
| {0} unneeded files to clean up | {0} file tidak diperlukan untuk dibersihkan |
| Windows has a record for {0} file that isn't in {InstallerFolder}: {1}. It causes no trouble day to day, but a repair, update or uninstall can fail on it. Open Details for what to do. | Windows punya catatan untuk {0} file yang tidak ada di {InstallerFolder}: {1}. Sehari-hari ini tidak merepotkan, tapi perbaikan, pembaruan atau pencopotan bisa gagal karenanya. Buka Detail untuk tahu apa yang harus dilakukan. |
| Windows has records for {0} files that aren't in {InstallerFolder}: {1}. They cause no trouble day to day, but a repair, update or uninstall can fail on them. Open Details for what to do. | Windows punya catatan untuk {0} file yang tidak ada di {InstallerFolder}: {1}. Sehari-hari ini tidak merepotkan, tapi perbaikan, pembaruan atau pencopotan bisa gagal karenanya. Buka Detail untuk tahu apa yang harus dilakukan. |
| {0} other program | {0} program lain |
| {0} other programs | {0} program lain |
| {0} file with no program named in the records | {0} file tanpa nama program dalam catatan |
| {0} files with no program named in the records | {0} file tanpa nama program dalam catatan |
| On this PC InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file rather than listing it. | Di PC ini InstallerClean tidak bisa memastikan file mana dalam cache yang menjadi milik program-program yang terpasang di sini, jadi satu-satunya file itu ditahan dan tidak dicantumkan. |
| On this PC InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} rather than listing them. | Di PC ini InstallerClean tidak bisa memastikan file mana dalam cache yang menjadi milik program-program yang terpasang di sini, jadi {0} {1} ditahan dan tidak dicantumkan. |
| InstallerClean couldn't match up everything in the Windows records, so it didn't read all of them. The unneeded files above are unaffected, but what it says about files missing from {InstallerFolder} may be short of the full picture. Re-scan to try again. | InstallerClean tidak bisa mencocokkan semua yang ada dalam catatan Windows, jadi tidak semuanya dibaca. File yang tidak diperlukan di atas tidak terpengaruh, tapi keterangan tentang file yang hilang dari {InstallerFolder} bisa jadi belum lengkap. Pindai ulang untuk mencoba lagi. |
| {0} of {1} {2} | {0} dari {1} {2} |
| {0} unneeded {1} ({2}) | {0} {1} tidak diperlukan ({2}) |
| {0} file left alone ({1}) | {0} file dibiarkan apa adanya ({1}) |
| {0} files left alone ({1}) | {0} file dibiarkan apa adanya ({1}) |

## Confirmation dialogs

| English | Bahasa Indonesia |
| --- | --- |
| Move {0} {1} ({2})? | Pindahkan {0} {1} ({2})? |
| Move to: | Pindahkan ke: |
| Delete {0} {1} ({2})? | Hapus {0} {1} ({2})? |
| This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. | File ini akan dihapus permanen. File ini [aman dihapus], tapi kalau Anda ingin cadangan, gunakan tombol Pindahkan saja. |
| Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. | File-file ini akan dihapus permanen. Semuanya [aman dihapus], tapi kalau Anda ingin cadangan, gunakan tombol Pindahkan saja. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | Folder itu ada di drive yang sama, jadi ruangnya belum kembali sampai Anda menghapusnya. Pilih folder di drive lain kalau Anda ingin ruangnya langsung kembali. |

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
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean tidak bisa mencocokkan catatan Windows Installer dengan isi {InstallerFolder}. Hampir tidak ada yang ditunjuk catatan itu benar-benar ada di sana, dan hampir tidak ada yang ada di sana disebut oleh catatan mana pun, jadi tidak ada file yang bisa ditunjukkan tidak diperlukan. Tidak ada yang ditawarkan dan tidak ada yang disingkirkan. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean tidak bisa mencocokkan catatan Windows Installer dengan isi {InstallerFolder}. Folder itu berisi file, tapi tidak satu pun catatan menunjuk apa pun di dalamnya, jadi tidak ada file yang bisa ditunjukkan tidak diperlukan. Tidak ada yang ditawarkan dan tidak ada yang disingkirkan. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean tidak bisa membaca cukup banyak catatan Windows Installer untuk memastikan apa yang masih diperlukan: daftar program terpasang kembali tidak lengkap, dan membaca catatan yang sama langsung dari registri juga menemui kesalahan. Sebuah file bisa tampak terisolasi hanya karena catatan yang menyebutkannya termasuk yang tidak terbaca, jadi InstallerClean berhenti. Tidak ada yang dihapus. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean tidak berhasil membuat Windows menguraikan jalur sebenarnya dari {InstallerFolder}, jadi tidak ada file yang bisa ditunjukkan berada di dalamnya dan tidak ada yang ditawarkan untuk dibersihkan. Pemindaian ini tidak menemukan apa pun karena pemeriksaan itu gagal, bukan karena foldernya bersih. Tidak ada yang disingkirkan. |
| Nothing was deleted | Tidak ada yang dihapus |
| Nothing was moved | Tidak ada yang dipindahkan |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | InstallerClean tidak bisa mengambil kunci yang dipakai Windows Installer untuk mencegah dua program mengubah perangkat lunak terpasang sekaligus, jadi tidak bisa memastikan sebuah file tidak menjadi diperlukan di tengah jalan, dan tidak ada yang dihapus. Coba lagi, dan mulai ulang Windows kalau terus terjadi. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. Try again, and restart Windows if it keeps happening. | InstallerClean tidak bisa mengambil kunci yang dipakai Windows Installer untuk mencegah dua program mengubah perangkat lunak terpasang sekaligus, jadi tidak bisa memastikan sebuah file tidak menjadi diperlukan di tengah jalan, dan tidak ada yang dipindahkan. Coba lagi, dan mulai ulang Windows kalau terus terjadi. |
| Invalid destination | Tujuan tidak valid |
| Could not write to destination | Tidak bisa menulis ke tujuan |
| Move failed | Pemindahan gagal |
| Delete failed | Penghapusan gagal |
| Setting not saved | Pengaturan tidak tersimpan |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | Perubahan tidak dapat disimpan. Saat berikutnya dijalankan, InstallerClean akan kembali ke pengaturan sebelumnya. |
| The destination cannot be inside the Windows Installer folder. | Tujuan tidak boleh berada di dalam folder Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Tujuan {0} mengarah ke dalam folder sistem Windows. Pilih jalur di luar %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% dan %ProgramData%. |
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
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | File ini sedang dibuka atau dikunci program lain, jadi untuk saat ini tidak ada yang bisa menyingkirkannya. File itu dibiarkan di tempatnya; coba lagi nanti. |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | File-file ini sedang dibuka atau dikunci program lain, jadi untuk saat ini tidak ada yang bisa menyingkirkannya. Semuanya dibiarkan di tempatnya; coba lagi nanti. |
| Windows reported a file error; the file was left in place. | Windows melaporkan kesalahan file; file dibiarkan di tempatnya. |
| Windows reported file errors; these files were left in place. | Windows melaporkan kesalahan file; file-file ini dibiarkan di tempatnya. |
| Something went wrong with this file; it was left in place. | Ada yang tidak beres dengan file ini; file dibiarkan di tempatnya. |
| Something went wrong with these files; they were left in place. | Ada yang tidak beres dengan file-file ini; semuanya dibiarkan di tempatnya. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Menolak memindahkan file ke dalam folder Windows Installer (tujuan: {0}). |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | Folder tujuan harus berupa jalur lengkap ke sebuah folder, diawali huruf drive atau berbagi jaringan (misalnya D:\Backup, atau \\server\backup). InstallerClean tidak bisa memakai yang ini: {0} |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | InstallerClean tidak bisa lagi memastikan folder tujuan, jadi berhenti daripada menulis ke tempat yang salah. Periksa {0}, lalu Pindai ulang dan coba lagi. |
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
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log merekam eksepsi tak tertangani dari InstallerClean.<br># Dengan hak tinggi, pesan eksepsi framework bisa memuat jalur file<br># dari sesi yang berjalan (termasuk profil pengguna lain yang didata<br># oleh kueri Windows Installer). Pesan kegagalan jaringan dari<br># pemeriksaan pembaruan atau pengiriman log hasil bisa memuat URL<br># tujuan serta alamat IP atau proksi yang teruraikan. Entri tentang<br># catatan Windows Installer yang tak terbaca bisa memuat SID akun<br># Windows (S-1-5-21-...) dan kode produk perangkat lunak terpasang.<br># Hapus ketiga jenis rincian itu sebelum melampirkan berkas ini ke<br># laporan bug publik.<br> |

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
| Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. | Memindahkan file yang tidak diperlukan ke folder tujuan. Hapus folder itu setelah Anda yakin tidak ada yang membutuhkannya. |
| Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. | Memindahkan file yang tidak diperlukan ke sebuah folder tujuan. Anda akan memilihnya sebentar lagi. Hapus folder itu setelah Anda yakin tidak ada yang membutuhkannya. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. | Memindahkan file yang tidak diperlukan ke folder tujuan. Folder itu ada di drive yang sama, jadi ruangnya baru kembali setelah Anda menghapus folder itu atau memindahkannya ke drive lain. Anda bisa melakukannya setelah yakin tidak ada yang membutuhkannya. |
| Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. | Menghapus permanen file yang tidak diperlukan. File-file itu aman disingkirkan, dan ruangnya langsung kembali. |
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
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Hapus permanen menyingkirkan file yang tidak diperlukan. Batal menutup jendela tanpa menghapus apa pun. |
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
| Backup folder | Folder tujuan |
| Patches | Patch |
| Product details | Detail produk |
| Backup folder | Folder tujuan |
| Operation progress | Kemajuan operasi |
| Scan {InstallerFolder} again | Pindai ulang {InstallerFolder} |
| Scanning progress | Kemajuan pemindaian |
| Startup scan progress | Kemajuan pemindaian awal |
| Details, unneeded files | Detail, file yang tidak diperlukan |
| Available for cleanup. | Tersedia untuk dibersihkan. |
| Details, files left alone | Detail, file yang dibiarkan apa adanya |
| Read-only inventory. | Daftar baca-saja. |
| Sorted by {0}, ascending | Diurutkan berdasarkan {0}, menaik |
| Sorted by {0}, descending | Diurutkan berdasarkan {0}, menurun |
| Scan results | Hasil pemindaian |
| Result details | Detail hasil |
| File details | Detail file |
| Product details | Detail produk |
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
| ,  | ,  |
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
| Error: unknown argument '{0}' | Kesalahan: argumen tidak dikenal '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Kesalahan: argumen tambahan yang tidak terduga '{0}'. Jika folder tujuan Anda mengandung spasi, apit seluruh jalur dengan tanda kutip: /m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | Kesalahan: argumen tambahan tak terduga '{0}'. /s dan /d tidak menerima argumen lain, dan hanya satu flag yang bisa dipakai per proses. |
| Cancelling... | Membatalkan... |
| Cancelled. | Dibatalkan. |
| Error: unexpected failure ({0}). Details written to {1}. | Kesalahan: kegagalan tak terduga ({0}). Rincian ditulis ke {1}. |
| Error: unexpected failure ({0}). The crash log could not be written. | Kesalahan: kegagalan tak terduga ({0}). Log kerusakan tidak bisa ditulis. |
| Scanning {InstallerFolder}... | Memindai {InstallerFolder}... |
| Found {0} unneeded {1} to clean up ({2}). | Ditemukan {0} {1} tidak diperlukan untuk dibersihkan ({2}). |
| Found no unneeded files. | Tidak ditemukan file yang tidak diperlukan. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it held back the one file ({2}) it might otherwise have offered. | InstallerClean tidak bisa memastikan file mana dalam cache yang menjadi milik program-program yang terpasang di sini, jadi satu-satunya file ({2}) yang mungkin ditawarkannya ditahan. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it held back all {0} {1} ({2}) it might otherwise have offered. | InstallerClean tidak bisa memastikan file mana dalam cache yang menjadi milik program-program yang terpasang di sini, jadi seluruh {0} {1} ({2}) yang mungkin ditawarkannya ditahan. |
| Windows has a record for {0} file that is not in {InstallerFolder}: {1}. It causes no trouble day to day, but a repair, update or uninstall can fail on it. Running that program's installer again, preferably the same version, usually restores the file. | Windows punya catatan untuk {0} file yang tidak ada di {InstallerFolder}: {1}. Sehari-hari ini tidak merepotkan, tapi perbaikan, pembaruan atau pencopotan bisa gagal karenanya. Menjalankan lagi penginstal program itu, sebaiknya versi yang sama, biasanya memulihkan filenya. |
| Windows has records for {0} files that are not in {InstallerFolder}: {1}. They cause no trouble day to day, but a repair, update or uninstall can fail on them. Running each program's installer again, preferably the same version, usually restores the files. | Windows punya catatan untuk {0} file yang tidak ada di {InstallerFolder}: {1}. Sehari-hari ini tidak merepotkan, tapi perbaikan, pembaruan atau pencopotan bisa gagal karenanya. Menjalankan lagi penginstal masing-masing program, sebaiknya versi yang sama, biasanya memulihkan file-filenya. |
| InstallerClean couldn't match up everything in the Windows records, so it didn't read all of them. What it found is unaffected, but what it says about files missing from {InstallerFolder} may be short of the full picture. Running it again may pick up more. | InstallerClean tidak bisa mencocokkan semua yang ada dalam catatan Windows, jadi tidak semuanya dibaca. Yang ditemukan tidak terpengaruh, tapi keterangan tentang file yang hilang dari {InstallerFolder} bisa jadi belum lengkap. Menjalankannya lagi mungkin menemukan lebih banyak. |
| Deleting {0} unneeded {1}... | Menghapus {0} {1} yang tidak diperlukan... |
| Permanently deleted {0} unneeded {1}. | {0} {1} yang tidak diperlukan telah dihapus permanen. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Kesalahan: tujuan pemindahan tidak ditentukan. Gunakan /m JALUR. (Default yang diatur di GUI bersifat per-pengguna dan tidak berlaku untuk tugas terjadwal atau proses akun layanan.) |
| Error: destination cannot be inside the Windows Installer folder. | Kesalahan: tujuan tidak boleh berada di dalam folder Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Kesalahan: tujuan harus berupa jalur absolut lengkap. Diterima: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Kesalahan: tujuan {0} mengarah ke dalam folder sistem Windows. Pilih jalur di luar %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% dan %ProgramData%. |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | Kesalahan: ruang tidak cukup di {0}. Memindahkan file-file ini perlu {1} sedangkan yang tersedia {2}. Tidak ada yang dipindahkan. |
| Error: something is using Windows Installer right now, such as a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | Kesalahan: ada yang sedang memakai Windows Installer saat ini, misalnya pembaruan Windows atau program yang memasang diri di latar belakang. /m dan /d diblokir selama itu berjalan. Coba lagi setelah selesai. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Kesalahan: ada transaksi Windows Installer sebelumnya yang tertunda di mesin ini. Lanjutkan atau batalkan pemasangan itu (atau mulai ulang Windows) sebelum membersihkan {InstallerFolder}. |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | Kesalahan: operasi file yang diantrekan setelah mulai ulang menyasar {InstallerFolder} ({0}). Mulai ulang Windows untuk menyelesaikan operasi itu sebelum membersihkan. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Kesalahan: Windows Installer sedang mengerjakan sesuatu, jadi /m dan /d diblokir. InstallerClean tidak akan menyentuh {InstallerFolder} selagi berubah. Coba lagi setelah selesai. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | Kesalahan: InstallerClean tidak bisa mengambil kunci Windows Installer yang mencegah dua program mengubah perangkat lunak terpasang sekaligus, jadi tidak bisa memastikan sebuah file tidak menjadi diperlukan di tengah jalan. Tidak ada yang dihapus. Coba lagi, dan mulai ulang Windows kalau terus terjadi. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. Try again, and restart Windows if it keeps happening. | Kesalahan: InstallerClean tidak bisa mengambil kunci Windows Installer yang mencegah dua program mengubah perangkat lunak terpasang sekaligus, jadi tidak bisa memastikan sebuah file tidak menjadi diperlukan di tengah jalan. Tidak ada yang dipindahkan. Coba lagi, dan mulai ulang Windows kalau terus terjadi. |
| Moving {0} unneeded {1} to {2}... | Memindahkan {0} {1} yang tidak diperlukan ke {2}... |
| Moved {0} unneeded {1}. | {0} {1} yang tidak diperlukan telah dipindahkan. |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then run the command again. | InstallerClean tidak bisa lagi memastikan folder tujuan, jadi berhenti daripada menulis ke tempat yang salah. Periksa {0}, lalu jalankan perintahnya lagi. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Proses InstallerClean lain memegang kunci instans-tunggal (GUI atau proses CLI lain). Kode keluar 75 (sementara); aman untuk dicoba lagi nanti. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Catatan: penulisan ke Log Peristiwa gagal. Periksa izin log Aplikasi atau Kebijakan Grup. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - pembersihan {InstallerFolder} |
| Removes cached .msi and .msp files that no installed program still needs. | Menghapus file .msi/.msp cache yang tak lagi dibutuhkan program terpasang. |
| Needs an elevated (administrator) prompt; Windows will not start it. | Perlu prompt administrator; Windows tidak akan menjalankannya. |
| Usage: | Penggunaan: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     Tampilkan bantuan ini (juga /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  Cetak versi (juga -v) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         Pindai saja - daftar yang tak dipakai |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         Hapus permanen yang tidak diperlukan |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         Pindahkan ke folder tujuan tersimpan |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m JALUR   Pindahkan ke jalur yang ditentukan |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli menahan prompt sampai selesai, sehingga skrip atau<br>tugas terjadwal bisa menunggunya. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | Folder disimpan per pengguna; tugas terjadwal atau SYSTEM: /m JALUR. |
| Exit codes: | Kode keluar: |
|   0   success: the run did what it was asked and nothing failed |   0   berhasil: menjalankan yang diminta dan tidak ada yang gagal |
|   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |   1   gagal: tidak ada yang diproses (argumen atau tujuan salah,<br>       pemindaian gagal, atau semua file gagal) |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   sebagian: sebagian diproses, sebagian tidak (gagal atau Ctrl+C) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  sementara: kondisi sementara memblokir proses (lihat pesannya) |
|   130 cancelled (Ctrl+C) |   130 dibatalkan (Ctrl+C) |
