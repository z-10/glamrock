# Windows Pentagram

This folder contains a real native GlamRock demo.

Files:

- `paint-it-pentagram.rock`
  Opens a real Win32 window, draws a pentagram into it, and scrolls its own source text upward in yellow.
- `Stage_Fright.tracklist`
  Window creation and device-context calls from `user32.dll`.
- `Black_Steel.tracklist`
  Pen, brush, and drawing calls from `gdi32.dll`.
- `Thunder_Road.tracklist`
  `Sleep` so the window stays open long enough to admire.

Usage:

```powershell
cd examples/windows
..\..\Starship\Rockstar\bin\Debug\net9.0\Rockstar.exe paint-it-pentagram.rock
```

Notes:

- Windows only.
- No shell-out helper is involved.
- The demo uses the built-in `STATIC` window class and draws directly through a window DC.
- The lyrics are read from the song file itself through the built-in `Tome of Power` album.
