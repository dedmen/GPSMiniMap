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

    public class MessagingCenter //: IEventAggregator
    {
        static readonly Subject<object> _subject = new Subject<object>();

        public static IDisposable Subscribe<T>(Action<T> action)
        {
            return _subject.OfType<T>()
                .AsObservable()
                .Subscribe(x =>
                {
                    try
                    {
                        action(x);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Messaging exception thrown {e}");
                        throw;
                    }

                });
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
