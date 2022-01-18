# qubic-miner-helper

### Helps to easy run multiple Qiner worker instances with same command line params.
#### Its now compatible with Qiner 0.4.4 and has auto restart on detected inactivity and on crashes / worker process close. 

Download latest version: https://github.com/ashupp/qubic-miner-helper/releases/latest  
Helper Server project: https://github.com/ashupp/qubic-miner-helper-server 

**Set your miner executable and Insert your ID, desired thread count and seconds until update into command line box.**   
**Then click on save command line**

## Version 1.1.2.0
- Improved UI by making text boxes readonly instead of disabled. Looks better and you can copy the values
- Added display of temperatures and load (needs coretemp running to show values: https://www.alcpu.com/CoreTemp/)
- Temperatures, load, time of last error reduction and overall restart times are transferred to server
- Added icon
- Removed unneccesary ressources

## Version 1.1.1.0
- Extended data is being sent to server. 
- Current rank, current pool errors left, current helper version

## Version 1.1.0.0
- You can now connect to the helper server if you have multiple mining machines to see all stats of your miners at once
- Get Helper Server here: https://github.com/ashupp/qubic-miner-helper-server 

### Features
- You can start multiple workers with desired Qiner sub threads. (sounds confusing but check screenshot)
- Restarts specific threads automatically if thread did not respond within 1 minute
- Restarts specific threads automatically if they crash/exit
- Additional info: Last time Error found with thread and reduced by how much
- Overall Iterations/s display
- Overall Errors found since program start display
- Take care - if the tool crashes you might have zombie Qiner threads which you have to stop manually
- No Virus, no bullshit but no support also 🙂 

![image](https://user-images.githubusercontent.com/1867828/149675551-b58862a2-5fc0-4dff-a91e-d70fba9fbcb5.png)


