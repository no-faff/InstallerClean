#!/usr/bin/env node
// Turkish (tr) satellite generator for InstallerClean. Copied from
// gen-strings-template.mjs; only OUT and the MAP values differ. Works FROM THE
// ENGLISH SOURCE (Strings.resx): replaces each key's inner <value>, strips the
// machine-contract Cli.EventLog* keys, keeps the human Cli keys, and
// self-verifies against the neutral. Output is LF, UTF-8. See the template for
// the whole of how the body works.
//
// Turkish plural rule (DisplayHelpers.CategoryFor, case "tr"): PluralCategory
// .Other at every count, because a noun stays singular after a numeral ("5
// dosya", never "5 dosyalar"). So there are NO .One/.Few/.Many override keys,
// and each Plural.* pair carries the same bare noun on both members.
//
// MAP escaping (template literals): \\ is one backslash (the paths), \n is a real
// newline (the multi-line values), {0}/{1} are .NET placeholders left verbatim,
// and &#10; is written literally where the neutral uses the XML entity.
import { readFileSync, writeFileSync } from 'node:fs';

const dir = 'src/InstallerClean.Core/Resources';
const BASE = `${dir}/Strings.resx`;            // English source (the "neutral")
const OUT = `${dir}/Strings.tr.resx`;

// Universal keeps: keys whose value is the same in every language (brand names,
// the pure-placeholder string, the size/elapsed format strings). Their still-
// English value is NOT a miss. Explicit by KEY on purpose: a future brand/format
// key then defaults to "flag until someone adds it here", never silently passes.
// Do NOT translate these values. Do NOT edit this list per language.
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

// Per-language keeps: empty for Turkish, which translates every translatable
// token (patch -> yama), so nothing beyond KEEP_ENGLISH stays English.
const ALSO_KEEP = [];

// Satellite-only plural overrides: empty. Turkish takes PluralCategory.Other at
// every count, so the neutral's one/other pair covers every form the UI needs.
const OVERRIDES = {};

const MAP = {
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `Hakkında`,
  'Window.Registered.Title': `Silinmemesi gereken kayıtlı dosyalar`,
  'Window.Orphaned.Title': `Silinmesi güvenli, gereksiz dosyalar`,
  'Section.Registered.Products': `ÜRÜNLER`,
  'Section.Registered.Patches': `YAMALAR`,
  'Section.Registered.Details': `ÜRÜN AYRINTILARI`,
  'Section.Backup.Folder': `BACKUP FOLDER`,
  'Section.SayThanks': `TEŞEKKÜR ETMEK İÇİN`,
  'Field.Reason': `Neden`,
  'Field.Author': `Yazar`,
  'Field.Application': `Uygulama`,
  'Field.Title': `Başlık`,
  'Field.Subject': `Konu`,
  'Field.Keywords': `Anahtar sözcükler`,
  'Field.SigningCertificate': `İmzalama sertifikası`,
  'Field.FileSize': `Dosya boyutu`,
  'Field.Comment': `Açıklama`,
  'Field.ProductName': `Ürün adı`,
  'Field.File': `Dosya`,
  'Field.Size': `Boyut`,
  'Field.Patches': `Yamalar`,
  'Field.UnknownProductName': `(bilinmiyor)`,
  'Field.PatchesOnly': `(yalnızca yama)`,
  'Field.Missing': `eksik`,
  'Action.About': `_Hakkında`,
  'Action.Copy': `Kopyala`,
  'Action.Cut': `Kes`,
  'Action.Paste': `Yapıştır`,
  'Action.SelectAll': `Tümünü seç`,
  'Action.Browse': `_Göz at...`,
  'Action.Cancel': `_İptal`,
  'Action.CheckForUpdates': `Güncelleştirmeleri _denetle`,
  'Action.Close': `_Kapat`,
  'Action.DeletePermanently': `_Kalıcı olarak sil`,
  'Action.Done': `_Tamam`,
  'Action.Details': `Ayrıntılar`,
  'Action.BuyMeACuppa': `_Bana bir çay ısmarla`,
  'Action.LeaveStarOnGitHub': `GitHub'da _yıldız bırak`,
  'Action.Licence': `Apache 2.0 lisansı`,
  'Action.Move': `_Taşı`,
  'Action.BackupFolderPlaceholder': `Path to folder if you move rather than delete.`,
  'Action.OpenReleasePage': `_Sürüm sayfasını aç`,
  'Action.Rescan': `_Yeniden tara`,
  'Action.ScanAgain': `Te_krar tara`,
  'Action.SendResultLog': `Rapor gönder`,
  'Action.SendResultLogConfirm': `_Gönder`,
  'Automation.BuyMeACuppa': `Bağış yap`,
  'Automation.BuyMeACuppa.About': `Bana bir çay ısmarla`,
  'Automation.CancelOperation': `İşlemi iptal et`,
  'Automation.CancelScan': `Taramayı iptal et`,
  'Automation.CancelStartupScan': `Başlangıç taramasını iptal et`,
  'Automation.Close': `Kapat`,
  'Automation.CloseWindow': `Pencereyi kapat`,
  'Automation.CloseResult': `Sonucu kapat ve ana pencereye dön`,
  'Automation.LeaveStarOnGitHub.About': `github'da yıldız bırak`,
  'Automation.Minimise': `Simge durumuna küçült`,
  'Automation.ConfirmDelete': `Delete permanently removes the unneeded files. Cancel closes without deleting.`,
  'Automation.ConfirmMove': `Taşı, gereksiz dosyaları seçilen hedef klasöre koyar. İptal, onları olduğu yerde bırakır.`,
  'Automation.SayThanks': `Teşekkür etmek için`,
  'Automation.ConfirmSendResultLog': `Gönder, gösterilen raporu No Faff'a iletir. İptal hiçbir şey göndermez.`,
  'Automation.CheckForUpdates': `Güncelleştirmeleri denetle`,
  'Automation.CheckForUpdates.HelpText': `github üzerindeki sürümler sayfasında daha yeni bir sürüm olup olmadığını denetler.`,
  'Automation.UpdateAvailable.HelpText': `Daha yeni sürümü indirmek için sürüm sayfasını açın ya da geçerli sürümü korumak için iptal edin.`,
  'Automation.Licence.HelpText': `github.com üzerindeki lisans dosyasını tarayıcınızda açar.`,
  'Automation.Section.BackupFolder': `Backup folder`,
  'Automation.Section.Products': `Ürünler`,
  'Automation.Section.Patches': `Yamalar`,
  'Automation.Section.ProductDetails': `Ürün ayrıntıları`,
  'Automation.BackupFolder': `Backup folder`,
  'Automation.OperationProgress': `İşlem ilerlemesi`,
  'Automation.RescanInstaller': `{InstallerFolder}'ı yeniden tara`,
  'Automation.ScanningProgress': `Tarama ilerlemesi`,
  'Automation.StartupScanProgress': `Başlangıç taraması ilerlemesi`,
  'Automation.ViewOrphanedFiles': `Ayrıntılar, gereksiz dosyalar`,
  'Automation.ViewOrphanedFiles.HelpText': `Temizlik için uygun.`,
  'Automation.ViewRegisteredFiles': `Ayrıntılar, kayıtlı dosyalar`,
  'Automation.ViewRegisteredFiles.HelpText': `Salt okunur envanter.`,
  'Automation.SortStatus.Ascending': `{0} ölçütüne göre artan sırada sıralandı`,
  'Automation.SortStatus.Descending': `{0} ölçütüne göre azalan sırada sıralandı`,
  'Automation.Scroll.ScanResults': `Tarama sonuçları`,
  'Automation.Scroll.ResultDetails': `Sonuç ayrıntıları`,
  'Automation.Scroll.FileDetails': `Dosya ayrıntıları`,
  'Automation.Scroll.DialogBody': `İletişim kutusu metni`,
  'Automation.ScanResultAnnouncement': `{0} ({1})`,
  'Automation.CompletionErrors': `İşlenemeyen dosyalar`,
  'Automation.RegisteredMissingSeeAlso': `Bu klasörü açıklar ve bir dosyanın nasıl kurtarılacağını README'de anlatır`,
  'Tooltip.BuyMeACuppa.About': `Susatan bir iş!`,
  'Tooltip.CancellingPending': `İptal istendi. InstallerClean, geçerli adımın durabileceği bir noktaya gelmesini bekliyor. Yoğun G/Ç sırasında ya da bir MSI veritabanı çağrısında bu birkaç saniye sürebilir.`,
  'Tooltip.Close': `Kapat`,
  'Tooltip.LeaveStarOnGitHub.About': `Bir yıldız, başkalarının InstallerClean'i bulmasına yardımcı olur.`,
  'Tooltip.Minimise': `Simge durumuna küçült`,
  'Tooltip.SendResultLog': `Size kalmış ama makbule geçer. Yalnızca uygulamanın çalışıp çalışmadığını ve insanların ne kadar yer açtığını bana bildiren anonim bir özet gönderir. Sonraki ekran, onaylamadan önce ne gönderileceğini görmenizi sağlar.`,
  'Tooltip.SendResultLog.NothingFound': `Size kalmış ama makbule geçer. Yalnızca uygulamanın çalışıp çalışmadığını bana bildiren anonim bir özet gönderir. Sonraki ekran, onaylamadan önce ne gönderileceğini görmenizi sağlar.`,
  'Tooltip.Move': `Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them.`,
  'Tooltip.MoveNeedsDestination': `Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them.`,
  'Tooltip.Delete': `Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away.`,
  'Tooltip.SigningCertificate': `Gömülü Authenticode sertifikasındaki konu adı. Zincir doğrulaması yapılmadı.`,
  'Body.MainExplanation.Lead': `Any unneeded files below are [safe to delete].`,
  'Body.MainExplanation.Why': `Bu dosyalar {InstallerFolder} içinde yer alır; bir program kaldırıldığında ({0}), daha yeni bir yama bir öncekinin yerini aldığında ({1}) ya da yayımcı onu geri çektiğinde ({2}) geride kalır. InstallerClean her zaman yalnızca Windows'un kendisinin işi bittiğini bildirdiği dosyaları listeler.`,
  'Body.MainExplanation.Action': `Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored.`,
  'Body.PendingReboot.MsiExecuteMutex': `Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back.`,
  'Body.PendingReboot.InstallerInProgress': `A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}.`,
  'Body.PendingReboot.PendingRenameInCache': `Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning.`,
  'Body.NoFileSelected': `Ayrıntıları görmek için bir dosya seçin.`,
  'Body.NoProductSelected': `Ayrıntıları görmek için bir ürün seçin.`,
  'Body.NoMetadata': `Kullanılabilir meta veri yok.`,
  'Body.RegisteredMissingFromDisk': `This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.&#10;&#10;It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.&#10;&#10;To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself.`,
  'Body.RegisteredMissingFromDisk.SeeAlso': `README [bu klasörü açıklar] ve bir dosyanın nasıl kurtarılacağını Microsoft'un kendi sözleriyle anlatır.`,
  'Body.NoPatches': `(yok)`,
  'Reason.Orphaned': `Sahipsiz`,
  'Reason.Superseded': `Yerine geçilmiş`,
  'Reason.Obsoleted': `Geçersiz kılınmış`,
  'Status.Scanning': `Taranıyor...`,
  'Status.Cancelling': `İptal ediliyor...`,
  'Status.StartingScan': `Tarama başlatılıyor...`,
  'Status.QueryingApi': `Yüklü yazılımlar için Windows sorgulanıyor...`,
  'Status.ScanningCache': `Yükleyici önbellek klasörü taranıyor...`,
  'Status.EnumeratingProducts': `Yüklü ürünler listeleniyor...`,
  'Status.CheckingRegistry': `Ek paketler için kayıt defteri denetleniyor...`,
  'Status.RegisteredPackagesFound': `{0} kayıtlı {1} bulundu.`,
  'Status.ScanComplete': `Tarama tamamlandı ({0})`,
  'Status.FoundProducts': `Yerel paketler taranıyor...`,
  'Status.FoundUnused': `Güvenle silebileceğiniz {0} {1} bulundu.`,
  'Status.PreparingDestination': `Hedef klasör hazırlanıyor...`,
  'Status.Moving': `Moving unneeded files...`,
  'Status.Deleting': `Deleting unneeded files...`,
  'Status.MoveCancelled.Partial': `Taşıma iptal edildi. {1} {2} içinden {0} tanesi işlendi.`,
  'Status.DeleteCancelled.Partial': `Silme iptal edildi. {1} {2} içinden {0} tanesi işlendi.`,
  'Status.MoveFailed': `Taşıma başarısız oldu ({0}). Ayrıntılar {1} içinde.`,
  'Status.MoveFailed.NoLog': `Taşıma başarısız oldu ({0}). Çökme günlüğü yazılamadı.`,
  'Status.DeleteFailed': `Silme başarısız oldu ({0}). Ayrıntılar {1} içinde.`,
  'Status.DeleteFailed.NoLog': `Silme başarısız oldu ({0}). Çökme günlüğü yazılamadı.`,
  'Status.ScanAccessDenied': `Erişim reddedildi. Windows taramayı reddetti.`,
  'Status.ScanFailedDb': `Tarama başarısız: Windows Installer kayıtları okunamadı.`,
  'Status.ScanCancelled': `Tarama iptal edildi.`,
  'Status.Done': `Hazır`,
  'Status.ScanFailedDetails': `Tarama başarısız oldu ({0}). Ayrıntılar {1} içinde.`,
  'Status.ScanFailedDetails.NoLog': `Tarama başarısız oldu ({0}). Çökme günlüğü yazılamadı.`,
  'Completion.AllClean': `Her şey temiz`,
  'Completion.NothingToCleanUp': `{InstallerFolder} içinde temizlenecek bir şey yok`,
  'Completion.NothingToCleanUpReceipt': `{2} içinde {0} {1} tarandı`,
  'Completion.Freed': `{0} yer açıldı`,
  'Completion.Moved': `{0} taşındı`,
  // Heading for the outcome where the operation acted on no file at all, then
  // the failure count line beneath it: 0 = files that failed, 1 = files tried
  // (succeeded + failed). The count selecting the form is {0}; the noun belongs
  // to {1}, so treat the two independently.
  'Completion.NothingMoved': `Hiçbir dosya taşınmadı`,
  'Completion.NothingDeleted': `Hiçbir dosya silinmedi`,
  'Completion.FailedCount.Singular': `{1} dosya içinden {0} tanesi taşınamadı.`,
  'Completion.FailedCount.Plural': `{1} dosya içinden {0} tanesi taşınamadı.`,
  'Completion.FailedCountDelete.Singular': `{1} dosya içinden {0} tanesi silinemedi.`,
  'Completion.FailedCountDelete.Plural': `{1} dosya içinden {0} tanesi silinemedi.`,
  'Completion.MoveSummary.Singular': `{0} {1} şu konuma taşındı: {2}`,
  'Completion.MoveSummary.Plural': `{0} {1} şu konuma taşındı: {2}`,
  'Completion.PermanentDeleteSummary.Singular': `{0} {1} permanently deleted`,
  'Completion.PermanentDeleteSummary.Plural': `{0} {1} permanently deleted`,
  'Summary.RegisteredStillUsed.Singular': `{0} dosya hâlâ gerekli`,
  'Summary.RegisteredStillUsed.Plural': `{0} dosya hâlâ gerekli`,
  'Summary.OrphanedToCleanUp.Singular': `temizlenecek {0} gereksiz dosya`,
  'Summary.OrphanedToCleanUp.Plural': `temizlenecek {0} gereksiz dosya`,
  'Summary.MissingFromDisk.Singular': `{0} kayıtlı dosya eksik (InstallerClean tarafından silinmedi). Şu anda bir sorun yok, ama ileride ilgili programı onarma, güncelleştirme ya da kaldırma işlemi başarısız olabilir. Ne yapılacağını öğrenmek için Ayrıntılar'ı açın.`,
  'Summary.MissingFromDisk.Plural': `{0} kayıtlı dosya eksik (InstallerClean tarafından silinmedi). Şu anda bir sorun yok, ama ileride ilgili programları onarma, güncelleştirme ya da kaldırma işlemi başarısız olabilir. Ne yapılacağını öğrenmek için Ayrıntılar'ı açın.`,
  'Summary.OperationFiles': `{1} {2} içinden {0}`,
  'Summary.OrphanedWindow': `{0} sahipsiz, {1} yerine geçilmiş, {2} geçersiz kılınmış ({3})`,
  'Summary.RegisteredWindow.Singular': `{0} kayıtlı dosya hâlâ gerekli ({1})`,
  'Summary.RegisteredWindow.Plural': `{0} kayıtlı dosya hâlâ gerekli ({1})`,
  'Confirm.MoveTitle': `{0} {1} ({2}) taşınsın mı?`,
  'Confirm.MoveDestination': `Dosyalar şu konuma taşınacak:`,
  'Confirm.DeleteTitle': `{0} {1} ({2}) silinsin mi?`,
  'Error.AdminRequiredTitle': `Erişim reddedildi`,
  'Error.AdminRequiredBody': `Windows, InstallerClean'in erişimini reddetti, bu yüzden işlem durduruldu. Hiçbir şey kaldırılmadı.\n\nInstallerClean zaten yönetici olarak çalışıyordu, dolayısıyla onu yeniden öyle başlatmak işe yaramaz. Windows erişimi neyin reddettiği konusunda başka bir şey söylemiyor, bu yüzden denenecek belirli bir şey yok.`,
  'Error.InstallerDbUnavailableTitle': `Windows Installer kayıtları okunamadı`,
  'Error.ScanFailedTitle': `Tarama başarısız`,
  'Error.InstallerDbEmpty': `Windows Installer kayıtları tamamen boş döndü: tek bir yüklü program ya da güncelleştirme bile önbellekteki bir kurulum dosyasında hak iddia etmiyor. Çalışan bir makinede bu olmaz (yeni kurulmuş bir Windows'ta bile bunlardan vardır), yani kayıtlar ya bozuk ya da okunamadı, ve bu yanıta inanan bir tarama {InstallerFolder} içindeki her dosyayı yanlışlıkla sahipsiz sayardı. InstallerClean bunun yerine durdu. Hiçbir şey kaldırılmadı.`,
  'Error.MsiAccessDenied': `Windows Installer, InstallerClean'in yüklü olanları listelemesine izin vermedi. InstallerClean zaten yönetici olarak çalışıyordu, dolayısıyla onu yeniden yönetici olarak çalıştırmak bir şey değiştirmez. Bu liste olmadan önbellekteki hangi dosyaların hâlâ gerekli olduğunu güvenle söylemenin yolu yok, bu yüzden InstallerClean durdu. Hiçbir şey kaldırılmadı.`,
  'Error.MsiNonSuccess': `Windows Installer, InstallerClean'e yüklü programların okunabilir bir listesini veremedi: arka arkaya {0} kayıt okunamaz döndü (son hata kodu {1}). InstallerClean, yalnızca kısmen okunmuş bir listeyle çalışmak yerine durdu. Hiçbir şey kaldırılmadı.`,
  'Error.InvalidDestinationTitle': `Geçersiz hedef`,
  'Error.DestinationWriteFailedTitle': `Hedefe yazılamadı`,
  'Error.MoveFailedTitle': `Taşıma başarısız`,
  'Error.DeleteFailedTitle': `Silme başarısız`,
  'Error.SettingNotSavedTitle': `Ayar kaydedilmedi`,
  'Error.SettingNotSavedBody': `Değişiklik kaydedilemedi. Bir sonraki açılışta InstallerClean önceki ayara dönecek.`,
  'Error.DestinationInsideInstaller': `Hedef, Windows Installer klasörünün içinde olamaz.`,
  'Error.DestinationInSystemFolder': `The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%.`,
  'Error.NotEnoughSpaceTitle': `Yetersiz alan`,
  'Error.NotEnoughSpaceBody': `{0} konumunda yeterli alan yok

Gerekli: {1}
Kullanılabilir: {2}`,
  'Error.AccessDeniedDestination': `{0} konumuna yazma izniniz yok.
Kullanıcı profilinizdeki ya da sahibi olduğunuz bir sürücüdeki bir klasörü deneyin.`,
  'Error.PathTooLong': `{0} yolu Windows için çok uzun. Daha kısa bir yol seçin.`,
  'Error.DestinationMissing': `{0} klasörü yok ve oluşturulamadı. Sürücü harfini ya da ağ yolunu kontrol edin.`,
  'Error.IOWriteDestination': `Windows {0} konumuna yazamıyor.
Ayrıntılar {1} içinde.`,
  'Error.IOWriteDestination.NoLog': `Windows {0} konumuna yazamıyor. Çökme günlüğü yazılamadı.`,
  'Error.WriteDestination': `{0} konumuna yazılamıyor.
Ayrıntılar {1} içinde.`,
  'Error.WriteDestination.NoLog': `{0} konumuna yazılamıyor. Çökme günlüğü yazılamadı.`,
  'Error.MissingSourceFile': `Dosya artık yok.`,
  'Error.SourceIsReparsePoint': `Kaynak dosya bir sembolik bağlantı ya da bağlantı noktası (junction); güvenlik için reddedildi.`,
  // Singular = the sentence for ONE file, which the CLI prints after "filename: ";
  // plural = the heading the completion overlay puts over a list of filenames.
  'Error.AccessDenied.Singular': `Windows bu dosyaya erişimi reddetti; dosya yerinde bırakıldı.`,
  'Error.AccessDenied.Plural': `Windows bu dosyalara erişimi reddetti; dosyalar yerinde bırakıldı.`,
  'Error.FileInUse.Singular': `This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later.`,
  'Error.FileInUse.Plural': `These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later.`,
  'Error.IOFailure.Singular': `Windows bir dosya hatası bildirdi; dosya yerinde bırakıldı.`,
  'Error.IOFailure.Plural': `Windows dosya hataları bildirdi; bu dosyalar yerinde bırakıldı.`,
  'Error.UnknownError.Singular': `Bu dosyada bir şeyler ters gitti; dosya yerinde bırakıldı.`,
  'Error.UnknownError.Plural': `Bu dosyalarda bir şeyler ters gitti; dosyalar yerinde bırakıldı.`,
  'Error.MoveIntoInstaller': `Dosyaların Windows Installer klasörüne taşınması reddediliyor (hedef: {0}).`,
  'Error.DestinationNotFullyQualified': `The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\\Backup, or \\\\server\\backup). InstallerClean can't use this one: {0}`,
  'BrowserLaunch.FailedTitle': `Tarayıcınız açılamadı`,
  'UpdateCheck.Title': `Güncelleştirmeleri denetle`,
  'UpdateCheck.Status.Checking': `Denetleniyor...`,
  'UpdateCheck.Status.UpToDate': `Güncel.`,
  'UpdateCheck.UpdateAvailable.Title': `Güncelleştirme mevcut`,
  'UpdateCheck.UpdateAvailable.Body': `{0} sürümünü çalıştırıyorsunuz.&#10;{1} sürümü mevcut.`,
  'UpdateCheck.Failed.NetworkUnavailable': `GitHub'a ulaşılamadı. İnternet bağlantınızı kontrol edip yeniden deneyin.`,
  'UpdateCheck.Failed.ServerError': `GitHub bir hata yanıtı döndürdü. Birkaç dakika sonra yeniden deneyin.`,
  'UpdateCheck.Failed.ResponseParseError': `GitHub'ın yanıtı tanınan bir sürüm içermiyordu. Daha sonra yeniden deneyin ya da sürümler sayfasını doğrudan açın.`,
  'UpdateCheck.Failed.Timeout': `Denetim zaman aşımına uğradı. GitHub bağlantınız yavaş olabilir; yeniden deneyin.`,
  'UpdateCheck.Failed.Unknown': `Denetim bilinmeyen bir nedenle başarısız oldu. Bildirmeniz gerekirse ayrıntılar crash.log içindedir.`,
  'BrowserLaunch.ClipboardOk': `InstallerClean tarayıcınızı açamadı. Bağlantı panonuzda, böylece onu kendiniz yapıştırabilirsiniz:&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean tarayıcınızı açamadı ve bağlantıyı panonuza da kopyalayamadı. Bağlantı şu:&#10;&#10;{0}`,
  'Error.DestinationChangedMidBatch': `InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then Re-scan and try again.`,
  'Error.CannotWriteFolder': `{0} konumuna yazılamıyor.`,
  'Error.NoUniqueFilename': `'{0}' için 10.000 denemeden sonra benzersiz bir dosya adı bulunamadı.`,
  'ResultLog.Sending': `Gönderiliyor...`,
  'ResultLog.Sent': `Teşekkürler! Rapor gönderildi.`,
  'ResultLog.Failed': `Gönderme başarısız oldu. Daha sonra yeniden deneyin.`,
  'ResultLog.NothingToSend': `Gönderilecek rapor yok.`,
  'ConfirmSendResultLog.Title': `Bunu göndermek ister misiniz?`,
  'ConfirmSendResultLog.Reassurance': `nofaff.netlify.app/api/result-log adresine gönderilir. Hiçbir şey sizi ya da makinenizi tanımlamaz; yalnızca InstallerClean'in çalıştığını ve [insanların ne kadar yer açtığını] bana bildirir.`,
  'Automation.ResultLogPreview': `Rapor önizlemesi`,
  'Startup.AlreadyRunningTitle': `InstallerClean`,
  'Startup.AlreadyRunningBody': `InstallerClean zaten çalışıyor.`,
  'Startup.UnhandledTitle': `InstallerClean`,
  'Startup.UnhandledBody': `Beklenmeyen bir hata oluştu ve InstallerClean kapanmak zorunda.

{0}

Ayrıntılar şuraya yazıldı:
{1}`,
  'Startup.UnhandledBody.NoLog': `Beklenmeyen bir hata oluştu ve InstallerClean kapanmak zorunda.

{0}

Çökme günlüğü yazılamadı.`,
  'Startup.ErrorTitle': `Başlangıç hatası`,
  'Startup.FailedToStart': `Başlatılamadı ({0}). Ayrıntılar şuraya yazıldı:
{1}`,
  'Startup.FailedToStart.NoLog': `Başlatılamadı ({0}). Çökme günlüğü yazılamadı.`,
  'FilePicker.ChooseDestinationTitle': `Taşınan dosyalar için hedef klasörü seçin`,
  'Version.Display': `Sürüm {0}`,
  'Plural.File.Singular': `dosya`,
  'Plural.File.Plural': `dosya`,
  'Plural.Error.Singular': `hata`,
  'Plural.Error.Plural': `hata`,
  'Plural.Package.Singular': `paket`,
  'Plural.Package.Plural': `paket`,
  'Plural.Product.Singular': `ürün`,
  'Plural.Product.Plural': `ürün`,
  'Plural.Patch.Singular': `yama`,
  'Plural.Patch.Plural': `yama`,
  'Display.Size.GB': `{0:F2} GB`,
  'Display.Size.MB': `{0:F1} MB`,
  'Display.Size.KB': `{0:F1} KB`,
  'Display.Size.B': `{0} B`,
  'Display.Elapsed.Ms': `{0:F0}ms`,
  'Display.Elapsed.S': `{0:F1}s`,
  'Display.ElapsedLong.LessThanASecond': `bir saniyeden az`,
  'Display.ElapsedLong.Seconds': `{0:F1} saniye`,
  'CrashLog.PrivacyHeader': `# crash.log captures unhandled exceptions from InstallerClean.\n# Under elevation the framework's exception messages can include\n# file paths from the running session (including other users'\n# profiles enumerated by Windows Installer queries). Network-\n# failure messages from the update check or result-log POST can\n# include the destination URL and the resolved IP / proxy address.\n# Entries about unreadable Windows Installer records can include a\n# Windows account SID (S-1-5-21-...) and the product codes of\n# installed software.\n# Redact all three classes of detail before attaching this file to\n# a public bug report.\n`,
  'Tooltip.ChangeLanguage': `Dili değiştir. Program yeniden başlatılacak.`,
  'Automation.ChangeLanguage': `Dili değiştir`,
  'Automation.ChangeLanguage.HelpText': `Program yeniden başlatılacak.`,
  'Cli.UnknownArgument': `Error: unknown argument '{0}'`,
  'Cli.Cancelling': `İptal ediliyor...`,
  'Cli.Cancelled': `İptal edildi.`,
  'Cli.GenericError': `Error: unexpected failure ({0}). Details written to {1}.`,
  'Cli.GenericError.NoLog': `Error: unexpected failure ({0}). The crash log could not be written.`,
  'Cli.ScanningInstaller': `{InstallerFolder} taranıyor...`,
  'Cli.FoundOrphans': `Found {0} unneeded {1} to clean up ({2}).`,
  'Cli.DeletingFiles': `Deleting {0} unneeded {1}...`,
  'Cli.DeletedFiles': `Permanently deleted {0} unneeded {1}.`,
  'Cli.NoMoveDestination': `Hata: taşıma hedefi belirtilmedi. /m YOL kullanın. (GUI'de ayarlanan bir varsayılan, kullanıcıya özeldir ve zamanlanmış ya da hizmet hesabı çalıştırmaları için geçerli değildir.)`,
  'Cli.MoveDestinationInsideInstaller': `Hata: hedef, Windows Installer klasörünün içinde olamaz.`,
  'Cli.MoveDestinationRelative': `Hata: hedef, tam nitelenmiş bir yol olmalıdır. Alınan: {0}`,
  'Cli.MoveDestinationInSystemFolder': `Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}.`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning.`,
  'Cli.MovingFiles': `Moving {0} unneeded {1} to {2}...`,
  'Cli.MovedFiles': `Moved {0} unneeded {1}.`,
  'Cli.MutexBlocked': `Başka bir InstallerClean işlemi tek örnek kilidini tutuyor (GUI ya da başka bir CLI çalıştırması). Çıkış 75 (geçici); daha sonra yeniden denemek güvenli.`,
  'Cli.EventLogUnavailable': `Not: Olay Günlüğü'ne yazma başarısız oldu. Uygulama günlüğü izinlerini ya da Grup İlkesi'ni kontrol edin.`,
  'Cli.Help.Header': `InstallerClean - {InstallerFolder} temizliği`,
  'Cli.Help.Usage': `Kullanım:`,
  'Cli.Help.Help': `  installerclean-cli --help     Bu yardımı göster (/?, -h de kabul edilir)`,
  'Cli.Help.Version': `  installerclean-cli --version  Sürümü yazdır (-v de kabul edilir)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s         Scan only - list unneeded files`,
  'Cli.Help.Delete': `  installerclean-cli /d         Delete unneeded files permanently`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m         Move to the saved backup folder`,
  'Cli.Help.MovePath': `  installerclean-cli /m YOL     Belirtilen yola taşı`,
  'Cli.Help.NoteLine1': `installerclean-cli blocks the prompt until it finishes, so a script or&#10;scheduled task can wait on it.`,
  'Cli.Help.ExitCodesHeader': `Çıkış kodları:`,
  'Cli.Help.ExitCodeOk': `  0   success: the run finished with nothing left to do`,
  'Cli.Help.ExitCodeError': `  1   failure: nothing processed (bad arguments, a bad destination, a&#10;       failed scan or every file failed)`,
  'Cli.Help.ExitCodePartial': `  2   partial: some processed, some not (a failure or a Ctrl+C part way)`,
  'Cli.Help.ExitCodeTransient': `  75  geçici: geçici bir durum çalıştırmayı engelledi (iletiye bakın)`,
  'Cli.Help.ExitCodeCancelled': `  130 iptal edildi (Ctrl+C)`,
  'Body.NotScanned.Lead': `Henüz tarama yapılmadı.`,
  'Body.NotScanned.Why': `Hiçbir programın hâlâ ihtiyaç duymadığı yükleyici dosyaları için {InstallerFolder} klasörüne bakmak üzere Yeniden tara'ya basın.`,
  'Confirm.MoveSameDrive': `That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away.`,
  'Error.ScanCorrelationFailed': `InstallerClean couldn't square this scan with the Windows Installer records: nearly every file Windows still lists as needed is missing from {InstallerFolder}, while what is actually in the folder matches almost nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed.`,
  'Error.CandidateOutsideCache': `Bu dosya doğrudan Windows Installer klasörünün içinde değil; güvenlik için reddedildi.`,
  'Completion.ReverifySkipped': `{0} {1} kept in place, because the records now claim what the scan flagged.`,
  'Completion.MoveCancelledSummary': `İptal etmeden önce {1} {2} içinden {0} tanesi taşındı.`,
  'Completion.PermanentDeleteCancelledSummary': `İptal etmeden önce {1} {2} içinden {0} tanesi kalıcı olarak silindi.`,
  'Body.PendingReboot.Lead': `Bu dosyalar şu anda temizlenemez.`,
  'Cli.TooManyArguments': `Hata: beklenmeyen fazladan argüman '{0}'. Taşıma klasörünüzün adında boşluk varsa tüm yolu tırnak içine alın: /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `That folder is saved per-user; scheduled or SYSTEM runs need /m PATH.`,
  'Completion.ReverifyIncomplete': `{0} {1} kept in place, because the Windows Installer records could not be fully read in the final check.`,
  'Error.ScanRecordsUnreadable': `InstallerClean, neyin hâlâ gerekli olduğundan emin olmaya yetecek kadar Windows Installer kaydını okuyamadı: yüklü programların listesi eksik döndü, aynı kayıtları doğrudan kayıt defterinden okumak da hatalarla karşılaştı. Bir dosya, yalnızca onu adlandıran kayıt okunamayanlardan biri olduğu için sahipsiz görünebilirdi, bu yüzden InstallerClean durdu. Hiçbir şey kaldırılmadı.`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer, yüklü programlar listesinin sonunu hiç bildirmedi: InstallerClean {0} kayıttan sonra vazgeçti (son hata kodu {1}). Sonu gelmeyen bir listeye güvenilemez, bu yüzden InstallerClean durdu. Hiçbir şey kaldırılmadı.`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer, bir programın yama listesinin sonunu hiç bildirmedi: InstallerClean {0} kayıttan sonra vazgeçti (son hata kodu {1}). Sonu gelmeyen bir listeye güvenilemez, bu yüzden InstallerClean durdu. Hiçbir şey kaldırılmadı.`,
  'UpdateCheck.Status.UpdateAvailable': `{0} sürümü mevcut.`,
  'Completion.DonateAsk': `Yardımcı olabildiğime sevindim. Gönlünüzden koparsa, bir bahşiş kutusu var.`,
  'About.Link.Guide': `Kılavuz ve SSS`,
  'About.Link.ReportProblem': `Sorun bildir`,
  'About.AutoUpdateCheck': `Güncelleştirmeleri otomatik olarak denetle`,
  'Automation.About.Guide.HelpText': `github üzerindeki readme'yi tarayıcınızda açar.`,
  'Automation.About.ReportProblem.HelpText': `github.com üzerindeki sorun izleyiciyi (Issues) tarayıcınızda açar.`,
  'Automation.AutoUpdateCheck.HelpText': `İşaretliyse InstallerClean, çalıştırdığınızda github üzerinde daha yeni bir sürüm olup olmadığını denetler.`,
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
  'Error.InstallerLockUnavailableTitle': `Hiçbir dosya silinmedi`,
  'Error.InstallerLockUnavailable': `InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening.`,
  'Cli.InstallerLockUnavailable': `Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening.`,
  'Completion.ReverifyRecordsChanged': `{0} {1} kept in place, because the Windows Installer records had changed by the final check.`,
  'Summary.RecordsNotMatched': `InstallerClean couldn't match up everything in the Windows records, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again.`,
  'Cli.RecordsNotMatched': `InstallerClean couldn't match up everything in the Windows records, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up.`,
};

let text = readFileSync(BASE, 'utf8');

// Remove ONLY the machine-contract Cli.* <data> elements BY NAME (the
// Cli.EventLog* set bar Cli.EventLogUnavailable): each is matched non-greedy to
// its own </data>. The human-facing Cli keys are KEPT, and their value is
// replaced from the MAP like any other key. Same predicate as
// scripts/check-resx-parity.mjs. The section comments left orphaned by a removed
// machine key (<!-- CLI output -->, the per-machine-key placeholder notes) are
// left in place deliberately: removing them needs fragile anchors that name
// specific keys, the exact step that broke before. They are harmless XML
// comments. Do NOT reintroduce comment surgery to "tidy" them.
const isMachineCliKey = (k) =>
  k.startsWith('Cli.') && k.includes('EventLog') && k !== 'Cli.EventLogUnavailable';
let cliMachineRemoved = 0;
text = text.replace(/[^\S\n]*<data name="(Cli\.[^"]*)"[\s\S]*?<\/data>\n?/g,
  (m, name) => { if (isMachineCliKey(name)) { cliMachineRemoved++; return ''; } return m; });

// Replace each key's inner <value> from MAP. The capture keeps the <data> tag,
// its attributes and the whitespace before <value>; any <comment> child and the
// </data> close sit outside the match. The closing quote anchors the name, so
// Status.MoveFailed never matches Status.MoveFailed.NoLog. A function replacement
// keeps $-sequences in a value from being read as backreferences.
const escRe = (s) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
const notApplied = [];
for (const [key, val] of Object.entries(MAP)) {
  const re = new RegExp('(<data name="' + escRe(key) + '"[^>]*>\\s*<value>)([\\s\\S]*?)(</value>)');
  let applied = false;
  text = text.replace(re, (m, p1, p2, p3) => { applied = true; return p1 + val + p3; });
  if (!applied) notApplied.push(key);
}

// Append the satellite-only override <data> elements before </root>. Values carry
// no XML-special characters (same as the MAP). Empty OVERRIDES means no block, so
// the output is byte-identical to a no-override language (e.g. Korean).
const overrideBlock = Object.entries(OVERRIDES)
  .map(([k, v]) => `  <data name="${k}" xml:space="preserve"><value>${v}</value></data>`)
  .join('\n');
if (overrideBlock) text = text.replace('</root>', overrideBlock + '\n</root>');

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
// Required = everything a satellite must carry: the non-Cli keys plus the
// human-facing Cli keys. The machine Cli keys are the complement; they must be
// absent from the output (isMachineCliKey is defined up in the strip section).
const neutralRequired = [...neutral.keys()].filter((k) => !isMachineCliKey(k));

// Satellite-only overrides: present, and each sharing its base key's {N} set
// (base = the <Prefix>.Plural sibling if the neutral has one, else the flat key).
const overrideKeys = Object.keys(OVERRIDES);
const overrideMissing = overrideKeys.filter((k) => !output.has(k));
const overrideArityMismatch = overrideKeys.filter((k) => {
  if (!output.has(k)) return true;
  const base = k.replace(/\.(?:One|Few|Many)$/, '');
  const ref = neutral.has(`${base}.Plural`) ? `${base}.Plural` : base;
  if (!neutral.has(ref)) return true; // base must exist in the neutral
  const a = placeholders(neutral.get(ref)), b = placeholders(output.get(k));
  return a.size !== b.size || [...a].some((i) => !b.has(i));
});

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
  if (!output.has(k)) return false; // already counted by missingFromOutput
  const a = placeholders(neutral.get(k)), b = placeholders(output.get(k));
  return a.size !== b.size || [...a].some((i) => !b.has(i));
});
const crlf = (written.match(/\r/g) || []).length;

// Untranslated-phrase gate (KEY-based, HARD): a value still byte-identical to the
// English neutral is a miss, UNLESS its key is a universal keep or in ALSO_KEEP.
const alsoKeep = new Set(ALSO_KEEP);
const untranslated = neutralRequired.filter((k) =>
  output.has(k) && output.get(k) === neutral.get(k) && !KEEP_ENGLISH.has(k) && !alsoKeep.has(k));

// Breakdown computed, never pinned: the non-Cli and human-Cli totals both grow with
// every string the app gains, and a hardcoded pair goes stale silently while the
// checked figure beside it stays right.
const nonCliRequired = neutralRequired.filter((k) => !k.startsWith('Cli.')).length;
console.log('translatable <data> in output:', output.size,
  '(expect', neutralRequired.length + overrideKeys.length,
  '=', nonCliRequired, 'non-Cli +', neutralRequired.length - nonCliRequired, 'Cli +',
  overrideKeys.length, 'override)');
console.log('machine Cli <data> removed:', cliMachineRemoved, `(expect ${cliMachineExpected})`);
console.log('MAP entries:', Object.keys(MAP).length, '| override keys:', overrideKeys.length, '| CRLF:', crlf, '(expect 0)');

// ALSO_KEEP audit roster, so a lazy "force it green" dump is visible at a glance.
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
if (overrideMissing.length) console.log('!! override key missing from output:', overrideMissing);
if (overrideArityMismatch.length) console.log('!! override arity differs from its base key:', overrideArityMismatch);
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
  !overrideMissing.length && !overrideArityMismatch.length &&
  output.size === neutralRequired.length + overrideKeys.length && cliMachineRemoved === cliMachineExpected && crlf === 0;
const ok = structuralOk && !untranslated.length;
console.log(ok ? '\nGENERATION OK' : '\nGENERATION HAS ISSUES (see above)');
