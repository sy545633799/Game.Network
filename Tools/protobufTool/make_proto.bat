@echo off

set SOURCE_FOLDER=.\

for /f "delims=" %%i in ('dir /b "%SOURCE_FOLDER%\*.proto"') do (
    protoc.exe   --csharp_out=.\C# %%i
)
echo "Э���������"

copy /y .\C#  ..\..\TestClient\ProtoFile   
copy /y .\C#  ..\..\TestServer\ProtoFile   

pause