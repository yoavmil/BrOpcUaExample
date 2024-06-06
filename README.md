# B&R Controller <=> dotnet app

This is a minimal example of B&R PLC simlutor running, and a PC host app connecting to it, reading and writing variables. The B&R IDE is called Automation Studio, in short **AS**.

## The PLC side

The PLC has 2 global variables: `flag` and `gCounter`. In C++ means, the controller does this:

```c++
bool flag;
uint8_t gCounter;
while (true) {
    if (flag) gCounter++;
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
	(* Insert code here *)
	 
END_PROGRAM
```

The code needs to compile and transfared to the simulator.

## The Host Side

The connection between a PLC and a dotnet PC app is by using `OPC UA` protocol. This replaces the old (and obsolete) `PVI` interface.

> Note: why not `PVI`? It isn't supported by modern dotnet, it isn't secure, and it looks as BR doesn't develop it anymore. `OPC UA` is an open protocol adopted by many PLC vendors.

Check list:
- Configuration View > your controller > Connectivity > add 

## Client side

To connect to a variable we need to know its full address, which contains a namespace, the program name and finally the variable name. The variables namespace is 6. 

for example if in program `MyProgram` the is a variable `MyVariable`, the address would be: `"ns=6;s=::MyProgram:MyVariable"`.

### Client SDK options

there is more than one option out there:
- [unified-automation](https://www.unified-automation.com/products/client-sdk/net-ua-client-sdk.html), commercial.
- [QuickOPC](https://www.opclabs.com/products/quickopc), commercial.
- [technosoftware](https://technosoftware.com/?product=opc-ua-client-net/), commercial.
- [OPCFoundation](https://www.nuget.org/packages/OPCFoundation.NetStandard.Opc.Ua/)
- ~~https://github.com/joc-luis/OPCUaClient~~, uses OPCFoundation internally.



## Resources and Tools
- https://www.youtube.com/watch?v=0RO-Veo4mBc&ab_channel=MA-ITMyAutomation-KennisenKundeinIAenIT
- https://github.com/rparak/OPCUA_Simple
- [UaExpert](https://www.unified-automation.com/downloads/opc-ua-clients.html) - at client demo tool
- [Min. Console Example](https://stackoverflow.com/a/30625358/2378218)
- [OPC Foundation Help](https://opcfoundation.github.io/UA-.NETStandard/help/)