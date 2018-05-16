# BlennyBackup
Sync two folders

Run (See details at BlennyBackup/Options/)
```
BlennyBackup.exe direct -d --source "D:/PathToSourceFolder/" --target "E:/PathToTargetFolder/" --pattern "*" --log "F:/PathToLogFile.txt" --report 100 --flush_delay 1000
```
Or (Example xml file at ConfigFileExample/config.xml, more details at BlennyBackup/Configuration)
```
BlennyBackup.exe xml -d --path "D:/PathToXMLFile.xml" --log "F:/PathToLogFile --report 100 --flush_delay 1000
```
![Console output](https://user-images.githubusercontent.com/9568412/40142412-d06efbec-5958-11e8-968a-7a4e4bf8bb55.png)
![Log File](https://user-images.githubusercontent.com/9568412/40142459-ee77b5de-5958-11e8-86aa-15b866c32135.png)
