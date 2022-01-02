A Rimworld mod that adds an overlay to show your colonists' foot traffic heatmap. You can see where they've been recently, which areas are visited frequently and optimize your base based on the info.

Functionality

- Adds a toggle to show different colors depending on the the average traffic amount over the past N days(configurable through settings).
- Color changes from least to most traffic: blue->green->yellow->red
- Heatmap is filtered down to the selected colonists. If none is selected, shows traffic from all colonists by default.
- Works with existing saves.

Notes

- Move speed is factored in, a cell with slower terrains or visited by slower pawns will accumulate heat faster.
- Approximately, a single visit(by a colonist with average speed, on normal terrain) will gradually fade out and disappear in N days.
- By default, color scale is adjusted to enhance display for less visited areas so the color differences among them are more prominent. This can be turned off in settings, to show the color linear to the actual traffic amount.
