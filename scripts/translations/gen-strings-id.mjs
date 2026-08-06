#!/usr/bin/env node
// Indonesian (id) satellite generator for InstallerClean. Copied from
// gen-strings-template.mjs / gen-strings-ko.mjs; only OUT, ALSO_KEEP and the MAP
// values differ. Works FROM THE ENGLISH SOURCE (Strings.resx): replaces each
// key's inner <value>, strips the machine-contract Cli.EventLog* keys, keeps
// the human Cli keys, and self-verifies against the neutral. Output is LF,
// UTF-8.
//
// Indonesian plural rule (DisplayHelpers.CategoryFor, case "id"): PluralCategory
// .Other at every count. Indonesian has no count inflection, so there are NO
// .One/.Few/.Many override keys (OVERRIDES empty), and the Plural.* pairs are
// identical (both "file" etc). The hardcoded .Singular/.Plural sentence pairs
// are translated on both members and come out identical; each placeholder set
// is preserved exactly.
//
// "file" and "patch" are byte-identical to the English .Singular, so they go in
// ALSO_KEEP (deliberate single-token keeps); their .Plural values differ from the
// English "files"/"patches" and pass the gate.
//
// Register: Anda (neutral-polite) throughout, the settled Indonesian software-UI
// convention (Windows id uses it too; there is no natural informal UI "you"),
// matching README.id.md. Platform terms sourced from Windows / Microsoft
// Terminology: About = Tentang, Start menu = menu Mulai, Event Log /
// Application log / Group Policy = Log Peristiwa / log Aplikasi / Kebijakan
// Grup, display language = bahasa tampilan.
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
  'Section.Backup.Folder': `BACKUP FOLDER`,
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
  'Action.BackupFolderPlaceholder': `Path to folder if you move rather than delete.`,
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
  'Automation.ConfirmDelete': `Delete permanently removes the unneeded files. Cancel closes without deleting.`,
  'Automation.ConfirmMove': `Pindahkan menaruh file yang tidak diperlukan di folder tujuan yang dipilih. Batal membiarkannya di tempatnya.`,
  'Automation.SayThanks': `Ucapkan terima kasih`,
  'Automation.ConfirmSendResultLog': `Kirim mengirimkan laporan yang ditampilkan ke No Faff. Batal tidak mengirim apa pun.`,
  'Automation.CheckForUpdates': `Periksa pembaruan`,
  'Automation.CheckForUpdates.HelpText': `Memeriksa halaman rilis github untuk mencari versi yang lebih baru.`,
  'Automation.UpdateAvailable.HelpText': `Buka halaman rilis untuk mengunduh versi yang lebih baru, atau batalkan untuk tetap memakai versi saat ini.`,
  'Automation.Licence.HelpText': `Membuka file lisensi di github.com melalui browser Anda.`,
  'Automation.Section.BackupFolder': `Backup folder`,
  'Automation.Section.Products': `Produk`,
  'Automation.Section.Patches': `Patch`,
  'Automation.Section.ProductDetails': `Detail produk`,
  'Automation.BackupFolder': `Backup folder`,
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
  'Tooltip.Move': `Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them.`,
  'Tooltip.MoveNeedsDestination': `Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them.`,
  'Tooltip.Delete': `Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away.`,
  'Tooltip.SigningCertificate': `Nama subjek dari sertifikat Authenticode yang disematkan. Rantai sertifikat tidak diverifikasi.`,

  // Body copy
  'Body.MainExplanation.Lead': `Any unneeded files below are [safe to delete].`,
  'Body.MainExplanation.Why': `File-file ini berada di {InstallerFolder}, tertinggal saat sebuah program dihapus instalasinya ({0}), patch yang lebih baru menggantikan yang lama ({1}), atau penerbitnya menariknya ({2}). InstallerClean hanya pernah mencantumkan file yang Windows sendiri laporkan sudah tidak terpakai.`,
  'Body.MainExplanation.Action': `Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored.`,
  'Body.PendingReboot.MsiExecuteMutex': `Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back.`,
  'Body.PendingReboot.InstallerInProgress': `A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}.`,
  'Body.PendingReboot.PendingRenameInCache': `Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning.`,
  'Body.NoFileSelected': `Pilih file untuk melihat detail.`,
  'Body.NoProductSelected': `Pilih produk untuk melihat detail.`,
  'Body.NoMetadata': `Tidak ada metadata yang tersedia.`,
  'Body.RegisteredMissingFromDisk': `This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.&#10;&#10;It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.&#10;&#10;To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself.`,
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
  'Status.Moving': `Moving unneeded files...`,
  'Status.Deleting': `Deleting unneeded files...`,
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
  'Completion.PermanentDeleteSummary.Singular': `{0} {1} permanently deleted`,
  'Completion.PermanentDeleteSummary.Plural': `{0} {1} permanently deleted`,

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
  'Error.DestinationInSystemFolder': `The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%.`,
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
  'Error.FileInUse.Singular': `This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later.`,
  'Error.FileInUse.Plural': `These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later.`,
  'Error.IOFailure.Singular': `Windows melaporkan kesalahan file; file dibiarkan di tempatnya.`,
  'Error.IOFailure.Plural': `Windows melaporkan kesalahan file; file-file ini dibiarkan di tempatnya.`,
  'Error.UnknownError.Singular': `Ada yang tidak beres dengan file ini; file dibiarkan di tempatnya.`,
  'Error.UnknownError.Plural': `Ada yang tidak beres dengan file-file ini; semuanya dibiarkan di tempatnya.`,

  // 0 = shell error code

  // 0 = destination
  'Error.MoveIntoInstaller': `Menolak memindahkan file ke dalam folder Windows Installer (tujuan: {0}).`,

  // 0 = the relative path the caller passed
  'Error.DestinationNotFullyQualified': `The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\\Backup, or \\\\server\\backup). InstallerClean can't use this one: {0}`,
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
  'Error.DestinationChangedMidBatch': `InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then Re-scan and try again.`,

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
  'CrashLog.PrivacyHeader': `# crash.log captures unhandled exceptions from InstallerClean.\n# Under elevation the framework's exception messages can include\n# file paths from the running session (including other users'\n# profiles enumerated by Windows Installer queries). Network-\n# failure messages from the update check or result-log POST can\n# include the destination URL and the resolved IP / proxy address.\n# Entries about unreadable Windows Installer records can include a\n# Windows account SID (S-1-5-21-...) and the product codes of\n# installed software.\n# Redact all three classes of detail before attaching this file to\n# a public bug report.\n`,
  'Tooltip.ChangeLanguage': `Ganti bahasa. Program akan dimulai ulang.`,
  'Automation.ChangeLanguage': `Ganti bahasa`,
  'Automation.ChangeLanguage.HelpText': `Program akan dimulai ulang.`,

  // Command-line tool (installerclean-cli): the HUMAN-facing Cli.* keys.
  // Descriptions translated; command tokens, flags, the {InstallerFolder} token
  // and the exit-code numbers verbatim; leading spaces kept (the screen is
  // column-aligned for a monospace terminal); PATH metavariable -> JALUR.
  'Cli.UnknownArgument': `Error: unknown argument '{0}'`,
  'Cli.Cancelling': `Membatalkan...`,
  'Cli.Cancelled': `Dibatalkan.`,
  'Cli.GenericError': `Error: unexpected failure ({0}). Details written to {1}.`,
  'Cli.GenericError.NoLog': `Error: unexpected failure ({0}). The crash log could not be written.`,
  'Cli.ScanningInstaller': `Memindai {InstallerFolder}...`,
  'Cli.FoundOrphans': `Found {0} unneeded {1} to clean up ({2}).`,
  'Cli.DeletingFiles': `Deleting {0} unneeded {1}...`,
  'Cli.DeletedFiles': `Permanently deleted {0} unneeded {1}.`,
  'Cli.NoMoveDestination': `Kesalahan: tujuan pemindahan tidak ditentukan. Gunakan /m JALUR. (Default yang diatur di GUI bersifat per-pengguna dan tidak berlaku untuk tugas terjadwal atau proses akun layanan.)`,
  'Cli.MoveDestinationInsideInstaller': `Kesalahan: tujuan tidak boleh berada di dalam folder Windows Installer.`,
  'Cli.MoveDestinationRelative': `Kesalahan: tujuan harus berupa jalur absolut lengkap. Diterima: {0}`,
  'Cli.MoveDestinationInSystemFolder': `Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}.`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning.`,
  'Cli.MovingFiles': `Moving {0} unneeded {1} to {2}...`,
  'Cli.MovedFiles': `Moved {0} unneeded {1}.`,
  'Cli.MutexBlocked': `Proses InstallerClean lain memegang kunci instans-tunggal (GUI atau proses CLI lain). Kode keluar 75 (sementara); aman untuk dicoba lagi nanti.`,
  'Cli.EventLogUnavailable': `Catatan: penulisan ke Log Peristiwa gagal. Periksa izin log Aplikasi atau Kebijakan Grup.`,
  'Cli.Help.Header': `InstallerClean - pembersihan {InstallerFolder}`,
  'Cli.Help.Usage': `Penggunaan:`,
  'Cli.Help.Help': `  installerclean-cli --help     Tampilkan bantuan ini (juga /?, -h)`,
  'Cli.Help.Version': `  installerclean-cli --version  Cetak versi (juga -v)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s         Scan only - list unneeded files`,
  'Cli.Help.Delete': `  installerclean-cli /d         Delete unneeded files permanently`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m         Move to the saved backup folder`,
  'Cli.Help.MovePath': `  installerclean-cli /m JALUR   Pindahkan ke jalur yang ditentukan`,
  'Cli.Help.NoteLine1': `installerclean-cli blocks the prompt until it finishes, so a script or&#10;scheduled task can wait on it.`,
  'Cli.Help.ExitCodesHeader': `Kode keluar:`,
  'Cli.Help.ExitCodeOk': `  0   success: the run finished with nothing left to do`,
  'Cli.Help.ExitCodeError': `  1   failure: nothing processed (bad arguments, a bad destination, a&#10;       failed scan or every file failed)`,
  'Cli.Help.ExitCodePartial': `  2   partial: some processed, some not (a failure or a Ctrl+C part way)`,
  'Cli.Help.ExitCodeTransient': `  75  sementara: kondisi sementara memblokir proses (lihat pesannya)`,
  'Cli.Help.ExitCodeCancelled': `  130 dibatalkan (Ctrl+C)`,
  'Body.NotScanned.Lead': `Belum ada yang dipindai.`,
  'Body.NotScanned.Why': `Tekan Pindai ulang untuk menelusuri {InstallerFolder} mencari file penginstal yang tidak lagi diperlukan program mana pun.`,
  'Confirm.MoveSameDrive': `That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away.`,
  'Error.ScanCorrelationFailed': `InstallerClean couldn't square this scan with the Windows Installer records: nearly every file Windows still lists as needed is missing from {InstallerFolder}, while what is actually in the folder matches almost nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed.`,
  'Error.CandidateOutsideCache': `File ini tidak berada langsung di dalam folder Windows Installer; ditolak demi keamanan.`,
  'Completion.ReverifySkipped': `{0} {1} kept in place, because the records now claim what the scan flagged.`,
  'Completion.MoveCancelledSummary': `{0} dari {1} {2} dipindahkan sebelum Anda membatalkan.`,
  'Completion.PermanentDeleteCancelledSummary': `{0} dari {1} {2} dihapus permanen sebelum Anda membatalkan.`,
  'Body.PendingReboot.Lead': `File-file ini tidak bisa dibersihkan sekarang.`,
  'Cli.TooManyArguments': `Kesalahan: argumen tambahan yang tidak terduga '{0}'. Jika folder pemindahan Anda mengandung spasi, apit seluruh jalur dengan tanda kutip: /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `That folder is saved per-user; scheduled or SYSTEM runs need /m PATH.`,
  'Completion.ReverifyIncomplete': `{0} {1} kept in place, because the Windows Installer records could not be fully read in the final check.`,
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
  'Tooltip.MoveSameDrive': `Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them.`,
  'Completion.MoveRestoreHint.Singular': `The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely).`,
  'Completion.MoveRestoreHint.Plural': `The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely).`,
  'Completion.MoveRestoreHintSameDrive.Singular': `The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely).`,
  'Completion.MoveRestoreHintSameDrive.Plural': `The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely).`,
  'Confirm.DeletePermanently.Singular': `This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead.`,
  'Confirm.DeletePermanently.Plural': `Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead.`,
  'Error.ScanCacheRootUnresolved': `InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed.`,
  'Automation.Scroll.ProductDetails': `Product details`,
  'Body.PendingReboot.Other': `Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back.`,
  'Cli.TooManyArgumentsNoPath': `Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run.`,
  'Cli.MissingFromDisk.Singular': `{0} registered file is missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, the same version, usually restores it.`,
  'Cli.MissingFromDisk.Plural': `{0} registered files are missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, the same version, usually restores them.`,
  'Cli.MoveNotEnoughSpace': `Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved.`,
  'Cli.PendingRebootBlocked.Other': `Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes.`,
  'Cli.FoundNoOrphans': `Found no unneeded files.`,
  'Cli.DestinationChangedMidBatch': `InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then run the command again.`,
  'Cli.Help.Summary': `Removes cached .msi and .msp files that no installed program still needs.`,
  'Cli.Help.Elevation': `Needs an elevated (administrator) prompt; Windows will not start it.`,
  'Error.InstallerLockUnavailableTitle': `Tidak ada yang dihapus`,
  'Error.InstallerLockUnavailable': `InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening.`,
  'Cli.InstallerLockUnavailable': `Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening.`,
  'Completion.ReverifyRecordsChanged': `{0} {1} kept in place, because the Windows Installer records had changed by the final check.`,
  'Summary.RecordsNotMatched': `InstallerClean couldn't match up everything in the Windows records, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again.`,
  'Cli.RecordsNotMatched': `InstallerClean couldn't match up everything in the Windows records, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up.`,
};

let text = readFileSync(BASE, 'utf8');

// Remove ONLY the machine-contract Cli.* <data> elements BY NAME (the
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
// Derived, never pinned: the machine set grows whenever the command line
// gains an event-log string, and a literal here would fail every generator
// at once while asserting nothing about what was actually stripped.
const cliMachineExpected = [...neutral.keys()].filter(isMachineCliKey).length;
const written = readFileSync(OUT, 'utf8');
const output = parse(written);
const neutralRequired = [...neutral.keys()].filter((k) => !isMachineCliKey(k));

const missingFromMap = neutralRequired.filter((k) => !(k in MAP));
const strayMapKeys = Object.keys(MAP).filter((k) => !neutral.has(k));
const machineLeaked = [...output.keys()].filter(isMachineCliKey);

// The one human-facing Cli.EventLog* key, asserted present rather than left to
// the counts: a predicate that stopped discriminating it takes it out of the
// output AND out of the required set, so every figure above still agrees. The
// MAP substitution notices today only through the order the two run in.
const humanCliStripped = !output.has('Cli.EventLogUnavailable');
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
console.log('machine Cli <data> removed:', cliMachineRemoved, `(expect ${cliMachineExpected})`);
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
if (humanCliStripped) console.log('!! Cli.EventLogUnavailable stripped: that key is human-facing and must stay');
if (untranslated.length) {
  const show = untranslated.slice(0, 40).join(', ');
  console.log('!! still English (untranslated), ' + untranslated.length + ': ' + show +
    (untranslated.length > 40 ? ', ...and ' + (untranslated.length - 40) + ' more' : ''));
  if (untranslated.length > 50)
    console.log('   (that is most of the file: this is the untranslated template. Translate the MAP values, then a real miss is listed on its own.)');
}

const structuralOk = !notApplied.length && !missingFromMap.length && !strayMapKeys.length &&
  !missingFromOutput.length && !arityMismatch.length && !machineLeaked.length &&
  !humanCliStripped &&
  output.size === neutralRequired.length && cliMachineRemoved === cliMachineExpected && crlf === 0;
const ok = structuralOk && !untranslated.length;
console.log(ok ? '\nGENERATION OK' : '\nGENERATION HAS ISSUES (see above)');
