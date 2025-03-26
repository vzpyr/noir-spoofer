# noir spoofer
perm spoofer written in .net c#

![noir](https://github.com/user-attachments/assets/5a3efbee-27eb-4140-8eb9-4f940509f4d0)

# information
- written as a console app in .net c#
- should work on most games (except VALORANT, as TPM chips aren't spoofable) if you reset your windows to clean old ban traces
- before using this spoofer, please make sure to test it for any bugs so you don't risk accidentally breaking your smbios table

# features
- simple and overall clean ui
- auto-checks if windows defender is disabled
- utilizes [ezkey](https://github.com/vzpyr/ezkey) for licensing and auto-encrypts and saves key
- spoofs smbios, disk drive ids, mac addresses and usb permissions on almost all motherboards (incl. ASUS)
- built-in temp spoofer (runs a cleaner and maps a temporary spoof driver) and tpm hider (using [tpm-spoofer](https://github.com/SamuelTulach/tpm-spoofer)
- built-in serial checker (exports them to a .txt file and opens it in notepad)

# how to use
1. install visual studio 2022 and .net
2. open the solution (.sln)
3. setup an ezkey instance or add a bypass for the key system
4. build the solution
5. run the outputted .exe
