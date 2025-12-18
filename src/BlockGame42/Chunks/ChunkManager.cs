using System;
using System.Collections.Generic;
using System.Text;

namespace BlockGame42.Chunks;

internal abstract class ChunkManager
{
    protected GameClient Client { get; private set; }

    public ChunkManager(GameClient client)
    {
        this.Client = client;
    }

    public abstract void Initialize();
    public abstract void Update();

}
