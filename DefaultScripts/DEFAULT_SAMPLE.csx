// log output
Msg("Hello Neos!");
Warn("Warning");
Error("Error");

// shorthand for Engine.Current.WorldManager.FocusedWorld
var world = FocusedWorld;
Msg($"WorldName: {world.Name}");
