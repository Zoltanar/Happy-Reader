# For 2.6.0
## Future Features  
#### Translation
- Make mouseover dictionary tooltip nicer
- apply translate stage 4 to pre-romaji
- allow multiple translators at the same time
- Romaji: Create own romaji engine using JMDict **(deinflections in progress)**.
#### Filters
- set default filter (different from permanent filter)
#### Test Tab
- list entries used per stage.
#### Text Hooking
- let user name threads
- VnrHook: handle maximum threads either change limit or reset ability
- look at global hooks for changing ITHVNR settings via global hotkeys [GlobalMouseKeyHook](https://github.com/gmamaladze/globalmousekeyhook)
#### Output Window
- alternate colors between blocks
#### User Games
- see details should change to correct user game tab when multiple user games for one vn
- allow process monitor to keep running always/make process monitor only have one instance- 
- Option to stop monitor
##### Entries
- Implement priority
- Change Series-specific to dropdown (none,title,series, producer?)
- Add text search
##### DatabaseDumpReader
- Parallelise?
- Run RSync while processing database dump, after downloading files
#### Other
- **Add screenshots to basic guide** (In progress, text hooking done)
- Producer Tab: charts for popularity
- Database: Sort By and show english release date
- Database: History item is duplicated when going back
- add 'update available' notice (can hide)
- Settings: if login response is error with id 'needlogin' show user that credentials are wrong
- Database Tab: Sort by Random
- style scrollbars

## Issues  
- can't show Output Window on top of some fullscreen games, steals focus on games where it works (might need direct draw)
