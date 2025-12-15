using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Protor;

public abstract class Prototype
{
    public string? Name { get; set; }

    [JsonIgnore]
    public bool IsAnonymous { get; set; }

    public virtual void InitializePrototype()
    {
    }

    public override string ToString()
    {
        return $"{Name} ({GetType().Name})";
    }
}