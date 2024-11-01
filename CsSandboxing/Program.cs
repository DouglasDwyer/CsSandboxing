using Gdk;
using Gtk;

namespace CsSandboxing;

class Test : Gtk.Window
{
    public Test() : base("among us") {
        var butt = new Button();
        butt.Label = "when i worked at microsoft";
        butt.Clicked += (_, _) => Console.WriteLine("we had an entire colony of ants dedicated to making one button");
        Add(butt);
    }

    protected override bool OnDeleteEvent(Event e)
    {
        Console.WriteLine("KYS!");
        Application.Quit();
        return true;
    }
}

internal class Program
{
    public static void Main(string[] args)
    {
        Application.Init();

        var winder = new Test();
        winder.ShowAll();
        Application.Run();
        winder.Destroy();
        Console.ReadKey();
    }
}