@echo off
setlocal EnableDelayedExpansion


del *.paa
for /f %%f in ('dir /b *.paa.png') do (
	set a=%%f
	set b=!a:~0,-8!
	ImageToPAA.exe !b!.paa.png !b!.paa
)
