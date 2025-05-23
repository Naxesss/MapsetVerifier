dotnet build src -c Release -r win-x64 -o app/api/win-x64 --self-contained
dotnet build src -c Release -r linux-x64 -o app/api/linux-x64 --self-contained

call npm install
call npm run dist

pause