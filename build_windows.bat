@echo off
setlocal
echo ============================================================================
echo   OpenTTRPG - Build Nativa para Windows (openttrpg_core.dll + OpenTTRPG.exe)
echo ============================================================================

if not exist "bin\release\win-x64" mkdir "bin\release\win-x64"
if not exist "bin\release\win-x64\saves" mkdir "bin\release\win-x64\saves"
if not exist "bin\release\win-x64\data_OpenTTRPG_windows_x86_64" mkdir "bin\release\win-x64\data_OpenTTRPG_windows_x86_64"

echo [1/4] Compilando biblioteca nativa C++17 (openttrpg_core.dll)...
g++ -std=c++17 -O2 -shared -Iinclude ^
    src/core/ActionEconomy.cpp ^
    src/core/Combatant.cpp ^
    src/core/Condition.cpp ^
    src/core/Damage.cpp ^
    src/core/Dice.cpp ^
    src/core/Encounter.cpp ^
    src/core/InitiativeTracker.cpp ^
    src/core/RequirementTree.cpp ^
    src/core/Resource.cpp ^
    src/core/TacticalMap.cpp ^
    src/api/openttrpg_api.cpp ^
    -o bin\release\win-x64\openttrpg_core.dll -static -static-libgcc -static-libstdc++

if %ERRORLEVEL% NEQ 0 (
    echo [ERRO] Falha ao compilar openttrpg_core.dll. Verifique se o MinGW-w64 ^(g++^) esta no PATH.
    exit /b 1
)

copy /Y "bin\release\win-x64\openttrpg_core.dll" "godot_ui\open-ttrpg\openttrpg_core.dll" >nul
copy /Y "bin\release\win-x64\openttrpg_core.dll" "bin\release\win-x64\data_OpenTTRPG_windows_x86_64\openttrpg_core.dll" >nul
if exist "saves\characters.json" copy /Y "saves\characters.json" "bin\release\win-x64\saves\characters.json" >nul
if exist "saves\enemies.json" copy /Y "saves\enemies.json" "bin\release\win-x64\saves\enemies.json" >nul

echo [2/4] Publicando executavel Windows C# CLI (OpenTTRPG.CLI.exe)...
dotnet publish bindings\csharp\OpenTTRPG.CLI.csproj -c Release -r win-x64 --self-contained false -o bin\release\win-x64

echo [3/4] Exportando Interface Grafica Godot 4 (.NET) para OpenTTRPG.exe...
where godot >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    godot --headless --path godot_ui\open-ttrpg --export-release "Windows Desktop" "..\..\bin\release\win-x64\OpenTTRPG.exe"
    copy /Y "bin\release\win-x64\openttrpg_core.dll" "bin\release\win-x64\data_OpenTTRPG_windows_x86_64\openttrpg_core.dll" >nul
) else (
    where godot-mono >nul 2>&1
    if %ERRORLEVEL% EQU 0 (
        godot-mono --headless --path godot_ui\open-ttrpg --export-release "Windows Desktop" "..\..\bin\release\win-x64\OpenTTRPG.exe"
        copy /Y "bin\release\win-x64\openttrpg_core.dll" "bin\release\win-x64\data_OpenTTRPG_windows_x86_64\openttrpg_core.dll" >nul
    )
)

echo [4/4] Executando auto-teste de integracao (Godot UI ^<--^> C# ^<--^> C++ Core)...
if exist "bin\release\win-x64\OpenTTRPG.console.exe" (
    "bin\release\win-x64\OpenTTRPG.console.exe" --headless -- --self-test
)

echo.
echo [CONCLUIDO] Release Windows pronta em: bin\release\win-x64\OpenTTRPG.exe
endlocal
