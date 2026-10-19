using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace BedwarsOverlay.App.Services
{
    /// <summary>
    /// Applies localized column headers and tooltips to the player DataGrid.
    /// DataGrid column headers cannot use DynamicResource easily, so this runs
    /// programmatically after language changes.
    /// Columns are resolved by their <see cref="DataGridColumn.SortMemberPath"/> instead of a
    /// hard-coded index, so hiding/reordering columns cannot shift the wrong header.
    /// </summary>
    public static class GridLocalizer
    {
        private static readonly (string SortMemberPath, string HeaderKey, string TooltipKey)[] ColumnMap =
        {
            ("Team", "Column_Team", "Tooltip_Team"),
            ("LevelRaw", "Column_Lvl", "Tooltip_Lvl"),
            ("Name", "Column_Name", "Tooltip_Name"),
            ("WinstreakRaw", "Column_WS", "Tooltip_WS"),
            ("FkdrRaw", "Column_FKDR", "Tooltip_FKDR"),
            ("WlrRaw", "Column_WLR", "Tooltip_WLR"),
            ("FinalKillsRaw", "Column_FKills", "Tooltip_FKills"),
            ("WinsRaw", "Column_Wins", "Tooltip_Wins"),
            ("ThreatRaw", "Column_Threat", "Tooltip_Threat"),
        };

        public static void ApplyLocalization(DataGrid playersGrid)
        {
            var loc = LocalizationService.Instance;

            foreach (var (sortMemberPath, headerKey, tooltipKey) in ColumnMap)
            {
                var column = FindColumn(playersGrid, sortMemberPath);
                if (column == null)
                {
                    continue;
                }

                column.Header = loc[headerKey];
                SetColumnTooltip(column, loc[tooltipKey]);
            }
        }

        private static DataGridColumn? FindColumn(DataGrid playersGrid, string sortMemberPath)
        {
            foreach (var column in playersGrid.Columns)
            {
                if (string.Equals(column.SortMemberPath, sortMemberPath, System.StringComparison.Ordinal))
                {
                    return column;
                }
            }

            return null;
        }

        private static void SetColumnTooltip(DataGridColumn column, string tooltipText)
        {
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
