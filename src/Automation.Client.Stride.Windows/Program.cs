using Automation.Client.Stride;

var windowed = args.Contains("--windowed", StringComparer.OrdinalIgnoreCase) ||
               Environment.GetEnvironmentVariable("AUTOMATION_WINDOWED") == "1";
var displaySize = windowed ? (Width: 1280, Height: 720) : FullscreenWindow.PrimaryDisplaySize;
if (!windowed) _ = FullscreenWindow.ApplyWhenReadyAsync();
using var game = new DishStationGame(!windowed, displaySize.Width, displaySize.Height);
game.Run();
