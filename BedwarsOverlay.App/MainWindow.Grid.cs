using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using BedwarsOverlay.App.Services;
using BedwarsOverlay.Core.Models;

namespace BedwarsOverlay.App
{
    public partial class MainWindow
    {
        private void EllipsisButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }

        private void CopyUsername_Click(object sender, RoutedEventArgs e)
        {
            if (PlayersGrid.SelectedItem is PlayerRow player)
            {
                try
                {
                    Clipboard.SetText(player.Name);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        LocalizationService.Instance["Clipboard_Error"].Replace("{0}", ex.Message),
                        LocalizationService.Instance["Error_Title"],
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenPlancke_Click(object sender, RoutedEventArgs e)
        {
            if (PlayersGrid.SelectedItem is PlayerRow player)
            {
                OpenUrl($"https://plancke.io/hypixel/player/stats/{player.Name}");
            }
        }

        private bool FilterPlayer(object obj)
        {
            if (obj is PlayerRow row)
            {
                string query = SearchBox.Text.Trim();
                if (string.IsNullOrEmpty(query)) return true;
                return row.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        private void ApplyColumnVisibility()
        {
            if (_settingsRepository == null)
            {
                return;
            }

            LevelColumn.Visibility = _settingsRepository.ShowLevelColumn ? Visibility.Visible : Visibility.Collapsed;
            WinstreakColumn.Visibility = _settingsRepository.ShowWinstreakColumn ? Visibility.Visible : Visibility.Collapsed;
            FkdrColumn.Visibility = _settingsRepository.ShowFkdrColumn ? Visibility.Visible : Visibility.Collapsed;
            WlrColumn.Visibility = _settingsRepository.ShowWlrColumn ? Visibility.Visible : Visibility.Collapsed;
            FinalKillsColumn.Visibility = _settingsRepository.ShowFinalKillsColumn ? Visibility.Visible : Visibility.Collapsed;
            WinsColumn.Visibility = _settingsRepository.ShowWinsColumn ? Visibility.Visible : Visibility.Collapsed;
            ThreatColumn.Visibility = _settingsRepository.ShowThreatColumn ? Visibility.Visible : Visibility.Collapsed;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _diagnosticLines.Clear();
            ClearPlayers();
        }

        private void ClearSearchFilter()
        {
            if (SearchBox.Text.Length == 0)
            {
                return;
            }

            SearchBox.Text = string.Empty;
            SearchPlaceholder.Visibility = Visibility.Visible;
            CollectionViewSource.GetDefaultView(_players)?.Refresh();
        }

        private void PlayersGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;

            var column = e.Column;
            var direction = column.SortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;

            string sortMember = column.SortMemberPath;
            if (string.IsNullOrEmpty(sortMember))
            {
                if (column is DataGridBoundColumn boundCol && boundCol.Binding is Binding binding)
                {
                    sortMember = binding.Path.Path;
                }
            }

            if (string.IsNullOrEmpty(sortMember)) return;

            ApplySort(sortMember, direction);

            foreach (var col in PlayersGrid.Columns)
            {
                col.SortDirection = (col == column) ? direction : null;
            }

            if (_settingsRepository != null)
            {
                _settingsRepository.SortColumn = sortMember;
                _settingsRepository.SortDirection = direction.ToString();
            }
        }

        private void ApplySort(string sortMemberPath, ListSortDirection direction)
        {
            var view = CollectionViewSource.GetDefaultView(_players);
            if (view != null)
            {
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription(sortMemberPath, direction));
                view.Refresh();
            }
        }

        private void RestoreSorting()
        {
            if (_settingsRepository == null) return;

            string sortCol = _settingsRepository.SortColumn;
            string sortDirStr = _settingsRepository.SortDirection;

            if (string.IsNullOrEmpty(sortCol) || string.IsNullOrEmpty(sortDirStr))
            {
                sortCol = "FkdrRaw";
                sortDirStr = "Descending";
            }

            // "Sniper" was renamed to "Threat"; migrate the sorting saved by older versions.
            if (sortCol == "SniperRaw")
            {
                sortCol = "ThreatRaw";
            }

            if (Enum.TryParse<ListSortDirection>(sortDirStr, out var direction))
            {
                foreach (var col in PlayersGrid.Columns)
                {
                    if (col.SortMemberPath == sortCol)
                    {
                        col.SortDirection = direction;
                        ApplySort(sortCol, direction);
                        break;
                    }
                }
            }
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            SearchPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateSearchPlaceholder();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSearchPlaceholder();
            CollectionViewSource.GetDefaultView(_players).Refresh();
        }

        private void MainGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!SearchBox.IsKeyboardFocusWithin)
            {
                return;
            }

            if (e.OriginalSource is DependencyObject source && IsDescendantOf(source, SearchPanel))
            {
                return;
            }

            MainGrid.Focus();
            Keyboard.ClearFocus();
            UpdateSearchPlaceholder();
        }

        private void UpdateSearchPlaceholder()
        {
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text) && !SearchBox.IsKeyboardFocusWithin
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private static bool IsDescendantOf(DependencyObject child, DependencyObject parent)
        {
            for (DependencyObject? current = child; current != null; current = VisualTreeHelper.GetParent(current))
            {
                if (current == parent)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
