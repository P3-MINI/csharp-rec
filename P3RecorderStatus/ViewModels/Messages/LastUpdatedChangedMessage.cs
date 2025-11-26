using CommunityToolkit.Mvvm.Messaging.Messages;
using System;

namespace P3RecorderStatus.ViewModels.Messages;

public sealed class LastUpdatedChangedMessage : ValueChangedMessage<DateTime?>
{
    public LastUpdatedChangedMessage(DateTime? value) : base(value)
    {
    }
}
