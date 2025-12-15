using BlockGame42.Blocks;
using BlockGame42.Rendering;
using Protor;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json.Serialization;

namespace BlockGame42.Blocks.Materials;

abstract class BlockMaterial : Prototype
{
    [JsonIgnore]
    public uint TexID { get; set; }
}
