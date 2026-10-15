namespace Application.Abstractions
{
    public interface IPasswordHashService
    {
        bool Verify(string password, string passwordHash);
        string Hash(string password);
    }
}
