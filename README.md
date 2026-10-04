# Games Panda Unity dedicated server

This repository contains the Unity server project used for Mirror multiplayer matches and Edgegap dedicated-server builds. The project snapshot comes from `RituGamesServer` branch `cricket_determenstic_server` at commit `02ae3241b` (29 September 2026).

Open this repository as a Unity project. `Assets`, `Packages`, and `ProjectSettings` are included. The local Edgegap build under `Builds/EdgegapServer` is generated output and is not committed; build the Linux dedicated server from Unity for deployment.

Deployment credentials and local configuration are deliberately excluded. Configure Edgegap and Firebase in your own environment before building or deploying. Do not commit local `.env` files or API settings assets.
