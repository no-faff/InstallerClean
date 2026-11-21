; InstallerClean installer language and message definitions, factored out of
; InstallerClean.iss so the main script stays on install logic and each added
; language touches only this file. Pulled in with #include.
;
; Save it as UTF-8. It carries non-ASCII overrides for many languages (CJK,
; Cyrillic, accented Latin), and a Unicode file pulled in with #include must be
; UTF-8, with or without a BOM.
[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "indonesian"; MessagesFile: "Languages\Indonesian.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "italian"; MessagesFile: "Languages\Italian.isl"
Name: "dutch"; MessagesFile: "compiler:Languages\Dutch.isl"
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "vietnamese"; MessagesFile: "Languages\Vietnamese.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "ukrainian"; MessagesFile: "compiler:Languages\Ukrainian.isl"
Name: "japanese"; MessagesFile: "Languages\Japanese.isl"
; Where no entry carries the exact language Windows displays, Inno takes the
; first entry with the same primary language, and for Chinese only one whose
; default ANSI code page also matches: 936 for Simplified (China, Singapore),
; 950 for Traditional (Taiwan, Hong Kong, Macau). So Chinese (Singapore) gets
; Simplified, and Hong Kong and Macau get Traditional, in either order of these
; two lines.
Name: "chinesetraditional"; MessagesFile: "Languages\ChineseTraditional.isl"
Name: "chinesesimplified"; MessagesFile: "Languages\ChineseSimplified.isl"
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"

; The Finished lines are standard Inno [Messages], overridden per language with
; the language-name prefix. They must live here, NOT in [CustomMessages]:
; [CustomMessages] entries are only reachable via {cm:Name} and would not
; override the wizard's own text. Each language's .isl supplies the rest of
; the wizard.
[Messages]
english.FinishedHeadingLabel=Setup complete
english.FinishedLabel=InstallerClean has been installed on your computer.
english.ClickFinish=Click Finish to close setup.

indonesian.FinishedHeadingLabel=Pemasangan selesai
indonesian.FinishedLabel=InstallerClean telah dipasang di komputer Anda.
indonesian.ClickFinish=Klik Selesai untuk menutup pemasangan.

german.FinishedHeadingLabel=Installation abgeschlossen
german.FinishedLabel=InstallerClean wurde auf Ihrem Computer installiert.
german.ClickFinish=Klicken Sie auf 'Fertigstellen', um die Installation zu beenden.

spanish.FinishedHeadingLabel=Instalación completada
spanish.FinishedLabel=InstallerClean se ha instalado en tu equipo.
spanish.ClickFinish=Haz clic en Finalizar para cerrar la instalación.

french.FinishedHeadingLabel=Installation terminée
french.FinishedLabel=InstallerClean a été installé sur votre ordinateur.
french.ClickFinish=Cliquez sur Terminer pour fermer l'installation.

italian.FinishedHeadingLabel=Installazione completata
italian.FinishedLabel=InstallerClean è stato installato nel computer.
italian.ClickFinish=Per chiudere l'installazione seleziona 'Fine'.

dutch.FinishedHeadingLabel=Installatie voltooid
dutch.FinishedLabel=InstallerClean is op uw computer geïnstalleerd.
dutch.ClickFinish=Klik op Voltooien om de installatie af te sluiten.

polish.FinishedHeadingLabel=Instalacja zakończona
polish.FinishedLabel=InstallerClean został zainstalowany na tym komputerze.
polish.ClickFinish=Kliknij Zakończ, aby zamknąć instalator.

brazilianportuguese.FinishedHeadingLabel=Instalação concluída
brazilianportuguese.FinishedLabel=O InstallerClean foi instalado no seu computador.
brazilianportuguese.ClickFinish=Clique em Concluir para fechar a instalação.

vietnamese.FinishedHeadingLabel=Cài đặt hoàn tất
vietnamese.FinishedLabel=InstallerClean đã được cài đặt lên máy tính của bạn.
vietnamese.ClickFinish=Bấm Hoàn tất để đóng trình cài đặt.

turkish.FinishedHeadingLabel=Kurulum tamamlandı
turkish.FinishedLabel=InstallerClean bilgisayarınıza kuruldu.
turkish.ClickFinish=Kurulumu kapatmak için Bitti'ye tıklayın.

russian.FinishedHeadingLabel=Установка завершена
russian.FinishedLabel=InstallerClean установлен на ваш компьютер.
russian.ClickFinish=Нажмите «Завершить», чтобы закрыть программу установки.

ukrainian.FinishedHeadingLabel=Встановлення завершено
ukrainian.FinishedLabel=InstallerClean встановлено на ваш комп'ютер.
ukrainian.ClickFinish=Натисніть «Готово», щоб закрити майстер встановлення.

japanese.FinishedHeadingLabel=セットアップ完了
japanese.FinishedLabel=InstallerClean がお使いのコンピューターにインストールされました。
japanese.ClickFinish=セットアップを終了するには「完了」を選択してください。

chinesetraditional.FinishedHeadingLabel=安裝完成
chinesetraditional.FinishedLabel=InstallerClean 已安裝在您的電腦上。
chinesetraditional.ClickFinish=按「完成」以結束安裝程式。

chinesesimplified.FinishedHeadingLabel=安装完成
chinesesimplified.FinishedLabel=InstallerClean 已安装在您的计算机上。
chinesesimplified.ClickFinish=单击“完成”以关闭安装程序。

korean.FinishedHeadingLabel=설치 완료
korean.FinishedLabel=InstallerClean이 컴퓨터에 설치되었습니다.
korean.ClickFinish=설치를 마치려면 '마침'을 클릭하세요.

; The single-instance mutex (Global\InstallerClean_SingleInstance) is shared by
; the GUI and the CLI, so Setup and Uninstall can legitimately find InstallerClean
; "running" with nothing visible on screen: a command-line or scheduled-task run
; holds the same mutex. Inno's default text only says "close all instances", which
; strands the user in that case, so these overrides name both processes and point
; at Task Manager. Kept here rather than in each .isl so every language stays in one place.
english.SetupAppRunningError=InstallerClean is still running, so Setup cannot continue.%n%nThis may be the app window, or a command-line or scheduled-task run working in the background. Close the window, or end InstallerClean.exe and installerclean-cli.exe in Task Manager, then click OK to continue, or Cancel to exit.
english.UninstallAppRunningError=InstallerClean is still running, so it cannot be removed.%n%nThis may be the app window, or a command-line or scheduled-task run working in the background. Close the window, or end InstallerClean.exe and installerclean-cli.exe in Task Manager, then click OK to continue, or Cancel to exit.

indonesian.SetupAppRunningError=InstallerClean masih berjalan, sehingga pemasangan tidak dapat diteruskan.%n%nIni bisa berupa jendela aplikasi, atau proses baris perintah atau tugas terjadwal yang berjalan di latar belakang. Tutup jendela tersebut, atau akhiri InstallerClean.exe dan installerclean-cli.exe di Task Manager, lalu klik Oke untuk meneruskan, atau Batal untuk keluar.
indonesian.UninstallAppRunningError=InstallerClean masih berjalan, sehingga tidak dapat dihapus.%n%nIni bisa berupa jendela aplikasi, atau proses baris perintah atau tugas terjadwal yang berjalan di latar belakang. Tutup jendela tersebut, atau akhiri InstallerClean.exe dan installerclean-cli.exe di Task Manager, lalu klik Oke untuk meneruskan, atau Batal untuk keluar.

german.SetupAppRunningError=InstallerClean wird noch ausgeführt, daher kann das Setup nicht fortgesetzt werden.%n%nDies kann das App-Fenster sein oder ein im Hintergrund laufender Vorgang über die Befehlszeile oder eine geplante Aufgabe. Schließen Sie das Fenster oder beenden Sie InstallerClean.exe und installerclean-cli.exe im Task-Manager, und klicken Sie dann auf OK, um fortzufahren, oder auf Abbrechen, um zu beenden.
german.UninstallAppRunningError=InstallerClean wird noch ausgeführt, daher kann es nicht deinstalliert werden.%n%nDies kann das App-Fenster sein oder ein im Hintergrund laufender Vorgang über die Befehlszeile oder eine geplante Aufgabe. Schließen Sie das Fenster oder beenden Sie InstallerClean.exe und installerclean-cli.exe im Task-Manager, und klicken Sie dann auf OK, um fortzufahren, oder auf Abbrechen, um zu beenden.

spanish.SetupAppRunningError=InstallerClean se está ejecutando, por lo que la instalación no puede continuar.%n%nPuede ser la ventana de la aplicación, o una ejecución en segundo plano desde la línea de comandos o una tarea programada. Cierra la ventana, o finaliza InstallerClean.exe e installerclean-cli.exe en el Administrador de tareas, y luego haz clic en Aceptar para continuar, o en Cancelar para salir.
spanish.UninstallAppRunningError=InstallerClean se está ejecutando, por lo que no se puede desinstalar.%n%nPuede ser la ventana de la aplicación, o una ejecución en segundo plano desde la línea de comandos o una tarea programada. Cierra la ventana, o finaliza InstallerClean.exe e installerclean-cli.exe en el Administrador de tareas, y luego haz clic en Aceptar para continuar, o en Cancelar para salir.

french.SetupAppRunningError=InstallerClean est toujours en cours d'exécution, l'installation ne peut donc pas continuer.%n%nIl peut s'agir de la fenêtre de l'application, ou d'une exécution en arrière-plan via la ligne de commande ou une tâche planifiée. Fermez la fenêtre, ou arrêtez InstallerClean.exe et installerclean-cli.exe dans le Gestionnaire des tâches, puis cliquez sur OK pour continuer ou sur Annuler pour quitter.
french.UninstallAppRunningError=InstallerClean est toujours en cours d'exécution, il ne peut donc pas être désinstallé.%n%nIl peut s'agir de la fenêtre de l'application, ou d'une exécution en arrière-plan via la ligne de commande ou une tâche planifiée. Fermez la fenêtre, ou arrêtez InstallerClean.exe et installerclean-cli.exe dans le Gestionnaire des tâches, puis cliquez sur OK pour continuer ou sur Annuler pour quitter.

italian.SetupAppRunningError=InstallerClean è ancora in esecuzione, quindi l'installazione non può continuare.%n%nPotrebbe essere la finestra dell'applicazione, oppure un'esecuzione in background dalla riga di comando o da un'attività pianificata. Chiudi la finestra, oppure termina InstallerClean.exe e installerclean-cli.exe in Gestione attività, quindi fai clic su OK per continuare o su Annulla per uscire.
italian.UninstallAppRunningError=InstallerClean è ancora in esecuzione, quindi non può essere disinstallato.%n%nPotrebbe essere la finestra dell'applicazione, oppure un'esecuzione in background dalla riga di comando o da un'attività pianificata. Chiudi la finestra, oppure termina InstallerClean.exe e installerclean-cli.exe in Gestione attività, quindi fai clic su OK per continuare o su Annulla per uscire.

dutch.SetupAppRunningError=InstallerClean wordt nog uitgevoerd, dus de installatie kan niet doorgaan.%n%nDit kan het venster van de app zijn, of een run via de opdrachtregel of een geplande taak die op de achtergrond werkt. Sluit het venster, of beëindig InstallerClean.exe en installerclean-cli.exe in Taakbeheer, en klik daarna op OK om door te gaan of op Annuleren om af te sluiten.
dutch.UninstallAppRunningError=InstallerClean wordt nog uitgevoerd, dus het kan niet worden verwijderd.%n%nDit kan het venster van de app zijn, of een run via de opdrachtregel of een geplande taak die op de achtergrond werkt. Sluit het venster, of beëindig InstallerClean.exe en installerclean-cli.exe in Taakbeheer, en klik daarna op OK om door te gaan of op Annuleren om af te sluiten.

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

japanese.SetupAppRunningError=InstallerClean がまだ実行中のため、セットアップを続行できません。%n%nアプリのウィンドウのほか、コマンドラインやスケジュールされたタスクによるバックグラウンド処理の可能性もあります。ウィンドウを閉じるか、タスク マネージャーで InstallerClean.exe と installerclean-cli.exe を終了してから、「OK」を選択して続行するか、「キャンセル」を選択して終了してください。
japanese.UninstallAppRunningError=InstallerClean がまだ実行中のため、アンインストールできません。%n%nアプリのウィンドウのほか、コマンドラインやスケジュールされたタスクによるバックグラウンド処理の可能性もあります。ウィンドウを閉じるか、タスク マネージャーで InstallerClean.exe と installerclean-cli.exe を終了してから、「OK」を選択して続行するか、「キャンセル」を選択して終了してください。

chinesetraditional.SetupAppRunningError=InstallerClean 仍在執行，因此安裝無法繼續。%n%n這可能是應用程式視窗，也可能是在背景執行的命令列或排定的工作。請關閉該視窗，或在工作管理員中結束 InstallerClean.exe 和 installerclean-cli.exe，然後按「確定」繼續，或按「取消」離開。
chinesetraditional.UninstallAppRunningError=InstallerClean 仍在執行，因此無法解除安裝。%n%n這可能是應用程式視窗，也可能是在背景執行的命令列或排定的工作。請關閉該視窗，或在工作管理員中結束 InstallerClean.exe 和 installerclean-cli.exe，然後按「確定」繼續，或按「取消」離開。

chinesesimplified.SetupAppRunningError=InstallerClean 仍在运行，因此安装无法继续。%n%n这可能是应用程序窗口，也可能是在后台运行的命令行或计划任务进程。请关闭该窗口，或在任务管理器中结束 InstallerClean.exe 和 installerclean-cli.exe，然后点击“确定”继续，或点击“取消”退出。
chinesesimplified.UninstallAppRunningError=InstallerClean 仍在运行，因此无法卸载。%n%n这可能是应用程序窗口，也可能是在后台运行的命令行或计划任务进程。请关闭该窗口，或在任务管理器中结束 InstallerClean.exe 和 installerclean-cli.exe，然后点击“确定”继续，或点击“取消”退出。

korean.SetupAppRunningError=InstallerClean이 아직 실행 중이므로 설치를 계속할 수 없습니다.%n%n앱 창일 수도 있고, 백그라운드에서 실행 중인 명령줄 또는 예약 작업 프로세스일 수도 있습니다. 창을 닫거나 작업 관리자에서 InstallerClean.exe 및 installerclean-cli.exe를 종료한 다음 [확인]을 클릭하여 계속하거나 [취소]를 클릭하여 종료하세요.
korean.UninstallAppRunningError=InstallerClean이 아직 실행 중이므로 제거할 수 없습니다.%n%n앱 창일 수도 있고, 백그라운드에서 실행 중인 명령줄 또는 예약 작업 프로세스일 수도 있습니다. 창을 닫거나 작업 관리자에서 InstallerClean.exe 및 installerclean-cli.exe를 종료한 다음 [확인]을 클릭하여 계속하거나 [취소]를 클릭하여 종료하세요.

[CustomMessages]
; Shown where the PC does not have .NET Framework 4.8, and Setup stops: the sentence,
; then the link to Microsoft's installer on a line of its own.
english.NeedsNetFramework48=InstallerClean needs .NET Framework 4.8, and this PC doesn't have it. Install it from Microsoft, restart the PC, then run this setup again.%n%nDownload .NET Framework 4.8 from Microsoft:%nhttps://go.microsoft.com/fwlink/?linkid=2088631
indonesian.NeedsNetFramework48=InstallerClean memerlukan .NET Framework 4.8, dan PC ini belum memilikinya. Pasang dari Microsoft, mulai ulang PC, lalu jalankan program pemasangan ini lagi.%n%nUnduh .NET Framework 4.8 dari Microsoft:%nhttps://go.microsoft.com/fwlink/?linkid=2088631
german.NeedsNetFramework48=InstallerClean benötigt .NET Framework 4.8, das auf diesem PC nicht installiert ist. Installieren Sie es von Microsoft, starten Sie den PC neu und führen Sie dieses Setup dann erneut aus.%n%n.NET Framework 4.8 von Microsoft herunterladen:%nhttps://go.microsoft.com/fwlink/?linkid=2088631
spanish.NeedsNetFramework48=InstallerClean necesita .NET Framework 4.8, y este PC no lo tiene. Instálalo desde Microsoft, reinicia el PC y luego vuelve a ejecutar este programa de instalación.%n%nDescarga .NET Framework 4.8 de Microsoft:%nhttps://go.microsoft.com/fwlink/?linkid=2088631
french.NeedsNetFramework48=InstallerClean nécessite .NET Framework 4.8, qui n'est pas installé sur ce PC. Installez-le depuis le site de Microsoft, redémarrez le PC, puis relancez ce programme d'installation.%n%nTélécharger .NET Framework 4.8 sur le site de Microsoft :%nhttps://go.microsoft.com/fwlink/?linkid=2088631
italian.NeedsNetFramework48=InstallerClean richiede .NET Framework 4.8, che non è presente in questo PC. Installalo dal sito di Microsoft, riavvia il PC, poi esegui di nuovo questo programma di installazione.%n%nScarica .NET Framework 4.8 da Microsoft:%nhttps://go.microsoft.com/fwlink/?linkid=2088631
dutch.NeedsNetFramework48=InstallerClean heeft .NET Framework 4.8 nodig, maar dat staat niet op deze pc. Installeer het van Microsoft, herstart de pc en voer deze installatie daarna opnieuw uit.%n%nDownload .NET Framework 4.8 van Microsoft:%nhttps://go.microsoft.com/fwlink/?linkid=2088631
polish.NeedsNetFramework48=InstallerClean wymaga .NET Framework 4.8, którego nie ma na tym komputerze. Zainstaluj go ze strony Microsoftu, uruchom ponownie komputer, a potem znów uruchom ten instalator.%n%nPobierz .NET Framework 4.8 ze strony Microsoftu:%nhttps://go.microsoft.com/fwlink/?linkid=2088631
brazilianportuguese.NeedsNetFramework48=O InstallerClean precisa do .NET Framework 4.8, que não está instalado neste PC. Instale-o pelo site da Microsoft, reinicie o PC e depois execute este instalador de novo.%n%nBaixe o .NET Framework 4.8 no site da Microsoft:%nhttps://go.microsoft.com/fwlink/?linkid=2088631
vietnamese.NeedsNetFramework48=InstallerClean cần .NET Framework 4.8, nhưng PC này chưa có. Hãy cài đặt nó từ Microsoft, khởi động lại PC, rồi chạy lại trình cài đặt này.%n%nTải .NET Framework 4.8 về từ Microsoft:%nhttps://go.microsoft.com/fwlink/?linkid=2088631
turkish.NeedsNetFramework48=InstallerClean için .NET Framework 4.8 gerekiyor ve bu bilgisayarda yüklü değil. Microsoft'tan yükleyin, bilgisayarı yeniden başlatın, ardından bu kurulumu yeniden çalıştırın.%n%n.NET Framework 4.8'i Microsoft'tan indirin:%nhttps://go.microsoft.com/fwlink/?linkid=2088631
russian.NeedsNetFramework48=Для InstallerClean нужен .NET Framework 4.8, а на этом компьютере его нет. Установите его с сайта Microsoft, перезагрузите компьютер, а затем снова запустите эту программу установки.%n%nСкачать .NET Framework 4.8 с сайта Microsoft:%nhttps://go.microsoft.com/fwlink/?linkid=2088631
ukrainian.NeedsNetFramework48=Для InstallerClean потрібен .NET Framework 4.8, а на цьому комп'ютері його немає. Встановіть його з сайту Microsoft, перезавантажте комп'ютер, а потім знову запустіть цю програму встановлення.%n%nЗавантажити .NET Framework 4.8 з сайту Microsoft:%nhttps://go.microsoft.com/fwlink/?linkid=2088631
japanese.NeedsNetFramework48=InstallerClean には .NET Framework 4.8 が必要ですが、この PC にはインストールされていません。Microsoft からインストールして PC を再起動し、このセットアップをもう一度実行してください。%n%nMicrosoft から .NET Framework 4.8 をダウンロード：%nhttps://go.microsoft.com/fwlink/?linkid=2088631
chinesetraditional.NeedsNetFramework48=InstallerClean 需要 .NET Framework 4.8，但這台電腦尚未安裝。請從 Microsoft 安裝，重新啟動電腦，然後再次執行此安裝程式。%n%n從 Microsoft 下載 .NET Framework 4.8：%nhttps://go.microsoft.com/fwlink/?linkid=2088631
chinesesimplified.NeedsNetFramework48=InstallerClean 需要 .NET Framework 4.8，但这台电脑尚未安装。请从 Microsoft 安装它，重启电脑，然后再次运行此安装程序。%n%n从 Microsoft 下载 .NET Framework 4.8：%nhttps://go.microsoft.com/fwlink/?linkid=2088631
korean.NeedsNetFramework48=InstallerClean에는 .NET Framework 4.8이 필요하지만 이 PC에는 설치되어 있지 않습니다. Microsoft에서 설치하고 PC를 다시 시작한 다음 이 설치 프로그램을 다시 실행하세요.%n%nMicrosoft에서 .NET Framework 4.8 내려받기:%nhttps://go.microsoft.com/fwlink/?linkid=2088631

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

dutch.UninstallApp=InstallerClean verwijderen
dutch.LaunchApp=InstallerClean starten

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

chinesetraditional.UninstallApp=解除安裝 InstallerClean
chinesetraditional.LaunchApp=啟動 InstallerClean

chinesesimplified.UninstallApp=卸载 InstallerClean
chinesesimplified.LaunchApp=启动 InstallerClean

korean.UninstallApp=InstallerClean 제거
korean.LaunchApp=InstallerClean 실행
