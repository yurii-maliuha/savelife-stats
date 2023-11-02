namespace SaveLife.Stats.Worker.Exceptions
{
    internal class ServiceOverwhelmedException : Exception
    {
        public ServiceOverwhelmedException()
        {
        }

        public ServiceOverwhelmedException(string? message) : base(message)
        {
        }
    }
}
