echo off

cd .\..\src\NetCore\Web\Cms
dotnet build -c Release -p:Configuration=Release -p:DeployOnBuild=true -p:PublishProfile=OpenSourceProfile

if errorlevel 1 goto error

cd .\..\Api
dotnet build -c Release -p:Configuration=Release -p:DeployOnBuild=true -p:PublishProfile=OpenSourceProfile

if errorlevel 1 goto error

cd .\..\Portal
dotnet build -c Release -p:Configuration=Release -p:DeployOnBuild=true -p:PublishProfile=OpenSourceProfile

if errorlevel 1 goto error

echo ==================
echo Publish Successful
echo ==================

goto end

:error
echo *****************
echo An error occurred
echo *****************

pause

:end