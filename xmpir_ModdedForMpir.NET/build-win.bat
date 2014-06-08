@rem -----------------------------------------------
@rem -- Build Native DLLs.                        --
@rem -- Called from gen-and-build-win.bat         --
@rem -- Wont' work on it's own, because it        --
@rem -- needs paths set in gen-and-build-win.bat  --
@rem -----------------------------------------------

@SET PATH_SAVED=%PATH%
@SET INCLUDE_SAVED=%INCLUDE%
@SET LIB_SAVED=%LIB%
@SET LIBPATH_SAVED=%LIBPATH%

@md _tmp

@rem --------------------------------------------
@rem -- Build 32-bit DLL                       --
@rem --------------------------------------------

@CD wrapper
@del /Q xmpir32.dll
@CD ..

@CALL %VCVARS32PATH%

@copy .\mpir-precompiled\win32\* _tmp > NUL
@copy .\src\xmpir.c _tmp > NUL
@CD _tmp
@cl /LD /Fexmpir32.dll /DXMPIR_FOR_WINDOWS /I. xmpir.c mpir.lib
@copy xmpir32.dll ..\wrapper > NUL
@del /Q *
@cd ..

@SET PATH=%PATH_SAVED%
@SET INCLUDE=%INCLUDE_SAVED%
@SET LIB=%LIB_SAVED%
@SET LIBPATH=%LIBPATH_SAVED%


@rem --------------------------------------------
@rem -- Build 64-bit DLL                       --
@rem --------------------------------------------

@CD wrapper
@del /Q xmpir64.dll
@CD ..

@CALL %VCVARS64PATH%

@copy .\mpir-precompiled\win64\* _tmp > NUL
@copy .\src\xmpir.c _tmp > NUL
@CD _tmp 
@cl /LD /Fexmpir64.dll /DXMPIR_FOR_WINDOWS /I. xmpir.c mpir.lib
@copy xmpir64.dll ..\wrapper > NUL
@del /Q *
@cd ..

@SET PATH=%PATH_SAVED%
@SET INCLUDE=%INCLUDE_SAVED%
@SET LIB=%LIB_SAVED%
@SET LIBPATH=%LIBPATH_SAVED%
