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
| BACKUP FOLDER | 백업 폴더 |
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
| (no program) | (프로그램 없음) |
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
| {0}. Details are in {1}. | {0}. 자세한 내용은 {1}에 있습니다. |
| {0}. The crash log could not be written. | {0}. 크래시 로그를 기록할 수 없었습니다. |
| {0}. Details are in {1}. | {0}. 자세한 내용은 {1}에 있습니다. |
| {0}. The crash log could not be written. | {0}. 크래시 로그를 기록할 수 없었습니다. |
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
| Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update and uninstall as normal. Putting them back into {InstallerFolder} restores everything. Or delete them permanently now. | 직접 고른 백업 폴더로 옮긴 다음, 프로그램이 여전히 정상적으로 업데이트되고 제거되는지 확인되면 그 폴더를 삭제하세요. {InstallerFolder}에 다시 넣으면 모두 원래대로 돌아갑니다. 아니면 지금 영구히 삭제하세요. |
| Nothing scanned yet. | 아직 검사하지 않았습니다. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | 다시 검사를 눌러 {InstallerFolder}에서 더 이상 어떤 프로그램도 필요로 하지 않는 설치 관리자 파일을 찾아보세요. |
| These files can't be cleaned up right now. | 지금은 이 파일들을 정리할 수 없습니다. |
| Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | 지금 무언가가 Windows Installer를 사용하고 있습니다. Windows 업데이트이거나 백그라운드에서 설치 중인 프로그램일 수 있습니다. 그동안 이동과 삭제는 일시 중지되어, InstallerClean이 변경 중인 {InstallerFolder}를 건드리지 않습니다. 끝나면 다시 검사하면 두 기능이 돌아옵니다. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | 이 컴퓨터에 이전 Windows Installer 트랜잭션이 중단된 채 남아 있습니다. {InstallerFolder}를 정리하기 전에 그 설치를 계속하거나 되돌리세요(또는 Windows를 다시 시작하세요). |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows가 다음 재시작 때 처리할 파일 이름 변경을 대기열에 넣어 두었고, 그 대상이 {InstallerFolder}입니다. 정리하기 전에 Windows를 다시 시작하세요. |
| A file operation is queued for the next restart and InstallerClean can't tell which files it names, so it can't rule out that they're in {InstallerFolder}. Restart Windows before cleaning. | 다음 재시작을 위해 예약된 파일 작업이 있는데 InstallerClean은 그 작업이 어떤 파일을 가리키는지 알 수 없으므로, 그 파일들이 {InstallerFolder}에 있지 않다고 단정할 수 없습니다. 정리하기 전에 Windows를 다시 시작하세요. |
| InstallerClean couldn't read one of the Windows settings it checks before touching {InstallerFolder}, so it can't tell whether an installer operation is running or waiting for a restart. Restart Windows and Re-scan. If the setting still won't read, this isn't a machine InstallerClean can clean. | InstallerClean이 {InstallerFolder}를 건드리기 전에 확인하는 Windows 설정 중 하나를 읽을 수 없어서, 설치 작업이 실행 중인지 재시작을 기다리는지 알 수 없습니다. Windows를 다시 시작한 뒤 다시 검사하세요. 그래도 설정을 읽을 수 없다면, 이 PC는 InstallerClean이 정리할 수 있는 PC가 아닙니다. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer가 진행 중인 작업이 있어 이동과 삭제가 일시 중지되었습니다. InstallerClean은 변경 중인 {InstallerFolder}를 건드리지 않습니다. 끝나면 다시 검사하면 두 기능이 돌아옵니다. |
| Select a file to view details. | 세부 정보를 보려면 파일을 선택하세요. |
| Select a product to view details. | 세부 정보를 보려면 제품을 선택하세요. |
| No metadata available. | 사용할 수 있는 메타데이터가 없습니다. |
| This installer file is missing. It causes no trouble now, and won't until the day you try to update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To put it back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | 이 설치 파일이 없습니다. 지금은 아무 문제도 일으키지 않으며, 이 파일이 속한 프로그램을 업데이트하거나 제거하려는 날이 오기 전까지는 문제가 없습니다. 그때 Windows가 이 파일을 찾지 못해 그 단계가 실패할 수 있습니다.<br><br>되돌리려면 지금 사용 중인 버전의 설치 프로그램이 필요합니다. 프로그램 제작사에서 구해 기존 설치본 위에 실행하세요. 더 새 버전으로는 되지 않습니다. 새 버전은 먼저 지금 있는 것을 제거해야 하는데, 바로 그 단계에 이 파일이 필요하기 때문입니다. 먼저 제거하는 방법도 같은 이유로 되지 않습니다. 이렇게 하면 파일이 복원되고 설정은 그대로 남아야 하지만, Microsoft가 보장하지는 않습니다. |
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
| Nothing offered on this PC | 이 PC에서는 아무것도 제시하지 않았습니다 |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({2}) rather than offering it. | InstallerClean이 캐시에 있는 어떤 파일이 여기 설치된 프로그램에 속하는지 확실히 알 수 없어서, 파일 하나({2})를 제시하지 않고 보류했습니다. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back all {0} {1} ({2}) rather than offering them. | InstallerClean이 캐시에 있는 어떤 파일이 여기 설치된 프로그램에 속하는지 확실히 알 수 없어서, {1} {0}개({2}) 전부를 제시하지 않고 보류했습니다. |
| InstallerClean couldn't establish that the cached file it found is unneeded, so it has held back the one file ({2}) rather than offering it. | InstallerClean이 찾은 캐시 파일이 필요 없다는 것을 확인하지 못해서, 그 파일 하나({2})를 제시하지 않고 보류했습니다. |
| InstallerClean couldn't establish that any of the cached files it found are unneeded, so it has held back all {0} {1} ({2}) rather than offering them. | InstallerClean이 찾은 캐시 파일 중 어느 것도 필요 없다는 것을 확인하지 못해서, {1} {0}개({2}) 전부를 제시하지 않고 보류했습니다. |
| Delete that folder when you're satisfied all is well. | 모든 것이 괜찮다고 확신하게 되면 그 폴더를 삭제하세요. |
| Delete that folder when you're satisfied all is well. You won't actually reclaim the space until you do. | 모든 것이 괜찮다고 확신하게 되면 그 폴더를 삭제하세요. 그때까지는 공간이 실제로 확보되지 않습니다. |
| {0} freed | {0} 확보 |
| {0} moved | {0} 이동 |
| Nothing was moved | 이동된 파일 없음 |
| Nothing was deleted | 삭제된 파일 없음 |
| {0} file could not be moved. | 파일 {0}개를 이동하지 못했습니다. |
| {0} files could not be moved. | 파일 {0}개를 이동하지 못했습니다. |
| {0} file could not be deleted. | 파일 {0}개를 삭제하지 못했습니다. |
| {0} files could not be deleted. | 파일 {0}개를 삭제하지 못했습니다. |
| {0} {1} moved to: {2} | {1} {0}개를 다음 위치로 이동함: {2} |
| {0} {1} moved to: {2} | {1} {0}개를 다음 위치로 이동함: {2} |
| {0} file held back. The scan said it was unneeded. The final check couldn't confirm that. | {0}개 파일을 보류했습니다. 검사는 필요 없다고 했지만, 최종 확인은 그것을 확인해 주지 못했습니다. |
| {0} files held back. The scan said these were unneeded. The final check couldn't confirm that. | {0}개 파일을 보류했습니다. 검사는 필요 없다고 했지만, 최종 확인은 그것을 확인해 주지 못했습니다. |
| {0} {1} kept in place, because Windows has a record of the program named inside. | {1} {0}개를 그대로 두었습니다. 파일 안에 이름이 적힌 프로그램의 기록을 Windows가 가지고 있기 때문입니다. |
| {0} {1} kept in place, because InstallerClean couldn't find a program named inside. | {1} {0}개를 그대로 두었습니다. InstallerClean이 파일 안에서 프로그램 이름을 찾지 못했기 때문입니다. |
| Moved {0} of {1} {2} to {3} before you cancelled. | 취소하기 전까지 {2} {1}개 중 {0}개를 {3}(으)로 이동했습니다. |
| Permanently deleted {0} of {1} {2} before you cancelled. | 취소하기 전까지 {2} {1}개 중 {0}개를 영구 삭제했습니다. |
| It's simple to undo. Move them back into {InstallerFolder} and everything will be back to how it was. | 되돌리기는 간단합니다. {InstallerFolder}로 다시 옮기면 모든 것이 원래대로 돌아갑니다. |
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
| Windows has a record for {0} file that isn't in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. Open Details for what to do. | Windows에 {InstallerFolder}에 없는 파일 {0}개의 기록이 있습니다: {1}. 평소에는 문제가 없지만 그 프로그램의 업데이트나 제거가 실패할 수 있습니다. 어떻게 할지는 세부 정보를 여세요. |
| Windows has records for {0} files that aren't in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. Open Details for what to do. | Windows에 {InstallerFolder}에 없는 파일 {0}개의 기록이 있습니다: {1}. 평소에는 문제가 없지만 그 프로그램들의 업데이트나 제거가 실패할 수 있습니다. 어떻게 할지는 세부 정보를 여세요. |
| {0} other program | 다른 프로그램 {0}개 |
| {0} other programs | 다른 프로그램 {0}개 |
| {0} file with no program named in the records | 기록에 프로그램 이름이 없는 파일 {0}개 |
| {0} files with no program named in the records | 기록에 프로그램 이름이 없는 파일 {0}개 |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file rather than offering it. | InstallerClean이 캐시에 있는 어떤 파일이 여기 설치된 프로그램에 속하는지 확실히 알 수 없어서, 파일 하나를 제시하지 않고 보류했습니다. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} rather than offering them. | InstallerClean이 캐시에 있는 어떤 파일이 여기 설치된 프로그램에 속하는지 확실히 알 수 없어서, {1} {0}개를 제시하지 않고 보류했습니다. |
| InstallerClean couldn't be certain about one of the cached files it found, so it has held that one back rather than offering it. | InstallerClean이 찾은 캐시 파일 중 하나에 대해 확신할 수 없어서, 그 파일을 제시하지 않고 보류했습니다. |
| InstallerClean couldn't be certain about some of the cached files it found, so it has held back {0} {1} rather than offering them. | InstallerClean이 찾은 캐시 파일 중 일부에 대해 확신할 수 없어서, {1} {0}개를 제시하지 않고 보류했습니다. |
| InstallerClean couldn't be certain that the one superseded file is no longer needed, so it has held it back. | InstallerClean이 대체된 그 파일 하나가 더 이상 필요하지 않다고 확실히 알 수 없어서, 그 파일을 보류했습니다. |
| InstallerClean couldn't be certain that {0} superseded files are no longer needed, so it has held them back. | InstallerClean이 대체된 파일 {0}개가 더 이상 필요하지 않다고 확실히 알 수 없어서, 그 파일들을 보류했습니다. |
| {0} of {1} {2} | {2} {1}개 중 {0}개 |
| {0} unneeded {1} ({2}) | 불필요한 {1} {0}개 ({2}) |
| {0} file left alone ({1}) | 파일 {0}개 그대로 둠 ({1}) |
| {0} files left alone ({1}) | 파일 {0}개 그대로 둠 ({1}) |
| {0} missing | {0}개 누락 |
| {0} missing | {0}개 누락 |

## Confirmation dialogs

| English | 한국어 |
| --- | --- |
| Move {0} {1} ({2})? | {1} {0}개를 이동하시겠습니까? ({2}) |
| This file will be moved to: | 이 파일을 다음 위치로 옮깁니다: |
| These files will be moved to: | 이 파일들을 다음 위치로 옮깁니다: |
| Delete {0} {1} ({2})? | {1} {0}개를 삭제하시겠습니까? ({2}) |
| This file will be deleted permanently. It's safe to do but if you'd like a backup, use Move instead. | 이 파일은 영구히 삭제됩니다. 안전한 작업이지만 백업을 원하시면 대신 이동을 사용하세요. |
| These files will be deleted permanently. It's safe to do but if you'd like a backup, use Move instead. | 이 파일들은 영구히 삭제됩니다. 안전한 작업이지만 백업을 원하시면 대신 이동을 사용하세요. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | 그 폴더는 같은 드라이브에 있어서, 폴더를 삭제하기 전까지는 공간이 돌아오지 않습니다. 공간을 바로 확보하려면 다른 드라이브의 폴더를 선택하세요. |

## Error messages

| English | 한국어 |
| --- | --- |
| This is also recorded in {0}. | 이 내용은 {0}에도 기록됩니다. |
| Access denied | 액세스 거부됨 |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows가 InstallerClean의 접근을 거부해서 작업을 멈췄습니다. 아무것도 제거되지 않았습니다.<br><br>InstallerClean은 이미 관리자 권한으로 실행 중이었으므로 그런 식으로 다시 시작해도 도움이 되지 않습니다. Windows는 무엇이 접근을 거부했는지 더 이상 알려주지 않으므로 구체적으로 시도해 볼 것이 없습니다. |
| Couldn't read the Windows Installer records | Windows Installer 기록을 읽을 수 없습니다 |
| Scan failed | 검사 실패 |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed. | Windows Installer 기록이 완전히 비어서 돌아왔습니다. 설치된 프로그램도, 업데이트도 캐시된 설치 파일을 하나도 요구하지 않습니다. 정상적으로 작동하는 컴퓨터에서는 이런 일이 없으므로(갓 설치한 Windows에도 그런 파일이 있습니다) 기록이 손상되었거나 읽을 수 없었던 것이고, 이 답을 그대로 믿은 검사는 {InstallerFolder}의 모든 파일을 잘못 고립된 것으로 판단했을 것입니다. InstallerClean은 그러지 않고 멈췄습니다. 아무것도 제거되지 않았습니다. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer가 InstallerClean에게 설치된 항목의 목록 표시를 허용하지 않았습니다. InstallerClean은 이미 관리자 권한으로 실행 중이었으므로 관리자 권한으로 다시 실행해도 달라지는 것이 없습니다. 그 목록이 없으면 캐시된 파일 중 어느 것이 아직 필요한지 안전하게 알아낼 방법이 없으므로 InstallerClean은 멈췄습니다. 아무것도 제거되지 않았습니다. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: it read {2} {3}, then {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer가 InstallerClean에게 읽을 수 있는 설치된 프로그램 목록을 주지 못했습니다. {2} {3}을(를) 읽은 다음 {0}개 항목이 연속으로 읽을 수 없는 상태로 돌아왔습니다(마지막 오류 코드 {1}). 일부만 읽은 목록으로 작업하는 대신 InstallerClean은 멈췄습니다. 아무것도 제거되지 않았습니다. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer가 설치된 프로그램 목록의 끝을 끝내 알리지 않았습니다. InstallerClean은 {2} {3}을(를) 읽은 다음 {0}개 항목에서 포기했습니다(마지막 오류 코드 {1}). 끝이 없는 목록은 믿을 수 없으므로 InstallerClean은 멈췄습니다. 아무것도 제거되지 않았습니다. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer가 한 프로그램의 패치 목록의 끝을 끝내 알리지 않았습니다. InstallerClean은 {2} {3}을(를) 읽은 다음 {0}개 항목에서 포기했습니다(마지막 오류 코드 {1}). 끝이 없는 목록은 믿을 수 없으므로 InstallerClean은 멈췄습니다. 아무것도 제거되지 않았습니다. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean이 Windows Installer 기록을 {InstallerFolder}의 내용과 대조하지 못했습니다. 기록이 가리키는 것 중 실제로 그곳에 있는 것이 거의 없고, 그곳에 있는 것 중 어떤 기록에도 이름이 없는 것이 거의 전부여서, 어떤 파일도 불필요하다고 밝힐 수 없었습니다. 아무것도 제시하지 않았고 아무것도 제거하지 않았습니다. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean이 Windows Installer 기록을 {InstallerFolder}의 내용과 대조하지 못했습니다. 폴더에 파일은 있지만 그 안의 어떤 것도 가리키는 기록이 하나도 없어서, 어떤 파일도 불필요하다고 밝힐 수 없었습니다. 아무것도 제시하지 않았고 아무것도 제거하지 않았습니다. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean이 무엇이 아직 필요한지 확신할 만큼 Windows Installer 기록을 읽지 못했습니다. 설치된 프로그램 목록이 일부 빠진 채로 돌아왔고, 같은 기록을 레지스트리에서 직접 읽는 것도 오류를 만났습니다. 어떤 파일을 가리키는 기록이 읽을 수 없는 것 중 하나였다는 이유만으로 그 파일이 고립된 것처럼 보일 수 있으므로 InstallerClean은 멈췄습니다. 아무것도 제거되지 않았습니다. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean이 Windows로부터 {InstallerFolder}의 실제 경로를 확인받지 못해서, 어떤 파일도 그 안에 있다고 밝힐 수 없었고 정리 대상으로 제시된 파일도 없습니다. 이번 검사가 아무것도 찾지 못한 것은 폴더가 깨끗해서가 아니라 그 확인이 실패했기 때문입니다. 아무것도 제거하지 않았습니다. |
| Nothing was deleted | 삭제된 파일 없음 |
| Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. | Windows가 InstallerClean에 Windows Installer가 사용 중인지 확인할 권한을 주지 않아, 도중에 파일이 필요해질 가능성을 배제할 수 없었고, 아무것도 삭제되지 않았습니다. |
| Nothing was moved | 이동된 파일 없음 |
| Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. | Windows가 InstallerClean에 Windows Installer가 사용 중인지 확인할 권한을 주지 않아, 도중에 파일이 필요해질 가능성을 배제할 수 없었고, 아무것도 이동되지 않았습니다. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | 두 프로그램이 설치된 소프트웨어를 동시에 변경하지 못하도록 Windows Installer가 사용하는 잠금을 InstallerClean이 가져오지 못해서, 작업 도중에 어떤 파일이 필요해지지 않는다고 확신할 수 없었고 아무것도 삭제하지 않았습니다. 다시 시도해 보시고, 계속 이러면 Windows를 다시 시작하세요. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. Try again, and restart Windows if it keeps happening. | 두 프로그램이 설치된 소프트웨어를 동시에 변경하지 못하도록 Windows Installer가 사용하는 잠금을 InstallerClean이 가져오지 못해서, 작업 도중에 어떤 파일이 필요해지지 않는다고 확신할 수 없었고 아무것도 이동하지 않았습니다. 다시 시도해 보시고, 계속 이러면 Windows를 다시 시작하세요. |
| Invalid destination | 잘못된 대상 |
| Move stopped | 이동 중지됨 |
| Couldn't use that backup folder | 해당 백업 폴더를 사용할 수 없음 |
| Move failed | 이동 실패 |
| Delete failed | 삭제 실패 |
| Setting not saved | 설정 저장 실패 |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | 변경 내용을 저장하지 못했습니다. 다음에 실행할 때 InstallerClean은 이전 설정으로 돌아갑니다. |
| The destination cannot be inside the Windows Installer folder. | 대상은 Windows Installer 폴더 안에 있을 수 없습니다. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | 대상 {0}이(가) Windows 시스템 폴더 아래를 가리킵니다. %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)%, %ProgramData% 바깥의 경로를 선택하세요. |
| Not enough space | 공간 부족 |
| There isn't room at {0}<br><br>Required: {1}<br>Available: {2} | {0}에 자리가 부족합니다<br><br>필요: {1}<br>사용 가능: {2} |
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
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | 백업 폴더는 드라이브 문자나 네트워크 공유로 시작하는 폴더의 전체 경로여야 합니다(예: D:\Backup 또는 \\server\backup). InstallerClean은 이 경로를 사용할 수 없습니다: {0} |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then Re-scan and try again. | InstallerClean이 백업 폴더를 더 이상 확인할 수 없어서, 중단했습니다. {0}을(를) 확인한 다음 다시 검사하고 다시 시도하세요. |
| Cannot write to {0}. | {0}에 쓸 수 없습니다. |
| A file called '{0}' is already in the backup folder. | '{0}'(이)라는 이름의 파일이 이미 백업 폴더에 있습니다. |

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
| The check failed for an unknown reason. Details are in {0} if you need to report it. | 알 수 없는 이유로 확인에 실패했습니다. 신고가 필요하면 자세한 내용이 {0}에 있습니다. |
| The check failed for an unknown reason. The crash log could not be written. | 알 수 없는 이유로 확인에 실패했습니다. 크래시 로그를 기록할 수 없었습니다. |

## Opening links in your browser

| English | 한국어 |
| --- | --- |
| Couldn't open your browser | 브라우저를 열 수 없음 |
| The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | 링크가 클립보드에 있으니 직접 붙여 넣으면 됩니다:<br><br>{0} |
| InstallerClean couldn't copy the link to your clipboard either, so here it is:<br><br>{0} | InstallerClean이 링크를 클립보드에 복사하지도 못했습니다. 링크는 다음과 같습니다:<br><br>{0} |

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
| It's already running. | 이미 실행 중입니다. |
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
| Move the unneeded files to the backup folder. | 불필요한 파일을 백업 폴더로 옮깁니다. |
| Move the unneeded files to a backup folder. You'll choose it next. | 불필요한 파일을 백업 폴더로 옮깁니다. 폴더는 곧이어 선택하게 됩니다. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder. | 불필요한 파일을 백업 폴더로 옮깁니다. 같은 드라이브에 있으므로 그 폴더를 삭제하기 전에는 공간이 확보되지 않습니다. |
| Delete the unneeded files permanently. Use Move instead if you'd like a chance to satisfy yourself all is well. | 불필요한 파일을 영구히 삭제합니다. 모든 것이 괜찮은지 직접 확인해 보고 싶으면 대신 이동을 사용하세요. |
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
| Backup folder | 백업 폴더 |
| Patches | 패치 |
| Product details | 제품 세부 정보 |
| Backup folder | 백업 폴더 |
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
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | 오류: 예상치 못한 추가 인수 '{0}'. 대상 폴더 경로에 공백이 있으면 전체 경로를 큰따옴표로 묶으세요: /m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | 오류: 예상치 못한 추가 인수 '{0}'. /s와 /d는 다른 인수를 받지 않으며, 한 번 실행에 플래그는 하나만 쓸 수 있습니다. |
| Cancelling... | 취소 중... |
| Cancelled. | 취소되었습니다. |
| Error: unexpected failure ({0}). Details written to {1}. | 오류: 예상치 못한 실패({0}). 자세한 내용을 {1}에 기록했습니다. |
| Error: unexpected failure ({0}). The crash log could not be written. | 오류: 예상치 못한 실패({0}). 크래시 로그를 기록하지 못했습니다. |
| Scanning {InstallerFolder}... | {InstallerFolder} 검사 중... |
| Found {0} unneeded {1} to clean up ({2}). | 정리할 불필요한 {1} {0}개를 찾았습니다 ({2}). |
| Found no unneeded files. | 불필요한 파일을 찾지 못했습니다. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({2}) rather than offering it. | InstallerClean이 캐시에 있는 어떤 파일이 여기 설치된 프로그램에 속하는지 확실히 알 수 없어서, 파일 하나({2})를 제시하지 않고 보류했습니다. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back all {0} {1} ({2}) rather than offering them. | InstallerClean이 캐시에 있는 어떤 파일이 여기 설치된 프로그램에 속하는지 확실히 알 수 없어서, {1} {0}개({2}) 전부를 제시하지 않고 보류했습니다. |
| InstallerClean couldn't establish that the cached file it found is unneeded, so it has held back the one file ({2}) rather than offering it. | InstallerClean이 찾은 캐시 파일이 필요 없다는 것을 확인하지 못해서, 그 파일 하나({2})를 제시하지 않고 보류했습니다. |
| InstallerClean couldn't establish that any of the cached files it found are unneeded, so it has held back all {0} {1} ({2}) rather than offering them. | InstallerClean이 찾은 캐시 파일 중 어느 것도 필요 없다는 것을 확인하지 못해서, {1} {0}개({2}) 전부를 제시하지 않고 보류했습니다. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({2}) rather than offering it. | InstallerClean이 캐시에 있는 어떤 파일이 여기 설치된 프로그램에 속하는지 확실히 알 수 없어서, 파일 하나({2})를 제시하지 않고 보류했습니다. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} ({2}) rather than offering them. | InstallerClean이 캐시에 있는 어떤 파일이 여기 설치된 프로그램에 속하는지 확실히 알 수 없어서, {1} {0}개({2})를 제시하지 않고 보류했습니다. |
| InstallerClean couldn't be certain about one of the cached files it found, so it has held that one back ({2}) rather than offering it. | InstallerClean이 찾은 캐시 파일 중 하나에 대해 확신할 수 없어서, 그 파일({2})을 제시하지 않고 보류했습니다. |
| InstallerClean couldn't be certain about some of the cached files it found, so it has held back {0} {1} ({2}) rather than offering them. | InstallerClean이 찾은 캐시 파일 중 일부에 대해 확신할 수 없어서, {1} {0}개({2})를 제시하지 않고 보류했습니다. |
| Why it couldn't be certain: | 확신할 수 없었던 이유: |
|   A file path in Windows Installer's own records wouldn't resolve, so nothing could be matched to it. |   Windows Installer 자체 기록에 있는 파일 경로를 확인할 수 없어서, 그 경로에 아무것도 대조할 수 없었습니다. |
|   A file Windows has a record of couldn't be identified, so it couldn't be matched to what's in the folder. |   Windows에 기록이 있는 파일 하나의 신원을 확인할 수 없어서, 폴더에 있는 것과 대조할 수 없었습니다. |
|   A program may be installed more than once on this PC, and the records can't say which copy a file belongs to. |   이 PC에 같은 프로그램이 두 번 이상 설치되어 있을 수 있고, 기록만으로는 파일이 어느 사본에 속하는지 알 수 없습니다. |
|   A file in the folder couldn't be identified, so it couldn't be matched against the records. |   폴더에 있는 파일 하나의 신원을 확인할 수 없어서, 기록과 대조할 수 없었습니다. |
|   A file says it belongs to a program that is still installed, so it may still be needed. |   어떤 파일이 아직 설치되어 있는 프로그램에 속한다고 밝히고 있어서, 아직 필요할 수 있습니다. |
|   Either a file wouldn't say which program it belongs to, or Windows wouldn't answer about that program. |   어떤 파일이 어느 프로그램에 속하는지 밝히지 않았거나, Windows가 그 프로그램에 대해 답하지 않았습니다. |
|   A check on which programs the files belong to gave answers that didn't line up with the files it was handed. |   파일이 어느 프로그램에 속하는지 확인하는 검사가, 건네받은 파일과 맞지 않는 답을 내놓았습니다. |
| Windows has a record for {0} file that is not in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. To put the file back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Windows에 {InstallerFolder}에 없는 파일 {0}개의 기록이 있습니다: {1}. 평소에는 문제가 없지만 그 프로그램의 업데이트나 제거가 실패할 수 있습니다. 파일을 되돌리려면 지금 사용 중인 버전의 설치 프로그램이 필요합니다. 프로그램 제작사에서 구해 기존 설치본 위에 실행하세요. 더 새 버전으로는 되지 않습니다. 새 버전은 먼저 지금 있는 것을 제거해야 하는데, 바로 그 단계에 이 파일이 필요하기 때문입니다. 먼저 제거하는 방법도 같은 이유로 되지 않습니다. 이렇게 하면 파일이 복원되고 설정은 그대로 남아야 하지만, Microsoft가 보장하지는 않습니다. |
| Windows has records for {0} files that are not in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. To put a file back, you need the installer for the version you already have of that program. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs the file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Windows에 {InstallerFolder}에 없는 파일 {0}개의 기록이 있습니다: {1}. 평소에는 문제가 없지만 그 프로그램들의 업데이트나 제거가 실패할 수 있습니다. 파일을 되돌리려면 그 프로그램의 지금 사용 중인 버전의 설치 프로그램이 필요합니다. 프로그램 제작사에서 구해 기존 설치본 위에 실행하세요. 더 새 버전으로는 되지 않습니다. 새 버전은 먼저 지금 있는 것을 제거해야 하는데, 바로 그 단계에 그 파일이 필요하기 때문입니다. 먼저 제거하는 방법도 같은 이유로 되지 않습니다. 이렇게 하면 파일이 복원되고 설정은 그대로 남아야 하지만, Microsoft가 보장하지는 않습니다. |
| InstallerClean couldn't be certain that the one superseded file is no longer needed, so it has held it back. | InstallerClean이 대체된 그 파일 하나가 더 이상 필요하지 않다고 확실히 알 수 없어서, 그 파일을 보류했습니다. |
| InstallerClean couldn't be certain that {0} superseded files are no longer needed, so it has held them back. | InstallerClean이 대체된 파일 {0}개가 더 이상 필요하지 않다고 확실히 알 수 없어서, 그 파일들을 보류했습니다. |
| Deleting {0} unneeded {1}... | 불필요한 {1} {0}개 삭제 중... |
| Permanently deleted {0} unneeded {1}. | 불필요한 {1} {0}개를 영구 삭제했습니다. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | 오류: 이동 대상이 지정되지 않았습니다. /m 경로를 사용하세요. (GUI에서 설정한 기본값은 사용자별로 저장되므로, 예약된 작업이나 서비스 계정 실행에는 적용되지 않습니다.) |
| Error: destination cannot be inside the Windows Installer folder. | 오류: 대상은 Windows Installer 폴더 안에 있을 수 없습니다. |
| Error: destination must be a fully qualified path. Got: {0} | 오류: 대상은 정규화된 전체 경로여야 합니다. 입력값: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | 오류: 대상 {0}이(가) Windows 시스템 폴더 아래를 가리킵니다. %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)%, %ProgramData% 바깥의 경로를 선택하세요. |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | 오류: {0}에 공간이 부족합니다. 이 파일들을 옮기려면 {1}이(가) 필요한데 {2}만 남아 있습니다. 아무것도 이동하지 않았습니다. |
| Error: something is using Windows Installer right now, such as a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | 오류: 지금 무언가가 Windows Installer를 사용하고 있습니다. Windows 업데이트이거나 백그라운드에서 설치 중인 프로그램일 수 있습니다. 그동안 /m과 /d는 차단됩니다. 끝나면 다시 시도하세요. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | 오류: 이 컴퓨터에 이전 Windows Installer 트랜잭션이 중단된 채 남아 있습니다. {InstallerFolder}를 정리하기 전에 그 설치를 계속하거나 되돌리세요(또는 Windows를 다시 시작하세요). |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | 오류: 재시작 후에 처리하도록 대기열에 든 파일 작업이 {InstallerFolder}를 대상으로 합니다({0}). 정리하기 전에 Windows를 다시 시작해 그 작업을 끝내세요. |
| Error: a file operation is queued for the next restart and InstallerClean can't tell which files it names, so it can't rule out {InstallerFolder}. Restart Windows before cleaning. | 오류: 다음 재시작을 위해 예약된 파일 작업이 있는데 InstallerClean은 그 작업이 어떤 파일을 가리키는지 알 수 없으므로, {InstallerFolder}를 배제할 수 없습니다. 정리하기 전에 Windows를 다시 시작하세요. |
| Error: InstallerClean couldn't read one of the registry values it checks before touching {InstallerFolder}, so it can't rule out a Windows Installer operation in flight or queued for the next restart. /m and /d are blocked. Restart Windows and try again. If the read still fails, this isn't a machine InstallerClean can clean. | 오류: InstallerClean이 {InstallerFolder}를 건드리기 전에 확인하는 레지스트리 값 중 하나를 읽을 수 없어서, 진행 중이거나 다음 재시작을 위해 예약된 Windows Installer 작업을 배제할 수 없습니다. /m과 /d가 차단되었습니다. Windows를 다시 시작한 뒤 다시 시도하세요. 그래도 읽기에 실패하면, 이 PC는 InstallerClean이 정리할 수 있는 PC가 아닙니다. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | 오류: Windows Installer가 진행 중인 작업이 있어 /m과 /d가 차단되었습니다. InstallerClean은 변경 중인 {InstallerFolder}를 건드리지 않습니다. 끝나면 다시 시도하세요. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | 오류: 두 프로그램이 설치된 소프트웨어를 동시에 변경하지 못하도록 하는 Windows Installer 잠금을 InstallerClean이 가져오지 못해서, 작업 도중에 어떤 파일이 필요해지지 않는다고 확신할 수 없었습니다. 아무것도 삭제하지 않았습니다. 다시 시도해 보시고, 계속 이러면 Windows를 다시 시작하세요. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. Try again, and restart Windows if it keeps happening. | 오류: 두 프로그램이 설치된 소프트웨어를 동시에 변경하지 못하도록 하는 Windows Installer 잠금을 InstallerClean이 가져오지 못해서, 작업 도중에 어떤 파일이 필요해지지 않는다고 확신할 수 없었습니다. 아무것도 이동하지 않았습니다. 다시 시도해 보시고, 계속 이러면 Windows를 다시 시작하세요. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. | 오류: Windows가 InstallerClean에 Windows Installer가 사용 중인지 확인할 권한을 주지 않아, 도중에 파일이 필요해질 가능성을 배제할 수 없었습니다. 아무것도 삭제되지 않았습니다. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. | 오류: Windows가 InstallerClean에 Windows Installer가 사용 중인지 확인할 권한을 주지 않아, 도중에 파일이 필요해질 가능성을 배제할 수 없었습니다. 아무것도 이동되지 않았습니다. |
| Moving {0} unneeded {1} to {2}... | 불필요한 {1} {0}개를 {2}(으)로 이동 중... |
| Moved {0} unneeded {1}. | 불필요한 {1} {0}개를 이동했습니다. |
| Check that your programs still update and uninstall as normal, then delete {0}. | 프로그램이 여전히 정상적으로 업데이트되고 제거되는지 확인한 다음 {0}을(를) 삭제하세요. |
| It's simple to undo. Move them back from {0} into {InstallerFolder} and everything will be back to how it was. | 되돌리기는 간단합니다. {0}에서 {InstallerFolder}로 다시 옮기면 모든 것이 원래대로 돌아갑니다. |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then run the command again. | InstallerClean이 백업 폴더를 더 이상 확인할 수 없어서, 중단했습니다. {0}을(를) 확인한 다음 명령을 다시 실행하세요. |
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
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         저장된 백업 폴더로 이동 |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m 경로    지정한 경로로 이동 |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli는 끝날 때까지 프롬프트를 붙잡고 있으므로 스크립트나<br>예약 작업이 이를 기다릴 수 있습니다. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | 폴더는 사용자별 저장. 예약 또는 SYSTEM 실행에는 /m 경로 필요. |
| Exit codes: | 종료 코드: |
|   0   success: the run did what it was asked and nothing failed |   0   성공: 요청한 일을 했고 실패한 것이 없음 |
|   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |   1   실패: 아무것도 처리되지 않음 (잘못된 인수나 대상,<br>       검사 실패 또는 모든 파일 실패) |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   부분: 일부는 처리되고 일부는 안 됨 (실패 또는 Ctrl+C) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  일시적: 일시적인 상황으로 실행이 차단됨 (메시지 참고) |
|   130 cancelled (Ctrl+C) |   130 취소됨 (Ctrl+C) |
