﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.ServiceCore.API
{
    public class ServiceHub : IDisposable
    {
        private bool _disposed;
        private IServiceHubClients _clients;

        public IServiceHubClients Clients
        {
            get
            {
                CheckDisposed();
                return _clients;
            }
            set
            {
                CheckDisposed();
                _clients = value;
            }
        }

        public virtual Task OnConnectedAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task OnDisconnectedAsync(Exception exception)
        {
            return Task.CompletedTask;
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Dispose(true);

            _disposed = true;
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
