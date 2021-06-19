@echo off
SET EXE=%~dp0..\PoeTradeSearch.exe
SET DAT=%~dp0update.dat
SET CMD=%~dp0update.cmd
SET COUNT=0
:Repeat 
del /f /s /q "%EXE%"  >nul 2>&1
cls
SET /a COUNT+=1
echo.
echo. 업데이트 대기중... (%COUNT%)
timeout 1  >nul 2>&1
if exist "%EXE%" goto Repeat
SET COUNT=0
move "%DAT%" "%EXE%"
:Repeat2
SET /a COUNT+=1
echo.
echo. 업데이트를 완료 하는중... (%COUNT%)
timeout 1  >nul 2>&1
if exist "%DAT%" goto Repeat2
del /s /q %~dp0Parser.txt >nul 2>&1
timeout 1  >nul 2>&1
del /s /q %~dp0FiltersKO.txt >nul 2>&1
timeout 1  >nul 2>&1
cls
echo.
echo. 업데이트를 마쳤습니다.
echo. 프로그램을 다시 실행해주세요.
echo.
echo. 참고: 업데이트 후 실행시 오류가 발생한다면...
echo. 데이터 폴더를 삭제 후 프로그램을 다시 실행해주세요.
echo. 단, 설정 Config.txt 파일은 필요하면 백업해 두세요.
echo.
echo. 아무키나 누르면 이 창을 닫습니다.
del /s /q "%CMD%" >nul 2>&1
pause >nul
del /s /q "%CMD%" >nul 2>&1