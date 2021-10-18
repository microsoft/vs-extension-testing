// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.
using System;

namespace Xunit.Harness
{
    internal class Settings
    {
        private bool? _isExp;

        internal static readonly Settings Default = new Settings();

        public string VsRootSuffix
        {
            get
            {
                if (!_isExp.HasValue)
                {
                    _isExp = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NOEXP"));
                }

                return _isExp.Value ? "Exp" : string.Empty;
            }
        }
    }
}
