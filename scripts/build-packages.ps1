if ([System.IO.Directory]::Exists('publish')) {
    Remove-Item -Recurse publish
} 
mkdir publish

dotnet publish src/Schedule-Cli.csproj -c Release -r win-x64 -o publish/Schedule-Cli-win-x64 --self-contained true -p:PublishSingleFile=true
dotnet publish src/Schedule-Cli.csproj -c Release -r win-x86 -o publish/Schedule-Cli-win-x86 --self-contained true -p:PublishSingleFile=true
dotnet publish src/Schedule-Cli.csproj -c Release -r linux-x64 -o publish/Schedule-Cli-linux-x64 --self-contained true -p:PublishSingleFile=true
dotnet publish src/Schedule-Cli.csproj -c Release -r osx-x64 -o publish/Schedule-Cli-osx-x64 --self-contained true -p:PublishSingleFile=true

Set-Location publish

Remove-Item Schedule-Cli-win-x64/*.pdb
Remove-Item Schedule-Cli-win-x86/*.pdb
Remove-Item Schedule-Cli-linux-x64/*.pdb
Remove-Item Schedule-Cli-osx-x64/*.pdb

wsl -- tar -czf Schedule-Cli-linux-x64.tgz Schedule-Cli-linux-x64/*
wsl -- tar -czf Schedule-Cli-osx-x64.tgz Schedule-Cli-osx-x64/*
7z a -r -tzip Schedule-Cli-win-x86.zip Schedule-Cli-win-x86/*
7z a -r -tzip Schedule-Cli-win-x64.zip Schedule-Cli-win-x64/*

Set-Location ..
