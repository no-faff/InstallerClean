; InstallerClean installer language and message definitions, factored out of
; InstallerClean.iss so the main script stays on install logic and each added
; language touches only this file. Pulled in with #include.
;
; This file MUST keep its UTF-8 BOM. #include reads it as its own file, and it
; carries accented Italian (Welcome/Finished); without the BOM, Inno Setup 6
; reads it in the system ANSI codepage and the accents garble.
[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"

; Welcome/Finished are standard Inno [Messages], overridden per language with
; the language-name prefix. They must live here, NOT in [CustomMessages]:
; [CustomMessages] entries are only reachable via {cm:Name} and would not
; override the wizard's own text. Italian.isl supplies the rest of the wizard.
[Messages]
english.WelcomeLabel1=Welcome to InstallerClean setup
english.WelcomeLabel2=This will install InstallerClean on your computer.
english.FinishedHeadingLabel=Setup complete
english.FinishedLabel=InstallerClean has been installed on your computer.
english.ClickFinish=Click Finish to close setup.
italian.WelcomeLabel1=Benvenuto nell'installazione di InstallerClean
italian.WelcomeLabel2=Questo installerà InstallerClean nel computer.
italian.FinishedHeadingLabel=Installazione completata
italian.FinishedLabel=InstallerClean è stato installato nel computer.
italian.ClickFinish=Per chiudere l'installazione seleziona 'Fine'.

; AppVerName is left unset (it is deprecated), so Inno composes the wizard
; title bar and the Add/Remove Programs name from the NameAndVersion message:
; Default.isl is "%1 version %2", Italian.isl "%1 versione %2". Inno Setup 7
; drops that middle word as its new default; overriding NameAndVersion in both
; languages matches that, so the title reads "InstallerClean 1.9.1" rather than
; "InstallerClean version/versione 1.9.1".
english.NameAndVersion=%1 %2
italian.NameAndVersion=%1 %2

[CustomMessages]
english.UninstallApp=Uninstall InstallerClean
english.LaunchApp=Launch InstallerClean
italian.UninstallApp=Disinstalla InstallerClean
italian.LaunchApp=Esegui InstallerClean
