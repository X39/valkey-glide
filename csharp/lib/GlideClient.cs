﻿// Copyright Valkey GLIDE Project Contributors - SPDX Identifier: Apache-2.0

using Glide.Commands;

using static Glide.ConnectionConfiguration;

namespace Glide;

public sealed class GlideClient(StandaloneClientConfiguration config) : BaseClient(config), IConnectionManagementCommands
{
}
