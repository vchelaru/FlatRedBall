namespace LevelEditor
{
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Main(string[] args)
        {
            {
                using (var game = new Game1())
                {
                    game.Run();
                }
            }
        }
    }
}

