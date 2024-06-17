using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Client.ComplexTypes;
using Opc.Ua.Configuration;

namespace PlcClient
{
    public class BrDevice
    {
        private readonly ApplicationInstance _application;
        private string _plcOpcUrl = "opc.tcp://127.0.0.1:4840";

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
            await ReadInitialNodeValues();
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

        // TODO demonstrate and move to OpcUtils
        private void WriteValue(Session session, NodeId variableId, DataValue value)
        {
            WriteValue nodeToWrite = new WriteValue();
            nodeToWrite.NodeId = variableId;
            nodeToWrite.AttributeId = Attributes.Value;
            nodeToWrite.Value = new DataValue();
            nodeToWrite.Value.WrappedValue = value.WrappedValue;

            WriteValueCollection nodesToWrite = new WriteValueCollection();
            nodesToWrite.Add(nodeToWrite);

            // read the attributes.
            StatusCodeCollection results = null;
            DiagnosticInfoCollection diagnosticInfos = null;

            ResponseHeader responseHeader = session.Write(
                null,
                nodesToWrite,
                out results,
                out diagnosticInfos);

            ClientBase.ValidateResponse(results, nodesToWrite);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToWrite);

            // check for error.
            if (StatusCode.IsBad(results[0]))
            {
                throw ServiceResultException.Create(results[0], 0, diagnosticInfos, responseHeader.StringTable);
            }
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

        private async Task<object?> ReadNodeValueAsync(NodeId nodeId)
        {
            var readValueId = new ReadValueId
            {
                NodeId = nodeId,
                AttributeId = Attributes.Value
            };

            var readValueIds = new ReadValueIdCollection { readValueId };

            var requestHeader = new RequestHeader();
            var readResults = await _session?.ReadAsync(requestHeader, 0, TimestampsToReturn.Both, readValueIds, CancellationToken.None);

            if (StatusCode.IsGood(readResults.Results[0].StatusCode))
            {
                return readResults.Results[0].Value;
            }

            return null;
        }

        private async Task<bool> WriteNodeValueAsync(NodeId nodeId, object value)
        {
            var isWritable = await IsNodeWritableAsync(nodeId);
            if (!isWritable) return false;
            var writeValue = new WriteValue
            {
                NodeId = nodeId,
                AttributeId = Attributes.Value,
                Value = new DataValue { Value = value, }
            };

            var writeValues = new WriteValueCollection { writeValue };

            var requestHeader = new RequestHeader();
            var writeResults = await _session?.WriteAsync(requestHeader, writeValues, CancellationToken.None);

            bool isGood = StatusCode.IsGood(writeResults.Results[0]);
            if (!isGood)
            {
                throw new Exception($"Write failed: {writeResults.Results[0]}");
            }
            return isGood;
        }

        private async Task<bool> IsNodeWritableAsync(NodeId nodeId)
        {
            var readValueId = new ReadValueId
            {
                NodeId = nodeId,
                AttributeId = Attributes.AccessLevel
            };

            var readValueIds = new ReadValueIdCollection { readValueId };

            var requestHeader = new RequestHeader();
            var readResults = await _session?.ReadAsync(requestHeader, 0, TimestampsToReturn.Both, readValueIds, CancellationToken.None);

            if (StatusCode.IsGood(readResults.Results[0].StatusCode))
            {
                var accessLevel = (byte)readResults.Results[0].Value;
                return (accessLevel & AccessLevels.CurrentWrite) != 0;
            }

            return false;
        }

        private async Task<NodeId?> GetNodeDataTypeAsync(NodeId nodeId)
        {
            var readValueId = new ReadValueId
            {
                NodeId = nodeId,
                AttributeId = Attributes.DataType
            };

            var readValueIds = new ReadValueIdCollection { readValueId };

            var requestHeader = new RequestHeader();
            var readResults = await _session?.ReadAsync(requestHeader, 0, TimestampsToReturn.Both, readValueIds, CancellationToken.None);

            if (StatusCode.IsGood(readResults.Results[0].StatusCode))
            {
                return (NodeId)readResults.Results[0].Value;
            }

            return null;
        }

        Session? _session;
        #region nodes 
        private async Task ReadInitialNodeValues()
        {
            if (_counterNodeId != null) _counter = await ReadNodeValueAsync(_counterNodeId) as byte? ?? 0;
            if (_flagNodeId != null) _flag = await ReadNodeValueAsync(_flagNodeId) as bool? ?? false;
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
                _ = WriteNodeValueAsync(_flagNodeId, value);
                // the event will set the internal _flag
            }
        }
        private bool _flag;
        NodeId _flagNodeId = new NodeId("ns=6;s=::AsGlobalPV:flag");
        #endregion
        

        #endregion nodes
    }
}
