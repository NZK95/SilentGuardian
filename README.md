# SilentGuardian
[![Downloads](https://img.shields.io/github/downloads/NZK95/SilentGuardian/total.svg)](https://github.com/NZK95/SilentGuardian/releases) <br>

SilentGuardian is a C# application designed to monitor user activity on a Windows PC and trigger alerts based on configurable behavioral conditions. <br>
It communicates with a Telegram bot for remote control and notifications.

<p align="center">
  <img src="https://github.com/NZK95/SilentGuardian/blob/master/docs/images/SilentGuardian%20%231.png">
</p>

## Key Features
- Global mouse and keyboard activity hooks <br>
- Activity analysis and behavior monitoring <br>
- Locks the workstation (Autolock on alert is too available) <br>
- Captures screenshots <br>
- Records screen video <br>
- Status monitoring via Telegram bot <br>
- Behavior and thresholds fully configurable through ```config.json``` <br>

## Requirements
Last version of SilentGuardian from [`releases`](https://github.com/NZK95/SilentGuardian/releases) <br>
Windows 10 or later <br>
Telegram Bot Token <br>

## Installation
1. Get and paste into ```config.json``` a bot token with ```BotFather``` bot in Telegram. <br>
2. Same thing with  ```chatID``` with ```@userinfobot``` bot. <br>
3. Write first message to your bot. <br>
4. Start and use program. <br>
0. Optionally configure ```config.json```.
