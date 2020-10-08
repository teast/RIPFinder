using System;
using Avalonia.Threading;

namespace RIPFinder
{
    public class Dispatcher
    {
        public void Invoke(Action action, DispatcherPriority dispatcherPriority = DispatcherPriority.Background)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => { action(); }, dispatcherPriority);
        }
    }    
}