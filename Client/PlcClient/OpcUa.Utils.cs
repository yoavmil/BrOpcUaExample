using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Client.ComplexTypes;
using System.Diagnostics;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.Reflection;
using System.Collections;

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

        public static async Task<T?> ReadStructureAsync<T>(Session session, NodeId nodeId) where T : new()
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
                return default(T);
            }
        }

        public static async Task WriteStructureAsync<T>(Session session, NodeId nodeId, T value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            // to write the value, as a ComplexType, we need some properties that could be read
            // from the PLC, such as BinaryEncodingId. So we read the structure (at least once)
            // from the PLC, and replace only the data we want to write, and then write it back.
            DataValueCollection readResult;
            var diagnosticInfo = new DiagnosticInfoCollection();
            var readValueId = new ReadValueId
            {
                NodeId = nodeId,
                AttributeId = Attributes.Value
            };

            WriteValueCollection writeValues = new();

            var collection = new ReadValueIdCollection { readValueId };
            session.Read(null, 0, TimestampsToReturn.Both, collection, out readResult, out diagnosticInfo);

            var extensionObject = readResult[0].Value as ExtensionObject;
            if (extensionObject != null)
            {
                var complexType = extensionObject.Body as BaseComplexType;
                if (complexType != null)
                {
                    CopySimilarProperties(value, complexType);

                    WriteValue nodeToWrite = new WriteValue();
                    nodeToWrite.NodeId = nodeId;
                    nodeToWrite.AttributeId = Attributes.Value;
                    nodeToWrite.Value = new DataValue();
                    nodeToWrite.Value.WrappedValue = readResult[0].WrappedValue;

                    writeValues.Add(nodeToWrite);
                }
            }

            var responses = await session.WriteAsync(null, writeValues, CancellationToken.None);
            bool isGood = StatusCode.IsGood(responses.Results[0]);
            if (!isGood)
            {
                throw new Exception($"Write failed: {responses.Results[0]}");
            }
        }

        public static void CopySimilarProperties(object source, object target)
        {
            // Serialize the source object to JSON
            string json = JsonConvert.SerializeObject(source);

            // Deserialize the JSON into the target object
            JsonConvert.PopulateObject(json, target);
        }
    }
}
