# Script to publish JwtApi to Cloud VM

Write-Output "Starting publish script."
cd C:\Users\joakim\source\repos\jdahl91\JwtApi\bin\Release\net8.0\publish

scp -r * xyz@xxx.xxx.xxx.xxx:/home/xyz/JwtApi

Write-Output "App successfully published to remote server."
