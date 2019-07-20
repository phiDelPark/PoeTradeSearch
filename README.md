[![release](https://img.shields.io/badge/release-Download-brightgreen.svg)](https://github.com/phiDelPark/PoeTradeSearch/releases)
[![License](https://img.shields.io/badge/license-GPL-blue.svg)](https://github.com/phiDelPark/PoeTradeSearch/blob/master/LICENSE)

한글 POE 거래소 검색
-------------

선택한 아이템을 POE 거래소에서 검색합니다.<br>
한글 거래소와 영문 거래소 두 서버 모두 검색이 가능하며 한쪽의 서버 접속이 안될때 골라 사용 하시면 됩니다.

**한글 POE 전용**이고 영문 POE 는 trade macro 가 있어서 안만들었습니다.

### 개발환경: 
* 한글 윈도우 10, 한글 POE 전체 창 모드<br>
* 윈7 은 프레임워크 버전이 낮아 [4.0 업데이트↗](https://www.microsoft.com/ko-kr/download/details.aspx?id=17851) 필수 (윈10 은 이미 최신이니 받지마세요)

### 사용: 
* 인게임 아이템 위에서 Ctrl+C 하면 검색창이 뜹니다.
### 종료: 
* 트레이 아이콘을 우클릭 하시면 됩니다.

### 참고:
 1. 브라우저는 윈도우 기본 브라우저를 사용하니 변경은 윈도우 기본 브라우저를 바꾸시면 됩니다.
 2. 작동키 Ctrl+C 는 그냥 실행해도 가능하지만 창고휠 이동과 단축키들은 관리자 권한으로 실행해야 합니다.<br>
    (기본 단축키 = F2 은신처, F4 나가기, F5 남은몬스터, F9 정보보기, F11 일시중지, ESC 창닫기, Ctrl+W 닌자열기)

### 옵션 파일 ( Config.txt ) 설명 

      "options":{
        "league":"Standard",    // 현재 리그 ["Legion", "Hardcore Legion", "Standard", "Hardcore"]
        "server":"kr",             // 기본 검색 서버 ["kr", "en"]
        "week_before":1,        // 1~2 주일 전 물품만 시세 조회 [1, 2]
        "by_type":true,          // 기본 검색시 아이템 유형으로 검색 [true, false]
        "ctrl_wheel":true       // 창고를 Ctrl+Wheel 로 이동 가능하게 할지 [true, false]
      },
       "shortcuts":[  
            {"keycode":113,"value":"{Enter}/hideout{Enter}"},    // F2. 은신처 ("{Enter}채팅명령어{Enter}")
            {"keycode":115,"value":"{Enter}/exit{Enter}"},         // F4. 나가기
            {"keycode":116,"value":"{Enter}/remaining{Enter}"}, // F5. 남은 몬스터
            {"keycode":120,"value":"사원보상표.jpg"},            // F9. 데이터 폴더의 이미지 출력 (단, .jpg 만 가능)
            {"keycode":121,"value":"신디보상표.jpg"},            // 주로 이렇게 POE 정보를 이미지로 만들어 사용
            {"keycode":122,"value":"{Pause}"},                     // F11. 값이 "{Pause}"면 일시 중지 키로 사용됨
            {"keycode":27,"value":"{Close}"},                       // ESC. 값이 "{Close}"면 창 닫기 키로 사용됨
            {"keycode":87,"ctrl":true,"value":"{Link}https//poe.ninja/{Link}" // Ctrl+W. 링크 열기 ("{Link}URL{Link}")
            {"keycode":0,"ctrl":true,"value":"{Run}"}               // 작동키 변경, 값이 "{Run}"면 Ctrl+설정키로 작동
        ],
      "checked":[  ]            // 기본적으로 자동 선택될 옵션들

키코드(keycode) 찾는법은 이 사이트 (http://cherrytree.at/misc/vk.htm) 로 가시면 바로 찾을 수 있습니다.<br>
작동키는 기본 Ctrl+C 로 작동하고 변경을 원하시면 원하는 키코드로 바꿔주세요. (작동키는 Ctrl + 설정키)<br>
옵션 파일에서 설정하는 추가 단축키 기능과 창고휠 이동은 관리자 권한으로 실행해야 작동합니다.<br>
옵션 파일 수정 후엔 저장 후 프로그램을 다시 실행해 주셔야 새로 읽어 갱신됩니다.
