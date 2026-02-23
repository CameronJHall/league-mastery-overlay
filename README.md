﻿<h1 align="center">League Mastery Overlay</h1>

<h4 align="center">a Windows desktop overlay to display champion mastery info during ARAM champ select</h4>

<p align="center">
    <img src="https://img.shields.io/badge/lisence-MIT-blue" alt="MIT License">
    <img src="https://img.shields.io/badge/c%23-green" alt="c#">
</p>

<p align="center">
  <a href="#why">Why</a> •
  <a href="#how-it-works">How It Works</a> •
  <a href="#status">Status</a> •
  <a href="#development">Development</a> •
  <a href="#disclaimer">Disclaimer</a>
</p>

> **Note:** This app will download League of Legends mastery icons on the first run and store them in the app data folder.
> This is to avoid bundling a large number of image assets with the app. The icons are sourced from the official
> League of Legends data dragon repository.

## Why

I want to try to hit master 5 on all champions in league, but most of the time I'm playing ARAM. 
This overlay helps me keep track of which champions I've hit mastery on and which I haven't.

It displays UI elements on top of the League client during champ select only.
It runs as a separate, transparent window and does not inject into the game or client.

## How it works

Written in C# (.NET 10, WPF), reads the League LCU lockfile for auth, polls the LCU API over HTTPS, renders a transparent, click-through, always-on-top window

Tracks the LeagueClientUx.exe window and positions itself accordingly.

## Status

The basic implementation is complete and functional. It can read the mastery data for the champion being hovered in champ
select and display the appropriate mastery level as an overlay on top of the client. It also shows progress towards the
next mastery level as a circular progress bar around the level icon. There is a basic status bar icon that has a 
context menu with a basic debugging option.

## Development

This project was built largely with the help of AI (GitHub Copilot). While the core architecture, design decisions, and feature requirements were determined by myself, AI assisted significantly with:

- Code structure and implementation
- API integration objects
- Debugging and optimization

Using Copilot allowed for much faster development than I would have been able to achieve on my own for a beginner C# project. That said, I want to revise a lot of the patterns to make them more intuitive.

## Upcoming Features

- Add stats and fun titles in the lobby overlay
  - "grey screen enjoyer", most deaths
  - "who wants a piece of the champ", longest win streak
  - "all for you", most healing
  - "tons of damage", most damage dealt
  - etc.
- Show an idle animation in client outside of champ select (pending layering solution)
- Add a settings menu to configure which mastery levels to show, toggle animations, etc.

## Disclaimer

Unofficial project. Use at your own risk.