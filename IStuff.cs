using System.Threading.Tasks;

namespace WebApplicationHangfire
{
  public interface IStuff
  {
    Task LongRunningStuff();
  }
}