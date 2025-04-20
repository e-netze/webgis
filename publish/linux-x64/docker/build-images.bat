call _clean.bat

set /P version=version eg 7.25.1001: 

cd ..\api
docker build -t webgis-api:%version% .
docker tag webgis-api:%version% webgis-api:latest

cd ..\portal
docker build -t webgis-portal:%version% .
docker tag webgis-portal:%version% webgis-portal:latest

cd ..\cms
docker build -t webgis-cms:%version% .
docker tag webgis-cms:%version% webgis-cms:latest