using System.Threading.Tasks;

namespace WebApplicationHangfire
{
  public class Stuff : IStuff
  {
    public Task LongRunningStuff() => Task.Delay(5000);
  }
}