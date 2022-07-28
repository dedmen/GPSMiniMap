using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace GPSMinimapReceiver.Messaging
{
    //public interface IEventAggregator : IDisposable
    //{
    //    IDisposable Subscribe<T>(Action<T> action); //where T : IDomainEvent;
    //    void Publish<T>(T @event); //where T : IDomainEvent;
    //}


    public static class MessagingCenterExtensions
    {
        public static IDisposable SubscribeWithCatch<T>(this IObservable<T> source, Action<T> action)
        {
            return source.Subscribe(action, e =>
            {
                Console.WriteLine($"Messaging exception thrown {e}");
            });
        }

    }

    public class MessagingCenter //: IEventAggregator
    {
        static readonly Subject<object> _subject = new Subject<object>();

        public static IDisposable Subscribe<T>(Action<T> action)
        {
            return _subject.OfType<T>()
                .AsObservable()
                .SubscribeWithCatch(action);
        }

        public static IObservable<T> GetEvent<T>()
        {
            return _subject.OfType<T>();
        }


        public static void Publish<T>(T sampleEvent)
        {
            _subject.OnNext(sampleEvent);
        }

        //public void Dispose()
        //{
        //    _subject.Dispose();
        //}

    }
}
