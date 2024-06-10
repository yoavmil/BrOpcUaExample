# B&R Controller <=> dotnet app

This is a minimal example of B&R PLC simlutor running, and a PC host app connecting to it, 
reading and writing variables. 

The B&R IDE is called Automation Studio, in short **AS**. The Host app is called **Client**, 
developed in .NET in Visual Studio 2022.

The projects intension ins't to demonstrate all the connection possiblities, just the bare minimum.

## The PLC side

For the Demo, the PLC has 2 global variables: `flag` and `gCounter`.
In C++ means, the controller does this:

```c++
bool flag;
uint8_t gCounter;
while (true) { // runs forever
    if (flag) gCounter++; // can overflow back to 0
}
```

In AS the global variables are defined at the logical view > Global.var:

```reStructuredText
VAR
	gCounter : USINT;
	flag : BOOL := TRUE;
END_VAR
```

And the program is called **Program** which has the code file `Main.st`.

```reStructuredText
PROGRAM _INIT
	gCounter:=0;
	flag :=TRUE;
END_PROGRAM

PROGRAM _CYCLIC
	IF flag = TRUE THEN
		gCounter := gCounter + 1;		
	END_IF	 
END_PROGRAM

PROGRAM _EXIT
	(* Nothing happens here *)
END_PROGRAM
```

The code needs to compile and transfared to the simulator.

## The Host Side

The connection between a PLC and a dotnet PC app is by using `OPC UA` protocol. This replaces the old (and obsolete) `PVI` interface.

> Note: why not `PVI`? It isn't supported by modern dotnet, it isn't secure, 
and it looks as BR doesn't maintain it anymore. 
`OPC UA` is an open protocol adopted by many PLC vendors.

To enable `OPC UA`, the PLC server needs to be configured.
Check list:
- Configuration View > your controller > Connectivity > add `OPC UA Nodeset file`
- Configuration View > your controller > Connectivity > add `OPC UA Default View file`
- Open the new `OPC UA Default view`, find the variables you want to read and write and 
  enable the tags you want.
- Physical View > Your controller > right click > configuration > OPC-UA System > 
  Activate
- Physical View > Your controller > right click > configuration > OPC-UA System > 
  Information models > PV > Version > 2.0 
  ([allows reading complex types, ex structs](https://opcfoundation.org/forum/opc-ua-implementation-stacks-tools-and-samples/method-call-for-custom-complex-types/#p4097))

## Client side

The Client App has a simple Winform window that can display the counter, and a checkbox to
toggle the flag.

The connection logic is inside `PlcClient.BrDevice`. Just follew the `Connect()` method and 
you catch up.

To connect to a variable we need to know its full address, which contains a namespace, 
the program name and finally the variable name. The variables namespace is 6. 

for example if in program `MyProgram` the is a variable `MyVariable`, the address would be: `"ns=6;s=::MyProgram:MyVariable"`.

To connect to a server, there is some certification validation going around, 
and for that there are 2 files I copied from the OPC Foundation samples: `App.Config` and
`Quickstarts.ReferenceClient.Config.xml`.

## Reading complex structures

This demo shows how to transfer structures that exist both in the server and in the client.
They client classes need to reflect the PLC structures, see `PlcStructs.cs`.

The client reads the variable data as converts it to Json, and later to C# objects,
see `ReadStructure<T>` at `PlcStructs.cs`.

### Client SDK options

there is more than one option out there:
- [unified-automation](https://www.unified-automation.com/products/client-sdk/net-ua-client-sdk.html), commercial.
- [QuickOPC](https://www.opclabs.com/products/quickopc), commercial.
- [technosoftware](https://technosoftware.com/?product=opc-ua-client-net/), commercial.
- [OPCFoundation](https://www.nuget.org/packages/OPCFoundation.NetStandard.Opc.Ua/), I used this one.
- ~~https://github.com/joc-luis/OPCUaClient~~, uses OPCFoundation internally.


## Resources and Tools
- [OPC Foundation Forum](https://opcfoundation.org/forum/)
- [How to activate OPC UA connectivity in B&R mapp View](https://www.youtube.com/watch?v=0RO-Veo4mBc&ab_channel=MA-ITMyAutomation-KennisenKundeinIAenIT)
- [OPC UA Simple App](https://github.com/rparak/OPCUA_Simple)
- [UaExpert](https://www.unified-automation.com/downloads/opc-ua-clients.html) - a client for debugging
- [Json Encode/Decoder Usage](https://github.com/parkey1231/UA-.NETStandard/blob/d31c8cc6e4412f169f56f6c3629c2f748db652ae/SampleApplications/Samples/NetCoreComplexClient/Program.cs#L285)

## Todo
- Hadnle PLC not found
- ~~Read~~/Write and reflect complex types, ex. structs
- variables from an program, and not global.
- write a class that encapsulates the variable and handles the access calls.

## Types Dictionary

| PLC           | OPC UA   | C#       |
| ------------- | -------- | -------- |
| BOOL          | Boolean  | bool     |
| SINT          | SByte    | sbyte    |
| USINT         | Byte     | byte     |
| INT           | Int16    | short    |
| UINT          | UInt16   | ushort   |
| DINT          | Int32    | int      |
| UDINT         | UInt32   | uint     |
| LINT          | Int64    | long     |
| ULINT         | UInt64   | ulong    |
| REAL          | Float    | float    |
| LREAL         | Double   | double   |
| STRING        | String   | string   |
| WSTRING       | String   | string   |
| TIME          | Duration | TimeSpan |
| DATE          | DateTime | DateTime |
| TIME_OF_DAY   | Time     | TimeSpan |
| DATE_AND_TIME | DateTime | DateTime |
| BYTE          | Byte     | byte     |
| WORD          | UInt16   | ushort   |
| DWORD         | UInt32   | uint     |
| LWORD         | UInt64   | ulong    |