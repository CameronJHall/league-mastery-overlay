<h1 align="center">League Mastery Overlay</h1>

<h4 align="center">a Windows desktop overlay to display champion mastery info during ARAM champ select</h4>

<p align="center">
    <img src="https://img.shields.io/badge/lisence-MIT-blue" alt="MIT License">
    <img src="https://img.shields.io/badge/c%23-green" alt="c#">
</p>

<p align="center">
  <a href="#why">Why</a> •
  <a href="#how-it-works">How It Works</a> •
  <a href="#status">Status</a> •
  <a href="#disclaimer">Disclaimer</a>
</p>

## Why

I want to try to hit master 5 on all champions in league, but most of the time I'm playing ARAM. 
This overlay helps me keep track of which champions I've hit mastery on and which I haven't.

It displays UI elements on top of the League client during champ select only.
It runs as a separate, transparent window and does not inject into the game or client.

## How it works

Written in C# (.NET 10, WPF), reads the League LCU lockfile for auth, polls the LCU API over HTTPS, renders a transparent, click-through, always-on-top window

Tracks the LeagueClientUx.exe window and positions itself accordingly.

## Status

Project skeleton and LCU integration are in place.
UI rendering for champion select is still in progress.

## Disclaimer

Unofficial project. Use at your own risk.