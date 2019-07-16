rem delete existing
rmdir "ZipPackage" /Q /S

rem Create required folders
mkdir "ZipPackage"

set "CONFIGURATION=Release\netstandard1.1"

rem Copy output files
copy "src\StbImageSharp\bin\%CONFIGURATION%\StbImageSharp.dll" "ZipPackage" /Y
copy "src\StbImageSharp\bin\%CONFIGURATION%\StbImageSharp.pdb" "ZipPackage" /Y