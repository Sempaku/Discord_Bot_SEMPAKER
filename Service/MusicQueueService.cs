namespace DotNet.Docker.Service
{
    public class MusicQueueService
    {
        private Queue<string> musicUrlQueue;

        public MusicQueueService()
        {
            musicUrlQueue = new Queue<string>();
        }

        public void Enqueue(string musicUrl)
        {
            musicUrlQueue.Enqueue(musicUrl);
        }

        public string Dequeue()
        {
            return musicUrlQueue.Dequeue();
        }

        public void RemoveFromQueue(string musicUrl)
        {
            // Найти индекс первого вхождения элемента с заданным названием в очереди
            int indexToRemove = musicUrlQueue.TakeWhile(url => url != musicUrl).Count();

            // Удалить элемент из очереди по найденному индексу
            if (indexToRemove < musicUrlQueue.Count)
            {
                musicUrlQueue = new Queue<string>(musicUrlQueue.Where((url, index) => index != indexToRemove));
            }
        }

        public Queue<string> GetQueue()
        {
            return new Queue<string>(musicUrlQueue);
        }

        public bool IsEmpty()
        {
            return musicUrlQueue.Count == 0;
        }

        public void SetQueue(Queue<string> queue)
        {
            musicUrlQueue = queue;
        }

        public void ClearQueue()
        {
            musicUrlQueue.Clear();
        }
    }
}