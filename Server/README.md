# GLITCHED stall server

The website, game download, copy verification and admin page for the GLITCHED stall. **The full guide is [../Docs/SERVER.md](../Docs/SERVER.md).**

```bat
node server.js            :: start (http://localhost:8080, admin at /admin)
npm test                  :: end-to-end check against a throwaway database
launcher\Make-Desktop-Shortcut.ps1   :: "GLITCHED Server" desktop shortcut (closing its window stops the server)
tools\export-game-key.cmd            :: put this server's public key into the Unity project before building
launcher\Reset-Admin-Password.cmd    :: forgot the admin password
```

`data/` holds the private licence key and the database. It is not in git: back it up, never share it.
