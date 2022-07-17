using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class Message
{
    public string type;

    public Message()
    {
        type = this.GetType().Name;
    }
}

public class MessagingSystem : SingletonComponent<MessagingSystem>
{
    public static MessagingSystem Instance
    {
        get => ((MessagingSystem) _Instance);
        set => _Instance = value;
    }

    public delegate bool MessageHandlerDelegate(Message message);

    /// <summary>
    /// 监听列表
    /// </summary>
    /// <returns></returns>
    private Dictionary<string, List<MessageHandlerDelegate>> _listenerDictionary =
        new Dictionary<string, List<MessageHandlerDelegate>>();

    public bool AttachListener(System.Type type, MessageHandlerDelegate handler)
    {
        if (type == null)
        {
            Debug.Log("MessagingSystem: AttachListener failed due to having no"+ "message type specified");
            return false;
        }

        string msgType = type.Name;
        if (!_listenerDictionary.ContainsKey(msgType))
        {
            _listenerDictionary.Add(msgType, new List<MessageHandlerDelegate>());
        }

        List<MessageHandlerDelegate> listenerList = _listenerDictionary[msgType];
        if (listenerList.Contains(handler))
        {
            return false;
        }
        
        listenerList.Add(handler);
        return true;
    }

    private Queue<Message> _messageQueue = new Queue<Message>();

    public bool QueueMessage(Message msg)
    {
        if (!_listenerDictionary.ContainsKey(msg.type))
        {
            return false;
        }
        
        _messageQueue.Enqueue(msg);
        return true;
    }

    private const int _maxQueueProcessingTime = 16667;
    private System.Diagnostics.Stopwatch timer = new Stopwatch();

    private void Update()
    {
        timer.Start();
        while (_messageQueue.Count > 0)
        {
            if (_maxQueueProcessingTime > 0.0f)
            {
                if (timer.Elapsed.Milliseconds > _maxQueueProcessingTime)
                {
                    timer.Stop();
                    return;
                }
            }

            Message msg = _messageQueue.Dequeue();
            if (!TriggerMessage(msg))
            {
                Debug.Log("Error when processing message "+ msg.type);
            }
        }
    }

    public bool TriggerMessage(Message msg)
    {
        string msgType = msg.type;
        if (!_listenerDictionary.ContainsKey(msgType))
        {
            Debug.Log("MessagingSystem: Message \""+msgType+"\" has no listeners!");
            return false;
        }

        List<MessageHandlerDelegate> listenerList = _listenerDictionary[msgType];

        for (int i = 0; i < listenerList.Count; i++)
        {
            if (listenerList[i](msg))
            {
                return true;
            }
        }
        return true;
    }
}
