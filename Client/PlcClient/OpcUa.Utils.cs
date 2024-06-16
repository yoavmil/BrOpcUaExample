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
                    jsonString = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(jsonString), Newtonsoft.Json.Formatting.Indented);
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

        #region read struct, doesn't work
        public static async Task<T?> ReadStructureAsync<T>(Session session, NodeId nodeId)
        {
            try
            {
                DataValueCollection result;
                var diagnosticInfo = new DiagnosticInfoCollection();
                var readValueId = new ReadValueId
                {
                    NodeId = nodeId,
                    AttributeId = Attributes.DataType
                };

                var collection = new ReadValueIdCollection { readValueId };
                session.Read(null, 0, TimestampsToReturn.Both, collection, out result, out diagnosticInfo);

                var node = session.ReadNode(nodeId);

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

        public static byte[] ObjectToByteArray(object obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            return Encoding.UTF8.GetBytes(json);
        }
        public static XmlElement SerializeToXmlElement<T>(T obj)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            XmlDocument xmlDoc = new XmlDocument();

            using (MemoryStream xmlStream = new MemoryStream())
            {
                xmlSerializer.Serialize(xmlStream, obj);
                xmlStream.Position = 0;
                xmlDoc.Load(xmlStream);
            }

            return xmlDoc.DocumentElement;
        }

        public static async Task WriteStructureAsync<T>(Session session, NodeId nodeId, T value)
        {
            DataValueCollection result;
            var diagnosticInfo = new DiagnosticInfoCollection();
            var readTypeId = new ReadValueId
            {
                NodeId = nodeId,
                AttributeId = Attributes.DataType
            };
            var readValueId = new ReadValueId
            {
                NodeId = nodeId,
                AttributeId = Attributes.Value
            };



            WriteValueCollection writeValues = new();

            var collection = new ReadValueIdCollection { readTypeId, readValueId };
            session.Read(null, 0, TimestampsToReturn.Both, collection, out result, out diagnosticInfo);
            //{
            //    var node = session.ReadNode((result[0].Value as NodeId));

            //    ExtensionObject extensionObject = new ExtensionObject();
            //    extensionObject.Body = value;
            //    //extensionObject.Body = ObjectToByteArray(value);
            //    //extensionObject.Body = SerializeToXmlElement(value);
            //    extensionObject.TypeId = NodeId.ToExpandedNodeId(node.NodeId, session.NamespaceUris);

            //    //var wrappedData = new { Data = new { Value = new { Body = value } } };
            //    //var jsonString = JsonConvert.SerializeObject(wrappedData);
            //    //var decoder = new JsonDecoder(jsonString, session.MessageContext);
            //    //var dataValue = decoder.ReadDataValue("Data");
            //    // IEncoder
            //    //var extensionObject = new ExtensionObject(new ExpandedNodeId(nodeId), value);
            //    //DataValue dataValue = new DataValue(extensionObject);
            //    var writeValue = new WriteValue
            //    {
            //        NodeId = nodeId,
            //        AttributeId = Attributes.Value,
            //        // Value = new DataValue(extensionObject)
            //        Value = new DataValue(extensionObject)
            //    };

            //}
            #region
            {

                var extensionObject = result[1].Value as ExtensionObject;
                if (extensionObject != null)
                {
                    var complexType = extensionObject.Body as BaseComplexType;
                    if (complexType != null)
                    {
                        CopySimilarProperties(value, complexType);
                        //foreach (var item in complexType.GetPropertyEnumerator())
                        //{
                        //    if (true && item.PropertyType == typeof(Byte))
                        //    {
                        //        var data = complexType[item.Name];
                        //        if (data != null)
                        //        {
                        //            complexType[item.Name] = (Byte)((Byte)(data) + 1);
                        //        }
                        //    }
                        //}

                        WriteValue nodeToWrite = new WriteValue();
                        nodeToWrite.NodeId = nodeId;
                        nodeToWrite.AttributeId = Attributes.Value;
                        nodeToWrite.Value = new DataValue();
                        nodeToWrite.Value.WrappedValue = result[1].WrappedValue;

                        writeValues.Add(nodeToWrite);
                    }
                }
            }
            #endregion



            var responses = await session.WriteAsync(null, writeValues, CancellationToken.None);
            bool isGood = StatusCode.IsGood(responses.Results[0]);
            if (!isGood)
            {
                throw new Exception($"Write failed: {responses.Results[0]}");
            }
            //throw new NotImplementedException();
        }

        public static void CopySimilarProperties(object source, object target)
        {
            // Serialize the source object to JSON
            string json = JsonConvert.SerializeObject(source);

            // Deserialize the JSON into the target object
            JsonConvert.PopulateObject(json, target);
        }

        #endregion

        private static void BuildWriteValueCollection(object obj, string prefix, NodeId root, ref WriteValueCollection writeValues)
        {
            if (writeValues == null) throw new ArgumentNullException(nameof(writeValues));


            Type type = obj.GetType();
            foreach (PropertyInfo property in type.GetProperties(/*BindingFlags.Public | BindingFlags.Instance*/))
            {
                object value = property.GetValue(obj);
                string propertyName = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
               
                if (value == null || property.PropertyType.IsValueType || property.PropertyType == typeof(string))
                {
                    NodeId nodeId = new NodeId($"{root}.{propertyName}");
                    var writeValue = new WriteValue
                    {
                        NodeId = nodeId,
                        AttributeId = Attributes.Value,
                        Value = new DataValue
                        {
                            Value = value
                        }
                    };
                    writeValues.Add(writeValue);
                }
                else if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType) && property.PropertyType != typeof(string))
                {
                    NodeId nodeId = new NodeId($"{root}.{propertyName}");
                    var writeValue = new WriteValue
                    {
                        NodeId = nodeId,
                        AttributeId = Attributes.Value,
                        Value = new DataValue(new Variant(value))
                    };
                    writeValues.Add(writeValue);
                }
                else
                {
                    BuildWriteValueCollection(value, propertyName, root, ref writeValues);
                }
            }
        }

        public static async Task WriteStructureAsync_<T>(Session session, NodeId nodeId, T value)
        {
            DataValueCollection result;
            var diagnosticInfo = new DiagnosticInfoCollection();
            var readValueId = new ReadValueId
            {
                NodeId = nodeId,
                AttributeId = Attributes.DataType
            };

            var collection = new ReadValueIdCollection { readValueId };
            session.Read(null, 0, TimestampsToReturn.Both, collection, out result, out diagnosticInfo);

            var node = session.ReadNode(nodeId);

            WriteValueCollection writeValues = new();
            BuildWriteValueCollection(value, "", nodeId, ref writeValues);

            var responses = await session.WriteAsync(null, writeValues, CancellationToken.None);
            for (int i = 0; i < responses.Results.Count; i++)
            {
                bool isGood = StatusCode.IsGood(responses.Results[i]);
                if (!isGood)
                {
                    throw new Exception($"Write failed: {responses.Results[i]}");
                }
            }

            throw new NotImplementedException();
        }
    }


}
