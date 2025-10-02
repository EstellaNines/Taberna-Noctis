using UnityEngine.Events;

// 简版：泛型消息负载容器定义
public interface IMessageData { }

public class MessageData<T> : IMessageData
{
    public UnityAction<T> MessageEvents;

    public MessageData(UnityAction<T> action)
    {
        MessageEvents += action;
    }
}