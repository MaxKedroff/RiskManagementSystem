using Application.Abstractions;

namespace Infrastructure.Security
{
    public sealed class TemporaryPasswordGenerator : ITemporaryPasswordGenerator
    {
        public string Generate()
        {
            return Guid.NewGuid().ToString("N")[..12];
        }
    }
}
