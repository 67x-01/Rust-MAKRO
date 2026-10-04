namespace Rust_Menü;
internal static class Menu
{
    public static void Goster()
    {
        Console.BackgroundColor = ConsoleColor.Yellow;
        Console.Clear();

        Console.ForegroundColor = ConsoleColor.Magenta;

        Console.WriteLine("================================");
        Console.WriteLine("         MADE BY IF ELSE        ");
        Console.WriteLine("================================");

        Console.ResetColor();
        Console.WriteLine("Makro");
        Console.WriteLine("Exit");
    }
}