﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.ServiceCore.API
{
    public interface IServiceClientProxy
    {
        /// <summary>
        /// Invokes a method on the connection(s) represented by the <see cref="IServiceClientProxy"/> instance.
        /// </summary>
        /// <param name="method">name of the method to invoke</param>
        /// <param name="args">argumetns to pass to the client</param>
        /// <returns>A task that represents when the data has been sent to the client.</returns>
        Task InvokeAsync(string method, object[] args);
    }
}
