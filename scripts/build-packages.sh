mkdir publish

dotnet publish src/Schedule-Cli.csproj -c Release -r win-x64 -o publish/Schedule-Cli-win-x64 --self-contained true -p:PublishSingleFile=true
dotnet publish src/Schedule-Cli.csproj -c Release -r win-x86 -o publish/Schedule-Cli-win-x86 --self-contained true -p:PublishSingleFile=true
dotnet publish src/Schedule-Cli.csproj -c Release -r linux-x64 -o publish/Schedule-Cli-linux-x64 --self-contained true -p:PublishSingleFile=true
dotnet publish src/Schedule-Cli.csproj -c Release -r osx-x64 -o publish/Schedule-Cli-osx-x64 --self-contained true -p:PublishSingleFile=true

cd publish

rm Schedule-Cli-win-x64/*.pdb
rm Schedule-Cli-win-x86/*.pdb
rm Schedule-Cli-linux-x64/*.pdb
rm Schedule-Cli-osx-x64/*.pdb

chmod +x Schedule-Cli-linux-x64/HRSchedule
chmod +x Schedule-Cli-osx-x64/HRSchedule

tar -czf Schedule-Cli-linux-x64.tgz Schedule-Cli-linux-x64/*
tar -czf Schedule-Cli-osx-x64.tgz Schedule-Cli-osx-x64/*
zip Schedule-Cli-win-x86.zip Schedule-Cli-win-x86/*
zip Schedule-Cli-win-x64.zip Schedule-Cli-win-x64/*

cd ..
