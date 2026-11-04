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
| Any file still to be checked against it will be left alone. | Hâlâ onunla karşılaştırılması gereken her dosya olduğu gibi bırakılacak. |
| Scan complete ({0}) | Tarama tamamlandı ({0}) |
| Found {0} {1} you can safely delete. | Güvenle silebileceğiniz {0} {1} bulundu. |
| Preparing destination folder... | Hedef klasör hazırlanıyor... |
| Moving unneeded files... | Gereksiz dosyalar taşınıyor... |
| Deleting unneeded files... | Gereksiz dosyalar siliniyor... |
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
| They sit in {InstallerFolder}. InstallerClean asks Windows about every installed program: a file is listed when no program claims it ({0}), or when a newer patch has replaced it and no program could roll back to it ({1}). | Bunlar {InstallerFolder} içinde duruyor. InstallerClean, yüklü her programı Windows'a sorar: bir dosya, hiçbir program onu sahiplenmediğinde ({0}) ya da daha yeni bir yama onun yerine geçtiğinde ve hiçbir program ona geri dönemeyecek durumdayken ({1}) listelenir. |
| Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update and uninstall as normal. Putting them back into {InstallerFolder} restores everything. Or delete them permanently now. | Onları seçeceğiniz bir yedek klasörüne taşıyın, sonra programlarınızın hâlâ normal şekilde güncellendiğine ve kaldırıldığına kanaat getirdiğinizde o klasörü silin. Onları {InstallerFolder} içine geri koymak her şeyi eski haline getirir. Ya da şimdi kalıcı olarak silin. |
| Nothing scanned yet. | Henüz tarama yapılmadı. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Hiçbir programın hâlâ ihtiyaç duymadığı yükleyici dosyaları için {InstallerFolder} klasörüne bakmak üzere Yeniden tara'ya basın. |
| Nothing to show until the next scan. | Bir sonraki taramaya kadar gösterilecek bir şey yok. |
| The scan that runs after a Move or Delete didn't finish, so there's no list to show. Press Re-scan to scan {InstallerFolder} again. | Taşı veya Sil'den sonra çalışan tarama tamamlanmadı, bu yüzden gösterilecek bir liste yok. {InstallerFolder} klasörünü yeniden taramak için Yeniden tara'ya basın. |
| What stopped it is recorded in {0}. | Taramayı durduran neden {0} içine kaydedildi. |
| These files can't be cleaned up right now. | Bu dosyalar şu anda temizlenemez. |
| Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Şu anda bir şey Windows Installer'ı kullanıyor, örneğin bir Windows güncelleştirmesi ya da arka planda kurulan bir program. O sürerken Taşı ve Sil duraklatılır, böylece InstallerClean değişmekte olan {InstallerFolder} klasörüne dokunmaz. Bittiğinde yeniden tarayın, ikisi de geri gelir. |
| Windows refused InstallerClean permission to check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If Windows still refuses, this isn't a machine InstallerClean can clean. | Windows, InstallerClean'e Windows Installer'ın meşgul olup olmadığını denetleme izni vermedi, bu yüzden Taşı ve Sil duraklatıldı ve InstallerClean {InstallerFolder} klasörüne dokunmayacak. Windows'u yeniden başlatın ve yeniden tarayın. Windows yine reddederse, burası InstallerClean'in temizleyebileceği bir makine değil. |
| InstallerClean couldn't check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If it still can't check, this isn't a machine InstallerClean can clean. | InstallerClean, Windows Installer'ın meşgul olup olmadığını denetleyemedi, bu yüzden Taşı ve Sil duraklatıldı ve InstallerClean {InstallerFolder} klasörüne dokunmayacak. Windows'u yeniden başlatın ve yeniden tarayın. Yine denetleyemezse, burası InstallerClean'in temizleyebileceği bir makine değil. |
| InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}). Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If it still can't check, this isn't a machine InstallerClean can clean. | InstallerClean, Windows Installer'ın meşgul olup olmadığını denetleyemedi, çünkü Windows bir hata verdi: {0} (hata {1}). Taşı ve Sil duraklatıldı ve InstallerClean {InstallerFolder} klasörüne dokunmayacak. Windows'u yeniden başlatın ve yeniden tarayın. Yine denetleyemezse, burası InstallerClean'in temizleyebileceği bir makine değil. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Bu makinede askıya alınmış önceki bir Windows Installer işlemi var. {InstallerFolder} klasörünü temizlemeden önce o kurulumu sürdürün ya da geri alın (veya Windows'u yeniden başlatın). |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows, bir sonraki yeniden başlatma için {InstallerFolder} klasörünü etkileyen bir dosya adı değişikliği sıraya aldı. Temizlemeden önce Windows'u yeniden başlatın. |
| A file operation is queued for the next restart and InstallerClean can't tell which files it names, so it can't rule out that they're in {InstallerFolder}. Restart Windows before cleaning. | Bir sonraki yeniden başlatma için sıraya alınmış bir dosya işlemi var ve InstallerClean bu işlemin hangi dosyaları adlandırdığını bilemiyor, bu yüzden bunların {InstallerFolder} içinde olmadığını dışlayamıyor. Temizlemeden önce Windows'u yeniden başlatın. |
| InstallerClean couldn't read one of the Windows settings it checks before touching {InstallerFolder}, so it can't tell whether an installer operation is running or waiting for a restart. Restart Windows and Re-scan. If the setting still won't read, this isn't a machine InstallerClean can clean. | InstallerClean, {InstallerFolder} klasörüne dokunmadan önce denetlediği Windows ayarlarından birini okuyamadı, bu yüzden bir yükleyici işleminin sürüp sürmediğini ya da yeniden başlatma beklediğini bilemiyor. Windows'u yeniden başlatın ve yeniden tarayın. Ayar yine okunamıyorsa, burası InstallerClean'in temizleyebileceği bir makine değil. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer'ın sürmekte olan bir işi var, bu yüzden Taşı ve Sil duraklatıldı. InstallerClean değişmekte olan {InstallerFolder} klasörüne dokunmayacak. Bittiğinde yeniden tarayın, ikisi de geri gelir. |
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
| Nothing offered on this PC | Bu bilgisayarda hiçbir şey sunulmadı |
| One file ({2}) was held back because Windows shows it was changed after InstallerClean started scanning. If a program was installing or updating, scan again once it has finished and InstallerClean will check it again. | Bir dosya ({2}), Windows'a göre InstallerClean taramaya başladıktan sonra değiştirildiği için geri tutuldu. Bir programın kurulumu ya da güncellemesi sürüyorduysa, bittiğinde yeniden tarayın; InstallerClean onu yeniden denetleyecek. |
| {0} {1} ({2}) were held back because Windows shows they were changed after InstallerClean started scanning. If a program was installing or updating, scan again once it has finished and InstallerClean will check them again. | {0} {1} ({2}), Windows'a göre InstallerClean taramaya başladıktan sonra değiştirildikleri için geri tutuldu. Bir programın kurulumu ya da güncellemesi sürüyorduysa, bittiğinde yeniden tarayın; InstallerClean onları yeniden denetleyecek. |
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
| {0} file held back. The scan said it was unneeded. The final check couldn't confirm that. | {0} dosya geri tutuldu. Tarama bunun gereksiz olduğunu söyledi. Son denetim bunu doğrulayamadı. |
| {0} files held back. The scan said these were unneeded. The final check couldn't confirm that. | {0} dosya geri tutuldu. Tarama bunların gereksiz olduğunu söyledi. Son denetim bunu doğrulayamadı. |
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
| InstallerClean's version, how you got it (setup, portable, slim, Scoop, the command-line tool on its own, built from source, or not known), whether the report was sent from InstallerClean or the command-line tool, and the language it was showing | InstallerClean'in sürümü, nasıl edinildiği (setup, taşınabilir, slim, Scoop, tek başına komut satırı aracı, kaynaktan derlenmiş veya bilinmiyor), raporun InstallerClean'den mi yoksa komut satırı aracından mı gönderildiği ve gösterildiği dil |
| Counts of anything that went wrong | Ters giden her şeyin sayısı |
| Each of these is a number or a label. Nothing in the report names you, your PC, your files or your programs, and I never see your IP address. | Bunların her biri bir sayı ya da etikettir. Raporda hiçbir şey sizi, bilgisayarınızı, dosyalarınızı veya programlarınızı tanımlamaz ve IP adresinizi asla görmem. |
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
| InstallerClean carried on without drive {0} and left alone any file still to be checked against it. Once it's responding normally, Re-scan. | InstallerClean {0} sürücüsü olmadan devam etti ve hâlâ onunla karşılaştırılması gereken her dosyayı olduğu gibi bıraktı. Yeniden normal yanıt vermeye başladığında Yeniden tara'ya basın. |
| InstallerClean carried on without {0} and left alone any file still to be checked against it. Once it's responding normally, Re-scan. | InstallerClean {0} konumu olmadan devam etti ve hâlâ onunla karşılaştırılması gereken her dosyayı olduğu gibi bıraktı. Yeniden normal yanıt vermeye başladığında Yeniden tara'ya basın. |
| InstallerClean carried on without more than one drive or share ({0}) and left alone any file still to be checked against one of them. Once they're responding normally, Re-scan. | InstallerClean birden fazla sürücü veya paylaşım ({0}) olmadan devam etti ve hâlâ bunlardan biriyle karşılaştırılması gereken her dosyayı olduğu gibi bıraktı. Yeniden normal yanıt vermeye başladıklarında Yeniden tara'ya basın. |
| Something was using Windows Installer when the scan started, such as a Windows Update or a program installing, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan once that has finished. | Tarama başladığında bir şey Windows Installer'ı kullanıyordu, örneğin bir Windows güncelleştirmesi ya da kurulan bir program; bu yüzden InstallerClean, {InstallerFolder} klasöründeki, yüklü hiçbir programın kayıtlarında adı geçmeyen dosyaları sunmak yerine geri tuttu. O bittiğinde Yeniden tara'ya basın. |
| A Windows Installer transaction was suspended on this machine when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Resume or roll back that install, or restart Windows, then Re-scan. | Tarama başladığında bu makinede askıya alınmış bir Windows Installer işlemi vardı, bu yüzden InstallerClean, {InstallerFolder} klasöründeki, yüklü hiçbir programın kayıtlarında adı geçmeyen dosyaları sunmak yerine geri tuttu. O kurulumu sürdürün ya da geri alın veya Windows'u yeniden başlatın, ardından Yeniden tara'ya basın. |
| When the scan started, Windows had a file rename queued for the next restart that affects {InstallerFolder}, so InstallerClean held back the files there that no installed program's records name, rather than offering them. Restart Windows, then Re-scan. | Tarama başladığında Windows, bir sonraki yeniden başlatma için {InstallerFolder} klasörünü etkileyen bir dosya adı değişikliğini sıraya almıştı, bu yüzden InstallerClean oradaki, yüklü hiçbir programın kayıtlarında adı geçmeyen dosyaları sunmak yerine geri tuttu. Windows'u yeniden başlatın, ardından Yeniden tara'ya basın. |
| When the scan started, a file operation was queued for the next restart and InstallerClean couldn't tell which files it names, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows, then Re-scan. | Tarama başladığında bir sonraki yeniden başlatma için sıraya alınmış bir dosya işlemi vardı ve InstallerClean bu işlemin hangi dosyaları adlandırdığını bilemedi, bu yüzden {InstallerFolder} klasöründeki, yüklü hiçbir programın kayıtlarında adı geçmeyen dosyaları sunmak yerine geri tuttu. Windows'u yeniden başlatın, ardından Yeniden tara'ya basın. |
| When the scan started, Windows refused InstallerClean permission to check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Tarama başladığında Windows, InstallerClean'e Windows Installer'ın meşgul olup olmadığını denetleme izni vermedi, bu yüzden InstallerClean, {InstallerFolder} klasöründeki, yüklü hiçbir programın kayıtlarında adı geçmeyen dosyaları sunmak yerine geri tuttu. Windows'u yeniden başlatın ve Yeniden tara'ya basın. |
| When the scan started, InstallerClean couldn't read one of the Windows settings it checks, so it couldn't tell whether an installer operation was running or waiting for a restart, and it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Tarama başladığında InstallerClean denetlediği Windows ayarlarından birini okuyamadı, bu yüzden bir yükleyici işleminin sürüp sürmediğini ya da yeniden başlatma bekleyip beklemediğini bilemedi ve {InstallerFolder} klasöründeki, yüklü hiçbir programın kayıtlarında adı geçmeyen dosyaları sunmak yerine geri tuttu. Windows'u yeniden başlatın ve Yeniden tara'ya basın. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Tarama başladığında InstallerClean, Windows Installer'ın meşgul olup olmadığını denetleyemedi, bu yüzden {InstallerFolder} klasöründeki, yüklü hiçbir programın kayıtlarında adı geçmeyen dosyaları sunmak yerine geri tuttu. Windows'u yeniden başlatın ve Yeniden tara'ya basın. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, because Windows gave an error: {0} (error {1}). So it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Tarama başladığında InstallerClean, Windows Installer'ın meşgul olup olmadığını denetleyemedi, çünkü Windows bir hata verdi: {0} (hata {1}). Bu yüzden {InstallerFolder} klasöründeki, yüklü hiçbir programın kayıtlarında adı geçmeyen dosyaları sunmak yerine geri tuttu. Windows'u yeniden başlatın ve Yeniden tara'ya basın. |
| Windows Installer had something in progress or waiting when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan once that has cleared. | Tarama başladığında Windows Installer'ın sürmekte olan ya da bekleyen bir işi vardı, bu yüzden InstallerClean, {InstallerFolder} klasöründeki, yüklü hiçbir programın kayıtlarında adı geçmeyen dosyaları sunmak yerine geri tuttu. O sonuçlandığında Yeniden tara'ya basın. |
| This PC's clock went back or lost time while the scan was running, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan. | Tarama sürerken bu bilgisayarın saati geri gitti ya da geri kaldı, bu yüzden InstallerClean, {InstallerFolder} klasöründeki, yüklü hiçbir programın kayıtlarında adı geçmeyen dosyaları sunmak yerine geri tuttu. Yeniden tara'ya basın. |
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
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed. | Windows Installer kayıtları tamamen boş döndü: tek bir yüklü program ya da güncelleştirme bile önbellekteki bir kurulum dosyasında hak iddia etmiyor. Çalışan bir makinede bu olmaz (yeni kurulmuş bir Windows'ta bile bunlardan vardır), yani kayıtlar ya bozuk ya da okunamadı, ve bu yanıta inanan bir tarama {InstallerFolder} içindeki her dosyayı yanlışlıkla sahipsiz sayardı. InstallerClean bunun yerine durdu. Hiçbir şey kaldırılmadı. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer, InstallerClean'in yüklü olanları listelemesine izin vermedi. InstallerClean zaten yönetici olarak çalışıyordu, dolayısıyla onu yeniden yönetici olarak çalıştırmak bir şey değiştirmez. Bu liste olmadan önbellekteki hangi dosyaların hâlâ gerekli olduğunu güvenle söylemenin yolu yok, bu yüzden InstallerClean durdu. Hiçbir şey kaldırılmadı. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: it read {1} {2}, then an entry came back unreadable (error code {0}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer, InstallerClean'e yüklü programların okunabilir bir listesini veremedi: {1} {2} okudu, sonra bir kayıt okunamaz döndü (hata kodu {0}). InstallerClean, yalnızca kısmen okunmuş bir listeyle çalışmak yerine durdu. Hiçbir şey kaldırılmadı. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer, yüklü programlar listesinin sonunu hiç bildirmedi: InstallerClean {2} {3} okudu, sonra {0} kayıttan sonra vazgeçti (son hata kodu {1}). Sonu gelmeyen bir listeye güvenilemez, bu yüzden InstallerClean durdu. Hiçbir şey kaldırılmadı. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer, bir programın yama listesinin sonunu hiç bildirmedi: InstallerClean {2} {3} okudu, sonra {0} kayıttan sonra vazgeçti (son hata kodu {1}). Sonu gelmeyen bir listeye güvenilemez, bu yüzden InstallerClean durdu. Hiçbir şey kaldırılmadı. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean, Windows Installer kayıtlarını {InstallerFolder} içeriğiyle eşleştiremedi. Kayıtların işaret ettiklerinin neredeyse hiçbiri orada değil ve orada olanların neredeyse hiçbirinin adı hiçbir kayıtta geçmiyor, bu yüzden hiçbir dosyanın gereksiz olduğu gösterilemedi. Hiçbir şey sunulmadı ve hiçbir şey kaldırılmadı. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean, Windows Installer kayıtlarını {InstallerFolder} içeriğiyle eşleştiremedi. Klasörde dosyalar var, ama tek bir kayıt bile içindeki hiçbir şeye işaret etmiyor, bu yüzden hiçbir dosyanın gereksiz olduğu gösterilemedi. Hiçbir şey sunulmadı ve hiçbir şey kaldırılmadı. |
| InstallerClean can't scan {InstallerFolder} on this PC. Windows reports that folder as {0}, and InstallerClean only scans an Installer folder that Windows reports at its own path. Nothing has been offered and nothing has been removed. | InstallerClean bu bilgisayarda {InstallerFolder} klasörünü tarayamıyor. Windows bu klasörü {0} olarak bildiriyor ve InstallerClean yalnızca, Windows'un klasörün kendi yolunda bildirdiği bir Installer klasörünü tarar. Hiçbir şey sunulmadı ve hiçbir şey kaldırılmadı. |
| Windows reports that {InstallerFolder} doesn't exist on this PC, so InstallerClean stopped. Nothing has been offered and nothing has been removed. | Windows, {InstallerFolder} klasörünün bu bilgisayarda bulunmadığını bildiriyor, bu yüzden InstallerClean durdu. Hiçbir şey sunulmadı ve hiçbir şey kaldırılmadı. |
| Windows reported an error when InstallerClean tried to list the files in {InstallerFolder}, so it couldn't get a complete list and stopped. Nothing has been offered and nothing has been removed. | InstallerClean, {InstallerFolder} klasöründeki dosyaları listelemeye çalışırken Windows bir hata bildirdi, bu yüzden InstallerClean tam bir liste alamadı ve durdu. Hiçbir şey sunulmadı ve hiçbir şey kaldırılmadı. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean, neyin hâlâ gerekli olduğundan emin olmaya yetecek kadar Windows Installer kaydını okuyamadı: yüklü programların listesi eksik döndü, aynı kayıtları doğrudan kayıt defterinden okumak da hatalarla karşılaştı. Bir dosya, yalnızca onu adlandıran kayıt okunamayanlardan biri olduğu için sahipsiz görünebilirdi, bu yüzden InstallerClean durdu. Hiçbir şey kaldırılmadı. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean, {InstallerFolder} klasörünün gerçek yolunu Windows'a çözdüremedi, bu yüzden hiçbir dosyanın onun içinde olduğu gösterilemedi ve hiçbiri temizlik için sunulmadı. Bu tarama, klasör temiz olduğu için değil, o denetim başarısız olduğu için hiçbir şey bulamadı. Hiçbir şey kaldırılmadı. |
| Nothing was deleted | Hiçbir dosya silinmedi |
| Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. | Windows, InstallerClean'e Windows Installer'ın meşgul olup olmadığını denetleme izni vermedi, bu yüzden işlem sürerken bir dosyanın gerekli hale gelmesini dışlayamadı ve hiçbir şey silinmedi. |
| Nothing was moved | Hiçbir dosya taşınmadı |
| Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. | Windows, InstallerClean'e Windows Installer'ın meşgul olup olmadığını denetleme izni vermedi, bu yüzden işlem sürerken bir dosyanın gerekli hale gelmesini dışlayamadı ve hiçbir şey taşınmadı. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | InstallerClean, Windows Installer'ın iki programın yüklü yazılımı aynı anda değiştirmesini engellemek için kullandığı kilidi alamadı, bu yüzden bir dosyanın işin ortasında gerekli hale gelmeyeceğini kesinleştiremedi ve hiçbir şey silinmedi. Yeniden deneyin, sürerse Windows'u yeniden başlatın. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. Try again, and restart Windows if it keeps happening. | InstallerClean, Windows Installer'ın iki programın yüklü yazılımı aynı anda değiştirmesini engellemek için kullandığı kilidi alamadı, bu yüzden bir dosyanın işin ortasında gerekli hale gelmeyeceğini kesinleştiremedi ve hiçbir şey taşınmadı. Yeniden deneyin, sürerse Windows'u yeniden başlatın. |
| Invalid destination | Geçersiz hedef |
| Move stopped | Taşıma durduruldu |
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
| InstallerClean {0} is available. | InstallerClean {0} sürümü mevcut. |
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
| Couldn't reach GitHub. Check your internet connection and try again. | GitHub'a ulaşılamadı. İnternet bağlantınızı kontrol edip yeniden deneyin. |
| GitHub returned an error response. Try again in a few minutes. | GitHub bir hata yanıtı döndürdü. Birkaç dakika sonra yeniden deneyin. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | GitHub'ın yanıtı tanınan bir sürüm içermiyordu. Daha sonra yeniden deneyin ya da sürümler sayfasını doğrudan açın. |
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
| Opens the issue tracker on github.com in your browser. | github.com üzerindeki sorun izleyiciyi (Issues) tarayıcınızda açar. |
| If ticked, InstallerClean checks github for a newer version when you run it. | İşaretliyse InstallerClean, çalıştırdığınızda github üzerinde daha yeni bir sürüm olup olmadığını denetler. |
| Download it from github | github'dan indirin |
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
| InstallerClean stopped waiting for drive {0} and left alone any file still to be checked against it. Once it's responding normally, run the command again. | InstallerClean {0} sürücüsünü beklemeyi kesti ve hâlâ onunla karşılaştırılması gereken her dosyayı olduğu gibi bıraktı. Tekrar normal yanıt vermeye başladığında komutu yeniden çalıştırın. |
| InstallerClean stopped waiting for {0} and left alone any file still to be checked against it. Once it's responding normally, run the command again. | InstallerClean {0} konumunu beklemeyi kesti ve hâlâ onunla karşılaştırılması gereken her dosyayı olduğu gibi bıraktı. Tekrar normal yanıt vermeye başladığında komutu yeniden çalıştırın. |
| InstallerClean stopped waiting for more than one drive or share ({0}) and left alone any file still to be checked against one of them. Once they're responding normally, run the command again. | InstallerClean birden fazla sürücüyü veya paylaşımı ({0}) beklemeyi kesti ve hâlâ bunlardan biriyle karşılaştırılması gereken her dosyayı olduğu gibi bıraktı. Tekrar normal yanıt vermeye başladıklarında komutu yeniden çalıştırın. |
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
| Error: a file operation is queued for the next restart and InstallerClean can't tell which files it names, so it can't rule out {InstallerFolder}. Restart Windows before cleaning. | Hata: bir sonraki yeniden başlatma için sıraya alınmış bir dosya işlemi var ve InstallerClean bu işlemin hangi dosyaları adlandırdığını bilemiyor, bu yüzden {InstallerFolder} klasörünü dışlayamıyor. Temizlemeden önce Windows'u yeniden başlatın. |
| Error: InstallerClean couldn't read one of the registry values it checks before touching {InstallerFolder}, so it can't rule out a Windows Installer operation in flight or queued for the next restart. /m and /d are blocked. Restart Windows and try again. If the read still fails, this isn't a machine InstallerClean can clean. | Hata: InstallerClean, {InstallerFolder} klasörüne dokunmadan önce denetlediği kayıt defteri değerlerinden birini okuyamadı, bu yüzden sürmekte olan ya da bir sonraki yeniden başlatma için sıraya alınmış bir Windows Installer işlemini dışlayamıyor. /m ve /d engellendi. Windows'u yeniden başlatın ve tekrar deneyin. Okuma yine başarısız olursa, burası InstallerClean'in temizleyebileceği bir makine değil. |
| Error: InstallerClean couldn't check whether Windows Installer is busy, so it can't rule out an installation in progress. /m and /d are blocked. Restart Windows and try again. If the check still fails, this isn't a machine InstallerClean can clean. | Hata: InstallerClean, Windows Installer'ın meşgul olup olmadığını denetleyemedi, bu yüzden süren bir kurulumu dışlayamıyor. /m ve /d engellendi. Windows'u yeniden başlatın ve tekrar deneyin. Denetim yine başarısız olursa, burası InstallerClean'in temizleyebileceği bir makine değil. |
| Error: InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}), so it can't rule out an installation in progress. /m and /d are blocked. Restart Windows and try again. If the check still fails, this isn't a machine InstallerClean can clean. | Hata: InstallerClean, Windows Installer'ın meşgul olup olmadığını denetleyemedi, çünkü Windows bir hata verdi: {0} (hata {1}); bu yüzden süren bir kurulumu dışlayamıyor. /m ve /d engellendi. Windows'u yeniden başlatın ve tekrar deneyin. Denetim yine başarısız olursa, burası InstallerClean'in temizleyebileceği bir makine değil. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Hata: Windows Installer'ın sürmekte olan bir işi var, bu yüzden /m ve /d engellendi. InstallerClean değişmekte olan {InstallerFolder} klasörüne dokunmayacak. Bittiğinde yeniden deneyin. |
| Something was using Windows Installer when the scan started, such as a Windows Update or a program installing, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again once that has finished. | Tarama başladığında bir şey Windows Installer'ı kullanıyordu, örneğin bir Windows güncelleştirmesi ya da kurulan bir program; bu yüzden InstallerClean, {InstallerFolder} klasöründeki, yüklü hiçbir programın kayıtlarında adı geçmeyen dosyaları sunmak yerine geri tuttu. O bittiğinde komutu yeniden çalıştırın. |
| A Windows Installer transaction was suspended on this machine when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Resume or roll back that install, or restart Windows, then run it again. | Tarama başladığında bu makinede askıya alınmış bir Windows Installer işlemi vardı, bu yüzden InstallerClean, {InstallerFolder} klasöründeki, yüklü hiçbir programın kayıtlarında adı geçmeyen dosyaları sunmak yerine geri tuttu. O kurulumu sürdürün ya da geri alın veya Windows'u yeniden başlatın, ardından komutu yeniden çalıştırın. |
| When the scan started, Windows had a file rename queued for the next restart that affects {InstallerFolder}, so InstallerClean held back the files there that no installed program's records name, rather than offering them. Restart Windows, then run it again. | Tarama başladığında Windows, bir sonraki yeniden başlatma için {InstallerFolder} klasörünü etkileyen bir dosya adı değişikliğini sıraya almıştı, bu yüzden InstallerClean oradaki, yüklü hiçbir programın kayıtlarında adı geçmeyen dosyaları sunmak yerine geri tuttu. Windows'u yeniden başlatın, ardından komutu yeniden çalıştırın. |
| When the scan started, a file operation was queued for the next restart and InstallerClean couldn't tell which files it names, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows, then run it again. | Tarama başladığında bir sonraki yeniden başlatma için sıraya alınmış bir dosya işlemi vardı ve InstallerClean bu işlemin hangi dosyaları adlandırdığını bilemedi, bu yüzden {InstallerFolder} klasöründeki, yüklü hiçbir programın kayıtlarında adı geçmeyen dosyaları sunmak yerine geri tuttu. Windows'u yeniden başlatın, ardından komutu yeniden çalıştırın. |
| When the scan started, Windows refused InstallerClean permission to check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Tarama başladığında Windows, InstallerClean'e Windows Installer'ın meşgul olup olmadığını denetleme izni vermedi, bu yüzden InstallerClean, {InstallerFolder} klasöründeki, yüklü hiçbir programın kayıtlarında adı geçmeyen dosyaları sunmak yerine geri tuttu. Windows'u yeniden başlatın ve komutu yeniden çalıştırın. |
| When the scan started, InstallerClean couldn't read one of the Windows settings it checks, so it couldn't tell whether an installer operation was running or waiting for a restart, and it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Tarama başladığında InstallerClean denetlediği Windows ayarlarından birini okuyamadı, bu yüzden bir yükleyici işleminin sürüp sürmediğini ya da yeniden başlatma bekleyip beklemediğini bilemedi ve {InstallerFolder} klasöründeki, yüklü hiçbir programın kayıtlarında adı geçmeyen dosyaları sunmak yerine geri tuttu. Windows'u yeniden başlatın ve komutu yeniden çalıştırın. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Tarama başladığında InstallerClean, Windows Installer'ın meşgul olup olmadığını denetleyemedi, bu yüzden {InstallerFolder} klasöründeki, yüklü hiçbir programın kayıtlarında adı geçmeyen dosyaları sunmak yerine geri tuttu. Windows'u yeniden başlatın ve komutu yeniden çalıştırın. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, because Windows gave an error: {0} (error {1}). So it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Tarama başladığında InstallerClean, Windows Installer'ın meşgul olup olmadığını denetleyemedi, çünkü Windows bir hata verdi: {0} (hata {1}). Bu yüzden {InstallerFolder} klasöründeki, yüklü hiçbir programın kayıtlarında adı geçmeyen dosyaları sunmak yerine geri tuttu. Windows'u yeniden başlatın ve komutu yeniden çalıştırın. |
| Windows Installer had something in progress or waiting when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again once that has cleared. | Tarama başladığında Windows Installer'ın sürmekte olan ya da bekleyen bir işi vardı, bu yüzden InstallerClean, {InstallerFolder} klasöründeki, yüklü hiçbir programın kayıtlarında adı geçmeyen dosyaları sunmak yerine geri tuttu. O sonuçlandığında komutu yeniden çalıştırın. |
| This PC's clock went back or lost time while the scan was running, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again. | Tarama sürerken bu bilgisayarın saati geri gitti ya da geri kaldı, bu yüzden InstallerClean, {InstallerFolder} klasöründeki, yüklü hiçbir programın kayıtlarında adı geçmeyen dosyaları sunmak yerine geri tuttu. Komutu yeniden çalıştırın. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | Hata: InstallerClean, iki programın yüklü yazılımı aynı anda değiştirmesini engelleyen Windows Installer kilidini alamadı, bu yüzden bir dosyanın işin ortasında gerekli hale gelmeyeceğini kesinleştiremedi. Hiçbir şey silinmedi. Yeniden deneyin, sürerse Windows'u yeniden başlatın. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. Try again, and restart Windows if it keeps happening. | Hata: InstallerClean, iki programın yüklü yazılımı aynı anda değiştirmesini engelleyen Windows Installer kilidini alamadı, bu yüzden bir dosyanın işin ortasında gerekli hale gelmeyeceğini kesinleştiremedi. Hiçbir şey taşınmadı. Yeniden deneyin, sürerse Windows'u yeniden başlatın. |
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
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         Yalnızca tara - gereksizleri listele |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         Gereksiz dosyaları kalıcı olarak sil |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         Kayıtlı yedek klasörüne taşı |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m YOL     Belirtilen yola taşı |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli bitene kadar komut istemini tutar, böylece bir betik<br>ya da zamanlanmış görev onu bekleyebilir. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | Klasör kullanıcıya özeldir; zamanlanmış veya SYSTEM: /m YOL. |
| Before it scans, it checks for a newer version of InstallerClean.<br>When you run it yourself, it asks before updating, and updates if nobody<br>answers within a minute. When it runs on its own, in a scheduled task or<br>a script, it updates without asking.<br>Add /noupdate to skip the check. | Taramadan önce InstallerClean'in daha yeni bir sürümü olup olmadığını<br>denetler. Kendiniz çalıştırdığınızda güncellemeden önce sorar ve bir<br>dakika içinde kimse yanıtlamazsa güncellenir. Zamanlanmış bir görevde veya<br>betikte kendi kendine çalıştığında sormadan güncellenir.<br>Denetimi atlamak için /noupdate ekleyin. |
| Exit codes: | Çıkış kodları: |
|   0   success: the run did what it was asked |   0   başarılı: isteneni yaptı |
|   1   nothing processed: the run failed or was refused |   1   hiçbir şey işlenmedi: çalıştırma başarısız oldu ya da reddedildi |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   kısmi: bir kısmı işlendi, bir kısmı işlenmedi (hata ya da Ctrl+C) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  geçici: geçici bir durum çalıştırmayı engelledi (iletiye bakın) |
|   130 cancelled (Ctrl+C) |   130 iptal edildi (Ctrl+C) |
