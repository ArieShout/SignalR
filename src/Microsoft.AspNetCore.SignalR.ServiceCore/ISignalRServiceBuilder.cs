// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.ServiceCore
{
    public interface ISignalRServiceBuilder
    {
        IServiceCollection Services { get; }
    }
}
