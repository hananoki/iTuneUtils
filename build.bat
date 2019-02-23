rem "C:\Program Files (x86)\MSBuild\12.0\bin\MSBuild.exe" /p:Configuration=Release Project/iTunesUtility.csproj

copy Project\bin\Release\iTunesUtility.exe .\Build
copy Project\bin\Release\*.dll .\Build

pause
