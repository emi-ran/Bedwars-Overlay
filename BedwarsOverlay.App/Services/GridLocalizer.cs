using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace BedwarsOverlay.App.Services
{
    /// <summary>
    /// Applies localized column headers and tooltips to the player DataGrid.
    /// DataGrid column headers cannot use DynamicResource easily, so this runs
    /// programmatically after language changes.
    /// </summary>
    public static class GridLocalizer
    {
        public static void ApplyLocalization(DataGrid playersGrid)
        {
            var loc = LocalizationService.Instance;

            // Column headers
            playersGrid.Columns[0].Header = loc["Column_Lvl"];
            playersGrid.Columns[1].Header = loc["Column_Name"];
            playersGrid.Columns[2].Header = loc["Column_WS"];
            playersGrid.Columns[3].Header = loc["Column_FKDR"];
            playersGrid.Columns[4].Header = loc["Column_WLR"];
            playersGrid.Columns[5].Header = loc["Column_FKills"];
            playersGrid.Columns[6].Header = loc["Column_Wins"];
            playersGrid.Columns[7].Header = loc["Column_Sniper"];

            // Column header tooltips
            SetColumnTooltip(playersGrid, 0, loc["Tooltip_Lvl"]);
            SetColumnTooltip(playersGrid, 1, loc["Tooltip_Name"]);
            SetColumnTooltip(playersGrid, 2, loc["Tooltip_WS"]);
            SetColumnTooltip(playersGrid, 3, loc["Tooltip_FKDR"]);
            SetColumnTooltip(playersGrid, 4, loc["Tooltip_WLR"]);
            SetColumnTooltip(playersGrid, 5, loc["Tooltip_FKills"]);
            SetColumnTooltip(playersGrid, 6, loc["Tooltip_Wins"]);
            SetColumnTooltip(playersGrid, 7, loc["Tooltip_Sniper"]);
        }

        private static void SetColumnTooltip(DataGrid playersGrid, int columnIndex, string tooltipText)
        {
            if (columnIndex < 0 || columnIndex >= playersGrid.Columns.Count)
                return;

            var column = playersGrid.Columns[columnIndex];
            if (column.HeaderStyle != null)
            {
                var newStyle = new Style(typeof(DataGridColumnHeader), column.HeaderStyle.BasedOn);
                foreach (var setter in column.HeaderStyle.Setters)
                {
                    if (setter is Setter s && s.Property == FrameworkElement.ToolTipProperty)
                        continue;
                    newStyle.Setters.Add(setter);
                }
                newStyle.Setters.Add(new Setter(FrameworkElement.ToolTipProperty, tooltipText));
                column.HeaderStyle = newStyle;
            }
        }
    }
}
