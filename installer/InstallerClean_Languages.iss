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

; The single-instance mutex (Global\InstallerClean_SingleInstance) is shared by
; the GUI and the CLI, so Setup and Uninstall can legitimately find InstallerClean
; "running" with nothing visible on screen: a command-line or scheduled-task run
; holds the same mutex. Inno's default text only says "close all instances", which
; strands the user in that case, so these overrides name both processes and point
; at Task Manager. Kept here rather than in each .isl so all 15 stay in one place.
english.SetupAppRunningError=InstallerClean is still running, so Setup cannot continue.%n%nThis may be the app window, or a command-line or scheduled-task run working in the background. Close the window, or end InstallerClean.exe and installerclean-cli.exe in Task Manager, then click OK to continue, or Cancel to exit.
english.UninstallAppRunningError=InstallerClean is still running, so it cannot be removed.%n%nThis may be the app window, or a command-line or scheduled-task run working in the background. Close the window, or end InstallerClean.exe and installerclean-cli.exe in Task Manager, then click OK to continue, or Cancel to exit.

indonesian.SetupAppRunningError=InstallerClean masih berjalan, sehingga pemasangan tidak dapat diteruskan.%n%nIni bisa berupa jendela aplikasi, atau proses baris perintah atau tugas terjadwal yang berjalan di latar belakang. Tutup jendela tersebut, atau akhiri InstallerClean.exe dan installerclean-cli.exe di Task Manager, lalu klik OK untuk meneruskan, atau Cancel untuk keluar.
indonesian.UninstallAppRunningError=InstallerClean masih berjalan, sehingga tidak dapat dihapus.%n%nIni bisa berupa jendela aplikasi, atau proses baris perintah atau tugas terjadwal yang berjalan di latar belakang. Tutup jendela tersebut, atau akhiri InstallerClean.exe dan installerclean-cli.exe di Task Manager, lalu klik OK untuk meneruskan, atau Cancel untuk keluar.

german.SetupAppRunningError=InstallerClean wird noch ausgeführt, daher kann das Setup nicht fortgesetzt werden.%n%nDies kann das App-Fenster sein oder ein im Hintergrund laufender Vorgang über die Befehlszeile oder eine geplante Aufgabe. Schließen Sie das Fenster oder beenden Sie InstallerClean.exe und installerclean-cli.exe im Task-Manager, und klicken Sie dann auf OK, um fortzufahren, oder auf Abbrechen, um zu beenden.
german.UninstallAppRunningError=InstallerClean wird noch ausgeführt, daher kann es nicht deinstalliert werden.%n%nDies kann das App-Fenster sein oder ein im Hintergrund laufender Vorgang über die Befehlszeile oder eine geplante Aufgabe. Schließen Sie das Fenster oder beenden Sie InstallerClean.exe und installerclean-cli.exe im Task-Manager, und klicken Sie dann auf OK, um fortzufahren, oder auf Abbrechen, um zu beenden.

spanish.SetupAppRunningError=InstallerClean se está ejecutando, por lo que la instalación no puede continuar.%n%nPuede ser la ventana de la aplicación, o una ejecución en segundo plano desde la línea de comandos o una tarea programada. Cierra la ventana, o finaliza InstallerClean.exe e installerclean-cli.exe en el Administrador de tareas, y luego haz clic en Aceptar para continuar, o en Cancelar para salir.
spanish.UninstallAppRunningError=InstallerClean se está ejecutando, por lo que no se puede desinstalar.%n%nPuede ser la ventana de la aplicación, o una ejecución en segundo plano desde la línea de comandos o una tarea programada. Cierra la ventana, o finaliza InstallerClean.exe e installerclean-cli.exe en el Administrador de tareas, y luego haz clic en Aceptar para continuar, o en Cancelar para salir.

french.SetupAppRunningError=InstallerClean est toujours en cours d'exécution, l'installation ne peut donc pas continuer.%n%nIl peut s'agir de la fenêtre de l'application, ou d'une exécution en arrière-plan via la ligne de commande ou une tâche planifiée. Fermez la fenêtre, ou arrêtez InstallerClean.exe et installerclean-cli.exe dans le Gestionnaire des tâches, puis cliquez sur OK pour continuer ou sur Annuler pour quitter.
french.UninstallAppRunningError=InstallerClean est toujours en cours d'exécution, il ne peut donc pas être désinstallé.%n%nIl peut s'agir de la fenêtre de l'application, ou d'une exécution en arrière-plan via la ligne de commande ou une tâche planifiée. Fermez la fenêtre, ou arrêtez InstallerClean.exe et installerclean-cli.exe dans le Gestionnaire des tâches, puis cliquez sur OK pour continuer ou sur Annuler pour quitter.

italian.SetupAppRunningError=InstallerClean è ancora in esecuzione, quindi l'installazione non può continuare.%n%nPotrebbe essere la finestra dell'applicazione, oppure un'esecuzione in background dalla riga di comando o da un'attività pianificata. Chiudi la finestra, oppure termina InstallerClean.exe e installerclean-cli.exe in Gestione attività, quindi fai clic su OK per continuare o su Annulla per uscire.
italian.UninstallAppRunningError=InstallerClean è ancora in esecuzione, quindi non può essere disinstallato.%n%nPotrebbe essere la finestra dell'applicazione, oppure un'esecuzione in background dalla riga di comando o da un'attività pianificata. Chiudi la finestra, oppure termina InstallerClean.exe e installerclean-cli.exe in Gestione attività, quindi fai clic su OK per continuare o su Annulla per uscire.

polish.SetupAppRunningError=InstallerClean jest nadal uruchomiony, więc instalacja nie może być kontynuowana.%n%nMoże to być okno aplikacji albo proces działający w tle z wiersza polecenia lub zaplanowanego zadania. Zamknij okno lub zakończ InstallerClean.exe i installerclean-cli.exe w Menedżerze zadań, a następnie kliknij OK, aby kontynuować, lub Anuluj, aby zakończyć.
polish.UninstallAppRunningError=InstallerClean jest nadal uruchomiony, więc nie można go odinstalować.%n%nMoże to być okno aplikacji albo proces działający w tle z wiersza polecenia lub zaplanowanego zadania. Zamknij okno lub zakończ InstallerClean.exe i installerclean-cli.exe w Menedżerze zadań, a następnie kliknij OK, aby kontynuować, lub Anuluj, aby zakończyć.

brazilianportuguese.SetupAppRunningError=O InstallerClean ainda está em execução, portanto a instalação não pode continuar.%n%nPode ser a janela do aplicativo, ou uma execução em segundo plano pela linha de comando ou por uma tarefa agendada. Feche a janela, ou finalize o InstallerClean.exe e o installerclean-cli.exe no Gerenciador de Tarefas e clique em OK para continuar, ou em Cancelar para sair.
brazilianportuguese.UninstallAppRunningError=O InstallerClean ainda está em execução, portanto não pode ser desinstalado.%n%nPode ser a janela do aplicativo, ou uma execução em segundo plano pela linha de comando ou por uma tarefa agendada. Feche a janela, ou finalize o InstallerClean.exe e o installerclean-cli.exe no Gerenciador de Tarefas e clique em OK para continuar, ou em Cancelar para sair.

vietnamese.SetupAppRunningError=InstallerClean vẫn đang chạy nên không thể tiếp tục cài đặt.%n%nĐó có thể là cửa sổ ứng dụng, hoặc một tiến trình dòng lệnh hay tác vụ đã lên lịch đang chạy ẩn. Hãy đóng cửa sổ, hoặc kết thúc InstallerClean.exe và installerclean-cli.exe trong Trình quản lý Tác vụ, rồi nhấn OK để tiếp tục, hoặc nhấn Hủy để thoát.
vietnamese.UninstallAppRunningError=InstallerClean vẫn đang chạy nên không thể gỡ cài đặt.%n%nĐó có thể là cửa sổ ứng dụng, hoặc một tiến trình dòng lệnh hay tác vụ đã lên lịch đang chạy ẩn. Hãy đóng cửa sổ, hoặc kết thúc InstallerClean.exe và installerclean-cli.exe trong Trình quản lý Tác vụ, rồi nhấn OK để tiếp tục, hoặc nhấn Hủy để thoát.

turkish.SetupAppRunningError=InstallerClean hâlâ çalışıyor, bu nedenle kurulum devam edemiyor.%n%nBu, uygulama penceresi ya da arka planda çalışan bir komut satırı veya zamanlanmış görev işlemi olabilir. Pencereyi kapatın ya da Görev Yöneticisi'nde InstallerClean.exe ve installerclean-cli.exe işlemlerini sonlandırın, ardından devam etmek için Tamam'a, çıkmak için İptal'e tıklayın.
turkish.UninstallAppRunningError=InstallerClean hâlâ çalışıyor, bu nedenle kaldırılamıyor.%n%nBu, uygulama penceresi ya da arka planda çalışan bir komut satırı veya zamanlanmış görev işlemi olabilir. Pencereyi kapatın ya da Görev Yöneticisi'nde InstallerClean.exe ve installerclean-cli.exe işlemlerini sonlandırın, ardından devam etmek için Tamam'a, çıkmak için İptal'e tıklayın.

russian.SetupAppRunningError=InstallerClean по-прежнему запущен, поэтому установка не может быть продолжена.%n%nЭто может быть окно приложения или фоновый процесс, запущенный из командной строки либо запланированной задачей. Закройте окно или завершите процессы InstallerClean.exe и installerclean-cli.exe в Диспетчере задач, затем нажмите «OK», чтобы продолжить, или «Отмена», чтобы выйти.
russian.UninstallAppRunningError=InstallerClean по-прежнему запущен, поэтому его нельзя удалить.%n%nЭто может быть окно приложения или фоновый процесс, запущенный из командной строки либо запланированной задачей. Закройте окно или завершите процессы InstallerClean.exe и installerclean-cli.exe в Диспетчере задач, затем нажмите «OK», чтобы продолжить, или «Отмена», чтобы выйти.

ukrainian.SetupAppRunningError=InstallerClean все ще працює, тому встановлення не може бути продовжено.%n%nЦе може бути вікно програми або фоновий процес, запущений із командного рядка чи запланованим завданням. Закрийте вікно або завершіть процеси InstallerClean.exe та installerclean-cli.exe у Диспетчері завдань, потім натисніть «OK», щоб продовжити, або «Скасувати», щоб вийти.
ukrainian.UninstallAppRunningError=InstallerClean все ще працює, тому його не можна видалити.%n%nЦе може бути вікно програми або фоновий процес, запущений із командного рядка чи запланованим завданням. Закрийте вікно або завершіть процеси InstallerClean.exe та installerclean-cli.exe у Диспетчері завдань, потім натисніть «OK», щоб продовжити, або «Скасувати», щоб вийти.

japanese.SetupAppRunningError=InstallerClean がまだ実行中のため、セットアップを続行できません。%n%nアプリのウィンドウのほか、コマンドラインやスケジュールされたタスクによるバックグラウンド処理の可能性もあります。ウィンドウを閉じるか、タスク マネージャーで InstallerClean.exe と installerclean-cli.exe を終了してから、[OK] をクリックして続行するか、[キャンセル] をクリックして終了してください。
japanese.UninstallAppRunningError=InstallerClean がまだ実行中のため、アンインストールできません。%n%nアプリのウィンドウのほか、コマンドラインやスケジュールされたタスクによるバックグラウンド処理の可能性もあります。ウィンドウを閉じるか、タスク マネージャーで InstallerClean.exe と installerclean-cli.exe を終了してから、[OK] をクリックして続行するか、[キャンセル] をクリックして終了してください。

chinesesimplified.SetupAppRunningError=InstallerClean 仍在运行，因此安装无法继续。%n%n这可能是应用程序窗口，也可能是在后台运行的命令行或计划任务进程。请关闭该窗口，或在任务管理器中结束 InstallerClean.exe 和 installerclean-cli.exe，然后点击“确定”继续，或点击“取消”退出。
chinesesimplified.UninstallAppRunningError=InstallerClean 仍在运行，因此无法卸载。%n%n这可能是应用程序窗口，也可能是在后台运行的命令行或计划任务进程。请关闭该窗口，或在任务管理器中结束 InstallerClean.exe 和 installerclean-cli.exe，然后点击“确定”继续，或点击“取消”退出。

korean.SetupAppRunningError=InstallerClean이 아직 실행 중이므로 설치를 계속할 수 없습니다.%n%n앱 창일 수도 있고, 백그라운드에서 실행 중인 명령줄 또는 예약 작업 프로세스일 수도 있습니다. 창을 닫거나 작업 관리자에서 InstallerClean.exe 및 installerclean-cli.exe를 종료한 다음 [확인]을 클릭하여 계속하거나 [취소]를 클릭하여 종료하세요.
korean.UninstallAppRunningError=InstallerClean이 아직 실행 중이므로 제거할 수 없습니다.%n%n앱 창일 수도 있고, 백그라운드에서 실행 중인 명령줄 또는 예약 작업 프로세스일 수도 있습니다. 창을 닫거나 작업 관리자에서 InstallerClean.exe 및 installerclean-cli.exe를 종료한 다음 [확인]을 클릭하여 계속하거나 [취소]를 클릭하여 종료하세요.

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
