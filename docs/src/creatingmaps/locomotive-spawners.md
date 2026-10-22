# Locomotive Spawners

This page describes how to have locomotives spawn on tracks in your map.

There are two types of locomotive spawners:

- Vanilla Locomotive Spawner: can only spawn base game locomotives.
- Custom Locomotive Spawner: can also spawn [Custom Car Loader](https://www.nexusmods.com/derailvalley/mods/324)(CCL) locomotives.

Both spawners function the same way, the only difference being how to set them up in the editor.  
The `Vanilla Locomotive Spawner` uses enums to define what locomotives can spawn, making it easy to set up quickly.  
The `Custom Locomotive Spawner` uses a list of locomotive IDs, making it compatible with CCL locomotives.

Nothing is preventing you from spawning vanilla locomotives with a custom spawner if you know their ID.

## Station-based vs. Track-based

Locomotive Spawners can be set up in two ways. Either by adding the component to the same object your `Track` component is on, or the same (or child) object of a `Station` component.

The main difference is, with a station-based spawner, the game will try to spawn you at a station that has a locomotive you're licensed to use when loading in after cars fail to spawn, such as when trackage is modified between sessions.

## Setup

Locomotive spawners may seem complicated at first, but they're actually quite simple. Each spawner consists of a list of 'Locomotive Groups', which are themselves a list of locomotives that should spawn.

When the game tries to spawn a locomotive, it'll pick a random locomotive group, then spawn all locomotives in that group in order. This allows you to spawn pairs of locomotives, such as the S282A (Locomotive) and S282B (Tender).
