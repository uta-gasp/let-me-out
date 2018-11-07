@echo off
cd Build
call "C:\Program Files\7-Zip\7z.exe" a let-me-out-hmd.zip *
scp2 -d -m644 let-me-out-hmd.zip csolsp@shell.sis.uta.fi:/home/staff/csolsp/public_html/shared/projects/gasp/letmeout
del let-me-out-hmd.zip
cd ..
pause
