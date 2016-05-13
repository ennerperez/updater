## NuGet (Engine)
 
Updater /E:nuget /P:{package}[/{version}] [/D:lib\net40]
Updater /E:nuget /P:{package} /V:{version} [/D:lib\net40]
 
- /P : Package ID
- /V : Version
- /D : Directories
 
## Examples
 
Updater /F /E:nuget /P:entityframework/6.1.2 /L /X:tmp /T:lib\net40