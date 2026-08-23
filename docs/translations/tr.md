# InstallerClean in Türkçe (Turkish)

The text of InstallerClean's interface and command-line tool in English on the left, with the Turkish translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Turkish can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.tr.resx`](../../src/InstallerClean.Core/Resources/Strings.tr.resx), so do not edit it by hand. The Turkish translation itself lives in [`gen-strings-tr.mjs`](../../scripts/translations/gen-strings-tr.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | Türkçe |
| --- | --- |
| InstallerClean | InstallerClean |
| About | Hakkında |
| Files left alone | Olduğu gibi bırakılan dosyalar |
| Unneeded files that are safe to delete | Silinmesi güvenli, gereksiz dosyalar |

## Section headings

| English | Türkçe |
| --- | --- |
| PATCHES | YAMALAR |
| PRODUCT DETAILS | ÜRÜN AYRINTILARI |
| BACKUP FOLDER | HEDEF KLASÖR |
| SAY THANKS | TEŞEKKÜR ETMEK İÇİN |

## Buttons and actions

| English | Türkçe |
| --- | --- |
| _About | _Hakkında |
| Copy | Kopyala |
| Cut | Kes |
| Paste | Yapıştır |
| Select all | Tümünü seç |
| _Browse... | _Göz at... |
| _Cancel | _İptal |
| Check for _updates | Güncelleştirmeleri _denetle |
| _Close | _Kapat |
| _Delete permanently | _Kalıcı olarak sil |
| _Done | _Tamam |
| Details | Ayrıntılar |
| _Buy me a cuppa | _Bana bir çay ısmarla |
| Leave a _star on GitHub | GitHub'da _yıldız bırak |
| Apache 2.0 licence | Apache 2.0 lisansı |
| _Move | _Taşı |
| Path to folder if you move rather than delete. | Silmek yerine taşıyacaksanız klasörün yolu. |
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
| Check for updates automatically | Güncelleştirmeleri otomatik olarak denetle |

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
| Moving unneeded files... | Gereksiz dosyalar taşınıyor... |
| Deleting unneeded files... | Gereksiz dosyalar siliniyor... |
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
| Any unneeded files below are [safe to delete]. | Aşağıdaki gereksiz dosyaların hepsi [güvenle silinebilir]. |
| They sit in {InstallerFolder}. InstallerClean asks Windows about every installed program: a file is listed when no program claims it ({0}), or when a newer patch has replaced it and no program could roll back to it ({1}). | Bunlar {InstallerFolder} içinde duruyor. InstallerClean, yüklü her programı Windows'a sorar: bir dosya, hiçbir program onu sahiplenmediğinde ({0}) ya da daha yeni bir yama onun yerine geçtiğinde ve hiçbir program ona geri dönemeyecek durumdayken ({1}) listelenir. |
| Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update, repair and uninstall as normal. Putting them back into {InstallerFolder} restores everything. Or delete them permanently now. | Onları seçeceğiniz bir hedef klasöre taşıyın, programlarınızın her zamanki gibi güncellendiğinden, onarıldığından ve kaldırılabildiğinden emin olunca da o klasörü silin. Onları {InstallerFolder} içine geri koymak her şeyi eski haline getirir. Ya da şimdi kalıcı olarak silin. |
| Nothing scanned yet. | Henüz tarama yapılmadı. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Hiçbir programın hâlâ ihtiyaç duymadığı yükleyici dosyaları için {InstallerFolder} klasörüne bakmak üzere Yeniden tara'ya basın. |
| These files can't be cleaned up right now. | Bu dosyalar şu anda temizlenemez. |
| Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Şu anda bir şey Windows Installer'ı kullanıyor, örneğin bir Windows güncelleştirmesi ya da arka planda kurulan bir program. O sürerken Taşı ve Sil duraklatılır, böylece InstallerClean değişmekte olan {InstallerFolder} klasörüne dokunmaz. Bittiğinde yeniden tarayın, ikisi de geri gelir. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Bu makinede askıya alınmış önceki bir Windows Installer işlemi var. {InstallerFolder} klasörünü temizlemeden önce o kurulumu sürdürün ya da geri alın (veya Windows'u yeniden başlatın). |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows, bir sonraki yeniden başlatma için {InstallerFolder} klasörünü etkileyen bir dosya adı değişikliği sıraya aldı. Temizlemeden önce Windows'u yeniden başlatın. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer'ın sürmekte olan bir işi var, bu yüzden Taşı ve Sil duraklatıldı. InstallerClean değişmekte olan {InstallerFolder} klasörüne dokunmayacak. Bittiğinde yeniden tarayın, ikisi de geri gelir. |
| Select a file to view details. | Ayrıntıları görmek için bir dosya seçin. |
| Select a product to view details. | Ayrıntıları görmek için bir ürün seçin. |
| No metadata available. | Kullanılabilir meta veri yok. |
| This installer file is missing. It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it, and its own last resort is reinstalling the program. | Bu yükleyici dosyası eksik. Şimdilik bir sorun çıkarmıyor ve ait olduğu programı onarmayı, güncellemeyi ya da kaldırmayı deneyeceğiniz güne kadar da çıkarmayacak. O adım bu yüzden başarısız olabilir, çünkü Windows bu dosyayı arar ve dosya yerinde değildir.<br><br>Düzeltmeyi denemek için o programın yükleyicisini üreticisinden indirin ve mevcut kopyanızın üzerine çalıştırın (önce kaldırmayın: kaldırma işleminin kendisi de bu dosyaya ihtiyaç duyan bir adımdır). Bulabiliyorsanız yüklü olan sürümü kullanın, çünkü Windows farklı bir sürümü reddedebilir. Bu, dosyayı geri getirmeli ve ayarlarınıza dokunmamalıdır, ancak Microsoft bunu garanti etmez ve kendi son çaresi programı yeniden kurmaktır. |
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
| Nothing removed | Hiçbir şey kaldırılmadı |
| Nothing to clean up in {InstallerFolder} | {InstallerFolder} içinde temizlenecek bir şey yok |
| Scanned {0} {1} in {2} | {2} içinde {0} {1} tarandı |
| Nothing offered on this PC | Bu bilgisayarda hiçbir şey sunulmadı |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({1}) it might otherwise have offered. | InstallerClean, önbellekteki dosyalardan hangisinin buradaki yüklü programlara ait olduğundan emin olamadı, bu yüzden sunabileceği tek dosyayı ({1}) tuttu. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back all {0} files ({1}) it might otherwise have offered. | InstallerClean, önbellekteki dosyalardan hangisinin buradaki yüklü programlara ait olduğundan emin olamadı, bu yüzden sunabileceği {0} dosyanın ({1}) tamamını tuttu. |
| The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | O klasördeki dosya [güvenle kaldırılabilir], yani klasörü istediğiniz zaman silin. O zamana dek, bir program gerçekten ihtiyaç duyarsa dosyayı {InstallerFolder} içine geri koyabilirsiniz (son derece düşük bir ihtimal). |
| The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | O klasördeki dosyalar [güvenle kaldırılabilir], yani klasörü istediğiniz zaman silin. O zamana dek, bir program gerçekten birine ihtiyaç duyarsa dosyaları {InstallerFolder} içine geri koyabilirsiniz (son derece düşük bir ihtimal). |
| The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | O klasördeki dosya [güvenle kaldırılabilir], yani yeri gerçekten geri kazanmak istediğinizde klasörü silin ya da başka bir sürücüye taşıyın. O zamana dek, bir program gerçekten ihtiyaç duyarsa dosyayı {InstallerFolder} içine geri koyabilirsiniz (son derece düşük bir ihtimal). |
| The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | O klasördeki dosyalar [güvenle kaldırılabilir], yani yeri gerçekten geri kazanmak istediğinizde klasörü silin ya da başka bir sürücüye taşıyın. O zamana dek, bir program gerçekten birine ihtiyaç duyarsa dosyaları {InstallerFolder} içine geri koyabilirsiniz (son derece düşük bir ihtimal). |
| {0} freed | {0} yer açıldı |
| {0} moved | {0} taşındı |
| Nothing was moved | Hiçbir dosya taşınmadı |
| Nothing was deleted | Hiçbir dosya silinmedi |
| {0} of {1} could not be moved. | {1} dosya içinden {0} tanesi taşınamadı. |
| {0} of {1} could not be moved. | {1} dosya içinden {0} tanesi taşınamadı. |
| {0} of {1} could not be deleted. | {1} dosya içinden {0} tanesi silinemedi. |
| {0} of {1} could not be deleted. | {1} dosya içinden {0} tanesi silinemedi. |
| {0} {1} moved to: {2} | {0} {1} şu konuma taşındı: {2} |
| {0} {1} moved to: {2} | {0} {1} şu konuma taşındı: {2} |
| {0} {1} kept in place, because the records now claim what the scan flagged. | {0} {1} yerinde bırakıldı, çünkü kayıtlar artık taramanın işaretlediğini sahipleniyor. |
| {0} {1} kept in place, because the Windows Installer records had changed by the final check. | {0} {1} yerinde bırakıldı, çünkü son denetime kadar Windows Installer kayıtları değişmişti. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read in the final check. | {0} {1} yerinde bırakıldı, çünkü son denetimde Windows Installer kayıtları tümüyle okunamadı. |
| {0} {1} kept in place, because by the final check InstallerClean could not be certain which cached files belong to the programs installed here. | {0} {1} yerinde bırakıldı, çünkü son denetime kadar InstallerClean önbellekteki dosyalardan hangisinin buradaki yüklü programlara ait olduğundan emin olamadı. |
| {0} {1} kept in place, because Windows has a record of the program named inside. | {0} {1} yerinde bırakıldı, çünkü Windows'ta içeride adı geçen programın kaydı var. |
| {0} {1} kept in place, because InstallerClean couldn't find a program named inside. | {0} {1} yerinde bırakıldı, çünkü InstallerClean içeride adı geçen bir program bulamadı. |
| Moved {0} of {1} {2} before you cancelled. | İptal etmeden önce {1} {2} içinden {0} tanesi taşındı. |
| Permanently deleted {0} of {1} {2} before you cancelled. | İptal etmeden önce {1} {2} içinden {0} tanesi kalıcı olarak silindi. |
| {0} {1} permanently deleted | {0} {1} kalıcı olarak silindi |
| {0} {1} permanently deleted | {0} {1} kalıcı olarak silindi |
| Glad to help. There's a tip jar if you're feeling kind. | Yardımcı olabildiğime sevindim. Gönlünüzden koparsa, bir bahşiş kutusu var. |

## Summaries and counts

| English | Türkçe |
| --- | --- |
| {0} file left alone | {0} dosya olduğu gibi bırakıldı |
| {0} files left alone | {0} dosya olduğu gibi bırakıldı |
| {0} unneeded file to clean up | temizlenecek {0} gereksiz dosya |
| {0} unneeded files to clean up | temizlenecek {0} gereksiz dosya |
| Windows has a record for {0} file that isn't in {InstallerFolder}: {1}. It causes no trouble day to day, but a repair, update or uninstall can fail on it. Open Details for what to do. | Windows'ta {InstallerFolder} içinde bulunmayan {0} dosya için kayıt var: {1}. Gündelik kullanımda sorun çıkarmaz, ama bir onarım, güncelleme ya da kaldırma işlemi bu yüzden başarısız olabilir. Ne yapmanız gerektiği için Ayrıntılar'ı açın. |
| Windows has records for {0} files that aren't in {InstallerFolder}: {1}. They cause no trouble day to day, but a repair, update or uninstall can fail on them. Open Details for what to do. | Windows'ta {InstallerFolder} içinde bulunmayan {0} dosya için kayıt var: {1}. Gündelik kullanımda sorun çıkarmazlar, ama bir onarım, güncelleme ya da kaldırma işlemi bu yüzden başarısız olabilir. Ne yapmanız gerektiği için Ayrıntılar'ı açın. |
| {0} other program | {0} program daha |
| {0} other programs | {0} program daha |
| {0} file with no program named in the records | kayıtlarda hiçbir program adı geçmeyen {0} dosya |
| {0} files with no program named in the records | kayıtlarda hiçbir program adı geçmeyen {0} dosya |
| On this PC InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file rather than listing it. | On this PC InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file rather than listing it. |
| On this PC InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} rather than listing them. | On this PC InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} rather than listing them. |
| InstallerClean couldn't match up everything in the Windows records, so it didn't read all of them. The unneeded files above are unaffected, but what it says about files missing from {InstallerFolder} may be short of the full picture. Re-scan to try again. | InstallerClean, Windows kayıtlarındaki her şeyi eşleştiremedi, bu yüzden hepsini okumadı. Yukarıdaki gereksiz dosyalar bundan etkilenmez, ama {InstallerFolder} içinde eksik olan dosyalar hakkında söyledikleri tam tabloyu vermiyor olabilir. Yeniden tarayarak tekrar deneyin. |
| {0} of {1} {2} | {1} {2} içinden {0} |
| {0} unneeded {1} ({2}) | {0} gereksiz {1} ({2}) |
| {0} file left alone ({1}) | {0} dosya olduğu gibi bırakıldı ({1}) |
| {0} files left alone ({1}) | {0} dosya olduğu gibi bırakıldı ({1}) |

## Confirmation dialogs

| English | Türkçe |
| --- | --- |
| Move {0} {1} ({2})? | {0} {1} ({2}) taşınsın mı? |
| Move to: | Şuraya taşı: |
| Delete {0} {1} ({2})? | {0} {1} ({2}) silinsin mi? |
| This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. | Bu dosya kalıcı olarak silinecek. [Güvenle silinebilir], ama bir yedek isterseniz onun yerine Taşı düğmesini kullanın. |
| Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. | Dosyalar kalıcı olarak silinecek. [Güvenle silinebilirler], ama bir yedek isterseniz onun yerine Taşı düğmesini kullanın. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | O klasör aynı sürücüde, bu yüzden siz onu silene kadar yer geri gelmez. Yeri hemen istiyorsanız başka bir sürücüde bir klasör seçin. |

## Error messages

| English | Türkçe |
| --- | --- |
| Access denied | Erişim reddedildi |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows, InstallerClean'in erişimini reddetti, bu yüzden işlem durduruldu. Hiçbir şey kaldırılmadı.<br><br>InstallerClean zaten yönetici olarak çalışıyordu, dolayısıyla onu yeniden öyle başlatmak işe yaramaz. Windows erişimi neyin reddettiği konusunda başka bir şey söylemiyor, bu yüzden denenecek belirli bir şey yok. |
| Couldn't read the Windows Installer records | Windows Installer kayıtları okunamadı |
| Scan failed | Tarama başarısız |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed. | Windows Installer kayıtları tamamen boş döndü: tek bir yüklü program ya da güncelleştirme bile önbellekteki bir kurulum dosyasında hak iddia etmiyor. Çalışan bir makinede bu olmaz (yeni kurulmuş bir Windows'ta bile bunlardan vardır), yani kayıtlar ya bozuk ya da okunamadı, ve bu yanıta inanan bir tarama {InstallerFolder} içindeki her dosyayı yanlışlıkla sahipsiz sayardı. InstallerClean bunun yerine durdu. Hiçbir şey kaldırılmadı. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer, InstallerClean'in yüklü olanları listelemesine izin vermedi. InstallerClean zaten yönetici olarak çalışıyordu, dolayısıyla onu yeniden yönetici olarak çalıştırmak bir şey değiştirmez. Bu liste olmadan önbellekteki hangi dosyaların hâlâ gerekli olduğunu güvenle söylemenin yolu yok, bu yüzden InstallerClean durdu. Hiçbir şey kaldırılmadı. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer, InstallerClean'e yüklü programların okunabilir bir listesini veremedi: arka arkaya {0} kayıt okunamaz döndü (son hata kodu {1}). InstallerClean, yalnızca kısmen okunmuş bir listeyle çalışmak yerine durdu. Hiçbir şey kaldırılmadı. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer, yüklü programlar listesinin sonunu hiç bildirmedi: InstallerClean {0} kayıttan sonra vazgeçti (son hata kodu {1}). Sonu gelmeyen bir listeye güvenilemez, bu yüzden InstallerClean durdu. Hiçbir şey kaldırılmadı. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer, bir programın yama listesinin sonunu hiç bildirmedi: InstallerClean {0} kayıttan sonra vazgeçti (son hata kodu {1}). Sonu gelmeyen bir listeye güvenilemez, bu yüzden InstallerClean durdu. Hiçbir şey kaldırılmadı. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean, Windows Installer kayıtlarını {InstallerFolder} içeriğiyle eşleştiremedi. Kayıtların işaret ettiklerinin neredeyse hiçbiri orada değil ve orada olanların neredeyse hiçbirinin adı hiçbir kayıtta geçmiyor, bu yüzden hiçbir dosyanın gereksiz olduğu gösterilemedi. Hiçbir şey sunulmadı ve hiçbir şey kaldırılmadı. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean, Windows Installer kayıtlarını {InstallerFolder} içeriğiyle eşleştiremedi. Klasörde dosyalar var, ama tek bir kayıt bile içindeki hiçbir şeye işaret etmiyor, bu yüzden hiçbir dosyanın gereksiz olduğu gösterilemedi. Hiçbir şey sunulmadı ve hiçbir şey kaldırılmadı. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean, neyin hâlâ gerekli olduğundan emin olmaya yetecek kadar Windows Installer kaydını okuyamadı: yüklü programların listesi eksik döndü, aynı kayıtları doğrudan kayıt defterinden okumak da hatalarla karşılaştı. Bir dosya, yalnızca onu adlandıran kayıt okunamayanlardan biri olduğu için sahipsiz görünebilirdi, bu yüzden InstallerClean durdu. Hiçbir şey kaldırılmadı. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean, {InstallerFolder} klasörünün gerçek yolunu Windows'a çözdüremedi, bu yüzden hiçbir dosyanın onun içinde olduğu gösterilemedi ve hiçbiri temizlik için sunulmadı. Bu tarama, klasör temiz olduğu için değil, o denetim başarısız olduğu için hiçbir şey bulamadı. Hiçbir şey kaldırılmadı. |
| Nothing was deleted | Hiçbir dosya silinmedi |
| Nothing was moved | Hiçbir dosya taşınmadı |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | InstallerClean, Windows Installer'ın iki programın yüklü yazılımı aynı anda değiştirmesini engellemek için kullandığı kilidi alamadı, bu yüzden bir dosyanın işin ortasında gerekli hale gelmeyeceğini kesinleştiremedi ve hiçbir şey silinmedi. Yeniden deneyin, sürerse Windows'u yeniden başlatın. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. Try again, and restart Windows if it keeps happening. | InstallerClean, Windows Installer'ın iki programın yüklü yazılımı aynı anda değiştirmesini engellemek için kullandığı kilidi alamadı, bu yüzden bir dosyanın işin ortasında gerekli hale gelmeyeceğini kesinleştiremedi ve hiçbir şey taşınmadı. Yeniden deneyin, sürerse Windows'u yeniden başlatın. |
| Invalid destination | Geçersiz hedef |
| Could not write to destination | Hedefe yazılamadı |
| Move failed | Taşıma başarısız |
| Delete failed | Silme başarısız |
| Setting not saved | Ayar kaydedilmedi |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | Değişiklik kaydedilemedi. Bir sonraki açılışta InstallerClean önceki ayara dönecek. |
| The destination cannot be inside the Windows Installer folder. | Hedef, Windows Installer klasörünün içinde olamaz. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | {0} hedefi bir Windows sistem klasörünün altına çözümleniyor. %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% ve %ProgramData% dışında bir yol seçin. |
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
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | Bu dosya başka bir program tarafından açılmış ya da kilitlenmiş, bu yüzden şu anda hiçbir şey onu kaldıramaz. Yerinde bırakıldı; daha sonra tekrar deneyin. |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | Bu dosyalar başka bir program tarafından açılmış ya da kilitlenmiş, bu yüzden şu anda hiçbir şey onları kaldıramaz. Yerlerinde bırakıldılar; daha sonra tekrar deneyin. |
| Windows reported a file error; the file was left in place. | Windows bir dosya hatası bildirdi; dosya yerinde bırakıldı. |
| Windows reported file errors; these files were left in place. | Windows dosya hataları bildirdi; bu dosyalar yerinde bırakıldı. |
| Something went wrong with this file; it was left in place. | Bu dosyada bir şeyler ters gitti; dosya yerinde bırakıldı. |
| Something went wrong with these files; they were left in place. | Bu dosyalarda bir şeyler ters gitti; dosyalar yerinde bırakıldı. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Dosyaların Windows Installer klasörüne taşınması reddediliyor (hedef: {0}). |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | Hedef klasör, bir sürücü harfi ya da ağ paylaşımıyla başlayan, bir klasöre giden tam bir yol olmalıdır (örneğin D:\Backup ya da \\sunucu\backup). InstallerClean bunu kullanamaz: {0} |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | InstallerClean hedef klasörü artık doğrulayamadı, bu yüzden yanlış yere yazmak yerine durdu. {0} konumunu denetleyin, sonra Yeniden tara deyip tekrar deneyin. |
| Cannot write to {0}. | {0} konumuna yazılamıyor. |
| Could not find a unique filename for '{0}' after 10,000 attempts. | '{0}' için 10.000 denemeden sonra benzersiz bir dosya adı bulunamadı. |

## Update check

| English | Türkçe |
| --- | --- |
| Check for updates | Güncelleştirmeleri denetle |
| Checking... | Denetleniyor... |
| Up to date. | Güncel. |
| Version {0} is available. | {0} sürümü mevcut. |
| Update available | Güncelleştirme mevcut |
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
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log, InstallerClean'in yakalanmamış özel durumlarını tutar.<br># Yükseltilmiş yetkiyle, çerçevenin özel durum iletileri çalışan<br># oturumdaki dosya yollarını içerebilir (Windows Installer<br># sorgularının numaralandırdığı diğer kullanıcı profilleri dahil).<br># Güncelleme denetiminden ya da sonuç günlüğünün gönderiminden gelen<br># ağ hatası iletileri hedef URL'yi ve çözümlenen IP ya da proxy<br># adresini içerebilir. Okunamayan Windows Installer kayıtlarına dair<br># girdiler bir Windows hesabı SID'si (S-1-5-21-...) ve yüklü<br># yazılımın ürün kodlarını içerebilir.<br># Bu dosyayı herkese açık bir hata bildirimine eklemeden önce üç tür<br># bilgiyi de çıkarın.<br> |

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
| Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. | Gereksiz dosyaları hedef klasöre taşır. Hiçbir şeyin onlara ihtiyacı olmadığına kanaat getirdiğinizde o klasörü silin. |
| Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. | Gereksiz dosyaları bir hedef klasöre taşır. Klasörü hemen ardından seçeceksiniz. Hiçbir şeyin onlara ihtiyacı olmadığına kanaat getirdiğinizde o klasörü silin. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. | Gereksiz dosyaları hedef klasöre taşır. Klasör aynı sürücüde olduğu için, onu silene ya da başka bir sürücüye taşıyana kadar yeri geri kazanamazsınız. Bunu, hiçbir şeyin onlara ihtiyacı olmadığına kanaat getirdiğinizde yapabilirsiniz. |
| Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. | Gereksiz dosyaları kalıcı olarak siler. Güvenle kaldırılabilirler ve yeri hemen geri kazanırsınız. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Gömülü Authenticode sertifikasındaki konu adı. Zincir doğrulaması yapılmadı. |
| Change language. The program will restart. | Dili değiştir. Program yeniden başlatılacak. |

## Screen reader labels

| English | Türkçe |
| --- | --- |
| Donate | Bağış yap |
| Buy me a cuppa | Bana bir çay ısmarla |
| Cancel operation | İşlemi iptal et |
| Cancel scan | Taramayı iptal et |
| Cancel startup scan | Başlangıç taramasını iptal et |
| Close | Kapat |
| Close window | Pencereyi kapat |
| Close result and return to main window | Sonucu kapat ve ana pencereye dön |
| Leave a star on github | github'da yıldız bırak |
| Minimise | Simge durumuna küçült |
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Kalıcı olarak sil, gereksiz dosyaları kaldırır. İptal, hiçbir şey silmeden kapatır. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Taşı, gereksiz dosyaları seçilen hedef klasöre koyar. İptal, onları olduğu yerde bırakır. |
| Say thanks | Teşekkür etmek için |
| Send posts the report shown to No Faff. Cancel sends nothing. | Gönder, gösterilen raporu No Faff'a iletir. İptal hiçbir şey göndermez. |
| Check for updates | Güncelleştirmeleri denetle |
| Checks github's releases page for a newer version. | github üzerindeki sürümler sayfasında daha yeni bir sürüm olup olmadığını denetler. |
| Opens the readme on github in your browser. | github üzerindeki readme'yi tarayıcınızda açar. |
| Opens the issue tracker on github.com in your browser. | github.com üzerindeki sorun izleyiciyi (Issues) tarayıcınızda açar. |
| If ticked, InstallerClean checks github for a newer version when you run it. | İşaretliyse InstallerClean, çalıştırdığınızda github üzerinde daha yeni bir sürüm olup olmadığını denetler. |
| Open the release page to download the newer version, or cancel to keep the current version. | Daha yeni sürümü indirmek için sürüm sayfasını açın ya da geçerli sürümü korumak için iptal edin. |
| Opens the licence file on github.com in your browser. | github.com üzerindeki lisans dosyasını tarayıcınızda açar. |
| Backup folder | Hedef klasör |
| Patches | Yamalar |
| Product details | Ürün ayrıntıları |
| Backup folder | Hedef klasör |
| Operation progress | İşlem ilerlemesi |
| Scan {InstallerFolder} again | {InstallerFolder}'ı yeniden tara |
| Scanning progress | Tarama ilerlemesi |
| Startup scan progress | Başlangıç taraması ilerlemesi |
| Details, unneeded files | Ayrıntılar, gereksiz dosyalar |
| Available for cleanup. | Temizlik için uygun. |
| Details, files left alone | Ayrıntılar, olduğu gibi bırakılan dosyalar |
| Read-only inventory. | Salt okunur envanter. |
| Sorted by {0}, ascending | {0} ölçütüne göre artan sırada sıralandı |
| Sorted by {0}, descending | {0} ölçütüne göre azalan sırada sıralandı |
| Scan results | Tarama sonuçları |
| Result details | Sonuç ayrıntıları |
| File details | Dosya ayrıntıları |
| Product details | Ürün ayrıntıları |
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
| ,  | ,  |
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
| Error: unknown argument '{0}' | Hata: bilinmeyen argüman '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Hata: beklenmeyen fazladan argüman '{0}'. Taşıma klasörünüzün adında boşluk varsa tüm yolu tırnak içine alın: /m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | Hata: beklenmeyen fazladan argüman '{0}'. /s ve /d başka argüman almaz ve her çalıştırmada yalnızca bir bayrak kullanılabilir. |
| Cancelling... | İptal ediliyor... |
| Cancelled. | İptal edildi. |
| Error: unexpected failure ({0}). Details written to {1}. | Hata: beklenmeyen arıza ({0}). Ayrıntılar {1} konumuna yazıldı. |
| Error: unexpected failure ({0}). The crash log could not be written. | Hata: beklenmeyen arıza ({0}). Çökme günlüğü yazılamadı. |
| Scanning {InstallerFolder}... | {InstallerFolder} taranıyor... |
| Found {0} unneeded {1} to clean up ({2}). | Temizlenecek {0} gereksiz {1} bulundu ({2}). |
| Found no unneeded files. | Gereksiz dosya bulunamadı. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it held back the one file ({2}) it might otherwise have offered. | InstallerClean, önbellekteki dosyalardan hangisinin buradaki yüklü programlara ait olduğundan emin olamadı, bu yüzden sunabileceği tek dosyayı ({2}) tuttu. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it held back all {0} {1} ({2}) it might otherwise have offered. | InstallerClean, önbellekteki dosyalardan hangisinin buradaki yüklü programlara ait olduğundan emin olamadı, bu yüzden sunabileceği {0} {1} ({2}) tamamını tuttu. |
| Windows has a record for {0} file that is not in {InstallerFolder}: {1}. It causes no trouble day to day, but a repair, update or uninstall can fail on it. Running that program's installer again, preferably the same version, usually restores the file. | Windows'ta {InstallerFolder} içinde bulunmayan {0} dosya için kayıt var: {1}. Gündelik kullanımda sorun çıkarmaz, ama bir onarım, güncelleme ya da kaldırma işlemi bu yüzden başarısız olabilir. O programın yükleyicisini, tercihen aynı sürümü, yeniden çalıştırmak dosyayı genellikle geri getirir. |
| Windows has records for {0} files that are not in {InstallerFolder}: {1}. They cause no trouble day to day, but a repair, update or uninstall can fail on them. Running each program's installer again, preferably the same version, usually restores the files. | Windows'ta {InstallerFolder} içinde bulunmayan {0} dosya için kayıt var: {1}. Gündelik kullanımda sorun çıkarmazlar, ama bir onarım, güncelleme ya da kaldırma işlemi bu yüzden başarısız olabilir. Her programın yükleyicisini, tercihen aynı sürümü, yeniden çalıştırmak dosyaları genellikle geri getirir. |
| InstallerClean couldn't match up everything in the Windows records, so it didn't read all of them. What it found is unaffected, but what it says about files missing from {InstallerFolder} may be short of the full picture. Running it again may pick up more. | InstallerClean, Windows kayıtlarındaki her şeyi eşleştiremedi, bu yüzden hepsini okumadı. Bulduklarını bu etkilemez, ama {InstallerFolder} içinde eksik olan dosyalar hakkında söyledikleri tam tabloyu vermiyor olabilir. Yeniden çalıştırmak daha fazlasını bulabilir. |
| Deleting {0} unneeded {1}... | {0} gereksiz {1} siliniyor... |
| Permanently deleted {0} unneeded {1}. | {0} gereksiz {1} kalıcı olarak silindi. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Hata: taşıma hedefi belirtilmedi. /m YOL kullanın. (GUI'de ayarlanan bir varsayılan, kullanıcıya özeldir ve zamanlanmış ya da hizmet hesabı çalıştırmaları için geçerli değildir.) |
| Error: destination cannot be inside the Windows Installer folder. | Hata: hedef, Windows Installer klasörünün içinde olamaz. |
| Error: destination must be a fully qualified path. Got: {0} | Hata: hedef, tam nitelenmiş bir yol olmalıdır. Alınan: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Hata: {0} hedefi bir Windows sistem klasörünün altına çözümleniyor. %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% ve %ProgramData% dışında bir yol seçin. |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | Hata: {0} konumunda yeterli alan yok. Bu dosyaların taşınması {1} gerektiriyor, kullanılabilir alan {2}. Hiçbir şey taşınmadı. |
| Error: something is using Windows Installer right now, such as a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | Hata: şu anda bir şey Windows Installer'ı kullanıyor, örneğin bir Windows güncelleştirmesi ya da arka planda kurulan bir program. O sürerken /m ve /d engellenir. Bittiğinde yeniden deneyin. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Hata: bu makinede askıya alınmış önceki bir Windows Installer işlemi var. {InstallerFolder} klasörünü temizlemeden önce o kurulumu sürdürün ya da geri alın (veya Windows'u yeniden başlatın). |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | Hata: yeniden başlatma sonrasına sıraya alınmış bir dosya işlemi {InstallerFolder} klasörünü hedefliyor ({0}). Temizlemeden önce o işlemi tamamlamak için Windows'u yeniden başlatın. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Hata: Windows Installer'ın sürmekte olan bir işi var, bu yüzden /m ve /d engellendi. InstallerClean değişmekte olan {InstallerFolder} klasörüne dokunmayacak. Bittiğinde yeniden deneyin. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | Hata: InstallerClean, iki programın yüklü yazılımı aynı anda değiştirmesini engelleyen Windows Installer kilidini alamadı, bu yüzden bir dosyanın işin ortasında gerekli hale gelmeyeceğini kesinleştiremedi. Hiçbir şey silinmedi. Yeniden deneyin, sürerse Windows'u yeniden başlatın. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. Try again, and restart Windows if it keeps happening. | Hata: InstallerClean, iki programın yüklü yazılımı aynı anda değiştirmesini engelleyen Windows Installer kilidini alamadı, bu yüzden bir dosyanın işin ortasında gerekli hale gelmeyeceğini kesinleştiremedi. Hiçbir şey taşınmadı. Yeniden deneyin, sürerse Windows'u yeniden başlatın. |
| Moving {0} unneeded {1} to {2}... | {0} gereksiz {1} şuraya taşınıyor: {2}... |
| Moved {0} unneeded {1}. | {0} gereksiz {1} taşındı. |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then run the command again. | InstallerClean hedef klasörü artık doğrulayamadı, bu yüzden yanlış yere yazmak yerine durdu. {0} konumunu denetleyin, sonra komutu yeniden çalıştırın. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Başka bir InstallerClean işlemi tek örnek kilidini tutuyor (GUI ya da başka bir CLI çalıştırması). Çıkış 75 (geçici); daha sonra yeniden denemek güvenli. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Not: Olay Günlüğü'ne yazma başarısız oldu. Uygulama günlüğü izinlerini ya da Grup İlkesi'ni kontrol edin. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - {InstallerFolder} temizliği |
| Removes cached .msi and .msp files that no installed program still needs. | Yüklü hiçbir programın artık gerek duymadığı .msi/.msp dosyalarını siler. |
| Needs an elevated (administrator) prompt; Windows will not start it. | Yönetici komut istemi gerektirir; Windows aksi halde başlatmaz. |
| Usage: | Kullanım: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     Bu yardımı göster (/?, -h de kabul edilir) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  Sürümü yazdır (-v de kabul edilir) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         Yalnızca tara - gereksizleri listele |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         Gereksiz dosyaları kalıcı olarak sil |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         Kayıtlı hedef klasöre taşı |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m YOL     Belirtilen yola taşı |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli bitene kadar komut istemini tutar, böylece bir betik<br>ya da zamanlanmış görev onu bekleyebilir. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | Klasör kullanıcıya özel kaydedilir; zamanlanmış çalıştırma /m YOL ister. |
| Exit codes: | Çıkış kodları: |
|   0   success: the run did what it was asked and nothing failed |   0   başarılı: isteneni yaptı ve hiçbir şey başarısız olmadı |
|   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |   1   başarısız: hiçbir şey işlenmedi (hatalı argüman, hatalı hedef,<br>       başarısız tarama ya da her dosyanın başarısız olması) |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   kısmi: bir kısmı işlendi, bir kısmı işlenmedi (hata ya da Ctrl+C) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  geçici: geçici bir durum çalıştırmayı engelledi (iletiye bakın) |
|   130 cancelled (Ctrl+C) |   130 iptal edildi (Ctrl+C) |
