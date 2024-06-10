using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using Opc.Ua.Client.ComplexTypes;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Globalization;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;

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

        /// <summary>
        /// Helper to cast a enumeration node value to an enumeration type.
        /// </summary>
        public static void CastInt32ToEnum(Session session, VariableNode variableNode, DataValue value)
        {
            if (value.Value?.GetType() == typeof(Int32))
            {
                // test if this is an enum datatype?
                Type systemType = session.Factory.GetSystemType(
                    NodeId.ToExpandedNodeId(variableNode.DataType, session.NamespaceUris)
                    );
                if (systemType != null)
                {
                    value.Value = Enum.ToObject(systemType, value.Value);
                }
            }
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

        public static string GetVauleAsJson(Session session, string name, DataValue value, bool prettify = true)
        {
            try
            {
                var jsonEncoder = new JsonEncoder(session.MessageContext, false);
                jsonEncoder.WriteDataValue(name, value);
                var jsonString = jsonEncoder.CloseAndReturnText();
                if (prettify)
                    jsonString = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(jsonString), Formatting.Indented);
                return jsonString;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to format the JSON output:", ex.Message);
                throw;
            }
        }

        public static void PrintValueAsJson(Session session, string name, DataValue value)
        {
            Debug.Write(GetVauleAsJson(session, name, value, true));
        }

        public static async Task<T?> ReadStructure<T>(Session session, NodeId nodeId)
        {
            try
            {
                var value = await session.ReadValueAsync(nodeId);
                string jsonString = GetVauleAsJson(session, "Data", value, true);
                var jObject = JObject.Parse(jsonString);
                var valueField = jObject["Data"]["Value"]; // get rid of the things that wrap the data
                var data = JsonConvert.DeserializeObject<T>(valueField.ToString());
                return data;
            }
            catch (Exception ex)
            {
                return default(T);
            }
        }
    }
}
