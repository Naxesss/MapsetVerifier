dotnet build src -c Release -r win-x86 -o app/api/win-x86 --self-contained
dotnet build src -c Release -r linux-x64 -o app/api/linux-x64 --self-contained

npm run dist