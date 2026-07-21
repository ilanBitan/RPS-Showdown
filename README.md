# RPS-Showdown

RPS-Showdown is a turn-based tactical strategy game built in Unity that combines Rock-Paper-Scissors combat with grid-based board movement. Players control units on a 6x7 battlefield, move one tile at a time, and resolve encounters through RPS matchups, special unit roles, and battle animations.

## Overview

The game supports both single-player and online play. In PvE, players can face AI opponents across three difficulty levels. In PvP, two players join the same room through Firebase, place their units during setup, and keep their boards synchronized through real-time move logging and battle state updates.

## Game Teaser

[![Click to Watch RPS-SHOWDOWN teaser on YouTube](https://img.shields.io/badge/YouTube-Watch%20Gameplay%20Teaser-red?style=for-the-badge&logo=youtube)](https://www.youtube.com/watch?v=NSYpIRhCA6g)

Click the button above to watch the gameplay teaser on YouTube.

## Features

- 6x7 tactical board with tile-based movement and unit placement
- Rock, Paper, Scissors combat resolution
- Special unit roles, including Flag and Trap
- Turn-based gameplay with a visible turn timer
- Three AI difficulty levels: Easy, Medium, and Hard
- Online PvP rooms with Firebase Realtime Database synchronization
- Email/password authentication with profile and stats tracking
- Battle and encounter animations for combat, traps, and flag captures
- Weapon sprite reveals and result-driven character animations during duels
- Mobile-friendly input support alongside keyboard controls

## Gameplay

1. Players begin by logging in or creating an account.
2. A mode is selected from the main menu: PvE or PvP.
3. Units are placed on the board and setup rules determine where flags and traps can be chosen.
4. On each turn, a unit can move one tile orthogonally or engage an adjacent enemy.
5. Combat is resolved through Rock-Paper-Scissors logic, with special outcomes for traps and flags.
6. Capturing the enemy flag ends the match.

## Game Modes

- PvE Easy, Medium, and Hard
- PvP room-based multiplayer

## Technical Stack

- Unity 2022.3.60f1
- C#
- TextMeshPro
- Firebase Authentication
- Firebase Realtime Database

## Animation System

The animation layer is tightly coupled to combat feedback and turn flow:

- Fight intro animation plays when combat starts
- Dedicated result animations for win/loss, trap encounters, and flag captures
- Weapon visuals update dynamically to reflect chosen RPS actions
- Units use movement and jump feedback during board actions
- Turn timer pauses during active battles to avoid race conditions

## Scenes

The project includes the following build scenes:

- LoginScene
- MainMenuScene
- GameScene
- SettingsScene
- ShopScene

## Project Structure

- Assets/Scripts/Units: unit models, visuals, and selection behavior
- Assets/Scripts/Managers: board, battle, turn, timer, setup, and game flow systems
- Assets/Scripts/AI: AI controllers for different difficulty levels
- Assets/Scripts/accounts: Firebase auth, profile, and user data services

## Notes

- Firebase configuration is required for login, profiles, and PvP rooms.
- The project is designed around the GameScene board flow and the Main Menu / Login scene flow.
