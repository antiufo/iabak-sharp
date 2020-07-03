# IaBak-sharp

A C# implementation for the [INTERNETARCHIVE.BAK](https://www.archiveteam.org/index.php?title=INTERNETARCHIVE.BAK) project.

![iabak-sharp screenshot](https://raw.githubusercontent.com/antiufo/iabak-sharp/master/images/iabak-sharp.png)

## Download

* [Download iabak-sharp (Windows x64)](https://github.com/antiufo/iabak-sharp/releases/download/0.1.9/iabak-sharp-v0.1.9-windows-x64.zip)
* [Download iabak-sharp (Linux x64)](https://github.com/antiufo/iabak-sharp/releases/download/0.1.9/iabak-sharp-v0.1.9-linux-x64.zip)

## Installation instructions
### Windows
* Extract the [zip](https://github.com/antiufo/iabak-sharp/releases/) to some folder.
* Launch the extracted application.
* If Windows warns you about the unsigned binary, you can click `More info` -> `Run anyway`.

### Linux
* Extract the [zip](https://github.com/antiufo/iabak-sharp/releases/) to some folder.
* Add the executable flag (`chmod 770 iabak-sharp`)
* Run the application: `./iabak-sharp`

On the first run, you will be asked:
* Email address _(optional, might be useful in the event a restore becomes necessary)_
* Nickname _(optional, used to populate leaderboards, although not implemented yet)_
* Destination folder for your backups
* How much space you want to leave free for other purposes.

## Using iabak-sharp
After the initial configuration, you just have to launch the application, with no arguments.

The tool takes care of requesting items to download, auto-updates and re-attempts.

You can close (`CTRL+C`) the application at any time, your downloads will be resumed when you reopen it.
Manually deleting individual items to free up space is also OK, but please re-run iabak-sharp, so that it will notice that some items have been deleted and will notify the server.

Your settings are stored in `%AppData%\IaBak-sharp\Configuration.json` on Windows, and in `$HOME/.config/IaBak-sharp/Configuration.json` on Linux (remember to close the application before modifying it).

## Project status

### Current status
* [X] Retrieval of items from IA
* [X] Hash consistency checks
* [X] Settings initialization
* [X] Server
* [X] User registration
* [X] Disk space checks
* [X] Self-update
* [X] Job assignment
* [X] Download resume (file granularity)
* [X] Ensure a single instance is running
* [X] Run on startup (Windows)

### Future improvements
* [ ] Run on startup (Linux)
* [ ] Prove that an item is actually being stored (hash range challange)
* [ ] Encryption support (for non-public items, would require cooperation with IA)
* [ ] Support more file retrieval mechanisms (eg. ipfs/torrent?)
* [ ] Local import of iabak git-annex items
* [ ] Download of user-chosen items/collections
* [ ] Data restore (auto-update makes this less of an issue)
* [ ] Download resume (byte granularity)

## Supported OSes
Supports Windows, Linux. Command line application only, no GUI.


