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
| The anonymous report | Laporan anonim |
| InstallerClean couldn't read the report. | InstallerClean tidak bisa membaca laporan. |

## Section headings

| English | Bahasa Indonesia |
| --- | --- |
| PATCHES | PATCH |
| PRODUCT DETAILS | DETAIL PRODUK |
| BACKUP FOLDER | FOLDER CADANGAN |
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
| Carry on _without it | Lanjutkan _tanpa menunggu |
| Check for _updates | Periksa pem_baruan |
| _Close | _Tutup |
| _Close without donating | _Tutup tanpa berdonasi |
| _Delete permanently | _Hapus permanen |
| _Done | _Selesai |
| Details | Detail |
| _Donate | _Donasi |
| _Donate $3 | _Donasi US$3 |
| Donate | Donasi |
| Leave a _star on GitHub | Beri _bintang di GitHub |
| Apache 2.0 licence | Lisensi Apache 2.0 |
| _Move | _Pindahkan |
| Path to folder if you move rather than delete. | Jalur ke folder jika Anda memindahkan, bukan menghapus. |
| _Re-scan | Pindai _ulang |

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
| (no program) | (tanpa program) |
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
| Checking registry for additional packages... | Memeriksa registri untuk paket tambahan... |
| Matching files against installed programs... | Mencocokkan file dengan program yang terpasang... |
| {0} of {1} | {0} dari {1} |
| Checking remaining files... | Memeriksa file yang tersisa... |
| Waiting for drive {0} to respond... | Menunggu drive {0} merespons... |
| Waiting for {0} to respond... | Menunggu {0} merespons... |
| Any file still to be checked against it will be left alone. | Setiap file yang masih harus diperiksa terhadapnya akan dibiarkan apa adanya. |
| Scan complete ({0}) | Pemindaian selesai ({0}) |
| Found {0} {1} you can safely delete. | Ditemukan {0} {1} yang aman Anda hapus. |
| Preparing destination folder... | Menyiapkan folder tujuan... |
| Moving unneeded files... | Memindahkan file yang tidak diperlukan... |
| Deleting unneeded files... | Menghapus file yang tidak diperlukan... |
| Move cancelled. {0} of {1} {2} processed. | Pemindahan dibatalkan. {0} dari {1} {2} diproses. |
| Delete cancelled. {0} of {1} {2} processed. | Penghapusan dibatalkan. {0} dari {1} {2} diproses. |
| {0}. Details are in {1}. | {0}. Detail di {1}. |
| {0}. The crash log could not be written. | {0}. Log kerusakan tidak bisa ditulis. |
| {0}. Details are in {1}. | {0}. Detail di {1}. |
| {0}. The crash log could not be written. | {0}. Log kerusakan tidak bisa ditulis. |
| Access denied. Windows refused the scan. | Akses ditolak. Windows menolak pemindaian. |
| Scan stopped. | Pemindaian dihentikan. |
| Scan cancelled. | Pemindaian dibatalkan. |
| Ready | Siap |
| Scan failed ({0}). Details in {1}. | Pemindaian gagal ({0}). Detail di {1}. |
| Scan failed ({0}). The crash log could not be written. | Pemindaian gagal ({0}). Log kerusakan tidak bisa ditulis. |

## Main screen text

| English | Bahasa Indonesia |
| --- | --- |
| Any unneeded files below are safe to delete. | File apa pun yang tidak diperlukan di bawah ini aman dihapus. |
| They sit in {InstallerFolder}. InstallerClean asks Windows about every installed program: a file is listed when no program claims it ({0}), or when a newer patch has replaced it and no program could roll back to it ({1}). | File-file itu ada di {InstallerFolder}. InstallerClean menanyakan setiap program yang terpasang kepada Windows: sebuah file masuk daftar jika tidak ada program yang mengakuinya ({0}), atau jika sebuah patch yang lebih baru telah menggantikannya dan tidak ada program yang bisa kembali kepadanya ({1}). |
| Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update and uninstall as normal. Putting them back into {InstallerFolder} restores everything. Or delete them permanently now. | Pindahkan ke folder cadangan pilihan Anda, lalu hapus folder itu setelah Anda yakin program-program Anda masih bisa diperbarui dan dicopot seperti biasa. Mengembalikannya ke {InstallerFolder} memulihkan semuanya. Atau hapus permanen sekarang. |
| Nothing scanned yet. | Belum ada yang dipindai. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Tekan Pindai ulang untuk menelusuri {InstallerFolder} mencari file penginstal yang tidak lagi diperlukan program mana pun. |
| Nothing to show until the next scan. | Tidak ada yang ditampilkan sampai pemindaian berikutnya. |
| The scan that runs after a Move or Delete didn't finish, so there's no list to show. Press Re-scan to scan {InstallerFolder} again. | Pemindaian yang berjalan setelah Pindahkan atau Hapus tidak selesai, jadi tidak ada daftar untuk ditampilkan. Tekan Pindai ulang untuk memindai {InstallerFolder} lagi. |
| What stopped it is recorded in {0}. | Apa yang menghentikannya dicatat di {0}. |
| These files can't be cleaned up right now. | File-file ini tidak bisa dibersihkan sekarang. |
| Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Ada yang sedang memakai Windows Installer saat ini, misalnya pembaruan Windows atau program yang memasang diri di latar belakang. Pindahkan dan Hapus dijeda selama itu berjalan, sehingga InstallerClean tidak menyentuh {InstallerFolder} selagi berubah. Setelah selesai, pindai ulang dan keduanya kembali aktif. |
| Windows refused InstallerClean permission to check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If Windows still refuses, this isn't a machine InstallerClean can clean. | Windows menolak izin InstallerClean untuk memeriksa apakah Windows Installer sedang sibuk, jadi Pindahkan dan Hapus dijeda dan InstallerClean tidak akan menyentuh {InstallerFolder}. Restart Windows lalu Pindai ulang. Kalau Windows tetap menolak, ini bukan komputer yang bisa dibersihkan InstallerClean. |
| InstallerClean couldn't check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If it still can't check, this isn't a machine InstallerClean can clean. | InstallerClean tidak bisa memeriksa apakah Windows Installer sedang sibuk, jadi Pindahkan dan Hapus dijeda dan InstallerClean tidak akan menyentuh {InstallerFolder}. Restart Windows lalu Pindai ulang. Kalau tetap tidak bisa memeriksa, ini bukan komputer yang bisa dibersihkan InstallerClean. |
| InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}). Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If it still can't check, this isn't a machine InstallerClean can clean. | InstallerClean tidak bisa memeriksa apakah Windows Installer sedang sibuk, karena Windows memberikan kesalahan: {0} (kesalahan {1}). Pindahkan dan Hapus dijeda dan InstallerClean tidak akan menyentuh {InstallerFolder}. Restart Windows lalu Pindai ulang. Kalau tetap tidak bisa memeriksa, ini bukan komputer yang bisa dibersihkan InstallerClean. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Ada transaksi Windows Installer sebelumnya yang tertunda di mesin ini. Lanjutkan atau batalkan pemasangan itu (atau mulai ulang Windows) sebelum membersihkan {InstallerFolder}. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows mengantrekan penggantian nama file untuk mulai ulang berikutnya yang memengaruhi {InstallerFolder}. Mulai ulang Windows sebelum membersihkan. |
| A file operation is queued for the next restart and InstallerClean can't tell which files it names, so it can't rule out that they're in {InstallerFolder}. Restart Windows before cleaning. | Ada operasi file yang mengantre untuk restart berikutnya dan InstallerClean tidak bisa mengetahui file mana saja yang disebutkannya, jadi tidak bisa memastikan file-file itu tidak ada di {InstallerFolder}. Restart Windows sebelum membersihkan. |
| InstallerClean couldn't read one of the Windows settings it checks before touching {InstallerFolder}, so it can't tell whether an installer operation is running or waiting for a restart. Restart Windows and Re-scan. If the setting still won't read, this isn't a machine InstallerClean can clean. | InstallerClean tidak bisa membaca salah satu pengaturan Windows yang diperiksanya sebelum menyentuh {InstallerFolder}, jadi tidak bisa tahu apakah ada operasi pemasangan yang sedang berjalan atau menunggu restart. Restart Windows lalu Pindai ulang. Kalau pengaturan itu tetap tidak terbaca, ini bukan komputer yang bisa dibersihkan InstallerClean. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer sedang mengerjakan sesuatu, jadi Pindahkan dan Hapus dijeda. InstallerClean tidak akan menyentuh {InstallerFolder} selagi berubah. Setelah selesai, pindai ulang dan keduanya kembali aktif. |
| Select a file to view details. | Pilih file untuk melihat detail. |
| Select a product to view details. | Pilih produk untuk melihat detail. |
| No metadata available. | Tidak ada metadata yang tersedia. |
| This installer file is missing. It causes no trouble now, and won't until the day you try to update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To put it back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | File pemasang ini hilang. Sekarang tidak menimbulkan masalah, dan tidak akan menimbulkannya sampai suatu hari Anda mencoba memperbarui atau mencopot program pemiliknya. Langkah itu bisa gagal, karena Windows mencari file ini dan tidak menemukannya.<br><br>Untuk mengembalikannya, Anda butuh pemasang versi yang sudah Anda miliki. Dapatkan dari pembuat programnya dan jalankan di atas salinan yang ada. Versi yang lebih baru tidak bisa: versi baru harus lebih dulu menghapus versi yang Anda miliki, dan justru langkah itulah yang membutuhkan file ini. Mencopot lebih dulu juga tidak berhasil, karena alasan yang sama. Ini semestinya memulihkan file itu dan membiarkan pengaturan Anda apa adanya, tetapi Microsoft tidak menjaminnya. |
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
| Nothing to clean up in {InstallerFolder} | Tidak ada yang perlu dibersihkan di {InstallerFolder} |
| Scanned {0} {1} in {2} | {0} {1} dipindai dalam {2} |
| Nothing offered on this PC | Tidak ada yang ditawarkan di PC ini |
| One file ({2}) was held back because Windows shows it was changed after InstallerClean started scanning. If a program was installing or updating, scan again once it has finished and InstallerClean will check it again. | Satu file ({2}) ditahan karena Windows menunjukkan file itu diubah setelah InstallerClean mulai memindai. Kalau ada program yang sedang dipasang atau diperbarui, pindai lagi setelah selesai dan InstallerClean akan memeriksanya lagi. |
| {0} {1} ({2}) were held back because Windows shows they were changed after InstallerClean started scanning. If a program was installing or updating, scan again once it has finished and InstallerClean will check them again. | {0} {1} ({2}) ditahan karena Windows menunjukkan file-file itu diubah setelah InstallerClean mulai memindai. Kalau ada program yang sedang dipasang atau diperbarui, pindai lagi setelah selesai dan InstallerClean akan memeriksanya lagi. |
| Delete that folder when you're satisfied all is well. | Hapus folder itu setelah Anda yakin semuanya baik-baik saja. |
| Delete that folder when you're satisfied all is well. You won't actually reclaim the space until you do. | Hapus folder itu setelah Anda yakin semuanya baik-baik saja. Ruang kosongnya baru benar-benar kembali setelah itu. |
| {0} freed | {0} dikosongkan |
| {0} moved | {0} dipindahkan |
| Nothing was moved | Tidak ada yang dipindahkan |
| Nothing was deleted | Tidak ada yang dihapus |
| {0} file could not be moved. | {0} file tidak bisa dipindahkan. |
| {0} files could not be moved. | {0} file tidak bisa dipindahkan. |
| {0} file could not be deleted. | {0} file tidak bisa dihapus. |
| {0} files could not be deleted. | {0} file tidak bisa dihapus. |
| {0} {1} moved to: {2} | {0} {1} dipindahkan ke: {2} |
| {0} {1} moved to: {2} | {0} {1} dipindahkan ke: {2} |
| {0} file held back. The scan said it was unneeded. The final check couldn't confirm that. | {0} file ditahan. Pemindaian menyebutnya tidak diperlukan. Pemeriksaan akhir tidak bisa memastikannya. |
| {0} files held back. The scan said these were unneeded. The final check couldn't confirm that. | {0} file ditahan. Pemindaian menyebutnya tidak diperlukan. Pemeriksaan akhir tidak bisa memastikannya. |
| {0} {1} kept in place, because Windows has a record of the program named inside. | {0} {1} dibiarkan di tempatnya, karena Windows punya catatan tentang program yang disebutkan di dalamnya. |
| {0} {1} kept in place, because InstallerClean couldn't find a program named inside. | {0} {1} dibiarkan di tempatnya, karena InstallerClean tidak menemukan nama program di dalamnya. |
| Moved {0} of {1} {2} to {3} before you cancelled. | {0} dari {1} {2} dipindahkan ke {3} sebelum Anda membatalkan. |
| Permanently deleted {0} of {1} {2} before you cancelled. | {0} dari {1} {2} dihapus permanen sebelum Anda membatalkan. |
| It's simple to undo. Move them back into {InstallerFolder} and everything will be back to how it was. | Mudah untuk dibatalkan. Pindahkan kembali ke {InstallerFolder} dan semuanya akan kembali seperti semula. |
| {0} {1} permanently deleted | {0} {1} dihapus permanen |
| {0} {1} permanently deleted | {0} {1} dihapus permanen |
| Any small donation is much appreciated | Donasi sekecil apa pun sangat dihargai |
| Send _anonymous report | Kirim laporan _anonim |
| The report adds your result to [this chart] on GitHub, which shows how much space people are reclaiming with InstallerClean. It also holds these technical details, to help me spot any bugs: | Laporan ini menambahkan hasil Anda ke [grafik ini] di GitHub, yang menunjukkan berapa banyak ruang yang dikosongkan orang dengan InstallerClean. Laporan ini juga memuat detail teknis berikut untuk membantu saya menemukan bug: |
| How much space was freed, and how many files were moved or deleted | Berapa banyak ruang yang dikosongkan, dan berapa banyak file yang dipindahkan atau dihapus |
| Whether a Move went to the same drive, another drive, a removable drive or a network share | Apakah Pindahkan menuju drive yang sama, drive lain, drive yang dapat dilepas, atau berbagi jaringan |
| How long the scan and the Move or Delete took | Berapa lama pemindaian serta Pindahkan atau Hapus berlangsung |
| How many installer files there are of each kind, and how much space they take | Berapa banyak file penginstal dari setiap jenis, dan berapa banyak ruang yang dipakainya |
| How many files were left alone, and why | Berapa banyak file yang dibiarkan apa adanya, dan alasannya |
| How often InstallerClean waited for a slow drive or network share, and how many it stopped waiting for | Seberapa sering InstallerClean menunggu drive atau berbagi jaringan yang lambat, dan berapa banyak yang berhenti ditunggunya |
| How many programs and patches Windows has a record of | Berapa banyak program dan patch yang dicatat Windows |
| Whether Windows still makes old-style short file names | Apakah Windows masih membuat nama file pendek gaya lama |
| Whether Windows is 10 or 11 and x64 or Arm, and which language and country or region it's set to | Apakah Windows versi 10 atau 11 dan x64 atau Arm, serta bahasa dan negara atau kawasan yang disetel di dalamnya |
| InstallerClean's version, how you got it (setup, portable, slim, Scoop, the command-line tool on its own, built from source, or not known), whether the report was sent from InstallerClean or the command-line tool, and the language it was showing | Versi InstallerClean, cara Anda mendapatkannya (setup, portable, slim, Scoop, alat baris perintah yang berdiri sendiri, dibuat dari kode sumber, atau tidak diketahui), apakah laporan dikirim dari InstallerClean atau alat baris perintah, dan bahasa yang ditampilkannya |
| Counts of anything that went wrong | Jumlah dari apa pun yang gagal |
| Each of these is a number or a label. Nothing in the report names you, your PC, your files or your programs, and I never see your IP address. | Masing-masing berupa angka atau label. Tidak ada di dalam laporan yang menyebut Anda, PC Anda, file Anda, atau program Anda, dan saya tidak pernah melihat alamat IP Anda. |
| See exactly what's sent | Lihat persis apa yang dikirim |

## Summaries and counts

| English | Bahasa Indonesia |
| --- | --- |
| {0} file left alone | {0} file dibiarkan apa adanya |
| {0} files left alone | {0} file dibiarkan apa adanya |
| {0} unneeded file to clean up | {0} file tidak diperlukan untuk dibersihkan |
| {0} unneeded files to clean up | {0} file tidak diperlukan untuk dibersihkan |
| Windows has a record for {0} file that isn't in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. Open Details for what to do. | Windows punya catatan untuk {0} file yang tidak ada di {InstallerFolder}: {1}. Sehari-hari ini tidak menimbulkan masalah, tetapi pembaruan atau pencopotan program itu bisa gagal. Buka Detail untuk tahu apa yang harus dilakukan. |
| Windows has records for {0} files that aren't in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. Open Details for what to do. | Windows punya catatan untuk {0} file yang tidak ada di {InstallerFolder}: {1}. Sehari-hari ini tidak menimbulkan masalah, tetapi pembaruan atau pencopotan program-program itu bisa gagal. Buka Detail untuk tahu apa yang harus dilakukan. |
| {0} other program | {0} program lain |
| {0} other programs | {0} program lain |
| {0} file with no program named in the records | {0} file tanpa nama program dalam catatan |
| {0} files with no program named in the records | {0} file tanpa nama program dalam catatan |
| InstallerClean carried on without drive {0} and left alone any file still to be checked against it. Once it's responding normally, Re-scan. | InstallerClean melanjutkan tanpa drive {0} dan membiarkan apa adanya setiap file yang masih harus diperiksa terhadapnya. Setelah drive itu kembali merespons dengan normal, tekan Pindai ulang. |
| InstallerClean carried on without {0} and left alone any file still to be checked against it. Once it's responding normally, Re-scan. | InstallerClean melanjutkan tanpa {0} dan membiarkan apa adanya setiap file yang masih harus diperiksa terhadapnya. Setelah lokasi itu kembali merespons dengan normal, tekan Pindai ulang. |
| InstallerClean carried on without more than one drive or share ({0}) and left alone any file still to be checked against one of them. Once they're responding normally, Re-scan. | InstallerClean melanjutkan tanpa lebih dari satu drive atau berbagi jaringan ({0}) dan membiarkan apa adanya setiap file yang masih harus diperiksa terhadap salah satunya. Setelah semuanya kembali merespons dengan normal, tekan Pindai ulang. |
| Something was using Windows Installer when the scan started, such as a Windows Update or a program installing, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan once that has finished. | Ada yang sedang memakai Windows Installer saat pemindaian dimulai, misalnya pembaruan Windows atau program yang sedang dipasang, jadi InstallerClean menahan file di {InstallerFolder} yang tidak disebut dalam catatan program terpasang mana pun, alih-alih menawarkannya. Tekan Pindai ulang setelah itu selesai. |
| A Windows Installer transaction was suspended on this machine when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Resume or roll back that install, or restart Windows, then Re-scan. | Ada transaksi Windows Installer yang tertunda di mesin ini saat pemindaian dimulai, jadi InstallerClean menahan file di {InstallerFolder} yang tidak disebut dalam catatan program terpasang mana pun, alih-alih menawarkannya. Lanjutkan atau batalkan pemasangan itu, atau mulai ulang Windows, lalu tekan Pindai ulang. |
| When the scan started, Windows had a file rename queued for the next restart that affects {InstallerFolder}, so InstallerClean held back the files there that no installed program's records name, rather than offering them. Restart Windows, then Re-scan. | Saat pemindaian dimulai, Windows telah mengantrekan penggantian nama file untuk mulai ulang berikutnya yang memengaruhi {InstallerFolder}, jadi InstallerClean menahan file di sana yang tidak disebut dalam catatan program terpasang mana pun, alih-alih menawarkannya. Mulai ulang Windows, lalu tekan Pindai ulang. |
| When the scan started, a file operation was queued for the next restart and InstallerClean couldn't tell which files it names, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows, then Re-scan. | Saat pemindaian dimulai, ada operasi file yang mengantre untuk restart berikutnya dan InstallerClean tidak bisa mengetahui file mana saja yang disebutkannya, jadi InstallerClean menahan file di {InstallerFolder} yang tidak disebut dalam catatan program terpasang mana pun, alih-alih menawarkannya. Restart Windows, lalu tekan Pindai ulang. |
| When the scan started, Windows refused InstallerClean permission to check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Saat pemindaian dimulai, Windows menolak izin InstallerClean untuk memeriksa apakah Windows Installer sedang sibuk, jadi InstallerClean menahan file di {InstallerFolder} yang tidak disebut dalam catatan program terpasang mana pun, alih-alih menawarkannya. Restart Windows lalu tekan Pindai ulang. |
| When the scan started, InstallerClean couldn't read one of the Windows settings it checks, so it couldn't tell whether an installer operation was running or waiting for a restart, and it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Saat pemindaian dimulai, InstallerClean tidak bisa membaca salah satu pengaturan Windows yang diperiksanya, jadi tidak bisa tahu apakah ada operasi pemasangan yang sedang berjalan atau menunggu restart, dan menahan file di {InstallerFolder} yang tidak disebut dalam catatan program terpasang mana pun, alih-alih menawarkannya. Restart Windows lalu tekan Pindai ulang. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Saat pemindaian dimulai, InstallerClean tidak bisa memeriksa apakah Windows Installer sedang sibuk, jadi InstallerClean menahan file di {InstallerFolder} yang tidak disebut dalam catatan program terpasang mana pun, alih-alih menawarkannya. Restart Windows lalu tekan Pindai ulang. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, because Windows gave an error: {0} (error {1}). So it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Saat pemindaian dimulai, InstallerClean tidak bisa memeriksa apakah Windows Installer sedang sibuk, karena Windows memberikan kesalahan: {0} (kesalahan {1}). Jadi InstallerClean menahan file di {InstallerFolder} yang tidak disebut dalam catatan program terpasang mana pun, alih-alih menawarkannya. Restart Windows lalu tekan Pindai ulang. |
| Windows Installer had something in progress or waiting when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan once that has cleared. | Windows Installer sedang mengerjakan atau menunggu sesuatu saat pemindaian dimulai, jadi InstallerClean menahan file di {InstallerFolder} yang tidak disebut dalam catatan program terpasang mana pun, alih-alih menawarkannya. Tekan Pindai ulang setelah itu beres. |
| This PC's clock went back or lost time while the scan was running, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan. | Jam PC ini mundur atau tertinggal selama pemindaian berjalan, jadi InstallerClean menahan file di {InstallerFolder} yang tidak disebut dalam catatan program terpasang mana pun, alih-alih menawarkannya. Tekan Pindai ulang. |
| {0} of {1} {2} | {0} dari {1} {2} |
| {0} unneeded {1} ({2}) | {0} {1} tidak diperlukan ({2}) |
| {0} file left alone ({1}) | {0} file dibiarkan apa adanya ({1}) |
| {0} files left alone ({1}) | {0} file dibiarkan apa adanya ({1}) |
| {0} missing | {0} hilang |
| {0} missing | {0} hilang |

## Confirmation dialogs

| English | Bahasa Indonesia |
| --- | --- |
| Move {0} {1} ({2})? | Pindahkan {0} {1} ({2})? |
| This file will be moved to: | File ini akan dipindahkan ke: |
| These files will be moved to: | File-file ini akan dipindahkan ke: |
| Delete {0} {1} ({2})? | Hapus {0} {1} ({2})? |
| This file will be deleted permanently. It's safe to do but if you'd like a backup, use Move instead. | File ini akan dihapus permanen. Ini aman dilakukan, tapi kalau Anda ingin cadangan, gunakan Pindahkan saja. |
| These files will be deleted permanently. It's safe to do but if you'd like a backup, use Move instead. | File-file ini akan dihapus permanen. Ini aman dilakukan, tapi kalau Anda ingin cadangan, gunakan Pindahkan saja. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | Folder itu ada di drive yang sama, jadi ruangnya belum kembali sampai Anda menghapusnya. Pilih folder di drive lain kalau Anda ingin ruangnya langsung kembali. |

## Error messages

| English | Bahasa Indonesia |
| --- | --- |
| This is also recorded in {0}. | Ini juga dicatat di {0}. |
| Access denied | Akses ditolak |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows menolak akses untuk InstallerClean, jadi prosesnya dihentikan. Tidak ada yang dihapus.<br><br>InstallerClean sudah berjalan sebagai administrator, jadi menjalankannya lagi dengan cara itu tidak akan membantu. Windows tidak menjelaskan lebih jauh apa yang menolak akses, jadi tidak ada hal khusus yang bisa dicoba. |
| InstallerClean stopped | InstallerClean berhenti |
| Scan failed | Pemindaian gagal |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed. | Catatan Windows Installer kembali sepenuhnya kosong: tidak satu pun program terpasang atau pembaruan yang mengklaim file pemasang di cache. Itu tidak terjadi pada komputer yang berfungsi (bahkan pemasangan Windows yang baru pun punya beberapa), jadi catatannya rusak atau tidak bisa dibaca, dan pemindaian yang memercayai jawaban ini akan keliru menyebut setiap file di {InstallerFolder} terisolasi. InstallerClean berhenti sebagai gantinya. Tidak ada yang dihapus. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer tidak mengizinkan InstallerClean menampilkan daftar apa saja yang terpasang. InstallerClean sudah berjalan sebagai administrator, jadi menjalankannya lagi sebagai administrator tidak akan mengubah apa pun. Tanpa daftar itu tidak ada cara yang aman untuk mengetahui file cache mana yang masih diperlukan, jadi InstallerClean berhenti. Tidak ada yang dihapus. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: it read {1} {2}, then an entry came back unreadable (error code {0}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer tidak bisa memberi InstallerClean daftar program terpasang yang terbaca: InstallerClean membaca {1} {2}, lalu satu entri kembali tidak terbaca (kode kesalahan {0}). Alih-alih bekerja dengan daftar yang hanya terbaca sebagian, InstallerClean berhenti. Tidak ada yang dihapus. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer tidak pernah menandai akhir daftar program terpasang: InstallerClean membaca {2} {3}, lalu menyerah setelah {0} entri (kode kesalahan terakhir {1}). Daftar yang tidak berujung tidak bisa dipercaya, jadi InstallerClean berhenti. Tidak ada yang dihapus. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer tidak pernah menandai akhir daftar patch sebuah program: InstallerClean membaca {2} {3}, lalu menyerah setelah {0} entri (kode kesalahan terakhir {1}). Daftar yang tidak berujung tidak bisa dipercaya, jadi InstallerClean berhenti. Tidak ada yang dihapus. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean tidak bisa mencocokkan catatan Windows Installer dengan isi {InstallerFolder}. Hampir tidak ada yang ditunjuk catatan itu benar-benar ada di sana, dan hampir tidak ada yang ada di sana disebut oleh catatan mana pun, jadi tidak ada file yang bisa ditunjukkan tidak diperlukan. Tidak ada yang ditawarkan dan tidak ada yang disingkirkan. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean tidak bisa mencocokkan catatan Windows Installer dengan isi {InstallerFolder}. Folder itu berisi file, tapi tidak satu pun catatan menunjuk apa pun di dalamnya, jadi tidak ada file yang bisa ditunjukkan tidak diperlukan. Tidak ada yang ditawarkan dan tidak ada yang disingkirkan. |
| InstallerClean can't scan {InstallerFolder} on this PC. Windows reports that folder as {0}, and InstallerClean only scans an Installer folder that Windows reports at its own path. Nothing has been offered and nothing has been removed. | InstallerClean tidak bisa memindai {InstallerFolder} di PC ini. Windows melaporkan folder itu sebagai {0}, dan InstallerClean hanya memindai folder Installer yang dilaporkan Windows di jalurnya sendiri. Tidak ada yang ditawarkan dan tidak ada yang disingkirkan. |
| Windows reports that {InstallerFolder} doesn't exist on this PC, so InstallerClean stopped. Nothing has been offered and nothing has been removed. | Windows melaporkan bahwa {InstallerFolder} tidak ada di PC ini, jadi InstallerClean berhenti. Tidak ada yang ditawarkan dan tidak ada yang disingkirkan. |
| Windows reported an error when InstallerClean tried to list the files in {InstallerFolder}, so it couldn't get a complete list and stopped. Nothing has been offered and nothing has been removed. | Windows melaporkan kesalahan saat InstallerClean mencoba membuat daftar file di {InstallerFolder}, jadi InstallerClean tidak bisa mendapatkan daftar lengkapnya dan berhenti. Tidak ada yang ditawarkan dan tidak ada yang disingkirkan. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean tidak bisa membaca cukup banyak catatan Windows Installer untuk memastikan apa yang masih diperlukan: daftar program terpasang kembali tidak lengkap, dan membaca catatan yang sama langsung dari registri juga menemui kesalahan. Sebuah file bisa tampak terisolasi hanya karena catatan yang menyebutkannya termasuk yang tidak terbaca, jadi InstallerClean berhenti. Tidak ada yang dihapus. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean tidak berhasil membuat Windows menguraikan jalur sebenarnya dari {InstallerFolder}, jadi tidak ada file yang bisa ditunjukkan berada di dalamnya dan tidak ada yang ditawarkan untuk dibersihkan. Pemindaian ini tidak menemukan apa pun karena pemeriksaan itu gagal, bukan karena foldernya bersih. Tidak ada yang disingkirkan. |
| Nothing was deleted | Tidak ada yang dihapus |
| Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. | Windows menolak izin InstallerClean untuk memeriksa apakah Windows Installer sedang sibuk, jadi tidak bisa mengesampingkan bahwa sebuah file menjadi diperlukan di tengah jalan, dan tidak ada yang dihapus. |
| Nothing was moved | Tidak ada yang dipindahkan |
| Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. | Windows menolak izin InstallerClean untuk memeriksa apakah Windows Installer sedang sibuk, jadi tidak bisa mengesampingkan bahwa sebuah file menjadi diperlukan di tengah jalan, dan tidak ada yang dipindahkan. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | InstallerClean tidak bisa mengambil kunci yang dipakai Windows Installer untuk mencegah dua program mengubah perangkat lunak terpasang sekaligus, jadi tidak bisa memastikan sebuah file tidak menjadi diperlukan di tengah jalan, dan tidak ada yang dihapus. Coba lagi, dan mulai ulang Windows kalau terus terjadi. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. Try again, and restart Windows if it keeps happening. | InstallerClean tidak bisa mengambil kunci yang dipakai Windows Installer untuk mencegah dua program mengubah perangkat lunak terpasang sekaligus, jadi tidak bisa memastikan sebuah file tidak menjadi diperlukan di tengah jalan, dan tidak ada yang dipindahkan. Coba lagi, dan mulai ulang Windows kalau terus terjadi. |
| Invalid destination | Tujuan tidak valid |
| Move stopped | Pemindahan dihentikan |
| Couldn't use that backup folder | Tidak bisa memakai folder cadangan itu |
| Move failed | Pemindahan gagal |
| Delete failed | Penghapusan gagal |
| Setting not saved | Pengaturan tidak tersimpan |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | Perubahan tidak dapat disimpan. Saat berikutnya dijalankan, InstallerClean akan kembali ke pengaturan sebelumnya. |
| The destination cannot be inside the Windows Installer folder. | Tujuan tidak boleh berada di dalam folder Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Tujuan {0} mengarah ke dalam folder sistem Windows. Pilih jalur di luar %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% dan %ProgramData%. |
| Not enough space | Ruang tidak cukup |
| There isn't room at {0}<br><br>Required: {1}<br>Available: {2} | Tidak cukup tempat di {0}<br><br>Diperlukan: {1}<br>Tersedia: {2} |
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
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | Folder cadangan harus berupa jalur lengkap ke sebuah folder, diawali huruf drive atau berbagi jaringan (misalnya D:\Backup, atau \\server\backup). InstallerClean tidak bisa memakai yang ini: {0} |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then Re-scan and try again. | InstallerClean tidak bisa lagi memastikan folder cadangan, jadi berhenti. Periksa {0}, lalu Pindai ulang dan coba lagi. |
| Cannot write to {0}. | Tidak bisa menulis ke {0}. |
| You can save files in {0} but not open them again, so InstallerClean won't move files there. | Anda bisa menyimpan file di {0}, tetapi tidak bisa membukanya lagi, jadi InstallerClean tidak akan memindahkan file ke sana. |
| A file called '{0}' is already in the backup folder. | File bernama '{0}' sudah ada di folder cadangan. |

## Update check

| English | Bahasa Indonesia |
| --- | --- |
| Check for updates | Periksa pembaruan |
| Checking... | Memeriksa... |
| Up to date. | Sudah versi terbaru. |
| InstallerClean {0} is available. | InstallerClean {0} tersedia. |
| It's strongly recommended to use the latest version. | Sangat disarankan untuk memakai versi terbaru. |
| Download it from GitHub | Unduh dari GitHub |
| Download .NET Framework 4.8 from Microsoft | Unduh .NET Framework 4.8 dari Microsoft |
| Update now | Perbarui sekarang |
| This copy was installed by Scoop. To update it, close InstallerClean and run: | Salinan ini dipasang oleh Scoop. Untuk memperbaruinya, tutup InstallerClean lalu jalankan: |
| This copy was installed by Scoop for all users. To update it, close InstallerClean, open a terminal as administrator and run: | Salinan ini dipasang oleh Scoop untuk semua pengguna. Untuk memperbaruinya, tutup InstallerClean, buka terminal sebagai administrator, lalu jalankan: |
| It needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then open InstallerClean to update. | Versi ini memerlukan .NET Framework 4.8. Pasang dari Microsoft, mulai ulang PC, lalu buka InstallerClean untuk memperbarui. |
| InstallerClean {0} can't be installed automatically. | InstallerClean {0} tidak dapat dipasang secara otomatis. |
| InstallerClean can't tell whether its setup installed it, so it can't update itself. | InstallerClean tidak dapat memastikan apakah dirinya dipasang oleh program penyiapannya, jadi tidak dapat memperbarui dirinya sendiri. |
| Copied | Tersalin |
| Couldn't copy it. Select the command and copy it yourself. | Tidak dapat menyalinnya. Pilih perintahnya lalu salin sendiri. |
| _Continue without updating | _Lanjutkan tanpa memperbarui |
| Downloading InstallerClean {0}... | Mengunduh InstallerClean {0}... |
| Checking the download... | Memeriksa unduhan... |
| The update couldn't be downloaded. | Pembaruan tidak dapat diunduh. |
| The update couldn't be installed. | Pembaruan tidak dapat dipasang. |
| Couldn't reach GitHub. Check your internet connection and try again. | Tidak bisa menjangkau GitHub. Periksa koneksi internet Anda dan coba lagi. |
| GitHub returned an error response. Try again in a few minutes. | GitHub mengembalikan respons kesalahan. Coba lagi dalam beberapa menit. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | Respons GitHub tidak memuat rilis yang dikenali. Coba lagi nanti, atau buka halaman rilis langsung. |
| The check timed out. Your connection to GitHub may be slow; try again. | Pemeriksaan kehabisan waktu. Koneksi Anda ke GitHub mungkin lambat; coba lagi. |
| The check failed for an unknown reason. Details are in {0} if you need to report it. | Pemeriksaan gagal karena alasan yang tidak diketahui. Detailnya ada di {0} jika Anda perlu melaporkannya. |
| The check failed for an unknown reason. The crash log could not be written. | Pemeriksaan gagal karena alasan yang tidak diketahui. Log kerusakan tidak bisa ditulis. |

## Opening links in your browser

| English | Bahasa Indonesia |
| --- | --- |
| Couldn't open your browser | Tidak bisa membuka browser Anda |
| The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | Tautannya ada di papan klip, jadi Anda bisa menempelkannya sendiri:<br><br>{0} |
| InstallerClean couldn't copy the link to your clipboard either, so here it is:<br><br>{0} | InstallerClean juga tidak bisa menyalin tautannya ke papan klip, jadi ini dia:<br><br>{0} |

## Startup and crashes

| English | Bahasa Indonesia |
| --- | --- |
| InstallerClean | InstallerClean |
| It's already running. | Sudah berjalan. |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | Terjadi kesalahan tak terduga dan InstallerClean perlu ditutup.<br><br>{0}<br><br>Detail ditulis ke:<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | Terjadi kesalahan tak terduga dan InstallerClean perlu ditutup.<br><br>{0}<br><br>Log kerusakan tidak bisa ditulis. |
| Startup error | Kesalahan saat memulai |
| Failed to start ({0}). Details written to:<br>{1} | Gagal memulai ({0}). Detail ditulis ke:<br>{1} |
| Failed to start ({0}). The crash log could not be written. | Gagal memulai ({0}). Log kerusakan tidak bisa ditulis. |
| Administrator rights needed | Perlu hak administrator |
| InstallerClean needs administrator rights to see every program installed on this PC, and it isn't running as administrator, so it won't scan. Nothing has been changed.<br><br>Run InstallerClean again as administrator. | InstallerClean perlu hak administrator untuk melihat setiap program yang terpasang di PC ini, dan saat ini tidak berjalan sebagai administrator, jadi tidak akan memindai. Tidak ada yang diubah.<br><br>Jalankan InstallerClean lagi sebagai administrator. |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log merekam eksepsi tak tertangani dari InstallerClean.<br># Dengan hak tinggi, pesan eksepsi framework bisa memuat jalur file<br># dari sesi yang berjalan (termasuk profil pengguna lain yang didata<br># oleh kueri Windows Installer). Pesan kegagalan jaringan dari<br># pemeriksaan pembaruan atau pengiriman log hasil bisa memuat URL<br># tujuan serta alamat IP atau proksi yang teruraikan. Entri tentang<br># catatan Windows Installer yang tak terbaca bisa memuat SID akun<br># Windows (S-1-5-21-...) dan kode produk perangkat lunak terpasang.<br># Hapus ketiga jenis rincian itu sebelum melampirkan berkas ini ke<br># laporan bug publik.<br> |

## Tooltips (hover text)

| English | Bahasa Indonesia |
| --- | --- |
| Any small donation is much appreciated | Donasi sekecil apa pun sangat dihargai |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Pembatalan diminta. InstallerClean sedang menunggu langkah yang berjalan mencapai titik berhenti. Ini bisa memakan waktu beberapa detik saat I/O berat atau panggilan basis data MSI. |
| Close | Tutup |
| I really should charge for InstallerClean - because it's been a huge amount of work - but I want to keep it free so it helps as many people as possible. Your donation makes it worthwhile and is very much appreciated. | Sebenarnya saya seharusnya memungut biaya untuk InstallerClean, karena pembuatannya memakan banyak sekali tenaga, tetapi saya ingin tetap menggratiskannya agar bisa membantu sebanyak mungkin orang. Donasi Anda membuat semua itu sepadan dan sangat saya hargai. |
| A star helps other people find it | Bintang membantu orang lain menemukan InstallerClean |
| Minimise | Kecilkan |
| Move the unneeded files to the backup folder | Memindahkan file yang tidak diperlukan ke folder cadangan |
| Move the unneeded files to a backup folder. You'll choose it next. | Memindahkan file yang tidak diperlukan ke sebuah folder cadangan. Anda akan memilihnya sebentar lagi. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder. | Memindahkan file yang tidak diperlukan ke folder cadangan. Folder itu ada di drive yang sama, jadi ruang kosongnya baru kembali setelah Anda menghapus folder itu. |
| Delete the unneeded files permanently. Use Move instead if you'd like a chance to satisfy yourself all is well. | Menghapus permanen file yang tidak diperlukan. Gunakan Pindahkan saja kalau Anda ingin kesempatan meyakinkan diri bahwa semuanya baik-baik saja. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Nama subjek dari sertifikat Authenticode yang disematkan. Rantai sertifikat tidak diverifikasi. |
| Change language. The program will restart. | Ganti bahasa. Program akan dimulai ulang. |

## Screen reader labels

| English | Bahasa Indonesia |
| --- | --- |
| Donate | Donasi |
| Donate $3 | Donasi US$3 |
| Donate | Donasi |
| Cancel operation | Batal, operasi |
| Cancel scan | Batal, pemindaian |
| Cancel startup scan | Batal, pemindaian awal |
| Carry on without it, drive {0} | Lanjutkan tanpa menunggu, drive {0} |
| Carry on without it, {0} | Lanjutkan tanpa menunggu, {0} |
| Close | Tutup |
| Close window | Tutup jendela |
| Close result and return to main window | Tutup hasil dan kembali ke jendela utama |
| Leave a star on github | Beri bintang di github |
| Minimise | Kecilkan |
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Hapus permanen menyingkirkan file yang tidak diperlukan. Batal menutup jendela tanpa menghapus apa pun. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Pindahkan menaruh file yang tidak diperlukan di folder tujuan yang dipilih. Batal membiarkannya di tempatnya. |
| Say thanks | Ucapkan terima kasih |
| Check for updates | Periksa pembaruan |
| Checks github's releases page for a newer version. | Memeriksa halaman rilis github untuk mencari versi yang lebih baru. |
| Opens the readme on github in your browser. | Membuka readme di github melalui browser Anda. |
| Opens the issue tracker on github.com in your browser. | Membuka pelacak masalah (Issues) di github.com melalui browser Anda. |
| If ticked, InstallerClean checks github for a newer version when you run it. | Jika dicentang, InstallerClean memeriksa apakah ada versi yang lebih baru di github saat Anda menjalankannya. |
| Download it from github | Unduh dari github |
| Download .NET Framework 4.8 from Microsoft | Unduh .NET Framework 4.8 dari Microsoft |
| Copy the command | Salin perintah |
| Scoop's update command | Perintah pembaruan Scoop |
| Opens the licence file on github.com in your browser. | Membuka file lisensi di github.com melalui browser Anda. |
| Backup folder | Folder cadangan |
| Patches | Patch |
| Product details | Detail produk |
| Backup folder | Folder cadangan |
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
| Waiting for a drive or share | Menunggu drive atau berbagi jaringan |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | File yang tidak dapat diproses |
| Explains this folder, and how to recover a file, in the README | Menjelaskan folder ini, dan cara memulihkan file, di README |
| {0} Press Alt+{1} to carry on without it. {2} | {0} Tekan Alt+{1} untuk melanjutkan tanpa menunggu. {2} |
| What the report holds | Isi laporan |
| What the report holds | Isi laporan |
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
| drive {0} | drive {0} |
| .  | .  |
| {0} TB | {0} TB |
| {0} GB | {0} GB |
| {0} MB | {0} MB |
| {0} KB | {0} KB |
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
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({2}) rather than offering it. | InstallerClean tidak bisa memastikan file mana dalam cache yang menjadi milik program-program yang terpasang di sini, jadi satu-satunya file ({2}) itu ditahan alih-alih ditawarkan. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} ({2}) rather than offering them. | InstallerClean tidak bisa memastikan file mana dalam cache yang menjadi milik program-program yang terpasang di sini, jadi {0} {1} ({2}) ditahan alih-alih ditawarkan. |
| InstallerClean couldn't establish that one file is unneeded, so it has held it back ({2}) rather than offering it. | InstallerClean tidak bisa membuktikan bahwa satu file tidak diperlukan, jadi file itu ({2}) ditahan alih-alih ditawarkan. |
| InstallerClean couldn't establish that {0} {1} are unneeded, so it has held them back ({2}) rather than offering them. | InstallerClean tidak bisa membuktikan bahwa {0} {1} tidak diperlukan, jadi file-file itu ({2}) ditahan alih-alih ditawarkan. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({2}) rather than offering it. | InstallerClean tidak bisa memastikan file mana dalam cache yang menjadi milik program-program yang terpasang di sini, jadi satu-satunya file ({2}) itu ditahan alih-alih ditawarkan. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} ({2}) rather than offering them. | InstallerClean tidak bisa memastikan file mana dalam cache yang menjadi milik program-program yang terpasang di sini, jadi {0} {1} ({2}) ditahan alih-alih ditawarkan. |
| InstallerClean couldn't establish that one file is unneeded, so it has held it back ({2}) rather than offering it. | InstallerClean tidak bisa membuktikan bahwa satu file tidak diperlukan, jadi file itu ({2}) ditahan alih-alih ditawarkan. |
| InstallerClean couldn't establish that {0} {1} are unneeded, so it has held them back ({2}) rather than offering them. | InstallerClean tidak bisa membuktikan bahwa {0} {1} tidak diperlukan, jadi file-file itu ({2}) ditahan alih-alih ditawarkan. |
| Why it couldn't be certain: | Kenapa tidak bisa dipastikan: |
|   A file path in Windows Installer's own records wouldn't resolve, so nothing could be matched to it. |   Sebuah jalur file dalam catatan Windows Installer sendiri tidak bisa diuraikan, jadi tidak ada yang bisa dicocokkan dengannya. |
|   A file Windows has a record of couldn't be identified, so it couldn't be matched to what's in the folder. |   Sebuah file yang dicatat Windows tidak bisa dikenali, jadi file itu tidak bisa dicocokkan dengan isi folder. |
|   A program may be installed more than once on this PC, and the records can't say which copy a file belongs to. |   Sebuah program mungkin terpasang lebih dari sekali di PC ini, dan catatan tidak bisa menyebutkan sebuah file milik salinan yang mana. |
|   Part of Windows Installer's records wouldn't read, so a file could belong to a program recorded there. |   Sebagian catatan Windows Installer tidak bisa dibaca, jadi sebuah file bisa saja milik program yang tercatat di sana. |
|   A file in the folder couldn't be identified, so it couldn't be matched against the records. |   Sebuah file dalam folder tidak bisa dikenali, jadi file itu tidak bisa dicocokkan dengan catatan. |
|   Either a file wouldn't say which program it belongs to, or Windows wouldn't give a clear answer about that program. |   Entah sebuah file tidak menyebutkan ia milik program mana, atau Windows tidak memberi jawaban yang jelas tentang program itu. |
|   A check on which programs the files belong to gave answers that didn't line up with the files it was handed. |   Pemeriksaan tentang file-file itu milik program mana memberi jawaban yang tidak cocok dengan file-file yang diserahkan kepadanya. |
|   Either a patch file wouldn't say which patch it is, or it wouldn't say which programs it's for, or Windows wouldn't say in full which programs have that patch. |   Entah sebuah file patch tidak menyebutkan ia patch yang mana, atau file itu tidak menyebutkan ia ditujukan untuk program mana saja, atau Windows tidak menyebutkan secara lengkap program mana saja yang memiliki patch itu. |
| Windows has a record for {0} file that is not in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. To put the file back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Windows punya catatan untuk {0} file yang tidak ada di {InstallerFolder}: {1}. Sehari-hari ini tidak menimbulkan masalah, tetapi pembaruan atau pencopotan program itu bisa gagal. Untuk mengembalikan file itu, Anda butuh pemasang versi yang sudah Anda miliki. Dapatkan dari pembuat programnya dan jalankan di atas salinan yang ada. Versi yang lebih baru tidak bisa: versi baru harus lebih dulu menghapus versi yang Anda miliki, dan justru langkah itulah yang membutuhkan file ini. Mencopot lebih dulu juga tidak berhasil, karena alasan yang sama. Ini semestinya memulihkan file itu dan membiarkan pengaturan Anda apa adanya, tetapi Microsoft tidak menjaminnya. |
| Windows has records for {0} files that are not in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. To put a file back, you need the installer for the version you already have of that program. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs the file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Windows punya catatan untuk {0} file yang tidak ada di {InstallerFolder}: {1}. Sehari-hari ini tidak menimbulkan masalah, tetapi pembaruan atau pencopotan program-program itu bisa gagal. Untuk mengembalikan sebuah file, Anda butuh pemasang versi program itu yang sudah Anda miliki. Dapatkan dari pembuat programnya dan jalankan di atas salinan yang ada. Versi yang lebih baru tidak bisa: versi baru harus lebih dulu menghapus versi yang Anda miliki, dan justru langkah itulah yang membutuhkan file itu. Mencopot lebih dulu juga tidak berhasil, karena alasan yang sama. Ini semestinya memulihkan file itu dan membiarkan pengaturan Anda apa adanya, tetapi Microsoft tidak menjaminnya. |
| InstallerClean couldn't be certain that the one superseded file is no longer needed, so it has held it back. | InstallerClean tidak bisa memastikan bahwa satu-satunya file yang digantikan itu sudah tidak diperlukan, jadi file itu ditahan. |
| InstallerClean couldn't be certain that {0} superseded files are no longer needed, so it has held them back. | InstallerClean tidak bisa memastikan bahwa {0} file yang digantikan sudah tidak diperlukan, jadi file-file itu ditahan. |
| InstallerClean stopped waiting for drive {0} and left alone any file still to be checked against it. Once it's responding normally, run the command again. | InstallerClean berhenti menunggu drive {0} dan membiarkan apa adanya setiap file yang masih harus diperiksa terhadapnya. Setelah drive itu kembali merespons dengan normal, jalankan perintahnya lagi. |
| InstallerClean stopped waiting for {0} and left alone any file still to be checked against it. Once it's responding normally, run the command again. | InstallerClean berhenti menunggu {0} dan membiarkan apa adanya setiap file yang masih harus diperiksa terhadapnya. Setelah lokasi itu kembali merespons dengan normal, jalankan perintahnya lagi. |
| InstallerClean stopped waiting for more than one drive or share ({0}) and left alone any file still to be checked against one of them. Once they're responding normally, run the command again. | InstallerClean berhenti menunggu lebih dari satu drive atau berbagi jaringan ({0}) dan membiarkan apa adanya setiap file yang masih harus diperiksa terhadap salah satunya. Setelah semuanya kembali merespons dengan normal, jalankan perintahnya lagi. |
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
| Error: a file operation is queued for the next restart and InstallerClean can't tell which files it names, so it can't rule out {InstallerFolder}. Restart Windows before cleaning. | Kesalahan: ada operasi file yang mengantre untuk restart berikutnya dan InstallerClean tidak bisa mengetahui file mana saja yang disebutkannya, jadi tidak bisa mengesampingkan {InstallerFolder}. Restart Windows sebelum membersihkan. |
| Error: InstallerClean couldn't read one of the registry values it checks before touching {InstallerFolder}, so it can't rule out a Windows Installer operation in flight or queued for the next restart. /m and /d are blocked. Restart Windows and try again. If the read still fails, this isn't a machine InstallerClean can clean. | Kesalahan: InstallerClean tidak bisa membaca salah satu nilai registri yang diperiksanya sebelum menyentuh {InstallerFolder}, jadi tidak bisa mengesampingkan operasi Windows Installer yang sedang berjalan atau mengantre untuk restart berikutnya. /m dan /d diblokir. Restart Windows lalu coba lagi. Kalau pembacaannya tetap gagal, ini bukan komputer yang bisa dibersihkan InstallerClean. |
| Error: InstallerClean couldn't check whether Windows Installer is busy, so it can't rule out an installation in progress. /m and /d are blocked. Restart Windows and try again. If the check still fails, this isn't a machine InstallerClean can clean. | Kesalahan: InstallerClean tidak bisa memeriksa apakah Windows Installer sedang sibuk, jadi tidak bisa mengesampingkan pemasangan yang sedang berjalan. /m dan /d diblokir. Restart Windows lalu coba lagi. Kalau pemeriksaannya tetap gagal, ini bukan komputer yang bisa dibersihkan InstallerClean. |
| Error: InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}), so it can't rule out an installation in progress. /m and /d are blocked. Restart Windows and try again. If the check still fails, this isn't a machine InstallerClean can clean. | Kesalahan: InstallerClean tidak bisa memeriksa apakah Windows Installer sedang sibuk, karena Windows memberikan kesalahan: {0} (kesalahan {1}), jadi tidak bisa mengesampingkan pemasangan yang sedang berjalan. /m dan /d diblokir. Restart Windows lalu coba lagi. Kalau pemeriksaannya tetap gagal, ini bukan komputer yang bisa dibersihkan InstallerClean. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Kesalahan: Windows Installer sedang mengerjakan sesuatu, jadi /m dan /d diblokir. InstallerClean tidak akan menyentuh {InstallerFolder} selagi berubah. Coba lagi setelah selesai. |
| Something was using Windows Installer when the scan started, such as a Windows Update or a program installing, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again once that has finished. | Ada yang sedang memakai Windows Installer saat pemindaian dimulai, misalnya pembaruan Windows atau program yang sedang dipasang, jadi InstallerClean menahan file di {InstallerFolder} yang tidak disebut dalam catatan program terpasang mana pun, alih-alih menawarkannya. Jalankan perintahnya lagi setelah itu selesai. |
| A Windows Installer transaction was suspended on this machine when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Resume or roll back that install, or restart Windows, then run it again. | Ada transaksi Windows Installer yang tertunda di mesin ini saat pemindaian dimulai, jadi InstallerClean menahan file di {InstallerFolder} yang tidak disebut dalam catatan program terpasang mana pun, alih-alih menawarkannya. Lanjutkan atau batalkan pemasangan itu, atau mulai ulang Windows, lalu jalankan perintahnya lagi. |
| When the scan started, Windows had a file rename queued for the next restart that affects {InstallerFolder}, so InstallerClean held back the files there that no installed program's records name, rather than offering them. Restart Windows, then run it again. | Saat pemindaian dimulai, Windows telah mengantrekan penggantian nama file untuk mulai ulang berikutnya yang memengaruhi {InstallerFolder}, jadi InstallerClean menahan file di sana yang tidak disebut dalam catatan program terpasang mana pun, alih-alih menawarkannya. Mulai ulang Windows, lalu jalankan perintahnya lagi. |
| When the scan started, a file operation was queued for the next restart and InstallerClean couldn't tell which files it names, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows, then run it again. | Saat pemindaian dimulai, ada operasi file yang mengantre untuk restart berikutnya dan InstallerClean tidak bisa mengetahui file mana saja yang disebutkannya, jadi InstallerClean menahan file di {InstallerFolder} yang tidak disebut dalam catatan program terpasang mana pun, alih-alih menawarkannya. Restart Windows, lalu jalankan perintahnya lagi. |
| When the scan started, Windows refused InstallerClean permission to check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Saat pemindaian dimulai, Windows menolak izin InstallerClean untuk memeriksa apakah Windows Installer sedang sibuk, jadi InstallerClean menahan file di {InstallerFolder} yang tidak disebut dalam catatan program terpasang mana pun, alih-alih menawarkannya. Restart Windows lalu jalankan perintahnya lagi. |
| When the scan started, InstallerClean couldn't read one of the Windows settings it checks, so it couldn't tell whether an installer operation was running or waiting for a restart, and it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Saat pemindaian dimulai, InstallerClean tidak bisa membaca salah satu pengaturan Windows yang diperiksanya, jadi tidak bisa tahu apakah ada operasi pemasangan yang sedang berjalan atau menunggu restart, dan menahan file di {InstallerFolder} yang tidak disebut dalam catatan program terpasang mana pun, alih-alih menawarkannya. Restart Windows lalu jalankan perintahnya lagi. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Saat pemindaian dimulai, InstallerClean tidak bisa memeriksa apakah Windows Installer sedang sibuk, jadi InstallerClean menahan file di {InstallerFolder} yang tidak disebut dalam catatan program terpasang mana pun, alih-alih menawarkannya. Restart Windows lalu jalankan perintahnya lagi. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, because Windows gave an error: {0} (error {1}). So it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Saat pemindaian dimulai, InstallerClean tidak bisa memeriksa apakah Windows Installer sedang sibuk, karena Windows memberikan kesalahan: {0} (kesalahan {1}). Jadi InstallerClean menahan file di {InstallerFolder} yang tidak disebut dalam catatan program terpasang mana pun, alih-alih menawarkannya. Restart Windows lalu jalankan perintahnya lagi. |
| Windows Installer had something in progress or waiting when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again once that has cleared. | Windows Installer sedang mengerjakan atau menunggu sesuatu saat pemindaian dimulai, jadi InstallerClean menahan file di {InstallerFolder} yang tidak disebut dalam catatan program terpasang mana pun, alih-alih menawarkannya. Jalankan perintahnya lagi setelah itu beres. |
| This PC's clock went back or lost time while the scan was running, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again. | Jam PC ini mundur atau tertinggal selama pemindaian berjalan, jadi InstallerClean menahan file di {InstallerFolder} yang tidak disebut dalam catatan program terpasang mana pun, alih-alih menawarkannya. Jalankan perintahnya lagi. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | Kesalahan: InstallerClean tidak bisa mengambil kunci Windows Installer yang mencegah dua program mengubah perangkat lunak terpasang sekaligus, jadi tidak bisa memastikan sebuah file tidak menjadi diperlukan di tengah jalan. Tidak ada yang dihapus. Coba lagi, dan mulai ulang Windows kalau terus terjadi. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. Try again, and restart Windows if it keeps happening. | Kesalahan: InstallerClean tidak bisa mengambil kunci Windows Installer yang mencegah dua program mengubah perangkat lunak terpasang sekaligus, jadi tidak bisa memastikan sebuah file tidak menjadi diperlukan di tengah jalan. Tidak ada yang dipindahkan. Coba lagi, dan mulai ulang Windows kalau terus terjadi. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. | Kesalahan: Windows menolak izin InstallerClean untuk memeriksa apakah Windows Installer sedang sibuk, jadi tidak bisa mengesampingkan bahwa sebuah file menjadi diperlukan di tengah jalan. Tidak ada yang dihapus. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. | Kesalahan: Windows menolak izin InstallerClean untuk memeriksa apakah Windows Installer sedang sibuk, jadi tidak bisa mengesampingkan bahwa sebuah file menjadi diperlukan di tengah jalan. Tidak ada yang dipindahkan. |
| Moving {0} unneeded {1} to {2}... | Memindahkan {0} {1} yang tidak diperlukan ke {2}... |
| Moved {0} unneeded {1}. | {0} {1} yang tidak diperlukan telah dipindahkan. |
| Check that your programs still update and uninstall as normal, then delete {0}. | Pastikan program Anda masih bisa diperbarui dan dicopot seperti biasa, lalu hapus {0}. |
| InstallerClean is free. If it helped, please donate $3: {0} | InstallerClean gratis. Jika bermanfaat, silakan berdonasi US$3: {0} |
| Update now? It's strongly recommended to use the latest version. [{0}/{1}] | Perbarui sekarang? Sangat disarankan untuk memakai versi terbaru. [{0}/{1}] |
| Y | Y |
| N | T |
| No answer after a minute, so InstallerClean updates without asking, as it does when nobody is there to answer. | Tidak ada jawaban setelah satu menit, jadi InstallerClean memperbarui tanpa bertanya, seperti saat tidak ada orang yang dapat menjawab. |
| Send an anonymous report of this run? It is saved at {0}. Type ? to see what it holds. [{1}/{2}/?] | Kirim laporan anonim dari proses ini? Disimpan di {0}. Ketik ? untuk melihat isinya. [{1}/{2}/?] |
| the chart at {0} | grafik di {0} |
| The report adds your result to {0}, which shows how much space people are reclaiming with InstallerClean. It also holds these technical details, to help me spot any bugs: | Laporan ini menambahkan hasil Anda ke {0}, yang menunjukkan berapa banyak ruang yang dikosongkan orang dengan InstallerClean. Laporan ini juga memuat detail teknis berikut untuk membantu saya menemukan bug: |
| The full report is printed below. | Laporan lengkap dicetak di bawah ini. |
| Report sent. Thank you. | Laporan terkirim. Terima kasih. |
| Report not sent. | Laporan tidak dikirim. |
| No answer after a minute. | Tidak ada jawaban setelah satu menit. |
| The report could not be sent. It is kept in {0}. | Laporan tidak dapat dikirim. Laporan disimpan di {0}. |
| Sending a report left waiting by an earlier run. | Mengirim laporan yang tertunda sejak proses sebelumnya. |
| The new version will scan and list what it finds, without deleting anything. To delete, run /d again afterwards. | Versi baru akan memindai dan menampilkan daftar temuannya, tanpa menghapus apa pun. Untuk menghapus, jalankan /d lagi setelahnya. |
| The new version will scan and list what it finds, without moving anything. To move, run /m again afterwards. | Versi baru akan memindai dan menampilkan daftar temuannya, tanpa memindahkan apa pun. Untuk memindahkan, jalankan /m lagi setelahnya. |
| InstallerClean is in use, so it can't update itself now. | InstallerClean sedang digunakan, jadi tidak dapat memperbarui dirinya sekarang. |
| Running {0} with InstallerClean {1} instead. | Menjalankan {0} dengan InstallerClean {1} sebagai gantinya. |
| The newer version couldn't be run. | Versi yang lebih baru tidak dapat dijalankan. |
| Running {0} with InstallerClean {1}. It takes this copy's place once the run ends. | Menjalankan {0} dengan InstallerClean {1}. Versi ini akan menggantikan salinan ini setelah proses selesai. |
| {0}: {1} | {0}: {1} |
| It needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then run InstallerClean again to update. | Versi ini memerlukan .NET Framework 4.8. Pasang dari Microsoft, mulai ulang PC, lalu jalankan InstallerClean lagi untuk memperbarui. |
| It's simple to undo. Move them back from {0} into {InstallerFolder} and everything will be back to how it was. | Mudah untuk dibatalkan. Pindahkan kembali dari {0} ke {InstallerFolder} dan semuanya akan kembali seperti semula. |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then run the command again. | InstallerClean tidak bisa lagi memastikan folder cadangan, jadi berhenti. Periksa {0}, lalu jalankan perintahnya lagi. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Proses InstallerClean lain memegang kunci instans-tunggal (GUI atau proses CLI lain). Kode keluar 75 (sementara); aman untuk dicoba lagi nanti. |
| Stopped after deleting {0} of {1} {2}: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 2 (partial). | Dihentikan setelah menghapus {0} dari {1} {2}: InstallerClean tidak bisa lagi memastikan bahwa tidak ada InstallerClean lain yang sedang bekerja. Jalankan perintah yang sama lagi. Kode keluar 2 (sebagian). |
| Stopped after moving {0} of {1} {2} to {3}: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 2 (partial). | Dihentikan setelah memindahkan {0} dari {1} {2} ke {3}: InstallerClean tidak bisa lagi memastikan bahwa tidak ada InstallerClean lain yang sedang bekerja. Jalankan perintah yang sama lagi. Kode keluar 2 (sebagian). |
| Stopped before deleting any file: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 75 (transient). | Dihentikan sebelum menghapus file apa pun: InstallerClean tidak bisa lagi memastikan bahwa tidak ada InstallerClean lain yang sedang bekerja. Jalankan perintah yang sama lagi. Kode keluar 75 (sementara). |
| Stopped before moving any file: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 75 (transient). | Dihentikan sebelum memindahkan file apa pun: InstallerClean tidak bisa lagi memastikan bahwa tidak ada InstallerClean lain yang sedang bekerja. Jalankan perintah yang sama lagi. Kode keluar 75 (sementara). |
| InstallerClean needs administrator rights to see every program installed on this PC, and it isn't running as administrator, so it won't scan. Nothing has been changed. Run it again from an administrator prompt. | InstallerClean perlu hak administrator untuk melihat setiap program yang terpasang di PC ini, dan saat ini tidak berjalan sebagai administrator, jadi tidak akan memindai. Tidak ada yang diubah. Jalankan lagi dari prompt administrator. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Catatan: penulisan ke Log Peristiwa gagal. Periksa izin log Aplikasi atau Kebijakan Grup. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - pembersihan {InstallerFolder} |
| Removes cached .msi and .msp files that no installed program still needs. | Menghapus file .msi/.msp cache yang tak lagi dibutuhkan program terpasang. |
| Needs an elevated (administrator) prompt and won't scan without one. | Perlu prompt administrator; tanpanya tidak akan memindai. |
| Usage: | Penggunaan: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     Tampilkan bantuan ini (juga /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  Cetak versi (juga -v) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         Pindai saja - daftar yang tak dipakai |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         Hapus permanen yang tidak diperlukan |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         Pindahkan ke folder cadangan tersimpan |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m JALUR   Pindahkan ke jalur yang ditentukan |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli menahan prompt sampai selesai, sehingga skrip atau<br>tugas terjadwal bisa menunggunya. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | Folder disimpan per pengguna; tugas terjadwal atau SYSTEM: /m JALUR. |
| Before it scans, it checks for a newer version of InstallerClean.<br>When you run it yourself, it asks before updating, and updates if nobody<br>answers within a minute. When it runs on its own, in a scheduled task or<br>a script, it updates without asking.<br>Add /noupdate to skip the check. | Sebelum memindai, alat ini memeriksa versi InstallerClean yang lebih baru.<br>Jika Anda menjalankannya sendiri, alat ini bertanya sebelum memperbarui,<br>dan memperbarui jika tidak ada yang menjawab dalam satu menit. Jika<br>berjalan otomatis lewat tugas terjadwal atau skrip, ia memperbarui tanpa<br>bertanya. Tambahkan /noupdate untuk melewati pemeriksaan. |
| Exit codes: | Kode keluar: |
|   0   success: the run did what it was asked |   0   berhasil: menjalankan yang diminta |
|   1   nothing processed: the run failed or was refused |   1   tidak ada yang diproses: proses gagal atau ditolak |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   sebagian: sebagian diproses, sebagian tidak (gagal atau Ctrl+C) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  sementara: kondisi sementara memblokir proses (lihat pesannya) |
|   130 cancelled (Ctrl+C) |   130 dibatalkan (Ctrl+C) |
