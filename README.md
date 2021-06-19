[![release](https://img.shields.io/badge/release-Download-brightgreen.svg)](https://github.com/phiDelPark/PoeTradeSearch/releases)
[![License](https://img.shields.io/badge/license-GPL-blue.svg)](https://github.com/phiDelPark/PoeTradeSearch/blob/master/LICENSE)

POE 거래소 검색 (한글/영문)
-------------

한글/영문 POE 거래소 검색 프로그램입니다.<br>
선택한 아이템을 POE 거래소에서 검색하며 아이템 이름이 한글이면 한국서버 영문이면 영국서버에서 검색합니다.

### 개발환경:
* 윈도우 10, POE 창 모드<br>
* 윈도우 7 은 프레임워크 버전 4.6 이상 필요, [설치법 보러가기↗](https://github.com/phiDelPark/PoeTradeSearch/wiki/Windows-7)

### 사용:
* 인게임 아이템 위에서 Ctrl+C 하면 검색창이 뜹니다.
* **관리자 모드**로 실행시 추가 단축키와 창고 힐 이동 사용이 가능합니다. (``관리자 모드 = 파일 우클릭``)
### 종료:
* 트레이 아이콘을 우클릭 하시면 됩니다.

### 참고:
 1. 리그 선택은 옵션 파일 설명을 참고하시고 해당 리그를 적어주시면 됩니다.
 2. 그외 더 궁굼하시거나 자주 묻는 질문 등은 보려면 [위키 페이지](https://github.com/phiDelPark/PoeTradeSearch/wiki) 를 참고하세요.

### 옵션 파일 ( Config.txt ) 설명

      "options":{
        "league":"Standard",         // 리그 선택 ["Delirium", "Hardcore Delirium", "Standard", "Hardcore"]
        "server_timeout":5,          // 서버 접속 대기 시간 (초) 인터넷이 느리면 더 올려 시간 초과를 방지
        "server_redirect":false,     // 일부 환경에서 서버 데이터를 못받아와 거래소 접속이 안되는 경우 사용
        "search_price_min":0,        // 시세 검색 최소 값 (단위는 카오스 오브입니다, 0 은 모두 검색)
        "auto_check_unique":true,    // 유니크 아이템은 기본적으로 옵션 모두 선택 [true, false]
        "auto_check_totalres":true,  // 저항 옵션의 경우 총 저항 합산 검색 자동 체크 [true, false]
        "auto_select_pseudo":true,   // 유사 옵션으로 검색이 가능하면 유사로 자동 선택 [true, false]
        "auto_select_corrupt":"",    // 검색시 선택한 타락 옵션으로 검색 (장비 한정) ["all", "no", "yes"]
        "auto_select_bytype":"",     // 검색시 이름이 아닌 유형으로 검색 (예: "weapon,armour,accessory")
        "check_updates":true,        // 시작시 최신 버전 체크 [true, false]
        "ctrl_wheel":false           // 창고 Ctrl+Wheel 로 탭이동 (전체 위치에서 가능) [true, false]
      },
       // 아래 단축키들과 창고 휠 이동은 관리자 권한으로 실행이 필요합니다.
       // 키코드(keycode) 는 이 링크를 참고 (https://github.com/phiDelPark/PoeTradeSearch/wiki)
       "shortcuts":[
            {"keycode":113,"value":"{Enter}/hideout{Enter}"},     // F2.  은신처 ("{Enter}채팅명령어{Enter}")
            {"keycode":115,"value":"{Enter}/exit{Enter}"},        // F4.  나가기
            {"keycode":116,"value":"{Enter}/remaining{Enter}"},   // F5.  남은 몬스터
            {"keycode":117,"value":"{Grid:Stash}"},               // F6.  창고 좌표 보기
            {"keycode":120,"value":"신디보상표.jpg"},              // F9.  데이터 폴더의 이미지 출력 (단 .jpg만 가능)
            {"keycode":121,"value":"사원보상표.jpg"},              // F10. 주로 이렇게 POE 정보를 이미지 만들어 사용
            {"keycode":78,"modifiers":2,"value":"{Link}URL{Link}"}, // Ctrl+N. 링크열기 (기본은 닌자로 설정)
            {"keycode":72,"modifiers":2,"value":"{Wiki}"},          // Ctrl+H. 현재 선택된 아이템을 위키로 열기
            {"keycode":0,"modifiers":2,"value":"{Run}"}             // 작동키 Ctrl+C. 변경은 키코드 0을 원하는 키로 바꿈
        ]

옵션 파일 수정 후엔 저장 후 프로그램을 다시 실행해 주셔야 옵션을 새로 읽어 갱신됩니다.<br><br>
참고: 최신 데이터로 자동 업데이트 하려면 FiltersKO.txt 파일 삭제후 실행하면 자동 업데이트합니다.
