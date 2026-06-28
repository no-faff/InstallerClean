; InstallerClean installer language and message definitions, factored out of
; InstallerClean.iss so the main script stays on install logic and each added
; language touches only this file. Pulled in with #include.
;
; This file MUST keep its UTF-8 BOM. #include reads it as its own file, and it
; carries non-ASCII overrides for many languages (CJK, Cyrillic, accented
; Latin); without the BOM, Inno Setup 6 reads it in the system ANSI codepage
; and those characters garble.
[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "indonesian"; MessagesFile: "Languages\Indonesian.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "vietnamese"; MessagesFile: "Languages\Vietnamese.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "ukrainian"; MessagesFile: "compiler:Languages\Ukrainian.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "chinesesimplified"; MessagesFile: "Languages\ChineseSimplified.isl"
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"

; Welcome/Finished are standard Inno [Messages], overridden per language with
; the language-name prefix. They must live here, NOT in [CustomMessages]:
; [CustomMessages] entries are only reachable via {cm:Name} and would not
; override the wizard's own text. Each language's .isl supplies the rest of
; the wizard.
[Messages]
english.WelcomeLabel1=Welcome to InstallerClean setup
english.WelcomeLabel2=This will install InstallerClean on your computer.
english.FinishedHeadingLabel=Setup complete
english.FinishedLabel=InstallerClean has been installed on your computer.
english.ClickFinish=Click Finish to close setup.

indonesian.WelcomeLabel1=Selamat datang di pemasangan InstallerClean
indonesian.WelcomeLabel2=Ini akan memasang InstallerClean di komputer Anda.
indonesian.FinishedHeadingLabel=Pemasangan selesai
indonesian.FinishedLabel=InstallerClean telah dipasang di komputer Anda.
indonesian.ClickFinish=Klik Selesai untuk menutup pemasangan.

german.WelcomeLabel1=Willkommen bei der Installation von InstallerClean
german.WelcomeLabel2=Dies wird InstallerClean auf Ihrem Computer installieren.
german.FinishedHeadingLabel=Installation abgeschlossen
german.FinishedLabel=InstallerClean wurde auf Ihrem Computer installiert.
german.ClickFinish=Klicken Sie auf 'Fertigstellen', um die Installation zu beenden.

spanish.WelcomeLabel1=Te damos la bienvenida a la instalación de InstallerClean
spanish.WelcomeLabel2=Esto instalará InstallerClean en tu equipo.
spanish.FinishedHeadingLabel=Instalación completada
spanish.FinishedLabel=InstallerClean se ha instalado en tu equipo.
spanish.ClickFinish=Haz clic en Finalizar para cerrar la instalación.

french.WelcomeLabel1=Bienvenue dans l'installation d'InstallerClean
french.WelcomeLabel2=Ceci installera InstallerClean sur votre ordinateur.
french.FinishedHeadingLabel=Installation terminée
french.FinishedLabel=InstallerClean a été installé sur votre ordinateur.
french.ClickFinish=Cliquez sur Terminer pour fermer l'installation.

italian.WelcomeLabel1=Benvenuto nell'installazione di InstallerClean
italian.WelcomeLabel2=Questo installerà InstallerClean nel computer.
italian.FinishedHeadingLabel=Installazione completata
italian.FinishedLabel=InstallerClean è stato installato nel computer.
italian.ClickFinish=Per chiudere l'installazione seleziona 'Fine'.

polish.WelcomeLabel1=Witamy w instalatorze InstallerClean
polish.WelcomeLabel2=InstallerClean zostanie zainstalowany na tym komputerze.
polish.FinishedHeadingLabel=Instalacja zakończona
polish.FinishedLabel=InstallerClean został zainstalowany na tym komputerze.
polish.ClickFinish=Kliknij Zakończ, aby zamknąć instalator.

brazilianportuguese.WelcomeLabel1=Bem-vindo à instalação do InstallerClean
brazilianportuguese.WelcomeLabel2=Isto instalará o InstallerClean no seu computador.
brazilianportuguese.FinishedHeadingLabel=Instalação concluída
brazilianportuguese.FinishedLabel=O InstallerClean foi instalado no seu computador.
brazilianportuguese.ClickFinish=Clique em Concluir para fechar a instalação.

vietnamese.WelcomeLabel1=Chào mừng bạn đến với trình cài đặt InstallerClean
vietnamese.WelcomeLabel2=Chương trình này sẽ cài đặt InstallerClean lên máy tính của bạn.
vietnamese.FinishedHeadingLabel=Cài đặt hoàn tất
vietnamese.FinishedLabel=InstallerClean đã được cài đặt lên máy tính của bạn.
vietnamese.ClickFinish=Bấm Hoàn tất để đóng trình cài đặt.

turkish.WelcomeLabel1=InstallerClean kurulumuna hoş geldiniz
turkish.WelcomeLabel2=Bu işlem InstallerClean'i bilgisayarınıza kuracak.
turkish.FinishedHeadingLabel=Kurulum tamamlandı
turkish.FinishedLabel=InstallerClean bilgisayarınıza kuruldu.
turkish.ClickFinish=Kurulumu kapatmak için Son'a tıklayın.

russian.WelcomeLabel1=Добро пожаловать в программу установки InstallerClean
russian.WelcomeLabel2=Программа установит InstallerClean на ваш компьютер.
russian.FinishedHeadingLabel=Установка завершена
russian.FinishedLabel=InstallerClean установлен на ваш компьютер.
russian.ClickFinish=Нажмите «Завершить», чтобы закрыть программу установки.

ukrainian.WelcomeLabel1=Ласкаво просимо до встановлення InstallerClean
ukrainian.WelcomeLabel2=InstallerClean буде встановлено на ваш комп'ютер.
ukrainian.FinishedHeadingLabel=Встановлення завершено
ukrainian.FinishedLabel=InstallerClean встановлено на ваш комп'ютер.
ukrainian.ClickFinish=Натисніть «Завершити», щоб закрити майстер встановлення.

japanese.WelcomeLabel1=InstallerClean セットアップへようこそ
japanese.WelcomeLabel2=InstallerClean をお使いのコンピューターにインストールします。
japanese.FinishedHeadingLabel=セットアップ完了
japanese.FinishedLabel=InstallerClean がお使いのコンピューターにインストールされました。
japanese.ClickFinish=「完了」をクリックするとセットアップを終了します。

chinesesimplified.WelcomeLabel1=欢迎使用 InstallerClean 安装程序
chinesesimplified.WelcomeLabel2=这将在您的计算机上安装 InstallerClean。
chinesesimplified.FinishedHeadingLabel=安装完成
chinesesimplified.FinishedLabel=InstallerClean 已安装在您的计算机上。
chinesesimplified.ClickFinish=单击“完成”以关闭安装程序。

korean.WelcomeLabel1=InstallerClean 설치를 시작합니다
korean.WelcomeLabel2=InstallerClean을 컴퓨터에 설치합니다.
korean.FinishedHeadingLabel=설치 완료
korean.FinishedLabel=InstallerClean이 컴퓨터에 설치되었습니다.
korean.ClickFinish=설치를 마치려면 '마침'을 클릭하세요.

[CustomMessages]
english.UninstallApp=Uninstall InstallerClean
english.LaunchApp=Launch InstallerClean

indonesian.UninstallApp=Hapus instalasi InstallerClean
indonesian.LaunchApp=Jalankan InstallerClean

german.UninstallApp=InstallerClean deinstallieren
german.LaunchApp=InstallerClean starten

spanish.UninstallApp=Desinstalar InstallerClean
spanish.LaunchApp=Ejecutar InstallerClean

french.UninstallApp=Désinstaller InstallerClean
french.LaunchApp=Lancer InstallerClean

italian.UninstallApp=Disinstalla InstallerClean
italian.LaunchApp=Esegui InstallerClean

polish.UninstallApp=Odinstaluj InstallerClean
polish.LaunchApp=Uruchom InstallerClean

brazilianportuguese.UninstallApp=Desinstalar o InstallerClean
brazilianportuguese.LaunchApp=Executar o InstallerClean

vietnamese.UninstallApp=Gỡ cài đặt InstallerClean
vietnamese.LaunchApp=Khởi chạy InstallerClean

turkish.UninstallApp=InstallerClean'i kaldır
turkish.LaunchApp=InstallerClean'i çalıştır

russian.UninstallApp=Удалить InstallerClean
russian.LaunchApp=Запустить InstallerClean

ukrainian.UninstallApp=Видалити InstallerClean
ukrainian.LaunchApp=Запустити InstallerClean

japanese.UninstallApp=InstallerClean をアンインストール
japanese.LaunchApp=InstallerClean を起動

chinesesimplified.UninstallApp=卸载 InstallerClean
chinesesimplified.LaunchApp=启动 InstallerClean

korean.UninstallApp=InstallerClean 제거
korean.LaunchApp=InstallerClean 실행
