call _clean.bat

:: set /P version=version eg 7.25.1001: 

cd ..\api
docker build -t webgis-api:%1 .
docker tag webgis-api:%1 webgis-api:latest

cd ..\portal
docker build -t webgis-portal:%1 .
docker tag webgis-portal:%1 webgis-portal:latest

cd ..\cms
docker build -t webgis-cms:%1% .
docker tag webgis-cms:%1 webgis-cms:latest