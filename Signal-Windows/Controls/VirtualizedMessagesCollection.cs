
using Signal_Windows.Models;
using Signal_Windows.Storage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signal_Windows.Controls
{
    public class SignalMessageContainer
    {
        public SignalMessage Message;
        public int Index;
        public SignalMessageContainer(SignalMessage message, int index)
        {
            Message = message;
            Index = index;
        }
    }

    public class SignalUnreadMarker
    {
        public string Text = "";
    }

    public class VirtualizedCollection : IList, INotifyCollectionChanged
    {
        private const int PAGE_SIZE = 50;
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        private Dictionary<int, IList<SignalMessageContainer>> Cache = new Dictionary<int, IList<SignalMessageContainer>>();
        private SignalConversation Conversation;
        private SignalUnreadMarker UnreadMarker = new SignalUnreadMarker();
        public int UnreadMarkerIndex = -1;

        public VirtualizedCollection(SignalConversation c)
        {
            Conversation = c;
            if (Conversation.LastSeenMessageIndex > 0 && Conversation.LastSeenMessageIndex < Conversation.MessagesCount )
            {
                UnreadMarkerIndex = (int) Conversation.LastSeenMessageIndex;
                UnreadMarker.Text = Conversation.UnreadCount > 1 ? $"{Conversation.UnreadCount} new messages" : "1 new message";
            }
            else
            {
                UnreadMarkerIndex = -1;
            }
        }

        private static int GetPageIndex(int itemIndex)
        {
            return itemIndex / PAGE_SIZE;
        }


        public object this[int index]
        {
            get
            {
                if (UnreadMarkerIndex > 0)
                {
                    if (index < UnreadMarkerIndex)
                    {
                        return Get(index);
                    }
                    else if (index == UnreadMarkerIndex)
                    {
                        return UnreadMarker;
                    }
                    else
                    {
                        return Get(index - 1);
                    }
                }
                else
                {
                    return Get(index);
                }
            }
            set => throw new NotImplementedException();
        }

        private SignalMessageContainer Get(int index)
        {
            int inpageIndex = index % PAGE_SIZE;
            int pageIndex = GetPageIndex(index);
            if (!Cache.ContainsKey(pageIndex))
            {
                Debug.WriteLine($"cache miss {pageIndex}");
                Cache[pageIndex] = SignalDBContext.GetMessagesLocked(Conversation, pageIndex * PAGE_SIZE, PAGE_SIZE);
            }
            var page = Cache[pageIndex];
            var item = page[inpageIndex];
            return page[inpageIndex];
        }

        public bool IsFixedSize => false;

        public bool IsReadOnly => false;

        public int Count
        {
            get
            {
                if (UnreadMarkerIndex > 0)
                {
                    return (int)Conversation.MessagesCount + 1;
                }
                else
                {
                    return (int)Conversation.MessagesCount;
                }
            }
        }

        public bool IsSynchronized => false;

        public object SyncRoot => this;

        /// <summary>
        /// "Adds" a SignalMessageContainer to this virtualized collection.</summary>
        /// <remarks>
        /// The message may (if incoming) or may not (if outgoing) already be present in the database, so we explicitly insert at the correct position in the cache line.
        /// Count is mapped to the SignalConversation's MessagesCount, so callers must update appropriately before calling this method, and no async method must be called in between.</remarks>
        /// <param name="value">The object to add to the VirtualizedMessagesCollection.</param>
        /// <returns>The position into which the new element was inserted, or -1 to indicate that the item was not inserted into the collection.</returns>
        public int Add(object value, bool forcedScroll)
        {
            if (forcedScroll && UnreadMarkerIndex > 0)
            {
                var old = UnreadMarkerIndex;
                UnreadMarkerIndex = -1;
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, UnreadMarker, old));
            }
            var message = value as SignalMessageContainer;
            int inpageIndex = message.Index % PAGE_SIZE;
            int pageIndex = GetPageIndex(message.Index);
            Debug.WriteLine($"VirtualizedCollection.Add Id={message.Message.Id} Index={message.Index} PageIndex={pageIndex} InpageIndex={inpageIndex} ");
            if (!Cache.ContainsKey(pageIndex))
            {
                Cache[pageIndex] = SignalDBContext.GetMessagesLocked(Conversation, pageIndex * PAGE_SIZE, PAGE_SIZE);
            }
            Cache[pageIndex].Insert(inpageIndex, message);
            int virtualIndex = GetVirtualIndex(message.Index);
            Debug.WriteLine($"NotifyCollectionChangedAction.Add index={message.Index} virtualIndex={virtualIndex}");
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, message, virtualIndex));
            return message.Index;
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(object value)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public int IndexOf(object value)
        {
            if (value is SignalMessageContainer)
            {
                SignalMessageContainer smc = (SignalMessageContainer) value;
                return GetVirtualIndex(smc.Index);
            }
            else if (value is SignalUnreadMarker)
            {
                return UnreadMarkerIndex;
            }
            else
            {
                return -1;
            }
        }

        internal int GetVirtualIndex(int rawIndex)
        {
            if (UnreadMarkerIndex > 0)
            {
                if (rawIndex < UnreadMarkerIndex)
                {
                    return rawIndex;
                }
                else
                {
                    return rawIndex + 1;
                }
            }
            else
            {
                return rawIndex;
            }
        }

        public void Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        public void Remove(object value)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public int Add(object value)
        {
            throw new NotImplementedException();
        }
    }
}
