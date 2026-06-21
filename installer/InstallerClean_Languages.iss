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

[CustomMessages]
english.UninstallApp=Uninstall InstallerClean
english.LaunchApp=Launch InstallerClean

italian.UninstallApp=Disinstalla InstallerClean
italian.LaunchApp=Esegui InstallerClean
italian.NameAndVersion=%1 %2