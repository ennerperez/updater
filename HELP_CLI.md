# Application Update Engine
--------------------------------------
 
 Updater [/F] /E:source [/L[[:logfile/logext]]] [/X:[.]tmp] [/T:path]
 
- /? : Display help
- /F : Replace existing files
- /L : Log operations, [log file or log extension]
- /E : Update engine
- /X : Cache file extension
- /T : Target directory
 
## GitHub (Engine)
 
 Updater /E:github /R:{username}/{repo}
 Updater /E:github /U:{username} /R:{repo}
 
- /U : User name
- /R : Repository path

## NuGet (Engine)
 
 Updater /E:nuget /P:{package}[/{version}]
 Updater /E:nuget /P:{package} /V:{version}
 
- /P : Package ID
- /V : Version
 
## Examples
 
 Updater /F /E:github /R:yck1509/ConfuserEx /L /X:tmp
 Updater /F /E:nuget /P:entityframework/6.1.2 /L /X:tmp
 