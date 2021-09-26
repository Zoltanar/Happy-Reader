# For Latest Committed
## Future Features  
#### Translation
- Yandex API Translation
- Make mouseover dictionary tooltip nicer
- export/import translation cache
- option to do this automatically: remove translations from cache if not used recently or originally cached too long ago.
- apply translate stage 4 to pre-romaji
#### Filters
- set default filter (different from permanent filter)
- handle mtl flag for release languages
#### Test Tab
- show if translation was cached or not and give option to re-fetch
- list entries used per stage.
#### Text Hooking
- use tab to also show captured clipboard text format like: \[process/pid] \[outputted] \[text]
- let user name threads and delete saved
- VnrHook: handle maximum threads either change limit or reset ability
- look at global hooks for changing ITHVNR settings via global hotkeys [GlobalMouseKeyHook](https://github.com/gmamaladze/globalmousekeyhook)
#### Output Window
- alternate colors between blocks
#### User Games
- see details should change to correct user game tab when multiple user games for one vn
- allow keeping time played for multiple games/allow process monitor to keep running always/make process monitor only have one instance
##### Entries
- Implement priority
- Change Series-specific to three-way dropdown (none,title,series)
#### Other
- Producer Tab: charts for popularity
- Producer Tab: list staff and number of contributions
- tray: update last played titles in real time
- **vn: display languages other than original**
- add 'update available' notice (can hide)
- warn if an instance is already running (maybe show database file for opening instance) 
- MainWindow: add close all tabs button
- Settings: if login response is error with id 'needlogin' show user that credentials are wrong
- DatabaseDumpReader: Parallelise?
- Logs: Link to folder, show size taken, option to clear.
- Information Tab: Show Size of Vndb Images, show total size.
- Use Screenshot Thumbnails when full-size is not found.
- show random titles in database tab

## Issues  
- can't show outputwindow on top of some fullscreen games, steals focus on games where it works (might need direct draw)
