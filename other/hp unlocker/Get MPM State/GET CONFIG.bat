@echo off
cd /d "%~dp0"
BiosConfigUtility64.exe /get:config.txt
config.txt