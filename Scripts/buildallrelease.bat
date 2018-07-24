dotnet publish C:\git\flecoqui\AMSReverseProxy\cs\AMSReverseProxy\AMSReverseProxy --self-contained -c Release -r win-x64
"C:\Program Files\7-Zip\7z.exe" a c:\temp\LatestRelease.win.zip C:\git\flecoqui\AMSReverseProxy\cs\AMSReverseProxy\AMSReverseProxy\bin\Release\netcoreapp2.0\win-x64\publish\
dotnet publish C:\git\flecoqui\AMSReverseProxy\cs\AMSReverseProxy\AMSReverseProxy  --self-contained -c Release -r centos-x64
"C:\Program Files\7-Zip\7z.exe" a c:\temp\LatestRelease.centos.tar C:\git\flecoqui\AMSReverseProxy\cs\AMSReverseProxy\AMSReverseProxy\bin\Release\netcoreapp2.0\centos-x64\publish\
"C:\Program Files\7-Zip\7z.exe" a c:\temp\LatestRelease.centos.tar.gz c:\temp\LatestRelease.centos.tar 
dotnet publish C:\git\flecoqui\AMSReverseProxy\cs\AMSReverseProxy\AMSReverseProxy  --self-contained -c Release -r rhel-x64
"C:\Program Files\7-Zip\7z.exe" a c:\temp\LatestRelease.rhel.tar C:\git\flecoqui\AMSReverseProxy\cs\AMSReverseProxy\AMSReverseProxy\bin\Release\netcoreapp2.0\rhel-x64\publish\
"C:\Program Files\7-Zip\7z.exe" a c:\temp\LatestRelease.rhel.tar.gz c:\temp\LatestRelease.rhel.tar 
dotnet publish C:\git\flecoqui\AMSReverseProxy\cs\AMSReverseProxy\AMSReverseProxy  --self-contained -c Release -r ubuntu-x64
"C:\Program Files\7-Zip\7z.exe" a c:\temp\LatestRelease.ubuntu.tar C:\git\flecoqui\AMSReverseProxy\cs\AMSReverseProxy\AMSReverseProxy\bin\Release\netcoreapp2.0\ubuntu-x64\publish\
"C:\Program Files\7-Zip\7z.exe" a c:\temp\LatestRelease.ubuntu.tar.gz c:\temp\LatestRelease.ubuntu.tar 
dotnet publish C:\git\flecoqui\AMSReverseProxy\cs\AMSReverseProxy\AMSReverseProxy  --self-contained -c Release -r debian-x64
"C:\Program Files\7-Zip\7z.exe" a c:\temp\LatestRelease.debian.tar C:\git\flecoqui\AMSReverseProxy\cs\AMSReverseProxy\AMSReverseProxy\bin\Release\netcoreapp2.0\debian-x64\publish\
"C:\Program Files\7-Zip\7z.exe" a c:\temp\LatestRelease.debian.tar.gz c:\temp\LatestRelease.debian.tar 
dotnet publish C:\git\flecoqui\AMSReverseProxy\cs\AMSReverseProxy\AMSReverseProxy  --self-contained -c Release -r osx-x64
"C:\Program Files\7-Zip\7z.exe" a c:\temp\LatestRelease.osx.tar C:\git\flecoqui\AMSReverseProxy\cs\AMSReverseProxy\AMSReverseProxy\bin\Release\netcoreapp2.0\osx-x64\publish\
"C:\Program Files\7-Zip\7z.exe" a c:\temp\LatestRelease.osx.tar.gz c:\temp\LatestRelease.osx.tar 
copy /Y c:\temp\LatestRelease.win.zip C:\git\flecoqui\AMSReverseProxy\Releases\LatestRelease.win.zip
copy /Y c:\temp\LatestRelease.centos.tar.gz C:\git\flecoqui\AMSReverseProxy\Releases\LatestRelease.centos.tar.gz
copy /Y c:\temp\LatestRelease.rhel.tar.gz C:\git\flecoqui\AMSReverseProxy\Releases\LatestRelease.rhel.tar.gz
copy /Y c:\temp\LatestRelease.ubuntu.tar.gz C:\git\flecoqui\AMSReverseProxy\Releases\LatestRelease.ubuntu.tar.gz
copy /Y c:\temp\LatestRelease.debian.tar.gz C:\git\flecoqui\AMSReverseProxy\Releases\LatestRelease.debian.tar.gz
copy /Y c:\temp\LatestRelease.osx.tar.gz C:\git\flecoqui\AMSReverseProxy\Releases\LatestRelease.osx.tar.gz




