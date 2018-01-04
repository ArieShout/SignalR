// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { ConnectionClosed } from "./Common"
import { HttpClient, DefaultHttpClient } from "./HttpClient"
import { Observable } from "./Observable"
import { ILogger, LogLevel } from "./ILogger"
import { LoggerFactory } from "./Loggers"
import { HubConnection } from "./HubConnection"
import { IServiceConnectionOptions } from "./IServiceConnectionOptions"

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
    private readonly options: IServiceConnectionOptions;
    private readonly logger: ILogger;
    private readonly httpClient: HttpClient = new DefaultHttpClient();

    constructor(url: string, options: IServiceConnectionOptions = {}) {
        this.options = options || {};
        this.url = url;
        this.logger = LoggerFactory.createLogger(options.logger);

        let headers;
        if (this.options.authHeader) {
            headers = new Map<string, string>();
            headers.set("Authorization", this.options.authHeader());
        }

        this.connectionPromise = this.httpClient.get(url, headers)
            .then(
                response => {
                    this.logger.log(LogLevel.Information, "Successfully get service endpoint information.");
                    const endpoint: IServiceEndpoint = JSON.parse(response.content as string);
                    this.connection = this.createHubConnection(endpoint);
                })
            .catch(error => {
                this.logger.log(LogLevel.Error, "Failed to get service endpoint information.");
                Promise.reject(error);
            });
    }

    private createHubConnection(endpoint: IServiceEndpoint): HubConnection {
        if (!this.options.accessToken) {
            this.options.accessToken = () => endpoint.jwtBearer;
        }

        if (!this.options.httpClient) {
            this.options.httpClient = this.httpClient;
        }

        let serviceUrl = endpoint.serviceUrl;
        if (this.options.uid) {
            if (serviceUrl.indexOf("?") > -1) {
                serviceUrl = serviceUrl + "&uid=" + this.options.uid;
            } else {
                serviceUrl = serviceUrl + "?uid=" + this.options.uid;
            }
        }

        return new HubConnection(serviceUrl, this.options);
    }

    // TODO: allow restartable connection
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
