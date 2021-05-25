# For Latest Committed
## Future Features  
#### Entries
- allow proxy mods with multiple proxies ([[m#1]]-[[m#2]])
#### Translation
- Offline dictionary for faster (mouseover?) word lookups
- option to do this automatically: remove translations from cache if not used recently or originally cached too long ago.
- apply translate stage 4 to pre-romaji
#### Filters
- staff filter by name instead of id
- add seiyuu filter to character
- set default filter (different from permanent filter)
#### Test Tab
- show if translation was cached or not and give option to re-fetch
- list entries used per stage.
#### ITHVNR
- use ithvnr tab to also show captured clipboard text format like: \[process/pid] \[outputted] \[text]
- better saving/loading game text threads, let user name threads and delete saved
- once any thread has been saved as posting, change default for new threads to stop/hide.
- text thread panel max height (scrollbar if needed)
- VnrHook: handle maximum threads either change limit or reset ability
- look at global hooks for changing ITHVNR settings via global hotkeys [GlobalMouseKeyHook](https://github.com/gmamaladze/globalmousekeyhook)
#### Output Window
- check if output window is in visible area of screens, if not, reset it.
- alternate colors between blocks
#### User Games
- option to always launch games with some locale if windows locale is not already set to that, and option to override it.
- context option to launch without hooking.
- allow keeping time played for multiple games/allow process monitor to keep running always/make process monitor only have one instance
- launch title from VN context menu
#### Other
- warn if an instance is already running (maybe show database file for opening instance) 
- show written tag description on tooltip
- MainWindow: allow scrolling on main window tab control when too many tabs and add close all tabs button
- Settings: if login response is error with id 'needlogin' show user that credentials are wrong

## Issues  
- can't show outputwindow on top of some fullscreen games, steals focus on games where it works (might need direct draw)
