// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { ConnectionClosed } from "./Common"
import { HttpClient } from "./HttpClient"
import { Observable } from "./Observable"
import { ILogger, LogLevel } from "./ILogger"
import { LoggerFactory } from "./Loggers"
import { HubConnection } from "./HubConnection"
import { IHubConnectionOptions } from "./IHubConnectionOptions"

export { TransportType } from "./Transports"
export { HttpConnection } from "./HttpConnection"
export { HubConnection } from "./HubConnection"
export { JsonHubProtocol } from "./JsonHubProtocol"
export { LogLevel, ILogger } from "./ILogger"
export { ConsoleLogger, NullLogger } from "./Loggers"

interface IServiceEndpoint {
    serviceUrl: string;
    jwtBearer: string;
}

export class ServiceConnection {
    private url: string = "";
    private connectionPromise: Promise<void>;
    private connection: HubConnection;
    private readonly options: IHubConnectionOptions;
    private readonly logger: ILogger;

    constructor(url: string, options: IHubConnectionOptions = {}) {
        this.options = options || {};
        this.url = url;
        this.logger = LoggerFactory.createLogger(options.logging);

        // TODO: enable custom authorization
        const httpClient = new HttpClient();
        this.connectionPromise = httpClient.get(url)
            .then(
                response => {
                    this.logger.log(LogLevel.Information, "Successfully get service endpoint information.");
                    const endpoint: IServiceEndpoint = JSON.parse(response);
                    if (!this.options.jwtBearer) {
                        this.options.jwtBearer = () => endpoint.jwtBearer;
                    }
                    this.connection = new HubConnection(endpoint.serviceUrl, this.options);
                })
            .catch(error => {
                this.logger.log(LogLevel.Error, "Failed to get service endpoint information.");
                Promise.reject(error);
            });
    }

    async start(): Promise<void> {
        // TODO: find a better way to assure connection existence
        await this.connectionPromise;
        return this.connection.start();
    }

    async stop(): Promise<void> {
        await this.connectionPromise;
        return this.connection.stop();
    }

    async stream<T>(methodName: string, ...args: any[]): Promise<Observable<T>> {
        await this.connectionPromise;
        return this.connection.stream<T>(methodName, ...args);
    }

    async send(methodName: string, ...args: any[]) {
        await this.connectionPromise;
        return this.connection.send(methodName, ...args);
    }

    async invoke(methodName: string, ...args: any[]): Promise<any> {
        await this.connectionPromise;
        return this.connection.invoke(methodName, ...args);
    }

    async on(methodName: string, method: (...args: any[]) => void) {
        await this.connectionPromise;
        this.connection.on(methodName, method);
    }

    async off(methodName: string, method: (...args: any[]) => void) {
        await this.connectionPromise;
        this.connection.off(methodName, method);
    }

    async onclose(callback: ConnectionClosed) {
        await this.connectionPromise;
        this.connection.onclose(callback);
    }
}
