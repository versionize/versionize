@echo off

dotnet run --project Versionize --framework net5.0
rm -rf nupkgs
dotnet pack --output nupkgs --include-source --configuration Release --include-symbols
