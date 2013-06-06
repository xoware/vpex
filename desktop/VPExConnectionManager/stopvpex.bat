REM Check Windows Version
ver | findstr /i "5\.0\." > nul
IF %ERRORLEVEL% EQU 0 goto ver_2000
ver | findstr /i "5\.1\." > nul
IF %ERRORLEVEL% EQU 0 goto ver_XP
ver | findstr /i "5\.2\." > nul
IF %ERRORLEVEL% EQU 0 goto ver_2003
ver | findstr /i "6\.0\." > nul
IF %ERRORLEVEL% EQU 0 goto ver_Vista
ver | findstr /i "6\.1\." > nul
IF %ERRORLEVEL% EQU 0 goto ver_Win7
goto warn_and_exit

:ver_Win7
:Run Windows 7 specific commands here
REM echo OS Version: Windows 7 (debug line)
TASKKILL /F /IM OPENVPN.EXE
goto end

:ver_Vista
:Run Windows Vista specific commands here
REM echo OS Version: Windows Vista (debug line)
TASKKILL /F /IM OPENVPN.EXE
goto end

:ver_2003
:Run Windows Server 2003 specific commands here
REM echo OS Version: Windows Server 2003 (debug line)
TASKKILL /F /IM OPENVPN.EXE
goto end

:ver_XP
:Run Windows XP specific commands here
REM echo OS Version: Windows XP (debug line)
TSKILL OPENVPN
goto end

:ver_2000
:Run Windows 2000 specific commands here
REM echo OS Version: Windows 2000 (debug line)
TSKILL OPENVPN
goto end

:warn_and_exit
echo Machine OS cannot be determined.

:end  
