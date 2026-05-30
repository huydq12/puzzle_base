using System.Collections.Generic;

namespace Utils.Pattern
{
    public class Observer : Singleton<Observer>
    {
        public delegate void CallBackObserver(object data);

        Dictionary<string, HashSet<CallBackObserver>> dictObserver = new Dictionary<string, HashSet<CallBackObserver>>();

        public void AddObserver(string topicName, CallBackObserver callbackObserver)
        {
            HashSet<CallBackObserver> listObserver = this.CreateListObserverForTopic(topicName);
            listObserver.Add(callbackObserver);
        }

        public void RemoveObserver(string topicName, CallBackObserver callbackObserver)
        {
            HashSet<CallBackObserver> listObserver = this.CreateListObserverForTopic(topicName);
            listObserver.Remove(callbackObserver);
        }

        public void Notify(string topicName, object Data)
        {
            HashSet<CallBackObserver> listObserver = this.CreateListObserverForTopic(topicName);
            foreach (CallBackObserver observer in listObserver)
            {
                observer(Data);
            }
        }
        public void Notify(string topicName)
        {
            HashSet<CallBackObserver> listObserver = this.CreateListObserverForTopic(topicName);
            foreach (CallBackObserver observer in listObserver)
            {
                observer(null);
            }
        }

        protected HashSet<CallBackObserver> CreateListObserverForTopic(string topicName)
        {
            if (!this.dictObserver.ContainsKey(topicName))
                this.dictObserver.Add(topicName, new HashSet<CallBackObserver>());
            return this.dictObserver[topicName];
        }

    }
}
