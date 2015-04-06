using StructureMap;

namespace SimpleStorage.IoC
{
    public class IoCFactory
    {
        private Container container;

        private readonly object @lock = new object();

        public Container GetContainer()
        {
            if (container == null)
                lock (@lock)
                    if (container == null)
                        container = new Container(new SimpleStorageRegistry());

            return container;
        }
    }
}