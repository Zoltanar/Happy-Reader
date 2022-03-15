# For Latest Committed
## Future Features  
#### Translation
- Make mouseover dictionary tooltip nicer
- **export/import translation cache**
- option to do this automatically: remove translations from cache if not used recently or originally cached too long ago.
- apply translate stage 4 to pre-romaji
- allow multiple translators at the same time
- Romaji: Create own romaji engine using JMDict (missing deinflections).
#### Filters
- set default filter (different from permanent filter)
- handle mtl flag for release languages
#### Test Tab
- show if translation was cached or not and give option to re-fetch
- list entries used per stage.
#### Text Hooking
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
- Change Series-specific to three-way dropdown (none,title,series, producer?)
##### DatabaseDumpReader
- DatabaseDumpReader: Parallelise?
- DatabaseDumpReader: Run RSync while processing database dump, after downloading files
#### Other
- Option to stop monitor
- **Add screenshots to basic guide**
- Producer Tab: charts for popularity
- Database: Sort By and show english release date
- Database: History item is duplicated when going back
- add 'update available' notice (can hide)
- warn if an instance is already running (maybe show database file for opening instance) 
- Main Window: add close all tabs button
- Settings: if login response is error with id 'needlogin' show user that credentials are wrong
- Database Tab: Handle broken connections to vndb (catch exception and/or allow reestablishment)
- Logs: Link to folder, show size taken, option to clear.
- Database Tab: Sort by Random
- style scrollbars

## Issues  
- can't show Output Window on top of some fullscreen games, steals focus on games where it works (might need direct draw)
