# WMIWatcher

## Purpose 

Have you ever wondered why svchost.exe and WmiPrvSE.exe are consuming for a long time CPU on your system?
![](Docs/Pics/WMICpuActivity.png)

This is caused by WMI queries executed by system management, security, monitoring or just slow software installed by your system administrator.
WMI is the secret weapon of administrators to sniff on your PC at without asking you. Usual queries are

- Which software do you have installed?
- Is the virus scanner up to date?
- Is Bitlocker enabled? 
- Is the Virus Scanner enabled?
- Which processes are running?
- ....

If you suspect something strange is happening on your machine, chances are good that some software is executing expensive WMI queries. This also can include
malicious software which also uses WMI quite a lot to e.g. start processes in a stealthy way. 
Until today it was very hard to find out which query was executed by which process.
There is WMI Activity Tracing built in into Windows ([some example events](https://www.darkoperator.com/blog/2017/10/14/basics-of-tracking-wmi-activity)) which is very complex 
(see [Bruce Dawson](https://randomascii.wordpress.com/2017/09/05/hey-synaptics-can-you-please-stop-polling/)) to use. 


## What Do You Get?

WMIWatcher will give you a live view of all executed WMI Queries on your system and the ability to log all queries (including the boot phase where many bad things happen).
The log file a CSV file which you can open with Excel and analyze further to understand which processes execute which queries. 

![](Docs/Pics/Console.png)

The console is good to get an overview what is currently running and to test if WMIWatcher is working. But normally you want to analyze the data
with Excel. Besides the exe there is WmiWatcher.csv which has | as column separator. When you open it it Excel will automatically split
the CSV file. To view Time as time you need to enter hh:mm:ss.000 as custom time format and you are ready to analyze your machine.
![](Docs/Pics/CSVFile.png)

All WMI Activity and initiating processes are no secret anymore. Now you can blame the right processes for their expensive WMI queries. E.g. lets check what Blizzards Battle.net is doing
to my machine. I play sometimes StarCraft II (I am getting old) which requires these game launchers. Where are the old times where you could install and run a game without any Internet connection?

After doing a little pivot table stuff I get this:
![](Docs/Pics/Battlenet.png)

When profiling I quickly find on my machine the recurring spikes
with a 4s interval
![](Docs/Pics/Battlenet_WPAView.png)

WMI is all about executing work on behalf of someone else. This can lead even good application developers down the wrong path by believing that if their application 
does not consume CPU that they did everything correctly. Most often the delegated work to WMI is not taken into account leading to big CPU consumption in some cases
which cannot be attributed to the origin.

The most dangerous queries besides slow ones are Polling Queries which are really bad because you can set the poll interval up to 1s which happens far too often. 
To find these ones either search for queries with WITHIN in the query string or filter the Operation field to NotificationQuery which 
will show you every new polling query. 
When the polling timeout has elapsed the Operation column will contain an ExecAsync call without a preceding Exec call. A synchronous WMI query is showing up first as Exec and then one more time 
as ExecAsync. Missing Exec calls for a query is also an indication of a polling WMI query.

## Running Time

WMIWatcher is a .NET Core 3 self contained application. Unzip it to a folder, run 
```
    C:>WMIWatcher.exe install
```
to install WMIWatcher as Windows system service which is automatically started. 
Besides the exe the WmiWatcher.csv file will be created
Alternatively you can run it also directly 
