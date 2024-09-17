using CsSandboxing;

namespace CsPlugin
{
    public class MyPlugin : ICallable
    {
        public void CallMe(string data)
        {
            TextWriter writer = new StringWriter();
            //Newtonsoft.Json.JsonSerializer.CreateDefault().Serialize(writer, new int[] { 29, 45 });
            Console.WriteLine($"I AM CALLED2! {data} {writer.ToString()}");
            File.WriteAllText("output2.txt", "this is file");

            /*unsafe
            {
                int yourMom = 28;
                int* theNull = &yourMom;
                Console.WriteLine($"bad ub {*(theNull + 17)}");
            }*/
        }

        public Type TestType()
        {
            return typeof(File);
        }
    }
}