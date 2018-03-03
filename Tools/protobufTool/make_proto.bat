@echo off

set SOURCE_FOLDER=.\

for /f "delims=" %%i in ('dir /b "%SOURCE_FOLDER%\*.proto"') do (
    protoc.exe   --csharp_out=.\C# %%i
)
echo "协议生成完毕"

copy /y .\C#  ..\..\TestClient\ProtoFile   
copy /y .\C#  ..\..\TestServer\ProtoFile   

pause