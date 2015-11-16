@rem Edit paths below to match your setup
@rem
@SET VCVARS32PATH="C:\Program Files (x86)\Microsoft Visual Studio 14.0\VC\bin\vcvars32.bat"
@SET VCVARS64PATH="C:\Program Files (x86)\Microsoft Visual Studio 14.0\VC\bin\x86_amd64\vcvarsx86_amd64.bat"
@SET PHPEXEPATH="f:\PortableDevTools\php-5.3.5-nts-Win32-VC9-x86\php.exe"
@rem

@cd generator
@CALL wrapper-src-generate.bat
@cd ..
@CALL build-win.bat
@CALL xmpir-for-csharp.bat
@copy .\wrapper\xmpir32.dll ..\src\Mpir.NET
@copy .\wrapper\xmpir64.dll ..\src\Mpir.NET
@copy .\wrapper\xmpir.cs ..\src\Mpir.NET
