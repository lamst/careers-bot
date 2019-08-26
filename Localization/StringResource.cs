// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Localization;

namespace Microsoft.CareersBot
{
    public class StringResource : IStringResource
    {
        private readonly IStringLocalizer<StringResource> localizer;

        /// <summary>
        /// Creates a new instance of the class
        /// </summary>
        /// <param name="localizer">The localizer to create localized strings </param>
        public StringResource(IStringLocalizer<StringResource> localizer)
        {
            this.localizer = localizer;
        }

        public string ErrorHelpNotFound => localizer["ErrorHelpNotFound"];

        public string Greeting => localizer["Greeting"];
    }
}