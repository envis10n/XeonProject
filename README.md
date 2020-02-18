# XeonProject
A C# game engine (text-based) that I'm writing while learning stuff.

Basic idea:
- Engine that is multi-threaded, running each internal "service" on its own thread.
- Generic interfaces for the internal Network handler, to make it protocol agnostic.
- Provide a comprehensive and robust plugin API to allow for plugins to handle the majority of game logic.
- Add hooks and events to the internal game system. This will allow plugins to handle events that can be emitted by other plugins.
