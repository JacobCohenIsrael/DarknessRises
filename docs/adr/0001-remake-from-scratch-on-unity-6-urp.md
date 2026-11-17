# Remake from scratch on Unity 6.6 with URP, for mobile

The 2019 prototype (Unity 2021.3, built-in render pipeline) is built on Unity's Survival Shooter tutorial scripts. Its code isn't worth refactoring; what's worth keeping is its feel and tuning numbers. We're rebuilding from an empty project on Unity 6000.6.5f1, the newest editor installed, using URP for real-time lights and shadows. That lighting model carries the whole game, which is also why we stay 3D with a fixed, tilted top-down camera instead of going 2D: real 3D lights throw long shadows from Obstacles when Lightning flashes.

The game ships on mobile (landscape), funded by ads. Development is PC-first (mouse and keyboard) for fast iteration, but every feature has to fit a mobile performance budget and work with touch controls later.
