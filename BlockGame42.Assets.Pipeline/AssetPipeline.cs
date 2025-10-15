using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42.Assets.Pipeline;
internal abstract class AssetPipeline
{
    public abstract string PrimaryInputExtension { get; }
    public abstract string PrimaryOutputExtension { get; }

    public abstract void Build(AssetBuildContext context);
}
