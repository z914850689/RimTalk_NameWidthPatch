@echo off
chcp 936 >nul
echo 编译 RimTalk_NameWidthPatch ...

set RimWorldDir=E:\SteamLibrary\steamapps\common\RimWorld
set RimTalkDir=E:\SteamLibrary\steamapps\workshop\content\294100\3551203752

cd /d "%~dp0Source\RimTalk_NameWidthPatch"

echo 还原NuGet包...
call dotnet restore

echo 编译中...
call dotnet build -c Release

if exist "bin\Release\net48\RimTalk_NameWidthPatch.dll" (
    mkdir "%~dp01.5\Assemblies" 2>nul
    mkdir "%~dp01.6\Assemblies" 2>nul
    copy /Y "bin\Release\net48\RimTalk_NameWidthPatch.dll" "%~dp01.5\Assemblies\"
    copy /Y "bin\Release\net48\RimTalk_NameWidthPatch.dll" "%~dp01.6\Assemblies\"
    echo 编译成功！
) else (
    echo 编译失败，请检查错误信息。
)

pause
