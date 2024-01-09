# git-lfs-synchronizer
## About

If you've got such exception  

```
Downloading [filePath].fbx (1.5 MB)
Error downloading object: [fileName].fbx (e2d9199): 
Smudge error: error opening media file: open Z:\[repo]\.git\lfs\objects\e2\d9\e2d91991d416e764ee29b7da43f6d94e29d9be1c9e8eadf72b69a9699c832fc7: The system cannot find the file specified.
```  

and `git lfs fetch --all` does not help

and lfs path in .git\.config is correct

This app is workaround for that problem.

## How it works
You execute this app in **server-mode** on machine with lfs server.  
If client have got such problem, he executes this app in **client-mode** and the app requests lfs files list from server, if some of them missing, then app downloads missing files and that's it.

## Requirements 
Download and install [dotnet 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

## Configuration
Config is located in `etc/config.json`  
Example:  
```json
{
  "isServer": false,
  "repos": [
    {
      "url": "http://26.58.143.118:5000",
      "name": "cock",
      "path": "D:\\Repositories\\cock"
    }
  ]
}
```
* set isServer = true if this app will be sending missing files
* you can add multiple repos to sync lfs
* set url with port of server where app is executed
* set **the same name** of repo in client and server app
* set local path to repo (directory must have .git folder), don't forget double '\' if windows
* url is mandatory if isServer = false

## Execution
Configure `etc/config.json` on client and server sides. Then:
On server side run `dotnet run --urls "http://[server address in using network]:[open port]/"` in git-lfs-synchronizer directory  
Example:
```
dotnet run --urls "http://26.58.143.118:5000"
```
On client side run `dotnet run` in git-lfs-synchronizer directory

If everything configured well and there are no problems with network, then the app sync all missing lfs files
