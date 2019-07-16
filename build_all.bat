dotnet --version
dotnet build build\StbImageSharp.sln /p:Configuration=Release --no-incremental
call copy_zip_package_files.bat
rename "ZipPackage" "StbImageSharp.%APPVEYOR_BUILD_VERSION%"
7z a StbImageSharp.%APPVEYOR_BUILD_VERSION%.zip StbImageSharp.%APPVEYOR_BUILD_VERSION%
