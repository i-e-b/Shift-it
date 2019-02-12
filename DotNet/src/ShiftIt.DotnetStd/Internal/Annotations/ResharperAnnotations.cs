using System;
#pragma warning disable 1591
// ReSharper disable once CheckNamespace
namespace JetBrains.Annotations {
    /// <summary>Marked element could be <c>null</c></summary>
    [AttributeUsage(AttributeTargets.All)] internal sealed class CanBeNullAttribute : Attribute { }
    /// <summary>Marked element could never be <c>null</c></summary>
    [AttributeUsage(AttributeTargets.All)] internal sealed class NotNullAttribute : Attribute { }
    /// <summary>IEnumerable, Task.Result, or Lazy.Value property can never be null.</summary>
    [AttributeUsage(AttributeTargets.All)] internal sealed class ItemNotNullAttribute : Attribute { }
    /// <summary>IEnumerable, Task.Result, or Lazy.Value property can be null.</summary>
    [AttributeUsage(AttributeTargets.All)]internal sealed class ItemCanBeNullAttribute : Attribute { }
}