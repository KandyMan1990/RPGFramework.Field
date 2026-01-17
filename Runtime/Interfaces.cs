using System.Threading.Tasks;

namespace RPGFramework.Field
{
    public interface IField
    {
        Task OnEnterAsync();
        Task OnExitAsync();
    }
}