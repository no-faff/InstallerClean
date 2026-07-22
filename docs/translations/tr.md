# InstallerClean in Türkçe (Turkish)

The text of InstallerClean's interface and command-line tool in English on the left, with the Turkish translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Turkish can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

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
| Apache 2.0 licence | Apache 2.0 lisansı |
| _Move | _Taşı |
| _Move instead | Bunun yerine _taşı |
| Path to folder if you Move instead of Delete | Silmek yerine taşıyacaksanız klasör yolu |
| Open _release page | _Sürüm sayfasını aç |
| _Re-scan | _Yeniden tara |
| _Scan again | Te_krar tara |
| Send report | Rapor gönder |
| _Send | _Gönder |

## About window

| English | Türkçe |
| --- | --- |
| Guide and FAQ | Kılavuz ve SSS |
| Report a problem | Sorun bildir |
| Check for updates automatically | Güncellemeleri otomatik olarak denetle |

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
| Enumerating installed products... | Yüklü ürünler listeleniyor... |
| Checking registry for additional packages... | Ek paketler için kayıt defteri denetleniyor... |
| Found {0} registered {1}. | {0} kayıtlı {1} bulundu. |
| Scan complete ({0}) | Tarama tamamlandı ({0}) |
| Scanning local packages... | Yerel paketler taranıyor... |
| Found {0} {1} you can safely delete. | Güvenle silebileceğiniz {0} {1} bulundu. |
| Preparing destination folder... | Hedef klasör hazırlanıyor... |
| Checking the Recycle Bin... | Geri Dönüşüm Kutusu kontrol ediliyor... |
| Moving {0} {1}... | {0} {1} taşınıyor... |
| Deleting {0} {1}... | {0} {1} siliniyor... |
| Move cancelled. {0} of {1} {2} processed. | Taşıma iptal edildi. {1} {2} içinden {0} tanesi işlendi. |
| Delete cancelled. {0} of {1} {2} processed. | Silme iptal edildi. {1} {2} içinden {0} tanesi işlendi. |
| Move failed ({0}). Details in {1}. | Taşıma başarısız oldu ({0}). Ayrıntılar {1} içinde. |
| Move failed ({0}). The crash log could not be written. | Taşıma başarısız oldu ({0}). Çökme günlüğü yazılamadı. |
| Delete failed ({0}). Details in {1}. | Silme başarısız oldu ({0}). Ayrıntılar {1} içinde. |
| Delete failed ({0}). The crash log could not be written. | Silme başarısız oldu ({0}). Çökme günlüğü yazılamadı. |
| Access denied. Windows refused the scan. | Erişim reddedildi. Windows taramayı reddetti. |
| Scan failed: couldn't read the Windows Installer records. | Tarama başarısız: Windows Installer kayıtları okunamadı. |
| Scan cancelled. | Tarama iptal edildi. |
| Ready | Hazır |
| Scan failed ({0}). Details in {1}. | Tarama başarısız oldu ({0}). Ayrıntılar {1} içinde. |
| Scan failed ({0}). The crash log could not be written. | Tarama başarısız oldu ({0}). Çökme günlüğü yazılamadı. |

## Main screen text

| English | Türkçe |
| --- | --- |
| Any unneeded files below are safe to delete. | Aşağıdaki gereksiz dosyalar güvenle silinebilir. |
| They sit in C:\Windows\Installer, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | Bu dosyalar C:\Windows\Installer içinde yer alır; bir program kaldırıldığında ({0}), daha yeni bir yama bir öncekinin yerini aldığında ({1}) ya da yayımcı onu geri çektiğinde ({2}) geride kalır. InstallerClean her zaman yalnızca Windows'un kendisinin işi bittiğini bildirdiği dosyaları listeler. |
| Delete them to the Recycle Bin, or use Move instead to keep a backup. Putting the files back in C:\Windows\Installer returns you to exactly where you started. | Onları Geri Dönüşüm Kutusu'na silin ya da yedek kopya saklamak için bunun yerine Taşı'yı kullanın. Dosyaları C:\Windows\Installer içine geri koyduğunuzda her şey tam olarak başladığınız noktaya döner. |
| Nothing scanned yet. | Henüz tarama yapılmadı. |
| Press Re-scan to look through C:\Windows\Installer for installer files that no program still needs. | Hiçbir programın hâlâ ihtiyaç duymadığı yükleyici dosyaları için C:\Windows\Installer klasörüne bakmak üzere Yeniden tara'ya basın. |
| These files can't be cleaned up right now. | Bu dosyalar şu anda temizlenemez. |
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
| Orphaned | Sahipsiz |
| Superseded | Yerine geçilmiş |
| Obsoleted | Geçersiz kılınmış |

## Completion screen

| English | Türkçe |
| --- | --- |
| All clean | Her şey temiz |
| Nothing to clean up in C:\Windows\Installer | C:\Windows\Installer içinde temizlenecek bir şey yok |
| Scanned {0} {1} in {2} | {2} içinde {0} {1} tarandı |
| Copy them back to C:\Windows\Installer if anything ever breaks ([extremely unlikely]). | Bir gün bir şey bozulursa onları C:\Windows\Installer içine geri kopyalayın ([ihtimali son derece düşük]). |
| Until then, you can restore them if anything ever breaks ([extremely unlikely]). | O zamana kadar, bir gün bir şey bozulursa onları geri yükleyebilirsiniz ([ihtimali son derece düşük]). |
| Empty it to actually reclaim the space. | Alanı gerçekten geri kazanmak için onu boşaltın. |
| {0} freed | {0} yer açıldı |
| {0} cleaned up | {0} temizlendi |
| {0} moved | {0} taşındı |
| Nothing was moved | Hiçbir dosya taşınmadı |
| Nothing was deleted | Hiçbir dosya silinmedi |
| {0} of {1} could not be moved. | {1} dosya içinden {0} tanesi taşınamadı. |
| {0} of {1} could not be moved. | {1} dosya içinden {0} tanesi taşınamadı. |
| {0} of {1} could not be deleted. | {1} dosya içinden {0} tanesi silinemedi. |
| {0} of {1} could not be deleted. | {1} dosya içinden {0} tanesi silinemedi. |
| {0} {1} moved to: {2} | {0} {1} şu konuma taşındı: {2} |
| {0} {1} moved to: {2} | {0} {1} şu konuma taşındı: {2} |
| {0} {1} moved to the Recycle Bin | {0} {1} Geri Dönüşüm Kutusu'na taşındı |
| {0} {1} moved to the Recycle Bin | {0} {1} Geri Dönüşüm Kutusu'na taşındı |
| {0} {1} kept in place, because a program started needing them again after the scan. | Taramadan sonra bir programın yeniden ihtiyaç duymaya başladığı {0} {1} yerinde bırakıldı. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read when the check was repeated. | Kontrol yinelendiğinde Windows Installer kayıtları tam olarak okunamadığı için {0} {1} yerinde bırakıldı. |
| Moved {0} of {1} {2} before you cancelled. | İptal etmeden önce {1} {2} içinden {0} tanesi taşındı. |
| Moved {0} of {1} {2} to the Recycle Bin before you cancelled. | İptal etmeden önce {1} {2} içinden {0} tanesi Geri Dönüşüm Kutusu'na taşındı. |
| Permanently deleted {0} of {1} {2} before you cancelled. | İptal etmeden önce {1} {2} içinden {0} tanesi kalıcı olarak silindi. |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. | {0} {1} kalıcı olarak silindi. Geri Dönüşüm Kutusu'na gitmedi. |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. | {0} {1} kalıcı olarak silindi. Geri Dönüşüm Kutusu'na gitmedi. |
| That's fine, it was safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Sorun değil, kaldırılması güvenliydi. InstallerClean yalnızca Windows'un işi bittiğini bildirdiği dosyaları temizler, bir programın hâlâ ihtiyaç duyduğu bir dosyayı asla temizlemez. Olası olmasa da bir silme işlemi bir programı onaramaz, güncelleyemez ya da kaldıramaz hale getirirse, onu üreticisinden yeniden yüklemek genellikle dosyayı geri yükler, ama Microsoft bunu garanti etmez. |
| That's fine, they were safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Sorun değil, kaldırılması güvenliydi. InstallerClean yalnızca Windows'un işi bittiğini bildirdiği dosyaları temizler, bir programın hâlâ ihtiyaç duyduğu bir dosyayı asla temizlemez. Olası olmasa da bir silme işlemi bir programı onaramaz, güncelleyemez ya da kaldıramaz hale getirirse, onu üreticisinden yeniden yüklemek genellikle dosyayı geri yükler, ama Microsoft bunu garanti etmez. |
| Glad to help. There's a tip jar if you're feeling kind. | Yardımcı olabildiğime sevindim. Gönlünüzden koparsa, bir bahşiş kutusu var. |

## Recycle Bin unavailable

| English | Türkçe |
| --- | --- |
| The Recycle Bin isn't available for this drive | Bu sürücü için Geri Dönüşüm Kutusu kullanılamıyor |
| So this {1} ({2}) hasn't been deleted. You can move it somewhere safe, or delete it permanently. | Bu yüzden bu {1} ({2}) silinmedi. Güvenli bir yere taşıyabilir ya da kalıcı olarak silebilirsiniz. |
| So these {0} {1} ({2}) haven't been deleted. You can move them somewhere safe, or delete them permanently. | Bu yüzden bu {0} {1} ({2}) silinmedi. Güvenli bir yere taşıyabilir ya da kalıcı olarak silebilirsiniz. |
| Deleting it is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Silmek güvenli. InstallerClean yalnızca Windows'un işi bittiğini bildirdiği dosyaları temizler, bir programın hâlâ ihtiyaç duyduğu bir dosyayı asla temizlemez; Geri Dönüşüm Kutusu ise yalnızca fazladan bir güvencedir. Olası olmasa da bir silme işlemi bir programı onaramaz, güncelleyemez ya da kaldıramaz hale getirirse, onu üreticisinden yeniden yüklemek genellikle dosyayı geri yükler, ama Microsoft bunu garanti etmez. |
| Deleting them is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Silmek güvenli. InstallerClean yalnızca Windows'un işi bittiğini bildirdiği dosyaları temizler, bir programın hâlâ ihtiyaç duyduğu bir dosyayı asla temizlemez; Geri Dönüşüm Kutusu ise yalnızca fazladan bir güvencedir. Olası olmasa da bir silme işlemi bir programı onaramaz, güncelleyemez ya da kaldıramaz hale getirirse, onu üreticisinden yeniden yüklemek genellikle dosyayı geri yükler, ama Microsoft bunu garanti etmez. |

## Summaries and counts

| English | Türkçe |
| --- | --- |
| {0} file still needed | {0} dosya hâlâ gerekli |
| {0} files still needed | {0} dosya hâlâ gerekli |
| {0} unneeded file to clean up | temizlenecek {0} gereksiz dosya |
| {0} unneeded files to clean up | temizlenecek {0} gereksiz dosya |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | {0} kayıtlı dosya eksik (InstallerClean tarafından silinmedi). Şu anda bir sorun yok, ama ileride bir onarma, güncelleme ya da kaldırma işlemi başarısız olabilir. Ne yapılacağını öğrenmek için Ayrıntılar'ı açın. |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | {0} kayıtlı dosya eksik (InstallerClean tarafından silinmedi). Şu anda bir sorun yok, ama ileride bir onarma, güncelleme ya da kaldırma işlemi başarısız olabilir. Ne yapılacağını öğrenmek için Ayrıntılar'ı açın. |
| {0} installed program could not be read during this scan, so superseded patches have been kept. Orphaned files are not affected. | Bu taramada {0} yüklü program okunamadı, bu yüzden yerine geçilmiş yamalar korundu. Sahipsiz dosyalar bundan etkilenmez. |
| {0} installed programs could not be read during this scan, so superseded patches have been kept. Orphaned files are not affected. | Bu taramada {0} yüklü program okunamadı, bu yüzden yerine geçilmiş yamalar korundu. Sahipsiz dosyalar bundan etkilenmez. |
| {0} of {1} {2} | {1} {2} içinden {0} |
| {0} orphaned, {1} superseded, {2} obsoleted ({3}) | {0} sahipsiz, {1} yerine geçilmiş, {2} geçersiz kılınmış ({3}) |
| {0} registered file that is still needed ({1}) | {0} kayıtlı dosya hâlâ gerekli ({1}) |
| {0} registered files that are still needed ({1}) | {0} kayıtlı dosya hâlâ gerekli ({1}) |

## Confirmation dialogs

| English | Türkçe |
| --- | --- |
| Move {0} {1} ({2})? | {0} {1} ({2}) taşınsın mı? |
| Files will be moved to: | Dosyalar şu konuma taşınacak: |
| Delete {0} {1} ({2})? | {0} {1} ({2}) silinsin mi? |
| Files will be moved to the Recycle Bin. If you'd like backup copies, use the Move button instead. | Dosyalar Geri Dönüşüm Kutusu'na taşınacak. Yedek kopya isterseniz bunun yerine Taşı düğmesini kullanın. |
| This folder is on the same drive, so the move won't free any space by itself. You'll get the space back when you delete the files from it, or you can pick a folder on another drive instead. | Bu klasör aynı sürücüde olduğu için taşımak tek başına yer açmaz. Dosyaları oradan sildiğinizde yeri geri kazanırsınız ya da bunun yerine başka bir sürücüde bir klasör seçebilirsiniz. |

## Error messages

| English | Türkçe |
| --- | --- |
| Access denied | Erişim reddedildi |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows, InstallerClean'in erişimini reddetti, bu yüzden işlem durduruldu. Hiçbir şey kaldırılmadı.<br><br>InstallerClean zaten yönetici olarak çalışıyordu, dolayısıyla onu yeniden öyle başlatmak işe yaramaz. Windows erişimi neyin reddettiği konusunda başka bir şey söylemiyor, bu yüzden denenecek belirli bir şey yok. |
| Couldn't read the Windows Installer records | Windows Installer kayıtları okunamadı |
| Scan failed | Tarama başarısız |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in C:\Windows\Installer orphaned. InstallerClean stopped instead. Nothing has been removed. | Windows Installer kayıtları tamamen boş döndü: tek bir kurulu program ya da güncelleme bile önbellekteki bir kurulum dosyasında hak iddia etmiyor. Çalışan bir makinede bu olmaz (yeni kurulmuş bir Windows'ta bile bunlardan vardır), yani kayıtlar ya bozuk ya da okunamadı, ve bu yanıta inanan bir tarama C:\Windows\Installer içindeki her dosyayı yanlışlıkla sahipsiz sayardı. InstallerClean bunun yerine durdu. Hiçbir şey kaldırılmadı. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer, InstallerClean'in kurulu olanları listelemesine izin vermedi. InstallerClean zaten yönetici olarak çalışıyordu, dolayısıyla onu yeniden yönetici olarak çalıştırmak bir şey değiştirmez. Bu liste olmadan önbellekteki hangi dosyaların hâlâ gerekli olduğunu güvenle söylemenin yolu yok, bu yüzden InstallerClean durdu. Hiçbir şey kaldırılmadı. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer, InstallerClean'e kurulu programların okunabilir bir listesini veremedi: arka arkaya {0} kayıt okunamaz döndü (son hata kodu {1}). InstallerClean, yalnızca kısmen okunmuş bir listeyle çalışmak yerine durdu. Hiçbir şey kaldırılmadı. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer, kurulu programlar listesinin sonunu hiç bildirmedi: InstallerClean {0} kayıttan sonra vazgeçti (son hata kodu {1}). Sonu gelmeyen bir listeye güvenilemez, bu yüzden InstallerClean durdu. Hiçbir şey kaldırılmadı. |
| Windows Installer couldn't give InstallerClean a readable list of one program's patches: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer, InstallerClean'e bir programın yamalarının okunabilir bir listesini veremedi: arka arkaya {0} kayıt okunamaz döndü (son hata kodu {1}). InstallerClean, yalnızca kısmen okunmuş bir listeyle çalışmak yerine durdu. Hiçbir şey kaldırılmadı. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer, bir programın yama listesinin sonunu hiç bildirmedi: InstallerClean {0} kayıttan sonra vazgeçti (son hata kodu {1}). Sonu gelmeyen bir listeye güvenilemez, bu yüzden InstallerClean durdu. Hiçbir şey kaldırılmadı. |
| InstallerClean couldn't square this scan with the Windows Installer records: every file Windows still lists as needed is missing from C:\Windows\Installer, while the files actually in the folder match nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. | InstallerClean bu taramayı Windows Installer kayıtlarıyla bağdaştıramadı: Windows'un hâlâ gerekli olarak listelediği her dosya C:\Windows\Installer içinde yok, klasörde gerçekten bulunan dosyalar ise hiçbir kayıtla eşleşmiyor. Hiçbir gerçek makine böyle görünmez, dolayısıyla bu, güvenle kaldırabileceğiniz dosyalara değil, kayıtları okumakta bir soruna işaret ediyor. Temizlik için hiçbir şey sunulmadı ve hiçbir şey kaldırılmadı. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean, neyin hâlâ gerekli olduğundan emin olmaya yetecek kadar Windows Installer kaydını okuyamadı: kurulu programların listesi eksik döndü, aynı kayıtları doğrudan kayıt defterinden okumak da hatalarla karşılaştı. Bir dosya, yalnızca onu adlandıran kayıt okunamayanlardan biri olduğu için sahipsiz görünebilirdi, bu yüzden InstallerClean durdu. Hiçbir şey kaldırılmadı. |
| Invalid destination | Geçersiz hedef |
| Could not write to destination | Hedefe yazılamadı |
| Move failed | Taşıma başarısız |
| Delete failed | Silme başarısız |
| Setting not saved | Ayar kaydedilmedi |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | Değişiklik kaydedilemedi. Bir sonraki açılışta InstallerClean önceki ayara dönecek. |
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
| This file is not directly inside the Windows Installer folder; refused for safety. | Bu dosya doğrudan Windows Installer klasörünün içinde değil; güvenlik için reddedildi. |
| Windows refused access to this file; it was left in place. | Windows bu dosyaya erişimi reddetti; dosya yerinde bırakıldı. |
| Windows refused access to these files; they were left in place. | Windows bu dosyalara erişimi reddetti; dosyalar yerinde bırakıldı. |
| This file is open or locked by another program, so nothing can move it just now. It was left in place; try again later. | Bu dosya başka bir program tarafından açık ya da kilitli, bu yüzden şu anda onu hiçbir şey taşıyamaz. Dosya yerinde bırakıldı; daha sonra yeniden deneyin. |
| These files are open or locked by another program, so nothing can move them just now. They were left in place; try again later. | Bu dosyalar başka bir program tarafından açık ya da kilitli, bu yüzden şu anda onları hiçbir şey taşıyamaz. Dosyalar yerinde bırakıldı; daha sonra yeniden deneyin. |
| Windows reported a file error; the file was left in place. | Windows bir dosya hatası bildirdi; dosya yerinde bırakıldı. |
| Windows reported file errors; these files were left in place. | Windows dosya hataları bildirdi; bu dosyalar yerinde bırakıldı. |
| Something went wrong with this file; it was left in place. | Bu dosyada bir şeyler ters gitti; dosya yerinde bırakıldı. |
| Something went wrong with these files; they were left in place. | Bu dosyalarda bir şeyler ters gitti; dosyalar yerinde bırakıldı. |
| Couldn't move this file to the Recycle Bin (error {0}), and InstallerClean can't tell you why from that code. The file was left in place. Try the Move button instead, since it doesn't use the Recycle Bin. | Bu dosya Geri Dönüşüm Kutusu'na taşınamadı (hata {0}) ve InstallerClean bu koda bakarak size nedenini söyleyemiyor. Dosya yerinde bırakıldı. Bunun yerine Taşı düğmesini deneyin, çünkü o Geri Dönüşüm Kutusu'nu kullanmıyor. |
| Windows refused access even with administrator rights (error {0}), and InstallerClean can't tell whether the problem is the file or the Recycle Bin. The file was left in place. The Move button will work if it's the Recycle Bin, but not if it's the file. | Windows yönetici haklarıyla bile erişimi reddetti (hata {0}) ve InstallerClean sorunun dosyada mı yoksa Geri Dönüşüm Kutusu'nda mı olduğunu ayırt edemiyor. Dosya yerinde bırakıldı. Sorun Geri Dönüşüm Kutusu'ndaysa Taşı düğmesi işe yarar, ama sorun dosyadaysa yaramaz. |
| This file is open or locked by another program (error {0}), so nothing can remove it just now. It was left in place; try again later. | Bu dosya başka bir program tarafından açık ya da kilitli (hata {0}), bu yüzden şu anda onu hiçbir şey kaldıramaz. Dosya yerinde bırakıldı; daha sonra yeniden deneyin. |
| Windows deleted this file outright rather than moving it to the Recycle Bin. InstallerClean asked for the Recycle Bin, and Windows did this instead. The file is gone. | Windows bu dosyayı Geri Dönüşüm Kutusu'na taşımak yerine doğrudan sildi. InstallerClean Geri Dönüşüm Kutusu'nu istedi, Windows ise farklı davrandı. Dosya gitti. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Dosyaların Windows Installer klasörüne taşınması reddediliyor (hedef: {0}). |
| The Move location needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | Taşıma konumu, bir sürücü harfiyle ya da ağ paylaşımıyla başlayan ve bir klasöre giden tam bir yol olmalıdır (örneğin D:\Backup ya da \\server\backup). InstallerClean bunu kullanamaz: {0} |
| The Move location changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | Dosyalar taşınırken Taşıma konumu değişti (bir şey klasörü değiştirdi ya da başka bir yere yönlendirdi), bu yüzden InstallerClean yanlış yere yazmaktansa durdu. {0} konumunu kontrol edin, ardından Yeniden tara ile tekrar deneyin. |
| Cannot write to {0}. | {0} konumuna yazılamıyor. |
| Could not find a unique filename for '{0}' after 10,000 attempts. | '{0}' için 10.000 denemeden sonra benzersiz bir dosya adı bulunamadı. |

## Update check

| English | Türkçe |
| --- | --- |
| Check for updates | Güncellemeleri denetle |
| Checking... | Denetleniyor... |
| Up to date. | Güncel. |
| Version {0} is available. | {0} sürümü mevcut. |
| Update available | Güncelleme mevcut |
| You're running version {0}.<br>Version {1} is available. | {0} sürümünü çalıştırıyorsunuz.<br>{1} sürümü mevcut. |
| Couldn't reach GitHub. Check your internet connection and try again. | GitHub'a ulaşılamadı. İnternet bağlantınızı kontrol edip yeniden deneyin. |
| GitHub returned an error response. Try again in a few minutes. | GitHub bir hata yanıtı döndürdü. Birkaç dakika sonra yeniden deneyin. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | GitHub'ın yanıtı tanınan bir sürüm içermiyordu. Daha sonra yeniden deneyin ya da sürümler sayfasını doğrudan açın. |
| The check timed out. Your connection to GitHub may be slow; try again. | Denetim zaman aşımına uğradı. GitHub bağlantınız yavaş olabilir; yeniden deneyin. |
| The check failed for an unknown reason. Details are in crash.log if you need to report it. | Denetim bilinmeyen bir nedenle başarısız oldu. Bildirmeniz gerekirse ayrıntılar crash.log içindedir. |

## Opening links in your browser

| English | Türkçe |
| --- | --- |
| Couldn't open your browser | Tarayıcınız açılamadı |
| InstallerClean couldn't open your browser. The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | InstallerClean tarayıcınızı açamadı. Bağlantı panonuzda, böylece onu kendiniz yapıştırabilirsiniz:<br><br>{0} |
| InstallerClean couldn't open your browser, and couldn't copy the link to your clipboard either. The link is:<br><br>{0} | InstallerClean tarayıcınızı açamadı ve bağlantıyı panonuza da kopyalayamadı. Bağlantı şu:<br><br>{0} |

## Sending the summary

| English | Türkçe |
| --- | --- |
| Sending... | Gönderiliyor... |
| Thanks! Report sent. | Teşekkürler! Rapor gönderildi. |
| Sending failed. Try again later. | Gönderme başarısız oldu. Daha sonra yeniden deneyin. |
| No report to send. | Gönderilecek rapor yok. |
| Send this? | Bunu göndermek ister misiniz? |
| It goes to nofaff.netlify.app/api/result-log. Nothing identifies you or your machine; it just lets me know InstallerClean's working and [how much space people are freeing]. | nofaff.netlify.app/api/result-log adresine gönderilir. Hiçbir şey sizi ya da makinenizi tanımlamaz; yalnızca InstallerClean'in çalıştığını ve [insanların ne kadar yer açtığını] bana bildirir. |

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
| It's thirsty work! | Susatan bir iş! |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | İptal istendi. InstallerClean, geçerli adımın durabileceği bir noktaya gelmesini bekliyor. Yoğun G/Ç sırasında ya da bir MSI veritabanı çağrısında bu birkaç saniye sürebilir. |
| Close | Kapat |
| A star helps other people find it. | Bir yıldız, başkalarının InstallerClean'i bulmasına yardımcı olur. |
| Minimise | Simge durumuna küçült |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | Size kalmış ama makbule geçer. Yalnızca uygulamanın çalışıp çalışmadığını ve insanların ne kadar yer açtığını bana bildiren anonim bir özet gönderir. Sonraki ekran, onaylamadan önce ne gönderileceğini görmenizi sağlar. |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | Size kalmış ama makbule geçer. Yalnızca uygulamanın çalışıp çalışmadığını bana bildiren anonim bir özet gönderir. Sonraki ekran, onaylamadan önce ne gönderileceğini görmenizi sağlar. |
| Move the unneeded files to the Move location. | Gereksiz dosyaları Taşıma konumuna taşır. |
| Move the unneeded files somewhere safe. You'll choose the folder next. | Gereksiz dosyaları güvenli bir yere taşır. Klasörü sonraki adımda seçeceksiniz. |
| Move the unneeded files to the Recycle Bin. | Gereksiz dosyaları Geri Dönüşüm Kutusu'na taşır. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Gömülü Authenticode sertifikasındaki konu adı. Zincir doğrulaması yapılmadı. |
| Change language. The program will restart. | Dili değiştir. Program yeniden başlatılacak. |

## Screen reader labels

| English | Türkçe |
| --- | --- |
| Donate | Bağış yap |
| Buy me a cuppa (About window) | Bana bir çay ısmarla (Hakkında penceresi) |
| Cancel operation | İşlemi iptal et |
| Cancel scan | Taramayı iptal et |
| Cancel startup scan | Başlangıç taramasını iptal et |
| Close | Kapat |
| Close window | Pencereyi kapat |
| Close result and return to main window | Sonucu kapat ve ana pencereye dön |
| Leave a star on GitHub (About window) | GitHub'da yıldız bırak (Hakkında penceresi) |
| Minimise | Simge durumuna küçült |
| Move all unneeded installer files to the Move location | Gereksiz tüm yükleyici dosyalarını Taşıma konumuna taşı |
| Move all unneeded installer files to the Recycle Bin | Gereksiz tüm yükleyici dosyalarını Geri Dönüşüm Kutusu'na taşı |
| Delete moves the unneeded files to the Recycle Bin. Cancel closes without deleting. | Sil, gereksiz dosyaları Geri Dönüşüm Kutusu'na taşır. İptal, silmeden kapatır. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Taşı, gereksiz dosyaları seçilen hedef klasöre koyar. İptal, onları olduğu yerde bırakır. |
| Choose how to handle the unneeded files: move them somewhere safe, delete them permanently or cancel. | Gereksiz dosyaları nasıl ele alacağınızı seçin: güvenli bir yere taşıyın, kalıcı olarak silin ya da iptal edin. |
| Move the unneeded files to a folder you choose | Gereksiz dosyaları seçtiğiniz bir klasöre taşı |
| Delete the unneeded files permanently because the Recycle Bin is unavailable for this drive | Bu sürücü için Geri Dönüşüm Kutusu kullanılamadığından gereksiz dosyaları kalıcı olarak sil |
| Posts to nofaff.netlify.app. Counts and labels only. You will see the exact payload before sending. | nofaff.netlify.app adresine gönderir. Yalnızca sayılar ve etiketler. Göndermeden önce tam olarak ne gönderileceğini göreceksiniz. |
| Say thanks | Teşekkür etmek için |
| Send posts the report shown to No Faff. Cancel sends nothing. | Gönder, gösterilen raporu No Faff'a iletir. İptal hiçbir şey göndermez. |
| Check for updates | Güncellemeleri denetle |
| Checks GitHub's releases page over HTTPS for a newer version. | Daha yeni bir sürüm için GitHub'ın sürümler sayfasını HTTPS üzerinden denetler. |
| Opens the guide (README) on github.com in your browser. | github.com üzerindeki kılavuzu (README) tarayıcınızda açar. |
| Opens the issue tracker on github.com in your browser. | github.com üzerindeki sorun izleyiciyi (Issues) tarayıcınızda açar. |
| When ticked, InstallerClean checks GitHub for a newer version when you run it. | İşaretliyse InstallerClean, her çalıştırdığınızda GitHub'da daha yeni bir sürüm olup olmadığını denetler. |
| Open the release page to download the newer version, or cancel to keep the current version. | Daha yeni sürümü indirmek için sürüm sayfasını açın ya da geçerli sürümü korumak için iptal edin. |
| Apache 2.0 licence | Apache 2.0 lisansı |
| Opens the licence file on github.com in your browser. | github.com üzerindeki lisans dosyasını tarayıcınızda açar. |
| Move location | Taşıma konumu |
| Products | Ürünler |
| Patches | Yamalar |
| Product details | Ürün ayrıntıları |
| Move location | Taşıma konumu |
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
| Report preview | Rapor önizlemesi |
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
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Hata: beklenmeyen fazladan argüman '{0}'. Taşıma klasörünüzün adında boşluk varsa tüm yolu tırnak içine alın: /m "D:\My Backup" |
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
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Hata: taşıma hedefi belirtilmedi. /m YOL kullanın. (GUI'de ayarlanan bir varsayılan, kullanıcıya özeldir ve zamanlanmış ya da hizmet hesabı çalıştırmaları için geçerli değildir.) |
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
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     Bu yardımı göster (/?, -h de kabul edilir) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  Sürümü yazdır (-v de kabul edilir) |
|   installerclean-cli /s         Scan only - list removable files |   installerclean-cli /s         Yalnızca tara - gereksiz dosyaları listele |
|   installerclean-cli /d         Delete removable files (Recycle Bin) |   installerclean-cli /d         Gereksiz dosyaları sil (Geri Dönüşüm Kutusu) |
|   installerclean-cli /m         Move to saved default location |   installerclean-cli /m         Kayıtlı varsayılan konuma taşı |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m YOL     Belirtilen yola taşı |
| installerclean-cli is a real console process and blocks the prompt | installerclean-cli gerçek bir konsol işlemidir ve bitene kadar istemi |
| until it finishes; redirect or pipe its output as you would any | bloke eder; çıktısını başka herhangi bir konsol exe'sinde olduğu gibi |
| other console exe. The GUI lives in InstallerClean.exe alongside it. | yönlendirin ya da boruya verin. GUI, yanındaki InstallerClean.exe'dedir. |
| The saved default is per-user; scheduled or SYSTEM runs need /m PATH. | Kaydedilen varsayılan kullanıcıya özeldir; zamanlanmış ya da SYSTEM çalıştırmaları /m YOL gerektirir. |
| Exit codes: | Çıkış kodları: |
|   0   success: every flagged file was processed |   0   başarılı: işaretlenen her dosya işlendi |
|   1   failure: nothing processed (bad args, scan failed, all files failed) |   1   hata: hiçbir şey işlenmedi (hatalı argümanlar, tarama başarısız, tüm dosyalar başarısız) |
|   2   partial: some files processed, some failed |   2   kısmi: bazı dosyalar işlendi, bazıları başarısız |
|   75  transient: a temporary condition blocked the run (see the message) |   75  geçici: geçici bir durum çalıştırmayı engelledi (iletiye bakın) |
|   130 cancelled (Ctrl+C) |   130 iptal edildi (Ctrl+C) |
