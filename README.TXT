1. FoldersSynchro:
Program synchronizes two folders: SOURCE and REPLICA. 
It maintain a full, identical copy of SOURCE folder at REPLICA folder.

2. Description:
This is one-way sinchronization: after the synchronization content of the REPLICA folder is modified to exactly match content of the SOURCE folder.
Synchronization is performed periodically.
File operations are logged to a LOG_FILE.txt and to the console output.
Folder paths, synchronization interval and log file path should be provided using the command line arguments.

3. Example usage:
FoldersSynchro.exe -s [PathToSource] -r [PathToReplica] -l [PathToLogFile] -i 10

