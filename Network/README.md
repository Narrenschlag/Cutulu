# Networking
As the name suggests, this module provides you with everything you need to create an efficient network relying application. 
First and foremost it provides you with a high performance and well commented networking base.

The already integrated protocols are TCP and UDP. You may override the virtual functions to implement your own protocols.

# Host
The [Host Manager](./Host/HostManager.cs) is the central class of the networking module. It needs to run so the clients can connect to it.
Start by initizalizing a new host as a value in your class. When you invoke the `Start` function it will start the host and establish the connections on the given ports.
Stop the host by invoking the `Stop` function.

# Client
The [Client Manager](./Client/ClientManager.cs) connects to a host and exchanges data with it.
Start by initizalizing a new client as a value in your class. When you invoke the `Start` function it will start the client and connect to the given host on the given ports.

# Some extra information
- `UDP` packets are only sent between host and client after establishing a tcp connection and exchanging the senders remote endpoint.
- `UDP` packets are not guaranteed to arrive in order nor at all.
- Use the [PacketOrganizer](./Addons/OrderedPackets/PacketOrganizer.cs) to organize your `UDP` packets in order.
- Do not spam frequent updates like position or velocity every frame via `TCP`. Use `UDP` instead.

- Take a look at the [Addons](./Addons/README.md)