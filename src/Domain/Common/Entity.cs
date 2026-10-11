namespace Domain.Common
{
    public abstract class Entity
    {
        public int Id { get; protected set; }

        protected Entity() { }

        protected Entity(int id)
        {
            if (id < 0)
                throw new ArgumentOutOfRangeException(nameof(id));

            Id = id;
        }
    }
}
