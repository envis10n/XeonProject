# XeonProject
A C# game engine (text-based) that I'm writing while learning stuff.

Basic idea:
- Engine that is multi-threaded, running each internal "service" on its own thread.
- Generic interfaces for the internal Network handler, to make it protocol agnostic.
- Provide a comprehensive and robust plugin API to allow for plugins to handle the majority of game logic.
- Add hooks and events to the internal game system. This will allow plugins to handle events that can be emitted by other plugins.

## Setup
This project requires .NET Core SDK 3.1

1. Clone the repo.
2. Build the solution.
3. (Optional) Enter the XeonNet folder and `dotnet build`
4. Run XeonProject to generate default config and make the plugins directory.
5. (Optional) Copy XeonNet.dll to the plugins directory.
6. (Optional) Edit XeonConfig.json
7. Run XeonProject