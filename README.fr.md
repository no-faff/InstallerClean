<p align="center">
  <a href="README.md">English</a> · <a href="README.zh-CN.md">简体中文</a> · <a href="README.es.md">Español</a> · <a href="README.ja.md">日本語</a> · <a href="README.pt-BR.md">Português (BR)</a> · <a href="README.ru.md">Русский</a> · <strong>Français</strong>
</p>

<p align="center"><em>Cette page est traduite, mais l'interface de l'application est actuellement en anglais uniquement.</em></p>

<p align="center">
  <img src="docs/icon.png" width="280" alt="InstallerClean">
</p>

<p align="center"><em>🎶 What's my line? I'm happy <a href="https://www.youtube.com/watch?v=HM-jHhUZfFI">cleaning Windows</a></em></p>

<h1 align="center">InstallerClean</h1>

<p align="center"><strong>Une alternative open source à <a href="https://www.homedev.com.au/free/patchcleaner">PatchCleaner</a>. Nettoyez en toute sécurité <code>C:\Windows\Installer</code>, le dossier caché de Windows qui grignote silencieusement votre espace disque.</strong></p>

<p align="center"><em>Utilisez-le une fois. Gagnez peut-être un peu d'espace. Puis jetez-le.</em></p>

<p align="center">
  <a href="LICENSE"><img src="https://img.shields.io/badge/licence-MIT-blue.svg" alt="Licence : MIT"></a>
  <a href="https://dotnet.microsoft.com/download/dotnet/10.0"><img src="https://img.shields.io/badge/.NET-10.0-purple.svg" alt=".NET 10"></a>
  <a href="https://github.com/no-faff/InstallerClean/actions/workflows/ci.yml"><img src="https://github.com/no-faff/InstallerClean/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases"><img src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4.svg" alt="Windows 10/11"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases/latest"><img src="https://img.shields.io/github/v/release/no-faff/InstallerClean" alt="Version GitHub"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases"><img src="https://img.shields.io/github/downloads/no-faff/InstallerClean/total?cacheSeconds=300" alt="Total des téléchargements"></a>
</p>

![Capture d'écran d'InstallerClean après un nettoyage réussi : 965 Mo libérés, 68 fichiers supprimés](docs/screenshots/04d-deleted-freed-success.webp)

- **En bref :** Trouve et supprime les fichiers inutiles dans `C:\Windows\Installer`, le dossier caché que Windows ne nettoie jamais.
- **Combien d'espace :** Ça dépend de vos logiciels. Sur ma machine, c'était presque 1 Go. Un utilisateur d'InstallerClean a [signalé](https://github.com/no-faff/InstallerClean/issues/12#issuecomment-4395580816) 25 Go. Avec Adobe Acrobat, ça peut dépasser 100 Go. Ou ne rien donner du tout. L'important, c'est que c'est rapide et gratuit ; tout ce qui peut être supprimé le sera.
- **Est-ce sûr :** Oui. Ne supprime que les fichiers que Windows lui-même déclare inutiles. La suppression envoie les fichiers à la Corbeille et ne supprime jamais définitivement sans vous demander. Le déplacement, lui, vous permet de les garder en lieu sûr.
- **Comment l'obtenir :** [Téléchargez la dernière version](../../releases/latest), lancez-la, c'est tout.

## Sommaire

- [Le dossier dont personne ne vous parle](#le-dossier-dont-personne-ne-vous-parle)
- [La recherche d'aide](#la-recherche-daide)
- [Ce qu'il fait](#ce-quil-fait)
- [Captures d'écran](#captures-décran)
- [Comment ça marche](#comment-ça-marche)
- [Est-ce sûr ?](#est-ce-sûr-)
- [Si un fichier manque bel et bien dans C:\Windows\Installer](#recovery)
- [Accessibilité](#accessibilité)
- [Ce qu'il ne fait pas](#ce-quil-ne-fait-pas)
- [FAQ](#faq)
- [Téléchargement](#téléchargement)
- [Comparaison avec PatchCleaner](#comparaison-avec-patchcleaner)
- [Ligne de commande](#ligne-de-commande)
- [Prérequis](#prérequis)
- [Compilation depuis les sources](#compilation-depuis-les-sources)
- [Contribuer](#contribuer)
- [Soutenir le projet](#soutenir-le-projet)
- [Historique des étoiles](#historique-des-étoiles)
- [Licence](#licence)

---

## Le dossier dont personne ne vous parle

Il existe un dossier caché sur tout PC Windows, nommé `C:\Windows\Installer`. Chaque fois que vous installez un logiciel basé sur Windows Installer, ou que vous appliquez un correctif à Microsoft Office, Adobe Acrobat, Visual Studio ou toute autre application `.msi`, une copie du programme d'installation ou du fichier de correctif `.msp` atterrit dans ce dossier. Et y reste.

Quand vous désinstallez le logiciel, les fichiers restent. Quand un nouveau correctif en remplace un ancien, les deux restent. Windows ne les nettoie jamais. Le Nettoyage de disque n'y touche pas. DISM concerne un tout autre dossier. Au fil des années, le dossier grossit : 10 Go, 30 Go, 50 Go. Sur les machines chargées de logiciels MSI (Acrobat est un coupable récurrent), il peut [dépasser les 100 Go](https://www.reddit.com/r/sysadmin/comments/1oxcrmh/acrobat_filling_up_the_cwindowsinstaller_folder/).

Ce ne sont pas des fichiers temporaires qui réapparaissent dès que vous fermez un outil de nettoyage. C'est du véritable poids mort : de vieux programmes d'installation de logiciels désinstallés depuis des années, et des correctifs remplacés trois fois plutôt qu'une. Une fois supprimés, ils ne reviennent pas.

**Si vous cherchez un moyen simple de libérer de l'espace disque sous Windows, ce dossier est l'un des meilleurs endroits où commencer.** InstallerClean repère les fichiers inutiles et les supprime sans risque.

[PatchCleaner](https://www.homedev.com.au/free/patchcleaner) a longtemps été l'outil de référence pour cette tâche, mais il n'a pas été mis à jour depuis mars 2016 et son code est fermé. InstallerClean en est une alternative open source, avec la détection des correctifs remplacés (qui repère les correctifs Acrobat que PatchCleaner exclut) et une interface moderne.

## La recherche d'aide

Si vous avez déjà cherché de l'aide pour ce dossier, vous savez comment ça se passe. Quelqu'un demande comment le nettoyer. On lui répond de lancer le Nettoyage de disque. Il essaie. Ça libère [600 Mo sur un dossier de 180 Go](https://learn.microsoft.com/en-us/answers/questions/4238108/windows-installer-folder-has-occupied-180gb). Et le fil de discussion retombe dans le silence.

> *« Tous les fils que j'ai trouvés ont tendance à recommander les mêmes choses, qui ne résolvent pas le problème, avant de s'éteindre. »*
>
> ksparks519, r/Windows10 (traduit de l'anglais)

Ou bien on lui dit de ne surtout pas y toucher. Dans un fil, une personne dont le dossier Installer pesait 60 Go s'est vu répondre [« n'y touchez pas »](https://www.reddit.com/r/techsupport/comments/1hw4suq/my_windows_installer_folder_is_like_60gb_so_i/). Quand elle a demandé ce qu'elle devait faire à la place, la réponse a été : *« Je viens de te le dire. »*

Le conseil habituel confond la suppression de fichiers au hasard (ce qui est réellement dangereux) avec la suppression de fichiers que Windows lui-même signale comme inutiles (ce qui ne l'est pas). InstallerClean, lui, fait la seconde.

Si vous avez déjà cherché de l'aide à ce sujet, vous avez sans doute déjà trouvé [PatchCleaner](https://www.homedev.com.au/free/patchcleaner) de [John Crawford](https://www.homedev.com.au/). C'est une excellente application. Je l'ai téléchargée et elle a fait exactement ce qu'elle annonçait : libérer énormément d'espace. La seule chose qu'elle ne gère pas, ce sont les correctifs Adobe ; elle les exclut par défaut, et sur les machines où Adobe est le plus gros responsable, beaucoup de fichiers supprimables restent en place :

> *« J'ai téléchargé PatchCleaner pour supprimer les fichiers `.msp` orphelins... mais 29 Go de fichiers sont "exclus par les filtres", donc PatchCleaner ne semble pas d'une grande aide. »*
>
> HeatherBunny1111, [r/techsupport](https://www.reddit.com/r/techsupport/comments/1qc4tcf/how_to_delete_msp_files_safely/) (traduit de l'anglais)

InstallerClean détecte les correctifs remplacés par des mises à jour plus récentes et les marque comme supprimables, y compris les correctifs Acrobat que PatchCleaner exclut.

## Ce qu'il fait

1. **Analyse** `C:\Windows\Installer` à la recherche de fichiers `.msi` et `.msp`
2. **Interroge** l'API Windows Installer pour repérer les fichiers encore enregistrés
3. **Affiche** ce qui est utile et ce qui ne l'est pas, avec les tailles
4. **Supprime** les fichiers inutiles : envoi à la Corbeille (si elle n'est pas disponible pour le lecteur, l'application vous demande confirmation avant toute suppression définitive), ou déplacement vers un dossier de votre choix

Aucune activité réseau automatique. Deux boutons facultatifs déclenchent un seul appel HTTPS quand vous les activez : **Vérifier les mises à jour** dans À propos, et **Envoyer le résumé** sur l'écran de fin. Voir [Ce qu'il ne fait pas](#ce-quil-ne-fait-pas) plus bas pour le détail complet.

## Captures d'écran

<p>
  <img src="docs/screenshots/01-initial-scan.webp" alt="Écran de démarrage affichant l'analyse en cours, avec 68 fichiers à nettoyer déjà trouvés" width="900"><br>
  <em>Analyse initiale. Très rapide.</em>
</p>

<p>
  <img src="docs/screenshots/02-main-window.webp" alt="Fenêtre principale affichant 116 fichiers encore utilisés et 68 fichiers à nettoyer" width="900"><br>
  <em>Résultats : ce qui est utilisé, ce qui est supprimable.</em>
</p>

<p>
  <img src="docs/screenshots/03a-details-registered.webp" alt="Fenêtre des fichiers enregistrés listant les produits installés et leurs métadonnées issues de la base de données de Windows Installer" width="900"><br>
  <em>Les fichiers encore utilisés, avec les métadonnées lues dans la base de données de Windows Installer.</em>
</p>

<p>
  <img src="docs/screenshots/03b-details-unused.webp" alt="Fenêtre des fichiers inutiles listant les fichiers .msi supprimables avec la raison de leur suppression" width="900"><br>
  <em>Les fichiers devenus inutiles.</em>
</p>

<p>
  <img src="docs/screenshots/04b-Delete-dialogue.webp" alt="Boîte de dialogue de confirmation de suppression indiquant que 68 fichiers (965 Mo) iront à la Corbeille" width="900"><br>
  <em>Une confirmation avant chaque action. La suppression envoie à la Corbeille ; le déplacement place les fichiers à l'endroit de votre choix.</em>
</p>

<p>
  <img src="docs/screenshots/04d-deleted-freed-success.webp" alt="Écran de réussite indiquant 965 Mo libérés après une suppression, avec 68 fichiers envoyés à la Corbeille" width="900"><br>
  <em>Après une suppression réussie.</em>
</p>

<p>
  <img src="docs/screenshots/06a-scanned-again-all-clean.webp" alt="Écran « tout est propre » affiché quand une nouvelle analyse ne trouve plus rien à supprimer" width="900"><br>
  <em>Après une nouvelle analyse. Plus rien à nettoyer.</em>
</p>

## Comment ça marche

InstallerClean identifie deux catégories de fichiers inutiles.

**Les fichiers orphelins** sont des programmes d'installation et des correctifs laissés derrière eux après la désinstallation d'un logiciel. Windows ne les référence plus, mais les fichiers occupent toujours de la place dans le dossier.

**Les correctifs remplacés** sont d'anciens correctifs `.msp` qui ont cédé la place à de plus récents. Windows les marque comme remplacés dans sa propre base de données, mais ne les supprime jamais. Les éditeurs qui publient des correctifs fréquents (Acrobat, Office, gros outils de développement) en accumulent indéfiniment.

Pour les trouver, InstallerClean appelle directement l'interface COM de Windows Installer via P/Invoke :

- `MsiEnumProductsEx` pour énumérer chaque produit installé
- `MsiEnumPatchesEx` pour trouver tous les correctifs enregistrés de chaque produit
- `MsiGetPatchInfoEx` pour lire l'état d'un correctif (appliqué, remplacé ou rendu obsolète)

Tout fichier `.msi` ou `.msp` de `C:\Windows\Installer` qu'aucun produit enregistré ne revendique est orphelin. Tout correctif marqué comme remplacé et non requis pour la désinstallation est marqué comme supprimable.

Si l'API renvoie des données incomplètes (rare, mais possible avec un état d'installation corrompu), l'application se rabat sur la lecture du registre. Ce repli n'ajoute des fichiers qu'à l'ensemble « encore utiles », jamais à l'ensemble « supprimables ».

Une fois un déplacement ou une suppression terminé, les sous-dossiers vides de `C:\Windows\Installer` (les répertoires que le cache laisse derrière lui une fois leur contenu parti) sont nettoyés dans la même passe. Les points d'analyse (reparse points) sont ignorés pendant cette étape, afin qu'une jonction implantée dans le cache ne puisse pas détourner le nettoyage hors du cache.

## Est-ce sûr ?

Oui. InstallerClean interroge la base de données que Windows utilise lui-même pour suivre ce qui est installé. Si Windows indique qu'un fichier n'est plus nécessaire, l'application le croit ; elle ne devine pas à partir des noms de fichiers ou des dates.

**Dans l'application.** La suppression envoie les fichiers à la Corbeille. Si la Corbeille n'est pas disponible pour ce lecteur (désactivée pour le lecteur, pleine ou endommagée), InstallerClean ne supprime pas vos fichiers définitivement en douce. Il s'arrête et vous laisse choisir : les déplacer ailleurs en lieu sûr, les supprimer définitivement ou tout annuler. Un fichier n'est jamais supprimé définitivement sans que vous l'ayez explicitement choisi. Le déplacement est l'option la plus prudente : il place les fichiers dans un dossier de votre choix, vous pouvez donc les conserver le temps de vérifier que rien n'a cassé. Rien n'est touché tant que vous n'avez pas confirmé. Si Windows Installer est en train d'écrire dans le cache, qu'une transaction précédente est suspendue ou qu'un renommage post-redémarrage visant le cache est en attente, le déplacement et la suppression sont désactivés et la raison précise est affichée. Les services d'analyse, de requête, de déplacement, de suppression, de réglages et de vérification du redémarrage en attente sont couverts par une suite de tests automatisés qui s'exécute à chaque commit (voir le badge CI plus haut).

**Vérifier le binaire.** InstallerClean n'est pas signé numériquement. Les certificats de signature de code coûtent de l'argent chaque année, et je préfère garder le projet gratuit, ouvert et financé par les dons.

- Les empreintes SHA-256 de chaque version sont listées sur la [page des versions](../../releases/latest).
- Des liens VirusTotal pour les variantes setup, portable, slim et CLI sont publiés à chaque version.
- Le code source est sur [github.com/no-faff/InstallerClean](https://github.com/no-faff/InstallerClean) et la CI compile et teste chaque commit (voir le badge CI vert plus haut).
- [MajorGeeks](https://www.majorgeeks.com/files/details/installerclean.html) teste chaque soumission dans une machine virtuelle et ne la référence que si elle passe son contrôle.
- [Softpedia](https://www.softpedia.com/get/System/Hard-Disk-Utils/InstallerClean.shtml) analyse chaque version à la recherche de virus, logiciels espions et publiciels.

<a href="https://www.softpedia.com/get/System/Hard-Disk-Utils/InstallerClean.shtml"><img src="docs/badges/softpedia-100-free2.webp" alt="Certifié 100 % propre par Softpedia" width="190"></a>

VirusTotal : propre sur tous les moteurs. Des liens à jour dans les notes de chaque version pour que vous puissiez revérifier.

<a id="recovery"></a>
## Si un fichier manque bel et bien dans `C:\Windows\Installer`

InstallerClean ne supprime jamais que les fichiers dont Windows déclare avoir fini de se servir, il ne peut donc pas laisser un programme dans l'impossibilité de se réparer, se mettre à jour ou se désinstaller. Retirer des fichiers de `C:\Windows\Installer` à la main, ou avec un outil qui ne consulte pas d'abord la base de données de Windows Installer, est une tout autre affaire, et c'est pour cela que le conseil habituel est de ne pas toucher à ce dossier. Ce conseil est généralement juste, mais pas si vous utilisez InstallerClean. Voici le tableau complet, et ce qu'il faut faire si un fichier nécessaire a déjà disparu.

### À propos de `C:\Windows\Installer`, et comment récupérer un fichier manquant

*Les citations de Microsoft ci-dessous sont reproduites dans leur version anglaise d'origine.*

`C:\Windows\Installer` est le cache de Windows Installer. Quand vous installez un programme basé sur MSI ou que vous appliquez un correctif, Windows en conserve ici une copie et note, pour chaque produit, le fichier qu'il s'attendra à retrouver plus tard. Ces fichiers ne servent pas pendant que le programme tourne ; ils servent quand Windows le répare, le met à jour ou le désinstalle. Supprimez-en un dont un programme a encore besoin et rien ne casse sur le moment ; c'est précisément pour cela qu'il est facile de les supprimer sans dommage apparent et de ne rencontrer des ennuis que des mois plus tard. Microsoft le formule ainsi :

> "If the installer cache is compromised, you may not immediately see problems until you take an action such as uninstalling, repairing, or updating a product."

La récupération n'a rien d'évident, et Microsoft ne s'en cache pas :

> "If application files are missing from the Windows Installer Cache, ask the vendor or support team for the application about the missing files. You must follow the procedures or steps recommended by the application vendor to restore the files. In some cases, you may have to rebuild the operating system and reinstall the application to fix the problem."

> "Windows support engineers cannot help you recover missing application files from the Windows Installer cache."

Et vous ne pouvez pas non plus emprunter le fichier à une autre machine :

> "Missing files cannot be copied between computers because the files are unique."

En pratique, la solution qui fonctionne le plus souvent consiste à télécharger, chez son éditeur, le programme d'installation du logiciel concerné, puis à le lancer par-dessus votre installation existante. Ne désinstallez pas d'abord : la désinstallation est elle-même l'une des étapes qui ont besoin du fichier manquant. Utilisez la version que vous avez actuellement installée si vous pouvez encore vous la procurer, car Windows pourrait en refuser une autre :

> "The upgrade cannot be installed by the Windows Installer service because the program to be upgraded may be missing, or the upgrade may update a different version of the program."

Cela rétablit normalement le fichier sans toucher à vos réglages, mais Microsoft ne le garantit pas, et son dernier recours documenté est de réinstaller le programme, voire de reconstruire Windows. C'est la position officielle, rapportée telle que je la trouve. Je n'en suis pas la cause et je ne peux pas faire mieux que les indications de Microsoft elles-mêmes ; je me contente de vous dire ce qu'il en est.

Rien de tout cela ne peut se produire à cause d'InstallerClean. Il ne supprime jamais que les fichiers que Windows lui-même signale comme n'étant plus nécessaires, de sorte que le fichier qu'une future réparation, mise à jour ou désinstallation ira chercher n'est jamais l'un de ceux qu'il a touchés. Les indications de Microsoft se trouvent sur [Restore missing Windows Installer cache files](https://learn.microsoft.com/en-us/troubleshoot/windows-client/application-management/missing-windows-installer-cache).

## Accessibilité

InstallerClean est conçu pour être pleinement utilisable au clavier et avec un lecteur d'écran.

- **Entièrement utilisable au clavier.** La touche Tab atteint chaque contrôle, et les colonnes des fenêtres de détail se trient au clavier : rien ici n'exige la souris. Le focus clavier reste visible où qu'il se trouve.
- **Narrateur et Accès vocal.** Chaque contrôle est étiqueté, et le mot affiché sur un bouton est exactement celui qui l'active à la voix. Quand un déplacement ou une suppression se termine, le résultat est annoncé à voix haute.
- **Pensé pour être lu.** Le texte respecte le contraste WCAG AA sur tout le thème sombre.

Si quelque chose ici vous gêne, [ouvrez un ticket](../../issues). Les problèmes d'accessibilité sont des bugs, pas des cas marginaux.

## Ce qu'il ne fait pas

- WinSxS (`C:\Windows\WinSxS`) est un dossier différent, avec des règles différentes. Pour celui-là, exécutez `Dism /Online /Cleanup-Image /StartComponentCleanup` depuis une invite élevée.
- Aucun service en arrière-plan, aucune tâche planifiée, aucun nettoyage automatique. L'application s'exécute quand vous la lancez.
- Le registre est lu en lecture seule. L'application interroge la base de données de Windows Installer ; elle ne la modifie pas.
- Aucune télémétrie automatique, aucune activité réseau en arrière-plan. L'application ne fait aucun appel réseau tant que vous ne cliquez pas sur l'un des deux boutons. **Vérifier les mises à jour**, dans À propos, interroge au clic l'API publique des versions de GitHub et vous indique si vous avez la dernière version (un seul GET HTTPS, chaîne d'identification `InstallerClean/<version>`). **Envoyer le résumé**, sur l'écran de fin, lit `%LOCALAPPDATA%\NoFaff\InstallerClean\last-run.json` et l'envoie par POST HTTPS à un point de terminaison No Faff, pour que je puisse voir si l'exécution a fonctionné. Le JSON ne contient que des compteurs et des étiquettes catégorielles : aucun chemin de fichier, aucun nom d'utilisateur, aucun identifiant de machine, aucune heure de la journée. Le clic ouvre une fenêtre de confirmation qui montre le JSON exact sur le point d'être envoyé ; examinez-le là, puis appuyez sur Envoyer pour confirmer, ou sur Annuler pour renoncer. Une fois par machine : après un envoi réussi, le bouton reste caché pour toujours ; si la première tentative échoue sur une erreur passagère, la session suivante vous redemande.
- Aucun supplément groupé. Pas de barres d'outils, pas d'offres tierces, pas d'incitations à payer.
- La seule autorisation demandée au-delà du lancement est celle d'administrateur, requise parce que `C:\Windows\Installer` est réservé aux administrateurs.

## FAQ

**Vais-je vraiment libérer des Go d'espace ?** Ça dépend de votre machine. Une installation neuve de Windows 11 sans logiciel supplémentaire n'a rien à supprimer. Une station de développement utilisée de longue date, ou toute machine chargée de logiciels basés sur MSI (Acrobat, Office, LibreOffice, gros outils de développement), peut en contenir des dizaines de Go. Lancez `installerclean-cli /s` pour voir exactement ce qui serait supprimé avant de vous engager.

<!-- reports-stats-start (generated by non-repo-files/refresh-reports-table.mjs; do not hand-edit between these markers) -->
Sur les 74 rapports que des utilisateurs nous ont gentiment envoyés (merci 🙏) depuis que la v1.8.0 a ajouté l'option :

| Résultat | Proportion | Minimum | Médiane | Maximum |
|---|---|---|---|---|
| Rien à supprimer | 65 % | - | - | - |
| Espace libéré | 35 % | 0,2 Go | 22 Go | 327 Go |
<!-- reports-stats-end -->

<details>
<summary>Voici à quoi ressemble un rapport</summary>

```json
{
  "schemaVersion": 3,
  "app": { "version": "1.9.0" },
  "os": "Windows 11 (X64)",
  "scan": {
    "durationMs": 1820,
    "registeredCount": 148,
    "orphanedCount": 40,
    "supersededCount": 25,
    "obsoletedCount": 5,
    "missingFromDiskCount": 0,
    "pendingReboot": "clean"
  },
  "operation": {
    "kind": "delete",
    "outcome": "complete",
    "filesProcessed": 70,
    "filesFailed": 0,
    "bytesFreed": 22548578304,
    "errors": [],
    "moveDestinationKind": null
  }
}
```

Il ne contient que des compteurs et des étiquettes catégorielles : aucun chemin de fichier, aucun nom d'utilisateur, aucun identifiant de machine.

</details>

**Pourquoi a-t-il besoin des droits d'administrateur ?** `C:\Windows\Installer` appartient à SYSTEM et son accès est restreint aux seuls administrateurs. Lire le dossier, accéder en écriture à l'API de requête de la base de données de Windows Installer, déplacer ou supprimer des fichiers : tout cela exige une élévation. Il n'existe aucune voie en mode utilisateur.

**Puis-je annuler une suppression ?** En général, oui. Quand la Corbeille est disponible pour le lecteur, la suppression y envoie les fichiers et vous pouvez les restaurer depuis la Corbeille. Si elle n'est pas disponible, l'application ne supprime jamais définitivement d'elle-même (voir [Est-ce sûr ?](#est-ce-sûr-)). Pour un filet de sécurité que vous maîtrisez, utilisez le déplacement afin de placer les fichiers dans un dossier de votre choix, et vérifiez que rien ne casse avant de les supprimer de là.

**Windows va-t-il se plaindre si je supprime ces fichiers ?** Non. InstallerClean ne supprime jamais que les fichiers dont Windows lui-même déclare avoir fini de se servir, donc rien de ce qu'il supprime n'est requis pour réparer, mettre à jour ou désinstaller un programme. Si un fichier nécessaire venait malgré tout à disparaître de `C:\Windows\Installer` par un autre moyen, voir [Si un fichier manque bel et bien dans C:\Windows\Installer](#recovery).

**Pourquoi pas `Win32_Product` (WMI) ?** [`Win32_Product` déclenche des opérations de réparation MSI sur chaque produit pendant l'énumération](https://gregramsey.net/2012/02/20/win32_product-is-evil/), ce qui peut prendre plusieurs minutes et solliciter le disque très fort. InstallerClean appelle directement l'API COM de Windows Installer, sans effet de bord.

**Pourquoi pas un simple script PowerShell ?** Un court script qui appelle `MsiEnumPatchesEx` suffit à *lister* les correctifs, mais l'essentiel d'InstallerClean tient dans ce qu'un script survole : la classification orphelin / remplacé, le repli sur le registre qui n'ajoute des fichiers qu'à l'ensemble « encore utiles » (jamais à « supprimables »), le blocage en cas de redémarrage en attente, le filet de sécurité du déplacement ailleurs, la progression par fichier avec annulation, et le choix par défaut de la Corbeille plutôt que la suppression définitive. Sur des machines réellement chargées en MSI, les cas limites (enregistrements corrompus, jonctions dans le cache, produits dans `HKU\.DEFAULT`, transactions Installer suspendues) sont faciles à mal gérer dans un script bricolé pour l'occasion. La `installerclean-cli` est la version sans interface graphique, si c'est de scripting que vous avez besoin.

**Fonctionne-t-il sous Windows 7 ou 8 ?** Non testé et non pris en charge. Cible Windows 10 et 11.

**Convient-il au RMM ou au déploiement de masse ?** Oui. La CLI se termine avec des codes distincts selon le résultat (0 succès, 2 partiel, 1 échec total, 75 transitoire, 130 Ctrl+C), de sorte qu'une tâche planifiée peut réessayer sur 75 sans le confondre avec un échec total. Elle écrit un résumé par exécution dans le journal d'événements Application et respecte le même mutex d'instance unique que l'interface graphique. Voir la section Ligne de commande.

## Téléchargement

Quatre variantes, choisissez-en une :

- **Setup** (`InstallerClean-setup.exe`) : un programme d'installation Windows classique, avec le runtime .NET 10 intégré. Ajoute une entrée au menu Démarrer et se désinstalle proprement. Bien rangé dans les Programmes, facile à retrouver dans six mois.
- **Portable** (`InstallerClean-portable.exe`) : un exe unique et autonome, runtime intégré. Pas d'installation, pas de désinstallation. Lancez-le, utilisez-le, supprimez-le. Relancez-le quand vous voulez.
- **Slim** (`InstallerClean-slim.exe`) : le téléchargement le plus léger. Nécessite que le [runtime .NET 10 Desktop](https://dotnet.microsoft.com/download/dotnet/10.0) soit déjà installé (ce qui est le cas si vous avez un Visual Studio à jour).
- **CLI** (`installerclean-cli.exe`) : la version en ligne de commande seule, un exe unique et autonome. Pas d'installation, rien laissé sur la machine ensuite. Déposez-le sur un poste client, lancez une analyse ou un nettoyage, supprimez-le. Conçu pour le scripting, les tâches planifiées et le déploiement de masse, quand vous voulez les opérations sans application de bureau sur le client. Voir [Ligne de commande](#ligne-de-commande) pour les arguments et les codes de sortie.

Téléchargez depuis la [page des versions](../../releases/latest), puis lancez. Windows SmartScreen affichera « Éditeur inconnu ». Cliquez sur **Informations complémentaires** puis sur **Exécuter quand même**. C'est normal pour un logiciel open source non signé.

L'application analyse automatiquement au démarrage. Examinez les résultats, puis cliquez sur **Supprimer** ou **Déplacer**.

Ou installez via [Scoop](https://scoop.sh) :

```
scoop bucket add no-faff https://github.com/no-faff/scoop-bucket
scoop install installerclean
```

## Comparaison avec PatchCleaner

| | **InstallerClean** | **PatchCleaner** |
|---|---|---|
| Dernière mise à jour | 2026 (actif) | 3 mars 2016 |
| Code source | Open source (MIT) | Fermé |
| Runtime | .NET 10 (autonome) | .NET + VBScript |
| API | Windows Installer COM (intra-processus) | Windows Installer COM (hors processus via VBScript) |
| Détection des correctifs remplacés | Oui | Non |
| Gestion d'Adobe | Détecte les correctifs remplacés | Exclus par défaut |
| Interface | Thème sombre (WPF) | Windows Forms |
| Collecte de données | Aucune | Aucune |
| Sûreté de la suppression | Corbeille, jamais de suppression définitive silencieuse | Définitive, sans Corbeille |

> **À propos de `Win32_Product` :** L'approche courante mais boguée pour lister les produits installés est `Win32_Product` (WMI), qui [déclenche des opérations de réparation MSI](https://gregramsey.net/2012/02/20/win32_product-is-evil/) sur chaque produit pendant l'énumération. InstallerClean comme PatchCleaner l'évitent. Tous deux utilisent l'interface COM de Windows Installer. Le nom de fichier `WMIProducts.vbs` dans le script de PatchCleaner est trompeur ; le script utilise COM MSI, pas WMI.

[Ultra Virus Killer (UVK)](https://www.carifred.com/uvk/) propose lui aussi un nettoyage du dossier Installer dans son module System Booster, mais c'est un outil payant (15 à 25 $) et le nettoyage n'y est qu'une petite fonctionnalité au sein d'une application bien plus large. InstallerClean est gratuit, ciblé et open source.

Les nettoyeurs système généralistes comme [CCleaner](https://www.ccleaner.com/) et [BleachBit](https://www.bleachbit.org/) ne touchent pas à `C:\Windows\Installer`. Le dossier exige des requêtes à l'API Windows Installer pour distinguer les paquets enregistrés des fichiers inutiles, et un nettoyeur générique qui se contenterait de parcourir l'arborescence pourrait casser des applications installées. InstallerClean est l'outil vers lequel se tourner quand c'est précisément ce dossier-là que vous voulez nettoyer.

## Ligne de commande

InstallerClean prend en charge un fonctionnement sans interface, pour le scripting et l'administration système :

```
Utilisation :
  installerclean-cli --help   Affiche cette aide (accepte aussi /?, -h)
  installerclean-cli --version  Affiche la version (accepte aussi -v)
  installerclean-cli /s       Analyse seule, liste les fichiers supprimables
  installerclean-cli /d       Supprime les fichiers (Corbeille)
  installerclean-cli /m       Déplace vers l'emplacement par défaut enregistré
  installerclean-cli /m PATH  Déplace vers le chemin indiqué
```

Pour lancer l'interface graphique, exécutez `InstallerClean.exe` (ou utilisez le raccourci du menu Démarrer si vous avez utilisé le programme d'installation Setup).

Lancé sans argument, ou avec une option non reconnue, `installerclean-cli` affiche cette aide et se termine avec le code `1` : une tâche planifiée qui perd son option échoue ainsi de façon visible, au lieu de réussir en silence sans rien faire. Un `--help`, `/?` ou `-h` explicite affiche la même aide et se termine avec le code `0`.

`/s` est un essai à blanc : il analyse, liste ce qui serait supprimé avec les noms de fichiers et les tailles, puis se termine. Utile pour auditer avant de nettoyer. Le code de sortie est `0` si l'analyse réussit, `1` si elle échoue et `130` en cas de Ctrl+C. Tous les fichiers se trouvent dans `C:\Windows\Installer`.

`/d` et `/m` analysent, puis agissent. `/d` envoie les fichiers supprimables à la Corbeille. `/m` les déplace vers un dossier (soit celui indiqué sur la ligne de commande, soit celui enregistré par défaut depuis l'interface graphique). Codes de sortie : `0` succès complet, `2` partiel (certains fichiers réussis, d'autres échoués), `1` échec total (analyse échouée, arguments incorrects ou tous les fichiers du lot en échec), `75` une condition transitoire a bloqué l'exécution (le message affiché précise laquelle et si un nouvel essai aidera), `130` Ctrl+C.

Toute la sortie de la CLI, y compris les messages d'erreur et de diagnostic, va vers stdout ; il n'existe pas de flux stderr distinct. Le code de sortie est le signal lisible par la machine (et l'entrée du journal d'événements Application, écrite à chaque exécution, le reflète), donc un script devrait se fonder sur le code de sortie plutôt que d'analyser le texte, et `installerclean-cli /s > audit.txt` capture toute l'exécution, y compris une éventuelle ligne d'erreur.

Les trois nécessitent une invite de commandes élevée (administrateur). Si une stratégie de groupe bloque l'invite d'élévation UAC, le processus refuse de démarrer et Windows renvoie l'erreur 740 à l'invite parente (`$LASTEXITCODE = 740` sous PowerShell). `taskkill /pid <pid>` ne déclenche pas d'annulation propre ; le mutex d'instance unique est récupéré au lancement suivant, par le chemin AbandonedMutexException.

À noter : la sortie de la CLI elle-même est en anglais. Les descriptions ci-dessus correspondent aux options disponibles.

### Pourquoi `installerclean-cli` et pas `installerclean.exe` ?

`InstallerClean.exe` est l'interface graphique WPF ; elle ne répond pas aux arguments de ligne de commande. `installerclean-cli.exe` est un exécutable console distinct, livré dans le même répertoire d'installation, qui expose les mêmes opérations d'analyse, de déplacement et de suppression à PowerShell, cmd et aux tâches planifiées. Comme c'est un véritable processus console, il bloque l'invite jusqu'à sa fin ; vous pouvez rediriger sa sortie ou la passer dans un tube, comme pour n'importe quel autre exe console.

Les téléchargements portable et slim ne contiennent que l'exe de l'interface graphique. Si vous voulez la ligne de commande sans l'interface, téléchargez `installerclean-cli.exe` depuis la [page des versions](../../releases/latest) et lancez-le directement. Le setup l'installe lui aussi à côté de l'interface graphique.

## Prérequis

- Windows 10 ou 11
- Privilèges d'administrateur (`C:\Windows\Installer` est réservé aux administrateurs)

Voir [Téléchargement](#téléchargement) pour les options setup, portable, slim et CLI.

## Compilation depuis les sources

```
git clone https://github.com/no-faff/InstallerClean.git
cd InstallerClean
dotnet build src/InstallerClean/InstallerClean.csproj
```

Lancer les tests :

```
dotnet test src/InstallerClean.Tests/
```

## Contribuer

Vous avez trouvé un bug ou vous avez une suggestion ? [Ouvrez un ticket](../../issues) ou démarrez une [discussion](../../discussions). Les pull requests sont les bienvenues. Lancez `dotnet test` avant de soumettre.

## Soutenir le projet

Si InstallerClean vous a été utile, vous pouvez [soutenir No Faff](https://nofaff.netlify.app/support) ou laisser une étoile sur GitHub.

## Historique des étoiles

<a href="https://www.star-history.com/?repos=no-faff%2FInstallerClean&type=date&legend=top-left">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/chart?repos=no-faff/InstallerClean&type=date&theme=dark&legend=top-left" />
   <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/chart?repos=no-faff/InstallerClean&type=date&legend=top-left" />
   <img alt="Graphique de l'historique des étoiles" src="https://api.star-history.com/chart?repos=no-faff/InstallerClean&type=date&legend=top-left" />
 </picture>
</a>

## Licence

[MIT](LICENSE)

---

🎶 [George Formby - When I'm Cleaning Windows](https://www.youtube.com/watch?v=sfmAeijj5cM). Régalez-vous !
