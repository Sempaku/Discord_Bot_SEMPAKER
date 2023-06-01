using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            // todo : удаляет все одинаковые url, а надо чтобы первый попавшийся!!
            musicUrlQueue = new Queue<string>(musicUrlQueue.Where(url => url != musicUrl));
        }

        public Queue<string> GetMusicUrlQueue()
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
    }
}