@echo off
setlocal
echo ============================================================================
echo   OpenTTRPG - Build Nativa para Windows (openttrpg_core.dll + .EXE)
echo ============================================================================

if not exist "bin\release\win-x64" mkdir "bin\release\win-x64"

echo [1/3] Compilando biblioteca nativa C++17 (openttrpg_core.dll)...
g++ -std=c++17 -O2 -shared -DOTTRPG_BUILD_DLL -Iinclude ^
    src/core/ActionEconomy.cpp ^
    src/core/Combatant.cpp ^
    src/core/Condition.cpp ^
    src/core/Damage.cpp ^
    src/core/Dice.cpp ^
    src/core/Encounter.cpp ^
    src/core/InitiativeTracker.cpp ^
    src/core/RequirementTree.cpp ^
    src/core/Resource.cpp ^
    src/api/openttrpg_api.cpp ^
    -o bin\release\win-x64\openttrpg_core.dll -static-libgcc -static-libstdc++

if %ERRORLEVEL% NEQ 0 (
    echo [ERRO] Falha ao compilar openttrpg_core.dll. Verifique se o MinGW-w64 ^(g++^) esta no PATH.
    exit /b 1
)

copy /Y "bin\release\win-x64\openttrpg_core.dll" "godot_ui\open-ttrpg\openttrpg_core.dll" >nul

echo [2/3] Publicando executavel Windows C# CLI (OpenTTRPG.CLI.exe)...
dotnet publish bindings\csharp\OpenTTRPG.CLI.csproj -c Release -r win-x64 --self-contained false -o bin\release\win-x64

echo [3/3] Caso o Godot 4 (.NET) com Export Templates esteja no PATH, exportando OpenTTRPG_UI.exe...
where godot >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    godot --headless --path godot_ui\open-ttrpg --export-release "Windows Desktop" "..\..\bin\release\win-x64\OpenTTRPG_UI.exe"
)

echo.
echo [CONCLUIDO] Arquivos gerados em: bin\release\win-x64\
endlocal
