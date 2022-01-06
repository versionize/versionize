@echo off

git checkout master
git pull || echo Could not pull changes from origin && exit /b

dotnet run --project Versionize --framework net6.0 || echo Exiting. Could not create a new version using versionize && exit /b
rm -rf nupkgs || echo ERROR && exit /b
dotnet pack --output nupkgs --include-source --configuration Release --include-symbols || echo Exiting. Could not run dotnet pack. Remove any tags created by versionize && exit /b

for /f "tokens=*" %%a in ('dotnet run --project Versionize --framework net6.0 inspect') do set _VERSION=%%a

dotnet nuget push nupkgs\Versionize.%_VERSION%.nupkg -s https://api.nuget.org/v3/index.json -k %NUGET_API_KEY%
