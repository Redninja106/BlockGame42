namespace BlockGame42.GUI;

struct LayoutScope(Panel panel, LayoutMode mode)
{
    public void Dispose()
    {
        panel.EndScope(mode);
    }
}
