﻿using Opc.Ua;
using Opc.Ua.Client;

namespace PlcClient
{
    internal class PlcVariableHandle<T> : IPlcVariableHandle<T> where T : new()
    {
        public string Name { get; set; }
        public string Program { get; set; } // TODO check

        public event EventHandler ValueChanged; // TODO

        public Task<T> ReadValueAsync()
        {
            return OpcUtils.ReadStructureAsync<T>(Session, NodeId);
        }

        public Task WriteValueAsync(T value)
        {
            return OpcUtils.WriteStructureAsync(Session, NodeId, value, ExtObj);
        }

        public NodeId NodeId { get; set; }
        public ExtensionObject ExtObj { get; set; } // for complex types
        public Session Session { get; set; }
    }
}
