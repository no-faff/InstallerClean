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

// Per-language keeps: empty for Turkish, which translates every translatable
// token (patch -> yama), so nothing beyond KEEP_ENGLISH stays English.
const ALSO_KEEP = [
  // The list separator Turkish uses is the one English uses. A punctuation
  // mark rather than a word, so there is nothing to translate and nothing to
  // get wrong; only ja and zh-Hans differ, taking the ideographic comma.
  'Display.ListSeparator',       // ", "
  // The size and elapsed unit suffixes. Turkish abbreviates them exactly as
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

// Satellite-only plural overrides: empty. Turkish takes PluralCategory.Other at
// every count, so the neutral's one/other pair covers every form the UI needs.
const OVERRIDES = {};

const MAP = {
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `Hakkında`,
  'Window.Registered.Title': `Olduğu gibi bırakılan dosyalar`,
  'Window.Orphaned.Title': `Silinmesi güvenli, gereksiz dosyalar`,
  // Section.Registered.Products and Automation.Section.Products are deliberately
  // not here. They left the neutral resx when the registered-files window stopped
  // having a products group of its own, so a value for either would be
  // regenerated into a key the app cannot use.
  'Section.Registered.Patches': `YAMALAR`,
  'Section.Registered.Details': `ÜRÜN AYRINTILARI`,
  'Section.Backup.Folder': `YEDEK KLASÖRÜ`,
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
  'Action.BackupFolderPlaceholder': `Silmek yerine taşıyacaksanız klasörün yolu.`,
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
  'Automation.ConfirmDelete': `Kalıcı olarak sil, gereksiz dosyaları kaldırır. İptal, hiçbir şey silmeden kapatır.`,
  'Automation.ConfirmMove': `Taşı, gereksiz dosyaları seçilen hedef klasöre koyar. İptal, onları olduğu yerde bırakır.`,
  'Automation.SayThanks': `Teşekkür etmek için`,
  'Automation.ConfirmSendResultLog': `Gönder, gösterilen raporu No Faff'a iletir. İptal hiçbir şey göndermez.`,
  'Automation.CheckForUpdates': `Güncelleştirmeleri denetle`,
  'Automation.CheckForUpdates.HelpText': `github üzerindeki sürümler sayfasında daha yeni bir sürüm olup olmadığını denetler.`,
  'Automation.UpdateAvailable.HelpText': `Daha yeni sürümü indirmek için sürüm sayfasını açın ya da geçerli sürümü korumak için iptal edin.`,
  'Automation.Licence.HelpText': `github.com üzerindeki lisans dosyasını tarayıcınızda açar.`,
  'Automation.Section.BackupFolder': `Yedek klasörü`,
  'Automation.Section.Patches': `Yamalar`,
  'Automation.Section.ProductDetails': `Ürün ayrıntıları`,
  'Automation.BackupFolder': `Yedek klasörü`,
  'Automation.OperationProgress': `İşlem ilerlemesi`,
  'Automation.RescanInstaller': `{InstallerFolder}'ı yeniden tara`,
  'Automation.ScanningProgress': `Tarama ilerlemesi`,
  'Automation.StartupScanProgress': `Başlangıç taraması ilerlemesi`,
  'Automation.ViewOrphanedFiles': `Ayrıntılar, gereksiz dosyalar`,
  'Automation.ViewOrphanedFiles.HelpText': `Temizlik için uygun.`,
  'Automation.ViewRegisteredFiles': `Ayrıntılar, olduğu gibi bırakılan dosyalar`,
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
  'Tooltip.Move': `Gereksiz dosyaları yedek klasörüne taşır.`,
  'Tooltip.MoveNeedsDestination': `Gereksiz dosyaları bir yedek klasörüne taşır. Klasörü hemen ardından seçeceksiniz.`,
  'Tooltip.Delete': `Gereksiz dosyaları kalıcı olarak siler. Her şeyin yolunda olduğuna kendiniz kanaat getirmek isterseniz onun yerine Taşı'yı kullanın.`,
  'Tooltip.SigningCertificate': `Gömülü Authenticode sertifikasındaki konu adı. Zincir doğrulaması yapılmadı.`,
  'Body.MainExplanation.Lead': `Aşağıdaki gereksiz dosyaların hepsi [güvenle silinebilir].`,
  'Body.MainExplanation.Why': `Bunlar {InstallerFolder} içinde duruyor. InstallerClean, yüklü her programı Windows'a sorar: bir dosya, hiçbir program onu sahiplenmediğinde ({0}) ya da daha yeni bir yama onun yerine geçtiğinde ve hiçbir program ona geri dönemeyecek durumdayken ({1}) listelenir.`,
  'Body.MainExplanation.Action': `Onları seçeceğiniz bir yedek klasörüne taşıyın, sonra programlarınızın hâlâ normal şekilde güncellendiğine ve kaldırıldığına kanaat getirdiğinizde o klasörü silin. Onları {InstallerFolder} içine geri koymak her şeyi eski haline getirir. Ya da şimdi kalıcı olarak silin.`,
  'Body.PendingReboot.MsiExecuteMutex': `Şu anda bir şey Windows Installer'ı kullanıyor, örneğin bir Windows güncelleştirmesi ya da arka planda kurulan bir program. O sürerken Taşı ve Sil duraklatılır, böylece InstallerClean değişmekte olan {InstallerFolder} klasörüne dokunmaz. Bittiğinde yeniden tarayın, ikisi de geri gelir.`,
  'Body.PendingReboot.InstallerInProgress': `Bu makinede askıya alınmış önceki bir Windows Installer işlemi var. {InstallerFolder} klasörünü temizlemeden önce o kurulumu sürdürün ya da geri alın (veya Windows'u yeniden başlatın).`,
  'Body.PendingReboot.PendingRenameInCache': `Windows, bir sonraki yeniden başlatma için {InstallerFolder} klasörünü etkileyen bir dosya adı değişikliği sıraya aldı. Temizlemeden önce Windows'u yeniden başlatın.`,
  'Body.NoFileSelected': `Ayrıntıları görmek için bir dosya seçin.`,
  'Body.NoProductSelected': `Ayrıntıları görmek için bir ürün seçin.`,
  'Body.NoMetadata': `Kullanılabilir meta veri yok.`,
  'Body.RegisteredMissingFromDisk': `Bu kurulum dosyası eksik. Şu anda bir sorun çıkarmıyor ve ait olduğu programı güncellemeye ya da kaldırmaya çalıştığınız güne kadar da çıkarmayacak. O adım o zaman başarısız olabilir, çünkü Windows bu dosyayı arar ve bulamaz.\n\nGeri koymak için, halihazırda sahip olduğunuz sürümün kurulum programına ihtiyacınız var. Onu programın üreticisinden edinin ve mevcut kopyanızın üzerine çalıştırın. Daha yeni bir sürüm işe yaramaz: önce sizdekini kaldırması gerekir ve bu dosyaya ihtiyaç duyan tam da o adımdır. Önce kaldırmak da aynı nedenle işe yaramaz. Bunun dosyayı geri getirmesi ve ayarlarınıza dokunmaması beklenir, ancak Microsoft bunu garanti etmez.`,
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
  'Status.Moving': `Gereksiz dosyalar taşınıyor...`,
  'Status.Deleting': `Gereksiz dosyalar siliniyor...`,
  'Status.MoveCancelled.Partial': `Taşıma iptal edildi. {1} {2} içinden {0} tanesi işlendi.`,
  'Status.DeleteCancelled.Partial': `Silme iptal edildi. {1} {2} içinden {0} tanesi işlendi.`,
  'Status.MoveFailed': `{0}. Ayrıntılar {1} içinde.`,
  'Status.MoveFailed.NoLog': `{0}. Çökme günlüğü yazılamadı.`,
  'Status.DeleteFailed': `{0}. Ayrıntılar {1} içinde.`,
  'Status.DeleteFailed.NoLog': `{0}. Çökme günlüğü yazılamadı.`,
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
  'Completion.PermanentDeleteSummary.Singular': `{0} {1} kalıcı olarak silindi`,
  'Completion.PermanentDeleteSummary.Plural': `{0} {1} kalıcı olarak silindi`,
  'Summary.RegisteredStillUsed.Singular': `{0} dosya olduğu gibi bırakıldı`,
  'Summary.RegisteredStillUsed.Plural': `{0} dosya olduğu gibi bırakıldı`,
  'Summary.OrphanedToCleanUp.Singular': `temizlenecek {0} gereksiz dosya`,
  'Summary.OrphanedToCleanUp.Plural': `temizlenecek {0} gereksiz dosya`,
  'Summary.NothingListed.Singular': `InstallerClean, önbellekteki dosyalardan hangisinin buradaki yüklü programlara ait olduğundan emin olamadı, bu yüzden tek dosyayı sunmak yerine geri tuttu.`,
  'Summary.NothingListed.Plural': `InstallerClean, önbellekteki dosyalardan hangisinin buradaki yüklü programlara ait olduğundan emin olamadı, bu yüzden {0} {1} sunmak yerine geri tuttu.`,
  'Summary.MissingFromDisk.Singular': `Windows'ta {InstallerFolder} içinde bulunmayan {0} dosyaya ait bir kayıt var: {1}. Günlük kullanımda sorun çıkarmaz, ama o programın güncellenmesi ya da kaldırılması başarısız olabilir. Ne yapılacağını öğrenmek için Ayrıntılar'ı açın.`,
  'Summary.MissingFromDisk.Plural': `Windows'ta {InstallerFolder} içinde bulunmayan {0} dosyaya ait kayıtlar var: {1}. Günlük kullanımda sorun çıkarmaz, ama o programların güncellenmesi ya da kaldırılması başarısız olabilir. Ne yapılacağını öğrenmek için Ayrıntılar'ı açın.`,
  'Summary.MissingFromDisk.OtherPrograms.Singular': `{0} program daha`,
  'Summary.MissingFromDisk.OtherPrograms.Plural': `{0} program daha`,
  'Summary.MissingFromDisk.Unnamed.Singular': `kayıtlarda hiçbir program adı geçmeyen {0} dosya`,
  'Summary.MissingFromDisk.Unnamed.Plural': `kayıtlarda hiçbir program adı geçmeyen {0} dosya`,
  'Summary.OperationFiles': `{1} {2} içinden {0}`,
  'Summary.OrphanedWindow': `{0} gereksiz {1} ({2})`,
  'Summary.RegisteredWindow.Singular': `{0} dosya olduğu gibi bırakıldı ({1})`,
  'Summary.RegisteredWindow.Plural': `{0} dosya olduğu gibi bırakıldı ({1})`,
  'Confirm.MoveTitle': `{0} {1} ({2}) taşınsın mı?`,
  'Confirm.DeleteTitle': `{0} {1} ({2}) silinsin mi?`,
  'Error.AdminRequiredTitle': `Erişim reddedildi`,
  'Error.AdminRequiredBody': `Windows, InstallerClean'in erişimini reddetti, bu yüzden işlem durduruldu. Hiçbir şey kaldırılmadı.\n\nInstallerClean zaten yönetici olarak çalışıyordu, dolayısıyla onu yeniden öyle başlatmak işe yaramaz. Windows erişimi neyin reddettiği konusunda başka bir şey söylemiyor, bu yüzden denenecek belirli bir şey yok.`,
  'Error.InstallerDbUnavailableTitle': `Windows Installer kayıtları okunamadı`,
  'Error.ScanFailedTitle': `Tarama başarısız`,
  'Error.InstallerDbEmpty': `Windows Installer kayıtları tamamen boş döndü: tek bir yüklü program ya da güncelleştirme bile önbellekteki bir kurulum dosyasında hak iddia etmiyor. Çalışan bir makinede bu olmaz (yeni kurulmuş bir Windows'ta bile bunlardan vardır), yani kayıtlar ya bozuk ya da okunamadı, ve bu yanıta inanan bir tarama {InstallerFolder} içindeki her dosyayı yanlışlıkla sahipsiz sayardı. InstallerClean bunun yerine durdu. Hiçbir şey kaldırılmadı.`,
  'Error.MsiAccessDenied': `Windows Installer, InstallerClean'in yüklü olanları listelemesine izin vermedi. InstallerClean zaten yönetici olarak çalışıyordu, dolayısıyla onu yeniden yönetici olarak çalıştırmak bir şey değiştirmez. Bu liste olmadan önbellekteki hangi dosyaların hâlâ gerekli olduğunu güvenle söylemenin yolu yok, bu yüzden InstallerClean durdu. Hiçbir şey kaldırılmadı.`,
  'Error.MsiNonSuccess': `Windows Installer, InstallerClean'e yüklü programların okunabilir bir listesini veremedi: {2} {3} okudu, sonra arka arkaya {0} kayıt okunamaz döndü (son hata kodu {1}). InstallerClean, yalnızca kısmen okunmuş bir listeyle çalışmak yerine durdu. Hiçbir şey kaldırılmadı.`,
  'Error.InvalidDestinationTitle': `Geçersiz hedef`,
  'Error.DestinationWriteFailedTitle': `Hedefe yazılamadı`,
  'Error.MoveFailedTitle': `Taşıma başarısız`,
  'Error.DeleteFailedTitle': `Silme başarısız`,
  'Error.SettingNotSavedTitle': `Ayar kaydedilmedi`,
  'Error.SettingNotSavedBody': `Değişiklik kaydedilemedi. Bir sonraki açılışta InstallerClean önceki ayara dönecek.`,
  'Error.DestinationInsideInstaller': `Hedef, Windows Installer klasörünün içinde olamaz.`,
  'Error.DestinationInSystemFolder': `{0} hedefi bir Windows sistem klasörünün altına çözümleniyor. %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% ve %ProgramData% dışında bir yol seçin.`,
  'Error.NotEnoughSpaceTitle': `Yetersiz alan`,
  'Error.NotEnoughSpaceBody': `{0} konumunda yeterince yer yok\n\nGerekli: {1}\nKullanılabilir: {2}`,
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
  'Error.FileInUse.Singular': `Bu dosya başka bir program tarafından açılmış ya da kilitlenmiş, bu yüzden şu anda hiçbir şey onu kaldıramaz. Yerinde bırakıldı; daha sonra tekrar deneyin.`,
  'Error.FileInUse.Plural': `Bu dosyalar başka bir program tarafından açılmış ya da kilitlenmiş, bu yüzden şu anda hiçbir şey onları kaldıramaz. Yerlerinde bırakıldılar; daha sonra tekrar deneyin.`,
  'Error.IOFailure.Singular': `Windows bir dosya hatası bildirdi; dosya yerinde bırakıldı.`,
  'Error.IOFailure.Plural': `Windows dosya hataları bildirdi; bu dosyalar yerinde bırakıldı.`,
  'Error.UnknownError.Singular': `Bu dosyada bir şeyler ters gitti; dosya yerinde bırakıldı.`,
  'Error.UnknownError.Plural': `Bu dosyalarda bir şeyler ters gitti; dosyalar yerinde bırakıldı.`,
  'Error.MoveIntoInstaller': `Dosyaların Windows Installer klasörüne taşınması reddediliyor (hedef: {0}).`,
  'Error.DestinationNotFullyQualified': `Yedek klasörü, bir sürücü harfi ya da ağ paylaşımıyla başlayan, bir klasöre giden tam bir yol olmalıdır (örneğin D:\\Backup ya da \\\\sunucu\\backup). InstallerClean bunu kullanamaz: {0}`,
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
  'UpdateCheck.Failed.Unknown': `Denetim bilinmeyen bir nedenle başarısız oldu. Bildirmeniz gerekirse ayrıntılar {0} içindedir.`,
  'BrowserLaunch.ClipboardOk': `The link is on your clipboard, so you can paste it in yourself:&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean couldn't copy the link to your clipboard either, so here it is:&#10;&#10;{0}`,
  'Error.DestinationChangedMidBatch': `InstallerClean yedek klasörünü artık doğrulayamadı, bu yüzden durdu. {0} konumunu denetleyin, sonra Yeniden tara deyip tekrar deneyin.`,
  'Error.CannotWriteFolder': `{0} konumuna yazılamıyor.`,
  'Error.DestinationCollision': `'{0}' adlı bir dosya yedek klasöründe zaten var.`,
  'ResultLog.Sending': `Gönderiliyor...`,
  'ResultLog.Sent': `Teşekkürler! Rapor gönderildi.`,
  'ResultLog.Failed': `Gönderme başarısız oldu. Daha sonra yeniden deneyin.`,
  'ResultLog.NothingToSend': `Gönderilecek rapor yok.`,
  'ConfirmSendResultLog.Title': `Bunu göndermek ister misiniz?`,
  'ConfirmSendResultLog.Reassurance': `nofaff.netlify.app/api/result-log adresine gönderilir. Hiçbir şey sizi ya da makinenizi tanımlamaz; yalnızca InstallerClean'in çalıştığını ve [insanların ne kadar yer açtığını] bana bildirir.`,
  'Automation.ResultLogPreview': `Rapor önizlemesi`,
  'Startup.AlreadyRunningTitle': `InstallerClean`,
  'Startup.AlreadyRunningBody': `Zaten çalışıyor.`,
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
  'Display.ListSeparator': `, `,
  'Display.ElapsedLong.LessThanASecond': `bir saniyeden az`,
  'Display.ElapsedLong.Seconds': `{0:F1} saniye`,
  'CrashLog.PrivacyHeader': `# crash.log, InstallerClean'in yakalanmamış özel durumlarını tutar.\n# Yükseltilmiş yetkiyle, çerçevenin özel durum iletileri çalışan\n# oturumdaki dosya yollarını içerebilir (Windows Installer\n# sorgularının numaralandırdığı diğer kullanıcı profilleri dahil).\n# Güncelleme denetiminden ya da sonuç günlüğünün gönderiminden gelen\n# ağ hatası iletileri hedef URL'yi ve çözümlenen IP ya da proxy\n# adresini içerebilir. Okunamayan Windows Installer kayıtlarına dair\n# girdiler bir Windows hesabı SID'si (S-1-5-21-...) ve yüklü\n# yazılımın ürün kodlarını içerebilir.\n# Bu dosyayı herkese açık bir hata bildirimine eklemeden önce üç tür\n# bilgiyi de çıkarın.\n`,
  'Tooltip.ChangeLanguage': `Dili değiştir. Program yeniden başlatılacak.`,
  'Automation.ChangeLanguage': `Dili değiştir`,
  'Automation.ChangeLanguage.HelpText': `Program yeniden başlatılacak.`,
  'Cli.UnknownArgument': `Hata: bilinmeyen argüman '{0}'`,
  'Cli.Cancelling': `İptal ediliyor...`,
  'Cli.Cancelled': `İptal edildi.`,
  'Cli.GenericError': `Hata: beklenmeyen arıza ({0}). Ayrıntılar {1} konumuna yazıldı.`,
  'Cli.GenericError.NoLog': `Hata: beklenmeyen arıza ({0}). Çökme günlüğü yazılamadı.`,
  'Cli.ScanningInstaller': `{InstallerFolder} taranıyor...`,
  'Cli.FoundOrphans': `Temizlenecek {0} gereksiz {1} bulundu ({2}).`,
  'Cli.DeletingFiles': `{0} gereksiz {1} siliniyor...`,
  'Cli.DeletedFiles': `{0} gereksiz {1} kalıcı olarak silindi.`,
  'Cli.NoMoveDestination': `Hata: taşıma hedefi belirtilmedi. /m YOL kullanın. (GUI'de ayarlanan bir varsayılan, kullanıcıya özeldir ve zamanlanmış ya da hizmet hesabı çalıştırmaları için geçerli değildir.)`,
  'Cli.MoveDestinationInsideInstaller': `Hata: hedef, Windows Installer klasörünün içinde olamaz.`,
  'Cli.MoveDestinationRelative': `Hata: hedef, tam nitelenmiş bir yol olmalıdır. Alınan: {0}`,
  'Cli.MoveDestinationInSystemFolder': `Hata: {0} hedefi bir Windows sistem klasörünün altına çözümleniyor. %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% ve %ProgramData% dışında bir yol seçin.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Hata: şu anda bir şey Windows Installer'ı kullanıyor, örneğin bir Windows güncelleştirmesi ya da arka planda kurulan bir program. O sürerken /m ve /d engellenir. Bittiğinde yeniden deneyin.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Hata: bu makinede askıya alınmış önceki bir Windows Installer işlemi var. {InstallerFolder} klasörünü temizlemeden önce o kurulumu sürdürün ya da geri alın (veya Windows'u yeniden başlatın).`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Hata: yeniden başlatma sonrasına sıraya alınmış bir dosya işlemi {InstallerFolder} klasörünü hedefliyor ({0}). Temizlemeden önce o işlemi tamamlamak için Windows'u yeniden başlatın.`,
  'Cli.MovingFiles': `{0} gereksiz {1} şuraya taşınıyor: {2}...`,
  'Cli.MovedFiles': `{0} gereksiz {1} taşındı.`,
  'Cli.MutexBlocked': `Başka bir InstallerClean işlemi tek örnek kilidini tutuyor (GUI ya da başka bir CLI çalıştırması). Çıkış 75 (geçici); daha sonra yeniden denemek güvenli.`,
  'Cli.EventLogUnavailable': `Not: Olay Günlüğü'ne yazma başarısız oldu. Uygulama günlüğü izinlerini ya da Grup İlkesi'ni kontrol edin.`,
  'Cli.Help.Header': `InstallerClean - {InstallerFolder} temizliği`,
  'Cli.Help.Usage': `Kullanım:`,
  'Cli.Help.Help': `  installerclean-cli --help     Bu yardımı göster (/?, -h de kabul edilir)`,
  'Cli.Help.Version': `  installerclean-cli --version  Sürümü yazdır (-v de kabul edilir)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s         Yalnızca tara - gereksizleri listele`,
  'Cli.Help.Delete': `  installerclean-cli /d         Gereksiz dosyaları kalıcı olarak sil`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m         Kayıtlı yedek klasörüne taşı`,
  'Cli.Help.MovePath': `  installerclean-cli /m YOL     Belirtilen yola taşı`,
  'Cli.Help.NoteLine1': `installerclean-cli bitene kadar komut istemini tutar, böylece bir betik&#10;ya da zamanlanmış görev onu bekleyebilir.`,
  'Cli.Help.ExitCodesHeader': `Çıkış kodları:`,
  'Cli.Help.ExitCodeOk': `  0   başarılı: isteneni yaptı ve hiçbir şey başarısız olmadı`,
  'Cli.Help.ExitCodeError': `  1   başarısız: hiçbir şey işlenmedi (hatalı argüman, hatalı hedef,&#10;       başarısız tarama ya da her dosyanın başarısız olması)`,
  'Cli.Help.ExitCodePartial': `  2   kısmi: bir kısmı işlendi, bir kısmı işlenmedi (hata ya da Ctrl+C)`,
  'Cli.Help.ExitCodeTransient': `  75  geçici: geçici bir durum çalıştırmayı engelledi (iletiye bakın)`,
  'Cli.Help.ExitCodeCancelled': `  130 iptal edildi (Ctrl+C)`,
  'Body.NotScanned.Lead': `Henüz tarama yapılmadı.`,
  'Body.NotScanned.Why': `Hiçbir programın hâlâ ihtiyaç duymadığı yükleyici dosyaları için {InstallerFolder} klasörüne bakmak üzere Yeniden tara'ya basın.`,
  'Confirm.MoveSameDrive': `O klasör aynı sürücüde, bu yüzden siz onu silene kadar yer geri gelmez. Yeri hemen istiyorsanız başka bir sürücüde bir klasör seçin.`,
  'Error.ScanCorrelationFailed': `InstallerClean, Windows Installer kayıtlarını {InstallerFolder} içeriğiyle eşleştiremedi. Kayıtların işaret ettiklerinin neredeyse hiçbiri orada değil ve orada olanların neredeyse hiçbirinin adı hiçbir kayıtta geçmiyor, bu yüzden hiçbir dosyanın gereksiz olduğu gösterilemedi. Hiçbir şey sunulmadı ve hiçbir şey kaldırılmadı.`,
  'Error.CandidateOutsideCache': `Bu dosya doğrudan Windows Installer klasörünün içinde değil; güvenlik için reddedildi.`,
  'Completion.MoveCancelledSummary': `İptal etmeden önce {1} {2} içinden {0} tanesi taşındı.`,
  'Completion.PermanentDeleteCancelledSummary': `İptal etmeden önce {1} {2} içinden {0} tanesi kalıcı olarak silindi.`,
  'Body.PendingReboot.Lead': `Bu dosyalar şu anda temizlenemez.`,
  'Cli.TooManyArguments': `Hata: beklenmeyen fazladan argüman '{0}'. Hedef klasörünüzün adında boşluk varsa tüm yolu tırnak içine alın: /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `Klasör kullanıcıya özeldir; zamanlanmış veya SYSTEM: /m YOL.`,
  'Error.ScanRecordsUnreadable': `InstallerClean, neyin hâlâ gerekli olduğundan emin olmaya yetecek kadar Windows Installer kaydını okuyamadı: yüklü programların listesi eksik döndü, aynı kayıtları doğrudan kayıt defterinden okumak da hatalarla karşılaştı. Bir dosya, yalnızca onu adlandıran kayıt okunamayanlardan biri olduğu için sahipsiz görünebilirdi, bu yüzden InstallerClean durdu. Hiçbir şey kaldırılmadı.`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer, yüklü programlar listesinin sonunu hiç bildirmedi: InstallerClean {2} {3} okudu, sonra {0} kayıttan sonra vazgeçti (son hata kodu {1}). Sonu gelmeyen bir listeye güvenilemez, bu yüzden InstallerClean durdu. Hiçbir şey kaldırılmadı.`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer, bir programın yama listesinin sonunu hiç bildirmedi: InstallerClean {2} {3} okudu, sonra {0} kayıttan sonra vazgeçti (son hata kodu {1}). Sonu gelmeyen bir listeye güvenilemez, bu yüzden InstallerClean durdu. Hiçbir şey kaldırılmadı.`,
  'UpdateCheck.Status.UpdateAvailable': `{0} sürümü mevcut.`,
  'Completion.DonateAsk': `Yardımcı olabildiğime sevindim. Gönlünüzden koparsa, bir bahşiş kutusu var.`,
  'About.Link.Guide': `Kılavuz ve SSS`,
  'About.Link.ReportProblem': `Sorun bildir`,
  'About.AutoUpdateCheck': `Güncelleştirmeleri otomatik olarak denetle`,
  'Automation.About.Guide.HelpText': `github üzerindeki readme'yi tarayıcınızda açar.`,
  'Automation.About.ReportProblem.HelpText': `github.com üzerindeki sorun izleyiciyi (Issues) tarayıcınızda açar.`,
  'Automation.AutoUpdateCheck.HelpText': `İşaretliyse InstallerClean, çalıştırdığınızda github üzerinde daha yeni bir sürüm olup olmadığını denetler.`,
  'Tooltip.MoveSameDrive': `Gereksiz dosyaları yedek klasörüne taşır. Klasör aynı sürücüde olduğu için, o klasörü silene kadar yeri geri kazanamazsınız.`,
  'Confirm.DeletePermanently.Singular': `Bu dosya kalıcı olarak silinecek. Bunu yapmak güvenlidir, ama bir yedek isterseniz onun yerine Taşı'yı kullanın.`,
  'Confirm.DeletePermanently.Plural': `Bu dosyalar kalıcı olarak silinecek. Bunu yapmak güvenlidir, ama bir yedek isterseniz onun yerine Taşı'yı kullanın.`,
  'Error.ScanCacheRootUnresolved': `InstallerClean, {InstallerFolder} klasörünün gerçek yolunu Windows'a çözdüremedi, bu yüzden hiçbir dosyanın onun içinde olduğu gösterilemedi ve hiçbiri temizlik için sunulmadı. Bu tarama, klasör temiz olduğu için değil, o denetim başarısız olduğu için hiçbir şey bulamadı. Hiçbir şey kaldırılmadı.`,
  'Automation.Scroll.ProductDetails': `Ürün ayrıntıları`,
  'Body.PendingReboot.Other': `Windows Installer'ın sürmekte olan bir işi var, bu yüzden Taşı ve Sil duraklatıldı. InstallerClean değişmekte olan {InstallerFolder} klasörüne dokunmayacak. Bittiğinde yeniden tarayın, ikisi de geri gelir.`,
  'Cli.TooManyArgumentsNoPath': `Hata: beklenmeyen fazladan argüman '{0}'. /s ve /d başka argüman almaz ve her çalıştırmada yalnızca bir bayrak kullanılabilir.`,
  'Cli.MissingFromDisk.Singular': `Windows'ta {InstallerFolder} içinde bulunmayan {0} dosyaya ait bir kayıt var: {1}. Günlük kullanımda sorun çıkarmaz, ama o programın güncellenmesi ya da kaldırılması başarısız olabilir. Dosyayı geri koymak için, halihazırda sahip olduğunuz sürümün kurulum programına ihtiyacınız var. Onu programın üreticisinden edinin ve mevcut kopyanızın üzerine çalıştırın. Daha yeni bir sürüm işe yaramaz: önce sizdekini kaldırması gerekir ve bu dosyaya ihtiyaç duyan tam da o adımdır. Önce kaldırmak da aynı nedenle işe yaramaz. Bunun dosyayı geri getirmesi ve ayarlarınıza dokunmaması beklenir, ancak Microsoft bunu garanti etmez.`,
  'Cli.MissingFromDisk.Plural': `Windows'ta {InstallerFolder} içinde bulunmayan {0} dosyaya ait kayıtlar var: {1}. Günlük kullanımda sorun çıkarmaz, ama o programların güncellenmesi ya da kaldırılması başarısız olabilir. Bir dosyayı geri koymak için, o programın halihazırda sahip olduğunuz sürümünün kurulum programına ihtiyacınız var. Onu programın üreticisinden edinin ve mevcut kopyanızın üzerine çalıştırın. Daha yeni bir sürüm işe yaramaz: önce sizdekini kaldırması gerekir ve o dosyaya ihtiyaç duyan tam da o adımdır. Önce kaldırmak da aynı nedenle işe yaramaz. Bunun dosyayı geri getirmesi ve ayarlarınıza dokunmaması beklenir, ancak Microsoft bunu garanti etmez.`,
  'Cli.MoveNotEnoughSpace': `Hata: {0} konumunda yeterli alan yok. Bu dosyaların taşınması {1} gerektiriyor, kullanılabilir alan {2}. Hiçbir şey taşınmadı.`,
  'Cli.PendingRebootBlocked.Other': `Hata: Windows Installer'ın sürmekte olan bir işi var, bu yüzden /m ve /d engellendi. InstallerClean değişmekte olan {InstallerFolder} klasörüne dokunmayacak. Bittiğinde yeniden deneyin.`,
  'Cli.FoundNoOrphans': `Gereksiz dosya bulunamadı.`,
  'Cli.NothingOffered.Singular': `InstallerClean, önbellekteki dosyalardan hangisinin buradaki yüklü programlara ait olduğundan emin olamadı, bu yüzden tek dosyayı ({2}) sunmak yerine geri tuttu.`,
  'Cli.NothingOffered.Plural': `InstallerClean, önbellekteki dosyalardan hangisinin buradaki yüklü programlara ait olduğundan emin olamadı, bu yüzden tüm {0} {1} ({2}) sunmak yerine geri tuttu.`,
  'Cli.DestinationChangedMidBatch': `InstallerClean yedek klasörünü artık doğrulayamadı, bu yüzden durdu. {0} konumunu denetleyin, sonra komutu yeniden çalıştırın.`,
  'Cli.Help.Summary': `Yüklü hiçbir programın artık gerek duymadığı .msi/.msp dosyalarını siler.`,
  'Cli.Help.Elevation': `Yönetici komut istemi gerektirir; Windows aksi halde başlatmaz.`,
  'Error.InstallerLockUnavailableTitle': `Hiçbir dosya silinmedi`,
  'Error.MoveInstallerLockUnavailableTitle': `Hiçbir dosya taşınmadı`,
  'Error.InstallerLockUnavailable': `InstallerClean, Windows Installer'ın iki programın yüklü yazılımı aynı anda değiştirmesini engellemek için kullandığı kilidi alamadı, bu yüzden bir dosyanın işin ortasında gerekli hale gelmeyeceğini kesinleştiremedi ve hiçbir şey silinmedi. Yeniden deneyin, sürerse Windows'u yeniden başlatın.`,
  'Error.MoveInstallerLockUnavailable': `InstallerClean, Windows Installer'ın iki programın yüklü yazılımı aynı anda değiştirmesini engellemek için kullandığı kilidi alamadı, bu yüzden bir dosyanın işin ortasında gerekli hale gelmeyeceğini kesinleştiremedi ve hiçbir şey taşınmadı. Yeniden deneyin, sürerse Windows'u yeniden başlatın.`,
  'Cli.InstallerLockUnavailable': `Hata: InstallerClean, iki programın yüklü yazılımı aynı anda değiştirmesini engelleyen Windows Installer kilidini alamadı, bu yüzden bir dosyanın işin ortasında gerekli hale gelmeyeceğini kesinleştiremedi. Hiçbir şey silinmedi. Yeniden deneyin, sürerse Windows'u yeniden başlatın.`,
  'Cli.MoveInstallerLockUnavailable': `Hata: InstallerClean, iki programın yüklü yazılımı aynı anda değiştirmesini engelleyen Windows Installer kilidini alamadı, bu yüzden bir dosyanın işin ortasında gerekli hale gelmeyeceğini kesinleştiremedi. Hiçbir şey taşınmadı. Yeniden deneyin, sürerse Windows'u yeniden başlatın.`,
  'Completion.ReverifyIdentityClaimed': `{0} {1} yerinde bırakıldı, çünkü Windows'ta içeride adı geçen programın kaydı var.`,
  'Completion.ReverifyIdentityUnreadable': `{0} {1} yerinde bırakıldı, çünkü InstallerClean içeride adı geçen bir program bulamadı.`,
  'Error.ScanNoRegisteredFileInFolder': `InstallerClean, Windows Installer kayıtlarını {InstallerFolder} içeriğiyle eşleştiremedi. Klasörde dosyalar var, ama tek bir kayıt bile içindeki hiçbir şeye işaret etmiyor, bu yüzden hiçbir dosyanın gereksiz olduğu gösterilemedi. Hiçbir şey sunulmadı ve hiçbir şey kaldırılmadı.`,
  'Completion.NothingOffered': `Bu bilgisayarda hiçbir şey sunulmadı`,
  'Completion.NothingOfferedBody.Singular': `InstallerClean, önbellekteki dosyalardan hangisinin buradaki yüklü programlara ait olduğundan emin olamadı, bu yüzden tek dosyayı ({2}) sunmak yerine geri tuttu.`,
  'Completion.NothingOfferedBody.Plural': `InstallerClean, önbellekteki dosyalardan hangisinin buradaki yüklü programlara ait olduğundan emin olamadı, bu yüzden tüm {0} {1} ({2}) sunmak yerine geri tuttu.`,
  'Summary.SupersededHeldBack.Singular': `InstallerClean, yerine geçilmiş tek dosyanın artık gerekli olmadığından emin olamadı, bu yüzden onu geri tuttu.`,
  'Summary.SupersededHeldBack.Plural': `InstallerClean, yerine geçilmiş {0} dosyanın artık gerekli olmadığından emin olamadı, bu yüzden onları geri tuttu.`,
  'Cli.SupersededHeldBack.Singular': `InstallerClean, yerine geçilmiş tek dosyanın artık gerekli olmadığından emin olamadı, bu yüzden onu geri tuttu.`,
  'Cli.SupersededHeldBack.Plural': `InstallerClean, yerine geçilmiş {0} dosyanın artık gerekli olmadığından emin olamadı, bu yüzden onları geri tuttu.`,
  'Completion.HeldBack.Singular': `{0} dosya geri tutuldu. Tarama bunun gereksiz olduğunu söyledi. Son denetim bunu doğrulayamadı.`,
  'Completion.HeldBack.Plural': `{0} dosya geri tutuldu. Tarama bunların gereksiz olduğunu söyledi. Son denetim bunu doğrulayamadı.`,
  'Body.PendingReboot.PendingRenameUnresolved': `Bir sonraki yeniden başlatma için sıraya alınmış bir dosya işlemi var ve InstallerClean bu işlemin hangi dosyaları adlandırdığını bilemiyor, bu yüzden bunların {InstallerFolder} içinde olmadığını dışlayamıyor. Temizlemeden önce Windows'u yeniden başlatın.`,
  'Completion.MoveRestoreHint': `Her şeyin yolunda olduğuna kanaat getirdiğinizde o klasörü silin.`,
  'Completion.MoveRestoreHintSameDrive': `Her şeyin yolunda olduğuna kanaat getirdiğinizde o klasörü silin. Yeri ancak o zaman gerçekten geri kazanırsınız.`,
  'Confirm.MoveDestination.Singular': `Bu dosya şuraya taşınacak:`,
  'Confirm.MoveDestination.Plural': `Bu dosyalar şuraya taşınacak:`,
  'Cli.NothingListed.Singular': `InstallerClean, önbellekteki dosyalardan hangisinin buradaki yüklü programlara ait olduğundan emin olamadı, bu yüzden tek dosyayı ({2}) sunmak yerine geri tuttu.`,
  'Cli.NothingListed.Plural': `InstallerClean, önbellekteki dosyalardan hangisinin buradaki yüklü programlara ait olduğundan emin olamadı, bu yüzden {0} {1} ({2}) sunmak yerine geri tuttu.`,
  'Cli.WithheldReasons.Header': `Neden emin olunamadı:`,
  'Cli.WithheldReasons.RecordedPath': `  Windows Installer'ın kendi kayıtlarındaki bir dosya yolu çözümlenemedi, bu yüzden onunla hiçbir şey eşleştirilemedi.`,
  'Cli.WithheldReasons.FileIdentity': `  Windows'un kaydı bulunan bir dosyanın kimliği belirlenemedi, bu yüzden klasörde bulunanlarla eşleştirilemedi.`,
  'Cli.WithheldReasons.SecondInstance': `  Bir program bu bilgisayara birden fazla kez yüklenmiş olabilir ve kayıtlar bir dosyanın hangi kopyaya ait olduğunu söyleyemiyor.`,
  'Cli.PendingRebootBlocked.PendingRenameUnresolved': `Hata: bir sonraki yeniden başlatma için sıraya alınmış bir dosya işlemi var ve InstallerClean bu işlemin hangi dosyaları adlandırdığını bilemiyor, bu yüzden {InstallerFolder} klasörünü dışlayamıyor. Temizlemeden önce Windows'u yeniden başlatın.`,
  'Cli.MoveRestoreHint': `Programlarınızın hâlâ normal şekilde güncellendiğini ve kaldırıldığını doğrulayın, sonra {0} klasörünü silin.`,
  'Error.ScanStoppedDetails': `Bu ayrıca {0} içine kaydedilir.`,
  'Cli.NothingListedPerFile.Singular': `InstallerClean, bulduğu önbellek dosyalarından biri hakkında emin olamadı, bu yüzden o dosyayı ({2}) sunmak yerine geri tuttu.`,
  'Cli.NothingListedPerFile.Plural': `InstallerClean, bulduğu önbellek dosyalarından bazıları hakkında emin olamadı, bu yüzden {0} {1} ({2}) sunmak yerine geri tuttu.`,
  'Cli.NothingOfferedPerFile.Singular': `InstallerClean, bulduğu önbellek dosyasının gereksiz olduğunu saptayamadı, bu yüzden o tek dosyayı ({2}) sunmak yerine geri tuttu.`,
  'Cli.NothingOfferedPerFile.Plural': `InstallerClean, bulduğu önbellek dosyalarının hiçbirinin gereksiz olduğunu saptayamadı, bu yüzden tüm {0} {1} ({2}) sunmak yerine geri tuttu.`,
  'Completion.NothingOfferedPerFileBody.Singular': `InstallerClean, bulduğu önbellek dosyasının gereksiz olduğunu saptayamadı, bu yüzden o tek dosyayı ({2}) sunmak yerine geri tuttu.`,
  'Completion.NothingOfferedPerFileBody.Plural': `InstallerClean, bulduğu önbellek dosyalarının hiçbirinin gereksiz olduğunu saptayamadı, bu yüzden tüm {0} {1} ({2}) sunmak yerine geri tuttu.`,
  'Summary.NothingListedPerFile.Singular': `InstallerClean, bulduğu önbellek dosyalarından biri hakkında emin olamadı, bu yüzden o dosyayı sunmak yerine geri tuttu.`,
  'Summary.NothingListedPerFile.Plural': `InstallerClean, bulduğu önbellek dosyalarından bazıları hakkında emin olamadı, bu yüzden {0} {1} sunmak yerine geri tuttu.`,
  'Cli.WithheldReasons.CandidateIdentity': `  Klasördeki bir dosyanın kimliği belirlenemedi, bu yüzden kayıtlarla eşleştirilemedi.`,
  'Cli.WithheldReasons.DeclaredProductInstalled': `  Bir dosya, hâlâ yüklü olan bir programa ait olduğunu söylüyor, bu yüzden hâlâ gerekli olabilir.`,
  'Cli.WithheldReasons.DeclaredProductUnestablished': `  Ya bir dosya hangi programa ait olduğunu söylemedi ya da Windows o program hakkında yanıt vermedi.`,
  'Cli.WithheldReasons.ScreenUnanswered': `  Dosyaların hangi programlara ait olduğuna dair bir denetim, kendisine verilen dosyalarla örtüşmeyen yanıtlar verdi.`,
  'Body.PendingReboot.RegistryCheckUnreadable': `InstallerClean couldn't read one of the Windows settings it checks before touching {InstallerFolder}, so it can't tell whether an installer operation is running or waiting for a restart. Restart Windows and Re-scan. If the setting still won't read, this isn't a machine InstallerClean can clean.`,
  'Cli.InstallerLockAccessRefused': `Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted.`,
  'Cli.MoveCancelledRestoreHint': `It's simple to undo. Move them back from {0} into {InstallerFolder} and everything will be back to how it was.`,
  'Cli.MoveInstallerLockAccessRefused': `Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved.`,
  'Cli.PendingRebootBlocked.RegistryCheckUnreadable': `Error: InstallerClean couldn't read one of the registry values it checks before touching {InstallerFolder}, so it can't rule out a Windows Installer operation in flight or queued for the next restart. /m and /d are blocked. Restart Windows and try again. If the read still fails, this isn't a machine InstallerClean can clean.`,
  'Completion.MoveCancelledRestoreHint': `It's simple to undo. Move them back into {InstallerFolder} and everything will be back to how it was.`,
  'Error.InstallerLockAccessRefused': `Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted.`,
  'Error.MoveInstallerLockAccessRefused': `Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved.`,
  'Error.MoveStoppedTitle': `Move stopped`,
  'Field.NoNamedProduct': `(no program)`,
  'Summary.RegisteredWindow.Missing.Plural': `{0} missing`,
  'Summary.RegisteredWindow.Missing.Singular': `{0} missing`,
  'UpdateCheck.Failed.Unknown.NoLog': `The check failed for an unknown reason. The crash log could not be written.`,
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
