using Newtonsoft.Json;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Client.ComplexTypes;
using System.Diagnostics;

namespace PlcClient
{
    internal class OpcUtils
    {

        /// <summary>
        /// Gets all the Nodes available, some of them may be doubled, 
        /// one for a string identifier and one for a numeric identifier,
        /// I don't know how to filter them yet.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public static IList<INode> BrowseAllVariables(Session session)
        {
            var result = new List<INode>();
            var nodesToBrowse = new ExpandedNodeIdCollection();
            nodesToBrowse.Add(ObjectIds.ObjectsFolder);

            while (nodesToBrowse.Count > 0)
            {
                var nextNodesToBrowse = new ExpandedNodeIdCollection();
                foreach (var node in nodesToBrowse)
                {
                    try
                    {
                        var organizers = session.NodeCache.FindReferences(
                            node,
                            ReferenceTypeIds.Organizes,
                            false,
                            false);
                        var components = session.NodeCache.FindReferences(
                            node,
                            ReferenceTypeIds.HasComponent,
                            false,
                            false);
                        var properties = session.NodeCache.FindReferences(
                            node,
                            ReferenceTypeIds.HasProperty,
                            false,
                            false);
                        nextNodesToBrowse.AddRange(organizers
                            .Where(n => n is ObjectNode)
                            .Select(n => n.NodeId).ToList());
                        nextNodesToBrowse.AddRange(components
                            .Where(n => n is ObjectNode)
                            .Select(n => n.NodeId).ToList());
                        result.AddRange(organizers.Where(n => n is VariableNode));
                        result.AddRange(components.Where(n => n is VariableNode));
                        result.AddRange(properties.Where(n => n is VariableNode));
                    }
                    catch (ServiceResultException sre)
                    {
                        if (sre.StatusCode == StatusCodes.BadUserAccessDenied)
                        {
                            Debug.WriteLine($"Access denied: Skip node {node}.");
                        }
                    }
                }
                nodesToBrowse = nextNodesToBrowse;
            }
            return result;
        }

        public static async Task PrintGlobalTypeNames(Session session, int namespaceIndex)
        {
            var complexTypeSystem = new ComplexTypeSystem(session);
            await complexTypeSystem.Load();

            Debug.WriteLine($"Custom types defined for this session:");
            foreach (var type in complexTypeSystem.GetDefinedTypes().Where((t) => t.Namespace.EndsWith(namespaceIndex.ToString())))
            {
                Debug.WriteLine($"{type.Namespace}/{type.Name}");
            }
        }

        /// <summary>
        /// Another way to print the structs, not sure what is the difference between this and 
        /// PrintDataTypeSystem
        /// </summary>
        /// <param name="session"></param>
        /// <param name="namespaceIndex"></param>
        public static void PrintDataTypeSystem(Session session, int namespaceIndex)
        {
            foreach (var dictionary in session.DataTypeSystem)
            {
                if (dictionary.Key.NamespaceIndex == namespaceIndex)
                {
                    Debug.WriteLine($" + {dictionary.Value.Name}");
                    foreach (var type in dictionary.Value.DataTypes)
                    {
                        Debug.WriteLine($" -- {type.Value.Name}");
                    }
                }
            }
        }
       
        public static async Task<bool> IsNodeWritableAsync(Session session, NodeId nodeId)
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

        public static async Task<T> ReadStructureAsync<T>(Session session, NodeId nodeId) where T : new()
        {
            try
            {
                var value = await session.ReadValueAsync(nodeId);
                T result = new T();
                if (value.Value is ExtensionObject)
                    CopySimilarProperties((value.Value as ExtensionObject).Body, result);
                else
                    result = (T)value.Value;
                    
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Read failed: {ex.Message}");
            }
        }

        public static async Task WriteStructureAsync<T>(Session session, NodeId nodeId, T value, ExtensionObject extObj = null)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            WriteValueCollection writeValues = new();

            if (typeof(T).IsClass)
            {
                if (extObj == null)
                {
                    // to write the value, as a ComplexType, we need some properties that could be read
                    // from the PLC, such as BinaryEncodingId. So we read the structure (at least once)
                    // from the PLC, and replace only the data we want to write, and then write it back.

                    DataValue dataValue = await session.ReadValueAsync(nodeId);
                    extObj = dataValue.Value as ExtensionObject;
                }

                if (extObj == null)
                    throw new InvalidOperationException($"Cannot find the PLC refkection of {nameof(T)} at {nodeId}");
                
                var complexType = extObj.Body as BaseComplexType;
                if (complexType != null)
                {
                    CopySimilarProperties(value, complexType);

                    WriteValue nodeToWrite = new WriteValue();
                    nodeToWrite.NodeId = nodeId;
                    nodeToWrite.AttributeId = Attributes.Value;
                    nodeToWrite.Value = new DataValue();
                    nodeToWrite.Value.WrappedValue = extObj;

                    writeValues.Add(nodeToWrite);
                }
            }
            else
            {
                WriteValue nodeToWrite = new WriteValue();
                nodeToWrite.NodeId = nodeId;
                nodeToWrite.AttributeId = Attributes.Value;
                nodeToWrite.Value = new DataValue() { Value = value };
                writeValues.Add(nodeToWrite);
            }

            var responses = await session.WriteAsync(null, writeValues, CancellationToken.None);
            bool isGood = StatusCode.IsGood(responses.Results[0]);
            if (!isGood)
            {
                throw new Exception($"Write failed: {responses.Results[0]}");
            }
        }

        private static void CopySimilarProperties(object source, object target)
        {
            // Serialize the source object to JSON
            string json = JsonConvert.SerializeObject(source);

            // Deserialize the JSON into the target object
            JsonConvert.PopulateObject(json, target);
        }
    }
}
