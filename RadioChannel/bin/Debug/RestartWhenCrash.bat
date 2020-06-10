@Echo off
:Start
RadioChannel.exe
echo Program terminated at %Date% %Time% with Error %ErrorLevel% >> c:\logs\program.log 
echo Press Ctrl-C if you don't want to restart automatically
ping -n 10 localhost

goto Start