﻿#region Copyright (c) Pixeval/Pixeval
// GPL v3 License
// 
// Pixeval/Pixeval
// Copyright (c) 2023 Pixeval/DateTimeOffSetDefaultValueProviders.cs
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;

namespace Pixeval.Misc;

public class MinDateTimeOffSetDefaultValueProvider : IDefaultValueProvider
{
    public object ProvideValue()
    {
        return DateTimeOffset.MinValue;
    }
}

public class MaxDateTimeOffSetDefaultValueProvider : IDefaultValueProvider
{
    public object ProvideValue()
    {
        return DateTimeOffset.MaxValue;
    }
}

public class CurrentDateTimeOffSetDefaultValueProvider : IDefaultValueProvider
{
    public object ProvideValue()
    {
        return DateTimeOffset.Now;
    }
}

public class DecrementedDateTimeOffSetDefaultValueProvider : IDefaultValueProvider
{
    public object ProvideValue()
    {
        return DateTimeOffset.Now - TimeSpan.FromDays(1);
    }
}