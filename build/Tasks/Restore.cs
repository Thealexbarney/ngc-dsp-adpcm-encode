﻿using Cake.Common.Tools.DotNetCore;
using Cake.Frosting;

namespace Build.Tasks
{
    public sealed class Restore : FrostingTask<Context>
    {
        public override void Run(Context context)
        {
            context.DotNetCoreRestore(context.SourceDir.FullPath);
            context.RestoredCore = true;
        }

        public override bool ShouldRun(Context context) => !context.RestoredCore;
    }
}