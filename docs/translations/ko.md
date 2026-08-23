# InstallerClean in 한국어 (Korean)

The text of InstallerClean's interface and command-line tool in English on the left, with the Korean translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Korean can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.ko.resx`](../../src/InstallerClean.Core/Resources/Strings.ko.resx), so do not edit it by hand. The Korean translation itself lives in [`gen-strings-ko.mjs`](../../scripts/translations/gen-strings-ko.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | 한국어 |
| --- | --- |
| InstallerClean | InstallerClean |
| About | 정보 |
| Files left alone | 그대로 둔 파일 |
| Unneeded files that are safe to delete | 안전하게 삭제할 수 있는 불필요한 파일 |

## Section headings

| English | 한국어 |
| --- | --- |
| PATCHES | 패치 |
| PRODUCT DETAILS | 제품 세부 정보 |
| BACKUP FOLDER | 대상 폴더 |
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
| Path to folder if you move rather than delete. | 삭제하지 않고 이동할 경우 사용할 폴더 경로입니다. |
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
| Moving unneeded files... | 불필요한 파일 이동 중... |
| Deleting unneeded files... | 불필요한 파일 삭제 중... |
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
| Any unneeded files below are [safe to delete]. | 아래에 있는 불필요한 파일은 모두 [안전하게 삭제할 수 있습니다]. |
| They sit in {InstallerFolder}. InstallerClean asks Windows about every installed program: a file is listed when no program claims it ({0}), or when a newer patch has replaced it and no program could roll back to it ({1}). | 이 파일들은 {InstallerFolder}에 있습니다. InstallerClean은 설치된 모든 프로그램에 대해 Windows에 문의합니다. 어떤 프로그램도 자기 것이라고 하지 않거나({0}), 더 새로운 패치가 그 파일을 대체했고 어떤 프로그램도 그 파일로 되돌아갈 수 없을 때({1}) 목록에 오릅니다. |
| Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update, repair and uninstall as normal. Putting them back into {InstallerFolder} restores everything. Or delete them permanently now. | 직접 고른 대상 폴더로 옮긴 다음, 프로그램들이 여전히 평소처럼 업데이트되고 복구되고 제거되는 것을 확인하면 그 폴더를 삭제하세요. {InstallerFolder}에 다시 넣으면 모든 것이 원래대로 돌아옵니다. 아니면 지금 바로 영구 삭제하세요. |
| Nothing scanned yet. | 아직 검사하지 않았습니다. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | 다시 검사를 눌러 {InstallerFolder}에서 더 이상 어떤 프로그램도 필요로 하지 않는 설치 관리자 파일을 찾아보세요. |
| These files can't be cleaned up right now. | 지금은 이 파일들을 정리할 수 없습니다. |
| Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | 지금 무언가가 Windows Installer를 사용하고 있습니다. Windows 업데이트이거나 백그라운드에서 설치 중인 프로그램일 수 있습니다. 그동안 이동과 삭제는 일시 중지되어, InstallerClean이 변경 중인 {InstallerFolder}를 건드리지 않습니다. 끝나면 다시 검사하면 두 기능이 돌아옵니다. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | 이 컴퓨터에 이전 Windows Installer 트랜잭션이 중단된 채 남아 있습니다. {InstallerFolder}를 정리하기 전에 그 설치를 계속하거나 되돌리세요(또는 Windows를 다시 시작하세요). |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows가 다음 재시작 때 처리할 파일 이름 변경을 대기열에 넣어 두었고, 그 대상이 {InstallerFolder}입니다. 정리하기 전에 Windows를 다시 시작하세요. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer가 진행 중인 작업이 있어 이동과 삭제가 일시 중지되었습니다. InstallerClean은 변경 중인 {InstallerFolder}를 건드리지 않습니다. 끝나면 다시 검사하면 두 기능이 돌아옵니다. |
| Select a file to view details. | 세부 정보를 보려면 파일을 선택하세요. |
| Select a product to view details. | 세부 정보를 보려면 제품을 선택하세요. |
| No metadata available. | 사용할 수 있는 메타데이터가 없습니다. |
| This installer file is missing. It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it, and its own last resort is reinstalling the program. | 이 설치 관리자 파일이 없습니다. 지금은 아무 문제도 일으키지 않으며, 이 파일이 속한 프로그램을 복구하거나 업데이트하거나 제거하려는 날이 오기 전까지는 문제가 되지 않습니다. 그때는 Windows가 이 파일을 찾는데 파일이 없으므로 그 단계가 실패할 수 있습니다.<br><br>해결을 시도하려면 해당 프로그램의 설치 관리자를 제조사에서 내려받아 기존 설치본 위에 실행하세요(먼저 제거하지 마세요. 제거 자체가 이 파일을 필요로 하는 단계입니다). 구할 수 있다면 설치되어 있는 것과 같은 버전을 사용하세요. Windows가 다른 버전을 거부할 수 있습니다. 이렇게 하면 파일이 복구되고 설정은 그대로 유지되는 것이 보통이지만 Microsoft가 보장하지는 않으며, Microsoft의 최후 수단은 프로그램을 다시 설치하는 것입니다. |
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
| Nothing removed | 제거된 것 없음 |
| Nothing to clean up in {InstallerFolder} | {InstallerFolder}에 정리할 것이 없습니다 |
| Scanned {0} {1} in {2} | {1} {0}개 검사, {2} 소요 |
| Nothing offered on this PC | 이 PC에서는 아무것도 제시하지 않았습니다 |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({1}) it might otherwise have offered. | InstallerClean이 캐시에 있는 어떤 파일이 여기 설치된 프로그램에 속하는지 확실히 알 수 없어서, 제시할 수도 있었던 파일 하나({1})를 그대로 두었습니다. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back all {0} files ({1}) it might otherwise have offered. | InstallerClean이 캐시에 있는 어떤 파일이 여기 설치된 프로그램에 속하는지 확실히 알 수 없어서, 제시할 수도 있었던 파일 {0}개({1}) 전부를 그대로 두었습니다. |
| The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | 그 폴더에 있는 파일은 [안전하게 제거할 수 있으므로], 원하실 때 폴더를 삭제하세요. 그때까지는 어떤 프로그램이 정말로 필요로 하는 경우 {InstallerFolder}에 다시 넣을 수 있습니다(가능성은 극히 낮습니다). |
| The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | 그 폴더에 있는 파일들은 [안전하게 제거할 수 있으므로], 원하실 때 폴더를 삭제하세요. 그때까지는 어떤 프로그램이 그중 하나를 정말로 필요로 하는 경우 {InstallerFolder}에 다시 넣을 수 있습니다(가능성은 극히 낮습니다). |
| The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | 그 폴더에 있는 파일은 [안전하게 제거할 수 있으므로], 공간을 실제로 되찾고 싶을 때 폴더를 삭제하거나 다른 드라이브로 옮기세요. 그때까지는 어떤 프로그램이 정말로 필요로 하는 경우 {InstallerFolder}에 다시 넣을 수 있습니다(가능성은 극히 낮습니다). |
| The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | 그 폴더에 있는 파일들은 [안전하게 제거할 수 있으므로], 공간을 실제로 되찾고 싶을 때 폴더를 삭제하거나 다른 드라이브로 옮기세요. 그때까지는 어떤 프로그램이 그중 하나를 정말로 필요로 하는 경우 {InstallerFolder}에 다시 넣을 수 있습니다(가능성은 극히 낮습니다). |
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
| {0} {1} kept in place, because the records now claim what the scan flagged. | {1} {0}개를 그대로 두었습니다. 기록이 이제 검사에서 표시한 것을 자기 것이라고 하기 때문입니다. |
| {0} {1} kept in place, because the Windows Installer records had changed by the final check. | {1} {0}개를 그대로 두었습니다. 마지막 확인 시점에는 Windows Installer 기록이 이미 바뀌어 있었기 때문입니다. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read in the final check. | {1} {0}개를 그대로 두었습니다. 마지막 확인에서 Windows Installer 기록을 전부 읽지 못했기 때문입니다. |
| {0} {1} kept in place, because by the final check InstallerClean could not be certain which cached files belong to the programs installed here. | {1} {0}개를 그대로 두었습니다. 마지막 확인 시점까지 InstallerClean이 캐시에 있는 어떤 파일이 여기 설치된 프로그램에 속하는지 확실히 알 수 없었기 때문입니다. |
| {0} {1} kept in place, because Windows has a record of the program named inside. | {1} {0}개를 그대로 두었습니다. 파일 안에 이름이 적힌 프로그램의 기록을 Windows가 가지고 있기 때문입니다. |
| {0} {1} kept in place, because InstallerClean couldn't find a program named inside. | {1} {0}개를 그대로 두었습니다. InstallerClean이 파일 안에서 프로그램 이름을 찾지 못했기 때문입니다. |
| Moved {0} of {1} {2} before you cancelled. | 취소하기 전까지 {2} {1}개 중 {0}개를 이동했습니다. |
| Permanently deleted {0} of {1} {2} before you cancelled. | 취소하기 전까지 {2} {1}개 중 {0}개를 영구 삭제했습니다. |
| {0} {1} permanently deleted | {1} {0}개 영구 삭제됨 |
| {0} {1} permanently deleted | {1} {0}개 영구 삭제됨 |
| Glad to help. There's a tip jar if you're feeling kind. | 도움이 되어 기쁩니다. 너그러운 마음이 있으시면 작은 성의도 반갑습니다. |

## Summaries and counts

| English | 한국어 |
| --- | --- |
| {0} file left alone | 파일 {0}개 그대로 둠 |
| {0} files left alone | 파일 {0}개 그대로 둠 |
| {0} unneeded file to clean up | 정리할 불필요한 파일 {0}개 |
| {0} unneeded files to clean up | 정리할 불필요한 파일 {0}개 |
| Windows has a record for {0} file that isn't in {InstallerFolder}: {1}. It causes no trouble day to day, but a repair, update or uninstall can fail on it. Open Details for what to do. | {InstallerFolder}에 없는 파일 {0}개에 대한 기록이 Windows에 있습니다: {1}. 평소에는 문제가 되지 않지만, 복구나 업데이트, 제거가 이 때문에 실패할 수 있습니다. 무엇을 해야 하는지는 세부 정보를 열어 확인하세요. |
| Windows has records for {0} files that aren't in {InstallerFolder}: {1}. They cause no trouble day to day, but a repair, update or uninstall can fail on them. Open Details for what to do. | {InstallerFolder}에 없는 파일 {0}개에 대한 기록이 Windows에 있습니다: {1}. 평소에는 문제가 되지 않지만, 복구나 업데이트, 제거가 이 때문에 실패할 수 있습니다. 무엇을 해야 하는지는 세부 정보를 열어 확인하세요. |
| {0} other program | 다른 프로그램 {0}개 |
| {0} other programs | 다른 프로그램 {0}개 |
| {0} file with no program named in the records | 기록에 프로그램 이름이 없는 파일 {0}개 |
| {0} files with no program named in the records | 기록에 프로그램 이름이 없는 파일 {0}개 |
| On this PC InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file rather than listing it. | 이 PC에서는 InstallerClean이 캐시에 있는 어떤 파일이 여기 설치된 프로그램에 속하는지 확실히 알 수 없어서, 그 파일 하나를 목록에 올리지 않고 그대로 두었습니다. |
| On this PC InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} rather than listing them. | 이 PC에서는 InstallerClean이 캐시에 있는 어떤 파일이 여기 설치된 프로그램에 속하는지 확실히 알 수 없어서, {1} {0}개를 목록에 올리지 않고 그대로 두었습니다. |
| InstallerClean couldn't match up everything in the Windows records, so it didn't read all of them. The unneeded files above are unaffected, but what it says about files missing from {InstallerFolder} may be short of the full picture. Re-scan to try again. | InstallerClean이 Windows 기록에 있는 모든 것을 대조하지 못해서 전부 읽지는 못했습니다. 위의 불필요한 파일은 영향을 받지 않지만, {InstallerFolder}에서 빠진 파일에 대한 설명은 전체를 담고 있지 않을 수 있습니다. 다시 검사해서 한 번 더 시도해 보세요. |
| {0} of {1} {2} | {2} {1}개 중 {0}개 |
| {0} unneeded {1} ({2}) | 불필요한 {1} {0}개 ({2}) |
| {0} file left alone ({1}) | 파일 {0}개 그대로 둠 ({1}) |
| {0} files left alone ({1}) | 파일 {0}개 그대로 둠 ({1}) |

## Confirmation dialogs

| English | 한국어 |
| --- | --- |
| Move {0} {1} ({2})? | {1} {0}개를 이동하시겠습니까? ({2}) |
| Move to: | 이동할 위치: |
| Delete {0} {1} ({2})? | {1} {0}개를 삭제하시겠습니까? ({2}) |
| This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. | 이 파일은 영구히 삭제됩니다. [안전하게 삭제할 수 있지만], 백업을 원하시면 대신 이동 단추를 사용하세요. |
| Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. | 파일들이 영구히 삭제됩니다. [안전하게 삭제할 수 있지만], 백업을 원하시면 대신 이동 단추를 사용하세요. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | 그 폴더는 같은 드라이브에 있어서, 폴더를 삭제하기 전까지는 공간이 돌아오지 않습니다. 공간을 바로 확보하려면 다른 드라이브의 폴더를 선택하세요. |

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
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean이 Windows Installer 기록을 {InstallerFolder}의 내용과 대조하지 못했습니다. 기록이 가리키는 것 중 실제로 그곳에 있는 것이 거의 없고, 그곳에 있는 것 중 어떤 기록에도 이름이 없는 것이 거의 전부여서, 어떤 파일도 불필요하다고 밝힐 수 없었습니다. 아무것도 제시하지 않았고 아무것도 제거하지 않았습니다. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean이 Windows Installer 기록을 {InstallerFolder}의 내용과 대조하지 못했습니다. 폴더에 파일은 있지만 그 안의 어떤 것도 가리키는 기록이 하나도 없어서, 어떤 파일도 불필요하다고 밝힐 수 없었습니다. 아무것도 제시하지 않았고 아무것도 제거하지 않았습니다. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean이 무엇이 아직 필요한지 확신할 만큼 Windows Installer 기록을 읽지 못했습니다. 설치된 프로그램 목록이 일부 빠진 채로 돌아왔고, 같은 기록을 레지스트리에서 직접 읽는 것도 오류를 만났습니다. 어떤 파일을 가리키는 기록이 읽을 수 없는 것 중 하나였다는 이유만으로 그 파일이 고립된 것처럼 보일 수 있으므로 InstallerClean은 멈췄습니다. 아무것도 제거되지 않았습니다. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean이 Windows로부터 {InstallerFolder}의 실제 경로를 확인받지 못해서, 어떤 파일도 그 안에 있다고 밝힐 수 없었고 정리 대상으로 제시된 파일도 없습니다. 이번 검사가 아무것도 찾지 못한 것은 폴더가 깨끗해서가 아니라 그 확인이 실패했기 때문입니다. 아무것도 제거하지 않았습니다. |
| Nothing was deleted | 삭제된 파일 없음 |
| Nothing was moved | 이동된 파일 없음 |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | 두 프로그램이 설치된 소프트웨어를 동시에 변경하지 못하도록 Windows Installer가 사용하는 잠금을 InstallerClean이 가져오지 못해서, 작업 도중에 어떤 파일이 필요해지지 않는다고 확신할 수 없었고 아무것도 삭제하지 않았습니다. 다시 시도해 보시고, 계속 이러면 Windows를 다시 시작하세요. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. Try again, and restart Windows if it keeps happening. | 두 프로그램이 설치된 소프트웨어를 동시에 변경하지 못하도록 Windows Installer가 사용하는 잠금을 InstallerClean이 가져오지 못해서, 작업 도중에 어떤 파일이 필요해지지 않는다고 확신할 수 없었고 아무것도 이동하지 않았습니다. 다시 시도해 보시고, 계속 이러면 Windows를 다시 시작하세요. |
| Invalid destination | 잘못된 대상 |
| Could not write to destination | 대상에 쓸 수 없음 |
| Move failed | 이동 실패 |
| Delete failed | 삭제 실패 |
| Setting not saved | 설정 저장 실패 |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | 변경 내용을 저장하지 못했습니다. 다음에 실행할 때 InstallerClean은 이전 설정으로 돌아갑니다. |
| The destination cannot be inside the Windows Installer folder. | 대상은 Windows Installer 폴더 안에 있을 수 없습니다. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | 대상 {0}이(가) Windows 시스템 폴더 아래로 확인됩니다. %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)%, %ProgramData% 바깥의 경로를 선택하세요. |
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
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | 이 파일은 다른 프로그램이 열어 두었거나 잠가 두어서 지금은 어떤 것도 제거할 수 없습니다. 파일은 그대로 두었습니다. 나중에 다시 시도하세요. |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | 이 파일들은 다른 프로그램이 열어 두었거나 잠가 두어서 지금은 어떤 것도 제거할 수 없습니다. 파일들은 그대로 두었습니다. 나중에 다시 시도하세요. |
| Windows reported a file error; the file was left in place. | Windows가 파일 오류를 알렸습니다. 파일은 그대로 두었습니다. |
| Windows reported file errors; these files were left in place. | Windows가 파일 오류를 알렸습니다. 이 파일들은 그대로 두었습니다. |
| Something went wrong with this file; it was left in place. | 이 파일에서 문제가 발생했습니다. 파일은 그대로 두었습니다. |
| Something went wrong with these files; they were left in place. | 이 파일들에서 문제가 발생했습니다. 파일은 그대로 두었습니다. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | 파일을 Windows Installer 폴더로 이동하는 것을 거부합니다(대상: {0}). |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | 대상 폴더는 드라이브 문자나 네트워크 공유로 시작하는 폴더의 전체 경로여야 합니다(예: D:\Backup 또는 \\server\backup). InstallerClean은 이 경로를 사용할 수 없습니다: {0} |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | InstallerClean이 대상 폴더를 더 이상 확인할 수 없어서, 엉뚱한 곳에 쓰는 대신 중단했습니다. {0}을(를) 확인한 다음 다시 검사하고 다시 시도하세요. |
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
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log에는 InstallerClean의 처리되지 않은 예외가 기록됩니다.<br># 권한이 상승된 상태에서는 프레임워크의 예외 메시지에 실행 중인<br># 세션의 파일 경로가 포함될 수 있습니다(Windows Installer 쿼리가<br># 열거한 다른 사용자의 프로필 포함). 업데이트 확인이나 결과 로그<br># 전송의 네트워크 실패 메시지에는 대상 URL과 확인된 IP 또는 프록시<br># 주소가 포함될 수 있습니다. 읽을 수 없는 Windows Installer 기록에<br># 대한 항목에는 Windows 계정 SID(S-1-5-21-...)와 설치된 소프트웨어의<br># 제품 코드가 포함될 수 있습니다.<br># 이 파일을 공개 버그 신고에 첨부하기 전에 세 가지 정보를 모두<br># 지우세요.<br> |

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
| Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. | 불필요한 파일을 대상 폴더로 옮깁니다. 아무것도 그 파일들을 필요로 하지 않는다고 확신하게 되면 그 폴더를 삭제하세요. |
| Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. | 불필요한 파일을 대상 폴더로 옮깁니다. 폴더는 곧이어 선택하게 됩니다. 아무것도 그 파일들을 필요로 하지 않는다고 확신하게 되면 그 폴더를 삭제하세요. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. | 불필요한 파일을 대상 폴더로 옮깁니다. 그 폴더가 같은 드라이브에 있어서, 폴더를 삭제하거나 다른 드라이브로 옮기기 전까지는 공간을 되찾지 못합니다. 아무것도 그 파일들을 필요로 하지 않는다고 확신하게 되면 그렇게 하시면 됩니다. |
| Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. | 불필요한 파일을 영구히 삭제합니다. 안전하게 제거할 수 있으며 공간은 바로 돌아옵니다. |
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
| Delete permanently removes the unneeded files. Cancel closes without deleting. | 영구 삭제하면 불필요한 파일이 제거됩니다. 취소하면 아무것도 삭제하지 않고 닫습니다. |
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
| Backup folder | 대상 폴더 |
| Patches | 패치 |
| Product details | 제품 세부 정보 |
| Backup folder | 대상 폴더 |
| Operation progress | 작업 진행 상황 |
| Scan {InstallerFolder} again | {InstallerFolder} 다시 검사 |
| Scanning progress | 검사 진행 상황 |
| Startup scan progress | 시작 검사 진행 상황 |
| Details, unneeded files | 세부 정보, 불필요한 파일 |
| Available for cleanup. | 정리할 수 있습니다. |
| Details, files left alone | 세부 정보, 그대로 둔 파일 |
| Read-only inventory. | 읽기 전용 목록입니다. |
| Sorted by {0}, ascending | {0} 기준 오름차순 정렬됨 |
| Sorted by {0}, descending | {0} 기준 내림차순 정렬됨 |
| Scan results | 검사 결과 |
| Result details | 결과 세부 정보 |
| File details | 파일 세부 정보 |
| Product details | 제품 세부 정보 |
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
| ,  | ,  |
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
| Error: unknown argument '{0}' | 오류: 알 수 없는 인수 '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | 오류: 예상치 못한 추가 인수 '{0}'. 이동 폴더 경로에 공백이 있으면 전체 경로를 큰따옴표로 묶으세요: /m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | 오류: 예상치 못한 추가 인수 '{0}'. /s와 /d는 다른 인수를 받지 않으며, 한 번 실행에 플래그는 하나만 쓸 수 있습니다. |
| Cancelling... | 취소 중... |
| Cancelled. | 취소되었습니다. |
| Error: unexpected failure ({0}). Details written to {1}. | 오류: 예상치 못한 실패({0}). 자세한 내용을 {1}에 기록했습니다. |
| Error: unexpected failure ({0}). The crash log could not be written. | 오류: 예상치 못한 실패({0}). 크래시 로그를 기록하지 못했습니다. |
| Scanning {InstallerFolder}... | {InstallerFolder} 검사 중... |
| Found {0} unneeded {1} to clean up ({2}). | 정리할 불필요한 {1} {0}개를 찾았습니다 ({2}). |
| Found no unneeded files. | 불필요한 파일을 찾지 못했습니다. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it held back the one file ({2}) it might otherwise have offered. | InstallerClean이 캐시에 있는 어떤 파일이 여기 설치된 프로그램에 속하는지 확실히 알 수 없어서, 제시할 수도 있었던 파일 하나({2})를 그대로 두었습니다. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it held back all {0} {1} ({2}) it might otherwise have offered. | InstallerClean이 캐시에 있는 어떤 파일이 여기 설치된 프로그램에 속하는지 확실히 알 수 없어서, 제시할 수도 있었던 {1} {0}개({2}) 전부를 그대로 두었습니다. |
| Windows has a record for {0} file that is not in {InstallerFolder}: {1}. It causes no trouble day to day, but a repair, update or uninstall can fail on it. Running that program's installer again, preferably the same version, usually restores the file. | {InstallerFolder}에 없는 파일 {0}개에 대한 기록이 Windows에 있습니다: {1}. 평소에는 문제가 되지 않지만, 복구나 업데이트, 제거가 이 때문에 실패할 수 있습니다. 그 프로그램의 설치 관리자를, 되도록 같은 버전으로 다시 실행하면 대개 파일이 복구됩니다. |
| Windows has records for {0} files that are not in {InstallerFolder}: {1}. They cause no trouble day to day, but a repair, update or uninstall can fail on them. Running each program's installer again, preferably the same version, usually restores the files. | {InstallerFolder}에 없는 파일 {0}개에 대한 기록이 Windows에 있습니다: {1}. 평소에는 문제가 되지 않지만, 복구나 업데이트, 제거가 이 때문에 실패할 수 있습니다. 각 프로그램의 설치 관리자를, 되도록 같은 버전으로 다시 실행하면 대개 파일이 복구됩니다. |
| InstallerClean couldn't match up everything in the Windows records, so it didn't read all of them. What it found is unaffected, but what it says about files missing from {InstallerFolder} may be short of the full picture. Running it again may pick up more. | InstallerClean이 Windows 기록에 있는 모든 것을 대조하지 못해서 전부 읽지는 못했습니다. 찾아낸 것은 영향을 받지 않지만, {InstallerFolder}에서 빠진 파일에 대한 설명은 전체를 담고 있지 않을 수 있습니다. 다시 실행하면 더 찾아낼 수도 있습니다. |
| Deleting {0} unneeded {1}... | 불필요한 {1} {0}개 삭제 중... |
| Permanently deleted {0} unneeded {1}. | 불필요한 {1} {0}개를 영구 삭제했습니다. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | 오류: 이동 대상이 지정되지 않았습니다. /m 경로를 사용하세요. (GUI에서 설정한 기본값은 사용자별로 저장되므로, 예약된 작업이나 서비스 계정 실행에는 적용되지 않습니다.) |
| Error: destination cannot be inside the Windows Installer folder. | 오류: 대상은 Windows Installer 폴더 안에 있을 수 없습니다. |
| Error: destination must be a fully qualified path. Got: {0} | 오류: 대상은 정규화된 전체 경로여야 합니다. 입력값: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | 오류: 대상 {0}이(가) Windows 시스템 폴더 아래로 확인됩니다. %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)%, %ProgramData% 바깥의 경로를 선택하세요. |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | 오류: {0}에 공간이 부족합니다. 이 파일들을 옮기려면 {1}이(가) 필요한데 {2}만 남아 있습니다. 아무것도 이동하지 않았습니다. |
| Error: something is using Windows Installer right now, such as a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | 오류: 지금 무언가가 Windows Installer를 사용하고 있습니다. Windows 업데이트이거나 백그라운드에서 설치 중인 프로그램일 수 있습니다. 그동안 /m과 /d는 차단됩니다. 끝나면 다시 시도하세요. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | 오류: 이 컴퓨터에 이전 Windows Installer 트랜잭션이 중단된 채 남아 있습니다. {InstallerFolder}를 정리하기 전에 그 설치를 계속하거나 되돌리세요(또는 Windows를 다시 시작하세요). |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | 오류: 재시작 후에 처리하도록 대기열에 든 파일 작업이 {InstallerFolder}를 대상으로 합니다({0}). 정리하기 전에 Windows를 다시 시작해 그 작업을 끝내세요. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | 오류: Windows Installer가 진행 중인 작업이 있어 /m과 /d가 차단되었습니다. InstallerClean은 변경 중인 {InstallerFolder}를 건드리지 않습니다. 끝나면 다시 시도하세요. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | 오류: 두 프로그램이 설치된 소프트웨어를 동시에 변경하지 못하도록 하는 Windows Installer 잠금을 InstallerClean이 가져오지 못해서, 작업 도중에 어떤 파일이 필요해지지 않는다고 확신할 수 없었습니다. 아무것도 삭제하지 않았습니다. 다시 시도해 보시고, 계속 이러면 Windows를 다시 시작하세요. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. Try again, and restart Windows if it keeps happening. | 오류: 두 프로그램이 설치된 소프트웨어를 동시에 변경하지 못하도록 하는 Windows Installer 잠금을 InstallerClean이 가져오지 못해서, 작업 도중에 어떤 파일이 필요해지지 않는다고 확신할 수 없었습니다. 아무것도 이동하지 않았습니다. 다시 시도해 보시고, 계속 이러면 Windows를 다시 시작하세요. |
| Moving {0} unneeded {1} to {2}... | 불필요한 {1} {0}개를 {2}(으)로 이동 중... |
| Moved {0} unneeded {1}. | 불필요한 {1} {0}개를 이동했습니다. |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then run the command again. | InstallerClean이 대상 폴더를 더 이상 확인할 수 없어서, 엉뚱한 곳에 쓰는 대신 중단했습니다. {0}을(를) 확인한 다음 명령을 다시 실행하세요. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | 다른 InstallerClean 프로세스가 단일 인스턴스 잠금을 보유하고 있습니다(GUI 또는 다른 CLI 실행). 종료 코드 75(일시적); 나중에 다시 시도해도 안전합니다. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | 참고: 이벤트 로그 쓰기에 실패했습니다. 응용 프로그램 로그 권한 또는 그룹 정책을 확인하세요. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - {InstallerFolder} 정리 |
| Removes cached .msi and .msp files that no installed program still needs. | 설치된 어떤 프로그램도 더는 필요로 하지 않는 .msi/.msp 파일을 제거합니다. |
| Needs an elevated (administrator) prompt; Windows will not start it. | 관리자 명령 프롬프트가 필요하며, 아니면 Windows가 실행하지 않습니다. |
| Usage: | 사용법: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     이 도움말 표시 (/?, -h도 사용 가능) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  버전 출력 (-v도 사용 가능) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         검사만 - 불필요한 파일 나열 |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         불필요한 파일 영구 삭제 |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         저장된 대상 폴더로 이동 |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m 경로    지정한 경로로 이동 |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli는 끝날 때까지 프롬프트를 붙잡고 있으므로 스크립트나<br>예약 작업이 이를 기다릴 수 있습니다. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | 폴더는 사용자별로 저장되며, 예약 실행에는 /m 경로가 필요합니다. |
| Exit codes: | 종료 코드: |
|   0   success: the run did what it was asked and nothing failed |   0   성공: 요청한 일을 했고 실패한 것이 없음 |
|   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |   1   실패: 아무것도 처리되지 않음 (잘못된 인수나 대상,<br>       검사 실패 또는 모든 파일 실패) |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   부분: 일부는 처리되고 일부는 안 됨 (실패 또는 Ctrl+C) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  일시적: 일시적인 상황으로 실행이 차단됨 (메시지 참고) |
|   130 cancelled (Ctrl+C) |   130 취소됨 (Ctrl+C) |
