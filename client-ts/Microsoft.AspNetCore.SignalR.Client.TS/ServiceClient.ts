// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { HubConnection } from "./HubConnection"
import { IHubConnectionOptions } from "./IHubConnectionOptions"

export { TransportType } from "./Transports"
export { HttpConnection } from "./HttpConnection"
export { HubConnection } from "./HubConnection"
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
        this.connection = new HubConnection(`http://${this.credential.hostName}/signalr`, this.options);
    }

    private parseConnectionString(connectionString: string): IServiceCredential {
        const dict = {};
        connectionString.split(";").forEach(x => {
            if (!x) return;
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

    subscribe(channels: string[]): Promise<any> {
        return this.connection.invoke("subscribe", channels);
    }

    unsubscribe(channels: string[]): Promise<any> {
        return this.connection.invoke("unsubscribe", channels);
    }

    publish(channel: string, event: string, data: any): Promise<any> {
        return this.connection.invoke("publish", channel, event, data);
    }

    on(channel: string, event: string, handler: (data: any) => void) {
        this.connection.on(`${channel}:${event}`, handler);
    }

    off(channel: string, event: string, handler: (data: any) => void) {
        this.connection.off(`${channel}:${event}`, handler);
    }
}
