@ECHO OFF

pyinstaller --onefile app.py
COPY dist\app.exe .
COPY dist\app.exe ..\..\Go\protocol\app.exe
ROBOCOPY templates ..\..\Go\protocol\templates *.txt /E /njh /njs /ndl /nc /ns
PAUSE