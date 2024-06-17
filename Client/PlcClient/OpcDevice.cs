using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Client.ComplexTypes;
using Opc.Ua.Configuration;

namespace PlcClient
{
    public class OpcDevice
    {
        private readonly ApplicationInstance _application;
        private string _plcOpcUrl = "opc.tcp://127.0.0.1:4840";

        public OpcDevice()
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
            _session = Session.Create(config, endpoint, false, "", 60000, new UserIdentity(new AnonymousIdentityToken()), null).Result;

            // this loads the defined types from the server, I think
            ComplexTypeSystem complexTypeSystem = new ComplexTypeSystem(_session);
            await complexTypeSystem.Load().ConfigureAwait(false);

            AddSubscriptions();

            await ReadInitialNodeValues();

            await OpcUtils.PrintGlobalTypeNames(_session, 6);
            OpcUtils.PrintDataTypeSystem(_session, 6);

            await DemonstrateReadVariables();
            await DemonstrateWriteVariables();
            await DemonstraitUsingHandles();
            await ReadInitialNodeValues();
        }

        private async Task DemonstraitUsingHandles()
        {
            var st1Handle = await CreateVariableHandleAsync<TDOs.Struct1>("struct1");
            await st1Handle.WriteValueAsync(new TDOs.Struct1());
        }

        private async Task DemonstrateWriteVariables()
        {
            NodeId struct1NodeId = new NodeId($"ns=6;s=::AsGlobalPV:struct1");
            NodeId struct2NodeId = new NodeId($"ns=6;s=::AsGlobalPV:struct2");

            var s2_modified = new TDOs.Struct2
            {
                myByte = 0xa0,
                myFloat = -0.123f
            };

            await OpcUtils.WriteStructureAsync(_session, struct2NodeId, s2_modified);

            var s1_modified = new TDOs.Struct1
            {
                enum1 = TDOs.Enum1.Option1_0,
                str="written from client",
                int_array=new byte[]
                {
                    9,8,7,6,5,4,3,2,1,0
                },
                myFloat = (float)Math.E,
                inner_struct = new TDOs.Struct2
                {
                    myByte = 0xAA,
                    myFloat =(float)-Math.PI
                }
            };

            await OpcUtils.WriteStructureAsync(_session, struct1NodeId, s1_modified);
        }

        private async Task DemonstrateReadVariables()
        {
            NodeId struct1NodeId = new NodeId($"ns=6;s=::AsGlobalPV:struct1");
            NodeId struct2NodeId = new NodeId($"ns=6;s=::AsGlobalPV:struct2");
            NodeId enumNodeId = new NodeId($"ns=6;s=::AsGlobalPV:e1");
            var s1 = await OpcUtils.ReadStructureAsync<TDOs.Struct1>(_session, struct1NodeId);
            var s2 = await OpcUtils.ReadStructureAsync<TDOs.Struct2>(_session, struct2NodeId);
            var e1 = await OpcUtils.ReadStructureAsync<TDOs.Enum1>(_session, enumNodeId);
        }

        void AddSubscriptions()
        {

            // Create a subscription
            var subscription = new Subscription(_session?.DefaultSubscription)
            {
                PublishingInterval = 1000,
                KeepAliveCount = 10,
                LifetimeCount = 30,
                MaxNotificationsPerPublish = 1000,
                Priority = 0
            };

            // Create a monitored item for the specified node
            var monitoredCounter = new MonitoredItem(subscription.DefaultItem)
            {
                StartNodeId = _counterNodeId,
                AttributeId = Attributes.Value,
                SamplingInterval = 1000,
                QueueSize = 10,
                DiscardOldest = true
            };
            monitoredCounter.Notification += OnDataChange;
            subscription.AddItem(monitoredCounter);

            var monitoredFlag = new MonitoredItem(subscription.DefaultItem)
            {
                StartNodeId = _flagNodeId,
                AttributeId = Attributes.Value,
                SamplingInterval = 1000,
                QueueSize = 10,
                DiscardOldest = true
            };
            monitoredFlag.Notification += OnDataChange;
            subscription.AddItem(monitoredFlag);

            _session?.AddSubscription(subscription);
            subscription.Create();
        }

        void OnDataChange(MonitoredItem item, MonitoredItemNotificationEventArgs e)
        {
            foreach (var value in item.DequeueValues())
            {
                if (item.StartNodeId == _counterNodeId) Counter = (byte)value.Value;
                if (item.AttributeId == _flagNodeId) _flag = (bool)value.Value;
            }
        }

        Session? _session;

        #region counter and flag demonstration
        private async Task ReadInitialNodeValues()
        {
            _counter = await OpcUtils.ReadStructureAsync<byte>(_session, _counterNodeId);
            _flag = await OpcUtils.ReadStructureAsync<bool>(_session, _flagNodeId);
        }

        #region counter
        public byte Counter
        {
            get
            {
                return _counter;
            }
            private set
            {
                if (_counter != value)
                {
                    _counter = value;
                    CounterChanged?.Invoke(_counter);
                }
            }
        }
        private byte _counter = 0;
        private NodeId? _counterNodeId = new NodeId($"ns=6;s=::AsGlobalPV:gCounter");
        // ns is namespace, and ns=6 is the namespace of the PLC variables

        public delegate void CounterChangedEventHandler(byte newCounter);
        public event CounterChangedEventHandler CounterChanged;
        #endregion

        #region flag
        public bool Flag
        {
            get
            {
                return _flag;
            }
            set
            {
                _ = OpcUtils.WriteStructureAsync(_session, _flagNodeId, value);
                // the event will set the internal _flag
            }
        }
        private bool _flag;
        NodeId _flagNodeId = new NodeId("ns=6;s=::AsGlobalPV:flag");
        #endregion

        #endregion nodes

        public async Task<IPlcVariableHandle<T>> CreateVariableHandleAsync<T>(
                string variableName,
                string programName = ""
            ) where T : new()
        {
            if (string.IsNullOrEmpty(programName)) programName = "AsGlobalPV";
            PlcVariableHandle<T> handle = new()
            {
                Name = variableName,
                Program = programName,
                NodeId = new NodeId($"ns=6;s=::{programName}:{variableName}"),
                Session = _session
            };

            // Try to read initial values and see if the variable name exist and if
            // it matches the type T
            try
            {
                DataValue value = await _session.ReadValueAsync(handle.NodeId);
                ExtensionObject extensionObject = value.Value as ExtensionObject;
                if (extensionObject != null && extensionObject.Body is BaseComplexType)
                {
                    var complexType = extensionObject.Body as BaseComplexType;
                    if (complexType != null)
                        handle.ExtObj = extensionObject;

                    if (complexType != null && !AreAllFieldsPresent(new T(), complexType))
                        throw new InvalidOperationException($"Type {nameof(T)} does not match variable {variableName}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Variable {variableName} not found: {ex.Message}");
            }

            return handle;
        }

        private static bool AreAllFieldsPresent(object source, object target)
        {
            if (source == null || target == null)
            {
                return false;
            }

            // Serialize both objects to JSON
            string sourceJson = JsonConvert.SerializeObject(source);
            string targetJson = JsonConvert.SerializeObject(target);

            // Parse the JSON strings into JObject
            JObject sourceJObject = JObject.Parse(sourceJson);
            JObject targetJObject = JObject.Parse(targetJson);

            // Compare the structures
            return AreAllFieldsPresent(sourceJObject, targetJObject);
        }

        private static bool AreAllFieldsPresent(JObject sourceJObject, JObject targetJObject)
        {
            foreach (var property in sourceJObject.Properties())
            {
                JToken targetToken;
                if (!targetJObject.TryGetValue(property.Name, out targetToken))
                {
                    return false; // Field not found in the target
                }

                if (property.Value.Type == JTokenType.Object)
                {
                    if (targetToken.Type != JTokenType.Object || !AreAllFieldsPresent((JObject)property.Value, (JObject)targetToken))
                    {
                        return false; // Nested object does not match
                    }
                }
            }

            return true; // All fields are present
        }
    }
}
