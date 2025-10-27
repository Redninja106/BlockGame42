using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.GUI;
internal class Panel
{
    private GUIRenderer renderer;
    private Font font;

    public Vector2 Size { get; set; }
    public Vector2 Position { get; set; }

    private Stack<LayoutState> states = [];
    private LayoutState state;

    public Extent LastItemExtent;

    public void Text(string text)
    {
        renderer.PushText(font, text, Vector2.Zero, 0xFFFFFFFF);
    }
    /// <summary>
    /// Shortcut for <c>BeginScope(LayoutMode.Row)</c>. The LayoutScope returned must be disposed to end the scope.
    /// </summary>
    public LayoutScope Row()
    {
        BeginScope(LayoutMode.Row);
        return new(this, LayoutMode.Row);
    }

    /// <summary>
    /// Shortcut for <c>BeginScope(LayoutMode.Column)</c>. The LayoutScope returned must be disposed to end the scope
    /// </summary>
    public LayoutScope Column()
    {
        BeginScope(LayoutMode.Column);
        return new(this, LayoutMode.Column);
    }

    public void BeginScope(LayoutMode mode)
    {
        states.Push(state);

        state = new()
        {
            LayoutMode = mode,
            Cursor = this.state.Cursor,
            Extent = new(this.state.Cursor, this.state.Cursor),
        };
    }

    public void EndScope(LayoutMode mode)
    {
        if (state.LayoutMode != mode)
        {
            throw new InvalidOperationException("mismatching scope types");
        }

        // return to old scope and add ended one as an item to it
        LayoutState endedState = state;
        state = states.Pop();
        InsertItem(endedState.Extent);
    }

    public void InsertItem(Extent itemExtent)
    {
        LastItemExtent = itemExtent;
        if (state.LayoutMode == LayoutMode.Row)
        {
            state.Cursor.X = LastItemExtent.Max.X;
        }
        else
        {
            state.Cursor.Y = LastItemExtent.Max.Y;
        }

        this.state.Extent = Extent.Union(this.state.Extent, itemExtent);

        // if (ShowGUIItemBounds)
        // {
        //     AddCommand(new DrawCommand.Rectangle(itemBounds, Color.Red, false));
        // }
    }

    public void Text(Font font, string text)
    {
        Extent textExtent = font.Measure(text);
        renderer.PushText(font, text, this.Position, 0xFFFFFFFF);
        InsertItem(textExtent);
    }

    struct LayoutState
    {
        public Extent Extent;
        public Vector2 Cursor;
        public LayoutMode LayoutMode;
    }
}
