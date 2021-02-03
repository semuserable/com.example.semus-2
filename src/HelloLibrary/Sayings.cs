namespace HelloLibrary
{
    public static class Greetings
    {
        public static string Say()
            => $"Hello from {nameof(Say)}!";
    }
    
    public static class Goodbye
    {
        public static string Say()
            => "Bye!";
    }
}
