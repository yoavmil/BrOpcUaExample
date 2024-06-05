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


















