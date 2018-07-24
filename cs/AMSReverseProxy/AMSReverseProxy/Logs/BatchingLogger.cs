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
using System;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;

namespace AMSReverseProxy.Logs
{


        public class BatchingLogger : ILogger
        {
            private readonly BatchingLoggerProvider _provider;
            private readonly string _category;

            public BatchingLogger(BatchingLoggerProvider loggerProvider, string categoryName)
            {
                _provider = loggerProvider;
                _category = categoryName;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                if (logLevel == LogLevel.None)
                {
                    return false;
                }
                return true;
            }

            public void Log<TState>(DateTimeOffset timestamp, LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (!IsEnabled(logLevel))
                {
                    return;
                }

                var builder = new StringBuilder();
                builder.Append(timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"));
                builder.Append(" [");
                builder.Append(logLevel.ToString());
                builder.Append("] ");
                builder.Append(_category);
                builder.Append(": ");
                builder.AppendLine(formatter(state, exception));

                if (exception != null)
                {
                    builder.AppendLine(exception.ToString());
                }

                _provider.AddMessage(timestamp, builder.ToString());
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                Log(DateTimeOffset.Now, logLevel, eventId, state, exception, formatter);
            }
        }

}
