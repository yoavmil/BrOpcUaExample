using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlcClient
{
    public interface IPlcVariableHandle<T>
    {
        string Name { get; set; }
        string Program { get; set; } // empty for global
        Task<T> ReadValueAsync();
        Task WriteValueAsync(T value);
        event EventHandler ValueChanged;
    }
}
