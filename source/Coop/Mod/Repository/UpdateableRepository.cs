using Common;
using System;

namespace Coop.Mod.Repository
{
    public interface IUpdateableRepository
    {
        void Add(IUpdateable updateable);
        void UpdateAll(TimeSpan timeSpan);
    }

    public class UpdateableRepository : IUpdateableRepository
    {
        private readonly UpdateableList Updateables;

        public UpdateableRepository()
        {
            Updateables = new UpdateableList();
        }

        public void Add(IUpdateable updateable)
        {
            Updateables.Add(updateable);
        }

        public void UpdateAll(TimeSpan timeSpan)
        {
            Updateables.UpdateAll(timeSpan);
        }
    }

}
