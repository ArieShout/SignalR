// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { IHubConnectionOptions } from "./IHubConnectionOptions"

export interface IServiceConnectionOptions extends IHubConnectionOptions {
    uid?: string;
    authHeader?: () => string;
}
