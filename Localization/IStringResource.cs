// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.CareersBot
{
    public interface IStringResource
    {
        string ErrorHelpNotFound { get; }

        string ErrorUnsupportedOrganization { get; }

        string ResponseGreeting { get; }

        string ResponseEnd { get; }

        string ResponseWelcome { get; }

        string PromptQuestion { get; }

        string RepromptQuestion { get; }
    }
}