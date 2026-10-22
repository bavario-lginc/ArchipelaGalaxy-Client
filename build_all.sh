#!/bin/sh
dotnet publish --self-contained --os win
dotnet publish --self-contained --os osx
dotnet publish --self-contained --os linux