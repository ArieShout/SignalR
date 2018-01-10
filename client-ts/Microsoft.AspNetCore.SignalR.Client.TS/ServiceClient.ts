// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { ConnectionClosed } from "./Common"
import { IConnection } from "./IConnection"
import { HttpConnection } from "./HttpConnection"
import { HubConnection } from "./HubConnection"
import { TransportType, TransferMode } from "./Transports"
import { Subject, Observable } from "./Observable"
import { IHubProtocol, ProtocolType, MessageType, HubMessage, CompletionMessage, ResultMessage, InvocationMessage, StreamInvocationMessage, NegotiationMessage } from "./IHubProtocol";
import { JsonHubProtocol } from "./JsonHubProtocol";
import { TextMessageFormat } from "./Formatters"
import { Base64EncodedHubProtocol } from "./Base64EncodedHubProtocol"
import { ILogger, LogLevel } from "./ILogger"
import { ConsoleLogger, NullLogger, LoggerFactory } from "./Loggers"
import { IHubConnectionOptions } from "./IHubConnectionOptions"
import * as jwt from "jsonwebtoken"

export { TransportType } from "./Transports"
export { HttpConnection } from "./HttpConnection"
export { JsonHubProtocol } from "./JsonHubProtocol"
export { LogLevel, ILogger } from "./ILogger"
export { ConsoleLogger, NullLogger } from "./Loggers"

export interface IServiceOptions extends IHubConnectionOptions {
    
}

interface IServiceCredential {
    hostName: string;
    accessKey: string;
}

export class ServiceClient {
    private readonly options: IServiceOptions;
    private readonly credential: IServiceCredential;
    private connection: HubConnection;

    constructor(connectionString: string, options: IServiceOptions = {}) {
        this.options = options || {};
        this.credential = this.parseConnectionString(connectionString);
    }

    private parseConnectionString(connectionString: string): IServiceCredential {
        const dict = {};
        connectionString.split(";").forEach(x => {
            const items = x.split("=", 2);
            dict[items[0].toLowerCase()] = items[1];
        });
        return {
            hostName: dict["hostname"],
            accessKey: dict["accesskey"]
        };
    }

    async start(): Promise<void> {
        await this.connection.start();
    }

    async subscribe(channel: string): Promise<void> {
        await this.connection.send("subscribe", channel);
    }

    async unsubscribe(channel: string): Promise<void> {
        await this.connection.send("unsubscribe", channel);
    }

    async publish(channel: string, event: string, data: any): Promise<void> {
        await this.connection.send("publish", "{channel}:{event}", data);
    }
}
