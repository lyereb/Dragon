set WORKSPACE=.
set GEN_CLIENT=%WORKSPACE%\Luban\Luban.dll
set CONF_ROOT=%WORKSPACE%\Datas

dotnet %GEN_CLIENT% ^
    -t client ^
    -c cs-simple-json ^
    -d json ^
    --conf %WORKSPACE%\luban.conf ^
    -x outputCodeDir=%WORKSPACE%\..\Client\Main\Assets\GameMain\GameConfig ^
    -x outputDataDir=%WORKSPACE%\..\Client\Main\Assets\AddressableAssets\Configs

pause