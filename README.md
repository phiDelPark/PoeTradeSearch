[![release](https://img.shields.io/badge/release-Download-brightgreen.svg)](https://github.com/phiDelPark/PoeTradeSearch/releases)
[![License](https://img.shields.io/badge/license-GPL-blue.svg)](https://github.com/phiDelPark/PoeTradeSearch/blob/master/LICENSE)

한글 POE 거래소 검색
-------------

선택한 아이템을 POE 거래소에서 검색합니다.<br>
한글 거래소와 영문 거래소 모두 검색이 가능하며 한쪽의 서버 접속이 안될때 골라 사용 하시면 됩니다.

**한글 POE 전용**이고 영문 POE 는 trade macro 가 있어서 안만들었습니다.

### 개발환경:
* 한글 윈도우 10, 한글 POE 전체 창 모드<br>
* 윈도우 7 은 [프레임워크 버전 4.5.2↗](https://www.microsoft.com/ko-KR/download/details.aspx?id=42642) 필수 (이 버전만 가능, 윈도우 10은 받지마세요)

### 사용:
* 인게임 아이템 위에서 Ctrl+C 하면 검색창이 뜹니다.
* 관리자 모드로 실행시 추가 단축키와 창고 힐 이동 사용이 가능합니다. (관리자 모드 = 파일 우클릭)
### 종료:
* 트레이 아이콘을 우클릭 하시면 됩니다.

### 참고:
 1. 리그 선택은 아래 옵션 파일 설명을 참고하시고 해당 리그를 적어주시면 됩니다.
 2. 그외 자주 묻는 질문이 더 궁굼 하시면 [위키 페이지 Q&A](https://github.com/phiDelPark/PoeTradeSearch/wiki/Q-&-A) 를 참고하세요.

### 옵션 파일 ( Config.txt ) 설명

      "options":{
        "league":"Standard",         // 리그 선택 ["Blight", "Hardcore Blight", "Standard", "Hardcore"]
        "server":"ko",               // 기본 검색 서버 ["ko", "en"]
        "server_timeout":5,          // 서버 접속 대기 시간 (초) 인터넷이 느리면 더 올려 시간 초과를 방지
        "server_redirect":false,     // 일부 환경에서 서버 데이터를 못받아와 거래소 접속이 안되는 경우 사용
        "search_week_before":1,      // 해당 주일 전 물품만 시세 조회 [1 ~ 2]
        "search_by_type":false,      // 기본 검색시 아이템 유형으로 검색 [true, false]
        "auto_select_pseudo":true,   // 유사 옵션으로 검색이 가능하면 유사로 자동 선택 [true, false]
        "check_updates":true,        // 시작시 최신 버전 체크 [true, false]
        "ctrl_wheel":false           // 창고 이동 POE 버전 3.8 이후 기본으로 지원되어 현잰 스탠용으로 남겨둠 (삭제 예정)
      },
       // 창고 Ctrl + Wheel 이동과 아래 단축키들은 관리자 권한으로 실행 필요
       // 키코드(keycode) 는 이 링크를 참고 (https://github.com/phiDelPark/PoeTradeSearch/wiki)
       "shortcuts":[
            {"keycode":113,"value":"{Enter}/hideout{Enter}"},    // F2.  은신처 ("{Enter}채팅명령어{Enter}")
            {"keycode":115,"value":"{Enter}/exit{Enter}"},       // F4.  나가기
            {"keycode":116,"value":"{Enter}/remaining{Enter}"},  // F5.  남은 몬스터
            {"keycode":120,"value":"신디보상표.jpg"},             // F9.  데이터 폴더의 이미지 출력 (단 .jpg만 가능)
            {"keycode":121,"value":"사원보상표.jpg"},             // F10. 주로 이렇게 POE 정보를 이미지 만들어 사용
            {"keycode":122,"value":"{Pause}"},                   // F11. 값이 "{Pause}"면 일시 중지 키로 사용됨
            {"keycode":27,"value":"{Close}"},                    // ESC. 값이 "{Close}"면 창 닫기 키로 사용됨
            {"keycode":78,"ctrl":true,"value":"{Link}URL{Link}"},// Ctrl+N. 링크열기 ("{Link}URL{Link}")
            {"keycode":0,"ctrl":true,"value":"{Run}"}            // 작동키 Ctrl+C. 변경은 키코드 0을 원하는 키로 바꿈
                                                                 // 참고: "ctrl":true 는 Ctrl을 같이 눌러야 한다는 뜻
        ],
      "checked":[ 기본적으로 자동 선택될 옵션들 ]                  // 옵션 파일 ( Config.txt ) 문서 참조

옵션 파일 수정 후엔 저장 후 프로그램을 다시 실행해 주셔야 옵션을 새로 읽어 갱신됩니다.
