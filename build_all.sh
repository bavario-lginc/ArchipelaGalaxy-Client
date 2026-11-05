#!/bin/sh
dotnet publish -c Release --self-contained --os win -p:PublishSingleFile=true
dotnet publish -c Release --self-contained --os osx -p:PublishSingleFile=true
dotnet publish -c Release --self-contained --os linux -p:PublishSingleFile=true