# InstallerClean in Türkçe (Turkish)

The text of InstallerClean's interface and command-line tool in English on the left, with the Turkish translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Turkish can read through the translation and flag anything that doesn't read well. See [Can you help translate InstallerClean?](../../README.tr.md#can-you-help-translate-installerclean) for how to suggest a change, whether an issue or a pull request.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. The translation file itself is [`Strings.tr.resx`](../../src/InstallerClean.Core/Resources/Strings.tr.resx). This page is generated from it by `scripts/gen-translation-table.mjs`, so do not edit it by hand.

## Window titles

| English | Türkçe |
| --- | --- |
| InstallerClean | InstallerClean |
| About | Hakkında |
| Registered files that should not be deleted | Silinmemesi gereken kayıtlı dosyalar |
| Unneeded files that are safe to delete | Silinmesi güvenli, gereksiz dosyalar |
| Confirm move | Taşımayı onayla |
| Confirm delete | Silmeyi onayla |
| Recycle Bin unavailable | Geri Dönüşüm Kutusu kullanılamıyor |

## Section headings

| English | Türkçe |
| --- | --- |
| PRODUCTS | ÜRÜNLER |
| PATCHES | YAMALAR |
| PRODUCT DETAILS | ÜRÜN AYRINTILARI |
| MOVE LOCATION | TAŞIMA KONUMU |
| SAY THANKS | TEŞEKKÜR ETMEK İÇİN |

## Buttons and actions

| English | Türkçe |
| --- | --- |
| _About | _Hakkında |
| Copy | Kopyala |
| Cut | Kes |
| Paste | Yapıştır |
| Select all | Tümünü seç |
| _Browse... | _Gözat... |
| _Cancel | _İptal |
| Check for _updates | Güncellemeleri _denetle |
| _Close | _Kapat |
| _Delete | _Sil |
| _Delete permanently | _Kalıcı olarak sil |
| _Done | _Tamam |
| Details | Ayrıntılar |
| _Buy me a cuppa | _Bana bir çay ısmarla |
| Leave a _star on GitHub | GitHub'da _yıldız bırak |
| MIT licence | MIT lisansı |
| _Move | _Taşı |
| _Move instead | Bunun yerine _taşı |
| Path to folder if you Move instead of Delete | Silmek yerine taşıyacaksanız klasör yolu |
| Open _release page | _Sürüm sayfasını aç |
| _Re-scan | _Yeniden tara |
| _Scan again | _Yeniden tara |
| Send report | Rapor gönder |
| _Send | _Gönder |

## Field labels

| English | Türkçe |
| --- | --- |
| Reason | Neden |
| Author | Yazar |
| Application | Uygulama |
| Title | Başlık |
| Subject | Konu |
| Keywords | Anahtar sözcükler |
| Signing certificate | İmzalama sertifikası |
| File size | Dosya boyutu |
| Comment | Açıklama |
| Product name | Ürün adı |
| File | Dosya |
| Size | Boyut |
| Patches | Yamalar |
| (unknown) | (bilinmiyor) |
| (patches only) | (yalnızca yama) |
| missing | eksik |

## Status and progress

| English | Türkçe |
| --- | --- |
| Scanning... | Taranıyor... |
| Cancelling... | İptal ediliyor... |
| Starting scan... | Tarama başlatılıyor... |
| Asking Windows about installed software... | Yüklü yazılımlar için Windows sorgulanıyor... |
| Scanning installer cache folder... | Yükleyici önbellek klasörü taranıyor... |
| Enumerating installed products... | Yüklü ürünler sıralanıyor... |
| Checking registry for additional packages... | Ek paketler için kayıt defteri denetleniyor... |
| Found {0} registered {1}. | {0} kayıtlı {1} bulundu. |
| Scan complete ({0}) | Tarama tamamlandı ({0}) |
| Scanning local packages... | Yerel paketler taranıyor... |
| Found {0} {1} you can safely delete. | Güvenle silebileceğiniz {0} {1} bulundu. |
| Preparing destination folder... | Hedef klasör hazırlanıyor... |
| Moving {0} {1}... | {0} {1} taşınıyor... |
| Deleting {0} {1}... | {0} {1} siliniyor... |
| Move cancelled. {0} of {1} {2} processed. | Taşıma iptal edildi. {1} {2} içinden {0} tanesi işlendi. |
| Delete cancelled. {0} of {1} {2} processed. | Silme iptal edildi. {1} {2} içinden {0} tanesi işlendi. |
| Move failed ({0}). Details in {1}. | Taşıma başarısız oldu ({0}). Ayrıntılar {1} içinde. |
| Move failed ({0}). The crash log could not be written. | Taşıma başarısız oldu ({0}). Çökme günlüğü yazılamadı. |
| Delete failed ({0}). Details in {1}. | Silme başarısız oldu ({0}). Ayrıntılar {1} içinde. |
| Delete failed ({0}). The crash log could not be written. | Silme başarısız oldu ({0}). Çökme günlüğü yazılamadı. |
| Access denied. Run as administrator. | Erişim reddedildi. Yönetici olarak çalıştırın. |
| Scan failed: installer database unavailable. | Tarama başarısız: yükleyici veritabanı kullanılamıyor. |
| Scan cancelled. | Tarama iptal edildi. |
| Ready | Hazır |
| Scan failed ({0}). Details in {1}. | Tarama başarısız oldu ({0}). Ayrıntılar {1} içinde. |
| Scan failed ({0}). The crash log could not be written. | Tarama başarısız oldu ({0}). Çökme günlüğü yazılamadı. |

## Main screen text

| English | Türkçe |
| --- | --- |
| The unneeded files below are safe to delete. | Aşağıdaki gereksiz dosyalar güvenle silinebilir. |
| They sit in C:\Windows\Installer, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | Bu dosyalar C:\Windows\Installer içinde yer alır; bir program kaldırıldığında ({0}), daha yeni bir yama bir öncekinin yerini aldığında ({1}) ya da yayımcı onu geri çektiğinde ({2}) geride kalır. InstallerClean her zaman yalnızca Windows'un kendisinin işi bittiğini bildirdiği dosyaları listeler. |
| Delete them to the Recycle Bin, or Move them elsewhere first if you'd rather keep a copy. | Geri Dönüşüm Kutusu'na göndermek için onları silin ya da bir kopyasını saklamak isterseniz önce başka bir yere taşıyın. |
| Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch the installer cache while it's changing. Once it's done, Re-scan and they come back. | Şu anda bir şey Windows Installer'ı kullanıyor; genellikle bir Windows Update ya da arka planda kurulan bir program. Bu sürerken Taşı ve Sil duraklatılır, böylece InstallerClean değişmekte olan yükleyici önbelleğine dokunmaz. İşlem bittiğinde Yeniden tara'yı kullanın, geri gelirler. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | Bu makinede önceki bir Windows Installer işlemi askıya alınmış durumda. Önbelleği temizlemeden önce o kurulumu sürdürün ya da geri alın (veya Windows'u yeniden başlatın). |
| Windows has a file rename queued for the next restart that affects the Installer cache. Restart Windows before cleaning. | Windows, bir sonraki yeniden başlatmada yükleyici önbelleğini etkileyen bir dosya yeniden adlandırması sıraya almış. Temizlemeden önce Windows'u yeniden başlatın. |
| Select a file to view details. | Ayrıntıları görmek için bir dosya seçin. |
| Select a product to view details. | Ayrıntıları görmek için bir ürün seçin. |
| No metadata available. | Kullanılabilir meta veri yok. |
| This installer file has been deleted. InstallerClean didn't do it, it never removes a file a program still needs; something else deleted this one before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. | Bu yükleyici dosyası silinmiş. Bunu InstallerClean yapmadı; bir programın hâlâ ihtiyaç duyduğu bir dosyayı asla kaldırmaz; bunu, siz InstallerClean'i çalıştırmadan önce başka bir şey silmiş.<br><br>Şu anda bir soruna yol açmaz ve ait olduğu programı onarmaya, güncellemeye ya da kaldırmaya çalıştığınız güne kadar da açmaz. O adım o zaman başarısız olabilir, çünkü Windows bu dosyayı arar ama bulamaz.<br><br>Düzeltmeyi denemek için o programın yükleyicisini üreticisinden indirin ve mevcut kopyanızın üzerine çalıştırın (önce kaldırmayın; kaldırma işlemi de bu dosyaya ihtiyaç duyan bir adımdır). Bulabiliyorsanız kurulu olan sürümü kullanın, çünkü Windows farklı bir sürümü reddedebilir. Bu genellikle dosyayı geri yükler ve ayarlarınıza normalde dokunulmaz, ama Microsoft bunu garanti etmez; onun son çaresi programı, hatta Windows'un kendisini yeniden yüklemektir. |
| The README [explains this folder], and how to recover a file, in Microsoft's own words. | README [bu klasörü açıklar] ve bir dosyanın nasıl kurtarılacağını Microsoft'un kendi sözleriyle anlatır. |
| (none) | (yok) |

## Reasons a file is unneeded

| English | Türkçe |
| --- | --- |
| Orphaned | Yetim |
| Superseded | Yerine geçilmiş |
| Obsoleted | Geçersiz kılınmış |

## Completion screen

| English | Türkçe |
| --- | --- |
| All clean | Her şey temiz |
| Nothing to clean up in C:\Windows\Installer | C:\Windows\Installer içinde temizlenecek bir şey yok |
| Scanned {0} {1} in {2} | {2} içinde {0} {1} tarandı |
| Copy them back if anything stops working | Bir şey çalışmamaya başlarsa onları geri kopyalayın |
| Restore them from the Recycle Bin if anything stops working | Bir şey çalışmamaya başlarsa onları Geri Dönüşüm Kutusu'ndan geri yükleyin |
| {0} freed | {0} açıldı |
| {0} moved | {0} taşındı |
| {0} moved, some files could not be processed | {0} taşındı, bazı dosyalar işlenemedi |
| {0} freed, some files could not be processed | {0} açıldı, bazı dosyalar işlenemedi |
| {0} {1} moved to {2} | {0} {1}, {2} konumuna taşındı |
| {0} {1} moved to {2}. {3} {4} | {0} {1}, {2} konumuna taşındı. {3} {4} |
| {0} {1} sent to the Recycle Bin | {0} {1} Geri Dönüşüm Kutusu'na gönderildi |
| {0} {1} sent to the Recycle Bin. {2} {3} | {0} {1} Geri Dönüşüm Kutusu'na gönderildi. {2} {3} |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. | {0} {1} kalıcı olarak silindi. Geri Dönüşüm Kutusu'na gitmedi. |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. | {0} {1} kalıcı olarak silindi. Geri Dönüşüm Kutusu'na gitmedi. |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. {2} {3} | {0} {1} kalıcı olarak silindi. Geri Dönüşüm Kutusu'na gitmedi. {2} {3} |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. {2} {3} | {0} {1} kalıcı olarak silindi. Geri Dönüşüm Kutusu'na gitmedi. {2} {3} |
| That's fine, it was safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Sorun değil, kaldırılması güvenliydi. InstallerClean yalnızca Windows'un işi bittiğini bildirdiği dosyaları temizler, bir programın hâlâ ihtiyaç duyduğu bir dosyayı asla. Olası olmasa da bir silme işlemi bir programı onaramaz, güncelleyemez ya da kaldıramaz hale getirirse, onu üreticisinden yeniden yüklemek genellikle dosyayı geri yükler, ama Microsoft bunu garanti etmez. |
| That's fine, they were safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Sorun değil, kaldırılması güvenliydi. InstallerClean yalnızca Windows'un işi bittiğini bildirdiği dosyaları temizler, bir programın hâlâ ihtiyaç duyduğu bir dosyayı asla. Olası olmasa da bir silme işlemi bir programı onaramaz, güncelleyemez ya da kaldıramaz hale getirirse, onu üreticisinden yeniden yüklemek genellikle dosyayı geri yükler, ama Microsoft bunu garanti etmez. |

## Recycle Bin unavailable

| English | Türkçe |
| --- | --- |
| The Recycle Bin isn't available for this drive | Bu sürücü için Geri Dönüşüm Kutusu kullanılamıyor |
| So this {1} ({2}) hasn't been deleted. You can move it somewhere safe, or delete it permanently. | Bu yüzden bu {1} ({2}) silinmedi. Güvenli bir yere taşıyabilir ya da kalıcı olarak silebilirsiniz. |
| So these {0} {1} ({2}) haven't been deleted. You can move them somewhere safe, or delete them permanently. | Bu yüzden bu {0} {1} ({2}) silinmedi. Güvenli bir yere taşıyabilir ya da kalıcı olarak silebilirsiniz. |
| Deleting it is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Silmek güvenli. InstallerClean yalnızca Windows'un işi bittiğini bildirdiği dosyaları temizler, bir programın hâlâ ihtiyaç duyduğu bir dosyayı asla, ve Geri Dönüşüm Kutusu yalnızca fazladan bir güvencedir. Olası olmasa da bir silme işlemi bir programı onaramaz, güncelleyemez ya da kaldıramaz hale getirirse, onu üreticisinden yeniden yüklemek genellikle dosyayı geri yükler, ama Microsoft bunu garanti etmez. |
| Deleting them is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Silmek güvenli. InstallerClean yalnızca Windows'un işi bittiğini bildirdiği dosyaları temizler, bir programın hâlâ ihtiyaç duyduğu bir dosyayı asla, ve Geri Dönüşüm Kutusu yalnızca fazladan bir güvencedir. Olası olmasa da bir silme işlemi bir programı onaramaz, güncelleyemez ya da kaldıramaz hale getirirse, onu üreticisinden yeniden yüklemek genellikle dosyayı geri yükler, ama Microsoft bunu garanti etmez. |

## Summaries and counts

| English | Türkçe |
| --- | --- |
| {0} file still needed | {0} dosya hâlâ gerekli |
| {0} files still needed | {0} dosya hâlâ gerekli |
| {0} unneeded file to clean up | temizlenecek {0} gereksiz dosya |
| {0} unneeded files to clean up | temizlenecek {0} gereksiz dosya |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | {0} kayıtlı dosya eksik (InstallerClean tarafından silinmedi). Şu anda bir sorun yok, ama ileride bir onarma, güncelleme ya da kaldırma işlemi başarısız olabilir. Ne yapılacağını öğrenmek için Ayrıntılar'ı açın. |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | {0} kayıtlı dosya eksik (InstallerClean tarafından silinmedi). Şu anda bir sorun yok, ama ileride bir onarma, güncelleme ya da kaldırma işlemi başarısız olabilir. Ne yapılacağını öğrenmek için Ayrıntılar'ı açın. |
| {0} stale MSI entry detected (file already gone from disk; InstallerClean doesn't unregister it). | {0} eski MSI kaydı algılandı (dosya diskten çoktan silinmiş; InstallerClean bu kaydı silmez). |
| {0} stale MSI entries detected (files already gone from disk; InstallerClean doesn't unregister them). | {0} eski MSI kaydı algılandı (dosya diskten çoktan silinmiş; InstallerClean bu kaydı silmez). |
| {0} of {1} {2} | {1} {2} içinden {0} |
| {0} orphaned, {1} superseded, {2} obsoleted ({3}) | {0} yetim, {1} yerine geçilmiş, {2} geçersiz kılınmış ({3}) |
| {0} registered file that is still needed ({1}) | {0} kayıtlı dosya hâlâ gerekli ({1}) |
| {0} registered files that are still needed ({1}) | {0} kayıtlı dosya hâlâ gerekli ({1}) |

## Confirmation dialogs

| English | Türkçe |
| --- | --- |
| Move {0} {1} ({2})? | {0} {1} ({2}) taşınsın mı? |
| Files will be moved to {0}. | Dosyalar {0} konumuna taşınacak. |
| Delete {0} {1} ({2})? | {0} {1} ({2}) silinsin mi? |
| Files will be sent to the Recycle Bin. If you'd like backup copies, use Move instead. | Dosyalar Geri Dönüşüm Kutusu'na gönderilecek. Yedek kopya isterseniz bunun yerine Taşı'yı kullanın. |

## Error messages

| English | Türkçe |
| --- | --- |
| Administrator rights required | Yönetici hakları gerekli |
| InstallerClean requires administrator privileges.<br><br>Please right-click and choose 'Run as administrator'. | InstallerClean yönetici ayrıcalıkları gerektirir.<br><br>Sağ tıklayıp 'Yönetici olarak çalıştır'ı seçin. |
| Installer database unavailable | Yükleyici veritabanı kullanılamıyor |
| Scan failed | Tarama başarısız |
| The Windows Installer database appears to be empty or inaccessible. This is unusual even on a fresh Windows install and typically means the database is corrupt or a third-party tool has cleared it. Running 'sfc /scannow' from an elevated prompt usually repairs it. | Windows Installer veritabanı boş ya da erişilemez görünüyor. Bu, yeni bir Windows kurulumunda bile olağan dışıdır ve genellikle veritabanının bozuk olduğu ya da üçüncü taraf bir aracın onu temizlediği anlamına gelir. Yükseltilmiş bir komut isteminden 'sfc /scannow' çalıştırmak genellikle onu onarır. |
| Access denied enumerating installed products. Run as administrator. | Yüklü ürünler sıralanırken erişim reddedildi. Yönetici olarak çalıştırın. |
| Windows Installer refused to list products after {0} consecutive failures (last error code {1}). Try restarting Windows, or run 'sfc /scannow' from an elevated prompt. | Windows Installer, {0} ardışık hatadan sonra ürünleri listelemeyi reddetti (son hata kodu {1}). Windows'u yeniden başlatmayı deneyin ya da yükseltilmiş bir komut isteminden 'sfc /scannow' çalıştırın. |
| Invalid destination | Geçersiz hedef |
| Could not write to destination | Hedefe yazılamadı |
| Move failed | Taşıma başarısız |
| Delete failed | Silme başarısız |
| The destination cannot be inside the Windows Installer folder. | Hedef, Windows Installer klasörünün içinde olamaz. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | {0} hedefi bir Windows sistem klasörünün altına çözümleniyor. %SystemRoot%, %ProgramFiles% ve %ProgramData% dışında bir yol seçin. |
| Not enough space | Yetersiz alan |
| Not enough space at {0}<br><br>Required: {1}<br>Available: {2} | {0} konumunda yeterli alan yok<br><br>Gerekli: {1}<br>Kullanılabilir: {2} |
| You don't have permission to write to {0}.<br>Try a folder in your user profile or on a drive you own. | {0} konumuna yazma izniniz yok.<br>Kullanıcı profilinizdeki ya da sahibi olduğunuz bir sürücüdeki bir klasörü deneyin. |
| The path {0} is too long for Windows. Pick a shorter path. | {0} yolu Windows için çok uzun. Daha kısa bir yol seçin. |
| The folder {0} does not exist and could not be created. Check the drive letter or network path. | {0} klasörü yok ve oluşturulamadı. Sürücü harfini ya da ağ yolunu kontrol edin. |
| Windows cannot write to {0}.<br>Details in {1}. | Windows {0} konumuna yazamıyor.<br>Ayrıntılar {1} içinde. |
| Windows cannot write to {0}. The crash log could not be written. | Windows {0} konumuna yazamıyor. Çökme günlüğü yazılamadı. |
| Cannot write to {0}.<br>Details in {1}. | {0} konumuna yazılamıyor.<br>Ayrıntılar {1} içinde. |
| Cannot write to {0}. The crash log could not be written. | {0} konumuna yazılamıyor. Çökme günlüğü yazılamadı. |
| File no longer exists. | Dosya artık yok. |
| Source file is a symlink or junction; refused for safety. | Kaynak dosya bir sembolik bağlantı ya da bağlantı noktası (junction); güvenlik için reddedildi. |
| Access denied. | Erişim reddedildi. |
| The operation failed. Try again or restart Windows. | İşlem başarısız oldu. Yeniden deneyin ya da Windows'u yeniden başlatın. |
| Unknown error. | Bilinmeyen hata. |
| Couldn't send this file to the Recycle Bin (error {0}). It may be locked, in use or blocked by Windows. Move it instead. | Bu dosya Geri Dönüşüm Kutusu'na gönderilemedi (hata {0}). Kilitli, kullanımda ya da Windows tarafından engellenmiş olabilir. Bunun yerine taşıyın. |
| Windows blocked access to this file, even with administrator rights (error {0}). It is usually an ownership or permissions lock. Move it instead. | Windows, yönetici haklarıyla bile bu dosyaya erişimi engelledi (hata {0}). Genellikle bir sahiplik ya da izin kilidi olur. Bunun yerine taşıyın. |
| This file is open or locked by another program (error {0}). Close that program, or whatever is scanning it, then try again, or Move it instead. | Bu dosya başka bir program tarafından açık ya da kilitli (hata {0}). O programı, ya da onu tarayan her ne varsa kapatıp yeniden deneyin ya da bunun yerine taşıyın. |
| The file was permanently deleted because it could not be sent to the Recycle Bin. | Geri Dönüşüm Kutusu'na gönderilemediği için dosya kalıcı olarak silindi. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Dosyaların Windows Installer klasörüne taşınması reddediliyor (hedef: {0}). |
| Destination must be a fully qualified path (relative paths resolve against the process current directory and are unsafe under elevation): {0} | Hedef, tam nitelenmiş bir yol olmalıdır (göreli yollar işlemin geçerli dizinine göre çözümlenir ve yükseltilmiş çalışmada güvenli değildir): {0} |
| Destination folder canonical path changed mid-batch: {0} | Hedef klasörün kurallı yolu işlem ortasında değişti: {0} |
| Cannot write to {0}. | {0} konumuna yazılamıyor. |
| Could not find a unique filename for '{0}' after 10,000 attempts. | '{0}' için 10.000 denemeden sonra benzersiz bir dosya adı bulunamadı. |

## Update check

| English | Türkçe |
| --- | --- |
| Check for updates | Güncellemeleri denetle |
| Checking... | Denetleniyor... |
| Up to date. | Güncel. |
| Update available | Güncelleme mevcut |
| You're running version {0}.<br>Version {1} is available. | {0} sürümünü çalıştırıyorsunuz.<br>{1} sürümü mevcut. |
| Couldn't reach GitHub. Check your internet connection and try again. | GitHub'a ulaşılamadı. İnternet bağlantınızı kontrol edip yeniden deneyin. |
| GitHub returned an error response. The releases API may be rate-limited; try again in a few minutes. | GitHub bir hata yanıtı döndürdü. Sürüm API'sinde hız sınırı olabilir; birkaç dakika sonra yeniden deneyin. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | GitHub'ın yanıtı tanınan bir sürüm içermiyordu. Daha sonra yeniden deneyin ya da sürümler sayfasını doğrudan açın. |
| The check timed out. Your connection to GitHub may be slow; try again. | Denetim zaman aşımına uğradı. GitHub bağlantınız yavaş olabilir; yeniden deneyin. |
| The check failed for an unknown reason. Details are in crash.log if you need to report it. | Denetim bilinmeyen bir nedenle başarısız oldu. Bildirmeniz gerekirse ayrıntılar crash.log içindedir. |

## Opening links in your browser

| English | Türkçe |
| --- | --- |
| Couldn't open your browser | Tarayıcınız açılamadı |
| The link couldn't be opened in your normal-user browser. The URL has been copied to your clipboard so you can open it manually:<br><br>{0} | Bağlantı, normal kullanıcı tarayıcınızda açılamadı. URL panonuza kopyalandı, böylece elle açabilirsiniz:<br><br>{0} |
| The link couldn't be opened in your normal-user browser, and copying it to the clipboard also failed. The URL is:<br><br>{0} | Bağlantı, normal kullanıcı tarayıcınızda açılamadı ve panoya kopyalanması da başarısız oldu. URL şu:<br><br>{0} |

## Sending the summary

| English | Türkçe |
| --- | --- |
| Sending... | Gönderiliyor... |
| Thanks! Report sent. | Teşekkürler! Rapor gönderildi. |
| Sending failed. Try again later. | Gönderme başarısız oldu. Daha sonra yeniden deneyin. |
| No report to send. | Gönderilecek rapor yok. |
| Send this to No Faff? | Bunu No Faff'a göndermek ister misiniz? |
| Nothing identifies you or your machine; it just lets me know InstallerClean's working and how much space people are freeing. It goes to nofaff.netlify.app/api/result-log. | Hiçbir şey sizi ya da makinenizi tanımlamaz; yalnızca InstallerClean'in çalıştığını ve insanların ne kadar yer açtığını bana bildirir. nofaff.netlify.app/api/result-log adresine gönderilir. |

## Startup and crashes

| English | Türkçe |
| --- | --- |
| InstallerClean | InstallerClean |
| InstallerClean is already running. | InstallerClean zaten çalışıyor. |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | Beklenmeyen bir hata oluştu ve InstallerClean kapanmak zorunda.<br><br>{0}<br><br>Ayrıntılar şuraya yazıldı:<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | Beklenmeyen bir hata oluştu ve InstallerClean kapanmak zorunda.<br><br>{0}<br><br>Çökme günlüğü yazılamadı. |
| Startup error | Başlangıç hatası |
| Failed to start ({0}). Details written to:<br>{1} | Başlatılamadı ({0}). Ayrıntılar şuraya yazıldı:<br>{1} |
| Failed to start ({0}). The crash log could not be written. | Başlatılamadı ({0}). Çökme günlüğü yazılamadı. |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Redact both classes of detail before attaching this file to a<br># public bug report.<br> | # crash.log, InstallerClean'in işlenmeyen özel durumlarını kaydeder.<br># Yükseltilmiş çalışmada framework'ün özel durum iletileri, çalışan<br># oturumdaki dosya yollarını içerebilir (Windows Installer sorgularıyla<br># sıralanan diğer kullanıcıların profilleri dahil). Güncelleme<br># denetiminden ya da sonuç günlüğü POST'undan gelen ağ hatası iletileri,<br># hedef URL'yi ve çözümlenen IP / proxy adresini içerebilir. Bu dosyayı<br># herkese açık bir hata bildirimine eklemeden önce her iki tür ayrıntıyı<br># da çıkarın.<br> |

## Tooltips (hover text)

| English | Türkçe |
| --- | --- |
| If it helped, buy me a cup of tea. | İşinize yaradıysa bana bir bardak çay ısmarlayın. |
| It's thirsty work! | Susatan bir iş! |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | İptal istendi. InstallerClean, geçerli adımın durabileceği bir noktaya gelmesini bekliyor. Yoğun G/Ç sırasında ya da bir MSI veritabanı çağrısında bu birkaç saniye sürebilir. |
| Close | Kapat |
| Leave a star on GitHub, report an Issue or post in Discussions. Any feedback welcome. | GitHub'da yıldız bırakın, bir sorun (Issue) bildirin ya da Tartışmalar'a yazın. Her türlü geri bildirim memnuniyetle karşılanır. |
| or report an Issue or post in Discussions. Any feedback welcome. | ya da bir sorun (Issue) bildirin ya da Tartışmalar'a yazın. Her türlü geri bildirim memnuniyetle karşılanır. |
| Minimise | Simge durumuna küçült |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | Size kalmış ama makbule geçer. Yalnızca uygulamanın çalışıp çalışmadığını ve insanların ne kadar yer açtığını bana bildiren anonim bir özet gönderir. Sonraki ekran, onaylamadan önce ne gönderileceğini görmenizi sağlar. |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | Size kalmış ama makbule geçer. Yalnızca uygulamanın çalışıp çalışmadığını bana bildiren anonim bir özet gönderir. Sonraki ekran, onaylamadan önce ne gönderileceğini görmenizi sağlar. |
| Move the unneeded files to the Move location. | Gereksiz dosyaları Taşıma konumuna taşır. |
| Move the unneeded files to the Move location. Choose one first. | Gereksiz dosyaları Taşıma konumuna taşır. Önce bir konum seçin. |
| Send the unneeded files to the Recycle Bin. | Gereksiz dosyaları Geri Dönüşüm Kutusu'na gönderir. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Gömülü Authenticode sertifikasındaki konu adı. Zincir doğrulaması yapılmadı. |
| Change language. The program will restart. | Dili değiştir. Program yeniden başlatılacak. |

## Screen reader labels

| English | Türkçe |
| --- | --- |
| Buy me a cup of tea | Bana bir bardak çay ısmarla |
| Buy me a cuppa (About window) | Bana bir çay ısmarla (Hakkında penceresi) |
| Cancel operation | İşlemi iptal et |
| Cancel scan | Taramayı iptal et |
| Cancel startup scan | Başlangıç taramasını iptal et |
| Close | Kapat |
| Close window | Pencereyi kapat |
| Close result and return to main window | Sonucu kapat ve ana pencereye dön |
| Leave a star on GitHub | GitHub'da yıldız bırak |
| Leave a star on GitHub (About window) | GitHub'da yıldız bırak (Hakkında penceresi) |
| Minimise | Simge durumuna küçült |
| Move all unneeded installer files to the chosen destination folder | Gereksiz tüm yükleyici dosyalarını seçilen hedef klasöre taşı |
| Send all unneeded installer files to the Recycle Bin | Gereksiz tüm yükleyici dosyalarını Geri Dönüşüm Kutusu'na gönder |
| Delete sends the unneeded files to the Recycle Bin. Cancel closes without deleting. | Sil, gereksiz dosyaları Geri Dönüşüm Kutusu'na gönderir. İptal, silmeden kapatır. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Taşı, gereksiz dosyaları seçilen hedef klasöre koyar. İptal, onları olduğu yerde bırakır. |
| Choose how to handle the unneeded files: move them somewhere safe, delete them permanently or cancel. | Gereksiz dosyaları nasıl ele alacağınızı seçin: güvenli bir yere taşıyın, kalıcı olarak silin ya da iptal edin. |
| Move the unneeded files to a folder you choose | Gereksiz dosyaları seçtiğiniz bir klasöre taşı |
| Delete the unneeded files permanently because the Recycle Bin is unavailable for this drive | Bu sürücü için Geri Dönüşüm Kutusu kullanılamadığından gereksiz dosyaları kalıcı olarak sil |
| Posts to nofaff.netlify.app. Counts and labels only. You will see the exact payload before sending. | nofaff.netlify.app adresine gönderir. Yalnızca sayılar ve etiketler. Göndermeden önce tam olarak ne gönderileceğini göreceksiniz. |
| Say thanks | Teşekkür etmek için |
| Send posts the report shown to No Faff. Cancel sends nothing. | Gönder, gösterilen raporu No Faff'a iletir. İptal hiçbir şey göndermez. |
| Check for updates | Güncellemeleri denetle |
| Checks the GitHub releases API over HTTPS for a newer version. | Daha yeni bir sürüm için GitHub sürüm API'sini HTTPS üzerinden denetler. |
| Open the release page to download the newer version, or cancel to keep the current version. | Daha yeni sürümü indirmek için sürüm sayfasını açın ya da geçerli sürümü korumak için iptal edin. |
| MIT licence | MIT lisansı |
| Opens the licence file on github.com in your browser. | github.com üzerindeki lisans dosyasını tarayıcınızda açar. |
| Move location | Taşıma konumu |
| Products | Ürünler |
| Patches | Yamalar |
| Product details | Ürün ayrıntıları |
| Move destination folder | Taşıma hedef klasörü |
| Operation progress | İşlem ilerlemesi |
| Scan C:\Windows\Installer again | C:\Windows\Installer'ı yeniden tara |
| Scanning progress | Tarama ilerlemesi |
| Startup scan progress | Başlangıç taraması ilerlemesi |
| Details, unneeded files | Ayrıntılar, gereksiz dosyalar |
| Available for cleanup. | Temizlik için uygun. |
| Details, registered files | Ayrıntılar, kayıtlı dosyalar |
| Read-only inventory. | Salt okunur envanter. |
| Sorted by {0}, ascending | {0} ölçütüne göre artan sırada sıralandı |
| Sorted by {0}, descending | {0} ölçütüne göre azalan sırada sıralandı |
| Scan results | Tarama sonuçları |
| Result details | Sonuç ayrıntıları |
| File details | Dosya ayrıntıları |
| Dialog text | İletişim kutusu metni |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | İşlenemeyen dosyalar |
| Explains this folder, and how to recover a file, in the README | Bu klasörü açıklar ve bir dosyanın nasıl kurtarılacağını README'de anlatır |
| Result log preview | Sonuç günlüğü önizlemesi |
| Change language | Dili değiştir |
| The program will restart. | Program yeniden başlatılacak. |

## File picker

| English | Türkçe |
| --- | --- |
| Choose destination folder for moved files | Taşınan dosyalar için hedef klasörü seçin |

## Version

| English | Türkçe |
| --- | --- |
| Version {0} | Sürüm {0} |

## Word forms (singular and plural)

| English | Türkçe |
| --- | --- |
| file | dosya |
| files | dosya |
| error | hata |
| errors | hata |
| package | paket |
| packages | paket |
| product | ürün |
| products | ürün |
| patch | yama |
| patches | yama |

## Sizes and times

| English | Türkçe |
| --- | --- |
| {0:F2} GB | {0:F2} GB |
| {0:F1} MB | {0:F1} MB |
| {0:F1} KB | {0:F1} KB |
| {0} B | {0} B |
| {0:F0}ms | {0:F0}ms |
| {0:F1}s | {0:F1}s |
| less than a second | bir saniyeden az |
| {0:F1} seconds | {0:F1} saniye |

## Command-line tool (installerclean-cli)

| English | Türkçe |
| --- | --- |
| Unknown argument: '{0}' | Bilinmeyen argüman: '{0}' |
| Cancelling... | İptal ediliyor... |
| Cancelled. | İptal edildi. |
| Error: {0}. Details written to {1}. | Hata: {0}. Ayrıntılar {1} içine yazıldı. |
| Error: {0}. The crash log could not be written. | Hata: {0}. Çökme günlüğü yazılamadı. |
| Scanning C:\Windows\Installer... | C:\Windows\Installer taranıyor... |
| Found {0} {1} to clean up ({2}). | Temizlenecek {0} {1} bulundu ({2}). |
| Nothing to do. | Yapılacak bir şey yok. |
| Deleting {0} {1}... | {0} {1} siliniyor... |
| Deleted {0} {1}. | {0} {1} silindi. |
| Error: the Recycle Bin is unavailable for this volume, so nothing was deleted. Use /m to move the files instead, or re-enable the Recycle Bin and run again. | Hata: bu birim için Geri Dönüşüm Kutusu kullanılamıyor, bu yüzden hiçbir şey silinmedi. Dosyaları taşımak için /m kullanın ya da Geri Dönüşüm Kutusu'nu yeniden etkinleştirip tekrar çalıştırın. |
| Error: no move destination specified. Use /m PATH or set a default in the GUI. | Hata: taşıma hedefi belirtilmedi. /m YOL kullanın ya da GUI'de bir varsayılan ayarlayın. |
| Error: destination cannot be inside the Windows Installer folder. | Hata: hedef, Windows Installer klasörünün içinde olamaz. |
| Error: destination must be a fully qualified path. Got: {0} | Hata: hedef, tam nitelenmiş bir yol olmalıdır. Alınan: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | Hata: {0} hedefi bir Windows sistem klasörünün altına çözümleniyor. %SystemRoot%, %ProgramFiles% ve %ProgramData% dışında bir yol seçin. |
| Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are blocked while that runs. Try again once it finishes. | Hata: şu anda bir şey Windows Installer'ı kullanıyor; genellikle bir Windows Update ya da arka planda kurulan bir program. Bu sürerken Taşı ve Sil engellenir. İşlem bittiğinde yeniden deneyin. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | Hata: bu makinede önceki bir Windows Installer işlemi askıya alınmış durumda. Önbelleği temizlemeden önce o kurulumu sürdürün ya da geri alın (veya Windows'u yeniden başlatın). |
| Error: a queued post-reboot file operation targets the Installer cache ({0}). Restart Windows to complete that operation before cleaning. | Hata: bir sonraki yeniden başlatma için sıraya alınmış bir dosya işlemi yükleyici önbelleğini hedefliyor ({0}). Temizlemeden önce bu işlemi tamamlamak için Windows'u yeniden başlatın. |
| Moving {0} {1} to {2}... | {0} {1}, {2} konumuna taşınıyor... |
| Moved {0} {1}. | {0} {1} taşındı. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Başka bir InstallerClean işlemi tek örnek kilidini tutuyor (GUI ya da başka bir CLI çalıştırması). Çıkış 75 (geçici); daha sonra yeniden denemek güvenli. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Not: Olay Günlüğü'ne yazma başarısız oldu. Uygulama günlüğü izinlerini ya da Grup İlkesi'ni kontrol edin. |
| InstallerClean - clean up C:\Windows\Installer | InstallerClean - C:\Windows\Installer temizliği |
| Usage: | Kullanım: |
|   installerclean-cli --help   Show this help (also accepts /?, -h) |   installerclean-cli --help     Bu yardımı göster (/?, -h de kabul edilir) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  Sürümü yazdır (-v de kabul edilir) |
|   installerclean-cli /s       Scan only - list removable files |   installerclean-cli /s         Yalnızca tara - gereksiz dosyaları listele |
|   installerclean-cli /d       Delete removable files (Recycle Bin) |   installerclean-cli /d         Gereksiz dosyaları sil (Geri Dönüşüm Kutusu) |
|   installerclean-cli /m       Move to saved default location |   installerclean-cli /m         Kayıtlı varsayılan konuma taşı |
|   installerclean-cli /m PATH  Move to specified path |   installerclean-cli /m YOL     Belirtilen yola taşı |
| installerclean-cli is a real console process and blocks the prompt | installerclean-cli gerçek bir konsol işlemidir ve bitene kadar istemi |
| until it finishes; redirect or pipe its output as you would any | bloke eder; çıktısını başka herhangi bir konsol exe'sinde olduğu gibi |
| other console exe. The GUI lives in InstallerClean.exe alongside it. | yönlendirin ya da boruya verin. GUI, yanındaki InstallerClean.exe'dedir. |
| Exit codes: | Çıkış kodları: |
|   0   success: every flagged file was processed |   0   başarılı: işaretlenen her dosya işlendi |
|   1   failure: nothing processed (bad args, scan failed, all files failed) |   1   hata: hiçbir şey işlenmedi (hatalı argümanlar, tarama başarısız, tüm dosyalar başarısız) |
|   2   partial: some files processed, some failed |   2   kısmi: bazı dosyalar işlendi, bazıları başarısız |
|   75  transient: a temporary condition blocked the run (see the message) |   75  geçici: geçici bir durum çalıştırmayı engelledi (iletiye bakın) |
|   130 cancelled (Ctrl+C) |   130 iptal edildi (Ctrl+C) |
