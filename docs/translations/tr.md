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
| The anonymous report | Anonim rapor |
| InstallerClean couldn't read the report. | InstallerClean raporu okuyamadı. |
| Third-party notices | Üçüncü taraf bildirimleri |

## Section headings

| English | Türkçe |
| --- | --- |
| PATCHES | YAMALAR |
| PRODUCT DETAILS | ÜRÜN AYRINTILARI |
| BACKUP FOLDER | YEDEK KLASÖRÜ |
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
| Carry on _without it | _Beklemeden devam et |
| Check for _updates | Güncelleştirmeleri _denetle |
| _Close | _Kapat |
| _Close without tipping | Bahşiş bırakmadan _kapat |
| _Delete permanently | _Kalıcı olarak sil |
| _Done | _Tamam |
| Details | Ayrıntılar |
| _Leave a tip | _Bahşiş bırak |
| Leave a tip | Bahşiş bırak |
| _Leave a tip | _Bahşiş bırak |
| Leave a _star on GitHub | GitHub'da _yıldız bırak |
| Apache 2.0 licence | Apache 2.0 lisansı |
| _Move | _Taşı |
| Path to folder if you move rather than delete. | Silmek yerine taşıyacaksanız klasörün yolu. |
| _Re-scan | _Yeniden tara |

## About window

| English | Türkçe |
| --- | --- |
| Guide and FAQ | Kılavuz ve SSS |
| What's new in this version | Bu sürümdeki yenilikler |
| Report a problem | Sorun bildir |
| Third-party notices | Üçüncü taraf bildirimleri |
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
| (no program) | (program yok) |
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
| Checking registry for additional packages... | Ek paketler için kayıt defteri denetleniyor... |
| Matching files against installed programs... | Dosyalar yüklü programlarla eşleştiriliyor... |
| {0} of {1} | {1} içinden {0} |
| Checking remaining files... | Kalan dosyalar denetleniyor... |
| Waiting for drive {0} to respond... | {0} sürücüsünden yanıt bekleniyor... |
| Waiting for {0} to respond... | {0} konumundan yanıt bekleniyor... |
| Any file InstallerClean can't check without it will be left alone. | InstallerClean'in o olmadan denetleyemediği her dosya olduğu gibi bırakılacak. |
| Scan complete ({0}) | Tarama tamamlandı ({0}) |
| Found {0} {1} you can safely delete. | Güvenle silebileceğiniz {0} {1} bulundu. |
| Preparing destination folder... | Hedef klasör hazırlanıyor... |
| Moving unneeded files... | Gereksiz dosyalar taşınıyor... |
| Deleting unneeded files... | Gereksiz dosyalar siliniyor... |
| Waiting for Windows Installer to finish... | Waiting for Windows Installer to finish... |
| Waiting for an installation that has not finished. If nothing is being installed, cancel and restart Windows. | Waiting for an installation that has not finished. If nothing is being installed, cancel and restart Windows. |
| Checking the remaining files again... | Checking the remaining files again... |
| Move cancelled. {0} of {1} {2} processed. | Taşıma iptal edildi. {1} {2} içinden {0} tanesi işlendi. |
| Delete cancelled. {0} of {1} {2} processed. | Silme iptal edildi. {1} {2} içinden {0} tanesi işlendi. |
| {0}. Details are in {1}. | {0}. Ayrıntılar {1} içinde. |
| {0}. The crash log could not be written. | {0}. Çökme günlüğü yazılamadı. |
| {0}. Details are in {1}. | {0}. Ayrıntılar {1} içinde. |
| {0}. The crash log could not be written. | {0}. Çökme günlüğü yazılamadı. |
| Access denied. Windows refused the scan. | Erişim reddedildi. Windows taramayı reddetti. |
| Scan stopped. | Tarama durduruldu. |
| Scan cancelled. | Tarama iptal edildi. |
| Ready | Hazır |
| Scan failed ({0}). Details in {1}. | Tarama başarısız oldu ({0}). Ayrıntılar {1} içinde. |
| Scan failed ({0}). The crash log could not be written. | Tarama başarısız oldu ({0}). Çökme günlüğü yazılamadı. |

## Main screen text

| English | Türkçe |
| --- | --- |
| Any unneeded files below are safe to delete. | Aşağıdaki gereksiz dosyaların hepsi güvenle silinebilir. |
| They sit in {InstallerFolder}. InstallerClean asks Windows about every installed program: a file is listed as safe to delete when no program claims it ({0}), or when a newer patch has replaced it and no program could roll back to it ({1}). | Bunlar {InstallerFolder} içinde duruyor. InstallerClean, yüklü her programı Windows'a sorar: bir dosya, hiçbir program onu sahiplenmediğinde ({0}) ya da daha yeni bir yama onun yerine geçtiğinde ve hiçbir program ona geri dönemeyecek durumdayken ({1}) silinmesi güvenli dosyalar arasında listelenir. |
| Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update and uninstall as normal. Putting them back into {InstallerFolder} restores everything. Or delete them permanently now. | Onları seçeceğiniz bir yedek klasörüne taşıyın, sonra programlarınızın hâlâ normal şekilde güncellendiğine ve kaldırıldığına kanaat getirdiğinizde o klasörü silin. Onları {InstallerFolder} içine geri koymak her şeyi eski haline getirir. Ya da şimdi kalıcı olarak silin. |
| Nothing scanned yet. | Henüz tarama yapılmadı. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Hiçbir programın hâlâ ihtiyaç duymadığı yükleyici dosyaları için {InstallerFolder} klasörüne bakmak üzere Yeniden tara'ya basın. |
| Nothing to show until the next scan. | Bir sonraki taramaya kadar gösterilecek bir şey yok. |
| The scan that runs after a Move or Delete didn't finish, so there's no list to show. Press Re-scan to scan {InstallerFolder} again. | Taşı veya Sil'den sonra çalışan tarama tamamlanmadı, bu yüzden gösterilecek bir liste yok. {InstallerFolder} klasörünü yeniden taramak için Yeniden tara'ya basın. |
| What stopped it is recorded in {0}. | Taramayı durduran neden {0} içine kaydedildi. |
| These files can't be cleaned up right now. | Bu dosyalar şu anda temizlenemez. |
| Something is using Windows Installer right now, such as Windows Update or a program being installed or removed. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} until it has finished. Then Re-scan and they come back. | Şu anda bir şey Windows Installer'ı kullanıyor, örneğin Windows Update ya da kurulmakta veya kaldırılmakta olan bir program. O sürerken Taşı ve Sil duraklatılır, böylece InstallerClean o bitene kadar {InstallerFolder} klasörüne dokunmaz. Sonra yeniden tarayın, ikisi de geri gelir. |
| Windows refused InstallerClean permission to check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows. InstallerClean will check again the next time you start it. If Windows still refuses, this isn't a machine InstallerClean can clean. | Windows, InstallerClean'e Windows Installer'ın meşgul olup olmadığını denetleme izni vermedi, bu yüzden Taşı ve Sil duraklatıldı ve InstallerClean {InstallerFolder} klasörüne dokunmayacak. Windows'u yeniden başlatın. InstallerClean, bir dahaki sefere açtığınızda yeniden denetleyecek. Windows yine reddederse, burası InstallerClean'in temizleyebileceği bir makine değil. |
| InstallerClean couldn't check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows. InstallerClean will check again the next time you start it. If it still can't check, this isn't a machine InstallerClean can clean. | InstallerClean, Windows Installer'ın meşgul olup olmadığını denetleyemedi, bu yüzden Taşı ve Sil duraklatıldı ve InstallerClean {InstallerFolder} klasörüne dokunmayacak. Windows'u yeniden başlatın. InstallerClean, bir dahaki sefere açtığınızda yeniden denetleyecek. Yine denetleyemezse, burası InstallerClean'in temizleyebileceği bir makine değil. |
| InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}). Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows. InstallerClean will check again the next time you start it. If it still can't check, this isn't a machine InstallerClean can clean. | InstallerClean, Windows Installer'ın meşgul olup olmadığını denetleyemedi, çünkü Windows bir hata verdi: {0} (hata {1}). Taşı ve Sil duraklatıldı ve InstallerClean {InstallerFolder} klasörüne dokunmayacak. Windows'u yeniden başlatın. InstallerClean, bir dahaki sefere açtığınızda yeniden denetleyecek. Yine denetleyemezse, burası InstallerClean'in temizleyebileceği bir makine değil. |
| Windows Installer is still running, as it does for a few minutes after a program has been installed, updated or removed, and for as long as an installer's window is left open. Move and Delete are paused until it has stopped, so InstallerClean won't touch {InstallerFolder}. Then Re-scan and they come back. | Windows Installer is still running, as it does for a few minutes after a program has been installed, updated or removed, and for as long as an installer's window is left open. Move and Delete are paused until it has stopped, so InstallerClean won't touch {InstallerFolder}. Then Re-scan and they come back. |
| An earlier Windows Installer installation hasn't finished, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows. An installation waiting for a restart carries on after it, and InstallerClean will check again the next time you start it. If this message comes back, run the setup of the program that was being installed or removed again, and Windows Installer will offer to undo the installation it didn't finish. | Windows Installer önceki bir kurulumu tamamlamadı, bu yüzden Taşı ve Sil duraklatıldı ve InstallerClean {InstallerFolder} klasörüne dokunmayacak. Windows'u yeniden başlatın. Yeniden başlatmayı bekleyen bir kurulum, yeniden başlatmadan sonra devam eder ve InstallerClean, bir dahaki sefere açtığınızda yeniden denetleyecek. Bu ileti yeniden görünürse, kurulmakta ya da kaldırılmakta olan programın kurulum programını tekrar çalıştırın, Windows Installer da tamamlamadığı kurulumun yaptığı değişiklikleri geri almayı önerecektir. |
| Windows has a change to {InstallerFolder} waiting for the next restart, so Move and Delete are paused. Restart Windows. InstallerClean will check again the next time you start it. | Windows'un bir sonraki yeniden başlatmada {InstallerFolder} klasörüne uygulamak üzere beklettiği bir değişiklik var, bu yüzden Taşı ve Sil duraklatıldı. Windows'u yeniden başlatın. InstallerClean, bir dahaki sefere açtığınızda yeniden denetleyecek. |
| Windows has a file change waiting for the next restart, and InstallerClean can't tell which files it affects, so it can't rule out that they're in {InstallerFolder}. Move and Delete are paused. Restart Windows. InstallerClean will check again the next time you start it. | Windows'un bir sonraki yeniden başlatmayı bekleyen bir dosya değişikliği var ve InstallerClean bunun hangi dosyaları etkilediğini bilemiyor, bu yüzden bunların {InstallerFolder} içinde olmadığını dışlayamıyor. Taşı ve Sil duraklatıldı. Windows'u yeniden başlatın. InstallerClean, bir dahaki sefere açtığınızda yeniden denetleyecek. |
| InstallerClean couldn't read one of the Windows settings it checks before touching {InstallerFolder}, so it can't tell whether Windows Installer is busy or has work waiting for a restart. Move and Delete are paused. Restart Windows. InstallerClean will check again the next time you start it. If the setting still won't read, this isn't a machine InstallerClean can clean. | InstallerClean, {InstallerFolder} klasörüne dokunmadan önce denetlediği Windows ayarlarından birini okuyamadı, bu yüzden Windows Installer'ın meşgul olup olmadığını ya da yeniden başlatma bekleyen bir işi olup olmadığını bilemiyor. Taşı ve Sil duraklatıldı. Windows'u yeniden başlatın. InstallerClean, bir dahaki sefere açtığınızda yeniden denetleyecek. Ayar yine okunamıyorsa, burası InstallerClean'in temizleyebileceği bir makine değil. |
| Windows Installer has something unfinished, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Once it has cleared, Re-scan and they come back. | Windows Installer'ın bitmemiş bir işi var, bu yüzden Taşı ve Sil duraklatıldı ve InstallerClean {InstallerFolder} klasörüne dokunmayacak. O sonuçlandığında yeniden tarayın, ikisi de geri gelir. |
| Select a file to view details. | Ayrıntıları görmek için bir dosya seçin. |
| Select a product to view details. | Ayrıntıları görmek için bir ürün seçin. |
| No metadata available. | Kullanılabilir meta veri yok. |
| This installer file is missing. It causes no trouble now, and won't until the day you try to update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To put it back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Bu kurulum dosyası eksik. Şu anda bir sorun çıkarmıyor ve ait olduğu programı güncellemeye ya da kaldırmaya çalıştığınız güne kadar da çıkarmayacak. O adım o zaman başarısız olabilir, çünkü Windows bu dosyayı arar ve bulamaz.<br><br>Geri koymak için, halihazırda sahip olduğunuz sürümün kurulum programına ihtiyacınız var. Onu programın üreticisinden edinin ve mevcut kopyanızın üzerine çalıştırın. Daha yeni bir sürüm işe yaramaz: önce sizdekini kaldırması gerekir ve bu dosyaya ihtiyaç duyan tam da o adımdır. Önce kaldırmak da aynı nedenle işe yaramaz. Bunun dosyayı geri getirmesi ve ayarlarınıza dokunmaması beklenir, ancak Microsoft bunu garanti etmez. |
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
| Nothing to clean up in {InstallerFolder} | {InstallerFolder} içinde temizlenecek bir şey yok |
| Scanned {0} {1} in {2} | {2} içinde {0} {1} tarandı |
| No files can be deleted | Hiçbir dosya silinemez |
| One file ({2}) was left alone because Windows shows it changed after the scan started. If something was installing or updating, scan again once it has finished. | Bir dosya ({2}), Windows'a göre tarama başladıktan sonra değiştiği için olduğu gibi bırakıldı. Bir şey kuruluyor ya da güncelleniyorduysa, bittiğinde yeniden tarayın. |
| {0} {1} ({2}) were left alone because Windows shows they changed after the scan started. If something was installing or updating, scan again once it has finished. | {0} {1} ({2}), Windows'a göre tarama başladıktan sonra değiştikleri için olduğu gibi bırakıldı. Bir şey kuruluyor ya da güncelleniyorduysa, bittiğinde yeniden tarayın. |
| Delete that folder when you're satisfied all is well. | Her şeyin yolunda olduğuna kanaat getirdiğinizde o klasörü silin. |
| Delete that folder when you're satisfied all is well. You won't actually reclaim the space until you do. | Her şeyin yolunda olduğuna kanaat getirdiğinizde o klasörü silin. Yeri ancak o zaman gerçekten geri kazanırsınız. |
| {0} freed | {0} yer açıldı |
| {0} moved | {0} taşındı |
| Nothing was moved | Hiçbir dosya taşınmadı |
| Nothing was deleted | Hiçbir dosya silinmedi |
| {0} file could not be moved. | {0} dosya taşınamadı. |
| {0} files could not be moved. | {0} dosya taşınamadı. |
| {0} file could not be deleted. | {0} dosya silinemedi. |
| {0} files could not be deleted. | {0} dosya silinemedi. |
| {0} {1} moved to: {2} | {0} {1} şu konuma taşındı: {2} |
| {0} {1} moved to: {2} | {0} {1} şu konuma taşındı: {2} |
| {0} file left alone. The scan listed it as safe to delete, but the final check before moving or deleting couldn't confirm it. | {0} dosya olduğu gibi bırakıldı. Tarama onu silinmesi güvenli dosyalar arasında listeledi, ancak taşıma ya da silme öncesindeki nihai denetim bunu doğrulayamadı. |
| {0} files left alone. The scan listed them as safe to delete, but the final check before moving or deleting couldn't confirm it. | {0} dosya olduğu gibi bırakıldı. Tarama bunları silinmesi güvenli dosyalar arasında listeledi, ancak taşıma ya da silme öncesindeki nihai denetim bunu doğrulayamadı. |
| Moved {0} of {1} {2} to {3} before you cancelled. | İptal etmeden önce {1} {2} içinden {0} tanesi {3} konumuna taşındı. |
| Permanently deleted {0} of {1} {2} before you cancelled. | İptal etmeden önce {1} {2} içinden {0} tanesi kalıcı olarak silindi. |
| It's simple to undo. Move them back into {InstallerFolder} and everything will be back to how it was. | Geri almak kolay. Onları {InstallerFolder} klasörüne geri taşıyın, her şey eskisi gibi olur. |
| {0} {1} permanently deleted | {0} {1} kalıcı olarak silindi |
| {0} {1} permanently deleted | {0} {1} kalıcı olarak silindi |
| Any tip is much appreciated | Her bahşiş çok makbule geçer |
| Send _anonymous report | _Anonim rapor gönder |
| The report adds your result to [this chart] on GitHub, which shows how much space people are reclaiming with InstallerClean. It also holds these technical details, to help me spot any bugs: | Rapor, sonucunuzu GitHub'daki [bu grafiğe] ekler; bu grafik, insanların InstallerClean ile ne kadar yer açtığını gösterir. Ayrıca hataları fark etmeme yardımcı olacak şu teknik ayrıntıları da içerir: |
| How much space was freed, and how many files were moved or deleted | Ne kadar yer açıldığı ve kaç dosyanın taşındığı ya da silindiği |
| Whether a Move went to the same drive, another drive, a removable drive or a network share | Taşı işleminin aynı sürücüye mi, başka bir sürücüye mi, çıkarılabilir bir sürücüye mi yoksa bir ağ paylaşımına mı yapıldığı |
| How long the scan and the Move or Delete took | Taramanın ve Taşı ya da Sil işleminin ne kadar sürdüğü |
| How many installer files there are of each kind, and how much space they take | Her türden kaç yükleyici dosyası olduğu ve ne kadar yer kapladıkları |
| How many files were left alone, and why | Kaç dosyanın olduğu gibi bırakıldığı ve neden |
| How often InstallerClean waited for a slow drive or network share, and how many it stopped waiting for | InstallerClean'in yavaş bir sürücüyü veya ağ paylaşımını ne sıklıkla beklediği ve kaçını beklemeyi kestiği |
| How many programs and patches Windows has a record of | Windows'ta kaydı bulunan program ve yama sayısı |
| Whether Windows still makes old-style short file names | Windows'un hâlâ eski tarz kısa dosya adları oluşturup oluşturmadığı |
| Whether Windows is 10 or 11 and x64 or Arm, and which language and country or region it's set to | Windows'un 10 mu 11 mi, x64 mü Arm mı olduğu ve hangi dile ve ülke veya bölgeye ayarlandığı |
| InstallerClean's version, how you got it (setup, portable, Scoop, the command-line tool on its own, built from source, or not known), whether the run was in InstallerClean or the command-line tool, and the language it was showing | InstallerClean'in sürümü, nasıl edinildiği (setup, taşınabilir, Scoop, tek başına komut satırı aracı, kaynaktan derlenmiş veya bilinmiyor), çalıştırmanın InstallerClean'de mi yoksa komut satırı aracında mı yapıldığı ve gösterildiği dil |
| Counts of anything that went wrong | Ters giden her şeyin sayısı |
| A random code made for this report, so that it's only counted once if it's sent more than once | Rapor birden fazla kez gönderilirse yalnızca bir kez sayılması için bu rapora özel oluşturulan rastgele bir kod |
| Apart from that code, each of these is a number or a label. Nothing in the report names you, your PC, your files or your programs, and I never see your IP address. | Bu kod dışında, bunların her biri bir sayı ya da etikettir. Raporda hiçbir şey sizi, bilgisayarınızı, dosyalarınızı veya programlarınızı tanımlamaz ve IP adresinizi asla görmem. |
| See exactly what's sent | Tam olarak neyin gönderildiğini gör |

## Summaries and counts

| English | Türkçe |
| --- | --- |
| {0} file left alone | {0} dosya olduğu gibi bırakıldı |
| {0} files left alone | {0} dosya olduğu gibi bırakıldı |
| {0} unneeded file to clean up | temizlenecek {0} gereksiz dosya |
| {0} unneeded files to clean up | temizlenecek {0} gereksiz dosya |
| Windows has a record for {0} file that isn't in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. Open Details for what to do. | Windows'ta {InstallerFolder} içinde bulunmayan {0} dosyaya ait bir kayıt var: {1}. Günlük kullanımda sorun çıkarmaz, ama o programın güncellenmesi ya da kaldırılması başarısız olabilir. Ne yapılacağını öğrenmek için Ayrıntılar'ı açın. |
| Windows has records for {0} files that aren't in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. Open Details for what to do. | Windows'ta {InstallerFolder} içinde bulunmayan {0} dosyaya ait kayıtlar var: {1}. Günlük kullanımda sorun çıkarmaz, ama o programların güncellenmesi ya da kaldırılması başarısız olabilir. Ne yapılacağını öğrenmek için Ayrıntılar'ı açın. |
| {0} other program | {0} program daha |
| {0} other programs | {0} program daha |
| {0} file with no program named in the records | kayıtlarda hiçbir program adı geçmeyen {0} dosya |
| {0} files with no program named in the records | kayıtlarda hiçbir program adı geçmeyen {0} dosya |
| InstallerClean stopped waiting for drive {0}, so it left alone any file it couldn't check without it. Re-scan once it's responding normally. | InstallerClean {0} sürücüsünü beklemeyi kesti, bu yüzden o sürücü olmadan denetleyemediği her dosyayı olduğu gibi bıraktı. Yeniden normal yanıt vermeye başladığında Yeniden tara'ya basın. |
| InstallerClean stopped waiting for {0}, so it left alone any file it couldn't check without it. Re-scan once it's responding normally. | InstallerClean {0} konumunu beklemeyi kesti, bu yüzden o konum olmadan denetleyemediği her dosyayı olduğu gibi bıraktı. Yeniden normal yanıt vermeye başladığında Yeniden tara'ya basın. |
| InstallerClean stopped waiting for more than one drive or share ({0}), so it left alone any file it couldn't check without them. Re-scan once they're responding normally. | InstallerClean birden fazla sürücüyü veya paylaşımı ({0}) beklemeyi kesti, bu yüzden bunlar olmadan denetleyemediği her dosyayı olduğu gibi bıraktı. Yeniden normal yanıt vermeye başladıklarında Yeniden tara'ya basın. |
| Windows Installer was busy when the scan started, for example with Windows Update or a program being installed or removed, so InstallerClean left alone the files it can't check while it's busy. Re-scan once it has finished. | Tarama başladığında Windows Installer meşguldü, örneğin Windows Update ya da kurulmakta veya kaldırılmakta olan bir programla, bu yüzden InstallerClean, Windows Installer meşgulken denetleyemediği dosyaları olduğu gibi bıraktı. O bittiğinde Yeniden tara'ya basın. |
| An earlier Windows Installer installation hadn't finished when the scan started, so InstallerClean left alone the files it can't check until it has. Restart Windows. An installation waiting for a restart carries on after it, and InstallerClean will check again the next time you start it. If this message comes back, run the setup of the program that was being installed or removed again, and Windows Installer will offer to undo the installation it didn't finish. | Tarama başladığında Windows Installer önceki bir kurulumu henüz tamamlamamıştı, bu yüzden InstallerClean, o kurulum tamamlanana kadar denetleyemediği dosyaları olduğu gibi bıraktı. Windows'u yeniden başlatın. Yeniden başlatmayı bekleyen bir kurulum, yeniden başlatmadan sonra devam eder ve InstallerClean, bir dahaki sefere açtığınızda yeniden denetleyecek. Bu ileti yeniden görünürse, kurulmakta ya da kaldırılmakta olan programın kurulum programını tekrar çalıştırın, Windows Installer da tamamlamadığı kurulumun yaptığı değişiklikleri geri almayı önerecektir. |
| Windows had a change to {InstallerFolder} waiting for the next restart when the scan started, so InstallerClean left alone the files it can't check until that change is made. Restart Windows. InstallerClean will check again the next time you start it. | Tarama başladığında Windows'un bir sonraki yeniden başlatmada {InstallerFolder} klasörüne uygulamak üzere beklettiği bir değişiklik vardı, bu yüzden InstallerClean, bu değişiklik uygulanana kadar denetleyemediği dosyaları olduğu gibi bıraktı. Windows'u yeniden başlatın. InstallerClean, bir dahaki sefere açtığınızda yeniden denetleyecek. |
| Windows had a file change waiting for the next restart when the scan started, and InstallerClean couldn't tell which files it affects, so it left alone the files it can't check until that change is made. Restart Windows. InstallerClean will check again the next time you start it. | Tarama başladığında Windows'un bir sonraki yeniden başlatmayı bekleyen bir dosya değişikliği vardı ve InstallerClean bunun hangi dosyaları etkilediğini bilemedi, bu yüzden bu değişiklik uygulanana kadar denetleyemediği dosyaları olduğu gibi bıraktı. Windows'u yeniden başlatın. InstallerClean, bir dahaki sefere açtığınızda yeniden denetleyecek. |
| Windows refused InstallerClean permission to check whether Windows Installer was busy when the scan started, so it left alone the files it can't check while Windows Installer is busy. Restart Windows. InstallerClean will check again the next time you start it. | Tarama başladığında Windows, InstallerClean'e Windows Installer'ın meşgul olup olmadığını denetleme izni vermedi, bu yüzden InstallerClean, Windows Installer meşgulken denetleyemediği dosyaları olduğu gibi bıraktı. Windows'u yeniden başlatın. InstallerClean, bir dahaki sefere açtığınızda yeniden denetleyecek. |
| InstallerClean couldn't read a Windows setting it checks, so it couldn't tell whether Windows Installer was busy or had work waiting for a restart when the scan started. It left alone the files it can't check while Windows Installer is busy. Restart Windows. InstallerClean will check again the next time you start it. | Tarama başladığında InstallerClean denetlediği bir Windows ayarını okuyamadı, bu yüzden Windows Installer'ın meşgul olup olmadığını ya da yeniden başlatma bekleyen bir işi olup olmadığını anlayamadı. InstallerClean, Windows Installer meşgulken denetleyemediği dosyaları olduğu gibi bıraktı. Windows'u yeniden başlatın. InstallerClean, bir dahaki sefere açtığınızda yeniden denetleyecek. |
| InstallerClean couldn't check whether Windows Installer was busy when the scan started, so it left alone the files it can't check while Windows Installer is busy. Restart Windows. InstallerClean will check again the next time you start it. | Tarama başladığında InstallerClean, Windows Installer'ın meşgul olup olmadığını denetleyemedi, bu yüzden Windows Installer meşgulken denetleyemediği dosyaları olduğu gibi bıraktı. Windows'u yeniden başlatın. InstallerClean, bir dahaki sefere açtığınızda yeniden denetleyecek. |
| InstallerClean couldn't check whether Windows Installer was busy when the scan started. Windows gave an error: {0} (error {1}). It left alone the files it can't check while Windows Installer is busy. Restart Windows. InstallerClean will check again the next time you start it. | Tarama başladığında InstallerClean, Windows Installer'ın meşgul olup olmadığını denetleyemedi. Windows bir hata verdi: {0} (hata {1}). InstallerClean, Windows Installer meşgulken denetleyemediği dosyaları olduğu gibi bıraktı. Windows'u yeniden başlatın. InstallerClean, bir dahaki sefere açtığınızda yeniden denetleyecek. |
| Windows Installer had something unfinished when the scan started, so InstallerClean left alone the files it can't check until it has finished. Re-scan once it has finished. | Tarama başladığında Windows Installer'ın bitmemiş bir işi vardı, bu yüzden InstallerClean, o iş bitene kadar denetleyemediği dosyaları olduğu gibi bıraktı. O bittiğinde Yeniden tara'ya basın. |
| This PC's clock went back or lost time while the scan was running, so InstallerClean couldn't tell which files had just been added, and left alone every file that could have been. Re-scan. | Tarama sürerken bu bilgisayarın saati geri gitti ya da geri kaldı, bu yüzden InstallerClean hangi dosyaların az önce eklendiğini bilemedi ve bunlardan biri olabilecek her dosyayı olduğu gibi bıraktı. Yeniden tara'ya basın. |
| {0} of {1} {2} | {1} {2} içinden {0} |
| {0} unneeded {1} ({2}) | {0} gereksiz {1} ({2}) |
| {0} file left alone ({1}) | {0} dosya olduğu gibi bırakıldı ({1}) |
| {0} files left alone ({1}) | {0} dosya olduğu gibi bırakıldı ({1}) |
| {0} missing | {0} eksik |
| {0} missing | {0} eksik |

## Confirmation dialogs

| English | Türkçe |
| --- | --- |
| Move {0} {1} ({2})? | {0} {1} ({2}) taşınsın mı? |
| This file will be moved to: | Bu dosya şuraya taşınacak: |
| These files will be moved to: | Bu dosyalar şuraya taşınacak: |
| Delete {0} {1} ({2})? | {0} {1} ({2}) silinsin mi? |
| This file will be deleted permanently. It's safe to do but if you'd like a backup, use Move instead. | Bu dosya kalıcı olarak silinecek. Bunu yapmak güvenlidir, ama bir yedek isterseniz onun yerine Taşı'yı kullanın. |
| These files will be deleted permanently. It's safe to do but if you'd like a backup, use Move instead. | Bu dosyalar kalıcı olarak silinecek. Bunu yapmak güvenlidir, ama bir yedek isterseniz onun yerine Taşı'yı kullanın. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | O klasör aynı sürücüde, bu yüzden siz onu silene kadar yer geri gelmez. Yeri hemen istiyorsanız başka bir sürücüde bir klasör seçin. |

## Error messages

| English | Türkçe |
| --- | --- |
| This is also recorded in {0}. | Bu ayrıca {0} içine kaydedilir. |
| Access denied | Erişim reddedildi |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows, InstallerClean'in erişimini reddetti, bu yüzden işlem durduruldu. Hiçbir şey kaldırılmadı.<br><br>InstallerClean zaten yönetici olarak çalışıyordu, dolayısıyla onu yeniden öyle başlatmak işe yaramaz. Windows erişimi neyin reddettiği konusunda başka bir şey söylemiyor, bu yüzden denenecek belirli bir şey yok. |
| InstallerClean stopped | InstallerClean durdu |
| Scan failed | Tarama başarısız |
| Nothing has been removed. | Hiçbir şey kaldırılmadı. |
| No file has been listed as safe to delete and nothing has been removed. | Hiçbir dosya silinmesi güvenli dosyalar arasında listelenmedi ve hiçbir şey kaldırılmadı. |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. | Windows Installer kayıtları tamamen boş döndü: tek bir yüklü program ya da güncelleştirme bile önbellekteki bir kurulum dosyasında hak iddia etmiyor. Çalışan bir makinede bu olmaz (yeni kurulmuş bir Windows'ta bile bunlardan vardır), yani kayıtlar ya bozuk ya da okunamadı, ve bu yanıta inanan bir tarama {InstallerFolder} içindeki her dosyayı yanlışlıkla sahipsiz sayardı. InstallerClean bunun yerine durdu. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. | Windows Installer, InstallerClean'in yüklü olanları listelemesine izin vermedi. InstallerClean zaten yönetici olarak çalışıyordu, dolayısıyla onu yeniden yönetici olarak çalıştırmak bir şey değiştirmez. Bu liste olmadan önbellekteki hangi dosyaların hâlâ gerekli olduğunu güvenle söylemenin yolu yok, bu yüzden InstallerClean durdu. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: it read {1} {2}, then an entry came back unreadable (error code {0}). Rather than work from a part-read list, InstallerClean stopped. | Windows Installer, InstallerClean'e yüklü programların okunabilir bir listesini veremedi: {1} {2} okudu, sonra bir kayıt okunamaz döndü (hata kodu {0}). InstallerClean, yalnızca kısmen okunmuş bir listeyle çalışmak yerine durdu. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. | Windows Installer, yüklü programlar listesinin sonunu hiç bildirmedi: InstallerClean {2} {3} okudu, sonra {0} kayıttan sonra vazgeçti (son hata kodu {1}). Sonu gelmeyen bir listeye güvenilemez, bu yüzden InstallerClean durdu. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. | Windows Installer, bir programın yama listesinin sonunu hiç bildirmedi: InstallerClean {2} {3} okudu, sonra {0} kayıttan sonra vazgeçti (son hata kodu {1}). Sonu gelmeyen bir listeye güvenilemez, bu yüzden InstallerClean durdu. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. | InstallerClean, Windows Installer kayıtlarını {InstallerFolder} içeriğiyle eşleştiremedi. Kayıtların işaret ettiklerinin neredeyse hiçbiri orada değil ve orada olanların neredeyse hiçbirinin adı hiçbir kayıtta geçmiyor, bu yüzden hiçbir dosyanın gereksiz olduğu gösterilemedi. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. | InstallerClean, Windows Installer kayıtlarını {InstallerFolder} içeriğiyle eşleştiremedi. Klasörde dosyalar var, ama tek bir kayıt bile içindeki hiçbir şeye işaret etmiyor, bu yüzden hiçbir dosyanın gereksiz olduğu gösterilemedi. |
| InstallerClean can't scan {InstallerFolder} on this PC. Windows reports that folder as {0}, and InstallerClean only scans an Installer folder that Windows reports at its own path. | InstallerClean bu bilgisayarda {InstallerFolder} klasörünü tarayamıyor. Windows bu klasörü {0} olarak bildiriyor ve InstallerClean yalnızca, Windows'un klasörün kendi yolunda bildirdiği bir Installer klasörünü tarar. |
| Windows reports that {InstallerFolder} doesn't exist on this PC, so InstallerClean stopped. | Windows, {InstallerFolder} klasörünün bu bilgisayarda bulunmadığını bildiriyor, bu yüzden InstallerClean durdu. |
| Windows reported an error when InstallerClean tried to list the files in {InstallerFolder}, so it couldn't get a complete list and stopped. | InstallerClean, {InstallerFolder} klasöründeki dosyaları listelemeye çalışırken Windows bir hata bildirdi, bu yüzden InstallerClean tam bir liste alamadı ve durdu. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. | InstallerClean, neyin hâlâ gerekli olduğundan emin olmaya yetecek kadar Windows Installer kaydını okuyamadı: yüklü programların listesi eksik döndü, aynı kayıtları doğrudan kayıt defterinden okumak da hatalarla karşılaştı. Bir dosya, yalnızca onu adlandıran kayıt okunamayanlardan biri olduğu için sahipsiz görünebilirdi, bu yüzden InstallerClean durdu. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was listed as safe to delete. This scan found nothing because that check failed, not because the folder is clean. | InstallerClean, {InstallerFolder} klasörünün gerçek yolunu Windows'a çözdüremedi, bu yüzden hiçbir dosyanın onun içinde olduğu gösterilemedi ve hiçbiri silinmesi güvenli dosyalar arasında listelenmedi. Bu tarama, klasör temiz olduğu için değil, o denetim başarısız olduğu için hiçbir şey bulamadı. |
| Invalid destination | Geçersiz hedef |
| Move stopped | Taşıma durduruldu |
| Delete stopped | Delete stopped |
| InstallerClean stopped because Windows Installer's records kept changing while it checked the files, which happens while programs are being installed or updated. Files already moved or deleted stay so. The rest were not touched. | InstallerClean stopped because Windows Installer's records kept changing while it checked the files, which happens while programs are being installed or updated. Files already moved or deleted stay so. The rest were not touched. |
| InstallerClean stopped because Windows would not let it watch the registry keys where Windows Installer keeps its records, so it cannot tell whether they change while it works. Files already moved or deleted stay so. The rest were not touched. | InstallerClean stopped because Windows would not let it watch the registry keys where Windows Installer keeps its records, so it cannot tell whether they change while it works. Files already moved or deleted stay so. The rest were not touched. |
| InstallerClean could not make its own folder, {0}, inside the Installer folder (Windows error {1}), so it did not move or delete anything. | InstallerClean could not make its own folder, {0}, inside the Installer folder (Windows error {1}), so it did not move or delete anything. |
| Couldn't use that backup folder | Bu yedek klasörü kullanılamadı |
| Move failed | Taşıma başarısız |
| Delete failed | Silme başarısız |
| Setting not saved | Ayar kaydedilmedi |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | Değişiklik kaydedilemedi. Bir sonraki açılışta InstallerClean önceki ayara dönecek. |
| The destination cannot be inside the Windows Installer folder. | Hedef, Windows Installer klasörünün içinde olamaz. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | {0} hedefi bir Windows sistem klasörünün altına çözümleniyor. %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% ve %ProgramData% dışında bir yol seçin. |
| Not enough space | Yetersiz alan |
| There isn't room at {0}<br><br>Required: {1}<br>Available: {2} | {0} konumunda yeterince yer yok<br><br>Gerekli: {1}<br>Kullanılabilir: {2} |
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
| This file could not be put back in its place. It is whole, in {0}. | This file could not be put back in its place. It is whole, in {0}. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Dosyaların Windows Installer klasörüne taşınması reddediliyor (hedef: {0}). |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | Yedek klasörü, bir sürücü harfi ya da ağ paylaşımıyla başlayan, bir klasöre giden tam bir yol olmalıdır (örneğin D:\Backup ya da \\sunucu\backup). InstallerClean bunu kullanamaz: {0} |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then Re-scan and try again. | InstallerClean yedek klasörünü artık doğrulayamadı, bu yüzden durdu. {0} konumunu denetleyin, sonra Yeniden tara deyip tekrar deneyin. |
| Cannot write to {0}. | {0} konumuna yazılamıyor. |
| You can save files in {0} but not open them again, so InstallerClean won't move files there. | {0} konumuna dosya kaydedebilirsiniz ama onları yeniden açamazsınız, bu yüzden InstallerClean oraya dosya taşımaz. |
| A file called '{0}' is already in the backup folder. | '{0}' adlı bir dosya yedek klasöründe zaten var. |

## Update check

| English | Türkçe |
| --- | --- |
| Check for updates | Güncelleştirmeleri denetle |
| Checking... | Denetleniyor... |
| Up to date. | Güncel. |
| InstallerClean {0} is available. You have {1}. | InstallerClean {0} sürümü mevcut. Sizdeki sürüm {1}. |
| It's strongly recommended to use the latest version. | En son sürümü kullanmanız önemle tavsiye edilir. |
| Download it from GitHub | GitHub'dan indirin |
| Download .NET Framework 4.8 from Microsoft | .NET Framework 4.8'i Microsoft'tan indirin |
| Update now | Şimdi güncelle |
| This copy was installed by Scoop. To update it, close InstallerClean and run: | Bu kopya Scoop ile yüklendi. Güncellemek için InstallerClean'i kapatıp şunu çalıştırın: |
| This copy was installed by Scoop for all users. To update it, close InstallerClean, open a terminal as administrator and run: | Bu kopya Scoop ile tüm kullanıcılar için yüklendi. Güncellemek için InstallerClean'i kapatın, yönetici olarak bir terminal açın ve şunu çalıştırın: |
| It needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then open InstallerClean to update. | Bu sürüm .NET Framework 4.8 gerektiriyor. Microsoft'tan yükleyin, bilgisayarı yeniden başlatın, ardından güncellemek için InstallerClean'i açın. |
| InstallerClean {0} can't be installed automatically. | InstallerClean {0} otomatik olarak yüklenemiyor. |
| InstallerClean can't tell whether its setup installed it, so it can't update itself. | InstallerClean kendi kurulum programıyla yüklenip yüklenmediğini anlayamadığı için kendini güncelleyemiyor. |
| Copied | Kopyalandı |
| Couldn't copy it. Select the command and copy it yourself. | Kopyalanamadı. Komutu seçip kendiniz kopyalayın. |
| _Continue without updating | Güncellemeden _devam et |
| Downloading InstallerClean {0}... | InstallerClean {0} indiriliyor... |
| Checking the download... | İndirilen dosya denetleniyor... |
| The update couldn't be downloaded. | Güncelleştirme indirilemedi. |
| The update couldn't be installed. | Güncelleştirme yüklenemedi. |
| InstallerClean has been updated to {0}. | InstallerClean {0} sürümüne güncellendi. |
| What's new | Yenilikler |
| Couldn't reach GitHub. Check your internet connection and try again. | GitHub'a ulaşılamadı. İnternet bağlantınızı kontrol edip yeniden deneyin. |
| The update check received an error instead of GitHub's releases page. Try again later, or open [the releases page] in your browser. | Güncelleştirme denetimi, GitHub'ın sürümler sayfası yerine bir hata aldı. Daha sonra yeniden deneyin ya da [sürümler sayfasını] tarayıcınızda açın. |
| The update check couldn't tell which version is the newest from GitHub's answer. Try again later, or open [the releases page] in your browser. | Güncelleştirme denetimi, GitHub'ın yanıtından hangi sürümün en yeni olduğunu anlayamadı. Daha sonra yeniden deneyin ya da [sürümler sayfasını] tarayıcınızda açın. |
| The check timed out. Your connection to GitHub may be slow; try again. | Denetim zaman aşımına uğradı. GitHub bağlantınız yavaş olabilir; yeniden deneyin. |
| The check failed for an unknown reason. Details are in {0} if you need to report it. | Denetim bilinmeyen bir nedenle başarısız oldu. Bildirmeniz gerekirse ayrıntılar {0} içindedir. |
| The check failed for an unknown reason. The crash log could not be written. | Denetim bilinmeyen bir nedenle başarısız oldu. Çökme günlüğü yazılamadı. |

## Opening links in your browser

| English | Türkçe |
| --- | --- |
| Couldn't open your browser | Tarayıcınız açılamadı |
| The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | Bağlantı panoda, yani kendiniz yapıştırabilirsiniz:<br><br>{0} |
| InstallerClean couldn't copy the link to your clipboard either, so here it is:<br><br>{0} | InstallerClean bağlantıyı panoya da kopyalayamadı, işte burada:<br><br>{0} |

## Startup and crashes

| English | Türkçe |
| --- | --- |
| InstallerClean | InstallerClean |
| It's already running. | Zaten çalışıyor. |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | Beklenmeyen bir hata oluştu ve InstallerClean kapanmak zorunda.<br><br>{0}<br><br>Ayrıntılar şuraya yazıldı:<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | Beklenmeyen bir hata oluştu ve InstallerClean kapanmak zorunda.<br><br>{0}<br><br>Çökme günlüğü yazılamadı. |
| Startup error | Başlangıç hatası |
| InstallerClean won't start because there is a file called {0} beside it, and InstallerClean doesn't come with one. Delete that file, then open InstallerClean again.<br><br>{1} | InstallerClean başlamıyor çünkü yanında {0} adlı bir dosya var ve InstallerClean böyle bir dosyayla gelmez. Bu dosyayı silin, ardından InstallerClean'i yeniden çalıştırın.<br><br>{1} |
| InstallerClean needs Windows 10 version 1607 or later, or Windows 11. | InstallerClean için Windows 10 sürüm 1607 veya üzeri ya da Windows 11 gerekiyor. |
| InstallerClean needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then open InstallerClean again. | InstallerClean için .NET Framework 4.8 gerekiyor. Microsoft'tan yükleyin, bilgisayarı yeniden başlatın, ardından InstallerClean'i yeniden açın. |
| Failed to start ({0}). Details written to:<br>{1} | Başlatılamadı ({0}). Ayrıntılar şuraya yazıldı:<br>{1} |
| Failed to start ({0}). The crash log could not be written. | Başlatılamadı ({0}). Çökme günlüğü yazılamadı. |
| Administrator rights needed | Yönetici hakları gerekli |
| InstallerClean needs administrator rights to see every program installed on this PC, and it isn't running as administrator, so it won't scan. Nothing has been changed.<br><br>Run InstallerClean again as administrator. | InstallerClean'in bu bilgisayarda yüklü her programı görebilmesi için yönetici hakları gerekir; şu anda yönetici olarak çalışmıyor, bu yüzden tarama yapmayacak. Hiçbir şey değiştirilmedi.<br><br>InstallerClean'i yönetici olarak yeniden çalıştırın. |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log, InstallerClean'in yakalanmamış özel durumlarını tutar.<br># Yükseltilmiş yetkiyle, çerçevenin özel durum iletileri çalışan<br># oturumdaki dosya yollarını içerebilir (Windows Installer<br># sorgularının numaralandırdığı diğer kullanıcı profilleri dahil).<br># Güncelleme denetiminden ya da sonuç günlüğünün gönderiminden gelen<br># ağ hatası iletileri hedef URL'yi ve çözümlenen IP ya da proxy<br># adresini içerebilir. Okunamayan Windows Installer kayıtlarına dair<br># girdiler bir Windows hesabı SID'si (S-1-5-21-...) ve yüklü<br># yazılımın ürün kodlarını içerebilir.<br># Bu dosyayı herkese açık bir hata bildirimine eklemeden önce üç tür<br># bilgiyi de çıkarın.<br> |

## Tooltips (hover text)

| English | Türkçe |
| --- | --- |
| Any tip is much appreciated | Her bahşiş çok makbule geçer |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | İptal istendi. InstallerClean, geçerli adımın durabileceği bir noktaya gelmesini bekliyor. Yoğun G/Ç sırasında ya da bir MSI veritabanı çağrısında bu birkaç saniye sürebilir. |
| Close | Kapat |
| I really should charge for InstallerClean - because it's been a huge amount of work - but I want to keep it free so it helps as many people as possible. Your donation makes it worthwhile and is very much appreciated. | Aslında InstallerClean için ücret almam gerekirdi, çünkü çok büyük bir emek harcandı; ama olabildiğince çok kişiye yardımcı olsun diye ücretsiz kalmasını istiyorum. Bağışınız tüm bu emeğe değmesini sağlıyor ve çok makbule geçiyor. |
| A star helps other people find it | Bir yıldız, başkalarının InstallerClean'i bulmasına yardımcı olur |
| Minimise | Simge durumuna küçült |
| Move the unneeded files to the backup folder | Gereksiz dosyaları yedek klasörüne taşır |
| Move the unneeded files to a backup folder. You'll choose it next. | Gereksiz dosyaları bir yedek klasörüne taşır. Klasörü hemen ardından seçeceksiniz. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder. | Gereksiz dosyaları yedek klasörüne taşır. Klasör aynı sürücüde olduğu için, o klasörü silene kadar yeri geri kazanamazsınız. |
| Delete the unneeded files permanently. Use Move instead if you'd like a chance to satisfy yourself all is well. | Gereksiz dosyaları kalıcı olarak siler. Her şeyin yolunda olduğuna kendiniz kanaat getirmek isterseniz onun yerine Taşı'yı kullanın. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Gömülü Authenticode sertifikasındaki konu adı. Zincir doğrulaması yapılmadı. |
| Change language. The program will restart. | Dili değiştir. Program yeniden başlatılacak. |

## Screen reader labels

| English | Türkçe |
| --- | --- |
| Leave a tip | Bahşiş bırak |
| Leave a tip | Bahşiş bırak |
| Leave a tip | Bahşiş bırak |
| Cancel operation | İşlemi iptal et |
| Cancel scan | Taramayı iptal et |
| Cancel startup scan | Başlangıç taramasını iptal et |
| Carry on without it, drive {0} | Beklemeden devam et, {0} sürücüsü |
| Carry on without it, {0} | Beklemeden devam et, {0} |
| Close | Kapat |
| Close window | Pencereyi kapat |
| Close result and return to main window | Sonucu kapat ve ana pencereye dön |
| Leave a star on github | github'da yıldız bırak |
| Minimise | Simge durumuna küçült |
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Kalıcı olarak sil, gereksiz dosyaları kaldırır. İptal, hiçbir şey silmeden kapatır. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Taşı, gereksiz dosyaları seçilen hedef klasöre koyar. İptal, onları olduğu yerde bırakır. |
| Say thanks | Teşekkür etmek için |
| Check for updates | Güncelleştirmeleri denetle |
| Checks github's releases page for a newer version. | github üzerindeki sürümler sayfasında daha yeni bir sürüm olup olmadığını denetler. |
| Opens the readme on github in your browser. | github üzerindeki readme'yi tarayıcınızda açar. |
| Opens this version's release page on github in your browser. | github üzerindeki bu sürümün sayfasını tarayıcınızda açar. |
| Opens the issue tracker on github.com in your browser. | github.com üzerindeki sorun izleyiciyi (Issues) tarayıcınızda açar. |
| Shows the licences of the fonts, icons and code InstallerClean includes. | InstallerClean'in içerdiği yazı tiplerinin, simgelerin ve kodun lisanslarını gösterir. |
| If ticked, InstallerClean checks github for a newer version when you run it. | İşaretliyse InstallerClean, çalıştırdığınızda github üzerinde daha yeni bir sürüm olup olmadığını denetler. |
| Download it from github | github'dan indirin |
| Opens InstallerClean's releases on github in your browser, newest first. | github üzerindeki InstallerClean sürümlerini en yenisi başta olmak üzere tarayıcınızda açar. |
| Download .NET Framework 4.8 from Microsoft | .NET Framework 4.8'i Microsoft'tan indirin |
| Copy the command | Komutu kopyala |
| Scoop's update command | Scoop güncelleme komutu |
| Opens the licence file on github.com in your browser. | github.com üzerindeki lisans dosyasını tarayıcınızda açar. |
| Backup folder | Yedek klasörü |
| Patches | Yamalar |
| Product details | Ürün ayrıntıları |
| Backup folder | Yedek klasörü |
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
| Waiting for a drive or share | Bir sürücü veya paylaşım bekleniyor |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | İşlenemeyen dosyalar |
| Explains this folder, and how to recover a file, in the README | Bu klasörü açıklar ve bir dosyanın nasıl kurtarılacağını README'de anlatır |
| {0} Press Alt+{1} to carry on without it. {2} | {0} Beklemeden devam etmek için Alt+{1} tuşlarına basın. {2} |
| What the report holds | Raporun içeriği |
| What the report holds | Raporun içeriği |
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
| drive {0} | {0} sürücüsü |
| .  | .  |
| {0} TB | {0} TB |
| {0} GB | {0} GB |
| {0} MB | {0} MB |
| {0} KB | {0} KB |
| {0} B | {0} B |
| {0:F0}ms | {0:F0}ms |
| {0:F1}s | {0:F1}s |
| less than a second | bir saniyeden az |
| {0:F1} seconds | {0:F1} saniye |

## Command-line tool (installerclean-cli)

| English | Türkçe |
| --- | --- |
| InstallerClean needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then run InstallerClean again. | InstallerClean için .NET Framework 4.8 gerekiyor. Microsoft'tan yükleyin, bilgisayarı yeniden başlatın, ardından InstallerClean'i yeniden çalıştırın. |
| Error: unknown argument '{0}' | Hata: bilinmeyen argüman '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Hata: beklenmeyen fazladan argüman '{0}'. Hedef klasörünüzün adında boşluk varsa tüm yolu tırnak içine alın: /m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | Hata: beklenmeyen fazladan argüman '{0}'. /s ve /d başka argüman almaz ve her çalıştırmada yalnızca bir bayrak kullanılabilir. |
| Cancelling... | İptal ediliyor... |
| Cancelled. | İptal edildi. |
| Error: unexpected failure ({0}). Details written to {1}. | Hata: beklenmeyen arıza ({0}). Ayrıntılar {1} konumuna yazıldı. |
| Error: unexpected failure ({0}). The crash log could not be written. | Hata: beklenmeyen arıza ({0}). Çökme günlüğü yazılamadı. |
| Scanning {InstallerFolder}... | {InstallerFolder} taranıyor... |
| Found {0} unneeded {1} to clean up ({2}). | Temizlenecek {0} gereksiz {1} bulundu ({2}). |
| No unneeded files identified. | Gereksiz dosya tespit edilmedi. |
| Windows has a record for {0} file that is not in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. To put the file back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Windows'ta {InstallerFolder} içinde bulunmayan {0} dosyaya ait bir kayıt var: {1}. Günlük kullanımda sorun çıkarmaz, ama o programın güncellenmesi ya da kaldırılması başarısız olabilir. Dosyayı geri koymak için, halihazırda sahip olduğunuz sürümün kurulum programına ihtiyacınız var. Onu programın üreticisinden edinin ve mevcut kopyanızın üzerine çalıştırın. Daha yeni bir sürüm işe yaramaz: önce sizdekini kaldırması gerekir ve bu dosyaya ihtiyaç duyan tam da o adımdır. Önce kaldırmak da aynı nedenle işe yaramaz. Bunun dosyayı geri getirmesi ve ayarlarınıza dokunmaması beklenir, ancak Microsoft bunu garanti etmez. |
| Windows has records for {0} files that are not in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. To put a file back, you need the installer for the version you already have of that program. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs the file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Windows'ta {InstallerFolder} içinde bulunmayan {0} dosyaya ait kayıtlar var: {1}. Günlük kullanımda sorun çıkarmaz, ama o programların güncellenmesi ya da kaldırılması başarısız olabilir. Bir dosyayı geri koymak için, o programın halihazırda sahip olduğunuz sürümünün kurulum programına ihtiyacınız var. Onu programın üreticisinden edinin ve mevcut kopyanızın üzerine çalıştırın. Daha yeni bir sürüm işe yaramaz: önce sizdekini kaldırması gerekir ve o dosyaya ihtiyaç duyan tam da o adımdır. Önce kaldırmak da aynı nedenle işe yaramaz. Bunun dosyayı geri getirmesi ve ayarlarınıza dokunmaması beklenir, ancak Microsoft bunu garanti etmez. |
| InstallerClean stopped waiting for drive {0}, so it left alone any file it couldn't check without it. Once it's responding normally, run the command again. | InstallerClean {0} sürücüsünü beklemeyi kesti, bu yüzden o sürücü olmadan denetleyemediği her dosyayı olduğu gibi bıraktı. Tekrar normal yanıt vermeye başladığında komutu yeniden çalıştırın. |
| InstallerClean stopped waiting for {0}, so it left alone any file it couldn't check without it. Once it's responding normally, run the command again. | InstallerClean {0} konumunu beklemeyi kesti, bu yüzden o konum olmadan denetleyemediği her dosyayı olduğu gibi bıraktı. Tekrar normal yanıt vermeye başladığında komutu yeniden çalıştırın. |
| InstallerClean stopped waiting for more than one drive or share ({0}), so it left alone any file it couldn't check without them. Once they're responding normally, run the command again. | InstallerClean birden fazla sürücüyü veya paylaşımı ({0}) beklemeyi kesti, bu yüzden bunlar olmadan denetleyemediği her dosyayı olduğu gibi bıraktı. Tekrar normal yanıt vermeye başladıklarında komutu yeniden çalıştırın. |
| Deleting {0} unneeded {1}... | {0} gereksiz {1} siliniyor... |
| Permanently deleted {0} unneeded {1}. | {0} gereksiz {1} kalıcı olarak silindi. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Hata: taşıma hedefi belirtilmedi. /m YOL kullanın. (GUI'de ayarlanan bir varsayılan, kullanıcıya özeldir ve zamanlanmış ya da hizmet hesabı çalıştırmaları için geçerli değildir.) |
| Error: destination cannot be inside the Windows Installer folder. | Hata: hedef, Windows Installer klasörünün içinde olamaz. |
| Error: destination must be a fully qualified path. Got: {0} | Hata: hedef, tam nitelenmiş bir yol olmalıdır. Alınan: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Hata: {0} hedefi bir Windows sistem klasörünün altına çözümleniyor. %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% ve %ProgramData% dışında bir yol seçin. |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | Hata: {0} konumunda yeterli alan yok. Bu dosyaların taşınması {1} gerektiriyor, kullanılabilir alan {2}. Hiçbir şey taşınmadı. |
| Error: something is using Windows Installer right now, such as Windows Update or a program being installed or removed. /m and /d are blocked while that runs. Try again once it has finished. | Hata: şu anda bir şey Windows Installer'ı kullanıyor, örneğin Windows Update ya da kurulmakta veya kaldırılmakta olan bir program. O sürerken /m ve /d engellenir. O bittiğinde yeniden deneyin. |
| Error: an earlier Windows Installer installation hasn't finished. /m and /d are blocked, so InstallerClean won't touch {InstallerFolder}. Restart Windows, then run the command again. An installation waiting for a restart carries on after it. If this message comes back, run the setup of the program that was being installed or removed again, and Windows Installer will offer to undo the installation it didn't finish. | Hata: Windows Installer önceki bir kurulumu tamamlamadı. /m ve /d engellendi, bu yüzden InstallerClean {InstallerFolder} klasörüne dokunmayacak. Windows'u yeniden başlatın, ardından komutu yeniden çalıştırın. Yeniden başlatmayı bekleyen bir kurulum, yeniden başlatmadan sonra devam eder. Bu ileti yeniden görünürse, kurulmakta ya da kaldırılmakta olan programın kurulum programını tekrar çalıştırın, Windows Installer da tamamlamadığı kurulumun yaptığı değişiklikleri geri almayı önerecektir. |
| Error: Windows has a change to {InstallerFolder} waiting for the next restart ({0}). /m and /d are blocked. Restart Windows, then try again. | Hata: Windows'un bir sonraki yeniden başlatmada {InstallerFolder} klasörüne uygulamak üzere beklettiği bir değişiklik var ({0}). /m ve /d engellendi. Windows'u yeniden başlatın, ardından yeniden deneyin. |
| Error: Windows has a file change waiting for the next restart, and InstallerClean can't tell which files it affects, so it can't rule out that they're in {InstallerFolder}. /m and /d are blocked. Restart Windows, then try again. | Hata: Windows'un bir sonraki yeniden başlatmayı bekleyen bir dosya değişikliği var ve InstallerClean bunun hangi dosyaları etkilediğini bilemiyor, bu yüzden bunların {InstallerFolder} içinde olmadığını dışlayamıyor. /m ve /d engellendi. Windows'u yeniden başlatın, ardından yeniden deneyin. |
| Error: InstallerClean couldn't read one of the registry values it checks before touching {InstallerFolder}, so it can't tell whether Windows Installer is busy or has work waiting for a restart. /m and /d are blocked. Restart Windows and try again. If the read still fails, this isn't a machine InstallerClean can clean. | Hata: InstallerClean, {InstallerFolder} klasörüne dokunmadan önce denetlediği kayıt defteri değerlerinden birini okuyamadı, bu yüzden Windows Installer'ın meşgul olup olmadığını ya da yeniden başlatma bekleyen bir işi olup olmadığını bilemiyor. /m ve /d engellendi. Windows'u yeniden başlatın ve tekrar deneyin. Okuma yine başarısız olursa, burası InstallerClean'in temizleyebileceği bir makine değil. |
| Error: InstallerClean couldn't check whether Windows Installer is busy, so it can't rule out an installation in progress. /m and /d are blocked. Restart Windows and try again. If the check still fails, this isn't a machine InstallerClean can clean. | Hata: InstallerClean, Windows Installer'ın meşgul olup olmadığını denetleyemedi, bu yüzden süren bir kurulumu dışlayamıyor. /m ve /d engellendi. Windows'u yeniden başlatın ve tekrar deneyin. Denetim yine başarısız olursa, burası InstallerClean'in temizleyebileceği bir makine değil. |
| Error: InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}), so it can't rule out an installation in progress. /m and /d are blocked. Restart Windows and try again. If the check still fails, this isn't a machine InstallerClean can clean. | Hata: InstallerClean, Windows Installer'ın meşgul olup olmadığını denetleyemedi, çünkü Windows bir hata verdi: {0} (hata {1}); bu yüzden süren bir kurulumu dışlayamıyor. /m ve /d engellendi. Windows'u yeniden başlatın ve tekrar deneyin. Denetim yine başarısız olursa, burası InstallerClean'in temizleyebileceği bir makine değil. |
| Error: Windows Installer is still running, as it does for a few minutes after a program has been installed, updated or removed, and for as long as an installer's window is left open. /m and /d are blocked until it has stopped. Try again in a few minutes. | Error: Windows Installer is still running, as it does for a few minutes after a program has been installed, updated or removed, and for as long as an installer's window is left open. /m and /d are blocked until it has stopped. Try again in a few minutes. |
| Error: Windows Installer has something unfinished, so /m and /d are blocked and InstallerClean won't touch {InstallerFolder}. Try again once it has cleared. | Hata: Windows Installer'ın bitmemiş bir işi var, bu yüzden /m ve /d engellendi ve InstallerClean {InstallerFolder} klasörüne dokunmayacak. O sonuçlandığında yeniden deneyin. |
| Waiting for Windows Installer to finish. InstallerClean carries on by itself when it has. | Waiting for Windows Installer to finish. InstallerClean carries on by itself when it has. |
| Checking the remaining files again. | Checking the remaining files again. |
| Error: Windows Installer stayed busy for 30 minutes, so InstallerClean stopped. Files already moved or deleted stay so. The rest were not touched. Run the command again later to carry on. | Error: Windows Installer stayed busy for 30 minutes, so InstallerClean stopped. Files already moved or deleted stay so. The rest were not touched. Run the command again later to carry on. |
| Error: Windows Installer's records kept changing while InstallerClean checked the files, which happens while programs are being installed or updated, so it stopped. Files already moved or deleted stay so. The rest were not touched. Try again once the installations have finished. | Error: Windows Installer's records kept changing while InstallerClean checked the files, which happens while programs are being installed or updated, so it stopped. Files already moved or deleted stay so. The rest were not touched. Try again once the installations have finished. |
| Error: Windows would not let InstallerClean watch the registry keys where Windows Installer keeps its records, so it cannot tell whether they change while it works, and it stopped. Files already moved or deleted stay so. The rest were not touched. | Error: Windows would not let InstallerClean watch the registry keys where Windows Installer keeps its records, so it cannot tell whether they change while it works, and it stopped. Files already moved or deleted stay so. The rest were not touched. |
| Error: InstallerClean could not make its own folder, {0}, inside {InstallerFolder} (Windows error {1}), so it did not move or delete anything. | Error: InstallerClean could not make its own folder, {0}, inside {InstallerFolder} (Windows error {1}), so it did not move or delete anything. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it stopped. Files already moved or deleted stay so. The rest were not touched. | Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it stopped. Files already moved or deleted stay so. The rest were not touched. |
| Windows Installer was busy when the scan started, for example with Windows Update or a program being installed or removed, so InstallerClean left alone the files it can't check while it's busy. Run the command again once it has finished. | Tarama başladığında Windows Installer meşguldü, örneğin Windows Update ya da kurulmakta veya kaldırılmakta olan bir programla, bu yüzden InstallerClean, Windows Installer meşgulken denetleyemediği dosyaları olduğu gibi bıraktı. O bittiğinde komutu yeniden çalıştırın. |
| An earlier Windows Installer installation hadn't finished when the scan started, so InstallerClean left alone the files it can't check until it has. Restart Windows, then run the command again. An installation waiting for a restart carries on after it. If this message comes back, run the setup of the program that was being installed or removed again, and Windows Installer will offer to undo the installation it didn't finish. | Tarama başladığında Windows Installer önceki bir kurulumu henüz tamamlamamıştı, bu yüzden InstallerClean, o kurulum tamamlanana kadar denetleyemediği dosyaları olduğu gibi bıraktı. Windows'u yeniden başlatın, ardından komutu yeniden çalıştırın. Yeniden başlatmayı bekleyen bir kurulum, yeniden başlatmadan sonra devam eder. Bu ileti yeniden görünürse, kurulmakta ya da kaldırılmakta olan programın kurulum programını tekrar çalıştırın, Windows Installer da tamamlamadığı kurulumun yaptığı değişiklikleri geri almayı önerecektir. |
| Windows had a change to {InstallerFolder} waiting for the next restart when the scan started, so InstallerClean left alone the files it can't check until that change is made. Restart Windows, then run the command again. | Tarama başladığında Windows'un bir sonraki yeniden başlatmada {InstallerFolder} klasörüne uygulamak üzere beklettiği bir değişiklik vardı, bu yüzden InstallerClean, bu değişiklik uygulanana kadar denetleyemediği dosyaları olduğu gibi bıraktı. Windows'u yeniden başlatın, ardından komutu yeniden çalıştırın. |
| Windows had a file change waiting for the next restart when the scan started, and InstallerClean couldn't tell which files it affects, so it left alone the files it can't check until that change is made. Restart Windows, then run the command again. | Tarama başladığında Windows'un bir sonraki yeniden başlatmayı bekleyen bir dosya değişikliği vardı ve InstallerClean bunun hangi dosyaları etkilediğini bilemedi, bu yüzden bu değişiklik uygulanana kadar denetleyemediği dosyaları olduğu gibi bıraktı. Windows'u yeniden başlatın, ardından komutu yeniden çalıştırın. |
| Windows refused InstallerClean permission to check whether Windows Installer was busy when the scan started, so it left alone the files it can't check while Windows Installer is busy. Restart Windows, then run the command again. | Tarama başladığında Windows, InstallerClean'e Windows Installer'ın meşgul olup olmadığını denetleme izni vermedi, bu yüzden InstallerClean, Windows Installer meşgulken denetleyemediği dosyaları olduğu gibi bıraktı. Windows'u yeniden başlatın, ardından komutu yeniden çalıştırın. |
| InstallerClean couldn't read a Windows setting it checks, so it couldn't tell whether Windows Installer was busy or had work waiting for a restart when the scan started. It left alone the files it can't check while Windows Installer is busy. Restart Windows, then run the command again. | Tarama başladığında InstallerClean denetlediği bir Windows ayarını okuyamadı, bu yüzden Windows Installer'ın meşgul olup olmadığını ya da yeniden başlatma bekleyen bir işi olup olmadığını anlayamadı. InstallerClean, Windows Installer meşgulken denetleyemediği dosyaları olduğu gibi bıraktı. Windows'u yeniden başlatın, ardından komutu yeniden çalıştırın. |
| InstallerClean couldn't check whether Windows Installer was busy when the scan started, so it left alone the files it can't check while Windows Installer is busy. Restart Windows, then run the command again. | Tarama başladığında InstallerClean, Windows Installer'ın meşgul olup olmadığını denetleyemedi, bu yüzden Windows Installer meşgulken denetleyemediği dosyaları olduğu gibi bıraktı. Windows'u yeniden başlatın, ardından komutu yeniden çalıştırın. |
| InstallerClean couldn't check whether Windows Installer was busy when the scan started. Windows gave an error: {0} (error {1}). It left alone the files it can't check while Windows Installer is busy. Restart Windows, then run the command again. | Tarama başladığında InstallerClean, Windows Installer'ın meşgul olup olmadığını denetleyemedi. Windows bir hata verdi: {0} (hata {1}). InstallerClean, Windows Installer meşgulken denetleyemediği dosyaları olduğu gibi bıraktı. Windows'u yeniden başlatın, ardından komutu yeniden çalıştırın. |
| Windows Installer had something unfinished when the scan started, so InstallerClean left alone the files it can't check until it has finished. Run the command again once it has finished. | Tarama başladığında Windows Installer'ın bitmemiş bir işi vardı, bu yüzden InstallerClean, o iş bitene kadar denetleyemediği dosyaları olduğu gibi bıraktı. O bittiğinde komutu yeniden çalıştırın. |
| This PC's clock went back or lost time while the scan was running, so InstallerClean couldn't tell which files had just been added, and left alone every file that could have been. Run the command again. | Tarama sürerken bu bilgisayarın saati geri gitti ya da geri kaldı, bu yüzden InstallerClean hangi dosyaların az önce eklendiğini bilemedi ve bunlardan biri olabilecek her dosyayı olduğu gibi bıraktı. Komutu yeniden çalıştırın. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. | Hata: Windows, InstallerClean'e Windows Installer'ın meşgul olup olmadığını denetleme izni vermedi, bu yüzden işlem sürerken bir dosyanın gerekli hale gelmesini dışlayamadı. Hiçbir şey silinmedi. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. | Hata: Windows, InstallerClean'e Windows Installer'ın meşgul olup olmadığını denetleme izni vermedi, bu yüzden işlem sürerken bir dosyanın gerekli hale gelmesini dışlayamadı. Hiçbir şey taşınmadı. |
| Moving {0} unneeded {1} to {2}... | {0} gereksiz {1} şuraya taşınıyor: {2}... |
| Moved {0} unneeded {1}. | {0} gereksiz {1} taşındı. |
| Check that your programs still update and uninstall as normal, then delete {0}. | Programlarınızın hâlâ normal şekilde güncellendiğini ve kaldırıldığını doğrulayın, sonra {0} klasörünü silin. |
| InstallerClean is free. If it helped, a tip is much appreciated: {0} | InstallerClean ücretsizdir. İşinize yaradıysa bir bahşiş çok makbule geçer: {0} |
| Update now? It's strongly recommended to use the latest version. [{0}/{1}] | Şimdi güncellensin mi? En son sürümü kullanmanız önemle tavsiye edilir. [{0}/{1}] |
| Y | E |
| N | H |
| No answer after a minute, so InstallerClean updates without asking, as it does when nobody is there to answer. | Bir dakika içinde yanıt gelmedi, bu yüzden InstallerClean, yanıtlayacak kimse olmadığında yaptığı gibi sormadan güncelleniyor. |
| Send an anonymous report of this run? It is saved at {0}. Type ? to see what it holds. [{1}/{2}/?] | Bu çalıştırmanın anonim raporu gönderilsin mi? {0} konumuna kaydedildi. İçeriğini görmek için ? yazın. [{1}/{2}/?] |
| the chart at {0} | {0} adresindeki grafiğe |
| The report adds your result to {0}, which shows how much space people are reclaiming with InstallerClean. It also holds these technical details, to help me spot any bugs: | Rapor, sonucunuzu {0} ekler; bu grafik, insanların InstallerClean ile ne kadar yer açtığını gösterir. Ayrıca hataları fark etmeme yardımcı olacak şu teknik ayrıntıları da içerir: |
| The full report is printed below. | Raporun tamamı aşağıda yazdırılmıştır. |
| Report sent. Thank you. | Rapor gönderildi. Teşekkürler. |
| Report not sent. | Rapor gönderilmedi. |
| No answer after a minute. | Bir dakika içinde yanıt gelmedi. |
| Cancelled while sending the report. It may have arrived anyway. | Rapor gönderilirken iptal edildi. Yine de ulaşmış olabilir. |
| The report could not be sent. It is kept in {0}. | Rapor gönderilemedi. {0} konumunda saklanıyor. |
| Sending a report left waiting by an earlier run. | Daha önceki bir çalıştırmadan beri bekleyen bir rapor gönderiliyor. |
| The new version will scan and list what it finds, without deleting anything. To delete, run /d again afterwards. | Yeni sürüm tarayıp bulduklarını listeleyecek, hiçbir şeyi silmeyecek. Silmek için ardından /d komutunu yeniden çalıştırın. |
| The new version will scan and list what it finds, without moving anything. To move, run /m again afterwards. | Yeni sürüm tarayıp bulduklarını listeleyecek, hiçbir şeyi taşımayacak. Taşımak için ardından /m komutunu yeniden çalıştırın. |
| InstallerClean is in use, so it can't update itself now. | InstallerClean kullanımda olduğu için şu anda kendini güncelleyemiyor. |
| Running {0} with InstallerClean {1} instead. | Bunun yerine {0} komutu InstallerClean {1} ile çalıştırılıyor. |
| The newer version couldn't be run. | Daha yeni sürüm çalıştırılamadı. |
| Running {0} with InstallerClean {1}. It takes this copy's place once the run ends. | {0} komutu InstallerClean {1} ile çalıştırılıyor. Çalışma bitince bu kopyanın yerini alacak. |
| {0}: {1} | {0}: {1} |
| It needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then run InstallerClean again to update. | Bu sürüm .NET Framework 4.8 gerektiriyor. Microsoft'tan yükleyin, bilgisayarı yeniden başlatın, ardından güncellemek için InstallerClean'i yeniden çalıştırın. |
| It's simple to undo. Move them back from {0} into {InstallerFolder} and everything will be back to how it was. | Geri almak kolay. Onları {0} konumundan {InstallerFolder} klasörüne geri taşıyın, her şey eskisi gibi olur. |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then run the command again. | InstallerClean yedek klasörünü artık doğrulayamadı, bu yüzden durdu. {0} konumunu denetleyin, sonra komutu yeniden çalıştırın. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Başka bir InstallerClean işlemi tek örnek kilidini tutuyor (GUI ya da başka bir CLI çalıştırması). Çıkış 75 (geçici); daha sonra yeniden denemek güvenli. |
| Stopped after deleting {0} of {1} {2}: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 2 (partial). | {1} {2} içinden {0} tanesi silindikten sonra durduruldu: InstallerClean, başka hiçbir InstallerClean'in işlem yapmadığından artık emin olamadı. Aynı komutu yeniden çalıştırın. Çıkış 2 (kısmi). |
| Stopped after moving {0} of {1} {2} to {3}: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 2 (partial). | {1} {2} içinden {0} tanesi {3} konumuna taşındıktan sonra durduruldu: InstallerClean, başka hiçbir InstallerClean'in işlem yapmadığından artık emin olamadı. Aynı komutu yeniden çalıştırın. Çıkış 2 (kısmi). |
| Stopped before deleting any file: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 75 (transient). | Herhangi bir dosya silinmeden önce durduruldu: InstallerClean, başka hiçbir InstallerClean'in işlem yapmadığından artık emin olamadı. Aynı komutu yeniden çalıştırın. Çıkış 75 (geçici). |
| Stopped before moving any file: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 75 (transient). | Herhangi bir dosya taşınmadan önce durduruldu: InstallerClean, başka hiçbir InstallerClean'in işlem yapmadığından artık emin olamadı. Aynı komutu yeniden çalıştırın. Çıkış 75 (geçici). |
| InstallerClean needs administrator rights to see every program installed on this PC, and it isn't running as administrator, so it won't scan. Nothing has been changed. Run it again from an administrator prompt. | InstallerClean'in bu bilgisayarda yüklü her programı görebilmesi için yönetici hakları gerekir; şu anda yönetici olarak çalışmıyor, bu yüzden tarama yapmayacak. Hiçbir şey değiştirilmedi. Bir yönetici komut isteminden yeniden çalıştırın. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Not: Olay Günlüğü'ne yazma başarısız oldu. Uygulama günlüğü izinlerini ya da Grup İlkesi'ni kontrol edin. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - {InstallerFolder} temizliği |
| Removes cached .msi and .msp files that no installed program still needs. | Yüklü hiçbir programın artık gerek duymadığı .msi/.msp dosyalarını siler. |
| Needs an elevated (administrator) prompt and won't scan without one. | Yönetici komut istemi gerektirir; aksi halde tarama yapmaz. |
| Usage: | Kullanım: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     Bu yardımı göster (/?, -h de kabul edilir) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  Sürümü yazdır (-v de kabul edilir) |
|   installerclean-cli --notices  Print the third-party notices |   installerclean-cli --notices  Üçüncü taraf bildirimlerini yazdır |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         Yalnızca tara - gereksizleri listele |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         Gereksiz dosyaları kalıcı olarak sil |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         Kayıtlı yedek klasörüne taşı |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m YOL     Belirtilen yola taşı |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli bitene kadar komut istemini tutar, böylece bir betik<br>ya da zamanlanmış görev onu bekleyebilir. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | Klasör kullanıcıya özeldir; zamanlanmış veya SYSTEM: /m YOL. |
| Before it scans, it checks for a newer version of InstallerClean, unless<br>"Check for updates automatically" is turned off for the account it runs<br>as. Where its window is on screen and nothing is piped into or out of it,<br>it asks before updating, and updates if nobody answers within a minute.<br>Anywhere else, such as a scheduled task set to run whether or not anyone<br>is logged on, it updates without asking.<br>Add /noupdate to skip the check. | Taramadan önce InstallerClean'in daha yeni bir sürümü olup olmadığını<br>denetler; ancak çalıştığı hesapta “Güncelleştirmeleri otomatik olarak<br>denetle” seçeneği kapalıysa denetlemez. Penceresi ekrandaysa ve girdisi<br>ile çıktısı yönlendirilmemişse, güncellemeden önce sorar ve bir dakika<br>içinde kimse yanıtlamazsa güncellenir. Başka her durumda, örneğin biri<br>oturum açmış olsa da olmasa da çalışacak şekilde ayarlanmış zamanlanmış<br>bir görevde, sormadan güncellenir. Denetimi atlamak için /noupdate<br>ekleyin. |
| Exit codes: | Çıkış kodları: |
|   0   success: the run did what it was asked |   0   başarılı: isteneni yaptı |
|   1   nothing processed: the run failed or was refused |   1   hiçbir şey işlenmedi: çalıştırma başarısız oldu ya da reddedildi |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   kısmi: bir kısmı işlendi, bir kısmı işlenmedi (hata ya da Ctrl+C) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  geçici: geçici bir durum çalıştırmayı engelledi (iletiye bakın) |
|   130 cancelled (Ctrl+C) |   130 iptal edildi (Ctrl+C) |
