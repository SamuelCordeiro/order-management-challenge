using System.Diagnostics;

namespace OrderManagement.Infrastructure.Observability;

public static class MessagingActivity
{
    public const string SourceName = "OrderManagement.Messaging";
    private static readonly ActivitySource Source = new(SourceName);

    public static Activity? Start(string operation, Guid orderId, Guid messageId) =>
        Source.StartActivity(operation, ActivityKind.Producer) is { } activity
            ? AddMessageTags(activity, orderId, messageId)
            : null;

    private static Activity AddMessageTags(Activity activity, Guid orderId, Guid messageId)
    {
        activity.SetTag("order.id", orderId);
        activity.SetTag("messaging.message.id", messageId);
        return activity;
    }
}
