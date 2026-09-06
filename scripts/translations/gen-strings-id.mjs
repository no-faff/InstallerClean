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

// Universal keeps: keys whose value is the same in every language, the brand names
// and the pure-placeholder announcement string. Their still-English value is NOT a
// miss. Explicit by KEY on purpose: a future brand key then defaults to "flag until
// someone adds it here", never silently passes. Do NOT translate these values. Do
// NOT edit this list per language.
//
// The four size suffixes and the two elapsed suffixes do not belong in this list,
// because they are not universal: French writes Go/Mo/Ko/o, Russian and Ukrainian
// write ГБ/МБ/КБ/Б and мс/с. Those three carry real
// values in their MAP; the languages that do abbreviate as English does keep them in
// ALSO_KEEP, which is the per-language list. Display.ListSeparator is the same shape,
// for the same reason.
const KEEP_ENGLISH = new Set([
  'Window.Main.Title',                 // InstallerClean
  'Startup.AlreadyRunningTitle',       // InstallerClean
  'Startup.UnhandledTitle',            // InstallerClean
  'Automation.ScanResultAnnouncement', // {0} ({1})
]);

// Per-language keeps: "file"/"patch" (and the Field.File label) are identical to
// the English, a deliberate keep (Indonesian uses the same loanword "file", and
// Windows id keeps "File"), not a missed value.
const ALSO_KEEP = [
  'Plural.File.Singular',
  'Plural.Patch.Singular',
  'Field.File',
  // The list separator Indonesian uses is the one English uses. A punctuation
  // mark rather than a word, so there is nothing to translate and nothing to
  // get wrong; only ja and zh-Hans differ, taking the ideographic comma.
  'Display.ListSeparator',       // ", "
  // The size and elapsed unit suffixes. Indonesian abbreviates them exactly as
  // English does, so there is nothing to translate and nothing to get wrong.
  // A per-language keep rather than a universal one because fr, ru and uk do
  // NOT: French takes Go/Mo/Ko/o, Russian and Ukrainian take ГБ/МБ/КБ/Б and
  // мс/с, and all three carry real values in their MAP.
  'Display.Size.GB',           // {0:F2} GB
  'Display.Size.MB',           // {0:F1} MB
  'Display.Size.KB',           // {0:F1} KB
  'Display.Size.B',            // {0} B
  'Display.Elapsed.Ms',        // {0:F0}ms
  'Display.Elapsed.S',         // {0:F1}s
];

const MAP = {
  // Window titles
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `Tentang`,
  'Window.Registered.Title': `File yang dibiarkan apa adanya`,
  'Window.Orphaned.Title': `File tidak diperlukan yang aman dihapus`,

  // Section headings
  // Section.Registered.Products and Automation.Section.Products are deliberately
  // not here. They left the neutral resx when the registered-files window stopped
  // having a products group of its own, so a value for either would be
  // regenerated into a key the app cannot use.
  'Section.Registered.Patches': `PATCH`,
  'Section.Registered.Details': `DETAIL PRODUK`,
  'Section.Backup.Folder': `FOLDER CADANGAN`,
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
  'Action.BackupFolderPlaceholder': `Jalur ke folder jika Anda memindahkan, bukan menghapus.`,
  'Action.OpenReleasePage': `Buka halaman _rilis`,
  'Action.Rescan': `Pindai _ulang`,
  'Action.ScanAgain': `Pindai _lagi`,
  'Action.SendResultLog': `Kirim laporan`,
  'Action.SendResultLogConfirm': `_Kirim`,

  // Automation names (screen reader / accessibility)
  'Automation.BuyMeACuppa': `Donasi`,
  'Automation.BuyMeACuppa.About': `Traktir saya secangkir kopi`,
  // The three Cancel names name the button and then say which one, the shape
  // Automation.ViewOrphanedFiles and its sibling already take, rather than the
  // verb batalkan: WCAG 2.5.3 (Label in Name) asks that a control's spoken name
  // contain the word drawn on it, and batal is not a word inside batalkan, only
  // its first five letters. Batal is what Microsoft's own Indonesian puts on
  // that button (comdlg32.dll string 372, en-US "Cancel"), and batalkan is what
  // it uses for Undo (windows.ui.xaml.dll string 5037). The alignment is about
  // the WORD alone. Casing is decided separately, by ear, and
  // Automation.CheckForUpdates.HelpText holds the reasoning for the one that
  // looks like a slip and is not.
  'Automation.CancelOperation': `Batal, operasi`,
  'Automation.CancelScan': `Batal, pemindaian`,
  'Automation.CancelStartupScan': `Batal, pemindaian awal`,
  'Automation.Close': `Tutup`,
  'Automation.CloseWindow': `Tutup jendela`,
  'Automation.CloseResult': `Tutup hasil dan kembali ke jendela utama`,
  'Automation.LeaveStarOnGitHub.About': `Beri bintang di github`,
  'Automation.Minimise': `Kecilkan`,
  'Automation.ConfirmDelete': `Hapus permanen menyingkirkan file yang tidak diperlukan. Batal menutup jendela tanpa menghapus apa pun.`,
  'Automation.ConfirmMove': `Pindahkan menaruh file yang tidak diperlukan di folder tujuan yang dipilih. Batal membiarkannya di tempatnya.`,
  'Automation.SayThanks': `Ucapkan terima kasih`,
  'Automation.ConfirmSendResultLog': `Kirim mengirimkan laporan yang ditampilkan ke No Faff. Batal tidak mengirim apa pun.`,
  'Automation.CheckForUpdates': `Periksa pembaruan`,
  'Automation.CheckForUpdates.HelpText': `Memeriksa halaman rilis github untuk mencari versi yang lebih baru.`,
  'Automation.UpdateAvailable.HelpText': `Buka halaman rilis untuk mengunduh versi yang lebih baru, atau batalkan untuk tetap memakai versi saat ini.`,
  'Automation.Licence.HelpText': `Membuka file lisensi di github.com melalui browser Anda.`,
  'Automation.Section.BackupFolder': `Folder cadangan`,
  'Automation.Section.Patches': `Patch`,
  'Automation.Section.ProductDetails': `Detail produk`,
  'Automation.BackupFolder': `Folder cadangan`,
  'Automation.OperationProgress': `Kemajuan operasi`,
  'Automation.RescanInstaller': `Pindai ulang {InstallerFolder}`,
  'Automation.ScanningProgress': `Kemajuan pemindaian`,
  'Automation.StartupScanProgress': `Kemajuan pemindaian awal`,
  'Automation.ViewOrphanedFiles': `Detail, file yang tidak diperlukan`,
  'Automation.ViewOrphanedFiles.HelpText': `Tersedia untuk dibersihkan.`,
  'Automation.ViewRegisteredFiles': `Detail, file yang dibiarkan apa adanya`,
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
  'Tooltip.Move': `Memindahkan file yang tidak diperlukan ke folder cadangan.`,
  'Tooltip.MoveNeedsDestination': `Memindahkan file yang tidak diperlukan ke sebuah folder cadangan. Anda akan memilihnya sebentar lagi.`,
  'Tooltip.Delete': `Menghapus permanen file yang tidak diperlukan. Gunakan Pindahkan saja kalau Anda ingin kesempatan meyakinkan diri bahwa semuanya baik-baik saja.`,
  'Tooltip.SigningCertificate': `Nama subjek dari sertifikat Authenticode yang disematkan. Rantai sertifikat tidak diverifikasi.`,

  // Body copy
  'Body.MainExplanation.Lead': `File apa pun yang tidak diperlukan di bawah ini [aman dihapus].`,
  'Body.MainExplanation.Why': `File-file itu ada di {InstallerFolder}. InstallerClean menanyakan setiap program yang terpasang kepada Windows: sebuah file masuk daftar jika tidak ada program yang mengakuinya ({0}), atau jika sebuah patch yang lebih baru telah menggantikannya dan tidak ada program yang bisa kembali kepadanya ({1}).`,
  'Body.MainExplanation.Action': `Pindahkan ke folder cadangan pilihan Anda, lalu hapus folder itu setelah Anda yakin program-program Anda masih bisa diperbarui dan dicopot seperti biasa. Mengembalikannya ke {InstallerFolder} memulihkan semuanya. Atau hapus permanen sekarang.`,
  'Body.PendingReboot.MsiExecuteMutex': `Ada yang sedang memakai Windows Installer saat ini, misalnya pembaruan Windows atau program yang memasang diri di latar belakang. Pindahkan dan Hapus dijeda selama itu berjalan, sehingga InstallerClean tidak menyentuh {InstallerFolder} selagi berubah. Setelah selesai, pindai ulang dan keduanya kembali aktif.`,
  'Body.PendingReboot.InstallerInProgress': `Ada transaksi Windows Installer sebelumnya yang tertunda di mesin ini. Lanjutkan atau batalkan pemasangan itu (atau mulai ulang Windows) sebelum membersihkan {InstallerFolder}.`,
  'Body.PendingReboot.PendingRenameInCache': `Windows mengantrekan penggantian nama file untuk mulai ulang berikutnya yang memengaruhi {InstallerFolder}. Mulai ulang Windows sebelum membersihkan.`,
  'Body.NoFileSelected': `Pilih file untuk melihat detail.`,
  'Body.NoProductSelected': `Pilih produk untuk melihat detail.`,
  'Body.NoMetadata': `Tidak ada metadata yang tersedia.`,
  'Body.RegisteredMissingFromDisk': `File pemasang ini hilang. Sekarang tidak menimbulkan masalah, dan tidak akan menimbulkannya sampai suatu hari Anda mencoba memperbarui atau mencopot program pemiliknya. Langkah itu bisa gagal, karena Windows mencari file ini dan tidak menemukannya.\n\nUntuk mengembalikannya, Anda butuh pemasang versi yang sudah Anda miliki. Dapatkan dari pembuat programnya dan jalankan di atas salinan yang ada. Versi yang lebih baru tidak bisa: versi baru harus lebih dulu menghapus versi yang Anda miliki, dan justru langkah itulah yang membutuhkan file ini. Mencopot lebih dulu juga tidak berhasil, karena alasan yang sama. Ini semestinya memulihkan file itu dan membiarkan pengaturan Anda apa adanya, tetapi Microsoft tidak menjaminnya.`,
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
  'Status.Moving': `Memindahkan file yang tidak diperlukan...`,
  'Status.Deleting': `Menghapus file yang tidak diperlukan...`,
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
  'Completion.PermanentDeleteSummary.Singular': `{0} {1} dihapus permanen`,
  'Completion.PermanentDeleteSummary.Plural': `{0} {1} dihapus permanen`,

  // Summaries
  'Summary.RegisteredStillUsed.Singular': `{0} file dibiarkan apa adanya`,
  'Summary.RegisteredStillUsed.Plural': `{0} file dibiarkan apa adanya`,
  'Summary.OrphanedToCleanUp.Singular': `{0} file tidak diperlukan untuk dibersihkan`,
  'Summary.OrphanedToCleanUp.Plural': `{0} file tidak diperlukan untuk dibersihkan`,
  'Summary.NothingListed.Singular': `InstallerClean tidak bisa memastikan file mana dalam cache yang menjadi milik program-program yang terpasang di sini, jadi satu-satunya file itu ditahan alih-alih ditawarkan.`,
  'Summary.NothingListed.Plural': `InstallerClean tidak bisa memastikan file mana dalam cache yang menjadi milik program-program yang terpasang di sini, jadi {0} {1} ditahan alih-alih ditawarkan.`,
  'Summary.MissingFromDisk.Singular': `Windows punya catatan untuk {0} file yang tidak ada di {InstallerFolder}: {1}. Sehari-hari ini tidak menimbulkan masalah, tetapi pembaruan atau pencopotan program itu bisa gagal. Buka Detail untuk tahu apa yang harus dilakukan.`,
  'Summary.MissingFromDisk.Plural': `Windows punya catatan untuk {0} file yang tidak ada di {InstallerFolder}: {1}. Sehari-hari ini tidak menimbulkan masalah, tetapi pembaruan atau pencopotan program-program itu bisa gagal. Buka Detail untuk tahu apa yang harus dilakukan.`,
  'Summary.MissingFromDisk.OtherPrograms.Singular': `{0} program lain`,
  'Summary.MissingFromDisk.OtherPrograms.Plural': `{0} program lain`,
  'Summary.MissingFromDisk.Unnamed.Singular': `{0} file tanpa nama program dalam catatan`,
  'Summary.MissingFromDisk.Unnamed.Plural': `{0} file tanpa nama program dalam catatan`,

  // 0 = current file count, 1 = total count, 2 = pluralised noun.
  'Summary.OperationFiles': `{0} dari {1} {2}`,

  // Orphaned-window footer. 0 = orphaned count, 1 = superseded count,
  // 2 = obsoleted count, 3 = size display.
  'Summary.OrphanedWindow': `{0} {1} tidak diperlukan ({2})`,

  // Registered-window footer. 0 = count, 1 = size display.
  'Summary.RegisteredWindow.Singular': `{0} file dibiarkan apa adanya ({1})`,
  'Summary.RegisteredWindow.Plural': `{0} file dibiarkan apa adanya ({1})`,

  // Confirmation dialogs

  // 0 = file count, 1 = pluralised "file"/"files", 2 = size display
  'Confirm.MoveTitle': `Pindahkan {0} {1} ({2})?`,

  'Confirm.DeleteTitle': `Hapus {0} {1} ({2})?`,

  // Error messages
  'Error.AdminRequiredTitle': `Akses ditolak`,
  'Error.AdminRequiredBody': `Windows menolak akses untuk InstallerClean, jadi prosesnya dihentikan. Tidak ada yang dihapus.\n\nInstallerClean sudah berjalan sebagai administrator, jadi menjalankannya lagi dengan cara itu tidak akan membantu. Windows tidak menjelaskan lebih jauh apa yang menolak akses, jadi tidak ada hal khusus yang bisa dicoba.`,
  'Error.InstallerDbUnavailableTitle': `Catatan Windows Installer tidak bisa dibaca`,
  'Error.ScanFailedTitle': `Pemindaian gagal`,
  'Error.InstallerDbEmpty': `Catatan Windows Installer kembali sepenuhnya kosong: tidak satu pun program terpasang atau pembaruan yang mengklaim file pemasang di cache. Itu tidak terjadi pada komputer yang berfungsi (bahkan pemasangan Windows yang baru pun punya beberapa), jadi catatannya rusak atau tidak bisa dibaca, dan pemindaian yang memercayai jawaban ini akan keliru menyebut setiap file di {InstallerFolder} terisolasi. InstallerClean berhenti sebagai gantinya. Tidak ada yang dihapus.`,
  'Error.MsiAccessDenied': `Windows Installer tidak mengizinkan InstallerClean menampilkan daftar apa saja yang terpasang. InstallerClean sudah berjalan sebagai administrator, jadi menjalankannya lagi sebagai administrator tidak akan mengubah apa pun. Tanpa daftar itu tidak ada cara yang aman untuk mengetahui file cache mana yang masih diperlukan, jadi InstallerClean berhenti. Tidak ada yang dihapus.`,
  'Error.MsiNonSuccess': `Windows Installer tidak bisa memberi InstallerClean daftar program terpasang yang terbaca: InstallerClean membaca {2} {3}, lalu {0} entri berturut-turut kembali tidak terbaca (kode kesalahan terakhir {1}). Alih-alih bekerja dengan daftar yang hanya terbaca sebagian, InstallerClean berhenti. Tidak ada yang dihapus.`,
  'Error.InvalidDestinationTitle': `Tujuan tidak valid`,
  'Error.DestinationWriteFailedTitle': `Tidak bisa menulis ke tujuan`,
  'Error.MoveFailedTitle': `Pemindahan gagal`,
  'Error.DeleteFailedTitle': `Penghapusan gagal`,
  'Error.SettingNotSavedTitle': `Pengaturan tidak tersimpan`,
  'Error.SettingNotSavedBody': `Perubahan tidak dapat disimpan. Saat berikutnya dijalankan, InstallerClean akan kembali ke pengaturan sebelumnya.`,
  'Error.DestinationInsideInstaller': `Tujuan tidak boleh berada di dalam folder Windows Installer.`,

  // 0 = the destination path the user typed
  'Error.DestinationInSystemFolder': `Tujuan {0} mengarah ke dalam folder sistem Windows. Pilih jalur di luar %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% dan %ProgramData%.`,
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
  'Error.FileInUse.Singular': `File ini sedang dibuka atau dikunci program lain, jadi untuk saat ini tidak ada yang bisa menyingkirkannya. File itu dibiarkan di tempatnya; coba lagi nanti.`,
  'Error.FileInUse.Plural': `File-file ini sedang dibuka atau dikunci program lain, jadi untuk saat ini tidak ada yang bisa menyingkirkannya. Semuanya dibiarkan di tempatnya; coba lagi nanti.`,
  'Error.IOFailure.Singular': `Windows melaporkan kesalahan file; file dibiarkan di tempatnya.`,
  'Error.IOFailure.Plural': `Windows melaporkan kesalahan file; file-file ini dibiarkan di tempatnya.`,
  'Error.UnknownError.Singular': `Ada yang tidak beres dengan file ini; file dibiarkan di tempatnya.`,
  'Error.UnknownError.Plural': `Ada yang tidak beres dengan file-file ini; semuanya dibiarkan di tempatnya.`,

  // 0 = shell error code

  // 0 = destination
  'Error.MoveIntoInstaller': `Menolak memindahkan file ke dalam folder Windows Installer (tujuan: {0}).`,

  // 0 = the relative path the caller passed
  'Error.DestinationNotFullyQualified': `Folder cadangan harus berupa jalur lengkap ke sebuah folder, diawali huruf drive atau berbagi jaringan (misalnya D:\\Backup, atau \\\\server\\backup). InstallerClean tidak bisa memakai yang ini: {0}`,
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
  'BrowserLaunch.ClipboardOk': `The link is on your clipboard, so you can paste it in yourself:&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean couldn't copy the link to your clipboard either, so here it is:&#10;&#10;{0}`,

  // 0 = the destination folder whose canonical path changed mid-batch
  'Error.DestinationChangedMidBatch': `InstallerClean tidak bisa lagi memastikan folder cadangan, jadi berhenti daripada menulis ke tempat yang salah. Periksa {0}, lalu Pindai ulang dan coba lagi.`,
  // 0 = folder, 1 = inner exception message
  'Error.CannotWriteFolder': `Tidak bisa menulis ke {0}.`,

  // 0 = file name
  'Error.DestinationCollision': `File bernama '{0}' sudah ada di folder cadangan.`,

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
  'Display.ListSeparator': `, `,
  'Display.ElapsedLong.LessThanASecond': `kurang dari satu detik`,
  'Display.ElapsedLong.Seconds': `{0:F1} detik`,
  'CrashLog.PrivacyHeader': `# crash.log merekam eksepsi tak tertangani dari InstallerClean.\n# Dengan hak tinggi, pesan eksepsi framework bisa memuat jalur file\n# dari sesi yang berjalan (termasuk profil pengguna lain yang didata\n# oleh kueri Windows Installer). Pesan kegagalan jaringan dari\n# pemeriksaan pembaruan atau pengiriman log hasil bisa memuat URL\n# tujuan serta alamat IP atau proksi yang teruraikan. Entri tentang\n# catatan Windows Installer yang tak terbaca bisa memuat SID akun\n# Windows (S-1-5-21-...) dan kode produk perangkat lunak terpasang.\n# Hapus ketiga jenis rincian itu sebelum melampirkan berkas ini ke\n# laporan bug publik.\n`,
  'Tooltip.ChangeLanguage': `Ganti bahasa. Program akan dimulai ulang.`,
  'Automation.ChangeLanguage': `Ganti bahasa`,
  'Automation.ChangeLanguage.HelpText': `Program akan dimulai ulang.`,

  // Command-line tool (installerclean-cli): the HUMAN-facing Cli.* keys.
  // Descriptions translated; command tokens, flags, the {InstallerFolder} token
  // and the exit-code numbers verbatim; leading spaces kept (the screen is
  // column-aligned for a monospace terminal); PATH metavariable -> JALUR.
  'Cli.UnknownArgument': `Kesalahan: argumen tidak dikenal '{0}'`,
  'Cli.Cancelling': `Membatalkan...`,
  'Cli.Cancelled': `Dibatalkan.`,
  'Cli.GenericError': `Kesalahan: kegagalan tak terduga ({0}). Rincian ditulis ke {1}.`,
  'Cli.GenericError.NoLog': `Kesalahan: kegagalan tak terduga ({0}). Log kerusakan tidak bisa ditulis.`,
  'Cli.ScanningInstaller': `Memindai {InstallerFolder}...`,
  'Cli.FoundOrphans': `Ditemukan {0} {1} tidak diperlukan untuk dibersihkan ({2}).`,
  'Cli.DeletingFiles': `Menghapus {0} {1} yang tidak diperlukan...`,
  'Cli.DeletedFiles': `{0} {1} yang tidak diperlukan telah dihapus permanen.`,
  'Cli.NoMoveDestination': `Kesalahan: tujuan pemindahan tidak ditentukan. Gunakan /m JALUR. (Default yang diatur di GUI bersifat per-pengguna dan tidak berlaku untuk tugas terjadwal atau proses akun layanan.)`,
  'Cli.MoveDestinationInsideInstaller': `Kesalahan: tujuan tidak boleh berada di dalam folder Windows Installer.`,
  'Cli.MoveDestinationRelative': `Kesalahan: tujuan harus berupa jalur absolut lengkap. Diterima: {0}`,
  'Cli.MoveDestinationInSystemFolder': `Kesalahan: tujuan {0} mengarah ke dalam folder sistem Windows. Pilih jalur di luar %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% dan %ProgramData%.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Kesalahan: ada yang sedang memakai Windows Installer saat ini, misalnya pembaruan Windows atau program yang memasang diri di latar belakang. /m dan /d diblokir selama itu berjalan. Coba lagi setelah selesai.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Kesalahan: ada transaksi Windows Installer sebelumnya yang tertunda di mesin ini. Lanjutkan atau batalkan pemasangan itu (atau mulai ulang Windows) sebelum membersihkan {InstallerFolder}.`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Kesalahan: operasi file yang diantrekan setelah mulai ulang menyasar {InstallerFolder} ({0}). Mulai ulang Windows untuk menyelesaikan operasi itu sebelum membersihkan.`,
  'Cli.MovingFiles': `Memindahkan {0} {1} yang tidak diperlukan ke {2}...`,
  'Cli.MovedFiles': `{0} {1} yang tidak diperlukan telah dipindahkan.`,
  'Cli.MutexBlocked': `Proses InstallerClean lain memegang kunci instans-tunggal (GUI atau proses CLI lain). Kode keluar 75 (sementara); aman untuk dicoba lagi nanti.`,
  'Cli.EventLogUnavailable': `Catatan: penulisan ke Log Peristiwa gagal. Periksa izin log Aplikasi atau Kebijakan Grup.`,
  'Cli.Help.Header': `InstallerClean - pembersihan {InstallerFolder}`,
  'Cli.Help.Usage': `Penggunaan:`,
  'Cli.Help.Help': `  installerclean-cli --help     Tampilkan bantuan ini (juga /?, -h)`,
  'Cli.Help.Version': `  installerclean-cli --version  Cetak versi (juga -v)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s         Pindai saja - daftar yang tak dipakai`,
  'Cli.Help.Delete': `  installerclean-cli /d         Hapus permanen yang tidak diperlukan`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m         Pindahkan ke folder cadangan tersimpan`,
  'Cli.Help.MovePath': `  installerclean-cli /m JALUR   Pindahkan ke jalur yang ditentukan`,
  'Cli.Help.NoteLine1': `installerclean-cli menahan prompt sampai selesai, sehingga skrip atau&#10;tugas terjadwal bisa menunggunya.`,
  'Cli.Help.ExitCodesHeader': `Kode keluar:`,
  'Cli.Help.ExitCodeOk': `  0   berhasil: menjalankan yang diminta dan tidak ada yang gagal`,
  'Cli.Help.ExitCodeError': `  1   gagal: tidak ada yang diproses (argumen atau tujuan salah,&#10;       pemindaian gagal, atau semua file gagal)`,
  'Cli.Help.ExitCodePartial': `  2   sebagian: sebagian diproses, sebagian tidak (gagal atau Ctrl+C)`,
  'Cli.Help.ExitCodeTransient': `  75  sementara: kondisi sementara memblokir proses (lihat pesannya)`,
  'Cli.Help.ExitCodeCancelled': `  130 dibatalkan (Ctrl+C)`,
  'Body.NotScanned.Lead': `Belum ada yang dipindai.`,
  'Body.NotScanned.Why': `Tekan Pindai ulang untuk menelusuri {InstallerFolder} mencari file penginstal yang tidak lagi diperlukan program mana pun.`,
  'Confirm.MoveSameDrive': `Folder itu ada di drive yang sama, jadi ruangnya belum kembali sampai Anda menghapusnya. Pilih folder di drive lain kalau Anda ingin ruangnya langsung kembali.`,
  'Error.ScanCorrelationFailed': `InstallerClean tidak bisa mencocokkan catatan Windows Installer dengan isi {InstallerFolder}. Hampir tidak ada yang ditunjuk catatan itu benar-benar ada di sana, dan hampir tidak ada yang ada di sana disebut oleh catatan mana pun, jadi tidak ada file yang bisa ditunjukkan tidak diperlukan. Tidak ada yang ditawarkan dan tidak ada yang disingkirkan.`,
  'Error.CandidateOutsideCache': `File ini tidak berada langsung di dalam folder Windows Installer; ditolak demi keamanan.`,
  'Completion.MoveCancelledSummary': `{0} dari {1} {2} dipindahkan sebelum Anda membatalkan.`,
  'Completion.PermanentDeleteCancelledSummary': `{0} dari {1} {2} dihapus permanen sebelum Anda membatalkan.`,
  'Body.PendingReboot.Lead': `File-file ini tidak bisa dibersihkan sekarang.`,
  'Cli.TooManyArguments': `Kesalahan: argumen tambahan yang tidak terduga '{0}'. Jika folder tujuan Anda mengandung spasi, apit seluruh jalur dengan tanda kutip: /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `Folder disimpan per pengguna; tugas terjadwal atau SYSTEM: /m JALUR.`,
  'Error.ScanRecordsUnreadable': `InstallerClean tidak bisa membaca cukup banyak catatan Windows Installer untuk memastikan apa yang masih diperlukan: daftar program terpasang kembali tidak lengkap, dan membaca catatan yang sama langsung dari registri juga menemui kesalahan. Sebuah file bisa tampak terisolasi hanya karena catatan yang menyebutkannya termasuk yang tidak terbaca, jadi InstallerClean berhenti. Tidak ada yang dihapus.`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer tidak pernah menandai akhir daftar program terpasang: InstallerClean membaca {2} {3}, lalu menyerah setelah {0} entri (kode kesalahan terakhir {1}). Daftar yang tidak berujung tidak bisa dipercaya, jadi InstallerClean berhenti. Tidak ada yang dihapus.`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer tidak pernah menandai akhir daftar tambalan sebuah program: InstallerClean membaca {2} {3}, lalu menyerah setelah {0} entri (kode kesalahan terakhir {1}). Daftar yang tidak berujung tidak bisa dipercaya, jadi InstallerClean berhenti. Tidak ada yang dihapus.`,
  'UpdateCheck.Status.UpdateAvailable': `Versi {0} tersedia.`,
  'Completion.DonateAsk': `Senang bisa membantu. Kalau Anda berbaik hati, secangkir kopi sangat saya hargai.`,
  'About.Link.Guide': `Panduan dan FAQ`,
  'About.Link.ReportProblem': `Laporkan masalah`,
  'About.AutoUpdateCheck': `Periksa pembaruan secara otomatis`,
  'Automation.About.Guide.HelpText': `Membuka readme di github melalui browser Anda.`,
  'Automation.About.ReportProblem.HelpText': `Membuka pelacak masalah (Issues) di github.com melalui browser Anda.`,
  'Automation.AutoUpdateCheck.HelpText': `Jika dicentang, InstallerClean memeriksa apakah ada versi yang lebih baru di github saat Anda menjalankannya.`,
  'Tooltip.MoveSameDrive': `Memindahkan file yang tidak diperlukan ke folder cadangan. Folder itu ada di drive yang sama, jadi ruang kosongnya baru kembali setelah Anda menghapus folder itu.`,
  'Confirm.DeletePermanently.Singular': `File ini akan dihapus permanen. Ini aman dilakukan, tapi kalau Anda ingin cadangan, gunakan Pindahkan saja.`,
  'Confirm.DeletePermanently.Plural': `File-file ini akan dihapus permanen. Ini aman dilakukan, tapi kalau Anda ingin cadangan, gunakan Pindahkan saja.`,
  'Error.ScanCacheRootUnresolved': `InstallerClean tidak berhasil membuat Windows menguraikan jalur sebenarnya dari {InstallerFolder}, jadi tidak ada file yang bisa ditunjukkan berada di dalamnya dan tidak ada yang ditawarkan untuk dibersihkan. Pemindaian ini tidak menemukan apa pun karena pemeriksaan itu gagal, bukan karena foldernya bersih. Tidak ada yang disingkirkan.`,
  'Automation.Scroll.ProductDetails': `Detail produk`,
  'Body.PendingReboot.Other': `Windows Installer sedang mengerjakan sesuatu, jadi Pindahkan dan Hapus dijeda. InstallerClean tidak akan menyentuh {InstallerFolder} selagi berubah. Setelah selesai, pindai ulang dan keduanya kembali aktif.`,
  'Cli.TooManyArgumentsNoPath': `Kesalahan: argumen tambahan tak terduga '{0}'. /s dan /d tidak menerima argumen lain, dan hanya satu flag yang bisa dipakai per proses.`,
  'Cli.MissingFromDisk.Singular': `Windows punya catatan untuk {0} file yang tidak ada di {InstallerFolder}: {1}. Sehari-hari ini tidak menimbulkan masalah, tetapi pembaruan atau pencopotan program itu bisa gagal. Untuk mengembalikan file itu, Anda butuh pemasang versi yang sudah Anda miliki. Dapatkan dari pembuat programnya dan jalankan di atas salinan yang ada. Versi yang lebih baru tidak bisa: versi baru harus lebih dulu menghapus versi yang Anda miliki, dan justru langkah itulah yang membutuhkan file ini. Mencopot lebih dulu juga tidak berhasil, karena alasan yang sama. Ini semestinya memulihkan file itu dan membiarkan pengaturan Anda apa adanya, tetapi Microsoft tidak menjaminnya.`,
  'Cli.MissingFromDisk.Plural': `Windows punya catatan untuk {0} file yang tidak ada di {InstallerFolder}: {1}. Sehari-hari ini tidak menimbulkan masalah, tetapi pembaruan atau pencopotan program-program itu bisa gagal. Untuk mengembalikan sebuah file, Anda butuh pemasang versi program itu yang sudah Anda miliki. Dapatkan dari pembuat programnya dan jalankan di atas salinan yang ada. Versi yang lebih baru tidak bisa: versi baru harus lebih dulu menghapus versi yang Anda miliki, dan justru langkah itulah yang membutuhkan file itu. Mencopot lebih dulu juga tidak berhasil, karena alasan yang sama. Ini semestinya memulihkan file itu dan membiarkan pengaturan Anda apa adanya, tetapi Microsoft tidak menjaminnya.`,
  'Cli.MoveNotEnoughSpace': `Kesalahan: ruang tidak cukup di {0}. Memindahkan file-file ini perlu {1} sedangkan yang tersedia {2}. Tidak ada yang dipindahkan.`,
  'Cli.PendingRebootBlocked.Other': `Kesalahan: Windows Installer sedang mengerjakan sesuatu, jadi /m dan /d diblokir. InstallerClean tidak akan menyentuh {InstallerFolder} selagi berubah. Coba lagi setelah selesai.`,
  'Cli.FoundNoOrphans': `Tidak ditemukan file yang tidak diperlukan.`,
  'Cli.NothingOffered.Singular': `InstallerClean tidak bisa memastikan file mana dalam cache yang menjadi milik program-program yang terpasang di sini, jadi satu-satunya file ({2}) itu ditahan alih-alih ditawarkan.`,
  'Cli.NothingOffered.Plural': `InstallerClean tidak bisa memastikan file mana dalam cache yang menjadi milik program-program yang terpasang di sini, jadi seluruh {0} {1} ({2}) ditahan alih-alih ditawarkan.`,
  'Cli.DestinationChangedMidBatch': `InstallerClean tidak bisa lagi memastikan folder cadangan, jadi berhenti daripada menulis ke tempat yang salah. Periksa {0}, lalu jalankan perintahnya lagi.`,
  'Cli.Help.Summary': `Menghapus file .msi/.msp cache yang tak lagi dibutuhkan program terpasang.`,
  'Cli.Help.Elevation': `Perlu prompt administrator; Windows tidak akan menjalankannya.`,
  'Error.InstallerLockUnavailableTitle': `Tidak ada yang dihapus`,
  'Error.MoveInstallerLockUnavailableTitle': `Tidak ada yang dipindahkan`,
  'Error.InstallerLockUnavailable': `InstallerClean tidak bisa mengambil kunci yang dipakai Windows Installer untuk mencegah dua program mengubah perangkat lunak terpasang sekaligus, jadi tidak bisa memastikan sebuah file tidak menjadi diperlukan di tengah jalan, dan tidak ada yang dihapus. Coba lagi, dan mulai ulang Windows kalau terus terjadi.`,
  'Error.MoveInstallerLockUnavailable': `InstallerClean tidak bisa mengambil kunci yang dipakai Windows Installer untuk mencegah dua program mengubah perangkat lunak terpasang sekaligus, jadi tidak bisa memastikan sebuah file tidak menjadi diperlukan di tengah jalan, dan tidak ada yang dipindahkan. Coba lagi, dan mulai ulang Windows kalau terus terjadi.`,
  'Cli.InstallerLockUnavailable': `Kesalahan: InstallerClean tidak bisa mengambil kunci Windows Installer yang mencegah dua program mengubah perangkat lunak terpasang sekaligus, jadi tidak bisa memastikan sebuah file tidak menjadi diperlukan di tengah jalan. Tidak ada yang dihapus. Coba lagi, dan mulai ulang Windows kalau terus terjadi.`,
  'Cli.MoveInstallerLockUnavailable': `Kesalahan: InstallerClean tidak bisa mengambil kunci Windows Installer yang mencegah dua program mengubah perangkat lunak terpasang sekaligus, jadi tidak bisa memastikan sebuah file tidak menjadi diperlukan di tengah jalan. Tidak ada yang dipindahkan. Coba lagi, dan mulai ulang Windows kalau terus terjadi.`,
  'Completion.ReverifyIdentityClaimed': `{0} {1} dibiarkan di tempatnya, karena Windows punya catatan tentang program yang disebutkan di dalamnya.`,
  'Completion.ReverifyIdentityUnreadable': `{0} {1} dibiarkan di tempatnya, karena InstallerClean tidak menemukan nama program di dalamnya.`,
  'Error.ScanNoRegisteredFileInFolder': `InstallerClean tidak bisa mencocokkan catatan Windows Installer dengan isi {InstallerFolder}. Folder itu berisi file, tapi tidak satu pun catatan menunjuk apa pun di dalamnya, jadi tidak ada file yang bisa ditunjukkan tidak diperlukan. Tidak ada yang ditawarkan dan tidak ada yang disingkirkan.`,
  'Completion.NothingOffered': `Tidak ada yang ditawarkan di PC ini`,
  'Completion.NothingOfferedBody.Singular': `InstallerClean tidak bisa memastikan file mana dalam cache yang menjadi milik program-program yang terpasang di sini, jadi satu-satunya file ({2}) itu ditahan alih-alih ditawarkan.`,
  'Completion.NothingOfferedBody.Plural': `InstallerClean tidak bisa memastikan file mana dalam cache yang menjadi milik program-program yang terpasang di sini, jadi seluruh {0} {1} ({2}) ditahan alih-alih ditawarkan.`,
  'Summary.SupersededHeldBack.Singular': `InstallerClean tidak bisa memastikan bahwa satu-satunya file yang digantikan itu sudah tidak diperlukan, jadi file itu ditahan.`,
  'Summary.SupersededHeldBack.Plural': `InstallerClean tidak bisa memastikan bahwa {0} file yang digantikan sudah tidak diperlukan, jadi file-file itu ditahan.`,
  'Cli.SupersededHeldBack.Singular': `InstallerClean tidak bisa memastikan bahwa satu-satunya file yang digantikan itu sudah tidak diperlukan, jadi file itu ditahan.`,
  'Cli.SupersededHeldBack.Plural': `InstallerClean tidak bisa memastikan bahwa {0} file yang digantikan sudah tidak diperlukan, jadi file-file itu ditahan.`,
  'Completion.HeldBack.Singular': `{0} file ditahan. Pemindaian menyebutnya tidak diperlukan. Pemeriksaan akhir tidak bisa memastikannya.`,
  'Completion.HeldBack.Plural': `{0} file ditahan. Pemindaian menyebutnya tidak diperlukan. Pemeriksaan akhir tidak bisa memastikannya.`,
  'Body.PendingReboot.PendingRenameUnresolved': `Ada operasi file yang mengantre untuk restart berikutnya dan InstallerClean tidak bisa mengetahui file mana saja yang disebutkannya, jadi tidak bisa memastikan file-file itu tidak ada di {InstallerFolder}. Restart Windows sebelum membersihkan.`,
  'Completion.MoveRestoreHint': `Hapus folder itu setelah Anda yakin semuanya baik-baik saja.`,
  'Completion.MoveRestoreHintSameDrive': `Hapus folder itu setelah Anda yakin semuanya baik-baik saja. Ruang kosongnya baru benar-benar kembali setelah itu.`,
  'Confirm.MoveDestination.Singular': `File ini akan dipindahkan ke:`,
  'Confirm.MoveDestination.Plural': `File-file ini akan dipindahkan ke:`,
  'Cli.NothingListed.Singular': `InstallerClean tidak bisa memastikan file mana dalam cache yang menjadi milik program-program yang terpasang di sini, jadi satu-satunya file ({2}) itu ditahan alih-alih ditawarkan.`,
  'Cli.NothingListed.Plural': `InstallerClean tidak bisa memastikan file mana dalam cache yang menjadi milik program-program yang terpasang di sini, jadi {0} {1} ({2}) ditahan alih-alih ditawarkan.`,
  'Cli.WithheldReasons.Header': `Kenapa tidak bisa dipastikan:`,
  'Cli.WithheldReasons.RecordedPath': `  Sebuah jalur file dalam catatan Windows Installer sendiri tidak bisa diuraikan, jadi tidak ada yang bisa dicocokkan dengannya.`,
  'Cli.WithheldReasons.FileIdentity': `  Sebuah file yang dicatat Windows tidak bisa dikenali, jadi file itu tidak bisa dicocokkan dengan isi folder.`,
  'Cli.WithheldReasons.SecondInstance': `  Sebuah program mungkin terpasang lebih dari sekali di PC ini, dan catatan tidak bisa menyebutkan sebuah file milik salinan yang mana.`,
  'Cli.PendingRebootBlocked.PendingRenameUnresolved': `Kesalahan: ada operasi file yang mengantre untuk restart berikutnya dan InstallerClean tidak bisa mengetahui file mana saja yang disebutkannya, jadi tidak bisa mengesampingkan {InstallerFolder}. Restart Windows sebelum membersihkan.`,
  'Cli.MoveRestoreHint': `Pastikan program Anda masih bisa diperbarui dan dicopot seperti biasa, lalu hapus {0}.`,
  'Error.ScanStoppedDetails': `Ini juga dicatat di {0}.`,
  'Cli.NothingListedPerFile.Singular': `InstallerClean tidak yakin tentang salah satu file dalam cache yang ditemukannya, jadi file itu ({2}) ditahan alih-alih ditawarkan.`,
  'Cli.NothingListedPerFile.Plural': `InstallerClean tidak yakin tentang beberapa file dalam cache yang ditemukannya, jadi {0} {1} ({2}) ditahan alih-alih ditawarkan.`,
  'Cli.NothingOfferedPerFile.Singular': `InstallerClean tidak bisa membuktikan bahwa file dalam cache yang ditemukannya tidak diperlukan, jadi satu-satunya file ({2}) itu ditahan alih-alih ditawarkan.`,
  'Cli.NothingOfferedPerFile.Plural': `InstallerClean tidak bisa membuktikan bahwa ada file dalam cache yang ditemukannya yang tidak diperlukan, jadi seluruh {0} {1} ({2}) ditahan alih-alih ditawarkan.`,
  'Completion.NothingOfferedPerFileBody.Singular': `InstallerClean tidak bisa membuktikan bahwa file dalam cache yang ditemukannya tidak diperlukan, jadi satu-satunya file ({2}) itu ditahan alih-alih ditawarkan.`,
  'Completion.NothingOfferedPerFileBody.Plural': `InstallerClean tidak bisa membuktikan bahwa ada file dalam cache yang ditemukannya yang tidak diperlukan, jadi seluruh {0} {1} ({2}) ditahan alih-alih ditawarkan.`,
  'Summary.NothingListedPerFile.Singular': `InstallerClean tidak yakin tentang salah satu file dalam cache yang ditemukannya, jadi file itu ditahan alih-alih ditawarkan.`,
  'Summary.NothingListedPerFile.Plural': `InstallerClean tidak yakin tentang beberapa file dalam cache yang ditemukannya, jadi {0} {1} ditahan alih-alih ditawarkan.`,
  'Cli.WithheldReasons.CandidateIdentity': `  Sebuah file dalam folder tidak bisa dikenali, jadi file itu tidak bisa dicocokkan dengan catatan.`,
  'Cli.WithheldReasons.DeclaredProductInstalled': `  Sebuah file menyatakan bahwa ia milik program yang masih terpasang, jadi file itu mungkin masih diperlukan.`,
  'Cli.WithheldReasons.DeclaredProductUnestablished': `  Entah sebuah file tidak menyebutkan ia milik program mana, atau Windows tidak menjawab tentang program itu.`,
  'Cli.WithheldReasons.ScreenUnanswered': `  Pemeriksaan tentang file-file itu milik program mana memberi jawaban yang tidak cocok dengan file-file yang diserahkan kepadanya.`,
};

// PARSE CONTROL. About the READING and not about the content, and it exits 2,
// which is a code no ordinary run of this generator can produce: a generator is
// red by intent for the whole gap between a string landing in English and its
// translation round, so its verdict lines and its exit 0 are load-bearing in
// ci.yml and are deliberately untouched here. This says something different from
// "the translation is not done". It says the file could not be read.
//
// BOTH LEGS. raw === 0 catches a file that declares no entry at all, which the
// equality cannot see on its own because 0 === 0 holds. parsed !== raw catches
// entries the reader dropped, which one <comment> moved above its <value> does to
// any regex wanting <value> on the same whitespace run, and the Visual Studio resx
// editor writes that shape. Counted with <data\b so a tab after the tag name is
// not read as an empty file, and neither figure is written down, so a string added
// to the resx cannot make this go stale.
//
// WHY IT IS HERE WHEN THE SELF-CHECK BELOW ALREADY REDDENS. The self-check reaches
// the right verdict through what it happens to compare, not through knowing it
// read anything: with the neutral's attribute order changed, this generator wrote a
// 389-entry file and its own self-check parsed THREE entries out of it, said
// GENERATION HAS ISSUES, and was right for a reason with nothing to do with the
// truth. A tool reasoning over three entries of a 389-entry artefact should say so.
const parseControl = (where, xml, parsed) => {
  const raw = (xml.match(/<data\b/g) || []).length;
  if (raw !== 0 && parsed === raw) return;
  console.error(`PARSE CONTROL FAILED for ${where}: ${raw} '<data' occurrence(s), ${parsed} parsed.`);
  console.error('Refusing to report on a file this generator cannot show it read.');
  process.exit(2);
};

let text = readFileSync(BASE, 'utf8');
// The transform below reaches every entry through '<data name="', one space and no
// \s+, which is NOT the spelling the self-check's parse() uses further down. A
// control that exercises a pattern the reader does not use proves the file has
// structure and proves nothing about whether this reader can reach it, so the
// source is controlled in its own spelling before a single value is replaced.
parseControl(BASE, text,
  [...text.matchAll(/<data name="([^"]+)"[^>]*>\s*<value>/g)].length);

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
const parse = (xml, where) => {
  const map = new Map();
  const re = /<data\s+name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)<\/value>/g;
  let m;
  while ((m = re.exec(xml)) !== null) map.set(m[1], m[2]);
  parseControl(where, xml, map.size);
  return map;
};
const neutral = parse(readFileSync(BASE, 'utf8'), BASE);
// Derived, never pinned: the machine set grows whenever the command line
// gains an event-log string, and a literal here would fail every generator
// at once while asserting nothing about what was actually stripped.
const cliMachineExpected = [...neutral.keys()].filter(isMachineCliKey).length;
const written = readFileSync(OUT, 'utf8');
const output = parse(written, OUT);
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
