using System.Threading.Tasks;

namespace NewRelic.Agent.Core
{
    public abstract class AsyncSingleton<T>
    {
        private Task _initTask;

        private T _instance;

        protected AsyncSingleton(T instance)
        {
            // Set the default instance from the argument, where it might be picked up in
            // the course of executing CreateInstance.
            _instance = instance;

            // Create the real instance we care about, perhaps referencing
            // the default instance in _instance.
            _initTask = Task.Run(async () =>
            {
                _instance = await CreateInstanceAsync().ConfigureAwait(false);
            });

            _initTask.Wait();
        }

        public T ExistingInstance
        {
            get
            {
                return _instance;
            }
        }

        public void SetInstance(T instance)
        {
            _instance = instance;
        }

        protected abstract Task<T> CreateInstanceAsync();
    }
}
