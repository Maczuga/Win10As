# Windows MQTT Client
## Parses information about your PC and sends it to your MQTT server!
Supports listening as well as publishing.

Most workers supports Home Assistant auto-discovery feature. Scripts for setting volume/muting/suspending have to be done manually.

### Publishing (from PC to MQTT server):
Commands to MQTT server are always formated in following way:

[device_topic]/[worker]/[stat] - examples:
- 'maczugapc/volume/mute' with payload set to "OFF" will inform the MQTT server, that your PC is muted.
- 'maczugapc/volume/level' with payload set to "15" will inform the MQTT server, that your PC volume is set to 15%.
- 'maczugapc/performance/cpu' with payload set to "20" will inform the MQTT server, that your PC current CPU usage is 15%.
- 'maczugapc/disks/d/free' with payload set to "50" will inform the MQTT server, that your PC current free D disk space is 50%.

#### Supported workers:

Camera (worker name: 'camera'):
- live - recent camera capture

Disks (worker name: 'disks'):
- [disk_letter]
- - free - free disk space in GB
- - free_pct - free disk space in %
- - total - total disk space in GB

Media player (worker name: 'mediaplayer'):
- none yet, might include support for common media players in future.

Performance (worker name: 'performance'):
- cpu - current CPU usage in %
- ram - current free RAM in MB

Power (worker name: 'power'):
- none yet, monitor / pc state to be included in future releases.

Volume (worker name: 'volume'):
- level - current volume level in %
- level_01 - special case for Home Assistant media_player component, which requires a value in specific format
- mute - ON/OFF - either muted or not

### Listening (receive commands from MQTT server):
Commands to MQTT server are always formated in following way:

[device_topic]/cmd/[worker]/[stat] - examples:
- 'maczugapc/cmd/volume/mute' with payload set to "OFF" will mute the PC.
- 'maczugapc/cmd/camera/snapshot' will save current camera image to a directory chosen in settings on your PC.

#### Supported workers:

Camera (worker name: 'camera'):
- snapshot - saves current camera image to selected earlier directory.

Disks (worker name: 'disks'):
- none so far

Media player (worker name: 'mediaplayer'):
- play_pause - toggles between play and pause status.
- stop - stops playing media on device.
- next_track
- prev_track

Performance (worker name: 'performance'):
- none so far

Power (worker name: 'power'):
- suspend
- hibernate
- monitor_off
- monitor_on

Volume (worker name: 'volume'):
- up - increases volume by payload value or 1 if no payload specified
- down - decreases volume by payload value or 1 if no payload specified
- level - sets volume to level specified in payload
- mute - sets mute to value specified in payload (ON/OFF)
- mute_toggle - toggles the mute value
