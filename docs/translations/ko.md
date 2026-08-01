# InstallerClean in 한국어 (Korean)

The text of InstallerClean's interface and command-line tool in English on the left, with the Korean translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Korean can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.ko.resx`](../../src/InstallerClean.Core/Resources/Strings.ko.resx), so do not edit it by hand. The Korean translation itself lives in [`gen-strings-ko.mjs`](../../scripts/translations/gen-strings-ko.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | 한국어 |
| --- | --- |
| InstallerClean | InstallerClean |
| About | 정보 |
| Registered files that should not be deleted | 삭제하지 말아야 할 등록된 파일 |
| Unneeded files that are safe to delete | 안전하게 삭제할 수 있는 불필요한 파일 |

## Section headings

| English | 한국어 |
| --- | --- |
| PRODUCTS | 제품 |
| PATCHES | 패치 |
| PRODUCT DETAILS | 제품 세부 정보 |
| BACKUP FOLDER | BACKUP FOLDER |
| SAY THANKS | 감사 인사 |

## Buttons and actions

| English | 한국어 |
| --- | --- |
| _About | 정보(_A) |
| Copy | 복사 |
| Cut | 잘라내기 |
| Paste | 붙여넣기 |
| Select all | 모두 선택 |
| _Browse... | 찾아보기(_B)... |
| _Cancel | 취소(_C) |
| Check for _updates | 업데이트 확인(_U) |
| _Close | 닫기(_C) |
| _Delete permanently | 영구 삭제(_D) |
| _Done | 완료(_D) |
| Details | 세부 정보 |
| _Buy me a cuppa | 커피 한 잔 사주기(_B) |
| Leave a _star on GitHub | GitHub에 별 남기기(_S) |
| Apache 2.0 licence | Apache 2.0 라이선스 |
| _Move | 이동(_M) |
| Path to folder if you move rather than delete. | Path to folder if you move rather than delete. |
| Open _release page | 릴리스 페이지 열기(_R) |
| _Re-scan | 다시 검사(_R) |
| _Scan again | 다시 검사(_S) |
| Send report | 보고서 보내기 |
| _Send | 보내기(_S) |

## About window

| English | 한국어 |
| --- | --- |
| Guide and FAQ | 안내서 및 자주 묻는 질문 |
| Report a problem | 문제 신고 |
| Check for updates automatically | 자동으로 업데이트 확인 |

## Field labels

| English | 한국어 |
| --- | --- |
| Reason | 이유 |
| Author | 작성자 |
| Application | 애플리케이션 |
| Title | 제목 |
| Subject | 주제 |
| Keywords | 키워드 |
| Signing certificate | 서명 인증서 |
| File size | 파일 크기 |
| Comment | 설명 |
| Product name | 제품 이름 |
| File | 파일 |
| Size | 크기 |
| Patches | 패치 |
| (unknown) | (알 수 없음) |
| (patches only) | (패치 전용) |
| missing | 누락 |

## Status and progress

| English | 한국어 |
| --- | --- |
| Scanning... | 검사 중... |
| Cancelling... | 취소 중... |
| Starting scan... | 검사를 시작하는 중... |
| Asking Windows about installed software... | 설치된 소프트웨어 정보를 Windows에 조회하는 중... |
| Scanning installer cache folder... | 설치 관리자 캐시 폴더를 검사하는 중... |
| Enumerating installed products... | 설치된 제품을 열거하는 중... |
| Checking registry for additional packages... | 레지스트리에서 추가 패키지를 확인하는 중... |
| Found {0} registered {1}. | 등록된 {1} {0}개를 찾았습니다. |
| Scan complete ({0}) | 검사 완료 ({0}) |
| Scanning local packages... | 로컬 패키지를 검사하는 중... |
| Found {0} {1} you can safely delete. | 안전하게 삭제할 수 있는 {1} {0}개를 찾았습니다. |
| Preparing destination folder... | 대상 폴더를 준비하는 중... |
| Moving files... | Moving files... |
| Deleting files... | Deleting files... |
| Move cancelled. {0} of {1} {2} processed. | 이동이 취소되었습니다. {2} {1}개 중 {0}개를 처리했습니다. |
| Delete cancelled. {0} of {1} {2} processed. | 삭제가 취소되었습니다. {2} {1}개 중 {0}개를 처리했습니다. |
| Move failed ({0}). Details in {1}. | 이동 실패 ({0}). 자세한 내용은 {1}에 있습니다. |
| Move failed ({0}). The crash log could not be written. | 이동 실패 ({0}). 크래시 로그를 기록할 수 없었습니다. |
| Delete failed ({0}). Details in {1}. | 삭제 실패 ({0}). 자세한 내용은 {1}에 있습니다. |
| Delete failed ({0}). The crash log could not be written. | 삭제 실패 ({0}). 크래시 로그를 기록할 수 없었습니다. |
| Access denied. Windows refused the scan. | 액세스가 거부되었습니다. Windows가 검사를 거부했습니다. |
| Scan failed: couldn't read the Windows Installer records. | 검사 실패: Windows Installer 기록을 읽을 수 없습니다. |
| Scan cancelled. | 검사가 취소되었습니다. |
| Ready | 준비됨 |
| Scan failed ({0}). Details in {1}. | 검사 실패 ({0}). 자세한 내용은 {1}에 있습니다. |
| Scan failed ({0}). The crash log could not be written. | 검사 실패 ({0}). 크래시 로그를 기록할 수 없었습니다. |

## Main screen text

| English | 한국어 |
| --- | --- |
| Any unneeded files below are [safe to delete]. | Any unneeded files below are [safe to delete]. |
| They sit in {InstallerFolder}, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | 이 파일들은 {InstallerFolder}에 있으며, 프로그램을 제거했거나({0}), 새 패치가 옛 패치를 대체했거나({1}), 게시자가 철회했을 때({2}) 남겨진 것입니다. InstallerClean은 Windows 자체가 다 썼다고 보고하는 파일만 나열합니다. |
| Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored. | Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored. |
| Nothing scanned yet. | 아직 검사하지 않았습니다. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | 다시 검사를 눌러 {InstallerFolder}에서 더 이상 어떤 프로그램도 필요로 하지 않는 설치 관리자 파일을 찾아보세요. |
| These files can't be cleaned up right now. | 지금은 이 파일들을 정리할 수 없습니다. |
| Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. |
| Select a file to view details. | 세부 정보를 보려면 파일을 선택하세요. |
| Select a product to view details. | 세부 정보를 보려면 제품을 선택하세요. |
| No metadata available. | 사용할 수 있는 메타데이터가 없습니다. |
| This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. | This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. |
| The README [explains this folder], and how to recover a file, in Microsoft's own words. | README에는 Microsoft의 표현 그대로 [이 폴더에 대한 설명]과 파일을 복구하는 방법이 담겨 있습니다. |
| (none) | (없음) |

## Reasons a file is unneeded

| English | 한국어 |
| --- | --- |
| Orphaned | 고립됨 |
| Superseded | 대체됨 |
| Obsoleted | 폐기됨 |

## Completion screen

| English | 한국어 |
| --- | --- |
| All clean | 모두 깨끗합니다 |
| Nothing to clean up in {InstallerFolder} | {InstallerFolder}에 정리할 것이 없습니다 |
| Scanned {0} {1} in {2} | {1} {0}개 검사, {2} 소요 |
| The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). |
| The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). |
| The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). |
| The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). |
| {0} freed | {0} 확보 |
| {0} moved | {0} 이동 |
| Nothing was moved | 이동된 파일 없음 |
| Nothing was deleted | 삭제된 파일 없음 |
| {0} of {1} could not be moved. | 파일 {1}개 중 {0}개를 이동하지 못했습니다. |
| {0} of {1} could not be moved. | 파일 {1}개 중 {0}개를 이동하지 못했습니다. |
| {0} of {1} could not be deleted. | 파일 {1}개 중 {0}개를 삭제하지 못했습니다. |
| {0} of {1} could not be deleted. | 파일 {1}개 중 {0}개를 삭제하지 못했습니다. |
| {0} {1} moved to: {2} | {1} {0}개를 다음 위치로 이동함: {2} |
| {0} {1} moved to: {2} | {1} {0}개를 다음 위치로 이동함: {2} |
| {0} {1} kept in place, because a program went back to needing what the scan flagged. | {1} {0}개는 검사 이후 어떤 프로그램이 다시 필요로 하게 되어 그대로 두었습니다. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read when the check was repeated. | {1} {0}개는 확인을 다시 했을 때 Windows Installer 기록을 완전히 읽을 수 없어 그대로 두었습니다. |
| Moved {0} of {1} {2} before you cancelled. | 취소하기 전까지 {2} {1}개 중 {0}개를 이동했습니다. |
| Permanently deleted {0} of {1} {2} before you cancelled. | 취소하기 전까지 {2} {1}개 중 {0}개를 영구 삭제했습니다. |
| {0} {1} permanently deleted | {0} {1} permanently deleted |
| {0} {1} permanently deleted | {0} {1} permanently deleted |
| Glad to help. There's a tip jar if you're feeling kind. | 도움이 되어 기쁩니다. 너그러운 마음이 있으시면 작은 성의도 반갑습니다. |

## Summaries and counts

| English | 한국어 |
| --- | --- |
| {0} file still needed | 아직 필요한 파일 {0}개 |
| {0} files still needed | 아직 필요한 파일 {0}개 |
| {0} unneeded file to clean up | 정리할 불필요한 파일 {0}개 |
| {0} unneeded files to clean up | 정리할 불필요한 파일 {0}개 |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | 등록된 파일 {0}개가 누락되었습니다(InstallerClean이 삭제한 것은 아닙니다). 지금은 문제가 없지만, 나중에 해당 프로그램을 복구, 업데이트 또는 제거할 때 실패할 수 있습니다. 어떻게 해야 할지는 세부 정보를 여세요. |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | 등록된 파일 {0}개가 누락되었습니다(InstallerClean이 삭제한 것은 아닙니다). 지금은 문제가 없지만, 나중에 해당 프로그램들을 복구, 업데이트 또는 제거할 때 실패할 수 있습니다. 어떻게 해야 할지는 세부 정보를 여세요. |
| {0} installed program could not be read during this scan, so superseded patches have been kept. Orphaned files are not affected. | 이번 검사에서 설치된 프로그램 {0}개를 읽을 수 없어 대체된 패치를 그대로 두었습니다. 고립된 파일은 영향을 받지 않습니다. |
| {0} installed programs could not be read during this scan, so superseded patches have been kept. Orphaned files are not affected. | 이번 검사에서 설치된 프로그램 {0}개를 읽을 수 없어 대체된 패치를 그대로 두었습니다. 고립된 파일은 영향을 받지 않습니다. |
| {0} of {1} {2} | {2} {1}개 중 {0}개 |
| {0} orphaned, {1} superseded, {2} obsoleted ({3}) | 고립됨 {0}개, 대체됨 {1}개, 폐기됨 {2}개 ({3}) |
| {0} registered file that is still needed ({1}) | 아직 필요한 등록된 파일 {0}개 ({1}) |
| {0} registered files that are still needed ({1}) | 아직 필요한 등록된 파일 {0}개 ({1}) |

## Confirmation dialogs

| English | 한국어 |
| --- | --- |
| Move {0} {1} ({2})? | {1} {0}개를 이동하시겠습니까? ({2}) |
| Files will be moved to: | 파일이 다음 위치로 이동됩니다: |
| Delete {0} {1} ({2})? | {1} {0}개를 삭제하시겠습니까? ({2}) |
| This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. | This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. |
| Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. | Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. |

## Error messages

| English | 한국어 |
| --- | --- |
| Access denied | 액세스 거부됨 |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows가 InstallerClean의 접근을 거부해서 작업을 멈췄습니다. 아무것도 제거되지 않았습니다.<br><br>InstallerClean은 이미 관리자 권한으로 실행 중이었으므로 그런 식으로 다시 시작해도 도움이 되지 않습니다. Windows는 무엇이 접근을 거부했는지 더 이상 알려주지 않으므로 구체적으로 시도해 볼 것이 없습니다. |
| Couldn't read the Windows Installer records | Windows Installer 기록을 읽을 수 없습니다 |
| Scan failed | 검사 실패 |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed. | Windows Installer 기록이 완전히 비어서 돌아왔습니다. 설치된 프로그램도, 업데이트도 캐시된 설치 파일을 하나도 요구하지 않습니다. 정상적으로 작동하는 컴퓨터에서는 이런 일이 없으므로(갓 설치한 Windows에도 그런 파일이 있습니다) 기록이 손상되었거나 읽을 수 없었던 것이고, 이 답을 그대로 믿은 검사는 {InstallerFolder}의 모든 파일을 잘못 고립된 것으로 판단했을 것입니다. InstallerClean은 그러지 않고 멈췄습니다. 아무것도 제거되지 않았습니다. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer가 InstallerClean에게 설치된 항목의 목록 표시를 허용하지 않았습니다. InstallerClean은 이미 관리자 권한으로 실행 중이었으므로 관리자 권한으로 다시 실행해도 달라지는 것이 없습니다. 그 목록이 없으면 캐시된 파일 중 어느 것이 아직 필요한지 안전하게 알아낼 방법이 없으므로 InstallerClean은 멈췄습니다. 아무것도 제거되지 않았습니다. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer가 InstallerClean에게 읽을 수 있는 설치된 프로그램 목록을 주지 못했습니다. {0}개 항목이 연속으로 읽을 수 없는 상태로 돌아왔습니다(마지막 오류 코드 {1}). 일부만 읽은 목록으로 작업하는 대신 InstallerClean은 멈췄습니다. 아무것도 제거되지 않았습니다. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer가 설치된 프로그램 목록의 끝을 끝내 알리지 않았습니다. InstallerClean은 {0}개 항목에서 포기했습니다(마지막 오류 코드 {1}). 끝이 없는 목록은 믿을 수 없으므로 InstallerClean은 멈췄습니다. 아무것도 제거되지 않았습니다. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer가 한 프로그램의 패치 목록의 끝을 끝내 알리지 않았습니다. InstallerClean은 {0}개 항목에서 포기했습니다(마지막 오류 코드 {1}). 끝이 없는 목록은 믿을 수 없으므로 InstallerClean은 멈췄습니다. 아무것도 제거되지 않았습니다. |
| InstallerClean couldn't square this scan with the Windows Installer records: every file Windows still lists as needed is missing from {InstallerFolder}, while the files actually in the folder match nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. | InstallerClean이 이번 검사를 Windows Installer 기록과 맞춰 볼 수 없었습니다. Windows가 여전히 필요하다고 올려 둔 파일은 모두 {InstallerFolder}에 없고, 그 폴더에 실제로 있는 파일은 어떤 기록과도 일치하지 않습니다. 실제 컴퓨터가 이런 모습일 리 없으므로, 이는 안전하게 제거할 수 있는 파일이 아니라 기록을 읽는 데 생긴 문제를 가리킵니다. 정리 대상으로 아무것도 제시하지 않았고 아무것도 제거되지 않았습니다. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean이 무엇이 아직 필요한지 확신할 만큼 Windows Installer 기록을 읽지 못했습니다. 설치된 프로그램 목록이 일부 빠진 채로 돌아왔고, 같은 기록을 레지스트리에서 직접 읽는 것도 오류를 만났습니다. 어떤 파일을 가리키는 기록이 읽을 수 없는 것 중 하나였다는 이유만으로 그 파일이 고립된 것처럼 보일 수 있으므로 InstallerClean은 멈췄습니다. 아무것도 제거되지 않았습니다. |
| Invalid destination | 잘못된 대상 |
| Could not write to destination | 대상에 쓸 수 없음 |
| Move failed | 이동 실패 |
| Delete failed | 삭제 실패 |
| Setting not saved | 설정 저장 실패 |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | 변경 내용을 저장하지 못했습니다. 다음에 실행할 때 InstallerClean은 이전 설정으로 돌아갑니다. |
| The destination cannot be inside the Windows Installer folder. | 대상은 Windows Installer 폴더 안에 있을 수 없습니다. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | 지정한 대상이 Windows 시스템 폴더 아래로 해석됩니다 ({0}). %SystemRoot%, %ProgramFiles%, %ProgramData% 밖의 경로를 선택하세요. |
| Not enough space | 공간 부족 |
| Not enough space at {0}<br><br>Required: {1}<br>Available: {2} | {0}에 공간이 부족합니다<br><br>필요: {1}<br>사용 가능: {2} |
| You don't have permission to write to {0}.<br>Try a folder in your user profile or on a drive you own. | {0}에 쓸 권한이 없습니다.<br>사용자 프로필 안의 폴더나 본인 소유의 드라이브를 사용해 보세요. |
| The path {0} is too long for Windows. Pick a shorter path. | Windows가 처리하기에는 경로가 너무 깁니다 ({0}). 더 짧은 경로를 선택하세요. |
| The folder {0} does not exist and could not be created. Check the drive letter or network path. | 폴더가 존재하지 않으며 만들 수도 없습니다 ({0}). 드라이브 문자나 네트워크 경로를 확인하세요. |
| Windows cannot write to {0}.<br>Details in {1}. | Windows가 {0}에 쓸 수 없습니다.<br>자세한 내용은 {1}에 있습니다. |
| Windows cannot write to {0}. The crash log could not be written. | Windows가 {0}에 쓸 수 없습니다. 크래시 로그를 기록할 수 없었습니다. |
| Cannot write to {0}.<br>Details in {1}. | {0}에 쓸 수 없습니다.<br>자세한 내용은 {1}에 있습니다. |
| Cannot write to {0}. The crash log could not be written. | {0}에 쓸 수 없습니다. 크래시 로그를 기록할 수 없었습니다. |
| File no longer exists. | 파일이 더 이상 존재하지 않습니다. |
| Source file is a symlink or junction; refused for safety. | 원본 파일이 심볼릭 링크 또는 정션입니다. 안전을 위해 거부했습니다. |
| This file is not directly inside the Windows Installer folder; refused for safety. | 이 파일은 Windows Installer 폴더 바로 아래에 있지 않습니다. 안전을 위해 거부했습니다. |
| Windows refused access to this file; it was left in place. | Windows가 이 파일에 대한 접근을 거부했습니다. 파일은 그대로 두었습니다. |
| Windows refused access to these files; they were left in place. | Windows가 이 파일들에 대한 접근을 거부했습니다. 파일은 그대로 두었습니다. |
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. |
| Windows reported a file error; the file was left in place. | Windows가 파일 오류를 알렸습니다. 파일은 그대로 두었습니다. |
| Windows reported file errors; these files were left in place. | Windows가 파일 오류를 알렸습니다. 이 파일들은 그대로 두었습니다. |
| Something went wrong with this file; it was left in place. | 이 파일에서 문제가 발생했습니다. 파일은 그대로 두었습니다. |
| Something went wrong with these files; they were left in place. | 이 파일들에서 문제가 발생했습니다. 파일은 그대로 두었습니다. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | 파일을 Windows Installer 폴더로 이동하는 것을 거부합니다(대상: {0}). |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} |
| The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. |
| Cannot write to {0}. | {0}에 쓸 수 없습니다. |
| Could not find a unique filename for '{0}' after 10,000 attempts. | 10,000번 시도한 후에도 '{0}'에 대한 고유한 파일 이름을 찾을 수 없었습니다. |

## Update check

| English | 한국어 |
| --- | --- |
| Check for updates | 업데이트 확인 |
| Checking... | 확인 중... |
| Up to date. | 최신 버전입니다. |
| Version {0} is available. | {0} 버전을 사용할 수 있습니다. |
| Update available | 업데이트 사용 가능 |
| You're running version {0}.<br>Version {1} is available. | 현재 {0} 버전을 사용하고 있습니다.<br>{1} 버전을 사용할 수 있습니다. |
| Couldn't reach GitHub. Check your internet connection and try again. | GitHub에 연결할 수 없습니다. 인터넷 연결을 확인하고 다시 시도하세요. |
| GitHub returned an error response. Try again in a few minutes. | GitHub가 오류 응답을 반환했습니다. 몇 분 후에 다시 시도하세요. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | GitHub의 응답에 인식할 수 있는 릴리스가 없습니다. 나중에 다시 시도하거나, 릴리스 페이지를 직접 여세요. |
| The check timed out. Your connection to GitHub may be slow; try again. | 확인 시간이 초과되었습니다. GitHub와의 연결이 느릴 수 있으니 다시 시도하세요. |
| The check failed for an unknown reason. Details are in crash.log if you need to report it. | 알 수 없는 이유로 확인에 실패했습니다. 신고가 필요하면 자세한 내용이 crash.log에 있습니다. |

## Opening links in your browser

| English | 한국어 |
| --- | --- |
| Couldn't open your browser | 브라우저를 열 수 없음 |
| InstallerClean couldn't open your browser. The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | InstallerClean이 브라우저를 열지 못했습니다. 링크를 클립보드에 복사해 두었으니 직접 붙여넣으시면 됩니다:<br><br>{0} |
| InstallerClean couldn't open your browser, and couldn't copy the link to your clipboard either. The link is:<br><br>{0} | InstallerClean이 브라우저를 열지 못했고, 링크를 클립보드에 복사하지도 못했습니다. 링크는 다음과 같습니다:<br><br>{0} |

## Sending the summary

| English | 한국어 |
| --- | --- |
| Sending... | 보내는 중... |
| Thanks! Report sent. | 감사합니다! 보고서를 보냈습니다. |
| Sending failed. Try again later. | 보내기에 실패했습니다. 나중에 다시 시도하세요. |
| No report to send. | 보낼 보고서가 없습니다. |
| Send this? | 이 내용을 보내시겠습니까? |
| It goes to nofaff.netlify.app/api/result-log. Nothing identifies you or your machine; it just lets me know InstallerClean's working and [how much space people are freeing]. | nofaff.netlify.app/api/result-log으로 전송됩니다. 사용자나 사용자의 컴퓨터를 식별할 수 있는 내용은 전혀 없습니다. 그저 InstallerClean이 잘 작동하는지와 [사람들이 공간을 얼마나 확보하고 있는지] 알 수 있게 해 줄 뿐입니다. |

## Startup and crashes

| English | 한국어 |
| --- | --- |
| InstallerClean | InstallerClean |
| InstallerClean is already running. | InstallerClean이 이미 실행 중입니다. |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | 예기치 않은 오류가 발생하여 InstallerClean을 닫아야 합니다.<br><br>{0}<br><br>자세한 내용을 기록한 위치:<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | 예기치 않은 오류가 발생하여 InstallerClean을 닫아야 합니다.<br><br>{0}<br><br>크래시 로그를 기록할 수 없었습니다. |
| Startup error | 시작 오류 |
| Failed to start ({0}). Details written to:<br>{1} | 시작하지 못했습니다 ({0}). 자세한 내용을 기록한 위치:<br>{1} |
| Failed to start ({0}). The crash log could not be written. | 시작하지 못했습니다 ({0}). 크래시 로그를 기록할 수 없었습니다. |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> |

## Tooltips (hover text)

| English | 한국어 |
| --- | --- |
| It's thirsty work! | 이게 생각보다 목마른 일입니다! |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | 취소가 요청되었습니다. InstallerClean이 현재 단계가 멈출 수 있는 지점에 이를 때까지 기다리고 있습니다. I/O가 많거나 MSI 데이터베이스를 호출하는 동안에는 몇 초 걸릴 수 있습니다. |
| Close | 닫기 |
| A star helps other people find it. | 별 하나가 다른 사람들이 InstallerClean을 찾는 데 도움이 됩니다. |
| Minimise | 최소화 |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | 보내실지는 자유지만 보내 주시면 정말 감사합니다. 익명 요약을 전송하며, 이는 프로그램이 잘 작동하는지와 사람들이 공간을 얼마나 확보하고 있는지 제가 알 수 있게 해 줍니다. 다음 화면에서 확인 전에 보낼 내용을 미리 볼 수 있습니다. |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | 보내실지는 자유지만 보내 주시면 정말 감사합니다. 익명 요약을 전송하며, 이는 프로그램이 잘 작동하는지 제가 알 수 있게 해 줍니다. 다음 화면에서 확인 전에 보낼 내용을 미리 볼 수 있습니다. |
| Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. | Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. |
| Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. | Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. | Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. |
| Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. | Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | 내장된 Authenticode 인증서의 주체 이름입니다. 인증서 체인은 검증하지 않았습니다. |
| Change language. The program will restart. | 언어를 변경합니다. 프로그램이 다시 시작됩니다. |

## Screen reader labels

| English | 한국어 |
| --- | --- |
| Donate | 후원 |
| Buy me a cuppa | 커피 한 잔 사주기 |
| Cancel operation | 작업 취소 |
| Cancel scan | 검사 취소 |
| Cancel startup scan | 시작 검사 취소 |
| Close | 닫기 |
| Close window | 창 닫기 |
| Close result and return to main window | 결과를 닫고 메인 창으로 돌아가기 |
| Leave a star on github | github에 별 남기기 |
| Minimise | 최소화 |
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Delete permanently removes the unneeded files. Cancel closes without deleting. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | 이동하면 불필요한 파일이 선택한 대상 폴더에 들어갑니다. 취소하면 파일은 있던 자리에 그대로 남습니다. |
| Say thanks | 감사 인사 |
| Send posts the report shown to No Faff. Cancel sends nothing. | 보내기를 누르면 표시된 보고서가 No Faff에 전송됩니다. 취소하면 아무것도 보내지 않습니다. |
| Check for updates | 업데이트 확인 |
| Checks github's releases page for a newer version. | github의 릴리스 페이지에서 새 버전이 있는지 확인합니다. |
| Opens the readme on github in your browser. | 브라우저에서 github의 readme를 엽니다. |
| Opens the issue tracker on github.com in your browser. | 브라우저에서 github.com의 이슈 트래커를 엽니다. |
| If ticked, InstallerClean checks github for a newer version when you run it. | 선택하면 InstallerClean이 실행할 때 github에서 새 버전이 있는지 확인합니다. |
| Open the release page to download the newer version, or cancel to keep the current version. | 새 버전을 내려받으려면 릴리스 페이지를 열고, 현재 버전을 유지하려면 취소하세요. |
| Opens the licence file on github.com in your browser. | 브라우저에서 github.com의 라이선스 파일을 엽니다. |
| Backup folder | Backup folder |
| Products | 제품 |
| Patches | 패치 |
| Product details | 제품 세부 정보 |
| Backup folder | Backup folder |
| Operation progress | 작업 진행 상황 |
| Scan {InstallerFolder} again | {InstallerFolder} 다시 검사 |
| Scanning progress | 검사 진행 상황 |
| Startup scan progress | 시작 검사 진행 상황 |
| Details, unneeded files | 세부 정보, 불필요한 파일 |
| Available for cleanup. | 정리할 수 있습니다. |
| Details, registered files | 세부 정보, 등록된 파일 |
| Read-only inventory. | 읽기 전용 목록입니다. |
| Sorted by {0}, ascending | {0} 기준 오름차순 정렬됨 |
| Sorted by {0}, descending | {0} 기준 내림차순 정렬됨 |
| Scan results | 검사 결과 |
| Result details | 결과 세부 정보 |
| File details | 파일 세부 정보 |
| Dialog text | 대화 상자 텍스트 |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | 처리할 수 없는 파일 |
| Explains this folder, and how to recover a file, in the README | 이 폴더에 대한 설명과 파일 복구 방법을 README에서 안내 |
| Report preview | 보고서 미리 보기 |
| Change language | 언어 변경 |
| The program will restart. | 프로그램이 다시 시작됩니다. |

## File picker

| English | 한국어 |
| --- | --- |
| Choose destination folder for moved files | 이동할 파일의 대상 폴더 선택 |

## Version

| English | 한국어 |
| --- | --- |
| Version {0} | 버전 {0} |

## Word forms (singular and plural)

| English | 한국어 |
| --- | --- |
| file | 파일 |
| files | 파일 |
| error | 오류 |
| errors | 오류 |
| package | 패키지 |
| packages | 패키지 |
| product | 제품 |
| products | 제품 |
| patch | 패치 |
| patches | 패치 |

## Sizes and times

| English | 한국어 |
| --- | --- |
| {0:F2} GB | {0:F2} GB |
| {0:F1} MB | {0:F1} MB |
| {0:F1} KB | {0:F1} KB |
| {0} B | {0} B |
| {0:F0}ms | {0:F0}ms |
| {0:F1}s | {0:F1}s |
| less than a second | 1초 미만 |
| {0:F1} seconds | {0:F1}초 |

## Command-line tool (installerclean-cli)

| English | 한국어 |
| --- | --- |
| Unknown argument: '{0}' | 알 수 없는 인수: '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | 오류: 예상치 못한 추가 인수 '{0}'. 이동 폴더 경로에 공백이 있으면 전체 경로를 큰따옴표로 묶으세요: /m "D:\My Backup" |
| Cancelling... | 취소 중... |
| Cancelled. | 취소되었습니다. |
| Error: {0}. Details written to {1}. | 오류: {0}. 자세한 내용을 {1}에 기록했습니다. |
| Error: {0}. The crash log could not be written. | 오류: {0}. 크래시 로그를 기록할 수 없었습니다. |
| Scanning {InstallerFolder}... | {InstallerFolder} 검사 중... |
| Found {0} {1} to clean up ({2}). | 정리할 {1} {0}개를 찾았습니다 ({2}). |
| Nothing to do. | 수행할 작업이 없습니다. |
| Deleting {0} {1}... | {1} {0}개를 삭제하는 중... |
| Permanently deleted {0} {1}. | Permanently deleted {0} {1}. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | 오류: 이동 대상이 지정되지 않았습니다. /m 경로를 사용하세요. (GUI에서 설정한 기본값은 사용자별로 저장되므로, 예약된 작업이나 서비스 계정 실행에는 적용되지 않습니다.) |
| Error: destination cannot be inside the Windows Installer folder. | 오류: 대상은 Windows Installer 폴더 안에 있을 수 없습니다. |
| Error: destination must be a fully qualified path. Got: {0} | 오류: 대상은 정규화된 전체 경로여야 합니다. 입력값: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | 오류: 지정한 대상이 Windows 시스템 폴더 아래로 해석됩니다 ({0}). %SystemRoot%, %ProgramFiles%, %ProgramData% 밖의 경로를 선택하세요. |
| Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. |
| Moving {0} {1} to {2}... | {1} {0}개를 {2}(으)로 이동하는 중... |
| Moved {0} {1}. | {1} {0}개를 이동했습니다. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | 다른 InstallerClean 프로세스가 단일 인스턴스 잠금을 보유하고 있습니다(GUI 또는 다른 CLI 실행). 종료 코드 75(일시적); 나중에 다시 시도해도 안전합니다. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | 참고: 이벤트 로그 쓰기에 실패했습니다. 응용 프로그램 로그 권한 또는 그룹 정책을 확인하세요. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - {InstallerFolder} 정리 |
| Usage: | 사용법: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     이 도움말 표시 (/?, -h도 사용 가능) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  버전 출력 (-v도 사용 가능) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         Scan only - list unneeded files |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         Delete unneeded files permanently |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         Move to the saved backup folder |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m 경로    지정한 경로로 이동 |
| installerclean-cli is a real console process and blocks the prompt | installerclean-cli는 실제 콘솔 프로세스로, 명령 프롬프트를 점유하며, |
| until it finishes; redirect or pipe its output as you would any | 실행이 끝날 때까지 기다립니다. 다른 콘솔 앱처럼 출력을 리디렉션하거나 |
| other console exe. The GUI lives in InstallerClean.exe alongside it. | 파이프로 넘길 수 있습니다. GUI는 옆의 InstallerClean.exe입니다. |
| The saved default is per-user; scheduled or SYSTEM runs need /m PATH. | 기본 위치는 사용자별입니다. 예약 작업이나 SYSTEM은 /m 경로가 필요합니다. |
| Exit codes: | 종료 코드: |
|   0   success: every flagged file was processed |   0   성공: 표시된 모든 파일을 처리함 |
|   1   failure: nothing processed (bad args, scan failed, all files failed) |   1   실패: 처리된 파일 없음 (잘못된 인수, 검사 실패, 모든 파일 실패) |
|   2   partial: some files processed, some failed |   2   부분 처리: 일부 파일은 처리됨, 일부는 실패 |
|   75  transient: a temporary condition blocked the run (see the message) |   75  일시적: 일시적인 상황으로 실행이 차단됨 (메시지 참고) |
|   130 cancelled (Ctrl+C) |   130 취소됨 (Ctrl+C) |
