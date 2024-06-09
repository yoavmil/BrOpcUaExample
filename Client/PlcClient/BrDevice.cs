using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System.Net.Sockets;
using static System.Net.Mime.MediaTypeNames;

namespace PlcClient
{
    public class BrDevice 
    {
        private readonly ApplicationInstance _application;
        private string _plcOpcUrl = "opc.tcp://127.0.0.1:4840";
        private string configPath = "App.config";

        public BrDevice()
        {
            string clientAppName = "PlcClient";

            _application = new ApplicationInstance();
            _application.ApplicationName = clientAppName;
            _application.ApplicationType   = ApplicationType.Client;
            _application.ConfigSectionName = "Quickstarts.ReferenceClient";
        }

        public async Task Connect()
        {
            // there are 2 config files, App.config and Quickstarts.ReferenceClient.Config.xml
            // App.config points to Quickstarts.ReferenceClient.Config.xml according to _application.ConfigSectionName
            // both files are copied from Reference Client example project
            await _application.LoadApplicationConfiguration(false);
            bool certOK = await _application.CheckApplicationInstanceCertificate(false, 0);
            if (!certOK)
            {
                throw new Exception("Application instance certificate invalid!");
            }

            var config = _application.ApplicationConfiguration;
            var selectedEndpoint = CoreClientUtils.SelectEndpoint(_plcOpcUrl, useSecurity: false, discoverTimeout: 1000);

            var endpointConfiguration = EndpointConfiguration.Create(config);
            var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);
            var session = Session.Create(config, endpoint, false, "", 60000, new UserIdentity(new AnonymousIdentityToken()), null).Result;

            var rootNode = await session.NodeCache.FindAsync(Objects.RootFolder).ConfigureAwait(false);

            // Assume the global variable is under the Objects folder.
            uint namespaceID = 6; // BR VPI
            var gCounterName = "gCounter"; // Replace with the actual name of the global variable.
            var gCounterNodeString = $"ns={namespaceID};s=::AsGlobalPV:{gCounterName}";
            var gCounterNodeId = new NodeId(gCounterNodeString);
            /*ns=6;s=::AsGlobalPV:gCounter#*/

            var flagName = "flag";
            var flagNodeString = $"ns={namespaceID};s=::AsGlobalPV:{flagName}";
            var flagNodeId = new NodeId(flagNodeString);


            await WriteNodeValueAsync(session, gCounterNodeId, (byte)0);

            // Read the value of the global variable
            var gCounterValue = await ReadNodeValueAsync(session, gCounterNodeId);

            if (gCounterValue != null)
            {
                Console.WriteLine($"Global Variable Value: {gCounterValue}");
            }
            else
            {
                Console.WriteLine("Failed to read the global variable value.");
            }

            // Read the value of the global variable
            var flagValue = await ReadNodeValueAsync(session, flagNodeId);

            if (flagValue != null)
            {
                Console.WriteLine($"Global Variable Value: {gCounterValue}");
            }
            else
            {
                Console.WriteLine("Failed to read the global variable value.");
            }

            await WriteNodeValueAsync(session, flagNodeId, false);

            // Read the value of the global variable
            flagValue = await ReadNodeValueAsync(session, flagNodeId);

            if (flagValue != null)
            {
                Console.WriteLine($"Global Variable Value: {gCounterValue}");
            }
            else
            {
                Console.WriteLine("Failed to read the global variable value.");
            }

            // Get the type of the global variable
            var dataTypeNodeId = await GetNodeDataTypeAsync(session, gCounterNodeId);
            if (dataTypeNodeId != null)
            {
                var dataTypeName = GetDataTypeName(dataTypeNodeId);
                Console.WriteLine($"Global Variable Data Type: {dataTypeName}");
            }
            else
            {
                Console.WriteLine("Failed to get the data type of the global variable.");
            }


            // Close session.
            session.Close();
        }

        private static async Task<object?> ReadNodeValueAsync(Session session, NodeId nodeId)
        {
            var readValueId = new ReadValueId
            {
                NodeId = nodeId,
                AttributeId = Attributes.Value
            };

            var readValueIds = new ReadValueIdCollection { readValueId };

            var requestHeader = new RequestHeader();
            var readResults = await session.ReadAsync(requestHeader, 0, TimestampsToReturn.Both, readValueIds, CancellationToken.None);
            
            if (StatusCode.IsGood(readResults.Results[0].StatusCode))
            {
                return readResults.Results[0].Value;
            }

            return null;
        }

        private static async Task<bool> WriteNodeValueAsync(Session session, NodeId nodeId, object value)
        {
            var isWritable = await IsNodeWritableAsync(session, nodeId);
            if (!isWritable) return false;
            var writeValue = new WriteValue
            {
                NodeId = nodeId,
                AttributeId = Attributes.Value,
                Value = new DataValue { Value = value, }
            };

            var writeValues = new WriteValueCollection { writeValue };

            var requestHeader = new RequestHeader();
            var writeResults = await session.WriteAsync(requestHeader, writeValues, CancellationToken.None);

            bool isGood = StatusCode.IsGood(writeResults.Results[0]);
            if (!isGood)
            {
                throw new Exception($"Write failed: {writeResults.Results[0]}");
            }
            return isGood;
        }

        private static async Task<bool> IsNodeWritableAsync(Session session, NodeId nodeId)
        {
            var readValueId = new ReadValueId
            {
                NodeId = nodeId,
                AttributeId = Attributes.AccessLevel
            };

            var readValueIds = new ReadValueIdCollection { readValueId };

            var requestHeader = new RequestHeader();
            var readResults = await session.ReadAsync(requestHeader, 0, TimestampsToReturn.Both, readValueIds, CancellationToken.None);

            if (StatusCode.IsGood(readResults.Results[0].StatusCode))
            {
                var accessLevel = (byte)readResults.Results[0].Value;
                return (accessLevel & AccessLevels.CurrentWrite) != 0;
            }

            return false;
        }

        private static async Task<NodeId?> GetNodeDataTypeAsync(Session session, NodeId nodeId)
        {
            var readValueId = new ReadValueId
            {
                NodeId = nodeId,
                AttributeId = Attributes.DataType
            };

            var readValueIds = new ReadValueIdCollection { readValueId };

            var requestHeader = new RequestHeader();
            var readResults = await session.ReadAsync(requestHeader, 0, TimestampsToReturn.Both, readValueIds, CancellationToken.None);

            if (StatusCode.IsGood(readResults.Results[0].StatusCode))
            {
                return (NodeId)readResults.Results[0].Value;
            }

            return null;
        }

        private static string GetDataTypeName(NodeId dataTypeNodeId)
        {
            Dictionary<NodeId, Type> dataTypeMapping = new Dictionary<NodeId, Type>
            {
                { DataTypeIds.Boolean, typeof(bool) },
                { DataTypeIds.SByte, typeof(sbyte) },
                { DataTypeIds.Byte, typeof(byte) },
                { DataTypeIds.Int16, typeof(short) },
                { DataTypeIds.UInt16, typeof(ushort) },
                { DataTypeIds.Int32, typeof(int) },
                { DataTypeIds.UInt32, typeof(uint) },
                { DataTypeIds.Int64, typeof(long) },
                { DataTypeIds.UInt64, typeof(ulong) },
                { DataTypeIds.Float, typeof(float) },
                { DataTypeIds.Double, typeof(double) },
                { DataTypeIds.String, typeof(string) },
                { DataTypeIds.DateTime, typeof(DateTime) },
                { DataTypeIds.Guid, typeof(Guid) },
                { DataTypeIds.ByteString, typeof(byte[]) },
                { DataTypeIds.XmlElement, typeof(System.Xml.XmlElement) },
                { DataTypeIds.NodeId, typeof(NodeId) },
                { DataTypeIds.ExpandedNodeId, typeof(ExpandedNodeId) },
                { DataTypeIds.StatusCode, typeof(StatusCode) },
                { DataTypeIds.QualifiedName, typeof(QualifiedName) },
                { DataTypeIds.LocalizedText, typeof(LocalizedText) },
                { DataTypeIds.DataValue, typeof(DataValue) },
                { DataTypeIds.DiagnosticInfo, typeof(DiagnosticInfo) },
                { DataTypeIds.Enumeration, typeof(Enum) },
                { DataTypeIds.Structure, typeof(object) } // For complex structures, typically you will define custom classes or structs
            };

            if (dataTypeMapping.TryGetValue(dataTypeNodeId, out Type csharpType))
            {
                return csharpType.Name;
            }
            return "Unknown";
        }

        public void ReadGlobal()
        {
            //var globalsNode = _client.Read("flag");

            //var flagTag = _client.Read("ns=6;s=::AsGlobalPV:flag");
        }
    }
}
