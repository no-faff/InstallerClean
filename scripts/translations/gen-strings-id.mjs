#!/usr/bin/env node
// Indonesian (id) satellite generator for InstallerClean. Copied from
// gen-strings-template.mjs / gen-strings-ko.mjs; only OUT, ALSO_KEEP and the MAP
// values differ. Works FROM THE ENGLISH SOURCE (Strings.resx): replaces each
// key's inner <value>, strips the 21 machine-contract Cli.EventLog* keys, keeps
// the human Cli keys, and self-verifies against the neutral. Output is LF,
// UTF-8.
//
// Indonesian plural rule (DisplayHelpers.CategoryFor, case "id"): PluralCategory
// .Other at every count. Indonesian has no count inflection, so there are NO
// .One/.Few/.Many override keys (OVERRIDES empty), and the Plural.* pairs are
// identical (both "file" etc). The hardcoded .Singular/.Plural sentence pairs
// are translated on both members and come out identical except
// RecycleUnavailable.Body.Singular, which keeps the neutral's design of dropping
// {0} ("this {1}" vs "these {0} {1}"); each placeholder set is preserved exactly.
//
// "file" and "patch" are byte-identical to the English .Singular, so they go in
// ALSO_KEEP (deliberate single-token keeps); their .Plural values differ from the
// English "files"/"patches" and pass the gate.
//
// Register: Anda (neutral-polite) throughout, the settled Indonesian software-UI
// convention (Windows id uses it too; there is no natural informal UI "you"),
// matching README.id.md. Platform terms sourced from Windows / Microsoft
// Terminology: Recycle Bin = Keranjang Sampah, About = Tentang, Start menu =
// menu Mulai, Event Log / Application log / Group Policy = Log Peristiwa /
// log Aplikasi / Kebijakan Grup, display language = bahasa tampilan.
import { readFileSync, writeFileSync } from 'node:fs';

const dir = 'src/InstallerClean.Core/Resources';
const BASE = `${dir}/Strings.resx`;            // English source (the "neutral")
const OUT = `${dir}/Strings.id.resx`;

// Universal keeps: keys whose value is the same in every language (brand names,
// the pure-placeholder string, the size/elapsed format strings). Their still-
// English value is NOT a miss. Do NOT translate these values. Do NOT edit this
// list per language.
const KEEP_ENGLISH = new Set([
  'Window.Main.Title',                 // InstallerClean
  'Startup.AlreadyRunningTitle',       // InstallerClean
  'Startup.UnhandledTitle',            // InstallerClean
  'Automation.ScanResultAnnouncement', // {0} ({1})
  'Display.Size.GB',                   // {0:F2} GB
  'Display.Size.MB',                   // {0:F1} MB
  'Display.Size.KB',                   // {0:F1} KB
  'Display.Size.B',                    // {0} B
  'Display.Elapsed.Ms',                // {0:F0}ms
  'Display.Elapsed.S',                 // {0:F1}s
]);

// Per-language keeps: "file"/"patch" (and the Field.File label) are identical to
// the English, a deliberate keep (Indonesian uses the same loanword "file", and
// Windows id keeps "File"), not a missed value.
const ALSO_KEEP = ['Plural.File.Singular', 'Plural.Patch.Singular', 'Field.File'];

const MAP = {
  // Window titles
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `Tentang`,
  'Window.Registered.Title': `File terdaftar yang sebaiknya tidak dihapus`,
  'Window.Orphaned.Title': `File tidak diperlukan yang aman dihapus`,

  // Section headings
  'Section.Registered.Products': `PRODUK`,
  'Section.Registered.Patches': `PATCH`,
  'Section.Registered.Details': `DETAIL PRODUK`,
  'Section.Move.Location': `LOKASI PEMINDAHAN`,
  'Section.SayThanks': `UCAPKAN TERIMA KASIH`,

  // Field labels (used in detail panels)
  'Field.Reason': `Alasan`,
  'Field.Author': `Pembuat`,
  'Field.Application': `Aplikasi`,
  'Field.Title': `Judul`,
  'Field.Subject': `Subjek`,
  'Field.Keywords': `Kata kunci`,
  'Field.SigningCertificate': `Sertifikat penandatanganan`,
  'Field.FileSize': `Ukuran file`,
  'Field.Comment': `Komentar`,
  'Field.ProductName': `Nama produk`,
  'Field.File': `File`,
  'Field.Size': `Ukuran`,
  'Field.Patches': `Patch`,

  // Placeholder shown for a registered package whose API ProductName is empty.
  'Field.UnknownProductName': `(tidak diketahui)`,
  'Field.PatchesOnly': `(patch saja)`,
  'Field.Missing': `hilang`,

  // Actions (button labels; underscore prefixes are WPF mnemonics)
  'Action.About': `_Tentang`,
  'Action.Copy': `Salin`,
  'Action.Cut': `Potong`,
  'Action.Paste': `Tempel`,
  'Action.SelectAll': `Pilih semua`,
  'Action.Browse': `Te_lusuri...`,
  'Action.Cancel': `_Batal`,
  'Action.CheckForUpdates': `Periksa pem_baruan`,
  'Action.Close': `_Tutup`,
  'Action.DeletePermanently': `_Hapus permanen`,
  'Action.Done': `_Selesai`,
  'Action.Details': `Detail`,
  'Action.BuyMeACuppa': `Traktir saya secangkir _kopi`,
  'Action.LeaveStarOnGitHub': `Beri _bintang di GitHub`,
  'Action.Licence': `Lisensi Apache 2.0`,
  'Action.Move': `_Pindahkan`,
  'Action.MoveDestinationPlaceholder': `Jalur folder jika Anda memilih Pindahkan, bukan Hapus`,
  'Action.OpenReleasePage': `Buka halaman _rilis`,
  'Action.Rescan': `Pindai _ulang`,
  'Action.ScanAgain': `Pindai _lagi`,
  'Action.SendResultLog': `Kirim laporan`,
  'Action.SendResultLogConfirm': `_Kirim`,

  // Automation names (screen reader / accessibility)
  'Automation.BuyMeACuppa': `Donasi`,
  'Automation.BuyMeACuppa.About': `Traktir saya secangkir kopi`,
  'Automation.CancelOperation': `Batalkan operasi`,
  'Automation.CancelScan': `Batalkan pemindaian`,
  'Automation.CancelStartupScan': `Batalkan pemindaian awal`,
  'Automation.Close': `Tutup`,
  'Automation.CloseWindow': `Tutup jendela`,
  'Automation.CloseResult': `Tutup hasil dan kembali ke jendela utama`,
  'Automation.LeaveStarOnGitHub.About': `Beri bintang di github`,
  'Automation.Minimise': `Kecilkan`,
  'Automation.ConfirmDelete': `Hapus memindahkan file yang tidak diperlukan ke Keranjang Sampah. Batal menutup tanpa menghapus.`,
  'Automation.ConfirmMove': `Pindahkan menaruh file yang tidak diperlukan di folder tujuan yang dipilih. Batal membiarkannya di tempatnya.`,
  'Automation.SayThanks': `Ucapkan terima kasih`,
  'Automation.ConfirmSendResultLog': `Kirim mengirimkan laporan yang ditampilkan ke No Faff. Batal tidak mengirim apa pun.`,
  'Automation.CheckForUpdates': `Periksa pembaruan`,
  'Automation.CheckForUpdates.HelpText': `Memeriksa halaman rilis github untuk mencari versi yang lebih baru.`,
  'Automation.UpdateAvailable.HelpText': `Buka halaman rilis untuk mengunduh versi yang lebih baru, atau batalkan untuk tetap memakai versi saat ini.`,
  'Automation.Licence.HelpText': `Membuka file lisensi di github.com melalui browser Anda.`,
  'Automation.Section.MoveLocation': `Lokasi pemindahan`,
  'Automation.Section.Products': `Produk`,
  'Automation.Section.Patches': `Patch`,
  'Automation.Section.ProductDetails': `Detail produk`,
  'Automation.MoveDestinationFolder': `Lokasi pemindahan`,
  'Automation.OperationProgress': `Kemajuan operasi`,
  'Automation.RescanInstaller': `Pindai ulang {InstallerFolder}`,
  'Automation.ScanningProgress': `Kemajuan pemindaian`,
  'Automation.StartupScanProgress': `Kemajuan pemindaian awal`,
  'Automation.ViewOrphanedFiles': `Detail, file yang tidak diperlukan`,
  'Automation.ViewOrphanedFiles.HelpText': `Tersedia untuk dibersihkan.`,
  'Automation.ViewRegisteredFiles': `Detail, file terdaftar`,
  'Automation.ViewRegisteredFiles.HelpText': `Daftar baca-saja.`,
  'Automation.SortStatus.Ascending': `Diurutkan berdasarkan {0}, menaik`,
  'Automation.SortStatus.Descending': `Diurutkan berdasarkan {0}, menurun`,
  'Automation.Scroll.ScanResults': `Hasil pemindaian`,
  'Automation.Scroll.ResultDetails': `Detail hasil`,
  'Automation.Scroll.FileDetails': `Detail file`,
  'Automation.Scroll.DialogBody': `Teks dialog`,
  'Automation.ScanResultAnnouncement': `{0} ({1})`,
  'Automation.CompletionErrors': `File yang tidak dapat diproses`,
  'Automation.RegisteredMissingSeeAlso': `Menjelaskan folder ini, dan cara memulihkan file, di README`,

  // Tooltips
  'Tooltip.BuyMeACuppa.About': `Membuat haus!`,
  'Tooltip.CancellingPending': `Pembatalan diminta. InstallerClean sedang menunggu langkah yang berjalan mencapai titik berhenti. Ini bisa memakan waktu beberapa detik saat I/O berat atau panggilan basis data MSI.`,
  'Tooltip.Close': `Tutup`,
  'Tooltip.LeaveStarOnGitHub.About': `Bintang membantu orang lain menemukan InstallerClean.`,
  'Tooltip.Minimise': `Kecilkan`,
  'Tooltip.SendResultLog': `Terserah Anda, tapi sangat dihargai. Mengirim ringkasan anonim yang sekadar memberi tahu saya apakah aplikasi berfungsi dan berapa banyak ruang yang dikosongkan orang-orang. Layar berikutnya memperlihatkan apa yang akan dikirim sebelum Anda mengonfirmasi.`,
  'Tooltip.SendResultLog.NothingFound': `Terserah Anda, tapi sangat dihargai. Mengirim ringkasan anonim yang sekadar memberi tahu saya apakah aplikasi berfungsi. Layar berikutnya memperlihatkan apa yang akan dikirim sebelum Anda mengonfirmasi.`,
  'Tooltip.Move': `Pindahkan file yang tidak diperlukan ke lokasi pemindahan.`,
  'Tooltip.MoveNeedsDestination': `Pindahkan file yang tidak diperlukan ke tempat aman. Anda akan memilih foldernya setelah ini.`,
  'Tooltip.Delete': `Pindahkan file yang tidak diperlukan ke Keranjang Sampah.`,
  'Tooltip.SigningCertificate': `Nama subjek dari sertifikat Authenticode yang disematkan. Rantai sertifikat tidak diverifikasi.`,

  // Body copy
  'Body.MainExplanation.Lead': `File yang tidak diperlukan di bawah ini aman dihapus.`,
  'Body.MainExplanation.Why': `File-file ini berada di {InstallerFolder}, tertinggal saat sebuah program dihapus instalasinya ({0}), patch yang lebih baru menggantikan yang lama ({1}), atau penerbitnya menariknya ({2}). InstallerClean hanya pernah mencantumkan file yang Windows sendiri laporkan sudah tidak terpakai.`,
  'Body.MainExplanation.Action': `Hapus ke Keranjang Sampah, atau gunakan Pindahkan sebagai gantinya untuk menyimpan salinan cadangan. Mengembalikan file ke {InstallerFolder} akan membuat semuanya persis seperti semula.`,
  'Body.PendingReboot.MsiExecuteMutex': `Ada sesuatu yang sedang menggunakan Windows Installer saat ini, biasanya Windows Update atau program yang memasang di latar belakang. Pindahkan dan Hapus dijeda selama itu berjalan, sehingga InstallerClean tidak menyentuh cache penginstal saat sedang berubah. Setelah selesai, Pindai ulang dan keduanya kembali.`,
  'Body.PendingReboot.InstallerInProgress': `Ada transaksi Windows Installer sebelumnya yang ditangguhkan di komputer ini. Lanjutkan atau batalkan instalasi itu (atau mulai ulang Windows) sebelum membersihkan cache.`,
  'Body.PendingReboot.PendingRenameInCache': `Windows memiliki antrean penggantian nama file untuk mulai ulang berikutnya yang memengaruhi cache Installer. Mulai ulang Windows sebelum membersihkan.`,
  'Body.NoFileSelected': `Pilih file untuk melihat detail.`,
  'Body.NoProductSelected': `Pilih produk untuk melihat detail.`,
  'Body.NoMetadata': `Tidak ada metadata yang tersedia.`,
  'Body.RegisteredMissingFromDisk': `File penginstal ini telah dihapus. InstallerClean tidak melakukannya, aplikasi ini tidak pernah menghapus file yang masih diperlukan sebuah program; sesuatu yang lain menghapus file ini sebelum Anda menjalankan InstallerClean.&#10;&#10;File ini tidak menimbulkan masalah sekarang, dan tidak akan menimbulkannya sampai suatu hari Anda mencoba memperbaiki, memperbarui, atau menghapus instalasi program pemiliknya. Langkah itu kemudian bisa gagal, karena Windows mencari file ini dan file-nya tidak ada.&#10;&#10;Untuk mencoba memperbaikinya, unduh penginstal program tersebut dari pembuatnya dan jalankan di atas salinan yang sudah ada (jangan menghapus instalasi terlebih dahulu, penghapusan instalasi sendiri adalah langkah yang memerlukan file ini). Gunakan versi yang Anda pasang jika bisa mendapatkannya, karena Windows mungkin menolak versi yang berbeda. Cara ini biasanya memulihkan file, dan pengaturan Anda umumnya tetap utuh, tetapi Microsoft tidak menjaminnya, langkah terakhir Microsoft sendiri adalah memasang ulang program itu, atau Windows itu sendiri.`,
  'Body.RegisteredMissingFromDisk.SeeAlso': `README [menjelaskan folder ini], dan cara memulihkan file, dengan kata-kata Microsoft sendiri.`,
  'Body.NoPatches': `(tidak ada)`,

  // Reasons (Reason column in the orphaned-files list)
  'Reason.Orphaned': `Terisolasi`,
  'Reason.Superseded': `Digantikan`,
  'Reason.Obsoleted': `Usang`,

  // Status / progress text
  'Status.Scanning': `Memindai...`,
  'Status.Cancelling': `Membatalkan...`,
  'Status.StartingScan': `Memulai pemindaian...`,
  'Status.QueryingApi': `Menanyai Windows tentang perangkat lunak yang terpasang...`,
  'Status.ScanningCache': `Memindai folder cache penginstal...`,
  'Status.EnumeratingProducts': `Mendata produk yang terpasang...`,
  'Status.CheckingRegistry': `Memeriksa registri untuk paket tambahan...`,

  // 0 = registered package count, 1 = pluralised "package"/"packages"
  'Status.RegisteredPackagesFound': `Ditemukan {0} {1} terdaftar.`,

  // 0 = elapsed time text (e.g. "1.2s")
  'Status.ScanComplete': `Pemindaian selesai ({0})`,
  'Status.FoundProducts': `Memindai paket lokal...`,

  // 0 = file count, 1 = pluralised noun ("file"/"files")
  'Status.FoundUnused': `Ditemukan {0} {1} yang aman Anda hapus.`,
  'Status.PreparingDestination': `Menyiapkan folder tujuan...`,

  // 0 = file count, 1 = pluralised noun
  'Status.Moving': `Memindahkan {0} {1}...`,
  'Status.Deleting': `Menghapus {0} {1}...`,
  'Status.MoveCancelled.Partial': `Pemindahan dibatalkan. {0} dari {1} {2} diproses.`,
  'Status.DeleteCancelled.Partial': `Penghapusan dibatalkan. {0} dari {1} {2} diproses.`,
  'Status.MoveFailed': `Pemindahan gagal ({0}). Detail di {1}.`,
  'Status.MoveFailed.NoLog': `Pemindahan gagal ({0}). Log kerusakan tidak bisa ditulis.`,
  'Status.DeleteFailed': `Penghapusan gagal ({0}). Detail di {1}.`,
  'Status.DeleteFailed.NoLog': `Penghapusan gagal ({0}). Log kerusakan tidak bisa ditulis.`,
  'Status.ScanAccessDenied': `Akses ditolak. Windows menolak pemindaian.`,
  'Status.ScanFailedDb': `Pemindaian gagal: catatan Windows Installer tidak bisa dibaca.`,
  'Status.ScanCancelled': `Pemindaian dibatalkan.`,
  'Status.Done': `Siap`,
  'Status.ScanFailedDetails': `Pemindaian gagal ({0}). Detail di {1}.`,
  'Status.ScanFailedDetails.NoLog': `Pemindaian gagal ({0}). Log kerusakan tidak bisa ditulis.`,

  // Completion screen
  'Completion.AllClean': `Semua bersih`,
  'Completion.NothingToCleanUp': `Tidak ada yang perlu dibersihkan di {InstallerFolder}`,
  'Completion.NothingToCleanUpReceipt': `{0} {1} dipindai dalam {2}`,

  // 0 = size freed (e.g. "120.5 MB")
  'Completion.Freed': `{0} dikosongkan`,
  'Completion.Moved': `{0} dipindahkan`,
  // Heading for the outcome where the operation acted on no file at all, then
  // the failure count line beneath it: 0 = files that failed, 1 = files tried
  // (succeeded + failed). The count selecting the form is {0}; the noun belongs
  // to {1}, so treat the two independently.
  'Completion.NothingMoved': `Tidak ada yang dipindahkan`,
  'Completion.NothingDeleted': `Tidak ada yang dihapus`,
  'Completion.FailedCount.Singular': `{0} dari {1} file tidak bisa dipindahkan.`,
  'Completion.FailedCount.Plural': `{0} dari {1} file tidak bisa dipindahkan.`,
  'Completion.FailedCountDelete.Singular': `{0} dari {1} file tidak bisa dihapus.`,
  'Completion.FailedCountDelete.Plural': `{0} dari {1} file tidak bisa dihapus.`,

  // 0 = moved count, 1 = pluralised noun, 2 = destination path
  'Completion.MoveSummary.Singular': `{0} {1} dipindahkan ke: {2}`,
  'Completion.MoveSummary.Plural': `{0} {1} dipindahkan ke: {2}`,

  // 0 = deleted count, 1 = pluralised noun

  // 0 = deleted count, 1 = pluralised noun
  'Completion.PermanentDeleteSummary.Singular': `{0} {1} dihapus permanen. File tidak masuk ke Keranjang Sampah.`,
  'Completion.PermanentDeleteSummary.Plural': `{0} {1} dihapus permanen. File tidak masuk ke Keranjang Sampah.`,

  // Summaries
  'Summary.RegisteredStillUsed.Singular': `{0} file masih diperlukan`,
  'Summary.RegisteredStillUsed.Plural': `{0} file masih diperlukan`,
  'Summary.OrphanedToCleanUp.Singular': `{0} file tidak diperlukan untuk dibersihkan`,
  'Summary.OrphanedToCleanUp.Plural': `{0} file tidak diperlukan untuk dibersihkan`,
  'Summary.MissingFromDisk.Singular': `{0} file terdaftar hilang (bukan dihapus oleh InstallerClean). Tidak masalah sekarang, tetapi perbaikan, pembaruan, atau penghapusan instalasi program itu di kemudian hari bisa gagal. Buka Detail untuk tahu apa yang harus dilakukan.`,
  'Summary.MissingFromDisk.Plural': `{0} file terdaftar hilang (bukan dihapus oleh InstallerClean). Tidak masalah sekarang, tetapi perbaikan, pembaruan, atau penghapusan instalasi program-program itu di kemudian hari bisa gagal. Buka Detail untuk tahu apa yang harus dilakukan.`,

  // 0 = current file count, 1 = total count, 2 = pluralised noun.
  'Summary.OperationFiles': `{0} dari {1} {2}`,

  // Orphaned-window footer. 0 = orphaned count, 1 = superseded count,
  // 2 = obsoleted count, 3 = size display.
  'Summary.OrphanedWindow': `{0} terisolasi, {1} digantikan, {2} usang ({3})`,

  // Registered-window footer. 0 = count, 1 = size display.
  'Summary.RegisteredWindow.Singular': `{0} file terdaftar yang masih diperlukan ({1})`,
  'Summary.RegisteredWindow.Plural': `{0} file terdaftar yang masih diperlukan ({1})`,

  // Confirmation dialogs

  // 0 = file count, 1 = pluralised "file"/"files", 2 = size display
  'Confirm.MoveTitle': `Pindahkan {0} {1} ({2})?`,

  // 0 = destination path
  'Confirm.MoveDestination': `File akan dipindahkan ke:`,
  'Confirm.DeleteTitle': `Hapus {0} {1} ({2})?`,

  // Error messages
  'Error.AdminRequiredTitle': `Akses ditolak`,
  'Error.AdminRequiredBody': `Windows menolak akses untuk InstallerClean, jadi prosesnya dihentikan. Tidak ada yang dihapus.\n\nInstallerClean sudah berjalan sebagai administrator, jadi menjalankannya lagi dengan cara itu tidak akan membantu. Windows tidak menjelaskan lebih jauh apa yang menolak akses, jadi tidak ada hal khusus yang bisa dicoba.`,
  'Error.InstallerDbUnavailableTitle': `Catatan Windows Installer tidak bisa dibaca`,
  'Error.ScanFailedTitle': `Pemindaian gagal`,
  'Error.InstallerDbEmpty': `Catatan Windows Installer kembali sepenuhnya kosong: tidak satu pun program terpasang atau pembaruan yang mengklaim file pemasang di cache. Itu tidak terjadi pada komputer yang berfungsi (bahkan pemasangan Windows yang baru pun punya beberapa), jadi catatannya rusak atau tidak bisa dibaca, dan pemindaian yang memercayai jawaban ini akan keliru menyebut setiap file di {InstallerFolder} terisolasi. InstallerClean berhenti sebagai gantinya. Tidak ada yang dihapus.`,
  'Error.MsiAccessDenied': `Windows Installer tidak mengizinkan InstallerClean menampilkan daftar apa saja yang terpasang. InstallerClean sudah berjalan sebagai administrator, jadi menjalankannya lagi sebagai administrator tidak akan mengubah apa pun. Tanpa daftar itu tidak ada cara yang aman untuk mengetahui file cache mana yang masih diperlukan, jadi InstallerClean berhenti. Tidak ada yang dihapus.`,
  'Error.MsiNonSuccess': `Windows Installer tidak bisa memberi InstallerClean daftar program terpasang yang terbaca: {0} entri berturut-turut kembali tidak terbaca (kode kesalahan terakhir {1}). Alih-alih bekerja dengan daftar yang hanya terbaca sebagian, InstallerClean berhenti. Tidak ada yang dihapus.`,
  'Error.InvalidDestinationTitle': `Tujuan tidak valid`,
  'Error.DestinationWriteFailedTitle': `Tidak bisa menulis ke tujuan`,
  'Error.MoveFailedTitle': `Pemindahan gagal`,
  'Error.DeleteFailedTitle': `Penghapusan gagal`,
  'Error.SettingNotSavedTitle': `Pengaturan tidak tersimpan`,
  'Error.SettingNotSavedBody': `Perubahan tidak dapat disimpan. Saat berikutnya dijalankan, InstallerClean akan kembali ke pengaturan sebelumnya.`,
  'Error.DestinationInsideInstaller': `Tujuan tidak boleh berada di dalam folder Windows Installer.`,

  // 0 = the destination path the user typed
  'Error.DestinationInSystemFolder': `Tujuan {0} mengarah ke dalam folder sistem Windows. Pilih jalur di luar %SystemRoot%, %ProgramFiles%, dan %ProgramData%.`,
  'Error.NotEnoughSpaceTitle': `Ruang tidak cukup`,

  // 0 = destination, 1 = required size, 2 = available size
  'Error.NotEnoughSpaceBody': `Ruang tidak cukup di {0}\n\nDiperlukan: {1}\nTersedia: {2}`,

  // 0 = destination
  'Error.AccessDeniedDestination': `Anda tidak punya izin untuk menulis ke {0}.\nCoba folder di profil pengguna Anda atau di drive milik Anda sendiri.`,
  'Error.PathTooLong': `Jalur {0} terlalu panjang untuk Windows. Pilih jalur yang lebih pendek.`,
  'Error.DestinationMissing': `Folder {0} tidak ada dan tidak bisa dibuat. Periksa huruf drive atau jalur jaringan.`,
  'Error.IOWriteDestination': `Windows tidak bisa menulis ke {0}.\nDetail di {1}.`,
  'Error.IOWriteDestination.NoLog': `Windows tidak bisa menulis ke {0}. Log kerusakan tidak bisa ditulis.`,
  'Error.WriteDestination': `Tidak bisa menulis ke {0}.\nDetail di {1}.`,
  'Error.WriteDestination.NoLog': `Tidak bisa menulis ke {0}. Log kerusakan tidak bisa ditulis.`,
  'Error.MissingSourceFile': `File sudah tidak ada lagi.`,
  'Error.SourceIsReparsePoint': `File sumber adalah symlink atau junction; ditolak demi keamanan.`,
  // Singular = the sentence for ONE file, which the CLI prints after "filename: ";
  // plural = the heading the completion overlay puts over a list of filenames.
  'Error.AccessDenied.Singular': `Windows menolak akses ke file ini; file dibiarkan di tempatnya.`,
  'Error.AccessDenied.Plural': `Windows menolak akses ke file-file ini; semuanya dibiarkan di tempatnya.`,
  'Error.FileInUse.Singular': `File ini sedang dibuka atau dikunci oleh program lain, jadi saat ini tidak ada yang bisa memindahkannya. File dibiarkan di tempatnya; coba lagi nanti.`,
  'Error.FileInUse.Plural': `File-file ini sedang dibuka atau dikunci oleh program lain, jadi saat ini tidak ada yang bisa memindahkannya. Semuanya dibiarkan di tempatnya; coba lagi nanti.`,
  'Error.IOFailure.Singular': `Windows melaporkan kesalahan file; file dibiarkan di tempatnya.`,
  'Error.IOFailure.Plural': `Windows melaporkan kesalahan file; file-file ini dibiarkan di tempatnya.`,
  'Error.UnknownError.Singular': `Ada yang tidak beres dengan file ini; file dibiarkan di tempatnya.`,
  'Error.UnknownError.Plural': `Ada yang tidak beres dengan file-file ini; semuanya dibiarkan di tempatnya.`,

  // 0 = shell error code

  // 0 = destination
  'Error.MoveIntoInstaller': `Menolak memindahkan file ke dalam folder Windows Installer (tujuan: {0}).`,

  // 0 = the relative path the caller passed
  'Error.DestinationNotFullyQualified': `Lokasi pemindahan harus berupa jalur lengkap ke sebuah folder, yang dimulai dengan huruf drive atau jalur jaringan (misalnya D:\\Backup, atau \\\\server\\backup). InstallerClean tidak bisa memakai yang ini: {0}`,
  'BrowserLaunch.FailedTitle': `Tidak bisa membuka browser Anda`,
  'UpdateCheck.Title': `Periksa pembaruan`,
  'UpdateCheck.Status.Checking': `Memeriksa...`,
  'UpdateCheck.Status.UpToDate': `Sudah versi terbaru.`,
  'UpdateCheck.UpdateAvailable.Title': `Pembaruan tersedia`,

  // 0 = installed version, 1 = latest version on GitHub
  'UpdateCheck.UpdateAvailable.Body': `Anda menjalankan versi {0}.&#10;Versi {1} tersedia.`,
  'UpdateCheck.Failed.NetworkUnavailable': `Tidak bisa menjangkau GitHub. Periksa koneksi internet Anda dan coba lagi.`,
  'UpdateCheck.Failed.ServerError': `GitHub mengembalikan respons kesalahan. Coba lagi dalam beberapa menit.`,
  'UpdateCheck.Failed.ResponseParseError': `Respons GitHub tidak memuat rilis yang dikenali. Coba lagi nanti, atau buka halaman rilis langsung.`,
  'UpdateCheck.Failed.Timeout': `Pemeriksaan kehabisan waktu. Koneksi Anda ke GitHub mungkin lambat; coba lagi.`,
  'UpdateCheck.Failed.Unknown': `Pemeriksaan gagal karena alasan yang tidak diketahui. Detailnya ada di crash.log jika Anda perlu melaporkannya.`,

  // 0 = the URL the user was trying to reach
  'BrowserLaunch.ClipboardOk': `InstallerClean tidak bisa membuka browser Anda. Tautannya sudah ada di clipboard, jadi Anda bisa menempelkannya sendiri:&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean tidak bisa membuka browser Anda, dan juga tidak bisa menyalin tautan ke clipboard. Tautannya:&#10;&#10;{0}`,

  // 0 = the destination folder whose canonical path changed mid-batch
  'Error.DestinationChangedMidBatch': `Lokasi pemindahan berubah saat file sedang dipindahkan (ada sesuatu yang mengganti atau mengalihkan folder itu), jadi InstallerClean berhenti daripada menulis ke tempat yang salah. Periksa {0}, lalu Pindai ulang dan coba lagi.`,

  // 0 = folder, 1 = inner exception message
  'Error.CannotWriteFolder': `Tidak bisa menulis ke {0}.`,

  // 0 = file name
  'Error.NoUniqueFilename': `Tidak bisa menemukan nama file yang unik untuk '{0}' setelah 10.000 percobaan.`,

  // Result log (post-cleanup diagnostic send)
  'ResultLog.Sending': `Mengirim...`,
  'ResultLog.Sent': `Terima kasih! Laporan terkirim.`,
  'ResultLog.Failed': `Pengiriman gagal. Coba lagi nanti.`,
  'ResultLog.NothingToSend': `Tidak ada laporan untuk dikirim.`,
  'ConfirmSendResultLog.Title': `Kirim ini?`,
  'ConfirmSendResultLog.Reassurance': `Dikirim ke nofaff.netlify.app/api/result-log. Tidak ada yang mengidentifikasi Anda atau komputer Anda; ini hanya memberi tahu saya bahwa InstallerClean berfungsi dan [berapa banyak ruang yang dikosongkan orang-orang].`,
  'Automation.ResultLogPreview': `Pratinjau laporan`,

  // Single instance / startup / crash
  'Startup.AlreadyRunningTitle': `InstallerClean`,
  'Startup.AlreadyRunningBody': `InstallerClean sudah berjalan.`,
  'Startup.UnhandledTitle': `InstallerClean`,
  'Startup.UnhandledBody': `Terjadi kesalahan tak terduga dan InstallerClean perlu ditutup.\n\n{0}\n\nDetail ditulis ke:\n{1}`,
  'Startup.UnhandledBody.NoLog': `Terjadi kesalahan tak terduga dan InstallerClean perlu ditutup.\n\n{0}\n\nLog kerusakan tidak bisa ditulis.`,
  'Startup.ErrorTitle': `Kesalahan saat memulai`,
  'Startup.FailedToStart': `Gagal memulai ({0}). Detail ditulis ke:\n{1}`,
  'Startup.FailedToStart.NoLog': `Gagal memulai ({0}). Log kerusakan tidak bisa ditulis.`,

  // File picker
  'FilePicker.ChooseDestinationTitle': `Pilih folder tujuan untuk file yang dipindahkan`,

  // Version display

  // 0 = major.minor.patch (e.g. "1.5.4")
  'Version.Display': `Versi {0}`,
  'Plural.File.Singular': `file`,
  'Plural.File.Plural': `file`,
  'Plural.Error.Singular': `kesalahan`,
  'Plural.Error.Plural': `kesalahan`,
  'Plural.Package.Singular': `paket`,
  'Plural.Package.Plural': `paket`,
  'Plural.Product.Singular': `produk`,
  'Plural.Product.Plural': `produk`,
  'Plural.Patch.Singular': `patch`,
  'Plural.Patch.Plural': `patch`,
  'Display.Size.GB': `{0:F2} GB`,
  'Display.Size.MB': `{0:F1} MB`,
  'Display.Size.KB': `{0:F1} KB`,
  'Display.Size.B': `{0} B`,
  'Display.Elapsed.Ms': `{0:F0}ms`,
  'Display.Elapsed.S': `{0:F1}s`,
  'Display.ElapsedLong.LessThanASecond': `kurang dari satu detik`,
  'Display.ElapsedLong.Seconds': `{0:F1} detik`,
  'CrashLog.PrivacyHeader': `# crash.log menangkap pengecualian tak tertangani dari InstallerClean.\n# Dalam mode dengan hak akses tinggi, pesan pengecualian framework bisa\n# memuat jalur file dari sesi yang berjalan (termasuk profil pengguna\n# lain yang didata oleh kueri Windows Installer). Pesan kegagalan\n# jaringan dari pemeriksaan pembaruan atau POST log hasil bisa memuat\n# URL tujuan dan alamat IP / proksi yang teresolusi. Hapus kedua jenis\n# detail ini sebelum melampirkan file ini ke laporan bug publik.\n`,
  'Tooltip.ChangeLanguage': `Ganti bahasa. Program akan dimulai ulang.`,
  'Automation.ChangeLanguage': `Ganti bahasa`,
  'Automation.ChangeLanguage.HelpText': `Program akan dimulai ulang.`,

  // Command-line tool (installerclean-cli): the HUMAN-facing Cli.* keys.
  // Descriptions translated; command tokens, flags, the {InstallerFolder} token
  // and the exit-code numbers verbatim; leading spaces kept (the screen is
  // column-aligned for a monospace terminal); PATH metavariable -> JALUR.
  'Cli.UnknownArgument': `Argumen tidak dikenal: '{0}'`,
  'Cli.Cancelling': `Membatalkan...`,
  'Cli.Cancelled': `Dibatalkan.`,
  'Cli.GenericError': `Kesalahan: {0}. Detail ditulis ke {1}.`,
  'Cli.GenericError.NoLog': `Kesalahan: {0}. Log kerusakan tidak bisa ditulis.`,
  'Cli.ScanningInstaller': `Memindai {InstallerFolder}...`,
  'Cli.FoundOrphans': `Ditemukan {0} {1} untuk dibersihkan ({2}).`,
  'Cli.NothingToDo': `Tidak ada yang perlu dilakukan.`,
  'Cli.DeletingFiles': `Menghapus {0} {1}...`,
  'Cli.DeletedFiles': `{0} {1} dihapus.`,
  'Cli.NoMoveDestination': `Kesalahan: tujuan pemindahan tidak ditentukan. Gunakan /m JALUR. (Default yang diatur di GUI bersifat per-pengguna dan tidak berlaku untuk tugas terjadwal atau proses akun layanan.)`,
  'Cli.MoveDestinationInsideInstaller': `Kesalahan: tujuan tidak boleh berada di dalam folder Windows Installer.`,
  'Cli.MoveDestinationRelative': `Kesalahan: tujuan harus berupa jalur absolut lengkap. Diterima: {0}`,
  'Cli.MoveDestinationInSystemFolder': `Kesalahan: tujuan {0} mengarah ke dalam folder sistem Windows. Pilih jalur di luar %SystemRoot%, %ProgramFiles%, dan %ProgramData%.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Kesalahan: ada sesuatu yang sedang menggunakan Windows Installer saat ini, biasanya Windows Update atau program yang memasang di latar belakang. Pindahkan dan Hapus diblokir selama itu berjalan. Coba lagi setelah selesai.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Kesalahan: ada transaksi Windows Installer sebelumnya yang ditangguhkan di komputer ini. Lanjutkan atau batalkan instalasi itu (atau mulai ulang Windows) sebelum membersihkan cache.`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Kesalahan: operasi file yang diantrekan setelah mulai ulang menyasar cache Installer ({0}). Mulai ulang Windows untuk menyelesaikan operasi itu sebelum membersihkan.`,
  'Cli.MovingFiles': `Memindahkan {0} {1} ke {2}...`,
  'Cli.MovedFiles': `{0} {1} dipindahkan.`,
  'Cli.MutexBlocked': `Proses InstallerClean lain memegang kunci instans-tunggal (GUI atau proses CLI lain). Kode keluar 75 (sementara); aman untuk dicoba lagi nanti.`,
  'Cli.EventLogUnavailable': `Catatan: penulisan ke Log Peristiwa gagal. Periksa izin log Aplikasi atau Kebijakan Grup.`,
  'Cli.Help.Header': `InstallerClean - pembersihan {InstallerFolder}`,
  'Cli.Help.Usage': `Penggunaan:`,
  'Cli.Help.Help': `  installerclean-cli --help     Tampilkan bantuan ini (juga /?, -h)`,
  'Cli.Help.Version': `  installerclean-cli --version  Cetak versi (juga -v)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s         Pindai saja - daftar file tidak diperlukan`,
  'Cli.Help.Delete': `  installerclean-cli /d         Hapus file tidak diperlukan (Keranjang)`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m         Pindahkan ke lokasi default tersimpan`,
  'Cli.Help.MovePath': `  installerclean-cli /m JALUR   Pindahkan ke jalur yang ditentukan`,
  'Cli.Help.NoteLine1': `installerclean-cli adalah proses konsol sungguhan dan memblokir prompt`,
  'Cli.Help.NoteLine2': `sampai selesai; alihkan atau salurkan keluarannya seperti`,
  'Cli.Help.NoteLine3': `file exe konsol lainnya. GUI ada di InstallerClean.exe di sebelahnya.`,
  'Cli.Help.ExitCodesHeader': `Kode keluar:`,
  'Cli.Help.ExitCodeOk': `  0   berhasil: setiap file yang ditandai telah diproses`,
  'Cli.Help.ExitCodeError': `  1   gagal: tidak ada yang diproses (argumen, pemindaian, semua file)`,
  'Cli.Help.ExitCodePartial': `  2   sebagian: sebagian file diproses, sebagian gagal`,
  'Cli.Help.ExitCodeTransient': `  75  sementara: kondisi sementara memblokir proses (lihat pesannya)`,
  'Cli.Help.ExitCodeCancelled': `  130 dibatalkan (Ctrl+C)`,
  'Body.NotScanned.Lead': `Belum ada yang dipindai.`,
  'Body.NotScanned.Why': `Tekan Pindai ulang untuk menelusuri {InstallerFolder} mencari file penginstal yang tidak lagi diperlukan program mana pun.`,
  'Confirm.MoveSameDrive': `Folder ini berada di drive yang sama, jadi pemindahan itu sendiri tidak akan mengosongkan ruang apa pun. Ruangnya akan kembali saat Anda menghapus file di dalamnya, atau Anda bisa memilih folder di drive lain.`,
  'Error.ScanCorrelationFailed': `InstallerClean tidak bisa mencocokkan pemindaian ini dengan catatan Windows Installer: setiap file yang masih didaftarkan Windows sebagai diperlukan tidak ada di {InstallerFolder}, sementara file yang benar-benar ada di folder itu tidak cocok dengan catatan mana pun. Tidak ada komputer nyata yang seperti itu, jadi ini menunjukkan masalah dalam membaca catatan, bukan file yang aman Anda hapus. Tidak ada yang ditawarkan untuk dibersihkan dan tidak ada yang dihapus.`,
  'Error.CandidateOutsideCache': `File ini tidak berada langsung di dalam folder Windows Installer; ditolak demi keamanan.`,
  'Completion.ReverifySkipped': `{0} {1} dibiarkan di tempatnya, karena sebuah program kembali membutuhkannya setelah pemindaian.`,
  'Completion.MoveCancelledSummary': `{0} dari {1} {2} dipindahkan sebelum Anda membatalkan.`,
  'Completion.PermanentDeleteCancelledSummary': `{0} dari {1} {2} dihapus permanen sebelum Anda membatalkan.`,
  'Body.PendingReboot.Lead': `File-file ini tidak bisa dibersihkan sekarang.`,
  'Cli.TooManyArguments': `Kesalahan: argumen tambahan yang tidak terduga '{0}'. Jika folder pemindahan Anda mengandung spasi, apit seluruh jalur dengan tanda kutip: /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `Lokasi tersimpan per pengguna; tugas terjadwal atau SYSTEM: /m JALUR.`,
  'Completion.ReverifyIncomplete': `{0} {1} dibiarkan di tempatnya, karena catatan Windows Installer tidak dapat dibaca sepenuhnya saat pemeriksaan diulang.`,
  'Summary.ProgramsUnreadable.Singular': `{0} program terpasang tidak dapat dibaca selama pemindaian ini, jadi patch yang digantikan tetap dipertahankan. File yang terisolasi tidak terpengaruh.`,
  'Summary.ProgramsUnreadable.Plural': `{0} program terpasang tidak dapat dibaca selama pemindaian ini, jadi patch yang digantikan tetap dipertahankan. File yang terisolasi tidak terpengaruh.`,
  'Error.ScanRecordsUnreadable': `InstallerClean tidak bisa membaca cukup banyak catatan Windows Installer untuk memastikan apa yang masih diperlukan: daftar program terpasang kembali tidak lengkap, dan membaca catatan yang sama langsung dari registri juga menemui kesalahan. Sebuah file bisa tampak terisolasi hanya karena catatan yang menyebutkannya termasuk yang tidak terbaca, jadi InstallerClean berhenti. Tidak ada yang dihapus.`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer tidak pernah menandai akhir daftar program terpasang: InstallerClean menyerah setelah {0} entri (kode kesalahan terakhir {1}). Daftar yang tidak berujung tidak bisa dipercaya, jadi InstallerClean berhenti. Tidak ada yang dihapus.`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer tidak pernah menandai akhir daftar patch sebuah program: InstallerClean menyerah setelah {0} entri (kode kesalahan terakhir {1}). Daftar yang tidak berujung tidak bisa dipercaya, jadi InstallerClean berhenti. Tidak ada yang dihapus.`,
  'UpdateCheck.Status.UpdateAvailable': `Versi {0} tersedia.`,
  'Completion.DonateAsk': `Senang bisa membantu. Kalau Anda berbaik hati, secangkir kopi sangat saya hargai.`,
  'About.Link.Guide': `Panduan dan FAQ`,
  'About.Link.ReportProblem': `Laporkan masalah`,
  'About.AutoUpdateCheck': `Periksa pembaruan secara otomatis`,
  'Automation.About.Guide.HelpText': `Membuka readme di github melalui browser Anda.`,
  'Automation.About.ReportProblem.HelpText': `Membuka pelacak masalah (Issues) di github.com melalui browser Anda.`,
  'Automation.AutoUpdateCheck.HelpText': `Jika dicentang, InstallerClean memeriksa apakah ada versi yang lebih baru di github saat Anda menjalankannya.`,
};

let text = readFileSync(BASE, 'utf8');

// Remove ONLY the 21 machine-contract Cli.* <data> elements BY NAME (the
// Cli.EventLog* set bar Cli.EventLogUnavailable).
const isMachineCliKey = (k) =>
  k.startsWith('Cli.') && k.includes('EventLog') && k !== 'Cli.EventLogUnavailable';
let cliMachineRemoved = 0;
text = text.replace(/[^\S\n]*<data name="(Cli\.[^"]*)"[\s\S]*?<\/data>\n?/g,
  (m, name) => { if (isMachineCliKey(name)) { cliMachineRemoved++; return ''; } return m; });

// Replace each key's inner <value> from MAP.
const escRe = (s) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
const notApplied = [];
for (const [key, val] of Object.entries(MAP)) {
  const re = new RegExp('(<data name="' + escRe(key) + '"[^>]*>\\s*<value>)([\\s\\S]*?)(</value>)');
  let applied = false;
  text = text.replace(re, (m, p1, p2, p3) => { applied = true; return p1 + val + p3; });
  if (!applied) notApplied.push(key);
}

// Normalise to LF with exactly one trailing newline.
text = text.replace(/\r\n/g, '\n');
if (!text.endsWith('\n')) text += '\n';

writeFileSync(OUT, text, 'utf8');

// ---------------- self-check the written file against the neutral ----------------
const placeholders = (s) => new Set([...s.matchAll(/\{(\d+)(?::[^}]*)?\}/g)].map((p) => p[1]));
const parse = (xml) => {
  const map = new Map();
  const re = /<data\s+name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)<\/value>/g;
  let m;
  while ((m = re.exec(xml)) !== null) map.set(m[1], m[2]);
  return map;
};
const neutral = parse(readFileSync(BASE, 'utf8'));
const written = readFileSync(OUT, 'utf8');
const output = parse(written);
const neutralRequired = [...neutral.keys()].filter((k) => !isMachineCliKey(k));

const missingFromMap = neutralRequired.filter((k) => !(k in MAP));
const strayMapKeys = Object.keys(MAP).filter((k) => !neutral.has(k));
const machineLeaked = [...output.keys()].filter(isMachineCliKey);
const missingFromOutput = neutralRequired.filter((k) => !output.has(k));
const arityMismatch = neutralRequired.filter((k) => {
  if (!output.has(k)) return false;
  const a = placeholders(neutral.get(k)), b = placeholders(output.get(k));
  return a.size !== b.size || [...a].some((i) => !b.has(i));
});
const crlf = (written.match(/\r/g) || []).length;

const alsoKeep = new Set(ALSO_KEEP);
const untranslated = neutralRequired.filter((k) =>
  output.has(k) && output.get(k) === neutral.get(k) && !KEEP_ENGLISH.has(k) && !alsoKeep.has(k));

// Breakdown computed, never pinned: the non-Cli and human-Cli totals both grow with
// every string the app gains, and a hardcoded pair goes stale silently while the
// checked figure beside it stays right.
const nonCliRequired = neutralRequired.filter((k) => !k.startsWith('Cli.')).length;
console.log('translatable <data> in output:', output.size,
  '(expect', neutralRequired.length,
  '=', nonCliRequired, 'non-Cli +', neutralRequired.length - nonCliRequired, 'Cli)');
console.log('machine Cli <data> removed:', cliMachineRemoved, '(expect 21)');
console.log('MAP entries:', Object.keys(MAP).length, '| CRLF:', crlf, '(expect 0)');

if (alsoKeep.size) {
  console.log('ALSO_KEEP (' + alsoKeep.size + '), kept identical to English:');
  for (const k of alsoKeep) {
    const v = output.get(k);
    const words = v == null ? 0 : v.replace(/\{\d+(?::[^}]*)?\}/g, ' ').trim().split(/\s+/).filter(Boolean).length;
    const suspicious = v != null && (words > 2 || v.length > 24);
    console.log('   ' + (suspicious ? '!! suspicious (longer than a word or two) ' : '') + k + ' = ' + JSON.stringify(v));
  }
}

if (notApplied.length) console.log('!! value not applied (regex miss):', notApplied);
if (missingFromMap.length) console.log('!! in neutral but missing from MAP:', missingFromMap);
if (strayMapKeys.length) console.log('!! in MAP but not in neutral:', strayMapKeys);
if (missingFromOutput.length) console.log('!! required key missing from output:', missingFromOutput);
if (arityMismatch.length) console.log('!! placeholder arity differs from neutral:', arityMismatch);
if (machineLeaked.length) console.log('!! machine Cli keys leaked into output:', machineLeaked);
if (untranslated.length) {
  const show = untranslated.slice(0, 40).join(', ');
  console.log('!! still English (untranslated), ' + untranslated.length + ': ' + show +
    (untranslated.length > 40 ? ', ...and ' + (untranslated.length - 40) + ' more' : ''));
  if (untranslated.length > 50)
    console.log('   (that is most of the file: this is the untranslated template. Translate the MAP values, then a real miss is listed on its own.)');
}

const structuralOk = !notApplied.length && !missingFromMap.length && !strayMapKeys.length &&
  !missingFromOutput.length && !arityMismatch.length && !machineLeaked.length &&
  output.size === neutralRequired.length && cliMachineRemoved === 21 && crlf === 0;
const ok = structuralOk && !untranslated.length;
console.log(ok ? '\nGENERATION OK' : '\nGENERATION HAS ISSUES (see above)');
