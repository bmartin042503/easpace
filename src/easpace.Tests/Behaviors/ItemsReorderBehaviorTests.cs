// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Behaviors;
using FluentAssertions;

namespace easpace.Tests.Behaviors;

public class ItemsReorderBehaviorTests
{
    // each item is 40 px high, stacked without spacing; its handle is the left 20 px
    private const double ItemHeight = 40;
    private static readonly double HandleX = 10;
    private static readonly double TextX = 100;

    private readonly List<ItemMoveRequest> _requests = [];

    private (Window Window, ItemsControl Items) ShowList(int count)
    {
        var items = new ItemsControl
        {
            ItemsSource = Enumerable.Range(0, count).Select(i => $"Item {i}").ToList(),
            ItemTemplate = new FuncDataTemplate<string>((text, _) => new DockPanel
            {
                Height = ItemHeight,
                Children =
                {
                    new Border { Classes = { "drag-handle" }, Width = 20, Background = Brushes.Transparent, [DockPanel.DockProperty] = Dock.Left },
                    new TextBlock { Text = text, Background = Brushes.Transparent }
                }
            }),
            VerticalAlignment = VerticalAlignment.Top
        };
        ItemsReorderBehavior.SetMoveCommand(items, new RelayCommand<ItemMoveRequest>(request => _requests.Add(request)));

        var window = new Window { Width = 300, Height = 400, Content = items };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, items);
    }

    private static Point ItemPoint(int index, double x, double fraction) => new(x, (index + fraction) * ItemHeight);

    private static IEnumerable<string> ClassesOf(ItemsControl items, int index) =>
        items.ContainerFromIndex(index)!.Classes.Where(c => c is "dragging" or "drop-before" or "drop-after");

    [AvaloniaTheory]
    [InlineData(0, 2, 0.75, 2)] // first below the third
    [InlineData(0, 3, 0.75, 3)] // first to the end
    [InlineData(3, 0, 0.25, 0)] // last to the start
    [InlineData(3, 1, 0.25, 1)] // last above the second
    [InlineData(1, 2, 0.25, 1)] // above the next one: stays where it is
    public void DraggingByTheHandle_MovesTheItemToTheMarkedGap(int from, int overIndex, double overFraction, int expectedTo)
    {
        var (window, _) = ShowList(4);

        window.MouseDown(ItemPoint(from, HandleX, 0.5), MouseButton.Left);
        window.MouseMove(ItemPoint(overIndex, TextX, overFraction));
        window.MouseUp(ItemPoint(overIndex, TextX, overFraction), MouseButton.Left);

        if (expectedTo == from) _requests.Should().BeEmpty();
        else _requests.Should().Equal(new ItemMoveRequest(from, expectedTo));

        window.Close();
    }

    [AvaloniaFact]
    public void Dragging_MarksTheDraggedItemAndTheGapAndCleansUpOnDrop()
    {
        var (window, items) = ShowList(4);

        window.MouseDown(ItemPoint(0, HandleX, 0.5), MouseButton.Left);
        window.MouseMove(ItemPoint(2, TextX, 0.75));

        // the gap below the third item is marked on the item after it; only the last item marks the gap below itself
        ClassesOf(items, 0).Should().Equal("dragging");
        ClassesOf(items, 3).Should().Equal("drop-before");

        window.MouseMove(ItemPoint(3, TextX, 0.75));

        ClassesOf(items, 3).Should().Equal("drop-after");

        window.MouseMove(ItemPoint(2, TextX, 0.25));

        ClassesOf(items, 3).Should().BeEmpty();
        ClassesOf(items, 2).Should().Equal("drop-before");

        // back next to itself: no move, so no gap is marked
        window.MouseMove(ItemPoint(0, TextX, 0.75));

        Enumerable.Range(1, 3).SelectMany(i => ClassesOf(items, i)).Should().BeEmpty();

        window.MouseMove(ItemPoint(2, TextX, 0.25));

        window.MouseUp(ItemPoint(2, TextX, 0.25), MouseButton.Left);

        Enumerable.Range(0, 4).SelectMany(i => ClassesOf(items, i)).Should().BeEmpty();
        _requests.Should().Equal(new ItemMoveRequest(0, 1));

        window.Close();
    }

    [AvaloniaFact]
    public void Escape_CancelsTheDragWithoutMoving()
    {
        var (window, items) = ShowList(4);

        window.MouseDown(ItemPoint(0, HandleX, 0.5), MouseButton.Left);
        window.MouseMove(ItemPoint(3, TextX, 0.75));
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);

        Enumerable.Range(0, 4).SelectMany(i => ClassesOf(items, i)).Should().BeEmpty();

        window.MouseMove(ItemPoint(2, TextX, 0.75));
        window.MouseUp(ItemPoint(2, TextX, 0.75), MouseButton.Left);

        _requests.Should().BeEmpty();
        Enumerable.Range(0, 4).SelectMany(i => ClassesOf(items, i)).Should().BeEmpty();

        window.Close();
    }

    [AvaloniaFact]
    public void PressingOutsideTheHandle_DoesNotDrag()
    {
        var (window, items) = ShowList(4);

        window.MouseDown(ItemPoint(0, TextX, 0.5), MouseButton.Left);
        window.MouseMove(ItemPoint(3, TextX, 0.75));

        ClassesOf(items, 0).Should().BeEmpty();

        window.MouseUp(ItemPoint(3, TextX, 0.75), MouseButton.Left);

        _requests.Should().BeEmpty();

        window.Close();
    }
}
