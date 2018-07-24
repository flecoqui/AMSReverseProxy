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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AMSReverseProxy
{

    public class EndpointConfiguration

    {

        public string Host { get; set; }

        public int? Port { get; set; }

        public string Scheme { get; set; }

        public string StoreName { get; set; }

        public string StoreLocation { get; set; }

        public string FilePath { get; set; }

        public string Password { get; set; }

    }
}
