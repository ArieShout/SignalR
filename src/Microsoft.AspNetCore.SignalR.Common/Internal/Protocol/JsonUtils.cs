// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.SignalR.Internal.Protocol
{
    public static class JsonUtils
    {
        public static T GetOptionalProperty<T>(JObject json, string property, JTokenType expectedType = JTokenType.None, T defaultValue = default)
        {
            var prop = json[property];

            if (prop == null)
            {
                return defaultValue;
            }

            return GetValue<T>(property, expectedType, prop);
        }

        public static T GetRequiredProperty<T>(JObject json, string property, JTokenType expectedType = JTokenType.None)
        {
            var prop = json[property];

            if (prop == null)
            {
                throw new FormatException($"Missing required property '{property}'.");
            }

            return GetValue<T>(property, expectedType, prop);
        }

        public static T GetValue<T>(string property, JTokenType expectedType, JToken prop)
        {
            if (expectedType != JTokenType.None && prop.Type != expectedType)
            {
                throw new FormatException($"Expected '{property}' to be of type {expectedType}.");
            }
            return prop.Value<T>();
        }

        public static IDictionary<string, string> GetPropertyDictionary(JObject json, string property)
        {
            IDictionary<string, JToken> prop = (JObject)json[property];
            return prop?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());
        }
    }
}
