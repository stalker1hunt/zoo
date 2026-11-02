
namespace ZooWorld
{
    public interface IAnimalLifecycle : IAnimalPopulation
    {
        void Register(AnimalActor animal);
        bool TryRemove(AnimalActor animal, bool eaten);
        void Forget(AnimalActor animal);
        void Clear();
    }
}
