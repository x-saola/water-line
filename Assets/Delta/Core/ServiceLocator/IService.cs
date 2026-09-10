using UnityEngine;

namespace Delta.Core
{
    public interface IService
    {
        void Initialize();
        void Shutdown();
    }
}
