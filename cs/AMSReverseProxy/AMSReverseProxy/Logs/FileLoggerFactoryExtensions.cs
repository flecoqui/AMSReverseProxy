//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AMSReverseProxy.Logs
{
    public static class FileLoggerFactoryExtensions
    {
        public static ILoggingBuilder AddFile(this ILoggingBuilder builder)
        {
            builder.Services.AddSingleton<ILoggerProvider, FileLoggerProvider>();
            return builder;
        }

        public static ILoggingBuilder AddFile(this ILoggingBuilder builder, Action<FileLoggerOptions> configure)
        {
            builder.AddFile();
            builder.Services.Configure(configure);

            return builder;
        }
    }
}
